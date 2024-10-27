using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerGameScript : NetworkBehaviour
{
    int player_index = 0;

    GameManager.ActionCardOption cardOption1 = GameManager.ActionCardOption.None;
    GameManager.ActionCardOption cardOption2 = GameManager.ActionCardOption.None;

    public struct Hold
    {
        GameManager.TokenType tokenType;
        int ammount;
    }

    
    public void ChooseACard()
    {

    }

    [ClientRpc]
    public void SetActionCardsClientRPC(GameManager.ActionCardOption o1, GameManager.ActionCardOption o2)
    {
        cardOption1 = o1;
        cardOption2 = o2;
    }
}
