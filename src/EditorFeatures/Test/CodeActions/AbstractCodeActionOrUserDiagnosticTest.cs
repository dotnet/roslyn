using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public abstract class AbstractCodeActionOrUserDiagnosticTest
    {
        protected abstract string GetLanguage();
        protected abstract ParseOptions GetScriptOptions();
        protected abstract Task<TestWorkspace> CreateWorkspaceFromFileAsync(string definition, ParseOptions parseOptions, CompilationOptions compilationOptions);

        protected void ApplyOptionsToWorkspace(Workspace workspace, IDictionary<OptionKey, object> options)
        {
            if (options != null)
            {
                var optionService = workspace.Services.GetService<IOptionService>();
                var optionSet = optionService.GetOptions();
                foreach (var option in options)
                {
                    optionSet = optionSet.WithChangedOption(option.Key, option.Value);
                }

                optionService.SetOptions(optionSet);
            }
        }

        private void TestAnnotations(
            string expectedText,
            IList<TextSpan> expectedSpans,
            SyntaxNode fixedRoot,
            string annotationKind,
            bool compareTokens)
        {
            expectedSpans = expectedSpans ?? new List<TextSpan>();
            var annotatedTokens = fixedRoot.GetAnnotatedNodesAndTokens(annotationKind).Select(n => (SyntaxToken)n).ToList();

            Assert.Equal(expectedSpans.Count, annotatedTokens.Count);

            if (expectedSpans.Count > 0)
            {
                var expectedTokens = TokenUtilities.GetTokens(TokenUtilities.GetSyntaxRoot(expectedText, GetLanguage()));
                var actualTokens = TokenUtilities.GetTokens(fixedRoot);

                for (var i = 0; i < Math.Min(expectedTokens.Count, actualTokens.Count); i++)
                {
                    var expectedToken = expectedTokens[i];
                    var actualToken = actualTokens[i];

                    var actualIsConflict = annotatedTokens.Contains(actualToken);
                    var expectedIsConflict = expectedSpans.Contains(expectedToken.Span);
                    Assert.Equal(expectedIsConflict, actualIsConflict);
                }
            }
        }

        protected async Task TestMissingAsync(
            string initialMarkup,
            IDictionary<OptionKey, object> options = null,
            string fixAllActionEquivalenceKey = null)
        {
            await TestMissingAsync(initialMarkup, parseOptions: null, options:options, fixAllActionEquivalenceKey: fixAllActionEquivalenceKey);
            await TestMissingAsync(initialMarkup, parseOptions: GetScriptOptions(), options:options, fixAllActionEquivalenceKey: fixAllActionEquivalenceKey);
        }

        protected Task TestMissingAsync(
            string initialMarkup,
            ParseOptions parseOptions,
            IDictionary<OptionKey, object> options = null,
            string fixAllActionEquivalenceKey = null)
        {
            return TestMissingAsync(initialMarkup, parseOptions, compilationOptions: null, options:options, fixAllActionEquivalenceKey: fixAllActionEquivalenceKey);
        }

        protected async Task TestMissingAsync(
            string initialMarkup,
            ParseOptions parseOptions, CompilationOptions compilationOptions,
            IDictionary<OptionKey, object> options = null,
            string fixAllActionEquivalenceKey = null)
        {
            using (var workspace = await CreateWorkspaceFromFileAsync(initialMarkup, parseOptions, compilationOptions))
            {
                ApplyOptionsToWorkspace(workspace, options);

                var actions = await GetCodeActionsAsync(workspace, fixAllActionEquivalenceKey);
                Assert.True(actions == null || actions.Count == 0);
            }
        }

        protected async Task<IList<CodeAction>> GetCodeActionsAsync(TestWorkspace workspace, string fixAllActionEquivalenceKey)
        {
            return MassageActions(await GetCodeActionsWorkerAsync(workspace, fixAllActionEquivalenceKey));
        }

        protected abstract Task<IList<CodeAction>> GetCodeActionsWorkerAsync(TestWorkspace workspace, string fixAllActionEquivalenceKey);

        protected async Task TestSmartTagTextAsync(
            string initialMarkup,
            string displayText,
            int index = 0,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null)
        {
            using (var workspace = await CreateWorkspaceFromFileAsync(initialMarkup, parseOptions, compilationOptions))
            {
                var actions = await GetCodeActionsAsync(workspace, fixAllActionEquivalenceKey: null);
                Assert.Equal(displayText, actions.ElementAt(index).Title);
            }
        }

        protected async Task TestExactActionSetOfferedAsync(
            string initialMarkup,
            IEnumerable<string> expectedActionSet,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null)
        {
            using (var workspace = await CreateWorkspaceFromFileAsync(initialMarkup, parseOptions, compilationOptions))
            {
                var actions = await GetCodeActionsAsync(workspace, fixAllActionEquivalenceKey: null);

                var actualActionSet = actions.Select(a => a.Title);
                Assert.True(actualActionSet.SequenceEqual(expectedActionSet),
                    "Expected: " + string.Join(", ", expectedActionSet) +
                    "\nActual: " + string.Join(", ", actualActionSet));
            }
        }

        protected async Task TestActionCountAsync(
            string initialMarkup,
            int count,
            ParseOptions parseOptions = null, CompilationOptions compilationOptions = null)
        {
            using (var workspace = await CreateWorkspaceFromFileAsync(initialMarkup, parseOptions, compilationOptions))
            {
                var actions = await GetCodeActionsAsync(workspace, fixAllActionEquivalenceKey: null);

                Assert.Equal(count, actions.Count());
            }
        }

        protected async Task TestAsync(
            string initialMarkup, string expectedMarkup,
            int index = 0, bool compareTokens = true,  
            IDictionary<OptionKey, object> options = null,
            string fixAllActionEquivalenceKey = null)
        {
            await TestAsync(initialMarkup, expectedMarkup, null, index, compareTokens, options, fixAllActionEquivalenceKey);
            await TestAsync(initialMarkup, expectedMarkup, GetScriptOptions(), index, compareTokens, options, fixAllActionEquivalenceKey);
        }

        protected Task TestAsync(
            string initialMarkup, string expectedMarkup,
            ParseOptions parseOptions,
            int index = 0, bool compareTokens = true,  
            IDictionary<OptionKey, object> options = null,
            string fixAllActionEquivalenceKey = null)
        {
            return TestAsync(initialMarkup, expectedMarkup, parseOptions, null, index, compareTokens, options, fixAllActionEquivalenceKey);
        }

        protected async Task TestAsync(
            string initialMarkup, string expectedMarkup,
            ParseOptions parseOptions, CompilationOptions compilationOptions,
            int index = 0, bool compareTokens = true, 
            IDictionary<OptionKey, object> options = null,
            string fixAllActionEquivalenceKey = null)
        {
            string expected;
            IDictionary<string, IList<TextSpan>> spanMap;
            MarkupTestFile.GetSpans(expectedMarkup.NormalizeLineEndings(), out expected, out spanMap);

            var conflictSpans = spanMap.GetOrAdd("Conflict", _ => new List<TextSpan>());
            var renameSpans = spanMap.GetOrAdd("Rename", _ => new List<TextSpan>());
            var warningSpans = spanMap.GetOrAdd("Warning", _ => new List<TextSpan>());

            using (var workspace = IsWorkspaceElement(initialMarkup) 
                ? await TestWorkspaceFactory.CreateWorkspaceAsync(initialMarkup) 
                : await CreateWorkspaceFromFileAsync(initialMarkup, parseOptions, compilationOptions))
            {
                ApplyOptionsToWorkspace(workspace, options);

                var actions = await GetCodeActionsAsync(workspace, fixAllActionEquivalenceKey);
                await TestActionsAsync(
                    workspace, expected, index,
                    actions,
                    conflictSpans, renameSpans, warningSpans,
                    compareTokens: compareTokens);
            }
        }

        protected async Task<Tuple<Solution, Solution>> TestActionsAsync(
            TestWorkspace workspace, string expected, 
            int index, IList<CodeAction> actions, 
            IList<TextSpan> conflictSpans, IList<TextSpan> renameSpans, IList<TextSpan> warningSpans, 
            bool compareTokens)
        {
            var operations = await VerifyInputsAndGetOperationsAsync(index, actions);
            return await TestOperationsAsync(workspace, expected, operations.ToList(), conflictSpans, renameSpans, warningSpans, compareTokens, expectedChangedDocumentId: null);
        }

        private static bool IsWorkspaceElement(string text)
        {
            return text.TrimStart('\r', '\n', ' ').StartsWith("<Workspace>", StringComparison.Ordinal);
        }

        protected async Task<Tuple<Solution, Solution>> TestOperationsAsync(
            TestWorkspace workspace,
            string expectedText,
            IList<CodeActionOperation> operations,
            IList<TextSpan> conflictSpans,
            IList<TextSpan> renameSpans,
            IList<TextSpan> warningSpans,
            bool compareTokens,
            DocumentId expectedChangedDocumentId)
        {
            var appliedChanges = ApplyOperationsAndGetSolution(workspace, operations);
            var oldSolution = appliedChanges.Item1;
            var newSolution = appliedChanges.Item2;

            if (IsWorkspaceElement(expectedText))
            {
                await VerifyAgainstWorkspaceDefinitionAsync(expectedText, newSolution);
                return Tuple.Create(oldSolution, newSolution);
            }

            var document = GetDocumentToVerify(expectedChangedDocumentId, oldSolution, newSolution);

            var fixedRoot = await document.GetSyntaxRootAsync();
            var actualText = compareTokens ? fixedRoot.ToString() : fixedRoot.ToFullString();

            if (compareTokens)
            {
                TokenUtilities.AssertTokensEqual(expectedText, actualText, GetLanguage());
            }
            else
            {
                Assert.Equal(expectedText, actualText);
            }

            TestAnnotations(expectedText, conflictSpans, fixedRoot, ConflictAnnotation.Kind, compareTokens);
            TestAnnotations(expectedText, renameSpans, fixedRoot, RenameAnnotation.Kind, compareTokens);
            TestAnnotations(expectedText, warningSpans, fixedRoot, WarningAnnotation.Kind, compareTokens);

            return Tuple.Create(oldSolution, newSolution);
        }

        private static Document GetDocumentToVerify(DocumentId expectedChangedDocumentId, Solution oldSolution, Solution newSolution)
        {
            Document document;
            // If the expectedChangedDocumentId is not mentioned then we expect only single document to be changed 
            if (expectedChangedDocumentId == null)
            {
                var projectDifferences = SolutionUtilities.GetSingleChangedProjectChanges(oldSolution, newSolution);
                var documentId = projectDifferences.GetChangedDocuments().FirstOrDefault() ?? projectDifferences.GetAddedDocuments().FirstOrDefault();
                Assert.NotNull(documentId);
                document = newSolution.GetDocument(documentId);
            }
            else
            {
                // This method obtains only the document changed and does not check the project state.
                document = newSolution.GetDocument(expectedChangedDocumentId);
            }

            return document;
        }

        private static async Task VerifyAgainstWorkspaceDefinitionAsync(string expectedText, Solution newSolution)
        {
            using (var expectedWorkspace = await TestWorkspaceFactory.CreateWorkspaceAsync(expectedText))
            {
                var expectedSolution = expectedWorkspace.CurrentSolution;
                Assert.Equal(expectedSolution.Projects.Count(), newSolution.Projects.Count());
                foreach (var project in newSolution.Projects)
                {
                    var expectedProject = expectedSolution.GetProjectsByName(project.Name).Single();
                    Assert.Equal(expectedProject.Documents.Count(), project.Documents.Count());

                    foreach (var doc in project.Documents)
                    {
                        var root = await doc.GetSyntaxRootAsync();
                        var expectedDocument = expectedProject.Documents.Single(d => d.Name == doc.Name);
                        var expectedRoot = await expectedDocument.GetSyntaxRootAsync();
                        Assert.Equal(expectedRoot.ToFullString(), root.ToFullString());
                    }
                }
            }
        }

        protected static async Task<IEnumerable<CodeActionOperation>> VerifyInputsAndGetOperationsAsync(int index, IList<CodeAction> actions)
        {
            Assert.NotNull(actions);
            if (actions.Count == 1)
            {
                var suppressionAction = actions.Single() as SuppressionCodeAction;
                if (suppressionAction != null)
                {
                    actions = suppressionAction.GetCodeActions().ToList();
                }
            }

            Assert.InRange(index, 0, actions.Count - 1);

            var action = actions[index];
            return await action.GetOperationsAsync(CancellationToken.None);
        }

        protected Tuple<Solution, Solution> ApplyOperationsAndGetSolution(
            TestWorkspace workspace,
            IEnumerable<CodeActionOperation> operations)
        {
            var applyChangesOperation = operations.OfType<ApplyChangesOperation>().First();
            var oldSolution = workspace.CurrentSolution;
            var newSolution = applyChangesOperation.ChangedSolution;

            return Tuple.Create(oldSolution, newSolution);
        }

        protected virtual IList<CodeAction> MassageActions(IList<CodeAction> actions)
        {
            return actions;
        }

        protected static IList<CodeAction> FlattenActions(IEnumerable<CodeAction> codeActions)
        {
            return codeActions?.SelectMany(a => a.HasCodeActions ? a.GetCodeActions().ToArray() : new[] { a }).ToList();
        }
    }
}
