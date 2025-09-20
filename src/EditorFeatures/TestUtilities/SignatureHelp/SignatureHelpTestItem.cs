// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;

public readonly struct SignatureHelpTestItem
{
    /// <summary>
    /// Includes prefix, signature, suffix.
    /// </summary>
    public readonly string? Signature;

    /// <summary>
    /// The method xml documentation.
    /// </summary>
    public readonly string? MethodDocumentation;

    /// <summary>
    /// The (currently selected/expected) parameter documentation. This can be null.
    /// </summary>
    public readonly string? ParameterDocumentation;

    /// <summary>
    /// The currently selected parameter index. For some reason it can be null.
    /// For methods without any parameters, it's still 0 if cursor is between the parentheses, "goo($$)" for example.
    /// </summary>
    public readonly int? CurrentParameterIndex;

    /// <summary>
    /// Description of the method, such as the list of anonymous types: 
    /// Anonymous Types:
    ///     'a is new { string Name, int Age }
    /// </summary>
    public readonly string? Description;

    /// <summary>
    /// Includes prefix, signature, suffix in pretty-printed form (i.e. when the signature wraps).
    /// </summary>
    public readonly string? PrettyPrintedSignature;

    /// <summary>
    /// Whether this item is expected to be selected.
    /// Note: If no item is expected to be selected, the verification of the actual selected item is skipped.
    /// </summary>
    public readonly bool IsSelected;

    /// <summary>
    /// The classification spans for the method documentation
    /// </summary>
    public readonly ImmutableArray<string>? ClassificationTypeNames;

    public SignatureHelpTestItem(
        string? signature,
        string? methodDocumentation = null,
        string? parameterDocumentation = null,
        int? currentParameterIndex = null,
        string? description = null,
        string? prettyPrintedSignature = null,
        bool isSelected = false,
        ImmutableArray<string>? classificationTypeNames = null)
    {
        this.Signature = signature;
        this.MethodDocumentation = methodDocumentation;
        this.ParameterDocumentation = parameterDocumentation;
        this.CurrentParameterIndex = currentParameterIndex;
        this.Description = description;
        this.PrettyPrintedSignature = prettyPrintedSignature;
        this.IsSelected = isSelected;
        this.ClassificationTypeNames = classificationTypeNames;
    }
}
