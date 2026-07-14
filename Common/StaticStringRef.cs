using Godot;
using System;

namespace Common;

/// <summary>
///  A class storing references to strings, NodePaths, and StringNames for the sake of Godot not having to convert when communicating with the engine
/// </summary>
public static class StaticStringRef
{
    public static readonly StringName rotation = "rotation";
    public static readonly StringName position = "position";
    public static readonly StringName scale = "scale";

    public static readonly StringName a_primaryInteraction = "Primary Interaction Button";
    public static readonly StringName a_secondaryInteraction = "Secondary Interaction Button";

    public static readonly StringName G_IngredientStorage = "Ingredient Storage";
    public static readonly StringName f_TakeIngredient = "TakeIngredient";
    public static readonly StringName f_TryPlaceIngredient = "TryPlaceIngredient";
    public static readonly StringName f_TryReturnIngredient = "TryReturnIngredientToParent";
}
