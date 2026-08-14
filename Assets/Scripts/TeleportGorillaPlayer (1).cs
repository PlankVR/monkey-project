using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Teleports the local player to Teleport Location.
//
// This is not triggered by a collider. Call Teleport() from a UnityEvent -
// e.g. drop this object into a PawScanner's On Unlocked () list and pick
// TeleportGorillaPlayer -> Teleport().
//
// Objects To Enable are turned ON and Objects To Disable are turned OFF
// at the moment the teleport starts. Both lists are restored to their
// previous state once the teleport finishes.
public class TeleportGorillaPlayer : MonoBehaviour
{
    [Header("Teleport")]
    [Tooltip("Where the player's HEAD ends up. The rig is offset to match.")]
    public Transform TeleportLocation;
    [Tooltip("Seconds to wait before the teleport, and again after it, before movement is handed back.")]
    public float WaitTime = 0.25f;

    [Header("Objects")]
    [Tooltip("These get enabled when the teleport starts, and disabled again when it finishes.")]
    public List<GameObject> ObjectsToEnable = new List<GameObject>();
    [Tooltip("These get disabled when the teleport starts, and enabled again when it finishes.")]
    public List<GameObject> ObjectsToDisable = new List<GameObject>();

    [Header("Audio")]
    public AudioSource TeleportSound;

    private bool isTeleporting;

    // Player.Instance is only set once the rig's Awake has run, which is not
    // guaranteed to be before this script's Start - so resolve it on demand,
    // with a scene lookup as a fallback.
    private static GorillaLocomotion.Player GetPlayer()
    {
        GorillaLocomotion.Player player = GorillaLocomotion.Player.Instance;

        if (player == null)
            player = FindObjectOfType<GorillaLocomotion.Player>();

        return player;
    }

    // Hook this up to PawScanner's On Unlocked () event.
    public void Teleport()
    {
        if (isTeleporting)
            return;

        if (TeleportLocation == null)
        {
            Debug.LogWarning("TeleportGorillaPlayer: TeleportLocation is not assigned.", this);
            return;
        }

        StartCoroutine(TPWD());
    }

    private IEnumerator TPWD()
    {
        isTeleporting = true;

        if (TeleportSound != null)
            TeleportSound.Play();

        SetActive(ObjectsToEnable, true);
        SetActive(ObjectsToDisable, false);

        yield return new WaitForSeconds(WaitTime);

        GorillaLocomotion.Player player = GetPlayer();

        if (player == null)
        {
            Debug.LogWarning("TeleportGorillaPlayer: no GorillaLocomotion.Player found in the scene.", this);
        }
        else
        {
            // Freeze locomotion across the teleport so a hand that was mid-grab
            // can't drag the rig back toward where it just came from.
            bool wasMovementDisabled = player.disableMovement;
            player.disableMovement = true;

            // TeleportTo handles the rest: it offsets the rig so the head lands
            // on target, zeroes velocity, and resets the hand followers and
            // velocity history.
            player.TeleportTo(TeleportLocation.position);

            yield return new WaitForSeconds(WaitTime);

            player.disableMovement = wasMovementDisabled;
        }

        // Put both lists back how they were
        SetActive(ObjectsToEnable, false);
        SetActive(ObjectsToDisable, true);

        isTeleporting = false;
    }

    private static void SetActive(List<GameObject> objects, bool state)
    {
        if (objects == null)
            return;

        foreach (GameObject go in objects)
        {
            if (go != null)
                go.SetActive(state);
        }
    }
}