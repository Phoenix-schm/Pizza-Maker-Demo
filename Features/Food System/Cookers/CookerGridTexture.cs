using Common;
using Features.FoodSystem.Ingredients;
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

    [ExportCategory("Colors")]
    [Export] private Color LineColor { get; set; } = Colors.White;
    [Export] private Color HighlightColor { get; set; } = Colors.White;
    [Export] private Color RedHighlightColor { get; set; } = Colors.DarkRed;

    [ExportToolButton("Redraw Grid")]
    public Callable RedrawButton => Callable.From(QueueRedraw);

    public Vector2 cellSize;    // the size of each cell in the grid relative to the current SubViewport

    public Cooker parentCooker;
    public Vector2I inputPos;
    public bool canBePlaced;
    public Ingredient SelectedIngredient
    { get { return selectedIngredient; } 
      set {
            selectedIngredient = value;

            if (value != null)
                itemSize = value.IngredientBase.GetCellSize(value.orientation);
            else
                itemSize = -Vector2I.One;
          } }
    private Ingredient selectedIngredient;
    private Vector2I itemSize;

    public async override void _Ready()
    {
        // Wait a from for screen to initialize. Otherwise errors will occur due to SubViewport not having size initialized
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        InitializeGrid();
        
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (SelectedIngredient != null)
            DrawSelectedCells(itemSize, inputPos, canBePlaced);
        
        // Draw grid on top of selection cells
        DrawGrid();
    }

    private void InitializeGrid()
    {
        // Null check
        if (CellCount.X == 0 || CellCount.Y == 0)
            return;

        cellSize = GetViewportRect().Size / CellCount;
    }

    /// <summary>
    /// Draws a grid based on the amount of cells in CellCount
    /// </summary>
    private void DrawGrid()
    {
        if (cellSize.X <= 0 || cellSize.Y <= 0)
            return;

        float lineWidth = MainLineWidth;
        // Vertical lines
        for (int x = 0; x <= CellCount.X; x++)
        {
            if (x == CellCount.X || x == 0)
                lineWidth = DividerLineWidth;
            else
                lineWidth = MainLineWidth;

            DrawLine(
                new Vector2(x * cellSize.X, 0), // Starting point of line
                new Vector2(x * cellSize.X, cellSize.Y * CellCount.Y),   // End point of line
                LineColor,
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
                new Vector2(0, y * cellSize.Y), // Starting point of line
                new Vector2(cellSize.X * CellCount.X, y * cellSize.Y),   // end point of line
                LineColor,
                lineWidth
                );
        }
    }

    private void DrawSelectedCells(Vector2I _itemSize, Vector2I _inputPosition, bool _canBePlaced)
    {
        if (_itemSize.X <= 0 || _itemSize.Y <= 0)
        {
            return;
        }

        for (int i = 0; i < parentCooker.tempTakenCells.Count; i++)
        {
            int index = parentCooker.tempTakenCells[i];

            // reverse index into vector coordinates
            int xCoord = Mathf.FloorToInt(index % CellCount.X);
            int yCoord = Mathf.FloorToInt(index / CellCount.X);

            DrawRect(
                new Rect2(new Vector2(xCoord, yCoord) * cellSize, cellSize),
                _canBePlaced ? HighlightColor : RedHighlightColor
                );
        }

    }
}
