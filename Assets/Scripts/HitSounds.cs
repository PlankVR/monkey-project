using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PuchinHitSounds : MonoBehaviour
{
    [Header("Remake Of Capuchins HitSounds")]
    public AudioSource hand;
    public List<HitSound> hitSounds = new List<HitSound>();
    [Header("Configuration")]
    public float coolDown = .45f;
    [Header("Tracks Players Velocity And Changes Volume")]
    public bool velocityBased;
    [Header("Dont Have To Add This If Your Not Added Velocity Based")]
    public Rigidbody player;

    private float lastHit;


    [Serializable]
    public class HitSound{
        [Header("< HitSound >")]
        public string name;
        public AudioClip[] audioClips;
    }

    void OnTriggerEnter(Collider other)
    {
        if(Time.time-lastHit<coolDown) return;
        lastHit = Time.time;
        foreach(HitSound hitsound in hitSounds){
            if(other.CompareTag(hitsound.name)){
                int randomHitsound = UnityEngine.Random.Range(0, hitsound.audioClips.Length);
                hand.clip = hitsound.audioClips[randomHitsound];
                hand.Play();
                if(player != null){
                                    if(velocityBased){
                    hand.volume = player.linearVelocity.magnitude;
                }
                }
            }
        }
    }
}
