using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace ForceCodeFPS
{
    public class r_WeaponPickup : NetworkBehaviour
    {
        [Header("Configuration")]
        public r_WeaponPickupBase m_WeaponPickupData;

        public void OnPickup()
        {
            // ����������� �������� ������� � �������
            DestroyPickupServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void DestroyPickupServerRpc(ServerRpcParams rpcParams = default)
        {
            DestroyPickupClientRpc();

            // ������� �� ������� (� ������ �������� � ����, ������ ��� �� NetworkObject)
            if (IsServer)
            {
                NetworkObject.Despawn(true);
            }
        }

        [ClientRpc]
        private void DestroyPickupClientRpc(ClientRpcParams rpcParams = default)
        {
            // ������ �������� ����� ������� ������������, ���� �����
        }
    }
}
