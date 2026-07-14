using Common;
using Features.FoodSystem.Ingredients;
using Godot;
using Godot.Collections;
using System;
using System.Linq;

namespace Features.FoodSystem.Cookers;

public enum eCookerType { CuttingBoard, Oven}

public partial class Cooker : StaticBody3D
{
    public event Action OnSendIngredient;           // will either send an ingredient or null
    public event Action<Vector3, bool> OnSendValidPlacement;    // will send a valid placement

    [Export] private MeshInstance3D CookerMesh { get; set; }
    [Export] public CookerGridTexture CookerGrid { get; set; }
    [Export] private SubViewport GridViewport { get; set; }
    [Export] private Node3D IngredientHolder { get; set; } // A controllable node3D where ingredients can position themselves relative to
    [Export] private Vector2I CookerSize { get; set; } = new Vector2I(6, 4);
    [ExportCategory("Ingredient Interaction")]
    [Export] public eCookerType CookerType { get; set; }
    // for iterating through ingredients and their takenSlots array
    [Export] public Array<Ingredient> IngredientsInCooker { get; set; }

    Vector2 planeSize;
    Vector2 cellSize;
    Rect2I gridRect;

    //  *** Positioning Information ***
    private Vector2I curGridCell;                   // The Vector2I cell position the mouse is hovering over
    private Vector2I lastGridCell = -Vector2I.One;

    private int curGridIndex;                       // The curGridCell flattened to an int
    private Vector2 curGridPosition;                // The Vector2 position the mouse takes up within the SubViewport
    private Vector3 curWorldPos;                    // the curGridPosition translated into 3D coordinates. Used for the blocky grid movement
    // ***

    // *** Dragging Logic ***
    private Ingredient hoveredIngredient;

    private bool CanBePlaced
    {
        get { return isCellsFree && canFitInCooker && isAllowedCooker; }
    }

    private bool isCellsFree;               // are there already ingredients in cells
    private bool canFitInCooker;            // can ingredient fit within grid
    private bool isAllowedCooker;           // if the cooker is a type the ingredient can use
    public Array<int> tempTakenCells = new Array<int>();   // Temporary cells for showing what cells the ingredient is taking up on the grid

    public override void _Ready()
    {
        planeSize = (CookerMesh.Mesh as PlaneMesh).Size;
        CookerGrid.parentCooker = this;
        CookerGrid.CellCount = CookerSize;

        gridRect = new Rect2I(Vector2I.Zero, CookerGrid.CellCount);
    }

    public override void _InputEvent(Camera3D camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, int shapeIdx)
    {
        // TODO: Send input to cookingModifier 

        if (IngredientsInCooker.Count == 0 && DragIngredientManager.Instance?.draggedIngredient == null)
            return;

        InitializeHoverLogic();

        // reset logic so that grid updates with ingredient rotation
        if (@event.IsActionPressed(StaticStringRef.a_secondaryInteraction))
            lastGridCell = -Vector2I.One;

        // If the mouse isn't hitting an upwards facing part of the collision
        // (mimics the needed collision of a plane)
        if (!normal.IsEqualApprox(GlobalBasis.Y))
            return;
        GetGridIndexFromInput(eventPosition);
        // Don't do extra calculations while on the same cell
        if (curGridCell == lastGridCell)
            return;

        if (DragIngredientManager.Instance?.draggedIngredient != null)
        {
            Ingredient curIngredient = DragIngredientManager.Instance.draggedIngredient;
            isAllowedCooker = curIngredient.CanPlaceOnCooker(CookerType);

            // multiply back to actual size (within grid subviewport)
            // TODO: Modify calculation to push ingredient left/up and center it
            // offset grid position with ingredient size so that it's centered with TakenSlots
            curGridPosition = new Vector2(curGridCell.X * cellSize.X, curGridCell.Y * cellSize.Y);

            curWorldPos = Get3DPositionFromGridPosition(curGridPosition + (cellSize * curIngredient.IngredientBase.GetCellSize(curIngredient.orientation)) / 2);
            TryPlaceIngredientInCell(curGridIndex, curIngredient);
            UpdateCookerGridTexture();
        }
        else if (DragIngredientManager.Instance != null)
            hoveredIngredient = TryGetHoveredIngredient();

        if (DragIngredientManager.Instance?.draggedIngredient != null)
        {
            // Check if cooker is a type that ingredient can be cooked on

            // translate position 
            Vector3 validGlobalPosition = IngredientHolder.ToGlobal(curWorldPos);
            
            OnSendValidPlacement?.Invoke(validGlobalPosition, CanBePlaced);
        }
        else
        {
            // send manager if the cell contains an ingredient or null
            OnSendIngredient?.Invoke();
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

        if (DragIngredientManager.Instance?.draggedIngredient != null)
            curGridCell = PushGridCellOffEdges(curGridCell, DragIngredientManager.Instance.draggedIngredient);

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

    /// <summary>
    /// Pushes the inputted starting position up/left based on grid size and ingredient size
    /// </summary>
    /// <param name="startingPos"></param>
    /// <param name="curIngredient"></param>
    /// <returns></returns>
    private Vector2I PushGridCellOffEdges(Vector2I startingPos, Ingredient curIngredient)
    {
        canFitInCooker = true;
        Vector2I fauxStartingPos = startingPos;
        Vector2I ingrediectCells = curIngredient.IngredientBase.GetCellSize(curIngredient.orientation);
        Rect2I ingredientGrid = new Rect2I(startingPos, ingrediectCells);

        if (gridRect.Encloses(ingredientGrid))
            return startingPos;
        else if (ingredientGrid.Encloses(gridRect))
        {
            // ingredient can't fit inside cooker at all
            canFitInCooker = false;
            return Vector2I.Zero;
        }

        canFitInCooker = false;
        do
        {
            ingredientGrid = new Rect2I(fauxStartingPos, ingrediectCells);
            // calculate current amount that's inside grid
            Rect2I intersection = gridRect.Intersection(ingredientGrid);

            // over the edge to the right, so move left
            if (intersection.Size.X < ingrediectCells.X)
            {
                fauxStartingPos.X--;
                //GD.Print($"Ingredient cells x: {ingrediectCells.X}");
            }
            // over the edge going down, so move up
            if (intersection.Size.Y < ingrediectCells.Y)
            {
                fauxStartingPos.Y--;
            }

            // if can't fit at all
            if (fauxStartingPos.X < 0 || fauxStartingPos.Y < 0)
            {
                //GD.PrintRich("[color=red]Is inside[/color]");
                return Vector2I.Zero;
            }

            // can finally fit inside grid
            if (intersection.Size.X == ingrediectCells.X && intersection.Size.Y == ingrediectCells.Y)
                canFitInCooker = true;

        } while (!canFitInCooker);

        return fauxStartingPos;
    }

    #endregion

    #region IngredientDraggingLogic
    private void UpdateCookerGridTexture()
    {
        CookerGrid.inputPos = curGridCell;
        CookerGrid.SelectedIngredient = DragIngredientManager.Instance.draggedIngredient;
        CookerGrid.canBePlaced = CanBePlaced;
        CookerGrid.QueueRedraw();
    }

    public void ResetCookerGridTexture()
    {
        CookerGrid.inputPos = -Vector2I.Zero;
        CookerGrid.SelectedIngredient = null;
        CookerGrid.QueueRedraw();
    }

    private void InitializeHoverLogic()
    {
        if (DragIngredientManager.Instance == null)
            return;

        // initialize important hover logic upon entering the cooker collision
        if (DragIngredientManager.hoveredStorage != this)
        {
            lastGridCell = -Vector2I.One;
            DragIngredientManager.Instance.UpdateHoverVariables(this);
        }
    }

    /// <summary>
    /// Fills the array TakenSlotsInCooker with index grid cell positions
    /// Used for highlighting cells and checking which cells an ingredient takes up
    /// </summary>
    /// <param name="startingCell"></param>
    /// <param name="_curIngredient"></param>
    private void TryPlaceIngredientInCell(int startingCell, Ingredient _curIngredient)
    {
        isCellsFree = true;
        // TODO: Check cells for if they can be placed.
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

        // check if there's any overlap between the potential takenCells and cells already taken
        foreach (Ingredient ingredient in IngredientsInCooker)
        {
            if (tempTakenCells.Any(x => ingredient.takenSlotsInCooker.Contains(x)))
            {
                isCellsFree = false;
                break;
            }
        }
    }

    private Ingredient TryGetHoveredIngredient()
    {
        // iterate through ingredients in cooker and check if they contain the current hovered cell
        foreach (Ingredient ingredient in IngredientsInCooker)
        {
            if (ingredient.takenSlotsInCooker.Contains(curGridIndex))
                // TODO: Create hover ingredient tweeen
                return ingredient;
        }

        return null;
    }

    /// <summary>
    /// Takes ingredient from cooker
    /// </summary>
    /// <param name="takenIngredient"></param>
    /// <returns></returns>
    public Ingredient TakeIngredient()
    {
        hoveredIngredient.Reparent(DragIngredientManager.Instance.IngredientHolder);
        IngredientsInCooker.Remove(hoveredIngredient);

        lastGridCell = -Vector2I.One;

        return hoveredIngredient;
    }

    /// <summary>
    /// Places ingredient on Cooker
    /// </summary>
    /// <param name="placedIngredient"></param>
    public bool TryPlaceIngredient(Ingredient placedIngredient)
    {
        if (!CanBePlaced)
            return false;

        IngredientsInCooker.Add(placedIngredient);
        placedIngredient.takenSlotsInCooker = tempTakenCells.Duplicate();
        tempTakenCells.Clear();

        //placedIngredient.PlaceOnCooker(CookerType);

        // replace parent
        placedIngredient.parentStorage = this;

        placedIngredient.Reparent(IngredientHolder);
        placedIngredient.Position = curWorldPos;

        ResetCookerGridTexture();
        lastGridCell = -Vector2I.One;
        UpdateCookerGridTexture();

        return true;
    }

    public bool TryReturnIngredientToParent(Ingredient returningIngredient)
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
        return true;
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
