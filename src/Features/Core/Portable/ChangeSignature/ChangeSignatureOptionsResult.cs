// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    /// <summary>
    /// A value of null indicates that the operation has been cancelled.
    /// </summary>
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
