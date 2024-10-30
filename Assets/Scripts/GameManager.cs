using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Threading.Tasks;
using System.Threading;

public class GameManager : NetworkBehaviour
{
    const int STAR_COMBAT_VALUE = -1;

    public GameObject playerPrefab;

    public static GameManager instance { get; private set; }

    List<PlayerGameScript> players = new List<PlayerGameScript>();

    public NetworkVariable<int> captain_player = new NetworkVariable<int>(0);
    public NetworkVariable<int> player_on_turn = new NetworkVariable<int>(0);

    public NetworkVariable<int> day_dice_value = new NetworkVariable<int>();
    public NetworkVariable<int> night_dice_value = new NetworkVariable<int>();
    public NetworkVariable<int> combat_dice_value = new NetworkVariable<int>();

    public bool playerReachedEndSquare = false;

    private int players_called_ready = 0;
    private bool playerCalledTurnOver = false;
    private bool playerBattleActive = false;

    public Canvas DiceUI;
    public Canvas CombatUI;
    public Canvas ActionCardUI;

    public enum TreasureCard
    {
        Treasure, MorgansMap, SaransSaber, LadyBeth, AdditionHoldSpace
    }

    public enum TokenType
    {
        Food, Gold, Cannon, None
    }

    public enum ActionCardOption
    {
        MoveForward, MoveBackward, LoadGold, LoadFood, LoadCannon, None
    }

    public enum GameState
    {
        Start, ChooseCards, PlayerTurn, End
    }

    public enum SquareType
    {
        PirateLair, Sea, Port
    }

    public GameState state = GameState.Start;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log("Hello world");

        Debug.Log(IsHost);

        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }

        if (IsServer)
        {
            //setup player list
            //StartGame();
        }
    }

    void StartGame()
    {
        // load players
        if (players.Count < 2)
        {
            // handle error
            
        }
        // shuffle players if needed

        // distribute initial resources
        DistributeStartingResources();
    }

    void EndGame()
    {
        // calculate the points

        // display results
    }

    void DistributeStartingResources()
    {
        // give everyone food and gold
        foreach(PlayerGameScript player in players)
        {
            player.AddInitialResources();
        }

        // set special cards on pirate lairs (9 random card)

        player_on_turn = captain_player;

        // start some coroutine that calls:
        BoardSetupPhaseOver();
    }


    void BoardSetupPhaseOver()
    {
        StartGameCycle();
    }

    async void StartGameCycle()
    {
        //throw dice
        day_dice_value.Value = -1;
        night_dice_value.Value = -1;
        ThrowDice();

        while (day_dice_value.Value < 0 && night_dice_value.Value < 0)
        {
            await Task.Delay(1000);
        }

        //players draw cards
        state = GameState.ChooseCards;
        DrawCards();

        await WaitForPlayers();

        //when all players are ready
        state = GameState.PlayerTurn;
        for (int i = 0; i < players.Count; i++)
        {
            StartPlayerTurn((captain_player.Value + i) % players.Count);
            // wait for end player turn
            while (playerCalledTurnOver == false)
            {
                await Task.Delay(100);
            }
        }

        EndGameCycle();
    }

    void EndGameCycle()
    {
        captain_player.Value = (captain_player.Value + 1) % players.Count;

        player_on_turn = captain_player;

        if (playerReachedEndSquare)
        {
            EndGame();
            return;
        }

        StartGameCycle();
    }


    public async Task WaitForPlayers()
    {
        while (players_called_ready < players.Count) // or required player count
        {
            await Task.Delay(1000);
        }

        players_called_ready = 0;
    }

    [Rpc(SendTo.Server)]
    public void PlayerIsReadyServerRpc()
    {
        players_called_ready++;
    }

    [Rpc(SendTo.Server)]
    public void PlayerEndedTurnServerRpc(int player)
    {
        if(player == player_on_turn.Value)
        {
            playerCalledTurnOver = true;
        }
    }

    void ThrowDice()
    {
        // throw 2 dice
        int dice1 = Random.Range(1, 6);
        int dice2 = Random.Range(1, 6);
        
        // let the captain player choose which dice is day which is night
        foreach(PlayerGameScript player in players)
        {
            player.OpenDiceUIClientRpc(dice1, dice2);
        }
    }
    
    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void ThrowDiceServerRpc()
    {
        Debug.Log("Throw dice rpc called");
        ThrowDice();
    }

    public void OnDayNightDiceOrderChosen(int day, int night)
    {
        day_dice_value.Value = day;
        night_dice_value.Value = night;

        CloseDiceDialogClientRpc();
    }

    int ThrowCombatDice()
    {
        // returns 2,4,8,10 or star(-1)
        return 0;
    }

    void DrawCards()
    {
        // send an rpc to client and wait for confirmation they are done
        foreach(PlayerGameScript player in players)
        {
            player.ChooseACardClientRpc();
        }
    }

    void StartPlayerTurn(int player_index)
    {
        player_on_turn.Value = player_index;

        // read the player card

        // Player script should hold this
        // rpc to set these on player that is on turn
        ActionCardOption card_action1 = ActionCardOption.MoveForward;
        ActionCardOption card_action2 = ActionCardOption.LoadFood;

        ExecutePlayerAction();
    }

    [ServerRpc]
    public void PlayerAction1EndedServerRPC()
    {
        ExecutePlayerAction(false);
    }

    [ServerRpc]
    public void PlayerAction2EndedServerRPC()
    {
        EndPlayerTurn();
    }


    public void ExecutePlayerAction(bool useDayOption = true)
    {
        ActionCardOption cardOption = ActionCardOption.MoveForward; // this is in player script

        if (cardOption == ActionCardOption.MoveForward || cardOption == ActionCardOption.MoveBackward)
        {
            // move player 
            EndOfPlayerMovement();
        }
        else
        {
            // hand player their resources
            LoadResourceToPlayerHold(cardOption);
        }
    }

    async void EndOfPlayerMovement(bool mustPay = true)
    {
        if (CheckBattleConditions())
        {

            StartBattlePhase();
            while (playerBattleActive)
            {
                await Task.Delay(1000);
            }
        }

        if (GetPlayerSquareType() == SquareType.PirateLair)
        {
            // give player the special card if there
        }
        else if(mustPay)
        {
            TryTaxPlayer();
        }
    }

    void EndPlayerTurn()
    {
        // nothing for now, maybe usefull somehow
    }

    void LoadResourceToPlayerHold(ActionCardOption cardOption)
    {
        // make a player choose a hold to put resource into
    }

    bool CheckBattleConditions()
    {
        //if any players on the player square
        return true;
    }

    void StartBattlePhase()
    {
        // let player choose the opponent if multiple people on the square
        List<int> opponents = GetPlayersOnBattleSquare();

        int opponent = 0;

        StartSingleBattle(opponent);
        
    }

    void StartSingleBattle(int defender)
    {
        int attacker = player_on_turn.Value;

        // Attacker adds cannon token
        int attacker_tokens = 0; // replace with function call

        int attacker_dice = ThrowCombatDice();
        bool usedSaransSaber = false;

        if (AllowSaransSaber(attacker, defender))
        {
            attacker_dice = ThrowCombatDice();
            usedSaransSaber = true;
        }

        if(attacker_dice == STAR_COMBAT_VALUE) // replace with star value
        {
            ResolveBattleResult(attacker, defender);
            return;
        }

        int defender_tokens = 0; // replace with function call

        int defender_dice = ThrowCombatDice();

        if (!usedSaransSaber && AllowSaransSaber(attacker, defender))
        {
            defender_dice = ThrowCombatDice();
        }

        if (defender_dice == STAR_COMBAT_VALUE) // replace with star value
        {
            ResolveBattleResult(defender, attacker);
            return;
        }

        int attackerHasLadyBeth = 2;
        int defenderHasLadyBeth = 0;
        
        if (attacker_dice + attacker_tokens + attackerHasLadyBeth == defender_dice + defender_tokens + defenderHasLadyBeth)
        {
            // tie, nothing happens
        }
        else if (attacker_dice + attacker_tokens + attackerHasLadyBeth > defender_dice + defender_tokens + defenderHasLadyBeth)
        {
            //attacker won battle
            ResolveBattleResult(attacker, defender);
        }else
        {
            //defender won battle
            ResolveBattleResult(defender, attacker);
        }
    }

    bool AllowSaransSaber(int attacker, int defender)
    {
        bool attackerHasActionCard = false;
        bool defenderHasActionCard = false;

        if (attackerHasActionCard)
        {
            // Ask for reroll confiromation
            bool reroll = false;

            return reroll;
        }
        if (defenderHasActionCard)
        {
            // Ask for reroll confiromation
            bool reroll = false;

            return reroll;
        }

        return false;
    }

    void ResolveBattleResult(int winner, int loser)
    {
        // give the player the choice of win spoils

        /*
         * winner has a choice of options:
         *  1) take everything from one enemy loot hold
         *  2) take one action card
         *  3) give looser a cursed own action card  
         */

        playerBattleActive = false;
    }

    void TryTaxPlayer()
    {
        if (true) //player has the needed resources
        {
            //remove the player resources
        }
        else
        {
            // remove the amount of the resources that the player has

            int result = ThrowCombatDice();

            // move the player back based on dice result
            // 2 or 4 = move to port (coins) square
            // 6 or 8 = move to sea (food) square
            // 10 = move back to pirate lair
            // star = stay put

            if (result != STAR_COMBAT_VALUE) // TODO: replace with star dice value
            {
                // move the player back

                EndOfPlayerMovement(false);
            }
        }
    }

    SquareType GetPlayerSquareType()
    {
        return SquareType.PirateLair;
    }

    List<int> GetPlayersOnBattleSquare()
    {
        List<int> result = new List<int>();

        // get the square in question
        for(int i = 0; i < players.Count; i++)
        {
            if (i == player_on_turn.Value) continue;

            //if (players[i].IsOnSquare())
            //{
            //    result.Add(i);
            //}
        }

        return result;
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void AddPlayerServerRPC(ulong playerNetworkObjectId)
    {
        NetworkObject playerNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[playerNetworkObjectId];
        GameObject player = playerNetworkObject.gameObject;

        players.Add(player.GetComponent<PlayerGameScript>());

        player.GetComponent<PlayerGameScript>().player_index.Value = players.Count - 1;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SwitchDiceValuesClientRpc()
    {
        DiceUI.GetComponent<DiceUIScript>().SwitchValues();
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void CloseDiceDialogClientRpc()
    {
        DiceUI.GetComponent<DiceUIScript>().CloseDiceDialog();
    }
}
