// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings
{
    public partial class PreviewTests
    {
        [WpfFact]
        public async Task TestExceptionInComputePreview()
        {
            using var workspace = CreateWorkspaceFromFile("class D {}", new TestParameters());
            await GetPreview(workspace, new ErrorCases.ExceptionInCodeAction());
        }

        [WpfFact]
        public void TestExceptionInDisplayText()
        {
            using var workspace = CreateWorkspaceFromFile("class D {}", new TestParameters());
            DisplayText(workspace, new ErrorCases.ExceptionInCodeAction());
        }

        [WpfFact]
        public async Task TestExceptionInActionSets()
        {
            using var workspace = CreateWorkspaceFromFile("class D {}", new TestParameters());
            await ActionSets(workspace, new ErrorCases.ExceptionInCodeAction());
        }

        private async Task GetPreview(TestWorkspace workspace, CodeRefactoringProvider provider)
        {
            var codeActions = new List<CodeAction>();
            RefactoringSetup(workspace, provider, codeActions, out var extensionManager, out var textBuffer);
            var suggestedAction = new CodeRefactoringSuggestedAction(
                workspace.ExportProvider.GetExportedValue<IThreadingContext>(),
                workspace.ExportProvider.GetExportedValue<SuggestedActionsSourceProvider>(),
                workspace, textBuffer, provider, codeActions.First());
            await suggestedAction.GetPreviewAsync(CancellationToken.None);
            Assert.True(extensionManager.IsDisabled(provider));
            Assert.False(extensionManager.IsIgnored(provider));
        }

        private void DisplayText(TestWorkspace workspace, CodeRefactoringProvider provider)
        {
            var codeActions = new List<CodeAction>();
            RefactoringSetup(workspace, provider, codeActions, out var extensionManager, out var textBuffer);
            var suggestedAction = new CodeRefactoringSuggestedAction(
                workspace.ExportProvider.GetExportedValue<IThreadingContext>(),
                workspace.ExportProvider.GetExportedValue<SuggestedActionsSourceProvider>(),
                workspace, textBuffer, provider, codeActions.First());
            var text = suggestedAction.DisplayText;
            Assert.True(extensionManager.IsDisabled(provider));
            Assert.False(extensionManager.IsIgnored(provider));
        }

        private async Task ActionSets(TestWorkspace workspace, CodeRefactoringProvider provider)
        {
            var codeActions = new List<CodeAction>();
            RefactoringSetup(workspace, provider, codeActions, out var extensionManager, out var textBuffer);
            var suggestedAction = new CodeRefactoringSuggestedAction(
                workspace.ExportProvider.GetExportedValue<IThreadingContext>(),
                workspace.ExportProvider.GetExportedValue<SuggestedActionsSourceProvider>(),
                workspace, textBuffer, provider, codeActions.First());
            var actionSets = await suggestedAction.GetActionSetsAsync(CancellationToken.None);
            Assert.True(extensionManager.IsDisabled(provider));
            Assert.False(extensionManager.IsIgnored(provider));
        }

        private static void RefactoringSetup(
            TestWorkspace workspace, CodeRefactoringProvider provider, List<CodeAction> codeActions,
            out EditorLayerExtensionManager.ExtensionManager extensionManager,
            out VisualStudio.Text.ITextBuffer textBuffer)
        {
            var document = GetDocument(workspace);
            textBuffer = workspace.GetTestDocument(document.Id).GetTextBuffer();
            var span = document.GetSyntaxRootAsync().Result.Span;
            var context = new CodeRefactoringContext(document, span, (a) => codeActions.Add(a), CancellationToken.None);
            provider.ComputeRefactoringsAsync(context).Wait();
            var action = codeActions.Single();
            extensionManager = document.Project.Solution.Workspace.Services.GetService<IExtensionManager>() as EditorLayerExtensionManager.ExtensionManager;
        }
    }
}
