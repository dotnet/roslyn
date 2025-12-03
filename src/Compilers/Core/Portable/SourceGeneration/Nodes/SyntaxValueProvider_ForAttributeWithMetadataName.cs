// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

public readonly struct GeneratorAttributeSyntaxContext
{
    /// <summary>
    /// The syntax node the attribute is attached to.  For example, with <c>[CLSCompliant] class C { }</c> this would
    /// the class declaration node.
    /// </summary>
    public SyntaxNode TargetNode { get; }

    /// <summary>
    /// The symbol that the attribute is attached to.  For example, with <c>[CLSCompliant] class C { }</c> this would be
    /// the <see cref="INamedTypeSymbol"/> for <c>"C"</c>.
    /// </summary>
    public ISymbol TargetSymbol { get; }

    /// <summary>
    /// Semantic model for the file that <see cref="TargetNode"/> is contained within.
    /// </summary>
    public SemanticModel SemanticModel { get; }

    /// <summary>
    /// <see cref="AttributeData"/>s for any matching attributes on <see cref="TargetSymbol"/>.  Always non-empty.  All
    /// these attributes will have an <see cref="AttributeData.AttributeClass"/> whose fully qualified name metadata
    /// name matches the name requested in <see cref="SyntaxValueProvider.ForAttributeWithMetadataName{T}"/>.
    /// <para>
    /// To get the entire list of attributes, use <see cref="ISymbol.GetAttributes"/> on <see cref="TargetSymbol"/>.
    /// </para>
    /// </summary>
    public ImmutableArray<AttributeData> Attributes { get; }

    internal GeneratorAttributeSyntaxContext(
        SyntaxNode targetNode,
        ISymbol targetSymbol,
        SemanticModel semanticModel,
        ImmutableArray<AttributeData> attributes)
    {
        TargetNode = targetNode;
        TargetSymbol = targetSymbol;
        SemanticModel = semanticModel;
        Attributes = attributes;
    }
}

public partial struct SyntaxValueProvider
{
    private static readonly char[] s_nestedTypeNameSeparators = new char[] { '+' };
    private static readonly SymbolDisplayFormat s_metadataDisplayFormat =
        SymbolDisplayFormat.QualifiedNameArityFormat.AddCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.UsePlusForNestedTypes);

    /// <summary>
    /// Creates an <see cref="IncrementalValuesProvider{T}"/> that can provide a transform over all <see
    /// cref="SyntaxNode"/>s if that node has an attribute on it that binds to a <see cref="INamedTypeSymbol"/> with the
    /// same fully-qualified metadata as the provided <paramref name="fullyQualifiedMetadataName"/>. <paramref
    /// name="fullyQualifiedMetadataName"/> should be the fully-qualified, metadata name of the attribute, including the
    /// <c>Attribute</c> suffix.  For example <c>"System.CLSCompliantAttribute"</c> for <see
    /// cref="System.CLSCompliantAttribute"/>.
    /// </summary>
    /// <param name="predicate">A function that determines if the given <see cref="SyntaxNode"/> attribute target (<see
    /// cref="GeneratorAttributeSyntaxContext.TargetNode"/>) should be transformed.  Nodes that do not pass this
    /// predicate will not have their attributes looked at at all.</param>
    /// <param name="transform">A function that performs the transform. This will only be passed nodes that return <see
    /// langword="true"/> for <paramref name="predicate"/> and which have a matching <see cref="AttributeData"/> whose
    /// <see cref="AttributeData.AttributeClass"/> has the same fully qualified, metadata name as <paramref
    /// name="fullyQualifiedMetadataName"/>.</param>
    /// <remarks>
    /// In the case of partial types, only the parts of the partial type that have the attribute syntactically
    /// declared on them will be returned.  If multiple parts have the same attribute declared on them, then
    /// all of those parts will be returned.
    /// </remarks>
    public IncrementalValuesProvider<T> ForAttributeWithMetadataName<T>(
        string fullyQualifiedMetadataName,
        Func<SyntaxNode, CancellationToken, bool> predicate,
        Func<GeneratorAttributeSyntaxContext, CancellationToken, T> transform)
    {
        var metadataName = fullyQualifiedMetadataName.Contains('+')
            ? MetadataTypeName.FromFullName(fullyQualifiedMetadataName.Split(s_nestedTypeNameSeparators).Last())
            : MetadataTypeName.FromFullName(fullyQualifiedMetadataName);

        var nodesWithAttributesMatchingSimpleName = this.ForAttributeWithSimpleName(metadataName.UnmangledTypeName, predicate);

        var compilationAndGroupedNodesProvider = nodesWithAttributesMatchingSimpleName
            .Combine(_context.CompilationProvider)
            .WithTrackingName("compilationAndGroupedNodes_ForAttributeWithMetadataName");

        var syntaxHelper = _context.SyntaxHelper;
        var finalProvider = compilationAndGroupedNodesProvider.SelectMany((tuple, cancellationToken) =>
        {
            var ((syntaxTree, syntaxNodes), compilation) = tuple;
            Debug.Assert(syntaxNodes.All(n => n.SyntaxTree == syntaxTree));

            var result = ArrayBuilder<T>.GetInstance();
            try
            {
                if (!syntaxNodes.IsEmpty)
                {
                    var semanticModel = compilation.GetSemanticModel(syntaxTree);

                    foreach (var targetNode in syntaxNodes)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var targetSymbol =
                            targetNode is ICompilationUnitSyntax compilationUnit ? semanticModel.Compilation.Assembly :
                            syntaxHelper.IsLambdaExpression(targetNode) ? semanticModel.GetSymbolInfo(targetNode, cancellationToken).Symbol :
                            semanticModel.GetDeclaredSymbol(targetNode, cancellationToken);
                        if (targetSymbol is null)
                            continue;

                        var attributes = getMatchingAttributes(
                            syntaxHelper, targetNode, targetSymbol, fullyQualifiedMetadataName, cancellationToken);
                        if (attributes.Length > 0)
                        {
                            result.Add(transform(
                                new GeneratorAttributeSyntaxContext(targetNode, targetSymbol, semanticModel, attributes),
                                cancellationToken));
                        }
                    }
                }

                return result.ToImmutableAndClear();
            }
            finally
            {
                result.Free();
            }
        }).WithTrackingName("result_ForAttributeWithMetadataName");

        return finalProvider;

        static ImmutableArray<AttributeData> getMatchingAttributes(
            ISyntaxHelper syntaxHelper,
            SyntaxNode attributeTarget,
            ISymbol symbol,
            string fullyQualifiedMetadataName,
            CancellationToken cancellationToken)
        {
            var targetSyntaxTree = attributeTarget.SyntaxTree;
            var result = ArrayBuilder<AttributeData>.GetInstance();

            var remappedTarget = syntaxHelper.RemapAttributeTarget(attributeTarget);

            addMatchingAttributes(symbol.GetAttributes());
            addMatchingAttributes((symbol as IMethodSymbol)?.GetReturnTypeAttributes());

            if (symbol is IAssemblySymbol assemblySymbol)
            {
                foreach (var module in assemblySymbol.Modules)
                    addMatchingAttributes(module.GetAttributes());
            }

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
                        // We're seeing all the attributes merged from all parts of a particular symbol.
                        // Ensure that we're only actually returning the attributes declared on this specific
                        // syntax node that we're currently looking at.
                        var attributeSyntax = attribute.ApplicationSyntaxReference.GetSyntax(cancellationToken);
                        var attributeOwnerSyntax = syntaxHelper.GetAttributeOwningNode(attributeSyntax);

                        if (attributeOwnerSyntax == remappedTarget)
                            result.Add(attribute);
                    }
                }
            }
        }
    }
}
