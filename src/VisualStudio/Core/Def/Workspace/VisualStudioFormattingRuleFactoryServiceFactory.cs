// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
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
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioFormattingRuleFactoryServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new Factory();

        private sealed class Factory : IHostDependentFormattingRuleFactoryService
        {
            public bool ShouldUseBaseIndentation(Document document)
                => IsContainedDocument(document);

            public bool ShouldNotFormatOrCommitOnPaste(Document document)
                => IsContainedDocument(document);

            private static bool IsContainedDocument(Document document)
            {
                var visualStudioWorkspace = document.Project.Solution.Workspace as VisualStudioWorkspaceImpl;
                return visualStudioWorkspace?.TryGetContainedDocument(document.Id) != null;
            }

            public AbstractFormattingRule CreateRule(Document document, int position)
            {
                if (document.Project.Solution.Workspace is not VisualStudioWorkspaceImpl visualStudioWorkspace)
                {
                    return NoOpFormattingRule.Instance;
                }

                var containedDocument = visualStudioWorkspace.TryGetContainedDocument(document.Id);
                if (containedDocument == null)
                {
                    return NoOpFormattingRule.Instance;
                }

                var textContainer = document.GetTextSynchronously(CancellationToken.None).Container;
                if (textContainer.TryGetTextBuffer() is not IProjectionBuffer)
                {
                    return NoOpFormattingRule.Instance;
                }

                using var pooledObject = SharedPools.Default<List<TextSpan>>().GetPooledObject();
                var spans = pooledObject.Object;

                var root = document.GetSyntaxRootSynchronously(CancellationToken.None);
                var text = root.SyntaxTree.GetText(CancellationToken.None);

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

            public IEnumerable<TextChange> FilterFormattedChanges(Document document, TextSpan span, IList<TextChange> changes)
            {
                if (document.Project.Solution.Workspace is not VisualStudioWorkspaceImpl visualStudioWorkspace)
                {
                    return changes;
                }

                var containedDocument = visualStudioWorkspace.TryGetContainedDocument(document.Id);
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
