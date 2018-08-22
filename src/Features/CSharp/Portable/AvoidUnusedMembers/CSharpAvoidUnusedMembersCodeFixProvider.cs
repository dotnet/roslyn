// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.AvoidUnusedMembers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.AvoidUnusedMembers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AvoidUnusedMembers), Shared]
    internal class CSharpAvoidUnusedMembersCodeFixProvider : AbstractAvoidUnusedMembersCodeFixProvider<FieldDeclarationSyntax>
    {
        protected override void AdjustDeclarators(HashSet<FieldDeclarationSyntax> fieldDeclarators, HashSet<SyntaxNode> declarators)
        {
            foreach (var fieldDeclarator in fieldDeclarators)
            {
                AdjustChildDeclarators(fieldDeclarator, fieldDeclarator.Declaration.Variables, declarators);
            }
        }
    }
}
