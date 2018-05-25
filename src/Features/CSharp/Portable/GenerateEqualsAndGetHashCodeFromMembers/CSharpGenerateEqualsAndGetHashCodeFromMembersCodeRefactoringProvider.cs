// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers;
using Microsoft.CodeAnalysis.PickMembers;

namespace Microsoft.CodeAnalysis.CSharp.GenerateEqualsAndGetHashCodeFromMembers
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp,
        Name = PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers), Shared]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers,
                    Before = PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers)]
    internal class CSharpGenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider 
        : AbstractGenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider
    {
        public CSharpGenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider() : this(null)
        {
        }

        public CSharpGenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider(IPickMembersService pickMembersService)
            : base(pickMembersService)
        {
        }

        protected override ImmutableArray<SyntaxNode> WrapWithUnchecked(ImmutableArray<SyntaxNode> statements)
            => ImmutableArray.Create<SyntaxNode>(SyntaxFactory.CheckedStatement(SyntaxKind.UncheckedStatement,
                SyntaxFactory.Block(statements.OfType<StatementSyntax>())));
    }
}
