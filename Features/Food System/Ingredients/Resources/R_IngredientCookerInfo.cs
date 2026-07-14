using Godot;
using System;

namespace Features.FoodSystem.Ingredients;
/// <summary>
/// Resource storing information on how an ingredient should "cook" on its associated cooker
/// </summary>
[GlobalClass]
public partial class R_IngredientCookerInfo : Resource
{
    [Export] public  float CookTime { get; set; }
    [Export] public RIngredientBase AssociatedIngredient { get; set; }
    // Additional special logic
}
