using Photon.Voice.Unity;
using Photon.Pun;
using UnityEngine;

public class BlendShapeTalk : MonoBehaviour
{
    [Header("This script is protected by copyright law. @shad0w_dev 2024")]

    [Tooltip("Set this to the Renderer you have the talking blend shape on")]
    public SkinnedMeshRenderer head;
    public int blendWeightIndex;

    public float output;
    bool doTheShit;

    private void Awake()
    {
        if (localRecorder())
        {
            Debug.Log("Photon Recorder Found!");
            doTheShit = true;
        }

        else
        {
            Debug.Log("Could not find Photon Recorder!");
            doTheShit = false;
        }
    }

    void Update()
    {
        if (doTheShit) output = localRecorder().LevelMeter.CurrentPeakAmp * 100;

        head.SetBlendShapeWeight(blendWeightIndex, output);
    }

    Recorder localRecorder()
    {
        return FindObjectOfType<Recorder>();
    }
}
