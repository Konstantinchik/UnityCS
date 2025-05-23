using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace ForceCodeFPS
{
    public class r_LobbyEntry : MonoBehaviour
    {
        [Header("Lobby Player UI")]
        public Text m_PlayerNameText;
        public Image m_PlayerBackgroundImage;

        public void SetupLobbyPlayer(ulong clientId)
        {
            // Попробуем получить имя из NetworkManager (по желанию — можно хранить имена в кастомном компоненте, см. ниже)
            string playerName = $"Player {clientId}";

            // Если у тебя есть компонент с ником (например, NetworkPlayerData), можешь заменить это:
            if (NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
            {
                GameObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject?.gameObject;
                if (playerObject != null && playerObject.TryGetComponent(out NetworkPlayerData playerData))
                {
                    playerName = playerData.PlayerName.Value;
                }
            }

            m_PlayerNameText.text = playerName;
        }
    }
}
