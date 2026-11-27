using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FutbolTotal
{
    /// <summary>
    /// Estilo visual de la interfaz (tipo retransmisión): paneles oscuros translúcidos, acento turquesa y blanco.
    /// Cambiar aquí cambia todos los menús.
    /// </summary>
    public static class UITheme
    {
        public static readonly Color Backdrop = new Color(0.01f, 0.02f, 0.05f, 0.72f);
        public static readonly Color Panel = new Color(0.04f, 0.06f, 0.11f, 0.94f);
        public static readonly Color PanelSoft = new Color(0.04f, 0.06f, 0.11f, 0.78f);
        public static readonly Color PanelLight = new Color(1f, 1f, 1f, 0.07f);
        public static readonly Color Accent = new Color(0.05f, 0.88f, 0.68f);
        public static readonly Color AccentDark = new Color(0.02f, 0.45f, 0.36f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.25f);
        public static readonly Color Text = Color.white;
        public static readonly Color TextDim = new Color(0.68f, 0.73f, 0.8f);
        public static readonly Color TextDark = new Color(0.03f, 0.05f, 0.08f);
        public static readonly Color Button = new Color(1f, 1f, 1f, 0.06f);
        public static readonly Color ButtonHighlight = new Color(0.05f, 0.88f, 0.68f, 1f);
        public static readonly Color ButtonPressed = new Color(0.02f, 0.65f, 0.5f, 1f);
        public static readonly Color BarBackground = new Color(1f, 1f, 1f, 0.12f);
        public static readonly Color Live = new Color(1f, 0.25f, 0.3f);
    }

    /// <summary>
    /// Construye la interfaz por código (uGUI). Decisión: sin prefabs ni referencias guardadas en escena, así la
    /// interfaz no se rompe al regenerar escenas y todo su aspecto se ajusta desde aquí y desde <see cref="UITheme"/>.
    /// Usa el texto clásico de uGUI con la fuente integrada para no depender de importar recursos de TextMeshPro.
    /// </summary>
    public static class UIFactory
    {
        private static Font font;

        /// <summary>
        /// Fuente de la interfaz: Bahnschrift (condensada, de marcador deportivo; viene con Windows 10/11) o, si no
        /// existe, otras del sistema; como último recurso, la integrada de Unity.
        /// </summary>
        public static Font Font
        {
            get
            {
                if (font != null) return font;
                string[] installed = Font.GetOSInstalledFontNames();
                string[] preferred = { "Bahnschrift", "Segoe UI", "Arial", "Helvetica" };
                foreach (string name in preferred)
                {
                    if (System.Array.IndexOf(installed, name) >= 0)
                    {
                        font = Font.CreateDynamicFontFromOSFont(name, 32);
                        break;
                    }
                }
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

        /// <summary>Imagen con un gráfico de <see cref="UISprites"/>; los redondeados se estiran sin deformar esquinas.</summary>
        public static Image AddSprite(RectTransform rect, Sprite sprite, Color color, bool sliced = true, bool raycast = false)
        {
            Image image = AddImage(rect, color, raycast);
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            return image;
        }

        /// <summary>Sombra suave bajo un texto para que se lea sobre cualquier fondo.</summary>
        public static void AddTextShadow(Text text, float distance = 2f, float alpha = 0.6f)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
            shadow.effectDistance = new Vector2(distance, -distance);
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
            Image image = AddSprite(rect, UISprites.Rounded, Color.white, raycast: true);
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
            button.onClick.AddListener(UISound.PlayClick);
            if (onClick != null) button.onClick.AddListener(onClick);

            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;

            // Barra de acento a la izquierda (visible al seleccionar) y texto que se oscurece sobre el acento.
            RectTransform bar = Rect("Accent", rect, new Vector2(0f, 0.18f), new Vector2(0f, 0.82f), new Vector2(0f, 0.5f),
                                     new Vector2(10f, 0f), new Vector2(5f, 0f));
            Image accent = AddSprite(bar, UISprites.Pill, UITheme.TextDark);
            Text label = Label("Text", rect, text.ToUpperInvariant(), fontSize, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            rect.gameObject.AddComponent<UIButtonFx>().Setup(label, accent);
            return button;
        }

        public static void SetButtonText(Button button, string text)
        {
            Text label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = text.ToUpperInvariant();
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
            AddSprite(background, UISprites.RoundedSmall, UITheme.BarBackground);
            RectTransform fill = Rect("Fill", background, Vector2.zero, new Vector2(1f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            fillImage = AddSprite(fill, UISprites.RoundedSmall, fillColor);
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
