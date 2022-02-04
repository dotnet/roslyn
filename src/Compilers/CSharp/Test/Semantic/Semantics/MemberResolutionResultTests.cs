// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
