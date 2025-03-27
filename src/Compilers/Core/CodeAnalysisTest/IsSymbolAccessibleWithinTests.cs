// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class IsSymbolAccessibleWithinTests
    {
        [Fact]
        public void CrossLanguageException()
        {
            var csharpTree = CSharpTestSource.Parse("class A { }");
            var vbTree = BasicTestSource.Parse(
@"Class A
End Class
");
            var csc = CSharpCompilation.Create("CS", new[] { csharpTree }, new MetadataReference[] { TestBase.MscorlibRef }) as Compilation;
            var Ac = csc.GlobalNamespace.GetMembers("A").First() as INamedTypeSymbol;

            var vbc = VisualBasicCompilation.Create("VB", new[] { vbTree }, new MetadataReference[] { TestBase.MscorlibRef }) as Compilation;
            var Av = vbc.GlobalNamespace.GetMembers("A").First() as INamedTypeSymbol;

            Assert.Throws<ArgumentException>(() => csc.IsSymbolAccessibleWithin(Av, Av));
            Assert.Throws<ArgumentException>(() => csc.IsSymbolAccessibleWithin(Av, Ac));
            Assert.Throws<ArgumentException>(() => csc.IsSymbolAccessibleWithin(Ac, Av));
            Assert.Throws<ArgumentException>(() => csc.IsSymbolAccessibleWithin(Ac, Ac, Av));

            Assert.Throws<ArgumentException>(() => vbc.IsSymbolAccessibleWithin(Ac, Ac));
            Assert.Throws<ArgumentException>(() => vbc.IsSymbolAccessibleWithin(Ac, Av));
            Assert.Throws<ArgumentException>(() => vbc.IsSymbolAccessibleWithin(Av, Ac));
            Assert.Throws<ArgumentException>(() => vbc.IsSymbolAccessibleWithin(Av, Av, Ac));
        }
    }
}
