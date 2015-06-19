// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal sealed class UnmergedDocumentChanges
    {
        public IEnumerable<TextChange> UnmergedChanges { get; }
        public string ProjectName { get; }
        public DocumentId DocumentId { get; }

        public UnmergedDocumentChanges(IEnumerable<TextChange> unmergedChanges, string projectName, DocumentId documentId)
        {
            UnmergedChanges = unmergedChanges;
            ProjectName = projectName;
            DocumentId = documentId;
        }
    }
}
