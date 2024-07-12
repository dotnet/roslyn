// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class IParameterSymbolExtensions
{
    public static bool IsRefOrOut(this IParameterSymbol symbol)
    {
        switch (symbol.RefKind)
        {
            case RefKind.Ref:
            case RefKind.Out:
                return true;
            default:
                return false;
        }
    }

    public static IPropertySymbol? GetAssociatedSynthesizedRecordProperty(this IParameterSymbol parameter, CancellationToken cancellationToken)
    {
        if (parameter is
            {
                DeclaringSyntaxReferences.Length: > 0,
                ContainingSymbol: IMethodSymbol
                {
                    MethodKind: MethodKind.Constructor,
                    DeclaringSyntaxReferences.Length: > 0,
                    ContainingType: { IsRecord: true } containingType,
                } constructor,
            })
        {
            // ok, we have a record constructor.  This might be the primary constructor or not.
            var parameterSyntax = parameter.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
            var constructorSyntax = constructor.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
            if (containingType.DeclaringSyntaxReferences.Any(predicate: static (r, arg) => r.GetSyntax(arg.cancellationToken) == arg.constructorSyntax, arg: (constructorSyntax, cancellationToken)))
            {
                // this was a primary constructor. see if we can map this parameter to a corresponding synthesized property 
                foreach (var member in containingType.GetMembers(parameter.Name))
                {
                    if (member is IPropertySymbol { DeclaringSyntaxReferences.Length: > 0 } property &&
                        property.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) == parameterSyntax)
                    {
                        return property;
                    }
                }
            }
        }

        return null;
    }

    public static bool IsPrimaryConstructor(this IParameterSymbol parameter, CancellationToken cancellationToken)
    {
        if (parameter is
            {
                ContainingSymbol: IMethodSymbol
                {
                    MethodKind: MethodKind.Constructor,
                    DeclaringSyntaxReferences: [var constructorReference, ..],
                    ContainingType: { } containingType,
                } constructor,
            })
        {
            var constructorSyntax = constructorReference.GetSyntax(cancellationToken);
            return containingType.DeclaringSyntaxReferences.Any(predicate: static (r, arg) => r.GetSyntax(arg.cancellationToken) == arg.constructorSyntax, arg: (constructorSyntax, cancellationToken));
        }

        return false;
    }
}
