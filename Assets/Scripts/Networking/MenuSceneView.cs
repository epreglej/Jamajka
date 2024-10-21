using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Unity.Services.Samples.ServerlessMultiplayerGame
{
    public class MenuSceneView : MonoBehaviour
    {
        [SerializeField] Button createLobbyButton;
        [SerializeField] Button joinLobbyButton;
        [SerializeField] TMP_InputField usernameInputField;
        //[SerializeField] Button startGameButton;

        void Awake()
        {
            createLobbyButton.onClick.AddListener(CreateGame);
            joinLobbyButton.onClick.AddListener(JoinGame);
            //startGameButton.onClick.AddListener(StartGame);
        }

        async Task Authenticate()
        {
            if (usernameInputField.text != "")
            {
                PlayerDataManager.instance.SetName(usernameInputField.text);
            }
            else
            {
                PlayerDataManager.instance.SetName("Player" + UnityEngine.Random.Range(0, 1000));
            }

            await AuthenticationManager.SignInAnonymously(PlayerDataManager.instance.GetName());
        }

        async void CreateGame()
        {
            try
            {
                await Authenticate();
                if (this == null) return;

                var relayJoinCode = await NetworkServiceManager.instance.InitializeHost();
                if (this == null) return;

                var lobby = await LobbyManager.instance.CreateLobby("Lobby",
                    6, PlayerDataManager.instance.GetName(), relayJoinCode);
                if (this == null) return;

                NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        
        async void JoinGame()
        {
            try
            {
                await Authenticate();
                if (this == null) return;

                var lobby = await LobbyManager.instance.QuickJoinLobby(PlayerDataManager.instance.GetName());
                if (this == null) return;

                var relayJoinCode = lobby.Data[LobbyManager.k_RelayJoinCodeKey].Value;
                await NetworkServiceManager.instance.InitializeClient(relayJoinCode);
                if (this == null) return;

                //SceneManager.LoadScene("Lobby");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /*
        async void StartGame()
        {
            if (true)
            {
                SceneManager.LoadScene("Game");
            }
            else
            {
                Debug.LogError("Only the host can start the game!");
            }
        }*/
    }
}
