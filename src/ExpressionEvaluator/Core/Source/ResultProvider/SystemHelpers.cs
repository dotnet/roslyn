// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

// A few dependencies from System.dll -- we want to avoid referencing the entire System.dll.

namespace System.Diagnostics
{
    internal static class Debug
    {
        [Conditional("DEBUG")]
        public static void Assert(bool condition)
        {
            Assert(condition, null);
        }

        [Conditional("DEBUG")]
        public static void Assert(bool condition, string message)
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
