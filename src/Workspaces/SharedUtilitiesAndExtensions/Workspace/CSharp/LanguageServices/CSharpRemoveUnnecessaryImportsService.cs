// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports;

[ExportLanguageService(typeof(IRemoveUnnecessaryImportsService), LanguageNames.CSharp), Shared]
internal partial class CSharpRemoveUnnecessaryImportsService :
    AbstractRemoveUnnecessaryImportsService<UsingDirectiveSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpRemoveUnnecessaryImportsService()
    {
    }

    private static ISyntaxFormatting SyntaxFormatting
        => CSharpSyntaxFormatting.Instance;

    protected override IUnnecessaryImportsProvider<UsingDirectiveSyntax> UnnecessaryImportsProvider
        => CSharpUnnecessaryImportsProvider.Instance;

    public override async Task<Document> RemoveUnnecessaryImportsAsync(
        Document document,
        Func<SyntaxNode, bool>? predicate,
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

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var oldRoot = (CompilationUnitSyntax)root;
            var newRoot = (CompilationUnitSyntax)new Rewriter(unnecessaryImports, cancellationToken).Visit(oldRoot);

            cancellationToken.ThrowIfCancellationRequested();

            var spansToFormat = new List<TextSpan>();
            AddFormattingSpans(newRoot, spansToFormat, cancellationToken);

            var options = await document.GetCodeFixOptionsAsync(cancellationToken).ConfigureAwait(false);
            var formattingOptions = options.GetFormattingOptions(SyntaxFormatting);
            var formattedRoot = SyntaxFormatting.GetFormattingResult(newRoot, spansToFormat, formattingOptions, rules: default, cancellationToken).GetFormattedRoot(cancellationToken);

            return document.WithSyntaxRoot(formattedRoot);
        }
    }

    private static void AddFormattingSpans(
        CompilationUnitSyntax compilationUnit,
        List<TextSpan> spans,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        spans.Add(TextSpan.FromBounds(0, GetEndPosition(compilationUnit, compilationUnit.Members)));

        foreach (var @namespace in compilationUnit.Members.OfType<BaseNamespaceDeclarationSyntax>())
            AddFormattingSpans(@namespace, spans, cancellationToken);
    }

    private static void AddFormattingSpans(
        BaseNamespaceDeclarationSyntax namespaceMember,
        List<TextSpan> spans,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        spans.Add(TextSpan.FromBounds(namespaceMember.SpanStart, GetEndPosition(namespaceMember, namespaceMember.Members)));

        foreach (var @namespace in namespaceMember.Members.OfType<BaseNamespaceDeclarationSyntax>())
            AddFormattingSpans(@namespace, spans, cancellationToken);
    }

    private static int GetEndPosition(SyntaxNode container, SyntaxList<MemberDeclarationSyntax> list)
        => list.Count > 0 ? list[0].SpanStart : container.Span.End;
}
