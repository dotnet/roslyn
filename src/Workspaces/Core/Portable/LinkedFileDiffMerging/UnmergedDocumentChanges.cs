// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
