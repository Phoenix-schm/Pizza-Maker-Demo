using Features.FoodSystem.Ingredients;
using Godot;
using System;

namespace Features.FoodSystem.IngredientPackages;

public enum ePackageSize { SmallBottle, MediumXShortPackage, MediumShortPackage}

[GlobalClass]
public partial class R_IngredientPackages : Resource
{
    [Export] public RIngredientBase StoredIngredient { get; set; }
    [Export] public int AmountInPackage { get; set; } = 3;
    [Export] public ePackageSize PackageSize { get; set; }

    public Vector3 GetPackageCellSize()
    {
        Vector3 newSize = new Vector3(1, 2, 1);       // default size is bottle
        switch (PackageSize)
        {
            case ePackageSize.SmallBottle:
                newSize = new Vector3(1, 2, 1);
                break;
            case ePackageSize.MediumXShortPackage:  //  pizza crust package
                newSize = new Vector3(2, .5f, 3);
                break;
            case ePackageSize.MediumShortPackage:   // onion package
                newSize = new Vector3(2, 1, 3);
                break;
        }

        return newSize;
    }


}
