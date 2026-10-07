using System;
using System.Collections.Generic;

/// <summary>Six crystal systems and visual presets for the 80 layer groups.</summary>
public static class LayerGroupCatalog
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
            new Preset(3, "In-plane half turn"),
            new Preset(4, "Mirror across the layer"),
            new Preset(5, "Glide across the layer"),
            new Preset(8, "Half turn through the layer"),
            new Preset(9, "In-plane screw axis"),
            new Preset(13, "Centered vertical mirrors")
        },
        new[] {
            new Preset(19, "Crossed twofold axes"),
            new Preset(20, "In-plane screw with crossed rotations"),
            new Preset(23, "Crossed vertical mirrors"),
            new Preset(24, "Mirror and glide"),
            new Preset(37, "Mirrors on three axes"),
            new Preset(47, "Centered full mirrors")
        },
        new[] {
            new Preset(49, "Fourfold rotation"),
            new Preset(50, "Fourfold rotoinversion"),
            new Preset(51, "Fourfold with layer reflection"),
            new Preset(52, "Fourfold with through-layer glide"),
            new Preset(55, "Fourfold with vertical mirrors"),
            new Preset(64, "Fourfold with glides and mirrors")
        },
        new[] {
            new Preset(65, "Threefold rotation"),
            new Preset(66, "Threefold with inversion"),
            new Preset(67, "Threefold with in-plane half turns"),
            new Preset(69, "Threefold with vertical mirrors"),
            new Preset(71, "Threefold with full mirrors")
        },
        new[] {
            new Preset(73, "Sixfold rotation"),
            new Preset(74, "Sixfold rotoinversion"),
            new Preset(75, "Sixfold with layer reflection"),
            new Preset(76, "Sixfold with in-plane half turns"),
            new Preset(77, "Sixfold with vertical mirrors"),
            new Preset(80, "Sixfold with full mirrors")
        }
    };

    public static CrystalSystem GetCrystalSystem(int number)
    {
        if (number < 1 || number > 80) throw new ArgumentOutOfRangeException(nameof(number));
        if (number <= 2) return CrystalSystem.Triclinic;
        if (number <= 18) return CrystalSystem.Monoclinic;
        if (number <= 48) return CrystalSystem.Orthorhombic;
        if (number <= 64) return CrystalSystem.Tetragonal;
        if (number <= 72) return CrystalSystem.Trigonal;
        return CrystalSystem.Hexagonal;
    }

    public static IReadOnlyList<Preset> GetPresets(CrystalSystem system) => presets[(int)system];
    public static string GetName(int number) => LayerGroupData.Names[number - 1];
}
