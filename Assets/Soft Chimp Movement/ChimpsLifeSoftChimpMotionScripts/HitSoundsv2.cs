using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace SoftChimpMotion
{
    public class HitSoundsv2 : MonoBehaviour
    {
        public AudioClip[] water, stone, tree, grass, metal, glass, snow, dirt, carpet, wood, sand, untaggedHitsounds;
        public AudioSource audioSource;
        public bool LeftController;
        private float hapticWaitSeconds = 0.05f;
        Dictionary<string, AudioClip[]> audio;
        private bool isTouchingSlip = false;

        void Start()
        {
            audio = new Dictionary<string, AudioClip[]> {
                { "Water", water },
                { "Stone", stone },
                { "Tree", tree },
                { "Grass", grass },
                { "Metal", metal },
                { "Glass", glass },
                { "Snow", snow },
                { "Dirt", dirt },
                { "Carpet", carpet },
                { "Wood", wood },
                { "Sand", sand },
            };
            // if it's null, then we just don't throw an error instead of saying, HEY THE HITSOUNDS AREN"T ASSIGNED PLEASE FUCKING ASSIGN IT!!!!!
            if (audioSource == null){}
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Untagged"))
            {
                PlayRandomSound(untaggedHitsounds, audioSource);
                StartVibration(LeftController, 0.15f, 0.15f);
            }
            else if (!other.gameObject.CompareTag("HandTag") && !other.gameObject.CompareTag("Player"))
            {
                PlayRandomSound(audio[other.gameObject.tag], audioSource);
                StartVibration(LeftController, 0.15f, 0.15f);
            }
            // check if there is a tag that isn't recognized, if it isn't then just don't throw an error.
            else if (other.gameObject.tag == null) {}
        }

        void PlayRandomSound(AudioClip[] audioClips, AudioSource audioSource)
        {
            audioSource.clip = audioClips[Random.Range(0, audioClips.Length)];
            audioSource.Play();
        }

        public void StartVibration(bool forLeftController, float amplitude, float duration)
        {
            StartCoroutine(HapticPulses(forLeftController, amplitude, duration));
        }

        private IEnumerator HapticPulses(bool forLeftController, float amplitude, float duration)
        {
            float startTime = Time.time;
            uint channel = 0u;
            InputDevice device = ((!forLeftController) ? InputDevices.GetDeviceAtXRNode(XRNode.RightHand) : InputDevices.GetDeviceAtXRNode(XRNode.LeftHand));
            while (Time.time < startTime + duration)
            {
                device.SendHapticImpulse(channel, amplitude, hapticWaitSeconds);
                yield return new WaitForSeconds(hapticWaitSeconds * 0.9f);
            }
        }
    }
}
