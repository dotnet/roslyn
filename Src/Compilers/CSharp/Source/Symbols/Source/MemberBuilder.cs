using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Roslyn.Compilers;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// A builder for the source of (part of) the declaration of a program element.
    /// Because some program elements may be declared in multiple parts,
    /// (namespaces, partial classes, and partial methods), and also to diagnose
    /// duplicate definitions, builders are grouped according to the thing they are
    /// attempting to builde.  They are then bound together, in one operation,
    /// to create the symbol for the declared item.
    /// </summary>
    internal abstract class MemberBuilder // : BinderContext // TODOngafter 0: builders should not be binders.
    {
        private readonly SourceLocation location;
        private readonly Symbol accessor;
        private readonly Binder enclosing;

        internal MemberBuilder(SourceLocation location, Symbol accessor, Binder enclosing) // : base(location, accessor, enclosing)
        {
            this.location = location;
            this.accessor = accessor;
            this.enclosing = enclosing;
        }

        internal SourceLocation Location()
        {
            return location;
        }

        internal SourceLocation Location(SyntaxNode node)
        {
            return enclosing.Location(node) as SourceLocation;
        }

        internal SourceLocation Location(SyntaxToken token)
        {
            return enclosing.Location(token) as SourceLocation;
        }

        internal SyntaxTree SourceTree
        {
            get
            {
                return enclosing.SourceTree;
            }
        }

        internal Binder Next
        {
            get
            {
                return enclosing;
            }
        }

        internal Binder Enclosing
        {
            get
            {
                return enclosing;
            }
        }

        internal Symbol Accessor
        {
            get
            {
                return accessor;
            }
        }

        internal abstract SyntaxTree Tree { get; }

        /// <summary>
        /// Null for global code container and other synthetic methods.
        /// </summary>
        internal abstract MemberDeclarationSyntax Syntax { get; }

        internal abstract SyntaxTokenList SyntaxModifiers { get; }

        internal abstract Location NameLocation { get; }

        /// Equals and GetHashCode on the "Key" are used to group binder contexts that are
        /// for (different parts of) the same symbol and therefore must be used together
        /// to make the symbol.
        internal abstract object Key();

        /// <summary>
        /// Once member binders are grouped into like-minded binders, they are used together to create the member.
        /// </summary>
        /// <param name="parent">The desired owner of the resulting symbol</param>
        /// <param name="contexts">The binder contexts for the parts of the symbol's declaration</param>
        /// <param name="diagnostics">Where to place diagnostics generated while constructing the symbol</param>
        /// <returns></returns>
        internal abstract Symbol MakeSymbol(Symbol parent, IEnumerable<MemberBuilder> contexts, DiagnosticBag diagnostics);
    }
}