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
    /// <summary>Local X interval of one fundamental drawing region.</summary>
    public readonly Vector2 domainXRange;
    /// <summary>True when the source region occupies Y >= 0; otherwise it spans all Y.</summary>
    public readonly bool domainAboveAxis;

    /// <summary>One source region, clipped in the nonperiodic Y direction for display.
    /// Transform this polygon with each matrix to show where copies of the drawing go.</summary>
    public Vector3[] CreateDomainOutline(float stripHalfWidth)
    {
        if (stripHalfWidth <= 0 || float.IsNaN(stripHalfWidth) || float.IsInfinity(stripHalfWidth))
            throw new ArgumentOutOfRangeException(nameof(stripHalfWidth));
        var lower = domainAboveAxis ? 0f : -stripHalfWidth;
        return new[] {
            new Vector3(domainXRange.x, lower, 0),
            new Vector3(domainXRange.y, lower, 0),
            new Vector3(domainXRange.y, stripHalfWidth, 0),
            new Vector3(domainXRange.x, stripHalfWidth, 0)
        };
    }

    public FriezeSymmetry(Group group, int repeats, float period)
    {
        if ((int)group < 1 || (int)group > 7) throw new ArgumentOutOfRangeException(nameof(group));
        if (repeats < 1) throw new ArgumentOutOfRangeException(nameof(repeats));
        if (period <= 0 || float.IsNaN(period) || float.IsInfinity(period))
            throw new ArgumentOutOfRangeException(nameof(period));

        name = group.ToString();
        domainAboveAxis = group == Group.p2 || group == Group.p11m
            || group == Group.p2mm || group == Group.p2mg;
        switch (group)
        {
            case Group.p1m1:
            case Group.p2mm:
            case Group.p2mg:
                // The perpendicular mirror reflects this half-period into negative X.
                domainXRange = new Vector2(0, period * 0.5f);
                break;
            case Group.p11g:
                // A glide supplies the other half-period, reflecting Y as it advances.
                domainXRange = new Vector2(-period * 0.25f, period * 0.25f);
                break;
            default:
                domainXRange = new Vector2(-period * 0.5f, period * 0.5f);
                break;
        }
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
