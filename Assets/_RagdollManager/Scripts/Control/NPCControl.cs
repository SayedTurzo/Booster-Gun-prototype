// © 2015 Mario Lelas
using UnityEngine;

namespace MLSpace
{
    /// <summary>
    /// npc control script using IRagdoll interface
    /// </summary>
    [RequireComponent(typeof(RagdollManager))]
    [RequireComponent(typeof(ThirdPersonCharacter))]
    public class NPCControl : MonoBehaviour, IRagdollUser
    {


#region Fields

        /// <summary>
        /// additial chest transform rotation on y axis
        /// </summary>
        [Tooltip("Additional chest transform rotation around y axis.")]
        public float additionalYRotation = 10.0f;

        /// <summary>
        /// additional chest transform rotation around x axis
        /// </summary>
        [Tooltip("Additional chest transform rotation around x axis.")]
        public float additionalXRotation = 25.0f;


        /// <summary>
        /// transform that will rotate towards camera direction
        /// </summary>
        [Tooltip("Chest transform will rotate towards camera direction.")]
        public Transform ChestTransform;

        /// <summary>
        /// npc aim transform
        /// </summary>
        [Tooltip ("Npc will aim at this transform.")]
        public Transform AimTransform;

        /// <summary>
        /// npc chase transform
        /// </summary>
        [Tooltip("Npc will chase this transform")]
        public Transform ChaseTransform;

        private RagdollManager m_Ragdoll;                   // this npc ragdoll manager component
        private ThirdPersonCharacter m_Character;           // this npc character component
        private ShootScript m_ShootScript = null;           // script for shooting


        private Vector3 m_Move = Vector3.zero;              // npc movement velocity
        private Vector3 m_HeadLookPos = Vector3.zero;       // npc head look position
        private Vector3 m_BodyLookDir = Vector3.zero;       // npc body look direction
        private Vector3 m_CurrentDest = Vector3.zero;       // current destination position
        private float m_CurrentDist = float.MaxValue;       // current distance to destination
        private bool m_IdlePose = true;                     // stand idle flag for testing

        private bool m_Aiming = true;                       // aiming flag
        private bool m_initialized = false;                 // is class initialized 

        private UnityEngine.AI.NavMeshPath m_path;                         // nav mesh path info class
 
        private Bounds m_Bounds;                            // bounds
        private Collider[] m_Colliders;                     // used for calculating bounds

#endregion




#region Properties

        /// <summary>
        /// IRagdollUser interface
        /// gets and sets falg to ignore hits
        /// </summary>
        public bool IgnoreHit { get; set; }

        /// <summary>
        /// gets bounds of character
        /// </summary>
        public Bounds Bound { get { calculateBounds(); return m_Bounds; } }

        /// <summary>
        /// IRagdollUser interface
        /// gets reference to ragdoll manager script
        /// </summary>
        public RagdollManager RagdollManager { get { return m_Ragdoll; } }

#endregion

        /// <summary>
        /// initialize class
        /// </summary>
        public void Initialize()
        {
            if (m_initialized) return;

            m_Character = GetComponent<ThirdPersonCharacter>();
            if (!m_Character) { Debug.LogError("cannot find 'ThirdPersonCharacter' component."); return; }
            m_Character.Initialize();
            if (!m_Character.Initialized) { Debug.LogError("cannot initialize 'ThirdPersonCharacter' component."); return; }

            m_Ragdoll = GetComponent<RagdollManager>();
            if (!m_Ragdoll) { Debug.LogError("cannot find 'RagdollManager' component."); return; }
            m_Ragdoll.Initialize();
            if (!m_Ragdoll.Initialized) { Debug.LogError("cannot initialize 'RagdollManager' component."); return; }

            m_ShootScript = GetComponent<ShootScript>();
            if (!m_ShootScript) { Debug.LogError("Cannot find 'ShootScript' component."); return; }

            m_Bounds = m_Character.Capsule.bounds;


            // setup important ragdoll events

            // event that will fire when hit
            m_Ragdoll.OnHit = () =>
            {
                m_Character.simulateRootMotion = false;
                m_Character.DisableMove = true;
                BallProjectile.DestroyBall(m_ShootScript.m_CurrentBall);
                m_Character.RigidBody.velocity = Vector3.zero;

                m_Character.RigidBody.detectCollisions = false;
                m_Character.RigidBody.isKinematic = true;
                m_Character.Capsule.enabled = false;
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
                if (!m_Ragdoll.IsFullRagdoll && !m_Ragdoll.IsGettingUp )
                {
                    m_Character.simulateRootMotion = true;
                    m_Character.RigidBody.detectCollisions = true;
                    m_Character.RigidBody.isKinematic = false;
                    m_Character.Capsule.enabled = true;
                }
            };

            // event that will be last fired ( when full ragdoll - on get up, when hit reaction - on blend end 
            m_Ragdoll.LastEvent = () =>
            {
                m_Character.simulateRootMotion = true;
                m_Character.DisableMove = false;

                m_Character.RigidBody.detectCollisions = true;
                m_Character.RigidBody.isKinematic = false;
                m_Character.Capsule.enabled = true;
            };

            // event that will be fired when ragdoll counter reach event time
            // this time we will revive him -
            // set event time to 3 and fire transition to animator 
            m_Ragdoll.RagdollEventTime = 4.0f;
            m_Ragdoll.OnTimeEnd = () =>
            {
                m_Ragdoll.BlendToMecanim();
            };

            if (m_IdlePose)
            {
                m_Character.Animator.SetBool("Aim", false);
            }
            else
            {
                m_Character.Animator.SetBool("Aim", true);
            }
            m_Character.Animator.SetBool("Idle", m_IdlePose);

            m_path = new UnityEngine.AI.NavMeshPath();

            
            if (!ChestTransform)
            {
                Debug.Log("ChestTransform not assigned. Checking from humanoid setup.");
                ChestTransform = m_Character.Animator.GetBoneTransform(HumanBodyBones.Chest);
                if (!ChestTransform)
                {
                    Debug.LogError("cannot find humanoid bone 'Chest'." + this.name );
                    return;
                }
            }

            IgnoreHit = false;
            
            if (!AimTransform) { Debug.LogWarning ("AimTransform not assigned."); ; }
            if (!ChaseTransform) { Debug.LogWarning("ChaseTransform not assigned."); ; }

            calculateBounds();

            m_initialized = true;
        }


        /// <summary>
        /// IRagdollUser interface
        /// start hit reaction flag
        /// </summary>
        /// <param name="hitParts">hit body parts</param>
        /// <param name="hitForce">hit velocity</param>
        public void StartHitReaction(int[] hitParts,Vector3 hitForce)
        {
#if DEBUG_INFO
            if (!m_initialized) { Debug.LogError("component not initialized."); return; }
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
            if (!m_initialized) { Debug.LogError("component not initialized."); return; }
#endif
            m_Ragdoll.StartRagdoll(bodyParts, bodyPartForce, overallForce);
        }


#region Unity Methods

        // Unity start method
        void Start()
        {
            Initialize();

        }

        // Unity LateUpdate method
        void LateUpdate()
        {
#if DEBUG_INFO
            if (!m_initialized) { Debug.LogError("component not initialized."); return; }
#endif
            // go to / from idle state on T
            if (Input.GetButtonDown ("Idle"))
            {
                    m_IdlePose = !m_IdlePose;
                    m_Character.Animator.SetBool("Idle", m_IdlePose);
                    if (m_IdlePose)
                    {
                        m_Character.Animator.SetFloat("Forward", 0.0f);
                        m_Character.Animator.SetFloat("Strafe", 0.0f);
                        m_Character.Animator.SetBool("Aim", false);
                    }
                    m_Move = Vector3.zero;
                    BallProjectile.DestroyBall(m_ShootScript .m_CurrentBall);
            }

            m_ShootScript.m_DisableShooting = true;

            // start aiming only in animated and not idle state
            m_Aiming = false;
            if (m_Ragdoll.State == RagdollManagerHum.RagdollState.Animated && !m_IdlePose)
            {
                m_Aiming = true;
            }

                m_Character.Animator.SetBool("Aim", m_Aiming);
                m_Character.Animator.SetBool("Idle", m_IdlePose);

            if (m_Ragdoll.State != RagdollManagerHum.RagdollState.Animated)
            {
                return;
            }
            if (m_IdlePose)
            {
                return;
            }

            if (m_Aiming)
            {
                m_ShootScript.m_DisableShooting = false;
                if (AimTransform)
                {
                    Quaternion additional_rot = Quaternion.AngleAxis(additionalYRotation, transform.up);
                    additional_rot *= Quaternion.AngleAxis(additionalXRotation, transform.right);
                    Vector3 pos = transform.forward;
                    pos = additional_rot * pos;
                    m_HeadLookPos = AimTransform.position + pos;

                    ChestTransform.LookAt(m_HeadLookPos);
                }
#if DEBUG_INFO
                else
                {
                    Debug.LogWarning("Aim transform is missing.");
                }
#endif
                if (ChaseTransform)
                {
                    if (isOnDestination())
                    {
                        Vector2 rndSphere = Random.insideUnitCircle * 10.0f;
                        Vector3 dest = ChaseTransform.position + new Vector3(rndSphere.x, 0.0f, rndSphere.y);

                        UnityEngine.AI.NavMeshHit nhit;
                        if (UnityEngine.AI.NavMesh.SamplePosition(dest, out nhit, 120.0f, UnityEngine.AI.NavMesh.AllAreas))
                        {
                            m_CurrentDest.x = nhit.position.x;
                            m_CurrentDest.z = nhit.position.z;
                        }
#if DEBUG_INFO
                        else Debug.Log("destination outside bounds on " + this.name);
#endif

                    }

                    if (m_Aiming) m_BodyLookDir = ChaseTransform.position - transform.position;
                    else m_BodyLookDir = Vector3.zero;
                    m_Character.Move(m_Move, m_BodyLookDir.normalized, Vector3.zero);
                }
#if DEBUG_INFO
                else
                {
                    Debug.LogWarning("Chase transform missing.");
                }
#endif
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

        #endregion

        // go to destination while avoiding player
        private bool isOnDestination()
        {
#if DEBUG_INFO
            if (!m_initialized) { Debug.LogError("component not initialized."); return false; }
#endif

            if (!UnityEngine.AI.NavMesh.CalculatePath(transform.position, m_CurrentDest, UnityEngine.AI.NavMesh.AllAreas, m_path))
            {
#if DEBUG_INFO
                Debug.LogWarning("Calculate path failed.");
#endif
                return false;
            }

            if (m_path.corners.Length < 2)
            {
#if DEBUG_INFO
                Debug.LogWarning("Calculate path failed. Not enough corners.");
#endif
                return false;
            }


            // GO TO TARGET
            Vector3 toTarget = m_path .corners [1] - transform.position;
            m_CurrentDist = toTarget.magnitude;
            if (m_CurrentDist < 2.0f)
            {
                return true;
            }

            // AROUND PLAYER
            Vector3 currentDirection = toTarget.normalized;

            Vector3 toPlayer = transform.position - ChaseTransform.position;
            float distToPlayer = toPlayer.magnitude;
            if(distToPlayer < 2.6f)
            {
                currentDirection += toPlayer.normalized ;
            }

            m_Move = currentDirection.normalized;
            return false;
        }



        // calculate bounds based on all colliders sizes
        private void calculateBounds()
        {
            if (!m_Ragdoll) m_Ragdoll = GetComponent<RagdollManager>();
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
