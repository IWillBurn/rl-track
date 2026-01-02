using Unity.Mathematics;
using UnityEngine;
public class CarController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody _rb;

    [Header("Physics Settings")]
    [SerializeField] private float _accelerationForward = 1f;
    [SerializeField] private float _accelerationReverse = 1f;
    [SerializeField] private float _maxSpeed = 10f;
    [SerializeField] private float _turnStrength = 180f;
    [SerializeField] private float _gravityForce = 10f;
    [SerializeField] private float _dragOnGround = 3f;

    [Header("On Ground Raycast Settings")]
    [SerializeField] LayerMask _groundLayer;
    [SerializeField] float _groundRayLength = 0.4f;
    [SerializeField] Transform _groundRayPoint;

    private float _speedInput;
    private float _turnInput;
    private bool _grounded;

    public void SetControll(float vert, float hor)
    {
        vert = Utils.Cut1Float(vert);
        hor = Utils.Cut1Float(hor);

        _speedInput = 0f;
        if (vert > 0) _speedInput = vert * _accelerationForward * 1000f;
        else if (vert < 0) _speedInput = vert * _accelerationReverse * 1000f;
        _turnInput = hor;

        ApplyControll();
    }

    private void ApplyControll()
    {
        int isHaveSpeed = _rb.linearVelocity.magnitude > 0.5f ? 1 : 0;
        if (_grounded) transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0f, _turnInput * _turnStrength * Time.deltaTime * isHaveSpeed, 0f));
    }

    private void FixedUpdate()
    {
        _grounded = false;
        RaycastHit hit;

        if (Physics.Raycast(_groundRayPoint.position, -transform.up, out hit, _groundRayLength, _groundLayer))
        {
            _grounded = true;
            transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
        }

        if (_grounded)
        {
            _rb.linearDamping = _dragOnGround;
            if (Mathf.Abs(_speedInput) > 0) _rb.AddForce(transform.forward * _speedInput);
        }
        else _rb.linearDamping = 0.1f;
    }

    void OnDrawGizmos()
    {
        // Visualize car forward direction

        Gizmos.color = new(1f, 0.7f, 0.7f);
        Vector3 rayEnd = transform.position + transform.forward * 3f;
        Gizmos.DrawLine(transform.position, rayEnd);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(rayEnd, 0.1f);
    }
}