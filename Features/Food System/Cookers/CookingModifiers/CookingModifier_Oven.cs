using Common;
using Features.FoodSystem.Ingredients;
using Godot;
using System;

namespace Features.FoodSystem.Cookers.Modifiers;

/// <summary>
/// Class for cooking ingredients in an oven
/// </summary>
public partial class CookingModifier_Oven : CookingModifier
{
    [Export] public StaticBody3D OvenDoor { get; set; }
    [Export] public bool IsDoorOpen { get; set; }
    [Export] public Cooker parentCooker { get; set; }

    public override void _EnterTree()
    {
        OvenDoor.InputEvent += OvenDoor_InputEvent;
    }
    public override void _ExitTree()
    {
        OvenDoor.InputEvent -= OvenDoor_InputEvent;
    }
    private void OvenDoor_InputEvent(Node camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, long shapeIdx)
    {
        OnInteractionWithCooker(parentCooker, @event);
    }

    public override void OnPlaceIngredient(Cooker parentCooker, Ingredient placedIngredient)
    {
        base.OnPlaceIngredient(parentCooker, placedIngredient);
        SetPhysicsProcess(true);    // Allow ingredient to "cook" but OnInteractionWithCooker() will disable processmode in children
    }

    public override void CookIngredient(Ingredient cookingIngredient, float delta)
    {
        // increase cook timer
        // if hit max time,
        //      transform ingredient
        // buffer time until burnt
        // if burnt,
        //      transform ingredient into burnt

        base.CookIngredient(cookingIngredient, delta);
    }

    public override void OnInteractionWithCooker(Cooker cooker, InputEvent @event)
    {
        if (!@event.IsActionPressed(StaticStringRef.a_primaryInteraction))
            return;

        IsDoorOpen = !IsDoorOpen;

        if (IsDoorOpen)
        {
            // Stop cooking ingredients
            cooker.IngredientHolder.ProcessMode = ProcessModeEnum.Disabled;
            return;
        }

        cooker.IngredientHolder.ProcessMode = ProcessModeEnum.Inherit;
    }
}
