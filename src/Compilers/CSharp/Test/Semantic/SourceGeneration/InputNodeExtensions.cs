// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration;

internal static class InputNodeExtensions
{
    public static InputNode<T> WithDefaultContext<T>(this InputNode<T> inputNode)
        => inputNode.WithContext(new TransformFactory(), AddIfMissing);

    public static InputNode<T1> WithContextFrom<T1, T2>(this InputNode<T1> inputNode, InputNode<T2> other)
        => inputNode.WithContext(other.TransformFactory, other.RegisterOutput);

    private static void AddIfMissing(ArrayBuilder<IIncrementalGeneratorOutputNode> builder, IIncrementalGeneratorOutputNode node)
    {
        if (!builder.Contains(node))
            builder.Add(node);
    }
}
