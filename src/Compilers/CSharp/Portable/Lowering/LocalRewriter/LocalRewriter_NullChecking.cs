// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        internal static ImmutableArray<BoundStatement> ConstructNullCheckedStatementList(
            ImmutableArray<ParameterSymbol> parameters,
            SyntheticBoundNodeFactory factory)
        {
            ArrayBuilder<BoundStatement>? statementList = null;
            MethodSymbol? throwIfNullMethod = null;
            foreach (ParameterSymbol param in parameters)
            {
                if (param.IsNullChecked)
                {
                    var isNullCheckableValueType = param.Type.IsNullableTypeOrTypeParameter() || param.Type.IsPointerOrFunctionPointer();
                    Debug.Assert(!param.Type.IsValueType || isNullCheckableValueType);
                    statementList ??= ArrayBuilder<BoundStatement>.GetInstance();
                    var constructedIf = isNullCheckableValueType
                        ? ConstructDirectNullCheck(param, factory)
                        : ConstructNullCheckHelperCall(param, ref throwIfNullMethod, factory);
                    statementList.Add(constructedIf);
                }
            }

            return statementList?.ToImmutableAndFree() ?? ImmutableArray<BoundStatement>.Empty;
        }

        private static BoundStatement ConstructNullCheckHelperCall(ParameterSymbol parameter, ref MethodSymbol? throwIfNullMethod, SyntheticBoundNodeFactory factory)
        {
            if (throwIfNullMethod is null)
            {
                Debug.Assert(factory.ModuleBuilderOpt is not null);
                var module = factory.ModuleBuilderOpt!;
                var diagnosticSyntax = factory.CurrentFunction.GetNonNullSyntaxNode();
                var diagnostics = factory.Diagnostics.DiagnosticBag;
                Debug.Assert(diagnostics is not null);
                throwIfNullMethod = module.EnsureThrowIfNullFunctionExists(diagnosticSyntax, factory, diagnostics);
            }

            var call = factory.Call(
                receiver: null,
                throwIfNullMethod,
                arg0: factory.Convert(factory.SpecialType(SpecialType.System_Object), factory.Parameter(parameter)),
                arg1: factory.StringLiteral(parameter.Name));
            return factory.HiddenSequencePoint(factory.ExpressionStatement(call));
        }

        private static BoundStatement ConstructDirectNullCheck(ParameterSymbol parameter, SyntheticBoundNodeFactory factory)
        {
            BoundExpression paramIsNullCondition;
            var loweredLeft = factory.Parameter(parameter);

            if (loweredLeft.Type.IsNullableType())
            {
                paramIsNullCondition = factory.Not(factory.MakeNullableHasValue(loweredLeft.Syntax, loweredLeft));
            }
            else
            {
                // Examples of how we might get here:
                // int*
                // delegate*<...>
                // T where T : int? (via some indirection)
                Debug.Assert(parameter.Type.IsPointerOrFunctionPointer()
                    || (parameter.Type.IsNullableTypeOrTypeParameter() && !parameter.Type.IsNullableType()));

                paramIsNullCondition = factory.MakeNullCheck(loweredLeft.Syntax, loweredLeft, BinaryOperatorKind.Equal);
            }

            var argumentName = ImmutableArray.Create<BoundExpression>(factory.StringLiteral(parameter.Name));
            BoundObjectCreationExpression ex = factory.New(factory.WellKnownMethod(WellKnownMember.System_ArgumentNullException__ctorString), argumentName);
            BoundThrowStatement throwArgNullStatement = factory.Throw(ex);

            return factory.HiddenSequencePoint(factory.If(paramIsNullCondition, throwArgNullStatement));
        }
    }
}
