// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.MoveToNamespace;
using Microsoft.CodeAnalysis.Test.Utilities.Workspaces;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.MoveToNamespace
{
    public abstract partial class AbstractMoveToNamespaceTests : AbstractCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new MoveToNamespaceCodeActionProvider();

        public async Task TestMoveToNamespaceAsync(
            string markup,
            bool expectedSuccess = true,
            string expectedMarkup = null,
            TestParameters? testParameters = null,
            string targetNamespace = null,
            bool optionCancelled = false,
            IReadOnlyDictionary<string, string> expectedSymbolChanges = null)
        {
            testParameters ??= new TestParameters();

            var moveToNamespaceOptions = optionCancelled
                ? MoveToNamespaceOptionsResult.Cancelled
                : new MoveToNamespaceOptionsResult(targetNamespace);

            var workspace = CreateWorkspaceFromFile(markup, testParameters.Value);
            using var testState = new TestState(workspace);

            testState.TestMoveToNamespaceOptionsService.SetOptions(moveToNamespaceOptions);
            if (expectedSuccess)
            {
                if (!optionCancelled && !string.IsNullOrEmpty(targetNamespace))
                {
                    await TestRefactorNotifyAsync(testState, workspace, targetNamespace, optionCancelled, expectedSymbolChanges);
                    await TestInRegularAndScriptAsync(markup, expectedMarkup);
                }
            }
            else
            {
                await TestMissingInRegularAndScriptAsync(markup, parameters: testParameters.Value);
            }
        }

        private async Task TestRefactorNotifyAsync(
            TestState testState,
            TestWorkspace workspace,
            string targetNamespace = null,
            bool optionCancelled = false,
            IReadOnlyDictionary<string, string> expectedSymbolChanges = null)
        {
            var actions = await testState.MoveToNamespaceService.GetCodeActionsAsync(
                        testState.InvocationDocument,
                        testState.TestInvocationDocument.SelectedSpans.Single(),
                        CancellationToken.None);

            var operationTasks = actions
                .Cast<AbstractMoveToNamespaceCodeAction>()
                .Select(action => action.GetOperationsAsync(action.GetOptions(CancellationToken.None), CancellationToken.None));

            var operations = new List<CodeActionOperation>();

            foreach (var task in operationTasks)
            {
                var taskOperations = await task;

                if (optionCancelled || string.IsNullOrEmpty(targetNamespace))
                {
                    Assert.Empty(taskOperations);
                }
                else
                {
                    Assert.NotEmpty(taskOperations);
                }

                operations.AddRange(taskOperations);
            }

            var refactorNotify = workspace.GetService<IRefactorNotifyService>() as TestRefactorNotify;

            var expectedRenames = expectedSymbolChanges.Select(kvp => new RenameTracking()
            {
                Original = kvp.Key,
                New = kvp.Value,
                OnBeforeCalled = false,
                OnAfterCalled = false
            }).ToImmutableArray();

            TestRefactorNotify.SymbolRenamedEventHandler beforeRename = (args) =>
            {
                var expectedRenameTracking = expectedRenames.First(r => r.New == args.NewName);
                Assert.False(expectedRenameTracking.OnBeforeCalled);
                expectedRenameTracking.OnBeforeCalled = true;
            };

            TestRefactorNotify.SymbolRenamedEventHandler afterRename = (args) =>
            {
                var expectedRenameTracking = expectedRenames.First(r => r.New == args.NewName);
                expectedRenameTracking.OnAfterCalled = true;
            };

            if (refactorNotify is object)
            {
                refactorNotify.OnBeforeRename += beforeRename;
                refactorNotify.OnAfterRename += afterRename;
            }

            foreach (var operation in operations)
            {
                operation.Apply(workspace, CancellationToken.None);
            }

            if (refactorNotify is object)
            {
                refactorNotify.OnBeforeRename -= beforeRename;
                refactorNotify.OnAfterRename -= afterRename;
            }

            foreach (var expectedRename in expectedRenames)
            {
                Assert.True(expectedRename.OnBeforeCalled, $"{expectedRename.Original} => {expectedRename.New} :: on before was not called");
                Assert.True(expectedRename.OnAfterCalled, $"{expectedRename.Original} => {expectedRename.New} :: on after was not called");
            }
        }

        public async Task TestMoveToNamespaceAnalysisAsync(string markup, string expectedNamespaceName)
        {
            var workspace = CreateWorkspaceFromFile(markup, new TestParameters());
            using var testState = new TestState(workspace);

            var analysis = await testState.MoveToNamespaceService.AnalyzeTypeAtPositionAsync(
                testState.InvocationDocument,
                testState.TestInvocationDocument.SelectedSpans.Single().Start,
                CancellationToken.None);

            Assert.True(analysis.CanPerform);
            Assert.Equal(expectedNamespaceName, analysis.OriginalNamespace);
            Assert.NotEmpty(analysis.Namespaces);
        }

        public Task TestCancelledOption(string markup) => TestMoveToNamespaceAsync(markup, expectedMarkup: markup, optionCancelled: true);

        private class RenameTracking
        {
            public string Original { get; set; }
            public string New { get; set; }
            public bool OnBeforeCalled { get; set; }
            public bool OnAfterCalled { get; set; }
        }
    }
}
