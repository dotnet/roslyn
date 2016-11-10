// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
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
        private readonly ConcurrentDictionary<Workspace, Tuple<DiagnosticAnalyzer, CodeFixProvider>> _analyzerAndFixerMap =
            new ConcurrentDictionary<Workspace, Tuple<DiagnosticAnalyzer, CodeFixProvider>>();

        internal abstract Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace);

        internal virtual Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(
            Workspace workspace, object fixProviderData)
        {
            return CreateDiagnosticProviderAndFixer(workspace);
        }

        private Tuple<DiagnosticAnalyzer, CodeFixProvider> GetOrCreateDiagnosticProviderAndFixer(
            Workspace workspace, object fixProviderData)
        {
            return fixProviderData == null
                ? _analyzerAndFixerMap.GetOrAdd(workspace, CreateDiagnosticProviderAndFixer)
                : CreateDiagnosticProviderAndFixer(workspace, fixProviderData);
        }

        internal async override Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(
            TestWorkspace workspace, object fixProviderData = null)
        {
            var providerAndFixer = GetOrCreateDiagnosticProviderAndFixer(workspace, fixProviderData);

            var provider = providerAndFixer.Item1;
            TextSpan span;
            var document = GetDocumentAndSelectSpan(workspace, out span);
            var allDiagnostics = await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(provider, document, span);
            AssertNoAnalyzerExceptionDiagnostics(allDiagnostics);
            return allDiagnostics;
        }

        internal override async Task<IEnumerable<Tuple<Diagnostic, CodeFixCollection>>> GetDiagnosticAndFixesAsync(
            TestWorkspace workspace, string fixAllActionId, object fixProviderData)
        {
            var providerAndFixer = GetOrCreateDiagnosticProviderAndFixer(workspace, fixProviderData);

            var provider = providerAndFixer.Item1;
            Document document;
            TextSpan span;
            string annotation = null;
            if (!TryGetDocumentAndSelectSpan(workspace, out document, out span))
            {
                document = GetDocumentAndAnnotatedSpan(workspace, out annotation, out span);
            }

            using (var testDriver = new TestDiagnosticAnalyzerDriver(document.Project, provider))
            {
                var diagnostics = await testDriver.GetAllDiagnosticsAsync(provider, document, span);
                AssertNoAnalyzerExceptionDiagnostics(diagnostics);

                var fixer = providerAndFixer.Item2;
                var ids = new HashSet<string>(fixer.FixableDiagnosticIds);
                var dxs = diagnostics.Where(d => ids.Contains(d.Id)).ToList();
                return await GetDiagnosticAndFixesAsync(dxs, provider, fixer, testDriver, document, span, annotation, fixAllActionId);
            }
        }

        protected async Task TestDiagnosticSeverityAndCountAsync(
            string initialMarkup,
            IDictionary<OptionKey, object> options,
            int diagnosticCount,
            string diagnosticId,
            DiagnosticSeverity diagnosticSeverity)
        {
            await TestDiagnosticSeverityAndCountAsync(initialMarkup, null, null, options, diagnosticCount, diagnosticId, diagnosticSeverity);
            await TestDiagnosticSeverityAndCountAsync(initialMarkup, GetScriptOptions(), null, options, diagnosticCount, diagnosticId, diagnosticSeverity);
        }

        protected async Task TestDiagnosticSeverityAndCountAsync(
            string initialMarkup,
            ParseOptions parseOptions,
            CompilationOptions compilationOptions,
            IDictionary<OptionKey, object> options,
            int diagnosticCount,
            string diagnosticId,
            DiagnosticSeverity diagnosticSeverity)
        {
            using (var workspace = await CreateWorkspaceFromFileAsync(initialMarkup, parseOptions, compilationOptions))
            {
                workspace.ApplyOptions(options);

                var diagnostics = (await GetDiagnosticsAsync(workspace)).Where(d => d.Id == diagnosticId);
                Assert.Equal(diagnosticCount, diagnostics.Count());
                Assert.Equal(diagnosticSeverity, diagnostics.Single().Severity);
            }
        }

        /// <summary>
        /// The internal method <see cref="AnalyzerExecutor.IsAnalyzerExceptionDiagnostic(Diagnostic)"/> does
        /// essentially this, but due to linked files between projects, this project cannot have internals visible
        /// access to the Microsoft.CodeAnalysis project without the cascading effect of many extern aliases, so it
        /// is re-implemented here in a way that is potentially overly aggressive with the knowledge that if this method
        /// starts failing on non-analyzer exception diagnostics, it can be appropriately tuned or re-evaluated.
        /// </summary>
        private void AssertNoAnalyzerExceptionDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            var analyzerExceptionDiagnostics = diagnostics.Where(diag => diag.Descriptor.CustomTags.Contains(WellKnownDiagnosticTags.AnalyzerException));
            AssertEx.Empty(analyzerExceptionDiagnostics, "Found analyzer exception diagnostics");
        }
    }
}
