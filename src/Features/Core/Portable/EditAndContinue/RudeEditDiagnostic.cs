// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue;

[DataContract]
internal readonly struct RudeEditDiagnostic
{
    [DataMember(Order = 0)]
    public readonly RudeEditKind Kind;

    /// <summary>
    /// Span in the new document. May be <c>default</c> if the document (or its entire content) has been deleted.
    /// </summary>
    [DataMember(Order = 1)]
    public readonly TextSpan Span;

    [DataMember(Order = 2)]
    public readonly ushort SyntaxKind;

    [DataMember(Order = 3)]
    public readonly string?[] Arguments;

    internal RudeEditDiagnostic(RudeEditKind kind, TextSpan span, ushort syntaxKind, string?[] arguments)
    {
        Kind = kind;
        Span = span;
        SyntaxKind = syntaxKind;
        Arguments = arguments;
    }

    internal RudeEditDiagnostic(RudeEditKind kind, TextSpan span, SyntaxNode? node = null, string?[]? arguments = null)
        : this(kind, span, (ushort)(node != null ? node.RawKind : 0), arguments ?? [])
    {
    }

    internal Diagnostic ToDiagnostic(SyntaxTree? tree)
    {
        var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(Kind);
        return Diagnostic.Create(descriptor, tree?.GetLocation(Span) ?? Location.None, Arguments);
    }
}
