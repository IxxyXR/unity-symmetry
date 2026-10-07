using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine.Rendering;


[ExecuteInEditMode]
public class SpaceGroupTest : MonoBehaviour
{
    [Header("Symmetry")]
    [Range(1, 230)] public int spaceGroup = 19;
    public Vector3Int repeats = new Vector3Int(2, 2, 2);
    [Min(0.01f)] public float cellSize = 2f;
    [ReadOnly] public string groupName;
    [ReadOnly] public string setting;
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

    [BoxGroup("Gizmos")] public bool symmetryGizmos;

    private SpaceGroupSymmetry sym;
    private List<Vector3> gizmoPath;

    private void OnEnable() { OnValidate(); }

    private void OnValidate()
    {
        repeats = Vector3Int.Max(repeats, Vector3Int.one);
        sym = new SpaceGroupSymmetry(spaceGroup, repeats, cellSize);
        groupName = sym.name;
        setting = sym.setting;
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
