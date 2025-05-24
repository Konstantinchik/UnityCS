using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace ForceCodeFPS
{
    public class r_ThirdPersonCamera : NetworkBehaviour
    {
        #region Public variables
        [Header("Third Person Camera")]
        public Camera m_ThirdPersonCamera;

        [Header("Third Person Camera Settings")]
        public float m_DeathCameraTime = 3f;
        #endregion

        #region Private variables
        [HideInInspector] public float m_CameraTime;

        [HideInInspector] public string m_LastAttackerName;
        [HideInInspector] public string m_ReceiverName;
        [HideInInspector] public float m_LastAttackerHealth;
        [HideInInspector] public string m_LastAttackerWeapon;
        #endregion

        #region Unity Callbacks
        private void Update()
        {
            if (!IsOwner) return;

            if (m_ThirdPersonCamera.gameObject.activeSelf)
                HandleDeathCamera();
        }
        #endregion

        #region Handling
        private void HandleDeathCamera()
        {
            if (m_CameraTime <= 0)
            {
                m_ThirdPersonCamera.gameObject.SetActive(false);

                // Если локальный игрок был последним атакующим — возвращаем сцену
                if (IsLocalPlayerWasAttacker())
                {
                    r_InGameManager.Instance.ResetLocalGameSettings();
                }
                else
                {
                    // Переходим в режим наблюдения
                    if (m_SpectatorController.instance)
                        m_SpectatorController.instance.CallEventOnDie(m_LastAttackerName, transform.name, m_LastAttackerHealth, m_LastAttackerWeapon);
                }

                Destroy(this);
            }
            else
            {
                m_CameraTime -= Time.deltaTime;
            }
        }
        #endregion

        #region Actions
        public void SetDeathCamera(string senderName, string receiverName, float senderHealth, string senderWeaponName)
        {
            m_LastAttackerName = senderName;
            m_ReceiverName = receiverName;
            m_LastAttackerHealth = senderHealth;
            m_LastAttackerWeapon = senderWeaponName;

            m_ThirdPersonCamera.gameObject.SetActive(true);
            m_CameraTime = m_DeathCameraTime;
        }

        private bool IsLocalPlayerWasAttacker()
        {
            // Здесь можно заменить на свою систему имени игрока
            var localName = NetworkManager.Singleton.LocalClient.PlayerObject.name;
            return m_LastAttackerName == localName;
        }
        #endregion
    }
}
