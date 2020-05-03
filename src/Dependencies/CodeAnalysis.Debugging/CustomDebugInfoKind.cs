// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Debugging
{
    /// <summary>
    /// The kinds of custom debug info that we know how to interpret.
    /// The values correspond to possible values of the "kind" byte
    /// in the record header.
    /// </summary>
    internal enum CustomDebugInfoKind : byte
    {
        /// <summary>
        /// C# only. Encodes the sizes of using groups that are applicable to the method.
        /// The actual import strings are stored separately trhu ISymUnmanagedWriter.UsingNamespace.
        /// </summary>
        UsingGroups = 0,

        /// <summary>
        /// C# only. Indicates that per-method debug information (import strings) is stored on another method, 
        /// whose token is specified.
        /// </summary>
        ForwardMethodInfo = 1,

        /// <summary>
        /// C# only. Indicates that per-module debug information (assembly reference aliases) is stored on another method, 
        /// whose token is specified.
        /// </summary>
        ForwardModuleInfo = 2,

        /// <summary>
        /// C# only. Specifies local scopes for state machine hoisted local variables.
        /// </summary>
        StateMachineHoistedLocalScopes = 3,

        /// <summary>
        /// C# and VB. The name of the state machine type. Emitted for async and iterator kick-off methods.
        /// </summary>
        StateMachineTypeName = 4,

        /// <summary>
        /// C# only. Dynamic flags for local variables and constants.
        /// </summary>
        DynamicLocals = 5,

        /// <summary>
        /// C# and VB. Encodes EnC local variable slot map.
        /// See https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#EditAndContinueLocalSlotMap.
        /// </summary>
        EditAndContinueLocalSlotMap = 6,

        /// <summary>
        /// C# and VB. Encodes EnC lambda map.
        /// See https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#EditAndContinueLambdaAndClosureMap.
        /// </summary>
        EditAndContinueLambdaMap = 7,

        /// <summary>
        /// C# and VB. Tuple element names for local variables and constants.
        /// </summary>
        TupleElementNames = 8,
    }
}
