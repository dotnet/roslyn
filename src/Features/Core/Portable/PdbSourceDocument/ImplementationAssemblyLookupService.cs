// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.PdbSourceDocument;

[Export(typeof(IImplementationAssemblyLookupService)), Shared]
internal sealed class ImplementationAssemblyLookupService : IImplementationAssemblyLookupService
{
    // We need to generate the namespace name in the same format that is used in metadata, which
    // is SymbolDisplayFormat.QualifiedNameOnlyFormat, which this is a copy of.
    private static readonly SymbolDisplayFormat s_metadataSymbolDisplayFormat = new(
                    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    private static readonly string PathSeparatorString = Path.DirectorySeparatorChar.ToString();

    // Cache for any type forwards. Key is the dll being inspected. Value is a dictionary
    // of namespace and type name, to the assembly name that the type is forwarded to
    private readonly Dictionary<string, Dictionary<(string @namespace, string typeName), string>?> _typeForwardCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _cacheLock = new();

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ImplementationAssemblyLookupService()
    {
    }

    public bool TryFindImplementationAssemblyPath(string referencedDllPath, [NotNullWhen(true)] out string? implementationDllPath)
    {
        var pathParts = referencedDllPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (TryNugetLibToRef(pathParts, out implementationDllPath))
            return true;

        if (TryProjectParentToRef(pathParts, out implementationDllPath))
            return true;

        if (TryTargetingPackToSharedSdk(pathParts, out implementationDllPath))
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
        var namespaceName = typeSymbol.ContainingNamespace.ToDisplayString(s_metadataSymbolDisplayFormat);

        try
        {
            while (File.Exists(dllPath))
            {
                // We try to use the cache to avoid loading the file
                if (TryGetCachedTypeForwards(dllPath, out var typeForwards))
                {
                    // If there are no type forwards in this DLL, or not one for this type, then it means
                    // we've found the right DLL
                    if (typeForwards?.TryGetValue((namespaceName, typeSymbol.MetadataName), out var assemblyName) != true)
                    {
                        return dllPath;
                    }

                    dllPath = Path.Combine(basePath, $"{assemblyName}.dll");
                    logger?.Log(FeaturesResources.Symbol_found_in_assembly_path_0, dllPath);

                    continue;
                }

                // If this dll wasn't in the cache, then populate the cache and try again
                using var fileStream = File.OpenRead(dllPath);
                using var reader = new PEReader(fileStream);
                var md = reader.GetMetadataReader();
                var cachedTypeForwards = GetAllTypeForwards(md);

                lock (_cacheLock)
                {
                    _typeForwardCache.Add(dllPath, cachedTypeForwards);
                }
            }
        }
        catch (Exception ex) when (IOUtilities.IsNormalIOException(ex))
        {
        }

        return null;

        bool TryGetCachedTypeForwards(string dllPath, [NotNullWhen(true)] out Dictionary<(string @namespace, string typeName), string>? typeForwards)
        {
            lock (_cacheLock)
            {
                return _typeForwardCache.TryGetValue(dllPath, out typeForwards);
            }
        }
    }

    public void Clear()
    {
        lock (_cacheLock)
        {
            _typeForwardCache.Clear();
        }
    }

    private static bool TryNugetLibToRef(string[] pathParts, [NotNullWhen(true)] out string? implementationDllPath)
    {
        implementationDllPath = null;

        // For some nuget packages if the reference path has a "ref" folder in it, then the implementation assembly
        // will be in the corresponding "lib" folder.
        var refIndex = Array.LastIndexOf(pathParts, "ref");
        if (refIndex == -1)
            return false;

        var pathToTry = Path.Combine(
                            string.Join(PathSeparatorString, pathParts, 0, refIndex),
                            "lib",
                            string.Join(PathSeparatorString, pathParts, refIndex + 1, pathParts.Length - refIndex - 1));

        if (IOUtilities.PerformIO(() => File.Exists(pathToTry)))
        {
            implementationDllPath = pathToTry;
            return true;
        }

        return false;
    }

    private static bool TryProjectParentToRef(string[] pathParts, [NotNullWhen(true)] out string? implementationDllPath)
    {
        implementationDllPath = null;

        // For projects if the reference path has a "ref" last folder, then the implementation assembly
        // will be in the corresponding parent folder.
        if (pathParts.Length < 2 || pathParts[^2] != "ref")
            return false;

        var pathToTry = Path.Combine(
                            string.Join(PathSeparatorString, pathParts, 0, pathParts.Length - 2),
                            pathParts[^1]);

        if (IOUtilities.PerformIO(() => File.Exists(pathToTry)))
        {
            implementationDllPath = pathToTry;
            return true;
        }

        return false;
    }

    private static bool TryTargetingPackToSharedSdk(string[] pathParts, [NotNullWhen(true)] out string? implementationDllPath)
    {
        implementationDllPath = null;
        if (pathParts is not [.., "packs", var packName, var packVersion, "ref", _, var dllFileName])
            return false;

        var referencedDllPath = string.Join(PathSeparatorString, pathParts);

        // We try to get the shared sdk name from the FrameworkList.xml file, in the data dir
        // eg. C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\6.0.5\data\FrameworkList.xml
        var frameworkXml = Path.Combine(referencedDllPath, "..", "..", "..", "data", "FrameworkList.xml");

        string? sdkName;
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

        if (sdkName is null)
            return false;

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

    private static Dictionary<(string, string), string>? GetAllTypeForwards(MetadataReader md)
    {
        EntityHandle lastAssemblyReferenceHandle = default;
        string? assemblyName = null;

        Dictionary<(string, string), string>? result = null;
        foreach (var eth in md.ExportedTypes)
        {
            var et = md.GetExportedType(eth);
            if (et.IsForwarder && et.Implementation.Kind == HandleKind.AssemblyReference)
            {
                if (!et.Implementation.Equals(lastAssemblyReferenceHandle))
                {
                    lastAssemblyReferenceHandle = et.Implementation;
                    var assemblyReference = md.GetAssemblyReference((AssemblyReferenceHandle)lastAssemblyReferenceHandle);
                    assemblyName = md.GetString(assemblyReference.Name);
                }

                Debug.Assert(assemblyName is not null);

                var foundNamespace = md.GetString(et.Namespace);
                var foundTypeName = md.GetString(et.Name);

                result ??= [];
                result.Add((foundNamespace, foundTypeName), assemblyName);
            }
        }

        return result;
    }
}
