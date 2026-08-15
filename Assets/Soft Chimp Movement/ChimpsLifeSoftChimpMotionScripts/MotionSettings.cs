using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace SoftChimpMotion
{
    public class MotionSettings : MonoBehaviour
    {
        public static MotionSettings Instance { get; private set; }

        public enum LocomotionState
        {
            Disabled,
            Enabled
        }

        [Header("Player Stuff")]

        public Rigidbody rb;
        public Transform headCol;
        public GameObject leftCon;
        public GameObject rightCon;

        [Header("Settings for Movement")]

        [SerializeField]
        public List<ProfileEntry> movementProfiles;

        public static string currentMovementMode = "Snappy";

        [Tooltip("Checks for Player Movement")]
        public LocomotionState locomotionEnabledState;

        [Header("Movement Bools (Changing might Affect Gameplay stuff)")]
        public int velocityHistorySize;
        public bool inWaterOrSpace;

        private Vector3 lastPosition;
        private Vector3[] velocityHistory;
        private int velocityIndex;
        public Vector3 currentVelocity;
        private Vector3 denormalizedVelocityAverage;

        private MovementCollision leftHand;
        private MovementCollision rightHand;
        private bool hasCheckedFile = false;

        private string lastAppliedMovementMode = "";

        // New fields for climbing integration
        private GrabClimb leftGrab;
        private GrabClimb rightGrab;
        public float climbGainMultiplier = 0.8f; // Tune this (0.5-1.0) to soften the constraint and reduce flings/jitter. Lower = smoother but less responsive.

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }

            if (leftCon != null)
            {
                leftHand = leftCon.GetComponent<MovementCollision>();
                leftGrab = leftCon.GetComponent<GrabClimb>(); // Assuming GrabClimb is on the same GameObject as MovementCollision
            }
            if (rightCon != null)
            {
                rightHand = rightCon.GetComponent<MovementCollision>();
                rightGrab = rightCon.GetComponent<GrabClimb>();
            }

            InitializeVelocities();
            ApplyMovementSettings(true); // Force apply at start
        }

        private void Update()
        {
            if (inWaterOrSpace)
            {
                rb.useGravity = false;
            }
            else
            {
                rb.useGravity = true;
            }

            StoreVelocities();
            ApplyMovementSettings();
        }

        private void FixedUpdate()
        {
            Vector3 totalTargetVelocity = Vector3.zero;
            int activeClimberCount = 0;
            float maxClimbSpeed = 4.5f; // Default from GrabClimb; make this a public field if it varies per hand

            // Collect from left hand
            if (leftGrab != null && leftGrab.IsClimbing)
            {
                if (leftGrab.ConnectedThing != null)
                {
                    Vector3 handPos = leftGrab.transform.position;
                    Vector3 connPos = leftGrab.ConnectedThing.position;
                    Vector3 target = (connPos - handPos) / Time.fixedDeltaTime * climbGainMultiplier;
                    totalTargetVelocity += target;
                    activeClimberCount++;
                    maxClimbSpeed = leftGrab.MaxClimbSpeed; // Use left's value as fallback
                }
                else
                {
                    // Safety: If ConnectedThing null, release to prevent zero-fling
                    leftGrab.ForceRelease();
                }
            }

            // Collect from right hand
            if (rightGrab != null && rightGrab.IsClimbing)
            {
                if (rightGrab.ConnectedThing != null)
                {
                    Vector3 handPos = rightGrab.transform.position;
                    Vector3 connPos = rightGrab.ConnectedThing.position;
                    Vector3 target = (connPos - handPos) / Time.fixedDeltaTime * climbGainMultiplier;
                    totalTargetVelocity += target;
                    activeClimberCount++;
                    maxClimbSpeed = rightGrab.MaxClimbSpeed;
                }
                else
                {
                    rightGrab.ForceRelease();
                }
            }

            // Apply averaged velocity if climbing
            if (activeClimberCount > 0)
            {
                Vector3 avgTargetVelocity = totalTargetVelocity / activeClimberCount;
                rb.linearVelocity = Vector3.ClampMagnitude(avgTargetVelocity, maxClimbSpeed);
            }
        }

        private void InitializeVelocities()
        {
            velocityHistory = new Vector3[velocityHistorySize];
            lastPosition = transform.position;
        }

        private void StoreVelocities()
        {
            velocityIndex = (velocityIndex + 1) % velocityHistorySize;
            Vector3 oldestVelocity = velocityHistory[velocityIndex];
            currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
            denormalizedVelocityAverage += (currentVelocity - oldestVelocity) / (float)velocityHistorySize;
            velocityHistory[velocityIndex] = currentVelocity;
            lastPosition = transform.position;
        }

        public Vector3 GetCurrentVelocity()
        {
            return currentVelocity;
        }

        public void TurnPlayer(float degrees)
        {
            transform.RotateAround(headCol.transform.position, transform.up, degrees);
            denormalizedVelocityAverage = Quaternion.Euler(0, degrees, 0) * denormalizedVelocityAverage;
            for (int i = 0; i < velocityHistory.Length; i++)
            {
                velocityHistory[i] = Quaternion.Euler(0, degrees, 0) * velocityHistory[i];
            }
        }

        public void AddMovementProfile(string name, MovementProfile profile)
        {
            if (!movementProfiles.Exists(x => x.name == name))
            {
                movementProfiles.Add(new ProfileEntry { name = name, profile = profile });
            }
        }

        public void RemoveMovementProfile(string name)
        {
            movementProfiles.RemoveAll(x => x.name == name);
        }

        public void SetMovementMode(string mode)
        {
            ProfileEntry profileEntry = movementProfiles.Find(x => x.name == mode);
            if (profileEntry != null)
            {
                currentMovementMode = mode;
                ApplyMovementSettings(true);
            }
            else
            {
                Debug.LogWarning("Movement mode not found: " + mode);
            }
        }

        public void ApplyMovementSettings(bool forceApply = false)
        {
            if (!forceApply && lastAppliedMovementMode == currentMovementMode)
            {
                return;
            }

            if (leftHand != null && rightHand != null)
            {
                ProfileEntry profileEntry = movementProfiles.Find(x => x.name == currentMovementMode);
                if (profileEntry != null)
                {
                    MovementProfile profile = profileEntry.profile;
                    leftHand.frequency = profile.frequency;
                    leftHand.damping = profile.damping;
                    leftHand.rotfrequency = profile.rotFrequency;
                    leftHand.rotDamping = profile.rotDamping;
                    leftHand.UnstickDistance = profile.unstickDistance;
                    leftHand.StickDistance = profile.stickDistance;
                    leftHand.climbDivider = profile.climbDivider;
                    leftHand.climbForce = profile.climbForce;

                    rightHand.frequency = profile.frequency;
                    rightHand.damping = profile.damping;
                    rightHand.rotfrequency = profile.rotFrequency;
                    rightHand.rotDamping = profile.rotDamping;
                    rightHand.UnstickDistance = profile.unstickDistance;
                    rightHand.StickDistance = profile.stickDistance;
                    rightHand.climbDivider = profile.climbDivider;
                    rightHand.climbForce = profile.climbForce;

                    lastAppliedMovementMode = currentMovementMode;
                }
            }
        }
    }

    [System.Serializable]
    public class ProfileEntry
    {
        public string name;
        public MovementProfile profile;
    }

    [System.Serializable]
    public class MovementProfile
    {
        [Tooltip("How quickly the character responds to movement inputs. Higher numbers make it feel snappier.")]
        public float frequency;

        [Tooltip("How much the movement bounces or wobbles. Higher numbers make it less bouncy and more stable.")]
        public float damping;

        [Tooltip("How fast the character turns or rotates. Higher numbers make turning quicker.")]
        public float rotFrequency;

        [Tooltip("How much the rotation bounces. Higher numbers make it smoother without extra wobbling.")]
        public float rotDamping;

        [Tooltip("The distance where the hand lets go of a surface while moving or climbing.")]
        public float unstickDistance;

        [Tooltip("The distance where the hand grabs onto a surface for moving or gripping.")]
        public float stickDistance;

        [Tooltip("A number that helps balance climbing speed or force (usually set to 2).")]
        public int climbDivider;

        [Tooltip("The push strength used when climbing to move up or along walls (usually set to 0).")]
        public float climbForce;

        public MovementProfile(
            float frequency, 
            float damping, 
            float rotFrequency, 
            float rotDamping, 
            float unstickDistance, 
            float stickDistance, 
            int climbDivider = 2,
            float climbForce = 0f)
        {
            this.frequency = frequency;
            this.damping = damping;
            this.rotFrequency = rotFrequency;
            this.rotDamping = rotDamping;
            this.unstickDistance = unstickDistance;
            this.stickDistance = stickDistance;
            this.climbDivider = climbDivider;
            this.climbForce = climbForce;
        }
    }
}