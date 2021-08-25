// © 2015 Mario Lelas
using UnityEngine;

/*
    Script that creates ConfigurableJoint on collided body and connect with this rigid body
    Testing RagdollManager bodyparts interaction with joints
*/

namespace MLSpace
{
    public class GrabTrigger : MonoBehaviour
    {
        private enum Mode { Grab, Jointed, Leniency };
        private Mode m_Mode = Mode.Grab;


        private float m_LeniencyCurTime = 0.0f;         // time counter between grabbing
        private float m_LeniencyMaxTime = 2.5f;         // max time between grabbing

        private SphereCollider m_Sphere;

        void Start()
        {
            m_Sphere = GetComponent<SphereCollider>();
            if (!m_Sphere) { Debug.LogError("Cannot find 'SphereCollider' component."); }
        }

        //// I disable ragdoll colliders for performance reasons 
        //// If you enable it. You can use this OnTriggerEnter function 
        //// instead manually checking for overlaps
        //// unity on trigger enter function
        //void OnTriggerEnter(Collider _collider)
        //{
        //    if (m_Mode == Mode.Jointed) return;

        //    if (_collider.gameObject.layer == LayerMask.NameToLayer("ColliderLayer"))
        //    {
        //        BodyColliderScript bcs = _collider.GetComponent<BodyColliderScript>();
        //        if (!bcs) { Debug.LogError("no body collider script."); return; }
        //        RagdollManager rm = bcs.ParentObject.GetComponent<RagdollManager>();
        //        rm.StartRagdoll();
        //        RagdollManager.BodyPartInfo b = rm.getBodyPartInfo(bcs.bodyPart);
        //        ConfigurableJoint cj = b.rigidBody.gameObject.AddComponent<ConfigurableJoint>();
        //        cj.connectedBody = this.GetComponent<Rigidbody>();
        //        cj.connectedAnchor = Vector3.zero;
        //        SoftJointLimit sjl = new SoftJointLimit();
        //        sjl.limit = 0.01f;
        //        cj.linearLimit = sjl;
        //        cj.xMotion = ConfigurableJointMotion.Limited;
        //        cj.yMotion = ConfigurableJointMotion.Limited;
        //        cj.zMotion = ConfigurableJointMotion.Limited;
        //        //// comment this if you dont want spring
        //        //SoftJointLimitSpring sjls = new SoftJointLimitSpring();
        //        //sjls.spring = 480;
        //        //cj.linearLimitSpring = sjls;
        //        // release and delete joint after some time
        //        rm.RagdollEventTime = 9.0f;
        //        rm.OnTimeEnd = () =>
        //        {
        //            b = rm.getBodyPartInfo(bcs.bodyPart);
        //            Destroy(cj);
        //            m_Mode = Mode.Grab;
        //            rm.StartRagdoll(null, Vector3.zero, Vector3.zero, true);
        //            rm.RagdollEventTime = 5.0f;
        //            rm.OnTimeEnd = () =>
        //            {
        //                rm.BlendToMecanim();
        //            };
        //        };
        //        m_Mode = Mode.Jointed;
        //    }
        //}

        void LateUpdate()
        {
            switch (m_Mode)
            {
                case Mode.Leniency:
                m_LeniencyCurTime += Time.deltaTime;
                    if (m_LeniencyCurTime > m_LeniencyMaxTime)
                        m_Mode = Mode.Grab;
                    break;
                case Mode.Grab:
                    Grab();
                    break;
            }
        }

        private void Grab()
        {
            Vector3 center = transform.position + m_Sphere.center;
            float radius = m_Sphere.radius * transform.localScale.x;

            int mask = LayerMask.GetMask("ColliderInactiveLayer");
            Collider[] overlaps = Physics.OverlapSphere(center, radius, mask);

            // grab first
            if (overlaps.Length > 0)
            {
               
                Collider col = overlaps[0];

                

                BodyColliderScript bcs = col.GetComponent<BodyColliderScript>();

                if (!bcs) { Debug.LogError("no body collider script."); return; }
                RagdollManagerHum rm = bcs.ParentObject.GetComponent<RagdollManagerHum>();
                rm.StartRagdoll();
                RagdollManager.BodyPartInfo b = rm.getBodyPartInfo(bcs.index );

                ConfigurableJoint cj = b.rigidBody.gameObject.AddComponent<ConfigurableJoint>();
                cj.connectedBody = this.GetComponent<Rigidbody>();
                cj.connectedAnchor = Vector3.zero;
                SoftJointLimit sjl = new SoftJointLimit();
                sjl.limit = 0.01f;
                cj.linearLimit = sjl;
                cj.xMotion = ConfigurableJointMotion.Limited;
                cj.yMotion = ConfigurableJointMotion.Limited;
                cj.zMotion = ConfigurableJointMotion.Limited;

                //// comment this if you dont want spring joints
                //SoftJointLimitSpring sjls = new SoftJointLimitSpring();
                //sjls.spring = 480;
                //cj.linearLimitSpring = sjls;
                // release and delete joint after some time

                rm.RagdollEventTime = 9.0f;
                rm.OnTimeEnd = () =>
                {
                    b = rm.getBodyPartInfo(bcs.index );
                    Destroy(cj);
                    m_Mode = Mode.Leniency;
                    m_LeniencyCurTime = 0.0f;
                    rm.StartRagdoll(null, Vector3.zero, Vector3.zero, true);
                    rm.RagdollEventTime = 5.0f;
                    rm.OnTimeEnd = () =>
                    {
                        rm.BlendToMecanim();
                    };
                };
                m_Mode = Mode.Jointed;
            }
        }
    } 

}
