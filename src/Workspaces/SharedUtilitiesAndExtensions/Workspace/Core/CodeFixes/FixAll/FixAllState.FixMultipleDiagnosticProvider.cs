// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal partial class FixAllState
    {
        /// <summary>
        /// Diagnostic provider to fetch document/project diagnostics to fix in a <see cref="FixAllContext"/>.
        /// </summary>
        internal sealed class FixMultipleDiagnosticProvider : FixAllContext.DiagnosticProvider
        {
            public ImmutableDictionary<Document, ImmutableArray<Diagnostic>> DocumentDiagnosticsMap { get; }
            public ImmutableDictionary<Project, ImmutableArray<Diagnostic>> ProjectDiagnosticsMap { get; }

            public FixMultipleDiagnosticProvider(ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnosticsMap)
            {
                DocumentDiagnosticsMap = diagnosticsMap;
                ProjectDiagnosticsMap = ImmutableDictionary<Project, ImmutableArray<Diagnostic>>.Empty;
            }

            public FixMultipleDiagnosticProvider(ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsMap)
            {
                ProjectDiagnosticsMap = diagnosticsMap;
                DocumentDiagnosticsMap = ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty;
            }

            public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            {
                var allDiagnosticsBuilder = ArrayBuilder<Diagnostic>.GetInstance();
                ImmutableArray<Diagnostic> diagnostics;
                if (!DocumentDiagnosticsMap.IsEmpty)
                {
                    foreach (var document in project.Documents)
                    {
                        if (DocumentDiagnosticsMap.TryGetValue(document, out diagnostics))
                        {
                            allDiagnosticsBuilder.AddRange(diagnostics);
                        }
                    }
                }

                if (ProjectDiagnosticsMap.TryGetValue(project, out diagnostics))
                {
                    allDiagnosticsBuilder.AddRange(diagnostics);
                }

                return Task.FromResult<IEnumerable<Diagnostic>>(allDiagnosticsBuilder.ToImmutableAndFree());
            }

            public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
            {
                if (DocumentDiagnosticsMap.TryGetValue(document, out var diagnostics))
                {
                    return Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);
                }

                return SpecializedTasks.EmptyEnumerable<Diagnostic>();
            }

            public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            {
                if (ProjectDiagnosticsMap.TryGetValue(project, out var diagnostics))
                {
                    return Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);
                }

                return SpecializedTasks.EmptyEnumerable<Diagnostic>();
            }
        }
    }
}
