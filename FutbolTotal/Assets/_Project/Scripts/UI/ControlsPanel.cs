using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FutbolTotal
{
    /// <summary>Pantalla de controles (teclado y mando). La usan el menú principal, la pausa y el HUD (F1).</summary>
    public static class ControlsPanel
    {
        private static readonly string[,] Rows =
        {
            { "Mover", "W A S D", "Stick izquierdo" },
            { "Sprint (gasta resistencia)", "Shift izquierdo", "RT / R2" },
            { "Pase corto  ·  sin balón: entrada", "J", "A / Cruz" },
            { "Pase largo / centro", "L", "X / Cuadrado" },
            { "Tiro", "K", "B / Círculo" },
            { "Cambiar de jugador", "Q", "LB / L1" },
            { "Pausa", "Esc", "Start" },
            { "Ver controles", "F1", "R3" },
            { "Ver el rol de la IA", "F2", "L3" },
        };

        /// <summary>Crea el panel (oculto no: el que llama decide cuándo activarlo).</summary>
        public static GameObject Build(Transform parent, UnityAction onClose, out Button closeButton)
        {
            RectTransform root = UIFactory.Stretch("ControlsPanel", parent);
            UIFactory.AddImage(root, UITheme.Backdrop, raycast: true);

            RectTransform box = UIFactory.Anchored("Box", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1160f, 800f));
            UIFactory.AddImage(box, UITheme.Panel);

            RectTransform title = UIFactory.Anchored("Title", box, new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(1000f, 70f));
            UIFactory.AddText(title, "CONTROLES", 46, UITheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);

            float[] columnX = { -330f, 150f, 420f };
            float[] columnWidth = { 480f, 260f, 260f };
            string[] headers = { "Acción", "Teclado", "Mando" };
            float y = -130f;
            for (int c = 0; c < 3; c++)
            {
                RectTransform header = UIFactory.Anchored("Header" + c, box, new Vector2(0.5f, 1f), new Vector2(columnX[c], y), new Vector2(columnWidth[c], 40f));
                UIFactory.AddText(header, headers[c], 26, UITheme.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);
            }

            for (int r = 0; r < Rows.GetLength(0); r++)
            {
                y -= 52f;
                if (r % 2 == 0)
                {
                    RectTransform stripe = UIFactory.Anchored("Stripe" + r, box, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(1080f, 48f));
                    UIFactory.AddImage(stripe, new Color(1f, 1f, 1f, 0.04f));
                }
                for (int c = 0; c < 3; c++)
                {
                    RectTransform cell = UIFactory.Anchored($"Cell{r}_{c}", box, new Vector2(0.5f, 1f), new Vector2(columnX[c], y), new Vector2(columnWidth[c], 48f));
                    UIFactory.AddText(cell, Rows[r, c], 27, c == 0 ? UITheme.Text : UITheme.Accent, TextAnchor.MiddleLeft,
                                      c == 0 ? FontStyle.Normal : FontStyle.Bold);
                }
            }

            RectTransform notes = UIFactory.Anchored("Notes", box, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1060f, 90f));
            UIFactory.AddText(notes,
                "Mantén pase o tiro para cargar la potencia: la acción se hace al soltar.\n" +
                "Pulsa pase o tiro justo antes de recibir para golpear al primer toque.",
                24, UITheme.TextDim);

            RectTransform buttonHolder = UIFactory.Anchored("CloseHolder", box, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(360f, 64f));
            closeButton = UIFactory.Button(buttonHolder, "Volver", onClose, 360f, 64f);
            ((RectTransform)closeButton.transform).anchoredPosition = Vector2.zero;
            return root.gameObject;
        }
    }
}
