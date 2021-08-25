// © 2015 Mario Lelas

using UnityEngine;

namespace MLSpace
{
    /// <summary>
    /// control player script
    /// </summary>
    [RequireComponent(typeof(RagdollManagerHum))]
    [RequireComponent(typeof(ThirdPersonCharacter))]
    public class PlayerControl : MonoBehaviour, IRagdollUser
    {

#region Fields

        /// <summary>
        /// camera controller that orbits around player
        /// </summary>
        public OrbitCameraController m_Camera;

        /// <summary>
        /// additional chest rotation on y axis
        /// </summary>
        [Tooltip("Additional chest rotation on y axis.")]
        public float additionalYRotation = 10.0f;

        /// <summary>
        /// additional chest rotation around x axis
        /// </summary>
        [Tooltip("Additional chest rotation on x axis.")]
        public float additionalXrotation = 25.0f;

        private RagdollManager m_Ragdoll = null;            // ragdoll manager reference
        private ThirdPersonCharacter m_Character = null;    // character reference
        private Transform m_Chest = null;                   // chest transform reference
        private Transform m_Spine;                          // hips or spine transform

        private ShootScript m_ShootScript = null;           // script for shooting
        private bool m_Aiming = true;                       // aim flag
        private bool m_initialized = false;                 // is class initialized ?

        // chest aim lerping for smooth transitions
        private float m_AimLerpValue = 0.0f;
        private const float m_AimLerpMax = 0.2f;
        private Vector3 m_AimLerpStart = Vector3.zero;
        private bool m_AimLerpFlag = false;
        private bool m_OldAimValue = false;

        private Bounds m_Bounds;
        private Collider[] m_Colliders; // for calculating bounds 


#endregion


#region Properties

        /// <summary>
        /// gets ragdoll manager current state
        /// </summary>
        public RagdollManager.RagdollState State
        {
            get
            {
#if DEBUG_INFO
                if (!m_Ragdoll) { Debug.LogError("object cannot be null."); return RagdollManagerHum.RagdollState.Animated; }
#endif
                return m_Ragdoll.State;
            }
        }

        /// <summary>
        /// gets chest transform
        /// </summary>
        public Transform Chest { get { return m_Chest; } }

        /// <summary>
        /// gets bounds of character
        /// </summary>
        public Bounds Bound { get { calculateBounds(); return m_Bounds; } }

        /// <summary>
        /// IRagdollUser interface
        /// gets reference to ragdoll manager script
        /// </summary>
        public RagdollManager RagdollManager { get { return m_Ragdoll; } }

        /// <summary>
        /// gets and sets flag to ignore hits
        /// </summary>
        public bool IgnoreHit { get; set; } 

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
            if (m_Ragdoll == null) { Debug.LogError("cannot find component 'RagdollManager'"); return; }

            m_ShootScript = GetComponent<ShootScript>();
            if (!m_ShootScript) { Debug.LogError("Cannot find 'ShootScript' component."); return; }

            m_Bounds = m_Character.Capsule.bounds;


            // setup important ragdoll events

            // event that will fire when hit
            m_Ragdoll.OnHit = () =>
            {
                m_Character.simulateRootMotion = false;
                m_Character.DisableMove = true;
                m_Character.RigidBody.velocity = Vector3.zero;

                m_Character.RigidBody.detectCollisions = false;
                m_Character.RigidBody.isKinematic = true;
                m_Character.Capsule.enabled = false;

                if (m_Ragdoll.IsFullRagdoll)
                {
                    m_Camera.SetTargetOffset(Vector3.up * 0.5f);
                    m_Camera.SwitchTargets(m_Spine);
                }
                BallProjectile.DestroyBall(m_ShootScript.m_CurrentBall);
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

                if (m_Ragdoll.IsFullRagdoll)
                {
                    m_Camera.SetTargetOffset(Vector3.zero);
                    if (m_Camera.defaultCameraTarget == null) { Debug.LogError("m_Camera.defaultCameraTarget cannot be null"); }
                    m_Camera.SwitchTargets(m_Camera.defaultCameraTarget);
                }
                m_Character.RigidBody.detectCollisions = true;
                m_Character.RigidBody.isKinematic = false;
                m_Character.Capsule.enabled = true;

            };

            // event that will be fired when ragdoll counter reach event time
            m_Ragdoll.RagdollEventTime = 3.0f;
            m_Ragdoll.OnTimeEnd = () =>
            {
                m_Ragdoll.BlendToMecanim();
            };




            m_Chest = m_Character.Animator.GetBoneTransform(HumanBodyBones.Chest);
            if (!m_Chest) { Debug.LogError("cannot find human body transform chest."); return; }

            m_Spine = m_Character.Animator.GetBoneTransform(HumanBodyBones.Hips);
            if (!m_Spine) { Debug.LogError("cannot find human body transform hips."); return; }

            m_Character.DampTime = 0.000f;

            m_Character.Animator.SetBool("Aim", true);

            IgnoreHit = false;

            calculateBounds();

            m_initialized = true;
        }
       
        /// <summary>
        /// IRagdollUser interface
        /// start hit reaction flag
        /// </summary>
        /// <param name="hitParts">hit body parts</param>
        /// <param name="hitForce">hit velocity</param>
        public void StartHitReaction(int[] hitParts,Vector3 hitForce )
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

        // unity start
        void Start()
        {
            Initialize();
        }

        // unity lateupdate
        void LateUpdate()
        {
#if DEBUG_INFO
            if (!m_initialized) { Debug.LogError("component not initialized"); return; }
#endif

            m_ShootScript.m_DisableShooting = true;
            m_Aiming = false;
            if (m_Ragdoll.State == RagdollManagerHum.RagdollState.Animated)
                m_Aiming = true;

            m_Character.Animator.SetBool("Aim", m_Aiming);


            if (m_Aiming)
            {
                m_ShootScript.m_DisableShooting = false;

                if (m_OldAimValue != m_Aiming)
                {
                    m_AimLerpValue = 0.0f;
                    m_AimLerpStart = m_Chest.forward * 100.0f;
                    m_AimLerpFlag = true;
                }

                Quaternion additional_rot = Quaternion.AngleAxis(additionalXrotation, transform.right);
                additional_rot *= Quaternion.AngleAxis(additionalYRotation, transform.up);
                Vector3 pos = m_Camera.transform.forward;
                pos = additional_rot * pos;
                Vector3 aimPosition = transform.position + pos * 100;
                // little up
                aimPosition = aimPosition + Vector3.up * 40.0f;

                if (m_AimLerpFlag)
                {
                    m_AimLerpValue += Time.deltaTime;
                    if (m_AimLerpValue > m_AimLerpMax)
                    {
                        m_AimLerpValue = m_AimLerpMax;
                        m_AimLerpFlag = false;
                    }
                    float val = m_AimLerpValue / m_AimLerpMax;
                    Vector3 AIMPOS = Vector3.Lerp(m_AimLerpStart, aimPosition, val);
                    m_Chest.LookAt(AIMPOS);
                }
                else
                {
                    m_Chest.LookAt(aimPosition);
                }
                inputs();
            }


            m_OldAimValue = m_Aiming;
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

        // input function
        private void inputs()
        {
#if DEBUG_INFO
            if (!m_initialized) { Debug.LogError("component not initialized"); return; }
#endif


            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            //if (ProjectilePrefab is InflatableBall)
            //{
            //    if (Input.GetButtonDown("Fire1"))
            //    {
            //        createBall();
            //    }
            //    if (Input.GetButton("Fire1"))
            //    {
            //        scaleBall();
            //    }
            //    if (Input.GetButtonUp("Fire1"))
            //    {
            //        fireBall();
            //    }
            //}
            //else
            //{
            //    if (Input.GetButtonDown("Fire1"))
            //    {
            //        createBall();
            //        fireBall();
            //    }
            //}


            Vector3 forward = transform .forward;
            Vector3 right = transform.right;
            Vector3 move = v * forward + h * right;
            Vector3 bodyLookDir = Vector3.zero;

            if (m_Camera)
            {
                forward = m_Camera.transform.forward;
                right = m_Camera.transform.right;
                move = v * forward + h * right;
                bodyLookDir = Vector3.zero;
                bodyLookDir = m_Camera.transform.forward;
            }
            //if (move.magnitude > 1f) move.Normalize();
            m_Character.Move(move, bodyLookDir, Vector3.zero);
        }

//        // create ball projectile function
//        private void createBall()
//        {
//#if DEBUG_INFO
//            if (!m_initialized) { Debug.LogError("component not initialized"); return; }
//#endif
//            m_CurrentBall = BallProjectile.CreateBall(ProjectilePrefab, FireTransform, this.gameObject);
//            if (!m_CurrentBall.Initialize()) { Debug.LogError("cannot initialize ball projectile"); return; }

//            m_CurrentBall.OnLifetimeExpire = BallProjectile.DestroyBall;
//            m_CurrentBall.SphereCollider.isTrigger = false;
//            m_CurrentBall.RigidBody.isKinematic = false;
//            m_CurrentBall.RigidBody.detectCollisions = true;

//            BallProjectile thisBall = m_CurrentBall;


//            if (thisBall is SoapBallProjectile)
//                SoapBallProjectile.Setup(thisBall as SoapBallProjectile);
//            else if (thisBall is RocketBallProjectile)
//                RocketBallProjectile.Setup(thisBall as RocketBallProjectile);
//            else if (thisBall is HarpoonBallProjectile)
//                HarpoonBallProjectile.Setup(thisBall as HarpoonBallProjectile);
//            else
//                InflatableBall.Setup(thisBall as InflatableBall);
//        }

//        // scale current ball
//        private void scaleBall()
//        {
//#if DEBUG_INFO
//            if (!m_initialized) { Debug.LogError("component not initialized"); return; }
//#endif

//            if (!m_CurrentBall) { return; }
//            if (!(m_CurrentBall is InflatableBall))return;
//            (m_CurrentBall as InflatableBall).inflate();
//        }

//        // shoot current ball
//        private void fireBall()
//        {
//#if DEBUG_INFO
//            if (!m_initialized) { Debug.LogError("component not initialized"); return; }
//#endif
//            if (!m_CurrentBall) { return; }

//            Vector3 force = FireTransform.forward * m_CurrentBall.hitStrength;
//            m_CurrentBall.RigidBody.velocity = force;
//            m_CurrentBall.State = BallProjectile.ProjectileStates.Fired;
//        }



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