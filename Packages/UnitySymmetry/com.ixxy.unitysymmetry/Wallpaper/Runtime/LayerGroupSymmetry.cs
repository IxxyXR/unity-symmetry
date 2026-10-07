using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The 80 crystallographic layer groups, repeating in local XY.</summary>
public sealed class LayerGroupSymmetry
{
    public readonly List<Matrix4x4> matrices;
    public readonly string name;

    /// <param name="number">International layer-group number (1 to 80).</param>
    /// <param name="repeats">Positive cell counts along the two in-plane lattice vectors.</param>
    /// <param name="cellSize">Positive in-plane cell edge length; source height is unchanged.</param>
    public LayerGroupSymmetry(int number, Vector2Int repeats, float cellSize)
    {
        if (number < 1 || number > 80) throw new ArgumentOutOfRangeException(nameof(number));
        if (repeats.x < 1 || repeats.y < 1) throw new ArgumentOutOfRangeException(nameof(repeats));
        if (cellSize <= 0 || float.IsNaN(cellSize) || float.IsInfinity(cellSize))
            throw new ArgumentOutOfRangeException(nameof(cellSize));

        name = LayerGroupData.Names[number - 1];
        var basis = Matrix4x4.Scale(new Vector3(cellSize, cellSize, 1));
        if (number >= 65)
            basis.SetColumn(1, new Vector4(-0.5f * cellSize, Mathf.Sqrt(3f) * 0.5f * cellSize, 0, 0));
        var inverseBasis = basis.inverse;
        var operations = LayerGroupData.Operations[number - 1];
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
            cellOperations.Add(basis * fractional * inverseBasis);
        }
        cellOperations[0] = Matrix4x4.identity;

        matrices = new List<Matrix4x4>();
        for (var x = 0; x < repeats.x; x++)
        for (var y = 0; y < repeats.y; y++)
        {
            var translation = Matrix4x4.Translate(basis.MultiplyVector(new Vector3(x, y, 0)));
            foreach (var operation in cellOperations)
                matrices.Add(translation * operation);
        }
    }
}
