using System;
using System.Collections.Generic;

/// <summary>Crystal-system categories and a small selection of visual presets.</summary>
public static class SpaceGroupCatalog
{
    public enum CrystalSystem
    {
        Triclinic, Monoclinic, Orthorhombic, Tetragonal, Trigonal, Hexagonal, Cubic
    }

    public readonly struct Preset
    {
        public readonly int number;
        public readonly string label;

        public Preset(int number, string label)
        {
            this.number = number;
            this.label = label;
        }
    }

    private static readonly Preset[][] presets =
    {
        new[] {
            new Preset(1, "Translation only"),
            new Preset(2, "Inversion pairs")
        },
        new[] {
            new Preset(3, "Twofold rotation"),
            new Preset(4, "Half-turn screw"),
            new Preset(6, "Mirror planes"),
            new Preset(7, "Glide planes"),
            new Preset(14, "Screw and glide")
        },
        new[] {
            new Preset(16, "Three twofold axes"),
            new Preset(19, "Three screw axes"),
            new Preset(25, "Crossed mirrors"),
            new Preset(47, "Mirrors on all three axes"),
            new Preset(69, "Face-centered mirrors"),
            new Preset(71, "Body-centered mirrors")
        },
        new[] {
            new Preset(75, "Fourfold rotation"),
            new Preset(76, "Quarter-turn screw"),
            new Preset(77, "Half-cell screw"),
            new Preset(99, "Fourfold with vertical mirrors"),
            new Preset(123, "Fourfold with full mirrors")
        },
        new[] {
            new Preset(143, "Threefold rotation"),
            new Preset(144, "One-third-cell screw"),
            new Preset(145, "Two-thirds-cell screw"),
            new Preset(156, "Threefold with mirrors"),
            new Preset(166, "Rhombohedral with mirrors")
        },
        new[] {
            new Preset(168, "Sixfold rotation"),
            new Preset(169, "One-sixth-cell screw"),
            new Preset(170, "Five-sixths-cell screw"),
            new Preset(173, "Half-cell screw"),
            new Preset(183, "Sixfold with vertical mirrors"),
            new Preset(194, "Sixfold screw with mirrors")
        },
        new[] {
            new Preset(195, "Tetrahedral rotations"),
            new Preset(198, "Tetrahedral screws"),
            new Preset(200, "Tetrahedral with inversion"),
            new Preset(207, "Cubic rotations"),
            new Preset(221, "Cubic with full mirrors"),
            new Preset(225, "Face-centered cubic mirrors")
        }
    };

    public static CrystalSystem GetCrystalSystem(int number)
    {
        if (number < 1 || number > 230) throw new ArgumentOutOfRangeException(nameof(number));
        if (number <= 2) return CrystalSystem.Triclinic;
        if (number <= 15) return CrystalSystem.Monoclinic;
        if (number <= 74) return CrystalSystem.Orthorhombic;
        if (number <= 142) return CrystalSystem.Tetragonal;
        if (number <= 167) return CrystalSystem.Trigonal;
        if (number <= 194) return CrystalSystem.Hexagonal;
        return CrystalSystem.Cubic;
    }

    public static IReadOnlyList<Preset> GetPresets(CrystalSystem system) => presets[(int)system];
    public static string GetName(int number) => SpaceGroupData.Names[number - 1];
}
