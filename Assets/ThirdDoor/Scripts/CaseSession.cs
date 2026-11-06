using UnityEngine;
using UnityEngine.SceneManagement;

// Only navigation survives a scene reload. The checkpoint owns the durable run state.
public static class CaseSession
{
    public static CaseId Current { get; private set; } = CaseId.Home;
    public static bool AutoBegin { get; private set; }
    private static RelationshipRun pendingRestore;

    public static void Select(CaseId selected)
    {
        if (selected == CaseId.Home || selected == CaseId.Office) Current = selected;
    }

    public static bool ContinueSaved()
    {
        RelationshipRun saved;
        if (!CheckpointStore.TryLoad(out saved)) return false;
        pendingRestore = saved;
        Current = saved.Case;
        AutoBegin = false;
        IntroSequence.PrepareReload(true);
        Reload();
        return true;
    }

    public static RelationshipRun TakePendingRestore()
    {
        RelationshipRun saved = pendingRestore;
        pendingRestore = null;
        return saved;
    }

    public static bool ConsumeAutoBegin()
    {
        bool value = AutoBegin;
        AutoBegin = false;
        return value;
    }

    public static void StartFresh(CaseId selected, bool showBriefing = false)
    {
        Select(selected);
        pendingRestore = null;
        CheckpointStore.Clear();
        AutoBegin = showBriefing;
        IntroSequence.PrepareReload(!showBriefing);
        Reload();
    }

    public static void ReturnToTitle()
    {
        var state = GameStateManager.Instance;
        if(state!=null&&!IntroSequence.IsActive&&!state.Run.Ended)state.SaveCheckpoint();
        if (state != null) Current = state.Run.Case;
        pendingRestore = null;
        AutoBegin = false;
        IntroSequence.PrepareReload(false);
        Reload();
    }

    private static void Reload()
    {
        Time.timeScale = 1;
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex >= 0 ? scene.buildIndex : 0);
    }
}
