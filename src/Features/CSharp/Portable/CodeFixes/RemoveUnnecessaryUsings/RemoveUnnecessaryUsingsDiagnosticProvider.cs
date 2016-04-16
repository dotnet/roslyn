// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveUnusedUsings
{
    [ExportDiagnosticProvider(PredefinedDiagnosticProviderNames.RemoveUnnecessaryImports, LanguageNames.CSharp)]
    internal sealed class RemoveUnnecessaryUsingsDiagnosticProvider : DocumentDiagnosticProvider
    {
        internal const string DiagnosticClassificationId = "RemoveUnnecessaryUsingsClassification";
        internal const string DiagnosticFixableId = "RemoveUnnecessaryUsingsFixable";
        internal static readonly DiagnosticDescriptor DiagnosticClassificationMD = new DiagnosticDescriptor(DiagnosticClassificationId,
                                                                                                            DiagnosticKind.Unnecessary,
                                                                                                            CSharpFeaturesResources.RemoveUnnecessaryUsings,
                                                                                                            CSharpFeaturesResources.RemoveUnnecessaryUsings,
                                                                                                            "Internal",
                                                                                                            DiagnosticSeverity.None);
        internal static readonly DiagnosticDescriptor DiagnosticFixableMD = new DiagnosticDescriptor(DiagnosticFixableId,
                                                                                                     DiagnosticKind.Hidden,
                                                                                                     CSharpFeaturesResources.RemoveUnnecessaryUsings,
                                                                                                     CSharpFeaturesResources.RemoveUnnecessaryUsings,
                                                                                                     "Internal",
                                                                                                     DiagnosticSeverity.None);

        public override IEnumerable<DiagnosticDescriptor> GetSupportedDiagnostics()
        {
            return ImmutableArray.Create(DiagnosticClassificationMD, DiagnosticFixableMD);
        }

        protected override async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(Document document, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!document.IsOpen())
            {
                return null;
            }

            if (document.SourceCodeKind == SourceCodeKind.Interactive)
            {
                // It's common to type usings in a submission that are intended for a future
                // submission.  We do not want to offer to remove these.
                return null;
            }

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var service = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
            var unnecessaryUsings = service.GetUnnecessaryImports(document, model, model.SyntaxTree.GetRoot(cancellationToken), cancellationToken);
            if (unnecessaryUsings == null)
            {
                return null;
            }

            var contiguousSpans = unnecessaryUsings.GetContiguousSpans();
            return CreateClassificationDiagnostics(contiguousSpans, model, document, cancellationToken).Concat(
                   CreateFixableDiagnostics(unnecessaryUsings, model, document, cancellationToken));
        }

        // Create one diagnostic for each unnecessary span that will be classified as Unnecessary
        private IEnumerable<Diagnostic> CreateClassificationDiagnostics(IEnumerable<TextSpan> contiguousSpans, SemanticModel model, Document document, CancellationToken cancellationToken)
        {
            var tree = model.SyntaxTree;

            foreach (var span in contiguousSpans)
            {
                if (tree.OverlapsHiddenPosition(span, cancellationToken))
                {
                    continue;
                }

                yield return Diagnostic.Create(DiagnosticClassificationMD, tree.GetLocation(span));
            }
        }

        // Create one diagnostic for the entire span of the usings block that will provide the fix.
        private IEnumerable<Diagnostic> CreateFixableDiagnostics(IEnumerable<SyntaxNode> nodes, SemanticModel model, Document document, CancellationToken cancellationToken)
        {
            var nodesContainingUnnecessaryUsings = new HashSet<SyntaxNode>();
            var tree = model.SyntaxTree;
            foreach (var node in nodes)
            {
                var nodeContainingUnnecessaryUsings = node.GetAncestors().First(n => n is NamespaceDeclarationSyntax || n is CompilationUnitSyntax);
                if (!nodesContainingUnnecessaryUsings.Add(nodeContainingUnnecessaryUsings))
                {
                    continue;
                }

                var span = nodeContainingUnnecessaryUsings is NamespaceDeclarationSyntax
                    ? ((NamespaceDeclarationSyntax)nodeContainingUnnecessaryUsings).Usings.GetContainedSpan()
                    : ((CompilationUnitSyntax)nodeContainingUnnecessaryUsings).Usings.GetContainedSpan();

                yield return Diagnostic.Create(DiagnosticFixableMD, tree.GetLocation(span));
            }
        }
    }
}
