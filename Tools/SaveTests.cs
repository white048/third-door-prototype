using System;
using System.Reflection;
using System.Text;

public static class SaveTests
{
    // Comparing the whole value tree makes newly added DTO fields participate automatically.
    private static string Fingerprint(object value)
    {
        if (value == null) return "null";
        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum || value is string) return value.ToString();
        var text = new StringBuilder("{");
        var array = value as Array;
        if (array != null)
        {
            foreach (object item in array) text.Append(Fingerprint(item)).Append(';');
        }
        else
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            Array.Sort(fields, (a, b) => string.CompareOrdinal(a.Name, b.Name));
            foreach (var field in fields) text.Append(field.Name).Append(':').Append(Fingerprint(field.GetValue(value))).Append(';');
        }
        return text.Append('}').ToString();
    }

    private static RelationshipRun RoundTrip(RelationshipRun run, Action<bool, string> check, string name)
    {
        RelationshipSaveData data = run.ExportSave();
        string expected = Fingerprint(data);
        RelationshipRun restored;
        check(RelationshipRun.TryRestoreSave(data, out restored), name + " is accepted");
        check(restored != null && Fingerprint(restored.ExportSave()) == expected, name + " preserves every saved value");
        return restored;
    }

    private static RelationshipRun Ready()
    {
        var run = RelationshipRun.Legacy();
        for (int i = 0; i < 6; i++) { if (i % 2 == 0) run.Rest(); run.Respond(i % 2, 1); }
        return run;
    }

    private static void Reject(Action<RelationshipSaveData> change, Action<bool, string> check, string name)
    {
        var data = Ready().ExportSave();
        change(data);
        RelationshipRun result;
        check(!RelationshipRun.TryRestoreSave(data, out result) && result == null, name);
    }

    public static void Run(Action<bool, string> check)
    {
        RoundTrip(RelationshipRun.Legacy(), check, "untouched home checkpoint");
        var office = RoundTrip(RelationshipRun.Legacy(CaseId.Office), check, "office checkpoint");
        check(office.Case == CaseId.Office, "restoring an office case retains its rules and narrative identity");

        var consumed = RelationshipRun.Legacy();
        consumed.SelectCard(ConversationCard.Pause, 0); consumed.Respond(0, 1); consumed.Rest();
        var afterRest = RoundTrip(consumed, check, "rest and spent card checkpoint");
        check(afterRest.RestUsedThisRound && !afterRest.CanRest && afterRest.Stock(ConversationCard.Pause) == 0 && !afterRest.CanRewind,
            "restoring does not refund a rest, spent card, or closed revision opportunity");

        var undo = RelationshipRun.Legacy();
        undo.Respond(0, 2); undo.Respond(1, 2); undo.Respond(0, 2);
        check(undo.SelectCard(ConversationCard.Listen, 1), "undo fixture can spend the listening card");
        undo.Respond(1, 1);
        var resumedUndo = RoundTrip(undo, check, "phase boundary undo checkpoint");
        check(undo.CanRewind && resumedUndo.CanRewind && undo.Rewind() && resumedUndo.Rewind(), "saved revision opportunity remains usable");
        check(Fingerprint(undo.ExportSave()) == Fingerprint(resumedUndo.ExportSave()) && resumedUndo.Messages.Count == 0,
            "restored undo removes the exact response and its newly heard envelope");
        check(resumedUndo.Stock(ConversationCard.Listen) == 0 && resumedUndo.Stock(ConversationCard.Rewind) == 0,
            "undo after loading refunds neither the played card nor revision stock");
        var rewoundAgain = RoundTrip(resumedUndo, check, "checkpoint after undo");
        rewoundAgain.Respond(1, 1);
        check(rewoundAgain.Stock(ConversationCard.Rewind) == 0 && !rewoundAgain.CanRewind && rewoundAgain.Stock(ConversationCard.TakeOver) == 1,
            "crossing a saved phase boundary again cannot duplicate discovered cards");

        var privacy = Ready();
        check(privacy.ActOnMessage(0, MessageAction.Deliver, MessagePayload.Need, 1).Breach, "privacy fixture discloses without permission");
        var resumedPrivacy = RoundTrip(privacy, check, "late privacy breach checkpoint");
        check(resumedPrivacy.HasUnrepairedDisclosure(0) && !resumedPrivacy.RedConsents && resumedPrivacy.BlueConsents,
            "loading retains the owner-specific breach and revoked consent");
        var repair = resumedPrivacy.ActOnMessage(0, MessageAction.Repair, MessagePayload.Need, 0);
        check(repair != null && repair.ConsentRenewed && resumedPrivacy.RedConsents && !resumedPrivacy.HasUnrepairedDisclosure(0),
            "loading preserves the prior consent needed for an honest renewed yes");
        RoundTrip(resumedPrivacy, check, "repaired disclosure checkpoint");

        var letters = RelationshipRun.Legacy();
        letters.Respond(0, 1);
        letters.ActOnMessage(0, MessageAction.AskPermission, MessagePayload.Need, 0);
        letters.Respond(1, 1);
        var resumedLetters = RoundTrip(letters, check, "permission and subsequent choice checkpoint");
        check(resumedLetters.MessageFrom(0).Permission == MessagePermission.Granted && resumedLetters.Correspondence.Count == 1,
            "permissions and already heard exchanges survive loading");
        check(resumedLetters.PreviewMessage(0, MessageAction.Deliver, MessagePayload.Need, 1) != null,
            "a restored envelope can be previewed");
        RoundTrip(resumedLetters, check, "checkpoint after correspondence preview");
        resumedLetters.ActOnMessage(0, MessageAction.Deliver, MessagePayload.Need, 1);
        check(resumedLetters.ActOnMessage(0, MessageAction.Deliver, MessagePayload.Need, 1) == null,
            "loading never makes a delivered envelope reusable");
        RoundTrip(resumedLetters, check, "delivered envelope checkpoint");

        var privateNote = RelationshipRun.Legacy(); privateNote.Respond(1, 1);
        privateNote.ActOnMessage(0, MessageAction.AskPermission, MessagePayload.Need, 1);
        var declined = RoundTrip(privateNote, check, "own voice refusal checkpoint");
        check(declined.Message(0).Sharing == SharingPreference.OwnVoice && declined.Message(0).Permission == MessagePermission.Declined
            && !declined.CanActOnMessage(0, MessageAction.AskPermission, MessagePayload.Need, 1),
            "a saved refusal cannot be bypassed by reopening the game");

        var terminal = Ready(); terminal.Decide(1);
        var resumedTerminal = RoundTrip(terminal, check, "finished case checkpoint");
        check(resumedTerminal.Ended && !resumedTerminal.CanTalk(0) && !resumedTerminal.CanRewind, "terminal data cannot reopen resolved choices");

        var original = Ready(); var exported = original.ExportSave(); string baseline = Fingerprint(original.ExportSave());
        RelationshipRun detached;
        check(RelationshipRun.TryRestoreSave(exported, out detached), "aliasing fixture restores");
        exported.State.Capacity = 0; exported.CardStock[0] = 9; exported.CardsAcquired[0] = false; exported.Rested[0] = false;
        exported.History[0].Option = 0; exported.Messages[0].HasNeed = false; exported.Disclosures[0] = true;
        exported.ConsentBeforeDisclosure[0] = true; exported.Undo.State.Capacity = 0; exported.Undo.Messages[0].HasNeed = false;
        check(Fingerprint(original.ExportSave()) == baseline && Fingerprint(detached.ExportSave()) == baseline,
            "exported and restored arrays, records, envelopes, flags and undo state have no shared mutable references");
        detached.History[0].Option = 2; detached.Message(0).HasNeed = false;
        check(Fingerprint(original.ExportSave()) == baseline, "restored history and live envelopes are independent of the original run");
        var eventData = letters.ExportSave();
        RelationshipRun eventClone;
        check(RelationshipRun.TryRestoreSave(eventData, out eventClone), "correspondence aliasing fixture restores");
        string eventBaseline = Fingerprint(eventClone.ExportSave());
        eventData.Correspondence[0].PermissionGranted = false;
        eventData.Correspondence[0].CapacityDelta = 999;
        eventData.Undo.Disclosures[0] = true;
        eventData.Undo.ConsentBeforeDisclosure[0] = true;
        check(Fingerprint(eventClone.ExportSave()) == eventBaseline, "event data and nested undo privacy arrays are detached from restored state");

        RelationshipRun missing;
        check(!RelationshipRun.TryRestoreSave(null, out missing) && missing == null, "missing save data is rejected without throwing");
        Reject(d => d.Version++, check, "unknown save version is rejected");
        Reject(d => d.Case = 999, check, "unknown case is rejected");
        Reject(d => d.State = null, check, "missing state is rejected");
        Reject(d => d.State.Capacity = -1, check, "out of bounds resources are rejected");
        Reject(d => d.State.Phase = 7, check, "invalid phase is rejected");
        Reject(d => d.CardStock = new int[6], check, "truncated inventory is rejected");
        Reject(d => d.CardStock[0] = 2, check, "duplicated card stock is rejected");
        Reject(d => d.CardsAcquired[0] = false, check, "stock without card acquisition is rejected");
        Reject(d => d.Rested = null, check, "missing rest flags are rejected");
        Reject(d => d.RestStops = 0, check, "rest totals must match consumed opportunities");
        Reject(d => d.History[0] = null, check, "missing history entries are rejected");
        Reject(d => d.History[0].Phase = 1, check, "history order is validated");
        Reject(d => d.History[1].Voice = d.History[0].Voice, check, "duplicate voice choices in a phase are rejected");
        Reject(d => d.Messages[0].Source = 4, check, "invalid envelope owner is rejected");
        Reject(d => d.Messages[1].Id = 0, check, "duplicate envelope identity is rejected");
        Reject(d => d.Disclosures = new bool[1], check, "truncated privacy state is rejected");
        Reject(d => d.Undo = null, check, "declared undo without its snapshot is rejected");
        Reject(d => d.Undo.State.Capacity = 99, check, "damaged undo resources are rejected");
        Reject(d => d.Undo.EventCount = 40, check, "undo cannot invent correspondence history");
        Reject(d => d.Correspondence = new[] { new CorrespondenceEventSaveData { MessageId = 10 } }, check, "events must reference a real envelope");

        var random = new Random(41279);
        for (int journey = 0; journey < 250; journey++)
        {
            var run = RelationshipRun.Legacy((CaseId)(journey % 2));
            for (int step = 0; step < 9 && !run.AllVoicesHeard; step++)
            {
                if (random.Next(3) == 0) run.Rest();
                int voice = run.RedDone ? 1 : run.BlueDone ? 0 : random.Next(2);
                run.SelectCard((ConversationCard)random.Next(-1, 6), voice);
                run.Respond(voice, random.Next(3));
                run = RoundTrip(run, check, "random checkpoint");
                if (run.CanRewind && random.Next(3) == 0)
                {
                    run.Rewind(); run = RoundTrip(run, check, "random checkpoint following undo");
                }
                if (run.Messages.Count > 0)
                {
                    int id = random.Next(run.Messages.Count);
                    run.ActOnMessage(id, (MessageAction)random.Next(4), (MessagePayload)random.Next(2), random.Next(-1, 2));
                    run = RoundTrip(run, check, "random correspondence checkpoint");
                }
            }
            run.Decide(random.Next(1, 3));
            RoundTrip(run, check, "random ending checkpoint");
        }
    }
}
