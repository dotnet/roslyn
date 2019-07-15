// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.LinkedFileDiffMerging
{
    [UseExportProvider]
    public partial class LinkedFileDiffMergingTests
    {
        private void TestLinkedFileSet(string startText, List<string> updatedTexts, string expectedMergedText, string languageName)
        {
            using var workspace = new AdhocWorkspace();
            var solution = workspace.CurrentSolution;
            var startSourceText = SourceText.From(startText);
            var documentIds = new List<DocumentId>();

            for (var i = 0; i < updatedTexts.Count; i++)
            {
                var projectId = ProjectId.CreateNewId();
                var documentId = DocumentId.CreateNewId(projectId);
                documentIds.Add(documentId);

                var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Create(), "ProjectName" + i, "AssemblyName" + i, languageName);

                solution = solution
                    .AddProject(projectInfo)
                    .AddDocument(documentId, "DocumentName", startSourceText, filePath: "FilePath");
            }

            var startingSolution = solution;
            var updatedSolution = solution;

            for (var i = 0; i < updatedTexts.Count; i++)
            {
                var text = updatedTexts[i];
                if (text != startText)
                {
                    updatedSolution = updatedSolution
                        .WithDocumentText(documentIds[i], SourceText.From(text));
                }
            }

            var mergedSolution = updatedSolution.WithMergedLinkedFileChangesAsync(startingSolution).Result;
            for (var i = 0; i < updatedTexts.Count; i++)
            {
                Assert.Equal(expectedMergedText, mergedSolution.GetDocument(documentIds[i]).GetTextAsync().Result.ToString());
            }
        }
    }
}
