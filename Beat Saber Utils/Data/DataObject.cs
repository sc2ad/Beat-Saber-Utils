using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BS_Utils.Data
{
    public class DataObject
    {
        public string updateCause;

        public string scene = "Menu";
        public bool partyMode = false;
        public string mode = null;

        // Beatmap
        public string songName = null;
        public string songSubName = null;
        public string songAuthorName = null;
        public string levelAuthorName = null;
        public string songCover = null;
        public string songHash = null;
        public float songBPM;
        public float noteJumpSpeed;
        public long songTimeOffset = 0;
        public long length = 0;
        public long start = 0;
        public long paused = 0;
        public string difficulty = null;
        public int notesCount = 0;
        public int bombsCount = 0;
        public int obstaclesCount = 0;
        public int maxScore = 0;
        public string maxRank = "E";
        public string environmentName = null;

        // Performance
        public int score = 0;
        public int currentMaxScore = 0;
        public string rank = "E";
        public float accuracy = 100.0f;
        public int passedNotes = 0;
        public int hitNotes = 0;
        public int missedNotes = 0;
        public int lastNoteScore = 0;
        public int passedBombs = 0;
        public int hitBombs = 0;
        public int combo = 0;
        public int maxCombo = 0;
        public int multiplier = 0;
        public float multiplierProgress = 0;
        public int batteryEnergy = 1;

        // Note cut
        public int noteID = -1;
        public string noteType = null;
        public string noteCutDirection = null;
        public int noteLine = 0;
        public int noteLayer = 0;
        public bool speedOK = false;
        public bool directionOK = false;
        public bool saberTypeOK = false;
        public bool wasCutTooSoon = false;
        public int initialScore = -1;
        public int finalScore = -1;
        public int cutMultiplier = 0;
        public float saberSpeed = 0;
        public float saberDirX = 0;
        public float saberDirY = 0;
        public float saberDirZ = 0;
        public string saberType = null;
        public float swingRating = 0;
        public float timeDeviation = 0;
        public float cutDirectionDeviation = 0;
        public float cutPointX = 0;
        public float cutPointY = 0;
        public float cutPointZ = 0;
        public float cutNormalX = 0;
        public float cutNormalY = 0;
        public float cutNormalZ = 0;
        public float cutDistanceToCenter = 0;

        // Mods
        public float modifierMultiplier = 1f;
        public string modObstacles = "All";
        public bool modInstaFail = false;
        public bool modNoFail = false;
        public bool modBatteryEnergy = false;
        public int batteryLives = 1;
        public bool modDisappearingArrows = false;
        public bool modNoBombs = false;
        public string modSongSpeed = "Normal";
        public float songSpeedMultiplier = 1f;
        public bool modNoArrows = false;
        public bool modGhostNotes = false;
        public bool modFailOnSaberClash = false;
        public bool modStrictAngles = false;
        public bool modFastNotes = false;

        // Player settings
        public bool staticLights = false;
        public bool leftHanded = false;
        public bool swapColors = false;
        public float playerHeight = 17f;
        public bool disableSFX = false;
        public bool reduceDebris = false;
        public bool noHUD = false;
        public bool advancedHUD = false;

        // Beatmap event
        public BeatmapEventType beatmapEventType;
        public int beatmapEventValue = 0;

        public void UpdateAccuracy()
        {
            accuracy = score / (float)maxScore;
        }

        public void ResetMapInfo()
        {
            songName = null;
            songSubName = null;
            songAuthorName = null;
            levelAuthorName = null;
            songCover = null;
            songHash = null;
            songBPM = 0f;
            noteJumpSpeed = 0f;
            songTimeOffset = 0;
            length = 0;
            start = 0;
            paused = 0;
            difficulty = null;
            notesCount = 0;
            obstaclesCount = 0;
            maxScore = 0;
            maxRank = "E";
            environmentName = null;
        }

        public void ResetPerformance()
        {
            score = 0;
            currentMaxScore = 0;
            rank = "E";
            accuracy = 0;
            passedNotes = 0;
            hitNotes = 0;
            missedNotes = 0;
            lastNoteScore = 0;
            passedBombs = 0;
            hitBombs = 0;
            combo = 0;
            maxCombo = 0;
            multiplier = 0;
            multiplierProgress = 0;
            batteryEnergy = 1;
        }

        public void ResetNoteCut()
        {
            noteID = -1;
            noteType = null;
            noteCutDirection = null;
            speedOK = false;
            directionOK = false;
            saberTypeOK = false;
            wasCutTooSoon = false;
            initialScore = -1;
            finalScore = -1;
            cutMultiplier = 0;
            saberSpeed = 0;
            saberDirX = 0;
            saberDirY = 0;
            saberDirZ = 0;
            saberType = null;
            swingRating = 0;
            timeDeviation = 0;
            cutDirectionDeviation = 0;
            cutPointX = 0;
            cutPointY = 0;
            cutPointZ = 0;
            cutNormalX = 0;
            cutNormalY = 0;
            cutNormalZ = 0;
            cutDistanceToCenter = 0;
        }

        public static event Action<ChangedProperties, string> statusChange;

        public void StatusChange(ChangedProperties properties, string cause)
        {
            statusChange?.Invoke(properties, cause);
        }
    }
}
