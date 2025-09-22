// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;

internal sealed partial class CSharpRemoveUnnecessaryNullableWarningSuppressionsDiagnosticAnalyzer
{
    private readonly struct Analyzer(
        SemanticModel semanticModel,
        PostfixUnaryExpressionSyntax node,
        CancellationToken cancellationToken)
    {
        private static bool ContainsErrorOrWarning(IEnumerable<Diagnostic> diagnostics)
            => diagnostics.Any(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning);

        public bool CanRemove()
        {
            var syntaxTree = semanticModel.SyntaxTree;
            var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);

            if (ContainsErrorOrWarning(node.GetDiagnostics()))
                return false;

            var semanticDiagnostics = semanticModel.GetDiagnostics(node.Span, cancellationToken);
            if (ContainsErrorOrWarning(semanticDiagnostics))
                return false;

            var updatedSyntaxTree = syntaxTree.WithRootAndOptions(
                root.ReplaceNode(node, node.Operand.WithAdditionalAnnotations(s_annotation)),
                syntaxTree.Options);
            var updatedNode = updatedSyntaxTree.GetRoot(cancellationToken).GetAnnotatedNodes(s_annotation).Single();

            var updatedCompilation = semanticModel.Compilation.ReplaceSyntaxTree(syntaxTree, updatedSyntaxTree);
            var updatedSemanticModel = updatedCompilation.GetSemanticModel(updatedSyntaxTree);
            var updatedDiagnostics = updatedSemanticModel.GetDiagnostics(updatedNode.Span, cancellationToken);

            if (ContainsErrorOrWarning(updatedDiagnostics))
                return false;

            return true;
        }
    }
}
