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
    private readonly List<Matrix4x4> cellOperations;

    public sealed class DomainOutline
    {
        public readonly Vector3 seed;
        public readonly Vector3[] vertices;
        public readonly Vector2Int[] edges;

        internal DomainOutline(Vector3 seed, Vector3[] vertices, Vector2Int[] edges)
        {
            this.seed = seed;
            this.vertices = vertices;
            this.edges = edges;
        }
    }

    /// <summary>A Dirichlet drawing domain around a general-position point.
    /// Applying the group matrices gives adjacent domains with shared boundaries.</summary>
    public DomainOutline CreateDomainOutline()
        => CreateDomainOutline(new Vector3(0.173f, 0.317f, 0.419f));

    /// <param name="fractionalSeed">Source point in fractional cell coordinates.
    /// It must be in general position, away from fixed points of group operations.</param>
    public DomainOutline CreateDomainOutline(Vector3 fractionalSeed)
    {
        if (float.IsNaN(fractionalSeed.x) || float.IsNaN(fractionalSeed.y) || float.IsNaN(fractionalSeed.z)
            || float.IsInfinity(fractionalSeed.x) || float.IsInfinity(fractionalSeed.y) || float.IsInfinity(fractionalSeed.z))
            throw new ArgumentOutOfRangeException(nameof(fractionalSeed));
        var size = cellBasis.MultiplyVector(Vector3.right).magnitude;
        var epsilon = size * 0.00001f;
        var seed = cellBasis.MultiplyPoint3x4(fractionalSeed);
        // Whole-cell seed shifts leave the orbit unchanged. Work within one cell
        // to keep neighbour calculations precise, then return the requested source frame.
        var seedInCell = cellBasis.MultiplyPoint3x4(new Vector3(
            fractionalSeed.x - Mathf.Floor(fractionalSeed.x),
            fractionalSeed.y - Mathf.Floor(fractionalSeed.y),
            fractionalSeed.z - Mathf.Floor(fractionalSeed.z)));
        var corners = new Vector3[8];
        for (var i = 0; i < corners.Length; i++)
            corners[i] = new Vector3((i & 1) == 0 ? -size : size,
                (i & 2) == 0 ? -size : size, (i & 4) == 0 ? -size : size);
        var faces = new List<List<Vector3>>();
        foreach (var indices in new[] { new[] {0, 1, 3, 2}, new[] {4, 6, 7, 5},
            new[] {0, 4, 5, 1}, new[] {2, 3, 7, 6}, new[] {0, 2, 6, 4}, new[] {1, 5, 7, 3} })
        {
            var face = new List<Vector3>();
            foreach (var index in indices) face.Add(corners[index]);
            faces.Add(face);
        }
        var a = cellBasis.MultiplyVector(Vector3.right);
        var b = cellBasis.MultiplyVector(Vector3.up);
        var c = cellBasis.MultiplyVector(Vector3.forward);
        // Bound the domain by the translation lattice first, including the third
        // nearest direction of the hexagonal basis. The resulting radius bounds
        // which symmetry-equivalent neighbours can contribute a face.
        foreach (var translation in new[] {a, -a, b, -b, c, -c, a + b, -a - b})
            faces = ClipDomain(faces, translation.normalized, translation.magnitude * 0.5f, epsilon);
        var radius = DomainRadius(faces);
        var neighbours = new List<Vector3>();
        var inverseBasis = cellBasis.inverse;
        for (var operationIndex = 0; operationIndex < cellOperations.Count; operationIndex++)
        {
            var delta = cellOperations[operationIndex].MultiplyPoint3x4(seedInCell) - seedInCell;
            var nearestCell = -Vector3Int.RoundToInt(inverseBasis.MultiplyVector(delta));
            // For the supported equal-edge orthogonal and hexagonal metrics, two
            // cells around the nearest image include every neighbour within 2*radius.
            for (var x = -2; x <= 2; x++)
            for (var y = -2; y <= 2; y++)
            for (var z = -2; z <= 2; z++)
            {
                var offset = new Vector3(nearestCell.x + x, nearestCell.y + y, nearestCell.z + z);
                var neighbour = delta + cellBasis.MultiplyVector(offset);
                if (neighbour.sqrMagnitude <= epsilon * epsilon)
                {
                    if (operationIndex != 0)
                        throw new ArgumentException("The domain seed lies on a symmetry element; choose a general-position point.", nameof(fractionalSeed));
                    continue;
                }
                if (neighbour.sqrMagnitude <= 4f * radius * radius) neighbours.Add(neighbour);
            }
        }
        neighbours.Sort((left, right) => left.sqrMagnitude.CompareTo(right.sqrMagnitude));
        foreach (var neighbour in neighbours)
        {
            // Farther bisectors cannot intersect the current bounded polyhedron.
            var distance = neighbour.magnitude;
            if (distance > 2f * radius + epsilon) break;
            faces = ClipDomain(faces, neighbour / distance, distance * 0.5f, epsilon);
            radius = DomainRadius(faces);
        }
        var vertices = new List<Vector3>();
        var edges = new List<Vector2Int>();
        var edgeKeys = new HashSet<(int, int)>();
        foreach (var face in faces)
        {
            var indices = new int[face.Count];
            for (var i = 0; i < face.Count; i++)
            {
                var vertex = face[i];
                var index = vertices.FindIndex(v => (v - vertex).sqrMagnitude <= epsilon * epsilon);
                if (index < 0) { index = vertices.Count; vertices.Add(vertex); }
                indices[i] = index;
            }
            for (var i = 0; i < indices.Length; i++)
            {
                var first = indices[i];
                var second = indices[(i + 1) % indices.Length];
                var key = (Mathf.Min(first, second), Mathf.Max(first, second));
                if (first != second && edgeKeys.Add(key)) edges.Add(new Vector2Int(first, second));
            }
        }
        for (var i = 0; i < vertices.Count; i++) vertices[i] += seed;
        return new DomainOutline(seed, vertices.ToArray(), edges.ToArray());
    }

    private static float DomainRadius(List<List<Vector3>> faces)
    {
        var squaredRadius = 0f;
        foreach (var face in faces)
        foreach (var vertex in face) squaredRadius = Mathf.Max(squaredRadius, vertex.sqrMagnitude);
        return Mathf.Sqrt(squaredRadius);
    }

    private static List<List<Vector3>> ClipDomain(List<List<Vector3>> faces, Vector3 normal, float distance, float epsilon)
    {
        var result = new List<List<Vector3>>();
        var cap = new List<Vector3>();
        foreach (var face in faces)
        {
            var clipped = new List<Vector3>();
            var previous = face[face.Count - 1];
            var previousDistance = Vector3.Dot(normal, previous) - distance;
            foreach (var current in face)
            {
                var currentDistance = Vector3.Dot(normal, current) - distance;
                var previousInside = previousDistance <= epsilon;
                var currentInside = currentDistance <= epsilon;
                if (previousInside != currentInside)
                {
                    var intersection = Vector3.LerpUnclamped(previous, current,
                        previousDistance / (previousDistance - currentDistance));
                    clipped.Add(intersection);
                    if (!cap.Exists(v => (v - intersection).sqrMagnitude <= epsilon * epsilon)) cap.Add(intersection);
                }
                if (currentInside) clipped.Add(current);
                previous = current;
                previousDistance = currentDistance;
            }
            if (clipped.Count >= 3) result.Add(clipped);
        }
        if (cap.Count >= 3)
        {
            var center = Vector3.zero;
            foreach (var vertex in cap) center += vertex;
            center /= cap.Count;
            var tangent = Vector3.Cross(normal, Mathf.Abs(normal.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            var bitangent = Vector3.Cross(normal, tangent);
            cap.Sort((left, right) => Mathf.Atan2(Vector3.Dot(left - center, bitangent), Vector3.Dot(left - center, tangent))
                .CompareTo(Mathf.Atan2(Vector3.Dot(right - center, bitangent), Vector3.Dot(right - center, tangent))));
            result.Add(cap);
        }
        return result;
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

        var index = number - 1;
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
        cellOperations = new List<Matrix4x4>(operations.Length / 12);
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
