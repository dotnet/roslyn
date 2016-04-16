// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    /// <summary>
    /// Helpers shared by both the text service and the editor service
    /// </summary>
    internal class MetadataAsSourceHelpers
    {
        private static readonly HashSet<SymbolKind> s_validSymbolKinds = new HashSet<SymbolKind>(new[]
            {
                SymbolKind.Event,
                SymbolKind.Field,
                SymbolKind.Method,
                SymbolKind.NamedType,
                SymbolKind.Property,
                SymbolKind.Parameter,
            });

#if false
        public static void ValidateSymbolArgument(ISymbol symbol, string parameterName)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(parameterName);
            }
            else if (!MetadataAsSourceHelpers.ValidSymbolKinds.Contains(symbol.Kind))
            {
                throw new ArgumentException(FeaturesResources.GeneratingSourceForSymbols, parameterName);
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

            // TODO (https://github.com/dotnet/roslyn/issues/6859): compilation.GetMetadataReference(assemblySymbol)?
            var assemblyReference = compilation.References.Where(r =>
            {
                var referencedSymbol = compilation.GetAssemblyOrModuleSymbol(r) as IAssemblySymbol;
                return
                    referencedSymbol != null &&
                    referencedSymbol.MetadataName == assemblySymbol.MetadataName;
            })
            .FirstOrDefault();

            return assemblyReference?.Display ?? FeaturesResources.LocationUnknown;
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
            var location = symbolId.Resolve(
                await generatedDocument.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false),
                ignoreAssemblyKey: true, cancellationToken: cancellationToken)
                .GetAllSymbols()
                .Select(s => s.Locations.Where(loc => loc.IsInSource).FirstOrDefault())
                .WhereNotNull()
                .FirstOrDefault();

            if (location == null)
            {
                // If we cannot find the symbol, then put the caret at the beginning of the file.
                var tree = await generatedDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                location = Location.Create(tree, new TextSpan(0, 0));
            }

            return location;
        }
    }
}
