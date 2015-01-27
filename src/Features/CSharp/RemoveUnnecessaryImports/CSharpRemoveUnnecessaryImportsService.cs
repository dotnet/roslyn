// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports
{
    [ExportLanguageService(typeof(IRemoveUnnecessaryImportsService), LanguageNames.CSharp), Shared]
    internal partial class CSharpRemoveUnnecessaryImportsService : IRemoveUnnecessaryImportsService
    {
        public static IEnumerable<SyntaxNode> GetUnnecessaryImports(SemanticModel semanticModel, SyntaxNode root, CancellationToken cancellationToken)
        {
            var diagnostics = semanticModel.GetDiagnostics(cancellationToken: cancellationToken);
            if (!diagnostics.Any())
            {
                return null;
            }

            var unnecessaryImports = new HashSet<UsingDirectiveSyntax>();

            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Id == "CS8019")
                {
                    var node = root.FindNode(diagnostic.Location.SourceSpan) as UsingDirectiveSyntax;

                    if (node != null)
                    {
                        unnecessaryImports.Add(node);
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested || !unnecessaryImports.Any())
            {
                return null;
            }

            return unnecessaryImports;
        }

        public Document RemoveUnnecessaryImports(Document document, SemanticModel model, SyntaxNode root, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_RemoveUnnecessaryImports_CSharp, cancellationToken))
            {
                var unnecessaryImports = GetUnnecessaryImports(model, root, cancellationToken) as ISet<UsingDirectiveSyntax>;
                if (unnecessaryImports == null)
                {
                    return document;
                }

                var oldRoot = (CompilationUnitSyntax)root;
                if (unnecessaryImports.Any(import => oldRoot.OverlapsHiddenPosition(cancellationToken)))
                {
                    return document;
                }

                var newRoot = (CompilationUnitSyntax)new Rewriter(unnecessaryImports, cancellationToken).Visit(oldRoot);
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                return document.WithSyntaxRoot(FormatResult(document, newRoot, cancellationToken));
            }
        }

        private SyntaxNode FormatResult(Document document, CompilationUnitSyntax newRoot, CancellationToken cancellationToken)
        {
            var spans = new List<TextSpan>();
            AddFormattingSpans(newRoot, spans, cancellationToken);
            return Formatter.Format(newRoot, spans, document.Project.Solution.Workspace, document.Project.Solution.Workspace.Options, cancellationToken: cancellationToken);
        }

        private void AddFormattingSpans(
            CompilationUnitSyntax compilationUnit,
            List<TextSpan> spans,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            spans.Add(TextSpan.FromBounds(0, GetEndPosition(compilationUnit, compilationUnit.Members)));

            foreach (var @namespace in compilationUnit.Members.OfType<NamespaceDeclarationSyntax>())
            {
                AddFormattingSpans(@namespace, spans, cancellationToken);
            }
        }

        private void AddFormattingSpans(
            NamespaceDeclarationSyntax namespaceMember,
            List<TextSpan> spans,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            spans.Add(TextSpan.FromBounds(namespaceMember.SpanStart, GetEndPosition(namespaceMember, namespaceMember.Members)));

            foreach (var @namespace in namespaceMember.Members.OfType<NamespaceDeclarationSyntax>())
            {
                AddFormattingSpans(@namespace, spans, cancellationToken);
            }
        }

        private int GetEndPosition(SyntaxNode container, SyntaxList<MemberDeclarationSyntax> list)
        {
            return list.Count > 0 ? list[0].SpanStart : container.Span.End;
        }
    }
}
