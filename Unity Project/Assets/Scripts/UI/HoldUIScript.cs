using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HoldUIScript : MonoBehaviour
{
    [SerializeField] private List<GameObject> _slots;
    [SerializeField] private TextMeshProUGUI _notificationText;
    [SerializeField] private Sprite _foodTokenSprite;
    [SerializeField] private Sprite _goldTokenSprite;
    [SerializeField] private Sprite _cannonTokenSprite;
    [SerializeField] private GameObject _slotContainer;
    private int[] _shownDeltas = new int[5]{0, 0, 0, 0, 0};

    
    public void UpdateSlot(int slotIndex, GameManager.TokenType tokenType, int amount) {
        GameObject slot = _slots[slotIndex];
        Image slotImage = slot.transform.GetChild(0).GetComponent<Image>();
        slotImage.sprite = GetSprite(tokenType);
        TextMeshProUGUI slotText = slot.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        string slotTextBefore = slotText.text;
        slotText.text = amount > 0 ? amount.ToString() + " " + tokenType.ToString() : "";
        if (amount > 0) {
            slotImage.color = Color.white;
        } else {
            slotImage.color = Color.clear;
        }

        int delta;
        string tokenName = tokenType != GameManager.TokenType.None ? tokenType.ToString() : "";
        if (slotTextBefore == "") {
            delta = amount;
            tokenName = tokenType.ToString();
        } else {
            string[] parts = slotTextBefore.Split(' ');
            string amountBeforeString = parts[0];
            tokenName = parts.Length > 1 ? parts[1] : tokenType.ToString();
            int amountBefore = int.Parse(amountBeforeString);
            delta = amount - amountBefore;
        }
        ShowResourceDelta(slotIndex, delta, tokenName);
    }

    private void ShowResourceDelta(int slotIndex, int delta, string tokenName) {
        if (delta == 0) return;
        Debug.Log("Resource delta: " + delta);
        GameObject slot = _slots[slotIndex];
        TextMeshProUGUI deltaText = slot.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        tokenName = " " + tokenName;
        deltaText.text = delta > 0 ? "+" + delta + tokenName : delta + tokenName;
        deltaText.color = delta > 0 ? Color.yellow : Color.red;
        deltaText.gameObject.SetActive(true);

        StartCoroutine(HideDeltaAfterSeconds(deltaText.gameObject, 4f, slotIndex));
    }

    private System.Collections.IEnumerator HideDeltaAfterSeconds(GameObject go, float seconds, int slotIndex) {
        while (_shownDeltas[slotIndex] == 1) {
            yield return null;
        }
        
        _shownDeltas[slotIndex] = 1;
        yield return new WaitForSeconds(seconds);
        go.SetActive(false);
        _shownDeltas[slotIndex] = 0;
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
