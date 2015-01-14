// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal struct BinaryOperatorSignature : IEquatable<BinaryOperatorSignature>
    {
        public static BinaryOperatorSignature Error = default(BinaryOperatorSignature);

        public readonly TypeSymbol LeftType;
        public readonly TypeSymbol RightType;
        public readonly TypeSymbol ReturnType;
        public readonly MethodSymbol Method;
        public readonly BinaryOperatorKind Kind;

        /// <summary>
        /// To duplicate native compiler behavior for some scenarios we force a priority among
        /// operators. If two operators are both applicable and both have a non-null Priority,
        /// the one with the numerically lower Priority value is preferred.
        /// </summary>
        public int? Priority;

        public BinaryOperatorSignature(BinaryOperatorKind kind, TypeSymbol leftType, TypeSymbol rightType, TypeSymbol returnType, MethodSymbol method = null)
        {
            this.Kind = kind;
            this.LeftType = leftType;
            this.RightType = rightType;
            this.ReturnType = returnType;
            this.Method = method;
            this.Priority = null;
        }

        public override string ToString()
        {
            return $"kind: {this.Kind} left: {this.LeftType} right: {this.RightType} return: {this.ReturnType}";
        }

        public bool Equals(BinaryOperatorSignature other)
        {
            return
                this.Kind == other.Kind &&
                this.LeftType == other.LeftType &&
                this.RightType == other.RightType &&
                this.ReturnType == other.ReturnType &&
                this.Method == other.Method;
        }

        public static bool operator ==(BinaryOperatorSignature x, BinaryOperatorSignature y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(BinaryOperatorSignature x, BinaryOperatorSignature y)
        {
            return !x.Equals(y);
        }

        public override bool Equals(object obj)
        {
            return obj is BinaryOperatorSignature && Equals((BinaryOperatorSignature)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(ReturnType,
                   Hash.Combine(LeftType,
                   Hash.Combine(RightType,
                   Hash.Combine(Method, (int)Kind))));
        }
    }
}
