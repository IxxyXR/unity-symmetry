using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine.Rendering;


[ExecuteInEditMode]
public class HelicalTest : MonoBehaviour
{
    [Header("Symmetry")]
    [Min(1)] public int copies = 12;
    public float angleDegrees = 30f;
    public float advance = 0.3f;

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

    [BoxGroup("Gizmos")] public bool stepGizmos;
    [BoxGroup("Gizmos"), Min(0.01f)] public float displayRadius = 1f;

    private HelicalSymmetry sym;
    private List<Vector3> gizmoPath;

    private void OnEnable() { OnValidate(); }

    private void OnValidate()
    {
        sym = new HelicalSymmetry(copies, angleDegrees, advance);
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

        if (stepGizmos)
        {
            for (var repeat = 0; repeat < copies; repeat++)
            {
                Gizmos.color = repeat == 0 ? Color.white : Color.blue;
                var frame = sym.matrices[repeat];
                var lower = -advance * 0.5f;
                var upper = advance * 0.5f;
                const int segments = 32;
                for (var segment = 0; segment < segments; segment++)
                {
                    var angle = 2f * Mathf.PI * segment / segments;
                    var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * displayRadius;
                    angle = 2f * Mathf.PI * (segment + 1) / segments;
                    var next = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * displayRadius;
                    var a = frame.MultiplyPoint3x4(radial + Vector3.up * lower);
                    var b = frame.MultiplyPoint3x4(radial + Vector3.up * upper);
                    Gizmos.DrawLine(a, frame.MultiplyPoint3x4(next + Vector3.up * lower));
                    if (upper != lower)
                    {
                        Gizmos.DrawLine(b, frame.MultiplyPoint3x4(next + Vector3.up * upper));
                        if (segment % (segments / 4) == 0) Gizmos.DrawLine(a, b);
                    }
                }
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

            foreach (var m in sym.matrices)
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
