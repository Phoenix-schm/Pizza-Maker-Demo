using Godot;
using System;

namespace Features.FoodSystem.Cookers;
/// <summary>
/// Cooker: Textures the panel in a grid based on the inputted CellSize
/// </summary>
[Tool]
public partial class CookerGridTexture : PanelContainer
{
    [Export] public Vector2I CellCount { get; set; } = new Vector2I(6, 4);
    [Export] private float MainLineWidth { get; set; } = 2;
    [Export] private float DividerLineWidth { get; set; } = 8;

    [ExportToolButton("Redraw Grid")]
    public Callable RedrawButton => Callable.From(QueueRedraw);

    public Vector2 CellSize;

    public Vector2 inputPos;

    public async override void _Ready()
    {
        // Wait a from for screen to initialize. Otherwise errors will occur
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        QueueRedraw();
    }

    public override void _Draw()
    {
        InitializeGrid();
        DrawGrid();   
    }

    private void InitializeGrid()
    {
        // Null check
        if (CellCount.X == 0 || CellCount.Y == 0)
            return;

        CellSize = GetViewportRect().Size / CellCount;
    }

    private void DrawGrid()
    {
        float lineWidth = MainLineWidth;

        // Vertical lines
        for (int x = 0; x <= CellCount.X; x++)
        {
            if (x == CellCount.X || x == 0)
                lineWidth = DividerLineWidth;
            else
                lineWidth = MainLineWidth;

            DrawLine(
                new Vector2(x * CellSize.X, 0), // Starting point of line
                new Vector2(x * CellSize.X, CellSize.Y * CellCount.Y),   // End point of line
                Colors.White,
                lineWidth
                );
        }

        // Horizontal lines
        for (int y = 0; y <= CellCount.Y; y++)
        {
            if (y == CellCount.Y || y == 0)  // thicken line if its an edge
                lineWidth = DividerLineWidth;
            else
                lineWidth = MainLineWidth;

            DrawLine(
                new Vector2(0, y * CellSize.Y), // Starting point of line
                new Vector2(CellSize.X * CellCount.X, y * CellSize.Y),   // end point of line
                Colors.White,
                lineWidth
                );

        }
    }
}
