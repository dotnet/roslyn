// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Debugging
{
    /// <summary>
    /// The kinds of custom debug info in Windows PDBs that we know how to interpret.
    /// The values correspond to possible values of the "kind" byte
    /// in the record header.
    /// </summary>
    internal enum CustomDebugInfoKind : byte
    {
        /// <summary>
        /// C# only. Encodes the sizes of using groups that are applicable to the method.
        /// The actual import strings are stored separately trhu ISymUnmanagedWriter.UsingNamespace.
        /// </summary>
        /// <remarks>
        /// Represented by <code>using</code> XML node in PDB tests.
        /// </remarks>
        UsingGroups = 0,

        /// <summary>
        /// C# only. Indicates that per-method debug information (import strings) is stored on another method, 
        /// whose token is specified.
        /// </summary>
        /// <remarks>
        /// Represented by <code>forward</code> XML node in PDB tests.
        /// </remarks>
        ForwardMethodInfo = 1,

        /// <summary>
        /// C# only. Indicates that per-module debug information (assembly reference aliases) is stored on another method, 
        /// whose token is specified.
        /// </summary>
        /// <remarks>
        /// Represented by <code>forwardToModule</code> XML node in PDB tests.
        /// </remarks>
        ForwardModuleInfo = 2,

        /// <summary>
        /// C# only. Specifies local scopes for state machine hoisted local variables.
        /// </summary>
        /// <remarks>
        /// Represented by <code>hoistedLocalScopes</code> XML node in PDB tests.
        /// Equivalent to <see cref="PortableCustomDebugInfoKinds.StateMachineHoistedLocalScopes"/> in Portable PDB.
        /// </remarks>
        StateMachineHoistedLocalScopes = 3,

        /// <summary>
        /// C# and VB. The name of the state machine type. Emitted for async and iterator kick-off methods.
        /// </summary>
        /// <remarks>
        /// Represented by <code>forwardIterator</code> XML node in PDB tests.
        /// </remarks>
        StateMachineTypeName = 4,

        /// <summary>
        /// C# only. Dynamic flags for local variables and constants.
        /// </summary>
        /// <remarks>
        /// Represented by <code>dynamicLocals</code> XML node in PDB tests.
        /// Equivalent to <see cref="PortableCustomDebugInfoKinds.DynamicLocalVariables"/> in Portable PDB.
        /// </remarks>
        DynamicLocals = 5,

        /// <summary>
        /// C# and VB. Encodes EnC local variable slot map.
        /// See https://github.com/dotnet/corefx/blob/main/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#EditAndContinueLocalSlotMap.
        /// </summary>
        /// <remarks>
        /// Represented by <code>encLocalSlotMap</code> XML node in PDB tests.
        /// Equivalent to <see cref="PortableCustomDebugInfoKinds.EncLocalSlotMap"/> in Portable PDB.
        /// </remarks>
        EditAndContinueLocalSlotMap = 6,

        /// <summary>
        /// C# and VB. Encodes EnC lambda map.
        /// See https://github.com/dotnet/corefx/blob/main/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#EditAndContinueLambdaAndClosureMap.
        /// </summary>
        /// <remarks>
        /// Represented by <code>encLambdaMap</code> XML node in PDB tests.
        /// Equivalent to <see cref="PortableCustomDebugInfoKinds.EncLambdaAndClosureMap"/> in Portable PDB.
        /// </remarks>
        EditAndContinueLambdaMap = 7,

        /// <summary>
        /// C# and VB. Tuple element names for local variables and constants.
        /// </summary>
        /// <remarks>
        /// Represented by <code>tupleElementNames</code> XML node in PDB tests.
        /// Equivalent to <see cref="PortableCustomDebugInfoKinds.TupleElementNames"/> in Portable PDB.
        /// </remarks>
        TupleElementNames = 8,

        /// <summary>
        /// C# and VB. Syntax offsets of nodes associated with state machine states in an async/iterator method and their corresponding state numbers.
        /// </summary>
        /// <remarks>
        /// Represented by <code>encStateMachineStateMap</code> XML node in PDB tests.
        /// Equivalent to <see cref="PortableCustomDebugInfoKinds.EncStateMachineStateMap"/> in Portable PDB.
        /// </remarks>
        EditAndContinueStateMachineStateMap = 9,
    }
}
