// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.RemoveUnusedMembers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnusedMembers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnusedMembers), Shared]
    internal class CSharpRemoveUnusedMembersCodeFixProvider : AbstractRemoveUnusedMembersCodeFixProvider<FieldDeclarationSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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
