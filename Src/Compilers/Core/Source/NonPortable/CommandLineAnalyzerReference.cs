// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Describes a command line analyzer assembly specification.
    /// </summary>
    public struct CommandLineAnalyzerReference
    {
        private readonly string reference;

        public CommandLineAnalyzerReference(string reference)
        {
            this.reference = reference;
        }

        /// <summary>
        /// Assembly file path.
        /// </summary>
        public string FilePath
        {
            get
            {
                return reference;
            }
        }

        internal static ImmutableArray<IDiagnosticAnalyzer> ResolveAndGetAnalyzers(ImmutableArray<CommandLineAnalyzerReference> analyzerCommandLineReferences, MetadataFileReferenceResolver metadataResolver, List<DiagnosticInfo> diagnostics, CommonMessageProvider messageProvider)
        {
            var builder = ImmutableArray.CreateBuilder<IDiagnosticAnalyzer>();

            foreach (var analyzerCommandLineReference in analyzerCommandLineReferences)
            {
                var analyzerReference = analyzerCommandLineReference.Resolve(metadataResolver, diagnostics, messageProvider);
                if (!analyzerReference.IsUnresolved)
                {
                    var resolverAnalyzerReference = (AnalyzerFileReference)analyzerReference;
                    resolverAnalyzerReference.AddAnalyzers(builder, diagnostics, messageProvider);
                }
            }

            return builder.ToImmutable();
        }

        internal AnalyzerReference Resolve(MetadataFileReferenceResolver metadataResolver, List<DiagnosticInfo> diagnosticsOpt, CommonMessageProvider messageProviderOpt)
        {
            Debug.Assert(metadataResolver != null);

            // use search paths and base path of the resolver - usually these are the same as the paths stored on the arguments:
            string fullPath = metadataResolver.ResolveMetadataFileChecked(this.reference, baseFilePath: null);
            if (fullPath == null)
            {
                if (diagnosticsOpt != null && messageProviderOpt != null)
                {
                    diagnosticsOpt.Add(new DiagnosticInfo(messageProviderOpt, messageProviderOpt.ERR_MetadataFileNotFound, this.reference));
                }

                return new UnresolvedAnalyzerReference(this.reference);
            }
            else
            {
                return new AnalyzerFileReference(fullPath);
            }
        }
    }
}
