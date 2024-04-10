// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class SimpleLocalScopeBinder : LocalScopeBinder
    {
        private readonly ImmutableArray<LocalSymbol> _locals;

        public SimpleLocalScopeBinder(ImmutableArray<LocalSymbol> locals, Binder next) :
            base(next)
        {
            _locals = locals;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            return _locals;
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(SyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
