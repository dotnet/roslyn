// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.MethodImplementation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot
{
    internal sealed class CopilotMethodImplementationProposedEditWrapper
    {
        private readonly MethodImplementationProposedEdit _methodImplementationProposedEdit;

        public CopilotMethodImplementationProposedEditWrapper(MethodImplementationProposedEdit proposedEdit)
        {
            _methodImplementationProposedEdit = proposedEdit;
        }

        public TextSpan SpanToReplace => _methodImplementationProposedEdit.SpanToReplace;

        public string? SymbolName => _methodImplementationProposedEdit.SymbolName;

        public CopilotMethodImplementationTagType TagType => (CopilotMethodImplementationTagType)_methodImplementationProposedEdit.TagType;
    }
}
