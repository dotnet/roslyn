// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal enum ConsistencyErrorKind
    {
        UnableToReadFile,
        MissingReference,
        LoadedAssemblyDiffers,
    }

    internal sealed class ConsistencyError
    {
        public ConsistencyErrorKind Kind { get; }
        public string FilePath { get; }
        public string RelatedAssemblyName { get; }
        public Exception Exception { get; }

        public ConsistencyError(ConsistencyErrorKind kind, string filePath, string relatedAssemblyName = null, Exception exception = null)
        {
            Kind = kind;
            FilePath = filePath;
            RelatedAssemblyName = relatedAssemblyName;
            Exception = exception;
        }
    }

    /// <summary>
    /// Validates that a set of analyzer assemblies on disk and their loaded
    /// equivalents are complete and consistent.
    /// 
    /// This type performs two checks:
    /// 1.) It checks that the assembly file on disk and the corresponding loaded
    /// assembly are the same. This is done by comparing the MVIDs values.
    /// 
    /// 2.) It checks that all of an assemblies dependencies are satisified by
    /// other assemblies in the set. E.g., if assembly A depends on assembly B,
    /// B must also be specified. A whitelist can be provided to skip checks for
    /// assemblies that are expected to be provided and loaded by the host, for
    /// example, mscorlib, System.*, and Microsoft.CodeAnalysis.*.
    /// 
    /// Together these rules catch cases where an analyzer may not have all the
    /// dependencies it needs to run correctly, or where it is going to run with
    /// different dependencies than expected.
    /// </summary>
    internal sealed class ConsistencyChecker
    {
        private readonly ImmutableArray<string> _prefixWhiteList;

        public ConsistencyChecker(IEnumerable<string> prefixWhiteList)
        {
            _prefixWhiteList = ImmutableArray.CreateRange(prefixWhiteList);
        }

        public ImmutableArray<ConsistencyError> CheckAssemblies(Dictionary<string, Assembly> pathsToAssemblies, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            ArrayBuilder<ConsistencyError> errors = new ArrayBuilder<ConsistencyError>();

            Dictionary<string, Guid> pathsToMvids = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            HashSet<AssemblyIdentity> identitiesOfAssembliesOnDisk = new HashSet<AssemblyIdentity>();
            Dictionary<string, ImmutableArray<AssemblyIdentity>> pathsToReferences = new Dictionary<string, ImmutableArray<AssemblyIdentity>>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in pathsToAssemblies.Keys)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using (var reader = new PEReader(FileUtilities.OpenRead(path)))
                    {
                        var metadataReader = reader.GetMetadataReader();

                        pathsToMvids[path] = metadataReader.GetModuleVersionIdOrThrow();
                        identitiesOfAssembliesOnDisk.Add(metadataReader.ReadAssemblyIdentityOrThrow());
                        pathsToReferences[path] = metadataReader.GetReferencedAssembliesOrThrow();
                    }
                }
                catch (Exception e)
                {
                    errors.Add(new ConsistencyError(ConsistencyErrorKind.UnableToReadFile, path, exception: e));
                }
            }

            if (errors.Count > 0)
            {
                return errors.ToImmutable();
            }

            foreach (var path in pathsToAssemblies.Keys)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Guid fileMvid = pathsToMvids[path];
                Guid loadedMvid = pathsToAssemblies[path].ManifestModule.ModuleVersionId;

                if (fileMvid != loadedMvid)
                {
                    errors.Add(
                        new ConsistencyError(
                            ConsistencyErrorKind.LoadedAssemblyDiffers,
                            path,
                            pathsToAssemblies[path].FullName));
                }

                ImmutableArray<AssemblyIdentity> references = pathsToReferences[path];
                foreach (var reference in references)
                {
                    if (!identitiesOfAssembliesOnDisk.Contains(reference) &&
                        !_prefixWhiteList.Any(prefix => reference.Name.StartsWith(prefix)))
                    {

                        errors.Add(
                            new ConsistencyError(
                                ConsistencyErrorKind.MissingReference,
                                path,
                                reference.ToString()));
                    }
                }
            }

            return errors.ToImmutable();
        }
    }
}

