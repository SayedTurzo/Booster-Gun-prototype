// © 2015 Mario Lelas
using UnityEngine;

namespace MLSpace
{
    /// <summary>
    /// Camera Controller
    /// </summary>
    public class OrbitCameraController : MonoBehaviour
    {
        /// <summary>
        /// default camera target transform
        /// </summary>
        public Transform defaultCameraTarget;

        /// <summary>
        /// camera rotation speed
        /// </summary>
        public float angularSpeed = 64f;

        /// <summary>
        /// camera min rotation on x axis
        /// </summary>
        public float minXAngle = -60;

        /// <summary>
        /// camera max rotation on x axis
        /// </summary>
        public float maxXAngle = 45;           

        /// <summary>
        /// camera min rotation on y axis
        /// </summary>
        public float minYAngle = -180;

        /// <summary>
        /// camera max rotation on y axis
        /// </summary>
        public float maxYAngle = 180;

        /// <summary>
        /// camera min zoom distance
        /// </summary>
        public float minZ = 1;

        /// <summary>
        /// camera max zoom distance
        /// </summary>
        public float maxZ = 10;

        /// <summary>
        /// camera zoom step
        /// </summary>
        public float zStep = 0.5f;

        private Transform m_currentTransform;   // current target transform
        private float m_totalXAngleDeg = 0;     // current x angle in degrees
        private float m_totalYAngleDeg = 0;     // current y angle in degrees
        private float m_currentZ;               // camera current zoom
        private Vector3 m_CurrentTargetPos;     // camera target position
        private Vector3 m_offsetPosition;       // camera offset from target
        private Vector3 m_startingPosition;     // camera start offset from target
        private float m_switchSpeed = 1f;       // camera target switch speed
        private float m_lerpTime = 0.0f;        // camera target switch current time
        private float m_lerpMaxTime = 0.5f;     // camera target switch max time
        private Transform m_oldTransform;       // old target transform for use when switching targets ( lerp)
        private bool m_switchingTargets;        // switch target flag
        private bool m_disableInput = false;    // disabling input flag
        private bool m_initialized = false;

        private Vector3 m_targetOffset = Vector3.zero;
        public void SetTargetOffset(Vector3 offset)
        {
            m_targetOffset = offset;
        }

        /// <summary>
        /// disables camera control input
        /// </summary>
        public bool DisableInput { get { return m_disableInput; } set { m_disableInput = value; } }

        /// <summary>
        /// initialize camera class
        /// </summary>
        public void Initialize()
        {
            if (m_initialized) return;
#if DEBUG_INFO
            if (defaultCameraTarget == null) { Debug.LogError("object cannot be null"); return; }
#endif
            //m_ProtectFromWalls = GetComponent<ProtectFromWalls>();
            //if (!m_ProtectFromWalls)
            //    Debug.LogWarning("cannot find 'ProtectFromWalls' script");

            m_currentTransform = defaultCameraTarget;
            m_CurrentTargetPos = defaultCameraTarget.position;
            m_offsetPosition = transform.position - m_CurrentTargetPos;
            m_startingPosition = m_offsetPosition;

            m_initialized = true;
        }

#region UNITY METHODS

        /// <summary>
        /// Unity awake method
        /// </summary>
        void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Unity late update method
        /// </summary>
        void LateUpdate()
        {
#if DEBUG_INFO
            if (!m_initialized)
            {

                Debug.LogError("component not initialized.");
                return;
            }

            if (m_currentTransform == null) { Debug.LogError("object cannot be null"); return; }

#endif
            if (m_disableInput) return;

            m_CurrentTargetPos = m_currentTransform.position;
            if (m_switchingTargets)
            {
                m_lerpTime += Time.deltaTime * m_switchSpeed;

                if (m_lerpTime > m_lerpMaxTime)
                {
                    m_CurrentTargetPos = m_currentTransform.position;
                    m_switchingTargets = false;
                }
                else
                {
#if DEBUG_INFO
                    if (m_oldTransform == null) { Debug.LogError("object cannot be null"); return; }
#endif
                    float val = m_lerpTime / m_lerpMaxTime;
                    m_CurrentTargetPos = Vector3.Lerp(m_oldTransform.position, m_currentTransform.position, val);
                }
            }

            // inputs
            float angleAroundY = UnityStandardAssets.CrossPlatformInput.CrossPlatformInputManager.GetAxisRaw("Mouse X"); 
            float angleAroundX = -UnityStandardAssets.CrossPlatformInput.CrossPlatformInputManager.GetAxisRaw("Mouse Y");
            if (Input.GetAxis("Mouse ScrollWheel") < 0)
                m_currentZ = m_currentZ + zStep;
            else if (Input.GetAxis("Mouse ScrollWheel") > 0)
                m_currentZ = m_currentZ - zStep;

            float currentAngleY = angleAroundY * Time.deltaTime * angularSpeed;
            float currentAngleX = angleAroundX * Time.deltaTime * angularSpeed;


            m_totalXAngleDeg += currentAngleX;
            m_totalYAngleDeg += currentAngleY;

            m_totalXAngleDeg = wrapAngle(m_totalXAngleDeg);
            m_totalYAngleDeg = wrapAngle(m_totalYAngleDeg);



            m_totalXAngleDeg = Mathf.Clamp(m_totalXAngleDeg, minXAngle, maxXAngle);
            m_totalYAngleDeg = Mathf.Clamp(m_totalYAngleDeg, minYAngle, maxYAngle);

            Quaternion rotation =
                Quaternion.Euler
                (
                    m_totalXAngleDeg,
                    m_totalYAngleDeg,
                    0
                );
            m_offsetPosition = rotation * m_startingPosition;

            
            m_currentZ = Mathf.Clamp(m_currentZ, minZ, maxZ);
            transform.position = m_CurrentTargetPos + (m_offsetPosition * m_currentZ);


            transform.LookAt(m_CurrentTargetPos + m_targetOffset);
        }
#endregion

        /// <summary>
        /// keep angle in -360 to 360 interval
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        private float wrapAngle(float angle)
        {
            float newAngle = angle;
            if (angle > 180)
                newAngle -= 360;
            if (angle < -180)
                newAngle += 360;
            return newAngle;
        }

        /// <summary>
        /// switch camera target 
        /// </summary>
        /// <param name="newTarget">new target </param>
        /// <param name="speed">transition speed</param>
        public void SwitchTargets(Transform newTarget, float speed = 1.0f)
        {
#if DEBUG_INFO
            if (!m_initialized)
            {
                Debug.LogError("component not initialized.");
                return;
            }

            if (newTarget == null) { Debug.LogError("object cannot be null"); return; }
            if (m_currentTransform == null) { Debug.LogError("object cannot be null"); return; }
#endif

            if (newTarget == m_currentTransform) return;
            m_switchingTargets = true;
            m_oldTransform = m_currentTransform;
            m_currentTransform = newTarget;
            m_lerpTime = 0.0f;
            m_switchSpeed = speed;

            //if(m_ProtectFromWalls)
            //{
            //    m_ProtectFromWalls.m_Pivot = newTarget;
            //}
        }
    }
    
}