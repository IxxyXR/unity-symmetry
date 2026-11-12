using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The 230 crystallographic space groups, using the first Hall setting for each.
/// Operations act on Cartesian coordinates; repeats use primitive lattice cells starting at cell zero.
/// </summary>
public sealed class SpaceGroupSymmetry
{
    public readonly List<Matrix4x4> matrices;
    public readonly Matrix4x4 cellBasis;
    public readonly string name;
    public readonly string setting;
    public readonly int hallNumber;
    private readonly int groupIndex;
    private readonly Matrix4x4 conventionalCellBasis;

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
        // Domain data is authored in conventional fractional coordinates. Changing
        // the repeat lattice must not deform the physical asymmetric unit.
        for (var i = 0; i < vertices.Length; i++)
            vertices[i] = conventionalCellBasis.MultiplyPoint3x4(fractionalVertices[i]);
        return new DomainOutline(vertices, (Vector2Int[])SpaceGroupDomainData.Edges[groupIndex].Clone());
    }

    /// <param name="number">International space-group number (1 to 230).</param>
    /// <param name="repeats">Positive primitive-cell counts along the three lattice vectors.</param>
    /// <param name="cellSize">Reference conventional-cell edge length; primitive vectors are derived from this basis.</param>
    /// <param name="compact">Choose lattice-equivalent copies that form a cohesive drawing cluster. False keeps the database representatives.</param>
    public SpaceGroupSymmetry(int number, Vector3Int repeats, float cellSize, bool compact = false)
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
        conventionalCellBasis = Matrix4x4.Scale(Vector3.one * cellSize);
        if (number >= 143 && number <= 194)
        {
            // Trigonal (including rhombohedral groups in their H setting) and hexagonal.
            // Keep c along Z, as in the conventional crystallographic coordinates.
            conventionalCellBasis.SetColumn(1, new Vector4(-0.5f * cellSize, Mathf.Sqrt(3f) * 0.5f * cellSize, 0, 0));
        }

        cellBasis = conventionalCellBasis * CreatePrimitiveBasis(name[0]);

        var operations = SpaceGroupData.Operations[index];
        var shifts = compact ? SpaceGroupRepresentativeData.PrimitiveShifts[index] : Array.Empty<sbyte>();
        var inverseBasis = conventionalCellBasis.inverse;
        var rotations = new HashSet<Matrix4x4>();
        var cellOperations = new List<Matrix4x4>(operations.Length / 12);
        for (var offset = 0; offset < operations.Length; offset += 12)
        {
            var fractional = Matrix4x4.identity;
            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 3; column++)
                    fractional[row, column] = operations[offset + row * 3 + column];
            }
            // Operations with the same rotation differ only by a primitive lattice
            // translation. Keep one representative of each rotation/reflection.
            if (!rotations.Add(fractional)) continue;
            for (var row = 0; row < 3; row++)
                fractional[row, 3] = operations[offset + 9 + row] / 12f;
            var operation = conventionalCellBasis * fractional * inverseBasis;
            if (shifts.Length != 0)
            {
                // Choose the precomputed lattice image that keeps this repeat cluster compact.
                // Rotations, reflection handedness and primitive repeat periods stay unchanged.
                int shiftOffset = cellOperations.Count * 3;
                var shift = new Vector3(shifts[shiftOffset], shifts[shiftOffset + 1], shifts[shiftOffset + 2]);
                operation = Matrix4x4.Translate(cellBasis.MultiplyVector(shift)) * operation;
            }
            cellOperations.Add(operation);
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

    // Columns are primitive lattice vectors in conventional fractional coordinates.
    private static Matrix4x4 CreatePrimitiveBasis(char centering)
    {
        var a = Vector3.right;
        var b = Vector3.up;
        var c = Vector3.forward;
        switch (centering)
        {
            case 'A':
                b = new Vector3(0, 0.5f, -0.5f);
                c = new Vector3(0, 0.5f, 0.5f);
                break;
            case 'B':
                a = new Vector3(0.5f, 0, -0.5f);
                c = new Vector3(0.5f, 0, 0.5f);
                break;
            case 'C':
                a = new Vector3(0.5f, -0.5f, 0);
                b = new Vector3(0.5f, 0.5f, 0);
                break;
            case 'I':
                a = new Vector3(-0.5f, 0.5f, 0.5f);
                b = new Vector3(0.5f, -0.5f, 0.5f);
                c = new Vector3(0.5f, 0.5f, -0.5f);
                break;
            case 'F':
                a = new Vector3(0, 0.5f, 0.5f);
                b = new Vector3(0.5f, 0, 0.5f);
                c = new Vector3(0.5f, 0.5f, 0);
                break;
            case 'R':
                // The database uses the hexagonal setting of rhombohedral groups.
                a = new Vector3(2f / 3f, 1f / 3f, 1f / 3f);
                b = new Vector3(-1f / 3f, 1f / 3f, 1f / 3f);
                c = new Vector3(-1f / 3f, -2f / 3f, 1f / 3f);
                break;
        }
        return new Matrix4x4(a, b, c, new Vector4(0, 0, 0, 1));
    }
}
