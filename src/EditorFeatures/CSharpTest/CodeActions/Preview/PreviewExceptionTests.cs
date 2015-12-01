// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings
{
    public partial class PreviewTests
    {
        [WpfFact]
        public async Task TestExceptionInComputePreview()
        {
            using (var workspace = await CreateWorkspaceFromFileAsync("class D {}", null, null))
            {
                await GetPreview(workspace, new ErrorCases.ExceptionInCodeAction());
            }
        }

        [WpfFact]
        public async Task TestExceptionInDisplayText()
        {
            using (var workspace = await CreateWorkspaceFromFileAsync("class D {}", null, null))
            {
                DisplayText(workspace, new ErrorCases.ExceptionInCodeAction());
            }
        }

        [WpfFact]
        public async Task TestExceptionInActionSets()
        {
            using (var workspace = await CreateWorkspaceFromFileAsync("class D {}", null, null))
            {
                await ActionSets(workspace, new ErrorCases.ExceptionInCodeAction());
            }
        }

        private async Task GetPreview(TestWorkspace workspace, CodeRefactoringProvider provider)
        {
            List<CodeAction> refactorings = new List<CodeAction>();
            ICodeActionEditHandlerService editHandler;
            EditorLayerExtensionManager.ExtensionManager extensionManager;
            VisualStudio.Text.ITextBuffer textBuffer;
            RefactoringSetup(workspace, provider, refactorings, out editHandler, out extensionManager, out textBuffer);
            var suggestedAction = new CodeRefactoringSuggestedAction(workspace, textBuffer, editHandler, new TestWaitIndicator(), refactorings.First(), provider);
            await suggestedAction.GetPreviewAsync(CancellationToken.None);
            Assert.True(extensionManager.IsDisabled(provider));
            Assert.False(extensionManager.IsIgnored(provider));
        }

        private void DisplayText(TestWorkspace workspace, CodeRefactoringProvider provider)
        {
            List<CodeAction> refactorings = new List<CodeAction>();
            ICodeActionEditHandlerService editHandler;
            EditorLayerExtensionManager.ExtensionManager extensionManager;
            VisualStudio.Text.ITextBuffer textBuffer;
            RefactoringSetup(workspace, provider, refactorings, out editHandler, out extensionManager, out textBuffer);
            var suggestedAction = new CodeRefactoringSuggestedAction(workspace, textBuffer, editHandler, new TestWaitIndicator(), refactorings.First(), provider);
            var text = suggestedAction.DisplayText;
            Assert.True(extensionManager.IsDisabled(provider));
            Assert.False(extensionManager.IsIgnored(provider));
        }

        private async Task ActionSets(TestWorkspace workspace, CodeRefactoringProvider provider)
        {
            List<CodeAction> refactorings = new List<CodeAction>();
            ICodeActionEditHandlerService editHandler;
            EditorLayerExtensionManager.ExtensionManager extensionManager;
            VisualStudio.Text.ITextBuffer textBuffer;
            RefactoringSetup(workspace, provider, refactorings, out editHandler, out extensionManager, out textBuffer);
            var suggestedAction = new CodeRefactoringSuggestedAction(workspace, textBuffer, editHandler, new TestWaitIndicator(), refactorings.First(), provider);
            var actionSets = await suggestedAction.GetActionSetsAsync(CancellationToken.None);
            Assert.True(extensionManager.IsDisabled(provider));
            Assert.False(extensionManager.IsIgnored(provider));
        }

        private static void RefactoringSetup(TestWorkspace workspace, CodeRefactoringProvider provider, List<CodeAction> refactorings, out ICodeActionEditHandlerService editHandler, out EditorLayerExtensionManager.ExtensionManager extensionManager, out VisualStudio.Text.ITextBuffer textBuffer)
        {
            var document = GetDocument(workspace);
            var span = document.GetSyntaxRootAsync().Result.Span;
            var context = new CodeRefactoringContext(document, span, (a) => refactorings.Add(a), CancellationToken.None);
            provider.ComputeRefactoringsAsync(context).Wait();
            var action = refactorings.Single();
            editHandler = workspace.ExportProvider.GetExportedValue<ICodeActionEditHandlerService>();
            extensionManager = document.Project.Solution.Workspace.Services.GetService<IExtensionManager>() as EditorLayerExtensionManager.ExtensionManager;
            textBuffer = document.GetTextAsync().Result.Container.GetTextBuffer();
        }
    }
}
