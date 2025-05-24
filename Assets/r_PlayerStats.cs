using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace ForceCodeFPS
{
    public class r_PlayerStats : NetworkBehaviour
    {
        public NetworkVariable<int> Kills = new NetworkVariable<int>(0);
        public NetworkVariable<int> Deaths = new NetworkVariable<int>(0);

        // ��������� ��� ������ (FixedString � ����� ��� �������� �� ����)
        public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>();

        public void AddKill()
        {
            if (IsServer)
            {
                Kills.Value++;
            }
        }

        public void AddDeath()
        {
            if (IsServer)
            {
                Deaths.Value++;
            }
        }

        public void ResetStats()
        {
            if (IsServer)
            {
                Kills.Value = 0;
                Deaths.Value = 0;
            }
        }

        // ���������� ��� ������ (������ ������ ������ ��������)
        public void SetPlayerName(string name)
        {
            if (IsServer)
            {
                PlayerName.Value = name;
            }
        }
    }
}
