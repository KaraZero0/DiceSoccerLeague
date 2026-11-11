#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DiceSoccer.Editor
{
    public static class CaptureAllScreenshots
    {
        private static string OutputDir = "/home/kara/.gemini/antigravity/brain/5186e7b4-ece4-4c5a-9816-86c193f29215/screenshots";

        public static void Run()
        {
            Debug.Log("[CAPTURE] Starting automated screen navigation and capture...");
            Directory.CreateDirectory(OutputDir);

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            var bootstrap = UnityEngine.Object.FindAnyObjectByType<DiceSoccerBootstrap>();
            if (bootstrap == null)
            {
                throw new Exception("DiceSoccerBootstrap not found in scene!");
            }

            // Ensure unlocked for full screen navigation
            PlayerPrefs.SetInt("diceSoccer.allUnlocked", 1);
            PlayerPrefs.SetInt("diceSoccer.fullscreen", 0);
            for (int i = 0; i < 100; i++)
            {
                PlayerPrefs.SetInt($"diceSoccer.tutorial.{i}", 1);
            }
            PlayerPrefs.Save();

            // Setup Camera and RenderTexture
            var camGo = new GameObject("CaptureCam", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;

            int captureW = 640;
            int captureH = 960;
            var rt = new RenderTexture(captureW, captureH, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            // Awake
            var awake = typeof(DiceSoccerBootstrap).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            awake?.Invoke(bootstrap, null);

            var canvasField = typeof(DiceSoccerBootstrap).GetField("_canvas", BindingFlags.Instance | BindingFlags.NonPublic);
            var canvas = canvasField?.GetValue(bootstrap) as Canvas;
            var applySafeArea = typeof(DiceSoccerBootstrap).GetMethod("ApplySafeArea", BindingFlags.Instance | BindingFlags.NonPublic);

            Action<string> capture = (fileName) =>
            {
                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                    applySafeArea?.Invoke(bootstrap, null);
                }
                Canvas.ForceUpdateCanvases();

                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(captureW, captureH, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, captureW, captureH), 0, 0);
                tex.Apply();

                var bytes = tex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex);
                File.WriteAllBytes(Path.Combine(OutputDir, fileName), bytes);
                Debug.Log($"[CAPTURE] Saved {fileName} ({bytes.Length / 1024} KB)");
            };

            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                applySafeArea?.Invoke(bootstrap, null);
                Canvas.ForceUpdateCanvases();
            }

            // 1. HOME SCREEN
            var showHome = typeof(DiceSoccerBootstrap).GetMethod("ShowHome", BindingFlags.Instance | BindingFlags.NonPublic);
            showHome?.Invoke(bootstrap, null);
            capture("01a_home_screen.png");

            // 1b. Home News Modal
            var showNewsModal = typeof(DiceSoccerBootstrap).GetMethod("ShowNewsModal", BindingFlags.Instance | BindingFlags.NonPublic);
            showNewsModal?.Invoke(bootstrap, null);
            capture("01b_home_news_modal.png");

            // 1c. Home Handbook
            var safeAreaField = typeof(DiceSoccerBootstrap).GetField("_safeArea", BindingFlags.Instance | BindingFlags.NonPublic);
            var safeArea = safeAreaField?.GetValue(bootstrap) as RectTransform;
            var showHandbook = typeof(DiceSoccerBootstrap).GetMethod("ShowHandbookDialog", BindingFlags.Instance | BindingFlags.NonPublic);
            showHome?.Invoke(bootstrap, null);
            showHandbook?.Invoke(bootstrap, new object[] { safeArea });
            capture("01c_home_handbook_dialog.png");

            // 2. PROFILE SELECT
            var showProfileSelect = typeof(DiceSoccerBootstrap).GetMethod("ShowProfileSelect", BindingFlags.Instance | BindingFlags.NonPublic);
            showProfileSelect?.Invoke(bootstrap, null);
            capture("02a_profile_select.png");

            // 3. LEAGUE / SEASON SELECT
            var showLeagueSelect = typeof(DiceSoccerBootstrap).GetMethod("ShowLeagueSelect", BindingFlags.Instance | BindingFlags.NonPublic);
            var selSeasonField = typeof(DiceSoccerBootstrap).GetField("_selectedLeagueSeasonIndex", BindingFlags.Instance | BindingFlags.NonPublic);
            
            selSeasonField?.SetValue(bootstrap, 0);
            showLeagueSelect?.Invoke(bootstrap, null);
            capture("03a_season_0_training.png");

            selSeasonField?.SetValue(bootstrap, 1);
            showLeagueSelect?.Invoke(bootstrap, null);
            capture("03b_season_1_bootcamp.png");

            selSeasonField?.SetValue(bootstrap, 2);
            showLeagueSelect?.Invoke(bootstrap, null);
            capture("03c_season_2_turfwar.png");

            // 4. LEAGUE FIXTURES
            selSeasonField?.SetValue(bootstrap, 0);
            var startCareer = typeof(DiceSoccerBootstrap).GetMethod("StartCareerMatch", BindingFlags.Instance | BindingFlags.NonPublic);
            startCareer?.Invoke(bootstrap, null);
            capture("04a_fixtures_league.png");

            // 4b. Fixtures Opponent Preview
            var showLeagueFixtures = typeof(DiceSoccerBootstrap).GetMethod("ShowLeagueFixtures", BindingFlags.Instance | BindingFlags.NonPublic);
            var fixturesField = typeof(DiceSoccerBootstrap).GetField("_seasonFixtures", BindingFlags.Instance | BindingFlags.NonPublic);
            var fixtures = fixturesField?.GetValue(bootstrap) as FixtureData[];
            if (fixtures != null && fixtures.Length > 0)
            {
                var showOpponentPreview = typeof(DiceSoccerBootstrap).GetMethod("ShowOpponentPreview", BindingFlags.Instance | BindingFlags.NonPublic);
                showOpponentPreview?.Invoke(bootstrap, new object[] { fixtures[0] });
                capture("04b_fixtures_opponent_preview.png");
            }

            // 4c. Fixtures Tournament Modal (Locked / Unlocked)
            showLeagueFixtures?.Invoke(bootstrap, null);
            var showLockedModal = typeof(DiceSoccerBootstrap).GetMethod("ShowLockedTournamentModal", BindingFlags.Instance | BindingFlags.NonPublic);
            showLockedModal?.Invoke(bootstrap, new object[] { "Winter Cup" });
            capture("04c_fixtures_locked_tournament_modal.png");

            // 4d. Season 1 Fixtures (Shows all 3 competition tabs: League, T2, T1)
            var seasonIndexField = typeof(DiceSoccerBootstrap).GetField("_seasonIndex", BindingFlags.Instance | BindingFlags.NonPublic);
            seasonIndexField?.SetValue(bootstrap, 1);
            startCareer?.Invoke(bootstrap, null);
            capture("04d_season1_fixtures_3tabs.png");
            seasonIndexField?.SetValue(bootstrap, 0);

            // 5. TEAM MANAGEMENT
            var showManage = typeof(DiceSoccerBootstrap).GetMethod("ShowManage", BindingFlags.Instance | BindingFlags.NonPublic);
            showManage?.Invoke(bootstrap, null);
            capture("05a_manage_lineup.png");

            // 5b. Manage Player Card Inspector
            var showCardModal = typeof(DiceSoccerBootstrap).GetMethod("ShowCardModal", BindingFlags.Instance | BindingFlags.NonPublic);
            var catalogField = typeof(DiceSoccerBootstrap).GetField("_catalog", BindingFlags.Instance | BindingFlags.NonPublic);
            var catalog = catalogField?.GetValue(bootstrap) as GameCatalog;
            if (showCardModal != null && catalog != null)
            {
                var p1 = catalog.GetPlayer(1);
                showCardModal.Invoke(bootstrap, new object[] { p1, null, null, false });
                capture("05b_manage_player_card_modal.png");
            }

            // 6. SHOP
            var showShop = typeof(DiceSoccerBootstrap).GetMethod("ShowShop", BindingFlags.Instance | BindingFlags.NonPublic);
            showShop?.Invoke(bootstrap, null);
            capture("06a_shop_main.png");

            // 7. SETTINGS / OPTIONS
            var showOptions = typeof(DiceSoccerBootstrap).GetMethod("ShowOptions", BindingFlags.Instance | BindingFlags.NonPublic);
            showOptions?.Invoke(bootstrap, null);
            capture("07a_settings_options.png");

            // 8. MULTIPLAYER
            var showMultiplayer = typeof(DiceSoccerBootstrap).GetMethod("ShowMultiplayer", BindingFlags.Instance | BindingFlags.NonPublic);
            showMultiplayer?.Invoke(bootstrap, null);
            capture("08a_multiplayer_lobby.png");

            // 9. IN-GAME MATCH
            // Temporarily ensure 2 equipped Home Entourages so HUD icons are visible in 09a screenshot if none equipped
            var eqField = typeof(DiceSoccerBootstrap).GetField("_equippedEntourageIds", BindingFlags.Instance | BindingFlags.NonPublic);
            var eqList = eqField?.GetValue(bootstrap) as System.Collections.Generic.List<int>;
            bool injectedEnt = false;
            if (eqList != null && eqList.Count == 0)
            {
                eqList.Add(6);
                eqList.Add(20);
                injectedEnt = true;
            }
            var buildLoading = typeof(DiceSoccerBootstrap).GetMethod("BuildLoadingTipScreen", BindingFlags.Instance | BindingFlags.Public);
            buildLoading?.Invoke(bootstrap, new object[] { null });
            capture("01b_loading_tip.png");

            startCareer?.Invoke(bootstrap, null);
            var startFixture = typeof(DiceSoccerBootstrap).GetMethod("StartCurrentFixture", BindingFlags.Instance | BindingFlags.NonPublic);
            startFixture?.Invoke(bootstrap, null);
            capture("09a_match_kickoff.png");
            if (injectedEnt && eqList != null) eqList.Clear();

            // 9e. In-Match Substitution Bar Drawer
            var ownedField = typeof(DiceSoccerBootstrap).GetField("_ownedPlayerIds", BindingFlags.Instance | BindingFlags.NonPublic);
            var ownedList = ownedField?.GetValue(bootstrap) as System.Collections.Generic.List<int>;
            bool addedBenchTmp = false;
            if (ownedList != null && !ownedList.Contains(10)) { ownedList.Add(10); addedBenchTmp = true; }
            var showSubBar = typeof(DiceSoccerBootstrap).GetMethod("ShowInMatchSubstitutionBar", BindingFlags.Instance | BindingFlags.NonPublic);
            showSubBar?.Invoke(bootstrap, null);
            capture("09e_match_substitution_bar.png");
            if (addedBenchTmp && ownedList != null) ownedList.Remove(10);
            startFixture?.Invoke(bootstrap, null);

            // 9b. Match Encounter & Dice (step AnimateEncounter far enough to show dice & totals at 0.135/0.865, 0.24)
            var matchField = typeof(DiceSoccerBootstrap).GetField("_match", BindingFlags.Instance | BindingFlags.NonPublic);
            var match = matchField?.GetValue(bootstrap) as MatchSimulation;
            if (match != null)
            {
                var beginEncounter = typeof(MatchSimulation).GetMethod("BeginEncounter", BindingFlags.Instance | BindingFlags.Public);
                beginEncounter?.Invoke(match, null);
                var homeDiceRoots = typeof(DiceSoccerBootstrap).GetField("_homeDieRoots", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(bootstrap) as GameObject[];
                var awayDiceRoots = typeof(DiceSoccerBootstrap).GetField("_awayDieRoots", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(bootstrap) as GameObject[];
                var homeDice = typeof(DiceSoccerBootstrap).GetField("_homeDice", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(bootstrap) as Image[];
                var awayDice = typeof(DiceSoccerBootstrap).GetField("_awayDice", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(bootstrap) as Image[];
                var homePlayers = typeof(DiceSoccerBootstrap).GetField("_homePlayers", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(bootstrap) as Image[];
                var awayPlayers = typeof(DiceSoccerBootstrap).GetField("_awayPlayers", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(bootstrap) as Image[];
                var setDieFace = typeof(DiceSoccerBootstrap).GetMethod("SetDieFace", BindingFlags.Static | BindingFlags.NonPublic);
                float[] dys = { 0.25f, 0.50f, 0.75f };
                if (homePlayers != null && awayPlayers != null)
                {
                    for (int s = 4; s <= 6; s++) { homePlayers[s].gameObject.SetActive(false); awayPlayers[s].gameObject.SetActive(false); }
                }
                if (homeDiceRoots != null && awayDiceRoots != null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (homeDiceRoots[i] != null)
                        {
                            homeDiceRoots[i].SetActive(true);
                            homeDiceRoots[i].GetComponent<RectTransform>().anchorMin = homeDiceRoots[i].GetComponent<RectTransform>().anchorMax = new Vector2(0.27f, dys[i]);
                            if (homeDice != null && i < match.LastHomeFaces.Length) { homeDice[i].enabled = true; setDieFace?.Invoke(bootstrap, new object[] { homeDice[i], match.LastHomeFaces[i] }); }
                        }
                        if (awayDiceRoots[i] != null)
                        {
                            awayDiceRoots[i].SetActive(true);
                            awayDiceRoots[i].GetComponent<RectTransform>().anchorMin = awayDiceRoots[i].GetComponent<RectTransform>().anchorMax = new Vector2(0.73f, dys[i]);
                            if (awayDice != null && i < match.LastAwayFaces.Length) { awayDice[i].enabled = true; setDieFace?.Invoke(bootstrap, new object[] { awayDice[i], match.LastAwayFaces[i] }); }
                        }
                    }
                }
                var hTot = typeof(DiceSoccerBootstrap).GetField("_homeTotalText", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(bootstrap) as Text;
                var aTot = typeof(DiceSoccerBootstrap).GetField("_awayTotalText", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(bootstrap) as Text;
                if (hTot != null) { hTot.rectTransform.anchorMin = hTot.rectTransform.anchorMax = new Vector2(0.135f, 0.24f); hTot.text = match.LastHomeTotal.ToString(); hTot.enabled = true; }
                if (aTot != null) { aTot.rectTransform.anchorMin = aTot.rectTransform.anchorMax = new Vector2(0.865f, 0.24f); aTot.text = match.LastAwayTotal.ToString(); aTot.enabled = true; }
                capture("09b_match_encounter_active.png");

                // 9f. BuffBelt Active State & Buffed High-Numeric Die (verifies no fixed 'x2' & clean numeric 16/24)
                var setBuffBelt = typeof(DiceSoccerBootstrap).GetMethod("SetBuffBeltDisplay", BindingFlags.Instance | BindingFlags.NonPublic);
                var getAbilityMedal = typeof(DiceSoccerBootstrap).GetMethod("GetAbilityMedalSprite", BindingFlags.Static | BindingFlags.NonPublic);
                var setSideDie = typeof(DiceSoccerBootstrap).GetMethod("SetSideDieVisual", BindingFlags.Instance | BindingFlags.NonPublic);
                var abTackle = catalog?.TryGetAbility(33);
                if (abTackle != null && setBuffBelt != null && getAbilityMedal != null)
                {
                    var medalSp = getAbilityMedal.Invoke(null, new object[] { abTackle }) as Sprite;
                    setBuffBelt.Invoke(bootstrap, new object[] { medalSp, "TACKLE -1!", "" });
                }
                setSideDie?.Invoke(bootstrap, new object[] { 0, 0, 16, 16, true });
                setSideDie?.Invoke(bootstrap, new object[] { 1, 0, 0, 2, true });
                capture("09f_match_buffbelt_and_numerics.png");
                typeof(DiceSoccerBootstrap).GetMethod("HideBuffBelt", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(bootstrap, null);
            }

            // 9c. Half Time Modal
            var showHalfTime = typeof(DiceSoccerBootstrap).GetMethod("ShowHalfTimeModal", BindingFlags.Instance | BindingFlags.NonPublic);
            showHalfTime?.Invoke(bootstrap, null);
            capture("09c_match_half_time.png");

            // 9d. Game Over Modal
            var showGameOver = typeof(DiceSoccerBootstrap).GetMethod("ShowGameOverModal", BindingFlags.Instance | BindingFlags.NonPublic);
            showGameOver?.Invoke(bootstrap, null);
            capture("09d_match_game_over.png");

            // 10. CELEBRATION MODALS
            // 10a. Season Completion
            var showSeasonCompletion = typeof(DiceSoccerBootstrap).GetMethod("ShowSeasonCompletion", BindingFlags.Instance | BindingFlags.NonPublic);
            showSeasonCompletion?.Invoke(bootstrap, null);
            capture("10a_season_completion.png");

            // 10b. Season Promoted Modal
            var showPromotedModal = typeof(DiceSoccerBootstrap).GetMethod("ShowSeasonPromotedModal", BindingFlags.Instance | BindingFlags.NonPublic);
            showPromotedModal?.Invoke(bootstrap, new object[] { 1 });
            capture("10b_season_promoted_modal.png");

            // 10c. Tournament Victory Modal
            var showVictoryModal = typeof(DiceSoccerBootstrap).GetMethod("ShowTournamentVictoryModal", BindingFlags.Instance | BindingFlags.NonPublic);
            showVictoryModal?.Invoke(bootstrap, new object[] { "Winter Cup" });
            capture("10c_tournament_victory_modal.png");

            // 11. PIXEL 20:9 FULLSCREEN VERIFICATION (540x1200 = 1080x2400 aspect)
            PlayerPrefs.SetInt("diceSoccer.fullscreen", 1);
            PlayerPrefs.Save();
            int fsW = 540;
            int fsH = 1200;
            var fsRt = new RenderTexture(fsW, fsH, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = fsRt;

            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                applySafeArea?.Invoke(bootstrap, null);
                Canvas.ForceUpdateCanvases();
            }

            Action<string> captureFs = (fileName) =>
            {
                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                    applySafeArea?.Invoke(bootstrap, null);
                }
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = fsRt;
                var tex = new Texture2D(fsW, fsH, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, fsW, fsH), 0, 0);
                tex.Apply();
                var bytes = tex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex);
                File.WriteAllBytes(Path.Combine(OutputDir, fileName), bytes);
                Debug.Log($"[CAPTURE-FS] Saved {fileName} ({bytes.Length / 1024} KB)");
            };

            showHome?.Invoke(bootstrap, null);
            captureFs("pixel_fs_01a_home.png");

            showProfileSelect?.Invoke(bootstrap, null);
            captureFs("pixel_fs_02a_profile.png");

            startCareer?.Invoke(bootstrap, null);
            captureFs("pixel_fs_04a_fixtures.png");

            showManage?.Invoke(bootstrap, null);
            captureFs("pixel_fs_05a_manage.png");

            showShop?.Invoke(bootstrap, null);
            captureFs("pixel_fs_06a_shop.png");

            var showKit = typeof(DiceSoccerBootstrap).GetMethod("ShowKitSelection", BindingFlags.Instance | BindingFlags.NonPublic);
            showKit?.Invoke(bootstrap, null);
            captureFs("pixel_fs_06b_kit.png");

            showMultiplayer?.Invoke(bootstrap, null);
            captureFs("pixel_fs_08a_multiplayer.png");

            startCareer?.Invoke(bootstrap, null);
            startFixture?.Invoke(bootstrap, null);
            captureFs("pixel_fs_09a_match.png");

            showVictoryModal?.Invoke(bootstrap, new object[] { "Winter Cup" });
            captureFs("pixel_fs_10c_victory.png");

            var showTrophies = typeof(DiceSoccerBootstrap).GetMethod("ShowTrophyRoom", BindingFlags.Instance | BindingFlags.NonPublic);
            showTrophies?.Invoke(bootstrap, null);
            captureFs("pixel_fs_11a_trophies.png");

            var showEntourage = typeof(DiceSoccerBootstrap).GetMethod("ShowEntourage", BindingFlags.Instance | BindingFlags.NonPublic);
            showEntourage?.Invoke(bootstrap, null);
            captureFs("pixel_fs_12a_entourage.png");

            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            // Restore default classic fullscreen preference before exit
            PlayerPrefs.SetInt("diceSoccer.fullscreen", 0);
            PlayerPrefs.Save();

            RenderTexture.active = null;
            cam.targetTexture = null;
            fsRt.Release();
            UnityEngine.Object.DestroyImmediate(fsRt);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(camGo);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);

            Debug.Log("[CAPTURE] All screenshots successfully generated!");
        }
    }
}
#endif
