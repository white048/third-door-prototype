using UnityEngine;

// Bake the complete bilingual corpus once. Changing language reuses the same immutable atlas.
public static class NarrativeFontWarmup
{
    public static Font BodyFont;
    public static void Warm()
    {
        if (BodyFont != null) ExhibitionFontCache.Get(BodyFont);
    }
}
