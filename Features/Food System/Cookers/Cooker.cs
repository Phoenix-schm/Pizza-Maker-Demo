using Common;
using Features.FoodSystem.Ingredients;
using Godot;
using Godot.Collections;
using System;
using System.Reflection;
using static Godot.TextServer;

namespace Features.FoodSystem.Cookers;
public partial class Cooker : StaticBody3D
{
    public event Action<Ingredient> OnSendIngredient;           // will either send an ingredient or null
    public event Action<Vector3, bool, Array<int>> OnSendValidPlacement;    // will send a valid placement

    [Export] private MeshInstance3D CookerMesh { get; set; }
    [Export] public CookerGridTexture CookerGrid { get; set; }
    [Export] private SubViewport GridViewport { get; set; }
    [Export] private Node3D IngredientHolder { get; set; } // A controllable node3D where ingredients can position themselves relative to
    [ExportCategory("Debugging")]

    // for iterating through ingredients and their takenSlots array
    [Export] public Array<Ingredient> IngredientsInCooker { get; set; }

    Vector2 planeSize;
    Vector2 cellSize;

    //  *** Positioning Information ***
    private Vector2I curGridCell;                   // The Vector2I cell position the mouse is hovering over
    private Vector2I lastGridCell = -Vector2I.One;

    private int curGridIndex;                       // The curGridCell flattened to an int
    private Vector2 curGridPosition;                // The Vector2 position the mouse takes up within the SubViewport
    private Vector3 curWorldPos;                    // the curGridPosition translated into 3D coordinates. Used for the blocky grid movement
    // ***

    // *** Dragging Logic ***
    private Ingredient hoveredIngredient;
    private bool canBePlaced;
    public Array<int> tempTakenCells = new Array<int>();   // Temporary cells for showing what cells the ingredient is taking up on the grid

    public override void _Ready()
    {
        planeSize = (CookerMesh.Mesh as PlaneMesh).Size;
        CookerGrid.parentCooker = this;
        // TODO: Replace with initializing on selecting an ingredient
        //DebugIngredient.parentCooker = this;

        // TODO: Replace with only initializing when selecting an ingredient through an ingredient manager
        //CookerGrid.SelectedIngredient = DebugIngredient;
    }

    public override void _InputEvent(Camera3D camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, int shapeIdx)
    {
        InitializeHoverLogic();
        // If the mouse isn't hitting an upwards facing part of the collision
        // (mimics the needed collision of a plane)
        if (!normal.IsEqualApprox(Basis.Y))
            return;

        GetGridIndexFromInput(eventPosition);
        // Don't do extra calculations while on the same cell
        if (curGridCell == lastGridCell)
            return;

        if (DragIngredientManager.Instance != null && DragIngredientManager.Instance.draggedIngredient != null)
        {
            Ingredient curIngredient = DragIngredientManager.Instance.draggedIngredient;
            // multiply back to actual size (within grid subviewport)
            // TODO: Modify calculation to push ingredient left/up and center it
            curGridPosition = new Vector2(curGridCell.X * cellSize.X, curGridCell.Y * cellSize.Y);

            // offset grid position with ingredient size so that it's centered with TakenSlots
            curWorldPos = Get3DPositionFromGridPosition(curGridPosition + (cellSize * curIngredient.IngredientBase.GetCellSize(curIngredient.orientation)) / 2);
            TryPlaceIngredientInCell(curGridIndex, curIngredient);
            UpdateCookerGridTexture();
        }
        else if (DragIngredientManager.Instance != null)
            hoveredIngredient = TryGetHoveredIngredient();

        if (DragIngredientManager.Instance?.draggedIngredient != null)
        {
            // Send manager curWorldPos(placing ingredient) and if it can be placed
            Vector3 validGlobalPosition = IngredientHolder.ToGlobal(curWorldPos);
            
            OnSendValidPlacement?.Invoke(validGlobalPosition, canBePlaced, tempTakenCells);
        }
        else
        {
            // send manager if the cell contains an ingredient or null
            OnSendIngredient?.Invoke(hoveredIngredient);
        }

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
        CookerGrid.SelectedIngredient = DragIngredientManager.Instance.draggedIngredient;
        CookerGrid.canBePlaced = canBePlaced;
        CookerGrid.QueueRedraw();
    }

    public void ResetCookerGridTexture()
    {
        CookerGrid.inputPos = -Vector2I.Zero;
        CookerGrid.SelectedIngredient = null;
        CookerGrid.QueueRedraw();
    }
    #endregion

    #region IngredientDraggingLogic
    // On Mouse Enter cooker area, send hover information to DragIngredientManager
    //      Whether the cell being hovered over contains an ingredient or is null
    // if null and DragIngredientManager.selectedIngredient != null,
    //      check if can place ingredient. Return null if can't

    private void InitializeHoverLogic()
    {
        if (DragIngredientManager.Instance == null)
            return;

        if (DragIngredientManager.hoveredCooker == this)
            return;

        // initialize important hover logic upon entering the cooker collision
        lastGridCell = -Vector2I.One;
        DragIngredientManager.Instance.UpdateHoverVariables(this);
    }

    /// <summary>
    /// Fills the array TakenSlotsInCooker with index grid cell positions
    /// Used for highlighting cells and checking which cells an ingredient takes up
    /// </summary>
    /// <param name="startingCell"></param>
    /// <param name="_curIngredient"></param>
    private void TryPlaceIngredientInCell(int startingCell, Ingredient _curIngredient)
    {
        canBePlaced = true;
        // TODO: Check cells for if they can be placed.
        foreach (Ingredient ingredient in IngredientsInCooker)
        {
            if (ingredient.takenSlotsInCooker.Contains(curGridIndex))
            {
                canBePlaced = false;
                break;
            }
        }

        tempTakenCells.Clear();
        for (int x = 0; x < _curIngredient.IngredientBase.GetCellSize(_curIngredient.orientation).X; x++)
        {
            for (int y = 0; y < _curIngredient.IngredientBase.GetCellSize(_curIngredient.orientation).Y; y++)
            {
                // Calculation for translating a (x, y) coordinate into a flat index position
                int fauxCurIndex = startingCell + x + (y * CookerGrid.CellCount.X);
                tempTakenCells.Add(fauxCurIndex);
            }
        }

        //DragIngredientManager.Instance.draggedIngredient.UpdateTakenCookerSlots(curGridIndex, this);
    }

    private Ingredient TryGetHoveredIngredient()
    {
        // iterate through ingredients in cooker and check if they contain the current hovered cell
        foreach (Ingredient ingredient in IngredientsInCooker)
        {
            if (ingredient.takenSlotsInCooker.Contains(curGridIndex))
                return ingredient;
        }

        return null;
    }

    /// <summary>
    /// Takes ingredient from cooker
    /// </summary>
    /// <param name="takenIngredient"></param>
    /// <returns></returns>
    public Ingredient TakeIngredient(Ingredient takenIngredient)
    {
        takenIngredient.Reparent(DragIngredientManager.Instance.IngredientHolder);
        IngredientsInCooker.Remove(takenIngredient);

        lastGridCell = -Vector2I.One;

        return takenIngredient;
    }

    /// <summary>
    /// Places ingredient on Cooker
    /// </summary>
    /// <param name="placedIngredient"></param>
    public void PlaceIngredient(Ingredient placedIngredient, Array<int> tempCells)
    {
        IngredientsInCooker.Add(placedIngredient);
        placedIngredient.takenSlotsInCooker = tempCells.Duplicate();
        tempTakenCells.Clear();

        // replace parent
        placedIngredient.parentPackage = null;
        placedIngredient.parentCooker = this;

        placedIngredient.Reparent(IngredientHolder);
        placedIngredient.Position = curWorldPos;

        ResetCookerGridTexture();
        lastGridCell = -Vector2I.One;
        UpdateCookerGridTexture();
    }

    public void ReturnIngredientToParent(Ingredient returningIngredient)
    {
        IngredientsInCooker.Add(returningIngredient);
        // take starting index the ingredient was originally taking
        int startIndex = returningIngredient.takenSlotsInCooker[0];

        // reverse index into vector coordinates
        int xCoord = Mathf.FloorToInt(startIndex % CookerGrid.CellCount.X);
        int yCoord = Mathf.FloorToInt(startIndex / CookerGrid.CellCount.X);
        Vector2 oldGridPos = new Vector2(xCoord * cellSize.X, yCoord * cellSize.Y);

        // offset grid position with ingredient size so that it's centered with TakenSlots
        Vector3 oldWorldPos = Get3DPositionFromGridPosition(oldGridPos + (cellSize * returningIngredient.IngredientBase.GetCellSize(returningIngredient.orientation)) / 2);

        returningIngredient.Reparent(IngredientHolder);
        returningIngredient.Position = oldWorldPos;
        returningIngredient.GlobalRotation = GlobalRotation; 
    }

    #endregion

    public override void _EnterTree()
    {
        if (DragIngredientManager.Instance == null)
            return;
        OnSendIngredient += DragIngredientManager.Instance.RecieveIngredient;
        OnSendValidPlacement += DragIngredientManager.Instance.RecieveIngredientPlacement;
    }

    public override void _ExitTree()
    {
        if (DragIngredientManager.Instance == null)
            return;
        OnSendIngredient -= DragIngredientManager.Instance.RecieveIngredient;
        OnSendValidPlacement -= DragIngredientManager.Instance.RecieveIngredientPlacement;
    }
}
