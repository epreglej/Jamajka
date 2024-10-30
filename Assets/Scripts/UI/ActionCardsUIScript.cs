using UnityEngine;
using UnityEngine.UI;
public class ActionCardsUIScript : MonoBehaviour
{
    public bool hasMorgansMap = false;

    // sidebar
    [SerializeField] Button actionCardSidebarButton;
    [SerializeField] GameObject actionCardSidebar;

    [SerializeField] Image actionCardSidebar_card1;
    [SerializeField] Image actionCardSidebar_card2;
    [SerializeField] Image actionCardSidebar_card3;

    [SerializeField] Image actionCardSidebar_card4; // Morgans Map


    // Choice ui
    [SerializeField] GameObject actionCard_ChoicePanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        actionCardSidebar.SetActive(false);
        actionCardSidebar_card4.gameObject.SetActive(false);

        actionCard_ChoicePanel.SetActive(false);
    }

    public void OnHoverCardButton()
    {
        actionCardSidebar.SetActive(true);
    }

    public void CloseCardsPreview()
    {
        actionCardSidebar.SetActive(false);
    }

    public void AquiredMorgansMap()
    {
        hasMorgansMap = true;
        actionCardSidebar_card4.gameObject.SetActive(true);
    }

    public void RemoveMorgansMap()
    {
        hasMorgansMap = false;
        actionCardSidebar_card4.gameObject.SetActive(false);
    }

    public void ChooseCardCalled()
    {
        actionCard_ChoicePanel.SetActive(true);
    }

    public void UpdateActionCards(GameObject player)
    {
        PlayerGameScript player_script = player.GetComponent<PlayerGameScript>();
        ActionCard card1 = player_script.action_card_1;
        ActionCard card2 = player_script.action_card_2;
        ActionCard card3 = player_script.action_card_3;

        //update sidebar

        actionCardSidebar_card1.GetComponent<ActionCardSidebarScript>().UpdateActionCard(card1);
        actionCardSidebar_card2.GetComponent<ActionCardSidebarScript>().UpdateActionCard(card2);
        actionCardSidebar_card3.GetComponent<ActionCardSidebarScript>().UpdateActionCard(card3);
        
        //update choice panel

        if (hasMorgansMap)
        {
            // sidebar
            ActionCard card4 = player_script.action_card_4;
            actionCardSidebar_card4.GetComponent<ActionCardSidebarScript>().UpdateActionCard(card4);
            
            //main panel
        }
    }
}
