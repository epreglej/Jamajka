using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public class SquareManager : NetworkBehaviour
{
    public static SquareManager instance { get; private set; }

    public List<Square> squares = new List<Square>();

    private int firstSquareID;
    public int lastSquareID;


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
            if (square.id == id)
            {
                return square.playerIndicesOnSquare;
            }
        }
        return null;
    }

    public Square GetPlayerSquare(int player_index)
    {
        foreach (var square in squares)
        {
            if (square.playerIndicesOnSquare.Contains(player_index))
            {
                return square;
            }
        }

        return null;
    }

    public int GetPlayerSquareID(int player_index)
    {
        foreach (var square in squares)
        {
            if (square.playerIndicesOnSquare.Contains(player_index))
            {
                return square.id;
            }
        }

        return -1;
    }

    public GameManager.SquareType GetPlayerSquareType(int player_index)
    {
        foreach (var square in squares)
        {
            if (square.playerIndicesOnSquare.Contains(player_index))
            {
                return square.type;
            }
        }

        return GameManager.SquareType.None;
    }

    public int FindPreviousSquareType(int square_id, GameManager.SquareType targetSquareType)
    {
        int delta = 1;

        while(((square_id - delta) > 0) && squares[square_id - delta].type != targetSquareType)
        {
            delta++;
        }

        return delta;
    }
}
