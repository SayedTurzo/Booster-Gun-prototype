// © 2015 Mario Lelas


using UnityEngine;

namespace MLSpace
{
    /// <summary>
    /// derived cball projectile class 
    /// </summary>
    public class SoapBallProjectile : BallProjectile
    {
        public float up_force = 1.0f;

        public static void Setup(SoapBallProjectile ball)
        {
            /*
            On hit start lifting character up by adding extra force on all bodyparts and set ragdoll event time delegate to fire 
            in 5 sec. then start another ragdoll removing extra force to make character fall down again.
            also set another timed event to fire getting up animation
            */

            ball.OnHit = () =>
            {
                if (ball.HitInfo.hitObject)
                {
                    RagdollManager rman = null;
                    IRagdollUser ragdollUser = null;


                    ragdollUser = ball.HitInfo.hitObject.GetComponent<IRagdollUser>();
                    if (ragdollUser == null )
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


                    Vector3 boundsSize = ragdollUser.Bound.size ;
                    float max = boundsSize.x;
                    if (boundsSize.y > max) max = boundsSize.y;
                    if (boundsSize.z > max) max = boundsSize.z;

                    ball.transform.localScale = new Vector3(max, max, max);
                    ball.RigidBody.isKinematic = true;
                    ball.SphereCollider.isTrigger = true;
                    ball.RigidBody.detectCollisions = false;
                    ball.transform.position = ragdollUser.Bound.center; 
                    ball.transform.SetParent(rman.RootTransform, true);

                    rman.StartRagdoll(null, null/*Vector3.zero*/, Vector3.zero, true);

                    Vector3 v = new Vector3(0.0f, ball.up_force, 0.0f);
                    for (int i = 0; i < (int)BodyParts.BODY_PART_COUNT; i++)
                    {
                        RagdollManager.BodyPartInfo b = rman.getBodyPartInfo(i);
                        b.extraForce = v;
                    }

                    ragdollUser.IgnoreHit = true;

                    rman.RagdollEventTime = 3.0f;
                    rman.OnTimeEnd = () =>
                    {
                        for (int i = 0; i < (int)BodyParts.BODY_PART_COUNT; i++)
                        {
                            RagdollManager.BodyPartInfo b = rman.getBodyPartInfo(i);
                            b.extraForce = Vector3.zero;
                        }

                        rman.StartRagdoll(null, Vector3.zero, Vector3.zero, true);

                        BallProjectile.DestroyBall(ball);

                        rman.RagdollEventTime = 5.0f;
                        rman.OnTimeEnd = () =>
                        {
                            rman.BlendToMecanim();
                        };
                        ragdollUser.IgnoreHit = false;
                    };
                }
            };
        }
    } 
}
