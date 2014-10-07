// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal interface ILinkedFileMergeConflictCommentAdditionService : ILanguageService
    {
        IEnumerable<TextChange> CreateCommentsForUnmergedChanges(SourceText originalSourceText, IEnumerable<UnmergedDocumentChanges> unmergedChanges);
    }
}