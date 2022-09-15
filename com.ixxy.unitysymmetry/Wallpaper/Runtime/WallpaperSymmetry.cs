using System;
using System.Collections.Generic;
using UnityEngine;


public class WallpaperSymmetry
{
    private int repeatX;
    private int repeatY;
    private double unitScale;
    private Vector2 unitOffset;
    private Vector2 spacing;
    private float finalScale;
    
    public readonly SymmetryGroup groupProperties;
    public readonly List<Matrix4x4> matrices;

    public WallpaperSymmetry(SymmetryGroup.R _group, int _repeatX, int _repeatY, float _finalScale)
    {
        repeatX = _repeatX;
        repeatY = _repeatY;
        var tileSize = Vector2.one;
        unitScale = 1;
        spacing = Vector2.one;
        finalScale = _finalScale;

        Vector4 d = Vector4.zero;

        switch (_group)
        {
            case SymmetryGroup.R.p1:
                // width, skewY, skewX, height
                d = new Vector4(2, 0, 0, 2);
                // unitOffset = new Vector2(-2, 0);
                unitOffset = new Vector2(0, 0);
                break;
            case SymmetryGroup.R.p2:
                // width, skewY, skewX, height
                d = new Vector4(2, 0, 0, 2);
                unitOffset = new Vector2(-2f, -2f);
                repeatY /= 2;
                break;
            case SymmetryGroup.R.pg:
                d = new Vector4(1.5f, 2.4f);
                unitOffset = new Vector2(-1.5f, 0);
                repeatY /= 2;
                break;
            case SymmetryGroup.R.pm:
                d = new Vector4(3, 1.2f, 0, 0);
                unitOffset = new Vector2(-4.5f, 0);
                repeatX /= 2;
                break;
            case SymmetryGroup.R.cm:
                d = new Vector4(1.5f, 1.2f, 0, 0);
                unitOffset = new Vector2(-2.25f, 0);
                break;
            case SymmetryGroup.R.p3:
                d = new Vector4(3, 0, 0, 0);
                unitOffset = new Vector2(-1.5f, 0);
                break;
            case SymmetryGroup.R.p4:
                d = new Vector4(3, 0, 0, 0);
                unitOffset = new Vector2(-3f, 0f);
                break;
            case SymmetryGroup.R.p6:
                d = new Vector4(4, 0, 0, 0);
                unitOffset = new Vector2(-3.5f, -2.232f);
                break;
            case SymmetryGroup.R.pmm:
                d = new Vector4(4, 2, 0, 0);
                unitOffset = new Vector2(-6, -1);
                break;
            case SymmetryGroup.R.p3m1:
                d = new Vector4(5, 0, 0, 0);
                tileSize = new Vector2(1, 0);
                unitOffset = new Vector2(-4.25f, -2.17f);
                break;
            case SymmetryGroup.R.p4m:
                d = new Vector4(4, 0, 0, 0);
                unitOffset = new Vector2(-4, 2f);
                break;
            case SymmetryGroup.R.p6m:
                d = new Vector4(5, 0, 0, 0);
                tileSize = new Vector2(0.5f, 1);
                unitOffset = new Vector2(-4.02f, -2.63f);
                break;
            case SymmetryGroup.R.pmg:
                d = new Vector4(1.5f, 1.2f);
                unitOffset = new Vector2(-3, -1.2f);
                break;
            case SymmetryGroup.R.pgg:
                d = new Vector4(1.5f, 1.2f);
                unitOffset = new Vector2(-3, -.6f);
                break;
            case SymmetryGroup.R.cmm:
                d = new Vector4(1.5f, 1.2f);
                unitOffset = new Vector2(-0.75f, -3f);
                break;
            case SymmetryGroup.R.p31m:
                d = new Vector4(3, 0, 0, 0);
                unitOffset = new Vector2(-3f, 1f);
                break;
            case SymmetryGroup.R.p4g:
                d = new Vector4(1.5f, 0, 0, 0);
                unitOffset = new Vector2(-4.5f, -3);
                break;
        }

        groupProperties = new SymmetryGroup(_group, tileSize, d); // TODO width and height don't do anything
        matrices = new List<Matrix4x4>();
        Initialize();
        
        for (var i = 0; i < matrices.Count; i++)
        {
            var m0 = matrices[i];
            var m = Matrix4x4.Scale(Vector3.one * finalScale) * m0;
            matrices[i] = m;
        }

        
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
        finalScale = _finalScale;
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
