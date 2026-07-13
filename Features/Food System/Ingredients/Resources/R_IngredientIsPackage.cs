using Godot;
using System;

namespace Features.FoodSystem.Ingredients;
/// <summary>
/// For ingredients that are packages
/// </summary>
[GlobalClass]
public partial class R_IngredientIsPackage : RIngredientBase
{
    [Export] public RIngredientBase StoredIngredient { get; set; }
    [Export] public int AmountInPackage { get; set; } = 3;
}
