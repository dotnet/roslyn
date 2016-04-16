// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    [ExportWorkspaceServiceFactory(typeof(IHostDependentFormattingRuleFactoryService), TestWorkspace.WorkspaceName), Shared]
    internal sealed class TestFormattingRuleFactoryServiceFactory : IWorkspaceServiceFactory
    {
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
            public TextSpan TextSpan = default(TextSpan);
            public bool UseBaseIndentation = false;

            public bool ShouldUseBaseIndentation(Document document)
            {
                return UseBaseIndentation;
            }

            public IFormattingRule CreateRule(Document document, int position)
            {
                if (BaseIndentation == 0)
                {
                    return new NoOpFormattingRule();
                }

                var root = document.GetSyntaxRootAsync().Result;
                return new BaseIndentationFormattingRule(root, TextSpan, BaseIndentation + 4);
            }

            public IEnumerable<TextChange> FilterFormattedChanges(Document document, TextSpan span, IList<TextChange> changes)
            {
                return changes;
            }

            public bool ShouldNotFormatOrCommitOnPaste(Document document)
            {
                return UseBaseIndentation;
            }
        }
    }
}
