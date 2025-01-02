using UnityEngine;
using UnityEngine.UI;

public class ActionCardSidebarScript : MonoBehaviour
{
    [SerializeField] Image leftIcon;
    [SerializeField] Image rightIcon;

    public Sprite moveForwardSprite;
    public Sprite moveBackwardSprite;
    public Sprite foodSprite;
    public Sprite goldSprite;
    public Sprite cannonSprite;
    public Sprite emptySprite;

    public void UpdateActionCard(ActionCard actionCard)
    {
        // update left icon
        leftIcon.sprite = GetActionOptionSprite(actionCard.leftOption);

        // update right icon
        rightIcon.sprite = GetActionOptionSprite(actionCard.rightOption);
    }

    public Sprite GetActionOptionSprite(GameManager.ActionCardOption option)
    {
        switch (option)
        {
            case GameManager.ActionCardOption.MoveForward:
                return moveForwardSprite;
            case GameManager.ActionCardOption.MoveBackward:
                return moveBackwardSprite;
            case GameManager.ActionCardOption.LoadFood:
                return foodSprite;
            case GameManager.ActionCardOption.LoadGold:
                return goldSprite;
            case GameManager.ActionCardOption.LoadCannon:
                return cannonSprite;
            default:
                return emptySprite;
        }
    }
}
