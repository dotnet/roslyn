// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Immutable;
using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Context for "Fix multiple occurrences" code fixes provided by an <see cref="FixAllProvider"/>.
    /// </summary>
    internal partial class FixMultipleContext
    {
        /// <summary>
        /// Diagnostic provider to fetch document/project diagnostics to fix in a <see cref="FixMultipleContext"/>.
        /// </summary>
        public sealed class FixMultipleDiagnosticProvider : DiagnosticProvider
        {
            private readonly ImmutableDictionary<Document, ImmutableArray<Diagnostic>> _documentDiagnosticsMap;
            private readonly ImmutableDictionary<Project, ImmutableArray<Diagnostic>> _projectDiagnosticsMap;

            public FixMultipleDiagnosticProvider(ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnosticsMap)
            {
                _documentDiagnosticsMap = diagnosticsMap;
                _projectDiagnosticsMap = ImmutableDictionary<Project, ImmutableArray<Diagnostic>>.Empty;
            }

            public FixMultipleDiagnosticProvider(ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsMap)
            {
                _projectDiagnosticsMap = diagnosticsMap;
                _documentDiagnosticsMap = ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty;
            }

            public ImmutableDictionary<Document, ImmutableArray<Diagnostic>> DocumentDiagnosticsToFix => _documentDiagnosticsMap;
            public ImmutableDictionary<Project, ImmutableArray<Diagnostic>> ProjectDiagnosticsToFix => _projectDiagnosticsMap;

            public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            {
                ImmutableArray<Diagnostic>.Builder allDiagnosticsBuilder = null;
                ImmutableArray<Diagnostic> diagnostics;
                if (!_documentDiagnosticsMap.IsEmpty)
                {
                    foreach (var document in project.Documents)
                    {
                        if (_documentDiagnosticsMap.TryGetValue(document, out diagnostics))
                        {
                            allDiagnosticsBuilder = allDiagnosticsBuilder ?? ImmutableArray.CreateBuilder<Diagnostic>(diagnostics.Length);
                            allDiagnosticsBuilder.AddRange(diagnostics);
                        }
                    }
                }

                if (_projectDiagnosticsMap.TryGetValue(project, out diagnostics))
                {
                    allDiagnosticsBuilder = allDiagnosticsBuilder ?? ImmutableArray.CreateBuilder<Diagnostic>(diagnostics.Length);
                    allDiagnosticsBuilder.AddRange(diagnostics);
                }

                IEnumerable<Diagnostic> allDiagnostics = allDiagnosticsBuilder != null ? allDiagnosticsBuilder.ToImmutable() : ImmutableArray<Diagnostic>.Empty;
                return Task.FromResult(allDiagnostics);
            }

            public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
            {
                ImmutableArray<Diagnostic> diagnostics;
                if (_documentDiagnosticsMap.TryGetValue(document, out diagnostics))
                {
                    return Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);
                }

                return Task.FromResult(SpecializedCollections.EmptyEnumerable<Diagnostic>());
            }

            public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            {
                ImmutableArray<Diagnostic> diagnostics;
                if (_projectDiagnosticsMap.TryGetValue(project, out diagnostics))
                {
                    return Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);
                }

                return Task.FromResult(SpecializedCollections.EmptyEnumerable<Diagnostic>());
            }
        }
    }
}
