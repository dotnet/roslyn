using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Collections;

namespace Roslyn.Compilers.CSharp
{
    internal static partial class NamedTypeSymbolExtensions
    {
        /// <summary>
        /// Returns a constructed type given its instance type and type arguments.
        /// </summary>
        /// <param name="instanceType">the instance type to construct the result from</param>
        /// <param name="typeArguments">the immediate type arguments to be replaced for type parameters in the instance type</param>
        /// <returns></returns>
        internal static NamedTypeSymbol Construct1(this NamedTypeSymbol instanceType, ReadOnlyArray<TypeSymbol> typeArguments)
        {
            Debug.Assert(instanceType.ConstructedFrom == instanceType);
            
            var sequenceEqual = true;
            var args = instanceType.TypeArguments;

            if (args.Count != typeArguments.Count)
            {
                sequenceEqual = false;
            }
            else
            {
                for (int i = 0; i < args.Count; i++)
                {
                    if (args[i] != typeArguments[i])
                    {
                        sequenceEqual = false;
                        break;
                    }
                }
            }

            return sequenceEqual
                ? instanceType
                : ConstructedNamedTypeSymbol.Make(instanceType, typeArguments);
        }

        /// <summary>
        /// Returns a constructed type given the type it is constructed from and type arguments for its enclosing types and itself.
        /// </summary>
        /// <param name="declaredType">the declared type to construct the result from</param>
        /// <param name="typeArguments">the type arguments that will replace the type parameters, starting with those for enclosing types</param>
        /// <returns></returns>
        internal static NamedTypeSymbol DeepConstruct(this NamedTypeSymbol declaredType, ReadOnlyArray<TypeSymbol> typeArguments)
        {
            Debug.Assert(declaredType.OriginalDefinition == declaredType);
            return new TypeMap(declaredType.AllTypeParameters().AsReadOnly<TypeSymbol>(), typeArguments).SubstituteNamedType(declaredType);
        }

        /// <summary>
        /// Return all of the type parameters of enclosing classes and the class itself, or an empty sequence when given null.
        /// </summary>
        /// <param name="declaredType">a named type whose type parameters are to be returned</param>
        /// <returns></returns>
        internal static IEnumerable<TypeParameterSymbol> AllTypeParameters(this NamedTypeSymbol declaredType)
        {
            return declaredType == null
                ? ConsList.Empty<TypeParameterSymbol>()
                : AllTypeParameters(declaredType.ContainingType).Concat(declaredType.TypeParameters.AsEnumerable());
        }
    }
}
