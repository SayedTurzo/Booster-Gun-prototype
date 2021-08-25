// © 2015 Mario Lelas

#define CALC_CUSTOM_VELOCITY_KINEMATIC

using UnityEngine;
using System.Collections.Generic;

namespace MLSpace
{
    public class RagdollManagerGen : RagdollManager
    {

        /// <summary>
        /// storing bones local positions 
        /// </summary>
        private Vector3[] m_OrigLocalPositions;

        /// <summary>
        /// gets number of bodyparts
        /// </summary>
        public override int BodypartCount
        {
            get
            {
                if (m_BodyParts == null) return 0;
                return m_BodyParts.Length;
            }
        }

        // Use this for initialization
        void Start()
        {
            Initialize();
        }

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

#if CALC_CUSTOM_VELOCITY_KINEMATIC
        void Update()
        {
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
        /// initialize class instance
        /// </summary>
        public override void Initialize()
        {
            if (m_Initialized) return;

            base.Initialize();

            m_Animator = GetComponent<Animator>();
            if (!m_Animator) { Debug.LogError("object cannot be null."); return; }

#if DEBUG_INFO
            if (!m_Animator.avatar.isValid)
            {
                Debug.LogError("character avatar not valid.");
                return;
            }
#endif

            if (RagdollBones == null)
            {
                Debug.LogError("object cannot be null.");
                return;
            }

            if (RagdollBones.Length == 0)
            {
                Debug.LogError("Ragdoll bones not found");
                return;
            }

            // keep track of colliders and rigid bodies
            m_BodyParts = new BodyPartInfo[RagdollBones.Length];
            m_AnimatedRotations = new Quaternion[RagdollBones.Length];
            m_OrigLocalPositions = new Vector3[RagdollBones.Length];

 

#if DEBUG_INFO
            for (int i = 0; i < RagdollBones.Length; i++)
            {
                if (!RagdollBones[i])
                {
                    Debug.LogError("cannot find transform at index  " + i.ToString() + "  " + this.name);
                    return;
                }
            }
#endif

            bool ragdollComplete = true;
            for (int i = 0; i < m_BodyParts.Length; ++i)
            {
                Rigidbody rb = RagdollBones[i].GetComponent<Rigidbody>();
                Collider col = RagdollBones[i].GetComponent<Collider>();
                BodyColliderScript bcs = RagdollBones[i].GetComponent<BodyColliderScript>();
                if (rb == null || col == null)
                {
                    ragdollComplete = false;
#if DEBUG_INFO
                    Debug.LogError("missing ragdoll part at: " + (i).ToString());
#endif
                }
                m_BodyParts[i] = new BodyPartInfo();
                m_BodyParts[i].transform = RagdollBones[i];
                m_BodyParts[i].rigidBody = rb;
                m_BodyParts[i].collider = col;
                m_BodyParts[i].index = bcs.index;
                m_BodyParts[i].bodyPart = bcs.bodyPart;
                m_BodyParts[i].orig_parent = RagdollBones[i].parent;
            }

            if (!ragdollComplete) { Debug.LogError("ragdoll is incomplete or missing"); return; }

            for (int i = 0; i < m_BodyParts.Length; i++)
            {
                m_OrigLocalPositions[i] = m_BodyParts[i].transform.localPosition;
            }

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

            m_InitialMode = m_Animator.updateMode;
            m_InitialRootMotion = m_Animator.applyRootMotion ;
            m_RootTransform = m_BodyParts[0].transform;


            m_Initialized = true;


            List<int> constraints = new List<int>();
            for (int i = 0; i < m_BodyParts.Length; i++)
            {
                if (m_BodyParts[i].bodyPart == BodyParts.LeftKnee ||
                    m_BodyParts[i].bodyPart == BodyParts.RightKnee)
                {
                    constraints.Add(m_BodyParts[i].index);
                }
            }
            createConstraints(constraints);


            disableRagdoll();
        }


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
            if (RagdollBones == null) { Debug.LogError("RagdollBones object cannot be null.");return false; }
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

            for (int i = 0;i<RagdollBones.Length;i++)
            {
                Transform xform = RagdollBones[i];
                if(!xform) { Debug.LogError("object cannot be null.");return false; }
                xform.gameObject.layer = LayerMask.NameToLayer("ColliderLayer");

                BodyColliderScript BCS = xform.GetComponent<BodyColliderScript>();
                if (!BCS)
                {
                    BCS = xform.gameObject.AddComponent<BodyColliderScript>();
                    if (BCS.Initialize())
                    {
                        BCS.bodyPart = BodyParts.None;
                        BCS.critical = false;
                        BCS.ParentObject = this.gameObject;
                        BCS.SetParentRagdollManager(this);
                        BCS.index = i;
                        Debug.Log("added collider script for " + this.name + " at index: " + i + " on " + BCS.name);
                    }
                    else
                    {
                        Debug.LogError("initializing collider script on " + xform.name + " FAILED.");
                    }
                }
#if DEBUG_INFO
                else Debug.LogWarning("collider at index: " + i + " exists for " + this.name + " on " + BCS.name);
#endif
            }
            return true;
        }

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

            m_CurrentBlendTime += Time.deltaTime;
            float blendAmount = m_CurrentBlendTime / blendTime;
            blendAmount = Mathf.Clamp01(blendAmount);


            m_AnimatedRootPosition = m_BodyParts[(int)BodyParts.Spine].transform.position;
            for (int i = 0; i < m_AnimatedRotations.Length; i++)
            {
                m_AnimatedRotations[i] = m_BodyParts[i].transform.rotation;
            }

            BodyPartInfo spine = m_BodyParts[0];
            spine.transform.position = Vector3.Lerp(spine.transitionPosition, m_AnimatedRootPosition, blendAmount);
            spine.transform.rotation = Quaternion.Slerp(spine.transitionRotation, m_AnimatedRotations[0], blendAmount);
            for (int i = 1; i < m_BodyParts.Length; i++)
            {
                BodyPartInfo b = m_BodyParts[i];
                b.transform.localPosition = Vector3.Lerp(b.transform.localPosition, m_OrigLocalPositions[i], blendAmount);
                b.transform.rotation = Quaternion.Slerp(b.transitionRotation, m_AnimatedRotations[i], blendAmount);
            }


            if (m_CurrentBlendTime >= blendTime)
            {
                m_Animator.updateMode = m_InitialMode; // revert to original update mode
                m_Animator.applyRootMotion = m_InitialRootMotion;

                if (m_GettingUp)
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
            m_CurrentEventTime = 0f;
            m_FullRagdoll = true;


            if (m_ForceVelocityOveral.HasValue)
            {
                for (int i = 0; i < m_BodyParts.Length; i++)
                    m_BodyParts[i].rigidBody.velocity = m_ForceVelocityOveral.Value;
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

            if (!useJoints)
            {
                for (int i = 1; i < BodypartCount; i++)
                {
                    BodyPartInfo b = m_BodyParts[i];
                    b.transform.SetParent(transform);
                }
            }

            if (m_ForceVel.HasValue )
                m_HitReactionMax = m_ForceVel.Value .magnitude / (weight * hitResistance);
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

            Vector3 newRootPosition = m_RootTransform.position;

            foreach (BodyPartInfo b in m_BodyParts)
            {
                b.transitionRotation = b.transform.rotation;
                b.transitionPosition = b.transform.position;
            }


            m_CurrentBlendTime = 0.0f;
            m_Animator.enabled = true; //enable animation
            m_Animator.updateMode = AnimatorUpdateMode.Normal; // set animator update to normal
            m_Animator.applyRootMotion = true; // false;
            m_state = RagdollState.Blend;

#if SAVE_ANIMATOR_STATES
            resetAnimatorStates();
#endif
            if (m_GettingUp && !m_HitReacWhileGettingUp)
            {
                m_Animator.applyRootMotion = false; // problems when getting up. must be false

                // shoot ray to check ground and set new root position on ground
                // comment or delete this if you dont want this feature
                Vector3 raypos = newRootPosition + Vector3.up * 0.01f;
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
                transform.position = newRootPosition;
            }
            m_GettingUpEnableInternal = true;

            if (m_OnStartTransition != null)
                m_OnStartTransition();
        }


    } 


}
