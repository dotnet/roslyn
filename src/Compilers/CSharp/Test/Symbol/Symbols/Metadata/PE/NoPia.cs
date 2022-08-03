// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.TestMetadata;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class NoPia : CSharpTestBase
    {
        [Fact]
        public void HideLocalTypeDefinitions()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.NoPia.LocalTypes1,
                TestReferences.SymbolsTests.NoPia.LocalTypes2
            });

            var localTypes1 = assemblies[0].Modules[0];
            var localTypes2 = assemblies[1].Modules[0];

            Assert.Equal(0, localTypes1.GlobalNamespace.GetMembers("I1").Length);
            Assert.Equal(0, localTypes1.GlobalNamespace.GetMembers("S1").Length);
            Assert.Equal(0, localTypes1.GlobalNamespace.GetMembers("NS1").OfType<NamespaceSymbol>().Single().
                                        GetTypeMembers().Length);

            Assert.Equal(0, localTypes2.GlobalNamespace.GetMembers("I1").Length);
            Assert.Equal(0, localTypes2.GlobalNamespace.GetMembers("S1").Length);
            Assert.Equal(0, localTypes2.GlobalNamespace.GetMembers("NS1").OfType<NamespaceSymbol>().Single().
                                        GetTypeMembers().Length);
        }

        [Fact]
        public void LocalTypeSubstitution1()
        {
            var assemblies1 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                        TestReferences.SymbolsTests.NoPia.LocalTypes1,
                                        TestReferences.SymbolsTests.NoPia.LocalTypes2,
                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                        Net40.mscorlib,
                                        TestReferences.SymbolsTests.MDTestLib1
                                    });

            var localTypes1_1 = assemblies1[0];
            var localTypes2_1 = assemblies1[1];
            var pia1_1 = assemblies1[2];

            var varI1 = pia1_1.GlobalNamespace.GetTypeMembers("I1").Single();
            var varS1 = pia1_1.GlobalNamespace.GetTypeMembers("S1").Single();
            var varNS1 = pia1_1.GlobalNamespace.GetMembers("NS1").OfType<NamespaceSymbol>().Single();
            var varI2 = varNS1.GetTypeMembers("I2").Single();
            var varS2 = varNS1.GetTypeMembers("S2").Single();

            NamedTypeSymbol classLocalTypes1;
            NamedTypeSymbol classLocalTypes2;

            classLocalTypes1 = localTypes1_1.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_1.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            MethodSymbol test1;
            MethodSymbol test2;

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            ImmutableArray<ParameterSymbol> param;

            param = test1.Parameters;

            Assert.Same(varI1, param[0].Type);
            Assert.Same(varI2, param[1].Type);

            param = test2.Parameters;

            Assert.Same(varS1, param[0].Type);
            Assert.Same(varS2, param[1].Type);

            var assemblies2 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                        TestReferences.SymbolsTests.NoPia.LocalTypes1,
                                        TestReferences.SymbolsTests.NoPia.LocalTypes2,
                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                        Net40.mscorlib
                                    });

            var localTypes1_2 = assemblies2[0];
            var localTypes2_2 = assemblies2[1];

            Assert.NotSame(localTypes1_1, localTypes1_2);
            Assert.NotSame(localTypes2_1, localTypes2_2);
            Assert.Same(pia1_1, assemblies2[2]);

            classLocalTypes1 = localTypes1_2.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_2.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Same(varI1, param[0].Type);
            Assert.Same(varI2, param[1].Type);

            param = test2.Parameters;

            Assert.Same(varS1, param[0].Type);
            Assert.Same(varS2, param[1].Type);

            var assemblies3 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                        TestReferences.SymbolsTests.NoPia.LocalTypes1,
                                        TestReferences.SymbolsTests.NoPia.LocalTypes2,
                                        TestReferences.SymbolsTests.NoPia.Pia1
                                    });

            var localTypes1_3 = assemblies3[0];
            var localTypes2_3 = assemblies3[1];
            var pia1_3 = assemblies3[2];

            Assert.NotSame(localTypes1_1, localTypes1_3);
            Assert.NotSame(localTypes2_1, localTypes2_3);
            Assert.NotSame(localTypes1_2, localTypes1_3);
            Assert.NotSame(localTypes2_2, localTypes2_3);
            Assert.NotSame(pia1_1, pia1_3);

            classLocalTypes1 = localTypes1_3.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_3.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Same(pia1_3.GlobalNamespace.GetTypeMembers("I1").Single(), param[0].Type);
            Assert.Same(pia1_3.GlobalNamespace.GetMembers("NS1").OfType<NamespaceSymbol>().Single().GetTypeMembers("I2").Single(), param[1].Type);

            // This tests that we cannot find canonical type for an embedded structure if we don't know
            // whether it is a structure because we can't find definition of the base class. Mscorlib is
            // not referenced.
            param = test2.Parameters;

            NoPiaMissingCanonicalTypeSymbol missing;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            missing = (NoPiaMissingCanonicalTypeSymbol)param[0].Type;
            Assert.Same(localTypes2_3, missing.EmbeddingAssembly);
            Assert.Null(missing.Guid);
            Assert.Equal(varS1.ToTestDisplayString(), missing.FullTypeName);
            Assert.Equal("f9c2d51d-4f44-45f0-9eda-c9d599b58257", missing.Scope);
            Assert.Equal(varS1.ToTestDisplayString(), missing.Identifier);

            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies4 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                        TestReferences.SymbolsTests.NoPia.LocalTypes1,
                                        TestReferences.SymbolsTests.NoPia.LocalTypes2,
                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                        Net40.mscorlib,
                                        TestReferences.SymbolsTests.MDTestLib1
                                    });

            for (int i = 0; i < assemblies1.Length; i++)
            {
                Assert.Same(assemblies1[i], assemblies4[i]);
            }

            var assemblies5 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                        TestReferences.SymbolsTests.NoPia.LocalTypes1,
                                        TestReferences.SymbolsTests.NoPia.LocalTypes2,
                                        TestReferences.SymbolsTests.NoPia.Pia2,
                                        Net40.mscorlib
                                    });

            var localTypes1_5 = assemblies5[0];
            var localTypes2_5 = assemblies5[1];

            classLocalTypes1 = localTypes1_5.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_5.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            missing = (NoPiaMissingCanonicalTypeSymbol)param[0].Type;
            Assert.Same(localTypes1_5, missing.EmbeddingAssembly);
            Assert.Equal("27e3e649-994b-4f58-b3c6-f8089a5f2c01", missing.Guid);
            Assert.Equal(varI1.ToTestDisplayString(), missing.FullTypeName);
            Assert.Null(missing.Scope);
            Assert.Null(missing.Identifier);

            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            param = test2.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies6 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                        TestReferences.SymbolsTests.NoPia.LocalTypes1,
                                        TestReferences.SymbolsTests.NoPia.LocalTypes2,
                                        TestReferences.SymbolsTests.NoPia.Pia3,
                                        Net40.mscorlib
                                    });

            var localTypes1_6 = assemblies6[0];
            var localTypes2_6 = assemblies6[1];

            classLocalTypes1 = localTypes1_6.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_6.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            param = test2.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies7 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                        TestReferences.SymbolsTests.NoPia.LocalTypes1,
                                        TestReferences.SymbolsTests.NoPia.LocalTypes2,
                                        TestReferences.SymbolsTests.NoPia.Pia4,
                                        Net40.mscorlib
                                    });

            var localTypes1_7 = assemblies7[0];
            var localTypes2_7 = assemblies7[1];

            classLocalTypes1 = localTypes1_7.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_7.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Equal(TypeKind.Interface, param[0].Type.TypeKind);
            Assert.Equal(TypeKind.Interface, param[1].Type.TypeKind);
            Assert.NotEqual(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, param[1].Type.Kind);

            param = test2.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies8 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                        TestReferences.SymbolsTests.NoPia.LocalTypes1,
                                        TestReferences.SymbolsTests.NoPia.LocalTypes2,
                                        TestReferences.SymbolsTests.NoPia.Pia4,
                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                        Net40.mscorlib
                                    });

            var localTypes1_8 = assemblies8[0];
            var localTypes2_8 = assemblies8[1];
            var pia4_8 = assemblies8[2];
            var pia1_8 = assemblies8[3];

            classLocalTypes1 = localTypes1_8.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_8.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            NoPiaAmbiguousCanonicalTypeSymbol ambiguous;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            ambiguous = (NoPiaAmbiguousCanonicalTypeSymbol)param[0].Type;
            Assert.Same(localTypes1_8, ambiguous.EmbeddingAssembly);
            Assert.Same(pia4_8.GlobalNamespace.GetTypeMembers("I1").Single(), ambiguous.FirstCandidate);
            Assert.Same(pia1_8.GlobalNamespace.GetTypeMembers("I1").Single(), ambiguous.SecondCandidate);
            Assert.False(ambiguous.IsSerializable);

            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaAmbiguousCanonicalTypeSymbol>(param[1].Type);

            var assemblies9 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                      TestReferences.SymbolsTests.NoPia.Library1,
                                      TestReferences.SymbolsTests.NoPia.LocalTypes1,
                                      TestReferences.SymbolsTests.NoPia.Pia4,
                                      Net40.mscorlib
                                  });

            var library1_9 = assemblies9[0];
            var localTypes1_9 = assemblies9[1];

            var assemblies10 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                       TestReferences.SymbolsTests.NoPia.Library1,
                                       TestReferences.SymbolsTests.NoPia.LocalTypes1,
                                       TestReferences.SymbolsTests.NoPia.Pia4,
                                       Net40.mscorlib,
                                       TestReferences.SymbolsTests.MDTestLib1
                                   });

            var library1_10 = assemblies10[0];
            var localTypes1_10 = assemblies10[1];

            Assert.NotSame(library1_9, library1_10);
            Assert.NotSame(localTypes1_9, localTypes1_10);

            GC.KeepAlive(localTypes1_1);
            GC.KeepAlive(localTypes2_1);
            GC.KeepAlive(pia1_1);
            GC.KeepAlive(localTypes1_9);
            GC.KeepAlive(library1_9);
        }

        [Fact]
        public void LocalTypeSubstitution2()
        {
            string localTypes1Source = @"
public class LocalTypes1
{
    public void Test1(I1 x, NS1.I2 y)
    { }
}
";

            string localTypes2Source = @"
public class LocalTypes2
{
    public void Test2(S1 x, NS1.S2 y)
    { }
}
";
            var mscorlibRef = Net40.mscorlib;
            var pia1CopyLink = TestReferences.SymbolsTests.NoPia.Pia1Copy.WithEmbedInteropTypes(true);
            var pia1CopyRef = TestReferences.SymbolsTests.NoPia.Pia1Copy.WithEmbedInteropTypes(false);

            // vbc /t:library /vbruntime- LocalTypes1.vb /l:Pia1.dll                
            var localTypes1 = CSharpCompilation.Create("LocalTypes1", new[] { Parse(localTypes1Source) }, new[] { pia1CopyLink, mscorlibRef });

            var localTypes1Asm = localTypes1.Assembly;

            var localTypes2 = CSharpCompilation.Create("LocalTypes2", new[] { Parse(localTypes2Source) }, new[] { mscorlibRef, pia1CopyLink });

            var localTypes2Asm = localTypes2.Assembly;

            var assemblies1 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                    {
                        TestReferences.SymbolsTests.NoPia.Pia1,
                        Net40.mscorlib,
                        TestReferences.SymbolsTests.MDTestLib1,
                        TestReferences.SymbolsTests.MDTestLib2
                    },
                new CSharpCompilation[] { localTypes1, localTypes2 });

            var localTypes1_1 = assemblies1[4];
            var localTypes2_1 = assemblies1[5];
            var pia1_1 = assemblies1[0];

            Assert.NotSame(localTypes1Asm, localTypes1_1);
            Assert.Equal(1, localTypes1_1.Modules[0].GetReferencedAssemblies().Length);
            Assert.Equal(1, localTypes1_1.Modules[0].GetReferencedAssemblySymbols().Length);
            Assert.Same(localTypes1.GetReferencedAssemblySymbol(mscorlibRef), localTypes1_1.Modules[0].GetReferencedAssemblySymbols()[0]);

            Assert.NotSame(localTypes2Asm, localTypes2_1);
            Assert.Equal(1, localTypes2_1.Modules[0].GetReferencedAssemblies().Length);
            Assert.Equal(1, localTypes2_1.Modules[0].GetReferencedAssemblySymbols().Length);
            Assert.Same(localTypes2.GetReferencedAssemblySymbol(mscorlibRef), localTypes2_1.Modules[0].GetReferencedAssemblySymbols()[0]);

            var varI1 = pia1_1.GlobalNamespace.GetTypeMembers("I1").Single();
            var varS1 = pia1_1.GlobalNamespace.GetTypeMembers("S1").Single();
            var varNS1 = pia1_1.GlobalNamespace.GetMembers("NS1").OfType<NamespaceSymbol>().Single();
            var varI2 = varNS1.GetTypeMembers("I2").Single();
            var varS2 = varNS1.GetTypeMembers("S2").Single();

            NamedTypeSymbol classLocalTypes1;
            NamedTypeSymbol classLocalTypes2;

            classLocalTypes1 = localTypes1_1.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_1.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            MethodSymbol test1;
            MethodSymbol test2;

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            ImmutableArray<ParameterSymbol> param;

            param = test1.Parameters;

            Assert.Same(varI1, param[0].Type);
            Assert.Same(varI2, param[1].Type);

            param = test2.Parameters;

            Assert.Same(varS1, param[0].Type);
            Assert.Same(varS2, param[1].Type);

            var assemblies2 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                        Net40.mscorlib,
                                        TestReferences.SymbolsTests.MDTestLib1
                                    },
                                new CSharpCompilation[] { localTypes1, localTypes2 });

            var localTypes1_2 = assemblies2[3];
            var localTypes2_2 = assemblies2[4];

            Assert.NotSame(localTypes1_1, localTypes1_2);
            Assert.NotSame(localTypes2_1, localTypes2_2);
            Assert.Same(pia1_1, assemblies2[0]);

            classLocalTypes1 = localTypes1_2.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_2.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Same(varI1, param[0].Type);
            Assert.Same(varI2, param[1].Type);

            param = test2.Parameters;

            Assert.Same(varS1, param[0].Type);
            Assert.Same(varS2, param[1].Type);

            var assemblies3 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                        Net40.mscorlib
                                    },
                                new CSharpCompilation[] { localTypes1, localTypes2 });

            var localTypes1_3 = assemblies3[2];
            var localTypes2_3 = assemblies3[3];

            Assert.NotSame(localTypes1_1, localTypes1_3);
            Assert.NotSame(localTypes2_1, localTypes2_3);
            Assert.NotSame(localTypes1_2, localTypes1_3);
            Assert.NotSame(localTypes2_2, localTypes2_3);
            Assert.Same(pia1_1, assemblies3[0]);

            classLocalTypes1 = localTypes1_3.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_3.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Same(varI1, param[0].Type);
            Assert.Same(varI2, param[1].Type);

            param = test2.Parameters;

            Assert.Same(varS1, param[0].Type);
            Assert.Same(varS2, param[1].Type);

            var assemblies4 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                        Net40.mscorlib,
                                        TestReferences.SymbolsTests.MDTestLib1,
                                        TestReferences.SymbolsTests.MDTestLib2
                                    },
                                new CSharpCompilation[] { localTypes1, localTypes2 });

            for (int i = 0; i < assemblies1.Length; i++)
            {
                Assert.Same(assemblies1[i], assemblies4[i]);
            }

            var assemblies5 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                        TestReferences.SymbolsTests.NoPia.Pia2,
                                        Net40.mscorlib
                                    },
                                new CSharpCompilation[] { localTypes1, localTypes2 });

            var localTypes1_5 = assemblies5[2];
            var localTypes2_5 = assemblies5[3];

            classLocalTypes1 = localTypes1_5.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_5.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            NoPiaMissingCanonicalTypeSymbol missing;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            missing = (NoPiaMissingCanonicalTypeSymbol)param[0].Type;
            Assert.Same(localTypes1_5, missing.EmbeddingAssembly);
            Assert.Equal("27e3e649-994b-4f58-b3c6-f8089a5f2c01", missing.Guid);
            Assert.Equal(varI1.ToTestDisplayString(), missing.FullTypeName);
            Assert.Null(missing.Scope);
            Assert.Null(missing.Identifier);
            Assert.False(missing.IsSerializable);

            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            param = test2.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            missing = (NoPiaMissingCanonicalTypeSymbol)param[0].Type;
            Assert.Same(localTypes2_5, missing.EmbeddingAssembly);
            Assert.Null(missing.Guid);
            Assert.Equal(varS1.ToTestDisplayString(), missing.FullTypeName);
            Assert.Equal("f9c2d51d-4f44-45f0-9eda-c9d599b58257", missing.Scope);
            Assert.Equal(varS1.ToTestDisplayString(), missing.Identifier);

            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies6 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                        TestReferences.SymbolsTests.NoPia.Pia3,
                                        Net40.mscorlib
                                    },
                                new CSharpCompilation[] { localTypes1, localTypes2 });

            var localTypes1_6 = assemblies6[2];
            var localTypes2_6 = assemblies6[3];

            classLocalTypes1 = localTypes1_6.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_6.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            param = test2.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies7 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                        TestReferences.SymbolsTests.NoPia.Pia4,
                                        Net40.mscorlib
                                    },
                                new CSharpCompilation[] { localTypes1, localTypes2 });

            var pia4_7 = assemblies7[0];
            var localTypes1_7 = assemblies7[2];
            var localTypes2_7 = assemblies7[3];

            classLocalTypes1 = localTypes1_7.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_7.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Equal(TypeKind.Interface, param[0].Type.TypeKind);
            Assert.Equal(TypeKind.Interface, param[1].Type.TypeKind);
            Assert.NotEqual(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.Same(pia4_7.GlobalNamespace.GetTypeMembers("I1").Single(), param[0].Type);
            Assert.Same(pia4_7, param[1].Type.ContainingAssembly);
            Assert.Equal("NS1.I2", param[1].Type.ToTestDisplayString());

            param = test2.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies8 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                        TestReferences.SymbolsTests.NoPia.Pia4,
                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                        Net40.mscorlib
                                    },
                                new CSharpCompilation[] { localTypes1, localTypes2 });

            var localTypes1_8 = assemblies8[3];
            var localTypes2_8 = assemblies8[4];
            var pia4_8 = assemblies8[0];
            var pia1_8 = assemblies8[1];

            classLocalTypes1 = localTypes1_8.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_8.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            NoPiaAmbiguousCanonicalTypeSymbol ambiguous;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            ambiguous = (NoPiaAmbiguousCanonicalTypeSymbol)param[0].Type;
            Assert.Same(localTypes1_8, ambiguous.EmbeddingAssembly);
            Assert.Same(pia4_8.GlobalNamespace.GetTypeMembers("I1").Single(), ambiguous.FirstCandidate);
            Assert.Same(pia1_8.GlobalNamespace.GetTypeMembers("I1").Single(), ambiguous.SecondCandidate);

            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaAmbiguousCanonicalTypeSymbol>(param[1].Type);

            var assemblies9 = MetadataTestHelpers.GetSymbolsForReferences(new MetadataReference[]
                                {
                    TestReferences.SymbolsTests.NoPia.Library1,
                    TestReferences.SymbolsTests.NoPia.Pia4,
                    Net40.mscorlib,
                    new CSharpCompilationReference(localTypes1)
            });

            var library1_9 = assemblies9[0];
            var localTypes1_9 = assemblies9[3];

            Assert.Equal("LocalTypes1", localTypes1_9.Identity.Name);

            var assemblies10 = MetadataTestHelpers.GetSymbolsForReferences(
                TestReferences.SymbolsTests.NoPia.Library1,
                TestReferences.SymbolsTests.NoPia.Pia4,
                Net40.mscorlib,
                TestReferences.SymbolsTests.MDTestLib1,
                new CSharpCompilationReference(localTypes1));

            var library1_10 = assemblies10[0];
            var localTypes1_10 = assemblies10[4];

            Assert.Equal("LocalTypes1", localTypes1_10.Identity.Name);

            Assert.NotSame(library1_9, library1_10);
            Assert.NotSame(localTypes1_9, localTypes1_10);

            GC.KeepAlive(localTypes1_1);
            GC.KeepAlive(localTypes2_1);
            GC.KeepAlive(pia1_1);
            GC.KeepAlive(localTypes1_9);
            GC.KeepAlive(library1_9);
        }

        [Fact]
        public void CyclicReference()
        {
            var mscorlibRef = TestReferences.SymbolsTests.MDTestLib1;
            var cyclic2Ref = TestReferences.SymbolsTests.Cyclic.Cyclic2.dll;
            var piaRef = TestReferences.SymbolsTests.NoPia.Pia1;
            var localTypes1Ref = TestReferences.SymbolsTests.NoPia.LocalTypes1;

            var tc1 = CSharpCompilation.Create("Cyclic1", references: new MetadataReference[] { mscorlibRef, cyclic2Ref, piaRef, localTypes1Ref });
            Assert.NotNull(tc1.Assembly); // force creation of SourceAssemblySymbol

            var tc2 = CSharpCompilation.Create("Cyclic1", references: new MetadataReference[] { mscorlibRef, cyclic2Ref, piaRef, localTypes1Ref });
            Assert.NotNull(tc2.Assembly); // force creation of SourceAssemblySymbol

            Assert.NotSame(tc1.GetReferencedAssemblySymbol(localTypes1Ref),
                            tc2.GetReferencedAssemblySymbol(localTypes1Ref));

            GC.KeepAlive(tc1);
            GC.KeepAlive(tc2);
        }

        [Fact]
        public void GenericsClosedOverLocalTypes1()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                {
                TestReferences.SymbolsTests.NoPia.LocalTypes3,
                TestReferences.SymbolsTests.NoPia.Pia1
            });

            var asmLocalTypes3 = assemblies[0];
            var localTypes3 = asmLocalTypes3.GlobalNamespace.GetTypeMembers("LocalTypes3").Single();

            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test1").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test2").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.Equal(SymbolKind.ErrorType, localTypes3.GetMembers("Test3").OfType<MethodSymbol>().Single().ReturnType.Kind);

            NoPiaIllegalGenericInstantiationSymbol illegal = (NoPiaIllegalGenericInstantiationSymbol)localTypes3.GetMembers("Test3").OfType<MethodSymbol>().Single().ReturnType;
            Assert.Equal("C31<I1>.I31<C33>", illegal.UnderlyingSymbol.ToTestDisplayString());

            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test4").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test5").OfType<MethodSymbol>().Single().ReturnType);

            assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                {
                TestReferences.SymbolsTests.NoPia.LocalTypes3,
                TestReferences.SymbolsTests.NoPia.Pia1,
                Net40.mscorlib
            });

            localTypes3 = assemblies[0].GlobalNamespace.GetTypeMembers("LocalTypes3").Single();

            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test1").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test2").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test3").OfType<MethodSymbol>().Single().ReturnType);
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test4").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test5").OfType<MethodSymbol>().Single().ReturnType);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test6").OfType<MethodSymbol>().Single().ReturnType);
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void GenericsClosedOverLocalTypes2()
        {
            var mscorlibRef = Net40.mscorlib;
            var pia5Link = TestReferences.SymbolsTests.NoPia.Pia5.WithEmbedInteropTypes(true);
            var pia5Ref = TestReferences.SymbolsTests.NoPia.Pia5.WithEmbedInteropTypes(false);
            var library2Ref = TestReferences.SymbolsTests.NoPia.Library2.WithEmbedInteropTypes(false);
            var library2Link = TestReferences.SymbolsTests.NoPia.Library2.WithEmbedInteropTypes(true);
            var pia1Link = TestReferences.SymbolsTests.NoPia.Pia1.WithEmbedInteropTypes(true);
            var pia1Ref = TestReferences.SymbolsTests.NoPia.Pia1.WithEmbedInteropTypes(false);

            Assert.True(pia1Link.Properties.EmbedInteropTypes);
            Assert.False(pia1Ref.Properties.EmbedInteropTypes);
            Assert.True(pia5Link.Properties.EmbedInteropTypes);
            Assert.False(pia5Ref.Properties.EmbedInteropTypes);
            Assert.True(library2Link.Properties.EmbedInteropTypes);
            Assert.False(library2Ref.Properties.EmbedInteropTypes);

            var tc1 = CSharpCompilation.Create("C1", references: new MetadataReference[] { mscorlibRef, pia5Link });

            var pia5Asm1 = tc1.GetReferencedAssemblySymbol(pia5Link);

            Assert.True(pia5Asm1.IsLinked);

            var varI5_1 = pia5Asm1.GlobalNamespace.GetTypeMembers("I5").Single();

            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(varI5_1.GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType);

            var tc2 = CSharpCompilation.Create("C1", references: new MetadataReference[] { mscorlibRef, pia5Ref });

            var pia5Asm2 = tc2.GetReferencedAssemblySymbol(pia5Ref);

            Assert.False(pia5Asm2.IsLinked);
            Assert.NotSame(pia5Asm1, pia5Asm2);

            var varI5_2 = pia5Asm2.GlobalNamespace.GetTypeMembers("I5").Single();
            Assert.NotEqual(SymbolKind.ErrorType, varI5_2.GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.Kind);

            var tc3 = CSharpCompilation.Create("C1", references: new MetadataReference[] { mscorlibRef, library2Ref, pia5Link, pia1Ref });

            var pia5Asm3 = tc3.GetReferencedAssemblySymbol(pia5Link);
            var library2Asm3 = tc3.GetReferencedAssemblySymbol(library2Ref);

            Assert.True(pia5Asm3.IsLinked);
            Assert.False(library2Asm3.IsLinked);

            Assert.Same(pia5Asm1, pia5Asm3);

            var varI7_3 = library2Asm3.GlobalNamespace.GetTypeMembers("I7").Single();
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(varI7_3.GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType);
            Assert.NotEqual(SymbolKind.ErrorType, varI7_3.GetMembers("Bar").OfType<MethodSymbol>().Single().ReturnType.Kind);

            var tc4 = CSharpCompilation.Create("C1", references: new MetadataReference[] { mscorlibRef, library2Ref, pia5Ref, pia1Ref });

            var pia5Asm4 = tc4.GetReferencedAssemblySymbol(pia5Ref);
            var library2Asm4 = tc4.GetReferencedAssemblySymbol(library2Ref);

            Assert.False(pia5Asm4.IsLinked);
            Assert.False(library2Asm4.IsLinked);

            Assert.NotSame(pia5Asm3, pia5Asm4);
            Assert.Same(pia5Asm2, pia5Asm4);
            Assert.NotSame(library2Asm3, library2Asm4);

            var varI7_4 = library2Asm4.GlobalNamespace.GetTypeMembers("I7").Single();
            Assert.NotEqual(SymbolKind.ErrorType, varI7_4.GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, varI7_4.GetMembers("Bar").OfType<MethodSymbol>().Single().ReturnType.Kind);

            var tc5 = CSharpCompilation.Create("C1", references: new MetadataReference[] { mscorlibRef, library2Ref, pia5Link, pia1Link });

            var pia1Asm5 = tc5.GetReferencedAssemblySymbol(pia1Link);
            var pia5Asm5 = tc5.GetReferencedAssemblySymbol(pia5Link);
            var library2Asm5 = tc5.GetReferencedAssemblySymbol(library2Ref);

            Assert.True(pia1Asm5.IsLinked);
            Assert.True(pia5Asm5.IsLinked);
            Assert.False(library2Asm5.IsLinked);

            Assert.Same(pia5Asm1, pia5Asm5);
            Assert.NotSame(library2Asm5, library2Asm3);
            Assert.NotSame(library2Asm5, library2Asm4);

            var varI7_5 = library2Asm5.GlobalNamespace.GetTypeMembers("I7").Single();
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(varI7_5.GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(varI7_5.GetMembers("Bar").OfType<MethodSymbol>().Single().ReturnType);

            var tc6 = CSharpCompilation.Create("C1", references: new MetadataReference[] { mscorlibRef, library2Ref, pia5Link, pia1Ref });

            var pia1Asm6 = tc6.GetReferencedAssemblySymbol(pia1Ref);
            var pia5Asm6 = tc6.GetReferencedAssemblySymbol(pia5Link);
            var library2Asm6 = tc6.GetReferencedAssemblySymbol(library2Ref);

            Assert.False(pia1Asm6.IsLinked);
            Assert.True(pia5Asm6.IsLinked);
            Assert.False(library2Asm6.IsLinked);

            Assert.Same(pia5Asm1, pia5Asm6);
            Assert.Same(library2Asm6, library2Asm3);

            var tc7 = CSharpCompilation.Create("C1", references: new MetadataReference[] { mscorlibRef, library2Link, pia5Link, pia1Ref });

            var pia5Asm7 = tc7.GetReferencedAssemblySymbol(pia5Link);
            var library2Asm7 = tc7.GetReferencedAssemblySymbol(library2Link);

            Assert.True(pia5Asm7.IsLinked);
            Assert.True(library2Asm7.IsLinked);

            Assert.Same(pia5Asm1, pia5Asm3);
            Assert.NotSame(library2Asm7, library2Asm3);
            Assert.NotSame(library2Asm7, library2Asm4);
            Assert.NotSame(library2Asm7, library2Asm5);

            var varI7_7 = library2Asm7.GlobalNamespace.GetTypeMembers("I7").Single();
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(varI7_7.GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType);
            Assert.NotEqual(SymbolKind.ErrorType, varI7_7.GetMembers("Bar").OfType<MethodSymbol>().Single().ReturnType.Kind);

            GC.KeepAlive(tc1);
            GC.KeepAlive(tc2);
            GC.KeepAlive(tc3);
            GC.KeepAlive(tc4);
            GC.KeepAlive(tc5);
            GC.KeepAlive(tc6);
            GC.KeepAlive(tc7);
        }

        [Fact]
        public void GenericsClosedOverLocalTypes3()
        {
            var varmscorlibRef = Net40.mscorlib;
            var varALink = TestReferences.SymbolsTests.NoPia.A.WithEmbedInteropTypes(true);
            var varARef = TestReferences.SymbolsTests.NoPia.A.WithEmbedInteropTypes(false);
            var varBLink = TestReferences.SymbolsTests.NoPia.B.WithEmbedInteropTypes(true);
            var varBRef = TestReferences.SymbolsTests.NoPia.B.WithEmbedInteropTypes(false);
            var varCLink = TestReferences.SymbolsTests.NoPia.C.WithEmbedInteropTypes(true);
            var varCRef = TestReferences.SymbolsTests.NoPia.C.WithEmbedInteropTypes(false);
            var varDLink = TestReferences.SymbolsTests.NoPia.D.WithEmbedInteropTypes(true);
            var varDRef = TestReferences.SymbolsTests.NoPia.D.WithEmbedInteropTypes(false);

            var tc1 = CSharpCompilation.Create("C1", references: new MetadataReference[] { varmscorlibRef, varCRef, varARef, varBLink });

            var varA1 = tc1.GetReferencedAssemblySymbol(varARef);
            var varB1 = tc1.GetReferencedAssemblySymbol(varBLink);
            var varC1 = tc1.GetReferencedAssemblySymbol(varCRef);

            var tc2 = CSharpCompilation.Create("C2", references: new MetadataReference[] { varmscorlibRef, varCRef, varARef, varDRef, varBLink });

            Assert.Same(varA1, tc2.GetReferencedAssemblySymbol(varARef));
            Assert.Same(varB1, tc2.GetReferencedAssemblySymbol(varBLink));
            Assert.Same(varC1, tc2.GetReferencedAssemblySymbol(varCRef));

            var varD2 = tc2.GetReferencedAssemblySymbol(varDRef);

            var tc3 = CSharpCompilation.Create("C3", references: new MetadataReference[] { varmscorlibRef, varCRef, varBLink });

            Assert.Same(varB1, tc3.GetReferencedAssemblySymbol(varBLink));
            Assert.True(tc3.GetReferencedAssemblySymbol(varCRef).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC1));

            var tc4 = CSharpCompilation.Create("C4", references: new MetadataReference[] { varmscorlibRef, varCRef, varARef, varBRef });

            Assert.Same(varA1, tc4.GetReferencedAssemblySymbol(varARef));

            var varB4 = tc4.GetReferencedAssemblySymbol(varBRef);
            var varC4 = tc4.GetReferencedAssemblySymbol(varCRef);

            Assert.NotSame(varC1, varC4);
            Assert.NotSame(varB1, varB4); // Link and Ref use different symbols.

            var tc5 = CSharpCompilation.Create("C5", references: new MetadataReference[] { varmscorlibRef, varCRef, varALink, varBLink });

            Assert.Same(varB1, tc5.GetReferencedAssemblySymbol(varBLink));

            var varA5 = tc5.GetReferencedAssemblySymbol(varALink);
            var varC5 = tc5.GetReferencedAssemblySymbol(varCRef);

            Assert.NotSame(varA1, varA5); // Link and Ref use different symbols.
            Assert.NotSame(varC1, varC5);
            Assert.NotSame(varC4, varC5);

            var tc6 = CSharpCompilation.Create("C6", references: new MetadataReference[] { varmscorlibRef, varARef, varBLink, varCLink });

            Assert.Same(varA1, tc6.GetReferencedAssemblySymbol(varARef));
            Assert.Same(varB1, tc6.GetReferencedAssemblySymbol(varBLink));

            var varC6 = tc6.GetReferencedAssemblySymbol(varCLink);

            Assert.NotSame(varC1, varC6); // Link and Ref use different symbols.
            Assert.NotSame(varC4, varC6); // Link and Ref use different symbols.
            Assert.NotSame(varC5, varC6); // Link and Ref use different symbols.

            var tc7 = CSharpCompilation.Create("C7", references: new MetadataReference[] { varmscorlibRef, varCRef, varARef });

            Assert.Same(varA1, tc7.GetReferencedAssemblySymbol(varARef));
            Assert.True(tc7.GetReferencedAssemblySymbol(varCRef).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC4));

            GC.KeepAlive(tc1);
            GC.KeepAlive(tc2);
            GC.KeepAlive(tc3);
            GC.KeepAlive(tc4);
            GC.KeepAlive(tc5);
            GC.KeepAlive(tc6);
            GC.KeepAlive(tc7);
        }

        [Fact]
        public void GenericsClosedOverLocalTypes4()
        {
            string localTypes3Source = @"
using System.Collections.Generic;

public class LocalTypes3
{
    public C31<C33>.I31<C33> Test1()
    {
        return null;
    }

    public C31<C33>.I31<I1> Test2()
    {
        return null;
    }

    public C31<I1>.I31<C33> Test3()
    {
        return null;
    }

    public C31<C33>.I31<I32<I1>> Test4()
    {
        return null;
    }

    public C31<I32<I1>>.I31<C33> Test5()
    {
        return null;
    }

    public List<I1> Test6()
    {
        return null;
    }

}


public class C31<T>
{
    public interface I31<S>
    {}
}

public class C32<T>
{}

public interface I32<S>
{}

public class C33
{}
";

            var mscorlibRef = Net40.mscorlib;
            var pia1CopyLink = TestReferences.SymbolsTests.NoPia.Pia1Copy.WithEmbedInteropTypes(true);
            var pia1CopyRef = TestReferences.SymbolsTests.NoPia.Pia1Copy.WithEmbedInteropTypes(false);

            // vbc /t:library /vbruntime- LocalTypes3.vb /l:Pia1.dll
            var varC_LocalTypes3 = CSharpCompilation.Create("LocalTypes3", new[] { Parse(localTypes3Source) }, new[] { mscorlibRef, pia1CopyLink });

            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[] { TestReferences.SymbolsTests.NoPia.Pia1 },
                new[] { varC_LocalTypes3 });

            var asmLocalTypes3 = assemblies[1];
            var localTypes3 = asmLocalTypes3.GlobalNamespace.GetTypeMembers("LocalTypes3").Single();

            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test1").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test2").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.Equal(SymbolKind.ErrorType, localTypes3.GetMembers("Test3").OfType<MethodSymbol>().Single().ReturnType.Kind);

            NoPiaIllegalGenericInstantiationSymbol illegal = (NoPiaIllegalGenericInstantiationSymbol)localTypes3.GetMembers("Test3").OfType<MethodSymbol>().Single().ReturnType;
            Assert.Equal("C31<I1>.I31<C33>", illegal.UnderlyingSymbol.ToTestDisplayString());

            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test4").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test5").OfType<MethodSymbol>().Single().ReturnType);

            assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                {
                                    TestReferences.SymbolsTests.NoPia.Pia1,
                                    Net40.mscorlib
                                },
                                new CSharpCompilation[] { varC_LocalTypes3 });

            localTypes3 = assemblies[2].GlobalNamespace.GetTypeMembers("LocalTypes3").Single();

            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test1").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test2").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test3").OfType<MethodSymbol>().Single().ReturnType);
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test4").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test5").OfType<MethodSymbol>().Single().ReturnType);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test6").OfType<MethodSymbol>().Single().ReturnType);
        }

        [Fact]
        public void GenericsClosedOverLocalTypes5()
        {
            string pia5Source = @"
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58259"")]
[assembly: ImportedFromTypeLib(""Pia5.dll"")]


[ComImport(), Guid(""27e3e649-994b-4f58-b3c6-f8089a5f2c05""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface I5
{
    List<I6> Foo();
}

[ComImport(), Guid(""27e3e649-994b-4f58-b3c6-f8089a5f2c06""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface I6
{ }
";

            string pia1Source = @"
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[assembly: ImportedFromTypeLib(""Pia1.dll"")]


[ComImport, Guid(""27e3e649-994b-4f58-b3c6-f8089a5f2c01""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface I1
{
    void Sub1(int x);
}
";

            string library2Source = @"
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58260"")]
[assembly: ImportedFromTypeLib(""Library2.dll"")]


[ComImport(), Guid(""27e3e649-994b-4f58-b3c6-f8089a5f2002""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface I7
{
    List<I5> Foo();
    List<I1> Bar();
}
";

            var mscorlibRef = Net40.mscorlib;

            // vbc /t:library /vbruntime- Pia5.vb
            var varC_Pia5 = CSharpCompilation.Create("Pia5", new[] { Parse(pia5Source) }, new[] { mscorlibRef });

            var pia5Link = new CSharpCompilationReference(varC_Pia5, embedInteropTypes: true);
            var pia5Ref = new CSharpCompilationReference(varC_Pia5, embedInteropTypes: false);
            Assert.True(pia5Link.Properties.EmbedInteropTypes);
            Assert.False(pia5Ref.Properties.EmbedInteropTypes);

            // vbc /t:library /vbruntime- Pia1.vb
            var varC_Pia1 = CSharpCompilation.Create("Pia1", new[] { Parse(pia1Source) }, new[] { mscorlibRef });

            var pia1Link = new CSharpCompilationReference(varC_Pia1, embedInteropTypes: true);
            var pia1Ref = new CSharpCompilationReference(varC_Pia1, embedInteropTypes: false);
            Assert.True(pia1Link.Properties.EmbedInteropTypes);
            Assert.False(pia1Ref.Properties.EmbedInteropTypes);

            // vbc /t:library /vbruntime- /r:Pia1.dll,Pia5.dll Library2.vb
            var varC_Library2 = CSharpCompilation.Create("Library2", new[] { Parse(library2Source) }, new MetadataReference[] { mscorlibRef, pia1Ref, pia5Ref });

            var library2Ref = new CSharpCompilationReference(varC_Library2, embedInteropTypes: false);
            var library2Link = new CSharpCompilationReference(varC_Library2, embedInteropTypes: true);
            Assert.True(library2Link.Properties.EmbedInteropTypes);
            Assert.False(library2Ref.Properties.EmbedInteropTypes);

            var tc1 = CSharpCompilation.Create("C1", new SyntaxTree[0], new MetadataReference[] { mscorlibRef, pia5Link });

            var pia5Asm1 = tc1.GetReferencedAssemblySymbol(pia5Link);

            Assert.True(pia5Asm1.IsLinked);

            var varI5_1 = pia5Asm1.GlobalNamespace.GetTypeMembers("I5").Single();

            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(varI5_1.GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType);

            var tc2 = CSharpCompilation.Create("C1", new SyntaxTree[0], new MetadataReference[] { mscorlibRef, pia5Ref });

            var pia5Asm2 = tc2.GetReferencedAssemblySymbol(pia5Ref);

            Assert.False(pia5Asm2.IsLinked);
            Assert.NotSame(pia5Asm1, pia5Asm2);

            var varI5_2 = pia5Asm2.GlobalNamespace.GetTypeMembers("I5").Single();
            Assert.NotEqual(SymbolKind.ErrorType, varI5_2.GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.Kind);

            var tc3 = CSharpCompilation.Create("C1", new SyntaxTree[0], new MetadataReference[] { mscorlibRef, library2Ref, pia5Link, pia1Ref });

            var pia5Asm3 = tc3.GetReferencedAssemblySymbol(pia5Link);
            var library2Asm3 = tc3.GetReferencedAssemblySymbol(library2Ref);

            Assert.True(pia5Asm3.IsLinked);
            Assert.False(library2Asm3.IsLinked);

            Assert.Same(pia5Asm1, pia5Asm3);

            var varI7_3 = library2Asm3.GlobalNamespace.GetTypeMembers("I7").Single();
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(varI7_3.GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType);
            Assert.NotEqual(SymbolKind.ErrorType, varI7_3.GetMembers("Bar").OfType<MethodSymbol>().Single().ReturnType.Kind);

            var tc4 = CSharpCompilation.Create("C1", new SyntaxTree[0], new MetadataReference[] { mscorlibRef, library2Ref, pia5Ref, pia1Ref });

            var pia5Asm4 = tc4.GetReferencedAssemblySymbol(pia5Ref);
            var library2Asm4 = tc4.GetReferencedAssemblySymbol(library2Ref);

            Assert.False(pia5Asm4.IsLinked);
            Assert.False(library2Asm4.IsLinked);

            Assert.NotSame(pia5Asm3, pia5Asm4);
            Assert.Same(pia5Asm2, pia5Asm4);
            Assert.NotSame(library2Asm3, library2Asm4);

            var varI7_4 = library2Asm4.GlobalNamespace.GetTypeMembers("I7").Single();
            Assert.NotEqual(SymbolKind.ErrorType, varI7_4.GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, varI7_4.GetMembers("Bar").OfType<MethodSymbol>().Single().ReturnType.Kind);

            var tc5 = CSharpCompilation.Create("C1", new SyntaxTree[0], new MetadataReference[] { mscorlibRef, library2Ref, pia5Link, pia1Link });

            var pia1Asm5 = tc5.GetReferencedAssemblySymbol(pia1Link);
            var pia5Asm5 = tc5.GetReferencedAssemblySymbol(pia5Link);
            var library2Asm5 = tc5.GetReferencedAssemblySymbol(library2Ref);

            Assert.True(pia1Asm5.IsLinked);
            Assert.True(pia5Asm5.IsLinked);
            Assert.False(library2Asm5.IsLinked);

            Assert.Same(pia5Asm1, pia5Asm5);
            Assert.NotSame(library2Asm5, library2Asm3);
            Assert.NotSame(library2Asm5, library2Asm4);

            var varI7_5 = library2Asm5.GlobalNamespace.GetTypeMembers("I7").Single();
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(varI7_5.GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(varI7_5.GetMembers("Bar").OfType<MethodSymbol>().Single().ReturnType);

            var tc6 = CSharpCompilation.Create("C1", new SyntaxTree[0], new MetadataReference[] { mscorlibRef, library2Ref, pia5Link, pia1Ref });

            var pia1Asm6 = tc6.GetReferencedAssemblySymbol(pia1Ref);
            var pia5Asm6 = tc6.GetReferencedAssemblySymbol(pia5Link);
            var library2Asm6 = tc6.GetReferencedAssemblySymbol(library2Ref);

            Assert.False(pia1Asm6.IsLinked);
            Assert.True(pia5Asm6.IsLinked);
            Assert.False(library2Asm6.IsLinked);

            Assert.Same(pia5Asm1, pia5Asm6);
            Assert.Same(library2Asm6, library2Asm3);

            var tc7 = CSharpCompilation.Create("C1", new SyntaxTree[0], new MetadataReference[] { mscorlibRef, library2Link, pia5Link, pia1Ref });

            var pia5Asm7 = tc7.GetReferencedAssemblySymbol(pia5Link);
            var library2Asm7 = tc7.GetReferencedAssemblySymbol(library2Link);

            Assert.True(pia5Asm7.IsLinked);
            Assert.True(library2Asm7.IsLinked);

            Assert.Same(pia5Asm1, pia5Asm3);
            Assert.NotSame(library2Asm7, library2Asm3);
            Assert.NotSame(library2Asm7, library2Asm4);
            Assert.NotSame(library2Asm7, library2Asm5);

            var varI7_7 = library2Asm7.GlobalNamespace.GetTypeMembers("I7").Single();
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(varI7_7.GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType);
            Assert.NotEqual(SymbolKind.ErrorType, varI7_7.GetMembers("Bar").OfType<MethodSymbol>().Single().ReturnType.Kind);

            GC.KeepAlive(tc1);
            GC.KeepAlive(tc2);
            GC.KeepAlive(tc3);
            GC.KeepAlive(tc4);
            GC.KeepAlive(tc5);
            GC.KeepAlive(tc6);
            GC.KeepAlive(tc7);

            var varI5 = varC_Pia5.SourceModule.GlobalNamespace.GetTypeMembers("I5").Single();
            var varI5_Foo = varI5.GetMembers("Foo").OfType<MethodSymbol>().Single();
            var varI6 = varC_Pia5.SourceModule.GlobalNamespace.GetTypeMembers("I6").Single();

            Assert.NotEqual(SymbolKind.ErrorType, varI5.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, varI6.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, varI5_Foo.ReturnType.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, ((NamedTypeSymbol)varI5_Foo.ReturnType).TypeArguments()[0].Kind);
            Assert.Equal("System.Collections.Generic.List<I6>", varI5_Foo.ReturnType.ToTestDisplayString());

            var varI7 = varC_Library2.SourceModule.GlobalNamespace.GetTypeMembers("I7").Single();
            var varI7_Foo = varI7.GetMembers("Foo").OfType<MethodSymbol>().Single();
            var varI7_Bar = varI7.GetMembers("Bar").OfType<MethodSymbol>().Single();

            Assert.NotEqual(SymbolKind.ErrorType, varI7_Foo.ReturnType.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, ((NamedTypeSymbol)varI7_Foo.ReturnType).TypeArguments()[0].Kind);
            Assert.Equal("System.Collections.Generic.List<I5>", varI7_Foo.ReturnType.ToTestDisplayString());

            Assert.NotEqual(SymbolKind.ErrorType, varI7_Bar.ReturnType.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, ((NamedTypeSymbol)varI7_Bar.ReturnType).TypeArguments()[0].Kind);
            Assert.Equal("System.Collections.Generic.List<I1>", varI7_Bar.ReturnTypeWithAnnotations.ToTestDisplayString());

            var varI1 = varC_Pia1.SourceModule.GlobalNamespace.GetTypeMembers("I1").Single();

            Assert.NotEqual(SymbolKind.ErrorType, varI1.Kind);
        }

        [Fact]
        public void GenericsClosedOverLocalTypes6()
        {
            var mscorlibRef = Net40.mscorlib;

            var varC_A = CSharpCompilation.Create("A", references: new[] { mscorlibRef });

            var varALink = new CSharpCompilationReference(varC_A, embedInteropTypes: true);
            var varARef = new CSharpCompilationReference(varC_A, embedInteropTypes: false);

            var varC_B = CSharpCompilation.Create("B", references: new[] { mscorlibRef });

            var varBLink = new CSharpCompilationReference(varC_B, embedInteropTypes: true);
            var varBRef = new CSharpCompilationReference(varC_B, embedInteropTypes: false);

            var varC_C = CSharpCompilation.Create("C", references: new MetadataReference[] { mscorlibRef, varARef, varBRef });

            var varCLink = new CSharpCompilationReference(varC_C, embedInteropTypes: true);
            var varCRef = new CSharpCompilationReference(varC_C, embedInteropTypes: false);

            var varC_D = CSharpCompilation.Create("D", references: new MetadataReference[] { mscorlibRef });

            var varDLink = new CSharpCompilationReference(varC_D, embedInteropTypes: true);
            var varDRef = new CSharpCompilationReference(varC_D, embedInteropTypes: false);

            var tc1 = CSharpCompilation.Create("C1", references: new MetadataReference[] { mscorlibRef, varCRef, varARef, varBLink });

            var varA1 = tc1.GetReferencedAssemblySymbol(varARef);
            var varB1 = tc1.GetReferencedAssemblySymbol(varBLink);
            var varC1 = tc1.GetReferencedAssemblySymbol(varCRef);

            var tc2 = CSharpCompilation.Create("C2", references: new MetadataReference[] { mscorlibRef, varCRef, varARef, varDRef, varBLink });

            Assert.Same(varA1, tc2.GetReferencedAssemblySymbol(varARef));
            Assert.Same(varB1, tc2.GetReferencedAssemblySymbol(varBLink));
            Assert.Same(varC1, tc2.GetReferencedAssemblySymbol(varCRef));

            var varD2 = tc2.GetReferencedAssemblySymbol(varDRef);

            var tc3 = CSharpCompilation.Create("C3", references: new MetadataReference[] { mscorlibRef, varCRef, varBLink });

            Assert.Same(varB1, tc3.GetReferencedAssemblySymbol(varBLink));
            Assert.True(tc3.GetReferencedAssemblySymbol(varCRef).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC1));

            var tc4 = CSharpCompilation.Create("C4", references: new MetadataReference[] { mscorlibRef, varCRef, varARef, varBRef });

            Assert.Same(varA1, tc4.GetReferencedAssemblySymbol(varARef));

            var varB4 = tc4.GetReferencedAssemblySymbol(varBRef);
            var varC4 = tc4.GetReferencedAssemblySymbol(varCRef);

            Assert.NotSame(varC1, varC4);
            Assert.NotSame(varB1, varB4); // Link and Ref use different symbols.

            var tc5 = CSharpCompilation.Create("C5", references: new MetadataReference[] { mscorlibRef, varCRef, varALink, varBLink });

            Assert.Same(varB1, tc5.GetReferencedAssemblySymbol(varBLink));

            var varA5 = tc5.GetReferencedAssemblySymbol(varALink);
            var varC5 = tc5.GetReferencedAssemblySymbol(varCRef);

            Assert.NotSame(varA1, varA5); // Link and Ref use different symbols.
            Assert.NotSame(varC1, varC5);
            Assert.NotSame(varC4, varC5);

            var tc6 = CSharpCompilation.Create("C6", references: new MetadataReference[] { mscorlibRef, varARef, varBLink, varCLink });

            Assert.Same(varA1, tc6.GetReferencedAssemblySymbol(varARef));
            Assert.Same(varB1, tc6.GetReferencedAssemblySymbol(varBLink));

            var varC6 = tc6.GetReferencedAssemblySymbol(varCLink);

            Assert.NotSame(varC1, varC6); // Link and Ref use different symbols.
            Assert.NotSame(varC4, varC6); // Link and Ref use different symbols.
            Assert.NotSame(varC5, varC6); // Link and Ref use different symbols.

            var tc7 = CSharpCompilation.Create("C7", references: new MetadataReference[] { mscorlibRef, varCRef, varARef });

            Assert.Same(varA1, tc7.GetReferencedAssemblySymbol(varARef));
            Assert.True(tc7.GetReferencedAssemblySymbol(varCRef).RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC4));

            GC.KeepAlive(tc1);
            GC.KeepAlive(tc2);
            GC.KeepAlive(tc3);
            GC.KeepAlive(tc4);
            GC.KeepAlive(tc5);
            GC.KeepAlive(tc6);
            GC.KeepAlive(tc7);
        }

        [Fact]
        [WorkItem(62863, "https://github.com/dotnet/roslyn/issues/62863")]
        public void ExplicitInterfaceImplementations()
        {
            var sourcePIA =
@"using System.Runtime.InteropServices;
[assembly: PrimaryInteropAssembly(0, 0)]
[assembly: Guid(""863D5BC0-46A1-49AC-97AA-A5F0D441A9DA"")]
[ComImport]
[Guid(""863D5BC0-46A1-49AD-97AA-A5F0D441A9DA"")]
public interface I1
{
    int F1();
}
";
            var sourceBase =
@"
public class C
{
    public long F1() => 0;
}

public class Base : C, I1
{
    int I1.F1()
    {
        throw new System.NotImplementedException();
    }
}
";
            var compilationPIA = CreateCompilation(sourcePIA, options: TestOptions.DebugDll);
            compilationPIA.VerifyDiagnostics();

            var referencePIAImage = compilationPIA.EmitToImageReference(embedInteropTypes: true);
            var referencePIASource = compilationPIA.ToMetadataReference(embedInteropTypes: true);

            var compilationBase = CreateCompilation(sourceBase, new[] { referencePIASource }, TestOptions.DebugDll);
            compilationBase.VerifyDiagnostics();

            var referenceBaseImage = compilationBase.EmitToImageReference();
            var referenceBaseSource = compilationBase.ToMetadataReference();

            var sourceDerived =
@"
public interface I2 : I1
{ }

public class Derived : Base, I2
{
}
";
            var compilationDerived1 = CreateCompilation(sourceDerived, new[] { referencePIASource, referenceBaseSource }, TestOptions.DebugDll);
            verify(compilationDerived1);

            var compilationDerived2 = CreateCompilation(sourceDerived, new[] { referencePIAImage, referenceBaseSource }, TestOptions.DebugDll);
            verify(compilationDerived2);

            var compilationDerived3 = CreateCompilation(sourceDerived, new[] { referencePIASource, referenceBaseImage }, TestOptions.DebugDll);
            verify(compilationDerived3);

            var compilationDerived4 = CreateCompilation(sourceDerived, new[] { referencePIAImage, referenceBaseImage }, TestOptions.DebugDll);
            verify(compilationDerived4);

            static void verify(CSharpCompilation compilationDerived)
            {
                var i1F1 = compilationDerived.GetTypeByMetadataName("I1").GetMember<MethodSymbol>("F1");
                var baseI1F1 = compilationDerived.GetTypeByMetadataName("Base").GetMember<MethodSymbol>("I1.F1");
                Assert.Same(i1F1, baseI1F1.ExplicitInterfaceImplementations.Single());
                compilationDerived.VerifyDiagnostics();
            }
        }
    }
}
