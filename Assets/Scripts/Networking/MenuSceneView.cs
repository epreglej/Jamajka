using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class MenuSceneView : MonoBehaviour
{
    [SerializeField] Button joinLobbyButton;
    [SerializeField] TMP_InputField usernameInputField;

    void Awake()
    {
        joinLobbyButton.onClick.AddListener(JoinOrCreateLobby);
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

    async Task<Lobby> CreateLobby()
    {
        try
        {
            // Usually you need to authenticate beforehand.

            var relayJoinCode = await NetworkServiceManager.instance.InitializeHost();
            if (this == null) return null;

            var lobby = await LobbyManager.instance.CreateLobby("Lobby",
                6, PlayerDataManager.instance.GetName(), relayJoinCode);
            if (this == null) return null;

            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
            return lobby;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return null;
        }
    }
        
    async Task<Lobby> JoinLobby()
    {
        try
        {
            // Usually you need to authenticate beforehand.

            var lobby = await LobbyManager.instance.QuickJoinLobby(PlayerDataManager.instance.GetName());
            if (this == null) return null;

            var relayJoinCode = lobby.Data[LobbyManager.k_RelayJoinCodeKey].Value;
            await NetworkServiceManager.instance.InitializeClient(relayJoinCode);
            if (this == null) return null;

            return lobby;
        }
        catch (Exception)
        {
            Debug.Log("Probably failed to find active lobby.");
            return null;
        }
    }

    async void JoinOrCreateLobby()
    {
        try
        {
            await Authenticate();
            if (this == null) return;
        }
        catch (Exception e) 
        {
            Debug.LogException(e);
        }

        Lobby lobby = null;

        try
        {
            lobby = await JoinLobby();
            if (this == null) return;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        if(lobby == null)
        {
            try
            {
                lobby = await CreateLobby();
                if (this == null) return;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

    }
}
