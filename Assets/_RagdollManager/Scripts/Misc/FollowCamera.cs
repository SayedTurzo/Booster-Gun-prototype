using UnityEngine;
using System.Collections;

namespace MLSpace
{
    public class FollowCamera : MonoBehaviour
    {
        public Transform Target;

        private Vector3 m_Offset = Vector3.zero;
        // Use this for initialization
        void Start()
        {
            if (!Target) { Debug.LogError("Target transform not assiged."); }

            m_Offset = Target.position - transform.position;
        }

        // Update is called once per frame
        void Update()
        {
#if DEBUG_INFO
            if (!Target) { Debug.LogError("Target transform not assiged."); return; }
#endif

            transform.position = Target.position - m_Offset;

        }
    } 
}
