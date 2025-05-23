using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace ForceCodeFPS
{
    public class r_InGameScoreboard : MonoBehaviour
    {
        [Header("Scoreboard Content")]
        public GameObject m_ScoreboardContent;

        [Header("Scoreboard Entry")]
        public GameObject m_ScoreboardEntry;

        [HideInInspector]
        public List<r_InGameScoreboardEntry> m_Players = new List<r_InGameScoreboardEntry>();

        private void FixedUpdate()
        {
            CheckPlayerList();
        }

        private void CheckPlayerList()
        {
            List<ulong> clientIds = GetClientIdList(); // ✅ Здесь вызывается метод, определённый ниже

            // Добавить новых
            foreach (ulong clientId in clientIds)
            {
                if (m_Players.Any(x => x.ClientId == clientId))
                    continue;

                GameObject entry = Instantiate(m_ScoreboardEntry, m_ScoreboardContent.transform);
                r_InGameScoreboardEntry item = entry.GetComponent<r_InGameScoreboardEntry>();
                item.Initialize(clientId);
                m_Players.Add(item);
            }

            // Удалить тех, кого больше нет
            for (int i = m_Players.Count - 1; i >= 0; i--)
            {
                if (!clientIds.Contains(m_Players[i].ClientId))
                {
                    Destroy(m_Players[i].gameObject);
                    m_Players.RemoveAt(i);
                }
            }

            SortScoreboard();
        }

        public void SortScoreboard()
        {
            var sorted = m_Players
                .OrderByDescending(x => NetworkPlayerDataManager.Instance.GetKills(x.ClientId))
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].transform.SetSiblingIndex(i);
            }
        }

        // ✅ Вот этот метод должен быть внутри класса
        private List<ulong> GetClientIdList()
        {
            return NetworkManager.Singleton.ConnectedClientsList
                .Select(client => client.ClientId)
                .ToList();
        }
    }
}