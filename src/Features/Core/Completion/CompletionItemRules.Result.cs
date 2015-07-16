// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Completion
{
    internal partial class CompletionItemRules
    {
        public struct Result<T> : IEquatable<Result<T>>
        {
            private readonly bool useValue;
            private readonly T value;

            private Result(T value)
            {
                this.useValue = true;
                this.value = value;
            }

            public static Result<T> Default { get; } = new Result<T>();

            public bool UseDefault => !this.useValue;

            public static implicit operator Result<T>(T value)
            {
                return new Result<T>(value);
            }

            public static explicit operator T(Result<T> result)
            {
                if (result.useValue)
                {
                    return result.value;
                }

                throw new InvalidOperationException("Can't return value for default");
            }

            public bool Equals(Result<T> other)
            {
                if (this.UseDefault && other.UseDefault)
                {
                    return true;
                }

                return this.useValue == other.useValue
                    && EqualityComparer<T>.Default.Equals(value, other.value);
            }

            public override bool Equals(object obj)
            {
                if (obj is Result<T>)
                {
                    return Equals((Result<T>)obj);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return EqualityComparer<bool>.Default.GetHashCode(this.useValue)
                     ^ EqualityComparer<T>.Default.GetHashCode(this.value);
            }

            public override string ToString()
            {
                if (!this.useValue)
                {
                    return "{use default}";
                }

                return this.value == null ? "{null}" : value.ToString();
            }
        }
    }
}
