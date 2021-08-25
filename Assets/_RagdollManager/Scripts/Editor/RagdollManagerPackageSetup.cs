// © 2015 Mario Lelas
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace MLSpace
{
    /// <summary>
    /// adds menu item that sets up package tags, layers and collision matrix
    /// </summary>
    public class RagdollManagerPackageSetup : MonoBehaviour
    {
        [MenuItem("Tools/Ragdoll Manager Package/Setup Tags Layers")]
        private static void Setup()
        {
            var defines = GetDefinesList(buildTargetGroups[0]);
            if (!defines.Contains("DEBUG_INFO"))
            {
                SetEnabled("DEBUG_INFO", true, false);
                SetEnabled("DEBUG_INFO", true, true);
            }


            const int PLAYERLAYER = 8;
            const int NPCLAYER = 9;
            const int COLLIDERLAYER = 10;
            const int COLLIDERINACTIVELAYER = 11;
            const int FIREBALLLAYER = 12;
            const int TRIGGERLAYER = 13;

            EditorUtils.AddLayer("PlayerLayer", PLAYERLAYER);
            EditorUtils.AddLayer("NPCLayer", NPCLAYER);
            EditorUtils.AddLayer("ColliderLayer", COLLIDERLAYER);
            EditorUtils.AddLayer("FireBallLayer", FIREBALLLAYER);
            EditorUtils.AddLayer("TriggerLayer", TRIGGERLAYER);
            EditorUtils.AddLayer("ColliderInactiveLayer", COLLIDERINACTIVELAYER);

            // trigger ignores all layers except npc and player for this case
            // fire ball layer ignores all but default
            for (int i = 0; i < 32; i++)
            {
                Physics.IgnoreLayerCollision(FIREBALLLAYER, i, true);
                Physics.IgnoreLayerCollision(TRIGGERLAYER, i, true);
                Physics.IgnoreLayerCollision(COLLIDERINACTIVELAYER, i, true);
            }
            Physics.IgnoreLayerCollision(FIREBALLLAYER, 0, false);
            Physics.IgnoreLayerCollision(TRIGGERLAYER, NPCLAYER, false);
            Physics.IgnoreLayerCollision(TRIGGERLAYER, PLAYERLAYER, false);


            EditorUtils.AddTag("NPC");

            InputAxis idleAxis = new InputAxis();
            idleAxis.name = "Idle";
            idleAxis.positiveButton = "f";
            idleAxis.descriptiveName = "Toggle npcs idle mode";
            EditorUtils.AddAxis(idleAxis);

            InputAxis pauseAxis = new InputAxis();
            pauseAxis.descriptiveName = "Toggle pause";
            pauseAxis.name = "Pause";
            pauseAxis.positiveButton = "p";
            EditorUtils.AddAxis(pauseAxis);


            /*
                For some reason LayerMask field get lost on export / import of unitypackage
                so i create all from code.
            */  


            string harpoonpath = "Assets/_RagdollManager/Prefabs/HarpoonBall.prefab";
            string soappath = "Assets/_RagdollManager/Prefabs/SoapBall.prefab";
            string rocketpath = "Assets/_RagdollManager/Prefabs/RocketBall.prefab";
            string inflatepath = "Assets/_RagdollManager/Prefabs/InflatableBall.prefab";

            string harpoontriggerpath = "Assets/_RagdollManager/Prefabs/HarpoonBallTrigger.prefab";
            string soaptriggerpath = "Assets/_RagdollManager/Prefabs/SoapBallTrigger.prefab";
            string rockettriggerpath = "Assets/_RagdollManager/Prefabs/RocketBallTrigger.prefab";
            string inflatetriggerpath = "Assets/_RagdollManager/Prefabs/InflatableBallTrigger.prefab";


            GameObject harpoonball = (GameObject)AssetDatabase.LoadAssetAtPath(harpoonpath, typeof(GameObject));
            GameObject soapball = (GameObject)AssetDatabase.LoadAssetAtPath(soappath, typeof(GameObject));
            GameObject rocketball = (GameObject)AssetDatabase.LoadAssetAtPath(rocketpath, typeof(GameObject));
            GameObject inflateball = (GameObject)AssetDatabase.LoadAssetAtPath(inflatepath, typeof(GameObject));

            GameObject harpoontrigger = (GameObject)AssetDatabase.LoadAssetAtPath(harpoontriggerpath, typeof(GameObject));
            GameObject soaptrigger = (GameObject)AssetDatabase.LoadAssetAtPath(soaptriggerpath, typeof(GameObject));
            GameObject rockettrigger = (GameObject)AssetDatabase.LoadAssetAtPath(rockettriggerpath, typeof(GameObject));
            GameObject inflatetrigger = (GameObject)AssetDatabase.LoadAssetAtPath(inflatetriggerpath, typeof(GameObject));


            AssetDatabase.StartAssetEditing();

            if (harpoonball)
            {
                BallProjectile ball = harpoonball.GetComponent<BallProjectile>();
                if(ball)
                {
                    ball.collidingLayers.value = LayerMask.GetMask("ColliderLayer", "ColliderInactiveLayer");
                }
                else
                {
                    Debug.LogError("Cannot find BallProjectile component on prefab " + harpoonpath);
                }
                
            }
            else
            {
                Debug.LogError("failed loading prefab " + harpoonpath);
            }
            if (soapball)
            {
                BallProjectile ball = soapball.GetComponent<BallProjectile>();
                if (ball)
                {
                    ball.collidingLayers.value = LayerMask.GetMask("ColliderLayer", "ColliderInactiveLayer");
                }
                else
                {
                    Debug.LogError("Cannot find BallProjectile component on prefab " + soappath);
                }
            }
            else
            {
                Debug.LogError("failed loading prefab " + soappath);
            }
            if (rocketball)
            {
                BallProjectile ball = rocketball.GetComponent<BallProjectile>();
                if (ball)
                {
                    ball.collidingLayers.value = LayerMask.GetMask("ColliderLayer", "ColliderInactiveLayer");
                }
                else
                {
                    Debug.LogError("Cannot find BallProjectile component on prefab " + rocketpath);
                }
            }
            else
            {
                Debug.LogError("failed loading prefab " + rocketpath);
            }
            if (inflateball)
            {
                BallProjectile ball = inflateball.GetComponent<BallProjectile>();
                if (ball)
                {
                    ball.collidingLayers.value = LayerMask.GetMask("ColliderLayer", "ColliderInactiveLayer");
                }
                else
                {
                    Debug.LogError("Cannot find BallProjectile component on prefab " + inflatepath);
                }
            }
            else
            {
                Debug.LogError("failed loading prefab " + inflatepath);
            }

            if (harpoontrigger)
            {
                int value = 1 << LayerMask.NameToLayer("PlayerLayer");
                value |= 1 << LayerMask.NameToLayer("NPCLayer");
                BallTrigger trigger = harpoontrigger.GetComponent<BallTrigger>();
                trigger.collidingWith .value = value;
            }
            else
            {
                Debug.LogError("failed loading prefab " + harpoontriggerpath);
            }

            if (soaptrigger )
            {
                int value = 1 << LayerMask.NameToLayer("PlayerLayer");
                value |= 1 << LayerMask.NameToLayer("NPCLayer");
                BallTrigger trigger = soaptrigger.GetComponent<BallTrigger>();
                trigger.collidingWith.value = value;
            }
            else
            {
                Debug.LogError("failed loading prefab " + soaptriggerpath);
            }

            if (rockettrigger)
            {
                int value = 1 << LayerMask.NameToLayer("PlayerLayer");
                value |= 1 << LayerMask.NameToLayer("NPCLayer");
                BallTrigger trigger = rockettrigger.GetComponent<BallTrigger>();
                trigger.collidingWith.value = value;
            }
            else
            {
                Debug.LogError("failed loading prefab " + rockettriggerpath);
            }

            if (inflatetrigger)
            {
                int value = 1 << LayerMask.NameToLayer("PlayerLayer");
                value |= 1 << LayerMask.NameToLayer("NPCLayer");
                BallTrigger trigger = inflatetrigger.GetComponent<BallTrigger>();
                trigger.collidingWith.value = value;
            }
            else
            {
                Debug.LogError("failed loading prefab " + inflatetriggerpath);
            }


            AssetDatabase.StopAssetEditing();
        }





        private static BuildTargetGroup[] buildTargetGroups = new BuildTargetGroup[]
            {
                BuildTargetGroup.Standalone,
               // BuildTargetGroup.WebPlayer,
                BuildTargetGroup.Android,
                BuildTargetGroup.iOS,
                BuildTargetGroup.WP8,
                BuildTargetGroup.BlackBerry
            };

        private static BuildTargetGroup[] mobileBuildTargetGroups = new BuildTargetGroup[]
            {
                BuildTargetGroup.Android,
                BuildTargetGroup.iOS,
                BuildTargetGroup.WP8,
                BuildTargetGroup.BlackBerry,
				BuildTargetGroup.PSM, 
				BuildTargetGroup.Tizen, 
				BuildTargetGroup.WSA 
            };


        /// <summary>
        /// get current defines list
        /// </summary>
        /// <param name="group">BuildTargetGroup group</param>
        /// <returns>defines list</returns>
        private static List<string> GetDefinesList(BuildTargetGroup group)
        {
            return new List<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';'));
        }

        /// <summary>
        /// set and enable define
        /// </summary>
        /// <param name="defineName"></param>
        /// <param name="enable"></param>
        /// <param name="mobile"></param>
        private static void SetEnabled(string defineName, bool enable, bool mobile)
        {
            foreach (var group in mobile ? mobileBuildTargetGroups : buildTargetGroups)
            {
                var defines = GetDefinesList(group);
                if (enable)
                {
                    if (defines.Contains(defineName))
                    {
                        return;
                    }
                    defines.Add(defineName);
                }
                else
                {
                    if (!defines.Contains(defineName))
                    {
                        return;
                    }
                    while (defines.Contains(defineName))
                    {
                        defines.Remove(defineName);
                    }
                }
                string definesString = string.Join(";", defines.ToArray());
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, definesString);
            }
        }
    } 
}
