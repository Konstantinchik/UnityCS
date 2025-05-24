using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerData : NetworkBehaviour
{
    public NetworkVariable<string> PlayerName = new NetworkVariable<string>();

    public static NetworkPlayerData instance;

    private void Awake()
    {
        instance = this;
    }

    // Костыль страшный
    public string GetPlayerName{ get=>PlayerName.Value; }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Устанавливаем имя один раз при входе в сеть
            string name = $"Player {OwnerClientId}";
            PlayerName.Value = name;
        }
    }
}