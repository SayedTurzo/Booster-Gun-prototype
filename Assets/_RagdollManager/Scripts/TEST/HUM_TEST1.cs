using UnityEngine;
using MLSpace;
using System.Collections.Generic;

[RequireComponent(typeof(RagdollManager))]
public class HUM_TEST1 : MonoBehaviour
{
    private RagdollManager m_Ragdoll;
    private Collider m_Capsule;
    private Rigidbody m_Rigidbody;
    private Animator m_Animator;

    // Use this for initialization
    void Start()
    {
        m_Ragdoll = GetComponent<RagdollManager>();

        m_Rigidbody = GetComponent<Rigidbody>();
        if (!m_Rigidbody) { Debug.LogWarning("Cannot find rigidbody"); }
        m_Rigidbody.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ |
            RigidbodyConstraints.FreezeRotationY;

        m_Animator = GetComponent<Animator>();
        if (!m_Animator) { Debug.LogError("Cannot find animator component."); }

        m_Capsule = GetComponent<Collider>();
        if (!m_Capsule) { Debug.LogError("Cannot find Collider component."); }

        m_Ragdoll.OnHit = () =>
        {
            m_Rigidbody.velocity = Vector3.zero;
            m_Rigidbody.detectCollisions = false;
            m_Rigidbody.isKinematic = true;
            m_Capsule.enabled = false;
        };
        m_Ragdoll.LastEvent = () =>
        {
            m_Rigidbody.detectCollisions = true;
            m_Rigidbody.isKinematic = false;
            m_Capsule.enabled = true;
        };
        m_Ragdoll.OnStartTransition = () =>
        {
            if (!m_Ragdoll.IsFullRagdoll && !m_Ragdoll.IsGettingUp)
            {
                m_Rigidbody.detectCollisions = true;
                m_Rigidbody.isKinematic = false;
                m_Capsule.enabled = true;
            }
        };
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            m_Ragdoll.StartRagdoll();
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            m_Ragdoll.BlendToMecanim();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            bool strafe = m_Animator.GetBool("Strafe");
            m_Animator.SetBool("Strafe", !strafe);
        }
        if (Input.GetMouseButtonDown(0))
        {
            doRagdoll();
        }
        if (Input.GetMouseButtonDown(1))
        {
            doHitReaction();
        }
    }

    private void doHitReaction()
    {
        Ray currentRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        int mask = LayerMask.GetMask("ColliderLayer", "ColliderInactiveLayer");
        RaycastHit rhit;
        if (Physics.Raycast(currentRay, out rhit, 120.0f, mask))
        {
            BodyColliderScript bcs = rhit.collider.GetComponent<BodyColliderScript>();
            int[] parts = new int[] { bcs.index };
            bcs.ParentRagdollManager.StartHitReaction(parts, currentRay.direction * 16.0f);
        }
    }

    private void doRagdoll()
    {
        Ray currentRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        int mask = LayerMask.GetMask("ColliderLayer", "ColliderInactiveLayer");
        RaycastHit rhit;
        if (Physics.Raycast(currentRay, out rhit, 120.0f, mask))
        {
            BodyColliderScript bcs = rhit.collider.GetComponent<BodyColliderScript>();
            int[] parts = new int[] { bcs.index };
            bcs.ParentRagdollManager .StartRagdoll(parts, currentRay.direction * 16.0f);
        }
    }
}
