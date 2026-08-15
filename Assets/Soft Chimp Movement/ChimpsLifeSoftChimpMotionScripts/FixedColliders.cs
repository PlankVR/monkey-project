using UnityEngine;

namespace SoftChimpMotion
{
    public class FixedColliders : MonoBehaviour
    {
        [Header("SCRIPT BY VOUIE.")]
        public Collider[] colliders;

        void Start()
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                for (int j = i + 1; j < colliders.Length; j++)
                {
                    Physics.IgnoreCollision(colliders[i], colliders[j]);
                }
            }
        }
    }
}
