// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveUnnecessaryCast
{
    [ExportDiagnosticProvider(PredefinedDiagnosticProviderNames.RemoveUnnecessaryCast, LanguageNames.CSharp)]
    internal sealed class RemoveUnnecessaryCastDiagnosticProvider : ScopedDiagnosticProvider
    {
        internal const string DiagnosticId = "RemoveUnnecessaryCast";
        internal static readonly DiagnosticDescriptor DiagnosticMD = new DiagnosticDescriptor(DiagnosticId,
                                                                                              DiagnosticKind.Unnecessary,
                                                                                              CSharpFeaturesResources.RemoveUnnecessaryCast,
                                                                                              CSharpFeaturesResources.CastIsRedundant,
                                                                                              "Internal",
                                                                                              DiagnosticSeverity.None);

        public override IEnumerable<DiagnosticDescriptor> GetSupportedDiagnostics()
        {
            return SpecializedCollections.SingletonEnumerable(DiagnosticMD);
        }

        protected override async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            if (!document.IsOpen())
            {
                return null;
            }

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(span);

            List<Diagnostic> result = null;

            if (node.IsKind(SyntaxKind.CompilationUnit))
            {
                var compilationUnit = (CompilationUnitSyntax)node;
                result = ProcessNodes(model, compilationUnit.AttributeLists, result, cancellationToken);
                result = ProcessNodes(model, compilationUnit.Members, result, cancellationToken);
            }
            else
            {
                result = ProcessNode(model, node, result, cancellationToken);
            }

            return result;
        }

        private List<Diagnostic> ProcessNodes<T>(
            SemanticModel model, IEnumerable<T> nodes, List<Diagnostic> result, CancellationToken cancellationToken) where T : SyntaxNode
        {
            foreach (var node in nodes)
            {
                result = ProcessNode(model, node, result, cancellationToken);
            }

            return result;
        }

        private List<Diagnostic> ProcessNode(SemanticModel model, SyntaxNode node, List<Diagnostic> result, CancellationToken cancellationToken)
        {
            foreach (var cast in node.DescendantNodesAndSelf().OfType<CastExpressionSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                Diagnostic diagnostic;
                if (TryRemoveCastExpression(model, cast, out diagnostic, cancellationToken))
                {
                    result = result ?? new List<Diagnostic>();
                    result.Add(diagnostic);
                }
            }

            return result;
        }

        private bool TryRemoveCastExpression(
            SemanticModel model, CastExpressionSyntax node, out Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            diagnostic = default(Diagnostic);

            if (!node.IsUnnecessaryCast(model, cancellationToken))
            {
                return false;
            }

            var tree = model.SyntaxTree;
            var span = TextSpan.FromBounds(node.OpenParenToken.SpanStart, node.CloseParenToken.Span.End);
            if (tree.OverlapsHiddenPosition(span, cancellationToken))
            {
                return false;
            }

            diagnostic = Diagnostic.Create(DiagnosticMD, tree.GetLocation(span));
            return true;
        }
    }
}
