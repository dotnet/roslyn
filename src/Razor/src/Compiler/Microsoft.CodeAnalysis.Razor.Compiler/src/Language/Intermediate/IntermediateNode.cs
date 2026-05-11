// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public abstract class IntermediateNode
{
    private ImmutableArray<RazorDiagnostic>.Builder? _diagnosticsBuilder;
    private ImmutableArray<RazorDiagnostic>? _diagnostics;

    public ImmutableArray<RazorDiagnostic> Diagnostics
        => _diagnostics ??= _diagnosticsBuilder?.ToImmutable() ?? [];

    public bool HasDiagnostics => _diagnosticsBuilder is { Count: > 0 };

    public SourceSpan? Source { get; set; }

    public bool IsImported { get; set; }

    public abstract IntermediateNodeCollection Children { get; }

    public abstract void Accept(IntermediateNodeVisitor visitor);

    public void AddDiagnostic(RazorDiagnostic diagnostic)
    {
        _diagnosticsBuilder ??= ImmutableArray.CreateBuilder<RazorDiagnostic>();
        _diagnosticsBuilder.Add(diagnostic);
        _diagnostics = null;
    }

    public void AddDiagnosticsFromNode(IntermediateNode node)
    {
        if (node.HasDiagnostics)
        {
            _diagnosticsBuilder ??= ImmutableArray.CreateBuilder<RazorDiagnostic>();
            _diagnosticsBuilder.AddRange(node.Diagnostics);
            _diagnostics = null;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members")]
    private string Tree
    {
        get
        {
            using var _ = StringBuilderPool.GetPooledObject(out var builder);

            var formatter = new IntermediateNodeFormatter(builder);
            formatter.FormatTree(this);

            return builder.ToString();
        }
    }

    internal string GetDebuggerDisplay()
    {
        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        var formatter = new IntermediateNodeFormatter(builder);
        formatter.FormatNode(this);

        return builder.ToString();
    }

    public virtual void FormatNode(IntermediateNodeFormatter formatter)
    {
    }
}
