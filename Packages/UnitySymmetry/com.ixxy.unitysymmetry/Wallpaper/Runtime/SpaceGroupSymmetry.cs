using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The 230 crystallographic space groups, using the first Hall setting for each.
/// Operations act on Cartesian coordinates; the finite repeat grid starts at cell zero.
/// </summary>
public sealed class SpaceGroupSymmetry
{
    public readonly List<Matrix4x4> matrices;
    public readonly Matrix4x4 cellBasis;
    public readonly string name;
    public readonly string setting;
    public readonly int hallNumber;
    private readonly int groupIndex;

    public sealed class DomainOutline
    {
        public readonly Vector3[] vertices;
        public readonly Vector2Int[] edges;

        internal DomainOutline(Vector3[] vertices, Vector2Int[] edges)
        {
            this.vertices = vertices;
            this.edges = edges;
        }
    }

    /// <summary>A fixed asymmetric-unit drawing region in this group's Hall setting.
    /// Applying the group matrices gives regions with disjoint interiors.
    /// Shared faces are included in both outlines for visualization.</summary>
    public DomainOutline CreateDomainOutline()
    {
        var fractionalVertices = SpaceGroupDomainData.Vertices[groupIndex];
        var vertices = new Vector3[fractionalVertices.Length];
        for (var i = 0; i < vertices.Length; i++)
            vertices[i] = cellBasis.MultiplyPoint3x4(fractionalVertices[i]);
        return new DomainOutline(vertices, (Vector2Int[])SpaceGroupDomainData.Edges[groupIndex].Clone());
    }

    /// <param name="number">International space-group number (1 to 230).</param>
    /// <param name="repeats">Positive cell counts along the three lattice vectors.</param>
    /// <param name="cellSize">Conventional cell edge length. Hexagonal cells use a 120 degree a/b angle.</param>
    public SpaceGroupSymmetry(int number, Vector3Int repeats, float cellSize)
    {
        if (number < 1 || number > 230) throw new ArgumentOutOfRangeException(nameof(number));
        if (repeats.x < 1 || repeats.y < 1 || repeats.z < 1)
            throw new ArgumentOutOfRangeException(nameof(repeats));
        if (cellSize <= 0 || float.IsNaN(cellSize) || float.IsInfinity(cellSize))
            throw new ArgumentOutOfRangeException(nameof(cellSize));

        groupIndex = number - 1;
        var index = groupIndex;
        name = SpaceGroupData.Names[index];
        setting = SpaceGroupData.Settings[index];
        hallNumber = SpaceGroupData.HallNumbers[index];
        cellBasis = Matrix4x4.Scale(Vector3.one * cellSize);
        if (number >= 143 && number <= 194)
        {
            // Trigonal (including rhombohedral groups in their H setting) and hexagonal.
            // Keep c along Z, as in the conventional crystallographic coordinates.
            cellBasis.SetColumn(1, new Vector4(-0.5f * cellSize, Mathf.Sqrt(3f) * 0.5f * cellSize, 0, 0));
        }

        var operations = SpaceGroupData.Operations[index];
        var inverseBasis = cellBasis.inverse;
        var cellOperations = new List<Matrix4x4>(operations.Length / 12);
        for (var offset = 0; offset < operations.Length; offset += 12)
        {
            var fractional = Matrix4x4.identity;
            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 3; column++)
                    fractional[row, column] = operations[offset + row * 3 + column];
                fractional[row, 3] = operations[offset + 9 + row] / 12f;
            }
            cellOperations.Add(cellBasis * fractional * inverseBasis);
        }
        cellOperations[0] = Matrix4x4.identity;

        matrices = new List<Matrix4x4>();
        for (var x = 0; x < repeats.x; x++)
        for (var y = 0; y < repeats.y; y++)
        for (var z = 0; z < repeats.z; z++)
        {
            var translation = Matrix4x4.Translate(cellBasis.MultiplyVector(new Vector3(x, y, z)));
            foreach (var operation in cellOperations)
                matrices.Add(translation * operation);
        }
    }
}
