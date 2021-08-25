// © 2015 Mario Lelas
using UnityEngine;

/*
    Examples of  RagdollManager usage.
    Add force on body part
    Connect with fixed joint
    Connect with configurable joint 
*/


namespace MLSpace
{
    public class TestWebScene2 : MonoBehaviour
    {
        public Transform parentToBe;

        private RagdollManagerHum m_Ragdoll;

        // Use this for initialization
        void Start()
        {
            m_Ragdoll = GetComponent<RagdollManagerHum>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                addBodypartForce();
            }
            if(Input.GetKeyDown (KeyCode .G))
            {
                addFixedJoint();
            }
            if(Input.GetKeyDown (KeyCode .H))
            {
                addConfigurableJoint();
            }
        }

        private void addBodypartForce()
        {
            m_Ragdoll.StartRagdoll();
            Vector3 v = new Vector3(0.0f, m_Ragdoll.weight * 12, 0.0f);
            RagdollManager.BodyPartInfo b = m_Ragdoll.getBodyPartInfo((int)BodyParts.LeftElbow);
            b.extraForce = v;


            m_Ragdoll.RagdollEventTime = 3.0f;
            m_Ragdoll.OnTimeEnd = () =>
            {
                m_Ragdoll.StartRagdoll(null, Vector3.zero, Vector3.zero, true);

                b = m_Ragdoll.getBodyPartInfo((int)BodyParts.LeftElbow);
                b.extraForce = Vector3.zero;

                m_Ragdoll.RagdollEventTime = 5.0f;
                m_Ragdoll.OnTimeEnd = () =>
                {
                    m_Ragdoll.BlendToMecanim();
                };
            };
        }

        private void addFixedJoint()
        {
            m_Ragdoll.StartRagdoll();
            RagdollManager.BodyPartInfo b = m_Ragdoll.getBodyPartInfo((int)BodyParts.RightKnee );

            // create and add fixed joint on right knee bodypart
            // when ragdoll timer reaches event time destroy it.

            FixedJoint fj = b.rigidBody.gameObject.AddComponent<FixedJoint>();
            fj.connectedBody = parentToBe.GetComponent<Rigidbody>();
            fj.connectedAnchor = Vector3.zero;

            m_Ragdoll.RagdollEventTime = 6.0f;
            m_Ragdoll.OnTimeEnd = () =>
            {
                m_Ragdoll.StartRagdoll(null, Vector3.zero, Vector3.zero, true);

                b = m_Ragdoll.getBodyPartInfo((int)BodyParts.RightKnee);
                Destroy(b.rigidBody .gameObject .GetComponent <FixedJoint >());

                m_Ragdoll.RagdollEventTime = 5.0f;
                m_Ragdoll.OnTimeEnd = () =>
                {
                    m_Ragdoll.BlendToMecanim();
                };
            };
        }

        private void addConfigurableJoint()
        {
            m_Ragdoll.StartRagdoll();

            // create and add configurable joint on right knee bodypart
            // when ragdoll timer reaches event time destroy it.

            RagdollManager.BodyPartInfo b = m_Ragdoll.getBodyPartInfo((int)BodyParts.RightKnee);

            ConfigurableJoint cj = b.rigidBody.gameObject.AddComponent<ConfigurableJoint>();
            cj.connectedBody = parentToBe.GetComponent<Rigidbody>();
            cj.connectedAnchor = parentToBe.position;
            SoftJointLimit sjl = new SoftJointLimit();
            sjl.limit = 0.05f;
            cj.linearLimit = sjl;
            cj.xMotion = ConfigurableJointMotion.Limited;
            cj.yMotion = ConfigurableJointMotion.Limited;
            cj.zMotion = ConfigurableJointMotion.Limited;

            m_Ragdoll.RagdollEventTime = 16.0f;
            m_Ragdoll.OnTimeEnd = () =>
            {
                m_Ragdoll.StartRagdoll(null, Vector3.zero, Vector3.zero, true);

                b = m_Ragdoll.getBodyPartInfo((int)BodyParts.RightKnee);
                Destroy(b.rigidBody.gameObject.GetComponent<FixedJoint>());

                m_Ragdoll.RagdollEventTime = 5.0f;
                m_Ragdoll.OnTimeEnd = () =>
                {
                    m_Ragdoll.BlendToMecanim();
                };
            };
        }
    }
}