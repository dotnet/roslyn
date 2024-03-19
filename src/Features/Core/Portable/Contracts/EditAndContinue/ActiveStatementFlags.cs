// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue;

/// <summary>
/// Flags regarding active statements information.
/// </summary>
[Flags]
internal enum ActiveStatementFlags
{
    None = 0,

    /// <summary>
    /// At least one of the threads whom this active statement belongs to is in a leaf frame.
    /// </summary>
    LeafFrame = 1,

    /// <summary>
    /// The statement is partially executed.
    /// </summary>
    /// <remarks>
    /// An active statement is partially executed if the thread is stopped in between two sequence points.
    /// This may happen when the users steps through the code in disassembly window (stepping over machine instructions),
    /// when the compiler emits a call to Debugger.Break (VB Stop statement), etc.
    /// 
    /// Partially executed active statement can't be edited.
    /// </remarks>
    PartiallyExecuted = 2,

    /// <summary>
    /// The statement IL is not in user code.
    /// </summary>
    NonUserCode = 4,

    /// <summary>
    /// Indicates that the active statement instruction belongs to the latest version of the containing method.
    /// If not set, the containing method was updated but the active statement was not remapped yet because the thread 
    /// has not returned to that instruction yet and was not remapped to the new version.
    /// </summary>
    /// <remarks>
    /// When the debugger asks the CLR for the active statement information it compares ICorDebugFunction.GetVersionNumber()
    /// and ICorDebugFunction.GetCurrentVersionNumber() to determine the value of this flag.
    /// </remarks>
    MethodUpToDate = 8,

    /// <summary>
    /// At least one of the threads whom this active statement belongs to is in a non-leaf frame.
    /// </summary>
    NonLeafFrame = 16,

    /// <summary>
    /// When applying updates while the code is executing, we will not attempt any remap for methods which are on the
    /// executing stack. This is done so we can avoid blocking an edit due an executing active statement. 
    /// Language services needs to acknowledge such active statements when emitting further remap information.
    /// </summary>
    Stale = 32
}
