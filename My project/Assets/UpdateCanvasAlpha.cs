using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class UpdateCanvasAlpha : MonoBehaviour
{
    public CanvasGroup myCanvasGroup;
    public CanvasGroup targetCanvasGroup;
    public float duration = 1f;

    // Update is called once per frame
    void Update()
    {
        myCanvasGroup.alpha = targetCanvasGroup.alpha;
    }

    public void HideCanvas()
    {
        DOTween.To(() => myCanvasGroup.alpha, x => myCanvasGroup.alpha = x, 0, duration);
    }
    
    public void ShowCanvas()
    {
        DOTween.To(() => myCanvasGroup.alpha, x => myCanvasGroup.alpha = x, 1, duration);
    }
}
