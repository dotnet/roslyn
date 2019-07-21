// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
