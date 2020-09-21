// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal sealed class ChangeSignatureResult
    {
        public bool Succeeded { get; }
        public Solution? UpdatedSolution { get; }
        public Glyph? Glyph { get; }
        public bool PreviewChanges { get; }
        public CannotChangeSignatureReason? CannotChangeSignatureReason { get; }
        public string? ConfirmationMessage { get; }

        /// <summary>
        /// Name of the symbol. Needed here for the Preview Changes dialog.
        /// </summary>
        public string? Name { get; }

        public ChangeSignatureResult(
            bool succeeded,
            Solution? updatedSolution = null,
            string? name = null,
            Glyph? glyph = null,
            bool previewChanges = false,
            CannotChangeSignatureReason? cannotChangeSignatureReason = null,
            string? confirmationMessage = null)
        {
            Succeeded = succeeded;
            UpdatedSolution = updatedSolution;
            Name = name;
            Glyph = glyph;
            PreviewChanges = previewChanges;
            CannotChangeSignatureReason = cannotChangeSignatureReason;
            ConfirmationMessage = confirmationMessage;
        }
    }
}
