// © 2015 Mario Lelas
using UnityEngine;
#if UNITY_5_3
using UnityEngine.SceneManagement;
#endif

namespace MLSpace
{
    /// <summary>
    /// Pauses and unpauses game
    /// Toggles controls info text
    /// Sets player hit interval 
    /// </summary>
    public class GameControl : MonoBehaviour
    {
        /// <summary>
        /// Information text UI
        /// </summary>
        public UnityEngine.UI.Text InfoUI;

        /// <summary>
        /// Information on player ammunition type and other
        /// </summary>
        public UnityEngine.UI.Text PlayerInfoText;

        /// <summary>
        /// hide cursor on start
        /// </summary>
        public bool hideCursor = true;

        /// <summary>
        /// text shown when ckicked F1
        /// </summary>
        [Multiline]
        public string InfoText = "Press F1 to hide controls" +
                        "\nW - Forward" +
                        "\nS - Back" +
                        "\nA - Left" +
                        "\nD - Right" +
                        "\nPress and hold mouse0 to inflate ball" +
                        "\nRelease mouse0 to shoot ball" +
                        "\nF - Toggle chase mode" +
                        "\n1 - Always take hits" +
                        "\n2 - Ignore hits when in ragdoll" +
                        "\n3 - Take hits only fully recovered";

        private bool m_Paused = false;      // is game paused flag
        private ShootScript m_PlayersShootScript;     // player control script reference
        private PlayerControl m_Player;     // player control script reference
        private bool m_ShowInfo = false;    // show controls info text flag
        

        // unity start method
        void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj)
            {
                m_Player = playerObj.GetComponent<PlayerControl>();
                if (!m_Player)
                {
                    Debug.LogWarning("Cannot find component 'PlayerControl'. ");
                }
                m_PlayersShootScript = playerObj.GetComponent<ShootScript>();
                if (!m_PlayersShootScript)
                    Debug.LogWarning("Cannot find component 'ShootScript' on player.");
            }
            else
            {
                Debug.LogWarning ("cannot find object with tag 'Player'.");
            }

            if (hideCursor)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        // Update is called once per frame
        void Update()
        {
            // quit 
            if (Input.GetKeyDown (KeyCode.Escape))
                Application.Quit();

            // restart
            if (Input.GetButtonDown("Submit"))
#if UNITY_5_3
                SceneManager.LoadScene(SceneManager .GetActiveScene ().buildIndex );
#else
                Application.LoadLevel(Application.loadedLevel);
#endif




            // pause / unpause
            if (Input.GetButtonDown("Pause"))
            {
                if (m_Paused)
                {
                    Time.timeScale = 1.0f;
                    m_Paused = false;
                }
                else
                {
                    Time.timeScale = 0.0f;
                    m_Paused = true;
                }
            }

            // set player hit interval
            if(Input.GetKeyDown(KeyCode.Alpha1 ))
            {
                if(m_Player)m_Player.RagdollManager .hitInterval = RagdollManagerHum.HitIntervals.Always ;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                if(m_Player)m_Player.RagdollManager.hitInterval = RagdollManagerHum.HitIntervals.OnGettingUp;
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                if(m_Player)m_Player.RagdollManager.hitInterval = RagdollManagerHum.HitIntervals.OnAnimated;
            }

            if(PlayerInfoText)
            {
#if DEBUG_INFO
                if (!m_PlayersShootScript) { Debug.LogError("object cannot be null."); return; }
                if(!m_PlayersShootScript.ProjectilePrefab) { Debug.LogError("object cannot be null."); return; }
#endif
                if (m_PlayersShootScript.ProjectilePrefab is SoapBallProjectile)
                    PlayerInfoText.text = "Current Ammunition: SoapBuble Ball";
                else if (m_PlayersShootScript.ProjectilePrefab is RocketBallProjectile)
                    PlayerInfoText.text = "Current Ammunition: Rocket Ball";
                else if (m_PlayersShootScript.ProjectilePrefab is HarpoonBallProjectile)
                    PlayerInfoText.text = "Current Ammunition: Harpoon Ball";
                else PlayerInfoText.text = "Current Ammunition: Inflatable Ball";
            }

            // toggle text info
            if(Input.GetKeyDown(KeyCode.F1 ))
                m_ShowInfo = !m_ShowInfo;

            if(InfoUI )
            {
                if(m_ShowInfo)
                {
                    InfoUI.text = InfoText;
                }
                else
                {
                    InfoUI.text = "Press F1 to show controls";
                }
            }
        }
    } 
}
