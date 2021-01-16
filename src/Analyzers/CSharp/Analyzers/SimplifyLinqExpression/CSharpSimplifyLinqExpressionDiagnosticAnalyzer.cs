// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SimplifyLinqExpression;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpSimplifyLinqExpressionDiagnosticAnalyzer : AbstractSimplifyLinqExpressionDiagnosticAnalyzer
    {
        protected override Location? TryGetArgumentListLocation(ImmutableArray<IArgumentOperation> arguments)
        {
            using var _ = ArrayBuilder<ArgumentListSyntax>.GetInstance(out var argumentLists);
            foreach (var argument in arguments)
            {
                if (argument.Syntax is ArgumentSyntax argumentNode &&
                    argumentNode.Parent is ArgumentListSyntax argumentList)
                {
                    argumentLists.Add(argumentList);
                }
            }

            // verify that all these arguments come from the same sytax list
            argumentLists.RemoveDuplicates();
            if (argumentLists.Count != 1)
            {
                return null;
            }

            return argumentLists[0].GetLocation();
        }

        protected override string? TryGetMethodName(IInvocationOperation invocation)
        {
            if (invocation.Syntax is InvocationExpressionSyntax invocationNode &&
                invocationNode.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                return memberAccess.Name.GetText().ToString();
            }

            return null;
        }

        protected override IInvocationOperation? TryGetNextInvocationInChain(IInvocationOperation invocation)
        {
            if (invocation.Parent is IArgumentOperation argument &&
                argument.Parent is IInvocationOperation nextInvocation)
            {
                return nextInvocation;
            }

            return null;
        }

        protected override INamedTypeSymbol? TryGetSymbolOfMemberAccess(IInvocationOperation invocation)
        {
            if (invocation.Syntax is not InvocationExpressionSyntax invocationNode ||
                invocationNode.Expression is not MemberAccessExpressionSyntax memberAccess ||
                memberAccess.Expression is null)
            {
                return null;
            }

            return invocation.SemanticModel.GetTypeInfo(memberAccess.Expression).Type as INamedTypeSymbol;
        }
    }
}
