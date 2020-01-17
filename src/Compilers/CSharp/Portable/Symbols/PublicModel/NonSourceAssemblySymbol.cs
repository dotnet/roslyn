// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class NonSourceAssemblySymbol : AssemblySymbol
    {
        private readonly Symbols.AssemblySymbol _underlying;

        public NonSourceAssemblySymbol(Symbols.AssemblySymbol underlying)
        {
            RoslynDebug.Assert(underlying is object);
            Debug.Assert(!(underlying is Symbols.SourceAssemblySymbol));
            _underlying = underlying;
        }

        internal override Symbols.AssemblySymbol UnderlyingAssemblySymbol => _underlying;
        internal override CSharp.Symbol UnderlyingSymbol => _underlying;
    }
}
