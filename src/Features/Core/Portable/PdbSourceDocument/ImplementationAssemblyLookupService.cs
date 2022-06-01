// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.PdbSourceDocument
{
    [Export(typeof(IImplementationAssemblyLookupService)), Shared]
    internal class ImplementationAssemblyLookupService : IImplementationAssemblyLookupService
    {
        private readonly Dictionary<string, Dictionary<string, string>?> _typeForwardCache = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ImplementationAssemblyLookupService()
        {
        }

        public bool TryFindImplementationAssemblyPath(string referencedDllPath, [NotNullWhen(true)] out string? implementationDllPath)
        {
            if (TryNugetLibToRef(referencedDllPath, out implementationDllPath))
                return true;

            if (TryTargetingPackToSharedSdk(referencedDllPath, out implementationDllPath))
                return true;

            implementationDllPath = null;
            return false;
        }

        public string? FollowTypeForwards(ISymbol symbol, string dllPath, IPdbSourceDocumentLogger? logger)
        {
            // If we find any type forwards we'll assume they're in the same directory
            var basePath = Path.GetDirectoryName(dllPath);
            if (basePath is null)
                return dllPath;

            // Only the top most containing type in the ExportedType table actually points to an assembly
            // so no point looking for nested types.
            var typeSymbol = MetadataAsSourceHelpers.GetTopLevelContainingNamedType(symbol);

            try
            {
                var typeFullMetadataName = $"{typeSymbol.ContainingNamespace.MetadataName}.{typeSymbol.MetadataName}";
                while (File.Exists(dllPath))
                {
                    // We try to use the cache to avoid loading the file
                    if (_typeForwardCache.TryGetValue(dllPath, out var typeForwards))
                    {
                        if (typeForwards is not null && typeForwards.TryGetValue(typeFullMetadataName, out var cachedAssemblyName))
                        {
                            dllPath = Path.Combine(basePath, $"{cachedAssemblyName}.dll");
                            logger?.Log(FeaturesResources.Symbol_found_in_assembly_path_0, dllPath);
                            continue;
                        }

                        return dllPath;
                    }

                    using var fileStream = File.OpenRead(dllPath);
                    using var reader = new PEReader(fileStream);
                    var md = reader.GetMetadataReader();

                    var assemblyName = GetExportedTypeAssemblyName(md, typeFullMetadataName, dllPath);
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

        public void Clear()
        {
            _typeForwardCache.Clear();
        }

        private static bool TryNugetLibToRef(string referenceDllPath, [NotNullWhen(true)] out string? implementationDllPath)
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

        private static bool TryTargetingPackToSharedSdk(string referencedDllPath, [NotNullWhen(true)] out string? implementationDllPath)
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
            var basePath = Path.GetFullPath(Path.Combine(referencedDllPath, "..", "..", "..", "..", "..", ".."));
            var dllPath = Path.Combine(basePath, "shared", sdkName, packVersion, dllFileName);

            if (IOUtilities.PerformIO(() => File.Exists(dllPath)))
            {
                implementationDllPath = dllPath;
                return true;
            }

            return false;
        }

        private string? GetExportedTypeAssemblyName(MetadataReader md, string typeFullMetadataName, string dllPath)
        {
            Dictionary<string, string>? lazyTypeCache = null;
            string? result = null;
            foreach (var eth in md.ExportedTypes)
            {
                var et = md.GetExportedType(eth);
                if (et.IsForwarder && et.Implementation.Kind == HandleKind.AssemblyReference)
                {
                    var fullMetadataName = $"{md.GetString(et.Namespace)}.{md.GetString(et.Name)}";

                    var ar = md.GetAssemblyReference((AssemblyReferenceHandle)et.Implementation);
                    var assemblyName = md.GetString(ar.Name);

                    lazyTypeCache ??= new();
                    lazyTypeCache[fullMetadataName] = assemblyName;

                    if (fullMetadataName.Equals(typeFullMetadataName, StringComparison.Ordinal))
                    {
                        // Save the result for returning later, but while we have the file open
                        // we will continue to go through and cache all of the type forwards
                        result = assemblyName;
                    }
                }
            }

            _typeForwardCache.Add(dllPath, lazyTypeCache);

            return result;
        }
    }
}
