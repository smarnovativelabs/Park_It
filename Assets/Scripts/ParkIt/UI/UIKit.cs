using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ParkIt
{
    /// <summary>Theme colours and widget builders. Every graphic is a sprite from <see cref="UISpriteAtlas"/>.</summary>
    public static class UIKit
    {
        // ---------- theme ----------
        public static readonly Color White = Color.white;
        public static readonly Color Ink = C("#350c5c");
        public static readonly Color TextMain = C("#350c5c");
        public static readonly Color TextSoft = C("#491471");
        public static readonly Color TextMuted = C("#501878");
        public static readonly Color TextDim = C("#71368f");
        public static readonly Color Orange = C("#d36c00");
        public static readonly Color Amber = C("#bb6500");
        public static readonly Color Emerald = C("#16703b");
        public static readonly Color Sky = C("#087699");
        public static readonly Color Rose = C("#ff3e67");
        public static readonly Color Backdrop = new Color(.22f, .08f, .38f, .72f);

        public static Color C(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        // ---------- fonts ----------
        private static Font sans;
        public static Font Sans => sans != null ? sans : (sans = Resources.Load<Font>("Fonts/LilitaOne-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        public static Font Mono => Sans;
        public static Font Heavy => Sans;

        private static Font OsFont(string[] names)
        {
            try
            {
                var f = Font.CreateDynamicFontFromOSFont(names, 32);
                if (f != null) return f;
            }
            catch (System.Exception) { }
            return Sans;
        }

        // ---------- layout primitives ----------
        public static RectTransform Rect(string name, Transform parent)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float inset = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static LayoutElement Size(Component c, float w = -1, float h = -1, float flexW = -1, float flexH = -1)
        {
            var le = c.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = w; le.minWidth = w;
            le.preferredHeight = h; le.minHeight = h;
            le.flexibleWidth = flexW; le.flexibleHeight = flexH;
            return le;
        }

        public static HorizontalOrVerticalLayoutGroup Layout(Component c, bool vertical, float spacing, RectOffset padding = null,
            TextAnchor align = TextAnchor.MiddleCenter, bool expandW = false, bool expandH = false)
        {
            var go = c.gameObject;
            var existing = go.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (existing != null && (existing is VerticalLayoutGroup) != vertical) Object.DestroyImmediate(existing);
            HorizontalOrVerticalLayoutGroup g = go.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (g == null) g = vertical ? (HorizontalOrVerticalLayoutGroup)go.AddComponent<VerticalLayoutGroup>() : go.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.padding = padding ?? new RectOffset();
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = expandW;
            g.childForceExpandHeight = expandH;
            return g;
        }

        public static ContentSizeFitter Fit(Component c, bool horizontal = true, bool vertical = true)
        {
            var f = c.GetComponent<ContentSizeFitter>() ?? c.gameObject.AddComponent<ContentSizeFitter>();
            f.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            f.verticalFit = vertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            return f;
        }

        // ---------- graphics ----------
        public static Image Img(string name, Transform parent, string sprite, Color? tint = null, bool raycast = false)
        {
            var img = Rect(name, parent).gameObject.AddComponent<Image>();
            SetSprite(img, sprite);
            img.color = tint ?? White;
            // These atlas sprites already contain their outline and candy colours.
            if (sprite == "icon_arrow_up" || sprite == "icon_arrow_down" || sprite == "icon_turn_left" || sprite == "icon_turn_right"
                || sprite == "icon_undo" || sprite == "icon_restart" || sprite == "icon_camera" || sprite == "icon_fuel" || sprite == "icon_gear" || sprite == "icon_sound_on" || sprite == "icon_sound_off") img.color = White;
            img.raycastTarget = raycast;
            return img;
        }

        public static void SetSprite(Image img, string sprite)
        {
            if (string.IsNullOrEmpty(sprite)) { img.sprite = null; return; }
            var s = UISpriteAtlas.Get(sprite);
            img.sprite = s;
            img.type = s != null && s.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = img.type == Image.Type.Simple;
        }

        public static Image Icon(Transform parent, string icon, Color tint, float size)
        {
            var img = Img(icon, parent, icon, tint);
            Size(img, size, size);
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, Font font = null, FontStyle style = FontStyle.Normal,
            TextAnchor align = TextAnchor.MiddleLeft)
        {
            var t = Rect("Text", parent).gameObject.AddComponent<Text>();
            t.font = font ?? Sans;
            t.fontSize = size;
            t.fontStyle = FontStyle.Normal;
            t.color = color;
            t.text = text;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            return t;
        }

        public static Text Bold(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
            => Label(parent, text, size, color, Sans, FontStyle.Bold, align);

        public static Text Caps(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
            => Label(parent, text, size, color, Mono, FontStyle.Bold, align);

        // ---------- interactive ----------
        public static Button Button(string name, Transform parent, string sprite, UnityAction onClick)
        {
            var img = Img(name, parent, sprite, string.IsNullOrEmpty(sprite) ? new Color(1, 1, 1, 0) : White, true);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.normalColor = White;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
            colors.pressedColor = new Color(.78f, .78f, .78f);
            colors.selectedColor = White;
            colors.disabledColor = new Color(.6f, .6f, .6f, .55f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = .08f;
            b.colors = colors;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            if (onClick != null) b.onClick.AddListener(onClick);
            b.onClick.AddListener(() => SfxPlayer.Play(Sfx.Click));
            return b;
        }

        /// <summary>Button with an icon followed by a label, laid out horizontally.</summary>
        public static Button IconTextButton(string name, Transform parent, string sprite, string icon, Color iconColor, string text, Color textColor,
            int fontSize, float height, UnityAction onClick, float iconSize = -1, int padX = 28)
        {
            var b = Button(name, parent, sprite, onClick);
            Layout(b, false, fontSize * .55f, new RectOffset(padX, padX, 0, 0));
            Size(b, -1, height);
            if (!string.IsNullOrEmpty(icon)) Icon(b.transform, icon, iconColor, iconSize > 0 ? iconSize : fontSize * 1.15f);
            if (!string.IsNullOrEmpty(text))
            {
                var t = Bold(b.transform, text, fontSize, textColor, TextAnchor.MiddleCenter);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            return b;
        }

        public static Button IconButton(string name, Transform parent, string sprite, string icon, Color iconColor, float size, float iconSize, UnityAction onClick)
        {
            var b = Button(name, parent, sprite, onClick);
            Size(b, size, size);
            var i = Img("Icon", b.transform, icon, iconColor);
            Place(i.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(iconSize, iconSize));
            return b;
        }

        /// <summary>Rounded difficulty badge ("Beginner", "Expert", ...).</summary>
        public static Image Badge(Transform parent, LevelDifficulty difficulty, int fontSize)
        {
            string sprite; Color color;
            switch (difficulty)
            {
                case LevelDifficulty.Beginner: sprite = "badge_green"; color = Emerald; break;
                case LevelDifficulty.Intermediate: sprite = "badge_blue"; color = Sky; break;
                case LevelDifficulty.Expert: sprite = "badge_amber"; color = Amber; break;
                case LevelDifficulty.Master: sprite = "badge_red"; color = Rose; break;
                default: sprite = "badge_slate"; color = TextSoft; break;
            }
            var img = Img("Badge", parent, sprite);
            Layout(img, false, 0, new RectOffset(Mathf.RoundToInt(fontSize * .8f), Mathf.RoundToInt(fontSize * .8f), Mathf.RoundToInt(fontSize * .3f), Mathf.RoundToInt(fontSize * .3f)));
            Bold(img.transform, difficulty.ToString(), fontSize, color, TextAnchor.MiddleCenter).horizontalOverflow = HorizontalWrapMode.Overflow;
            return img;
        }

        public static void SetBadge(Image badge, LevelDifficulty difficulty)
        {
            if (badge == null) return;
            string sprite; Color color;
            switch (difficulty)
            {
                case LevelDifficulty.Beginner: sprite = "badge_green"; color = Emerald; break;
                case LevelDifficulty.Intermediate: sprite = "badge_blue"; color = Sky; break;
                case LevelDifficulty.Expert: sprite = "badge_amber"; color = Amber; break;
                case LevelDifficulty.Master: sprite = "badge_red"; color = Rose; break;
                default: sprite = "badge_slate"; color = TextSoft; break;
            }
            SetSprite(badge, sprite);
            var t = badge.GetComponentInChildren<Text>();
            t.text = difficulty.ToString();
            t.color = color;
        }

        /// <summary>Full-screen dimmer that swallows clicks behind modals.</summary>
        public static Image Dimmer(Transform parent, UnityAction onClick = null)
        {
            var img = Img("Backdrop", parent, "white", Backdrop, true);
            Stretch(img.rectTransform);
            if (onClick != null)
            {
                var b = img.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(onClick);
            }
            return img;
        }

        public static string Arrow(Direction d)
        {
            switch (d)
            {
                case Direction.North: return "↑";
                case Direction.East: return "→";
                case Direction.South: return "↓";
                default: return "←";
            }
        }
    }
}
