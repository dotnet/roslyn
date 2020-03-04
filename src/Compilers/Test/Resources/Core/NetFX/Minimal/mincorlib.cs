// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
	public class Object {}
    public struct Byte {}
    public struct Int16 { }
    public struct Int32 { }
    public struct Int64 { }
    public struct Single { }
    public struct Double { }
    public struct Char { }
    public struct Boolean { }
    public struct SByte { }
    public struct UInt16 { }
    public struct UInt32 { }
    public struct UInt64 { }
    public struct IntPtr { }
    public struct UIntPtr { }
    public class String {}
    public class Delegate {}
    public class MulticastDelegate {}
    public class Array {}
    public class Exception {}
    public class Type {}
    public class ValueType {}
    public class Enum {}
    public struct Void { }

    public struct RuntimeTypeHandle { }
    public struct RuntimeFieldHandle { }

    public interface IDisposable { }

    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
    public class Attribute { }

    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets validOn) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
        public AttributeTargets ValidOn { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    public class ParamArrayAttribute : Attribute { }

    public enum AttributeTargets
    {
        All = 0x7fff,
        Assembly = 1,
        Class = 4,
        Constructor = 0x20,
        Delegate = 0x1000,
        Enum = 0x10,
        Event = 0x200,
        Field = 0x100,
        GenericParameter = 0x4000,
        Interface = 0x400,
        Method = 0x40,
        Module = 2,
        Parameter = 0x800,
        Property = 0x80,
        ReturnValue = 0x2000,
        Struct = 8
    }

}

namespace System.Collections
{
    public interface IEnumerable {}
    public interface IEnumerator { }    
}

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public class OutAttribute : Attribute { }
}

namespace System.Reflection
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Class)]
    public class DefaultMemberAttribute : Attribute {}
}


// This shouldn't be necessary, remove when bug #15911 is fixed.
// Right now we can't define delegates without these types defined in corlib.
namespace System
{
    public interface IAsyncResult
    {
    }

    public delegate void AsyncCallback(IAsyncResult ar);
}
