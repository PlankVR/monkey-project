using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using easyInputs;
using SoftChimpMotion;

public class GrabPhysics : MonoBehaviour
{
    [Header("EasyInput Hand")]
    public EasyHand handController = EasyHand.LeftHand;

    [Header("Grabbing Settings")]
    public float radius = 0.1f;
    
    public AudioSource AudioSourceComponent;
    public AudioClip DropItem;
    public AudioClip GrabItem;

    public LayerMask grabLayer;
    public Transform grabParent;

    // public NetworkRunner networkRunner;

    [Header("Debug Options")]
    public bool isGrabbing;
    public bool DebugGrab;

    private FixedJoint fixedJoint;
    private Rigidbody grabbedRigidbody;
    private Transform grabbedTransform;
    private MovementCollision movementForObject;

    private void FixedUpdate()
    {
        if (DebugGrab)
        {
            AttachObjectDebug();
            return;
        }

        bool gripPressed = EasyInputs.GetGripButtonDown(handController);
        bool gripReleased = EasyInputs.GetGripButtonFloat(handController) < 0.1f;

        if (gripPressed && !isGrabbing)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, radius, grabLayer, QueryTriggerInteraction.Ignore);
            if (hits.Length > 0)
            {
                grabbedRigidbody = hits[0].attachedRigidbody;
                grabbedTransform = hits[0].transform;

                if (grabbedTransform != null && grabParent != null)
                    grabbedTransform.SetParent(grabParent, true);

                if (grabbedRigidbody != null)
                {
                    fixedJoint = gameObject.AddComponent<FixedJoint>();
                    fixedJoint.autoConfigureConnectedAnchor = false;
                    fixedJoint.connectedBody = grabbedRigidbody;
                    fixedJoint.connectedAnchor = grabbedRigidbody.transform.InverseTransformPoint(transform.position);
                }

                movementForObject = grabbedTransform.GetComponent<MovementCollision>();
                if (movementForObject != null)
                    movementForObject.enabled = true;

                isGrabbing = true;
                AudioSourceComponent.PlayOneShot(GrabItem);
            }
        }
        else if (gripReleased && isGrabbing)
        {
            ReleaseObject();
            AudioSourceComponent.PlayOneShot(DropItem);
        }
    }

    public void AttachObjectDebug()
    {
        if (isGrabbing) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, grabLayer, QueryTriggerInteraction.Ignore);
        if (hits.Length > 0)
        {
            grabbedRigidbody = hits[0].attachedRigidbody;
            grabbedTransform = hits[0].transform;

            if (grabbedTransform != null && grabParent != null)
                grabbedTransform.SetParent(grabParent, true);

            if (grabbedRigidbody != null)
            {
                fixedJoint = gameObject.AddComponent<FixedJoint>();
                fixedJoint.autoConfigureConnectedAnchor = false;
                fixedJoint.connectedBody = grabbedRigidbody;
                fixedJoint.connectedAnchor = grabbedRigidbody.transform.InverseTransformPoint(transform.position);
            }

            movementForObject = grabbedTransform.GetComponent<MovementCollision>();
            if (movementForObject != null)
                movementForObject.enabled = true;

            isGrabbing = true;
            AudioSourceComponent.PlayOneShot(GrabItem);
        }
    }

    public void DetachObjectDebug()
    {
        ReleaseObject();
        AudioSourceComponent.PlayOneShot(DropItem);
    }

    private void ReleaseObject()
    {
        if (grabbedTransform != null)
        {
            grabbedTransform.SetParent(null, true);

            if (movementForObject != null)
                movementForObject.enabled = false;

            grabbedTransform = null;
            movementForObject = null;
        }

        if (fixedJoint != null)
        {
            Destroy(fixedJoint);
            fixedJoint = null;
        }

        grabbedRigidbody = null;
        isGrabbing = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
