// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;
using Roslyn.Utilities;
using Xunit;
using System.Collections.Concurrent;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    public abstract class AbstractDiagnosticProviderBasedUserDiagnosticTest : AbstractUserDiagnosticTest
    {
        private readonly ConcurrentDictionary<Workspace, Tuple<DiagnosticAnalyzer, CodeFixProvider>> _analyzerAndFixerMap =
            new ConcurrentDictionary<Workspace, Tuple<DiagnosticAnalyzer, CodeFixProvider>>();

        internal abstract Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace);

        private Tuple<DiagnosticAnalyzer, CodeFixProvider> GetOrCreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return _analyzerAndFixerMap.GetOrAdd(workspace, CreateDiagnosticProviderAndFixer);
        }

        internal async override Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(TestWorkspace workspace)
        {
            var providerAndFixer = GetOrCreateDiagnosticProviderAndFixer(workspace);

            var provider = providerAndFixer.Item1;
            TextSpan span;
            var document = GetDocumentAndSelectSpan(workspace, out span);
            var allDiagnostics = await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(provider, document, span);
            AssertNoAnalyzerExceptionDiagnostics(allDiagnostics);
            return allDiagnostics;
        }

        internal override async Task<IEnumerable<Tuple<Diagnostic, CodeFixCollection>>> GetDiagnosticAndFixesAsync(TestWorkspace workspace, string fixAllActionId)
        {
            var providerAndFixer = GetOrCreateDiagnosticProviderAndFixer(workspace);

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
