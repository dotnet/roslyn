// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

//test

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class MissingTypeReferences : CSharpTestBase
    {
        [Fact]
        public void Test1()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(TestReferences.SymbolsTests.MDTestLib2);

            TestMissingTypeReferencesHelper1(assembly);

            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(mrefs: new[]
                                    {
                                        TestReferences.SymbolsTests.MissingTypes.MDMissingType,
                                        TestReferences.SymbolsTests.MissingTypes.MDMissingTypeLib,
                                        TestMetadata.Net40.mscorlib
                                    });

            TestMissingTypeReferencesHelper2(assemblies);
        }

        private void TestMissingTypeReferencesHelper1(AssemblySymbol assembly)
        {
            var module0 = assembly.Modules[0];

            var localTC10 = module0.GlobalNamespace.GetTypeMembers("TC10").Single();

            MissingMetadataTypeSymbol @base = (MissingMetadataTypeSymbol)localTC10.BaseType();
            Assert.Equal(SymbolKind.ErrorType, @base.Kind);
            Assert.False(@base.IsNamespace);
            Assert.True(@base.IsType);
            Assert.Equal("Object", @base.Name);
            Assert.Equal("System", @base.ContainingSymbol.Name);
            Assert.Equal(0, @base.Arity);
            Assert.Equal("System.Object[missing]", @base.ToTestDisplayString());
            Assert.NotNull(@base.ContainingAssembly);
            Assert.NotNull(@base.ContainingNamespace);
            Assert.NotNull(@base.ContainingSymbol);
            Assert.True(@base.ContainingAssembly.IsMissing);
            Assert.Equal("mscorlib", @base.ContainingAssembly.Identity.Name);

            var localTC8 = module0.GlobalNamespace.GetTypeMembers("TC8").Single();
            var genericBase = (ErrorTypeSymbol)localTC8.BaseType();
            Assert.Equal("C1<System.Type[missing]>[missing]", genericBase.ToTestDisplayString());

            @base = (MissingMetadataTypeSymbol)genericBase.ConstructedFrom;
            Assert.Equal(SymbolKind.ErrorType, @base.Kind);
            Assert.False(@base.IsNamespace);
            Assert.True(@base.IsType);
            Assert.Equal("C1", @base.Name);
            Assert.Equal(1, @base.Arity);
            Assert.Equal("C1<>[missing]", @base.ToTestDisplayString());
            Assert.NotNull(@base.ContainingAssembly);
            Assert.NotNull(@base.ContainingNamespace);
            Assert.NotNull(@base.ContainingSymbol);
            Assert.True(@base.ContainingAssembly.IsMissing);
            Assert.Equal("MDTestLib1", @base.ContainingAssembly.Identity.Name);

            var localTC7 = module0.GlobalNamespace.GetTypeMembers("TC7").Single();
            genericBase = (ErrorTypeSymbol)localTC7.BaseType();
            @base = (MissingMetadataTypeSymbol)genericBase.OriginalDefinition;

            Assert.Equal("C1<TC7_T1>[missing].C3[missing].C4<TC7_T2>[missing]", genericBase.ToTestDisplayString());
            Assert.True(genericBase.ContainingAssembly.IsMissing);
            Assert.True(@base.ContainingAssembly.IsMissing);
            Assert.Equal(@base.GetUseSiteDiagnostic().ToString(), genericBase.GetUseSiteDiagnostic().ToString());
            Assert.Equal(@base.ErrorInfo.ToString(), genericBase.ErrorInfo.ToString());

            var constructedFrom = genericBase.ConstructedFrom;
            Assert.Equal("C1<TC7_T1>[missing].C3[missing].C4<>[missing]", constructedFrom.ToTestDisplayString());

            Assert.Same(constructedFrom, constructedFrom.Construct(constructedFrom.TypeParameters.ToArray()));
            Assert.Equal(genericBase, constructedFrom.Construct(genericBase.TypeArguments()));

            genericBase = (ErrorTypeSymbol)genericBase.ContainingSymbol;
            Assert.Equal("C1<TC7_T1>[missing].C3[missing]", genericBase.ToTestDisplayString());
            Assert.Same(genericBase, genericBase.ConstructedFrom);

            genericBase = (ErrorTypeSymbol)genericBase.ContainingSymbol;
            Assert.Equal("C1<TC7_T1>[missing]", genericBase.ToTestDisplayString());
            Assert.Same(genericBase.OriginalDefinition, genericBase.ConstructedFrom);
            Assert.Equal("C1<>[missing]", genericBase.OriginalDefinition.ToTestDisplayString());

            Assert.Equal(SymbolKind.ErrorType, @base.Kind);
            Assert.False(@base.IsNamespace);
            Assert.True(@base.IsType);
            Assert.Equal("C4", @base.Name);
            Assert.Equal(1, @base.Arity);
            Assert.Equal("C1<>[missing].C3[missing].C4<>[missing]", @base.ToTestDisplayString());
            Assert.NotNull(@base.ContainingAssembly);
            Assert.NotNull(@base.ContainingNamespace);
            Assert.NotNull(@base.ContainingSymbol);
            Assert.Equal("MDTestLib1", @base.ContainingAssembly.Identity.Name);

            Assert.Equal(SymbolKind.ErrorType, @base.ContainingSymbol.Kind);
            Assert.NotNull(@base.ContainingSymbol.ContainingAssembly);
            Assert.Same(@base.ContainingAssembly, @base.ContainingSymbol.ContainingAssembly);

            Assert.Equal(SymbolKind.ErrorType, @base.ContainingSymbol.ContainingSymbol.Kind);
            Assert.NotNull(@base.ContainingSymbol.ContainingSymbol.ContainingAssembly);
            Assert.Same(@base.ContainingAssembly, @base.ContainingSymbol.ContainingSymbol.ContainingAssembly);
        }

        private void TestMissingTypeReferencesHelper2(AssemblySymbol[] assemblies, bool reflectionOnly = false)
        {
            var module1 = assemblies[0].Modules[0];
            var module2 = assemblies[1].Modules[0];

            var assembly2 = (MetadataOrSourceAssemblySymbol)assemblies[1];

            NamedTypeSymbol localTC = module1.GlobalNamespace.GetTypeMembers("TC1").Single();
            var @base = (MissingMetadataTypeSymbol)localTC.BaseType();
            Assert.Equal(SymbolKind.ErrorType, @base.Kind);
            Assert.False(@base.IsNamespace);
            Assert.True(@base.IsType);
            Assert.Equal("MissingC1", @base.Name);
            Assert.Equal(0, @base.Arity);
            Assert.Equal("MissingNS1.MissingC1[missing]", @base.ToTestDisplayString());
            Assert.Same(@base.ContainingAssembly, module2.ContainingAssembly);
            Assert.NotNull(@base.ContainingNamespace);
            Assert.Equal("MissingNS1", @base.ContainingNamespace.Name);
            Assert.Equal("", @base.ContainingNamespace.ContainingNamespace.Name);
            Assert.NotNull(@base.ContainingSymbol);
            Assert.NotNull(@base.ContainingAssembly);

            localTC = module1.GlobalNamespace.GetTypeMembers("TC2").Single();
            @base = (MissingMetadataTypeSymbol)localTC.BaseType();
            Assert.Equal(SymbolKind.ErrorType, @base.Kind);
            Assert.False(@base.IsNamespace);
            Assert.True(@base.IsType);
            Assert.Equal("MissingC2", @base.Name);
            Assert.Equal(0, @base.Arity);
            Assert.Equal("MissingNS2.MissingNS3.MissingC2[missing]", @base.ToTestDisplayString());
            Assert.Same(@base.ContainingAssembly, module2.ContainingAssembly);
            Assert.Equal("MissingNS3", @base.ContainingNamespace.Name);
            Assert.Equal("MissingNS2", @base.ContainingNamespace.ContainingNamespace.Name);
            Assert.Equal("", @base.ContainingNamespace.ContainingNamespace.ContainingNamespace.Name);
            Assert.NotNull(@base.ContainingSymbol);
            Assert.NotNull(@base.ContainingAssembly);

            localTC = module1.GlobalNamespace.GetTypeMembers("TC3").Single();
            @base = (MissingMetadataTypeSymbol)localTC.BaseType();
            Assert.Equal(SymbolKind.ErrorType, @base.Kind);
            Assert.False(@base.IsNamespace);
            Assert.True(@base.IsType);
            Assert.Equal("MissingC3", @base.Name);
            Assert.Equal(0, @base.Arity);
            Assert.Equal("NS4.MissingNS5.MissingC3[missing]", @base.ToTestDisplayString());
            Assert.Same(@base.ContainingAssembly, module2.ContainingAssembly);
            Assert.NotNull(@base.ContainingNamespace);
            Assert.NotNull(@base.ContainingSymbol);
            Assert.NotNull(@base.ContainingModule);

            localTC = module1.GlobalNamespace.GetTypeMembers("TC4").Single();
            var genericBase = localTC.BaseType();
            Assert.Equal(SymbolKind.ErrorType, genericBase.Kind);
            Assert.Equal("MissingC4<T1, S1>[missing]", genericBase.ToTestDisplayString());

            @base = (MissingMetadataTypeSymbol)genericBase.OriginalDefinition;
            Assert.Equal(SymbolKind.ErrorType, @base.Kind);
            Assert.False(@base.IsNamespace);
            Assert.True(@base.IsType);
            Assert.Equal("MissingC4", @base.Name);
            Assert.Equal(2, @base.Arity);
            Assert.Equal("MissingC4<,>[missing]", @base.ToTestDisplayString());
            Assert.Same(@base.ContainingAssembly, module2.ContainingAssembly);
            Assert.NotNull(@base.ContainingNamespace);
            Assert.NotNull(@base.ContainingSymbol);
            Assert.NotNull(@base.ContainingModule);
            var missingC4 = @base;

            localTC = module1.GlobalNamespace.GetTypeMembers("TC5").Single();
            genericBase = localTC.BaseType();
            Assert.Equal("MissingC4<T1, S1>[missing].MissingC5<U1, V1, W1>[missing]", genericBase.ToTestDisplayString());

            @base = (MissingMetadataTypeSymbol)genericBase.OriginalDefinition;
            Assert.Equal(SymbolKind.ErrorType, @base.Kind);
            Assert.False(@base.IsNamespace);
            Assert.True(@base.IsType);
            Assert.Equal("MissingC5", @base.Name);
            Assert.Equal(3, @base.Arity);
            Assert.Equal("MissingC4<,>[missing].MissingC5<,,>[missing]", @base.ToTestDisplayString());
            Assert.Same(@base.ContainingAssembly, module2.ContainingAssembly);
            Assert.True(@base.ContainingNamespace.IsGlobalNamespace);
            Assert.Same(@base.ContainingSymbol, missingC4);

            var localC6 = module2.GlobalNamespace.GetTypeMembers("C6").Single();

            localTC = module1.GlobalNamespace.GetTypeMembers("TC6").Single();

            genericBase = localTC.BaseType();
            Assert.Equal("C6.MissingC7<U, V>[missing]", genericBase.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, genericBase.ContainingSymbol.Kind);

            @base = (MissingMetadataTypeSymbol)genericBase.OriginalDefinition;
            Assert.Equal(SymbolKind.ErrorType, @base.Kind);
            Assert.False(@base.IsNamespace);
            Assert.True(@base.IsType);
            Assert.Equal("MissingC7", @base.Name);
            Assert.Equal(2, @base.Arity);
            Assert.Equal("C6.MissingC7<,>[missing]", @base.ToTestDisplayString());
            Assert.Same(@base.ContainingAssembly, module2.ContainingAssembly);
            Assert.Same(@base.ContainingSymbol, localC6);
            Assert.Same(@base.ContainingNamespace, localC6.ContainingNamespace);

            var missingC7 = @base;

            localTC = module1.GlobalNamespace.GetTypeMembers("TC7").Single();
            genericBase = localTC.BaseType();
            Assert.Equal("C6.MissingC7<U, V>[missing].MissingC8[missing]", genericBase.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, genericBase.ContainingSymbol.Kind);

            @base = (MissingMetadataTypeSymbol)genericBase.OriginalDefinition;
            Assert.Equal(SymbolKind.ErrorType, @base.Kind);
            Assert.False(@base.IsNamespace);
            Assert.True(@base.IsType);
            Assert.Equal("MissingC8", @base.Name);
            Assert.Equal(0, @base.Arity);
            Assert.Equal("C6.MissingC7<,>[missing].MissingC8[missing]", @base.ToTestDisplayString());
            Assert.Same(@base.ContainingAssembly, module2.ContainingAssembly);
            if (!reflectionOnly)
            {
                Assert.Same(@base.ContainingSymbol, missingC7);
            }
            Assert.Equal(missingC7.ToTestDisplayString(), @base.ContainingSymbol.ToTestDisplayString());
            Assert.Same(@base.ContainingNamespace, localC6.ContainingNamespace);

            var missingC8 = @base;

            localTC = module1.GlobalNamespace.GetTypeMembers("TC8").Single();
            genericBase = localTC.BaseType();
            Assert.Equal("C6.MissingC7<U, V>[missing].MissingC8[missing].MissingC9[missing]", genericBase.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, genericBase.ContainingSymbol.Kind);

            @base = (MissingMetadataTypeSymbol)genericBase.OriginalDefinition;
            Assert.Equal(SymbolKind.ErrorType, @base.Kind);
            Assert.False(@base.IsNamespace);
            Assert.True(@base.IsType);
            Assert.Equal("MissingC9", @base.Name);
            Assert.Equal(0, @base.Arity);
            Assert.Equal("C6.MissingC7<,>[missing].MissingC8[missing].MissingC9[missing]", @base.ToTestDisplayString());
            Assert.Same(@base.ContainingAssembly, module2.ContainingAssembly);
            if (!reflectionOnly)
            {
                Assert.Same(@base.ContainingSymbol, missingC8);
            }
            Assert.Equal(missingC8.ToTestDisplayString(), @base.ContainingSymbol.ToTestDisplayString());
            Assert.Same(@base.ContainingNamespace, localC6.ContainingNamespace);

            Assert.IsAssignableFrom<MissingMetadataTypeSymbol>(assembly2.CachedTypeByEmittedName("MissingNS1.MissingC1"));
            Assert.IsAssignableFrom<MissingMetadataTypeSymbol>(assembly2.CachedTypeByEmittedName("MissingNS2.MissingNS3.MissingC2"));
            Assert.IsAssignableFrom<MissingMetadataTypeSymbol>(assembly2.CachedTypeByEmittedName("NS4.MissingNS5.MissingC3"));
            Assert.IsAssignableFrom<MissingMetadataTypeSymbol>(assembly2.CachedTypeByEmittedName("MissingC4`2"));
        }

        [Fact]
        public void Equality()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.MissingTypes.MissingTypesEquality1,
                TestReferences.SymbolsTests.MissingTypes.MissingTypesEquality2,
                TestReferences.SymbolsTests.MDTestLib1,
                TestReferences.SymbolsTests.MDTestLib2
            });

            var asm1 = assemblies[0];

            var asm1classC = asm1.GlobalNamespace.GetTypeMembers("C").Single();

            var asm1m1 = asm1classC.GetMembers("M1").OfType<MethodSymbol>().Single();
            var asm1m2 = asm1classC.GetMembers("M2").OfType<MethodSymbol>().Single();
            var asm1m3 = asm1classC.GetMembers("M3").OfType<MethodSymbol>().Single();
            var asm1m4 = asm1classC.GetMembers("M4").OfType<MethodSymbol>().Single();
            var asm1m5 = asm1classC.GetMembers("M5").OfType<MethodSymbol>().Single();
            var asm1m6 = asm1classC.GetMembers("M6").OfType<MethodSymbol>().Single();
            var asm1m7 = asm1classC.GetMembers("M7").OfType<MethodSymbol>().Single();
            var asm1m8 = asm1classC.GetMembers("M8").OfType<MethodSymbol>().Single();

            Assert.NotEqual(asm1m2.ReturnType, asm1m1.ReturnType);
            Assert.NotEqual(asm1m3.ReturnType, asm1m1.ReturnType);
            Assert.NotEqual(asm1m4.ReturnType, asm1m1.ReturnType);

            Assert.NotEqual(asm1m5.ReturnType, asm1m4.ReturnType);
            Assert.NotEqual(asm1m6.ReturnType, asm1m4.ReturnType);

            Assert.Equal(asm1m7.ReturnType, asm1m1.ReturnType);
            Assert.Equal(asm1m8.ReturnType, asm1m4.ReturnType);

            var asm2 = assemblies[1];

            var asm2classC = asm2.GlobalNamespace.GetTypeMembers("C").Single();

            var asm2m1 = asm2classC.GetMembers("M1").OfType<MethodSymbol>().Single();
            var asm2m4 = asm2classC.GetMembers("M4").OfType<MethodSymbol>().Single();

            Assert.Equal(asm2m1.ReturnType, asm1m1.ReturnType);

            Assert.NotSame(asm1m4.ReturnType, asm2m4.ReturnType);
            Assert.Equal(asm2m4.ReturnType, asm1m4.ReturnType);

            Assert.Equal(asm1.GetSpecialType(SpecialType.System_Boolean), asm1.GetSpecialType(SpecialType.System_Boolean));
            Assert.Equal(asm1.GetSpecialType(SpecialType.System_Boolean), asm2.GetSpecialType(SpecialType.System_Boolean));

            MissingMetadataTypeSymbol[] missingTypes1 = new MissingMetadataTypeSymbol[15];
            MissingMetadataTypeSymbol[] missingTypes2 = new MissingMetadataTypeSymbol[15];

            var defaultName = new AssemblyIdentity("missing");

            missingTypes1[0] = new MissingMetadataTypeSymbol.TopLevel(new MissingAssemblySymbol(defaultName).Modules[0], "", "test1", 0, true);
            missingTypes1[1] = new MissingMetadataTypeSymbol.TopLevel(new MissingAssemblySymbol(defaultName).Modules[0], "", "test1", 1, true);
            missingTypes1[2] = new MissingMetadataTypeSymbol.TopLevel(new MissingAssemblySymbol(defaultName).Modules[0], "", "test2", 0, true);
            missingTypes1[3] = new MissingMetadataTypeSymbol.TopLevel(new MissingAssemblySymbol(new AssemblyIdentity("asm1")).Modules[0], "", "test1", 0, true);
            missingTypes1[4] = new MissingMetadataTypeSymbol.TopLevel(new MissingAssemblySymbol(new AssemblyIdentity("asm1")).Modules[0], "", "test1", 1, true);
            missingTypes1[5] = new MissingMetadataTypeSymbol.TopLevel(new MissingAssemblySymbol(new AssemblyIdentity("asm1")).Modules[0], "", "test2", 0, true);
            missingTypes1[6] = new MissingMetadataTypeSymbol.TopLevel(new MissingAssemblySymbol(new AssemblyIdentity("asm2")).Modules[0], "", "test1", 0, true);
            missingTypes1[7] = new MissingMetadataTypeSymbol.TopLevel(asm1.Modules[0], "", "test1", 0, true);
            missingTypes1[8] = new MissingMetadataTypeSymbol.TopLevel(asm1.Modules[0], "", "test1", 1, true);
            missingTypes1[9] = new MissingMetadataTypeSymbol.TopLevel(asm1.Modules[0], "", "test2", 0, true);
            missingTypes1[10] = new MissingMetadataTypeSymbol.TopLevel(asm2.Modules[0], "", "test1", 0, true);
            missingTypes1[11] = new MissingMetadataTypeSymbol.Nested(asm1classC, "test1", 0, true);
            missingTypes1[12] = new MissingMetadataTypeSymbol.Nested(asm1classC, "test1", 1, true);
            missingTypes1[13] = new MissingMetadataTypeSymbol.Nested(asm1classC, "test2", 0, true);
            missingTypes1[14] = new MissingMetadataTypeSymbol.Nested(asm2classC, "test1", 0, true);

            missingTypes2[0] = new MissingMetadataTypeSymbol.TopLevel(new MissingAssemblySymbol(defaultName).Modules[0], "", "test1", 0, true);
            missingTypes2[1] = new MissingMetadataTypeSymbol.TopLevel(new MissingAssemblySymbol(defaultName).Modules[0], "", "test1", 1, true);
            missingTypes2[2] = new MissingMetadataTypeSymbol.TopLevel(new MissingAssemblySymbol(defaultName).Modules[0], "", "test2", 0, true);
            missingTypes2[3] = new MissingMetadataTypeSymbol.TopLevel(new MissingAssemblySymbol(new AssemblyIdentity("asm1")).Modules[0], "", "test1", 0, true);
            missingTypes2[4] = new MissingMetadataTypeSymbol.TopLevel(new MissingAssemblySymbol(new AssemblyIdentity("asm1")).Modules[0], "", "test1", 1, true);
            missingTypes2[5] = new MissingMetadataTypeSymbol.TopLevel(new MissingAssemblySymbol(new AssemblyIdentity("asm1")).Modules[0], "", "test2", 0, true);
            missingTypes2[6] = new MissingMetadataTypeSymbol.TopLevel(new MissingAssemblySymbol(new AssemblyIdentity("asm2")).Modules[0], "", "test1", 0, true);
            missingTypes2[7] = new MissingMetadataTypeSymbol.TopLevel(asm1.Modules[0], "", "test1", 0, true);
            missingTypes2[8] = new MissingMetadataTypeSymbol.TopLevel(asm1.Modules[0], "", "test1", 1, true);
            missingTypes2[9] = new MissingMetadataTypeSymbol.TopLevel(asm1.Modules[0], "", "test2", 0, true);
            missingTypes2[10] = new MissingMetadataTypeSymbol.TopLevel(asm2.Modules[0], "", "test1", 0, true);
            missingTypes2[11] = new MissingMetadataTypeSymbol.Nested(asm1classC, "test1", 0, true);
            missingTypes2[12] = new MissingMetadataTypeSymbol.Nested(asm1classC, "test1", 1, true);
            missingTypes2[13] = new MissingMetadataTypeSymbol.Nested(asm1classC, "test2", 0, true);
            missingTypes2[14] = new MissingMetadataTypeSymbol.Nested(asm2classC, "test1", 0, true);

            for (int i = 0; i < missingTypes1.Length; i++)
            {
                for (int j = 0; j < missingTypes2.Length; j++)
                {
                    if (i == j)
                    {
                        Assert.Equal(missingTypes2[j], missingTypes1[i]);
                        Assert.Equal(missingTypes1[i], missingTypes2[j]);
                    }
                    else
                    {
                        Assert.NotEqual(missingTypes2[j], missingTypes1[i]);
                        Assert.NotEqual(missingTypes1[i], missingTypes2[j]);
                    }
                }
            }

            var missingAssembly = new MissingAssemblySymbol(new AssemblyIdentity("asm1"));
            Assert.True(missingAssembly.Equals(missingAssembly));
            Assert.NotEqual(new object(), missingAssembly);
            Assert.False(missingAssembly.Equals(null));
        }
    }
}
