// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Emit;

/// <summary>
/// Describes rude edit to be reported at runtime.
/// </summary>
public readonly struct RuntimeRudeEdit
{
    /// <summary>
    /// Error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Error code.
    /// </summary>
    public int ErrorCode { get; }

    [Obsolete("Specify errorCode")]
    public RuntimeRudeEdit(string message)
        : this(message, errorCode: 0)
    {
    }

    public RuntimeRudeEdit(string message, int errorCode)
    {
        if (errorCode < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(errorCode));
        }

        Message = message;
        ErrorCode = errorCode;
    }

    internal RuntimeRudeEdit(HotReloadExceptionCode code)
    {
        Message = code.GetExceptionMessage();
        ErrorCode = code.GetExceptionCodeValue();
    }
}
