// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;
using Roslyn.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    public abstract class AbstractSuppressionDiagnosticTest : AbstractUserDiagnosticTest
    {
        protected AbstractSuppressionDiagnosticTest(ITestOutputHelper logger = null)
            : base(logger)
        {
        }

        protected abstract int CodeActionIndex { get; }
        protected virtual bool IncludeSuppressedDiagnostics => false;
        protected virtual bool IncludeUnsuppressedDiagnostics => true;
        protected virtual bool IncludeNoLocationDiagnostics => true;

        protected Task TestAsync(string initial, string expected)
            => TestAsync(initial, expected, parseOptions: null, index: CodeActionIndex);

        internal abstract Tuple<DiagnosticAnalyzer, IConfigurationFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace);

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
        {
            return actions.SelectMany(a => a is AbstractConfigurationActionWithNestedActions
                ? a.NestedCodeActions
                : ImmutableArray.Create(a)).ToImmutableArray();
        }

        private ImmutableArray<Diagnostic> FilterDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            if (!IncludeNoLocationDiagnostics)
            {
                diagnostics = diagnostics.Where(d => d.Location.IsInSource);
            }

            if (!IncludeSuppressedDiagnostics)
            {
                diagnostics = diagnostics.Where(d => !d.IsSuppressed);
            }

            if (!IncludeUnsuppressedDiagnostics)
            {
                diagnostics = diagnostics.Where(d => d.IsSuppressed);
            }

            return diagnostics.ToImmutableArray();
        }

        internal override async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(
            TestWorkspace workspace, TestParameters parameters)
        {
            var (analyzer, _) = CreateDiagnosticProviderAndFixer(workspace);
            AddAnalyzerToWorkspace(workspace, analyzer, parameters);

            var document = GetDocumentAndSelectSpan(workspace, out var span);
            var diagnostics = await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(workspace, document, span);
            return FilterDiagnostics(diagnostics);
        }

        internal override async Task<(ImmutableArray<Diagnostic>, ImmutableArray<CodeAction>, CodeAction actionToInvoke)> GetDiagnosticAndFixesAsync(
            TestWorkspace workspace, TestParameters parameters)
        {
            var (analyzer, fixer) = CreateDiagnosticProviderAndFixer(workspace);
            AddAnalyzerToWorkspace(workspace, analyzer, parameters);

            string annotation = null;
            if (!TryGetDocumentAndSelectSpan(workspace, out var document, out var span))
            {
                document = GetDocumentAndAnnotatedSpan(workspace, out annotation, out span);
            }

            var testDriver = new TestDiagnosticAnalyzerDriver(workspace, document.Project, includeSuppressedDiagnostics: IncludeSuppressedDiagnostics);
            var diagnostics = (await testDriver.GetAllDiagnosticsAsync(document, span))
                .Where(d => fixer.IsFixableDiagnostic(d));

            var filteredDiagnostics = FilterDiagnostics(diagnostics);

            var wrapperCodeFixer = new WrapperCodeFixProvider(fixer, filteredDiagnostics.Select(d => d.Id));
            return await GetDiagnosticAndFixesAsync(
                filteredDiagnostics, wrapperCodeFixer, testDriver, document,
                span, CodeActionOptions.Default, annotation, parameters.index);
        }
    }
}
