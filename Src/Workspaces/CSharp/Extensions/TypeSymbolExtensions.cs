using System.Collections.Generic;
using Roslyn.Compilers.CSharp;

namespace Roslyn.Services.Editor.CSharp.Extensions
{
    internal static partial class TypeSymbolExtensions
    {
        public static IList<NamedTypeSymbol> GetAllInterfacesIncludingThis(this TypeSymbol type)
        {
            var allInterfaces = type.AllInterfaces;
            if (type is NamedTypeSymbol)
            {
                var namedType = type as NamedTypeSymbol;
                if (namedType.TypeKind == TypeKind.Interface &&
                    !allInterfaces.Contains(namedType))
                {
                    var result = new List<NamedTypeSymbol>() { namedType };

                    result.AddRange(allInterfaces.AsEnumerable());
                    return result;
                }
            }

            return allInterfaces.AsList();
        }

        public static bool IsNullOrError(this TypeSymbol type)
        {
            return type == null || type.TypeKind == TypeKind.Error;
        }
    }
}