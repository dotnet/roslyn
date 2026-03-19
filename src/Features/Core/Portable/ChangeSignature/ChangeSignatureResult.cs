// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.ChangeSignature;

internal sealed class ChangeSignatureResult(
    bool succeeded,
    Solution? updatedSolution = null,
    string? name = null,
    Glyph? glyph = null,
    bool previewChanges = false,
    ChangeSignatureFailureKind? changeSignatureFailureKind = null,
    string? confirmationMessage = null)
{
    [MemberNotNullWhen(true, nameof(UpdatedSolution))]
    public bool Succeeded { get; } = succeeded;
    public Solution? UpdatedSolution { get; } = updatedSolution;
    public Glyph? Glyph { get; } = glyph;
    public bool PreviewChanges { get; } = previewChanges;
    public ChangeSignatureFailureKind? ChangeSignatureFailureKind { get; } = changeSignatureFailureKind;
    public string? ConfirmationMessage { get; } = confirmationMessage;

    /// <summary>
    /// Name of the symbol. Needed here for the Preview Changes dialog.
    /// </summary>
    public string? Name { get; } = name;
}
