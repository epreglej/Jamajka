using System.Collections.Generic;
using UnityEngine;

public class SquareManager : MonoBehaviour
{
    public List<Square> squares = new List<Square>();

    private int firstSquareID;
    private int lastSquareID;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find all Squares that are child to current GameObject (Square Manager)
        squares.AddRange(gameObject.GetComponentsInChildren<Square>());
        squares.Sort((x, y) => x.id.CompareTo(y.id));

        firstSquareID = squares[0].id;
        lastSquareID = squares[-1].id;
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
