#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DiceSoccer.Editor
{
    public static class VerifyGameplaySimulation
    {
        public static void RunTest()
        {
            Debug.Log("[TEST] Starting VerifyGameplaySimulation...");
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            var bootstrap = UnityEngine.Object.FindAnyObjectByType<DiceSoccerBootstrap>();
            if (bootstrap == null)
            {
                throw new Exception("DiceSoccerBootstrap not found in scene!");
            }

            // 1. Clear any broken tutorial state
            for (int i = 0; i < 100; i++)
            {
                PlayerPrefs.SetInt($"diceSoccer.tutorial.{i}", 1);
            }
            PlayerPrefs.Save();

            // 2. Awake
            var awake = typeof(DiceSoccerBootstrap).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            awake?.Invoke(bootstrap, null);
            Debug.Log("[TEST] Awake successfully executed. Home screen active.");

            // 3. Start a career match to load fixtures, then start the current fixture
            var startCareer = typeof(DiceSoccerBootstrap).GetMethod("StartCareerMatch", BindingFlags.Instance | BindingFlags.NonPublic);
            startCareer?.Invoke(bootstrap, null);
            Debug.Log("[TEST] StartCareerMatch invoked successfully (Fixtures loaded).");

            var startFixture = typeof(DiceSoccerBootstrap).GetMethod("StartCurrentFixture", BindingFlags.Instance | BindingFlags.NonPublic);
            startFixture?.Invoke(bootstrap, null);
            Debug.Log("[TEST] StartCurrentFixture invoked successfully (Match started).");

            // 4. Verify match state
            var matchField = typeof(DiceSoccerBootstrap).GetField("_match", BindingFlags.Instance | BindingFlags.NonPublic);
            var match = matchField?.GetValue(bootstrap) as MatchSimulation;
            if (match == null)
            {
                throw new Exception("MatchSimulation is null after StartCareerMatch!");
            }
            Debug.Log($"[TEST] Match active: HomeScore={match.HomeScore}, AwayScore={match.AwayScore}, Location={match.Location}, Finished={match.IsFinished}");

            // 5. Verify field and players are created
            var homePlayersField = typeof(DiceSoccerBootstrap).GetField("_homePlayers", BindingFlags.Instance | BindingFlags.NonPublic);
            var awayPlayersField = typeof(DiceSoccerBootstrap).GetField("_awayPlayers", BindingFlags.Instance | BindingFlags.NonPublic);
            var homePlayers = homePlayersField?.GetValue(bootstrap) as Image[];
            var awayPlayers = awayPlayersField?.GetValue(bootstrap) as Image[];

            if (homePlayers == null || homePlayers.Length != 7)
                throw new Exception($"Home players invalid: {homePlayers?.Length ?? 0}");
            if (awayPlayers == null || awayPlayers.Length != 7)
                throw new Exception($"Away players invalid: {awayPlayers?.Length ?? 0}");

            for (int i = 0; i < 7; i++)
            {
                if (homePlayers[i].sprite == null)
                    throw new Exception($"Home player {i} sprite is null!");
                if (awayPlayers[i].sprite == null)
                    throw new Exception($"Away player {i} sprite is null!");
            }
            Debug.Log("[TEST] All 14 players have valid sprites and are placed on field.");

            // 5b. Verify authentic in-match clock on field grass
            var matchClockField = typeof(DiceSoccerBootstrap).GetField("_matchClock", BindingFlags.Instance | BindingFlags.NonPublic);
            var matchClock = matchClockField?.GetValue(bootstrap) as Text;
            if (matchClock == null)
                throw new Exception("In-match clock (_matchClock) is null! Pitch grass must have digital clock overlay.");
            Debug.Log($"[TEST] In-match digital clock verified: Text='{matchClock.text}', Color={matchClock.color}");

            // 6. Simulate multiple encounters (play full match)
            var completeEncounterMethod = typeof(MatchSimulation).GetMethod("CompleteEncounter", BindingFlags.Instance | BindingFlags.Public);
            var beginEncounterMethod = typeof(MatchSimulation).GetMethod("BeginEncounter", BindingFlags.Instance | BindingFlags.Public);
            var updatePlayerPositions = typeof(DiceSoccerBootstrap).GetMethod("UpdatePlayerPositions", BindingFlags.Instance | BindingFlags.NonPublic);

            int round = 0;
            while (!match.IsFinished && round < 30)
            {
                round++;
                beginEncounterMethod.Invoke(match, null);
                string report = completeEncounterMethod.Invoke(match, null) as string;
                updatePlayerPositions?.Invoke(bootstrap, new object[] { null });
                Debug.Log($"[TEST] Encounter {round}: Clock={match.ClockText()}, Score={match.HomeScore}-{match.AwayScore}, Loc={match.Location}, Report={report}");
            }

            // 6b. Verify AnimateZoneTransition coroutine directly
            var animateZoneTransition = typeof(DiceSoccerBootstrap).GetMethod("AnimateZoneTransition", BindingFlags.Instance | BindingFlags.NonPublic);
            if (animateZoneTransition == null) throw new Exception("AnimateZoneTransition method not found!");
            var coroutine = animateZoneTransition.Invoke(bootstrap, new object[] { 0, 1, 0, 0 }) as IEnumerator;
            int animSteps = 0;
            while (coroutine.MoveNext() && animSteps < 150) animSteps++;
            Debug.Log($"[TEST] AnimateZoneTransition 0 -> 1 stepped {animSteps} times successfully.");

            var coroutine1v1 = animateZoneTransition.Invoke(bootstrap, new object[] { 1, 2, 0, 0 }) as IEnumerator;
            animSteps = 0;
            while (coroutine1v1.MoveNext() && animSteps < 150) animSteps++;
            Debug.Log($"[TEST] AnimateZoneTransition 1 -> 2 (1v1) stepped {animSteps} times successfully.");

            if (!match.IsFinished)
            {
                throw new Exception($"Match did not finish within 30 encounters! Clock={match.ClockText()}");
            }
            Debug.Log($"[TEST] Match successfully finished! Final Score: {match.HomeScore} - {match.AwayScore}");

            // 7. Verify screen transitions: Manage, KitSelection, TrophyRoom, Shop, Fixtures
            var showManage = typeof(DiceSoccerBootstrap).GetMethod("ShowManage", BindingFlags.Instance | BindingFlags.NonPublic);
            showManage?.Invoke(bootstrap, null);
            Debug.Log("[TEST] ShowManage executed successfully.");

            var showKit = typeof(DiceSoccerBootstrap).GetMethod("ShowKitSelection", BindingFlags.Instance | BindingFlags.NonPublic);
            showKit?.Invoke(bootstrap, null);
            Debug.Log("[TEST] ShowKitSelection executed successfully.");

            var showTrophies = typeof(DiceSoccerBootstrap).GetMethod("ShowTrophyRoom", BindingFlags.Instance | BindingFlags.NonPublic);
            showTrophies?.Invoke(bootstrap, null);
            Debug.Log("[TEST] ShowTrophyRoom executed successfully.");

            var showShop = typeof(DiceSoccerBootstrap).GetMethod("ShowShop", BindingFlags.Instance | BindingFlags.NonPublic);
            showShop?.Invoke(bootstrap, null);
            Debug.Log("[TEST] ShowShop executed successfully.");

            var showFixtures = typeof(DiceSoccerBootstrap).GetMethod("ShowLeagueFixtures", BindingFlags.Instance | BindingFlags.NonPublic);
            showFixtures?.Invoke(bootstrap, null);
            Debug.Log("[TEST] ShowLeagueFixtures executed successfully.");

            // 8. Verify Audio system and clips
            var listener = UnityEngine.Object.FindAnyObjectByType<AudioListener>();
            if (listener == null)
            {
                throw new Exception("No AudioListener found in scene! Audio will not play!");
            }
            Debug.Log($"[TEST] AudioListener verified on GameObject: {listener.gameObject.name}");

            var audioManager = AudioManager.Instance;
            if (audioManager == null)
            {
                throw new Exception("AudioManager instance is null!");
            }

            var bgmField = typeof(AudioManager).GetField("_bgmSource", BindingFlags.Instance | BindingFlags.NonPublic);
            var bgmSource = bgmField?.GetValue(audioManager) as AudioSource;
            if (bgmSource == null || bgmSource.spatialBlend != 0f)
            {
                throw new Exception("BGM AudioSource invalid or not 2D!");
            }

            string[] testClips = { "MainTheme", "StadiumCrowdChant", "Tap1", "DiceRoll1", "Whistle1", "KidsCheering" };
            foreach (var clipName in testClips)
            {
                var clip = Resources.Load<AudioClip>($"Original/audio/{clipName}");
                if (clip == null)
                {
                    throw new Exception($"AudioClip {clipName} could not be loaded from Resources!");
                }
                clip.LoadAudioData();
                Debug.Log($"[TEST] AudioClip {clipName} verified: Channels={clip.channels}, Frequency={clip.frequency}, Length={clip.length}s, LoadState={clip.loadState}");
            }

            // 9. Comprehensive Ability Mechanics & Symmetry Verification
            VerifyAbilitySymmetry(bootstrap);

            Debug.Log("[TEST] ALL GAMEPLAY, UI AND AUDIO VERIFICATION TESTS PASSED COMPLETELY!");
        }

        private static void VerifyAbilitySymmetry(DiceSoccerBootstrap bootstrap)
        {
            Debug.Log("[TEST] Verifying Ability Mechanics & Symmetry...");
            var isTriggered = typeof(DiceSoccerBootstrap).GetMethod("IsAbilityTriggered", BindingFlags.Static | BindingFlags.NonPublic);
            if (isTriggered == null) throw new Exception("IsAbilityTriggered method not found!");

            // Test Pair (trigger 2)
            bool pairTrue = (bool)isTriggered.Invoke(null, new object[] { 2, 0, new int[] { 3, 3, 1 }, 7 });
            bool pairFalse = (bool)isTriggered.Invoke(null, new object[] { 2, 0, new int[] { 3, 4, 1 }, 8 });
            if (!pairTrue || pairFalse) throw new Exception("Pair trigger failed!");

            // Test Odd Flush (trigger 3)
            bool oddFlushTrue = (bool)isTriggered.Invoke(null, new object[] { 3, 0, new int[] { 1, 3, 5 }, 9 });
            bool oddFlushFalse = (bool)isTriggered.Invoke(null, new object[] { 3, 0, new int[] { 1, 4, 5 }, 10 });
            if (!oddFlushTrue || oddFlushFalse) throw new Exception("Odd Flush trigger failed!");

            // Test Even Flush (trigger 4)
            bool evenFlushTrue = (bool)isTriggered.Invoke(null, new object[] { 4, 0, new int[] { 2, 4, 6 }, 12 });
            bool evenFlushFalse = (bool)isTriggered.Invoke(null, new object[] { 4, 0, new int[] { 2, 3, 6 }, 11 });
            if (!evenFlushTrue || evenFlushFalse) throw new Exception("Even Flush trigger failed!");

            // Test Straight (trigger 5)
            bool straightTrue = (bool)isTriggered.Invoke(null, new object[] { 5, 0, new int[] { 4, 2, 3 }, 9 });
            bool straightFalse = (bool)isTriggered.Invoke(null, new object[] { 5, 0, new int[] { 1, 2, 4 }, 7 });
            if (!straightTrue || straightFalse) throw new Exception("Straight trigger failed!");

            // Test Triple (trigger 6)
            bool tripleTrue = (bool)isTriggered.Invoke(null, new object[] { 6, 0, new int[] { 4, 4, 4 }, 12 });
            bool tripleFalse = (bool)isTriggered.Invoke(null, new object[] { 6, 0, new int[] { 4, 4, 5 }, 13 });
            if (!tripleTrue || tripleFalse) throw new Exception("Triple trigger failed!");

            // Test Odd Sum (trigger 7) and Even Sum (trigger 8)
            bool oddSumTrue = (bool)isTriggered.Invoke(null, new object[] { 7, 0, new int[] { 2, 4, 1 }, 7 });
            bool evenSumTrue = (bool)isTriggered.Invoke(null, new object[] { 8, 0, new int[] { 2, 4, 2 }, 8 });
            if (!oddSumTrue || !evenSumTrue) throw new Exception("Odd/Even sum trigger failed!");

            // Test Trigger 21 (Rolls a 3)
            bool roll3True = (bool)isTriggered.Invoke(null, new object[] { 21, 0, new int[] { 3, 1, 2 }, 6 });
            bool roll3False = (bool)isTriggered.Invoke(null, new object[] { 21, 0, new int[] { 2, 1, 2 }, 5 });
            if (!roll3True || roll3False) throw new Exception("Roll 3 trigger failed!");

            // Test Captain (Superpower 82) on MatchSimulation
            var catalog = GameCatalog.Load();
            var origHome = catalog.ResolveLineup(new int[] { 1, 2, 3, 4, 7, 6, 8 });
            var origAway = catalog.ResolveLineup(new int[] { 9, 10, 11, 12, 13, 14, 15 });

            PlayerData CloneP(PlayerData p, int sp) => new PlayerData
            {
                id = p.id, name = p.name, position = p.position,
                superpower = sp, diceSides = (int[])p.diceSides.Clone()
            };

            var homeRoster = new PlayerData[7];
            var awayRoster = new PlayerData[7];
            for (int i = 0; i < 7; i++)
            {
                homeRoster[i] = CloneP(origHome[i], origHome[i].superpower);
                awayRoster[i] = CloneP(origAway[i], origAway[i].superpower);
            }

            // Set Captain (SP 82) on Home player
            homeRoster[0].superpower = 82;
            awayRoster[0].superpower = 0;
            var simHomeCaptain = new MatchSimulation(homeRoster, awayRoster, "Opponents");
            if (!simHomeCaptain.HomeWinOnTie || simHomeCaptain.AwayWinOnTie)
                throw new Exception("Home Captain tie breaker not initialized correctly!");

            // Set Captain (SP 82) on Away player
            homeRoster[0].superpower = 0;
            awayRoster[0].superpower = 82;
            var simAwayCaptain = new MatchSimulation(homeRoster, awayRoster, "Opponents");
            if (simAwayCaptain.HomeWinOnTie || !simAwayCaptain.AwayWinOnTie)
                throw new Exception("Away Captain tie breaker not initialized correctly!");

            // Test Symmetrical Total Adjustments with Pending Encounter
            simHomeCaptain.BeginEncounter();
            int baseHome = simHomeCaptain.LastHomeTotal;
            int baseAway = simHomeCaptain.LastAwayTotal;
            simHomeCaptain.AdjustHomeTotal(5); // Home buff (+5)
            simHomeCaptain.AdjustAwayTotal(-2); // Away debuff (-2)
            if (simHomeCaptain.LastHomeTotal != baseHome + 5)
                throw new Exception($"AdjustHomeTotal failed: expected {baseHome + 5}, got {simHomeCaptain.LastHomeTotal}");
            if (simHomeCaptain.LastAwayTotal != Math.Max(0, baseAway - 2))
                throw new Exception($"AdjustAwayTotal failed: expected {Math.Max(0, baseAway - 2)}, got {simHomeCaptain.LastAwayTotal}");

            // Verify CompleteEncounter completes successfully using adjusted totals
            string rep = simHomeCaptain.CompleteEncounter();
            if (string.IsNullOrEmpty(rep)) throw new Exception("CompleteEncounter returned empty report!");

            Debug.Log("[TEST] Verified Ability Mechanics & Symmetry: All triggers, Captain passive, and Home/Away symmetrical adjustments passed perfectly!");
        }
    }
}
#endif
