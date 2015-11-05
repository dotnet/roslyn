// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.InternalUtilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal class MetadataAndSymbolCache
    {
        // Store 100 entries -- arbitrary number
        private const int CacheSize = 100;
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
            ArrayBuilder<ModuleMetadata> moduleBuilder = null;

            foreach (string moduleName in manifestModule.GetModuleNames())
            {
                if (moduleBuilder == null)
                {
                    moduleBuilder = ArrayBuilder<ModuleMetadata>.GetInstance();
                    moduleBuilder.Add(manifestModule);
                }

                var module = CreateModuleMetadata(PathUtilities.CombineAbsoluteAndRelativePaths(assemblyDir, moduleName), prefetchEntireImage: false);
                moduleBuilder.Add(module);
            }

            return (moduleBuilder != null) ? moduleBuilder.ToImmutableAndFree() : ImmutableArray.Create(manifestModule);
        }

        internal Metadata GetMetadata(string fullPath, MetadataReferenceProperties properties)
        {
            // Check if we have an entry in the dictionary.
            FileKey? fileKey = GetUniqueFileKey(fullPath);

            Metadata metadata;
            if (fileKey.HasValue && _metadataCache.TryGetValue(fileKey.Value, out metadata) && metadata != null)
            {
                CompilerServerLogger.Log("Using already loaded metadata for assembly reference '{0}'", fileKey);
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
                var primaryModule = CreateModuleMetadata(fullPath, prefetchEntireImage: false);

                // Get all the modules, and load them. Create an assembly metadata.
                var allModules = GetAllModules(primaryModule, Path.GetDirectoryName(fullPath));
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
            FileInfo fileInfo;

            try
            {
                fileInfo = new FileInfo(filePath);

                if (!fileInfo.Exists)
                {
                    return null;
                }
                else
                {
                    return new FileKey(fileInfo.FullName, fileInfo.LastWriteTimeUtc);
                }
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

    internal class CachingMetadataReference : PortableExecutableReference
    {
        private static readonly MetadataAndSymbolCache s_mdCache = new MetadataAndSymbolCache();

        internal CachingMetadataReference(string fullPath, MetadataReferenceProperties properties)
            : base(properties, fullPath)
        {
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
