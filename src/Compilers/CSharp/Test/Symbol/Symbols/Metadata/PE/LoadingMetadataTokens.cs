// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class LoadingMetadataTokens : CSharpTestBase
    {
        [Fact]
        public void LoadPESymbols()
        {
            var compilation = CreateCompilation(
@"
public class C
{
    public int f = 0;

    public int P { get; set; }

    public void M(int p)
    {
    }

    public void GM<T>()
    {
    }

    public event System.Action E;
}

public struct S
{
}",
                parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute());

            CompileAndVerify(compilation, symbolValidator: module =>
            {
                var peModule = (PEModuleSymbol)module;

                var assembly = peModule.ContainingAssembly;
                Assert.Equal(536870913, assembly.MetadataToken);

                var class1 = module.GlobalNamespace.GetTypeMember("C");
                Assert.Equal(33554434, class1.MetadataToken);

                var field = class1.GetMember("f");
                Assert.Equal(67108865, field.MetadataToken);

                var property = class1.GetMember("P");
                Assert.Equal(385875969, property.MetadataToken);

                var method = class1.GetMember("M");
                Assert.Equal(100663299, method.MetadataToken);

                var parameter = method.GetParameters().Single();
                Assert.Equal(134217730, parameter.MetadataToken);

                var genericMethod = class1.GetMember("GM");
                Assert.Equal(100663300, genericMethod.MetadataToken);

                var typeParameter = genericMethod.GetMemberTypeParameters().Single();
                Assert.Equal(704643073, typeParameter.MetadataToken);

                var event1 = class1.GetMember("E");
                Assert.Equal(335544321, event1.MetadataToken);

                var struct1 = module.GlobalNamespace.GetTypeMember("S");
                Assert.Equal(33554435, struct1.MetadataToken);
            });
        }

        [Fact]
        public void LoadSourceSymbols()
        {
            var compilation = CreateCompilation(
@"
public class C
{
    public int f = 0;

    public int P { get; set; }

    public void M(int p)
    {
    }

    public void GM<T>()
    {
    }

    public event System.Action E;
}

public struct S
{
}");

            var assembly = compilation.Assembly;
            Assert.Equal(0, assembly.MetadataToken);

            var class1 = compilation.GlobalNamespace.GetTypeMember("C");
            Assert.Equal(0, class1.MetadataToken);

            var field = class1.GetMember("f");
            Assert.Equal(0, field.MetadataToken);

            var property = class1.GetMember("P");
            Assert.Equal(0, property.MetadataToken);

            var method = class1.GetMember("M");
            Assert.Equal(0, method.MetadataToken);

            var parameter = method.GetParameters().Single();
            Assert.Equal(0, parameter.MetadataToken);

            var genericMethod = class1.GetMember("GM");
            Assert.Equal(0, genericMethod.MetadataToken);

            var typeParameter = genericMethod.GetMemberTypeParameters().Single();
            Assert.Equal(0, typeParameter.MetadataToken);

            var event1 = class1.GetMember("E");
            Assert.Equal(0, event1.MetadataToken);

            var struct1 = compilation.GlobalNamespace.GetTypeMember("S");
            Assert.Equal(0, struct1.MetadataToken);
        }
    }
}
