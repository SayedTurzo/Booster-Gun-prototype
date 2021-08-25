// © 2016 Mario Lelas
using UnityEngine;



namespace MLSpace
{

    public class PlayerShootScript : ShootScript
    {
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

            if (ProjectilePrefab is InflatableBall)
            {
                if (Input.GetButtonDown("Fire1"))
                {
                    createBall();
                }
                if (Input.GetButton("Fire1"))
                {
                    scaleBall();
                }
                if (Input.GetButtonUp("Fire1"))
                {
                    fireBall();
                }
            }
            else
            {
                if (Input.GetButtonDown("Fire1"))
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
    }
}
