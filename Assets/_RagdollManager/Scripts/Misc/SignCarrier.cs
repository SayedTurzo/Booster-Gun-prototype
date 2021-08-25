// © 2015 Mario Lelas
using UnityEngine;


/*
    Script for testing scene
    It just movig position of GameObject ( 3D Text )
*/

namespace MLSpace
{
    public class SignCarrier : MonoBehaviour
    {
        public Transform parent;

        void LateUpdate()
        {
            if (!parent) return;

            this.transform.position = parent.position;
            Vector3 direction = this.transform.position - Camera.main.transform.position;
            this.transform.rotation = Quaternion.LookRotation(-direction.normalized);
        }
    } 
}
