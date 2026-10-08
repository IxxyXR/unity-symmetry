"""Generate fixed space-group drawing outlines from the CCTBX asymmetric-unit tables.

uv run --python 3.12 --with gemmi==0.7.3 --with spglib==2.7.0 Tools/generate_space_group_domains.py
Only the closed geometric regions are needed; boundary ownership rules are omitted.
"""
from fractions import Fraction as F
from itertools import combinations
from pathlib import Path
from urllib.request import urlopen
import gemmi
import spglib

REVISION = "a918c3666b2065fc68b927c0f5c903a7f9042395"
SOURCE = f"https://raw.githubusercontent.com/cctbx/cctbx_project/{REVISION}/cctbx/sgtbx/direct_space_asu"
ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "Packages/UnitySymmetry/com.ixxy.unitysymmetry/Wallpaper/Runtime/SpaceGroupDomainData.cs"


def dot(a, b):
    return sum(x * y for x, y in zip(a, b))


def cross(a, b):
    return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])


class Cut:
    """The CCTBX half-space convention is n dot x + c >= 0."""
    def __init__(self, n, c):
        self.n = tuple(map(F, n))
        self.c = F(c)

    def __pos__(self): return self
    def __neg__(self): return Cut([-v for v in self.n], -self.c)
    def __invert__(self): return Cut([-v for v in self.n], self.c)
    def __mul__(self, value): return Cut(self.n, self.c * value)
    def __truediv__(self, value): return Cut(self.n, self.c / value)
    # Nested cut expressions assign shared boundaries to one copy. They do not
    # change the outline of the closed region, so ignore them when called.
    def __call__(self, boundary): return self
    def __and__(self, boundary): return self
    def __or__(self, boundary): return self

    def change_basis(self, op):
        inverse = op.inverse()
        n = tuple(sum(self.n[j] * F(inverse.rot[j][i], op.DEN) for j in range(3)) for i in range(3))
        t = tuple(F(v, op.DEN) for v in op.tran)
        return Cut(n, self.c - dot(n, t))


class Domain:
    def __init__(self, hall):
        self.hall = hall
        self.cuts = []

    def __and__(self, cut):
        self.cuts.append(cut)
        return self

    def change_basis(self, expression):
        basis = gemmi.Op(expression.replace('a', 'x').replace('b', 'y').replace('c', 'z'))
        # CCTBX a/b/c notation describes basis vectors, not coordinates.
        # Its coordinate operator is inverse(transpose(rotation), translation).
        basis.rot = [list(column) for column in zip(*basis.rot)]
        op = basis.inverse()
        operations = gemmi.symops_from_hall(self.hall)
        operations.change_basis_forward(op)
        result = Domain(gemmi.find_spacegroup_by_ops(operations).hall)
        result.cuts = [cut.change_basis(op) for cut in self.cuts]
        return result


def load_table():
    # Execute the pinned table with lightweight geometric equivalents of its
    # cut/ASU classes. No CCTBX installation is required by the generator.
    namespace = {'cut': Cut, 'direct_space_asu': Domain, 'r1': F(1)}
    for name in ['short_cuts.py', 'reference_table.py']:
        source = urlopen(f"{SOURCE}/{name}").read().decode('utf-8')
        source = '\n'.join(line for line in source.splitlines()
                           if not line.startswith(('from ', 'import ', 'r1 ='))
                           # This assertion concerns CCTBX's Hall-string spelling.
                           # The operation-set assertion below checks settings exactly.
                           and not line.lstrip().startswith('assert result.hall_symbol'))
        exec(compile(source, name, 'exec'), namespace)
    return namespace['get_asu']


def outline(cuts):
    vertices = set()
    for a, b, c in combinations(cuts, 3):
        determinant = dot(a.n, cross(b.n, c.n))
        if determinant == 0:
            continue
        terms = [cross(b.n, c.n), cross(c.n, a.n), cross(a.n, b.n)]
        point = tuple(-sum(cut.c * term[i] for cut, term in zip([a,b,c], terms)) / determinant for i in range(3))
        if all(dot(cut.n, point) + cut.c >= 0 for cut in cuts):
            vertices.add(point)
    vertices = sorted(vertices)
    edges = set()
    for a, b in combinations(cuts, 2):
        direction = cross(a.n, b.n)
        if direction == (0,0,0):
            continue
        on_edge = [i for i, v in enumerate(vertices) if dot(a.n, v)+a.c == 0 and dot(b.n, v)+b.c == 0]
        if len(on_edge) >= 2:
            ordered = sorted(on_edge, key=lambda i: dot(vertices[i], direction))
            edges.add(tuple(sorted((ordered[0], ordered[-1]))))
    used = sorted({i for edge in edges for i in edge})
    indices = {old: new for new, old in enumerate(used)}
    return [vertices[i] for i in used], sorted((indices[a], indices[b]) for a,b in edges)


def apply(op, point):
    return tuple(sum(F(op.rot[i][j], op.DEN) * point[j] for j in range(3)) + F(op.tran[i], op.DEN) for i in range(3))


def literal(value):
    if value.denominator == 1:
        return f"{value.numerator}f"
    return f"{value.numerator}f/{value.denominator}f"


def main():
    assert gemmi.__version__ == '0.7.3' and spglib.__version__ == '2.7.0'
    table = load_table()
    groups = {}
    for hall in range(1, 531):
        info = spglib.get_spacegroup_type(hall)
        groups.setdefault(info.number, info)
    lines = [f"// Generated by Tools/generate_space_group_domains.py from CCTBX {REVISION}.",
             "// BSD-style license; see Packages/UnitySymmetry/Third Party Notices.md.",
             "// Closed asymmetric-unit outlines in the first spglib Hall setting.",
             "// Group 4 uses the simpler Y-half box illustrated in the documentation.",
             "using UnityEngine;", "", "internal static class SpaceGroupDomainData", "{",
             "    internal static readonly Vector3[][] Vertices =", "    {"]
    all_edges = []
    shifted = 0
    for number in range(1, 231):
        domain = table(number)
        source = gemmi.find_spacegroup_by_ops(gemmi.symops_from_hall(domain.hall))
        target_ops = gemmi.symops_from_hall(groups[number].hall_symbol)
        target = gemmi.find_spacegroup_by_ops(target_ops)
        # basisop maps reference coordinates into the setting's coordinates.
        cb = target.basisop.combine(source.basisop.inverse())
        source_ops = gemmi.symops_from_hall(domain.hall)
        source_ops.change_basis_forward(cb)
        assert source_ops == target_ops, f"Setting mismatch for group {number}"
        shifted += cb.triplet() != 'x,y,z'
        vertices, edges = outline(domain.cuts)
        vertices = [apply(cb, v) for v in vertices]
        if number == 4:
            # P21: (-x, y+1/2, -z). Center X/Z so the screw operation maps
            # the lower Y-half box directly into the upper half of one cell.
            cuts = [Cut((1,0,0),F(1,2)), Cut((-1,0,0),F(1,2)),
                    Cut((0,1,0),0), Cut((0,-1,0),F(1,2)),
                    Cut((0,0,1),F(1,2)), Cut((0,0,-1),F(1,2))]
            vertices, edges = outline(cuts)
        assert len(vertices) >= 4 and len(edges) >= 6, f"Empty region for group {number}"
        all_edges.append(edges)
        lines += [f"        // {number}: {groups[number].international_short}, Hall {groups[number].hall_number}",
                  "        new Vector3[] {",
                  *[f"            new Vector3({', '.join(map(literal, v))})," for v in vertices],
                  "        },"]
    lines += ["    };", "", "    internal static readonly Vector2Int[][] Edges =", "    {"]
    for number, edges in enumerate(all_edges, 1):
        lines += [f"        // {number}", "        new Vector2Int[] {",
                  *[f"            new Vector2Int({a}, {b})," for a,b in edges], "        },"]
    lines += ["    };", "}", ""]
    OUTPUT.write_text('\n'.join(lines), encoding='utf-8', newline='\n')
    print(f"Generated 230 domain outlines; mapped {shifted} alternative origins/settings.")
    print(f"Edges per source region: {min(map(len, all_edges))} to {max(map(len, all_edges))}.")


if __name__ == '__main__':
    main()
