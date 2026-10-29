// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

    /// <summary>
    /// True for nodes that Razor codegen synthesizes as compiler plumbing rather than
    /// content derived from user-authored source. Consumers that distinguish "user API
    /// surface" from "generator plumbing" -- such as the decl/impl partial-file split
    /// for components -- use this flag to decide where a node belongs.
    /// </summary>
    internal bool IsSynthesizedHelper { get; init; }

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

    /// <summary>
    ///  Returns a deep copy of this node and its descendants. The node-specific state is produced by
    ///  <see cref="CloneNode"/>; this method copies the common state (source span, imported flag,
    ///  diagnostics) and deep-clones the children onto it. A node with a property that aliases one of
    ///  its <see cref="Children"/> overrides this to re-point that property at the cloned child.
    /// </summary>
    internal virtual IntermediateNode Clone()
    {
        var clone = CloneNode();

        clone.Source = Source;
        clone.IsImported = IsImported;
        clone.AddDiagnosticsFromNode(this);

        foreach (var child in Children)
        {
            clone.Children.Add(child.Clone());
        }

        return clone;
    }

    /// <summary>
    ///  Creates a copy of this node carrying only its own state -- the node-specific fields (including the
    ///  init-only <see cref="IsSynthesizedHelper"/>) and any child nodes held outside <see cref="Children"/>
    ///  (deep-cloned). The common state and the <see cref="Children"/> are copied by <see cref="Clone"/>.
    ///  Overridden by every node kind that can appear in an unresolved tree; the base throws so an
    ///  unexpected kind fails loudly rather than silently producing an incomplete copy.
    /// </summary>
    protected virtual IntermediateNode CloneNode()
        => throw new NotSupportedException($"{GetType().Name} does not support cloning.");
}
