using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ToggleTriggerOnGrab : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Collider[] colliders;

    void Awake()
    {
        // Get the XRGrabInteractable component on the same GameObject
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        // Get all colliders on the GameObject and its children
        colliders = GetComponentsInChildren<Collider>();

        // Subscribe to the grab and release events
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    void OnDestroy()
    {
        // Unsubscribe from the grab and release events
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        // Set all colliders to be triggers
        foreach (var collider in colliders)
        {
            collider.isTrigger = true;
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        // Set all colliders to not be triggers
        foreach (var collider in colliders)
        {
            collider.isTrigger = false;
        }
    }
}
