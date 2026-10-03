using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.Serialization;

public class GlobalSettings : MonoBehaviour
{
    public static GlobalSettings Instance;
    public static Script LuaScript;
    
    [Header("NPC 属性值定义区 注意:在更改了以下属性定义后 存档可能会重置")]
    [Header("\t饥饿值区间(包含) - 越高越不饿")]
    public Vector2Int hungerRange = new Vector2Int(0, 3);
    [Header("\t健康情况 - 越低越健康")]
    public List<string> healthRange = new List<string>{"健康","轻伤","重伤","恍惚"};
    [Header("\t属性值 - 除了无属性,1克2 2克3 ... 最后一位克第一位")]
    public List<string> attributes = new List<string> {"无","金","木","土","水","火"};
    
    [Header("战斗数据")]
    [Header("\t克制乘数")]
    public float advantageAttributeMultiplier = 1.2f;
    [Header("\t不利乘数")]
    public float disadvantageAttributeMultiplier = 0.8f;
    [Header("\tNPC最终战力计算描述")]
    [TextArea(3,5)] public string npcFinalBattlePowerCalculationDescription;
    [Header("\tNPC最终战力计算公式(Lua)")]
    [TextArea(3,5)] public string npcFinalBattlePowerLua;
    
    [Header("对话数据")]
    [Header("\t跟一个NPC对话时，最多可交谈次数")]
    public int chatTopicCount = 3;
    
    [Header("玩家资源")]
    [Header("\t最大可持有药物数")]
    public int maxMedicineCount = 2;
    [Header("\t最大可持有食物数")]
    public int maxFoodCount = 2;
    [Header("\t每日最多可交谈的NPC的数量")]
    public int maxChatCount = 3;
    //[Header("\t")]
    //public int maxLockUpCount = 1;
    [Header("\t初始药物持有数量")]
    public int initMedicineCount = 2;
    [Header("\t初始食物持有数量")]
    public int initFoodCount = 2;
    [Header("\t初始可交谈次数")]
    public int initChatCount = 2;
    //[Header("\t")]
    //public int initLockUpCount = 2;
    
    [FormerlySerializedAs("npcStatuses")] [Header("游戏开始时NPC的初始属性(对话相关的属性定义在对话数据库)")]
    public List<MainGameLogic.NpcStatus> npcInitStatuses = new List<MainGameLogic.NpcStatus>();

    [Header("除了第一天在开始时给予一次 其余 每日结束后在第二天开始时的游戏内情况")] 
    [Header("\t变量\n\tpChatCount:对话次数\t\t|pMaxChatCount:最多对话次数\n\tpMedicineCount:药物数量\t|pMaxMedicineCount:最多药物数量\n\tpFoodCount:食物数量\t\t|pMaxFoodCount:最多食物数量")]
    [TextArea(5,15)]
    public string newDayLua;

    [Space(20f)][TextArea(5,20)] [Header("调试功能")] public string luaExecuteOnGameStart;
    
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        GameAPI api = new GameAPI();
        
        UserData.RegisterAssembly();
        LuaScript = new Script();
        LuaScript.Globals["GameAPI"] = api;
        
        LuaScript.DoString(luaExecuteOnGameStart);
    }
    
    // Lua Stuff -------------------------------------------------------------------------------------
    [MoonSharpUserData]
    public class GameAPI
    {
        public void PrintMessage(string message)
        {
            Debug.Log(message);
        }
        
    }
}
