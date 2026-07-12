using Common;
using Features.FoodSystem.Cookers;
using Features.FoodSystem.IngredientPackages;
using Features.FoodSystem.Ingredients;
using Godot;
using Godot.Collections;
using System;

namespace Features.FoodSystem;
public partial class DragIngredientManager : Node
{
    public static DragIngredientManager Instance { get; private set; }

    [ExportCategory("Raycasting")]
    [Export] private Node3D DraggingPlaneOrigin { get; set; }

    // only one cooker can be selected at a time
    // TODO: tell hoveredCooker.CookerGrid about selectedIngredient
    public static Cooker hoveredCooker;
    public static IngredientPackage hoveredPackage;

    public Ingredient draggedIngredient;

    private Camera3D dragCamera;

    public override void _Ready()
    {
        dragCamera = GetWindow().GetCamera3D();
        if (dragCamera == null)
            GameLogger.Log(LogLevel.ERROR, "Camera not set to instance");

        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        // TODO: Create Drag State Machine to prevent overlapping dragging states
        // move ingredient with mouse position
        PhysicsDirectSpaceState3D spaceState = dragCamera.GetWorld3D().DirectSpaceState;
        Vector2 mousePos = GetWindow().GetMousePosition();

        Vector3 origin = dragCamera.ProjectRayOrigin(mousePos);
        Vector3 end = origin + dragCamera.ProjectRayNormal(mousePos) * 50;

        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(origin, end);

        Dictionary result = spaceState.IntersectRay(query);

        if (result.Count == 0)  // not hitting anything
        {
            hoveredCooker = null;
            hoveredPackage = null;

            Plane draggingPlane = new(dragCamera.Basis.Z, DraggingPlaneOrigin.GlobalPosition);
            end = dragCamera.ProjectRayNormal(mousePos);

            Vector3 worldMousePos = (Vector3)draggingPlane.IntersectsRay(origin, end);
            draggedIngredient.GlobalPosition = worldMousePos;
        }

    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(StaticStringRef.primaryInteraction))
        {
            if (hoveredPackage != null && draggedIngredient == null)
            {
                draggedIngredient = hoveredPackage.SpawnIngredient();
                AddChild(draggedIngredient);
                draggedIngredient.Owner = GetTree().Root;
                SetProcess(true);
            }
            else if (hoveredPackage != null && draggedIngredient != null)
            {
                if (hoveredPackage.TryReturnIngredientToPackage(draggedIngredient))
                {
                    SetProcess(false);
                    draggedIngredient = null;
                }
            }

            // On Click on package, if draggedIngredient != null (TODO: and hasn't been cooked)
            //      return to package
            // On Click on package, if draggedIngredient == null,
            //      instantiate PackedScene and populate with information based on storedIngredient resource

            // Update with hovered ingredient information
            // On Mouse Click, if hovered ingredient == null and holding ingredient
            //      check if can place ingredient (hold logic in Cooker)
            // On Mouse Click, if hovered ingredient != null and hovering over cooker
            //      select ingredient

            // On Mouse Click, if not hovered over cooker
            //      return ingredient back to parent
        }

        if (draggedIngredient == null) // other logic should not occur
            return;

        if (@event.IsActionPressed(StaticStringRef.secondaryInteraction))
        {
            // On Mouse Right Click, rotate ingredient
        }
    }


    // if draggIngredient != null, move it around. Parent it to node3d(?)

    // TODO: Create IngredientHolder for holding dragged ingredients
    // TODO: Create IngredientPackage for spawning ingredient into the world

    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            GameLogger.Warning("Excess instance of singleton. Deleting...");
            QueueFree();
            return;
        }

        Instance = this;
    }
}
