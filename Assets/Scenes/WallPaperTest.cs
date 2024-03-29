using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine.Rendering;


[ExecuteInEditMode]
public class WallPaperTest : MonoBehaviour
{
    [Header("Simple Settings")]
    public bool SimpleSettingsOnly;
    public SymmetryGroup.R group;
    public int RepeatX = 1;
    public int RepeatY = 1;
    public float scale = .1f;
    
    [Header("Symmetry")]
    public Vector2 TileSize = Vector2.one;
    public float UnitScale = 1f;
    public Vector2 UnitOffset = Vector2.zero;
    public Vector2 Spacing = Vector2.one;
    public Vector4 d;

    [Header("Grid")]
    public float width = 1;
    public float height = 1;
    public float skewX = 0;
    public float skewY = 0;

    [Header("Transform Each")]
    public Vector3 PositionEach = Vector3.zero;
    public Vector3 RotationEach = Vector3.zero;
    public Vector3 ScaleEach = Vector3.one;
    public bool ApplyAfter = true;
        
    [BoxGroup("Gizmos")] public bool symmetryGizmos;
    [BoxGroup("Gizmos")] public bool domainGizmos;
    public float inset = .01f;

    private WallpaperSymmetry sym;
    private List<Vector2> gizmoPath;

    private void OnValidate()
    {
        if (SimpleSettingsOnly)
        {
            sym = new WallpaperSymmetry(group, RepeatX, RepeatY, scale, width, height, skewX, skewY);
            UnitOffset = sym.UnitOffset;
            d = sym.D;
        }
        else
        {
            sym = new WallpaperSymmetry(group, RepeatX, RepeatY, TileSize, UnitScale, UnitOffset, Spacing, d, scale);
        }
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
                Graphics.DrawMeshInstanced(mesh, subMeshIndex, material, batches[batchIndex], null, castShadows, receiveShadows);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (sym==null) return;
            
        if (symmetryGizmos)
        {
                gizmoPath = new List<Vector2>
                {
                    new Vector2(0, 0),
                    new Vector2(0, 0.75f),
                    new Vector2(0.1f, 0.75f),
                    new Vector2(0.1f, 0.3f),
                    new Vector2(0.3f, 0.3f),
                    new Vector2(0.3f, 0),
                };

            bool initial = true;
            foreach (var m in sym.matrices)
            {
                var path = gizmoPath.Select(v => (Vector2)m.MultiplyPoint3x4(v)).ToList();
                DrawPathGizmo(path, initial);
                initial = false;
            }
        }
        
        if (domainGizmos)
        {
            bool initial = true;
            foreach (var m in sym.matrices)
            {
                var points = sym.groupProperties.fundamentalRegion.points;
                var insetPath = InsetPolygon(points.ToList(), inset);
                var path = insetPath.Select(v => (Vector2)m.MultiplyPoint3x4(v)).ToList();
                DrawPathGizmo(path, initial);
                initial = false;
            }
        }
    }

    private void DrawPathGizmo(List<Vector2> path, bool initial)
    {
        var initialPoint = new Vector3(path[0].x, path[0].y, 0);
        var prevPoint = initialPoint;
        for (int i = 1; i < path.Count; i++)
        {
            if (initial)
            {
                Gizmos.color = Color.white;
            }
            else if (i==1) Gizmos.color = Color.red;
            else
            {
                Gizmos.color = Color.yellow;
            }
            
            var currentPoint = new Vector3(path[i].x, path[i].y, 0);
            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
        Gizmos.DrawLine(prevPoint, initialPoint);
    }
    
    public static List<Vector2> InsetPolygon(List<Vector2> originalPoly, float insetAmount)
    {
        int Mod(int x, int m) {return (x % m + m) % m;}
        
        insetAmount = -insetAmount;
        Vector2 offsetDir = Vector2.zero;
    
        // Create the Vector3 vertices
        List<Vector2> offsetPoly = new List<Vector2>();
        for (int i = 0; i < originalPoly.Count; i++)
        {
            if (insetAmount != 0)
            {
                Vector2 tangent1 = (originalPoly[(i + 1) % originalPoly.Count] - originalPoly[i]).normalized;
                Vector2 tangent2 = (originalPoly[i] - originalPoly[Mod(i - 1, originalPoly.Count)]).normalized;
    
                Vector2 normal1 = new Vector2(-tangent1.y, tangent1.x).normalized;
                Vector2 normal2 = new Vector2(-tangent2.y, tangent2.x).normalized;
    
                offsetDir = (normal1 + normal2) / 2;
                offsetDir *= insetAmount / offsetDir.magnitude;
            }
            offsetPoly.Add(new Vector2(originalPoly[i].x - offsetDir.x, originalPoly[i].y - offsetDir.y));
        }
    
        return offsetPoly;
    }
}
