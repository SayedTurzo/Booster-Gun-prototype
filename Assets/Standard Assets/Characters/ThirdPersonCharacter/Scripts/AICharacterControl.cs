using System;
using UnityEngine;

namespace UnityStandardAssets.Characters.ThirdPerson
{
    [RequireComponent(typeof (UnityEngine.AI.NavMeshAgent))]
    [RequireComponent(typeof (ThirdPersonCharacter))]
    public class AICharacterControl : MonoBehaviour
    {
        // TEMP
        public UnityEngine.UI.Text DBGUI;

        public UnityEngine.AI.NavMeshAgent agent { get; private set; } // the navmesh agent required for the path finding
        public ThirdPersonCharacter character { get; private set; } // the character we are controlling
        public Transform target; // target to aim for

        // Use this for initialization
        private void Start()
        {
            // get the components on the object we need ( should not be null due to require component so no need to check )
            agent = GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();
            character = GetComponent<ThirdPersonCharacter>();

	        agent.updateRotation = false;
            agent.updatePosition = false;
        }


        // Update is called once per frame
        private void Update()
        {
            if (!agent.enabled) 
            {
                character.Move(Vector3.zero, false, false);
                return;
            }

#if DEBUG_INFO
            
            if (target == null) { Debug.LogError("object cannot be null."); return; }
#endif
            if (target != null)
            {
                agent.SetDestination(target.position);

                // use the values to move the character
                character.Move(agent .desiredVelocity , false, false);
            }
            else
            {
                // We still need to call the character's move function, but we send zeroed input as the move param.
                character.Move(Vector3.zero, false, false);
            }

        }


        public void SetTarget(Transform _target)
        {
            this.target = _target;
        }
    }
}
