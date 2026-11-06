using UnityEngine;

// Runtime-built assets have explicit owners, including when rebuilding in the editor.
public class ExhibitionResourceLifetime : MonoBehaviour
{
    private Object[] owned;
    public void Own(params Object[] assets) { owned=assets; }
    private void OnDestroy()
    {
        if(owned==null)return;
        foreach(var asset in owned)
        {
            if(asset==null)continue;
            if(asset is Font font)ExhibitionFontCache.Release(font);
            if(Application.isPlaying)Destroy(asset);else DestroyImmediate(asset);
        }
    }
}
