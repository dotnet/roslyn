// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class WithElementSyntaxExtensions
{
#if !ROSLYN_4_12_OR_LOWER
    public static ImmutableArray<IMethodSymbol> GetCreationMethods(
        this WithElementSyntax? withElement, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (withElement?.Parent is not CollectionExpressionSyntax collectionExpression)
            return [];

        var position = withElement.SpanStart;
        var within = semanticModel.GetEnclosingNamedType(position, cancellationToken);
        if (within == null)
            return [];

        if (semanticModel.GetTypeInfo(collectionExpression, cancellationToken).ConvertedType is not INamedTypeSymbol collectionExpressionType)
            return [];

        var result = TryGetInterfaceItems() ??
            TryGetCollectionBuilderItems() ??
            collectionExpressionType.InstanceConstructors;

        return result.WhereAsArray(c => c.IsAccessibleWithin(within: within, throughType: c.ContainingType));

        ImmutableArray<IMethodSymbol>? TryGetInterfaceItems()
        {
            // When the type is IList<T> or ICollection<T>, we can provide a signature help item for the `(int capacity)`
            // constructor of List<T>, as that's what the compiler will call into.

            var ilistOfTType = semanticModel.Compilation.IListOfTType();
            var icollectionOfTType = semanticModel.Compilation.ICollectionOfTType();

            if (!Equals(ilistOfTType, collectionExpressionType.OriginalDefinition) &&
                !Equals(icollectionOfTType, collectionExpressionType.OriginalDefinition))
            {
                return null;
            }

            var listOfTType = semanticModel.Compilation.ListOfTType();
            if (listOfTType is null)
                return [];

            var constructedListType = listOfTType.Construct(collectionExpressionType.TypeArguments.Single());
            var constructor = constructedListType.InstanceConstructors.FirstOrDefault(
                static m => m.Parameters is [{ Type.SpecialType: SpecialType.System_Int32, Name: "capacity" }]);

            return constructor is null ? [] : [constructor];
        }

        ImmutableArray<IMethodSymbol>? TryGetCollectionBuilderItems()
        {
            // If the type has a [CollectionBuilder(typeof(...), "...")] attribute on it, find the method it points to, and
            // produce the synthesized signature help items for it (e.g. without the ReadOnlySpan<T> parameter).
            var constructedBuilderMethods = CollectionExpressionUtilities.TryGetCollectionBuilderFactoryMethods(
                semanticModel.Compilation, collectionExpressionType);
            if (constructedBuilderMethods is null)
                return null;

            // Create a synthesized method with the ReadOnlySpan<T> parameter removed.  This corresponds to the parameters
            // that actually have to be passed to the with element.
            return constructedBuilderMethods.Value.SelectAsArray(
                constructedMethod => CodeGenerationSymbolFactory.CreateMethodSymbol(
                    constructedMethod,
                    parameters: constructedMethod.Parameters[..^1],
                    containingType: constructedMethod.ContainingType));
        }
    }
#endif
}
