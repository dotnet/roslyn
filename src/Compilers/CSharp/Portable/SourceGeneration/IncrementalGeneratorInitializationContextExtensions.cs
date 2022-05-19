// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if false
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration;

internal static partial class IncrementalGeneratorInitializationContextExtensions
{
    private static readonly char[] s_nestedTypeNameSeparators = new char[] { '+' };
    private static readonly SymbolDisplayFormat s_metadataDisplayFormat =
        SymbolDisplayFormat.QualifiedNameArityFormat.AddCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.UsePlusForNestedTypes);

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
    /// <remarks>
    /// The <typeparamref name="T"/> should be given the type of the syntax node that owns the <see
    /// cref="AttributeListSyntax"/> that contains the matching attribute.  For the example above, that would be a <see
    /// cref="ClassDeclarationSyntax"/>.  <see cref="SyntaxNode"/> can be used as the type argument to return every
    /// syntax node of any type that has such a matching attribute on it.
    /// </remarks>
    public static IncrementalValuesProvider<T> ForAttributeWithMetadataName<T>(this IncrementalGeneratorInitializationContext context, string fullyQualifiedMetadataName)
        where T : SyntaxNode
    {
        var metadataName = fullyQualifiedMetadataName.Contains('+')
            ? MetadataTypeName.FromFullName(fullyQualifiedMetadataName.Split(s_nestedTypeNameSeparators).Last())
            : MetadataTypeName.FromFullName(fullyQualifiedMetadataName);

        var simpleTypeName = metadataName.UnmangledTypeName;
        var nodesWithAttributesMatchingSimpleName = context.SyntaxProvider.CreateSyntaxProviderForAttribute<T>(simpleTypeName);

        var collectedNodes = nodesWithAttributesMatchingSimpleName
            .Collect()
            .WithComparer(SyntaxNodeArrayComparer<T>.Instance)
            .WithTrackingName("collectedNodes_ForAttributeWithMetadataName");

        // Group all the nodes by syntax tree, so we can process a whole syntax tree at a time.  This will let us make
        // the required semantic model for it once, instead of potentially many times (in the rare, but possible case of
        // a single file with a ton of matching nodes in it).
        var groupedNodes = collectedNodes.SelectMany(
            static (array, cancellationToken) =>
                array.GroupBy(static n => n.SyntaxTree)
                     .Select(static g => new SyntaxNodeGrouping<T>(g))).WithTrackingName("groupedNodes_ForAttributeWithMetadataName");

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

                if (attribute.AttributeClass.ToDisplayString(s_metadataDisplayFormat) == fullyQualifiedMetadataName)
                    return true;
            }
        }

        return false;
    }

    private class SyntaxNodeArrayComparer<TSyntaxNode> : IEqualityComparer<ImmutableArray<TSyntaxNode>>
        where TSyntaxNode : SyntaxNode
    {
        public static readonly IEqualityComparer<ImmutableArray<TSyntaxNode>> Instance = new SyntaxNodeArrayComparer<TSyntaxNode>();

        public bool Equals([AllowNull] ImmutableArray<TSyntaxNode> x, [AllowNull] ImmutableArray<TSyntaxNode> y)
        {
            if (x == y)
                return true;

            if (x.Length != y.Length)
                return false;

            for (int i = 0, n = x.Length; i < n; i++)
            {
                if (x[i] != y[i])
                    return false;
            }

            return true;
        }

        public int GetHashCode([DisallowNull] ImmutableArray<TSyntaxNode> obj)
        {
            var hashCode = 0;
            foreach (var node in obj)
                hashCode = Hash.Combine(hashCode, node.GetHashCode());

            return hashCode;
        }
    }
}
#endif
