﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
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
        private const string DefaultDocumentName = "DocumentName";
        private static readonly string s_defaultDocumentPath = @$"Document\Path\{DefaultDocumentName}";

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
                var documentRenameResult = await Rename.Renamer.RenameDocumentAsync(document, endDocument.DocumentName, endDocument.DocumentFolders);

                foreach (var action in documentRenameResult.ApplicableActions)
                {
                    foreach (var error in action.GetErrors())
                    {
                        Assert.True(remainingErrors.Contains(error), $"Error '{error}' was unexpected");
                        remainingErrors.Remove(error);
                    }

                    // https://github.com/dotnet/roslyn/issues/44220
                    Assert.NotNull(action.GetDescription());
                }

                solution = await documentRenameResult.UpdateSolutionAsync(solution, CancellationToken.None);
                var updatedDocument = solution.GetDocument(documentId);

                if (endDocument.DocumentName is object)
                {
                    Assert.Equal(endDocument.DocumentName, updatedDocument.Name);
                }

                if (endDocument.DocumentFolders is object)
                {
                    AssertEx.SetEqual(endDocument.DocumentFolders, updatedDocument.Folders);
                }

                AssertEx.EqualOrDiff(endDocument.Text, (await updatedDocument.GetTextAsync()).ToString());
                Assert.Equal(0, remainingErrors.Count);
            }
        }

        private static string[] GetDocumentFolders(string filePath)
        {
            if (filePath is null)
            {
                return null;
            }

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
            var defaultDocumentPath = documentPath ?? s_defaultDocumentPath;

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
                    DocumentName = newDocumentName,
                    DocumentFilePath = newDocumentPath
                }
            };

            return TestRenameDocument(startDocuments, endDocuments, expectedErrors);
        }

        protected async Task TestEmptyActionSet(string startText, string newDocumentName = null, string newDocumentPath = null, string documentName = null, string documentPath = null)
        {
            var defaultDocumentName = documentName ?? DefaultDocumentName;
            var defaultDocumentPath = documentPath ?? s_defaultDocumentPath;

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
                    DocumentName = newDocumentName,
                    DocumentFilePath = newDocumentPath
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
                var documentRenameResult = await Rename.Renamer.RenameDocumentAsync(document, endDocument.DocumentName, endDocument.DocumentFolders);
                Assert.Empty(documentRenameResult.ApplicableActions);
            }
        }

        protected async Task TestRenameMappedFile(string startText, string documentName, string newDocumentName)
        {
            using var workspace = new AdhocWorkspace();
            var solution = workspace.CurrentSolution;

            var projectId = ProjectId.CreateNewId();
            var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Create(), "ProjectName", "AssemblyName", LanguageName, filePath: "");

            solution = solution.AddProject(projectInfo);

            var startSourceText = SourceText.From(startText);
            var documentId = DocumentId.CreateNewId(projectId);

            var documentInfo = DocumentInfo.Create(
                documentId,
                documentName,
                GetDocumentFolders(s_defaultDocumentPath),
                SourceCodeKind.Regular,
                TextLoader.From(TextAndVersion.Create(startSourceText, VersionStamp.Create(), documentName)),
                s_defaultDocumentPath,
                isGenerated: true,
                designTimeOnly: false,
                new TestDocumentServiceProvider());

            solution = solution.AddDocument(documentInfo);

            var document = solution.GetDocument(documentId);
            var documentRenameResult = await Rename.Renamer.RenameDocumentAsync(document, newDocumentName, GetDocumentFolders(s_defaultDocumentPath));
            Assert.Empty(documentRenameResult.ApplicableActions);
        }
    }
}
