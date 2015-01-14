// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    public class MemberResolutionResultTests
    {
        [Fact]
        public void Equality()
        {
            var d = default(MemberResolutionResult<MethodSymbol>);
            Assert.Throws<NotSupportedException>(() => d.Equals(d));
            Assert.Throws<NotSupportedException>(() => d.GetHashCode());
        }
    }
}
