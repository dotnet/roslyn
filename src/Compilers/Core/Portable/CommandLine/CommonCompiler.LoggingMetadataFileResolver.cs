// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class CommonCompiler
    {
        internal sealed class LoggingMetadataReferencesResolver : MetadataFileReferenceResolver
        {
            private readonly MetadataFileReferenceResolver _resolver;
            private readonly TouchedFileLogger _logger;

            public LoggingMetadataReferencesResolver(MetadataFileReferenceResolver resolver, TouchedFileLogger logger)
            {
                Debug.Assert(logger != null);
                _resolver = resolver;
                _logger = logger;
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
                return new LoggingMetadataReferencesResolver(_resolver.WithSearchPaths(searchPaths), _logger);
            }

            internal override MetadataFileReferenceResolver WithBaseDirectory(string baseDirectory)
            {
                return new LoggingMetadataReferencesResolver(_resolver.WithBaseDirectory(baseDirectory), _logger);
            }

            public override string ResolveReference(string reference, string baseFilePath)
            {
                var path = _resolver.ResolveReference(reference, baseFilePath);
                if (path != null)
                {
                    _logger.AddRead(path);
                }
                return path;
            }
        }
    }
}
