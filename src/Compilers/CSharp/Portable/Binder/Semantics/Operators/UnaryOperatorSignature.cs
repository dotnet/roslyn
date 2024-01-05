// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal struct UnaryOperatorSignature
    {
        public static UnaryOperatorSignature Error = default(UnaryOperatorSignature);

        public readonly MethodSymbol Method;
        public readonly TypeSymbol ConstrainedToTypeOpt;
        public readonly TypeSymbol OperandType;
        public readonly TypeSymbol ReturnType;
        public readonly UnaryOperatorKind Kind;

        public UnaryOperatorSignature(UnaryOperatorKind kind, TypeSymbol operandType, TypeSymbol returnType)
        {
            this.Kind = kind;
            this.OperandType = operandType;
            this.ReturnType = returnType;
            this.Method = null;
            this.ConstrainedToTypeOpt = null;
        }

        public UnaryOperatorSignature(UnaryOperatorKind kind, TypeSymbol operandType, TypeSymbol returnType, MethodSymbol method, TypeSymbol constrainedToTypeOpt)
        {
            this.Kind = kind;
            this.OperandType = operandType;
            this.ReturnType = returnType;
            this.Method = method;
            this.ConstrainedToTypeOpt = constrainedToTypeOpt;
        }

        public override string ToString()
        {
            return $"kind: {this.Kind} operandType: {this.OperandType} operandRefKind: {this.RefKind} return: {this.ReturnType}";
        }

        public RefKind RefKind
        {
            get
            {
                if ((object)Method != null)
                {
                    Debug.Assert(Method.ParameterCount == 1);

                    if (!Method.ParameterRefKinds.IsDefaultOrEmpty)
                    {
                        Debug.Assert(Method.ParameterRefKinds.Length == 1);

                        return Method.ParameterRefKinds.Single();
                    }
                }

                return RefKind.None;
            }
        }
    }
}
