// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.SimplifyLinqExpression;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpSimplifyLinqExpressionDiagnosticAnalyzer : AbstractSimplifyLinqExpressionDiagnosticAnalyzer
    {
        protected override IInvocationOperation? TryGetPreviousInvocationInChain(ImmutableArray<IArgumentOperation> arguments)
        {
            if (arguments.Length != 1)
            {
                return null;
            }

            var argument = arguments.Single();
            if (argument.Syntax is not InvocationExpressionSyntax invocationExpression)
            {
                return null;
            }

            var model = argument.SemanticModel;
            return model?.GetOperation(invocationExpression, default) as IInvocationOperation;
        }

        protected override Location? TryGetArgumentListLocation(ImmutableArray<IArgumentOperation> arguments)
        {
            var list = new List<ArgumentListSyntax>();
            foreach (var argument in arguments)
            {
                if (argument.Syntax is ArgumentSyntax argumentNode &&
                    argumentNode.Parent is ArgumentListSyntax argumentList)
                {
                    list.Add(argumentList);
                }
            }

            // verify that all these arguments come from the same sytax list
            if (!list.Any() ||
                !list.TrueForAll(argList => argList == list[0]))
            {
                return null;
            }

            return list[0].GetLocation();
        }
    }
}
