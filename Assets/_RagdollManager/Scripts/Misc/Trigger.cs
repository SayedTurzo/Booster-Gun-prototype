// © 2015 Mario Lelas
using UnityEngine;

namespace MLSpace
{
    /// <summary>
    /// trigger types
    /// </summary>
    public enum TriggerTypes { Jump, Crouch, None };

    /// <summary>
    /// trigger class
    /// </summary>
    public class Trigger : MonoBehaviour
    {
        /// <summary>
        /// trigger type
        /// </summary>
        public TriggerTypes m_TriggerType = TriggerTypes.None;

        // unity on trigger enter method
        void OnTriggerEnter(Collider _collider)
        {
            // used only for testing sample assets AIThirdPersonCharacter
            UnityStandardAssets.Characters.ThirdPerson.AICharacterControlCustom aIThirdPersonScaracterScript =
                _collider.gameObject.GetComponent<UnityStandardAssets.Characters.ThirdPerson.AICharacterControlCustom>();

            if (!aIThirdPersonScaracterScript)
            {
#if DEBUG_INFO
                Debug.LogWarning("Does not have UnityStandardAssets Ai script. " + _collider.name + " on " + this.name );
#endif
                return;
            }

            switch (m_TriggerType )
            {
                case TriggerTypes.Crouch:
                    aIThirdPersonScaracterScript.SetCrouch(true);
                    break;
                case TriggerTypes.Jump:
                    aIThirdPersonScaracterScript.SetJump(true);
                    break;
            }


        }

        // unity on trigger exit method
        void OnTriggerExit(Collider _collider)
        {
            // used only for testing sample assets AIThirdPersonCharacter
            UnityStandardAssets.Characters.ThirdPerson.AICharacterControlCustom aIThirdPersonScaracterScript =
    _collider.gameObject.GetComponent<UnityStandardAssets.Characters.ThirdPerson.AICharacterControlCustom>();
            if (!aIThirdPersonScaracterScript)
            {
#if DEBUG_INFO
                Debug.LogWarning("Does not have UnityStandardAssets Ai script. " + _collider .name + " on " + this.name);
#endif
                return;
            }

            switch (m_TriggerType)
            {
                case TriggerTypes.Crouch:
                    aIThirdPersonScaracterScript.SetCrouch(false);
                    break;
            }
        }
        
    } 
}
