using UnityEngine;
using Unity.Netcode;
using System.Threading.Tasks;
using System.Collections.Generic;


public class PlayerGameScript : NetworkBehaviour
{
    public NetworkVariable<int> player_index = new NetworkVariable<int>(0);

    GameManager.ActionCardOption cardOption1 = GameManager.ActionCardOption.None;
    GameManager.ActionCardOption cardOption2 = GameManager.ActionCardOption.None;

    List<Hold> holds = new List<Hold>();

    private GameManager.TokenType replaceHold_token;
    private int replaceHold_ammount;
    private int replaceHold_index = -1;
    public struct Hold
    {
        public GameManager.TokenType tokenType;
        public int ammount;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        for (int i = 0; i < 5; i++)
        {
            Hold h = new();
            h.tokenType = GameManager.TokenType.None;
            h.ammount = 0;

            holds.Add(h);
        }

        //GameManager.instance.AddPlayerServerRPC(gameObject);
    }

    public void AddInitialResources()
    {
        Hold hold1 = holds[0];
        hold1.tokenType = GameManager.TokenType.Food;
        hold1.ammount = 3;

        Hold hold2 = holds[1];
        hold2.tokenType = GameManager.TokenType.Gold;
        hold2.ammount = 3;
    }

    public void AddResources(GameManager.TokenType token, int ammount)
    {
        int index_of_freeHold = -1;

        for(int i = 0; i < 5; i++)
        {
            Hold h = holds[i];
            if(h.tokenType == GameManager.TokenType.None)
            {
                index_of_freeHold = i;
                break;
            }
        }

        if(index_of_freeHold >= 0)
        {
            Hold h = holds[index_of_freeHold];
            h.tokenType = token;
            h.ammount = ammount;
        }
        else
        {
            replaceHold_ammount = ammount;
            replaceHold_token = token;

            ReplaceResource();
        }

    }

    public void ReplaceResource()
    {
        // open the needed ui
    }

    public void OnReplaceResourceHoldChosen()
    {
        Hold h = holds[replaceHold_index];
        h.tokenType = replaceHold_token;
        h.ammount = replaceHold_ammount;

        // display the choice

        replaceHold_index = -1;
    }


    [ClientRpc]
    public void ChooseACardClientRpc()
    {
        if (!IsOwner) return;

        // draw a card
        // draw 3 first round, 1 others
        // draw to 4 if has MorgansMap

        // choose a card

        GameManager.instance.PlayerIsReadyServerRpc();
    }

    public void SetActionCards(GameManager.ActionCardOption o1, GameManager.ActionCardOption o2)
    {
        cardOption1 = o1;
        cardOption2 = o2;
    }
}
