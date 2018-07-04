// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class IsSymbolAccessibleWithinTests
    {
        [Fact]
        public void CrossLanguageException()
        {
            var csharpTree = CSharpSyntaxTree.ParseText("class A { }");
            var vbTree = VisualBasicSyntaxTree.ParseText(
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
