using UnityEngine;

public static class EndingArchive
{
    private const string HomeKey = "ThirdDoor_Endings_v2", OfficeKey = "ThirdDoor_Endings_Office_v1";
    private static readonly int[] masks = { -1, -1 };
    private static CaseId ActiveCase
    {
        get
        {
            var state=GameStateManager.Instance;
            return IntroSequence.IsActive || state==null ? CaseSession.Current : state.Run.Case;
        }
    }
    private static string Key(CaseId selected) => selected==CaseId.Office?OfficeKey:HomeKey;
    private static int Mask(CaseId selected)
    {
        int index=(int)selected;
        if(masks[index]<0)masks[index]=PlayerPrefs.GetInt(Key(selected),0);
        return masks[index];
    }
    public static bool IsValidationRun
    {
        get
        {
            foreach (string argument in System.Environment.GetCommandLineArgs())
                if (argument.StartsWith("--validate-demo=")) return true;
            return false;
        }
    }
    public static bool Known(RelationshipEnding ending)
    { return Known(ActiveCase,ending); }
    public static bool Known(CaseId selected,RelationshipEnding ending)
    {
        if (ending == RelationshipEnding.None) return false;
        var state = GameStateManager.Instance;
        return (Mask(selected) & (1 << ((int)ending - 1))) != 0 || (state != null && state.Run.Case==selected && state.Run.Ending == ending);
    }
    public static int Count
    {
        get { int count = 0; for (int i = 1; i <= 7; i++) if (Known((RelationshipEnding)i)) count++; return count; }
    }
    public static void Record(RelationshipEnding ending)
    {
        if (IsValidationRun || ending == RelationshipEnding.None) return;
        CaseId selected=ActiveCase;
        int mask=Mask(selected) | (1 << ((int)ending - 1));
        masks[(int)selected]=mask;
        PlayerPrefs.SetInt(Key(selected), mask);
        PlayerPrefs.Save();
    }
}
