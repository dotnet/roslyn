// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class SourceAssemblySymbol : AssemblySymbol, ISourceAssemblySymbol
    {
        private readonly Symbols.SourceAssemblySymbol _underlying;

        public SourceAssemblySymbol(Symbols.SourceAssemblySymbol underlying)
        {
            Debug.Assert(underlying is object);
            _underlying = underlying;
        }

        internal override Symbols.AssemblySymbol UnderlyingAssemblySymbol => _underlying;
        internal override CSharp.Symbol UnderlyingSymbol => _underlying;

        Compilation ISourceAssemblySymbol.Compilation => _underlying.DeclaringCompilation;
    }
}
