// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        /// <summary>
        /// A helper type for mapping <see cref="ISymbol"/> back to an originating <see cref="Project"/>.
        /// </summary>
        /// <remarks>
        /// In IDE scenarios we have the need to map from an <see cref="ISymbol"/> to the <see cref="Project"/> that
        /// contained a <see cref="Compilation"/> that could have produced that symbol.  This is especially needed with
        /// OOP scenarios where we have to communicate to OOP from VS (And vice versa) what symbol we are referring to.
        /// To do this, we pass along a project where this symbol could be found, and enough information (a <see
        /// cref="SymbolKey"/>) to resolve that symbol back in that that <see cref="Project"/>.
        /// <para>
        /// This is challenging however as symbols do not necessarily have back-pointers to <see cref="Compilation"/>s,
        /// and as such, we can't just see which Project produced the <see cref="Compilation"/> that produced that <see
        /// cref="ISymbol"/>.  In other words, the <see cref="ISymbol"/> doesn't <c>root</c> the compilation.  Because
        /// of that we keep track of those symbols per project in a <em>weak</em> fashion.  Then, we can later see if a
        /// symbol came from a particular project by checking if it is one of those weak symbols.  We use weakly held
        /// symbols to that a <see cref="ProjectState"/> instance doesn't hold symbols alive.  But, we know if we are
        /// holding the symbol itself, then the weak-ref will stay alive such that we can do this containment check.
        /// </para>
        /// </remarks>
        private readonly struct UnrootedSymbolSet
        {
            /// <summary>
            /// The <see cref="IAssemblySymbol"/> produced directly by <see cref="Compilation.Assembly"/>.
            /// </summary>
            public readonly WeakReference<IAssemblySymbol> PrimaryAssemblySymbol;

            /// <summary>
            /// The <see cref="IDynamicTypeSymbol"/> produced directly by <see cref="Compilation.DynamicType"/>.  Only
            /// valid for <see cref="LanguageNames.CSharp"/>.
            /// </summary>
            public readonly WeakReference<ITypeSymbol?> PrimaryDynamicSymbol;

            /// <summary>
            /// The <see cref="IAssemblySymbol"/>s or <see cref="IModuleSymbol"/>s produced through <see
            /// cref="Compilation.GetAssemblyOrModuleSymbol(MetadataReference)"/> for all the references exposed by <see
            /// cref="Compilation.References"/>/
            /// </summary>
            public readonly WeakSet<ISymbol> SecondaryReferencedSymbols;

            public UnrootedSymbolSet(WeakReference<IAssemblySymbol> primaryAssemblySymbol, WeakReference<ITypeSymbol?> primaryDynamicSymbol, WeakSet<ISymbol> secondaryReferencedSymbols)
            {
                PrimaryAssemblySymbol = primaryAssemblySymbol;
                PrimaryDynamicSymbol = primaryDynamicSymbol;
                SecondaryReferencedSymbols = secondaryReferencedSymbols;
            }
        }
    }
}
