// © 2015 Mario Lelas
using UnityEngine;
using System.Collections;

namespace MLSpace
{
    /// <summary>
    /// class derived from BallProjectile
    /// adds additional force upon impact
    /// </summary>
    public class HarpoonBallProjectile : BallProjectile
    {
        /// <summary>
        /// additional force. Multiplies hit strength upon impact.
        /// </summary>
        [Tooltip ("Additional force. Multiplies hit strength upon impact.")]
        public float force;

        // unity physics on collision enter
        void OnCollisionEnter(Collision _collision)
        {
            // stop and disable if hit object is not within colliding layers 
            if(!Utils.DoesMaskContainsLayer (collidingLayers,_collision .collider .gameObject .layer ))
            {
                this.State = ProjectileStates.Done;
                this.RigidBody.velocity = Vector3.zero;
                this.RigidBody.isKinematic = true;
                this.RigidBody.detectCollisions = false;
                this.SphereCollider.enabled = false;
            }
        }

        public static void Setup(HarpoonBallProjectile ball)
        {
            /*
           On hit increase hit force to single bodypart and set ragdoll event time delegate to fire 
           in 5 sec.  to fire getting up animation
           */

            ball.OnHit = () =>
            {
                if (ball.HitInfo.hitObject)
                {
                    RagdollManager rman = null;
                    Collider col = null;
                    IRagdollUser ragdollUser = null;

                    ragdollUser = ball.HitInfo.hitObject.GetComponent<IRagdollUser>();
                    if (ragdollUser == null)
                    {
#if DEBUG_INFO
                        Debug.LogError("Ball::OnHit cannot find ragdoll user object on " +
                            ball.HitInfo.hitObject.name + ".");
#endif
                        return;
                    }

                    rman = ragdollUser.RagdollManager;

                    if (!rman)
                    {
#if DEBUG_INFO
                        Debug.LogError("Ball::OnHit cannot find RagdollManager component on " +
                            ball.HitInfo.hitObject.name + ".");
#endif
                        return;
                    }
                    if (!rman.AcceptHit)
                    {
                        BallProjectile.DestroyBall(ball);
                        return;
                    }
                    if (ragdollUser.IgnoreHit)
                    {
                        BallProjectile.DestroyBall(ball);
                        return;
                    }

                    col = ball.HitInfo.collider;
                    if (!col)
                    {
#if DEBUG_INFO
                        Debug.Log("Ball::OnHit cannot find collider component on " +
                            ball.HitInfo.hitObject.name + ".");
#endif
                        return;
                    }

                    ball.RigidBody.isKinematic = true;
                    ball.SphereCollider.isTrigger = true;
                    ball.RigidBody.detectCollisions = false;
                    ball.transform.position = col.bounds.center;
                    ball.transform.SetParent(col.transform);

#if DEBUG_INFO
                    if (ball.HitInfo.bodyPartIndices  == null)
                    {
                        Debug.LogError("object cannot be null.");
                        return;
                    }
#endif

                    float strength = ball.CurrentHitStrength * ball.force;
                    rman.StartRagdoll(ball.HitInfo.bodyPartIndices, ball.HitInfo.hitDirection * strength,
                        ball.HitInfo.hitDirection * strength * 0.45f);

                    ragdollUser.IgnoreHit = true;
                    rman.RagdollEventTime = 3.0f;
                    rman.OnTimeEnd = () =>
                    {
                        rman.BlendToMecanim();
                        ragdollUser.IgnoreHit = false;
                    };
                }
            };
        }
    } 

}
