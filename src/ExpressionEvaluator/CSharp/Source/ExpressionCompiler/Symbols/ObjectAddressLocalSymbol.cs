﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class ObjectAddressLocalSymbol : PlaceholderLocalSymbol
    {
        private readonly ulong _address;

        internal ObjectAddressLocalSymbol(MethodSymbol method, string name, TypeSymbol type, ulong address) :
            base(method, name, name, type)
        {
            Debug.Assert(type.SpecialType == SpecialType.System_Object);
            _address = address;
        }

        internal override bool IsWritable
        {
            // Return true?
            get { return false; }
        }

        internal override BoundExpression RewriteLocal(CSharpCompilation compilation, EENamedTypeSymbol container, SyntaxNode syntax, DiagnosticBag diagnostics)
        {
            var method = GetIntrinsicMethod(compilation, ExpressionCompilerConstants.GetObjectAtAddressMethodName);
            var argument = new BoundLiteral(
                syntax,
                Microsoft.CodeAnalysis.ConstantValue.Create(_address),
                method.Parameters[0].Type);
            var call = BoundCall.Synthesized(
                syntax,
                receiverOpt: null,
                method: method,
                arguments: ImmutableArray.Create<BoundExpression>(argument));
            Debug.Assert(call.Type == this.Type);
            return call;
        }
    }
}
