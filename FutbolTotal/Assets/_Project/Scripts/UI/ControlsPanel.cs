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
            { "Tiro (mantén para más potencia)", "K", "B / Círculo" },
            { "Cambiar de jugador", "Q", "LB / L1" },
            { "Pausa", "Esc", "Start" },
            { "Ver controles", "F1", "R3" },
            { "Ver el rol de la IA", "F2", "L3" },
        };

        public static GameObject Build(Transform parent, UnityAction onClose, out Button closeButton)
        {
            RectTransform root = UIFactory.Stretch("ControlsPanel", parent);
            UIFactory.AddImage(root, UITheme.Backdrop, raycast: true);

            RectTransform box = UIFactory.Anchored("Box", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 820f));
            UIFactory.AddSprite(box, UISprites.Rounded, UITheme.Panel);
            RectTransform header = UIFactory.Rect("Header", box, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 84f));
            UIFactory.AddSprite(header, UISprites.Rounded, UITheme.PanelLight);
            UIFactory.AddText(UIFactory.Rect("Title", header, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(50f, 0f), new Vector2(-100f, 0f)),
                              "CONTROLES", 44, UITheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            RectTransform accent = UIFactory.Rect("Accent", header, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(50f, 0f), new Vector2(140f, 5f));
            UIFactory.AddSprite(accent, UISprites.Pill, UITheme.Accent);

            float[] columnX = { -320f, 170f, 430f };
            float[] columnWidth = { 500f, 240f, 260f };
            string[] headers = { "ACCIÓN", "TECLADO", "MANDO" };
            float y = -130f;
            for (int c = 0; c < 3; c++)
            {
                RectTransform h = UIFactory.Anchored("Header" + c, box, new Vector2(0.5f, 1f), new Vector2(columnX[c], y), new Vector2(columnWidth[c], 36f));
                UIFactory.AddText(h, headers[c], 20, UITheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            }

            for (int r = 0; r < Rows.GetLength(0); r++)
            {
                y -= 56f;
                RectTransform stripe = UIFactory.Anchored("Row" + r, box, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(1100f, 50f));
                UIFactory.AddSprite(stripe, UISprites.RoundedSmall, r % 2 == 0 ? UITheme.PanelLight : new Color(1f, 1f, 1f, 0.02f));
                for (int c = 0; c < 3; c++)
                {
                    RectTransform cell = UIFactory.Anchored($"Cell{r}_{c}", box, new Vector2(0.5f, 1f), new Vector2(columnX[c], y), new Vector2(columnWidth[c], 50f));
                    if (c > 0)
                    {
                        // Tecla o botón dentro de una "píldora".
                        RectTransform key = UIFactory.Rect("Key", cell, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                                           Vector2.zero, new Vector2(Mathf.Min(columnWidth[c] - 10f, 30f + Rows[r, c].Length * 13f), 36f));
                        UIFactory.AddSprite(key, UISprites.Pill, new Color(1f, 1f, 1f, 0.14f));
                        UIFactory.Label("Text", key, Rows[r, c].ToUpperInvariant(), 20, UITheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                    }
                    else
                    {
                        UIFactory.AddText(cell, Rows[r, c], 26, UITheme.Text, TextAnchor.MiddleLeft);
                    }
                }
            }

            RectTransform notes = UIFactory.Anchored("Notes", box, new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(1080f, 70f));
            UIFactory.AddText(notes,
                "Las acciones se cargan manteniendo el botón y se hacen al soltarlo.\n" +
                "Pulsa pase o tiro justo antes de recibir para golpear al primer toque.",
                22, UITheme.TextDim);

            RectTransform holder = UIFactory.Anchored("CloseHolder", box, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(360f, 64f));
            closeButton = UIFactory.Button(holder, "Volver", onClose, 360f, 64f);
            ((RectTransform)closeButton.transform).anchoredPosition = Vector2.zero;
            return root.gameObject;
        }
    }
}
