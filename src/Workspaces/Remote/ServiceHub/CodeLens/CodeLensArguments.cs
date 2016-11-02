// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    /// <summary>
    /// helper type to package diagnostic arguments to pass around between remote hosts
    /// </summary>
    internal class CodeLensArguments
    {
        public Guid ProjectIdGuid;
        public string ProjectIdDebugName;
        public Guid DocumentIdGuid;
        public string DocumentIdDebugName;
        public int Start;
        public int Length;

        public CodeLensArguments()
        {
        }

        public CodeLensArguments(DocumentId documentId, SyntaxNode syntaxNode)
        {
            ProjectIdGuid = documentId.ProjectId.Id;
            ProjectIdDebugName = documentId.ProjectId.DebugName;
            DocumentIdGuid = documentId.Id;
            DocumentIdDebugName = documentId.DebugName;
            Start = syntaxNode.Span.Start;
            Length = syntaxNode.Span.Length;
        }

        public DocumentId GetDocumentId()
            =>
            DocumentId.CreateFromSerialized(ProjectId.CreateFromSerialized(ProjectIdGuid, ProjectIdDebugName),
                DocumentIdGuid, DocumentIdDebugName);

        public TextSpan GetTextSpan() => new TextSpan(Start, Length);
    }
}