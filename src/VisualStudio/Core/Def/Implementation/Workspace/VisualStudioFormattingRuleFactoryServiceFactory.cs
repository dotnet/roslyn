// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IHostDependentFormattingRuleFactoryService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioFormattingRuleFactoryServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        public VisualStudioFormattingRuleFactoryServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Factory();
        }

        private sealed class Factory : IHostDependentFormattingRuleFactoryService
        {
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
                return visualStudioWorkspace?.TryGetContainedDocument(document.Id) != null;
            }

            public AbstractFormattingRule CreateRule(Document document, int position)
            {
                try
                {
                    if (!(document.Project.Solution.Workspace is VisualStudioWorkspaceImpl visualStudioWorkspace))
                    {
                        return NoOpFormattingRule.Instance;
                    }

                    var containedDocument = visualStudioWorkspace.TryGetContainedDocument(document.Id);
                    if (containedDocument == null)
                    {
                        return NoOpFormattingRule.Instance;
                    }

                    return containedDocument.CreateFormattingRule(document, position);
                }
                catch (Exception e) when (FatalError.ReportWithoutCrash(e))
                {
                    return NoOpFormattingRule.Instance;
                }
            }

            public IEnumerable<TextChange> FilterFormattedChanges(Document document, TextSpan span, IList<TextChange> changes)
            {
                try
                {
                    if (!(document.Project.Solution.Workspace is VisualStudioWorkspaceImpl visualStudioWorkspace))
                    {
                        return changes;
                    }

                    var containedDocument = visualStudioWorkspace.TryGetContainedDocument(document.Id);
                    if (containedDocument == null)
                    {
                        return changes;
                    }

                    return containedDocument.FilterFormattedChanges(span, changes);
                }
                catch (Exception e) when (FatalError.ReportWithoutCrash(e))
                {
                    return changes;
                }
            }
        }
    }
}
