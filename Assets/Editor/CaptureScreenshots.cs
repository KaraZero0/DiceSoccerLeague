#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiceSoccer.Editor
{
    public static class CaptureScreenshots
    {
        public static void Capture()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            var bootstrap = UnityEngine.Object.FindAnyObjectByType<DiceSoccerBootstrap>();
            if (bootstrap == null) return;

            // Mark tutorials as seen so they don't block screens
            for (int i = 0; i < 100; i++)
            {
                PlayerPrefs.SetInt($"diceSoccer.tutorial.{i}", 1);
            }
            PlayerPrefs.DeleteKey("diceSoccer.allUnlocked");
            PlayerPrefs.DeleteKey("diceSoccer.roster");
            PlayerPrefs.Save();

            var awakeMethod = typeof(DiceSoccerBootstrap).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            awakeMethod?.Invoke(bootstrap, null);

            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            int width = 640;
            int height = 960;

            var camGo = new GameObject("CaptureCamera", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            cam.orthographicSize = height / 2f;
            camGo.transform.position = new Vector3(0, 0, -100);

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;

            var rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;

            var safeAreaField = typeof(DiceSoccerBootstrap).GetField("_safeArea", BindingFlags.Instance | BindingFlags.NonPublic);
            var safeArea = safeAreaField?.GetValue(bootstrap) as RectTransform;
            if (safeArea != null)
            {
                safeArea.anchorMin = Vector2.zero;
                safeArea.anchorMax = Vector2.one;
                safeArea.offsetMin = Vector2.zero;
                safeArea.offsetMax = Vector2.zero;
            }

            Directory.CreateDirectory("Screenshots");

            void SaveScreen(string name)
            {
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                File.WriteAllBytes($"Screenshots/{name}.png", tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                Debug.Log($"Saved Screenshots/{name}.png");
            }

            void Call(string methodName, params object[] args)
            {
                var method = typeof(DiceSoccerBootstrap).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
                method?.Invoke(bootstrap, args);
            }

            // 1. Profile Select & New Profile
            try { Call("ShowProfileSelect"); SaveScreen("ProfileSelect"); } catch (Exception e) { Debug.LogWarning(e); }
            try { Call("ShowNewProfileModal", 1); SaveScreen("NewProfile"); } catch (Exception e) { Debug.LogWarning(e); }

            // 2. Home
            Call("ShowHome");
            SaveScreen("Home");

            // 3. League / Season Select Carousel
            try { Call("ShowLeagueSelect"); SaveScreen("LeagueSelect"); } catch (Exception e) { Debug.LogWarning(e); }

            // 4. Fixtures
            try { Call("StartCareerMatch"); SaveScreen("Fixtures"); } catch (Exception e) { Debug.LogWarning(e); }

            // 3. Match
            try
            {
                var catalog = GameCatalog.Load();
                var fixtures = catalog.GetFixtures(0, "League");
                var lineupField = typeof(DiceSoccerBootstrap).GetField("_lineupIds", BindingFlags.Instance | BindingFlags.NonPublic);
                var fixtureField = typeof(DiceSoccerBootstrap).GetField("_currentFixture", BindingFlags.Instance | BindingFlags.NonPublic);
                var matchField = typeof(DiceSoccerBootstrap).GetField("_match", BindingFlags.Instance | BindingFlags.NonPublic);
                
                var lineupIds = (int[])lineupField.GetValue(bootstrap);
                var homeLineup = catalog.ResolveLineup(lineupIds);
                fixtureField.SetValue(bootstrap, fixtures[0]);
                
                var match = new MatchSimulation(
                    homeLineup,
                    catalog.ResolveLineup(fixtures[0].lineup),
                    fixtures[0].teamName,
                    0,
                    new int[0],
                    fixtures[0].entourageLineup,
                    0,
                    fixtures[0].possessionBonus);
                matchField.SetValue(bootstrap, match);

                var showMatchScreenMethod = typeof(DiceSoccerBootstrap).GetMethod("ShowMatchScreen", BindingFlags.Instance | BindingFlags.NonPublic);
                showMatchScreenMethod?.Invoke(bootstrap, new object[] { homeLineup });
                SaveScreen("Match");

                var buffBeltField = typeof(DiceSoccerBootstrap).GetField("_buffBeltGo", BindingFlags.Instance | BindingFlags.NonPublic);
                var buffBeltGo = (GameObject)buffBeltField?.GetValue(bootstrap);
                if (buffBeltGo != null)
                {
                    buffBeltGo.SetActive(true);
                    SaveScreen("Match_BuffBelt");
                    buffBeltGo.SetActive(false);
                }

                // Capture Offensive Zone
                var locField = typeof(MatchSimulation).GetField("_location", BindingFlags.Instance | BindingFlags.NonPublic);
                var updatePositionsMethod = typeof(DiceSoccerBootstrap).GetMethod("UpdatePlayerPositions", BindingFlags.Instance | BindingFlags.NonPublic);
                var setBallMethod = typeof(DiceSoccerBootstrap).GetMethod("SetBallPosition", BindingFlags.Instance | BindingFlags.NonPublic);
                var pitchRootField = typeof(DiceSoccerBootstrap).GetField("_fieldPitchRoot", BindingFlags.Instance | BindingFlags.NonPublic);
                var pitchRoot = (RectTransform)pitchRootField?.GetValue(bootstrap);

                if (locField != null && pitchRoot != null)
                {
                    locField.SetValue(match, 1);
                    pitchRoot.anchoredPosition = new Vector2(-375f, 0f);
                    updatePositionsMethod?.Invoke(bootstrap, new object[] { null });
                    setBallMethod?.Invoke(bootstrap, new object[] { 1, 0, false, -1 });
                    SaveScreen("Match_Offensive");

                    locField.SetValue(match, 2);
                    pitchRoot.anchoredPosition = new Vector2(-640f, 0f);
                    updatePositionsMethod?.Invoke(bootstrap, new object[] { null });
                    setBallMethod?.Invoke(bootstrap, new object[] { 2, 0, false, -1 });
                    SaveScreen("Match_1v1");

                    // Reset back to midfield
                    locField.SetValue(match, 0);
                    pitchRoot.anchoredPosition = Vector2.zero;
                    updatePositionsMethod?.Invoke(bootstrap, new object[] { null });
                    setBallMethod?.Invoke(bootstrap, new object[] { 0, 0, false, -1 });
                }

                // 3b. Goal Celebration (media_1792577925152_01871c83.png)
                try
                {
                    var overlayField = typeof(DiceSoccerBootstrap).GetField("_overlayContainer", BindingFlags.Instance | BindingFlags.NonPublic);
                    var overlay = (RectTransform)overlayField?.GetValue(bootstrap);
                    var getSpriteMethod = typeof(DiceSoccerBootstrap).GetMethod("GetUiSprite", BindingFlags.Static | BindingFlags.NonPublic);
                    if (overlay != null && getSpriteMethod != null)
                    {
                        // Rotating sunburst pattern
                        var sunburstGo = new GameObject("GoalSunburst", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                        sunburstGo.transform.SetParent(overlay, false);
                        var sbRect = sunburstGo.GetComponent<RectTransform>();
                        sbRect.anchorMin = sbRect.anchorMax = new Vector2(0.5f, 0.52f);
                        sbRect.sizeDelta = new Vector2(1600f, 1600f);
                        var sbImg = sunburstGo.GetComponent<UnityEngine.UI.Image>();
                        sbImg.sprite = (Sprite)getSpriteMethod.Invoke(null, new object[] { "Original/sprites/GoalAnimationSS4/GoalSunburstPattern" });

                        // Multicolored drifting confetti
                        Color32[] confettiColors = new Color32[]
                        {
                            new Color32(235, 45, 45, 255), // Red
                            new Color32(40, 120, 245, 255), // Blue
                            new Color32(255, 60, 180, 255), // Pink/Magenta
                            new Color32(255, 220, 20, 255), // Yellow
                            new Color32(45, 200, 75, 255),  // Green
                            new Color32(160, 50, 230, 255)  // Purple
                        };
                        var confettiList = new System.Collections.Generic.List<GameObject>();
                        for (int i = 0; i < 35; i++)
                        {
                            int cId = (i % 3) + 1;
                            float x = UnityEngine.Random.Range(0.04f, 0.96f);
                            float y = UnityEngine.Random.Range(0.35f, 0.98f);
                            var cGo = new GameObject($"Confetti_{i}", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                            cGo.transform.SetParent(overlay, false);
                            var cRect = cGo.GetComponent<RectTransform>();
                            cRect.anchorMin = cRect.anchorMax = new Vector2(x, y);
                            cRect.sizeDelta = new Vector2(28f, 28f);
                            cRect.localEulerAngles = new Vector3(0, 0, UnityEngine.Random.Range(0f, 360f));
                            var cImg = cGo.GetComponent<UnityEngine.UI.Image>();
                            cImg.sprite = (Sprite)getSpriteMethod.Invoke(null, new object[] { $"Original/sprites/ConfettiSS/Confetti{cId}" });
                            cImg.color = confettiColors[i % confettiColors.Length];
                            cImg.preserveAspect = true;
                            confettiList.Add(cGo);
                        }

                        // Comic GOAL! explosion banner
                        var bannerGo = new GameObject("GoalBanner", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                        bannerGo.transform.SetParent(overlay, false);
                        var bnRect = bannerGo.GetComponent<RectTransform>();
                        bnRect.anchorMin = bnRect.anchorMax = new Vector2(0.5f, 0.54f);
                        bnRect.sizeDelta = new Vector2(580f, 475f);
                        var bnImg = bannerGo.GetComponent<UnityEngine.UI.Image>();
                        bnImg.sprite = (Sprite)getSpriteMethod.Invoke(null, new object[] { "Original/sprites/Prompt1/Goal" });
                        bnImg.preserveAspect = true;

                        // Naked Streaker running joyfully across bottom grass
                        var streakerGo = new GameObject("Streaker", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                        streakerGo.transform.SetParent(overlay, false);
                        var stRect = streakerGo.GetComponent<RectTransform>();
                        stRect.anchorMin = stRect.anchorMax = new Vector2(0.72f, 0.17f);
                        stRect.sizeDelta = new Vector2(120f, 175f);
                        var stImg = streakerGo.GetComponent<UnityEngine.UI.Image>();
                        stImg.sprite = (Sprite)getSpriteMethod.Invoke(null, new object[] { "Original/sprites/GoalAnimationSS4/Streaker01" });
                        stImg.preserveAspect = true;

                        // Bring top scoreboard to front and update score to 1 - 0
                        var homeScoreField = typeof(MatchSimulation).GetField("_homeScore", BindingFlags.Instance | BindingFlags.NonPublic);
                        homeScoreField?.SetValue(match, 1);
                        var scoreField = typeof(DiceSoccerBootstrap).GetField("_matchScore", BindingFlags.Instance | BindingFlags.NonPublic);
                        var scoreText = (UnityEngine.UI.Text)scoreField?.GetValue(bootstrap);
                        if (scoreText != null) scoreText.text = "1 - 0";
                        var scoreboardField = typeof(DiceSoccerBootstrap).GetField("_matchScoreboard", BindingFlags.Instance | BindingFlags.NonPublic);
                        var scoreboardImg = (UnityEngine.UI.Image)scoreboardField?.GetValue(bootstrap);
                        if (scoreboardImg != null) scoreboardImg.transform.SetAsLastSibling();

                        SaveScreen("Goal");

                        UnityEngine.Object.DestroyImmediate(streakerGo);
                        UnityEngine.Object.DestroyImmediate(bannerGo);
                        foreach (var c in confettiList) UnityEngine.Object.DestroyImmediate(c);
                        UnityEngine.Object.DestroyImmediate(sunburstGo);
                    }
                }
                catch (Exception e) { Debug.LogWarning(e); }

                // 4. Pause Menu
                Call("ShowPauseMenu");
                SaveScreen("PauseMenu");

                // Resume
                Call("ResumeFromPause");
            }
            catch (Exception e) { Debug.LogWarning(e); }

            // 5. Manage
            try { Call("ShowManage"); SaveScreen("Manage"); } catch (Exception e) { Debug.LogWarning(e); }

            // 5b. Kit Selection
            try { Call("ShowKitSelection"); SaveScreen("KitSelection"); } catch (Exception e) { Debug.LogWarning(e); }

            // 5c. Player Picker / Deck Roster (MID tab matching media_1792421110155_320ad894.png)
            try
            {
                var deckFilterField = typeof(DiceSoccerBootstrap).GetField("_deckCategoryFilter", BindingFlags.Instance | BindingFlags.NonPublic);
                deckFilterField?.SetValue(bootstrap, 1);
                Call("ShowPlayerPicker", -1);
                SaveScreen("PlayerPicker");
            }
            catch (Exception e) { Debug.LogWarning(e); }

            // 6. Hiring
            try { Call("ShowHiring"); SaveScreen("Hiring"); } catch (Exception e) { Debug.LogWarning(e); }

            // 7. Shop
            try
            {
                var shopTabField = typeof(DiceSoccerBootstrap).GetField("_shopTab", BindingFlags.Instance | BindingFlags.NonPublic);
                shopTabField?.SetValue(bootstrap, 0);
                Call("ShowShop");
                SaveScreen("Shop");
                SaveScreen("Shop_Coins");

                shopTabField?.SetValue(bootstrap, 2);
                Call("ShowShop");
                SaveScreen("Shop_Shorts");
            }
            catch (Exception e) { Debug.LogWarning(e); }

            // 8. Trophies
            try { Call("ShowTrophyRoom"); SaveScreen("Trophies"); } catch (Exception e) { Debug.LogWarning(e); }

            // 9. Multiplayer
            try { Call("ShowMultiplayer"); SaveScreen("Multiplayer"); } catch (Exception e) { Debug.LogWarning(e); }

            // 10. Options & Donation Modal & Easter Eggs
            try 
            { 
                Call("ShowHome"); 
                Call("ShowOptions"); 
                SaveScreen("Options"); 
                Call("ShowHome"); 
            } 
            catch (Exception e) { Debug.LogWarning(e); }
            try 
            { 
                Call("ShowDonationModal"); 
                SaveScreen("DonationModal"); 
                Call("ShowHome"); 
            } 
            catch (Exception e) { Debug.LogWarning(e); }
            try
            {
                Call("ShowHome");
                var safeAreaVal = safeAreaField?.GetValue(bootstrap) as RectTransform;
                Call("ShowHandbookDialog", safeAreaVal);
                SaveScreen("Handbook");
                Call("ShowHome");
            }
            catch (Exception e) { Debug.LogWarning(e); }
            try
            {
                Call("ShowHome");
                var safeAreaVal = safeAreaField?.GetValue(bootstrap) as RectTransform;
                Call("ShowCreditsDialog", safeAreaVal);
                SaveScreen("Credits");
                Call("ShowHome");
            }
            catch (Exception e) { Debug.LogWarning(e); }
            try
            {
                Call("ShowHome");
                Call("ShowLegendsEasterEggModal");
                SaveScreen("LegendsDedication");
                Call("ShowHome");
            }
            catch (Exception e) { Debug.LogWarning(e); }

            // 11. Match FullScreen on 20:9 Aspect Ratio (Google Pixel 10)
            try
            {
                PlayerPrefs.SetInt("diceSoccer.fullscreen", 1);
                int fsHeight = 1422;
                var fsRt = new RenderTexture(width, fsHeight, 24);
                cam.orthographicSize = fsHeight / 2f;
                cam.targetTexture = fsRt;

                var safeAreaVal = safeAreaField?.GetValue(bootstrap) as RectTransform;
                if (safeAreaVal != null)
                {
                    safeAreaVal.anchorMin = Vector2.zero;
                    safeAreaVal.anchorMax = Vector2.one;
                    safeAreaVal.offsetMin = Vector2.zero;
                    safeAreaVal.offsetMax = Vector2.zero;
                }

                var catalog = GameCatalog.Load();
                var fixtures = catalog.GetFixtures(0, "League");
                var lineupField = typeof(DiceSoccerBootstrap).GetField("_lineupIds", BindingFlags.Instance | BindingFlags.NonPublic);
                var fixtureField = typeof(DiceSoccerBootstrap).GetField("_currentFixture", BindingFlags.Instance | BindingFlags.NonPublic);
                var matchField = typeof(DiceSoccerBootstrap).GetField("_match", BindingFlags.Instance | BindingFlags.NonPublic);
                
                var lineupIds = (int[])lineupField.GetValue(bootstrap);
                var homeLineup = catalog.ResolveLineup(lineupIds);
                fixtureField.SetValue(bootstrap, fixtures[0]);
                
                var match = new MatchSimulation(
                    homeLineup,
                    catalog.ResolveLineup(fixtures[0].lineup),
                    fixtures[0].teamName,
                    0,
                    new int[0],
                    fixtures[0].entourageLineup,
                    0,
                    fixtures[0].possessionBonus);
                matchField.SetValue(bootstrap, match);

                var showMatchScreenMethod = typeof(DiceSoccerBootstrap).GetMethod("ShowMatchScreen", BindingFlags.Instance | BindingFlags.NonPublic);
                showMatchScreenMethod?.Invoke(bootstrap, new object[] { homeLineup });

                cam.Render();
                RenderTexture.active = fsRt;
                var fsTex = new Texture2D(width, fsHeight, TextureFormat.RGB24, false);
                fsTex.ReadPixels(new Rect(0, 0, width, fsHeight), 0, 0);
                fsTex.Apply();
                File.WriteAllBytes("Screenshots/Match_FullScreen.png", fsTex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(fsTex);
                RenderTexture.active = null;
                cam.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(fsRt);
                Debug.Log("Saved Screenshots/Match_FullScreen.png");

                // Reset back to classic
                PlayerPrefs.SetInt("diceSoccer.fullscreen", 0);
            }
            catch (Exception e) { Debug.LogWarning(e); }

            RenderTexture.active = null;
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(camGo);
        }
    }
}
#endif
