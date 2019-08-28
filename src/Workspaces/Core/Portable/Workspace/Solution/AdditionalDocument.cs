// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents an additional file passed down to analyzers.
    /// </summary>
    public sealed class AdditionalDocument : TextDocument
    {
        internal AdditionalDocument(Project project, TextDocumentState state)
            : base(project, state, TextDocumentKind.AdditionalDocument)
        {
        }
    }
}
