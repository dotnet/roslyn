﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.RemoveUnusedMembers;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnusedMembers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpRemoveUnusedMembersDiagnosticAnalyzer
        : AbstractRemoveUnusedMembersDiagnosticAnalyzer<
            DocumentationCommentTriviaSyntax,
            IdentifierNameSyntax,
            TypeDeclarationSyntax,
            MemberDeclarationSyntax>
    {
        protected override IEnumerable<TypeDeclarationSyntax> GetTypeDeclarations(INamedTypeSymbol namedType, CancellationToken cancellationToken)
        {
            return namedType.DeclaringSyntaxReferences
                .Select(r => r.GetSyntax(cancellationToken))
                .OfType<TypeDeclarationSyntax>();
        }

        protected override SyntaxList<MemberDeclarationSyntax> GetMembers(TypeDeclarationSyntax typeDeclaration)
            => typeDeclaration.Members;
    }
}
