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
    /// Some declared program elements are containers (e.g. namespaces, classes), and therefore
    /// may contain the declarations of further elements.  For containers, the MemberBinderContexts()
    /// method may be used to construct a binder context for each of the declared members contained in the
    /// container declaration.  Since containers may be declared in multiple parts, the member
    /// binders will be merged across all the parts.
    /// </summary>
    internal abstract class NamespaceOrTypeBuilder : MemberBuilder
    {
        internal NamespaceOrTypeBuilder(SourceLocation location, Symbol accessor, Binder enclosing) : base(location, accessor, enclosing) { }

        /// <summary>
        /// the member binders returned from TypeOrNamespaceMemberBinderContexts and
        /// NonContainerMemberBinderContexts are first grouped (be Equals/HashCode)
        /// </summary>
        /// <returns></returns>
        internal abstract IEnumerable<NamespaceOrTypeBuilder> TypeOrNamespaceBuilders(NamespaceOrTypeSymbol current);
        internal abstract IEnumerable<MemberBuilder> NonContainerBuilders(NamespaceOrTypeSymbol current);
    }
}