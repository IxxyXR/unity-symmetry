using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The thirteen general line-group families, with local Z as the line axis.</summary>
public sealed class LineGroupSymmetry
{
    // Family numbering follows Damnjanovic and Milosevic, Line Groups in Physics, Table 2.2.
    public enum Family
    {
        ScrewRotations = 1,
        TranslationRotoreflection = 2,
        TranslationHorizontalMirror = 3,
        HalfStepHorizontalMirror = 4,
        ScrewHalfTurns = 5,
        TranslationVerticalMirrors = 6,
        GlideRotations = 7,
        HalfStepVerticalMirrors = 8,
        TranslationDiagonalMirrors = 9,
        GlideRotoreflection = 10,
        TranslationFullMirrors = 11,
        GlideHorizontalMirror = 12,
        HalfStepFullMirrors = 13
    }

    public readonly List<Matrix4x4> matrices;
    public readonly string name;

    public static bool UsesFreeAngle(Family family) => family == Family.ScrewRotations || family == Family.ScrewHalfTurns;

    /// <param name="n">Positive axial rotation order, including noncrystallographic orders.</param>
    /// <param name="repeats">Positive count of generalized steps, starting at step zero.</param>
    /// <param name="advance">Positive distance along Z per generalized step.</param>
    /// <param name="angleDegrees">Twist per step for families 1 and 5; other families determine it from n.</param>
    public LineGroupSymmetry(Family family, int n, int repeats, float advance, float angleDegrees = 0f)
    {
        if ((int)family < 1 || (int)family > 13) throw new ArgumentOutOfRangeException(nameof(family));
        if (n < 1) throw new ArgumentOutOfRangeException(nameof(n));
        if (repeats < 1) throw new ArgumentOutOfRangeException(nameof(repeats));
        if (advance <= 0 || float.IsNaN(advance) || float.IsInfinity(advance))
            throw new ArgumentOutOfRangeException(nameof(advance));
        if (UsesFreeAngle(family) && (float.IsNaN(angleDegrees) || float.IsInfinity(angleDegrees)))
            throw new ArgumentOutOfRangeException(nameof(angleDegrees));

        name = family.ToString();
        var horizontalMirror = Matrix4x4.Scale(new Vector3(1, 1, -1));
        var verticalMirror = Matrix4x4.Scale(new Vector3(1, -1, 1));
        var halfTurn = Matrix4x4.Scale(new Vector3(1, -1, -1));
        var halfRotation = Matrix4x4.Rotate(Quaternion.AngleAxis(180f / n, Vector3.forward));
        var rotoreflection = halfRotation * horizontalMirror;
        var diagonalHalfTurn = halfRotation * halfTurn;
        var cosets = new List<Matrix4x4> { Matrix4x4.identity };
        switch (family)
        {
            case Family.TranslationRotoreflection:
            case Family.GlideRotoreflection:
                cosets.Add(rotoreflection); break;
            case Family.TranslationHorizontalMirror:
            case Family.HalfStepHorizontalMirror:
            case Family.GlideHorizontalMirror:
                cosets.Add(horizontalMirror); break;
            case Family.ScrewHalfTurns:
                cosets.Add(halfTurn); break;
            case Family.TranslationVerticalMirrors:
            case Family.HalfStepVerticalMirrors:
                cosets.Add(verticalMirror); break;
            case Family.TranslationDiagonalMirrors:
                cosets.Add(verticalMirror);
                cosets.Add(diagonalHalfTurn);
                cosets.Add(rotoreflection);
                break;
            case Family.TranslationFullMirrors:
            case Family.HalfStepFullMirrors:
                cosets.Add(verticalMirror);
                cosets.Add(halfTurn);
                cosets.Add(horizontalMirror);
                break;
        }

        var halfStep = family == Family.HalfStepHorizontalMirror || family == Family.HalfStepVerticalMirrors
            || family == Family.HalfStepFullMirrors;
        var glideStep = family == Family.GlideRotations || family == Family.GlideRotoreflection
            || family == Family.GlideHorizontalMirror;
        var stepAngle = UsesFreeAngle(family) ? angleDegrees : halfStep ? 180f / n : 0f;
        matrices = new List<Matrix4x4>();
        for (var repeat = 0; repeat < repeats; repeat++)
        {
            // Direct powers avoid accumulated drift; a glide reflects on odd steps only.
            var step = Matrix4x4.TRS(Vector3.forward * (repeat * advance),
                Quaternion.AngleAxis(repeat * stepAngle, Vector3.forward), Vector3.one);
            if (glideStep && (repeat & 1) != 0) step *= verticalMirror;
            foreach (var coset in cosets)
            for (var rotation = 0; rotation < n; rotation++)
            {
                var axialRotation = Matrix4x4.Rotate(Quaternion.AngleAxis(rotation * (360f / n), Vector3.forward));
                matrices.Add(step * coset * axialRotation);
            }
        }
        matrices[0] = Matrix4x4.identity;
    }
}
