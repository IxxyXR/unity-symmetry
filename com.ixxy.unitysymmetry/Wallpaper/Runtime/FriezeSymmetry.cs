using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The seven planar frieze groups, repeating along local X and preserving Z.</summary>
public sealed class FriezeSymmetry
{
    // International Tables for Crystallography, Volume E numbering.
    public enum Group { p1 = 1, p2 = 2, p1m1 = 3, p11m = 4, p11g = 5, p2mm = 6, p2mg = 7 }

    public readonly List<Matrix4x4> matrices;
    public readonly string name;

    public FriezeSymmetry(Group group, int repeats, float period)
    {
        if ((int)group < 1 || (int)group > 7) throw new ArgumentOutOfRangeException(nameof(group));
        if (repeats < 1) throw new ArgumentOutOfRangeException(nameof(repeats));
        if (period <= 0 || float.IsNaN(period) || float.IsInfinity(period))
            throw new ArgumentOutOfRangeException(nameof(period));

        name = group.ToString();
        var perpendicularMirror = Matrix4x4.Scale(new Vector3(-1, 1, 1));
        var parallelMirror = Matrix4x4.Scale(new Vector3(1, -1, 1));
        var halfTurn = Matrix4x4.Scale(new Vector3(-1, -1, 1));
        var halfTranslation = Matrix4x4.Translate(Vector3.right * (period * 0.5f));
        var glide = halfTranslation * parallelMirror;
        var operations = new List<Matrix4x4> { Matrix4x4.identity };
        switch (group)
        {
            case Group.p2: operations.Add(halfTurn); break;
            case Group.p1m1: operations.Add(perpendicularMirror); break;
            case Group.p11m: operations.Add(parallelMirror); break;
            case Group.p11g: operations.Add(glide); break;
            case Group.p2mm:
                operations.Add(perpendicularMirror);
                operations.Add(parallelMirror);
                operations.Add(halfTurn);
                break;
            case Group.p2mg:
                operations.Add(perpendicularMirror);
                operations.Add(glide);
                operations.Add(halfTranslation * halfTurn);
                break;
        }
        matrices = new List<Matrix4x4>();
        for (var repeat = 0; repeat < repeats; repeat++)
        {
            var translation = Matrix4x4.Translate(Vector3.right * (repeat * period));
            foreach (var operation in operations) matrices.Add(translation * operation);
        }
    }
}
