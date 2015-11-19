// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal abstract class AbstractDocumentDifferenceService : IDocumentDifferenceService
    {
        public async Task<DocumentDifferenceResult> GetDifferenceAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken)
        {
            try
            {
                var syntaxFactsService = newDocument.Project.LanguageServices.GetService<ISyntaxFactsService>();
                if (syntaxFactsService == null)
                {
                    // somehow, we can't get the service. without it, there is nothing we can do.
                    return new DocumentDifferenceResult(InvocationReasons.DocumentChanged);
                }

                // this is based on the implementation detail where opened documents use strong references
                // to tree and text rather than recoverable versions.
                SourceText oldText;
                SourceText newText;
                if (!oldDocument.TryGetText(out oldText) ||
                    !newDocument.TryGetText(out newText))
                {
                    // no cheap way to determine top level changes. assumes top level has changed
                    return new DocumentDifferenceResult(InvocationReasons.DocumentChanged);
                }

                // quick check whether two tree versions are same
                VersionStamp oldVersion;
                VersionStamp newVersion;
                if (oldDocument.TryGetSyntaxVersion(out oldVersion) &&
                    newDocument.TryGetSyntaxVersion(out newVersion) &&
                    oldVersion.Equals(newVersion))
                {
                    // nothing has changed. don't do anything.
                    // this could happen if a document is opened/closed without any buffer change
                    return null;
                }

                var range = newText.GetEncompassingTextChangeRange(oldText);
                if (range == default(TextChangeRange))
                {
                    // nothing has changed. don't do anything
                    return null;
                }

                var incrementalParsingCandidate = range.NewLength != newText.Length;

                // see whether we can get it without explicit parsing
                SyntaxNode oldRoot;
                SyntaxNode newRoot;
                if (!oldDocument.TryGetSyntaxRoot(out oldRoot) ||
                    !newDocument.TryGetSyntaxRoot(out newRoot))
                {
                    if (!incrementalParsingCandidate)
                    {
                        // no cheap way to determine top level changes. assumes top level has changed
                        return new DocumentDifferenceResult(InvocationReasons.DocumentChanged);
                    }

                    // explicitly parse them
                    oldRoot = await oldDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                    newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                }

                // at this point, we must have these version already calculated
                VersionStamp oldTopLevelChangeVersion;
                VersionStamp newTopLevelChangeVersion;
                if (!oldDocument.TryGetTopLevelChangeTextVersion(out oldTopLevelChangeVersion) ||
                    !newDocument.TryGetTopLevelChangeTextVersion(out newTopLevelChangeVersion))
                {
                    throw ExceptionUtilities.Unreachable;
                }

                // quicker common case
                if (incrementalParsingCandidate)
                {
                    if (oldTopLevelChangeVersion.Equals(newTopLevelChangeVersion))
                    {
                        return new DocumentDifferenceResult(InvocationReasons.SyntaxChanged, GetChangedMember(syntaxFactsService, oldRoot, newRoot, range));
                    }

                    return new DocumentDifferenceResult(InvocationReasons.DocumentChanged, GetBestGuessChangedMember(syntaxFactsService, oldRoot, newRoot, range));
                }

                if (oldTopLevelChangeVersion.Equals(newTopLevelChangeVersion))
                {
                    return new DocumentDifferenceResult(InvocationReasons.SyntaxChanged);
                }

                return new DocumentDifferenceResult(InvocationReasons.DocumentChanged);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static SyntaxNode GetChangedMember(
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
            if (!syntaxFactsService.ContainsInMemberBody(oldMember, range.Span))
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

        private static SyntaxNode GetBestGuessChangedMember(
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
}
