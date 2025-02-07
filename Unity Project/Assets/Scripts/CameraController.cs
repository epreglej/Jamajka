using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CameraController : NetworkBehaviour
{
    public static CameraController instance { get; private set; }

    private Vector3 initialPosition;
    public GameObject player;

    private void Start()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }

        initialPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (this.player != null)
        {
            transform.position = Vector3.Lerp(transform.position, player.transform.position + new Vector3(-15, 30, 0), 0.05f);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, initialPosition, 0.05f);
        }
    }

    [ClientRpc]
    public void TrackPlayerClientRpc(NetworkBehaviourReference playerReference, ClientRpcParams clientRpcParams)
    {
        if (playerReference.TryGet<PlayerMovement>(out PlayerMovement player))
        {
            this.player = player.gameObject;
        }
    }

    [ClientRpc]
    public void ResetCameraClientRpc()
    {
        this.player = null;
    }
}
