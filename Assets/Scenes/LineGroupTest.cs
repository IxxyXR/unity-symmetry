using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine.Rendering;


[ExecuteInEditMode]
public class LineGroupTest : MonoBehaviour
{
    [Header("Symmetry")]
    public LineGroupSymmetry.Family family = LineGroupSymmetry.Family.ScrewHalfTurns;
    [Min(1)] public int n = 5;
    public float angleDegrees = 27f;
    [Min(1)] public int repeats = 8;
    [Min(0f)] public float advance = 1.1f;
    [ReadOnly] public string groupName;
    [ReadOnly] public int copyCount;

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
    [BoxGroup("Gizmos")] public bool domainGizmos;
    [BoxGroup("Gizmos"), Min(0.01f)] public float domainRadius = 1.5f;

    private LineGroupSymmetry sym;
    private LineGroupSymmetry.DomainOutline domainOutline;
    private List<Vector3> gizmoPath;

    private void OnEnable() { OnValidate(); }

    private void OnValidate()
    {
        n = Mathf.Max(n, 1);
        repeats = Mathf.Max(repeats, 1);
        sym = new LineGroupSymmetry(family, n, repeats, advance, angleDegrees);
        domainRadius = Mathf.Max(0.01f, domainRadius);
        domainOutline = sym.CreateDomainOutline(domainRadius);
        groupName = sym.name;
        copyCount = sym.matrices.Count;
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

        DrawInstances(mesh, material, GetDrawMatrices());
    }

    public List<Matrix4x4> GetDrawMatrices()
    {
        var source = Matrix4x4.TRS(Position, Quaternion.Euler(Rotation), Scale);
        return GetSymmetryMatrices().Select(m => m * source).ToList();
    }

    private List<Matrix4x4> GetSymmetryMatrices()
    {
        var matrices = new List<Matrix4x4>();
        var transformEach = Matrix4x4.TRS(PositionEach, Quaternion.Euler(RotationEach), ScaleEach);
        var cumulative = transformEach;
        foreach (var m in sym.matrices)
        {
            matrices.Add(ApplyAfter ? cumulative * m : m * cumulative);
            cumulative *= transformEach;
        }
        return matrices;
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
            for (var i = 0; i < matrices.Count; i++)
            {
                var matrix = matrices[i];
                Gizmos.color = i == 0 ? Color.white
                    : matrix.determinant < 0 ? new Color(1f, 0.7f, 0.15f) : Color.blue;
                foreach (var edge in domainOutline.edges)
                    Gizmos.DrawLine(matrix.MultiplyPoint3x4(domainOutline.vertices[edge.x]),
                        matrix.MultiplyPoint3x4(domainOutline.vertices[edge.y]));
            }
        }
        if (symmetryGizmos)
        {
            if (gizmoPath == null || gizmoPath.Count == 0)
            {
                gizmoPath = new List<Vector3>
                {
                    new Vector3(-0.25f, -0.5f, 0),
                    new Vector3(0.25f, -0.5f, 0),
                    new Vector3(0.25f, -0.2f, 0),
                    new Vector3(-0.05f, -0.2f, 0),
                    new Vector3(-0.05f, 0.5f, 0),
                    // new Vector3(-0.25f, 0.5f, 0),
                };
            }

            foreach (var m in matrices)
            {
                var path = gizmoPath.Select(v => m.MultiplyPoint3x4(v)).ToList();
                DrawPathGizmo(path);
            }
        }

    }

    private void DrawPathGizmo(List<Vector3> path)
    {
        var initialPoint = path[0];
        var prevPoint = initialPoint;
        for (int i = 1; i < path.Count; i++)
        {
            if (i==1) Gizmos.color = Color.red;
            else Gizmos.color = Color.yellow;

            var currentPoint = path[i];
            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(prevPoint, initialPoint);
    }
}
