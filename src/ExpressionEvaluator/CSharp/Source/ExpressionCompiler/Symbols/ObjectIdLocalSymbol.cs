// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class ObjectIdLocalSymbol : PlaceholderLocalSymbol
    {
        private readonly bool _isWritable;

        internal ObjectIdLocalSymbol(MethodSymbol method, TypeSymbol type, string name, string displayName, bool isWritable) :
            base(method, name, displayName, type)
        {
            _isWritable = isWritable;
        }

        internal override bool IsWritable
        {
            get { return _isWritable; }
        }

        internal override BoundExpression RewriteLocal(CSharpCompilation compilation, EENamedTypeSymbol container, CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
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
                local.Type.TypeSymbol);
        }

        private sealed class ObjectIdExpressions : PseudoVariableExpressions
        {
            private readonly CSharpCompilation _compilation;

            internal ObjectIdExpressions(CSharpCompilation compilation)
            {
                _compilation = compilation;
            }

            internal override BoundExpression GetValue(BoundPseudoVariable variable, DiagnosticBag diagnostics)
            {
                var method = GetIntrinsicMethod(_compilation, ExpressionCompilerConstants.GetVariableValueMethodName);
                var local = variable.LocalSymbol;
                var expr = InvokeGetMethod(method, variable.Syntax, local.Name);
                return ConvertToLocalType(_compilation, expr, local.Type.TypeSymbol, diagnostics);
            }

            internal override BoundExpression GetAddress(BoundPseudoVariable variable)
            {
                var method = GetIntrinsicMethod(_compilation, ExpressionCompilerConstants.GetVariableAddressMethodName);
                // Currently the MetadataDecoder does not support byref return types
                // so the return type of GetVariableAddress(Of T)(name As String)
                // is an error type. Since the method is only used for emit, an
                // updated placeholder method is used instead.

                // TODO: refs are available
                // Debug.Assert(method.ReturnType.TypeKind == TypeKind.Error); // If byref return types are supported in the future, use method as is.
                method = new PlaceholderMethodSymbol(
                    method.ContainingType,
                    method.Name,
                    m => method.TypeParameters.SelectAsArray(t => (TypeParameterSymbol)new SimpleTypeParameterSymbol(m, t.Ordinal, t.Name)),
                    m => m.TypeParameters[0], // return type is <>T&
                    m => method.Parameters.SelectAsArray(p => (ParameterSymbol)new SynthesizedParameterSymbol(m, p.Type.TypeSymbol, p.Ordinal, p.RefKind, p.Name, p.Type.CustomModifiers, p.CountOfCustomModifiersPrecedingByRef)));
                var local = variable.LocalSymbol;
                return InvokeGetMethod(method.Construct(local.Type.TypeSymbol), variable.Syntax, local.Name);
            }

            private static BoundExpression InvokeGetMethod(MethodSymbol method, CSharpSyntaxNode syntax, string name)
            {
                var argument = new BoundLiteral(
                    syntax,
                    Microsoft.CodeAnalysis.ConstantValue.Create(name),
                    method.Parameters[0].Type.TypeSymbol);
                return BoundCall.Synthesized(
                    syntax,
                    receiverOpt: null,
                    method: method,
                    arguments: ImmutableArray.Create<BoundExpression>(argument));
            }
        }
    }
}
