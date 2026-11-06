using UnityEngine;
using UnityEngine.UI;

public sealed class CaseArtworkView : MonoBehaviour
{
    private RawImage image;
    private GameObject plate,fallback;
    private ExhibitionText caption;
    private CaseId shown=(CaseId)(-1);
    private Language language;
    public Texture2D Displayed => image!=null?image.texture as Texture2D:null;
    public void Configure(RawImage target,GameObject art,GameObject geometry,ExhibitionText label=null)
    {image=target;plate=art;fallback=geometry;caption=label;Refresh();}
    private void OnEnable(){shown=(CaseId)(-1);}
    private void LateUpdate(){if(image!=null&&(shown!=CaseInfo.Active||language!=LocalizationManager.Current))Refresh();}
    private void Refresh()
    {
        shown=CaseInfo.Active;language=LocalizationManager.Current;
        var texture=IllustrationLibrary.ForCase(shown);image.texture=texture;
        plate.SetActive(texture!=null);if(fallback!=null)fallback.SetActive(texture==null);
        if(caption!=null){caption.gameObject.SetActive(texture!=null);caption.text=shown==CaseId.Office?"PLATE 02 / A REASON LEFT BLANK":"PLATE 01 / WORDS WE CARRY";}
    }
}
