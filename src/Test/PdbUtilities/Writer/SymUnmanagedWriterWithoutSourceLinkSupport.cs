// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.DiaSymReader;

namespace Roslyn.Test.PdbUtilities
{
    internal class SymUnmanagedWriterWithoutSourceLinkSupport : DelegatingSymUnmanagedWriter
    {
        public SymUnmanagedWriterWithoutSourceLinkSupport(ISymWriterMetadataProvider metadataProvider)
            : base(SymUnmanagedWriterFactory.CreateWriter(metadataProvider))
        {
        }

        public override void SetSourceLinkData(byte[] data)
            => throw new SymUnmanagedWriterException("xxx", new NotSupportedException(), "<lib name>");
    }
}
