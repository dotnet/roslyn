// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Snippets;

internal abstract class AbstractSnippetExpansionLanguageHelper
    : ISnippetExpansionLanguageHelper
{
    public abstract Guid LanguageServiceGuid { get; }
    public abstract string FallbackDefaultLiteral { get; }

    public abstract Document AddImports(Document document, AddImportPlacementOptions addImportOptions, SyntaxFormattingOptions formattingOptions, int position, XElement snippetNode, CancellationToken cancellationToken);
    public abstract ITrackingSpan? InsertEmptyCommentAndGetEndPositionTrackingSpan(IVsExpansionSession expansionSession, ITextView textView, ITextBuffer subjectBuffer);

    public bool TryGetSubjectBufferSpan(ITextView textView, ITextBuffer subjectBuffer, VsTextSpan surfaceBufferTextSpan, out SnapshotSpan subjectBufferSpan)
    {
        var snapshotSpan = textView.TextSnapshot.GetSpan(surfaceBufferTextSpan);
        var subjectBufferSpanCollection = textView.BufferGraph.MapDownToBuffer(snapshotSpan, SpanTrackingMode.EdgeExclusive, subjectBuffer);

        // Bail if a snippet span does not map down to exactly one subject buffer span.
        if (subjectBufferSpanCollection.Count == 1)
        {
            subjectBufferSpan = subjectBufferSpanCollection.Single();
            return true;
        }

        subjectBufferSpan = default;
        return false;
    }

    protected static bool TryAddImportsToContainedDocument(Document document, IEnumerable<string> memberImportsNamespaces)
    {
        var containedDocument = ContainedDocument.TryGetContainedDocument(document.Id);
        if (containedDocument == null)
        {
            return false;
        }

        if (containedDocument.ContainedLanguageHost is IVsContainedLanguageHostInternal containedLanguageHost)
        {
            foreach (var importClause in memberImportsNamespaces)
            {
                if (containedLanguageHost.InsertImportsDirective(importClause) != VSConstants.S_OK)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
