// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal interface IMergeConflictHandler
    {
        IEnumerable<TextChange> CreateEdits(SourceText originalSourceText, IEnumerable<UnmergedDocumentChanges> unmergedChanges);
    }
}
