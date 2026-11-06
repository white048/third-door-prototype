using System;
using System.Collections.Generic;
using System.Text;

// The second case must require a different judgement, not a memorised button sequence.
public static class OfficeTests
{
    public static string[] LastWitnesses { get; private set; } = new string[0];
    private static string Fingerprint(RelationshipRun run)
    {
        var text = new StringBuilder();
        text.Append(run.Case).Append('/').Append(run.Phase).Append('/').Append(run.RedDone).Append('/').Append(run.BlueDone)
            .Append('/').Append(run.Capacity).Append('/').Append(run.Load).Append('/').Append(run.Bridge)
            .Append('/').Append(run.RedClarity).Append('/').Append(run.BlueClarity).Append('/').Append(run.RedTrust).Append('/').Append(run.BlueTrust)
            .Append('/').Append(run.Boundaries).Append('/').Append(run.ManagedMessages).Append('/').Append(run.Alliances)
            .Append('/').Append(run.RestStops).Append('/').Append(run.RestUsedThisRound).Append('/').Append(run.CanRest)
            .Append('/').Append(run.RedConsents).Append('/').Append(run.BlueConsents).Append('/').Append(run.Ending)
            .Append('/').Append(run.SelectedCard).Append('/').Append(run.Revision).Append('/').Append(run.RevisionsUsed)
            .Append('/').Append(run.CanRewind).Append('/').Append(run.OfficePublicRequestDelivered);
        for (int i = 0; i < 7; i++) text.Append('/').Append(run.Stock((ConversationCard)i)).Append(',').Append(run.Acquired((ConversationCard)i));
        foreach (var record in run.History)
            text.Append('|').Append(record.Phase).Append(',').Append(record.Voice).Append(',').Append(record.Option).Append(',').Append(record.Card)
                .Append(',').Append(record.CapacityBefore).Append(',').Append(record.LoadBefore).Append(',').Append(record.BridgeBefore)
                .Append(',').Append(record.CapacityAfter).Append(',').Append(record.LoadAfter).Append(',').Append(record.BridgeAfter)
                .Append(',').Append(record.CreatedMessageId).Append(',').Append(record.VoiceConsentsAfter).Append(',').Append(record.PriorDisclosure);
        foreach (var item in run.Messages)
            text.Append('|').Append(item.Id).Append(',').Append(item.Source).Append(',').Append(item.Phase).Append(',').Append(item.SourceTrust)
                .Append(',').Append(item.PriorManaged).Append(',').Append(item.PriorAlliances).Append(',').Append(item.HasNeed)
                .Append(',').Append(item.Promised).Append(',').Append(item.PermissionPhase).Append(',').Append(item.Permission).Append(',').Append(item.Status).Append(',').Append(item.Sharing);
        foreach (var entry in run.Correspondence)
            text.Append('|').Append(entry.MessageId).Append(',').Append(entry.Source).Append(',').Append(entry.Phase).Append(',').Append(entry.Action)
                .Append(',').Append(entry.Payload).Append(',').Append(entry.Breach).Append(',').Append(entry.PermissionGranted).Append(',').Append(entry.RecipientReady)
                .Append(',').Append(entry.WasPromise).Append(',').Append(entry.ConsentRenewed).Append(',').Append(entry.PrefersOwnVoice)
                .Append(',').Append(entry.CapacityDelta).Append(',').Append(entry.LoadDelta).Append(',').Append(entry.BridgeDelta).Append(',').Append(entry.SourceTrustDelta).Append(',').Append(entry.RecipientTrustDelta);
        text.Append('/').Append(run.HasUnrepairedDisclosure(0)).Append('/').Append(run.HasUnrepairedDisclosure(1));
        return text.ToString();
    }

    private static RelationshipRun PlayScopeRoute(bool deliver, bool reverseOrder = false)
    {
        var run = RelationshipRun.Legacy(CaseId.Office);
        for (int round = 0; round < 3; round++)
        {
            run.Rest();
            int first = reverseOrder ? 1 : 0;
            run.Respond(first, first == 0 ? 1 : 2);
            if (deliver && round == 0 && first == 1)
                run.ActOnMessage(run.MessageFrom(1).Id, MessageAction.Deliver, MessagePayload.Request, 0);
            int second = 1 - first;
            run.Respond(second, second == 0 ? 1 : 2);
            if (deliver && round == 0 && second == 1)
                run.ActOnMessage(run.MessageFrom(1).Id, MessageAction.Deliver, MessagePayload.Request, 0);
        }
        return run;
    }

    public static void Run(Action<bool, string> check)
    {
        var scoped = PlayScopeRoute(true);
        check(scoped.Case == CaseId.Office && scoped.AllVoicesHeard, "Office is a complete second case with six authored responses");
        check(scoped.OfficePublicRequestDelivered && scoped.Correspondence.Count == 1, "Office agreement records the actual delivery of the colleague's public request");
        check(scoped.RedConsents && scoped.BlueConsents, "each person separately confirms the limited work agreement");
        check(scoped.Decide(1) == RelationshipEnding.SharedTable, "protecting scope and carrying the public request reaches the Office agreement");

        var reversed = PlayScopeRoute(true, true);
        check(reversed.RedConsents && reversed.BlueConsents && reversed.Decide(1) == RelationshipEnding.SharedTable, "the successful Office route permits either physical speaking order");
        var unsent = PlayScopeRoute(false);
        check(!unsent.OfficePublicRequestDelivered && !unsent.RedConsents, "the manager cannot consent to a request the player never conveyed");
        check(unsent.Decide(1) != RelationshipEnding.SharedTable, "dialogue scores alone cannot replace the physical delivery objective");

        var intrusive = RelationshipRun.Legacy(CaseId.Office);
        for (int i = 0; i < 6; i++) { if (i % 2 == 0) intrusive.Rest(); intrusive.Respond(i % 2, 1); }
        check(!intrusive.BlueConsents && intrusive.BlueClarity < 2, "repeating the Home listening option pressures the colleague instead of clarifying public scope");
        check(intrusive.Decide(1) != RelationshipEnding.SharedTable, "the Home 222222 sequence cannot solve the Office case");

        var privacy = RelationshipRun.Legacy(CaseId.Office);
        privacy.Rest(); privacy.Respond(1, 2);
        var colleague = privacy.MessageFrom(1);
        check(colleague != null && colleague.HasNeed && colleague.Sharing == SharingPreference.OwnVoice, "respecting a work boundary reveals a note without granting permission over private reasons");
        string before = Fingerprint(privacy);
        var no = privacy.PreviewMessage(colleague.Id, MessageAction.AskPermission, MessagePayload.Need, 1);
        check(no != null && !no.PermissionGranted && no.PrefersOwnVoice && Fingerprint(privacy) == before, "Office permission preview respects the colleague's preference without modifying state");
        privacy.ActOnMessage(colleague.Id, MessageAction.AskPermission, MessagePayload.Need, 1);
        check(privacy.BlueTrust >= 2 && colleague.Permission == MessagePermission.Declined, "a trusted colleague can refuse to share private reasons");
        check(!privacy.CanActOnMessage(colleague.Id, MessageAction.AskPermission, MessagePayload.Need, 1), "repeated asking cannot turn a clear refusal into permission");
        check(!privacy.CanActOnMessage(colleague.Id, MessageAction.Deliver, MessagePayload.Request, 1), "the public request must physically reach its actual recipient");
        check(privacy.ActOnMessage(colleague.Id, MessageAction.Deliver, MessagePayload.Request, 0) != null && privacy.OfficePublicRequestDelivered && !privacy.HasUnrepairedDisclosure(1), "declined private permission does not prevent an authorised public request being delivered");
        before = Fingerprint(privacy);
        check(privacy.ActOnMessage(colleague.Id, MessageAction.Deliver, MessagePayload.Request, 0) == null && Fingerprint(privacy) == before, "public deliveries cannot be repeated for bridge or trust rewards");

        var breach = RelationshipRun.Legacy(CaseId.Office);
        breach.Respond(0, 1); breach.Respond(1, 2);
        var privateDelivery = breach.ActOnMessage(breach.MessageFrom(1).Id, MessageAction.Deliver, MessagePayload.Need, 0);
        check(privateDelivery != null && privateDelivery.Breach && breach.HasUnrepairedDisclosure(1) && !breach.OfficePublicRequestDelivered, "revealing private reasons cannot substitute for the authorised work request");
        check(RelationshipRun.OpensExit(breach.Decide(2)), "a failed dispatch never removes the player's own exit");

        var fresh = RelationshipRun.Legacy(CaseId.Office);
        check(RelationshipRun.OpensExit(fresh.Decide(2)), "Office allows departure before resolving either person's problem");
        var suspended = RelationshipRun.Legacy(CaseId.Office);
        check(suspended.Decide(0) == RelationshipEnding.None && !suspended.Ended, "the unresolved Office case cannot be silently finalised before both voices have spoken");

        var spent = RelationshipRun.Legacy(CaseId.Office);
        spent.Respond(0, 0); spent.Respond(1, 2); spent.Respond(0, 0); spent.Respond(1, 0); spent.Respond(0, 0);
        int oldClarity = spent.BlueClarity;
        var exhaustedScope = spent.Respond(1, 2);
        check(exhaustedScope.CapacityBefore == 0 && exhaustedScope.TooExhaustedToListen && spent.BlueClarity == oldClarity && !spent.BlueConsents, "confirming a work boundary still requires breath and cannot grant free consent at zero capacity");

        var random = new Random(172309);
        for (int journey = 0; journey < 600; journey++)
        {
            var run = RelationshipRun.Legacy(CaseId.Office);
            for (int turn = 0; turn < 6; turn++)
            {
                if (random.Next(3) == 0) run.Rest();
                int voice = run.RedDone ? 1 : run.BlueDone ? 0 : random.Next(2);
                var card = (ConversationCard)random.Next(-1, 7);
                if (run.CanEquip(card, voice)) run.SelectCard(card, voice);
                int option = random.Next(3);
                before = Fingerprint(run);
                int capacity = run.Capacity, load = run.Load, bridge = run.Bridge;
                var delta = run.Preview(voice, option);
                check(Fingerprint(run) == before, "every Office response preview preserves the full run and existing ledger");
                var record = run.Respond(voice, option);
                check(record != null && run.Capacity - capacity == delta[0] && run.Load - load == delta[1] && run.Bridge - bridge == delta[2], "Office response costs match the displayed preview including cards and pressure");
                for (int actionIndex = 0; actionIndex < 2; actionIndex++)
                {
                    if (run.Messages.Count == 0) continue;
                    var item = run.Message(random.Next(run.Messages.Count));
                    var action = (MessageAction)random.Next(4); var payload = (MessagePayload)random.Next(2); int context = random.Next(-1, 2);
                    before = Fingerprint(run);
                    var itemReference = item; var status = item.Status;
                    var preview = run.PreviewMessage(item.Id, action, payload, context);
                    check(Fingerprint(run) == before && object.ReferenceEquals(itemReference, run.Message(item.Id)) && item.Status == status, "Office dispatch previews preserve derived delivery flags and live note references");
                    if (preview == null) continue;
                    capacity = run.Capacity; load = run.Load; bridge = run.Bridge;
                    var actual = run.ActOnMessage(item.Id, action, payload, context);
                    check(actual != null && actual.CapacityDelta == preview.CapacityDelta && actual.LoadDelta == preview.LoadDelta && actual.BridgeDelta == preview.BridgeDelta && actual.SourceTrustDelta == preview.SourceTrustDelta && actual.RecipientTrustDelta == preview.RecipientTrustDelta, "Office dispatch preview resolves to its actual receipt");
                    check(actual.CapacityDelta == run.Capacity - capacity && actual.LoadDelta == run.Load - load && actual.BridgeDelta == run.Bridge - bridge, "Office receipts record clamped actual resource changes");
                }
                check(run.Capacity >= 0 && run.Capacity <= RelationshipRun.CapacityMax && run.Load >= 0 && run.Load <= RelationshipRun.LoadMax && run.Bridge >= 0 && run.Bridge <= RelationshipRun.BridgeMax && run.RedTrust >= 0 && run.RedTrust <= 4 && run.BlueTrust >= 0 && run.BlueTrust <= 4 && run.Messages.Count <= 2 && run.History.Count <= 6, "Office random routes keep resources, messages and turns bounded");
            }
            bool couldAgree = run.RedConsents && run.BlueConsents && run.OfficePublicRequestDelivered && run.Bridge >= 4 && run.ManagedMessages < 2 && run.Alliances == 0 && !run.HasUnrepairedDisclosure(0) && !run.HasUnrepairedDisclosure(1);
            var ending = run.Decide(random.Next(3));
            check(ending != RelationshipEnding.SharedTable || couldAgree, "every randomly reached Office agreement satisfies both confirmations, public delivery and privacy");
            check(!run.CanTalk(0) && !run.CanTalk(1) && !run.CanRest && !run.CanRewind, "Office endings lock all gameplay mutations");
            if (ending == RelationshipEnding.SharedTable) check(RelationshipRun.OpensExit(ending), "an agreed work scope opens the personal exit");
        }

        CheckEndingReachability(check);
    }

    private static void CheckEndingReachability(Action<bool, string> check)
    {
        var witnesses = new Dictionary<RelationshipEnding, string>();
        // Include both voice orders and a real public dispatch. No cards are needed to reach any ending.
        for (int sequence = 0; sequence < 729; sequence++)
        for (int restMask = 0; restMask < 8; restMask++)
        for (int final = 0; final < 3; final++)
        for (int dispatch = 0; dispatch < 2; dispatch++)
        for (int order = 0; order < 2; order++)
        {
            var run = RelationshipRun.Legacy(CaseId.Office);
            int code = sequence;
            string route = "";
            bool sent = false;
            for (int step = 0; step < 6; step++)
            {
                if (step % 2 == 0 && (restMask & (1 << run.Phase)) != 0) run.Rest();
                int option = code % 3; code /= 3;
                int voice = (step + order) % 2;
                run.Respond(voice, option);
                route += (option + 1).ToString();
                if (dispatch != 0 && !sent && voice == 1)
                {
                    var item = run.MessageFrom(1);
                    if (item != null)
                        sent = run.ActOnMessage(item.Id, MessageAction.Deliver, MessagePayload.Request, 0) != null;
                }
            }
            bool validAgreement = run.RedConsents && run.BlueConsents && run.AllVoicesHeard && run.OfficePublicRequestDelivered && run.Bridge >= 4 && run.ManagedMessages < 2 && run.Alliances == 0 && !run.HasUnrepairedDisclosure(0) && !run.HasUnrepairedDisclosure(1);
            var ending = run.Decide(final);
            check(ending != RelationshipEnding.None, "every complete Office route resolves to an ending");
            if (final == 2) check(RelationshipRun.OpensExit(ending), "the personal Office exit stays available across exhaustive routes");
            if (ending == RelationshipEnding.SharedTable) check(validAgreement, "exhaustive Office agreement requires public delivery, both confirmations and no remaining alliance");
            if (!witnesses.ContainsKey(ending))
                witnesses[ending] = "responses=" + route + " / firstVoice=" + order + " / restMask=" + restMask + " / publicDispatch=" + sent + " / final=" + (final + 1);
        }
        var report = new List<string> { "OFFICE ENDINGS / exhaustive combinations: 69984" };
        for (int i = 1; i <= 7; i++)
        {
            var ending = (RelationshipEnding)i;
            check(witnesses.ContainsKey(ending), "Office ending reachable: " + ending);
            report.Add(ending + " / " + witnesses[ending]);
        }
        LastWitnesses = report.ToArray();
        foreach (string line in LastWitnesses) Console.WriteLine(line);
    }
}
