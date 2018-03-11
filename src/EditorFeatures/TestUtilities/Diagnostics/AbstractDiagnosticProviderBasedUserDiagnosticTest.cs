// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    public abstract class AbstractDiagnosticProviderBasedUserDiagnosticTest : AbstractUserDiagnosticTest
    {
        private readonly ConcurrentDictionary<Workspace, (DiagnosticAnalyzer, CodeFixProvider)> _analyzerAndFixerMap =
            new ConcurrentDictionary<Workspace, (DiagnosticAnalyzer, CodeFixProvider)>();

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

        internal async override Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(
            TestWorkspace workspace, TestParameters parameters)
        {
            var providerAndFixer = GetOrCreateDiagnosticProviderAndFixer(workspace, parameters);

            var provider = providerAndFixer.Item1;
            var document = GetDocumentAndSelectSpan(workspace, out var span);
            var allDiagnostics = await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(provider, document, span);
            AssertNoAnalyzerExceptionDiagnostics(allDiagnostics);
            return allDiagnostics;
        }

        internal override async Task<IEnumerable<Tuple<Diagnostic, CodeFixCollection>>> GetDiagnosticAndFixesAsync(
            TestWorkspace workspace, TestParameters parameters)
        {
            var providerAndFixer = GetOrCreateDiagnosticProviderAndFixer(workspace, parameters);

            var provider = providerAndFixer.Item1;
            string annotation = null;
            if (!TryGetDocumentAndSelectSpan(workspace, out var document, out var span))
            {
                document = GetDocumentAndAnnotatedSpan(workspace, out annotation, out span);
            }

            var testDriver = new TestDiagnosticAnalyzerDriver(document.Project, provider);
            var diagnostics = await testDriver.GetAllDiagnosticsAsync(provider, document, span);
            AssertNoAnalyzerExceptionDiagnostics(diagnostics);

            var fixer = providerAndFixer.Item2;
            if (fixer == null)
            {
                return diagnostics.Select(d => Tuple.Create(d, (CodeFixCollection) null));
            }
            
            var ids = new HashSet<string>(fixer.FixableDiagnosticIds);
            var dxs = diagnostics.Where(d => ids.Contains(d.Id)).ToList();
            return await GetDiagnosticAndFixesAsync(
                dxs, provider, fixer, testDriver, document, span, annotation, parameters.fixAllActionEquivalenceKey);
        }

        protected async Task TestDiagnosticInfoAsync(
            string initialMarkup,
            IDictionary<OptionKey, object> options,
            string diagnosticId,
            DiagnosticSeverity diagnosticSeverity,
            LocalizableString diagnosticMessage = null)
        {
            await TestDiagnosticInfoAsync(initialMarkup, null, null, options, diagnosticId, diagnosticSeverity, diagnosticMessage);
            await TestDiagnosticInfoAsync(initialMarkup, GetScriptOptions(), null, options, diagnosticId, diagnosticSeverity, diagnosticMessage);
        }

        protected async Task TestDiagnosticInfoAsync(
            string initialMarkup,
            ParseOptions parseOptions,
            CompilationOptions compilationOptions,
            IDictionary<OptionKey, object> options,
            string diagnosticId,
            DiagnosticSeverity diagnosticSeverity,
            LocalizableString diagnosticMessage = null)
        {
            var testOptions = new TestParameters(parseOptions, compilationOptions, options);
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, testOptions))
            {
                var diagnostics = (await GetDiagnosticsAsync(workspace, testOptions)).Where(d => d.Id == diagnosticId);
                Assert.Equal(1, diagnostics.Count());

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
        }

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
        /// <summary>
        /// The internal method <see cref="AnalyzerExecutor.IsAnalyzerExceptionDiagnostic(Diagnostic)"/> does
        /// essentially this, but due to linked files between projects, this project cannot have internals visible
        /// access to the Microsoft.CodeAnalysis project without the cascading effect of many extern aliases, so it
        /// is re-implemented here in a way that is potentially overly aggressive with the knowledge that if this method
        /// starts failing on non-analyzer exception diagnostics, it can be appropriately tuned or re-evaluated.
        /// </summary>
        private void AssertNoAnalyzerExceptionDiagnostics(IEnumerable<Diagnostic> diagnostics)
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
        {
            var analyzerExceptionDiagnostics = diagnostics.Where(diag => diag.Descriptor.CustomTags.Contains(WellKnownDiagnosticTags.AnalyzerException));
            AssertEx.Empty(analyzerExceptionDiagnostics, "Found analyzer exception diagnostics");
        }
    }
}
