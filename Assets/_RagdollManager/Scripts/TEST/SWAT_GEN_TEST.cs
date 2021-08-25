using UnityEngine;
using MLSpace;

public class SWAT_GEN_TEST : MonoBehaviour
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
            if(m_Ragdoll.IsFullRagdoll)
            {
                bool upwards = OrientTransform.forward.y > 0.0f;
                if(upwards)
                {
                    m_Animator.CrossFade("GetUpFrontGen", 0.0f, 0, 0.0f);
                    Vector3 _up = OrientTransform.up;
                    _up.y = 0.0f;
                    transform.forward = -_up;
                }
                else
                {
                    m_Animator.CrossFade("GetUpBackGen", 0.0f, 0, 0.0f);
                    Vector3 _up = OrientTransform.up;
                    _up.y = 0.0f;
                    transform.forward = _up;
                }
            }
        };

    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown (KeyCode.F))
        {
            m_Ragdoll.StartRagdoll();
        }
        if(Input.GetKeyDown (KeyCode.G))
        {
            m_Ragdoll.BlendToMecanim();
        }
    }
}
