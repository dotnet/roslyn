﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualStudio.Threading;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.UnitTests.SolutionTestHelpers;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.Workspace)]
    public class SolutionTests : TestBase
    {
#nullable enable
        private static readonly MetadataReference s_mscorlib = TestMetadata.Net451.mscorlib;
        private static readonly DocumentId s_unrelatedDocumentId = DocumentId.CreateNewId(ProjectId.CreateNewId());

        private static Workspace CreateWorkspaceWithProjectAndDocuments()
        {
            var projectId = ProjectId.CreateNewId();

            var workspace = CreateWorkspace();

            Assert.True(workspace.TryApplyChanges(workspace.CurrentSolution
                .AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp)
                .AddDocument(DocumentId.CreateNewId(projectId), "goo.cs", SourceText.From("public class Goo { }", Encoding.UTF8, SourceHashAlgorithms.Default))
                .AddAdditionalDocument(DocumentId.CreateNewId(projectId), "add.txt", SourceText.From("text", Encoding.UTF8, SourceHashAlgorithms.Default))
                .AddAnalyzerConfigDocument(DocumentId.CreateNewId(projectId), "editorcfg", SourceText.From("config", Encoding.UTF8, SourceHashAlgorithms.Default), filePath: "/a/b")));

            return workspace;
        }

        private static Workspace CreateWorkspaceWithProjectAndLinkedDocuments(
            string docContents, ParseOptions? parseOptions1 = null, ParseOptions? parseOptions2 = null)
        {
            parseOptions1 ??= CSharpParseOptions.Default;
            parseOptions2 ??= CSharpParseOptions.Default;
            var projectId1 = ProjectId.CreateNewId();
            var projectId2 = ProjectId.CreateNewId();

            var workspace = CreateWorkspace();

            // note: despite the additional-doc and analyzer-config doc being at the same path in multiple projects,
            // they will still be treated as unique as the workspace only has the concept of linked docs for normal
            // docs.
            Assert.True(workspace.TryApplyChanges(workspace.CurrentSolution
                .AddProject(projectId1, "proj1", "proj1.dll", LanguageNames.CSharp).WithProjectParseOptions(projectId1, parseOptions1)
                .AddDocument(DocumentId.CreateNewId(projectId1), "goo.cs", SourceText.From(docContents, Encoding.UTF8, SourceHashAlgorithms.Default), filePath: "goo.cs")
                .AddAdditionalDocument(DocumentId.CreateNewId(projectId1), "add.txt", SourceText.From("text", Encoding.UTF8, SourceHashAlgorithms.Default), filePath: "add.txt")
                .AddAnalyzerConfigDocument(DocumentId.CreateNewId(projectId1), "editorcfg", SourceText.From("config", Encoding.UTF8, SourceHashAlgorithms.Default), filePath: "/a/b")
                .AddProject(projectId2, "proj2", "proj2.dll", LanguageNames.CSharp).WithProjectParseOptions(projectId2, parseOptions2)
                .AddDocument(DocumentId.CreateNewId(projectId2), "goo.cs", SourceText.From(docContents, Encoding.UTF8, SourceHashAlgorithms.Default), filePath: "goo.cs")
                .AddAdditionalDocument(DocumentId.CreateNewId(projectId2), "add.txt", SourceText.From("text", Encoding.UTF8, SourceHashAlgorithms.Default), filePath: "add.txt")
                .AddAnalyzerConfigDocument(DocumentId.CreateNewId(projectId2), "editorcfg", SourceText.From("config", Encoding.UTF8, SourceHashAlgorithms.Default), filePath: "/a/b")));

            return workspace;
        }

        private static IEnumerable<T> EmptyEnumerable<T>()
        {
            yield break;
        }

        // Returns an enumerable that can only be enumerated once.
        private static IEnumerable<T> OnceEnumerable<T>(params T[] items)
            => OnceEnumerableImpl(new StrongBox<int>(), items);

        private static IEnumerable<T> OnceEnumerableImpl<T>(StrongBox<int> counter, T[] items)
        {
            Assert.Equal(0, counter.Value);
            counter.Value++;

            foreach (var item in items)
            {
                yield return item;
            }
        }

        [Fact]
        public void RemoveDocument_Errors()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            Assert.Throws<ArgumentNullException>(() => solution.RemoveDocument(null!));
            Assert.Throws<InvalidOperationException>(() => solution.RemoveDocument(s_unrelatedDocumentId));
        }

        [Fact]
        public void RemoveDocuments_Errors()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            Assert.Throws<ArgumentNullException>(() => solution.RemoveDocuments(default));
            Assert.Throws<InvalidOperationException>(() => solution.RemoveDocuments(ImmutableArray.Create(s_unrelatedDocumentId)));
            Assert.Throws<ArgumentNullException>(() => solution.RemoveDocuments(ImmutableArray.Create((DocumentId)null!)));
        }

        [Fact]
        public void RemoveAdditionalDocument_Errors()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            Assert.Throws<ArgumentNullException>(() => solution.RemoveAdditionalDocument(null!));
            Assert.Throws<InvalidOperationException>(() => solution.RemoveAdditionalDocument(s_unrelatedDocumentId));
        }

        [Fact]
        public void RemoveAdditionalDocuments_Errors()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            Assert.Throws<ArgumentNullException>(() => solution.RemoveAdditionalDocuments(default));
            Assert.Throws<InvalidOperationException>(() => solution.RemoveAdditionalDocuments(ImmutableArray.Create(s_unrelatedDocumentId)));
            Assert.Throws<ArgumentNullException>(() => solution.RemoveAdditionalDocuments(ImmutableArray.Create((DocumentId)null!)));
        }

        [Fact]
        public void RemoveAnalyzerConfigDocument_Errors()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            Assert.Throws<ArgumentNullException>(() => solution.RemoveAnalyzerConfigDocument(null!));
            Assert.Throws<InvalidOperationException>(() => solution.RemoveAnalyzerConfigDocument(s_unrelatedDocumentId));
        }

        [Fact]
        public void RemoveAnalyzerConfigDocuments_Errors()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            Assert.Throws<ArgumentNullException>(() => solution.RemoveAnalyzerConfigDocuments(default));
            Assert.Throws<InvalidOperationException>(() => solution.RemoveAnalyzerConfigDocuments(ImmutableArray.Create(s_unrelatedDocumentId)));
            Assert.Throws<ArgumentNullException>(() => solution.RemoveAnalyzerConfigDocuments(ImmutableArray.Create((DocumentId)null!)));
        }

        [Fact]
        public void WithDocumentName()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.Single().DocumentIds.Single();
            var name = "new name";

            var newSolution1 = solution.WithDocumentName(documentId, name);
            Assert.Equal(name, newSolution1.GetDocument(documentId)!.Name);

            var newSolution2 = newSolution1.WithDocumentName(documentId, name);
            Assert.Same(newSolution1, newSolution2);

            Assert.Throws<ArgumentNullException>(() => solution.WithDocumentName(documentId, name: null!));

            Assert.Throws<ArgumentNullException>(() => solution.WithDocumentName(null!, name));
            Assert.Throws<InvalidOperationException>(() => solution.WithDocumentName(s_unrelatedDocumentId, name));
        }

        [Fact]
        public void WithDocumentFolders()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.Single().DocumentIds.Single();
            var folders = new[] { "folder1", "folder2" };

            var newSolution1 = solution.WithDocumentFolders(documentId, folders);
            Assert.Equal(folders, newSolution1.GetDocument(documentId)!.Folders);

            var newSolution2 = newSolution1.WithDocumentFolders(documentId, folders);
            Assert.Same(newSolution2, newSolution1);

            // empty:
            var newSolution3 = solution.WithDocumentFolders(documentId, new string[0]);
            Assert.Equal(new string[0], newSolution3.GetDocument(documentId)!.Folders);

            var newSolution4 = solution.WithDocumentFolders(documentId, ImmutableArray<string>.Empty);
            Assert.Same(newSolution3, newSolution4);

            var newSolution5 = solution.WithDocumentFolders(documentId, null);
            Assert.Same(newSolution3, newSolution5);

            Assert.Throws<ArgumentNullException>(() => solution.WithDocumentFolders(documentId, folders: new string[] { null! }));

            Assert.Throws<ArgumentNullException>(() => solution.WithDocumentFolders(null!, folders));
            Assert.Throws<InvalidOperationException>(() => solution.WithDocumentFolders(s_unrelatedDocumentId, folders));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34837")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/37125")]
        public void WithDocumentFilePath()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.Single().DocumentIds.Single();
            var path = "new path";

            var newSolution1 = solution.WithDocumentFilePath(documentId, path);
            Assert.Equal(path, newSolution1.GetDocument(documentId)!.FilePath);
            AssertEx.Equal(new[] { documentId }, newSolution1.GetDocumentIdsWithFilePath(path));

            var newSolution2 = newSolution1.WithDocumentFilePath(documentId, path);
            Assert.Same(newSolution1, newSolution2);

            // empty path (TODO https://github.com/dotnet/roslyn/issues/37125):
            var newSolution3 = solution.WithDocumentFilePath(documentId, "");
            Assert.Equal("", newSolution3.GetDocument(documentId)!.FilePath);
            Assert.Empty(newSolution3.GetDocumentIdsWithFilePath(""));

            // TODO: https://github.com/dotnet/roslyn/issues/37125
            Assert.Throws<ArgumentNullException>(() => solution.WithDocumentFilePath(documentId, filePath: null!));

            Assert.Throws<ArgumentNullException>(() => solution.WithDocumentFilePath(null!, path));
            Assert.Throws<InvalidOperationException>(() => solution.WithDocumentFilePath(s_unrelatedDocumentId, path));
        }

        [Fact]
        public void WithSourceCodeKind()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.Single().DocumentIds.Single();

            Assert.Same(solution, solution.WithDocumentSourceCodeKind(documentId, SourceCodeKind.Regular));

            var newSolution1 = solution.WithDocumentSourceCodeKind(documentId, SourceCodeKind.Script);
            Assert.Equal(SourceCodeKind.Script, newSolution1.GetDocument(documentId)!.SourceCodeKind);

            Assert.Throws<ArgumentOutOfRangeException>(() => solution.WithDocumentSourceCodeKind(documentId, (SourceCodeKind)(-1)));

            Assert.Throws<ArgumentNullException>(() => solution.WithDocumentSourceCodeKind(null!, SourceCodeKind.Script));
            Assert.Throws<InvalidOperationException>(() => solution.WithDocumentSourceCodeKind(s_unrelatedDocumentId, SourceCodeKind.Script));
        }

        [Fact, Obsolete("Testing obsolete API")]
        public void WithSourceCodeKind_Obsolete()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.Single().DocumentIds.Single();

            var newSolution = solution.WithDocumentSourceCodeKind(documentId, SourceCodeKind.Interactive);
            Assert.Equal(SourceCodeKind.Script, newSolution.GetDocument(documentId)!.SourceCodeKind);
        }

        [Fact]
        public void WithDocumentSyntaxRoot()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.Single().DocumentIds.Single();

            var tree = CS.SyntaxFactory.ParseSyntaxTree("class NewClass {}");
            Assert.Equal(SourceHashAlgorithm.Sha1, tree.GetText().ChecksumAlgorithm);

            var root = tree.GetRoot();

            var newSolution1 = solution.WithDocumentSyntaxRoot(documentId, root, PreservationMode.PreserveIdentity);
            Assert.True(newSolution1.GetDocument(documentId)!.TryGetSyntaxRoot(out var actualRoot));
            Assert.Equal(root.ToString(), actualRoot!.ToString());

            // the actual root has a new parent SyntaxTree:
            Assert.NotSame(root, actualRoot);

            var newSolution2 = newSolution1.WithDocumentSyntaxRoot(documentId, actualRoot);
            Assert.Same(newSolution1, newSolution2);

            Assert.Throws<ArgumentOutOfRangeException>(() => solution.WithDocumentSyntaxRoot(documentId, root, (PreservationMode)(-1)));

            Assert.Throws<ArgumentNullException>(() => solution.WithDocumentSyntaxRoot(null!, root));
            Assert.Throws<InvalidOperationException>(() => solution.WithDocumentSyntaxRoot(s_unrelatedDocumentId, root));
        }

        [Fact, WorkItem(37125, "https://github.com/dotnet/roslyn/issues/41940")]
        public async Task WithDocumentSyntaxRoot_AnalyzerConfigWithoutFilePath()
        {
            var projectId = ProjectId.CreateNewId();

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                            .AddProject(projectId, "goo", "goo.dll", LanguageNames.CSharp)
                            .AddDocument(DocumentId.CreateNewId(projectId), "goo.cs", "public class Goo { }")
                            .AddAnalyzerConfigDocument(DocumentId.CreateNewId(projectId), "editorcfg", SourceText.From("config"));

            var project = solution.GetProject(projectId)!;
            var compilation = (await project.GetCompilationAsync())!;
            var tree = compilation.SyntaxTrees.Single();
            var provider = compilation.Options.SyntaxTreeOptionsProvider!;
            Assert.Throws<ArgumentException>(() => provider.TryGetDiagnosticValue(tree, "CA1234", CancellationToken.None, out _));
        }

        [Fact]
        public void WithDocumentText_SourceText()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.Single().DocumentIds.Single();

            var text = SourceText.From("new text", encoding: null, SourceHashAlgorithm.Sha1);

            var newSolution1 = solution.WithDocumentText(documentId, text, PreservationMode.PreserveIdentity);
            var newDocument1 = newSolution1.GetRequiredDocument(documentId);

            Assert.True(newDocument1.TryGetText(out var actualText));
            Assert.Same(text, actualText);

            var newSolution2 = newSolution1.WithDocumentText(documentId, text, PreservationMode.PreserveIdentity);
            Assert.Same(newSolution1, newSolution2);

            Assert.Throws<ArgumentNullException>(() => solution.WithDocumentText(documentId, (SourceText)null!, PreservationMode.PreserveIdentity));
            Assert.Throws<ArgumentOutOfRangeException>(() => solution.WithDocumentText(documentId, text, (PreservationMode)(-1)));

            Assert.Throws<ArgumentNullException>(() => solution.WithDocumentText((DocumentId)null!, text, PreservationMode.PreserveIdentity));
            Assert.Throws<InvalidOperationException>(() => solution.WithDocumentText(s_unrelatedDocumentId, text, PreservationMode.PreserveIdentity));
        }

        [Fact]
        public void WithDocumentText_TextAndVersion()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.Single().DocumentIds.Single();
            var textAndVersion = TextAndVersion.Create(SourceText.From("new text"), VersionStamp.Default);

            var newSolution1 = solution.WithDocumentText(documentId, textAndVersion, PreservationMode.PreserveIdentity);
            Assert.True(newSolution1.GetDocument(documentId)!.TryGetText(out var actualText));
            Assert.True(newSolution1.GetDocument(documentId)!.TryGetTextVersion(out var actualVersion));
            Assert.Same(textAndVersion.Text, actualText);
            Assert.Equal(textAndVersion.Version, actualVersion);

            var newSolution2 = newSolution1.WithDocumentText(documentId, textAndVersion, PreservationMode.PreserveIdentity);
            Assert.Same(newSolution1, newSolution2);

            Assert.Throws<ArgumentNullException>(() => solution.WithDocumentText(documentId, (SourceText)null!, PreservationMode.PreserveIdentity));
            Assert.Throws<ArgumentOutOfRangeException>(() => solution.WithDocumentText(documentId, textAndVersion, (PreservationMode)(-1)));

            Assert.Throws<ArgumentNullException>(() => solution.WithDocumentText((DocumentId)null!, textAndVersion, PreservationMode.PreserveIdentity));
            Assert.Throws<InvalidOperationException>(() => solution.WithDocumentText(s_unrelatedDocumentId, textAndVersion, PreservationMode.PreserveIdentity));
        }

        [Fact]
        public void WithDocumentText_MultipleDocuments()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.Single().DocumentIds.Single();
            var text = SourceText.From("new text");

            var newSolution1 = solution.WithDocumentText(new[] { documentId }, text, PreservationMode.PreserveIdentity);
            Assert.True(newSolution1.GetDocument(documentId)!.TryGetText(out var actualText));
            Assert.Same(text, actualText);

            var newSolution2 = newSolution1.WithDocumentText(new[] { documentId }, text, PreservationMode.PreserveIdentity);
            Assert.Same(newSolution1, newSolution2);

            // documents not in solution are skipped: https://github.com/dotnet/roslyn/issues/42029
            Assert.Same(solution, solution.WithDocumentText(new DocumentId[] { null! }, text));
            Assert.Same(solution, solution.WithDocumentText(new DocumentId[] { s_unrelatedDocumentId }, text));

            Assert.Throws<ArgumentNullException>(() => solution.WithDocumentText((DocumentId[])null!, text, PreservationMode.PreserveIdentity));
            Assert.Throws<ArgumentNullException>(() => solution.WithDocumentText(new[] { documentId }, null!, PreservationMode.PreserveIdentity));
            Assert.Throws<ArgumentOutOfRangeException>(() => solution.WithDocumentText(new[] { documentId }, text, (PreservationMode)(-1)));
        }

        public enum TextUpdateType
        {
            SourceText,
            TextLoader,
            TextAndVersion,
        }

        [Theory, CombinatorialData]
        public async Task WithDocumentText_LinkedFiles(
            PreservationMode mode,
            TextUpdateType updateType)
        {
            using var workspace = CreateWorkspaceWithProjectAndLinkedDocuments("public class Goo { }");
            var solution = workspace.CurrentSolution;

            var documentId1 = solution.Projects.First().DocumentIds.Single();
            var documentId2 = solution.Projects.Last().DocumentIds.Single();

            var document1 = solution.GetRequiredDocument(documentId1);
            var document2 = solution.GetRequiredDocument(documentId2);

            var text1 = await document1.GetTextAsync();
            var text2 = await document2.GetTextAsync();
            var version1 = await document1.GetTextVersionAsync();
            var version2 = await document2.GetTextVersionAsync();
            var root1 = await document1.GetRequiredSyntaxRootAsync(CancellationToken.None);
            var root2 = await document2.GetRequiredSyntaxRootAsync(CancellationToken.None);

            Assert.Equal(text1.ToString(), text2.ToString());
            Assert.Equal(version1, version2);

            // We get different red nodes, but they should be backed by the same green nodes.
            Assert.NotEqual(root1, root2);
            Assert.True(root1.IsIncrementallyIdenticalTo(root2));

            var text = SourceText.From("new text", encoding: null, SourceHashAlgorithm.Sha1);
            var textAndVersion = TextAndVersion.Create(text, VersionStamp.Create());
            solution = UpdateSolution(mode, updateType, solution, documentId1, text, textAndVersion);

            // because we only forked one doc, the text/versions should be different in this interim solution.

            document1 = solution.GetRequiredDocument(documentId1);
            document2 = solution.GetRequiredDocument(documentId2);

            text1 = await document1.GetTextAsync();
            text2 = await document2.GetTextAsync();
            version1 = await document1.GetTextVersionAsync();
            version2 = await document2.GetTextVersionAsync();
            root1 = await document1.GetRequiredSyntaxRootAsync(CancellationToken.None);
            root2 = await document2.GetRequiredSyntaxRootAsync(CancellationToken.None);

            Assert.NotEqual(text1.ToString(), text2.ToString());
            Assert.NotEqual(version1, version2);

            // We get different red and green nodes.
            Assert.NotEqual(root1, root2);
            Assert.False(root1.IsIncrementallyIdenticalTo(root2));

            // Now apply the change to the workspace.  This should bring the linked document in sync with the one we changed.
            workspace.TryApplyChanges(solution);
            solution = workspace.CurrentSolution;

            document1 = solution.GetRequiredDocument(documentId1);
            document2 = solution.GetRequiredDocument(documentId2);

            text1 = await document1.GetTextAsync();
            text2 = await document2.GetTextAsync();
            version1 = await document1.GetTextVersionAsync();
            version2 = await document2.GetTextVersionAsync();
            root1 = await document1.GetRequiredSyntaxRootAsync(CancellationToken.None);
            root2 = await document2.GetRequiredSyntaxRootAsync(CancellationToken.None);

            Assert.Equal(text1.ToString(), text2.ToString());
            Assert.Equal(version1, version2);

            // We get different red nodes, but they should be backed by the same green nodes.
            Assert.NotEqual(root1, root2);
            Assert.True(root1.IsIncrementallyIdenticalTo(root2));
        }

        private static Solution UpdateSolution(PreservationMode mode, TextUpdateType updateType, Solution solution, DocumentId documentId1, SourceText text, TextAndVersion textAndVersion)
        {
            solution = updateType switch
            {
                TextUpdateType.SourceText => solution.WithDocumentText(documentId1, text, mode),
                TextUpdateType.TextAndVersion => solution.WithDocumentText(documentId1, textAndVersion, mode),
                TextUpdateType.TextLoader => solution.WithDocumentTextLoader(documentId1, TextLoader.From(textAndVersion), mode),
                _ => throw ExceptionUtilities.UnexpectedValue(updateType)
            };
            return solution;
        }

        [Theory, CombinatorialData]
        public async Task WithDocumentText_LinkedFiles_PPConditionalDirective_SameParseOptions(
            PreservationMode mode,
            TextUpdateType updateType)
        {
            using var workspace = CreateWorkspaceWithProjectAndLinkedDocuments("""
                #if NETSTANDARD
                public class Goo { }
                """);
            var solution = workspace.CurrentSolution;

            var documentId1 = solution.Projects.First().DocumentIds.Single();
            var documentId2 = solution.Projects.Last().DocumentIds.Single();

            var document1 = solution.GetRequiredDocument(documentId1);
            var document2 = solution.GetRequiredDocument(documentId2);

            var text1 = await document1.GetTextAsync();
            var text2 = await document2.GetTextAsync();
            var version1 = await document1.GetTextVersionAsync();
            var version2 = await document2.GetTextVersionAsync();
            var root1 = await document1.GetRequiredSyntaxRootAsync(CancellationToken.None);
            var root2 = await document2.GetRequiredSyntaxRootAsync(CancellationToken.None);

            Assert.Equal(text1.ToString(), text2.ToString());
            Assert.Equal(version1, version2);

            // We can reuse trees with conditional directives if the parse options are the same.
            Assert.NotEqual(root1, root2);
            Assert.True(root1.IsIncrementallyIdenticalTo(root2));

            var text = SourceText.From("new text", encoding: null, SourceHashAlgorithm.Sha1);
            var textAndVersion = TextAndVersion.Create(text, VersionStamp.Create());
            solution = UpdateSolution(mode, updateType, solution, documentId1, text, textAndVersion);

            // because we only forked one doc, the text/versions should be different in this interim solution.

            document1 = solution.GetRequiredDocument(documentId1);
            document2 = solution.GetRequiredDocument(documentId2);

            text1 = await document1.GetTextAsync();
            text2 = await document2.GetTextAsync();
            version1 = await document1.GetTextVersionAsync();
            version2 = await document2.GetTextVersionAsync();
            root1 = await document1.GetRequiredSyntaxRootAsync(CancellationToken.None);
            root2 = await document2.GetRequiredSyntaxRootAsync(CancellationToken.None);

            Assert.NotEqual(text1.ToString(), text2.ToString());
            Assert.NotEqual(version1, version2);

            // We get different red and green nodes entirely
            Assert.NotEqual(root1, root2);
            Assert.False(root1.IsIncrementallyIdenticalTo(root2));

            // Now apply the change to the workspace.  This should bring the linked document in sync with the one we changed.
            workspace.TryApplyChanges(solution);
            solution = workspace.CurrentSolution;

            document1 = solution.GetRequiredDocument(documentId1);
            document2 = solution.GetRequiredDocument(documentId2);

            text1 = await document1.GetTextAsync();
            text2 = await document2.GetTextAsync();
            version1 = await document1.GetTextVersionAsync();
            version2 = await document2.GetTextVersionAsync();
            root1 = await document1.GetRequiredSyntaxRootAsync(CancellationToken.None);
            root2 = await document2.GetRequiredSyntaxRootAsync(CancellationToken.None);

            Assert.Equal(text1.ToString(), text2.ToString());
            Assert.Equal(version1, version2);

            // We can reuse trees with conditional directives if the parse options are the same.
            Assert.NotEqual(root1, root2);
            Assert.True(root1.IsIncrementallyIdenticalTo(root2));
        }

        [Theory, CombinatorialData]
        public async Task WithDocumentText_LinkedFiles_PPConditionalDirective_DifferentParseOptions1(
            PreservationMode mode,
            TextUpdateType updateType)
        {
            var parseOptions1 = CSharpParseOptions.Default.WithPreprocessorSymbols("UNIQUE_NAME");
            var parseOptions2 = CSharpParseOptions.Default;

            using var workspace = CreateWorkspaceWithProjectAndLinkedDocuments("""
                #if NETSTANDARD
                public class Goo { }
                """, parseOptions1, parseOptions2);
            var solution = workspace.CurrentSolution;

            var documentId1 = solution.Projects.First().DocumentIds.Single();
            var documentId2 = solution.Projects.Last().DocumentIds.Single();

            var document1 = solution.GetRequiredDocument(documentId1);
            var document2 = solution.GetRequiredDocument(documentId2);

            var text1 = await document1.GetTextAsync();
            var text2 = await document2.GetTextAsync();
            var version1 = await document1.GetTextVersionAsync();
            var version2 = await document2.GetTextVersionAsync();
            var root1 = await document1.GetRequiredSyntaxRootAsync(CancellationToken.None);
            var root2 = await document2.GetRequiredSyntaxRootAsync(CancellationToken.None);

            Assert.Equal(text1.ToString(), text2.ToString());
            Assert.Equal(version1, version2);

            // We can never reuse trees with conditional directives.
            Assert.NotEqual(root1, root2);
            Assert.False(root1.IsIncrementallyIdenticalTo(root2));

            Assert.Equal(parseOptions1, root1.SyntaxTree.Options);
            Assert.Equal(parseOptions2, root2.SyntaxTree.Options);

            // Because we removed pp directives, we'll be able to reuse after this.
            var text = SourceText.From("new text without pp directives", encoding: null, SourceHashAlgorithm.Sha1);
            var textAndVersion = TextAndVersion.Create(text, VersionStamp.Create());
            solution = UpdateSolution(mode, updateType, solution, documentId1, text, textAndVersion);

            // because we only forked one doc, the text/versions should be different in this interim solution.

            document1 = solution.GetRequiredDocument(documentId1);
            document2 = solution.GetRequiredDocument(documentId2);

            text1 = await document1.GetTextAsync();
            text2 = await document2.GetTextAsync();
            version1 = await document1.GetTextVersionAsync();
            version2 = await document2.GetTextVersionAsync();
            root1 = await document1.GetRequiredSyntaxRootAsync(CancellationToken.None);
            root2 = await document2.GetRequiredSyntaxRootAsync(CancellationToken.None);

            Assert.NotEqual(text1.ToString(), text2.ToString());
            Assert.NotEqual(version1, version2);

            // We get different red and green nodes entirely
            Assert.NotEqual(root1, root2);
            Assert.False(root1.IsIncrementallyIdenticalTo(root2));

            Assert.Equal(parseOptions1, root1.SyntaxTree.Options);
            Assert.Equal(parseOptions2, root2.SyntaxTree.Options);

            // Now apply the change to the workspace.  This should bring the linked document in sync with the one we changed.
            workspace.TryApplyChanges(solution);
            solution = workspace.CurrentSolution;

            document1 = solution.GetRequiredDocument(documentId1);
            document2 = solution.GetRequiredDocument(documentId2);

            text1 = await document1.GetTextAsync();
            text2 = await document2.GetTextAsync();
            version1 = await document1.GetTextVersionAsync();
            version2 = await document2.GetTextVersionAsync();
            root1 = await document1.GetRequiredSyntaxRootAsync(CancellationToken.None);
            root2 = await document2.GetRequiredSyntaxRootAsync(CancellationToken.None);

            Assert.Equal(text1.ToString(), text2.ToString());
            Assert.Equal(version1, version2);

            // We can reuse trees once they don't have conditional directives.
            Assert.NotEqual(root1, root2);
            Assert.True(root1.IsIncrementallyIdenticalTo(root2));

            Assert.Equal(parseOptions1, root1.SyntaxTree.Options);
            Assert.Equal(parseOptions2, root2.SyntaxTree.Options);
        }

        [Theory, CombinatorialData]
        public async Task WithDocumentText_LinkedFiles_PPConditionalDirective_DifferentParseOptions2(
            PreservationMode mode,
            TextUpdateType updateType)
        {
            using var workspace = CreateWorkspaceWithProjectAndLinkedDocuments("""
                #if NETSTANDARD
                public class Goo { }
                """, CSharpParseOptions.Default.WithPreprocessorSymbols("UNIQUE_NAME"), CSharpParseOptions.Default);
            var solution = workspace.CurrentSolution;

            var documentId1 = solution.Projects.First().DocumentIds.Single();
            var documentId2 = solution.Projects.Last().DocumentIds.Single();

            var document1 = solution.GetRequiredDocument(documentId1);
            var document2 = solution.GetRequiredDocument(documentId2);

            var text1 = await document1.GetTextAsync();
            var text2 = await document2.GetTextAsync();
            var version1 = await document1.GetTextVersionAsync();
            var version2 = await document2.GetTextVersionAsync();
            var root1 = await document1.GetRequiredSyntaxRootAsync(CancellationToken.None);
            var root2 = await document2.GetRequiredSyntaxRootAsync(CancellationToken.None);

            Assert.Equal(text1.ToString(), text2.ToString());
            Assert.Equal(version1, version2);

            // We can never reuse trees with conditional directives.
            Assert.NotEqual(root1, root2);
            Assert.False(root1.IsIncrementallyIdenticalTo(root2));

            // Because we still have pp directives, we'll still not be able to reuse the file.
            var text = SourceText.From("#if true", encoding: null, SourceHashAlgorithm.Sha1);
            var textAndVersion = TextAndVersion.Create(text, VersionStamp.Create());
            solution = UpdateSolution(mode, updateType, solution, documentId1, text, textAndVersion);

            // because we only forked one doc, the text/versions should be different in this interim solution.

            document1 = solution.GetRequiredDocument(documentId1);
            document2 = solution.GetRequiredDocument(documentId2);

            text1 = await document1.GetTextAsync();
            text2 = await document2.GetTextAsync();
            version1 = await document1.GetTextVersionAsync();
            version2 = await document2.GetTextVersionAsync();
            root1 = await document1.GetRequiredSyntaxRootAsync(CancellationToken.None);
            root2 = await document2.GetRequiredSyntaxRootAsync(CancellationToken.None);

            Assert.NotEqual(text1.ToString(), text2.ToString());
            Assert.NotEqual(version1, version2);

            // We get different red and green nodes entirely
            Assert.NotEqual(root1, root2);
            Assert.False(root1.IsIncrementallyIdenticalTo(root2));

            // Now apply the change to the workspace.  This should bring the linked document in sync with the one we changed.
            workspace.TryApplyChanges(solution);
            solution = workspace.CurrentSolution;

            document1 = solution.GetRequiredDocument(documentId1);
            document2 = solution.GetRequiredDocument(documentId2);

            text1 = await document1.GetTextAsync();
            text2 = await document2.GetTextAsync();
            version1 = await document1.GetTextVersionAsync();
            version2 = await document2.GetTextVersionAsync();
            root1 = await document1.GetRequiredSyntaxRootAsync(CancellationToken.None);
            root2 = await document2.GetRequiredSyntaxRootAsync(CancellationToken.None);

            Assert.Equal(text1.ToString(), text2.ToString());
            Assert.Equal(version1, version2);

            // We can never reuse trees with conditional directives.
            Assert.NotEqual(root1, root2);
            Assert.False(root1.IsIncrementallyIdenticalTo(root2));
        }

        [Theory, CombinatorialData]
        public async Task WithDocumentText_LinkedFiles_NonConditionalDirective(
            PreservationMode mode,
            TextUpdateType updateType)
        {
            using var workspace = CreateWorkspaceWithProjectAndLinkedDocuments("""
                #nullable enable // should not impact being able to reuse.
                public class Goo { }
                """);
            var solution = workspace.CurrentSolution;

            var documentId1 = solution.Projects.First().DocumentIds.Single();
            var documentId2 = solution.Projects.Last().DocumentIds.Single();

            var document1 = solution.GetRequiredDocument(documentId1);
            var document2 = solution.GetRequiredDocument(documentId2);

            var text1 = await document1.GetTextAsync();
            var text2 = await document2.GetTextAsync();
            var version1 = await document1.GetTextVersionAsync();
            var version2 = await document2.GetTextVersionAsync();
            var root1 = await document1.GetRequiredSyntaxRootAsync(CancellationToken.None);
            var root2 = await document2.GetRequiredSyntaxRootAsync(CancellationToken.None);

            Assert.Equal(text1.ToString(), text2.ToString());
            Assert.Equal(version1, version2);

            // We get different red nodes, but they should be backed by the same green nodes.
            Assert.NotEqual(root1, root2);
            Assert.True(root1.IsIncrementallyIdenticalTo(root2));

            var text = SourceText.From("new text", encoding: null, SourceHashAlgorithm.Sha1);
            var textAndVersion = TextAndVersion.Create(text, VersionStamp.Create());
            solution = UpdateSolution(mode, updateType, solution, documentId1, text, textAndVersion);

            // because we only forked one doc, the text/versions should be different in this interim solution.

            document1 = solution.GetRequiredDocument(documentId1);
            document2 = solution.GetRequiredDocument(documentId2);

            text1 = await document1.GetTextAsync();
            text2 = await document2.GetTextAsync();
            version1 = await document1.GetTextVersionAsync();
            version2 = await document2.GetTextVersionAsync();
            root1 = await document1.GetRequiredSyntaxRootAsync(CancellationToken.None);
            root2 = await document2.GetRequiredSyntaxRootAsync(CancellationToken.None);

            Assert.NotEqual(text1.ToString(), text2.ToString());
            Assert.NotEqual(version1, version2);

            // We get different red and green nodes.
            Assert.NotEqual(root1, root2);
            Assert.False(root1.IsIncrementallyIdenticalTo(root2));

            // Now apply the change to the workspace.  This should bring the linked document in sync with the one we changed.
            workspace.TryApplyChanges(solution);
            solution = workspace.CurrentSolution;

            document1 = solution.GetRequiredDocument(documentId1);
            document2 = solution.GetRequiredDocument(documentId2);

            text1 = await document1.GetTextAsync();
            text2 = await document2.GetTextAsync();
            version1 = await document1.GetTextVersionAsync();
            version2 = await document2.GetTextVersionAsync();
            root1 = await document1.GetRequiredSyntaxRootAsync(CancellationToken.None);
            root2 = await document2.GetRequiredSyntaxRootAsync(CancellationToken.None);

            Assert.Equal(text1.ToString(), text2.ToString());
            Assert.Equal(version1, version2);

            // We get different red nodes, but they should be backed by the same green nodes.
            Assert.NotEqual(root1, root2);
            Assert.True(root1.IsIncrementallyIdenticalTo(root2));
        }

        [Fact]
        public void WithAdditionalDocumentText_SourceText()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.Single().AdditionalDocumentIds.Single();
            var text = SourceText.From("new text");

            var newSolution1 = solution.WithAdditionalDocumentText(documentId, text, PreservationMode.PreserveIdentity);
            Assert.True(newSolution1.GetAdditionalDocument(documentId)!.TryGetText(out var actualText));
            Assert.Same(text, actualText);

            var newSolution2 = newSolution1.WithAdditionalDocumentText(documentId, text, PreservationMode.PreserveIdentity);
            Assert.Same(newSolution1, newSolution2);

            Assert.Throws<ArgumentNullException>(() => solution.WithAdditionalDocumentText(documentId, (SourceText)null!, PreservationMode.PreserveIdentity));
            Assert.Throws<ArgumentOutOfRangeException>(() => solution.WithAdditionalDocumentText(documentId, text, (PreservationMode)(-1)));

            Assert.Throws<ArgumentNullException>(() => solution.WithAdditionalDocumentText((DocumentId)null!, text, PreservationMode.PreserveIdentity));
            Assert.Throws<InvalidOperationException>(() => solution.WithAdditionalDocumentText(s_unrelatedDocumentId, text, PreservationMode.PreserveIdentity));
        }

        [Fact]
        public void WithAdditionalDocumentText_TextAndVersion()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.Single().AdditionalDocumentIds.Single();
            var textAndVersion = TextAndVersion.Create(SourceText.From("new text"), VersionStamp.Default);

            var newSolution1 = solution.WithAdditionalDocumentText(documentId, textAndVersion, PreservationMode.PreserveIdentity);
            Assert.True(newSolution1.GetAdditionalDocument(documentId)!.TryGetText(out var actualText));
            Assert.True(newSolution1.GetAdditionalDocument(documentId)!.TryGetTextVersion(out var actualVersion));
            Assert.Same(textAndVersion.Text, actualText);
            Assert.Equal(textAndVersion.Version, actualVersion);

            var newSolution2 = newSolution1.WithAdditionalDocumentText(documentId, textAndVersion, PreservationMode.PreserveIdentity);
            Assert.Same(newSolution1, newSolution2);

            Assert.Throws<ArgumentNullException>(() => solution.WithAdditionalDocumentText(documentId, (SourceText)null!, PreservationMode.PreserveIdentity));
            Assert.Throws<ArgumentOutOfRangeException>(() => solution.WithAdditionalDocumentText(documentId, textAndVersion, (PreservationMode)(-1)));

            Assert.Throws<ArgumentNullException>(() => solution.WithAdditionalDocumentText((DocumentId)null!, textAndVersion, PreservationMode.PreserveIdentity));
            Assert.Throws<InvalidOperationException>(() => solution.WithAdditionalDocumentText(s_unrelatedDocumentId, textAndVersion, PreservationMode.PreserveIdentity));
        }

        [Fact]
        public void WithAnalyzerConfigDocumentText_SourceText()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.Single().AnalyzerConfigDocumentIds.Single();
            var text = SourceText.From("new text");

            var newSolution1 = solution.WithAnalyzerConfigDocumentText(documentId, text, PreservationMode.PreserveIdentity);
            Assert.True(newSolution1.GetAnalyzerConfigDocument(documentId)!.TryGetText(out var actualText));
            Assert.Same(text, actualText);

            var newSolution2 = newSolution1.WithAnalyzerConfigDocumentText(documentId, text, PreservationMode.PreserveIdentity);
            Assert.Same(newSolution1, newSolution2);

            Assert.Throws<ArgumentNullException>(() => solution.WithAnalyzerConfigDocumentText(documentId, (SourceText)null!, PreservationMode.PreserveIdentity));
            Assert.Throws<ArgumentOutOfRangeException>(() => solution.WithAnalyzerConfigDocumentText(documentId, text, (PreservationMode)(-1)));

            Assert.Throws<ArgumentNullException>(() => solution.WithAnalyzerConfigDocumentText((DocumentId)null!, text, PreservationMode.PreserveIdentity));
            Assert.Throws<InvalidOperationException>(() => solution.WithAnalyzerConfigDocumentText(s_unrelatedDocumentId, text, PreservationMode.PreserveIdentity));
        }

        [Fact]
        public void WithAnalyzerConfigDocumentText_TextAndVersion()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.Single().AnalyzerConfigDocumentIds.Single();
            var textAndVersion = TextAndVersion.Create(SourceText.From("new text"), VersionStamp.Default);

            var newSolution1 = solution.WithAnalyzerConfigDocumentText(documentId, textAndVersion, PreservationMode.PreserveIdentity);
            Assert.True(newSolution1.GetAnalyzerConfigDocument(documentId)!.TryGetText(out var actualText));
            Assert.True(newSolution1.GetAnalyzerConfigDocument(documentId)!.TryGetTextVersion(out var actualVersion));
            Assert.Same(textAndVersion.Text, actualText);
            Assert.Equal(textAndVersion.Version, actualVersion);

            var newSolution2 = newSolution1.WithAnalyzerConfigDocumentText(documentId, textAndVersion, PreservationMode.PreserveIdentity);
            Assert.Same(newSolution1, newSolution2);

            Assert.Throws<ArgumentNullException>(() => solution.WithAnalyzerConfigDocumentText(documentId, (SourceText)null!, PreservationMode.PreserveIdentity));
            Assert.Throws<ArgumentOutOfRangeException>(() => solution.WithAnalyzerConfigDocumentText(documentId, textAndVersion, (PreservationMode)(-1)));

            Assert.Throws<ArgumentNullException>(() => solution.WithAnalyzerConfigDocumentText((DocumentId)null!, textAndVersion, PreservationMode.PreserveIdentity));
            Assert.Throws<InvalidOperationException>(() => solution.WithAnalyzerConfigDocumentText(s_unrelatedDocumentId, textAndVersion, PreservationMode.PreserveIdentity));
        }

        [Fact]
        public void WithDocumentTextLoader()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.Single().DocumentIds.Single();
            var loader = new TestTextLoader("new text");

            var newSolution1 = solution.WithDocumentTextLoader(documentId, loader, PreservationMode.PreserveIdentity);
            Assert.Equal("new text", newSolution1.GetDocument(documentId)!.GetTextSynchronously(CancellationToken.None).ToString());

            // Reusal is not currently implemented: https://github.com/dotnet/roslyn/issues/42028
            var newSolution2 = solution.WithDocumentTextLoader(documentId, loader, PreservationMode.PreserveIdentity);
            Assert.NotSame(newSolution1, newSolution2);

            Assert.Throws<ArgumentNullException>(() => solution.WithDocumentTextLoader(documentId, null!, PreservationMode.PreserveIdentity));
            Assert.Throws<ArgumentOutOfRangeException>(() => solution.WithDocumentTextLoader(documentId, loader, (PreservationMode)(-1)));

            Assert.Throws<ArgumentNullException>(() => solution.WithDocumentTextLoader(null!, loader, PreservationMode.PreserveIdentity));
            Assert.Throws<InvalidOperationException>(() => solution.WithDocumentTextLoader(s_unrelatedDocumentId, loader, PreservationMode.PreserveIdentity));
        }

        [Fact]
        public void WithAdditionalDocumentTextLoader()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.Single().AdditionalDocumentIds.Single();
            var loader = new TestTextLoader("new text");

            var newSolution1 = solution.WithAdditionalDocumentTextLoader(documentId, loader, PreservationMode.PreserveIdentity);
            Assert.Equal("new text", newSolution1.GetAdditionalDocument(documentId)!.GetTextSynchronously(CancellationToken.None).ToString());

            // Reusal is not currently implemented: https://github.com/dotnet/roslyn/issues/42028
            var newSolution2 = solution.WithAdditionalDocumentTextLoader(documentId, loader, PreservationMode.PreserveIdentity);
            Assert.NotSame(newSolution1, newSolution2);

            Assert.Throws<ArgumentNullException>(() => solution.WithAdditionalDocumentTextLoader(documentId, null!, PreservationMode.PreserveIdentity));
            Assert.Throws<ArgumentOutOfRangeException>(() => solution.WithAdditionalDocumentTextLoader(documentId, loader, (PreservationMode)(-1)));

            Assert.Throws<ArgumentNullException>(() => solution.WithAdditionalDocumentTextLoader(null!, loader, PreservationMode.PreserveIdentity));
            Assert.Throws<InvalidOperationException>(() => solution.WithAdditionalDocumentTextLoader(s_unrelatedDocumentId, loader, PreservationMode.PreserveIdentity));
        }

        [Fact]
        public void WithAnalyzerConfigDocumentTextLoader()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.Single().AnalyzerConfigDocumentIds.Single();
            var loader = new TestTextLoader("new text");

            var newSolution1 = solution.WithAnalyzerConfigDocumentTextLoader(documentId, loader, PreservationMode.PreserveIdentity);
            Assert.Equal("new text", newSolution1.GetAnalyzerConfigDocument(documentId)!.GetTextSynchronously(CancellationToken.None).ToString());

            // Reusal is not currently implemented: https://github.com/dotnet/roslyn/issues/42028
            var newSolution2 = solution.WithAnalyzerConfigDocumentTextLoader(documentId, loader, PreservationMode.PreserveIdentity);
            Assert.NotSame(newSolution1, newSolution2);

            Assert.Throws<ArgumentNullException>(() => solution.WithAnalyzerConfigDocumentTextLoader(documentId, null!, PreservationMode.PreserveIdentity));
            Assert.Throws<ArgumentOutOfRangeException>(() => solution.WithAnalyzerConfigDocumentTextLoader(documentId, loader, (PreservationMode)(-1)));

            Assert.Throws<ArgumentNullException>(() => solution.WithAnalyzerConfigDocumentTextLoader(null!, loader, PreservationMode.PreserveIdentity));
            Assert.Throws<InvalidOperationException>(() => solution.WithAnalyzerConfigDocumentTextLoader(s_unrelatedDocumentId, loader, PreservationMode.PreserveIdentity));
        }

        [Fact]
        public void WithProjectAssemblyName()
        {
            var projectId = ProjectId.CreateNewId();

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                .AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp);

            // any character is allowed
            var assemblyName = "\0<>a/b/*.dll";

            var newSolution = solution.WithProjectAssemblyName(projectId, assemblyName);
            Assert.Equal(assemblyName, newSolution.GetProject(projectId)!.AssemblyName);

            Assert.Same(newSolution, newSolution.WithProjectAssemblyName(projectId, assemblyName));

            Assert.Throws<ArgumentNullException>("assemblyName", () => solution.WithProjectAssemblyName(projectId, null!));
            Assert.Throws<ArgumentNullException>("projectId", () => solution.WithProjectAssemblyName(null!, "x.dll"));
            Assert.Throws<InvalidOperationException>(() => solution.WithProjectAssemblyName(ProjectId.CreateNewId(), "x.dll"));
        }

        [Fact]
        public void WithProjectOutputFilePath()
        {
            var projectId = ProjectId.CreateNewId();

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                .AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp);

            // any character is allowed
            var path = "\0<>a/b/*.dll";

            SolutionTestHelpers.TestProperty(
                solution,
                (s, value) => s.WithProjectOutputFilePath(projectId, value),
                s => s.GetRequiredProject(projectId).OutputFilePath,
                (string?)path,
                defaultThrows: false);

            Assert.Throws<ArgumentNullException>("projectId", () => solution.WithProjectOutputFilePath(null!, "x.dll"));
            Assert.Throws<InvalidOperationException>(() => solution.WithProjectOutputFilePath(ProjectId.CreateNewId(), "x.dll"));
        }

        [Fact]
        public void WithProjectOutputRefFilePath()
        {
            var projectId = ProjectId.CreateNewId();

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                            .AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp);

            // any character is allowed
            var path = "\0<>a/b/*.dll";

            SolutionTestHelpers.TestProperty(
                solution,
                (s, value) => s.WithProjectOutputRefFilePath(projectId, value),
                s => s.GetRequiredProject(projectId).OutputRefFilePath,
                (string?)path,
                defaultThrows: false);

            Assert.Throws<ArgumentNullException>("projectId", () => solution.WithProjectOutputRefFilePath(null!, "x.dll"));
            Assert.Throws<InvalidOperationException>(() => solution.WithProjectOutputRefFilePath(ProjectId.CreateNewId(), "x.dll"));
        }

        [Fact]
        public void WithProjectCompilationOutputInfo()
        {
            var projectId = ProjectId.CreateNewId();

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                            .AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp);

            // any character is allowed
            var path = "\0<>a/b/*.dll";

            SolutionTestHelpers.TestProperty(
                solution,
                (s, value) => s.WithProjectCompilationOutputInfo(projectId, value),
                s => s.GetRequiredProject(projectId).CompilationOutputInfo,
                new CompilationOutputInfo(path),
                defaultThrows: false);

            Assert.Throws<ArgumentNullException>("projectId", () => solution.WithProjectCompilationOutputInfo(null!, new CompilationOutputInfo("x.dll")));
            Assert.Throws<InvalidOperationException>(() => solution.WithProjectCompilationOutputInfo(ProjectId.CreateNewId(), new CompilationOutputInfo("x.dll")));
        }

        [Fact]
        public void WithProjectDefaultNamespace()
        {
            var projectId = ProjectId.CreateNewId();

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution.
                AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp);

            // any character is allowed
            var defaultNamespace = "\0<>a/b/*";

            SolutionTestHelpers.TestProperty(
                solution,
                (s, value) => s.WithProjectDefaultNamespace(projectId, value),
                s => s.GetRequiredProject(projectId).DefaultNamespace,
                (string?)defaultNamespace,
                defaultThrows: false);

            Assert.Throws<ArgumentNullException>("projectId", () => solution.WithProjectDefaultNamespace(null!, "x"));
            Assert.Throws<InvalidOperationException>(() => solution.WithProjectDefaultNamespace(ProjectId.CreateNewId(), "x"));
        }

        [Fact]
        public void WithProjectChecksumAlgorithm()
        {
            var projectId = ProjectId.CreateNewId();

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution.
                AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp);

            SolutionTestHelpers.TestProperty(
                solution,
                (s, value) => s.WithProjectChecksumAlgorithm(projectId, value),
                s => s.GetRequiredProject(projectId).State.ChecksumAlgorithm,
                SourceHashAlgorithms.Default,
                defaultThrows: false);
        }

        [Fact]
        public async Task WithProjectChecksumAlgorithm_DocumentUpdates()
        {
            var projectId = ProjectId.CreateNewId();
            var documentAId = DocumentId.CreateNewId(projectId);
            var documentBId = DocumentId.CreateNewId(projectId);
            var documentCId = DocumentId.CreateNewId(projectId);
            var fileDocumentId = DocumentId.CreateNewId(projectId);

            var fileD = Temp.CreateFile();
            var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes("Text");
            fileD.WriteAllBytes(bytes);

            var sha256 = SHA256.Create();
            var sha1 = SHA1.Create();
            var checksumSHA1 = sha1.ComputeHash(bytes);
            var checksumSHA256 = sha256.ComputeHash(bytes);
            sha256.Dispose();
            sha1.Dispose();

            using var workspace = CreateWorkspace();

            var textLoaderA = new TestTextLoader("class A {}", SourceHashAlgorithm.Sha1);
            var textC = SourceText.From("class C {}", encoding: null, checksumAlgorithm: SourceHashAlgorithm.Sha1);

            var solution = workspace.CurrentSolution
                .AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp)
                .WithProjectChecksumAlgorithm(projectId, SourceHashAlgorithm.Sha1);

            solution = solution.AddDocument(DocumentInfo.Create(documentAId, "a.cs", loader: textLoaderA, filePath: "a.cs"));
            solution = solution.AddDocument(documentBId, "b.cs", "class B {}", filePath: "b.cs");
            solution = solution.AddDocument(documentCId, "c.cs", textC, filePath: "c.cs");
            solution = solution.AddDocument(DocumentInfo.Create(fileDocumentId, "d.cs", loader: new FileTextLoader(fileD.Path, defaultEncoding: null), filePath: fileD.Path));

            await Verify(solution.GetRequiredDocument(documentAId), SourceHashAlgorithm.Sha1);
            await Verify(solution.GetRequiredDocument(documentBId), SourceHashAlgorithm.Sha1);
            await Verify(solution.GetRequiredDocument(documentCId), SourceHashAlgorithm.Sha1);
            await Verify(solution.GetRequiredDocument(fileDocumentId), SourceHashAlgorithm.Sha1, checksumSHA1);

            // only file loader based documents support updating checksum alg:
            solution = solution.WithProjectChecksumAlgorithm(projectId, SourceHashAlgorithm.Sha256);
            await Verify(solution.GetRequiredDocument(documentAId), SourceHashAlgorithm.Sha1);
            await Verify(solution.GetRequiredDocument(documentBId), SourceHashAlgorithm.Sha1);
            await Verify(solution.GetRequiredDocument(documentCId), SourceHashAlgorithm.Sha1);
            await Verify(solution.GetRequiredDocument(fileDocumentId), SourceHashAlgorithm.Sha256, checksumSHA256);

            // only file loader based documents support updating checksum alg:
            solution = solution.WithProjectChecksumAlgorithm(projectId, SourceHashAlgorithm.Sha1);
            await Verify(solution.GetRequiredDocument(documentAId), SourceHashAlgorithm.Sha1);
            await Verify(solution.GetRequiredDocument(documentBId), SourceHashAlgorithm.Sha1);
            await Verify(solution.GetRequiredDocument(documentCId), SourceHashAlgorithm.Sha1);
            await Verify(solution.GetRequiredDocument(fileDocumentId), SourceHashAlgorithm.Sha1, checksumSHA1);

            static async Task Verify(Document document, SourceHashAlgorithm expectedAlgorithm, byte[]? expectedChecksum = null)
            {
                Assert.Equal(expectedAlgorithm, document.State.LoadTextOptions.ChecksumAlgorithm);
                Assert.Equal(expectedAlgorithm, (await document.GetTextAsync(default)).ChecksumAlgorithm);
                Assert.Equal(expectedAlgorithm, document.GetTextSynchronously(default).ChecksumAlgorithm);
                Assert.Equal(expectedAlgorithm, (await document.GetRequiredSyntaxTreeAsync(default)).GetText().ChecksumAlgorithm);
                Assert.Equal(expectedAlgorithm, document.GetRequiredSyntaxTreeSynchronously(default).GetText().ChecksumAlgorithm);

                if (expectedChecksum != null)
                {
                    Assert.Equal(expectedChecksum, document.GetTextSynchronously(default).GetChecksum());
                }

                var compilation = await document.Project.GetRequiredCompilationAsync(default);
                var tree = compilation.SyntaxTrees.Single(t => t.FilePath == document.FilePath);
                Assert.Equal(expectedAlgorithm, (await tree.GetTextAsync(default)).ChecksumAlgorithm);
                Assert.Equal(expectedAlgorithm, tree.GetText(default).ChecksumAlgorithm);
            }
        }

        [Fact]
        public void WithProjectName()
        {
            var projectId = ProjectId.CreateNewId();

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                            .AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp);

            // any character is allowed
            var projectName = "\0<>a/b/*";

            SolutionTestHelpers.TestProperty(
                solution,
                (s, value) => s.WithProjectName(projectId, value),
                s => s.GetRequiredProject(projectId).Name,
                projectName,
                defaultThrows: true);

            Assert.Throws<ArgumentNullException>("projectId", () => solution.WithProjectName(null!, "x"));
            Assert.Throws<InvalidOperationException>(() => solution.WithProjectName(ProjectId.CreateNewId(), "x"));
        }

        [Fact]
        public void WithProjectFilePath()
        {
            var projectId = ProjectId.CreateNewId();

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                            .AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp);

            // any character is allowed
            var path = "\0<>a/b/*.csproj";

            SolutionTestHelpers.TestProperty(
                solution,
                (s, value) => s.WithProjectFilePath(projectId, value),
                s => s.GetRequiredProject(projectId).FilePath,
                (string?)path,
                defaultThrows: false);

            Assert.Throws<ArgumentNullException>("projectId", () => solution.WithProjectFilePath(null!, "x"));
            Assert.Throws<InvalidOperationException>(() => solution.WithProjectFilePath(ProjectId.CreateNewId(), "x"));
        }

        [Fact]
        public void WithProjectCompilationOptionsExceptionHandling()
        {
            var projectId = ProjectId.CreateNewId();

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                            .AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp);

            var options = new CSharpCompilationOptions(OutputKind.NetModule);

            Assert.Throws<ArgumentNullException>("projectId", () => solution.WithProjectCompilationOptions(null!, options));
            Assert.Throws<InvalidOperationException>(() => solution.WithProjectCompilationOptions(ProjectId.CreateNewId(), options));
        }

        [Theory]
        [CombinatorialData]
        public void WithProjectCompilationOptionsReplacesSyntaxTreeOptionProvider([CombinatorialValues(LanguageNames.CSharp, LanguageNames.VisualBasic)] string languageName)
        {
            var projectId = ProjectId.CreateNewId();

            using var workspace = CreateWorkspace();

            var solution = workspace.CurrentSolution
                .AddProject(projectId, "proj1", "proj1.dll", languageName);

            // We always have a non-null SyntaxTreeOptionsProvider for C# and VB projects
            var originalSyntaxTreeOptionsProvider = solution.Projects.Single().CompilationOptions!.SyntaxTreeOptionsProvider;
            Assert.NotNull(originalSyntaxTreeOptionsProvider);

            var defaultOptions = solution.Projects.Single().Services.GetRequiredService<ICompilationFactoryService>().GetDefaultCompilationOptions();
            Assert.Null(defaultOptions.SyntaxTreeOptionsProvider);

            solution = solution.WithProjectCompilationOptions(projectId, defaultOptions);

            // The CompilationOptions we replaced with didn't have a SyntaxTreeOptionsProvider, but we would have placed it
            // back. The SyntaxTreeOptionsProvider should behave the same as the prior one and thus should be equal.
            var newSyntaxTreeOptionsProvider = solution.Projects.Single().CompilationOptions!.SyntaxTreeOptionsProvider;
            Assert.NotNull(newSyntaxTreeOptionsProvider);
            Assert.Equal(originalSyntaxTreeOptionsProvider, newSyntaxTreeOptionsProvider);
        }

        [Fact]
        public void WithProjectParseOptions()
        {
            var projectId = ProjectId.CreateNewId();

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                            .AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp);

            var options = new CSharpParseOptions(CS.LanguageVersion.CSharp1);

            SolutionTestHelpers.TestProperty(
                solution,
                (s, value) => s.WithProjectParseOptions(projectId, value),
                s => s.GetRequiredProject(projectId).ParseOptions!,
                (ParseOptions)options,
                defaultThrows: true);

            Assert.Throws<ArgumentNullException>("projectId", () => solution.WithProjectParseOptions(null!, options));
            Assert.Throws<InvalidOperationException>(() => solution.WithProjectParseOptions(ProjectId.CreateNewId(), options));
        }

        [Fact]
        public async Task ChangingLanguageVersionReparses()
        {
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            using var workspace = CreateWorkspace();
            var document = workspace.CurrentSolution
                            .AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp)
                            .AddDocument(documentId, "Test.cs", "// File")
                            .GetRequiredDocument(documentId);

            var oldTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);

            Assert.Equal(document.Project.ParseOptions, oldTree.Options);

            document = document.Project.WithParseOptions(new CSharpParseOptions(languageVersion: CS.LanguageVersion.CSharp1)).GetRequiredDocument(documentId);

            var newTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);

            Assert.Equal(document.Project.ParseOptions, newTree.Options);

            Assert.False(oldTree.GetRoot().IsIncrementallyIdenticalTo(newTree.GetRoot()));
        }

        [Theory]
        [InlineData("#if DEBUG", false, LanguageNames.CSharp)]
        [InlineData(@"#region ""goo""", true, LanguageNames.CSharp)]
        [InlineData(@"#nullable enable", true, LanguageNames.CSharp)]
        [InlineData(@"#elif DEBUG", true, LanguageNames.CSharp)]
        [InlineData("// File", true, LanguageNames.CSharp)]
        [InlineData("#if DEBUG", false, LanguageNames.VisualBasic)]
        [InlineData(@"#region ""goo""", true, LanguageNames.VisualBasic)]
        [InlineData(@"#ElseIf DEBUG", true, LanguageNames.VisualBasic)]
        [InlineData("' File", true, LanguageNames.VisualBasic)]
        public async Task ChangingPreprocessorDirectivesMayReparse(string source, bool expectReuse, string languageName)
        {
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            using var workspace = CreateWorkspace();
            var document = workspace.CurrentSolution
                            .AddProject(projectId, "proj1", "proj1.dll", languageName)
                            .AddDocument(documentId, "Test", source)
                            .GetRequiredDocument(documentId);

            var oldTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);

            // Hold onto the old root, so we don't actually release the root; if the root were to fall away
            // we're unable to use IsIncrementallyIdenticalTo to see if we didn't reparse, since asking for
            // the old root will recover the tree and produce a new green node.
            var oldRoot = oldTree.GetRoot();

            Assert.Equal(document.Project.ParseOptions, oldTree.Options);

            ParseOptions newOptions = languageName == LanguageNames.CSharp
                ? new CSharpParseOptions(preprocessorSymbols: new[] { "DEBUG" })
                : new VisualBasicParseOptions(preprocessorSymbols: new KeyValuePair<string, object?>[] { new("DEBUG", null) });

            document = document.Project.WithParseOptions(newOptions).GetRequiredDocument(documentId);

            var newTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);

            Assert.Equal(document.Project.ParseOptions, newTree.Options);

            Assert.Equal(expectReuse, oldRoot.IsIncrementallyIdenticalTo(newTree.GetRoot()));
        }

        [Fact]
        public void WithProjectReferences()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var projectId = solution.Projects.Single().Id;

            var projectId2 = ProjectId.CreateNewId();
            solution = solution.AddProject(projectId2, "proj2", "proj2.dll", LanguageNames.CSharp);
            var projectRef = new ProjectReference(projectId2);

            SolutionTestHelpers.TestListProperty(solution,
                (old, value) => old.WithProjectReferences(projectId, value),
                opt => opt.GetProject(projectId)!.AllProjectReferences,
                projectRef,
                allowDuplicates: false);

            var projectRefs = (IEnumerable<ProjectReference>)ImmutableArray.Create(
                new ProjectReference(projectId2),
                new ProjectReference(projectId2, ImmutableArray.Create("alias")),
                new ProjectReference(projectId2, embedInteropTypes: true));

            var solution2 = solution.WithProjectReferences(projectId, projectRefs);
            Assert.Same(projectRefs, solution2.GetProject(projectId)!.AllProjectReferences);

            Assert.Throws<ArgumentNullException>("projectId", () => solution.WithProjectReferences(null!, new[] { projectRef }));
            Assert.Throws<InvalidOperationException>(() => solution.WithProjectReferences(ProjectId.CreateNewId(), new[] { projectRef }));

            // cycles:
            Assert.Throws<InvalidOperationException>(() => solution2.WithProjectReferences(projectId2, new[] { new ProjectReference(projectId) }));
            Assert.Throws<InvalidOperationException>(() => solution.WithProjectReferences(projectId, new[] { new ProjectReference(projectId) }));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42406")]
        public void WithProjectReferences_ProjectNotInSolution()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var projectId = solution.Projects.Single().Id;
            var externalProjectRef = new ProjectReference(ProjectId.CreateNewId());

            var projectRefs = (IEnumerable<ProjectReference>)ImmutableArray.Create(externalProjectRef);
            var newSolution1 = solution.WithProjectReferences(projectId, projectRefs);
            Assert.Same(projectRefs, newSolution1.GetProject(projectId)!.AllProjectReferences);

            // project reference is not included:
            Assert.Empty(newSolution1.GetProject(projectId)!.ProjectReferences);
        }

        [Fact]
        public void AddProjectReferences()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var projectId = solution.Projects.Single().Id;
            var projectId2 = ProjectId.CreateNewId();
            var projectId3 = ProjectId.CreateNewId();

            solution = solution
                .AddProject(projectId2, "proj2", "proj2.dll", LanguageNames.CSharp)
                .AddProject(projectId3, "proj3", "proj3.dll", LanguageNames.CSharp);

            var projectRef2 = new ProjectReference(projectId2);
            var projectRef3 = new ProjectReference(projectId3);
            var externalProjectRef = new ProjectReference(ProjectId.CreateNewId());

            solution = solution.AddProjectReference(projectId3, projectRef2);

            var solution2 = solution.AddProjectReferences(projectId, EmptyEnumerable<ProjectReference>());
            Assert.Same(solution, solution2);

            var e = OnceEnumerable(projectRef2, externalProjectRef);

            var solution3 = solution.AddProjectReferences(projectId, e);
            AssertEx.Equal(new[] { projectRef2 }, solution3.GetProject(projectId)!.ProjectReferences);
            AssertEx.Equal(new[] { projectRef2, externalProjectRef }, solution3.GetProject(projectId)!.AllProjectReferences);

            Assert.Throws<ArgumentNullException>("projectId", () => solution.AddProjectReferences(null!, new[] { projectRef2 }));
            Assert.Throws<ArgumentNullException>("projectReferences", () => solution.AddProjectReferences(projectId, null!));
            Assert.Throws<ArgumentNullException>("projectReferences[0]", () => solution.AddProjectReferences(projectId, new ProjectReference[] { null! }));
            Assert.Throws<ArgumentException>("projectReferences[1]", () => solution.AddProjectReferences(projectId, new[] { projectRef2, projectRef2 }));
            Assert.Throws<ArgumentException>("projectReferences[1]", () => solution.AddProjectReferences(projectId, new[] { new ProjectReference(projectId2), new ProjectReference(projectId2) }));

            // dup:
            Assert.Throws<InvalidOperationException>(() => solution.AddProjectReferences(projectId3, new[] { projectRef2 }));

            // cycles:
            Assert.Throws<InvalidOperationException>(() => solution3.AddProjectReferences(projectId2, new[] { projectRef3 }));
            Assert.Throws<InvalidOperationException>(() => solution3.AddProjectReferences(projectId, new[] { new ProjectReference(projectId) }));
        }

        [Fact]
        public void RemoveProjectReference()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var projectId = solution.Projects.Single().Id;

            var projectId2 = ProjectId.CreateNewId();
            solution = solution.AddProject(projectId2, "proj2", "proj2.dll", LanguageNames.CSharp);
            var projectRef2 = new ProjectReference(projectId2);
            var externalProjectRef = new ProjectReference(ProjectId.CreateNewId());

            solution = solution.WithProjectReferences(projectId, new[] { projectRef2, externalProjectRef });

            // remove reference to a project that's not part of the solution:
            var solution2 = solution.RemoveProjectReference(projectId, externalProjectRef);
            AssertEx.Equal(new[] { projectRef2 }, solution2.GetProject(projectId)!.AllProjectReferences);

            // remove reference to a project that's part of the solution:
            var solution3 = solution.RemoveProjectReference(projectId, projectRef2);
            AssertEx.Equal(new[] { externalProjectRef }, solution3.GetProject(projectId)!.AllProjectReferences);

            var solution4 = solution3.RemoveProjectReference(projectId, externalProjectRef);
            Assert.Empty(solution4.GetProject(projectId)!.AllProjectReferences);

            Assert.Throws<ArgumentNullException>("projectId", () => solution.RemoveProjectReference(null!, projectRef2));
            Assert.Throws<ArgumentNullException>("projectReference", () => solution.RemoveProjectReference(projectId, null!));

            // removing a reference that's not in the list:
            Assert.Throws<ArgumentException>("projectReference", () => solution.RemoveProjectReference(projectId, new ProjectReference(ProjectId.CreateNewId())));

            // project not in solution:
            Assert.Throws<InvalidOperationException>(() => solution.RemoveProjectReference(ProjectId.CreateNewId(), projectRef2));
        }

        [Fact]
        public void ProjectReferences_Submissions()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var projectId0 = ProjectId.CreateNewId();
            var submissionId1 = ProjectId.CreateNewId();
            var submissionId2 = ProjectId.CreateNewId();
            var submissionId3 = ProjectId.CreateNewId();

            solution = solution
                .AddProject(projectId0, "non-submission", "non-submission.dll", LanguageNames.CSharp)
                .AddProject(ProjectInfo.Create(submissionId1, VersionStamp.Default, name: "submission1", assemblyName: "submission1.dll", LanguageNames.CSharp, isSubmission: true))
                .AddProject(ProjectInfo.Create(submissionId2, VersionStamp.Default, name: "submission2", assemblyName: "submission2.dll", LanguageNames.CSharp, isSubmission: true))
                .AddProject(ProjectInfo.Create(submissionId3, VersionStamp.Default, name: "submission3", assemblyName: "submission3.dll", LanguageNames.CSharp, isSubmission: true))
                .AddProjectReference(submissionId2, new ProjectReference(submissionId1))
                .WithProjectReferences(submissionId2, new[] { new ProjectReference(submissionId1) });

            // submission may be referenced from multiple submissions (forming a tree):
            _ = solution.AddProjectReferences(submissionId3, new[] { new ProjectReference(submissionId1) });
            _ = solution.WithProjectReferences(submissionId3, new[] { new ProjectReference(submissionId1) });

            // submission may reference a non-submission project:
            _ = solution.AddProjectReferences(submissionId3, new[] { new ProjectReference(projectId0) });
            _ = solution.WithProjectReferences(submissionId3, new[] { new ProjectReference(projectId0) });

            // submission can't reference multiple submissions:
            Assert.Throws<InvalidOperationException>(() => solution.AddProjectReferences(submissionId2, new[] { new ProjectReference(submissionId3) }));
            Assert.Throws<InvalidOperationException>(() => solution.WithProjectReferences(submissionId1, new[] { new ProjectReference(submissionId2), new ProjectReference(submissionId3) }));

            // non-submission project can't reference a submission:
            Assert.Throws<InvalidOperationException>(() => solution.AddProjectReferences(projectId0, new[] { new ProjectReference(submissionId1) }));
            Assert.Throws<InvalidOperationException>(() => solution.WithProjectReferences(projectId0, new[] { new ProjectReference(submissionId1) }));
        }

        [Fact]
        public void WithProjectMetadataReferences()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var projectId = solution.Projects.Single().Id;
            var metadataRef = (MetadataReference)new TestMetadataReference();

            SolutionTestHelpers.TestListProperty(solution,
                (old, value) => old.WithProjectMetadataReferences(projectId, value),
                opt => opt.GetProject(projectId)!.MetadataReferences,
                metadataRef,
                allowDuplicates: false);

            Assert.Throws<ArgumentNullException>("projectId", () => solution.WithProjectMetadataReferences(null!, new[] { metadataRef }));
            Assert.Throws<InvalidOperationException>(() => solution.WithProjectMetadataReferences(ProjectId.CreateNewId(), new[] { metadataRef }));
        }

        [Fact]
        public void AddMetadataReferences()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var projectId = solution.Projects.Single().Id;

            var solution2 = solution.AddMetadataReferences(projectId, EmptyEnumerable<MetadataReference>());
            Assert.Same(solution, solution2);

            var metadataRef1 = new TestMetadataReference();
            var metadataRef2 = new TestMetadataReference();

            var solution3 = solution.AddMetadataReferences(projectId, OnceEnumerable(metadataRef1, metadataRef2));
            AssertEx.Equal(new[] { metadataRef1, metadataRef2 }, solution3.GetProject(projectId)!.MetadataReferences);

            Assert.Throws<ArgumentNullException>("projectId", () => solution.AddMetadataReferences(null!, new[] { metadataRef1 }));
            Assert.Throws<ArgumentNullException>("metadataReferences", () => solution.AddMetadataReferences(projectId, null!));
            Assert.Throws<ArgumentNullException>("metadataReferences[0]", () => solution.AddMetadataReferences(projectId, new MetadataReference[] { null! }));
            Assert.Throws<ArgumentException>("metadataReferences[1]", () => solution.AddMetadataReferences(projectId, new[] { metadataRef1, metadataRef1 }));

            // dup:
            Assert.Throws<InvalidOperationException>(() => solution3.AddMetadataReferences(projectId, new[] { metadataRef1 }));
        }

        [Fact]
        public void RemoveMetadataReference()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var projectId = solution.Projects.Single().Id;
            var metadataRef1 = new TestMetadataReference();
            var metadataRef2 = new TestMetadataReference();

            solution = solution.WithProjectMetadataReferences(projectId, new[] { metadataRef1, metadataRef2 });

            var solution2 = solution.RemoveMetadataReference(projectId, metadataRef1);
            AssertEx.Equal(new[] { metadataRef2 }, solution2.GetProject(projectId)!.MetadataReferences);

            var solution3 = solution2.RemoveMetadataReference(projectId, metadataRef2);
            Assert.Empty(solution3.GetProject(projectId)!.MetadataReferences);

            Assert.Throws<ArgumentNullException>("projectId", () => solution.RemoveMetadataReference(null!, metadataRef1));
            Assert.Throws<ArgumentNullException>("metadataReference", () => solution.RemoveMetadataReference(projectId, null!));

            // removing a reference that's not in the list:
            Assert.Throws<InvalidOperationException>(() => solution.RemoveMetadataReference(projectId, new TestMetadataReference()));

            // project not in solution:
            Assert.Throws<InvalidOperationException>(() => solution.RemoveMetadataReference(ProjectId.CreateNewId(), metadataRef1));
        }

        [Fact]
        public void WithProjectAnalyzerReferences()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var projectId = solution.Projects.Single().Id;
            var analyzerRef = (AnalyzerReference)new TestAnalyzerReference();

            SolutionTestHelpers.TestListProperty(solution,
                (old, value) => old.WithProjectAnalyzerReferences(projectId, value),
                opt => opt.GetProject(projectId)!.AnalyzerReferences,
                analyzerRef,
                allowDuplicates: false);

            Assert.Throws<ArgumentNullException>("projectId", () => solution.WithProjectAnalyzerReferences(null!, new[] { analyzerRef }));
            Assert.Throws<InvalidOperationException>(() => solution.WithProjectAnalyzerReferences(ProjectId.CreateNewId(), new[] { analyzerRef }));
        }

        [Fact]
        public void AddAnalyzerReferences_Project()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var projectId = solution.Projects.Single().Id;

            var solution2 = solution.AddAnalyzerReferences(projectId, EmptyEnumerable<AnalyzerReference>());
            Assert.Same(solution, solution2);

            var analyzerRef1 = new TestAnalyzerReference();
            var analyzerRef2 = new TestAnalyzerReference();

            var solution3 = solution.AddAnalyzerReferences(projectId, OnceEnumerable(analyzerRef1, analyzerRef2));
            AssertEx.Equal(new[] { analyzerRef1, analyzerRef2 }, solution3.GetProject(projectId)!.AnalyzerReferences);

            var solution4 = solution3.AddAnalyzerReferences(projectId, new AnalyzerReference[0]);

            Assert.Same(solution, solution2);
            Assert.Throws<ArgumentNullException>("projectId", () => solution.AddAnalyzerReferences(null!, new[] { analyzerRef1 }));
            Assert.Throws<ArgumentNullException>("analyzerReferences", () => solution.AddAnalyzerReferences(projectId, null!));
            Assert.Throws<ArgumentNullException>("analyzerReferences[0]", () => solution.AddAnalyzerReferences(projectId, new AnalyzerReference[] { null! }));
            Assert.Throws<ArgumentException>("analyzerReferences[1]", () => solution.AddAnalyzerReferences(projectId, new[] { analyzerRef1, analyzerRef1 }));

            // dup:
            Assert.Throws<InvalidOperationException>(() => solution3.AddAnalyzerReferences(projectId, new[] { analyzerRef1 }));
        }

        [Fact]
        public void RemoveAnalyzerReference_Project()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var projectId = solution.Projects.Single().Id;
            var analyzerRef1 = new TestAnalyzerReference();
            var analyzerRef2 = new TestAnalyzerReference();

            solution = solution.WithProjectAnalyzerReferences(projectId, new[] { analyzerRef1, analyzerRef2 });

            var solution2 = solution.RemoveAnalyzerReference(projectId, analyzerRef1);
            AssertEx.Equal(new[] { analyzerRef2 }, solution2.GetProject(projectId)!.AnalyzerReferences);

            var solution3 = solution2.RemoveAnalyzerReference(projectId, analyzerRef2);
            Assert.Empty(solution3.GetProject(projectId)!.AnalyzerReferences);

            Assert.Throws<ArgumentNullException>("projectId", () => solution.RemoveAnalyzerReference(null!, analyzerRef1));
            Assert.Throws<ArgumentNullException>("analyzerReference", () => solution.RemoveAnalyzerReference(projectId, null!));

            // removing a reference that's not in the list:
            Assert.Throws<InvalidOperationException>(() => solution.RemoveAnalyzerReference(projectId, new TestAnalyzerReference()));

            // project not in solution:
            Assert.Throws<InvalidOperationException>(() => solution.RemoveAnalyzerReference(ProjectId.CreateNewId(), analyzerRef1));
        }

        [Fact]
        public void WithAnalyzerReferences()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var analyzerRef = (AnalyzerReference)new TestAnalyzerReference();

            SolutionTestHelpers.TestListProperty(solution,
                (old, value) => old.WithAnalyzerReferences(value),
                opt => opt.AnalyzerReferences,
                analyzerRef,
                allowDuplicates: false);
        }

        [Fact]
        public void AddAnalyzerReferences()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;

            var solution2 = solution.AddAnalyzerReferences(EmptyEnumerable<AnalyzerReference>());
            Assert.Same(solution, solution2);

            var analyzerRef1 = new TestAnalyzerReference();
            var analyzerRef2 = new TestAnalyzerReference();

            var solution3 = solution.AddAnalyzerReferences(OnceEnumerable(analyzerRef1, analyzerRef2));
            AssertEx.Equal(new[] { analyzerRef1, analyzerRef2 }, solution3.AnalyzerReferences);

            var solution4 = solution3.AddAnalyzerReferences(new AnalyzerReference[0]);

            Assert.Same(solution, solution2);
            Assert.Throws<ArgumentNullException>("analyzerReferences", () => solution.AddAnalyzerReferences(null!));
            Assert.Throws<ArgumentNullException>("analyzerReferences[0]", () => solution.AddAnalyzerReferences(new AnalyzerReference[] { null! }));
            Assert.Throws<ArgumentException>("analyzerReferences[1]", () => solution.AddAnalyzerReferences(new[] { analyzerRef1, analyzerRef1 }));

            // dup:
            Assert.Throws<InvalidOperationException>(() => solution3.AddAnalyzerReferences(new[] { analyzerRef1 }));
        }

        [Fact]
        public void RemoveAnalyzerReference()
        {
            using var workspace = CreateWorkspaceWithProjectAndDocuments();
            var solution = workspace.CurrentSolution;
            var analyzerRef1 = new TestAnalyzerReference();
            var analyzerRef2 = new TestAnalyzerReference();

            solution = solution.WithAnalyzerReferences(new[] { analyzerRef1, analyzerRef2 });

            var solution2 = solution.RemoveAnalyzerReference(analyzerRef1);
            AssertEx.Equal(new[] { analyzerRef2 }, solution2.AnalyzerReferences);

            var solution3 = solution2.RemoveAnalyzerReference(analyzerRef2);
            Assert.Empty(solution3.AnalyzerReferences);

            Assert.Throws<ArgumentNullException>("analyzerReference", () => solution.RemoveAnalyzerReference(null!));

            // removing a reference that's not in the list:
            Assert.Throws<InvalidOperationException>(() => solution.RemoveAnalyzerReference(new TestAnalyzerReference()));
        }

        [Fact]
        public void AddDocument_Loader()
        {
            var projectId = ProjectId.CreateNewId();
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution.AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp).
                WithProjectChecksumAlgorithm(projectId, SourceHashAlgorithms.Default).
                WithProjectParseOptions(projectId, new CSharpParseOptions(kind: SourceCodeKind.Script));

            var loader = new TestTextLoader(checksumAlgorithm: SourceHashAlgorithm.Sha1);
            var documentId = DocumentId.CreateNewId(projectId);
            var folders = new[] { "folder1", "folder2" };

            var solution2 = solution.AddDocument(documentId, "name", loader, folders);
            var document = solution2.GetRequiredDocument(documentId);
            AssertEx.Equal(folders, document.Folders);
            Assert.Equal(SourceCodeKind.Script, document.SourceCodeKind);
            Assert.Equal(SourceHashAlgorithm.Sha1, document.GetTextSynchronously(default).ChecksumAlgorithm);

            Assert.Throws<ArgumentNullException>("documentId", () => solution.AddDocument(documentId: null!, "name", loader));
            Assert.Throws<ArgumentNullException>("name", () => solution.AddDocument(documentId, name: null!, loader));
            Assert.Throws<ArgumentNullException>("loader", () => solution.AddDocument(documentId, "name", loader: null!));
            Assert.Throws<InvalidOperationException>(() => solution.AddDocument(documentId: DocumentId.CreateNewId(ProjectId.CreateNewId()), "name", loader));
        }

        [Fact]
        public void AddDocument_Text()
        {
            var projectId = ProjectId.CreateNewId();
            using var workspace = CreateWorkspace();

            var solution = workspace.CurrentSolution.AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp).
                WithProjectChecksumAlgorithm(projectId, SourceHashAlgorithms.Default).
                WithProjectParseOptions(projectId, new CSharpParseOptions(kind: SourceCodeKind.Script));

            var documentId = DocumentId.CreateNewId(projectId);
            var filePath = Path.Combine(TempRoot.Root, "x.cs");
            var folders = new[] { "folder1", "folder2" };

            var solution2 = solution.AddDocument(documentId, "name", "text", folders, filePath);
            var document = solution2.GetRequiredDocument(documentId);
            var sourceText = document.GetTextSynchronously(default);

            Assert.Equal("text", sourceText.ToString());
            Assert.Equal(SourceHashAlgorithms.Default, sourceText.ChecksumAlgorithm);
            AssertEx.Equal(folders, document.Folders);
            Assert.Equal(filePath, document.FilePath);
            Assert.False(document.State.Attributes.IsGenerated);
            Assert.Equal(SourceCodeKind.Script, document.SourceCodeKind);

            Assert.Throws<ArgumentNullException>("documentId", () => solution.AddDocument(documentId: null!, "name", "text"));
            Assert.Throws<ArgumentNullException>("name", () => solution.AddDocument(documentId, name: null!, "text"));
            Assert.Throws<ArgumentNullException>("text", () => solution.AddDocument(documentId, "name", text: (string)null!));
            Assert.Throws<InvalidOperationException>(() => solution.AddDocument(documentId: DocumentId.CreateNewId(ProjectId.CreateNewId()), "name", "text"));
        }

        [Fact]
        public void AddDocument_SourceText()
        {
            var projectId = ProjectId.CreateNewId();
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution.AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp).
                WithProjectChecksumAlgorithm(projectId, SourceHashAlgorithms.Default).
                WithProjectParseOptions(projectId, new CSharpParseOptions(kind: SourceCodeKind.Script));

            var documentId = DocumentId.CreateNewId(projectId);
            var sourceText = SourceText.From("text", checksumAlgorithm: SourceHashAlgorithms.Default);
            var filePath = Path.Combine(TempRoot.Root, "x.cs");
            var folders = new[] { "folder1", "folder2" };

            var solution2 = solution.AddDocument(documentId, "name", sourceText, folders, filePath, isGenerated: true);
            var document = solution2.GetRequiredDocument(documentId);

            AssertEx.Equal(folders, document.Folders);
            Assert.Equal(filePath, document.FilePath);
            Assert.True(document.State.Attributes.IsGenerated);
            Assert.Equal(SourceCodeKind.Script, document.SourceCodeKind);

            Assert.Throws<ArgumentNullException>("documentId", () => solution.AddDocument(documentId: null!, "name", sourceText));
            Assert.Throws<ArgumentNullException>("name", () => solution.AddDocument(documentId, name: null!, sourceText));
            Assert.Throws<ArgumentNullException>("text", () => solution.AddDocument(documentId, "name", text: (SourceText)null!));
            Assert.Throws<InvalidOperationException>(() => solution.AddDocument(documentId: DocumentId.CreateNewId(ProjectId.CreateNewId()), "name", sourceText));
        }

        [Fact]
        public void AddDocument_SyntaxRoot()
        {
            var projectId = ProjectId.CreateNewId();
            using var workspace = CreateWorkspace();

            var solution = workspace.CurrentSolution.AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp).
                WithProjectChecksumAlgorithm(projectId, SourceHashAlgorithms.Default).
                WithProjectParseOptions(projectId, new CSharpParseOptions(kind: SourceCodeKind.Script));

            var documentId = DocumentId.CreateNewId(projectId);
            var filePath = Path.Combine(TempRoot.Root, "x.cs");
            var folders = new[] { "folder1", "folder2" };

            var root = CSharp.SyntaxFactory.ParseCompilationUnit("class C {}");
            var solution2 = solution.AddDocument(documentId, "name", root, folders, filePath);
            var document2 = solution2.GetRequiredDocument(documentId);

            AssertEx.Equal(folders, document2.Folders);
            Assert.Equal(filePath, document2.FilePath);
            Assert.False(document2.State.Attributes.IsGenerated);
            Assert.Equal(SourceCodeKind.Script, document2.SourceCodeKind);

            Assert.Throws<ArgumentNullException>("documentId", () => solution.AddDocument(documentId: null!, "name", root));
            Assert.Throws<ArgumentNullException>("name", () => solution.AddDocument(documentId, name: null!, root));
            Assert.Throws<ArgumentNullException>("syntaxRoot", () => solution.AddDocument(documentId, "name", syntaxRoot: null!));
            Assert.Throws<InvalidOperationException>(() => solution.AddDocument(documentId: DocumentId.CreateNewId(ProjectId.CreateNewId()), "name", syntaxRoot: root));
        }

        [Fact]
        public void AddDocument_SyntaxRoot_ExplicitTree()
        {
            var projectId = ProjectId.CreateNewId();
            using var workspace = CreateWorkspace();

            var solution = workspace.CurrentSolution.AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp).
                WithProjectChecksumAlgorithm(projectId, SourceHashAlgorithms.Default).
                WithProjectParseOptions(projectId, new CSharpParseOptions(kind: SourceCodeKind.Script));

            var documentId = DocumentId.CreateNewId(projectId);
            var filePath = Path.Combine(TempRoot.Root, "x.cs");
            var folders = new[] { "folder1", "folder2" };

            var root = CSharp.SyntaxFactory.ParseSyntaxTree(SourceText.From("class C {}", encoding: null, SourceHashAlgorithm.Sha1)).GetRoot();
            Assert.Equal(SourceHashAlgorithm.Sha1, root.SyntaxTree.GetText().ChecksumAlgorithm);

            var solution2 = solution.AddDocument(documentId, "name", root, folders, filePath);
            var document2 = solution2.GetRequiredDocument(documentId);
            var sourceText = document2.GetTextSynchronously(default);
            Assert.Equal("class C {}", sourceText.ToString());

            // the checksum algorithm of the tree is ignored, instead the one set on the project is used:
            Assert.Equal(SourceHashAlgorithms.Default, sourceText.ChecksumAlgorithm);

            AssertEx.Equal(folders, document2.Folders);
            Assert.Equal(filePath, document2.FilePath);
            Assert.False(document2.State.Attributes.IsGenerated);
            Assert.Equal(SourceCodeKind.Script, document2.SourceCodeKind);
        }

        [Fact]
        public void AddDocument_SyntaxRoot_SynthesizedTree()
        {
            var projectId = ProjectId.CreateNewId();
            using var workspace = CreateWorkspace();

            var solution = workspace.CurrentSolution.AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp).
                WithProjectChecksumAlgorithm(projectId, SourceHashAlgorithms.Default).
                WithProjectParseOptions(projectId, new CSharpParseOptions(kind: SourceCodeKind.Script));

            var documentId = DocumentId.CreateNewId(projectId);

            var root = CSharp.SyntaxFactory.ParseCompilationUnit("class C {}");
            Assert.Equal(SourceHashAlgorithm.Sha1, root.SyntaxTree.GetText().ChecksumAlgorithm);

            var solution2 = solution.AddDocument(documentId, "name", root);
            var document2 = solution2.GetRequiredDocument(documentId);
            var sourceText = document2.GetTextSynchronously(default);
            Assert.Equal("class C {}", sourceText.ToString());

            // the checksum algorithm of the tree is ignored, instead the one set on the project is used:
            Assert.Equal(SourceHashAlgorithms.Default, sourceText.ChecksumAlgorithm);
        }

#nullable disable
        [Fact]
        public void TestAddProject()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            var pid = ProjectId.CreateNewId();
            solution = solution.AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp);
            Assert.True(solution.ProjectIds.Any(), "Solution was expected to have projects");
            Assert.NotNull(pid);
            var project = solution.GetProject(pid);
            Assert.False(project.HasDocuments);
        }

        [Fact]
        public void TestUpdateAssemblyName()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            var project1 = ProjectId.CreateNewId();
            solution = solution.AddProject(project1, "goo", "goo.dll", LanguageNames.CSharp);
            solution = solution.WithProjectAssemblyName(project1, "bar");
            var project = solution.GetProject(project1);
            Assert.Equal("bar", project.AssemblyName);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543964")]
        public void MultipleProjectsWithSameDisplayName()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            var project1 = ProjectId.CreateNewId();
            var project2 = ProjectId.CreateNewId();
            solution = solution.AddProject(project1, "name", "assemblyName", LanguageNames.CSharp);
            solution = solution.AddProject(project2, "name", "assemblyName", LanguageNames.CSharp);
            Assert.Equal(2, solution.GetProjectsByName("name").Count());
        }

        [Fact]
        public async Task TestAddFirstDocumentAsync()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                .AddDocument(did, "goo.cs", "public class Goo { }");

            // verify project & document
            Assert.NotNull(pid);
            var project = solution.GetProject(pid);
            Assert.NotNull(project);
            Assert.True(solution.ContainsProject(pid), "Solution was expected to have project " + pid);
            Assert.True(project.HasDocuments, "Project was expected to have documents");
            Assert.Equal(project, solution.GetProject(pid));
            Assert.NotNull(did);
            var document = solution.GetDocument(did);
            Assert.True(project.ContainsDocument(did), "Project was expected to have document " + did);
            Assert.Equal(document, project.GetDocument(did));
            Assert.Equal(document, solution.GetDocument(did));
            var semantics = await document.GetSemanticModelAsync();
            Assert.NotNull(semantics);

            await ValidateSolutionAndCompilationsAsync(solution);

            var pid2 = solution.Projects.Single().Id;
            var did2 = DocumentId.CreateNewId(pid2);
            solution = solution.AddDocument(did2, "bar.cs", "public class Bar { }");

            // verify project & document
            var project2 = solution.GetProject(pid2);
            Assert.NotNull(project2);
            Assert.NotNull(did2);
            var document2 = solution.GetDocument(did2);
            Assert.True(project2.ContainsDocument(did2), "Project was expected to have document " + did2);
            Assert.Equal(document2, project2.GetDocument(did2));
            Assert.Equal(document2, solution.GetDocument(did2));

            await ValidateSolutionAndCompilationsAsync(solution);
        }

        [Fact]
        public async Task AddTwoDocumentsForSingleProject()
        {
            var projectId = ProjectId.CreateNewId();

            var documentInfo1 = DocumentInfo.Create(DocumentId.CreateNewId(projectId), "file1.cs");
            var documentInfo2 = DocumentInfo.Create(DocumentId.CreateNewId(projectId), "file2.cs");

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                            .AddProject(projectId, "goo", "goo.dll", LanguageNames.CSharp)
                            .AddDocuments(ImmutableArray.Create(documentInfo1, documentInfo2));

            var project = Assert.Single(solution.Projects);

            var document1 = project.GetDocument(documentInfo1.Id);
            var document2 = project.GetDocument(documentInfo2.Id);

            Assert.NotSame(document1, document2);

            await ValidateSolutionAndCompilationsAsync(solution);
        }

        [Fact]
        public async Task AddTwoDocumentsForTwoProjects()
        {
            var projectId1 = ProjectId.CreateNewId();
            var projectId2 = ProjectId.CreateNewId();

            var documentInfo1 = DocumentInfo.Create(DocumentId.CreateNewId(projectId1), "file1.cs");
            var documentInfo2 = DocumentInfo.Create(DocumentId.CreateNewId(projectId2), "file2.cs");

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                            .AddProject(projectId1, "project1", "project1.dll", LanguageNames.CSharp)
                            .AddProject(projectId2, "project2", "project2.dll", LanguageNames.CSharp)
                            .AddDocuments(ImmutableArray.Create(documentInfo1, documentInfo2));

            var project1 = solution.GetProject(projectId1);
            var project2 = solution.GetProject(projectId2);

            var document1 = project1.GetDocument(documentInfo1.Id);
            var document2 = project2.GetDocument(documentInfo2.Id);

            Assert.NotSame(document1, document2);
            Assert.NotSame(document1.Project, document2.Project);

            await ValidateSolutionAndCompilationsAsync(solution);
        }

        [Fact]
        public void AddTwoDocumentsWithMissingProject()
        {
            var projectId1 = ProjectId.CreateNewId();
            var projectId2 = ProjectId.CreateNewId();

            var documentInfo1 = DocumentInfo.Create(DocumentId.CreateNewId(projectId1), "file1.cs");
            var documentInfo2 = DocumentInfo.Create(DocumentId.CreateNewId(projectId2), "file2.cs");

            // We're only adding the first project, but not the second one
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                            .AddProject(projectId1, "project1", "project1.dll", LanguageNames.CSharp);

            Assert.ThrowsAny<InvalidOperationException>(() => solution.AddDocuments(ImmutableArray.Create(documentInfo1, documentInfo2)));
        }

        [Fact]
        public void RemoveZeroDocuments()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            Assert.Same(solution, solution.RemoveDocuments(ImmutableArray<DocumentId>.Empty));
        }

        [Fact]
        public async Task RemoveTwoDocuments()
        {
            var projectId = ProjectId.CreateNewId();

            var documentInfo1 = DocumentInfo.Create(DocumentId.CreateNewId(projectId), "file1.cs");
            var documentInfo2 = DocumentInfo.Create(DocumentId.CreateNewId(projectId), "file2.cs");

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                            .AddProject(projectId, "project1", "project1.dll", LanguageNames.CSharp)
                            .AddDocuments(ImmutableArray.Create(documentInfo1, documentInfo2));

            solution = solution.RemoveDocuments(ImmutableArray.Create(documentInfo1.Id, documentInfo2.Id));

            var finalProject = solution.Projects.Single();
            Assert.Empty(finalProject.Documents);
            Assert.Empty((await finalProject.GetCompilationAsync()).SyntaxTrees);
        }

        [Fact]
        public void RemoveTwoDocumentsFromDifferentProjects()
        {
            var projectId1 = ProjectId.CreateNewId();
            var projectId2 = ProjectId.CreateNewId();

            var documentInfo1 = DocumentInfo.Create(DocumentId.CreateNewId(projectId1), "file1.cs");
            var documentInfo2 = DocumentInfo.Create(DocumentId.CreateNewId(projectId2), "file2.cs");

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                            .AddProject(projectId1, "project1", "project1.dll", LanguageNames.CSharp)
                            .AddProject(projectId2, "project2", "project2.dll", LanguageNames.CSharp)
                            .AddDocuments(ImmutableArray.Create(documentInfo1, documentInfo2));

            Assert.All(solution.Projects, p => Assert.Single(p.Documents));

            solution = solution.RemoveDocuments(ImmutableArray.Create(documentInfo1.Id, documentInfo2.Id));

            Assert.All(solution.Projects, p => Assert.Empty(p.Documents));
        }

        [Fact]
        public void RemoveDocumentFromUnrelatedProject()
        {
            var projectId1 = ProjectId.CreateNewId();
            var projectId2 = ProjectId.CreateNewId();

            var documentInfo1 = DocumentInfo.Create(DocumentId.CreateNewId(projectId1), "file1.cs");

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                            .AddProject(projectId1, "project1", "project1.dll", LanguageNames.CSharp)
                            .AddProject(projectId2, "project2", "project2.dll", LanguageNames.CSharp)
                            .AddDocument(documentInfo1);

            // This should throw if we're removing one document from the wrong project. Right now we don't test the RemoveDocument
            // API due to https://github.com/dotnet/roslyn/issues/41211.
            Assert.Throws<ArgumentException>(() => solution.GetProject(projectId2).RemoveDocuments(ImmutableArray.Create(documentInfo1.Id)));
        }

        [Fact]
        public void RemoveAdditionalDocumentFromUnrelatedProject()
        {
            var projectId1 = ProjectId.CreateNewId();
            var projectId2 = ProjectId.CreateNewId();

            var documentInfo1 = DocumentInfo.Create(DocumentId.CreateNewId(projectId1), "file1.txt");

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                            .AddProject(projectId1, "project1", "project1.dll", LanguageNames.CSharp)
                            .AddProject(projectId2, "project2", "project2.dll", LanguageNames.CSharp)
                            .AddAdditionalDocument(documentInfo1);

            // This should throw if we're removing one document from the wrong project. Right now we don't test the RemoveAdditionalDocument
            // API due to https://github.com/dotnet/roslyn/issues/41211.
            Assert.Throws<ArgumentException>(() => solution.GetProject(projectId2).RemoveAdditionalDocuments(ImmutableArray.Create(documentInfo1.Id)));
        }

        [Fact]
        public void RemoveAnalyzerConfigDocumentFromUnrelatedProject()
        {
            var projectId1 = ProjectId.CreateNewId();
            var projectId2 = ProjectId.CreateNewId();

            var documentInfo1 = DocumentInfo.Create(DocumentId.CreateNewId(projectId1), ".editorconfig");

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                            .AddProject(projectId1, "project1", "project1.dll", LanguageNames.CSharp)
                            .AddProject(projectId2, "project2", "project2.dll", LanguageNames.CSharp)
                            .AddAnalyzerConfigDocuments(ImmutableArray.Create(documentInfo1));

            // This should throw if we're removing one document from the wrong project. Right now we don't test the RemoveAdditionalDocument
            // API due to https://github.com/dotnet/roslyn/issues/41211.
            Assert.Throws<ArgumentException>(() => solution.GetProject(projectId2).RemoveAnalyzerConfigDocuments(ImmutableArray.Create(documentInfo1.Id)));
        }

        [Fact]
        public async Task TestOneCSharpProjectAsync()
        {
            using var workspace = CreateWorkspace();

            var solution = workspace.CurrentSolution
                .AddProject("goo", "goo.dll", LanguageNames.CSharp)
                .AddMetadataReference(s_mscorlib)
                .AddDocument("goo.cs", "public class Goo { }")
                .Project.Solution;

            await ValidateSolutionAndCompilationsAsync(solution);
        }

        [Fact]
        public async Task TestTwoCSharpProjectsAsync()
        {
            using var workspace = CreateWorkspace();

            var pm1 = ProjectId.CreateNewId();
            var pm2 = ProjectId.CreateNewId();
            var doc1 = DocumentId.CreateNewId(pm1);
            var doc2 = DocumentId.CreateNewId(pm2);

            var solution = workspace.CurrentSolution
                .AddProject(pm1, "goo", "goo.dll", LanguageNames.CSharp)
                .AddProject(pm2, "bar", "bar.dll", LanguageNames.CSharp)
                .AddProjectReference(pm2, new ProjectReference(pm1))
                .AddDocument(doc1, "goo.cs", "public class Goo { }")
                .AddDocument(doc2, "bar.cs", "public class Bar : Goo { }");

            await ValidateSolutionAndCompilationsAsync(solution);
        }

        [Fact]
        public async Task TestCrossLanguageProjectsAsync()
        {
            var pm1 = ProjectId.CreateNewId();
            var pm2 = ProjectId.CreateNewId();
            using var workspace = CreateWorkspace();

            var solution = workspace.CurrentSolution
                .AddProject(pm1, "goo", "goo.dll", LanguageNames.CSharp)
                .AddMetadataReference(pm1, s_mscorlib)
                .AddProject(pm2, "bar", "bar.dll", LanguageNames.VisualBasic)
                .AddMetadataReference(pm2, s_mscorlib)
                .AddProjectReference(pm2, new ProjectReference(pm1))
                .AddDocument(DocumentId.CreateNewId(pm1), "goo.cs", "public class X { }")
                .AddDocument(DocumentId.CreateNewId(pm2), "bar.vb", "Public Class Y\r\nInherits X\r\nEnd Class");

            await ValidateSolutionAndCompilationsAsync(solution);
        }

        private static async Task ValidateSolutionAndCompilationsAsync(Solution solution)
        {
            foreach (var project in solution.Projects)
            {
                Assert.True(solution.ContainsProject(project.Id), "Solution was expected to have project " + project.Id);
                Assert.Equal(project, solution.GetProject(project.Id));

                // these won't always be unique in real-world but should be for these tests
                Assert.Equal(project, solution.GetProjectsByName(project.Name).FirstOrDefault());

                var compilation = await project.GetCompilationAsync();
                Assert.NotNull(compilation);

                // check that the options are the same
                Assert.Equal(project.CompilationOptions, compilation.Options);

                // check that all known metadata references are present in the compilation
                foreach (var meta in project.MetadataReferences)
                {
                    Assert.True(compilation.References.Contains(meta), "Compilation references were expected to contain " + meta);
                }

                // check that all project-to-project reference metadata is present in the compilation
                foreach (var referenced in project.ProjectReferences)
                {
                    if (solution.ContainsProject(referenced.ProjectId))
                    {
                        var referencedMetadata = await solution.State.GetMetadataReferenceAsync(referenced, solution.GetProjectState(project.Id), CancellationToken.None);
                        Assert.NotNull(referencedMetadata);
                        if (referencedMetadata is CompilationReference compilationReference)
                        {
                            compilation.References.Single(r =>
                            {
                                var cr = r as CompilationReference;
                                return cr != null && cr.Compilation == compilationReference.Compilation;
                            });
                        }
                    }
                }

                // check that the syntax trees are the same
                var docs = project.Documents.ToList();
                var trees = compilation.SyntaxTrees.ToList();
                Assert.Equal(docs.Count, trees.Count);

                foreach (var doc in docs)
                {
                    Assert.True(trees.Contains(await doc.GetSyntaxTreeAsync()), "trees list was expected to contain the syntax tree of doc");
                }
            }
        }

#if false
        [Fact(Skip = "641963")]
        public void TestDeepProjectReferenceTree()
        {
            int projectCount = 5;
            var solution = CreateSolutionWithProjectDependencyChain(projectCount);
            ProjectId[] projectIds = solution.ProjectIds.ToArray();

            Compilation compilation;
            for (int i = 0; i < projectCount; i++)
            {
                Assert.False(solution.GetProject(projectIds[i]).TryGetCompilation(out compilation));
            }

            var top = solution.GetCompilationAsync(projectIds.Last(), CancellationToken.None).Result;
            var partialSolution = solution.GetPartialSolution();
            for (int i = 0; i < projectCount; i++)
            {
                // While holding a compilation, we also hold its references, plus one further level
                // of references alive.  However, the references are only partial Declaration 
                // compilations
                var isPartialAvailable = i >= projectCount - 3;
                var isFinalAvailable = i == projectCount - 1;

                var projectId = projectIds[i];
                Assert.Equal(isFinalAvailable, solution.GetProject(projectId).TryGetCompilation(out compilation));
                Assert.Equal(isPartialAvailable, partialSolution.ProjectIds.Contains(projectId) && partialSolution.GetProject(projectId).TryGetCompilation(out compilation));
            }
        }
#endif

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636431")]
        public async Task TestProjectDependencyLoadingAsync()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var projectIds = Enumerable.Range(0, 5).Select(i => ProjectId.CreateNewId()).ToArray();
            for (var i = 0; i < projectIds.Length; i++)
            {
                solution = solution.AddProject(projectIds[i], i.ToString(), i.ToString(), LanguageNames.CSharp);
                if (i >= 1)
                {
                    solution = solution.AddProjectReference(projectIds[i], new ProjectReference(projectIds[i - 1]));
                }
            }

            await solution.GetProject(projectIds[0]).GetCompilationAsync(CancellationToken.None);
            await solution.GetProject(projectIds[2]).GetCompilationAsync(CancellationToken.None);
        }

        [Fact]
        public async Task TestAddMetadataReferencesAsync()
        {
            var mefReference = TestMetadata.Net451.SystemCore;
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            var project1 = ProjectId.CreateNewId();
            solution = solution.AddProject(project1, "goo", "goo.dll", LanguageNames.CSharp);
            solution = solution.AddMetadataReference(project1, s_mscorlib);

            solution = solution.AddMetadataReference(project1, mefReference);
            var assemblyReference = (IAssemblySymbol)solution.GetProject(project1).GetCompilationAsync().Result.GetAssemblyOrModuleSymbol(mefReference);
            var namespacesAndTypes = assemblyReference.GlobalNamespace.GetAllNamespacesAndTypes(CancellationToken.None);
            var foundSymbol = from symbol in namespacesAndTypes
                              where symbol.Name.Equals("Enumerable")
                              select symbol;
            Assert.Equal(1, foundSymbol.Count());
            solution = solution.RemoveMetadataReference(project1, mefReference);
            assemblyReference = (IAssemblySymbol)solution.GetProject(project1).GetCompilationAsync().Result.GetAssemblyOrModuleSymbol(mefReference);
            Assert.Null(assemblyReference);

            await ValidateSolutionAndCompilationsAsync(solution);
        }

        private class MockDiagnosticAnalyzer : DiagnosticAnalyzer
        {
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override void Initialize(AnalysisContext analysisContext)
            {
            }
        }

        [Fact]
        public void TestProjectDiagnosticAnalyzers()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            var project1 = ProjectId.CreateNewId();
            solution = solution.AddProject(project1, "goo", "goo.dll", LanguageNames.CSharp);
            Assert.Empty(solution.Projects.Single().AnalyzerReferences);

            DiagnosticAnalyzer analyzer = new MockDiagnosticAnalyzer();
            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create(analyzer));

            // Test AddAnalyzer
            var newSolution = solution.AddAnalyzerReference(project1, analyzerReference);
            var actualAnalyzerReferences = newSolution.Projects.Single().AnalyzerReferences;
            Assert.Equal(1, actualAnalyzerReferences.Count);
            Assert.Equal(analyzerReference, actualAnalyzerReferences[0]);
            var actualAnalyzers = actualAnalyzerReferences[0].GetAnalyzersForAllLanguages();
            Assert.Equal(1, actualAnalyzers.Length);
            Assert.Equal(analyzer, actualAnalyzers[0]);

            // Test ProjectChanges
            var changes = newSolution.GetChanges(solution).GetProjectChanges().Single();
            var addedAnalyzerReference = changes.GetAddedAnalyzerReferences().Single();
            Assert.Equal(analyzerReference, addedAnalyzerReference);
            var removedAnalyzerReferences = changes.GetRemovedAnalyzerReferences();
            Assert.Empty(removedAnalyzerReferences);
            solution = newSolution;

            // Test RemoveAnalyzer
            solution = solution.RemoveAnalyzerReference(project1, analyzerReference);
            actualAnalyzerReferences = solution.Projects.Single().AnalyzerReferences;
            Assert.Empty(actualAnalyzerReferences);

            // Test AddAnalyzers
            analyzerReference = new AnalyzerImageReference(ImmutableArray.Create(analyzer));
            DiagnosticAnalyzer secondAnalyzer = new MockDiagnosticAnalyzer();
            var secondAnalyzerReference = new AnalyzerImageReference(ImmutableArray.Create(secondAnalyzer));
            var analyzerReferences = new[] { analyzerReference, secondAnalyzerReference };
            solution = solution.AddAnalyzerReferences(project1, analyzerReferences);
            actualAnalyzerReferences = solution.Projects.Single().AnalyzerReferences;
            Assert.Equal(2, actualAnalyzerReferences.Count);
            Assert.Equal(analyzerReference, actualAnalyzerReferences[0]);
            Assert.Equal(secondAnalyzerReference, actualAnalyzerReferences[1]);

            solution = solution.RemoveAnalyzerReference(project1, analyzerReference);
            actualAnalyzerReferences = solution.Projects.Single().AnalyzerReferences;
            Assert.Equal(1, actualAnalyzerReferences.Count);
            Assert.Equal(secondAnalyzerReference, actualAnalyzerReferences[0]);

            // Test WithAnalyzers
            solution = solution.WithProjectAnalyzerReferences(project1, analyzerReferences);
            actualAnalyzerReferences = solution.Projects.Single().AnalyzerReferences;
            Assert.Equal(2, actualAnalyzerReferences.Count);
            Assert.Equal(analyzerReference, actualAnalyzerReferences[0]);
            Assert.Equal(secondAnalyzerReference, actualAnalyzerReferences[1]);
        }

        [Fact]
        public void TestProjectParseOptions()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            var project1 = ProjectId.CreateNewId();
            solution = solution.AddProject(project1, "goo", "goo.dll", LanguageNames.CSharp);
            solution = solution.AddMetadataReference(project1, s_mscorlib);

            // Parse Options
            var oldParseOptions = solution.GetProject(project1).ParseOptions;
            var newParseOptions = new CSharpParseOptions(preprocessorSymbols: new[] { "AFTER" });
            solution = solution.WithProjectParseOptions(project1, newParseOptions);
            var newUpdatedParseOptions = solution.GetProject(project1).ParseOptions;
            Assert.NotEqual(oldParseOptions, newUpdatedParseOptions);
            Assert.Same(newParseOptions, newUpdatedParseOptions);
        }

        [Fact]
        public async Task TestRemoveProjectAsync()
        {
            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution;

            var pid = ProjectId.CreateNewId();
            sol = sol.AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp);
            Assert.True(sol.ProjectIds.Any(), "Solution was expected to have projects");
            Assert.NotNull(pid);
            var project = sol.GetProject(pid);
            Assert.False(project.HasDocuments);

            var sol2 = sol.RemoveProject(pid);
            Assert.False(sol2.ProjectIds.Any());

            await ValidateSolutionAndCompilationsAsync(sol);
        }

        [Fact]
        public async Task TestRemoveProjectWithReferencesAsync()
        {
            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution;

            var pid = ProjectId.CreateNewId();
            var pid2 = ProjectId.CreateNewId();
            sol = sol.AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                   .AddProject(pid2, "bar", "bar.dll", LanguageNames.CSharp)
                   .AddProjectReference(pid2, new ProjectReference(pid));

            Assert.Equal(2, sol.Projects.Count());

            // remove the project that is being referenced
            // this should leave a dangling reference
            var sol2 = sol.RemoveProject(pid);

            Assert.False(sol2.ContainsProject(pid));
            Assert.True(sol2.ContainsProject(pid2), "sol2 was expected to contain project " + pid2);
            Assert.Equal(1, sol2.Projects.Count());
            Assert.True(sol2.GetProject(pid2).AllProjectReferences.Any(r => r.ProjectId == pid), "sol2 project pid2 was expected to contain project reference " + pid);

            await ValidateSolutionAndCompilationsAsync(sol2);
        }

        [Fact]
        public async Task TestRemoveProjectWithReferencesAndAddItBackAsync()
        {
            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution;

            var pid = ProjectId.CreateNewId();
            var pid2 = ProjectId.CreateNewId();
            sol = sol.AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                   .AddProject(pid2, "bar", "bar.dll", LanguageNames.CSharp)
                   .AddProjectReference(pid2, new ProjectReference(pid));

            Assert.Equal(2, sol.Projects.Count());

            // remove the project that is being referenced
            var sol2 = sol.RemoveProject(pid);

            Assert.False(sol2.ContainsProject(pid));
            Assert.True(sol2.ContainsProject(pid2), "sol2 was expected to contain project " + pid2);
            Assert.Equal(1, sol2.Projects.Count());
            Assert.True(sol2.GetProject(pid2).AllProjectReferences.Any(r => r.ProjectId == pid), "sol2 pid2 was expected to contain " + pid);

            var sol3 = sol2.AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp);

            Assert.True(sol3.ContainsProject(pid), "sol3 was expected to contain " + pid);
            Assert.True(sol3.ContainsProject(pid2), "sol3 was expected to contain " + pid2);
            Assert.Equal(2, sol3.Projects.Count());

            await ValidateSolutionAndCompilationsAsync(sol3);
        }

        [Fact]
        public async Task TestGetSyntaxRootAsync()
        {
            var text = "public class Goo { }";

            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                            .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                            .AddDocument(did, "goo.cs", text);

            var document = sol.GetDocument(did);
            Assert.False(document.TryGetSyntaxRoot(out _));

            var root = await document.GetSyntaxRootAsync();
            Assert.NotNull(root);
            Assert.Equal(text, root.ToString());

            Assert.True(document.TryGetSyntaxRoot(out root));
            Assert.NotNull(root);
        }

        [Fact]
        public async Task TestUpdateDocumentAsync()
        {
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            using var workspace = CreateWorkspace();
            var solution1 = workspace.CurrentSolution
                .AddProject(projectId, "ProjectName", "AssemblyName", LanguageNames.CSharp)
                .AddDocument(documentId, "DocumentName", SourceText.From("class Class{}"));

            var document = solution1.GetDocument(documentId);
            var newRoot = await Formatter.FormatAsync(document, CSharpSyntaxFormattingOptions.Default, CancellationToken.None).Result.GetSyntaxRootAsync();
            var solution2 = solution1.WithDocumentSyntaxRoot(documentId, newRoot);

            Assert.NotEqual(solution1, solution2);

            var newText = solution2.GetDocument(documentId).GetTextAsync().Result.ToString();
            Assert.Equal("class Class { }", newText);
        }

        [Fact]
        public void TestUpdateSyntaxTreeWithAnnotations()
        {
            var text = "public class Goo { }";

            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                            .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                            .AddDocument(did, "goo.cs", text);

            var document = sol.GetDocument(did);
            var tree = document.GetSyntaxTreeAsync().Result;
            var root = tree.GetRoot();

            var annotation = new SyntaxAnnotation();
            var annotatedRoot = root.WithAdditionalAnnotations(annotation);

            var sol2 = sol.WithDocumentSyntaxRoot(did, annotatedRoot);
            var doc2 = sol2.GetDocument(did);
            var tree2 = doc2.GetSyntaxTreeAsync().Result;
            var root2 = tree2.GetRoot();
            // text should not be available yet (it should be defer created from the node)
            // and getting the document or root should not cause it to be created.
            Assert.False(tree2.TryGetText(out _));

            var text2 = tree2.GetText();
            Assert.NotNull(text2);

            Assert.NotSame(tree, tree2);
            Assert.NotSame(annotatedRoot, root2);

            Assert.True(annotatedRoot.IsEquivalentTo(root2));
            Assert.True(root2.HasAnnotation(annotation));
        }

        [Theory]
        [InlineData(LanguageNames.CSharp)]
        [InlineData(LanguageNames.VisualBasic)]
        public async Task ParsedTreeRootOwnership(string language)
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var source = (language == LanguageNames.CSharp) ? "class C {}" : "Class C : End Class";

            using var workspace = CreateWorkspace();

            var sol = workspace.CurrentSolution
                .AddProject(pid, "test", "test.dll", language)
                .AddDocument(did, "test", source);

            var document = sol.GetDocument(did);

            // update the document syntax root:
            var syntaxRoot = await document.GetSyntaxRootAsync(CancellationToken.None);

            SyntaxNode newSyntaxRoot;
            if (language == LanguageNames.CSharp)
            {
                var classNode = syntaxRoot.DescendantNodes().OfType<CS.Syntax.ClassDeclarationSyntax>().Single();
                newSyntaxRoot = syntaxRoot.ReplaceNode(classNode, classNode.WithModifiers(CS.SyntaxFactory.TokenList(CS.SyntaxFactory.ParseToken("public"))));
            }
            else
            {
                var classNode = syntaxRoot.DescendantNodes().OfType<VB.Syntax.ClassStatementSyntax>().Single();
                newSyntaxRoot = syntaxRoot.ReplaceNode(classNode, classNode.WithModifiers(VB.SyntaxFactory.TokenList(VB.SyntaxFactory.ParseToken("Public"))));
            }

            var documentWithAttribute = document.WithSyntaxRoot(newSyntaxRoot);

            var tree = await documentWithAttribute.GetSyntaxTreeAsync(CancellationToken.None);
            var root = await documentWithAttribute.GetRequiredSyntaxRootAsync(CancellationToken.None);

            Assert.Same(tree, root.SyntaxTree);
            Assert.Same(tree, tree.WithRootAndOptions(root, tree.Options));
            Assert.Same(tree, tree.WithFilePath(tree.FilePath));
        }

        [Fact]
        public void TestUpdatingFilePathUpdatesSyntaxTree()
        {
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            const string OldFilePath = @"Z:\OldFilePath.cs";
            const string NewFilePath = @"Z:\NewFilePath.cs";

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                            .AddProject(projectId, "goo", "goo.dll", LanguageNames.CSharp)
                            .AddDocument(documentId, "OldFilePath.cs", "public class Goo { }", filePath: OldFilePath);

            // scope so later asserts don't accidentally use oldDocument
            {
                var oldDocument = solution.GetDocument(documentId);
                Assert.Equal(OldFilePath, oldDocument.FilePath);
                Assert.Equal(OldFilePath, oldDocument.GetSyntaxTreeAsync().Result.FilePath);
            }

            solution = solution.WithDocumentFilePath(documentId, NewFilePath);

            {
                var newDocument = solution.GetDocument(documentId);
                Assert.Equal(NewFilePath, newDocument.FilePath);
                Assert.Equal(NewFilePath, newDocument.GetSyntaxTreeAsync().Result.FilePath);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542736")]
        public void TestDocumentChangedOnDiskIsNotObserved()
        {
            var text1 = "public class A {}";
            var text2 = "public class B {}";

            var file = Temp.CreateFile().WriteAllText(text1, Encoding.UTF8);

            // create a solution that evicts from the cache immediately.
            using var workspace = CreateWorkspaceWithRecoverableText();
            var sol = workspace.CurrentSolution;

            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            sol = sol.AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                     .AddDocument(did, "x", new WorkspaceFileTextLoader(workspace.Services.SolutionServices, file.Path, Encoding.UTF8));

            var observedText = GetObservedText(sol, did, text1);

            // change text on disk & verify it is changed
            file.WriteAllText(text2);
            var textOnDisk = file.ReadAllText();
            Assert.Equal(text2, textOnDisk);

            // stop observing it and let GC reclaim it
            if (PlatformInformation.IsWindows || PlatformInformation.IsRunningOnMono)
            {
                Assert.IsType<TemporaryStorageService>(workspace.Services.GetService<ITemporaryStorageServiceInternal>());
                observedText.AssertReleased();
            }
            else
            {
                // If this assertion fails, it means a new target supports the true temporary storage service, and the
                // condition above should be updated to ensure 'AssertReleased' is called for this target.
                Assert.IsType<TrivialTemporaryStorageService>(workspace.Services.GetService<ITemporaryStorageServiceInternal>());
            }

            // if we ask for the same text again we should get the original content
            var observedText2 = sol.GetDocument(did).GetTextAsync().Result;
            Assert.Equal(text1, observedText2.ToString());
        }

        [Fact]
        public void TestGetTextAsync()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                                    .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                                    .AddDocument(did, "goo.cs", text);

            var doc = sol.GetDocument(did);

            var docText = doc.GetTextAsync().Result;

            Assert.NotNull(docText);
            Assert.Equal(text, docText.ToString());
        }

        [Fact]
        public void TestGetLoadedTextAsync()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            var file = Temp.CreateFile().WriteAllText(text, Encoding.UTF8);

            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                                    .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                                    .AddDocument(did, "x", new WorkspaceFileTextLoader(workspace.Services.SolutionServices, file.Path, Encoding.UTF8));

            var doc = sol.GetDocument(did);

            var docText = doc.GetTextAsync().Result;

            Assert.NotNull(docText);
            Assert.Equal(text, docText.ToString());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/19427")]
        public void TestGetRecoveredTextAsync()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                                    .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                                    .AddDocument(did, "goo.cs", text);

            // observe the text and then wait for the references to be GC'd
            var observed = GetObservedText(sol, did, text);
            observed.AssertReleased();

            // get it async and force it to recover from temporary storage
            var doc = sol.GetDocument(did);
            var docText = doc.GetTextAsync().Result;

            Assert.NotNull(docText);
            Assert.Equal(text, docText.ToString());
        }

        [Fact]
        public void TestGetSyntaxTreeAsync()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                                    .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                                    .AddDocument(did, "goo.cs", text);

            var doc = sol.GetDocument(did);

            var docTree = doc.GetSyntaxTreeAsync().Result;

            Assert.NotNull(docTree);
            Assert.Equal(text, docTree.GetRoot().ToString());
        }

        [Fact]
        public void TestGetSyntaxTreeFromLoadedTextAsync()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            var file = Temp.CreateFile().WriteAllText(text, Encoding.UTF8);

            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                                    .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                                    .AddDocument(did, "x", new WorkspaceFileTextLoader(workspace.Services.SolutionServices, file.Path, Encoding.UTF8));

            var doc = sol.GetDocument(did);
            var docTree = doc.GetSyntaxTreeAsync().Result;

            Assert.NotNull(docTree);
            Assert.Equal(text, docTree.GetRoot().ToString());
        }

        [Fact]
        public void TestGetSyntaxTreeFromAddedTree()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var tree = CSharp.SyntaxFactory.ParseSyntaxTree("public class C {}").GetRoot(CancellationToken.None);
            tree = tree.WithAdditionalAnnotations(new SyntaxAnnotation("test"));

            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                                    .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                                    .AddDocument(did, "x", tree);

            var doc = sol.GetDocument(did);
            var docTree = doc.GetSyntaxRootAsync().Result;

            Assert.NotNull(docTree);
            Assert.True(tree.IsEquivalentTo(docTree));
            Assert.NotNull(docTree.GetAnnotatedNodes("test").Single());
        }

        [Fact]
        public async Task TestGetSyntaxRootAsync2Async()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                                    .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                                    .AddDocument(did, "goo.cs", text);

            var doc = sol.GetDocument(did);

            var docRoot = await doc.GetSyntaxRootAsync();

            Assert.NotNull(docRoot);
            Assert.Equal(text, docRoot.ToString());
        }

        [Fact]
        public void TestGetCompilationAsync()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                                    .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                                    .AddDocument(did, "goo.cs", text);

            var proj = sol.GetProject(pid);

            var compilation = proj.GetCompilationAsync().Result;

            Assert.NotNull(compilation);
            Assert.Equal(1, compilation.SyntaxTrees.Count());
        }

        [Fact]
        public void TestGetSemanticModelAsync()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                                    .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                                    .AddDocument(did, "goo.cs", text);

            var doc = sol.GetDocument(did);

            var docModel = doc.GetSemanticModelAsync().Result;
            Assert.NotNull(docModel);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/13433")]
        public void TestGetTextDoesNotKeepTextAlive()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                                    .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                                    .AddDocument(did, "goo.cs", text);

            // observe the text and then wait for the references to be GC'd
            var observed = GetObservedText(sol, did, text);
            observed.AssertReleased();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ObjectReference<SourceText> GetObservedText(Solution solution, DocumentId documentId, string expectedText = null)
        {
            var observedText = solution.GetDocument(documentId).GetTextAsync().Result;

            if (expectedText != null)
            {
                Assert.Equal(expectedText, observedText.ToString());
            }

            return new ObjectReference<SourceText>(observedText);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/13433")]
        public void TestGetTextAsyncDoesNotKeepTextAlive()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                                    .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                                    .AddDocument(did, "goo.cs", text);

            // observe the text and then wait for the references to be GC'd
            var observed = GetObservedTextAsync(sol, did, text);
            observed.AssertReleased();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ObjectReference<SourceText> GetObservedTextAsync(Solution solution, DocumentId documentId, string expectedText = null)
        {
            var observedText = solution.GetDocument(documentId).GetTextAsync().Result;

            if (expectedText != null)
            {
                Assert.Equal(expectedText, observedText.ToString());
            }

            return new ObjectReference<SourceText>(observedText);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/13433")]
        public void TestGetSyntaxRootDoesNotKeepRootAlive()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";

            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                                    .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                                    .AddDocument(did, "goo.cs", text);

            // get it async and wait for it to get GC'd
            var observed = GetObservedSyntaxTreeRoot(sol, did);
            observed.AssertReleased();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ObjectReference<SyntaxNode> GetObservedSyntaxTreeRoot(Solution solution, DocumentId documentId)
        {
            var observedTree = solution.GetDocument(documentId).GetSyntaxRootAsync().Result;
            return new ObjectReference<SyntaxNode>(observedTree);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/13433")]
        public void TestGetSyntaxRootAsyncDoesNotKeepRootAlive()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";

            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                                    .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                                    .AddDocument(did, "goo.cs", text);

            // get it async and wait for it to get GC'd
            var observed = GetObservedSyntaxTreeRootAsync(sol, did);
            observed.AssertReleased();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ObjectReference<SyntaxNode> GetObservedSyntaxTreeRootAsync(Solution solution, DocumentId documentId)
        {
            var observedTree = solution.GetDocument(documentId).GetSyntaxRootAsync().Result;
            return new ObjectReference<SyntaxNode>(observedTree);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/13506")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/13506")]
        public void TestRecoverableSyntaxTreeCSharp()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = @"public class C {
    public void Method1() {}
    public void Method2() {}
    public void Method3() {}
    public void Method4() {}
    public void Method5() {}
    public void Method6() {}
}";

            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                                    .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                                    .AddDocument(did, "goo.cs", text);

            TestRecoverableSyntaxTree(sol, did);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/13433")]
        public void TestRecoverableSyntaxTreeVisualBasic()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = @"Public Class C
    Sub Method1()
    End Sub
    Sub Method2()
    End Sub
    Sub Method3()
    End Sub
    Sub Method4()
    End Sub
    Sub Method5()
    End Sub
    Sub Method6()
    End Sub
End Class";

            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                                    .AddProject(pid, "goo", "goo.dll", LanguageNames.VisualBasic)
                                    .AddDocument(did, "goo.vb", text);

            TestRecoverableSyntaxTree(sol, did);
        }

        private static void TestRecoverableSyntaxTree(Solution sol, DocumentId did)
        {
            // get it async and wait for it to get GC'd
            var observed = GetObservedSyntaxTreeRootAsync(sol, did);
            observed.AssertReleased();

            var doc = sol.GetDocument(did);

            // access the tree & root again (recover it)
            var tree = doc.GetSyntaxTreeAsync().Result;

            // this should cause reparsing
            var root = tree.GetRoot();

            // prove that the new root is correctly associated with the tree
            Assert.Equal(tree, root.SyntaxTree);

            // reset the syntax root, to make it 'refactored' by adding an attribute
            var newRoot = doc.GetSyntaxRootAsync().Result.WithAdditionalAnnotations(SyntaxAnnotation.ElasticAnnotation);
            var doc2 = doc.Project.Solution.WithDocumentSyntaxRoot(doc.Id, newRoot, PreservationMode.PreserveValue).GetDocument(doc.Id);

            // get it async and wait for it to get GC'd
            var observed2 = GetObservedSyntaxTreeRootAsync(doc2.Project.Solution, did);
            observed2.AssertReleased();

            // access the tree & root again (recover it)
            var tree2 = doc2.GetSyntaxTreeAsync().Result;

            // this should cause deserialization
            var root2 = tree2.GetRoot();

            // prove that the new root is correctly associated with the tree
            Assert.Equal(tree2, root2.SyntaxTree);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/13433")]
        public void TestGetCompilationAsyncDoesNotKeepCompilationAlive()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                                    .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                                    .AddDocument(did, "goo.cs", text);

            // get it async and wait for it to get GC'd
            var observed = GetObservedCompilationAsync(sol, pid);
            observed.AssertReleased();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ObjectReference<Compilation> GetObservedCompilationAsync(Solution solution, ProjectId projectId)
        {
            var observed = solution.GetProject(projectId).GetCompilationAsync().Result;
            return new ObjectReference<Compilation>(observed);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/13433")]
        public void TestGetCompilationDoesNotKeepCompilationAlive()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            using var workspace = CreateWorkspace();
            var sol = workspace.CurrentSolution
                                    .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                                    .AddDocument(did, "goo.cs", text);

            // get it async and wait for it to get GC'd
            var observed = GetObservedCompilation(sol, pid);
            observed.AssertReleased();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ObjectReference<Compilation> GetObservedCompilation(Solution solution, ProjectId projectId)
        {
            var observed = solution.GetProject(projectId).GetCompilationAsync().Result;
            return new ObjectReference<Compilation>(observed);
        }

        [Theory]
        [InlineData(LanguageNames.CSharp)]
        [InlineData(LanguageNames.VisualBasic)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/63834")]
        public void RecoverableTree_With(string language)
        {
            using var workspace = CreateWorkspace();

            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var sol = workspace.CurrentSolution
                .AddProject(pid, "test", "test.dll", language)
                .AddDocument(did, "test", SourceText.From(language == LanguageNames.CSharp ? "class C {}" : "Class C : End Class", Encoding.UTF8, SourceHashAlgorithm.Sha256), filePath: "old path");

            var document = sol.GetDocument(did);
            var tree = document.GetSyntaxTreeSynchronously(default);
            //var recoverableTree = Assert.IsAssignableFrom<IRecoverableSyntaxTree>(tree);

            var tree2 = tree.WithFilePath("new path");
            //var recoverableTree2 = Assert.IsAssignableFrom<IRecoverableSyntaxTree>(tree2);

            Assert.Equal("new path", tree2.FilePath);
            Assert.Same(tree2, tree2.GetRoot().SyntaxTree);
            Assert.Same(tree.Options, tree2.Options);
            Assert.Same(tree.Encoding, tree2.Encoding);
            Assert.Equal(tree.Length, tree2.Length);
            //Assert.Equal(recoverableTree.ContainsDirectives, recoverableTree2.ContainsDirectives);

            // unchanged:
            Assert.Same(tree, tree.WithFilePath("old path"));

            var newRoot = (language == LanguageNames.CSharp) ? CS.SyntaxFactory.ParseCompilationUnit("""
                #define X
                #if X
                class NewType {}
                #endif
                """) : (SyntaxNode)VB.SyntaxFactory.ParseCompilationUnit("""
                #Define X
                #If X
                Class C
                End Class
                #End If
                """);

            Assert.True(newRoot.ContainsDirectives);

            var tree3 = tree.WithRootAndOptions(newRoot, tree.Options);
            //var recoverableTree3 = Assert.IsAssignableFrom<IRecoverableSyntaxTree>(tree3);

            Assert.Equal("old path", tree3.FilePath);
            Assert.Same(tree3, tree3.GetRoot().SyntaxTree);
            Assert.Same(tree.Options, tree3.Options);
            Assert.Same(tree.Encoding, tree3.Encoding);
            Assert.Equal(newRoot.FullSpan.Length, tree3.Length);
            //Assert.True(recoverableTree3.ContainsDirectives);

            var newOptions = tree.Options.WithKind(SourceCodeKind.Script);
            var tree4 = tree.WithRootAndOptions(tree.GetRoot(), newOptions);
            //var recoverableTree4 = Assert.IsAssignableFrom<IRecoverableSyntaxTree>(tree4);

            Assert.Equal("old path", tree4.FilePath);
            Assert.Same(tree4, tree4.GetRoot().SyntaxTree);
            Assert.Same(newOptions, tree4.Options);
            Assert.Same(tree.Encoding, tree4.Encoding);
            Assert.Equal(tree.Length, tree4.Length);
            //Assert.Equal(recoverableTree.ContainsDirectives, recoverableTree4.ContainsDirectives);

            // unchanged:
            Assert.Same(tree, tree.WithRootAndOptions(tree.GetRoot(), tree.Options));
        }

        [Fact]
        public void TestWorkspaceLanguageServiceOverride()
        {
            var hostServices = FeaturesTestCompositions.Features.AddParts(new[]
            {
                typeof(TestLanguageServiceA),
                typeof(TestLanguageServiceB),
            }).GetHostServices();

            var ws = new AdhocWorkspace(hostServices, ServiceLayer.Host);
            var service = ws.Services.GetLanguageServices(LanguageNames.CSharp).GetService<ITestLanguageService>();
            Assert.NotNull(service as TestLanguageServiceA);

            var ws2 = new AdhocWorkspace(hostServices, "Quasimodo");
            var service2 = ws2.Services.GetLanguageServices(LanguageNames.CSharp).GetService<ITestLanguageService>();
            Assert.NotNull(service2 as TestLanguageServiceB);
        }

#if false
        [Fact]
        public void TestSolutionInfo()
        {
            var oldSolutionId = SolutionId.CreateNewId("oldId");
            var oldVersion = VersionStamp.Create();
            var solutionInfo = SolutionInfo.Create(oldSolutionId, oldVersion, null, null);

            var newSolutionId = SolutionId.CreateNewId("newId");
            solutionInfo = solutionInfo.WithId(newSolutionId);
            Assert.NotEqual(oldSolutionId, solutionInfo.Id);
            Assert.Equal(newSolutionId, solutionInfo.Id);
            
            var newVersion = oldVersion.GetNewerVersion();
            solutionInfo = solutionInfo.WithVersion(newVersion);
            Assert.NotEqual(oldVersion, solutionInfo.Version);
            Assert.Equal(newVersion, solutionInfo.Version);

            Assert.Null(solutionInfo.FilePath);
            var newFilePath = @"C:\test\fake.sln";
            solutionInfo = solutionInfo.WithFilePath(newFilePath);
            Assert.Equal(newFilePath, solutionInfo.FilePath);

            Assert.Equal(0, solutionInfo.Projects.Count());
        }
#endif

        private interface ITestLanguageService : ILanguageService
        {
        }

        [ExportLanguageService(typeof(ITestLanguageService), LanguageNames.CSharp, ServiceLayer.Default), Shared, PartNotDiscoverable]
        private class TestLanguageServiceA : ITestLanguageService
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public TestLanguageServiceA()
            {
            }
        }

        [ExportLanguageService(typeof(ITestLanguageService), LanguageNames.CSharp, "Quasimodo"), Shared, PartNotDiscoverable]
        private class TestLanguageServiceB : ITestLanguageService
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public TestLanguageServiceB()
            {
            }
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/666263")]
        public async Task TestDocumentFileAccessFailureMissingFile()
        {
            var workspace = new AdhocWorkspace();
            var solution = workspace.CurrentSolution;

            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            solution = solution.AddProject(pid, "goo", "goo", LanguageNames.CSharp)
                               .AddDocument(did, "x", new WorkspaceFileTextLoader(solution.Services, @"C:\doesnotexist.cs", Encoding.UTF8))
                               .WithDocumentFilePath(did, "document path");

            var doc = solution.GetDocument(did);
            var text = await doc.GetTextAsync().ConfigureAwait(false);

            var diagnostic = await doc.State.GetLoadDiagnosticAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(@"C:\doesnotexist.cs: (0,0)-(0,0)", diagnostic.Location.GetLineSpan().ToString());
            Assert.Equal("", text.ToString());

            // Verify invariant: The compilation is guaranteed to have a syntax tree for each document of the project (even if the contnet fails to load).
            var compilation = await solution.State.GetCompilationAsync(doc.Project.State, CancellationToken.None).ConfigureAwait(false);
            var syntaxTree = compilation.SyntaxTrees.Single();
            Assert.Equal("", syntaxTree.ToString());
        }

        [Fact]
        public void TestGetProjectForAssemblySymbol()
        {
            var pid1 = ProjectId.CreateNewId("p1");
            var pid2 = ProjectId.CreateNewId("p2");
            var pid3 = ProjectId.CreateNewId("p3");
            var did1 = DocumentId.CreateNewId(pid1);
            var did2 = DocumentId.CreateNewId(pid2);
            var did3 = DocumentId.CreateNewId(pid3);

            var text1 = @"
Public Class A
End Class";

            var text2 = @"
Public Class B
End Class
";

            var text3 = @"
public class C : B {
}
";

            var text4 = @"
public class C : A {
}
";

            var solution = new AdhocWorkspace().CurrentSolution
                .AddProject(pid1, "GooA", "Goo.dll", LanguageNames.VisualBasic)
                .AddDocument(did1, "A.vb", text1)
                .AddMetadataReference(pid1, s_mscorlib)
                .AddProject(pid2, "GooB", "Goo2.dll", LanguageNames.VisualBasic)
                .AddDocument(did2, "B.vb", text2)
                .AddMetadataReference(pid2, s_mscorlib)
                .AddProject(pid3, "Bar", "Bar.dll", LanguageNames.CSharp)
                .AddDocument(did3, "C.cs", text3)
                .AddMetadataReference(pid3, s_mscorlib)
                .AddProjectReference(pid3, new ProjectReference(pid1))
                .AddProjectReference(pid3, new ProjectReference(pid2));

            var project3 = solution.GetProject(pid3);
            var comp3 = project3.GetCompilationAsync().Result;
            var classC = comp3.GetTypeByMetadataName("C");
            var projectForBaseType = solution.GetProject(classC.BaseType.ContainingAssembly);
            Assert.Equal(pid2, projectForBaseType.Id);

            // switch base type to A then try again
            var solution2 = solution.WithDocumentText(did3, SourceText.From(text4));
            project3 = solution2.GetProject(pid3);
            comp3 = project3.GetCompilationAsync().Result;
            classC = comp3.GetTypeByMetadataName("C");
            projectForBaseType = solution2.GetProject(classC.BaseType.ContainingAssembly);
            Assert.Equal(pid1, projectForBaseType.Id);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1088127")]
        public void TestEncodingRetainedAfterTreeChanged()
        {
            var ws = new AdhocWorkspace();
            var proj = ws.AddProject("proj", LanguageNames.CSharp);
            var doc = ws.AddDocument(proj.Id, "a.cs", SourceText.From("public class c { }", Encoding.UTF32));

            Assert.Equal(Encoding.UTF32, doc.GetTextAsync().Result.Encoding);

            // updating root doesn't change original encoding
            var root = doc.GetSyntaxRootAsync().Result;
            var newRoot = root.WithLeadingTrivia(root.GetLeadingTrivia().Add(CS.SyntaxFactory.Whitespace("    ")));
            var newDoc = doc.WithSyntaxRoot(newRoot);

            Assert.Equal(Encoding.UTF32, newDoc.GetTextAsync().Result.Encoding);
        }

        [Fact]
        public void TestProjectWithNoMetadataReferencesHasIncompleteReferences()
        {
            var workspace = new AdhocWorkspace();
            var project = workspace.AddProject("CSharpProject", LanguageNames.CSharp);
            Assert.False(project.HasSuccessfullyLoadedAsync().Result);
            Assert.Empty(project.GetCompilationAsync().Result.ExternalReferences);
        }

        [Fact]
        public void TestProjectWithNoBrokenReferencesHasNoIncompleteReferences()
        {
            var workspace = new AdhocWorkspace();
            var project1 = workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "CSharpProject",
                    "CSharpProject",
                    LanguageNames.CSharp,
                    metadataReferences: new[] { MscorlibRef }));
            var project2 = workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "VisualBasicProject",
                    "VisualBasicProject",
                    LanguageNames.VisualBasic,
                    metadataReferences: new[] { MscorlibRef },
                    projectReferences: new[] { new ProjectReference(project1.Id) }));

            // Nothing should have incomplete references, and everything should build
            Assert.True(project1.HasSuccessfullyLoadedAsync().Result);
            Assert.True(project2.HasSuccessfullyLoadedAsync().Result);
            Assert.Equal(2, project2.GetCompilationAsync().Result.ExternalReferences.Length);
        }

        [Fact]
        public void TestProjectWithBrokenCrossLanguageReferenceHasIncompleteReferences()
        {
            var workspace = new AdhocWorkspace();
            var project1 = workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "CSharpProject",
                    "CSharpProject",
                    LanguageNames.CSharp,
                    metadataReferences: new[] { MscorlibRef }));
            workspace.AddDocument(project1.Id, "Broken.cs", SourceText.From("class "));

            var project2 = workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "VisualBasicProject",
                    "VisualBasicProject",
                    LanguageNames.VisualBasic,
                    metadataReferences: new[] { MscorlibRef },
                    projectReferences: new[] { new ProjectReference(project1.Id) }));

            Assert.True(project1.HasSuccessfullyLoadedAsync().Result);
            Assert.False(project2.HasSuccessfullyLoadedAsync().Result);
            Assert.Single(project2.GetCompilationAsync().Result.ExternalReferences);
        }

        [Fact]
        public async Task TestFrozenPartialProjectHasDifferentSemanticVersions_AddedDoc()
        {
            using var workspace = WorkspaceTestUtilities.CreateWorkspaceWithPartialSemantics();
            var project = workspace.CurrentSolution.AddProject("CSharpProject", "CSharpProject", LanguageNames.CSharp);
            project = project.AddDocument("Extra.cs", SourceText.From("class Extra { }")).Project;

            var documentToFreeze = project.AddDocument("DocumentToFreeze.cs", SourceText.From(""));
            var frozenDocument = documentToFreeze.WithFrozenPartialSemantics(CancellationToken.None);

            // Because we had no compilation produced yet, we expect that only the DocumentToFreeze is in the compilation
            Assert.NotSame(frozenDocument, documentToFreeze);
            var tree = Assert.Single((await frozenDocument.Project.GetCompilationAsync()).SyntaxTrees);
            Assert.Equal("DocumentToFreeze.cs", tree.FilePath);

            // Versions should be different
            Assert.NotEqual(
                await documentToFreeze.Project.GetDependentSemanticVersionAsync(),
                await frozenDocument.Project.GetDependentSemanticVersionAsync());

            Assert.NotEqual(
                await documentToFreeze.Project.GetSemanticVersionAsync(),
                await frozenDocument.Project.GetSemanticVersionAsync());
        }

        [Fact]
        public async Task TestFrozenPartialProjectHasDifferentSemanticVersions_ChangedDoc1()
        {
            using var workspace = WorkspaceTestUtilities.CreateWorkspaceWithPartialSemantics();
            var project = workspace.CurrentSolution.AddProject("CSharpProject", "CSharpProject", LanguageNames.CSharp);
            project = project.AddDocument("Extra.cs", SourceText.From("class Extra { }")).Project;

            var documentToFreezeOriginal = project.AddDocument("DocumentToFreeze.cs", SourceText.From("class DocumentToFreeze { void M() { } }"));
            project = documentToFreezeOriginal.Project;
            var compilation = await project.GetCompilationAsync();

            var solution = project.Solution.WithDocumentText(documentToFreezeOriginal.Id, SourceText.From("class DocumentToFreeze { void M() { /*no top level change*/ } }"));
            var documentToFreezeChanged = solution.GetDocument(documentToFreezeOriginal.Id);
            var tree = await documentToFreezeChanged.GetSyntaxTreeAsync();

            var frozenDocument = documentToFreezeChanged.WithFrozenPartialSemantics(CancellationToken.None);

            Assert.NotSame(frozenDocument, documentToFreezeChanged);

            // Versions should the same since there wasn't a top level change different
            Assert.Equal(
                await documentToFreezeOriginal.GetTopLevelChangeTextVersionAsync(),
                await frozenDocument.GetTopLevelChangeTextVersionAsync());

            Assert.Equal(
                await documentToFreezeChanged.GetTopLevelChangeTextVersionAsync(),
                await frozenDocument.GetTopLevelChangeTextVersionAsync());

            Assert.Equal(
                await documentToFreezeOriginal.Project.GetDependentSemanticVersionAsync(),
                await frozenDocument.Project.GetDependentSemanticVersionAsync());

            Assert.Equal(
                await documentToFreezeChanged.Project.GetDependentSemanticVersionAsync(),
                await frozenDocument.Project.GetDependentSemanticVersionAsync());

            Assert.Equal(
                await documentToFreezeOriginal.Project.GetSemanticVersionAsync(),
                await frozenDocument.Project.GetSemanticVersionAsync());

            Assert.Equal(
                await documentToFreezeChanged.Project.GetSemanticVersionAsync(),
                await frozenDocument.Project.GetSemanticVersionAsync());
        }

        [Fact]
        public async Task TestFrozenPartialProjectHasDifferentSemanticVersions_ChangedDoc2()
        {
            using var workspace = WorkspaceTestUtilities.CreateWorkspaceWithPartialSemantics();
            var project = workspace.CurrentSolution.AddProject("CSharpProject", "CSharpProject", LanguageNames.CSharp);
            project = project.AddDocument("Extra.cs", SourceText.From("class Extra { }")).Project;

            var documentToFreezeOriginal = project.AddDocument("DocumentToFreeze.cs", SourceText.From("class DocumentToFreeze { void M() { } }"));
            project = documentToFreezeOriginal.Project;
            var compilation = await project.GetCompilationAsync();

            var solution = project.Solution.WithDocumentText(documentToFreezeOriginal.Id, SourceText.From("class DocumentToFreeze { void M() { } public void NewMethod() { } }"));
            var documentToFreezeChanged = solution.GetDocument(documentToFreezeOriginal.Id);
            var tree = await documentToFreezeChanged.GetSyntaxTreeAsync();

            var frozenDocument = documentToFreezeChanged.WithFrozenPartialSemantics(CancellationToken.None);

            Assert.NotSame(frozenDocument, documentToFreezeChanged);

            // Before/after the change must always result in a top level change.
            // After the change, we should get the same version between the doc and its frozen version.
            Assert.NotEqual(
                await documentToFreezeOriginal.GetTopLevelChangeTextVersionAsync(),
                await frozenDocument.GetTopLevelChangeTextVersionAsync());

            Assert.Equal(
                await documentToFreezeChanged.GetTopLevelChangeTextVersionAsync(),
                await frozenDocument.GetTopLevelChangeTextVersionAsync());

            Assert.NotEqual(
                await documentToFreezeOriginal.Project.GetDependentSemanticVersionAsync(),
                await frozenDocument.Project.GetDependentSemanticVersionAsync());

            Assert.Equal(
                await documentToFreezeChanged.Project.GetDependentSemanticVersionAsync(),
                await frozenDocument.Project.GetDependentSemanticVersionAsync());

            Assert.NotEqual(
                await documentToFreezeOriginal.Project.GetSemanticVersionAsync(),
                await frozenDocument.Project.GetSemanticVersionAsync());

            Assert.Equal(
                await documentToFreezeChanged.Project.GetSemanticVersionAsync(),
                await frozenDocument.Project.GetSemanticVersionAsync());
        }

        [Fact]
        public void TestFrozenPartialProjectAlwaysIsIncomplete()
        {
            var workspace = new AdhocWorkspace();
            var project1 = workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "CSharpProject",
                    "CSharpProject",
                    LanguageNames.CSharp,
                    metadataReferences: new[] { MscorlibRef }));

            var project2 = workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "VisualBasicProject",
                    "VisualBasicProject",
                    LanguageNames.VisualBasic,
                    metadataReferences: new[] { MscorlibRef },
                    projectReferences: new[] { new ProjectReference(project1.Id) }));

            var document = workspace.AddDocument(project2.Id, "Test.cs", SourceText.From(""));

            // Nothing should have incomplete references, and everything should build
            var frozenSolution = document.WithFrozenPartialSemantics(CancellationToken.None).Project.Solution;

            Assert.True(frozenSolution.GetProject(project1.Id).HasSuccessfullyLoadedAsync().Result);
            Assert.True(frozenSolution.GetProject(project2.Id).HasSuccessfullyLoadedAsync().Result);
        }

        [Fact]
        public async Task TestFrozenPartialSemanticsProjectDoesNotHaveAdditionalDocumentsFromInProgressChange()
        {
            using var workspace = CreateWorkspaceWithPartialSemantics();
            var project = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp)
                .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project;

            // Fetch the compilation to ensure further changes produce in progress states
            var originalCompilation = await project.GetCompilationAsync();
            project = project.AddAdditionalDocument("Test.txt", "").Project;

            // Freeze semantics -- this should give us a compilation and state that don't include the additional file,
            // since the compilation won't represent that either
            var frozenDocument = project.Documents.Single().WithFrozenPartialSemantics(CancellationToken.None);

            Assert.Empty(frozenDocument.Project.AdditionalDocuments);
        }

        [Fact]
        public async Task TestFrozenPartialSemanticsNoCompilationYetBuilt()
        {
            using var workspace = CreateWorkspaceWithPartialSemantics();
            var project = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp)
                .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project
                .AddDocument("RegularDocument2.cs", "// Source File", filePath: "RegularDocument2.cs").Project;

            // Freeze semantics -- that document should be there, but nothing else will be yet.
            var frozenDocument = project.Documents.First().WithFrozenPartialSemantics(CancellationToken.None);

            Assert.Single(frozenDocument.Project.Documents);
            var singleTree = Assert.Single((await frozenDocument.Project.GetCompilationAsync()).SyntaxTrees);
            Assert.Same(await frozenDocument.GetSyntaxTreeAsync(), singleTree);
        }

        [Fact]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1467404")]
        public async Task TestFrozenPartialSemanticsHandlesDocumentWithSamePathBeingRemovedAndAdded()
        {
            using var workspace = CreateWorkspaceWithPartialSemantics();
            var project = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp)
                .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project;

            // Fetch the compilation to ensure further changes produce in progress states
            var originalCompilation = await project.GetCompilationAsync();
            project = project.RemoveDocument(project.DocumentIds.Single())
                .AddDocument("RegularDocument.cs", "// Source File", filePath: "RegularDocument.cs").Project;

            // Freeze semantics -- with the new document; this should still give us a project with a single document, the previous
            // tree having been removed.
            var frozenDocument = project.Documents.Single().WithFrozenPartialSemantics(CancellationToken.None);

            Assert.Single(frozenDocument.Project.Documents);
            var singleTree = Assert.Single((await frozenDocument.Project.GetCompilationAsync()).SyntaxTrees);
            Assert.Same(await frozenDocument.GetSyntaxTreeAsync(), singleTree);
        }

        [Fact]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1467404")]
        public async Task TestFrozenPartialSemanticsHandlesRemoveAndAddWithNullPathAndDifferentNames()
        {
            using var workspace = CreateWorkspaceWithPartialSemantics();
            var project = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp)
                .AddDocument("RegularDocument.cs", "// Source File", filePath: null).Project;

            // Fetch the compilation to ensure further changes produce in progress states
            var originalCompilation = await project.GetCompilationAsync();
            project = project.RemoveDocument(project.DocumentIds.Single())
                .AddDocument("RegularDocument2.cs", "// Source File", filePath: null).Project;

            // Freeze semantics -- with the new document; this should still give us a project with two documents: the new
            // one will be added, and the old one will stay around since the name differed.
            var frozenDocument = project.Documents.Single().WithFrozenPartialSemantics(CancellationToken.None);

            Assert.Equal(2, frozenDocument.Project.Documents.Count());
            var treesInCompilation = (await frozenDocument.Project.GetCompilationAsync()).SyntaxTrees;
            Assert.Equal(2, treesInCompilation.Count());

            foreach (var document in frozenDocument.Project.Documents)
                Assert.Contains(await document.GetRequiredSyntaxTreeAsync(CancellationToken.None), treesInCompilation);
        }

        [Fact]
        public async Task TestFrozenPartialSemanticsAfterSingleTextEdit()
        {
            using var workspace = CreateWorkspaceWithPartialSemantics();
            var document = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp)
                .AddDocument("RegularDocument.cs", "// Source File", filePath: null);

            // Fetch the compilation to ensure further changes produce in progress states
            var originalCompilation = await document.Project.GetCompilationAsync();
            document = document.WithText(SourceText.From("// Source File with Changes"));

            var frozenDocument = document.WithFrozenPartialSemantics(CancellationToken.None);

            Assert.Contains(await frozenDocument.GetSyntaxTreeAsync(), (await frozenDocument.Project.GetCompilationAsync()).SyntaxTrees);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestFrozenPartialSemanticsWithMulitipleUnrelatedEdits([CombinatorialValues(1, 2, 3)] int documentToFreeze)
        {
            using var workspace = CreateWorkspaceWithPartialSemantics();
            var solution = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp).Solution;

            var documentId1 = DocumentId.CreateNewId(solution.ProjectIds.Single());
            var documentId2 = DocumentId.CreateNewId(solution.ProjectIds.Single());
            var documentId3 = DocumentId.CreateNewId(solution.ProjectIds.Single());

            solution = solution
                .AddDocument(documentId1, nameof(documentId1), "// Document 1")
                .AddDocument(documentId2, nameof(documentId2), "// Document 2")
                .AddDocument(documentId3, nameof(documentId3), "// Document 3");

            // Fetch the compilation to ensure further changes produce in progress states
            var originalCompilation = await solution.Projects.Single().GetCompilationAsync();

            solution = solution
                .WithDocumentText(documentId1, SourceText.From("// Document 1 Changed"))
                .WithDocumentText(documentId2, SourceText.From("// Document 2 Changed"))
                .WithDocumentText(documentId3, SourceText.From("// Document 3 Changed"));

            // We will freeze the appropriate document -- the reason we have three here is the code path might work accidentally if it
            // was the first or last change made, so covering "first", "middle" and "last" ensures that's covered.
            var documentIdToFreeze = documentToFreeze == 1 ? documentId1 : documentToFreeze == 2 ? documentId2 : documentId3;

            var frozen = solution.GetRequiredDocument(documentIdToFreeze).WithFrozenPartialSemantics(CancellationToken.None);

            var tree = await frozen.GetSyntaxTreeAsync();
            Assert.Contains("Changed", tree.ToString());
            Assert.Contains(tree, (await frozen.Project.GetCompilationAsync()).SyntaxTrees);
        }

        [Fact]
        public void TestProjectCompletenessWithMultipleProjects()
        {
            GetMultipleProjects(out var csBrokenProject, out var vbNormalProject, out var dependsOnBrokenProject, out var dependsOnVbNormalProject, out var transitivelyDependsOnBrokenProjects, out var transitivelyDependsOnNormalProjects);

            // check flag for a broken project itself
            Assert.False(csBrokenProject.HasSuccessfullyLoadedAsync().Result);

            // check flag for a normal project itself
            Assert.True(vbNormalProject.HasSuccessfullyLoadedAsync().Result);

            // check flag for normal project that directly reference a broken project
            Assert.True(dependsOnBrokenProject.HasSuccessfullyLoadedAsync().Result);

            // check flag for normal project that directly reference only normal project
            Assert.True(dependsOnVbNormalProject.HasSuccessfullyLoadedAsync().Result);

            // check flag for normal project that indirectly reference a borken project
            // normal project -> normal project -> broken project
            Assert.True(transitivelyDependsOnBrokenProjects.HasSuccessfullyLoadedAsync().Result);

            // check flag for normal project that indirectly reference only normal project
            // normal project -> normal project -> normal project
            Assert.True(transitivelyDependsOnNormalProjects.HasSuccessfullyLoadedAsync().Result);
        }

        private sealed class TestSmallFileTextLoader : FileTextLoader
        {
            public TestSmallFileTextLoader(string path, Encoding encoding)
                : base(path, encoding)
            {
            }

            // set max file length to 1 byte
            internal override int MaxFileLength => 1;
        }

        [Fact]
        public async Task TestMassiveFileSize()
        {
            using var root = new TempRoot();
            var file = root.CreateFile(prefix: "massiveFile", extension: ".cs").WriteAllText("hello");

            var loader = new TestSmallFileTextLoader(file.Path, Encoding.UTF8);

            var textLength = FileUtilities.GetFileLength(file.Path);

            var expected = string.Format(WorkspacesResources.File_0_size_of_1_exceeds_maximum_allowed_size_of_2, file.Path, textLength, 1);
            var exceptionThrown = false;

            try
            {
                // test async one
                var unused = await loader.LoadTextAndVersionAsync(new LoadTextOptions(SourceHashAlgorithms.Default), CancellationToken.None);
            }
            catch (InvalidDataException ex)
            {
                exceptionThrown = true;
                Assert.Equal(expected, ex.Message);
            }

            Assert.True(exceptionThrown);

            exceptionThrown = false;
            try
            {
                // test sync one
                var unused = loader.LoadTextAndVersionSynchronously(new LoadTextOptions(SourceHashAlgorithms.Default), CancellationToken.None);
            }
            catch (InvalidDataException ex)
            {
                exceptionThrown = true;
                Assert.Equal(expected, ex.Message);
            }

            Assert.True(exceptionThrown);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18697")]
        public void TestWithSyntaxTree()
        {
            // get one to get to syntax tree factory
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            var dummyProject = solution.AddProject("dummy", "dummy", LanguageNames.CSharp);

            var factory = dummyProject.Services.GetService<ISyntaxTreeFactoryService>();

            // create the origin tree
            var text = SourceText.From("// empty", encoding: null, SourceHashAlgorithms.Default);
            var strongTree = factory.ParseSyntaxTree("dummy", dummyProject.ParseOptions, text, CancellationToken.None);

            var sourceText = strongTree.GetText();
        }

        [Fact]
        public void TestUpdateDocumentsOrder()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            var pid = ProjectId.CreateNewId();

            VersionStamp GetVersion() => solution.GetProject(pid).Version;
            ImmutableArray<DocumentId> GetDocumentIds() => solution.GetProject(pid).DocumentIds.ToImmutableArray();
            ImmutableArray<SyntaxTree> GetSyntaxTrees()
            {
                return solution.GetProject(pid).GetCompilationAsync().Result.SyntaxTrees.ToImmutableArray();
            }

            solution = solution.AddProject(pid, "test", "test.dll", LanguageNames.CSharp);

            var text1 = "public class Test1 {}";
            var did1 = DocumentId.CreateNewId(pid);
            solution = solution.AddDocument(did1, "test1.cs", text1);

            var text2 = "public class Test2 {}";
            var did2 = DocumentId.CreateNewId(pid);
            solution = solution.AddDocument(did2, "test2.cs", text2);

            var text3 = "public class Test3 {}";
            var did3 = DocumentId.CreateNewId(pid);
            solution = solution.AddDocument(did3, "test3.cs", text3);

            var text4 = "public class Test4 {}";
            var did4 = DocumentId.CreateNewId(pid);
            solution = solution.AddDocument(did4, "test4.cs", text4);

            var text5 = "public class Test5 {}";
            var did5 = DocumentId.CreateNewId(pid);
            solution = solution.AddDocument(did5, "test5.cs", text5);

            var oldVersion = GetVersion();

            solution = solution.WithProjectDocumentsOrder(pid, ImmutableList.CreateRange(new[] { did5, did4, did3, did2, did1 }));

            var newVersion = GetVersion();

            // Make sure we have a new version because the order changed.
            Assert.NotEqual(oldVersion, newVersion);

            var documentIds = GetDocumentIds();

            Assert.Equal(did5, documentIds[0]);
            Assert.Equal(did4, documentIds[1]);
            Assert.Equal(did3, documentIds[2]);
            Assert.Equal(did2, documentIds[3]);
            Assert.Equal(did1, documentIds[4]);

            var syntaxTrees = GetSyntaxTrees();

            Assert.Equal(documentIds.Count(), syntaxTrees.Count());

            Assert.Equal("test5.cs", syntaxTrees[0].FilePath, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("test4.cs", syntaxTrees[1].FilePath, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("test3.cs", syntaxTrees[2].FilePath, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("test2.cs", syntaxTrees[3].FilePath, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("test1.cs", syntaxTrees[4].FilePath, StringComparer.OrdinalIgnoreCase);

            solution = solution.WithProjectDocumentsOrder(pid, ImmutableList.CreateRange(new[] { did5, did4, did3, did2, did1 }));

            var newSameVersion = GetVersion();

            // Make sure we have the same new version because the order hasn't changed.
            Assert.Equal(newVersion, newSameVersion);
        }

        [Fact]
        public void TestUpdateDocumentsOrderExceptions()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            var pid = ProjectId.CreateNewId();

            solution = solution.AddProject(pid, "test", "test.dll", LanguageNames.CSharp);

            var text1 = "public class Test1 {}";
            var did1 = DocumentId.CreateNewId(pid);
            solution = solution.AddDocument(did1, "test1.cs", text1);

            var text2 = "public class Test2 {}";
            var did2 = DocumentId.CreateNewId(pid);
            solution = solution.AddDocument(did2, "test2.cs", text2);

            var text3 = "public class Test3 {}";
            var did3 = DocumentId.CreateNewId(pid);
            solution = solution.AddDocument(did3, "test3.cs", text3);

            var text4 = "public class Test4 {}";
            var did4 = DocumentId.CreateNewId(pid);
            solution = solution.AddDocument(did4, "test4.cs", text4);

            var text5 = "public class Test5 {}";
            var did5 = DocumentId.CreateNewId(pid);
            solution = solution.AddDocument(did5, "test5.cs", text5);

            solution = solution.RemoveDocument(did5);

            Assert.Throws<ArgumentException>(() => solution = solution.WithProjectDocumentsOrder(pid, ImmutableList.Create<DocumentId>()));
            Assert.Throws<ArgumentNullException>(() => solution = solution.WithProjectDocumentsOrder(pid, null));
            Assert.Throws<InvalidOperationException>(() => solution = solution.WithProjectDocumentsOrder(pid, ImmutableList.CreateRange(new[] { did5, did3, did2, did1 })));
            Assert.Throws<ArgumentException>(() => solution = solution.WithProjectDocumentsOrder(pid, ImmutableList.CreateRange(new[] { did3, did2, did1 })));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAddingEditorConfigFileWithDiagnosticSeverity([CombinatorialValues(LanguageNames.CSharp, LanguageNames.VisualBasic)] string languageName)
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            var extension = languageName == LanguageNames.CSharp ? ".cs" : ".vb";
            var projectId = ProjectId.CreateNewId();
            var sourceDocumentId = DocumentId.CreateNewId(projectId);

            solution = solution.AddProject(projectId, "Test", "Test.dll", languageName);
            solution = solution.AddDocument(sourceDocumentId, "Test" + extension, "", filePath: @"Z:\Test" + extension);

            var originalSyntaxTree = await solution.GetDocument(sourceDocumentId).GetSyntaxTreeAsync();
            var originalCompilation = await solution.GetProject(projectId).GetCompilationAsync();

            var editorConfigDocumentId = DocumentId.CreateNewId(projectId);
            solution = solution.AddAnalyzerConfigDocuments(ImmutableArray.Create(
                DocumentInfo.Create(
                    editorConfigDocumentId,
                    ".editorconfig",
                    filePath: @"Z:\.editorconfig",
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From("[*.*]\r\n\r\ndotnet_diagnostic.CA1234.severity = error"), VersionStamp.Default)))));

            var newSyntaxTree = await solution.GetDocument(sourceDocumentId).GetSyntaxTreeAsync();
            var project = solution.GetProject(projectId);
            var newCompilation = await project.GetCompilationAsync();

            Assert.Same(originalSyntaxTree, newSyntaxTree);
            Assert.NotSame(originalCompilation, newCompilation);
            Assert.NotEqual(originalCompilation.Options, newCompilation.Options);

            var provider = project.CompilationOptions.SyntaxTreeOptionsProvider;
            Assert.True(provider.TryGetDiagnosticValue(newSyntaxTree, "CA1234", CancellationToken.None, out var severity));
            Assert.Equal(ReportDiagnostic.Error, severity);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAddingAndRemovingEditorConfigFileWithDiagnosticSeverity([CombinatorialValues(LanguageNames.CSharp, LanguageNames.VisualBasic)] string languageName)
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            var extension = languageName == LanguageNames.CSharp ? ".cs" : ".vb";
            var projectId = ProjectId.CreateNewId();
            var sourceDocumentId = DocumentId.CreateNewId(projectId);

            solution = solution.AddProject(projectId, "Test", "Test.dll", languageName);
            solution = solution.AddDocument(sourceDocumentId, "Test" + extension, "", filePath: @"Z:\Test" + extension);

            var editorConfigDocumentId = DocumentId.CreateNewId(projectId);
            solution = solution.AddAnalyzerConfigDocuments(ImmutableArray.Create(
                DocumentInfo.Create(
                    editorConfigDocumentId,
                    ".editorconfig",
                    filePath: @"Z:\.editorconfig",
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From("[*.*]\r\n\r\ndotnet_diagnostic.CA1234.severity = error"), VersionStamp.Default)))));

            var syntaxTreeAfterAddingEditorConfig = await solution.GetDocument(sourceDocumentId).GetSyntaxTreeAsync();

            var project = solution.GetProject(projectId);

            var provider = project.CompilationOptions.SyntaxTreeOptionsProvider;
            Assert.True(provider.TryGetDiagnosticValue(syntaxTreeAfterAddingEditorConfig, "CA1234", CancellationToken.None, out var severity));
            Assert.Equal(ReportDiagnostic.Error, severity);

            solution = solution.RemoveAnalyzerConfigDocument(editorConfigDocumentId);
            project = solution.GetProject(projectId);

            var syntaxTreeAfterRemovingEditorConfig = await solution.GetDocument(sourceDocumentId).GetSyntaxTreeAsync();

            provider = project.CompilationOptions.SyntaxTreeOptionsProvider;
            Assert.False(provider.TryGetDiagnosticValue(syntaxTreeAfterAddingEditorConfig, "CA1234", CancellationToken.None, out _));

            var finalCompilation = await project.GetCompilationAsync();

            Assert.True(finalCompilation.ContainsSyntaxTree(syntaxTreeAfterRemovingEditorConfig));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestChangingAnEditorConfigFile([CombinatorialValues(LanguageNames.CSharp, LanguageNames.VisualBasic)] string languageName)
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            var extension = languageName == LanguageNames.CSharp ? ".cs" : ".vb";
            var projectId = ProjectId.CreateNewId();
            var sourceDocumentId = DocumentId.CreateNewId(projectId);

            solution = solution.AddProject(projectId, "Test", "Test.dll", languageName);
            solution = solution.AddDocument(sourceDocumentId, "Test" + extension, "", filePath: @"Z:\Test" + extension);

            var editorConfigDocumentId = DocumentId.CreateNewId(projectId);
            solution = solution.AddAnalyzerConfigDocuments(ImmutableArray.Create(
                DocumentInfo.Create(
                    editorConfigDocumentId,
                    ".editorconfig",
                    filePath: @"Z:\.editorconfig",
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From("[*.*]\r\n\r\ndotnet_diagnostic.CA1234.severity = error"), VersionStamp.Default)))));

            var syntaxTreeBeforeEditorConfigChange = await solution.GetDocument(sourceDocumentId).GetSyntaxTreeAsync();

            var project = solution.GetProject(projectId);
            var provider = project.CompilationOptions.SyntaxTreeOptionsProvider;
            Assert.Equal(provider, (await project.GetCompilationAsync()).Options.SyntaxTreeOptionsProvider);
            Assert.True(provider.TryGetDiagnosticValue(syntaxTreeBeforeEditorConfigChange, "CA1234", CancellationToken.None, out var severity));
            Assert.Equal(ReportDiagnostic.Error, severity);

            solution = solution.WithAnalyzerConfigDocumentTextLoader(
                editorConfigDocumentId,
                TextLoader.From(TextAndVersion.Create(SourceText.From("[*.*]\r\n\r\ndotnet_diagnostic.CA6789.severity = error"), VersionStamp.Default)),
                PreservationMode.PreserveValue);

            var syntaxTreeAfterEditorConfigChange = await solution.GetDocument(sourceDocumentId).GetSyntaxTreeAsync();

            project = solution.GetProject(projectId);
            provider = project.CompilationOptions.SyntaxTreeOptionsProvider;
            Assert.Equal(provider, (await project.GetCompilationAsync()).Options.SyntaxTreeOptionsProvider);
            Assert.True(provider.TryGetDiagnosticValue(syntaxTreeBeforeEditorConfigChange, "CA6789", CancellationToken.None, out severity));
            Assert.Equal(ReportDiagnostic.Error, severity);

            var finalCompilation = await project.GetCompilationAsync();

            Assert.True(finalCompilation.ContainsSyntaxTree(syntaxTreeAfterEditorConfigChange));
        }

        [Fact]
        public void TestAddingAndRemovingGlobalEditorConfigFileWithDiagnosticSeverity()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            var projectId = ProjectId.CreateNewId();
            var sourceDocumentId = DocumentId.CreateNewId(projectId);

            solution = solution.AddProject(projectId, "Test", "Test.dll", LanguageNames.CSharp);
            solution = solution.AddDocument(sourceDocumentId, "Test.cs", "", filePath: @"Z:\Test.cs");

            var originalProvider = solution.GetProject(projectId).CompilationOptions.SyntaxTreeOptionsProvider;
            Assert.False(originalProvider.TryGetGlobalDiagnosticValue("CA1234", default, out _));

            var editorConfigDocumentId = DocumentId.CreateNewId(projectId);
            solution = solution.AddAnalyzerConfigDocuments(ImmutableArray.Create(
                DocumentInfo.Create(
                    editorConfigDocumentId,
                    ".globalconfig",
                    filePath: @"Z:\.globalconfig",
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From("is_global = true\r\n\r\ndotnet_diagnostic.CA1234.severity = error"), VersionStamp.Default)))));

            var newProvider = solution.GetProject(projectId).CompilationOptions.SyntaxTreeOptionsProvider;
            Assert.True(newProvider.TryGetGlobalDiagnosticValue("CA1234", default, out var severity));
            Assert.Equal(ReportDiagnostic.Error, severity);

            solution = solution.RemoveAnalyzerConfigDocument(editorConfigDocumentId);
            var finalProvider = solution.GetProject(projectId).CompilationOptions.SyntaxTreeOptionsProvider;
            Assert.False(finalProvider.TryGetGlobalDiagnosticValue("CA1234", default, out _));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/3705")]
        public async Task TestAddingEditorConfigFileWithIsGeneratedCodeOption()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            var projectId = ProjectId.CreateNewId();
            var sourceDocumentId = DocumentId.CreateNewId(projectId);

            solution = solution.AddProject(projectId, "Test", "Test.dll", LanguageNames.CSharp)
                .WithProjectMetadataReferences(projectId, new[] { TestMetadata.Net451.mscorlib })
                .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(NullableContextOptions.Enable));
            var src = @"
class C
{
    void M(C? c)
    {
        _ = c.ToString();   // warning CS8602: Dereference of a possibly null reference.
    }
}";
            solution = solution.AddDocument(sourceDocumentId, "Test.cs", src, filePath: @"Z:\Test.cs");

            var originalSyntaxTree = await solution.GetDocument(sourceDocumentId).GetSyntaxTreeAsync();
            var originalCompilation = await solution.GetProject(projectId).GetCompilationAsync();

            // warning CS8602: Dereference of a possibly null reference.
            var diagnostics = originalCompilation.GetDiagnostics();
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("CS8602", diagnostic.Id);

            var editorConfigDocumentId = DocumentId.CreateNewId(projectId);
            solution = solution.AddAnalyzerConfigDocuments(ImmutableArray.Create(
                DocumentInfo.Create(
                    editorConfigDocumentId,
                    ".editorconfig",
                    filePath: @"Z:\.editorconfig",
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From("[*.*]\r\n\r\ngenerated_code = true"), VersionStamp.Default)))));

            var newSyntaxTree = await solution.GetDocument(sourceDocumentId).GetSyntaxTreeAsync();
            var newCompilation = await solution.GetProject(projectId).GetCompilationAsync();

            Assert.Same(originalSyntaxTree, newSyntaxTree);
            Assert.NotSame(originalCompilation, newCompilation);
            Assert.NotEqual(originalCompilation.Options, newCompilation.Options);

            // warning CS8669: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            // Auto-generated code requires an explicit '#nullable' directive in source.
            diagnostics = newCompilation.GetDiagnostics();
            diagnostic = Assert.Single(diagnostics);
            Assert.Contains("CS8669", diagnostic.Id);
        }

        [Fact]
        public void NoCompilationProjectsHaveNullSyntaxTreesAndSemanticModels()
        {
            using var workspace = CreateWorkspace(new[] { typeof(NoCompilationLanguageService) });
            var solution = workspace.CurrentSolution;
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            solution = solution.AddProject(projectId, "Test", "Test.dll", NoCompilationConstants.LanguageName);
            solution = solution.AddDocument(documentId, "Test.cs", "", filePath: @"Z:\Test.txt");

            var document = solution.GetDocument(documentId)!;

            Assert.False(document.TryGetSyntaxTree(out _));
            Assert.Null(document.GetSyntaxTreeAsync().Result);
            Assert.Null(document.GetSyntaxTreeSynchronously(CancellationToken.None));

            Assert.False(document.TryGetSemanticModel(out _));
            Assert.Null(document.GetSemanticModelAsync().Result);
        }

        [Fact]
        public void ChangingFilePathOfFileInNoCompilationProjectWorks()
        {
            using var workspace = CreateWorkspace(new[] { typeof(NoCompilationLanguageService) });
            var solution = workspace.CurrentSolution;
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            solution = solution.AddProject(projectId, "Test", "Test.dll", NoCompilationConstants.LanguageName);
            solution = solution.AddDocument(documentId, "Test.cs", "", filePath: @"Z:\Test.txt");

            Assert.Null(solution.GetDocument(documentId)!.GetSyntaxTreeAsync().Result);

            solution = solution.WithDocumentFilePath(documentId, @"Z:\NewPath.txt");

            Assert.Null(solution.GetDocument(documentId)!.GetSyntaxTreeAsync().Result);
        }

        [Fact]
        public void AddingAndRemovingProjectsUpdatesFilePathMap()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            var projectId = ProjectId.CreateNewId();
            var editorConfigDocumentId = DocumentId.CreateNewId(projectId);

            const string editorConfigFilePath = @"Z:\.editorconfig";

            var projectInfo =
                ProjectInfo.Create(projectId, VersionStamp.Default, "Test", "Test", LanguageNames.CSharp)
                    .WithAnalyzerConfigDocuments(new[] { DocumentInfo.Create(editorConfigDocumentId, ".editorconfig", filePath: editorConfigFilePath) });

            solution = solution.AddProject(projectInfo);

            Assert.Equal(editorConfigDocumentId, Assert.Single(solution.GetDocumentIdsWithFilePath(editorConfigFilePath)));

            solution = solution.RemoveProject(projectId);

            Assert.Empty(solution.GetDocumentIdsWithFilePath(editorConfigFilePath));
        }

        private static void GetMultipleProjects(
            out Project csBrokenProject,
            out Project vbNormalProject,
            out Project dependsOnBrokenProject,
            out Project dependsOnVbNormalProject,
            out Project transitivelyDependsOnBrokenProjects,
            out Project transitivelyDependsOnNormalProjects)
        {
            var workspace = new AdhocWorkspace();

            csBrokenProject = workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "CSharpProject",
                    "CSharpProject",
                    LanguageNames.CSharp,
                    metadataReferences: new[] { MscorlibRef }).WithHasAllInformation(hasAllInformation: false));

            vbNormalProject = workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "VisualBasicProject",
                    "VisualBasicProject",
                    LanguageNames.VisualBasic,
                    metadataReferences: new[] { MscorlibRef }));

            dependsOnBrokenProject = workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "VisualBasicProject",
                    "VisualBasicProject",
                    LanguageNames.VisualBasic,
                    metadataReferences: new[] { MscorlibRef },
                    projectReferences: new[] { new ProjectReference(csBrokenProject.Id), new ProjectReference(vbNormalProject.Id) }));

            dependsOnVbNormalProject = workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "CSharpProject",
                    "CSharpProject",
                    LanguageNames.CSharp,
                    metadataReferences: new[] { MscorlibRef },
                    projectReferences: new[] { new ProjectReference(vbNormalProject.Id) }));

            transitivelyDependsOnBrokenProjects = workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "CSharpProject",
                    "CSharpProject",
                    LanguageNames.CSharp,
                    metadataReferences: new[] { MscorlibRef },
                    projectReferences: new[] { new ProjectReference(dependsOnBrokenProject.Id) }));

            transitivelyDependsOnNormalProjects = workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "VisualBasicProject",
                    "VisualBasicProject",
                    LanguageNames.VisualBasic,
                    metadataReferences: new[] { MscorlibRef },
                    projectReferences: new[] { new ProjectReference(dependsOnVbNormalProject.Id) }));
        }

        [Fact]
        public void TestOptionChangesForLanguagesNotInSolution()
        {
            // Create an empty solution with no projects.
            using var workspace = CreateWorkspace();
            var s0 = workspace.CurrentSolution;
            var optionService = workspace.Services.GetRequiredService<ILegacyWorkspaceOptionService>().LegacyGlobalOptions;

            // Apply an option change to a C# option.
            var option = FormattingOptions.UseTabs;
            var defaultValue = option.DefaultValue;
            var changedValue = !defaultValue;
            var options = s0.Options.WithChangedOption(option, LanguageNames.CSharp, changedValue);

            // Verify option change is preserved even if the solution has no project with that language.
            var s1 = s0.WithOptions(options);
            VerifyOptionSet(s1.Options);

            // Verify option value is preserved on adding a project for a different language.
            var s2 = s1.AddProject("P1", "A1", LanguageNames.VisualBasic).Solution;
            VerifyOptionSet(s2.Options);

            // Verify option value is preserved on removing a project.
            var s4 = s2.RemoveProject(s2.Projects.Single(p => p.Name == "P1").Id);
            VerifyOptionSet(s4.Options);

            return;

            void VerifyOptionSet(OptionSet optionSet)
            {
                Assert.Equal(changedValue, optionSet.GetOption(option, LanguageNames.CSharp));
                Assert.Equal(defaultValue, optionSet.GetOption(option, LanguageNames.VisualBasic));
            }
        }

        [Fact]
        public async Task TestUpdatedDocumentTextIsObservablyConstantAsync()
        {
            using var workspace = CreateWorkspace();
            var pid = ProjectId.CreateNewId();
            var text = SourceText.From("public class C { }");
            var version = VersionStamp.Create();
            var docInfo = DocumentInfo.Create(DocumentId.CreateNewId(pid), "c.cs", loader: TextLoader.From(TextAndVersion.Create(text, version)));
            var projInfo = ProjectInfo.Create(
                pid,
                version: VersionStamp.Default,
                name: "TestProject",
                assemblyName: "TestProject.dll",
                language: LanguageNames.CSharp,
                documents: new[] { docInfo });

            var solution = workspace.CurrentSolution.AddProject(projInfo);
            var doc = solution.GetDocument(docInfo.Id);

            // change document
            var root = await doc.GetSyntaxRootAsync();
            var newRoot = root.WithAdditionalAnnotations(new SyntaxAnnotation());
            Assert.NotSame(root, newRoot);
            var newDoc = doc.Project.Solution.WithDocumentSyntaxRoot(doc.Id, newRoot).GetDocument(doc.Id);
            Assert.NotSame(doc, newDoc);

            var newDocText = await newDoc.GetTextAsync();
            var sameText = await newDoc.GetTextAsync();
            Assert.Same(newDocText, sameText);

            var newDocTree = await newDoc.GetSyntaxTreeAsync();
            var treeText = newDocTree.GetText();
            Assert.Same(newDocText, treeText);
        }

        [Fact]
        public async Task ReplacingTextMultipleTimesDoesNotRootIntermediateCopiesIfCompilationNotAskedFor()
        {
            // This test replicates the pattern of some operation changing a bunch of files, but the files aren't kept open.
            // In Visual Studio we do large refactorings by opening files with an invisible editor, making changes, and closing
            // again. This process means we'll queue up intermediate changes to those files, but we don't want to hold onto
            // the intermediate edits when we don't really need to since the final version will be all that matters.

            using var workspace = CreateWorkspaceWithProjectAndDocuments();

            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.Single().DocumentIds.Single();

            // Fetch the compilation, so further edits are going to be incremental updates of this one
            var originalCompilation = await solution.Projects.Single().GetCompilationAsync();

            // Create a source text we'll release and ensure it disappears. We'll also make sure we don't accidentally root
            // that solution in the middle.
            var sourceTextToRelease = ObjectReference.CreateFromFactory(static () => SourceText.From(Guid.NewGuid().ToString()));
            var solutionWithSourceTextToRelease = sourceTextToRelease.GetObjectReference(
                static (sourceText, document) => document.Project.Solution.WithDocumentText(document.Id, sourceText, PreservationMode.PreserveIdentity),
                solution.GetDocument(documentId));

            // Change it again, this time by editing the text loader; this replicates us closing a file, and we don't want to pin the changes from the
            // prior change.
            var finalSolution = solutionWithSourceTextToRelease.GetObjectReference(
                static (s, documentId) => s.WithDocumentTextLoader(documentId, new TestTextLoader(Guid.NewGuid().ToString()), PreservationMode.PreserveValue), documentId).GetReference();

            // The text in the middle shouldn't be held at all, since we replaced it.
            solutionWithSourceTextToRelease.ReleaseStrongReference();
            sourceTextToRelease.AssertReleased();

            GC.KeepAlive(finalSolution);
        }

        [Theory]
        [InlineData("a/proj.csproj", "a/.editorconfig", "a/b/test.cs", "*.cs", true, true)]
        [InlineData("a/proj.csproj", "a/b/.editorconfig", "a/b/test.cs", "*.cs", false, true)]
        [InlineData("a/proj.csproj", "a/.editorconfig", null, "*.cs", true, true)]
        [InlineData("a/proj.csproj", "a/b/.editorconfig", null, "*.cs", false, false)]
        [InlineData("a/proj.csproj", "a/.editorconfig", "", "*.cs", true, true)]
        [InlineData("a/proj.csproj", "a/b/.editorconfig", "", "*.cs", false, false)]
        [InlineData(null, "a/.editorconfig", "a/b/test.cs", "*.cs", false, true)]
        [InlineData(null, "a/.editorconfig", null, "*.cs", false, false)]
        [InlineData("a/proj.csproj", "a/.editorconfig", null, "*test.cs", false, true)]
        public async Task EditorConfigOptions(string projectPath, string configPath, string sourcePath, string pattern, bool appliedToEntireProject, bool appliedToDocument)
        {
            projectPath = string.IsNullOrEmpty(projectPath) ? projectPath : Path.Combine(TempRoot.Root, projectPath);
            configPath = Path.Combine(TempRoot.Root, configPath);
            sourcePath = string.IsNullOrEmpty(sourcePath) ? sourcePath : Path.Combine(TempRoot.Root, sourcePath);

            using var workspace = CreateWorkspace();
            var projectId = ProjectId.CreateNewId();

            var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Default,
                name: "proj1",
                assemblyName: "proj1.dll",
                language: LanguageNames.CSharp,
                filePath: projectPath);

            var documentId = DocumentId.CreateNewId(projectId);

            var solution = workspace.CurrentSolution
                .AddProject(projectInfo)
                .AddDocument(documentId, "test.cs", SourceText.From("public class C { }"), filePath: sourcePath)
                .AddAnalyzerConfigDocument(DocumentId.CreateNewId(projectId), ".editorconfig", SourceText.From($"[{pattern}]\nindent_style = tab"), filePath: configPath);

            var document = solution.GetRequiredDocument(documentId);

#pragma warning disable RS0030 // Do not used banned APIs
            var documentOptions = await document.GetOptionsAsync(CancellationToken.None);
            Assert.Equal(appliedToDocument, documentOptions.GetOption(FormattingOptions.UseTabs));
#pragma warning restore

            var syntaxTree = await document.GetSyntaxTreeAsync();
            var documentOptionsViaSyntaxTree = document.Project.State.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
            Assert.Equal(appliedToDocument, documentOptionsViaSyntaxTree.TryGetValue("indent_style", out var value) == true && value == "tab");

            var projectOptions = document.Project.GetAnalyzerConfigOptions();
            Assert.Equal(appliedToEntireProject, projectOptions?.AnalyzerOptions.TryGetValue("indent_style", out value) == true && value == "tab");
        }

        [Fact]
        public void GetRelatedDocumentsDoesNotReturnOtherTypesOfDocuments()
        {
            using var workspace = CreateWorkspace();

            const string FilePath = "File.cs";

            var solution = workspace.CurrentSolution
                .AddProject("TestProject", "TestProject", LanguageNames.CSharp)
                .AddDocument("File.cs", "", filePath: FilePath).Project
                .AddAdditionalDocument("File.cs", text: "", filePath: FilePath).Project.Solution;

            // GetDocumentIdsWithFilePath should return two, since it'll count all types of documents
            Assert.Equal(2, solution.GetDocumentIdsWithFilePath(FilePath).Length);

            var regularDocumentId = solution.Projects.Single().DocumentIds.Single();

            Assert.Single(solution.GetRelatedDocumentIds(regularDocumentId));
        }
    }
}
