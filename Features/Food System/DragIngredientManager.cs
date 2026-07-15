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
    [Export] public Node3D IngredientHolder { get; set; } // holds dragged ingredient for smoother movement
    [ExportCategory("Dragging")]
    [Export] private float DragSpeed { get; set; } = 2;
    [Export] private float RaycastRotationSpeed { get; set; } = .75f;
    [Export(PropertyHint.Range, "10, 150, 10")] private float DragRotationWeight { get; set; } = 85;

    // only one cooker can be selected at a time
    public static Node hoveredStorage;
    private Vector3 hoveredPos;
    private bool canBePlaced;

    public Ingredient draggedIngredient;


    // *** Movement ***
    private Camera3D dragCamera;
    private Vector3 worldMousePos;
    private Vector3 targetHolderRotation;

    private bool startedDrag;
    private Vector3 lastDraggingPosition;

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

        // soft bug fix to prevent jittery starting movement
        if (startedDrag)
        {
            lastDraggingPosition = draggedIngredient.Position;
            draggedIngredient.GlobalPosition = worldMousePos;
            startedDrag = false;
        }

        if (canBePlaced && hoveredStorage is Cooker)
            draggedIngredient.GlobalPosition = StaticFunc.ExpDecay(draggedIngredient.GlobalPosition, hoveredPos, 16 * DragSpeed, (float)delta);
        else
            draggedIngredient.GlobalPosition = StaticFunc.ExpDecay(draggedIngredient.GlobalPosition, worldMousePos, 16 * DragSpeed, (float)delta);

        draggedIngredient.GlobalRotation = StaticFunc.ExpDecay(draggedIngredient.GlobalRotation, targetHolderRotation, 16 * RaycastRotationSpeed, (float)delta);
        //GD.Print(draggedIngredient.GlobalRotation);

        // TODO: Move to ingredient
        float targetZRotation = Mathf.Clamp((draggedIngredient.Position.X - lastDraggingPosition.X) * DragRotationWeight, -45, 45);
        if (draggedIngredient.orientation == eIngredientOrientation.Horizontal)
            draggedIngredient.IngredientMesh.RotationDegrees = StaticFunc.ExpDecay(draggedIngredient.IngredientMesh.RotationDegrees, new Vector3(0, draggedIngredient.IngredientMesh.RotationDegrees.Y, targetZRotation), 16, (float)delta * 12);
        else
            draggedIngredient.IngredientMesh.RotationDegrees = StaticFunc.ExpDecay(draggedIngredient.IngredientMesh.RotationDegrees, new Vector3(targetZRotation, draggedIngredient.IngredientMesh.RotationDegrees.Y, 0), 16, (float)delta * 12);
        lastDraggingPosition = draggedIngredient.Position;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(StaticStringRef.a_primaryInteraction))
        {
            if (draggedIngredient == null)
                OnInteractWithoutIngredient();
            else if (draggedIngredient != null)
                OnInteractWithIngredient();
        }

        if (draggedIngredient == null) // other logic should not occur
            return;

        if (@event.IsActionPressed(StaticStringRef.a_secondaryInteraction))
        {
            // On Mouse Right Click, rotate ingredient
            draggedIngredient.FlipOrientation();
        }
    }

    private void OnInteractWithoutIngredient()
    {
        // if there is a hoveredStorage, try and take ingredient
        if (hoveredStorage != null && hoveredStorage.IsInGroup(StaticStringRef.G_IngredientStorage))
        {
            draggedIngredient = (Ingredient)(GodotObject)hoveredStorage.Call(StaticStringRef.f_TakeIngredient);
        }

        // if an ingredient was taken
        if (draggedIngredient != null)
        {
            if (draggedIngredient.GetParent() == null)
            {
                IngredientHolder.AddChild(draggedIngredient);
                draggedIngredient.Owner = GetTree().Root;
            }
            else
                draggedIngredient.Reparent(IngredientHolder);

            startedDrag = true;
            SetProcess(true);
        }
    }

    private void OnInteractWithIngredient()
    {
        SetProcess(false);
        // if can be placed where player clicked
        bool? isPlaced = (bool?)hoveredStorage?.Call(StaticStringRef.f_TryPlaceIngredient, draggedIngredient);

        // else, return ingredient to its parent
        if (isPlaced == null || (bool)!isPlaced)
            draggedIngredient.parentStorage.Call(StaticStringRef.f_TryReturnIngredient, draggedIngredient);

        draggedIngredient = null;
    }

    /// <summary>
    /// Raycast function for positioning ingredient while moving
    /// </summary>
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
            UpdateHoverVariables();

            Plane draggingPlane = new(dragCamera.GlobalBasis.Z, DraggingPlaneOrigin.GlobalPosition);
            end = dragCamera.ProjectRayNormal(mousePos);

            worldMousePos = (Vector3)draggingPlane.IntersectsRay(origin, end);
            targetHolderRotation = Vector3.Zero;
        }
        else // hitting an allowed collider
        {
            if (result["collider"].Obj is IngredientPackage pacakage)
            {
                targetHolderRotation = pacakage.GlobalRotation;
            }
            else if (result["collider"].Obj is Cooker cooker)
            {
                targetHolderRotation = cooker.GlobalRotation;
            }
            else // Hitting "something" that's allowed, but don't want it to be interactable
            {
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

    public void RecieveIngredient()
    {
        canBePlaced = false;
    }

    public void RecieveIngredientPlacement(Vector3 _validPlacement, bool _canBePlaced)
    {
        hoveredPos = _validPlacement;
        canBePlaced = _canBePlaced;
    }

    /// <summary>
    /// Updates hover variables based on inputted node. 
    /// </summary>
    /// <param name="hoveredNode"></param>
    public void UpdateHoverVariables(Node3D hoveredNode = null)
    {
        if (hoveredStorage != hoveredNode)
        {
            if (hoveredStorage is Cooker cooker)
                cooker.ResetCookerGridTexture();
            else
                canBePlaced = false;

            //canBePlaced = false;
            hoveredStorage = hoveredNode;
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
