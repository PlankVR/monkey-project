using UnityEngine;
using easyInputs;

namespace SoftChimpMotion
{
    public class GrabClimb : MonoBehaviour
    {
        [Header("Player Configuration")]
        public Transform PlayerTransform;
        public AudioSource AudioSourceComponent;
        public AudioClip Drop;
        public AudioClip Grab;

        [Header("Options")]
        public LayerMask ClimbingLayerMask;
        public float DetectionRadius = 0.058f;
        public float DetectionDistance = 0.1f;
        public float SnapForce = 1.1f;
        public float MaxClimbSpeed = 4.5f;

        [Header("Hand Setup")]
        public EasyHand handController = EasyHand.LeftHand;

        [Header("Debugging")]
        public bool AutoGrip = false;

        private bool IsGripping;
        private bool isClimbing;

        private Vector3 Velocity;
        private Transform connectedThing;
        private Collider CurrentClimbableCollider;
        private Rigidbody PlayerRigidbody;
        private Rigidbody ClimbableRigidbody;
        private MotionSettings PlayerMotionSettings;

        private Vector3 HitPoint;
        private Collider HitCollider;

        // Make these public/protected if not already, for MotionSettings access
        public bool IsClimbing { get { return isClimbing; } private set { isClimbing = value; } } // Changed to property for encapsulation
        public Transform ConnectedThing { get { return connectedThing; } private set { connectedThing = value; } }

        private void Start()
        {
            PlayerRigidbody = PlayerTransform.GetComponent<Rigidbody>();
            PlayerMotionSettings = PlayerTransform.GetComponent<MotionSettings>();
        }

        private void Update()
        {
            if (AutoGrip)
            {
                IsGripping = true;
            }
            else
            {
                if (EasyInputs.GetGripButtonDown(handController))
                {
                    IsGripping = true;
                }
                else if (EasyInputs.GetGripButtonFloat(handController) < 0.1f)
                {
                    IsGripping = false;
                }
            }

            DetectClimbable();
        }

        private void DetectClimbable()
        {
            HitCollider = null;

            var overlaps = Physics.OverlapSphere(transform.position, DetectionRadius, ClimbingLayerMask);
            if (overlaps.Length > 0)
            {
                HitCollider = overlaps[0];
                HitPoint = transform.position;
            }
            else
            {
                if (Physics.SphereCast(transform.position, DetectionRadius, transform.forward, out var hit, DetectionDistance, ClimbingLayerMask))
                {
                    HitCollider = hit.collider;
                    HitPoint = hit.point;
                }
            }
        }

        private void FixedUpdate()
        {
            if (IsGripping && (IsClimbing || HitCollider != null))
            {
                HandleGrip();
            }
            else if (IsClimbing && !IsGripping)
            {
                HandleRelease();
            }
        }

        private void HandleGrip()
        {
            if (!IsClimbing && HitCollider != null)
            {
                AudioSourceComponent.clip = Grab;
                AudioSourceComponent.Play();

                if (ConnectedThing != null)
                    Destroy(ConnectedThing.gameObject);

                ConnectedThing = new GameObject("FixedObjectJoint").transform;
                ConnectedThing.position = HitPoint;
                ConnectedThing.parent = HitCollider.transform;

                CurrentClimbableCollider = HitCollider;
                ClimbableRigidbody = CurrentClimbableCollider.attachedRigidbody;

                if (ClimbableRigidbody != null)
                {
                    Velocity = PlayerMotionSettings.currentVelocity * 100f;
                    ClimbableRigidbody.AddForce(Velocity.magnitude * SnapForce * Velocity.normalized, ForceMode.Acceleration);
                }

                IsClimbing = true;
            }

            // REMOVE the velocity set here -- now handled in MotionSettings.FixedUpdate
        }

        private void HandleRelease()
        {
            if (ConnectedThing != null)
                Destroy(ConnectedThing.gameObject);

            ClimbableRigidbody = null;
            ConnectedThing = null;

            AudioSourceComponent.clip = Drop;
            AudioSourceComponent.Play();

            if (CurrentClimbableCollider != null)
            {
                CurrentClimbableCollider.enabled = true;
                CurrentClimbableCollider = null;
            }

            IsClimbing = false;
        }

        public void ForceRelease()
        {
            if (IsClimbing)
            {
                HandleRelease();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, DetectionRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, transform.forward * DetectionDistance);
        }
    }
}