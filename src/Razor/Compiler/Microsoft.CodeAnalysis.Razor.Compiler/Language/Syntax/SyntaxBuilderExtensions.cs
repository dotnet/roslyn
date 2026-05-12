// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class SyntaxBuilderExtensions
{
    /// <summary>
    ///  Produces a <see cref="GreenNode"/> for a <see cref="InternalSyntax.SyntaxList"/> from the
    ///  contents of <paramref name="builder"/>.
    /// </summary>
    public static GreenNode? ToGreenListNode<TNode>(ref readonly this PooledArrayBuilder<TNode> builder)
        where TNode : SyntaxNode
    {
        switch (builder.Count)
        {
            case 0:
                return null;
            case 1:
                return builder[0].Green;
            case 2:
                return InternalSyntax.SyntaxList.List(builder[0].Green, builder[1].Green);
            case 3:
                return InternalSyntax.SyntaxList.List(builder[0].Green, builder[1].Green, builder[2].Green);

            case int count:
                var copy = new ArrayElement<GreenNode>[count];

                for (var i = 0; i < count; i++)
                {
                    copy[i].Value = builder[i].Green;
                }

                return InternalSyntax.SyntaxList.List(copy);
        }
    }

    /// <summary>
    ///  Produces a <see cref="GreenNode"/> for a <see cref="InternalSyntax.SyntaxList"/> from the
    ///  contents of <paramref name="builder"/>.
    /// </summary>
    public static GreenNode? ToGreenListNode(ref readonly this PooledArrayBuilder<SyntaxToken> builder)
    {
        switch (builder.Count)
        {
            case 0:
                return null;
            case 1:
                return builder[0].Node;
            case 2:
                return InternalSyntax.SyntaxList.List(builder[0].Node, builder[1].Node);
            case 3:
                return InternalSyntax.SyntaxList.List(builder[0].Node, builder[1].Node, builder[2].Node);

            case int count:
                var copy = new ArrayElement<GreenNode>[count];

                for (var i = 0; i < count; i++)
                {
                    copy[i].Value = builder[i].Node!;
                }

                return InternalSyntax.SyntaxList.List(copy);
        }
    }

    /// <summary>
    ///  Produces a <see cref="GreenNode"/> for a <see cref="InternalSyntax.SyntaxList"/> from the
    ///  contents of <paramref name="builder"/> and clears it.
    /// </summary>
    /// 
    /// <remarks>
    ///  <para>
    ///   Because this method mutates <paramref name="builder"/>, <c>builder.AsRef()</c> must be
    ///   called if it is is declared in a <c>using</c> statement.
    ///  </para>
    ///
    ///  <code>
    ///  using PooledArrayBuilder&lt;GreenNode&gt; builder = [];
    ///
    ///  // Use AsRef() inline
    ///  builder.AsRef().ToGreenListNodeAndClear();
    ///
    ///  // Declare a local ref variable with AsRef() to avoid taking a ref multiple times.
    ///  ref PooledArrayBuilder&lt;GreenNode&gt; builderRef = ref builder.AsRef();
    ///  builderRef.ToGreenListNodeAndClear();
    ///  </code>
    /// </remarks>
    public static GreenNode? ToGreenListNodeAndClear<TNode>(ref this PooledArrayBuilder<TNode> builder)
        where TNode : SyntaxNode
    {
        var result = builder.ToGreenListNode();
        builder.Clear();

        return result;
    }

    /// <summary>
    ///  Produces a <see cref="GreenNode"/> for a <see cref="InternalSyntax.SyntaxList"/> from the
    ///  contents of <paramref name="builder"/> and clears it.
    /// </summary>
    /// 
    /// <remarks>
    ///  <para>
    ///   Because this method mutates <paramref name="builder"/>, <c>builder.AsRef()</c> must be
    ///   called if it is is declared in a <c>using</c> statement.
    ///  </para>
    ///
    ///  <code>
    ///  using PooledArrayBuilder&lt;SyntaxToken&gt; builder = [];
    ///
    ///  // Use AsRef() inline
    ///  builder.AsRef().ToGreenListNodeAndClear();
    ///
    ///  // Declare a local ref variable with AsRef() to avoid taking a ref multiple times.
    ///  ref PooledArrayBuilder&lt;SyntaxToken&gt; builderRef = ref builder.AsRef();
    ///  builderRef.ToGreenListNodeAndClear();
    ///  </code>
    /// </remarks>
    public static GreenNode? ToGreenListNodeAndClear(ref this PooledArrayBuilder<SyntaxToken> builder)
    {
        var result = builder.ToGreenListNode();
        builder.Clear();

        return result;
    }

    /// <summary>
    ///  Produces a <see cref="SyntaxNode"/> for a <see cref="SyntaxList"/> from the
    ///  contents of <paramref name="builder"/>.
    /// </summary>
    public static SyntaxNode? ToListNode<TNode>(ref readonly this PooledArrayBuilder<TNode> builder)
        where TNode : SyntaxNode
        => builder.ToListNode(parent: null, position: 0);

    /// <summary>
    ///  Produces a <see cref="SyntaxNode"/> with the given <paramref name="parent"/>
    ///  for a <see cref="SyntaxList"/> from the contents of <paramref name="builder"/>.
    /// </summary>
    public static SyntaxNode? ToListNode<TNode>(
        ref readonly this PooledArrayBuilder<TNode> builder, SyntaxNode parent)
        where TNode : SyntaxNode
        => builder.ToListNode(parent, parent.Position);

    /// <summary>
    ///  Produces a <see cref="SyntaxNode"/> with the given <paramref name="parent"/>
    ///  and <paramref name="position"/> for a <see cref="SyntaxList"/> from the contents
    ///  of <paramref name="builder"/>.
    /// </summary>
    public static SyntaxNode? ToListNode<TNode>(
        ref readonly this PooledArrayBuilder<TNode> builder, SyntaxNode? parent, int position)
        where TNode : SyntaxNode
        => builder.ToGreenListNode() is GreenNode listNode
            ? listNode.CreateRed(parent, position)
            : null;

    /// <summary>
    ///  Produces a <see cref="SyntaxNode"/> for a <see cref="SyntaxList"/> from the
    ///  contents of <paramref name="builder"/>.
    /// </summary>
    public static SyntaxNode? ToListNode(ref readonly this PooledArrayBuilder<SyntaxToken> builder)
        => builder.ToListNode(parent: null, position: 0);

    /// <summary>
    ///  Produces a <see cref="SyntaxNode"/> with the given <paramref name="parent"/>
    ///  for a <see cref="SyntaxList"/> from the contents of <paramref name="builder"/>.
    /// </summary>
    public static SyntaxNode? ToListNode(
        ref readonly this PooledArrayBuilder<SyntaxToken> builder, SyntaxNode parent)
        => builder.ToListNode(parent, parent.Position);

    /// <summary>
    ///  Produces a <see cref="SyntaxNode"/> with the given <paramref name="parent"/>
    ///  and <paramref name="position"/> for a <see cref="SyntaxList"/> from the contents
    ///  of <paramref name="builder"/>.
    /// </summary>
    public static SyntaxNode? ToListNode(
        ref readonly this PooledArrayBuilder<SyntaxToken> builder, SyntaxNode? parent, int position)
        => builder.ToGreenListNode() is GreenNode listNode
            ? listNode.CreateRed(parent, position)
            : null;

    /// <summary>
    ///  Produces a <see cref="SyntaxNode"/> for a <see cref="SyntaxList"/> from the
    ///  contents of <paramref name="builder"/> and clears it.
    /// </summary>
    ///
    /// <remarks>
    ///  <para>
    ///   Because this method mutates <paramref name="builder"/>, <c>builder.AsRef()</c> must be
    ///   called if it is is declared in a <c>using</c> statement.
    ///  </para>
    ///
    ///  <code>
    ///  using PooledArrayBuilder&lt;SyntaxNode&gt; builder = [];
    ///
    ///  // Use AsRef() inline
    ///  builder.AsRef().ToListNodeAndClear();
    ///
    ///  // Declare a local ref variable with AsRef() to avoid taking a ref multiple times.
    ///  ref PooledArrayBuilder&lt;SyntaxNode&gt; builderRef = ref builder.AsRef();
    ///  builderRef.ToListNodeAndClear();
    ///  </code>
    /// </remarks>
    public static SyntaxNode? ToListNodeAndClear<TNode>(ref this PooledArrayBuilder<TNode> builder)
        where TNode : SyntaxNode
        => builder.ToListNodeAndClear(parent: null, position: 0);

    /// <summary>
    ///  Produces a <see cref="SyntaxNode"/> with the given <paramref name="parent"/>
    ///  for a <see cref="SyntaxList"/> from the contents of <paramref name="builder"/>
    ///  and clears it.
    /// </summary>
    ///
    /// <remarks>
    ///  <para>
    ///   Because this method mutates <paramref name="builder"/>, <c>builder.AsRef()</c> must be
    ///   called if it is is declared in a <c>using</c> statement.
    ///  </para>
    ///
    ///  <code>
    ///  using PooledArrayBuilder&lt;SyntaxNode&gt; builder = [];
    ///
    ///  // Use AsRef() inline
    ///  builder.AsRef().ToListNodeAndClear(parent);
    ///
    ///  // Declare a local ref variable with AsRef() to avoid taking a ref multiple times.
    ///  ref PooledArrayBuilder&lt;SyntaxNode&gt; builderRef = ref builder.AsRef();
    ///  builderRef.ToListNodeAndClear(parent);
    ///  </code>
    /// </remarks>
    public static SyntaxNode? ToListNodeAndClear<TNode>(
        ref this PooledArrayBuilder<TNode> builder, SyntaxNode parent)
        where TNode : SyntaxNode
    {
        var result = builder.ToListNode(parent, parent.Position);
        builder.Clear();

        return result;
    }

    /// <summary>
    ///  Produces a <see cref="SyntaxNode"/> with the given <paramref name="parent"/>
    ///  and <paramref name="position"/> for a <see cref="SyntaxList"/> from the contents
    ///  of <paramref name="builder"/> and clears it.
    /// </summary>
    ///
    /// <remarks>
    ///  <para>
    ///   Because this method mutates <paramref name="builder"/>, <c>builder.AsRef()</c> must be
    ///   called if it is is declared in a <c>using</c> statement.
    ///  </para>
    ///
    ///  <code>
    ///  using PooledArrayBuilder&lt;SyntaxNode&gt; builder = [];
    ///
    ///  // Use AsRef() inline
    ///  builder.AsRef().ToListNodeAndClear(parent, position);
    ///
    ///  // Declare a local ref variable with AsRef() to avoid taking a ref multiple times.
    ///  ref PooledArrayBuilder&lt;SyntaxNode&gt; builderRef = ref builder.AsRef();
    ///  builderRef.ToListNodeAndClear(parent, position);
    ///  </code>
    /// </remarks>
    public static SyntaxNode? ToListNodeAndClear<TNode>(
        ref this PooledArrayBuilder<TNode> builder, SyntaxNode? parent, int position)
        where TNode : SyntaxNode
    {
        var result = builder.ToListNode(parent, position);
        builder.Clear();

        return result;
    }

    /// <summary>
    ///  Produces a <see cref="SyntaxNode"/> for a <see cref="SyntaxList"/> from the
    ///  contents of <paramref name="builder"/> and clears it.
    /// </summary>
    ///
    /// <remarks>
    ///  <para>
    ///   Because this method mutates <paramref name="builder"/>, <c>builder.AsRef()</c> must be
    ///   called if it is is declared in a <c>using</c> statement.
    ///  </para>
    ///
    ///  <code>
    ///  using PooledArrayBuilder&lt;SyntaxToken&gt; builder = [];
    ///
    ///  // Use AsRef() inline
    ///  builder.AsRef().ToListNodeAndClear();
    ///
    ///  // Declare a local ref variable with AsRef() to avoid taking a ref multiple times.
    ///  ref PooledArrayBuilder&lt;SyntaxToken&gt; builderRef = ref builder.AsRef();
    ///  builderRef.ToListNodeAndClear();
    ///  </code>
    /// </remarks>
    public static SyntaxNode? ToListNodeAndClear(ref this PooledArrayBuilder<SyntaxToken> builder)
        => builder.ToListNodeAndClear(parent: null, position: 0);

    /// <summary>
    ///  Produces a <see cref="SyntaxNode"/> with the given <paramref name="parent"/>
    ///  for a <see cref="SyntaxList"/> from the contents of <paramref name="builder"/>
    ///  and clears it.
    /// </summary>
    ///
    /// <remarks>
    ///  <para>
    ///   Because this method mutates <paramref name="builder"/>, <c>builder.AsRef()</c> must be
    ///   called if it is is declared in a <c>using</c> statement.
    ///  </para>
    ///
    ///  <code>
    ///  using PooledArrayBuilder&lt;SyntaxToken&gt; builder = [];
    ///
    ///  // Use AsRef() inline
    ///  builder.AsRef().ToListNodeAndClear(parent);
    ///
    ///  // Declare a local ref variable with AsRef() to avoid taking a ref multiple times.
    ///  ref PooledArrayBuilder&lt;SyntaxToken&gt; builderRef = ref builder.AsRef();
    ///  builderRef.ToListNodeAndClear(parent);
    ///  </code>
    /// </remarks>
    public static SyntaxNode? ToListNodeAndClear(
        ref this PooledArrayBuilder<SyntaxToken> builder, SyntaxNode parent)
    {
        var result = builder.ToListNode(parent, parent.Position);
        builder.Clear();

        return result;
    }

    /// <summary>
    ///  Produces a <see cref="SyntaxNode"/> with the given <paramref name="parent"/>
    ///  and <paramref name="position"/> for a <see cref="SyntaxList"/> from the contents
    ///  of <paramref name="builder"/> and clears it.
    /// </summary>
    ///
    /// <remarks>
    ///  <para>
    ///   Because this method mutates <paramref name="builder"/>, <c>builder.AsRef()</c> must be
    ///   called if it is is declared in a <c>using</c> statement.
    ///  </para>
    ///
    ///  <code>
    ///  using PooledArrayBuilder&lt;SyntaxToken&gt; builder = [];
    ///
    ///  // Use AsRef() inline
    ///  builder.AsRef().ToListNodeAndClear(parent, position);
    ///
    ///  // Declare a local ref variable with AsRef() to avoid taking a ref multiple times.
    ///  ref PooledArrayBuilder&lt;SyntaxToken&gt; builderRef = ref builder.AsRef();
    ///  builderRef.ToListNodeAndClear(parent, position);
    ///  </code>
    /// </remarks>
    public static SyntaxNode? ToListNodeAndClear(
        ref this PooledArrayBuilder<SyntaxToken> builder, SyntaxNode? parent, int position)
    {
        var result = builder.ToListNode(parent, position);
        builder.Clear();

        return result;
    }

    /// <summary>
    ///  Produces a <see cref="SyntaxList{TNode}"/> from the contents of <paramref name="builder"/>.
    /// </summary>
    public static SyntaxList<TNode> ToList<TNode>(ref readonly this PooledArrayBuilder<TNode> builder)
        where TNode : SyntaxNode
        => builder.ToList(parent: null, position: 0);

    /// <summary>
    ///  Produces a <see cref="SyntaxList{TNode}"/> with the given <paramref name="parent"/>
    ///  from the contents of <paramref name="builder"/>.
    /// </summary>
    public static SyntaxList<TNode> ToList<TNode>(
        ref readonly this PooledArrayBuilder<TNode> builder, SyntaxNode parent)
        where TNode : SyntaxNode
        => builder.ToList(parent, parent.Position);

    /// <summary>
    ///  Produces a <see cref="SyntaxList{TNode}"/> with the given <paramref name="parent"/>
    ///  and <paramref name="position"/> from the contents of <paramref name="builder"/>.
    /// </summary>
    public static SyntaxList<TNode> ToList<TNode>(
        ref readonly this PooledArrayBuilder<TNode> builder, SyntaxNode? parent, int position)
        where TNode : SyntaxNode
        => builder.ToGreenListNode() is GreenNode listNode
            ? new(listNode.CreateRed(parent, position))
            : default;

    /// <summary>
    ///  Produces a <see cref="SyntaxTokenList"/> from the contents of <paramref name="builder"/>.
    /// </summary>
    public static SyntaxTokenList ToList(ref readonly this PooledArrayBuilder<SyntaxToken> builder)
        => builder.ToList(parent: null, position: 0, index: 0);

    /// <summary>
    ///  Produces a <see cref="SyntaxTokenList"/> with the given <paramref name="parent"/>
    ///  from the contents of <paramref name="builder"/>.
    /// </summary>
    public static SyntaxTokenList ToList(
        ref readonly this PooledArrayBuilder<SyntaxToken> builder, SyntaxNode parent)
        => builder.ToList(parent, parent.Position, index: 0);

    /// <summary>
    ///  Produces a <see cref="SyntaxTokenList"/> with the given <paramref name="parent"/>
    ///  and <paramref name="position"/> from the contents of <paramref name="builder"/>.
    /// </summary>
    public static SyntaxTokenList ToList(
        ref readonly this PooledArrayBuilder<SyntaxToken> builder, SyntaxNode? parent, int position, int index)
        => builder.ToGreenListNode() is GreenNode listNode
            ? new(parent, listNode, position, index)
            : default;

    /// <summary>
    ///  Produces a <see cref="SyntaxList{TNode}"/> from the contents of <paramref name="builder"/> and clears it.
    /// </summary>
    ///
    /// <remarks>
    ///  <para>
    ///   Because this method mutates <paramref name="builder"/>, <c>builder.AsRef()</c> must be
    ///   called if it is is declared in a <c>using</c> statement.
    ///  </para>
    ///
    ///  <code>
    ///  using PooledArrayBuilder&lt;SyntaxNode&gt; builder = [];
    ///
    ///  // Use AsRef() inline
    ///  builder.AsRef().ToListAndClear();
    ///
    ///  // Declare a local ref variable with AsRef() to avoid taking a ref multiple times.
    ///  ref PooledArrayBuilder&lt;SyntaxNode&gt; builderRef = ref builder.AsRef();
    ///  builderRef.ToListAndClear();
    ///  </code>
    /// </remarks>
    public static SyntaxList<TNode> ToListAndClear<TNode>(ref this PooledArrayBuilder<TNode> builder)
        where TNode : SyntaxNode
        => builder.ToListAndClear(parent: null, position: 0);

    /// <summary>
    ///  Produces a <see cref="SyntaxList{TNode}"/> with the given <paramref name="parent"/>
    ///  from the contents of <paramref name="builder"/> and clears it.
    /// </summary>
    ///
    /// <remarks>
    ///  <para>
    ///   Because this method mutates <paramref name="builder"/>, <c>builder.AsRef()</c> must be
    ///   called if it is is declared in a <c>using</c> statement.
    ///  </para>
    ///
    ///  <code>
    ///  using PooledArrayBuilder&lt;SyntaxNode&gt; builder = [];
    ///
    ///  // Use AsRef() inline
    ///  builder.AsRef().ToListAndClear();
    ///
    ///  // Declare a local ref variable with AsRef() to avoid taking a ref multiple times.
    ///  ref PooledArrayBuilder&lt;SyntaxNode&gt; builderRef = ref builder.AsRef();
    ///  builderRef.ToListAndClear();
    ///  </code>
    /// </remarks>
    public static SyntaxList<TNode> ToListAndClear<TNode>(
        ref this PooledArrayBuilder<TNode> builder, SyntaxNode parent)
        where TNode : SyntaxNode
        => builder.ToListAndClear(parent, parent.Position);

    /// <summary>
    ///  Produces a <see cref="SyntaxList{TNode}"/> with the given <paramref name="parent"/>
    ///  and <paramref name="position"/> from the contents of <paramref name="builder"/> and clears it.
    /// </summary>
    ///
    /// <remarks>
    ///  <para>
    ///   Because this method mutates <paramref name="builder"/>, <c>builder.AsRef()</c> must be
    ///   called if it is is declared in a <c>using</c> statement.
    ///  </para>
    ///
    ///  <code>
    ///  using PooledArrayBuilder&lt;SyntaxNode&gt; builder = [];
    ///
    ///  // Use AsRef() inline
    ///  builder.AsRef().ToListAndClear(parent, position);
    ///
    ///  // Declare a local ref variable with AsRef() to avoid taking a ref multiple times.
    ///  ref PooledArrayBuilder&lt;SyntaxNode&gt; builderRef = ref builder.AsRef();
    ///  builderRef.ToListAndClear(parent, position);
    ///  </code>
    /// </remarks>
    public static SyntaxList<TNode> ToListAndClear<TNode>(
        ref this PooledArrayBuilder<TNode> builder, SyntaxNode? parent, int position)
        where TNode : SyntaxNode
    {
        var result = builder.ToList(parent, position);
        builder.Clear();

        return result;
    }

    /// <summary>
    ///  Produces a <see cref="SyntaxTokenList"/> from the contents of <paramref name="builder"/> and clears it.
    /// </summary>
    ///
    /// <remarks>
    ///  <para>
    ///   Because this method mutates <paramref name="builder"/>, <c>builder.AsRef()</c> must be
    ///   called if it is is declared in a <c>using</c> statement.
    ///  </para>
    ///
    ///  <code>
    ///  using PooledArrayBuilder&lt;SyntaxToken&gt; builder = [];
    ///
    ///  // Use AsRef() inline
    ///  builder.AsRef().ToListAndClear();
    ///
    ///  // Declare a local ref variable with AsRef() to avoid taking a ref multiple times.
    ///  ref PooledArrayBuilder&lt;SyntaxToken&gt; builderRef = ref builder.AsRef();
    ///  builderRef.ToListAndClear();
    ///  </code>
    /// </remarks>
    public static SyntaxTokenList ToListAndClear(ref this PooledArrayBuilder<SyntaxToken> builder)
        => builder.ToListAndClear(parent: null, position: 0, index: 0);

    /// <summary>
    ///  Produces a <see cref="SyntaxTokenList"/> with the given <paramref name="parent"/>
    ///  from the contents of <paramref name="builder"/> and clears it.
    /// </summary>
    ///
    /// <remarks>
    ///  <para>
    ///   Because this method mutates <paramref name="builder"/>, <c>builder.AsRef()</c> must be
    ///   called if it is is declared in a <c>using</c> statement.
    ///  </para>
    ///
    ///  <code>
    ///  using PooledArrayBuilder&lt;SyntaxToken&gt; builder = [];
    ///
    ///  // Use AsRef() inline
    ///  builder.AsRef().ToListAndClear();
    ///
    ///  // Declare a local ref variable with AsRef() to avoid taking a ref multiple times.
    ///  ref PooledArrayBuilder&lt;SyntaxToken&gt; builderRef = ref builder.AsRef();
    ///  builderRef.ToListAndClear();
    ///  </code>
    /// </remarks>
    public static SyntaxTokenList ToListAndClear(
        ref this PooledArrayBuilder<SyntaxToken> builder, SyntaxNode parent)
        => builder.ToListAndClear(parent, parent.Position, index: 0);

    /// <summary>
    ///  Produces a <see cref="SyntaxTokenList"/> with the given <paramref name="parent"/>
    ///  and <paramref name="position"/> from the contents of <paramref name="builder"/> and clears it.
    /// </summary>
    ///
    /// <remarks>
    ///  <para>
    ///   Because this method mutates <paramref name="builder"/>, <c>builder.AsRef()</c> must be
    ///   called if it is is declared in a <c>using</c> statement.
    ///  </para>
    ///
    ///  <code>
    ///  using PooledArrayBuilder&lt;SyntaxToken&gt; builder = [];
    ///
    ///  // Use AsRef() inline
    ///  builder.AsRef().ToListAndClear(parent, position);
    ///
    ///  // Declare a local ref variable with AsRef() to avoid taking a ref multiple times.
    ///  ref PooledArrayBuilder&lt;SyntaxToken&gt; builderRef = ref builder.AsRef();
    ///  builderRef.ToListAndClear(parent, position);
    ///  </code>
    /// </remarks>
    public static SyntaxTokenList ToListAndClear(
        ref this PooledArrayBuilder<SyntaxToken> builder, SyntaxNode? parent, int position, int index)
    {
        var result = builder.ToList(parent, position, index);
        builder.Clear();

        return result;
    }
}
