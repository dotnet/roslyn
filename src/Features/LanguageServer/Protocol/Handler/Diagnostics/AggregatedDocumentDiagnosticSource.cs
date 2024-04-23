//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Diagnostics;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.CodeAnalysis.Diagnostics;
//using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
//using Microsoft.CodeAnalysis.PooledObjects;

//namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

///// <summary>
///// Aggregates multiple source diagnostics
///// </summary>
//internal sealed class AggregatedDocumentDiagnosticSource : AbstractDocumentDiagnosticSource<TextDocument>
//{
//    private readonly IDiagnosticSourceManager _diagnosticSourceManager;
//    private readonly string? _sourceName;

//    public AggregatedDocumentDiagnosticSource(IDiagnosticSourceManager diagnosticSourceManager,
//        TextDocument document, string? sourceName) : base(document)
//    {
//        _diagnosticSourceManager = diagnosticSourceManager;
//        _sourceName = sourceName;
//    }

//    public override bool IsLiveSource() => true;

//    public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken)
//    {
//        using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var diagnostics);
//        foreach (var name in GetSourceNames())
//        {
//            var sources = await _diagnosticSourceManager.CreateDiagnosticSourcesAsync(context, name, true, cancellationToken).ConfigureAwait(false);
//            foreach (var source in sources)
//            {
//                Debug.Assert(source.IsLiveSource(), "All document sources should be live");
//                var namedDiagnostics = await source.GetDiagnosticsAsync(context, cancellationToken).ConfigureAwait(false);
//                diagnostics.AddRange(namedDiagnostics);
//            }
//        }

//        return diagnostics.ToImmutableAndClear();
//    }

//    private IEnumerable<string> GetSourceNames()
//    {
//        if (this._sourceName != null)
//        {
//            yield return this._sourceName;
//        }
//        else
//        {
//            foreach (var name in _diagnosticSourceManager.GetSourceNames(isDocument: true))
//            {
//                if (name != PullDiagnosticCategories.Task)
//                    yield return name;
//            }
//        }
//    }
//}
