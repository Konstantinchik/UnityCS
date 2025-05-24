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
            // Запрашиваем удаление объекта у сервера
            DestroyPickupServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void DestroyPickupServerRpc(ServerRpcParams rpcParams = default)
        {
            DestroyPickupClientRpc();

            // Удаляем на сервере (и объект исчезнет у всех, потому что он NetworkObject)
            if (IsServer)
            {
                NetworkObject.Despawn(true);
            }
        }

        [ClientRpc]
        private void DestroyPickupClientRpc(ClientRpcParams rpcParams = default)
        {
            // Можешь добавить здесь эффекты исчезновения, если нужно
        }
    }
}
