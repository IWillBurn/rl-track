using System;
using UnityEngine;

[Serializable]
public struct WallRayData
{
    public float angleDeg;
    public Vector3 direction;
    public float distance;
    public Vector3 hitPoint;
    public bool hit;

    override public string ToString()
    {
        return "(" + distance.ToString() + ", " + hit.ToString() + ")";
    }
}

[Serializable]
public struct CheckpointRayData
{
    public float angleDeg;
    public Vector3 direction;
    public float distance;
    public Vector3 hitPoint;
    public bool hit;
    public bool goodHit;

    override public string ToString()
    {
        return "(" + distance.ToString() + ", " + hit.ToString() + ", " + goodHit.ToString() + ")";
    }
}

[Serializable]
public struct CarTrackData
{
    public WallRayData[] wallRayDatas;
    public CheckpointRayData[] checkpointRayDatas;
    public CheckpointData checkpointData;
    public CarData carData;
    public bool hitWall;

    public CarTrackData
    (
        WallRayData[] wallRayDatas,
        CheckpointRayData[] checkpointRayDatas,
        CheckpointData checkpointData,
        CarData carData,
        bool hitWall
    )
    {
        this.wallRayDatas = wallRayDatas;
        this.checkpointRayDatas = checkpointRayDatas;
        this.checkpointData = checkpointData;
        this.carData = carData;
        this.hitWall = hitWall;
    }
}

[Serializable]
public struct CarData
{
    public Transform transform;
    public Vector3 linearVelocity;
    public Vector3 angularVelocity;

    public CarData
    (
        Transform transform,
        Vector3 linearVelocity,
        Vector3 angularVelocity
    )
    {
        this.transform = transform;
        this.linearVelocity = linearVelocity;
        this.angularVelocity = angularVelocity;
    }
}