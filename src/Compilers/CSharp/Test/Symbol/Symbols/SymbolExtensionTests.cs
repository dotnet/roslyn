// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class SymbolExtensionTests : CSharpTestBase
    {
        [Fact]
        public void HasNameQualifier()
        {
            var source =
@"class C { }
namespace N
{
    class C { }
    namespace NA
    {
        class C { }
        namespace NB
        {
            class C { }
        }
    }
}
namespace NA
{
    class C { }
    namespace NA
    {
        class C { }
    }
    namespace NB
    {
        class C { }
    }
}
namespace NB
{
    class C { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();
            var namespaceNames = new[]
            {
                "",
                ".",
                "N",
                "NA",
                "NB",
                "n",
                "AN",
                "NAB",
                "N.",
                ".NA",
                ".NB",
                "N.N",
                "N.NA",
                "N.NB",
                "N..NB",
                "N.NA.NA",
                "N.NA.NB",
                "NA.N",
                "NA.NA",
                "NA.NB",
                "NA.NA.NB",
                "NA.NB.NB",
            };
            HasNameQualifierCore(namespaceNames, compilation.GetMember<NamedTypeSymbol>("C"), "");
            HasNameQualifierCore(namespaceNames, compilation.GetMember<NamedTypeSymbol>("N.C"), "N");
            HasNameQualifierCore(namespaceNames, compilation.GetMember<NamedTypeSymbol>("N.NA.C"), "N.NA");
            HasNameQualifierCore(namespaceNames, compilation.GetMember<NamedTypeSymbol>("N.NA.NB.C"), "N.NA.NB");
            HasNameQualifierCore(namespaceNames, compilation.GetMember<NamedTypeSymbol>("NA.C"), "NA");
            HasNameQualifierCore(namespaceNames, compilation.GetMember<NamedTypeSymbol>("NA.NA.C"), "NA.NA");
            HasNameQualifierCore(namespaceNames, compilation.GetMember<NamedTypeSymbol>("NA.NB.C"), "NA.NB");
            HasNameQualifierCore(namespaceNames, compilation.GetMember<NamedTypeSymbol>("NB.C"), "NB");
        }

        private void HasNameQualifierCore(string[] namespaceNames, NamedTypeSymbol type, string expectedName)
        {
            Assert.True(Array.IndexOf(namespaceNames, expectedName) >= 0);
            foreach (var namespaceName in namespaceNames)
            {
                Assert.Equal(namespaceName == expectedName, type.HasNameQualifier(namespaceName));
            }
        }
    }
}
