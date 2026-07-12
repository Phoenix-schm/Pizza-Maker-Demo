using Godot;
using Godot.Collections;
using System;

namespace Features.FoodSystem.Ingredients;

public partial class DebugIngredientLogic : Node
{
    // Debug mesh sizes used for visualizing on the grid
    [Export] public Dictionary<eIngredientSize, Vector3> DebugSizes { get; set; }
}
