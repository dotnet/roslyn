// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host
{
    internal partial class TextFactoryServiceFactory
    {
        public class TextFactoryService : ITextFactoryService
        {
            public SourceText CreateText(Stream stream, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new EncodedStringText(stream, encodingOpt: null);
            }

            public SourceText CreateText(Stream stream, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new EncodedStringText(stream, encoding);
            }
        }
    }
}