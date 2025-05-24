using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ForceCodeFPS
{
    public class NetworkPlayerDataManager : NetworkBehaviour
    {
        public static NetworkPlayerDataManager Instance { get; private set; }

        private Dictionary<ulong, string> playerNicknames = new Dictionary<ulong, string>();
        private Dictionary<ulong, int> playerKills = new Dictionary<ulong, int>();
        private Dictionary<ulong, int> playerDeaths = new Dictionary<ulong, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!playerNicknames.ContainsKey(clientId))
                playerNicknames[clientId] = "Player " + clientId;

            playerKills[clientId] = 0;
            playerDeaths[clientId] = 0;
        }

        private void OnClientDisconnected(ulong clientId)
        {
            playerNicknames.Remove(clientId);
            playerKills.Remove(clientId);
            playerDeaths.Remove(clientId);
        }

        public void SetNickname(ulong clientId, string nickname)
        {
            if (IsServer && playerNicknames.ContainsKey(clientId))
                playerNicknames[clientId] = nickname;
        }

        public string GetNickname(ulong clientId)
        {
            return playerNicknames.TryGetValue(clientId, out var nickname) ? nickname : "Unknown";
        }

        public void AddKill(ulong clientId)
        {
            if (playerKills.ContainsKey(clientId))
                playerKills[clientId]++;
        }

        public void AddDeath(ulong clientId)
        {
            if (playerDeaths.ContainsKey(clientId))
                playerDeaths[clientId]++;
        }

        public int GetKills(ulong clientId)
        {
            return playerKills.TryGetValue(clientId, out var kills) ? kills : 0;
        }

        public int GetDeaths(ulong clientId)
        {
            return playerDeaths.TryGetValue(clientId, out var deaths) ? deaths : 0;
        }
    }
}

