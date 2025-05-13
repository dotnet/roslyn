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

        var collectionExpressionOriginalType = collectionExpressionType.OriginalDefinition;
        var compilation = semanticModel.Compilation;
        var result = TryGetInterfaceItems() ??
            TryGetCollectionBuilderItems() ??
            collectionExpressionType.InstanceConstructors;

        return result.WhereAsArray(c => c.IsAccessibleWithin(within: within, throughType: c.ContainingType));

        ImmutableArray<IMethodSymbol>? TryGetInterfaceItems()
        {
            // When the type is IList<T> or ICollection<T>, we can provide a signature help item for the `(int capacity)`
            // constructor of List<T>, as that's what the compiler will call into.  When the type is IDictionary<,> we
            // provide signature help for the overloads of Dictionary<,> that take a capacity or IEqualityComparer.

            if (Equals(compilation.IListOfTType(), collectionExpressionOriginalType) ||
                Equals(compilation.ICollectionOfTType(), collectionExpressionOriginalType))
            {
                var constructedType = compilation.ListOfTType()?.Construct([.. collectionExpressionType.TypeArguments]);
                return constructedType is not null
                    ? constructedType.InstanceConstructors.WhereAsArray(c => c.Parameters.All(p => p.Name is "capacity"))
                    : [];
            }
            else if (Equals(compilation.IDictionaryOfTKeyTValueType(), collectionExpressionOriginalType))
            {
                var constructedType = compilation.DictionaryOfTKeyTValueType()?.Construct([.. collectionExpressionType.TypeArguments]);
                return constructedType is not null
                    ? constructedType.InstanceConstructors.WhereAsArray(c => c.Parameters.All(p => p.Name is "capacity" or "comparer"))
                    : [];
            }
            else
            {
                return null;
            }
        }

        ImmutableArray<IMethodSymbol>? TryGetCollectionBuilderItems()
        {
            // If the type has a [CollectionBuilder(typeof(...), "...")] attribute on it, find the method it points to, and
            // produce the synthesized signature help items for it (e.g. without the ReadOnlySpan<T> parameter).
            var constructedBuilderMethods = CollectionExpressionUtilities.TryGetCollectionBuilderFactoryMethods(
                semanticModel.Compilation, collectionExpressionType);
            if (constructedBuilderMethods is null)
                return null;

            var readonlySpanOfTType = semanticModel.Compilation.ReadOnlySpanOfTType();
            return constructedBuilderMethods.Value.SelectAsArray(constructedMethod =>
            {
                // Create a synthesized method with the ReadOnlySpan<T> parameter removed.  This corresponds to the parameters
                // that actually have to be passed to the with element.
                return CodeGenerationSymbolFactory.CreateMethodSymbol(
                    constructedMethod,
                    parameters: constructedMethod.Parameters[..^1],
                    containingType: constructedMethod.ContainingType);
            });
        }
    }
}
