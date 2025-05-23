using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerData : NetworkBehaviour
{
    public NetworkVariable<string> PlayerName = new NetworkVariable<string>();

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // ������������� ��� ���� ��� ��� ����� � ����
            string name = $"Player {OwnerClientId}";
            PlayerName.Value = name;
        }
    }
}