// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PullMemberUp;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    [ExportLanguageService(typeof(IPullMemberUpActionAndSolutionGenerator), LanguageNames.CSharp), Shared]
    internal class CSharpCodeActionAndSolutionGenerator : AbstractCodeAndSolutionGenerator
    {
        protected override void RemoveNode(DocumentEditor editor, SyntaxNode node, ISymbol symbol)
        {
            if (node is VariableDeclaratorSyntax variableDeclarator &&
                (symbol.Kind == SymbolKind.Field || symbol.Kind == SymbolKind.Event))
            {
                if (variableDeclarator.Parent != null &&
                    variableDeclarator.Parent.Parent is BaseFieldDeclarationSyntax fieldOrEventDeclaration)
                {
                    if (fieldOrEventDeclaration.Declaration.Variables.Count() == 1)
                    {
                        // If there is only one variable, e.g.
                        // public int i = 0;
                        // Just remove all
                        editor.RemoveNode(fieldOrEventDeclaration);
                    }
                    else
                    {
                        // If there are multiple variables, e.g.
                        // public int i, j = 0;
                        // Remove only one variable
                        editor.RemoveNode(variableDeclarator);
                    }
                }
            }
            else
            {
                editor.RemoveNode(node);
            }
        }

        protected override void ChangeMemberToPublicAndNonStatic(
            DocumentEditor editor,
            ISymbol symbol,
            SyntaxNode memberSyntax,
            SyntaxNode containingTypeNode,
            ICodeGenerationService codeGenerationService)
        {
            var modifiers = DeclarationModifiers.From(symbol).WithIsStatic(false);
            if (symbol is IEventSymbol eventSymbol)
            {
                if (memberSyntax.Parent != null &&
                    memberSyntax.Parent.Parent is BaseFieldDeclarationSyntax eventDeclaration)
                {
                    if (eventDeclaration.Declaration.Variables.Count > 1)
                    {
                        ChangeOneEventToPublicNonStaticFromMultipleEvents(
                            editor,
                            eventSymbol,
                            memberSyntax,
                            containingTypeNode,
                            modifiers,
                            codeGenerationService);
                        return;
                    }
                    else
                    {
                        editor.SetModifiers(eventDeclaration, modifiers);
                        editor.SetAccessibility(eventDeclaration, Accessibility.Public);
                        return;
                    }
                }
            }

            editor.SetAccessibility(memberSyntax, Accessibility.Public);
            editor.SetModifiers(memberSyntax, modifiers);
        }

        private void ChangeOneEventToPublicNonStaticFromMultipleEvents(
            DocumentEditor editor,
            IEventSymbol eventSymbol,
            SyntaxNode memberSyntax,
            SyntaxNode containingTypeNode,
            DeclarationModifiers modifiers,
            ICodeGenerationService codeGenerationService)
        {
            // Change one event to public and non-static from many events
            var options = new CodeGenerationOptions(generateMethodBodies: false, generateMembers: false);
            // 1. Remove it from its original declaration.
            editor.RemoveNode(memberSyntax);

            // 2. Change its modifiers and accessibility, then add it back to containing type.
            var publicAndNonStaticSymbol = CodeGenerationSymbolFactory.CreateEventSymbol(
                eventSymbol,
                accessibility: Accessibility.Public,
                modifiers: modifiers);
            var nonStaticSyntax = codeGenerationService.CreateEventDeclaration(publicAndNonStaticSymbol, options: options);
            editor.AddMember(containingTypeNode, nonStaticSyntax);
        }
    }
}
