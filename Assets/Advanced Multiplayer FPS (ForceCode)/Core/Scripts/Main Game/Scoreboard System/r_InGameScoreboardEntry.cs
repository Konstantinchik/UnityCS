using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace ForceCodeFPS
{
    public class r_InGameScoreboardEntry : MonoBehaviour
    {
        #region Public variables
        [Header("Entry Information")]
        public Text m_PlayerName;
        public Text m_PlayerKills;
        public Text m_PlayerDeaths;
        public Text m_PlayerPing;
        #endregion

        #region Private variables
        [HideInInspector] public ulong ClientId;
        #endregion

        #region Functions
        public void FixedUpdate()
        {
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(ClientId)) return;

            m_PlayerKills.text = NetworkPlayerDataManager.Instance.GetKills(ClientId).ToString();
            m_PlayerDeaths.text = NetworkPlayerDataManager.Instance.GetDeaths(ClientId).ToString();
            m_PlayerPing.text = GetPing(ClientId).ToString();
        }
        #endregion

        #region Actions
        public void Initialize(ulong clientId)
        {
            ClientId = clientId;

            string nickname = NetworkPlayerDataManager.Instance.GetNickname(clientId);
            m_PlayerName.text = clientId == NetworkManager.Singleton.LocalClientId ? nickname + "   [YOU]" : nickname;
        }
        #endregion

        private int GetPing(ulong clientId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
                if (transport is Unity.Netcode.Transports.UTP.UnityTransport utp)
                {
                    return (int)utp.GetCurrentRtt(clientId);
                }
            }
            return 0;
        }
    }
}