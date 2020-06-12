
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable enable

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Root type for operation constant values.
    /// </summary>
    internal abstract class OperationConstantValue
    {
        internal static OperationConstantValue None => NoConstantValue.Instance;
        internal static OperationConstantValue Create(ConstantValue? originalConstant)
            => originalConstant switch
            {
                null => NoConstantValue.Instance,
                { IsBad: true } => NoConstantValue.Instance,
                { IsString: true } => new StringConstantValue(originalConstant),
                { IsBoolean: true } => originalConstant.BooleanValue ? NonStringConstantValue.True : NonStringConstantValue.False,
                { IsNull: true } => NonStringConstantValue.Null,
                _ => new NonStringConstantValue(originalConstant.Value)
            };

        internal static OperationConstantValue FromBoolean(bool value)
            => Create(value ? ConstantValue.True : ConstantValue.False);

        protected OperationConstantValue() { }

        internal abstract Optional<object?> Value { get; }

        private sealed class NoConstantValue : OperationConstantValue
        {
            internal static readonly NoConstantValue Instance = new NoConstantValue();
            private NoConstantValue() { }
            internal override Optional<object?> Value => default;
        }

        private sealed class NonStringConstantValue : OperationConstantValue
        {
            internal static readonly NonStringConstantValue True = new NonStringConstantValue(true);
            internal static readonly NonStringConstantValue False = new NonStringConstantValue(false);
            internal static readonly NonStringConstantValue Null = new NonStringConstantValue(null);
            internal NonStringConstantValue(object? constantValue)
            {
                Debug.Assert(!(constantValue is string));
                Value = new Optional<object?>(constantValue);
            }

            internal override Optional<object?> Value { get; }
        }

        private sealed class StringConstantValue : OperationConstantValue
        {
            /// <summary>
            /// Some string constant values can have large costs to realize. To compensate, we realize
            /// constant values lazily, and hold onto a weak reference. If the next time a user asks for the contant
            /// value the previous one still exists, we can avoid rerealizing it. But we don't want to root the constant
            /// value if no users are still using it.
            /// </summary>
            private WeakReference<string>? _constantValueReference;
            private readonly ConstantValue _originalConstantValue;

            internal StringConstantValue(ConstantValue originalConstantValue)
            {
                Debug.Assert(originalConstantValue.IsString);
                _originalConstantValue = originalConstantValue;
            }

            internal override Optional<object?> Value
            {
                get
                {
                    string? constantValue = null;
                    if (_constantValueReference?.TryGetTarget(out constantValue) != true)
                    {
                        // Note: we could end up realizing the constant value multiple times if there's
                        // a race here. Currently, this isn't believed to be an issue, as the assignment
                        // to _constantValueReference is atomic so the worst that will happen is we return
                        // different instances of a string constant to users.
                        constantValue = _originalConstantValue.StringValue;
                        Debug.Assert(constantValue != null);
                        _constantValueReference = new WeakReference<string>(constantValue);
                    }

                    return new Optional<object?>(constantValue);
                }
            }
        }
    }
}
