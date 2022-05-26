// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class NonSourceAssemblySymbol : AssemblySymbol
    {
        private readonly Symbols.AssemblySymbol _underlying;

        public NonSourceAssemblySymbol(Symbols.AssemblySymbol underlying)
        {
            Debug.Assert(underlying is object);
            Debug.Assert(!(underlying is Symbols.SourceAssemblySymbol));
            _underlying = underlying;
        }

        internal override Symbols.AssemblySymbol UnderlyingAssemblySymbol => _underlying;
        internal override CSharp.Symbol UnderlyingSymbol => _underlying;
    }
}
