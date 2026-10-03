using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class InspectWindowCanvas : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    
    public TMPro.TMP_Text statusText;
    public TextMeshProUGUI relationsTMP;
    public GameObject ExecutePanel;

    //private string _statusText = "角色: {0}\n精神: {1}\n饥饿: {2}\n信任: {3}\n---------";
    
    void Start()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        ExecutePanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void HideWindow()
    {
        DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0, .5f).onComplete += () =>
        {
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        };
        ExecutePanel.SetActive(false);
    }

    public void ShowWindow()
    {
        DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1, .5f).onComplete += () =>
        {
            canvasGroup.alpha = 1;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        };
    }

    public void UpdateInfo(MainGameLogic.NpcStatus nS)
    {
        statusText.text = string.Format("角色: {0}\n等级: {1}\n经验值: {2}\n积分: {3}\n属性: {4}\n健康状态: {5}\n饥饿值: {6}\n连胜数: {7}\n连败数: {8}\n---------"
            , nS.npcName,nS.lvl,nS.exp,nS.credit,GS().attributes[nS.attribute],GS().healthRange[nS.health],nS.hunger,nS.continuousWinTimes,nS.continuousLossTimes);
        UpdateRelations(nS);
    }
    public GlobalSettings GS(){return GlobalSettings.Instance;}
    public void UpdateRelations(MainGameLogic.NpcStatus nS)
    {
        var g = MainGameLogic.Instance.npcStatuses;
        var text = "关系情况:\n";
        for (var i = 0; i < nS.npcRelations.Count; i++)
        {
            if (g[i] == nS) 
                text += "\n";
            else
                text += $"{g[i].npcName} -> 好感:{nS.npcRelations[i].relationValue}, 信任:{nS.npcRelations[i].trustValue}, 恐惧:{nS.npcRelations[i].horrorValue}.\n";
        }
        relationsTMP.SetText(text);
    }
}
