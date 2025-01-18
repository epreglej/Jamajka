using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class Square : NetworkBehaviour
{
    // ID must be interpreted as a sequential number of the square for easier player movement configuration
    public int id;
    public int resourceValue;
    public GameManager.SquareType type;
    public GameManager.TreasureCard treasureCard;

    public List<int> playerIndicesOnSquare;

    // ili na onnetworkspawn staviti
    private void Start()
    {
        Transform canvasTransform = transform.Find("Canvas"); // Find child named "Canvas"

        if (canvasTransform != null)
        {
            TextMeshProUGUI textComponent = canvasTransform.Find("Resources").GetComponent<TextMeshProUGUI>();

            if(resourceValue == 0)
            {
                textComponent.text = "";
            }
            else if (resourceValue == 1)
            {
                textComponent.text = "◆";
            }
            else if (resourceValue == 2)
            {
                textComponent.text = "◆◆";
            }
            else if (resourceValue == 3) 
            {
                textComponent.text = "◆◆◆";
            }
            else if(resourceValue == 4)
            {
                textComponent.text = "◆◆\n◆◆";
            }
        }
        else
        {
            Debug.LogWarning("No Canvas child found for this Square.");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // SetTreasureCard(GameManager.TreasureCard.SaransSaber);
    }

    [ClientRpc]
    public void AddPlayerIndexToSquareClientRpc(int index)
    {
        playerIndicesOnSquare.Add(index);
    }

    [ClientRpc]
    public void RemovePlayerIndexFromSquareClientRpc(int index)
    {
        playerIndicesOnSquare.Remove(index);
    }

    public List<int> GetPlayerIndexesOnSquare()
    {
        return playerIndicesOnSquare;
    }

    public int GetResourceValue()
    {
        return resourceValue;
    }

    public void SetTreasureCard(GameManager.TreasureCard treasureCard)
    {
        SetTreasureCardClientRpc(treasureCard);
    }

    [ClientRpc]
    private void SetTreasureCardClientRpc(GameManager.TreasureCard treasureCard)
    {
        if(type == GameManager.SquareType.PirateLair)
        {
            Debug.Log("Setting treasure");
            this.treasureCard = treasureCard;
        }
    }

    public GameManager.TreasureCard GetTreasureCard()
    {
        return this.treasureCard;
    }
}

