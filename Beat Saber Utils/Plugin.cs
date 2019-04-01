using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using IllusionPlugin;
using Harmony;
using BS_Utils.Data;

namespace BS_Utils
{
    public class Plugin : IPlugin
    {
        public string Name => "Beat Saber Utils";
        public string Version => "1.2.1";
        internal static bool patched = false;
        internal static HarmonyInstance harmony;
        public static Gameplay.LevelData LevelData = new Gameplay.LevelData();
        public delegate void LevelDidFinish(StandardLevelScenesTransitionSetupDataSO levelScenesTransitionSetupDataSO, LevelCompletionResults levelCompletionResults);
        public static event LevelDidFinish LevelDidFinishEvent;

        /// <summary>
        /// Handles useful Data from the song and menu.
        /// This field can and should be used in other mods, so long as this Plugin is assumed to exist.
        /// You can also hook into an event that is called every time the Data is updated by doing <code>Data.statusChange += yourHookHere</code>
        /// And you can access Data directly by doing: <code>BS_Utils.Plugin.dataManager.Data</code>
        /// </summary>
        public static DataManager dataManager = new DataManager();

        public void OnApplicationStart()
        {
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            //Create Harmony Instance
            harmony = HarmonyInstance.Create("com.kyle1413.BeatSaber.BS-Utils");
            dataManager.OnApplicationStart();
        }

        private void SceneManagerOnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            if (newScene.name == "MenuCore")
            {
                Utilities.Logger.Log("Removing Isolated Level");
                Gameplay.Gamemode.IsIsolatedLevel = false;
                Gameplay.Gamemode.IsolatingMod = "";
                LevelData.Clear();
            }
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode arg1)
        {

        }

        public void OnApplicationQuit()
        {
            SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
            dataManager.OnApplicationQuit();
        }

        public void OnLevelWasLoaded(int level)
        {

        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnUpdate()
        {
            dataManager.OnUpdate();
        }

        public void OnFixedUpdate()
        {
        }

        internal static void TriggerLevelFinishEvent(StandardLevelScenesTransitionSetupDataSO levelScenesTransitionSetupDataSO, LevelCompletionResults levelCompletionResults)
        {
            LevelDidFinishEvent?.Invoke(levelScenesTransitionSetupDataSO, levelCompletionResults);
        }
        internal static void ApplyHarmonyPatches()
        {
            if (patched) return;
            try
            {
                Utilities.Logger.Log("Applying Harmony Patches");
                harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
                patched = true;
            }
            catch (Exception ex)
            {
                Utilities.Logger.Log("Exception Trying to Apply Harmony Patches");
                Utilities.Logger.Log(ex.ToString());
            }


        }
    }
}
