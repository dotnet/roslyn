// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using CSReferenceManager = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.ReferenceManager;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class BaseTypeResolution : CSharpTestBase
    {
        [Fact]
        public void Test1()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(TestReferences.NetFx.v4_0_21006.mscorlib);

            TestBaseTypeResolutionHelper1(assembly);

            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(mrefs: new[]
            {
                TestReferences.SymbolsTests.MDTestLib1,
                TestReferences.SymbolsTests.MDTestLib2,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            TestBaseTypeResolutionHelper2(assemblies);

            assemblies = MetadataTestHelpers.GetSymbolsForReferences(mrefs: new[]
            {
                TestReferences.SymbolsTests.MDTestLib1,
                TestReferences.SymbolsTests.MDTestLib2
            });

            // TestBaseTypeResolutionHelper3(assemblies); // TODO(alekseyt): this test is not valid.  See email of 7/23/2010 for explanation.

            assemblies = MetadataTestHelpers.GetSymbolsForReferences(mrefs: new[]
            {
                TestReferences.SymbolsTests.MultiModule.Assembly,
                TestReferences.SymbolsTests.MultiModule.Consumer
            });

            TestBaseTypeResolutionHelper4(assemblies);
        }

        private void TestBaseTypeResolutionHelper1(AssemblySymbol assembly)
        {
            var module0 = assembly.Modules[0];

            var sys = module0.GlobalNamespace.GetMembers("System");
            var collections = ((NamespaceSymbol)sys[0]).GetMembers("Collections");
            var generic = ((NamespaceSymbol)collections[0]).GetMembers("Generic");
            var dictionary = ((NamespaceSymbol)generic[0]).GetMembers("Dictionary");
            var @base = ((NamedTypeSymbol)dictionary[0]).BaseType();

            AssertBaseType(@base, "System.Object");
            Assert.Null(@base.BaseType());

            var concurrent = ((NamespaceSymbol)collections[0]).GetMembers("Concurrent");

            var orderablePartitioners = ((NamespaceSymbol)concurrent[0]).GetMembers("OrderablePartitioner");
            NamedTypeSymbol orderablePartitioner = null;

            foreach (var p in orderablePartitioners)
            {
                var t = p as NamedTypeSymbol;

                if ((object)t != null && t.Arity == 1)
                {
                    orderablePartitioner = t;
                    break;
                }
            }

            @base = orderablePartitioner.BaseType();

            AssertBaseType(@base, "System.Collections.Concurrent.Partitioner<TSource>");
            Assert.Same(((NamedTypeSymbol)@base).TypeArguments()[0], orderablePartitioner.TypeParameters[0]);

            var partitioners = ((NamespaceSymbol)concurrent[0]).GetMembers("Partitioner");
            NamedTypeSymbol partitioner = null;

            foreach (var p in partitioners)
            {
                var t = p as NamedTypeSymbol;

                if ((object)t != null && t.Arity == 0)
                {
                    partitioner = t;
                    break;
                }
            }

            Assert.NotNull(partitioner);
        }

        private void TestBaseTypeResolutionHelper2(AssemblySymbol[] assemblies)
        {
            var module1 = assemblies[0].Modules[0];
            var module2 = assemblies[1].Modules[0];

            var varTC2 = module1.GlobalNamespace.GetTypeMembers("TC2").Single();
            var varTC3 = module1.GlobalNamespace.GetTypeMembers("TC3").Single();
            var varTC4 = module1.GlobalNamespace.GetTypeMembers("TC4").Single();

            AssertBaseType(varTC2.BaseType(), "C1<TC2_T1>.C2<TC2_T2>");
            AssertBaseType(varTC3.BaseType(), "C1<TC3_T1>.C3");
            AssertBaseType(varTC4.BaseType(), "C1<TC4_T1>.C3.C4<TC4_T2>");

            var varC1 = module1.GlobalNamespace.GetTypeMembers("C1").Single();
            AssertBaseType(varC1.BaseType(), "System.Object");
            Assert.Equal(0, varC1.Interfaces().Length);

            var varTC5 = module2.GlobalNamespace.GetTypeMembers("TC5").Single();
            var varTC6 = module2.GlobalNamespace.GetTypeMembers("TC6").Single();
            var varTC7 = module2.GlobalNamespace.GetTypeMembers("TC7").Single();
            var varTC8 = module2.GlobalNamespace.GetTypeMembers("TC8").Single();
            var varTC9 = varTC6.GetTypeMembers("TC9").Single();

            AssertBaseType(varTC5.BaseType(), "C1<TC5_T1>.C2<TC5_T2>");
            AssertBaseType(varTC6.BaseType(), "C1<TC6_T1>.C3");
            AssertBaseType(varTC7.BaseType(), "C1<TC7_T1>.C3.C4<TC7_T2>");
            AssertBaseType(varTC8.BaseType(), "C1<System.Type>");
            AssertBaseType(varTC9.BaseType(), "TC6<TC6_T1>");

            var varCorTypes = module2.GlobalNamespace.GetMembers("CorTypes").OfType<NamespaceSymbol>().Single();

            var varCorTypes_Derived = varCorTypes.GetTypeMembers("Derived").Single();
            AssertBaseType(varCorTypes_Derived.BaseType(),
                           "CorTypes.NS.Base<System.Boolean, System.SByte, System.Byte, System.Int16, System.UInt16, System.Int32, System.UInt32, System.Int64, System.UInt64, System.Single, System.Double, System.Char, System.String, System.IntPtr, System.UIntPtr, System.Object>");

            var varCorTypes_Derived1 = varCorTypes.GetTypeMembers("Derived1").Single();
            AssertBaseType(varCorTypes_Derived1.BaseType(),
                           "CorTypes.Base<System.Int32[], System.Double[,]>");

            var varI101 = module1.GlobalNamespace.GetTypeMembers("I101").Single();
            var varI102 = module1.GlobalNamespace.GetTypeMembers("I102").Single();

            var varC203 = module1.GlobalNamespace.GetTypeMembers("C203").Single();
            Assert.Equal(1, varC203.Interfaces().Length);
            Assert.Same(varI101, varC203.Interfaces()[0]);

            var varC204 = module1.GlobalNamespace.GetTypeMembers("C204").Single();
            Assert.Equal(2, varC204.Interfaces().Length);
            Assert.Same(varI101, varC204.Interfaces()[0]);
            Assert.Same(varI102, varC204.Interfaces()[1]);
        }

        private void TestBaseTypeResolutionHelper3(AssemblySymbol[] assemblies)
        {
            var module1 = assemblies[0].Modules[0];
            var module2 = assemblies[1].Modules[0];

            var varCorTypes = module2.GlobalNamespace.GetMembers("CorTypes").OfType<NamespaceSymbol>().Single();

            var varCorTypes_Derived = varCorTypes.GetTypeMembers("Derived").Single();
            AssertBaseType(varCorTypes_Derived.BaseType(),
                           "CorTypes.NS.Base<System.Boolean,System.SByte,System.Byte,System.Int16,System.UInt16,System.Int32,System.UInt32,System.Int64,System.UInt64,System.Single,System.Double,System.Char,System.String,System.IntPtr,System.UIntPtr,System.Object>");

            foreach (var arg in varCorTypes_Derived.BaseType().TypeArguments())
            {
                Assert.IsAssignableFrom<MissingMetadataTypeSymbol>(arg);
            }
        }

        private void TestBaseTypeResolutionHelper4(AssemblySymbol[] assemblies)
        {
            var module1 = assemblies[0].Modules[0];
            var module2 = assemblies[0].Modules[1];
            var module3 = assemblies[0].Modules[2];
            var module0 = assemblies[1].Modules[0];

            var derived1 = module0.GlobalNamespace.GetTypeMembers("Derived1").Single();
            var base1 = derived1.BaseType();

            var derived2 = module0.GlobalNamespace.GetTypeMembers("Derived2").Single();
            var base2 = derived2.BaseType();

            var derived3 = module0.GlobalNamespace.GetTypeMembers("Derived3").Single();
            var base3 = derived3.BaseType();

            AssertBaseType(base1, "Class1");
            AssertBaseType(base2, "Class2");
            AssertBaseType(base3, "Class3");

            Assert.Same(base1, module1.GlobalNamespace.GetTypeMembers("Class1").Single());
            Assert.Same(base2, module2.GlobalNamespace.GetTypeMembers("Class2").Single());
            Assert.Same(base3, module3.GlobalNamespace.GetTypeMembers("Class3").Single());

            return;
        }

        internal static void AssertBaseType(TypeSymbol @base, string name)
        {
            Assert.NotEqual(SymbolKind.ErrorType, @base.Kind);
            Assert.Equal(name, @base.ToTestDisplayString());
        }

        [Fact]
        public void Test2()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(mrefs: new[]
                                    {
                                        TestReferences.SymbolsTests.DifferByCase.Consumer,
                                        TestReferences.SymbolsTests.DifferByCase.TypeAndNamespaceDifferByCase
                                    });

            var module0 = assemblies[0].Modules[0] as PEModuleSymbol;
            var module1 = assemblies[1].Modules[0] as PEModuleSymbol;

            var bases = new HashSet<NamedTypeSymbol>();

            var localTC1 = module0.GlobalNamespace.GetTypeMembers("TC1").Single();
            var base1 = localTC1.BaseType();
            bases.Add(base1);
            Assert.NotEqual(SymbolKind.ErrorType, base1.Kind);
            Assert.Equal("SomeName.Dummy", base1.ToTestDisplayString());

            var localTC2 = module0.GlobalNamespace.GetTypeMembers("TC2").Single();
            var base2 = localTC2.BaseType();
            bases.Add(base2);
            Assert.NotEqual(SymbolKind.ErrorType, base2.Kind);
            Assert.Equal("somEnamE", base2.ToTestDisplayString());

            var localTC3 = module0.GlobalNamespace.GetTypeMembers("TC3").Single();
            var base3 = localTC3.BaseType();
            bases.Add(base3);
            Assert.NotEqual(SymbolKind.ErrorType, base3.Kind);
            Assert.Equal("somEnamE1", base3.ToTestDisplayString());

            var localTC4 = module0.GlobalNamespace.GetTypeMembers("TC4").Single();
            var base4 = localTC4.BaseType();
            bases.Add(base4);
            Assert.NotEqual(SymbolKind.ErrorType, base4.Kind);
            Assert.Equal("SomeName1", base4.ToTestDisplayString());

            var localTC5 = module0.GlobalNamespace.GetTypeMembers("TC5").Single();
            var base5 = localTC5.BaseType();
            bases.Add(base5);
            Assert.NotEqual(SymbolKind.ErrorType, base5.Kind);
            Assert.Equal("somEnamE2.OtherName", base5.ToTestDisplayString());

            var localTC6 = module0.GlobalNamespace.GetTypeMembers("TC6").Single();
            var base6 = localTC6.BaseType();
            bases.Add(base6);
            Assert.NotEqual(SymbolKind.ErrorType, base6.Kind);
            Assert.Equal("SomeName2.OtherName", base6.ToTestDisplayString());

            var localTC7 = module0.GlobalNamespace.GetTypeMembers("TC7").Single();
            var base7 = localTC7.BaseType();
            bases.Add(base7);
            Assert.NotEqual(SymbolKind.ErrorType, base7.Kind);
            Assert.Equal("NestingClass.somEnamE3", base7.ToTestDisplayString());

            var localTC8 = module0.GlobalNamespace.GetTypeMembers("TC8").Single();
            var base8 = localTC8.BaseType();
            bases.Add(base8);
            Assert.NotEqual(SymbolKind.ErrorType, base8.Kind);
            Assert.Equal("NestingClass.SomeName3", base8.ToTestDisplayString());

            Assert.Equal(8, bases.Count);

            Assert.Equal(base1, module1.TypeHandleToTypeMap[((PENamedTypeSymbol)base1).Handle]);
            Assert.Equal(base2, module1.TypeHandleToTypeMap[((PENamedTypeSymbol)base2).Handle]);
            Assert.Equal(base3, module1.TypeHandleToTypeMap[((PENamedTypeSymbol)base3).Handle]);
            Assert.Equal(base4, module1.TypeHandleToTypeMap[((PENamedTypeSymbol)base4).Handle]);
            Assert.Equal(base5, module1.TypeHandleToTypeMap[((PENamedTypeSymbol)base5).Handle]);
            Assert.Equal(base6, module1.TypeHandleToTypeMap[((PENamedTypeSymbol)base6).Handle]);
            Assert.Equal(base7, module1.TypeHandleToTypeMap[((PENamedTypeSymbol)base7).Handle]);
            Assert.Equal(base8, module1.TypeHandleToTypeMap[((PENamedTypeSymbol)base8).Handle]);

            Assert.Equal(base1, module0.TypeRefHandleToTypeMap[(TypeReferenceHandle)module0.Module.GetBaseTypeOfTypeOrThrow(((PENamedTypeSymbol)localTC1).Handle)]);
            Assert.Equal(base2, module0.TypeRefHandleToTypeMap[(TypeReferenceHandle)module0.Module.GetBaseTypeOfTypeOrThrow(((PENamedTypeSymbol)localTC2).Handle)]);
            Assert.Equal(base3, module0.TypeRefHandleToTypeMap[(TypeReferenceHandle)module0.Module.GetBaseTypeOfTypeOrThrow(((PENamedTypeSymbol)localTC3).Handle)]);
            Assert.Equal(base4, module0.TypeRefHandleToTypeMap[(TypeReferenceHandle)module0.Module.GetBaseTypeOfTypeOrThrow(((PENamedTypeSymbol)localTC4).Handle)]);
            Assert.Equal(base5, module0.TypeRefHandleToTypeMap[(TypeReferenceHandle)module0.Module.GetBaseTypeOfTypeOrThrow(((PENamedTypeSymbol)localTC5).Handle)]);
            Assert.Equal(base6, module0.TypeRefHandleToTypeMap[(TypeReferenceHandle)module0.Module.GetBaseTypeOfTypeOrThrow(((PENamedTypeSymbol)localTC6).Handle)]);
            Assert.Equal(base7, module0.TypeRefHandleToTypeMap[(TypeReferenceHandle)module0.Module.GetBaseTypeOfTypeOrThrow(((PENamedTypeSymbol)localTC7).Handle)]);
            Assert.Equal(base8, module0.TypeRefHandleToTypeMap[(TypeReferenceHandle)module0.Module.GetBaseTypeOfTypeOrThrow(((PENamedTypeSymbol)localTC8).Handle)]);

            var assembly1 = (MetadataOrSourceAssemblySymbol)assemblies[1];

            Assert.Equal(base1, assembly1.CachedTypeByEmittedName(base1.ToTestDisplayString()));
            Assert.Equal(base2, assembly1.CachedTypeByEmittedName(base2.ToTestDisplayString()));
            Assert.Equal(base3, assembly1.CachedTypeByEmittedName(base3.ToTestDisplayString()));
            Assert.Equal(base4, assembly1.CachedTypeByEmittedName(base4.ToTestDisplayString()));
            Assert.Equal(base5, assembly1.CachedTypeByEmittedName(base5.ToTestDisplayString()));
            Assert.Equal(base6, assembly1.CachedTypeByEmittedName(base6.ToTestDisplayString()));

            Assert.Equal(base7.ContainingType, assembly1.CachedTypeByEmittedName(base7.ContainingType.ToTestDisplayString()));

            Assert.Equal(7, assembly1.EmittedNameToTypeMapCount);
        }

        [Fact]
        public void Test3()
        {
            var mscorlibRef = TestReferences.NetFx.v4_0_21006.mscorlib;

            var c1 = CSharpCompilation.Create("Test", references: new MetadataReference[] { mscorlibRef });

            Assert.Equal("System.Object", ((SourceModuleSymbol)c1.Assembly.Modules[0]).GetCorLibType(SpecialType.System_Object).ToTestDisplayString());

            var localMTTestLib1Ref = TestReferences.SymbolsTests.V1.MTTestLib1.dll;

            var c2 = CSharpCompilation.Create("Test2", references: new MetadataReference[] { localMTTestLib1Ref });
            Assert.Equal("System.Object[missing]", ((SourceModuleSymbol)c2.Assembly.Modules[0]).GetCorLibType(SpecialType.System_Object).ToTestDisplayString());
        }

        [Fact]
        public void CrossModuleReferences1()
        {
            var compilationDef1 = @"
class Test1 : M3
{
}

class Test2 : M4
{
}
";
            var crossRefModule1 = TestReferences.SymbolsTests.netModule.CrossRefModule1;
            var crossRefModule2 = TestReferences.SymbolsTests.netModule.CrossRefModule2;
            var crossRefLib = TestReferences.SymbolsTests.netModule.CrossRefLib;

            var compilation1 = CreateCompilation(compilationDef1, new MetadataReference[] { crossRefLib }, TestOptions.ReleaseDll);

            compilation1.VerifyDiagnostics();

            var test1 = compilation1.GetTypeByMetadataName("Test1");
            var test2 = compilation1.GetTypeByMetadataName("Test2");

            Assert.False(test1.BaseType().IsErrorType());
            Assert.False(test1.BaseType().BaseType().IsErrorType());
            Assert.False(test2.BaseType().IsErrorType());
            Assert.False(test2.BaseType().BaseType().IsErrorType());
            Assert.False(test2.BaseType().BaseType().BaseType().IsErrorType());

            var compilationDef2 = @"
public class M3 : M1
{}

public class M4 : M2
{}
";
            var compilation2 = CreateCompilation(compilationDef2, new MetadataReference[] { crossRefModule1, crossRefModule2 }, TestOptions.ReleaseDll);

            compilation2.VerifyDiagnostics();

            var m3 = compilation2.GetTypeByMetadataName("M3");
            var m4 = compilation2.GetTypeByMetadataName("M4");

            Assert.False(m3.BaseType().IsErrorType());
            Assert.False(m3.BaseType().BaseType().IsErrorType());
            Assert.False(m4.BaseType().IsErrorType());
            Assert.False(m4.BaseType().BaseType().IsErrorType());

            var compilation3 = CreateCompilation(compilationDef2, new MetadataReference[] { crossRefModule2 }, TestOptions.ReleaseDll);

            m3 = compilation3.GetTypeByMetadataName("M3");
            m4 = compilation3.GetTypeByMetadataName("M4");

            Assert.True(m3.BaseType().IsErrorType());
            Assert.False(m4.BaseType().IsErrorType());
            Assert.True(m4.BaseType().BaseType().IsErrorType());

            // Expected:
            //error CS0246: The type or namespace name 'M1' could not be found (are you missing a using directive or an
            //        assembly reference?)
            //CrossRefModule2.netmodule: error CS0011: The base class or interface 'M1' in assembly 'CrossRefModule1.netmodule'
            //        referenced by type 'M2' could not be resolved

            DiagnosticDescription[] errors = {
                // (2,19): error CS0246: The type or namespace name 'M1' could not be found (are you missing a using directive or an assembly reference?)
                // public class M3 : M1
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "M1").WithArguments("M1"),
                // (5,19): error CS7079: The type 'M1' is defined in a module that has not been added. You must add the module 'CrossRefModule1.netmodule'.
                // public class M4 : M2
                Diagnostic(ErrorCode.ERR_NoTypeDefFromModule, "M2").WithArguments("M1", "CrossRefModule1.netmodule"),
                // error CS8014: Reference to 'CrossRefModule1.netmodule' netmodule missing.
                Diagnostic(ErrorCode.ERR_MissingNetModuleReference).WithArguments("CrossRefModule1.netmodule")
                                             };

            compilation3.VerifyDiagnostics(errors);
        }
    }
}
