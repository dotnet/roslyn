// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Metalama.Compiler;


/// <summary>
///     Processes a <see cref="Diagnostic" /> through a set of <see cref="DiagnosticFilter" /> and to determine
///     if the <see cref="Diagnostic" /> must be suppressed.
/// </summary>
public sealed class DiagnosticFilterRunner
{
    private readonly Compilation _compilation;
    private readonly Func<SyntaxTree, SemanticModel> _getSemanticModel;
    private readonly DiagnosticFilterCollection _filters;

    public DiagnosticFilterRunner(Compilation compilation, Func<SyntaxTree, SemanticModel> getSemanticModel, DiagnosticFilterCollection filters)
    {
        _compilation = compilation;
        _getSemanticModel = getSemanticModel;
        _filters = filters;
    }

    public bool TryGetSuppression(Diagnostic diagnostic, CancellationToken cancellationToken, out Suppression suppression)
    {
        var location = diagnostic.Location;
        var filePath = location.SourceTree?.FilePath;

        if (filePath == null)
        {
            suppression = default;
            return false;
        }

        if (!_filters.TryGetFilters(filePath, diagnostic.Id, out var filters))
        {
            suppression = default;
            return false;
        }


        var model = _getSemanticModel(location.SourceTree!);

        var reportedNode = location.SourceTree!.GetRoot().FindNode(location.SourceSpan, getInnermostNodeForTie: true);

        for (var node = reportedNode; node != null; node = node.Parent)
        {
            var declaredSymbol = model.GetDeclaredSymbol(node, cancellationToken);

            if (declaredSymbol != null)
            {
                DiagnosticFilteringRequest request = new(diagnostic, node, _compilation, declaredSymbol);

                foreach (var filter in filters)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (filter.Filter(request))
                    {
                        suppression = Suppression.Create(filter.Descriptor, diagnostic);
                        return true;
                    }
                }
            }
        }

        suppression = default;
        return false;
    }
}
