// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SolutionCrawler;

internal abstract class AbstractDocumentDifferenceService : IDocumentDifferenceService
{
    protected abstract bool IsContainedInMemberBody(SyntaxNode oldMember, TextSpan span);

    public async Task<SyntaxNode?> GetChangedMemberAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken)
    {
        try
        {
            var syntaxFactsService = newDocument.Project.Services.GetService<ISyntaxFactsService>();
            if (syntaxFactsService == null)
            {
                // somehow, we can't get the service. without it, there is nothing we can do.
                return null;
            }
            // this is based on the implementation detail where opened documents use strong references
            // to tree and text rather than recoverable versions.
            if (!oldDocument.TryGetText(out var oldText) ||
                !newDocument.TryGetText(out var newText))
            {
                // no cheap way to determine top level changes. assumes top level has changed
                return null;
            }
            // quick check whether two tree versions are same
            if (oldDocument.TryGetSyntaxVersion(out var oldVersion) &&
                newDocument.TryGetSyntaxVersion(out var newVersion) &&
                oldVersion.Equals(newVersion))
            {
                // nothing has changed. don't do anything.
                // this could happen if a document is opened/closed without any buffer change
                return null;
            }

            var range = newText.GetEncompassingTextChangeRange(oldText);
            if (range == default)
            {
                // nothing has changed. don't do anything
                return null;
            }

            var incrementalParsingCandidate = range.NewLength != newText.Length;
            // see whether we can get it without explicit parsing
            if (!oldDocument.TryGetSyntaxRoot(out var oldRoot) ||
                !newDocument.TryGetSyntaxRoot(out var newRoot))
            {
                if (!incrementalParsingCandidate)
                {
                    // no cheap way to determine top level changes. assumes top level has changed
                    return null;
                }

                // explicitly parse them
                oldRoot = await oldDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                Contract.ThrowIfNull(oldRoot);
                Contract.ThrowIfNull(newRoot);
            }

            // at this point, we must have these version already calculated
            if (!oldDocument.TryGetTopLevelChangeTextVersion(out var oldTopLevelChangeVersion) ||
                !newDocument.TryGetTopLevelChangeTextVersion(out var newTopLevelChangeVersion))
            {
                throw ExceptionUtilities.Unreachable();
            }

            // quicker common case
            if (incrementalParsingCandidate)
            {
                if (oldTopLevelChangeVersion.Equals(newTopLevelChangeVersion))
                {
                    return GetChangedMember(syntaxFactsService, oldRoot, newRoot, range);
                }

                return GetBestGuessChangedMember(syntaxFactsService, oldRoot, newRoot, range);
            }

            if (oldTopLevelChangeVersion.Equals(newTopLevelChangeVersion))
            {
                return null;
            }

            return null;
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    private SyntaxNode? GetChangedMember(
        ISyntaxFactsService syntaxFactsService, SyntaxNode oldRoot, SyntaxNode newRoot, TextChangeRange range)
    {
        // if either old or new tree contains skipped text, re-analyze whole document
        if (oldRoot.ContainsSkippedText || newRoot.ContainsSkippedText)
        {
            return null;
        }

        var oldMember = syntaxFactsService.GetContainingMemberDeclaration(oldRoot, range.Span.Start);
        var newMember = syntaxFactsService.GetContainingMemberDeclaration(newRoot, range.Span.Start);

        // reached the top (compilation unit)
        if (oldMember == null || newMember == null)
        {
            return null;
        }

        // member doesn't contain the change
        if (!IsContainedInMemberBody(oldMember, range.Span))
        {
            return null;
        }

        // member signature has changed
        if (!oldMember.IsEquivalentTo(newMember, topLevel: true))
        {
            return null;
        }

        // looks like inside of the body has changed
        return newMember;
    }

    private static SyntaxNode? GetBestGuessChangedMember(
        ISyntaxFactsService syntaxFactsService, SyntaxNode oldRoot, SyntaxNode newRoot, TextChangeRange range)
    {
        // if either old or new tree contains skipped text, re-analyze whole document
        if (oldRoot.ContainsSkippedText || newRoot.ContainsSkippedText)
        {
            return null;
        }

        // there was top level changes, so we can't use equivalent to see whether two members are same.
        // so, we use some simple text based heuristic to find a member that has changed.
        //
        // if we have a differ that do diff on member level or a way to track member between incremental parsing, then
        // that would be preferable. but currently we don't have such thing.

        // get top level elements at the position where change has happened
        var oldMember = syntaxFactsService.GetContainingMemberDeclaration(oldRoot, range.Span.Start);
        var newMember = syntaxFactsService.GetContainingMemberDeclaration(newRoot, range.Span.Start);

        // reached the top (compilation unit)
        if (oldMember == null || newMember == null)
        {
            return null;
        }

        // if old member was empty, just use new member
        if (oldMember.Span.IsEmpty)
        {
            return newMember;
        }

        // looks like change doesn't belong to existing member
        if (!oldMember.Span.Contains(range.Span))
        {
            return null;
        }

        // change happened inside of the old member, check whether new member seems just delta of that change
        var lengthDelta = range.NewLength - range.Span.Length;

        return (oldMember.Span.Length + lengthDelta) == newMember.Span.Length ? newMember : null;
    }
}
