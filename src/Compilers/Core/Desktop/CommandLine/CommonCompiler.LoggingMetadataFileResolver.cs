// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class CommonCompiler
    {
        internal class LoggingMetadataReferencesResolver : MetadataFileReferenceResolver
        {
            protected readonly TouchedFileLogger logger;

            public LoggingMetadataReferencesResolver(ImmutableArray<string> searchPaths, string baseDirectory, TouchedFileLogger logger)
                : base(searchPaths, baseDirectory)
            {
                this.logger = logger;
            }

            protected override bool FileExists(string fullPath)
            {
                if (logger != null && fullPath != null)
                {
                    logger.AddRead(fullPath);
                }

                return base.FileExists(fullPath);
            }
        }
    }
}
