using Common;
using Features.FoodSystem.Ingredients;
using Godot;
using Godot.Collections;
using System;

namespace Features.FoodSystem.Cookers;
public partial class Cooker : StaticBody3D
{
    [Export] private MeshInstance3D CookerMesh { get; set; }
    [Export] public CookerGridTexture CookerGrid { get; set; }
    [Export] private SubViewport GridViewport { get; set; }

    [ExportCategory("Debugging")]
    [Export] private Ingredient DebugIngredient { get; set; }

    // for iterating through ingredients and their takenSlots array
    [Export] public Array<Ingredient> IngredientsInCooker { get; set; }

    Vector2 planeSize;
    Vector2 cellSize;

    private Vector2I curGridCell;                   // The Vector2I cell position the mouse is hovering over
    private Vector2I lastGridCell = -Vector2I.One;

    private int curGridIndex;                       // The curGridCell flattened to an int

    private Vector2 curGridPosition;                // The Vector2 position the mouse takes up within the SubViewport

    private Vector3 curWorldPos;                    // the curGridPosition translated into 3D coordinates. Used for the blocky grid movement

    public override void _Ready()
    {
        planeSize = (CookerMesh.Mesh as PlaneMesh).Size;
        CookerGrid.parentCooker = this;
        // TODO: Replace with initializing on selecting an ingredient
        DebugIngredient.parentCooker = this;

        // TODO: Replace with only initializing when selecting an ingredient through an ingredient manager
        CookerGrid.SelectedIngredient = DebugIngredient;
    }

    public override void _InputEvent(Camera3D camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, int shapeIdx)
    {
        // TODO: Convert to using Input.IsActionPressed for controller inputs
        if (@event is not InputEventMouseMotion)
            return;

        // If the mouse isn't hitting an upwards facing part of the collision
        // (mimics the needed collision of a plane)
        if (!normal.IsEqualApprox(Basis.Y))
            return;

        GetGridIndexFromInput(eventPosition);

        // Don't do extra calculations while on the same cell
        if (curGridCell == lastGridCell)
            return;

        // TODO: Replace with "selected" ingredient
        // TODO: "Push" starting current position up/left based on size
        DebugIngredient.UpdateTakenCookerSlots(curGridIndex);

        // multiply back to actual size (within grid)
        curGridPosition = new Vector2(curGridCell.X * cellSize.X, curGridCell.Y * cellSize.Y);

        // offset grid position with ingredient size so that it's centered with TakenSlots
        // TODO: Replace calculation iwht "selected" ingredient
        curWorldPos = Get3DPositionFromGridPosition(curGridPosition + (cellSize * DebugIngredient.IngredientBase.GetCellSize(DebugIngredient.orientation)) / 2);

        //GameLogger.Log(LogLevel.INFO, $"Current Pos: {curWorldPos}");
        // Position ingredient mesh at translated coordinates.
        DebugIngredient.Position = curWorldPos;
        UpdateCookerGridTexture();

        lastGridCell = curGridCell;
    }

    #region TranslatePositionInformation
    /// <summary>
    /// Translates the 3D event position into a grid based position on the viewport
    /// </summary>
    /// <param name="eventPosition"></param>
    /// <returns></returns>
    Vector2 GetGridIndexFromInput(Vector3 eventPosition)
    {
        cellSize = CookerGrid.cellSize;

        // Using code from https://www.youtube.com/watch?v=80mT-2EfZyU&list=PLPJKm_oJXYjxxuHdKVvtiurnzK-8gGfgU&index=8
        
        // Remove transformations from InventoryMesh * eventPosition to get proper mouse3D coordinates relative center of inventory
        Vector3 mouse3D = CookerMesh.GlobalTransform.AffineInverse() * eventPosition;
        // Only need x/z values for movement on mesh surface
        Vector2 xzMouse2D = new(mouse3D.X, mouse3D.Z);

        // Get Size of mesh plane
        // normalize mouse position to coordinate in Viewport
        xzMouse2D += planeSize / 2;
        xzMouse2D /= planeSize;
        Vector2 target = xzMouse2D * GridViewport.Size;

        // mouseButton.Position = xzMouse2D * GridViewport.Size; // if need to push input to subviewport

        // Divide to get closest smallest integer on grid and round to floor
        curGridCell = new Vector2I(Mathf.FloorToInt(target.X / cellSize.X), Mathf.FloorToInt(target.Y / cellSize.Y));

        // Convert grid cell Vector into singular index position
        curGridIndex = curGridCell.X + (curGridCell.Y * CookerGrid.CellCount.X);

        return curGridCell;
    }

    /// <summary>
    /// Reverses the transformation done from GetGridIndexFromInput into local coordinates.
    /// Used for moving 3D ingredients around based on movement on the grid
    /// </summary>
    /// <param name="currentPosition"></param>
    /// <returns></returns>
    private Vector3 Get3DPositionFromGridPosition(Vector2 currentPosition)
    {
        Vector2 unScaledTarget = currentPosition / (Vector2)GridViewport.Size;
        unScaledTarget *= planeSize;
        unScaledTarget -= planeSize / 2;

        return new Vector3(unScaledTarget.X, 0, unScaledTarget.Y);
    }

    private void UpdateCookerGridTexture()
    {
        CookerGrid.inputPos = curGridCell;
        CookerGrid.QueueRedraw();
    }
    #endregion

    #region IngredientDraggingLogic
    // On Mouse Enter cooker area, send hover information to DragIngredientManager
    //      Whether the cell being hovered over contains an ingredient or is null
    // if null and DragIngredientManager.selectedIngredient != null,
    //      check if can place ingredient. Return null if can't

    #endregion
}
