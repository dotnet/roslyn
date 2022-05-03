// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.Composition;
using Newtonsoft.Json.Linq;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;
using Microsoft.CodeAnalysis.Options;

using System.Diagnostics;
using System.IO;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
#endif

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    [UseExportProvider]
    public abstract partial class AbstractCodeActionOrUserDiagnosticTest
    {
        public sealed class TestParameters
        {
            /// <summary>
            /// editorconfig options.
            /// </summary>
            internal readonly OptionsCollection options;
            internal readonly OptionsCollection globalOptions;
            internal readonly TestHost testHost;
            internal readonly string workspaceKind;
            internal readonly object fixProviderData;
            internal readonly ParseOptions parseOptions;
            internal readonly CompilationOptions compilationOptions;
            internal readonly int index;
            internal readonly CodeActionPriority? priority;
            internal readonly bool retainNonFixableDiagnostics;
            internal readonly bool includeDiagnosticsOutsideSelection;
            internal readonly string title;

            internal TestParameters(
                ParseOptions parseOptions = null,
                CompilationOptions compilationOptions = null,
                OptionsCollection options = null,
                OptionsCollection globalOptions = null,
                object fixProviderData = null,
                int index = 0,
                CodeActionPriority? priority = null,
                bool retainNonFixableDiagnostics = false,
                bool includeDiagnosticsOutsideSelection = false,
                string title = null,
                TestHost testHost = TestHost.InProcess,
                string workspaceKind = null)
            {
                this.parseOptions = parseOptions;
                this.compilationOptions = compilationOptions;
                this.options = options;
                this.globalOptions = globalOptions;
                this.fixProviderData = fixProviderData;
                this.index = index;
                this.priority = priority;
                this.retainNonFixableDiagnostics = retainNonFixableDiagnostics;
                this.includeDiagnosticsOutsideSelection = includeDiagnosticsOutsideSelection;
                this.title = title;
                this.testHost = testHost;
                this.workspaceKind = workspaceKind;
            }

            public static readonly TestParameters Default = new(parseOptions: null);

            public TestParameters WithParseOptions(ParseOptions parseOptions)
                => new(parseOptions, compilationOptions, options, globalOptions, fixProviderData, index, priority, retainNonFixableDiagnostics, includeDiagnosticsOutsideSelection, title, testHost, workspaceKind);

            public TestParameters WithCompilationOptions(CompilationOptions compilationOptions)
                => new(parseOptions, compilationOptions, options, globalOptions, fixProviderData, index, priority, retainNonFixableDiagnostics, includeDiagnosticsOutsideSelection, title, testHost, workspaceKind);

            internal TestParameters WithOptions(OptionsCollection options)
                => new(parseOptions, compilationOptions, options, globalOptions, fixProviderData, index, priority, retainNonFixableDiagnostics, includeDiagnosticsOutsideSelection, title, testHost, workspaceKind);

            internal TestParameters WithGlobalOptions(OptionsCollection globalOptions)
                => new(parseOptions, compilationOptions, options, globalOptions, fixProviderData, index, priority, retainNonFixableDiagnostics, includeDiagnosticsOutsideSelection, title, testHost, workspaceKind);

            public TestParameters WithFixProviderData(object fixProviderData)
                => new(parseOptions, compilationOptions, options, globalOptions, fixProviderData, index, priority, retainNonFixableDiagnostics, includeDiagnosticsOutsideSelection, title, testHost, workspaceKind);

            public TestParameters WithIndex(int index)
                => new(parseOptions, compilationOptions, options, globalOptions, fixProviderData, index, priority, retainNonFixableDiagnostics, includeDiagnosticsOutsideSelection, title, testHost, workspaceKind);

            public TestParameters WithRetainNonFixableDiagnostics(bool retainNonFixableDiagnostics)
                => new(parseOptions, compilationOptions, options, globalOptions, fixProviderData, index, priority, retainNonFixableDiagnostics, includeDiagnosticsOutsideSelection, title, testHost, workspaceKind);

            public TestParameters WithIncludeDiagnosticsOutsideSelection(bool includeDiagnosticsOutsideSelection)
                => new(parseOptions, compilationOptions, options, globalOptions, fixProviderData, index, priority, retainNonFixableDiagnostics, includeDiagnosticsOutsideSelection, title, testHost, workspaceKind);

            public TestParameters WithWorkspaceKind(string workspaceKind)
                => new(parseOptions, compilationOptions, options, globalOptions, fixProviderData, index, priority, retainNonFixableDiagnostics, includeDiagnosticsOutsideSelection, title, testHost, workspaceKind);
        }

#pragma warning disable IDE0052 // Remove unread private members (unused when CODE_STYLE is set)
        private readonly ITestOutputHelper _logger;
#pragma warning restore

        protected AbstractCodeActionOrUserDiagnosticTest(ITestOutputHelper logger = null)
        {
            _logger = logger;
        }

        private const string AutoGeneratedAnalyzerConfigHeader = @"# auto-generated .editorconfig for code style options";

        protected internal abstract string GetLanguage();
        protected ParenthesesOptionsProvider ParenthesesOptionsProvider => new ParenthesesOptionsProvider(this.GetLanguage());
        protected abstract ParseOptions GetScriptOptions();

        private protected virtual IDocumentServiceProvider GetDocumentServiceProvider()
            => null;

        protected virtual TestComposition GetComposition()
            => EditorTestCompositions.EditorFeatures
                .AddExcludedPartTypes(typeof(IDiagnosticUpdateSourceRegistrationService))
                .AddParts(typeof(MockDiagnosticUpdateSourceRegistrationService));

        protected virtual void InitializeWorkspace(TestWorkspace workspace, TestParameters parameters)
        {
        }

        protected virtual TestParameters SetParameterDefaults(TestParameters parameters)
            => parameters;

        protected TestWorkspace CreateWorkspaceFromOptions(string workspaceMarkupOrCode, TestParameters parameters = null)
        {
            parameters ??= TestParameters.Default;

            var composition = GetComposition().WithTestHostParts(parameters.testHost);

            parameters = SetParameterDefaults(parameters);

            var documentServiceProvider = GetDocumentServiceProvider();
            var workspace = TestWorkspace.IsWorkspaceElement(workspaceMarkupOrCode)
                ? TestWorkspace.Create(XElement.Parse(workspaceMarkupOrCode), openDocuments: false, composition: composition, documentServiceProvider: documentServiceProvider, workspaceKind: parameters.workspaceKind)
                : TestWorkspace.Create(GetLanguage(), parameters.compilationOptions, parameters.parseOptions, files: new[] { workspaceMarkupOrCode }, composition: composition, documentServiceProvider: documentServiceProvider, workspaceKind: parameters.workspaceKind);

#if !CODE_STYLE
            if (parameters.testHost == TestHost.OutOfProcess && _logger != null)
            {
                var remoteHostProvider = (InProcRemoteHostClientProvider)workspace.Services.GetRequiredService<IRemoteHostClientProvider>();
                remoteHostProvider.TraceListener = new XunitTraceListener(_logger);
            }
#endif
            InitializeWorkspace(workspace, parameters);

            // We create an .editorconfig at project root to apply the options.
            // We need to ensure that our projects/documents are rooted for
            // execution from CodeStyle layer as we will be adding a rooted .editorconfig to each project
            // to apply the options.
            if (parameters.options != null)
            {
                MakeProjectsAndDocumentsRooted(workspace);
                AddAnalyzerConfigDocumentWithOptions(workspace, parameters.options);
            }

#if !CODE_STYLE
            workspace.ApplyOptions(parameters.options);

            if (parameters.globalOptions != null)
            {
                workspace.ApplyOptions(parameters.globalOptions);
                parameters.globalOptions.SetGlobalOptions(workspace.GlobalOptions);
            }
#endif
            return workspace;
        }

        private static void MakeProjectsAndDocumentsRooted(TestWorkspace workspace)
        {
            const string defaultRootFilePath = @"z:\";
            var newSolution = workspace.CurrentSolution;
            foreach (var projectId in workspace.CurrentSolution.ProjectIds)
            {
                var project = newSolution.GetProject(projectId);

                string projectRootFilePath;
                if (!PathUtilities.IsAbsolute(project.FilePath))
                {
                    projectRootFilePath = defaultRootFilePath;
                    newSolution = newSolution.WithProjectFilePath(projectId, Path.Combine(projectRootFilePath, project.FilePath));
                }
                else
                {
                    projectRootFilePath = PathUtilities.GetPathRoot(project.FilePath);
                }

                foreach (var documentId in project.DocumentIds)
                {
                    var document = newSolution.GetDocument(documentId);
                    if (!PathUtilities.IsAbsolute(document.FilePath))
                    {
                        newSolution = newSolution.WithDocumentFilePath(documentId, Path.Combine(projectRootFilePath, document.FilePath));
                    }
                    else
                    {
                        Assert.Equal(projectRootFilePath, PathUtilities.GetPathRoot(document.FilePath));
                    }
                }
            }

            var applied = workspace.TryApplyChanges(newSolution);
            Assert.True(applied);
            return;
        }

        private static void AddAnalyzerConfigDocumentWithOptions(TestWorkspace workspace, OptionsCollection options)
        {
            Debug.Assert(options != null);
            var analyzerConfigText = GenerateAnalyzerConfigText(options);

            var newSolution = workspace.CurrentSolution;
            foreach (var project in workspace.Projects)
            {
                Assert.True(PathUtilities.IsAbsolute(project.FilePath));
                var projectRootFilePath = PathUtilities.GetPathRoot(project.FilePath);
                var documentId = DocumentId.CreateNewId(project.Id);
                newSolution = newSolution.AddAnalyzerConfigDocument(
                    documentId,
                    ".editorconfig",
                    SourceText.From(analyzerConfigText),
                    filePath: Path.Combine(projectRootFilePath, ".editorconfig"));
            }

            var applied = workspace.TryApplyChanges(newSolution);
            Assert.True(applied);
            return;

            string GenerateAnalyzerConfigText(OptionsCollection options)
            {
                var textBuilder = new StringBuilder();

                // Add an auto-generated header at the top so we can skip this file in expected baseline validation.
                textBuilder.AppendLine(AutoGeneratedAnalyzerConfigHeader);
                textBuilder.AppendLine();
                textBuilder.AppendLine(options.GetEditorConfigText());
                return textBuilder.ToString();
            }
        }

        private static TestParameters WithRegularOptions(TestParameters parameters)
            => parameters.WithParseOptions(parameters.parseOptions?.WithKind(SourceCodeKind.Regular));

        private TestParameters WithScriptOptions(TestParameters parameters)
            => parameters.WithParseOptions(parameters.parseOptions?.WithKind(SourceCodeKind.Script) ?? GetScriptOptions());

        protected async Task TestMissingInRegularAndScriptAsync(
            string initialMarkup,
            TestParameters parameters = null,
            int codeActionIndex = 0)
        {
            var ps = parameters ?? TestParameters.Default;
            await TestMissingAsync(initialMarkup, WithRegularOptions(ps), codeActionIndex);
            await TestMissingAsync(initialMarkup, WithScriptOptions(ps), codeActionIndex);
        }

        protected async Task TestMissingAsync(
            string initialMarkup,
            TestParameters parameters = null,
            int codeActionIndex = 0)
        {
            var ps = parameters ?? TestParameters.Default;
            using var workspace = CreateWorkspaceFromOptions(initialMarkup, ps);

            var (actions, _) = await GetCodeActionsAsync(workspace, ps);
            var offeredActions = Environment.NewLine + string.Join(Environment.NewLine, actions.Select(action => action.Title));

            if (codeActionIndex == 0)
            {
                Assert.True(actions.Length == 0, "An action was offered when none was expected. Offered actions:" + offeredActions);
            }
            else
            {
                Assert.True(actions.Length <= codeActionIndex, "An action was offered at the specified index when none was expected. Offered actions:" + offeredActions);
            }
        }

        protected async Task TestDiagnosticMissingAsync(
            string initialMarkup, TestParameters parameters = null)
        {
            var ps = parameters ?? TestParameters.Default;
            using var workspace = CreateWorkspaceFromOptions(initialMarkup, ps);
            var diagnostics = await GetDiagnosticsWorkerAsync(workspace, ps);
            Assert.True(0 == diagnostics.Length, $"Expected no diagnostics, but got {diagnostics.Length}");
        }

        protected abstract Task<(ImmutableArray<CodeAction>, CodeAction actionToInvoke)> GetCodeActionsAsync(
            TestWorkspace workspace, TestParameters parameters);

        protected abstract Task<ImmutableArray<Diagnostic>> GetDiagnosticsWorkerAsync(
            TestWorkspace workspace, TestParameters parameters);

        protected Task TestSmartTagTextAsync(string initialMarkup, string displayText, int index)
            => TestSmartTagTextAsync(initialMarkup, displayText, new TestParameters(index: index));

        protected Task TestSmartTagGlyphTagsAsync(string initialMarkup, ImmutableArray<string> glyphTags, int index)
            => TestSmartTagGlyphTagsAsync(initialMarkup, glyphTags, new TestParameters(index: index));

        protected async Task TestSmartTagTextAsync(
            string initialMarkup,
            string displayText,
            TestParameters parameters = null)
        {
            var ps = parameters ?? TestParameters.Default;
            using var workspace = CreateWorkspaceFromOptions(initialMarkup, ps);
            var (_, action) = await GetCodeActionsAsync(workspace, ps);
            Assert.Equal(displayText, action.Title);
        }

        protected async Task TestSmartTagGlyphTagsAsync(
            string initialMarkup,
            ImmutableArray<string> glyph,
            TestParameters parameters = null)
        {
            var ps = parameters ?? TestParameters.Default;
            using var workspace = CreateWorkspaceFromOptions(initialMarkup, ps);
            var (_, action) = await GetCodeActionsAsync(workspace, ps);
            Assert.Equal(glyph, action.Tags);
        }

        protected async Task TestExactActionSetOfferedAsync(
            string initialMarkup,
            IEnumerable<string> expectedActionSet,
            TestParameters parameters = null)
        {
            var ps = parameters ?? TestParameters.Default;
            using var workspace = CreateWorkspaceFromOptions(initialMarkup, ps);
            var (actions, _) = await GetCodeActionsAsync(workspace, ps);

            var actualActionSet = actions.Select(a => a.Title);
            Assert.True(actualActionSet.SequenceEqual(expectedActionSet),
                "Expected: " + string.Join(", ", expectedActionSet) +
                "\nActual: " + string.Join(", ", actualActionSet));
        }

        protected async Task TestActionCountAsync(
            string initialMarkup,
            int count,
            TestParameters parameters = null)
        {
            var ps = parameters ?? TestParameters.Default;
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, ps))
            {
                var (actions, _) = await GetCodeActionsAsync(workspace, ps);

                Assert.Equal(count, actions.Length);
            }
        }

        internal Task TestInRegularAndScriptAsync(
            string initialMarkup,
            string expectedMarkup,
            int index = 0,
            CodeActionPriority? priority = null,
            CompilationOptions compilationOptions = null,
            OptionsCollection options = null,
            OptionsCollection globalOptions = null,
            object fixProviderData = null,
            ParseOptions parseOptions = null,
            string title = null,
            TestHost testHost = TestHost.InProcess)
        {
            return TestInRegularAndScript1Async(
                initialMarkup, expectedMarkup,
                new TestParameters(parseOptions, compilationOptions, options, globalOptions, fixProviderData, index, priority, title: title, testHost: testHost));
        }

        internal Task TestInRegularAndScript1Async(
            string initialMarkup,
            string expectedMarkup,
            int index = 0,
            TestParameters parameters = null)
        {
            return TestInRegularAndScript1Async(initialMarkup, expectedMarkup, (parameters ?? TestParameters.Default).WithIndex(index));
        }

        internal async Task TestInRegularAndScript1Async(
            string initialMarkup,
            string expectedMarkup,
            TestParameters parameters)
        {
            await TestAsync(initialMarkup, expectedMarkup, WithRegularOptions(parameters));

            // VB scripting is not supported:
            if (GetLanguage() == LanguageNames.CSharp)
            {
                await TestAsync(initialMarkup, expectedMarkup, WithScriptOptions(parameters));
            }
        }

        internal Task TestAsync(
            string initialMarkup,
            string expectedMarkup,
            ParseOptions parseOptions,
            CompilationOptions compilationOptions = null,
            int index = 0,
            OptionsCollection options = null,
            OptionsCollection globalOptions = null,
            object fixProviderData = null,
            CodeActionPriority? priority = null,
            TestHost testHost = TestHost.InProcess)
        {
            return TestAsync(
                initialMarkup,
                expectedMarkup,
                new TestParameters(parseOptions, compilationOptions, options, globalOptions, fixProviderData, index, priority, testHost: testHost));
        }

        private async Task TestAsync(
            string initialMarkup,
            string expectedMarkup,
            TestParameters parameters)
        {
            MarkupTestFile.GetSpans(
                initialMarkup.NormalizeLineEndings(),
                out var initialMarkupWithoutSpans, out IDictionary<string, ImmutableArray<TextSpan>> initialSpanMap);

            const string UnnecessaryMarkupKey = "Unnecessary";
            var unnecessarySpans = initialSpanMap.GetOrAdd(UnnecessaryMarkupKey, _ => ImmutableArray<TextSpan>.Empty);

            MarkupTestFile.GetSpans(
                expectedMarkup.NormalizeLineEndings(),
                out var expected, out IDictionary<string, ImmutableArray<TextSpan>> expectedSpanMap);

            var conflictSpans = expectedSpanMap.GetOrAdd("Conflict", _ => ImmutableArray<TextSpan>.Empty);
            var renameSpans = expectedSpanMap.GetOrAdd("Rename", _ => ImmutableArray<TextSpan>.Empty);
            var warningSpans = expectedSpanMap.GetOrAdd("Warning", _ => ImmutableArray<TextSpan>.Empty);
            var navigationSpans = expectedSpanMap.GetOrAdd("Navigation", _ => ImmutableArray<TextSpan>.Empty);

            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
            {
                // Ideally this check would always run, but there are several hundred tests that would need to be
                // updated with {|Unnecessary:|} spans.
                if (unnecessarySpans.Any())
                {
                    var allDiagnostics = await GetDiagnosticsWorkerAsync(workspace, parameters
                        .WithRetainNonFixableDiagnostics(true)
                        .WithIncludeDiagnosticsOutsideSelection(true));

                    TestUnnecessarySpans(allDiagnostics, unnecessarySpans, UnnecessaryMarkupKey, initialMarkupWithoutSpans);
                }

                var (_, action) = await GetCodeActionsAsync(workspace, parameters);
                await TestActionAsync(
                    workspace, expected, action,
                    conflictSpans, renameSpans, warningSpans, navigationSpans,
                    parameters);
            }
        }

        private static void TestUnnecessarySpans(
            ImmutableArray<Diagnostic> diagnostics,
            ImmutableArray<TextSpan> expectedSpans,
            string markupKey,
            string initialMarkupWithoutSpans)
        {
            var unnecessaryLocations = diagnostics.SelectMany(GetUnnecessaryLocations)
                .OrderBy(location => location.SourceSpan.Start)
                .ThenBy(location => location.SourceSpan.End)
                .ToArray();

            if (expectedSpans.Length != unnecessaryLocations.Length)
            {
                AssertEx.Fail(BuildFailureMessage(expectedSpans, WellKnownDiagnosticTags.Unnecessary, markupKey, initialMarkupWithoutSpans, diagnostics));
            }

            for (var i = 0; i < expectedSpans.Length; i++)
            {
                var actual = unnecessaryLocations[i].SourceSpan;
                var expected = expectedSpans[i];
                Assert.Equal(expected, actual);
            }

            static IEnumerable<Location> GetUnnecessaryLocations(Diagnostic diagnostic)
            {
                if (diagnostic.Descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.Unnecessary))
                    yield return diagnostic.Location;

                if (!diagnostic.Properties.TryGetValue(WellKnownDiagnosticTags.Unnecessary, out var additionalUnnecessaryLocationsString))
                    yield break;

                var locations = JArray.Parse(additionalUnnecessaryLocationsString);
                foreach (var locationIndex in locations)
                    yield return diagnostic.AdditionalLocations[(int)locationIndex];
            }
        }

        private static string BuildFailureMessage(
            ImmutableArray<TextSpan> expectedSpans,
            string diagnosticTag,
            string markupKey,
            string initialMarkupWithoutSpans,
            ImmutableArray<Diagnostic> diagnosticsWithTag)
        {
            var message = $"Expected {expectedSpans.Length} diagnostic spans with custom tag '{diagnosticTag}', but there were {diagnosticsWithTag.Length}.";

            if (expectedSpans.Length == 0)
            {
                message += $" If a diagnostic span tagged '{diagnosticTag}' is expected, surround the span in the test markup with the following syntax: {{|Unnecessary:...}}";

                var segments = new List<(int originalStringIndex, string segment)>();

                foreach (var diagnostic in diagnosticsWithTag)
                {
                    var documentOffset = initialMarkupWithoutSpans.IndexOf(diagnosticsWithTag.First().Location.SourceTree.ToString());
                    if (documentOffset == -1)
                        continue;

                    segments.Add((documentOffset + diagnostic.Location.SourceSpan.Start, "{|" + markupKey + ":"));
                    segments.Add((documentOffset + diagnostic.Location.SourceSpan.End, "|}"));
                }

                if (segments.Any())
                {
                    message += Environment.NewLine
                        + "Example:" + Environment.NewLine
                        + Environment.NewLine
                        + InsertSegments(initialMarkupWithoutSpans, segments);
                }
            }

            return message;
        }

        private static string InsertSegments(string originalString, IEnumerable<(int originalStringIndex, string segment)> segments)
        {
            var builder = new StringBuilder();

            var positionInOriginalString = 0;

            foreach (var (originalStringIndex, segment) in segments.OrderBy(s => s.originalStringIndex))
            {
                builder.Append(originalString, positionInOriginalString, originalStringIndex - positionInOriginalString);
                builder.Append(segment);

                positionInOriginalString = originalStringIndex;
            }

            builder.Append(originalString, positionInOriginalString, originalString.Length - positionInOriginalString);
            return builder.ToString();
        }

        internal async Task<Tuple<Solution, Solution>> TestActionAsync(
            TestWorkspace workspace, string expected,
            CodeAction action,
            ImmutableArray<TextSpan> conflictSpans,
            ImmutableArray<TextSpan> renameSpans,
            ImmutableArray<TextSpan> warningSpans,
            ImmutableArray<TextSpan> navigationSpans,
            TestParameters parameters)
        {
            var operations = await VerifyActionAndGetOperationsAsync(workspace, action, parameters);
            return await TestOperationsAsync(
                workspace, expected, operations, conflictSpans, renameSpans,
                warningSpans, navigationSpans, expectedChangedDocumentId: null);
        }

        protected static async Task<Tuple<Solution, Solution>> TestOperationsAsync(
            TestWorkspace workspace,
            string expectedText,
            ImmutableArray<CodeActionOperation> operations,
            ImmutableArray<TextSpan> conflictSpans,
            ImmutableArray<TextSpan> renameSpans,
            ImmutableArray<TextSpan> warningSpans,
            ImmutableArray<TextSpan> navigationSpans,
            DocumentId expectedChangedDocumentId)
        {
            var appliedChanges = await ApplyOperationsAndGetSolutionAsync(workspace, operations);
            var oldSolution = appliedChanges.Item1;
            var newSolution = appliedChanges.Item2;

            if (TestWorkspace.IsWorkspaceElement(expectedText))
            {
                var newSolutionWithLinkedFiles = await newSolution.WithMergedLinkedFileChangesAsync(oldSolution);
                await VerifyAgainstWorkspaceDefinitionAsync(expectedText, newSolutionWithLinkedFiles, workspace.ExportProvider);
                return Tuple.Create(oldSolution, newSolution);
            }

            var document = GetDocumentToVerify(expectedChangedDocumentId, oldSolution, newSolution);

            var fixedRoot = await document.GetSyntaxRootAsync();
            var actualText = fixedRoot.ToFullString();

            // To help when a user just writes a test (and supplied no 'expectedText') just print
            // out the entire 'actualText' (without any trimming).  in the case that we have both,
            // call the normal AssertEx helper which will print out a good diff.
            if (expectedText == "")
            {
                Assert.Equal((object)expectedText, actualText);
            }
            else
            {
                AssertEx.EqualOrDiff(expectedText, actualText);
            }

            TestAnnotations(conflictSpans, ConflictAnnotation.Kind);
            TestAnnotations(renameSpans, RenameAnnotation.Kind);
            TestAnnotations(warningSpans, WarningAnnotation.Kind);
            TestAnnotations(navigationSpans, NavigationAnnotation.Kind);

            return Tuple.Create(oldSolution, newSolution);

            void TestAnnotations(ImmutableArray<TextSpan> expectedSpans, string annotationKind)
            {
                var annotatedItems = fixedRoot.GetAnnotatedNodesAndTokens(annotationKind).OrderBy(s => s.SpanStart).ToList();

                Assert.True(expectedSpans.Length == annotatedItems.Count,
                    $"Annotations of kind '{annotationKind}' didn't match. Expected: {expectedSpans.Length}. Actual: {annotatedItems.Count}.");

                for (var i = 0; i < Math.Min(expectedSpans.Length, annotatedItems.Count); i++)
                {
                    var actual = annotatedItems[i].Span;
                    var expected = expectedSpans[i];
                    Assert.Equal(expected, actual);
                }
            }
        }

        protected static Document GetDocumentToVerify(DocumentId expectedChangedDocumentId, Solution oldSolution, Solution newSolution)
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

        private static async Task VerifyAgainstWorkspaceDefinitionAsync(string expectedText, Solution newSolution, ExportProvider exportProvider)
        {
            using (var expectedWorkspace = TestWorkspace.Create(expectedText, exportProvider: exportProvider))
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
                        var expectedDocuments = expectedProject.Documents.Where(d => d.Name == doc.Name);

                        if (expectedDocuments.Any())
                        {
                            Assert.Single(expectedDocuments);
                        }
                        else
                        {
                            AssertEx.Fail($"Could not find document with name '{doc.Name}'");
                        }

                        var expectedDocument = expectedDocuments.Single();

                        var expectedRoot = await expectedDocument.GetSyntaxRootAsync();
                        VerifyExpectedDocumentText(expectedRoot.ToFullString(), root.ToFullString());
                    }

                    foreach (var additionalDoc in project.AdditionalDocuments)
                    {
                        var root = await additionalDoc.GetTextAsync();
                        var expectedDocument = expectedProject.AdditionalDocuments.Single(d => d.Name == additionalDoc.Name);
                        var expectedRoot = await expectedDocument.GetTextAsync();
                        VerifyExpectedDocumentText(expectedRoot.ToString(), root.ToString());
                    }

                    foreach (var analyzerConfigDoc in project.AnalyzerConfigDocuments)
                    {
                        var root = await analyzerConfigDoc.GetTextAsync();
                        var actualString = root.ToString();
                        if (actualString.StartsWith(AutoGeneratedAnalyzerConfigHeader))
                        {
                            // Skip validation for analyzer config file that is auto-generated by test framework
                            // for applying code style options.
                            continue;
                        }

                        var expectedDocument = expectedProject.AnalyzerConfigDocuments.Single(d => d.FilePath == analyzerConfigDoc.FilePath);
                        var expectedRoot = await expectedDocument.GetTextAsync();
                        VerifyExpectedDocumentText(expectedRoot.ToString(), actualString);
                    }
                }
            }

            return;

            // Local functions.
            static void VerifyExpectedDocumentText(string expected, string actual)
            {
                if (expected == "")
                {
                    Assert.Equal((object)expected, actual);
                }
                else
                {
                    Assert.Equal(expected, actual);
                }
            }
        }

        internal async Task<ImmutableArray<CodeActionOperation>> VerifyActionAndGetOperationsAsync(
            TestWorkspace workspace, CodeAction action, TestParameters parameters = null)
        {
            parameters ??= TestParameters.Default;

            if (action is null)
            {
                var diagnostics = await GetDiagnosticsWorkerAsync(workspace, parameters.WithRetainNonFixableDiagnostics(true));

                throw new Exception("No action was offered when one was expected. Diagnostics from the compilation: " + string.Join("", diagnostics.Select(d => Environment.NewLine + d.ToString())));
            }

            if (parameters.priority != null)
            {
                Assert.Equal(parameters.priority.Value, action.Priority);
            }

            if (parameters.title != null)
            {
                Assert.Equal(parameters.title, action.Title);
            }

            return await action.GetOperationsAsync(CancellationToken.None);
        }

        protected static async Task<Tuple<Solution, Solution>> ApplyOperationsAndGetSolutionAsync(
            TestWorkspace workspace,
            IEnumerable<CodeActionOperation> operations)
        {
            Tuple<Solution, Solution> result = null;
            foreach (var operation in operations)
            {
                if (operation is ApplyChangesOperation && result == null)
                {
                    var oldSolution = workspace.CurrentSolution;
                    var newSolution = ((ApplyChangesOperation)operation).ChangedSolution;
                    result = Tuple.Create(oldSolution, newSolution);
                }
                else if (operation.ApplyDuringTests)
                {
                    var oldSolution = workspace.CurrentSolution;
                    await operation.TryApplyAsync(workspace, new ProgressTracker(), CancellationToken.None);
                    var newSolution = workspace.CurrentSolution;
                    result = Tuple.Create(oldSolution, newSolution);
                }
            }

            if (result == null)
            {
                throw new InvalidOperationException("No ApplyChangesOperation found");
            }

            return result;
        }

        protected virtual ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => actions;

        internal static void VerifyCodeActionsRegisteredByProvider(CodeFixProvider provider, List<CodeFix> fixes)
        {
            if (provider.GetFixAllProvider() == null)
            {
                // Only require unique equivalence keys when the fixer supports FixAll
                return;
            }

            var diagnosticsAndEquivalenceKeyToTitleMap = new Dictionary<(Diagnostic diagnostic, string equivalenceKey), string>();
            foreach (var fix in fixes)
            {
                VerifyCodeAction(fix.Action, fix.Diagnostics, provider, diagnosticsAndEquivalenceKeyToTitleMap);
            }

            return;

            static void VerifyCodeAction(
                CodeAction codeAction,
                ImmutableArray<Diagnostic> diagnostics,
                CodeFixProvider provider,
                Dictionary<(Diagnostic diagnostic, string equivalenceKey), string> diagnosticsAndEquivalenceKeyToTitleMap)
            {
                if (!codeAction.NestedCodeActions.IsEmpty)
                {
                    // Only validate leaf code actions.
                    foreach (var nestedAction in codeAction.NestedCodeActions)
                    {
                        VerifyCodeAction(nestedAction, diagnostics, provider, diagnosticsAndEquivalenceKeyToTitleMap);
                    }

                    return;
                }

                foreach (var diagnostic in diagnostics)
                {
                    var key = (diagnostic, codeAction.EquivalenceKey);
                    var existingTitle = diagnosticsAndEquivalenceKeyToTitleMap.GetOrAdd(key, _ => codeAction.Title);
                    if (existingTitle != codeAction.Title)
                    {
                        var messageSuffix = codeAction.EquivalenceKey != null
                            ? string.Empty
                            : @"
Consider using the title as the equivalence key instead of 'null'";

                        Assert.False(true, @$"Expected different 'CodeAction.EquivalenceKey' for code actions registered for same diagnostic:
- Name: '{provider.GetType().Name}'
- Title 1: '{codeAction.Title}'
- Title 2: '{existingTitle}'
- Shared equivalence key: '{codeAction.EquivalenceKey ?? "<null>"}'{messageSuffix}");
                    }
                }
            }
        }

        protected static ImmutableArray<CodeAction> FlattenActions(ImmutableArray<CodeAction> codeActions)
        {
            return codeActions.SelectMany(a => a.NestedCodeActions.Length > 0
                ? a.NestedCodeActions
                : ImmutableArray.Create(a)).ToImmutableArray();
        }

        protected static ImmutableArray<CodeAction> GetNestedActions(ImmutableArray<CodeAction> codeActions)
            => codeActions.SelectMany(a => a.NestedCodeActions).ToImmutableArray();

        /// <summary>
        /// Tests all the code actions for the given <paramref name="input"/> string.  Each code
        /// action must produce the corresponding output in the <paramref name="outputs"/> array.
        ///
        /// Will throw if there are more outputs than code actions or more code actions than outputs.
        /// </summary>
        protected Task TestAllInRegularAndScriptAsync(
            string input,
            params string[] outputs)
        {
            return TestAllInRegularAndScriptAsync(input, parameters: null, outputs);
        }

        protected async Task TestAllInRegularAndScriptAsync(
            string input,
            TestParameters parameters,
            params string[] outputs)
        {
            parameters ??= TestParameters.Default;

            for (var index = 0; index < outputs.Length; index++)
            {
                var output = outputs[index];
                await TestInRegularAndScript1Async(input, output, parameters.WithIndex(index));
            }

            await TestActionCountAsync(input, outputs.Length, parameters);
        }

        protected static void GetDocumentAndSelectSpanOrAnnotatedSpan(
            TestWorkspace workspace,
            out Document document,
            out TextSpan span,
            out string annotation)
        {
            annotation = null;
            if (!TryGetDocumentAndSelectSpan(workspace, out document, out span))
            {
                document = GetDocumentAndAnnotatedSpan(workspace, out annotation, out span);
            }
        }

        private static bool TryGetDocumentAndSelectSpan(TestWorkspace workspace, out Document document, out TextSpan span)
        {
            var hostDocument = workspace.Documents.FirstOrDefault(d => d.SelectedSpans.Any());
            if (hostDocument == null)
            {
                // If there wasn't a span, see if there was a $$ caret.  we'll create an empty span
                // there if so.
                hostDocument = workspace.Documents.FirstOrDefault(d => d.CursorPosition != null);
                if (hostDocument == null)
                {
                    document = null;
                    span = default;
                    return false;
                }

                span = new TextSpan(hostDocument.CursorPosition.Value, 0);
                document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                return true;
            }

            span = hostDocument.SelectedSpans.Single();
            document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
            return true;
        }

        private static Document GetDocumentAndAnnotatedSpan(TestWorkspace workspace, out string annotation, out TextSpan span)
        {
            var annotatedDocuments = workspace.Documents.Where(d => d.AnnotatedSpans.Any());
            var hostDocument = annotatedDocuments.Single();
            var annotatedSpan = hostDocument.AnnotatedSpans.Single();
            annotation = annotatedSpan.Key;
            span = annotatedSpan.Value.Single();
            return workspace.CurrentSolution.GetDocument(hostDocument.Id);
        }

        protected static FixAllScope? GetFixAllScope(string annotation)
        {
            if (annotation == null)
            {
                return null;
            }

            return annotation switch
            {
                "FixAllInDocument" => FixAllScope.Document,
                "FixAllInProject" => FixAllScope.Project,
                "FixAllInSolution" => FixAllScope.Solution,
                "FixAllInContainingMember" => FixAllScope.ContainingMember,
                "FixAllInContainingType" => FixAllScope.ContainingType,
                "FixAllInSelection" => FixAllScope.Custom,
                _ => throw new InvalidProgramException("Incorrect FixAll annotation in test"),
            };
        }
    }
}
