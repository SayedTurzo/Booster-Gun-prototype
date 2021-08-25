// © 2015 Mario Lelas
using UnityEngine;

/*
    Used for one of test scenes
    Focuses camera on targets.
*/



namespace MLSpace
{
    /// <summary>
    /// camera that focuses on assigned targets
    /// </summary>
    public class WebScene1Camera : MonoBehaviour
    {
        /// <summary>
        /// focus targets
        /// </summary>
        public Transform lookAtTarget1, lookAtTarget2;

        public enum FocusPoint { Target1, Target2, Middle };
        private FocusPoint m_Focus = FocusPoint.Middle;

        private bool lerping = false;
        private Vector3 lSTart, lEnd;
        private Vector3 currentPoint;
        private float currTime = 0.0f, maxTime = 1.0f;

        // Update is called once per frame
        void LateUpdate()
        {
            if (lerping)
            {
                currTime += Time.deltaTime;
                switch (m_Focus)
                {
                    case FocusPoint.Middle:
                        Vector3 dir = lookAtTarget2.position - lookAtTarget1.position;
                        float dist = Vector3.Distance(lookAtTarget1.position, lookAtTarget2.position);
                        lEnd = lookAtTarget1.position + (dir.normalized * (dist * 0.5f));
                        break;
                    case FocusPoint.Target1:
                        lEnd = lookAtTarget1.position;
                        break;
                    case FocusPoint.Target2:
                        lEnd = lookAtTarget2.position;
                        break;
                }

                float lValue = currTime / maxTime;
                currentPoint = Vector3.Lerp(lSTart, lEnd, lValue);

                if (currTime >= maxTime )
                {
                    lerping = false;
                    currentPoint = lEnd;
                }
            }
            else
            {
                currentPoint = lookAtTarget1.position;
                switch (m_Focus)
                {
                    case FocusPoint.Middle:
                        Vector3 dir = lookAtTarget2.position - lookAtTarget1.position;
                        float dist = Vector3.Distance(lookAtTarget1.position, lookAtTarget2.position);
                        currentPoint = lookAtTarget1.position + (dir.normalized * (dist * 0.5f));
                        break;
                    case FocusPoint.Target2:
                        currentPoint = lookAtTarget2.position;
                        break;
                }
            }
            transform.LookAt(currentPoint);
        }

        public void FocusOnTarget1()
        {
            m_Focus = FocusPoint.Target1;
            lerping = true;
            lSTart = currentPoint;
            currTime = 0.0f;
        }

        public void FocusOnTarget2()
        {
            m_Focus = FocusPoint.Target2;
            lerping = true;
            lSTart = currentPoint;
            currTime = 0.0f;
        }

        public void FocusOnMiddle()
        {
            m_Focus = FocusPoint.Middle;
            lerping = true;
            lSTart = currentPoint;
            currTime = 0.0f;
        }
    } 
}
