using UnityEngine;
using Unity.Netcode;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Collections;


public class PlayerGameScript : NetworkBehaviour
{
    // public NetworkVariable<FixedString32Bytes> username = new NetworkVariable<FixedString32Bytes>();
    // public string username = string.Empty;
    public NetworkVariable<int> player_index = new NetworkVariable<int>(-1);
    public NetworkVariable<int> currentSquareID = new NetworkVariable<int>(0);

    // card options
    GameManager.ActionCardOption cardOption1 = GameManager.ActionCardOption.None;
    GameManager.ActionCardOption cardOption2 = GameManager.ActionCardOption.None;

    public List<Hold> holds = new List<Hold>();
    public List<ActionCard> deck = new List<ActionCard>();
    public List<ActionCard> usedCards = new List<ActionCard>();

    // action cards in hand
    private int replace_action_card_number = -1; // last used card (1, 2, 3 or 4)
    public ActionCard action_card_1 = null;
    public ActionCard action_card_2 = null;
    public ActionCard action_card_3 = null;
    public ActionCard action_card_4 = null; // only if morgans map


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
            GameManager.instance.AddPlayerServerRpc(playerNetworkObjectId);

            //string username = "Player" + Random.Range(1, 1000);
            GameManager.instance.AddPlayerUsernameServerRpc("Player" + Random.Range(1, 1000));

            SquareManager.instance.squares[0].AddPlayerIndexToSquareClientRpc(player_index.Value);
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

    [Rpc(SendTo.Owner)]
    public void InitializeDeckClientRpc()
    {
        // add action cards to deck
        ActionCard[] allCards = Resources.LoadAll<ActionCard>("ActionCards");

        string[] decks = { "AB", "ED", "JR", "MR", "OL", "SB"};

        List<ActionCard> myCards = new List<ActionCard>();
        string playerId = decks[player_index.Value];
        Debug.Log("Loading deck " + playerId + ", " + player_index.Value);

        foreach (var card in allCards)
        {
            if (card.cardID.StartsWith(playerId))
            {
                myCards.Add(card);
            }
        }

        deck = myCards;
        Shuffle(deck);

        UpdateActionCardsInHand();

        ActionCardsUIScript ui = GameManager.instance.ActionCardUI.GetComponent<ActionCardsUIScript>();
        ui.UpdateActionCards(gameObject);
    }

    // TODO - DUJE - ovak mozes dodavat i makivat Holdove
    public void AddInitialResources()
    {
        InitializeDeckClientRpc();
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

        if(replace_action_card_number >= 0) UpdateActionCardsInHand();

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

    [Rpc(SendTo.Owner)]
    public void OpenDiceUIClientRpc(int day, int night)
    {
        Debug.Log("Is captain ? " + player_index.Value + " " + GameManager.instance.captain_player.Value);

        bool isCaptain = player_index.Value == GameManager.instance.captain_player.Value;
        GameManager.instance.DiceUI.GetComponent<DiceUIScript>().OpenDiceDialog(day, night, isCaptain);
    }

    public void UpdateActionCardsInHand()
    {
        if (deck.Count == 0) ReshuffleDeck();

        if(action_card_1 == null)
        {
            action_card_1 = deck[0];
            action_card_2 = deck[1];
            action_card_3 = deck[2];

            usedCards.Add(deck[0]);
            usedCards.Add(deck[1]);
            usedCards.Add(deck[2]);

            deck.RemoveAt(2);
            deck.RemoveAt(1);
            deck.RemoveAt(0);
        }
        else
        {
            ActionCard drawnCard = deck[0];
            deck.RemoveAt(0);
            usedCards.Add(drawnCard);

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
        }

        ActionCardsUIScript ui = GameManager.instance.ActionCardUI.GetComponent<ActionCardsUIScript>();
        ui.UpdateActionCards(gameObject);
    }

    public void ReshuffleDeck()
    {
        deck.AddRange(usedCards);

        usedCards.Clear();

        Shuffle(deck);
    }


    private void Shuffle(List<ActionCard> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            ActionCard value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
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
        GameManager.ActionCardData data = new GameManager.ActionCardData { cardID = playedCard.cardID };

        GameManager.instance.SetPlayedActionCardServerRpc(data);
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
