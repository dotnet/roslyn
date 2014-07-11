// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
namespace Microsoft.CodeAnalysis.CSharp
{
    internal struct BinaryOperatorSignature
    {
        public static BinaryOperatorSignature Error = default(BinaryOperatorSignature);

        public readonly TypeSymbol LeftType;
        public readonly TypeSymbol RightType;
        public readonly TypeSymbol ReturnType;
        public readonly MethodSymbol Method;
        public readonly BinaryOperatorKind Kind;

        public BinaryOperatorSignature(BinaryOperatorKind kind, TypeSymbol leftType, TypeSymbol rightType, TypeSymbol returnType, MethodSymbol method = null)
        {
            this.Kind = kind;
            this.LeftType = leftType;
            this.RightType = rightType;
            this.ReturnType = returnType;
            this.Method = method;
        }

        public override string ToString()
        {
            return string.Format("kind: {0} left: {1} right: {2} return: {3}",
                this.Kind, this.LeftType, this.RightType, this.ReturnType);
        }

        public bool Equals(BinaryOperatorSignature other)
        {
            return
                this.Kind == other.Kind &&
                this.LeftType.Equals(other.LeftType) &&
                this.RightType.Equals(other.RightType) &&
                this.ReturnType.Equals(other.ReturnType) &&
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
