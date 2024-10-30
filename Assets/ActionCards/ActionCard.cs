using UnityEngine;

[CreateAssetMenu(fileName = "ActionCard", menuName = "Scriptable Objects/ActionCard")]
public class ActionCard : ScriptableObject
{
    public GameManager.ActionCardOption leftOption;
    public GameManager.ActionCardOption rightOption;
    public Sprite backgroundImage;

    private Sprite moveForwardSprite;
    private Sprite moveBackwardSprite;
    private Sprite foodSprite;
    private Sprite goldSprite;
    private Sprite cannonSprite;
    private Sprite emptySprite;

    private void OnEnable()
    {
        moveForwardSprite = Resources.Load<Sprite>("Sprites/ActionCardIcons/MoveForward");
        moveBackwardSprite = Resources.Load<Sprite>("Sprites/ActionCardIcons/MoveBackwards");
        foodSprite = Resources.Load<Sprite>("Sprites/ActionCardIcons/FoodToken");
        goldSprite = Resources.Load<Sprite>("Sprites/ActionCardIcons/GoldToken");
        cannonSprite = Resources.Load<Sprite>("Sprites/ActionCardIcons/CannonToken");
        emptySprite = Resources.Load<Sprite>("Sprites/ActionCardIcons/EmptySprite");
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
