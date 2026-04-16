using Godot;

namespace WorldUtils;

public static class WorldUtilsExtension
{
    /// Returns a copy of Transform that rotates the Y axis towards specific direction
    /// using Vector3(0,0,-1) will make the Y axis point towards Z Axis
    public static Transform3D PointYTowards(this Transform3D transform, Vector3 direction)
    {
        var basis = Basis.LookingAt(direction, Vector3.Up);
        basis = basis.Rotated(basis.X, Mathf.DegToRad(90));
        return transform with { Basis = basis };
    }

    /// Vector Projection, where the current vector is the origin, both a&b are points in world space. 
    /// Think of this as finding the casted shadow (scalar point) that vector A will do in Vector B. 
    /// reference: en.wikipedia.org/wiki/Vector_projection
    public static Vector3 ProjectPoints(this Vector3 origin, Vector3 a, Vector3 b)
    {
        var dirB = b - origin;
        var dirA = a - origin;
        var scalar = dirA.Dot(dirB) / dirB.Dot(dirB);
        return origin + (scalar * dirB);
    }
}