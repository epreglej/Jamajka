using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerGameScript : NetworkBehaviour
{
    int player_index = 0;
    

    public struct Hold
    {
        GameManager.TokenType tokenType;
        int ammount;
    }


}
