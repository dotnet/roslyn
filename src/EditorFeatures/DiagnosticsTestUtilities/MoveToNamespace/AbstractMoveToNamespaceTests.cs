// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.MoveToNamespace;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.MoveToNamespace
{
    public abstract partial class AbstractMoveToNamespaceTests : AbstractCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(EditorTestWorkspace workspace, TestParameters parameters)
            => new MoveToNamespaceCodeActionProvider();

        public async Task TestMoveToNamespaceAsync(
            string markup,
            bool expectedSuccess = true,
            string expectedMarkup = null,
            TestParameters testParameters = null,
            string targetNamespace = null,
            bool optionCancelled = false,
            IReadOnlyDictionary<string, string> expectedSymbolChanges = null)
        {
            testParameters ??= new TestParameters();

            var moveToNamespaceOptions = optionCancelled
                ? MoveToNamespaceOptionsResult.Cancelled
                : new MoveToNamespaceOptionsResult(targetNamespace);

            var workspace = CreateWorkspaceFromOptions(markup, testParameters);
            using var testState = new TestState(workspace);

            testState.TestMoveToNamespaceOptionsService.SetOptions(moveToNamespaceOptions);
            if (expectedSuccess)
            {
                var actions = await testState.MoveToNamespaceService.GetCodeActionsAsync(
                    testState.InvocationDocument,
                    testState.TestInvocationDocument.SelectedSpans.Single(),
                    CancellationToken.None);

                var operationTasks = actions
                    .Cast<AbstractMoveToNamespaceCodeAction>()
                    .Select(action => action.GetOperationsAsync(workspace.CurrentSolution, action.GetOptions(CancellationToken.None), CodeAnalysisProgress.None, CancellationToken.None));

                foreach (var task in operationTasks)
                {
                    var operations = await task;

                    if (optionCancelled || string.IsNullOrEmpty(targetNamespace))
                    {
                        Assert.Empty(operations);
                    }
                    else
                    {
                        Assert.NotEmpty(operations);
                        var renamedCodeActionsOperations = operations
                            .Where(operation => operation is TestSymbolRenamedCodeActionOperationFactoryWorkspaceService.Operation)
                            .Cast<TestSymbolRenamedCodeActionOperationFactoryWorkspaceService.Operation>()
                            .ToImmutableArray();

                        Assert.NotEmpty(renamedCodeActionsOperations);

                        Assert.NotNull(expectedSymbolChanges);

                        var checkedCodeActions = new HashSet<TestSymbolRenamedCodeActionOperationFactoryWorkspaceService.Operation>(renamedCodeActionsOperations.Length);
                        foreach (var (originalName, newName) in expectedSymbolChanges)
                        {
                            var codeAction = renamedCodeActionsOperations.FirstOrDefault(a => a._symbol.ToDisplayString() == originalName);
                            Assert.Equal(newName, codeAction?._newName);
                            Assert.False(checkedCodeActions.Contains(codeAction));

                            checkedCodeActions.Add(codeAction);
                        }
                    }
                }

                if (!optionCancelled && !string.IsNullOrEmpty(targetNamespace))
                {
                    await TestInRegularAndScriptAsync(markup, expectedMarkup, options: testParameters.options);
                }
            }
            else
            {
                await TestMissingInRegularAndScriptAsync(markup, parameters: testParameters);
            }
        }

        public async Task TestMoveToNamespaceAnalysisAsync(string markup, string expectedNamespaceName)
        {
            var workspace = CreateWorkspaceFromOptions(markup, new TestParameters());
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
    }
}
