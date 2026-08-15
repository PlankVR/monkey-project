using UnityEngine;

namespace SoftChimpMotion
{
    public class SimpleMovementFeedback : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float frequency = 50f;
        public float damping = 1f;
        public float rotfrequency = 100f;
        public float rotDamping = 0.9f;

        [Header("Physics References")]
        [SerializeField] Rigidbody playerRigidbody;
        [SerializeField] Transform target;

        [Header("Force Settings")]
        public float climbForce = 1000f;
        public float climbDrag = 500f;

        [Header("Stick Distance Settings")]
        public float unstickThreshold = 2f;

        private Vector3 _previousPosition;
        private Rigidbody _rigidbody;
        private bool _isColliding;
        private bool _wasIsColliding;

        void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.maxAngularVelocity = float.PositiveInfinity;
            _previousPosition = transform.position;
        }

        void Update()
        {
            if (Vector3.Distance(transform.position, target.position) > unstickThreshold)
            {
                transform.position = target.position;
                _rigidbody.linearVelocity = Vector3.zero;
            }
        }

        void FixedUpdate()
        {
            PIDMovement();
            PIDRotation();
            if (_isColliding)
                ApplyClimbForce();
        }

        void PIDMovement()
        {
            float kp = (6f * frequency) * (6f * frequency) * 0.25f;
            float kd = 4.5f * frequency * damping;
            float g = 1 / (1 + kd * Time.fixedDeltaTime + kp * Time.fixedDeltaTime * Time.fixedDeltaTime);
            float ksg = kp * g;
            float kdg = (kd + kp * Time.fixedDeltaTime) * g;

            Vector3 force = (target.position - transform.position) * ksg + 
                            (playerRigidbody.linearVelocity - _rigidbody.linearVelocity) * kdg;
            _rigidbody.AddForce(force, ForceMode.Acceleration);
        }

        void PIDRotation()
        {
            float kp = (6f * rotfrequency) * (6f * rotfrequency) * 0.25f;
            float kd = 4.5f * rotfrequency * rotDamping;
            float g = 1 / (1 + kd * Time.fixedDeltaTime + kp * Time.fixedDeltaTime * Time.fixedDeltaTime);
            float ksg = kp * g;
            float kdg = (kd + kp * Time.fixedDeltaTime) * g;

            Quaternion q = target.rotation * Quaternion.Inverse(transform.rotation);
            if (q.w < 0)
            {
                q.x = -q.x; q.y = -q.y; q.z = -q.z; q.w = -q.w;
            }

            q.ToAngleAxis(out float angle, out Vector3 axis);
            axis.Normalize();
            axis *= Mathf.Deg2Rad;

            Vector3 torque = ksg * axis * angle + -_rigidbody.angularVelocity * kdg;
            _rigidbody.AddTorque(torque, ForceMode.Acceleration);
        }

        void ApplyClimbForce()
        {
            Vector3 displacement = transform.position - target.position;
            Vector3 force = displacement * climbForce;
            float drag = GetDrag();

            playerRigidbody.AddForce(force, ForceMode.Acceleration);
            playerRigidbody.AddForce(drag * -playerRigidbody.linearVelocity * climbDrag, ForceMode.Acceleration);

            if (!_wasIsColliding)
                _wasIsColliding = true;
        }

        float GetDrag()
        {
            Vector3 handVelocity = (target.localPosition - _previousPosition) / Time.fixedDeltaTime;
            float drag = 1 / handVelocity.magnitude + 0.01f;
            drag = Mathf.Clamp(drag, 0.03f, 1f);
            _previousPosition = transform.position;
            return drag;
        }

        void OnCollisionEnter(Collision collision)
        {
            _isColliding = true;
        }

        void OnCollisionExit(Collision other)
        {
            _isColliding = false;
            _wasIsColliding = false;
        }
    }
}
