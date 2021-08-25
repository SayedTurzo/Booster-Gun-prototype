// © 2015 Mario Lelas
using UnityEngine;
using MLSpace;


/*
    Basic sample for testing RagdollManager.
    Interacts with bodyparts on mouse button click.

    WARNING:
    This is  script for this test scene only.
    Not recomend to use in actual game.
*/


public class TestWebScene1 : MonoBehaviour
{
    private RagdollManagerHum m_Ragdoll;
    private float m_HitForce = 16.0f;

    // Use this for initialization
    void Start ()
    {
        m_Ragdoll = GetComponent<RagdollManagerHum>();

        m_Ragdoll.RagdollEventTime = 3.0f;
        m_Ragdoll.OnTimeEnd = () =>
         {
             m_Ragdoll.BlendToMecanim();
         };
	}

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            doHitReaction();
        }
        if (Input.GetMouseButtonDown(1))
        {
            doRagdoll();
        }
    }

    private void doHitReaction()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int mask = LayerMask.GetMask("ColliderLayer", "ColliderInactiveLayer");
        RaycastHit rhit;
        if (Physics.Raycast(ray, out rhit, 120.0f, mask))
        {
            BodyColliderScript bcs = rhit.collider.GetComponent<BodyColliderScript>();
            if (bcs.ParentObject == this.gameObject)
            {
                int[] parts = new int[] { bcs.index };
                m_Ragdoll.StartHitReaction(parts, ray.direction * m_HitForce);
            }
        }
    }

    private void doRagdoll()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int mask = LayerMask.GetMask("ColliderLayer", "ColliderInactiveLayer");
        RaycastHit rhit;
        if (Physics.Raycast(ray, out rhit, 120.0f, mask))
        {
            BodyColliderScript bcs = rhit.collider.GetComponent<BodyColliderScript>();
            if (bcs.ParentObject == this.gameObject)
            {
                int[] parts = new int[] { bcs.index };
                m_Ragdoll.StartRagdoll(parts, ray.direction * m_HitForce);
            }
        }
    }
}
