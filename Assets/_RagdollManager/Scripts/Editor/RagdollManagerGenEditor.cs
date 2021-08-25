// © 2016 Mario Lelas
using UnityEngine;
using UnityEditor;

namespace MLSpace
{
    [CustomEditor(typeof(RagdollManagerGen))]
    public class RagdollManagerGenEditor : Editor
    {
        // unity OnInspectorGUI method
        public override void OnInspectorGUI()
        {
            RagdollManagerGen ragMan = (RagdollManagerGen)target;

            DrawDefaultInspector();

            if (ragMan.hitInterval == RagdollManagerHum.HitIntervals.Timed)
            {
                float hitInterval = (float)EditorGUILayout.FloatField("Hit Interval", ragMan.hitTimeInterval);
                ragMan.hitTimeInterval = hitInterval;
            }


            bool addColliderScripts = GUILayout.Button("Add Collider Scripts");
            if (addColliderScripts)
            {
                ragMan.addBodyColliderScripts();
            }
            bool removeRagdoll = GUILayout.Button("Remove Ragdoll");
            if (removeRagdoll)
            {

                for (int i = 0; i < ragMan.RagdollBones .Length ; i++)
                {
                    Transform t = ragMan.RagdollBones[i];
                    if (!t) continue;
                    CharacterJoint[] t_joints = t.GetComponents<CharacterJoint>();
                    Collider[] t_cols = t.GetComponents<Collider>();
                    Rigidbody[] t_rbs = t.GetComponents<Rigidbody>();
                    BodyColliderScript[] t_bcs = t.GetComponents<BodyColliderScript>();
                    foreach (CharacterJoint cj in t_joints)
                        DestroyImmediate(cj);
                    foreach (Collider c in t_cols)
                        DestroyImmediate(c);
                    foreach (Rigidbody rb in t_rbs)
                        DestroyImmediate(rb);
                    foreach (BodyColliderScript b in t_bcs)
                        DestroyImmediate(b);
                    ragMan.RagdollBones[i] = null;
                }
                ragMan.RagdollBones = null;

                EditorUtility.SetDirty(ragMan);
                serializedObject.ApplyModifiedProperties();
            }


            if (GUI.changed)
            {
                EditorUtility.SetDirty(ragMan);
                serializedObject.ApplyModifiedProperties();
            }
        }

    } 
}
