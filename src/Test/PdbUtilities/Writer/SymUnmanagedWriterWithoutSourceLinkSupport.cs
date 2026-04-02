// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.DiaSymReader;

namespace Roslyn.Test.PdbUtilities
{
    internal class SymUnmanagedWriterWithoutSourceLinkSupport : DelegatingSymUnmanagedWriter
    {
        public SymUnmanagedWriterWithoutSourceLinkSupport(ISymWriterMetadataProvider metadataProvider)
#pragma warning disable CA1416 // Windows-only API used in test utilities
            : base(SymUnmanagedWriterFactory.CreateWriter(metadataProvider))
#pragma warning restore CA1416
        {
        }

        public override void SetSourceLinkData(byte[] data)
            => throw new SymUnmanagedWriterException("xxx", new NotSupportedException(), "<lib name>");
    }
}
