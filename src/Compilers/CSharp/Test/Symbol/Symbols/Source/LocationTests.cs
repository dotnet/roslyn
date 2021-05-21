// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LocationTests : CSharpTestBase
    {
        [Fact]
        public void Simple1()
        {
            var text =
                "namespace N.S{class C{int F; void M(int P}{}}";

            // 000000000011111111112222222222333333333344444444445555555555666666666677777777778
            // 012345678901234567890123456789012345678901234567890123456789012345678901234567890
            var comp = CreateEmptyCompilation(text, new[] { MscorlibRef });
            var global = comp.GlobalNamespace;
            var n = global.GetMembers("N").Single() as NamespaceSymbol;
            AssertPos(n, 10, 1);
            var s = n.GetMembers("S").Single() as NamespaceSymbol;
            AssertPos(s, 12, 1);
            var c = s.GetTypeMembers("C", 0).Single() as NamedTypeSymbol;
            AssertPos(c, 20, 1);
            var obj = c.BaseType();
            Assert.Equal("MetadataFile(CommonLanguageRuntimeLibrary)", obj.Locations[0].ToString());
            var f = c.GetMembers("F").Single() as FieldSymbol;
            AssertPos(f, 26, 1);
            var m = c.GetMembers("M").Single() as MethodSymbol;
            AssertPos(m, 34, 1);
            var p = m.Parameters[0];
            AssertPos(p, 40, 1);
        }

        private void AssertPos(Symbol sym, int start, int length)
        {
            var pos = sym.Locations.Single();
            Assert.Equal(start, pos.SourceSpan.Start);
            Assert.Equal(length, pos.SourceSpan.Length);
        }
    }
}
