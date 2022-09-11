using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PointSymmetry {
    
    [Serializable]
    public enum Family
    {
        Cn,
        Cnv,
        Cnh,
        Sn,
        Dn,
        Dnh,
        Dnd,
        T,
        Th,
        Td,
        O,
        Oh,
        I,
        Ih,
    }

    private Vector3 horizontalReflection = new Vector3(-1, 1, 1);
    private Vector3 verticalReflection = new Vector3(1, -1, 1);
    private Vector3 inversion = new Vector3(-1, -1, -1);

    public readonly List<Matrix4x4> matrices;
    
    public readonly Family family;
    public readonly int n;
    public readonly float radius;

    private List<Matrix4x4> getRotations(float angle)
    {
        var rotations = new List<Matrix4x4>();
        for (int i = 0; i < n; i++)
        {
            Vector3 centerOfRotation = Vector3.forward * radius;
            Vector3 rotationAxis = Vector3.up;
            var rotation = Matrix4x4.Rotate(Quaternion.AngleAxis(angle * i, rotationAxis));
            var m = rotation * Matrix4x4.Translate(-centerOfRotation);
            rotations.Add(m);
        }
        return rotations;
    }

    private List<Matrix4x4> rotateAll(List<Matrix4x4> _matrices, Vector3 axis, float angle)
    {
        return _matrices.Select(m => Matrix4x4.Rotate(Quaternion.Euler(axis*angle)) * m).ToList();
    }
    
    private List<Matrix4x4> reflectAll(List<Matrix4x4> _matrices, Vector3 reflection, bool rotate=false)
    {
        var tr = Matrix4x4.Scale(reflection);
        if (rotate) tr *= Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0));
        return _matrices.Select(m => tr * m).ToList();
    }
    
    public PointSymmetry(Family pointGroupFamily, int _n, float _radius)
    {
        family = pointGroupFamily;
        n = _n;
        radius = _radius;
        float angle = 360 / n;

        switch (pointGroupFamily)
        {
            case Family.Cn:
                matrices = getRotations(angle);
                break;
            case Family.Cnv:
                matrices = getRotations(angle);
                matrices.AddRange(reflectAll(getRotations(angle), horizontalReflection));
                break;
            case Family.Cnh:
                matrices = getRotations(angle);
                matrices.AddRange(reflectAll(getRotations(angle), verticalReflection));
                break;
            case Family.Sn:
                matrices = getRotations(angle * 2);
                var m1 = getRotations(angle * 2).Select(m => Matrix4x4.Rotate(Quaternion.Euler(0, angle, 0)) * m)
                    .ToList();
                matrices.AddRange(reflectAll(m1, verticalReflection));
                break;
            case Family.Dn:
                matrices = getRotations(angle);
                matrices.AddRange(rotateAll(getRotations(angle), Vector3.forward, 180));
                break;
            case Family.Dnh:
                matrices = getRotations(angle);
                matrices.AddRange(reflectAll(getRotations(angle), horizontalReflection));
                matrices.AddRange(reflectAll(matrices, verticalReflection));
                break;
            case Family.Dnd:
                matrices = getRotations(angle);
                matrices.AddRange(reflectAll(getRotations(angle), horizontalReflection));
                matrices.AddRange(rotateAll(reflectAll(matrices, verticalReflection), Vector3.up, angle/2f));
                break;
            case Family.T:
                var tetra = Tetrahedron();
                matrices = matricesForPolyhedra(tetra);
                break;
            case Family.Th:
                var tetraH = Cube();
                matrices = matricesForPolyhedra(tetraH);
                break;
            case Family.Td:
                var tetraD = Tetrahedron();
                matrices = matricesForPolyhedra(tetraD);
                matrices.AddRange(reflectAll(matrices, horizontalReflection));
                break;
            case Family.O:
                var octa = Octahedron();
                matrices = matricesForPolyhedra(octa);
                break;
            case Family.Oh:
                var octaH = Octahedron();
                matrices = matricesForPolyhedra(octaH);
                matrices.AddRange(reflectAll(matrices, horizontalReflection));
                break;
            case Family.I:
                var icosa = Icosahedron();
                matrices = matricesForPolyhedra(icosa);
                break;
            case Family.Ih:
                var icosaH = Icosahedron();
                matrices = matricesForPolyhedra(icosaH);
                matrices.AddRange(reflectAll(matrices, horizontalReflection));
                break;
        }

    }

    private List<Matrix4x4> matricesForPolyhedra(List<List<Vector3>> poly)
    {
        var result = new List<Matrix4x4>();
        var centerOfRotation = Vector3.forward * radius;
        foreach (var face in poly)
        {
            var centroid = average(face);
            for (var i = 0; i < face.Count; i++)
            {
                var vert = face[i];
                var direction = vert - centroid;
                var rotation = Matrix4x4.Rotate(Quaternion.LookRotation(direction, centroid));
                var m = rotation * Matrix4x4.Translate(-centerOfRotation);
                result.Add(m);
            }
        }

        return result;
    }

    private Vector3 average(List<Vector3> points)
    {
        return points.Aggregate(new Vector3(0,0,0), (s,v) => s + v) / (float)points.Count;

    }

    public List<List<Vector3>> Tetrahedron(float sideLength = 1)
    {
        float X = sideLength / (2 * Mathf.Sqrt(2));
        float Y = -X;
        
        var a = new Vector3(X, X, X);
        var b = new Vector3(Y, Y, X);
        var c = new Vector3(Y, X, Y);
        var d = new Vector3(X, Y, Y);
    
        return new List<List<Vector3>>
        {
            new List<Vector3>{a,b,c},
            new List<Vector3>{a,b,d},
            new List<Vector3>{a,c,d},
            new List<Vector3>{b,c,d},
        };
    }
    
    public List<List<Vector3>> Cube(float sideLength = 1)
    {
        float X = 0.5f*sideLength;
        float Y = -0.5f*sideLength;

        var a = new Vector3(Y, Y, Y);
        var b = new Vector3(X, Y, Y);
        var c = new Vector3(Y, X, Y);
        var d = new Vector3(Y, Y, X);
        var e = new Vector3(X, X, Y);
        var f = new Vector3(X, Y, X);
        var g = new Vector3(Y, X, X);
        var h = new Vector3(X, X, X);
            
        return new List<List<Vector3>>
        {
            new List<Vector3>{a,b,e,c},
            new List<Vector3>{a,b,f,d},
            new List<Vector3>{a,c,g,d},
            new List<Vector3>{h,f,d,g},
            new List<Vector3>{h,e,b,f},
            new List<Vector3>{h,e,c,g},
        };
    }
    
    public List<List<Vector3>> Octahedron(float sideLength = 1)
    {
        float X = sideLength / Mathf.Sqrt(2);
        float Y = -X;

        var a = new Vector3(X, 0, 0);
        var b = new Vector3(0, X, 0);
        var c = new Vector3(0, 0, X);
        var d = new Vector3(Y, 0, 0);
        var e = new Vector3(0, Y, 0);
        var f = new Vector3(0, 0, Y);
            
        return new List<List<Vector3>>
        {
            new List<Vector3>{b, a, c},
            new List<Vector3>{b, a, f},
            new List<Vector3>{b, c, d},
            new List<Vector3>{b, d, f},
            new List<Vector3>{e, f, d},
            new List<Vector3>{e, f, a},
            new List<Vector3>{e, c, a},
            new List<Vector3>{e, c, d},
        };
    }
    
    public List<List<Vector3>> Icosahedron(float sideLength = 1)
    {
        float root5 = Mathf.Sqrt(5);
        float n  = sideLength/2;
        float X = n*(1+root5)/2;
        float Y = -X;
        float Z = n;
        float W = -n;

        var a = new Vector3(X,Z,0);
        var b = new Vector3(Y,Z,0);
        var c = new Vector3(X,W,0);
        var d = new Vector3(Y,W,0);
        var e = new Vector3(Z,0,X);
        var f = new Vector3(Z,0,Y);
        var g = new Vector3(W,0,X);
        var h = new Vector3(W,0,Y);
        var i = new Vector3(0,X,Z);
        var j = new Vector3(0,Y,Z);
        var k = new Vector3(0,X,W);
        var l = new Vector3(0,Y,W);
            
        return new List<List<Vector3>>
        {
            new List<Vector3>{a,i,e},
            new List<Vector3>{a,f,k},
            new List<Vector3>{c,e,j},
            new List<Vector3>{c,l,f},
            new List<Vector3>{b,g,i},
            new List<Vector3>{b,k,h},
            new List<Vector3>{d,j,g},
            new List<Vector3>{d,h,l},
            new List<Vector3>{a,k,i},
            new List<Vector3>{b,i,k},
            new List<Vector3>{c,j,l},
            new List<Vector3>{d,l,j},
            new List<Vector3>{e,c,a},
            new List<Vector3>{f,a,c},
            new List<Vector3>{g,b,d},
            new List<Vector3>{h,d,b},
            new List<Vector3>{i,g,e},
            new List<Vector3>{j,e,g},
            new List<Vector3>{k,f,h},
            new List<Vector3>{l,h,f},
        };
    }
    
    public List<List<Vector3>> Dodecahedron(float sideLength = 1)
    {
        float root5 = Mathf.Sqrt(5);
        float phi = (1 + root5) / 2;
        float phibar = (1 - root5) / 2;
        float X = sideLength/(root5-1);
        float Y = X*phi;
        float Z = X*phibar;
        float S = -X;
        float T = -Y;
        float W = -Z;
        
        var a = new Vector3(X,X,X);
        var b = new Vector3(X,X,S);
        var c = new Vector3(X,S,X);
        var d = new Vector3(X,S,S);
        var e = new Vector3(S,X,X);
        var f = new Vector3(S,X,S);
        var g = new Vector3(S,S,X);
        var h = new Vector3(S,S,S);
        var i = new Vector3(W,Y,0);
        var j = new Vector3(Z,Y,0);
        var k = new Vector3(W,T,0);
        var l = new Vector3(Z,T,0);
        var m = new Vector3(Y,0,W);
        var n = new Vector3(Y,0,Z);
        var o = new Vector3(T,0,W);
        var p = new Vector3(T,0,Z);
        var q = new Vector3(0,W,Y);
        var r = new Vector3(0,Z,Y);
        var s = new Vector3(0,W,T);
        var t = new Vector3(0,Z,T);
            
        return new List<List<Vector3>>
        {
            new List<Vector3>{b,i,a,m,n},
            new List<Vector3>{e,j,f,p,o},
            new List<Vector3>{c,k,d,n,m},
            new List<Vector3>{h,l,g,o,p},
            new List<Vector3>{c,m,a,q,r},
            new List<Vector3>{b,n,d,t,s},
            new List<Vector3>{e,o,g,r,q},
            new List<Vector3>{h,p,f,s,t},
            new List<Vector3>{e,q,a,i,j},
            new List<Vector3>{c,r,g,l,k},
            new List<Vector3>{b,s,f,j,i},
            new List<Vector3>{h,t,d,k,l},
        };
    }

}

