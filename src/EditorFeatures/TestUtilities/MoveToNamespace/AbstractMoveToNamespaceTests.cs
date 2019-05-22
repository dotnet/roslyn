// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.MoveToNamespace;
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
            bool testAnalysis = false,
            IReadOnlyDictionary<string, string> expectedSymbolChanges = null
            )
        {
            testParameters ??= new TestParameters();

            var moveToNamespaceOptions = TestMoveToNamespaceOptionsService.DefaultOptions;

            if (optionCancelled)
            {
                moveToNamespaceOptions = MoveToNamespaceOptionsResult.Cancelled;
            }
            else if (!string.IsNullOrEmpty(targetNamespace))
            {
                moveToNamespaceOptions = new MoveToNamespaceOptionsResult(targetNamespace);
            }

            var workspace = CreateWorkspaceFromFile(markup, testParameters.Value);
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
                    .Select(action => action.GetOperationsAsync(action.GetOptions(CancellationToken.None), CancellationToken.None));

                foreach (var task in operationTasks)
                {
                    var operations = await task;

                    if (optionCancelled)
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
                        foreach (var kvp in expectedSymbolChanges)
                        {
                            var originalName = kvp.Key;
                            var newName = kvp.Value;

                            var codeAction = renamedCodeActionsOperations.FirstOrDefault(a => a._symbol.ToDisplayString() == originalName);
                            Assert.Equal(newName, codeAction?._newName);
                            Assert.False(checkedCodeActions.Contains(codeAction));

                            checkedCodeActions.Add(codeAction);
                        }
                    }

                }

                if (!optionCancelled)
                {
                    await TestInRegularAndScriptAsync(markup, expectedMarkup);
                }
            }
            else
            {
                await TestMissingInRegularAndScriptAsync(markup, parameters: testParameters.Value);
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
    }
}
