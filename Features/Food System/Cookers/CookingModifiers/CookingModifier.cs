using Features.FoodSystem.Ingredients;
using Godot;
using System;

namespace Features.FoodSystem.Cookers.Modifiers;
public abstract partial class CookingModifier : Node
{
    public virtual void OnPlaceIngredient(Cooker parentCooker, Ingredient placedIngredient)
    {
        placedIngredient.maxCookingTime = placedIngredient.IngredientBase.CookingInformation[parentCooker.CookerType].CookTime;
        placedIngredient.curCookingType = parentCooker.CookerType;
        placedIngredient.isCooking = true;

        // provide ingredient with logic for cooking
        placedIngredient.onCookIngredient += CookIngredient;
        //placedIngredient.SetPhysicsProcess(true);
    }

    /// <summary>
    /// Special interaction information specific to a cooker
    /// </summary>
    /// <param name="cooker">For interacting with specific variables of cooker</param>
    /// <param name="shapeidx">the hit collision of the cooker</param>
    /// <param name="event">the event hitting that collision</param>
    public virtual void OnInteractionWithCooker(Cooker cooker, InputEvent @event)
    {
        // interaction logic here
    }
    public virtual Ingredient OnInteractionWithIngredient(InputEvent @event, Ingredient hoveredIngredient)
    {
        return hoveredIngredient;
    }

    public virtual void OnTakeIngredient(Cooker parentCooker, Ingredient takenIngredient)
    {
        // take away logic
        takenIngredient.SetPhysicsProcess(false);
        takenIngredient.onCookIngredient -= CookIngredient;
    }

    /// <summary>
    /// cooking logic of an ingredient
    /// </summary>
    public virtual void CookIngredient(Ingredient cookingIngredient, float delta)
    {
        // set origin type of ingredient
        cookingIngredient.originCookerType = cookingIngredient.curCookingType;
    }
}
