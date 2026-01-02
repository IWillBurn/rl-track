using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class TrackGenerator : MonoBehaviour
{
    [Header("Seed Settings")]
    [SerializeField] private bool _useSeed = true;
    [SerializeField] private int _seed = 1337;

    [Header("Spline Source")]
    [SerializeField] private SplineContainer _spline;

    [Header("Track settings")]
    [SerializeField, Min(2)] private int _segments = 200;
    [SerializeField] private float _trackWidth = 5f;

    [Header("Border Settings")]
    [SerializeField] private float _borderHeight = 2f;
    [SerializeField] private bool _generateBorders = true;

    [Header("Checkpoints Settings")]
    [SerializeField] private bool _placeCheckpoints = true;
    [SerializeField] private List<Transform> _checkpoints = new List<Transform>();

    [Header("Random Objects Settings")]
    [SerializeField] private bool _placeRandomObjects = true;
    [SerializeField] private List<Transform> _randomObjects = new List<Transform>();

    [Tooltip("Meshes")]
    [SerializeField] private MeshFilter _startCapFilter;
    [SerializeField] private MeshFilter _endCapFilter;
    [SerializeField] private MeshFilter _floorFilter;
    [SerializeField] private MeshFilter _leftBorderFilter;
    [SerializeField] private MeshFilter _rightBorderFilter;

    public List<Transform> Checkpoints => _checkpoints;

    [ContextMenu("Generate Track")]
    public void GenerateTrack()
    {
        if (_useSeed) UnityEngine.Random.InitState(_seed);
        else UnityEngine.Random.InitState(Mathf.FloorToInt(Time.time * 1000000f));

#if UNITY_EDITOR
        if (!gameObject.scene.IsValid() || PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;
#endif

        if (_spline == null)
        {
            Debug.LogWarning("TrackGenerator: SplineContainer is not assigned.");
            return;
        }

        if (!_floorFilter) _floorFilter = GetOrCreateChildMesh("Floor");

        if (_generateBorders)
        {
            if (!_leftBorderFilter) _leftBorderFilter = GetOrCreateChildMesh("LeftBorder");
            if (!_rightBorderFilter) _rightBorderFilter = GetOrCreateChildMesh("RightBorder");
        }

        GenerateFloorMesh();

        if (_generateBorders)
        {
            GenerateBordersMesh();
            GenerateEndCaps();
        }

        if (_placeCheckpoints) PlaceCheckpoints();
        if (_placeRandomObjects) PlaceRandomObjects();
    }

    MeshFilter GetOrCreateChildMesh(string name)
    {
        Transform child = transform.Find(name);
        MeshFilter mf;

        if (child == null)
        {
            GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            mf = go.GetComponent<MeshFilter>();
        }
        else
        {
            mf = child.GetComponent<MeshFilter>();
            if (!mf) mf = child.gameObject.AddComponent<MeshFilter>();
            if (!child.GetComponent<MeshRenderer>()) child.gameObject.AddComponent<MeshRenderer>();
        }

        return mf;
    }
    void SampleSpline(float t, out Vector3 pos, out Vector3 tangent, out Vector3 up)
    {
        t = Mathf.Clamp01(t);

        var nativeSpline = _spline.Spline;

        float3 localPos = SplineUtility.EvaluatePosition(nativeSpline, t);
        float3 localTan = SplineUtility.EvaluateTangent(nativeSpline, t);

        pos = _spline.transform.TransformPoint(localPos);
        tangent = _spline.transform.TransformDirection(localTan).normalized;
        up = _spline.transform.up;
    }

    void GenerateFloorMesh()
    {
        int vCount = _segments * 2;
        int tCount = (_segments - 1) * 2 * 3;

        var vertices = new Vector3[vCount];
        var uvs = new Vector2[vCount];
        var tris = new int[tCount];

        for (int i = 0; i < _segments; i++)
        {
            float t = (_segments == 1) ? 0f : i / (float)(_segments - 1);
            SampleSpline(t, out Vector3 pos, out Vector3 tangent, out Vector3 up);

            Vector3 side = Vector3.Cross(up, tangent).normalized;

            Vector3 leftWorld = pos - side * (_trackWidth * 0.5f);
            Vector3 rightWorld = pos + side * (_trackWidth * 0.5f);

            int vi = i * 2;
            vertices[vi] = transform.InverseTransformPoint(leftWorld);
            vertices[vi + 1] = transform.InverseTransformPoint(rightWorld);

            float v = t;
            uvs[vi] = new Vector2(0, v);
            uvs[vi + 1] = new Vector2(1, v);

            if (i < _segments - 1)
            {
                int ti = i * 6;

                int v0 = vi;
                int v1 = vi + 1;
                int v2 = vi + 2;
                int v3 = vi + 3;

                tris[ti + 0] = v0;
                tris[ti + 1] = v2;
                tris[ti + 2] = v1;

                tris[ti + 3] = v1;
                tris[ti + 4] = v2;
                tris[ti + 5] = v3;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "TrackFloor";
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        _floorFilter.sharedMesh = mesh;
        UpdateMeshCollider(_floorFilter);
    }
    void GenerateBordersMesh()
    {
        if (_leftBorderFilter == null || _rightBorderFilter == null) return;

        int vCount = _segments * 2;
        int segCount = _segments - 1;

        var verticesL = new Vector3[vCount];
        var verticesR = new Vector3[vCount];
        var trisL = new int[segCount * 6];
        var trisR = new int[segCount * 6];

        for (int i = 0; i < _segments; i++)
        {
            float t = (_segments == 1) ? 0f : i / (float)(_segments - 1);
            SampleSpline(t, out Vector3 pos, out Vector3 tangent, out Vector3 up);

            Vector3 side = Vector3.Cross(up, tangent).normalized;

            Vector3 edgeLeft = pos - side * (_trackWidth * 0.5f);
            Vector3 edgeRight = pos + side * (_trackWidth * 0.5f);

            int vi = i * 2;

            verticesL[vi] = transform.InverseTransformPoint(edgeLeft);
            verticesL[vi + 1] = transform.InverseTransformPoint(edgeLeft + up * _borderHeight);

            verticesR[vi] = transform.InverseTransformPoint(edgeRight);
            verticesR[vi + 1] = transform.InverseTransformPoint(edgeRight + up * _borderHeight);

            if (i < _segments - 1)
            {
                int ti = i * 6;

                int v0 = vi;
                int v1 = vi + 1;
                int v2 = vi + 2;
                int v3 = vi + 3;

                trisL[ti + 0] = v0;
                trisL[ti + 1] = v1;
                trisL[ti + 2] = v2;

                trisL[ti + 3] = v2;
                trisL[ti + 4] = v1;
                trisL[ti + 5] = v3;

                trisR[ti + 0] = v0;
                trisR[ti + 1] = v2;
                trisR[ti + 2] = v1;

                trisR[ti + 3] = v2;
                trisR[ti + 4] = v3;
                trisR[ti + 5] = v1;
            }
        }

        Mesh meshL = new Mesh();
        meshL.name = "LeftBorder";
        meshL.vertices = verticesL;
        meshL.triangles = trisL;
        meshL.RecalculateNormals();
        meshL.RecalculateBounds();

        Mesh meshR = new Mesh();
        meshR.name = "RightBorder";
        meshR.vertices = verticesR;
        meshR.triangles = trisR;
        meshR.RecalculateNormals();
        meshR.RecalculateBounds();

        _leftBorderFilter.sharedMesh = meshL;
        _rightBorderFilter.sharedMesh = meshR;

        UpdateMeshCollider(_leftBorderFilter);
        UpdateMeshCollider(_rightBorderFilter);
    }

    void UpdateMeshCollider(MeshFilter mf)
    {
        if (mf == null) return;

        var col = mf.GetComponent<MeshCollider>();
        if (col == null) col = mf.gameObject.AddComponent<MeshCollider>();

        col.sharedMesh = null;
        col.sharedMesh = mf.sharedMesh;
    }

    void PlaceCheckpoints()
    {
        if (_checkpoints == null || _checkpoints.Count == 0) return;

        List<Transform> valid = new List<Transform>();
        foreach (var cp in _checkpoints)
        {
            if (cp != null) valid.Add(cp);
        }

        if (valid.Count == 0) return;

        var nativeSpline = _spline.Spline;
        bool isClosed = nativeSpline.Closed;

        for (int i = 0; i < valid.Count; i++)
        {
            int div = isClosed ? valid.Count : valid.Count + 1;
            int ind = isClosed ? i : (i + 1);

            float t = valid.Count == 1 ? 0.5f : ind / (float)(div);

            SampleSpline(t, out Vector3 pos, out Vector3 tangent, out Vector3 up);

            Vector3 side = Vector3.Cross(up, tangent).normalized;
            Quaternion rot = Quaternion.LookRotation(side, up);

            valid[i].position = pos;
            valid[i].rotation = rot;
        }
    }
    void PlaceRandomObjects()
    {
        if (_randomObjects == null || _randomObjects.Count == 0)
            return;

        List<Transform> valid = new List<Transform>();
        foreach (var obj in _randomObjects)
        {
            if (obj != null) valid.Add(obj);
        }

        if (valid.Count == 0) return;

        float maxSideOffset = _trackWidth * 0.4f;

        for (int i = 0; i < valid.Count; i++)
        {
            float t = UnityEngine.Random.Range(0f, 1f);

            SampleSpline(t, out Vector3 pos, out Vector3 tangent, out Vector3 up);

            Vector3 side = Vector3.Cross(up, tangent).normalized;

            float offset = UnityEngine.Random.Range(-maxSideOffset, maxSideOffset);
            pos += side * offset;

            Quaternion rot = Quaternion.LookRotation(tangent, up);

            valid[i].position = pos;
            valid[i].rotation = rot;
        }
    }

    void ClearCapMesh(MeshFilter mf)
    {
        if (mf == null) return;

        mf.sharedMesh = null;

        var col = mf.GetComponent<MeshCollider>();
        if (col != null) col.sharedMesh = null;
    }
    void GenerateEndCaps()
    {
        if (!_generateBorders)
        {
            ClearCapMesh(_startCapFilter);
            ClearCapMesh(_endCapFilter);
            return;
        }

        var nativeSpline = _spline.Spline;
        bool isClosed = nativeSpline.Closed;
        if (isClosed)
        {
            ClearCapMesh(_startCapFilter);
            ClearCapMesh(_endCapFilter);
            return;
        }
        if (!_startCapFilter) _startCapFilter = GetOrCreateChildMesh("StartBorderCap");
        if (!_endCapFilter) _endCapFilter = GetOrCreateChildMesh("EndBorderCap");

        GenerateCapAt(0f, _startCapFilter, isStart: true);
        GenerateCapAt(1f, _endCapFilter, isStart: false);
    }
    void GenerateCapAt(float t, MeshFilter mf, bool isStart)
    {
        SampleSpline(t, out Vector3 pos, out Vector3 tangent, out Vector3 up);

        if (tangent.sqrMagnitude < 1e-6f)
        {
            float dt = 1f / Mathf.Max(_segments - 1, 1);
            float t2 = Mathf.Clamp01(isStart ? t + dt : t - dt);

            Vector3 pos2;
            Vector3 tan2;
            Vector3 up2;
            SampleSpline(t2, out pos2, out tan2, out up2);

            tangent = (pos2 - pos).normalized;
            if (tangent.sqrMagnitude < 1e-6f) tangent = Vector3.forward;
        }

        Vector3 side = Vector3.Cross(up, tangent);
        if (side.sqrMagnitude < 1e-6f) side = Vector3.right; 
        side.Normalize();

        Vector3 edgeLeft = pos - side * (_trackWidth * 0.5f);
        Vector3 edgeRight = pos + side * (_trackWidth * 0.5f);

        Vector3[] vertices = new Vector3[4];
        int[] triangles = new int[6];

        vertices[0] = transform.InverseTransformPoint(edgeLeft);
        vertices[1] = transform.InverseTransformPoint(edgeRight);
        vertices[2] = transform.InverseTransformPoint(edgeLeft + up * _borderHeight);
        vertices[3] = transform.InverseTransformPoint(edgeRight + up * _borderHeight);

        triangles[0] = 0;
        triangles[1] = 2;
        triangles[2] = 1;

        triangles[3] = 2;
        triangles[4] = 3;
        triangles[5] = 1;

        Mesh mesh = new Mesh();
        mesh.name = isStart ? "StartBorderCap" : "EndBorderCap";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (mesh.vertexCount < 3 || mesh.triangles == null || mesh.triangles.Length < 3)
        {
            mf.sharedMesh = null;
            var c = mf.GetComponent<MeshCollider>();
            if (c != null) c.sharedMesh = null;
            return;
        }

        mf.sharedMesh = mesh;
        UpdateMeshCollider(mf);
    }

    void OnValidate()
    {
        _segments = Mathf.Max(2, _segments);
        _borderHeight = Mathf.Max(0f, _borderHeight);

        if (!Application.isEditor) return;

#if UNITY_EDITOR
        if (!gameObject.scene.IsValid() || PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;
#endif

        if (_spline != null) GenerateTrack();
    }
}