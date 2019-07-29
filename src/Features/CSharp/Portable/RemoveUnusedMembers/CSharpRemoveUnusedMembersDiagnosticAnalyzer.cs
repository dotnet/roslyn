// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
