using Godot;
using System;

namespace Common;

/// <summary>
/// Class storing general use static functions
/// </summary>
public static class StaticFunc
{
    /// <summary>
    /// Exponential decay function. Should be used to replace use of lerp
    /// </summary>
    /// <param name="a">From Vector</param>
    /// <param name="b">To Vector</param>
    /// <param name="dt">delta time</param>
    /// <param name="decay">Exponential decay constant. Useful range approx 1 to 25, from slow to fast</param>
    /// <returns></returns>
    public static Vector3 ExpDecay(Vector3 a, Vector3 b, float dt, float decay = 16)
    {
        if (a.IsEqualApprox(b))
            return a;
        return b + (a - b) * (float)Math.Exp(-decay * dt);
    }

    /// <summary>
    /// Exponential decay function. Should be used to replace use of lerp
    /// </summary>
    /// <param name="a">From float</param>
    /// <param name="b">To float</param>
    /// <param name="dt">delta time</param>
    /// <param name="decay">Exponential decay constant. Useful range approx 1 to 25, from slow to fast</param>
    /// <returns></returns>
    public static float ExpDecay(float a, float b, float dt, float decay = 16)
    {
        if (Mathf.IsEqualApprox(a, b))
            return a;
        return b + (a - b) * (float)Math.Exp(-decay * dt);
    }

    /// <summary>
    /// Clamps a Vector within set clamps
    /// </summary>
    /// <param name="a"> Vector to clamp</param>
    /// <param name="toClampMax"></param>
    /// <param name="toClampMin"></param>
    /// <returns></returns>
    public static Vector2I ClampVector(Vector2I a, Vector2I toClampMax, Vector2I toClampMin)
    {
        if (a.X < toClampMin.X)
            a.X = toClampMin.X;
        if (a.Y < toClampMin.Y)
            a.Y = toClampMin.Y;
        if (a.X > toClampMax.X)
            a.X = toClampMax.X;
        if (a.Y > toClampMax.Y)
            a.Y = toClampMax.Y;

        return a;
    }

    public static Transform3D AlignWithY(Transform3D transform, Vector3 newY)
    {
        transform.Basis.Y = newY;
        transform.Basis.X = -transform.Basis.Z.Cross(newY);
        transform.Basis = transform.Basis.Orthonormalized();

        return transform;
    }
}
