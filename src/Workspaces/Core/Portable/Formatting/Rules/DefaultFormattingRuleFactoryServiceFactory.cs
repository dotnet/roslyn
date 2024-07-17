// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting.Rules;

[ExportWorkspaceService(typeof(IHostDependentFormattingRuleFactoryService), ServiceLayer.Default), Shared]
internal sealed class DefaultFormattingRuleFactoryService : IHostDependentFormattingRuleFactoryService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefaultFormattingRuleFactoryService()
    {
    }

    public bool ShouldNotFormatOrCommitOnPaste(DocumentId documentId)
        => false;

    public bool ShouldUseBaseIndentation(DocumentId documentId)
        => false;

    public AbstractFormattingRule CreateRule(ParsedDocument document, int position)
        => NoOpFormattingRule.Instance;

    public IEnumerable<TextChange> FilterFormattedChanges(DocumentId document, TextSpan span, IList<TextChange> changes)
        => changes;
}
