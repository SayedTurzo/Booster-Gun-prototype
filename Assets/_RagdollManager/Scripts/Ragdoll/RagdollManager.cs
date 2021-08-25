// © 2016 Mario Lelas

using UnityEngine;
using System.Collections.Generic;

namespace MLSpace
{
    /// <summary>
    /// base ragdoll and hit reaction manager
    /// </summary>
    public abstract class RagdollManager : MonoBehaviour
    {
        /// <summary>
        /// Accepting hits on on certain interval enum
        /// </summary>
        public enum HitIntervals
        {
            Always,         // always accept hits
            OnBlend,        // accept hits after ragdoll - on blend
            OnGettingUp,    // accept hits after blend - on getting up if enabled and animated
            OnAnimated,     // accept hits only when animating
            Timed           // accept hits on time intervals
        };
        
        /// <summary>
        /// ragdoll manager states
        /// </summary>
        public enum RagdollState : int
        {
            Ragdoll = 0,    // Ragdoll on, animator off
            Blend,          // Animator on, blending between last ragdoll and current animator transforms
            GettingUpAnim,  // Get up animation after ragdoll
            Animated,       // Animator full on
        }

        /// <summary>
        ///  Class that holds useful information for each body part
        /// </summary>
        public class BodyPartInfo
        {
            public BodyParts bodyPart = BodyParts.None;                 // current body part
            public int index = -1;                                      // index of body part
            public Transform transform = null;                          // transform of body part
            public Transform orig_parent = null;                        // original parent of body part
            public Collider collider = null;                            // collider of body part
            public Rigidbody rigidBody = null;                          // rigidbody of body part
            public Vector3 transitionPosition = Vector3.zero;           // transition position used for blending
            public Quaternion transitionRotation = Quaternion.identity; // transition rotation used for blending
            public Vector3 extraForce = Vector3.zero;                   // extra force used for adding to body part in ragdoll mode
            public ConfigurableJoint constraintJoint = null;            // constraint to add body parts like legs
            public Vector3 previusPosition = Vector3.zero;              // previus position to help calculate velocity
            public Vector3 customVelocity = Vector3.zero;               // custom velocity calculated on kinematic bodies
        }

        

#region Fields
        /// <summary>
        /// ragdoll transforms with colliders and rigid bodies 
        /// </summary>
        public Transform[] RagdollBones;

        /// <summary>
        /// name of active collider layer
        /// </summary>
        [Tooltip("Name of active collider layer ( when ragdolled ).")]
        public string activeColLayerName = "ColliderLayer";

        /// <summary>
        /// name of inactive collider layer
        /// </summary>
        [Tooltip("Name of inactive collider layer ( when animation runs ).")]
        public string inactiveColLayerName = "ColliderInactiveLayer";



        /// <summary>
        /// create joints on constrained bodyparts / legs
        /// </summary>
        public bool useJoints = true;

        /// <summary>
        /// How long do we blend from ragdoll to animator
        /// </summary>
        [Tooltip("Blend time from ragdoll to animator.")]
        public float blendTime = 0.4f;

        [Tooltip("Set time interval."), HideInInspector]
        public float hitTimeInterval = 0.25f;

        [Tooltip("Controls how character reacts to hits.")]
        public float hitResistance = 8.0f;

        [Tooltip("Tolerance to hit velocity. If hit velocity is higher this, than character goes to full ragdoll")]
        public float hitReactionTolerance = 20.0f;

        [Tooltip("Influences hit reactions.")]
        public float weight = 32.0f;

        /// <summary>
        /// hit interval
        /// </summary>
        [Tooltip("Accept hits intervals enum.")]
        public HitIntervals hitInterval = HitIntervals.Always;

        // ragdoll event time
        protected float m_RagdollEventTime = 6.0f;

        // current hit timer
        protected float m_CurrentHitTime = 0.0f;

        // transition  timer
        protected float m_CurrentBlendTime = 0.0f;

        // array of body parts
        protected BodyPartInfo[] m_BodyParts;

        // reference to animator
        protected Animator m_Animator;

        // is ragdoll physics on
        protected bool m_RagdollEnabled = true;

        // initial animator update mode
        protected AnimatorUpdateMode m_InitialMode =
            AnimatorUpdateMode.Normal;

        // does have root motion
        protected bool m_InitialRootMotion = false;

        protected bool m_FireHitReaction = false;                 // fire hit reaction flag
        protected Vector3? m_ForceVel = null;                     // hit force velocity
        protected bool m_FireAnimatorBlend = false;               // fire blend to animator flag
        protected bool m_FireRagdoll = false;                     // fire ragdoll flag
        protected Vector3? m_ForceVelocityOveral = null;          // force velocity on non hits parts
        protected bool m_FullRagdoll = false;                     // full ragdoll on flag
        protected bool m_GettingUpEnableInternal = true;          // internal setup of getting up animation
        protected bool m_GettingUp = false;                       // is getting up from ragdoll in progress
        protected bool m_HitReacWhileGettingUp = false;           // is hit made in getting up mode
        protected float m_CurrentEventTime = 0.0f;                // event current time
        protected RagdollState m_state = RagdollState.Animated;   // The current state
        protected bool m_Initialized = false;                     // is class initialized


        protected int m_ActiveLayer;        // layer when ragdolled
        protected int m_InactiveLayer;      // layer when not ragdolled

        protected List<int> m_ConstraintIndices;    // list of constrained indices ( legs usualy )

        // EVENTS
        protected VoidFunc m_OnBlendEnd = null;       // on ragdoll end event
        protected VoidFunc m_OnTimeEnd = null;        // on time event
        protected VoidFunc m_InternalOnHit = null;    // internal hit reaction event
        protected VoidFunc m_LastEvent = null;        // last event on after ragdoll
        protected VoidFunc m_OnGetUp = null;          // after get up animation event
        protected VoidFunc m_OnHit = null;            // on hit event
        protected VoidFunc m_OnStartTransition = null;// event on transition to animated start

#if SAVE_ANIMATOR_STATES
        protected List<AnimatorStateInfo> m_SavedCurrentStates =
            new List<AnimatorStateInfo>();                                          // saved animator states on disable
        protected Dictionary<AnimatorControllerParameter, object> m_SavedParams =
            new Dictionary<AnimatorControllerParameter, object>();                  // saved animator parameters on disable
#endif

        protected Transform m_RootTransform;

        // keep track of important animated positions and rotations
        protected Vector3 m_AnimatedRootPosition = Vector3.zero;
        protected Quaternion[] m_AnimatedRotations;


        // timer and flag of hit reaction system
        protected float m_HitReactionTimer = 0.0f;
        protected float m_HitReactionMax = 0.0f;
        protected bool m_HitReactionUnderway = false;

        protected bool m_AcceptHit = true;                      // does system accepts hit ( based on hit interval )
        protected bool m_IgnoreHitInterval = false;             // ignore hit interval flag

        protected int[] m_HitParts = null;                      // body parts hit array

        protected ForceMode m_ForceMode = ForceMode.VelocityChange; // mode of adding extra force to body parts

#endregion


#region Properties

        /// <summary>
        /// gets and sets ForceMode on body parts extra force
        /// </summary>
        public ForceMode ExtraForceMode { get { return m_ForceMode; } set { m_ForceMode = value; } }

        /// <summary>
        /// return true if component is initialized
        /// </summary>
        public bool Initialized { get { return m_Initialized; } }

        /// <summary>
        /// returns true if ragdoll manager accepts hits based on hit interval
        /// </summary>
        public bool AcceptHit { get { return m_AcceptHit; } }


        /// <summary>
        /// gets and sets ragdoll timed event time
        /// </summary>
        public float RagdollEventTime { get { return m_RagdollEventTime; } set { m_RagdollEventTime = value; } }

        /// <summary>
        /// gets current state of ragdoll manager
        /// </summary>
        public RagdollState State { get { return m_state; } }

        /// <summary>
        /// gets getting up flag
        /// </summary>
        public bool IsGettingUp { get { return m_GettingUp; } }

        /// <summary>
        /// gets and sets on ragdoll end delegate.
        /// </summary>
        public VoidFunc OnBlendEnd { get { return m_OnBlendEnd; } set { m_OnBlendEnd = value; } }

        /// <summary>
        /// gets and sets on get up event
        /// </summary>
        public VoidFunc OnGetUpEvent { get { return m_OnGetUp; } set { m_OnGetUp = value; } }

        /// <summary>
        /// gets and sets last event after hit
        /// </summary>
        public VoidFunc LastEvent { get { return m_LastEvent; } set { m_LastEvent = value; } }

        /// <summary>
        /// gets and sets on time end delegate.
        /// </summary>
        public VoidFunc OnTimeEnd { get { return m_OnTimeEnd; } set { m_OnTimeEnd = value; } }

        /// <summary>
        /// gets and sets on hit event
        /// </summary>
        public VoidFunc OnHit { get { return m_OnHit; } set { m_OnHit = value; } }

        /// <summary>
        /// gets and sets on start to transition to animated event
        /// </summary>
        public VoidFunc OnStartTransition { get { return m_OnStartTransition; } set { m_OnStartTransition = value; } }

        /// <summary>
        /// returns true if its full ragdoll on
        /// </summary>
        public bool IsFullRagdoll { get { return m_FullRagdoll; } }


        /// <summary>
        /// gets spine bone transform
        /// </summary>
        public Transform RootTransform
        {
            get
            {
                return m_RootTransform;
            }
        }

        /// <summary>
        /// gets number of bodyparts
        /// </summary>
        public abstract int BodypartCount { get; }

#endregion

        /// <summary>
        /// initialize component
        /// </summary>
        public virtual void Initialize()
        {
            int ac = LayerMask.NameToLayer(activeColLayerName);
            int inac = LayerMask.NameToLayer(inactiveColLayerName);

            if (ac == -1) Debug.LogError("Cannot find " + activeColLayerName + " layer. Does it exist.");
            if (inac == -1) Debug.LogError("Cannot find " + inactiveColLayerName + " layer. Does it exist.");

            m_ActiveLayer = ac;
            m_InactiveLayer = inac;

            m_CurrentHitTime = hitTimeInterval;
        }

        /// <summary>
        /// set constrained body part 
        /// </summary>
        /// <param name="indices">indices of to be constrained body parts</param>
        public void createConstraints(List<int> indices)
        {
#if DEBUG_INFO
            if (m_BodyParts == null)
            {
                Debug.LogError("object cannot be null.");
                return;
            }
#endif
            if (m_ConstraintIndices != null)
            {
                for (int i = 0; i < m_ConstraintIndices.Count; i++)
                {
                    Destroy(m_BodyParts[m_ConstraintIndices[i]].constraintJoint);
                }
                m_ConstraintIndices.Clear();
                m_ConstraintIndices = null;
            }
            m_ConstraintIndices = indices;
            if (useJoints)
            {
                for (int i = 0; i < indices.Count; i++)
                {
                    int index = indices[i];
                    ConfigurableJoint cfj =
                        m_BodyParts[index].transform.gameObject.AddComponent<ConfigurableJoint>();
                    cfj.connectedBody = null;
                    cfj.connectedAnchor = Vector3.zero;
                    cfj.anchor = Vector3.zero;
                    SoftJointLimit sjl = new SoftJointLimit();
                    sjl.limit = 0.00f;
                    cfj.linearLimit = sjl;
                    cfj.xMotion = ConfigurableJointMotion.Free;
                    cfj.yMotion = ConfigurableJointMotion.Free;
                    cfj.zMotion = ConfigurableJointMotion.Free;
                    cfj.angularXMotion = ConfigurableJointMotion.Free;
                    cfj.angularYMotion = ConfigurableJointMotion.Free;
                    cfj.angularZMotion = ConfigurableJointMotion.Free;
                    cfj.configuredInWorldSpace = true;
                    //cfj.enablePreprocessing = false;
                    m_BodyParts[index].constraintJoint = cfj;
                }
            }
        }

        /// <summary>
        /// setup colliders and rigid bodies for ragdoll start
        /// set colliders to be triggers and set rigidbodies to be kinematic
        /// </summary>
        protected virtual void enableRagdoll(bool gravity = true)
        {
#if DEBUG_INFO
            if (m_BodyParts == null) { Debug.LogError("object cannot be null."); return; }
#endif

            if (m_RagdollEnabled)
            {
                return;
            }

            for (int i = 0; i < m_BodyParts.Length; ++i)
            {
#if DEBUG_INFO
                if (m_BodyParts[i] == null) { Debug.LogError("object cannot be null."); continue; }
#else
                if (m_BodyParts[i] == null) continue;
#endif
                if (m_BodyParts[i].collider != null)
                {
                    m_BodyParts[i].collider.isTrigger = false;
                }
#if DEBUG_INFO
                else Debug.LogWarning("body part collider is null.");
#endif


                if (m_BodyParts[i].rigidBody)
                {
                    m_BodyParts[i].rigidBody.useGravity = gravity;
                    m_BodyParts[i].rigidBody.isKinematic = false;
                }
#if DEBUG_INFO
                else Debug.LogWarning("body part rigid body is null.");
#endif

                // Unity 5.2.3 upwards
                // switch to layer that interacts with enviroment
                m_BodyParts[i].transform.gameObject.layer = m_ActiveLayer;
            }
            m_CurrentBlendTime = 0.0f;
            m_RagdollEnabled = true;

        }

        /// <summary>
        /// disable ragdoll. setup colliders and rigid bodies for normal use
        /// set colliders to not be triggers and set rigidbodies to not be kinematic
        /// </summary>
        protected virtual  void disableRagdoll()
        {
#if DEBUG_INFO
            if (m_BodyParts == null) { Debug.LogError("object cannot be null."); return; }
#endif

            if (!m_RagdollEnabled) return;

            for (int i = 0; i < m_BodyParts.Length; ++i)
            {
#if DEBUG_INFO
                if (m_BodyParts[i] == null) { Debug.LogError("object cannot be null."); continue; }
#else
                if (m_BodyParts[i] == null) continue;
#endif
                if (m_BodyParts[i].collider != null)
                {
                    m_BodyParts[i].collider.isTrigger = true;
                }
#if DEBUG_INFO
                else Debug.LogWarning("body part collider is null.");
#endif

                if (m_BodyParts[i].rigidBody)
                {
                    m_BodyParts[i].rigidBody.useGravity = false;
                    m_BodyParts[i].rigidBody.isKinematic = true;
                }
#if DEBUG_INFO
                else Debug.LogWarning("body part rigid body is null.");
#endif

                // Unity 5.2.3 upwards
                // switch to layer that interacts with nothing
                m_BodyParts[i].transform.gameObject.layer = m_InactiveLayer;
            }
            m_RagdollEnabled = false;
        }


        public abstract bool addBodyColliderScripts();

#if SAVE_ANIMATOR_STATES
        protected void saveAnimatorStates()
        {
            m_SavedCurrentStates.Clear();
            for (int i = 0; i < m_Animator.layerCount; i++)
            {
                AnimatorStateInfo curstate = m_Animator.GetCurrentAnimatorStateInfo(i);
                m_SavedCurrentStates.Add(curstate);
            }

            m_SavedParams.Clear();
            ;
            for (int i = 0; i < m_Animator.parameters.Length; i++)
            {
                AnimatorControllerParameter par = m_Animator.parameters[i];
                object val = null;
                switch (par.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        val = (object)m_Animator.GetBool(par.name);
                        break;
                    case AnimatorControllerParameterType.Float:
                        val = (object)m_Animator.GetFloat(par.name);
                        break;
                    case AnimatorControllerParameterType.Int:
                        val = (object)m_Animator.GetInteger(par.name);
                        break;
                }
                m_SavedParams.Add(par, val);
            }
        }

        // reset animator states and parameters
        protected void resetAnimatorStates()
        {
            foreach (KeyValuePair<AnimatorControllerParameter, object> pair in m_SavedParams)
            {
                AnimatorControllerParameter p = pair.Key;
                object v = pair.Value;
                switch (p.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        {
                            bool bval = (bool)v;
                            m_Animator.SetBool(p.name, bval);
                        }
                        break;
                    case AnimatorControllerParameterType.Float:
                        {
                            float fval = (float)v;
                            m_Animator.SetFloat(p.name, fval);
                        }
                        break;
                    case AnimatorControllerParameterType.Int:
                        {
                            int ival = (int)v;
                            m_Animator.SetInteger(p.name, ival);
                        }
                        break;
                }
            }
            for (int i = 0; i < m_SavedCurrentStates.Count; i++)
            {
                AnimatorStateInfo state = m_SavedCurrentStates[i];
                m_Animator.CrossFade(state.fullPathHash, 0.0f, i, state.normalizedTime);
            }
        }
#endif

        /// <summary>
        /// starts ragdoll flag by adding velocity to chosen body part index and overall velocity to all parts
        /// </summary>
        /// <param name="part">hit body part indices</param>
        /// <param name="velocityHit">force on hit body part</param>
        /// <param name="velocityOverall">overall force applied on rest of bodyparts</param>
        public void StartRagdoll
            (
            int[] hit_parts = null,
            Vector3? hitForce = null,
            Vector3? overallHitForce = null,
            bool ignoreHitInverval = false
            )
        {
#if DEBUG_INFO
            if (!m_Initialized)
            {
                Debug.LogError("component not initialized.");
                return;
            }
#endif
            m_CurrentEventTime = 0f;
            m_HitReacWhileGettingUp = false;
            m_HitParts = hit_parts;
            m_ForceVel = hitForce;
            m_ForceVelocityOveral = overallHitForce;

            m_IgnoreHitInterval = ignoreHitInverval;
            m_FireRagdoll = true;
        }

        /// <summary>
        /// set hit reaction flag and hit velocity
        /// </summary>
        /// <param name="hit_parts">hit parts indices</param>
        /// <param name="forceVelocity"></param>
        public void StartHitReaction(
            int[] hit_parts,
            Vector3 forceVelocity,
            bool ignoreHitInterval = false)
        {
#if DEBUG_INFO
            if (!m_Initialized)
            {
                Debug.LogError("component not initialized.");
                return;
            }
#endif
            m_HitParts = hit_parts;
            m_ForceVel = forceVelocity;
            m_IgnoreHitInterval = ignoreHitInterval;
            m_FireHitReaction = true;
        }

        //        /// <summary>
        //        /// disable ragdoll and transition to mechanim animations
        //        /// and reset all extra body forces
        //        /// </summary>
        public void BlendToMecanim()
        {
#if DEBUG_INFO
            if (!m_Initialized)
            {
                Debug.LogError("component not initialized.");
                return;
            }
#endif

            foreach (BodyPartInfo b in m_BodyParts)
            {
                b.extraForce = Vector3.zero;
            }
            m_FireAnimatorBlend = true;
        }

        /// <summary>
        /// get info on body part based on index
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public BodyPartInfo getBodyPartInfo(int part)
        {
#if DEBUG_INFO
            if (!m_Initialized)
            {
                Debug.LogError("component not initialized.");
                return null;
            }
            if (m_BodyParts == null) { Debug.LogError("object cannot be null."); return null; }
            if (part >= m_BodyParts.Length) { Debug.LogError("Index out of range."); return null; }
#else
            if (m_BodyParts == null) { return null; }
            if (part >= m_BodyParts.Length) { return null; }
#endif

            return m_BodyParts[part];
        }

        // animator event
        void OnGetUp(AnimationEvent e)
        {
            m_GettingUp = false;
            m_Animator.updateMode = m_InitialMode; // revert to original update mode
            m_Animator.applyRootMotion = m_InitialRootMotion;
            m_state = RagdollState.Animated;
            m_HitReacWhileGettingUp = false;


            if (m_OnGetUp != null)
                m_OnGetUp();
            if (m_LastEvent != null)
                m_LastEvent();
        }


    }
}
