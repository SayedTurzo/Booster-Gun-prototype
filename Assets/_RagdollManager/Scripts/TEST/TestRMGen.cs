using UnityEngine;
using System.Collections;

namespace MLSpace
{
    public class TestRMGen : MonoBehaviour
    {
        private RagdollManagerGen m_Ragdoll;

        // Use this for initialization
        void Start()
        {
            m_Ragdoll = GetComponent<RagdollManagerGen>();
        }

        // Update is called once per frame
        void Update()
        {
            float force = 16.0f;
            Vector3 dir = Camera.main.transform.forward;
            Vector3 forceVelocity = dir * force;
            int[] parts = new int[] { 6, 8, 11, 12 };
            if(Input.GetKeyDown (KeyCode .F))
            {
                m_Ragdoll.StartRagdoll(parts, forceVelocity, Vector3.zero);
            }
            if(Input.GetKeyDown (KeyCode .G))
            {
                m_Ragdoll.BlendToMecanim();
            }
            if(Input.GetKeyDown(KeyCode.H))
            {
                m_Ragdoll.StartHitReaction(parts, forceVelocity);
            }
        }
    } 
}
