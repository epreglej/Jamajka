using UnityEngine;
using Unity.Netcode;

public class ActionCard_CardOptionScript : MonoBehaviour
{

    [SerializeField] int option_index = 0;
    public GameObject player = null;

    public void OnOptionChosen()
    {
        Debug.Log("Clicked option");
        player.GetComponent<PlayerGameScript>().ActionCardChosen(option_index);
    }
}
