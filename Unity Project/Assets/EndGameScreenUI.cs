using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class EndGameScreenUI : MonoBehaviour
{

    [SerializeField] Image end_screen_panel;

    [SerializeField] TextMeshProUGUI winner_name;
    [SerializeField] TextMeshProUGUI winner_points;

    [SerializeField] TextMeshProUGUI _2nd_place_name;
    [SerializeField] TextMeshProUGUI _2nd_place_points;

    [SerializeField] TextMeshProUGUI _3rd_place_name;
    [SerializeField] TextMeshProUGUI _3rd_place_points;

    [SerializeField] TextMeshProUGUI _4th_place_name;
    [SerializeField] TextMeshProUGUI _4th_place_points;

    [SerializeField] Button QuitGame_Button;
    [SerializeField] Button RestartGame_Button;

    private void Start()
    {
        end_screen_panel.gameObject.SetActive(false);
    }

    public void ShowGameResults(List<string> order, Dictionary<string, int> results)
    {
        winner_name.text = order[0];
        winner_points.text = results.GetValueOrDefault(order[0]).ToString();

        _2nd_place_name.text = order[1];
        _2nd_place_points.text = results.GetValueOrDefault(order[1]).ToString();

        if(order.Count > 2)
        {
            _3rd_place_name.text = order[2];
            _3rd_place_points.text = results.GetValueOrDefault(order[2]).ToString();
        }

        if(order.Count > 3)
        {
            _4th_place_name.text = order[3];
            _4th_place_points.text = results.GetValueOrDefault(order[3]).ToString();
        }

        end_screen_panel.gameObject.SetActive(true);
    }

    public void OnQuitPressed()
    {
        GameManager.instance.PlayerCalledQuitGame();
    }

    public void OnRestartPressed()
    {
        GameManager.instance.PlayerCalledRestartGame();
    }
}
