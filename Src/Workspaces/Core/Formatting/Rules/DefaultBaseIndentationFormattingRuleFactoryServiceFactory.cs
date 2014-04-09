// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
#if MEF
    using Microsoft.CodeAnalysis.Host.Mef;

    [ExportWorkspaceServiceFactory(typeof(IBaseIndentationFormattingRuleFactoryService), ServiceLayer.Default)]
#endif
    internal sealed class DefaultBaseIndentationFormattingRuleFactoryServiceFactory : IWorkspaceServiceFactory
    {
        public DefaultBaseIndentationFormattingRuleFactoryServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Factory();
        }

        private sealed class Factory : IBaseIndentationFormattingRuleFactoryService
        {
            private readonly IFormattingRule singleton = new NoOpFormattingRule();

            public bool ShouldUseBaseIndentation(Document document)
            {
                return false;
            }

            public IFormattingRule CreateRule(Document document, int position)
            {
                return singleton;
            }

            public IEnumerable<TextChange> FilterFormattedChanges(Document document, TextSpan span, IList<TextChange> changes)
            {
                return changes;
            }
        }
    }
}
