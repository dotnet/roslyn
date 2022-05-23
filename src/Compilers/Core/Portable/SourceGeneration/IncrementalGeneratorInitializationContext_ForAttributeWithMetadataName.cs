// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.SourceGeneration;
using System.Threading;

namespace Microsoft.CodeAnalysis;

using Aliases = ArrayBuilder<(string aliasName, string symbolName)>;

public readonly struct GeneratorAttributeSyntaxContext<TSyntaxNode>
    where TSyntaxNode : SyntaxNode
{
    public TSyntaxNode Node { get; }
    public SemanticModel SemanticModel { get; }
    public AttributeData AttributeData { get; }

    internal GeneratorAttributeSyntaxContext(TSyntaxNode node, SemanticModel semanticModel, AttributeData attributeData)
    {
        Node = node;
        SemanticModel = semanticModel;
        AttributeData = attributeData;
    }
}

public partial struct IncrementalGeneratorInitializationContext
{
    private static readonly char[] s_nestedTypeNameSeparators = new char[] { '+' };
    private static readonly SymbolDisplayFormat s_metadataDisplayFormat =
        SymbolDisplayFormat.QualifiedNameArityFormat.AddCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.UsePlusForNestedTypes);

#pragma warning disable CA1200 // Avoid using cref tags with a prefix
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
    /// cref="T:Microsoft.CodeAnalysis.CSharp.Syntax.AttributeListSyntax"/> that contains the matching attribute.  For
    /// the example above, that would be a <see cref="T:Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax"/>.
    /// <see cref="SyntaxNode"/> can be used as the type argument to return every syntax node of any type that has such
    /// a matching attribute on it.
    /// </remarks>
    public IncrementalValuesProvider<T> ForAttributeWithMetadataName<T>(string fullyQualifiedMetadataName)
        where T : SyntaxNode
#pragma warning restore CA1200 // Avoid using cref tags with a prefix
    {
        return ForAttributeWithMetadataName<T, T>(
            fullyQualifiedMetadataName,
            (context, attributeData, cancellationToken) => context.Node);
    }

    /// <summary>
    /// Creates an <see cref="IncrementalValuesProvider{T}"/> that can provide a transform over all <typeparamref
    /// name="TSyntaxNode"/>s if that node has an attribute on it that binds to a <see cref="INamedTypeSymbol"/> with
    /// the same fully-qualified metadata as the provided <paramref name="fullyQualifiedMetadataName"/>. <paramref
    /// name="fullyQualifiedMetadataName"/> should be the fully-qualified, metadata name of the attribute, including the
    /// <c>Attribute</c> suffix.  For example <c>System.CLSCompliantAttribute</c> for <see
    /// cref="System.CLSCompliantAttribute"/>.
    /// </summary>
    public IncrementalValuesProvider<TResult> ForAttributeWithMetadataName<TSyntaxNode, TResult>(
        string fullyQualifiedMetadataName,
        Func<GeneratorAttributeSyntaxContext<TSyntaxNode>, AttributeData, CancellationToken, TResult> transform)
        where TSyntaxNode : SyntaxNode
    {
        var metadataName = fullyQualifiedMetadataName.Contains('+')
            ? MetadataTypeName.FromFullName(fullyQualifiedMetadataName.Split(s_nestedTypeNameSeparators).Last())
            : MetadataTypeName.FromFullName(fullyQualifiedMetadataName);

        var nodesWithAttributesMatchingSimpleName = this.ForAttributeWithSimpleName<TSyntaxNode>(metadataName.UnmangledTypeName);

        var collectedNodes = nodesWithAttributesMatchingSimpleName
            .Collect()
            .WithComparer(ImmutableArrayValueComparer<TSyntaxNode>.Instance)
            .WithTrackingName("collectedNodes_ForAttributeWithMetadataName");

        // Group all the nodes by syntax tree, so we can process a whole syntax tree at a time.  This will let us make
        // the required semantic model for it once, instead of potentially many times (in the rare, but possible case of
        // a single file with a ton of matching nodes in it).
        var groupedNodes = collectedNodes.SelectMany(
            static (array, cancellationToken) =>
                array.GroupBy(static n => n.SyntaxTree)
                     .Select(static g => new SyntaxNodeGrouping<TSyntaxNode>(g))).WithTrackingName("groupedNodes_ForAttributeWithMetadataName");

        var compilationAndGroupedNodesProvider = groupedNodes
            .Combine(this.CompilationProvider)
            .WithTrackingName("compilationAndGroupedNodes_ForAttributeWithMetadataName");

        return compilationAndGroupedNodesProvider.SelectMany((tuple, cancellationToken) =>
        {
            var (grouping, compilation) = tuple;

            var result = ArrayBuilder<TResult>.GetInstance();
            try
            {
                var syntaxTree = grouping.SyntaxTree;
                var semanticModel = compilation.GetSemanticModel(syntaxTree);

                foreach (var node in grouping.SyntaxNodes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
                    if (HasMatchingAttribute(symbol, fullyQualifiedMetadataName, out var attributeData))
                    {
                        result.Add(transform(
                            new GeneratorAttributeSyntaxContext<TSyntaxNode>(node, semanticModel, attributeData),
                            attributeData,
                            cancellationToken));
                    }
                }

                return result.ToImmutable();
            }
            finally
            {
                result.Free();
            }
        }).WithTrackingName("result_ForAttributeWithMetadataName");
    }

    private static bool HasMatchingAttribute(
        ISymbol? symbol,
        string fullyQualifiedMetadataName,
        [NotNullWhen(true)] out AttributeData? attributeData)
    {
        if (symbol is not null)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass is null)
                    continue;

                if (attribute.AttributeClass.ToDisplayString(s_metadataDisplayFormat) == fullyQualifiedMetadataName)
                {
                    attributeData = attribute;
                    return true;
                }
            }
        }

        attributeData = null;
        return false;
    }
}
