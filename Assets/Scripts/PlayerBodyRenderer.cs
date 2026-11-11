using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DiceSoccer
{
    /// <summary>
    /// Resolved jersey, sleeve, pattern, and shorts colors for either team in a match.
    /// </summary>
    public struct MatchKitAppearance
    {
        public Color32 ShirtColor;
        public Color32 SleeveColor;
        public int PatternStyle;
        public Color32 PatternColor;
        public Color32 PantsColor;

        public static MatchKitAppearance FromIds(
            GameCatalog catalog,
            int shirtId,
            int customShirtColorId,
            int customSleeveColorId,
            int customPatternStyle,
            int customPatternColorId,
            int pantsColorId)
        {
            var kit = new MatchKitAppearance
            {
                ShirtColor = new Color32(220, 45, 45, 255),
                SleeveColor = new Color32(255, 255, 255, 255),
                PatternStyle = 0,
                PatternColor = new Color32(255, 255, 255, 255),
                PantsColor = new Color32(25, 65, 185, 255),
            };
            if (catalog == null) return kit;

            var shirt = catalog.GetShirt(shirtId);
            if (customShirtColorId > 0)
            {
                kit.ShirtColor = catalog.GetColourRgb(customShirtColorId);
                kit.SleeveColor = catalog.GetColourRgb(customSleeveColorId > 0 ? customSleeveColorId : customShirtColorId);
                kit.PatternStyle = Mathf.Clamp(customPatternStyle, 0, 7);
                kit.PatternColor = catalog.GetColourRgb(customPatternColorId > 0 ? customPatternColorId : customShirtColorId);
            }
            else if (shirt != null)
            {
                kit.ShirtColor = catalog.GetColourRgb(shirt.shirtColour);
                kit.SleeveColor = catalog.GetColourRgb(shirt.sleeveColour);
                kit.PatternStyle = Mathf.Clamp(shirt.shirtStyle, 0, 7);
                kit.PatternColor = catalog.GetColourRgb(shirt.styleColour > 0 ? shirt.styleColour : shirt.sleeveColour);
            }

            if (pantsColorId > 0)
                kit.PantsColor = catalog.GetColourRgb(pantsColorId);

            return kit;
        }

        public static MatchKitAppearance FromFixture(GameCatalog catalog, FixtureData fixture)
        {
            if (fixture == null)
            {
                return new MatchKitAppearance
                {
                    ShirtColor = new Color32(35, 110, 225, 255),
                    SleeveColor = new Color32(255, 255, 255, 255),
                    PatternStyle = 0,
                    PatternColor = new Color32(255, 255, 255, 255),
                    PantsColor = new Color32(30, 50, 120, 255),
                };
            }

            int shirtCol = fixture.shirt > 0 ? fixture.shirt : 2;
            int sleeveCol = fixture.sleevesSock > 0 ? fixture.sleevesSock : shirtCol;
            int patCol = fixture.shirtPatternColor > 0 ? fixture.shirtPatternColor : sleeveCol;
            int patStyle = Mathf.Clamp(fixture.shirtPatternStyle, 0, 7);
            int pantsCol = fixture.pants > 0 ? fixture.pants : 3;

            return new MatchKitAppearance
            {
                ShirtColor = catalog != null ? catalog.GetColourRgb(shirtCol) : new Color32(48, 121, 237, 255),
                SleeveColor = catalog != null ? catalog.GetColourRgb(sleeveCol) : new Color32(255, 255, 255, 255),
                PatternStyle = patStyle,
                PatternColor = catalog != null ? catalog.GetColourRgb(patCol) : new Color32(255, 255, 255, 255),
                PantsColor = catalog != null ? catalog.GetColourRgb(pantsCol) : new Color32(25, 45, 140, 255),
            };
        }
    }

    /// <summary>
    /// Static sprite/palette utilities matching the 2012 Cocos2d iOS PlayerSprite, KitManager,
    /// GoalAnimationSS4, and LoadingScreenSprite atlas rules.
    /// </summary>
    public static class PlayerBodySprites
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static Sprite[] _loadingBodyFrames;
        private static Sprite _loadingHeadSprite;
        private static Sprite _loadingBubbleSprite;
        private static Sprite _whitePixelSprite;

        public static Color32 GetSkinColor(int skinColour)
        {
            // Exact KitManager.getSkinColour: ccColor3B values from original ARMv7 Mach-O
            switch (skinColour)
            {
                case 2: return new Color32(240, 176, 107, 255);
                case 3: return new Color32(202, 133, 51, 255);
                case 4: return new Color32(122, 69, 22, 255);
                case 5: return new Color32(65, 28, 18, 255);
                default: return new Color32(255, 220, 174, 255);
            }
        }

        public static string ResolveHeadPath(PlayerData player, int fallbackSlot = 1)
        {
            int id = 1;
            if (player != null)
            {
                if (player.id >= 1 && player.id <= 96)
                    id = player.id;
                else if (player.id >= 1000)
                    id = ((player.id - 1000) % 96) + 1;
                else if (player.imageId >= 1 && player.imageId <= 96)
                    id = player.imageId;
                else if (fallbackSlot >= 1 && fallbackSlot <= 96)
                    id = fallbackSlot;
            }
            else
            {
                id = Mathf.Clamp(fallbackSlot, 1, 96);
            }
            return $"Original/images/PlayerHead{id:00}";
        }

        public static Sprite LoadSprite(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;
            if (Cache.TryGetValue(resourcePath, out var cached) && cached != null) return cached;
            var sp = Resources.Load<Sprite>(resourcePath);
            if (sp == null)
            {
                var tex = Resources.Load<Texture2D>(resourcePath);
                if (tex != null)
                {
                    sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    sp.name = tex.name;
                }
            }
            if (sp != null) Cache[resourcePath] = sp;
            return sp;
        }

        public static Sprite WhitePixel()
        {
            if (_whitePixelSprite != null) return _whitePixelSprite;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var px = new[] { Color.white, Color.white, Color.white, Color.white };
            tex.SetPixels(px);
            tex.Apply();
            _whitePixelSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
            return _whitePixelSprite;
        }

        public static Image CreateLayer(string name, Transform parent, string spritePath, Color32 tint, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = LoadSprite(spritePath);
            img.color = tint;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.enabled = img.sprite != null;
            return img;
        }

        public static void EnsureLoadingAtlasSliced(out Sprite[] bodyFrames, out Sprite headSprite, out Sprite bubbleSprite)
        {
            if (_loadingBodyFrames != null && _loadingHeadSprite != null && _loadingBubbleSprite != null)
            {
                bodyFrames = _loadingBodyFrames;
                headSprite = _loadingHeadSprite;
                bubbleSprite = _loadingBubbleSprite;
                return;
            }

            var tex = Resources.Load<Texture2D>("Original/images/LoadingScreenSprite");
            if (tex == null)
            {
                bodyFrames = Array.Empty<Sprite>();
                headSprite = null;
                bubbleSprite = null;
                return;
            }

            // Texture2D y=0 is bottom (512px height).
            // Row 0: 14 frames inside y=[310, 452]; Row 1 cols 0..7: 8 frames inside y=[110, 250].
            var frames = new List<Sprite>(22);
            for (int c = 0; c < 14; c++)
            {
                float x = c * 140f;
                if (x + 140f <= tex.width)
                    frames.Add(Sprite.Create(tex, new Rect(x, 310f, 140f, 142f), new Vector2(0.5f, 0.5f), 100f));
            }
            for (int c = 0; c < 8; c++)
            {
                float x = c * 140f;
                frames.Add(Sprite.Create(tex, new Rect(x, 110f, 140f, 140f), new Vector2(0.5f, 0.5f), 100f));
            }

            _loadingBodyFrames = frames.ToArray();
            // Col 8 (x=1141..1232, Unity y=219..310): Willy's bouncing head
            _loadingHeadSprite = Sprite.Create(tex, new Rect(1140f, 218f, 94f, 94f), new Vector2(0.5f, 0.5f), 100f);
            // Col 9 (x=1266..1660, Unity y=66..300): Comic speech bubble for TIP text
            _loadingBubbleSprite = Sprite.Create(tex, new Rect(1264f, 64f, 396f, 238f), new Vector2(0.5f, 0.5f), 100f);

            bodyFrames = _loadingBodyFrames;
            headSprite = _loadingHeadSprite;
            bubbleSprite = _loadingBubbleSprite;
        }
    }

    /// <summary>
    /// Layered Cocos2d-style field player built from PlayerBody3 + PlayerHead assets,
    /// supporting dynamic kit colors, skin tones, GK variants, run cycle (00..21), kick cycle (01..07),
    /// and yellow/red card badges.
    /// </summary>
    public class LayeredPlayerRig : MonoBehaviour
    {
        private bool _isGoalkeeper;
        private int _currentRunFrame = -1;
        private int _currentKickFrame = -1;

        private Image _secShorts;
        private Image _secArm;
        private Image _secShoe;
        private Image _secSleeve;
        private Image _domShorts;
        private Image _domSleeve;
        private Image _domArm;
        private Image _domShoe;
        private Image _gkGloves;
        private Image _cardBadge;

        public static LayeredPlayerRig Attach(
            GameObject host,
            PlayerData player,
            MatchKitAppearance kit,
            bool isGoalkeeper,
            int fallbackSlot)
        {
            var rig = host.AddComponent<LayeredPlayerRig>();
            rig.Build(player, kit, isGoalkeeper, fallbackSlot);
            return rig;
        }

        private static bool IsJuniorPlayer(PlayerData player, int fallbackSlot)
        {
            int pid = (player != null && player.id > 0) ? player.id : fallbackSlot;
            // Exact PlayerSprite.initWithParent: (0x65f16..0x65f24): bitmask 0x5e => IDs 1, 2, 3, 4, 6
            return pid >= 1 && pid <= 6 && ((1 << pid) & 0x5e) != 0;
        }

        private static RectTransform CreateContainer(string name, Transform parent, Vector2 anchoredPos, float uniformScale)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(140f, 180f);
            rect.localScale = new Vector3(uniformScale, uniformScale, 1f);
            return rect;
        }

        private void Build(PlayerData player, MatchKitAppearance kit, bool isGoalkeeper, int fallbackSlot)
        {
            _isGoalkeeper = isGoalkeeper;
            bool isFemale = player != null && player.gender == 1;
            bool isJunior = IsJuniorPlayer(player, fallbackSlot);
            Color32 skinColor = PlayerBodySprites.GetSkinColor(player != null ? player.skinColour : 1);

            const string b3 = "Original/sprites/PlayerBody3/";
            string prefix = (isGoalkeeper ? "GK_" : string.Empty) + (isFemale ? "Female" : "Male");

            // Canvas uses 640x960 Retina 2x space (= 2px per Cocos2d point), so 1.0x root scale matches iOS proportions.
            // Junior kids use scale=0.7f on body1/body2 and shifted head/body offsets from ARMv7 PlayerSprite.initWithParent:
            float bodyScale = isJunior ? 0.7f : 1.0f;
            Vector2 bodyPos = isJunior ? new Vector2(0f, -59f) : new Vector2(0f, -35f);     // (0, -29.5) / (0, -17.5) pts
            Vector2 headPos = isJunior ? new Vector2(4f, 8f) : new Vector2(4f, 56f);        // (2, +4) / (2, +28) pts
            float groundOffsetY = -(bodyPos.y + (-46f * bodyScale));                        // Align Shadow center (0, -23pts) to y=0

            var rigRoot = new GameObject("PlayerBodyRig", typeof(RectTransform));
            rigRoot.transform.SetParent(transform, false);
            var rootRect = rigRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(0f, groundOffsetY);
            rootRect.sizeDelta = new Vector2(140f, 200f);
            rootRect.localScale = Vector3.one;

            // Cocos2d PlayerSprite hierarchy: body2 (z=0) -> head (z=1) -> body1 (z=2)
            var body2 = CreateContainer("BodyBack", rigRoot.transform, bodyPos, bodyScale);

            // Body2 back layers (all sizes in exact Retina 2x PNG pixels, positions = Cocos2d pts * 2):
            if (!isGoalkeeper)
            {
                _secSleeve = PlayerBodySprites.CreateLayer("SecondarySleeve", body2, b3 + "SecondarySleeve00",
                    kit.SleeveColor, new Vector2(10f, 34f), new Vector2(60f, 50f));
            }
            PlayerBodySprites.CreateLayer("Shadow", body2, b3 + "Shadow",
                new Color32(255, 255, 255, 220), new Vector2(0f, -46f), new Vector2(126f, 84f));
            _secShorts = PlayerBodySprites.CreateLayer("SecondaryShorts", body2,
                b3 + (isGoalkeeper ? "GK_SecondaryShorts" : "SecondaryShorts00"),
                kit.PantsColor, isGoalkeeper ? new Vector2(3f, 7f) : new Vector2(-4f, -25f),
                isGoalkeeper ? new Vector2(104f, 130f) : new Vector2(84f, 66f));
            _secArm = PlayerBodySprites.CreateLayer("SecondaryArm", body2,
                b3 + (isGoalkeeper ? "GK_SecondaryArmOutline" : "SecondaryArmOutline00"),
                skinColor, new Vector2(3f, 7f), new Vector2(104f, 130f));
            _secShoe = PlayerBodySprites.CreateLayer("SecondaryShoe", body2,
                b3 + (isGoalkeeper ? "GK_SecondaryShoe" : "SecondaryShoe00"),
                kit.SleeveColor, new Vector2(1f, -38f), new Vector2(126f, 84f));

            // Torso layers at (-2, +7) Cocos2d pts = (-4, +14) Retina px:
            Vector2 torsoPos = new Vector2(-4f, 14f);
            Vector2 torsoSize = new Vector2(64f, 94f);
            PlayerBodySprites.CreateLayer("Butt", body2, b3 + prefix + "ButtColour",
                kit.PantsColor, torsoPos, torsoSize);
            PlayerBodySprites.CreateLayer("Shirt", body2, b3 + prefix + "ShirtColour",
                kit.ShirtColor, torsoPos, torsoSize);
            if (kit.PatternStyle > 0 && kit.PatternStyle <= 7)
            {
                PlayerBodySprites.CreateLayer("Design", body2, $"{b3}{prefix}Design{kit.PatternStyle}",
                    kit.PatternColor, torsoPos, torsoSize);
            }
            PlayerBodySprites.CreateLayer("BodyOutline", body2, b3 + prefix + "BodyOutline",
                skinColor, torsoPos, torsoSize);

            // Head (z=1 relative to root, scale=1.0f -> full-size expressive caricature head)
            var headImg = PlayerBodySprites.CreateLayer("Head", rigRoot.transform,
                PlayerBodySprites.ResolveHeadPath(player, fallbackSlot),
                new Color32(255, 255, 255, 255), headPos, new Vector2(92f, 72f));
            float headHeight = 72f;
            if (headImg.sprite != null)
            {
                headImg.rectTransform.sizeDelta = headImg.sprite.rect.size;
                headHeight = headImg.sprite.rect.size.y;
            }

            // Body1 front layers (z=2 relative to root -> overlays neck/shoulder overlap cleanly)
            var body1 = CreateContainer("BodyFront", rigRoot.transform, bodyPos, bodyScale);
            _domShorts = PlayerBodySprites.CreateLayer("DominantShorts", body1,
                b3 + (isGoalkeeper ? "GK_DominantShorts" : "DominantShorts00"),
                kit.PantsColor, isGoalkeeper ? new Vector2(6f, 4f) : new Vector2(-1f, -29f),
                isGoalkeeper ? new Vector2(104f, 134f) : new Vector2(84f, 66f));
            _domSleeve = PlayerBodySprites.CreateLayer("DominantSleeve", body1,
                b3 + (isGoalkeeper ? "GK_DominantSleeve" : "DominantSleeve00"),
                kit.SleeveColor, isGoalkeeper ? new Vector2(6f, 4f) : new Vector2(10f, 33f),
                isGoalkeeper ? new Vector2(104f, 134f) : new Vector2(60f, 50f));
            _domArm = PlayerBodySprites.CreateLayer("DominantArm", body1,
                b3 + (isGoalkeeper ? "GK_DominantArmOutline" : "DominantArmOutline00"),
                skinColor, new Vector2(6f, 4f), new Vector2(104f, 134f));

            if (isGoalkeeper)
            {
                _gkGloves = PlayerBodySprites.CreateLayer("GKGloves", body1, b3 + "GK_GloveColour",
                    new Color32(250, 210, 60, 255), new Vector2(6f, 4f), new Vector2(104f, 134f));
            }

            _domShoe = PlayerBodySprites.CreateLayer("DominantShoe", body1,
                b3 + (isGoalkeeper ? "GK_DominantShoe" : "DominantShoe00"),
                kit.SleeveColor, new Vector2(1f, -38f), new Vector2(126f, 84f));

            // Card status icon floating cleanly right above head top
            _cardBadge = new GameObject("CardBadge", typeof(RectTransform), typeof(Image), typeof(Outline))
                .GetComponent<Image>();
            _cardBadge.transform.SetParent(rigRoot.transform, false);
            _cardBadge.sprite = PlayerBodySprites.WhitePixel();
            _cardBadge.raycastTarget = false;
            var cardRect = _cardBadge.rectTransform;
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(-10f, headPos.y + headHeight * 0.5f + 16f);
            cardRect.sizeDelta = new Vector2(14f, 19f);
            cardRect.localEulerAngles = new Vector3(0f, 0f, 10f);
            var cardOutline = _cardBadge.GetComponent<Outline>();
            cardOutline.effectColor = Color.black;
            cardOutline.effectDistance = new Vector2(1.5f, -1.5f);
            _cardBadge.enabled = false;

            SetIdlePose();
        }

        public void SetCardState(bool yellow, bool red)
        {
            if (_cardBadge == null) return;
            if (red)
            {
                _cardBadge.color = new Color32(235, 35, 35, 255);
                _cardBadge.enabled = true;
            }
            else if (yellow)
            {
                _cardBadge.color = new Color32(255, 215, 25, 255);
                _cardBadge.enabled = true;
            }
            else
            {
                _cardBadge.enabled = false;
            }
        }

        public void SetIdlePose()
        {
            if (_currentRunFrame == 0 && _currentKickFrame == 0) return;
            _currentRunFrame = 0;
            _currentKickFrame = 0;
            ApplyFrame("00", isKick: false);
        }

        public void SetRunFrame(int frameIndex)
        {
            int idx = ((frameIndex % 22) + 22) % 22;
            if (_currentKickFrame == 0 && _currentRunFrame == idx) return;
            _currentRunFrame = idx;
            _currentKickFrame = 0;
            ApplyFrame($"{idx:00}", isKick: false);
        }

        public void SetKickFrame(int frameIndex)
        {
            int idx = Mathf.Clamp(frameIndex, 1, 7);
            if (_currentKickFrame == idx) return;
            _currentKickFrame = idx;
            _currentRunFrame = -1;
            ApplyFrame($"{idx:00}", isKick: true);
        }

        private static void SetLayerPose(Image img, Vector2 pos, Vector2 size, string spritePath, bool enabled = true)
        {
            if (img == null) return;
            img.rectTransform.anchoredPosition = pos;
            img.rectTransform.sizeDelta = size;
            img.sprite = PlayerBodySprites.LoadSprite(spritePath);
            img.enabled = enabled && img.sprite != null;
        }

        private void ApplyFrame(string frameSuffix, bool isKick)
        {
            const string b3 = "Original/sprites/PlayerBody3/";
            if (_isGoalkeeper && !isKick && frameSuffix == "00")
            {
                SetLayerPose(_secShorts, new Vector2(3f, 7f), new Vector2(104f, 130f), b3 + "GK_SecondaryShorts");
                SetLayerPose(_secArm, new Vector2(3f, 7f), new Vector2(104f, 130f), b3 + "GK_SecondaryArmOutline");
                SetLayerPose(_secShoe, new Vector2(1f, -38f), new Vector2(126f, 84f), b3 + "GK_SecondaryShoe");
                if (_secSleeve != null) _secSleeve.enabled = false;

                SetLayerPose(_domShorts, new Vector2(6f, 4f), new Vector2(104f, 134f), b3 + "GK_DominantShorts");
                SetLayerPose(_domSleeve, new Vector2(6f, 4f), new Vector2(104f, 134f), b3 + "GK_DominantSleeve");
                SetLayerPose(_domArm, new Vector2(6f, 4f), new Vector2(104f, 134f), b3 + "GK_DominantArmOutline");
                SetLayerPose(_domShoe, new Vector2(1f, -38f), new Vector2(126f, 84f), b3 + "GK_DominantShoe");
                if (_gkGloves != null) _gkGloves.enabled = true;
                return;
            }

            if (isKick)
            {
                SetLayerPose(_secShorts, new Vector2(-4f, -25f), new Vector2(84f, 66f), b3 + "KickSecondaryShorts" + frameSuffix);
                SetLayerPose(_secShoe, new Vector2(1f, -38f), new Vector2(126f, 84f), b3 + "KickSecondaryShoe" + frameSuffix);
                SetLayerPose(_secArm, new Vector2(3f, 7f), new Vector2(104f, 130f), b3 + "SecondaryArmOutline00");
                SetLayerPose(_secSleeve, new Vector2(10f, 34f), new Vector2(60f, 50f), b3 + "SecondarySleeve00");
                SetLayerPose(_domShorts, new Vector2(-1f, -29f), new Vector2(84f, 66f), b3 + "DominantShorts00");
                SetLayerPose(_domSleeve, new Vector2(10f, 33f), new Vector2(60f, 50f), b3 + "KickDominantSleeve" + frameSuffix);
                SetLayerPose(_domArm, new Vector2(6f, 4f), new Vector2(104f, 134f), b3 + "KickDominantArmOutline" + frameSuffix);
                SetLayerPose(_domShoe, new Vector2(1f, -38f), new Vector2(126f, 84f), b3 + "DominantShoe00");
            }
            else
            {
                SetLayerPose(_secShorts, new Vector2(-4f, -25f), new Vector2(84f, 66f), b3 + "SecondaryShorts" + frameSuffix);
                SetLayerPose(_secArm, new Vector2(3f, 7f), new Vector2(104f, 130f), b3 + "SecondaryArmOutline" + frameSuffix);
                SetLayerPose(_secShoe, new Vector2(1f, -38f), new Vector2(126f, 84f), b3 + "SecondaryShoe" + frameSuffix);
                SetLayerPose(_secSleeve, new Vector2(10f, 34f), new Vector2(60f, 50f), b3 + "SecondarySleeve" + frameSuffix);
                SetLayerPose(_domShorts, new Vector2(-1f, -29f), new Vector2(84f, 66f), b3 + "DominantShorts" + frameSuffix);
                SetLayerPose(_domSleeve, new Vector2(10f, 33f), new Vector2(60f, 50f), b3 + "DominantSleeve" + frameSuffix);
                SetLayerPose(_domArm, new Vector2(6f, 4f), new Vector2(104f, 134f), b3 + "DominantArmOutline" + frameSuffix);
                SetLayerPose(_domShoe, new Vector2(1f, -38f), new Vector2(126f, 84f), b3 + "DominantShoe" + frameSuffix);
            }

            if (_gkGloves != null) _gkGloves.enabled = false;
        }
    }
}
