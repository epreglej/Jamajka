using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbySceneView : MonoBehaviour
{
    [SerializeField] Button toggleReadyStateButton;
    [SerializeField] TMP_Text toggleReadyStateButtonText;
    bool isHost => LobbyManager.instance.isHost;
    

    void Awake()
    {
        toggleReadyStateButton.onClick.AddListener(ToggleReadyState);
    }

    void Start()
    {
        LobbyManager.OnPlayersReady += PlayersReady;
    }

    void PlayersReady()
    {
        Debug.Log("Signal received!");
        if (isHost)
        {
            Debug.Log("Starting game!");
            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
    }

    async void ToggleReadyState()
    {
        try
        {
            await LobbyManager.instance.ToggleReadyState();
            if(toggleReadyStateButtonText.text == "Ready")
            {
                toggleReadyStateButtonText.text = "Unready";
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}
