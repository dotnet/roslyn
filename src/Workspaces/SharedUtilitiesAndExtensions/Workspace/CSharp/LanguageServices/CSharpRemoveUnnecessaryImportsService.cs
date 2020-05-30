﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports
{
    [ExportLanguageService(typeof(IRemoveUnnecessaryImportsService), LanguageNames.CSharp), Shared]
    internal partial class CSharpRemoveUnnecessaryImportsService :
        AbstractRemoveUnnecessaryImportsService<UsingDirectiveSyntax>
    {
        public static readonly CSharpRemoveUnnecessaryImportsService Instance = new CSharpRemoveUnnecessaryImportsService();

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Incorrectly used in production code: https://github.com/dotnet/roslyn/issues/42839")]
        public CSharpRemoveUnnecessaryImportsService()
        {
        }

        protected override IUnnecessaryImportsProvider UnnecessaryImportsProvider
            => CSharpUnnecessaryImportsProvider.Instance;

        public override async Task<Document> RemoveUnnecessaryImportsAsync(
            Document document,
            Func<SyntaxNode, bool> predicate,
            CancellationToken cancellationToken)
        {
            predicate ??= Functions<SyntaxNode>.True;
            using (Logger.LogBlock(FunctionId.Refactoring_RemoveUnnecessaryImports_CSharp, cancellationToken))
            {
                var unnecessaryImports = await GetCommonUnnecessaryImportsOfAllContextAsync(
                    document, predicate, cancellationToken).ConfigureAwait(false);
                if (unnecessaryImports == null || unnecessaryImports.Any(import => import.OverlapsHiddenPosition(cancellationToken)))
                {
                    return document;
                }

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var oldRoot = (CompilationUnitSyntax)root;
                var newRoot = (CompilationUnitSyntax)new Rewriter(document, unnecessaryImports, cancellationToken).Visit(oldRoot);

                cancellationToken.ThrowIfCancellationRequested();
                return document.WithSyntaxRoot(await FormatResultAsync(document, newRoot, cancellationToken).ConfigureAwait(false));
            }
        }

        private async Task<SyntaxNode> FormatResultAsync(Document document, CompilationUnitSyntax newRoot, CancellationToken cancellationToken)
        {
            var spans = new List<TextSpan>();
            AddFormattingSpans(newRoot, spans, cancellationToken);
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return Formatter.Format(newRoot, spans, document.Project.Solution.Workspace, options, cancellationToken: cancellationToken);
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

        private static int GetEndPosition(SyntaxNode container, SyntaxList<MemberDeclarationSyntax> list)
            => list.Count > 0 ? list[0].SpanStart : container.Span.End;
    }
}
