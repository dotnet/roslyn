// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

internal sealed class RuntimeRudeEditDescription(int markerId, RudeEditKind kind, LinePosition position, string[] arguments)
{
    public int MarkerId { get; } = markerId;

    public string GetMessage(SyntaxTree tree)
        => new RudeEditDiagnostic(kind, tree.GetText().Lines.GetTextSpan(new LinePositionSpan(position, position)), syntaxKind: 0, arguments).ToDiagnostic(tree).ToString();
}
