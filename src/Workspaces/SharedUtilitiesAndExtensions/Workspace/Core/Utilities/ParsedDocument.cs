// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Represents a <see cref="Document"/> content that has been parsed.
/// </summary>
/// <remarks>
/// Used to front-load <see cref="SyntaxTree"/> parsing and <see cref="SourceText"/> retrieval to a caller that has knowledge of whether or not these operations
/// should be performed synchronously or asynchronously. The <see cref="ParsedDocument"/> is then passed to a feature whose implementation is entirely synchronous.
/// In general, any feature API that accepts <see cref="ParsedDocument"/> should be synchronous and not access <see cref="Document"/> or <see cref="Solution"/> snapshots.
/// In exceptional cases such API may be asynchronous as long as it completes synchronously in most common cases and async completion is rare. It is still desirable to improve the design
/// of such feature to either not be invoked on a UI thread or be entirely synchronous.
/// </remarks>
internal readonly record struct ParsedDocument(DocumentId Id, SourceText Text, SyntaxNode Root, HostLanguageServices HostLanguageServices)
{
    public SyntaxTree SyntaxTree => Root.SyntaxTree;

    public LanguageServices LanguageServices => HostLanguageServices.LanguageServices;
    public SolutionServices SolutionServices => LanguageServices.SolutionServices;

    public static async ValueTask<ParsedDocument> CreateAsync(Document document, CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        return new ParsedDocument(document.Id, text, root, document.Project.GetExtendedLanguageServices());
    }

#if !CODE_STYLE
    public static ParsedDocument CreateSynchronously(Document document, CancellationToken cancellationToken)
    {
        var text = document.GetTextSynchronously(cancellationToken);
        var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
        return new ParsedDocument(document.Id, text, root, document.Project.GetExtendedLanguageServices());
    }
#endif

    public ParsedDocument WithChangedText(SourceText text, CancellationToken cancellationToken)
    {
        var root = SyntaxTree.WithChangedText(text).GetRoot(cancellationToken);
        return new ParsedDocument(Id, text, root, HostLanguageServices);
    }

    public ParsedDocument WithChangedRoot(SyntaxNode root, CancellationToken cancellationToken)
    {
        var text = root.SyntaxTree.GetText(cancellationToken);
        return new ParsedDocument(Id, text, root, HostLanguageServices);
    }
}
