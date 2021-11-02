// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame
{
    internal partial struct StackFrameParser
    {
        private readonly struct ParseResult<T>
        {
            public readonly bool Success;
            public readonly T? Value;

            public static readonly ParseResult<T> Abort = new(false, default);
            public static readonly ParseResult<T> Empty = new(true, default);

            public ParseResult(T? value)
                : this(true, value)
            { }

            private ParseResult(bool success, T? value)
            {
                Success = success;
                Value = value;
            }

            public void Deconstruct(out bool success, out T? value)
            {
                success = Success;
                value = Value;
            }

            public static implicit operator ParseResult<T>(T value) => new(value);
        }
    }
}
