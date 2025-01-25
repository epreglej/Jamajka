using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class CombatUIScript : NetworkBehaviour
{
    [SerializeField] RectTransform OpponentChoicePanel;


    [SerializeField] RectTransform CombatPanel;
    [SerializeField] RectTransform AttackerPanel;
    [SerializeField] RectTransform DefenderPanel;

    [SerializeField] RectTransform CannonTokenPanel;
    [SerializeField] TextMeshProUGUI cannon_tokens_number_text;


    [SerializeField] RectTransform CombatDicePanel;

    [SerializeField] RectTransform SaransSaberPanel;

    [SerializeField] GameObject AttackerWinnerPanel;
    [SerializeField] GameObject DefenderWinnerPanel;
    [SerializeField] GameObject TiePanel;

    // Victory choice UI variables, used for stealing resources and treasure cards
    [SerializeField] private GameObject ChooseHoldPanel;
    [SerializeField] private List<RectTransform> holdPanels = new List<RectTransform>();
    [SerializeField] private TextMeshProUGUI chooseHoldText;
    [SerializeField] private GameObject VictoryChoicePanel;
    [SerializeField] private GameObject ChooseTreasureCardPanel;
    [SerializeField] private Button _backButton;
    private PlayerGameScript winnerPlayer = null;
    private PlayerGameScript loserPlayer = null;
    private int chosenHoldIndex = -1;

    // Resource loading UI variables
    private int _loadResourceAmount = -1;
    private GameManager.TokenType _loadResourceType = GameManager.TokenType.None;
    private bool _loadingResources = false;
    private bool _dayAction = false;

    bool attackerTurn = true;
    bool isAttacker = false;
    bool isDefender = false;

    public PlayerGameScript attacker;
    public PlayerGameScript defender;
    
    int attackTokens = 0;

    private void Start()
    {
        CombatPanel.gameObject.SetActive(false);
        OpponentChoicePanel.gameObject.SetActive(false);
        ChooseHoldPanel.SetActive(false);
        VictoryChoicePanel.SetActive(false);
        ChooseTreasureCardPanel.SetActive(false);

        AttackerPanel.Find("CannonNumber").GetComponent<TextMeshProUGUI>().SetText("0 Cannon tokens");
        AttackerPanel.Find("LadyBeth").gameObject.SetActive(false);
        AttackerPanel.Find("Dice Result").GetComponent<TextMeshProUGUI>().SetText("Rolled dice: ");
        AttackerPanel.Find("TotalPoints").GetComponent<TextMeshProUGUI>().SetText("= 0 Attack points");
        AttackerPanel.Find("PlayerName").GetComponent<TextMeshProUGUI>().SetText("");

        DefenderPanel.Find("CannonNumber").GetComponent<TextMeshProUGUI>().SetText("0 Cannon tokens");
        DefenderPanel.Find("LadyBeth").gameObject.SetActive(false);
        DefenderPanel.Find("Dice Result").GetComponent<TextMeshProUGUI>().SetText("Rolled dice: ");
        DefenderPanel.Find("TotalPoints").GetComponent<TextMeshProUGUI>().SetText("= 0 Attack points");
        DefenderPanel.Find("PlayerName").GetComponent<TextMeshProUGUI>().SetText("");

        TiePanel.SetActive(false);
        AttackerWinnerPanel.SetActive(false);
        DefenderWinnerPanel.SetActive(false);
    }

    public void OpenOpponentChoice(int[] players)
    {
        OpponentChoicePanel.gameObject.SetActive(true);
        Transform container = OpponentChoicePanel.Find("OpponentContainer");

        foreach(Component component in container.GetComponentsInChildren<Button>())
        {
            component.gameObject.SetActive(false);
        }

        int i = 1;
        
        foreach (int player in players)
        {
            Debug.LogWarning(player);
            string child_name = "Opponent Choice " + i;

            Button choice_button = container.Find(child_name).GetComponent<Button>();

            /*
            foreach (var e in GameManager.instance.players) 
            {
                Debug.Log(e.player_index.Value + " " + e.username.Value.ToString());
                if(e.player_index.Value == player)
                {
                    choice_button.GetComponentInChildren<TMP_Text>().text = e.username.Value.ToString();
                }
            }
            */

            // Novo rjesenje vuce usernameove iz GameManager.instance.usernames (imaju isti index ko player indexi)
            for(int j = 0; j < players.Count() + 1; j++)
            {
                if (GameManager.instance.players[j].player_index.Value == player)
                {
                    choice_button.GetComponentInChildren<TMP_Text>().text = GameManager.instance.usernames[j].Value.ToString();
                }
            }

            choice_button.gameObject.SetActive(true);
            choice_button.onClick.RemoveAllListeners();
            choice_button.onClick.AddListener(() => { OpponentChoicePressed(player); });
            i++;
        }
        
    }

    public void OpponentChoicePressed(int player)
    {
        GameManager.instance.ReturnOpponentChoiceResultServerRpc(player);
        OpponentChoicePanel.gameObject.SetActive(false);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void BeginAttackerCannonChoiceClientRPC()
    {
        attackerTurn = true;
        if (isAttacker)
        {
            Debug.Log("Attacker cannon choice (UI manager)");
            CannonTokenPanel.gameObject.SetActive(true);
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void BeginDefenderCannonChoiceClientRPC()
    {
        attackerTurn = false;

        if (isDefender)
        {
            Debug.Log("Defender cannon choice (UI manager)");
            CannonTokenPanel.gameObject.SetActive(true);
        }

    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void RollAttackerDicePhaseClientRPC()
    {
        if (isAttacker)
        {
            Debug.Log("Attacker dice roll (UI manager)");
            CombatDicePanel.gameObject.SetActive(true);
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void RollDefenderDicePhaseClientRPC()
    {
        if (isDefender)
        {
            Debug.Log("Defender dice roll (UI manager)");
            CombatDicePanel.gameObject.SetActive(true);
        }
    }

    public void AddAttackToken()
    {
        // TODO : add max check for resources
        attackTokens += 1;

        cannon_tokens_number_text.SetText(attackTokens.ToString());

    }

    public void RemoveAttackToken()
    {
        attackTokens = Mathf.Max(attackTokens - 1, 0);

        cannon_tokens_number_text.SetText(attackTokens.ToString());
    }

    public void ConfirmAttackTokenChoice()
    {
        if(isAttacker || isDefender)
        {
            CannonTokenPanel.gameObject.SetActive(false);
        }

        if (attackerTurn)
        {
            GameManager.instance.SetAttackerCannonTokensServerRpc(attackTokens);
            UpdateCannonTokensClientRpc(true, attackTokens);
        }
        else
        {
            GameManager.instance.SetDefenderCannonTokensServerRpc(attackTokens);
            UpdateCannonTokensClientRpc(false, attackTokens);
        }

        attackTokens = 0;
        cannon_tokens_number_text.SetText(attackTokens.ToString());
    }

    public void OnRollDicePressed()
    {
        if (isAttacker || isDefender)
        {
            CombatDicePanel.gameObject.SetActive(false);
        }

        int dice_value = GameManager.COMBAT_DICE_VALUES[Random.Range(0, 5)];

        if (attackerTurn)
        {
            GameManager.instance.SetAttackerDiceServerRpc(dice_value);
        }
        else
        {
            GameManager.instance.SetDefenderDiceServerRpc(dice_value);
        }

        UpdateDiceResultClientRpc(attackerTurn, dice_value);
    }

    public void SetPlayersInBattle(int a, int d, int caller, bool attacker_has_ladybeth, bool defender_has_ladybeth)
    {
        Debug.Log(a + " " + d + " " + caller);
        attacker = GameManager.instance.players[a];
        defender = GameManager.instance.players[d];

        CannonTokenPanel.gameObject.SetActive(false);
        CombatDicePanel.gameObject.SetActive(false);

        if (attacker_has_ladybeth) AttackerPanel.Find("LadyBeth").gameObject.SetActive(true);
        if (defender_has_ladybeth) DefenderPanel.Find("LadyBeth").gameObject.SetActive(true);

        if(attacker.player_index.Value == caller)
        {
            isAttacker = true;
            Debug.Log("I am attacker");
        }
        if(defender.player_index.Value == caller)
        {
            isDefender = true;
            Debug.Log("I am defender");
        }

        foreach (var player in GameManager.instance.usernames) { Debug.Log(player.Value.ToString()); }
        AttackerPanel.Find("PlayerName").GetComponent<TextMeshProUGUI>().SetText(GameManager.instance.usernames[a].Value.ToString());
        DefenderPanel.Find("PlayerName").GetComponent<TextMeshProUGUI>().SetText(GameManager.instance.usernames[d].Value.ToString());
        //AttackerPanel.Find("PlayerName").GetComponent<TextMeshProUGUI>().SetText(GameManager.instance.players[a].username.Value.ToString());
        //DefenderPanel.Find("PlayerName").GetComponent<TextMeshProUGUI>().SetText(GameManager.instance.players[d].username.Value.ToString());

        string[] pirates = { "AB", "ED", "JR", "MR", "OL", "SB" };

        AttackerPanel.Find("Image").GetComponent<Image>().sprite = Resources.Load<Sprite>("Sprites/PiratePicture/" + pirates[a]);
        DefenderPanel.Find("Image").GetComponent<Image>().sprite = Resources.Load<Sprite>("Sprites/PiratePicture/" + pirates[d]);

        CombatPanel.gameObject.SetActive(true);
    }

    [Rpc(SendTo.Everyone)]
    public void EnableSarabsSaberClientRpc(bool attacker)
    {
        if((attacker && isAttacker) || (!attacker && isDefender))
            SaransSaberPanel.gameObject.SetActive(true);
    }

    public void OnConfirm_SaransSaber()
    {
        SaransSaberPanel.gameObject.SetActive(false);

        GameManager.instance.ReturnSaransSaberResultServerRpc(true);
    }

    public void OnDecline_SaransSaber()
    {
        SaransSaberPanel.gameObject.SetActive(false);

        GameManager.instance.ReturnSaransSaberResultServerRpc(false);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void UpdateCannonTokensClientRpc(bool forAttacker, int ammount)
    {
        RectTransform panel;
        if (forAttacker) panel = AttackerPanel;
        else panel = DefenderPanel;

        panel.Find("CannonNumber").GetComponent<TextMeshProUGUI>().SetText(ammount + " Cannon tokens");
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void UpdateDiceResultClientRpc(bool forAttacker, int ammount)
    {
        RectTransform panel;
        if (forAttacker) panel = AttackerPanel;
        else panel = DefenderPanel;

        if(ammount <= 8) panel.Find("Dice Result").GetComponent<TextMeshProUGUI>().SetText("Rolled dice: " + ammount);
        else panel.Find("Dice Result").GetComponent<TextMeshProUGUI>().SetText("Rolled dice: STAR");
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void UpdateTotalResultClientRpc(bool forAttacker, int ammount)
    {
        RectTransform panel;
        if (forAttacker) panel = AttackerPanel;
        else panel = DefenderPanel;

        panel.Find("TotalPoints").GetComponent<TextMeshProUGUI>().SetText("= " + ammount + " Attack points");
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void DisplayWinnerClientRpc(int result)
    {
        if(result == 0)
        {
            // Tie
            TiePanel.SetActive(true);

        }else if(result > 0)
        {
            // attacker won
            AttackerWinnerPanel.SetActive(true);
        }
        else
        {
            // defender won
            DefenderWinnerPanel.SetActive(true);
        }
    }

    public void OnCombatEnd()
    {
        CombatPanel.gameObject.SetActive(false);

        AttackerPanel.Find("CannonNumber").GetComponent<TextMeshProUGUI>().SetText("0 Cannon tokens");
        AttackerPanel.Find("LadyBeth").gameObject.SetActive(false);
        AttackerPanel.Find("Dice Result").GetComponent<TextMeshProUGUI>().SetText("Rolled dice: ");
        AttackerPanel.Find("TotalPoints").GetComponent<TextMeshProUGUI>().SetText("= 0 Attack points");
        AttackerPanel.Find("PlayerName").GetComponent<TextMeshProUGUI>().SetText("");

        DefenderPanel.Find("CannonNumber").GetComponent<TextMeshProUGUI>().SetText("0 Cannon tokens");
        DefenderPanel.Find("LadyBeth").gameObject.SetActive(false);
        DefenderPanel.Find("Dice Result").GetComponent<TextMeshProUGUI>().SetText("Rolled dice: ");
        DefenderPanel.Find("TotalPoints").GetComponent<TextMeshProUGUI>().SetText("= 0 Attack points");
        DefenderPanel.Find("PlayerName").GetComponent<TextMeshProUGUI>().SetText("");

        TiePanel.SetActive(false);
        AttackerWinnerPanel.SetActive(false);
        DefenderWinnerPanel.SetActive(false);
        
        isDefender = false;
        isAttacker = false;
    }

    public void DisplayVictoryChoice(int winner, int loser) {
        winnerPlayer = GameManager.instance.players[winner];
        loserPlayer = GameManager.instance.players[loser];

        VictoryChoicePanel.SetActive(true);
        GameManager.instance.HoldUI.GetComponent<Canvas>().sortingOrder = 1;
    }

    private void DisplayChooseHoldPanel(int winner, int loser) {
        loserPlayer = GameManager.instance.players[loser];
        winnerPlayer = GameManager.instance.players[winner];
        chooseHoldText.text = "Choose a Hold to Steal from";
        //Debug.Log("LoserPlayer index: " + loserPlayer.player_index.Value + ", parameter: " + loser);
        List<PlayerGameScript.Hold> holds = loserPlayer.holds;

        DisplayHolds(holds);

        ChooseHoldPanel.SetActive(true);
        VictoryChoicePanel.SetActive(false);
    }

    public void ChooseHoldOnClick(int chosenHoldIndex) {
        if (_loadingResources) {
            OnButtonLoadResourceIntoHold(chosenHoldIndex);
        }
        else if (this.chosenHoldIndex == -1) {
            this.chosenHoldIndex = chosenHoldIndex;
            chooseHoldText.text = "Place resources from hold " + (chosenHoldIndex + 1) + " into one of your holds";
            DisplayOwnHolds();
            GameManager.instance.HoldUI.GetComponent<Canvas>().sortingOrder = 0;
        } else {
            // swap resources
            PlayerGameScript.Hold winnerHold = winnerPlayer.holds[chosenHoldIndex];
            PlayerGameScript.Hold loserHold = loserPlayer.holds[this.chosenHoldIndex];

            // TODO - DUJE: add logic for checking if the swap is valid (same resource type etc.)
            winnerHold.amount = loserHold.amount;
            winnerHold.tokenType = loserHold.tokenType;
            loserHold.amount = 0;
            loserHold.tokenType = GameManager.TokenType.None;

            winnerPlayer.holds[chosenHoldIndex] = winnerHold;
            loserPlayer.holds[this.chosenHoldIndex] = loserHold;

            GameManager.instance.UpdatePlayerHoldsServerRpc(winnerPlayer.player_index.Value, winnerHold.tokenType, winnerHold.amount, chosenHoldIndex);
            GameManager.instance.UpdatePlayerHoldsServerRpc(loserPlayer.player_index.Value, loserHold.tokenType, loserHold.amount, this.chosenHoldIndex);

            // reset state
            ChooseHoldPanel.SetActive(false);
            chooseHoldText.text = "Choose a Hold to Steal from";
            this.chosenHoldIndex = -1;
            winnerPlayer = null;
            loserPlayer = null;
            GameManager.instance.HoldUI.GetComponent<Canvas>().sortingOrder = 0;

            GameManager.instance.OnWinnerChoiceCompleteRpc();
        }
    }

    public void OnButtonStealResources() {
        if (winnerPlayer == null || loserPlayer == null) {
            Debug.LogError("Winner or loser player not set");
            return;
        }
        DisplayChooseHoldPanel(winnerPlayer.player_index.Value, loserPlayer.player_index.Value);
    }

    public void OnButtonStealTreasureCard() {
        if (winnerPlayer == null || loserPlayer == null) {
            Debug.LogError("Winner or loser player not set");
            return;
        }

        VictoryChoicePanel.SetActive(false);


        // TODO - DUJE: replace StealTreasureCardsRpc with just a check of which cards the loser has
        GameManager.instance.players[loserPlayer.player_index.Value].StealTreasureCardsRpc(winnerPlayer.player_index.Value);

        // NOTE - DUJE: below should be called at the end of the victory choice (rpc) chain
        //GameManager.instance.OnWinnerChoiceCompleteRpc();
    }

    private void DisplayOwnHolds() {
        List<PlayerGameScript.Hold> holds = winnerPlayer.holds;
        DisplayHolds(holds);

        bool allHoldsFull = holds.TrueForAll(hold => hold.amount > 0);
        for (int i = 0; i < 5; i++) {
            Button holdButton = holdPanels[i].GetComponentInChildren<Button>();
            holdButton.interactable = allHoldsFull ? holds[i].tokenType != loserPlayer.holds[chosenHoldIndex].tokenType : holds[i].amount == 0;
        }
    }

    public void DisplayStealTreasureCardPanel(bool[] treasureCards) {   
        ChooseTreasureCardPanel.SetActive(true);
        VictoryChoicePanel.SetActive(false);

        string debugCards = "Again, the treasure cards are (which buttons should be active): ";
        for (int i = 0; i < 4; i++) {
            debugCards += treasureCards[i] + " ";
        }

        for (int i = 0; i < 4; i++) {
            Button button = ChooseTreasureCardPanel.transform.GetChild(i+1).GetComponent<Button>();
            button.interactable = treasureCards[i];
        }
    }

    public void OnButtonChooseTreasureCard(int cardIndex) {
        // 1 - Morgan's Map
        // 2 - Saran's Saber
        // 3 - Lady Beth
        // 4 - 6th Hold
        // TODO - DUJE: probably replace with enum

        if (winnerPlayer == null || loserPlayer == null) {
            Debug.LogError("Winner or loser player not set");
            return;
        }

        GameManager.instance.StealTreasureCardServerRpc(winnerPlayer.player_index.Value, loserPlayer.player_index.Value, cardIndex);
        ChooseTreasureCardPanel.SetActive(false);
        winnerPlayer = null;
        loserPlayer = null;
        GameManager.instance.HoldUI.GetComponent<Canvas>().sortingOrder = 0;
        GameManager.instance.OnWinnerChoiceCompleteRpc();
    }

    public void OnButtonBackToVictoryChoice() {
        chosenHoldIndex = -1;
        ChooseTreasureCardPanel.SetActive(false);
        ChooseHoldPanel.SetActive(false);
        VictoryChoicePanel.SetActive(true);
        GameManager.instance.HoldUI.GetComponent<Canvas>().sortingOrder = 1;
    }

    private void DisplayHolds(List<PlayerGameScript.Hold> holds) {
        for (int i = 0; i < 5; i++) {
            TextMeshProUGUI holdText = holdPanels[i].Find("HoldContentsText").GetComponent<TextMeshProUGUI>();
            PlayerGameScript.Hold hold = holds[i];
            holdText.text = hold.amount.ToString() + " " + hold.tokenType.ToString();
            Image holdImage = holdPanels[i].Find("HoldContentsImage").GetComponent<Image>();
            holdImage.color = hold.amount > 0 ? Color.white : Color.clear;
            holdImage.sprite = GameManager.instance.HoldUI.GetSprite(hold.tokenType);

            Button holdButton = holdPanels[i].GetComponentInChildren<Button>();
            holdButton.interactable = hold.amount > 0;
        }
    }

    // used for loading resources into holds, not stealing
    public void DisplayHoldLoadingPanel(List<PlayerGameScript.Hold> holds, GameManager.TokenType tokenType, 
                                        int amount, PlayerGameScript winnerPlayer, bool dayAction) {
        Debug.Log("Hello from display hold loading panel");
        ChooseHoldPanel.SetActive(true);
        DisplayHolds(holds);

        chooseHoldText.text = "Place " + amount + " " + tokenType.ToString() + " into one of your holds";
        _loadResourceAmount = amount;
        _loadResourceType = tokenType;
        this.winnerPlayer = winnerPlayer; // reusing the winner player variable for loading resources
        _loadingResources = true;
        _dayAction = dayAction;
        _backButton.interactable = false;

        if (holds.TrueForAll(hold => hold.tokenType == tokenType)) {
            Debug.Log("All holds are full of the same resource type, skipping loading");
            // reset choose hold panel state
            chooseHoldText.text = "Choose a Hold to Steal from";
            _loadResourceAmount = -1;
            _loadResourceType = GameManager.TokenType.None;
            winnerPlayer = null;
            _loadingResources = false;
            _backButton.interactable = true;
            ChooseHoldPanel.SetActive(false);
            if (_dayAction) {
                GameManager.instance.PlayerAction1EndedServerRPC();
            } else {
                GameManager.instance.PlayerAction2EndedServerRPC();
            }
        }

        bool allHoldsFull = holds.TrueForAll(hold => hold.amount > 0);

        for (int i = 0; i < 5; i++) {
            Button holdButton = holdPanels[i].GetComponentInChildren<Button>();
            holdButton.interactable = allHoldsFull ? holds[i].tokenType != tokenType : holds[i].amount == 0;
        }
    }

    public void OnButtonLoadResourceIntoHold(int holdIndex) {
        Debug.Log("Loading " + _loadResourceAmount + " " + _loadResourceType + " into hold " + holdIndex);
        GameManager.instance.UpdatePlayerHoldsServerRpc(winnerPlayer.player_index.Value, _loadResourceType, _loadResourceAmount, holdIndex);
        
        // reset choose hold panel state
        chooseHoldText.text = "Choose a Hold to Steal from";
        _loadResourceAmount = -1;
        _loadResourceType = GameManager.TokenType.None;
        winnerPlayer = null;
        _loadingResources = false;
        _backButton.interactable = true;
        ChooseHoldPanel.SetActive(false);

        if (_dayAction) {
            GameManager.instance.PlayerAction1EndedServerRPC();
        } else {
            GameManager.instance.PlayerAction2EndedServerRPC();
        }
    }
}
