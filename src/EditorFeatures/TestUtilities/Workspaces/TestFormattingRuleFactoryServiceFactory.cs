// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

[ExportWorkspaceServiceFactory(typeof(IHostDependentFormattingRuleFactoryService), ServiceLayer.Test), Shared, PartNotDiscoverable]
internal sealed class TestFormattingRuleFactoryServiceFactory : IWorkspaceServiceFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TestFormattingRuleFactoryServiceFactory()
    {
    }

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
    {
        // return new factory per workspace
        return new Factory();
    }

    public sealed class Factory : IHostDependentFormattingRuleFactoryService
    {
        public int BaseIndentation = 0;
        public TextSpan TextSpan = default;
        public bool UseBaseIndentation = false;

        public bool ShouldUseBaseIndentation(DocumentId documentId)
            => UseBaseIndentation;

        public bool ShouldNotFormatOrCommitOnPaste(DocumentId documentId)
            => UseBaseIndentation;

        public AbstractFormattingRule CreateRule(ParsedDocument document, int position)
        {
            if (BaseIndentation == 0)
            {
                return NoOpFormattingRule.Instance;
            }

            return new BaseIndentationFormattingRule(document.Root, TextSpan, BaseIndentation + 4);
        }

        public IEnumerable<TextChange> FilterFormattedChanges(DocumentId document, TextSpan span, IList<TextChange> changes)
            => changes;
    }
}
