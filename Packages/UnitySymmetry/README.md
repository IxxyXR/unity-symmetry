# Unity Symmetry

The runtime generators expose a `List<Matrix4x4> matrices`. Apply each matrix to the same source mesh or object transform. Slot zero is identity for point, helical and space-group symmetry.

## Helical symmetry

```csharp
var symmetry = new HelicalSymmetry(copies: 12, angleDegrees: 30f, advance: 0.3f);
```

Each successive copy rotates around local Y and advances along local Y. `copies` includes the original and must be positive. Angle and advance can be negative or zero.

The example project contains `Assets/Scenes/Helical Test.unity`. Change copies, angle and advance on its `HelicalTest` component, as with the existing Point Group Test and Wallpaper Test scenes.

## Space-group symmetry

```csharp
var symmetry = new SpaceGroupSymmetry(
    number: 19, repeats: new Vector3Int(2, 2, 2), cellSize: 2f);
```

`number` is the international space-group number, from 1 to 230. The generator includes each group's rotations, reflections, centering translations, screw axes and glide planes. Each group uses its first Hall setting in spglib; `name`, `hallNumber` and `setting` identify that selection. Alternative settings and custom cell metrics are not exposed.

`cellSize` is a positive, uniform edge length. Trigonal and hexagonal groups (143–194) use a conventional hexagonal cell with 120 degrees between a and b; rhombohedral groups use their hexagonal setting. Other groups use orthogonal, equal-length cell axes. The conventional c axis is local Z. `cellBasis` converts fractional cell coordinates to Cartesian coordinates.

`repeats` gives positive cell counts along a, b and c. The grid starts at cell (0, 0, 0), and each cell contains the complete set of group operations. Matrix order is cell first (x, then y, then z), operation second. Matrices are affine transforms of the source; they do not wrap vertices into cell boundaries or merge coincident copies at special positions.

The example project contains `Assets/Scenes/Space Group Test.unity`. Its `SpaceGroupTest` inspector offers seven crystal systems and 2–6 named presets per system, alongside a live model preview. An advanced number field selects any of the 230 groups. Repeats and cell size remain adjustable; group name, setting and copy count are displayed. It starts with group 19 (`P2_12_12_1`), whose four operations across eight cells produce 32 copies.

## Space-group selection

`SpaceGroupCatalog.GetCrystalSystem(number)` classifies any group into the seven conventional crystal systems. `GetPresets(system)` returns a small collection of group numbers and descriptive labels; `GetName(number)` returns the crystallographic symbol. The catalog is available to runtime UI consumers without any Editor dependency.

The preset selection covers rotations, mirrors, inversion, screw axes, glide planes and centered lattices. It does not restrict the generator: all 230 group numbers remain supported. The demo stores only the selected group number; category and preset are derived from it.

## Space-group data

The static C# catalog is generated from the BSD-licensed spglib 2.7.0 database. The Unity package has no Python or spglib runtime dependency. Attribution is in `Third Party Notices.md`.

To regenerate the catalog from the repository root:

```sh
uv run --python 3.12 --with spglib==2.7.0 Tools/generate_space_groups.py
```
