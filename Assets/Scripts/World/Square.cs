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
    // public TreasureCard tc 

    public List<int> playerIndicesOnSquare;

    // ili na onnetworkspawn staviti
    private void Start()
    {
        Transform canvasTransform = transform.Find("Canvas"); // Find child named "Canvas"

        if (canvasTransform != null)
        {
            TextMeshProUGUI textComponent = canvasTransform.GetComponentInChildren<TextMeshProUGUI>();

            if (textComponent != null)
            {
                if (int.TryParse(textComponent.text, out int parsedId))
                {
                    id = parsedId;
                }
                else
                {
                    Debug.LogWarning("Failed to parse Square ID from the Text (TMP). Ensure it contains a valid integer.");
                }
            }
            else
            {
                Debug.LogWarning("No TextMeshProUGUI component found within the Canvas of this Square.");
            }
        }
        else
        {
            Debug.LogWarning("No Canvas child found for this Square.");
        }
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
}

