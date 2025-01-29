using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    public bool isMoving = false;
    private Queue<int> movementQueue = new Queue<int>();
    private List<int> squaresToMove = new List<int>();
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float movementSpeed = 15f;
    private float rotationSpeed = 10f;

    private enum MovementState
    {
        Idle,
        Moving
    }
    private MovementState currentState = MovementState.Idle;

    private void Start()
    {
        if (SquareManager.instance != null)
        {
            this.transform.position = SquareManager.instance.squares[0].transform.position;
        }
    }

    public void MoveXSquares(int x)
    {
        if (currentState != MovementState.Idle)
        {
            movementQueue.Enqueue(x);
            return;
        }

        StartMovement(x);
    }

    private void StartMovement(int x)
    {
        isMoving = true;
        currentState = MovementState.Moving;
        Debug.Log("Move for x squares: " + x);

        int startingField = this.GetComponent<PlayerGameScript>().currentSquareID.Value;

        // Generate the list of squares to move through
        squaresToMove.Clear();
        if (x > 0)
        {
            // Moving forward
            for (int i = 1; i <= x; i++)
            {
                int nextSquareID = (startingField + i) % 31;
                squaresToMove.Add(nextSquareID);
            }
        }
        else if (x < 0)
        {
            // Moving backward
            Debug.Log("Moving backwards.");
            for (int i = -1; i >= x; i--)
            {
                // Ensure the result is non-negative and within the valid range
                int nextSquareID = (startingField + i + 31) % 31;
                squaresToMove.Add(nextSquareID);
            }
        }

        // Start moving to the first square in the list
        if (squaresToMove.Count > 0)
        {
            MoveToSquare(squaresToMove[0]);
        }
        else
        {
            // No squares to move to (e.g., x = 0)
            currentState = MovementState.Idle;
            isMoving = false;
            Debug.Log("No movement required.");
        }

        // Track the player with the camera
        CameraController.instance.TrackPlayerClientRpc(this, default);
    }

    private void MoveToSquare(int squareID)
    {
        if (squareID == 0 && this.GetComponent<PlayerGameScript>().currentSquareID.Value == 30)
        {
            Debug.Log("Lap completed.");
        }

        foreach (var square in SquareManager.instance.squares)
        {
            if (square.id == this.GetComponent<PlayerGameScript>().currentSquareID.Value)
            {
                square.RemovePlayerIndexFromSquareClientRpc(this.GetComponent<PlayerGameScript>().player_index.Value);
            }
        }

        foreach (var square in SquareManager.instance.squares)
        {
            if (square.id == squareID)
            {
                Vector3 destination = square.transform.position;

                if (square.GetPlayerIndexesOnSquare().Count == 1)
                {
                    destination += new Vector3(-2, 0, 2);
                }
                else if (square.GetPlayerIndexesOnSquare().Count == 2)
                {
                    destination += new Vector3(2, 0, -2);
                }
                else if (square.GetPlayerIndexesOnSquare().Count == 3)
                {
                    destination += new Vector3(-3, 0, 1);
                }

                square.AddPlayerIndexToSquareClientRpc(this.GetComponent<PlayerGameScript>().player_index.Value);
                this.GetComponent<PlayerGameScript>().currentSquareID.Value = squareID;

                SetTargetPositionClientRpc(destination);
            }
        }
    }

    [ClientRpc]
    private void SetTargetPositionClientRpc(Vector3 destination)
    {
        targetPosition = destination;
        targetRotation = Quaternion.LookRotation((destination - transform.position).normalized);
        currentState = MovementState.Moving;
    }

    private void Update()
    {
        if (currentState == MovementState.Moving)
        {
            MoveTowardsTargetPosition();
        }
    }

    private void MoveTowardsTargetPosition()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, movementSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            if (squaresToMove.Count == 0)
            {
                currentState = MovementState.Idle;
                isMoving = false;
                Debug.Log("Movement over");

                CameraController.instance.ResetCameraClientRpc();

                if (movementQueue.Count > 0)
                {
                    int nextX = movementQueue.Dequeue();
                    StartMovement(nextX);
                }
                return;
            }

            squaresToMove.RemoveAt(0);

            if (squaresToMove.Count > 0)
            {
                MoveToSquare(squaresToMove[0]);
            }
            else
            {
                currentState = MovementState.Idle;
                isMoving = false;
                Debug.Log("Movement over");

                CameraController.instance.ResetCameraClientRpc();

                if (movementQueue.Count > 0)
                {
                    int nextX = movementQueue.Dequeue();
                    StartMovement(nextX);
                }
            }
        }
    }
}