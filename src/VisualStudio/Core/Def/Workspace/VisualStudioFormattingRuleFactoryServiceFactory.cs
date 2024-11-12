// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

[ExportWorkspaceService(typeof(IHostDependentFormattingRuleFactoryService), ServiceLayer.Host), Shared]
internal sealed class VisualStudioFormattingRuleFactoryService : IHostDependentFormattingRuleFactoryService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioFormattingRuleFactoryService()
    {
    }
    public bool ShouldUseBaseIndentation(DocumentId documentId)
        => IsContainedDocument(documentId);

    public bool ShouldNotFormatOrCommitOnPaste(DocumentId documentId)
        => IsContainedDocument(documentId);

    private static bool IsContainedDocument(DocumentId documentId)
        => ContainedDocument.TryGetContainedDocument(documentId) != null;

    public AbstractFormattingRule CreateRule(ParsedDocument document, int position)
    {
        var containedDocument = ContainedDocument.TryGetContainedDocument(document.Id);
        if (containedDocument == null)
        {
            return NoOpFormattingRule.Instance;
        }

        var textContainer = document.Text.Container;
        if (textContainer.TryGetTextBuffer() is not IProjectionBuffer)
        {
            return NoOpFormattingRule.Instance;
        }

        using var pooledObject = SharedPools.Default<List<TextSpan>>().GetPooledObject();
        var spans = pooledObject.Object;

        var root = document.Root;
        var text = document.Text;

        spans.AddRange(containedDocument.GetEditorVisibleSpans());

        for (var i = 0; i < spans.Count; i++)
        {
            var visibleSpan = spans[i];
            if (visibleSpan.IntersectsWith(position) || visibleSpan.End == position)
            {
                return containedDocument.GetBaseIndentationRule(root, text, spans, i);
            }
        }

        // in razor (especially in @helper tag), it is possible for us to be asked for next line of visible span
        var line = text.Lines.GetLineFromPosition(position);
        if (line.LineNumber > 0)
        {
            line = text.Lines[line.LineNumber - 1];

            // find one that intersects with previous line
            for (var i = 0; i < spans.Count; i++)
            {
                var visibleSpan = spans[i];
                if (visibleSpan.IntersectsWith(line.Span))
                {
                    return containedDocument.GetBaseIndentationRule(root, text, spans, i);
                }
            }
        }

        FatalError.ReportAndCatch(
            new InvalidOperationException($"Can't find an intersection. Visible spans count: {spans.Count}"));

        return NoOpFormattingRule.Instance;
    }

    public IEnumerable<TextChange> FilterFormattedChanges(DocumentId documentId, TextSpan span, IList<TextChange> changes)
    {
        var containedDocument = ContainedDocument.TryGetContainedDocument(documentId);
        if (containedDocument == null)
        {
            return changes;
        }

        // in case of a Venus, when format document command is issued, Venus will call format API with each script block spans.
        // in that case, we need to make sure formatter doesn't overstep other script blocks content. in actual format selection case,
        // we need to format more than given selection otherwise, we will not adjust indentation of first token of the given selection.
        foreach (var visibleSpan in containedDocument.GetEditorVisibleSpans())
        {
            if (visibleSpan != span)
            {
                continue;
            }

            return changes.Where(c => span.IntersectsWith(c.Span));
        }

        return changes;
    }
}
