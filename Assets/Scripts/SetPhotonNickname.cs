using UnityEngine;
using Photon.Pun;
using System.Text;

#if OCULUS_PLATFORM
using Oculus.Platform;
using Oculus.Platform.Models;
#endif

public class SetPhotonNickname : MonoBehaviour
{
    private const string FallbackPrefix = "HLUMBDY";
    private const string SavedNameKey = "SavedPhotonNickname";

    void Start()
    {
#if OCULUS_PLATFORM
        try
        {
            Core.AsyncInitialize().OnComplete(OnOculusInitialized);
        }
        catch
        {
            UseSavedOrCreateFallback();
        }
#else
        UseSavedOrCreateFallback();
#endif
    }

#if OCULUS_PLATFORM
    private void OnOculusInitialized(Message msg)
    {
        if (msg.IsError)
        {
            UseSavedOrCreateFallback();
            return;
        }

        Users.GetLoggedInUser().OnComplete(OnUserReceived);
    }

    private void OnUserReceived(Message<User> msg)
    {
        if (msg.IsError || msg.Data == null || string.IsNullOrWhiteSpace(msg.Data.OculusID))
        {
            UseSavedOrCreateFallback();
            return;
        }

        PhotonNetwork.NickName = msg.Data.OculusID;
        Debug.Log("Using Meta username: " + PhotonNetwork.NickName);
    }
#endif

    private void UseSavedOrCreateFallback()
    {
        if (PlayerPrefs.HasKey(SavedNameKey))
        {
            PhotonNetwork.NickName = PlayerPrefs.GetString(SavedNameKey);
            Debug.Log("Loaded saved nickname: " + PhotonNetwork.NickName);
            return;
        }

        string newName = FallbackPrefix + RandomDigits(4);

        PhotonNetwork.NickName = newName;

        PlayerPrefs.SetString(SavedNameKey, newName);
        PlayerPrefs.Save();

        Debug.Log("Generated new nickname: " + newName);
    }

    private string RandomDigits(int length)
    {
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < length; i++)
        {
            sb.Append(Random.Range(0, 10));
        }

        return sb.ToString();
    }
}