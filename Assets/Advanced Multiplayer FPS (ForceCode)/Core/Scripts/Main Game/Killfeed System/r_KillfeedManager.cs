using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace ForceCodeFPS
{
    public class r_KillfeedManager : NetworkBehaviour
    {
        public static r_KillfeedManager Instance;

        #region Public Variables
        //[Header("Photonview")]
        //public PhotonView m_Photonview;

        [Header("UI")]
        public Transform m_KillfeedContent;
        public GameObject m_KillfeedPrefab;

        [Header("Killfeed UI Settings")]
        public Color m_UsernameColor;
        public Color m_DefaultTextColor;

        [Header("Killfeed Settings")]
        public float m_KillfeedDuration = 5f;
        #endregion

        #region Functions
        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
        #endregion

        #region Actions
        public void AddKillfeed(ulong killerClientId, string killerName, string weaponName, ulong victimClientId, string victimName)
        {
            AddKillfeedClientRpc(killerClientId, killerName, weaponName, victimClientId, victimName);
        }
        #endregion

        #region Network Events
        [ClientRpc]
        private void AddKillfeedClientRpc(ulong killerId, string killerName, string weaponName, ulong victimId, string victimName)
        {
            GameObject killfeedItem = Instantiate(m_KillfeedPrefab, m_KillfeedContent);

            // Определяем цвета
            Color killerColor = NetworkManager.Singleton.LocalClientId == killerId ? m_UsernameColor : m_DefaultTextColor;
            Color victimColor = NetworkManager.Singleton.LocalClientId == victimId ? m_UsernameColor : m_DefaultTextColor;

            string killerColorHex = ColorUtility.ToHtmlStringRGBA(killerColor);
            string victimColorHex = ColorUtility.ToHtmlStringRGBA(victimColor);

            // Устанавливаем текст
            killfeedItem.GetComponent<Text>().text = $"<color=#{killerColorHex}>{killerName}</color> [{weaponName}] <color=#{victimColorHex}>{victimName}</color>";

            // Удаление через X секунд
            Destroy(killfeedItem, m_KillfeedDuration);
        }
        #endregion
    }
}