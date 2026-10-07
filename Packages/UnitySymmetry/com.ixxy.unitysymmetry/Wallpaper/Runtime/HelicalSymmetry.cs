using System;
using System.Collections.Generic;
using UnityEngine;

/// Repeated rotation and translation along the local Y axis. Slot zero is identity.
public sealed class HelicalSymmetry
{
    public readonly List<Matrix4x4> matrices;

    public HelicalSymmetry(int copies, float angleDegrees, float advance)
    {
        if (copies < 1) { throw new ArgumentOutOfRangeException(nameof(copies)); }
        matrices = new List<Matrix4x4>(copies);
        for (int i = 0; i < copies; i++)
        {
            matrices.Add(Matrix4x4.TRS(Vector3.up * (i * advance),
                Quaternion.AngleAxis(i * angleDegrees, Vector3.up), Vector3.one));
        }
    }
}
