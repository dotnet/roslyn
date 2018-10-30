using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    internal class PullMemberUpSyntaxEditor
    {
        public void RemoveNode(DocumentEditor editor, SyntaxNode node, ISymbol symbol)
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

        public void ChangeMemberToNonStatic(
            DocumentEditor editor,
            ISymbol memberSymbol,
            SyntaxNode memberSyntax,
            SyntaxNode containingTypeSyntax,
            ICodeGenerationService codeGenerationService)
        {
            var modifier = DeclarationModifiers.From(memberSymbol).WithIsStatic(false);
            if (memberSymbol is IEventSymbol eventSymbol)
            {
                if (memberSyntax.Parent != null &&
                    memberSyntax.Parent.Parent is BaseFieldDeclarationSyntax eventDeclaration)
                {
                    if (eventDeclaration.Declaration.Variables.Count == 1)
                    {
                        editor.SetModifiers(eventDeclaration, modifier);
                    }
                    else if (eventDeclaration.Declaration.Variables.Count > 1)
                    {
                        var options = new CodeGenerationOptions(generateMethodBodies: false, generateMembers: false);
                        var nonStaticSymbol = CodeGenerationSymbolFactory.CreateEventSymbol(
                            eventSymbol.GetAttributes(),
                            eventSymbol.DeclaredAccessibility,
                            modifier,
                            eventSymbol.Type,
                            eventSymbol.ExplicitInterfaceImplementations,
                            eventSymbol.Name,
                            eventSymbol.AddMethod,
                            eventSymbol.RemoveMethod,
                            eventSymbol.RaiseMethod);
                        var nonStaticSyntax = codeGenerationService.CreateEventDeclaration(nonStaticSymbol, options: options);
                        editor.RemoveNode(memberSyntax);
                        editor.AddMember(containingTypeSyntax, nonStaticSyntax);
                    }
                }
            }
            else
            {
                editor.SetModifiers(memberSyntax, modifier);
            }
        }

        public void ChangeMemberToPublic(
            DocumentEditor editor,
            ISymbol memberSymbol,
            SyntaxNode memberSytax,
            SyntaxNode containingTypeSyntax,
            ICodeGenerationService codeGenerationService)
        {
            if (memberSymbol is IEventSymbol eventSymbol)
            {
                if (memberSytax.Parent != null &&
                    memberSytax.Parent.Parent is BaseFieldDeclarationSyntax eventDeclaration)
                {
                    if (eventDeclaration.Declaration.Variables.Count == 1)
                    {
                        editor.SetAccessibility(eventDeclaration, Accessibility.Public);
                    }
                    else if (eventDeclaration.Declaration.Variables.Count > 1)
                    {
                        // If multiple declaration on same line
                        // e.g. private EventHandler event Event1, Event2, Event3
                        // change Event1 to public need to create a new declaration
                        var options = new CodeGenerationOptions(generateMethodBodies: false, generateMembers: false);
                        var publicSyntax = codeGenerationService.CreateEventDeclaration(eventSymbol, CodeGenerationDestination.InterfaceType, options);

                        editor.RemoveNode(memberSytax);
                        editor.AddMember(containingTypeSyntax, publicSyntax);
                    }
                }
            }
            else
            {
                editor.SetAccessibility(memberSytax, Accessibility.Public);
            }
        }
    }
}
