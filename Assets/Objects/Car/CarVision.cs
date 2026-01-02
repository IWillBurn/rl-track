using UnityEngine;
using System;

[ExecuteAlways]
public class CarVision : MonoBehaviour
{
    [Header("Rays Settings")]
    [SerializeField, Min(1)] private int _rayCount = 16;
    [SerializeField, Min(0.01f)] private float _wallRayLength = 15;
    [SerializeField, Min(0.01f)] private float _checkpointRayLength = 15f;
    [SerializeField] private float _verticalOffset = 0f;

    [Header("Collision Layers Settings")]
    [SerializeField] private LayerMask _wallsLayerMask;
    [SerializeField] private LayerMask _checkpointLayerMask;

    private WallRayData[] _wallRays;
    private CheckpointRayData[] _checkpointRays;

    public WallRayData[] WallRays => _wallRays;
    public CheckpointRayData[] CheckpointRays => _checkpointRays;

    private void OnValidate()
    {
        if (_rayCount < 1) _rayCount = 1;

        if (_wallRayLength < 0.01f) _wallRayLength = 0.01f;

        if (_checkpointRayLength < 0.01f) _checkpointRayLength = 0.01f;

        if (_wallRays == null || _wallRays.Length != _rayCount)
            _wallRays = new WallRayData[_rayCount];

        if (_checkpointRays == null || _checkpointRays.Length != _rayCount)
            _checkpointRays = new CheckpointRayData[_rayCount];
    }

    public void Start()
    {
        if (_wallRays == null || _wallRays.Length != _rayCount)
            _wallRays = new WallRayData[_rayCount];

        if (_checkpointRays == null || _checkpointRays.Length != _rayCount)
            _checkpointRays = new CheckpointRayData[_rayCount];
    }

    public void Scan(int num)
    {
        if (_wallRays == null || _wallRays.Length != _rayCount)
            _wallRays = new WallRayData[_rayCount];

        if (_checkpointRays == null || _checkpointRays.Length != _rayCount)
            _checkpointRays = new CheckpointRayData[_rayCount];

        Vector3 origin = transform.position + Vector3.up * _verticalOffset;

        for (int i = 0; i < _rayCount; i++)
        {
            // Calculate direction
            float t = _rayCount == 1 ? 0.5f : (float)i / (_rayCount - 1);
            float angle = Mathf.Lerp(-180, 180, t);
            Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 dir = rot * transform.forward;

            // Cast ray to wall and handle results
            WallRayData dataWall = new WallRayData { angleDeg = angle, direction = dir };
            if (Physics.Raycast(origin, dir, out RaycastHit wallRaycastHit, _wallRayLength, _wallsLayerMask, QueryTriggerInteraction.Ignore))
            {
                dataWall.hit = true;
                dataWall.distance = wallRaycastHit.distance / _wallRayLength;
                dataWall.hitPoint = wallRaycastHit.point;
                _wallRays[i] = dataWall;
            }
            else
            {
                dataWall.hit = false;
                dataWall.distance = 1f;
                dataWall.hitPoint = origin + dir * _wallRayLength;
                _wallRays[i] = dataWall;
            }

            // Cast ray to checkpoint and handle results
            CheckpointRayData dataCheckpoint = new CheckpointRayData { angleDeg = angle, direction = dir };
            if (Physics.Raycast(origin, dir, out RaycastHit checkpointRaycastHit, _checkpointRayLength, _checkpointLayerMask, QueryTriggerInteraction.Collide))
            {
                dataCheckpoint.hit = true;
                dataCheckpoint.distance = checkpointRaycastHit.distance / _checkpointRayLength;
                dataCheckpoint.hitPoint = checkpointRaycastHit.point;
                dataCheckpoint.goodHit = (num + 1 == checkpointRaycastHit.transform.gameObject.GetComponentInParent<Checkpoint>().GetCheckopointData().num);
                _checkpointRays[i] = dataCheckpoint;
            }
            else
            {
                dataCheckpoint.hit = false;
                dataCheckpoint.distance = 1f;
                dataCheckpoint.hitPoint = origin + dir * _checkpointRayLength;
                dataCheckpoint.goodHit = false;
                _checkpointRays[i] = dataCheckpoint;
            }
        }
    }
    private void OnDrawGizmos()
    {
        if (_wallRays == null || _wallRays.Length == 0 || _checkpointRays == null || _checkpointRays.Length == 0)
            return;

        // Visualize wall colliding rays
        Vector3 originWall = transform.position + Vector3.up * _verticalOffset;
        foreach (var ray in _wallRays)
        {
            Gizmos.color = Color.green;

            if (ray.hit)
            {
                Gizmos.color = Color.red;
            }

            Gizmos.DrawLine(originWall, ray.hitPoint);
            Gizmos.DrawSphere(ray.hitPoint, 0.05f);
        }

        // Visualize checkpoint colliding rays
        Vector3 originCheckpoint = transform.position + Vector3.up * (_verticalOffset + 0.5f);
        foreach (var ray in _checkpointRays)
        {
            Gizmos.color = Color.blue;

            if (ray.hit)
            {
                Gizmos.color = new(1f, 0f, 1f);
            }

            if (ray.goodHit)
            {
                Gizmos.color = new(1f, 1f, 1f);
            }

            Gizmos.DrawLine(originCheckpoint, ray.hitPoint);
            Gizmos.DrawSphere(ray.hitPoint, 0.05f);
        }
    }
}
