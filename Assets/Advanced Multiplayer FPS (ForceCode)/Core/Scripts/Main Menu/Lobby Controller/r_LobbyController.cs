using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace ForceCodeFPS
{
    public class r_LobbyController : MonoBehaviour
    {
        public static r_LobbyController instance;

        [Header("Lobby UI")]
        public r_LobbyControllerUI m_LobbyUI;

        [Header("Matchmaking Time")]
        public float m_SearchGameTime = 5f;
        public float m_StartGameTime = 3f;

        [Header("Matchmaking Configuration")]
        public int m_RequiredPlayers = 2;

        private bool m_StartingGame;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            HandleButtons();
        }

        private void Update()
        {
            if (InGameLobby())
            {
                if (!m_StartingGame && NetworkManager.Singleton.ConnectedClients.Count >= m_RequiredPlayers)
                    StartCoroutine(StartGame());
            }
        }

        public bool InGameLobby()
        {
            return NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient;
        }

        public IEnumerator SearchGame()
        {
            SetLobbyMenu(true);
            yield return new WaitForSeconds(m_SearchGameTime);

            if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsClient)
            {
                string randomScene = r_CreateRoomController.instance.GetRandomMapName();
                GameSettings.CurrentGameMode = r_CreateRoomController.instance.GetRandomGameMode();
                GameSettings.MaxPlayers = 8;

                r_NetworkHandler.instance.CreateRoomAndStartHost(randomScene); // ты должен сам реализовать этот метод
                yield return new WaitForSeconds(2f);
            }

            DisplayGameInformation();
            ListLobbyPlayers();
        }

        public IEnumerator StartGame()
        {
            m_StartingGame = true;

            yield return new WaitForSeconds(m_StartGameTime);

            if (NetworkManager.Singleton.ConnectedClients.Count < m_RequiredPlayers)
            {
                Debug.LogWarning("Not enough players to start the game.");
                m_StartingGame = false;
                yield break;
            }

            if (NetworkManager.Singleton.IsHost)
            {
                string sceneName = GameSettings.CurrentMapName;
                if (!string.IsNullOrEmpty(sceneName))
                {
                    NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                }
                else
                {
                    Debug.LogError("No map selected.");
                }
            }
        }

        public void LeaveLobby()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
            }

            CleanLocalPlayerList();
            SetLobbyMenu(false);
        }

        public void ListLobbyPlayers()
        {
            CleanLocalPlayerList();

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                GameObject entry = Instantiate(m_LobbyUI.m_PlayerListEntry.gameObject, m_LobbyUI.m_PlayerListContent);
                entry.GetComponent<r_LobbyEntry>().SetupLobbyPlayer(client.ClientId);
            }
        }

        public void CleanLocalPlayerList()
        {
            foreach (Transform child in m_LobbyUI.m_PlayerListContent)
            {
                Destroy(child.gameObject);
            }
        }

        private void HandleButtons()
        {
            m_LobbyUI.m_LeaveLobbyButton.onClick.AddListener(() =>
            {
                StopAllCoroutines();
                LeaveLobby();
                r_AudioController.instance?.PlayClickSound();
            });

            m_LobbyUI.m_SearchGameButton.onClick.AddListener(() =>
            {
                StartCoroutine(SearchGame());
                SetLobbyMenu(true);
                r_AudioController.instance?.PlayClickSound();
            });
        }

        public void DisplayGameInformation()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
            {
                m_LobbyUI.m_GameInformationText.text = $"{GameSettings.CurrentMapName} / {GameSettings.CurrentGameMode}";
                m_LobbyUI.m_GameMapImage.sprite = r_CreateRoomController.instance.GetMapSprite(GameSettings.CurrentMapName);
            }
            else
            {
                m_LobbyUI.m_GameInformationText.text = "Searching...";
                m_LobbyUI.m_GameMapImage.sprite = null;
            }
        }

        public void SetLobbyMenu(bool state)
        {
            m_LobbyUI.m_LobbyPanel.SetActive(state);
            m_LobbyUI.m_MenuPanel.SetActive(!state);
        }
    }
}
