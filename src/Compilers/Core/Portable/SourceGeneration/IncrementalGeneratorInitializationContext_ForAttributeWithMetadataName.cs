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

internal readonly struct GeneratorAttributeSyntaxContext
{
    /// <summary>
    /// The syntax node the attribute is attached to.  For example, with <c>[CLSCompliant] class C { }</c> this would
    /// the class declaration node.
    /// </summary>
    public SyntaxNode AttributeTarget { get; }

    /// <summary>
    /// Semantic model for the file that <see cref="AttributeTarget"/> is contained within.
    /// </summary>
    public SemanticModel SemanticModel { get; }

    /// <summary>
    /// <see cref="AttributeData"/>s for any matching attributes on <see cref="AttributeTarget"/>.  Always non-empty.
    /// </summary>
    public ImmutableArray<AttributeData> Attributes { get; }

    internal GeneratorAttributeSyntaxContext(
        SyntaxNode attribueTarget,
        SemanticModel semanticModel,
        ImmutableArray<AttributeData> attributes)
    {
        AttributeTarget = attribueTarget;
        SemanticModel = semanticModel;
        Attributes = attributes;
    }
}

public partial struct IncrementalGeneratorInitializationContext
{
    private static readonly char[] s_nestedTypeNameSeparators = new char[] { '+' };
    private static readonly SymbolDisplayFormat s_metadataDisplayFormat =
        SymbolDisplayFormat.QualifiedNameArityFormat.AddCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.UsePlusForNestedTypes);

    /// <summary>
    /// Creates an <see cref="IncrementalValuesProvider{T}"/> that can provide a transform over all <see
    /// cref="SyntaxNode"/>s if that node has an attribute on it that binds to a <see cref="INamedTypeSymbol"/> with the
    /// same fully-qualified metadata as the provided <paramref name="fullyQualifiedMetadataName"/>. <paramref
    /// name="fullyQualifiedMetadataName"/> should be the fully-qualified, metadata name of the attribute, including the
    /// <c>Attribute</c> suffix.  For example <c>"System.CLSCompliantAttribute</c> for <see
    /// cref="System.CLSCompliantAttribute"/>.
    /// </summary>
    /// <param name="predicate">A function that determines if the given <see cref="SyntaxNode"/> attribute target (<see
    /// cref="GeneratorAttributeSyntaxContext.AttributeTarget"/>) should be transformed.  Nodes that do not pass this
    /// predicate will not have their attributes looked at at all.</param>
    /// <param name="transform">A function that performs the transform. This will only be passed nodes that return <see
    /// langword="true"/> for <paramref name="predicate"/> and which have a matchin <see cref="AttributeData"/> whose
    /// <see cref="AttributeData.AttributeClass"/> has the same fully qualified, metadata name as <paramref
    /// name="fullyQualifiedMetadataName"/>.</param>
    internal IncrementalValuesProvider<T> ForAttributeWithMetadataName<T>(
        string fullyQualifiedMetadataName,
        Func<SyntaxNode, CancellationToken, bool> predicate,
        Func<GeneratorAttributeSyntaxContext, CancellationToken, T> transform)
    {
        var metadataName = fullyQualifiedMetadataName.Contains('+')
            ? MetadataTypeName.FromFullName(fullyQualifiedMetadataName.Split(s_nestedTypeNameSeparators).Last())
            : MetadataTypeName.FromFullName(fullyQualifiedMetadataName);

        var nodesWithAttributesMatchingSimpleName = this.ForAttributeWithSimpleName(metadataName.UnmangledTypeName, predicate);

        var collectedNodes = nodesWithAttributesMatchingSimpleName
            .Collect()
            .WithComparer(ImmutableArrayValueComparer<SyntaxNode>.Instance)
            .WithTrackingName("collectedNodes_ForAttributeWithMetadataName");

        // Group all the nodes by syntax tree, so we can process a whole syntax tree at a time.  This will let us make
        // the required semantic model for it once, instead of potentially many times (in the rare, but possible case of
        // a single file with a ton of matching nodes in it).
        var groupedNodes = collectedNodes.SelectMany(
            static (array, cancellationToken) =>
                array.GroupBy(static n => n.SyntaxTree)
                     .Select(static g => new SyntaxNodeGrouping<SyntaxNode>(g))).WithTrackingName("groupedNodes_ForAttributeWithMetadataName");

        var compilationAndGroupedNodesProvider = groupedNodes
            .Combine(this.CompilationProvider)
            .WithTrackingName("compilationAndGroupedNodes_ForAttributeWithMetadataName");

        var syntaxHelper = this.SyntaxHelper;
        var finalProvider = compilationAndGroupedNodesProvider.SelectMany((tuple, cancellationToken) =>
        {
            var (grouping, compilation) = tuple;

            var result = ArrayBuilder<T>.GetInstance();
            try
            {
                var syntaxTree = grouping.SyntaxTree;
                var semanticModel = compilation.GetSemanticModel(syntaxTree);

                foreach (var attributeTarget in grouping.SyntaxNodes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var symbol =
                        attributeTarget is ICompilationUnitSyntax compilationUnit ? semanticModel.Compilation.Assembly :
                        syntaxHelper.IsLambdaExpression(attributeTarget) ? semanticModel.GetSymbolInfo(attributeTarget, cancellationToken).Symbol :
                        semanticModel.GetDeclaredSymbol(attributeTarget, cancellationToken);

                    var attributes = getMatchingAttributes(attributeTarget, symbol, fullyQualifiedMetadataName);
                    if (attributes.Length > 0)
                    {
                        result.Add(transform(
                            new GeneratorAttributeSyntaxContext(attributeTarget, semanticModel, attributes),
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

        return finalProvider;

        static ImmutableArray<AttributeData> getMatchingAttributes(
            SyntaxNode attributeTarget,
            ISymbol? symbol,
            string fullyQualifiedMetadataName)
        {
            var targetSyntaxTree = attributeTarget.SyntaxTree;
            var result = ArrayBuilder<AttributeData>.GetInstance();

            addMatchingAttributes(symbol?.GetAttributes());
            addMatchingAttributes((symbol as IMethodSymbol)?.GetReturnTypeAttributes());

            return result.ToImmutableAndFree();

            void addMatchingAttributes(ImmutableArray<AttributeData>? attributes)
            {
                if (!attributes.HasValue)
                    return;

                foreach (var attribute in attributes.Value)
                {
                    if (attribute.ApplicationSyntaxReference?.SyntaxTree == targetSyntaxTree &&
                        attribute.AttributeClass?.ToDisplayString(s_metadataDisplayFormat) == fullyQualifiedMetadataName)
                    {
                        result.Add(attribute);
                    }
                }
            }
        }
    }
}
