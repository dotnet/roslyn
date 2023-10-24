// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal enum DeclarationBehavior
{
    None,
    Delete,
    MoveIn,
    MoveOut,
    SplitIn,
    SplitOut
}

internal enum ReturnBehavior
{
    None,
    Initialization,
    Assignment
}

internal enum ParameterBehavior
{
    None,
    Input,
    Out,
    Ref
}

/// <summary>
/// status code for extract method operations
/// </summary>
[Flags]
internal enum OperationStatusFlag
{
    Failed = 0x0,

    /// <summary>
    /// operation has succeeded
    /// </summary>
    Succeeded = 0x1,
}
