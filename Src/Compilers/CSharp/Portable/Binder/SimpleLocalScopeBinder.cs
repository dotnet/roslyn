// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class SimpleLocalScopeBinder : LocalScopeBinder
    {
        private readonly ImmutableArray<LocalSymbol> locals;

        public SimpleLocalScopeBinder(ImmutableArray<LocalSymbol> locals, Binder next) :
            base(next)
        {
            this.locals = locals;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            return this.locals;
        }
    }
}
