using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace ForceCodeFPS
{
    public class r_InGameScore : NetworkBehaviour
    {
        public static r_InGameScore Instance;

        #region Public Variables
        [Header("Score UI")]
        public Text m_WinningUserText;
        #endregion

        #region Private Variables
        [HideInInspector] public int m_WinningScore = 1;
        #endregion

        #region Functions
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(Instance.gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (IsServer)
                SetWinningScore(r_InGameMode.Instance.FindGameMode().m_WinningKills);
        }
        #endregion

        #region Actions
        public void UpdateScore()
        {
            if (r_InGameMode.Instance.m_GameMode == r_GameModeType.FFA)
            {
                //Check winning score
                CheckWinningScoreFFA();
            }
        }

        private void CheckWinningScoreFFA()
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var playerObject = client.PlayerObject;
                if (playerObject == null) continue;

                var playerStats = playerObject.GetComponent<r_PlayerStats>();
                if (playerStats == null) continue;

                if (playerStats.Kills.Value >= m_WinningScore)
                {
                    Debug.Log("Winner: " + playerStats.PlayerName.Value);
                    Debug.Log("Kills: " + playerStats.Kills.Value);

                    m_WinningUserText.text = $"Winner: {playerStats.PlayerName.Value}";
                    m_WinningUserText.gameObject.SetActive(true);

                    r_InGameManager.Instance.SetGameState(r_GameState.ENDING);
                    break;
                }
            }
        }
        #endregion

        #region Set 
        private void SetWinningScore(int _winning_score) => this.m_WinningScore = _winning_score;
        #endregion

        #region Callbacks - OLD
        //public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) => UpdateScore();
        #endregion
    }
}