// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor;

internal static partial class Assumed
{
    [InterpolatedStringHandler]
    public readonly ref struct ThrowIfTrueInterpolatedStringHandler
    {
        private readonly PooledStringBuilderHelper _builder;

        public ThrowIfTrueInterpolatedStringHandler(int literalLength, int formattedCount, bool condition, out bool success)
        {
            success = condition;
            _builder = new(literalLength, success);
        }

        public void AppendLiteral(string value)
            => _builder.AppendLiteral(value);

        public void AppendFormatted<TValue>(TValue value)
            => _builder.AppendFormatted(value);

        public void AppendFormatted<TValue>(TValue value, string format)
            where TValue : IFormattable
            => _builder.AppendFormatted(value, format);

        public string GetFormattedText()
            => _builder.GetFormattedText();
    }

    [InterpolatedStringHandler]
    public readonly ref struct ThrowIfFalseInterpolatedStringHandler
    {
        private readonly PooledStringBuilderHelper _builder;

        public ThrowIfFalseInterpolatedStringHandler(int literalLength, int formattedCount, bool condition, out bool success)
        {
            success = !condition;
            _builder = new(literalLength, success);
        }

        public void AppendLiteral(string value)
            => _builder.AppendLiteral(value);

        public void AppendFormatted<TValue>(TValue value)
            => _builder.AppendFormatted(value);

        public void AppendFormatted<TValue>(TValue value, string format)
            where TValue : IFormattable
            => _builder.AppendFormatted(value, format);

        public string GetFormattedText()
            => _builder.GetFormattedText();
    }

    [InterpolatedStringHandler]
    public readonly ref struct ThrowIfNullInterpolatedStringHandler<T>
    {
        private readonly PooledStringBuilderHelper _builder;

        public ThrowIfNullInterpolatedStringHandler(int literalLength, int formattedCount, T? value, out bool success)
        {
            success = value is null;
            _builder = new(literalLength, success);
        }

        public void AppendLiteral(string value)
            => _builder.AppendLiteral(value);

        public void AppendFormatted<TValue>(TValue value)
            => _builder.AppendFormatted(value);

        public void AppendFormatted<TValue>(TValue value, string format)
            where TValue : IFormattable
            => _builder.AppendFormatted(value, format);

        public string GetFormattedText()
            => _builder.GetFormattedText();
    }

    [InterpolatedStringHandler]
    public readonly ref struct UnreachableInterpolatedStringHandler
    {
        private readonly PooledStringBuilderHelper _builder;

        public UnreachableInterpolatedStringHandler(int literalLength, int formattedCount)
        {
            _builder = new(literalLength, condition: true);
        }

        public void AppendLiteral(string value)
            => _builder.AppendLiteral(value);

        public void AppendFormatted<TValue>(TValue value)
            => _builder.AppendFormatted(value);

        public void AppendFormatted<TValue>(TValue value, string format)
            where TValue : IFormattable
            => _builder.AppendFormatted(value, format);

        public string GetFormattedText()
            => _builder.GetFormattedText();
    }

    private ref struct PooledStringBuilderHelper
    {
        private StringBuilder? _builder;

        public PooledStringBuilderHelper(int capacity, bool condition)
        {
            if (condition)
            {
                _builder = StringBuilderPool.Default.Get();
                _builder.EnsureCapacity(capacity);
            }
        }

        public readonly void AppendLiteral(string value)
            => _builder!.Append(value);

        public readonly void AppendFormatted<T>(T value)
            => _builder!.Append(value?.ToString());

        public readonly void AppendFormatted<TValue>(TValue value, string format)
            where TValue : IFormattable
            => _builder!.Append(value?.ToString(format, formatProvider: null));

        public string GetFormattedText()
        {
            var builder = Interlocked.Exchange(ref _builder, null);

            if (builder is not null)
            {
                var result = builder.ToString();
                StringBuilderPool.Default.Return(builder);

                return result;
            }

            // GetFormattedText() should never be called if the condition passed in was false.
            return Unreachable<string>();
        }
    }
}
