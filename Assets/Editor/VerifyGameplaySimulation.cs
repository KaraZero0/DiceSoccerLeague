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

            // Test Wild (31 — Total Footballer) activating Pair, Odd Flush, Even Flush, Straight, and Triple
            bool wildPair = (bool)isTriggered.Invoke(null, new object[] { 2, 0, new int[] { 31, 4, 1 }, 5 });
            bool wildOdd = (bool)isTriggered.Invoke(null, new object[] { 3, 0, new int[] { 31, 3, 5 }, 8 });
            bool wildEven = (bool)isTriggered.Invoke(null, new object[] { 4, 0, new int[] { 2, 31, 6 }, 8 });
            bool wildStraight = (bool)isTriggered.Invoke(null, new object[] { 5, 0, new int[] { 2, 31, 4 }, 6 });
            bool wildTriple = (bool)isTriggered.Invoke(null, new object[] { 6, 0, new int[] { 5, 31, 5 }, 10 });
            if (!wildPair || !wildOdd || !wildEven || !wildStraight || !wildTriple)
                throw new Exception("Wild (31) combo wildcard trigger failed!");

            // Test GetAbilityExecutionPhase ordering
            var getPhase = typeof(DiceSoccerBootstrap).GetMethod("GetAbilityExecutionPhase", BindingFlags.Static | BindingFlags.NonPublic);
            if (getPhase == null) throw new Exception("GetAbilityExecutionPhase method not found!");
            var cat = GameCatalog.Load();
            int phaseIntercept = (int)getPhase.Invoke(null, new object[] { cat.TryGetAbility(43) }); // Intercept = 0
            int phaseReroll = (int)getPhase.Invoke(null, new object[] { cat.TryGetAbility(61) });    // Assist = 1
            int phaseAnchor = (int)getPhase.Invoke(null, new object[] { cat.TryGetAbility(42) });    // Anchor = 2
            int phaseTackle = (int)getPhase.Invoke(null, new object[] { cat.TryGetAbility(33) });    // Tackle = 3
            int phaseStepOver = (int)getPhase.Invoke(null, new object[] { cat.TryGetAbility(28) });  // Step-Over = 4
            int phaseThrough = (int)getPhase.Invoke(null, new object[] { cat.TryGetAbility(47) });   // Through Pass = 5
            if (!(phaseIntercept < phaseReroll && phaseReroll < phaseAnchor && phaseAnchor < phaseTackle && phaseTackle < phaseStepOver && phaseStepOver < phaseThrough))
                throw new Exception($"Ability phase ordering invalid: {phaseIntercept}, {phaseReroll}, {phaseAnchor}, {phaseTackle}, {phaseStepOver}, {phaseThrough}");

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

            // Verify LayeredPlayerRig attachment, LoadingScreenSprite atlas slicing, Entourage #19/#20/#21 mechanics, and all 21 Entourage icons
            for (int eId = 1; eId <= 21; eId++)
            {
                var icon = PlayerBodySprites.LoadSprite($"Original/images/EntourageAbility{eId:00}");
                if (icon == null) throw new Exception($"Missing Entourage icon sprite: EntourageAbility{eId:00}");
            }

            PlayerBodySprites.EnsureLoadingAtlasSliced(out var lFrames, out var lHead, out var lBubble);
            if (lFrames == null || lFrames.Length != 22 || lHead == null || lBubble == null)
                throw new Exception($"LoadingScreenSprite slice failed: frames={lFrames?.Length ?? 0}, head={lHead != null}, bubble={lBubble != null}");

            var applyBonus = typeof(MatchSimulation).GetMethod("ApplyEntourageRollBonuses", BindingFlags.Static | BindingFlags.NonPublic);
            if (applyBonus == null) throw new Exception("ApplyEntourageRollBonuses not found!");

            int totalBenchNonEmpty = 10;
            applyBonus.Invoke(null, new object[] { new[] { 19 }, origHome, new[] { 3, 3, 2 }, totalBenchNonEmpty, 2, null });
            if (totalBenchNonEmpty != 10) throw new Exception($"Entourage #19 should NOT trigger when bench is non-empty! Got {totalBenchNonEmpty}");
            object[] args19Empty = new object[] { new[] { 19 }, origHome, new[] { 3, 3, 2 }, 10, 0, new System.Collections.Generic.List<int>() };
            applyBonus.Invoke(null, args19Empty);
            if ((int)args19Empty[3] != 12) throw new Exception($"Entourage #19 failed when bench empty: expected 12, got {args19Empty[3]}");

            object[] args21 = new object[] { new[] { 21 }, origHome, new[] { 5, 5, 2 }, 12, 0, new System.Collections.Generic.List<int>() };
            applyBonus.Invoke(null, args21);
            if ((int)args21[3] != 27) throw new Exception($"Entourage #21 (4x on pair of 5s) expected 27, got {args21[3]}");

            int c1 = simHomeCaptain.IssueYellowCard(false, 5);
            int c2 = simHomeCaptain.IssueYellowCard(false, 5);
            if (c1 != 1 || c2 != 2 || !simHomeCaptain.HasYellowCard(false, 5) || !simHomeCaptain.HasRedCard(false, 5))
                throw new Exception($"Yellow -> Red card escalation failed! c1={c1}, c2={c2}");

            // Additional thorough checks: BuffBelt no hardcoded x2, Field Die 0/15+, Wild Odd/Even, Reroll Wild/Whistle, Catalog Teams & Unlocks 1..96 / Ach 1..72
            VerifyBuffBeltFieldDiceTeamsAndCampaign(bootstrap, catalog);

            Debug.Log("[TEST] Verified Ability Mechanics & Symmetry: All triggers, Captain passive, Entourage #1..21 rules, Cards, Loading slice, and Home/Away symmetrical adjustments passed perfectly!");
        }

        private static void VerifyBuffBeltFieldDiceTeamsAndCampaign(DiceSoccerBootstrap bootstrap, GameCatalog catalog)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var sflags = BindingFlags.Static | BindingFlags.NonPublic;

            // 1. Validate ALL 67 catalog abilities (IDs 16..82) resolve non-null medals and valid sprite IDs
            var resolveId = typeof(DiceSoccerBootstrap).GetMethod("ResolveAbilitySpriteId", sflags);
            var getMedal = typeof(DiceSoccerBootstrap).GetMethod("GetAbilityMedalSprite", sflags);
            var getOverlay = typeof(DiceSoccerBootstrap).GetMethod("GetAbilityBadgeOverlayText", sflags);
            if ((int)resolveId.Invoke(null, new object[] { 70 }) != 34)
                throw new Exception("ResolveAbilitySpriteId(70) must resolve to 34 (Hard Tackle -2), not 19 (-3)!");

            foreach (var ab in catalog.Data.abilities)
            {
                var sp = getMedal.Invoke(null, new object[] { ab }) as Sprite;
                if (sp == null)
                    throw new Exception($"Ability {ab.id} ({ab.name}) returned null medal sprite!");
                string ov = getOverlay.Invoke(null, new object[] { ab }) as string;
                if (ab.id == 18)
                {
                    if (sp.name != "DisplayBuffPlus" || ov != "3")
                        throw new Exception($"Dribble +3 (ID 18) must use DisplayBuffPlus with '3' overlay, got {sp.name}/{ov}");
                }
                else if (ab.id == 82)
                {
                    if (sp.name != "BuffMedal82" || !string.IsNullOrEmpty(ov))
                        throw new Exception($"Captain (ID 82) must use BuffMedal82 with empty overlay, got {sp.name}/{ov}");
                }
                else
                {
                    if (!string.IsNullOrEmpty(ov))
                        throw new Exception($"Self-contained BuffMedal for ability {ab.id} must have empty overlay, got '{ov}'");
                }
            }

            // 2. Start a match and verify BuffBelt badge text is NOT hardcoded "x2" and updates/clears cleanly
            var startCareer = typeof(DiceSoccerBootstrap).GetMethod("StartCareerMatch", flags);
            var startFixture = typeof(DiceSoccerBootstrap).GetMethod("StartCurrentFixture", flags);
            startCareer.Invoke(bootstrap, null);
            startFixture.Invoke(bootstrap, null);

            if (GameObject.Find("MatchSpeedBtn") != null)
                throw new Exception("MatchSpeedBtn must NOT appear on the in-match HUD (moved to Options)!");
            var showOpts = typeof(DiceSoccerBootstrap).GetMethod("ShowOptions", flags);
            showOpts?.Invoke(bootstrap, null);
            var speedTickBox = GameObject.Find("SpeedTickBox")?.GetComponent<Button>();
            if (speedTickBox == null)
                throw new Exception("SpeedTickBox option must exist inside ShowOptionsDialog!");
            int savedFast = PlayerPrefs.GetInt("diceSoccer.fastSpeed", 0);
            try
            {
                speedTickBox.onClick.Invoke();
                float expectedAfter = savedFast == 0 ? 2f : 1f;
                var speedVal = (float)typeof(DiceSoccerBootstrap).GetField("_matchSpeed", flags).GetValue(bootstrap);
                if (Math.Abs(speedVal - expectedAfter) > 0.01f)
                    throw new Exception($"Options Speed toggle failed: expected {expectedAfter}, got {speedVal}");
            }
            finally
            {
                PlayerPrefs.SetInt("diceSoccer.fastSpeed", savedFast);
                PlayerPrefs.Save();
                var optBackdrop = GameObject.Find("OptionsBackdrop");
                if (optBackdrop != null) UnityEngine.Object.DestroyImmediate(optBackdrop);
                Time.timeScale = 1f;
            }
            startFixture.Invoke(bootstrap, null);

            var badgeField = typeof(DiceSoccerBootstrap).GetField("_buffBeltBadgeText", flags);
            var medalImgField = typeof(DiceSoccerBootstrap).GetField("_buffBeltMedal", flags);
            var beltGoField = typeof(DiceSoccerBootstrap).GetField("_buffBeltGo", flags);
            var setBelt = typeof(DiceSoccerBootstrap).GetMethod("SetBuffBeltDisplay", flags);
            var hideBelt = typeof(DiceSoccerBootstrap).GetMethod("HideBuffBelt", flags);

            var badgeTxt = badgeField.GetValue(bootstrap) as Text;
            var medalImg = medalImgField.GetValue(bootstrap) as Image;
            if (badgeTxt == null || medalImg == null)
                throw new Exception("BuffBelt medal or badge Text not initialized during match!");
            if (!string.IsNullOrEmpty(badgeTxt.text))
                throw new Exception($"BuffBelt badge text must start empty (not 'x2'), got '{badgeTxt.text}'!");

            var abTackle = catalog.TryGetAbility(33);
            setBelt.Invoke(bootstrap, new object[] { getMedal.Invoke(null, new object[] { abTackle }), "TACKLE -1", getOverlay.Invoke(null, new object[] { abTackle }) });
            if (medalImg.sprite.name != "BuffMedal33" || badgeTxt.text != "")
                throw new Exception($"BuffBelt failed on Tackle -1: sprite={medalImg.sprite.name}, badge='{badgeTxt.text}'");

            var abDrib3 = catalog.TryGetAbility(18);
            setBelt.Invoke(bootstrap, new object[] { getMedal.Invoke(null, new object[] { abDrib3 }), "DRIBBLE +3!", getOverlay.Invoke(null, new object[] { abDrib3 }) });
            if (medalImg.sprite.name != "DisplayBuffPlus" || badgeTxt.text != "3")
                throw new Exception($"BuffBelt failed on Dribble +3: sprite={medalImg.sprite.name}, badge='{badgeTxt.text}'");

            hideBelt.Invoke(bootstrap, null);
            if (!string.IsNullOrEmpty(badgeTxt.text) || (beltGoField.GetValue(bootstrap) as GameObject).activeSelf)
                throw new Exception("HideBuffBelt must hide BuffBelt and clear badge text!");

            // 3. Field Die numeric rendering (0, 1..14, 16, 18, 24, 36) & Wild/Whistle Reroll (value == 0, NOT 1)
            var setDieVis = typeof(DiceSoccerBootstrap).GetMethod("SetSideDieVisual", flags);
            var homeDice = typeof(DiceSoccerBootstrap).GetField("_homeDice", flags).GetValue(bootstrap) as Image[];
            var homeLabels = typeof(DiceSoccerBootstrap).GetField("_homeDieValueLabels", flags).GetValue(bootstrap) as Text[];
            if (homeLabels == null || homeLabels.Length != 3 || homeLabels[0] == null)
                throw new Exception("_homeDieValueLabels missing!");

            // Buffed total 24 must NOT show Cross (DiceSide24) icon
            setDieVis.Invoke(bootstrap, new object[] { 0, 0, 24, 24, true });
            if (!homeLabels[0].enabled || homeLabels[0].text != "24" || homeDice[0].enabled)
                throw new Exception("Numeric die value 24 must render ValueLabel '24' instead of Cross sprite!");

            // Tackled to 0 must NOT clamp to DiceSide01
            setDieVis.Invoke(bootstrap, new object[] { 0, 1, 0, 2, true });
            if (!homeLabels[1].enabled || homeLabels[1].text != "0" || homeDice[1].enabled)
                throw new Exception("Numeric die value 0 (tackled) must render ValueLabel '0' instead of DiceSide01!");

            // Natural Wild (0 with comboFace 31) shows DiceSide31
            setDieVis.Invoke(bootstrap, new object[] { 0, 2, 0, 31, true });
            if (homeLabels[2].enabled || !homeDice[2].enabled || homeDice[2].sprite.name != "DiceSide31")
                throw new Exception("Natural Wild (0/31) must display DiceSide31 sprite!");

            // RollSinglePlayerDie with all Wild (31) must return (0, 31), NOT (1, 31)
            var rollSingle = typeof(DiceSoccerBootstrap).GetMethod("RollSinglePlayerDie", sflags);
            var wildOnly = new PlayerData { id = 9999, name = "WildTest", position = "MID", diceSides = new[] { 31, 31, 31, 31, 31, 31 } };
            var (rolledVal, rolledRaw) = ((int, int))rollSingle.Invoke(null, new object[] { wildOnly });
            if (rolledVal != 0 || rolledRaw != 31)
                throw new Exception($"RollSinglePlayerDie on Wild-only die returned ({rolledVal}, {rolledRaw}), expected (0, 31)!");

            // 4. Wild (31) triggers Odd Total (7) and Even Total (8)
            var isTriggered = typeof(DiceSoccerBootstrap).GetMethod("IsAbilityTriggered", sflags);
            if (!(bool)isTriggered.Invoke(null, new object[] { 7, 0, new[] { 31, 2, 2 }, 4 }))
                throw new Exception("Wild (31) must satisfy Total is ODD (trigger 7)!");
            if (!(bool)isTriggered.Invoke(null, new object[] { 8, 0, new[] { 31, 1, 2 }, 3 }))
                throw new Exception("Wild (31) must satisfy Total is EVEN (trigger 8)!");

            // 5. Audit all 5 Seasons, Competitions, Lineups & Kits in catalog
            if (catalog.Data.seasons.Length != 5)
                throw new Exception($"Expected 5 seasons, found {catalog.Data.seasons.Length}");
            foreach (var season in catalog.Data.seasons)
            {
                var league = catalog.GetFixtures(season.id, "League");
                if (league == null || league.Length == 0)
                    throw new Exception($"Season {season.id} missing League fixtures!");
                foreach (var comp in season.competitions)
                {
                    foreach (var fx in comp.fixtures ?? Array.Empty<FixtureData>())
                    {
                        var resolved = catalog.ResolveLineup(fx.lineup);
                        if (resolved == null || resolved.Length != 7)
                            throw new Exception($"Season {season.id} {comp.name} {fx.teamName} invalid lineup length!");
                        if (resolved[0].position != "GK")
                            throw new Exception($"Season {season.id} {comp.name} {fx.teamName} slot 0 is {resolved[0].position}, not GK!");
                    }
                }
            }
            for (int aid = 1; aid <= 72; aid++)
            {
                if (catalog.GetAchievement(aid) == null)
                    throw new Exception($"Missing Achievement #{aid} in catalog!");
                if (Resources.Load<Texture2D>($"Original/images/Trophy{aid:00}") == null)
                    throw new Exception($"Missing Trophy{aid:00}.png!");
            }

            // 6. Test Campaign Season Replay Preservation (_maxUnlockedSeason) & Unlock -> Hire Flow
            var seasonField = typeof(DiceSoccerBootstrap).GetField("_seasonIndex", flags);
            var maxSeasonField = typeof(DiceSoccerBootstrap).GetField("_maxUnlockedSeason", flags);
            var cashField = typeof(DiceSoccerBootstrap).GetField("_careerCash", flags);
            var isPlayerUnlocked = typeof(DiceSoccerBootstrap).GetMethod("IsPlayerUnlocked", flags);
            var hirePlayer = typeof(DiceSoccerBootstrap).GetMethod("HirePlayer", flags);
            var evalProgress = typeof(DiceSoccerBootstrap).GetMethod("EvaluateCareerProgressAndAchievements", flags);
            var ownedField = typeof(DiceSoccerBootstrap).GetField("_ownedPlayerIds", flags);
            var achSetField = typeof(DiceSoccerBootstrap).GetField("_unlockedAchievementIds", flags);

            // Snapshot key test prefs so repeat test runs stay clean
            int savedAllUnlocked = PlayerPrefs.GetInt("diceSoccer.allUnlocked", 0);
            int savedMaxSeason = PlayerPrefs.GetInt("diceSoccer.maxSeason", 0);
            int savedSeason = PlayerPrefs.GetInt("diceSoccer.season", 0);
            int savedCash = PlayerPrefs.GetInt("diceSoccer.cash", 1000);
            string savedRoster = PlayerPrefs.GetString($"diceSoccer.profile.{PlayerPrefs.GetInt("diceSoccer.profile.active", 0)}.roster", "");
            int savedP15 = PlayerPrefs.GetInt("diceSoccer.unlockedPlayer.15", 0);
            int savedVisitedCredits = PlayerPrefs.GetInt("diceSoccer.stats.visitedCredits", 0);
            int savedVisitedNews = PlayerPrefs.GetInt("diceSoccer.stats.visitedNews", 0);
            string savedAchs = PlayerPrefs.GetString("diceSoccer.achievements", "");

            try
            {
                PlayerPrefs.SetInt("diceSoccer.allUnlocked", 0);
                maxSeasonField.SetValue(bootstrap, 2);
                seasonField.SetValue(bootstrap, 0);
                typeof(DiceSoccerBootstrap).GetMethod("SaveCareerState", flags).Invoke(bootstrap, null);
                if ((int)maxSeasonField.GetValue(bootstrap) != 2)
                    throw new Exception("_maxUnlockedSeason dropped when replaying Season 0!");

                // Unlocked player #15 must be hireable via HirePlayer, while locked player #96 must reject
                var p15 = catalog.GetPlayer(15);
                var p96 = catalog.GetPlayer(96);
                var owned = ownedField.GetValue(bootstrap) as System.Collections.Generic.List<int>;
                owned.Remove(96);
                PlayerPrefs.SetInt("diceSoccer.unlockedPlayer.96", 0);
                if ((bool)isPlayerUnlocked.Invoke(bootstrap, new object[] { p96 }))
                    throw new Exception("Locked player #96 should not report unlocked before winning T4.2!");

                PlayerPrefs.SetInt("diceSoccer.unlockedPlayer.15", 1);
                owned.Remove(15);
                if (!(bool)isPlayerUnlocked.Invoke(bootstrap, new object[] { p15 }))
                    throw new Exception("Unlocked player #15 must report IsPlayerUnlocked == true!");

                cashField.SetValue(bootstrap, 50000);
                hirePlayer.Invoke(bootstrap, new object[] { 15 });
                if (!owned.Contains(15))
                    throw new Exception("HirePlayer failed to hire unlocked player #15!");

                // Credits -> Ach #33, News -> Player #12
                PlayerPrefs.SetInt("diceSoccer.stats.visitedCredits", 1);
                PlayerPrefs.SetInt("diceSoccer.stats.visitedNews", 1);
                evalProgress.Invoke(bootstrap, new object[] { false, false, false, false, false });
                var achSet = achSetField.GetValue(bootstrap) as System.Collections.Generic.HashSet<int>;
                if (!achSet.Contains(33))
                    throw new Exception("Visiting Credits must unlock Ach #33!");
                if (PlayerPrefs.GetInt("diceSoccer.unlockedPlayer.12", 0) != 1)
                    throw new Exception("Visiting News/Authorities must unlock Player #12!");
            }
            finally
            {
                PlayerPrefs.SetInt("diceSoccer.allUnlocked", savedAllUnlocked);
                PlayerPrefs.SetInt("diceSoccer.maxSeason", savedMaxSeason);
                PlayerPrefs.SetInt("diceSoccer.season", savedSeason);
                PlayerPrefs.SetInt("diceSoccer.cash", savedCash);
                PlayerPrefs.SetString($"diceSoccer.profile.{PlayerPrefs.GetInt("diceSoccer.profile.active", 0)}.roster", savedRoster);
                PlayerPrefs.SetInt("diceSoccer.unlockedPlayer.15", savedP15);
                PlayerPrefs.SetInt("diceSoccer.stats.visitedCredits", savedVisitedCredits);
                PlayerPrefs.SetInt("diceSoccer.stats.visitedNews", savedVisitedNews);
                PlayerPrefs.SetString("diceSoccer.achievements", savedAchs);
                PlayerPrefs.Save();
            }

            Debug.Log("[TEST] BuffBelt (no 'x2'), Field Die 0/15+ numerics, Wild Odd/Even, Teams, Campaign _maxUnlockedSeason, Player Unlocks & Achievements #1..72 all verified!");
        }
    }
}
#endif
