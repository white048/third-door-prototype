using UnityEngine;

public class ExhibitionMotion : MonoBehaviour
{
    private PlayerController player;
    private Light sun;
    private float phase;
    private bool playedArrival;
    public void Configure(PlayerController p){player=p;sun=GameObject.Find("Raking afternoon light").GetComponent<Light>();}
    private void Update()
    {
        var state=GameStateManager.Instance;
        if(state==null||sun==null)return;
        var run=state.Run;
        Color target=new Color(1,.95f,.86f);
        if(run.Ending==RelationshipEnding.Loop||run.Ending==RelationshipEnding.Alliance)target=new Color(1,.49f,.34f);
        else if(run.Ending==RelationshipEnding.Distance)target=new Color(.78f,.88f,1);
        else if(run.Ending==RelationshipEnding.Exhaustion)target=new Color(.8f,.84f,.86f);
        else if(run.Ending==RelationshipEnding.FalsePeace)target=new Color(.8f,.94f,.87f);
        sun.color=Color.Lerp(sun.color,target,Time.deltaTime*.7f);
        float targetIntensity=run.Ending==RelationshipEnding.Loop||run.Ending==RelationshipEnding.Alliance?.65f:run.Ending==RelationshipEnding.Exhaustion?.8f:1.05f;
        sun.intensity=Mathf.Lerp(sun.intensity,targetIntensity,Time.deltaTime*.7f);
        AudioManager.Instance?.SetPressure(run.Load/8f,run.Capacity/8f);
        if(!IntroSequence.IsActive&&!playedArrival)
        {
            playedArrival=true;
            AudioManager.Instance.Play(GameSound.ChoiceClick,.22f);
        }
        if(player.ControlsEnabled&&!ExhibitionHUD.Paused)
        {
            var controller=player.GetComponent<CharacterController>();
            if(controller.isGrounded&&new Vector2(controller.velocity.x,controller.velocity.z).magnitude>.5f)
            {
                phase+=Time.deltaTime;
                if(phase>.52f){phase=0;AudioManager.Instance.PlayFootstep();}
            }
        }
    }
}
