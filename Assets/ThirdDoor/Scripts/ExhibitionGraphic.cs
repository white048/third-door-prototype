using UnityEngine;
using UnityEngine.UI;

// Vector shapes stay sharp at any window size; the grain is deterministic and never animated.
public class ExhibitionGraphic : MaskableGraphic
{
    public enum Shape { Disc,Ring,Triangle,Grain }
    public Shape shape;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r=rectTransform.rect;
        if(shape==Shape.Grain)
        {
            var random=new System.Random(1923);
            for(int i=0;i<1800;i++)
            {
                float x=r.x+(float)random.NextDouble()*r.width;
                float y=r.y+(float)random.NextDouble()*r.height;
                float s=.4f+(float)random.NextDouble()*1.2f;
                Quad(vh,new Vector2(x,y),new Vector2(s,s));
            }
            return;
        }
        if(shape==Shape.Triangle)
        {
            Add(vh,new Vector2(r.xMin,r.yMin));Add(vh,new Vector2(r.xMax,r.yMin));Add(vh,new Vector2(r.center.x,r.yMax));
            vh.AddTriangle(0,1,2);return;
        }
        const int segments=160;
        Vector2 center=r.center;
        for(int i=0;i<segments;i++)
        {
            float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
            Vector2 p=new Vector2(Mathf.Cos(a)*r.width*.5f,Mathf.Sin(a)*r.height*.5f);
            Vector2 q=new Vector2(Mathf.Cos(b)*r.width*.5f,Mathf.Sin(b)*r.height*.5f);
            int index=vh.currentVertCount;
            if(shape==Shape.Ring)
            {
                Add(vh,center+p);Add(vh,center+q);Add(vh,center+q*.985f);Add(vh,center+p*.985f);
                vh.AddTriangle(index,index+1,index+2);vh.AddTriangle(index,index+2,index+3);
            }
            else
            {
                Add(vh,center);Add(vh,center+p);Add(vh,center+q);vh.AddTriangle(index,index+1,index+2);
            }
        }
    }
    private void Add(VertexHelper vh,Vector2 p){vh.AddVert(p,color,Vector2.zero);}
    private void Quad(VertexHelper vh,Vector2 at,Vector2 size)
    {
        int i=vh.currentVertCount;
        Add(vh,at);Add(vh,at+new Vector2(size.x,0));Add(vh,at+size);Add(vh,at+new Vector2(0,size.y));
        vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
    }
}
