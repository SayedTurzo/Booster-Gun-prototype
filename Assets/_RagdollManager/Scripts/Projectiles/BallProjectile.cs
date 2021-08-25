// © 2015 Mario Lelas

//#if DEBUG_INFO
//#define DEBUG_DRAW
//#endif

using UnityEngine;
using System.Collections.Generic;

namespace MLSpace
{
    /// <summary>
    /// void delegate that takes BallProjectile class as parameter
    /// </summary>
    /// <param name="thisBall"></param>
    public delegate void BallProjectileFunc(BallProjectile thisBall);

    /// <summary>
    ///  Base Projectile class 
    ///  Checking for collision between set collider layer by 
    ///  checking spherecast from last postion to current
    /// </summary>
    [RequireComponent (typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class BallProjectile : MonoBehaviour
    {
        /// <summary>
        /// Information on hit object 
        /// </summary>
        public class HitInformation
        {
            public GameObject hitObject = null;
            public Collider collider = null;
            public int[] bodyPartIndices = null;
            public Vector3 hitDirection = Vector3.zero;
            public float hitStrength = 0.0f;
        }

        /// <summary>
        /// projectile ball counter
        /// </summary>
        public static int BallCount = 0;

        /// <summary>
        /// possible ball states enumeration
        /// </summary>
        public enum ProjectileStates { Ready, Fired, Done };

        /// <summary>
        /// lifetime of projectile
        /// </summary>
        [Tooltip("Lifetime of fire ball projectile.")]
        public float lifetime = 12.0f;

        /// <summary>
        /// hit strength of fire ball projectile
        /// </summary>
        [Tooltip("Hit strength of fire ball projetile.")]
        public float hitStrength = 25.0f;

        /// <summary>
        /// colliding layers of fire ball. Use 'ColliderLayer'
        /// </summary>
        [Tooltip("colliding layers of fire ball.")]
        public LayerMask collidingLayers;



        private ProjectileStates m_State = ProjectileStates.Ready;  // current state of fire ball
        private float m_CurrentLifetime = 0.0f;                 // lifetime of fire ball
        private Vector3? m_LastPosition = null;          // position on previus frame
        private float m_CurrentHitStrength = 25.0f;             // current hit strength of fire ball
        private BallProjectileFunc m_OnLifetimeExpire = null;   // on lifetime expire event
        private SphereCollider m_SphereCollider;                // sphere collider component
        private Rigidbody m_Rigidbody;                          // reference to rigid body 

        
        private GameObject m_Owner;                     // owner character of this fireball ( to ignore hits if wanted)
        private HitInformation m_HitInfo = null;        // information on hit object
        private VoidFunc m_OnHit = null;                // on hit delegate

        /// <summary>
        /// gets information on hit object
        /// </summary>
        public HitInformation HitInfo { get { return m_HitInfo; } }

       /// <summary>
       /// gets and sets on hit event
       /// </summary>
        public VoidFunc OnHit { get { return m_OnHit; } set { m_OnHit = value; } }

        /// <summary>
        /// gets owner object of projectile
        /// </summary>
        public GameObject Owner { get { return m_Owner; } }

        /// <summary>
        /// gets reference to rigidbody component
        /// </summary>
        public Rigidbody RigidBody 
        { 
            get 
            { 
#if DEBUG_INFO
                if (!m_Rigidbody) { Debug.LogError("object cannot be null."); return null; }
#endif
                return m_Rigidbody; 
            } 
        }

        /// <summary>
        /// gets reference to sphere collider component
        /// </summary>
        public SphereCollider SphereCollider { get { return m_SphereCollider; } }

        /// <summary>
        /// gets current life time
        /// </summary>
        public float CurrentLifetime { get { return m_CurrentLifetime; } }
        
        /// <summary>
        /// gets and sets state of fire ball
        /// </summary>
        public ProjectileStates State { get { return m_State; } set { m_State = value; } }

        /// <summary>
        /// gets and sets on lifetime expire event
        /// </summary>
        public BallProjectileFunc OnLifetimeExpire { get { return m_OnLifetimeExpire; } set { m_OnLifetimeExpire = value; } }

        /// <summary>
        /// gets and sets current hit strength
        /// </summary>
        public float CurrentHitStrength { get { return m_CurrentHitStrength; } set { m_CurrentHitStrength = value; } }

        /// <summary>
        /// initialize component
        /// </summary>
        /// <returns>is initialization success</returns>
        public virtual bool Initialize()
        {
            m_SphereCollider = GetComponent<SphereCollider>();
            if (!m_SphereCollider) { Debug.LogError("SphereCollider component missing."); return false; }

            m_Rigidbody = GetComponent<Rigidbody>();
            if (!m_Rigidbody) { Debug.LogError("cannot find 'Rigidbody' component."); return false; }

            m_HitInfo = new HitInformation();

            return true;
        }

        // Unity MonoBehaviour start method
        void Start()
        {
            if (!Initialize()) { Debug.LogError("cannot initialize component."); return; }
        }

        // Unity MonoBehaviour LateUpdate method
        void LateUpdate()
        {
#if DEBUG_INFO
            if (!RigidBody) { Debug.LogError("object cannot be null."); return; }
#endif
            update();
        }

        // virtual update for derived classes
        protected virtual void update()
        {
            Vector3 transformPosition = transform.position;

            // advance lifetime starting from time when fired onwards
            if (m_State != ProjectileStates.Ready)
            {
                m_CurrentLifetime += Time.deltaTime;
                if (m_CurrentLifetime > lifetime)
                {

                    if (m_OnLifetimeExpire != null)
                    {
                        m_OnLifetimeExpire(this);
                    }
                    else
                    {
                        Destroy(this.gameObject);
                    }
                    return;
                }
            }
            
#if DEBUG_DRAW
            positionList.Add(transformPosition);
            radiusList.Add(m_SphereCollider.radius * this.transform.localScale.x);
#endif
            // check for collision only when fired
            if (m_State == ProjectileStates.Fired && m_LastPosition .HasValue )
            {

                // shoot sphere from last position to current 
                // and check if we have a hit

                int mask = collidingLayers;


#if DEBUG_INFO
                if (!m_SphereCollider)
                {
                    Debug.LogError("SphereCollider missing.");
                    return;
                }
#endif

                float radius = m_SphereCollider.radius * this.transform.localScale.x;
                Vector3 difference = transformPosition - m_LastPosition.Value ;
                Vector3 direction = difference.normalized;
                float length = difference.magnitude;
                Vector3 rayPos = m_LastPosition.Value;


                Ray ray = new Ray(rayPos, direction);

                RaycastHit[] hits = Physics.SphereCastAll(ray, radius, length, mask);

                List<int> chosenHits = new List<int>();
                RagdollManager ragMan = null;

                RaycastHit? rayhit = null;

                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit rhit = hits[i];
                    BodyColliderScript bcs = rhit.collider.GetComponent<BodyColliderScript>();
                    if (!bcs)
                    {
#if DEBUG_INFO
                        Debug.LogError("BodyColliderScript missing on " + rhit.collider.name);
#endif
                        continue;
                    }

                    if (!bcs.ParentObject)
                    {
#if DEBUG_INFO
                        Debug.LogError("BodyColliderScript.ParentObject missing on " + rhit.collider.name);
#endif
                        continue;
                    }
                    if (bcs.ParentObject == this.m_Owner)
                    {
                        continue;
                    }

                    if(!ragMan)
                    {
                        ragMan = bcs.ParentRagdollManager;
                        m_HitInfo.hitObject = bcs.ParentObject;
                        m_HitInfo.collider = rhit.collider;
                        m_HitInfo.hitDirection = direction;
                        m_HitInfo.hitStrength = m_CurrentHitStrength;
                        rayhit = rhit;
                    }

                    chosenHits.Add(bcs.index);
                }


                if (hits.Length > 0)
                {
                    if(ragMan)
                    {

                        if (!rayhit.HasValue)
                        {
#if DEBUG_INFO
                            Debug.LogError("object cannot be null.");
#endif
                            return;
                        }

                        Vector3 n = rayhit.Value.normal;
                        Vector3 r = Vector3.Reflect(direction, n);
                        this.transform.position = rayPos + ray.direction *
                            (rayhit.Value.distance - radius);
                        Vector3 vel = r;
                        this.m_Rigidbody.velocity = vel;

                        Vector3 force = direction * m_CurrentHitStrength;
                        m_State = ProjectileStates.Done;

                        m_HitInfo.bodyPartIndices = chosenHits.ToArray();

                        if (m_OnHit != null)
                            m_OnHit();
                        else
                        {
                            ragMan.StartHitReaction(m_HitInfo.bodyPartIndices, force);
                        }
                    }
#if DEBUG_INFO
                    else
                    {
                        BodyColliderScript bcs = hits[0].collider.GetComponent<BodyColliderScript>();
                        if (!bcs)
                            return;
                        if (!bcs.ParentObject)
                            return;
                        if (bcs.ParentObject == this.m_Owner)
                            return;
                        Debug.LogWarning("RagdollUser interface not implemented. " +
                        bcs.ParentObject.name);
                    }
#endif
                }

            }
            m_LastPosition  = transformPosition;
        }

        /// <summary>
        /// Creates ball projectile, assignes owner and increments ball counter
        /// </summary>
        /// <param name="prefab">prefab of projectile</param>
        /// <param name="xform">position and rotation transform</param>
        /// <param name="_owner">owner game object</param>
        /// <returns>created ball projectile</returns>
        public static BallProjectile CreateBall(BallProjectile prefab,Transform xform, GameObject _owner)
        {
#if DEBUG_INFO
            if(!prefab){Debug.LogError("object cannot be null.");return null;}
#endif
            BallCount++;
            if (xform)
            {
                BallProjectile newBall = (BallProjectile)Instantiate(prefab,
                   xform.position,
                   xform.rotation);
                newBall.name = newBall.name + BallProjectile.BallCount;
                newBall.m_Owner = _owner;
                return newBall;
            }
            else
            {
                BallProjectile newBall = (BallProjectile)Instantiate(prefab);
                newBall.name = newBall.name + BallProjectile.BallCount;
                newBall.m_Owner = _owner;
                return newBall;
            }
        }

        /// <summary>
        /// destroys ball projectile and decrements counter
        /// </summary>
        /// <param name="ball"></param>
        public static void DestroyBall(BallProjectile ball)
        {
            if (!ball) return;

            BallProjectile.BallCount--;
            Destroy(ball.gameObject);
        }


#if DEBUG_DRAW
        List<Vector3> positionList = new List<Vector3>();
        List<float> radiusList = new List<float>();
        void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            for(int i = 0;i<positionList .Count-1;i++)
            {
                Gizmos.DrawWireSphere(positionList[i], radiusList[i]);
                Gizmos.DrawLine(positionList[i],positionList[i + 1]);
            }
        }
#endif

    } 
}
