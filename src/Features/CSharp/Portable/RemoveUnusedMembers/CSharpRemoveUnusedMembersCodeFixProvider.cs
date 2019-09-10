// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.RemoveUnusedMembers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnusedMembers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnusedMembers), Shared]
    internal class CSharpRemoveUnusedMembersCodeFixProvider : AbstractRemoveUnusedMembersCodeFixProvider<FieldDeclarationSyntax>
    {
        [ImportingConstructor]
        public CSharpRemoveUnusedMembersCodeFixProvider()
        {
        }

        /// <summary>
        /// This method adjusts the <paramref name="declarators"/> to remove based on whether or not all variable declarators
        /// within a field declaration should be removed,
        /// i.e. if all the fields declared within a field declaration are unused,
        /// we can remove the entire field declaration instead of individual variable declarators.
        /// </summary>
        protected override void AdjustAndAddAppropriateDeclaratorsToRemove(HashSet<FieldDeclarationSyntax> fieldDeclarators, HashSet<SyntaxNode> declarators)
        {
            foreach (var fieldDeclarator in fieldDeclarators)
            {
                AdjustAndAddAppropriateDeclaratorsToRemove(fieldDeclarator, fieldDeclarator.Declaration.Variables, declarators);
            }
        }
    }
}
