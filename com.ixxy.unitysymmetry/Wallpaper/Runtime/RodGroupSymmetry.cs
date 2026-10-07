using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The 75 crystallographic rod groups, repeating along local Z.</summary>
public sealed class RodGroupSymmetry
{
    public readonly List<Matrix4x4> matrices;
    public readonly string name;

    /// <param name="number">International rod-group number (1 to 75).</param>
    /// <param name="repeats">Positive number of periods, starting at period zero.</param>
    /// <param name="period">Positive repeat distance along Z in Cartesian units.</param>
    public RodGroupSymmetry(int number, int repeats, float period)
    {
        if (number < 1 || number > 75) throw new ArgumentOutOfRangeException(nameof(number));
        if (repeats < 1) throw new ArgumentOutOfRangeException(nameof(repeats));
        if (period <= 0 || float.IsNaN(period) || float.IsInfinity(period))
            throw new ArgumentOutOfRangeException(nameof(period));

        name = RodGroupData.Names[number - 1];
        var basis = Matrix4x4.Scale(new Vector3(1, 1, period));
        if (number >= 42)
            basis.SetColumn(1, new Vector4(-0.5f, Mathf.Sqrt(3f) * 0.5f, 0, 0));
        var inverseBasis = basis.inverse;
        var operations = RodGroupData.Operations[number - 1];
        var periodOperations = new List<Matrix4x4>(operations.Length / 12);
        for (var offset = 0; offset < operations.Length; offset += 12)
        {
            var fractional = Matrix4x4.identity;
            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 3; column++)
                    fractional[row, column] = operations[offset + row * 3 + column];
                fractional[row, 3] = operations[offset + 9 + row] / 12f;
            }
            periodOperations.Add(basis * fractional * inverseBasis);
        }
        periodOperations[0] = Matrix4x4.identity;

        matrices = new List<Matrix4x4>();
        for (var repeat = 0; repeat < repeats; repeat++)
        {
            var translation = Matrix4x4.Translate(Vector3.forward * (repeat * period));
            foreach (var operation in periodOperations)
                matrices.Add(translation * operation);
        }
    }
}
