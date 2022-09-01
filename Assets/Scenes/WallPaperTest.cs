using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine.Rendering;


[ExecuteInEditMode]
public class WallPaperTest : MonoBehaviour
{
    [Header("Symmetry")]
    public SymmetryGroup.R group;
    public Vector2 TileSize = Vector2.one;
    public float UnitScale = 1f;
    public Vector2 UnitOffset = Vector2.zero;
    public Vector2 Spacing = Vector2.one;
    public Vector4 d;
    
    [Header("Iteration")]
    public int RepeatX = 1;
    public int RepeatY = 1;

    [Header("Transform Each")]
    public Vector3 PositionEach = Vector3.zero;
    public Vector3 RotationEach = Vector3.zero;
    public Vector3 ScaleEach = Vector3.one;
    public bool ApplyAfter = true;
        
    [BoxGroup("Gizmos")] public bool symmetryGizmos;
    [BoxGroup("Gizmos")] public bool domainGizmos;
    
    private WallpaperSymmetry sym;
    private List<Vector2> gizmoPath;

    private void OnValidate()
    {
        sym = new WallpaperSymmetry(group, RepeatX, RepeatY, TileSize, UnitScale, UnitOffset, Spacing, d);
    }

    void Update()
    {
        GetComponent<MeshRenderer>().enabled = false;
        Mesh mesh;
        Material material;
        if (Application.isPlaying)
        {
            mesh = GetComponent<MeshFilter>().mesh;
            material = GetComponent<MeshRenderer>().material;
        }
        else
        {
            mesh = GetComponent<MeshFilter>().sharedMesh;
            material = GetComponent<MeshRenderer>().sharedMaterial;
        }

        if (mesh == null) return;

        var matrices = new List<Matrix4x4>();
        var cumulativeTransform = Matrix4x4.TRS(PositionEach, Quaternion.Euler(RotationEach), ScaleEach);
        var currentCumulativeTransform = cumulativeTransform;

        foreach (var m in sym.matrices)
        {
            matrices.Add(
                ApplyAfter ? currentCumulativeTransform * m : m * currentCumulativeTransform
            );
            currentCumulativeTransform *= cumulativeTransform;
        }
        DrawInstances(mesh, material, matrices);
    }
    
    private List<List<T>> Split<T> (List<T> source, int size)
    {
        return source
            .Select ((x, i) => new { Index = i, Value = x })
            .GroupBy (x => x.Index / size)
            .Select (x => x.Select (v => v.Value).ToList ())
            .ToList ();
    }
    
    public void DrawInstances(Mesh mesh, Material material, List<Matrix4x4> matrices)
    {
        ShadowCastingMode castShadows = ShadowCastingMode.TwoSided;
        bool receiveShadows = true;

        List<List<Matrix4x4>> batches;
        batches = Split(matrices, 1023);

        for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
        {
            for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
            {
                Graphics.DrawMeshInstanced (mesh, subMeshIndex, material, batches[batchIndex], null, castShadows, receiveShadows);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (sym==null) return;
            
        if (symmetryGizmos)
        {
            if (gizmoPath == null || gizmoPath.Count == 0)
            {
                gizmoPath = new List<Vector2>
                {
                    new Vector2(-0.25f, -0.5f),
                    new Vector2(0.25f, -0.5f),
                    new Vector2(0.25f, -0.2f),
                    new Vector2(-0.05f, -0.2f),
                    new Vector2(-0.05f, 0.5f),
                    // new Vector2(-0.25f, 0.5f),
                };
            }
            
            foreach (var m in sym.matrices)
            {
                var path = gizmoPath.Select(v => (Vector2)m.MultiplyPoint3x4(v)).ToList();
                DrawPathGizmo(path);
            }
        }
        
        if (domainGizmos)
        {
            foreach (var m in sym.matrices)
            {
                var points = sym.groupProperties.fundamentalRegion.points;
                var path = points.Select(v => (Vector2)m.MultiplyPoint3x4(v)).ToList();
                DrawPathGizmo(path);
            }
        }
    }

    private void DrawPathGizmo(List<Vector2> path)
    {
        var initialPoint = new Vector3(path[0].x, path[0].y, 0);
        var prevPoint = initialPoint;
        for (int i = 1; i < path.Count; i++)
        {
            if (i==1) Gizmos.color = Color.red;
            else Gizmos.color = Color.yellow;
            
            var currentPoint = new Vector3(path[i].x, path[i].y, 0);
            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(prevPoint, initialPoint);
    }
}
