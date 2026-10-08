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
    public int n = 3;
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
    
    [BoxGroup("Gizmos")] public bool frameGizmos;
    [BoxGroup("Gizmos"), Min(0.01f)] public float displayRadius = 1f;

    private PointSymmetry sym;
    private List<Vector2> gizmoPath;

    private void OnValidate()
    {
        sym = new PointSymmetry(family, n, radius);
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
        var transformBefore = Matrix4x4.TRS(Position, Quaternion.Euler(Rotation), Scale);
        var cumulativeTransform = Matrix4x4.TRS(PositionEach, Quaternion.Euler(RotationEach), ScaleEach);
        var currentCumulativeTransform = cumulativeTransform;

        foreach (var m in sym.matrices)
        {
            matrices.Add(
                (ApplyAfter ? currentCumulativeTransform * m : m * currentCumulativeTransform) * transformBefore
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
            
        if (frameGizmos)
        {
            Gizmos.color = Color.white;
            List<List<Vector3>> faces = null;
            switch (family)
            {
                case PointSymmetry.Family.T:
                case PointSymmetry.Family.Th:
                case PointSymmetry.Family.Td: faces = sym.Tetrahedron(); break;
                case PointSymmetry.Family.O:
                case PointSymmetry.Family.Oh: faces = sym.Octahedron(); break;
                case PointSymmetry.Family.I:
                case PointSymmetry.Family.Ih: faces = sym.Icosahedron(); break;
            }
            if (faces != null)
            {
                foreach (var face in faces)
                for (var i = 0; i < face.Count; i++)
                    Gizmos.DrawLine(sym.referenceFrame.MultiplyPoint3x4(face[i] * displayRadius),
                        sym.referenceFrame.MultiplyPoint3x4(face[(i + 1) % face.Count] * displayRadius));
            }
            else
            {
                // Axial symmetry frame: two rings and angular sectors around local Y.
                const int segments = 48;
                for (var segment = 0; segment < segments; segment++)
                {
                    var angle = 2f * Mathf.PI * segment / segments;
                    var a = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * displayRadius;
                    angle = 2f * Mathf.PI * (segment + 1) / segments;
                    var b = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * displayRadius;
                    foreach (var height in new[] { -displayRadius, displayRadius })
                        Gizmos.DrawLine(sym.referenceFrame.MultiplyPoint3x4(a + Vector3.up * height),
                            sym.referenceFrame.MultiplyPoint3x4(b + Vector3.up * height));
                }
                var sectors = family == PointSymmetry.Family.Sn ? 2 * n : n;
                for (var sector = 0; sector < sectors; sector++)
                {
                    var angle = 2f * Mathf.PI * sector / sectors;
                    var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * displayRadius;
                    var lower = radial - Vector3.up * displayRadius;
                    var upper = radial + Vector3.up * displayRadius;
                    Gizmos.DrawLine(sym.referenceFrame.MultiplyPoint3x4(lower), sym.referenceFrame.MultiplyPoint3x4(upper));
                    Gizmos.DrawLine(sym.referenceFrame.MultiplyPoint3x4(-Vector3.up * displayRadius), sym.referenceFrame.MultiplyPoint3x4(lower));
                    Gizmos.DrawLine(sym.referenceFrame.MultiplyPoint3x4(Vector3.up * displayRadius), sym.referenceFrame.MultiplyPoint3x4(upper));
                }
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
            
            foreach (var m in sym.matrices)
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
