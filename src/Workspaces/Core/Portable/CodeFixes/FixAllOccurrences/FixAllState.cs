using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal partial class FixAllState
    {
        private readonly FixAllContext.DiagnosticProvider _diagnosticProvider;

        public string CodeActionEquivalenceKey { get; }
        public CodeFixProvider CodeFixProvider { get; }
        public ImmutableHashSet<string> DiagnosticIds { get; }
        public Document Document { get; }
        public Project Project { get; }
        public FixAllScope Scope { get; }
        public Solution Solution { get; }

        internal FixAllState(
            Document document,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string codeActionEquivalenceKey,
            IEnumerable<string> diagnosticIds,
            FixAllContext.DiagnosticProvider fixAllDiagnosticProvider)
            : this(document, document.Project, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, fixAllDiagnosticProvider)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }
        }

        internal FixAllState(
            Project project,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string codeActionEquivalenceKey,
            IEnumerable<string> diagnosticIds,
            FixAllContext.DiagnosticProvider fixAllDiagnosticProvider)
            : this(null, project, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, fixAllDiagnosticProvider)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }
        }

        private FixAllState(
            Document document,
            Project project,
            CodeFixProvider codeFixProvider,
            FixAllScope scope,
            string codeActionEquivalenceKey,
            IEnumerable<string> diagnosticIds,
            FixAllContext.DiagnosticProvider fixAllDiagnosticProvider)
        {
            Contract.ThrowIfNull(project);

            if (codeFixProvider == null)
            {
                throw new ArgumentNullException(nameof(codeFixProvider));
            }

            if (diagnosticIds == null)
            {
                throw new ArgumentNullException(nameof(diagnosticIds));
            }

            if (diagnosticIds.Any(d => d == null))
            {
                throw new ArgumentException(WorkspacesResources.DiagnosticCannotBeNull, nameof(diagnosticIds));
            }

            if (fixAllDiagnosticProvider == null)
            {
                throw new ArgumentNullException(nameof(fixAllDiagnosticProvider));
            }

            this.Document = document;
            this.Project = project;
            this.CodeFixProvider = codeFixProvider;
            this.Scope = scope;
            this.CodeActionEquivalenceKey = codeActionEquivalenceKey;
            this.DiagnosticIds = ImmutableHashSet.CreateRange(diagnosticIds);
            _diagnosticProvider = fixAllDiagnosticProvider;
        }

        public FixAllState GetStateForScopeAndActionId(FixAllScope scope, string codeActionEquivalenceKey)
        {
            throw new NotImplementedException();
        }

        public FixAllContext CreateFixAllContext(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal static FixAllState Create(
            Document document,
            FixAllProviderInfo fixAllProviderInfo,
            CodeFixProvider originalFixProvider,
            IEnumerable<Diagnostic> originalFixDiagnostics,
            Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync,
            Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync)
        {
            var diagnosticIds = GetFixAllDiagnosticIds(fixAllProviderInfo, originalFixDiagnostics).ToImmutableHashSet();
            var diagnosticProvider = new FixAllDiagnosticProvider(diagnosticIds, getDocumentDiagnosticsAsync, getProjectDiagnosticsAsync);
            return new FixAllState(
                document: document,
                codeFixProvider: originalFixProvider,
                scope: FixAllScope.Document,
                codeActionEquivalenceKey: null,
                diagnosticIds: diagnosticIds,
                fixAllDiagnosticProvider: diagnosticProvider);
        }

        internal static FixAllState Create(
            Project project,
            FixAllProviderInfo fixAllProviderInfo,
            CodeFixProvider originalFixProvider,
            IEnumerable<Diagnostic> originalFixDiagnostics,
            Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync,
            Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync)
        {
            var diagnosticIds = GetFixAllDiagnosticIds(fixAllProviderInfo, originalFixDiagnostics).ToImmutableHashSet();
            var diagnosticProvider = new FixAllDiagnosticProvider(diagnosticIds, getDocumentDiagnosticsAsync, getProjectDiagnosticsAsync);
            return new FixAllState(
                project: project,
                codeFixProvider: originalFixProvider,
                scope: FixAllScope.Project,
                codeActionEquivalenceKey: null, diagnosticIds: diagnosticIds,
                fixAllDiagnosticProvider: diagnosticProvider);
        }

        private static IEnumerable<string> GetFixAllDiagnosticIds(FixAllProviderInfo fixAllProviderInfo, IEnumerable<Diagnostic> originalFixDiagnostics)
        {
            return originalFixDiagnostics
                .Where(fixAllProviderInfo.CanBeFixed)
                .Select(d => d.Id);
        }

        #region FixMultiple

        internal static FixAllState Create(
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnosticsToFix,
            CodeFixProvider codeFixProvider,
            string codeActionEquivalenceKey)
        {
            var triggerDocument = diagnosticsToFix.First().Key;
            var diagnosticIds = GetDiagnosticsIds(diagnosticsToFix.Values);
            var diagnosticProvider = new FixMultipleDiagnosticProvider(diagnosticsToFix);
            return new FixAllState(
                triggerDocument, codeFixProvider, FixAllScope.Custom, codeActionEquivalenceKey, diagnosticIds, diagnosticProvider);
        }

        internal static FixAllState Create(
            ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsToFix,
            CodeFixProvider codeFixProvider,
            string codeActionEquivalenceKey)
        {
            var triggerProject = diagnosticsToFix.First().Key;
            var diagnosticIds = GetDiagnosticsIds(diagnosticsToFix.Values);
            var diagnosticProvider = new FixMultipleDiagnosticProvider(diagnosticsToFix);
            return new FixAllState(
                triggerProject, codeFixProvider, FixAllScope.Custom, codeActionEquivalenceKey, diagnosticIds, diagnosticProvider);
        }

        private static ImmutableHashSet<string> GetDiagnosticsIds(IEnumerable<ImmutableArray<Diagnostic>> diagnosticsCollection)
        {
            var uniqueIds = ImmutableHashSet.CreateBuilder<string>();
            foreach (var diagnostics in diagnosticsCollection)
            {
                foreach (var diagnostic in diagnostics)
                {
                    uniqueIds.Add(diagnostic.Id);
                }
            }

            return uniqueIds.ToImmutable();
        }

        #endregion
    }
}