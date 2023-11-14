using System;
using UnityEngine;

public class RandomGenerator : MonoBehaviour
{
    public static uint seed;

    private static uint factor = 1_103_515_245;
    private static uint increment = 12345;
    private static uint modulo = (uint)Math.Pow(2, 31) - 1;

    public static float GetRandomValue()
    {
        // Linear Congruential Generator method
        uint calculated = (factor * seed + increment) % modulo;
        seed = calculated;
        return calculated / (float)modulo;
    }

    public static int GetRandomValueInt(int min, int max)
    {
        return (int)GetRandomValueInRange(min, max);
    }

    public static float GetRandomValueInRange(float min, float max)
    {
        return (min + GetRandomValue() * (max - min));
    }
}