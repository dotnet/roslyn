// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Text.Projection;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IHostDependentFormattingRuleFactoryService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioFormattingRuleFactoryServiceFactory : IWorkspaceServiceFactory
    {
        public VisualStudioFormattingRuleFactoryServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Factory();
        }

        private sealed class Factory : IHostDependentFormattingRuleFactoryService
        {
            private readonly IFormattingRule _noopRule = new NoOpFormattingRule();

            public bool ShouldUseBaseIndentation(Document document)
            {
                return IsContainedDocument(document);
            }

            public bool ShouldNotFormatOrCommitOnPaste(Document document)
            {
                return IsContainedDocument(document);
            }

            private bool IsContainedDocument(Document document)
            {
                var visualStudioWorkspace = document.Project.Solution.Workspace as VisualStudioWorkspaceImpl;
                if (visualStudioWorkspace == null)
                {
                    return false;
                }

                var containedDocument = visualStudioWorkspace.GetHostDocument(document.Id);
                return containedDocument is ContainedDocument;
            }

            public IFormattingRule CreateRule(Document document, int position)
            {
                var visualStudioWorkspace = document.Project.Solution.Workspace as VisualStudioWorkspaceImpl;
                if (visualStudioWorkspace == null)
                {
                    return _noopRule;
                }

                var containedDocument = visualStudioWorkspace.GetHostDocument(document.Id) as ContainedDocument;
                if (containedDocument == null)
                {
                    return _noopRule;
                }

                var textContainer = document.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None).Container;
                var buffer = textContainer.TryGetTextBuffer() as IProjectionBuffer;
                if (buffer == null)
                {
                    return _noopRule;
                }

                using (var pooledObject = SharedPools.Default<List<TextSpan>>().GetPooledObject())
                {
                    var spans = pooledObject.Object;

                    var root = document.GetSyntaxRootAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                    var text = document.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);

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

                    throw new InvalidOperationException();
                }
            }

            public IEnumerable<TextChange> FilterFormattedChanges(Document document, TextSpan span, IList<TextChange> changes)
            {
                var visualStudioWorkspace = document.Project.Solution.Workspace as VisualStudioWorkspaceImpl;
                if (visualStudioWorkspace == null)
                {
                    return changes;
                }

                var containedDocument = visualStudioWorkspace.GetHostDocument(document.Id) as ContainedDocument;
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
    }
}
