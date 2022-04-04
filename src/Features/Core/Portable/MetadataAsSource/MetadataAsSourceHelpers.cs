// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    /// <summary>
    /// Helpers shared by both the text service and the editor service
    /// </summary>
    internal class MetadataAsSourceHelpers
    {

#if false
        public static void ValidateSymbolArgument(ISymbol symbol, string parameterName)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(parameterName);
            }
            else if (!MetadataAsSourceHelpers.ValidSymbolKinds.Contains(symbol.Kind))
            {
                throw new ArgumentException(FeaturesResources.generating_source_for_symbols_of_this_type_is_not_supported, parameterName);
            }
        }
#endif

        public static string GetAssemblyInfo(IAssemblySymbol assemblySymbol)
        {
            return string.Format(
                "{0} {1}",
                FeaturesResources.Assembly,
                assemblySymbol.Identity.GetDisplayName());
        }

        public static string GetAssemblyDisplay(Compilation compilation, IAssemblySymbol assemblySymbol)
        {
            // This method is only used to generate a comment at the top of Metadata-as-Source documents and
            // previous submissions are never viewed as metadata (i.e. we always have compilations) so there's no
            // need to consume compilation.ScriptCompilationInfo.PreviousScriptCompilation.
            var assemblyReference = compilation.GetMetadataReference(assemblySymbol);
            return assemblyReference?.Display ?? FeaturesResources.location_unknown;
        }

        public static INamedTypeSymbol GetTopLevelContainingNamedType(ISymbol symbol)
        {
            // Traverse up until we find a named type that is parented by the namespace
            var topLevelNamedType = symbol;
            while (topLevelNamedType.ContainingSymbol != symbol.ContainingNamespace ||
                topLevelNamedType.Kind != SymbolKind.NamedType)
            {
                topLevelNamedType = topLevelNamedType.ContainingSymbol;
            }

            return (INamedTypeSymbol)topLevelNamedType;
        }

        public static async Task<Location> GetLocationInGeneratedSourceAsync(SymbolKey symbolId, Document generatedDocument, CancellationToken cancellationToken)
        {
            var resolution = symbolId.Resolve(
                await generatedDocument.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false),
                ignoreAssemblyKey: true, cancellationToken: cancellationToken);

            var location = GetFirstSourceLocation(resolution);
            if (location == null)
            {
                // If we cannot find the location of the  symbol.  Just put the caret at the 
                // beginning of the file.
                var tree = await generatedDocument.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                location = Location.Create(tree, new TextSpan(0, 0));
            }

            return location;
        }

        private static Location? GetFirstSourceLocation(SymbolKeyResolution resolution)
        {
            foreach (var symbol in resolution)
            {
                foreach (var location in symbol.Locations)
                {
                    if (location.IsInSource)
                    {
                        return location;
                    }
                }
            }

            return null;
        }

        public static bool TryGetImplementationAssemblyPath(string referenceDllPath, [NotNullWhen(true)] out string? implementationDllPath)
        {
            implementationDllPath = null;

            // For some nuget packages if the reference path has a "ref" folder in it, then the implementation assembly
            // will be in the corresponding "lib" folder.
            // TODO: Support more cases, like SDK references: https://github.com/dotnet/sdk/issues/12360
            var start = referenceDllPath.IndexOf(@"\ref\");
            if (start == -1)
                return false;

            var pathToTry = referenceDllPath.Substring(0, start) +
                            @"\lib\" +
                            referenceDllPath.Substring(start + 5);

            if (IOUtilities.PerformIO(() => File.Exists(pathToTry)))
            {
                implementationDllPath = pathToTry;
                return true;
            }

            return false;
        }
    }
}
