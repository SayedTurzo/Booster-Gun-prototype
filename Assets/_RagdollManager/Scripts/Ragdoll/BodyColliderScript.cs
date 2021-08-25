// © 2015 Mario Lelas

using UnityEngine;

namespace MLSpace
{
    /// <summary>
    ///  Helper class derived from ColliderScript that hold additional information about body part
    /// </summary>
    public class BodyColliderScript : ColliderScript
    {
        public bool critical = false;                   // you can apply additional damage if critial
        public BodyParts bodyPart = BodyParts.None ;    // collider body part
        public int index = -1;                          // index of collider

        [SerializeField, HideInInspector]
        private RagdollManager m_ParentRagdollManager;  // reference to parents ragdollmanager script

        public void SetParentRagdollManager(RagdollManager rm)
        {
            m_ParentRagdollManager = rm;
        }

        /// <summary>
        /// gets reference to parents ragdoll manager script
        /// </summary>
        public RagdollManager ParentRagdollManager
        {
            get { return m_ParentRagdollManager; }
        }
    } 
}
