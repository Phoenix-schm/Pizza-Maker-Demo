using Godot;
using System;

namespace Features.FoodSystem.Shelves;
public partial class Shelf : StaticBody3D
{
    // Similar code to Cooker and how it deals with storing ingredients, but with pacakge
    // TODO: DragPackageManager
    // list of held pacakges. Packages store their own coordinates
    // remove ShelfGridTexture? at least remove topTexture in shelf scene

    // Use TopTexture subviewport for selecting front packages,
    // use secondary action to rotate up/down, front/back packages based on context of front held package (allows for accssing packages too far back)


    // TODO: Ingredients that are packages

    // Move packages by holding down primary action (called in Package script)
    // take ingredient from package by single click.
    //      Pass on single click to DragIngredientManager(?)

    // TODO: Create temporary IngredientPackage spawner for testing
    // TODO: Create camera movement manager
}
