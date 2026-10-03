using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TitleSceneButtons : MonoBehaviour
{
    public Button startButton;
    public Button exitButton;

    void Start()
    {

        StartCoroutine(WaitAndDo());

    }

    IEnumerator WaitAndDo()
    {
        yield return new WaitUntil(() => GameTitleLoader.Instance != null);

        Init();
    }

    void Init()
    {
        Debug.Log("Button inited");
        if (startButton != null)
        {
            // Disable Game Title
            
            // Load main Scene
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(() => { Application.Quit(); });
        }
    }
}
