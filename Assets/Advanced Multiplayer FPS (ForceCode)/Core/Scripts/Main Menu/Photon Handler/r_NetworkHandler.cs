using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace ForceCodeFPS
{

    /// <summary>
    /// Запуск хоста и загрузка выбранной сцены
    /// r_NetworkHandler.instance.CreateRoomAndStartHost("MyGameScene");
    /// Присоединение как клиент
    /// r_NetworkHandler.instance.JoinAsClient();
    /// Запуск сервера без клиента (например, для теста)
    /// r_NetworkHandler.instance.StartServer("MyGameScene");
    /// </summary>
    public class r_NetworkHandler : MonoBehaviour
    {
        public static r_NetworkHandler instance;

        #region Functions
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void StartServer(string sceneName)
        {
            Debug.Log("[NGO] Starting dedicated server...");
            NetworkManager.Singleton.StartServer();
            SceneManager.LoadScene(sceneName);
        }
        #endregion

        #region Actions


        #endregion

        #region Callbacks

        public void CreateRoomAndStartHost(string sceneName)
        {
            Debug.Log("[NGO] Starting host and loading scene: " + sceneName);
            NetworkManager.Singleton.StartHost();
            SceneManager.LoadScene(sceneName);
        }

        public void JoinAsClient()
        {
            Debug.Log("[NGO] Starting client...");
            NetworkManager.Singleton.StartClient();
        }

   
        #endregion
    }
}