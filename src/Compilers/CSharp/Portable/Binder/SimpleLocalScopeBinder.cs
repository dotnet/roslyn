// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Immutable;

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
    }
}
