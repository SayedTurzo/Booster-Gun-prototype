// © 2015 Mario Lelas
using UnityEngine;

namespace MLSpace
{
    /// <summary>
    /// class that assignes different ammunition to characters
    /// when triggered
    /// </summary>
    public class BallTrigger : MonoBehaviour
    {
        /// <summary>
        /// ammunition prefab to set
        /// </summary>
        public BallProjectile ball_prefab;

        /// <summary>
        /// layers to interact with
        /// </summary>
        [Tooltip ("Layers to interact with")]
        public LayerMask collidingWith;


        // unity initialization method
        void Start()
        {
            if (!ball_prefab)
            {
                Debug.LogError("ball_prefab not assigned. " + this.name);
                return;
            }
        }


        // unity update method
        void Update()
        {
            Vector3 pos = transform.position;
            float sinVal = Mathf.Cos(Time.time) * 0.01f ;
            pos.y += sinVal;
            transform.position = pos;
        }

        // unity physics on trigger enter method
        void OnTriggerEnter(Collider _collider)
        {
#if DEBUG_INFO
            if (!ball_prefab)
            {
                Debug.LogError("No ball prefab assigned.");
                return;
            }
#endif

            ShootScript shootScript = _collider.gameObject.GetComponent<ShootScript>();
            if (shootScript == null)
                return;
            shootScript.ProjectilePrefab = ball_prefab;
        }
    }



}