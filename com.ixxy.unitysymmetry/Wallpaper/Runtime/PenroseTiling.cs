using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>A finite Penrose rhomb patch obtained by golden-ratio triangle subdivision.</summary>
public sealed class PenroseTiling
{
    public enum Kind { Thin, Thick }
    public enum TileSelection { Both, Thin, Thick }

    public readonly struct Tile
    {
        public readonly Kind kind;
        public readonly int[] indices;
        public bool completeRhomb => indices.Length == 4;
        public Tile(Kind kind, int[] indices) { this.kind = kind; this.indices = indices; }
    }

    private readonly struct Triangle
    {
        public readonly Kind kind;
        public readonly int a, b, c;
        public Triangle(Kind kind, int a, int b, int c)
        { this.kind = kind; this.a = a; this.b = b; this.c = c; }
    }

    public readonly List<Vector2> vertices = new List<Vector2>();
    public readonly List<Tile> tiles = new List<Tile>();
    public readonly List<Matrix4x4> matrices = new List<Matrix4x4>();
    public readonly float edgeLength;

    /// <param name="subdivisions">Nonnegative subdivision count; each level increases tile density.</param>
    /// <param name="radius">Positive circumradius of the decagonal seed patch in local XY.</param>
    /// <param name="tileSelection">Tile types receiving placement transforms; tile geometry remains complete.</param>
    public PenroseTiling(int subdivisions, float radius, TileSelection tileSelection = TileSelection.Both)
    {
        if (subdivisions < 0) throw new ArgumentOutOfRangeException(nameof(subdivisions));
        if (radius <= 0 || float.IsNaN(radius) || float.IsInfinity(radius))
            throw new ArgumentOutOfRangeException(nameof(radius));
        var inversePhi = 2f / (1f + Mathf.Sqrt(5f));
        edgeLength = radius * Mathf.Pow(inversePhi, subdivisions);
        vertices.Add(Vector2.zero);
        for (var i = 0; i < 10; i++)
        {
            var angle = (2 * i - 1) * Mathf.PI / 10f;
            vertices.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
        var triangles = new List<Triangle>();
        for (var i = 0; i < 10; i++)
        {
            var b = i + 1;
            var c = (i + 1) % 10 + 1;
            // Alternating handedness supplies the matching subdivision orientations.
            triangles.Add((i & 1) == 0
                ? new Triangle(Kind.Thin, 0, c, b) : new Triangle(Kind.Thin, 0, b, c));
        }
        for (var level = 0; level < subdivisions; level++)
        {
            var next = new List<Triangle>();
            var divisionPoints = new Dictionary<(int, int), int>();
            foreach (var triangle in triangles)
            {
                var a = triangle.a;
                var b = triangle.b;
                var c = triangle.c;
                if (triangle.kind == Kind.Thin)
                {
                    var point = Divide(a, b, inversePhi, divisionPoints);
                    next.Add(new Triangle(Kind.Thin, c, point, b));
                    next.Add(new Triangle(Kind.Thick, point, c, a));
                }
                else
                {
                    var q = Divide(b, a, inversePhi, divisionPoints);
                    var point = Divide(b, c, inversePhi, divisionPoints);
                    next.Add(new Triangle(Kind.Thick, point, c, a));
                    next.Add(new Triangle(Kind.Thick, q, point, b));
                    next.Add(new Triangle(Kind.Thin, point, q, a));
                }
            }
            triangles = next;
        }

        var unpaired = new Dictionary<(int, int), Triangle>();
        foreach (var triangle in triangles)
        {
            var key = BaseEdge(triangle);
            if (unpaired.TryGetValue(key, out var partner))
            {
                tiles.Add(new Tile(triangle.kind, new[] { triangle.a, triangle.b, partner.a, triangle.c }));
                unpaired.Remove(key);
            }
            else unpaired.Add(key, triangle);
        }
        // Preserve deterministic tile order for the remaining clipped boundary rhombs.
        foreach (var triangle in triangles)
            if (unpaired.ContainsKey(BaseEdge(triangle)))
                tiles.Add(new Tile(triangle.kind, new[] { triangle.a, triangle.b, triangle.c }));

        foreach (var tile in tiles)
        {
            if (tileSelection == TileSelection.Thin && tile.kind != Kind.Thin
                || tileSelection == TileSelection.Thick && tile.kind != Kind.Thick) continue;
            var center = Vector2.zero;
            foreach (var index in tile.indices) center += vertices[index];
            center /= tile.indices.Length;
            var direction = vertices[tile.indices[1]] - vertices[tile.indices[0]];
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            matrices.Add(Matrix4x4.TRS(new Vector3(center.x, center.y, 0),
                Quaternion.AngleAxis(angle, Vector3.forward), Vector3.one));
        }
    }

    private static (int, int) BaseEdge(Triangle triangle)
        => (Mathf.Min(triangle.b, triangle.c), Mathf.Max(triangle.b, triangle.c));

    private int Divide(int start, int end, float fraction, Dictionary<(int, int), int> points)
    {
        var key = (start, end);
        if (points.TryGetValue(key, out var existing)) return existing;
        var index = vertices.Count;
        vertices.Add(Vector2.LerpUnclamped(vertices[start], vertices[end], fraction));
        points.Add(key, index);
        return index;
    }
}
