using System.Collections;
using System.Collections.Generic;
using System.IO;
using DG.Tweening.Plugins.Core.PathCore;
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
    
    public static MainGameLogic Instance=null;
    public static int CurrentLookAtCell = -1;
    public static int CurrentBattleOpponentCell = -1;
    public static bool CurrentCellSelectionForBattle = false;
    public static int CurrentCellSelectionForBattleFirstSelected = -1;
    
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
        public static List<string> AttributeRange=new List<string>{"无","金","木","土","水","火"};
        public static List<string> HealthRange = new List<string>{"健康","轻伤","重伤","恍惚"};
        public static Vector2Int HungerRange=new Vector2Int(0,3);
        public static float AdvantageAttributeMultiplier = 1.2f;
        public static float DisadvantageAttributeMultiplier = 0.8f;
        
        public string npcName;
        public int lvl=0;
        public int exp=0;
        public int credit=0;
        [Tooltip("0-无  金木水火土-12345")]
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
        public static int MaxMedicineCount = 2;
        public static int MaxFoodCount = 2;
        public static int MaxChatCount = 3;
        public static int MaxLockUpCount = 1;
        
        public int medicineCount = MaxMedicineCount;
        public int foodCount = MaxFoodCount;
        public int chatTimes = MaxChatCount;
        public int lockUpTimes = MaxLockUpCount;
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
    
    
    
    public string dayText = "  DAY: {0}";
    void Start()
    {
        
        Resume();
        if(GameTitleLoader.Instance != null)
            ReturnToTitle.AddListener(GameTitleLoader.Instance.UnloadGame);
        UpdateDayText();
        LoadMainGameData();
        
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            var systems = FindObjectsOfType<EventSystem>(true); // true = 包含未激活的
            Debug.Log($"当前场景中一共找到 {systems.Length} 个 EventSystem：");
            foreach (var es in systems)
            {
                Debug.Log($"名称: {es.name} | 场景: {es.gameObject.scene.name} | 激活: {es.gameObject.activeInHierarchy}", es.gameObject);
            }
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

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
        if (pResource.chatTimes == 0)
        {
            if (chatBtn != null)
                chatBtn.interactable = false;
        }
        else
        {
            if (chatBtn != null)
                chatBtn.interactable = true;
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

    public void UpdatePlayerResourceText()
    {
        pResTMP.SetText(string.Format("玩家资源:\n  药物: {0}\n  食物: {1}\n  私聊次数: {2}\n  禁闭权: {3}"
            ,pResource.medicineCount,pResource.foodCount,pResource.chatTimes,pResource.lockUpTimes));
    }

    public void PlayerGetDayResource()
    {
        pResource.chatTimes = PlayerResource.MaxChatCount;
        pResource.foodCount = PlayerResource.MaxFoodCount;
        pResource.medicineCount = PlayerResource.MaxMedicineCount;
        pResource.lockUpTimes = PlayerResource.MaxLockUpCount;
        UpdatePlayerResourceText();
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
        // float value1AttributeMulti = 1;
        // float value2AttributeMulti = 1;
        // float value1SicknessMulti = 1;
        // float value2SicknessMulti = 1;
        // float v1winOrLoseCorrection = 1;
        // float v2winOrLoseCorrection = 1;
        float value1 = npcStatuses[indexA].baseBattlePower;
        float value2 = npcStatuses[indexB].baseBattlePower;
        BattleTextDetail.SetText(BattleTextDetailTextPrefix+$"\n{(value1)/(value2+value1)*100:F2}% : {(value2)/(value2+value1)*100:F2}%");

    }

    public void StartBattle()
    {
        float value1 = npcStatuses[CurrentLookAtCell].baseBattlePower;
        float value2 = npcStatuses[CurrentBattleOpponentCell].baseBattlePower;
        
        var r = UnityEngine.Random.Range(0f, 1f);
        if (((value1) / (value1 + value2))>r )
        {
            // Battle A win the battle
            BattleResultTMP.SetText($"0-A-{((value1) / (value1 + value2)):F2}-B-1 => {r:F2}\n受试者A胜出\n受试者A状态变化:\n受试者B状态变化:");
        }
        else
        {
            // Battle B win the battle
            BattleResultTMP.SetText($"0-A-{((value1) / (value1 + value2)):F2}-B-1 => {r:F2}\n受试者B胜出\n受试者A状态变化:\n受试者B状态变化:");

        }
        
    }

    public static void SetCurrentCellSelectionForBattle(bool v)
    {
        CurrentCellSelectionForBattle = v;
    }

    public void DialogActed(Button calledButton)
    {
        pResource.chatTimes -= 1;
        UpdatePlayerResourceText();
        if (pResource.chatTimes == 0)
        {
            calledButton.interactable = false;
        }
    }
    
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
                    if (Instance.pResource.foodCount>0 && Instance.npcStatuses[CurrentLookAtCell].hunger<NpcStatus.HungerRange[1]) btn2.interactable = true;
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
