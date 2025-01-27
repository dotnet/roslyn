// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class CompilationExtensions
{
    /// <summary>
    /// Gets a type by its metadata name to use for code analysis within a <see cref="Compilation"/>. This method
    /// attempts to find the "best" symbol to use for code analysis, which is the symbol matching the first of the
    /// following rules.
    ///
    /// <list type="number">
    ///   <item><description>
    ///     If only one type with the given name is found within the compilation and its referenced assemblies, that
    ///     type is returned regardless of accessibility.
    ///   </description></item>
    ///   <item><description>
    ///     If the current <paramref name="compilation"/> defines the symbol, that symbol is returned.
    ///   </description></item>
    ///   <item><description>
    ///     If exactly one referenced assembly defines the symbol in a manner that makes it visible to the current
    ///     <paramref name="compilation"/>, that symbol is returned.
    ///   </description></item>
    ///   <item><description>
    ///     Otherwise, this method returns <see langword="null"/>.
    ///   </description></item>
    /// </list>
    /// </summary>
    /// <param name="compilation">The <see cref="Compilation"/> to consider for analysis.</param>
    /// <param name="fullyQualifiedMetadataName">The fully-qualified metadata type name to find.</param>
    /// <returns>The symbol to use for code analysis; otherwise, <see langword="null"/>.</returns>
    public static INamedTypeSymbol? GetBestTypeByMetadataName(this Compilation compilation, string fullyQualifiedMetadataName)
    {
        INamedTypeSymbol? type = null;

        foreach (var currentType in compilation.GetTypesByMetadataName(fullyQualifiedMetadataName))
        {
            if (ReferenceEquals(currentType.ContainingAssembly, compilation.Assembly))
            {
                Debug.Assert(type is null);
                return currentType;
            }

            switch (currentType.GetResultantVisibility())
            {
                case Utilities.SymbolVisibility.Public:
                case Utilities.SymbolVisibility.Internal when currentType.ContainingAssembly.GivesAccessTo(compilation.Assembly):
                    break;

                default:
                    continue;
            }

            if (type is object)
            {
                // Multiple visible types with the same metadata name are present
                return null;
            }

            type = currentType;
        }

        return type;
    }

    /// <summary>
    /// Gets implicit method, that wraps top-level statements.
    /// </summary>
    public static IMethodSymbol? GetTopLevelStatementsMethod(this Compilation compilation)
    {
        foreach (var candidateTopLevelType in compilation.SourceModule.GlobalNamespace.GetTypeMembers(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName, arity: 0))
        {
            foreach (var candidateMember in candidateTopLevelType.GetMembers(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName))
            {
                if (candidateMember is IMethodSymbol method)
                    return method;
            }
        }

        return null;
    }

    public static INamedTypeSymbol? TryGetCallingConventionSymbol(this Compilation compilation, string callingConvention)
        => compilation.GetBestTypeByMetadataName($"System.Runtime.CompilerServices.CallConv{callingConvention}");
}
