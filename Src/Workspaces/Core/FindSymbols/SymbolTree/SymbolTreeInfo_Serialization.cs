// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Versions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo : IObjectWritable
    {
        private const string ProjectSymbolTreeInfoPersistenceName = "<ProjectSymbolTreeInfoPersistence>";
        private const string PrefixMetadataSymbolTreeInfo = "<MetadataSymbolTreeInfoPersistence>_";
        private const string SerializationFormat = "1";

        /// <summary>
        /// this is for a project in a solution
        /// </summary>
        private static async Task<ValueTuple<bool, SymbolTreeInfo>> LoadOrCreateAsync(Project project, CancellationToken cancellationToken)
        {
            if (await project.IsForkedProjectWithSemanticChangesAsync(cancellationToken).ConfigureAwait(false))
            {
                return ValueTuple.Create(false, await CreateAsync(project, cancellationToken).ConfigureAwait(false));
            }

            var persistentStorageService = project.Solution.Workspace.Services.GetService<IPersistentStorageService>();

            var projectVersion = await project.GetVersionAsync(cancellationToken).ConfigureAwait(false);
            var semanticVersion = await project.GetSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

            // attempt to load from persisted state
            SymbolTreeInfo info;
            var succeeded = false;
            using (var storage = persistentStorageService.GetStorage(project.Solution))
            {
                using (var stream = await storage.ReadStreamAsync(project, ProjectSymbolTreeInfoPersistenceName, cancellationToken).ConfigureAwait(false))
                {
                    if (stream != null)
                    {
                        using (var reader = new ObjectReader(stream))
                        {
                            info = ReadFrom(reader);
                            if (info != null && project.CanReusePersistedSemanticVersion(projectVersion, semanticVersion, info.version))
                            {
                                return ValueTuple.Create(true, info);
                            }
                        }
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // compute it if we couldn't load it from cache
                info = await CreateAsync(project, cancellationToken).ConfigureAwait(false);
                if (info != null)
                {
                    using (var stream = SerializableBytes.CreateWritableStream())
                    using (var writer = new ObjectWriter(stream, cancellationToken: cancellationToken))
                    {
                        info.WriteTo(writer);
                        stream.Position = 0;

                        succeeded = await storage.WriteStreamAsync(project, ProjectSymbolTreeInfoPersistenceName, stream, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return ValueTuple.Create(succeeded, info);
        }

        /// <summary>
        /// this is for a metadata reference in a solution
        /// </summary>
        private static async Task<SymbolTreeInfo> LoadOrCreateAsync(Solution solution, IAssemblySymbol assembly, string filePath, CancellationToken cancellationToken)
        {
            // if assembly is not from a file, just create one on the fly
            if (filePath == null || !File.Exists(filePath) || !FilePathUtilities.PartOfFrameworkOrReferencePaths(filePath))
            {
                return Create(VersionStamp.Default, assembly, cancellationToken);
            }

            // if solution is not from a disk, just create one.
            if (solution.FilePath == null || !File.Exists(solution.FilePath))
            {
                return Create(VersionStamp.Default, assembly, cancellationToken);
            }

            // okay, see whether we can get one from persistence service.
            var relativePath = FilePathUtilities.GetRelativePath(solution.FilePath, filePath);
            var version = VersionStamp.Create(File.GetLastWriteTimeUtc(filePath));

            var persistentStorageService = solution.Workspace.Services.GetService<IPersistentStorageService>();

            // attempt to load from persisted state. metadata reference is solution wise information
            SymbolTreeInfo info;
            using (var storage = persistentStorageService.GetStorage(solution))
            {
                var key = PrefixMetadataSymbolTreeInfo + relativePath;
                using (var stream = await storage.ReadStreamAsync(key, cancellationToken).ConfigureAwait(false))
                {
                    if (stream != null)
                    {
                        using (var reader = new ObjectReader(stream))
                        {
                            info = ReadFrom(reader);
                            if (info != null && VersionStamp.CanReusePersistedVersion(version, info.version))
                            {
                                return info;
                            }
                        }
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // compute it if we couldn't load it from cache
                info = Create(version, assembly, cancellationToken);
                if (info != null)
                {
                    using (var stream = SerializableBytes.CreateWritableStream())
                    using (var writer = new ObjectWriter(stream, cancellationToken: cancellationToken))
                    {
                        info.WriteTo(writer);
                        stream.Position = 0;

                        await storage.WriteStreamAsync(key, stream, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return info;
        }

        private static Task<SymbolTreeInfo> CreateAsync(IAssemblySymbol assembly, CancellationToken cancellationToken)
        {
            // TODO delete this method soon.
            return Task.FromResult(Create(VersionStamp.Default, assembly, cancellationToken));
        }

        private static async Task<SymbolTreeInfo> CreateAsync(Project project, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var version = await project.GetSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

            return Create(version, compilation.Assembly, cancellationToken);
        }

        public void WriteTo(ObjectWriter writer)
        {
            writer.WriteString(SerializationFormat);
            this.version.WriteTo(writer);

            writer.WriteInt32(this.nodes.Count);
            foreach (var node in this.nodes)
            {
                writer.WriteString(node.Name);
                writer.WriteInt32(node.ParentIndex);
            }
        }

        internal static SymbolTreeInfo ReadFrom(ObjectReader reader)
        {
            try
            {
                var formatVersion = reader.ReadString();
                if (!string.Equals(formatVersion, SerializationFormat, StringComparison.Ordinal))
                {
                    return null;
                }

                var version = VersionStamp.ReadFrom(reader);

                var count = reader.ReadInt32();
                if (count == 0)
                {
                    return new SymbolTreeInfo(version, ImmutableArray<Node>.Empty);
                }

                var nodes = new Node[count];
                for (var i = 0; i < count; i++)
                {
                    var name = reader.ReadString();
                    var parentIndex = reader.ReadInt32();

                    nodes[i] = new Node(name, parentIndex);
                }

                return new SymbolTreeInfo(version, nodes);
            }
            catch (Exception)
            {
            }

            return null;
        }
    }
}