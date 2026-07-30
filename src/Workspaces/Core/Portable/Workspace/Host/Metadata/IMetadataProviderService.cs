// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host;

internal readonly record struct MetadataProviderResult(Metadata Metadata, bool IsCacheable);

internal interface IMetadataProviderService : IWorkspaceService
{
    MetadataProviderResult GetMetadata(string resolvedPath, MetadataImageKind kind);
}

internal abstract class AbstractMetadataProviderService : IMetadataProviderService
{
    public virtual MetadataProviderResult GetMetadata(string resolvedPath, MetadataImageKind kind)
    {
        var module = ModuleMetadata.CreateFromStream(OpenRead(resolvedPath), PEStreamOptions.PrefetchEntireImage);

        if (kind == MetadataImageKind.Module)
            return new(module, IsCacheable: true);

        try
        {
            try
            {
                if (module.GetModuleNames().IsEmpty)
                    return new(AssemblyMetadata.Create(module), IsCacheable: true);
            }
            catch (BadImageFormatException)
            {
                // Preserve the normal reference behavior of reporting malformed module names
                // when the compilation consumes the metadata rather than while creating the reference.
                module.Dispose();
                return new(MetadataReference.CreateFromFile(resolvedPath).GetMetadata(), IsCacheable: false);
            }

            // A manifest-only key cannot detect changes to secondary modules, so do not share
            // multi-module assemblies until all constituent modules participate in the key.
            module.Dispose();
            return new(MetadataReference.CreateFromFile(resolvedPath).GetMetadata(), IsCacheable: false);
        }
        catch
        {
            module.Dispose();
            throw;
        }
    }

    private static Stream OpenRead(string resolvedPath)
    {
        try
        {
            return new FileStream(resolvedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (DirectoryNotFoundException e)
        {
            throw new FileNotFoundException(e.Message, resolvedPath, e);
        }
        catch (IOException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new IOException(e.Message, e);
        }
    }
}

[ExportWorkspaceService(typeof(IMetadataProviderService), ServiceLayer.Default), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultMetadataProviderService() : AbstractMetadataProviderService;
