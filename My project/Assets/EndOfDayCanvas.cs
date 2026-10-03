using System.Collections;
using System.Collections.Generic;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using TMPro;
using UnityEngine;

public class EndOfDayCanvas : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    
    // Start is called before the first frame update
    void Start()
    {
        HideEndOfDay();
    }


    public void NextDay()
    {
        HideEndOfDay();
        DialogueLua.SetVariable("CurrentDay",DialogueLua.GetVariable("CurrentDay").AsInt+1);
        var g = GameObject.FindGameObjectWithTag("DayTMP");
        if (g!=null)
        {
            var tmp = g.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.SetText($"  DAY: {DialogueLua.GetVariable("CurrentDay").AsInt}");
            }
        }
        if(MainGameLogic.Instance!=null)
            MainGameLogic.Instance.PlayerGetDayResource();
        //Debug.Log("Next Day");
        
    }

    public void HideEndOfDay()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
    public void DisplayEndOfDay()
    {
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }

    public void FindSaveSystemAndSaveGame()
    {
        var g = GameObject.FindGameObjectWithTag("Dialogue Manager");
        if (g)
        {
            var saveSystem = g.GetComponent<SaveSystem>();
            if (saveSystem)
            {
                saveSystem.SaveGameToSlot(0);
                Debug.Log("Save System saved!");
            }
        }
        
    }
}
