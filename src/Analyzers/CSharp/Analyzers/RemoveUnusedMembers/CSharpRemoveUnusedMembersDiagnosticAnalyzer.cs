// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.RemoveUnusedMembers;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnusedMembers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpRemoveUnusedMembersDiagnosticAnalyzer
        : AbstractRemoveUnusedMembersDiagnosticAnalyzer<DocumentationCommentTriviaSyntax, IdentifierNameSyntax>
    {
    }
}
