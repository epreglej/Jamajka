using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum SquareType
{
    PirateLair, Sea, Port
}

public class Square : MonoBehaviour
{
    // ID must be interpreted as a sequential number of the square for easier player movement configuration
    public int id = 0;
    public SquareType type;
    public List<GameObject> playerGameObjectsOnSquare;

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

    public void AddPlayerGameObjectToSquare(GameObject playerGameObject)
    {
        playerGameObjectsOnSquare.Add(playerGameObject);
    }

    public void RemovePlayerGameObjectFromSquare(GameObject playerGameObject)
    {
        playerGameObjectsOnSquare.Remove(playerGameObject);
    }

    public List<GameObject> GetPlayerGameObjectsOnSquare()
    {
        return playerGameObjectsOnSquare;
    }
}

