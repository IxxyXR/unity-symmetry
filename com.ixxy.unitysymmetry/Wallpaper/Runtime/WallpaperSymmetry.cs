using System.Collections.Generic;
using UnityEngine;

public class WallpaperSymmetry
{
    private int repeatX;
    private int repeatY;
    private double unitScale;
    private Vector2 unitOffset;
    private Vector2 spacing;
    private Vector4 d;

    public readonly SymmetryGroup groupProperties;
    public readonly List<Matrix4x4> matrices;

    public Vector2 UnitOffset => unitOffset;
    public Vector4 D => d;

    public WallpaperSymmetry(SymmetryGroup.R _group, int _repeatX, int _repeatY, float _finalScale, float _w, float _h, float _sx, float _sy)
    {
        repeatX = _repeatX;
        repeatY = _repeatY;
        var tileSize = Vector2.one;
        unitScale = 1;
        spacing = Vector2.one;

        switch (_group)
        {
            case SymmetryGroup.R.p1:
                // Degrees of freedom: 4
                // width, skewY, skewX, height
                d = new Vector4(_w, _sx, _sy, _h);
                unitOffset = new Vector2(0, 0);
                break;
            case SymmetryGroup.R.p2:
                // Degrees of freedom: 6
                // width, skewY, skewX, height
                d = new Vector4(_w * 2, _sx, _sy, _h * 2);
                unitOffset = new Vector2(-d[0], -d[1]);
                break;
            case SymmetryGroup.R.pg:
                // Degrees of freedom: 4
                // width, height
                d = new Vector4(_w * 2, _h * 2);
                unitOffset = new Vector2(-d[0], 0);
                break;
            case SymmetryGroup.R.pm:
                // Degrees of freedom: 4
                // width, height
                d = new Vector4(_w * 3, _h * 1.2f, 0, 0);
                unitOffset = new Vector2(-d[0], 0);
                break;
            case SymmetryGroup.R.cm:
                // Degrees of freedom: 4
                d = new Vector4(_w * 1.5f, _h * 1.2f, 0, 0);
                unitOffset = new Vector2(-d[0], 0);
                break;
            case SymmetryGroup.R.p3:
                // Degrees of freedom: 4
                d = new Vector4(_w * 4, 0, 0, 0);
                unitOffset = new Vector2(-d[0]*.75f, -Mathf.Sqrt(3)*(d[0]/4));
                break;
            case SymmetryGroup.R.p4:
                // Degrees of freedom: 4
                d = new Vector4(_w * 3, 0, 0, 0);
                unitOffset = new Vector2(-d[0], 0);
                break;
            case SymmetryGroup.R.p6:
                // Degrees of freedom: 4
                d = new Vector4(_w * 4, 0, 0, 0);
                unitOffset = new Vector2(-d[0]*.75f, -Mathf.Sqrt(3)*(d[0]/4));
                break;
            case SymmetryGroup.R.pmm:
                // Degrees of freedom: 5
                d = new Vector4(_w * 2, _h * 2, 0, 0);
                unitOffset = new Vector2(-d[0], 0);
                break;
            case SymmetryGroup.R.p3m1:
                // Degrees of freedom: 4
                d = new Vector4(_w * 5, 0, 0, 0);
                unitOffset = new Vector2(-d[0] * .75f, -d[0] * .43333f);
                break;
            case SymmetryGroup.R.p4m:
                // Degrees of freedom: 4
                d = new Vector4(_w * 4, 0, 0, 0);
                unitOffset = new Vector2(-d[0], 0);
                break;
            case SymmetryGroup.R.p6m:
                // Degrees of freedom: 4
                d = new Vector4(_w * 5, 0, 0, 0);
                unitOffset = new Vector2(-d[0] * .75f, -d[0] * .43333f);
                break;
            case SymmetryGroup.R.pmg:
                // Degrees of freedom: 5
                d = new Vector4(_w * 1.5f, _h * 1.2f);
                unitOffset = new Vector2(-d[0] * 2, -d[1]);
                break;
            case SymmetryGroup.R.pgg:
                // Degrees of freedom: 5
                d = new Vector4(_w * 1.5f, _h * 1.2f);
                unitOffset = new Vector2(-d[0] * 1.5f, -d[1]/2f);
                break;
            case SymmetryGroup.R.cmm:
                // Degrees of freedom: 5
                d = new Vector4(_w * 1.5f, _h * 1.2f);
                unitOffset = new Vector2(-d[0] / 2f, -d[1] * 2);
                break;
            case SymmetryGroup.R.p31m:
                // Degrees of freedom: 4
                d = new Vector4(_w * 3, 0, 0, 0);
                unitOffset = new Vector2(-d[0], 0);
                break;
            case SymmetryGroup.R.p4g:
                // Degrees of freedom: 4
                d = new Vector4(_w * 1.5f, 0, 0, 0);
                unitOffset = new Vector2(-d[0] * 3, -d[0] * 2);
                break;
        }

        groupProperties = new SymmetryGroup(_group, tileSize, d);
        matrices = new List<Matrix4x4>();
        Initialize();
        
        // Offset all transforms so that they first transform is identity
        // Also store a sum of all translations for later averaging
        // var center = matrices[0].MultiplyPoint(Vector3.zero);
        for (var i = 1; i < matrices.Count; i++)
        {
            var m0 = matrices[i];
            var m = matrices[0].inverse * m0;
            matrices[i] = m;
            // var prevCenter = center;
            // center = m.MultiplyPoint(Vector3.zero) + prevCenter;
        }
        matrices[0] = Matrix4x4.identity;

        for (var i = 0; i < matrices.Count; i++)
        {
            var m0 = matrices[i];
            var m = Matrix4x4.Scale(Vector3.one * _finalScale) * m0;
            matrices[i] = m;
        }

        // In this loop we offset all transforms to center them
        // center /= matrices.Count; // Average translation
        // var centeringTransform = Matrix4x4.Translate(-center);
        // for (var i = 0; i < matrices.Count; i++)
        // {
        //     var m = matrices[i];
        //     m = centeringTransform * m;            
        //     matrices[i] = m;
        // }
    }
    
    public WallpaperSymmetry(SymmetryGroup.R _group, int _repeatX, int _repeatY, 
        Vector2 _tileSize, double _unitScale, Vector2 _unitOffset, 
        Vector2 _spacing, Vector4 d, float _finalScale)
    {
        repeatX = _repeatX;
        repeatY = _repeatY;
        unitScale = _unitScale;
        _unitScale = 1f;
        unitOffset = _unitOffset * (float)_unitScale;
        spacing = _spacing * (float)_unitScale;
        groupProperties = new SymmetryGroup(_group, _tileSize * (float)_unitScale, d * (float)_unitScale); // TODO width and height don't do anything
        matrices = new List<Matrix4x4>();

        Initialize();
    }

    private void Initialize()
    {
        var initialMatrix = Matrix4x4.identity;
        var translation = Matrix4x4.Translate(unitOffset);
        var scale = Matrix4x4.Scale(Vector3.one * (float)unitScale);
        createTranslations(initialMatrix * translation * scale);

        var ms = groupProperties.getCosetReps();
        foreach (var m in ms)
        {
            createTranslations(m * translation * scale);
        }
    }
    
    private void createSingleDirectionTranslations(Matrix4x4 m, double dx, double dy)
    {
        var translation = Matrix4x4.Translate(new Vector2((float)dx, (float)dy));
        for (int n=0; n < repeatX; n++)
        {
            matrices.Add(m * translation);
            m = translation * m;
        }
    }

    private void createTranslations(Matrix4x4 m)
    {
        double Ux = groupProperties.getTranslationX()[0] * spacing.x;
        double Vx = groupProperties.getTranslationX()[1] * spacing.x;
        double Uy = groupProperties.getTranslationY()[0] * spacing.y;
        double Vy = groupProperties.getTranslationY()[1] * spacing.y;

        for (int n = 0; n < repeatY; n++)
        {
            createSingleDirectionTranslations(m, Ux, Uy);
            m = Matrix4x4.Translate(new Vector3((float)Vx, (float)Vy, 0)) * m;
        }
    }
}
