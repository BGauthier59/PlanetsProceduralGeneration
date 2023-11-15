using UnityEngine;

public class ExBeziers : MonoBehaviour
{
    private static Vector3 QuadraticBeziersCurve(Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        Vector3 a = Vector3.Lerp(p1, p2, t);
        Vector3 b = Vector3.Lerp(p2, p3, t);
        return Vector3.Lerp(a, b, t);
    }
    
    public static Vector3 CubicBeziersCurve(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float t)
    {
        Vector3 a = QuadraticBeziersCurve(p1, p2, p3, t);
        Vector3 b = QuadraticBeziersCurve(p2, p3, p4, t);
        return Vector3.Lerp(a, b, t);
    }
}
