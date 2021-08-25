using UnityEngine;
using System.Collections;

namespace MLSpace
{
    public class NPCShootScript : ShootScript
    {
        /// <summary>
        /// modes for shooting ball
        /// </summary>
        private enum ScaleBallModes { None, CreateBallProjectile, Scale, Release };

        /// <summary>
        /// rate of fire
        /// </summary>
        [Tooltip("Rate of fire.")]
        public float shootInterval = 4.0f;

        private float m_CurrentShootTime = 0.0f;            // current shoot time
        private float m_CurrentShootScaleTarget = 1.0f;     // current projectile ball scale target
        private ScaleBallModes m_ScaleBallMode =
                ScaleBallModes.None;                        // current state when shooting scale balls

        // Update is called once per frame
        void LateUpdate()
        {
#if DEBUG_INFO
            if (!ProjectilePrefab)
            {
                Debug.LogError("ProjectilePrefab cannot be null.");
                return;
            }
#endif

            if (m_DisableShooting) return;

            // shooting procedure
            m_CurrentShootTime += Time.deltaTime;
            if (m_CurrentShootTime >= shootInterval)
            {
                m_CurrentShootScaleTarget = Random.Range(InflatableBall.MinScale, InflatableBall.MaxScale);
                m_ScaleBallMode = ScaleBallModes.CreateBallProjectile;
                m_CurrentShootTime = 0.0f;
            }

            if (ProjectilePrefab is InflatableBall)
            {
                if (m_ScaleBallMode == ScaleBallModes.CreateBallProjectile)
                {
                    createBall();
                }
                else if (m_ScaleBallMode == ScaleBallModes.Scale)
                {
                    float currentBallScale = (m_CurrentBall as InflatableBall).CurrentBallScale;

                    scaleBall();

                    if (currentBallScale >= m_CurrentShootScaleTarget)
                        m_ScaleBallMode = ScaleBallModes.Release;
                }
                else if (m_ScaleBallMode == ScaleBallModes.Release)
                {
                    fireBall();
                }
            }
            else
            {
                if (m_ScaleBallMode == ScaleBallModes.CreateBallProjectile)
                {
                    createBall();
                    fireBall();
                }
            }
            if (m_CurrentBall)
            {
                if (m_CurrentBall.State == BallProjectile.ProjectileStates.Ready)
                    m_CurrentBall.transform.position = FireTransform.position;
            }
        }

        protected override void createBall()
        {
            m_ScaleBallMode = ScaleBallModes.Scale;
            base.createBall();
        }

        protected override void scaleBall()
        {
            if (!m_CurrentBall) { m_ScaleBallMode = ScaleBallModes.None; return; }
            base.scaleBall();
        }

        protected override void fireBall()
        {
            if (!m_CurrentBall) { m_ScaleBallMode = ScaleBallModes.None; return; }

            base.fireBall();
            m_ScaleBallMode = ScaleBallModes.None;
        }
    } 
}
