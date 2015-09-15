// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public abstract class AbstractCodeActionTest : AbstractCodeActionOrUserDiagnosticTest
    {
        protected abstract object CreateCodeRefactoringProvider(Workspace workspace);

        protected override IList<CodeAction> GetCodeActionsWorker(TestWorkspace workspace, string fixAllActionEquivalenceKey)
        {
            return GetCodeRefactoring(workspace)?.Actions?.ToList();
        }

        internal ICodeRefactoring GetCodeRefactoring(TestWorkspace workspace)
        {
            return GetCodeRefactorings(workspace).FirstOrDefault();
        }

        private IEnumerable<ICodeRefactoring> GetCodeRefactorings(TestWorkspace workspace)
        {
            var provider = CreateCodeRefactoringProvider(workspace);
            return SpecializedCollections.SingletonEnumerable(
                GetCodeRefactoring((CodeRefactoringProvider)provider, workspace));
        }

        private CodeRefactoring GetCodeRefactoring(
            CodeRefactoringProvider provider,
            TestWorkspace workspace)
        {
            var document = GetDocument(workspace);
            var span = workspace.Documents.Single(d => !d.IsLinkFile && d.SelectedSpans.Count == 1).SelectedSpans.Single();
            var actions = new List<CodeAction>();
            var context = new CodeRefactoringContext(document, span, (a) => actions.Add(a), CancellationToken.None);
            provider.ComputeRefactoringsAsync(context).Wait();
            return actions.Count > 0 ? new CodeRefactoring(provider, actions) : null;
        }

        protected void TestActionsOnLinkedFiles(
            TestWorkspace workspace,
            string expectedText,
            int index,
            IList<CodeAction> actions,
            string expectedPreviewContents = null,
            bool compareTokens = true)
        {
            var operations = VerifyInputsAndGetOperations(index, actions);

            VerifyPreviewContents(workspace, expectedPreviewContents, operations);

            var applyChangesOperation = operations.OfType<ApplyChangesOperation>().First();
            applyChangesOperation.Apply(workspace, CancellationToken.None);

            foreach (var document in workspace.Documents)
            {
                var fixedRoot = workspace.CurrentSolution.GetDocument(document.Id).GetSyntaxRootAsync().Result;
                var actualText = compareTokens ? fixedRoot.ToString() : fixedRoot.ToFullString();

                if (compareTokens)
                {
                    TokenUtilities.AssertTokensEqual(expectedText, actualText, GetLanguage());
                }
                else
                {
                    Assert.Equal(expectedText, actualText);
                }
            }
        }

        private static void VerifyPreviewContents(TestWorkspace workspace, string expectedPreviewContents, IEnumerable<CodeActionOperation> operations)
        {
            if (expectedPreviewContents != null)
            {
                var editHandler = workspace.ExportProvider.GetExportedValue<ICodeActionEditHandlerService>();
                var content = editHandler.GetPreviews(workspace, operations, CancellationToken.None).TakeNextPreviewAsync().PumpingWaitResult();
                var diffView = content as IWpfDifferenceViewer;
                Assert.NotNull(diffView);
                var previewContents = diffView.RightView.TextBuffer.AsTextContainer().CurrentText.ToString();
                diffView.Close();

                Assert.Equal(expectedPreviewContents, previewContents);
            }
        }

        protected static Document GetDocument(TestWorkspace workspace)
        {
            return workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
        }
    }
}
