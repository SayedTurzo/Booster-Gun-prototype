// © 2015 Mario Lelas
using UnityEngine;

namespace MLSpace
{
    /// <summary>
    /// Helper class that hold information about body part ( parent object )
    /// </summary>
    public class ColliderScript : MonoBehaviour
    {
        public GameObject ParentObject;     // collider of what object
        private Collider m_Collider;        // reference to collider component

        /// <summary>
        /// gets reference to collider component
        /// </summary>
        public Collider Collider { get { return m_Collider; } }

        /// <summary>
        /// Initialize 
        /// </summary>
        /// <returns></returns>
        public bool Initialize()
        {
            if (!m_Collider)
            {
                m_Collider = GetComponent<Collider>();
                if (!m_Collider) { Debug.LogWarning("Collider scipt cannot find 'Collider' component. " + this.name); return false; }
            }
            return true;
        }

        // unity start
        void Start()
        {
            Initialize();   
        }
    } 
}
