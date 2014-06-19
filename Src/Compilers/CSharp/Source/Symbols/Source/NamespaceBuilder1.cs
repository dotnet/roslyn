using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Roslyn.Compilers.Internal;
using Roslyn.Compilers.Collections;

namespace Roslyn.Compilers.CSharp
{
    // Used only in the "old" implementation of symbols from sources.
    internal class NamespaceBuilder1 : NamespaceOrTypeBuilder
    {
        private readonly SingleDeclaration declaration;
        internal NamespaceBuilder1(Symbol owner, SingleDeclaration declaration, BinderContext enclosingContext)
            : base(declaration.Location, owner, enclosingContext)
        {
            this.declaration = declaration;
        }

        internal override object Key() { return declaration.Name; }
        
        internal override MemberDeclarationSyntax Syntax
        {
            get { return declaration.Syntax.GetSyntax() as NamespaceDeclarationSyntax; }
        }

        internal override IEnumerable<SyntaxToken> SyntaxModifiers
        {
            get
            {
                return Enumerable.Empty<SyntaxToken>(); // namespace declarations lack modifiers
            }
        }

        internal override IEnumerable<NamespaceOrTypeBuilder> TypeOrNamespaceBuilders(NamespaceOrTypeSymbol current)
        {
            BinderContext next; // push diagnostics to the top level
            NamespaceOrTypeSymbol merged; // lookup in a merged context
            if (this is TopLevelBuilder1)
            {
                next = this;
                merged = Compilation.GlobalNamespace;
            }
            else
            {
                next = this.Next;
                merged = (next as InContainerBinderContext).Container.GetMembers(current.Name).OfType<Symbol, NamespaceSymbol>().Single();
            }

            BinderContext bodyContext = new InContainerBinderContext(declaration, merged, next);
            foreach (var d in declaration.SingleChildren)
            {
              yield return d.Kind == DeclarationKind.Namespace
                  ? (NamespaceOrTypeBuilder)new NamespaceBuilder1(current, d, bodyContext)
                  : (NamespaceOrTypeBuilder)new NamedTypeBuilder(current, d, bodyContext);
            }
        }

        internal override IEnumerable<MemberBuilder> NonContainerBuilders(NamespaceOrTypeSymbol current)
        {
            return ConsList<MemberBuilder>.Empty;
        }

        internal override Symbol MakeSymbol(Symbol parent, IEnumerable<MemberBuilder> contexts)
        {
            return new SourceNamespaceSymbol(parent, declaration.Name, contexts.Select(c => (NamespaceBuilder1)c));
        }

        internal SourceLocation NameLocation
        {
            get
            {
                return declaration.NameLocation;
            }
        }
    }
}