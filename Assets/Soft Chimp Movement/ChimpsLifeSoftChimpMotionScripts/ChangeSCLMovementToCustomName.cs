using UnityEngine;
using UnityEngine.UI;
using SoftChimpMotion;

public class ChangeSCLMovementToCustomName : MonoBehaviour
{
    public Button targetButton;
    public string customMovementName = "In-between";
    private MotionSettings motionSettings;

    private void Start()
    {
        motionSettings = FindObjectOfType<MotionSettings>();
        if (motionSettings == null) Debug.LogError("MotionSettings not found in the scene.");
        else
        {
            if (targetButton != null) targetButton.onClick.AddListener(SetMovementToSCL);
            else Debug.LogWarning("Button not assigned.");
        }
    }

    public void SetMovementToSCL()
    {
        if (motionSettings != null) motionSettings.SetMovementMode(customMovementName);
        else Debug.LogWarning("MotionSettings is null, cannot set movement mode.");
    }
}
