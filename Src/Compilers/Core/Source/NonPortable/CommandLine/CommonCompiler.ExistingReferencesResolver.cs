// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Instrumentation;
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
        internal sealed class ExistingReferencesResolver : LoggingMetadataReferencesResolver
        {
            private readonly ImmutableArray<MetadataFileReference> availableReferences;
            private readonly AssemblyIdentityComparer assemblyIdentityComparer;

            public ExistingReferencesResolver(
                ImmutableArray<MetadataFileReference> availableReferences,
                ImmutableArray<string> referencePaths,
                string baseDirectory,
                AssemblyIdentityComparer assemblyIdentityComparer,
                TouchedFileLogger logger)
                : base(referencePaths, baseDirectory, logger)
            {
                Debug.Assert(!availableReferences.Any(r => r.Properties.Kind != MetadataImageKind.Assembly));

                this.availableReferences = availableReferences;
                this.assemblyIdentityComparer = assemblyIdentityComparer;
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
                foreach (var fileReference in availableReferences)
                {
                    var identity = ((AssemblyMetadata)fileReference.GetMetadata()).Assembly.Identity;
                    if (assemblyIdentityComparer.ReferenceMatchesDefinition(displayName, identity))
                    {
                        return fileReference.FullPath;
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

                foreach (var fileReference in availableReferences)
                {
                    if (string.Equals(fileReference.FullPath, fullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return fullPath;
                    }
                }

                return null;
            }
        }
    }
}
