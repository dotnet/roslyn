// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Performance;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Performance
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpRemoveEmptyFinalizers : RemoveEmptyFinalizers<SyntaxKind>
    {
        protected override bool IsEmptyFinalizer(SyntaxNode node, SemanticModel model)
        {
            foreach (var exp in node.DescendantNodes().OfType<StatementSyntax>().Where(n => !n.IsKind(SyntaxKind.Block) && !n.IsKind(SyntaxKind.EmptyStatement)))
            {
                // NOTE: FxCop only checks if there is any method call within a given destructor to decide an empty finalizer.
                // Here in order to minimize false negatives, we conservatively treat it as non-empty finalizer if its body contains any statements.
                // But, still conditional methods like Debug.Fail() will be considered as being empty as FxCop currently does.

                var method = exp as ExpressionStatementSyntax;
                if (method != null && HasConditionalAttribute(method.Expression, model))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private bool HasConditionalAttribute(SyntaxNode root, SemanticModel model)
        {
            var node = root as InvocationExpressionSyntax;
            if (node != null)
            {
                var exp = node.Expression as MemberAccessExpressionSyntax;
                if (exp != null)
                {
                    var symbol = model.GetSymbolInfo(exp.Name).Symbol;
                    if (symbol != null && symbol.GetAttributes().Any(n => n.AttributeClass.Equals(WellKnownTypes.ConditionalAttribute(model.Compilation))))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
