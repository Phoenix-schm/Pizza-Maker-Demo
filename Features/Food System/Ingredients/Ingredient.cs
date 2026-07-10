using Godot;
using Godot.Collections;
using System;

namespace Features.FoodSystem.Ingredients;

public enum eIngredientSize { Small = 1, Medium = 2, Large = 4, XL = 8}
[Tool]
public partial class Ingredient : Node3D
{
    [ExportCategory("Debugging")]
    [Export] private bool UseDebug { get; set; } = true;
    [Export] private eIngredientSize DebugSize { get; set; } = eIngredientSize.Small;
    [Export] public bool RandomizeSize { get; set; } = false;

    [ExportToolButton("ResizeIngredient")]
    public Callable ResizeButton => Callable.From(DebugResize);

    [ExportCategory("Node Communication")]
    [Export] private MeshInstance3D IngredientMesh { get; set; }

    private DebugIngredientLogic debugLogic;

    public override void _Ready()
    {
        if (UseDebug)
        {
            debugLogic = GetNodeOrNull("DebugIngredientLogic") as DebugIngredientLogic;
            if (debugLogic != null)
                (IngredientMesh.Mesh as BoxMesh).Size = debugLogic.DebugSizes[DebugSize];
        }
    }

    private void DebugResize()
    {
        if (!UseDebug || debugLogic == null)
            return;

        if (RandomizeSize)
        {
            eIngredientSize[] newSize = (eIngredientSize[])Enum.GetValues(typeof(eIngredientSize));
            Random newRandom = new();
            DebugSize = newSize[newRandom.Next(newSize.Length)];
        }

        (IngredientMesh.Mesh as BoxMesh).Size = debugLogic.DebugSizes[DebugSize];
    }

}
