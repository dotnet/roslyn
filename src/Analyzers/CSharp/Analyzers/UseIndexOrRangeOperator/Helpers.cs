// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator;

internal static class Helpers
{
    public static bool IsStringRemoveMethod(IMethodSymbol method)
        => method is { ContainingType.SpecialType: SpecialType.System_String, Name: nameof(string.Remove) };

    /// <summary>
    /// Find an `int MyType.Count` or `int MyType.Length` property.
    /// </summary>
    public static IPropertySymbol? TryGetLengthOrCountProperty(ITypeSymbol namedType)
        => TryGetNoArgInt32Property(namedType, nameof(string.Length)) ??
           TryGetNoArgInt32Property(namedType, nameof(ICollection.Count));

    /// <summary>
    /// Tried to find a public, non-static, int-returning property in the given type with the
    /// specified <paramref name="name"/>.
    /// </summary>
    public static IPropertySymbol? TryGetNoArgInt32Property(ITypeSymbol type, string name)
        => type.GetMembers(name)
               .OfType<IPropertySymbol>()
               .Where(p => IsPublicInstance(p) &&
                           p.Type.SpecialType == SpecialType.System_Int32)
               .FirstOrDefault();

    public static bool IsPublicInstance(ISymbol symbol)
        => symbol is { IsStatic: false, DeclaredAccessibility: Accessibility.Public };

    /// <summary>
    /// Checks if this <paramref name="operation"/> is `expr.Length` where `expr` is equivalent
    /// to the <paramref name="instance"/> we were originally invoking an accessor/method off
    /// of.
    /// </summary>
    public static bool IsInstanceLengthCheck(IPropertySymbol lengthLikeProperty, IOperation instance, IOperation operation)
        => operation is IPropertyReferenceOperation { Instance: not null } propertyRef &&
           lengthLikeProperty.Equals(propertyRef.Property) &&
           CSharpSyntaxFacts.Instance.AreEquivalent(instance.Syntax, propertyRef.Instance.Syntax);

    /// <summary>
    /// Checks if <paramref name="operation"/> is a binary subtraction operator. If so, it
    /// will be returned through <paramref name="subtraction"/>.
    /// </summary>
    public static bool IsSubtraction(IOperation operation, [NotNullWhen(true)] out IBinaryOperation? subtraction)
    {
        if (operation is IBinaryOperation { OperatorKind: BinaryOperatorKind.Subtract } binaryOperation)
        {
            subtraction = binaryOperation;
            return true;
        }

        subtraction = null;
        return false;
    }

    /// <summary>
    /// Look for methods like "SomeType MyType.Get(int)".  Also matches against the 'getter'
    /// of an indexer like 'SomeType MyType.this[int]`
    /// </summary>
    public static bool IsIntIndexingMethod(IMethodSymbol method)
        => method != null &&
           method.MethodKind is MethodKind.PropertyGet or MethodKind.Ordinary &&
           IsPublicInstance(method) &&
           method.Parameters.Length == 1 &&
           // From: https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/ranges.md#decisions-made-during-implementation
           //
           // When looking for the pattern members, we look for original definitions, not
           // constructed members
           method.OriginalDefinition.Parameters[0].Type.SpecialType == SpecialType.System_Int32;

    /// <summary>
    /// Look for methods like "SomeType MyType.Slice(int start, int length)".  Note that the
    /// names of the parameters are checked to ensure they are appropriate slice-like.  These
    /// names were picked by examining the patterns in the BCL for slicing members.
    /// </summary>
    public static bool IsTwoArgumentSliceLikeMethod(IMethodSymbol method)
    {
        // From: https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/ranges.md#decisions-made-during-implementation
        //
        // When looking for the pattern members, we look for original definitions, not
        // constructed members
        return method != null &&
            IsPublicInstance(method) &&
            method.Parameters.Length == 2 &&
            IsSliceFirstParameter(method.OriginalDefinition.Parameters[0]) &&
            IsSliceSecondParameter(method.OriginalDefinition.Parameters[1]);
    }

    /// <summary>
    /// Look for methods like "SomeType MyType.Slice(int start)".  Note that the
    /// name of the parameter is checked to ensure it is appropriate slice-like.
    /// This name was picked by examining the patterns in the BCL for slicing members.
    /// </summary>
    public static bool IsOneArgumentSliceLikeMethod(IMethodSymbol method)
    {
        // From: https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/ranges.md#decisions-made-during-implementation
        //
        // When looking for the pattern members, we look for original definitions, not
        // constructed members
        return method != null &&
            IsPublicInstance(method) &&
            method.Parameters.Length == 1 &&
            IsSliceFirstParameter(method.OriginalDefinition.Parameters[0]);
    }

    private static bool IsSliceFirstParameter(IParameterSymbol parameter)
        => parameter.Type.SpecialType == SpecialType.System_Int32 &&
           (parameter.Name == "start" || parameter.Name == "startIndex");

    private static bool IsSliceSecondParameter(IParameterSymbol parameter)
        => parameter.Type.SpecialType == SpecialType.System_Int32 &&
           (parameter.Name == "count" || parameter.Name == "length");

    /// <summary>
    /// Finds a public, non-static indexer in the given type.  The indexer has to accept the
    /// provided <paramref name="parameterType"/> and must return the provided <paramref
    /// name="returnType"/>.
    /// </summary>
    public static IPropertySymbol? GetIndexer(ITypeSymbol type, ITypeSymbol parameterType, ITypeSymbol returnType)
        => type.GetMembers(WellKnownMemberNames.Indexer)
               .OfType<IPropertySymbol>()
               .Where(p => p.IsIndexer &&
                           IsPublicInstance(p) &&
                           returnType.Equals(p.Type) &&
                           p.Parameters.Length == 1 &&
                           p.Parameters[0].Type.Equals(parameterType))
               .FirstOrDefault();

    /// <summary>
    /// Finds a public, non-static overload of <paramref name="method"/> in the containing type.
    /// The overload must have the same return type as <paramref name="method"/>.  It must only
    /// have a single parameter, with the provided <paramref name="parameterType"/>.
    /// </summary>
    public static IMethodSymbol? GetOverload(IMethodSymbol method, ITypeSymbol parameterType)
        => method.MethodKind != MethodKind.Ordinary
            ? null
            : method.ContainingType.GetMembers(method.Name)
                                   .OfType<IMethodSymbol>()
                                   .Where(m => IsPublicInstance(m) &&
                                               m.Parameters.Length == 1 &&
                                               m.Parameters[0].Type.Equals(parameterType) &&
                                               m.ReturnType.Equals(method.ReturnType))
                                   .FirstOrDefault();
}
