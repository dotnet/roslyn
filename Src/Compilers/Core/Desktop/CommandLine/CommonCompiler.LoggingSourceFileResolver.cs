// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Instrumentation;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class CommonCompiler
    {
        internal sealed class LoggingSourceFileResolver : SourceFileResolver
        {
            private readonly TouchedFileLogger logger;

            public LoggingSourceFileResolver(ImmutableArray<string> searchPaths, string baseDirectory, TouchedFileLogger logger)
                : base(searchPaths, baseDirectory)
            {
                this.logger = logger;
            }

            protected override bool FileExists(string fullPath)
            {
                if (logger != null && fullPath != null)
                {
                    this.logger.AddRead(fullPath);
                }

                return base.FileExists(fullPath);
            }
        }
    }
}
