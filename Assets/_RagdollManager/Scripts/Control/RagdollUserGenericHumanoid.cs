// © 2016 Mario Lelas
using UnityEngine;

namespace MLSpace
{
    /// <summary>
    /// implements ragdoll methods and humanoid get up animations on generic avatar
    /// </summary>
    [RequireComponent(typeof(RagdollManager))]
    public class RagdollUserGenericHumanoid : MonoBehaviour, IRagdollUser
    {
        public Transform OrientTransform;

        private RagdollManager m_Ragdoll;       // ragdoll manager reference
        private Bounds m_Bounds;                // bounds of ragdoll user
        private CapsuleCollider m_Capsule;      // bound capsule of ragdoll user
        private Rigidbody m_Rigidbody;          // rigid body of ragdoll user
        private Animator m_Animator;            // animator component
        private bool m_Initialized = false;     // is compoenent initialized ?
        private Collider[] m_Colliders;         // for calculating bounds

        /// <summary>
        /// IRagdollUser interface
        /// gets and sets ignore hits flag
        /// </summary>
        public bool IgnoreHit { get; set; }

        /// <summary>
        /// gets bounds of character
        /// </summary>
        public Bounds Bound { get { calculateBounds(); return m_Bounds; } }

        /// <summary>
        /// IRagdollInterface
        /// gets reference to ragdoll manager component
        /// </summary>
        public RagdollManager RagdollManager { get { return m_Ragdoll; } }



        /// <summary>
        /// initialize component
        /// </summary>
        public void Initialize()
        {
            if (m_Initialized) return;

            m_Capsule = GetComponent<CapsuleCollider>();
            if (!m_Capsule) { Debug.LogWarning("Cannot find capsule collider"); return; }

            m_Rigidbody = GetComponent<Rigidbody>();
            if (!m_Rigidbody) { Debug.LogWarning("Cannot find rigidbody"); return; }

            m_Animator = GetComponent<Animator>();
            if (!m_Animator) { Debug.LogError("Cannot find animator component."); return; }

            m_Ragdoll = GetComponent<RagdollManager>();
            if (!m_Ragdoll) { Debug.LogError("cannot find 'RagdollManager' component."); return; }

            Collider col = GetComponent<Collider>();
            if (!col) { Debug.LogError("object cannot be null."); return; }

            m_Bounds = col.bounds;

            // setup important ragdoll events

            // event that will fire when hit
            m_Ragdoll.OnHit = () =>
            {
                m_Rigidbody.velocity = Vector3.zero;
                m_Rigidbody.isKinematic = true;
                m_Capsule.enabled = false;
            };

            // allow movement when transitioning to animated
            m_Ragdoll.OnStartTransition = () =>
            {
                /* 
                    Enable simulating root motion if 
                    character is not in full ragdoll to
                    make character not freeze on place when hit.
                    Otherwise root motion will interfere with getting up animation.
                */
                if (!m_Ragdoll.IsFullRagdoll && !m_Ragdoll.IsGettingUp)
                {
                    m_Rigidbody.isKinematic = false;
                    m_Capsule.enabled = true;
                }
                if (m_Ragdoll.IsFullRagdoll)
                {
                    bool upwards = OrientTransform.forward.y > 0.0f;
                    if (upwards)
                    {
                        m_Animator.CrossFade("GetUpFrontGen", 0.0f);
                        Vector3 _up = OrientTransform.up;
                        _up.y = 0.0f;
                        transform.forward = -_up;
                    }
                    else
                    {
                        m_Animator.CrossFade("GetUpBackGen", 0.0f);
                        Vector3 _up = OrientTransform.up;
                        _up.y = 0.0f;
                        transform.forward = _up;
                    }
                }
            };

            // event that will be last fired ( when full ragdoll - on get up, when hit reaction - on blend end 
            m_Ragdoll.LastEvent = () =>
            {
                m_Rigidbody.isKinematic = false;
                m_Capsule.enabled = true;
            };

            // event that will be fired when ragdoll counter reach event time
            m_Ragdoll.RagdollEventTime = 3.0f;
            m_Ragdoll.OnTimeEnd = () =>
            {
                m_Ragdoll.BlendToMecanim();
            };

            IgnoreHit = false;


            m_Initialized = true;
        }

        // Use this for initialization
        void Start()
        {
            Initialize();
        }


        // unity update method
        void Update()
        {
#if DEBUG_INFO
            if (!m_Initialized)
            {
                Debug.LogError("component not initialized.");
                return;
            }
#endif
            if (Input.GetKeyDown(KeyCode.F))
            {
                m_Ragdoll.StartRagdoll();
            }
            if (Input.GetKeyDown(KeyCode.G))
            {
                m_Ragdoll.BlendToMecanim();
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                bool strafe = m_Animator.GetBool("Strafe");
                m_Animator.SetBool("Strafe", !strafe);
            }
        }

#if DEBUG_INFO

        void OnDrawGizmosSelected()
        {
            calculateBounds();
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(m_Bounds.center, m_Bounds.size);
        }
#endif

        /// <summary>
        /// IRagdollUser interface
        /// start hit reaction flag
        /// </summary>
        /// <param name="hitParts">hit body parts</param>
        /// <param name="hitForce">hit velocity</param>
        public void StartHitReaction(int[] hitParts, Vector3 hitForce)
        {
#if DEBUG_INFO
            if (!m_Initialized)
            {
                Debug.LogError("component not initialized.");
                return;
            }
#endif
            m_Ragdoll.StartHitReaction(hitParts, hitForce);
        }


        /// <summary>
        /// IRagdollUser interface
        /// starts full ragdoll method
        /// </summary>
        /// <param name="bodyParts">hit parts</param>
        /// <param name="bodyPartForce">force on hit parts</param>
        /// <param name="overallForce">force on all parts</param>
        public void StartRagdoll(int[] bodyParts, Vector3 bodyPartForce, Vector3 overallForce)
        {
#if DEBUG_INFO
            if (!m_Initialized)
            {
                Debug.LogError("component not initialized.");
                return;
            }
#endif
            m_Ragdoll.StartRagdoll(bodyParts, bodyPartForce, overallForce);
        }

        // calculate bounds by combining all ragdoll colliders
        private void calculateBounds()
        {
            if (!m_Ragdoll) m_Ragdoll = GetComponent<RagdollManager>();
#if DEBUG_INFO
            if(!m_Ragdoll) { Debug.LogError("Cannot find RagdollManager component."); return; }
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
