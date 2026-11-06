using System;

public sealed partial class RelationshipRun
{
    // This is an observed action, not a score which can be manufactured with cards.
    public bool OfficePublicRequestDelivered
    {
        get
        {
            if(DispatchRules)return PublicPlanVerified(1);
            foreach(var item in correspondence)
                if(item.Source==1&&item.Action==MessageAction.Deliver&&item.Payload==MessagePayload.Request&&!item.Breach)return true;
            return false;
        }
    }
    private bool NeedsBreath(int voice,int option)=>option==1||(Case==CaseId.Office&&voice==1&&option==2);
    private bool IsConsentResponse(int voice,int option)=>Case==CaseId.Office&&voice==1?option==2:option==1;
    private void ApplyOfficeScope(int option)
    {
        if(option==1)
        {
            // A better explanation is not owed in exchange for being heard.
            Capacity--;Load++;AddTrust(1,-1);return;
        }
        if(option==2)
        {
            Capacity--;Load--;AddClarity(1,1);AddTrust(1,1);Bridge+=Phase==2?2:1;
            if(Phase==2)
            {
                RepairDisclosure(1);
                ManagedMessages=Math.Max(0,ManagedMessages-1);Alliances=Math.Max(0,Alliances-1);
                BlueConsents=BlueTrust>=1&&BlueClarity>=2&&(!DispatchRules||PublicPlanVerified(0));
            }
            return;
        }
        Capacity-=Phase==2?2:1;Load+=Phase==0?2:Phase==1?1:2;AddTrust(1,1);
        if(Phase>0){ManagedMessages++;Bridge--;}
    }
}
