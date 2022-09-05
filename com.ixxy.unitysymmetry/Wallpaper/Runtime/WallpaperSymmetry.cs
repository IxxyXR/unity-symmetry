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
                d = new Vector4(2, 0, 0, 2);
                // unitOffset = new Vector2(-2, 0);
                unitOffset = new Vector2(0, 0);
                break;
            case SymmetryGroup.R.p2:
                d = new Vector4(2, 0, 0, 2);
                unitOffset = new Vector2(-2.5f, 0.5f);
                break;
            case SymmetryGroup.R.p3:
                d = new Vector4(3, 0, 0, 0);
                unitOffset = new Vector2(-2.75f, -1.8f);
                break;
            case SymmetryGroup.R.p4:
                d = new Vector4(2, 0, 0, 0);
                unitOffset = new Vector2(-2.5f, -0.5f);
                break;
            case SymmetryGroup.R.p6:
                d = new Vector4(4, 0, 0, 0);
                unitOffset = new Vector2(-3.5f, -2.232f);
                break;
            case SymmetryGroup.R.pm:
                d = new Vector4(2, 2, 0, 0);
                unitOffset = new Vector2(-2, -1);
                break;
            case SymmetryGroup.R.pmm:
                d = new Vector4(2, 2, 0, 0);
                unitOffset = new Vector2(-2, -1);
                break;
            case SymmetryGroup.R.p3m1:
                d = new Vector4(5, 0, 0, 0);
                tileSize = new Vector2(1, 0);
                unitOffset = new Vector2(-4.25f, -2.17f);
                break;
            case SymmetryGroup.R.p4m:
                d = new Vector4(4, 0, 0, 0);
                tileSize = new Vector2(-3.26f, -4);
                spacing = new Vector2(1, 2);
                unitScale = 2;
                unitOffset = new Vector2(-4.73f, 4);
                break;
            case SymmetryGroup.R.p6m:
                d = new Vector4(5, 0, 0, 0);
                tileSize = new Vector2(0.5f, 1);
                unitOffset = new Vector2(-4.02f, -2.63f);
                break;
            case SymmetryGroup.R.cm:
                d = new Vector4(1f, 1f);
                spacing = new Vector2(2, .5f);
                unitOffset = new Vector2(-2, 0);
                break;
            case SymmetryGroup.R.pg:
                d = new Vector4(1.5f, 1.5f);
                unitOffset = new Vector2(-1.5f, 0);
                break;
            case SymmetryGroup.R.pmg:
                d = new Vector4(1.5f, 1.2f);
                unitOffset = new Vector2(0, 0);
                break;
            case SymmetryGroup.R.pgg:
                d = new Vector4(1.5f, 1.2f);
                unitOffset = new Vector2(0, 0);
                break;
            case SymmetryGroup.R.cmm:
                d = new Vector4(1.5f, 1.2f);
                unitOffset = new Vector2(0, 0);
                break;
            case SymmetryGroup.R.p31m:
                d = new Vector4(3, 0, 0, 0);
                unitOffset = new Vector2(-3.46f, -1.41f);
                break;
            case SymmetryGroup.R.p4g:
                d = new Vector4(1.5f, 0, 0, 0);
                unitOffset = new Vector2(0, 0);
                break;
        }

        groupProperties = new SymmetryGroup(_group, tileSize, d); // TODO width and height don't do anything
        matrices = new List<Matrix4x4>();

        switch (_group)
        {
            case SymmetryGroup.R.cm:
                tileSize *= 2;
                for (var v = 0; v < _repeatY; v++)
                {
                    for (var u = 0; u < _repeatX; u++)
                    {
                        var m = Matrix4x4.identity;
                        m = u % 2 == 0 ? m : Matrix4x4.Scale(new Vector3(-1, 1, 1));
                        m = v % 2 == 0 ? m : Matrix4x4.Translate(new Vector3(tileSize.x, 0, 0));
                        matrices.Add(Matrix4x4.Translate(new Vector3(u * tileSize.x , v * tileSize.y, 0)) * m);
                    }
                }
                break;
            default:
                Initialize();
                break;
        }
        
        for (var i = 0; i < matrices.Count; i++)
        {
            var m0 = matrices[i];
            var m = Matrix4x4.Scale(Vector3.one * finalScale) * m0;
            matrices[i] = m;
        }

        for (var i = 1; i < matrices.Count; i++)
        {
            var m0 = matrices[i];
            var m = matrices[0].inverse * m0;
            matrices[i] = m;
        }
        matrices[0] = Matrix4x4.identity;
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
