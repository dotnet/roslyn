// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.MoveToNamespace;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.MoveToNamespace
{
    public abstract partial class AbstractMoveToNamespaceTests : AbstractCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new MoveToNamespaceCodeActionProvider();

        public async Task TestMoveToNamespaceViaCommandAsync(
            string markup,
            bool expectedSuccess = true,
            string expectedMarkup = null,
            TestParameters? testParameters = null,
            string targetNamespace = null,
            bool optionCancelled = false
            )
        {
            testParameters = testParameters ?? new TestParameters();

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
            using (var testState = new TestState(workspace))
            {
                testState.TestMoveToNamespaceOptionsService.SetOptions(moveToNamespaceOptions);
                if (expectedSuccess && !optionCancelled)
                {
                    await TestInRegularAndScriptAsync(markup, expectedMarkup);
                }
                else if (optionCancelled)
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
                        Assert.Empty(operations);
                    }
                }
                else
                {
                    await TestMissingInRegularAndScriptAsync(markup, parameters: testParameters.Value);
                }
            }
        }

        public Task TestCancelledOption(string markup) => TestMoveToNamespaceViaCommandAsync(markup, expectedMarkup: markup, optionCancelled: true);
    }
}
