using System;
using System.Collections.Generic;

// This model has no Unity dependency. Outcomes can be explored and tested without an editor license.
public enum RelationshipEnding { None, SharedTable, Boundary, Distance, Loop, Alliance, Exhaustion, FalsePeace }
public enum ConversationCard { None = -1, Pause, Quote, Boundary, Listen, Reframe, TakeOver, Rewind }
public enum CaseId { Home = 0, Office = 1 }

public sealed class ConversationRecord
{
    public int Phase, Voice, Option;
    public ConversationCard Card;
    public int CapacityBefore, LoadBefore, BridgeBefore, CapacityAfter, LoadAfter, BridgeAfter;
    public bool TooExhaustedToListen, UnderPressure, VoiceConsentsAfter, RepairedDisclosure, PriorDisclosure;
    public int PriorManagedMessages, PriorAlliances;
    public int VoiceTrustBefore;
    public int CreatedMessageId = -1;
    public bool DispatchRules;
    public bool DeckRules;
    public int PriorReceipt;
}

public sealed partial class RelationshipRun
{
    public CaseId Case { get; private set; }
    public const int CapacityMax = 8, LoadMax = 8, BridgeMax = 6, PhaseCount = 3;
    public int Capacity { get; private set; } = 6;
    public int Load { get; private set; } = 1;
    public int Bridge { get; private set; }
    public int Phase { get; private set; }
    public int RedClarity { get; private set; }
    public int BlueClarity { get; private set; }
    public int RedTrust { get; private set; } = 1;
    public int BlueTrust { get; private set; } = 1;
    public int Boundaries { get; private set; }
    public int ManagedMessages { get; private set; }
    public int Alliances { get; private set; }
    public int RestStops { get; private set; }
    public int Revision { get; private set; }
    public int RevisionsUsed { get; private set; }
    public bool RedDone { get; private set; }
    public bool BlueDone { get; private set; }
    public bool RedConsents { get; private set; }
    public bool BlueConsents { get; private set; }
    public bool Ended => Ending != RelationshipEnding.None;
    public bool AllVoicesHeard => Phase == 2 && RedDone && BlueDone;
    public RelationshipEnding Ending { get; private set; }
    public ConversationCard SelectedCard { get; private set; } = ConversationCard.None;
    private readonly List<ConversationRecord> history = new List<ConversationRecord>();
    private readonly int[] stock = new int[7];
    private readonly bool[] acquired = new bool[7];
    private readonly bool[] rested = new bool[3];
    private Snapshot beforeLastChoice;
    public IReadOnlyList<ConversationRecord> History => history;
    public bool CanRest => !Ended && !rested[Phase] && (Capacity < CapacityMax || Load > 0);
    public bool RestUsedThisRound => rested[Phase];
    public bool CanRewind => !DeckRules && !Ended && beforeLastChoice != null && Stock(ConversationCard.Rewind) > 0;

    public RelationshipRun(CaseId caseId = CaseId.Home,bool dispatchRules=true,bool deckRules=false)
    {
        if (caseId != CaseId.Home && caseId != CaseId.Office) throw new ArgumentOutOfRangeException(nameof(caseId));
        Case = caseId;
        DispatchRules=dispatchRules;
        if(deckRules){if(caseId!=CaseId.Office||!dispatchRules)throw new ArgumentException("Deck mode requires the office dispatch case.");caseDeck=new CaseDeckData();}
        Draw(ConversationCard.Pause);
        Draw(ConversationCard.Quote);
        Draw(ConversationCard.Boundary);
    }

    private static int Clamp(int value, int max) => Math.Max(0, Math.Min(value, max));
    public int Clarity(int voice) => voice == 0 ? RedClarity : BlueClarity;
    public int Trust(int voice) => voice == 0 ? RedTrust : BlueTrust;
    public bool CanTalk(int voice) => (!DeckRules||DeckReady)&&!Ended && voice >= 0 && voice < 2 && !(voice == 0 ? RedDone : BlueDone);
    public int Stock(ConversationCard card) => card >= ConversationCard.Pause && card <= ConversationCard.Rewind ? stock[(int)card] : 0;
    public bool Acquired(ConversationCard card) => card >= ConversationCard.Pause && card <= ConversationCard.Rewind && acquired[(int)card];

    public bool CanEquip(ConversationCard card, int voice)
    {
        if (Ended || voice < 0 || voice > 1) return false;
        if (card == ConversationCard.None) return true;
        if (DeckRules) return false;
        if (card == ConversationCard.Rewind || Stock(card) == 0) return false;
        if (card == ConversationCard.Quote && Clarity(1 - voice) == 0) return false;
        if (card == ConversationCard.Reframe && Clarity(voice) == 0) return false;
        if ((card == ConversationCard.Listen || card == ConversationCard.Reframe || card == ConversationCard.TakeOver) && Capacity < 2) return false;
        return true;
    }

    public bool SelectCard(ConversationCard card, int voice)
    {
        if (!CanTalk(voice) || !CanEquip(card, voice)) return false;
        SelectedCard = card;
        Revision++;
        return true;
    }

    public ConversationRecord Respond(int voice, int option)
    {
        if (!CanTalk(voice) || option < 0 || option > 2) return null;
        beforeLastChoice = DeckRules?null:new Snapshot(this);
        var record = new ConversationRecord
        {
            Phase = Phase, Voice = voice, Option = option,
            DispatchRules=DispatchRules,DeckRules=DeckRules,PriorReceipt=ReceiptState(voice),
            CapacityBefore = Capacity, LoadBefore = Load, BridgeBefore = Bridge,
            PriorManagedMessages = ManagedMessages, PriorAlliances = Alliances,
            VoiceTrustBefore = Trust(voice),
            PriorDisclosure = HasUnrepairedDisclosure(voice),
            Card = CanEquip(SelectedCard, voice) ? SelectedCard : ConversationCard.None
        };
        if (record.Card == ConversationCard.Pause || record.Card == ConversationCard.Boundary) ApplyCard(record.Card, voice);
        Normalize();
        record.TooExhaustedToListen = NeedsBreath(voice, option) && Capacity <= 0;
        record.RepairedDisclosure = Phase == 2 && IsConsentResponse(voice, option) && !record.TooExhaustedToListen && HasUnrepairedDisclosure(voice);
        record.UnderPressure = Load >= 6;
        ApplyResponse(voice, option);
        if (record.UnderPressure && option != 2) Capacity--;
        if (record.Card != ConversationCard.None)
        {
            stock[(int)record.Card]--;
            if (record.Card != ConversationCard.Pause && record.Card != ConversationCard.Boundary) ApplyCard(record.Card, voice);
        }
        Normalize();
        record.CapacityAfter = Capacity;
        record.LoadAfter = Load;
        record.BridgeAfter = Bridge;
        record.VoiceConsentsAfter = voice == 0 ? RedConsents : BlueConsents;
        ObserveMessage(record);
        history.Add(record);
        SelectedCard = ConversationCard.None;
        if (voice == 0) RedDone = true; else BlueDone = true;
        if (!DeckRules&&RedDone && BlueDone && Phase < 2)
        {
            Phase++;
            RedDone = BlueDone = false;
            if (Phase == 1) { Draw(ConversationCard.Listen); Draw(ConversationCard.Reframe); }
            if (Phase == 2) { Draw(ConversationCard.TakeOver); Draw(ConversationCard.Rewind); }
        }
        Revision++;
        return record;
    }

    public int[] Preview(int voice, int option)
    {
        if (!CanTalk(voice) || option < 0 || option > 2) return new[] { 0, 0, 0 };
        var snapshot = new Snapshot(this);
        int capacity = Capacity, load = Load, bridge = Bridge;
        var card = CanEquip(SelectedCard, voice) ? SelectedCard : ConversationCard.None;
        if (card == ConversationCard.Pause || card == ConversationCard.Boundary) ApplyCard(card, voice);
        Normalize();
        bool pressure = Load >= 6;
        ApplyResponse(voice, option);
        if (pressure && option != 2) Capacity--;
        if (card != ConversationCard.Pause && card != ConversationCard.Boundary) ApplyCard(card, voice);
        Normalize();
        var delta = new[] { Capacity - capacity, Load - load, Bridge - bridge };
        snapshot.Restore(this);
        return delta;
    }

    private void ApplyResponse(int voice, int option)
    {
        if (NeedsBreath(voice, option) && Capacity <= 0) { Load++; return; }
        if (Case == CaseId.Office && voice == 1) { ApplyOfficeScope(option); return; }
        if (Phase == 0)
        {
            if (option == 0) { Capacity--; Load += 2; AddTrust(voice, 1); }
            else if (option == 1) { Capacity -= 2; Load--; AddClarity(voice, 1); AddTrust(voice, 1); Bridge++; }
            else { Capacity++; Load -= 2; Boundaries++; if (Clarity(voice) == 0) AddTrust(voice, -1); }
        }
        else if (Phase == 1)
        {
            if (option == 0) { Capacity--; Load++; ManagedMessages++; AddTrust(voice, 1); Bridge--; }
            else if (option == 1) { Capacity--; Load--; AddClarity(voice, 1); AddTrust(voice, 1); Bridge++; }
            else { Capacity--; Load += 2; Alliances++; AddTrust(voice, 2); AddTrust(1 - voice, -1); }
        }
        else
        {
            if (option == 0) { Capacity -= 2; Load += 2; ManagedMessages++; AddTrust(voice, 1); Bridge--; }
            else if (option == 1)
            {
                Capacity--; Load--; Bridge += Clarity(voice) > 0 ? 2 : 1;
                RepairDisclosure(voice);
                ManagedMessages=Math.Max(0,ManagedMessages-1);
                Alliances=Math.Max(0,Alliances-1);
                AddClarity(voice, 1);
                bool consent = Trust(voice) >= 1 && Clarity(voice) >= 2 && (Case != CaseId.Office || OfficePublicRequestDelivered)
                    &&(!DispatchRules||PublicPlanVerified(1-voice));
                if (voice == 0) RedConsents = consent; else BlueConsents = consent;
            }
            else { Capacity++; Load -= 2; Boundaries++; if (Clarity(voice) == 0) AddTrust(voice, -1); }
        }
    }

    private void ApplyCard(ConversationCard card, int voice)
    {
        switch (card)
        {
            case ConversationCard.Pause: Capacity += 2; Load--; break;
            case ConversationCard.Quote: Load++; Bridge++; break;
            case ConversationCard.Boundary: Capacity++; Load -= 2; Boundaries++; break;
            case ConversationCard.Listen: Capacity--; AddClarity(voice, 1); Bridge++; break;
            case ConversationCard.Reframe: Capacity--; Load--; Bridge++; AddTrust(voice, -1); break;
            case ConversationCard.TakeOver: Capacity -= 2; Load += 2; ManagedMessages++; AddTrust(0, 1); AddTrust(1, 1); break;
        }
    }

    public bool Rest()
    {
        if (!CanRest) return false;
        rested[Phase] = true;
        RestStops++;
        Capacity += 2;
        Load--;
        beforeLastChoice = null;
        Normalize();
        Revision++;
        return true;
    }

    public bool Rewind()
    {
        if (!CanRewind) return false;
        beforeLastChoice.Restore(this);
        // Spent cards and rest opportunities are not refunded. Draws cannot be duplicated.
        stock[(int)ConversationCard.Rewind]--;
        history.RemoveAt(history.Count - 1);
        beforeLastChoice = null;
        SelectedCard = ConversationCard.None;
        RevisionsUsed++;
        Revision++;
        return true;
    }

    public RelationshipEnding Decide(int option)
    {
        if (Ended || option < 0 || option > 2) return Ending;
        if(DeckRules&&option==1&&Phase<2){AdvanceEncounter();return RelationshipEnding.None;}
        if(DeckRules&&option==1&&(!AllVoicesHeard||!BothPlansVerified||!RedConsents||!BlueConsents||CurrentTasks))return RelationshipEnding.None;
        if (option == 0 && !AllVoicesHeard) return RelationshipEnding.None;
        if (option == 2)
        {
            if (Capacity == 0 && Load >= 6) Ending = RelationshipEnding.Exhaustion;
            else if (Boundaries >= 2 || (RedClarity > 0 && BlueClarity > 0 && Load <= 3)) Ending = RelationshipEnding.Boundary;
            else Ending = RelationshipEnding.Distance;
        }
        else if (option == 1 && AllVoicesHeard && RedConsents && BlueConsents && Bridge >= 4 && ManagedMessages < 2 && !HasUnrepairedDisclosure(0) && !HasUnrepairedDisclosure(1) && (Case != CaseId.Office || (OfficePublicRequestDelivered && Alliances == 0))&&(!DispatchRules||BothPlansVerified)&&(!DeckRules||!CurrentTasks))
            Ending = RelationshipEnding.SharedTable;
        else if (Capacity <= 1 || Load >= 8) Ending = RelationshipEnding.Exhaustion;
        else if (Alliances >= (Case==CaseId.Office?1:2)) Ending = RelationshipEnding.Alliance;
        else if (ManagedMessages >= 2||(DispatchRules&&HasAssumedPlan)) Ending = RelationshipEnding.FalsePeace;
        else if (option == 1 && Bridge < 4 && Load < 6) Ending = RelationshipEnding.Distance;
        else Ending = RelationshipEnding.Loop;
        SelectedCard = ConversationCard.None;
        beforeLastChoice = null;
        Revision++;
        return Ending;
    }

    public static bool OpensExit(RelationshipEnding ending) => ending == RelationshipEnding.SharedTable || ending == RelationshipEnding.Boundary || ending == RelationshipEnding.Distance || ending == RelationshipEnding.Exhaustion;

    private void Draw(ConversationCard card)
    {
        int i = (int)card;
        if (acquired[i]) return;
        acquired[i] = true;
        stock[i] = 1;
    }
    private void AddTrust(int voice, int amount) { if (voice == 0) RedTrust += amount; else BlueTrust += amount; }
    private void AddClarity(int voice, int amount) { if (voice == 0) RedClarity += amount; else BlueClarity += amount; }
    private void Normalize()
    {
        Capacity = Clamp(Capacity, CapacityMax); Load = Clamp(Load, LoadMax); Bridge = Clamp(Bridge, BridgeMax);
        RedClarity = Clamp(RedClarity, 3); BlueClarity = Clamp(BlueClarity, 3);
        RedTrust = Clamp(RedTrust, 4); BlueTrust = Clamp(BlueTrust, 4);
    }

    // Snapshots deliberately exclude consumables and discovered cards.
    private sealed partial class Snapshot
    {
        private readonly int capacity, load, bridge, phase, redClarity, blueClarity, redTrust, blueTrust, boundaries, managed, alliances;
        private readonly bool redDone, blueDone, redConsent, blueConsent;
        private readonly CorrespondenceSnapshot correspondence;
        public Snapshot(RelationshipRun run)
        {
            capacity = run.Capacity; load = run.Load; bridge = run.Bridge; phase = run.Phase;
            redClarity = run.RedClarity; blueClarity = run.BlueClarity; redTrust = run.RedTrust; blueTrust = run.BlueTrust;
            boundaries = run.Boundaries; managed = run.ManagedMessages; alliances = run.Alliances;
            redDone = run.RedDone; blueDone = run.BlueDone; redConsent = run.RedConsents; blueConsent = run.BlueConsents;
            correspondence = new CorrespondenceSnapshot(run);
        }
        public void Restore(RelationshipRun run)
        {
            run.Capacity = capacity; run.Load = load; run.Bridge = bridge; run.Phase = phase;
            run.RedClarity = redClarity; run.BlueClarity = blueClarity; run.RedTrust = redTrust; run.BlueTrust = blueTrust;
            run.Boundaries = boundaries; run.ManagedMessages = managed; run.Alliances = alliances;
            run.RedDone = redDone; run.BlueDone = blueDone; run.RedConsents = redConsent; run.BlueConsents = blueConsent;
            correspondence.Restore(run);
        }
    }
}
