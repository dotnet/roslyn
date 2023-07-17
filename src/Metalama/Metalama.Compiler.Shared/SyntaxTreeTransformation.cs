// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;

namespace Metalama.Compiler;

public enum SyntaxTreeTransformationKind
{
    None,
    Add,
    Remove,
    Replace
}

public readonly struct SyntaxTreeTransformation
{
    private SyntaxTreeTransformation(SyntaxTree? newTree, SyntaxTree? oldTree)
    {
        OldTree = oldTree;
        NewTree = newTree;
    }

    public static SyntaxTreeTransformation AddTree(SyntaxTree tree)
    {
        if (tree == null)
        {
            throw new ArgumentNullException(nameof(tree));
        }

        return new SyntaxTreeTransformation(tree, null);
    }

    public static SyntaxTreeTransformation RemoveTree(SyntaxTree tree)
    {
        if (tree == null)
        {
            throw new ArgumentNullException(nameof(tree));
        }

        return new SyntaxTreeTransformation(null, tree);
    }

    public static SyntaxTreeTransformation ReplaceTree(SyntaxTree oldTree, SyntaxTree newTree)
    {
        if (oldTree == null)
        {
            throw new ArgumentNullException(nameof(oldTree));
        }

        if (newTree == null)
        {
            throw new ArgumentNullException(nameof(newTree));
        }

        if (oldTree.FilePath != newTree.FilePath)
        {
            throw new ArgumentOutOfRangeException();
        }

        return new SyntaxTreeTransformation(newTree, oldTree);
    }

    public SyntaxTree? OldTree { get; }
    public SyntaxTree? NewTree { get; }

    public string FilePath =>
        OldTree?.FilePath ?? NewTree?.FilePath ?? throw new InvalidOperationException();

    public SyntaxTreeTransformationKind Kind => (OldTree, NewTree) switch
    {
        (null, null) => SyntaxTreeTransformationKind.None,
        (not null, null) => SyntaxTreeTransformationKind.Remove,
        (null, not null) => SyntaxTreeTransformationKind.Add,
        _ => SyntaxTreeTransformationKind.Replace
    };
}
