using UnityEngine;
using Unity.Netcode;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Collections;


public class PlayerGameScript : NetworkBehaviour
{
    public string username = string.Empty;
    public NetworkVariable<int> player_index = new NetworkVariable<int>(-1);

    // card options
    GameManager.ActionCardOption cardOption1 = GameManager.ActionCardOption.None;
    GameManager.ActionCardOption cardOption2 = GameManager.ActionCardOption.None;

    public List<Hold> holds = new List<Hold>();
    public List<ActionCard> deck = new List<ActionCard>();

    // action cards in hand
    private int replace_action_card_number = -1; // last used card (1, 2, 3 or 4)
    public ActionCard action_card_1;
    public ActionCard action_card_2;
    public ActionCard action_card_3;
    public ActionCard action_card_4; // only if morgans map


    // hold varables
    private GameManager.TokenType replaceHold_token;
    private int replaceHold_amount;
    private int replaceHold_index = -1;

    // action cards
    public bool hasMorgansMap = false; // draw 2 cards treasure card
    public bool hasSaransSaber = false; // rerol combat dice
    public bool hasLadyBeth = false; // +2 in combat
    public bool has6thHold = false; // 6. hold spot, we ignore this for now

    public struct Hold
    {
        public GameManager.TokenType tokenType;
        public int amount;
    }

    public void Awake()
    {
        username = "Player" + Random.Range(1,1000);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        for (int i = 0; i < 5; i++)
        {
            Hold h = new();
            h.tokenType = GameManager.TokenType.None;
            h.amount = 0;

            holds.Add(h);
        }    
    }

    private void Start()
    {
        if (IsOwner)
        {
            ulong playerNetworkObjectId = GetComponent<NetworkObject>().NetworkObjectId;
            GameManager.instance.AddPlayerServerRPC(playerNetworkObjectId);
            InitializeDeck();
        }
    }
    
    // Osigurava da svaki player ima listu drugih na istom indexu
    [Rpc(SendTo.NotServer, RequireOwnership = false)]
    public void SetupListOnClientRpc(int index)
    {
        Debug.Log("Setting player to index " + index);
        SetupListOnClient(index);

        if(index != player_index.Value)
        {
            Debug.LogError("WHATAFAK, PlayerGameScript");
        }
    }

    // Lokalno postavljanje playera
    async void SetupListOnClient(int index)
    {
        while (GameManager.instance.players.Count < index)
        {
            await Task.Delay(10);
        }

        if (GameManager.instance.players.Count == index) GameManager.instance.players.Add(this);

    }

    // TODO : DOMINIK
    public void InitializeDeck()
    {
        // add action cards to deck

        ActionCardsUIScript ui = GameManager.instance.ActionCardUI.GetComponent<ActionCardsUIScript>();
        ui.UpdateActionCards(gameObject);
    }

    // TODO - DUJE - ovak mozes dodavat i makivat Holdove
    public void AddInitialResources()
    {
        Hold hold1 = holds[0];
        hold1.tokenType = GameManager.TokenType.Food;
        hold1.amount = 3;

        Hold hold2 = holds[1];
        hold2.tokenType = GameManager.TokenType.Gold;
        hold2.amount = 3;
    }

    // TODO - DUJE - ovo pozivas da bi dodal resurse u hold ( ako ima free space sam doda - treba prikazat, ako nema free space treba zamijenit )

    async public void AddResources(GameManager.TokenType token, int amount)
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
            h.amount = amount;
        }
        else
        {
            replaceHold_amount = amount;
            replaceHold_token = token;

            // TODO - DUJE - ovo pokrece odabir holda koji swapas
            await ReplaceResource();
        }

        // TODO - DUJE - refresh ui za prikaz dostupnih resursi

    }

    async public Task ReplaceResource()
    {
        // TODO - DUJE - open the needed ui

        // wait for the replace index to be chosen
        while(replaceHold_index < 0)
        {
            await Task.Delay(100);
        }

        OnReplaceResourceHoldChosen();
    }

    public void OnReplaceResourceHoldChosen()
    {
        Hold h = holds[replaceHold_index];
        h.tokenType = replaceHold_token;
        h.amount = replaceHold_amount;

        replaceHold_index = -1;
    }


    [Rpc(SendTo.Everyone)]
    public void ChooseACardClientRpc()
    {
        if (!IsOwner) return;

        // TODO - DOMINIK

        // draw a card
        // draw 3 first round, 1 others
        // draw to 4 if has MorgansMap
        
        // UpdateActionCardsInHand()

        GameManager.instance.ActionCardUI.GetComponent<ActionCardsUIScript>().ChooseCardCalled();
    }

    public void ActionCardChosen(int index)
    {
        if (!IsOwner) return;

        Debug.Log("Player chose card " + index.ToString());

        replace_action_card_number = index;

        GameManager.instance.ActionCardUI.GetComponent<ActionCardsUIScript>().CloseChooseACardMenu();

        GameManager.instance.PlayerIsReadyServerRpc();
    }

    [ClientRpc]
    public void OpenDiceUIClientRpc(int day, int night)
    {
        if (IsOwner)
        {
            bool isCaptain = player_index.Value == GameManager.instance.captain_player.Value;
            GameManager.instance.DiceUI.GetComponent<DiceUIScript>().OpenDiceDialog(day, night, isCaptain);
        }
    }

    public void UpdateActionCardsInHand()
    {
        ActionCard drawnCard = deck[0];
        deck.RemoveAt(0);

        switch (replace_action_card_number)
        {
            case 1:
                action_card_1 = drawnCard;
                break;
            case 2:
                action_card_2 = drawnCard;
                break;
            case 3:
                action_card_3 = drawnCard;
                break;
            case 4:
                action_card_4 = drawnCard;
                break;
        }

        ActionCardsUIScript ui = GameManager.instance.ActionCardUI.GetComponent<ActionCardsUIScript>();
        ui.UpdateActionCards(gameObject);
    }

    [Rpc(SendTo.Owner)]
    public void GetPlayedActionCardIDRpc()
    {
        ActionCard playedCard;

        if (replace_action_card_number == 1) playedCard = action_card_1;
        else if (replace_action_card_number == 2) playedCard = action_card_2;
        else if (replace_action_card_number == 3) playedCard = action_card_3;
        else playedCard = action_card_4;

        Debug.Log("Getting card id: " + playedCard.cardID + ", on index " + replace_action_card_number);
  
        GameManager.instance.currentPlayedCard.Value = new GameManager.ActionCardData { cardID = playedCard.cardID };
    }


    public void SetActionCards(GameManager.ActionCardOption o1, GameManager.ActionCardOption o2)
    {
        cardOption1 = o1;
        cardOption2 = o2;
    }

    [Rpc(SendTo.Owner)]
    public void SetupBattleUIClientRPC(int a, int d, bool a_has_ladybeth = false, bool d_has_ladybeth = false)
    {
        GameManager.instance.CombatUI.GetComponent<CombatUIScript>().SetPlayersInBattle(a, d, player_index.Value, a_has_ladybeth, d_has_ladybeth);
    }

    [Rpc(SendTo.Owner)]
    public void OpenOpponentChoiceClientRpc(int[] players)
    {
        GameManager.instance.CombatUI.GetComponent<CombatUIScript>().OpenOpponentChoice(players);
    }
}
