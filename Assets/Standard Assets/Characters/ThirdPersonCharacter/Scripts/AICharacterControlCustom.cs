using UnityEngine;

namespace UnityStandardAssets.Characters.ThirdPerson
{
    [RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
    [RequireComponent(typeof(ThirdPersonCharacter))]
    public class AICharacterControlCustom : MonoBehaviour
    {
        public UnityEngine.AI.NavMeshAgent agent { get; private set; } // the navmesh agent required for the path finding
        public ThirdPersonCharacter character { get; private set; } // the character we are controlling
        public Transform[] targets; // target to aim for
        private int m_CurrentTargetIndex = 0;

        private bool m_Jump = false;
        private bool m_Crouch = false;

        // Use this for initialization
        private void Start()
        {
            // get the components on the object we need ( should not be null due to require component so no need to check )
            agent = GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();
            character = GetComponent<ThirdPersonCharacter>();

            agent.updateRotation = false;
            agent.updatePosition = false;

            if (targets.Length == 0) { Debug.LogError("No waypoints exists."); return; }
            agent.SetDestination(targets[m_CurrentTargetIndex].position);

            //SetCrouch(true);
        }

        // Update is called once per frame
        private void Update()
        {
            if (!agent.enabled)
            {
                character.Move(Vector3.zero, false, false);
                return;
            }




            if(Input.GetKeyDown (KeyCode.J))
            {
                m_Jump = true;
            }
            if (Input.GetKey(KeyCode.C))
                m_Crouch = true;

#if DEBUG_INFO

            if (targets == null) { Debug.LogError("object cannot be null."); return; }
            if (targets.Length == 0) { Debug.LogError("No waypoints exists."); return; }
            if (m_CurrentTargetIndex < 0 || m_CurrentTargetIndex >= targets.Length)
            {
                Debug.LogError("target index out of range."); return;
            }
#endif
            float distanceFromTarget = Vector3.Distance(transform.position, targets[m_CurrentTargetIndex].position);
            Vector3 manualVelocity = targets[m_CurrentTargetIndex].position - transform.position;
            manualVelocity.y = 0.0f;
            manualVelocity.Normalize();
            float speed = 60.0f;
            manualVelocity = manualVelocity * Time.deltaTime * speed;

            Debug.DrawLine(targets[m_CurrentTargetIndex].position,
                targets[m_CurrentTargetIndex].position + Vector3.up * 6f, Color.blue);
            Debug.DrawLine(transform.position, transform.position + agent.desiredVelocity, Color.yellow);
            Debug.DrawLine(transform.position, transform.position + manualVelocity, Color.red);


            if (distanceFromTarget < 1.0f)
            {
                m_CurrentTargetIndex++;
                if (m_CurrentTargetIndex >= targets.Length)
                    m_CurrentTargetIndex = 0;
            }

            // use the values to move the character
            character.Move(manualVelocity, m_Crouch , m_Jump);

            m_Jump = false;
            //m_Crouch = false;
        }


        public void SetTargets(Transform[] _targets)
        {
            this.targets = _targets;
        }

        public void SetJump(bool _jump)
        {
            m_Jump = _jump;
        }

        public void SetCrouch(bool _crouch)
        {
            m_Crouch = _crouch;
        }
    } 
}
