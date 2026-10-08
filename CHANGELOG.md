# Changelog

## Unreleased

### Added

1. Helical transforms with adjustable copy count, angle and advance along local Y.
2. All 230 space groups, conventional cell bases, seven crystal-system categories and named presets.
3. All 75 rod groups and all 80 layer groups, with catalogs and finite repeat windows.
4. All seven frieze groups and all thirteen general line-group families, including arbitrary axial rotation order and free-angle screw families.
5. Penrose thin/thick rhomb patches, indexed tile geometry, boundary half-rhombs and motif placements. A tile selector creates transforms for Thin, Thick or Both while retaining the full patch geometry.
6. Interactive example scenes and live inspector previews for each added generator.
7. Reproducible space-, rod- and layer-group table generation tools and third-party attribution.

### Changed

1. Point-group transforms use the first placement as their reference frame, with exact identity in slot zero.
2. The example project now uses Unity 2022.3.62f2. The runtime package's declared minimum remains Unity 2019.4.
3. The documentation now covers installation, all supported generators and shared transform conventions.

This section records the additions since the original point/wallpaper implementation; it does not assign a new package version or release date.
