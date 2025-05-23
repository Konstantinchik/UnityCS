using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace ForceCodeFPS
{
    /// <summary>
    /// Как использовать
    /// На хосте, чтобы установить свойства:
    /// GameSettingsManager.Instance.SetGameSettingsServerRpc("Dust2", "Deathmatch", "map01", 1);
    /// 
    /// На клиенте, чтобы получить:
    /// string currentMap = GameSettingsManager.Instance.GetMap();
    /// </summary>
    public class GameSettingsManager : NetworkBehaviour
    {
        public static GameSettingsManager Instance;

        [Header("Game Properties")]
        public NetworkVariable<FixedString32Bytes> GameMap = new NetworkVariable<FixedString32Bytes>();
        public NetworkVariable<FixedString32Bytes> GameMode = new NetworkVariable<FixedString32Bytes>();
        public NetworkVariable<FixedString32Bytes> GameMapImageID = new NetworkVariable<FixedString32Bytes>();
        public NetworkVariable<int> RoomState = new NetworkVariable<int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // если нужно сохранять между сценами
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetGameSettingsServerRpc(string map, string mode, string imageID, int state)
        {
            GameMap.Value = map;
            GameMode.Value = mode;
            GameMapImageID.Value = imageID;
            RoomState.Value = state;
        }

        public string GetMap() => GameMap.Value.ToString();
        public string GetMode() => GameMode.Value.ToString();
        public string GetImageID() => GameMapImageID.Value.ToString();
        public int GetRoomState() => RoomState.Value;
    }
}
