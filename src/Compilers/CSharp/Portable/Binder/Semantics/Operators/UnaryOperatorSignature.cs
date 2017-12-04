// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal struct UnaryOperatorSignature
    {
        public static UnaryOperatorSignature Error = default(UnaryOperatorSignature);

        public readonly MethodSymbol Method;
        public readonly TypeSymbol OperandType;
        public readonly TypeSymbol ReturnType;
        public readonly UnaryOperatorKind Kind;

        public UnaryOperatorSignature(UnaryOperatorKind kind, TypeSymbol operandType, TypeSymbol returnType, MethodSymbol method = null)
        {
            this.Kind = kind;
            this.OperandType = operandType;
            this.ReturnType = returnType;
            this.Method = method;
        }

        public override string ToString()
        {
            return $"kind: {this.Kind} operand: {this.OperandType} return: {this.ReturnType}";
        }

        public RefKind RefKind
        {
            get
            {
                Debug.Assert(
                    Method != null &&
                    Method.ParameterCount == 1 &&
                    (Method.ParameterRefKinds.IsDefaultOrEmpty || Method.ParameterRefKinds.Length == 1));

                return Method.ParameterRefKinds.IsDefaultOrEmpty
                    ? RefKind.None
                    : Method.ParameterRefKinds[0];
            }
        }
    }
}
