// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp.Dialog
{
    [ExportLanguageService(typeof(AbstractInterfacePullerWithDialog), LanguageNames.CSharp), Shared]
    internal class CSharpInterfacePullerViaDialog : AbstractInterfacePullerWithDialog
    {
        protected override void ChangeMemberToPublicAndNonStatic(DocumentEditor editor, ISymbol symbol, SyntaxNode memberSyntax, SyntaxNode containingTypeNode, ICodeGenerationService codeGenerationService)
        {
            var modifier = DeclarationModifiers.From(symbol).WithIsStatic(false);
            if (symbol is IEventSymbol eventSymbol)
            {
                if (memberSyntax.Parent != null &&
                    memberSyntax.Parent.Parent is BaseFieldDeclarationSyntax eventDeclaration)
                {
                    if (eventDeclaration.Declaration.Variables.Count > 1)
                    {
                        var options = new CodeGenerationOptions(generateMethodBodies: false, generateMembers: false);
                        var publicAndNonStaticSymbol = CodeGenerationSymbolFactory.CreateEventSymbol(
                            eventSymbol.GetAttributes(),
                            Accessibility.Public,
                            modifier,
                            eventSymbol.Type,
                            eventSymbol.ExplicitInterfaceImplementations,
                            eventSymbol.Name,
                            eventSymbol.AddMethod,
                            eventSymbol.RemoveMethod,
                            eventSymbol.RaiseMethod);
                        var nonStaticSyntax = codeGenerationService.CreateEventDeclaration(publicAndNonStaticSymbol, options: options);
                        editor.RemoveNode(memberSyntax);
                        editor.AddMember(containingTypeNode, nonStaticSyntax);
                        return;
                    }
                    else
                    {
                        editor.SetModifiers(eventDeclaration, modifier);
                        editor.SetAccessibility(eventDeclaration, Accessibility.Public);
                        return;
                    }
                }
            }
            editor.SetAccessibility(memberSyntax, Accessibility.Public);
            editor.SetModifiers(memberSyntax, modifier);
        }
    }
}
