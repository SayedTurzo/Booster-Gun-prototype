using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_5_3
using UnityEngine.SceneManagement;
#endif
using UnityStandardAssets.CrossPlatformInput;

[RequireComponent(typeof (Image))]
public class ForcedReset : MonoBehaviour
{
    private void Update()
    {
        // if we have forced a reset ...
        if (CrossPlatformInputManager.GetButtonDown("ResetObject"))
        {
#if UNITY_5_3
            //... reload the scene
            SceneManager.LoadScene(SceneManager.GetSceneAt(0).path);
#else
            Application.LoadLevel(Application.loadedLevel);
#endif
        }
    }
}
