using System;
using System.Data;
using UnityEngine;

[Serializable]
public struct CheckpointData
{
    public int num;
    public Transform transform;

    public CheckpointData(int num, Transform transform)
    {
        this.num = num;
        this.transform = transform;
    }
}