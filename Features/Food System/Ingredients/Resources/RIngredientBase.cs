using Godot;
using System;

namespace Features.FoodSystem.Ingredients;

[GlobalClass]
public partial class RIngredientBase : Resource
{
    // Name of ingredient
    [Export] public string Name { get; private set; } = "NULL";
    // Size the ingredient takes up when cooking
    [Export] public eIngredientSize Size { get; set; }
    // the starting orientation of this ingredient. How it should be rotated
    [Export] public eIngredientOrientation DefaultOrientation { get; set; }

    /// <summary>
    /// Returns cells in x/y direction the ingredient takes up based on curOrientation
    /// </summary>
    /// <param name="curOrientation"></param>
    /// <returns></returns>
    public Vector2I GetCellSize(eIngredientOrientation curOrientation)
    {
        Vector2I newSize = new(1, 1);

        switch (Size)
        {
            case eIngredientSize.Small:
                newSize = new(1, 1);
                break;
            case eIngredientSize.Medium:
                newSize = curOrientation == eIngredientOrientation.Vertical ? new(1, 2) : new(2, 1);
                break;
            case eIngredientSize.Large:
                newSize = new(2, 2);
                break;
            case eIngredientSize.XL:
                newSize = curOrientation == eIngredientOrientation.Vertical ? new(2, 4) : new Vector2I(4, 2);
                break;
        }

        return newSize;
    }

    /// <summary>
    /// Returns the cellSize.x * cellSize.Y
    /// Useful for quick math when iterating through the /amout/ of cells the ingredient takes up.
    /// </summary>
    /// <returns></returns>
    public int GetCellLength()
    {
        // Orientation doesn't matter
        Vector2I cellSize = GetCellSize(eIngredientOrientation.Horizontal);
        return cellSize.X * cellSize.Y;
    }
}
