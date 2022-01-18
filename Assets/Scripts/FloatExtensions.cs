using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FloatExtensions
{
    public static bool NotZero(this float value)
    {
        return Mathf.Abs(value)>0;
    }
}
