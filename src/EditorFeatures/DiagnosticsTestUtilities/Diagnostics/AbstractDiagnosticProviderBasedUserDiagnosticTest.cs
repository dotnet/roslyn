// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable
#if CODE_STYLE
extern alias CODESTYLE_UTILITIES;
#endif

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.UseAutoProperty;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic.UseAutoProperty;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
#if CODE_STYLE
    using OptionsCollectionAlias = CODESTYLE_UTILITIES::Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.OptionsCollection;
#else
    using OptionsCollectionAlias = OptionsCollection;
#endif
    public abstract partial class AbstractDiagnosticProviderBasedUserDiagnosticTest : AbstractUserDiagnosticTest
    {
        private readonly ConcurrentDictionary<Workspace, (DiagnosticAnalyzer, CodeFixProvider)> _analyzerAndFixerMap = [];

        protected AbstractDiagnosticProviderBasedUserDiagnosticTest(ITestOutputHelper logger)
           : base(logger)
        {
        }

        internal abstract (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace);

        internal virtual (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace, TestParameters parameters)
            => CreateDiagnosticProviderAndFixer(workspace);

        private (DiagnosticAnalyzer, CodeFixProvider) GetOrCreateDiagnosticProviderAndFixer(
            Workspace workspace, TestParameters parameters)
        {
            return parameters.fixProviderData == null
                ? _analyzerAndFixerMap.GetOrAdd(workspace, CreateDiagnosticProviderAndFixer)
                : CreateDiagnosticProviderAndFixer(workspace, parameters);
        }

        internal virtual bool ShouldSkipMessageDescriptionVerification(DiagnosticDescriptor descriptor)
        {
            if (descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.NotConfigurable))
            {
                if (!descriptor.IsEnabledByDefault || descriptor.DefaultSeverity == DiagnosticSeverity.Hidden)
                {
                    // The message only displayed if either enabled and not hidden, or configurable
                    return true;
                }
            }

            return false;
        }

        [Fact]
        public void TestSupportedDiagnosticsMessageTitle()
        {
            using var workspace = new AdhocWorkspace();
            var diagnosticAnalyzer = CreateDiagnosticProviderAndFixer(workspace).Item1;
            if (diagnosticAnalyzer == null)
            {
                return;
            }

            foreach (var descriptor in diagnosticAnalyzer.SupportedDiagnostics)
            {
                if (descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.NotConfigurable))
                {
                    // The title only displayed for rule configuration
                    continue;
                }

                Assert.NotEqual("", descriptor.Title?.ToString() ?? "");
            }
        }

        [Fact]
        public void TestSupportedDiagnosticsMessageDescription()
        {
            using var workspace = new AdhocWorkspace();
            var diagnosticAnalyzer = CreateDiagnosticProviderAndFixer(workspace).Item1;
            if (diagnosticAnalyzer == null)
            {
                return;
            }

            foreach (var descriptor in diagnosticAnalyzer.SupportedDiagnostics)
            {
                if (ShouldSkipMessageDescriptionVerification(descriptor))
                {
                    continue;
                }

                Assert.NotEqual("", descriptor.MessageFormat?.ToString() ?? "");
            }
        }

        [Fact]
        public void TestSupportedDiagnosticsMessageHelpLinkUri()
        {
            using var workspace = new AdhocWorkspace();
            var diagnosticAnalyzer = CreateDiagnosticProviderAndFixer(workspace).Item1;
            if (diagnosticAnalyzer == null)
                return;

            foreach (var descriptor in diagnosticAnalyzer.SupportedDiagnostics)
            {
                // These don't come up in UI.
                if (descriptor.DefaultSeverity == DiagnosticSeverity.Hidden && descriptor.CustomTags.Contains(WellKnownDiagnosticTags.NotConfigurable))
                    continue;

                if (descriptor.Id is "RE0001" or "JSON001" or "JSON002") // Currently not documented. https://github.com/dotnet/roslyn/issues/48530
                    continue;

                if (descriptor.Id == "IDE0043") // Intentionally undocumented. It will be removed in favor of CA2241
                    continue;

                if (descriptor.Id == "IDE1007")
                    continue;

                Assert.NotEqual("", descriptor.HelpLinkUri ?? "");
            }
        }

        internal override async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(
            EditorTestWorkspace workspace, TestParameters parameters)
        {
            var (analyzer, _) = GetOrCreateDiagnosticProviderAndFixer(workspace, parameters);
            AddAnalyzerToWorkspace(workspace, analyzer);

            var document = GetDocumentAndSelectSpan(workspace, out var span);
            var allDiagnostics = await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(workspace, document, span, includeNonLocalDocumentDiagnostics: parameters.includeNonLocalDocumentDiagnostics);
            AssertNoAnalyzerExceptionDiagnostics(allDiagnostics);
            return allDiagnostics;
        }

        internal override async Task<(ImmutableArray<Diagnostic>, ImmutableArray<CodeAction>, CodeAction actionToInvoke)> GetDiagnosticAndFixesAsync(
            EditorTestWorkspace workspace, TestParameters parameters)
        {
            var (analyzer, fixer) = GetOrCreateDiagnosticProviderAndFixer(workspace, parameters);
            AddAnalyzerToWorkspace(workspace, analyzer);

            GetDocumentAndSelectSpanOrAnnotatedSpan(workspace, out var document, out var span, out var annotation);

            var testDriver = new TestDiagnosticAnalyzerDriver(workspace, includeNonLocalDocumentDiagnostics: parameters.includeNonLocalDocumentDiagnostics);
            var filterSpan = parameters.includeDiagnosticsOutsideSelection ? (TextSpan?)null : span;
            var diagnostics = (await testDriver.GetAllDiagnosticsAsync(document, filterSpan)).ToImmutableArray();
            AssertNoAnalyzerExceptionDiagnostics(diagnostics);

            if (fixer == null)
            {
                return (diagnostics, ImmutableArray<CodeAction>.Empty, null);
            }

            var ids = new HashSet<string>(fixer.FixableDiagnosticIds);
            var dxs = diagnostics.Where(d => ids.Contains(d.Id)).ToList();
            var (resultDiagnostics, codeActions, actionToInvoke) = await GetDiagnosticAndFixesAsync(
                dxs, fixer, testDriver, document, span, annotation, parameters.index);

            // If we are also testing non-fixable diagnostics,
            // then the result diagnostics need to include all diagnostics,
            // not just the fixable ones returned from GetDiagnosticAndFixesAsync.
            if (parameters.retainNonFixableDiagnostics)
            {
                resultDiagnostics = diagnostics;
            }

            return (resultDiagnostics, codeActions, actionToInvoke);
        }

        private protected async Task TestDiagnosticInfoAsync(
            string initialMarkup,
            string diagnosticId,
            DiagnosticSeverity diagnosticSeverity,
            OptionsCollectionAlias options = null,
            OptionsCollectionAlias globalOptions = null,
            LocalizableString diagnosticMessage = null)
        {
            await TestDiagnosticInfoAsync(initialMarkup, parseOptions: null, compilationOptions: null, options, globalOptions, diagnosticId, diagnosticSeverity, diagnosticMessage);
            await TestDiagnosticInfoAsync(initialMarkup, GetScriptOptions(), compilationOptions: null, options, globalOptions, diagnosticId, diagnosticSeverity, diagnosticMessage);
        }

        private protected async Task TestDiagnosticInfoAsync(
            string initialMarkup,
            ParseOptions parseOptions,
            CompilationOptions compilationOptions,
            OptionsCollectionAlias options,
            OptionsCollectionAlias globalOptions,
            string diagnosticId,
            DiagnosticSeverity diagnosticSeverity,
            LocalizableString diagnosticMessage = null)
        {
            var testOptions = new TestParameters(parseOptions, compilationOptions, options: options, globalOptions: globalOptions);
            using var workspace = CreateWorkspaceFromOptions(initialMarkup, testOptions);
            var diagnostics = (await GetDiagnosticsAsync(workspace, testOptions)).ToImmutableArray();
            diagnostics = diagnostics.WhereAsArray(d => d.Id == diagnosticId);
            Assert.Equal(1, diagnostics.Length);

            var hostDocument = workspace.Documents.Single(d => d.SelectedSpans.Any());
            var expected = hostDocument.SelectedSpans.Single();
            var actual = diagnostics.Single().Location.SourceSpan;
            Assert.Equal(expected, actual);

            Assert.Equal(diagnosticSeverity, diagnostics.Single().Severity);

            if (diagnosticMessage != null)
            {
                Assert.Equal(diagnosticMessage, diagnostics.Single().GetMessage());
            }
        }

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
        /// <summary>
        /// The internal method <see cref="AnalyzerExecutor.IsAnalyzerExceptionDiagnostic(Diagnostic)"/> does
        /// essentially this, but due to linked files between projects, this project cannot have internals visible
        /// access to the Microsoft.CodeAnalysis project without the cascading effect of many extern aliases, so it
        /// is re-implemented here in a way that is potentially overly aggressive with the knowledge that if this method
        /// starts failing on non-analyzer exception diagnostics, it can be appropriately tuned or re-evaluated.
        /// </summary>
        private static void AssertNoAnalyzerExceptionDiagnostics(IEnumerable<Diagnostic> diagnostics)
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
        {
            var analyzerExceptionDiagnostics = diagnostics.Where(diag => diag.Descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.AnalyzerException));
            AssertEx.Empty(analyzerExceptionDiagnostics, "Found analyzer exception diagnostics");
        }

        // This region provides instances of code fix providers from Features layers, such that the corresponding 
        // analyzer has been ported to CodeStyle layer, but not the fixer.
        // This enables porting the tests for the ported analyzer in CodeStyle layer.
        #region CodeFixProvider Helpers

        // https://github.com/dotnet/roslyn/issues/43091 blocks porting the fixer to CodeStyle layer.
        protected static CodeFixProvider GetCSharpUseAutoPropertyCodeFixProvider() => new CSharpUseAutoPropertyCodeFixProvider();

        // https://github.com/dotnet/roslyn/issues/43091 blocks porting the fixer to CodeStyle layer.
        protected static CodeFixProvider GetVisualBasicUseAutoPropertyCodeFixProvider() => new VisualBasicUseAutoPropertyCodeFixProvider();

        #endregion
    }
}
