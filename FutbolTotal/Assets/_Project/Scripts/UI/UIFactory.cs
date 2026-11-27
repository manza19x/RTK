using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FutbolTotal
{
    /// <summary>Colores de la interfaz temporal. Cambiar aquí cambia todos los menús.</summary>
    public static class UITheme
    {
        public static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.6f);
        public static readonly Color Panel = new Color(0.06f, 0.08f, 0.12f, 0.92f);
        public static readonly Color PanelSoft = new Color(0.06f, 0.08f, 0.12f, 0.7f);
        public static readonly Color Accent = new Color(1f, 0.84f, 0.2f);
        public static readonly Color Text = Color.white;
        public static readonly Color TextDim = new Color(0.75f, 0.78f, 0.84f);
        public static readonly Color Button = new Color(0.16f, 0.21f, 0.3f, 0.95f);
        public static readonly Color ButtonHighlight = new Color(0.95f, 0.75f, 0.15f, 1f);
        public static readonly Color ButtonPressed = new Color(0.8f, 0.6f, 0.1f, 1f);
        public static readonly Color BarBackground = new Color(1f, 1f, 1f, 0.15f);
    }

    /// <summary>
    /// Construye la interfaz por código (uGUI). Decisión: sin prefabs ni referencias guardadas en escena, así la
    /// interfaz no se rompe al regenerar escenas y todo su aspecto se ajusta desde aquí y desde <see cref="UITheme"/>.
    /// Usa el texto clásico de uGUI con la fuente integrada para no depender de importar recursos de TextMeshPro.
    /// </summary>
    public static class UIFactory
    {
        private static Font font;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return canvas;
        }

        /// <summary>Crea el EventSystem con el módulo de UI del Input System (ratón, teclado y mando) si no existe.</summary>
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null || Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        /// <summary>RectTransform hijo con anclas, pivote, posición y tamaño.</summary>
        public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                                         Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>RectTransform que ocupa todo el padre.</summary>
        public static RectTransform Stretch(string name, Transform parent)
        {
            RectTransform rect = Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>Rectángulo centrado en un punto del padre (ancla = pivote).</summary>
        public static RectTransform Anchored(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            return Rect(name, parent, anchor, anchor, anchor, position, size);
        }

        public static Image AddImage(RectTransform rect, Color color, bool raycast = false)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static Text AddText(RectTransform rect, string text, int size, Color color,
                                   TextAnchor alignment = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = style;
            label.supportRichText = true;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>Texto que ocupa todo el rectángulo indicado.</summary>
        public static Text Label(string name, Transform parent, string text, int size, Color color,
                                 TextAnchor alignment = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            return AddText(Stretch(name, parent), text, size, color, alignment, style);
        }

        /// <summary>Botón con texto. Si el padre tiene un LayoutGroup, usa width/height como tamaño preferido.</summary>
        public static Button Button(Transform parent, string text, UnityAction onClick, float width = 460f, float height = 64f,
                                    int fontSize = 30)
        {
            RectTransform rect = Anchored("Button_" + text, parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height));
            Image image = AddImage(rect, Color.white, raycast: true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = UITheme.Button;
            colors.highlightedColor = UITheme.ButtonHighlight;
            colors.selectedColor = UITheme.ButtonHighlight;
            colors.pressedColor = UITheme.ButtonPressed;
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);

            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;

            Label("Text", rect, text, fontSize, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            return button;
        }

        public static void SetButtonText(Button button, string text)
        {
            Text label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = text;
        }

        /// <summary>Lista vertical centrada (para columnas de botones).</summary>
        public static RectTransform VerticalList(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, float spacing)
        {
            RectTransform rect = Anchored(name, parent, anchor, position, size);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return rect;
        }

        /// <summary>Barra horizontal. Devuelve el relleno: se ajusta con <see cref="SetBar"/>.</summary>
        public static RectTransform Bar(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color fillColor,
                                        out Image fillImage)
        {
            RectTransform background = Anchored(name, parent, anchor, position, size);
            AddImage(background, UITheme.BarBackground);
            RectTransform fill = Rect("Fill", background, Vector2.zero, new Vector2(1f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            fillImage = AddImage(fill, fillColor);
            return fill;
        }

        public static void SetBar(RectTransform fill, float value01)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(value01), 1f);
        }

        /// <summary>Quita la selección (al cerrar un menú), para que Intro o A no activen un botón oculto.</summary>
        public static void ClearSelection()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        /// <summary>Selecciona un botón para poder navegar con mando o teclado.</summary>
        public static void Select(Selectable selectable)
        {
            if (selectable == null) return;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            selectable.Select();
        }
    }
}
