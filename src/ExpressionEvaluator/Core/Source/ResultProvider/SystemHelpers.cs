// A few dependencies from System.dll, which we want to avoid when compiling against Desktop Framework 2.0.
namespace System.Linq
{
    internal class Dummy { }
}
    
namespace System.Diagnostics
{
    internal static class Debug
    {
        [Conditional("DEBUG")]
        public static void Assert(bool condition, string message = null)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message ?? "Assertion failed");
            }
        }
    }
}

namespace System.CodeDom.Compiler
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    internal sealed class GeneratedCodeAttribute : Attribute
    {
        public GeneratedCodeAttribute(string tool, string version) { }
    }
}

namespace System.ComponentModel
{
    public enum EditorBrowsableState
    {
        Always = 0,
        Never = 1,
        Advanced = 2
    }

    [AttributeUsage(AttributeTargets.All)]
    internal sealed class EditorBrowsableAttribute : Attribute
    {
        public EditorBrowsableAttribute(EditorBrowsableState state) { }
    }
}