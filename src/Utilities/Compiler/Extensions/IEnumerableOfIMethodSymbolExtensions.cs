using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Analyzer.Utilities.Extensions
{
    internal static class IEnumerableOfIMethodSymbolExtensions
    {
        /// <summary>
        /// Excludes <paramref name="methods"/> that have an attribute that precisely matches <paramref name="attributeType"/>.
        /// </summary>
        /// <param name="methods">List of <see cref="IMethodSymbol"/> to filter.</param>
        /// <param name="attributeType">The <see cref="INamedTypeSymbol"/> of the attribute class to search.</param>
        /// <returns>A filtered list of methods.</returns>
        public static IEnumerable<IMethodSymbol> WhereMethodDoesNotContainAttribute(
            this IEnumerable<IMethodSymbol> methods,
            INamedTypeSymbol? attributeType)
        {
            if (attributeType == null)
            {
                return methods;
            }

            return methods.Where(m => !m.HasAttribute(attributeType));
        }

        /// <summary>
        /// Returns a list of method symbols from a given list of the method symbols, which has its parameter type as
        /// expectedParameterType as its first parameter or the last parameter in addition to matching all the other 
        /// parameter types of the selectedOverload method symbol
        /// </summary>
        /// <param name="methods">List of <see cref="IMethodSymbol"/> to scan for possible overloads</param>
        /// <param name="selectedOverload"><see cref="IMethodSymbol"/> that is currently picked by the user</param>
        /// <param name="expectedParameterType"><see cref="INamedTypeSymbol"/> type of the leading parameter or the trailing parameter</param>
        /// <param name="trailingOnly"><see cref="INamedTypeSymbol"/> If the expected parameter should appear at the trailing position of the parameter list of the method overload</param>
        public static IEnumerable<IMethodSymbol> GetMethodOverloadsWithDesiredParameterAtLeadingOrTrailing(
             this IEnumerable<IMethodSymbol> methods,
             IMethodSymbol selectedOverload,
             INamedTypeSymbol expectedParameterType,
             bool trailingOnly = false)
        {
            return methods.Where(candidateMethod =>
            {
                if (!candidateMethod.Parameters.HasExactly(selectedOverload.Parameters.Count() + 1))
                {
                    return false;
                }

                // The expected method overload should either have the expectedParameterType parameter as the first argument or as the last argument
                // Assume expectedParameterType is the last parameter so j, which is the index of the parameter
                // in candidateMethod to compare against selectedOverload's parameter is set to 0
                int j = 0;

                if (!trailingOnly && candidateMethod.Parameters.First().Type.Equals(expectedParameterType) && candidateMethod.Parameters[0].RefKind == RefKind.None)
                {
                    // If expectedParameterType is the first parameter then the parameters to compare in candidateMethod against selectedOverload
                    // is offset by 1
                    j = 1;
                }
                else
                {
                    var lastParameter = candidateMethod.Parameters.Last();
                    if (!lastParameter.Type.Equals(expectedParameterType) || lastParameter.RefKind != RefKind.None)
                    {
                        // expectedParameterType is neither the first parameter nor the last parameter
                        return false;
                    }
                }

                for (int i = 0; i < selectedOverload.Parameters.Count(); i++, j++)
                {
                    if (!selectedOverload.Parameters[i].Type.Equals(candidateMethod.Parameters[j].Type) ||
                        selectedOverload.Parameters[i].IsParams != candidateMethod.Parameters[j].IsParams ||
                        selectedOverload.Parameters[i].RefKind != candidateMethod.Parameters[j].RefKind)
                    {
                        return false;
                    }
                }

                return true;
            });
        }

        /// <summary>
        /// Returns a list of method symbols from a given list of the method symbols, which has its parameter type as
        /// expectedParameterType as its last parameter in addition to matching all the other parameter types of the 
        /// selectedOverload method symbol
        /// </summary>
        /// <param name="methods">List of <see cref="IMethodSymbol"/> to scan for possible overloads</param>
        /// <param name="selectedOverload"><see cref="IMethodSymbol"/> that is currently picked by the user</param>
        /// <param name="expectedTrailingParameterType"><see cref="INamedTypeSymbol"/> type of the leading parameter or the trailing parameter</param>
        public static IEnumerable<IMethodSymbol> GetMethodOverloadsWithDesiredParameterAtTrailing(
             this IEnumerable<IMethodSymbol> methods,
             IMethodSymbol selectedOverload,
             INamedTypeSymbol expectedTrailingParameterType)
        {
            return GetMethodOverloadsWithDesiredParameterAtLeadingOrTrailing(methods, selectedOverload, expectedTrailingParameterType, trailingOnly: true);
        }

        /// <summary>
        /// Given a <see cref="IEnumerable{IMethodSymbol}"/>, this method returns the method symbol which 
        /// matches the expectedParameterTypesInOrder parameter requirement
        /// </summary>
        /// <param name="members"></param>
        /// <param name="expectedParameterTypesInOrder"></param>
        /// <returns></returns>
        public static IMethodSymbol? GetFirstOrDefaultMemberWithParameterInfos(this IEnumerable<IMethodSymbol>? members, params ParameterInfo[] expectedParameterTypesInOrder)
        {
            var expectedParameterCount = expectedParameterTypesInOrder.Length;
            return members?.FirstOrDefault(member =>
            {
                if (member.Parameters.Count() != expectedParameterCount)
                {
                    return false;
                }

                for (int i = 0; i < expectedParameterCount; i++)
                {
                    // check IsParams only on the last parameter
                    if (i == expectedParameterCount - 1 &&
                        member.Parameters[i].IsParams != expectedParameterTypesInOrder[i].IsParams)
                    {
                        return false;
                    }

                    var parameterType = member.Parameters[i].Type;
                    if (expectedParameterTypesInOrder[i].IsArray)
                    {
                        var arrayParameterSymbol = parameterType as IArrayTypeSymbol;
                        if (arrayParameterSymbol?.Rank != expectedParameterTypesInOrder[i].ArrayRank)
                        {
                            return false;
                        }

                        parameterType = arrayParameterSymbol.ElementType;
                    }

                    if (!expectedParameterTypesInOrder[i].ParameterType.Equals(parameterType))
                    {
                        return false;
                    }
                }

                return true;
            });
        }
    }

    // Contains the expected properties of a parameter
    internal class ParameterInfo
    {
        public int ArrayRank { get; private set; }
        public bool IsArray { get; private set; }
        public bool IsParams { get; private set; }
        public INamedTypeSymbol ParameterType { get; private set; }

        private ParameterInfo(INamedTypeSymbol type, bool isArray, int arrayRank, bool isParams)
        {
            ParameterType = type;
            IsArray = isArray;
            ArrayRank = arrayRank;
            IsParams = isParams;
        }

        public static ParameterInfo GetParameterInfo(INamedTypeSymbol type, bool isArray, int arrayRank, bool isParams)
        {
            return new ParameterInfo(type, isArray, arrayRank, isParams);
        }
    }
}
