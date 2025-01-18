using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    private bool isMoving = false;

    void Start()
    {
        this.transform.position = SquareManager.instance.squares[0].transform.position;

        // ovo zvati za kretanje
        // MoveXSquares(-1);
    }

    public void MoveXSquares(int x)
    {
        StartCoroutine(MoveXSquaresCoroutine(x));
    }

    private IEnumerator MoveXSquaresCoroutine(int x)
    {
        int startingField = this.GetComponent<PlayerGameScript>().currentSquareID.Value;

        if (x > 0)
        {
            for (int i = 1; i <= x; i++)
            {
                MoveToSquare((startingField + i) % 31);
                yield return new WaitForSeconds(1f);
            }
        }
        else if (x < 0) 
        {
            Debug.Log("moving backwards.");
            for (int i = -1; i >= x; i--)
            {
                MoveToSquare(((startingField + i) % 31 + 31) % 31);
                yield return new WaitForSeconds(1f);
            }
        }
    }

    public void MoveToSquare(int squareID)
    {
        if(squareID == 0 && this.GetComponent<PlayerGameScript>().currentSquareID.Value == 30)
        {
            // TODO: dominik?
            // ovo nije dobar kod ako se mogu vracati unazad pa onda opet 1 korak naprijed???
            Debug.Log("Lap completed.");
        }

        foreach (var square in SquareManager.instance.squares)
        {
            if (square.id == squareID) 
            {
                Vector3 destination = square.transform.position;
                if (square.GetPlayerIndexesOnSquare().Count == 1)
                {
                    destination = destination + new Vector3(-2, 0, 2);
                }
                else if (square.GetPlayerIndexesOnSquare().Count == 2)
                {
                    destination = destination + new Vector3(2, 0, -2);
                }
                else if (square.GetPlayerIndexesOnSquare().Count == 3)
                {
                    destination = destination + new Vector3(-3, 0, 1);
                }

                square.AddPlayerIndexToSquareClientRpc(this.GetComponent<PlayerGameScript>().player_index.Value);
                this.GetComponent<PlayerGameScript>().currentSquareID = new NetworkVariable<int>(squareID);
                MoveTowardsTargetPositionClientRpc(destination);
            }
            else if (square.id == this.GetComponent<PlayerGameScript>().currentSquareID.Value)
            {
                square.RemovePlayerIndexFromSquareClientRpc(this.GetComponent<PlayerGameScript>().player_index.Value);
            }
        }
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
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.01f);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, 0.05f);

            yield return null;
        }
    }
}
