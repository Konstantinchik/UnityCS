using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.IO;

namespace ForceCodeFPS
{
    public class r_ChatManager : NetworkBehaviour
    {
        public static r_ChatManager Instance;

        #region Public Variables
        //[Space(10)] public PhotonView m_PhotonView; - ÒÅÏÅÐÜ ÍÅ ÈÑÏÎËÜÇÓÅÒÑß

        [Header("Chat Panel")]
        public GameObject m_ChatPanel;

        [Header("Chat System")]
        public Transform m_ChatContent;
        public GameObject m_ChatPrefab;

        [Header("Chat Field")]
        public InputField m_ChatMessageInput;
        public Text m_ChatPlaceHolder;

        [Header("Chat UI Settings")]
        public Color m_UsernameColor;
        public Color m_DefaultTextColor;

        [Header("Chat Placeholder Settings")]
        public string m_ChatPlaceHolderText;

        [Header("Chat Settings")]
        public float m_ChatDuration;
        #endregion

        #region Private Variables
        //Current chat state
        [HideInInspector] public bool m_ChatOpened;
        #endregion

        #region Functions - Unity Events
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start() => SetDefault();

        private void Update() => HandleInputs();
        #endregion

        #region Actions - Chat Logic
        private void SetDefault()
        {
            //Disable chat
            this.m_ChatOpened = false;

            //Update UI
            this.m_ChatPanel.SetActive(m_ChatOpened);

            //Set placeholder 
            this.m_ChatPlaceHolder.text = this.m_ChatPlaceHolderText;
        }

        private void HandleInputs()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (this.m_ChatOpened)
                {
                    if (!string.IsNullOrEmpty(this.m_ChatMessageInput.text))
                    {
                        //Send chat
                        SendChatServerRpc(NetworkManager.Singleton.LocalClientId, m_ChatMessageInput.text);
                    }

                    //Clean inputfield
                    this.m_ChatMessageInput.text = string.Empty;

                    //Update chat panel
                    UpdateChatPanel(false);
                }
                else
                {
                    //Update chat panel
                    UpdateChatPanel(true);

                    //Focus inputfield
                    this.m_ChatMessageInput.Select();
                    this.m_ChatMessageInput.ActivateInputField();
                }
            }
        }

        private void UpdateChatPanel(bool _state)
        {
            //Set state
            this.m_ChatOpened = _state;

            //Update UI
            this.m_ChatPanel.SetActive(m_ChatOpened);

            //Disable placeholder on chat opened
            this.m_ChatPlaceHolder.gameObject.SetActive(!m_ChatOpened);

            //Controllable
            if (r_InGameManager.Instance.m_CurrentPlayer != null)
            {
                r_InGameManager.Instance.m_CurrentPlayer.GetComponent<r_PlayerController>().m_InputManager.m_Controllable = !this.m_ChatOpened;
            }
        }


        #endregion

        #region Network Events
        [ServerRpc(RequireOwnership = false)]
        private void SendChatServerRpc(ulong senderClientId, string message)
        {
            string playerName = GetPlayerName(senderClientId);
            BroadcastChatClientRpc(playerName, message);
        }

        [ClientRpc]
        private void BroadcastChatClientRpc(string playerName, string message)
        {
            GameObject entry = Instantiate(m_ChatPrefab, m_ChatContent);
            Color color = NetworkManager.Singleton.LocalClientId == GetClientIdByName(playerName)
                ? m_UsernameColor : m_DefaultTextColor;

            string colorRGBA = ColorUtility.ToHtmlStringRGBA(color);
            entry.GetComponentInChildren<Text>().text = $"<color=#{colorRGBA}>{playerName}</color> : {message}";
            Destroy(entry, m_ChatDuration);
        }

        private string GetPlayerName(ulong clientId)
        {
            var player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            var playerData = player?.GetComponent<r_PlayerController>();
            return playerData?.playerName.Value ?? $"Player {clientId}";
        }

        private ulong GetClientIdByName(string name)
        {
            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                var player = kvp.Value.PlayerObject;
                var playerData = player?.GetComponent<r_PlayerController>();
                if (playerData?.playerName.Value == name)
                    return kvp.Key;
            }
            return ulong.MaxValue;
            #endregion
        }
    }
}