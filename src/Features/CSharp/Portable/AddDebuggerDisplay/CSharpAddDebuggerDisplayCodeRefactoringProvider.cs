// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.AddDebuggerDisplay;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.AddDebuggerDisplay
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpAddDebuggerDisplayCodeRefactoringProvider)), Shared]
    internal sealed class CSharpAddDebuggerDisplayCodeRefactoringProvider : AbstractAddDebuggerDisplayCodeRefactoringProvider<TypeDeclarationSyntax, MethodDeclarationSyntax>
    {
        protected override bool HasDebuggerDisplayAttribute(TypeDeclarationSyntax typeDeclaration)
        {
            return typeDeclaration.AttributeLists
                .SelectMany(list => list.Attributes)
                .Any(IsDebuggerDisplayAttribute);
        }

        private bool IsDebuggerDisplayAttribute(AttributeSyntax attribute)
        {
            // Purposely bails for efficiency if anything called "DebuggerDisplay" is already applied, regardless of
            // whether it's the "real" one.

            var name = attribute.Name;

            while (name is QualifiedNameSyntax { Right: var rightSide })
            {
                name = rightSide;
            }

            return name is IdentifierNameSyntax { Identifier: var identifier } && IsDebuggerDisplayAttributeIdentifier(identifier);
        }

        protected override bool IsToStringOverride(MethodDeclarationSyntax methodDeclaration)
        {
            // Purposely bails for efficiency if no "ToString" override is in the same syntax tree, regardless of
            // whether it's declared in another partial class file. Since the DebuggerDisplay attribute will refer to
            // it, it's nicer to have them both in the same file anyway.

            return methodDeclaration is
            {
                Arity: 0,
                ParameterList:
                { Parameters: { Count: 0 } },
                Identifier:
                { ValueText: nameof(ToString) },
                Modifiers: var modifiers
            } && modifiers.Any(SyntaxKind.OverrideKeyword);
        }

        protected override bool DeclaresToStringOverride(TypeDeclarationSyntax typeDeclaration)
        {
            return typeDeclaration.Members.OfType<MethodDeclarationSyntax>().Any(IsToStringOverride);
        }
    }
}
