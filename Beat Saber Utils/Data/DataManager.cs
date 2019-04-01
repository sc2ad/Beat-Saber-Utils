using BS_Utils.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BS_Utils.Data
{
    /// <summary>
    /// Copied from: https://github.com/sc2ad/BeatSaberDataWrappers/blob/master/BeatSaberDataWrappers/Plugin.cs
    /// Author: Sc2ad
    /// </summary>
    public class DataManager
    {
        public DataObject data = new DataObject();

        private bool headInObstacle = false;

        private GameplayCoreSceneSetupData gameplayCoreSceneSetupData;
        private GamePauseManager gamePauseManager;
        private ScoreController scoreController;
        private StandardLevelGameplayManager gameplayManager;
        private GameplayModifiersModelSO gameplayModifiersSO;
        private AudioTimeSyncController audioTimeSyncController;
        private BeatmapObjectCallbackController beatmapObjectCallbackController;
        private PlayerHeadAndObstacleInteraction playerHeadAndObstacleInteraction;
        private GameEnergyCounter gameEnergyCounter;
        private Dictionary<NoteCutInfo, NoteData> noteCutMapping = new Dictionary<NoteCutInfo, NoteData>();

        /// protected NoteCutInfo AfterCutScoreBuffer._noteCutInfo
		private FieldInfo noteCutInfoField = typeof(AfterCutScoreBuffer).GetField("_noteCutInfo", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        /// protected List<AfterCutScoreBuffer> ScoreController._afterCutScoreBuffers // contains a list of after cut buffers
        private FieldInfo afterCutScoreBuffersField = typeof(ScoreController).GetField("_afterCutScoreBuffers", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        /// private int AfterCutScoreBuffer#_afterCutScoreWithMultiplier
        private FieldInfo afterCutScoreWithMultiplierField = typeof(AfterCutScoreBuffer).GetField("_afterCutScoreWithMultiplier", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        /// private int AfterCutScoreBuffer#_multiplier
        private FieldInfo afterCutScoreBufferMultiplierField = typeof(AfterCutScoreBuffer).GetField("_multiplier", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        /// private static LevelCompletionResults.Rank LevelCompletionResults.GetRankForScore(int score, int maxPossibleScore)
        private MethodInfo getRankForScoreMethod = typeof(LevelCompletionResults).GetMethod("GetRankForScore", BindingFlags.NonPublic | BindingFlags.Static);

        private void SceneManagerOnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            data.scene = newScene.name;

            if (newScene.name == "MenuCore")
            {
                // Menu
                data.scene = "Menu";

                Gamemode.Init();

                // TODO: get the current song, mode and mods while in menu
                data.ResetMapInfo();

                data.ResetPerformance();

                // Release references for AfterCutScoreBuffers that don't resolve due to player leaving the map before finishing.
                noteCutMapping.Clear();

                data.StatusChange(ChangedProperties.AllButNoteCut, "menu");
            }
            else if (newScene.name == "GameCore")
            {
                // In game
                data.scene = "Song";

                gamePauseManager = FindFirstOrDefault<GamePauseManager>();
                scoreController = FindFirstOrDefault<ScoreController>();
                gameplayManager = FindFirstOrDefault<StandardLevelGameplayManager>();
                beatmapObjectCallbackController = FindFirstOrDefault<BeatmapObjectCallbackController>();
                gameplayModifiersSO = FindFirstOrDefault<GameplayModifiersModelSO>();
                audioTimeSyncController = FindFirstOrDefault<AudioTimeSyncController>();
                playerHeadAndObstacleInteraction = FindFirstOrDefault<PlayerHeadAndObstacleInteraction>();
                gameEnergyCounter = FindFirstOrDefault<GameEnergyCounter>();

                gameplayCoreSceneSetupData = Plugin.LevelData.GameplayCoreSceneSetupData;

                // Register event listeners
                // private GameEvent GamePauseManager#_gameDidPauseSignal
                AddSubscriber(gamePauseManager, "_gameDidPauseSignal", OnGamePause);
                // private GameEvent GamePauseManager#_gameDidResumeSignal
                AddSubscriber(gamePauseManager, "_gameDidResumeSignal", OnGameResume);
                // public ScoreController#noteWasCutEvent<NoteData, NoteCutInfo, int multiplier> // called after AfterCutScoreBuffer is created
                scoreController.noteWasCutEvent += OnNoteWasCut;
                // public ScoreController#noteWasMissedEvent<NoteData, int multiplier>
                scoreController.noteWasMissedEvent += OnNoteWasMissed;
                // public ScoreController#scoreDidChangeEvent<int> // score
                scoreController.scoreDidChangeEvent += OnScoreDidChange;
                // public ScoreController#comboDidChangeEvent<int> // combo
                scoreController.comboDidChangeEvent += OnComboDidChange;
                // public ScoreController#multiplierDidChangeEvent<int, float> // multiplier, progress [0..1]
                scoreController.multiplierDidChangeEvent += OnMultiplierDidChange;
                // private GameEvent GameplayManager#_levelFinishedSignal
                AddSubscriber(gameplayManager, "_levelFinishedSignal", OnLevelFinished);
                // private GameEvent GameplayManager#_levelFailedSignal
                AddSubscriber(gameplayManager, "_levelFailedSignal", OnLevelFailed);
                // public event Action<BeatmapEventData> BeatmapObjectCallbackController#beatmapEventDidTriggerEvent
                beatmapObjectCallbackController.beatmapEventDidTriggerEvent += OnBeatmapEventDidTrigger;

                IDifficultyBeatmap diff = gameplayCoreSceneSetupData.difficultyBeatmap;
                IBeatmapLevel level = diff.level;

                data.partyMode = Gamemode.IsPartyActive;
                data.mode = Gamemode.GameMode;

                GameplayModifiers gameplayModifiers = gameplayCoreSceneSetupData.gameplayModifiers;
                PlayerSpecificSettings playerSettings = gameplayCoreSceneSetupData.playerSpecificSettings;
                PracticeSettings practiceSettings = gameplayCoreSceneSetupData.practiceSettings;

                float songSpeedMul = gameplayModifiers.songSpeedMul;
                if (practiceSettings != null) songSpeedMul = practiceSettings.songSpeedMul;
                float modifierMultiplier = gameplayModifiersSO.GetTotalMultiplier(gameplayModifiers);

                data.songName = level.songName;
                data.songSubName = level.songSubName;
                data.songAuthorName = level.songAuthorName;
                data.levelAuthorName = level.levelAuthorName;
                data.songBPM = level.beatsPerMinute;
                data.noteJumpSpeed = diff.noteJumpMovementSpeed;
                data.songHash = level.levelID.Substring(0, Math.Min(32, level.levelID.Length));
                data.songTimeOffset = (long)(level.songTimeOffset * 1000f / songSpeedMul);
                data.length = (long)(level.beatmapLevelData.audioClip.length * 1000f / songSpeedMul);
                data.start = GetCurrentTime() - (long)(audioTimeSyncController.songTime * 1000f / songSpeedMul);
                if (practiceSettings != null) data.start -= (long)(practiceSettings.startSongTime * 1000f / songSpeedMul);
                data.paused = 0;
                data.difficulty = diff.difficulty.Name();
                data.notesCount = diff.beatmapData.notesCount;
                data.bombsCount = diff.beatmapData.bombsCount;
                data.obstaclesCount = diff.beatmapData.obstaclesCount;
                data.environmentName = level.environmentSceneInfo.sceneName;
                data.maxScore = ScoreController.GetScoreForGameplayModifiersScoreMultiplier(ScoreController.MaxScoreForNumberOfNotes(diff.beatmapData.notesCount), modifierMultiplier);
                data.maxRank = RankModel.MaxRankForGameplayModifiers(gameplayModifiers, gameplayModifiersSO).ToString();

                try
                {
                    // From https://support.unity3d.com/hc/en-us/articles/206486626-How-can-I-get-pixels-from-unreadable-textures-
                    var texture = level.coverImage.texture;
                    var active = RenderTexture.active;
                    var temporary = RenderTexture.GetTemporary(
                        texture.width,
                        texture.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear
                    );

                    Graphics.Blit(texture, temporary);
                    RenderTexture.active = temporary;

                    var cover = new Texture2D(texture.width, texture.height);
                    cover.ReadPixels(new Rect(0, 0, temporary.width, temporary.height), 0, 0);
                    cover.Apply();

                    RenderTexture.active = active;
                    RenderTexture.ReleaseTemporary(temporary);

                    data.songCover = Convert.ToBase64String(
                        ImageConversion.EncodeToPNG(cover)
                    );
                }
                catch
                {
                    data.songCover = null;
                }

                data.ResetPerformance();

                data.modifierMultiplier = modifierMultiplier;
                data.songSpeedMultiplier = songSpeedMul;
                data.batteryLives = gameEnergyCounter.batteryLives;

                data.modObstacles = gameplayModifiers.enabledObstacleType.ToString();
                data.modInstaFail = gameplayModifiers.instaFail;
                data.modNoFail = gameplayModifiers.noFail;
                data.modBatteryEnergy = gameplayModifiers.batteryEnergy;
                data.modDisappearingArrows = gameplayModifiers.disappearingArrows;
                data.modNoBombs = gameplayModifiers.noBombs;
                data.modSongSpeed = gameplayModifiers.songSpeed.ToString();
                data.modNoArrows = gameplayModifiers.noArrows;
                data.modGhostNotes = gameplayModifiers.ghostNotes;
                data.modFailOnSaberClash = gameplayModifiers.failOnSaberClash;
                data.modStrictAngles = gameplayModifiers.strictAngles;
                data.modFastNotes = gameplayModifiers.fastNotes;

                data.staticLights = playerSettings.staticLights;
                data.leftHanded = playerSettings.leftHanded;
                data.swapColors = playerSettings.swapColors;
                data.playerHeight = playerSettings.playerHeight;
                data.disableSFX = playerSettings.disableSFX;
                data.noHUD = playerSettings.noTextsAndHuds;
                data.advancedHUD = playerSettings.advancedHud;

                data.StatusChange(ChangedProperties.AllButNoteCut, "songStart");
            }
        }
        public void OnApplicationStart()
        {
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
        }
        public void OnApplicationQuit()
        {
            SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;

            if (gamePauseManager != null)
            {
                RemoveSubscriber(gamePauseManager, "_gameDidPauseSignal", OnGamePause);
                RemoveSubscriber(gamePauseManager, "_gameDidResumeSignal", OnGameResume);
            }

            if (scoreController != null)
            {
                scoreController.noteWasCutEvent -= OnNoteWasCut;
                scoreController.noteWasMissedEvent -= OnNoteWasMissed;
                scoreController.scoreDidChangeEvent -= OnScoreDidChange;
                scoreController.comboDidChangeEvent -= OnComboDidChange;
                scoreController.multiplierDidChangeEvent -= OnMultiplierDidChange;
            }

            if (gameplayManager != null)
            {
                RemoveSubscriber(gameplayManager, "_levelFinishedSignal", OnLevelFinished);
                RemoveSubscriber(gameplayManager, "_levelFailedSignal", OnLevelFailed);
            }

            if (beatmapObjectCallbackController != null)
            {
                beatmapObjectCallbackController.beatmapEventDidTriggerEvent -= OnBeatmapEventDidTrigger;
            }
        }

        private void OnGamePause()
        {
            data.paused = GetCurrentTime();
            data.StatusChange(ChangedProperties.Beatmap, "pause");
        }

        private void OnGameResume()
        {
            data.start = GetCurrentTime() - (long)(audioTimeSyncController.songTime * 1000f / data.songSpeedMultiplier);
            data.paused = 0;
            data.StatusChange(ChangedProperties.Beatmap, "resume");
        }

        private void OnNoteWasCut(NoteData noteData, NoteCutInfo info, int multiplier)
        {
            SetNoteCutStatus(noteData, info);

            int score, afterScore, cutDistanceScore;

            ScoreController.ScoreWithoutMultiplier(info, null, out score, out afterScore, out cutDistanceScore);

            data.initialScore = score;
            data.finalScore = -1;
            data.cutMultiplier = multiplier;

            if (noteData.noteType == NoteType.Bomb)
            {
                data.passedBombs++;
                data.hitBombs++;

                data.StatusChange(ChangedProperties.PerformanceAndNoteCut, "bombCut");
            }
            else
            {
                data.passedNotes++;

                if (info.allIsOK)
                {
                    data.hitNotes++;

                    data.StatusChange(ChangedProperties.PerformanceAndNoteCut, "noteCut");
                }
                else
                {
                    data.missedNotes++;

                    data.StatusChange(ChangedProperties.PerformanceAndNoteCut, "noteMissed");
                }
                data.UpdateAccuracy();
            }

            List<AfterCutScoreBuffer> list = (List<AfterCutScoreBuffer>)afterCutScoreBuffersField.GetValue(scoreController);

            foreach (AfterCutScoreBuffer acsb in list)
            {
                if (noteCutInfoField.GetValue(acsb) == info)
                {
                    noteCutMapping.Add(info, noteData);

                    acsb.didFinishEvent += OnNoteWasFullyCut;
                    break;
                }
            }
        }

        private void OnNoteWasFullyCut(AfterCutScoreBuffer buffer)
        {
            int score, afterScore, cutDistanceScore;

            NoteCutInfo info = (NoteCutInfo)noteCutInfoField.GetValue(buffer);
            NoteData noteData = noteCutMapping[info];

            noteCutMapping.Remove(info);
            SetNoteCutStatus(noteData, info);

            ScoreController.ScoreWithoutMultiplier(info, null, out score, out afterScore, out cutDistanceScore);
            int multiplier = (int)afterCutScoreBufferMultiplierField.GetValue(buffer);
            afterScore = (int)afterCutScoreWithMultiplierField.GetValue(buffer) / multiplier;

            data.initialScore = score;
            data.finalScore = score + afterScore;
            data.multiplier = multiplier;

            data.StatusChange(ChangedProperties.PerformanceAndNoteCut, "noteFullyCut");

            buffer.didFinishEvent -= OnNoteWasFullyCut;
        }

        private void OnNoteWasMissed(NoteData noteData, int multiplier)
        {
            data.batteryEnergy = gameEnergyCounter.batteryEnergy;

            if (noteData.noteType == NoteType.Bomb)
            {
                data.passedBombs++;

                data.StatusChange(ChangedProperties.Performance, "bombMissed");
            }
            else
            {
                data.passedNotes++;
                data.missedNotes++;

                data.StatusChange(ChangedProperties.Performance, "noteMissed");
            }
        }

        private void OnScoreDidChange(int scoreBeforeMultiplier)
        {
            data.score = ScoreController.GetScoreForGameplayModifiersScoreMultiplier(scoreBeforeMultiplier, data.modifierMultiplier);

            int currentMaxScoreBeforeMultiplier = ScoreController.MaxScoreForNumberOfNotes(data.passedNotes);
            data.currentMaxScore = ScoreController.GetScoreForGameplayModifiersScoreMultiplier(currentMaxScoreBeforeMultiplier, data.modifierMultiplier);

            RankModel.Rank rank = RankModel.GetRankForScore(scoreBeforeMultiplier, data.score, currentMaxScoreBeforeMultiplier, data.currentMaxScore);
            data.rank = RankModel.GetRankName(rank);

            data.StatusChange(ChangedProperties.Performance, "scoreChanged");
        }

        private void OnComboDidChange(int combo)
        {
            data.combo = combo;
            data.maxCombo = scoreController.maxCombo;

            data.StatusChange(ChangedProperties.Performance, "comboChange");
        }

        private void OnMultiplierDidChange(int multiplier, float multiplierProgress)
        {
            data.multiplier = multiplier;
            data.multiplierProgress = multiplierProgress;

            data.StatusChange(ChangedProperties.Performance, "multiplierChange");
        }

        private void OnLevelFinished()
        {
            data.StatusChange(ChangedProperties.Performance, "finished");
        }

        private void OnLevelFailed()
        {
            data.StatusChange(ChangedProperties.Performance, "failed");
        }

        private void OnBeatmapEventDidTrigger(BeatmapEventData beatmapEventData)
        {
            data.beatmapEventType = beatmapEventData.type;
            data.beatmapEventValue = beatmapEventData.value;

            data.StatusChange(ChangedProperties.BeatmapEvent, "beatmapEvent");
        }

        private static T FindFirstOrDefault<T>() where T : UnityEngine.Object
        {
            T obj = Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();
            if (obj == null)
            {
                Log("Couldn't find " + typeof(T).FullName);
                throw new InvalidOperationException("Couldn't find " + typeof(T).FullName);
            }
            return obj;
        }

        private void AddSubscriber(object obj, string field, Action action)
        {
            Type t = obj.GetType();
            FieldInfo gameEventField = t.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (gameEventField == null)
            {
                Log("Can't subscribe to " + t.Name + "." + field);
                return;
            }

            MethodInfo methodInfo = gameEventField.FieldType.GetMethod("Subscribe");
            methodInfo.Invoke(gameEventField.GetValue(obj), new object[] { action });
        }

        private void RemoveSubscriber(object obj, string field, Action action)
        {
            Type t = obj.GetType();
            FieldInfo gameEventField = t.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (gameEventField == null)
            {
                Log("Can't unsubscribe from " + t.Name + "." + field);
                return;
            }

            MethodInfo methodInfo = gameEventField.FieldType.GetMethod("Unsubscribe");
            methodInfo.Invoke(gameEventField.GetValue(obj), new object[] { action });
        }

        private void SetNoteCutStatus(NoteData noteData, NoteCutInfo noteCutInfo)
        {
            data.noteID = noteData.id;
            data.noteType = noteData.noteType.ToString();
            data.noteCutDirection = noteData.cutDirection.ToString();
            data.noteLine = noteData.lineIndex;
            data.noteLayer = (int)noteData.noteLineLayer;
            data.speedOK = noteCutInfo.speedOK;
            data.directionOK = noteCutInfo.directionOK;
            data.saberTypeOK = noteCutInfo.saberTypeOK;
            data.wasCutTooSoon = noteCutInfo.wasCutTooSoon;
            data.saberSpeed = noteCutInfo.saberSpeed;
            data.saberDirX = noteCutInfo.saberDir[0];
            data.saberDirY = noteCutInfo.saberDir[1];
            data.saberDirZ = noteCutInfo.saberDir[2];
            data.saberType = noteCutInfo.saberType.ToString();
            data.swingRating = noteCutInfo.swingRating;
            data.timeDeviation = noteCutInfo.timeDeviation;
            data.cutDirectionDeviation = noteCutInfo.cutDirDeviation;
            data.cutPointX = noteCutInfo.cutPoint[0];
            data.cutPointY = noteCutInfo.cutPoint[1];
            data.cutPointZ = noteCutInfo.cutPoint[2];
            data.cutNormalX = noteCutInfo.cutNormal[0];
            data.cutNormalY = noteCutInfo.cutNormal[1];
            data.cutNormalZ = noteCutInfo.cutNormal[2];
            data.cutDistanceToCenter = noteCutInfo.cutDistanceToCenter;
        }

        private long GetCurrentTime()
        {
            return (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).Ticks / TimeSpan.TicksPerMillisecond);
        }

        private static void Log(string msg)
        {
            Utilities.Logger.Log(msg);
        }

        public void OnUpdate()
        {
            bool currentHeadInObstacle = false;

            if (playerHeadAndObstacleInteraction != null)
            {
                currentHeadInObstacle = playerHeadAndObstacleInteraction.intersectingObstacles.Count > 0;
            }

            if (!headInObstacle && currentHeadInObstacle)
            {
                headInObstacle = true;

                data.StatusChange(ChangedProperties.Performance, "obstacleEnter");
            }
            else if (headInObstacle && !currentHeadInObstacle)
            {
                headInObstacle = false;

                data.StatusChange(ChangedProperties.Performance, "obstacleExit");
            }
        }
    }
}
