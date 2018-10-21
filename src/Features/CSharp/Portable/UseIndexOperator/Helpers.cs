// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOperator
{
    internal static class Helpers
    {
        public static IPropertySymbol GetLengthOrCountProperty(INamedTypeSymbol namedType)
            => GetNoArgInt32Property(namedType, nameof(string.Length)) ??
               GetNoArgInt32Property(namedType, nameof(ICollection.Count));

        private static IPropertySymbol GetNoArgInt32Property(INamedTypeSymbol type, string name)
            => type.GetMembers(name)
                   .OfType<IPropertySymbol>()
                   .Where(p => IsPublicInstance(p) &&
                               p.Type.SpecialType == SpecialType.System_Int32)
                   .FirstOrDefault();

        public static bool IsPublicInstance(ISymbol symbol)
            => !symbol.IsStatic && symbol.DeclaredAccessibility == Accessibility.Public;

        public static IPropertySymbol GetIndexer(INamedTypeSymbol namedType, ITypeSymbol parameterType)
            => namedType.GetMembers()
                        .OfType<IPropertySymbol>()
                        .Where(p => p.IsIndexer &&
                                    IsPublicInstance(p) &&
                                    p.Parameters.Length == 1 &&
                                    p.Parameters[0].Type.Equals(parameterType))
                        .FirstOrDefault();
    }
}
