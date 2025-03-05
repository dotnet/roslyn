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
    public static ImmutableArray<IMethodSymbol> GetCreationMethods(
        SemanticModel semanticModel, WithElementSyntax? withElement, CancellationToken cancellationToken)
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
                m => m.Parameters is [{ Type.SpecialType: SpecialType.System_Int32, Name: "capacity" }]);

            return constructor is null ? [] : [constructor];
        }

        ImmutableArray<IMethodSymbol>? TryGetCollectionBuilderItems()
        {
            // If the type has a [CollectionBuilder(typeof(...), "...")] attribute on it, find the method it points to, and
            // produce the synthesized signature help items for it (e.g. without the ReadOnlySpan<T> parameter).
            var readonlySpanOfTType = semanticModel.Compilation.ReadOnlySpanOfTType();
            var attribute = collectionExpressionType.GetAttributes().FirstOrDefault(
                a => a.AttributeClass.IsCollectionBuilderAttribute());
            if (attribute is not { ConstructorArguments: [{ Value: INamedTypeSymbol builderType }, { Value: string builderMethodName }] })
                return null;

            var builderMethod = builderType
                .GetMembers(builderMethodName)
                .OfType<IMethodSymbol>()
                .Where(m =>
                    m.IsStatic && m.Parameters.Length >= 1 &&
                    m.Arity == collectionExpressionType.Arity &&
                    (Equals(m.Parameters[0].Type.OriginalDefinition, readonlySpanOfTType) ||
                     Equals(m.Parameters.Last().Type.OriginalDefinition, readonlySpanOfTType)))
                .FirstOrDefault();

            if (builderMethod is null)
                return [];

            var constructedBuilderMethod = builderMethod.Construct([.. collectionExpressionType.TypeArguments]);
            var slicedParameters = Equals(constructedBuilderMethod.Parameters[0].Type.OriginalDefinition, readonlySpanOfTType)
                ? builderMethod.Parameters[1..]
                : builderMethod.Parameters[..^1];

            var slicedMethod = CodeGenerationSymbolFactory.CreateMethodSymbol(
                constructedBuilderMethod,
                parameters: slicedParameters,
                containingType: constructedBuilderMethod.ContainingType);
            return [slicedMethod];
        }
    }
}
