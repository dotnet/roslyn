// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class ObjectIdLocalSymbol : PlaceholderLocalSymbol
    {
        private readonly string _id;
        private readonly bool _isWritable;

        internal ObjectIdLocalSymbol(MethodSymbol method, TypeSymbol type, string id, bool isWritable) :
            base(method, id, type)
        {
            _id = id;
            _isWritable = isWritable;
        }

        internal override bool IsWritable
        {
            get { return _isWritable; }
        }

        internal override BoundExpression RewriteLocal(CSharpCompilation compilation, EENamedTypeSymbol container, CSharpSyntaxNode syntax)
        {
            return RewriteLocalInternal(compilation, container, syntax, this);
        }

        internal static BoundExpression RewriteLocal(CSharpCompilation compilation, EENamedTypeSymbol container, CSharpSyntaxNode syntax, LocalSymbol local)
        {
            return RewriteLocalInternal(compilation, container, syntax, local);
        }

        private static BoundExpression RewriteLocalInternal(CSharpCompilation compilation, EENamedTypeSymbol container, CSharpSyntaxNode syntax, LocalSymbol local)
        {
            return new BoundPseudoVariable(
                syntax,
                local,
                new ObjectIdExpressions(compilation),
                local.Type);
        }

        private sealed class ObjectIdExpressions : PseudoVariableExpressions
        {
            private readonly CSharpCompilation _compilation;

            internal ObjectIdExpressions(CSharpCompilation compilation)
            {
                _compilation = compilation;
            }

            internal override BoundExpression GetValue(BoundPseudoVariable variable)
            {
                var getValueMethod = GetIntrinsicMethod(this._compilation, ExpressionCompilerConstants.GetVariableValueMethodName);
                var local = variable.LocalSymbol;
                var expr = InvokeGetMethod(getValueMethod, variable.Syntax, local.Name);
                return ConvertToLocalType(_compilation, expr, local.Type);
            }

            internal override BoundExpression GetAddress(BoundPseudoVariable variable)
            {
                var getAddressMethod = GetIntrinsicMethod(this._compilation, ExpressionCompilerConstants.GetVariableAddressMethodName);
                var local = variable.LocalSymbol;
                return InvokeGetMethod(getAddressMethod.Construct(local.Type), variable.Syntax, local.Name);
            }

            private static BoundExpression InvokeGetMethod(MethodSymbol method, CSharpSyntaxNode syntax, string name)
            {
                var argument = new BoundLiteral(
                    syntax,
                    Microsoft.CodeAnalysis.ConstantValue.Create(name),
                    method.Parameters[0].Type);
                return BoundCall.Synthesized(
                    syntax,
                    receiverOpt: null,
                    method: method,
                    arguments: ImmutableArray.Create<BoundExpression>(argument));
            }
        }
    }
}
