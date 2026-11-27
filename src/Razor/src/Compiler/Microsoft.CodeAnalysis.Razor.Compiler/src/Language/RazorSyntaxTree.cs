// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorSyntaxTree
{
    internal SyntaxNode Root { get; }
    public RazorParserOptions Options { get; }
    public RazorSourceDocument Source { get; }

    private readonly ImmutableArray<RazorDiagnostic> _diagnostics;
    private ImmutableArray<RazorDiagnostic> _allDiagnostics;

    internal RazorSyntaxTree(
        SyntaxNode root,
        RazorSourceDocument source,
        ImmutableArray<RazorDiagnostic> diagnostics,
        RazorParserOptions options)
    {
        ArgHelper.ThrowIfNull(root);
        ArgHelper.ThrowIfNull(source);
        ArgHelper.ThrowIfNull(options);

        Root = root;
        Source = source;
        _diagnostics = diagnostics.NullToEmpty();
        Options = options;
    }

    public ImmutableArray<RazorDiagnostic> Diagnostics
    {
        get
        {
            if (_allDiagnostics.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref _allDiagnostics, ComputeAllDiagnostics(_diagnostics, Root));
            }

            return _allDiagnostics;

            static ImmutableArray<RazorDiagnostic> ComputeAllDiagnostics(ImmutableArray<RazorDiagnostic> treeDiagnostics, SyntaxNode root)
            {
                using var pooledList = ListPool<RazorDiagnostic>.GetPooledObject(out var rootDiagnostics);
                using var diagnosticSet = new PooledHashSet<RazorDiagnostic>();

                foreach (var diagnostic in treeDiagnostics)
                {
                    diagnosticSet.Add(diagnostic);
                }

                root.CollectAllDiagnostics(rootDiagnostics);

                if (rootDiagnostics.Count > 0)
                {
                    foreach (var diagnostic in rootDiagnostics)
                    {
                        diagnosticSet.Add(diagnostic);
                    }
                }

                return diagnosticSet.OrderByAsArray(static d => d.Span.AbsoluteIndex);
            }
        }
    }

    public static RazorSyntaxTree Parse(
        RazorSourceDocument source,
        RazorParserOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgHelper.ThrowIfNull(source);

        options ??= RazorParserOptions.Default;
        var parser = new RazorParser(options);
        return parser.Parse(source, cancellationToken);
    }
}
