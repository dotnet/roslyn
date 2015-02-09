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
            var parameterType = compilation.GetSpecialType(SpecialType.System_String);
            var getValueMethod = container.GetOrAddSynthesizedMethod(
                ExpressionCompilerConstants.GetVariableValueMethodName,
                (c, n, s) =>
                {
                    var returnType = compilation.GetSpecialType(SpecialType.System_Object);
                    return new PlaceholderMethodSymbol(
                        c,
                        s,
                        n,
                        returnType,
                        m => ImmutableArray.Create<ParameterSymbol>(new SynthesizedParameterSymbol(m, parameterType, ordinal: 0, refKind: RefKind.None)));
                });
            var getAddressMethod = container.GetOrAddSynthesizedMethod(
                ExpressionCompilerConstants.GetVariableAddressMethodName,
                (c, n, s) =>
                {
                    return new PlaceholderMethodSymbol(
                        c,
                        s,
                        n,
                        m => ImmutableArray.Create<TypeParameterSymbol>(new SimpleTypeParameterSymbol(m, 0, "<>T")),
                        m => m.TypeParameters[0], // return type is <>T&
                        m => ImmutableArray.Create<ParameterSymbol>(new SynthesizedParameterSymbol(m, parameterType, ordinal: 0, refKind: RefKind.None)),
                        returnValueIsByRef: true);
                });
            return new BoundPseudoVariable(
                syntax,
                local,
                new ObjectIdExpressions(compilation, getValueMethod, getAddressMethod),
                local.Type);
        }

        private sealed class ObjectIdExpressions : PseudoVariableExpressions
        {
            private readonly CSharpCompilation _compilation;
            private readonly MethodSymbol _getValueMethod;
            private readonly MethodSymbol _getAddressMethod;

            internal ObjectIdExpressions(CSharpCompilation compilation, MethodSymbol getValueMethod, MethodSymbol getAddressMethod)
            {
                _compilation = compilation;
                _getValueMethod = getValueMethod;
                _getAddressMethod = getAddressMethod;
            }

            internal override BoundExpression GetValue(BoundPseudoVariable variable)
            {
                var local = variable.LocalSymbol;
                var expr = InvokeGetMethod(_getValueMethod, variable.Syntax, local.Name);
                return ConvertToLocalType(_compilation, expr, local.Type);
            }

            internal override BoundExpression GetAddress(BoundPseudoVariable variable)
            {
                var local = variable.LocalSymbol;
                return InvokeGetMethod(_getAddressMethod.Construct(local.Type), variable.Syntax, local.Name);
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
