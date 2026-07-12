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
    [Export(PropertyHint.Layers3DPhysics)] private int CollisionMask { get; set; }
    [Export] private Node3D DraggingPlaneOrigin { get; set; } // for dragging ingredient in front of camera
    [Export] private Node3D IngredientHolder { get; set; } // holds dragged ingredient for smoother movement
    

    // only one cooker can be selected at a time
    // TODO: tell hoveredCooker.CookerGrid about selectedIngredient
    public static Cooker hoveredCooker;
    public static IngredientPackage hoveredPackage;
    private Ingredient hoveredIngredient;
    private Vector3 hoveredPos;
    private bool canBePlaced;

    public Ingredient draggedIngredient;


    // *** Movement ***
    private Camera3D dragCamera;
    private Vector3 worldMousePos;
    private Vector3 targetHolderRotation;

    public override void _Ready()
    {
        dragCamera = GetWindow().GetCamera3D();
        if (dragCamera == null)
            GameLogger.Log(LogLevel.ERROR, "Camera not set to instance");

        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        AlighObjectWithNormal();

        draggedIngredient.GlobalPosition = worldMousePos;
        draggedIngredient.GlobalRotation = targetHolderRotation;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(StaticStringRef.primaryInteraction))
        {
            if (draggedIngredient == null)
            {
                if (hoveredPackage != null)
                {
                    draggedIngredient = hoveredPackage.SpawnIngredient();
                    IngredientHolder.AddChild(draggedIngredient);
                    draggedIngredient.Owner = GetTree().Root;
                    SetProcess(true);
                    return;
                }

                if (hoveredCooker != null && hoveredIngredient != null)
                {

                }
            }
            else if (draggedIngredient != null)
            {
                if (hoveredPackage != null)
                {
                    if (hoveredPackage.TryReturnIngredientToPackage(draggedIngredient))
                    {
                        SetProcess(false);
                        draggedIngredient = null;
                    }
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

    private void AlighObjectWithNormal()
    {
        // TODO: Create Drag State Machine to prevent overlapping dragging states
        // move ingredient with mouse position
        PhysicsDirectSpaceState3D spaceState = dragCamera.GetWorld3D().DirectSpaceState;
        Vector2 mousePos = GetWindow().GetMousePosition();

        Vector3 origin = dragCamera.ProjectRayOrigin(mousePos);
        Vector3 end = origin + dragCamera.ProjectRayNormal(mousePos) * 50;

        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(origin, end, (uint)CollisionMask);

        Dictionary result = spaceState.IntersectRay(query);
        Vector3 newNormal;

        if (result.Count == 0)  // not hitting anything
        {
            hoveredCooker = null;
            hoveredPackage = null;

            Plane draggingPlane = new(dragCamera.GlobalBasis.Z, DraggingPlaneOrigin.GlobalPosition);
            end = dragCamera.ProjectRayNormal(mousePos);

            worldMousePos = (Vector3)draggingPlane.IntersectsRay(origin, end);
            targetHolderRotation = Vector3.Zero;
        }
        else // hitting an allowed collider
        {
            if (result["collider"].Obj is IngredientPackage pacakage)
            {
                hoveredCooker = null;
                targetHolderRotation = pacakage.GlobalRotation;
            }
            else if (result["collider"].Obj is Cooker cooker)
            {
                hoveredPackage = null;
                targetHolderRotation = cooker.GlobalRotation;
            }
            else // Hitting "something" that's allowed, but don't want it to be interactable
            {
                hoveredPackage = null;
                hoveredCooker = null;
                newNormal = (Vector3)result["normal"];
                Transform3D newTransform = StaticFunc.AlignWithY(draggedIngredient.GlobalTransform, newNormal);
                Vector3 newRotation = draggedIngredient.Rotation;

                newRotation.X = newTransform.Basis.GetEuler().X;
                if (Mathf.RadToDeg(newTransform.Basis.GetEuler().X) > 55 || Mathf.RadToDeg(newTransform.Basis.GetEuler().X) <= 0) // greater than certaion angle, less than 360
                    newRotation.X = 0;

                targetHolderRotation = newRotation;
            }

            // set position to whatever was hit
            worldMousePos = (Vector3)result["position"];
        }
    }

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
