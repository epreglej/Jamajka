using UnityEngine;
using UnityEngine.UI;

public class ActionCardSidebarScript : MonoBehaviour
{
    [SerializeField] Image leftIcon;
    [SerializeField] Image rightIcon;

    public void UpdateActionCard(ActionCard actionCard)
    {
        // update left icon
        leftIcon.sprite = actionCard.GetActionOptionSprite(actionCard.leftOption);

        // update right icon
        rightIcon.sprite = actionCard.GetActionOptionSprite(actionCard.rightOption);
    }
}
