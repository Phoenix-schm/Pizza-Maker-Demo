using Common;
using Features.FoodSystem.Ingredients;
using Godot;
using System;

namespace Features.FoodSystem.IngredientPackages;
public partial class IngredientPackage : StaticBody3D
{
    [Export] private R_IngredientPackages PackageInfo { get; set; }
    [Export] private PackedScene IngredientScene { get; set; }

    [ExportCategory("Debugging")]
    [Export] private bool OverrideIngredientSize { get; set; }
    [Export] private eIngredientSize debugSize { get; set; }

    public override void _Ready()
    {
        AddToGroup(StaticStringRef.G_IngredientStorage);
    }

    // TODO: Check for if player is holding PrimaryAction on Package. Call DragPackageManager
    //       Remove OnClick event from DragIngredientManager(?) store action here for control on distinguishing between single click and held click

    public override void _InputEvent(Camera3D camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, int shapeIdx)
    {
        // Upon hovering over the ingredient package, initialize singleton
        if (DragIngredientManager.Instance == null)
            return;

        if (DragIngredientManager.hoveredStorage != this)
            DragIngredientManager.Instance.UpdateHoverVariables(this);
    }

    public Ingredient TakeIngredient()
    {
        GameLogger.Log(LogLevel.INFO, $"Spawning ingredient: {PackageInfo.StoredIngredient.Name}");
        Ingredient newIngredient = new();

        newIngredient.IngredientBase = PackageInfo.StoredIngredient;
        Node3D ingredientMesh = IngredientScene.Instantiate() as Node3D;
        newIngredient.IngredientMesh = ingredientMesh;
        newIngredient.parentStorage = this;

        AddChild(newIngredient);
        newIngredient.AddChild(ingredientMesh);
        newIngredient.Owner = GetTree().Root;

        return newIngredient;
    }

    /// <summary>
    /// Tries to return ingredient to the called upon package.
    /// </summary>
    /// <param name="returnedIngredient"></param>
    /// <returns></returns>
    public bool TryReturnIngredientToParent(Ingredient returnedIngredient)
    {
        bool wasReturned = true;
        if (returnedIngredient.IngredientBase != PackageInfo.StoredIngredient)
        {
            wasReturned = false;
            return wasReturned;
        }
        GameLogger.Log(LogLevel.INFO, $"Deleting ingredient: {returnedIngredient.IngredientBase.Name}");
        returnedIngredient.Reparent(this);
        // TODO: Tween ingredient back to parent

        // TODO: Await tween to finish
        returnedIngredient.QueueFree();
        return wasReturned;
    }

    /// <summary>
    /// Duplicate logic for sake of use interface methods
    /// </summary>
    /// <param name="placedIngredient"></param>
    /// <returns></returns>
    public bool TryPlaceIngredient(Ingredient placedIngredient)
    {
        return TryReturnIngredientToParent(placedIngredient);
    }
}
