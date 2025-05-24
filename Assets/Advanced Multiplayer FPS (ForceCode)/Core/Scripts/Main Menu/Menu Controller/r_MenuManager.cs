using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace ForceCodeFPS
{
    [System.Serializable]
    public class r_MenuPanel
    {
        [Header("Panel Information")]
        public string m_PanelName;
        public GameObject m_Panel;

        [Header("Panel Buttons")]
        public Button m_OpenButton;
        public Button m_CloseButton;

        [Space(10)] public bool m_Default;
    }

    public class r_MenuManager : MonoBehaviour
    {
        #region Public variables
        [Header("Menu Panels")]
        public List<r_MenuPanel> m_Panels = new List<r_MenuPanel>();

        [Header("Username UI")]
        public InputField m_UsernameInput;
        public Button m_UsenameSaveButton;

        [Header("Panels to hide")]
        public GameObject[] m_HidingPanels;
        #endregion

        #region Unity Callbacks
        private void Awake() => HandleButtons();
        private void Start() => SetDefaults();
        #endregion

        #region Core Logic
        private void SetDefaults()
        {
            CheckUsername();
            SetDefaultMenuPanel();

            if (Time.timeScale != 1)
                Time.timeScale = 1;
        }

        private void CheckUsername()
        {
            if (PlayerPrefs.HasKey("username"))
            {
                string savedName = PlayerPrefs.GetString("username");
                m_UsernameInput.text = savedName;

                // Вы можешь здесь сохранить это имя в Singleton или сетевом объекте, чтобы передать его при спавне
                NetworkPlayerData.LocalPlayerName = savedName;
            }
            else
            {
                string generatedName = "Player" + Random.Range(1, 999);
                m_UsernameInput.text = generatedName;
                NetworkPlayerData.LocalPlayerName = generatedName;
            }
        }

        private void SaveUsername(string _Username)
        {
            PlayerPrefs.SetString("username", _Username);
            NetworkPlayerData.LocalPlayerName = _Username;
        }

        private void SetDefaultMenuPanel()
        {
            foreach (r_MenuPanel _panel in m_Panels)
            {
                if (_panel.m_Default)
                {
                    HandleHidingPanels(false);
                    _panel.m_Panel.SetActive(true);
                }
            }
        }

        private void HandleHidingPanels(bool _state)
        {
            foreach (GameObject _panel in m_HidingPanels)
                _panel.SetActive(_state);
        }

        private void HandleButtons()
        {
            m_UsenameSaveButton.onClick.AddListener(delegate
            {
                if (!string.IsNullOrEmpty(m_UsernameInput.text))
                {
                    SaveUsername(m_UsernameInput.text);
                }

                r_AudioController.instance.PlayClickSound();
            });

            foreach (r_MenuPanel _panel in m_Panels)
            {
                _panel.m_OpenButton.onClick.AddListener(delegate
                {
                    _panel.m_Panel.SetActive(true);
                    HandleHidingPanels(false);
                    r_AudioController.instance.PlayClickSound();
                });

                _panel.m_CloseButton.onClick.AddListener(delegate
                {
                    _panel.m_Panel.SetActive(false);
                    HandleHidingPanels(true);
                    r_AudioController.instance.PlayClickSound();
                });
            }
        }
        #endregion
    }

    // Хранилище имени игрока для NGO
    public class NetworkPlayerData : NetworkBehaviour
    {
        public NetworkVariable<string> PlayerName = new("Player", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    }
}
