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
            var compilation = semanticModel.Compilation;

            var readonlySpanOfTType = compilation.ReadOnlySpanOfTType();
            var attribute = collectionExpressionType.GetAttributes().FirstOrDefault(
                static a => a.AttributeClass.IsCollectionBuilderAttribute());

            // https://github.com/dotnet/csharplang/blob/main/proposals/collection-expression-arguments.md#create-method-candidates
            // A [CollectionBuilder(...)] attribute specifies the builder type and method name of a method to be invoked
            // to construct an instance of the collection type.
            if (attribute is not { ConstructorArguments: [{ Value: INamedTypeSymbol builderType }, { Value: string builderMethodName }] })
                return null;

            // Find all the methods in the builder type with the given name that have a ReadOnlySpan<T> as either their
            // first or last parameter.
            var builderMethods = builderType
                // The method must have the name specified in the [CollectionBuilder(...)] attribute.
                .GetMembers(builderMethodName)
                .OfType<IMethodSymbol>()
                .Where(m =>
                    // The method must be static.
                    m.IsStatic &&
                    // The arity of the method must match the arity of the collection type.
                    m.Arity == collectionExpressionType.Arity &&
                    m.Parameters.Length >= 1 &&
                    // The method must have a first (or last) parameter of type System.ReadOnlySpan<E>, passed by value.
                    (Equals(m.Parameters[0].Type.OriginalDefinition, readonlySpanOfTType) ||
                     Equals(m.Parameters.Last().Type.OriginalDefinition, readonlySpanOfTType)))
                .ToImmutableArray();

            // Instance the construction method if generic. And filter to only those that return the collection type
            // being created.
            var constructedBuilderMethods = builderMethods
                .Select(m => m.Construct([.. collectionExpressionType.TypeArguments]))
                .Where(m =>
                {
                    // There is an identity conversion, implicit reference conversion, or boxing conversion from the method return type to the collection type.
                    var conversion = compilation.ClassifyConversion(m.ReturnType, collectionExpressionType);
                    return conversion.IsIdentityOrImplicitReference() || conversion.IsBoxing;
                })
                .ToImmutableArray();

            return constructedBuilderMethods.SelectAsArray(constructedMethod =>
            {
                // Create a synthesized method with the ReadOnlySpan<T> parameter removed.  This corresponds to the parameters
                // that actually have to be passed to the with element.
                var slicedParameters = Equals(constructedMethod.Parameters[0].Type.OriginalDefinition, readonlySpanOfTType)
                    ? constructedMethod.Parameters[1..]
                    : constructedMethod.Parameters[..^1];

                return CodeGenerationSymbolFactory.CreateMethodSymbol(
                    constructedMethod,
                    parameters: slicedParameters,
                    containingType: constructedMethod.ContainingType);
            });
        }
    }
}
