// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal sealed class UnmergedDocumentChanges
    {
        public IEnumerable<TextChange> UnmergedChanges { get; private set; }
        public SourceText Text { get; private set; }
        public string ProjectName { get; private set; }

        public UnmergedDocumentChanges(IEnumerable<TextChange> unmergedChanges, SourceText text, string projectName)
        {
            UnmergedChanges = unmergedChanges;
            Text = text;
            ProjectName = projectName;
        }
    }
}
