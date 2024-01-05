﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    public enum ApplyChangesKind
    {
        AddProject = 0,
        RemoveProject = 1,
        AddProjectReference = 2,
        RemoveProjectReference = 3,
        AddMetadataReference = 4,
        RemoveMetadataReference = 5,
        AddDocument = 6,
        RemoveDocument = 7,
        ChangeDocument = 8,
        AddAnalyzerReference = 9,
        RemoveAnalyzerReference = 10,
        AddAdditionalDocument = 11,
        RemoveAdditionalDocument = 12,
        ChangeAdditionalDocument = 13,
        ChangeCompilationOptions = 14,
        ChangeParseOptions = 15,
        ChangeDocumentInfo = 16,
        AddAnalyzerConfigDocument = 17,
        RemoveAnalyzerConfigDocument = 18,
        ChangeAnalyzerConfigDocument = 19,
        AddSolutionAnalyzerReference = 20,
        RemoveSolutionAnalyzerReference = 21,
    }
}
