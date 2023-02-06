// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Symbols;

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
    }

    internal static class RuntimeCapabilityHelpers
    {
        public static bool RuntimeSupportsCapability(IAssemblySymbolInternal assembly, RuntimeCapability capability)
        {
            switch (capability)
            {
                case RuntimeCapability.ByRefFields:
                    return RuntimeSupportsFeature(assembly, SpecialMember.System_Runtime_CompilerServices_RuntimeFeature__ByRefFields);

                case RuntimeCapability.CovariantReturnsOfClasses:
                    // check for the runtime feature indicator and the required attribute.
                    return RuntimeSupportsFeature(assembly, SpecialMember.System_Runtime_CompilerServices_RuntimeFeature__CovariantReturnsOfClasses) &&
                           assembly.GetSpecialType(SpecialType.System_Runtime_CompilerServices_PreserveBaseOverridesAttribute) is { TypeKind: TypeKind.Class };

                case RuntimeCapability.DefaultImplementationsOfInterfaces:
                    return RuntimeSupportsFeature(assembly, SpecialMember.System_Runtime_CompilerServices_RuntimeFeature__DefaultImplementationsOfInterfaces);

                case RuntimeCapability.NumericIntPtr:
                    // CorLibrary should never be null, but that invariant is broken in some cases for MissingAssemblySymbol.
                    // Tracked by https://github.com/dotnet/roslyn/issues/61262
                    return assembly.CorLibrary is not null &&
                           RuntimeSupportsFeature(assembly, SpecialMember.System_Runtime_CompilerServices_RuntimeFeature__NumericIntPtr);

                case RuntimeCapability.UnmanagedSignatureCallingConvention:
                    return RuntimeSupportsFeature(assembly, SpecialMember.System_Runtime_CompilerServices_RuntimeFeature__UnmanagedSignatureCallingConvention);

                case RuntimeCapability.VirtualStaticsInInterfaces:
                    return RuntimeSupportsFeature(assembly, SpecialMember.System_Runtime_CompilerServices_RuntimeFeature__VirtualStaticsInInterfaces);

                default:
                    return false;
            }
        }

        private static bool RuntimeSupportsFeature(IAssemblySymbolInternal assembly, SpecialMember feature)
        {
            Debug.Assert((SpecialType)SpecialMembers.GetDescriptor(feature).DeclaringTypeId == SpecialType.System_Runtime_CompilerServices_RuntimeFeature);
            return assembly.IsStaticClass(assembly.GetSpecialType(SpecialType.System_Runtime_CompilerServices_RuntimeFeature)) &&
                   assembly.GetSpecialTypeMember(feature) is not null;
        }
    }
}
