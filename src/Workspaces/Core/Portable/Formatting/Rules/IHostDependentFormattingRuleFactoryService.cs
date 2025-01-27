// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting.Rules;

internal interface IHostDependentFormattingRuleFactoryService : IWorkspaceService
{
    bool ShouldNotFormatOrCommitOnPaste(DocumentId documentId);
    bool ShouldUseBaseIndentation(DocumentId documentId);
    AbstractFormattingRule CreateRule(ParsedDocument document, int position);
    IEnumerable<TextChange> FilterFormattedChanges(DocumentId documentId, TextSpan span, IList<TextChange> changes);
}
