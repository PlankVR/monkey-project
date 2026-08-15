using UnityEngine;
using System.Collections.Generic;

namespace SoftChimpMotion
{
    public class MovementCollision : MonoBehaviour
    {
        public MovementCollision OtherHand;

        [Header("Movement Settings")]

        [Header("Tracking Speed")]
        public float frequency = 50f;

        [Header("How much to Soften Movement")]
        public float damping = 1f;

        [Header("I Dont Remember these, Just Keep them the same lol.")]
        public float rotfrequency = 100f;
        public float rotDamping = 0.9f;

        [Header("Player Rigid Body")]
        [SerializeField] Rigidbody playerRigidbody;

        [Header("Will Change to Hand Or Controller")]
        [SerializeField] Transform target;
        [Space]
        [Header("Movement Force Required to Move")]
        public float climbForce = 1000f;
        public float climbDrag = 500f;

        [Header("Speed Multiplier When Hitting With Both Hands")]
        public float climbDivider = 2f;

        [Header("Bools for When Hand Tracking Is Added")]
        public bool isHandEnabled;
        public bool isControllerEnabled;

        [Header("Objects To Track to")]
        public GameObject Hand;
        public GameObject Controller;
        Vector3 _previousPosition;
        Rigidbody _rigidbody;

        [Header("Collision Data")]
        private List<ContactPoint> _activeCollisions = new List<ContactPoint>();
        public bool _wasIsColliding;

        [Header("Stick and Unstick Distances")]
        public float StickDistance = 0.1f;
        public float UnstickDistance = 0.2f;

        [Header("Unstick Distance")]
        public float unstickThreshold = 2f;

        [Header("Speed Control")]
        public float minSpeed = 0.5f;
        public float maxSpeed = 5f;

        void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.maxAngularVelocity = float.PositiveInfinity;
            _previousPosition = transform.position;
        }

        void Update()
        {
            if (Controller.activeInHierarchy)
            {
                isControllerEnabled = true;
                isHandEnabled = false;
            }
            else
            {
                isHandEnabled = true;
                isControllerEnabled = false;
            }

            if (Vector3.Distance(transform.position, target.position) > unstickThreshold)
            {
                transform.position = target.position;
                _rigidbody.linearVelocity = Vector3.zero;
                _activeCollisions.Clear();
            }
        }

        void FixedUpdate()
        {
            if (isHandEnabled)
            {
                target = Hand.transform;
            }
            else if (isControllerEnabled)
            {
                target = Controller.transform;
            }

            PIDMovement();
            PIDRotation();
            if (_activeCollisions.Count > 0) HookesLaw();
        }

        void PIDMovement()
        {
            float kp = (6f * frequency) * (6f * frequency) * 0.25f;
            float kd = 4.5f * frequency * damping;
            float g = 1 / (1 + kd * Time.fixedDeltaTime + kp * Time.fixedDeltaTime * Time.fixedDeltaTime);
            float ksg = kp * g;
            float kdg = (kd + kp * Time.fixedDeltaTime) * g;
            Vector3 targetPosition = target.position;

            Vector3 force = (targetPosition - transform.position) * ksg + (playerRigidbody.linearVelocity - _rigidbody.linearVelocity) * kdg;
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
                q.x = -q.x;
                q.y = -q.y;
                q.z = -q.z;
                q.w = -q.w;
            }
            q.ToAngleAxis(out float angle, out Vector3 axis);
            axis.Normalize();
            axis *= Mathf.Deg2Rad;
            Vector3 torque = ksg * axis * angle + -_rigidbody.angularVelocity * kdg;
            _rigidbody.AddTorque(torque, ForceMode.Acceleration);
        }

        void HookesLaw()
        {
            Vector3 displacementFromResting = transform.position - target.position;
            Vector3 force = displacementFromResting * climbForce;

            float drag = GetDrag();

            if (_activeCollisions.Count > 0 && !_wasIsColliding && !OtherHand._wasIsColliding)
            {
                playerRigidbody.linearVelocity = playerRigidbody.linearVelocity / climbDivider;
                _wasIsColliding = true;
            }

            if (OtherHand._activeCollisions.Count > 0)
            {
                playerRigidbody.AddForce(force / 2, ForceMode.Acceleration);
                playerRigidbody.AddForce((drag * -playerRigidbody.linearVelocity * climbDrag) / 2, ForceMode.Acceleration);
            }
            else
            {
                playerRigidbody.AddForce(force, ForceMode.Acceleration);
                playerRigidbody.AddForce(drag * -playerRigidbody.linearVelocity * climbDrag, ForceMode.Acceleration);
            }
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
            foreach (ContactPoint contact in collision.contacts)
            {
                _activeCollisions.Add(contact);
            }
        }

        void OnCollisionStay(Collision collision)
        {
            _activeCollisions.Clear();
            foreach (ContactPoint contact in collision.contacts)
            {
                _activeCollisions.Add(contact);
            }
        }

        void OnCollisionExit(Collision other)
        {
            _activeCollisions.Clear();
            _wasIsColliding = false;
        }
    }
}
