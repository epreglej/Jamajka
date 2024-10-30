using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
public class DiceUIScript : MonoBehaviour
{

    int day_dice_value;
    int night_dice_value;

    public TextMeshProUGUI day_number_text;
    public TextMeshProUGUI night_number_text;

    public Button switch_button;
    public Button confirm_button;

    public GameObject holder_panel;

    void Start()
    {
        holder_panel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space");
            GameManager.instance.ThrowDiceServerRpc();
        }
    }

    public void OpenDiceDialog(int day, int night, bool isCaptain)
    {
        Debug.Log("Opening dice window");
        holder_panel.SetActive(true);
        day_dice_value = day;
        night_dice_value = night;

        day_number_text.text = day.ToString();
        night_number_text.text = night.ToString();
        
        confirm_button.gameObject.SetActive(isCaptain);
        switch_button.gameObject.SetActive(isCaptain);
    }

    public void OnSwitchPressed()
    {
        GameManager.instance.SwitchDiceValuesClientRpc();
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SwitchValues()
    {
        Debug.Log("Switch numbers called");
        int temp = day_dice_value;
        day_dice_value = night_dice_value;
        night_dice_value = temp;

        day_number_text.text = day_dice_value.ToString();
        night_number_text.text = night_dice_value.ToString();
    }

    public void OnConfirmPressed()
    {
        Debug.Log("Confirm pressed");
        GameManager.instance.OnDayNightDiceOrderChosen(day_dice_value, night_dice_value);
    }

  
    public void CloseDiceDialog()
    {
        Debug.Log("Close the dialog");
        holder_panel.SetActive(false);
    }
}
