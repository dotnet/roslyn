﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.InternalUtilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal class MetadataAndSymbolCache
    {
        // Store 500 entries -- Out of ~8.7M projects, only about 4,000 had more than 500 references
        private const int CacheSize = 500;
        private readonly ConcurrentLruCache<FileKey, Metadata> _metadataCache =
            new ConcurrentLruCache<FileKey, Metadata>(CacheSize);

        private ModuleMetadata CreateModuleMetadata(string path, bool prefetchEntireImage)
        {
            // TODO: exception handling?
            var fileStream = FileUtilities.OpenRead(path);

            var options = PEStreamOptions.PrefetchMetadata;
            if (prefetchEntireImage)
            {
                options |= PEStreamOptions.PrefetchEntireImage;
            }

            return ModuleMetadata.CreateFromStream(fileStream, options);
        }

        private ImmutableArray<ModuleMetadata> GetAllModules(ModuleMetadata manifestModule, string assemblyDir)
        {
            ArrayBuilder<ModuleMetadata>? moduleBuilder = null;

            foreach (string moduleName in manifestModule.GetModuleNames())
            {
                if (moduleBuilder == null)
                {
                    moduleBuilder = ArrayBuilder<ModuleMetadata>.GetInstance();
                    moduleBuilder.Add(manifestModule);
                }

                var module = CreateModuleMetadata(PathUtilities.CombineAbsoluteAndRelativePaths(assemblyDir, moduleName)!, prefetchEntireImage: false);
                moduleBuilder.Add(module);
            }

            return (moduleBuilder != null) ? moduleBuilder.ToImmutableAndFree() : ImmutableArray.Create(manifestModule);
        }

        internal Metadata GetMetadata(string fullPath, MetadataReferenceProperties properties)
        {
            // Check if we have an entry in the dictionary.
            FileKey? fileKey = GetUniqueFileKey(fullPath);

            Metadata? metadata;
            if (fileKey.HasValue && _metadataCache.TryGetValue(fileKey.Value, out metadata) && metadata != null)
            {
                return metadata;
            }

            if (properties.Kind == MetadataImageKind.Module)
            {
                var result = CreateModuleMetadata(fullPath, prefetchEntireImage: true);
                //?? never add modules to cache?
                return result;
            }
            else
            {
                Debug.Assert(fileKey.HasValue);
                var primaryModule = CreateModuleMetadata(fullPath, prefetchEntireImage: false);

                // Get all the modules, and load them. Create an assembly metadata.
                var allModules = GetAllModules(primaryModule, Path.GetDirectoryName(fullPath)!);
                Metadata result = AssemblyMetadata.Create(allModules);

                result = _metadataCache.GetOrAdd(fileKey.Value, result);

                return result;
            }
        }

        /// <summary>
        /// A unique file key encapsulates a file path, and change date
        /// that can be used as the key to a dictionary.
        /// If a file hasn't changed name or timestamp, we assume
        /// it is unchanged.
        /// 
        /// Returns null if the file doesn't exist or otherwise can't be accessed.
        /// </summary>
        private FileKey? GetUniqueFileKey(string filePath)
        {
            try
            {
                return FileKey.Create(filePath);
            }
            catch (Exception)
            {
                // There are several exceptions that can occur here: NotSupportedException or PathTooLongException
                // for a bad path, UnauthorizedAccessException for access denied, etc. Rather than listing them all,
                // just catch all exceptions.
                return null;
            }
        }
    }

    internal sealed class CachingMetadataReference : PortableExecutableReference
    {
        private static readonly MetadataAndSymbolCache s_mdCache = new MetadataAndSymbolCache();

        public new string FilePath { get; }

        public CachingMetadataReference(string fullPath, MetadataReferenceProperties properties)
            : base(properties, fullPath)
        {
            FilePath = fullPath;
        }

        protected override DocumentationProvider CreateDocumentationProvider()
        {
            return DocumentationProvider.Default;
        }

        protected override Metadata GetMetadataImpl()
        {
            return s_mdCache.GetMetadata(FilePath, Properties);
        }

        protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
        {
            return new CachingMetadataReference(this.FilePath, properties);
        }
    }
}
