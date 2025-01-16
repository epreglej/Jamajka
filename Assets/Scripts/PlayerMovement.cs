using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    void Start()
    {
        this.transform.position = SquareManager.instance.squares[0].transform.position;

        // MoveXSquares(10);
    }

    public void MoveToSquare(int squareID)
    {
        foreach (var square in SquareManager.instance.squares)
        {
            if (square.id == squareID) 
            {
                Vector3 destination = square.transform.position;
                if (square.GetPlayerIndexesOnSquare().Count == 1)
                {
                    destination = destination + new Vector3(-1, 0, 2);
                }
                else if (square.GetPlayerIndexesOnSquare().Count == 2)
                {
                    destination = destination + new Vector3(2, 0, -1);
                }
                else if (square.GetPlayerIndexesOnSquare().Count == 3)
                {
                    destination = destination + new Vector3(-2, 0, 1);
                }

                square.AddPlayerIndexToSquareClientRpc(this.GetComponent<PlayerGameScript>().player_index.Value);
                this.GetComponent<PlayerGameScript>().currentSquareID = new NetworkVariable<int>(squareID);
                MoveTowardsTargetPositionClientRpc(destination);
            }
        }
    }

    public void MoveXSquares(int x)
    {
        MoveToSquare(this.GetComponent<PlayerGameScript>().currentSquareID.Value + x);
    }

    [ClientRpc]
    private void MoveTowardsTargetPositionClientRpc(Vector3 targetPosition)
    {
        StartCoroutine(MoveCoroutine(targetPosition));
    }

    private IEnumerator MoveCoroutine(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.1f);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, 0.05f);

            yield return null;
        }
    }
}
