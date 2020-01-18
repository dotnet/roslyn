// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal sealed class ChangeSignatureOptionsResult
    {
        public readonly bool PreviewChanges;
        public readonly SignatureChange UpdatedSignature;

        public ChangeSignatureOptionsResult(SignatureChange updatedSignature, bool previewChanges)
        {
            UpdatedSignature = updatedSignature;
            PreviewChanges = previewChanges;
        }
    }
}
