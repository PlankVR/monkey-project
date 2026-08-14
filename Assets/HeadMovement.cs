using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadMovement : MonoBehaviour
{
    public Transform headTransform;
    public Transform bodyTransform;
    public Vector3 bodyOffset = new Vector3(0, -0.43f, 0);

    [Header("Body Follow Settings")]
    public float bodyFollowSpeed = 5f;        // How fast the body catches up
    public float rotationThreshold = 30f;     // How many degrees before body starts following

    private float currentBodyYaw;

    void Start()
    {
        currentBodyYaw = bodyTransform.eulerAngles.y;
    }

    void LateUpdate()
    {
        // Position always follows head
        bodyTransform.position = headTransform.position + bodyOffset;

        // Get head's current Y rotation
        float headYaw = headTransform.eulerAngles.y;

        // Find the difference between head and body rotation
        float yawDifference = Mathf.DeltaAngle(currentBodyYaw, headYaw);

        // Only start rotating body if head has turned past the threshold
        if (Mathf.Abs(yawDifference) > rotationThreshold)
        {
            float targetYaw = headYaw - Mathf.Sign(yawDifference) * rotationThreshold;
            currentBodyYaw = Mathf.LerpAngle(currentBodyYaw, targetYaw, Time.deltaTime * bodyFollowSpeed);
        }

        // Apply only Y rotation to body, keep X and Z as they were
        Vector3 bodyEuler = bodyTransform.eulerAngles;
        bodyTransform.rotation = Quaternion.Euler(bodyEuler.x, currentBodyYaw, bodyEuler.z);
    }
}