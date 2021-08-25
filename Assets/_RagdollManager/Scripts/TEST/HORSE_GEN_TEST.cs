using UnityEngine;
using MLSpace;

public class HORSE_GEN_TEST : MonoBehaviour
{

    public Transform OrientTransform;

    private RagdollManager m_Ragdoll;
    private Animator m_Animator;

    // Use this for initialization
    void Start()
    {
        m_Ragdoll = GetComponent<RagdollManager>();
        m_Animator = GetComponent<Animator>();

        m_Ragdoll.OnStartTransition = () =>
        {
            if(m_Ragdoll .IsFullRagdoll )
            {
                m_Animator.CrossFade("HorseGetUp", 0.0f, 0, 0.0f);
                Vector3 forw = OrientTransform.forward;
                forw.y = 0.0f;
                transform.forward = forw;
            }
        };


    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown (KeyCode .F))
        {
            m_Ragdoll.StartRagdoll();
        }
        if(Input.GetKeyDown (KeyCode .G))
        {
            m_Ragdoll.BlendToMecanim();
        }
    }
}
