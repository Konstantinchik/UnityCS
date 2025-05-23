using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace ForceCodeFPS
{
    public class NetworkPlayerState : NetworkBehaviour
    {
        public static NetworkPlayerState LocalPlayerState;

        public NetworkVariable<int> Kills = new NetworkVariable<int>();
        public NetworkVariable<int> Deaths = new NetworkVariable<int>();
        public NetworkVariable<int> WeaponIndex = new NetworkVariable<int>();

        public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                LocalPlayerState = this;
                PlayerName.Value = NetworkPlayerData.LocalPlayerName;
            }
        }

        [ServerRpc]
        public void AddKillServerRpc()
        {
            Kills.Value++;
        }

        [ServerRpc]
        public void AddDeathServerRpc()
        {
            Deaths.Value++;
        }

        [ServerRpc]
        public void SetWeaponIndexServerRpc(int index)
        {
            WeaponIndex.Value = index;
        }
    }
}
