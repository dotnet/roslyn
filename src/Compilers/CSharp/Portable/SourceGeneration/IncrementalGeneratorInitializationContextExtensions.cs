// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration;

internal static partial class IncrementalGeneratorInitializationContextExtensions
{
    private static readonly char[] s_nestedTypeNameSeparators = new char[] { '+' };

    /// <summary>
    /// Returns all syntax nodes of type <typeparamref name="T"/> if that node has an attribute on it that binds to a
    /// <see cref="INamedTypeSymbol"/> with the same fully-qualified metadata as the provided <paramref
    /// name="fullyQualifiedMetadataName"/>. <paramref name="fullyQualifiedMetadataName"/> should be the
    /// fully-qualified, metadata name of the attribute, including the <c>Attribute</c> suffix.  For example
    /// <c>System.CLSCompliantAttribute</c> for <see cref="System.CLSCompliantAttribute"/>.
    /// <para>This provider understands <see langword="using"/> aliases and will find matches even when the attribute
    /// references an alias name.  For example, given:
    /// <code>
    /// using XAttribute = System.CLSCompliantAttribute;
    /// [X]
    /// class C { }
    /// </code>
    /// Then
    /// <c>context.SyntaxProvider.CreateSyntaxProviderForAttribute&lt;ClassDeclarationSyntax&gt;(typeof(CLSCompliantAttribute).FullName)</c>
    /// will find the <c>C</c> class.</para>
    /// </summary>
    public static IncrementalValuesProvider<T> ForAttributeWithMetadataName<T>(this IncrementalGeneratorInitializationContext context, string fullyQualifiedMetadataName)
        where T : SyntaxNode
    {
        var metadataName = fullyQualifiedMetadataName.IndexOf('+') >= 0
            ? MetadataTypeName.FromFullName(fullyQualifiedMetadataName.Split(s_nestedTypeNameSeparators).Last())
            : MetadataTypeName.FromFullName(fullyQualifiedMetadataName);

        var simpleTypeName = metadataName.UnmangledTypeName;
        var nodesWithAttributesMatchingSimpleName = context.SyntaxProvider.CreateSyntaxProviderForAttribute<T>(simpleTypeName);

        var collectedNodes = nodesWithAttributesMatchingSimpleName.Collect().WithTrackingName("collectedNodes_ForAttributeWithMetadataName");
        var groupedNodes = collectedNodes.SelectMany(
            (array, cancellationToken) => array.GroupBy(n => n.SyntaxTree).Select(g => new SyntaxNodeGrouping<T>(g))).WithTrackingName("groupedNodes_ForAttributeWithMetadataName");

        var compilationAndGroupedNodesProvider = groupedNodes
            .Combine(context.CompilationProvider)
            .WithTrackingName("compilationAndGroupedNodes_ForAttributeWithMetadataName");

        return compilationAndGroupedNodesProvider.SelectMany((tuple, cancellationToken) =>
        {
            var (grouping, compilation) = tuple;

            var result = ArrayBuilder<T>.GetInstance();
            try
            {
                var syntaxTree = grouping.SyntaxTree;
                var semanticModel = compilation.GetSemanticModel(syntaxTree);

                foreach (var node in grouping.SyntaxNodes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
                    if (HasMatchingAttribute(symbol, fullyQualifiedMetadataName))
                        result.Add(node);
                }

                return result.ToImmutable();
            }
            finally
            {
                result.Free();
            }
        }).WithTrackingName("result_ForAttributeWithMetadataName");
    }

    private static bool HasMatchingAttribute(ISymbol? symbol, string fullyQualifiedMetadataName)
    {
        if (symbol is not null)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass is null)
                    continue;

                if (attribute.AttributeClass.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat) == fullyQualifiedMetadataName)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Wraps a grouping of nodes within a syntax tree so we can have value-semantics around them usable by the
    /// incremental driver.  Note: we do something very sneaky here.  Specifically, as long as we have the same <see
    /// cref="SyntaxTree"/> from before, then we know we must have the same nodes as before (since the nodes are
    /// entirely determined from the text+options which is exactly what the syntax tree represents).  Similarly, if the
    /// syntax tree changes, we will always get different nodes (since they point back at the syntax tree).  So we can
    /// just use the syntax tree itself to determine value semantics here.
    /// </summary>
    private class SyntaxNodeGrouping<TSyntaxNode> : IEquatable<SyntaxNodeGrouping<TSyntaxNode>>
        where TSyntaxNode : SyntaxNode
    {
        public readonly SyntaxTree SyntaxTree;
        public readonly ImmutableArray<TSyntaxNode> SyntaxNodes;

        public SyntaxNodeGrouping(IGrouping<SyntaxTree, TSyntaxNode> grouping)
        {
            SyntaxTree = grouping.Key;
            SyntaxNodes = grouping.OrderBy(n => n.FullSpan.Start).ToImmutableArray();
        }

        public override int GetHashCode()
            => SyntaxTree.GetHashCode();

        public override bool Equals(object? obj)
            => Equals(obj as SyntaxNodeGrouping<TSyntaxNode>);

        public bool Equals(SyntaxNodeGrouping<TSyntaxNode>? obj)
            => this.SyntaxTree == obj?.SyntaxTree;
    }
}
