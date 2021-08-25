// © 2016 Mario Lelas
using UnityEngine;

namespace MLSpace
{
    /// <summary>
    /// Put this script on any object you want to shoot from.
    /// Point it towards direction to which you want to shoot.
    /// </summary>
    public class ShootScript : MonoBehaviour
    {
        [Tooltip ("Shoot projectile prefab")]
        public BallProjectile ProjectilePrefab;

        [Tooltip ("Projectile fire position and shoot direction.")]
        public Transform FireTransform;

        [Tooltip("Owner character of shooter script.")]
        public GameObject Owner;

        [HideInInspector ]
        public  BallProjectile m_CurrentBall = null;        // current ball reference

        [HideInInspector]
        public bool m_DisableShooting = false;              // disable shooting flag


        void Start()
        {

            if (!ProjectilePrefab) { Debug.LogError("projectile prefab not assigned."); return; }
            if (!FireTransform) { Debug.LogError("fire transform not assign."); return; }

        }

        // create ball projectile function
        protected virtual void createBall()
        {
            m_CurrentBall = BallProjectile.CreateBall(ProjectilePrefab, FireTransform, Owner);
            if (!m_CurrentBall.Initialize()) { Debug.LogError("cannot initialize ball projectile"); return; }

            m_CurrentBall.OnLifetimeExpire = BallProjectile.DestroyBall;
            m_CurrentBall.SphereCollider.isTrigger = false;
            m_CurrentBall.RigidBody.isKinematic = false;
            m_CurrentBall.RigidBody.detectCollisions = true;

            BallProjectile thisBall = m_CurrentBall;


            if (thisBall is SoapBallProjectile)
                SoapBallProjectile.Setup(thisBall as SoapBallProjectile);
            else if (thisBall is RocketBallProjectile)
                RocketBallProjectile.Setup(thisBall as RocketBallProjectile);
            else if (thisBall is HarpoonBallProjectile)
                HarpoonBallProjectile.Setup(thisBall as HarpoonBallProjectile);
            else
                InflatableBall.Setup(thisBall as InflatableBall);
        }

        // scale current ball
        protected virtual void scaleBall()
        {
            if (!m_CurrentBall) { return; }
            if (!(m_CurrentBall is InflatableBall)) return;
            (m_CurrentBall as InflatableBall).inflate();
        }

        // shoot current ball
        protected virtual void fireBall()
        {
            if (!m_CurrentBall) { return; }

            Vector3 force = FireTransform.forward * m_CurrentBall.hitStrength;
            m_CurrentBall.RigidBody.velocity = force;
            m_CurrentBall.transform.position = FireTransform.position;
            m_CurrentBall.State = BallProjectile.ProjectileStates.Fired;
        }
    } 

}
