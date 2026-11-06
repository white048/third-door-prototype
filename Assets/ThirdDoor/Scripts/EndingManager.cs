using UnityEngine;

public class EndingManager : MonoBehaviour
{
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private Light middleRoomLight;
    [SerializeField] private Light exitBackLight;
    [SerializeField] private Transform redPressureWall;
    [SerializeField] private Transform bluePressureWall;
    [SerializeField] private Renderer[] guidanceLineRenderers;
    [SerializeField] private Renderer[] sideDoorRenderers;
    [SerializeField] private Transform middleCeiling;

    public void Configure(DialogueUI ui,Light light){dialogueUI=ui;middleRoomLight=light;}
    public void ConfigureEnvironment(Light exitLight,Transform red,Transform blue,Renderer[] guides,Renderer[] doors,Transform ceiling)
    {
        exitBackLight=exitLight;redPressureWall=red;bluePressureWall=blue;guidanceLineRenderers=guides;sideDoorRenderers=doors;middleCeiling=ceiling;
        if(red!=null)red.gameObject.SetActive(false);if(blue!=null)blue.gameObject.SetActive(false);
        if(exitLight!=null)exitLight.enabled=false;
    }
    public void ShowEnding(RelationshipEnding ending,RelationshipRun run)
    {
        bool pressure=ending==RelationshipEnding.Loop||ending==RelationshipEnding.Alliance;
        if(redPressureWall!=null)redPressureWall.gameObject.SetActive(pressure);
        if(bluePressureWall!=null)bluePressureWall.gameObject.SetActive(pressure);
        if(middleRoomLight!=null)
        {
            middleRoomLight.intensity=pressure?.25f:ending==RelationshipEnding.Exhaustion?.4f:1.2f;
            middleRoomLight.color=pressure?ExhibitionDesign.Red:Color.white;
        }
        if(exitBackLight!=null)
        {
            exitBackLight.enabled=RelationshipRun.OpensExit(ending);
            exitBackLight.intensity=ending==RelationshipEnding.SharedTable?2.2f:.9f;
            exitBackLight.color=ending==RelationshipEnding.Distance?new Color(.8f,.9f,1):Color.white;
        }
        dialogueUI.ShowEndingText(NarrativeText.EndingText(run));
    }
    public void ShowEnding(bool good){if(GameStateManager.Instance!=null)ShowEnding(GameStateManager.Instance.Run.Ending,GameStateManager.Instance.Run);}
    public void UpdateProgressFeedback(int bridge)
    {
        if(middleRoomLight==null)return;
        middleRoomLight.intensity=.55f+bridge*.1f;
        middleRoomLight.color=Color.Lerp(new Color(1,.84f,.67f),Color.white,bridge/6f);
    }
}
