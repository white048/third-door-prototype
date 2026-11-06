using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public enum CheckpointStatus { None, Saved, Loaded, RecoveredBackup, Missing, Invalid, IoError, Cleared }

// Only explicit callers save. A suspended menu or a preview never changes the checkpoint.
public static class CheckpointStore
{
    private const int FileVersion = 1;
    private const int MaxBytes = 262144;
    private const string FileName = "checkpoint-v1.json";
    public static CheckpointStatus LastStatus { get; private set; }
    public static string LastError { get; private set; } = "";

    [Serializable]
    private sealed class CheckpointFile
    {
        public int Version;
        public string Payload, Digest;
    }

    public static string StoragePath
    {
        get
        {
            if (!EndingArchive.IsValidationRun) return Path.Combine(Application.persistentDataPath, FileName);
            // Validation is never allowed to inspect, overwrite, or clear a player's normal save.
            string scenario = "validation";
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (!argument.StartsWith("--validate-demo=", StringComparison.Ordinal)) continue;
                var clean = new StringBuilder();
                foreach (char c in argument.Substring("--validate-demo=".Length))
                    if (char.IsLetterOrDigit(c) || c == '-' || c == '_') clean.Append(c);
                if (clean.Length > 0) scenario = clean.ToString(0, Math.Min(clean.Length, 80));
                break;
            }
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "QA", "Checkpoints", scenario, FileName));
        }
    }

    public static bool HasCheckpoint
    {
        get { RelationshipRun ignored; return TryLoad(out ignored); }
    }

    public static bool Save(RelationshipRun run)
    {
        if (run == null) return Set(CheckpointStatus.Invalid, "No run to save.");
        if (run.Ended) return Set(CheckpointStatus.Invalid, "A completed case cannot be continued.");
        string temporary = null;
        try
        {
            RelationshipSaveData data = run.ExportSave();
            RelationshipRun verified;
            if (!RelationshipRun.TryRestoreSave(data, out verified)) return Set(CheckpointStatus.Invalid, "Run state could not be validated.");
            string payload = JsonUtility.ToJson(data);
            var file = new CheckpointFile { Version = FileVersion, Payload = payload, Digest = Hash(payload) };
            byte[] bytes = new UTF8Encoding(false, true).GetBytes(JsonUtility.ToJson(file));
            if (bytes.Length > MaxBytes) return Set(CheckpointStatus.Invalid, "Checkpoint is too large.");
            string path = StoragePath;
            temporary = path + ".tmp";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            if (File.Exists(path))
            {
                RelationshipRun prior;
                // A corrupt primary must not replace a known good backup during the next save.
                bool primaryValid = Read(path, out prior) == CheckpointStatus.Loaded;
                File.Replace(temporary, path, primaryValid ? path + ".bak" : null);
            }
            else File.Move(temporary, path);
            return Set(CheckpointStatus.Saved);
        }
        catch (Exception error)
        {
            return Set(CheckpointStatus.IoError, ErrorLabel(error));
        }
        finally
        {
            if (temporary != null)
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (Exception) { /* A stale temporary file is never considered a checkpoint. */ }
            }
        }
    }

    public static bool TryLoad(out RelationshipRun run)
    {
        run = null;
        try
        {
            string path = StoragePath;
            CheckpointStatus primary = Read(path, out run);
            if (primary == CheckpointStatus.Loaded) return Set(CheckpointStatus.Loaded);
            CheckpointStatus backup = Read(path + ".bak", out run);
            if (backup == CheckpointStatus.Loaded) return Set(CheckpointStatus.RecoveredBackup);
            CheckpointStatus failed = primary == CheckpointStatus.IoError || backup == CheckpointStatus.IoError
                ? CheckpointStatus.IoError : primary == CheckpointStatus.Invalid || backup == CheckpointStatus.Invalid
                ? CheckpointStatus.Invalid : CheckpointStatus.Missing;
            return Set(failed, failed == CheckpointStatus.Invalid ? "Checkpoint data is damaged or incompatible."
                : failed == CheckpointStatus.IoError ? "Checkpoint storage could not be read." : "");
        }
        catch (Exception error) { return Set(CheckpointStatus.IoError, ErrorLabel(error)); }
    }

    public static bool Clear()
    {
        try
        {
            string path = StoragePath;
            foreach (string candidate in new[] { path, path + ".bak", path + ".tmp" })
                if (File.Exists(candidate)) File.Delete(candidate);
            return Set(CheckpointStatus.Cleared);
        }
        catch (Exception error) { return Set(CheckpointStatus.IoError, ErrorLabel(error)); }
    }

    private static CheckpointStatus Read(string path, out RelationshipRun run)
    {
        run = null;
        try
        {
            if (!File.Exists(path)) return CheckpointStatus.Missing;
            long size = new FileInfo(path).Length;
            if (size < 1 || size > MaxBytes) return CheckpointStatus.Invalid;
            string json = File.ReadAllText(path, new UTF8Encoding(false, true));
            var file = JsonUtility.FromJson<CheckpointFile>(json);
            if (file == null || file.Version != FileVersion || string.IsNullOrEmpty(file.Payload)
                || string.IsNullOrEmpty(file.Digest) || !string.Equals(file.Digest, Hash(file.Payload), StringComparison.Ordinal))
                return CheckpointStatus.Invalid;
            var data = JsonUtility.FromJson<RelationshipSaveData>(file.Payload);
            if (RelationshipRun.TryRestoreSave(data, out run) && !run.Ended) return CheckpointStatus.Loaded;
            run = null;
            return CheckpointStatus.Invalid;
        }
        catch (IOException) { return CheckpointStatus.IoError; }
        catch (UnauthorizedAccessException) { return CheckpointStatus.IoError; }
        catch (System.Security.SecurityException) { return CheckpointStatus.IoError; }
        catch (Exception) { return CheckpointStatus.Invalid; }
    }

    private static string Hash(string payload)
    {
        using (var sha = SHA256.Create())
        {
            byte[] digest = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var text = new StringBuilder(digest.Length * 2);
            foreach (byte value in digest) text.Append(value.ToString("x2"));
            return text.ToString();
        }
    }
    private static string ErrorLabel(Exception error) => "Checkpoint storage is unavailable (" + error.GetType().Name + ").";
    private static bool Set(CheckpointStatus status, string error = "")
    {
        LastStatus = status; LastError = error;
        return status == CheckpointStatus.Saved || status == CheckpointStatus.Loaded || status == CheckpointStatus.RecoveredBackup || status == CheckpointStatus.Cleared;
    }
}
