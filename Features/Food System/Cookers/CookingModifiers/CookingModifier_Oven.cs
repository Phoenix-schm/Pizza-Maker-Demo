using Common;
using Godot;
using System;

namespace Features.FoodSystem.Cookers.Modifiers;

/// <summary>
/// Class for cooking ingredients in an oven
/// </summary>
public partial class CookingModifier_Oven : Node
{
    [Export] public CollisionObject3D OvenDoor { get; set; }
    [Export] public bool IsDoorOpen { get; set; }

    public void CookIngredients(Cooker cooker, int shapeidx, InputEvent @event)
    {
        if (!@event.IsActionPressed(StaticStringRef.a_primaryInteraction))
            return;

        uint shapeOwner = cooker.ShapeFindOwner(shapeidx);
        if (OvenDoor == cooker.ShapeOwnerGetOwner(shapeOwner) as CollisionObject3D)
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
