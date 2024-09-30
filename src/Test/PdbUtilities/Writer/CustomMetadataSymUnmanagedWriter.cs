// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DiaSymReader;

namespace Roslyn.Test.PdbUtilities;

internal sealed class CustomMetadataSymUnmanagedWriter(SymUnmanagedWriter target, byte[] customMetadata) : DelegatingSymUnmanagedWriter(target)
{
    private readonly byte[] _customMetadata = customMetadata;

    public override void DefineCustomMetadata(byte[] metadata)
    {
        base.DefineCustomMetadata(_customMetadata);
    }
}
