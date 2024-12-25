// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;

internal readonly struct Result<T>
{
    public readonly bool Success;
    public readonly T? Value;

    public static readonly Result<T> Abort = new(false, default);
    public static readonly Result<T> Empty = new(true, default);

    public Result(T? value)
        : this(true, value)
    { }

    private Result(bool success, T? value)
    {
        Success = success;
        Value = value;
    }

    public void Deconstruct(out bool success, out T? value)
    {
        success = Success;
        value = Value;
    }

    public static implicit operator Result<T>(T value) => new(value);
}
