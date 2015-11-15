// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal sealed class ChangeSignatureOptionsResult
    {
        public bool IsCancelled { get; set; }
        public bool PreviewChanges { get; internal set; }
        public SignatureChange UpdatedSignature { get; set; }
    }
}
