// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security;
using System.Threading;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal sealed class AnalyzerDependencyChecker
    {
        private readonly ImmutableHashSet<string> _analyzerFilePaths;

        private readonly SortedSet<string> _examinedFilePaths = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly SortedSet<string> _filePathsToExamine = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, string> _dependencyPathToAnalyzerPathMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public AnalyzerDependencyChecker(IEnumerable<string> analyzerFilePaths)
        {
            _analyzerFilePaths = analyzerFilePaths.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public ImmutableArray<AnalyzerDependencyConflict> Run(CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var analyzerFilePath in _analyzerFilePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (File.Exists(analyzerFilePath))
                {
                    AddDependenciesToWorkList(analyzerFilePath, analyzerFilePath);
                }
            }

            List<DependencyInfo> dependencies = new List<DependencyInfo>();

            while (_filePathsToExamine.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string filePath = _filePathsToExamine.Min;
                _filePathsToExamine.Remove(filePath);
                _examinedFilePaths.Add(filePath);

                AssemblyIdentity assemblyIdentity = TryReadAssemblyIdentity(filePath);
                if (assemblyIdentity != null)
                {
                    var analyzerPath = _dependencyPathToAnalyzerPathMap[filePath];

                    dependencies.Add(new DependencyInfo(filePath, assemblyIdentity, analyzerPath));

                    AddDependenciesToWorkList(analyzerPath, filePath);
                }
            }

            ImmutableArray<AnalyzerDependencyConflict>.Builder conflicts = ImmutableArray.CreateBuilder<AnalyzerDependencyConflict>();

            foreach (var identityGroup in dependencies.GroupBy(di => di.Identity))
            {
                var identityGroupArray = identityGroup.ToImmutableArray();

                for (int i = 0; i < identityGroupArray.Length; i++)
                {
                    for (int j = i + 1; j < identityGroupArray.Length; j++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        byte[] hash1;
                        byte[] hash2;
                        if ((hash1 = identityGroupArray[i].TryGetFileHash()) != null &&
                            (hash2 = identityGroupArray[j].TryGetFileHash()) != null &&
                            !HashesAreEqual(hash1, hash2))
                        {
                            conflicts.Add(new AnalyzerDependencyConflict(
                                identityGroupArray[i].DependencyFilePath,
                                identityGroupArray[j].DependencyFilePath,
                                identityGroupArray[i].AnalyzerFilePath,
                                identityGroupArray[j].AnalyzerFilePath));
                        }
                    }
                }
            }

            return conflicts.ToImmutable();
        }

        private bool HashesAreEqual(byte[] hash1, byte[] hash2)
        {
            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] != hash2[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void AddDependenciesToWorkList(string analyzerFilePath, string assemblyPath)
        {
            ImmutableArray<string> referencedAssemblyNames = GetReferencedAssemblyNames(assemblyPath);
            foreach (var reference in referencedAssemblyNames)
            {
                string referenceFilePath = Path.Combine(Path.GetDirectoryName(analyzerFilePath), reference + ".dll");

                if (!_examinedFilePaths.Contains(referenceFilePath) &&
                    File.Exists(referenceFilePath))
                {
                    _filePathsToExamine.Add(referenceFilePath);

                    _dependencyPathToAnalyzerPathMap[referenceFilePath] = analyzerFilePath;
                }
            }
        }

        private ImmutableArray<string> GetReferencedAssemblyNames(string assemblyPath)
        {
            try
            {
                using (var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
                using (var peReader = new PEReader(stream))
                {
                    var metadataReader = peReader.GetMetadataReader();

                    var builder = ImmutableArray.CreateBuilder<string>();
                    foreach (var referenceHandle in metadataReader.AssemblyReferences)
                    {
                        var reference = metadataReader.GetAssemblyReference(referenceHandle);

                        builder.Add(metadataReader.GetString(reference.Name));
                    }

                    return builder.ToImmutable();
                }
            }
            catch { }

            return ImmutableArray<string>.Empty;
        }

        private AssemblyIdentity TryReadAssemblyIdentity(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
                using (var peReader = new PEReader(stream))
                {
                    var metadataReader = peReader.GetMetadataReader();

                    string name;
                    Version version;
                    string cultureName;
                    ImmutableArray<byte> publicKeyToken;

                    var assemblyDefinition = metadataReader.GetAssemblyDefinition();
                    name = metadataReader.GetString(assemblyDefinition.Name);
                    version = assemblyDefinition.Version;
                    cultureName = metadataReader.GetString(assemblyDefinition.Culture);
                    publicKeyToken = metadataReader.GetBlobContent(assemblyDefinition.PublicKey);

                    return new AssemblyIdentity(name, version, cultureName, publicKeyToken, hasPublicKey: false);
                }
            }
            catch { }

            return null;
        }

        private sealed class DependencyInfo
        {
            private byte[] _lazyFileHash;
            private bool _triedToComputeFileHash = false;

            public DependencyInfo(string dependencyFilePath, AssemblyIdentity identity, string analyzerFilePath)
            {
                DependencyFilePath = dependencyFilePath;
                Identity = identity;
                AnalyzerFilePath = analyzerFilePath;
            }

            public string AnalyzerFilePath { get; }

            public string DependencyFilePath { get; }

            public AssemblyIdentity Identity { get; }

            public byte[] TryGetFileHash()
            {
                if (!_triedToComputeFileHash)
                {
                    _triedToComputeFileHash = true;

                    _lazyFileHash = TryComputeFileHash(DependencyFilePath);
                }

                return _lazyFileHash;
            }

            private byte[] TryComputeFileHash(string filePath)
            {
                try
                {
                    using (var cryptoProvider = new SHA1CryptoServiceProvider())
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
                    {
                        return cryptoProvider.ComputeHash(stream);
                    }
                }
                catch { }

                return null;
            }
        }
    }
}