using UnityEngine;
using System.Collections;

namespace MLSpace
{
    public class RocketBallProjectile : BallProjectile
    {
        public float up_force = 18.0f;

        private Transform m_Afterburner;

        public override bool Initialize()
        {
            bool r = base.Initialize();
            if (!r) { Debug.LogError("Failed ti initialize base class."); return false; }

            m_Afterburner = transform.Find("Afterburner");
            if(!m_Afterburner) { Debug.LogError("cannot find child 'Afterburner' " + this.name );return false; }
            m_Afterburner.gameObject.SetActive(false);

            return r;
        }

        public void startAfterburner()
        {
#if DEBUG_INFO
            if (!m_Afterburner) { Debug.LogError("object cannot be null"); return; }
#endif
            m_Afterburner.gameObject.SetActive(true);
            m_Afterburner.transform.LookAt(m_Afterburner.transform.position + Vector3.down);
        }

        protected override void update()
        {
#if DEBUG_INFO
            if (!m_Afterburner) { Debug.LogError("object cannot be null"); return; }
#endif
            base.update();
            m_Afterburner.transform.LookAt(m_Afterburner.transform.position + Vector3.down);
        }

        public static void Setup(RocketBallProjectile ball)
        {
            /*
           On hit start lifting character up by adding extra force to single bodypart and set ragdoll event time delegate to fire 
           in 5 sec. then start another ragdoll removing extra force to make character fall down again.
           also set another timed event to fire getting up animation
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

                    Vector3 boundsSize = col.bounds.size;
                    float max = boundsSize.x;
                    if (boundsSize.y > max) max = boundsSize.y;
                    if (boundsSize.z > max) max = boundsSize.z;



                    ball.transform.localScale = new Vector3(max, max, max);
                    ball.RigidBody.isKinematic = true;
                    ball.SphereCollider.isTrigger = true;
                    ball.RigidBody.detectCollisions = false;
                    ball.transform.position = col.bounds.center;
                    ball.transform.SetParent(col.transform);
                    ball.startAfterburner();

#if DEBUG_INFO
                    if (ball.HitInfo.bodyPartIndices == null)
                    {
                        Debug.LogError("object cannot be null.");
                        return;
                    }
#endif

                    rman.StartRagdoll(ball.HitInfo.bodyPartIndices,
                        ball.HitInfo.hitDirection * ball.HitInfo.hitStrength);


                    Vector3 v = new Vector3(0.0f, ball.up_force, 0.0f);
                    RagdollManager.BodyPartInfo b = rman.getBodyPartInfo(
                        ball.HitInfo.bodyPartIndices[0]);
                    b.extraForce = v;

                    ragdollUser.IgnoreHit = true;
                    rman.RagdollEventTime = 3.0f;
                    rman.OnTimeEnd = () =>
                    {
                        rman.StartRagdoll(null, Vector3.zero, Vector3.zero, true);

                        b = rman.getBodyPartInfo(ball.HitInfo.bodyPartIndices[0]);
                        b.extraForce = Vector3.zero;

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
