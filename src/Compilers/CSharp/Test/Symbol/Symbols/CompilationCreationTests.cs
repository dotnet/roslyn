// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CompilationCreationTests : CSharpTestBase
    {
        #region Helpers

        private static SyntaxTree CreateSyntaxTree(string className)
        {
            var text = string.Format("public partial class {0} {{ }}", className);
            var path = string.Format("{0}.cs", className);
            return SyntaxFactory.ParseSyntaxTree(text, path: path);
        }

        private static void CheckCompilationSyntaxTrees(CSharpCompilation compilation, params SyntaxTree[] expectedSyntaxTrees)
        {
            ImmutableArray<SyntaxTree> actualSyntaxTrees = compilation.SyntaxTrees;

            int numTrees = expectedSyntaxTrees.Length;

            Assert.Equal(numTrees, actualSyntaxTrees.Length);
            for (int i = 0; i < numTrees; i++)
            {
                Assert.Equal(expectedSyntaxTrees[i], actualSyntaxTrees[i]);
            }

            for (int i = 0; i < numTrees; i++)
            {
                for (int j = 0; j < numTrees; j++)
                {
                    Assert.Equal(Math.Sign(compilation.CompareSyntaxTreeOrdering(expectedSyntaxTrees[i], expectedSyntaxTrees[j])), Math.Sign(i.CompareTo(j)));
                }
            }

            var types = expectedSyntaxTrees.Select(tree => compilation.GetSemanticModel(tree).GetDeclaredSymbol(tree.GetCompilationUnitRoot().Members.Single())).ToArray();
            for (int i = 0; i < numTrees; i++)
            {
                for (int j = 0; j < numTrees; j++)
                {
                    Assert.Equal(Math.Sign(compilation.CompareSourceLocations(types[i].Locations[0], types[j].Locations[0])), Math.Sign(i.CompareTo(j)));
                }
            }
        }

        #endregion

        [Fact]
        public void CorLibTypes()
        {
            var mdTestLib1 = TestReferences.SymbolsTests.MDTestLib1;

            var c1 = CSharpCompilation.Create("Test", references: new MetadataReference[] { MscorlibRef_v4_0_30316_17626, mdTestLib1 });

            TypeSymbol c107 = c1.GlobalNamespace.GetTypeMembers("C107").Single();

            Assert.Equal(SpecialType.None, c107.SpecialType);

            for (int i = 1; i <= (int)SpecialType.Count; i++)
            {
                NamedTypeSymbol type = c1.GetSpecialType((SpecialType)i);
                Assert.False(type.IsErrorType());
                Assert.Equal((SpecialType)i, type.SpecialType);
            }

            Assert.Equal(SpecialType.None, c107.SpecialType);

            var arrayOfc107 = ArrayTypeSymbol.CreateCSharpArray(c1.Assembly, TypeSymbolWithAnnotations.Create(c107));

            Assert.Equal(SpecialType.None, arrayOfc107.SpecialType);

            var c2 = CSharpCompilation.Create("Test", references: new[] { mdTestLib1 });

            Assert.Equal(SpecialType.None, c2.GlobalNamespace.GetTypeMembers("C107").Single().SpecialType);
        }

        [Fact]
        public void CyclicReference()
        {
            var mscorlibRef = TestReferences.NetFx.v4_0_30319.mscorlib;
            var cyclic2Ref = TestReferences.SymbolsTests.Cyclic.Cyclic2.dll;

            var tc1 = CSharpCompilation.Create("Cyclic1", references: new[] { mscorlibRef, cyclic2Ref });
            Assert.NotNull(tc1.Assembly); // force creation of SourceAssemblySymbol

            var cyclic1Asm = (SourceAssemblySymbol)tc1.Assembly;
            var cyclic1Mod = (SourceModuleSymbol)cyclic1Asm.Modules[0];

            var cyclic2Asm = (PEAssemblySymbol)tc1.GetReferencedAssemblySymbol(cyclic2Ref);
            var cyclic2Mod = (PEModuleSymbol)cyclic2Asm.Modules[0];

            Assert.Same(cyclic2Mod.GetReferencedAssemblySymbols()[1], cyclic1Asm);
            Assert.Same(cyclic1Mod.GetReferencedAssemblySymbols()[1], cyclic2Asm);
        }

        [Fact]
        public void MultiTargeting1()
        {
            var varV1MTTestLib2Ref = TestReferences.SymbolsTests.V1.MTTestLib2.dll;
            var asm1 = MetadataTestHelpers.GetSymbolsForReferences(mrefs: new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    varV1MTTestLib2Ref
                });

            Assert.Equal("mscorlib", asm1[0].Identity.Name);
            Assert.Equal(0, asm1[0].BoundReferences().Length);
            Assert.Equal("MTTestLib2", asm1[1].Identity.Name);
            Assert.Equal(1, (from a in asm1[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm1[1].BoundReferences() where object.ReferenceEquals(a, asm1[0]) select a).Count());
            Assert.Equal(SymbolKind.ErrorType, asm1[1].GlobalNamespace.GetTypeMembers("Class4").
                                  Single().
                                  GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.Kind);

            var asm2 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    varV1MTTestLib2Ref,
                    TestReferences.SymbolsTests.V1.MTTestLib1.dll
                });

            Assert.Same(asm2[0], asm1[0]);

            Assert.Equal("MTTestLib2", asm2[1].Identity.Name);
            Assert.NotSame(asm2[1], asm1[1]);
            Assert.Same(((PEAssemblySymbol)asm2[1]).Assembly, ((PEAssemblySymbol)asm1[1]).Assembly);
            Assert.Equal(2, (from a in asm2[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());
            Assert.Equal(1, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[2]) select a).Count());

            var retval1 = asm2[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval1.Kind);
            Assert.Same(retval1, asm2[2].GlobalNamespace.GetMembers("Class1").Single());

            Assert.Equal("MTTestLib1", asm2[2].Identity.Name);
            Assert.Equal(1, asm2[2].Identity.Version.Major);
            Assert.Equal(1, (from a in asm2[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm2[2].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());

            var varV2MTTestLib3Ref = TestReferences.SymbolsTests.V2.MTTestLib3.dll;
            var asm3 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    varV1MTTestLib2Ref,
                    TestReferences.SymbolsTests.V2.MTTestLib1.dll,
                    varV2MTTestLib3Ref
                });

            Assert.Same(asm3[0], asm1[0]);

            Assert.Equal("MTTestLib2", asm3[1].Identity.Name);
            Assert.NotSame(asm3[1], asm1[1]);
            Assert.NotSame(asm3[1], asm2[1]);
            Assert.Same(((PEAssemblySymbol)asm3[1]).Assembly, ((PEAssemblySymbol)asm1[1]).Assembly);
            Assert.Equal(2, (from a in asm3[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
            Assert.Equal(1, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

            var retval2 = asm3[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval2.Kind);
            Assert.Same(retval2, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

            Assert.Equal("MTTestLib1", asm3[2].Identity.Name);
            Assert.NotSame(asm3[2], asm2[2]);
            Assert.NotSame(((PEAssemblySymbol)asm3[2]).Assembly, ((PEAssemblySymbol)asm2[2]).Assembly);
            Assert.Equal(2, asm3[2].Identity.Version.Major);
            Assert.Equal(1, (from a in asm3[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm3[2].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());

            Assert.Equal("MTTestLib3", asm3[3].Identity.Name);
            Assert.Equal(3, (from a in asm3[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
            Assert.Equal(1, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[1]) select a).Count());
            Assert.Equal(1, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

            var type1 = asm3[3].GlobalNamespace.GetTypeMembers("Class5").
                          Single();

            var retval3 = type1.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval3.Kind);
            Assert.Same(retval3, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

            var retval4 = type1.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval4.Kind);
            Assert.Same(retval4, asm3[2].GlobalNamespace.GetMembers("Class2").Single());

            var retval5 = type1.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval5.Kind);
            Assert.Same(retval5, asm3[1].GlobalNamespace.GetMembers("Class4").Single());

            var varV3MTTestLib4Ref = TestReferences.SymbolsTests.V3.MTTestLib4.dll;
            var asm4 = MetadataTestHelpers.GetSymbolsForReferences(new MetadataReference[]
                {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                varV1MTTestLib2Ref,
                TestReferences.SymbolsTests.V3.MTTestLib1.dll,
                varV2MTTestLib3Ref,
                varV3MTTestLib4Ref
            });

            Assert.Same(asm3[0], asm1[0]);

            Assert.Equal("MTTestLib2", asm4[1].Identity.Name);
            Assert.NotSame(asm4[1], asm1[1]);
            Assert.NotSame(asm4[1], asm2[1]);
            Assert.NotSame(asm4[1], asm3[1]);
            Assert.Same(((PEAssemblySymbol)asm4[1]).Assembly, ((PEAssemblySymbol)asm1[1]).Assembly);
            Assert.Equal(2, (from a in asm4[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(1, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

            var retval6 = asm4[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval6.Kind);
            Assert.Same(retval6, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

            Assert.Equal("MTTestLib1", asm4[2].Identity.Name);
            Assert.NotSame(asm4[2], asm2[2]);
            Assert.NotSame(asm4[2], asm3[2]);
            Assert.NotSame(((PEAssemblySymbol)asm4[2]).Assembly, ((PEAssemblySymbol)asm2[2]).Assembly);
            Assert.NotSame(((PEAssemblySymbol)asm4[2]).Assembly, ((PEAssemblySymbol)asm3[2]).Assembly);
            Assert.Equal(3, asm4[2].Identity.Version.Major);
            Assert.Equal(1, (from a in asm4[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[2].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());

            Assert.Equal("MTTestLib3", asm4[3].Identity.Name);
            Assert.NotSame(asm4[3], asm3[3]);
            Assert.Same(((PEAssemblySymbol)asm4[3]).Assembly, ((PEAssemblySymbol)asm3[3]).Assembly);
            Assert.Equal(3, (from a in asm4[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(1, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
            Assert.Equal(1, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

            var type2 = asm4[3].GlobalNamespace.GetTypeMembers("Class5").
                          Single();

            var retval7 = type2.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval7.Kind);
            Assert.Same(retval7, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

            var retval8 = type2.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval8.Kind);
            Assert.Same(retval8, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

            var retval9 = type2.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval9.Kind);
            Assert.Same(retval9, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

            Assert.Equal("MTTestLib4", asm4[4].Identity.Name);
            Assert.Equal(4, (from a in asm4[4].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[3]) select a).Count());

            var type3 = asm4[4].GlobalNamespace.GetTypeMembers("Class6").
                          Single();

            var retval10 = type3.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval10.Kind);
            Assert.Same(retval10, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

            var retval11 = type3.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval11.Kind);
            Assert.Same(retval11, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

            var retval12 = type3.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval12.Kind);
            Assert.Same(retval12, asm4[2].GlobalNamespace.GetMembers("Class3").Single());

            var retval13 = type3.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval13.Kind);
            Assert.Same(retval13, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

            var retval14 = type3.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval14.Kind);
            Assert.Same(retval14, asm4[3].GlobalNamespace.GetMembers("Class5").Single());

            var asm5 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                varV2MTTestLib3Ref
            });

            Assert.Same(asm5[0], asm1[0]);
            Assert.True(asm5[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm3[3]));

            var asm6 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                varV1MTTestLib2Ref
            });

            Assert.Same(asm6[0], asm1[0]);
            Assert.Same(asm6[1], asm1[1]);

            var asm7 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                varV1MTTestLib2Ref,
                varV2MTTestLib3Ref,
                varV3MTTestLib4Ref
            });

            Assert.Same(asm7[0], asm1[0]);
            Assert.Same(asm7[1], asm1[1]);
            Assert.NotSame(asm7[2], asm3[3]);
            Assert.NotSame(asm7[2], asm4[3]);
            Assert.NotSame(asm7[3], asm4[4]);

            Assert.Equal("MTTestLib3", asm7[2].Identity.Name);
            Assert.Same(((PEAssemblySymbol)asm7[2]).Assembly, ((PEAssemblySymbol)asm3[3]).Assembly);
            Assert.Equal(2, (from a in asm7[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
            Assert.Equal(1, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());

            var type4 = asm7[2].GlobalNamespace.GetTypeMembers("Class5").
                          Single();

            var retval15 = type4.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal(SymbolKind.ErrorType, retval15.Kind);

            var retval16 = type4.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal(SymbolKind.ErrorType, retval16.Kind);

            var retval17 = type4.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval17.Kind);
            Assert.Same(retval17, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

            Assert.Equal("MTTestLib4", asm7[3].Identity.Name);
            Assert.Same(((PEAssemblySymbol)asm7[3]).Assembly, ((PEAssemblySymbol)asm4[4]).Assembly);
            Assert.Equal(3, (from a in asm7[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
            Assert.Equal(1, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());
            Assert.Equal(1, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[2]) select a).Count());

            var type5 = asm7[3].GlobalNamespace.GetTypeMembers("Class6").
                          Single();

            var retval18 = type5.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal(SymbolKind.ErrorType, retval18.Kind);

            var retval19 = type5.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal(SymbolKind.ErrorType, retval19.Kind);

            var retval20 = type5.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal(SymbolKind.ErrorType, retval20.Kind);

            var retval21 = type5.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval21.Kind);
            Assert.Same(retval21, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

            var retval22 = type5.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval22.Kind);
            Assert.Same(retval22, asm7[2].GlobalNamespace.GetMembers("Class5").Single());

            // This test shows that simple reordering of references doesn't pick different set of assemblies
            var asm8 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                varV3MTTestLib4Ref,
                varV1MTTestLib2Ref,
                varV2MTTestLib3Ref
            });

            Assert.Same(asm8[0], asm1[0]);
            Assert.Same(asm8[0], asm1[0]);
            Assert.Same(asm8[2], asm7[1]);
            Assert.True(asm8[3].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[3]));
            Assert.Same(asm8[3], asm7[2]);
            Assert.True(asm8[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[4]));
            Assert.Same(asm8[1], asm7[3]);

            var asm9 = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                varV3MTTestLib4Ref
            });

            Assert.Same(asm9[0], asm1[0]);
            Assert.True(asm9[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[4]));

            var asm10 = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                varV1MTTestLib2Ref,
                TestReferences.SymbolsTests.V3.MTTestLib1.dll,
                varV2MTTestLib3Ref,
                varV3MTTestLib4Ref
            });

            Assert.Same(asm10[0], asm1[0]);
            Assert.Same(asm10[1], asm4[1]);
            Assert.Same(asm10[2], asm4[2]);
            Assert.Same(asm10[3], asm4[3]);
            Assert.Same(asm10[4], asm4[4]);

            // Run the same tests again to make sure we didn't corrupt prior state by loading additional assemblies
            Assert.Equal("MTTestLib2", asm1[1].Identity.Name);
            Assert.Equal(1, (from a in asm1[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm1[1].BoundReferences() where ReferenceEquals(a, asm1[0]) select a).Count());
            Assert.Equal(SymbolKind.ErrorType, asm1[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.Kind);

            Assert.Same(asm2[0], asm1[0]);

            Assert.Equal("MTTestLib2", asm2[1].Identity.Name);
            Assert.NotSame(asm2[1], asm1[1]);
            Assert.Same(((PEAssemblySymbol)asm2[1]).Assembly, ((PEAssemblySymbol)asm1[1]).Assembly);
            Assert.Equal(2, (from a in asm2[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm2[1].BoundReferences() where ReferenceEquals(a, asm2[0]) select a).Count());
            Assert.Equal(1, (from a in asm2[1].BoundReferences() where ReferenceEquals(a, asm2[2]) select a).Count());

            retval1 = asm2[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval1.Kind);
            Assert.Same(retval1, asm2[2].GlobalNamespace.GetMembers("Class1").Single());

            Assert.Equal("MTTestLib1", asm2[2].Identity.Name);
            Assert.Equal(1, asm2[2].Identity.Version.Major);
            Assert.Equal(1, (from a in asm2[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm2[2].BoundReferences() where ReferenceEquals(a, asm2[0]) select a).Count());

            Assert.Same(asm3[0], asm1[0]);

            Assert.Equal("MTTestLib2", asm3[1].Identity.Name);
            Assert.NotSame(asm3[1], asm1[1]);
            Assert.NotSame(asm3[1], asm2[1]);
            Assert.Same(((PEAssemblySymbol)asm3[1]).Assembly, ((PEAssemblySymbol)asm1[1]).Assembly);
            Assert.Equal(2, (from a in asm3[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm3[1].BoundReferences() where ReferenceEquals(a, asm3[0]) select a).Count());
            Assert.Equal(1, (from a in asm3[1].BoundReferences() where ReferenceEquals(a, asm3[2]) select a).Count());

            retval2 = asm3[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval2.Kind);
            Assert.Same(retval2, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

            Assert.Equal("MTTestLib1", asm3[2].Identity.Name);
            Assert.NotSame(asm3[2], asm2[2]);
            Assert.NotSame(((PEAssemblySymbol)asm3[2]).Assembly, ((PEAssemblySymbol)asm2[2]).Assembly);
            Assert.Equal(2, asm3[2].Identity.Version.Major);
            Assert.Equal(1, (from a in asm3[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm3[2].BoundReferences() where ReferenceEquals(a, asm3[0]) select a).Count());

            Assert.Equal("MTTestLib3", asm3[3].Identity.Name);
            Assert.Equal(3, (from a in asm3[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm3[3].BoundReferences() where ReferenceEquals(a, asm3[0]) select a).Count());
            Assert.Equal(1, (from a in asm3[3].BoundReferences() where ReferenceEquals(a, asm3[1]) select a).Count());
            Assert.Equal(1, (from a in asm3[3].BoundReferences() where ReferenceEquals(a, asm3[2]) select a).Count());

            type1 = asm3[3].GlobalNamespace.GetTypeMembers("Class5").
                          Single();

            retval3 = type1.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval3.Kind);
            Assert.Same(retval3, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

            retval4 = type1.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval4.Kind);
            Assert.Same(retval4, asm3[2].GlobalNamespace.GetMembers("Class2").Single());

            retval5 = type1.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval5.Kind);
            Assert.Same(retval5, asm3[1].GlobalNamespace.GetMembers("Class4").Single());

            Assert.Same(asm3[0], asm1[0]);

            Assert.Equal("MTTestLib2", asm4[1].Identity.Name);
            Assert.NotSame(asm4[1], asm1[1]);
            Assert.NotSame(asm4[1], asm2[1]);
            Assert.NotSame(asm4[1], asm3[1]);
            Assert.Same(((PEAssemblySymbol)asm4[1]).Assembly, ((PEAssemblySymbol)asm1[1]).Assembly);
            Assert.Equal(2, (from a in asm4[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[1].BoundReferences() where ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(1, (from a in asm4[1].BoundReferences() where ReferenceEquals(a, asm4[2]) select a).Count());

            retval6 = asm4[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval6.Kind);
            Assert.Same(retval6, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

            Assert.Equal("MTTestLib1", asm4[2].Identity.Name);
            Assert.NotSame(asm4[2], asm2[2]);
            Assert.NotSame(asm4[2], asm3[2]);
            Assert.NotSame(((PEAssemblySymbol)asm4[2]).Assembly, ((PEAssemblySymbol)asm2[2]).Assembly);
            Assert.NotSame(((PEAssemblySymbol)asm4[2]).Assembly, ((PEAssemblySymbol)asm3[2]).Assembly);
            Assert.Equal(3, asm4[2].Identity.Version.Major);
            Assert.Equal(1, (from a in asm4[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[2].BoundReferences() where ReferenceEquals(a, asm4[0]) select a).Count());

            Assert.Equal("MTTestLib3", asm4[3].Identity.Name);
            Assert.NotSame(asm4[3], asm3[3]);
            Assert.Same(((PEAssemblySymbol)asm4[3]).Assembly, ((PEAssemblySymbol)asm3[3]).Assembly);
            Assert.Equal(3, (from a in asm4[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[3].BoundReferences() where ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(1, (from a in asm4[3].BoundReferences() where ReferenceEquals(a, asm4[1]) select a).Count());
            Assert.Equal(1, (from a in asm4[3].BoundReferences() where ReferenceEquals(a, asm4[2]) select a).Count());

            type2 = asm4[3].GlobalNamespace.GetTypeMembers("Class5").
                          Single();

            retval7 = type2.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval7.Kind);
            Assert.Same(retval7, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

            retval8 = type2.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval8.Kind);
            Assert.Same(retval8, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

            retval9 = type2.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval9.Kind);
            Assert.Same(retval9, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

            Assert.Equal("MTTestLib4", asm4[4].Identity.Name);
            Assert.Equal(4, (from a in asm4[4].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where ReferenceEquals(a, asm4[1]) select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where ReferenceEquals(a, asm4[2]) select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where ReferenceEquals(a, asm4[3]) select a).Count());

            type3 = asm4[4].GlobalNamespace.GetTypeMembers("Class6").
                          Single();

            retval10 = type3.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval10.Kind);
            Assert.Same(retval10, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

            retval11 = type3.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval11.Kind);
            Assert.Same(retval11, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

            retval12 = type3.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval12.Kind);
            Assert.Same(retval12, asm4[2].GlobalNamespace.GetMembers("Class3").Single());

            retval13 = type3.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval13.Kind);
            Assert.Same(retval13, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

            retval14 = type3.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval14.Kind);
            Assert.Same(retval14, asm4[3].GlobalNamespace.GetMembers("Class5").Single());

            Assert.Same(asm7[0], asm1[0]);
            Assert.Same(asm7[1], asm1[1]);
            Assert.NotSame(asm7[2], asm3[3]);
            Assert.NotSame(asm7[2], asm4[3]);
            Assert.NotSame(asm7[3], asm4[4]);

            Assert.Equal("MTTestLib3", asm7[2].Identity.Name);
            Assert.Same(((PEAssemblySymbol)asm7[2]).Assembly, ((PEAssemblySymbol)asm3[3]).Assembly);
            Assert.Equal(2, (from a in asm7[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm7[2].BoundReferences() where ReferenceEquals(a, asm7[0]) select a).Count());
            Assert.Equal(1, (from a in asm7[2].BoundReferences() where ReferenceEquals(a, asm7[1]) select a).Count());

            type4 = asm7[2].GlobalNamespace.GetTypeMembers("Class5").
                          Single();

            retval15 = type4.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal(SymbolKind.ErrorType, retval15.Kind);

            retval16 = type4.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal(SymbolKind.ErrorType, retval16.Kind);

            retval17 = type4.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval17.Kind);
            Assert.Same(retval17, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

            Assert.Equal("MTTestLib4", asm7[3].Identity.Name);
            Assert.Same(((PEAssemblySymbol)asm7[3]).Assembly, ((PEAssemblySymbol)asm4[4]).Assembly);
            Assert.Equal(3, (from a in asm7[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm7[3].BoundReferences() where ReferenceEquals(a, asm7[0]) select a).Count());
            Assert.Equal(1, (from a in asm7[3].BoundReferences() where ReferenceEquals(a, asm7[1]) select a).Count());
            Assert.Equal(1, (from a in asm7[3].BoundReferences() where ReferenceEquals(a, asm7[2]) select a).Count());

            type5 = asm7[3].GlobalNamespace.GetTypeMembers("Class6").
                          Single();

            retval18 = type5.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal(SymbolKind.ErrorType, retval18.Kind);

            retval19 = type5.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal(SymbolKind.ErrorType, retval19.Kind);

            retval20 = type5.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal(SymbolKind.ErrorType, retval20.Kind);

            retval21 = type5.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval21.Kind);
            Assert.Same(retval21, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

            retval22 = type5.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval22.Kind);
            Assert.Same(retval22, asm7[2].GlobalNamespace.GetMembers("Class5").Single());
        }

        [Fact]
        public void MultiTargeting2()
        {
            var varMTTestLib1_V1_Name = new AssemblyIdentity("MTTestLib1", new Version("1.0.0.0"));

            var varC_MTTestLib1_V1 = CreateCompilation(varMTTestLib1_V1_Name,
                           new string[] {
                               // AssemblyPaths.SymbolsTests.V1.MTTestModule1.netmodule
                               @"
public class Class1
{
}
"
                               },
                           new[] { TestReferences.NetFx.v4_0_30319.mscorlib });

            var asm_MTTestLib1_V1 = varC_MTTestLib1_V1.SourceAssembly().BoundReferences();

            var varMTTestLib2_Name = new AssemblyIdentity("MTTestLib2");

            var varC_MTTestLib2 = CreateCompilation(varMTTestLib2_Name,
                           new string[] {
                               // AssemblyPaths.SymbolsTests.V1.MTTestModule2.netmodule
                               @"
public class Class4
{
    Class1 Foo()
    {
        return null;
    }

    public Class1 Bar;

}
"
                               },
                           new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib, varC_MTTestLib1_V1.ToMetadataReference() });

            var asm_MTTestLib2 = varC_MTTestLib2.SourceAssembly().BoundReferences();

            Assert.Same(asm_MTTestLib2[0], asm_MTTestLib1_V1[0]);
            Assert.Same(asm_MTTestLib2[1], varC_MTTestLib1_V1.SourceAssembly());

            var c2 = CreateCompilation(new AssemblyIdentity("c2"),
                           null,
                           new MetadataReference[]
                               {
                                   TestReferences.NetFx.v4_0_30319.mscorlib,
                                   varC_MTTestLib2.ToMetadataReference(),
                                   varC_MTTestLib1_V1.ToMetadataReference()
                               });

            var asm2 = c2.SourceAssembly().BoundReferences();

            Assert.Same(asm2[0], asm_MTTestLib1_V1[0]);
            Assert.Same(asm2[1], varC_MTTestLib2.SourceAssembly());
            Assert.Same(asm2[2], varC_MTTestLib1_V1.SourceAssembly());

            Assert.Equal("MTTestLib2", asm2[1].Identity.Name);
            Assert.Equal(2, (from a in asm2[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());
            Assert.Equal(1, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[2]) select a).Count());

            var retval1 = asm2[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval1.Kind);
            Assert.Same(retval1, asm2[2].GlobalNamespace.GetMembers("Class1").Single());

            Assert.Equal("MTTestLib1", asm2[2].Identity.Name);
            Assert.Equal(1, asm2[2].Identity.Version.Major);
            Assert.Equal(1, (from a in asm2[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm2[2].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());

            var varMTTestLib1_V2_Name = new AssemblyIdentity("MTTestLib1", new Version("2.0.0.0"));

            var varC_MTTestLib1_V2 = CreateCompilation(varMTTestLib1_V2_Name,
                           new string[] {
                               // AssemblyPaths.SymbolsTests.V2.MTTestModule1.netmodule
                               @"
public class Class1
{
}

public class Class2
{
}
"
                               },
                           new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib });

            var asm_MTTestLib1_V2 = varC_MTTestLib1_V2.SourceAssembly().BoundReferences();

            var varMTTestLib3_Name = new AssemblyIdentity("MTTestLib3");

            var varC_MTTestLib3 = CreateCompilation(varMTTestLib3_Name,
                new string[] {
                    // AssemblyPaths.SymbolsTests.V2.MTTestModule3.netmodule
                    @"
public class Class5
{
    Class1 Foo1()
    {
        return null;
    }

    Class2 Foo2()
    {
        return null;
    }

    Class4 Foo3()
    {
        return null;
    }

    Class1 Bar1;
    Class2 Bar2;
    Class4 Bar3;
}
"
                   },
               new MetadataReference[]
                   {
                       TestReferences.NetFx.v4_0_30319.mscorlib,
                       varC_MTTestLib2.ToMetadataReference(),
                       varC_MTTestLib1_V2.ToMetadataReference()
                   });

            var asm_MTTestLib3 = varC_MTTestLib3.SourceAssembly().BoundReferences();

            Assert.Same(asm_MTTestLib3[0], asm_MTTestLib1_V1[0]);
            Assert.NotSame(asm_MTTestLib3[1], varC_MTTestLib2.SourceAssembly());
            Assert.NotSame(asm_MTTestLib3[2], varC_MTTestLib1_V1.SourceAssembly());

            var c3 = CreateCompilation(new AssemblyIdentity("c3"),
                null,
                new MetadataReference[]
                    {
                        TestReferences.NetFx.v4_0_30319.mscorlib,
                        varC_MTTestLib2.ToMetadataReference(),
                        varC_MTTestLib1_V2.ToMetadataReference(),
                        varC_MTTestLib3.ToMetadataReference()
                    });

            var asm3 = c3.SourceAssembly().BoundReferences();

            Assert.Same(asm3[0], asm_MTTestLib1_V1[0]);
            Assert.Same(asm3[1], asm_MTTestLib3[1]);
            Assert.Same(asm3[2], asm_MTTestLib3[2]);
            Assert.Same(asm3[3], varC_MTTestLib3.SourceAssembly());

            Assert.Equal("MTTestLib2", asm3[1].Identity.Name);
            Assert.Same(((RetargetingAssemblySymbol)asm3[1]).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly());
            Assert.Equal(2, (from a in asm3[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
            Assert.Equal(1, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

            var retval2 = asm3[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval2.Kind);
            Assert.Same(retval2, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

            Assert.Equal("MTTestLib1", asm3[2].Identity.Name);
            Assert.NotSame(asm3[2], asm2[2]);
            Assert.NotSame(asm3[2].DeclaringCompilation, asm2[2].DeclaringCompilation);
            Assert.Equal(2, asm3[2].Identity.Version.Major);
            Assert.Equal(1, (from a in asm3[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm3[2].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());

            Assert.Equal("MTTestLib3", asm3[3].Identity.Name);
            Assert.Equal(3, (from a in asm3[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
            Assert.Equal(1, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[1]) select a).Count());
            Assert.Equal(1, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

            var type1 = asm3[3].GlobalNamespace.GetTypeMembers("Class5").
                          Single();

            var retval3 = type1.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval3.Kind);
            Assert.Same(retval3, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

            var retval4 = type1.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval4.Kind);
            Assert.Same(retval4, asm3[2].GlobalNamespace.GetMembers("Class2").Single());

            var retval5 = type1.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval5.Kind);
            Assert.Same(retval5, asm3[1].GlobalNamespace.GetMembers("Class4").Single());

            var varMTTestLib1_V3_Name = new AssemblyIdentity("MTTestLib1", new Version("3.0.0.0"));

            var varC_MTTestLib1_V3 = CreateCompilation(varMTTestLib1_V3_Name,
                           new string[] {
                               // AssemblyPaths.SymbolsTests.V3.MTTestModule1.netmodule
                               @"
public class Class1
{
}

public class Class2
{
}

public class Class3
{
}
"
                               },
                           new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib });

            var asm_MTTestLib1_V3 = varC_MTTestLib1_V3.SourceAssembly().BoundReferences();

            var varMTTestLib4_Name = new AssemblyIdentity("MTTestLib4");

            var varC_MTTestLib4 = CreateCompilation(varMTTestLib4_Name,
                           new string[] {
                               // AssemblyPaths.SymbolsTests.V3.MTTestModule4.netmodule
                               @"
public class Class6
{
    Class1 Foo1()
    {
        return null;
    }

    Class2 Foo2()
    {
        return null;
    }

    Class3 Foo3()
    {
        return null;
    }

    Class4 Foo4()
    {
        return null;
    }

    Class5 Foo5()
    {
        return null;
    }

    Class1 Bar1;
    Class2 Bar2;
    Class3 Bar3;
    Class4 Bar4;
    Class5 Bar5;

}
"
                               },
                           new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib,
                                    varC_MTTestLib2.ToMetadataReference(), varC_MTTestLib1_V3.ToMetadataReference(), varC_MTTestLib3.ToMetadataReference() });

            var asm_MTTestLib4 = varC_MTTestLib4.SourceAssembly().BoundReferences();

            Assert.Same(asm_MTTestLib4[0], asm_MTTestLib1_V1[0]);
            Assert.NotSame(asm_MTTestLib4[1], varC_MTTestLib2.SourceAssembly());
            Assert.Same(asm_MTTestLib4[2], varC_MTTestLib1_V3.SourceAssembly());
            Assert.NotSame(asm_MTTestLib4[3], varC_MTTestLib3.SourceAssembly());

            var c4 = CreateCompilation(new AssemblyIdentity("c4"),
                           null,
                           new MetadataReference[]
                               {
                                   TestReferences.NetFx.v4_0_30319.mscorlib,
                                   varC_MTTestLib2.ToMetadataReference(),
                                   varC_MTTestLib1_V3.ToMetadataReference(),
                                   varC_MTTestLib3.ToMetadataReference(),
                                   varC_MTTestLib4.ToMetadataReference()
                               });

            var asm4 = c4.SourceAssembly().BoundReferences();

            Assert.Same(asm4[0], asm_MTTestLib1_V1[0]);
            Assert.Same(asm4[1], asm_MTTestLib4[1]);
            Assert.Same(asm4[2], asm_MTTestLib4[2]);
            Assert.Same(asm4[3], asm_MTTestLib4[3]);
            Assert.Same(asm4[4], varC_MTTestLib4.SourceAssembly());

            Assert.Equal("MTTestLib2", asm4[1].Identity.Name);
            Assert.NotSame(asm4[1], varC_MTTestLib2.SourceAssembly());
            Assert.NotSame(asm4[1], asm2[1]);
            Assert.NotSame(asm4[1], asm3[1]);
            Assert.Same(((RetargetingAssemblySymbol)asm4[1]).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly());
            Assert.Equal(2, (from a in asm4[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(1, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

            var retval6 = asm4[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval6.Kind);
            Assert.Same(retval6, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

            Assert.Equal("MTTestLib1", asm4[2].Identity.Name);
            Assert.NotSame(asm4[2], asm2[2]);
            Assert.NotSame(asm4[2], asm3[2]);
            Assert.NotSame(asm4[2].DeclaringCompilation, asm2[2].DeclaringCompilation);
            Assert.NotSame(asm4[2].DeclaringCompilation, asm3[2].DeclaringCompilation);
            Assert.Equal(3, asm4[2].Identity.Version.Major);
            Assert.Equal(1, (from a in asm4[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[2].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());

            Assert.Equal("MTTestLib3", asm4[3].Identity.Name);
            Assert.NotSame(asm4[3], asm3[3]);
            Assert.Same(((RetargetingAssemblySymbol)asm4[3]).UnderlyingAssembly, asm3[3]);
            Assert.Equal(3, (from a in asm4[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(1, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
            Assert.Equal(1, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

            var type2 = asm4[3].GlobalNamespace.GetTypeMembers("Class5").
                          Single();

            var retval7 = type2.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval7.Kind);
            Assert.Same(retval7, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

            var retval8 = type2.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval8.Kind);
            Assert.Same(retval8, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

            var retval9 = type2.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval9.Kind);
            Assert.Same(retval9, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

            Assert.Equal("MTTestLib4", asm4[4].Identity.Name);
            Assert.Equal(4, (from a in asm4[4].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[3]) select a).Count());

            var type3 = asm4[4].GlobalNamespace.GetTypeMembers("Class6").
                          Single();

            var retval10 = type3.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval10.Kind);
            Assert.Same(retval10, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

            var retval11 = type3.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval11.Kind);
            Assert.Same(retval11, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

            var retval12 = type3.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval12.Kind);
            Assert.Same(retval12, asm4[2].GlobalNamespace.GetMembers("Class3").Single());

            var retval13 = type3.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval13.Kind);
            Assert.Same(retval13, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

            var retval14 = type3.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval14.Kind);
            Assert.Same(retval14, asm4[3].GlobalNamespace.GetMembers("Class5").Single());

            var c5 = CreateCompilation(new AssemblyIdentity("c5"),
                           null,
                           new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib, varC_MTTestLib3.ToMetadataReference() });

            var asm5 = c5.SourceAssembly().BoundReferences();

            Assert.Same(asm5[0], asm2[0]);
            Assert.True(asm5[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm3[3]));

            var c6 = CreateCompilation(new AssemblyIdentity("c6"),
                           null,
                           new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib, varC_MTTestLib2.ToMetadataReference() });

            var asm6 = c6.SourceAssembly().BoundReferences();

            Assert.Same(asm6[0], asm2[0]);
            Assert.True(asm6[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()));

            var c7 = CreateCompilation(new AssemblyIdentity("c7"),
                           null,
                          new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib, varC_MTTestLib2.ToMetadataReference(), varC_MTTestLib3.ToMetadataReference(), varC_MTTestLib4.ToMetadataReference() });

            var asm7 = c7.SourceAssembly().BoundReferences();

            Assert.Same(asm7[0], asm2[0]);
            Assert.True(asm7[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()));
            Assert.NotSame(asm7[2], asm3[3]);
            Assert.NotSame(asm7[2], asm4[3]);
            Assert.NotSame(asm7[3], asm4[4]);

            Assert.Equal("MTTestLib3", asm7[2].Identity.Name);
            Assert.Same(((RetargetingAssemblySymbol)asm7[2]).UnderlyingAssembly, asm3[3]);
            Assert.Equal(2, (from a in asm7[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
            Assert.Equal(1, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());

            var type4 = asm7[2].GlobalNamespace.GetTypeMembers("Class5").
                          Single();

            var retval15 = type4.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal("MTTestLib1", retval15.ContainingAssembly.Name);
            Assert.Equal(0, (from a in asm7 where a != null && a.Name == "MTTestLib1" select a).Count());

            var retval16 = type4.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal("MTTestLib1", retval16.ContainingAssembly.Name);

            var retval17 = type4.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval17.Kind);
            Assert.Same(retval17, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

            Assert.Equal("MTTestLib4", asm7[3].Identity.Name);
            Assert.Same(((RetargetingAssemblySymbol)asm7[3]).UnderlyingAssembly, asm4[4]);
            Assert.Equal(3, (from a in asm7[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
            Assert.Equal(1, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());
            Assert.Equal(1, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[2]) select a).Count());

            var type5 = asm7[3].GlobalNamespace.GetTypeMembers("Class6").
                          Single();

            var retval18 = type5.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal("MTTestLib1", retval18.ContainingAssembly.Name);

            var retval19 = type5.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal("MTTestLib1", retval19.ContainingAssembly.Name);

            var retval20 = type5.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal("MTTestLib1", retval20.ContainingAssembly.Name);

            var retval21 = type5.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval21.Kind);
            Assert.Same(retval21, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

            var retval22 = type5.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval22.Kind);
            Assert.Same(retval22, asm7[2].GlobalNamespace.GetMembers("Class5").Single());

            // This test shows that simple reordering of references doesn't pick different set of assemblies
            var c8 = CreateCompilation(new AssemblyIdentity("c8"),
                           null,
                           new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib, varC_MTTestLib4.ToMetadataReference(), varC_MTTestLib2.ToMetadataReference(), varC_MTTestLib3.ToMetadataReference() });

            var asm8 = c8.SourceAssembly().BoundReferences();

            Assert.Same(asm8[0], asm2[0]);
            Assert.True(asm8[2].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[1]));
            Assert.Same(asm8[2], asm7[1]);
            Assert.True(asm8[3].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[3]));
            Assert.Same(asm8[3], asm7[2]);
            Assert.True(asm8[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[4]));
            Assert.Same(asm8[1], asm7[3]);

            var c9 = CreateCompilation(new AssemblyIdentity("c9"),
                           null,
                           new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib, varC_MTTestLib4.ToMetadataReference() });

            var asm9 = c9.SourceAssembly().BoundReferences();

            Assert.Same(asm9[0], asm2[0]);
            Assert.True(asm9[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[4]));

            var c10 = CreateCompilation(new AssemblyIdentity("c10"),
                           null,
                           new MetadataReference[] {
                                   TestReferences.NetFx.v4_0_30319.mscorlib,
                                   varC_MTTestLib2.ToMetadataReference(),
                                   varC_MTTestLib1_V3.ToMetadataReference(),
                                   varC_MTTestLib3.ToMetadataReference(),
                                   varC_MTTestLib4.ToMetadataReference() });

            var asm10 = c10.SourceAssembly().BoundReferences();

            Assert.Same(asm10[0], asm2[0]);
            Assert.Same(asm10[1], asm4[1]);
            Assert.Same(asm10[2], asm4[2]);
            Assert.Same(asm10[3], asm4[3]);
            Assert.Same(asm10[4], asm4[4]);

            // Run the same tests again to make sure we didn't corrupt prior state by loading additional assemblies
            Assert.Same(asm2[0], asm_MTTestLib1_V1[0]);

            Assert.Equal("MTTestLib2", asm2[1].Identity.Name);
            Assert.Equal(2, (from a in asm2[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());
            Assert.Equal(1, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[2]) select a).Count());

            retval1 = asm2[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval1.Kind);
            Assert.Same(retval1, asm2[2].GlobalNamespace.GetMembers("Class1").Single());

            Assert.Equal("MTTestLib1", asm2[2].Identity.Name);
            Assert.Equal(1, asm2[2].Identity.Version.Major);
            Assert.Equal(1, (from a in asm2[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm2[2].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());

            Assert.Same(asm_MTTestLib3[0], asm_MTTestLib1_V1[0]);
            Assert.NotSame(asm_MTTestLib3[1], varC_MTTestLib2.SourceAssembly());
            Assert.NotSame(asm_MTTestLib3[2], varC_MTTestLib1_V1.SourceAssembly());

            Assert.Same(asm3[0], asm_MTTestLib1_V1[0]);
            Assert.Same(asm3[1], asm_MTTestLib3[1]);
            Assert.Same(asm3[2], asm_MTTestLib3[2]);
            Assert.Same(asm3[3], varC_MTTestLib3.SourceAssembly());

            Assert.Equal("MTTestLib2", asm3[1].Identity.Name);
            Assert.Same(((RetargetingAssemblySymbol)asm3[1]).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly());
            Assert.Equal(2, (from a in asm3[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
            Assert.Equal(1, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

            retval2 = asm3[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval2.Kind);
            Assert.Same(retval2, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

            Assert.Equal("MTTestLib1", asm3[2].Identity.Name);
            Assert.NotSame(asm3[2], asm2[2]);
            Assert.NotSame(asm3[2].DeclaringCompilation, asm2[2].DeclaringCompilation);
            Assert.Equal(2, asm3[2].Identity.Version.Major);
            Assert.Equal(1, (from a in asm3[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm3[2].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());

            Assert.Equal("MTTestLib3", asm3[3].Identity.Name);
            Assert.Equal(3, (from a in asm3[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
            Assert.Equal(1, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[1]) select a).Count());
            Assert.Equal(1, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

            type1 = asm3[3].GlobalNamespace.GetTypeMembers("Class5").
                          Single();

            retval3 = type1.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval3.Kind);
            Assert.Same(retval3, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

            retval4 = type1.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval4.Kind);
            Assert.Same(retval4, asm3[2].GlobalNamespace.GetMembers("Class2").Single());

            retval5 = type1.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval5.Kind);
            Assert.Same(retval5, asm3[1].GlobalNamespace.GetMembers("Class4").Single());

            Assert.Same(asm4[0], asm_MTTestLib1_V1[0]);
            Assert.Same(asm4[1], asm_MTTestLib4[1]);
            Assert.Same(asm4[2], asm_MTTestLib4[2]);
            Assert.Same(asm4[3], asm_MTTestLib4[3]);
            Assert.Same(asm4[4], varC_MTTestLib4.SourceAssembly());

            Assert.Equal("MTTestLib2", asm4[1].Identity.Name);
            Assert.NotSame(asm4[1], varC_MTTestLib2.SourceAssembly());
            Assert.NotSame(asm4[1], asm2[1]);
            Assert.NotSame(asm4[1], asm3[1]);
            Assert.Same(((RetargetingAssemblySymbol)asm4[1]).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly());
            Assert.Equal(2, (from a in asm4[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(1, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

            retval6 = asm4[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval6.Kind);
            Assert.Same(retval6, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

            Assert.Equal("MTTestLib1", asm4[2].Identity.Name);
            Assert.NotSame(asm4[2], asm2[2]);
            Assert.NotSame(asm4[2], asm3[2]);
            Assert.NotSame(asm4[2].DeclaringCompilation, asm2[2].DeclaringCompilation);
            Assert.NotSame(asm4[2].DeclaringCompilation, asm3[2].DeclaringCompilation);
            Assert.Equal(3, asm4[2].Identity.Version.Major);
            Assert.Equal(1, (from a in asm4[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[2].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());

            Assert.Equal("MTTestLib3", asm4[3].Identity.Name);
            Assert.NotSame(asm4[3], asm3[3]);
            Assert.Same(((RetargetingAssemblySymbol)asm4[3]).UnderlyingAssembly, asm3[3]);
            Assert.Equal(3, (from a in asm4[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(1, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
            Assert.Equal(1, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

            type2 = asm4[3].GlobalNamespace.GetTypeMembers("Class5").
                          Single();

            retval7 = type2.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval7.Kind);
            Assert.Same(retval7, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

            retval8 = type2.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval8.Kind);
            Assert.Same(retval8, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

            retval9 = type2.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval9.Kind);
            Assert.Same(retval9, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

            Assert.Equal("MTTestLib4", asm4[4].Identity.Name);
            Assert.Equal(4, (from a in asm4[4].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[3]) select a).Count());

            type3 = asm4[4].GlobalNamespace.GetTypeMembers("Class6").
                          Single();

            retval10 = type3.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval10.Kind);
            Assert.Same(retval10, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

            retval11 = type3.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval11.Kind);
            Assert.Same(retval11, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

            retval12 = type3.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval12.Kind);
            Assert.Same(retval12, asm4[2].GlobalNamespace.GetMembers("Class3").Single());

            retval13 = type3.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval13.Kind);
            Assert.Same(retval13, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

            retval14 = type3.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval14.Kind);
            Assert.Same(retval14, asm4[3].GlobalNamespace.GetMembers("Class5").Single());

            Assert.Same(asm5[0], asm2[0]);
            Assert.True(asm5[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm3[3]));

            Assert.Same(asm6[0], asm2[0]);
            Assert.True(asm6[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()));

            Assert.Same(asm7[0], asm2[0]);
            Assert.True(asm7[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()));
            Assert.NotSame(asm7[2], asm3[3]);
            Assert.NotSame(asm7[2], asm4[3]);
            Assert.NotSame(asm7[3], asm4[4]);

            Assert.Equal("MTTestLib3", asm7[2].Identity.Name);
            Assert.Same(((RetargetingAssemblySymbol)asm7[2]).UnderlyingAssembly, asm3[3]);
            Assert.Equal(2, (from a in asm7[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
            Assert.Equal(1, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());

            type4 = asm7[2].GlobalNamespace.GetTypeMembers("Class5").
                          Single();

            retval15 = type4.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal("MTTestLib1", retval15.ContainingAssembly.Name);
            Assert.Equal(0, (from a in asm7 where a != null && a.Name == "MTTestLib1" select a).Count());

            retval16 = type4.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal("MTTestLib1", retval16.ContainingAssembly.Name);

            retval17 = type4.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval17.Kind);
            Assert.Same(retval17, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

            Assert.Equal("MTTestLib4", asm7[3].Identity.Name);
            Assert.Same(((RetargetingAssemblySymbol)asm7[3]).UnderlyingAssembly, asm4[4]);
            Assert.Equal(3, (from a in asm7[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
            Assert.Equal(1, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());
            Assert.Equal(1, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[2]) select a).Count());

            type5 = asm7[3].GlobalNamespace.GetTypeMembers("Class6").
                          Single();

            retval18 = type5.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal("MTTestLib1", retval18.ContainingAssembly.Name);

            retval19 = type5.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal("MTTestLib1", retval19.ContainingAssembly.Name);

            retval20 = type5.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal("MTTestLib1", retval20.ContainingAssembly.Name);

            retval21 = type5.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval21.Kind);
            Assert.Same(retval21, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

            retval22 = type5.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval22.Kind);
            Assert.Same(retval22, asm7[2].GlobalNamespace.GetMembers("Class5").Single());
        }

        [Fact]
        public void MultiTargeting3()
        {
            var varMTTestLib2_Name = new AssemblyIdentity("MTTestLib2");

            var varC_MTTestLib2 = CreateCompilation(varMTTestLib2_Name, (string[])null,
                           new[] {
                                        TestReferences.NetFx.v4_0_30319.mscorlib,
                                        TestReferences.SymbolsTests.V1.MTTestLib1.dll,
                                        TestReferences.SymbolsTests.V1.MTTestModule2.netmodule
                                     });

            var asm_MTTestLib2 = varC_MTTestLib2.SourceAssembly().BoundReferences();

            var c2 = CreateCompilation(new AssemblyIdentity("c2"),
                           null,
                           new MetadataReference[] {
                                                           TestReferences.NetFx.v4_0_30319.mscorlib,
                                                           TestReferences.SymbolsTests.V1.MTTestLib1.dll,
                                                           new CSharpCompilationReference(varC_MTTestLib2)
                                                       });

            var asm2Prime = c2.SourceAssembly().BoundReferences();
            var asm2 = new AssemblySymbol[] { asm2Prime[0], asm2Prime[2], asm2Prime[1] };

            Assert.Same(asm2[0], asm_MTTestLib2[0]);
            Assert.Same(asm2[1], varC_MTTestLib2.SourceAssembly());
            Assert.Same(asm2[2], asm_MTTestLib2[1]);

            Assert.Equal("MTTestLib2", asm2[1].Identity.Name);
            Assert.Equal(4, (from a in asm2[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(2, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());
            Assert.Equal(2, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[2]) select a).Count());

            var retval1 = asm2[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Same(retval1, asm2[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Bar").OfType<FieldSymbol>().Single().Type.TypeSymbol);

            Assert.NotEqual(SymbolKind.ErrorType, retval1.Kind);
            Assert.Same(retval1, asm2[2].GlobalNamespace.GetMembers("Class1").Single());

            Assert.Equal("MTTestLib1", asm2[2].Identity.Name);
            Assert.Equal(1, asm2[2].Identity.Version.Major);
            Assert.Equal(1, (from a in asm2[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm2[2].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());

            var varMTTestLib3_Name = new AssemblyIdentity("MTTestLib3");

            var varC_MTTestLib3 = CreateCompilation(varMTTestLib3_Name,
                           null,
                           new MetadataReference[]
                               {
                                    TestReferences.NetFx.v4_0_30319.mscorlib,
                                    TestReferences.SymbolsTests.V2.MTTestLib1.dll,
                                    new CSharpCompilationReference(varC_MTTestLib2),
                                    TestReferences.SymbolsTests.V2.MTTestModule3.netmodule
                               });

            var asm_MTTestLib3Prime = varC_MTTestLib3.SourceAssembly().BoundReferences();
            var asm_MTTestLib3 = new AssemblySymbol[] { asm_MTTestLib3Prime[0], asm_MTTestLib3Prime[2], asm_MTTestLib3Prime[1] };

            Assert.Same(asm_MTTestLib3[0], asm_MTTestLib2[0]);
            Assert.NotSame(asm_MTTestLib3[1], varC_MTTestLib2.SourceAssembly());
            Assert.NotSame(asm_MTTestLib3[2], asm_MTTestLib2[1]);

            var c3 = CreateCompilation(new AssemblyIdentity("c3"),
                           null,
                           new MetadataReference[]
                               {
                                   TestReferences.NetFx.v4_0_30319.mscorlib,
                                   TestReferences.SymbolsTests.V2.MTTestLib1.dll,
                                   new CSharpCompilationReference(varC_MTTestLib2),
                                   new CSharpCompilationReference(varC_MTTestLib3)
                               });

            var asm3Prime = c3.SourceAssembly().BoundReferences();
            var asm3 = new AssemblySymbol[] { asm3Prime[0], asm3Prime[2], asm3Prime[1], asm3Prime[3] };

            Assert.Same(asm3[0], asm_MTTestLib2[0]);
            Assert.Same(asm3[1], asm_MTTestLib3[1]);
            Assert.Same(asm3[2], asm_MTTestLib3[2]);
            Assert.Same(asm3[3], varC_MTTestLib3.SourceAssembly());

            Assert.Equal("MTTestLib2", asm3[1].Identity.Name);
            Assert.Same(((RetargetingAssemblySymbol)asm3[1]).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly());
            Assert.Equal(4, (from a in asm3[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(2, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
            Assert.Equal(2, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

            var retval2 = asm3[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Same(retval2, asm3[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Bar").OfType<FieldSymbol>().Single().Type.TypeSymbol);

            Assert.NotEqual(SymbolKind.ErrorType, retval2.Kind);
            Assert.Same(retval2, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

            Assert.Equal("MTTestLib1", asm3[2].Identity.Name);
            Assert.NotSame(asm3[2], asm2[2]);
            Assert.NotSame(asm3[2], asm2[2]);
            Assert.NotSame(((PEAssemblySymbol)asm3[2]).Assembly, ((PEAssemblySymbol)asm2[2]).Assembly);
            Assert.Equal(2, asm3[2].Identity.Version.Major);
            Assert.Equal(1, (from a in asm3[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm3[2].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());

            Assert.Equal("MTTestLib3", asm3[3].Identity.Name);
            Assert.Equal(6, (from a in asm3[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(2, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
            Assert.Equal(2, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[1]) select a).Count());
            Assert.Equal(2, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

            var type1 = asm3[3].GlobalNamespace.GetTypeMembers("Class5").Single();

            var retval3 = type1.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval3.Kind);
            Assert.Same(retval3, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

            var retval4 = type1.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval4.Kind);
            Assert.Same(retval4, asm3[2].GlobalNamespace.GetMembers("Class2").Single());

            var retval5 = type1.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval5.Kind);
            Assert.Same(retval5, asm3[1].GlobalNamespace.GetMembers("Class4").Single());

            var varMTTestLib4_Name = new AssemblyIdentity("MTTestLib4");

            var varC_MTTestLib4 = CreateCompilation(varMTTestLib4_Name,
                           null,
                           new MetadataReference[]
                                {
                                    TestReferences.NetFx.v4_0_30319.mscorlib,
                                    TestReferences.SymbolsTests.V3.MTTestLib1.dll,
                                    new CSharpCompilationReference(varC_MTTestLib2),
                                    new CSharpCompilationReference(varC_MTTestLib3),
                                    TestReferences.SymbolsTests.V3.MTTestModule4.netmodule
                                });

            var asm_MTTestLib4Prime = varC_MTTestLib4.SourceAssembly().BoundReferences();
            var asm_MTTestLib4 = new AssemblySymbol[] { asm_MTTestLib4Prime[0], asm_MTTestLib4Prime[2], asm_MTTestLib4Prime[1], asm_MTTestLib4Prime[3] };

            Assert.Same(asm_MTTestLib4[0], asm_MTTestLib2[0]);
            Assert.NotSame(asm_MTTestLib4[1], varC_MTTestLib2.SourceAssembly());
            Assert.NotSame(asm_MTTestLib4[2], asm3[2]);
            Assert.NotSame(asm_MTTestLib4[2], asm2[2]);
            Assert.NotSame(asm_MTTestLib4[3], varC_MTTestLib3.SourceAssembly());

            var c4 = CreateCompilation(new AssemblyIdentity("c4"),
                           null,
                           new MetadataReference[] {
                                                           TestReferences.NetFx.v4_0_30319.mscorlib,
                                                           TestReferences.SymbolsTests.V3.MTTestLib1.dll,
                                                           new CSharpCompilationReference(varC_MTTestLib2),
                                                           new CSharpCompilationReference(varC_MTTestLib3),
                                                           new CSharpCompilationReference(varC_MTTestLib4)
                                                       });

            var asm4Prime = c4.SourceAssembly().BoundReferences();
            var asm4 = new AssemblySymbol[] { asm4Prime[0], asm4Prime[2], asm4Prime[1], asm4Prime[3], asm4Prime[4] };

            Assert.Same(asm4[0], asm_MTTestLib2[0]);
            Assert.Same(asm4[1], asm_MTTestLib4[1]);
            Assert.Same(asm4[2], asm_MTTestLib4[2]);
            Assert.Same(asm4[3], asm_MTTestLib4[3]);
            Assert.Same(asm4[4], varC_MTTestLib4.SourceAssembly());

            Assert.Equal("MTTestLib2", asm4[1].Identity.Name);
            Assert.NotSame(asm4[1], varC_MTTestLib2.SourceAssembly());
            Assert.NotSame(asm4[1], asm2[1]);
            Assert.NotSame(asm4[1], asm3[1]);
            Assert.Same(((RetargetingAssemblySymbol)asm4[1]).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly());
            Assert.Equal(4, (from a in asm4[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(2, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(2, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

            var retval6 = asm4[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval6.Kind);
            Assert.Same(retval6, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

            Assert.Equal("MTTestLib1", asm4[2].Identity.Name);
            Assert.NotSame(asm4[2], asm2[2]);
            Assert.NotSame(asm4[2], asm3[2]);
            Assert.NotSame(((PEAssemblySymbol)asm4[2]).Assembly, ((PEAssemblySymbol)asm2[2]).Assembly);
            Assert.NotSame(((PEAssemblySymbol)asm4[2]).Assembly, ((PEAssemblySymbol)asm3[2]).Assembly);
            Assert.Equal(3, asm4[2].Identity.Version.Major);
            Assert.Equal(1, (from a in asm4[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[2].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());

            Assert.Equal("MTTestLib3", asm4[3].Identity.Name);
            Assert.NotSame(asm4[3], asm3[3]);
            Assert.Same(((RetargetingAssemblySymbol)asm4[3]).UnderlyingAssembly, asm3[3]);
            Assert.Equal(6, (from a in asm4[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(2, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(2, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
            Assert.Equal(2, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

            var type2 = asm4[3].GlobalNamespace.GetTypeMembers("Class5").
                          Single();

            var retval7 = type2.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval7.Kind);
            Assert.Same(retval7, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

            var retval8 = type2.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval8.Kind);
            Assert.Same(retval8, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

            var retval9 = type2.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval9.Kind);
            Assert.Same(retval9, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

            Assert.Equal("MTTestLib4", asm4[4].Identity.Name);
            Assert.Equal(8, (from a in asm4[4].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(2, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(2, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
            Assert.Equal(2, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());
            Assert.Equal(2, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[3]) select a).Count());

            var type3 = asm4[4].GlobalNamespace.GetTypeMembers("Class6").
                          Single();

            var retval10 = type3.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval10.Kind);
            Assert.Same(retval10, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

            var retval11 = type3.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval11.Kind);
            Assert.Same(retval11, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

            var retval12 = type3.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval12.Kind);
            Assert.Same(retval12, asm4[2].GlobalNamespace.GetMembers("Class3").Single());

            var retval13 = type3.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval13.Kind);
            Assert.Same(retval13, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

            var retval14 = type3.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval14.Kind);
            Assert.Same(retval14, asm4[3].GlobalNamespace.GetMembers("Class5").Single());

            var c5 = CreateCompilation(new AssemblyIdentity("c5"),
                           null,
                           new MetadataReference[] {
                                                            TestReferences.NetFx.v4_0_30319.mscorlib,
                                                            new CSharpCompilationReference(varC_MTTestLib3)
                                                       });

            var asm5 = c5.SourceAssembly().BoundReferences();

            Assert.Same(asm5[0], asm2[0]);
            Assert.True(asm5[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm3[3]));

            var c6 = CreateCompilation(new AssemblyIdentity("c6"),
                           null,
                           new MetadataReference[] {
                                                           TestReferences.NetFx.v4_0_30319.mscorlib,
                                                           new CSharpCompilationReference(varC_MTTestLib2)
                                                       });

            var asm6 = c6.SourceAssembly().BoundReferences();

            Assert.Same(asm6[0], asm2[0]);
            Assert.True(asm6[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()));

            var c7 = CreateCompilation(new AssemblyIdentity("c7"),
                           null,
                           new MetadataReference[] {
                                                           TestReferences.NetFx.v4_0_30319.mscorlib,
                                                           new CSharpCompilationReference(varC_MTTestLib2),
                                                           new CSharpCompilationReference(varC_MTTestLib3),
                                                           new CSharpCompilationReference(varC_MTTestLib4)
                                                       });

            var asm7 = c7.SourceAssembly().BoundReferences();

            Assert.Same(asm7[0], asm2[0]);
            Assert.True(asm7[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()));
            Assert.NotSame(asm7[2], asm3[3]);
            Assert.NotSame(asm7[2], asm4[3]);
            Assert.NotSame(asm7[3], asm4[4]);

            Assert.Equal("MTTestLib3", asm7[2].Identity.Name);
            Assert.Same(((RetargetingAssemblySymbol)asm7[2]).UnderlyingAssembly, asm3[3]);
            Assert.Equal(4, (from a in asm7[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(2, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
            Assert.Equal(2, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());

            var type4 = asm7[2].GlobalNamespace.GetTypeMembers("Class5").
                          Single();

            var retval15 = type4.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            AssemblySymbol missingAssembly;

            missingAssembly = retval15.ContainingAssembly;

            Assert.True(missingAssembly.IsMissing);
            Assert.Equal("MTTestLib1", missingAssembly.Identity.Name);

            var retval16 = type4.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Same(missingAssembly, retval16.ContainingAssembly);

            var retval17 = type4.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval17.Kind);
            Assert.Same(retval17, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

            Assert.Equal("MTTestLib4", asm7[3].Identity.Name);
            Assert.Same(((RetargetingAssemblySymbol)asm7[3]).UnderlyingAssembly, asm4[4]);
            Assert.Equal(6, (from a in asm7[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(2, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
            Assert.Equal(2, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());
            Assert.Equal(2, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[2]) select a).Count());

            var type5 = asm7[3].GlobalNamespace.GetTypeMembers("Class6").
                          Single();

            var retval18 = type5.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal("MTTestLib1", ((MissingMetadataTypeSymbol)retval18).ContainingAssembly.Identity.Name);

            var retval19 = type5.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Same(retval18.ContainingAssembly, retval19.ContainingAssembly);

            var retval20 = type5.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Same(retval18.ContainingAssembly, retval20.ContainingAssembly);

            var retval21 = type5.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval21.Kind);
            Assert.Same(retval21, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

            var retval22 = type5.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval22.Kind);
            Assert.Same(retval22, asm7[2].GlobalNamespace.GetMembers("Class5").Single());

            // This test shows that simple reordering of references doesn't pick different set of assemblies
            var c8 = CreateCompilation(new AssemblyIdentity("c8"),
                           null,
                           new MetadataReference[] {
                                                           TestReferences.NetFx.v4_0_30319.mscorlib,
                                                           new CSharpCompilationReference(varC_MTTestLib4),
                                                           new CSharpCompilationReference(varC_MTTestLib2),
                                                           new CSharpCompilationReference(varC_MTTestLib3)
                                                       });

            var asm8 = c8.SourceAssembly().BoundReferences();

            Assert.Same(asm8[0], asm2[0]);
            Assert.True(asm8[2].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[1]));
            Assert.Same(asm8[2], asm7[1]);
            Assert.True(asm8[3].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[3]));
            Assert.Same(asm8[3], asm7[2]);
            Assert.True(asm8[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[4]));
            Assert.Same(asm8[1], asm7[3]);

            var c9 = CreateCompilation(new AssemblyIdentity("c9"),
                           null,
                           new MetadataReference[] {
                                                           TestReferences.NetFx.v4_0_30319.mscorlib,
                                                           new CSharpCompilationReference(varC_MTTestLib4)
                                                       });

            var asm9 = c9.SourceAssembly().BoundReferences();

            Assert.Same(asm9[0], asm2[0]);
            Assert.True(asm9[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[4]));

            var c10 = CreateCompilation(new AssemblyIdentity("c10"),
                           null,
                           new MetadataReference[] {
                                                           TestReferences.NetFx.v4_0_30319.mscorlib,
                                                           TestReferences.SymbolsTests.V3.MTTestLib1.dll,
                                                           new CSharpCompilationReference(varC_MTTestLib2),
                                                           new CSharpCompilationReference(varC_MTTestLib3),
                                                           new CSharpCompilationReference(varC_MTTestLib4)
                                                       });

            var asm10Prime = c10.SourceAssembly().BoundReferences();
            var asm10 = new AssemblySymbol[] { asm10Prime[0], asm10Prime[2], asm10Prime[1], asm10Prime[3], asm10Prime[4] };

            Assert.Same(asm10[0], asm2[0]);
            Assert.Same(asm10[1], asm4[1]);
            Assert.Same(asm10[2], asm4[2]);
            Assert.Same(asm10[3], asm4[3]);
            Assert.Same(asm10[4], asm4[4]);

            // Run the same tests again to make sure we didn't corrupt prior state by loading additional assemblies
            Assert.Same(asm2[0], asm_MTTestLib2[0]);

            Assert.Equal("MTTestLib2", asm2[1].Identity.Name);
            Assert.Equal(4, (from a in asm2[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(2, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());
            Assert.Equal(2, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[2]) select a).Count());

            retval1 = asm2[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval1.Kind);
            Assert.Same(retval1, asm2[2].GlobalNamespace.GetMembers("Class1").Single());

            Assert.Equal("MTTestLib1", asm2[2].Identity.Name);
            Assert.Equal(1, asm2[2].Identity.Version.Major);
            Assert.Equal(1, (from a in asm2[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm2[2].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());

            Assert.Same(asm_MTTestLib3[0], asm_MTTestLib2[0]);
            Assert.NotSame(asm_MTTestLib3[1], varC_MTTestLib2.SourceAssembly());
            Assert.NotSame(asm_MTTestLib3[2], asm_MTTestLib2[1]);

            Assert.Same(asm3[0], asm_MTTestLib2[0]);
            Assert.Same(asm3[1], asm_MTTestLib3[1]);
            Assert.Same(asm3[2], asm_MTTestLib3[2]);
            Assert.Same(asm3[3], varC_MTTestLib3.SourceAssembly());

            Assert.Equal("MTTestLib2", asm3[1].Identity.Name);
            Assert.Same(((RetargetingAssemblySymbol)asm3[1]).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly());
            Assert.Equal(4, (from a in asm3[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(2, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
            Assert.Equal(2, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

            retval2 = asm3[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval2.Kind);
            Assert.Same(retval2, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

            Assert.Equal("MTTestLib1", asm3[2].Identity.Name);
            Assert.NotSame(asm3[2], asm2[2]);
            Assert.NotSame(((PEAssemblySymbol)asm3[2]).Assembly, ((PEAssemblySymbol)asm2[2]).Assembly);
            Assert.Equal(2, asm3[2].Identity.Version.Major);
            Assert.Equal(1, (from a in asm3[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm3[2].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());

            Assert.Equal("MTTestLib3", asm3[3].Identity.Name);
            Assert.Equal(6, (from a in asm3[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(2, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
            Assert.Equal(2, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[1]) select a).Count());
            Assert.Equal(2, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

            type1 = asm3[3].GlobalNamespace.GetTypeMembers("Class5").
                          Single();

            retval3 = type1.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval3.Kind);
            Assert.Same(retval3, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

            retval4 = type1.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval4.Kind);
            Assert.Same(retval4, asm3[2].GlobalNamespace.GetMembers("Class2").Single());

            retval5 = type1.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval5.Kind);
            Assert.Same(retval5, asm3[1].GlobalNamespace.GetMembers("Class4").Single());

            Assert.Same(asm4[0], asm_MTTestLib2[0]);
            Assert.Same(asm4[1], asm_MTTestLib4[1]);
            Assert.Same(asm4[2], asm_MTTestLib4[2]);
            Assert.Same(asm4[3], asm_MTTestLib4[3]);
            Assert.Same(asm4[4], varC_MTTestLib4.SourceAssembly());

            Assert.Equal("MTTestLib2", asm4[1].Identity.Name);
            Assert.NotSame(asm4[1], varC_MTTestLib2.SourceAssembly());
            Assert.NotSame(asm4[1], asm2[1]);
            Assert.NotSame(asm4[1], asm3[1]);
            Assert.Same(((RetargetingAssemblySymbol)asm4[1]).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly());
            Assert.Equal(4, (from a in asm4[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(2, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(2, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

            retval6 = asm4[1].GlobalNamespace.GetTypeMembers("Class4").
                          Single().
                          GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval6.Kind);
            Assert.Same(retval6, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

            Assert.Equal("MTTestLib1", asm4[2].Identity.Name);
            Assert.NotSame(asm4[2], asm2[2]);
            Assert.NotSame(asm4[2], asm3[2]);
            Assert.NotSame(((PEAssemblySymbol)asm4[2]).Assembly, ((PEAssemblySymbol)asm2[2]).Assembly);
            Assert.NotSame(((PEAssemblySymbol)asm4[2]).Assembly, ((PEAssemblySymbol)asm3[2]).Assembly);
            Assert.Equal(3, asm4[2].Identity.Version.Major);
            Assert.Equal(1, (from a in asm4[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[2].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());

            Assert.Equal("MTTestLib3", asm4[3].Identity.Name);
            Assert.NotSame(asm4[3], asm3[3]);
            Assert.Same(((RetargetingAssemblySymbol)asm4[3]).UnderlyingAssembly, asm3[3]);
            Assert.Equal(6, (from a in asm4[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(2, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(2, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
            Assert.Equal(2, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

            type2 = asm4[3].GlobalNamespace.GetTypeMembers("Class5").
                          Single();

            retval7 = type2.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval7.Kind);
            Assert.Same(retval7, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

            retval8 = type2.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval8.Kind);
            Assert.Same(retval8, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

            retval9 = type2.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval9.Kind);
            Assert.Same(retval9, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

            Assert.Equal("MTTestLib4", asm4[4].Identity.Name);
            Assert.Equal(8, (from a in asm4[4].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(2, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(2, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
            Assert.Equal(2, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());
            Assert.Equal(2, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[3]) select a).Count());

            type3 = asm4[4].GlobalNamespace.GetTypeMembers("Class6").
                          Single();

            retval10 = type3.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval10.Kind);
            Assert.Same(retval10, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

            retval11 = type3.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval11.Kind);
            Assert.Same(retval11, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

            retval12 = type3.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval12.Kind);
            Assert.Same(retval12, asm4[2].GlobalNamespace.GetMembers("Class3").Single());

            retval13 = type3.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval13.Kind);
            Assert.Same(retval13, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

            retval14 = type3.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval14.Kind);
            Assert.Same(retval14, asm4[3].GlobalNamespace.GetMembers("Class5").Single());

            Assert.Same(asm5[0], asm2[0]);
            Assert.True(asm5[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm3[3]));

            Assert.Same(asm6[0], asm2[0]);
            Assert.True(asm6[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()));

            Assert.Same(asm7[0], asm2[0]);
            Assert.True(asm7[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()));
            Assert.NotSame(asm7[2], asm3[3]);
            Assert.NotSame(asm7[2], asm4[3]);
            Assert.NotSame(asm7[3], asm4[4]);

            Assert.Equal("MTTestLib3", asm7[2].Identity.Name);
            Assert.Same(((RetargetingAssemblySymbol)asm7[2]).UnderlyingAssembly, asm3[3]);
            Assert.Equal(4, (from a in asm7[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(2, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
            Assert.Equal(2, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());

            type4 = asm7[2].GlobalNamespace.GetTypeMembers("Class5").
                          Single();

            retval15 = type4.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            missingAssembly = retval15.ContainingAssembly;

            Assert.True(missingAssembly.IsMissing);
            Assert.Equal("MTTestLib1", missingAssembly.Identity.Name);

            retval16 = type4.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Same(missingAssembly, retval16.ContainingAssembly);

            retval17 = type4.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval17.Kind);
            Assert.Same(retval17, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

            Assert.Equal("MTTestLib4", asm7[3].Identity.Name);
            Assert.Same(((RetargetingAssemblySymbol)asm7[3]).UnderlyingAssembly, asm4[4]);
            Assert.Equal(6, (from a in asm7[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(2, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
            Assert.Equal(2, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());
            Assert.Equal(2, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[2]) select a).Count());

            type5 = asm7[3].GlobalNamespace.GetTypeMembers("Class6").
                          Single();

            retval18 = type5.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal("MTTestLib1", ((MissingMetadataTypeSymbol)retval18).ContainingAssembly.Identity.Name);

            retval19 = type5.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Same(retval18.ContainingAssembly, retval19.ContainingAssembly);

            retval20 = type5.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Same(retval18.ContainingAssembly, retval20.ContainingAssembly);

            retval21 = type5.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval21.Kind);
            Assert.Same(retval21, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

            retval22 = type5.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.NotEqual(SymbolKind.ErrorType, retval22.Kind);
            Assert.Same(retval22, asm7[2].GlobalNamespace.GetMembers("Class5").Single());
        }

        [Fact]
        public void MultiTargeting4()
        {
            var localC1_V1_Name = new AssemblyIdentity("c1", new Version("1.0.0.0"));

            var localC1_V1 = CreateCompilation(localC1_V1_Name,
                           new string[] {
                               // AssemblyPaths.SymbolsTests.MultiTargeting.Source1Module.netmodule
                               @"
public class C1<T>
{
    public class C2<S>
    {
        public C1<T>.C2<S> Foo()
        {
            return null;
        }
    }
}
"
                               },
                           new[] { TestReferences.NetFx.v4_0_30319.mscorlib });

            var asm1_V1 = localC1_V1.SourceAssembly();

            var localC1_V2_Name = new AssemblyIdentity("c1", new Version("2.0.0.0"));

            var localC1_V2 = CreateCompilation(localC1_V2_Name,
                           new string[] {
                               // AssemblyPaths.SymbolsTests.MultiTargeting.Source1Module.netmodule
                               @"
public class C1<T>
{
    public class C2<S>
    {
        public C1<T>.C2<S> Foo()
        {
            return null;
        }
    }
}
"
                               },
                           new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib });

            var asm1_V2 = localC1_V2.SourceAssembly();

            var localC4_V1_Name = new AssemblyIdentity("c4", new Version("1.0.0.0"));

            var localC4_V1 = CreateCompilation(localC4_V1_Name,
                           new string[] {
                               // AssemblyPaths.SymbolsTests.MultiTargeting.Source4Module.netmodule
                               @"
public class C4
{
}
"
                               },
                           new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib });

            var asm4_V1 = localC4_V1.SourceAssembly();

            var localC4_V2_Name = new AssemblyIdentity("c4", new Version("2.0.0.0"));

            var localC4_V2 = CreateCompilation(localC4_V2_Name,
                           new string[] {
                               // AssemblyPaths.SymbolsTests.MultiTargeting.Source4Module.netmodule
                               @"
public class C4
{
}
"
                               },
                           new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib });

            var asm4_V2 = localC4_V2.SourceAssembly();

            var c7 = CreateCompilation(new AssemblyIdentity("C7"),
                           new string[] {
                               // AssemblyPaths.SymbolsTests.MultiTargeting.Source7Module.netmodule
                               @"
public class C7
{}

public class C8<T>
{ }
"
                               },
                           new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib });

            var asm7 = c7.SourceAssembly();

            var c3 = CreateCompilation(new AssemblyIdentity("C3"),
                           new string[] {
                               // AssemblyPaths.SymbolsTests.MultiTargeting.Source3Module.netmodule
                               @"
public class C3
{
    public C1<C3>.C2<C4> Foo()
    {
        return null;
    }

    public static C6<C4> Bar()
    {
        return null;
    }

    public C8<C7> Foo1()
    {
        return null;
    }

    public void Foo2(ref C300[,] x1,
                    out C4 x2,
                    ref C7[] x3,
                    C4 x4 = null)
    {
        x2 = null;
    }

    internal virtual TFoo3 Foo3<TFoo3>() where TFoo3: C4
    {
        return null;
    }

    public C8<C4> Foo4()
    {
        return null;
    }

    public abstract class C301 :
        I1
    {
    }

    internal class C302
    {
    }

}

public class C6<T> where T: new ()
{}

public class C300
{}

public interface I1
{}

namespace ns1
{
    namespace ns2
    {
        public class C303
        {}

    }

    public class C304
    {
        public class C305
        {}

    }

}
"
                               },
                           new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib,
                                                           new CSharpCompilationReference(localC1_V1),
                                                           new CSharpCompilationReference(localC4_V1),
                                                           new CSharpCompilationReference(c7)
                                                       });

            var asm3 = c3.SourceAssembly();

            var localC3Foo2 = asm3.GlobalNamespace.GetTypeMembers("C3").
                          Single().GetMembers("Foo2").OfType<MethodSymbol>().Single();

            var c5 = CreateCompilation(new AssemblyIdentity("C5"),
                           new string[] {
                               // AssemblyPaths.SymbolsTests.MultiTargeting.Source5Module.netmodule
                               @"
public class C5 :
    ns1.C304.C305
{}
"
                               },
                           new MetadataReference[] {
                                                           TestReferences.NetFx.v4_0_30319.mscorlib,
                                                           new CSharpCompilationReference(c3),
                                                           new CSharpCompilationReference(localC1_V2),
                                                           new CSharpCompilationReference(localC4_V2),
                                                           new CSharpCompilationReference(c7)
                                                       });

            var asm5 = c5.SourceAssembly().BoundReferences();

            Assert.NotSame(asm5[1], asm3);
            Assert.Same(((RetargetingAssemblySymbol)asm5[1]).UnderlyingAssembly, asm3);
            Assert.Same(asm5[2], asm1_V2);
            Assert.Same(asm5[3], asm4_V2);
            Assert.Same(asm5[4], asm7);

            var type3 = asm5[1].GlobalNamespace.GetTypeMembers("C3").
                          Single();

            var type1 = asm1_V2.GlobalNamespace.GetTypeMembers("C1").
                          Single();

            var type2 = type1.GetTypeMembers("C2").
                          Single();

            var type4 = asm4_V2.GlobalNamespace.GetTypeMembers("C4").
                          Single();

            var retval1 = (NamedTypeSymbol)type3.GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.TypeSymbol;

            Assert.Equal("C1<C3>.C2<C4>", retval1.ToTestDisplayString());

            Assert.Same(retval1.OriginalDefinition, type2);

            var args1 = retval1.ContainingType.TypeArguments.Concat(retval1.TypeArguments).SelectAsArray(TypeMap.AsTypeSymbol);
            var params1 = retval1.ContainingType.TypeParameters.Concat(retval1.TypeParameters);

            Assert.Same(params1[0], type1.TypeParameters[0]);
            Assert.Same(params1[1].OriginalDefinition, type2.TypeParameters[0].OriginalDefinition);

            Assert.Same(args1[0], type3);
            Assert.Same(args1[0].ContainingAssembly, asm5[1]);
            Assert.Same(args1[1], type4);

            var retval2 = retval1.ContainingType;

            Assert.Equal("C1<C3>", retval2.ToTestDisplayString());
            Assert.Same(retval2.OriginalDefinition, type1);

            var bar = type3.GetMembers("Bar").OfType<MethodSymbol>().Single();
            var retval3 = (NamedTypeSymbol)bar.ReturnType.TypeSymbol;
            var type6 = asm5[1].GlobalNamespace.GetTypeMembers("C6").
                          Single();

            Assert.Equal("C6<C4>", retval3.ToTestDisplayString());

            Assert.Same(retval3.OriginalDefinition, type6);
            Assert.Same(retval3.ContainingAssembly, asm5[1]);

            var args3 = retval3.TypeArguments;
            var params3 = retval3.TypeParameters;

            Assert.Same(params3[0], type6.TypeParameters[0]);
            Assert.Same(params3[0].ContainingAssembly, asm5[1]);
            Assert.Same(args3[0].TypeSymbol, type4);

            var foo1 = type3.GetMembers("Foo1").OfType<MethodSymbol>().Single();
            var retval4 = foo1.ReturnType;

            Assert.Equal("C8<C7>", retval4.ToTestDisplayString());

            Assert.Same(retval4,
                          asm3.GlobalNamespace.GetTypeMembers("C3").
                          Single().
                          GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType);

            var foo1Params = foo1.Parameters;
            Assert.Equal(0, foo1Params.Length);

            var foo2 = type3.GetMembers("Foo2").OfType<MethodSymbol>().Single();
            Assert.NotEqual(localC3Foo2, foo2);
            Assert.Same(localC3Foo2, ((RetargetingMethodSymbol)foo2).UnderlyingMethod);
            Assert.Equal(1, ((RetargetingMethodSymbol)foo2).Locations.Length);

            var foo2Params = foo2.Parameters;
            Assert.Equal(4, foo2Params.Length);
            Assert.Same(localC3Foo2.Parameters[0], ((RetargetingParameterSymbol)foo2Params[0]).UnderlyingParameter);
            Assert.Same(localC3Foo2.Parameters[1], ((RetargetingParameterSymbol)foo2Params[1]).UnderlyingParameter);
            Assert.Same(localC3Foo2.Parameters[2], ((RetargetingParameterSymbol)foo2Params[2]).UnderlyingParameter);
            Assert.Same(localC3Foo2.Parameters[3], ((RetargetingParameterSymbol)foo2Params[3]).UnderlyingParameter);

            var x1 = foo2Params[0];
            var x2 = foo2Params[1];
            var x3 = foo2Params[2];
            var x4 = foo2Params[3];

            Assert.Equal("x1", x1.Name);
            Assert.NotEqual(localC3Foo2.Parameters[0].Type.TypeSymbol, x1.Type.TypeSymbol);
            Assert.Equal(localC3Foo2.Parameters[0].ToTestDisplayString(), x1.ToTestDisplayString());
            Assert.Same(asm5[1], x1.ContainingAssembly);
            Assert.Same(foo2, x1.ContainingSymbol);
            Assert.False(x1.HasExplicitDefaultValue);
            Assert.False(x1.IsOptional);
            Assert.Equal(RefKind.Ref, x1.RefKind);
            Assert.Equal(2, ((ArrayTypeSymbol)x1.Type.TypeSymbol).Rank);

            Assert.Equal("x2", x2.Name);
            Assert.NotEqual(localC3Foo2.Parameters[1].Type.TypeSymbol, x2.Type.TypeSymbol);
            Assert.Equal(RefKind.Out, x2.RefKind);

            Assert.Equal("x3", x3.Name);
            Assert.Same(localC3Foo2.Parameters[2].Type.TypeSymbol, x3.Type.TypeSymbol);

            Assert.Equal("x4", x4.Name);
            Assert.True(x4.HasExplicitDefaultValue);
            Assert.True(x4.IsOptional);

            Assert.Equal("Foo2", foo2.Name);
            Assert.Equal(localC3Foo2.ToTestDisplayString(), foo2.ToTestDisplayString());
            Assert.Same(asm5[1], foo2.ContainingAssembly);
            Assert.Same(type3, foo2.ContainingSymbol);
            Assert.Equal(Accessibility.Public, foo2.DeclaredAccessibility);
            Assert.False(foo2.HidesBaseMethodsByName);
            Assert.False(foo2.IsAbstract);
            Assert.False(foo2.IsExtern);
            Assert.False(foo2.IsGenericMethod);
            Assert.False(foo2.IsOverride);
            Assert.False(foo2.IsSealed);
            Assert.False(foo2.IsStatic);
            Assert.False(foo2.IsVararg);
            Assert.False(foo2.IsVirtual);
            Assert.True(foo2.ReturnsVoid);
            Assert.Equal(0, foo2.TypeParameters.Length);
            Assert.Equal(0, foo2.TypeArguments.Length);

            Assert.True(bar.IsStatic);
            Assert.False(bar.ReturnsVoid);

            var foo3 = type3.GetMembers("Foo3").OfType<MethodSymbol>().Single();

            Assert.Equal(Accessibility.Internal, foo3.DeclaredAccessibility);
            Assert.True(foo3.IsGenericMethod);
            Assert.True(foo3.IsVirtual);

            var foo3TypeParams = foo3.TypeParameters;
            Assert.Equal(1, foo3TypeParams.Length);
            Assert.Equal(1, foo3.TypeArguments.Length);
            Assert.Same(foo3TypeParams[0], foo3.TypeArguments[0].TypeSymbol);

            var typeC301 = type3.GetTypeMembers("C301").Single();
            var typeC302 = type3.GetTypeMembers("C302").Single();
            var typeC6 = asm5[1].GlobalNamespace.GetTypeMembers("C6").Single();

            Assert.Equal(typeC301.ToTestDisplayString(),
                asm3.GlobalNamespace.GetTypeMembers("C3").Single().
                        GetTypeMembers("C301").Single().ToTestDisplayString());

            Assert.Equal(typeC6.ToTestDisplayString(),
                asm3.GlobalNamespace.GetTypeMembers("C6").Single().ToTestDisplayString());

            Assert.Equal(typeC301.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat),
                asm3.GlobalNamespace.GetTypeMembers("C3").Single().
                        GetTypeMembers("C301").Single().ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat));

            Assert.Equal(typeC6.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat),
                asm3.GlobalNamespace.GetTypeMembers("C6").Single().ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat));

            Assert.Equal(type3.GetMembers().Length,
                asm3.GlobalNamespace.GetTypeMembers("C3").Single().GetMembers().Length);

            Assert.Equal(type3.GetTypeMembers().Length,
                asm3.GlobalNamespace.GetTypeMembers("C3").Single().GetTypeMembers().Length);

            Assert.Same(typeC301, type3.GetTypeMembers("C301", 0).Single());

            Assert.Equal(0, type3.Arity);
            Assert.Equal(1, typeC6.Arity);

            Assert.NotNull(type3.BaseType);
            Assert.Equal("System.Object", type3.BaseType.ToTestDisplayString());

            Assert.Equal(Accessibility.Public, type3.DeclaredAccessibility);
            Assert.Equal(Accessibility.Internal, typeC302.DeclaredAccessibility);

            Assert.Equal(0, type3.Interfaces.Length);
            Assert.Equal(1, typeC301.Interfaces.Length);
            Assert.Equal("I1", typeC301.Interfaces.Single().Name);

            Assert.False(type3.IsAbstract);
            Assert.True(typeC301.IsAbstract);

            Assert.False(type3.IsSealed);
            Assert.False(type3.IsStatic);

            Assert.Equal(0, type3.TypeArguments.Length);
            Assert.Equal(0, type3.TypeParameters.Length);

            var localC6Params = typeC6.TypeParameters;
            Assert.Equal(1, localC6Params.Length);
            Assert.Equal(1, typeC6.TypeArguments.Length);
            Assert.Same(localC6Params[0], typeC6.TypeArguments[0].TypeSymbol);

            Assert.Same(((RetargetingNamedTypeSymbol)type3).UnderlyingNamedType,
                asm3.GlobalNamespace.GetTypeMembers("C3").Single());
            Assert.Equal(1, ((RetargetingNamedTypeSymbol)type3).Locations.Length);

            Assert.Equal(TypeKind.Class, type3.TypeKind);
            Assert.Equal(TypeKind.Interface, asm5[1].GlobalNamespace.GetTypeMembers("I1").Single().TypeKind);

            var localC6_T = localC6Params[0];
            var foo3TypeParam = foo3TypeParams[0];

            Assert.Equal(0, localC6_T.ConstraintTypes.Length);

            Assert.Equal(1, foo3TypeParam.ConstraintTypes.Length);
            Assert.Same(type4, foo3TypeParam.ConstraintTypes.Single().TypeSymbol);

            Assert.Same(typeC6, localC6_T.ContainingSymbol);
            Assert.False(foo3TypeParam.HasConstructorConstraint);

            Assert.True(localC6_T.HasConstructorConstraint);

            Assert.False(foo3TypeParam.HasReferenceTypeConstraint);
            Assert.False(foo3TypeParam.HasValueTypeConstraint);

            Assert.Equal("TFoo3", foo3TypeParam.Name);
            Assert.Equal("T", localC6_T.Name);

            Assert.Equal(0, foo3TypeParam.Ordinal);
            Assert.Equal(0, localC6_T.Ordinal);

            Assert.Equal(VarianceKind.None, foo3TypeParam.Variance);
            Assert.Same(((RetargetingTypeParameterSymbol)localC6_T).UnderlyingTypeParameter,
                asm3.GlobalNamespace.GetTypeMembers("C6").Single().TypeParameters[0]);

            var ns1 = asm5[1].GlobalNamespace.GetMembers("ns1").OfType<NamespaceSymbol>().Single();
            var ns2 = ns1.GetMembers("ns2").OfType<NamespaceSymbol>().Single();

            Assert.Equal("ns1.ns2", ns2.ToTestDisplayString());
            Assert.Equal(2, ns1.GetMembers().Length);

            Assert.Equal(1, ns1.GetTypeMembers().Length);
            Assert.Same(ns1.GetTypeMembers("C304").Single(), ns1.GetTypeMembers("C304", 0).Single());

            Assert.Same(asm5[1].Modules[0], asm5[1].Modules[0].GlobalNamespace.ContainingSymbol);
            Assert.Same(asm5[1].Modules[0].GlobalNamespace, ns1.ContainingSymbol);
            Assert.Same(asm5[1].Modules[0], ns1.Extent.Module);
            Assert.Equal(1, ns1.ConstituentNamespaces.Length);
            Assert.Same(ns1, ns1.ConstituentNamespaces[0]);
            Assert.False(ns1.IsGlobalNamespace);
            Assert.True(asm5[1].Modules[0].GlobalNamespace.IsGlobalNamespace);

            Assert.Same(asm3.Modules[0].GlobalNamespace,
                ((RetargetingNamespaceSymbol)asm5[1].Modules[0].GlobalNamespace).UnderlyingNamespace);
            Assert.Same(asm3.Modules[0].GlobalNamespace.GetMembers("ns1").Single(),
                ((RetargetingNamespaceSymbol)ns1).UnderlyingNamespace);

            var module3 = (RetargetingModuleSymbol)asm5[1].Modules[0];

            Assert.Equal("C3.dll", module3.ToTestDisplayString());
            Assert.Equal("C3.dll", module3.Name);

            Assert.Same(asm5[1], module3.ContainingSymbol);
            Assert.Same(asm5[1], module3.ContainingAssembly);
            Assert.Null(module3.ContainingType);

            var retval5 = type3.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType;

            Assert.Equal("C8<C4>", retval5.ToTestDisplayString());

            var typeC5 = c5.Assembly.GlobalNamespace.GetTypeMembers("C5").Single();

            Assert.Same(asm5[1], typeC5.BaseType.ContainingAssembly);
            Assert.Equal("ns1.C304.C305", typeC5.BaseType.ToTestDisplayString());
            Assert.NotEqual(SymbolKind.ErrorType, typeC5.Kind);
        }

        [Fact]
        public void MultiTargeting5()
        {
            var c1_Name = new AssemblyIdentity("c1");

            var text = @"
class Module1
{
    Class4 M1()
    {}

    Class4.Class4_1 M2()
    {}

    Class4 M3()
    {}
}
";
            var tree = Parse(text);

            var c1 = CreateCompilationWithMscorlib(tree, new MetadataReference[]
            {
                TestReferences.SymbolsTests.V1.MTTestLib1.dll,
                TestReferences.SymbolsTests.V1.MTTestModule2.netmodule
            });

            var c2_Name = new AssemblyIdentity("MTTestLib2");

            var c2 = CreateCompilation(c2_Name, null, new MetadataReference[]
            {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                TestReferences.SymbolsTests.V2.MTTestLib1.dll,
                new CSharpCompilationReference(c1)
            });

            SourceAssemblySymbol c1AsmSource = (SourceAssemblySymbol)c1.Assembly;
            PEAssemblySymbol Lib1_V1 = (PEAssemblySymbol)c1AsmSource.Modules[0].GetReferencedAssemblySymbols()[1];
            PEModuleSymbol module1 = (PEModuleSymbol)c1AsmSource.Modules[1];

            Assert.Equal(LocationKind.MetadataFile, ((MetadataLocation)Lib1_V1.Locations[0]).Kind);
            SourceAssemblySymbol c2AsmSource = (SourceAssemblySymbol)c2.Assembly;
            RetargetingAssemblySymbol c1AsmRef = (RetargetingAssemblySymbol)c2AsmSource.Modules[0].GetReferencedAssemblySymbols()[2];
            PEAssemblySymbol Lib1_V2 = (PEAssemblySymbol)c2AsmSource.Modules[0].GetReferencedAssemblySymbols()[1];
            PEModuleSymbol module2 = (PEModuleSymbol)c1AsmRef.Modules[1];

            Assert.Equal(1, Lib1_V1.Identity.Version.Major);
            Assert.Equal(2, Lib1_V2.Identity.Version.Major);

            Assert.NotEqual(module1, module2);
            Assert.Same(module1.Module, module2.Module);

            NamedTypeSymbol classModule1 = c1AsmRef.Modules[0].GlobalNamespace.GetTypeMembers("Module1").Single();
            MethodSymbol m1 = classModule1.GetMembers("M1").OfType<MethodSymbol>().Single();
            MethodSymbol m2 = classModule1.GetMembers("M2").OfType<MethodSymbol>().Single();
            MethodSymbol m3 = classModule1.GetMembers("M3").OfType<MethodSymbol>().Single();

            Assert.Same(module2, m1.ReturnType.TypeSymbol.ContainingModule);
            Assert.Same(module2, m2.ReturnType.TypeSymbol.ContainingModule);
            Assert.Same(module2, m3.ReturnType.TypeSymbol.ContainingModule);
        }

        // Very simplistic test if a compilation has a single type with the given full name. Does NOT handle generics.
        private bool HasSingleTypeOfKind(CSharpCompilation c, TypeKind kind, string fullName)
        {
            string[] names = fullName.Split('.');

            NamespaceOrTypeSymbol current = c.GlobalNamespace;
            foreach (string name in names)
            {
                var matchingSym = current.GetMembers(name);
                if (matchingSym.Length != 1)
                {
                    return false;
                }

                current = (NamespaceOrTypeSymbol)matchingSym.First();
            }

            return current is TypeSymbol && ((TypeSymbol)current).TypeKind == kind;
        }

        [Fact]
        public void AddRemoveReferences()
        {
            var mscorlibRef = TestReferences.NetFx.v4_0_30319.mscorlib;
            var systemCoreRef = TestReferences.NetFx.v4_0_30319.System_Core;
            var systemRef = TestReferences.NetFx.v4_0_30319.System;

            CSharpCompilation c = CSharpCompilation.Create("Test");
            Assert.False(HasSingleTypeOfKind(c, TypeKind.Struct, "System.Int32"));
            c = c.AddReferences(mscorlibRef);
            Assert.True(HasSingleTypeOfKind(c, TypeKind.Struct, "System.Int32"));
            Assert.False(HasSingleTypeOfKind(c, TypeKind.Class, "System.Linq.Enumerable"));
            c = c.AddReferences(systemCoreRef);
            Assert.True(HasSingleTypeOfKind(c, TypeKind.Class, "System.Linq.Enumerable"));
            Assert.False(HasSingleTypeOfKind(c, TypeKind.Class, "System.Uri"));
            c = c.ReplaceReference(systemCoreRef, systemRef);
            Assert.False(HasSingleTypeOfKind(c, TypeKind.Class, "System.Linq.Enumerable"));
            Assert.True(HasSingleTypeOfKind(c, TypeKind.Class, "System.Uri"));
            c = c.RemoveReferences(systemRef);
            Assert.False(HasSingleTypeOfKind(c, TypeKind.Class, "System.Uri"));
            Assert.True(HasSingleTypeOfKind(c, TypeKind.Struct, "System.Int32"));
            c = c.RemoveReferences(mscorlibRef);
            Assert.False(HasSingleTypeOfKind(c, TypeKind.Struct, "System.Int32"));
        }

        private sealed class Resolver : MetadataReferenceResolver
        {
            private readonly string _data, _core, _system;

            public Resolver(string data, string core, string system)
            {
                _data = data;
                _core = core;
                _system = system;
            }

            public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
            {
                switch (reference)
                {
                    case "System.Data":
                        return ImmutableArray.Create(MetadataReference.CreateFromFile(_data));

                    case "System.Core":
                        return ImmutableArray.Create(MetadataReference.CreateFromFile(_core));

                    case "System":
                        return ImmutableArray.Create(MetadataReference.CreateFromFile(_system));

                    default:
                        if (File.Exists(reference))
                        {
                            return ImmutableArray.Create(MetadataReference.CreateFromFile(reference));
                        }

                        return ImmutableArray<PortableExecutableReference>.Empty;
                }
            }

            public override bool Equals(object other) => true;
            public override int GetHashCode() => 1;
        }

        [Fact]
        public void CompilationWithReferenceDirectives()
        {
            var data = Temp.CreateFile().WriteAllBytes(TestResources.NetFX.v4_0_30319.System_Data).Path;
            var core = Temp.CreateFile().WriteAllBytes(TestResources.NetFX.v4_0_30319.System_Core).Path;
            var xml = Temp.CreateFile().WriteAllBytes(TestResources.NetFX.v4_0_30319.System_Xml).Path;
            var system = Temp.CreateFile().WriteAllBytes(TestResources.NetFX.v4_0_30319.System).Path;

            var trees = new[]
            {
                SyntaxFactory.ParseSyntaxTree($@"
#r ""System.Data""
#r ""{xml}""
#r ""{core}""
", options: TestOptions.Script),

                SyntaxFactory.ParseSyntaxTree(@"
#r ""System""
", options: TestOptions.Script),

                SyntaxFactory.ParseSyntaxTree(@"
new System.Data.DataSet();
System.Linq.Expressions.Expression.Constant(123);
System.Diagnostics.Process.GetCurrentProcess();
", options: TestOptions.Script)
            };

            var compilation = CreateCompilationWithMscorlib45(
                trees,
                options: TestOptions.ReleaseDll.WithMetadataReferenceResolver(new Resolver(data, core, system)));

            compilation.VerifyDiagnostics();

            var boundRefs = compilation.Assembly.BoundReferences();

            AssertEx.Equal(new[]
            {
                "System.Data",
                "System.Xml",
                "System.Core",
                "System",
                "mscorlib"
            }, boundRefs.Select(r => r.Name));
        }

        [Fact]
        public void CompilationWithReferenceDirectives_Errors()
        {
            var data = Temp.CreateFile().WriteAllBytes(TestResources.NetFX.v4_0_30319.System_Data).Path;
            var core = Temp.CreateFile().WriteAllBytes(TestResources.NetFX.v4_0_30319.System_Core).Path;
            var system = Temp.CreateFile().WriteAllBytes(TestResources.NetFX.v4_0_30319.System).Path;

            var trees = new[] {
                    SyntaxFactory.ParseSyntaxTree(@"
#r System
#r ""~!@#$%^&*():\?/""
#r ""non-existing-reference""
", options: TestOptions.Script),
                SyntaxFactory.ParseSyntaxTree(@"
#r ""System.Core""
")
                };

            var compilation = CreateCompilationWithMscorlib45(
                trees,
                options: TestOptions.ReleaseDll.WithMetadataReferenceResolver(new Resolver(data, core, system)));

            compilation.VerifyDiagnostics(
                // (3,1): error CS0006: Metadata file '~!@#$%^&*():\?/' could not be found
                Diagnostic(ErrorCode.ERR_NoMetadataFile, @"#r ""~!@#$%^&*():\?/""").WithArguments(@"~!@#$%^&*():\?/"),
                // (4,1): error CS0006: Metadata file 'non-existing-reference' could not be found
                Diagnostic(ErrorCode.ERR_NoMetadataFile, @"#r ""non-existing-reference""").WithArguments("non-existing-reference"),
                // (2,4): error CS7010: Quoted file name expected
                Diagnostic(ErrorCode.ERR_ExpectedPPFile, "System"),
                // (2,1): error CS7011: #r is only allowed in scripts
                Diagnostic(ErrorCode.ERR_ReferenceDirectiveOnlyAllowedInScripts, "r"));
        }

        private class DummyReferenceResolver : MetadataReferenceResolver
        {
            private readonly string _targetDll;

            public DummyReferenceResolver(string targetDll)
            {
                _targetDll = targetDll;
            }

            public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
            {
                var path = reference.EndsWith("-resolve", StringComparison.Ordinal) ? _targetDll : reference;
                return ImmutableArray.Create(MetadataReference.CreateFromFile(path, properties));
            }

            public override bool Equals(object other) => true;
            public override int GetHashCode() => 1;
        }

        [Fact]
        public void MetadataReferenceProvider()
        {
            var csClasses01 = Temp.CreateFile().WriteAllBytes(TestResources.MetadataTests.InterfaceAndClass.CSClasses01).Path;
            var csInterfaces01 = Temp.CreateFile().WriteAllBytes(TestResources.MetadataTests.InterfaceAndClass.CSInterfaces01).Path;

            var source = @"
#r """ + typeof(object).Assembly.Location + @"""
#r """ + "!@#$%^/&*-resolve" + @"""
#r """ + csInterfaces01 + @"""
class C : Metadata.ICSPropImpl { }";

            var compilation = CreateCompilationWithMscorlib45(
                new[] { Parse(source, options: TestOptions.Script) },
                options: TestOptions.ReleaseDll.WithMetadataReferenceResolver(new DummyReferenceResolver(csClasses01)));

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void CompilationWithReferenceDirective_NoResolver()
        {
            var compilation = CreateCompilationWithMscorlib45(
                new[] { SyntaxFactory.ParseSyntaxTree(@"#r ""bar""", TestOptions.Script, "a.csx", Encoding.UTF8) },
                options: TestOptions.ReleaseDll.WithMetadataReferenceResolver(null));

            compilation.VerifyDiagnostics(
                // a.csx(1,1): error CS7099: Metadata references not supported.
                // #r "bar"
                Diagnostic(ErrorCode.ERR_MetadataReferencesNotSupported, @"#r ""bar"""));
        }

        [Fact]
        public void GlobalUsings1()
        {
            var trees = new[]
            {
                SyntaxFactory.ParseSyntaxTree(@"
WriteLine(1);
Console.WriteLine(2);
", options: TestOptions.Script),
                SyntaxFactory.ParseSyntaxTree(@"
class C 
{ 
    void Foo() { Console.WriteLine(3); }
}
")
            };

            var compilation = CreateCompilationWithMscorlib45(
                trees,
                options: TestOptions.ReleaseDll.WithUsings(ImmutableArray.Create("System.Console", "System")));

            var diagnostics = compilation.GetDiagnostics().ToArray();

            // global usings are only visible in script code:
            DiagnosticsUtils.VerifyErrorCodes(diagnostics,
                // (4,18): error CS0103: The name 'Console' does not exist in the current context
                new ErrorDescription() { Code = (int)ErrorCode.ERR_NameNotInContext, Line = 4, Column = 18 });
        }

        [Fact]
        public void GlobalUsings_Errors()
        {
            var trees = new[] {
                SyntaxFactory.ParseSyntaxTree(@"
WriteLine(1);
Console.WriteLine(2);
", options: TestOptions.Script)
            };

            var compilation = CreateCompilationWithMscorlib45(
                trees,
                options: TestOptions.ReleaseDll.WithUsings("System.Console!", "Blah"));

            compilation.VerifyDiagnostics(
                // error CS0234: The type or namespace name 'Console!' does not exist in the namespace 'System' (are you missing an assembly reference?)
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS).WithArguments("Console!", "System"),
                // error CS0246: The type or namespace name 'Blah' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound).WithArguments("Blah"),
                // (2,1): error CS0103: The name 'WriteLine' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "WriteLine").WithArguments("WriteLine"),
                // (3,1): error CS0103: The name 'Console' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Console").WithArguments("Console"));
        }

        [Fact]
        public void ReferenceToAssemblyWithSpecialCharactersInName()
        {
            var r = TestReferences.SymbolsTests.Metadata.InvalidCharactersInAssemblyName;

            var st = SyntaxFactory.ParseSyntaxTree("class C { static void Main() { new lib.Class1(); } }");
            var compilation = CSharpCompilation.Create("foo", references: new[] { MscorlibRef, r }, syntaxTrees: new[] { st });
            var diags = compilation.GetDiagnostics().ToArray();
            Assert.Equal(0, diags.Length);

            using (var stream = new MemoryStream())
            {
                compilation.Emit(stream);
            }
        }

        [Fact]
        public void SyntaxTreeOrderConstruct()
        {
            var tree1 = CreateSyntaxTree("A");
            var tree2 = CreateSyntaxTree("B");

            SyntaxTree[] treeOrder1 = new[] { tree1, tree2 };
            var compilation1 = CSharpCompilation.Create("Compilation1", syntaxTrees: treeOrder1);
            CheckCompilationSyntaxTrees(compilation1, treeOrder1);

            SyntaxTree[] treeOrder2 = new[] { tree2, tree1 };
            var compilation2 = CSharpCompilation.Create("Compilation2", syntaxTrees: treeOrder2);
            CheckCompilationSyntaxTrees(compilation2, treeOrder2);
        }

        [Fact]
        public void SyntaxTreeOrderAdd()
        {
            var tree1 = CreateSyntaxTree("A");
            var tree2 = CreateSyntaxTree("B");
            var tree3 = CreateSyntaxTree("C");
            var tree4 = CreateSyntaxTree("D");

            SyntaxTree[] treeList1 = new[] { tree1, tree2 };
            var compilation1 = CSharpCompilation.Create("Compilation1", syntaxTrees: treeList1);
            CheckCompilationSyntaxTrees(compilation1, treeList1);

            SyntaxTree[] treeList2 = new[] { tree3, tree4 };
            var compilation2 = compilation1.AddSyntaxTrees(treeList2);
            CheckCompilationSyntaxTrees(compilation1, treeList1); //compilation1 untouched
            CheckCompilationSyntaxTrees(compilation2, treeList1.Concat(treeList2).ToArray());

            SyntaxTree[] treeList3 = new[] { tree4, tree3 };
            var compilation3 = CSharpCompilation.Create("Compilation3", syntaxTrees: treeList3);
            CheckCompilationSyntaxTrees(compilation3, treeList3);

            SyntaxTree[] treeList4 = new[] { tree2, tree1 };
            var compilation4 = compilation3.AddSyntaxTrees(treeList4);
            CheckCompilationSyntaxTrees(compilation3, treeList3); //compilation3 untouched
            CheckCompilationSyntaxTrees(compilation4, treeList3.Concat(treeList4).ToArray());
        }

        [Fact]
        public void SyntaxTreeOrderRemove()
        {
            var tree1 = CreateSyntaxTree("A");
            var tree2 = CreateSyntaxTree("B");
            var tree3 = CreateSyntaxTree("C");
            var tree4 = CreateSyntaxTree("D");

            SyntaxTree[] treeList1 = new[] { tree1, tree2, tree3, tree4 };
            var compilation1 = CSharpCompilation.Create("Compilation1", syntaxTrees: treeList1);
            CheckCompilationSyntaxTrees(compilation1, treeList1);

            SyntaxTree[] treeList2 = new[] { tree3, tree1 };
            var compilation2 = compilation1.RemoveSyntaxTrees(treeList2);
            CheckCompilationSyntaxTrees(compilation1, treeList1); //compilation1 untouched
            CheckCompilationSyntaxTrees(compilation2, tree2, tree4);

            SyntaxTree[] treeList3 = new[] { tree4, tree3, tree2, tree1 };
            var compilation3 = CSharpCompilation.Create("Compilation3", syntaxTrees: treeList3);
            CheckCompilationSyntaxTrees(compilation3, treeList3);

            SyntaxTree[] treeList4 = new[] { tree3, tree1 };
            var compilation4 = compilation3.RemoveSyntaxTrees(treeList4);
            CheckCompilationSyntaxTrees(compilation3, treeList3); //compilation3 untouched
            CheckCompilationSyntaxTrees(compilation4, tree4, tree2);
        }

        [Fact]
        public void SyntaxTreeOrderReplace()
        {
            var tree1 = CreateSyntaxTree("A");
            var tree2 = CreateSyntaxTree("B");
            var tree3 = CreateSyntaxTree("C");

            SyntaxTree[] treeList1 = new[] { tree1, tree2 };
            var compilation1 = CSharpCompilation.Create("Compilation1", syntaxTrees: treeList1);
            CheckCompilationSyntaxTrees(compilation1, treeList1);

            var compilation2 = compilation1.ReplaceSyntaxTree(tree1, tree3);
            CheckCompilationSyntaxTrees(compilation1, treeList1); //compilation1 untouched
            CheckCompilationSyntaxTrees(compilation2, tree3, tree2);

            SyntaxTree[] treeList3 = new[] { tree2, tree1 };
            var compilation3 = CSharpCompilation.Create("Compilation3", syntaxTrees: treeList3);
            CheckCompilationSyntaxTrees(compilation3, treeList3);

            var compilation4 = compilation3.ReplaceSyntaxTree(tree1, tree3);
            CheckCompilationSyntaxTrees(compilation3, treeList3); //compilation3 untouched
            CheckCompilationSyntaxTrees(compilation4, tree2, tree3);
        }

        [WorkItem(578706, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578706")]
        [Fact]
        public void DeclaringCompilationOfAddedModule()
        {
            var source1 = "public class C1 { }";
            var source2 = "public class C2 { }";

            var lib1 = CreateCompilationWithMscorlib(source1, assemblyName: "Lib1", options: TestOptions.ReleaseModule);
            var ref1 = lib1.EmitToImageReference(); // NOTE: can't use a compilation reference for a module.

            var lib2 = CreateCompilationWithMscorlib(source2, new[] { ref1 }, assemblyName: "Lib2");
            lib2.VerifyDiagnostics();

            var sourceAssembly = lib2.Assembly;
            var sourceModule = sourceAssembly.Modules[0];
            var sourceType = sourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C2");

            Assert.IsType<SourceAssemblySymbol>(sourceAssembly);
            Assert.Equal(lib2, sourceAssembly.DeclaringCompilation);

            Assert.IsType<SourceModuleSymbol>(sourceModule);
            Assert.Equal(lib2, sourceModule.DeclaringCompilation);

            Assert.IsType<SourceNamedTypeSymbol>(sourceType);
            Assert.Equal(lib2, sourceType.DeclaringCompilation);


            var addedModule = sourceAssembly.Modules[1];
            var addedModuleAssembly = addedModule.ContainingAssembly;
            var addedModuleType = addedModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C1");

            Assert.IsType<SourceAssemblySymbol>(addedModuleAssembly);
            Assert.Equal(lib2, addedModuleAssembly.DeclaringCompilation); //NB: not lib1, not null

            Assert.IsType<PEModuleSymbol>(addedModule);
            Assert.Null(addedModule.DeclaringCompilation);

            Assert.IsAssignableFrom<PENamedTypeSymbol>(addedModuleType);
            Assert.Null(addedModuleType.DeclaringCompilation);
        }
    }
}
