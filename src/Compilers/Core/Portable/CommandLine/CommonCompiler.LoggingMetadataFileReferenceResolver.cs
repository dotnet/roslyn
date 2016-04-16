// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class CommonCompiler
    {
        internal sealed class LoggingMetadataFileReferenceResolver : MetadataReferenceResolver, IEquatable<LoggingMetadataFileReferenceResolver>
        {
            private readonly TouchedFileLogger _loggerOpt;
            private readonly RelativePathResolver _pathResolver;
            private readonly Func<string, MetadataReferenceProperties, PortableExecutableReference> _provider;

            public LoggingMetadataFileReferenceResolver(RelativePathResolver pathResolver, Func<string, MetadataReferenceProperties, PortableExecutableReference> provider, TouchedFileLogger loggerOpt)
            {
                Debug.Assert(pathResolver != null);
                Debug.Assert(provider != null);

                _pathResolver = pathResolver;
                _provider = provider;
                _loggerOpt = loggerOpt;
            }

            public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
            {
                string fullPath = _pathResolver.ResolvePath(reference, baseFilePath);

                if (fullPath != null)
                {
                    _loggerOpt?.AddRead(fullPath);
                    return ImmutableArray.Create(_provider(fullPath, properties));
                }

                return ImmutableArray<PortableExecutableReference>.Empty;
            }

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }

            public bool Equals(LoggingMetadataFileReferenceResolver other)
            {
                throw new NotImplementedException();
            }

            public override bool Equals(object other) => Equals(other as LoggingMetadataFileReferenceResolver);
        }
    }
}
