using Godot;
using System;

namespace Common;

/// <summary>
///  A class storing references to strings, NodePaths, and StringNames for the sake of Godot not having to convert when communicating with the engine
/// </summary>
public static class StaticStringRef
{
    public static readonly StringName rotation = "rotation";
    public static readonly StringName position = "position";
    public static readonly StringName scale = "scale";
}
