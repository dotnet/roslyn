// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class CommonCompiler
    {
        /// <summary>
        /// Looks for metadata references among the assembly file references given to the compilation when constructed.
        /// When scripts are included into a project we don't want #r's to reference other assemblies than those 
        /// specified explicitly in the project references.
        /// </summary>
        internal sealed class ExistingReferencesResolver : MetadataFileReferenceResolver
        {
            private readonly MetadataFileReferenceResolver _resolver;
            private readonly ImmutableArray<PortableExecutableReference> _availableReferences;
            private readonly AssemblyIdentityComparer _assemblyIdentityComparer;

            public ExistingReferencesResolver(
                MetadataFileReferenceResolver resolver,
                ImmutableArray<PortableExecutableReference> availableReferences,
                AssemblyIdentityComparer assemblyIdentityComparer)
            {
                Debug.Assert(!availableReferences.Any(r => r.Properties.Kind != MetadataImageKind.Assembly));

                _resolver = resolver;
                _availableReferences = availableReferences;
                _assemblyIdentityComparer = assemblyIdentityComparer;
            }

            public override ImmutableArray<string> SearchPaths
            {
                get { return _resolver.SearchPaths; }
            }

            public override string BaseDirectory
            {
                get { return _resolver.BaseDirectory; }
            }

            internal override MetadataFileReferenceResolver WithSearchPaths(ImmutableArray<string> searchPaths)
            {
                return new ExistingReferencesResolver(_resolver.WithSearchPaths(searchPaths), _availableReferences, _assemblyIdentityComparer);
            }

            internal override MetadataFileReferenceResolver WithBaseDirectory(string baseDirectory)
            {
                return new ExistingReferencesResolver(_resolver.WithBaseDirectory(baseDirectory), _availableReferences, _assemblyIdentityComparer);
            }

            public override string ResolveReference(string reference, string baseFilePath)
            {
                if (PathUtilities.IsFilePath(reference))
                {
                    return ResolveMetadataFile(reference, baseFilePath);
                }
                else
                {
                    return ResolveAssemblyName(reference);
                }
            }

            /// <summary>
            /// When compiling to a file all unresolved assembly names have to match one of the file references specified on command line.
            /// </summary>
            private string ResolveAssemblyName(string displayName)
            {
                foreach (var fileReference in _availableReferences)
                {
                    var identity = ((AssemblyMetadata)fileReference.GetMetadata()).GetAssembly().Identity;
                    if (_assemblyIdentityComparer.ReferenceMatchesDefinition(displayName, identity))
                    {
                        return fileReference.FilePath;
                    }
                }

                return null;
            }

            /// <summary>
            /// When compiling to a file all relative paths have to match one of the file references specified on command line.
            /// </summary>
            private string ResolveMetadataFile(string path, string basePath)
            {
                var fullPath = _resolver.ResolveReference(path, basePath);

                foreach (var fileReference in _availableReferences)
                {
                    if (string.Equals(fileReference.FilePath, fullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return fullPath;
                    }
                }

                return null;
            }
        }
    }
}
