using Common;
using Godot;
using System;

namespace Features.FoodSystem.Cookers;
public partial class Cooker : StaticBody3D
{
    [Export] MeshInstance3D CookerMesh { get; set; }
    [Export] CookerGridTexture CookerGrid { get; set; }
    [Export] SubViewport GridViewport { get; set; }

    Vector2 planeSize;
    Vector2 cellSize;

    private Vector2I curIndex;
    private Vector2I lastIndex = -Vector2I.One;

    private Vector2 curPosition;
    private Vector2 lastPosition = -Vector2I.One;

    public override void _Ready()
    {
        planeSize = (CookerMesh.Mesh as PlaneMesh).Size;
    }

    public override void _InputEvent(Camera3D camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, int shapeIdx)
    {
        // TODO: Convert to using Input.IsActionPressed for controller inputs
        if (@event is not InputEventMouseMotion)
            return;

        // If the mouse isn't hitting an upwards facing part of the collision
        // (mimics the needed collision of a plane)
        if (!normal.IsEqualApprox(Basis.Y))
            return;

        GetGridIndexFromInput(eventPosition);

        // Don't do extra calculations while on the same cell
        if (curIndex == lastIndex)
            return;

        // multiply back to actual size (within grid)
        curPosition = new Vector2(curIndex.X * cellSize.X, curIndex.Y * cellSize.Y);

        //GameLogger.Log(LogLevel.INFO, $"Current Pos: {curIndex}");
        lastPosition = curPosition;
        lastIndex = curIndex;
    }

    /// <summary>
    /// Translates the 3D event position into a grid based position on the viewport
    /// </summary>
    /// <param name="eventPosition"></param>
    /// <returns></returns>
    Vector2 GetGridIndexFromInput(Vector3 eventPosition)
    {
        cellSize = CookerGrid.CellSize;

        // Remove transformations from InventoryMesh * eventPosition to get proper mouse3D coordinates relative center of inventory
        Vector3 mouse3D = CookerMesh.GlobalTransform.AffineInverse() * eventPosition;
        // Only need x/z values for movement on mesh surface
        Vector2 xzMouse2D = new(mouse3D.X, mouse3D.Z);

        // Get Size of mesh plane
        // normalize mouse position to coordinate in Viewport
        xzMouse2D += planeSize / 2;
        xzMouse2D /= planeSize;
        Vector2 target = xzMouse2D * GridViewport.Size;

        // mouseButton.Position = xzMouse2D * GridViewport.Size; // if need to push input to subviewport

        // Divide to get closest smallest integer on grid and round to floor
        curIndex = new Vector2I(Mathf.FloorToInt(target.X / cellSize.X), Mathf.FloorToInt(target.Y / cellSize.Y));
        // Clamp to ignore collision outside plane and to accomodate off-by-one
        curIndex = StaticFunc.ClampVector(curIndex, CookerGrid.CellCount - Vector2I.One, Vector2I.Zero);

        return curIndex;
    }

    /// <summary>
    /// Reverses the transformation done from GetGridIndexFromInput into local coordinates
    /// </summary>
    /// <param name="currentPosition"></param>
    /// <returns></returns>
    Vector3 Get3DWorldPositionFromIndex(Vector2 currentPosition)
    {
        Vector2 unScaledTarget = currentPosition / (Vector2)GridViewport.Size;
        unScaledTarget *= planeSize;
        unScaledTarget -= planeSize / 2;

        return new Vector3(unScaledTarget.X, 0, unScaledTarget.Y);
    }
}
