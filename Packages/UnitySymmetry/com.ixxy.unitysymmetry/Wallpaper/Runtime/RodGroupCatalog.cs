using System;
using System.Collections.Generic;

/// <summary>Six crystal systems and visual presets for the 75 rod groups.</summary>
public static class RodGroupCatalog
{
    public enum CrystalSystem
    {
        Triclinic, Monoclinic, Orthorhombic, Tetragonal, Trigonal, Hexagonal
    }

    public readonly struct Preset
    {
        public readonly int number;
        public readonly string label;
        public Preset(int number, string label) { this.number = number; this.label = label; }
    }

    private static readonly Preset[][] presets =
    {
        new[] {
            new Preset(1, "Translation only"),
            new Preset(2, "Inversion pairs")
        },
        new[] {
            new Preset(3, "Transverse twofold rotation"),
            new Preset(8, "Axial twofold rotation"),
            new Preset(9, "Half-turn screw"),
            new Preset(4, "Longitudinal mirrors"),
            new Preset(10, "Transverse mirrors"),
            new Preset(5, "Glide planes")
        },
        new[] {
            new Preset(13, "Crossed twofold rotations"),
            new Preset(14, "Screw with crossed rotations"),
            new Preset(15, "Crossed longitudinal mirrors"),
            new Preset(20, "Mirrors on three axes"),
            new Preset(22, "Mirrors and glide")
        },
        new[] {
            new Preset(23, "Fourfold rotation"),
            new Preset(24, "Quarter-turn screw"),
            new Preset(25, "Half-period screw"),
            new Preset(26, "Three-quarter-period screw"),
            new Preset(34, "Fourfold with longitudinal mirrors"),
            new Preset(41, "Fourfold screw with mirrors")
        },
        new[] {
            new Preset(42, "Threefold rotation"),
            new Preset(43, "One-third-period screw"),
            new Preset(44, "Two-thirds-period screw"),
            new Preset(45, "Threefold with inversion"),
            new Preset(49, "Threefold with mirrors"),
            new Preset(51, "Threefold with full mirrors")
        },
        new[] {
            new Preset(53, "Sixfold rotation"),
            new Preset(54, "One-sixth-period screw"),
            new Preset(58, "Five-sixths-period screw"),
            new Preset(56, "Half-period screw"),
            new Preset(68, "Sixfold with longitudinal mirrors"),
            new Preset(75, "Sixfold mirrors and glide")
        }
    };

    public static CrystalSystem GetCrystalSystem(int number)
    {
        if (number < 1 || number > 75) throw new ArgumentOutOfRangeException(nameof(number));
        if (number <= 2) return CrystalSystem.Triclinic;
        if (number <= 12) return CrystalSystem.Monoclinic;
        if (number <= 22) return CrystalSystem.Orthorhombic;
        if (number <= 41) return CrystalSystem.Tetragonal;
        if (number <= 52) return CrystalSystem.Trigonal;
        return CrystalSystem.Hexagonal;
    }

    public static IReadOnlyList<Preset> GetPresets(CrystalSystem system) => presets[(int)system];
    public static string GetName(int number) => RodGroupData.Names[number - 1];
}
