// © 2016 Mario Lelas
using UnityEngine;

namespace MLSpace
{
    /// <summary>
    /// Horse constrol script
    /// </summary>
    [RequireComponent(typeof(ThirdPersonHorse))]
    public class HorseController : MonoBehaviour, IRagdollUser
    {
        [Tooltip("Helper for transform orientation when exiting ragdoll mode.")]
        public Transform OrientTransform;

        private ThirdPersonHorse m_Horse;       // reference to third person horse controller
        private RagdollManager m_Ragdoll;       // reference to ragdoll manager         
        private Bounds m_Bounds;                // bounds of object
        private Bounds m_OldBounds;             // for storing values when colliders are inactive
        private Collider[] m_Colliders;         // for calculating bounds
        private bool m_Initialized = false;     // is component initialized ?


        /// <summary>
        /// gets current bounds around character
        /// </summary>
        public Bounds Bound { get { calculateBounds(); return m_Bounds; } }

        /// <summary>
        /// IRagdoll interface implementation
        /// gets and sets ignore hit flag. 
        /// </summary>
        public bool IgnoreHit { get; set; }

        /// <summary>
        /// IRagdoll interface implementation
        /// gets reference to ragdoll manager
        /// </summary>
        public RagdollManager RagdollManager { get { return m_Ragdoll; } }


        /// <summary>
        /// initialize component
        /// </summary>
        public void Initialize()
        {
            if (m_Initialized) return;

            m_Horse = GetComponent<ThirdPersonHorse>();
            m_Ragdoll = GetComponent<RagdollManager>();

            if (!OrientTransform) { Debug.LogError("OrientTransform not assigned."); return; }

            m_Ragdoll.OnHit = () =>
            {
                m_Horse.simulateRootMotion = false;
                m_Horse.DisableMove = true;
                m_Horse.RigidBody.velocity = Vector3.zero;

                m_Horse.RigidBody.isKinematic = true;
                m_Horse.RigidBody.detectCollisions = false;
                foreach (Collider c in m_Horse.Colliders)
                    c.enabled = false;
                m_Horse.DisableAngleControl = true;
            };

            // allow movement when transitioning to animated
            m_Ragdoll.OnStartTransition = () =>
            {
                /* 
                    Enable simulating root motion on transition  if 
                    character is not in full ragdoll to
                    make character not freeze on place when hit.
                    Otherwise root motion will interfere with getting up animation.
                */
                if (!m_Ragdoll.IsFullRagdoll && !m_Ragdoll.IsGettingUp)
                {
                    m_Horse.simulateRootMotion = true;
                    m_Horse.RigidBody.detectCollisions = true;
                    m_Horse.RigidBody.isKinematic = false;
                    foreach (Collider c in m_Horse.Colliders)
                        c.enabled = true;
                }
                if (m_Ragdoll.IsFullRagdoll)
                {
                    m_Horse.Animator.CrossFade("HorseGetUp", 0.0f, 0, 0.0f);
                    Vector3 forw = OrientTransform.forward;
                    forw.y = 0.0f;
                    transform.forward = forw;
                }
            };

            // event that will be last fired ( when full ragdoll - on get up, when hit reaction - on blend end 
            m_Ragdoll.LastEvent = () =>
            {
                m_Horse.DisableAngleControl = false;
                m_Horse.simulateRootMotion = true;
                m_Horse.DisableMove = false;

                m_Horse.RigidBody.detectCollisions = true;
                m_Horse.RigidBody.isKinematic = false;
                foreach (Collider c in m_Horse.Colliders)
                    c.enabled = true;
            };


            calculateBounds();

            m_Initialized = true;
        }

        /// <summary>
        /// IRagdoll interface implementation
        /// Starts hit rection precedure
        /// </summary>
        /// <param name="hitParts"></param>
        /// <param name="hitForce"></param>
        public void StartHitReaction(int[] hitParts, Vector3 hitForce)
        {
            m_Ragdoll.StartHitReaction(hitParts, hitForce);
        }

        /// <summary>
        /// IRagdoll interface implementation
        /// Starts ragdoll procedure
        /// </summary>
        /// <param name="bodyParts">hit body parts</param>
        /// <param name="bodyPartForce">force on hit parts</param>
        /// <param name="overallForce">force on all parts</param>
        public void StartRagdoll(int[] bodyParts, Vector3 bodyPartForce, Vector3 overallForce)
        {
            m_Ragdoll.StartRagdoll(bodyParts, bodyPartForce, overallForce);
        }

#region Unity Methods

        // unity start method
        void Start()
        {
            Initialize();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
                m_Ragdoll.BlendToMecanim();

            // MOVING HORSE
            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.Alpha1))
                if (Input.GetKey(KeyCode.LeftShift))
                    move = transform.forward * 1.0f;
                else
                    move = transform.forward * 0.5f;
            if (Input.GetKey(KeyCode.Alpha2))
                move = -transform.forward * 0.5f;
            m_Horse.Move(move, Vector3.zero, Vector3.zero);
        }

#if DEBUG_INFO

        void OnDrawGizmosSelected()
        {
            calculateBounds();
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(m_Bounds.center, m_Bounds.size);
        }

#endif 


#endregion

        // calculate bounds by combining all ragdoll colliders
        private void calculateBounds()
        {
            if (!m_Ragdoll) m_Ragdoll = GetComponent<RagdollManager>();
#if DEBUG_INFO
            if (!m_Ragdoll) { Debug.LogError("Cannot find RagdollManager component."); return; }
#endif
            if (m_Colliders == null)
                m_Colliders = m_Ragdoll.RagdollBones[0].GetComponentsInChildren<Collider>();

            Vector3 vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 vMax = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);
            for (int i = 0; i < m_Colliders.Length; i++)
            {
                vMin = Vector3.Min(vMin, m_Colliders[i].bounds.min);
                vMax = Vector3.Max(vMax, m_Colliders[i].bounds.max);
            }
            m_Bounds.SetMinMax(vMin, vMax);
        }
    }

}
