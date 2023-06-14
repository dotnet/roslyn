// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    /// <summary>
    /// A value of null indicates that the operation has been cancelled.
    /// </summary>
    internal sealed class ChangeSignatureOptionsResult(SignatureChange updatedSignature, bool previewChanges)
    {
        public readonly bool PreviewChanges = previewChanges;
        public readonly SignatureChange UpdatedSignature = updatedSignature;
    }
}
