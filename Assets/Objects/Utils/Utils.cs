using System;
using System.Data;
using UnityEngine;
public static class Utils
{
    static public Vector3 NormEuler(Vector3 euler)
    {
        return new Vector3(
            Mathf.Repeat(euler.x, 360f) / 360f,
            Mathf.Repeat(euler.y, 360f) / 360f,
            Mathf.Repeat(euler.z, 360f) / 360f
        );
    }

    static public float NormEulerSingle(float euler)
    {
        return Mathf.Repeat(euler, 360f) / 360f;
    }

    static public float Cut1Float(float value)
    {
        if (value < -1) return -1;
        if (value > 1) return 1;
        return value;
    }

    static public int IntRange(float value)
    {
        if (Mathf.Abs(value) <= 0.1f) return 0;
        float cut = Cut1Float(value);
        return Mathf.FloorToInt(cut * 5f);
    }
}
