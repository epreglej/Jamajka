using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HoldUIScript : MonoBehaviour
{
    [SerializeField] private List<GameObject> _slots;
    [SerializeField] private Sprite _foodTokenSprite;
    [SerializeField] private Sprite _goldTokenSprite;
    [SerializeField] private Sprite _cannonTokenSprite;
    [SerializeField] private GameObject _slotContainer;

    
    public void UpdateSlot(int slotIndex, GameManager.TokenType tokenType, int amount) {
        GameObject slot = _slots[slotIndex];
        Image slotImage = slot.transform.GetChild(0).GetComponent<Image>();
        slotImage.sprite = GetSprite(tokenType);
        slot.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = amount > 0 ? amount.ToString() : "";
        if (amount > 0) {
            slotImage.color = Color.white;
        } else {
            slotImage.color = Color.clear;
        }
    }

    public Sprite GetSprite(GameManager.TokenType tokenType) {
        switch (tokenType) {
            case GameManager.TokenType.Food:
                return _foodTokenSprite;
            case GameManager.TokenType.Gold:
                return _goldTokenSprite;
            case GameManager.TokenType.Cannon:
                return _cannonTokenSprite;
            default:
                return null;
        }
    }

    public void Show() {
        _slotContainer.SetActive(true);
    }

    public void Hide() {
        _slotContainer.SetActive(false);
    }

    public void Clear() {
        foreach (GameObject slot in _slots) {
            slot.transform.GetChild(0).GetComponent<Image>().color = Color.clear;
            slot.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "";
        }
    }
}
