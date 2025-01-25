using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUIScript : MonoBehaviour
{
    public GameObject backgroundGameObject;
    public GameObject playButtonGameObject;
    public GameObject playerListGameObject;

    public GameObject startGameButton;

    public TMP_Text player1Label;
    public TMP_Text player2Label;
    public TMP_Text player3Label;
    public TMP_Text player4Label;

    private List<TMP_Text> playerLabelList = new List<TMP_Text>();

    private void Awake()
    {
        playerLabelList.Add(player1Label);
        playerLabelList.Add(player2Label);
        playerLabelList.Add(player3Label);
        playerLabelList.Add(player4Label);
    }

    private void Start()
    {
        startGameButton.SetActive(false);
        InvokeRepeating("UpdatePlayerListUI", 0f, 1f);
    }

    public void UpdatePlayerListUI()
    {
        if (GameManager.instance != null)
        {
            for (int i = 0; i < GameManager.instance.usernames.Count; i++)
            {
                playerLabelList[i].text = GameManager.instance.usernames[i].ToString();
                playerLabelList[i].gameObject.SetActive(true);
            }
        }
    }

    public void Play()
    {
        ushort port = 7777;

        // default is 7777 but we still ask NetworkManager for the asigned port
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport transport)
        {
            port = (ushort)transport.ConnectionData.Port;
        }
        else
        {
            Debug.LogWarning("NetworkManager or UnityTransport not found.");
        }

        bool alreadyInUse = System.Net.NetworkInformation.IPGlobalProperties
            .GetIPGlobalProperties().GetActiveUdpListeners().Any(p => p.Port == port);

        if (alreadyInUse)
        {
            Debug.Log("Port is in use. Starting as a client...");
            StartClient();
        }
        else
        {
            Debug.Log("Port is free. Starting as a host...");
            StartHost();
        }

        backgroundGameObject.SetActive(false);
        playButtonGameObject.SetActive(false);
        playerListGameObject.SetActive(true);
    }

    public void StartGame()
    {
        if (GameManager.instance.StartGame())
        {
            playerListGameObject.SetActive(false);
            startGameButton.SetActive(false);
        }

    }

    private void StartHost()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartHost();
            Debug.Log("Hosting game...");
        }
        else
        {
            Debug.LogError("No NetworkManager found!");
        }

        startGameButton.SetActive(true);
    }

    private void StartClient()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartClient();
            Debug.Log("Joining game as client...");
        }
        else
        {
            Debug.LogError("No NetworkManager found!");
        }
    }
}