using System.Collections;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;

public class StartCorrespondConversation : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartCurrentNpcConversation(Transform conversant=null)
    {
        var _name = MainGameLogic.Instance.npcStatuses[MainGameLogic.CurrentLookAtCell].npcName;
        
        DialogueManager.StartConversation($"{_name}/{_name}对话入口",null, conversant);
    }
}
