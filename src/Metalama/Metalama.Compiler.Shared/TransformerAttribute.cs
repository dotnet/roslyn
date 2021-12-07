using System;

namespace Metalama.Compiler
{
    /// <summary>
    /// Place this attribute onto a type to cause it to be considered a source transformer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TransformerAttribute : Attribute
    {
    }
}
