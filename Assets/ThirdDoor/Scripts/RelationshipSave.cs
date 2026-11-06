using System;

// Save data deliberately contains values only. It can be validated and restored without Unity.
[Serializable]
public sealed class RelationshipSaveData
{
    public int Version;
    public int Case;
    public RelationshipStateData State;
    public int Revision, RevisionsUsed, RestStops, Ending, SelectedCard;
    public int[] CardStock;
    public bool[] CardsAcquired, Rested;
    public ConversationRecordSaveData[] History;
    public MessageSaveData[] Messages;
    public CorrespondenceEventSaveData[] Correspondence;
    public bool[] Disclosures, ConsentBeforeDisclosure;
    public bool HasUndo;
    public bool DispatchRules;
    public RelationshipSnapshotSaveData Undo;
}

[Serializable]
public sealed class RelationshipStateData
{
    public int Capacity, Load, Bridge, Phase, RedClarity, BlueClarity, RedTrust, BlueTrust;
    public int Boundaries, ManagedMessages, Alliances;
    public bool RedDone, BlueDone, RedConsents, BlueConsents;
}

[Serializable]
public sealed class RelationshipSnapshotSaveData
{
    public RelationshipStateData State;
    public MessageSaveData[] Messages;
    public int EventCount;
    public bool[] Disclosures, ConsentBeforeDisclosure;
}

[Serializable]
public sealed class ConversationRecordSaveData
{
    public int Phase, Voice, Option, Card;
    public int CapacityBefore, LoadBefore, BridgeBefore, CapacityAfter, LoadAfter, BridgeAfter;
    public bool TooExhaustedToListen, UnderPressure, VoiceConsentsAfter, RepairedDisclosure, PriorDisclosure;
    public int PriorManagedMessages, PriorAlliances, VoiceTrustBefore, CreatedMessageId;
    public bool DispatchRules;
    public int PriorReceipt;
}

[Serializable]
public sealed class MessageSaveData
{
    public int Id, Source, Phase, SourceTrust, PriorManaged, PriorAlliances;
    public bool HasNeed, Promised;
    public int PermissionPhase, Permission, Status, Sharing;
    public int PublicProgress,DispatchAttempts;
    public bool Inspected,PrivateHeard,GuessHeard,Assumed;
}

[Serializable]
public sealed class CorrespondenceEventSaveData
{
    public int MessageId, Source, Phase, Action, Payload;
    public bool Breach, PermissionGranted, RecipientReady, WasPromise, ConsentRenewed, PrefersOwnVoice;
    public int CapacityDelta, LoadDelta, BridgeDelta, SourceTrustDelta, RecipientTrustDelta;
    public bool Packet,Completed,Provisional,Interrupted,PriorityMiss,Guarded,Unbelieved,QuestionedGuess,CorrectedAssumption;
    public int First,Second,Tool,PublicBefore,PublicAfter,HearingRoom,HeardCount;
}

public sealed partial class RelationshipRun
{
    public const int SaveVersion = 2;

    public RelationshipSaveData ExportSave()
    {
        var data = new RelationshipSaveData
        {
            Version = SaveVersion, Case = (int)Case, DispatchRules=DispatchRules,
            State = new RelationshipStateData
            {
                Capacity = Capacity, Load = Load, Bridge = Bridge, Phase = Phase,
                RedClarity = RedClarity, BlueClarity = BlueClarity, RedTrust = RedTrust, BlueTrust = BlueTrust,
                Boundaries = Boundaries, ManagedMessages = ManagedMessages, Alliances = Alliances,
                RedDone = RedDone, BlueDone = BlueDone, RedConsents = RedConsents, BlueConsents = BlueConsents
            },
            Revision = Revision, RevisionsUsed = RevisionsUsed, RestStops = RestStops,
            Ending = (int)Ending, SelectedCard = (int)SelectedCard,
            CardStock = (int[])stock.Clone(), CardsAcquired = (bool[])acquired.Clone(), Rested = (bool[])rested.Clone(),
            History = new ConversationRecordSaveData[history.Count], Messages = SaveMessages(messages.ToArray()),
            Correspondence = new CorrespondenceEventSaveData[correspondence.Count],
            Disclosures = (bool[])disclosures.Clone(), ConsentBeforeDisclosure = (bool[])consentBeforeDisclosure.Clone(),
            HasUndo = beforeLastChoice != null,
            Undo = beforeLastChoice == null ? null : beforeLastChoice.Export()
        };
        for (int i = 0; i < history.Count; i++) data.History[i] = SaveRecord(history[i]);
        for (int i = 0; i < correspondence.Count; i++) data.Correspondence[i] = SaveEvent(correspondence[i]);
        return data;
    }

    public static bool TryRestoreSave(RelationshipSaveData data, out RelationshipRun run)
    {
        run = null;
        if (!ValidSave(data)) return false;
        var restored = new RelationshipRun((CaseId)data.Case,data.Version>=2&&data.DispatchRules);
        RestoreState(restored, data.State);
        restored.Revision = data.Revision; restored.RevisionsUsed = data.RevisionsUsed; restored.RestStops = data.RestStops;
        restored.Ending = (RelationshipEnding)data.Ending; restored.SelectedCard = (ConversationCard)data.SelectedCard;
        Array.Copy(data.CardStock, restored.stock, restored.stock.Length);
        Array.Copy(data.CardsAcquired, restored.acquired, restored.acquired.Length);
        Array.Copy(data.Rested, restored.rested, restored.rested.Length);
        for (int i = 0; i < data.History.Length; i++) restored.history.Add(LoadRecord(data.History[i]));
        foreach (var item in data.Messages) restored.messages.Add(LoadMessage(item));
        foreach (var item in data.Correspondence) restored.correspondence.Add(LoadEvent(item));
        Array.Copy(data.Disclosures, restored.disclosures, 2);
        Array.Copy(data.ConsentBeforeDisclosure, restored.consentBeforeDisclosure, 2);
        restored.beforeLastChoice = data.HasUndo ? new Snapshot(data.Undo) : null;
        run = restored;
        return true;
    }

    private static bool InRange(int value, int maximum) => value >= 0 && value <= maximum;
    private static bool Flags(bool[] values, int length) => values != null && values.Length == length;
    private static bool ValidState(RelationshipStateData s)
    {
        return s != null && InRange(s.Capacity, CapacityMax) && InRange(s.Load, LoadMax) && InRange(s.Bridge, BridgeMax)
            && InRange(s.Phase, PhaseCount - 1) && InRange(s.RedClarity, 3) && InRange(s.BlueClarity, 3)
            && InRange(s.RedTrust, 4) && InRange(s.BlueTrust, 4) && InRange(s.Boundaries, 64)
            && InRange(s.ManagedMessages, 64) && InRange(s.Alliances, 64)
            && (s.Phase == PhaseCount - 1 || !s.RedDone || !s.BlueDone);
    }

    private static bool ValidProgress(RelationshipStateData state, ConversationRecordSaveData[] records, int count)
    {
        if (count != state.Phase * 2 + (state.RedDone ? 1 : 0) + (state.BlueDone ? 1 : 0)) return false;
        bool red = false, blue = false;
        for (int i = state.Phase * 2; i < count; i++)
        {
            if (records[i].Voice == 0) red = true; else blue = true;
        }
        return red == state.RedDone && blue == state.BlueDone;
    }

    private static bool ValidMessages(MessageSaveData[] items, int phase)
    {
        if (items == null || items.Length > 2) return false;
        bool[] sources = new bool[2];
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item == null || item.Id != i || !InRange(item.Source, 1) || sources[item.Source]
                || !InRange(item.Phase, phase) || !InRange(item.SourceTrust, 4)
                || !InRange(item.PriorManaged, 64) || !InRange(item.PriorAlliances, 64)
                || item.PermissionPhase < -1 || item.PermissionPhase > phase
                || !InRange(item.Permission, (int)MessagePermission.Declined)
                || !InRange(item.Status, (int)MessageStatus.KeptPrivate)
                ||!InRange(item.PublicProgress,3)||!InRange(item.DispatchAttempts,5)
                || !InRange(item.Sharing, (int)SharingPreference.OwnVoice)) return false;
            if ((item.Permission == (int)MessagePermission.Unasked) != (item.PermissionPhase == -1)) return false;
            if (item.Promised && item.Status != (int)MessageStatus.Held) return false;
            sources[item.Source] = true;
        }
        return true;
    }

    private static bool ValidSave(RelationshipSaveData d)
    {
        if (d == null || (d.Version!=1&&d.Version!=SaveVersion) || (d.Version==1&&d.DispatchRules) || !InRange(d.Case, (int)CaseId.Office) || !ValidState(d.State)
            || !InRange(d.Revision, int.MaxValue - 1) || !InRange(d.RevisionsUsed, 1) || !InRange(d.RestStops, PhaseCount)
            || !InRange(d.Ending, (int)RelationshipEnding.FalsePeace) || d.SelectedCard < -1 || d.SelectedCard >= (int)ConversationCard.Rewind
            || d.CardStock == null || d.CardStock.Length != 7 || !Flags(d.CardsAcquired, 7) || !Flags(d.Rested, PhaseCount)
            || d.History == null || d.History.Length > 6 || d.Correspondence == null || d.Correspondence.Length > 64
            || !Flags(d.Disclosures, 2) || !Flags(d.ConsentBeforeDisclosure, 2) || !ValidMessages(d.Messages, d.State.Phase)) return false;
        int restCount = 0;
        for (int i = 0; i < d.Rested.Length; i++)
        {
            if (d.Rested[i]) restCount++;
            if (i > d.State.Phase && d.Rested[i]) return false;
        }
        if (restCount != d.RestStops || d.Revision < d.History.Length + d.Correspondence.Length + d.RestStops + d.RevisionsUsed) return false;
        for (int i = 0; i < 7; i++)
        {
            if (!InRange(d.CardStock[i], 1) || (!d.CardsAcquired[i] && d.CardStock[i] != 0)) return false;
            if ((i < 3 || (i < 5 && d.State.Phase >= 1) || d.State.Phase == 2) && !d.CardsAcquired[i]) return false;
        }
        if (d.CardsAcquired[3] != d.CardsAcquired[4] || d.CardsAcquired[5] != d.CardsAcquired[6]) return false;
        if (d.State.Phase == 0 && (d.CardsAcquired[3] || d.CardsAcquired[5])) return false;
        if (d.RevisionsUsed != (d.CardsAcquired[6] ? 1 - d.CardStock[6] : 0)) return false;
        if (d.SelectedCard >= 0 && (d.CardStock[d.SelectedCard] == 0 || d.Ending != 0)) return false;
        for (int i = 0; i < d.History.Length; i++)
        {
            var h = d.History[i];
            if (h == null || h.Phase != i / 2 || !InRange(h.Voice, 1) || !InRange(h.Option, 2)
                || h.Card < -1 || h.Card >= (int)ConversationCard.Rewind
                || !InRange(h.CapacityBefore, CapacityMax) || !InRange(h.CapacityAfter, CapacityMax)
                || !InRange(h.LoadBefore, LoadMax) || !InRange(h.LoadAfter, LoadMax)
                || !InRange(h.BridgeBefore, BridgeMax) || !InRange(h.BridgeAfter, BridgeMax)
                || !InRange(h.PriorManagedMessages, 64) || !InRange(h.PriorAlliances, 64) || !InRange(h.VoiceTrustBefore, 4)
                ||!InRange(h.PriorReceipt,3)||h.DispatchRules!=d.DispatchRules
                || h.CreatedMessageId < -1 || h.CreatedMessageId >= d.Messages.Length) return false;
            if (i % 2 == 1 && h.Voice == d.History[i - 1].Voice) return false;
            if (h.Card >= 0 && d.CardStock[h.Card] != 0) return false;
            if (h.CreatedMessageId >= 0 && (d.Messages[h.CreatedMessageId].Source != h.Voice || d.Messages[h.CreatedMessageId].Phase != h.Phase)) return false;
        }
        if (!ValidProgress(d.State, d.History, d.History.Length)) return false;
        foreach (var e in d.Correspondence)
        {
            if (e == null || !InRange(e.MessageId, d.Messages.Length - 1) || e.Source != d.Messages[e.MessageId].Source
                || !InRange(e.Phase, d.State.Phase) || !InRange(e.Action, (int)MessageAction.Reconfirm) || (!d.DispatchRules&&e.Action==(int)MessageAction.Reconfirm)
                || !InRange(e.Payload, (int)MessagePayload.Need) || Math.Abs((long)e.CapacityDelta) > CapacityMax
                || Math.Abs((long)e.LoadDelta) > LoadMax || Math.Abs((long)e.BridgeDelta) > BridgeMax
                || Math.Abs((long)e.SourceTrustDelta) > 4 || Math.Abs((long)e.RecipientTrustDelta) > 4) return false;
            if(!ValidPacketEvent(e,d.DispatchRules))return false;
        }
        if (d.Ending != 0 && (d.HasUndo || d.SelectedCard != -1)) return false;
        if (d.HasUndo)
        {
            var u = d.Undo;
            if (u == null || !ValidState(u.State) || d.History.Length == 0 || !Flags(u.Disclosures, 2)
                || !Flags(u.ConsentBeforeDisclosure, 2) || u.EventCount != d.Correspondence.Length
                || !ValidMessages(u.Messages, u.State.Phase) || u.Messages.Length > d.Messages.Length
                || !ValidProgress(u.State, d.History, d.History.Length - 1)) return false;
            var last = d.History[d.History.Length - 1];
            if (u.State.Phase != last.Phase || u.State.Capacity != last.CapacityBefore
                || u.State.Load != last.LoadBefore || u.State.Bridge != last.BridgeBefore) return false;
            for (int i = 0; i < u.Messages.Length; i++) if (u.Messages[i].Source != d.Messages[i].Source) return false;
        }
        return true;
    }

    private static void RestoreState(RelationshipRun r, RelationshipStateData s)
    {
        r.Capacity = s.Capacity; r.Load = s.Load; r.Bridge = s.Bridge; r.Phase = s.Phase;
        r.RedClarity = s.RedClarity; r.BlueClarity = s.BlueClarity; r.RedTrust = s.RedTrust; r.BlueTrust = s.BlueTrust;
        r.Boundaries = s.Boundaries; r.ManagedMessages = s.ManagedMessages; r.Alliances = s.Alliances;
        r.RedDone = s.RedDone; r.BlueDone = s.BlueDone; r.RedConsents = s.RedConsents; r.BlueConsents = s.BlueConsents;
    }

    private static ConversationRecordSaveData SaveRecord(ConversationRecord h) => new ConversationRecordSaveData
    {
        Phase = h.Phase, Voice = h.Voice, Option = h.Option, Card = (int)h.Card,
        CapacityBefore = h.CapacityBefore, LoadBefore = h.LoadBefore, BridgeBefore = h.BridgeBefore,
        CapacityAfter = h.CapacityAfter, LoadAfter = h.LoadAfter, BridgeAfter = h.BridgeAfter,
        TooExhaustedToListen = h.TooExhaustedToListen, UnderPressure = h.UnderPressure,
        VoiceConsentsAfter = h.VoiceConsentsAfter, RepairedDisclosure = h.RepairedDisclosure, PriorDisclosure = h.PriorDisclosure,
        PriorManagedMessages = h.PriorManagedMessages, PriorAlliances = h.PriorAlliances,
        VoiceTrustBefore = h.VoiceTrustBefore, CreatedMessageId = h.CreatedMessageId,DispatchRules=h.DispatchRules,PriorReceipt=h.PriorReceipt
    };
    private static ConversationRecord LoadRecord(ConversationRecordSaveData h) => new ConversationRecord
    {
        Phase = h.Phase, Voice = h.Voice, Option = h.Option, Card = (ConversationCard)h.Card,
        CapacityBefore = h.CapacityBefore, LoadBefore = h.LoadBefore, BridgeBefore = h.BridgeBefore,
        CapacityAfter = h.CapacityAfter, LoadAfter = h.LoadAfter, BridgeAfter = h.BridgeAfter,
        TooExhaustedToListen = h.TooExhaustedToListen, UnderPressure = h.UnderPressure,
        VoiceConsentsAfter = h.VoiceConsentsAfter, RepairedDisclosure = h.RepairedDisclosure, PriorDisclosure = h.PriorDisclosure,
        PriorManagedMessages = h.PriorManagedMessages, PriorAlliances = h.PriorAlliances,
        VoiceTrustBefore = h.VoiceTrustBefore, CreatedMessageId = h.CreatedMessageId,DispatchRules=h.DispatchRules,PriorReceipt=h.PriorReceipt
    };
    private static MessageSaveData[] SaveMessages(MessageEnvelope[] items)
    {
        var saved = new MessageSaveData[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            var m = items[i];
            saved[i] = new MessageSaveData
            {
                Id = m.Id, Source = m.Source, Phase = m.Phase, SourceTrust = m.SourceTrust, PriorManaged = m.PriorManaged, PriorAlliances = m.PriorAlliances,
                HasNeed = m.HasNeed, Promised = m.Promised, PermissionPhase = m.PermissionPhase,
                Permission = (int)m.Permission, Status = (int)m.Status, Sharing = (int)m.Sharing,
                PublicProgress=m.PublicProgress,DispatchAttempts=m.DispatchAttempts,Inspected=m.Inspected,PrivateHeard=m.PrivateHeard,GuessHeard=m.GuessHeard,Assumed=m.Assumed
            };
        }
        return saved;
    }
    private static MessageEnvelope LoadMessage(MessageSaveData m) => new MessageEnvelope
    {
        Id = m.Id, Source = m.Source, Phase = m.Phase, SourceTrust = m.SourceTrust, PriorManaged = m.PriorManaged, PriorAlliances = m.PriorAlliances,
        HasNeed = m.HasNeed, Promised = m.Promised, PermissionPhase = m.PermissionPhase,
        Permission = (MessagePermission)m.Permission, Status = (MessageStatus)m.Status, Sharing = (SharingPreference)m.Sharing,
        PublicProgress=m.PublicProgress,DispatchAttempts=m.DispatchAttempts,Inspected=m.Inspected,PrivateHeard=m.PrivateHeard,GuessHeard=m.GuessHeard,Assumed=m.Assumed
    };
    private static CorrespondenceEventSaveData SaveEvent(CorrespondenceEvent e) => new CorrespondenceEventSaveData
    {
        MessageId = e.MessageId, Source = e.Source, Phase = e.Phase, Action = (int)e.Action, Payload = (int)e.Payload,
        Breach = e.Breach, PermissionGranted = e.PermissionGranted, RecipientReady = e.RecipientReady, WasPromise = e.WasPromise,
        ConsentRenewed = e.ConsentRenewed, PrefersOwnVoice = e.PrefersOwnVoice,
        CapacityDelta = e.CapacityDelta, LoadDelta = e.LoadDelta, BridgeDelta = e.BridgeDelta,
        SourceTrustDelta = e.SourceTrustDelta, RecipientTrustDelta = e.RecipientTrustDelta,
        Packet=e.Packet,Completed=e.Completed,Provisional=e.Provisional,Interrupted=e.Interrupted,PriorityMiss=e.PriorityMiss,Guarded=e.Guarded,Unbelieved=e.Unbelieved,QuestionedGuess=e.QuestionedGuess,CorrectedAssumption=e.CorrectedAssumption,
        First=(int)e.First,Second=(int)e.Second,Tool=(int)e.Tool,PublicBefore=e.PublicBefore,PublicAfter=e.PublicAfter,HearingRoom=e.HearingRoom,HeardCount=e.HeardCount
    };
    private static CorrespondenceEvent LoadEvent(CorrespondenceEventSaveData e) => new CorrespondenceEvent
    {
        MessageId = e.MessageId, Source = e.Source, Phase = e.Phase, Action = (MessageAction)e.Action, Payload = (MessagePayload)e.Payload,
        Breach = e.Breach, PermissionGranted = e.PermissionGranted, RecipientReady = e.RecipientReady, WasPromise = e.WasPromise,
        ConsentRenewed = e.ConsentRenewed, PrefersOwnVoice = e.PrefersOwnVoice,
        CapacityDelta = e.CapacityDelta, LoadDelta = e.LoadDelta, BridgeDelta = e.BridgeDelta,
        SourceTrustDelta = e.SourceTrustDelta, RecipientTrustDelta = e.RecipientTrustDelta,
        Packet=e.Packet,Completed=e.Completed,Provisional=e.Provisional,Interrupted=e.Interrupted,PriorityMiss=e.PriorityMiss,Guarded=e.Guarded,Unbelieved=e.Unbelieved,QuestionedGuess=e.QuestionedGuess,CorrectedAssumption=e.CorrectedAssumption,
        First=(DispatchFragment)e.First,Second=(DispatchFragment)e.Second,Tool=(DispatchTool)e.Tool,PublicBefore=e.PublicBefore,PublicAfter=e.PublicAfter,HearingRoom=e.HearingRoom,HeardCount=e.HeardCount
    };

    private sealed partial class Snapshot
    {
        public Snapshot(RelationshipSnapshotSaveData data)
        {
            var s = data.State;
            capacity = s.Capacity; load = s.Load; bridge = s.Bridge; phase = s.Phase;
            redClarity = s.RedClarity; blueClarity = s.BlueClarity; redTrust = s.RedTrust; blueTrust = s.BlueTrust;
            boundaries = s.Boundaries; managed = s.ManagedMessages; alliances = s.Alliances;
            redDone = s.RedDone; blueDone = s.BlueDone; redConsent = s.RedConsents; blueConsent = s.BlueConsents;
            correspondence = new CorrespondenceSnapshot(data);
        }
        public RelationshipSnapshotSaveData Export()
        {
            var data = correspondence.Export();
            data.State = new RelationshipStateData
            {
                Capacity = capacity, Load = load, Bridge = bridge, Phase = phase,
                RedClarity = redClarity, BlueClarity = blueClarity, RedTrust = redTrust, BlueTrust = blueTrust,
                Boundaries = boundaries, ManagedMessages = managed, Alliances = alliances,
                RedDone = redDone, BlueDone = blueDone, RedConsents = redConsent, BlueConsents = blueConsent
            };
            return data;
        }
    }

    private sealed partial class CorrespondenceSnapshot
    {
        public CorrespondenceSnapshot(RelationshipSnapshotSaveData data)
        {
            items = new MessageEnvelope[data.Messages.Length];
            for (int i = 0; i < items.Length; i++) items[i] = LoadMessage(data.Messages[i]);
            eventCount = data.EventCount;
            red = data.Disclosures[0]; blue = data.Disclosures[1];
            redConsent = data.ConsentBeforeDisclosure[0]; blueConsent = data.ConsentBeforeDisclosure[1];
        }
        public RelationshipSnapshotSaveData Export() => new RelationshipSnapshotSaveData
        {
            Messages = SaveMessages(items), EventCount = eventCount,
            Disclosures = new[] { red, blue }, ConsentBeforeDisclosure = new[] { redConsent, blueConsent }
        };
    }
}
