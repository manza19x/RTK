using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FutbolTotal
{
    /// <summary>
    /// Efecto de los botones: al pasar el ratón o seleccionarlos con mando/teclado crecen un poco, aparece la barra
    /// de acento y el texto pasa a oscuro sobre el fondo turquesa. Usa tiempo sin escalar (funciona en pausa).
    /// </summary>
    public class UIButtonFx : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private Text label;
        private Image accent;
        private bool selected;
        private bool hovered;
        private float amount;

        public void Setup(Text text, Image accentBar)
        {
            label = text;
            accent = accentBar;
            Apply();
        }

        public void OnSelect(BaseEventData eventData) => selected = true;
        public void OnDeselect(BaseEventData eventData) => selected = false;
        public void OnPointerEnter(PointerEventData eventData) => hovered = true;
        public void OnPointerExit(PointerEventData eventData) => hovered = false;

        private void OnDisable()
        {
            selected = false;
            hovered = false;
            amount = 0f;
            Apply();
        }

        private void Update()
        {
            float target = selected || hovered ? 1f : 0f;
            amount = Mathf.MoveTowards(amount, target, Time.unscaledDeltaTime * 8f);
            Apply();
        }

        private void Apply()
        {
            float eased = amount * amount * (3f - 2f * amount);
            transform.localScale = Vector3.one * (1f + 0.035f * eased);
            if (label != null) label.color = Color.Lerp(UITheme.Text, UITheme.TextDark, eased);
            if (accent != null) accent.color = new Color(UITheme.TextDark.r, UITheme.TextDark.g, UITheme.TextDark.b, eased);
        }
    }
}
