using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditorInternal.Profiling.Memory.Experimental;

namespace ForceCodeFPS
{
    [System.Serializable]
    public class RoomData
    {
        public string RoomName;
        public string MapName;
        public string GameMode;
        public int PlayerCount;
        public int MaxPlayers;
        public string IPAddress;
    }

    /// <summary>
    /// Вызов SetupRoom() из RoomListManager или аналогичного класса:
    /// GameObject newItem = Instantiate(roomItemPrefab, roomListParent);
    /// var roomItem = newItem.GetComponent<r_RoomBrowserItem>();
    /// roomItem.SetupRoom(roomData); // где roomData — это объект RoomData
    /// </summary>
    public class r_RoomBrowserItem : MonoBehaviour
    {
        #region Variables
        [Header("Room Browser UI")]
        public Text m_RoomNameText;
        public Text m_MapNameText;
        public Text m_GameModeText;
        public Text m_PlayersText;

        [Header("Join Room UI")]
        public Button m_JoinRoomButton;

        [Header("Room Configuration")]
        private RoomData m_RoomData;
        #endregion

        #region Unity Calls
        private void Awake()
        {
            m_JoinRoomButton.onClick.AddListener(() =>
            {
                JoinRoom();
                r_AudioController.instance?.PlayClickSound();
            });
        }
        #endregion

        /// <summary>
        /// In this section below we setup the UI with the room information for our room browser.
        /// There is a join button on the room browser item, if we press on this button, it checks if the room has place for another player.
        /// </summary>
        #region Actions
        public void SetupRoom(RoomData roomData)
        {
            m_RoomData = roomData;

            m_RoomNameText.text = roomData.RoomName;
            m_MapNameText.text = roomData.MapName;
            m_GameModeText.text = roomData.GameMode;
            m_PlayersText.text = $"{roomData.PlayerCount}/{roomData.MaxPlayers}";
        }

        public void JoinRoom()
        {
            if (m_RoomData.PlayerCount >= m_RoomData.MaxPlayers)
            {
                Debug.Log("Room is full");
                return;
            }

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetConnectionData(m_RoomData.IPAddress, 7777); // Используйте свой порт, если он другой

            NetworkManager.Singleton.StartClient();
        }
        #endregion
    }
}