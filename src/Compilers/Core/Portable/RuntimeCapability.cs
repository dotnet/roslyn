// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies capabilities that may or may not be supported by the common language runtime the compilation is
    /// targeting.
    /// </summary>
    public enum RuntimeCapability
    {
        /// <summary>
        /// Represents a runtime feature where types can define ref fields.
        /// </summary>
        ByRefFields = 1,

        /// <summary>
        /// Represents a runtime feature where overriding methods can return more derived types than the method they override.
        /// </summary>
        CovariantReturnsOfClasses = 2,

        /// <summary>
        /// Represents a runtime feature where interfaces can define default method implementations.
        /// </summary>
        DefaultImplementationsOfInterfaces = 3,

        /// <summary>
        /// Indicates that this version of the runtime supports IntPtr and UIntPtr as numeric types.
        /// </summary>
        NumericIntPtr = 4,

        /// <summary>
        /// Represents a runtime feature where C# function pointers can be declared with an unmanaged calling convention.
        /// </summary>
        UnmanagedSignatureCallingConvention = 5,

        /// <summary>
        /// Indicates that this version of runtime supports virtual static members of interfaces.
        /// </summary>
        VirtualStaticsInInterfaces = 6,

        /// <summary>
        /// Indicates that this version of runtime supports inline array types.
        /// </summary>
        InlineArrayTypes = 7,

        /// <summary>
        /// Indicates that this version of runtime supports generic type parameters allowing substitution with a ref struct.
        /// </summary>
        ByRefLikeGenerics = 8,

        /// <summary>
        /// Indicates that this version of the runtime supports generating async state machines.
        /// </summary>
        RuntimeAsyncMethods = 9,
    }
}
