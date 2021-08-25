// © 2015 Unity Technologies modified by Mario Lelas
using UnityEngine;

namespace MLSpace
{
    /// <summary>
    /// main class for manipulating third person characters
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(CapsuleCollider))]
	[RequireComponent(typeof(Animator))]
    public class ThirdPersonCharacter : MonoBehaviour
    {

#region Fields


        [SerializeField]
        private float m_MovingTurnSpeed = 360;          // turn speed when moving
        [SerializeField]
        private float m_StationaryTurnSpeed = 180;      // turn speed when stationary
        [SerializeField]
        private float m_MoveSpeedMultiplier = 1f;       // move speed multiplier
        [SerializeField]
        private float m_AnimatorSpeed = 1.0f;           // animator speed
        [HideInInspector]
        public bool simulateRootMotion = true;          // simulate root motion in OnAnimatorMove method

        public PhysicMaterial maxFrictionMaterial;      // max friction material
        public PhysicMaterial zeroFrictionMaterial;     // zero friction material
            
        // REQUIRED COMPONENTS
        private Rigidbody m_Rigidbody;                  // reference to rigid body
        private Animator m_Animator;                    // reference to animator
        private CapsuleCollider m_Capsule;              // reference to capsule collider

        private float m_SideAmount;                 // turn / strafe amount
        private float m_ForwardAmount;              // forward amount
        private bool m_Initialized = false;         // is class initialized ?
        private bool m_DisableMove = false;         // disable move flag
        private Vector3 m_currentBodyDirection;     // The current direction where the character body is looking
        private Vector3 m_MoveWS = Vector3.zero;        // current move velocity world space
        private float m_DampTime = 0.1f;                // animator blend tree blend time
        private float m_StrafeAmount = 0.0f;            // strafe value in strafing mode
        

#endregion

#region Properties

        /// <summary>
        /// returns true if class is initialized
        /// </summary>
        public bool Initialized { get { return m_Initialized; } }

        /// <summary>
        /// gets and sets animator blend tree blend time
        /// </summary>
        public float DampTime { get { return m_DampTime; } set { m_DampTime = value; } }

        /// <summary>
        /// gets current move velocity
        /// </summary>
        public Vector3 MoveVelocity { get { return m_MoveWS; } }

        /// <summary>
        /// gets reference to rigid body
        /// </summary>
        public Rigidbody RigidBody { get { return m_Rigidbody; } }

        /// <summary>
        /// gets reference to animator
        /// </summary>
        public Animator Animator { get { return m_Animator; } }

        /// <summary>
        /// gets reference to capsule collider
        /// </summary>
        public CapsuleCollider Capsule { get { return m_Capsule; } }

        /// <summary>
        /// gets and sets disable moving
        /// </summary>
        public bool DisableMove { get { return m_DisableMove; } set { m_DisableMove = value; } }

        /// <summary>
        /// gets and sets turn amount
        /// </summary>
        public float SideAmount { get { return m_SideAmount; } set { m_SideAmount = value; } }

        /// <summary>
        /// gets and sets forward amount
        /// </summary>
        public float ForwardAmount { get { return m_ForwardAmount; } set { m_ForwardAmount = value; } }

        /// <summary>
        /// gets and sets current animator speed
        /// </summary>
        public float AnimatorSpeed { get { return m_AnimatorSpeed; } set { m_AnimatorSpeed = value; } }

#endregion

        

#region UNITY_METHODS

        private void Start()
        {
            // initialize all
            Initialize();
        }
        
        private void OnAnimatorMove()
        {
#if DEBUG_INFO
            if (!m_Rigidbody) { Debug.LogError("object cannot be null"); return; }
            if (!m_Animator) { Debug.LogError("object cannot be null"); return; }
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
        }

#endregion

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

            m_Capsule = GetComponent<CapsuleCollider>();
            if (!m_Capsule) { Debug.LogError("object cannot be null"); return; }


            m_Rigidbody.constraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationY |
                RigidbodyConstraints.FreezeRotationZ;

            m_Initialized = true;
        }

        /// <summary>
        /// apply extra rotation for faster turning
        /// </summary>
        /// <param name="extraSpeed">extra applied speed</param>
        public void ApplyExtraTurnRotation(float extraSpeed = 1.0f)
        {
#if DEBUG_INFO
            if (!m_Initialized)
            {
                Debug.LogError("component not initialized.");
                return;
            }
#endif
            if (m_DisableMove) return;

            // help the character turn faster (this is in addition to root rotation in the animation)
            float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount) *
                extraSpeed;
            transform.Rotate(0, m_SideAmount * turnSpeed * Time.deltaTime, 0);
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
        public void Move(Vector3 move,
            Vector3 rotateDir, Vector3 headLookPos, float? side = null)
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
                m_currentBodyDirection = rotateDir;

                if (move.magnitude > 1f) move.Normalize();
                m_MoveWS = move;
                
                // convert the world relative moveInput vector into a local-relative
                // turn amount and forward amount required to head in the desired
                // direction.
                Vector3 localMove = transform.InverseTransformDirection(move);
                Vector3 localRotationDir = transform.InverseTransformDirection(m_currentBodyDirection);
                m_ForwardAmount = localMove.z;
                m_StrafeAmount = localMove.x;
                m_SideAmount = Mathf.Atan2(localRotationDir.x, localRotationDir.z);

                ApplyExtraTurnRotation();

                setFriction(ref move);
            }
            // send input and other state parameters to the animator
            updateAnimator();


        }

        /// <summary>
        /// update animator parameters
        /// </summary>
        private void updateAnimator()
        {
#if DEBUG_INFO
            if (!m_Animator) { Debug.LogError("object cannot be null"); return; }
#endif
            m_Animator.SetFloat("Forward", m_ForwardAmount , m_DampTime, Time.deltaTime);
            m_Animator.SetFloat("Strafe", m_StrafeAmount, m_DampTime, Time.deltaTime);
            m_Animator.speed = m_AnimatorSpeed;
        }

        /// <summary>
        /// set friction material
        /// </summary>
        /// <param name="move"></param>
        private void setFriction(ref Vector3 move)
        {
#if DEBUG_INFO
            if (!m_Capsule )
            {
                Debug.LogError("object cannot be null.");
                return;
            }
#endif
            // set friction to low or high, depending on if we're moving
            if (move.magnitude == 0)
            {
                // when not moving this helps prevent sliding on slopes:
                m_Capsule.material = maxFrictionMaterial;
            }
            else
            {
                // but when moving, we want no friction:
                m_Capsule.material = zeroFrictionMaterial;
            }
        }

    }
}
