"""Offline packing of symmetry-equivalent convex drawing regions.

Keep the source region fixed. Copies must form one face-connected cluster.
Prefer filled, regular clusters around natural domain centers, then shared faces
with less exposed boundary. Compact bounds and short center-to-center distances
are tie-breaks; a small bounding box alone does not decide the arrangement.
"""

from functools import lru_cache
from itertools import combinations
from math import gcd, sqrt


def dot(a, b):
    return sum(x * y for x, y in zip(a, b))


def cross(a, b):
    return (
        a[1] * b[2] - a[2] * b[1],
        a[2] * b[0] - a[0] * b[2],
        a[0] * b[1] - a[1] * b[0],
    )


def subtract(a, b):
    return tuple(x - y for x, y in zip(a, b))


def polygon(points):
    points = sorted(set(points))

    def turn(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])

    lower, upper = [], []
    for p in points:
        while len(lower) > 1 and turn(lower[-2], lower[-1], p) <= 0:
            lower.pop()
        lower.append(p)
    for p in reversed(points):
        while len(upper) > 1 and turn(upper[-2], upper[-1], p) <= 0:
            upper.pop()
        upper.append(p)
    return lower[:-1] + upper[:-1]


def intersection_area(subject, clip):
    # Convex polygon clipping. Plane matching is exact integer arithmetic;
    # floating point is used only for the area of the shared face.
    def side(a, b, p):
        return (b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0])

    output = subject
    for a, b in zip(clip, clip[1:] + clip[:1]):
        if not output:
            return 0.0
        incoming, output = output, []
        previous = incoming[-1]
        dp = side(a, b, previous)
        for current in incoming:
            dc = side(a, b, current)
            if (dp >= 0) != (dc >= 0):
                t = dp / (dp - dc)
                output.append(
                    (
                        previous[0] + t * (current[0] - previous[0]),
                        previous[1] + t * (current[1] - previous[1]),
                    )
                )
            if dc >= 0:
                output.append(current)
            previous, dp = current, dc
    return (
        abs(
            sum(
                a[0] * b[1] - a[1] * b[0]
                for a, b in zip(output, output[1:] + output[:1])
            )
        )
        * 0.5
        if output
        else 0.0
    )


def face_indices(vertices):
    planes = {}
    for ia, ib, ic in combinations(range(len(vertices)), 3):
        a, b, c = vertices[ia], vertices[ib], vertices[ic]
        n = cross(subtract(b, a), subtract(c, a))
        divisor = gcd(gcd(abs(n[0]), abs(n[1])), abs(n[2]))
        if not divisor:
            continue
        n = tuple(x // divisor for x in n)
        h = dot(n, a)
        values = [dot(n, v) - h for v in vertices]
        if min(values) < 0 < max(values):
            continue
        if max(values) > 0:
            n = tuple(-x for x in n)
            h = -h
        planes[n, h] = [i for i, v in enumerate(vertices) if dot(n, v) == h]
    return list(planes.values())


def region(vertices, indices, hexagonal):
    box = tuple(min(v[k] for v in vertices) for k in range(3)), tuple(
        max(v[k] for v in vertices) for k in range(3)
    )
    center = tuple(sum(v[k] for v in vertices) / len(vertices) for k in range(3))
    faces = {}
    for ids in indices:
        points = [vertices[i] for i in ids]
        n = None
        for a, b, c in combinations(points, 3):
            trial = cross(subtract(b, a), subtract(c, a))
            divisor = gcd(gcd(abs(trial[0]), abs(trial[1])), abs(trial[2]))
            if divisor:
                n = tuple(x // divisor for x in trial)
                break
        h = dot(n, points[0])
        if any(dot(n, v) > h for v in vertices):
            n = tuple(-x for x in n)
            h = -h
        drop = max(range(3), key=lambda k: abs(n[k]))
        axes = tuple(k for k in range(3) if k != drop)
        outline = polygon([tuple(v[k] for k in axes) for v in points])
        sy = sqrt(3) / 2 if hexagonal else 1
        weight = sqrt(n[0] ** 2 + (n[1] / sy) ** 2 + n[2] ** 2) * sy / abs(n[drop])
        faces[n] = (h, axes, outline, weight)
    return box, center, faces


def hull_volume6(points):
    """Exact six-times volume of the convex hull, using an incremental integer hull."""
    points = sorted(set(points))
    a = points[0]
    b = max(points, key=lambda p: dot(subtract(p, a), subtract(p, a)))
    c = max(
        points,
        key=lambda p: dot(
            cross(subtract(b, a), subtract(p, a)), cross(subtract(b, a), subtract(p, a))
        ),
    )
    normal = cross(subtract(b, a), subtract(c, a))
    d = max(points, key=lambda p: abs(dot(normal, subtract(p, a))))
    assert dot(normal, subtract(d, a)) != 0
    inside = tuple(a[k] + b[k] + c[k] + d[k] for k in range(3))

    def outward(face):
        x, y, z = face
        n = cross(subtract(y, x), subtract(z, x))
        return (x, z, y) if dot(n, inside) > 4 * dot(n, x) else face

    faces = [outward(f) for f in ((a, b, c), (a, b, d), (a, c, d), (b, c, d))]
    for point in points:
        visible = [
            f
            for f in faces
            if dot(
                cross(subtract(f[1], f[0]), subtract(f[2], f[0])), subtract(point, f[0])
            )
            > 0
        ]
        if not visible:
            continue
        edges = {}
        for face in visible:
            for x, y in zip(face, face[1:] + face[:1]):
                key = tuple(sorted((x, y)))
                edges[key] = edges.get(key, 0) + 1
        removed = set(visible)
        faces = [f for f in faces if f not in removed]
        faces.extend(
            outward((x, y, point)) for (x, y), count in edges.items() if count == 1
        )
    return abs(sum(dot(a, cross(b, c)) for a, b, c in faces))


def pack(vertices, copies, lattice, shifts, zero, hexagonal):
    indices = face_indices(vertices)
    regions = [region(vs, indices, hexagonal) for vs in copies]
    count = len(regions)
    if count == 1:
        return [zero], 0.0
    shift_lookup = {s: i for i, s in enumerate(shifts)}

    def box_at(i, s):
        return tuple(
            tuple(v[k] + lattice[s][k] for k in range(3)) for v in regions[i][0]
        )

    boxes = [[box_at(i, s) for s in range(len(shifts))] for i in range(count)]
    centers = [
        [tuple(regions[i][1][k] + offset[k] for k in range(3)) for offset in lattice]
        for i in range(count)
    ]

    @lru_cache(maxsize=None)
    def contact(i, j, delta):
        # Relative translation is enough, even when the two absolute shifts are
        # outside each other's candidate range.
        offset = tuple(
            sum(
                delta[a]
                * lattice[shift_lookup[tuple(1 if q == a else 0 for q in range(3))]][k]
                for a in range(3)
            )
            for k in range(3)
        )
        a = regions[i][0]
        b = tuple(tuple(v[k] + offset[k] for k in range(3)) for v in regions[j][0])
        if any(a[0][k] > b[1][k] or b[0][k] > a[1][k] for k in range(3)):
            return 0.0
        total = 0.0
        for normal, (h, axes, outline, weight) in regions[i][2].items():
            other = regions[j][2].get(tuple(-x for x in normal))
            if other is None:
                continue
            oh, _, other_outline, _ = other
            if h != -oh + dot(normal, offset):
                continue
            moved = [
                (p[0] + offset[axes[0]], p[1] + offset[axes[1]]) for p in other_outline
            ]
            total += intersection_area(outline, moved) * weight
        return round(total, 8)

    def area(i, si, j, sj):
        if i > j:
            return area(j, sj, i, si)
        delta = tuple(shifts[sj][k] - shifts[si][k] for k in range(3))
        return contact(i, j, delta)

    def shape(chosen):
        bb = [boxes[i][s] for i, s in chosen.items()]
        extent = [max(b[1][k] for b in bb) - min(b[0][k] for b in bb) for k in range(3)]
        diagonal = (
            4 * extent[0] ** 2 + 3 * extent[1] ** 2 + 4 * extent[2] ** 2
            if hexagonal
            else sum(d * d for d in extent)
        )
        center_list = [centers[i][s] for i, s in chosen.items()]
        distance = sum(
            sum(
                (a[k] - b[k]) ** 2 * (0.75 if hexagonal and k == 1 else 1)
                for k in range(3)
            )
            for a, b in combinations(center_list, 2)
        )
        return (
            extent[0] * extent[1] * extent[2],
            diagonal,
            round(distance, 8),
            sum(sum(c * c for c in shifts[s]) for s in chosen.values()),
        )

    chosen = {0: zero}
    # Grow through full or partial shared faces, never through just an edge or vertex.
    while len(chosen) < count:
        best = None
        for i in range(1, count):
            if i in chosen:
                continue
            for s in range(len(shifts)):
                joined = sum(area(i, s, j, t) for j, t in chosen.items())
                if joined <= 1e-6:
                    continue
                trial = dict(chosen)
                trial[i] = s
                value = (-round(joined, 7), shape(trial), i, s)
                if best is None or value < best[0]:
                    best = value, i, s
        if best is None:
            raise ValueError("No face-connected packing within search window")
        _, i, s = best
        chosen[i] = s

    def refine(chosen):
        for _ in range(8):
            changed = False
            for i in range(1, count):
                others = set(chosen) - {i}
                components = []
                while others:
                    component = {others.pop()}
                    pending = list(component)
                    while pending:
                        j = pending.pop()
                        adjacent = {
                            k for k in others if area(j, chosen[j], k, chosen[k]) > 1e-6
                        }
                        others -= adjacent
                        component |= adjacent
                        pending.extend(adjacent)
                    components.append(component)
                current = chosen[i]
                best = (
                    -round(
                        sum(
                            area(i, current, j, t) for j, t in chosen.items() if j != i
                        ),
                        7,
                    ),
                    shape(chosen),
                )
                for s in range(len(shifts)):
                    touching = {
                        j: area(i, s, j, t) for j, t in chosen.items() if j != i
                    }
                    if any(
                        not any(touching[j] > 1e-6 for j in component)
                        for component in components
                    ):
                        continue
                    trial = dict(chosen)
                    trial[i] = s
                    value = (-round(sum(touching.values()), 7), shape(trial))
                    if value < best:
                        best, chosen[i] = value, s
                changed |= chosen[i] != current
            if not changed:
                break
        return chosen

    def is_connected(chosen):
        visited = {0}
        while True:
            adjacent = visited | {
                j
                for j in chosen
                if any(
                    area(i, chosen[i], j, chosen[j]) > 1e-6 for i in visited if i != j
                )
            }
            if adjacent == visited:
                return len(visited) == count
            visited = adjacent

    def quality(chosen):
        # Occupied volume is constant. A smaller convex hull means less empty
        # space between copies and a fuller, more regular solid, without choosing
        # a shape merely because it fits a small axis-aligned bounding box.
        hull = hull_volume6(
            [
                tuple(v[k] + lattice[s][k] for k in range(3))
                for i, s in chosen.items()
                for v in copies[i]
            ]
        )
        shared = sum(
            area(i, chosen[i], j, chosen[j]) for i, j in combinations(range(count), 2)
        )
        return hull, -round(shared, 7), shape(chosen)

    candidates = [chosen.copy(), refine(chosen)]
    original = {i: zero for i in range(count)}
    if is_connected(original):
        candidates.extend((original, refine(original.copy())))
    # Shared domain vertices are natural centers of regular repeat blocks.
    # For example, the central vertex of P23's tetrahedron assembles its twelve
    # copies into a cube; greedy face growth alone can miss that packing.
    seen_candidates = {tuple(c[i] for i in range(count)) for c in candidates}
    for target in [*vertices, regions[0][1], (0, 0, 0)]:
        trial = {0: zero}
        for i in range(1, count):

            def distance(s):
                d = tuple(centers[i][s][k] - target[k] for k in range(3))
                return (
                    (4 * d[0] ** 2 + 3 * d[1] ** 2 + 4 * d[2] ** 2)
                    if hexagonal
                    else sum(x * x for x in d)
                )

            trial[i] = min(
                range(len(shifts)),
                key=lambda s: (distance(s), sum(x * x for x in shifts[s])),
            )
        key = tuple(trial[i] for i in range(count))
        if key not in seen_candidates and is_connected(trial):
            candidates.append(trial)
            seen_candidates.add(key)
    chosen = min(candidates, key=quality)
    alternate = refine(chosen.copy())
    chosen = min((chosen, alternate), key=quality)
    # The resulting adjacency graph must be one component.
    visited = {0}
    while True:
        next_set = visited | {
            j
            for j in chosen
            if any(area(i, chosen[i], j, chosen[j]) > 1e-6 for i in visited if i != j)
        }
        if next_set == visited:
            break
        visited = next_set
    assert len(visited) == count
    shared = sum(
        area(i, chosen[i], j, chosen[j]) for i, j in combinations(range(count), 2)
    )
    return [chosen[i] for i in range(count)], shared
