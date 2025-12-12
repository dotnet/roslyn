// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports;

[ExportLanguageService(typeof(IRemoveUnnecessaryImportsService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpRemoveUnnecessaryImportsService() :
    AbstractRemoveUnnecessaryImportsService<UsingDirectiveSyntax>
{
    private static readonly SyntaxAnnotation s_annotation = new();

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

            var newRoot = new Rewriter(unnecessaryImports, cancellationToken).Visit(root);

            cancellationToken.ThrowIfCancellationRequested();

            using var _ = ArrayBuilder<TextSpan>.GetInstance(out var spansToFormat);
            foreach (var node in newRoot.GetAnnotatedNodes(s_annotation))
            {
                if (node is CompilationUnitSyntax { Members: [var firstMemberA, ..] })
                {
                    spansToFormat.Add(TextSpan.FromBounds(0, firstMemberA.SpanStart));
                }
                else if (node is BaseNamespaceDeclarationSyntax { Members: [var firstMemberB, ..] } baseNamespace)
                {
                    spansToFormat.Add(TextSpan.FromBounds(baseNamespace.Name.Span.End, firstMemberB.SpanStart));
                }
            }

            var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
            var formattedRoot = SyntaxFormatting.GetFormattingResult(newRoot, spansToFormat, formattingOptions, rules: default, cancellationToken).GetFormattedRoot(cancellationToken);

            return document.WithSyntaxRoot(formattedRoot);
        }
    }
}
