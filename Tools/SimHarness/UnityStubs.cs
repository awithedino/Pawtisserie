using System;
namespace UnityEngine
{
    public class Object { public static void DestroyImmediate(Object o) { } }
    public class TextAsset : Object { public string text; }
    public class ScriptableObject : Object { public static T CreateInstance<T>() where T : ScriptableObject, new() => new T(); }
    public static class Application { public static string dataPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "../../../../../Assets")); }
    public static class Debug { public static void Log(object o) { } public static void LogWarning(object o) { } public static void LogError(object o) { } }
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
    public static class Mathf { public static float Abs(float f) => Math.Abs(f); public static int Max(int a, int b) => Math.Max(a, b); public static int RoundToInt(float f) => (int)Math.Round(f, MidpointRounding.ToEven); }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } }
    public struct Color { public float r, g, b, a; public Color(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; } }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string s) { } }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string s) { } }
    public class MinAttribute : Attribute { public MinAttribute(float f) { } }
    public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    public class SerializeFieldAttribute : Attribute { }
    public class CreateAssetMenuAttribute : Attribute { public string menuName, fileName; }
}
namespace UnityEngine.Serialization
{
    public class FormerlySerializedAsAttribute : Attribute { public FormerlySerializedAsAttribute(string oldName) { } }
}
