using UnityEngine;

public class ExhibitionMeshLifetime : MonoBehaviour
{
    public Mesh mesh;
    private void OnDestroy(){if(mesh!=null){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}}
}

