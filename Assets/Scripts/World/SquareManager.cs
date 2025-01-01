using System.Collections.Generic;
using UnityEngine;

public class SquareManager : MonoBehaviour
{
    public List<Square> squares = new List<Square>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find all Squares that are child to current GameObject (Square Manager)
        squares.AddRange(gameObject.GetComponentsInChildren<Square>());
    }

    public Dictionary<GameObject, Square> GetSquaresOfAllPlayerGameObjects()
    {
        Dictionary<GameObject, Square> squaresOfAllPlayerGameObjects = new Dictionary<GameObject, Square>();

        foreach (var player in GameManager.instance.players)
        {
            squaresOfAllPlayerGameObjects.Add(player.gameObject, player.gameObject.GetComponent<Square>());
        }

        return squaresOfAllPlayerGameObjects;
    }
}
