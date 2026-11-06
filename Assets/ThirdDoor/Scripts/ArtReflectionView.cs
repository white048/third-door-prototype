using UnityEngine;
using UnityEngine.UI;

public sealed class ArtReflectionView : MonoBehaviour
{
    private DialogueUI dialogue;
    private GameObject root;
    private RawImage image;
    private ExhibitionText caption;
    public void Configure(DialogueUI ui,GameObject paper,RawImage target,ExhibitionText label)
    {dialogue=ui;root=paper;image=target;caption=label;root.SetActive(false);}
    private void LateUpdate()
    {
        bool visible=dialogue.IsDialogueOpen&&dialogue.IsReflection&&dialogue.ReflectionArtwork!=null;
        root.SetActive(visible);if(!visible)return;
        image.texture=dialogue.ReflectionArtwork;
        caption.text=NarrativeText.L("图像手记 / 拼贴中的细节","VISUAL NOTES / DETAILS IN THE COLLAGE");
    }
}
