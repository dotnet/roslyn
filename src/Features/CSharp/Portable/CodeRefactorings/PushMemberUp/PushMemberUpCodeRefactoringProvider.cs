// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PushMemberUp
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.PushMember)), Shared]
    internal class PushMemberUpCodeRefactoringProvider : AbstractPushMemberUpCodeRefactoringProvider
    {
        protected override bool IsUserSelectIdentifer(SyntaxNode userSelectedSyntax)
        {
            var identifier = GetIdentifier(userSelectedSyntax);
            return identifier.Span.Contains(Context.Span);
        }

        private SyntaxToken GetIdentifier(SyntaxNode userSelectedSyntax)
        {
            switch (userSelectedSyntax)
            {
                case VariableDeclaratorSyntax variableSyntax:
                    return variableSyntax.Identifier;
                case MethodDeclarationSyntax methodSyntax:
                    return methodSyntax.Identifier;
                case PropertyDeclarationSyntax propertySyntax:
                    return propertySyntax.Identifier;
                case IndexerDeclarationSyntax indexerSyntax:
                    return indexerSyntax.ThisKeyword;
                default:
                    return default;
            }
        }

        protected override void ProcessingClassesRefactoring(IEnumerable<INamedTypeSymbol> targetClasses, SyntaxNode userSelectSyntax)
        {
            foreach (var eachClass in targetClasses)
            {
                var classPusher = new ClassPusher(eachClass, SemanticModel, userSelectSyntax, Context.Document);
                var action = classPusher.ComputeRefactoring();
                if (action != null)
                {
                    Context.RegisterRefactoring(action);
                }
            }
        }

        protected override void ProcessingInterfacesRefactoring(IEnumerable<INamedTypeSymbol> targetInterfaces, SyntaxNode userSelectSyntax)
        {
            foreach (var eachInterface in targetInterfaces)
            {
                var interfacePusher = new InterfacePusher(eachInterface, SemanticModel, userSelectSyntax, Context.Document);
                var action = interfacePusher.ComputeRefactoring();

                if (action != null)
                {
                    Context.RegisterRefactoring(action);
                }
            }
        }
    }
}
