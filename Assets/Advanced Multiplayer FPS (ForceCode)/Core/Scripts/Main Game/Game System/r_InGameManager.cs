using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEditor.PackageManager;

namespace ForceCodeFPS
{
    #region Serializable Enums
    [System.Serializable] public enum r_GameState { WAITING = 0, STARTING = 1, PLAYING = 2, ENDING = 3, ENDED = 4 }
    [System.Serializable] public enum r_InGameMenuType { SCOREBOARD, LOADOUT, SETTINGS }
    #endregion

    #region Serializable Classes
    [System.Serializable]
    public class r_PauseMenuPanel
    {
        public string m_PanelName;
        public r_InGameMenuType m_InGameMenuType;

        [Header("Panel Information")]
        public GameObject m_Panel;

        [Header("Panel Buttons")]
        public Button m_OpenButton;
    }
    #endregion

    public class r_InGameManager : NetworkBehaviour
    {
        public static r_InGameManager Instance;

        #region Public Variables
        [Header("Scene Camera")]
        public Camera m_MainCamera;

        [Header("Scene Audio")]
        public AudioListener m_MainListener;

        [Header("Pause Menu UI")]
        public Canvas m_MenuCanvas;
        public GameObject m_PauseMenu;

        [Header("Game Menu UI")]
        public Button m_DeployButton;
        public Button m_LeaveButton;

        [Header("Game state UI")]
        public GameObject m_GameStateText;

        [Header("Player Prefab")]
        public GameObject m_PlayerPrefab;

        [Header("Timescale Settings")]
        public float m_OnEndTimeScale = 0.5f;

        [Space(10)] public Transform[] m_SpawnPoints;

        [Space(10)] public List<r_PauseMenuPanel> m_MenuPanels = new List<r_PauseMenuPanel>();
        #endregion

        #region Private Variables
        //Game Information
        public r_GameState m_CurrentGameState = r_GameState.WAITING;

        //Current Player
        [HideInInspector] public GameObject m_CurrentPlayer;

        //States
        [HideInInspector] public bool m_Paused;
        [HideInInspector] public bool m_Spawned;
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

            HandleButtons();
        }

        //private void Start() => StartGame();
        private void Start()
        {
            if (IsServer)
                SetGameState(r_GameState.WAITING);

            m_MenuCanvas.sortingOrder = 0;
            m_Paused = false;
            OnUpdatePauseMenu();
        }

        //private void Update() => HandlePauseMenu();
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                m_Paused = !m_Paused;
                OnUpdatePauseMenu();
            }
        }
        #endregion

        #region Handling
        private void HandleButtons()
        {
            /*
            this.m_DeployButton.onClick.AddListener(delegate { SpawnPlayer(); r_AudioController.instance.PlayClickSound(); });
            this.m_LeaveButton.onClick.AddListener(delegate { LeaveRoom(); r_AudioController.instance.PlayClickSound(); });

            foreach (r_PauseMenuPanel _menu_item in m_MenuPanels)
            {
                if (_menu_item.m_OpenButton != null)
                {
                    _menu_item.m_OpenButton.onClick.AddListener(delegate
                    {
                        HideMenuPanels();

                    //Enable panel
                    if (_menu_item.m_Panel != null) _menu_item.m_Panel.SetActive(true);
                    });
                }
            }*/
            m_DeployButton.onClick.AddListener(() => { SpawnPlayer(); r_AudioController.instance.PlayClickSound(); });
            m_LeaveButton.onClick.AddListener(() => { LeaveRoom(); r_AudioController.instance.PlayClickSound(); });

            foreach (var panel in m_MenuPanels)
            {
                if (panel.m_OpenButton != null)
                {
                    panel.m_OpenButton.onClick.AddListener(() =>
                    {
                        HideMenuPanels();
                        if (panel.m_Panel != null) panel.m_Panel.SetActive(true);
                    });
                }
            }
        }

        private void HandlePauseMenu()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                //Change pause state
                this.m_Paused = !this.m_Paused;

                //Update Pause Menu
                OnUpdatePauseMenu();
            }
        }
        #endregion

        #region Actions


        public void SetGameState(r_GameState _state)
        {
            if (IsServer)
                //m_CurrentGameState.value = _state;

                CheckGameState(_state);
        }

        /*
        [PunRPC]
        private void SetGameState_RPC(r_GameState _state)
        {
            //Set game state
            this.m_CurrentGameState = _state;

            //Check game state
            CheckGameState(_state);
        }
        */

        private void CheckGameState(r_GameState _state)
        {
            /*
            #region OLD
            /*
            switch (_state)
            {
                
                case r_GameState.WAITING:

                    //Start waiting countdown
                    r_InGameTimer.instance.StartTimer(r_InGameMode.Instance.FindGameMode().m_MatchWaitingDuration);

                    //Disable deploying
                    this.m_DeployButton.interactable = false;

                    SetMenuPanel(r_InGameMenuType.SCOREBOARD, true);

                    //Update current game state text
                    UpdateGameStateText(true, "Waiting for players");

                    break;

                case r_GameState.STARTING:

                    //Start game countdown
                    r_InGameTimer.instance.StartTimer(r_InGameMode.Instance.FindGameMode().m_MatchPlayingDuration);

                    //Enable Pause Menu
                    this.m_Paused = true;

                    //Update Pause Menu
                    OnUpdatePauseMenu();

                    SetMenuPanel(r_InGameMenuType.LOADOUT, true);

                    //Enable deploying button
                    this.m_DeployButton.interactable = true;

                    //Change game state
                    SetGameState(r_GameState.PLAYING);

                    UpdateGameStateText(false, string.Empty);

                    break;

                case r_GameState.PLAYING: break;

                case r_GameState.ENDING:

                    //Set time scale
                    Time.timeScale = this.m_OnEndTimeScale;

                    if (PhotonNetwork.IsMasterClient)
                    {
                        //avoid people joining the current room
                        PhotonNetwork.CurrentRoom.IsOpen = false;
                    }

                    //Start game countdown
                    r_InGameTimer.instance.StartTimer(r_InGameMode.Instance.FindGameMode().m_MatchEndingDuration);

                    //Update current game state text
                    UpdateGameStateText(true, "Ending");

                    //Select Team
                    SetMenuPanel(r_InGameMenuType.SCOREBOARD, true);

                    //Enable pause menu
                    this.m_Paused = true;

                    //Update Pause menu
                    OnUpdatePauseMenu();

                    //Enable deploying button
                    this.m_DeployButton.interactable = false;

                    //Disable controls for everyone
                    if (PhotonNetwork.IsMasterClient)
                    {
                        photonView.RPC(nameof(DisableAllPlayerControls), RpcTarget.AllBuffered);
                    }

                    break;

                case r_GameState.ENDED:

                    //Reset Time scale
                    Time.timeScale = 1;

                    //Enable cursor
                    Cursor.lockState = CursorLockMode.None;

                    //End the game, return to main menu
                    EndGame();

                    break;
                    
            #endregion
            */
            switch (_state)
            {
                case r_GameState.WAITING:
                    m_DeployButton.interactable = false;
                    SetMenuPanel(r_InGameMenuType.SCOREBOARD, true);
                    UpdateGameStateText(true, "Waiting for players");
                    break;
                case r_GameState.STARTING:
                    m_Paused = true;
                    OnUpdatePauseMenu();
                    SetMenuPanel(r_InGameMenuType.LOADOUT, true);
                    m_DeployButton.interactable = true;
                    SetGameState(r_GameState.PLAYING);
                    UpdateGameStateText(false, string.Empty);
                    break;
                case r_GameState.ENDING:
                    Time.timeScale = m_OnEndTimeScale;
                    UpdateGameStateText(true, "Ending");
                    SetMenuPanel(r_InGameMenuType.SCOREBOARD, true);
                    m_Paused = true;
                    OnUpdatePauseMenu();
                    m_DeployButton.interactable = false;
                    break;
                case r_GameState.ENDED:
                    Time.timeScale = 1;
                    Cursor.lockState = CursorLockMode.None;
                    EndGame();
                    break;
            }
        }


        public void OnEndedCountdown()
        {
            /* OLD
            int _game_states_length = System.Enum.GetNames(typeof(r_GameState)).Length;

            //avoid to get out of range index issue
            if ((int)(this.m_CurrentGameState + 1) < _game_states_length)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    //Change to next game state when finish the current countdown
                    SetGameState(this.m_CurrentGameState + 1);
                }
            }
            */
            int nextState = (int)m_CurrentGameState + 1;
            if (nextState < System.Enum.GetValues(typeof(r_GameState)).Length)
                SetGameState((r_GameState)nextState);
        }

        /*
        [PunRPC]
        private void DisableAllPlayerControls()
        {
            GameObject[] _players = GameObject.FindGameObjectsWithTag("Player");

            if (_players.Length > 0)
            {
                foreach (GameObject _player in _players)
                {
                    r_PlayerController _player_controller = _player.GetComponent<r_PlayerController>();

                    if (_player_controller != null)
                    {
                        _player_controller.m_InputManager.m_Controllable = false;
                        _player_controller.m_PlayerUI.m_PlayerHUD.SetActive(false);
                    }
                }
            }
        }
        */

        private void SpawnPlayer()
        {
            /*
            if (this.m_Spawned) return;

            Transform _spawn_point = this.m_SpawnPoints[Random.Range(0, this.m_SpawnPoints.Length)];

            if (_spawn_point)
            {
                //Instantiate Player prefab
                this.m_CurrentPlayer = (GameObject)PhotonNetwork.Instantiate("Player/" + m_PlayerPrefab.name, _spawn_point.position, _spawn_point.rotation, 0);

                //Setup player
                this.m_CurrentPlayer.GetComponent<r_PlayerConfig>().SetupLocalPlayer(r_LoadoutManagerGame.instance.m_LoadoutClasses[r_LoadoutManagerGame.instance.m_SelectedLoadoutIndex].GetLoadoutWeaponIDS());

                //Prioritize player HUD
                this.m_CurrentPlayer.GetComponentInChildren<Canvas>().sortingOrder = 0;

                //Disable scene camera 
                this.m_MainCamera.enabled = false;
                this.m_MainListener.enabled = false;

                //Update states
                this.m_Paused = false;
                this.m_Spawned = true;

                //Change button interactable
                this.m_DeployButton.interactable = false;

                //Update pause menu
                OnUpdatePauseMenu();
            }
            */
            if (m_Spawned || !IsClient) return;

            Transform spawnPoint = m_SpawnPoints[Random.Range(0, m_SpawnPoints.Length)];

            var playerObject = Instantiate(m_PlayerPrefab, spawnPoint.position, spawnPoint.rotation);
            var networkObject = playerObject.GetComponent<NetworkObject>();
            networkObject.SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);

            playerObject.GetComponent<r_PlayerConfig>().SetupLocalPlayer(
                r_LoadoutManagerGame.instance.m_LoadoutClasses[
                    r_LoadoutManagerGame.instance.m_SelectedLoadoutIndex
                ].GetLoadoutWeaponIDS());

            playerObject.GetComponentInChildren<Canvas>().sortingOrder = 0;
            m_MainCamera.enabled = false;
            m_MainListener.enabled = false;

            m_CurrentPlayer = playerObject;
            m_Paused = false;
            m_Spawned = true;
            m_DeployButton.interactable = false;
            OnUpdatePauseMenu();
        }

        private void OnUpdatePauseMenu()
        {
            /*
            //Cursor lock update
            Cursor.lockState = this.m_Paused ? CursorLockMode.None : CursorLockMode.Locked;

            //Pause menu
            this.m_PauseMenu.SetActive(this.m_Paused);

            //Lock player controller
            if (this.m_CurrentPlayer != null)
            {
                if (this.m_CurrentGameState != r_GameState.ENDING && this.m_CurrentGameState != r_GameState.ENDED)
                {
                    r_PlayerController _player_controller = this.m_CurrentPlayer.GetComponent<r_PlayerController>();

                    if (_player_controller != null)
                    {
                        _player_controller.m_InputManager.m_Controllable = !this.m_Paused;
                    }
                }
            }
            */
            Cursor.lockState = m_Paused ? CursorLockMode.None : CursorLockMode.Locked;
            m_PauseMenu.SetActive(m_Paused);

            if (m_CurrentPlayer != null && m_CurrentGameState != r_GameState.ENDING && m_CurrentGameState != r_GameState.ENDED)
            {
                var controller = m_CurrentPlayer.GetComponent<r_PlayerController>();
                if (controller != null)
                {
                    controller.m_InputManager.m_Controllable = !m_Paused;
                }
            }
        }

        public void ResetLocalGameSettings()
        {
            /*
            //Enable scene camera
            this.m_MainCamera.enabled = true;
            this.m_MainListener.enabled = true;

            //Update states
            this.m_Paused = true;
            this.m_Spawned = false;

            //Update pause menu
            OnUpdatePauseMenu();

            //Change button interactable
            this.m_DeployButton.interactable = true;

            //Empty current player
            this.m_CurrentPlayer = null;
            */
            m_MainCamera.enabled = true;
            m_MainListener.enabled = true;
            m_Paused = true;
            m_Spawned = false;
            OnUpdatePauseMenu();
            m_DeployButton.interactable = true;
            m_CurrentPlayer = null;
        }

        public void UpdateGameStateText(bool _state, string _text)
        {
            this.m_GameStateText.GetComponentInChildren<Text>().text = _text;

            this.m_GameStateText.SetActive(_state);
        }

        private void HideMenuPanels()
        {
            foreach (r_PauseMenuPanel _panel in m_MenuPanels)
            {
                //Disable Panel
                _panel.m_Panel.SetActive(false);
            }
        }

        private void SetMenuPanel(r_InGameMenuType _menu_type, bool _state)
        {
            /*
            if (FindInGameMenuPanel(_menu_type) != null)
            {
                //Hide menu panels
                HideMenuPanels();

                //Set panel state
                FindInGameMenuPanel(_menu_type).m_Panel.SetActive(_state);
            }
            */
            var panel = m_MenuPanels.Find(x => x.m_InGameMenuType == _menu_type);
            if (panel != null)
            {
                HideMenuPanels();
                panel.m_Panel.SetActive(_state);
            }
        }

        private void EndGame()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        private void LeaveRoom()
        {
            if (IsClient)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }
        #endregion

        #region Get - ������
        /*
        public Player GetPlayerByActorID(int _actor_id)
        {
            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.Players.ContainsKey(_actor_id))
            {
                return PhotonNetwork.CurrentRoom.GetPlayer(_actor_id);
            }
            return null;
        }

        public Player GetRandomPhotonPlayer(string _sender_name)
        {
            foreach (Player _player in PhotonNetwork.PlayerList)
            {
                if (_player.NickName != _sender_name)
                    return _player;
            }
            return null;
        }

        private r_PauseMenuPanel FindInGameMenuPanel(r_InGameMenuType _menu_type) => this.m_MenuPanels.Find(x => x.m_InGameMenuType == _menu_type);
        */
        #endregion
    } 
}
