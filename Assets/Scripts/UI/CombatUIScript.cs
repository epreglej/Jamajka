using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using System.Threading.Tasks;
using System.Collections.Generic;

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

    [SerializeField] private GameObject ChooseHoldPanel;
    [SerializeField] private List<RectTransform> holdPanels = new List<RectTransform>();
    [SerializeField] private TextMeshProUGUI chooseHoldText;
    private PlayerGameScript winnerPlayer = null;
    private PlayerGameScript loserPlayer = null;
    private int chosenHoldIndex = -1;

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

        AttackerPanel.Find("CannonNumber").GetComponent<TextMeshProUGUI>().SetText("0 Cannon tokens");
        AttackerPanel.Find("LadyBeth").gameObject.SetActive(false);
        AttackerPanel.Find("Dice Result").GetComponent<TextMeshProUGUI>().SetText("Rolled dice: ");
        AttackerPanel.Find("TotalPoints").GetComponent<TextMeshProUGUI>().SetText("= 0 Attack points");

        DefenderPanel.Find("CannonNumber").GetComponent<TextMeshProUGUI>().SetText("0 Cannon tokens");
        DefenderPanel.Find("LadyBeth").gameObject.SetActive(false);
        DefenderPanel.Find("Dice Result").GetComponent<TextMeshProUGUI>().SetText("Rolled dice: ");
        DefenderPanel.Find("TotalPoints").GetComponent<TextMeshProUGUI>().SetText("= 0 Attack points");

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
            string child_name = "Opponent Choice " + i;

            Button choice_button = container.Find(child_name).GetComponent<Button>();

            foreach (var e in GameManager.instance.players) 
            {
                if(e.player_index.Value == player)
                {
                    choice_button.GetComponentInChildren<TMP_Text>().text = e.username;
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

        // TODO : setup attacker and defender name and image
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

        DefenderPanel.Find("CannonNumber").GetComponent<TextMeshProUGUI>().SetText("0 Cannon tokens");
        DefenderPanel.Find("LadyBeth").gameObject.SetActive(false);
        DefenderPanel.Find("Dice Result").GetComponent<TextMeshProUGUI>().SetText("Rolled dice: ");
        DefenderPanel.Find("TotalPoints").GetComponent<TextMeshProUGUI>().SetText("= 0 Attack points");

        TiePanel.SetActive(false);
        AttackerWinnerPanel.SetActive(false);
        DefenderWinnerPanel.SetActive(false);
    }

    public void DisplayVictoryChoice(int winner, int loser) {
        // TODO - DUJE: implement victory choice UI
        // for now, just default to steal from loser holds

        DisplayChooseHoldPanel(winner, loser);
    }

    private void DisplayChooseHoldPanel(int winner, int loser) {
        loserPlayer = GameManager.instance.players[loser];
        winnerPlayer = GameManager.instance.players[winner];
        //Debug.Log("LoserPlayer index: " + loserPlayer.player_index.Value + ", parameter: " + loser);
        List<PlayerGameScript.Hold> holds = loserPlayer.holds;

        for (int i = 0; i < 5; i++) {
            TextMeshProUGUI holdText = holdPanels[i].Find("HoldContentsText").GetComponent<TextMeshProUGUI>();
            PlayerGameScript.Hold hold = holds[i];
            holdText.text = hold.amount.ToString() + " " + hold.tokenType.ToString();
        }

        ChooseHoldPanel.SetActive(true);
    }

    public void ChooseHoldOnClick(int chosenHoldIndex) {
        if (this.chosenHoldIndex == -1) {
            this.chosenHoldIndex = chosenHoldIndex;
            chooseHoldText.text = "Place resources from hold " + chosenHoldIndex+1 + " into one of your holds";
            DisplayOwnHolds();
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
            GameManager.instance.OnWinnerChoiceCompleteRpc();
        }
    }

    private void DisplayOwnHolds() {
        List<PlayerGameScript.Hold> holds = winnerPlayer.holds;

        for (int i = 0; i < 5; i++) {
            TextMeshProUGUI holdText = holdPanels[i].Find("HoldContentsText").GetComponent<TextMeshProUGUI>();
            PlayerGameScript.Hold hold = holds[i];
            holdText.text = hold.amount.ToString() + " " + hold.tokenType.ToString();
        }
    }
}
