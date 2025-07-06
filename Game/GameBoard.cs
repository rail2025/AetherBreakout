using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace AetherBreakout.Game
{
    public class GameBoard
    {
        public List<Brick> Bricks { get; private set; } = new();

        private static readonly uint[] RowColors = {
            ImGui.GetColorU32(new Vector4(0.8f, 0.2f, 0.2f, 1.0f)), // Red
            ImGui.GetColorU32(new Vector4(1.0f, 0.5f, 0.2f, 1.0f)), // Orange
            ImGui.GetColorU32(new Vector4(0.2f, 0.8f, 0.2f, 1.0f)), // Green
            ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 0.2f, 1.0f)), // Yellow
            ImGui.GetColorU32(new Vector4(0.2f, 0.5f, 1.0f, 1.0f)), // Blue
            ImGui.GetColorU32(new Vector4(0.8f, 0.2f, 1.0f, 1.0f)), // Purple
        };

        public void CreateLevel(int level)
        {
            Bricks.Clear();

            int numRows = 5 + (level / 2);
            int numCols = 10;
            float padding = 0.5f;

            float brickWidth = (GameSession.GameBoardSize.X / numCols) - padding;
            float brickHeight = 4.0f;

            float topOffset = 25;

            for (int row = 0; row < numRows; row++)
            {
                var rowColor = RowColors[row % RowColors.Length];
                for (int col = 0; col < numCols; col++)
                {
                    var brickPos = new Vector2(
                        col * (brickWidth + padding) + (padding / 2),
                        topOffset + (row * (brickHeight + padding))
                    );
                    Bricks.Add(new Brick(brickPos, new Vector2(brickWidth, brickHeight), rowColor));
                }
            }
        }
    }
}