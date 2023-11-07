// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal sealed class UnmergedDocumentChanges(IEnumerable<TextChange> unmergedChanges, string projectName, DocumentId documentId)
    {
        public IEnumerable<TextChange> UnmergedChanges { get; } = unmergedChanges;
        public string ProjectName { get; } = projectName;
        public DocumentId DocumentId { get; } = documentId;
    }
}
