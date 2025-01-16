using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public class SquareManager : NetworkBehaviour
{
    public static SquareManager instance { get; private set; }

    public List<Square> squares = new List<Square>();

    private int firstSquareID;
    private int lastSquareID;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        };
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find all Squares that are child to current GameObject (Square Manager)
        squares.AddRange(gameObject.GetComponentsInChildren<Square>());
        squares.Sort((x, y) => x.id.CompareTo(y.id));

        firstSquareID = squares[0].id;
        lastSquareID = squares[squares.Count - 1].id;
    }

    public List<int> GetPlayerIndicesFromSquareWithId(int id)
    {
        foreach (var square in squares)
        {
            if(square.id == id)
            {
                return square.playerIndicesOnSquare;
            }
        }
        return null;
    }
}
