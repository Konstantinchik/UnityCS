using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

namespace ForceCodeFPS
{
    public class r_PlayerConfig : NetworkBehaviour
    {
        #region References
        public r_PlayerController m_PlayerController
        {
            get => this.transform.GetComponent<r_PlayerController>();
            set => this.m_PlayerController = value;
        }

        public r_WeaponManager m_WeaponManager
        {
            get => this.m_PlayerController != null ? this.m_PlayerController.m_WeaponManager : this.transform.GetComponent<r_WeaponManager>();
            set => this.m_WeaponManager = value;
        }
        #endregion

        #region Public Variables
        [Header("Local Player Configuration")]
        public MonoBehaviour[] m_LocalScripts;
        public GameObject[] m_LocalObjects;
        #endregion

        #region Network Variables
        private NetworkVariable<FixedString64Bytes> m_PlayerName = new NetworkVariable<FixedString64Bytes>(
            writePerm: NetworkVariableWritePermission.Server);
        #endregion

        private void Start()
        {
            if (IsOwner)
            {
                EnableLocalSettings();
                SubmitNameToServer(GetLocalPlayerName());
            }

            SetObjectName(m_PlayerName.Value);
            m_PlayerName.OnValueChanged += (_, newValue) => SetObjectName(newValue);
        }

        private void EnableLocalSettings()
        {
            if (m_LocalScripts != null)
                foreach (var script in m_LocalScripts)
                    script.enabled = true;

            if (m_LocalObjects != null)
                foreach (var obj in m_LocalObjects)
                    obj.SetActive(true);
        }

        private string GetLocalPlayerName()
        {
            // Можно заменить на что-то свое — например, ввод имени через UI.
            return System.Environment.UserName;
        }

        private void SetObjectName(string name)
        {
            this.gameObject.name = name;
        }

        private void SubmitNameToServer(string name)
        {
            SubmitNameServerRpc(name);
        }

        [ServerRpc]
        private void SubmitNameServerRpc(string name)
        {
            m_PlayerName.Value = name;
        }

        public void SetupLocalPlayer(int[] loadout_weapon_ids)
        {
            if (!IsOwner) return;

            // Применение выбранного вооружения
            m_WeaponManager.OnLoadoutSelect(loadout_weapon_ids);
        }
    }
}
