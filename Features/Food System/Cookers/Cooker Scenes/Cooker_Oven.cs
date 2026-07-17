using Features.FoodSystem.Ingredients;
using Godot;

namespace Features.FoodSystem.Cookers;
/// <summary>
/// Cooker that only uses X axis for moving ingredients around grid.
/// NOTE: Must move ingredient holder down from center to position correctly
/// </summary>
public partial class Cooker_Oven : Cooker
{
    /// <summary>
    /// Pushes the inputted starting position up/left based on grid size and ingredient size
    /// </summary>
    /// <param name="startingPos"></param>
    /// <param name="curIngredient"></param>
    /// <returns></returns>
    protected override Vector2I PushGridCellOffEdges(Vector2I startingPos, Ingredient curIngredient)
    {
        canFitInCooker = true;
        Vector2I fauxStartingPos = startingPos;
        Vector2I ingrediectCells = curIngredient.IngredientBase.GetCellSize(curIngredient.orientation);
        Rect2I ingredientGrid = new Rect2I(startingPos, ingrediectCells);
        Rect2I intersection = ingredientGrid.Intersection(gridRect);

        if (gridRect.Encloses(ingredientGrid))
            return startingPos;
        else if (intersection == gridRect) 
            return startingPos;
        else if (ingredientGrid.Encloses(gridRect))
        {
            // ingredient can't fit inside cooker at all
            canFitInCooker = false;
            return Vector2I.Zero;
        }

        canFitInCooker = false;
        do
        {
            ingredientGrid = new Rect2I(fauxStartingPos, ingrediectCells);
            intersection = gridRect.Intersection(ingredientGrid);
            // calculate current amount that's inside grid

            // over the edge to the right, so move left
            if (intersection.Size.X < ingrediectCells.X)
            {
                fauxStartingPos.X--;
                //GD.Print($"Ingredient cells x: {ingrediectCells.X}");
            }

            // ignore y axis

            // if can't fit at all
            if (fauxStartingPos.X < 0)
            {
                //GD.PrintRich("[color=red]Is inside[/color]");
                return Vector2I.Zero;
            }

            // can finally fit inside grid
            if (intersection.Size.X == ingrediectCells.X)
                canFitInCooker = true;

        } while (!canFitInCooker);

        return fauxStartingPos;
    }

    // override temp variables to only use x axis
    protected override void TryPlaceIngredientInCell(int startingCell, Ingredient _curIngredient)
    {
        isCellsFree = true;
        tempTakenCells.Clear();

        for (int x = 0; x < _curIngredient.IngredientBase.GetCellSize(_curIngredient.orientation).X; x++)
        {
            // Calculation only requires x axis
            int fauxCurIndex = startingCell + x;
            tempTakenCells.Add(fauxCurIndex);
        }

        CheckIfTempCellsAreTaken();
    }

    protected override Vector2 ScaleIngredientPosWithCellSize(Ingredient curIngredient)
    {
        // ignore y axis in order to only move in x axis
        Vector2 ingredientPos = new(cellSize.X * curIngredient.IngredientBase.GetCellSize(curIngredient.orientation).X / 2, 0);
        return ingredientPos;
    }
}
