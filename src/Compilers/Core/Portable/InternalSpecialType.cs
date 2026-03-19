// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the Ids of types that are expected to be defined in core library.
    /// Unlike ids in <see cref="SpecialType"/> enum, these ids are not meant for public consumption
    /// and are meant for internal usage in compilers.
    /// </summary>
    internal enum InternalSpecialType : sbyte
    {
        // Value 0 represents an unknown type
        Unknown = SpecialType.None,

        /// <summary>
        /// Indicates that the type is <see cref="System.ReadOnlySpan{T}"/> from the COR library.
        /// </summary>
        /// <remarks>
        /// Check for this special type cannot be used to find the "canonical" definition of <see cref="ReadOnlySpan{T}"/>
        /// since it is fully legal for it to come from sources other than the COR library, e.g. from `System.Memory` package.
        /// The <see cref="WellKnownType.System_ReadOnlySpan_T"/> should be used for that purpose instead
        /// This entry mostly exists so that compiler can tell this type apart when resolving other members of the COR library
        /// </remarks>
        System_ReadOnlySpan_T = SpecialType.Count + 1,
        First = System_ReadOnlySpan_T,

        System_IFormatProvider,

        /// <summary>
        /// Indicates that the type is <see cref="System.Type"/> from the COR library.
        /// </summary>
        /// <remarks>
        /// Check for this special type cannot be used to find the "canonical" definition of <see cref="System.Type"/>
        /// since it is fully legal for it to come from sources other than the COR library.
        /// The <see cref="WellKnownType.System_Type"/> should be used for that purpose instead
        /// This entry mostly exists so that compiler can tell this type apart when resolving other members of the COR library
        /// </remarks>
        System_Type,

        /// <summary>
        /// Indicates that the type is <see cref="System.Reflection.MethodBase"/> from the COR library.
        /// </summary>
        /// <remarks>
        /// Check for this special type cannot be used to find the "canonical" definition of <see cref="System.Reflection.MethodBase"/>
        /// since it is fully legal for it to come from sources other than the COR library.
        /// The <see cref="WellKnownType.System_Reflection_MethodBase"/> should be used for that purpose instead
        /// This entry mostly exists so that compiler can tell this type apart when resolving other members of the COR library
        /// </remarks>
        System_Reflection_MethodBase,

        /// <summary>
        /// Indicates that the type is <see cref="System.Reflection.MethodInfo"/> from the COR library.
        /// </summary>
        /// <remarks>
        /// Check for this special type cannot be used to find the "canonical" definition of <see cref="System.Reflection.MethodInfo"/>
        /// since it is fully legal for it to come from sources other than the COR library.
        /// The <see cref="WellKnownType.System_Reflection_MethodInfo"/> should be used for that purpose instead
        /// This entry mostly exists so that compiler can tell this type apart when resolving other members of the COR library
        /// </remarks>
        System_Reflection_MethodInfo,

        /// <summary>
        /// Indicates that the type is <code>System.Runtime.CompilerServices.AsyncHelpers</code> from the COR library.
        /// </summary>
        System_Runtime_CompilerServices_AsyncHelpers,

        /// <summary>
        /// Indicates that the type is <see cref="System.Threading.Tasks.Task"/> from the COR library.
        /// </summary>
        /// <remarks>
        /// Check for this special type cannot be used to find the "canonical" definition of <see cref="System.Threading.Tasks.Task"/>
        /// since it is fully legal for it to come from sources other than the COR library.
        /// The <see cref="WellKnownType.System_Threading_Tasks_Task"/> should be used for that purpose instead.
        /// This entry mostly exists so that compiler can tell this type apart when resolving other members of the COR library.
        /// </remarks>
        System_Threading_Tasks_Task,

        /// <summary>
        /// Indicates that the type is <see cref="System.Threading.Tasks.Task{TResult}"/> from the COR library.
        /// </summary>
        /// <remarks>
        /// Check for this special type cannot be used to find the "canonical" definition of <see cref="System.Threading.Tasks.Task{TResult}"/>
        /// since it is fully legal for it to come from sources other than the COR library.
        /// The <see cref="WellKnownType.System_Threading_Tasks_Task_T"/> should be used for that purpose instead.
        /// This entry mostly exists so that compiler can tell this type apart when resolving other members of the COR library.
        /// </remarks>
        System_Threading_Tasks_Task_T,

        /// <summary>
        /// Indicates that the type is <see cref="System.Threading.Tasks.ValueTask"/> from the COR library.
        /// </summary>
        /// <remarks>
        /// Check for this special type cannot be used to find the "canonical" definition of <see cref="System.Threading.Tasks.ValueTask"/>
        /// since it is fully legal for it to come from sources other than the COR library.
        /// The <see cref="WellKnownType.System_Threading_Tasks_ValueTask"/> should be used for that purpose instead.
        /// This entry mostly exists so that compiler can tell this type apart when resolving other members of the COR library.
        /// </remarks>
        System_Threading_Tasks_ValueTask,

        /// <summary>
        /// Indicates that the type is <see cref="System.Threading.Tasks.ValueTask{TResult}"/> from the COR library.
        /// </summary>
        /// <remarks>
        /// Check for this special type cannot be used to find the "canonical" definition of <see cref="System.Threading.Tasks.ValueTask{TResult}"/>
        /// since it is fully legal for it to come from sources other than the COR library.
        /// The <see cref="WellKnownType.System_Threading_Tasks_ValueTask_T"/> should be used for that purpose instead.
        /// This entry mostly exists so that compiler can tell this type apart when resolving other members of the COR library.
        /// </remarks>
        System_Threading_Tasks_ValueTask_T,

        /// <summary>
        /// Indicates that the type is <see cref="System.Runtime.CompilerServices.ICriticalNotifyCompletion"/> from the COR library.
        /// </summary>
        /// <remarks>
        /// Check for this special type cannot be used to find the "canonical" definition of <see cref="System.Runtime.CompilerServices.ICriticalNotifyCompletion"/>
        /// since it is fully legal for it to come from sources other than the COR library.
        /// The <see cref="WellKnownType.System_Runtime_CompilerServices_ICriticalNotifyCompletion"/> should be used for that purpose instead.
        /// This entry mostly exists so that compiler can tell this type apart when resolving other members of the COR library.
        /// </remarks>
        System_Runtime_CompilerServices_ICriticalNotifyCompletion,

        /// <summary>
        /// Indicates that this type is the System.Runtime.InteropServices.ExtendedLayoutAttribute type from the COR library.
        /// </summary>
        System_Runtime_InteropServices_ExtendedLayoutAttribute,
        /// <summary>
        /// Indicates that this type is the System.Runtime.InteropServices.ExtendedLayoutKind type from the COR library.
        /// </summary>
        System_Runtime_InteropServices_ExtendedLayoutKind,

        /// <summary>
        /// This item should be kept last and it doesn't represent any specific type.
        /// </summary>
        NextAvailable
    }
}
