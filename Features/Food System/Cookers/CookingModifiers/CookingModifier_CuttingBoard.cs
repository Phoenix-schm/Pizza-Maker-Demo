using Features.FoodSystem.Ingredients;
using Godot;
using System;

namespace Features.FoodSystem.Cookers.Modifiers;

/// <summary>
/// Cooking logic for cutting board
/// </summary>
public partial class CookingModifier_CuttingBoard : CookingModifier
{
    public override void CookIngredient(Ingredient cookingIngredient, float delta)
    {
        cookingIngredient.curCookTime += delta;

        if (cookingIngredient.curCookTime >= cookingIngredient.maxCookingTime)
        {
            //  increase cook time
            // if hit cook time
            //      transform ingredient and stop cooking
            base.CookIngredient(cookingIngredient, delta);
            cookingIngredient.SetPhysicsProcess(false);
        }
    }

    public override Ingredient OnInteractionWithIngredient(InputEvent @event, Ingredient hoveredIngredient)
    {
        // if click on hovered ingredient,
        // start CookIngredient
        // do not need to click and hold
        hoveredIngredient.SetPhysicsProcess(true);
        return base.OnInteractionWithIngredient(@event, hoveredIngredient);
    }
}
