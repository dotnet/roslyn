// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Describes a method, lambda or local function that generates a state machine.
/// </summary>
/// <param name="IsAsync">
/// If the method is marked as async and generates a state machine.
/// Method marked with async keyword in C# and VB generates a state machine even if it doesn't have any await expressions (<paramref name="HasSuspensionPoints"/> is false).
/// </param>
/// <param name="IsIterator">
/// If the method is marked as iterator and generates a state machine.
/// In C# an (async) iterator method must have a yield statement (<paramref name="IsIterator"/> is true, <paramref name="HasSuspensionPoints"/> is true).
/// In VB a method without a Yield statement can be marked as an Iterator (<paramref name="IsIterator"/> is true, <paramref name="HasSuspensionPoints"/> may be false).
/// </param>
/// <param name="HasSuspensionPoints">
/// True if any awaits and/or yields are present in the method.
/// </param>
internal readonly record struct StateMachineInfo(bool IsAsync, bool IsIterator, bool HasSuspensionPoints)
{
    public static StateMachineInfo None = default;

    /// <summary>
    /// True if a state machine is generated.
    /// </summary>
    public bool IsStateMachine => IsAsync || IsIterator;
}
