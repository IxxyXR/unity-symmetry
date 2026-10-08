using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine.Rendering;


[ExecuteInEditMode]
public class PointGroupTest : MonoBehaviour
{
    [Header("Symmetry")]
    public PointSymmetry.Family family;
    [Min(1)] public int n = 3;
    public float radius = 1f;
    
    [Header("Transform Before")]
    public Vector3 Position = Vector3.zero;
    public Vector3 Rotation = Vector3.zero;
    public Vector3 Scale = Vector3.one;
    
    [Header("Transform Each")]
    public Vector3 PositionEach = Vector3.zero;
    public Vector3 RotationEach = Vector3.zero;
    public Vector3 ScaleEach = Vector3.one;
    public bool ApplyAfter = true;
        
    [BoxGroup("Gizmos"), InspectorName("Sample Shape Gizmos")] public bool symmetryGizmos;
    
    [BoxGroup("Gizmos"), UnityEngine.Serialization.FormerlySerializedAs("frameGizmos")] public bool domainGizmos;
    [BoxGroup("Gizmos"), Min(0.01f)] public float displayRadius = 1f;

    private PointSymmetry sym;
    private PointSymmetry.DomainOutline domainOutline;
    private List<Vector2> gizmoPath;

    private void OnEnable() { OnValidate(); }

    private void OnValidate()
    {
        n = Mathf.Max(1, n);
        displayRadius = Mathf.Max(0.01f, displayRadius);
        sym = new PointSymmetry(family, n, radius);
        domainOutline = sym.CreateDomainOutline(displayRadius);
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

        var transformBefore = Matrix4x4.TRS(Position, Quaternion.Euler(Rotation), Scale);
        DrawInstances(mesh, material, GetSymmetryMatrices().Select(m => m * transformBefore).ToList());
    }

    private List<Matrix4x4> GetSymmetryMatrices()
    {
        var result = new List<Matrix4x4>();
        var transformEach = Matrix4x4.TRS(PositionEach, Quaternion.Euler(RotationEach), ScaleEach);
        var cumulative = transformEach;
        foreach (var matrix in sym.matrices)
        {
            result.Add(ApplyAfter ? cumulative * matrix : matrix * cumulative);
            cumulative *= transformEach;
        }
        return result;
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
            
        var matrices = GetSymmetryMatrices();
        if (domainGizmos)
        {
            // Draw the source last so shared edges remain white.
            for (var copy = matrices.Count - 1; copy >= 0; copy--)
            {
                Gizmos.color = copy == 0 ? Color.white : Color.blue;
                foreach (var edge in domainOutline.edges)
                    Gizmos.DrawLine(matrices[copy].MultiplyPoint3x4(domainOutline.vertices[edge.x]),
                        matrices[copy].MultiplyPoint3x4(domainOutline.vertices[edge.y]));
            }
        }
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
            
            foreach (var m in matrices)
            {
                var path = gizmoPath.Select(v => (Vector2)m.MultiplyPoint3x4(v)).ToList();
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
