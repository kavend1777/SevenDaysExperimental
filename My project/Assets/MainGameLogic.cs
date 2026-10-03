using System.Collections;
using System.Collections.Generic;
using System.IO;
using DG.Tweening.Plugins.Core.PathCore;
using MoonSharp.Interpreter;
using PixelCrushers.DialogueSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Path = System.IO.Path;
using Toggle = UnityEngine.UI.Toggle;

public class MainGameLogic : MonoBehaviour
{
    public UnityEvent OnFoodGiven;
    public UnityEvent OnMedicineGiven;
    public UnityEvent OnNpcLockup;
    public UnityEvent OnCellSelectForBattleFirstSelected;
    public UnityEvent OnBattleStart;
    /// <summary>
    /// first int -> winner, second int -> loser
    /// </summary>
    public class BattleResultEvent : UnityEvent<int, int> { }
    public BattleResultEvent OnBattleResult;
    
    public static MainGameLogic Instance=null;
    public static int CurrentLookAtCell = -1;
    public static int CurrentBattleOpponentCell = -1;
    public static bool CurrentCellSelectionForBattle = false;
    public static int CurrentCellSelectionForBattleFirstSelected = -1;
    
    public static Vector2Int CurrentBattlePair = new Vector2Int(-1,-1);
    
    public GlobalSettings GS => GlobalSettings.Instance;
    public int currentChatTopicCount=0;
    
    [System.Serializable]
    public class NpcRelations
    {
        public static Vector2Int RelationValueRange=new Vector2Int(-5,5);
        public static Vector2Int TrustValueRange=new Vector2Int(-3,3);
        public static Vector2Int HorrorValueRange=new Vector2Int(0,5);
        
        public int relationValue = 0;
        public int trustValue = 0;
        public int horrorValue = 0;
    }
    [System.Serializable]
    public class NpcStatus
    {
        public static Vector2Int LvlRange=new Vector2Int(0,5);
        public static Vector2Int ExpRange=new Vector2Int(0,10);
        public static Vector2Int CreditRange=new Vector2Int(0,15);
        // 克制情况: 金->木->水->火->土->金  | 1->2->3->4->5->1
        //public static List<string> AttributeRange=new List<string>{"无","金","木","土","水","火"};
        //public static List<string> HealthRange = new List<string>{"健康","轻伤","重伤","恍惚"};
        //public static Vector2Int HungerRange=new Vector2Int(0,3);
        public static float AdvantageAttributeMultiplier = 1.2f;
        public static float DisadvantageAttributeMultiplier = 0.8f;
        
        public string npcName;
        public int lvl=0;
        public int exp=0;
        public int credit=0;
        [Tooltip("0-无  金木土水火-12345")]
        public int attribute = 0;
        [Tooltip("0-最健康")]
        public int health = 0;

        public int baseBattlePower = 100;
        
        public int hunger = 3;
        public int continuousWinTimes = 0;
        public int continuousLossTimes = 0;
        
        public List<NpcRelations> npcRelations = new List<NpcRelations>();
        //public List<string> npcRelationCorresNpcs = new List<string>();
    }

    [System.Serializable]
    public class PlayerResource
    {
        
        public int medicineCount = 0;
        public int foodCount = 0;
        public int chatTimes = 0;
        public int lockUpTimes = 0;
    }
    [System.Serializable]
    public class SaveType
    {
        public PlayerResource playerResource;
        public List<NpcStatus> npcStatuses = new List<NpcStatus>();
    }
    
    public UnityEvent ReturnToTitle;
    
    public List<NpcStatus> npcStatuses = new List<NpcStatus>();
    public PlayerResource pResource;
    
    public InspectWindowCanvas inspectWindowCanvas;

    public GameObject PauseMenu;
    
    [Space(20f)]
    public TMPro.TextMeshProUGUI dayTMP;
    public TextMeshProUGUI pResTMP;
    public GameObject ShowBattleCellSelectionPanel;
    public GameObject BattleCanvas;
    public TextMeshProUGUI BattleTextNpc1;
    public TextMeshProUGUI BattleResultTMP;
    public TextMeshProUGUI BattleTextNpc2;
    public TextMeshProUGUI BattleTextDetail;
    [TextArea(3, 10)] public string BattleTextDetailTextPrefix;


    private List<bool> CurrentDayTalkedCell = new List<bool>{false,false,false};
    public string dayText = "  DAY: {0}";
    void Start()
    {
        
        Resume();
        if(GameTitleLoader.Instance != null)
            ReturnToTitle.AddListener(GameTitleLoader.Instance.UnloadGame);
        UpdateDayText();
        LoadMainGameData();
        //Debug.Log($"Current Day:{DialogueLua.GetVariable("CurrentDay").AsInt}");
        
        // Day 0 init player resource & npc Statuses
        if (DialogueLua.GetVariable("CurrentDay").AsInt == 0)
        {
            pResource.chatTimes = GS.initChatCount;
            pResource.foodCount = GS.initFoodCount;
            pResource.medicineCount = GS.initMedicineCount;
            UpdatePlayerResourceText();
            npcStatuses = GS.npcInitStatuses;
        }
        else
        {
            //Debug.Log($"Current Day is not 0");
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!PauseMenu.activeSelf)
                PauseMenu.SetActive(true);
            else
            {
                Resume();
            }
        }
    }

    public void SaveMainGameData()
    {
        var playerSavePath = Path.Combine(Application.persistentDataPath, "NpcRelations.json");

        SaveType toSave=new SaveType
        {
            npcStatuses =  this.npcStatuses,
            playerResource = this.pResource
        };
        File.WriteAllText(playerSavePath, JsonUtility.ToJson(toSave,true));
    }

    public void LoadMainGameData()
    {
        var playerSavePath = Path.Combine(Application.persistentDataPath, "NpcRelations.json");

        if (File.Exists(playerSavePath))
        {
            SaveType st = JsonUtility.FromJson<SaveType>(File.ReadAllText(playerSavePath));
            if (st != null)
            {
                if(st.npcStatuses != null)
                    this.npcStatuses = st.npcStatuses;
                if(st.playerResource != null)
                    this.pResource = st.playerResource;
            }
            else
            {
                pResource = new PlayerResource();
            }
            
        }
        else
        {
            pResource = new PlayerResource();
        }
        
        UpdatePlayerResourceText();

    }

    void OnEnable()
    {
        if (Instance == null) Instance = this;
    }
    void OnDestroy()
    {
        if(Instance == this)
            Instance = null;
        Debug.Log("Logic OnDestroy");
    }

    public void LookInsideInspectWindow(int index)
    {
        if (CurrentCellSelectionForBattle)
        {
            if (CurrentCellSelectionForBattleFirstSelected >= 0)
            {
                CurrentBattlePair[0] = CurrentCellSelectionForBattleFirstSelected;
                CurrentBattlePair[1] = index;
                ApplyBattleCellSelection(CurrentCellSelectionForBattleFirstSelected,index);
                CurrentCellSelectionForBattleFirstSelected = -1;
                OnBattleStart?.Invoke();
                return;
            }
            OnCellSelectForBattleFirstSelected?.Invoke();
            CurrentCellSelectionForBattleFirstSelected = index;
            return;
        }
        if (index >= 0 && index < npcStatuses.Count)
        {
            inspectWindowCanvas.UpdateInfo(npcStatuses[index]);
        }
        inspectWindowCanvas.ShowWindow();
        CurrentLookAtCell = index;
        var go = GameObject.FindGameObjectWithTag("ChatButton");
        Button chatBtn = null;
        if (go != null)
        {
            chatBtn = go.GetComponent<Button>();
            
            
        }
        if (pResource.chatTimes == 0 || CurrentDayTalkedCell[index] == true)
        {
            if (chatBtn != null)
                chatBtn.interactable = false;
        }
        else
        {
            if (chatBtn != null)
                chatBtn.interactable = true;
            InitCurrentChatTopicCount();
            DialogueManager.ShowAlert($"你有 {currentChatTopicCount} 次对话次数.");
        }
        
    }

    public string GetNpcNameByIndex(int index)
    {
        return npcStatuses[index].npcName;
    }

    public void DebugPrint(Transform trans)
    {
        Debug.Log("Debug Print");
    }
    
    public void UpdateDayText()
    {
        dayTMP.SetText(string.Format(dayText,DialogueLua.GetVariable("CurrentDay").AsInt));
        
    }

    public void UpdateBattleButtonText(TextMeshProUGUI battleButtonTMP,string overrideText="")
    {
        if(overrideText=="")
            battleButtonTMP.SetText($"A: {npcStatuses[CurrentCellSelectionForBattleFirstSelected].npcName}");
        else
        {
            battleButtonTMP.SetText(overrideText);
        }
    }

    public void InitCurrentChatTopicCount()
    {
        currentChatTopicCount = GS.chatTopicCount;
    }

    public void UpdatePlayerResourceText()
    {
        pResTMP.SetText(string.Format("玩家资源:\n  药物: {0}\n  食物: {1}\n  私聊次数: {2}"
            ,pResource.medicineCount,pResource.foodCount,pResource.chatTimes));
    }

    public void PlayerGetDayResource()
    {
        // pResource.chatTimes = PlayerResource.MaxChatCount;
        // pResource.foodCount = PlayerResource.MaxFoodCount;
        // pResource.medicineCount = PlayerResource.MaxMedicineCount;
        // pResource.lockUpTimes = PlayerResource.MaxLockUpCount;
        GlobalSettings.LuaScript.Globals["pChatCount"] = pResource.chatTimes;
        GlobalSettings.LuaScript.Globals["pMedicineCount"] = pResource.medicineCount;
        GlobalSettings.LuaScript.Globals["pFoodCount"] = pResource.foodCount;
        GlobalSettings.LuaScript.Globals["pMaxChatCount"] = GS.maxChatCount;
        GlobalSettings.LuaScript.Globals["pMaxMedicineCount"] = GS.maxMedicineCount;
        GlobalSettings.LuaScript.Globals["pMaxFoodCount"] = GS.maxFoodCount;


        GlobalSettings.LuaScript.DoString(GS.newDayLua);

        pResource.chatTimes = (int)GlobalSettings.LuaScript.Globals.Get("pChatCount").Number;
        pResource.medicineCount = (int)GlobalSettings.LuaScript.Globals.Get("pMedicineCount").Number;
        pResource.foodCount = (int)GlobalSettings.LuaScript.Globals.Get("pFoodCount").Number;
        
        UpdatePlayerResourceText();
        for (int i = 0; i < CurrentDayTalkedCell.Count; i++)
        {
            CurrentDayTalkedCell[i] = false;
        }
    }
    
    public void QuitToTitle()
    {
        ReturnToTitle?.Invoke();
        Destroy(gameObject);
        GameTitleLoader.Instance.DisplayTitle();
    }

    public void Resume()
    {
        PauseMenu.SetActive(false);
    }

    public void QuitToDesktop()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
        Application.Quit();
    }

    public void GiveFood()
    {
        pResource.foodCount -= 1;
        DialogueLua.SetVariable($"{npcStatuses[CurrentLookAtCell].npcName}被给予食物",true);
        OnFoodGiven?.Invoke();
        
        npcStatuses[CurrentLookAtCell].hunger += 1;
        DialogueManager.ShowAlert("已给予1份食物");
        var go = GameObject.FindGameObjectWithTag("ExecutePanel");
        if (go != null)
            ToggleGameObjectActive(go);
        UpdatePlayerResourceText();
        inspectWindowCanvas.UpdateInfo(npcStatuses[CurrentLookAtCell]);
    }
    public void GiveMedicine()
    {
        pResource.medicineCount -= 1;
        DialogueLua.SetVariable($"{npcStatuses[CurrentLookAtCell].npcName}被治疗",true);
        OnMedicineGiven?.Invoke();
        
        
        
        npcStatuses[CurrentLookAtCell].health -= 1;
        DialogueManager.ShowAlert("已给予1份药物");
        var go = GameObject.FindGameObjectWithTag("ExecutePanel");
        if (go != null)
            ToggleGameObjectActive(go);
        UpdatePlayerResourceText();
        inspectWindowCanvas.UpdateInfo(npcStatuses[CurrentLookAtCell]);
    }

    public void LockUpNpc()
    {
        pResource.lockUpTimes -= 1;
        DialogueLua.SetVariable($"{npcStatuses[CurrentLookAtCell].npcName}被关禁闭",true);
        OnNpcLockup?.Invoke();
        
        DialogueManager.ShowAlert("关禁闭中");
        var go = GameObject.FindGameObjectWithTag("ExecutePanel");
        if (go != null)
            ToggleGameObjectActive(go);
        UpdatePlayerResourceText();
        inspectWindowCanvas.UpdateInfo(npcStatuses[CurrentLookAtCell]);
    }

    public void DisplayCellSelectionPanel()
    {
        for (int i = 0; i < ShowBattleCellSelectionPanel.transform.childCount; i++)
        {
            ShowBattleCellSelectionPanel.transform.GetChild(i).gameObject.SetActive(true);
        }
        ShowBattleCellSelectionPanel.transform.GetChild(CurrentLookAtCell).gameObject.SetActive(false);
        ShowBattleCellSelectionPanel.SetActive(true);
    }

    public void ApplyBattleCellSelection(int index)
    {
        //ShowBattleCellSelectionPanel.SetActive(false);
        if (index < 0) return;
        
        
        
        CurrentBattleOpponentCell = index;
        BattleCanvas.SetActive(true);
        
        // Init Battle Canvas Text
        BattleTextNpc1.SetText("受试者A: "+npcStatuses[CurrentLookAtCell].npcName);
        BattleTextNpc2.SetText("受试者B: "+npcStatuses[CurrentBattleOpponentCell].npcName);
        // float value1AttributeMulti = 1;
        // float value2AttributeMulti = 1;
        // float value1SicknessMulti = 1;
        // float value2SicknessMulti = 1;
        // float v1winOrLoseCorrection = 1;
        // float v2winOrLoseCorrection = 1;
        float value1 = npcStatuses[CurrentLookAtCell].baseBattlePower;
        float value2 = npcStatuses[CurrentBattleOpponentCell].baseBattlePower;
        BattleTextDetail.SetText(BattleTextDetailTextPrefix+$"\n{(value1)/(value2+value1)*100:F2}% : {(value2)/(value2+value1)*100:F2}%");
    }

    public void ApplyBattleCellSelection(int indexA,int indexB)
    {
        BattleCanvas.SetActive(true);
        
        // Init Battle Canvas Text
        BattleTextNpc1.SetText("受试者A: "+npcStatuses[indexA].npcName);
        BattleTextNpc2.SetText("受试者B: "+npcStatuses[indexB].npcName);
        
        BattleTextDetail.SetText(GS.npcFinalBattlePowerCalculationDescription+"\n"+GetBattleWinRateString(npcStatuses[indexA], npcStatuses[indexB]));

    }
    
    public float GetNpcFinalBattlePower(NpcStatus npcStatus)
    {
        
        GlobalSettings.LuaScript.Globals["npcBaseBattlePower"]=npcStatus.baseBattlePower;
        GlobalSettings.LuaScript.Globals["npcLvl"] = npcStatus.lvl;
        GlobalSettings.LuaScript.Globals["advantageAttributeMultiplier"] = GS.advantageAttributeMultiplier;
        GlobalSettings.LuaScript.Globals["disadvantageAttributeMultiplier"] = GS.disadvantageAttributeMultiplier;

        
        
        DynValue result = GlobalSettings.LuaScript.DoString(GS.npcFinalBattlePowerLua);
        if (result.Type == DataType.Number)
        {
            Debug.Log($"最终战力计算成功:{result.Number}");
            return (float)(result.Number);
        }
        Debug.LogError(result.ToString());
        return npcStatus.baseBattlePower;
    }
    /// <summary>
    /// return "XX% : XX%"
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public string GetBattleWinRateString(NpcStatus a, NpcStatus b)
    {
        return $"{GetNpcFinalBattlePower(a)/(GetNpcFinalBattlePower(a)+GetNpcFinalBattlePower(b))*100:F2}% : {GetNpcFinalBattlePower(b)/(GetNpcFinalBattlePower(a)+GetNpcFinalBattlePower(b))*100:F2}%";
    }
    
    public void StartBattle()
    {
        float value1 = GetNpcFinalBattlePower(npcStatuses[CurrentBattlePair[0]]);
        float value2 = GetNpcFinalBattlePower(npcStatuses[CurrentBattlePair[1]]);
        
        var r = UnityEngine.Random.Range(0f, 1f);

        int winnerIndex;
        int loserIndex;
        
        if (((value1) / (value1 + value2))>r )
        {
            // Battle A win the battle
            BattleResultTMP.SetText($"0-A-{((value1) / (value1 + value2)):F2}-B-1 => {r:F2}\n受试者A胜出\n受试者A状态变化:\n受试者B状态变化:");
            winnerIndex = CurrentBattlePair[0];
            loserIndex =  CurrentBattlePair[1];
        }
        else
        {
            // Battle B win the battle
            BattleResultTMP.SetText($"0-A-{((value1) / (value1 + value2)):F2}-B-1 => {r:F2}\n受试者B胜出\n受试者A状态变化:\n受试者B状态变化:");
            winnerIndex = CurrentBattlePair[1];
            loserIndex =  CurrentBattlePair[0];
        }
        
    }

    public static void SetCurrentCellSelectionForBattle(bool v)
    {
        CurrentCellSelectionForBattle = v;
    }

    public void DialogActed(Button calledButton)
    {
        if (!CurrentDayTalkedCell[CurrentLookAtCell])
        {
            pResource.chatTimes -= 1;
            CurrentDayTalkedCell[CurrentLookAtCell] = true;
        }

        currentChatTopicCount -= 1;
        DialogueManager.ShowAlert($"当前对话剩余次数:{currentChatTopicCount}");
        UpdatePlayerResourceText();
        if (pResource.chatTimes == 0 || currentChatTopicCount == 0)
        {
            calledButton.interactable = false;
        }
    }
    //public GlobalSettings GS(){return GlobalSettings.Instance;}
    public static void ToggleGameObjectActive(GameObject go)
    {
        go.SetActive(!go.activeSelf);

        if (go.CompareTag("ExecutePanel"))
        {
            if (go.activeSelf)
            {
                if(go.transform.GetChild(0).TryGetComponent<Button>(out var btn))
                    if (Instance.pResource.medicineCount>0 && Instance.npcStatuses[CurrentLookAtCell].health>0) btn.interactable = true;
                    else  btn.interactable = false;
                if(go.transform.GetChild(1).TryGetComponent<Button>(out var btn2))
                    if (Instance.pResource.foodCount>0 && Instance.npcStatuses[CurrentLookAtCell].hunger<Instance.GS.hungerRange[1]) btn2.interactable = true;
                    else  btn2.interactable = false;
                if(go.transform.GetChild(2).TryGetComponent<Button>(out var btn3))
                    if (Instance.pResource.lockUpTimes==0) btn3.interactable = false;
                    else  btn3.interactable = true;

            }
            else
            {
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    if (go.transform.GetChild(i).TryGetComponent<Button>(out var btn))
                    {
                        btn.interactable = true;
                    }
                }
            }
        }
    }
}
