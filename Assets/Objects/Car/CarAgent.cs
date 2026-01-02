using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Text;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class CarAgent : Agent
{
    [Header("Prefabs")]
    [SerializeField] private GameObject _carPrefab;
    [SerializeField] private Transform _parentTransfrom;

    [Header("Tracks Settings")]
    [SerializeField] private List<TrackGenerator> _tracks;
    [SerializeField] private List<Vector3> _startPositions;
    [SerializeField] private List<Vector3> _startRotations;
    [SerializeField] private bool _regenerateTrack;
    private int _trackInd;

    [Header("Camera Settings")]
    [SerializeField] private bool _pinCamera;
    [SerializeField] private Transform _camera;
    [SerializeField] private Vector3 _cameraLocalPosition;
    [SerializeField] private Vector3 _cameraLocalRotation;

    [Header("Car Settings")]
    [SerializeField] private GameObject _car;
    private CarVision _carVision;
    private CarDataHolder _dataHolder;
    private CarController _controller;

    [Header("Lock Settings")]
    [SerializeField] private bool _useLock;
    [SerializeField] private float _lockTime;
    private bool _lock = true;
    private float _lockTimer = 0f;

    [Header("Checkpoint Settings")]
    [SerializeField] int _lastCheckpoint = 0;
    [SerializeField] float _lastCheckpointTime = 0f;

    // ========== REWARDING ========== 
    private void UpdateReward(CarTrackData data)
    {
        // Price of existence
        AddReward(-0.05f);

        // Getting checkpoint
        if (_lastCheckpoint != data.checkpointData.num)
        {
            _lastCheckpoint = data.checkpointData.num;
            _lastCheckpointTime = 0f;
            AddReward(5f);

            // End race
            if (data.checkpointData.num == 39)
            {
                AddReward(100f);
                EndEpisode();
                return;
            }
        }

        // Hitting wall penalty
        if (data.hitWall)
        {
            AddReward(-0.05f);
        }

        // Losing way penalty
        if (_lastCheckpointTime > 5f)
        {
            AddReward(-20);
            EndEpisode();
            return;
        }
    }

    // ========== SERVICE ========== 
    private void ResetState()
    {
        if (_car != null)
        {
            _camera.transform.parent = null;
            Destroy(_car);
        }

        _lastCheckpoint = -1;
        _lastCheckpointTime = 0f;
    }

    private int GetTrackInd()
    {
        _trackInd = UnityEngine.Random.Range(0, _startPositions.Count);
        return _trackInd;
    }

    private CarController InstantiateCar()
    {
        _car = Instantiate(_carPrefab, _parentTransfrom);

        _car.transform.localEulerAngles = _startRotations[_trackInd];
        float deltaValue = UnityEngine.Random.Range(0f, 0.5f);
        float deltaValueHor = UnityEngine.Random.Range(-2f, 2f);
        Vector3 posOffset = -_car.transform.forward * (deltaValue + 1.5f) + _car.transform.right * deltaValueHor;
        _car.transform.localPosition = _startPositions[_trackInd] + posOffset;

        Vector3 rotOffset = new(0f, UnityEngine.Random.Range(-15f, 15f), 0f);
        _car.transform.localEulerAngles = _startRotations[_trackInd] + rotOffset;

        _controller = _car.GetComponent<CarController>();
        _dataHolder = _car.GetComponent<CarDataHolder>();
        _carVision = _car.GetComponent<CarVision>();

        return _controller;
    }

    private void SetupCamera()
    {
        if (_pinCamera)
        {
            _camera.parent = _car.transform;
            _camera.localPosition = _cameraLocalPosition;
            _camera.localEulerAngles = _cameraLocalRotation;
        }
    }

    private void SetupLock()
    {
        if (_useLock)
        {
            _lock = true;
        }
    }

    private void UpdateLock()
    {
        if (_useLock && _lock)
        {
            _lockTimer += Time.deltaTime;
            if (_lockTimer >= _lockTime)
            {
                _lockTimer = 0;
                _lock = false;
            }
        }
    }

    // ========== MLAGENT OVERRIDES ========== 
    public override void OnEpisodeBegin()
    {
        ResetState();

        GetTrackInd();

        InstantiateCar();

        SetupCamera();

        SetupLock();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        _carVision.Scan(_lastCheckpoint);
        CarTrackData data = _dataHolder.GetData();

        if (data.checkpointData.transform == null) data.checkpointData.transform = _tracks[_trackInd].Checkpoints[0].transform;
        sensor.AddObservation(Vector3.Dot(data.carData.transform.forward, Quaternion.Euler(0, -90, 0) * data.checkpointData.transform.forward)); // 1
        //sensor.AddObservation(data.carData.transform.forward); // 3?
        //sensor.AddObservation(Quaternion.Euler(0, -90, 0) * data.checkpointData.transform.forward); // 3?
        //sensor.AddObservation(data.carData.linearVelocity); // 3?
        //sensor.AddObservation(data.carData.angularVelocity); // 3?


        // 16 * 2 = 32
        foreach (var r in data.wallRayDatas)
        {
            sensor.AddObservation(r.hit ? 1.0f : 0.0f);     // 1
            sensor.AddObservation(r.distance);              // 1
        }

        // 16 * 3 = 48
        foreach (var r in data.checkpointRayDatas)
        {
            sensor.AddObservation(r.hit ? 1.0f : 0.0f);     // 1
            sensor.AddObservation(r.distance);              // 1
            sensor.AddObservation(r.goodHit ? 1.0f : 0.0f); // 1
        }

        // 13 + 32 + 48 = 93
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        _controller.SetControll((actions.DiscreteActions[0] - 5) / 5f, (actions.DiscreteActions[1] - 5) / 5f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> actions = actionsOut.DiscreteActions;

        if (_lock)
        {
            actions[0] = 5;
            actions[1] = 5;
            return;
        }

        actions[0] = Utils.IntRange(Input.GetAxis("Vertical")) + 5;
        actions[1] = Utils.IntRange(Input.GetAxis("Horizontal")) + 5;
    }

    // ========== UNITY ENGINE OVERRIDES ========== 

    private void FixedUpdate()
    {
        // Timers
        _lastCheckpointTime += Time.fixedDeltaTime;
        UpdateLock();

        // Getting data
        if (_carVision == null) return;
        _carVision.Scan(_lastCheckpoint);

        if (_dataHolder == null) return;
        CarTrackData data = _dataHolder.GetData();

        // Rewarding
        UpdateReward(data);
    }
}
