using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public partial class DemoValidation
{
    private IEnumerator ProbeRibbon(string name)
    {
        string before=JsonUtility.ToJson(state.Run.ExportSave());
        if(name=="00-empty-strip")
        {
            var station=FindObjectOfType<NarrativeRibbonStation>();
            yield return Approach(new Vector3(1.8f,.05f,-5.2f),station.transform.position+Vector3.up*2.1f);
            Physics.SyncTransforms();var camera=player.PlayerCamera;
            bool sight=Physics.SphereCast(camera.transform.position,.28f,camera.transform.forward,out RaycastHit hit,5.5f,~0,QueryTriggerInteraction.Collide);
            Check(sight&&hit.collider.GetComponentInParent<NarrativeRibbonStation>()==station,"physical paper roll is reached through the real interaction ray");
            yield return Shot("00-physical-strip");station.Interact(player);yield return null;
            Check(NarrativeNotebook.Open&&!player.ControlsEnabled,"E at the physical paper roll opens the live strip");
            Click("Close notebook");yield return null;
        }
        var book=FindObjectOfType<NarrativeNotebook>();book.Show(false);yield return null;
        CheckTextLayout("notes before the collage");
        Click("Collage ribbon tab");yield return null;yield return new WaitForEndOfFrame();
        var ribbon=FindObjectOfType<NarrativeRibbon>();
        Check(ribbon!=null&&NarrativeNotebook.Open&&!player.ControlsEnabled,"collage strip opens through the notebook and holds world input");
        Check(ribbon.ResponseCount==state.Run.History.Count&&ribbon.ReceiptCount==state.Run.Correspondence.Count,"strip contains every actual response and receipt, including a continued run");
        Check(ribbon.UsesCutouts!=IllustrationLibrary.ValidationFallback,"strip uses the generated transparent cutouts or its geometry fallback");
        Check(ribbon.VisibleSheets>0&&ribbon.VisibleSheets<8,"long strip only keeps nearby sheets active");
        ribbon.SetReadingPosition(0);yield return Shot(name+"-cover");CheckTextLayout("ribbon cover");
        ribbon.JumpToChapter(0);ribbon.SetReadingPosition(Mathf.Clamp01(ribbon.ReadingPosition+.05f));
        yield return Shot(name+"-response");CheckTextLayout("ribbon first response");
        float reading=ribbon.ReadingPosition;
        LocalizationManager.ToggleLanguage();yield return null;yield return new WaitForEndOfFrame();
        Check(Mathf.Abs(reading-ribbon.ReadingPosition)<.015f,"live translation keeps the reader's place in the strip");
        CheckTextLayout("translated collage strip");LocalizationManager.ToggleLanguage();yield return null;
        if(state.Run.Phase>0){Click("Ribbon chapter / 1");yield return null;Check(ribbon.ReadingPosition>0,"chapter button jumps within the actual long strip");}
        if(name.Contains("ending")){Click("Ribbon chapter / 3");yield return Shot(name+"-last-page");CheckTextLayout("strip ending");}
        var eventPointer=new PointerEventData(EventSystem.current){position=new Vector2(Screen.width*.65f,Screen.height*.45f),scrollDelta=new Vector2(0,-3)};
        var scroller=GameObject.Find("Ribbon viewport").GetComponent<UnityEngine.UI.ScrollRect>();float old=ribbon.ReadingPosition;
        ExecuteEvents.Execute(scroller.gameObject,eventPointer,ExecuteEvents.scrollHandler);yield return null;
        Check(ribbon.ReadingPosition>=old-.01f,"pointer scroll advances or clamps at the end of the strip");
        Check(JsonUtility.ToJson(state.Run.ExportSave())==before,"reading, translating and scrolling the collage cannot alter the model or undo snapshot");
        Click("Close notebook");yield return null;
        Check(player.ControlsEnabled&&!NarrativeNotebook.Open,"closing the strip restores exploration, also after an ending");
    }
    private IEnumerator ProbeProofCancellation()
    {
        int source=state.Run.Case==CaseId.Office?1:0;
        int recipient=1-source;var door=recipient==0?red:blue;
        yield return Approach(recipient==0?new Vector3(-3.3f,.05f,1.8f):new Vector3(3.3f,.05f,1.8f),door.transform.position+Vector3.up*1.8f);
        var book=FindObjectOfType<NarrativeNotebook>();book.ShowCorrespondence(recipient);yield return null;
        Click("Envelope source / "+source);yield return null;Click("Message payload / 1");yield return null;
        var folio=FindObjectOfType<CorrespondenceFolio>();string before=JsonUtility.ToJson(state.Run.ExportSave());
        Click("Message action / Deliver");yield return null;yield return new WaitForEndOfFrame();
        Check(folio.ProofOpen,"private payload opens a full delivery proof before it can be shared");
        Check(GameObject.Find("Proof actual words").GetComponent<ExhibitionText>().text.Contains(CorrespondenceText.Need(state.Run.MessageFrom(source))),"proof prints the selected private words verbatim");
        yield return Shot("16-private-delivery-proof");CheckTextLayout("private delivery proof");
        var behind=GameObject.Find("Envelope source / "+source).GetComponent<RectTransform>();
        var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,behind.TransformPoint(behind.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        var proofLayer=GameObject.Find("Dispatch proof").transform;
        Check(hits.Count>0&&(hits[0].gameObject.transform==proofLayer||hits[0].gameObject.transform.IsChildOf(proofLayer)),"delivery proof blocks envelope buttons behind the page");
        LocalizationManager.ToggleLanguage();yield return null;yield return new WaitForEndOfFrame();CheckTextLayout("translated private proof");
        Check(folio.ProofOpen&&folio.SelectedPayload==MessagePayload.Need,"language change preserves the exact words chosen for delivery");
        LocalizationManager.ToggleLanguage();yield return null;Click("Cancel dispatch proof");yield return null;
        Check(!folio.ProofOpen&&JsonUtility.ToJson(state.Run.ExportSave())==before,"canceling a private delivery spends nothing and changes no consent or trust");
        book.Close();yield return null;
    }
}
