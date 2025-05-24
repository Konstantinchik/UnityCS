using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace ForceCodeFPS
{
    public class r_PlayerHealth : NetworkBehaviour
    {
        #region References
        public r_PlayerController m_PlayerController;
        #endregion

        #region Public Variables
        [Header("Health Base Configuration")]
        public r_PlayerHealthBase m_HealthBase;
        #endregion

        #region Private Variables
        [HideInInspector] public NetworkVariable<float> m_Health = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Server);
        [HideInInspector] public bool m_IsDeath;

        [HideInInspector] public string m_LastAttackerName;
        [HideInInspector] public float m_LastAttackerHealth;
        [HideInInspector] public string m_LastAttackerWeapon;
        #endregion

        #region Unity Callbacks
        private void Start()
        {
            if (IsServer)
                SetDefaults();

            m_Health.OnValueChanged += OnHealthChanged;
        }
        #endregion

        #region Initialization
        private void SetDefaults()
        {
            m_Health.Value = m_HealthBase.m_MaxHealth;
            m_IsDeath = false;
        }
        #endregion

        #region Public API
        public void DecreaseHealth(string senderName, float amount, Vector3 senderPosition, float senderHealth, string senderWeaponName)
        {
            DecreaseHealthServerRpc(senderName, amount, senderPosition, senderHealth, senderWeaponName);
        }

        public void IncreaseHealth(float amount)
        {
            IncreaseHealthServerRpc(amount);
        }
        #endregion

        #region Server RPCs
        [ServerRpc]
        private void DecreaseHealthServerRpc(string senderName, float amount, Vector3 senderPosition, float senderHealth, string senderWeaponName)
        {
            if (m_IsDeath) return;

            m_LastAttackerName = senderName;
            m_LastAttackerHealth = senderHealth;
            m_LastAttackerWeapon = senderWeaponName;

            m_Health.Value -= amount;

            DecreaseHealthClientRpc(senderName, amount, senderPosition);

            if (m_Health.Value <= 0f)
            {
                Suicide();
            }
        }

        [ServerRpc]
        private void IncreaseHealthServerRpc(float amount)
        {
            if (m_Health.Value >= m_HealthBase.m_MaxHealth) return;

            m_Health.Value = Mathf.Min(m_Health.Value + amount, m_HealthBase.m_MaxHealth);
        }
        #endregion

        #region Client RPCs
        [ClientRpc]
        private void DecreaseHealthClientRpc(ulong senderClientId, float amount, Vector3 senderPosition)
        {
            if (IsOwner)
            {
                //string senderName = "Player " + senderClientId;
                m_PlayerController.m_PlayerUI.SetDamageIndicator(senderClientId, senderPosition);
                m_PlayerController.m_PlayerAudio.OnPlayerHurtAudioPlay(transform.position);
            }

            m_PlayerController.m_PlayerUI.SetHealthText(m_Health.Value);
            m_PlayerController.m_PlayerUI.SetBloodyScreen();

            if (!IsOwner || senderName != System.Environment.UserName)
            {
                m_PlayerController.m_PlayerCamera.OnCameraHit(senderPosition);
            }
        }
        #endregion

        #region Suicide / Death
        private void Suicide()
        {
            m_Health.Value = 0;
            m_IsDeath = true;

            m_PlayerController.m_WeaponManager.OnDropAllWeapons();

            if (IsOwner)
            {
                // Здесь можно заменить систему статистики на вашу локальную реализацию.
                // Например, сохранить количество смертей в PlayerPrefs или NetworkVariable.

                m_PlayerController.m_ThirdPersonManager.m_ThirdPersonCamera.SetDeathCamera(
                    m_LastAttackerName,
                    gameObject.name,
                    m_LastAttackerHealth,
                    m_LastAttackerWeapon
                );
            }

            m_PlayerController.m_ThirdPersonManager.transform.parent = null;
            m_PlayerController.m_ThirdPersonManager.ThirdPersonSuicide();

            if (IsOwner)
            {
                // Сервер удаляет объект игрока
                if (IsServer)
                {
                    NetworkObject.Despawn(true);
                }
                else
                {
                    RequestDespawnServerRpc();
                }
            }
        }

        [ServerRpc]
        private void RequestDespawnServerRpc()
        {
            NetworkObject.Despawn(true);
        }
        #endregion

        #region Event Handlers
        private void OnHealthChanged(float previous, float current)
        {
            if (IsOwner)
            {
                m_PlayerController.m_PlayerUI.SetHealthText(current);
            }
        }
        #endregion
    }
}
