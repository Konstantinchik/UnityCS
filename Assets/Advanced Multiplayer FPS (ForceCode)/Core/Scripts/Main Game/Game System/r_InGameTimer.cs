using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace ForceCodeFPS
{
    /// <summary>
    /// r_InGameTimer.Instance.StartTimer(300); // запустить таймер на 5 минут
    /// </summary>
    public class r_InGameTimer : NetworkBehaviour
    {
        public static r_InGameTimer Instance;

        //public const string RoomMatchTimerProperty = "StartTime";

        #region Public Variables
        [Header("Timer UI")]
        public Text m_TimerText;
        #endregion

        #region Private Variables
        /*
        //Local timer for each player
        [HideInInspector] public int m_StartedTimerValue;

        //Countdown Duration
        [HideInInspector] public int m_TimerDuration = 0;

        //Countdown State
        [HideInInspector] private bool m_TimerStarted;
        */
        private NetworkVariable<float> timerStartTime = new NetworkVariable<float>();
        private NetworkVariable<float> timerDuration = new NetworkVariable<float>();
        private bool timerStarted = false;
        #endregion

        #region Functions
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (!timerStarted) return;

            float remaining = TimeRemaining();

            int minutes = Mathf.FloorToInt(remaining / 60);
            int seconds = Mathf.FloorToInt(remaining % 60);

            m_TimerText.text = $"{minutes:00}:{seconds:00}";

            if (remaining <= 0f)
            {
                timerStarted = false;
                r_InGameManager.Instance.OnEndedCountdown();
            }
        }
        #endregion

        #region Actions
        public void StartTimer(int _duration)
        {
            if (!IsServer) return;

            timerStartTime.Value = Time.time;
            timerDuration.Value = _duration;

            // Also trigger the flag on clients to start reading the timer
            StartTimerClientRpc();
        }

        [ClientRpc]
        private void StartTimerClientRpc()
        {
            timerStarted = true;
        }

        private float TimeRemaining()
        {
            float elapsed = Time.time - timerStartTime.Value;
            return Mathf.Max(0f, timerDuration.Value - elapsed);
        }
        #endregion

        #region Handling - OLD
        /*
        private void HandleTimer()
        {
            if (!this.m_TimerStarted) return;

            //Calculate minutes and seconds
            int _minutes = ((int)TimeRemaining() / 60);
            int _seconds = ((int)TimeRemaining() % 60);

            //Set UI Text
            this.m_TimerText.text = _minutes.ToString("00") + ":" + _seconds.ToString("00");

            //Check timer
            if (TimeRemaining() <= 0.1f)
            {
                //Disable countdown
                this.m_TimerStarted = false;

                //Finished Countdown
                r_InGameManager.Instance.OnEndedCountdown();
            }
        }
        */
        #endregion

        #region Get - OLD
        //private float TimeRemaining() => this.m_TimerDuration - (PhotonNetwork.ServerTimestamp - this.m_StartedTimerValue) / 1000f;
        #endregion

        #region Callbacks - OLD
        //public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) => InitializeTimer();
        #endregion
    }
}