using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiceSoccer
{
    public sealed class DiceSoccerBootstrap : MonoBehaviour
    {
        private const float ReferenceWidth = 640f;
        private const float ReferenceHeight = 960f;
        private const float ShakeDeltaThreshold = 1.8f;
        private const float ShakeCooldownSeconds = 0.8f;
        private const float ShakeQuietSecondsToRearm = 0.25f;
        private const float EdgeTapWidth = 0.12f;
        private const string LineupPreferenceKey = "diceSoccer.lineup";
        private const string RosterPreferenceKey = "diceSoccer.roster";
        private static readonly Color Ink = new Color32(24, 35, 44, 255);
        private static readonly Color Paper = new Color32(244, 239, 220, 255);
        private static readonly Color Grass = new Color32(47, 125, 77, 255);
        private static readonly Color Gold = new Color32(238, 184, 70, 255);

        private Canvas _canvas;
        private RectTransform _safeArea;
        private Rect _lastSafeArea;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private Text _matchScore;
        private Text _matchClock;
        private Text _matchReport;
        private Button _encounterButton;
        private Button _backButton;
        private Button _substitutionButton;
        private Image[] _homeDice;
        private Image[] _awayDice;
        private GameObject[] _homeDieRoots;
        private GameObject[] _awayDieRoots;
        private Text[] _homeDieValueLabels;
        private Text[] _awayDieValueLabels;
        private Image[] _homeDieBuffBadges;
        private Image[] _awayDieBuffBadges;
        private Image[] _homeDieGlows;
        private Image[] _awayDieGlows;
        private int _selectedBuffDieIndex = -1;
        private int _selectedBuffDieSide = -1;
        private int _awaitingBuffSide = -1;
        private bool _isAwaitingBuffSelection;
        private Image[] _homePlayers;
        private Image[] _awayPlayers;
        private float[] _homePlayerScales;
        private float[] _awayPlayerScales;
        private Image _ballMarker;
        private Image _matchScoreboard;
        private Transform _field;
        private RectTransform _fieldPitchRoot;
        private RectTransform _matchPlayRoot;
        private Image _shakePrompt;
        private Text _homeTotalText;
        private Text _awayTotalText;
        private GameObject _buffBeltGo;
        private Text _buffBeltText;
        private Image _buffBeltMedal;
        private Text _buffBeltBadgeText;
        private MatchSimulation _match;
        private GameCatalog _catalog;
        private FixtureData[] _seasonFixtures;
        private FixtureData _currentFixture;
        private string _competitionName = "League";
        private int[] _lineupIds;
        private List<int> _ownedPlayerIds;
        private int _selectedFixtureIndex;
        private int _hireCandidateIndex;
        private int _selectedLineupSlot;
        private int _seasonIndex;
        private int _maxUnlockedSeason;
        private int _fixtureIndex;
        private int _careerCash;
        private HashSet<int> _unlockedShirtIds = new HashSet<int> { 1 };
        private HashSet<int> _unlockedPantsIds = new HashSet<int> { 1 };
        private int _currentShirtId = 1;
        private int _currentPantsId = 1;
        private int _currentCrestId = 1;
        private int _currentPatternStyle = 0;
        private int _currentShirtColorId = 1;
        private int _currentPatternColorId = 10;
        private int _currentSleeveColorId = 1;
        private string _teamName = "SANUKU";
        private int _currentProfileSlot = 0;
        private int _selectedLeagueSeasonIndex = 0;
        private int _deckCategoryFilter = -1; // -1 = Auto/Target slot role, 0 = ALL, 1 = GK, 2 = DEF, 3 = MID, 4 = FWD
        private int _deckPage = 0;
        private int _kitEditTab = 0; // 0 = Jersey, 1 = Pattern, 2 = Shorts, 3 = Sleeves
        private HashSet<int> _unlockedAchievementIds = new HashSet<int>();
        private HashSet<int> _unlockedEntourageIds = new HashSet<int>();
        private List<int> _equippedEntourageIds = new List<int>();
        private int _shopTab;
        private int _shopPage;
        private int _trophyPage;
        private bool _currentResultRecorded;
        private bool _soundEnabled = true;
        private int _matchMaxHomeRoll;
        private bool _matchHadZeroHomeRoll;
        private bool _matchHadZeroAwayRoll;
        private bool _matchLastMinGoal;
        private bool _matchLastMinMiss;
        private bool _matchLastMinSave;
        private bool _matchLastMinConcede;
        private bool _matchLastMinEqualizer;
        private bool _matchLastMinTieBreaker;
        private readonly List<int> _pendingUnlockedPlayerIds = new List<int>();
        private readonly List<AchievementData> _pendingUnlockedAchievements = new List<AchievementData>();
        private Vector3 _previousAcceleration;
        private bool _hasAccelerationSample;
        private bool _shakeArmed = true;
        private float _shakeQuietSince = -1f;
        private float _shakeRearmAfter;
        private RectTransform _overlayContainer;
        private Image _ballShadow;
        private bool _inEncounterAnimation;
        private Coroutine _idleBobbingCoroutine;
        private bool _isLocalMultiplayer;
        private int _p2RivalTeamIndex = 0;
        private float _matchSpeed = 1f;
        private bool _isKickoffPlayed;
        private int _lastSubstitutedSlot = -1;
        private int _remainingSubs = 3;
        private readonly Vector2[] _homeBasePositions = new Vector2[7];
        private readonly Vector2[] _awayBasePositions = new Vector2[7];
        private readonly Image[] _homeEntourageIcons = new Image[2];
        private readonly Image[] _awayEntourageIcons = new Image[2];
        private bool _homeShirleyUsed;
        private bool _awayShirleyUsed;
        private Text _subRemainingText;
        private RectTransform _subDrawerGo;

        private void TriggerHaptic()
        {
            if (PlayerPrefs.GetInt("diceSoccer.vibration", 1) != 0)
            {
#if UNITY_IOS || UNITY_ANDROID
                Handheld.Vibrate();
#endif
            }
        }
        private static readonly Dictionary<int, Sprite> DiceSprites = new Dictionary<int, Sprite>();
        private static readonly Dictionary<string, Sprite> UiSprites =
            new Dictionary<string, Sprite>();
        private static Sprite DiceBackgroundSprite;
        private static readonly int[] StandardDiceFaces =
            { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 16, 17, 18, 24, 31, 32 };

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            if (FindAnyObjectByType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }
            _soundEnabled = PlayerPrefs.GetInt("diceSoccer.sound", 1) != 0;
            AudioManager.Instance.SoundEnabled = _soundEnabled;
            InitBasePositions();
            CreateCanvas();
            ApplySafeArea();
            LoadCareerProgress();
            bool hasProfile = PlayerPrefs.GetInt("diceSoccer.profile.0.exists", 0) == 1 ||
                              PlayerPrefs.GetInt("diceSoccer.profile.1.exists", 0) == 1 ||
                              PlayerPrefs.GetInt("diceSoccer.profile.2.exists", 0) == 1;
            if (!hasProfile)
            {
                ShowProfileSelect();
            }
            else
            {
                ShowHome();
            }
        }

        private void InitBasePositions()
        {
            // Verified tactical 1-3-3 formation for 7-a-side pitch
            // Home team (facing right): GK [0], DEF [1..3], MID/ATT [4..6]
            _homeBasePositions[0] = new Vector2(0.08f, 0.50f);
            _homeBasePositions[1] = new Vector2(0.18f, 0.76f);
            _homeBasePositions[2] = new Vector2(0.17f, 0.50f);
            _homeBasePositions[3] = new Vector2(0.18f, 0.24f);
            _homeBasePositions[4] = new Vector2(0.32f, 0.74f);
            _homeBasePositions[5] = new Vector2(0.36f, 0.50f);
            _homeBasePositions[6] = new Vector2(0.32f, 0.26f);

            // Away team (facing left, mirrored):
            _awayBasePositions[0] = new Vector2(0.92f, 0.50f);
            _awayBasePositions[1] = new Vector2(0.82f, 0.24f);
            _awayBasePositions[2] = new Vector2(0.83f, 0.50f);
            _awayBasePositions[3] = new Vector2(0.82f, 0.76f);
            _awayBasePositions[4] = new Vector2(0.68f, 0.26f);
            _awayBasePositions[5] = new Vector2(0.64f, 0.50f);
            _awayBasePositions[6] = new Vector2(0.68f, 0.74f);
        }

        private void Update()
        {
            if (_lastSafeArea != Screen.safeArea || _lastScreenWidth != Screen.width ||
                _lastScreenHeight != Screen.height)
                ApplySafeArea();

            if (_shakePrompt != null && _shakePrompt.gameObject.activeSelf)
            {
                float pulse = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f);
                _shakePrompt.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
            }

            if (_homePlayers != null && _awayPlayers != null && !_inEncounterAnimation)
            {
                var bob = 1f + 0.025f * Mathf.Sin(Time.unscaledTime * 3.5f);
                for (var i = 0; i < _homePlayers.Length; i++)
                {
                    if (_homePlayers[i] != null && _homePlayers[i].gameObject.activeSelf)
                    {
                        float baseScale = (_homePlayerScales != null && i < _homePlayerScales.Length) ? _homePlayerScales[i] : 1f;
                        _homePlayers[i].rectTransform.localScale = new Vector3(-baseScale, baseScale * bob, 1f); // Home faces right towards opponent
                    }
                    if (_awayPlayers[i] != null && _awayPlayers[i].gameObject.activeSelf)
                    {
                        float baseScale = (_awayPlayerScales != null && i < _awayPlayerScales.Length) ? _awayPlayerScales[i] : 1f;
                        _awayPlayers[i].rectTransform.localScale = new Vector3(baseScale, baseScale * bob, 1f); // Away faces left towards home
                    }
                }
            }

            DetectShake();
            DetectEdgeTap();
        }

        private void DetectShake()
        {
            var acceleration = Input.acceleration;
            if (!_hasAccelerationSample)
            {
                _previousAcceleration = acceleration;
                _hasAccelerationSample = true;
                return;
            }

            var accelerationDelta = (acceleration - _previousAcceleration).magnitude;
            _previousAcceleration = acceleration;

            if (_match == null || _match.IsFinished || _encounterButton == null ||
                !_encounterButton.interactable || _inEncounterAnimation ||
                Time.timeScale <= 0f || (_overlayContainer != null && _overlayContainer.childCount > 0))
                return;

            if (accelerationDelta >= ShakeDeltaThreshold)
            {
                _shakeQuietSince = -1f;
                if (_shakeArmed)
                {
                    _shakeArmed = false;
                    _shakeRearmAfter = Time.unscaledTime + ShakeCooldownSeconds;
                    PlayEncounter();
                }
                return;
            }

            if (_shakeArmed || Time.unscaledTime < _shakeRearmAfter)
                return;

            if (_shakeQuietSince < 0f)
                _shakeQuietSince = Time.unscaledTime;
            else if (Time.unscaledTime - _shakeQuietSince >= ShakeQuietSecondsToRearm)
                _shakeArmed = true;
        }

        private static bool IsPointerOverHudButton(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;
            var ped = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, results);
            if (results.Count == 0) return false;
            var top = results[0].gameObject;
            if (top == null) return false;
            return top.name != "FieldTapTrigger" && top.name != "ShakePrompt" &&
                   top.GetComponentInParent<Button>() != null;
        }

        private void DetectEdgeTap()
        {
            if (_match == null || _match.IsFinished || _encounterButton == null ||
                !_encounterButton.interactable || _inEncounterAnimation ||
                Time.timeScale <= 0f || (_overlayContainer != null && _overlayContainer.childCount > 0))
                return;

            if (_isLocalMultiplayer)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (!IsPointerOverHudButton(Input.mousePosition))
                        PlayEncounter();
                    return;
                }
                foreach (var touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Began && !IsPointerOverHudButton(touch.position))
                    {
                        PlayEncounter();
                        return;
                    }
                }
                return;
            }

            var edgeWidth = Screen.width * EdgeTapWidth;
            if (Input.GetMouseButtonDown(0) &&
                (Input.mousePosition.x <= edgeWidth || Input.mousePosition.x >= Screen.width - edgeWidth))
            {
                if (!IsPointerOverHudButton(Input.mousePosition))
                    PlayEncounter();
                return;
            }

            foreach (var touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began &&
                    (touch.position.x <= edgeWidth || touch.position.x >= Screen.width - edgeWidth))
                {
                    if (!IsPointerOverHudButton(touch.position))
                        PlayEncounter();
                    return;
                }
            }
        }

        private void CreateCanvas()
        {
            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = Color.black;
            }

            var canvasObject = new GameObject("Game Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            scaler.dynamicPixelsPerUnit = 2.5f;

            var canvasBg = new GameObject("CanvasBackground", typeof(RectTransform), typeof(Image));
            canvasBg.transform.SetParent(canvasObject.transform, false);
            var bgRect = canvasBg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
            var bgImg = canvasBg.GetComponent<Image>();
            bgImg.color = Color.black;
            bgImg.raycastTarget = false;

            var safeAreaObject = new GameObject("Safe Area", typeof(RectTransform));
            safeAreaObject.transform.SetParent(canvasObject.transform, false);
            _safeArea = safeAreaObject.GetComponent<RectTransform>();

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("Event System", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }
        }

        private void ShowHome()
        {
            _match = null;
            _encounterButton = null;
            _isLocalMultiplayer = false;
            AudioManager.Instance.PlayBgm("MainTheme", 0.5f);
            AudioManager.Instance.StopCrowd();
            ClearCanvas();
            AddMenuBackdrop(true);
            AddMenuHeader(null, null);
            AddMenuFooter();

            var frame = CreateCenteredReferenceFrame("HomeFrame");

            // Official Dice Soccer Title Logo in the sky
            var homeLogo = AddResourceImage("HomeLogo", "Original/images/StartScreenLogo", frame,
                new Vector2(0.5f, 0.77f), new Vector2(0.5f, 0.77f));
            homeLogo.rectTransform.sizeDelta = new Vector2(460f, 210f);
            homeLogo.preserveAspect = true;
            homeLogo.raycastTarget = false;
            StartCoroutine(AnimatePulse(homeLogo.rectTransform));

            var catalogAvailable = Resources.Load<TextAsset>("Original/data/UnityCatalog") != null;

            // Manage unlocks after match 1 or if all unlocked
            var manageUnlocked = catalogAvailable && (_fixtureIndex > 0 || _seasonIndex > 0 || _maxUnlockedSeason > 0 || PlayerPrefs.GetInt("diceSoccer.allUnlocked", 0) == 1);
            // Multiplayer unlocks after training season or if all unlocked
            var mpUnlocked = catalogAvailable && (_seasonIndex > 0 || _maxUnlockedSeason > 0 || _fixtureIndex >= 3 || PlayerPrefs.GetInt("diceSoccer.allUnlocked", 0) == 1);

            // Authentic left-aligned menu buttons: Campaign, Multiplayer, Manage (matches 2012 iOS v1.2.5 layout)
            var campaignBtn = AddSpriteButton("Campaign", "Original/images/MainMenuButtons01",
                "Original/images/MainMenuButtons01Pressed", frame,
                new Vector2(0.38f, 0.56f), new Vector2(0.54f, 0.115f), ShowLeagueSelect);
            campaignBtn.interactable = catalogAvailable;

            var mpNormal = mpUnlocked ? "Original/images/MainMenuButtons02" : "Original/images/MainMenuButtons02Disabled";
            var mpPressed = mpUnlocked ? "Original/images/MainMenuButtons02Pressed" : "Original/images/MainMenuButtons02Disabled";
            var mpBtn = AddSpriteButton("Multiplayer", mpNormal, mpPressed, frame,
                new Vector2(0.38f, 0.43f), new Vector2(0.54f, 0.115f), mpUnlocked ? ShowMultiplayer : () =>
                {
                    AudioManager.Instance.PlayDisappoint();
                });
            mpBtn.interactable = catalogAvailable;

            var manageNormal = manageUnlocked ? "Original/images/MainMenuButtons03" : "Original/images/MainMenuButtons03Disabled";
            var managePressed = manageUnlocked ? "Original/images/MainMenuButtons03Pressed" : "Original/images/MainMenuButtons03Disabled";
            var manageBtn = AddSpriteButton("Manage", manageNormal, managePressed, frame,
                new Vector2(0.38f, 0.30f), new Vector2(0.54f, 0.115f), manageUnlocked ? ShowManage : () =>
                {
                    AudioManager.Instance.PlayDisappoint();
                });
            manageBtn.interactable = catalogAvailable;

            // Authentic right vertical button stack: iCloud, Support (Handbook), News (matches 2012 iOS v1.2.5 layout)
            AddSpriteButton("iCloudButton", "Original/sprites/iCloudSS/iCloudButton",
                "Original/sprites/iCloudSS/iCloudButtonPressed", frame,
                new Vector2(0.86f, 0.68f), new Vector2(0.13f, 0.088f), ShowProfileSelect);

            AddSpriteButton("SupportButton", "Original/sprites/StartScreenLogoAnimation2/SupportButton",
                "Original/sprites/StartScreenLogoAnimation2/SupportButtonPressed", frame,
                new Vector2(0.86f, 0.56f), new Vector2(0.13f, 0.088f), () => ShowHandbookDialog(_safeArea));

            AddSpriteButton("NewsButton", "Original/sprites/StartScreenLogoAnimation2/NewsButton",
                "Original/sprites/StartScreenLogoAnimation2/NewsButtonPressed", frame,
                new Vector2(0.86f, 0.44f), new Vector2(0.13f, 0.088f), ShowNewsModal);

            if (!catalogAvailable)
            {
                AddText("Run Tools/extract_assets.py to import the original game data.",
                    frame, 16, new Color32(255, 193, 133, 255),
                    new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.19f));
            }
        }

        private void ShowNewsModal()
        {
            var overlay = AddPanel("NewsOverlay", _safeArea, new Color32(0, 0, 0, 180),
                new Vector2(0.5f, 0.5f), Vector2.one);
            overlay.raycastTarget = true;

            var box = AddResourceImage("NewsBox", "Original/sprites/ShopSS/ShopBox", overlay.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            box.rectTransform.sizeDelta = new Vector2(440f, 480f);
            box.preserveAspect = false;

            var title = AddText("DICE SOCCER NEWS", box.transform, 22, Ink,
                new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.94f));
            title.fontStyle = FontStyle.Bold;

            var body = AddText(
                "WELCOME TO THE 2026 REMASTER!\n\n" +
                "• All 96 original 2012 players fully unlocked\n" +
                "• All 5 official leagues & tournaments\n" +
                "• 1v1 Pass & Play local multiplayer\n" +
                "• High-resolution full screen & classic modes\n" +
                "• Special thanks to jairsldn & Karazero for preserving the game!\n\n" +
                "Enjoy the match!",
                box.transform, 13, Ink,
                new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.82f));
            body.alignment = TextAnchor.MiddleCenter;

            AddButton("☕ PAYPAL", box.transform, new Color32(0, 112, 186, 255),
                new Vector2(0.30f, 0.12f), new Vector2(0.38f, 0.11f), ShowDonationModal);
            AddButton("OK", box.transform, Grass,
                new Vector2(0.70f, 0.12f), new Vector2(0.38f, 0.11f), () =>
                {
                    Destroy(overlay.gameObject);
                    PlayerPrefs.SetInt("diceSoccer.stats.visitedNews", 1);
                    EvaluateCareerProgressAndAchievements(false, false, false, true);
                    ShowNextUnlockModalThen(null);
                });
        }

        private void ShowLeagueSelect()
        {
            _match = null;
            ClearCanvas();
            AddMenuBackdrop(true);
            AddMenuHeader(null, ShowHome);
            _catalog ??= GameCatalog.Load();

            _selectedLeagueSeasonIndex = Mathf.Clamp(_selectedLeagueSeasonIndex, 0, _catalog.Data.seasons.Length - 1);
            var season = _catalog.Data.seasons[_selectedLeagueSeasonIndex];

            var frame = CreateCenteredReferenceFrame("LeagueSelectFrame");

            // Large Season Crest in Center (matching media_1792577571727_b6fde5ba.png):
            string crestPath = _selectedLeagueSeasonIndex switch
            {
                0 => "Original/sprites/SeasonCrest/Season0Crest",
                1 => "Original/sprites/SeasonCrest1/Season1Crest",
                2 => "Original/sprites/SeasonCrest/Season2Crest",
                3 => "Original/sprites/SeasonCrest/Season3Crest",
                _ => "Original/sprites/SeasonCrest/Season4Crest"
            };

            bool isUnlocked = _selectedLeagueSeasonIndex <= _maxUnlockedSeason || PlayerPrefs.GetInt("diceSoccer.allUnlocked", 0) == 1;

            var crestGo = AddResourceImage("SeasonCrest", crestPath, frame,
                new Vector2(0.5f, 0.54f), new Vector2(0.5f, 0.54f));
            crestGo.rectTransform.sizeDelta = new Vector2(340f, 340f);
            crestGo.preserveAspect = true;
            crestGo.raycastTarget = true;
            var crestBtn = crestGo.gameObject.AddComponent<Button>();
            crestBtn.transition = Selectable.Transition.None;
            crestBtn.onClick.AddListener(() =>
            {
                if (isUnlocked)
                {
                    _seasonIndex = _selectedLeagueSeasonIndex;
                    StartCareerMatch();
                }
                else
                {
                    AudioManager.Instance.PlayDisappoint();
                }
            });

            if (!isUnlocked)
            {
                crestGo.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                var lockImg = AddResourceImage("CrestLock", "Original/sprites/SeasonCrest1/SeasonCrestLock", crestGo.transform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                lockImg.rectTransform.sizeDelta = new Vector2(100f, 120f);
                lockImg.preserveAspect = true;
                lockImg.raycastTarget = false;
            }

            // Left & Right Navigation Arrows
            if (_selectedLeagueSeasonIndex > 0)
            {
                AddSpriteButton("LeftArrow", "Original/images/LeftArrow", "Original/images/LeftArrowSelected", frame,
                    new Vector2(0.09f, 0.54f), new Vector2(0.12f, 0.09f), () =>
                    {
                        _selectedLeagueSeasonIndex--;
                        ShowLeagueSelect();
                    });
            }
            else
            {
                var leftDisabled = AddResourceImage("LeftArrowDisabled", "Original/images/LeftArrowDisabled", frame,
                    new Vector2(0.09f, 0.54f), new Vector2(0.09f, 0.54f));
                leftDisabled.rectTransform.sizeDelta = new Vector2(70f, 80f);
                leftDisabled.preserveAspect = true;
                leftDisabled.raycastTarget = false;
            }

            if (_selectedLeagueSeasonIndex < _catalog.Data.seasons.Length - 1)
            {
                AddSpriteButton("RightArrow", "Original/images/RightArrow", "Original/images/RightArrowSelected", frame,
                    new Vector2(0.91f, 0.54f), new Vector2(0.12f, 0.09f), () =>
                    {
                        _selectedLeagueSeasonIndex++;
                        ShowLeagueSelect();
                    });
            }
            else
            {
                var rightDisabled = AddResourceImage("RightArrowDisabled", "Original/images/RightArrowDisabled", frame,
                    new Vector2(0.91f, 0.54f), new Vector2(0.91f, 0.54f));
                rightDisabled.rectTransform.sizeDelta = new Vector2(70f, 80f);
                rightDisabled.preserveAspect = true;
                rightDisabled.raycastTarget = false;
            }

            // Season Name below crest (white with bold black outline matching media_1792577571727_b6fde5ba.png)
            var nameText = AddText(season.name.ToUpperInvariant(), frame, 26, Color.white,
                new Vector2(0.08f, 0.31f), new Vector2(0.92f, 0.38f));
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleCenter;
            var nameOutline = nameText.gameObject.AddComponent<Outline>();
            nameOutline.effectColor = Color.black;
            nameOutline.effectDistance = new Vector2(2f, -2f);

            // Stars calculation and authentic StarCounter icon (matching media_1792577571727_b6fde5ba.png):
            var leagueFixtures = _catalog.GetFixtures(_selectedLeagueSeasonIndex, "League");
            int maxStars = (leagueFixtures != null && leagueFixtures.Length > 0) ? leagueFixtures.Length * 3 : 9;
            int earnedStars = GetEarnedStarsForSeason(_selectedLeagueSeasonIndex);

            var starContainer = new GameObject("StarProgress", typeof(RectTransform));
            starContainer.transform.SetParent(frame, false);
            var starRect = starContainer.GetComponent<RectTransform>();
            starRect.anchorMin = starRect.anchorMax = new Vector2(0.5f, 0.16f);
            starRect.sizeDelta = new Vector2(170f, 50f);

            var starsText = AddText($"{earnedStars}/{maxStars}", starContainer.transform, 32, Ink,
                new Vector2(0f, 0f), new Vector2(0.60f, 1f));
            starsText.fontStyle = FontStyle.Bold;
            starsText.alignment = TextAnchor.MiddleRight;

            var starImg = AddResourceImage("StarIcon", "Original/sprites/SeasonCrest/StarCounter", starContainer.transform,
                new Vector2(0.64f, 0.05f), new Vector2(0.98f, 0.95f));
            starImg.preserveAspect = true;
            starImg.raycastTarget = false;
        }

        private int GetEarnedStarsForSeason(int seasonId)
        {
            if (_catalog == null) return 0;
            int stars = 0;
            var fixtures = _catalog.GetFixtures(seasonId, "League");
            if (fixtures != null)
            {
                for (int i = 0; i < fixtures.Length; i++)
                {
                    stars += PlayerPrefs.GetInt($"diceSoccer.season.{seasonId}.match.{i}.stars", 0);
                }
            }
            return stars;
        }

        private void ShowProfileSelect()
        {
            _match = null;
            ClearCanvas();
            AddMenuBackdrop(true);

            var frame = CreateCenteredReferenceFrame("ProfileSelectFrame");

            // Title Hero Logo at top (Easter Egg: Tap 5 times to reveal archival dedication!)
            var logo = AddResourceImage("StartScreenLogo", "Original/images/StartScreenLogo",
                frame, new Vector2(0.18f, 0.72f), new Vector2(0.82f, 0.96f));
            logo.preserveAspect = true;
            logo.raycastTarget = true;

            int logoTaps = 0;
            float lastTapTime = 0f;
            var logoBtn = logo.gameObject.AddComponent<Button>();
            logoBtn.transition = Selectable.Transition.None;
            logoBtn.onClick.AddListener(() =>
            {
                if (Time.time - lastTapTime > 2.5f) logoTaps = 0;
                lastTapTime = Time.time;
                logoTaps++;
                AudioManager.Instance.PlayTap();
                if (logoTaps >= 5)
                {
                    logoTaps = 0;
                    ShowLegendsEasterEggModal();
                }
            });

            float[] slotYs = { 0.58f, 0.44f, 0.30f };
            bool anyExists = false;
            for (int s = 0; s < 3; s++)
            {
                if (PlayerPrefs.GetInt($"diceSoccer.profile.{s}.exists", 0) == 1)
                    anyExists = true;
            }

            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                float y = slotYs[i];
                bool exists = PlayerPrefs.GetInt($"diceSoccer.profile.{slot}.exists", 0) == 1;
                string pName = PlayerPrefs.GetString($"diceSoccer.profile.{slot}.name", "New Profile");
                int pCrest = Mathf.Clamp(PlayerPrefs.GetInt($"diceSoccer.profile.{slot}.crest", 1), 1, 3);
                int pSeason = PlayerPrefs.GetInt($"diceSoccer.profile.{slot}.season", 0);
                int pCash = PlayerPrefs.GetInt($"diceSoccer.profile.{slot}.cash", 1000);

                // Slot main button (explicit 410x120 aspect ratio matching ProfileButton.png 456x150)
                var slotBtn = AddSpriteButton($"ProfileSlot_{slot}", "Original/images/ProfileButton",
                    "Original/images/ProfileButtonSelected", frame,
                    new Vector2(0.42f, y), new Vector2(0.64f, 0.125f), () =>
                    {
                        if (exists)
                        {
                            SelectProfile(slot);
                        }
                        else
                        {
                            ShowNewProfileModal(slot);
                        }
                    });
                var slotImg = slotBtn.GetComponent<Image>();
                slotImg.preserveAspect = false;

                if (exists)
                {
                    // Crest centered directly inside the rounded tab of ProfileButton (tab center is x=0.111f, y=0.50f)
                    var crest = AddResourceImage($"SlotCrest_{slot}", $"Original/images/CustomCrest{pCrest}", slotBtn.transform,
                        new Vector2(0.111f, 0.50f), new Vector2(0.111f, 0.50f));
                    crest.rectTransform.sizeDelta = new Vector2(58f, 58f);
                    crest.preserveAspect = true;
                    crest.raycastTarget = false;

                    // Name in center on main button body
                    var nameTxt = AddText(pName.ToUpperInvariant(), slotBtn.transform, 22, Ink,
                        new Vector2(0.25f, 0.50f), new Vector2(0.96f, 0.90f));
                    nameTxt.fontStyle = FontStyle.Bold;
                    nameTxt.alignment = TextAnchor.MiddleLeft;

                    // Subtext
                    var subTxt = AddText($"SEASON {pSeason}  •  {pCash:N0} COINS", slotBtn.transform, 13, new Color32(90, 80, 70, 255),
                        new Vector2(0.25f, 0.12f), new Vector2(0.96f, 0.50f));
                    subTxt.alignment = TextAnchor.MiddleLeft;

                    // Active Delete button to the right of profile button
                    AddSpriteButton($"Delete_{slot}", "Original/images/DeleteProfileButton",
                        "Original/images/DeleteProfileButtonSelected", frame,
                        new Vector2(0.85f, y), new Vector2(0.12f, 0.082f), () =>
                        {
                            ShowDeleteConfirmation(slot, pName);
                        });
                }
                else
                {
                    // Subtle placeholder crest for empty slot centered in badge tab
                    var placeholderCrest = AddResourceImage($"SlotCrestEmpty_{slot}", "Original/images/CustomCrest1", slotBtn.transform,
                        new Vector2(0.111f, 0.50f), new Vector2(0.111f, 0.50f));
                    placeholderCrest.rectTransform.sizeDelta = new Vector2(58f, 58f);
                    placeholderCrest.preserveAspect = true;
                    placeholderCrest.color = new Color(0.2f, 0.2f, 0.2f, 0.28f);
                    placeholderCrest.raycastTarget = false;

                    // New Profile empty slot label
                    var newTxt = AddText("New Profile", slotBtn.transform, 22, new Color32(110, 100, 90, 255),
                        new Vector2(0.25f, 0.20f), new Vector2(0.96f, 0.80f));
                    newTxt.fontStyle = FontStyle.Bold;
                    newTxt.alignment = TextAnchor.MiddleLeft;

                    // Disabled grey Delete button for empty slot (matching media_1792577433465_f8339b5f.png)
                    var delDis = AddResourceImage($"DeleteDisabled_{slot}", "Original/images/DeleteProfileButtonDisabled",
                        frame, new Vector2(0.85f, y), new Vector2(0.85f, y));
                    delDis.rectTransform.sizeDelta = new Vector2(76f, 76f);
                    delDis.preserveAspect = true;
                    delDis.raycastTarget = false;
                }
            }

            // Bottom Action button
            if (anyExists)
            {
                AddSpriteButton("EnterGameBtn", "Original/images/StartButton",
                    "Original/images/StartButtonPressed", frame,
                    new Vector2(0.50f, 0.14f), new Vector2(0.48f, 0.088f), () =>
                    {
                        int targetSlot = _currentProfileSlot;
                        if (PlayerPrefs.GetInt($"diceSoccer.profile.{targetSlot}.exists", 0) != 1)
                        {
                            for (int s = 0; s < 3; s++)
                            {
                                if (PlayerPrefs.GetInt($"diceSoccer.profile.{s}.exists", 0) == 1)
                                {
                                    targetSlot = s;
                                    break;
                                }
                            }
                        }
                        SelectProfile(targetSlot);
                    });
            }
            else
            {
                AddSpriteButton("CreateProfileBtn", "Original/images/StartButton",
                    "Original/images/StartButtonPressed", frame,
                    new Vector2(0.50f, 0.14f), new Vector2(0.48f, 0.088f), () =>
                    {
                        ShowNewProfileModal(0);
                    });
            }
        }

        private void ShowNewProfileModal(int slot)
        {
            var overlay = AddPanel("NewProfileOverlay", _safeArea, new Color32(0, 0, 0, 242),
                new Vector2(0.5f, 0.5f), Vector2.one);
            overlay.raycastTarget = true;

            var box = new GameObject("ModalContainer", typeof(RectTransform));
            box.transform.SetParent(overlay.transform, false);
            var boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.70f);
            boxRect.sizeDelta = new Vector2(560f, 320f);

            // Title: "New Profile"
            var title = AddText("New Profile", box.transform, 28, Color.white,
                new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.98f));
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;

            // White Input Box (matching media_1792577433465_f8339b5f.png)
            var inputGo = new GameObject("ProfileInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputGo.transform.SetParent(box.transform, false);
            var inRect = inputGo.GetComponent<RectTransform>();
            inRect.anchorMin = new Vector2(0.05f, 0.52f);
            inRect.anchorMax = new Vector2(0.95f, 0.74f);
            inRect.offsetMin = inRect.offsetMax = Vector2.zero;

            var inBg = inputGo.GetComponent<Image>();
            inBg.color = Color.white;

            var inField = inputGo.GetComponent<InputField>();
            inField.characterLimit = 14;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(inputGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.03f, 0.05f);
            textRect.anchorMax = new Vector2(0.88f, 0.95f);
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;

            var inText = textGo.GetComponent<Text>();
            inText.font = GetBuiltinFont();
            inText.fontSize = 24;
            inText.color = Color.black;
            inText.alignment = TextAnchor.MiddleLeft;
            inText.fontStyle = FontStyle.Bold;

            inField.textComponent = inText;
            inField.text = "Sanuku";

            // Clear 'x' button inside input on the right
            var clearBtnGo = new GameObject("ClearButton", typeof(RectTransform), typeof(Button));
            clearBtnGo.transform.SetParent(inputGo.transform, false);
            var clearRect = clearBtnGo.GetComponent<RectTransform>();
            clearRect.anchorMin = new Vector2(0.89f, 0.15f);
            clearRect.anchorMax = new Vector2(0.98f, 0.85f);
            clearRect.offsetMin = clearRect.offsetMax = Vector2.zero;

            var clearText = AddText("x", clearBtnGo.transform, 20, new Color32(140, 140, 140, 255),
                Vector2.zero, Vector2.one);
            clearText.fontStyle = FontStyle.Bold;
            clearText.alignment = TextAnchor.MiddleCenter;

            var clearBtn = clearBtnGo.GetComponent<Button>();
            clearBtn.onClick.AddListener(() =>
            {
                inField.text = "";
                inField.ActivateInputField();
            });

            // Subtext: "Enter up to 14 characters."
            var subText = AddText("Enter up to 14 characters.", box.transform, 17, new Color32(235, 235, 235, 255),
                new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.48f));
            subText.fontStyle = FontStyle.Bold;
            subText.alignment = TextAnchor.MiddleCenter;

            // Two Buttons: "Back" and "Create" (matching media_1792577433465_f8339b5f.png)
            var backBtnGo = new GameObject("BackButton", typeof(RectTransform), typeof(Button));
            backBtnGo.transform.SetParent(box.transform, false);
            var backRect = backBtnGo.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.12f, 0.08f);
            backRect.anchorMax = new Vector2(0.44f, 0.28f);
            backRect.offsetMin = backRect.offsetMax = Vector2.zero;
            var backBtn = backBtnGo.GetComponent<Button>();
            var backText = AddText("Back", backBtnGo.transform, 26, Color.white, Vector2.zero, Vector2.one);
            backText.fontStyle = FontStyle.Bold;
            backText.alignment = TextAnchor.MiddleCenter;
            var backShadow = backText.gameObject.AddComponent<Shadow>();
            backShadow.effectColor = new Color(0, 0, 0, 0.75f);
            backShadow.effectDistance = new Vector2(2f, -2f);
            backBtn.onClick.AddListener(() =>
            {
                AudioManager.Instance.PlayTap();
                Destroy(overlay.gameObject);
            });

            var createBtnGo = new GameObject("CreateButton", typeof(RectTransform), typeof(Button));
            createBtnGo.transform.SetParent(box.transform, false);
            var createRect = createBtnGo.GetComponent<RectTransform>();
            createRect.anchorMin = new Vector2(0.56f, 0.08f);
            createRect.anchorMax = new Vector2(0.88f, 0.28f);
            createRect.offsetMin = createRect.offsetMax = Vector2.zero;
            var createBtn = createBtnGo.GetComponent<Button>();
            var createText = AddText("Create", createBtnGo.transform, 26, Color.white, Vector2.zero, Vector2.one);
            createText.fontStyle = FontStyle.Bold;
            createText.alignment = TextAnchor.MiddleCenter;
            var createShadow = createText.gameObject.AddComponent<Shadow>();
            createShadow.effectColor = new Color(0, 0, 0, 0.75f);
            createShadow.effectDistance = new Vector2(2f, -2f);
            createBtn.onClick.AddListener(() =>
            {
                AudioManager.Instance.PlayTap();
                string name = inField.text.Trim();
                if (string.IsNullOrEmpty(name)) name = "Sanuku";
                if (name.Length > 14) name = name.Substring(0, 14);

                CreateProfile(slot, name);
                Destroy(overlay.gameObject);
                ShowHome();
            });
        }

        private void SelectProfile(int slot)
        {
            _currentProfileSlot = slot;
            _teamName = PlayerPrefs.GetString($"diceSoccer.profile.{slot}.name", "SANUKU");
            _currentCrestId = Mathf.Clamp(PlayerPrefs.GetInt($"diceSoccer.profile.{slot}.crest", 1), 1, 3);
            PlayerPrefs.SetInt("diceSoccer.profile.active", slot);
            PlayerPrefs.Save();
            LoadCareerProgress();
            ShowHome();
        }

        private void CreateProfile(int slot, string name)
        {
            _currentProfileSlot = slot;
            _teamName = name;
            _currentCrestId = 1;
            _careerCash = 1000;
            _seasonIndex = 0;
            _maxUnlockedSeason = 0;
            _selectedLeagueSeasonIndex = 0;
            _fixtureIndex = 0;

            var lower = name.Trim().ToLowerInvariant();
            bool isLegend = lower == "karazero" || lower == "jairsldn";
            if (isLegend)
            {
                _careerCash = 50000; // Special Founder & Archivist Coin Bonus!
            }

            PlayerPrefs.SetInt($"diceSoccer.profile.{slot}.exists", 1);
            PlayerPrefs.SetString($"diceSoccer.profile.{slot}.name", _teamName);
            PlayerPrefs.SetInt($"diceSoccer.profile.{slot}.crest", _currentCrestId);
            PlayerPrefs.SetInt($"diceSoccer.profile.{slot}.cash", _careerCash);
            PlayerPrefs.SetInt($"diceSoccer.profile.{slot}.season", _seasonIndex);
            PlayerPrefs.SetInt($"diceSoccer.profile.{slot}.maxSeason", _maxUnlockedSeason);
            PlayerPrefs.SetInt("diceSoccer.maxSeason", _maxUnlockedSeason);
            PlayerPrefs.SetInt($"diceSoccer.profile.{slot}.fixture", _fixtureIndex);
            PlayerPrefs.SetInt("diceSoccer.profile.active", slot);
            PlayerPrefs.Save();

            SaveCareerState();

            if (isLegend)
            {
                ShowLegendsEasterEggModal();
            }
        }

        private void ShowDeleteConfirmation(int slot, string pName)
        {
            var overlay = AddPanel("DelOverlay", _safeArea, new Color32(0, 0, 0, 195),
                new Vector2(0.5f, 0.5f), Vector2.one);
            overlay.raycastTarget = true;

            var box = AddResourceImage("DelBox", "Original/sprites/ShopSS/ShopBox", overlay.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            box.rectTransform.sizeDelta = new Vector2(400f, 260f);
            box.preserveAspect = false;

            var txt = AddText($"Delete profile\n\"{pName}\"?", box.transform, 20, Ink,
                new Vector2(0.08f, 0.40f), new Vector2(0.92f, 0.90f));
            txt.fontStyle = FontStyle.Bold;

            AddButton("YES", box.transform, new Color32(200, 60, 60, 255),
                new Vector2(0.28f, 0.20f), new Vector2(0.36f, 0.22f), () =>
                {
                    PlayerPrefs.SetInt($"diceSoccer.profile.{slot}.exists", 0);
                    PlayerPrefs.DeleteKey($"diceSoccer.profile.{slot}.name");
                    PlayerPrefs.Save();
                    Destroy(overlay.gameObject);
                    ShowProfileSelect();
                });

            AddButton("NO", box.transform, Grass,
                new Vector2(0.72f, 0.20f), new Vector2(0.36f, 0.22f), () =>
                {
                    Destroy(overlay.gameObject);
                });
        }

        private void ShowOptions()
        {
            ShowOptionsDialog(_safeArea != null ? _safeArea : _canvas.transform);
        }

        private void ToggleDisplayMode()
        {
            int current = PlayerPrefs.GetInt("diceSoccer.fullscreen", 0);
            PlayerPrefs.SetInt("diceSoccer.fullscreen", current == 1 ? 0 : 1);
            PlayerPrefs.Save();
            ApplySafeArea();
            ShowOptions();
        }

        private void ToggleSound()
        {
            _soundEnabled = !_soundEnabled;
            AudioManager.Instance.SoundEnabled = _soundEnabled;
            PlayerPrefs.SetInt("diceSoccer.sound", _soundEnabled ? 1 : 0);
            PlayerPrefs.Save();
            ShowOptions();
        }

        private void UnlockAllContent(bool showNotice = true)
        {
            _catalog ??= GameCatalog.Load();
            _ownedPlayerIds = new List<int>();
            foreach (var p in _catalog.Data.players)
            {
                if (p.id > 0 && p.id < 1000 && !_ownedPlayerIds.Contains(p.id))
                    _ownedPlayerIds.Add(p.id);
            }
            _careerCash = 999999;

            if (_catalog.Data.shirts != null)
            {
                foreach (var s in _catalog.Data.shirts)
                {
                    if (s != null && s.id > 0) _unlockedShirtIds.Add(s.id);
                }
            }
            if (_catalog.Data.colours != null)
            {
                foreach (var c in _catalog.Data.colours)
                {
                    if (c != null && c.id > 0) _unlockedPantsIds.Add(c.id);
                }
            }
            if (_catalog.Data.achievements != null)
            {
                foreach (var a in _catalog.Data.achievements)
                {
                    if (a != null && a.id > 0) _unlockedAchievementIds.Add(a.id);
                }
            }
            if (_catalog.Data.entourage != null)
            {
                foreach (var e in _catalog.Data.entourage)
                {
                    if (e != null && e.id > 0) _unlockedEntourageIds.Add(e.id);
                }
            }

            PlayerPrefs.SetString(RosterPreferenceKey, string.Join(",", _ownedPlayerIds));
            PlayerPrefs.SetInt("diceSoccer.cash", _careerCash);
            PlayerPrefs.SetString("diceSoccer.unlockedShirts", string.Join(",", _unlockedShirtIds));
            PlayerPrefs.SetString("diceSoccer.unlockedPants", string.Join(",", _unlockedPantsIds));
            PlayerPrefs.SetString("diceSoccer.achievements", string.Join(",", _unlockedAchievementIds));
            PlayerPrefs.SetString("diceSoccer.unlockedEntourage", string.Join(",", _unlockedEntourageIds));
            _maxUnlockedSeason = _catalog.Data.seasons != null ? Mathf.Max(0, _catalog.Data.seasons.Length - 1) : 4;
            PlayerPrefs.SetInt("diceSoccer.maxSeason", _maxUnlockedSeason);
            PlayerPrefs.SetInt($"diceSoccer.profile.{_currentProfileSlot}.maxSeason", _maxUnlockedSeason);
            PlayerPrefs.SetInt("diceSoccer.allUnlocked", 1);
            PlayerPrefs.Save();
            if (showNotice) AudioManager.Instance.PlayCash();
        }

        private void ShowDonationModal()
        {
            var backdrop = AddPanel("DonationBackdrop", _safeArea,
                new Color(0f, 0f, 0f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.one);
            backdrop.raycastTarget = true;

            var box = AddResourceImage("DonationBox", "Original/sprites/AchievementsSS4/LongDialogBox", backdrop.transform,
                new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f));
            box.rectTransform.sizeDelta = new Vector2(380f, 520f);
            box.preserveAspect = false;

            var title = AddText("DICE SOCCER REMASTER", box.transform, 20, Ink,
                new Vector2(0.08f, 0.83f), new Vector2(0.92f, 0.91f));
            title.fontStyle = FontStyle.Bold;

            var desc = AddText(
                "Dice Soccer Remaster is a 100% free non-profit fan project preserving this 2012 iOS classic on modern Android.\n\n" +
                "All 96 authentic players, kits, coins and seasons are fully unlocked and free for everyone!\n\n" +
                "If you enjoy the remaster and wish to support the work, feel free to buy a coffee via PayPal:",
                box.transform, 13, Ink,
                new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.82f));
            desc.alignment = TextAnchor.UpperCenter;

            // PayPal Button
            AddButton("☕ BUY A COFFEE (PAYPAL)", box.transform, new Color32(0, 112, 186, 255),
                new Vector2(0.5f, 0.28f), new Vector2(0.80f, 0.08f), () =>
                {
                    Application.OpenURL("https://paypal.me/Freekara3");
                });

            // Re-unlock Button
            AddButton("★ UNLOCK EVERYTHING FREE", box.transform, Grass,
                new Vector2(0.5f, 0.18f), new Vector2(0.80f, 0.08f), () =>
                {
                    UnlockAllContent(false);
                    Destroy(backdrop.gameObject);
                });

            // Close Button
            AddButton("CLOSE", box.transform, new Color32(68, 79, 84, 255),
                new Vector2(0.5f, 0.08f), new Vector2(0.50f, 0.06f), () =>
                {
                    Destroy(backdrop.gameObject);
                });
        }

        private void ShowLegendsEasterEggModal()
        {
            var overlay = AddPanel("LegendsOverlay", _safeArea, new Color(0f, 0f, 0f, 0.92f),
                new Vector2(0.5f, 0.5f), Vector2.one);
            overlay.raycastTarget = true;

            var box = AddResourceImage("LegendsBox", "Original/sprites/AchievementsSS4/LongDialogBox", overlay.transform,
                new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f));
            box.rectTransform.sizeDelta = new Vector2(500f, 680f);
            box.preserveAspect = false;

            var title = AddText("★ THE ARCHIVE LEGENDS ★", box.transform, 21, Ink,
                new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.90f));
            title.fontStyle = FontStyle.Bold;

            var body = AddText(
                "A SPECIAL DEDICATION\n\n" +
                "👑 KARAZERO\n" +
                "Project Creator, Engineering & 1:1 Preservation\n\n" +
                "🏆 JAIRSLDN\n" +
                "The Legendary Archive Guardian\n\n" +
                "Across the entire internet, jairsldn was the only person who preserved and held the original 2012 iOS binaries and files of Dice Soccer.\n\n" +
                "Without his archival passion and Karazero's dedication, this game would have been lost to history.\n\n" +
                "Dice Soccer lives on for all fans!",
                box.transform, 14, Ink,
                new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.80f));
            body.alignment = TextAnchor.MiddleCenter;

            // Confetti burst
            for (var i = 0; i < 20; i++)
            {
                var cId = UnityEngine.Random.Range(1, 4);
                var x = UnityEngine.Random.Range(0.05f, 0.95f);
                var y = UnityEngine.Random.Range(0.20f, 0.95f);
                var cImg = AddResourceImage($"EasterConfetti{i}",
                    $"Original/sprites/ConfettiSS/Confetti{cId}", overlay.transform,
                    new Vector2(x, y), new Vector2(x, y));
                cImg.rectTransform.sizeDelta = new Vector2(24f, 24f);
                cImg.rectTransform.localEulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(0f, 360f));
                cImg.raycastTarget = false;
            }

            AudioManager.Instance.PlayMedalCeremonyLong();

            AddButton("LONG LIVE DICE SOCCER! ⚽", box.transform, Grass,
                new Vector2(0.5f, 0.11f), new Vector2(0.78f, 0.08f), () =>
                {
                    Destroy(overlay.gameObject);
                });
        }

        private void ShowMultiplayer()
        {
            _catalog ??= GameCatalog.Load();
            EnsureLineupLoaded();
            ClearCanvas();
            AddMenuBackdrop(false);

            var sunGlow = AddResourceImage("SunGlow", "Original/sprites/MultiplayerBGSS/MultiplayerSunGlow",
                _canvas.transform, new Vector2(0.5f, 0.70f), new Vector2(0.5f, 0.70f));
            sunGlow.rectTransform.sizeDelta = new Vector2(600f, 400f);
            sunGlow.preserveAspect = true;
            sunGlow.raycastTarget = false;

            _safeArea.SetAsLastSibling();
            AddMenuHeader("1 VS 1", ShowHome);

            var frame = CreateCenteredReferenceFrame("MultiplayerFrame");

            var subTitle = AddText("1 VS 1 PASS & PLAY ON THIS DEVICE", frame, 14, Ink,
                new Vector2(0.05f, 0.81f), new Vector2(0.95f, 0.86f));
            subTitle.fontStyle = FontStyle.Bold;

            // Player 1 Team Box (Left)
            var p1Box = AddResourceImage("P1Box", "Original/sprites/ShopSS/ShopBox", frame,
                new Vector2(0.04f, 0.48f), new Vector2(0.44f, 0.78f));
            p1Box.preserveAspect = false;

            var p1Badge = AddResourceImage("P1Badge", "Original/images/CustomCrest1", p1Box.transform,
                new Vector2(0.5f, 0.70f), new Vector2(0.5f, 0.70f));
            p1Badge.rectTransform.sizeDelta = new Vector2(64f, 64f);
            p1Badge.preserveAspect = true;

            var p1Title = AddText("PLAYER 1\n(RED TEAM)", p1Box.transform, 15, Ink,
                new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.56f));
            p1Title.fontStyle = FontStyle.Bold;

            AddText("Tap Left Edge to Roll", p1Box.transform, 12, new Color32(80, 80, 80, 255),
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.28f));

            // VS Label
            var vsText = AddText("VS", frame, 30, Ink,
                new Vector2(0.44f, 0.58f), new Vector2(0.56f, 0.68f));
            vsText.fontStyle = FontStyle.Bold;

            // Player 2 Team Box (Right) - Tap to cycle rival team
            string[] p2TeamNames = new string[] { "BLUE TEAM", "GREEN STARS", "GOLD TITANS", "ROYAL UNITED" };
            int[] p2CrestIds = new int[] { 2, 3, 1, 2 };
            _p2RivalTeamIndex = Mathf.Clamp(_p2RivalTeamIndex, 0, p2TeamNames.Length - 1);

            var p2Box = AddResourceImage("P2Box", "Original/sprites/ShopSS/ShopBox", frame,
                new Vector2(0.56f, 0.48f), new Vector2(0.96f, 0.78f));
            p2Box.preserveAspect = false;
            p2Box.raycastTarget = true;
            var p2Btn = p2Box.gameObject.AddComponent<Button>();
            p2Btn.transition = Selectable.Transition.None;
            p2Btn.onClick.AddListener(() =>
            {
                _p2RivalTeamIndex = (_p2RivalTeamIndex + 1) % p2TeamNames.Length;
                AudioManager.Instance.PlayTap();
                ShowMultiplayer();
            });

            var p2Badge = AddResourceImage("P2Badge", $"Original/images/CustomCrest{p2CrestIds[_p2RivalTeamIndex]}", p2Box.transform,
                new Vector2(0.5f, 0.70f), new Vector2(0.5f, 0.70f));
            p2Badge.rectTransform.sizeDelta = new Vector2(64f, 64f);
            p2Badge.preserveAspect = true;
            p2Badge.raycastTarget = false;

            var p2Title = AddText($"PLAYER 2\n({p2TeamNames[_p2RivalTeamIndex]})\n<size=10><color=#505050>(Tap to switch)</color></size>", p2Box.transform, 13, Ink,
                new Vector2(0.04f, 0.28f), new Vector2(0.96f, 0.58f));
            p2Title.fontStyle = FontStyle.Bold;
            p2Title.alignment = TextAnchor.MiddleCenter;
            p2Title.raycastTarget = false;

            AddText("Tap Right Edge to Roll", p2Box.transform, 12, new Color32(80, 80, 80, 255),
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.28f));

            // Instructions Box
            var infoBox = AddResourceImage("InfoBox", "Original/sprites/AchievementsSS4/LongDialogBox", frame,
                new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.44f));
            infoBox.preserveAspect = false;

            var infoText = AddText(
                "Challenge a friend on this device!\n\n" +
                "• P1 taps the left half of the screen to roll Red dice\n" +
                "• P2 taps the right half of the screen to roll Blue dice\n" +
                "• No server required — play anywhere completely offline!",
                infoBox.transform, 12, Ink,
                new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.88f));
            infoText.alignment = TextAnchor.MiddleCenter;

            // Start Match Button
            AddSpriteButton("Start1v1Btn", "Original/sprites/EnemyLineupSS1/FormationPlayButton",
                "Original/sprites/EnemyLineupSS1/FormationPlayButtonPressed", frame,
                new Vector2(0.50f, 0.16f), new Vector2(0.38f, 0.09f), StartLocalMultiplayerMatch);

            AddButton("BACK TO MENU", frame, new Color32(68, 79, 84, 255),
                new Vector2(0.50f, 0.06f), new Vector2(0.55f, 0.055f), ShowHome);
        }

        private void StartLocalMultiplayerMatch()
        {
            _catalog ??= GameCatalog.Load();
            EnsureLineupLoaded();
            _isLocalMultiplayer = true;

            var p1Lineup = _catalog.ResolveLineup(_lineupIds);

            // Rival team preset for Player 2
            string[] p2TeamNames = new string[] { "BLUE TEAM", "GREEN STARS", "GOLD TITANS", "ROYAL UNITED" };
            int[] p2Shirts = new int[] { 2, 3, 4, 10 };
            int[] p2Pants = new int[] { 2, 3, 4, 10 };
            int[][] p2Lineups = new int[][]
            {
                new int[] { 9, 10, 11, 12, 14, 15, 16 },
                new int[] { 17, 18, 19, 20, 22, 23, 24 },
                new int[] { 25, 26, 27, 28, 30, 31, 32 },
                new int[] { 33, 34, 35, 36, 38, 39, 40 }
            };

            var p2Idx = Mathf.Clamp(_p2RivalTeamIndex, 0, p2Lineups.Length - 1);
            var p2Lineup = _catalog.ResolveLineup(p2Lineups[p2Idx]);

            _currentFixture = new FixtureData
            {
                id = 999,
                teamName = $"PLAYER 2 ({p2TeamNames[p2Idx]})",
                lineup = p2Lineup.Select(p => p.id).ToArray(),
                cashWin = 1000,
                cashDraw = 500,
                cashLose = 200,
                shirt = p2Shirts[p2Idx],
                pants = p2Pants[p2Idx]
            };

            ResetMatchTrackingState();
            _match = new MatchSimulation(
                p1Lineup,
                p2Lineup,
                "PLAYER 2",
                0,
                Array.Empty<int>(),
                Array.Empty<int>(),
                0,
                0);

            ShowMatchScreen(p1Lineup);
        }

        private void StartCareerMatch()
        {
            _catalog ??= GameCatalog.Load();
            EnsureLineupLoaded();
            _isLocalMultiplayer = false;
            _seasonIndex = Mathf.Clamp(_seasonIndex, 0, _catalog.Data.seasons.Length - 1);
            _competitionName = "League";
            _seasonFixtures = _catalog.GetFixtures(_seasonIndex, _competitionName);
            if (_seasonFixtures.Length == 0)
                throw new InvalidDataException($"Season {_seasonIndex} has no League fixtures.");
            _fixtureIndex = LoadCompetitionProgress(_competitionName);
            _selectedFixtureIndex = Mathf.Clamp(_fixtureIndex, 0, _seasonFixtures.Length - 1);
            ShowLeagueFixtures();
        }

        private string GetMatchNoSpritePath(int number)
        {
            if (number == 11 || number == 12 || number == 14)
                return $"Original/sprites/MatchSelectionSS2/MatchNo{number}";
            return $"Original/sprites/MatchSelectionSS1/MatchNo{number}";
        }

        private void ShowLeagueFixtures()
        {
            if (_seasonFixtures == null || _seasonFixtures.Length == 0)
                throw new InvalidOperationException("League fixtures have not been loaded.");

            _match = null;
            ClearCanvas();
            AddMenuBackdrop(true);
            AddMenuHeader(null, ShowLeagueSelect);

            _selectedFixtureIndex = Mathf.Clamp(_selectedFixtureIndex, 0, _seasonFixtures.Length - 1);
            var fixture = _seasonFixtures[_selectedFixtureIndex];
            var tournament = _competitionName == "T1" || _competitionName == "T2";
            var locked = _selectedFixtureIndex > _fixtureIndex ||
                         (tournament && _selectedFixtureIndex == _fixtureIndex &&
                          _fixtureIndex > 0 && !IsPreviousTournamentFixtureWon());
            var isCompleted = _selectedFixtureIndex < _fixtureIndex;

            var frame = CreateCenteredReferenceFrame("FixturesFrame");

            // 1. Top Sky Header: Season Name in 2 lines centered (matches 2012 iOS layout)
            var seasonTitle = _seasonIndex == 0
                ? "TRAINING\nSEASON"
                : (_competitionName == "League" ? $"SEASON {_seasonIndex}\nLEAGUE" : $"TOURNAMENT\n{_competitionName}");
            var seasonText = AddText(seasonTitle, frame, 22, Ink,
                new Vector2(0.1f, 0.77f), new Vector2(0.9f, 0.88f));
            seasonText.alignment = TextAnchor.MiddleCenter;
            seasonText.fontStyle = FontStyle.Bold;
            seasonText.lineSpacing = 0.9f;

            // Top-right Opponent Preview button (matching frame_000830.jpg on iPod Touch 4)
            var previewBtn = AddSpriteButton("PreviewBtn",
                locked ? "Original/sprites/MatchSelectionSS2/PreviewButtonDisabled" : "Original/sprites/MatchSelectionSS2/PreviewButton",
                locked ? "Original/sprites/MatchSelectionSS2/PreviewButtonDisabled" : "Original/sprites/MatchSelectionSS2/PreviewButtonPressed",
                frame, new Vector2(0.83f, 0.80f), new Vector2(0.24f, 0.065f),
                locked ? null : () => ShowOpponentPreview(fixture));
            previewBtn.interactable = !locked;

            // 2. Star Counter: e.g. "0/9 ★" or "2/9 ★" (matching frame_000830.jpg)
            int maxStars = _seasonFixtures.Length * 3;
            int earnedStars = GetEarnedStarsForSeason(_seasonIndex);
            var starCounterContainer = new GameObject("FixtureStarCounter", typeof(RectTransform));
            starCounterContainer.transform.SetParent(frame, false);
            var scRect = starCounterContainer.GetComponent<RectTransform>();
            scRect.anchorMin = scRect.anchorMax = new Vector2(0.5f, 0.75f);
            scRect.sizeDelta = new Vector2(130f, 36f);

            var starCounterText = AddText($"{earnedStars}/{maxStars}", starCounterContainer.transform, 18, Ink,
                new Vector2(0f, 0f), new Vector2(0.65f, 1f));
            starCounterText.alignment = TextAnchor.MiddleRight;
            starCounterText.fontStyle = FontStyle.Bold;

            var starCounterIcon = AddResourceImage("StarCounterIcon", "Original/sprites/SeasonCrest/StarCounter",
                starCounterContainer.transform, new Vector2(0.68f, 0.12f), new Vector2(0.96f, 0.88f));
            starCounterIcon.preserveAspect = true;
            starCounterIcon.raycastTarget = false;

            // 3. Giant Soccer Ball in Center (Left half + Flipped Right half meeting seamlessly at container center)
            var ballSprite = locked
                ? "Original/sprites/MatchSelectionSS1/SoccerBallButtonLocked"
                : "Original/sprites/MatchSelectionSS1/SoccerBallButton";

            var ballContainer = new GameObject("SoccerBallContainer", typeof(RectTransform));
            ballContainer.transform.SetParent(frame, false);
            var containerRect = ballContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.585f);
            containerRect.anchorMax = new Vector2(0.5f, 0.585f);
            containerRect.sizeDelta = new Vector2(180f, 180f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            StartCoroutine(AnimatePulse(containerRect));

            float halfW = 180f * (96f / 190f);
            float halfH = 180f;

            var ballLeft = AddResourceImage("BallLeft", ballSprite, ballContainer.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            ballLeft.preserveAspect = false;
            ballLeft.rectTransform.pivot = new Vector2(1f, 0.5f);
            ballLeft.rectTransform.anchoredPosition = Vector2.zero;
            ballLeft.rectTransform.sizeDelta = new Vector2(halfW, halfH);

            var ballRight = AddResourceImage("BallRight", ballSprite, ballContainer.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            ballRight.preserveAspect = false;
            ballRight.rectTransform.pivot = new Vector2(1f, 0.5f);
            ballRight.rectTransform.anchoredPosition = Vector2.zero;
            ballRight.rectTransform.sizeDelta = new Vector2(halfW, halfH);
            ballRight.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            // Match Number on the ball
            var matchNum = _selectedFixtureIndex + 1;
            var matchNumPath = GetMatchNoSpritePath(matchNum);
            var matchNumImg = AddResourceImage("MatchNumber", matchNumPath, ballContainer.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            if (matchNumImg != null && matchNumImg.sprite != null)
            {
                matchNumImg.rectTransform.sizeDelta = new Vector2(54f, 54f);
                matchNumImg.preserveAspect = true;
            }
            else
            {
                if (matchNumImg != null) Destroy(matchNumImg.gameObject);
                var numTxt = AddText(matchNum.ToString(), ballContainer.transform, 34, Color.black,
                    new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
                numTxt.alignment = TextAnchor.MiddleCenter;
                numTxt.fontStyle = FontStyle.Bold;
            }

            // Left & Right Navigation Chevrons
            var canPrev = _selectedFixtureIndex > 0;
            var leftArrow = AddSpriteButton("LeftArrow",
                canPrev ? "Original/images/LeftArrow" : "Original/images/LeftArrowDisabled",
                canPrev ? "Original/images/LeftArrowSelected" : "Original/images/LeftArrowDisabled",
                frame, new Vector2(0.12f, 0.585f), new Vector2(0.10f, 0.12f),
                canPrev ? () => { _selectedFixtureIndex--; ShowLeagueFixtures(); } : null);
            leftArrow.interactable = canPrev;

            var canNext = _selectedFixtureIndex < _seasonFixtures.Length - 1;
            var rightArrow = AddSpriteButton("RightArrow",
                canNext ? "Original/images/RightArrowSelected" : "Original/images/RightArrowDisabled",
                canNext ? "Original/images/RightArrowSelected" : "Original/images/RightArrowDisabled",
                frame, new Vector2(0.88f, 0.585f), new Vector2(0.10f, 0.12f),
                canNext ? () => { _selectedFixtureIndex++; ShowLeagueFixtures(); } : null);
            rightArrow.interactable = canNext;

            // 3 Stars below ball overlapping bottom rim (matching frame_000830.jpg: left=1st star, right=2nd star, mid=3rd star)
            int matchStars = PlayerPrefs.GetInt($"diceSoccer.season.{_seasonIndex}.match.{_selectedFixtureIndex}.stars", isCompleted ? 2 : 0);
            string goldStarPath = "Original/sprites/MatchSelectionSS1/GoldStar";
            string emptyStarPath = "Original/sprites/MatchSelectionSS1/EmptyStar";

            var starLeft = AddResourceImage("StarLeft", matchStars >= 1 ? goldStarPath : emptyStarPath, ballContainer.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            starLeft.rectTransform.anchoredPosition = new Vector2(-46f, -74f);
            starLeft.rectTransform.sizeDelta = new Vector2(62f, 50f);
            starLeft.preserveAspect = true;

            var starRight = AddResourceImage("StarRight", matchStars >= 2 ? goldStarPath : emptyStarPath, ballContainer.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            starRight.rectTransform.anchoredPosition = new Vector2(46f, -74f);
            starRight.rectTransform.sizeDelta = new Vector2(62f, 50f);
            starRight.preserveAspect = true;

            var starMid = AddResourceImage("StarMid", matchStars >= 3 ? goldStarPath : emptyStarPath, ballContainer.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            starMid.rectTransform.anchoredPosition = new Vector2(0f, -84f);
            starMid.rectTransform.sizeDelta = new Vector2(74f, 60f);
            starMid.preserveAspect = true;
            starMid.transform.SetAsLastSibling();

            // 4. Match Info Below Ball
            var matchTitle = AddText($"MATCH {_selectedFixtureIndex + 1}", frame, 22, Ink,
                new Vector2(0.1f, 0.390f), new Vector2(0.9f, 0.435f));
            matchTitle.alignment = TextAnchor.MiddleCenter;
            matchTitle.fontStyle = FontStyle.Bold;

            var vsText = AddText($"vs. {fixture.teamName.ToUpperInvariant()}", frame, 17, Ink,
                new Vector2(0.1f, 0.345f), new Vector2(0.9f, 0.390f));
            vsText.alignment = TextAnchor.MiddleCenter;
            vsText.fontStyle = FontStyle.Bold;

            string savedScore = PlayerPrefs.GetString($"diceSoccer.season.{_seasonIndex}.match.{_selectedFixtureIndex}.score",
                isCompleted ? "1 - 0" : "");
            if (!string.IsNullOrEmpty(savedScore))
            {
                var scoreLabel = AddText(savedScore, frame, 20, Ink,
                    new Vector2(0.2f, 0.290f), new Vector2(0.8f, 0.340f));
                scoreLabel.alignment = TextAnchor.MiddleCenter;
                scoreLabel.fontStyle = FontStyle.Bold;
            }

            // 5. Action Buttons: MANAGE and PLAY! side-by-side on grass
            AddSpriteButton("ManageBtn", "Original/sprites/EnemyLineupSS1/ManageButton",
                "Original/sprites/EnemyLineupSS1/ManageButtonPressed", frame,
                new Vector2(0.32f, 0.20f), new Vector2(0.28f, 0.085f), ShowManage);

            var playNormal = locked
                ? "Original/sprites/MatchSelectionSS2/ManagePlayButtonDisabled"
                : "Original/sprites/MatchSelectionSS1/ManagePlayButton";
            var playPressed = locked
                ? "Original/sprites/MatchSelectionSS2/ManagePlayButtonDisabled"
                : "Original/sprites/MatchSelectionSS1/ManagePlayButtonPressed";
            var capturedMatchIdx = _selectedFixtureIndex;
            var playBtn = AddSpriteButton("PlayBtn", playNormal, playPressed, frame,
                new Vector2(0.68f, 0.20f), new Vector2(0.28f, 0.085f),
                locked ? null : () => StartFixture(capturedMatchIdx));
            playBtn.interactable = !locked;

            // 6. Bottom yellow bar & competition tabs (fixed 77px height docked to bottom of _safeArea)
            var footerBar = AddResourceImage("FixtureFooter", "Original/images/FooterBG", _safeArea,
                new Vector2(0f, 0f), new Vector2(1f, 0f));
            footerBar.rectTransform.pivot = new Vector2(0.5f, 0f);
            footerBar.rectTransform.anchoredPosition = Vector2.zero;
            footerBar.rectTransform.sizeDelta = new Vector2(0f, 77f);
            footerBar.preserveAspect = false;
            footerBar.raycastTarget = false;

            var competitions = _catalog.GetCompetitions(_seasonIndex);
            var availableComps = new List<CompetitionData>();
            foreach (var comp in competitions)
            {
                if (comp.fixtures != null && comp.fixtures.Length > 0)
                    availableComps.Add(comp);
            }

            var tabWidth = 0.26f;
            for (var i = 0; i < availableComps.Count; i++)
            {
                var comp = availableComps[i];
                var cName = comp.name;
                var isSel = cName == _competitionName;
                var isAvailable = IsCompetitionAvailable(cName);
                var tabPath = $"Original/sprites/MatchSelectionSS1/Campaign{cName}Tab" + (isSel ? "Selected" : string.Empty);
                float xCenter = availableComps.Count switch
                {
                    1 => 0.50f,
                    2 => 0.35f + i * 0.30f,
                    _ => 0.20f + i * 0.30f
                };
                var tabBtn = AddSpriteButton($"Tab_{cName}", tabPath, tabPath, footerBar.transform,
                    new Vector2(xCenter, 0.65f), new Vector2(tabWidth, 1.12f),
                    () =>
                    {
                        if (isAvailable)
                        {
                            SelectCompetition(cName);
                        }
                        else
                        {
                            AudioManager.Instance.PlayDisappoint();
                            ShowLockedTournamentModal(cName);
                        }
                    });
                tabBtn.interactable = true;
                if (!isAvailable)
                {
                    tabBtn.image.color = new Color(0.6f, 0.6f, 0.6f, 0.85f);
                    var lockImg = AddResourceImage($"Lock_{cName}", "Original/sprites/MatchSelectionSS1/TournamentLocked", tabBtn.transform,
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                    lockImg.rectTransform.sizeDelta = new Vector2(40f, 50f);
                    lockImg.preserveAspect = true;
                    lockImg.raycastTarget = false;
                }
            }

            if (_seasonIndex == 0 && _fixtureIndex == 0 && PlayerPrefs.GetInt("diceSoccer.tutorial.5", 0) == 0)
            {
                ShowTutorialPopup(5, () =>
                {
                    if (PlayerPrefs.GetInt("diceSoccer.tutorial.24", 0) == 0)
                        ShowTutorialPopup(24);
                });
            }
        }

        private void ShowLockedTournamentModal(string compName)
        {
            var container = _overlayContainer != null ? _overlayContainer : (_safeArea != null ? _safeArea : _canvas.transform);
            var modal = AddPanel("LockedTournamentModal", container,
                new Color(0f, 0f, 0f, 0.78f), new Vector2(0.5f, 0.5f), Vector2.one);
            modal.raycastTarget = true;

            var box = AddResourceImage("Box", "Original/sprites/AchievementsSS4/LongDialogBox", modal.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            box.rectTransform.sizeDelta = new Vector2(400f, 320f);
            box.preserveAspect = false;

            StartCoroutine(PopInBounceAnimation(box.rectTransform));

            var lockIcon = AddResourceImage("LockIcon", "Original/sprites/MatchSelectionSS1/TournamentLocked", box.transform,
                new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f));
            lockIcon.rectTransform.sizeDelta = new Vector2(65f, 80f);
            lockIcon.preserveAspect = true;

            var title = AddText("TOURNAMENT LOCKED", box.transform, 20, Ink,
                new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.64f));
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;

            var seasonName = (_catalog?.Data?.seasons != null && _seasonIndex < _catalog.Data.seasons.Length)
                ? _catalog.Data.seasons[_seasonIndex].name : "current";

            var msg = AddText($"Complete all fixtures in the\n{seasonName} League\nto unlock this tournament!",
                box.transform, 14, Ink,
                new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.50f));
            msg.alignment = TextAnchor.MiddleCenter;

            AddSpriteButton("OkBtn", "Original/images/ContinueButton", "Original/images/ContinueButtonPressed", box.transform,
                new Vector2(0.5f, 0.12f), new Vector2(0.48f, 0.15f), () => Destroy(modal.gameObject));
        }

        private GameObject CreateTrophyCup(int tier, Transform parent, Vector2 anchor, float scale = 0.45f)
        {
            var root = new GameObject($"TrophyCup_Tier{tier}", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(352f * scale, 780f * scale);
            rect.anchoredPosition = Vector2.zero;

            int t = Mathf.Clamp(tier, 1, 6);
            string colorSuffix = t switch
            {
                1 or 2 => "Bronze",
                3 or 5 => "Silver",
                _ => "Gold"
            };
            string pipType = (t == 1 || t == 3 || t == 4) ? "Dots" : "Stars";

            // 1. Radiant Curved Ribbons behind cup & die
            string[] curveNames =
            {
                $"TrophyLowerLeftCurve{colorSuffix}",
                $"TrophyLowerRightCurve{colorSuffix}",
                $"TrophyTopLeftCurve{colorSuffix}",
                $"TrophyTopRightCurve{colorSuffix}"
            };
            foreach (var cn in curveNames)
            {
                var cImg = AddResourceImage(cn, $"Original/sprites/MultiplayerSS3/{cn}", root.transform,
                    Vector2.zero, Vector2.one);
                cImg.preserveAspect = true;
                cImg.raycastTarget = false;
            }

            // 2. Trophy Base (wooden pedestal + silver nameplate)
            var baseImg = AddResourceImage("TrophyBase", "Original/sprites/MultiplayerSS3/TrophyBase", root.transform,
                Vector2.zero, Vector2.one);
            baseImg.preserveAspect = true;
            baseImg.raycastTarget = false;

            // 3. Trophy Tier Cup Cradle
            var cupImg = AddResourceImage($"TrophyCupTier{t}", $"Original/sprites/MultiplayerSS3/TrophyTier{t}Trophy", root.transform,
                Vector2.zero, Vector2.one);
            cupImg.preserveAspect = true;
            cupImg.raycastTarget = false;

            // 4. Trophy Stars around stem
            var starsImg = AddResourceImage($"TrophyStarsTier{t}", $"Original/sprites/MultiplayerSS3/TrophyTier{t}Stars", root.transform,
                Vector2.zero, Vector2.one);
            starsImg.preserveAspect = true;
            starsImg.raycastTarget = false;

            // 5. Crest Plaque centered on lower wooden front panel (below silver nameplate)
            var plaque = AddResourceImage("TrophyPlaque", "Original/sprites/MultiplayerSS3/TrophyCrestPlaque", root.transform,
                new Vector2(0.517f, 0.080f), new Vector2(0.517f, 0.080f));
            plaque.rectTransform.sizeDelta = new Vector2(82f * scale, 86f * scale);
            plaque.preserveAspect = true;
            plaque.raycastTarget = false;

            var plaqueCrest = AddResourceImage("PlaqueCrest", $"Original/images/CustomCrest{_currentCrestId}", plaque.transform,
                new Vector2(0.16f, 0.16f), new Vector2(0.84f, 0.84f));
            plaqueCrest.preserveAspect = true;
            plaqueCrest.raycastTarget = false;

            // 6. Giant 3D Die resting atop cradle + matching pips/stars
            var diceImg = AddResourceImage("TrophyDice", "Original/sprites/MultiplayerSS3/TrophyDice", root.transform,
                Vector2.zero, Vector2.one);
            diceImg.preserveAspect = true;
            diceImg.raycastTarget = false;

            var pipsImg = AddResourceImage($"TrophyDice{pipType}{colorSuffix}",
                $"Original/sprites/MultiplayerSS3/TrophyDice{pipType}{colorSuffix}", root.transform,
                Vector2.zero, Vector2.one);
            pipsImg.preserveAspect = true;
            pipsImg.raycastTarget = false;

            return root;
        }

        private void ShowTournamentVictoryModal(string compName)
        {
            var container = _overlayContainer != null ? _overlayContainer : (_safeArea != null ? _safeArea : _canvas.transform);

            AudioManager.Instance.PlayMedalCeremonyLong();
            AudioManager.Instance.PlayEffect("KidsCheering", 1f, 1f);
            AudioManager.Instance.PlayCash();
            TriggerHaptic();

            int cupPrize = 25000 * (_seasonIndex + 1);
            _careerCash += cupPrize;
            PlayerPrefs.SetInt($"diceSoccer.season.{_seasonIndex}.{compName}.trophyWon", 1);
            SaveCareerState();

            var modal = AddPanel("TournamentVictoryModal", container,
                new Color(0f, 0f, 0f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.one);
            modal.raycastTarget = true;

            for (var i = 0; i < 30; i++)
            {
                var cId = UnityEngine.Random.Range(1, 4);
                var x = UnityEngine.Random.Range(0.05f, 0.95f);
                var y = UnityEngine.Random.Range(0.40f, 0.95f);
                var cImg = AddResourceImage($"VictoryConfetti{i}",
                    $"Original/sprites/ConfettiSS/Confetti{cId}", modal.transform,
                    new Vector2(x, y), new Vector2(x, y));
                cImg.rectTransform.sizeDelta = new Vector2(26f, 26f);
                cImg.rectTransform.localEulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(0f, 360f));
                cImg.raycastTarget = false;
            }

            var box = AddResourceImage("Box", "Original/sprites/AchievementsSS4/LongDialogBox", modal.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            box.rectTransform.sizeDelta = new Vector2(430f, 540f);
            box.preserveAspect = false;

            StartCoroutine(PopInBounceAnimation(box.rectTransform));

            var title = AddText($"{compName.ToUpperInvariant()}\nCHAMPIONS!", box.transform, 22, Ink,
                new Vector2(0.06f, 0.775f), new Vector2(0.94f, 0.895f));
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;

            // Complete Championship Trophy (Curves + Base + Cradle + Stars + Giant Die + Pips + Team Plaque)
            int cupTier = Mathf.Clamp(_seasonIndex + 1, 1, 6);
            CreateTrophyCup(cupTier, box.transform, new Vector2(0.5f, 0.56f), 0.285f);

            var seasonName = (_catalog?.Data?.seasons != null && _seasonIndex < _catalog.Data.seasons.Length)
                ? _catalog.Data.seasons[_seasonIndex].name : "";

            var desc = AddText($"Congratulations!\nYour team has conquered the\n{seasonName} {compName} Tournament!",
                box.transform, 14, new Color32(50, 45, 40, 255),
                new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.345f));
            desc.alignment = TextAnchor.MiddleCenter;

            var prizeText = AddText($"+{cupPrize:N0} CASH BONUS", box.transform, 19, new Color32(195, 35, 20, 255),
                new Vector2(0.08f, 0.15f), new Vector2(0.92f, 0.225f));
            prizeText.fontStyle = FontStyle.Bold;
            prizeText.alignment = TextAnchor.MiddleCenter;

            AddSpriteButton("ContinueBtn", "Original/images/ContinueButton", "Original/images/ContinueButtonPressed", box.transform,
                new Vector2(0.5f, 0.085f), new Vector2(0.52f, 0.10f), () =>
                {
                    Destroy(modal.gameObject);
                    ShowLeagueFixtures();
                });
        }

        private void ShowSeasonPromotedModal(int newSeasonIndex)
        {
            var container = _overlayContainer != null ? _overlayContainer : (_safeArea != null ? _safeArea : _canvas.transform);

            AudioManager.Instance.PlayMedalCeremonyLong();
            AudioManager.Instance.PlayEffect("KidsCheering", 1f, 1f);
            TriggerHaptic();

            var modal = AddPanel("SeasonPromotedModal", container,
                new Color(0f, 0f, 0f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.one);
            modal.raycastTarget = true;

            var box = AddResourceImage("Box", "Original/sprites/AchievementsSS4/LongDialogBox", modal.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            box.rectTransform.sizeDelta = new Vector2(430f, 540f);
            box.preserveAspect = false;

            StartCoroutine(PopInBounceAnimation(box.rectTransform));

            var season = _catalog.Data.seasons[newSeasonIndex];
            string crestPath = newSeasonIndex switch
            {
                0 => "Original/sprites/SeasonCrest/Season0Crest",
                1 => "Original/sprites/SeasonCrest1/Season1Crest",
                2 => "Original/sprites/SeasonCrest/Season2Crest",
                3 => "Original/sprites/SeasonCrest/Season3Crest",
                _ => "Original/sprites/SeasonCrest/Season4Crest"
            };
            var crest = AddResourceImage("NewSeasonCrest", crestPath, box.transform,
                new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f));
            crest.rectTransform.sizeDelta = new Vector2(170f, 170f);
            crest.preserveAspect = true;

            var title = AddText("PROMOTED!", box.transform, 24, Ink,
                new Vector2(0.06f, 0.77f), new Vector2(0.94f, 0.89f));
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;

            var desc = AddText($"WELCOME TO\n{season.name.ToUpperInvariant()}!\n\nNew competitions, stronger rivals,\nand elite players are now available!",
                box.transform, 15, new Color32(50, 45, 40, 255),
                new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.44f));
            desc.alignment = TextAnchor.MiddleCenter;

            AddSpriteButton("PlayNowBtn", "Original/images/ContinueButton", "Original/images/ContinueButtonPressed", box.transform,
                new Vector2(0.5f, 0.08f), new Vector2(0.52f, 0.10f), () =>
                {
                    Destroy(modal.gameObject);
                    ShowLeagueFixtures();
                });
        }

        private void SelectCompetition(string competitionName)
        {
            if (!IsCompetitionAvailable(competitionName))
                return;
            var fixtures = _catalog.GetFixtures(_seasonIndex, competitionName);
            if (fixtures.Length == 0)
                return;
            _competitionName = competitionName;
            _seasonFixtures = fixtures;
            _fixtureIndex = LoadCompetitionProgress(competitionName);
            _selectedFixtureIndex = Mathf.Clamp(_fixtureIndex, 0, _seasonFixtures.Length - 1);
            ShowLeagueFixtures();
        }

        private bool IsCompetitionAvailable(string competitionName)
        {
            if (competitionName == "League")
                return true;
            return LoadCompetitionProgress("League") >= _catalog.GetFixtures(_seasonIndex, "League").Length;
        }

        private bool IsPreviousTournamentFixtureWon()
        {
            var previousIndex = _fixtureIndex - 1;
            var result = PlayerPrefs.GetInt(TournamentResultKey(_competitionName, previousIndex), 0);
            return result > 0;
        }

        private int LoadCompetitionProgress(string competitionName)
        {
            var key = CompetitionProgressKey(competitionName);
            var stored = PlayerPrefs.GetInt(key, -1);
            if (stored < 0 && competitionName == "League" && _seasonIndex == 0)
                stored = PlayerPrefs.GetInt("diceSoccer.fixture", 0);
            return Mathf.Clamp(stored < 0 ? 0 : stored, 0,
                _catalog.GetFixtures(_seasonIndex, competitionName).Length);
        }

        private string CompetitionProgressKey(string competitionName)
        {
            return $"diceSoccer.season.{_seasonIndex}.{competitionName}.fixture";
        }

        private string TournamentResultKey(string competitionName, int fixtureIndex)
        {
            return $"diceSoccer.season.{_seasonIndex}.{competitionName}.result.{fixtureIndex}";
        }

        private void StartFixture(int fixtureIndex)
        {
            if (fixtureIndex < 0 || fixtureIndex > _fixtureIndex ||
                fixtureIndex >= _seasonFixtures.Length)
                throw new InvalidOperationException("The selected competition fixture is still locked.");

            _selectedFixtureIndex = fixtureIndex;
            StartCurrentFixture();
        }

        private void ShowSeasonCompletion()
        {
            _match = null;
            _encounterButton = null;
            ClearCanvas();
            AddMenuBackdrop(true);
            AddMenuHeader("SEASON SUMMARY", ShowLeagueFixtures);

            var season = _catalog.Data.seasons[_seasonIndex];
            int earnedStars = GetEarnedStarsForSeason(_seasonIndex);
            var leagueFixtures = _catalog.GetFixtures(_seasonIndex, "League");
            int maxStars = (leagueFixtures != null && leagueFixtures.Length > 0) ? leagueFixtures.Length * 3 : 9;
            int threshold = MatchSimulation.PromotionThresholdForSeason(_seasonIndex);
            bool canPromote = MatchSimulation.CanPromoteToSeason(_seasonIndex, earnedStars);
            bool isFinalSeason = _seasonIndex >= _catalog.Data.seasons.Length - 1;

            var frame = CreateCenteredReferenceFrame("SeasonCompletionFrame");

            string crestPath = _seasonIndex switch
            {
                0 => "Original/sprites/SeasonCrest/Season0Crest",
                1 => "Original/sprites/SeasonCrest1/Season1Crest",
                2 => "Original/sprites/SeasonCrest/Season2Crest",
                3 => "Original/sprites/SeasonCrest/Season3Crest",
                _ => "Original/sprites/SeasonCrest/Season4Crest"
            };
            var crest = AddResourceImage("SeasonCrest", crestPath, frame,
                new Vector2(0.5f, 0.70f), new Vector2(0.5f, 0.70f));
            crest.rectTransform.sizeDelta = new Vector2(170f, 170f);
            crest.preserveAspect = true;

            var title = AddText($"{season.name.ToUpperInvariant()} LEAGUE COMPLETE",
                frame, 22, Gold,
                new Vector2(0.05f, 0.53f), new Vector2(0.95f, 0.60f));
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;

            var starText = AddText($"★ {earnedStars} / {maxStars} STARS EARNED",
                frame, 20, Paper,
                new Vector2(0.05f, 0.46f), new Vector2(0.95f, 0.52f));
            starText.fontStyle = FontStyle.Bold;
            starText.alignment = TextAnchor.MiddleCenter;

            if (isFinalSeason)
            {
                var finalMsg = AddText("CONGRATULATIONS!\nYou have conquered the highest league!",
                    frame, 16, Gold,
                    new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.44f));
                finalMsg.fontStyle = FontStyle.Bold;
                finalMsg.alignment = TextAnchor.MiddleCenter;

                AddButton("BACK TO SEASONS", frame, Grass,
                    new Vector2(0.5f, 0.25f), new Vector2(0.72f, 0.08f), ShowLeagueSelect);
            }
            else if (canPromote)
            {
                var nextSeason = _catalog.Data.seasons[_seasonIndex + 1];
                var promoMsg = AddText($"PROMOTION EARNED!\nYou have qualified for {nextSeason.name}!",
                    frame, 16, Gold,
                    new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.44f));
                promoMsg.fontStyle = FontStyle.Bold;
                promoMsg.alignment = TextAnchor.MiddleCenter;

                AddButton($"UNLOCK {nextSeason.name.ToUpperInvariant()}", frame, Grass,
                    new Vector2(0.5f, 0.25f), new Vector2(0.75f, 0.085f), AdvanceCareer);
            }
            else
            {
                var lockMsg = AddText($"PROMOTION REQUIRES {threshold} STARS\nEarn {threshold - earnedStars} more stars to unlock {_catalog.Data.seasons[_seasonIndex + 1].name}!",
                    frame, 15, new Color32(235, 90, 75, 255),
                    new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.44f));
                lockMsg.alignment = TextAnchor.MiddleCenter;

                AddButton("REPLAY FIXTURES", frame, Grass,
                    new Vector2(0.5f, 0.25f), new Vector2(0.72f, 0.08f), ShowLeagueFixtures);
            }

            AddButton("BACK TO FIXTURES", frame, new Color32(68, 79, 84, 255),
                new Vector2(0.5f, 0.15f), new Vector2(0.65f, 0.065f), ShowLeagueFixtures);
        }

        private int CountUnusedBenchPlayers()
        {
            if (_ownedPlayerIds == null || _lineupIds == null) return 0;
            int count = 0;
            for (int i = 0; i < _ownedPlayerIds.Count; i++)
            {
                if (Array.IndexOf(_lineupIds, _ownedPlayerIds[i]) < 0)
                    count++;
            }
            return count;
        }

        private void ResetMatchTrackingState()
        {
            _matchSpeed = PlayerPrefs.GetInt("diceSoccer.fastSpeed", 0) == 1 ? 2f : 1f;
            _currentResultRecorded = false;
            _shakeArmed = true;
            _shakeQuietSince = -1f;
            _hasAccelerationSample = false;
            _isKickoffPlayed = false;
            _lastSubstitutedSlot = -1;
            _remainingSubs = 3;
            _homeShirleyUsed = false;
            _awayShirleyUsed = false;
            _matchMaxHomeRoll = 0;
            _matchHadZeroHomeRoll = false;
            _matchHadZeroAwayRoll = false;
            _matchLastMinGoal = false;
            _matchLastMinMiss = false;
            _matchLastMinSave = false;
            _matchLastMinConcede = false;
            _matchLastMinEqualizer = false;
            _matchLastMinTieBreaker = false;
            _pendingUnlockedPlayerIds.Clear();
            _pendingUnlockedAchievements.Clear();
        }

        private void StartCurrentFixture()
        {
            if (_catalog == null || _seasonFixtures == null ||
                _selectedFixtureIndex < 0 || _selectedFixtureIndex >= _seasonFixtures.Length)
                throw new InvalidOperationException("There is no current competition fixture to start.");

            _currentFixture = _seasonFixtures[_selectedFixtureIndex];
            ResetMatchTrackingState();
            var homeLineup = _catalog.ResolveLineup(_lineupIds);
            _match = new MatchSimulation(
                homeLineup,
                _catalog.ResolveLineup(_currentFixture.lineup),
                _currentFixture.teamName,
                _seasonIndex,
                _equippedEntourageIds.ToArray(),
                _currentFixture.entourageLineup,
                0,
                _currentFixture.possessionBonus,
                CountUnusedBenchPlayers(),
                0);

            if (_seasonIndex == 0 && _selectedFixtureIndex == 0 && PlayerPrefs.GetInt("diceSoccer.tutorial.11", 0) == 0)
            {
                ShowTutorialPopup(11, () => ShowLoadingWithTip(() => ShowMatchScreen(homeLineup)));
            }
            else
            {
                ShowLoadingWithTip(() => ShowMatchScreen(homeLineup));
            }
        }

        private void ShowLoadingWithTip(Action onComplete)
        {
            if (Application.isBatchMode)
            {
                onComplete?.Invoke();
                return;
            }
            StartCoroutine(RunLoadingWithTipRoutine(onComplete));
        }

        private IEnumerator RunLoadingWithTipRoutine(Action onComplete)
        {
            BuildLoadingTipScreen(null);
            float elapsed = 0f;
            const float minDuration = 1.05f;
            bool tapped = false;
            var tapSkipGo = new GameObject("LoadingSkipBtn", typeof(RectTransform), typeof(Button));
            tapSkipGo.transform.SetParent(_safeArea, false);
            var skipRect = tapSkipGo.GetComponent<RectTransform>();
            skipRect.anchorMin = Vector2.zero;
            skipRect.anchorMax = Vector2.one;
            skipRect.offsetMin = skipRect.offsetMax = Vector2.zero;
            var skipBtn = tapSkipGo.GetComponent<Button>();
            skipBtn.transition = Selectable.Transition.None;
            skipBtn.onClick.AddListener(() => { if (elapsed > 0.2f) tapped = true; });

            var willyBody = _safeArea.Find("LoadingFrame/WillyBody")?.GetComponent<Image>();
            PlayerBodySprites.EnsureLoadingAtlasSliced(out var bodyFrames, out _, out _);

            while (elapsed < minDuration && !tapped)
            {
                elapsed += Time.deltaTime;
                if (willyBody != null && bodyFrames != null && bodyFrames.Length > 0)
                {
                    int idx = (int)(elapsed * 15f) % bodyFrames.Length;
                    willyBody.sprite = bodyFrames[idx];
                }
                yield return null;
            }
            onComplete?.Invoke();
        }

        /// <summary>
        /// Renders the 2012 iOS Willy-juggling loading screen + speech bubble TIP overlay.
        /// Publicly callable for UI screenshot verification as well as match transitions.
        /// </summary>
        public void BuildLoadingTipScreen(string forcedTip = null)
        {
            _catalog ??= GameCatalog.Load();
            ClearCanvas();
            AddStretchPanel("LoadingBG", _canvas.transform, new Color32(28, 120, 42, 255));
            _safeArea.SetAsLastSibling();
            var frame = CreateCenteredReferenceFrame("LoadingFrame");
            var bg = AddResourceImage("PitchBG", "Original/images/GreenField", frame,
                Vector2.zero, Vector2.one);
            bg.preserveAspect = false;
            bg.raycastTarget = false;

            PlayerBodySprites.EnsureLoadingAtlasSliced(out var bodyFrames, out var headSprite, out var bubbleSprite);

            var bodyGo = new GameObject("WillyBody", typeof(RectTransform), typeof(Image));
            bodyGo.transform.SetParent(frame, false);
            var bodyRect = bodyGo.GetComponent<RectTransform>();
            bodyRect.anchorMin = bodyRect.anchorMax = new Vector2(0.66f, 0.44f);
            bodyRect.sizeDelta = new Vector2(150f, 150f);
            var bodyImg = bodyGo.GetComponent<Image>();
            bodyImg.sprite = (bodyFrames != null && bodyFrames.Length > 0) ? bodyFrames[0] : null;
            bodyImg.preserveAspect = true;
            bodyImg.raycastTarget = false;

            var headGo = new GameObject("WillyHead", typeof(RectTransform), typeof(Image));
            headGo.transform.SetParent(bodyGo.transform, false);
            var headRect = headGo.GetComponent<RectTransform>();
            headRect.anchorMin = headRect.anchorMax = new Vector2(0.5f, 0.5f);
            headRect.anchoredPosition = new Vector2(2f, 54f);
            headRect.sizeDelta = new Vector2(96f, 96f);
            var headImg = headGo.GetComponent<Image>();
            headImg.sprite = headSprite;
            headImg.preserveAspect = true;
            headImg.raycastTarget = false;

            var bubbleGo = new GameObject("TipBubble", typeof(RectTransform), typeof(Image));
            bubbleGo.transform.SetParent(frame, false);
            var bRect = bubbleGo.GetComponent<RectTransform>();
            bRect.anchorMin = bRect.anchorMax = new Vector2(0.42f, 0.72f);
            bRect.sizeDelta = new Vector2(400f, 240f);
            var bImg = bubbleGo.GetComponent<Image>();
            bImg.sprite = bubbleSprite;
            bImg.preserveAspect = true;
            bImg.raycastTarget = false;

            string[] tips = _catalog?.Data?.tips;
            string tipBody = !string.IsNullOrEmpty(forcedTip)
                ? forcedTip
                : (tips != null && tips.Length > 0 ? tips[UnityEngine.Random.Range(0, tips.Length)] : "Tap the field or shake to roll!");
            var tipText = AddText("TIP: " + tipBody, bubbleGo.transform, 15, Ink,
                new Vector2(0.11f, 0.24f), new Vector2(0.91f, 0.88f));
            tipText.fontStyle = FontStyle.Bold;
            tipText.alignment = TextAnchor.MiddleCenter;

            var loadingLbl = AddText("LOADING...", frame, 24, Paper,
                new Vector2(0.15f, 0.16f), new Vector2(0.85f, 0.25f));
            loadingLbl.fontStyle = FontStyle.Bold;
            loadingLbl.alignment = TextAnchor.MiddleCenter;
        }

        private void ShowMatchScreen(PlayerData[] homeLineup)
        {
            AudioManager.Instance.PlayCrowdChant(0.35f);
            ClearCanvas();

            // Solid black letterbox background behind safe area
            var bgObj = AddStretchPanel("MatchLetterboxBG", _canvas.transform, Color.black);
            bgObj.raycastTarget = false;
            _safeArea.SetAsLastSibling();

            // 1. Pitch Field (fills full safe area edge-to-edge):
            var fieldGo = new GameObject("FieldContainer", typeof(RectTransform), typeof(RectMask2D));
            fieldGo.transform.SetParent(_safeArea, false);
            var fieldRect = fieldGo.GetComponent<RectTransform>();
            fieldRect.anchorMin = Vector2.zero;
            fieldRect.anchorMax = Vector2.one;
            fieldRect.offsetMin = fieldRect.offsetMax = Vector2.zero;
            _field = fieldRect;

            // Pitch Root for horizontal camera panning (built in 1920x960 local coordinates, scaled dynamically by UpdateMatchFieldScale):
            var pitchRootGo = new GameObject("FieldPitchRoot", typeof(RectTransform));
            pitchRootGo.transform.SetParent(_field, false);
            _fieldPitchRoot = pitchRootGo.GetComponent<RectTransform>();
            _fieldPitchRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _fieldPitchRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _fieldPitchRoot.sizeDelta = new Vector2(1920f, 960f);
            _fieldPitchRoot.anchoredPosition = new Vector2(GetPitchOffsetForLocation(_match.Location), 0f);

            // Left half of pitch (goal on left, center circle on right):
            var leftFieldGo = new GameObject("FieldLeft", typeof(RectTransform), typeof(Image));
            leftFieldGo.transform.SetParent(_fieldPitchRoot, false);
            var leftRect = leftFieldGo.GetComponent<RectTransform>();
            leftRect.anchorMin = leftRect.anchorMax = new Vector2(0.5f, 0.5f);
            leftRect.pivot = new Vector2(1f, 0.5f);
            leftRect.sizeDelta = new Vector2(960f, 960f);
            leftRect.anchoredPosition = Vector2.zero;
            var leftImg = leftFieldGo.GetComponent<Image>();
            leftImg.sprite = GetUiSprite("Original/sprites/Season0Field2(1.1)/Field");
            leftImg.preserveAspect = false;
            leftImg.raycastTarget = false;

            // Right half of pitch (flipped horizontally to form full pitch):
            var rightFieldGo = new GameObject("FieldRight", typeof(RectTransform), typeof(Image));
            rightFieldGo.transform.SetParent(_fieldPitchRoot, false);
            var rightRect = rightFieldGo.GetComponent<RectTransform>();
            rightRect.anchorMin = rightRect.anchorMax = new Vector2(0.5f, 0.5f);
            rightRect.pivot = new Vector2(1f, 0.5f);
            rightRect.sizeDelta = new Vector2(960f, 960f);
            rightRect.anchoredPosition = Vector2.zero;
            rightRect.localScale = new Vector3(-1f, 1f, 1f);
            var rightImg = rightFieldGo.GetComponent<Image>();
            rightImg.sprite = GetUiSprite("Original/sprites/Season0Field2(1.1)/Field");
            rightImg.preserveAspect = false;
            rightImg.raycastTarget = false;

            // Authentic Season Field Objects & Scenery (Seasons 0 to 4):
            CreateSeasonFieldObjects(_seasonIndex, _fieldPitchRoot, 1f);

            // Sky clouds floating gently above field:
            var matchClouds = AddResourceImage("PitchSkyClouds",
                "Original/sprites/StartScreenSS/StartScreenClouds",
                _fieldPitchRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            matchClouds.rectTransform.anchoredPosition = new Vector2(0f, 380f);
            matchClouds.rectTransform.sizeDelta = new Vector2(800f, 180f);
            matchClouds.color = new Color(1f, 1f, 1f, 0.55f);
            matchClouds.raycastTarget = false;
            StartCoroutine(AnimateCloudsDrift(matchClouds.rectTransform, 10f));

            UpdateMatchFieldScale();

            // 2. Play Area Container (Fixed 640x960 reference box centered in pitch, preserving 1:1 tactical layout in all aspect ratios)
            var playRootGo = new GameObject("MatchPlayRoot", typeof(RectTransform));
            playRootGo.transform.SetParent(_field, false);
            _matchPlayRoot = playRootGo.GetComponent<RectTransform>();
            _matchPlayRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _matchPlayRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _matchPlayRoot.sizeDelta = new Vector2(640f, 960f);
            _matchPlayRoot.anchoredPosition = Vector2.zero;

            // Soccer ball with shadow (sized to authentic proportions):
            var ballShadow = AddResourceImage("BallShadow", "Original/sprites/PlayerBody3/GameSoccerBallShadow",
                _matchPlayRoot, Vector2.zero, Vector2.zero);
            ballShadow.rectTransform.sizeDelta = new Vector2(36f, 18f);
            ballShadow.raycastTarget = false;
            _ballShadow = ballShadow;

            var ball = AddResourceImage("Ball", "Original/sprites/PlayerBody3/GameSoccerBall",
                _matchPlayRoot, Vector2.zero, Vector2.zero);
            ball.rectTransform.sizeDelta = new Vector2(38f, 38f);
            ball.raycastTarget = false;
            _ballMarker = ball;

            // Formation players:
            var awayLineup = _catalog.ResolveLineup(_currentFixture.lineup);
            _homePlayers = AddFormationPlayers(homeLineup, _matchPlayRoot, true);
            _awayPlayers = AddFormationPlayers(awayLineup, _matchPlayRoot, false);

            // Ball and shadow always stay crisp above pitch elements and boots
            if (_ballShadow != null) _ballShadow.transform.SetAsLastSibling();
            if (_ballMarker != null) _ballMarker.transform.SetAsLastSibling();

            if (_idleBobbingCoroutine != null) StopCoroutine(_idleBobbingCoroutine);
            _idleBobbingCoroutine = StartCoroutine(IdlePlayerBobbing());

            // Dice on Field:
            CreateFieldDice(_matchPlayRoot);

            // Duel Totals at bottom-left & bottom-right beside bottom dice (matching original 2012 iOS layout):
            _homeTotalText = AddText("", _matchPlayRoot, 50, new Color32(50, 45, 40, 255),
                new Vector2(0.135f, 0.24f), new Vector2(0.135f, 0.24f));
            _homeTotalText.rectTransform.sizeDelta = new Vector2(100f, 76f);
            _homeTotalText.fontStyle = FontStyle.Bold;
            _homeTotalText.alignment = TextAnchor.MiddleCenter;
            _homeTotalText.enabled = false;

            _awayTotalText = AddText("", _matchPlayRoot, 50, new Color32(50, 45, 40, 255),
                new Vector2(0.865f, 0.24f), new Vector2(0.865f, 0.24f));
            _awayTotalText.rectTransform.sizeDelta = new Vector2(100f, 76f);
            _awayTotalText.fontStyle = FontStyle.Bold;
            _awayTotalText.alignment = TextAnchor.MiddleCenter;
            _awayTotalText.enabled = false;

            // Authentic Buff Belt Tray (matching media_1792577723570_15c588e0.png):
            _buffBeltGo = new GameObject("BuffBelt", typeof(RectTransform), typeof(Image));
            _buffBeltGo.transform.SetParent(_safeArea, false);
            var bbRect = _buffBeltGo.GetComponent<RectTransform>();
            bbRect.anchorMin = new Vector2(0.5f, 0f);
            bbRect.anchorMax = new Vector2(0.5f, 0f);
            bbRect.pivot = new Vector2(0.5f, 0f);
            bbRect.anchoredPosition = Vector2.zero;
            bbRect.sizeDelta = new Vector2(640f, 222f);
            var bbImg = _buffBeltGo.GetComponent<Image>();
            bbImg.sprite = GetUiSprite("Original/images/BuffBelt");
            bbImg.preserveAspect = false;
            bbImg.raycastTarget = false;

            _buffBeltMedal = AddResourceImage("BuffMedal", "Original/sprites/Buff6/MedalWhiteRing", _buffBeltGo.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            _buffBeltMedal.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _buffBeltMedal.rectTransform.anchoredPosition = new Vector2(0f, -55f);
            _buffBeltMedal.rectTransform.sizeDelta = new Vector2(130f, 130f);
            _buffBeltMedal.preserveAspect = true;
            _buffBeltMedal.raycastTarget = false;

            // Dynamic badge overlay text (empty for self-contained BuffMedal* & EntourageAbility* sprites; only used for DisplayBuff* templates such as ID 18)
            var medalBadgeText = AddText(string.Empty, _buffBeltMedal.transform, 40, Color.white,
                new Vector2(0.36f, 0.05f), new Vector2(0.94f, 0.95f));
            medalBadgeText.fontStyle = FontStyle.Bold;
            medalBadgeText.alignment = TextAnchor.MiddleCenter;
            medalBadgeText.raycastTarget = false;
            var medalOutline = medalBadgeText.gameObject.AddComponent<Outline>();
            medalOutline.effectColor = new Color(0f, 0f, 0f, 0.92f);
            medalOutline.effectDistance = new Vector2(2f, -2f);
            _buffBeltBadgeText = medalBadgeText;

            _buffBeltText = AddText("PLEASE SELECT TARGET", _buffBeltGo.transform, 24, Paper,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            _buffBeltText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _buffBeltText.rectTransform.anchoredPosition = new Vector2(0f, 32f);
            _buffBeltText.rectTransform.sizeDelta = new Vector2(520f, 40f);
            _buffBeltText.alignment = TextAnchor.MiddleCenter;
            _buffBeltText.fontStyle = FontStyle.Bold;
            _buffBeltGo.SetActive(false);

            // Shake button & prompt dynamically centered between duelists:
            int matchLoc = _match != null ? _match.Location : 0;
            float initialShakeX = (matchLoc == 2) ? 0.475f : (matchLoc == -2 ? 0.525f : 0.50f);
            _shakePrompt = AddResourceImage("ShakePrompt", "Original/sprites/Prompt1/Shake",
                _matchPlayRoot, new Vector2(initialShakeX, 0.50f), new Vector2(initialShakeX, 0.50f));
            _shakePrompt.rectTransform.sizeDelta = new Vector2(215f, 176f);
            _shakePrompt.preserveAspect = true;
            _shakePrompt.raycastTarget = true;
            var shakeBtn = _shakePrompt.gameObject.AddComponent<Button>();
            shakeBtn.transition = Selectable.Transition.None;
            shakeBtn.onClick.AddListener(PlayEncounter);

            // Invisible full-field tap trigger for rolling:
            var tapFieldGo = new GameObject("FieldTapTrigger", typeof(RectTransform), typeof(Image), typeof(Button));
            tapFieldGo.transform.SetParent(_field, false);
            tapFieldGo.transform.SetAsFirstSibling();
            var tapFieldImg = tapFieldGo.GetComponent<Image>();
            tapFieldImg.color = Color.clear;
            tapFieldImg.raycastTarget = true;
            var tapFieldRect = tapFieldGo.GetComponent<RectTransform>();
            tapFieldRect.anchorMin = Vector2.zero;
            tapFieldRect.anchorMax = Vector2.one;
            tapFieldRect.offsetMin = tapFieldRect.offsetMax = Vector2.zero;
            var tapFieldBtn = tapFieldGo.GetComponent<Button>();
            tapFieldBtn.transition = Selectable.Transition.None;
            tapFieldBtn.onClick.AddListener(PlayEncounter);
            _encounterButton = tapFieldBtn;

            UpdatePlayerPositions();
            SetBallPosition(_match.Location, _match.Possession, false);

            // Keep match report tracker (hidden to avoid pitch clutter):
            _matchReport = AddText("", _field, 12, Ink, Vector2.zero, Vector2.zero);
            _matchReport.enabled = false;

            // 2. Top HUD: Authentic Scoreboard (scoreboardBG sits at the very top):
            var scoreboard = AddResourceImage("MatchScoreboard",
                "Original/images/scoreboardBG", _safeArea,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            _matchScoreboard = scoreboard;
            scoreboard.rectTransform.pivot = new Vector2(0.5f, 1f);
            scoreboard.rectTransform.anchoredPosition = Vector2.zero;
            scoreboard.rectTransform.sizeDelta = new Vector2(640f, 150f);
            scoreboard.preserveAspect = false;
            scoreboard.raycastTarget = false;

            // Home Team Crest Shield centered in the left yellow box of scoreboardBG:
            // Box is x=15..95 (center 55), y=5..85 (center 45) -> relative to top-center: (-265, -45)
            var homeCrestShield = AddResourceImage("HomeShield", $"Original/images/CustomCrest{_currentCrestId}", scoreboard.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            homeCrestShield.rectTransform.anchoredPosition = new Vector2(-265f, -45f);
            homeCrestShield.rectTransform.sizeDelta = new Vector2(74f, 74f);
            homeCrestShield.preserveAspect = true;
            homeCrestShield.raycastTarget = false;

            var homeTitle = _isLocalMultiplayer ? "PLAYER 1" : (!string.IsNullOrEmpty(_teamName) ? _teamName.ToUpperInvariant() : "SANUKU");
            var homeNameText = AddText(homeTitle, scoreboard.transform, 12, Ink,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            homeNameText.rectTransform.anchoredPosition = new Vector2(-265f, -107f);
            homeNameText.rectTransform.sizeDelta = new Vector2(110f, 24f);
            homeNameText.fontStyle = FontStyle.Bold;
            homeNameText.alignment = TextAnchor.MiddleCenter;

            // Away Team Crest centered in the right red box of scoreboardBG:
            // Box is x=545..625 (center 585), y=5..85 (center 45) -> relative to top-center: (+265, -45)
            var awayCrestBody = AddResourceImage("AwayCrestBody", "Original/sprites/EnemyCrest1/EnemyCrest1Body", scoreboard.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            awayCrestBody.rectTransform.anchoredPosition = new Vector2(265f, -45f);
            awayCrestBody.rectTransform.sizeDelta = new Vector2(74f, 74f);
            awayCrestBody.preserveAspect = true;
            awayCrestBody.raycastTarget = false;

            var awayCrestDesign = AddResourceImage("AwayDesign", "Original/sprites/EnemyCrest1/EnemyCrest1Design", awayCrestBody.transform,
                Vector2.zero, Vector2.one);
            awayCrestDesign.rectTransform.offsetMin = awayCrestDesign.rectTransform.offsetMax = Vector2.zero;
            awayCrestDesign.preserveAspect = true;
            awayCrestDesign.raycastTarget = false;

            var awayNameText = AddText(_currentFixture.teamName.ToUpperInvariant(), scoreboard.transform, 11, Ink,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            awayNameText.rectTransform.anchoredPosition = new Vector2(265f, -107f);
            awayNameText.rectTransform.sizeDelta = new Vector2(110f, 24f);
            awayNameText.fontStyle = FontStyle.Bold;
            awayNameText.alignment = TextAnchor.MiddleCenter;
            awayNameText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Center Scoreboard Scores (center box is at x=0, y=-48 from top-center):
            _matchScore = AddText($"{_match.HomeScore} - {_match.AwayScore}", scoreboard.transform, 44, Ink,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            _matchScore.rectTransform.anchoredPosition = new Vector2(0f, -48f);
            _matchScore.rectTransform.sizeDelta = new Vector2(180f, 50f);
            _matchScore.fontStyle = FontStyle.Bold;
            _matchScore.alignment = TextAnchor.MiddleCenter;
            _matchScore.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Pause button at center directly docked under score (overlapping bottom border):
            var pauseImg = AddResourceImage("Pause", "Original/images/PauseButton", scoreboard.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            var pauseRect = pauseImg.rectTransform;
            pauseRect.anchoredPosition = new Vector2(0f, -124f);
            pauseRect.sizeDelta = new Vector2(58f, 52f);
            pauseImg.preserveAspect = true;
            pauseImg.raycastTarget = true;
            var pauseBtn = pauseImg.gameObject.AddComponent<Button>();
            pauseBtn.targetGraphic = pauseImg;
            pauseBtn.onClick.AddListener(() =>
            {
                AudioManager.Instance.PlayTap();
                ShowPauseMenu();
            });
            pauseBtn.transition = Selectable.Transition.SpriteSwap;
            var pauseSpriteState = pauseBtn.spriteState;
            pauseSpriteState.highlightedSprite = GetUiSprite("Original/images/PauseButtonPressed");
            pauseSpriteState.pressedSprite = GetUiSprite("Original/images/PauseButtonPressed");
            pauseBtn.spriteState = pauseSpriteState;

            // Authentic in-match digital clock centered in bottom grass field area (matching 2012 iOS original)
            var clockGo = new GameObject("MatchClock", typeof(RectTransform), typeof(Text), typeof(Shadow));
            clockGo.transform.SetParent(_field, false);
            var clockRect = clockGo.GetComponent<RectTransform>();
            clockRect.anchorMin = new Vector2(0.5f, 0.038f);
            clockRect.anchorMax = new Vector2(0.5f, 0.038f);
            clockRect.pivot = new Vector2(0.5f, 0.5f);
            clockRect.anchoredPosition = Vector2.zero;
            clockRect.sizeDelta = new Vector2(240f, 44f);

            _matchClock = clockGo.GetComponent<Text>();
            _matchClock.font = GetBuiltinFont();
            _matchClock.fontSize = 28;
            _matchClock.fontStyle = FontStyle.Bold;
            _matchClock.alignment = TextAnchor.MiddleCenter;
            _matchClock.color = new Color32(230, 245, 255, 240);
            _matchClock.raycastTarget = false;
            _matchClock.text = _match != null ? _match.ClockText() : "15:00";

            var clockShadow = clockGo.GetComponent<Shadow>();
            clockShadow.effectColor = new Color32(40, 110, 140, 180);
            clockShadow.effectDistance = new Vector2(1.5f, -1.5f);

            // Authentic In-Match Entourage Slots under Left (Home) and Right (Away) scoreboard crests:
            BuildScoreboardEntourageSlots(scoreboard.transform, _match.HomeEntourages, true);
            BuildScoreboardEntourageSlots(scoreboard.transform, _match.AwayEntourages, false);

            // Interactive On-Field Substitution Trigger Button (bottom-left of safe area):
            BuildInMatchSubstitutionTrigger();

            // Overlay container for animations (Kickoff, Goal, Save, etc.):
            var overlayGo = new GameObject("OverlayContainer", typeof(RectTransform), typeof(RectMask2D));
            overlayGo.transform.SetParent(_safeArea, false);
            _overlayContainer = overlayGo.GetComponent<RectTransform>();
            _overlayContainer.anchorMin = Vector2.zero;
            _overlayContainer.anchorMax = Vector2.one;
            _overlayContainer.offsetMin = _overlayContainer.offsetMax = Vector2.zero;

            // Keep BuffBelt above HUD badges (SubTriggerBtn) while still below Pause/Modal overlays:
            if (_buffBeltGo != null)
                _buffBeltGo.transform.SetAsLastSibling();
            _overlayContainer.SetAsLastSibling();

            Time.timeScale = _matchSpeed;

            // Kick-off banner:
            if (!_isKickoffPlayed)
            {
                _isKickoffPlayed = true;
                StartCoroutine(PlayKickOffAnimation());
            }

            // Poof smoke if substituted:
            if (_lastSubstitutedSlot >= 0)
            {
                StartCoroutine(PlayPoofAnimation(_lastSubstitutedSlot));
                _lastSubstitutedSlot = -1;
            }
        }

        private static Color32 GetCardBorderColor(int playerId)
        {
            // Original IPA v1.2.5 Thumb-2 (0x5b358 .. 0x5b7dc) little-endian ccColor3B {r, g, b}:
            // 1..16:  0x0000e9aa -> (170, 233, 0)   Green
            // 17..32: 0x00fca757 -> (87, 167, 252)  Blue
            // 33..48: 0x003737fc -> (252, 55, 55)   Red
            // 49..64: 0x002dccfc -> (252, 204, 45)  Yellow/Gold
            // 65..80: 0x00c9c9c9 -> (201, 201, 201) Silver
            // >80:    0x00ffffff -> (255, 255, 255) Platinum/White
            if (playerId <= 16) return new Color32(170, 233, 0, 255);
            if (playerId <= 32) return new Color32(87, 167, 252, 255);
            if (playerId <= 48) return new Color32(252, 55, 55, 255);
            if (playerId <= 64) return new Color32(252, 204, 45, 255);
            if (playerId <= 80) return new Color32(201, 201, 201, 255);
            return new Color32(255, 255, 255, 255);
        }

        private static Color32 GetPositionColor(string position)
        {
            // ARMv7 0x5a420 .. 0x5a4f6:
            switch (position)
            {
                case "GK": return new Color32(40, 40, 40, 255);
                case "FWD": return new Color32(246, 50, 50, 255);
                case "DEF": return new Color32(0, 100, 255, 255);
                case "MID": return new Color32(50, 180, 50, 255);
                default: return new Color32(80, 80, 80, 255);
            }
        }

        private static string GetPlayerNumberSpritePath(int digit)
        {
            return digit <= 4
                ? $"Original/sprites/Card3/PlayerNumber{digit}"
                : $"Original/sprites/Card6/PlayerNumber{digit}";
        }

        private static string GetDiceSideSpritePath(int sideValue)
        {
            if (sideValue >= 16)
            {
                var abilityPath = $"Original/sprites/Card3/DiceFaceAbility{sideValue}";
                if (TryGetUiSprite(abilityPath) != null)
                    return abilityPath;
            }
            int clamped = Mathf.Clamp(sideValue, 1, 14);
            return $"Original/sprites/Card6/DiceNumber{clamped:00}";
        }

        private static int ResolveAbilitySpriteId(int abilityId)
        {
            return abilityId switch
            {
                29 or 30 => 28,
                50 or 55 => 46,
                52 => 42,
                53 or 67 or 68 => 33,
                54 or 66 or 74 or 75 => 39,
                56 or 58 => 41,
                57 => 22,
                59 => 27,
                61 or 62 or 63 => 51,
                69 or 71 => 34,
                70 => 34,
                72 or 73 => 37,
                76 or 78 or 79 or 81 => 43,
                80 => 47,
                _ => abilityId
            };
        }

        private static string GetSuperpowerIconPath(int superpowerId)
        {
            if (superpowerId <= 0) return null;
            int spriteId = ResolveAbilitySpriteId(superpowerId);
            var path = $"Original/sprites/Card3/DiceFaceAbility{spriteId}";
            return TryGetUiSprite(path) != null ? path : null;
        }

        private static string ResolvePlayerHeadPath(int id)
        {
            if (id >= 1000)
            {
                var p = $"Original/images/PlayerHead{id}";
                if (TryGetUiSprite(p) != null) return p;
                p = $"Original/sprites/Prompt1/PlayerHead{id}";
                if (TryGetUiSprite(p) != null) return p;
                return "Original/images/PlayerHead1012";
            }
            var p1 = $"Original/images/PlayerHead{id:00}";
            if (TryGetUiSprite(p1) != null) return p1;
            var p2 = $"Original/sprites/Prompt1/PlayerHead{id:00}";
            if (TryGetUiSprite(p2) != null) return p2;
            var p3 = $"Original/sprites/Prompt2/PlayerHead{id:00}";
            if (TryGetUiSprite(p3) != null) return p3;
            return "Original/images/PlayerHead01";
        }

        private static string GetPlayerFigurePath(PlayerData player, int fallbackId = 1)
        {
            int id = 1;
            if (player != null)
            {
                if (player.id >= 1 && player.id <= 96)
                    id = player.id;
                else if (player.id >= 1000)
                    id = ((player.id - 1000) % 96) + 1;
                else if (player.imageId > 0 && player.imageId <= 96)
                    id = player.imageId;
            }
            else if (fallbackId >= 1 && fallbackId <= 96)
            {
                id = fallbackId;
            }

            var path = $"Original/sprites/PlayerFullFigureSS/PlayerFullFigure{id:000}";
            if (TryGetUiSprite(path) != null)
                return path;
            return "Original/sprites/PlayerFullFigureSS/PlayerFullFigure001";
        }

        private static string GetPlayerFigurePath(int playerId)
        {
            var catalog = GameCatalog.Load();
            var p = catalog != null ? catalog.GetPlayer(playerId) : null;
            return GetPlayerFigurePath(p, playerId);
        }

        private static float GetPlayerFigureVisualScale(int playerId)
        {
            // Junior squad players (1, 2, 3, 4, 6) were drawn with ~100px blank headroom in the 340px canvas.
            // Calibrate visual scale so they stand uniformly alongside adult/varsity figures without extreme discrepancy.
            switch (playerId)
            {
                case 1: return 1.25f; // Willy the Kid (GK)
                case 2: return 1.28f; // Earnest Mulligan (DEF)
                case 3: return 1.22f; // Jo Lee-Roger (DEF)
                case 4: return 1.26f; // Sherman 'Tank' (DEF)
                case 6: return 1.24f; // David Little (FWD)
                default: return 1.0f;
            }
        }

        private GameObject CreatePlayerCardFront(PlayerData player, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, bool isHire = false, bool isInteractive = true,
            UnityEngine.Events.UnityAction onClick = null, bool isLocked = false, Vector2? sizeDelta = null)
        {
            var root = new GameObject($"CardFront_{player.id}", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            if (sizeDelta.HasValue)
            {
                rect.anchorMin = rect.anchorMax = anchorMin;
                rect.sizeDelta = sizeDelta.Value;
                rect.anchoredPosition = Vector2.zero;
            }
            else
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }

            var border = AddResourceImage("Border", "Original/sprites/PlayerCardBG/CardBorder", root.transform,
                Vector2.zero, Vector2.one);
            border.color = isLocked ? new Color32(160, 165, 170, 255) : GetCardBorderColor(player.id);
            border.preserveAspect = false;

            var field = AddResourceImage("Field", "Original/sprites/PlayerCardBG/CardField", border.transform,
                new Vector2(0.045f, 0.035f), new Vector2(0.955f, 0.965f));
            field.preserveAspect = false;

            // Number badge (Top Left)
            var numBase = AddResourceImage("NumberBase", "Original/sprites/Card3/PlayerNumberBase", field.transform,
                new Vector2(0.03f, 0.80f), new Vector2(0.21f, 0.96f));
            numBase.color = GetPositionColor(player.position);
            numBase.preserveAspect = true;

            var numOutline = AddResourceImage("NumberOutline", "Original/sprites/Card3/PlayerNumberOutline", numBase.transform,
                Vector2.zero, Vector2.one);
            numOutline.preserveAspect = true;

            if (player.id < 10)
            {
                var dPath = GetPlayerNumberSpritePath(player.id % 10);
                AddResourceImage("Digit", dPath, numBase.transform,
                    new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f)).preserveAspect = true;
            }
            else
            {
                var d1Path = GetPlayerNumberSpritePath(player.id / 10);
                var d2Path = GetPlayerNumberSpritePath(player.id % 10);
                AddResourceImage("Digit1", d1Path, numBase.transform,
                    new Vector2(0.06f, 0.12f), new Vector2(0.52f, 0.88f)).preserveAspect = true;
                AddResourceImage("Digit2", d2Path, numBase.transform,
                    new Vector2(0.48f, 0.12f), new Vector2(0.94f, 0.88f)).preserveAspect = true;
            }

            // Position badge (Top Right)
            var posPath = $"Original/sprites/Card6/{player.position}Type";
            var posImg = AddResourceImage("PositionType", posPath, field.transform,
                new Vector2(0.79f, 0.80f), new Vector2(0.97f, 0.96f));
            posImg.preserveAspect = true;

            // Name Tag Banner (Top Center between number & position - non-overlapping and masked)
            var nameBox = new GameObject("NameBox", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            nameBox.transform.SetParent(field.transform, false);
            var nameBoxRect = nameBox.GetComponent<RectTransform>();
            nameBoxRect.anchorMin = new Vector2(0.225f, 0.812f);
            nameBoxRect.anchorMax = new Vector2(0.775f, 0.945f);
            nameBoxRect.offsetMin = nameBoxRect.offsetMax = Vector2.zero;
            var nameBoxImg = nameBox.GetComponent<Image>();
            nameBoxImg.color = new Color32(245, 246, 248, 255);
            var nameOutline = nameBox.AddComponent<Outline>();
            nameOutline.effectColor = new Color32(50, 50, 50, 255);
            nameOutline.effectDistance = new Vector2(1, -1);

            string rawName = (player.name ?? "").ToUpperInvariant().Trim();
            if (rawName.Length > 8 && rawName.Contains(" "))
            {
                int spaceIdx = rawName.IndexOf(' ');
                rawName = rawName.Substring(0, spaceIdx) + "\n" + rawName.Substring(spaceIdx + 1);
            }

            var nameText = AddText(rawName, nameBox.transform, 10, Ink,
                new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.fontStyle = FontStyle.Bold;
            nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameText.verticalOverflow = VerticalWrapMode.Truncate;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 6;
            nameText.resizeTextMaxSize = 11;
            nameText.lineSpacing = 0.85f;

            // Player figure & shadow standing on the grass (proportional to card size)
            var figPath = GetPlayerFigurePath(player.id);
            var figure = AddResourceImage("Figure", figPath, field.transform,
                new Vector2(0.24f, 0.20f), new Vector2(0.76f, 0.78f));
            figure.rectTransform.pivot = new Vector2(0.5f, 0.032f);
            figure.rectTransform.offsetMin = figure.rectTransform.offsetMax = Vector2.zero;
            float cardVScale = GetPlayerFigureVisualScale(player.id);
            figure.rectTransform.localScale = new Vector3(cardVScale, cardVScale, 1f);
            figure.preserveAspect = true;

            var shadowGo = new GameObject("FigureShadow", typeof(RectTransform), typeof(Image));
            shadowGo.transform.SetParent(figure.transform, false);
            var shadowRect = shadowGo.GetComponent<RectTransform>();
            shadowRect.anchorMin = Vector2.zero;
            shadowRect.anchorMax = Vector2.one;
            shadowRect.offsetMin = shadowRect.offsetMax = Vector2.zero;
            var shadowImg = shadowGo.GetComponent<Image>();
            shadowImg.sprite = GetUiSprite("Original/sprites/PlayerFullFigureSS/PlayerFullFigureShadow");
            shadowImg.color = new Color(0, 0, 0, 0.45f);
            shadowImg.preserveAspect = true;
            shadowImg.raycastTarget = false;
            shadowGo.transform.SetAsFirstSibling();

            // Superpower Box (Bottom Right, above dice bar)
            var spIconPath = GetSuperpowerIconPath(player.superpower);
            if (!string.IsNullOrEmpty(spIconPath))
            {
                var spBox = AddResourceImage("SPBox", "Original/sprites/Card6/SPBox", field.transform,
                    new Vector2(0.78f, 0.17f), new Vector2(0.96f, 0.31f));
                spBox.preserveAspect = true;

                var spIcon = AddResourceImage("SPIcon", spIconPath, spBox.transform,
                    new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
                spIcon.preserveAspect = true;
            }

            // Authentic Dice bar at bottom with 6 white square dice slots
            var diceBarRoot = new GameObject("DiceBar", typeof(RectTransform));
            diceBarRoot.transform.SetParent(field.transform, false);
            var barRect = diceBarRoot.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.045f, 0.030f);
            barRect.anchorMax = new Vector2(0.955f, 0.165f);
            barRect.offsetMin = barRect.offsetMax = Vector2.zero;

            // Layer 1: Base border with authentic white cutout slots and dividers
            var diceBarBorder = AddResourceImage("DiceBarBorder", "Original/sprites/Card3/DiceBarBorder", diceBarRoot.transform,
                Vector2.zero, Vector2.one);
            diceBarBorder.preserveAspect = false;
            diceBarBorder.raycastTarget = false;

            // Exact horizontal centers of the 6 slots in the 400px bar
            float[] slotCenterXs = { 0.125f, 0.2725f, 0.420f, 0.57125f, 0.720f, 0.86875f };

            // Layer 2: Dice pips / numbers / abilities centered inside each white square slot
            for (var i = 0; i < 6; i++)
            {
                float cx = slotCenterXs[i];
                var side = (player.diceSides != null && i < player.diceSides.Length) ? player.diceSides[i] : 1;
                var sidePath = GetDiceSideSpritePath(side);

                var sideImg = AddResourceImage($"DiceSide{i + 1}", sidePath, diceBarRoot.transform,
                    new Vector2(cx - 0.054f, 0.16f), new Vector2(cx + 0.054f, 0.84f));
                sideImg.preserveAspect = true;
                sideImg.raycastTarget = false;
            }

            // Layer 3: Position-colored bar overlay (transparent over slots, tinted over outer frame & dividers)
            var diceBarColour = AddResourceImage("DiceBarColour", "Original/sprites/Card3/DiceBarColour", diceBarRoot.transform,
                Vector2.zero, Vector2.one);
            diceBarColour.color = GetPositionColor(player.position);
            diceBarColour.preserveAspect = false;
            diceBarColour.raycastTarget = false;

            if (isLocked)
            {
                var lockImg = AddResourceImage("Locked", "Original/sprites/PlayerCardBG/CardLocked", border.transform,
                    Vector2.zero, Vector2.one);
                lockImg.preserveAspect = false;
            }
            else if (isHire)
            {
                var hireImg = AddResourceImage("HireMe", "Original/sprites/PlayerCardBG/CardHireMe", border.transform,
                    Vector2.zero, Vector2.one);
                hireImg.preserveAspect = false;
                var costText = AddText($"{player.cost:N0}", hireImg.transform, 22, Ink,
                    new Vector2(0.48f, 0.40f), new Vector2(0.84f, 0.49f));
                costText.alignment = TextAnchor.MiddleCenter;
                costText.fontStyle = FontStyle.Bold;
            }

            if (isInteractive)
            {
                border.raycastTarget = true;
                var btn = root.AddComponent<Button>();
                btn.targetGraphic = border;
                btn.onClick.AddListener(() => onClick?.Invoke());
            }
            else
            {
                border.raycastTarget = false;
            }

            return root;
        }

        private GameObject CreatePlayerCardBack(PlayerData player, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, bool isInteractive = true,
            UnityEngine.Events.UnityAction onClick = null)
        {
            var root = new GameObject($"CardBack_{player.id}", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var back = AddResourceImage("CardBack", "Original/sprites/PlayerCardBG/CardBack", root.transform,
                Vector2.zero, Vector2.one);
            back.preserveAspect = false;

            var nameText = AddText(player.name.ToUpperInvariant(), back.transform, 20, Ink,
                new Vector2(0.08f, 0.87f), new Vector2(0.92f, 0.94f));
            nameText.fontStyle = FontStyle.Bold;

            var roleText = AddText($"{player.position}  |  #{player.id}", back.transform, 14, Gold,
                new Vector2(0.1f, 0.81f), new Vector2(0.9f, 0.86f));
            roleText.fontStyle = FontStyle.Bold;

            // Primary Superpower
            var spBox = AddResourceImage("SPBox", "Original/sprites/Card6/SPBox", back.transform,
                new Vector2(0.08f, 0.65f), new Vector2(0.26f, 0.79f));
            spBox.preserveAspect = true;

            var spIconPath = GetSuperpowerIconPath(player.superpower);
            if (!string.IsNullOrEmpty(spIconPath))
            {
                var spIcon = AddResourceImage("SPIcon", spIconPath, spBox.transform,
                    new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
                spIcon.preserveAspect = true;
            }

            var ability = player.superpower > 0 ? _catalog.GetAbility(player.superpower) : null;
            var abName = ability != null ? ability.name : "Normal Player";
            var abDesc = ability != null ? ability.description : "No special superpower assigned to this player.";

            var title = AddText(abName.ToUpperInvariant(), back.transform, 15, Ink,
                new Vector2(0.28f, 0.72f), new Vector2(0.92f, 0.79f));
            title.alignment = TextAnchor.MiddleLeft;
            title.fontStyle = FontStyle.Bold;

            var desc = AddText(abDesc, back.transform, 11, Ink,
                new Vector2(0.28f, 0.58f), new Vector2(0.92f, 0.72f));
            desc.alignment = TextAnchor.UpperLeft;

            // Additional Abilities from dice sides
            var extraAbilities = new List<int>();
            if (player.diceSides != null)
            {
                foreach (var side in player.diceSides)
                {
                    if (side >= 16 && side != player.superpower && !extraAbilities.Contains(side))
                        extraAbilities.Add(side);
                }
            }

            var bioTop = 0.54f;
            if (extraAbilities.Count > 0)
            {
                var extraHeader = AddText("ADDITIONAL ABILITIES:", back.transform, 12, Gold,
                    new Vector2(0.08f, 0.49f), new Vector2(0.92f, 0.55f));
                extraHeader.alignment = TextAnchor.MiddleLeft;
                extraHeader.fontStyle = FontStyle.Bold;

                var extraAb = _catalog.GetAbility(extraAbilities[0]);
                if (extraAb != null)
                {
                    var extraText = AddText($"{extraAb.name}: {extraAb.description}", back.transform, 10, Ink,
                        new Vector2(0.08f, 0.40f), new Vector2(0.92f, 0.49f));
                    extraText.alignment = TextAnchor.UpperLeft;
                }
                bioTop = 0.38f;
            }

            // Player Bio
            var bioHeader = AddText("PLAYER BIO", back.transform, 13, Gold,
                new Vector2(0.08f, bioTop - 0.05f), new Vector2(0.92f, bioTop));
            bioHeader.alignment = TextAnchor.MiddleLeft;
            bioHeader.fontStyle = FontStyle.Bold;

            var bioContent = string.IsNullOrWhiteSpace(player.description)
                ? "No additional player biography available."
                : player.description;
            var bioText = AddText(bioContent, back.transform, 12, Ink,
                new Vector2(0.08f, 0.08f), new Vector2(0.92f, bioTop - 0.05f));
            bioText.alignment = TextAnchor.UpperLeft;

            if (isInteractive)
            {
                var btn = root.AddComponent<Button>();
                btn.onClick.AddListener(() => onClick?.Invoke());
            }

            return root;
        }

        private void ShowCardModal(PlayerData player, UnityEngine.Events.UnityAction onAction = null,
            string actionLabel = null, bool isHire = false)
        {
            var modalBackdrop = AddPanel("CardModalBackdrop", _safeArea,
                new Color(0f, 0f, 0f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.one);
            modalBackdrop.raycastTarget = true;

            var modalFrame = CreateCenteredReferenceFrame("CardModalFrame", modalBackdrop.transform);

            var cardContainer = new GameObject("CardContainer", typeof(RectTransform));
            cardContainer.transform.SetParent(modalFrame, false);
            var containerRect = cardContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.56f);
            containerRect.anchorMax = new Vector2(0.5f, 0.56f);
            containerRect.sizeDelta = new Vector2(340f, 510f); // 2:3 ratio

            GameObject frontObj = null;
            GameObject backObj = null;
            var isFlipping = false;
            var showingBack = false;

            UnityEngine.Events.UnityAction flipAction = () =>
            {
                if (isFlipping) return;
                StartCoroutine(FlipCardAnimation(containerRect, frontObj, backObj, !showingBack, () =>
                {
                    showingBack = !showingBack;
                    isFlipping = false;
                }));
                isFlipping = true;
            };

            frontObj = CreatePlayerCardFront(player, cardContainer.transform, Vector2.zero, Vector2.one,
                isHire, true, flipAction);
            backObj = CreatePlayerCardBack(player, cardContainer.transform, Vector2.zero, Vector2.one,
                true, flipAction);
            backObj.SetActive(false);

            AddText("TAP CARD TO FLIP", modalFrame, 14, Gold,
                new Vector2(0.1f, 0.25f), new Vector2(0.9f, 0.29f));

            if (onAction != null && !string.IsNullOrEmpty(actionLabel))
            {
                if (actionLabel.Contains("HIRE") || isHire)
                {
                    AddSpriteButton("HireBtn", "Original/images/BuyButton", "Original/images/BuyButtonPressed",
                        modalFrame, new Vector2(0.5f, 0.18f), new Vector2(0.48f, 0.075f), () =>
                        {
                            Destroy(modalBackdrop.gameObject);
                            onAction.Invoke();
                        });
                }
                else
                {
                    AddSpriteButton("AssignBtn", "Original/sprites/EnemyLineupSS1/AssignPlayerButton",
                        "Original/sprites/EnemyLineupSS1/AssignPlayerButtonPressed",
                        modalFrame, new Vector2(0.5f, 0.18f), new Vector2(0.48f, 0.075f), () =>
                        {
                            Destroy(modalBackdrop.gameObject);
                            onAction.Invoke();
                        });
                }
            }

            AddSpriteButton("CloseBtn", "Original/sprites/KitSelectionSS/DoneButton",
                "Original/sprites/KitSelectionSS/DoneButtonPressed", modalFrame,
                new Vector2(0.5f, 0.09f), new Vector2(0.36f, 0.065f), () =>
                {
                    Destroy(modalBackdrop.gameObject);
                });
        }

        private IEnumerator FlipCardAnimation(RectTransform cardRect, GameObject frontObj, GameObject backObj,
            bool toBack, Action onComplete)
        {
            AudioManager.Instance.PlayTap();
            var duration = 0.14f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                cardRect.localScale = new Vector3(Mathf.Lerp(1f, 0f, t), 1f, 1f);
                yield return null;
            }

            frontObj.SetActive(!toBack);
            backObj.SetActive(toBack);

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                cardRect.localScale = new Vector3(Mathf.Lerp(0f, 1f, t), 1f, 1f);
                yield return null;
            }

            cardRect.localScale = Vector3.one;
            onComplete?.Invoke();
        }

        private void AddPlayerCardRow(PlayerData player, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            string rightButtonLabel, Color rightButtonColor, bool buttonInteractable,
            UnityEngine.Events.UnityAction onButtonClick, UnityEngine.Events.UnityAction onCardClick,
            bool isHire = false, string headerPrefix = null)
        {
            var rowPanel = AddPanel($"PlayerRow_{player.id}", parent, new Color32(28, 38, 46, 245),
                (anchorMin + anchorMax) * 0.5f, anchorMax - anchorMin);

            // Left: Player head preview with position badge and number
            var cardContainer = new GameObject("PlayerHeadContainer", typeof(RectTransform));
            cardContainer.transform.SetParent(rowPanel.transform, false);
            var cardRect = cardContainer.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.015f, 0.08f);
            cardRect.anchorMax = new Vector2(0.18f, 0.92f);
            cardRect.offsetMin = cardRect.offsetMax = Vector2.zero;

            var headBg = AddPanel("HeadBg", cardContainer.transform, GetPositionColor(player.position),
                new Vector2(0.5f, 0.5f), Vector2.one);
            headBg.color = new Color(headBg.color.r, headBg.color.g, headBg.color.b, 0.35f);

            var headPath = ResolvePlayerHeadPath(player.id);
            var headImg = AddResourceImage("Head", headPath, cardContainer.transform,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
            headImg.preserveAspect = true;
            headImg.raycastTarget = false;

            var posPath = $"Original/sprites/Card6/{player.position}Type";
            var posImg = AddResourceImage("PosBadge", posPath, cardContainer.transform,
                new Vector2(0.55f, 0.55f), new Vector2(1f, 1f));
            posImg.preserveAspect = true;
            posImg.raycastTarget = false;

            var numOutline = AddResourceImage("NumberOutline", "Original/sprites/Card3/PlayerNumberOutline", cardContainer.transform,
                new Vector2(0f, 0f), new Vector2(0.40f, 0.40f));
            numOutline.preserveAspect = true;
            numOutline.raycastTarget = false;

            var dPath = GetPlayerNumberSpritePath(player.id % 10);
            var numImg = AddResourceImage("Digit", dPath, numOutline.transform,
                new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f));
            numImg.preserveAspect = true;
            numImg.raycastTarget = false;

            var cardClickBtn = cardContainer.AddComponent<Button>();
            cardClickBtn.onClick.AddListener(() => onCardClick?.Invoke());

            // Center details
            var centerObj = new GameObject("Details", typeof(RectTransform));
            centerObj.transform.SetParent(rowPanel.transform, false);
            var centerRect = centerObj.GetComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.20f, 0.06f);
            centerRect.anchorMax = new Vector2(0.74f, 0.94f);
            centerRect.offsetMin = centerRect.offsetMax = Vector2.zero;

            var titleStr = string.IsNullOrEmpty(headerPrefix)
                ? $"{player.name}"
                : $"{headerPrefix}: {player.name}";
            var title = AddText(titleStr, centerObj.transform, 14, Paper,
                new Vector2(0f, 0.68f), new Vector2(1f, 1f));
            title.alignment = TextAnchor.MiddleLeft;
            title.fontStyle = FontStyle.Bold;

            // 6 Dice icons row in center
            var diceBarObj = new GameObject("MiniDiceRow", typeof(RectTransform));
            diceBarObj.transform.SetParent(centerObj.transform, false);
            var diceBarRect = diceBarObj.GetComponent<RectTransform>();
            diceBarRect.anchorMin = new Vector2(0f, 0.32f);
            diceBarRect.anchorMax = new Vector2(0.95f, 0.68f);
            diceBarRect.offsetMin = diceBarRect.offsetMax = Vector2.zero;

            if (player.diceSides != null)
            {
                var slotW = 1f / 6f;
                for (var i = 0; i < 6 && i < player.diceSides.Length; i++)
                {
                    var side = player.diceSides[i];
                    var sidePath = GetDiceSideSpritePath(side);
                    var tileGo = new GameObject($"SlotTile{i}", typeof(RectTransform), typeof(Image));
                    tileGo.transform.SetParent(diceBarObj.transform, false);
                    var tileRect = tileGo.GetComponent<RectTransform>();
                    tileRect.anchorMin = new Vector2(i * slotW + 0.02f, 0.05f);
                    tileRect.anchorMax = new Vector2((i + 1) * slotW - 0.02f, 0.95f);
                    tileRect.offsetMin = tileRect.offsetMax = Vector2.zero;
                    var tileImg = tileGo.GetComponent<Image>();
                    tileImg.color = Color.white;
                    tileImg.raycastTarget = false;

                    var diceImg = AddResourceImage($"Dice{i}", sidePath, tileGo.transform,
                        new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));
                    diceImg.preserveAspect = true;
                    diceImg.raycastTarget = false;
                }
            }

            // Superpower label
            var abilityName = player.superpower > 0 ? _catalog.GetAbility(player.superpower).name : "No ability";
            var abText = AddText(abilityName, centerObj.transform, 11, Gold,
                new Vector2(0f, 0f), new Vector2(1f, 0.32f));
            abText.alignment = TextAnchor.MiddleLeft;

            // Right button
            var button = AddButton(rightButtonLabel, rowPanel.transform, rightButtonColor,
                new Vector2(0.865f, 0.5f), new Vector2(0.23f, 0.72f), onButtonClick);
            button.interactable = buttonInteractable;
        }

        private void ShowSubstitutionSlots()
        {
            if (_match == null || !_match.CanBeginEncounter || _remainingSubs <= 0)
                return;

            ClearCanvas();
            AddMenuBackdrop();
            AddMenuHeader($"SUBSTITUTE PLAYER ({_remainingSubs} LEFT)",
                () => ShowMatchScreen(_catalog.ResolveLineup(_lineupIds)));

            var topInfo = new GameObject("TopInfo", typeof(RectTransform)).GetComponent<RectTransform>();
            topInfo.SetParent(_safeArea, false);
            topInfo.anchorMin = new Vector2(0f, 1f);
            topInfo.anchorMax = new Vector2(1f, 1f);
            topInfo.pivot = new Vector2(0.5f, 1f);
            topInfo.anchoredPosition = new Vector2(0f, -82f);
            topInfo.sizeDelta = new Vector2(0f, 52f);
            AddText("Choose the position to change.",
                topInfo, 15, Ink, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));

            var content = CreateScrollList("Substitution positions", _safeArea,
                new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), 7 * 96f,
                new Vector2(0f, 102f), new Vector2(0f, -142f));
            var lineup = _catalog.ResolveLineup(_lineupIds);
            for (var slot = 0; slot < lineup.Length; slot++)
            {
                var capturedSlot = slot;
                var player = lineup[slot];
                var yMin = 1f - (slot + 1) / 7f;
                var yMax = 1f - slot / 7f;
                AddPlayerCardRow(player, content,
                    new Vector2(0.02f, yMin + 0.005f), new Vector2(0.98f, yMax - 0.005f),
                    "CHANGE", new Color32(68, 79, 84, 255), true,
                    () => ShowSubstitutionBench(capturedSlot),
                    () => ShowCardModal(player),
                    false, SlotName(slot));
            }

            var bottomBar = CreateBottomBar("SubSlotsBottomBar", 96f);
            AddButton("CANCEL", bottomBar, new Color32(68, 79, 84, 255),
                new Vector2(0.5f, 0.5f), new Vector2(0.65f, 0.60f),
                () => ShowMatchScreen(_catalog.ResolveLineup(_lineupIds)));
        }

        private void ShowSubstitutionBench(int slot)
        {
            ClearCanvas();
            AddMenuBackdrop();
            AddMenuHeader($"BENCH — {SlotName(slot)}", ShowSubstitutionSlots);

            var topInfo = new GameObject("TopInfo", typeof(RectTransform)).GetComponent<RectTransform>();
            topInfo.SetParent(_safeArea, false);
            topInfo.anchorMin = new Vector2(0f, 1f);
            topInfo.anchorMax = new Vector2(1f, 1f);
            topInfo.pivot = new Vector2(0.5f, 1f);
            topInfo.anchoredPosition = new Vector2(0f, -82f);
            topInfo.sizeDelta = new Vector2(0f, 60f);
            AddText("Select a role-eligible player to replace the current player.",
                topInfo, 14, Ink, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));

            var bench = new List<PlayerData>();
            foreach (var playerId in _ownedPlayerIds)
            {
                if (Array.IndexOf(_lineupIds, playerId) < 0)
                {
                    var player = _catalog.GetPlayer(playerId);
                    if (IsRoleAllowed(player, slot))
                        bench.Add(player);
                }
            }

            if (bench.Count == 0)
            {
                var emptyFrame = CreateCenteredReferenceFrame("EmptyBenchFrame");
                AddText("No eligible player is available on the bench.",
                    emptyFrame, 16, Paper, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.58f));
            }
            else
            {
                var content = CreateScrollList("Bench players", _safeArea,
                    new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), bench.Count * 96f,
                    new Vector2(0f, 102f), new Vector2(0f, -148f));
                for (var index = 0; index < bench.Count; index++)
                {
                    var player = bench[index];
                    var capturedPlayerId = player.id;
                    var yMin = 1f - (index + 1) / (float)bench.Count;
                    var yMax = 1f - index / (float)bench.Count;
                    AddPlayerCardRow(player, content,
                        new Vector2(0.02f, yMin + 0.005f), new Vector2(0.98f, yMax - 0.005f),
                        "SUB", Grass, true,
                        () => CompleteSubstitution(slot, capturedPlayerId),
                        () => ShowCardModal(player, () => CompleteSubstitution(slot, capturedPlayerId),
                            "SUBSTITUTE INTO MATCH"));
                }
            }

            var bottomBar = CreateBottomBar("SubBenchBottomBar", 96f);
            AddButton("CANCEL", bottomBar, new Color32(68, 79, 84, 255),
                new Vector2(0.5f, 0.5f), new Vector2(0.65f, 0.60f),
                ShowSubstitutionSlots);
        }

        private void CompleteSubstitution(int slot, int playerId)
        {
            var replacement = _catalog.GetPlayer(playerId);
            _match.SubstituteHomePlayer(slot, replacement);
            _lineupIds[slot] = playerId;
            _remainingSubs = Mathf.Max(0, _remainingSubs - 1);
            _match.SetHomeBenchCount(CountUnusedBenchPlayers());
            PlayerPrefs.SetString(LineupPreferenceKey, string.Join(",", _lineupIds));
            PlayerPrefs.Save();
            _lastSubstitutedSlot = slot;
            AudioManager.Instance.PlayTransform();
            ShowMatchScreen(_catalog.ResolveLineup(_lineupIds));
        }

        private void BuildScoreboardEntourageSlots(Transform scoreboardTransform, int[] entourageIds, bool isHome)
        {
            var slots = isHome ? _homeEntourageIcons : _awayEntourageIcons;
            Array.Clear(slots, 0, slots.Length);
            if (entourageIds == null || entourageIds.Length == 0) return;

            for (int i = 0; i < Mathf.Min(2, entourageIds.Length); i++)
            {
                int eid = entourageIds[i];
                if (eid <= 0) continue;

                float x = isHome ? (-265f + i * 62f) : (265f - i * 62f);
                var ringGo = new GameObject($"{(isHome ? "Home" : "Away")}Entourage{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
                ringGo.transform.SetParent(scoreboardTransform, false);
                var rRect = ringGo.GetComponent<RectTransform>();
                rRect.anchorMin = rRect.anchorMax = new Vector2(0.5f, 1f);
                rRect.pivot = new Vector2(0.5f, 0.5f);
                rRect.anchoredPosition = new Vector2(x, -178f);
                rRect.sizeDelta = new Vector2(56f, 56f);

                var bgRing = ringGo.GetComponent<Image>();
                bgRing.sprite = GetUiSprite("Original/sprites/PlayerBody3/EntourageActiveState1");
                bgRing.color = new Color(1f, 1f, 1f, 0.92f);
                bgRing.preserveAspect = true;
                bgRing.raycastTarget = true;

                var iconImg = AddResourceImage("Icon", $"Original/images/EntourageAbility{eid:00}", ringGo.transform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                iconImg.rectTransform.sizeDelta = new Vector2(46f, 46f);
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                slots[i] = iconImg;

                int capturedId = eid;
                bool capturedHome = isHome;
                float capturedX = x;
                var btn = ringGo.GetComponent<Button>();
                btn.targetGraphic = bgRing;
                btn.onClick.AddListener(() =>
                {
                    AudioManager.Instance.PlayTap();
                    ShowEntourageInfoBar(capturedId, capturedHome, capturedX);
                });
            }
        }

        private void ShowEntourageInfoBar(int entourageId, bool isHome, float originX)
        {
            if (_overlayContainer == null) return;
            var existing = _overlayContainer.Find("EntourageInfoDismiss");
            if (existing != null) Destroy(existing.gameObject);

            var ent = _catalog?.GetEntourage(entourageId);
            if (ent == null) return;

            var dismissGo = AddStretchPanel("EntourageInfoDismiss", _overlayContainer, new Color(0f, 0f, 0f, 0.35f));
            dismissGo.raycastTarget = true;
            var dismissBtn = dismissGo.gameObject.AddComponent<Button>();
            dismissBtn.transition = Selectable.Transition.None;
            dismissBtn.onClick.AddListener(() => Destroy(dismissGo.gameObject));

            var bar = AddResourceImage("EntourageInfoBar", "Original/sprites/GoalAnimationSS4/EntourageInfoBar",
                dismissGo.transform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f));
            bar.rectTransform.sizeDelta = new Vector2(470f, 200f);
            bar.preserveAspect = true;
            bar.raycastTarget = false;

            var icon = AddResourceImage("EntIcon", $"Original/images/EntourageAbility{entourageId:00}", bar.transform,
                new Vector2(0.16f, 0.50f), new Vector2(0.16f, 0.50f));
            icon.rectTransform.sizeDelta = new Vector2(78f, 78f);
            icon.preserveAspect = true;

            var title = AddText($"{ent.name.ToUpperInvariant()} ({(isHome ? "YOURS" : "RIVAL")})", bar.transform, 16, Gold,
                new Vector2(0.28f, 0.62f), new Vector2(0.95f, 0.88f));
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleLeft;

            string desc = !string.IsNullOrEmpty(ent.inGameDescription) ? ent.inGameDescription : ent.abilityDescription;
            var descTxt = AddText(desc ?? "", bar.transform, 13, Paper,
                new Vector2(0.28f, 0.14f), new Vector2(0.94f, 0.62f));
            descTxt.alignment = TextAnchor.UpperLeft;
        }

        private IEnumerator PulseEntourageIcon(bool isHome, int entourageId)
        {
            if (_match == null) yield break;
            var arr = isHome ? _match.HomeEntourages : _match.AwayEntourages;
            var icons = isHome ? _homeEntourageIcons : _awayEntourageIcons;
            if (arr == null) yield break;

            Image targetIcon = null;
            for (int i = 0; i < Mathf.Min(2, arr.Length); i++)
            {
                if (arr[i] == entourageId) { targetIcon = icons[i]; break; }
            }
            if (targetIcon == null || targetIcon.transform.parent == null) yield break;

            var ringImg = targetIcon.transform.parent.GetComponent<Image>();
            var t = targetIcon.transform.parent;
            for (int f = 1; f <= 7; f++)
            {
                if (t == null) yield break;
                float s = 1f + 0.22f * Mathf.Sin(f / 7f * Mathf.PI);
                t.localScale = new Vector3(s, s, 1f);
                if (ringImg != null)
                    ringImg.sprite = GetUiSprite($"Original/sprites/PlayerBody3/EntourageTriggerState{f}");
                yield return new WaitForSeconds(0.055f);
            }
            for (int f = 1; f <= 4; f++)
            {
                if (ringImg != null)
                    ringImg.sprite = GetUiSprite($"Original/sprites/PlayerBody3/EntourageTriggerShine{f}");
                yield return new WaitForSeconds(0.045f);
            }
            if (t != null) t.localScale = Vector3.one;
            if (ringImg != null) ringImg.sprite = GetUiSprite("Original/sprites/PlayerBody3/EntourageActiveState1");
        }

        private void BuildInMatchSubstitutionTrigger()
        {
            bool hasBench = CountUnusedBenchPlayers() > 0;
            bool canSub = !_isLocalMultiplayer && _remainingSubs > 0 && hasBench;

            var subGo = new GameObject("SubTriggerBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            subGo.transform.SetParent(_safeArea, false);
            var sRect = subGo.GetComponent<RectTransform>();
            sRect.anchorMin = sRect.anchorMax = new Vector2(0f, 0f);
            sRect.pivot = new Vector2(0.5f, 0.5f);
            sRect.anchoredPosition = new Vector2(54f, 52f);
            sRect.sizeDelta = new Vector2(68f, 68f);

            var bgImg = subGo.GetComponent<Image>();
            bgImg.sprite = GetUiSprite("Original/images/SubstitutionCountBG");
            bgImg.color = canSub ? Color.white : new Color(0.75f, 0.75f, 0.75f, 0.55f);
            bgImg.preserveAspect = true;
            bgImg.raycastTarget = true;

            _subRemainingText = AddText(_remainingSubs.ToString(), subGo.transform, 24, Ink,
                new Vector2(0.15f, 0.30f), new Vector2(0.85f, 0.90f));
            _subRemainingText.fontStyle = FontStyle.Bold;
            _subRemainingText.alignment = TextAnchor.MiddleCenter;

            var subLabel = AddText("SUB", subGo.transform, 11, Ink,
                new Vector2(0.10f, 0.08f), new Vector2(0.90f, 0.36f));
            subLabel.fontStyle = FontStyle.Bold;
            subLabel.alignment = TextAnchor.MiddleCenter;

            var btn = subGo.GetComponent<Button>();
            btn.targetGraphic = bgImg;
            btn.interactable = canSub;
            btn.onClick.AddListener(() =>
            {
                if (_match == null || !_match.CanBeginEncounter || _remainingSubs <= 0 || _inEncounterAnimation) return;
                AudioManager.Instance.PlayTap();
                ShowInMatchSubstitutionBar();
            });
            _substitutionButton = btn;
        }

        private void ShowInMatchSubstitutionBar()
        {
            if (_overlayContainer == null || _match == null || _remainingSubs <= 0) return;
            if (_subDrawerGo != null) Destroy(_subDrawerGo.gameObject);

            var scrim = AddStretchPanel("SubDrawerScrim", _overlayContainer, new Color(0f, 0f, 0f, 0.62f));
            scrim.raycastTarget = true;
            _subDrawerGo = scrim.rectTransform;

            var bar = AddResourceImage("SubstitutionBar", "Original/sprites/GoalAnimationSS4/SubstitutionBar",
                scrim.transform, new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.50f));
            bar.rectTransform.sizeDelta = new Vector2(760f, 640f);
            bar.preserveAspect = false;
            bar.raycastTarget = true;

            // NOTE: SubstitutionBar.png opaque chalkboard panel spans normalized X=[0.26..0.75] inside texture
            var titleTxt = AddText($"SUBSTITUTIONS ({_remainingSubs} LEFT)", bar.transform, 14, Paper,
                new Vector2(0.28f, 0.87f), new Vector2(0.72f, 0.94f));
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;

            var lineup = _catalog.ResolveLineup(_lineupIds);
            int selectedSlot = -1;
            int selectedBenchId = -1;

            var benchPanel = new GameObject("SubBenchList", typeof(RectTransform)).GetComponent<RectTransform>();
            benchPanel.SetParent(bar.transform, false);
            benchPanel.anchorMin = new Vector2(0.28f, 0.19f);
            benchPanel.anchorMax = new Vector2(0.72f, 0.49f);
            benchPanel.offsetMin = benchPanel.offsetMax = Vector2.zero;

            var slotBoxes = new Image[7];
            Button tickBtn = null;

            void RebuildBenchForSlot(int targetSlot)
            {
                for (int c = benchPanel.childCount - 1; c >= 0; c--)
                    Destroy(benchPanel.GetChild(c).gameObject);

                var eligible = new List<PlayerData>();
                foreach (var pid in _ownedPlayerIds)
                {
                    if (Array.IndexOf(_lineupIds, pid) < 0)
                    {
                        var pd = _catalog.GetPlayer(pid);
                        if (pd != null && IsRoleAllowed(pd, targetSlot))
                            eligible.Add(pd);
                    }
                }

                if (eligible.Count == 0)
                {
                    var noTxt = AddText($"NO ELIGIBLE {SlotName(targetSlot)} ON BENCH", benchPanel, 13, Paper, Vector2.zero, Vector2.one);
                    noTxt.alignment = TextAnchor.MiddleCenter;
                    return;
                }

                for (int bi = 0; bi < Mathf.Min(6, eligible.Count); bi++)
                {
                    var bp = eligible[bi];
                    int row = bi / 3;
                    int col = bi % 3;
                    float cx = 0.18f + col * 0.32f;
                    float cy = row == 0 ? 0.72f : 0.28f;
                    var box = AddResourceImage($"BenchBox{bi}", "Original/sprites/GoalAnimationSS4/SubstitutionBarBox",
                        benchPanel, new Vector2(cx, cy), new Vector2(cx, cy));
                    box.rectTransform.sizeDelta = new Vector2(76f, 68f);
                    box.preserveAspect = true;
                    box.raycastTarget = true;

                    var bHead = AddResourceImage("Head", PlayerBodySprites.ResolveHeadPath(bp, bi + 1), box.transform,
                        new Vector2(0.5f, 0.60f), new Vector2(0.5f, 0.60f));
                    bHead.rectTransform.sizeDelta = new Vector2(44f, 38f);
                    bHead.preserveAspect = true;
                    bHead.raycastTarget = false;

                    var bName = AddText(bp.name.Split(' ')[0].ToUpperInvariant(), box.transform, 10, Ink,
                        new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.28f));
                    bName.fontStyle = FontStyle.Bold;
                    bName.alignment = TextAnchor.MiddleCenter;

                    int capturedPid = bp.id;
                    var bBtn = box.gameObject.AddComponent<Button>();
                    bBtn.targetGraphic = box;
                    bBtn.onClick.AddListener(() =>
                    {
                        AudioManager.Instance.PlayTap();
                        selectedBenchId = capturedPid;
                        for (int k = 0; k < benchPanel.childCount; k++)
                        {
                            var img = benchPanel.GetChild(k).GetComponent<Image>();
                            if (img != null) img.color = (img == box) ? new Color32(255, 225, 90, 255) : Color.white;
                        }
                        if (tickBtn != null) tickBtn.interactable = (selectedSlot >= 0 && selectedBenchId > 0);
                    });
                }
            }

            string[] shortRoles = { "GK", "LB", "CB", "RB", "LW", "CF", "RW" };
            // 7 Current Lineup Slots cleanly inside opaque drawer width [0.28..0.72]
            for (int slot = 0; slot < 7; slot++)
            {
                int row = slot / 4;
                int col = slot % 4;
                float cx = row == 0 ? (0.345f + col * 0.103f) : (0.395f + col * 0.105f);
                float cy = row == 0 ? 0.77f : 0.63f;
                var sBox = AddResourceImage($"SlotBox{slot}", "Original/sprites/GoalAnimationSS4/SubstitutionBarBox",
                    bar.transform, new Vector2(cx, cy), new Vector2(cx, cy));
                sBox.rectTransform.sizeDelta = new Vector2(66f, 60f);
                sBox.preserveAspect = true;
                sBox.raycastTarget = true;
                slotBoxes[slot] = sBox;

                var p = lineup[slot];
                var sHead = AddResourceImage("Head", PlayerBodySprites.ResolveHeadPath(p, slot + 1), sBox.transform,
                    new Vector2(0.5f, 0.60f), new Vector2(0.5f, 0.60f));
                sHead.rectTransform.sizeDelta = new Vector2(38f, 34f);
                sHead.preserveAspect = true;
                sHead.raycastTarget = false;

                var sRole = AddText(shortRoles[slot], sBox.transform, 11, Ink,
                    new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.28f));
                sRole.fontStyle = FontStyle.Bold;
                sRole.alignment = TextAnchor.MiddleCenter;

                int capSlot = slot;
                var sBtn = sBox.gameObject.AddComponent<Button>();
                sBtn.targetGraphic = sBox;
                sBtn.onClick.AddListener(() =>
                {
                    AudioManager.Instance.PlayTap();
                    selectedSlot = capSlot;
                    selectedBenchId = -1;
                    for (int k = 0; k < 7; k++)
                        if (slotBoxes[k] != null) slotBoxes[k].color = (k == capSlot) ? new Color32(255, 225, 90, 255) : Color.white;
                    if (tickBtn != null) tickBtn.interactable = false;
                    RebuildBenchForSlot(capSlot);
                });
            }

            // Pre-select slot 1 (LB) so bench options are immediately browsable if eligible
            selectedSlot = 1;
            slotBoxes[1].color = new Color32(255, 225, 90, 255);
            RebuildBenchForSlot(1);

            var instTxt = AddText("TAP SLOT TO REPLACE:", bar.transform, 11, Paper,
                new Vector2(0.28f, 0.50f), new Vector2(0.72f, 0.55f));
            instTxt.fontStyle = FontStyle.Bold;
            instTxt.alignment = TextAnchor.MiddleCenter;

            // Confirm Tick & Cancel Cross at bottom inside drawer
            tickBtn = AddSpriteButton("ConfirmSub", "Original/sprites/GoalAnimationSS4/SubstitutionBarTick",
                "Original/sprites/GoalAnimationSS4/SubstitutionBarTickPressed", bar.transform,
                new Vector2(0.41f, 0.10f), new Vector2(0.14f, 0.11f), () =>
                {
                    if (selectedSlot >= 0 && selectedBenchId > 0)
                    {
                        Destroy(scrim.gameObject);
                        _subDrawerGo = null;
                        CompleteSubstitution(selectedSlot, selectedBenchId);
                    }
                });
            tickBtn.interactable = false;

            AddSpriteButton("CancelSub", "Original/sprites/GoalAnimationSS4/SubstitutionBarCross",
                "Original/sprites/GoalAnimationSS4/SubstitutionBarCrossPressed", bar.transform,
                new Vector2(0.59f, 0.10f), new Vector2(0.14f, 0.11f), () =>
                {
                    Destroy(scrim.gameObject);
                    _subDrawerGo = null;
                });
        }

        private void ShowManage()
        {
            _catalog ??= GameCatalog.Load();
            EnsureLineupLoaded();
            ClearCanvas();

            // Vertical Soccer Pitch Backdrop (preserves natural pitch circle & grass stripe proportions via EnvelopeParent)
            AddFullscreenPitchBackdrop();

            _safeArea.SetAsLastSibling();
            var header = AddMenuHeader(null, ShowHome);

            // Top-left UpButton next to Back button inside header bar
            bool entourageUnlocked = _seasonIndex > 0 || _maxUnlockedSeason > 0 || _unlockedEntourageIds.Count > 0 || PlayerPrefs.GetInt("diceSoccer.allUnlocked", 0) == 1;
            AddSpriteButton("HeaderUpBtn",
                entourageUnlocked ? "Original/sprites/EnemyLineupSS1/UpButton" : "Original/sprites/EnemyLineupSS1/UpButtonDisabled",
                entourageUnlocked ? "Original/sprites/EnemyLineupSS2/UpButtonPressed" : "Original/sprites/EnemyLineupSS1/UpButtonDisabled",
                header, new Vector2(0.20f, 0.50f), new Vector2(0.11f, 0.93f), ShowEntourage);

            var lineup = _catalog.ResolveLineup(_lineupIds);

            // Formation Container: fixed 640x960 reference box centered on grass pitch.
            // Preserves authentic 2:3 trading card proportions in all display modes (including Fullscreen)
            var formRect = CreateCenteredReferenceFrame("FormationRoot");

            // 1-3-3 Formation Positions:
            // Top row: 3 Forwards (Left #8 Jerry, Center #6 David inside StrikerStar, Right #7 Justin)
            // Middle row: 3 Defenders (Left #4 Sherman, Center #3 Jo, Right #2 Ernest)
            // Bottom row: 1 Goalkeeper (Center #1 Willy)
            var slotPositions = new Vector2[]
            {
                new Vector2(0.500f, 0.235f), // Slot 0: GK (#1 Willy The Kid)
                new Vector2(0.790f, 0.490f), // Slot 1: Right DEF (#2 Ernest Mulligan)
                new Vector2(0.500f, 0.490f), // Slot 2: Center DEF (#3 Jo Lee-Roger)
                new Vector2(0.210f, 0.490f), // Slot 3: Left DEF (#4 Sherman Tank)
                new Vector2(0.790f, 0.745f), // Slot 4: Right FWD (#7 Justin Huff)
                new Vector2(0.500f, 0.745f), // Slot 5: Center Striker (#6 David Little, Striker Star)
                new Vector2(0.210f, 0.745f), // Slot 6: Left FWD (#8 Jerry Hat-trick)
            };

            const float cardW = 156f;
            const float cardH = 234f; // Exact 2:3 trading card ratio (156 / 234 = 2 / 3)

            for (var slot = 0; slot < lineup.Length; slot++)
            {
                var capturedSlot = slot;
                var player = lineup[slot];
                var center = slotPositions[slot];

                // If Center Striker (slot 5), add StrikerStar frame behind it; otherwise subtle white card slot border
                if (slot == 5)
                {
                    var strikerStar = AddResourceImage("StrikerStar",
                        "Original/sprites/EnemyLineupSS1/StrikerStar", formRect,
                        center, center);
                    strikerStar.rectTransform.sizeDelta = new Vector2(178f, 258f);
                    strikerStar.preserveAspect = false;
                    strikerStar.raycastTarget = false;
                }
                else
                {
                    var slotBorder = AddResourceImage($"SlotBorder_{slot}",
                        "Original/sprites/PlayerCardBG/CardBorder", formRect,
                        center, center);
                    slotBorder.rectTransform.sizeDelta = new Vector2(162f, 240f);
                    slotBorder.color = new Color(1f, 1f, 1f, 0.45f);
                    slotBorder.preserveAspect = false;
                    slotBorder.raycastTarget = false;
                }

                CreatePlayerCardFront(player, formRect,
                    center, center,
                    false, true, () => ShowPlayerPicker(capturedSlot),
                    false, new Vector2(cardW, cardH));
            }

            // Authentic Bottom Bar on Grass (fixed 108px height docked to bottom)
            var bottomBar = CreateBottomBar("ManageBottomBar", 108f);

            AddSpriteButton("BenchBtn", "Original/sprites/EnemyLineupSS1/BenchButton",
                "Original/sprites/EnemyLineupSS1/BenchButtonPressed", bottomBar,
                new Vector2(0.173f, 0.50f), new Vector2(0.297f, 0.86f), () => ShowPlayerPicker(-1));

            AddSpriteButton("ShopBtn", "Original/sprites/EnemyLineupSS1/FormationShopButton",
                "Original/sprites/EnemyLineupSS1/FormationShopButtonPressed", bottomBar,
                new Vector2(0.419f, 0.50f), new Vector2(0.131f, 0.74f), ShowShop);

            AddSpriteButton("KitBtn", "Original/sprites/EnemyLineupSS1/KitButton",
                "Original/sprites/EnemyLineupSS1/KitButtonPressed", bottomBar,
                new Vector2(0.581f, 0.50f), new Vector2(0.131f, 0.74f), ShowKitSelection);

            AddSpriteButton("PlayBtn", "Original/sprites/EnemyLineupSS1/FormationPlayButton",
                "Original/sprites/EnemyLineupSS1/FormationPlayButtonPressed", bottomBar,
                new Vector2(0.827f, 0.50f), new Vector2(0.297f, 0.86f), StartCareerMatch);

            if (PlayerPrefs.GetInt("diceSoccer.tutorial.6", 0) == 0)
            {
                ShowTutorialPopup(6);
            }
        }

        private void ShowOpponentPreview(FixtureData fixture)
        {
            if (fixture == null || fixture.lineup == null)
            {
                ShowLeagueFixtures();
                return;
            }

            _catalog ??= GameCatalog.Load();
            ClearCanvas();

            AddFullscreenPitchBackdrop();

            _safeArea.SetAsLastSibling();
            AddMenuHeader(fixture.teamName, ShowLeagueFixtures);

            var opponentLineup = _catalog.ResolveLineup(fixture.lineup);

            // Formation Container: fixed 640x960 reference box centered on grass pitch
            var formRect = CreateCenteredReferenceFrame("FormationRoot");

            var slotPositions = new Vector2[]
            {
                new Vector2(0.500f, 0.235f),
                new Vector2(0.790f, 0.490f),
                new Vector2(0.500f, 0.490f),
                new Vector2(0.210f, 0.490f),
                new Vector2(0.790f, 0.745f),
                new Vector2(0.500f, 0.745f),
                new Vector2(0.210f, 0.745f),
            };

            const float cardW = 156f;
            const float cardH = 234f;

            for (var slot = 0; slot < opponentLineup.Length; slot++)
            {
                var player = opponentLineup[slot];
                var center = slotPositions[slot];
                if (slot == 5)
                {
                    var strikerStar = AddResourceImage("StrikerStar",
                        "Original/sprites/EnemyLineupSS1/StrikerStar", formRect,
                        center, center);
                    strikerStar.rectTransform.sizeDelta = new Vector2(178f, 258f);
                    strikerStar.preserveAspect = false;
                    strikerStar.raycastTarget = false;
                }
                else
                {
                    var slotBorder = AddResourceImage($"SlotBorder_{slot}",
                        "Original/sprites/PlayerCardBG/CardBorder", formRect,
                        center, center);
                    slotBorder.rectTransform.sizeDelta = new Vector2(162f, 240f);
                    slotBorder.color = new Color(1f, 1f, 1f, 0.45f);
                    slotBorder.preserveAspect = false;
                    slotBorder.raycastTarget = false;
                }

                CreatePlayerCardFront(player, formRect,
                    center, center,
                    false, true, () => ShowCardModal(player),
                    false, new Vector2(cardW, cardH));
            }

            var bottomBar = CreateBottomBar("OpponentBottomBar", 108f);

            AddSpriteButton("ManageBtn", "Original/sprites/EnemyLineupSS1/ManageButton",
                "Original/sprites/EnemyLineupSS1/ManageButtonPressed", bottomBar,
                new Vector2(0.32f, 0.50f), new Vector2(0.297f, 0.86f), ShowManage);

            AddSpriteButton("PlayBtn", "Original/sprites/EnemyLineupSS1/FormationPlayButton",
                "Original/sprites/EnemyLineupSS1/FormationPlayButtonPressed", bottomBar,
                new Vector2(0.68f, 0.50f), new Vector2(0.297f, 0.86f), () => StartFixture(_selectedFixtureIndex));

            if (PlayerPrefs.GetInt("diceSoccer.tutorial.45", 0) == 0)
            {
                ShowTutorialPopup(45);
            }
        }

        private bool IsPlayerUnlocked(PlayerData player)
        {
            if (player == null || player.id <= 0 || player.id >= 1000) return false;
            if (_ownedPlayerIds != null && _ownedPlayerIds.Contains(player.id)) return true;
            if (PlayerPrefs.GetInt("diceSoccer.allUnlocked", 0) == 1) return true;
            if (PlayerPrefs.GetInt($"diceSoccer.unlockedPlayer.{player.id}", 0) == 1) return true;
            return string.IsNullOrWhiteSpace(player.unlockCriteria);
        }

        private void SpendCash(int amount)
        {
            if (amount <= 0) return;
            _careerCash = Mathf.Max(0, _careerCash - amount);
            int spent = PlayerPrefs.GetInt("diceSoccer.stats.cashSpent", 0) + amount;
            PlayerPrefs.SetInt("diceSoccer.stats.cashSpent", spent);
        }

        private void ShowPlayerPicker(int slot)
        {
            _catalog ??= GameCatalog.Load();
            EnsureLineupLoaded();
            _selectedLineupSlot = slot;

            ClearCanvas();

            // Background: Sunny stadium with clouds (matching media_1792421110155_320ad894.png)
            AddMenuBackdrop(true);

            // Header: Back arrow (returns to ShowManage), cash, crest (matching media_1792421110155_320ad894.png)
            AddMenuHeader(null, ShowManage);

            // Determine active position tab (0 = FWD, 1 = MID, 2 = DEF, 3 = GK)
            int defaultTab = slot switch
            {
                0 => 3,       // GK
                1 or 2 or 3 => 2, // DEF
                4 or 5 or 6 => 0, // FWD
                _ => 0
            };
            int activeTab = _deckCategoryFilter >= 0 ? _deckCategoryFilter : defaultTab;
            if (activeTab < 0 || activeTab > 3) activeTab = 0;

            string positionKey = activeTab switch
            {
                0 => "FWD",
                1 => "MID",
                2 => "DEF",
                3 => "GK",
                _ => "FWD"
            };

            // 1. Collect candidates for the active position:
            var candidates = new List<PlayerData>();

            // (a) Owned players of this position
            foreach (var pId in _ownedPlayerIds)
            {
                var p = _catalog.GetPlayer(pId);
                if (p != null && p.position == positionKey)
                    candidates.Add(p);
            }

            // (b) Unowned hireable players of this position
            foreach (var p in _catalog.Data.players)
            {
                if (p.id > 0 && p.id < 1000 && p.cost > 0 &&
                    IsPlayerUnlocked(p) &&
                    !_ownedPlayerIds.Contains(p.id) &&
                    p.position == positionKey)
                {
                    candidates.Add(p);
                }
            }

            // (c) Locked players of this position
            foreach (var p in _catalog.Data.players)
            {
                if (p.id > 0 && p.id < 1000 &&
                    (!IsPlayerUnlocked(p) || p.cost <= 0) &&
                    !_ownedPlayerIds.Contains(p.id) &&
                    p.position == positionKey)
                {
                    candidates.Add(p);
                }
            }

            // Sort candidates by Player ID (matching authentic Cocos2d catalog ordering: 5, 13, 21, 29, 37...)
            candidates.Sort((a, b) => a.id.CompareTo(b.id));

            // 2. Pagination: 4 cards per page (2 columns x 2 rows, filled column-by-column)
            const int cardsPerPage = 4;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(candidates.Count / (float)cardsPerPage));
            _deckPage = Mathf.Clamp(_deckPage, 0, totalPages - 1);

            var pickerFrame = CreateCenteredReferenceFrame("PickerFrame");

            Vector2[] cardCenters = {
                new Vector2(0.28f, 0.62f), // Column 1 Top
                new Vector2(0.28f, 0.28f), // Column 1 Bottom
                new Vector2(0.72f, 0.62f), // Column 2 Top
                new Vector2(0.72f, 0.28f)  // Column 2 Bottom
            };
            const float pickCardW = 184f;
            const float pickCardH = 276f; // Exact 2:3 ratio

            int startIdx = _deckPage * cardsPerPage;
            for (int i = 0; i < cardsPerPage && (startIdx + i) < candidates.Count; i++)
            {
                var player = candidates[startIdx + i];
                var capturedPlayer = player;
                var pId = player.id;
                bool isOwned = _ownedPlayerIds.Contains(pId);
                bool isLocked = !isOwned && (!IsPlayerUnlocked(player) || player.cost <= 0);
                bool isHire = !isOwned && !isLocked && player.cost > 0;

                var c = cardCenters[i];

                CreatePlayerCardFront(player, pickerFrame, c, c,
                    isHire: isHire, isInteractive: true,
                    onClick: () =>
                    {
                        if (isLocked)
                        {
                            AudioManager.Instance.PlayTap();
                            return;
                        }
                        if (isHire)
                        {
                            if (_careerCash >= capturedPlayer.cost)
                            {
                                SpendCash(capturedPlayer.cost);
                                _ownedPlayerIds.Add(pId);
                                if (slot >= 0 && IsRoleAllowed(capturedPlayer, slot))
                                {
                                    _lineupIds[slot] = pId;
                                    PlayerPrefs.SetString(LineupPreferenceKey, string.Join(",", _lineupIds));
                                }
                                SaveCareerState();
                                EvaluateCareerProgressAndAchievements(false, false, false, true);
                                AudioManager.Instance.PlayTransform();
                                if (slot >= 0 && IsRoleAllowed(capturedPlayer, slot))
                                    ShowManage();
                                else
                                    ShowPlayerPicker(slot);
                                ShowNextUnlockModalThen(null);
                            }
                            else
                            {
                                AudioManager.Instance.PlayTap();
                            }
                            return;
                        }
                        // Owned player
                        if (slot >= 0)
                        {
                            AssignPlayerToSlot(slot, pId);
                        }
                        else
                        {
                            ShowCardModal(capturedPlayer);
                        }
                    },
                    isLocked: isLocked, sizeDelta: new Vector2(pickCardW, pickCardH));
            }

            // 3. Navigation Chevrons on sides (matching media_1792421110155_320ad894.png)
            bool canPrev = _deckPage > 0;
            var leftArrow = AddSpriteButton("LeftArrow",
                canPrev ? "Original/images/LeftArrow" : "Original/images/LeftArrowDisabled",
                canPrev ? "Original/images/LeftArrowSelected" : "Original/images/LeftArrowDisabled",
                pickerFrame, new Vector2(0.06f, 0.48f), new Vector2(0.08f, 0.10f),
                canPrev ? () => { _deckPage--; AudioManager.Instance.PlayTap(); ShowPlayerPicker(slot); } : null);
            leftArrow.interactable = canPrev;

            bool canNext = _deckPage < totalPages - 1;
            var rightArrow = AddSpriteButton("RightArrow",
                canNext ? "Original/images/RightArrow" : "Original/images/RightArrowDisabled",
                canNext ? "Original/images/RightArrowSelected" : "Original/images/RightArrowDisabled",
                pickerFrame, new Vector2(0.94f, 0.48f), new Vector2(0.08f, 0.10f),
                canNext ? () => { _deckPage++; AudioManager.Instance.PlayTap(); ShowPlayerPicker(slot); } : null);
            rightArrow.interactable = canNext;

            // 4. Authentic Bottom Folder Tabs: FWD (Tab01), MID (Tab02), DEF (Tab03), GK (Tab04)
            var bottomBar = CreateBottomBar("PickerTabsBar", 108f);
            string[] tabSprites = { "Tab01", "Tab02", "Tab03", "Tab04" };
            float[] tabXs = { 0.14f, 0.38f, 0.62f, 0.86f };

            for (int t = 0; t < 4; t++)
            {
                int tabIdx = t;
                bool isSelected = (activeTab == tabIdx);
                float tabY = isSelected ? 0.49f : 0.18f; // Active tab is elevated inside 108px bottom bar
                string spritePath = $"Original/images/{tabSprites[t]}";
                string pressedPath = $"Original/images/{tabSprites[t]}Pressed";

                AddSpriteButton($"Tab_{t}", spritePath, pressedPath, bottomBar,
                    new Vector2(tabXs[t], tabY), new Vector2(0.22f, 0.88f), () =>
                    {
                        _deckCategoryFilter = tabIdx;
                        _deckPage = 0;
                        AudioManager.Instance.PlayTap();
                        ShowPlayerPicker(slot);
                    });
            }
        }

        private void ShowHiring()
        {
            _catalog ??= GameCatalog.Load();
            EnsureLineupLoaded();
            ClearCanvas();
            AddMenuBackdrop(false);
            AddMenuHeader(null, ShowManage);

            var candidates = new List<PlayerData>();
            foreach (var player in _catalog.Data.players)
            {
                if (player.id > 0 && player.id < 1000 && player.cost > 0 &&
                    IsPlayerUnlocked(player) &&
                    !_ownedPlayerIds.Contains(player.id))
                    candidates.Add(player);
            }
            if (candidates.Count == 0)
            {
                AddText("NO PLAYERS AVAILABLE TO HIRE",
                    _safeArea, 20, Ink, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.58f));
                return;
            }

            _hireCandidateIndex = Mathf.Clamp(_hireCandidateIndex, 0, candidates.Count - 1);
            var candidate = candidates[_hireCandidateIndex];
            var canAfford = _careerCash >= candidate.cost;
            var capturedPlayerId = candidate.id;

            var hiringFrame = CreateCenteredReferenceFrame("HiringFrame");

            // Centered player card matching authentic 2:3 ratio
            var cardCenter = new Vector2(0.5f, 0.56f);
            CreatePlayerCardFront(candidate, hiringFrame,
                cardCenter, cardCenter,
                true, true, () => ShowCardModal(candidate),
                false, new Vector2(300f, 450f));

            // Left & Right Navigation Chevrons
            var canPrev = _hireCandidateIndex > 0;
            var leftArrow = AddSpriteButton("LeftArrow",
                canPrev ? "Original/images/LeftArrow" : "Original/images/LeftArrowDisabled",
                canPrev ? "Original/images/LeftArrowSelected" : "Original/images/LeftArrowDisabled",
                hiringFrame, new Vector2(0.10f, 0.55f), new Vector2(0.10f, 0.12f),
                canPrev ? () => { _hireCandidateIndex--; ShowHiring(); } : null);
            leftArrow.interactable = canPrev;

            var canNext = _hireCandidateIndex < candidates.Count - 1;
            var rightArrow = AddSpriteButton("RightArrow",
                canNext ? "Original/images/RightArrow" : "Original/images/RightArrowDisabled",
                canNext ? "Original/images/RightArrowSelected" : "Original/images/RightArrowDisabled",
                hiringFrame, new Vector2(0.90f, 0.55f), new Vector2(0.10f, 0.12f),
                canNext ? () => { _hireCandidateIndex++; ShowHiring(); } : null);
            rightArrow.interactable = canNext;

            // Authentic HIRE Button (BuyButton.png)
            var hireBtn = AddSpriteButton("HireBtn", "Original/images/BuyButton",
                "Original/images/BuyButtonPressed", hiringFrame,
                new Vector2(0.50f, 0.21f), new Vector2(0.42f, 0.08f),
                canAfford ? () => HirePlayer(capturedPlayerId) : null);
            hireBtn.interactable = canAfford;
            if (!canAfford)
            {
                var hireImg = hireBtn.GetComponent<Image>();
                if (hireImg != null) hireImg.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            }

            // Bottom Footer & Back to Manage (fixed 77px bar anchored to bottom)
            var footerBar = AddResourceImage("HiringFooter", "Original/images/FooterBG", _safeArea,
                new Vector2(0f, 0f), new Vector2(1f, 0f));
            footerBar.rectTransform.pivot = new Vector2(0.5f, 0f);
            footerBar.rectTransform.anchoredPosition = Vector2.zero;
            footerBar.rectTransform.sizeDelta = new Vector2(0f, 77f);
            footerBar.preserveAspect = false;
            footerBar.raycastTarget = false;

            AddSpriteButton("BackToManage", "Original/sprites/EnemyLineupSS1/ManageButton",
                "Original/sprites/EnemyLineupSS1/ManageButtonPressed", footerBar.transform,
                new Vector2(0.5f, 0.50f), new Vector2(0.30f, 0.875f), ShowManage);
        }

        private void HirePlayer(int playerId)
        {
            var player = _catalog.GetPlayer(playerId);
            if (player.id <= 0 || player.id >= 1000 || player.cost <= 0 ||
                !IsPlayerUnlocked(player))
                throw new InvalidOperationException(
                    $"Player {player.name} is not eligible for hiring from the available roster.");
            if (_ownedPlayerIds.Contains(player.id))
                throw new InvalidOperationException($"Player {player.name} is already owned.");
            if (_careerCash < player.cost)
                return;

            SpendCash(player.cost);
            _ownedPlayerIds.Add(player.id);
            PlayerPrefs.SetString(RosterPreferenceKey, string.Join(",", _ownedPlayerIds));
            SaveCareerState();
            EvaluateCareerProgressAndAchievements(false, false, false, true);
            AudioManager.Instance.PlayBuy();
            ShowHiring();
            ShowNextUnlockModalThen(null);
        }

        private void AssignPlayerToSlot(int targetSlot, int playerId)
        {
            if (!CanAssignPlayer(playerId, targetSlot))
                return;

            var updatedLineup = (int[])_lineupIds.Clone();
            updatedLineup[targetSlot] = playerId;
            var resolved = _catalog.ResolveLineup(updatedLineup);
            ValidateLineupRoles(resolved);
            _lineupIds = updatedLineup;
            PlayerPrefs.SetString(LineupPreferenceKey, string.Join(",", _lineupIds));
            PlayerPrefs.Save();
            AudioManager.Instance.PlayTap();
            ShowManage();
        }

        private bool CanAssignPlayer(int playerId, int targetSlot)
        {
            if (_lineupIds == null || _ownedPlayerIds == null ||
                targetSlot < 0 || targetSlot >= _lineupIds.Length ||
                !_ownedPlayerIds.Contains(playerId))
                return false;

            var duplicateSlot = Array.IndexOf(_lineupIds, playerId);
            if (duplicateSlot >= 0 && duplicateSlot != targetSlot)
                return false;
            return IsRoleAllowed(_catalog.GetPlayer(playerId), targetSlot);
        }

        private static bool IsRoleAllowed(PlayerData player, int slot)
        {
            if (slot == 0)
                return player.position == "GK";
            if (player.position == "GK")
                return false;
            if (player.position == "MID")
                return true;
            return slot <= 3 ? player.position == "DEF" : player.position == "FWD";
        }

        private void ValidateLineupRoles(PlayerData[] lineup)
        {
            var playerIds = new HashSet<int>();
            for (var slot = 0; slot < lineup.Length; slot++)
            {
                if (!playerIds.Add(lineup[slot].id))
                    throw new InvalidOperationException(
                        $"Player {lineup[slot].name} cannot occupy more than one lineup slot.");
                if (!IsRoleAllowed(lineup[slot], slot))
                    throw new InvalidOperationException(
                        $"Player {lineup[slot].name} cannot fill {SlotName(slot)}.");
            }
        }

        private static string SlotName(int slot)
        {
            switch (slot)
            {
                case 0: return "Goalkeeper";
                case 1: return "Left Defender";
                case 2: return "Central Defender";
                case 3: return "Right Defender";
                case 4: return "Left Forward";
                case 5: return "Central Forward";
                case 6: return "Right Forward";
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        private void EnsureLineupLoaded()
        {
            if (_ownedPlayerIds == null)
            {
                var savedRoster = PlayerPrefs.GetString(RosterPreferenceKey, string.Empty);
                _ownedPlayerIds = string.IsNullOrEmpty(savedRoster)
                    ? new List<int>(_catalog.Data.initialLineup)
                    : ParsePlayerIds(savedRoster, "saved roster");
                if (PlayerPrefs.GetInt("diceSoccer.allUnlocked", 0) != 0)
                {
                    foreach (var p in _catalog.Data.players)
                    {
                        if (p.id > 0 && p.id < 1000 && !_ownedPlayerIds.Contains(p.id))
                            _ownedPlayerIds.Add(p.id);
                    }
                }
                if (_ownedPlayerIds.Count == 0 || _ownedPlayerIds.Contains(0) ||
                    _ownedPlayerIds.Count != new HashSet<int>(_ownedPlayerIds).Count)
                    throw new InvalidDataException("The saved roster contains missing or duplicate player IDs.");
                foreach (var playerId in _ownedPlayerIds)
                    _catalog.GetPlayer(playerId);
            }

            if (_lineupIds == null)
            {
                var savedLineup = PlayerPrefs.GetString(LineupPreferenceKey, string.Empty);
                _lineupIds = string.IsNullOrEmpty(savedLineup)
                    ? (int[])_catalog.Data.initialLineup.Clone()
                    : ParsePlayerIds(savedLineup, "saved lineup").ToArray();
            }
            if (_lineupIds.Length != 7 || _lineupIds.Any(id => !_ownedPlayerIds.Contains(id)))
                throw new InvalidDataException("The saved lineup must contain seven owned player IDs.");
            ValidateLineupRoles(_catalog.ResolveLineup(_lineupIds));
        }

        private static List<int> ParsePlayerIds(string serialized, string source)
        {
            var ids = new List<int>();
            foreach (var value in serialized.Split(','))
            {
                if (!int.TryParse(value, out var playerId) || playerId <= 0)
                    throw new InvalidDataException($"The {source} contains an invalid player ID.");
                ids.Add(playerId);
            }
            return ids;
        }

        private static Transform CreateScrollList(string objectName, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, float contentHeight,
            Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            var viewportObject = new GameObject(objectName, typeof(RectTransform), typeof(Image),
                typeof(Mask), typeof(ScrollRect));
            viewportObject.transform.SetParent(parent, false);
            var viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = anchorMin;
            viewportRect.anchorMax = anchorMax;
            viewportRect.offsetMin = offsetMin ?? Vector2.zero;
            viewportRect.offsetMax = offsetMax ?? Vector2.zero;
            var viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.04f);
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;

            var contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewportObject.transform, false);
            var contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, contentHeight);

            var scroll = viewportObject.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            return contentRect;
        }

        private void ShowShop()
        {
            _catalog ??= GameCatalog.Load();
            ClearCanvas();

            // Authentic green shelf background (v1.2.5 native)
            AddStretchImage("ShopBackground", "Original/sprites/TrophyRoomBGSS/GreenShelfBG", _canvas.transform);
            _safeArea.SetAsLastSibling();

            AddMenuHeader(null, ShowHome);

            // Top wood beam trim directly under fixed header
            var topBeam = AddResourceImage("TopBeam", "Original/sprites/TrophyRoomBGSS/WoodenShelfBG", _safeArea,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            topBeam.rectTransform.anchoredPosition = new Vector2(0f, -82f);
            topBeam.rectTransform.sizeDelta = new Vector2(800f, 18f);
            topBeam.preserveAspect = false;
            topBeam.raycastTarget = false;

            // Centered 640x960 frame so shelves, boxes, and navigation arrows always align pixel-perfectly
            var shopFrame = CreateCenteredReferenceFrame("ShopFrame");

            // 3 horizontal wooden shelves across screen
            var shelfYs = new[] { 0.67f, 0.46f, 0.25f };
            for (var s = 0; s < shelfYs.Length; s++)
            {
                var shelf = AddResourceImage($"Shelf_{s}", "Original/sprites/TrophyRoomBGSS/WoodenShelfBG", shopFrame,
                    new Vector2(0.5f, shelfYs[s]), new Vector2(0.5f, shelfYs[s]));
                shelf.rectTransform.sizeDelta = new Vector2(800f, 26f);
                shelf.preserveAspect = false;
                shelf.raycastTarget = false;
            }

            // Render current tab products on the 3 shelves
            if (_shopTab == 0)
                RenderShopCoins(shopFrame);
            else if (_shopTab == 1)
                RenderShopShirts(shopFrame);
            else
                RenderShopShorts(shopFrame);

            // Bottom authentic Shop category tabs docked in fixed 106px bottom bar
            var shopTabBar = CreateBottomBar("ShopTabsBar", 106f);
            var coinsSprite = _shopTab == 0 ? "Original/sprites/ShopSS/CoinsTab" : "Original/sprites/ShopSS/CoinsTabPressed";
            AddSpriteButton("CoinsTab", coinsSprite, coinsSprite, shopTabBar,
                new Vector2(0.18f, 0.50f), new Vector2(0.31f, 0.86f), () =>
                {
                    _shopTab = 0;
                    _shopPage = 0;
                    AudioManager.Instance.PlayTap();
                    ShowShop();
                });

            var shirtsSprite = _shopTab == 1 ? "Original/sprites/ShopSS/ShirtTab" : "Original/sprites/ShopSS/ShirtTabPressed";
            AddSpriteButton("ShirtTab", shirtsSprite, shirtsSprite, shopTabBar,
                new Vector2(0.50f, 0.50f), new Vector2(0.31f, 0.86f), () =>
                {
                    _shopTab = 1;
                    _shopPage = 0;
                    AudioManager.Instance.PlayTap();
                    ShowShop();
                });

            var shortsSprite = _shopTab == 2 ? "Original/sprites/ShopSS/ShortsTab" : "Original/sprites/ShopSS/ShortsTabPressed";
            AddSpriteButton("ShortsTab", shortsSprite, shortsSprite, shopTabBar,
                new Vector2(0.82f, 0.50f), new Vector2(0.31f, 0.86f), () =>
                {
                    _shopTab = 2;
                    _shopPage = 0;
                    AudioManager.Instance.PlayTap();
                    ShowShop();
                });
        }

        private void RenderShopCoins(Transform parentFrame)
        {
            var coinPackages = new[]
            {
                new { coins = 25000, price = "FREE", pouches = 1 },
                new { coins = 250000, price = "FREE", pouches = 2 },
                new { coins = 1200000, price = "FREE", pouches = 3 }
            };

            var cardYs = new[] { 0.765f, 0.555f, 0.345f };

            for (var i = 0; i < 3; i++)
            {
                var pkg = coinPackages[i];
                var cardY = cardYs[i];

                var box = AddResourceImage($"ShopBox_{i}", "Original/sprites/ShopSS/ShopBox", parentFrame,
                    new Vector2(0.5f, cardY), new Vector2(0.5f, cardY));
                box.rectTransform.sizeDelta = new Vector2(300f, 180f);
                box.preserveAspect = false;

                // Pouches
                if (pkg.pouches == 1)
                {
                    var pouch = AddResourceImage("Pouch", "Original/sprites/ShopSS/ShopMoneyPouch", box.transform,
                        new Vector2(0.5f, 0.67f), new Vector2(0.5f, 0.67f));
                    pouch.rectTransform.sizeDelta = new Vector2(160f, 116f);
                    pouch.preserveAspect = true;
                    pouch.raycastTarget = false;
                }
                else if (pkg.pouches == 2)
                {
                    var p1 = AddResourceImage("Pouch1", "Original/sprites/ShopSS/ShopMoneyPouch", box.transform,
                        new Vector2(0.35f, 0.67f), new Vector2(0.35f, 0.67f));
                    p1.rectTransform.sizeDelta = new Vector2(120f, 88f);
                    p1.preserveAspect = true;
                    p1.raycastTarget = false;

                    var p2 = AddResourceImage("Pouch2", "Original/sprites/ShopSS/ShopMoneyPouch", box.transform,
                        new Vector2(0.65f, 0.67f), new Vector2(0.65f, 0.67f));
                    p2.rectTransform.sizeDelta = new Vector2(120f, 88f);
                    p2.preserveAspect = true;
                    p2.raycastTarget = false;
                }
                else
                {
                    var p1 = AddResourceImage("Pouch1", "Original/sprites/ShopSS/ShopMoneyPouch", box.transform,
                        new Vector2(0.26f, 0.64f), new Vector2(0.26f, 0.64f));
                    p1.rectTransform.sizeDelta = new Vector2(100f, 74f);
                    p1.preserveAspect = true;
                    p1.raycastTarget = false;

                    var p2 = AddResourceImage("Pouch2", "Original/sprites/ShopSS/ShopMoneyPouch", box.transform,
                        new Vector2(0.50f, 0.70f), new Vector2(0.50f, 0.70f));
                    p2.rectTransform.sizeDelta = new Vector2(110f, 80f);
                    p2.preserveAspect = true;
                    p2.raycastTarget = false;

                    var p3 = AddResourceImage("Pouch3", "Original/sprites/ShopSS/ShopMoneyPouch", box.transform,
                        new Vector2(0.74f, 0.64f), new Vector2(0.74f, 0.64f));
                    p3.rectTransform.sizeDelta = new Vector2(100f, 74f);
                    p3.preserveAspect = true;
                    p3.raycastTarget = false;
                }

                // Money bar with coin count
                var bar = AddResourceImage("MoneyBar", "Original/sprites/ShopSS/ShopMoneyBar", box.transform,
                    new Vector2(0.5f, 0.31f), new Vector2(0.5f, 0.31f));
                bar.rectTransform.sizeDelta = new Vector2(210f, 38f);
                bar.preserveAspect = false;
                bar.raycastTarget = false;

                var coinsTxt = AddText(pkg.coins.ToString("N0"), bar.transform, 18, Ink,
                    new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
                coinsTxt.alignment = TextAnchor.MiddleCenter;
                coinsTxt.fontStyle = FontStyle.Bold;

                // Price label below bar
                var priceTxt = AddText(pkg.price, box.transform, 17, Ink,
                    new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.20f));
                priceTxt.alignment = TextAnchor.MiddleCenter;
                priceTxt.fontStyle = FontStyle.Bold;

                // Tapping card rewards coins (no microtransactions)
                var boxBtn = box.gameObject.AddComponent<Button>();
                var capturedCoins = pkg.coins;
                boxBtn.onClick.AddListener(() =>
                {
                    _careerCash += capturedCoins;
                    SaveCareerState();
                    AudioManager.Instance.PlayCash();
                    ShowShop();
                });
            }

            // Left / Right arrows on middle shelf
            var leftArrow = AddSpriteButton("LeftArrow", "Original/images/LeftArrowDisabled",
                "Original/images/LeftArrowDisabled", parentFrame,
                new Vector2(0.09f, 0.555f), new Vector2(0.10f, 0.10f), null);
            leftArrow.interactable = false;

            var rightArrow = AddSpriteButton("RightArrow", "Original/images/RightArrowSelected",
                "Original/images/RightArrowSelected", parentFrame,
                new Vector2(0.91f, 0.555f), new Vector2(0.10f, 0.10f), null);
            rightArrow.interactable = false;
        }

        private void RenderShopShirts(Transform parentFrame)
        {
            var shirts = _catalog.Data.shirts ?? Array.Empty<ShirtData>();
            const int perPage = 3;
            var maxPages = Mathf.Max(1, Mathf.CeilToInt(shirts.Length / (float)perPage));
            _shopPage = Mathf.Clamp(_shopPage, 0, maxPages - 1);

            var startIdx = _shopPage * perPage;
            var count = Mathf.Min(perPage, shirts.Length - startIdx);
            var cardYs = new[] { 0.765f, 0.555f, 0.345f };

            for (var i = 0; i < count; i++)
            {
                var shirt = shirts[startIdx + i];
                var cardY = cardYs[i];

                var box = AddResourceImage($"ShopBox_Shirt_{i}", "Original/sprites/ShopSS/ShopBox", parentFrame,
                    new Vector2(0.5f, cardY), new Vector2(0.5f, cardY));
                box.rectTransform.sizeDelta = new Vector2(300f, 180f);
                box.preserveAspect = false;

                var previewBox = new GameObject("KitPreview", typeof(RectTransform));
                previewBox.transform.SetParent(box.transform, false);
                var prevRect = previewBox.GetComponent<RectTransform>();
                prevRect.anchorMin = new Vector2(0.5f, 0.60f);
                prevRect.anchorMax = new Vector2(0.5f, 0.60f);
                prevRect.sizeDelta = new Vector2(145f, 105f);
                prevRect.pivot = new Vector2(0.5f, 0.5f);

                var shirtBase = AddResourceImage("ShirtBase", "Original/sprites/KitSelectionSS/KitSelectionShirt", previewBox.transform,
                    Vector2.zero, Vector2.one);
                shirtBase.preserveAspect = true;
                shirtBase.color = _catalog.GetColourRgb(shirt.shirtColour);
                shirtBase.raycastTarget = false;

                var sleeves = AddResourceImage("Sleeves", "Original/sprites/KitSelectionSS/KitSelectionSleeves", previewBox.transform,
                    Vector2.zero, Vector2.one);
                sleeves.preserveAspect = true;
                sleeves.color = _catalog.GetColourRgb(shirt.sleeveColour);
                sleeves.raycastTarget = false;

                if (shirt.shirtStyle > 0 && shirt.shirtStyle <= 7)
                {
                    var pattern = AddResourceImage("Pattern", $"Original/sprites/KitSelectionSS/KitSelectionDesign{shirt.shirtStyle}", previewBox.transform,
                        Vector2.zero, Vector2.one);
                    pattern.preserveAspect = true;
                    pattern.color = _catalog.GetColourRgb(shirt.styleColour);
                    pattern.raycastTarget = false;
                }

                var owned = _unlockedShirtIds.Contains(shirt.id);
                if (owned)
                {
                    var soldOut = AddResourceImage("SoldOut", "Original/sprites/ShopSS/ShopSoldOut", box.transform,
                        new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.16f));
                    soldOut.rectTransform.sizeDelta = new Vector2(150f, 48f);
                    soldOut.preserveAspect = true;
                    soldOut.raycastTarget = false;
                }
                else
                {
                    var priceTxt = AddText("FREE", box.transform, 18, Ink,
                        new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.22f));
                    priceTxt.alignment = TextAnchor.MiddleCenter;
                    priceTxt.fontStyle = FontStyle.Bold;

                    var capturedShirt = shirt;
                    var boxBtn = box.gameObject.AddComponent<Button>();
                    boxBtn.onClick.AddListener(() =>
                    {
                        _unlockedShirtIds.Add(capturedShirt.id);
                        SaveCareerState();
                        AudioManager.Instance.PlayBuy();
                        ShowShop();
                    });
                }
            }

            // Left & Right navigation arrows on middle shelf
            var canPrev = _shopPage > 0;
            var leftArrow = AddSpriteButton("LeftArrow",
                canPrev ? "Original/images/LeftArrow" : "Original/images/LeftArrowDisabled",
                canPrev ? "Original/images/LeftArrowSelected" : "Original/images/LeftArrowDisabled",
                parentFrame, new Vector2(0.09f, 0.555f), new Vector2(0.10f, 0.10f),
                canPrev ? () => { _shopPage--; ShowShop(); } : null);
            leftArrow.interactable = canPrev;

            var canNext = _shopPage < maxPages - 1;
            var rightArrow = AddSpriteButton("RightArrow",
                canNext ? "Original/images/RightArrowSelected" : "Original/images/RightArrowDisabled",
                canNext ? "Original/images/RightArrowSelected" : "Original/images/RightArrowDisabled",
                parentFrame, new Vector2(0.91f, 0.555f), new Vector2(0.10f, 0.10f),
                canNext ? () => { _shopPage++; ShowShop(); } : null);
            rightArrow.interactable = canNext;
        }

        private void RenderShopShorts(Transform parentFrame)
        {
            var colours = _catalog.Data.colours ?? Array.Empty<ColourData>();
            const int perPage = 3;
            var maxPages = Mathf.Max(1, Mathf.CeilToInt(colours.Length / (float)perPage));
            _shopPage = Mathf.Clamp(_shopPage, 0, maxPages - 1);

            var startIdx = _shopPage * perPage;
            var count = Mathf.Min(perPage, colours.Length - startIdx);
            var cardYs = new[] { 0.765f, 0.555f, 0.345f };

            for (var i = 0; i < count; i++)
            {
                var col = colours[startIdx + i];
                var cardY = cardYs[i];

                var box = AddResourceImage($"ShopBox_Shorts_{i}", "Original/sprites/ShopSS/ShopBox", parentFrame,
                    new Vector2(0.5f, cardY), new Vector2(0.5f, cardY));
                box.rectTransform.sizeDelta = new Vector2(300f, 180f);
                box.preserveAspect = false;

                var shorts = AddResourceImage("ShortsPreview", "Original/sprites/KitSelectionSS/KitSelectionShorts", box.transform,
                    new Vector2(0.5f, 0.60f), new Vector2(0.5f, 0.60f));
                shorts.rectTransform.sizeDelta = new Vector2(145f, 105f);
                shorts.preserveAspect = true;
                shorts.color = _catalog.GetColourRgb(col.id);
                shorts.raycastTarget = false;

                var owned = _unlockedPantsIds.Contains(col.id);
                if (owned)
                {
                    var soldOut = AddResourceImage("SoldOut", "Original/sprites/ShopSS/ShopSoldOut", box.transform,
                        new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.16f));
                    soldOut.rectTransform.sizeDelta = new Vector2(150f, 48f);
                    soldOut.preserveAspect = true;
                    soldOut.raycastTarget = false;
                }
                else
                {
                    var priceTxt = AddText("FREE", box.transform, 18, Ink,
                        new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.22f));
                    priceTxt.alignment = TextAnchor.MiddleCenter;
                    priceTxt.fontStyle = FontStyle.Bold;

                    var capturedCol = col;
                    var boxBtn = box.gameObject.AddComponent<Button>();
                    boxBtn.onClick.AddListener(() =>
                    {
                        _unlockedPantsIds.Add(capturedCol.id);
                        SaveCareerState();
                        AudioManager.Instance.PlayBuy();
                        ShowShop();
                    });
                }
            }

            // Left & Right navigation arrows on middle shelf
            var canPrev = _shopPage > 0;
            var leftArrow = AddSpriteButton("LeftArrow",
                canPrev ? "Original/images/LeftArrow" : "Original/images/LeftArrowDisabled",
                canPrev ? "Original/images/LeftArrowSelected" : "Original/images/LeftArrowDisabled",
                parentFrame, new Vector2(0.09f, 0.555f), new Vector2(0.10f, 0.10f),
                canPrev ? () => { _shopPage--; ShowShop(); } : null);
            leftArrow.interactable = canPrev;

            var canNext = _shopPage < maxPages - 1;
            var rightArrow = AddSpriteButton("RightArrow",
                canNext ? "Original/images/RightArrowSelected" : "Original/images/RightArrowDisabled",
                canNext ? "Original/images/RightArrowSelected" : "Original/images/RightArrowDisabled",
                parentFrame, new Vector2(0.91f, 0.555f), new Vector2(0.10f, 0.10f),
                canNext ? () => { _shopPage++; ShowShop(); } : null);
            rightArrow.interactable = canNext;
        }

        private void ShowKitSelection()
        {
            _catalog ??= GameCatalog.Load();
            ClearCanvas();

            // Background (aspect-fill via EnvelopeParent keeps mannequin unstretched in all aspect ratios)
            var bg = AddResourceImage("KitBG", "Original/images/KitSelectionBG", _safeArea,
                Vector2.zero, Vector2.one);
            bg.preserveAspect = false;
            var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = ReferenceWidth / ReferenceHeight;

            // Header: Back button, cash, crest
            var header = AddMenuHeader(null, () =>
            {
                SaveCareerState();
                AudioManager.Instance.PlayTap();
                ShowManage();
            });

            // Tap crest on header to cycle crest (0.85f to 0.97f inside fixed header)
            AddButton("", header, Color.clear,
                new Vector2(0.91f, 0.50f), new Vector2(0.12f, 1.0f), () =>
                {
                    _currentCrestId = (_currentCrestId % 3) + 1;
                    SaveCareerState();
                    AudioManager.Instance.PlayTap();
                    ShowKitSelection();
                });

            // ==========================================
            // CENTER: Character Kit Preview (parented to KitBG so shirt/shorts stay locked to mannequin!)
            // ==========================================
            var kitBox = new GameObject("CenterKitPreview", typeof(RectTransform));
            kitBox.transform.SetParent(bg.transform, false);
            var kitRect = kitBox.GetComponent<RectTransform>();
            kitRect.anchorMin = new Vector2(185f / 640f, (960f - 527f - 350f) / 960f);
            kitRect.anchorMax = new Vector2((185f + 260f) / 640f, (960f - 527f) / 960f);
            kitRect.offsetMin = Vector2.zero;
            kitRect.offsetMax = Vector2.zero;

            // Layer 1: Shorts
            var shorts = AddResourceImage("Shorts", "Original/sprites/KitSelectionSS/KitSelectionShorts", kitBox.transform,
                Vector2.zero, Vector2.one);
            shorts.preserveAspect = true;
            shorts.color = _catalog.GetColourRgb(_currentPantsId);

            // Layer 2: Shirt Body
            var shirt = AddResourceImage("Shirt", "Original/sprites/KitSelectionSS/KitSelectionShirt", kitBox.transform,
                Vector2.zero, Vector2.one);
            shirt.preserveAspect = true;
            shirt.color = _catalog.GetColourRgb(_currentShirtColorId);

            // Layer 3: Sleeves
            var sleeves = AddResourceImage("Sleeves", "Original/sprites/KitSelectionSS/KitSelectionSleeves", kitBox.transform,
                Vector2.zero, Vector2.one);
            sleeves.preserveAspect = true;
            sleeves.color = _catalog.GetColourRgb(_currentSleeveColorId);

            // Layer 4: Pattern Design (if style > 0)
            if (_currentPatternStyle > 0 && _currentPatternStyle <= 7)
            {
                var pat = AddResourceImage("Pattern", $"Original/sprites/KitSelectionSS/KitSelectionDesign{_currentPatternStyle}", kitBox.transform,
                    Vector2.zero, Vector2.one);
                pat.preserveAspect = true;
                pat.color = _catalog.GetColourRgb(_currentPatternColorId);
            }

            // ==========================================
            // Top Pair of Silver Arrows: Cycle Shirt Presets (chest level inside KitBG)
            // ==========================================
            var allShirts = _catalog.Data.shirts ?? Array.Empty<ShirtData>();
            AddSpriteButton("ShirtLeft", "Original/images/LeftArrowDisabled", "Original/images/LeftArrowSelected", bg.transform,
                new Vector2(0.26f, 0.38f), new Vector2(0.08f, 0.08f), () =>
                {
                    if (allShirts.Length > 0)
                    {
                        int currIdx = Array.FindIndex(allShirts, s => s.id == _currentShirtId);
                        if (currIdx < 0) currIdx = 0;
                        int nextIdx = (currIdx - 1 + allShirts.Length) % allShirts.Length;
                        ApplyShirt(allShirts[nextIdx]);
                    }
                });

            AddSpriteButton("ShirtRight", "Original/images/RightArrowDisabled", "Original/images/RightArrowSelected", bg.transform,
                new Vector2(0.74f, 0.38f), new Vector2(0.08f, 0.08f), () =>
                {
                    if (allShirts.Length > 0)
                    {
                        int currIdx = Array.FindIndex(allShirts, s => s.id == _currentShirtId);
                        if (currIdx < 0) currIdx = 0;
                        int nextIdx = (currIdx + 1) % allShirts.Length;
                        ApplyShirt(allShirts[nextIdx]);
                    }
                });

            // ==========================================
            // Bottom Pair of Silver Arrows: Cycle Shorts Colors (hip/shorts level inside KitBG)
            // ==========================================
            var allColours = _catalog.Data.colours ?? Array.Empty<ColourData>();
            AddSpriteButton("ShortsLeft", "Original/images/LeftArrowDisabled", "Original/images/LeftArrowSelected", bg.transform,
                new Vector2(0.26f, 0.22f), new Vector2(0.08f, 0.08f), () =>
                {
                    if (allColours.Length > 0)
                    {
                        int currIdx = Array.FindIndex(allColours, c => c.id == _currentPantsId);
                        if (currIdx < 0) currIdx = 0;
                        int nextIdx = (currIdx - 1 + allColours.Length) % allColours.Length;
                        _currentPantsId = allColours[nextIdx].id;
                        SaveCareerState();
                        AudioManager.Instance.PlayTap();
                        ShowKitSelection();
                    }
                });

            AddSpriteButton("ShortsRight", "Original/images/RightArrowDisabled", "Original/images/RightArrowSelected", bg.transform,
                new Vector2(0.74f, 0.22f), new Vector2(0.08f, 0.08f), () =>
                {
                    if (allColours.Length > 0)
                    {
                        int currIdx = Array.FindIndex(allColours, c => c.id == _currentPantsId);
                        if (currIdx < 0) currIdx = 0;
                        int nextIdx = (currIdx + 1) % allColours.Length;
                        _currentPantsId = allColours[nextIdx].id;
                        SaveCareerState();
                        AudioManager.Instance.PlayTap();
                        ShowKitSelection();
                    }
                });

            // ==========================================
            // Bottom Right: Authentic Done Button docked cleanly at bottom bar
            // ==========================================
            var kitBottomBar = CreateBottomBar("KitBottomBar", 120f);
            AddSpriteButton("DoneBtn", "Original/sprites/KitSelectionSS/DoneButton",
                "Original/sprites/KitSelectionSS/DoneButtonPressed", kitBottomBar,
                new Vector2(0.78f, 0.56f), new Vector2(0.24f, 0.52f), () =>
                {
                    SaveCareerState();
                    AudioManager.Instance.PlayTap();
                    ShowManage();
                });
        }

        private void ApplyShirt(ShirtData s)
        {
            _currentShirtId = s.id;
            _currentShirtColorId = s.shirtColour;
            _currentSleeveColorId = s.sleeveColour;
            _currentPatternStyle = s.shirtStyle;
            _currentPatternColorId = s.styleColour;
            SaveCareerState();
            AudioManager.Instance.PlayTap();
            ShowKitSelection();
        }

        private void ShowTrophyRoom()
        {
            _catalog ??= GameCatalog.Load();
            ClearCanvas();
            AddMenuBackdrop(true);
            var header = AddMenuHeader($"TROPHIES  {_unlockedAchievementIds.Count}/72", ShowHome);

            if (PlayerPrefs.GetInt("diceSoccer.tutorial.4", 0) == 0)
                ShowTutorialPopup(4);

            const int cols = 4;
            const int rows = 3;
            const int perPage = cols * rows;
            const int totalTrophies = 72;
            var maxPages = Mathf.CeilToInt(totalTrophies / (float)perPage);
            _trophyPage = Mathf.Clamp(_trophyPage, 0, maxPages - 1);

            var frame = CreateCenteredReferenceFrame("TrophyRoomFrame");

            var shelfYs = new[] { 0.70f, 0.48f, 0.26f };
            foreach (var sy in shelfYs)
            {
                var shelf = AddResourceImage("Shelf", "Original/sprites/TrophyRoomBGSS/WoodenShelfBG", frame,
                    new Vector2(0.02f, sy - 0.08f), new Vector2(0.98f, sy - 0.05f));
                shelf.preserveAspect = false;
                shelf.raycastTarget = false;
            }

            var startIdx = _trophyPage * perPage;
            var colXs = new[] { 0.16f, 0.38f, 0.62f, 0.84f };

            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < cols; c++)
                {
                    var trophyId = startIdx + r * cols + c + 1;
                    if (trophyId > totalTrophies) continue;

                    var posX = colXs[c];
                    var posY = shelfYs[r] + 0.035f;
                    var unlocked = _unlockedAchievementIds.Contains(trophyId);

                    var baseSprite = unlocked ? "Original/sprites/TrophyBG/TrophyBG" : "Original/sprites/TrophyBG/TrophyBGLocked";
                    var capturedId = trophyId;
                    var slotBtn = AddSpriteButton($"TrophySlot_{capturedId}", baseSprite, baseSprite, frame,
                        new Vector2(posX, posY), new Vector2(0.20f, 0.14f), () =>
                        {
                            AudioManager.Instance.PlayTap();
                            ShowTrophyDetails(capturedId);
                        });

                    if (unlocked)
                    {
                        var trophyImg = AddResourceImage("TrophyIcon", $"Original/images/Trophy{trophyId:00}", slotBtn.transform,
                            new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
                        trophyImg.preserveAspect = true;
                        trophyImg.raycastTarget = false;
                    }
                }
            }

            const float navY = 0.12f;
            if (_trophyPage > 0)
            {
                AddSpriteButton("LeftArrow", "Original/images/LeftArrow", "Original/images/LeftArrowSelected", frame,
                    new Vector2(0.14f, navY), new Vector2(0.12f, 0.055f), () =>
                    {
                        _trophyPage--;
                        AudioManager.Instance.PlayTap();
                        ShowTrophyRoom();
                    });
            }

            AddText($"PAGE {_trophyPage + 1} OF {maxPages}", frame, 16, Paper,
                new Vector2(0.3f, navY - 0.025f), new Vector2(0.7f, navY + 0.025f));

            if (_trophyPage < maxPages - 1)
            {
                AddSpriteButton("RightArrow", "Original/images/RightArrow", "Original/images/RightArrowSelected", frame,
                    new Vector2(0.86f, navY), new Vector2(0.12f, 0.055f), () =>
                    {
                        _trophyPage++;
                        AudioManager.Instance.PlayTap();
                        ShowTrophyRoom();
                    });
            }

            AddSpriteButton("Refresh", "Original/sprites/TrophyRoomBGSS/RefreshButton", "Original/sprites/TrophyRoomBGSS/RefreshButtonPressed", header,
                new Vector2(0.805f, 0.50f), new Vector2(0.08f, 0.72f), () =>
                {
                    CheckAchievements(false, false);
                    AudioManager.Instance.PlayMedalCeremony();
                    ShowTrophyRoom();
                });
        }

        private void ShowTrophyDetails(int achievementId)
        {
            var ach = _catalog.GetAchievement(achievementId);
            if (ach == null) return;

            var modal = new GameObject("TrophyDetailsModal", typeof(RectTransform), typeof(Image));
            modal.transform.SetParent(_safeArea, false);
            var modalRect = modal.GetComponent<RectTransform>();
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.offsetMin = Vector2.zero;
            modalRect.offsetMax = Vector2.zero;
            modal.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);

            var box = AddResourceImage("Box", "Original/sprites/AchievementsSS4/LongDialogBox", modal.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            box.rectTransform.sizeDelta = new Vector2(560f, 440f);
            box.preserveAspect = false;

            var unlocked = _unlockedAchievementIds.Contains(achievementId);

            var trophy = AddResourceImage("Trophy", $"Original/images/Trophy{achievementId:00}", box.transform,
                new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f));
            trophy.rectTransform.sizeDelta = new Vector2(110f, 130f);
            trophy.preserveAspect = true;
            if (!unlocked)
                trophy.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);

            var statusText = unlocked ? "STATUS: UNLOCKED" : "STATUS: LOCKED";
            var statusColor = unlocked ? Grass : (Color)new Color32(230, 80, 50, 255);
            AddText(statusText, box.transform, 17, statusColor,
                new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f));

            var desc = unlocked ? ach.unlockedDescription : ach.lockedDescription;
            if (string.IsNullOrEmpty(desc)) desc = ach.lockedDescription;
            AddText(desc, box.transform, 16, Paper,
                new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.54f));

            var rewardText = "";
            if (ach.goldReward > 0) rewardText += $"+{ach.goldReward:N0} Cash  ";
            if (ach.entourageReward > 0) rewardText += $"Unlocks Entourage #{ach.entourageReward}  ";
            if (ach.pantsReward > 0) rewardText += $"Unlocks Pants #{ach.pantsReward}";
            if (!string.IsNullOrEmpty(rewardText))
            {
                AddText($"Reward: {rewardText}", box.transform, 14, Gold,
                    new Vector2(0.06f, 0.26f), new Vector2(0.94f, 0.36f));
            }

            AddButton("DONE", box.transform, Gold,
                new Vector2(0.5f, 0.12f), new Vector2(0.44f, 0.12f), () =>
                {
                    AudioManager.Instance.PlayTap();
                    Destroy(modal);
                });
        }

        private void ShowEntourage()
        {
            _catalog ??= GameCatalog.Load();
            ClearCanvas();
            AddMenuBackdrop(true);
            AddMenuHeader("ENTOURAGE STAFF", ShowManage);

            if (PlayerPrefs.GetInt("diceSoccer.tutorial.49", 0) == 0)
                ShowTutorialPopup(49);

            var topControls = new GameObject("EntourageTopControls", typeof(RectTransform)).GetComponent<RectTransform>();
            topControls.SetParent(_safeArea, false);
            topControls.anchorMin = new Vector2(0f, 1f);
            topControls.anchorMax = new Vector2(1f, 1f);
            topControls.pivot = new Vector2(0.5f, 1f);
            topControls.anchoredPosition = new Vector2(0f, -82f);
            topControls.sizeDelta = new Vector2(0f, 120f);

            var topTitle = AddText("EQUIPPED STAFF (MAX 2)", topControls, 15, Ink,
                new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.98f));
            topTitle.fontStyle = FontStyle.Bold;

            var slot1Name = _equippedEntourageIds.Count > 0 ? (_catalog.GetEntourage(_equippedEntourageIds[0])?.name ?? "STAFF 1") : "[EMPTY SLOT 1]";
            var slot2Name = _equippedEntourageIds.Count > 1 ? (_catalog.GetEntourage(_equippedEntourageIds[1])?.name ?? "STAFF 2") : "[EMPTY SLOT 2]";

            var s1Color = _equippedEntourageIds.Count > 0 ? Grass : (Color)new Color32(60, 65, 70, 255);
            AddButton(slot1Name, topControls, s1Color,
                new Vector2(0.265f, 0.34f), new Vector2(0.43f, 0.46f), () =>
                {
                    if (_equippedEntourageIds.Count > 0)
                    {
                        _equippedEntourageIds.RemoveAt(0);
                        SaveCareerState();
                        AudioManager.Instance.PlayTap();
                        ShowEntourage();
                    }
                });

            var s2Color = _equippedEntourageIds.Count > 1 ? Grass : (Color)new Color32(60, 65, 70, 255);
            AddButton(slot2Name, topControls, s2Color,
                new Vector2(0.735f, 0.34f), new Vector2(0.43f, 0.46f), () =>
                {
                    if (_equippedEntourageIds.Count > 1)
                    {
                        _equippedEntourageIds.RemoveAt(1);
                        SaveCareerState();
                        AudioManager.Instance.PlayTap();
                        ShowEntourage();
                    }
                });

            var staffList = _catalog.Data.entourage ?? Array.Empty<EntourageData>();
            var content = CreateScrollList("EntourageList", _safeArea,
                new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), staffList.Length * 115f,
                new Vector2(0f, 94f), new Vector2(0f, -210f));

            for (var i = 0; i < staffList.Length; i++)
            {
                var ent = staffList[i];
                var yMin = 1f - (i + 1) / (float)staffList.Length;
                var yMax = 1f - i / (float)staffList.Length;

                var card = AddResourceImage($"EntCard_{ent.id}", "Original/sprites/ShopSS/ShopBox", content,
                    new Vector2(0.02f, yMin + 0.005f), new Vector2(0.98f, yMax - 0.005f));
                card.preserveAspect = false;

                var figPath = GetEntourageFigurePath(ent.id);
                var fig = AddResourceImage("Figure", figPath, card.transform,
                    new Vector2(0.02f, 0.08f), new Vector2(0.22f, 0.92f));
                fig.preserveAspect = true;

                var roleTag = ent.position == "OFF" ? "[OFFICIAL]" : ent.position == "WAG" ? "[WAG]" : "[SPECIALIST]";
                var tagColor = ent.position == "OFF" ? new Color32(0, 80, 200, 255) : ent.position == "WAG" ? new Color32(195, 25, 115, 255) : new Color32(145, 70, 0, 255);

                var entName = AddText($"{roleTag} {ent.name.ToUpperInvariant()}", card.transform, 14, tagColor,
                    new Vector2(0.24f, 0.65f), new Vector2(0.72f, 0.95f));
                entName.fontStyle = FontStyle.Bold;

                AddText($"{ent.abilityName}: {ent.inGameDescription}", card.transform, 12, Ink,
                    new Vector2(0.24f, 0.12f), new Vector2(0.72f, 0.65f));

                var isEquipped = _equippedEntourageIds.Contains(ent.id);
                var isHired = _unlockedEntourageIds.Contains(ent.id);
                var capturedEnt = ent;

                if (isEquipped)
                {
                    AddButton("EQUIPPED", card.transform, Grass,
                        new Vector2(0.85f, 0.5f), new Vector2(0.24f, 0.5f), () =>
                        {
                            _equippedEntourageIds.Remove(capturedEnt.id);
                            SaveCareerState();
                            AudioManager.Instance.PlayTap();
                            ShowEntourage();
                        });
                }
                else if (isHired)
                {
                    var canEquip = _equippedEntourageIds.Count < 2;
                    AddButton(canEquip ? "EQUIP" : "FULL", card.transform, canEquip ? Gold : new Color32(60, 65, 70, 255),
                        new Vector2(0.85f, 0.5f), new Vector2(0.24f, 0.5f), () =>
                        {
                            if (_equippedEntourageIds.Count < 2)
                            {
                                _equippedEntourageIds.Add(capturedEnt.id);
                                SaveCareerState();
                                AudioManager.Instance.PlayTap();
                                ShowEntourage();
                            }
                        });
                }
                else
                {
                    bool isLockedByCriteria = !string.IsNullOrWhiteSpace(ent.unlockCriteria) && PlayerPrefs.GetInt("diceSoccer.allUnlocked", 0) == 0;
                    if (isLockedByCriteria)
                    {
                        var lockBtn = AddButton("LOCKED", card.transform, new Color32(75, 78, 84, 255),
                            new Vector2(0.85f, 0.5f), new Vector2(0.24f, 0.5f), () => AudioManager.Instance.PlayDisappoint());
                        lockBtn.interactable = true;
                        AddText(ent.unlockCriteria, card.transform, 10, new Color32(150, 45, 35, 255),
                            new Vector2(0.24f, 0.01f), new Vector2(0.72f, 0.16f));
                    }
                    else
                    {
                        var canAfford = _careerCash >= ent.cost;
                        string buyLabel = ent.cost <= 0 ? "FREE" : $"{ent.cost:N0}";
                        AddButton(canAfford ? buyLabel : $"NEED {ent.cost:N0}", card.transform, canAfford ? Grass : new Color32(60, 65, 70, 255),
                            new Vector2(0.85f, 0.5f), new Vector2(0.24f, 0.5f), () =>
                            {
                                if (_careerCash >= capturedEnt.cost)
                                {
                                    SpendCash(capturedEnt.cost);
                                    _unlockedEntourageIds.Add(capturedEnt.id);
                                    SaveCareerState();
                                    EvaluateCareerProgressAndAchievements(false, false, false, true);
                                    AudioManager.Instance.PlayBuy();
                                    ShowEntourage();
                                    ShowNextUnlockModalThen(null);
                                }
                            });
                    }
                }
            }

            var bottomBar = CreateBottomBar("EntourageBottomBar", 88f);
            AddButton("BACK TO PLAYERS", bottomBar, new Color32(68, 79, 84, 255),
                new Vector2(0.5f, 0.5f), new Vector2(0.6f, 0.62f), ShowManage);
        }

        private static string GetEntourageFigurePath(int id)
        {
            if (id == 1 || id == 7 || id == 15)
                return $"Original/sprites/EntourageFullFigure2/EntourageFullFigure{id:00}";
            return $"Original/sprites/EntourageFullFigure1/EntourageFullFigure{id:00}";
        }

        private void PlayEncounter()
        {
            if (_match == null || !_match.CanBeginEncounter || !_encounterButton.interactable || _inEncounterAnimation)
                return;

            PlayerPrefs.SetInt("diceSoccer.tutorial.roll", 1);
            PlayerPrefs.Save();

            _shakeArmed = false;
            _shakeQuietSince = -1f;
            _shakeRearmAfter = Time.unscaledTime + ShakeCooldownSeconds;
            _match.BeginEncounter();
            AudioManager.Instance.PlayDiceRoll();
            _encounterButton.interactable = false;
            _substitutionButton.interactable = false;
            if (_backButton != null) _backButton.interactable = false;
            var attackingSide = _match.Possession;
            StartCoroutine(AnimateEncounter(
                _match.LastHomeFaces, _match.LastAwayFaces, attackingSide));
        }

        private static bool IsAbilityTriggered(int trigger, int playerSlot, int[] faces, int total)
        {
            if (faces == null || faces.Length == 0) return false;

            int wildCount = 0;
            for (int i = 0; i < faces.Length; i++)
                if (faces[i] == 31) wildCount++;

            switch (trigger)
            {
                case 0: // Local reroll (processed during player's roll phase)
                case 1: // Captain tie breaker
                    return false;

                case 2: // PAIR: at least two dice show identical positive values (or Wild 31)
                    if (faces.Length < 2) return false;
                    if (wildCount >= 2) return true;
                    if (wildCount == 1)
                    {
                        for (int i = 0; i < faces.Length; i++)
                            if (faces[i] != 31 && faces[i] > 0) return true;
                    }
                    for (int a = 0; a < faces.Length; a++)
                        for (int b = a + 1; b < faces.Length; b++)
                            if (faces[a] == faces[b] && faces[a] > 0)
                                return true;
                    return false;

                case 3: // ALL dice are ODD (Wild 31 acts as wildcard)
                    for (int i = 0; i < faces.Length; i++)
                    {
                        if (faces[i] == 31) continue;
                        if (faces[i] <= 0 || faces[i] % 2 == 0) return false;
                    }
                    return true;

                case 4: // ALL dice are EVEN (Wild 31 acts as wildcard)
                    for (int i = 0; i < faces.Length; i++)
                    {
                        if (faces[i] == 31) continue;
                        if (faces[i] <= 0 || faces[i] % 2 != 0) return false;
                    }
                    return true;

                case 5: // 3 consecutive numbers (Wild 31 acts as wildcard)
                    if (faces.Length < 3) return false;
                    var nonWild = new List<int>(3);
                    for (int i = 0; i < 3; i++)
                    {
                        if (faces[i] == 31) continue;
                        if (faces[i] <= 0) return false;
                        nonWild.Add(faces[i]);
                    }
                    if (nonWild.Count <= 1) return true;
                    nonWild.Sort();
                    if (nonWild.Count == 2)
                    {
                        int gap = nonWild[1] - nonWild[0];
                        return gap == 1 || gap == 2;
                    }
                    return (nonWild[1] == nonWild[0] + 1) && (nonWild[2] == nonWild[1] + 1);

                case 6: // TRIPLE: all 3 dice identical (Wild 31 acts as wildcard)
                    if (faces.Length < 3) return false;
                    int firstVal = -1;
                    for (int i = 0; i < 3; i++)
                    {
                        if (faces[i] == 31) continue;
                        if (faces[i] <= 0) return false;
                        if (firstVal < 0) firstVal = faces[i];
                        else if (faces[i] != firstVal) return false;
                    }
                    return true;

                case 7: // Total is ODD (Wild 31 acts as wildcard)
                    return wildCount > 0 || (total % 2 != 0);

                case 8: // Total is EVEN (Wild 31 acts as wildcard)
                    return wildCount > 0 || (total % 2 == 0);

                case 9:
                case 10:
                case 11:
                case 12:
                case 13:
                case 14:
                case 15:
                case 16:
                case 17:
                case 18:
                    int val9 = trigger - 8;
                    return Array.IndexOf(faces, val9) >= 0 || wildCount > 0;

                case 19:
                case 20:
                case 21:
                case 22:
                case 23:
                case 24:
                case 25:
                case 26:
                case 27:
                case 28:
                    int val19 = trigger - 18;
                    return Array.IndexOf(faces, val19) >= 0 || wildCount > 0;

                default:
                    return false;
            }
        }

        private static int GetAbilityExecutionPhase(AbilityData ab)
        {
            if (ab == null) return 6;
            // Phase 0: Intercept (removes/cancels opponent re-rolls)
            if (ab.id == 43 || (ab.id >= 76 && ab.id <= 79) || ab.id == 81) return 0;
            // Phase 1: Re-rolls (Assist, Parry, Cross, Perfect Cross)
            if (ab.type == 5 && ab.id != 64 && ab.id != 31 && ab.id != 32) return 1;
            // Phase 2: Anchor (Immunity to all abilities)
            if (ab.id == 42 || ab.id == 52 || ab.id == 60) return 2;
            // Phase 3: Offensive Buffs & Defensive Debuffs (Multipliers, +1/+5, Tackles, Charge, Offside Trap)
            if (ab.id == 27 || ab.id == 59 || ab.id == 64 ||
                ab.id == 21 || ab.id == 39 || ab.id == 40 || ab.id == 54 || ab.id == 66 || ab.id == 74 || ab.id == 75 ||
                (ab.type == 0 && ab.id != 41)) return 3;
            // Phase 4: Cleanse, Strip, Saving Tackle, Feint
            if (ab.id == 28 || ab.id == 29 || ab.id == 30 || ab.id == 48 ||
                ab.id == 44 || ab.id == 22 || ab.id == 41 || ab.id == 56 || ab.id == 57 || ab.id == 58 ||
                ab.type == 3 || ab.type == 4) return 4;
            // Phase 5: End-of-Play (Through Pass, Dive!)
            return 5;
        }

        private static (int value, int lastRawFace) RollSinglePlayerDie(PlayerData player)
        {
            if (player == null || player.diceSides == null || player.diceSides.Length == 0)
            {
                int fallback = UnityEngine.Random.Range(2, 7);
                return (fallback, fallback);
            }

            int total = 0;
            int lastRawFace = 1;
            int safety = 0;
            while (safety++ < 5)
            {
                int f = player.diceSides[UnityEngine.Random.Range(0, player.diceSides.Length)];
                if (f >= 101) f -= 100;
                lastRawFace = f;
                if (f >= 1 && f <= 14) { total += f; break; }
                if (f >= 16 && f <= 18) { total += (f - 15); continue; }
                if (f == 24) continue;
                if (f == 31 || f == 32 || f == 23 || f == 26) break;
                total += Mathf.Clamp(f, 1, 6);
                break;
            }
            return (Mathf.Max(0, total), lastRawFace);
        }

        private void SetBuffBeltDisplay(Sprite medalSprite, string message, string badgeText = "")
        {
            if (_buffBeltGo != null) _buffBeltGo.SetActive(true);
            if (_buffBeltMedal != null && medalSprite != null) _buffBeltMedal.sprite = medalSprite;
            if (_buffBeltBadgeText != null) _buffBeltBadgeText.text = badgeText ?? string.Empty;
            if (_buffBeltText != null) _buffBeltText.text = message ?? string.Empty;
        }

        private void HideBuffBelt()
        {
            if (_buffBeltBadgeText != null) _buffBeltBadgeText.text = string.Empty;
            if (_buffBeltGo != null) _buffBeltGo.SetActive(false);
        }

        private static string GetAbilityBadgeOverlayText(AbilityData ab)
        {
            if (ab != null && ab.id == 18) return "3";
            return string.Empty;
        }

        private void ClearSideDieVisualForRoll(int side, int dieIdx)
        {
            var dImgs = (side == 0) ? _homeDice : _awayDice;
            var labels = (side == 0) ? _homeDieValueLabels : _awayDieValueLabels;
            if (dImgs != null && dieIdx >= 0 && dieIdx < dImgs.Length && dImgs[dieIdx] != null)
                dImgs[dieIdx].enabled = false;
            if (labels != null && dieIdx >= 0 && dieIdx < labels.Length && labels[dieIdx] != null)
                labels[dieIdx].enabled = false;
        }

        private void SetSideDieVisual(int side, int dieIdx, int value, int rawComboFace = 0, bool allowSpecialIcon = true)
        {
            var dImgs = (side == 0) ? _homeDice : _awayDice;
            var labels = (side == 0) ? _homeDieValueLabels : _awayDieValueLabels;
            Image faceImg = (dImgs != null && dieIdx >= 0 && dieIdx < dImgs.Length) ? dImgs[dieIdx] : null;
            Text valLabel = (labels != null && dieIdx >= 0 && dieIdx < labels.Length) ? labels[dieIdx] : null;

            bool isSpecialZero = allowSpecialIcon && value == 0 &&
                (rawComboFace == 31 || rawComboFace == 32 || rawComboFace == 23 || rawComboFace == 26);

            if (isSpecialZero)
            {
                if (valLabel != null) valLabel.enabled = false;
                if (faceImg != null) SetDieFace(faceImg, rawComboFace);
                return;
            }

            if (value >= 1 && value <= 14)
            {
                if (valLabel != null) valLabel.enabled = false;
                if (faceImg != null) SetDieFace(faceImg, value);
                return;
            }

            // Numeric values 0 (tackled / disqualified) or >= 15 (buffed/multiplied totals) render crisp numeric digits
            if (faceImg != null) faceImg.enabled = false;
            if (valLabel != null)
            {
                valLabel.text = Mathf.Max(0, value).ToString();
                valLabel.enabled = true;
            }
        }

        private void SetRawSideDieFace(int side, int dieIdx, int rawFace)
        {
            var dImgs = (side == 0) ? _homeDice : _awayDice;
            var labels = (side == 0) ? _homeDieValueLabels : _awayDieValueLabels;
            if (labels != null && dieIdx >= 0 && dieIdx < labels.Length && labels[dieIdx] != null)
                labels[dieIdx].enabled = false;
            if (dImgs != null && dieIdx >= 0 && dieIdx < dImgs.Length && dImgs[dieIdx] != null)
                SetDieFace(dImgs[dieIdx], rawFace);
        }

        private IEnumerator AnimateEncounter(int[] homeFaces, int[] awayFaces, int attackingSide)
        {
            _inEncounterAnimation = true;
            if (_shakePrompt != null) _shakePrompt.gameObject.SetActive(false);
            HideBuffBelt();
            _matchReport.text = "Rolling the dice...";

            var loc = _match.Location;
            var oldLocation = loc;
            var (activeHome, activeAway) = GetActiveDuelSlots(loc);
            float[] ys = { 0.25f, 0.50f, 0.75f };

            var homeSeqs = _match.LastHomeRollSequences;
            var awaySeqs = _match.LastAwayRollSequences;

            // 1. Poof smoke on active players before rolling
            for (var i = 0; i < activeHome.Length; i++)
            {
                var p = _homePlayers[activeHome[i]];
                if (p != null) StartCoroutine(PlayPoofAtPosition(new Vector2(p.rectTransform.anchorMin.x, p.rectTransform.anchorMin.y + 0.12f)));
            }
            for (var i = 0; i < activeAway.Length; i++)
            {
                var p = _awayPlayers[activeAway[i]];
                if (p != null) StartCoroutine(PlayPoofAtPosition(new Vector2(p.rectTransform.anchorMin.x, p.rectTransform.anchorMin.y + 0.12f)));
            }
            yield return new WaitForSeconds(0.12f);

            // 2. Hide active players, show dice roots at matching positions (matching media_1792421110113_45d0305f.png)
            for (var i = 0; i < activeHome.Length; i++) _homePlayers[activeHome[i]].gameObject.SetActive(false);
            for (var i = 0; i < activeAway.Length; i++) _awayPlayers[activeAway[i]].gameObject.SetActive(false);

            if (_homeDieRoots != null && _awayDieRoots != null)
            {
                for (var i = 0; i < 3; i++)
                {
                    if (i < homeFaces.Length && _homeDieRoots[i] != null)
                    {
                        _homeDieRoots[i].SetActive(true);
                        float y = homeFaces.Length == 1 ? 0.50f : ys[i];
                        float x = homeFaces.Length == 1 ? (oldLocation == 2 ? 0.28f : 0.33f) : 0.27f;
                        _homeDieRoots[i].GetComponent<RectTransform>().anchorMin =
                            _homeDieRoots[i].GetComponent<RectTransform>().anchorMax = new Vector2(x, y);
                        if (_homeDice[i] != null) _homeDice[i].enabled = false;
                        if (_homeDieValueLabels != null && _homeDieValueLabels[i] != null) _homeDieValueLabels[i].enabled = false;
                    }
                    else if (_homeDieRoots[i] != null) _homeDieRoots[i].SetActive(false);

                    if (i < awayFaces.Length && _awayDieRoots[i] != null)
                    {
                        _awayDieRoots[i].SetActive(true);
                        float y = awayFaces.Length == 1 ? 0.50f : ys[i];
                        float x = awayFaces.Length == 1 ? (oldLocation == 2 ? 0.67f : 0.72f) : 0.73f;
                        _awayDieRoots[i].GetComponent<RectTransform>().anchorMin =
                            _awayDieRoots[i].GetComponent<RectTransform>().anchorMax = new Vector2(x, y);
                        if (_awayDice[i] != null) _awayDice[i].enabled = false;
                        if (_awayDieValueLabels != null && _awayDieValueLabels[i] != null) _awayDieValueLabels[i].enabled = false;
                    }
                    else if (_awayDieRoots[i] != null) _awayDieRoots[i].SetActive(false);
                }

                // 3. Tumble rolling dice animation at 20 FPS (0.050s per frame, constant 0x3d4ccccd in Cocos2d binary)
                AudioManager.Instance.PlayDiceRoll();
                const float rollFrameDelay = 0.050f;
                for (var frame = 0; frame < 13; frame++)
                {
                    int rollIdx = frame + 1;
                    var rollSprite = GetUiSprite($"Original/sprites/PlayerBody3/DiceRoll{rollIdx:00}");
                    for (var i = 0; i < homeFaces.Length; i++)
                        if (_homeDieRoots[i] != null) _homeDieRoots[i].GetComponent<Image>().sprite = rollSprite;
                    for (var i = 0; i < awayFaces.Length; i++)
                        if (_awayDieRoots[i] != null) _awayDieRoots[i].GetComponent<Image>().sprite = rollSprite;
                    yield return new WaitForSeconds(rollFrameDelay);
                }

                // 4. Settle dice on INITIAL faces (so Dribble 16..18 and Cross 24 are visibly shown first!)
                AudioManager.Instance.PlayTap();
                TriggerHaptic();
                var diceBaseSprite = GetUiSprite("Original/sprites/Buff6/DiceBackground");
                for (var i = 0; i < homeFaces.Length; i++)
                {
                    int initFace = (homeSeqs != null && i < homeSeqs.Length && homeSeqs[i] != null && homeSeqs[i].Length > 0)
                        ? homeSeqs[i][0] : homeFaces[i];
                    if (_homeDieRoots[i] != null) _homeDieRoots[i].GetComponent<Image>().sprite = diceBaseSprite;
                    SetRawSideDieFace(0, i, initFace);
                }
                for (var i = 0; i < awayFaces.Length; i++)
                {
                    int initFace = (awaySeqs != null && i < awaySeqs.Length && awaySeqs[i] != null && awaySeqs[i].Length > 0)
                        ? awaySeqs[i][0] : awayFaces[i];
                    if (_awayDieRoots[i] != null) _awayDieRoots[i].GetComponent<Image>().sprite = diceBaseSprite;
                    SetRawSideDieFace(1, i, initFace);
                }

                // Elastic settle bounce on landing
                for (int f = 0; f < 6; f++)
                {
                    float p = f / 5f;
                    float punch = 1f + Mathf.Sin(p * Mathf.PI) * 0.18f;
                    for (var i = 0; i < homeFaces.Length; i++)
                        if (_homeDieRoots[i] != null) _homeDieRoots[i].transform.localScale = new Vector3(punch, punch, 1f);
                    for (var i = 0; i < awayFaces.Length; i++)
                        if (_awayDieRoots[i] != null) _awayDieRoots[i].transform.localScale = new Vector3(punch, punch, 1f);
                    yield return new WaitForSeconds(0.025f);
                }
                for (var i = 0; i < homeFaces.Length; i++)
                    if (_homeDieRoots[i] != null) _homeDieRoots[i].transform.localScale = Vector3.one;
                for (var i = 0; i < awayFaces.Length; i++)
                    if (_awayDieRoots[i] != null) _awayDieRoots[i].transform.localScale = Vector3.one;
            }

            // Prepare combo faces (preserving 31 Wildcard) and numeric values (where 31 Wildcard = 0, and Dribble bonuses are tracked)
            int[] comboHomeFaces = (int[])homeFaces.Clone();
            int[] comboAwayFaces = (int[])awayFaces.Clone();
            int[] originalHomeFaces = new int[homeFaces.Length];
            int[] originalAwayFaces = new int[awayFaces.Length];
            int[] homeDribbleBonus = new int[homeFaces.Length];
            int[] awayDribbleBonus = new int[awayFaces.Length];
            bool[] homeBoosted = new bool[homeFaces.Length];
            bool[] awayBoosted = new bool[awayFaces.Length];
            bool[] homeReduced = new bool[homeFaces.Length];
            bool[] awayReduced = new bool[awayFaces.Length];
            bool[] homeImmune = new bool[homeFaces.Length];
            bool[] awayImmune = new bool[awayFaces.Length];
            bool[] homeIntercepted = new bool[homeFaces.Length];
            bool[] awayIntercepted = new bool[awayFaces.Length];

            var localTriggeredAbilities = new List<(int side, AbilityData ability, int slot)>();

            // 4b. Visually animate LocalReroll faces (Dribble +1/+2/+3 IDs 16..18 and Cross ID 24) on the pitch!
            for (int side = 0; side <= 1; side++)
            {
                bool isHome = (side == 0);
                var seqs = isHome ? homeSeqs : awaySeqs;
                var facesArr = isHome ? homeFaces : awayFaces;
                var comboArr = isHome ? comboHomeFaces : comboAwayFaces;
                var origArr = isHome ? originalHomeFaces : originalAwayFaces;
                var dribArr = isHome ? homeDribbleBonus : awayDribbleBonus;
                var boostArr = isHome ? homeBoosted : awayBoosted;
                var dieRoots = isHome ? _homeDieRoots : _awayDieRoots;
                var dieBadges = isHome ? _homeDieBuffBadges : _awayDieBuffBadges;

                for (int i = 0; i < facesArr.Length; i++)
                {
                    int finalRaw = facesArr[i];
                    int cleanRaw = finalRaw >= 101 ? finalRaw - 100 : finalRaw;
                    int baseNum = (cleanRaw == 31 || cleanRaw == 32 || cleanRaw == 23 || cleanRaw == 26) ? 0 : cleanRaw;
                    origArr[i] = baseNum;

                    if (seqs != null && i < seqs.Length && seqs[i] != null && seqs[i].Length > 0)
                    {
                        for (int s = 0; s < seqs[i].Length - 1; s++)
                        {
                            int stepFace = seqs[i][s];
                            int nextFace = seqs[i][s + 1];
                            var localAb = _catalog?.TryGetAbility(stepFace);

                            if (stepFace >= 16 && stepFace <= 18)
                            {
                                int bonus = stepFace - 15;
                                dribArr[i] += bonus;
                                boostArr[i] = true;
                            }
                            else if (stepFace == 24 && localAb != null)
                            {
                                // Cross also grants a targeted re-roll of 1 die of choice in Phase 1
                                localTriggeredAbilities.Add((side, localAb, i));
                            }

                            if (dieRoots != null && i < dieRoots.Length && dieRoots[i] != null)
                            {
                                string burstName = localAb != null ? localAb.name : (stepFace == 24 ? "Cross" : $"Dribble +{stepFace - 15}");
                                Sprite medal = GetAbilityMedalSprite(localAb);

                                SetBuffBeltDisplay(medal, $"{burstName.ToUpper()}! ROLLING AGAIN...", GetAbilityBadgeOverlayText(localAb));
                                if (dieBadges != null && i < dieBadges.Length && dieBadges[i] != null)
                                {
                                    dieBadges[i].sprite = medal;
                                    dieBadges[i].enabled = true;
                                }

                                AudioManager.Instance.PlayPowerup(2);
                                yield return new WaitForSeconds(0.28f);

                                // Re-roll this die visually on the pitch
                                ClearSideDieVisualForRoll(side, i);
                                AudioManager.Instance.PlayDiceRoll();
                                for (int f = 1; f <= 7; f++)
                                {
                                    dieRoots[i].GetComponent<Image>().sprite = GetUiSprite($"Original/sprites/PlayerBody3/DiceRoll{f:00}");
                                    yield return new WaitForSeconds(0.040f);
                                }
                                dieRoots[i].GetComponent<Image>().sprite = GetUiSprite("Original/sprites/Buff6/DiceBackground");
                                SetRawSideDieFace(side, i, nextFace);
                                yield return new WaitForSeconds(0.20f);
                            }
                        }

                        // Check if final face is Assist (23) or Dive! (26)
                        int lastFace = seqs[i][seqs[i].Length - 1];
                        if (lastFace == 23 || lastFace == 26)
                        {
                            var lastAb = _catalog?.TryGetAbility(lastFace);
                            if (lastAb != null) localTriggeredAbilities.Add((side, lastAb, i));
                        }
                    }

                    // Keep Dribble bonus as part of baseline so Phase 4 Marking / Cleanse / Entourage swaps preserve natural Dribble pips (Intercept strips Dribble explicitly)
                    origArr[i] = baseNum + dribArr[i];
                    facesArr[i] = origArr[i];
                    if (dribArr[i] > 0)
                    {
                        SetSideDieVisual(side, i, facesArr[i], comboArr[i], true);
                    }
                }
            }

            int homeDiceSum = 0;
            for (int i = 0; i < homeFaces.Length; i++) homeDiceSum += homeFaces[i];
            int awayDiceSum = 0;
            for (int i = 0; i < awayFaces.Length; i++) awayDiceSum += awayFaces[i];

            // 5. Reveal base duel totals with counting animation
            HideBuffBelt();
            _matchReport.text = "Counting the dice...";

            if (_homeTotalText != null && _awayTotalText != null)
            {
                Vector2 hTotPos = new Vector2(0.135f, 0.24f);
                Vector2 aTotPos = new Vector2(0.865f, 0.24f);

                _homeTotalText.rectTransform.anchorMin = _homeTotalText.rectTransform.anchorMax = hTotPos;
                _awayTotalText.rectTransform.anchorMin = _awayTotalText.rectTransform.anchorMax = aTotPos;
                _homeTotalText.rectTransform.anchoredPosition = Vector2.zero;
                _awayTotalText.rectTransform.anchoredPosition = Vector2.zero;

                _homeTotalText.color = new Color32(50, 45, 40, 255);
                _awayTotalText.color = new Color32(50, 45, 40, 255);

                _homeTotalText.enabled = true;
                _awayTotalText.enabled = true;

                int targetH = _match.LastHomeTotal;
                int targetA = _match.LastAwayTotal;
                int maxTot = Mathf.Max(targetH, targetA);
                int countSteps = Mathf.Clamp(maxTot, 4, 10);

                for (int step = 1; step <= countSteps; step++)
                {
                    float t = (float)step / countSteps;
                    int curH = Mathf.RoundToInt(Mathf.Lerp(0, targetH, t));
                    int curA = Mathf.RoundToInt(Mathf.Lerp(0, targetA, t));
                    _homeTotalText.text = curH.ToString();
                    _awayTotalText.text = curA.ToString();

                    AudioManager.Instance.PlayTap();
                    float punch = 1f + 0.12f * (1f - (float)step / countSteps);
                    _homeTotalText.rectTransform.localScale = new Vector3(punch, punch, 1f);
                    _awayTotalText.rectTransform.localScale = new Vector3(punch, punch, 1f);
                    yield return new WaitForSeconds(0.055f);
                }

                _homeTotalText.text = targetH.ToString();
                _awayTotalText.text = targetA.ToString();
                _homeTotalText.rectTransform.localScale = Vector3.one;
                _awayTotalText.rectTransform.localScale = Vector3.one;
            }

            yield return new WaitForSeconds(0.50f);

            // 6. Interactive Superpower & Ability activation (interleaved by Execution Phase 0..5 across Home & Away!)
            PlayerData[] currentLineup = _match.HomeTeam ?? ((_catalog != null && _lineupIds != null) ? _catalog.ResolveLineup(_lineupIds) : null);
            PlayerData[] awayLineup = _match.AwayTeam ?? ((_catalog != null && _currentFixture != null && _currentFixture.lineup != null) ? _catalog.ResolveLineup(_currentFixture.lineup) : null);

            var allAbilities = new List<(int side, AbilityData ability, int slot, PlayerData player)>();

            // Queue any local dice-face abilities (Cross 24, Assist 23, Dive! 26)
            foreach (var (lSide, lAb, lSlot) in localTriggeredAbilities)
            {
                PlayerData lPlayer = (lSide == 0)
                    ? (currentLineup != null && lSlot < activeHome.Length ? currentLineup[activeHome[lSlot]] : null)
                    : (awayLineup != null && lSlot < activeAway.Length ? awayLineup[activeAway[lSlot]] : null);
                allAbilities.Add((lSide, lAb, lSlot, lPlayer));
            }

            for (int i = 0; i < activeHome.Length; i++)
            {
                int s = activeHome[i];
                if (currentLineup != null && s < currentLineup.Length && currentLineup[s] != null)
                {
                    int spId = currentLineup[s].superpower;
                    if (spId > 0)
                    {
                        var ab = _catalog?.TryGetAbility(spId);
                        if (ab != null && ((ab.id != 47 && ab.id != 80) || _match.Possession == 0) &&
                            IsAbilityTriggered(ab.trigger, i, comboHomeFaces, homeDiceSum))
                        {
                            allAbilities.Add((0, ab, i, currentLineup[s]));
                        }
                    }
                }
            }

            for (int i = 0; i < activeAway.Length; i++)
            {
                int s = activeAway[i];
                if (awayLineup != null && s < awayLineup.Length && awayLineup[s] != null)
                {
                    int spId = awayLineup[s].superpower;
                    if (spId > 0)
                    {
                        var ab = _catalog?.TryGetAbility(spId);
                        if (ab != null && ((ab.id != 47 && ab.id != 80) || _match.Possession == 1) &&
                            IsAbilityTriggered(ab.trigger, i, comboAwayFaces, awayDiceSum))
                        {
                            allAbilities.Add((1, ab, i, awayLineup[s]));
                        }
                    }
                }
            }

            // Sort abilities across both teams by canonical execution phase (0: Intercept, 1: Reroll, 2: Anchor, 3: Buff/Debuff, 4: Cleanse/Strip/Feint, 5: ThroughPass/Dive)
            allAbilities.Sort((a, b) =>
            {
                int phaseCmp = GetAbilityExecutionPhase(a.ability).CompareTo(GetAbilityExecutionPhase(b.ability));
                if (phaseCmp != 0) return phaseCmp;
                int slotCmp = a.ability.slot.CompareTo(b.ability.slot);
                if (slotCmp != 0) return slotCmp;
                return a.side.CompareTo(b.side);
            });

            void UpdateSideDieVisual(int s, int dieIdx, int val)
            {
                var cFaces = (s == 0) ? comboHomeFaces : comboAwayFaces;
                int rawCombo = (cFaces != null && dieIdx >= 0 && dieIdx < cFaces.Length) ? cFaces[dieIdx] : 0;
                SetSideDieVisual(s, dieIdx, val, rawCombo, true);
            }

            void AdjustSideTotal(int s, int delta)
            {
                if (s == 0)
                {
                    _match.AdjustHomeTotal(delta);
                    if (_homeTotalText != null) _homeTotalText.text = _match.LastHomeTotal.ToString();
                }
                else
                {
                    _match.AdjustAwayTotal(delta);
                    if (_awayTotalText != null) _awayTotalText.text = _match.LastAwayTotal.ToString();
                }
            }

            // Pulse HUD Entourage icons triggered during roll/aura phase (#1,#2,#4,#5,#6,#15,#19,#20,#21)
            if (_match.LastHomeTriggeredEntourages.Count > 0)
            {
                foreach (var eid in _match.LastHomeTriggeredEntourages)
                    StartCoroutine(PulseEntourageIcon(true, eid));
            }
            if (_match.LastAwayTriggeredEntourages.Count > 0)
            {
                foreach (var eid in _match.LastAwayTriggeredEntourages)
                    StartCoroutine(PulseEntourageIcon(false, eid));
            }

            bool homeProduced2x = false;
            bool awayProduced2x = false;
            int pendingThroughPassSide = -1;

            foreach (var (side, activeAbility, triggeringSlot, triggeringPlayer) in allAbilities)
            {
                bool isHome = (side == 0);
                int oppSide = isHome ? 1 : 0;
                bool isHuman = isHome || _isLocalMultiplayer;
                string pPrefix = (!isHome && _isLocalMultiplayer) ? "P2: " : "";

                int[] myFaces = isHome ? homeFaces : awayFaces;
                int[] oppFaces = isHome ? awayFaces : homeFaces;
                int[] myOrigFaces = isHome ? originalHomeFaces : originalAwayFaces;
                int[] oppOrigFaces = isHome ? originalAwayFaces : originalHomeFaces;
                int[] oppDribble = isHome ? awayDribbleBonus : homeDribbleBonus;
                bool[] myBoosted = isHome ? homeBoosted : awayBoosted;
                bool[] oppBoosted = isHome ? awayBoosted : homeBoosted;
                bool[] myReduced = isHome ? homeReduced : awayReduced;
                bool[] oppReduced = isHome ? awayReduced : homeReduced;
                bool[] myImmune = isHome ? homeImmune : awayImmune;
                bool[] oppImmune = isHome ? awayImmune : homeImmune;
                bool[] myIntercepted = isHome ? homeIntercepted : awayIntercepted;
                bool[] oppIntercepted = isHome ? awayIntercepted : homeIntercepted;

                var myDieRoots = isHome ? _homeDieRoots : _awayDieRoots;
                var oppDieRoots = isHome ? _awayDieRoots : _homeDieRoots;
                var myDieBadges = isHome ? _homeDieBuffBadges : _awayDieBuffBadges;
                var oppDieBadges = isHome ? _awayDieBuffBadges : _homeDieBuffBadges;
                var myActiveSlots = isHome ? activeHome : activeAway;
                var myLineup = isHome ? currentLineup : awayLineup;

                int burstDie = triggeringSlot >= 0 && triggeringSlot < myFaces.Length ? triggeringSlot : 0;

                // Skip re-roll abilities if this slot was intercepted by the opponent!
                if (activeAbility.type == 5 && activeAbility.id != 64 && myIntercepted[burstDie])
                {
                    SetBuffBeltDisplay(GetAbilityMedalSprite(activeAbility), $"{activeAbility.name.ToUpper()} INTERCEPTED!", GetAbilityBadgeOverlayText(activeAbility));
                    yield return new WaitForSeconds(0.45f);
                    continue;
                }

                Vector2 burstPos = (myDieRoots != null && burstDie < myDieRoots.Length && myDieRoots[burstDie] != null)
                    ? myDieRoots[burstDie].GetComponent<RectTransform>().anchorMin
                    : new Vector2(isHome ? 0.27f : 0.73f, 0.50f);

                string badgeSpritePath = GetTriggerBadgePath(activeAbility.trigger);
                Sprite abilityMedalSprite = GetAbilityMedalSprite(activeAbility);
                string abilityBadgeOverlay = GetAbilityBadgeOverlayText(activeAbility);

                yield return StartCoroutine(PlayAbilityBurst(burstPos, badgeSpritePath, activeAbility.name));

                // 0. Intercept (ID 43, 76, 77, 78, 79, 81): Remove one/all Re-rolls from opponent die(s)
                if (activeAbility.id == 43 || (activeAbility.id >= 76 && activeAbility.id <= 79) || activeAbility.id == 81)
                {
                    if (oppFaces.Length > 0)
                    {
                        int chosenOpp = 0;
                        if (activeAbility.target == 1)
                        {
                            int bestDef = 0;
                            for (int d = 0; d < oppFaces.Length; d++)
                            {
                                if (oppDribble[d] > 0) { bestDef = d; break; }
                            }
                            if (isHuman)
                            {
                                SetBuffBeltDisplay(abilityMedalSprite, $"{pPrefix}PLEASE SELECT TARGET", abilityBadgeOverlay);
                                int[] eligible = new int[oppFaces.Length];
                                for (int e = 0; e < eligible.Length; e++) eligible[e] = e;
                                yield return StartCoroutine(SelectBuffTarget(oppSide, eligible, bestDef, c => chosenOpp = c));
                            }
                            else chosenOpp = bestDef;
                        }

                        AudioManager.Instance.PlayDebuff();
                        for (int d = 0; d < oppFaces.Length; d++)
                        {
                            if (activeAbility.target == 1 && d != chosenOpp) continue;
                            if (oppImmune[d]) continue;
                            oppIntercepted[d] = true;
                            if (oppDieBadges != null && d < oppDieBadges.Length && oppDieBadges[d] != null)
                            {
                                oppDieBadges[d].sprite = abilityMedalSprite;
                                oppDieBadges[d].enabled = true;
                            }
                            // If opponent die had a Dribble local re-roll bonus, Intercept strips it from both current and baseline!
                            if (oppDribble[d] > 0)
                            {
                                int strip = (activeAbility.id == 76) ? oppDribble[d] : Mathf.Min(oppDribble[d], 3);
                                oppDribble[d] -= strip;
                                oppFaces[d] = Math.Max(0, oppFaces[d] - strip);
                                oppOrigFaces[d] = Math.Max(0, oppOrigFaces[d] - strip);
                                UpdateSideDieVisual(oppSide, d, oppFaces[d]);
                                AdjustSideTotal(oppSide, -strip);
                            }
                        }
                        SetBuffBeltDisplay(abilityMedalSprite, $"{activeAbility.name.ToUpper()} BLOCKED REROLLS!", abilityBadgeOverlay);
                        yield return new WaitForSeconds(0.65f);
                    }
                }
                // 1. Anchor (ID 42, 52 single die; ID 60 whole team): Immunity to ALL abilities
                else if (activeAbility.id == 42 || activeAbility.id == 52 || activeAbility.id == 60)
                {
                    int chosenOwn = 0;
                    if (activeAbility.target == 1)
                    {
                        int bestOwn = 0;
                        int maxV = -1;
                        for (int d = 0; d < myFaces.Length; d++)
                            if (myFaces[d] > maxV) { maxV = myFaces[d]; bestOwn = d; }

                        if (isHuman)
                        {
                            SetBuffBeltDisplay(abilityMedalSprite, $"{pPrefix}PLEASE SELECT TARGET", abilityBadgeOverlay);
                            int[] eligible = new int[myFaces.Length];
                            for (int e = 0; e < eligible.Length; e++) eligible[e] = e;
                            yield return StartCoroutine(SelectBuffTarget(side, eligible, bestOwn, c => chosenOwn = c));
                        }
                        else chosenOwn = bestOwn;
                    }

                    AudioManager.Instance.PlayBuff();
                    for (int d = 0; d < myFaces.Length; d++)
                    {
                        if (activeAbility.target == 1 && d != chosenOwn) continue;
                        myImmune[d] = true;
                        if (myFaces[d] < myOrigFaces[d])
                        {
                            int diff = myOrigFaces[d] - myFaces[d];
                            myFaces[d] = myOrigFaces[d];
                            UpdateSideDieVisual(side, d, myFaces[d]);
                            AdjustSideTotal(side, diff);
                        }
                        if (myDieBadges != null && d < myDieBadges.Length && myDieBadges[d] != null)
                        {
                            myDieBadges[d].sprite = abilityMedalSprite;
                            myDieBadges[d].enabled = true;
                        }
                    }
                    SetBuffBeltDisplay(abilityMedalSprite, $"{activeAbility.name.ToUpper()} IMMUNITY!", abilityBadgeOverlay);
                    yield return new WaitForSeconds(0.65f);
                }
                // 2. Saving Tackle (ID 44): Choose a die and switch its positive and negative bonuses
                else if (activeAbility.id == 44)
                {
                    int bestOwn = 0;
                    int maxReduction = 0;
                    for (int d = 0; d < myFaces.Length; d++)
                    {
                        int red = myOrigFaces[d] - myFaces[d];
                        if (red > maxReduction) { maxReduction = red; bestOwn = d; }
                    }

                    if (maxReduction > 0 || isHuman)
                    {
                        int chosenDie = bestOwn;
                        if (isHuman)
                        {
                            SetBuffBeltDisplay(abilityMedalSprite, $"{pPrefix}PLEASE SELECT TARGET", abilityBadgeOverlay);
                            int[] eligible = new int[myFaces.Length];
                            for (int e = 0; e < eligible.Length; e++) eligible[e] = e;
                            yield return StartCoroutine(SelectBuffTarget(side, eligible, bestOwn, c => chosenDie = c));
                        }

                        AudioManager.Instance.PlayBuff();
                        int oldVal = myFaces[chosenDie];
                        int reduction = Mathf.Max(1, myOrigFaces[chosenDie] - oldVal);
                        int newVal = myOrigFaces[chosenDie] + reduction;
                        myFaces[chosenDie] = newVal;
                        myReduced[chosenDie] = false;
                        myBoosted[chosenDie] = true;
                        UpdateSideDieVisual(side, chosenDie, newVal);
                        if (myDieBadges != null && chosenDie < myDieBadges.Length && myDieBadges[chosenDie] != null)
                        {
                            myDieBadges[chosenDie].sprite = abilityMedalSprite;
                            myDieBadges[chosenDie].enabled = true;
                        }
                        AdjustSideTotal(side, newVal - oldVal);
                    }
                    SetBuffBeltDisplay(abilityMedalSprite, $"{activeAbility.name.ToUpper()} REVERSED!", abilityBadgeOverlay);
                    yield return new WaitForSeconds(0.65f);
                }
                // A. Through Pass (ID 47, 80): Mark extra forward advance once duel completes!
                else if (activeAbility.id == 47 || activeAbility.id == 80)
                {
                    if (_match.Possession == side)
                    {
                        SetBuffBeltDisplay(abilityMedalSprite, "THROUGH PASS! ADVANCING!", abilityBadgeOverlay);
                        yield return StartCoroutine(PlayThroughPassAnimation(isHome));
                        pendingThroughPassSide = side;
                        yield return new WaitForSeconds(0.50f);
                    }
                }
                // A2. Dive! (ID 26, 45): Roll the REFEREE die ([10, 32, 32, 32, 32, 32]) at the end of this play
                else if (activeAbility.id == 26 || activeAbility.id == 45)
                {
                    SetBuffBeltDisplay(abilityMedalSprite, "DIVE! ROLLING REFEREE DIE...", abilityBadgeOverlay);
                    AudioManager.Instance.PlayWhistle();
                    int[] refSides = { 10, 32, 32, 32, 32, 32 };
                    int refRoll = refSides[UnityEngine.Random.Range(0, refSides.Length)];

                    if (myDieRoots != null && burstDie < myDieRoots.Length && myDieRoots[burstDie] != null)
                    {
                        ClearSideDieVisualForRoll(side, burstDie);
                        AudioManager.Instance.PlayDiceRoll();
                        for (int f = 1; f <= 7; f++)
                        {
                            myDieRoots[burstDie].GetComponent<Image>().sprite = GetUiSprite($"Original/sprites/PlayerBody3/DiceRoll{f:00}");
                            yield return new WaitForSeconds(0.045f);
                        }
                        myDieRoots[burstDie].GetComponent<Image>().sprite = GetUiSprite("Original/sprites/Buff6/DiceBackground");
                        SetRawSideDieFace(side, burstDie, refRoll);
                    }

                    if (refRoll == 10)
                    {
                        AudioManager.Instance.PlayBuff();
                        int oldVal = myFaces[burstDie];
                        myFaces[burstDie] = 10;
                        myBoosted[burstDie] = true;
                        UpdateSideDieVisual(side, burstDie, 10);
                        AdjustSideTotal(side, 10 - oldVal);
                        SetBuffBeltDisplay(abilityMedalSprite, "REFEREE FOOLED! +10 FREE KICK!", abilityBadgeOverlay);
                    }
                    else
                    {
                        // Referee Whistle (32): caught diving -> +1 to all non-immune opponents (Ability 32)
                        AudioManager.Instance.PlayDebuff();
                        if (activeAbility.id == 45)
                        {
                            UpdateSideDieVisual(side, burstDie, myFaces[burstDie]);
                        }
                        else
                        {
                            var myComboFaces = isHome ? comboHomeFaces : comboAwayFaces;
                            if (burstDie < myComboFaces.Length) myComboFaces[burstDie] = 32;
                            UpdateSideDieVisual(side, burstDie, myFaces[burstDie]);
                        }
                        for (int d = 0; d < oppFaces.Length; d++)
                        {
                            if (oppImmune[d]) continue;
                            oppFaces[d] += 1;
                            oppBoosted[d] = true;
                            UpdateSideDieVisual(oppSide, d, oppFaces[d]);
                            AdjustSideTotal(oppSide, 1);
                        }
                        SetBuffBeltDisplay(GetUiSprite("Original/sprites/Buff6/BuffMedal32"), "CAUGHT DIVING! +1 TO OPPONENTS!");
                    }
                    yield return new WaitForSeconds(0.70f);
                }
                // B. Defensive Wall (ID 27, 59: +1 to all teammates) or +5 (ID 64)
                else if (activeAbility.id == 27 || activeAbility.id == 59 || activeAbility.id == 64)
                {
                    SetBuffBeltDisplay(abilityMedalSprite, (activeAbility.id == 64) ? "+5 TRIPLE BONUS!" : $"{activeAbility.name.ToUpper()} +1!", abilityBadgeOverlay);
                    AudioManager.Instance.PlayBuff();
                    if (activeAbility.id == 64)
                    {
                        AdjustSideTotal(side, 5);
                    }
                    else
                    {
                        for (int d = 0; d < myFaces.Length; d++)
                        {
                            myFaces[d] += 1;
                            myBoosted[d] = true;
                            UpdateSideDieVisual(side, d, myFaces[d]);
                            if (myDieBadges != null && d < myDieBadges.Length && myDieBadges[d] != null)
                            {
                                myDieBadges[d].sprite = abilityMedalSprite;
                                myDieBadges[d].enabled = true;
                            }
                            AdjustSideTotal(side, 1);
                        }
                    }
                    yield return new WaitForSeconds(0.70f);
                }
                // D. Team-wide Debuffs against Opponent (Target == 2: Sliding Tackle ID 20, 36; Clearance ID 35; Offside Trap ID 38)
                else if (activeAbility.type == 0 && activeAbility.target == 2 && oppFaces.Length > 0)
                {
                    SetBuffBeltDisplay(abilityMedalSprite, $"{activeAbility.name.ToUpper()}!", abilityBadgeOverlay);
                    AudioManager.Instance.PlayDebuff();
                    int teamRed = (activeAbility.id == 20) ? 3 : ((activeAbility.id == 36) ? 2 : 1);
                    for (int d = 0; d < oppFaces.Length; d++)
                    {
                        if (oppImmune[d]) continue;
                        int oldVal = oppFaces[d];
                        int newVal = (activeAbility.id == 38) ? Mathf.FloorToInt(oldVal / 2f) : Math.Max(0, oldVal - teamRed);
                        oppFaces[d] = newVal;
                        oppReduced[d] = true;
                        UpdateSideDieVisual(oppSide, d, newVal);
                        if (oppDieBadges != null && d < oppDieBadges.Length && oppDieBadges[d] != null)
                        {
                            oppDieBadges[d].sprite = abilityMedalSprite;
                            oppDieBadges[d].enabled = true;
                        }
                        AdjustSideTotal(oppSide, newVal - oldVal);
                    }
                    yield return new WaitForSeconds(0.70f);
                }
                // E. Multiplier (Critical Striker x2, Bicycle Kick x3, Critical Striker x4)
                else if (activeAbility.id == 21 || activeAbility.id == 39 || activeAbility.id == 40 || activeAbility.id == 54 || activeAbility.id == 66 || activeAbility.id == 74 || activeAbility.id == 75)
                {
                    int multiplier = (activeAbility.id == 21) ? 4 : ((activeAbility.id == 40) ? 3 : 2);
                    int bestDie = 0;
                    int maxVal = -1;
                    for (int d = 0; d < myFaces.Length; d++)
                        if (myFaces[d] > maxVal) { maxVal = myFaces[d]; bestDie = d; }

                    int chosenDie = bestDie;
                    if (isHuman)
                    {
                        SetBuffBeltDisplay(abilityMedalSprite, $"{pPrefix}PLEASE SELECT TARGET", abilityBadgeOverlay);
                        int[] eligible = new int[myFaces.Length];
                        for (int e = 0; e < eligible.Length; e++) eligible[e] = e;
                        yield return StartCoroutine(SelectBuffTarget(side, eligible, bestDie, chosen => chosenDie = chosen));
                    }

                    if (chosenDie >= 0 && chosenDie < myFaces.Length)
                    {
                        if (myDieBadges != null && chosenDie < myDieBadges.Length && myDieBadges[chosenDie] != null)
                        {
                            myDieBadges[chosenDie].sprite = abilityMedalSprite;
                            myDieBadges[chosenDie].enabled = true;
                        }

                        AudioManager.Instance.PlayPowerup(1);
                        AudioManager.Instance.PlayBuff();

                        if (myDieRoots != null && chosenDie < myDieRoots.Length && myDieRoots[chosenDie] != null)
                        {
                            for (int f = 0; f < 6; f++)
                            {
                                float p = f / 5f;
                                float punch = 1f + Mathf.Sin(p * Mathf.PI) * 0.32f;
                                myDieRoots[chosenDie].transform.localScale = new Vector3(punch, punch, 1f);
                                yield return new WaitForSeconds(0.025f);
                            }
                            myDieRoots[chosenDie].transform.localScale = Vector3.one;
                        }

                        int originalVal = myFaces[chosenDie];
                        int boost = originalVal * (multiplier - 1);
                        myFaces[chosenDie] *= multiplier;
                        myBoosted[chosenDie] = true;
                        if (multiplier == 2)
                        {
                            if (isHome) homeProduced2x = true;
                            else awayProduced2x = true;
                        }
                        UpdateSideDieVisual(side, chosenDie, myFaces[chosenDie]);

                        AdjustSideTotal(side, boost);
                        SetBuffBeltDisplay(abilityMedalSprite, $"{activeAbility.name.ToUpper()} x{multiplier}!", abilityBadgeOverlay);
                        yield return new WaitForSeconds(0.70f);
                    }
                }
                // F. Reroll (BuffType 5: Assist ID 23/61/63, Cross ID 24, Parry ID 51/62, Perfect Cross ID 25)
                else if (activeAbility.type == 5 && myFaces.Length > 0)
                {
                    int rerollCount = (activeAbility.id == 25) ? Mathf.Min(2, myFaces.Length) : 1;
                    for (int r = 0; r < rerollCount; r++)
                    {
                        int bestDie = 0;
                        int minVal = 999;
                        for (int d = 0; d < myFaces.Length; d++)
                            if (myFaces[d] < minVal) { minVal = myFaces[d]; bestDie = d; }

                        int chosenDie = bestDie;
                        if (isHuman)
                        {
                            SetBuffBeltDisplay(abilityMedalSprite,
                                (rerollCount > 1) ? $"{pPrefix}SELECT DIE {r + 1} OF {rerollCount}" : $"{pPrefix}PLEASE SELECT TARGET",
                                abilityBadgeOverlay);
                            int[] eligible = new int[myFaces.Length];
                            for (int e = 0; e < eligible.Length; e++) eligible[e] = e;
                            yield return StartCoroutine(SelectBuffTarget(side, eligible, bestDie, chosen => chosenDie = chosen));
                        }

                        if (chosenDie >= 0 && chosenDie < myFaces.Length)
                        {
                            if (myDieBadges != null && chosenDie < myDieBadges.Length && myDieBadges[chosenDie] != null)
                            {
                                myDieBadges[chosenDie].sprite = abilityMedalSprite;
                                myDieBadges[chosenDie].enabled = true;
                            }

                            ClearSideDieVisualForRoll(side, chosenDie);
                            AudioManager.Instance.PlayDiceRoll();
                            for (int f = 1; f <= 7; f++)
                            {
                                var rollSprite = GetUiSprite($"Original/sprites/PlayerBody3/DiceRoll{f:00}");
                                if (myDieRoots != null && myDieRoots[chosenDie] != null)
                                    myDieRoots[chosenDie].GetComponent<Image>().sprite = rollSprite;
                                yield return new WaitForSeconds(0.045f);
                            }
                            if (myDieRoots != null && myDieRoots[chosenDie] != null)
                                myDieRoots[chosenDie].GetComponent<Image>().sprite = GetUiSprite("Original/sprites/Buff6/DiceBackground");

                            PlayerData targetPlayer = (myLineup != null && chosenDie < myActiveSlots.Length && myActiveSlots[chosenDie] < myLineup.Length)
                                ? myLineup[myActiveSlots[chosenDie]] : triggeringPlayer;
                            int oldVal = myFaces[chosenDie];
                            var (newVal, newRawFace) = RollSinglePlayerDie(targetPlayer);
                            var myComboArr = isHome ? comboHomeFaces : comboAwayFaces;
                            if (chosenDie < myComboArr.Length) myComboArr[chosenDie] = newRawFace;
                            myFaces[chosenDie] = newVal;
                            myOrigFaces[chosenDie] = newVal;
                            myBoosted[chosenDie] = false;
                            myReduced[chosenDie] = false;
                            UpdateSideDieVisual(side, chosenDie, newVal);

                            AdjustSideTotal(side, newVal - oldVal);
                            string rolledDisplay = newRawFace == 31 ? "WILD" : (newRawFace == 32 ? "WHISTLE" : newVal.ToString());
                            SetBuffBeltDisplay(abilityMedalSprite, $"{activeAbility.name.ToUpper()} REROLLED TO {rolledDisplay}!", abilityBadgeOverlay);
                            yield return new WaitForSeconds(0.60f);
                        }
                    }
                }
                // G. Single-die Tackle / Debuff (BuffType 0, Target 1: Tackle -1, Hard Tackle -2/-3, Charge /2)
                else if (activeAbility.type == 0 && activeAbility.target == 1 && oppFaces.Length > 0 && activeAbility.id != 41)
                {
                    int bestDie = 0;
                    int maxVal = -1;
                    for (int d = 0; d < oppFaces.Length; d++)
                        if (!oppImmune[d] && oppFaces[d] > maxVal) { maxVal = oppFaces[d]; bestDie = d; }

                    int chosenDie = bestDie;
                    if (isHuman)
                    {
                        SetBuffBeltDisplay(abilityMedalSprite, $"{pPrefix}PLEASE SELECT TARGET", abilityBadgeOverlay);
                        int[] oppEligible = new int[oppFaces.Length];
                        for (int e = 0; e < oppEligible.Length; e++) oppEligible[e] = e;
                        yield return StartCoroutine(SelectBuffTarget(oppSide, oppEligible, bestDie, chosen => chosenDie = chosen));
                    }

                    if (chosenDie >= 0 && chosenDie < oppFaces.Length && !oppImmune[chosenDie])
                    {
                        if (oppDieBadges != null && chosenDie < oppDieBadges.Length && oppDieBadges[chosenDie] != null)
                        {
                            oppDieBadges[chosenDie].sprite = abilityMedalSprite;
                            oppDieBadges[chosenDie].enabled = true;
                        }

                        AudioManager.Instance.PlayTap();
                        int oldVal = oppFaces[chosenDie];
                        int reduction = 1;
                        if (activeAbility.id == 19) reduction = 3;
                        else if (activeAbility.id == 34 || activeAbility.id == 69 || activeAbility.id == 70 || activeAbility.id == 71) reduction = 2;
                        else if (activeAbility.id == 37 || activeAbility.id == 72 || activeAbility.id == 73) reduction = Mathf.CeilToInt(oldVal / 2f);

                        int newVal = Math.Max(0, oldVal - reduction);
                        oppFaces[chosenDie] = newVal;
                        oppReduced[chosenDie] = true;
                        UpdateSideDieVisual(oppSide, chosenDie, newVal);

                        AdjustSideTotal(oppSide, newVal - oldVal);
                        SetBuffBeltDisplay(abilityMedalSprite, $"{activeAbility.name.ToUpper()} -{reduction}!", abilityBadgeOverlay);
                        yield return new WaitForSeconds(0.70f);
                    }
                }
                // H. Feint (BuffType 3: ID 46, 49, 50, 55): Steal opposite enemy's +ve and give him your -ve
                else if (activeAbility.type == 3 && oppFaces.Length > 0 && myFaces.Length > 0)
                {
                    int chosenMyDie = 0;
                    if (activeAbility.id != 49) // Single die target
                    {
                        int bestMyDie = 0;
                        int bestGain = -999;
                        for (int d = 0; d < myFaces.Length; d++)
                        {
                            int oppD = Mathf.Min(d, oppFaces.Length - 1);
                            int myNeg = Mathf.Max(0, myOrigFaces[d] - myFaces[d]);
                            int oppPos = oppImmune[oppD] ? 0 : Mathf.Max(0, oppFaces[oppD] - oppOrigFaces[oppD]);
                            int gain = (myNeg + oppPos > 0) ? (myNeg + oppPos) : (oppFaces[oppD] - myFaces[d]);
                            if (gain > bestGain) { bestGain = gain; bestMyDie = d; }
                        }

                        chosenMyDie = bestMyDie;
                        if (isHuman)
                        {
                            SetBuffBeltDisplay(abilityMedalSprite, $"{pPrefix}PLEASE SELECT TARGET", abilityBadgeOverlay);
                            int[] eligible = new int[myFaces.Length];
                            for (int e = 0; e < eligible.Length; e++) eligible[e] = e;
                            yield return StartCoroutine(SelectBuffTarget(side, eligible, bestMyDie, chosen => chosenMyDie = chosen));
                        }
                    }

                    AudioManager.Instance.PlayTransform();
                    for (int d = 0; d < myFaces.Length; d++)
                    {
                        if (activeAbility.id != 49 && d != chosenMyDie) continue;
                        int oppD = Mathf.Min(d, oppFaces.Length - 1);
                        if (oppImmune[oppD]) continue;

                        int myOld = myFaces[d];
                        int oppOld = oppFaces[oppD];
                        int myNeg = Mathf.Max(0, myOrigFaces[d] - myOld);
                        int oppPos = Mathf.Max(0, oppOld - oppOrigFaces[oppD]);

                        int myNew = myOld;
                        int oppNew = oppOld;

                        if (myNeg > 0 || oppPos > 0)
                        {
                            // Authentic Feint formula: steal opposite enemy's +ve bonus and give him your -ve reduction
                            myNew = myOld + myNeg + oppPos;
                            oppNew = Math.Max(0, oppOld - oppPos - myNeg);
                            myReduced[d] = false;
                            if (oppPos > 0) { myBoosted[d] = true; oppBoosted[oppD] = false; }
                            if (myNeg > 0) oppReduced[oppD] = true;
                        }
                        else if (oppOld > myOld)
                        {
                            myNew = oppOld;
                            oppNew = myOld;
                        }

                        if (myNew != myOld || oppNew != oppOld)
                        {
                            myFaces[d] = myNew;
                            oppFaces[oppD] = oppNew;
                            UpdateSideDieVisual(side, d, myNew);
                            UpdateSideDieVisual(oppSide, oppD, oppNew);
                            if (myDieBadges != null && d < myDieBadges.Length && myDieBadges[d] != null)
                            {
                                myDieBadges[d].sprite = abilityMedalSprite;
                                myDieBadges[d].enabled = true;
                            }
                            AdjustSideTotal(side, myNew - myOld);
                            AdjustSideTotal(oppSide, oppNew - oppOld);
                        }
                    }

                    SetBuffBeltDisplay(abilityMedalSprite, $"{activeAbility.name.ToUpper()}!", abilityBadgeOverlay);
                    yield return new WaitForSeconds(0.70f);
                }
                // I. Clear Reductions (Step-Over ID 28, 29, 30; Shielding ID 48)
                else if (activeAbility.id == 28 || activeAbility.id == 29 || activeAbility.id == 30 || activeAbility.id == 48)
                {
                    int chosenShieldDie = 0;
                    if (activeAbility.id == 48)
                    {
                        int bestDef = 0;
                        int maxLoss = -1;
                        for (int d = 0; d < myFaces.Length; d++)
                        {
                            int loss = myOrigFaces[d] - myFaces[d];
                            if (loss > maxLoss) { maxLoss = loss; bestDef = d; }
                        }
                        chosenShieldDie = bestDef;
                        if (isHuman)
                        {
                            SetBuffBeltDisplay(abilityMedalSprite, $"{pPrefix}PLEASE SELECT TARGET", abilityBadgeOverlay);
                            int[] eligible = new int[myFaces.Length];
                            for (int e = 0; e < eligible.Length; e++) eligible[e] = e;
                            yield return StartCoroutine(SelectBuffTarget(side, eligible, bestDef, chosen => chosenShieldDie = chosen));
                        }
                    }

                    AudioManager.Instance.PlayBuff();
                    int totalRestored = 0;
                    for (int d = 0; d < myFaces.Length; d++)
                    {
                        if (activeAbility.id == 48 && d != chosenShieldDie) continue;
                        if (myFaces[d] < myOrigFaces[d])
                        {
                            int diff = myOrigFaces[d] - myFaces[d];
                            myFaces[d] = myOrigFaces[d];
                            myReduced[d] = false;
                            totalRestored += diff;
                            UpdateSideDieVisual(side, d, myFaces[d]);
                            if (myDieBadges != null && d < myDieBadges.Length && myDieBadges[d] != null)
                            {
                                myDieBadges[d].sprite = abilityMedalSprite;
                                myDieBadges[d].enabled = true;
                            }
                        }
                    }
                    if (totalRestored > 0) AdjustSideTotal(side, totalRestored);
                    SetBuffBeltDisplay(abilityMedalSprite, $"{activeAbility.name.ToUpper()} CLEARED REDUCTIONS!", abilityBadgeOverlay);
                    yield return new WaitForSeconds(0.70f);
                }
                // J. Strip Bonuses / Marking (Man Marking ID 41, 56, 58; Zonal Marking ID 22, 57)
                else if ((activeAbility.type == 4 || activeAbility.id == 41) && oppFaces.Length > 0)
                {
                    int chosenMarkDie = 0;
                    if (activeAbility.target == 1)
                    {
                        int bestDef = 0;
                        int maxGain = -1;
                        for (int d = 0; d < oppFaces.Length; d++)
                        {
                            int gain = oppFaces[d] - oppOrigFaces[d];
                            if (!oppImmune[d] && gain > maxGain) { maxGain = gain; bestDef = d; }
                        }
                        chosenMarkDie = bestDef;
                        if (isHuman)
                        {
                            SetBuffBeltDisplay(abilityMedalSprite, $"{pPrefix}PLEASE SELECT TARGET", abilityBadgeOverlay);
                            int[] oppEligible = new int[oppFaces.Length];
                            for (int e = 0; e < oppEligible.Length; e++) oppEligible[e] = e;
                            yield return StartCoroutine(SelectBuffTarget(oppSide, oppEligible, bestDef, chosen => chosenMarkDie = chosen));
                        }
                    }

                    AudioManager.Instance.PlayDebuff();
                    int totalStripped = 0;
                    for (int d = 0; d < oppFaces.Length; d++)
                    {
                        if (activeAbility.target == 1 && d != chosenMarkDie) continue;
                        if (oppImmune[d]) continue;
                        if (oppFaces[d] > oppOrigFaces[d] || oppBoosted[d])
                        {
                            int diff = Mathf.Max(0, oppFaces[d] - oppOrigFaces[d]);
                            oppFaces[d] = oppOrigFaces[d];
                            oppBoosted[d] = false;
                            totalStripped += diff;
                            UpdateSideDieVisual(oppSide, d, oppFaces[d]);
                            if (oppDieBadges != null && d < oppDieBadges.Length && oppDieBadges[d] != null)
                            {
                                oppDieBadges[d].sprite = abilityMedalSprite;
                                oppDieBadges[d].enabled = true;
                            }
                        }
                    }
                    if (totalStripped > 0) AdjustSideTotal(oppSide, -totalStripped);
                    SetBuffBeltDisplay(abilityMedalSprite, $"{activeAbility.name.ToUpper()} STRIPPED BONUSES!", abilityBadgeOverlay);
                    yield return new WaitForSeconds(0.70f);
                }
            }

            if (allAbilities.Count == 0)
            {
                yield return new WaitForSeconds(0.35f);
            }

            // Conditional & Post-Powerup Entourages (#3, #7, #8, #9, #10, #17, #18, #20)
            yield return StartCoroutine(ResolvePostRollEntouragesRoutine(
                homeFaces, awayFaces, originalHomeFaces, originalAwayFaces,
                activeHome, activeAway, homeImmune, awayImmune, homeProduced2x, awayProduced2x));

            // Captain Tie-Breaker Notification
            if (_match.LastHomeTotal == _match.LastAwayTotal && (_match.HomeWinOnTie || _match.AwayWinOnTie))
            {
                SetBuffBeltDisplay(GetUiSprite("Original/sprites/Buff6/BuffMedal82"), "CAPTAIN BREAKS THE TIE!");
                AudioManager.Instance.PlayPowerup(1);
                yield return new WaitForSeconds(0.70f);
            }

            // Track roll achievements before completing encounter
            if (!_isLocalMultiplayer)
            {
                int finalHomeRoll = _match.LastHomeTotal;
                int finalAwayRoll = _match.LastAwayTotal;
                if (finalHomeRoll > _matchMaxHomeRoll) _matchMaxHomeRoll = finalHomeRoll;
                if (finalHomeRoll == 0) _matchHadZeroHomeRoll = true;
                if (finalAwayRoll == 0) _matchHadZeroAwayRoll = true;
            }

            // 7. Complete encounter evaluation using the BUFFED totals!
            var oldHomeScore = _match.HomeScore;
            var oldAwayScore = _match.AwayScore;
            var oldPossession = _match.Possession;
            int secondsBeforeResolution = _match.SecondsRemaining;

            var report = _match.CompleteEncounter();
            report = CompactMatchReport(report, _match.ClockText());

            if (!_isLocalMultiplayer && _seasonIndex > 0 && secondsBeforeResolution < 60)
            {
                if (_match.HomeScore > oldHomeScore)
                {
                    _matchLastMinGoal = true;
                    if (_match.HomeScore == _match.AwayScore) _matchLastMinEqualizer = true;
                    if (oldHomeScore == oldAwayScore) _matchLastMinTieBreaker = true;
                }
                else if (_match.AwayScore > oldAwayScore)
                {
                    _matchLastMinConcede = true;
                }
                else if (oldLocation == 2 && (report.Contains("SAVED") || report.Contains("MISS") || report.Contains("DENIED")))
                {
                    _matchLastMinMiss = true;
                }
                else if (oldLocation == -2 && (report.Contains("SAVED") || report.Contains("SAVE")))
                {
                    _matchLastMinSave = true;
                }
            }

            _matchScore.text = $"{_match.HomeScore} - {_match.AwayScore}";
            if (_matchClock != null) _matchClock.text = _match.ClockText();
            _matchReport.text = report;
            if (_buffBeltBadgeText != null) _buffBeltBadgeText.text = string.Empty;
            if (_buffBeltText != null) _buffBeltText.text = report;

            // Pause on revealed outcome
            yield return new WaitForSeconds(0.75f);

            // 7. Smoke poof on dice before hiding them
            if (_homeDieRoots != null && _awayDieRoots != null)
            {
                for (var i = 0; i < 3; i++)
                {
                    if (_homeDieRoots[i] != null && _homeDieRoots[i].activeSelf)
                        StartCoroutine(PlayPoofAtPosition(_homeDieRoots[i].GetComponent<RectTransform>().anchorMin));
                    if (_awayDieRoots[i] != null && _awayDieRoots[i].activeSelf)
                        StartCoroutine(PlayPoofAtPosition(_awayDieRoots[i].GetComponent<RectTransform>().anchorMin));
                }
            }
            yield return new WaitForSeconds(0.08f);

            // Hide dice, totals, buff belt, badges, and glows
            if (_homeDieRoots != null && _awayDieRoots != null)
            {
                for (var i = 0; i < 3; i++)
                {
                    if (_homeDieRoots[i] != null) _homeDieRoots[i].SetActive(false);
                    if (_awayDieRoots[i] != null) _awayDieRoots[i].SetActive(false);
                    if (_homeDieValueLabels != null && _homeDieValueLabels[i] != null) _homeDieValueLabels[i].enabled = false;
                    if (_awayDieValueLabels != null && _awayDieValueLabels[i] != null) _awayDieValueLabels[i].enabled = false;
                    if (_homeDieBuffBadges != null && _homeDieBuffBadges[i] != null) _homeDieBuffBadges[i].enabled = false;
                    if (_awayDieBuffBadges != null && _awayDieBuffBadges[i] != null) _awayDieBuffBadges[i].enabled = false;
                    if (_homeDieGlows != null && _homeDieGlows[i] != null) _homeDieGlows[i].enabled = false;
                    if (_awayDieGlows != null && _awayDieGlows[i] != null) _awayDieGlows[i].enabled = false;
                }
            }
            if (_homeTotalText != null) _homeTotalText.enabled = false;
            if (_awayTotalText != null) _awayTotalText.enabled = false;
            HideBuffBelt();

            // Restore duel players on the pitch before shot/resolution with authentic poofs
            AudioManager.Instance.PlayTransform();
            UpdatePlayerPositions(oldLocation);
            for (var i = 0; i < activeHome.Length; i++)
            {
                var p = _homePlayers[activeHome[i]];
                if (p != null) StartCoroutine(PlayPoofAtPosition(new Vector2(p.rectTransform.anchorMin.x, p.rectTransform.anchorMin.y + 0.12f)));
            }
            for (var i = 0; i < activeAway.Length; i++)
            {
                var p = _awayPlayers[activeAway[i]];
                if (p != null) StartCoroutine(PlayPoofAtPosition(new Vector2(p.rectTransform.anchorMin.x, p.rectTransform.anchorMin.y + 0.12f)));
            }
            yield return new WaitForSeconds(0.12f);

            var scored = _match.HomeScore > oldHomeScore || _match.AwayScore > oldAwayScore || report.Contains("GOAL");
            if (scored)
            {
                bool homeScored = _match.HomeScore > oldHomeScore;
                Image striker = homeScored ? _homePlayers[5] : _awayPlayers[5];
                Image goalie = homeScored ? _awayPlayers[0] : _homePlayers[0];

                // Striker anticipation wind-up & shot lunge + Kick01..07 sprite frames:
                float kickDir = homeScored ? 32f : -32f;
                var strikerRig = striker != null ? striker.GetComponent<LayeredPlayerRig>() : null;
                if (striker != null)
                {
                    strikerRig?.SetKickFrame(1);
                    striker.rectTransform.anchoredPosition = new Vector2(-kickDir * 0.35f, 0f);
                    yield return new WaitForSeconds(0.05f);
                    strikerRig?.SetKickFrame(3);
                    yield return new WaitForSeconds(0.04f);
                    strikerRig?.SetKickFrame(5);
                    striker.rectTransform.anchoredPosition = new Vector2(kickDir, 0f);
                    AudioManager.Instance.PlayKick();
                }

                // Animate ball shooting into the goal net with arc and rapid spin (0.42s flight)
                var ballStart = _ballMarker != null ? _ballMarker.rectTransform.anchorMin : (homeScored ? new Vector2(0.335f, 0.40f) : new Vector2(0.665f, 0.40f));
                var ballTarget = homeScored ? new Vector2(0.80f, 0.50f) : new Vector2(0.20f, 0.50f);
                if (strikerRig != null) strikerRig.SetKickFrame(7);
                yield return StartCoroutine(SmoothPassBall(ballStart, ballTarget, 0.42f, true));
                strikerRig?.SetIdlePose();

                // Goalkeeper reacts defeated / beaten dive
                if (goalie != null)
                {
                    float fallDir = homeScored ? 20f : -20f;
                    goalie.rectTransform.anchoredPosition = new Vector2(fallDir, -14f);
                    goalie.rectTransform.localEulerAngles = new Vector3(0f, 0f, homeScored ? -16f : 16f);
                }
                if (striker != null) striker.rectTransform.anchoredPosition = Vector2.zero;

                yield return new WaitForSeconds(0.22f);

                if (goalie != null)
                {
                    goalie.rectTransform.anchoredPosition = Vector2.zero;
                    goalie.rectTransform.localEulerAngles = Vector3.zero;
                }

                // Authentic GOAL celebration animation
                yield return StartCoroutine(PlayGoalAnimation(homeScored));

                // Shift field smoothly back to midfield (0)
                yield return StartCoroutine(SmoothShiftFieldAndBall(oldLocation, 0, _match.Possession));
                UpdatePlayerPositions();
                yield return StartCoroutine(PlayKickOffAnimation());
            }
            else if (report.Contains("SAVED"))
            {
                bool homeShooting = oldLocation == 2;
                Image striker = homeShooting ? _homePlayers[5] : _awayPlayers[5];
                Image goalie = homeShooting ? _awayPlayers[0] : _homePlayers[0];

                float kickDir = homeShooting ? 25f : -25f;
                var strikerRig = striker != null ? striker.GetComponent<LayeredPlayerRig>() : null;
                if (striker != null)
                {
                    strikerRig?.SetKickFrame(2);
                    striker.rectTransform.anchoredPosition = new Vector2(-kickDir * 0.3f, 0f);
                    yield return new WaitForSeconds(0.04f);
                    strikerRig?.SetKickFrame(4);
                    yield return new WaitForSeconds(0.04f);
                    strikerRig?.SetKickFrame(6);
                    striker.rectTransform.anchoredPosition = new Vector2(kickDir, 0f);
                    AudioManager.Instance.PlayKick();
                }

                // Ball shoots towards goalkeeper
                var ballStart = _ballMarker != null ? _ballMarker.rectTransform.anchorMin : (homeShooting ? new Vector2(0.335f, 0.40f) : new Vector2(0.665f, 0.40f));
                var ballTarget = homeShooting ? new Vector2(0.65f, 0.45f) : new Vector2(0.35f, 0.45f);

                // Goalkeeper leaps to parry the shot
                if (goalie != null)
                {
                    float diveDir = homeShooting ? -18f : 18f;
                    goalie.rectTransform.anchoredPosition = new Vector2(diveDir, 14f);
                    goalie.rectTransform.localEulerAngles = new Vector3(0f, 0f, homeShooting ? 12f : -12f);
                }

                if (strikerRig != null) strikerRig.SetKickFrame(7);
                yield return StartCoroutine(SmoothPassBall(ballStart, ballTarget, 0.38f, false));
                strikerRig?.SetIdlePose();

                AudioManager.Instance.PlayTap();
                if (striker != null) striker.rectTransform.anchoredPosition = Vector2.zero;

                yield return StartCoroutine(PlaySaveAnimation());

                if (goalie != null)
                {
                    goalie.rectTransform.anchoredPosition = Vector2.zero;
                    goalie.rectTransform.localEulerAngles = Vector3.zero;
                }

                // Pitch shifts and players walk smoothly to cleared half
                if (oldLocation != _match.Location)
                {
                    yield return StartCoroutine(AnimateZoneTransition(oldLocation, _match.Location, oldPossession, _match.Possession));
                }
            }
            else
            {
                // Outfield duel resolution
                if (report.Contains("TIE") || report.Contains("Tie") || report.Contains("Win on Tie"))
                {
                    yield return StartCoroutine(PlayTieAnimation());
                }
                else if (report.Contains("Through Pass"))
                {
                    yield return StartCoroutine(PlayThroughPassAnimation(_match.Possession == 0));
                }
                else if (oldPossession != _match.Possession)
                {
                    yield return StartCoroutine(PlayPossessionChangeAnimation(_match.Possession == 0));
                }
                else
                {
                    AudioManager.Instance.PlayTap();
                }

                if (pendingThroughPassSide >= 0 && !_match.IsFinished && _match.Possession == pendingThroughPassSide && Math.Abs(_match.Location) < 2)
                {
                    _match.AdvanceLocation(pendingThroughPassSide);
                    AudioManager.Instance.PlayWhistle();
                }

                // Smoothly shift field and walk players to new location
                if (oldLocation != _match.Location)
                {
                    yield return StartCoroutine(AnimateZoneTransition(oldLocation, _match.Location, oldPossession, _match.Possession));
                }
                else
                {
                    var fromPos = _ballMarker != null ? _ballMarker.rectTransform.anchorMin : GetBallPosition(oldLocation, oldPossession);
                    var toPos = GetBallPosition(_match.Location, _match.Possession, _match.BallHolderSlotIndex);
                    yield return StartCoroutine(SmoothPassBall(fromPos, toPos, 0.32f, false));
                }
            }

            if (_match.IsFinished)
            {
                AudioManager.Instance.PlayTripleWhistle();
                if (!_currentResultRecorded)
                {
                    RecordFixtureResult();
                    _currentResultRecorded = true;
                }
                ShowGameOverModal();
            }
            else
            {
                _matchReport.text = GetEncounterPreviewText();
                if (_shakePrompt != null) _shakePrompt.gameObject.SetActive(true);
                _encounterButton.interactable = true;
                _substitutionButton.interactable = _ownedPlayerIds.Count > 7;
                if (_backButton != null) _backButton.interactable = true;
            }

            _inEncounterAnimation = false;
        }

        private IEnumerator PlayKickOffAnimation()
        {
            if (_overlayContainer == null) yield break;
            if (_encounterButton != null) _encounterButton.interactable = false;
            if (_substitutionButton != null) _substitutionButton.interactable = false;

            var banner = AddResourceImage("KickoffBanner",
                "Original/sprites/Prompt2/Kickoff", _overlayContainer,
                new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f));
            banner.rectTransform.sizeDelta = new Vector2(460f, 300f);
            banner.preserveAspect = true;
            AudioManager.Instance.PlayWhistle();

            var elapsed = 0f;
            while (elapsed < 0.18f)
            {
                elapsed += Time.deltaTime;
                banner.rectTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.08f, elapsed / 0.18f);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < 0.08f)
            {
                elapsed += Time.deltaTime;
                banner.rectTransform.localScale = Vector3.Lerp(Vector3.one * 1.08f, Vector3.one, elapsed / 0.08f);
                yield return null;
            }
            yield return new WaitForSeconds(0.25f);

            elapsed = 0f;
            var col = banner.color;
            while (elapsed < 0.24f)
            {
                elapsed += Time.deltaTime;
                banner.color = new Color(col.r, col.g, col.b, 1f - elapsed / 0.24f);
                yield return null;
            }
            if (banner != null) Destroy(banner.gameObject);

            if (_encounterButton != null) _encounterButton.interactable = !_match.IsFinished;
            if (_substitutionButton != null) _substitutionButton.interactable = !_match.IsFinished && _ownedPlayerIds.Count > 7;
        }

        private IEnumerator PlayAbilityBurst(Vector2 anchorPos, string triggerBadgePath, string abilityName)
        {
            Transform parent = _matchPlayRoot != null ? _matchPlayRoot : _field;
            if (parent == null) yield break;

            AudioManager.Instance.PlayPowerup(1);
            AudioManager.Instance.PlayBuff();

            var burstContainer = new GameObject("AbilityBurstContainer", typeof(RectTransform));
            burstContainer.transform.SetParent(parent, false);
            var containerRect = burstContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = containerRect.anchorMax = anchorPos;
            containerRect.anchoredPosition = new Vector2(0f, 30f);
            burstContainer.transform.SetAsLastSibling();

            try
            {
                // 1. Ray burst background (TriggerBurst01..09)
                var rayGo = new GameObject("RayBurst", typeof(RectTransform), typeof(Image));
                rayGo.transform.SetParent(burstContainer.transform, false);
                var rayRect = rayGo.GetComponent<RectTransform>();
                rayRect.sizeDelta = new Vector2(220f, 220f);
                rayRect.anchoredPosition = Vector2.zero;
                var rayImg = rayGo.GetComponent<Image>();
                rayImg.preserveAspect = true;
                rayImg.raycastTarget = false;

                // 2. Trigger badge (e.g. Trigger21 "3*" or Trigger2 "PAIR!")
                var badgeGo = new GameObject("TriggerBadge", typeof(RectTransform), typeof(Image));
                badgeGo.transform.SetParent(burstContainer.transform, false);
                var badgeRect = badgeGo.GetComponent<RectTransform>();
                badgeRect.sizeDelta = new Vector2(160f, 130f);
                badgeRect.anchoredPosition = new Vector2(0f, 10f);
                var badgeImg = badgeGo.GetComponent<Image>();
                var badgeSprite = TryGetUiSprite(triggerBadgePath);
                if (badgeSprite != null) badgeImg.sprite = badgeSprite;
                badgeImg.preserveAspect = true;
                badgeImg.raycastTarget = false;

                // 3. Ribbon banner (e.g. AbilityName-Critical Striker)
                Image ribbonImg = null;
                if (!string.IsNullOrEmpty(abilityName))
                {
                    var ribbonSprite = TryGetUiSprite($"Original/sprites/BuffTrigger/AbilityName-{abilityName}")
                                    ?? TryGetUiSprite($"Original/sprites/BuffTrigger1/AbilityName-{abilityName}");
                    if (ribbonSprite != null)
                    {
                        var ribbonGo = new GameObject("AbilityRibbon", typeof(RectTransform), typeof(Image));
                        ribbonGo.transform.SetParent(burstContainer.transform, false);
                        var ribbonRect = ribbonGo.GetComponent<RectTransform>();
                        ribbonRect.sizeDelta = new Vector2(240f, 48f);
                        ribbonRect.anchoredPosition = new Vector2(0f, -45f);
                        ribbonImg = ribbonGo.GetComponent<Image>();
                        ribbonImg.sprite = ribbonSprite;
                        ribbonImg.preserveAspect = true;
                        ribbonImg.raycastTarget = false;
                    }
                }

                // Animate ray burst cycling + badge punch (9 frames * 0.050s = 0.45s)
                float elapsed = 0f;
                const float duration = 0.45f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);

                    // Ray burst animation frames (TriggerBurst01..09)
                    int burstFrame = Mathf.Clamp((int)(t * 9f) + 1, 1, 9);
                    var bSprite = TryGetUiSprite($"Original/sprites/BuffTrigger/TriggerBurst{burstFrame:00}");
                    if (bSprite != null) rayImg.sprite = bSprite;

                    // Badge scale punch
                    float badgeScale = 1f + Mathf.Sin(t * Mathf.PI) * 0.28f;
                    badgeRect.localScale = new Vector3(badgeScale, badgeScale, 1f);

                    if (ribbonImg != null)
                    {
                        float ribbonScale = Mathf.Clamp01(t * 3f);
                        ribbonImg.rectTransform.localScale = new Vector3(ribbonScale, ribbonScale, 1f);
                    }

                    yield return null;
                }
            }
            finally
            {
                if (burstContainer != null)
                    Destroy(burstContainer);
            }
        }

        private IEnumerator ResolvePostRollEntouragesRoutine(
            int[] homeFaces,
            int[] awayFaces,
            int[] origHomeFaces,
            int[] origAwayFaces,
            int[] activeHomeSlots,
            int[] activeAwaySlots,
            bool[] homeImmune,
            bool[] awayImmune,
            bool homeProduced2x,
            bool awayProduced2x)
        {
            if (_match == null) yield break;

            for (int side = 0; side < 2; side++)
            {
                bool isHome = side == 0;
                int oppSide = isHome ? 1 : 0;
                int[] entourages = isHome ? _match.HomeEntourages : _match.AwayEntourages;
                if (entourages == null || entourages.Length == 0) continue;

                int[] myFaces = isHome ? homeFaces : awayFaces;
                int[] oppFaces = isHome ? awayFaces : homeFaces;
                int[] myOrig = isHome ? origHomeFaces : origAwayFaces;
                int[] oppOrig = isHome ? origAwayFaces : origHomeFaces;
                int[] mySlots = isHome ? activeHomeSlots : activeAwaySlots;
                int[] oppSlots = isHome ? activeAwaySlots : activeHomeSlots;
                bool[] oppImmune = isHome ? awayImmune : homeImmune;
                bool produced2x = isHome ? homeProduced2x : awayProduced2x;
                bool isHuman = isHome || _isLocalMultiplayer;

                void ApplyDieChange(int targetSide, int dieIdx, int newVal, bool allowSpecialIcon = true)
                {
                    var arr = targetSide == 0 ? homeFaces : awayFaces;
                    if (dieIdx < 0 || dieIdx >= arr.Length) return;
                    int diff = newVal - arr[dieIdx];
                    arr[dieIdx] = newVal;
                    SetSideDieVisual(targetSide, dieIdx, newVal, 0, allowSpecialIcon);
                    if (targetSide == 0)
                    {
                        _match.AdjustHomeTotal(diff);
                        if (_homeTotalText != null) _homeTotalText.text = _match.LastHomeTotal.ToString();
                    }
                    else
                    {
                        _match.AdjustAwayTotal(diff);
                        if (_awayTotalText != null) _awayTotalText.text = _match.LastAwayTotal.ToString();
                    }
                }

                void ShowEntourageBanner(int eid, string message)
                {
                    StartCoroutine(PulseEntourageIcon(isHome, eid));
                    AudioManager.Instance.PlayPowerup(1);
                    SetBuffBeltDisplay(GetUiSprite($"Original/images/EntourageAbility{eid:00}"), message);
                }

                foreach (int eid in entourages)
                {
                    int myTot = isHome ? _match.LastHomeTotal : _match.LastAwayTotal;
                    int oppTot = isHome ? _match.LastAwayTotal : _match.LastHomeTotal;

                    if (eid == 3 && myTot < 5 && myFaces.Length > 0) // Miguel Panini: Re-roll any die when total < 5
                    {
                        int chosen = 0;
                        int minV = int.MaxValue;
                        for (int d = 0; d < myFaces.Length; d++)
                            if (myFaces[d] < minV) { minV = myFaces[d]; chosen = d; }
                        if (isHuman && myFaces.Length > 1)
                        {
                            SetBuffBeltDisplay(GetUiSprite("Original/images/EntourageAbility03"), "MIGUEL PANINI: SELECT TARGET");
                            int[] elig = new int[myFaces.Length];
                            for (int e = 0; e < elig.Length; e++) elig[e] = e;
                            yield return StartCoroutine(SelectBuffTarget(side, elig, chosen, c => chosen = c));
                        }
                        int slotIdx = chosen < mySlots.Length ? mySlots[chosen] : 0;
                        int rolled = _match.RollSingleDieForSlot(isHome, slotIdx);
                        ApplyDieChange(side, chosen, rolled);
                        ShowEntourageBanner(3, $"MIGUEL PANINI RE-ROLLS -> {rolled}!");
                        yield return new WaitForSeconds(0.60f);
                    }
                    else if (eid == 7 && (oppTot - myTot) > 5 && myFaces.Length > 0) // Dicey Dave: Double 1 die when losing roll by >5
                    {
                        int chosen = 0;
                        int maxV = -1;
                        for (int d = 0; d < myFaces.Length; d++)
                            if (myFaces[d] > maxV) { maxV = myFaces[d]; chosen = d; }
                        if (isHuman && myFaces.Length > 1)
                        {
                            SetBuffBeltDisplay(GetUiSprite("Original/images/EntourageAbility07"), "DICEY DAVE: SELECT TARGET");
                            int[] elig = new int[myFaces.Length];
                            for (int e = 0; e < elig.Length; e++) elig[e] = e;
                            yield return StartCoroutine(SelectBuffTarget(side, elig, chosen, c => chosen = c));
                        }
                        ApplyDieChange(side, chosen, myFaces[chosen] * 2);
                        ShowEntourageBanner(7, "DICEY DAVE DOUBLES A DIE!");
                        yield return new WaitForSeconds(0.60f);
                    }
                    else if (eid == 8 && _match.SecondsRemaining < 420 && !(isHome ? _homeShirleyUsed : _awayShirleyUsed))
                    {
                        if (isHome) _homeShirleyUsed = true;
                        else _awayShirleyUsed = true;

                        int deltaSec = 0;
                        int myScore = isHome ? _match.HomeScore : _match.AwayScore;
                        int oppScore = isHome ? _match.AwayScore : _match.HomeScore;
                        if (isHuman && !Application.isBatchMode)
                        {
                            yield return StartCoroutine(ShowManipulateTimeOverlay(choice => deltaSec = choice));
                        }
                        else
                        {
                            deltaSec = (myScore <= oppScore) ? 300 : -300;
                        }
                        _match.AdjustMatchClock(deltaSec);
                        if (_matchClock != null) _matchClock.text = _match.ClockText();
                        ShowEntourageBanner(8, deltaSec > 0 ? "SHIRLEY TWEETY ADDS +5 MIN!" : "SHIRLEY TWEETY SUBTRACTS 5 MIN!");
                        yield return new WaitForSeconds(0.60f);
                    }
                    else if (eid == 9 && oppTot > 20 && myFaces.Length > 0 && oppFaces.Length > 0) // Jeff Blather: Swap powerups of 3 dice
                    {
                        int count = Mathf.Min(3, Mathf.Min(myFaces.Length, oppFaces.Length));
                        bool anySwapped = false;
                        for (int d = 0; d < count; d++)
                        {
                            int myMod = myFaces[d] - (d < myOrig.Length ? myOrig[d] : myFaces[d]);
                            int oppMod = oppFaces[d] - (d < oppOrig.Length ? oppOrig[d] : oppFaces[d]);
                            if (myMod == oppMod || (d < oppImmune.Length && oppImmune[d])) continue;
                            int newMy = Mathf.Max(1, myFaces[d] - myMod + oppMod);
                            int newOpp = Mathf.Max(1, oppFaces[d] - oppMod + myMod);
                            ApplyDieChange(side, d, newMy);
                            ApplyDieChange(oppSide, d, newOpp);
                            anySwapped = true;
                        }
                        if (anySwapped)
                        {
                            ShowEntourageBanner(9, "JEFF BLATHER SWAPS POWERUPS!");
                            yield return new WaitForSeconds(0.65f);
                        }
                    }
                    else if (eid == 10 && oppTot > myTot && oppFaces.Length > 0) // Lionel Scrimmage: Flip + and - for one die
                    {
                        int bestOppIdx = -1;
                        int bestDelta = 0;
                        for (int d = 0; d < oppFaces.Length; d++)
                        {
                            if (d < oppImmune.Length && oppImmune[d]) continue;
                            int delta = oppFaces[d] - (d < oppOrig.Length ? oppOrig[d] : oppFaces[d]);
                            if (delta > bestDelta) { bestDelta = delta; bestOppIdx = d; }
                        }
                        if (bestOppIdx >= 0 && bestDelta > 0)
                        {
                            int flipped = Mathf.Max(1, (bestOppIdx < oppOrig.Length ? oppOrig[bestOppIdx] : oppFaces[bestOppIdx]) - bestDelta);
                            ApplyDieChange(oppSide, bestOppIdx, flipped);
                            ShowEntourageBanner(10, "LIONEL SCRIMMAGE FLIPS POWERUP!");
                            yield return new WaitForSeconds(0.60f);
                        }
                        else
                        {
                            int bestOwnIdx = -1;
                            int bestNeg = 0;
                            for (int d = 0; d < myFaces.Length; d++)
                            {
                                int delta = (d < myOrig.Length ? myOrig[d] : myFaces[d]) - myFaces[d];
                                if (delta > bestNeg) { bestNeg = delta; bestOwnIdx = d; }
                            }
                            if (bestOwnIdx >= 0 && bestNeg > 0)
                            {
                                int flippedOwn = (bestOwnIdx < myOrig.Length ? myOrig[bestOwnIdx] : myFaces[bestOwnIdx]) + bestNeg;
                                ApplyDieChange(side, bestOwnIdx, flippedOwn);
                                ShowEntourageBanner(10, "LIONEL SCRIMMAGE FLIPS DEBUFF!");
                                yield return new WaitForSeconds(0.60f);
                            }
                        }
                    }
                    else if (eid == 17 && produced2x && oppFaces.Length > 0) // Parshall LeBlynde: Reduce an enemy die by half
                    {
                        int targetOpp = 0;
                        int maxV = -1;
                        for (int d = 0; d < oppFaces.Length; d++)
                        {
                            if (d < oppImmune.Length && oppImmune[d]) continue;
                            if (oppFaces[d] > maxV) { maxV = oppFaces[d]; targetOpp = d; }
                        }
                        if (maxV > 0 && !(targetOpp < oppImmune.Length && oppImmune[targetOpp]))
                        {
                            if (isHuman && oppFaces.Length > 1)
                            {
                                SetBuffBeltDisplay(GetUiSprite("Original/images/EntourageAbility17"), "PARSHALL: SELECT ENEMY DIE");
                                int[] elig = new int[oppFaces.Length];
                                for (int e = 0; e < elig.Length; e++) elig[e] = e;
                                yield return StartCoroutine(SelectBuffTarget(oppSide, elig, targetOpp, c => targetOpp = c));
                            }
                            ApplyDieChange(oppSide, targetOpp, Mathf.Max(1, oppFaces[targetOpp] / 2));
                            ShowEntourageBanner(17, "PARSHALL HALVES ENEMY DIE!");
                            yield return new WaitForSeconds(0.60f);
                        }
                    }
                    else if (eid == 18 && myTot < 10 && oppFaces.Length > 0) // The Special One: Anchor 3 enemy dice (strip powerups)
                    {
                        int count = Mathf.Min(3, oppFaces.Length);
                        bool anyAnchored = false;
                        for (int d = 0; d < count; d++)
                        {
                            if (d < oppImmune.Length && oppImmune[d]) continue;
                            int origVal = d < oppOrig.Length ? oppOrig[d] : oppFaces[d];
                            if (oppFaces[d] > origVal)
                            {
                                ApplyDieChange(oppSide, d, origVal);
                                anyAnchored = true;
                            }
                        }
                        if (anyAnchored)
                        {
                            ShowEntourageBanner(18, "THE SPECIAL ONE ANCHORS DICE!");
                            yield return new WaitForSeconds(0.60f);
                        }
                    }
                    else if (eid == 20) // Pierre Bear Stare: Check any die buffed > 7 after powerups
                    {
                        for (int d = 0; d < oppFaces.Length && d < oppSlots.Length; d++)
                        {
                            int origVal = d < oppOrig.Length ? oppOrig[d] : oppFaces[d];
                            if (oppFaces[d] > 7 && origVal <= 7)
                            {
                                bool oppIsHome = oppSide == 0;
                                int card = _match.IssueYellowCard(oppIsHome, oppSlots[d]);
                                var rigs = oppIsHome ? _homePlayers : _awayPlayers;
                                if (rigs != null && oppSlots[d] < rigs.Length && rigs[oppSlots[d]] != null)
                                    rigs[oppSlots[d]].GetComponent<LayeredPlayerRig>()?.SetCardState(_match.HasYellowCard(oppIsHome, oppSlots[d]), _match.HasRedCard(oppIsHome, oppSlots[d]));
                                if (card == 2) ApplyDieChange(oppSide, d, 0, false);
                                ShowEntourageBanner(20, card == 2 ? "SECOND YELLOW! RED CARD!" : "PIERRE BEAR ISSUES YELLOW CARD!");
                                yield return new WaitForSeconds(0.60f);
                            }
                        }
                    }
                }
            }
        }

        private IEnumerator ShowManipulateTimeOverlay(Action<int> onChosen)
        {
            if (_overlayContainer == null)
            {
                onChosen?.Invoke(300);
                yield break;
            }
            var scrim = AddStretchPanel("TimeManipScrim", _overlayContainer, new Color(0f, 0f, 0f, 0.72f));
            scrim.raycastTarget = true;

            AddText("SHIRLEY TWEETY\nMANIPULATE MATCH CLOCK", scrim.transform, 18, Gold,
                new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.78f));

            int picked = 0;
            AddSpriteButton("Add5Min", "Original/sprites/GoalAnimationSS4/Add5Min",
                "Original/sprites/GoalAnimationSS4/Add5MinPressed", scrim.transform,
                new Vector2(0.32f, 0.44f), new Vector2(0.38f, 0.24f), () => picked = 300);

            AddSpriteButton("Minus5Min", "Original/sprites/GoalAnimationSS4/Minus5Min",
                "Original/sprites/GoalAnimationSS4/Minus5MinPressed", scrim.transform,
                new Vector2(0.68f, 0.44f), new Vector2(0.38f, 0.24f), () => picked = -300);

            float waitElapsed = 0f;
            while (picked == 0 && waitElapsed < 15f)
            {
                waitElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (picked == 0) picked = 300;
            Destroy(scrim.gameObject);
            onChosen?.Invoke(picked);
        }

        private IEnumerator PlayGoalAnimation(bool homeScored)
        {
            if (_overlayContainer == null) yield break;
            AudioManager.Instance.PlayGoal();
            AudioManager.Instance.PlayHighlightReel();
            AudioManager.Instance.PlayWhistle();
            TriggerHaptic();

            // Bring header scoreboard to front so it remains visible on top of sunburst rays
            if (_matchScoreboard != null) _matchScoreboard.transform.SetAsLastSibling();

            var sunburst = AddResourceImage("GoalSunburst",
                "Original/sprites/GoalAnimationSS4/GoalSunburstPattern", _overlayContainer,
                new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f));
            sunburst.rectTransform.sizeDelta = new Vector2(1600f, 1600f);
            sunburst.preserveAspect = false;
            sunburst.raycastTarget = false;

            var banner = AddResourceImage("GoalBanner",
                "Original/sprites/Prompt1/Goal", _overlayContainer,
                new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f));
            banner.rectTransform.sizeDelta = new Vector2(560f, 450f);
            banner.preserveAspect = true;

            Color32[] confettiColors = new Color32[]
            {
                new Color32(235, 45, 45, 255),
                new Color32(40, 120, 245, 255),
                new Color32(255, 60, 180, 255),
                new Color32(255, 220, 20, 255),
                new Color32(45, 200, 75, 255),
                new Color32(160, 50, 230, 255)
            };
            var confettiList = new List<Image>();
            for (var i = 0; i < 35; i++)
            {
                var cId = UnityEngine.Random.Range(1, 4);
                var x = UnityEngine.Random.Range(0.04f, 0.96f);
                var y = UnityEngine.Random.Range(0.60f, 0.98f);
                var cImg = AddResourceImage($"Confetti{i}",
                    $"Original/sprites/ConfettiSS/Confetti{cId}", _overlayContainer,
                    new Vector2(x, y), new Vector2(x, y));
                cImg.rectTransform.sizeDelta = new Vector2(30f, 30f);
                cImg.color = confettiColors[i % confettiColors.Length];
                cImg.rectTransform.localEulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(0f, 360f));
                confettiList.Add(cImg);
            }

            var celebration = _match != null ? _match.LastGoalCelebration : GoalCelebrationType.Streaker;
            var animObjects = new List<GameObject>();
            Image streakerImg = null;
            RectTransform goalCharRect = null;
            var penguinRigs = new List<(RectTransform root, Image leftFin, Image rightFin, float baseY)>();

            if (celebration == GoalCelebrationType.Streaker)
            {
                streakerImg = AddResourceImage("StreakerRunner",
                    "Original/sprites/GoalAnimationSS4/Streaker01", _overlayContainer,
                    new Vector2(-0.15f, 0.17f), new Vector2(-0.15f, 0.17f));
                streakerImg.rectTransform.sizeDelta = new Vector2(120f, 175f);
                streakerImg.preserveAspect = true;
                animObjects.Add(streakerImg.gameObject);
            }
            else if (celebration == GoalCelebrationType.Player)
            {
                PlayerData[] scorerTeam = homeScored ? _match?.HomeTeam : _match?.AwayTeam;
                PlayerData scorer = (scorerTeam != null && scorerTeam.Length > 5 && scorerTeam[5] != null) ? scorerTeam[5] : null;
                MatchKitAppearance kit = homeScored
                    ? MatchKitAppearance.FromIds(_catalog, _currentShirtId, _currentShirtColorId, _currentSleeveColorId, _currentPatternStyle, _currentPatternColorId, _currentPantsId)
                    : MatchKitAppearance.FromFixture(_catalog, _currentFixture);
                Color32 skinCol = PlayerBodySprites.GetSkinColor(scorer != null ? scorer.skinColour : 1);

                var charGo = new GameObject("GoalCharacterRig", typeof(RectTransform));
                charGo.transform.SetParent(_overlayContainer, false);
                goalCharRect = charGo.GetComponent<RectTransform>();
                goalCharRect.anchorMin = goalCharRect.anchorMax = new Vector2(0.5f, 0.24f);
                goalCharRect.sizeDelta = new Vector2(150f, 200f);
                goalCharRect.localScale = new Vector3(homeScored ? -1.45f : 1.45f, 1.45f, 1f);
                animObjects.Add(charGo);

                const string gs = "Original/sprites/GoalAnimationSS4/";
                Vector2 gSize = new Vector2(124f, 162f) * 0.85f;
                PlayerBodySprites.CreateLayer("Shirt", charGo.transform, gs + "GoalCharacterShirtColour", kit.ShirtColor, Vector2.zero, gSize);
                PlayerBodySprites.CreateLayer("Sleeve", charGo.transform, gs + "GoalCharacterSleeveColour", kit.SleeveColor, Vector2.zero, gSize);
                if (kit.PatternStyle > 0 && kit.PatternStyle <= 7)
                    PlayerBodySprites.CreateLayer("Design", charGo.transform, $"{gs}GoalCharacterShirtDesign{kit.PatternStyle}", kit.PatternColor, Vector2.zero, gSize);
                PlayerBodySprites.CreateLayer("Shoes", charGo.transform, gs + "GoalCharacterShoesColour", kit.SleeveColor, Vector2.zero, gSize);
                PlayerBodySprites.CreateLayer("Shorts", charGo.transform, gs + "GoalCharacterShortsColour", kit.PantsColor, Vector2.zero, gSize);
                PlayerBodySprites.CreateLayer("Skin", charGo.transform, gs + "GoalCharacterSkinOutline", skinCol, Vector2.zero, gSize);
                var head = PlayerBodySprites.CreateLayer("Head", charGo.transform, PlayerBodySprites.ResolveHeadPath(scorer, 6), Color.white, new Vector2(6f, 62f), new Vector2(78f, 62f));
                if (head.sprite != null) head.rectTransform.sizeDelta = head.sprite.rect.size * 0.85f;
            }
            else if (celebration == GoalCelebrationType.Penguin)
            {
                const string gs = "Original/sprites/GoalAnimationSS4/";
                float[] xs = { 0.50f, 0.27f, 0.73f };
                float[] scales = { 0.80f, 0.54f, 0.54f };
                float[] ys = { 0.23f, 0.20f, 0.20f };
                for (int pIdx = 0; pIdx < 3; pIdx++)
                {
                    var pgGo = new GameObject($"GoalPenguin{pIdx}", typeof(RectTransform));
                    pgGo.transform.SetParent(_overlayContainer, false);
                    var pRect = pgGo.GetComponent<RectTransform>();
                    pRect.anchorMin = pRect.anchorMax = new Vector2(xs[pIdx], ys[pIdx]);
                    pRect.sizeDelta = new Vector2(220f, 180f);
                    pRect.localScale = new Vector3(scales[pIdx], scales[pIdx], 1f);
                    animObjects.Add(pgGo);

                    Vector2 pSize = new Vector2(336f, 250f);
                    PlayerBodySprites.CreateLayer("Body", pgGo.transform, gs + "PenguinBody", Color.white, Vector2.zero, pSize);
                    var lf = PlayerBodySprites.CreateLayer("LeftFin", pgGo.transform, gs + "PenguinLeftFin1", Color.white, Vector2.zero, pSize);
                    var rf = PlayerBodySprites.CreateLayer("RightFin", pgGo.transform, gs + "PenguinRightFin1", Color.white, Vector2.zero, pSize);
                    penguinRigs.Add((pRect, lf, rf, ys[pIdx]));
                }
            }

            var elapsed = 0f;
            const float totalDuration = 1.6f;
            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / totalDuration);

                if (sunburst != null)
                    sunburst.rectTransform.Rotate(0f, 0f, 45f * Time.deltaTime);

                if (banner != null)
                {
                    if (progress < 0.18f)
                    {
                        float scale = Mathf.Lerp(0f, 1.25f, progress / 0.18f);
                        banner.rectTransform.localScale = new Vector3(scale, scale, 1f);
                    }
                    else if (progress < 0.28f)
                    {
                        float scale = Mathf.Lerp(1.25f, 1.0f, (progress - 0.18f) / 0.10f);
                        banner.rectTransform.localScale = new Vector3(scale, scale, 1f);
                    }
                    else
                    {
                        float breathe = 1.0f + 0.025f * Mathf.Sin((elapsed - 0.28f) * 8f);
                        banner.rectTransform.localScale = new Vector3(breathe, breathe, 1f);
                    }
                }

                if (streakerImg != null)
                {
                    var runX = Mathf.Lerp(-0.15f, 1.15f, progress);
                    var bob = Mathf.Abs(Mathf.Sin(elapsed * 12f)) * 0.025f;
                    streakerImg.rectTransform.anchorMin = streakerImg.rectTransform.anchorMax = new Vector2(runX, 0.17f + bob);
                    int frame = ((int)(elapsed * 18f) % 12) + 1;
                    streakerImg.sprite = GetUiSprite($"Original/sprites/GoalAnimationSS4/Streaker{frame:00}");
                }

                if (goalCharRect != null)
                {
                    float slideX = homeScored ? Mathf.Lerp(0.18f, 0.56f, Mathf.SmoothStep(0f, 1f, progress))
                                              : Mathf.Lerp(0.82f, 0.44f, Mathf.SmoothStep(0f, 1f, progress));
                    float hop = Mathf.Abs(Mathf.Sin(elapsed * 10f)) * 0.055f;
                    goalCharRect.anchorMin = goalCharRect.anchorMax = new Vector2(slideX, 0.22f + hop);
                    goalCharRect.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(elapsed * 10f) * 7f);
                }

                if (penguinRigs.Count > 0)
                {
                    int finFrame = ((int)(elapsed / 0.08f) % 9) + 1;
                    var ls = PlayerBodySprites.LoadSprite($"Original/sprites/GoalAnimationSS4/PenguinLeftFin{finFrame}");
                    var rs = PlayerBodySprites.LoadSprite($"Original/sprites/GoalAnimationSS4/PenguinRightFin{finFrame}");
                    for (int p = 0; p < penguinRigs.Count; p++)
                    {
                        var (pRect, lf, rf, bY) = penguinRigs[p];
                        if (lf != null && ls != null) lf.sprite = ls;
                        if (rf != null && rs != null) rf.sprite = rs;
                        if (pRect != null)
                        {
                            float waddle = Mathf.Abs(Mathf.Sin(elapsed * 11f + p * 1.1f)) * 0.035f;
                            pRect.anchorMin = pRect.anchorMax = new Vector2(pRect.anchorMin.x, bY + waddle);
                            pRect.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(elapsed * 9f + p) * 6f);
                        }
                    }
                }

                for (var i = 0; i < confettiList.Count; i++)
                {
                    var c = confettiList[i];
                    if (c != null)
                    {
                        var p = c.rectTransform.anchorMin;
                        float driftX = Mathf.Sin(elapsed * 3f + i) * 0.0012f;
                        c.rectTransform.anchorMin = c.rectTransform.anchorMax = new Vector2(p.x + driftX, p.y - Time.deltaTime * 0.18f);
                        c.rectTransform.Rotate(0f, 0f, 120f * Time.deltaTime);
                    }
                }
                yield return null;
            }

            foreach (var go in animObjects) if (go != null) Destroy(go);
            foreach (var c in confettiList) if (c != null) Destroy(c.gameObject);
            if (banner != null) Destroy(banner.gameObject);
            if (sunburst != null) Destroy(sunburst.gameObject);
        }

        private IEnumerator PlaySaveAnimation()
        {
            if (_overlayContainer == null) yield break;
            AudioManager.Instance.PlayDisappoint();
            var banner = AddResourceImage("SaveBanner",
                "Original/sprites/Prompt1/Save", _overlayContainer,
                new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f));
            banner.rectTransform.sizeDelta = new Vector2(440f, 280f);
            banner.preserveAspect = true;

            // Elastic overshoot pop in
            var elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(0f, 1.15f, elapsed / 0.25f);
                banner.rectTransform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < 0.12f)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1.15f, 1.0f, elapsed / 0.12f);
                banner.rectTransform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            yield return new WaitForSeconds(0.65f);

            elapsed = 0f;
            var col = banner.color;
            while (elapsed < 0.35f)
            {
                elapsed += Time.deltaTime;
                banner.color = new Color(col.r, col.g, col.b, 1f - elapsed / 0.35f);
                yield return null;
            }
            if (banner != null) Destroy(banner.gameObject);
        }

        private IEnumerator PlayTieAnimation()
        {
            if (_overlayContainer == null) yield break;
            AudioManager.Instance.PlayCrowdApplause();
            AudioManager.Instance.PlayWhistle();
            StartCoroutine(PlayPoofAtPosition(new Vector2(0.5f, 0.55f)));
            var banner = AddResourceImage("TieBanner",
                "Original/sprites/Prompt1/Tie", _overlayContainer,
                new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f));
            banner.rectTransform.sizeDelta = new Vector2(440f, 280f);
            banner.preserveAspect = true;

            var elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(0f, 1.15f, elapsed / 0.25f);
                banner.rectTransform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < 0.12f)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1.15f, 1.0f, elapsed / 0.12f);
                banner.rectTransform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            yield return new WaitForSeconds(0.65f);

            elapsed = 0f;
            var col = banner.color;
            while (elapsed < 0.30f)
            {
                elapsed += Time.deltaTime;
                banner.color = new Color(col.r, col.g, col.b, 1f - elapsed / 0.30f);
                yield return null;
            }
            if (banner != null) Destroy(banner.gameObject);
        }

        private IEnumerator PlayThroughPassAnimation(bool home)
        {
            if (_overlayContainer == null) yield break;
            AudioManager.Instance.PlayWhistle();
            TriggerHaptic();
            var path = home
                ? "Original/sprites/BuffTrigger1/ThroughPassGreen"
                : "Original/sprites/BuffTrigger1/ThroughPassRed";
            var banner = AddResourceImage("ThroughPassBanner", path, _overlayContainer,
                new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f));
            banner.rectTransform.sizeDelta = new Vector2(420f, 220f);
            banner.preserveAspect = true;

            var elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                var x = Mathf.Lerp(home ? 0.35f : 0.65f, 0.5f, elapsed / 0.5f);
                banner.rectTransform.anchorMin = banner.rectTransform.anchorMax = new Vector2(x, 0.55f);
                yield return null;
            }
            yield return new WaitForSeconds(0.35f);
            if (banner != null) Destroy(banner.gameObject);
        }

        private IEnumerator PlayPossessionChangeAnimation(bool homeGainedPossession)
        {
            // 1. Identify tackling player
            var (activeHome, activeAway) = GetActiveDuelSlots(_match != null ? _match.Location : 0);
            Image tackler = null;
            float lungeDir = homeGainedPossession ? 22f : -22f;

            int winnerSlot = _match != null ? _match.BallHolderSlotIndex : 1;

            if (homeGainedPossession && activeHome.Length > 0 && _homePlayers != null)
            {
                int slotIdx = Mathf.Clamp(winnerSlot, 0, activeHome.Length - 1);
                int slot = activeHome[slotIdx];
                if (slot >= 0 && slot < _homePlayers.Length) tackler = _homePlayers[slot];
            }
            else if (!homeGainedPossession && activeAway.Length > 0 && _awayPlayers != null)
            {
                int slotIdx = Mathf.Clamp(winnerSlot, 0, activeAway.Length - 1);
                int slot = activeAway[slotIdx];
                if (slot >= 0 && slot < _awayPlayers.Length) tackler = _awayPlayers[slot];
            }

            // 2. Tackler lunges forward and dispossesses opponent
            AudioManager.Instance.PlayKick();
            if (tackler != null)
            {
                tackler.rectTransform.anchoredPosition = new Vector2(lungeDir, 0f);
            }

            // 3. Ball smoothly slides & rolls to winning player's feet
            int loc = _match != null ? _match.Location : 0;
            Vector2 fromPos = _ballMarker != null ? _ballMarker.rectTransform.anchorMin : GetBallPosition(loc, homeGainedPossession ? 1 : 0);
            Vector2 toPos = GetBallPosition(loc, homeGainedPossession ? 0 : 1, winnerSlot);
            yield return StartCoroutine(SmoothPassBall(fromPos, toPos, 0.32f, false));

            // 4. Tackler recovers to normal stance
            if (tackler != null)
            {
                tackler.rectTransform.anchoredPosition = Vector2.zero;
            }

            // 5. Possession change banner animation
            if (_overlayContainer != null)
            {
                var path = homeGainedPossession
                    ? "Original/sprites/Prompt1/PossessionChangeGreen"
                    : "Original/sprites/Prompt1/PossessionChangeRed";
                var banner = AddResourceImage("PossessionChangeBanner", path, _overlayContainer,
                    new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f));
                banner.rectTransform.sizeDelta = new Vector2(440f, 240f);
                banner.preserveAspect = true;

                if (homeGainedPossession)
                    AudioManager.Instance.PlayWhistle();
                else
                    AudioManager.Instance.PlayDisappoint();
                TriggerHaptic();

                var elapsed = 0f;
                while (elapsed < 0.22f)
                {
                    elapsed += Time.deltaTime;
                    banner.rectTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.05f, elapsed / 0.22f);
                    yield return null;
                }
                elapsed = 0f;
                while (elapsed < 0.10f)
                {
                    elapsed += Time.deltaTime;
                    banner.rectTransform.localScale = Vector3.Lerp(Vector3.one * 1.05f, Vector3.one, elapsed / 0.10f);
                    yield return null;
                }
                yield return new WaitForSeconds(0.35f);
                elapsed = 0f;
                var col = banner.color;
                while (elapsed < 0.22f)
                {
                    elapsed += Time.deltaTime;
                    banner.color = new Color(col.r, col.g, col.b, 1f - elapsed / 0.22f);
                    yield return null;
                }
                if (banner != null) Destroy(banner.gameObject);
            }
        }

        private IEnumerator PlayPoofAnimation(int slot)
        {
            if (_homePlayers == null || slot < 0 || slot >= _homePlayers.Length) yield break;
            var target = _homePlayers[slot];
            var poofObj = new GameObject("PoofEffect", typeof(RectTransform), typeof(Image));
            poofObj.transform.SetParent(_field, false);
            var rect = poofObj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = target.rectTransform.anchorMin;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(100f, 100f);
            var img = poofObj.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;

            for (var f = 1; f <= 13; f++)
            {
                img.sprite = GetUiSprite($"Original/sprites/PlayerBody3/Poof{f:00}");
                yield return new WaitForSeconds(0.05f);
            }
            Destroy(poofObj);
        }

        private void ShowGameOverModal()
        {
            var container = _overlayContainer != null ? _overlayContainer : (_safeArea != null ? _safeArea : _canvas.transform);
            Time.timeScale = 1.0f;

            if (_isLocalMultiplayer)
            {
                var p1Won = _match.HomeScore > _match.AwayScore;
                var p2Won = _match.AwayScore > _match.HomeScore;
                var isDraw = _match.HomeScore == _match.AwayScore;

                if (!_currentResultRecorded)
                {
                    _currentResultRecorded = true;
                    int mpMatches = PlayerPrefs.GetInt("diceSoccer.stats.mpMatches", 0) + 1;
                    PlayerPrefs.SetInt("diceSoccer.stats.mpMatches", mpMatches);
                    if (p1Won)
                    {
                        int mpWins = PlayerPrefs.GetInt("diceSoccer.stats.mpWins", 0) + 1;
                        PlayerPrefs.SetInt("diceSoccer.stats.mpWins", mpWins);
                    }
                    EvaluateCareerProgressAndAchievements(p1Won, isDraw, true, true);
                }

                if (p1Won || p2Won)
                {
                    AudioManager.Instance.PlayMedalCeremonyLong();
                    AudioManager.Instance.PlayEffect("KidsCheering", 1f, 1f);
                }
                else
                {
                    AudioManager.Instance.PlayEffect("ChildrenAaaah", 1f, 0.9f);
                }

                var mpBackdrop = AddPanel("GameOverBackdrop", container,
                    new Color(0f, 0f, 0f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.one);
                mpBackdrop.raycastTarget = true;

                var mpBox = AddResourceImage("GameOverBox", "Original/sprites/GameOver/GameOverBox",
                    mpBackdrop.transform,
                    new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f));
                mpBox.rectTransform.sizeDelta = new Vector2(410f, 600f);
                mpBox.preserveAspect = true;

                StartCoroutine(PopInBounceAnimation(mpBox.rectTransform));

                var winnerText = isDraw
                    ? "IT'S A DRAW!"
                    : (p1Won ? "PLAYER 1 (RED) WINS!" : "PLAYER 2 (BLUE) WINS!");
                var winnerColor = isDraw ? (Color)Ink : (Color)(p1Won ? new Color32(215, 45, 45, 255) : new Color32(45, 115, 215, 255));

                var mpTitle = AddText(winnerText, mpBox.transform, 22, winnerColor,
                    new Vector2(0.08f, 0.74f), new Vector2(0.92f, 0.84f));
                mpTitle.fontStyle = FontStyle.Bold;
                mpTitle.alignment = TextAnchor.MiddleCenter;

                var scoreText = AddText($"P1 (RED)  {_match.HomeScore} - {_match.AwayScore}  P2 (BLUE)", mpBox.transform, 22, Ink,
                    new Vector2(0.08f, 0.61f), new Vector2(0.92f, 0.71f));
                scoreText.fontStyle = FontStyle.Bold;
                scoreText.alignment = TextAnchor.MiddleCenter;

                int homePossPct = (_match.HomePossessionTicks + _match.AwayPossessionTicks > 0)
                    ? Mathf.RoundToInt((float)_match.HomePossessionTicks / (_match.HomePossessionTicks + _match.AwayPossessionTicks) * 100f)
                    : 50;
                int awayPossPct = 100 - homePossPct;

                var mpStats = AddText(
                    $"Possession: {homePossPct}% - {awayPossPct}%\n" +
                    $"Shots: {_match.HomeShots} - {_match.AwayShots}   Saves: {_match.HomeSaves} - {_match.AwaySaves}\n" +
                    $"Tackles Won: {_match.HomeTackles} - {_match.AwayTackles}",
                    mpBox.transform, 13, Ink,
                    new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.58f));
                mpStats.alignment = TextAnchor.MiddleCenter;

                var subTitle = AddText(isDraw ? "Great battle between both managers!" : (p1Won ? "Player 1 takes the victory!" : "Player 2 takes the victory!"),
                    mpBox.transform, 13, new Color32(70, 75, 80, 255),
                    new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.44f));
                subTitle.alignment = TextAnchor.MiddleCenter;

                // Rematch button
                AddButton("⚔ REMATCH (1v1)", mpBox.transform, Grass,
                    new Vector2(0.5f, 0.26f), new Vector2(0.75f, 0.085f), () =>
                    {
                        Destroy(mpBackdrop.gameObject);
                        ShowNextUnlockModalThen(StartLocalMultiplayerMatch);
                    });

                // Main menu button
                AddButton("MAIN MENU", mpBox.transform, new Color32(68, 79, 84, 255),
                    new Vector2(0.5f, 0.14f), new Vector2(0.75f, 0.075f), () =>
                    {
                        _isLocalMultiplayer = false;
                        Destroy(mpBackdrop.gameObject);
                        ShowNextUnlockModalThen(ShowHome);
                    });

                if (p1Won || p2Won)
                {
                    for (var i = 0; i < 24; i++)
                    {
                        var cId = UnityEngine.Random.Range(1, 4);
                        var x = UnityEngine.Random.Range(0.05f, 0.95f);
                        var y = UnityEngine.Random.Range(0.40f, 0.95f);
                        var cImg = AddResourceImage($"VictoryConfetti{i}",
                            $"Original/sprites/ConfettiSS/Confetti{cId}", mpBackdrop.transform,
                            new Vector2(x, y), new Vector2(x, y));
                        cImg.rectTransform.sizeDelta = new Vector2(22f, 22f);
                        cImg.rectTransform.localEulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(0f, 360f));
                        cImg.raycastTarget = false;
                    }
                }
                return;
            }

            var won = _match.HomeScore > _match.AwayScore;
            var drew = _match.HomeScore == _match.AwayScore;

            if (won)
            {
                AudioManager.Instance.PlayMedalCeremonyLong();
                AudioManager.Instance.PlayEffect("KidsCheering", 1f, 1f);
            }
            else if (drew)
            {
                AudioManager.Instance.PlayEffect("ChildrenAaaah", 1f, 0.9f);
            }
            else
            {
                AudioManager.Instance.PlayKidsBooing();
            }

            if (_lastPayout > 0)
                AudioManager.Instance.PlayCash();

            var backdrop = AddPanel("GameOverBackdrop", container,
                new Color(0f, 0f, 0f, 0.82f), new Vector2(0.5f, 0.5f), Vector2.one);
            backdrop.raycastTarget = true;

            // Base yellow card with "GAMEOVER" header (proportional 460x610 frame)
            var box = AddResourceImage("GameOverBox", "Original/sprites/GameOver/GameOverBox", backdrop.transform,
                new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f));
            box.rectTransform.sizeDelta = new Vector2(460f, 610f);
            box.preserveAspect = true;

            // Colored bottom pill overlay (green VictoryBox, yellow DrawBox, red DefeatBox)
            var pillOverlayPath = won
                ? "Original/sprites/GameOver/GameOverVictoryBox"
                : drew ? "Original/sprites/GameOver/GameOverDrawBox" : "Original/sprites/GameOver/GameOverDefeatBox";
            var pillOverlay = AddResourceImage("GameOverPillOverlay", pillOverlayPath, box.transform,
                Vector2.zero, Vector2.one);
            pillOverlay.preserveAspect = true;
            pillOverlay.raycastTarget = false;

            // Authentic Cocos2d scale pop-in bounce animation
            StartCoroutine(PopInBounceAnimation(box.rectTransform));

            var titlePath = won
                ? "Original/sprites/GameOver/GameOverVictory"
                : drew ? "Original/sprites/GameOver/GameOverDraw" : "Original/sprites/GameOver/GameOverDefeat";
            var title = AddResourceImage("GameOverTitle", titlePath, box.transform,
                new Vector2(0.5f, 0.77f), new Vector2(0.5f, 0.77f));
            title.rectTransform.sizeDelta = new Vector2(310f, 75f);
            title.preserveAspect = true;

            var scoreTextSingle = AddText($"{_match.HomeScore} - {_match.AwayScore}", box.transform, 38, Ink,
                new Vector2(0.1f, 0.61f), new Vector2(0.9f, 0.72f));
            scoreTextSingle.fontStyle = FontStyle.Bold;

            var rewardTextSingle = AddText($"REWARD:\n{_lastPayout:N0} credits", box.transform, 20, Ink,
                new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.59f));
            rewardTextSingle.fontStyle = FontStyle.Bold;

            var statsTextSingle = AddText(
                $"Shots: {_match.HomeShots} - {_match.AwayShots}   Saves: {_match.HomeSaves} - {_match.AwaySaves}\nTackles Won: {_match.HomeTackles} - {_match.AwayTackles}",
                box.transform, 14, Ink,
                new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.43f));
            statsTextSingle.alignment = TextAnchor.MiddleCenter;

            if (won)
            {
                var highScoreBadge = AddResourceImage("NewHighScore", "Original/sprites/GameOver/GameOverNewHighScore", box.transform,
                    new Vector2(0.83f, 0.55f), new Vector2(0.83f, 0.55f));
                highScoreBadge.rectTransform.sizeDelta = new Vector2(125f, 92f);
                highScoreBadge.preserveAspect = true;
                highScoreBadge.raycastTarget = false;
            }

            var stars = _match.CalculateStars();
            var starIndex = Mathf.Clamp(stars, 0, 3);
            var starImg = AddResourceImage("StarRating",
                $"Original/sprites/GameOver/Gold{starIndex}Star", box.transform,
                new Vector2(0.5f, 0.19f), new Vector2(0.5f, 0.19f));
            starImg.rectTransform.sizeDelta = new Vector2(280f, 115f);
            starImg.preserveAspect = true;

            // Victory confetti shower:
            if (won)
            {
                for (var i = 0; i < 24; i++)
                {
                    var cId = UnityEngine.Random.Range(1, 4);
                    var x = UnityEngine.Random.Range(0.05f, 0.95f);
                    var y = UnityEngine.Random.Range(0.40f, 0.95f);
                    var cImg = AddResourceImage($"VictoryConfetti{i}",
                        $"Original/sprites/ConfettiSS/Confetti{cId}", backdrop.transform,
                        new Vector2(x, y), new Vector2(x, y));
                    cImg.rectTransform.sizeDelta = new Vector2(22f, 22f);
                    cImg.rectTransform.localEulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(0f, 360f));
                    cImg.raycastTarget = false;
                }
            }

            AddSpriteButton("Continue", "Original/images/ContinueButton",
                "Original/images/ContinueButtonPressed", box.transform,
                new Vector2(0.5f, 0.03f), new Vector2(0.55f, 0.11f),
                () => {
                    Action proceedToFixtures = () =>
                    {
                        if (_competitionName == "League" && _selectedFixtureIndex >= _seasonFixtures.Length - 1)
                        {
                            ShowSeasonCompletion();
                        }
                        else if (_competitionName != "League" && _selectedFixtureIndex >= _seasonFixtures.Length - 1 && won)
                        {
                            ShowTournamentVictoryModal(_competitionName);
                        }
                        else
                        {
                            ShowLeagueFixtures();
                            if (_seasonIndex == 0 && _selectedFixtureIndex == 0 && PlayerPrefs.GetInt("diceSoccer.tutorial.21", 0) == 0)
                            {
                                ShowTutorialPopup(21);
                            }
                        }
                    };

                    Destroy(backdrop.gameObject);
                    ShowNextUnlockModalThen(proceedToFixtures);
                });
        }

        private void ShowNextUnlockModalThen(Action onAllDone)
        {
            if (_pendingUnlockedPlayerIds.Count > 0)
            {
                int nextPid = _pendingUnlockedPlayerIds[0];
                _pendingUnlockedPlayerIds.RemoveAt(0);
                ShowPlayerUnlockedModal(nextPid, () => ShowNextUnlockModalThen(onAllDone));
                return;
            }
            if (_pendingUnlockedAchievements.Count > 0)
            {
                var nextAch = _pendingUnlockedAchievements[0];
                _pendingUnlockedAchievements.RemoveAt(0);
                ShowAchievementUnlockedModal(nextAch, () => ShowNextUnlockModalThen(onAllDone));
                return;
            }
            onAllDone?.Invoke();
        }

        private void ShowPlayerUnlockedModal(int playerId, Action onDone)
        {
            var unlockContainer = _overlayContainer != null ? _overlayContainer : (_safeArea != null ? _safeArea : (_canvas != null ? _canvas.transform : null));
            if (unlockContainer == null)
            {
                onDone?.Invoke();
                return;
            }

            _catalog ??= GameCatalog.Load();
            var player = _catalog.GetPlayer(playerId);
            if (player == null)
            {
                onDone?.Invoke();
                return;
            }

            AudioManager.Instance.PlayMedalCeremony();

            var backdrop = AddPanel("PlayerUnlockedBackdrop", unlockContainer,
                new Color(0f, 0f, 0f, 0.82f), new Vector2(0.5f, 0.5f), Vector2.one);
            backdrop.raycastTarget = true;

            var box = AddResourceImage("PlayerUnlockedBox", "Original/sprites/AchievementsSS4/LongDialogBox", backdrop.transform,
                new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f));
            box.rectTransform.sizeDelta = new Vector2(410f, 520f);
            box.preserveAspect = false;

            StartCoroutine(PopInBounceAnimation(box.rectTransform));

            var posBadgeSuffix = player.position == "MID" ? "MF"
                : player.position == "FWD" ? "FWD"
                : player.position == "DEF" ? "DEF" : "GK";
            var badgeImg = AddResourceImage("UnlockBadge", $"Original/sprites/AchievementsSS4/UnlockPlayer{posBadgeSuffix}", box.transform,
                new Vector2(0.22f, 0.85f), new Vector2(0.22f, 0.85f));
            badgeImg.rectTransform.sizeDelta = new Vector2(82f, 82f);
            badgeImg.preserveAspect = true;

            var title = AddText("PLAYER\nUNLOCKED", box.transform, 22, Ink,
                new Vector2(0.36f, 0.77f), new Vector2(0.92f, 0.92f));
            title.alignment = TextAnchor.MiddleLeft;
            title.fontStyle = FontStyle.Bold;

            var sub = AddText($"{player.name.ToUpperInvariant()} is\nnow available for\nhire!", box.transform, 19, Ink,
                new Vector2(0.10f, 0.54f), new Vector2(0.90f, 0.74f));
            sub.alignment = TextAnchor.MiddleCenter;
            sub.fontStyle = FontStyle.Bold;

            var figImg = AddResourceImage("UnlockedFigure", GetPlayerFigurePath(player, playerId), box.transform,
                new Vector2(0.5f, 0.31f), new Vector2(0.5f, 0.31f));
            figImg.rectTransform.sizeDelta = new Vector2(130f, 220f);
            figImg.preserveAspect = true;
            figImg.raycastTarget = false;

            AddSpriteButton("UnlockedDone", "Original/images/DoneButton",
                "Original/images/DoneButtonPressed", box.transform,
                new Vector2(0.5f, 0.04f), new Vector2(0.48f, 0.11f), () =>
                {
                    AudioManager.Instance.PlayTap();
                    Destroy(backdrop.gameObject);
                    onDone?.Invoke();
                });
        }

        private IEnumerator PopInBounceAnimation(RectTransform targetRect)
        {
            var elapsed = 0f;
            const float scaleUpTime = 0.22f;
            while (elapsed < scaleUpTime)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / scaleUpTime);
                targetRect.localScale = Vector3.Lerp(Vector3.one * 0.4f, Vector3.one * 1.15f, t);
                yield return null;
            }
            elapsed = 0f;
            const float settleTime = 0.12f;
            while (elapsed < settleTime)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / settleTime);
                targetRect.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, t);
                yield return null;
            }
            targetRect.localScale = Vector3.one;
        }

        private void ShowPauseMenu()
        {
            if (_overlayContainer == null || _match == null || _match.IsFinished) return;
            Time.timeScale = 0f;
            var backdrop = AddPanel("PauseBackdrop", _overlayContainer,
                new Color(0f, 0f, 0f, 0.78f), new Vector2(0.5f, 0.5f), Vector2.one);
            backdrop.raycastTarget = true;

            var box = AddResourceImage("PauseBox", "Original/sprites/AchievementsSS4/LongDialogBox", backdrop.transform,
                new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.50f));
            box.rectTransform.sizeDelta = new Vector2(380f, 580f);
            box.preserveAspect = true;

            // Paused header banner:
            var header = AddResourceImage("PausedHeader", "Original/images/Paused", box.transform,
                new Vector2(0.5f, 0.83f), new Vector2(0.5f, 0.83f));
            header.rectTransform.sizeDelta = new Vector2(240f, 55f);
            header.preserveAspect = true;

            // 1. Resume Button:
            AddSpriteButton("Resume", "Original/images/ResumeButton",
                "Original/images/ResumeButtonPressed", box.transform,
                new Vector2(0.5f, 0.69f), new Vector2(0.68f, 0.12f),
                () => {
                    Time.timeScale = _matchSpeed;
                    Destroy(backdrop.gameObject);
                });

            // 2. Substitution Button:
            var subBtn = AddSpriteButton("Substitution", "Original/images/SubstitutionButton",
                "Original/images/SubstitutionButtonPressed", box.transform,
                new Vector2(0.5f, 0.53f), new Vector2(0.68f, 0.12f),
                () => {
                    if (_remainingSubs > 0 && _ownedPlayerIds.Count > 7)
                    {
                        Time.timeScale = _matchSpeed;
                        Destroy(backdrop.gameObject);
                        ShowInMatchSubstitutionBar();
                    }
                });
            if (_remainingSubs <= 0 || _ownedPlayerIds.Count <= 7)
                subBtn.interactable = false;

            AddText($"{_remainingSubs} SUBS LEFT", box.transform, 13, Ink,
                new Vector2(0.15f, 0.42f), new Vector2(0.85f, 0.48f));

            // 3. Pause Options Button:
            AddSpriteButton("PauseOptions", "Original/images/PauseOptionsButton",
                "Original/images/PauseOptionsButtonPressed", box.transform,
                new Vector2(0.5f, 0.31f), new Vector2(0.65f, 0.12f),
                () => ShowOptionsDialog(backdrop.transform));

            // 4. Forfeit Button:
            AddSpriteButton("Forfeit", "Original/images/ForfeitButton",
                "Original/images/ForfeitButtonPressed", box.transform,
                new Vector2(0.5f, 0.16f), new Vector2(0.65f, 0.12f),
                () => {
                    Time.timeScale = 1f;
                    ShowForfeitConfirmation(backdrop.transform);
                });
        }

        private void ShowForfeitConfirmation(Transform parent)
        {
            var forfeitModal = AddPanel("ForfeitModal", parent,
                new Color(0f, 0f, 0f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.one);
            forfeitModal.raycastTarget = true;

            var box = AddResourceImage("ForfeitBox", "Original/images/DialogBox", forfeitModal.transform,
                new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f));
            box.rectTransform.sizeDelta = new Vector2(340f, 260f);
            box.preserveAspect = true;

            var prompt = AddText("ARE YOU SURE YOU WANT TO FORFEIT?", box.transform, 15, Ink,
                new Vector2(0.08f, 0.50f), new Vector2(0.92f, 0.86f));
            prompt.alignment = TextAnchor.MiddleCenter;

            // YES button
            AddSpriteButton("YesButton", "Original/images/YesButton",
                "Original/images/YesButtonPressed", box.transform,
                new Vector2(0.28f, 0.22f), new Vector2(0.38f, 0.22f), () =>
                {
                    Destroy(parent.gameObject);
                    _match.ForfeitMatch();
                    _currentResultRecorded = true;
                    RecordFixtureResult();
                    ShowGameOverModal();
                });

            // NO button
            AddSpriteButton("NoButton", "Original/images/NoButton",
                "Original/images/NoButtonPressed", box.transform,
                new Vector2(0.72f, 0.22f), new Vector2(0.38f, 0.22f), () =>
                {
                    Destroy(forfeitModal.gameObject);
                });
        }

        private void ShowOptionsDialog(Transform parent)
        {
            var optionsBackdrop = AddPanel("OptionsBackdrop", parent,
                new Color(0f, 0f, 0f, 0.60f), new Vector2(0.5f, 0.5f), Vector2.one);
            optionsBackdrop.raycastTarget = true;

            var box = AddResourceImage("OptionsBox", "Original/sprites/AchievementsSS4/LongDialogBox", optionsBackdrop.transform,
                new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f));
            box.rectTransform.sizeDelta = new Vector2(360f, 560f);
            box.preserveAspect = false;

            var title = AddText("OPTIONS", box.transform, 22, Ink,
                new Vector2(0.12f, 0.845f), new Vector2(0.88f, 0.925f));
            title.fontStyle = FontStyle.Bold;

            void AddOptionCheckboxRow(string namePrefix, string labelText, float centerY, bool initialValue, Func<bool> onToggle)
            {
                var label = AddText(labelText, box.transform, 15, Ink,
                    new Vector2(0.15f, centerY - 0.038f), new Vector2(0.68f, centerY + 0.038f));
                label.alignment = TextAnchor.MiddleLeft;
                label.fontStyle = FontStyle.Bold;

                var tickBox = AddResourceImage($"{namePrefix}TickBox", "Original/sprites/OptionsSS/OptionsTickBox", box.transform,
                    new Vector2(0.77f, centerY), new Vector2(0.77f, centerY));
                tickBox.rectTransform.sizeDelta = new Vector2(28f, 28f);
                tickBox.preserveAspect = true;

                var tick = AddResourceImage($"{namePrefix}Tick", "Original/sprites/OptionsSS/OptionsTick", tickBox.transform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                tick.rectTransform.sizeDelta = new Vector2(22f, 22f);
                tick.preserveAspect = true;
                tick.gameObject.SetActive(initialValue);

                void HandleToggle()
                {
                    bool next = onToggle();
                    tick.gameObject.SetActive(next);
                    AudioManager.Instance.PlayTap();
                }

                var boxBtn = tickBox.gameObject.AddComponent<Button>();
                boxBtn.onClick.AddListener(HandleToggle);
                var labelBtn = label.gameObject.AddComponent<Button>();
                labelBtn.transition = Selectable.Transition.None;
                labelBtn.onClick.AddListener(HandleToggle);
            }

            AddOptionCheckboxRow("Music", "MUSIC", 0.775f, AudioManager.Instance.MusicEnabled, () =>
            {
                AudioManager.Instance.MusicEnabled = !AudioManager.Instance.MusicEnabled;
                return AudioManager.Instance.MusicEnabled;
            });

            AddOptionCheckboxRow("Sfx", "SFX", 0.690f, AudioManager.Instance.SfxEnabled, () =>
            {
                AudioManager.Instance.SfxEnabled = !AudioManager.Instance.SfxEnabled;
                return AudioManager.Instance.SfxEnabled;
            });

            AddOptionCheckboxRow("Vib", "VIBRATION", 0.605f, PlayerPrefs.GetInt("diceSoccer.vibration", 1) != 0, () =>
            {
                bool next = PlayerPrefs.GetInt("diceSoccer.vibration", 1) == 0;
                PlayerPrefs.SetInt("diceSoccer.vibration", next ? 1 : 0);
                PlayerPrefs.Save();
                if (next) TriggerHaptic();
                return next;
            });

            AddOptionCheckboxRow("Fs", "FULL SCREEN", 0.520f, PlayerPrefs.GetInt("diceSoccer.fullscreen", 0) == 1, () =>
            {
                bool next = PlayerPrefs.GetInt("diceSoccer.fullscreen", 0) == 0;
                PlayerPrefs.SetInt("diceSoccer.fullscreen", next ? 1 : 0);
                PlayerPrefs.Save();
                ApplySafeArea();
                return next;
            });

            AddOptionCheckboxRow("Speed", "2X MATCH SPEED", 0.435f, PlayerPrefs.GetInt("diceSoccer.fastSpeed", 0) == 1, () =>
            {
                bool next = PlayerPrefs.GetInt("diceSoccer.fastSpeed", 0) == 0;
                PlayerPrefs.SetInt("diceSoccer.fastSpeed", next ? 1 : 0);
                PlayerPrefs.Save();
                _matchSpeed = next ? 2f : 1f;
                return next;
            });

            // Button 1: CREDITS
            AddSpriteButton("Credits", "Original/sprites/OptionsSS/CreditsButton",
                "Original/sprites/OptionsSS/CreditsButtonPressed", box.transform,
                new Vector2(0.5f, 0.325f), new Vector2(0.64f, 0.085f), () =>
                {
                    ShowCreditsDialog(optionsBackdrop.transform);
                });

            // Button 2: HANDBOOK
            AddSpriteButton("Handbook", "Original/sprites/OptionsSS/HandbookButton",
                "Original/sprites/OptionsSS/HandbookButtonPressed", box.transform,
                new Vector2(0.5f, 0.220f), new Vector2(0.64f, 0.085f), () =>
                {
                    ShowHandbookDialog(optionsBackdrop.transform);
                });

            // Button 3: DONE
            AddSpriteButton("Done", "Original/images/DoneButton",
                "Original/images/DoneButtonPressed", box.transform,
                new Vector2(0.5f, 0.095f), new Vector2(0.42f, 0.080f), () =>
                {
                    Destroy(optionsBackdrop.gameObject);
                });
        }

        private void ShowHandbookDialog(Transform parent)
        {
            var hbBackdrop = AddPanel("HandbookBackdrop", parent,
                new Color(0f, 0f, 0f, 0.90f), new Vector2(0.5f, 0.5f), Vector2.one);
            hbBackdrop.raycastTarget = true;

            var box = AddResourceImage("HandbookBox", "Original/sprites/AchievementsSS4/LongDialogBox", hbBackdrop.transform,
                new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f));
            box.rectTransform.sizeDelta = new Vector2(460f, 660f);
            box.preserveAspect = false;

            var title = AddText("HANDBOOK", box.transform, 22, Ink,
                new Vector2(0.12f, 0.83f), new Vector2(0.88f, 0.91f));
            title.fontStyle = FontStyle.Bold;

            var body = AddText(
                "DICE SOCCER RULES:\n\n" +
                "• Shake or tap to roll your team's dice.\n\n" +
                "• Highest total moves the ball forward toward the goal.\n\n" +
                "• CF in opposing penalty box shoots on goal!\n\n" +
                "• Match superpowers and dice triggers to turn the tide!\n\n" +
                "• Hire new talent and equip Entourage staff for permanent buffs.",
                box.transform, 14, Ink,
                new Vector2(0.12f, 0.22f), new Vector2(0.88f, 0.80f));
            body.alignment = TextAnchor.UpperLeft;

            AddSpriteButton("CloseHandbook", "Original/images/DoneButton",
                "Original/images/DoneButtonPressed", box.transform,
                new Vector2(0.5f, 0.11f), new Vector2(0.48f, 0.08f), () =>
                {
                    Destroy(hbBackdrop.gameObject);
                    PlayerPrefs.SetInt("diceSoccer.stats.visitedNews", 1);
                    EvaluateCareerProgressAndAchievements(false, false, false, true);
                    ShowNextUnlockModalThen(null);
                });
        }

        private void ShowCreditsDialog(Transform parent)
        {
            var crBackdrop = AddPanel("CreditsBackdrop", parent,
                new Color(0f, 0f, 0f, 0.90f), new Vector2(0.5f, 0.5f), Vector2.one);
            crBackdrop.raycastTarget = true;

            var box = AddResourceImage("CreditsBox", "Original/sprites/AchievementsSS4/LongDialogBox", crBackdrop.transform,
                new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f));
            box.rectTransform.sizeDelta = new Vector2(460f, 660f);
            box.preserveAspect = false;

            var title = AddText("CREDITS", box.transform, 22, Ink,
                new Vector2(0.12f, 0.83f), new Vector2(0.88f, 0.91f));
            title.fontStyle = FontStyle.Bold;

            var body = AddText(
                "DICE SOCCER (2012)\n\n" +
                "Original Game Design & Art:\n" +
                "Red Robot Labs & Foursaken Media\n\n" +
                "Sound & Music:\n" +
                "Original 2012 Audio Archive\n\n" +
                "2026 Preservation & Remake:\n" +
                "Karazero\n\n" +
                "Archive Guardian:\n" +
                "jairsldn\n" +
                "(The sole keeper of the 2012 files,\nwithout whom this project could not exist)\n\n" +
                "100% Faithful Unity Remake.",
                box.transform, 13, Ink,
                new Vector2(0.08f, 0.27f), new Vector2(0.92f, 0.81f));
            body.alignment = TextAnchor.MiddleCenter;

            // PayPal Support Button
            AddButton("☕ BUY A COFFEE (PAYPAL)", box.transform, new Color32(0, 112, 186, 255),
                new Vector2(0.5f, 0.20f), new Vector2(0.68f, 0.07f), ShowDonationModal);

            AddSpriteButton("CloseCredits", "Original/images/DoneButton",
                "Original/images/DoneButtonPressed", box.transform,
                new Vector2(0.5f, 0.11f), new Vector2(0.48f, 0.08f), () =>
                {
                    Destroy(crBackdrop.gameObject);
                    PlayerPrefs.SetInt("diceSoccer.stats.visitedCredits", 1);
                    EvaluateCareerProgressAndAchievements(false, false, false, true);
                    ShowNextUnlockModalThen(null);
                });
        }

        private void ShowTutorialPopup(int tutorialId, Action onDone = null, bool force = false)
        {
            var prefKey = $"diceSoccer.tutorial.{tutorialId}";
            if (!force && PlayerPrefs.GetInt(prefKey, 0) != 0)
            {
                onDone?.Invoke();
                return;
            }

            var tut = _catalog.GetTutorial(tutorialId);
            if (tut == null || tut.popupText == null || tut.popupText.Length == 0)
            {
                onDone?.Invoke();
                return;
            }

            var currentTextIndex = 0;
            GameObject backdropGo = null;

            Action renderPage = null;
            renderPage = () =>
            {
                if (backdropGo != null) Destroy(backdropGo);

                backdropGo = new GameObject("TutorialBackdrop", typeof(RectTransform), typeof(Image));
                backdropGo.transform.SetParent(_safeArea, false);
                var bdRect = backdropGo.GetComponent<RectTransform>();
                bdRect.anchorMin = Vector2.zero;
                bdRect.anchorMax = Vector2.one;
                bdRect.offsetMin = bdRect.offsetMax = Vector2.zero;
                var bdImg = backdropGo.GetComponent<Image>();
                bdImg.color = new Color(0f, 0f, 0f, 0.78f);
                bdImg.raycastTarget = true;

                bool hasCp = (tut.customPopup != null && tut.customPopup.Length > 0);

                var box = AddResourceImage("TutorialBox", "Original/sprites/AchievementsSS4/LongDialogBox", backdropGo.transform,
                    new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f));
                box.rectTransform.sizeDelta = hasCp ? new Vector2(440f, 680f) : new Vector2(410f, 440f);
                box.preserveAspect = false;

                // Speaker portrait: Willy the Kid (KidHead)
                var headImg = AddResourceImage("SpeakerHead", "Original/sprites/AchievementsSS4/KidHead", box.transform,
                    new Vector2(0.18f, 0.89f), new Vector2(0.18f, 0.89f));
                headImg.rectTransform.sizeDelta = new Vector2(82f, 82f);
                headImg.preserveAspect = true;

                var title = AddText("Willy the Kid", box.transform, 21, new Color32(42, 138, 36, 255),
                    new Vector2(0.32f, 0.83f), new Vector2(0.90f, 0.93f));
                title.alignment = TextAnchor.MiddleLeft;
                title.fontStyle = FontStyle.Bold;

                // Optional custom illustration in center
                if (hasCp)
                {
                    var cpId = tut.customPopup[Mathf.Min(currentTextIndex, tut.customPopup.Length - 1)];
                    var cpPath = (cpId == 4 || cpId == 7)
                        ? $"Original/sprites/CustomPopup2/Custom{cpId}"
                        : $"Original/sprites/CustomPopup1/Custom{cpId}";
                    var cpImg = AddResourceImage("CustomIllustration", cpPath, box.transform,
                        new Vector2(0.5f, 0.60f), new Vector2(0.5f, 0.60f));
                    cpImg.rectTransform.sizeDelta = new Vector2(260f, 180f);
                    cpImg.preserveAspect = true;
                }

                // Text
                var textContent = tut.popupText[currentTextIndex];
                var textYMin = hasCp ? 0.20f : 0.24f;
                var textYMax = hasCp ? 0.44f : 0.78f;
                var promptText = AddText(textContent, box.transform, 17, Ink,
                    new Vector2(0.12f, textYMin), new Vector2(0.88f, textYMax));
                promptText.alignment = TextAnchor.MiddleCenter;
                promptText.resizeTextForBestFit = true;
                promptText.resizeTextMinSize = 14;
                promptText.resizeTextMaxSize = 19;
                promptText.verticalOverflow = VerticalWrapMode.Truncate;

                // Button: NEXT or DONE
                var isLast = currentTextIndex >= tut.popupText.Length - 1;
                AddSpriteButton("TutorialDone", "Original/images/DoneButton",
                    "Original/images/DoneButtonPressed", box.transform,
                    new Vector2(0.5f, 0.05f), new Vector2(0.48f, 0.11f), () =>
                    {
                        AudioManager.Instance.PlayTap();
                        if (isLast)
                        {
                            PlayerPrefs.SetInt(prefKey, 1);
                            PlayerPrefs.Save();
                            Destroy(backdropGo);
                            onDone?.Invoke();
                        }
                        else
                        {
                            currentTextIndex++;
                            renderPage();
                        }
                    });
            };

            renderPage();
        }

        private static string CompactMatchReport(string report, string clock)
        {
            var clockSuffix = $"  {clock}";
            if (report.EndsWith(clockSuffix, StringComparison.Ordinal))
                report = report.Substring(0, report.Length - clockSuffix.Length);

            if (report.IndexOf(" vs ", StringComparison.Ordinal) >= 0)
            {
                var resultStart = report.IndexOf(". ", StringComparison.Ordinal);
                if (resultStart >= 0)
                    report = report.Substring(resultStart + 2);
            }

            return report;
        }

        private int _lastPayout;

        private void RecordFixtureResult()
        {
            if (_isLocalMultiplayer)
            {
                _lastPayout = 0;
                return;
            }

            var won = _match.HomeScore > _match.AwayScore;
            var drew = _match.HomeScore == _match.AwayScore;
            var lost = _match.HomeScore < _match.AwayScore;
            bool forfeitedEarly = _match.SecondsRemaining > 0 && _match.HomeShots == 0 && _match.AwayShots == 0;

            if (won) PlayerPrefs.SetInt("diceSoccer.stats.wins", PlayerPrefs.GetInt("diceSoccer.stats.wins", 0) + 1);
            else if (drew) PlayerPrefs.SetInt("diceSoccer.stats.draws", PlayerPrefs.GetInt("diceSoccer.stats.draws", 0) + 1);
            else if (lost) PlayerPrefs.SetInt("diceSoccer.stats.losses", PlayerPrefs.GetInt("diceSoccer.stats.losses", 0) + 1);

            if (!forfeitedEarly && _currentFixture != null && !string.IsNullOrEmpty(_currentFixture.teamName) &&
                _currentFixture.teamName.IndexOf("Chancy", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                PlayerPrefs.SetInt("diceSoccer.stats.survivedChancy", 1);
            }

            _lastPayout = _competitionName == "League"
                ? won ? _currentFixture.cashWin
                    : drew ? _currentFixture.cashDraw
                    : _currentFixture.cashLose
                : 0;

            // Check Entourage bonuses (accumulate into _lastPayout first to avoid double-counting):
            if (_equippedEntourageIds.Contains(11) && won) // Sheikh Abdullah: +10,000 cash for victory
            {
                _lastPayout += 10000;
            }
            if (_equippedEntourageIds.Contains(12) && drew) // Ronin Chelskovic: Draw awards win money
            {
                var extra = _currentFixture.cashWin - _currentFixture.cashDraw;
                _lastPayout += extra;
            }
            if (_equippedEntourageIds.Contains(13)) // Sylvester Punter: +15k on win, -15k if you don't win or abandon
            {
                _lastPayout += won ? 15000 : -15000;
            }
            if (_equippedEntourageIds.Contains(14)) // Hilly Will: +50k on win, -50k if you don't win or abandon
            {
                _lastPayout += won ? 50000 : -50000;
            }

            _careerCash = Mathf.Max(0, _careerCash + _lastPayout);
            if (_competitionName == "League")
            {
                int matchStars = Mathf.Clamp(_match.CalculateStars(), 0, 3);
                string starKey = $"diceSoccer.season.{_seasonIndex}.match.{_selectedFixtureIndex}.stars";
                int prevStars = PlayerPrefs.GetInt(starKey, 0);
                if (matchStars > prevStars)
                    PlayerPrefs.SetInt(starKey, matchStars);
                PlayerPrefs.SetString($"diceSoccer.season.{_seasonIndex}.match.{_selectedFixtureIndex}.score",
                    $"{_match.HomeScore} - {_match.AwayScore}");
                if (won)
                    PlayerPrefs.SetInt($"diceSoccer.season.{_seasonIndex}.League.won.{_selectedFixtureIndex}", 1);

                if (_selectedFixtureIndex == _fixtureIndex)
                    _fixtureIndex++;
                if (_seasonIndex == 0)
                    PlayerPrefs.SetInt("diceSoccer.fixture", _fixtureIndex);
            }
            else
            {
                PlayerPrefs.SetInt(TournamentResultKey(_competitionName, _selectedFixtureIndex),
                    won ? 1 : -1);
                if (won && _selectedFixtureIndex == _fixtureIndex)
                    _fixtureIndex++;
                if (won && _seasonFixtures != null && _selectedFixtureIndex >= _seasonFixtures.Length - 1)
                    PlayerPrefs.SetInt($"diceSoccer.season.{_seasonIndex}.{_competitionName}.trophyWon", 1);
            }
            PlayerPrefs.SetInt(CompetitionProgressKey(_competitionName), _fixtureIndex);
            SaveCareerState();
            EvaluateCareerProgressAndAchievements(won, drew, false, true, !forfeitedEarly);
        }

        private string SeasonEndButtonLabel()
        {
            if (_catalog?.Data?.seasons == null) return "BACK TO FIXTURES";
            int earnedStars = GetEarnedStarsForSeason(_seasonIndex);
            if (_seasonIndex < _catalog.Data.seasons.Length - 1 && MatchSimulation.CanPromoteToSeason(_seasonIndex, earnedStars))
            {
                var nextSeason = _catalog.Data.seasons[_seasonIndex + 1];
                return $"UNLOCK {nextSeason.name.ToUpperInvariant()}";
            }
            return "BACK TO FIXTURES";
        }

        private void AdvanceCareer()
        {
            if (_catalog?.Data?.seasons == null)
            {
                ShowLeagueFixtures();
                return;
            }

            int earnedStars = GetEarnedStarsForSeason(_seasonIndex);
            if (!MatchSimulation.CanPromoteToSeason(_seasonIndex, earnedStars))
            {
                ShowLeagueFixtures();
                return;
            }

            if (_seasonIndex < _catalog.Data.seasons.Length - 1)
            {
                _seasonIndex++;
                _maxUnlockedSeason = Mathf.Max(_maxUnlockedSeason, _seasonIndex);
                _selectedLeagueSeasonIndex = _seasonIndex;
                _competitionName = "League";
                _seasonFixtures = _catalog.GetFixtures(_seasonIndex, _competitionName);
                _fixtureIndex = LoadCompetitionProgress(_competitionName);
                _selectedFixtureIndex = _fixtureIndex;

                PlayerPrefs.SetInt("diceSoccer.season", _seasonIndex);
                PlayerPrefs.SetInt($"diceSoccer.profile.{_currentProfileSlot}.season", _seasonIndex);
                PlayerPrefs.SetInt("diceSoccer.maxSeason", _maxUnlockedSeason);
                PlayerPrefs.SetInt($"diceSoccer.profile.{_currentProfileSlot}.maxSeason", _maxUnlockedSeason);
                SaveCareerState();
                EvaluateCareerProgressAndAchievements(false, false, false, false, false);

                ShowSeasonPromotedModal(_seasonIndex);
            }
            else
            {
                ShowLeagueSelect();
            }
        }

        private void LoadCareerProgress()
        {
            _catalog ??= GameCatalog.Load();
            _matchSpeed = PlayerPrefs.GetInt("diceSoccer.fastSpeed", 0) == 1 ? 2f : 1f;
            int maxSeasonCap = (_catalog?.Data?.seasons != null && _catalog.Data.seasons.Length > 0)
                ? _catalog.Data.seasons.Length - 1 : 4;

            _currentProfileSlot = Mathf.Clamp(PlayerPrefs.GetInt("diceSoccer.profile.active", 0), 0, 2);
            _teamName = PlayerPrefs.GetString($"diceSoccer.profile.{_currentProfileSlot}.name",
                PlayerPrefs.GetString("diceSoccer.teamName", "SANUKU"));
            _seasonIndex = Mathf.Clamp(PlayerPrefs.GetInt($"diceSoccer.profile.{_currentProfileSlot}.season", PlayerPrefs.GetInt("diceSoccer.season", 0)), 0, maxSeasonCap);
            _maxUnlockedSeason = Mathf.Clamp(
                Mathf.Max(_seasonIndex, PlayerPrefs.GetInt($"diceSoccer.profile.{_currentProfileSlot}.maxSeason", PlayerPrefs.GetInt("diceSoccer.maxSeason", _seasonIndex))),
                0, maxSeasonCap);
            _selectedLeagueSeasonIndex = _seasonIndex;
            _fixtureIndex = Mathf.Max(0, PlayerPrefs.GetInt($"diceSoccer.profile.{_currentProfileSlot}.fixture", PlayerPrefs.GetInt("diceSoccer.fixture", 0)));
            _careerCash = Mathf.Max(0, PlayerPrefs.GetInt($"diceSoccer.profile.{_currentProfileSlot}.cash", PlayerPrefs.GetInt("diceSoccer.cash", 1000)));
            _currentShirtId = Mathf.Max(1, PlayerPrefs.GetInt("diceSoccer.currentShirt", 1));
            _currentPantsId = Mathf.Max(1, PlayerPrefs.GetInt("diceSoccer.currentPants", 1));
            _currentCrestId = Mathf.Clamp(PlayerPrefs.GetInt($"diceSoccer.profile.{_currentProfileSlot}.crest", PlayerPrefs.GetInt("diceSoccer.currentCrest", 1)), 1, 3);
            _currentPatternStyle = Mathf.Clamp(PlayerPrefs.GetInt("diceSoccer.currentPatternStyle", 0), 0, 7);
            _currentShirtColorId = Mathf.Max(1, PlayerPrefs.GetInt("diceSoccer.currentShirtColor", 1));
            _currentPatternColorId = Mathf.Max(1, PlayerPrefs.GetInt("diceSoccer.currentPatternColor", 10));
            _currentSleeveColorId = Mathf.Max(1, PlayerPrefs.GetInt("diceSoccer.currentSleeveColor", 1));

            _unlockedShirtIds = LoadIntSet("diceSoccer.unlockedShirts", new[] { 1 });
            _unlockedPantsIds = LoadIntSet("diceSoccer.unlockedPants", new[] { 1 });
            _unlockedAchievementIds = LoadIntSet("diceSoccer.achievements", Array.Empty<int>());
            _unlockedEntourageIds = LoadIntSet("diceSoccer.unlockedEntourage", Array.Empty<int>());
            _equippedEntourageIds = new List<int>(LoadIntSet("diceSoccer.equippedEntourage", Array.Empty<int>()));
        }

        private static HashSet<int> LoadIntSet(string key, int[] defaultValues)
        {
            var raw = PlayerPrefs.GetString(key, null);
            if (string.IsNullOrEmpty(raw))
                return new HashSet<int>(defaultValues ?? Array.Empty<int>());
            var set = new HashSet<int>();
            foreach (var part in raw.Split(','))
            {
                if (int.TryParse(part.Trim(), out var val))
                    set.Add(val);
            }
            if (set.Count == 0 && defaultValues != null)
            {
                foreach (var v in defaultValues) set.Add(v);
            }
            return set;
        }

        private void SaveCareerState()
        {
            _maxUnlockedSeason = Mathf.Max(_maxUnlockedSeason, _seasonIndex);

            PlayerPrefs.SetInt("diceSoccer.profile.active", _currentProfileSlot);
            PlayerPrefs.SetInt($"diceSoccer.profile.{_currentProfileSlot}.exists", 1);
            PlayerPrefs.SetString($"diceSoccer.profile.{_currentProfileSlot}.name", _teamName);
            PlayerPrefs.SetInt($"diceSoccer.profile.{_currentProfileSlot}.season", _seasonIndex);
            PlayerPrefs.SetInt($"diceSoccer.profile.{_currentProfileSlot}.maxSeason", _maxUnlockedSeason);
            PlayerPrefs.SetInt($"diceSoccer.profile.{_currentProfileSlot}.fixture", _fixtureIndex);
            PlayerPrefs.SetInt($"diceSoccer.profile.{_currentProfileSlot}.cash", _careerCash);
            PlayerPrefs.SetInt($"diceSoccer.profile.{_currentProfileSlot}.crest", _currentCrestId);

            // Maintain standard keys
            PlayerPrefs.SetString("diceSoccer.teamName", _teamName);
            PlayerPrefs.SetInt("diceSoccer.season", _seasonIndex);
            PlayerPrefs.SetInt("diceSoccer.maxSeason", _maxUnlockedSeason);
            PlayerPrefs.SetInt("diceSoccer.fixture", _fixtureIndex);
            PlayerPrefs.SetInt("diceSoccer.cash", _careerCash);
            PlayerPrefs.SetInt("diceSoccer.currentShirt", _currentShirtId);
            PlayerPrefs.SetInt("diceSoccer.currentPants", _currentPantsId);
            PlayerPrefs.SetInt("diceSoccer.currentCrest", _currentCrestId);
            PlayerPrefs.SetInt("diceSoccer.currentPatternStyle", _currentPatternStyle);
            PlayerPrefs.SetInt("diceSoccer.currentShirtColor", _currentShirtColorId);
            PlayerPrefs.SetInt("diceSoccer.currentPatternColor", _currentPatternColorId);
            PlayerPrefs.SetInt("diceSoccer.currentSleeveColor", _currentSleeveColorId);
            PlayerPrefs.SetString("diceSoccer.unlockedShirts", string.Join(",", _unlockedShirtIds));
            PlayerPrefs.SetString("diceSoccer.unlockedPants", string.Join(",", _unlockedPantsIds));
            PlayerPrefs.SetString("diceSoccer.achievements", string.Join(",", _unlockedAchievementIds));
            PlayerPrefs.SetString("diceSoccer.unlockedEntourage", string.Join(",", _unlockedEntourageIds));
            PlayerPrefs.SetString("diceSoccer.equippedEntourage", string.Join(",", _equippedEntourageIds));
            PlayerPrefs.Save();
        }

        private void CheckAchievements(bool won, bool drew)
        {
            EvaluateCareerProgressAndAchievements(won, drew, false, false, _match != null && !_isLocalMultiplayer);
        }

        private int GetSeasonCompletedCount(int seasonIdx, string compName)
        {
            var fixtures = _catalog?.GetFixtures(seasonIdx, compName);
            if (fixtures == null || fixtures.Length == 0) return 0;
            int val = PlayerPrefs.GetInt($"diceSoccer.season.{seasonIdx}.{compName}.fixture", -1);
            if (val < 0 && seasonIdx == 0 && compName == "League")
                val = PlayerPrefs.GetInt("diceSoccer.fixture", 0);
            return Mathf.Clamp(val < 0 ? 0 : val, 0, fixtures.Length);
        }

        private bool IsSeasonLeagueAllWon(int seasonIdx)
        {
            var fixtures = _catalog?.GetFixtures(seasonIdx, "League");
            if (fixtures == null || fixtures.Length == 0) return false;
            if (GetSeasonCompletedCount(seasonIdx, "League") < fixtures.Length) return false;
            for (int i = 0; i < fixtures.Length; i++)
            {
                if (PlayerPrefs.GetInt($"diceSoccer.season.{seasonIdx}.League.won.{i}", 0) != 1)
                    return false;
            }
            return true;
        }

        private bool IsTournamentMatchWon(int seasonIdx, string compName, int fixtureIndex)
        {
            return PlayerPrefs.GetInt($"diceSoccer.season.{seasonIdx}.{compName}.result.{fixtureIndex}", 0) == 1;
        }

        private bool DidWinAnyTournamentMatch(int seasonIdx, string compName)
        {
            var fixtures = _catalog?.GetFixtures(seasonIdx, compName);
            if (fixtures == null) return false;
            for (int i = 0; i < fixtures.Length; i++)
            {
                if (IsTournamentMatchWon(seasonIdx, compName, i)) return true;
            }
            return false;
        }

        private bool IsTournamentWon(int seasonIdx, string compName)
        {
            if (PlayerPrefs.GetInt($"diceSoccer.season.{seasonIdx}.{compName}.trophyWon", 0) == 1)
                return true;
            var fixtures = _catalog?.GetFixtures(seasonIdx, compName);
            return fixtures != null && fixtures.Length > 0 && IsTournamentMatchWon(seasonIdx, compName, fixtures.Length - 1);
        }

        private int GetTotalEarnedStarsAllSeasons()
        {
            int total = 0;
            int count = (_catalog?.Data?.seasons != null) ? _catalog.Data.seasons.Length : 5;
            for (int s = 0; s < count; s++)
                total += GetEarnedStarsForSeason(s);
            return total;
        }

        private bool HasEquippedWag()
        {
            if (_equippedEntourageIds == null || _equippedEntourageIds.Count == 0 || _catalog == null) return false;
            foreach (int eid in _equippedEntourageIds)
            {
                var ent = _catalog.GetEntourage(eid);
                if (ent != null && ent.position == "WAG") return true;
            }
            return false;
        }

        private void EvaluateCareerProgressAndAchievements(bool won, bool drew, bool isLocalMp = false, bool enqueueModals = false, bool isCareerMatchEnd = false)
        {
            _catalog ??= GameCatalog.Load();
            int wins = PlayerPrefs.GetInt("diceSoccer.stats.wins", 0);
            int draws = PlayerPrefs.GetInt("diceSoccer.stats.draws", 0);
            int losses = PlayerPrefs.GetInt("diceSoccer.stats.losses", 0);
            int mpMatches = PlayerPrefs.GetInt("diceSoccer.stats.mpMatches", 0);
            int mpWins = PlayerPrefs.GetInt("diceSoccer.stats.mpWins", 0);
            int cashSpent = PlayerPrefs.GetInt("diceSoccer.stats.cashSpent", 0);
            int totalStars = GetTotalEarnedStarsAllSeasons();
            bool hasMatchScores = !isLocalMp && isCareerMatchEnd && _match != null;
            int homeScore = hasMatchScores ? _match.HomeScore : -1;
            int awayScore = hasMatchScores ? _match.AwayScore : -1;

            // 1. Evaluate all unlockable players (#5..#96) in ascending ID order (tier unlockers #27/#46/#64/#72 check lower IDs already evaluated)
            if (isCareerMatchEnd && _seasonIndex == 0 && _selectedFixtureIndex == 0 && won && PlayerPrefs.GetInt("diceSoccer.unlockedPlayer.5", 0) == 0)
            {
                PlayerPrefs.SetInt("diceSoccer.unlockedPlayer.5", 1);
                if (enqueueModals && !_pendingUnlockedPlayerIds.Contains(5)) _pendingUnlockedPlayerIds.Add(5);
            }

            int[] unlockableIds =
            {
                8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32,
                33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57,
                58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82,
                83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96
            };

            bool anyPlayerChanged = false;
            for (int idx = 0; idx < unlockableIds.Length; idx++)
            {
                int pid = unlockableIds[idx];
                if (PlayerPrefs.GetInt($"diceSoccer.unlockedPlayer.{pid}", 0) == 1) continue;

                bool cond = pid switch
                {
                    8 => GetSeasonCompletedCount(0, "League") >= 3,
                    9 => hasMatchScores && awayScore == 0,
                    10 => GetSeasonCompletedCount(1, "League") >= 6,
                    11 => _careerCash >= 10000,
                    12 => PlayerPrefs.GetInt("diceSoccer.stats.visitedNews", 0) == 1,
                    13 => GetSeasonCompletedCount(1, "T1") >= 4,
                    14 => GetSeasonCompletedCount(1, "League") >= 1,
                    15 => GetSeasonCompletedCount(0, "League") >= 2,
                    16 => IsSeasonLeagueAllWon(0),
                    17 => GetSeasonCompletedCount(1, "T2") >= 2,
                    18 => GetSeasonCompletedCount(1, "League") >= 9,
                    19 => GetSeasonCompletedCount(1, "T1") >= 2,
                    20 => GetSeasonCompletedCount(1, "T2") >= 4,
                    21 => !isLocalMp && _matchMaxHomeRoll >= 20,
                    22 => !isLocalMp && _matchHadZeroHomeRoll,
                    23 => wins >= 10,
                    24 => hasMatchScores && homeScore >= 3,
                    25 => !isLocalMp && _matchMaxHomeRoll >= 50,
                    26 => wins >= 20,
                    27 => AreAllPlayersInRangeUnlocked(1, 16),
                    28 => PlayerPrefs.GetInt("diceSoccer.stats.survivedChancy", 0) == 1,
                    29 => DidWinAnyTournamentMatch(2, "T2"),
                    30 => IsSeasonLeagueAllWon(1),
                    31 => draws >= 10,
                    32 => IsTournamentWon(1, "T1"),
                    33 => !isLocalMp && _matchHadZeroAwayRoll,
                    34 => !isLocalMp && _matchMaxHomeRoll >= 40,
                    35 => !isLocalMp && _matchMaxHomeRoll >= 30,
                    36 => GetSeasonCompletedCount(2, "League") >= 4,
                    37 => wins >= 30,
                    38 => hasMatchScores && awayScore >= 3,
                    39 => GetSeasonCompletedCount(2, "League") >= 12,
                    40 => DidWinAnyTournamentMatch(2, "T1"),
                    41 => _maxUnlockedSeason >= 2,
                    42 => IsTournamentWon(1, "T2"),
                    43 => IsTournamentWon(2, "T1"),
                    44 => GetSeasonCompletedCount(2, "T1") >= 3,
                    45 => GetSeasonCompletedCount(2, "T1") >= 5,
                    46 => AreAllPlayersInRangeUnlocked(17, 32),
                    47 => hasMatchScores && homeScore >= 7,
                    48 => totalStars >= 119,
                    49 => GetSeasonCompletedCount(3, "League") >= 4,
                    50 => totalStars >= 118,
                    51 => losses >= 10,
                    52 => wins >= 40,
                    53 => IsTournamentWon(2, "T2"),
                    54 => _careerCash >= 2000000,
                    55 => GetSeasonCompletedCount(2, "T2") >= 3,
                    56 => _careerCash >= 5000000,
                    57 => DidWinAnyTournamentMatch(3, "T2"),
                    58 => GetSeasonCompletedCount(2, "T2") >= 5,
                    59 => cashSpent >= 10000000,
                    60 => !isLocalMp && _matchMaxHomeRoll >= 60,
                    61 => GetSeasonCompletedCount(3, "League") >= 8,
                    62 => _maxUnlockedSeason >= 3,
                    63 => hasMatchScores && homeScore >= 5,
                    64 => AreAllPlayersInRangeUnlocked(33, 48),
                    65 => wins >= 50,
                    66 => !isLocalMp && _matchMaxHomeRoll >= 70,
                    67 => !isLocalMp && _matchMaxHomeRoll >= 100,
                    68 => !isLocalMp && _matchMaxHomeRoll >= 80,
                    69 => IsTournamentWon(1, "T1") && IsTournamentWon(1, "T2") && IsTournamentWon(2, "T1") && IsTournamentWon(2, "T2") && IsTournamentWon(3, "T1") && IsTournamentWon(3, "T2"),
                    70 => IsTournamentWon(3, "T1"),
                    71 => DidWinAnyTournamentMatch(3, "T1"),
                    72 => AreAllPlayersInRangeUnlocked(49, 64),
                    73 => _maxUnlockedSeason >= 4,
                    74 => !isLocalMp && _matchMaxHomeRoll >= 90,
                    75 => GetSeasonCompletedCount(3, "T1") >= 3,
                    76 => IsTournamentWon(3, "T2"),
                    77 => totalStars >= 120,
                    78 => IsSeasonLeagueAllWon(2),
                    79 => GetSeasonCompletedCount(3, "T2") >= 3,
                    80 => IsSeasonLeagueAllWon(3),
                    81 => !isLocalMp && _matchLastMinConcede,
                    82 => IsTournamentMatchWon(4, "T1", 2),
                    83 => GetSeasonCompletedCount(4, "League") >= 3,
                    84 => GetSeasonCompletedCount(4, "League") >= 7,
                    85 => !isLocalMp && _matchLastMinMiss,
                    86 => !isLocalMp && _matchLastMinGoal,
                    87 => mpWins >= 2,
                    88 => GetSeasonCompletedCount(4, "League") >= 11,
                    89 => GetSeasonCompletedCount(4, "T1") >= 1,
                    90 => mpMatches >= 1,
                    91 => GetSeasonCompletedCount(4, "T2") >= 2,
                    92 => IsTournamentMatchWon(4, "T2", 3),
                    93 => IsSeasonLeagueAllWon(4),
                    94 => GetSeasonCompletedCount(4, "League") >= 14 && GetEarnedStarsForSeason(4) >= 30,
                    95 => IsTournamentWon(4, "T1"),
                    96 => IsTournamentWon(4, "T2"),
                    _ => false
                };

                if (cond)
                {
                    PlayerPrefs.SetInt($"diceSoccer.unlockedPlayer.{pid}", 1);
                    anyPlayerChanged = true;
                    if (enqueueModals && !_pendingUnlockedPlayerIds.Contains(pid))
                        _pendingUnlockedPlayerIds.Add(pid);
                }
            }

            // 2. Standalone Entourage milestone unlocks (#15, #16, #20)
            bool entourageAdded = false;
            if (GetSeasonCompletedCount(4, "League") >= 11 && _unlockedEntourageIds.Add(15)) entourageAdded = true;
            if (IsTournamentMatchWon(4, "T1", 2) && _unlockedEntourageIds.Add(16)) entourageAdded = true;
            if (GetSeasonCompletedCount(4, "League") >= 14 && _unlockedEntourageIds.Add(20)) entourageAdded = true;

            // 3. Evaluate all 72 Achievements
            var newAchievements = new List<AchievementData>();
            for (int aid = 1; aid <= 72; aid++)
            {
                if (_unlockedAchievementIds.Contains(aid)) continue;
                bool cond = aid switch
                {
                    1 => GetSeasonCompletedCount(0, "League") >= 3,
                    2 => IsSeasonLeagueAllWon(0),
                    3 => _maxUnlockedSeason >= 2,
                    4 => IsSeasonLeagueAllWon(1),
                    5 => IsTournamentWon(1, "T1"),
                    6 => IsTournamentWon(1, "T2"),
                    7 => _maxUnlockedSeason >= 3,
                    8 => IsSeasonLeagueAllWon(2),
                    9 => IsTournamentWon(2, "T1"),
                    10 => IsTournamentWon(2, "T2"),
                    11 => _maxUnlockedSeason >= 4,
                    12 => IsSeasonLeagueAllWon(3),
                    13 => IsTournamentWon(3, "T1"),
                    14 => IsTournamentWon(3, "T2"),
                    15 => IsSeasonLeagueAllWon(1) && IsSeasonLeagueAllWon(2) && IsSeasonLeagueAllWon(3),
                    16 => IsTournamentWon(1, "T1") && IsTournamentWon(1, "T2") && IsTournamentWon(2, "T1") && IsTournamentWon(2, "T2") && IsTournamentWon(3, "T1") && IsTournamentWon(3, "T2"),
                    17 => wins >= 10,
                    18 => wins >= 20,
                    19 => wins >= 30,
                    20 => wins >= 40,
                    21 => wins >= 50,
                    22 => draws >= 10,
                    23 => losses >= 10,
                    24 => CountUnlockedPlayersByPosition("GK") >= 10,
                    25 => CountUnlockedPlayersByPosition("FWD") >= 30,
                    26 => CountUnlockedPlayersByPosition("MID") >= 10,
                    27 => CountUnlockedPlayersByPosition("DEF") >= 30,
                    28 => AreAllPlayersInRangeUnlocked(1, 16),
                    29 => AreAllPlayersInRangeUnlocked(17, 32),
                    30 => AreAllPlayersInRangeUnlocked(33, 48),
                    31 => AreAllPlayersInRangeUnlocked(49, 64),
                    32 => AreAllPlayersInRangeUnlocked(65, 80),
                    33 => PlayerPrefs.GetInt("diceSoccer.stats.visitedCredits", 0) == 1,
                    34 => _careerCash >= 10000,
                    35 => _careerCash >= 2000000,
                    36 => _careerCash >= 5000000,
                    37 => cashSpent >= 10000000,
                    38 => hasMatchScores && awayScore == 0,
                    39 => hasMatchScores && awayScore >= 3,
                    40 => hasMatchScores && homeScore >= 3,
                    41 => hasMatchScores && homeScore >= 5,
                    42 => hasMatchScores && homeScore >= 4,
                    43 => hasMatchScores && homeScore >= 6,
                    44 => !isLocalMp && _matchMaxHomeRoll >= 20,
                    45 => !isLocalMp && _matchMaxHomeRoll >= 30,
                    46 => !isLocalMp && _matchMaxHomeRoll >= 40,
                    47 => !isLocalMp && _matchMaxHomeRoll >= 50,
                    48 => !isLocalMp && _matchMaxHomeRoll >= 60,
                    49 => !isLocalMp && _matchMaxHomeRoll >= 70,
                    50 => !isLocalMp && _matchMaxHomeRoll >= 80,
                    51 => !isLocalMp && _matchMaxHomeRoll >= 90,
                    52 => !isLocalMp && _matchMaxHomeRoll >= 100,
                    53 => !isLocalMp && _matchHadZeroHomeRoll,
                    54 => !isLocalMp && _matchHadZeroAwayRoll,
                    55 => !isLocalMp && _seasonIndex > 0 && _matchLastMinGoal,
                    56 => !isLocalMp && _seasonIndex > 0 && _matchLastMinMiss,
                    57 => !isLocalMp && _seasonIndex > 0 && _matchLastMinSave,
                    58 => !isLocalMp && _seasonIndex > 0 && _matchLastMinConcede,
                    59 => !isLocalMp && _seasonIndex > 0 && _matchLastMinEqualizer,
                    60 => !isLocalMp && _seasonIndex > 0 && _matchLastMinTieBreaker,
                    61 => GetSeasonCompletedCount(4, "League") >= 14 && GetEarnedStarsForSeason(4) >= 30,
                    62 => IsSeasonLeagueAllWon(4),
                    63 => IsTournamentWon(4, "T1"),
                    64 => IsTournamentWon(4, "T2"),
                    65 => isCareerMatchEnd && won && HasEquippedWag(),
                    66 => !isLocalMp && _matchMaxHomeRoll >= 150,
                    67 => isCareerMatchEnd && won && _seasonIndex == 4 && CountUnusedBenchPlayers() == 0,
                    68 => isCareerMatchEnd && won && _seasonIndex == 4 && _equippedEntourageIds.Count == 0,
                    69 => AreAllPlayersInRangeUnlocked(81, 96),
                    70 => _unlockedEntourageIds.Count >= 21,
                    71 => mpMatches >= 1,
                    72 => mpWins >= 2,
                    _ => false
                };

                if (cond)
                {
                    var ach = _catalog.GetAchievement(aid);
                    if (ach != null) newAchievements.Add(ach);
                }
            }

            foreach (var ach in newAchievements)
            {
                _unlockedAchievementIds.Add(ach.id);
                if (ach.goldReward > 0) _careerCash += ach.goldReward;
                if (ach.entourageReward > 0) _unlockedEntourageIds.Add(ach.entourageReward);
                if (ach.pantsReward > 0) _unlockedPantsIds.Add(ach.pantsReward);
                if (enqueueModals) _pendingUnlockedAchievements.Add(ach);
            }

            // Re-check bankroll/all-entourage achievements if a gold/entourage reward just satisfied them
            if (newAchievements.Count > 0)
            {
                if (!_unlockedAchievementIds.Contains(34) && _careerCash >= 10000) GrantBonusAchievement(34, enqueueModals);
                if (!_unlockedAchievementIds.Contains(35) && _careerCash >= 2000000) GrantBonusAchievement(35, enqueueModals);
                if (!_unlockedAchievementIds.Contains(36) && _careerCash >= 5000000) GrantBonusAchievement(36, enqueueModals);
                if (!_unlockedAchievementIds.Contains(70) && _unlockedEntourageIds.Count >= 21) GrantBonusAchievement(70, enqueueModals);
            }

            if (newAchievements.Count > 0 || anyPlayerChanged || entourageAdded)
            {
                SaveCareerState();
            }
        }

        private void GrantBonusAchievement(int aid, bool enqueueModals)
        {
            var ach = _catalog?.GetAchievement(aid);
            if (ach == null || _unlockedAchievementIds.Contains(aid)) return;
            _unlockedAchievementIds.Add(aid);
            if (ach.goldReward > 0) _careerCash += ach.goldReward;
            if (ach.entourageReward > 0) _unlockedEntourageIds.Add(ach.entourageReward);
            if (ach.pantsReward > 0) _unlockedPantsIds.Add(ach.pantsReward);
            if (enqueueModals) _pendingUnlockedAchievements.Add(ach);
        }

        private bool AreAllPlayersInRangeUnlocked(int minId, int maxId)
        {
            if (_catalog?.Data?.players == null) return false;
            int found = 0;
            foreach (var p in _catalog.Data.players)
            {
                if (p == null || p.id < minId || p.id > maxId) continue;
                found++;
                if (!IsPlayerUnlocked(p)) return false;
            }
            return found > 0;
        }

        private int CountUnlockedPlayersByPosition(string pos)
        {
            if (_catalog?.Data?.players == null) return 0;
            int count = 0;
            foreach (var p in _catalog.Data.players)
            {
                if (p != null && p.id >= 1 && p.id <= 96 && p.position == pos && IsPlayerUnlocked(p))
                    count++;
            }
            return count;
        }

        private void ShowAchievementUnlockedModal(AchievementData ach, Action onDone = null)
        {
            var container = _overlayContainer != null ? _overlayContainer : (_safeArea != null ? _safeArea : _canvas.transform);
            var modal = new GameObject("AchievementUnlockedModal", typeof(RectTransform), typeof(Image));
            modal.transform.SetParent(container, false);
            var modalRect = modal.GetComponent<RectTransform>();
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modal.GetComponent<Image>().color = new Color(0, 0, 0, 0.80f);

            var box = AddResourceImage("Box", "Original/sprites/AchievementsSS4/LongDialogBox", modal.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            box.rectTransform.sizeDelta = new Vector2(420f, 520f);
            box.preserveAspect = false;

            var trophyId = Mathf.Clamp(ach.id, 1, 72);
            var trophy = AddResourceImage("Trophy", $"Original/images/Trophy{trophyId:00}", box.transform,
                new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f));
            trophy.rectTransform.sizeDelta = new Vector2(150f, 150f);
            trophy.preserveAspect = true;

            var title = AddText("ACHIEVEMENT UNLOCKED!", box.transform, 21, Ink,
                new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.89f));
            title.fontStyle = FontStyle.Bold;

            var desc = string.IsNullOrEmpty(ach.unlockedDescription) ? ach.lockedDescription : ach.unlockedDescription;
            AddText(desc, box.transform, 15, Ink,
                new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.44f));

            var rewardText = "";
            if (ach.goldReward > 0) rewardText += $"+{ach.goldReward:N0} Cash  ";
            if (ach.entourageReward > 0) rewardText += $"Unlocked Entourage #{ach.entourageReward}  ";
            if (ach.pantsReward > 0) rewardText += $"Unlocked Pants #{ach.pantsReward}";
            if (!string.IsNullOrEmpty(rewardText))
            {
                AddText($"Reward: {rewardText}", box.transform, 14, new Color32(42, 138, 36, 255),
                    new Vector2(0.08f, 0.21f), new Vector2(0.92f, 0.29f)).fontStyle = FontStyle.Bold;
            }

            AddSpriteButton("DoneBtn", "Original/images/DoneButton", "Original/images/DoneButtonPressed", box.transform,
                new Vector2(0.5f, 0.09f), new Vector2(0.48f, 0.10f), () =>
                {
                    Destroy(modal);
                    onDone?.Invoke();
                });
        }

        private Image[] CreateDiceRowBelt(Transform parent, bool home)
        {
            var dice = new Image[3];
            var xs = home ? new[] { 0.16f, 0.27f, 0.38f } : new[] { 0.62f, 0.73f, 0.84f };
            const float y = 0.162f;

            for (var i = 0; i < 3; i++)
            {
                var background = AddPanel($"{(home ? "Home" : "Away")}Die{i + 1}", parent,
                    Color.white, new Vector2(xs[i], y), new Vector2(0.095f, 0.055f));

                if (DiceBackgroundSprite == null)
                {
                    var texture = Resources.Load<Texture2D>("Original/sprites/Buff6/DiceBackground");
                    if (texture == null)
                        throw new FileNotFoundException(
                            "Required original image is missing: Resources/Original/sprites/Buff6/DiceBackground.png");
                    DiceBackgroundSprite = Sprite.Create(texture,
                        new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
                background.sprite = DiceBackgroundSprite;
                background.preserveAspect = true;

                var faceObject = new GameObject("Face", typeof(RectTransform), typeof(Image));
                faceObject.transform.SetParent(background.transform, false);
                var faceRect = faceObject.GetComponent<RectTransform>();
                faceRect.anchorMin = new Vector2(0.12f, 0.12f);
                faceRect.anchorMax = new Vector2(0.88f, 0.88f);
                faceRect.offsetMin = faceRect.offsetMax = Vector2.zero;
                dice[i] = faceObject.GetComponent<Image>();
                dice[i].preserveAspect = true;
                dice[i].enabled = false;
            }

            return dice;
        }

        private static void ShowFinalFaces(Image[] dice, int[] faces)
        {
            for (var i = 0; i < dice.Length; i++)
            {
                if (i < faces.Length)
                    SetDieFace(dice[i], faces[i]);
                else
                    dice[i].enabled = false;
            }
        }

        private static void SetDieFace(Image image, int face)
        {
            var sprite = GetDiceSprite(face);
            image.enabled = true;
            image.color = Color.white;
            image.sprite = sprite;
        }

        private static Sprite GetDiceSprite(int face)
        {
            if (face >= 101) face -= 100;
            if (face < 1) face = 1;

            if (DiceSprites.TryGetValue(face, out var sprite))
                return sprite;

            var path = $"Original/sprites/Buff6/DiceSide{face:00}";
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null)
            {
                int fallback = Mathf.Clamp(face, 1, 6);
                path = $"Original/sprites/Buff6/DiceSide{fallback:00}";
                texture = Resources.Load<Texture2D>(path);
            }
            if (texture == null)
                throw new FileNotFoundException($"Required original die face is missing: Resources/{path}.png");
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = texture.name;
            DiceSprites.Add(face, sprite);
            return sprite;
        }

        private static Vector2 ParseCocosPoint(string s)
        {
            if (string.IsNullOrEmpty(s)) return Vector2.zero;
            s = s.Trim('{', '}', ' ');
            var parts = s.Split(',');
            if (parts.Length == 2 &&
                float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
            {
                return new Vector2(x, y);
            }
            return Vector2.zero;
        }

        private void CreateSeasonFieldObjects(int seasonIndex, RectTransform pitchRoot, float pitchScale)
        {
            if (_catalog?.Data?.seasons == null || seasonIndex < 0 || seasonIndex >= _catalog.Data.seasons.Length)
                return;

            var season = _catalog.Data.seasons[seasonIndex];
            var objects = season.fieldObjects?.Normal;
            if (objects == null || objects.Length == 0)
                return;

            for (int i = 0; i < objects.Length; i++)
            {
                var pt = ParseCocosPoint(objects[i]);
                int objNum = i + 1;
                string spritePath = $"Original/sprites/Season{seasonIndex}Field1(1.1)/Object{objNum}";
                var sprite = GetUiSprite(spritePath);
                if (sprite == null)
                    continue;

                var go = new GameObject($"FieldObj_{seasonIndex}_{objNum}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(pitchRoot, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(pt.x * 2.0f * pitchScale, pt.y * 1.5f * pitchScale);
                rect.sizeDelta = new Vector2(sprite.rect.width * pitchScale, sprite.rect.height * pitchScale);
                var img = go.GetComponent<Image>();
                img.sprite = sprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }

            // For Season 3, also place authentic stadium banners along the upper sideline:
            if (seasonIndex == 3)
            {
                for (int b = 1; b <= 5; b++)
                {
                    string bPath = $"Original/sprites/Season3Field1(1.1)/Banner{b}";
                    var bSprite = GetUiSprite(bPath);
                    if (bSprite != null)
                    {
                        var bGo = new GameObject($"Banner_{b}", typeof(RectTransform), typeof(Image));
                        bGo.transform.SetParent(pitchRoot, false);
                        var bRect = bGo.GetComponent<RectTransform>();
                        bRect.anchorMin = bRect.anchorMax = new Vector2(0.5f, 0.5f);
                        bRect.pivot = new Vector2(0.5f, 0.5f);
                        float xPos = -600f + (b - 1) * 300f;
                        bRect.anchoredPosition = new Vector2(xPos * pitchScale, 360f * pitchScale);
                        bRect.sizeDelta = new Vector2(bSprite.rect.width * pitchScale, bSprite.rect.height * pitchScale);
                        var bImg = bGo.GetComponent<Image>();
                        bImg.sprite = bSprite;
                        bImg.preserveAspect = true;
                        bImg.raycastTarget = false;
                    }
                }
            }
        }

        private IEnumerator AnimateCloudsDrift(RectTransform cloudRect, float speed)
        {
            if (cloudRect == null) yield break;
            while (cloudRect != null && cloudRect.gameObject.activeInHierarchy)
            {
                var pos = cloudRect.anchoredPosition;
                pos.x += speed * Time.deltaTime;
                if (pos.x > 500f)
                {
                    pos.x = -500f;
                }
                cloudRect.anchoredPosition = pos;
                yield return null;
            }
        }

        private static float GetPitchOffsetForLocation(int location)
        {
            switch (location)
            {
                case -2: return 640f;
                case -1: return 375f;
                case 0:  return 0f;
                case 1:  return -375f;
                case 2:  return -640f;
                default: return 0f;
            }
        }

        private static (int[] homeSlots, int[] awaySlots) GetActiveDuelSlots(int location)
        {
            switch (location)
            {
                case -2: return (new[] { 0 }, new[] { 5 }); // Home GK vs Away CF
                case -1: return (new[] { 1, 2, 3 }, new[] { 4, 5, 6 }); // Home Def vs Away Att
                case 0:  return (new[] { 4, 5, 6 }, new[] { 4, 5, 6 }); // Home Mid vs Away Mid
                case 1:  return (new[] { 4, 5, 6 }, new[] { 1, 2, 3 }); // Home Att vs Away Def
                case 2:  return (new[] { 5 }, new[] { 0 }); // Home CF vs Away GK
                default: return (new[] { 4, 5, 6 }, new[] { 4, 5, 6 });
            }
        }

        private void CreateFieldDice(Transform parent)
        {
            _homeDice = new Image[3];
            _awayDice = new Image[3];
            _homeDieRoots = new GameObject[3];
            _awayDieRoots = new GameObject[3];
            _homeDieValueLabels = new Text[3];
            _awayDieValueLabels = new Text[3];
            _homeDieBuffBadges = new Image[3];
            _awayDieBuffBadges = new Image[3];
            _homeDieGlows = new Image[3];
            _awayDieGlows = new Image[3];

            float[] ys = { 0.25f, 0.50f, 0.75f };

            for (var i = 0; i < 3; i++)
            {
                int dieIdx = i;

                // Home Die Root (matching media_1792421110113_45d0305f.png):
                var hRoot = new GameObject($"HomeDie{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
                hRoot.transform.SetParent(parent, false);
                var hRect = hRoot.GetComponent<RectTransform>();
                hRect.anchorMin = hRect.anchorMax = new Vector2(0.27f, ys[i]);
                hRect.sizeDelta = new Vector2(92f, 92f);
                var hImg = hRoot.GetComponent<Image>();
                hImg.sprite = GetUiSprite("Original/sprites/Buff6/DiceBackground");
                hImg.preserveAspect = true;
                hImg.raycastTarget = true;
                var hBtn = hRoot.GetComponent<Button>();
                hBtn.transition = Selectable.Transition.None;
                hBtn.onClick.AddListener(() => OnFieldDieTapped(0, dieIdx));

                // Glow child
                var hGlowGo = new GameObject("Glow", typeof(RectTransform), typeof(Image));
                hGlowGo.transform.SetParent(hRoot.transform, false);
                var hGlowRect = hGlowGo.GetComponent<RectTransform>();
                hGlowRect.anchorMin = Vector2.zero;
                hGlowRect.anchorMax = Vector2.one;
                hGlowRect.offsetMin = new Vector2(-12f, -12f);
                hGlowRect.offsetMax = new Vector2(12f, 12f);
                var hGlowImg = hGlowGo.GetComponent<Image>();
                hGlowImg.sprite = GetUiSprite("Original/sprites/Buff6/DiceGlow");
                hGlowImg.preserveAspect = true;
                hGlowImg.raycastTarget = false;
                hGlowImg.enabled = false;
                _homeDieGlows[i] = hGlowImg;

                var hFaceGo = new GameObject("Face", typeof(RectTransform), typeof(Image));
                hFaceGo.transform.SetParent(hRoot.transform, false);
                var hFaceRect = hFaceGo.GetComponent<RectTransform>();
                hFaceRect.anchorMin = Vector2.zero;
                hFaceRect.anchorMax = Vector2.one;
                hFaceRect.offsetMin = hFaceRect.offsetMax = Vector2.zero;
                var hFaceImg = hFaceGo.GetComponent<Image>();
                hFaceImg.preserveAspect = true;
                hFaceImg.raycastTarget = false;
                hFaceImg.enabled = false;

                var hValLbl = AddText(string.Empty, hRoot.transform, 42, new Color32(34, 34, 34, 255),
                    new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));
                hValLbl.name = "ValueLabel";
                hValLbl.fontStyle = FontStyle.Bold;
                hValLbl.alignment = TextAnchor.MiddleCenter;
                hValLbl.raycastTarget = false;
                hValLbl.enabled = false;
                _homeDieValueLabels[i] = hValLbl;

                // Buff Badge child (top-right corner overlay)
                var hBadgeGo = new GameObject("BuffBadge", typeof(RectTransform), typeof(Image));
                hBadgeGo.transform.SetParent(hRoot.transform, false);
                var hBadgeRect = hBadgeGo.GetComponent<RectTransform>();
                hBadgeRect.anchorMin = hBadgeRect.anchorMax = new Vector2(0.88f, 0.88f);
                hBadgeRect.sizeDelta = new Vector2(38f, 38f);
                hBadgeRect.anchoredPosition = Vector2.zero;
                var hBadgeImg = hBadgeGo.GetComponent<Image>();
                hBadgeImg.preserveAspect = true;
                hBadgeImg.raycastTarget = false;
                hBadgeImg.enabled = false;
                _homeDieBuffBadges[i] = hBadgeImg;

                _homeDice[i] = hFaceImg;
                _homeDieRoots[i] = hRoot;
                hRoot.SetActive(false);

                // Away Die Root (matching media_1792421110113_45d0305f.png):
                var aRoot = new GameObject($"AwayDie{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
                aRoot.transform.SetParent(parent, false);
                var aRect = aRoot.GetComponent<RectTransform>();
                aRect.anchorMin = aRect.anchorMax = new Vector2(0.73f, ys[i]);
                aRect.sizeDelta = new Vector2(92f, 92f);
                var aImg = aRoot.GetComponent<Image>();
                aImg.sprite = GetUiSprite("Original/sprites/Buff6/DiceBackground");
                aImg.preserveAspect = true;
                aImg.raycastTarget = true;
                var aBtn = aRoot.GetComponent<Button>();
                aBtn.transition = Selectable.Transition.None;
                aBtn.onClick.AddListener(() => OnFieldDieTapped(1, dieIdx));

                // Glow child
                var aGlowGo = new GameObject("Glow", typeof(RectTransform), typeof(Image));
                aGlowGo.transform.SetParent(aRoot.transform, false);
                var aGlowRect = aGlowGo.GetComponent<RectTransform>();
                aGlowRect.anchorMin = Vector2.zero;
                aGlowRect.anchorMax = Vector2.one;
                aGlowRect.offsetMin = new Vector2(-12f, -12f);
                aGlowRect.offsetMax = new Vector2(12f, 12f);
                var aGlowImg = aGlowGo.GetComponent<Image>();
                aGlowImg.sprite = GetUiSprite("Original/sprites/Buff6/DiceGlow");
                aGlowImg.preserveAspect = true;
                aGlowImg.raycastTarget = false;
                aGlowImg.enabled = false;
                _awayDieGlows[i] = aGlowImg;

                var aFaceGo = new GameObject("Face", typeof(RectTransform), typeof(Image));
                aFaceGo.transform.SetParent(aRoot.transform, false);
                var aFaceRect = aFaceGo.GetComponent<RectTransform>();
                aFaceRect.anchorMin = Vector2.zero;
                aFaceRect.anchorMax = Vector2.one;
                aFaceRect.offsetMin = aFaceRect.offsetMax = Vector2.zero;
                var aFaceImg = aFaceGo.GetComponent<Image>();
                aFaceImg.preserveAspect = true;
                aFaceImg.raycastTarget = false;
                aFaceImg.enabled = false;

                var aValLbl = AddText(string.Empty, aRoot.transform, 42, new Color32(34, 34, 34, 255),
                    new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));
                aValLbl.name = "ValueLabel";
                aValLbl.fontStyle = FontStyle.Bold;
                aValLbl.alignment = TextAnchor.MiddleCenter;
                aValLbl.raycastTarget = false;
                aValLbl.enabled = false;
                _awayDieValueLabels[i] = aValLbl;

                // Buff Badge child (top-left corner overlay)
                var aBadgeGo = new GameObject("BuffBadge", typeof(RectTransform), typeof(Image));
                aBadgeGo.transform.SetParent(aRoot.transform, false);
                var aBadgeRect = aBadgeGo.GetComponent<RectTransform>();
                aBadgeRect.anchorMin = aBadgeRect.anchorMax = new Vector2(0.12f, 0.88f);
                aBadgeRect.sizeDelta = new Vector2(38f, 38f);
                aBadgeRect.anchoredPosition = Vector2.zero;
                var aBadgeImg = aBadgeGo.GetComponent<Image>();
                aBadgeImg.preserveAspect = true;
                aBadgeImg.raycastTarget = false;
                aBadgeImg.enabled = false;
                _awayDieBuffBadges[i] = aBadgeImg;

                _awayDice[i] = aFaceImg;
                _awayDieRoots[i] = aRoot;
                aRoot.SetActive(false);
            }
        }

        private void OnFieldDieTapped(int side, int index)
        {
            if (!_isAwaitingBuffSelection) return;
            if (_awaitingBuffSide >= 0 && side != _awaitingBuffSide) return;
            _selectedBuffDieSide = side;
            _selectedBuffDieIndex = index;
            AudioManager.Instance.PlayTap();
        }

        private static string GetTriggerBadgePath(int trigger)
        {
            if (trigger > 0)
            {
                var p1 = $"Original/sprites/BuffTrigger/Trigger{trigger}";
                if (TryGetUiSprite(p1) != null) return p1;
                var p2 = $"Original/sprites/BuffTrigger1/Trigger{trigger}";
                if (TryGetUiSprite(p2) != null) return p2;
            }
            return "Original/sprites/BuffTrigger/Trigger21";
        }

        private static Sprite GetAbilityMedalSprite(AbilityData ab)
        {
            if (ab == null)
                return GetUiSprite("Original/sprites/Buff6/DisplayBuffTimes");

            if (ab.id == 18)
                return GetUiSprite("Original/sprites/Buff6/DisplayBuffPlus");

            int spriteId = ResolveAbilitySpriteId(ab.id);

            var medalSprite = TryGetUiSprite($"Original/sprites/Buff6/BuffMedal{spriteId}");
            if (medalSprite != null) return medalSprite;

            var dispSprite = TryGetUiSprite($"Original/sprites/Buff6/DisplayBuff{spriteId}");
            if (dispSprite != null) return dispSprite;

            if (ab.type == 5) return GetUiSprite("Original/sprites/Buff6/DisplayBuffReroll");
            if (ab.type == 0 || ab.type == 4) return GetUiSprite("Original/sprites/Buff6/DisplayBuffMinus");
            return GetUiSprite("Original/sprites/Buff6/DisplayBuffTimes");
        }

        private IEnumerator SelectBuffTarget(int targetSide, int[] eligibleIndices, int defaultFallbackIndex, Action<int> onSelected)
        {
            _isAwaitingBuffSelection = true;
            _awaitingBuffSide = targetSide;
            _selectedBuffDieIndex = -1;
            _selectedBuffDieSide = -1;

            var glows = targetSide == 0 ? _homeDieGlows : _awayDieGlows;
            if (glows != null)
            {
                for (int i = 0; i < eligibleIndices.Length; i++)
                {
                    int idx = eligibleIndices[i];
                    if (idx >= 0 && idx < glows.Length && glows[idx] != null)
                        glows[idx].enabled = true;
                }
            }

            float elapsed = 0f;
            float maxWait = Application.isBatchMode ? 0.35f : 3600f;
            while (elapsed < maxWait && _selectedBuffDieIndex < 0)
            {
                elapsed += Time.deltaTime;
                float pulse = 0.5f + Mathf.PingPong(elapsed * 2.5f, 0.5f);
                if (glows != null)
                {
                    for (int i = 0; i < eligibleIndices.Length; i++)
                    {
                        int idx = eligibleIndices[i];
                        if (idx >= 0 && idx < glows.Length && glows[idx] != null)
                            glows[idx].color = new Color(1f, 1f, 1f, pulse);
                    }
                }
                yield return null;
            }

            if (glows != null)
            {
                for (int i = 0; i < glows.Length; i++)
                    if (glows[i] != null) glows[i].enabled = false;
            }

            _isAwaitingBuffSelection = false;
            _awaitingBuffSide = -1;

            int chosen = _selectedBuffDieIndex;
            if (chosen < 0 || Array.IndexOf(eligibleIndices, chosen) < 0)
            {
                // Auto-select fallback matching autoTargetBuff in Cocos2d binary
                chosen = (defaultFallbackIndex >= 0 && Array.IndexOf(eligibleIndices, defaultFallbackIndex) >= 0)
                    ? defaultFallbackIndex
                    : eligibleIndices[0];
            }

            onSelected?.Invoke(chosen);
        }

        private IEnumerator AnimateZoneTransition(int fromLoc, int toLoc, int oldPossession, int newPossession)
        {
            if (_homePlayers == null || _awayPlayers == null || _match == null) yield break;

            _inEncounterAnimation = true;
            if (_shakePrompt != null) _shakePrompt.gameObject.SetActive(false);

            float pitchScale = (_fieldPitchRoot != null && _fieldPitchRoot.localScale.y > 0f)
                ? _fieldPitchRoot.localScale.y : 1f;
            var startPitchX = GetPitchOffsetForLocation(fromLoc) * pitchScale;
            var targetPitchX = GetPitchOffsetForLocation(toLoc) * pitchScale;

            bool isHomeAdvancing = (toLoc > fromLoc) || (toLoc == fromLoc && newPossession == 0);
            var (fromHomeSlots, fromAwaySlots) = GetActiveDuelSlots(fromLoc);
            var (toHomeSlots, toAwaySlots) = GetActiveDuelSlots(toLoc);

            float[] ys = { 0.13f, 0.38f, 0.63f };

            var fromHomeSet = new HashSet<int>(fromHomeSlots);
            var toHomeSet = new HashSet<int>(toHomeSlots);
            var fromAwaySet = new HashSet<int>(fromAwaySlots);
            var toAwaySet = new HashSet<int>(toAwaySlots);

            var fadingPlayers = new List<Image>();
            var activeMovingPlayers = new List<(Image player, float startX, float targetX, float startY, float targetY, bool isHome)>();

            // Home leaving players (in previous zone, not in new zone)
            foreach (var slot in fromHomeSlots)
            {
                if (!toHomeSet.Contains(slot))
                {
                    var p = _homePlayers[slot];
                    if (p != null && p.gameObject.activeSelf) fadingPlayers.Add(p);
                }
            }

            // Away leaving players (in previous zone, not in new zone)
            foreach (var slot in fromAwaySlots)
            {
                if (!toAwaySet.Contains(slot))
                {
                    var p = _awayPlayers[slot];
                    if (p != null && p.gameObject.activeSelf) fadingPlayers.Add(p);
                }
            }

            // Home advancing players (in from AND in to)
            for (int i = 0; i < toHomeSlots.Length; i++)
            {
                int slot = toHomeSlots[i];
                if (fromHomeSet.Contains(slot))
                {
                    var p = _homePlayers[slot];
                    if (p != null)
                    {
                        p.gameObject.SetActive(true);
                        var cg = p.GetComponent<CanvasGroup>();
                        if (cg != null) cg.alpha = 1f;
                        float startX = p.rectTransform.anchorMin.x;
                        float startY = p.rectTransform.anchorMin.y;
                        float targetX = toHomeSlots.Length == 1 ? (toLoc == 2 ? 0.28f : 0.33f) : 0.27f;
                        float targetY = toHomeSlots.Length == 1 ? 0.38f : ys[i];
                        activeMovingPlayers.Add((p, startX, targetX, startY, targetY, true));
                    }
                }
            }

            // Home entering players (not in from, but in to)
            for (int i = 0; i < toHomeSlots.Length; i++)
            {
                int slot = toHomeSlots[i];
                if (!fromHomeSet.Contains(slot))
                {
                    var p = _homePlayers[slot];
                    if (p != null)
                    {
                        p.gameObject.SetActive(true);
                        var cg = p.GetComponent<CanvasGroup>();
                        if (cg != null) cg.alpha = 0f;
                        float startX = -0.08f;
                        float targetX = toHomeSlots.Length == 1 ? (toLoc == 2 ? 0.28f : 0.33f) : 0.27f;
                        float targetY = toHomeSlots.Length == 1 ? 0.38f : ys[i];
                        p.rectTransform.anchorMin = p.rectTransform.anchorMax = new Vector2(startX, targetY);
                        p.rectTransform.anchoredPosition = Vector2.zero;
                        float currentScale = (_homePlayerScales != null && slot < _homePlayerScales.Length) ? _homePlayerScales[slot] : 1f;
                        p.rectTransform.localScale = new Vector3(-currentScale, currentScale, 1f);
                        activeMovingPlayers.Add((p, startX, targetX, targetY, targetY, true));
                    }
                }
            }

            // Away advancing players (in from AND in to)
            for (int i = 0; i < toAwaySlots.Length; i++)
            {
                int slot = toAwaySlots[i];
                if (fromAwaySet.Contains(slot))
                {
                    var p = _awayPlayers[slot];
                    if (p != null)
                    {
                        p.gameObject.SetActive(true);
                        var cg = p.GetComponent<CanvasGroup>();
                        if (cg != null) cg.alpha = 1f;
                        float startX = p.rectTransform.anchorMin.x;
                        float startY = p.rectTransform.anchorMin.y;
                        float targetX = toAwaySlots.Length == 1 ? (toLoc == 2 ? 0.67f : 0.72f) : 0.73f;
                        float targetY = toAwaySlots.Length == 1 ? 0.38f : ys[i];
                        activeMovingPlayers.Add((p, startX, targetX, startY, targetY, false));
                    }
                }
            }

            // Away entering players (not in from, but in to)
            for (int i = 0; i < toAwaySlots.Length; i++)
            {
                int slot = toAwaySlots[i];
                if (!fromAwaySet.Contains(slot))
                {
                    var p = _awayPlayers[slot];
                    if (p != null)
                    {
                        p.gameObject.SetActive(true);
                        var cg = p.GetComponent<CanvasGroup>();
                        if (cg != null) cg.alpha = 0f;
                        float startX = 1.08f;
                        float targetX = toAwaySlots.Length == 1 ? (toLoc == 2 ? 0.67f : 0.72f) : 0.73f;
                        float targetY = toAwaySlots.Length == 1 ? 0.38f : ys[i];
                        p.rectTransform.anchorMin = p.rectTransform.anchorMax = new Vector2(startX, targetY);
                        p.rectTransform.anchoredPosition = Vector2.zero;
                        float currentScale = (_awayPlayerScales != null && slot < _awayPlayerScales.Length) ? _awayPlayerScales[slot] : 1f;
                        p.rectTransform.localScale = new Vector3(currentScale, currentScale, 1f);
                        activeMovingPlayers.Add((p, startX, targetX, targetY, targetY, false));
                    }
                }
            }

            var startBallPos = _ballMarker != null ? _ballMarker.rectTransform.anchorMin : GetBallPosition(fromLoc, oldPossession);
            var targetBallPos = GetBallPosition(toLoc, newPossession, _match != null ? _match.BallHolderSlotIndex : 1);
            float rollSpin = (targetPitchX < startPitchX) ? -1f : 1f;

            if (_ballShadow != null) _ballShadow.transform.SetAsLastSibling();
            if (_ballMarker != null) _ballMarker.transform.SetAsLastSibling();

            const float duration = 0.95f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, progress);

                // 1. Shift pitch background
                if (_fieldPitchRoot != null)
                {
                    _fieldPitchRoot.anchoredPosition = new Vector2(Mathf.Lerp(startPitchX, targetPitchX, smoothT), 0f);
                }

                // 2. Fade out leaving players (over first 0.38s)
                float fadeOutAlpha = Mathf.Clamp01(1f - (elapsed / 0.38f));
                for (int i = 0; i < fadingPlayers.Count; i++)
                {
                    var p = fadingPlayers[i];
                    if (p != null)
                    {
                        var cg = p.GetComponent<CanvasGroup>();
                        if (cg != null) cg.alpha = fadeOutAlpha;
                        if (fadeOutAlpha <= 0f && p.gameObject.activeSelf) p.gameObject.SetActive(false);
                    }
                }

                // 3. Step bounce and forward lean oscillation (authentic walking choreography)
                float stepFreq = 16f;
                float stepBounce = Mathf.Abs(Mathf.Sin(elapsed * stepFreq)) * 7f;
                float stepTilt = Mathf.Sin(elapsed * stepFreq) * 3.5f;

                // 4. Update all advancing & entering players
                float fadeInAlpha = Mathf.Clamp01(elapsed / 0.32f);
                for (int i = 0; i < activeMovingPlayers.Count; i++)
                {
                    var item = activeMovingPlayers[i];
                    var p = item.player;
                    if (p != null)
                    {
                        var cg = p.GetComponent<CanvasGroup>();
                        if (cg != null && cg.alpha < 1f) cg.alpha = fadeInAlpha;

                        float curX = Mathf.Lerp(item.startX, item.targetX, smoothT);
                        float curY = Mathf.Lerp(item.startY, item.targetY, smoothT);
                        p.rectTransform.anchorMin = p.rectTransform.anchorMax = new Vector2(curX, curY);
                        p.rectTransform.anchoredPosition = new Vector2(0f, stepBounce);

                        float lean = (item.isHome ? -1f : 1f) * (isHomeAdvancing ? stepTilt : -stepTilt);
                        p.rectTransform.localEulerAngles = new Vector3(0f, 0f, lean);

                        var rig = p.GetComponent<LayeredPlayerRig>();
                        if (rig != null) rig.SetRunFrame((int)(elapsed * 20f));
                    }
                }

                // 5. Ball tracks smoothly in front of advancing ball carrier
                var ballPos = Vector2.Lerp(startBallPos, targetBallPos, smoothT);
                if (_ballMarker != null)
                {
                    _ballMarker.rectTransform.anchorMin = _ballMarker.rectTransform.anchorMax = ballPos;
                    _ballMarker.rectTransform.Rotate(0f, 0f, rollSpin * 520f * Time.deltaTime);
                }
                if (_ballShadow != null)
                {
                    _ballShadow.rectTransform.anchorMin = _ballShadow.rectTransform.anchorMax =
                        new Vector2(ballPos.x, ballPos.y - 0.015f);
                }

                yield return null;
            }

            // Cleanup & lock into final exact alignment
            if (_fieldPitchRoot != null)
                _fieldPitchRoot.anchoredPosition = new Vector2(targetPitchX, 0f);

            for (int i = 0; i < fadingPlayers.Count; i++)
            {
                if (fadingPlayers[i] != null)
                {
                    var cg = fadingPlayers[i].GetComponent<CanvasGroup>();
                    if (cg != null) cg.alpha = 1f;
                    fadingPlayers[i].gameObject.SetActive(false);
                }
            }

            UpdatePlayerPositions(toLoc);
            SetBallPosition(toLoc, newPossession, false);
        }

        private IEnumerator SmoothShiftFieldAndBall(int fromLoc, int toLoc, int possession)
        {
            float pitchScale = (_fieldPitchRoot != null && _fieldPitchRoot.localScale.y > 0f)
                ? _fieldPitchRoot.localScale.y : 1f;
            var startPitchX = GetPitchOffsetForLocation(fromLoc) * pitchScale;
            var targetPitchX = GetPitchOffsetForLocation(toLoc) * pitchScale;
            var startBallPos = _ballMarker != null ? _ballMarker.rectTransform.anchorMin : GetBallPosition(fromLoc, possession);
            var targetBallPos = GetBallPosition(toLoc, possession, _match != null ? _match.BallHolderSlotIndex : 1);
            var elapsed = 0f;
            const float duration = 0.75f;

            if (_ballShadow != null) _ballShadow.transform.SetAsLastSibling();
            if (_ballMarker != null) _ballMarker.transform.SetAsLastSibling();

            float rollSpin = (targetPitchX < startPitchX) ? -1f : 1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                if (_fieldPitchRoot != null)
                {
                    _fieldPitchRoot.anchoredPosition = new Vector2(Mathf.Lerp(startPitchX, targetPitchX, t), 0f);
                }
                var ballPos = Vector2.Lerp(startBallPos, targetBallPos, t);
                if (_ballMarker != null)
                {
                    _ballMarker.rectTransform.anchorMin = _ballMarker.rectTransform.anchorMax = ballPos;
                    _ballMarker.rectTransform.Rotate(0f, 0f, rollSpin * 450f * Time.deltaTime);
                }
                if (_ballShadow != null)
                {
                    _ballShadow.rectTransform.anchorMin = _ballShadow.rectTransform.anchorMax =
                        new Vector2(ballPos.x, ballPos.y - 0.015f);
                }

                // Running bounce and natural lean on visible players during pitch transition
                float bounce = Mathf.Abs(Mathf.Sin(elapsed * 12f)) * 4.5f;
                float tilt = Mathf.Sin(elapsed * 12f) * 2.5f;
                if (_homePlayers != null)
                {
                    for (int i = 0; i < _homePlayers.Length; i++)
                        if (_homePlayers[i] != null && _homePlayers[i].gameObject.activeSelf)
                        {
                            _homePlayers[i].rectTransform.anchoredPosition = new Vector2(0f, bounce);
                            _homePlayers[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, -tilt);
                            _homePlayers[i].GetComponent<LayeredPlayerRig>()?.SetRunFrame((int)(elapsed * 20f));
                        }
                }
                if (_awayPlayers != null)
                {
                    for (int i = 0; i < _awayPlayers.Length; i++)
                        if (_awayPlayers[i] != null && _awayPlayers[i].gameObject.activeSelf)
                        {
                            _awayPlayers[i].rectTransform.anchoredPosition = new Vector2(0f, bounce);
                            _awayPlayers[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, tilt);
                            _awayPlayers[i].GetComponent<LayeredPlayerRig>()?.SetRunFrame((int)(elapsed * 20f));
                        }
                }

                yield return null;
            }

            if (_fieldPitchRoot != null)
                _fieldPitchRoot.anchoredPosition = new Vector2(targetPitchX, 0f);
            if (_ballMarker != null)
            {
                _ballMarker.rectTransform.anchorMin = _ballMarker.rectTransform.anchorMax = targetBallPos;
                _ballMarker.rectTransform.localEulerAngles = Vector3.zero;
            }
            if (_ballShadow != null)
                _ballShadow.rectTransform.anchorMin = _ballShadow.rectTransform.anchorMax =
                    new Vector2(targetBallPos.x, targetBallPos.y - 0.015f);

            if (_homePlayers != null)
                for (int i = 0; i < _homePlayers.Length; i++)
                    if (_homePlayers[i] != null)
                    {
                        _homePlayers[i].rectTransform.anchoredPosition = Vector2.zero;
                        _homePlayers[i].rectTransform.localEulerAngles = Vector3.zero;
                        _homePlayers[i].GetComponent<LayeredPlayerRig>()?.SetIdlePose();
                    }
            if (_awayPlayers != null)
                for (int i = 0; i < _awayPlayers.Length; i++)
                    if (_awayPlayers[i] != null)
                    {
                        _awayPlayers[i].rectTransform.anchoredPosition = Vector2.zero;
                        _awayPlayers[i].rectTransform.localEulerAngles = Vector3.zero;
                        _awayPlayers[i].GetComponent<LayeredPlayerRig>()?.SetIdlePose();
                    }

            if (_ballShadow != null) _ballShadow.transform.SetAsLastSibling();
            if (_ballMarker != null) _ballMarker.transform.SetAsLastSibling();
        }

        private IEnumerator SmoothPassBall(Vector2 start, Vector2 target, float duration, bool withArc = false)
        {
            if (_ballMarker == null) yield break;
            if (_ballShadow != null) _ballShadow.transform.SetAsLastSibling();
            _ballMarker.transform.SetAsLastSibling();

            float elapsed = 0f;
            float spinDir = target.x >= start.x ? -1f : 1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                var cur = Vector2.Lerp(start, target, smoothT);
                float arcY = withArc ? Mathf.Sin(smoothT * Mathf.PI) * 0.06f : 0f;

                _ballMarker.rectTransform.anchorMin = _ballMarker.rectTransform.anchorMax = new Vector2(cur.x, cur.y + arcY);
                _ballMarker.rectTransform.Rotate(0f, 0f, spinDir * 900f * Time.deltaTime);

                if (_ballShadow != null)
                {
                    _ballShadow.rectTransform.anchorMin = _ballShadow.rectTransform.anchorMax = new Vector2(cur.x, cur.y - 0.015f);
                    float shadowScale = withArc ? Mathf.Lerp(1f, 0.75f, Mathf.Sin(smoothT * Mathf.PI)) : 1f;
                    _ballShadow.rectTransform.localScale = new Vector3(shadowScale, shadowScale, 1f);
                }

                yield return null;
            }

            _ballMarker.rectTransform.anchorMin = _ballMarker.rectTransform.anchorMax = target;
            _ballMarker.rectTransform.localEulerAngles = Vector3.zero;
            if (_ballShadow != null)
            {
                _ballShadow.rectTransform.anchorMin = _ballShadow.rectTransform.anchorMax = new Vector2(target.x, target.y - 0.015f);
                _ballShadow.rectTransform.localScale = Vector3.one;
            }
        }

        private IEnumerator IdlePlayerBobbing()
        {
            while (true)
            {
                if (!_inEncounterAnimation && _homePlayers != null && _awayPlayers != null)
                {
                    float t = Time.time * 3f;
                    for (int i = 0; i < _homePlayers.Length; i++)
                    {
                        if (_homePlayers[i] != null && _homePlayers[i].gameObject.activeSelf)
                        {
                            float bob = Mathf.Sin(t + i * 0.7f) * 2.2f;
                            _homePlayers[i].rectTransform.anchoredPosition = new Vector2(0f, bob);
                        }
                    }
                    for (int i = 0; i < _awayPlayers.Length; i++)
                    {
                        if (_awayPlayers[i] != null && _awayPlayers[i].gameObject.activeSelf)
                        {
                            float bob = Mathf.Sin(t + i * 0.7f + 1.2f) * 2.2f;
                            _awayPlayers[i].rectTransform.anchoredPosition = new Vector2(0f, bob);
                        }
                    }
                }
                yield return null;
            }
        }

        private static Sprite GetLogoShineSprite(int frame)
        {
            if (frame == 7)
                return GetUiSprite("Original/sprites/StartScreenLogoAnimation2/LogoShine07");
            return GetUiSprite($"Original/sprites/StartScreenLogoAnimation/LogoShine{frame:00}");
        }

        private IEnumerator AnimateLogoShine(Image shineOverlay)
        {
            while (shineOverlay != null)
            {
                yield return new WaitForSeconds(3.5f);
                if (shineOverlay == null) yield break;
                shineOverlay.enabled = true;
                for (int frame = 1; frame <= 11; frame++)
                {
                    if (shineOverlay == null) yield break;
                    shineOverlay.sprite = GetLogoShineSprite(frame);
                    yield return new WaitForSeconds(0.055f);
                }
                if (shineOverlay != null) shineOverlay.enabled = false;
            }
        }

        private IEnumerator AnimatePulse(RectTransform targetRect)
        {
            while (targetRect != null)
            {
                float scale = 1f + Mathf.Sin(Time.time * 2.2f) * 0.025f;
                targetRect.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
        }

        private Vector2 GetBallPosition(int location, int possession, int slotIndex = -1)
        {
            if (slotIndex < 0)
            {
                slotIndex = _match != null ? _match.BallHolderSlotIndex : 1;
            }

            // In 1v1 encounters (location 2 or -2), striker and GK face off at y = 0.38f
            if (Mathf.Abs(location) == 2)
            {
                if (location == 2) // Home Striker attacking Away GK
                {
                    return possession == 0 ? new Vector2(0.335f, 0.395f) : new Vector2(0.625f, 0.395f);
                }
                else // Away Striker attacking Home GK
                {
                    return possession == 1 ? new Vector2(0.665f, 0.395f) : new Vector2(0.375f, 0.395f);
                }
            }

            // In 3v3 outfield encounters (locations -1, 0, 1):
            // Ball rests right next to the boots of the active ball holder:
            float y = slotIndex switch
            {
                0 => 0.145f,
                2 => 0.645f,
                _ => 0.395f
            };

            float x = possession == 0 ? 0.325f : 0.675f;
            return new Vector2(x, y);
        }

        private void SetBallPosition(int location, int possession, bool smooth, int slotIndex = -1)
        {
            var target = GetBallPosition(location, possession, slotIndex);
            if (_ballMarker != null)
            {
                _ballMarker.rectTransform.anchorMin = _ballMarker.rectTransform.anchorMax = target;
                _ballMarker.rectTransform.anchoredPosition = Vector2.zero;
            }
            if (_ballShadow != null)
            {
                _ballShadow.rectTransform.anchorMin = _ballShadow.rectTransform.anchorMax =
                    new Vector2(target.x, target.y - 0.012f);
                _ballShadow.rectTransform.anchoredPosition = Vector2.zero;
            }
            if (_ballShadow != null) _ballShadow.transform.SetAsLastSibling();
            if (_ballMarker != null) _ballMarker.transform.SetAsLastSibling();
        }

        private string GetEncounterPreviewText()
        {
            if (_match == null) return "Shake or tap either screen edge to roll.";

            if (_isLocalMultiplayer)
            {
                switch (_match.Location)
                {
                    case 0:
                        return "MIDFIELD BATTLE: P1 (RED) VS P2 (BLUE) — TAP TO ROLL!";
                    case 1:
                        return _match.Possession == 0
                            ? "P1 ATTACKING BLUE DEFENSE! — TAP TO ROLL"
                            : "P2 COUNTER-ATTACKING FROM MIDFIELD! — TAP TO ROLL";
                    case 2:
                        return _match.Possession == 0
                            ? "P1 1-ON-1 SHOT ON GOAL! — P1 vs P2 GOALKEEPER"
                            : "P2 DEFENSIVE CLEARANCE! — TAP TO ROLL";
                    case -1:
                        return _match.Possession == 1
                            ? "P2 ATTACKING RED DEFENSE! — TAP TO ROLL"
                            : "P1 COUNTER-ATTACKING FROM RED DEFENSE! — TAP TO ROLL";
                    case -2:
                        return _match.Possession == 1
                            ? "P2 1-ON-1 SHOT ON GOAL! — P2 vs P1 GOALKEEPER"
                            : "P1 DEFENSIVE CLEARANCE! — TAP TO ROLL";
                    default:
                        return "P1 (RED) VS P2 (BLUE) — TAP TO ROLL!";
                }
            }

            switch (_match.Location)
            {
                case 0:
                    return "MIDFIELD BATTLE — WIN POSSESSION & ADVANCE";
                case 1:
                    return _match.Possession == 0
                        ? "ATTACKING BLUE DEFENSE — BREAK THROUGH!"
                        : "DEFENDING MIDFIELD — BLUE COUNTER ATTACK";
                case 2:
                    return _match.Possession == 0
                        ? "SHOT ON GOAL! — YOUR CF vs BLUE GOALKEEPER"
                        : "BLUE CLEARANCE ATTEMPT";
                case -1:
                    return _match.Possession == 1
                        ? "DEFENDING RED GOAL — STOP THE ATTACK!"
                        : "CLEARING FROM RED DEFENSE";
                case -2:
                    return _match.Possession == 1
                        ? "GOAL THREAT! — BLUE CF vs YOUR GOALKEEPER"
                        : "RED GOALKEEPER CLEARANCE";
                default:
                    return "Shake or tap either screen edge to roll.";
            }
        }

        private Image[] AddFormationPlayers(PlayerData[] lineup, Transform parent, bool home)
        {
            var players = new Image[lineup.Length];
            var scales = new float[lineup.Length];
            var kit = home
                ? MatchKitAppearance.FromIds(_catalog, _currentShirtId, _currentShirtColorId, _currentSleeveColorId, _currentPatternStyle, _currentPatternColorId, _currentPantsId)
                : MatchKitAppearance.FromFixture(_catalog, _currentFixture);

            for (var slot = 0; slot < lineup.Length; slot++)
            {
                var pData = lineup[slot];

                var playerGo = new GameObject($"{(home ? "Home" : "Away")}Player{slot + 1}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                playerGo.transform.SetParent(parent, false);
                var player = playerGo.GetComponent<Image>();
                // Assign root sprite transparently so root Image satisfies non-null sprite checks while LayeredPlayerRig renders body/kit parts
                player.sprite = GetUiSprite("Original/sprites/PlayerBody3/Shadow");
                // Pin pivot to boot soles
                player.rectTransform.pivot = new Vector2(0.5f, 0.032f);
                player.rectTransform.sizeDelta = new Vector2(140f, 238f);
                player.color = Color.clear;
                player.raycastTarget = false;
                player.preserveAspect = true;

                float vScale = 1.0f;
                scales[slot] = vScale;
                player.rectTransform.localScale = new Vector3((home ? -1f : 1f) * vScale, vScale, 1f);

                LayeredPlayerRig.Attach(playerGo, pData, kit, isGoalkeeper: slot == 0, fallbackSlot: slot + 1);

                players[slot] = player;
            }

            if (home) _homePlayerScales = scales;
            else _awayPlayerScales = scales;

            return players;
        }

        private void UpdatePlayerPositions(int? overrideLocation = null)
        {
            if (_homePlayers == null || _awayPlayers == null || _match == null) return;
            var loc = overrideLocation.HasValue ? overrideLocation.Value : _match.Location;
            var (activeHome, activeAway) = GetActiveDuelSlots(loc);

            // Ground baseline Ys matching media_1792421110195_880f72c3.png:
            // Row 0 (bottom): 0.13f, Row 1 (center): 0.38f, Row 2 (top): 0.63f
            float[] ys = { 0.13f, 0.38f, 0.63f };

            // Inactive slots hidden
            for (var slot = 0; slot < 7; slot++)
            {
                _homePlayers[slot].gameObject.SetActive(false);
                _awayPlayers[slot].gameObject.SetActive(false);
            }

            // Position active Home players (face right towards opponent/goal)
            for (var i = 0; i < activeHome.Length; i++)
            {
                var slot = activeHome[i];
                var p = _homePlayers[slot];
                p.gameObject.SetActive(true);
                var cg = p.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;
                float y = activeHome.Length == 1 ? 0.38f : ys[i];
                float x = activeHome.Length == 1 ? (loc == 2 ? 0.30f : 0.34f) : 0.29f;
                p.rectTransform.anchorMin = p.rectTransform.anchorMax = new Vector2(x, y);
                p.rectTransform.anchoredPosition = Vector2.zero;
                p.rectTransform.localEulerAngles = Vector3.zero;
                float currentScale = (_homePlayerScales != null && slot < _homePlayerScales.Length) ? _homePlayerScales[slot] : 1f;
                p.rectTransform.localScale = new Vector3(-currentScale, currentScale, 1f);
                var rig = p.GetComponent<LayeredPlayerRig>();
                if (rig != null)
                {
                    rig.SetIdlePose();
                    rig.SetCardState(_match.HasYellowCard(true, slot), _match.HasRedCard(true, slot));
                }
            }

            // Position active Away players (face left towards opponent/goal)
            for (var i = 0; i < activeAway.Length; i++)
            {
                var slot = activeAway[i];
                var p = _awayPlayers[slot];
                p.gameObject.SetActive(true);
                var cg = p.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;
                float y = activeAway.Length == 1 ? 0.38f : ys[i];
                float x = activeAway.Length == 1 ? (loc == 2 ? 0.66f : 0.70f) : 0.71f;
                p.rectTransform.anchorMin = p.rectTransform.anchorMax = new Vector2(x, y);
                p.rectTransform.anchoredPosition = Vector2.zero;
                p.rectTransform.localEulerAngles = Vector3.zero;
                float currentScale = (_awayPlayerScales != null && slot < _awayPlayerScales.Length) ? _awayPlayerScales[slot] : 1f;
                p.rectTransform.localScale = new Vector3(currentScale, currentScale, 1f);
                var rig = p.GetComponent<LayeredPlayerRig>();
                if (rig != null)
                {
                    rig.SetIdlePose();
                    rig.SetCardState(_match.HasYellowCard(false, slot), _match.HasRedCard(false, slot));
                }
            }

            if (_shakePrompt != null)
            {
                // In 1v1, striker (0.28) and GK (0.67) midpoint is exactly (0.28 + 0.67)/2 = 0.475f.
                // In 1v1 defending, GK (0.33) and striker (0.72) midpoint is (0.33 + 0.72)/2 = 0.525f.
                // In 3v3 outfield battles, midpoint between home (0.27) and away (0.73) is 0.50f.
                float shakeX = (loc == 2) ? 0.475f : (loc == -2 ? 0.525f : 0.50f);
                _shakePrompt.rectTransform.anchorMin = _shakePrompt.rectTransform.anchorMax = new Vector2(shakeX, 0.50f);
                _shakePrompt.rectTransform.anchoredPosition = Vector2.zero;
            }

            if (_ballShadow != null) _ballShadow.transform.SetAsLastSibling();
            if (_ballMarker != null) _ballMarker.transform.SetAsLastSibling();
        }

        private IEnumerator PlayPoofAtPosition(Vector2 anchorPos)
        {
            Transform parent = _matchPlayRoot != null ? _matchPlayRoot : _field;
            if (parent == null) yield break;
            var poofObj = new GameObject("PoofEffect", typeof(RectTransform), typeof(Image));
            poofObj.transform.SetParent(parent, false);
            var rect = poofObj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchorPos;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(110f, 110f);
            var img = poofObj.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;

            for (var f = 1; f <= 13; f++)
            {
                img.sprite = GetUiSprite($"Original/sprites/PlayerBody3/Poof{f:00}");
                yield return new WaitForSeconds(0.050f); // 20 FPS (0x3d4ccccd) authentic to Cocos2d binary
            }
            Destroy(poofObj);
        }

        private void AnimateAttackingPlayers(int attackingSide, float amount)
        {
            var players = attackingSide == 0 ? _homePlayers : _awayPlayers;
            if (players == null) return;
            var direction = attackingSide == 0 ? 1f : -1f;

            for (var slot = 0; slot < players.Length; slot++)
            {
                if (players[slot].gameObject.activeSelf)
                {
                    var rect = players[slot].rectTransform;
                    rect.anchoredPosition = new Vector2(direction * 14f * amount, 5f * amount);
                }
            }
        }

        private void ClearCanvas()
        {
            Time.timeScale = 1.0f;
            if (_idleBobbingCoroutine != null)
            {
                StopCoroutine(_idleBobbingCoroutine);
                _idleBobbingCoroutine = null;
            }

            for (var i = _canvas.transform.childCount - 1; i >= 0; i--)
            {
                var child = _canvas.transform.GetChild(i);
                if (child != _safeArea && child.name != "CanvasBackground")
                {
                    if (Application.isPlaying)
                        Destroy(child.gameObject);
                    else
                        DestroyImmediate(child.gameObject);
                }
            }
            for (var i = _safeArea.childCount - 1; i >= 0; i--)
            {
                var child = _safeArea.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
            _backButton = null;
            _substitutionButton = null;
            _encounterButton = null;
            _overlayContainer = null;
            _inEncounterAnimation = false;
            _fieldPitchRoot = null;
            _shakePrompt = null;
            _homeTotalText = null;
            _awayTotalText = null;
            _homeDieRoots = null;
            _awayDieRoots = null;
            _homeDieValueLabels = null;
            _awayDieValueLabels = null;
            _buffBeltGo = null;
            _buffBeltText = null;
            _buffBeltMedal = null;
            _buffBeltBadgeText = null;
            _homePlayerScales = null;
            _awayPlayerScales = null;
            _matchPlayRoot = null;
        }

        private void ApplySafeArea()
        {
            if (_safeArea == null)
                return;

            var area = Screen.safeArea;
            float screenW = Screen.width;
            float screenH = Screen.height;

            var activeCam = (_canvas != null && _canvas.worldCamera != null) ? _canvas.worldCamera : Camera.main;
            if (activeCam != null && activeCam.targetTexture != null)
            {
                screenW = activeCam.targetTexture.width;
                screenH = activeCam.targetTexture.height;
            }

            const float targetAspect = ReferenceWidth / ReferenceHeight; // 640 / 960 = 2/3 = 0.666667f
            float currentAspect = screenW / Mathf.Max(1f, screenH);

            if (_canvas != null)
            {
                var scaler = _canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    // On screens wider than 2:3 (tablets/landscape), match height so reference frame never shrinks below 640x960
                    scaler.matchWidthOrHeight = currentAspect > targetAspect ? 1f : 0f;
                }
            }

            bool fullScreen = PlayerPrefs.GetInt("diceSoccer.fullscreen", 0) == 1;

            if (fullScreen)
            {
                // True full screen: fills 100% of the display edge-to-edge while centered frames preserve 2:3 proportions
                _safeArea.anchorMin = Vector2.zero;
                _safeArea.anchorMax = Vector2.one;
            }
            else
            {
                // Strict authentic 2:3 aspect ratio letterboxing
                if (currentAspect < targetAspect)
                {
                    // Screen is taller than 2:3 (modern phones) -> top/bottom letterbox bars
                    float viewportHeightFraction = currentAspect / targetAspect;
                    float marginY = (1f - viewportHeightFraction) * 0.5f;
                    _safeArea.anchorMin = new Vector2(0f, marginY);
                    _safeArea.anchorMax = new Vector2(1f, 1f - marginY);
                }
                else
                {
                    // Screen is wider than 2:3 (tablets / batchmode) -> left/right pillarbox bars
                    float viewportWidthFraction = targetAspect / currentAspect;
                    float marginX = (1f - viewportWidthFraction) * 0.5f;
                    _safeArea.anchorMin = new Vector2(marginX, 0f);
                    _safeArea.anchorMax = new Vector2(1f - marginX, 1f);
                }
            }

            _safeArea.offsetMin = Vector2.zero;
            _safeArea.offsetMax = Vector2.zero;
            _lastSafeArea = area;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            UpdateMatchFieldScale();
        }

        private float GetMatchPitchScale()
        {
            bool fullScreen = PlayerPrefs.GetInt("diceSoccer.fullscreen", 0) == 1;
            if (!fullScreen)
                return 1f;

            float canvasW = ReferenceWidth;
            float canvasH = ReferenceHeight;
            var activeCam = (_canvas != null && _canvas.worldCamera != null) ? _canvas.worldCamera : Camera.main;
            const float targetAspect = ReferenceWidth / ReferenceHeight;

            if (activeCam != null && activeCam.targetTexture != null)
            {
                float aspect = (float)activeCam.targetTexture.width / Mathf.Max(1, activeCam.targetTexture.height);
                if (aspect <= targetAspect)
                    canvasH = ReferenceWidth / aspect;
                else
                    canvasW = ReferenceHeight * aspect;
            }
            else if (Screen.width > 0 && Screen.height > 0)
            {
                float aspect = (float)Screen.width / Screen.height;
                if (aspect <= targetAspect)
                    canvasH = ReferenceWidth / aspect;
                else
                    canvasW = ReferenceHeight * aspect;
            }

            if (_safeArea != null)
            {
                if (_safeArea.rect.width > canvasW) canvasW = _safeArea.rect.width;
                if (_safeArea.rect.height > canvasH) canvasH = _safeArea.rect.height;
            }

            return Mathf.Max(1f, canvasH / ReferenceHeight, canvasW / ReferenceWidth);
        }

        private void UpdateMatchFieldScale()
        {
            if (_fieldPitchRoot == null)
                return;

            float pitchScale = GetMatchPitchScale();
            _fieldPitchRoot.localScale = new Vector3(pitchScale, pitchScale, 1f);
            if (_match != null)
            {
                _fieldPitchRoot.anchoredPosition = new Vector2(GetPitchOffsetForLocation(_match.Location) * pitchScale, 0f);
            }
        }

        private RectTransform CreateCenteredReferenceFrame(string objectName = "ContentFrame", Transform parent = null)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent != null ? parent : _safeArea, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(ReferenceWidth, ReferenceHeight);
            return rect;
        }

        private RectTransform CreateBottomBar(string objectName, float height)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(_safeArea, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(ReferenceWidth, height);
            return rect;
        }

        private void AddFullscreenPitchBackdrop()
        {
            var backdrop = new GameObject("PitchBackdrop", typeof(RectTransform), typeof(AspectRatioFitter));
            backdrop.transform.SetParent(_canvas.transform, false);
            var rect = backdrop.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var fitter = backdrop.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = ReferenceWidth / ReferenceHeight;

            var pitchLeft = AddResourceImage("PitchLeft", "Original/images/GreenField", backdrop.transform,
                new Vector2(0f, 0f), new Vector2(0.5f, 1f));
            pitchLeft.preserveAspect = false;
            pitchLeft.rectTransform.localScale = Vector3.one;
            pitchLeft.raycastTarget = false;

            var pitchRight = AddResourceImage("PitchRight", "Original/images/GreenField", backdrop.transform,
                new Vector2(0.5f, 0f), new Vector2(1f, 1f));
            pitchRight.preserveAspect = false;
            pitchRight.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            pitchRight.raycastTarget = false;
        }

        private void AddMenuBackdrop(bool showClouds = false)
        {
            var bg = AddStretchImage("MenuBackground",
                "Original/sprites/StartScreenSS/StartScreenBG", _canvas.transform);
            bg.raycastTarget = false;
            var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = ReferenceWidth / ReferenceHeight;

            _safeArea.SetAsLastSibling();
            if (!showClouds)
                return;

            var clouds = AddResourceImage("MenuClouds",
                "Original/sprites/StartScreenSS/StartScreenClouds",
                _safeArea, new Vector2(0f, 1f), new Vector2(1f, 1f));
            clouds.rectTransform.pivot = new Vector2(0.5f, 1f);
            clouds.rectTransform.anchoredPosition = new Vector2(0f, -58f);
            clouds.rectTransform.sizeDelta = new Vector2(0f, 230f);
            clouds.raycastTarget = false;
            StartCoroutine(AnimateCloudsDrift(clouds.rectTransform, 14f));
        }

        private RectTransform AddMenuHeader(string title, UnityEngine.Events.UnityAction backAction)
        {
            var header = AddResourceImage("MenuHeader", "Original/images/MenuHeader",
                _safeArea, new Vector2(0f, 1f), new Vector2(1f, 1f));
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = Vector2.zero;
            header.rectTransform.sizeDelta = new Vector2(0f, 77f);
            header.preserveAspect = false;
            header.raycastTarget = false;

            if (backAction != null)
                AddSpriteButton("Back", "Original/images/MenuHeaderBackButton",
                    "Original/images/MenuHeaderBackButtonPressed", header.transform,
                    new Vector2(0.08f, 0.50f), new Vector2(0.12f, 0.93f), backAction);

            if (!string.IsNullOrEmpty(title))
            {
                var fontSize = title.Length > 12 ? 11 : (title.Length > 9 ? 13 : 18);
                var titleText = AddText(title.ToUpperInvariant(), header.transform, fontSize, Ink,
                    new Vector2(backAction != null ? 0.16f : 0.05f, 0.08f), new Vector2(0.39f, 0.92f));
                titleText.alignment = TextAnchor.MiddleLeft;
                titleText.fontStyle = FontStyle.Bold;
                titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
                titleText.verticalOverflow = VerticalWrapMode.Truncate;
                titleText.resizeTextForBestFit = true;
                titleText.resizeTextMinSize = 8;
                titleText.resizeTextMaxSize = fontSize;
            }

            var cashText = AddText(_careerCash.ToString("N0"), header.transform, 18, Ink,
                new Vector2(0.38f, 0.08f), new Vector2(0.68f, 0.92f));
            cashText.alignment = TextAnchor.MiddleRight;
            cashText.fontStyle = FontStyle.Bold;
            cashText.horizontalOverflow = HorizontalWrapMode.Overflow;
            cashText.verticalOverflow = VerticalWrapMode.Truncate;
            cashText.resizeTextForBestFit = true;
            cashText.resizeTextMinSize = 11;
            cashText.resizeTextMaxSize = 18;

            var coinImg = AddResourceImage("CareerCoin", "Original/images/CoinSmall", header.transform,
                new Vector2(0.69f, 0.19f), new Vector2(0.76f, 0.81f));
            coinImg.preserveAspect = true;
            coinImg.raycastTarget = false;

            // Authentic crest frame on top-right
            var crestBox = AddResourceImage("HeaderCrestBox", "Original/images/CustomCrestBox", header.transform,
                new Vector2(0.85f, 0.06f), new Vector2(0.97f, 0.94f));
            crestBox.preserveAspect = true;
            crestBox.raycastTarget = false;

            var crestDesign = AddResourceImage("HeaderCrestDesign", $"Original/images/CustomCrest{_currentCrestId}", crestBox.transform,
                new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f));
            crestDesign.preserveAspect = true;
            crestDesign.raycastTarget = false;

            return header.rectTransform;
        }

        private void AddMenuFooter()
        {
            var footer = AddResourceImage("MenuFooter", "Original/images/FooterBG",
                _safeArea, new Vector2(0f, 0f), new Vector2(1f, 0f));
            footer.rectTransform.pivot = new Vector2(0.5f, 0f);
            footer.rectTransform.anchoredPosition = Vector2.zero;
            footer.rectTransform.sizeDelta = new Vector2(0f, 115f);
            footer.preserveAspect = false;
            footer.raycastTarget = false;

            AddSpriteButton("Shop", "Original/images/BottomTab01",
                "Original/images/BottomTab01Pressed", footer.transform,
                new Vector2(0.17f, 0.50f), new Vector2(0.32f, 0.833f), ShowShop);
            AddSpriteButton("Trophies", "Original/images/BottomTab02",
                "Original/images/BottomTab02Pressed", footer.transform,
                new Vector2(0.50f, 0.50f), new Vector2(0.32f, 0.833f), ShowTrophyRoom);
            AddSpriteButton("Options", "Original/images/BottomTab03",
                "Original/images/BottomTab03Pressed", footer.transform,
                new Vector2(0.83f, 0.50f), new Vector2(0.32f, 0.833f), ShowOptions);
        }

        private static Button AddSpriteButton(string objectName, string resourcePath,
            string pressedSpritePath, Transform parent, Vector2 anchor, Vector2 size,
            UnityEngine.Events.UnityAction onClick)
        {
            var image = AddResourceImage(objectName, resourcePath, parent,
                anchor - size * 0.5f, anchor + size * 0.5f);
            image.preserveAspect = true;
            image.color = Color.white;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                AudioManager.Instance.PlayTap();
                onClick?.Invoke();
            });
            button.transition = Selectable.Transition.SpriteSwap;
            var spriteState = button.spriteState;
            spriteState.highlightedSprite = GetUiSprite(pressedSpritePath);
            spriteState.pressedSprite = GetUiSprite(pressedSpritePath);
            button.spriteState = spriteState;
            return button;
        }

        private static Image AddPanel(string objectName, Transform parent, Color color,
            Vector2 anchor, Vector2 size)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor - size * 0.5f;
            rect.anchorMax = anchor + size * 0.5f;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Image AddStretchPanel(string objectName, Transform parent, Color color)
        {
            var image = AddPanel(objectName, parent, color, Vector2.zero, Vector2.zero);
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return image;
        }

        private static Image AddStretchImage(string objectName, string resourcePath, Transform parent)
        {
            var image = AddResourceImage(objectName, resourcePath, parent, Vector2.zero, Vector2.one);
            image.preserveAspect = false;
            var rect = image.rectTransform;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return image;
        }

        private static Image AddResourceImage(string objectName, string resourcePath, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = gameObject.GetComponent<Image>();
            var sprite = TryGetUiSprite(resourcePath);
            if (sprite != null)
                image.sprite = sprite;
            else
                image.color = new Color(0, 0, 0, 0);
            image.preserveAspect = true;
            return image;
        }

        private static Sprite TryGetUiSprite(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
                return null;
            if (UiSprites.TryGetValue(resourcePath, out var sprite))
                return sprite;

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
                return null;
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = texture.name;
            UiSprites.Add(resourcePath, sprite);
            return sprite;
        }

        private static Sprite GetUiSprite(string resourcePath)
        {
            var sprite = TryGetUiSprite(resourcePath);
            if (sprite != null)
                return sprite;
            throw new FileNotFoundException(
                $"Required original image is missing: Resources/{resourcePath}.png");
        }

        private static Text AddText(string value, Transform parent, int fontSize, Color color,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var gameObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var text = gameObject.GetComponent<Text>();
            text.text = value;
            text.font = GetBuiltinFont();
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button AddButton(string label, Transform parent, Color color,
            Vector2 anchor, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var image = AddPanel(label, parent, color, anchor, size);
            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.65f);
            outline.effectDistance = new Vector2(2, -2);

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                AudioManager.Instance.PlayTap();
                onClick?.Invoke();
            });
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color32(200, 200, 200, 255);
            colors.disabledColor = new Color32(110, 110, 110, 180);
            button.colors = colors;
            var text = AddText(label, image.transform, 17, Paper, Vector2.zero, Vector2.one);
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 8;
            text.resizeTextMaxSize = 17;
            return button;
        }

        private static Button AddPlayerButton(string label, int playerId, Transform parent,
            Color color, Vector2 anchor, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var button = AddButton(label, parent, color, anchor, size, onClick);
            var textRect = button.GetComponentInChildren<Text>().rectTransform;
            textRect.anchorMin = new Vector2(0.22f, 0f);
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var portrait = AddResourceImage("PlayerHead",
                $"Original/images/PlayerHead{playerId:00}", button.transform,
                new Vector2(0.025f, 0.12f), new Vector2(0.195f, 0.88f));
            portrait.raycastTarget = false;
            return button;
        }

        private static Font GetBuiltinFont()
        {
            var originalFont = Resources.Load<Font>("Original/fonts/Krungthep");
            return originalFont != null
                ? originalFont
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
