using System.Linq;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Extensions
{
    /// <summary>
    /// Provides extensions to <see cref="Compilation"/>.
    /// </summary>
    internal static class CompilationExtensions
    {
        /// <summary>
        /// Gets the type within the compilation's assembly using its canonical CLR metadata name. If not found, gets the first public type from all referenced assemblies. If not found, gets the first type from all referenced assemblies.
        /// </summary>
        /// <param name="compilation">The compilation.</param>
        /// <param name="fullyQualifiedMetadataName">The fully qualified metadata name.</param>
        /// <returns>A <see cref="INamedTypeSymbol"/> if found; <see langword="null"/> otherwise.</returns>
        internal static INamedTypeSymbol GetVisibleTypeByMetadataName(this Compilation compilation, string fullyQualifiedMetadataName)
        {
            var typeSymbol = compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
            if (typeSymbol is object)
            {
                return typeSymbol;
            }

            var segments = fullyQualifiedMetadataName.Split('.');

            var @namespace = compilation.GlobalNamespace;

            for (var s = 0; s < segments.Length - 1; s++)
            {
                @namespace = @namespace.GetMembers(segments[s]).OfType<INamespaceSymbol>().SingleOrDefault();

                if (@namespace is null)
                {
                    return null;
                }
            }

            var metadataName = segments[segments.Length - 1];
            INamedTypeSymbol publicType = null;
            INamedTypeSymbol anyType = null;

            foreach (var member in @namespace.GetMembers())
            {
                if (member is INamedTypeSymbol type && type.MetadataName == metadataName)
                {
                    if (type.ContainingAssembly.Equals(compilation.Assembly))
                    {
                        return type;
                    }
                    else if (publicType is null && type.DeclaredAccessibility == Accessibility.Public)
                    {
                        publicType = type;
                        break;
                    }
                    else if (anyType is null)
                    {
                        anyType = type;
                    }
                }
            }

            return publicType ?? anyType;
        }

        /// <summary>
        /// Gets a type by its full type name and cache it at the compilation level.
        /// </summary>
        /// <param name="compilation">The compilation.</param>
        /// <param name="fullTypeName">Namespace + type name, e.g. "System.Exception".</param>
        /// <returns>The <see cref="INamedTypeSymbol"/> if found, null otherwise.</returns>
        internal static INamedTypeSymbol GetOrCreateWellKnownType(this Compilation compilation, string fullTypeName) =>
            WellKnownTypeProvider.GetOrCreate(compilation).GetTypeByMetadataName(fullTypeName);
    }
}
