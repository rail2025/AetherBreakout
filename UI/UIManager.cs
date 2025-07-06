using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace AetherBreakout.UI
{
    public static class UIManager
    {
        private static readonly Dictionary<char, bool[,]> FontMap = new()
        {
            ['0'] = new[,]
            {
                { true, true, true },
                { true, false, true },
                { true, false, true },
                { true, false, true },
                { true, true, true },
            },
            ['1'] = new[,]
            {
                { false, true, false },
                { true, true, false },
                { false, true, false },
                { false, true, false },
                { true, true, true },
            },
            ['2'] = new[,]
            {
                { true, true, true },
                { false, false, true },
                { true, true, true },
                { true, false, false },
                { true, true, true },
            },
            ['3'] = new[,]
            {
                { true, true, true },
                { false, false, true },
                { true, true, true },
                { false, false, true },
                { true, true, true },
            },
            ['4'] = new[,]
            {
                { true, false, true },
                { true, false, true },
                { true, true, true },
                { false, false, true },
                { false, false, true },
            },
            ['5'] = new[,]
            {
                { true, true, true },
                { true, false, false },
                { true, true, true },
                { false, false, true },
                { true, true, true },
            },
            ['6'] = new[,]
            {
                { true, true, true },
                { true, false, false },
                { true, true, true },
                { true, false, true },
                { true, true, true },
            },
            ['7'] = new[,]
            {
                { true, true, true },
                { false, false, true },
                { false, false, true },
                { false, false, true },
                { false, false, true },
            },
            ['8'] = new[,]
            {
                { true, true, true },
                { true, false, true },
                { true, true, true },
                { true, false, true },
                { true, true, true },
            },
            ['9'] = new[,]
            {
                { true, true, true },
                { true, false, true },
                { true, true, true },
                { false, false, true },
                { true, true, true },
            },
        };

        public static void DrawBlockyText(ImDrawListPtr drawList, string text, Vector2 pos, float size, uint color)
        {
            var currentPos = pos;
            float blockWidth = 3 * size;
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
                }
                currentPos.X += blockWidth + characterSpacing;
            }
        }
    }
}