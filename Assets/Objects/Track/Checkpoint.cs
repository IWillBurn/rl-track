using System;
using System.Data;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private CheckpointData _data;

    public CheckpointData GetCheckopointData()
    {
        return _data;
    }

    void OnDrawGizmos()
    {
        // Visualize checkpoint forward direction

        Gizmos.color = new(1, 0.7f, 0.7f);
        Vector3 rayEnd = transform.position + Quaternion.Euler(0, -90, 0) * transform.forward * 3f;
        Gizmos.DrawLine(transform.position, rayEnd);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(rayEnd, 0.1f);
    }
}
