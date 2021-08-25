// © 2015 Mario Lelas
using UnityEngine;

namespace MLSpace
{
    /// <summary>
    /// ragdoll interface for ragdoll users
    /// </summary>
    public interface IRagdollUser
    {
        /// <summary>
        /// gets reference to ragdoll manager
        /// </summary>
        RagdollManager RagdollManager { get; }


        /// <summary>
        /// gets bounds of character
        /// </summary>
        Bounds Bound { get; }

        /// <summary>
        /// begin hit reaction
        /// </summary>
        /// <param name="hitParts"></param>
        /// <param name="hitForce"></param>
        void StartHitReaction(int[] hitParts, Vector3 hitForce);


        /// <summary>
        /// begin ragdoll
        /// </summary>
        /// <param name="bodyParts"></param>
        /// <param name="bodyPartForce"></param>
        /// <param name="overallForce"></param>
        void StartRagdoll(int[] bodyParts, Vector3 bodyPartForce, Vector3 overallForce);

        /// <summary>
        /// gets and sets flag to ignore hits
        /// </summary>
        bool IgnoreHit { get; set; }
    } 
}
