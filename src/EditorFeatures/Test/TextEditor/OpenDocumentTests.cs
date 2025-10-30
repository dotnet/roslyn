// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.TextEditor;

[UseExportProvider]
public sealed class OpenDocumentTests
{
    [WpfFact, Trait(Traits.Feature, Traits.Features.Workspace)]
    public void LinkedFiles()
    {
        // We want to assert that if the open document is linked into multiple projects, we
        // update all documents at the same time with the changed text. Otherwise things will get
        // out of sync.
        var hostServices = EditorTestCompositions.EditorFeatures.GetHostServices();

        using var workspace = new AdhocWorkspace(hostServices);
        var textBufferFactoryService = ((IMefHostExportProvider)hostServices).GetExports<ITextBufferFactoryService>().Single().Value;
        var buffer = textBufferFactoryService.CreateTextBuffer("Hello", textBufferFactoryService.TextContentType);
        var sourceTextContainer = buffer.AsTextContainer();

        // We're going to add two projects that both consume the same file
        const string FilePath = "Z:\\Foo.cs";
        var documentIds = new List<DocumentId>();
        for (var i = 0; i < 2; i++)
        {
            var projectId = workspace.AddProject($"Project{i}", LanguageNames.CSharp).Id;
            var documentId = DocumentId.CreateNewId(projectId);
            workspace.AddDocument(DocumentInfo.Create(documentId, "Foo.cs", filePath: FilePath));
            workspace.OnDocumentOpened(documentId, sourceTextContainer);

            documentIds.Add(documentId);
        }

        // Confirm the files have been linked by file path. This isn't the core part of this test but without it
        // nothing else will work.
        AssertEx.SetEqual(documentIds, workspace.CurrentSolution.GetDocumentIdsWithFilePath(FilePath));
        Assert.Equal(documentIds.Last(), workspace.CurrentSolution.GetDocument(documentIds.First()).GetLinkedDocumentIds().Single());
        Assert.Equal(documentIds.First(), workspace.CurrentSolution.GetDocument(documentIds.Last()).GetLinkedDocumentIds().Single());

        // Now the core test: first, if we make a modified version of the source text, and attempt to get the document for it,
        // both copies should be updated.
        var originalSnapshot = buffer.CurrentSnapshot;
        buffer.Insert(5, ", World!");

        var newDocumentWithChanges = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();

        // Since we're calling this on the current snapshot and we observed the text edit synchronously,
        // no forking actually should have happened.
        Assert.Same(workspace.CurrentSolution, newDocumentWithChanges.Project.Solution);
        Assert.Equal("Hello, World!", newDocumentWithChanges.GetTextSynchronously(CancellationToken.None).ToString());
        Assert.Equal("Hello, World!", newDocumentWithChanges.GetLinkedDocuments().Single().GetTextSynchronously(CancellationToken.None).ToString());

        // Now let's fetch back for the original snapshot. Both linked copies should be updated.
        var originalDocumentWithChanges = originalSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        Assert.NotSame(workspace.CurrentSolution, originalDocumentWithChanges.Project.Solution);
        Assert.Equal("Hello", originalDocumentWithChanges.GetTextSynchronously(CancellationToken.None).ToString());
        Assert.Equal("Hello", originalDocumentWithChanges.GetLinkedDocuments().Single().GetTextSynchronously(CancellationToken.None).ToString());
    }
}
