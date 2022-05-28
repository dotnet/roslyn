// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.CodeAnalysis.PdbSourceDocument;
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

        /// <summary>
        /// Uses various heuristics to try to find the implementation assembly for a reference assembly without
        /// loading 
        /// </summary>
        public static bool TryFindImplementationAssemblyPath(string referencedDllPath, [NotNullWhen(true)] out string? implementationDllPath)
        {
            if (TryNugetLibToRef(referencedDllPath, out implementationDllPath))
                return true;

            if (TryTargetingPackToSharedSdk(referencedDllPath, out implementationDllPath))
                return true;

            implementationDllPath = null;
            return false;

            static bool TryNugetLibToRef(string referenceDllPath, [NotNullWhen(true)] out string? implementationDllPath)
            {
                implementationDllPath = null;

                // For some nuget packages if the reference path has a "ref" folder in it, then the implementation assembly
                // will be in the corresponding "lib" folder.
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

            static bool TryTargetingPackToSharedSdk(string referencedDllPath, [NotNullWhen(true)] out string? implementationDllPath)
            {
                implementationDllPath = null;

                // eg. C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\6.0.5\ref\net6.0\Foo.dll
                var parts = referencedDllPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (parts is not [.., "packs", var packName, var packVersion, "ref", _, var dllFileName])
                    return false;

                // We try to get the shared sdk name from the FrameworkList.xml file, in the data dir
                // eg. C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\6.0.5\data\FrameworkList.xml
                var frameworkXml = Path.Combine(referencedDllPath, "..", "..", "..", "data", "FrameworkList.xml");

                string sdkName;
                try
                {
                    using var fr = File.OpenRead(frameworkXml);
                    using var xr = XmlReader.Create(fr);
                    xr.Read();
                    sdkName = xr.GetAttribute("FrameworkName");
                }
                catch
                {
                    // This could be a file read error, or XML error, but we don't really care, as we're only trying to
                    // use a heuristic to provide better results, we don't have to be super resiliant to all things.
                    return false;
                }

                // If it exists, the implementation dll will be in the shared sdk folder for this pack
                // eg. C:\Program Files\dotnet\shared\Microsoft.NETCore.App\6.0.5\Foo.dll
                // But first we go up six levels to get to the common root. The pattern match above
                // ensures this will be valid.
                var basePath = Path.Combine(referencedDllPath, "..", "..", "..", "..", "..", "..");
                var dllPath = Path.Combine(basePath, "shared", sdkName, packVersion, dllFileName);

                if (IOUtilities.PerformIO(() => File.Exists(dllPath)))
                {
                    implementationDllPath = dllPath;
                    return true;
                }

                return false;
            }
        }

        public static bool IsReferenceAssembly(IAssemblySymbol assemblySymbol)
        {
            foreach (var attribute in assemblySymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.Name == nameof(ReferenceAssemblyAttribute) &&
                    attribute.AttributeClass.ToNameDisplayString() == typeof(ReferenceAssemblyAttribute).FullName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Given an implementation assembly path, follows any type forwards that might be in place
        /// for the containing type of <paramref name="symbol"/>, to ensure the right implementation
        /// assembly will be found.
        /// </summary>
        public static string? FollowTypeForwards(ISymbol symbol, string dllPath, IPdbSourceDocumentLogger? logger)
        {
            // If we find any type forwards we'll assume they're in the same directory
            var basePath = Path.GetDirectoryName(dllPath);
            if (basePath is null)
                return dllPath;

            // Only the top most containing type in the ExportedType table actually points to an assembly
            // so no point looking for nested types.
            var typeSymbol = GetTopLevelContainingNamedType(symbol);

            try
            {
                while (File.Exists(dllPath))
                {
                    using var fileStream = File.OpenRead(dllPath);
                    using var reader = new PEReader(fileStream);
                    var md = reader.GetMetadataReader();

                    var assemblyName = GetExportedTypeAssemblyName(md, typeSymbol);
                    if (assemblyName is null)
                    {
                        // Didn't find a type forward, so the current DLL is the right one to use
                        return dllPath;
                    }

                    dllPath = Path.Combine(basePath, $"{assemblyName}.dll");
                    logger?.Log(FeaturesResources.Symbol_found_in_assembly_path_0, dllPath);
                }
            }
            catch (Exception ex) when (IOUtilities.IsNormalIOException(ex))
            {
            }

            return null;
        }

        private static string? GetExportedTypeAssemblyName(MetadataReader md, INamedTypeSymbol typeSymbol)
        {
            foreach (var eth in md.ExportedTypes)
            {
                var et = md.GetExportedType(eth);
                if (!et.IsForwarder)
                    continue;

                if (md.StringComparer.Equals(et.Name, typeSymbol.MetadataName) &&
                    md.StringComparer.Equals(et.Namespace, typeSymbol.ContainingNamespace.MetadataName))
                {
                    var handle = et.Implementation;
                    if (handle.Kind == HandleKind.AssemblyReference)
                    {
                        var ar = md.GetAssemblyReference((AssemblyReferenceHandle)handle);
                        return md.GetString(ar.Name);
                    }

                    break;
                }
            }

            return null;
        }
    }
}
