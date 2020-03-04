// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal sealed class ChangeSignatureOptionsResult
    {
        public bool IsCancelled { get; set; }
        public bool PreviewChanges { get; internal set; }
        public SignatureChange UpdatedSignature { get; set; }
    }
}
