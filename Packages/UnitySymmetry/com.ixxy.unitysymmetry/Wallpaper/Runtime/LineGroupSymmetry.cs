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
    /// <summary>Angular extent of a fundamental domain, starting at local +X towards +Y.</summary>
    public readonly float domainAngleDegrees;
    /// <summary>Local Z bounds of a fundamental domain. Its radial extent is unbounded.</summary>
    public readonly Vector2 domainZRange;

    public sealed class DomainOutline
    {
        public readonly Vector3[] vertices;
        public readonly Vector2Int[] edges;

        internal DomainOutline(Vector3[] vertices, Vector2Int[] edges)
        {
            this.vertices = vertices;
            this.edges = edges;
        }

        /// <summary>A continuous edge walk, retracing edges so no interior diagonals are drawn.</summary>
        public Vector3[] GetWirePath()
        {
            var path = new List<Vector3> { vertices[0] };
            var visited = new bool[edges.Length];
            void Walk(int vertex)
            {
                for (var i = 0; i < edges.Length; i++)
                {
                    if (visited[i] || edges[i].x != vertex && edges[i].y != vertex) continue;
                    visited[i] = true;
                    var next = edges[i].x == vertex ? edges[i].y : edges[i].x;
                    path.Add(vertices[next]);
                    Walk(next);
                    path.Add(vertices[vertex]);
                }
            }
            Walk(0);
            return path.ToArray();
        }
    }

    /// <summary>Clip the radially unbounded domain to a cylinder for visualization.
    /// Curved outer boundaries are approximated by straight segments; matrices are unchanged.</summary>
    /// <param name="radius">Positive display radius, independent of the group's axial period.</param>
    /// <param name="arcSegments">Segments per full circle, at least three.</param>
    public DomainOutline CreateDomainOutline(float radius, int arcSegments = 48)
    {
        if (radius <= 0 || float.IsNaN(radius) || float.IsInfinity(radius))
            throw new ArgumentOutOfRangeException(nameof(radius));
        if (arcSegments < 3) throw new ArgumentOutOfRangeException(nameof(arcSegments));
        var fullCircle = domainAngleDegrees == 360f;
        var segments = Mathf.Max(1, Mathf.CeilToInt(arcSegments * domainAngleDegrees / 360f));
        var arcPoints = fullCircle ? segments : segments + 1;
        var vertices = new List<Vector3>();
        var edges = new List<Vector2Int>();
        for (var level = 0; level < 2; level++)
        {
            var offset = vertices.Count;
            var z = level == 0 ? domainZRange.x : domainZRange.y;
            for (var i = 0; i < arcPoints; i++)
            {
                var angle = Mathf.Deg2Rad * domainAngleDegrees * i / segments;
                vertices.Add(new Vector3(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle), z));
                if (i > 0) edges.Add(new Vector2Int(offset + i - 1, offset + i));
            }
            if (fullCircle) edges.Add(new Vector2Int(offset + arcPoints - 1, offset));
        }
        edges.Add(new Vector2Int(0, arcPoints));
        if (fullCircle)
        {
            // Four longitudinal edges make the cylindrical extent readable.
            for (var i = 1; i < 4; i++)
            {
                var index = i * segments / 4;
                if (index > (i - 1) * segments / 4)
                    edges.Add(new Vector2Int(index, index + arcPoints));
            }
        }
        else
        {
            edges.Add(new Vector2Int(arcPoints - 1, 2 * arcPoints - 1));
            var lowerAxis = vertices.Count;
            vertices.Add(new Vector3(0, 0, domainZRange.x));
            var upperAxis = vertices.Count;
            vertices.Add(new Vector3(0, 0, domainZRange.y));
            edges.Add(new Vector2Int(lowerAxis, 0));
            edges.Add(new Vector2Int(lowerAxis, arcPoints - 1));
            edges.Add(new Vector2Int(upperAxis, arcPoints));
            edges.Add(new Vector2Int(upperAxis, 2 * arcPoints - 1));
            edges.Add(new Vector2Int(lowerAxis, upperAxis));
        }
        return new DomainOutline(vertices.ToArray(), edges.ToArray());
    }

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

        // A Z-preserving mirror halves the angular sector. Any Z-reversing coset
        // halves the step height; its images fill the negative half of each step.
        var dividesAngle = cosets.Exists(m => m.m22 > 0 && m.m00 * m.m11 - m.m01 * m.m10 < 0);
        var dividesHeight = cosets.Exists(m => m.m22 < 0);
        domainAngleDegrees = (dividesAngle ? 180f : 360f) / n;
        domainZRange = dividesHeight ? new Vector2(0, advance * 0.5f)
            : new Vector2(-advance * 0.5f, advance * 0.5f);

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
