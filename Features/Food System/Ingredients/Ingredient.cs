using Features.FoodSystem.Cookers;
using Features.FoodSystem.IngredientPackages;
using Godot;
using Godot.Collections;
using System;

namespace Features.FoodSystem.Ingredients;

public enum eIngredientSize { Small = 1, Medium = 2, Large = 4, XL = 8}
public enum eIngredientOrientation { Horizontal, Vertical }
public partial class Ingredient : Node3D
{
    [Export] public RIngredientBase IngredientBase { get; set; }

    [ExportCategory("Debugging")]
    [Export] private bool UseDebug { get; set; } = true;

    [ExportCategory("Node Communication")]
    [Export] private MeshInstance3D IngredientMesh { get; set; }

    private DebugIngredientLogic debugLogic;
    private eIngredientSize ingredientSize;
    public eIngredientOrientation orientation;
    public int length;                              // the cellSize.x * cellSize.y. Used for moving ingredient around grid

    public Array<int> takenSlotsInCooker = new Array<int>();
    public Cooker parentCooker;
    public IngredientPackage parentPackage;

    public override void _Ready()
    {
        if (UseDebug)
        {
            debugLogic = GetNodeOrNull("DebugIngredientLogic") as DebugIngredientLogic;
            ingredientSize = IngredientBase.Size;
            if (debugLogic != null)
                (IngredientMesh.Mesh as BoxMesh).Size = debugLogic.DebugSizes[IngredientBase.Size];
        }

        length = IngredientBase.GetCellLength();
    }

    private void DebugResize()
    {
        if (!UseDebug || debugLogic == null)
            return;

        (IngredientMesh.Mesh as BoxMesh).Size = debugLogic.DebugSizes[IngredientBase.Size];
    }

    /// <summary>
    /// Fills the array TakenSlotsInCooker with index grid cell positions
    /// Used for highlighting cells and checking which cells an ingredient takes up
    /// </summary>
    /// <param name="startingCell"></param>
    public void UpdateTakenCookerSlots(int startingCell, Cooker hoveredCooker)
    {
        takenSlotsInCooker.Clear();
        for (int x = 0; x < IngredientBase.GetCellSize(orientation).X; x++)
        {
            for (int y = 0; y < IngredientBase.GetCellSize(orientation).Y; y++)
            {
                // Calculation for translating a (x, y) coordinate into a flat index position
                int fauxCurIndex = startingCell + x + (y * hoveredCooker.CookerGrid.CellCount.X);
                takenSlotsInCooker.Add(fauxCurIndex);
            }
        }
    }
}
