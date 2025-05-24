using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForceCodeFPS
{
    public class r_RoomBrowserController : MonoBehaviour
    {
        public static r_RoomBrowserController instance;

        [Header("Room Browser")]
        public List<LocalRoomInfo> localRooms = new List<LocalRoomInfo>();

        [Header("Room Browser Content")]
        public Transform m_RoomBrowserContent;

        [Header("Room Browser Item")]
        public r_RoomBrowserItem m_RoomBrowserItem;

        [Header("Room Browser Refresh")]
        public Button m_RoomBrowserRefreshButton;

        private void Awake()
        {
            if (instance)
            {
                Destroy(this);
                return;
            }

            instance = this;
            m_RoomBrowserRefreshButton.onClick.AddListener(RefreshRoomBrowser);
        }

        public void RefreshRoomBrowser()
        {
            RemoveRoomsBrowserItems();

            localRooms = LocalRoomRegistry.GetAvailableRooms();

            foreach (var room in localRooms)
            {
                var item = Instantiate(m_RoomBrowserItem, m_RoomBrowserContent);
                //item.SetupRoom(room);
            }
        }

        public void RemoveRoomsBrowserItems()
        {
            foreach (Transform child in m_RoomBrowserContent)
                Destroy(child.gameObject);
        }
    }

    [System.Serializable]
    public class LocalRoomInfo
    {
        public string roomName;
        public string ipAddress;
        public int port;
        public int currentPlayers;
        public int maxPlayers;
    }

    public static class LocalRoomRegistry
    {
        private static List<LocalRoomInfo> rooms = new List<LocalRoomInfo>();

        public static void RegisterRoom(string name, string ip, int port, int current, int max)
        {
            rooms.Add(new LocalRoomInfo
            {
                roomName = name,
                ipAddress = ip,
                port = port,
                currentPlayers = current,
                maxPlayers = max
            });
        }

        public static List<LocalRoomInfo> GetAvailableRooms()
        {
            // В будущем сюда можно добавить поиск по UDP Broadcast или Multicast
            return new List<LocalRoomInfo>(rooms);
        }

        public static void ClearRooms() => rooms.Clear();
    }
}
