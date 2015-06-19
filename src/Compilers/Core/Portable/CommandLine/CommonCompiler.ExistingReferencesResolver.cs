// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class CommonCompiler
    {
        /// <summary>
        /// Looks for metadata references among the assembly file references given to the compilation when constructed.
        /// When scripts are included into a project we don't want #r's to reference other assemblies than those 
        /// specified explicitly in the project references.
        /// </summary>
        internal sealed class ExistingReferencesResolver : LoggingMetadataReferencesResolver
        {
            private readonly ImmutableArray<PortableExecutableReference> _availableReferences;
            private readonly AssemblyIdentityComparer _assemblyIdentityComparer;

            public ExistingReferencesResolver(
                ImmutableArray<PortableExecutableReference> availableReferences,
                ImmutableArray<string> referencePaths,
                string baseDirectory,
                AssemblyIdentityComparer assemblyIdentityComparer,
                TouchedFileLogger logger)
                : base(referencePaths, baseDirectory, logger)
            {
                Debug.Assert(!availableReferences.Any(r => r.Properties.Kind != MetadataImageKind.Assembly));

                _availableReferences = availableReferences;
                _assemblyIdentityComparer = assemblyIdentityComparer;
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
                var fullPath = base.ResolveReference(path, basePath);

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
