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
    public Cooker parentCooker;
    public IngredientPackage parentPackage;
    // ***

    // *** Cooking Information ***
    private float maxCookingTime;
    private float curCookTime;
    private eCookerType curCookingType;
    private bool isCooking;

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
        curCookTime += (float)delta;

        if (curCookTime >= maxCookingTime)
        {
            GameLogger.Debug("Finished cooking");
            SetPhysicsProcess(false);
        }
    }

    private RIngredientBase TransformIngredient()
    {
        //RIngredientBase transformIngredient = IngredientBase.CookingInformation[curCookingType].AssociatedIngredient;
        //ingredientSize = IngredientBase.Size;
        //if (debugLogic != null)
        //    (IngredientMesh.Mesh as BoxMesh).Size = debugLogic.DebugSizes[IngredientBase.Size];


        return IngredientBase.CookingInformation[curCookingType].AssociatedIngredient;
    }

    public RIngredientBase OnInteract()
    {

        // IngredientIsPackage script
        // - Add to ingredient scene, turn off
        // - if ingredient is/becomes a package, turn on and override OnInteract function

        // Called in Cooker.TakeIngredient
        // if is single click pick up and move around
        // if is ingredient package and secondary action
        //      // pick up ingredient inside
        return null;
    }

    // Cooker Modifiers script
    // - What occurs hen an ingredient lands on the Cooker
    // - Whether to wait for some other button press

    public bool CanPlaceOnCooker(eCookerType cookerType)
    {
        if (IngredientBase.IsPizzaCrust && cookerType == eCookerType.Oven)
            return true;

        if (IngredientBase.CookingInformation.ContainsKey(cookerType))
            return true;

        return false;
    }

    public void PlaceOnCooker(eCookerType cookerType)
    {
        GameLogger.Debug($"Will start cooking on: {cookerType}");
        maxCookingTime = IngredientBase.CookingInformation[cookerType].CookTime;
        curCookTime = 0;
        curCookingType = cookerType;
        isCooking = true;

        SetPhysicsProcess(true);
    }

    public void PauseCooking()
    {
        SetPhysicsProcess(false);
    }

    #endregion
}
