// © 2016 Mario Lelas

#define CALC_CUSTOM_VELOCITY_KINEMATIC

using UnityEngine;
using System.Collections.Generic;

namespace MLSpace
{
    /// <summary>
    /// body parts enum
    /// </summary>
    public enum BodyParts : int
    {
        Spine = 0,
        Chest,
        Head,
        LeftShoulder,
        RightShoulder,
        LeftElbow,
        RightElbow,
        LeftHip,
        RightHip,
        LeftKnee,
        RightKnee,
        BODY_PART_COUNT,
        None,
    }

    /// <summary>
    /// ragdoll and hit reaction manager
    /// </summary>
    public class RagdollManagerHum : RagdollManager
    {
        /// <summary>
        /// use get up animation after ragdoll
        /// </summary>
        [Tooltip("Use get up animation after ragdoll ?"), HideInInspector]
        public bool enableGetUpAnimation = true;

        /// <summary>
        /// name of get up from back animation state clip 
        /// </summary>
        [Tooltip("Name of 'get up from back' animation state."), HideInInspector]
        public string nameOfGetUpFromBackState = "GetUpBack";

        /// <summary>
        /// name of get up from front animation state clip 
        /// </summary>
        [Tooltip("Name of 'get up from front' animation state."), HideInInspector]
        public string nameOfGetUpFromFrontState = "GetUpFront";

        // transform used for getting up orientation
        // forward axis must point towards character front
        protected Transform m_OrientTransform;

        /// <summary>
        /// gets number of bodyparts
        /// </summary>
        public override int BodypartCount
        {
            get { return (int)BodyParts.BODY_PART_COUNT; }
        }

        /// <summary>
        /// initialize class instance
        /// </summary>
        public override void Initialize()
        {
            if (m_Initialized) return;

            base.Initialize();

            m_Animator = GetComponent<Animator>();
            if (!m_Animator) { Debug.LogError("object cannot be null."); return; }

#if DEBUG_INFO
            if (!m_Animator.avatar.isHuman)
            {
                Debug.LogError("character avatar must be of humanoid type");
                return;
            }
            if (!m_Animator.avatar.isValid)
            {
                Debug.LogError("character avatar not valid.");
                return;
            }
#endif



            // keep track of colliders and rigid bodies
            m_BodyParts = new BodyPartInfo[(int)BodyParts.BODY_PART_COUNT];
            m_AnimatedRotations = new Quaternion[(int)BodyParts.BODY_PART_COUNT];

#if DEBUG_INFO
            if (!RagdollBones[(int)BodyParts.Spine]) { Debug.LogError("cannot find transform - spineBone  " + this.name); return; }
            if (!RagdollBones[(int)BodyParts.Chest]) { Debug.LogError("cannot find transform - chestBone  " + this.name); return; }
            if (!RagdollBones[(int)BodyParts.Head]) { Debug.LogError("cannot find transform - headBone  " + this.name); return; }
            if (!RagdollBones[(int)BodyParts.LeftShoulder]) { Debug.LogError("cannot find transform - lShoulderBone  " + this.name); return; }
            if (!RagdollBones[(int)BodyParts.LeftElbow]) { Debug.LogError("cannot find transform - lElbowBone  " + this.name); return; }
            if (!RagdollBones[(int)BodyParts.RightShoulder]) { Debug.LogError("cannot find transform - rShoulderBone  " + this.name); return; }
            if (!RagdollBones[(int)BodyParts.RightElbow]) { Debug.LogError("cannot find transform - rElbowBone  " + this.name); return; }
            if (!RagdollBones[(int)BodyParts.LeftHip]) { Debug.LogError("cannot find transform - lHipBone  " + this.name); return; }
            if (!RagdollBones[(int)BodyParts.LeftKnee]) { Debug.LogError("cannot find transform - lKneeBone  " + this.name); return; }
            if (!RagdollBones[(int)BodyParts.RightHip]) { Debug.LogError("cannot find transform - rHipBone  " + this.name); return; }
            if (!RagdollBones[(int)BodyParts.RightKnee]) { Debug.LogError("cannot find transform - rKneeBone  " + this.name); return; }
#endif
            
            bool ragdollComplete = true;
            for (int i = 0; i < (int)BodyParts.BODY_PART_COUNT; ++i)
            {
                Rigidbody rb = RagdollBones[i].GetComponent<Rigidbody>();
                Collider col = RagdollBones[i].GetComponent<Collider>();
                if (rb == null || col == null)
                {
                    ragdollComplete = false;
#if DEBUG_INFO
                    Debug.LogError("missing ragdoll part: " + ((BodyParts)i).ToString());
#endif
                }
                m_BodyParts[i] = new BodyPartInfo();
                m_BodyParts[i].transform = RagdollBones[i];
                m_BodyParts[i].rigidBody = rb;
                m_BodyParts[i].collider = col;
                m_BodyParts[i].bodyPart = (BodyParts)i;
                m_BodyParts[i].index = i;
                m_BodyParts[i].orig_parent = RagdollBones[i].parent;

            }
            if (!ragdollComplete) { Debug.LogError("ragdoll is incomplete or missing"); return; }

            /* 
                NOTE:

                m_OrientTransform should be hips transform with forward vector 
                pointing forwards of character , 
                but I found out that not all models hips bone transform are oriented that way ( ?? ).
                So I am creating new object as m_OrientTransform to be  oriented as character, 
                but positioned on hip transform.

                If your hip bone is oriented so its looking in character forward directioon,
                you can assign hip transform as m_OrientTransform and not create new object.
                Or you can make m_OrientTransform field public and assign orient transform as you wish.

                I made it this way so it would be less setup for users.

            */

            GameObject orientTransformObj = new GameObject("OrientTransform");
            orientTransformObj.transform.position = RagdollBones[(int)BodyParts.Spine].position;
            orientTransformObj.transform.rotation = this.transform.rotation;
            orientTransformObj.transform.SetParent(RagdollBones[(int)BodyParts.Spine]);
            m_OrientTransform = orientTransformObj.transform;

            m_InitialMode = m_Animator.updateMode;
            m_InitialRootMotion = m_Animator.applyRootMotion;
            m_RootTransform = m_BodyParts[(int)BodyParts.Spine].transform ;


            List<int> constraint_indices = new List<int>();
            constraint_indices.Add((int)BodyParts.LeftKnee);
            constraint_indices.Add((int)BodyParts.RightKnee);
            createConstraints(constraint_indices);

            

            m_Initialized = true;

            disableRagdoll();
        }

        /// <summary>
        /// setup body collider scripts on body parts automaticly
        /// </summary>
        /// <returns></returns>
        public override bool addBodyColliderScripts()
        {

            Animator anim = GetComponent<Animator>();

            if (!anim)
            {
                Debug.LogError("addBodyColliderScripts() FAILED. Cannot find 'Animator' component. " +
                    this.name);
                return false;
            }

#if DEBUG_INFO
            if (RagdollBones == null) { Debug.LogError("RagdollBones object cannot be null."); return false; }
#endif

            // removing existing ones
            for (int i = 0; i < RagdollBones.Length; i++)
            {
                Transform t = RagdollBones[i];
                if (!t) continue;
                BodyColliderScript[] t_bcs = t.GetComponents<BodyColliderScript>();
                foreach (BodyColliderScript b in t_bcs)
                    DestroyImmediate(b);
            }


            Transform spine1T = RagdollBones[(int)BodyParts.Spine];
            if (!spine1T) spine1T = anim.GetBoneTransform(HumanBodyBones.Hips);
            BodyColliderScript spineBCS = spine1T.GetComponent<BodyColliderScript>();
            spine1T.gameObject.layer = LayerMask.NameToLayer("ColliderLayer");
            if (!spineBCS)
            {
                spineBCS = spine1T.gameObject.AddComponent<BodyColliderScript>();
                if (spineBCS.Initialize())
                {
                    spineBCS.index = (int)BodyParts.Spine;
                    spineBCS.bodyPart = BodyParts.Spine;
                    spineBCS.critical = false;
                    spineBCS.ParentObject = this.gameObject;
                    spineBCS.SetParentRagdollManager(this);
                    Debug.Log("added hips collider script for " + this.name + " on " + spineBCS.name);
                }
                else
                {
                    Debug.LogError("initializing collider script on " + spine1T.name + " FAILED.");
                }
            }
#if DEBUG_INFO
            else Debug.LogWarning("hips collider exists for " + this.name + " on " + spineBCS.name);
#endif


            Transform chestT = RagdollBones[(int)BodyParts.Chest];
            if (!chestT) chestT = anim.GetBoneTransform(HumanBodyBones.Chest);
            BodyColliderScript chestBCS = chestT.GetComponent<BodyColliderScript>();
            chestT.gameObject.layer = LayerMask.NameToLayer("ColliderLayer");
            if (!chestBCS)
            {
                chestBCS = chestT.gameObject.AddComponent<BodyColliderScript>();
                if (chestBCS.Initialize())
                {
                    chestBCS.index = (int)BodyParts.Chest;
                    chestBCS.bodyPart = BodyParts.Chest;
                    chestBCS.critical = false;
                    chestBCS.ParentObject = this.gameObject;
                    chestBCS.SetParentRagdollManager(this);
                    Debug.Log("added chest collider script for " + this.name + " on " + chestBCS.name);
                }
                else
                {
                    Debug.LogError("initializing collider script on " + chestT.name + " FAILED.");
                }
            }
#if DEBUG_INFO
            else Debug.LogWarning("chest collider exists for " + this.name + " on " + chestBCS.name);
#endif


            Transform headT = RagdollBones[(int)BodyParts.Head];
            if (!headT) headT = anim.GetBoneTransform(HumanBodyBones.Head);
            BodyColliderScript headBCS = headT.GetComponent<BodyColliderScript>();
            headT.gameObject.layer = LayerMask.NameToLayer("ColliderLayer");
            if (!headBCS)
            {
                headBCS = headT.gameObject.AddComponent<BodyColliderScript>();
                if (headBCS.Initialize())
                {
                    headBCS.index = (int)BodyParts.Head;
                    headBCS.bodyPart = BodyParts.Head;
                    headBCS.critical = true;
                    headBCS.ParentObject = this.gameObject;
                    headBCS.SetParentRagdollManager(this);
                    Debug.Log("added head collider script for " + this.name + " on " + headBCS.name);
                }
                else
                {
                    Debug.LogError("initializing collider script on " + headT.name + " FAILED.");
                }
            }
#if DEBUG_INFO
            else Debug.LogWarning("head collider exists for " + this.name + " on " + headBCS.name);
#endif

            Transform xform = RagdollBones[(int)BodyParts.LeftHip];
            if (!xform) xform = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            BodyColliderScript BCS = xform.GetComponent<BodyColliderScript>();
            xform.gameObject.layer = LayerMask.NameToLayer("ColliderLayer");
            if (!BCS)
            {
                BCS = xform.gameObject.AddComponent<BodyColliderScript>();
                if (BCS.Initialize())
                {
                    BCS.index = (int)BodyParts.LeftHip;
                    BCS.bodyPart = BodyParts.LeftHip;
                    BCS.critical = false;
                    BCS.ParentObject = this.gameObject;
                    BCS.SetParentRagdollManager(this);
                    Debug.Log("added left hip collider script for " + this.name + " on " + BCS.name);
                }
                else
                {
                    Debug.LogError("initializing collider script on " + xform.name + " FAILED.");
                }
            }
#if DEBUG_INFO
            else Debug.LogWarning("left hip  collider exists for " + this.name + " on " + BCS.name);
#endif


            xform = RagdollBones[(int)BodyParts.LeftKnee];
            if (!xform) xform = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            BCS = xform.GetComponent<BodyColliderScript>();
            xform.gameObject.layer = LayerMask.NameToLayer("ColliderLayer");
            if (!BCS)
            {
                BCS = xform.gameObject.AddComponent<BodyColliderScript>();
                if (BCS.Initialize())
                {
                    BCS.index = (int)BodyParts.LeftKnee;
                    BCS.bodyPart = BodyParts.LeftKnee;
                    BCS.critical = false;
                    BCS.ParentObject = this.gameObject;
                    BCS.SetParentRagdollManager(this);
                    Debug.Log("added left knee collider script for " + this.name + " on " + BCS.name);
                }
                else
                {
                    Debug.LogError("initializing collider script on " + xform.name + " FAILED.");
                }
            }
#if DEBUG_INFO
            else Debug.LogWarning("left knee collider exists for " + this.name + " on " + BCS.name);
#endif


            xform = RagdollBones[(int)BodyParts.RightHip];
            if (!xform) xform = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            BCS = xform.GetComponent<BodyColliderScript>();
            xform.gameObject.layer = LayerMask.NameToLayer("ColliderLayer");
            if (!BCS)
            {
                BCS = xform.gameObject.AddComponent<BodyColliderScript>();
                if (BCS.Initialize())
                {
                    BCS.index = (int)BodyParts.RightHip;
                    BCS.bodyPart = BodyParts.RightHip;
                    BCS.critical = false;
                    BCS.ParentObject = this.gameObject;
                    BCS.SetParentRagdollManager(this);
                    Debug.Log("added right hip collider script for " + this.name + " on " + BCS.name);
                }
                else
                {
                    Debug.LogError("initializing collider script on " + xform.name + " FAILED.");
                }
            }
#if DEBUG_INFO
            else Debug.LogWarning("right hip collider exists for " + this.name + " on " + BCS.name);
#endif


            xform = RagdollBones[(int)BodyParts.RightKnee];
            if (!xform) xform = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            BCS = xform.GetComponent<BodyColliderScript>();
            xform.gameObject.layer = LayerMask.NameToLayer("ColliderLayer");
            if (!BCS)
            {
                BCS = xform.gameObject.AddComponent<BodyColliderScript>();
                if (BCS.Initialize())
                {
                    BCS.index = (int)BodyParts.RightKnee;
                    BCS.bodyPart = BodyParts.RightKnee;
                    BCS.critical = false;
                    BCS.ParentObject = this.gameObject;
                    BCS.SetParentRagdollManager(this);
                    Debug.Log("added right knee collider script for " + this.name + " on " + BCS.name);
                }
                else
                {
                    Debug.LogError("initializing collider script on " + xform.name + " FAILED.");
                }
            }
#if DEBUG_INFO
            else Debug.LogWarning("right knee collider exists for " + this.name + " on " + BCS.name);
#endif


            xform = RagdollBones[(int)BodyParts.LeftShoulder];
            if (!xform) xform = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            BCS = xform.GetComponent<BodyColliderScript>();
            xform.gameObject.layer = LayerMask.NameToLayer("ColliderLayer");
            if (!BCS)
            {
                BCS = xform.gameObject.AddComponent<BodyColliderScript>();
                if (BCS.Initialize())
                {
                    BCS.index = (int)BodyParts.LeftShoulder;
                    BCS.bodyPart = BodyParts.LeftShoulder;
                    BCS.critical = false;
                    BCS.ParentObject = this.gameObject;
                    BCS.SetParentRagdollManager(this);
                    Debug.Log("added left shoulder collider script for " + this.name + " on " + BCS.name);
                }
                else
                {
                    Debug.LogError("initializing collider script on " + xform.name + " FAILED.");
                }
            }
#if DEBUG_INFO
            else Debug.LogWarning("left shoulder collider exists for " + this.name + " on " + BCS.name);
#endif


            xform = RagdollBones[(int)BodyParts.LeftElbow];
            if (!xform) xform = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            BCS = xform.GetComponent<BodyColliderScript>();
            xform.gameObject.layer = LayerMask.NameToLayer("ColliderLayer");
            if (!BCS)
            {
                BCS = xform.gameObject.AddComponent<BodyColliderScript>();
                if (BCS.Initialize())
                {
                    BCS.index = (int)BodyParts.LeftElbow;
                    BCS.bodyPart = BodyParts.LeftElbow;
                    BCS.critical = false;
                    BCS.ParentObject = this.gameObject;
                    BCS.SetParentRagdollManager(this);
                    Debug.Log("added left elbow collider script for " + this.name + " on " + BCS.name);
                }
                else
                {
                    Debug.LogError("initializing collider script on " + xform.name + " FAILED.");
                }
            }
#if DEBUG_INFO
            else Debug.LogWarning("left elbow collider exists for " + this.name + " on " + BCS.name);
#endif


            xform = RagdollBones[(int)BodyParts.RightShoulder];
            if (!xform) xform = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
            BCS = xform.GetComponent<BodyColliderScript>();
            xform.gameObject.layer = LayerMask.NameToLayer("ColliderLayer");
            if (!BCS)
            {
                BCS = xform.gameObject.AddComponent<BodyColliderScript>();
                if (BCS.Initialize())
                {
                    BCS.index = (int)BodyParts.RightShoulder;
                    BCS.bodyPart = BodyParts.RightShoulder;
                    BCS.critical = false;
                    BCS.ParentObject = this.gameObject;
                    BCS.SetParentRagdollManager(this);
                    Debug.Log("added right shoulder collider script for " + this.name + " on " + BCS.name);
                }
                else
                {
                    Debug.LogError("initializing collider script on " + xform.name + " FAILED.");
                }
            }
#if DEBUG_INFO
            else Debug.LogWarning("right shoulder collider exists for " + this.name + " on " + BCS.name);
#endif


            xform = RagdollBones[(int)BodyParts.RightElbow];
            if (!xform) xform = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
            BCS = xform.GetComponent<BodyColliderScript>();
            xform.gameObject.layer = LayerMask.NameToLayer("ColliderLayer");
            if (!BCS)
            {
                BCS = xform.gameObject.AddComponent<BodyColliderScript>();
                if (BCS.Initialize())
                {
                    BCS.index = (int)BodyParts.RightElbow;
                    BCS.bodyPart = BodyParts.RightElbow;
                    BCS.critical = false;
                    BCS.ParentObject = this.gameObject;
                    BCS.SetParentRagdollManager(this);
                    Debug.Log("added right elbow collider script for " + this.name + " on " + BCS.name);
                }
                else
                {
                    Debug.LogError("initializing collider script on " + xform.name + " FAILED.");
                }
            }
#if DEBUG_INFO
            else Debug.LogWarning("right elbow collider exists for " + this.name + " on " + BCS.name);
#endif

            return true;
        }

        /// <summary>
        /// get info on body part based on humaniod enumeration
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public BodyPartInfo getBodyPartInfo(BodyParts part)
        {
#if DEBUG_INFO
            if (!m_Initialized)
            {
                Debug.LogError("component not initialized.");
                return null;
            }
            if (m_BodyParts == null) { Debug.LogError("object cannot be null."); return null; }
#endif
            if (part < BodyParts.BODY_PART_COUNT)
                return m_BodyParts[(int)part];
            else return null;
        }


#region UNITY_METHODS

        // Unity start method
        void Start()
        {
            // initialize all
            Initialize();
        }

#if CALC_CUSTOM_VELOCITY_KINEMATIC
        void Update()
        {
#if DEBUG_INFO
            if (!m_Initialized)
            {
                Debug.LogError("component not initialized.");
                return;
            }
#endif
            if (Time.deltaTime > 0)
            {
                // calculating rigid bodies velocity when in kinematic state
                foreach (BodyPartInfo b in m_BodyParts)
                {
                    b.customVelocity = b.rigidBody.position - b.previusPosition;
                    b.customVelocity /= Time.deltaTime;
                    b.previusPosition = b.rigidBody.position;
                }
            }
        }
#endif

        /// <summary>
        /// Unity late update method
        /// </summary>
        void LateUpdate()
        {
#if DEBUG_INFO
            if (!m_Initialized)
            {
                Debug.LogError("component not initialized.");
                return;
            }
#endif

            m_AcceptHit = true;
            switch (hitInterval)
            {
                case HitIntervals.OnBlend:
                    if (m_state < RagdollState.Blend)
                        m_AcceptHit = false;
                    break;
                case HitIntervals.OnGettingUp:
                    if (m_state < RagdollState.GettingUpAnim)
                        m_AcceptHit = false;
                    break;
                case HitIntervals.OnAnimated:
                    if (m_state < RagdollState.Animated)
                        m_AcceptHit = false;
                    break;
                case HitIntervals.Timed:
                    if (m_CurrentHitTime < hitTimeInterval)
                    {
                        m_AcceptHit = false;
                    }
                    break;
            }

            m_CurrentHitTime += Time.deltaTime;

            if (m_FireHitReaction)
            {
                startHitReaction();
                m_FireHitReaction = false;
            }

            if (m_FireRagdoll)
            {
                startRagdoll();
                m_FireRagdoll = false;
            }

            if (m_FireAnimatorBlend)
            {
                startTransition();
                m_FireAnimatorBlend = false;
            }



            switch (m_state)
            {
                case RagdollState.Ragdoll:
                    updateRagdoll();
                    break;
                case RagdollState.Blend:
                    updateTransition();
                    break;
            }
        }

#if DEBUG_INFO
        void OnDrawGizmos()
        {
            if (m_BodyParts == null) return;
            if (m_BodyParts.Length == 0) return;

            if (!m_Animator)
                m_Animator = GetComponent<Animator>();

            Gizmos.color = Color.white;
            Gizmos.DrawLine(m_BodyParts[(int)BodyParts.Spine].transform.position,
                m_BodyParts[(int)BodyParts.Chest].transform.position);
            Gizmos.DrawLine(m_BodyParts[(int)BodyParts.Chest].transform.position,
                m_BodyParts[(int)BodyParts.Head].transform.position);
            Gizmos.DrawLine(m_BodyParts[(int)BodyParts.Chest].transform.position,
                m_BodyParts[(int)BodyParts.LeftShoulder].transform.position);
            Gizmos.DrawLine(m_BodyParts[(int)BodyParts.Chest].transform.position,
                m_BodyParts[(int)BodyParts.RightShoulder].transform.position);
            Gizmos.DrawLine(m_BodyParts[(int)BodyParts.LeftShoulder].transform.position,
                m_BodyParts[(int)BodyParts.LeftElbow].transform.position);
            Gizmos.DrawLine(m_BodyParts[(int)BodyParts.RightShoulder].transform.position,
                m_BodyParts[(int)BodyParts.RightElbow].transform.position);
            Gizmos.DrawLine(m_BodyParts[(int)BodyParts.Spine].transform.position,
                m_BodyParts[(int)BodyParts.LeftHip].transform.position);
            Gizmos.DrawLine(m_BodyParts[(int)BodyParts.Spine].transform.position,
                m_BodyParts[(int)BodyParts.RightHip].transform.position);
            Gizmos.DrawLine(m_BodyParts[(int)BodyParts.LeftHip].transform.position,
                m_BodyParts[(int)BodyParts.LeftKnee].transform.position);
            Gizmos.DrawLine(m_BodyParts[(int)BodyParts.RightHip].transform.position,
                m_BodyParts[(int)BodyParts.RightKnee].transform.position);

            Transform lhand = m_BodyParts[(int)BodyParts.LeftElbow].transform.GetChild(0);
            Transform rhand = m_BodyParts[(int)BodyParts.RightElbow].transform.GetChild(0);
            Transform lfoot = m_BodyParts[(int)BodyParts.LeftKnee].transform.GetChild(0);
            Transform rfoot = m_BodyParts[(int)BodyParts.RightKnee].transform.GetChild(0);

            Gizmos.DrawLine(m_BodyParts[(int)BodyParts.LeftElbow].transform.position, lhand.position);
            Gizmos.DrawLine(m_BodyParts[(int)BodyParts.RightElbow].transform.position, rhand.position);
            Gizmos.DrawLine(m_BodyParts[(int)BodyParts.LeftKnee].transform.position, lfoot.position);
            Gizmos.DrawLine(m_BodyParts[(int)BodyParts.RightKnee].transform.position, rfoot.position);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(m_Animator.GetBoneTransform(HumanBodyBones.Hips).position,
                m_Animator.GetBoneTransform(HumanBodyBones.Chest).position);
            Gizmos.DrawLine(m_Animator.GetBoneTransform(HumanBodyBones.Chest).position,
                m_Animator.GetBoneTransform(HumanBodyBones.Head).position);
            Gizmos.DrawLine(m_Animator.GetBoneTransform(HumanBodyBones.Chest).position,
                m_Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position);
            Gizmos.DrawLine(m_Animator.GetBoneTransform(HumanBodyBones.Chest).position,
                m_Animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position);
            Gizmos.DrawLine(m_Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position,
                m_Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position);
            Gizmos.DrawLine(m_Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position,
                m_Animator.GetBoneTransform(HumanBodyBones.LeftHand).position);
            Gizmos.DrawLine(m_Animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position,
                m_Animator.GetBoneTransform(HumanBodyBones.RightLowerArm).position);
            Gizmos.DrawLine(m_Animator.GetBoneTransform(HumanBodyBones.RightLowerArm).position,
                m_Animator.GetBoneTransform(HumanBodyBones.RightHand).position);
            Gizmos.DrawLine(m_Animator.GetBoneTransform(HumanBodyBones.Hips).position,
                m_Animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position);
            Gizmos.DrawLine(m_Animator.GetBoneTransform(HumanBodyBones.Hips).position,
                m_Animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position);
            Gizmos.DrawLine(m_Animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position,
                m_Animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position);
            Gizmos.DrawLine(m_Animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position,
                m_Animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).position);
            Gizmos.DrawLine(m_Animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position,
                m_Animator.GetBoneTransform(HumanBodyBones.LeftFoot).position);
            Gizmos.DrawLine(m_Animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).position,
                m_Animator.GetBoneTransform(HumanBodyBones.RightFoot).position);
        }
#endif

#endregion

        // update ragdolled mode
        private void updateRagdoll()
        {
#if DEBUG_INFO
            if (m_BodyParts == null) { Debug.LogError("object cannot be null."); return; }
#endif
            // if in ragdoll check timed event
            if (m_HitReactionUnderway)
            {
                m_HitReactionTimer += Time.deltaTime;
                if (m_HitReactionTimer >= m_HitReactionMax)
                {
                    m_HitReactionUnderway = false;
                    if (m_InternalOnHit != null)
                    {
                        m_InternalOnHit();
                        return;
                    }
                }
            }
            else
            {
                m_CurrentEventTime += Time.deltaTime;
                if (m_CurrentEventTime >= m_RagdollEventTime)
                {
                    if (m_OnTimeEnd != null)
                    {
                        m_OnTimeEnd();
                        return;
                    }
                }
            }
            for (int i = 0; i < m_BodyParts.Length; i++)
            {
                BodyPartInfo bpi = m_BodyParts[i];
                bpi.rigidBody.AddForce(bpi.extraForce, m_ForceMode);
            }

        }

        // update in transition mode
        private void updateTransition()
        {
#if DEBUG_INFO
            if (m_BodyParts == null) { Debug.LogError("object cannot be null."); return; }
            if (!m_Animator) Debug.LogError("object cannot be null");
#endif

            m_AnimatedRootPosition = m_BodyParts[(int)BodyParts.Spine].transform.position;
            for (int i = 0; i < (int)BodyParts.BODY_PART_COUNT; i++)
            {
                m_AnimatedRotations[i] = m_BodyParts[i].transform.rotation;
            }

            m_CurrentBlendTime += Time.deltaTime;
            float blendAmount = m_CurrentBlendTime / blendTime;
            blendAmount = Mathf.Clamp01(blendAmount);

            BodyPartInfo spine = m_BodyParts[(int)BodyParts.Spine];
            spine.transform.position = Vector3.Lerp(spine.transitionPosition, m_AnimatedRootPosition, blendAmount);
            for (int i = 0; i < m_BodyParts.Length; i++)
            {
                BodyPartInfo b = m_BodyParts[i];
                b.transform.rotation = Quaternion.Slerp(b.transitionRotation, m_AnimatedRotations[i], blendAmount);
            }

            if (m_CurrentBlendTime >= blendTime)
            {

                m_Animator.updateMode = m_InitialMode; // revert to original update mode
                m_Animator.applyRootMotion = m_InitialRootMotion;

                if (m_GettingUp && enableGetUpAnimation)
                {
                    m_state = RagdollState.GettingUpAnim;
                }
                else
                {
                    if (m_LastEvent != null)
                        m_LastEvent();
                    m_state = RagdollState.Animated;
                    m_HitReacWhileGettingUp = false;
                }
                if (OnBlendEnd != null)
                {
                    OnBlendEnd(); // start on ragdoll end event if exists
                }
            }
        }

        // start ragdoll method
        private void startRagdoll()
        {
#if DEBUG_INFO
            if (m_BodyParts == null) { Debug.LogError("object cannot be null."); return; }
            if (!m_Animator) Debug.LogError("object cannot be null");
#endif


            if (!m_AcceptHit && !m_IgnoreHitInterval) return;

            enableRagdoll(true);

#if SAVE_ANIMATOR_STATES
            saveAnimatorStates();
#endif

            m_HitReacWhileGettingUp = false;
            m_Animator.enabled = false; //disable animation
            m_state = RagdollState.Ragdoll;
            m_FullRagdoll = true;
            m_CurrentHitTime = 0.0f;

            if (!useJoints)
            {
                for (int i = 1; i < BodypartCount; i++)
                {
                    BodyPartInfo b = m_BodyParts [i];
                    b.transform.SetParent(transform);
                }
            }

            if (m_ForceVelocityOveral.HasValue)
            {
                for (int i = 0; i < m_BodyParts.Length; i++)
                {
                    m_BodyParts[i].rigidBody.velocity = m_ForceVelocityOveral.Value;
                }
            }
#if CALC_CUSTOM_VELOCITY_KINEMATIC
            else
            {
                for (int i = 0; i < m_BodyParts.Length; i++)
                {
                    BodyPartInfo b = m_BodyParts[i];
                    b.rigidBody.velocity = b.customVelocity;
                }
            }
#endif

                if (m_HitParts != null)
            {
                if (m_ForceVel.HasValue)
                {
                    for (int i = 0; i < m_HitParts.Length; i++)
                    {
                        BodyPartInfo b = m_BodyParts[m_HitParts[i]];
                        b.rigidBody.velocity = m_ForceVel.Value;
                    }
                }
            }

            m_ForceVel = null;
            m_GettingUp = true;
            m_IgnoreHitInterval = false;
            m_ForceVelocityOveral = null;
            m_HitParts = null;


            if (m_OnHit != null)
                m_OnHit();
        }

        // start hit reaction method
        private void startHitReaction()
        {
#if DEBUG_INFO
            if (m_BodyParts == null) { Debug.LogError("object cannot be null."); return; }
            if (!m_Animator) Debug.LogError("object cannot be null");
#endif
            if (m_HitParts == null)
            {
#if DEBUG_INFO
                Debug.LogWarning("Ragdoll::StartHitReaction must have body parts hit passed.");
#endif
                return;
            }

            if (!m_AcceptHit && !m_IgnoreHitInterval)
            {
                return;
            }

            if (m_state == RagdollState.Ragdoll)
            {
                startRagdoll();
                return;
            }
#if SAVE_ANIMATOR_STATES
            saveAnimatorStates();
#endif

            if (m_GettingUp) m_HitReacWhileGettingUp = true;

            m_Animator.enabled = false;
            m_state = RagdollState.Ragdoll;
            m_HitReactionTimer = 0.0f;
            m_CurrentHitTime = 0.0f;

            if (!useJoints)
            {
                for (int i = 1; i < BodypartCount; i++)
                {
                    BodyPartInfo b = m_BodyParts[i]; 
                    b.transform.SetParent(transform);
                }
            }

            if (m_ForceVel.HasValue)
                m_HitReactionMax = m_ForceVel.Value.magnitude / (weight * hitResistance);
            m_HitReactionUnderway = true;


            bool swoop = false;
            if (m_ConstraintIndices.Count > 0)
            {
                swoop = true;
                for (int i = 0; i < m_ConstraintIndices.Count; i++)
                {
                    bool exists = System.Array.Exists(m_HitParts, elem => elem == m_ConstraintIndices[i]);
                    if (!exists)
                    {
                        swoop = false;
                        break;
                    }
                }
            }

            if (!swoop)
            {
                float force = m_ForceVel.HasValue ? m_ForceVel.Value.magnitude : 0.0f;
                m_FullRagdoll = force > hitReactionTolerance;
                if (m_FullRagdoll)
                {
                    m_CurrentEventTime = 0.0f;
                    startRagdoll();
                }
                else
                {
                    enableRagdoll(false);
                    for (int i = 0; i < m_HitParts.Length; i++)
                    {
                        applyHitReactionOnBodyPart(force, m_HitParts[i]);
                    }
                }
                if (m_ForceVel.HasValue)
                {
                    for (int i = 0; i < m_HitParts.Length; i++)
                    {
                        m_BodyParts[m_HitParts[i]].rigidBody.velocity = m_ForceVel.Value;
                    }
                }
                if (m_FullRagdoll)
                    m_GettingUp = true;
                m_IgnoreHitInterval = false;
                m_ForceVel = null;
                m_ForceVelocityOveral = null;
                m_HitParts = null;
                if (m_OnHit != null)
                    m_OnHit();
            }
            else
            {
                m_CurrentEventTime = 0.0f;
                startRagdoll();
            }
        }

        // apply hit reaction on single body part
        private void applyHitReactionOnBodyPart(float force, int hitPart)
        {
            for (int i = 0; i < m_ConstraintIndices.Count; i++)
            {
                if (hitPart != m_ConstraintIndices[i])
                {
                    if (useJoints)
                    {
                        Transform T = m_BodyParts[m_ConstraintIndices[i]].transform;
                        Vector3 anchor = Vector3.zero;
                        if (T.childCount > 0)
                        {
                            Transform tChild = T.GetChild(0);
                            anchor = tChild.localPosition;
                        }
                        ConfigurableJoint cfj = m_BodyParts[m_ConstraintIndices[i]].constraintJoint;
                        cfj.xMotion = ConfigurableJointMotion.Locked;
                        cfj.yMotion = ConfigurableJointMotion.Locked;
                        cfj.zMotion = ConfigurableJointMotion.Locked;
                        cfj.anchor = anchor;
                    }
                    else
                    {
                        Rigidbody rb = m_BodyParts[m_ConstraintIndices[i]].rigidBody;
                        rb.isKinematic = true;
                    }
                }
            }
            m_HitReacWhileGettingUp = true;
            m_InternalOnHit = () =>
            {
                m_GettingUpEnableInternal = false;
                m_FireAnimatorBlend = true;
                m_InternalOnHit = null;
            };
        }



        // start blend to animator method
        private void startTransition()
        {
            if (m_state != RagdollState.Ragdoll) return;

#if DEBUG_INFO
            if (!m_Animator) { Debug.LogError("object cannot be null - " + this.name); return; }
            if (m_BodyParts == null) { Debug.LogError("object cannot be null"); return; }
            if (!m_OrientTransform) { Debug.LogError("object cannot be null. - " + this.name); return; }
#endif


            disableRagdoll();



            if (useJoints)
            {
                for (int i = 0; i < m_ConstraintIndices.Count; i++)
                {
                    m_BodyParts[m_ConstraintIndices[i]].constraintJoint.xMotion = ConfigurableJointMotion.Free;
                    m_BodyParts[m_ConstraintIndices[i]].constraintJoint.yMotion = ConfigurableJointMotion.Free;
                    m_BodyParts[m_ConstraintIndices[i]].constraintJoint.zMotion = ConfigurableJointMotion.Free;
                }
            }
            else
            {
                for (int i = 1; i < BodypartCount; i++)
                {
                    BodyPartInfo b = m_BodyParts[i];
                    b.transform.SetParent(b.orig_parent);
                }
            }

            foreach (BodyPartInfo b in m_BodyParts)
            {
                b.transitionRotation = b.transform.rotation;
                b.transitionPosition = b.transform.position;
            }

            

            m_CurrentBlendTime = 0.0f;
            m_Animator.enabled = true; //enable animation
            m_Animator.updateMode = AnimatorUpdateMode.Normal; // set animator update to normal
            m_Animator.applyRootMotion =  true; 
            m_state = RagdollState.Blend;

#if SAVE_ANIMATOR_STATES
            resetAnimatorStates();
#endif

            if (m_GettingUp && !m_HitReacWhileGettingUp)
            {
                m_Animator.applyRootMotion = false; // problems when getting up. must be false

                Vector3 newRootPosition = m_OrientTransform.position;

                // shoot ray to check ground and set new root position on ground
                // comment or delete this if you dont want this feature
                Vector3 raypos = m_OrientTransform.position + Vector3.up * 0.01f;
                Ray ray = new Ray(raypos, Vector3.down);
                
                // ignore colliders
                int layerMask = ~LayerMask.GetMask("ColliderLayer", "ColliderInactiveLayer");

                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 20f, layerMask))
                {
                    newRootPosition.y = hit.point.y;
                    newRootPosition.x = hit.point.x;
                    newRootPosition.z = hit.point.z;
                }

                bool upwards = m_OrientTransform.forward.y > 0.0f;

                if (upwards)
                {

                    if (m_GettingUpEnableInternal && enableGetUpAnimation)
                    {
                        m_Animator.CrossFade(nameOfGetUpFromFrontState, 0.0f, 0, 0.0f);
                        Vector3 _up = -m_OrientTransform.up;
                        _up.y = 0.0f;
                        transform.forward = _up;
                    }
                }
                else
                {
                    if (m_GettingUpEnableInternal && enableGetUpAnimation)
                    {
                        m_Animator.CrossFade(nameOfGetUpFromBackState, 0.0f, 0, 0.0f);
                        Vector3 _up = m_OrientTransform.up;
                        _up.y = 0.0f;
                        transform.forward = _up;
                    }
                }
                transform.position = newRootPosition;
            }

            m_GettingUpEnableInternal = true;

            if (m_OnStartTransition != null)
                m_OnStartTransition();
        }

        /// <summary>
        /// Deprecated. Use method with int[] first parameter.
        /// starts ragdoll flag by adding velocity to chosen body part and overall velocity to all parts
        /// </summary>
        /// <param name="part">hit body parts</param>
        /// <param name="velocityHit">force on hit body parts</param>
        /// <param name="velocityOverall">overall force applied on rest of bodyparts</param>
        public void StartRagdoll
            (
            BodyParts[] hit_parts = null,
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

            if (hit_parts != null)
            {
                m_HitParts = new int[hit_parts.Length];
                for (int i = 0; i < hit_parts.Length; i++)
                    m_HitParts[i] = (int)hit_parts[i];
            }

            m_CurrentEventTime = 0f;

            m_ForceVel = hitForce;
            m_ForceVelocityOveral = overallHitForce;

            m_IgnoreHitInterval = ignoreHitInverval;
            m_FireRagdoll = true;
        }


        /// <summary>
        /// Deprecated. Use method with int[] first parameter.
        /// Set hit reaction flag and hit velocity
        /// </summary>
        /// <param name="hit_parts">hit parts indices</param>
        /// <param name="forceVelocity"></param>
        public void StartHitReaction(
            BodyParts[] hit_parts,
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
            if (hit_parts != null)
            {
                m_HitParts = new int[hit_parts.Length];
                for (int i = 0; i < hit_parts.Length; i++)
                    m_HitParts[i] = (int)hit_parts[i];
            }
            m_ForceVel = forceVelocity;
            m_IgnoreHitInterval = ignoreHitInterval;
            m_FireHitReaction = true;
        }

    }
}
