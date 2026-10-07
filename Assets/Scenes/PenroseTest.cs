using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public sealed class PenroseTest : MonoBehaviour
{
    [Range(0, 7)] public int subdivisions = 4;
    [Min(0.01f)] public float radius = 5f;
    public bool showOutlines = true;
    public bool showMotifs = true;
    [Range(0.01f, 0.5f)] public float motifSize = 0.12f;

    public int TileCount => tiling.tiles.Count;
    public int BoundaryTileCount { get; private set; }
    public Mesh OutlineMesh => outlineMesh;
    public Material OutlineMaterial => outlineMaterial;

    private PenroseTiling tiling;
    private Mesh outlineMesh;
    private Material outlineMaterial;

    private void OnEnable() { OnValidate(); }

    private void OnValidate()
    {
        tiling = new PenroseTiling(subdivisions, radius);
        if (outlineMesh != null) DestroyImmediate(outlineMesh);
        outlineMesh = new Mesh { name = "Penrose tile outlines", hideFlags = HideFlags.HideAndDontSave };
        outlineMesh.indexFormat = IndexFormat.UInt32;
        var positions = new List<Vector3>();
        var colors = new List<Color>();
        var indices = new List<int>();
        var edges = new HashSet<(int, int)>();
        BoundaryTileCount = 0;
        foreach (var tile in tiling.tiles)
        {
            if (!tile.completeRhomb) BoundaryTileCount++;
            var color = tile.kind == PenroseTiling.Kind.Thin
                ? new Color(1f, 0.7f, 0.15f) : new Color(0.2f, 0.65f, 1f);
            for (var i = 0; i < tile.indices.Length; i++)
            {
                var a = tile.indices[i];
                var b = tile.indices[(i + 1) % tile.indices.Length];
                if (!edges.Add((Mathf.Min(a, b), Mathf.Max(a, b)))) continue;
                indices.Add(positions.Count);
                positions.Add(tiling.vertices[a]);
                colors.Add(color);
                indices.Add(positions.Count);
                positions.Add(tiling.vertices[b]);
                colors.Add(color);
            }
        }
        outlineMesh.SetVertices(positions);
        outlineMesh.SetColors(colors);
        outlineMesh.SetIndices(indices, MeshTopology.Lines, 0);
        outlineMesh.RecalculateBounds();
        if (outlineMaterial == null)
        {
            outlineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"))
                { name = "Penrose outline colors", hideFlags = HideFlags.HideAndDontSave };
            outlineMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            outlineMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            outlineMaterial.SetInt("_Cull", (int)CullMode.Off);
            outlineMaterial.SetInt("_ZWrite", 0);
        }
    }

    public List<Matrix4x4> GetDrawMatrices()
    {
        var matrices = new List<Matrix4x4>(tiling.matrices.Count);
        var scale = Matrix4x4.Scale(Vector3.one * (tiling.edgeLength * motifSize));
        foreach (var placement in tiling.matrices) matrices.Add(placement * scale);
        return matrices;
    }

    private void Update()
    {
        var renderer = GetComponent<MeshRenderer>();
        renderer.enabled = false;
        if (showOutlines) Graphics.DrawMesh(outlineMesh, Matrix4x4.identity, outlineMaterial, 0);
        if (!showMotifs) return;
        var mesh = GetComponent<MeshFilter>().sharedMesh;
        var matrices = GetDrawMatrices();
        for (var start = 0; start < matrices.Count; start += 1023)
        {
            var batch = matrices.GetRange(start, Mathf.Min(1023, matrices.Count - start));
            for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
                Graphics.DrawMeshInstanced(mesh, submesh, renderer.sharedMaterial, batch,
                    null, ShadowCastingMode.TwoSided, true);
        }
    }

    private void OnDisable()
    {
        if (outlineMesh != null) DestroyImmediate(outlineMesh);
        if (outlineMaterial != null) DestroyImmediate(outlineMaterial);
        outlineMesh = null;
        outlineMaterial = null;
    }
}
