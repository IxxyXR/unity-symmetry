// Based on https://github.com/hwatheod/wallpaper

using System;
using System.Linq;
using UnityEngine;

public class Polygon
{
    // Polygon coordinates.
    public Vector2[] points;
    public Vector2 center;

    /**
     * @param points {{0,0}, {10,0}, {10,10}, {0,10}} Coordinates of points in order (clockwise or counterclockwise).
     * @param offsetX X offset to be applied to each point.
     * @param offsetY Y offset to be applied to each point.
     */
    public Polygon(Vector2[] _points, double offsetX, double offsetY)
    {
        if (_points.Length < 1)
        {
            Debug.LogError("Empty polygon");
            return;
        }

        points = _points.Select(v => new Vector2((float)(v.x + offsetX), (float)(v.y + offsetY))).ToArray();
    }

    public static Polygon Rectangle(double dx, double dy, double offsetX, double offsetY)
    {
        return new Polygon(new[]
        {
            new Vector2(0, 0),
            new Vector2((float)dx, 0),
            new Vector2((float)dx, (float)dy),
            new Vector2(0, (float)dy)
        }, offsetX, offsetY);
    }

    public static Polygon Parallelogram(double d1x, double d2x, double d1y, double d2y, double offsetX, double offsetY)
    {
        return new Polygon(new[]
        {
            new Vector2(0f, 0f),
            new Vector2((float)d1x, (float)d1y),
            new Vector2((float)(d1x + d2x), (float)(d1y + d2y)),
            new Vector2((float)d2x, (float)d2y)
        }, offsetX, offsetY);
    }
    
    public static Polygon Parallelogram2(double d1x, double d2x, double d1y, double d2y, double offsetX, double offsetY)
    {
        return new Polygon(new[]
        {
            new Vector2(0, 0),
            new Vector2((float)d2x / 2, (float)d2y / 2),
            new Vector2((float)(d1x + d2x) / 2, (float)(d1y + d2y) / 2),
            new Vector2((float)d1x / 2, (float)d1y / 2)
        }, offsetX, offsetY);
    }

    public static Polygon EquilaterialTriangle(double hexSize, double offsetX, double offsetY)
    {
        return new Polygon(new[]
        {
            new Vector2(0, 0),
            new Vector2((float)hexSize / 2 * 1 / 2, (float)(hexSize / 2 * (Math.Sqrt(3) / 2))),
            new Vector2((float)hexSize / 2, 0),
            new Vector2((float)hexSize / 2 * 1 / 2, (float)(-hexSize / 2 * Math.Sqrt(3) / 2))
        }, offsetX, offsetY);
    }

    public static Polygon Hex2(double hexSize, double offsetX, double offsetY)
    {
        return new Polygon(new[]
        {
            new Vector2(0, 0),
            new Vector2(0, (float)(hexSize / 2 * Math.Sqrt(3) / 2)),
            new Vector2((float)hexSize / 4, (float)(hexSize / 2 * Math.Sqrt(3) / 2)),
            new Vector2((float)(3 * hexSize / 8), (float)(hexSize * Math.Sqrt(3) / 8))
        }, offsetX, offsetY);
    }
    
    public static Polygon Tri(double hexSize, double offsetX, double offsetY)
    {
        return new Polygon(new[]
        {
            new Vector2(0, 0),
            new Vector2((float)hexSize / 2 * 1 / 2, (float)(hexSize / 2 * (Math.Sqrt(3) / 2))),
            new Vector2((float)hexSize / 2, 0)
        }, offsetX, offsetY);
    }
    
    public static Polygon Tri2(double d1x, double d2x, double d1y, double d2y, double offsetX, double offsetY)
    {
        return new Polygon(new[]
        {
            new Vector2(0, 0),
            new Vector2((float)d2x / 2, (float)d2y / 2),
            new Vector2((float)(d1x + d2x) / 2, (float)(d1y + d2y) / 2)
        }, offsetX, offsetY);
    }
    
    public static Polygon Tri3(double hexSize, double offsetX, double offsetY)
    {
        return new Polygon(new[]
        {
            new Vector2(0, 0),
            new Vector2(0, (float)(hexSize / 2 * Math.Sqrt(3) / 2)),
            new Vector2((float)(hexSize / 4), (float)(hexSize / 2 * Math.Sqrt(3) / 2))
        }, offsetX, offsetY);
    }
    
    public static Polygon Tri4(double baseSize, double offsetX, double offsetY)
    {
        return new Polygon(new[]
        {
            new Vector2(0, 0),
            new Vector2((float)baseSize, 0),
            new Vector2((float)(baseSize / 2), (float)((baseSize / 2) * (Math.Sqrt(3) / 3)))
        }, offsetX, offsetY);
    }
}

public class SymmetryGroup
{
    public Polygon fundamentalRegion; // fundamental region for the symmetry group
    public Vector2 center; // center of fundamental tile for the translation subgroup
    private Vector2 translationX;
    private Vector2 translationY; // the 2 translation vectors for the translation subgroup

    private Matrix4x4[] cosetReps; // coset representatives of the translation subgroup in the symmetry group,

    // which can be applied to the fundamental region to get the fundamental tile
    // Identity matrix is NOT included.
    private R id;

    private Matrix4x4 setReflectionMatrix(Vector2 p1, Vector2 p2)
    {
        
        // if (p1.x == p2.x)
        // {
        //     var trX = Matrix4x4.Translate(Vector3.right * p1.x);
        //     return trX.inverse * Matrix4x4.Scale(new Vector3(-1, 1, 1)) * trX;
        // }
        //
        // if (p1.y == p2.y)
        // {
        //     var trY = Matrix4x4.Translate(Vector3.up * p1.y);
        //     return trY.inverse * Matrix4x4.Scale(new Vector3(1, -1, 1)) * trY;
        // }

        // var m = new Matrix4x4();
        //
        // float dx = p2[0] - p1[0];
        // float dy = p2[1] - p1[1];
        // /* orthogonal direction is (dy, -dx) */
        // var unitNormal = new Vector2(
        //     dy / (float)Math.Sqrt(dy*dy + dx*dx),
        //     -dx / (float)Math.Sqrt(dy*dy + dx*dx)
        // );
        // /* The reflection matrix fixes the 2 points while taking the unit normal to its negative. */
        // m.setPolyToPoly(
        //     new [] {p1[0], p1[1], p2[0], p2[1], p1[0] + unitNormal[0], p1[1] + unitNormal[1]}, 0,
        //     new [] {p1[0], p1[1], p2[0], p2[1], p1[0] - unitNormal[0], p1[1] - unitNormal[1]}, 0, 3
        // );
        
        // var tr = Matrix4x4.Translate(-p1);
        // var rot = Matrix4x4.Rotate(Quaternion.Euler(0, 0, -Vector3.Angle(Vector3.up, p2 - p1)));
        // var scale = Matrix4x4.Scale(new Vector3(-1, 1, 1));
        // var mat = tr * rot * scale * rot.inverse * tr.inverse;
        // return mat;
        
        var plane = new Plane();
        plane.Set3Points(
            new Vector3(p1.x, p1.y, 0),
            new Vector3(p2.x, p2.y, 0),
            new Vector3(p1.x, p1.y, 1.1f)
        );
        var normals = plane.normal;

        var reflectionMat = new Matrix4x4();
        reflectionMat.m00 = (1F - 2F * normals.x * normals.x);
        reflectionMat.m01 = (-2F * normals.x * normals.y);
        reflectionMat.m02 = (-2F * normals.x * normals.z);
        reflectionMat.m03 = (-2F * plane.distance * normals.x);

        reflectionMat.m10 = (-2F * normals.y * normals.x);
        reflectionMat.m11 = (1F - 2F * normals.y * normals.y);
        reflectionMat.m12 = (-2F * normals.y * normals.z);
        reflectionMat.m13 = (-2F * plane.distance * normals.y);

        reflectionMat.m20 = (-2F * normals.z * normals.x);
        reflectionMat.m21 = (-2F * normals.z * normals.y);
        reflectionMat.m22 = (1F - 2F * normals.z * normals.z);
        reflectionMat.m23 = (-2F * plane.distance * normals.z);

        reflectionMat.m30 = 0F;
        reflectionMat.m31 = 0F;
        reflectionMat.m32 = 0F;
        reflectionMat.m33 = 1F;

        return reflectionMat;
    }

    [Serializable]
    public enum R
    {
        p1,
        pg,
        cm,
        pm,
        p6,
        p6m,
        p3,
        p3m1,
        p31m,
        p4,
        p4m,
        p4g,
        p2,
        pgg,
        pmg,
        pmm,
        cmm,
    }

    public Matrix4x4 setRotate(double rotation, Vector2 pivot)
    {
        var tr = Matrix4x4.Translate(pivot);
        return tr * Matrix4x4.Rotate(Quaternion.Euler(0, 0, (float)rotation)) * tr.inverse;
    }

    public SymmetryGroup(R symmetryGroupId, Vector2 tileSize, Vector4 d)
    {
        id = symmetryGroupId;

        double d1x, d1y, d2x, d2y, offsetX, offsetY;

        switch (symmetryGroupId)
        {
            case R.p1:
                d1x = d.x; // 2
                d1y = d.y; // 0
                d2x = d.z; // .8
                d2y = d.w; // 2
                offsetX = tileSize.x / 2 - (d1x + d2x) / 2;
                offsetY = tileSize.y / 2 - (d1y + d2y) / 2;
                fundamentalRegion = Polygon.Parallelogram(d1x, d2x, d1y, d2y, offsetX, offsetY);
                center = new Vector2(
                    (float)((d1x + d2x) / 2 + offsetX),
                    (float)((d1y + d2y) / 2 + offsetY)
                );
                translationX = new Vector2((float)d1x, (float)d2x);
                translationY = new Vector2((float)d1y, (float)d2y);
                cosetReps = new Matrix4x4[] { };
                break;

            case R.p2:
                d1x = d.x; // 2
                d1y = d.y; // 0
                d2x = d.z; // .8
                d2y = d.w; // 2
                offsetX = tileSize.x / 2 - (d1x + d2x) / 2;
                offsetY = tileSize.y / 2 - (d1y + d2y) / 2;
                fundamentalRegion = Polygon.Parallelogram(d1x, d2x, d1y, d2y, offsetX, offsetY);
                center = new Vector2(
                    (float)(d1x / 2 + offsetX),
                    (float)(d1y / 2 + offsetY)
                );
                translationX = new Vector2((float)d1x, (float)d2x * 2);
                translationY = new Vector2((float)d1y, (float)d2y * 2);
                cosetReps = new Matrix4x4[1];
                cosetReps[0] = setRotate(180, center);
                break;

            case R.p3:
                double hexSize = d.x; // 300
                d1x = 3 * hexSize / 4;
                d1y = hexSize * (Math.Sqrt(3) / 4);
                d2x = d1x;
                d2y = -d1y;
                offsetX = tileSize.x / 2;
                offsetY = tileSize.y / 2;
                fundamentalRegion = Polygon.EquilaterialTriangle(hexSize, offsetX, offsetY);
                center = new Vector2(tileSize.x / 2, tileSize.y / 2);
                translationX = new Vector2((float)d1x, (float)d2x);
                translationY = new Vector2((float)d1y, (float)d2y);
                cosetReps = new Matrix4x4[2];
                cosetReps[0] = setRotate(120, center);
                cosetReps[1] = setRotate(240, center);
                break;

            case R.p4:
                double squareSize = d.x; // 300
                d1x = squareSize;
                d1y = 0;
                d2x = 0;
                d2y = squareSize;
                offsetX = tileSize.x / 2;
                offsetY = tileSize.y / 2;
                fundamentalRegion = Polygon.Parallelogram2(d1x, d2x, d1y, d2y, offsetX, offsetY);
                center = new Vector2(
                    tileSize.x / 2,
                    tileSize.y / 2
                );
                translationX = new Vector2((float)d1x, (float)d2x);
                translationY = new Vector2((float)d1y, (float)d2y);
                cosetReps = new Matrix4x4[3];
                cosetReps[0] = setRotate(90, center);
                cosetReps[1] = setRotate(180, center);
                cosetReps[2] = setRotate(270, center);
                break;

            case R.p6:
                hexSize = d.x; // 400
                d1x = 3 * hexSize / 4;
                d1y = hexSize * (Math.Sqrt(3) / 4);
                d2x = d1x;
                d2y = -d1y;
                offsetX = tileSize.x / 2;
                offsetY = tileSize.y / 2;
                fundamentalRegion = Polygon.Hex2(hexSize, offsetX, offsetY);
                center = new Vector2(
                    tileSize.x / 2,
                    tileSize.y / 2
                );
                translationX = new Vector2((float)d1x, (float)d2x);
                translationY = new Vector2((float)d1y, (float)d2y);
                cosetReps = new Matrix4x4[5];
                for (int i = 0; i < 5; i++)
                {
                    cosetReps[i] = setRotate(60 * (i + 1), center);
                }

                break;

            case R.pmm:
                double dx = d.x; // 400
                double dy = d.y; // 200
                offsetX = tileSize.x / 2 - dx / 4;
                offsetY = tileSize.y / 2 - dy / 4;
                fundamentalRegion = Polygon.Rectangle(dx/2, dy/2, offsetX, offsetY);
                center = new Vector2(
                    tileSize.x / 2,
                    tileSize.y / 2
                );
                translationX = new Vector2((float)dx, 0);
                translationY = new Vector2(0, (float)dy);
                cosetReps = new Matrix4x4[3];
                cosetReps[0] = setReflectionMatrix(fundamentalRegion.points[0], fundamentalRegion.points[1]);
                cosetReps[1] = setReflectionMatrix(fundamentalRegion.points[0], fundamentalRegion.points[3]);
                cosetReps[2] = setRotate(180, new Vector2((float)offsetX, (float)offsetY));
                break;

            case R.p3m1:
                hexSize = d.x; // 500
                d1x = 3 * hexSize / 4;
                d1y = hexSize * (Math.Sqrt(3) / 4);
                d2x = d1x;
                d2y = -d1y;
                offsetX = tileSize.x / 2;
                offsetY = tileSize.y / 2;
                fundamentalRegion = Polygon.Tri(hexSize, offsetX, offsetY);
                center = new Vector2(
                    tileSize.x / 2,
                    tileSize.y / 2
                );
                translationX = new Vector2((float)d1x, (float)d2x);
                translationY = new Vector2((float)d1y, (float)d2y);
                cosetReps = new Matrix4x4[5];
                cosetReps[0] = setRotate(120, center);
                cosetReps[1] = setRotate(240, center);

                cosetReps[2] = setReflectionMatrix(fundamentalRegion.points[2], fundamentalRegion.points[0]);
                cosetReps[3] = cosetReps[2];
                cosetReps[3] = cosetReps[0] * cosetReps[3];
                cosetReps[4] = cosetReps[2];
                cosetReps[4] = cosetReps[1] * cosetReps[4];
                break;

            case R.p4m:
                squareSize = d.x; // 400
                d1x = squareSize;
                d1y = 0;
                d2x = 0;
                d2y = squareSize;
                offsetX = tileSize.x / 2;
                offsetY = tileSize.y / 2;
                fundamentalRegion = Polygon.Tri2(d1x, d2x, d1y, d2y, offsetX, offsetY);
                center = new Vector2(
                    tileSize.x / 2,
                    tileSize.y / 2
                );
                translationX = new Vector2((float)d1x, (float)d2x);
                translationY = new Vector2((float)d1y, (float)d2y);
                cosetReps = new Matrix4x4[7];
                cosetReps[0] = setRotate(90, center);
                cosetReps[1] = setRotate(180, center);
                cosetReps[2] = setRotate(270, center);

                cosetReps[3] = setReflectionMatrix(fundamentalRegion.points[1], fundamentalRegion.points[2]);
                for (int i = 4; i < 7; i++)
                {
                    cosetReps[i] = cosetReps[3];
                    cosetReps[i] = cosetReps[i - 4] * cosetReps[i];
                }
                break;

            case R.p6m:
                hexSize = d.x; // 500
                d1x = 3 * hexSize / 4;
                d1y = hexSize * (Math.Sqrt(3) / 4);
                d2x = d1x;
                d2y = -d1y;
                offsetX = tileSize.x / 2;
                offsetY = tileSize.y / 2;
                fundamentalRegion = Polygon.Tri3(hexSize, offsetX, offsetY);
                center = new Vector2(
                    tileSize.x / 2,
                    tileSize.y / 2
                );
                translationX = new Vector2((float)d1x, (float)d2x);
                translationY = new Vector2((float)d1y, (float)d2y);
                cosetReps = new Matrix4x4[11];
                for (int i = 0; i < 5; i++)
                {
                    cosetReps[i] = setRotate(60 * (i + 1), center);
                }

                cosetReps[5] = setReflectionMatrix(fundamentalRegion.points[0], fundamentalRegion.points[2]);
                for (int i = 6; i < 11; i++)
                {
                    cosetReps[i] = cosetReps[5];
                    cosetReps[i] = cosetReps[i - 6] * cosetReps[i];
                }

                break;

            case R.pm:
                dx = d.x; // 3
                dy = d.y; // 1.2
                offsetX = tileSize.x / 2 - dx / 4;
                offsetY = tileSize.y / 2 - dy / 2;
                fundamentalRegion = Polygon.Rectangle(dx / 2, dy, offsetX, offsetY);
                center = new Vector2(
                    tileSize.x / 2,
                    tileSize.y / 2
                );
                translationX = new Vector2((float)dx, 0);
                translationY = new Vector2(0, (float)dy);
                cosetReps = new Matrix4x4[1];

                cosetReps[0] = setReflectionMatrix(fundamentalRegion.points[0], fundamentalRegion.points[3]);
                break;

            case R.cm:
                dx = d.x; // 1.5
                dy = d.y; // 1.2
                offsetX = tileSize.x / 2 - dx / 2;
                offsetY = tileSize.y / 2 - dy / 2;
                fundamentalRegion = Polygon.Rectangle(dx, dy, offsetX, offsetY);
                center = new Vector2(
                    tileSize.x / 2,
                    tileSize.y / 2
                );
                translationX = new Vector2((float)dx, (float)dx);
                translationY = new Vector2((float)dy, (float)-dy);
                cosetReps = new Matrix4x4[1];

                cosetReps[0] = setReflectionMatrix(fundamentalRegion.points[0], fundamentalRegion.points[3]);
                break;

            case R.pg:
                dx = d.x; // 1.5
                dy = d.y; // 1.2
                offsetX = tileSize.x / 2 - dx / 2;
                offsetY = tileSize.y / 2 - dy / 2;
                fundamentalRegion = Polygon.Rectangle(dx, dy, offsetX, offsetY);
                center = new Vector2(
                    tileSize.x / 2,
                    tileSize.y / 2
                );
                translationX = new Vector2((float)dx, 0);
                translationY = new Vector2(0, (float)(2 * dy));
                cosetReps = new Matrix4x4[1];

                cosetReps[0] = setReflectionMatrix(new Vector2((float)(dx / 2 + offsetX), (float)(0 + offsetY)),
                    new Vector2((float)(dx / 2 + offsetX), (float)(dy + offsetY)));
                cosetReps[0] = Matrix4x4.Translate(new Vector3(0, (float)dy, 0)) * cosetReps[0];
                break;

            case R.pmg:
                dx = d.x; // 1.5
                dy = d.y; // 1.2
                offsetX = tileSize.x / 2 - dx / 2;
                offsetY = tileSize.y / 2 - dy / 2;
                fundamentalRegion = Polygon.Rectangle(dx, dy, offsetX, offsetY);
                center = new Vector2(
                    tileSize.x / 2,
                    tileSize.y / 2
                );
                translationX = new Vector2((float)(2 * dx), 0);
                translationY = new Vector2(0, (float)(2 * dy));
                cosetReps = new Matrix4x4[3];
                cosetReps[0] = setReflectionMatrix(fundamentalRegion.points[1], fundamentalRegion.points[2]);
                cosetReps[1] = setRotate(180, new Vector2((float)(dx / 2 + offsetX), (float)(0 + offsetY)));
                cosetReps[2] = cosetReps[1];
                cosetReps[2] = cosetReps[0] * cosetReps[2];
                break;

            case R.pgg:

                dx = d.x; // 150
                dy = d.y; // 120
                offsetX = tileSize.x / 2 - dx / 2;
                offsetY = tileSize.y / 2 - dy / 2;
                fundamentalRegion = Polygon.Rectangle(dx, dy, offsetX, offsetY);
                center = new Vector2(
                    tileSize.x / 2,
                    tileSize.y / 2
                );
                translationX = new Vector2((float)(2 * dx), 0);
                translationY = new Vector2(0, (float)(2 * dy));
                
                cosetReps = new Matrix4x4[3];
                cosetReps[0] = setReflectionMatrix(fundamentalRegion.points[1], fundamentalRegion.points[2]);
                cosetReps[0] =  Matrix4x4.Translate(new Vector3(0, (float)dy, 0)) * cosetReps[0];
                cosetReps[1] = setRotate(180, new Vector2((float)(dx / 2 + offsetX), (float)(0 + offsetY)));
                cosetReps[2] = cosetReps[1];
                cosetReps[2] = cosetReps[0] * cosetReps[2];
                break;

            case R.cmm:
                dx = d.x; // 150
                dy = d.y; // 120
                offsetX = tileSize.x / 2 - dx / 2;
                offsetY = tileSize.y / 2 - dy / 2;
                fundamentalRegion = Polygon.Rectangle(dx, dy, offsetX, offsetY);
                center = new Vector2(
                    tileSize.x / 2,
                    tileSize.y / 2
                );
                translationX = new Vector2((float)dx, (float)dx);
                translationY = new Vector2((float)(2 * dy), (float)(-2 * dy));
                cosetReps = new Matrix4x4[3];

                cosetReps[0] = setReflectionMatrix(fundamentalRegion.points[1], fundamentalRegion.points[2]);
                cosetReps[1] = setRotate(180, new Vector2((float)(dx / 2 + offsetX), (float)(0 + offsetY)));
                cosetReps[2] = cosetReps[0];
                cosetReps[2] = cosetReps[1] * cosetReps[2];
                break;

            case R.p31m:
                double baseSize = d.x; // 300
                offsetX = tileSize.x / 2 - baseSize / 2;
                offsetY = tileSize.y / 2;
                fundamentalRegion = Polygon.Tri4(baseSize, offsetX, offsetY);
                center = new Vector2(
                    (float)(3 * baseSize / 4 + offsetX),
                    (float)(baseSize * Math.Sqrt(3) / 4 + offsetY)
                );
                translationX = new Vector2((float)baseSize, (float)(baseSize / 2));
                translationY = new Vector2(0, (float)(baseSize * Math.Sqrt(3) / 2));
                cosetReps = new Matrix4x4[5];
                cosetReps[0] = setRotate(120, new Vector2(fundamentalRegion.points[2][0], fundamentalRegion.points[2][1]));
                cosetReps[1] = setRotate(240, new Vector2(fundamentalRegion.points[2][0], fundamentalRegion.points[2][1]));
                cosetReps[2] = setReflectionMatrix(fundamentalRegion.points[1], center);
                cosetReps[3] = cosetReps[0];
                cosetReps[3] = cosetReps[2] * cosetReps[3];
                cosetReps[4] = cosetReps[1];
                cosetReps[4] = cosetReps[2] * cosetReps[4];
                break;

            case R.p4g:
                squareSize = d.x; // 150
                offsetX = tileSize.x / 2 - squareSize / 2;
                offsetY = tileSize.y / 2 - squareSize / 2;
                fundamentalRegion = Polygon.Rectangle(squareSize, squareSize, offsetX, offsetY);
                center = new Vector2(
                    (float)(0 + offsetX),
                    (float)(0 + offsetY)
                );
                translationX = new Vector2((float)(2 * squareSize), (float)(2 * squareSize));
                translationY = new Vector2((float)(2 * squareSize), (float)(-2 * squareSize));
                cosetReps = new Matrix4x4[7];
                for (int i = 0; i < 3; i++)
                {
                    cosetReps[i] = setRotate(90 * (i + 1), center);
                }

                cosetReps[3] = setReflectionMatrix(fundamentalRegion.points[2], fundamentalRegion.points[3]);
                for (int i = 4; i < 7; i++)
                {
                    cosetReps[i] = cosetReps[i - 4];
                    cosetReps[i] = cosetReps[3] * cosetReps[i];
                }

                break;
        }
    }

    public Vector2 getTranslationX()
    {
        return translationX;
    }

    public Vector2 getTranslationY()
    {
        return translationY;
    }

    public Matrix4x4[] getCosetReps()
    {
        return cosetReps;
    }

    public R getId()
    {
        return id;
    }
}