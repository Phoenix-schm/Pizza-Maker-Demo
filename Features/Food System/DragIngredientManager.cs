using Common;
using Features.FoodSystem.Cookers;
using Features.FoodSystem.Ingredients;
using Godot;
using System;

namespace Features.FoodSystem;
public partial class DragIngredientManager : Node
{
    public static DragIngredientManager Instance { get; private set; }

    // only one cooker can be selected at a time
    // TODO: tell hoveredCooker.CookerGrid about selectedIngredient
    public static Cooker hoveredCooker;

    public Ingredient draggedIngredient;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(StaticStringRef.primaryInteraction))
        {
            // Update with hovered ingredient information
            // On Mouse Click, if hovered ingredient == null and holding ingredient
            //      check if can place ingredient (hold logic in Cooker)
            // On Mouse Click, if hovered ingredient != null
            //      select ingredient
        }

        if (draggedIngredient == null) // other logic should not occur
            return;

        if (@event.IsActionPressed(StaticStringRef.secondaryInteraction))
        {
            // On Mouse Right Click, rotate ingredient
        }
    }


    // if draggIngredient != null, move it around. Parent it to node3d(?)

    // TODO: Create IngredientHolder for holding dragged ingredients
    // TODO: Create IngredientPackage for spawning ingredient into the world

    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            GameLogger.Warning("Excess instance of singleton. Deleting...");
            QueueFree();
            return;
        }

        Instance = this;
    }
}
