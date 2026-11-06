using UnityEngine;

public class RelationshipInstallation : MonoBehaviour
{
    private PlayerController player;
    private Transform canopy,marker;
    private GameObject table;
    private Transform[] slips;
    private LineRenderer[] wires;
    private Light restLight;
    private TextMesh act,tableLabel;
    private int revision=-1;
    private CaseId shownCase=(CaseId)(-1);
    private RelationshipRun observed;
    private float markerAngle;
    private readonly Vector3[] wireStarts={new Vector3(-4.8f,3.9f,4.8f),new Vector3(4.8f,3.9f,4.8f),new Vector3(-4.8f,3.9f,4.8f)};
    private readonly Vector3[] linePoints=new Vector3[28];
    public void Configure(PlayerController p,Transform ceiling,GameObject sharedTable,Transform[] paper,LineRenderer[] lines,Transform index,Light breathing,TextMesh phase)
    {
        player=p;canopy=ceiling;table=sharedTable;slips=paper;wires=lines;marker=index;restLight=breathing;act=phase;
        tableLabel=table.GetComponentInChildren<TextMesh>(true);
    }
    private void LateUpdate()
    {
        var state=GameStateManager.Instance;if(state==null||player==null)return;var run=state.Run;
        CaseId activeCase=CaseInfo.Active;
        if(revision!=run.Revision||shownCase!=activeCase||observed!=run)
        {
            revision=run.Revision;shownCase=activeCase;observed=run;table.SetActive(run.Ending==RelationshipEnding.SharedTable);
            bool office=activeCase==CaseId.Office;
            act.text=run.Ended?"POSITION 0"+(int)run.Ending+" / 07":(office?new[]{"I / WHAT IS PUBLIC","II / TERMS OF THE TASK","III / A LIMITED AGREEMENT"}:new[]{"I / BORROWED VOICES","II / WORDS IN TRANSIT","III / WHO SPEAKS NEXT"})[run.Phase];
            if(tableLabel!=null)tableLabel.text=office?"REASON LEFT BLANK":"SPEAK / LISTEN";
            for(int i=0;i<slips.Length;i++)slips[i].gameObject.SetActive((!run.Ended||!RelationshipRun.OpensExit(run.Ending))&&i<run.Load);
        }
        float pressure=(run.Ending==RelationshipEnding.Loop||run.Ending==RelationshipEnding.Alliance)?1.7f:run.Load*.12f;
        canopy.position=Vector3.Lerp(canopy.position,new Vector3(0,7.7f-pressure,8),Time.deltaTime*.7f);
        player.PlayerCamera.fieldOfView=Mathf.Lerp(player.PlayerCamera.fieldOfView,64-run.Load*.28f+(RelationshipRun.OpensExit(run.Ending)?2:0),Time.deltaTime*.6f);
        restLight.intensity=Mathf.Lerp(restLight.intensity,run.CanRest?.65f:.12f,Time.deltaTime);
        for(int i=0;i<slips.Length;i++)
        {
            if(!slips[i].gameObject.activeSelf)continue;
            slips[i].position=new Vector3(Mathf.Sin(i*1.7f+Time.time*.25f)*.18f,1.1f+i*.38f,7.7f+i*.025f);
            slips[i].rotation=Quaternion.Euler(0,0,Mathf.Sin(i+Time.time*.3f)*(3+run.Load));
        }
        for(int w=0;w<3;w++)
        {
            bool liberated=RelationshipRun.OpensExit(run.Ending);
            wires[w].enabled=w==2?run.Bridge>0:!liberated;
            if(!wires[w].enabled)continue;
            Vector3 end=w==2?new Vector3(4.8f,3.9f,4.8f):new Vector3(0,2.1f,7.7f);
            wires[w].widthMultiplier=w==2?.008f+run.Bridge*.005f:.012f+run.Load*.004f;
            for(int i=0;i<28;i++)
            {
                float t=i/27f;Vector3 p=Vector3.Lerp(wireStarts[w],end,t);
                p.y-=Mathf.Sin(t*Mathf.PI)*(w==2?.2f:run.Load*.065f);
                linePoints[i]=p;
            }
            wires[w].SetPositions(linePoints);
        }
        float target=run.Ended?(int)run.Ending*42:run.Phase*100+run.Load*5;
        markerAngle=run.Ending==RelationshipEnding.Loop?markerAngle+Time.deltaTime*16:Mathf.LerpAngle(markerAngle,target,Time.deltaTime*.7f);
        float a=markerAngle*Mathf.Deg2Rad;
        marker.position=new Vector3(3.1f+Mathf.Sin(a)*4.4f,6.35f+Mathf.Cos(a)*4.4f,16.6f);
        marker.rotation=Quaternion.Euler(0,0,-markerAngle);
    }
}
