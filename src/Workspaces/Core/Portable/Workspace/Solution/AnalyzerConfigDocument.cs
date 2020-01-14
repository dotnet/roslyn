// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis
{
    public sealed class AnalyzerConfigDocument : TextDocument
    {
        internal AnalyzerConfigDocument(Project project, AnalyzerConfigDocumentState state)
            : base(project, state, TextDocumentKind.AnalyzerConfigDocument)
        {
        }
    }
}
