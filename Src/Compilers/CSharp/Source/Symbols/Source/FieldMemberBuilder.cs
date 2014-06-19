using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Roslyn.Compilers.CSharp
{
    internal class FieldMemberBuilder : MemberBuilder
    {
        private readonly NamedTypeSymbol owner;
        private readonly FieldDeclarationSyntax declaration;
        private readonly VariableDeclaratorSyntax declarator;
        internal readonly TypeSymbol Type;

        internal FieldMemberBuilder(NamedTypeSymbol owner, Binder enclosing, FieldDeclarationSyntax declaration, TypeSymbol type, VariableDeclaratorSyntax declarator)
            : base(enclosing.Location(declarator) as SourceLocation, owner, enclosing)
        {
            this.owner = owner;
            this.declaration = declaration;
            this.declarator = declarator;
            this.Type = type;
        }

        internal override SyntaxTree Tree
        {
            get
            {
                return Enclosing.SourceTree;
            }
        }
        internal FieldDeclarationSyntax Declaration
        {
            get
            {
                return declaration;
            }
        }

        internal VariableDeclaratorSyntax Declarator
        {
            get
            {
                return declarator;
            }
        }

        internal override MemberDeclarationSyntax Syntax
        {
            get
            {
                return declaration;
            }
        }

        internal override Location NameLocation
        {
            get
            {
                return new SourceLocation(Tree, declarator.Identifier);
            }
        }

        internal override SyntaxTokenList SyntaxModifiers
        {
            get
            {
                return declaration.Modifiers;
            }
        }

        internal string Name
        {
            get
            {
                return declarator.Identifier.ValueText;
            }
        }

        internal override Symbol MakeSymbol(Symbol parent, IEnumerable<MemberBuilder> builders, DiagnosticBag diagnostics)
        {
            var fieldBuilders = builders.OfType<FieldMemberBuilder>().AsReadOnly();
            return new SourceFieldSymbol(Name, owner, fieldBuilders, diagnostics);
        }

        internal override object Key()
        {
            return Name;
        }
    }
}
