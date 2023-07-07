﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.GenerateFromMembers
{
    internal abstract partial class AbstractGenerateFromMembersCodeRefactoringProvider : CodeRefactoringProvider
    {
        protected class SelectedMemberInfo
        {
            public readonly INamedTypeSymbol ContainingType;
            public readonly ImmutableArray<SyntaxNode> SelectedDeclarations;
            public readonly ImmutableArray<ISymbol> SelectedMembers;

            public SelectedMemberInfo(
                INamedTypeSymbol containingType,
                ImmutableArray<SyntaxNode> selectedDeclarations,
                ImmutableArray<ISymbol> selectedMembers)
            {
                ContainingType = containingType;
                SelectedDeclarations = selectedDeclarations;
                SelectedMembers = selectedMembers;
            }
        }
    }
}
