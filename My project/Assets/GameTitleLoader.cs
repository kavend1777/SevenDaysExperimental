using System.Collections;
using System.Collections.Generic;
using System.IO;
using DG.Tweening.Plugins.Core.PathCore;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using UnityEngine;

using UnityEngine.SceneManagement;
using Path = DG.Tweening.Plugins.Core.PathCore.Path;

public class GameTitleLoader : MonoBehaviour
{
    public static GameTitleLoader Instance;

    public GameObject Title;

    public string mainSceneName;
    // Start is called before the first frame update
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            DisplayTitle();
        }
        else
        {
            Destroy(gameObject);
        }
        
    }

    public void DisplayTitle()
    {
        Title.SetActive(true);
    }

    public void HideTitle()
    {
        Title.SetActive(false);
    }

    public void LoadGame()
    {
        bool fileE = File.Exists(Application.persistentDataPath + "/NpcRelations.json");
        if (SaveSystem.HasSavedGameInSlot(0) &&fileE)
        {
            SavedGameData d = SaveSystem.storer.RetrieveSavedGameData(0);
            string k = "ds";
            
            var dsData = d.GetData(k);
            
            PersistentDataManager.ApplySaveData(dsData);
        }
        SceneManager.LoadScene(mainSceneName,LoadSceneMode.Additive);
        
        HideTitle();
    }

    public void UnloadGame()
    {
        SceneManager.UnloadSceneAsync(mainSceneName);
    }
    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
