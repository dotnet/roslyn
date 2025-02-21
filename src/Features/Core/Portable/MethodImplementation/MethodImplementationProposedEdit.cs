// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MethodImplementation;

/// <summary>
/// The individual piece of each method implementation that will eventually be proposed as an edit.
/// E.g. the input validation, the business logic, etc. or arrange, act, assert for tests.
/// </summary>
internal sealed record MethodImplementationProposedEdit
{
    public TextSpan SpanToReplace { get; }

    // May be null if the piece of the comment to document does not have an
    // associated name.
    public string? SymbolName { get; }

    public MethodImplementationTagType TagType { get; }

    public MethodImplementationProposedEdit(TextSpan spanToReplace, string? symbolName, MethodImplementationTagType tagType)
    {
        SpanToReplace = spanToReplace;
        SymbolName = symbolName;
        TagType = tagType;
    }
}
