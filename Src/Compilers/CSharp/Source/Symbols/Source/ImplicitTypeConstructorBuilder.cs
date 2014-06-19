using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Compilers.Internal;
using Roslyn.Compilers.Collections;

namespace Roslyn.Compilers.CSharp
{
    internal class ImplicitTypeConstructorBuilder
    {
        private readonly ReadOnlyArray<StatementSyntax> statements;
        internal Binder Enclosing { get; private set; }

        internal ImplicitTypeConstructorBuilder(NamedTypeSymbol container, Binder enclosing, ReadOnlyArray<StatementSyntax> statements)
        {
            this.statements = statements;
            this.Enclosing = enclosing;
        }

        internal MethodBodyBinder CreateBodyBinder(ImplicitTypeConstructorSymbol owner)
        {
            // TODO: (tomat) is this correct?
            var compilation = ((SourceAssemblySymbol)owner.ContainingAssembly).Compilation;
            return new MethodBodyBinder(new MethodBodyBinding(compilation, owner, Enclosing, statements.AsEnumerable()), Enclosing);
        }

        internal ReadOnlyArray<StatementSyntax> Statements
        {
            get
            {
                return statements;
            }
        }

        // TODO: check nullity @ call sites
        internal MemberDeclarationSyntax Syntax
        {
            get
            {
                return null;
            }
        }

        internal Location NameLocation
        {
            get
            {
                Contract.Fail();
                return null;
            }
        }

        internal SyntaxTree SyntaxTree
        {
            get
            {
                return Enclosing.SourceTree;
            }
        }

        internal SyntaxTokenList SyntaxModifiers
        {
            get
            {
                // TODO: ?
                return default(SyntaxTokenList);
            }
        }

        internal Symbol MakeSymbol(Symbol parent, ReadOnlyArray<ImplicitTypeConstructorBuilder> builders, DiagnosticBag diagnostics)
        {
            return new ImplicitTypeConstructorSymbol((NamedTypeSymbol)Enclosing.Accessor, builders);
        }
    }
}
