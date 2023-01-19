// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ImplementInterface
{
    internal abstract partial class AbstractImplementInterfaceService
    {
        private static class AccessibilityHelper
        {
            public static bool IsLessAccessibleThan(ISymbol? first, INamedTypeSymbol second)
            {
                if (first is null)
                {
                    return false;
                }

                if (first.DeclaredAccessibility <= Accessibility.NotApplicable ||
                    second.DeclaredAccessibility <= Accessibility.NotApplicable)
                {
                    return false;
                }

                if (first.DeclaredAccessibility < second.DeclaredAccessibility)
                {
                    return true;
                }

                switch (first)
                {
                    case IPropertySymbol propertySymbol:
                        if (IsTypeLessAccessibleThanOtherType(propertySymbol.Type, second, new()))
                        {
                            return true;
                        }

                        if (IsLessAccessibleThan(propertySymbol.GetMethod, second))
                        {
                            return true;
                        }

                        if (IsLessAccessibleThan(propertySymbol.SetMethod, second))
                        {
                            return true;
                        }

                        return false;

                    case IMethodSymbol methodSymbol:
                        if (IsTypeLessAccessibleThanOtherType(methodSymbol.ReturnType, second, new()))
                        {
                            return true;
                        }

                        foreach (var parameter in methodSymbol.Parameters)
                        {
                            if (IsTypeLessAccessibleThanOtherType(parameter.Type, second, new()))
                            {
                                return true;
                            }
                        }

                        foreach (var typeArg in methodSymbol.TypeArguments)
                        {
                            if (IsTypeLessAccessibleThanOtherType(typeArg, second, new()))
                            {
                                return true;
                            }
                        }

                        return false;

                    case IEventSymbol eventSymbol:
                        return IsTypeLessAccessibleThanOtherType(eventSymbol.Type, second, new());

                    default:
                        return false;
                }
            }

            private static bool IsTypeLessAccessibleThanOtherType(ITypeSymbol? first, INamedTypeSymbol second, HashSet<ITypeSymbol> alreadyCheckingTypes)
            {
                if (first is null)
                {
                    return false;
                }

                alreadyCheckingTypes.Add(first);

                if (first is ITypeParameterSymbol typeParameter)
                {
                    foreach (var constraint in typeParameter.ConstraintTypes)
                    {
                        if (alreadyCheckingTypes.Contains(constraint))
                        {
                            continue;
                        }

                        if (IsTypeLessAccessibleThanOtherType(constraint, second, alreadyCheckingTypes))
                        {
                            return true;
                        }
                    }
                }

                if (first.DeclaredAccessibility <= Accessibility.NotApplicable ||
                    second.DeclaredAccessibility <= Accessibility.NotApplicable)
                {
                    return false;
                }

                if (first.DeclaredAccessibility < second.DeclaredAccessibility)
                {
                    return true;
                }

                if (first is INamedTypeSymbol namedType)
                {
                    foreach (var genericParam in namedType.TypeArguments)
                    {
                        if (alreadyCheckingTypes.Contains(genericParam))
                        {
                            continue;
                        }

                        if (IsTypeLessAccessibleThanOtherType(genericParam, second, alreadyCheckingTypes))
                        {
                            return true;
                        }
                    }
                }

                if (IsTypeLessAccessibleThanOtherType(first.ContainingType, second, alreadyCheckingTypes))
                {
                    return true;
                }

                return false;
            }
        }
    }
}
