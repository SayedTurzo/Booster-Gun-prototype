// © 2015 Mario Lelas
using UnityEngine;

/*
    Script for testing demo on andriod 
*/

namespace MLSpace
{
    public class AndroidTest : MonoBehaviour
    {
        private RagdollManagerHum m_Ragdoll;

        private float m_HitForce = 16.0f;

        private bool m_DoubleClick = false;
        private float m_DoubleClickMaxTime = 0.25f;
        private float m_DoubleClickCurTime = 0.0f;
        private int clickCount = 0;
        private Ray currentRay;


        // Use this for initialization
        void Start()
        {
            m_Ragdoll = GetComponent<RagdollManagerHum>();

            Animator anim = GetComponent<Animator>();

            //// example of disabling character capsule
            //// at entering ragdoll
            //// and enabling it after ragdoll ends
            //CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            //Rigidbody rigidbody = GetComponent<Rigidbody>();
            m_Ragdoll.OnHit = () =>
            {
                //capsule.enabled = false;
                //rigidbody.isKinematic = true;
                anim.applyRootMotion  = false;
            };
            m_Ragdoll.LastEvent = () =>
            {
                //capsule.enabled = true;
                //rigidbody.isKinematic = false;
                anim.applyRootMotion = true;
            };


            m_Ragdoll.RagdollEventTime = 3.0f;
            m_Ragdoll.OnTimeEnd = () =>
            {
                m_Ragdoll.BlendToMecanim();
            };
        }

        // Update is called once per frame
        void Update()
        {

            bool clicked = Input.GetMouseButtonDown(0);
            if (clicked)
            {
                currentRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                m_DoubleClickCurTime = 0.0f;
            }


            m_DoubleClickCurTime += Time.deltaTime;
            if (m_DoubleClickCurTime <= m_DoubleClickMaxTime)
            {
                if (clicked) clickCount++;
                if (clickCount > 1)
                    m_DoubleClick = true;
            }
            else
            {
                if (clickCount > 0)
                {
                    if (m_DoubleClick)
                    {
                        doRagdoll();
                    }
                    else
                    {
                        doHitReaction();
                    }
                    m_DoubleClickCurTime = 0.0f;
                }
                clickCount = 0;
                m_DoubleClick = false;
            }
        }

        private void doHitReaction()
        {
            int mask = LayerMask.GetMask("ColliderLayer", "ColliderInactiveLayer");
            RaycastHit rhit;
            if (Physics.Raycast(currentRay, out rhit, 120.0f, mask))
            {
                BodyColliderScript bcs = rhit.collider.GetComponent<BodyColliderScript>();
                int[] parts = new int[] { bcs.index };
                m_Ragdoll.StartHitReaction(parts, currentRay.direction * m_HitForce);
            }
        }

        private void doRagdoll()
        {
            int mask = LayerMask.GetMask("ColliderLayer", "ColliderInactiveLayer");
            RaycastHit rhit;
            if (Physics.Raycast(currentRay, out rhit, 120.0f, mask))
            {
                BodyColliderScript bcs = rhit.collider.GetComponent<BodyColliderScript>();
                int[] parts = new int[] { bcs.index };
                m_Ragdoll.StartRagdoll(parts, currentRay.direction * m_HitForce);
            }
        }
    } 
}
