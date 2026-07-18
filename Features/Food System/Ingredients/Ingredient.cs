using Common;
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
    [Export] public MeshInstance3D IngredientMesh { get; set; }

    private DebugIngredientLogic debugLogic;
    private eIngredientSize ingredientSize;
    public eIngredientOrientation orientation;
    public int length;                              // the cellSize.x * cellSize.y. Used for moving ingredient around grid

    // *** Dragging Information ***
    public Array<int> takenSlotsInCooker = new Array<int>();
    public Node parentStorage;          // node that stored ingredient
    // ***

    // *** Cooking Information ***
    public eCookerType? originCookerType = null;     // override to allow an ingredient to be placed back on the cooker they came from

    public float maxCookingTime;
    public float curCookTime;
    public eCookerType? curCookingType = null;
    public bool isCooking;
    public Action<Ingredient, float> onCookIngredient;
    public bool isBurnt = false;

    public override void _Ready()
    {
        if (UseDebug)
        {
            debugLogic = GetNodeOrNull("DebugIngredientLogic") as DebugIngredientLogic;
            ingredientSize = IngredientBase.Size;
            if (debugLogic != null)
                (IngredientMesh.Mesh as BoxMesh).Size = debugLogic.DebugSizes[IngredientBase.Size];
        }

        orientation = IngredientBase.DefaultOrientation;
        IngredientMesh.RotationDegrees = GetNewRotation();

        length = IngredientBase.GetCellLength();

        // should not be cooking on ready
        SetPhysicsProcess(false);
    }

    private void DebugResize()
    {
        if (!UseDebug || debugLogic == null)
            return;

        (IngredientMesh.Mesh as BoxMesh).Size = debugLogic.DebugSizes[IngredientBase.Size];
    }

    #region DraggingLogic
    public void FlipOrientation()
    {
        orientation = orientation == eIngredientOrientation.Horizontal ? eIngredientOrientation.Vertical : eIngredientOrientation.Horizontal;
        IngredientMesh.RotationDegrees = GetNewRotation(); 
    }

    private Vector3 GetNewRotation()
    {
        Vector3 newRotation = Vector3.Zero;
        switch (orientation)
        {
            case eIngredientOrientation.Horizontal:
                newRotation.Y = 0;
                break;
            case eIngredientOrientation.Vertical:
                newRotation.Y = 90;
                break;
        }

        return newRotation;
    }
    #endregion

    #region CookingLogic

    public override void _PhysicsProcess(double delta)
    {
        // use for cooking cutting board
        // emit particles when done
        //curCookTime += (float)delta;

        //if (curCookTime >= maxCookingTime)
        //{
        //    GameLogger.Debug("Finished cooking");
        //    originCookerType = curCookingType;
        //    SetPhysicsProcess(false);
        //}

        // when place in oven,
        //      keep cooking until burnt


        onCookIngredient?.Invoke(this, (float)delta);
    }

    private RIngredientBase TransformIngredient()
    {
        //RIngredientBase transformIngredient = IngredientBase.CookingInformation[curCookingType].AssociatedIngredient;
        //ingredientSize = IngredientBase.Size;
        //if (debugLogic != null)
        //    (IngredientMesh.Mesh as BoxMesh).Size = debugLogic.DebugSizes[IngredientBase.Size];


        return IngredientBase.CookingInformation[(eCookerType)curCookingType].AssociatedIngredient;
    }

    public bool CanPlaceOnCooker(eCookerType cookerType)
    {
        if (originCookerType != null && originCookerType == cookerType)
            return true;

        if (IngredientBase.IsPizzaCrust && cookerType == eCookerType.Oven)
            return true;

        if (IngredientBase.CookingInformation.ContainsKey(cookerType))
            return true;

        return false;
    }

    #endregion
}
