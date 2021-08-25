// © 2016 Mario Lelas
using UnityEngine;

namespace MLSpace
{
    /// <summary>
    /// constrol horse script
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Animator))]
    public class ThirdPersonHorse : MonoBehaviour
    {
        public PhysicMaterial maxFrictionMaterial;      // max friction material
        public PhysicMaterial zeroFrictionMaterial;     // zero friction material
        public Collider[] m_Colliders;                  // move colliders
        public Transform ForwardRayPosition;            // forward transform 
        public Transform BackRayPosition;               // back transform

        [SerializeField]
        private float m_MoveSpeedMultiplier = 1f;       // move speed multiplier

        [HideInInspector]
        public bool simulateRootMotion = true;          // simulate root motion in OnAnimatorMove method


        private Animator m_Animator;                // reference to animator component
        private Rigidbody m_Rigidbody;              // reference to rigidbody component

        private float m_ForwardAmount;                  // forward amount
        private float m_DampTime = 0.3f;                // animator blend tree blend time
        private bool m_Initialized = false;         // is class initialized ?
        private bool m_DisableMove = false;         // disable move flag
        private Vector3 m_MoveWS = Vector3.zero;        // current move velocity world space
        private bool m_DisableAngleControl = false;


        /// <summary>
        /// gets rigidbody reference
        /// </summary>
        public Rigidbody RigidBody { get { return m_Rigidbody; } }

        /// <summary>
        /// gets move colliders
        /// </summary>
        public Collider[] Colliders { get { return m_Colliders; } }

        /// <summary>
        /// gets animator reference
        /// </summary>
        public Animator Animator { get { return m_Animator; } }

        /// <summary>
        /// gets and sets angle control disable flag
        /// </summary>
        public bool DisableAngleControl { get { return m_DisableAngleControl; } set { m_DisableAngleControl = value; } }
        
        /// <summary>
        /// gets and sets disable moving
        /// </summary>
        public bool DisableMove { get { return m_DisableMove; } set { m_DisableMove = value; } }

        /// <summary>
        /// gets current move velocity
        /// </summary>
        public Vector3 MoveVelocity { get { return m_MoveWS; } }

        /// <summary>
        /// gets and sets forward amount
        /// </summary>
        public float ForwardAmount { get { return m_ForwardAmount; } set { m_ForwardAmount = value; } }


        // Use this for initialization
        void Start()
        {
            Initialize();
        }


        // control by script
        void OnAnimatorMove()
        {
#if DEBUG_INFO
            if (!m_Rigidbody) { Debug.LogError("object cannot be null."); return; }
            if (!m_Animator) { Debug.LogError("object cannot be null."); return; }
#endif

            // we implement this function to override the default root motion.
            // this allows us to modify the positional speed before it's applied.
            if (Time.deltaTime > 0 && simulateRootMotion)
            {
                float multiplier = m_MoveSpeedMultiplier;
                Vector3 v = (m_Animator.deltaPosition * multiplier) / Time.deltaTime;

                if (m_Rigidbody.useGravity) v.y = m_Rigidbody.velocity.y;

                m_Rigidbody.velocity = v;
            }
            horseAngleControl();
        }

        /// <summary>
        /// initialize all
        /// </summary>
        public void Initialize()
        {
            if (m_Initialized) return;

            m_Animator = GetComponent<Animator>();
            if (!m_Animator) { Debug.LogError("object cannot be null"); return; }

            m_Rigidbody = GetComponent<Rigidbody>();
            if (!m_Rigidbody) { Debug.LogError("object cannot be null"); return; }

            m_Rigidbody.constraints =
                RigidbodyConstraints.FreezeRotationY
                | RigidbodyConstraints.FreezeRotationZ ;


            if (m_Colliders == null)
            {
                Debug.LogError("Colliders cannot be null.");
                return;
            }

            m_Initialized = true;
        }


        /// <summary>
        /// main character move function
        /// </summary>
        /// <param name="move">move velocity</param>
        /// <param name="crouch">crouch flag</param>
        /// <param name="jump">jump flag</param>
        /// <param name="rotateDir">body rotation direction</param>
        /// <param name="headLookPos">head look position</param>
        /// <param name="turn">turn amount nullable</param>
        public void Move(
            Vector3 move,
            Vector3 rotateDir,
            Vector3 headLookPos, 
            float? side = null
            )
        {
#if DEBUG_INFO
            if (!m_Initialized)
            {
                Debug.LogError("component not initialized.");
                return;
            }
#endif

            if (!m_DisableMove)
            {


                if (rotateDir.magnitude > 1f) rotateDir.Normalize();

                if (move.magnitude > 1f) move.Normalize();
                m_MoveWS = move;

                // convert the world relative moveInput vector into a local-relative
                // turn amount and forward amount required to head in the desired
                // direction.
                Vector3 localMove = transform.InverseTransformDirection(move);
                m_ForwardAmount = localMove.z;

                setFriction(ref move);
                //horseAngleControl();
            }
            // send input and other state parameters to the animator
            updateAnimator();
        }

        public float angularSpeed = 0.15f;

        // control angle by shoot ray from front and back positions
        private void horseAngleControl()
        {
            if (m_DisableAngleControl) return;


            float distFront = 0.0f;
            float distBack = 0.0f;
            Vector3 angVelocity = Vector3.zero;

            int mask = ~LayerMask.GetMask("NPCLayer", "ColliderInactiveLayer", "ColliderLayer");
            Ray ray = new Ray(ForwardRayPosition.position, Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 12.0f, mask))
            {
                distFront = hit.distance;
            }
            if (distFront > 0.3f)
            {
                angVelocity += new Vector3(angularSpeed, 0, 0);
            }

            ray.origin = BackRayPosition.position;
            if (Physics.Raycast(ray, out hit, 12.0f, mask))
            {
                distBack = hit.distance;
            }
            if (distBack > 0.3f)
            {
                angVelocity += new Vector3(-angularSpeed, 0.0f, 0.0f);
            }
            m_Rigidbody.rotation *= Quaternion.Euler(angVelocity);
        }

        // update animator values
        private void updateAnimator()
        {
            m_Animator.SetFloat("Forward", m_ForwardAmount, m_DampTime, Time.deltaTime);
        }

        // set friction material
        private void setFriction(ref Vector3 move)
        {
            // set friction to low or high, depending on if we're moving
            if (m_Animator.GetFloat("Forward") == 0)
            {
                // when not moving this helps prevent sliding on slopes:
                //m_Capsule.material = maxFrictionMaterial;
                for (int i = 0; i < m_Colliders.Length; i++)
                    m_Colliders[i].material = maxFrictionMaterial;
            }
            else
            {
                // but when moving, we want no friction:
                //m_Capsule.material = zeroFrictionMaterial;
                for (int i = 0; i < m_Colliders.Length; i++)
                    m_Colliders[i].material = zeroFrictionMaterial;
            }
        }
    }
}
