using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace ForceCodeFPS
{
    public class m_SpectatorHolder : MonoBehaviour
    {
        public static m_SpectatorHolder instance;

        #region Public Variables
        [Header("Camera")]
        public Camera m_Camera;

        [Header("Spectator UI")]
        public GameObject m_SpectatorPanel;

        [Header("Spectator UI Content")]
        public Text m_KillerNameText;
        public Text m_KillerHealthText;
        public Text m_KillerWeaponText;
        public RawImage m_KillerWeaponImage;

        [Header("Spectator Settings")]
        public float m_SpectateTime = 10f;
        public float m_SpectateTimeReset = 10f;

        [Header("Spectator Camera Settings")]
        public float m_SpectateSpeed = 10f;
        #endregion

        #region Private Variables
        [HideInInspector] public Transform m_Target;
        [HideInInspector] public bool m_Spectating;
        [HideInInspector] public r_PlayerController m_TargetController;
        #endregion

        #region Unity Callbacks
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            m_SpectateTime = m_SpectateTimeReset;
        }

        private void FixedUpdate()
        {
            if (this.m_Target != null)
                HandleSpectating();
            else if (this.m_Spectating)
                CancelSpectate();
        }

        private void LateUpdate()
        {
            if (this.m_Target != null && this.m_Spectating)
                HandleSpectatingCamera();
        }
        #endregion

        #region Spectate Logic
        private void HandleSpectating()
        {
            m_Spectating = m_SpectateTime > 0;

            if (m_Spectating)
            {
                m_SpectateTime -= Time.deltaTime;
                m_TargetController = m_Target.GetComponent<r_PlayerController>();
            }
            else
            {
                CancelSpectate();
            }
        }

        private void HandleSpectatingCamera()
        {
            if (m_TargetController?.m_ThirdPersonManager != null)
            {
                Vector3 pos = m_TargetController.m_ThirdPersonManager.m_SpectateHolder.position;
                m_Camera.transform.position = pos;

                m_Camera.transform.LookAt(m_TargetController.m_PlayerCamera.m_CameraHolder);
            }
        }

        private void CancelSpectate()
        {
            m_Camera.enabled = false;
            m_Target = null;
            m_SpectateTime = m_SpectateTimeReset;
            m_Camera.transform.position = Vector3.zero;
            SetUIPanel(m_SpectatorPanel, false);
            r_InGameManager.Instance.ResetLocalGameSettings();
            m_Spectating = false;
        }
        #endregion

        #region Public Interface
        public void SetTarget(ulong attackerClientId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(attackerClientId, out var client))
            {
                var netObj = client.PlayerObject;

                if (netObj == null)
                {
                    CancelSpectate();
                    return;
                }

                m_Target = netObj.transform;

                // Допустим, здоровье и оружие уже можно прочитать из NetworkPlayerState
                NetworkPlayerState playerState = netObj.GetComponent<NetworkPlayerState>();
                string killerName = playerState.PlayerName.Value.ToString();
                float killerHealth = playerState.Health.Value;
                string weaponName = playerState.WeaponName.Value.ToString();

                SetSpectate();
                UpdateUI(killerName, killerHealth, weaponName);
                SetUIText(m_KillerNameText, "Killed By " + killerName);
            }
            else
            {
                CancelSpectate();
            }
        }

        public void SetTarget(string killerName, float killerHealth, string weaponName)
        {
            // Здесь, например:
            UpdateUI(killerName, killerHealth, weaponName);
            SetUIText(m_KillerNameText, "Killed By " + killerName);
            SetSpectate();
        }

        public void UpdateUI(string _attacker, float _attackerHealth, string _attackerWeaponName)
        {
            m_KillerNameText.text = _attacker;
            m_KillerHealthText.text = _attackerHealth.ToString("000");
            m_KillerWeaponText.text = _attackerWeaponName;

            if (m_Target.TryGetComponent(out r_WeaponManager weaponManager))
            {
                var weapon = weaponManager.FindWeaponByName(_attackerWeaponName);
                if (weapon != null)
                    m_KillerWeaponImage.texture = weapon.m_WeaponData.m_WeaponTexture;
            }
        }

        private void SetSpectate()
        {
            m_Camera.enabled = true;
            SetUIPanel(m_SpectatorPanel, true);
        }

        public void SetUIPanel(GameObject _panel, bool _state) => _panel.SetActive(_state);
        public void SetUIText(Text _text, string _string) => _text.text = _string;
        #endregion
    }
}
