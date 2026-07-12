using Common;
using Features.FoodSystem.Ingredients;
using Godot;
using System;

namespace Features.FoodSystem.IngredientPackages;
public partial class IngredientPackage : StaticBody3D
{
    // TODO: IngredientPackage resource
    // TODO: move stored ingredient to resoure
    [Export] private RIngredientBase StoredIngredient { get; set; }
    [Export] private PackedScene IngredientScene { get; set; }

    [ExportCategory("Debugging")]
    [Export] private bool OverrideIngredientSize { get; set; }
    [Export] private eIngredientSize debugSize { get; set; }

    public override void _InputEvent(Camera3D camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, int shapeIdx)
    {
        // Upon hovering over the ingredient package, initialize singleton
        if (DragIngredientManager.Instance == null)
            return;

        if (DragIngredientManager.hoveredPackage != this)
        {
            DragIngredientManager.Instance.UpdateHoverVariables(this);
        }
    }

    public Ingredient SpawnIngredient()
    {
        GameLogger.Log(LogLevel.INFO, $"Spawning ingredient: {StoredIngredient.Name}");
        Ingredient newIngredient = IngredientScene.Instantiate() as Ingredient;
        newIngredient.IngredientBase = StoredIngredient;
        newIngredient.parentPackage = this;

        return newIngredient;
    }

    /// <summary>
    /// Tries to return ingredient to the called upon package.
    /// </summary>
    /// <param name="returnedIngredient"></param>
    /// <returns></returns>
    public bool TryReturnIngredientToPackage(Ingredient returnedIngredient)
    {
        bool wasReturned = true;
        if (returnedIngredient.IngredientBase != StoredIngredient)
        {
            wasReturned = false;
            return wasReturned;
        }
        GameLogger.Log(LogLevel.INFO, $"Deleting ingredient: {returnedIngredient.IngredientBase.Name}");
        returnedIngredient.Reparent(this);
        // TODO: Tween ingredient back to parent

        // Remove references to ingredient
        returnedIngredient.parentCooker?.IngredientsInCooker.Remove(returnedIngredient);
        // TODO: Await tween to finish
        returnedIngredient.QueueFree();
        return wasReturned;
    }
}
