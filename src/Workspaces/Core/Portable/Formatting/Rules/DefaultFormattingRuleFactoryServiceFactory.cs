// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    [ExportWorkspaceServiceFactory(typeof(IHostDependentFormattingRuleFactoryService), ServiceLayer.Default), Shared]
    internal sealed class DefaultFormattingRuleFactoryServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        public DefaultFormattingRuleFactoryServiceFactory()
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
                return false;
            }

            public AbstractFormattingRule CreateRule(Document document, int position)
            {
                return NoOpFormattingRule.Instance;
            }

            public IEnumerable<TextChange> FilterFormattedChanges(Document document, TextSpan span, IList<TextChange> changes)
            {
                return changes;
            }

            public bool ShouldNotFormatOrCommitOnPaste(Document document)
            {
                return false;
            }
        }
    }
}
