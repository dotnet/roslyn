// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal sealed class ChangeSignatureResult
    {
        [MemberNotNullWhen(true, nameof(UpdatedSolution))]
        public bool Succeeded { get; }
        public Solution? UpdatedSolution { get; }
        public Glyph? Glyph { get; }
        public bool PreviewChanges { get; }
        public ChangeSignatureFailureKind? ChangeSignatureFailureKind { get; }
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
            ChangeSignatureFailureKind? changeSignatureFailureKind = null,
            string? confirmationMessage = null)
        {
            Succeeded = succeeded;
            UpdatedSolution = updatedSolution;
            Name = name;
            Glyph = glyph;
            PreviewChanges = previewChanges;
            ChangeSignatureFailureKind = changeSignatureFailureKind;
            ConfirmationMessage = confirmationMessage;
        }
    }
}
