# Unity Symmetry

Generate local affine transforms for point, wallpaper, helical, space, rod, layer, frieze and general line-group symmetry, or motif placements and indexed geometry for Penrose tilings. The runtime generators expose a `List<Matrix4x4> matrices` and use Unity types in the global namespace.

## Installation and examples

1. In Unity's Package Manager, choose **Add package from git URL** and enter `https://github.com/IxxyXR/unity-symmetry.git#upm`.
2. To use the interactive test scenes, clone the full [repository](https://github.com/IxxyXR/unity-symmetry) and open its Unity 2022.3.62f2 project. The scenes live in `Assets/Scenes`; the standalone UPM package contains the runtime library.

The package declares Unity 2019.4 as its minimum version in `package.json`; the example project's Editor version is separate. The newer generators use static C# data or analytic construction, with no Python dependency at runtime.

## Visualization in the example scenes

Every demo separates a geometric preview from its sample shapes or motifs:

| Demo | Geometric preview | Meaning |
| --- | --- | --- |
| Point | **Domain Gizmos** | White source wedge or cone and blue copies from `PointSymmetry.CreateDomainOutline()` |
| Wallpaper | **Domain Gizmos** | `groupProperties.fundamentalRegion.points` |
| Helical | **Step Gizmos** | Axial step regions clipped to cylinders around local Y |
| Space | **Domain Gizmos** | White source asymmetric unit and blue copies from `SpaceGroupSymmetry.CreateDomainOutline()` |
| Rod | **Cell Gizmos** | Translation periods along Z, clipped to a cylinder |
| Layer | **Cell Gizmos** | In-plane lattice cells from `LayerGroupSymmetry.cellBasis`, including 120-degree hexagonal bases |
| Frieze | **Domain Gizmos** | White source drawing region and blue transformed regions from `CreateDomainOutline()` |
| Line | **Domain Gizmos** | `CreateDomainOutline()` |
| Penrose | **Show Outlines** | `PenroseTiling.vertices` and `tiles` |

All symmetry demos retain independent **Sample Shape Gizmos** toggles; Penrose instead separates **Show Motifs** from **Show Outlines**. Display Radius, Strip Half Width and Domain Radius adjust visual clipping, without changing the transforms. At zero helical advance, step previews are flat rings.

Frames, step regions and translation cells are labelled as such; they are not claimed to be fundamental domains for all group operations. Wallpaper, Line and Penrose use the same domain or tile sources as the corresponding Open Brush previews. Colors, insets, display extents and scene scaling belong to the application.

`PointSymmetry.referenceFrame` maps the original geometric frame to the first-placement reference coordinates used by its matrices. `LayerGroupSymmetry.cellBasis` maps fractional in-plane lattice coordinates to Cartesian coordinates, retaining the original Z coordinate.

## Applying transforms

Apply each operation to the same source pose, rather than accumulating operations between copies:

```csharp
var symmetry = new HelicalSymmetry(copies: 12, angleDegrees: 30f, advance: 0.3f);
Matrix4x4 patternLocalToWorld = Matrix4x4.identity; // Replace with your pattern transform.
Matrix4x4 sourceLocalPose = Matrix4x4.TRS(
    new Vector3(1f, 0f, 0f), Quaternion.identity, Vector3.one);
foreach (Matrix4x4 operation in symmetry.matrices)
{
    Matrix4x4 worldPose = patternLocalToWorld * operation * sourceLocalPose;
    // Use worldPose in your rendering or placement code.
}
```

Here `patternLocalToWorld` is your pattern's world-space transform. Scaling it scales both the pattern spacing and the source geometry. To change spacing without resizing a motif, adjust the generator's distance parameters instead. The generators do not create GameObjects or manage linked content.

Point, helical, space, rod, layer, frieze and general line-group generators have exact identity in slot zero. The simple wallpaper constructor rebases the first operation to identity before applying `_finalScale`, so slot zero is identity when that scale is one. The advanced wallpaper constructor uses its supplied offset and scale without this rebasing. Penrose matrices are tile placements relative to the patch center and generally do not begin with identity.

Rotations and reflections are represented by full matrices. Preserve reflected handedness with negative scale or an appropriate matrix-based rendering path; copying only position and quaternion rotation loses reflections. Repeat counts produce finite samples, not infinite patterns, and copies at special positions are not deduplicated.

## Point-group symmetry

```csharp
var symmetry = new PointSymmetry(
    pointGroupFamily: PointSymmetry.Family.Cnv, _n: 5, _radius: 1f);
```

`Family` includes `Cn`, `Cnv`, `Cnh`, `Sn`, `Dn`, `Dnh`, `Dnd`, `T`, `Th`, `Td`, `O`, `Oh`, `I` and `Ih`. Supply a positive `_n`; it controls axial families. The polyhedral families use their fixed geometry. `_radius` is used in the source placement construction, and the resulting matrices are expressed relative to the first placement so that the original remains unchanged. Axial rotations use local Y.

The example project contains `Assets/Scenes/Point Group Test.unity`. Its `PointGroupTest` component exposes family, order, radius, source transforms and transforms applied to each copy.

### Point-group drawing domains

`PointSymmetry.CreateDomainOutline(displayRadius)` returns Cartesian `vertices` and vertex-index-pair `edges` in the same first-placement reference coordinates as `matrices`. Apply those matrices to the outline to show the source region and its copies. Domains extend indefinitely; `displayRadius` clips the drawing guide without changing any transforms. The symmetry center is at `(0, 0, radius)` in this reference frame. The source region includes the direction from that center to the unchanged main pointer; the display extent can still clip its position.

1. `Cn` uses a full-height angular wedge; `Cnv` halves its angle and `Cnh` uses only its upper half. `Sn` uses narrower full-height wedges, with alternating copies reflected vertically.
2. `Dn` uses an upper-half wedge. `Dnh` and `Dnd` halve its angle; their transformed copies show aligned versus staggered divisions in the lower half. Axial guides are clipped to a cylinder of radius and half-height `displayRadius`.
3. `T`, `O` and `I` use triangular cones from a polyhedron face center and two adjacent vertices. `Td`, `Oh` and `Ih` divide these at the edge midpoint. `Th` uses a four-sided cone from the positive octant where X is the largest coordinate. Polyhedral guides use radial edges and great-circle arcs at `displayRadius`.

The outline's `arcEdges` identifies the subset of `edges` at the curved display cutoff. Consumers can style those arcs independently from the straight domain boundaries.

For a less cluttered preview, call `CreateDomainOutline(displayRadius, drawArcs: false)`. Axial wedges retain their straight boundary-face outlines without the outer cylinder arcs. A single horizontal symmetry boundary is shown as a square; the identity-only group has no boundaries to draw. Polyhedral cones show only their radial rays, with no closing edges at the display cutoff. This changes only the display cutoff, not the symmetry boundaries or operations.

Under the unmodified operations, the unbounded source domain and its copies cover space with disjoint interiors and shared boundaries. **Point Group Test** shows the source in white and copies in blue, separately from **Sample Shape Gizmos**. Extra Transform Each operations can introduce overlap. The former Frame Gizmos checkbox is retained as Domain Gizmos. Outline geometry is rebuilt when settings change.

## Wallpaper symmetry

```csharp
var symmetry = new WallpaperSymmetry(
    _group: SymmetryGroup.R.p4m, _repeatX: 3, _repeatY: 3,
    _finalScale: 1f, _w: 1f, _h: 1f, _sx: 0f, _sy: 0f);
```

`SymmetryGroup.R` selects one of the seventeen wallpaper groups. The pattern lies in local XY. `_repeatX` and `_repeatY` control the finite repeat window; `_w`, `_h`, `_sx` and `_sy` feed group-specific width, height and skew parameters. Not every group uses every parameter. `_finalScale` scales both translations and geometry in the simple constructor.

The advanced overload accepts `_tileSize`, `_unitScale`, `_unitOffset`, `_spacing`, `d` and `_finalScale` after the group and repeat counts. It retains the legacy low-level behavior: `_finalScale` is unused in this overload, and it does not rebase its first placement to identity. `groupProperties` exposes the underlying domain and lattice information; `UnitOffset` and `D` expose the generated simple settings.

The example project contains `Assets/Scenes/Wallpaper Test.unity`. Its `WallPaperTest` component offers simple group settings or the advanced lattice parameters, plus symmetry and domain gizmos.

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

`cellSize` is a positive, uniform edge length. Trigonal and hexagonal groups (143ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“194) use a conventional hexagonal cell with 120 degrees between a and b; rhombohedral groups use their hexagonal setting. Other groups use orthogonal, equal-length cell axes. The conventional c axis is local Z. `cellBasis` converts fractional cell coordinates to Cartesian coordinates.

`repeats` gives positive cell counts along a, b and c. The grid starts at cell (0, 0, 0), and each cell contains the complete set of group operations. Matrix order is cell first (x, then y, then z), operation second. Matrices are affine transforms of the source; they do not wrap vertices into cell boundaries or merge coincident copies at special positions.

The example project contains `Assets/Scenes/Space Group Test.unity`. Its `SpaceGroupTest` inspector offers seven crystal systems and 2ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“6 named presets per system, alongside a live model preview. An advanced number field selects any of the 230 groups. Repeats and cell size remain adjustable; group name, setting and copy count are displayed. For example, group 19 (`P2_12_12_1`) has four operations; a 2 x 2 x 2 cell window produces 32 copies.

## Space-group drawing domains

`SpaceGroupSymmetry.CreateDomainOutline()` returns a fixed asymmetric-unit outline for any of the 230 space groups. The returned outline contains Cartesian `vertices` and vertex-index-pair `edges`. Geometry belongs to the package; the caller chooses rendering and colors. There is no adjustable seed or runtime polyhedron clipping.

```csharp
var symmetry = new SpaceGroupSymmetry(19, new Vector3Int(2, 2, 2), 2f);
var domain = symmetry.CreateDomainOutline();
foreach (var operation in symmetry.matrices)
{
    foreach (var edge in domain.edges)
    {
        Vector3 start = operation.MultiplyPoint3x4(domain.vertices[edge.x]);
        Vector3 end = operation.MultiplyPoint3x4(domain.vertices[edge.y]);
        // Render the edge from start to end.
    }
}
```

The outlines are derived from the [CCTBX asymmetric-unit reference table](https://github.com/cctbx/cctbx_project/blob/a918c3666b2065fc68b927c0f5c903a7f9042395/cctbx/sgtbx/direct_space_asu/reference_table.py), converted to the same first spglib Hall setting used by the transform generator, including alternative origins. Boundary ownership conditions are omitted: the closed outlines share faces, but their interiors are disjoint under the unmodified space-group operations. The complete infinite orbit fills space; a finite repeat window displays only the selected copies and can leave gaps near its edges. Transform matrices are unchanged.

Group 4 (`P2_1`, unique Y axis) uses a simpler equivalent box: fractional X and Z range from `-1/2` to `1/2`, and Y from `0` to `1/2`. Its screw operation `(-x, y + 1/2, -z)` places a matching box directly above it, filling a conventional cell. Other groups use boxes, wedges or small polyhedra, with 6 to 16 edges per source outline. More operations or repeats still increase the total number of displayed edges.

In **Space Group Test**, **Domain Gizmos** shows the source in white and transformed copies in blue, independently from **Sample Shape Gizmos**. Place source geometry within the white region using Transform Before. The outlines follow the same Transform Each operations as the motifs; extra transforms can introduce overlap. The old Cell Gizmos setting is retained under the renamed Domain Gizmos checkbox. Source geometry is scaled by the conventional cell basis when settings change.

## Space-group selection

`SpaceGroupCatalog.GetCrystalSystem(number)` classifies any group into the seven conventional crystal systems. `GetPresets(system)` returns a small collection of group numbers and descriptive labels; `GetName(number)` returns the crystallographic symbol. The catalog is available to runtime UI consumers without any Editor dependency.

The preset selection covers rotations, mirrors, inversion, screw axes, glide planes and centered lattices. It does not restrict the generator: all 230 group numbers remain supported. The demo stores only the selected group number; category and preset are derived from it.

## Rod-group symmetry

```csharp
var symmetry = new RodGroupSymmetry(number: 24, repeats: 6, period: 1.5f);
```

`number` is the international rod-group number, from 1 to 75. The generator uses the conventional general-position operations from PyXtal, including rotations, reflections, inversion, screw axes and glide planes. `name` gives the crystallographic symbol.

`repeats` is a positive count of complete periods, starting at period zero. `period` is the positive repeat distance along local Z; it does not change the radial distance of the source from that axis. Cartesian rotations for trigonal and hexagonal groups are obtained from the conventional 120-degree coordinate basis. Matrix order is period first, operation second, with exact identity in slot zero. Copies are affine transforms of the source, with no vertex wrapping or merging at special positions.

`RodGroupCatalog.GetCrystalSystem(number)` returns one of six systems: triclinic, monoclinic, orthorhombic, tetragonal, trigonal and hexagonal. `GetPresets(system)` returns named presets and their group numbers; `GetName(number)` returns the crystallographic symbol. There are 31 curated presets. All 75 groups remain available through the generator.

The example project contains `Assets/Scenes/Rod Group Test.unity`. Select its Rod Group Symmetry object to choose a category and preset, view the model preview, adjust period and repeats, or enter any group number in the advanced selector. Only the group number is stored; category and preset are derived from it. The default is group 24 (`p41`), with four operations in each of six periods, producing 24 copies.

The static rod table has no Python runtime dependency. To regenerate it from the pinned MIT-licensed PyXtal source:

```sh
python Tools/generate_rod_groups.py
```

Attribution and source revision are in `Third Party Notices.md`.

## Layer-group symmetry

```csharp
var symmetry = new LayerGroupSymmetry(
    number: 52, repeats: new Vector2Int(3, 3), cellSize: 2f);
```

`number` is the international layer-group number, from 1 to 80. The generator uses conventional general-position operations from PyXtal. These include in-plane rotations and translations, reflections across the layer, and in-plane screw or glide operations that exchange the two sides of the layer. `name` gives the crystallographic symbol.

`repeats` gives positive cell counts along the two lattice vectors in local XY. The grid starts at cell (0, 0). `cellSize` is the positive, uniform in-plane edge length; it does not scale the source's Z distance from the layer. Trigonal and hexagonal groups (65ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“80) use a 120-degree a/b basis. Other groups use orthogonal, equal-length in-plane axes. There is no repetition along Z.

Matrix order is cell first (x, then y), operation second, with exact identity in slot zero. Matrices transform the entire source, with no vertex wrapping or merging coincident copies at special positions. A source offset from Z=0 shows the operations that exchange the sides of the layer.

`LayerGroupCatalog.GetCrystalSystem(number)` returns one of six systems: triclinic, monoclinic, orthorhombic, tetragonal, trigonal and hexagonal. `GetPresets(system)` returns 31 curated presets across those systems; `GetName(number)` returns the crystallographic symbol. All 80 group numbers remain supported.

The example project contains `Assets/Scenes/Layer Group Test.unity`. Select its Layer Group Symmetry object to choose a category and preset, view the live preview, adjust repeats and cell size, or select any group number in the advanced field. The demo stores only the group number. Its default is group 52 (`p4/n`), with an off-plane motif to show through-layer glides, and eight operations across nine cells, producing 72 copies.

To regenerate the static table from the pinned MIT-licensed PyXtal source, using only Python's standard library:

```sh
python Tools/generate_layer_groups.py
```

The Unity package has no Python runtime dependency. Attribution and source revision are in `Third Party Notices.md`.

## Frieze symmetry

```csharp
var symmetry = new FriezeSymmetry(
    FriezeSymmetry.Group.p2mg, repeats: 6, period: 1.5f);
```

The seven frieze groups repeat a planar border along local X. All operations act in XY and preserve Z. `repeats` is the positive number of complete periods, starting at period zero; `period` is the positive repeat distance. Slot zero is exact identity. Matrix order is period first, operation second, with no wrapping of vertices or merging of coincident copies.

The enum follows International Tables for Crystallography, Volume E numbering:

| Group | Symbol | Operations per period | Pattern |
|---|---|---|---|
| 1 | p1 | 1 | Translation only |
| 2 | p2 | 2 | Half turns |
| 3 | p1m1 | 2 | Mirrors perpendicular to the strip |
| 4 | p11m | 2 | Mirror parallel to the strip |
| 5 | p11g | 2 | Parallel reflection with a half-period glide |
| 6 | p2mm | 4 | Both mirrors and half turns |
| 7 | p2mg | 4 | Perpendicular mirrors, glide reflections and half turns |

Mirrors perpendicular to the strip lie at X=0 for the first period; parallel mirrors lie at Y=0. For p2mg, the glide advances by half a period and the half-turn centers are offset by a quarter period from the perpendicular mirrors.

The example project contains `Assets/Scenes/Frieze Test.unity`. Its inspector offers all seven groups in one selector, adjustable repeats and period, and a live model preview. It starts with p2mg across six periods, producing 24 copies. An asymmetric motif offset from the strip axis makes the mirror and glide operations visible. The runtime is implemented directly from the seven group definitions, with no database or external dependency.

## Frieze drawing regions

`FriezeSymmetry.CreateDomainOutline(stripHalfWidth)` returns a four-vertex source region in local XY. Apply each `matrices` entry to that polygon to preview the drawing's destinations. `domainXRange` describes its X interval; `domainAboveAxis` identifies groups whose source region occupies the positive side of the axis. The Y extent is a display clip, not a limit on the symmetry.

| Group | Source X interval | Source Y extent |
| --- | --- | --- |
| `p1` | -period/2 to period/2 | Both sides |
| `p2` | -period/2 to period/2 | Above axis |
| `p1m1` | 0 to period/2 | Both sides |
| `p11m` | -period/2 to period/2 | Above axis |
| `p11g` | -period/4 to period/4 | Both sides |
| `p2mm` | 0 to period/2 | Above axis |
| `p2mg` | 0 to period/2 | Above axis |

In **Frieze Test**, **Domain Gizmos** shows the source region in white and its actual transformed copies in blue. **Sample Shape Gizmos** remains independent. The domains use the same Transform Each operations as the motifs, while Transform Before positions the source motif within the drawing region. Unmodified domain interiors do not overlap; crossing a source boundary or using additional Transform Each operations can introduce overlap. The old Cell Gizmos setting is retained under the renamed Domain Gizmos checkbox.

## General line-group symmetry

```csharp
var symmetry = new LineGroupSymmetry(
    LineGroupSymmetry.Family.ScrewHalfTurns,
    n: 5, repeats: 8, advance: 1.1f, angleDegrees: 27f);
```

The generator implements the thirteen line-group families in Damnjanovic and Milosevic, *Line Groups in Physics* (2010), Table 2.2. The local line axis is Z. Unlike crystallographic rod groups, `n` can be any positive axial rotation order, including fivefold and sevenfold. It is the order of the pure axial rotation subgroup, so each step includes n rotations of the motif.

`repeats` is a positive count of generalized steps, beginning at zero. `advance` is the nonnegative distance along Z per step. Zero advance produces rotation/reflection samples at a single axial position; repeated placements are retained. The general screw families (1 and 5) use the supplied twist angle; clockwise and counterclockwise twists are allowed. `UsesFreeAngle(family)` identifies these two families. For other families the angle parameter is ignored and the family determines the compatible operation:

| Family | Operation along Z | Additional motif symmetry | Copies per step |
|---|---|---|---|
| 1 ScrewRotations | Free-angle screw | Axial rotations | n |
| 2 TranslationRotoreflection | Translation | Rotoreflection | 2n |
| 3 TranslationHorizontalMirror | Translation | Transverse mirror | 2n |
| 4 HalfStepHorizontalMirror | 180/n degree screw | Transverse mirror | 2n |
| 5 ScrewHalfTurns | Free-angle screw | Transverse half turns | 2n |
| 6 TranslationVerticalMirrors | Translation | Longitudinal mirrors | 2n |
| 7 GlideRotations | Longitudinal glide | Axial rotations | n |
| 8 HalfStepVerticalMirrors | 180/n degree screw | Longitudinal mirrors | 2n |
| 9 TranslationDiagonalMirrors | Translation | Diagonal mirrors and half turns | 4n |
| 10 GlideRotoreflection | Longitudinal glide | Rotoreflection | 2n |
| 11 TranslationFullMirrors | Translation | Longitudinal and transverse mirrors | 4n |
| 12 GlideHorizontalMirror | Longitudinal glide | Transverse mirror | 2n |
| 13 HalfStepFullMirrors | 180/n degree screw | Full mirrors and half turns | 4n |

Generalized steps are not always full translation periods. A glide alternates between reflecting and not reflecting; two steps give a pure translation by twice `advance`. The fixed half-step screws likewise give a pure translation after two steps, combined with an axial rotation already in the motif symmetry. A freely chosen screw twist need not have a pure translation period, so those families can also represent incommensurate helices.

Matrices are ordered by step, motif-symmetry coset, then axial rotation. Slot zero is exact identity. The finite step window transforms the whole motif without wrapping vertices or merging coincident copies; it is a bounded sample of the infinite group.

The example project contains `Assets/Scenes/Line Group Test.unity`, with one thirteen-family selector, adjustable axial order, step distance and step count, and a live preview. Twist is editable only for the two free-angle families. The default has fivefold screw-and-half-turn symmetry with eight steps, producing 80 copies. The implementation uses analytic generators, with no symmetry database or external runtime dependency.

Reference: https://doi.org/10.1007/978-3-642-11172-3

## Line-group domains

`LineGroupSymmetry` also describes a fundamental domain through `domainAngleDegrees` and `domainZRange`. Its angle starts at local +X towards +Y, and its radial extent is unbounded. Rotations give a sector of 360/n degrees; a Z-preserving mirror halves that angle. Families with a Z-reversing operation use the positive half-step, from Z=0 to `advance/2`; other families use the full step centered on Z=0. Applying the group operations fills angular sectors and successive axial steps with disjoint interiors and shared boundaries.

```csharp
var symmetry = new LineGroupSymmetry(
    LineGroupSymmetry.Family.ScrewHalfTurns,
    n: 5, repeats: 8, advance: 1.1f, angleDegrees: 27f);
var outline = symmetry.CreateDomainOutline(radius: 1.5f);
// outline.vertices and outline.edges describe one domain in local coordinates.
// Apply each symmetry matrix to these vertices to preview its domain copy.
Vector3[] continuousPath = outline.GetWirePath();
```

The display radius clips the domain to a cylinder; it is not a group parameter and does not change copy transforms. The optional `arcSegments` argument (default 48) controls the straight-line approximation per full circle. `edges` are vertex-index pairs. `GetWirePath()` returns a continuous outline that retraces edges without introducing interior diagonals. No renderers or GameObjects are created. In `Line Group Test`, **Domain Gizmos** uses this same API, with an adjustable **Domain Radius**. **Sample Shape Gizmos** independently shows a sample motif; both follow the demo's Transform Each settings.

Screw families rotate the sectors between steps. Their domain remains usable when the screw angle has no pure translation period. At zero advance the outline is flat, and repeated rotations or reflections can overlap; it is a preview rather than a 3D fundamental domain. The non-overlap statement applies to positive advance and the unmodified group operations; arbitrary additional transforms applied separately to each domain can make them overlap.

## Penrose tiling

```csharp
var tiling = new PenroseTiling(subdivisions: 4, radius: 5f,
    tileSelection: PenroseTiling.TileSelection.Both);
```

`TileSelection` offers `Thin`, `Thick`, or `Both` (the default). It filters placement transforms only; `tiles` and `vertices` always describe the complete patch, including boundary half-rhombs. A selected type may have no transforms at low subdivision depths.

The generator subdivides a decagonal seed of ten Robinson triangles using the golden ratio, then pairs triangles across their shared base edges into thin and thick Penrose rhombs. At the finite patch boundary, unmatched triangles are retained as clipped half-rhombs. Shared subdivision points and edges use vertex indices, with no coordinate-tolerance matching.

`subdivisions` is a nonnegative count. Increasing it produces more, smaller tiles inside the same patch. `radius` is the positive circumradius of the decagonal patch in local XY. `edgeLength` gives the complete rhomb edge length at that depth. Tile counts grow exponentially with subdivision depth; the example inspector offers levels 0ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“7.

`vertices` contains the indexed patch coordinates. Each entry of `tiles` gives a thin/thick `kind` and a cyclic array of vertex `indices`. `completeRhomb` is true for four-vertex tiles and false for the three-vertex boundary fragments.

`matrices` contains one motif placement per selected tile, preserving their order in `tiles`. The translation is the average of the polygon vertices, and the rotation aligns local X with its first edge. Placements are relative to the center of the patch, so the first matrix generally includes a translation and rotation. Ordering is deterministic for a given subdivision count. Motifs can share content while using these individual positions and orientations.

The example project contains `Assets/Scenes/Penrose Test.unity`. Its `PenroseTest` component displays unique tile outline edges and repeated motifs, with a tile-type selector for motif transforms, separate visibility toggles and a live inspector preview. Motif size is a fraction of the current tile edge length, so it follows changes in subdivision depth. The demo starts with four subdivisions and a radius of five. Its outline mesh and material are generated in memory and cleaned up with the component.

Construction reference: [Penrose Tiling Explained](https://preshing.com/20110831/penrose-tiling-explained/). Background: [Robinson triangle tilings](https://tilings.math.uni-bielefeld.de/substitution/robinson-triangle/).

## Space-group data

The static C# catalog is generated from the BSD-licensed spglib 2.7.0 database. The Unity package has no Python or spglib runtime dependency. Attribution is in `Third Party Notices.md`.

To regenerate the catalog from the repository root:

```sh
uv run --python 3.12 --with spglib==2.7.0 Tools/generate_space_groups.py
uv run --python 3.12 --with gemmi==0.7.3 --with spglib==2.7.0 Tools/generate_space_group_domains.py
```
