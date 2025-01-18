using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Threading.Tasks;
using System.Threading;
using Unity.Collections;

public class GameManager : NetworkBehaviour
{
    const int STAR_COMBAT_VALUE = 1000;

    public GameObject playerPrefab;
    public MainMenuUIScript mainMenuUIScript;

    public static GameManager instance { get; private set; }

    public List<PlayerGameScript> players = new List<PlayerGameScript>();
    public NetworkList<FixedString32Bytes> usernames = new NetworkList<FixedString32Bytes>();

    public NetworkVariable<int> captain_player = new NetworkVariable<int>(0);
    public NetworkVariable<int> player_on_turn = new NetworkVariable<int>(0);

    public NetworkVariable<int> day_dice_value = new NetworkVariable<int>();
    public NetworkVariable<int> night_dice_value = new NetworkVariable<int>();
    public NetworkVariable<int> combat_dice_value = new NetworkVariable<int>();

    public NetworkVariable<int> attacker_combat_dice = new NetworkVariable<int>();
    public NetworkVariable<int> attacker_cannon_tokens = new NetworkVariable<int>();
    public NetworkVariable<int> defender_combat_dice = new NetworkVariable<int>();
    public NetworkVariable<int> defender_cannon_tokens = new NetworkVariable<int>();

    public bool playerReachedEndSquare = false;

    
    private int players_called_ready = 0;
    private bool playerCalledTurnOver = false;
    public bool playerBattleActive = false;

    // Ovi cekaju da playeri kliknu nest u UI
    private TaskCompletionSource<bool> saransSaberResponse;
    private TaskCompletionSource<int> opponentChoiceResponse;

    // UI prefabi
    public Canvas DiceUI;
    public Canvas CombatUI;
    public Canvas ActionCardUI;

    // Za rollanje Combat kocke - samo random index na listu
    public static List<int> COMBAT_DICE_VALUES = new List<int>{2, 4, 6, 8, STAR_COMBAT_VALUE};

    public enum TreasureCard
    {
        Plus7, Plus5, Plus3, Minus4, Minus3, Minus2, MorgansMap, SaransSaber, LadyBeth, AdditionHoldSpace, None
    }

    public enum TokenType
    {
        Food, Gold, Cannon, None
    }

    public enum ActionCardOption
    {
        MoveForward, MoveBackward, LoadGold, LoadFood, LoadCannon, None
    }

    // Od ovoga sam odustal od koristenja na pocetku tak da je useless, al za svaki slucaj ne brisat
    public enum GameState
    {
        Start, ChooseCards, PlayerTurn, End
    }

    public enum SquareType
    {
        PirateLair, Sea, Port, None
    }

    [System.Serializable]
    public struct ActionCardData : INetworkSerializable
    {
        public string cardID;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref cardID);
        }
    }

    public Dictionary<string, ActionCard> actionCardLookup = new Dictionary<string, ActionCard>();
    public List<TreasureCard> startingTreasureCards = new List<TreasureCard>();
    public NetworkVariable<ActionCardData> currentPlayedCard = new NetworkVariable<ActionCardData>();


    public GameState state = GameState.Start;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }

        Debug.Log("Hello world");
        Debug.Log(IsHost);
        Debug.Log("Conected clients: " + NetworkManager.Singleton.ConnectedClients.Count);
    }

    private void Start()
    {
        ActionCard[] allCards = Resources.LoadAll<ActionCard>("ActionCards");

        foreach (ActionCard card in allCards)
        {
            actionCardLookup[card.cardID] = card;
        }

        Debug.Log("Loaded " + allCards.Length + " action cards");

        startingTreasureCards.Add(TreasureCard.Plus7);
        startingTreasureCards.Add(TreasureCard.Plus7);
        startingTreasureCards.Add(TreasureCard.Plus5);
        startingTreasureCards.Add(TreasureCard.Plus3);
        startingTreasureCards.Add(TreasureCard.Minus4);
        startingTreasureCards.Add(TreasureCard.Minus3);
        startingTreasureCards.Add(TreasureCard.Minus2);
        startingTreasureCards.Add(TreasureCard.SaransSaber);
        startingTreasureCards.Add(TreasureCard.LadyBeth);
        startingTreasureCards.Add(TreasureCard.AdditionHoldSpace);
        startingTreasureCards.Add(TreasureCard.MorgansMap);
    }

    private void Update()
    {
        if (!IsServer) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space");
            StartGame();
        }
    }

    void StartGame()
    {
        // load players
        if (players.Count < 2)
        {
            // TODO - BOARD handle error - wrati na meni valjda
        }
        else
        {
            for(int i = 0; i < players.Count; i++)
            {
                PlayerGameScript player = players[i].GetComponent<PlayerGameScript>();
                player.SetupListOnClientRpc(i);
            }
        }

        // distribute initial resources
        DistributeStartingResources();
    }

    void EndGame()
    {
        // TODO - BOARD

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

        Shuffle(startingTreasureCards);

        int i = 0;
        foreach(Square sq in SquareManager.instance.squares)
        {
            if(sq.type == SquareType.PirateLair)
            {
                sq.SetTreasureCard(startingTreasureCards[i]);
                i++;
            }
        }

        player_on_turn.Value = captain_player.Value;

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
        await Task.Delay(100);
        ThrowDice();

        while (day_dice_value.Value < 0 && night_dice_value.Value < 0)
        {
            await Task.Delay(1000);
        }

        //players draw cards and choose
        state = GameState.ChooseCards;
        DrawCards();

        await WaitForPlayers();

        Debug.Log("All players chose their cards");

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
            playerCalledTurnOver = false;
        }

        EndGameCycle();
    }

    void EndGameCycle()
    {
        captain_player.Value = (captain_player.Value + 1) % players.Count;

        player_on_turn.Value = captain_player.Value;

        if (playerReachedEndSquare)
        {
            EndGame();
            return;
        }

        StartGameCycle();
    }

    async void StartPlayerTurn(int player_index)
    {
        player_on_turn.Value = player_index;

        currentPlayedCard.Value = new ActionCardData { cardID = "" };

        players[player_on_turn.Value].GetComponent<PlayerGameScript>().GetPlayedActionCardIDRpc();

        while (currentPlayedCard.Value.cardID == "")
        {
            await Task.Delay(100);
        }

        ExecutePlayerAction();
    }

    void EndPlayerTurn()
    {
        playerCalledTurnOver = true;
    }

    #region Dice
    void ThrowDice()
    {
        // throw 2 dice
        int dice1 = Random.Range(1, 7);
        int dice2 = Random.Range(1, 7);

        // let the captain player choose which dice is day which is night
        foreach (PlayerGameScript player in players)
        {
            player.OpenDiceUIClientRpc(dice1, dice2);
        }
    }

    public void OnDayNightDiceOrderChosen(int day, int night)
    {
        SetDayNightDiceServerRpc(day, night);
    }

    [Rpc(SendTo.Server)]
    public void SetDayNightDiceServerRpc(int day, int night)
    {
        day_dice_value.Value = day;
        night_dice_value.Value = night;

        CloseDiceDialogClientRpc();
    }

    async Task ThrowCombatDice(bool attacker = true)
    {
        if (attacker) attacker_combat_dice.Value = -1;
        else defender_combat_dice.Value = -1;

        if (attacker)
        {
            CombatUI.GetComponent<CombatUIScript>().RollAttackerDicePhaseClientRPC();
            while (attacker_combat_dice.Value < 0)
            {
                await Task.Delay(1000);
            }
        }
        else
        {
            CombatUI.GetComponent<CombatUIScript>().RollDefenderDicePhaseClientRPC();
            while (defender_combat_dice.Value < 0)
            {
                await Task.Delay(1000);
            }
        }
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

    #endregion

    #region Battle
    bool CheckBattleConditions()
    {
        int square_id = SquareManager.instance.GetPlayerSquareID(player_on_turn.Value);
        List<int> opponent_indices = SquareManager.instance.GetPlayerIndicesFromSquareWithId(square_id);
        if (opponent_indices == null || square_id == 0) return false;
        return opponent_indices.Count > 1;
    }

    async void StartBattlePhase()
    {
        // let player choose the opponent if multiple people on the square
        playerBattleActive = true;

        List<int> opponents = GetPlayersOnBattleSquare();

        players[player_on_turn.Value].OpenOpponentChoiceClientRpc(opponents.ToArray());

        int opponent = await WaitForOpponentChoiceResponse();

        StartSingleBattle(opponent);

    }
    void EndBattlePhase()
    {
        // remove the combat ui from the screen
        CloseBattleUIClientRpc();
    }

    List<int> GetPlayersOnBattleSquare()
    {
        List<int> result = new List<int>();
        
        // get the square in question
        int square_id = SquareManager.instance.GetPlayerSquareID(player_on_turn.Value);
        List<int> opponent_indices = SquareManager.instance.GetPlayerIndicesFromSquareWithId(square_id);

        foreach(int i in opponent_indices)
        {
            if (i == player_on_turn.Value) continue;

            result.Add(i);
        }

        return result;
    }


    private Task<int> WaitForOpponentChoiceResponse()
    {
        opponentChoiceResponse = new TaskCompletionSource<int>();
        return opponentChoiceResponse.Task;
    }

    [Rpc(SendTo.Server)]
    public void ReturnOpponentChoiceResultServerRpc(int player)
    {
        if (opponentChoiceResponse != null)
        {
            opponentChoiceResponse.TrySetResult(player);
        }
    }
    async void StartSingleBattle(int defender)
    {
        // initial setup
        int attacker = player_on_turn.Value;
        int attackerHasLadyBeth = players[attacker].hasLadyBeth ? 2 : 0;
        int defenderHasLadyBeth = players[defender].hasLadyBeth ? 2 : 0;

        attacker_cannon_tokens.Value = -1;
        defender_cannon_tokens.Value = -1;

        Debug.Log("Battle between " + attacker + " and " + defender);

        CombatUIScript UI_manager = CombatUI.GetComponent<CombatUIScript>();

        foreach (PlayerGameScript p in players)
        {
            p.SetupBattleUIClientRPC(attacker, defender, attackerHasLadyBeth > 0, defenderHasLadyBeth > 0);
        }

        // Attacker adds cannon token
        UI_manager.BeginAttackerCannonChoiceClientRPC();

        while (attacker_cannon_tokens.Value < 0)
        {
            await Task.Delay(1000);
        }

        Debug.Log("Attacker added tokens: " + attacker_cannon_tokens.Value);

        // Attacker throws dice
        Debug.Log("Attacker needs to throw dice");
        await ThrowCombatDice();

        Debug.Log("Attacker rolled: " + attacker_combat_dice.Value);

        bool usedSaransSaber = false;

        if (await AllowSaransSaber(attacker, defender))
        {
            await ThrowCombatDice();

            usedSaransSaber = true;
        }


        if (attacker_combat_dice.Value == STAR_COMBAT_VALUE)
        {
            ResolveBattleResult(attacker, defender);
            return;
        }

        int attacker_points = attacker_combat_dice.Value + attacker_cannon_tokens.Value + attackerHasLadyBeth;
        CombatUI.GetComponent<CombatUIScript>().UpdateTotalResultClientRpc(true, attacker_points);

        // Defender adds cannon token
        Debug.Log("Defender gets to add tokens");
        UI_manager.BeginDefenderCannonChoiceClientRPC();

        while (defender_cannon_tokens.Value < 0)
        {
            await Task.Delay(1000);
        }

        Debug.Log("Defender added tokens: " + defender_cannon_tokens.Value);


        // Defender throws dice
        await ThrowCombatDice(false);
        Debug.Log("Defender rolled: " + defender_combat_dice.Value);

        if (!usedSaransSaber && await AllowSaransSaber(attacker, defender))
        {
            await ThrowCombatDice(false);
        }

        if (defender_combat_dice.Value == STAR_COMBAT_VALUE) // replace with star value
        {
            ResolveBattleResult(defender, attacker);
            return;
        }

        int defender_points = defender_combat_dice.Value + defender_cannon_tokens.Value + defenderHasLadyBeth;
        CombatUI.GetComponent<CombatUIScript>().UpdateTotalResultClientRpc(false, defender_points);

        // Results of battle
        if (attacker_points == defender_points)
        {
            // tie, nothing happens
            ResolveBattleResult(-1, -1);
        }
        else if (attacker_points > defender_points)
        {
            //attacker won battle
            ResolveBattleResult(attacker, defender);
        }
        else
        {
            //defender won battle
            ResolveBattleResult(defender, attacker);
        }
    }

    async Task<bool> AllowSaransSaber(int attacker, int defender)
    {

        bool attackerHasActionCard = players[attacker].hasSaransSaber;
        bool defenderHasActionCard = players[defender].hasSaransSaber;

        if (attackerHasActionCard)
        {
            CombatUI.GetComponent<CombatUIScript>().EnableSarabsSaberClientRpc(true);

            return await WaitForSaransSaberResponse();
        }

        if (defenderHasActionCard)
        {
            CombatUI.GetComponent<CombatUIScript>().EnableSarabsSaberClientRpc(false);
            return await WaitForSaransSaberResponse();
        }

        return false;
    }

    private Task<bool> WaitForSaransSaberResponse()
    {
        saransSaberResponse = new TaskCompletionSource<bool>();
        return saransSaberResponse.Task;
    }

    [Rpc(SendTo.Server)]
    public void ReturnSaransSaberResultServerRpc(bool used)
    {
        if (saransSaberResponse != null)
        {
            saransSaberResponse.TrySetResult(used);
        }
    }

    async void ResolveBattleResult(int winner, int loser)
    {
        // give the player the choice of win spoils
        if (winner == -1)
        {
            // its a tie
            CombatUI.GetComponent<CombatUIScript>().DisplayWinnerClientRpc(0);
        }
        else
        {
            CombatUI.GetComponent<CombatUIScript>().DisplayWinnerClientRpc(winner == player_on_turn.Value ? 1 : -1);

            /*
             * winner has a choice of options:
             *  1) take everything from one enemy loot hold
             *  2) take one action card
             *  3) give looser a cursed own action card -- ignore  
             */


        }

        await Task.Delay(5000);

        playerBattleActive = false;
    }


    #endregion
    
    #region BattleUI
    [Rpc(SendTo.Server)]
    public void SetAttackerCannonTokensServerRpc(int ammount)
    {
        attacker_cannon_tokens.Value = ammount;
    }

    [Rpc(SendTo.Server)]
    public void SetDefenderCannonTokensServerRpc(int ammount)
    {
        defender_cannon_tokens.Value = ammount;
    }

    [Rpc(SendTo.Server)]
    public void SetAttackerDiceServerRpc(int ammount)
    {
        attacker_combat_dice.Value = ammount;
    }

    [Rpc(SendTo.Server)]
    public void SetDefenderDiceServerRpc(int ammount)
    {
        defender_combat_dice.Value = ammount;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void CloseBattleUIClientRpc()
    {
        CombatUI.GetComponent<CombatUIScript>().OnCombatEnd();
    }
    #endregion

    #region ActionCards
    void DrawCards()
    {
        // send an rpc to client and wait for confirmation they are done
        foreach (PlayerGameScript player in players)
        {
            player.ChooseACardClientRpc();
        }
    }

    [Rpc(SendTo.Server)]
    public void SetPlayedActionCardServerRpc(ActionCardData playedCardData)
    {
        ActionCard playedCard = actionCardLookup[playedCardData.cardID];

        currentPlayedCard.Value = playedCardData;
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


    async public void ExecutePlayerAction(bool useDayOption = true)
    {
        // read the player card
        Debug.Log(currentPlayedCard.Value.cardID);

        ActionCard playedCard = actionCardLookup[currentPlayedCard.Value.cardID];

        // Player script should hold this
        // rpc to set these on player that is on turn
        ActionCardOption card_action1 = playedCard.leftOption;
        ActionCardOption card_action2 = playedCard.rightOption;

        ActionCardOption cardOption = useDayOption ? card_action1 : card_action2;
        int ammount = useDayOption ? day_dice_value.Value : night_dice_value.Value;

        if (cardOption == ActionCardOption.MoveForward || cardOption == ActionCardOption.MoveBackward)
        {
            if (cardOption == ActionCardOption.MoveBackward) ammount *= -1;
            PlayerMovement movementComponent = players[player_on_turn.Value].GetComponent<PlayerMovement>();
            Debug.Log("Move for " + ammount);
            movementComponent.MoveXSquares(ammount);

            while (movementComponent.isMoving) await Task.Delay(1500);

            EndOfPlayerMovement(true, useDayOption);
        }
        else
        {
            // hand player their resources
            Debug.Log("Adding resources to player");
            LoadResourceToPlayerHold(cardOption, useDayOption);
        }
    }
    #endregion

    async void EndOfPlayerMovement(bool mustPay = true, bool dayAction = true)
    {
        Debug.Log("End of player movement");

        if (CheckBattleConditions())
        {
            Debug.Log("Entering battle phase");
            StartBattlePhase();
            while (playerBattleActive)
            {
                await Task.Delay(1000);
            }
            EndBattlePhase();
            Debug.Log("End Of Battle");
            await Task.Delay(1000);
        }else
        {
            Debug.Log("No battle");
        }
        SquareType player_square_type = SquareManager.instance.GetPlayerSquareType(player_on_turn.Value);
        PlayerGameScript player_gamescript = players[player_on_turn.Value].GetComponent<PlayerGameScript>();
        if (!mustPay || player_square_type == SquareType.PirateLair)
        {
            if(player_square_type == SquareType.PirateLair)
            {
                Square playerSq = SquareManager.instance.GetPlayerSquare(player_on_turn.Value);
                TreasureCard tr_card = playerSq.GetTreasureCard();
                if(tr_card != TreasureCard.None)
                {
                    if(tr_card == TreasureCard.MorgansMap)
                    {
                        player_gamescript.GetTreasureMorgansMapRpc();
                    }
                    else if(tr_card == TreasureCard.LadyBeth)
                    {
                        player_gamescript.GetTreasureLadyBethRpc();
                    }
                    else if(tr_card == TreasureCard.AdditionHoldSpace)
                    {
                        player_gamescript.GetTreasureAdditionalHoldRpc();
                    }
                    else if (tr_card == TreasureCard.SaransSaber)
                    {
                        player_gamescript.GetTreasureSaransSaberRpc();
                    }
                    else
                    {
                        player_gamescript.GetTreasurePointsRpc(((int)tr_card));
                    }
                }
            }
            
            if (dayAction) PlayerAction1EndedServerRPC();
            else PlayerAction2EndedServerRPC();
        }
        else
        {
            TryTaxPlayer(dayAction);
        }
    }

    void LoadResourceToPlayerHold(ActionCardOption cardOption, bool dayAction = true)
    {
        // TODO - DUJE make a player choose a hold to put resource into


        if (dayAction) PlayerAction1EndedServerRPC();
        else PlayerAction2EndedServerRPC();
    }

    async void TryTaxPlayer(bool dayAction)
    {
        if (true) //TODO - DUJE player has the needed resources
        {
            //TODO - DUJE remove the player resources
        }
        else
        {
            // TODO - DUJE remove the amount of the resources that the player has

            await ThrowCombatDice();

            // move the player back based on dice result
            // 2 or 4 = move to port (coins) square
            // 6 or 8 = move to sea (food) square
            // 10 = move back to pirate lair
            // star = stay put
            int square = SquareManager.instance.GetPlayerSquareID(player_on_turn.Value);
            int move_ammount = 0;

            if (attacker_combat_dice.Value == 2 || attacker_combat_dice.Value == 4) move_ammount = -1 * SquareManager.instance.FindPreviousSquareType(square, SquareType.Port);
            else if (attacker_combat_dice.Value == 6 || attacker_combat_dice.Value == 8) move_ammount = -1 * SquareManager.instance.FindPreviousSquareType(square, SquareType.Sea);
            else if (attacker_combat_dice.Value == 10) move_ammount = -1 * SquareManager.instance.FindPreviousSquareType(square, SquareType.PirateLair);

            if (attacker_combat_dice.Value != STAR_COMBAT_VALUE)
            {
                PlayerMovement movementComponent = players[player_on_turn.Value].GetComponent<PlayerMovement>();
                movementComponent.MoveXSquares(move_ammount);

                while (movementComponent.isMoving) await Task.Delay(1000);

                EndOfPlayerMovement(false, dayAction);
            }
        }

        if (dayAction) PlayerAction1EndedServerRPC();
        else PlayerAction2EndedServerRPC();
    }


    #region Util
    // im not sure this works, but if you need it xd ( better to just call all player scripts in loop and check if owner )
    public PlayerGameScript GetOwnerPlayerScript(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            var playerObject = client.PlayerObject;
            if (playerObject != null)
            {
                return playerObject.GetComponent<PlayerGameScript>();
            }
        }
        return null;
    }


    [Rpc(SendTo.Server)]
    public void AddPlayerServerRpc(ulong playerNetworkObjectId)
    {
        NetworkObject playerNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[playerNetworkObjectId];
        PlayerGameScript player = playerNetworkObject.gameObject.GetComponent<PlayerGameScript>();

        players.Add(player);
        // usernames.Add(player.username);

        player.player_index.Value = players.Count - 1;
    }


    [Rpc(SendTo.Server)]
    public void AddPlayerUsernameServerRpc(string username)
    {
        usernames.Add(username);
    }


    [ClientRpc]
    public void UpdatePlayerListUIClientRpc()
    {
        mainMenuUIScript.UpdatePlayerListUI();
    }

    public async Task WaitForPlayers()
    {
        while (players_called_ready < players.Count)
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

    private void Shuffle(List<TreasureCard> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            TreasureCard value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    #endregion


}
