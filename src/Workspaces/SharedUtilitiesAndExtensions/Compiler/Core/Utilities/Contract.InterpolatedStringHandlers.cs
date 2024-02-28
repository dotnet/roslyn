// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Text;

#pragma warning disable IDE0060 // Remove unused parameter - https://github.com/dotnet/roslyn/issues/58168

namespace Roslyn.Utilities;

internal static partial class Contract
{
    [InterpolatedStringHandler]
    public readonly struct ThrowIfTrueInterpolatedStringHandler
    {
        private readonly StringBuilder _stringBuilder;

        public ThrowIfTrueInterpolatedStringHandler(int literalLength, int formattedCount, bool condition, out bool success)
        {
            _stringBuilder = condition ? new StringBuilder(capacity: literalLength) : null!;
            success = condition;
        }

        public void AppendLiteral(string value) => _stringBuilder.Append(value);

        public void AppendFormatted<T>(T value) => _stringBuilder.Append(value?.ToString());

        public string GetFormattedText() => _stringBuilder.ToString();
    }

    [InterpolatedStringHandler]
    public readonly struct ThrowIfFalseInterpolatedStringHandler
    {
        private readonly StringBuilder _stringBuilder;

        public ThrowIfFalseInterpolatedStringHandler(int literalLength, int formattedCount, bool condition, out bool success)
        {
            _stringBuilder = condition ? null! : new StringBuilder(capacity: literalLength);
            success = !condition;
        }

        public void AppendLiteral(string value) => _stringBuilder.Append(value);

        public void AppendFormatted<T>(T value) => _stringBuilder.Append(value?.ToString());

        public string GetFormattedText() => _stringBuilder.ToString();
    }

    [InterpolatedStringHandler]
    public readonly struct ThrowIfNullInterpolatedStringHandler<T>
    {
        private readonly StringBuilder _stringBuilder;

        public ThrowIfNullInterpolatedStringHandler(int literalLength, int formattedCount, T? value, out bool success)
        {
            _stringBuilder = value is null ? new StringBuilder(capacity: literalLength) : null!;
            success = value is null;
        }

        public void AppendLiteral(string value) => _stringBuilder.Append(value);

        public void AppendFormatted<T2>(T2 value) => _stringBuilder.Append(value?.ToString());

        public string GetFormattedText() => _stringBuilder.ToString();
    }
}
