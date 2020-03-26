// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Renamer
{
    [UseExportProvider]
    public abstract class RenamerTests : TestBase
    {
        protected const string DefaultDocumentName = "DocumentName";
        protected static readonly string DefaultDocumentPath = @$"Document\Path\{DefaultDocumentName}";

        protected abstract string LanguageName { get; }

        protected struct DocumentWithInfo
        {
            public string Text { get; set; }
            public string DocumentName { get; set; }
            public string DocumentFilePath { get; set; }
            public string[] DocumentFolders => GetDocumentFolders(DocumentFilePath);
        }

        protected async Task TestRenameDocument(
            DocumentWithInfo[] startDocuments,
            DocumentWithInfo[] endDocuments,
            string[] expectedErrors = null)
        {
            using var workspace = new AdhocWorkspace();
            var solution = workspace.CurrentSolution;

            var projectId = ProjectId.CreateNewId();
            var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Create(), "ProjectName", "AssemblyName", LanguageName, filePath: "");
            var documentIdToDocumentInfoMap = new List<(DocumentId, DocumentWithInfo)>();

            solution = solution
                    .AddProject(projectInfo);

            var remainingErrors = new HashSet<string>(expectedErrors ?? new string[0]);

            for (var i = 0; i < startDocuments.Length; i++)
            {
                var startDocument = startDocuments[i];
                var startSourceText = SourceText.From(startDocument.Text);
                var documentId = DocumentId.CreateNewId(projectId);

                solution = solution
                    .AddDocument(
                        documentId,
                        startDocument.DocumentName,
                        startSourceText,
                        filePath: startDocument.DocumentFilePath,
                        folders: startDocument.DocumentFolders);

                documentIdToDocumentInfoMap.Add((documentId, endDocuments[i]));
            }

            foreach (var (documentId, endDocument) in documentIdToDocumentInfoMap)
            {
                var document = solution.GetDocument(documentId);
                var documentRenameResult = await Rename.Renamer.RenameDocumentNameAsync(document, endDocument.DocumentName, workspace.Options);
                var documentFoldersRenameResult = await Rename.Renamer.RenameDocumentFoldersAsync(document, endDocument.DocumentFolders, workspace.Options);

                foreach (var action in documentRenameResult.ApplicableActions.Concat(documentFoldersRenameResult.ApplicableActions))
                {
                    foreach (var error in action.GetErrors())
                    {
                        Assert.True(remainingErrors.Contains(error), $"Error '{error}' was unexpected");
                        remainingErrors.Remove(error);
                    }
                }

                solution = await documentRenameResult.UpdateSolutionAsync(solution, CancellationToken.None);
                var updatedDocument = solution.GetDocument(documentId);
                Assert.Equal(endDocument.DocumentName, updatedDocument.Name);

                solution = await documentFoldersRenameResult.UpdateSolutionAsync(solution, CancellationToken.None);
                updatedDocument = solution.GetDocument(documentId);
                AssertEx.SetEqual(endDocument.DocumentFolders, updatedDocument.Folders);

                AssertEx.EqualOrDiff(endDocument.Text, (await updatedDocument.GetTextAsync()).ToString());
                Assert.Equal(0, remainingErrors.Count);
            }
        }

        private static string[] GetDocumentFolders(string filePath)
        {
            var splitPath = filePath.Split('\\');
            if (splitPath.Length == 1)
            {
                return splitPath;
            }

            return splitPath.Take(splitPath.Length - 1).ToArray();
        }

        protected Task TestRenameDocument(string startText, string expectedText, string newDocumentName = null, string newDocumentPath = null, string documentName = null, string documentPath = null, string[] expectedErrors = null)
        {
            var defaultDocumentName = documentName ?? DefaultDocumentName;
            var defaultDocumentPath = documentPath ?? DefaultDocumentPath;

            var startDocuments = new[]
            {
                new DocumentWithInfo()
                {
                    Text = startText,
                    DocumentName = defaultDocumentName,
                    DocumentFilePath = defaultDocumentPath
                }
            };

            var endDocuments = new[]
            {
                new DocumentWithInfo()
                {
                    Text = expectedText,
                    DocumentName = newDocumentName ?? defaultDocumentName,
                    DocumentFilePath = newDocumentPath ?? defaultDocumentPath
                }
            };

            return TestRenameDocument(startDocuments, endDocuments, expectedErrors);
        }

        protected async Task TestEmptyActionSet(string startText, string newDocumentName = null, string newDocumentPath = null, string documentName = null, string documentPath = null)
        {
            var defaultDocumentName = documentName ?? DefaultDocumentName;
            var defaultDocumentPath = documentPath ?? DefaultDocumentPath;

            var startDocuments = new[]
            {
                new DocumentWithInfo()
                {
                    Text = startText,
                    DocumentName = defaultDocumentName,
                    DocumentFilePath = defaultDocumentPath
                }
            };

            var endDocuments = new[]
            {
                new DocumentWithInfo()
                {
                    Text = startText,
                    DocumentName = newDocumentName ?? defaultDocumentName,
                    DocumentFilePath = newDocumentPath ?? defaultDocumentPath
                }
            };

            using var workspace = new AdhocWorkspace();
            var solution = workspace.CurrentSolution;

            var projectId = ProjectId.CreateNewId();
            var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Create(), "ProjectName", "AssemblyName", LanguageName, filePath: "");
            var documentIdToDocumentInfoMap = new List<(DocumentId, DocumentWithInfo)>();

            solution = solution
                    .AddProject(projectInfo);

            for (var i = 0; i < startDocuments.Length; i++)
            {
                var startDocument = startDocuments[i];
                var startSourceText = SourceText.From(startDocument.Text);
                var documentId = DocumentId.CreateNewId(projectId);

                solution = solution
                    .AddDocument(
                        documentId,
                        startDocument.DocumentName,
                        startSourceText,
                        filePath: startDocument.DocumentFilePath,
                        folders: startDocument.DocumentFolders);

                documentIdToDocumentInfoMap.Add((documentId, endDocuments[i]));
            }

            foreach (var (documentId, endDocument) in documentIdToDocumentInfoMap)
            {
                var document = solution.GetDocument(documentId);
                var documentRenameResult = await Rename.Renamer.RenameDocumentNameAsync(document, endDocument.DocumentName, workspace.Options);
                var documentFoldersRenameResult = await Rename.Renamer.RenameDocumentFoldersAsync(document, endDocument.DocumentFolders, workspace.Options);

                Assert.Empty(documentRenameResult.ApplicableActions);
                Assert.Empty(documentFoldersRenameResult.ApplicableActions);
            }
        }
    }
}
