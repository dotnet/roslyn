// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
