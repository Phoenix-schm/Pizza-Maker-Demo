using Common;
using Features.FoodSystem.Ingredients;
using Godot;
using System;
using System.Net;

namespace Features.FoodSystem.Cookers.Modifiers;

/// <summary>
/// Class for cooking ingredients in an oven
/// </summary>
public partial class CookingModifier_Oven : CookingModifier
{
    [Export] public StaticBody3D OvenDoor { get; set; }
    [ExportCategory("Cooking Variables")]
    [Export] public bool IsDoorOpen { get; set; }
    [Export] public Vector3 ClosedDoorRotation { get; set; } = new Vector3(0,0,0);
    [Export] public Vector3 OpenDoorRotation { get; set; } = new Vector3(0, 125, 0);
    [Export] public float TimeTilBurnt { get; set; }
 

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
        // send OvenDoor interaction to actual door logic
        if (!@event.IsActionPressed(StaticStringRef.a_primaryInteraction))
        {
            return;
        }

        IsDoorOpen = !IsDoorOpen;

        if (IsDoorOpen)
        {
            GD.Print("door open");
            OvenDoor.RotationDegrees = OpenDoorRotation;
            foreach(Node child in ParentCooker.IngredientHolder.GetChildren())
            {
                if (child is Ingredient)
                    child.SetPhysicsProcess(false);
            }
            // Stop cooking ingredients
            //ParentCooker.IngredientHolder.ProcessMode = ProcessModeEnum.Disabled;
            return;
        }

        OvenDoor.RotationDegrees = ClosedDoorRotation;
        foreach (Node child in ParentCooker.IngredientHolder.GetChildren())
        {
            if (child is Ingredient)
                child.SetPhysicsProcess(true);
        }
        //ParentCooker.IngredientHolder.ProcessMode = ProcessModeEnum.Inherit;
        //OnInteractionWithCooker(ParentCooker, @event);
    }

    public override void OnPlaceIngredient(Cooker parentCooker, Ingredient placedIngredient)
    {
        base.OnPlaceIngredient(parentCooker, placedIngredient);
        SetPhysicsProcess(true);    // Allow ingredient to "cook" but OnInteractionWithCooker() will disable processmode in children
    }

    public override void CookIngredient(Ingredient cookingIngredient, float delta)
    {
        cookingIngredient.curCookTime += delta;

        if (cookingIngredient.originCookerType == null && cookingIngredient.curCookTime >= cookingIngredient.maxCookingTime && cookingIngredient.curCookTime < TimeTilBurnt)
        {
            //  increase cook time
            // if hit cook time
            //      transform ingredient
            cookingIngredient.originCookerType = ParentCooker.CookerType;
            GameLogger.Info($"Ingredient is cooked: {cookingIngredient.originCookerType}");
            base.CookIngredient(cookingIngredient, delta);
        }

        if (cookingIngredient.curCookTime > TimeTilBurnt && !cookingIngredient.isBurnt)
        {
            // buffer time until burnt
            // if burnt,
            //      transform ingredient into burnt
            cookingIngredient.isBurnt = true;
            GameLogger.Info("Ingredient is burnt");
        }
    }

    public override void OnInteractionWithCooker(Cooker cooker, InputEvent @event)
    {
    }
}
