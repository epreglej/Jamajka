using UnityEngine;

[CreateAssetMenu(fileName = "ActionCard", menuName = "Scriptable Objects/ActionCard")]
public class ActionCard : ScriptableObject
{
    public string cardID;
    public GameManager.ActionCardOption leftOption;
    public GameManager.ActionCardOption rightOption;
    public Sprite backgroundImage;
}
