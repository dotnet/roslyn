using System;

namespace ReferencedLibrary
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class SomeMetadataAttribute : Attribute
    {
    }
}
