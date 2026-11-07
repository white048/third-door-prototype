using UnityEngine;
using UnityEngine.UI;

// Small print marks stay crisp at every UI scale and do not require a font or raster atlas.
public sealed class ToolMark : MaskableGraphic
{
    public DispatchTool Tool;
    public static ToolMark Create(Transform parent,DispatchTool tool,Vector2 at,Vector2 size,Color ink)
    {
        var rect=ExhibitionDesign.Rect(parent,"Tool mark / "+tool,at,size);
        var mark=rect.gameObject.AddComponent<ToolMark>();mark.Tool=tool;mark.color=ink;mark.raycastTarget=false;return mark;
    }
    private void Quad(VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Vector2 d)
    {
        var rect=rectTransform.rect;int first=v.currentVertCount;
        foreach(var p in new[]{a,b,c,d})v.AddVert(new Vector3(rect.x+p.x*rect.width,rect.y+p.y*rect.height),color,Vector2.zero);
        v.AddTriangle(first,first+1,first+2);v.AddTriangle(first,first+2,first+3);
    }
    private void Line(VertexHelper v,float x,float y,float ex,float ey,float thickness=.065f)
    {var a=new Vector2(x,y);var b=new Vector2(ex,ey);var side=(b-a).normalized;side=new Vector2(-side.y,side.x)*thickness*.5f;Quad(v,a-side,b-side,b+side,a+side);}
    private void Box(VertexHelper v,float x,float y,float w,float h)
    {Quad(v,new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h));}
    private void Frame(VertexHelper v,float x,float y,float w,float h)
    {Line(v,x,y,x+w,y);Line(v,x+w,y,x+w,y+h);Line(v,x+w,y+h,x,y+h);Line(v,x,y+h,x,y);}
    private void Arrow(VertexHelper v,float x,float y,float ex,float ey)
    {Line(v,x,y,ex,ey);var end=new Vector2(ex,ey);var back=(new Vector2(x,y)-end).normalized*.22f;var side=new Vector2(-back.y,back.x)*.7f;Line(v,ex,ey,ex+back.x+side.x,ey+back.y+side.y);Line(v,ex,ey,ex+back.x-side.x,ey+back.y-side.y);}
    protected override void OnPopulateMesh(VertexHelper v)
    {
        v.Clear();
        switch(Tool)
        {
            case DispatchTool.Quote:Box(v,.12f,.5f,.24f,.3f);Box(v,.58f,.5f,.24f,.3f);Line(v,.36f,.5f,.2f,.2f);Line(v,.82f,.5f,.66f,.2f);break;
            case DispatchTool.Pause:Box(v,.2f,.15f,.19f,.7f);Box(v,.61f,.15f,.19f,.7f);break;
            case DispatchTool.Listen:Frame(v,.12f,.35f,.75f,.5f);Line(v,.26f,.35f,.26f,.12f);Line(v,.26f,.12f,.48f,.35f);Line(v,.28f,.61f,.72f,.61f);break;
            case DispatchTool.Reframe:Frame(v,.13f,.15f,.45f,.45f);Frame(v,.42f,.42f,.45f,.45f);Arrow(v,.17f,.83f,.78f,.22f);break;
            case DispatchTool.Boundary:Line(v,.18f,.12f,.18f,.88f,.11f);Line(v,.8f,.12f,.8f,.88f,.11f);Line(v,.18f,.5f,.8f,.5f);break;
            case DispatchTool.TakeOver:for(int i=0;i<3;i++){float y=.24f+i*.25f;Frame(v,.12f,y-.06f,.12f,.12f);Line(v,.36f,y,.87f,y);}break;
            case DispatchTool.Check:Frame(v,.12f,.12f,.76f,.76f);Line(v,.24f,.48f,.43f,.27f,.09f);Line(v,.43f,.27f,.78f,.76f,.09f);break;
            case DispatchTool.Receipt:Frame(v,.23f,.1f,.55f,.8f);Line(v,.35f,.68f,.66f,.68f);Line(v,.35f,.47f,.66f,.47f);Line(v,.35f,.26f,.53f,.26f);break;
            case DispatchTool.Handoff:Arrow(v,.1f,.72f,.88f,.72f);Arrow(v,.88f,.28f,.1f,.28f);break;
        }
    }
}
