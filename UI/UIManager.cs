using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace AetherBreakout.UI
{
    public static class UIManager
    {
        private static readonly Dictionary<char, bool[,]> FontMap = new()
        {
            ['0'] = new[,] { { true, true, true }, { true, false, true }, { true, false, true }, { true, false, true }, { true, true, true }, },
            ['1'] = new[,] { { false, true, false }, { true, true, false }, { false, true, false }, { false, true, false }, { true, true, true }, },
            ['2'] = new[,] { { true, true, true }, { false, false, true }, { true, true, true }, { true, false, false }, { true, true, true }, },
            ['3'] = new[,] { { true, true, true }, { false, false, true }, { true, true, true }, { false, false, true }, { true, true, true }, },
            ['4'] = new[,] { { true, false, true }, { true, false, true }, { true, true, true }, { false, false, true }, { false, false, true }, },
            ['5'] = new[,] { { true, true, true }, { true, false, false }, { true, true, true }, { false, false, true }, { true, true, true }, },
            ['6'] = new[,] { { true, true, true }, { true, false, false }, { true, true, true }, { true, false, true }, { true, true, true }, },
            ['7'] = new[,] { { true, true, true }, { false, false, true }, { false, false, true }, { false, false, true }, { false, false, true }, },
            ['8'] = new[,] { { true, true, true }, { true, false, true }, { true, true, true }, { true, false, true }, { true, true, true }, },
            ['9'] = new[,] { { true, true, true }, { true, false, true }, { true, true, true }, { false, false, true }, { true, true, true }, },
            ['A'] = new[,] { { true, true, true }, { true, false, true }, { true, true, true }, { true, false, true }, { true, false, true }, },
            ['B'] = new[,] { { true, true, false }, { true, false, true }, { true, true, false }, { true, false, true }, { true, true, false }, },
            ['G'] = new[,] { { true, true, true }, { true, false, false }, { true, false, true }, { true, false, true }, { true, true, true }, },
            ['I'] = new[,] { { true, true, true }, { false, true, false }, { false, true, false }, { false, true, false }, { true, true, true }, },
            ['L'] = new[,] { { true, false, false }, { true, false, false }, { true, false, false }, { true, false, false }, { true, true, true }, },
            ['M'] = new[,] { { true, false, true }, { true, true, true }, { true, false, true }, { true, false, true }, { true, false, true }, },
            ['S'] = new[,] { { true, true, true }, { true, false, false }, { true, true, true }, { false, false, true }, { true, true, true }, },
            ['x'] = new[,] { { true, false, true }, { false, true, false }, { true, false, true } },
            ['\''] = new[,] { { true, true }, { true, false } },
            [' '] = new[,] { { false } },
            ['J'] = new[,] { { false, false, true }, { false, false, true }, { false, false, true }, { true, false, true }, { true, true, true }, },
            ['U'] = new[,] { { true, false, true }, { true, false, true }, { true, false, true }, { true, false, true }, { true, true, true }, },
            ['T'] = new[,] { { true, true, true }, { false, true, false }, { false, true, false }, { false, true, false }, { false, true, false }, },
            ['H'] = new[,] { { true, false, true }, { true, false, true }, { true, true, true }, { true, false, true }, { true, false, true }, },
            ['E'] = new[,] { { true, true, true }, { true, false, false }, { true, true, false }, { true, false, false }, { true, true, true }, },
            ['R'] = new[,] { { true, true, false }, { true, false, true }, { true, true, false }, { true, false, true }, { true, false, true }, },
            ['N'] = new[,] { { true, false, true }, { true, true, true }, { true, true, true }, { true, true, true }, { true, false, true }, },
        };

        public static void DrawBlockyText(ImDrawListPtr drawList, string text, Vector2 pos, float size, uint color)
        {
            var currentPos = pos;
            float characterSpacing = 1 * size;

            foreach (var character in text)
            {
                if (FontMap.TryGetValue(character, out var shape))
                {
                    for (int y = 0; y < shape.GetLength(0); y++)
                    {
                        for (int x = 0; x < shape.GetLength(1); x++)
                        {
                            if (shape[y, x])
                            {
                                var p1 = currentPos + new Vector2(x * size, y * size);
                                drawList.AddRectFilled(p1, p1 + new Vector2(size, size), color);
                            }
                        }
                    }
                    currentPos.X += (shape.GetLength(1) * size) + characterSpacing;
                }
            }
        }

        public static float GetBlockyTextWidth(string text, float size)
        {
            float totalWidth = 0;
            float characterSpacing = 1 * size;

            foreach (var character in text)
            {
                if (FontMap.TryGetValue(character, out var shape))
                {
                    totalWidth += (shape.GetLength(1) * size) + characterSpacing;
                }
            }
            return totalWidth;
        }
    }
}