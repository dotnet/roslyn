// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.MoveToNamespace;
using Microsoft.CodeAnalysis.Test.Utilities.Utilities;
using Microsoft.CodeAnalysis.Text;
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

            var workspace = CreateWorkspaceFromOptions(markup, testParameters.Value);
            using var testState = new TestState(workspace);

            testState.TestMoveToNamespaceOptionsService.SetOptions(moveToNamespaceOptions);
            if (expectedSuccess)
            {
                var actions = await testState.MoveToNamespaceService.GetCodeActionsAsync(
                        testState.InvocationDocument,
                        testState.TestInvocationDocument.SelectedSpans.Single(),
                        CancellationToken.None);

                foreach (var action in actions)
                {
                    if (optionCancelled || string.IsNullOrEmpty(targetNamespace))
                    {
                        Assert.Empty(await action.GetOperationsAsync(CancellationToken.None));
                    }
                    else
                    {
                        Assert.NotNull(expectedSymbolChanges);
                        Assert.NotNull(expectedMarkup);

                        var solutionPair = ApplyOperationsAndGetSolution(
                            workspace,
                            await action.GetOperationsAsync(CancellationToken.None));

                        var oldSolution = solutionPair.Item1;
                        var newSolution = solutionPair.Item2;

                        await RenameHelpers.AssertRenameAnnotationsAsync(oldSolution, newSolution, expectedSymbolChanges);
                    }
                }

                if (!optionCancelled && !string.IsNullOrEmpty(targetNamespace))
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
