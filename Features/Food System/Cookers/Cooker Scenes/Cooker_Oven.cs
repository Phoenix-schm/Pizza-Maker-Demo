using Features.FoodSystem.Ingredients;
using Godot;
using Microsoft.VisualBasic;
using System;
using System.Linq;

namespace Features.FoodSystem.Cookers;
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
            GD.Print("ingredient too big");
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
            // over the edge going down, so move up
            //if (intersection.Size.Y < ingrediectCells.Y)
            //{
            //    fauxStartingPos.Y--;
            //}

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

    // override curGridPos to use only x axis
}
