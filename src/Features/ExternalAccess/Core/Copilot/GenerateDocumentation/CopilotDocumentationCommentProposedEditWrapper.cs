// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Text;

#if Unified_ExternalAccess
namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.Copilot;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;
#endif

internal sealed class CopilotDocumentationCommentProposedEditWrapper
{

    private readonly DocumentationCommentProposedEdit _documentationCommentProposedEdit;

    public CopilotDocumentationCommentProposedEditWrapper(DocumentationCommentProposedEdit proposedEdit)
    {
        _documentationCommentProposedEdit = proposedEdit;
    }

    public TextSpan SpanToReplace => _documentationCommentProposedEdit.SpanToReplace;

    public string? SymbolName => _documentationCommentProposedEdit.SymbolName;

    public CopilotDocumentationCommentTagType TagType => (CopilotDocumentationCommentTagType)_documentationCommentProposedEdit.TagType;
}
