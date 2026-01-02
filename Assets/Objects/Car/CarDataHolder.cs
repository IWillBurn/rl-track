using UnityEngine;

public class CarDataHolder : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private CarVision _carVision;
    [SerializeField] private Rigidbody _rb;

    [Header("Layers Settings")]
    [SerializeField] private LayerMask _checkpointLayer;
    [SerializeField] private LayerMask _wallLayer;

    private CarTrackData _data;

    public void Start()
    {
        _data = new(
            _carVision.WallRays,
            _carVision.CheckpointRays, 
            new(-1, _data.checkpointData.transform),
            new(transform, _rb.linearVelocity, _rb.angularVelocity),
            _data.hitWall
        );
    }

    public void RecalculateData()
    {
        _data = new(
            _carVision.WallRays,
            _carVision.CheckpointRays,
            new(_data.checkpointData.num,
            _data.checkpointData.transform),
            new(transform, _rb.linearVelocity, _rb.angularVelocity),
            _data.hitWall);
    }

    public CarTrackData GetData()
    {
        RecalculateData();
        return _data;
    }

    public void OnTriggerEnter(Collider other)
    {
        if ((_checkpointLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            Checkpoint checkpoint = other.gameObject.GetComponentInParent<Checkpoint>();
            CheckpointData checkpointData = checkpoint.GetCheckopointData();
            if (checkpointData.num == ((_data.checkpointData.num + 1) % 40))
            {
                _data.checkpointData = checkpointData;
            }
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        if ((_wallLayer.value & (1 << collision.gameObject.layer)) != 0) _data.hitWall = true;
    }

    public void OnCollisionExit(Collision collision)
    {
        if ((_wallLayer.value & (1 << collision.gameObject.layer)) != 0) _data.hitWall = false;
    }
}
