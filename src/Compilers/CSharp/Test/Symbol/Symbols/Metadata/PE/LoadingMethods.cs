// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Reflection;

//test

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class LoadingMethods : CSharpTestBase
    {
        [Fact]
        public void Test1()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(mrefs: new[]
            {
                TestReferences.SymbolsTests.MDTestLib1,
                TestReferences.SymbolsTests.MDTestLib2,
                TestReferences.SymbolsTests.Methods.CSMethods,
                TestReferences.SymbolsTests.Methods.VBMethods,
                TestReferences.NetFx.v4_0_21006.mscorlib,
                TestReferences.SymbolsTests.Methods.ByRefReturn
            },
            options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal));

            var module1 = assemblies[0].Modules[0];
            var module2 = assemblies[1].Modules[0];
            var module3 = assemblies[2].Modules[0];
            var module4 = assemblies[3].Modules[0];
            var module5 = assemblies[4].Modules[0];
            var byrefReturn = assemblies[5].Modules[0];

            var varTC10 = module2.GlobalNamespace.GetTypeMembers("TC10").Single();

            Assert.Equal(6, varTC10.GetMembers().Length);

            var localM1 = (MethodSymbol)varTC10.GetMembers("M1").Single();
            var localM2 = (MethodSymbol)varTC10.GetMembers("M2").Single();
            var localM3 = (MethodSymbol)varTC10.GetMembers("M3").Single();
            var localM4 = (MethodSymbol)varTC10.GetMembers("M4").Single();
            var localM5 = (MethodSymbol)varTC10.GetMembers("M5").Single();

            Assert.Equal("void TC10.M1()", localM1.ToTestDisplayString());
            Assert.True(localM1.ReturnsVoid);
            Assert.Equal(Accessibility.Public, localM1.DeclaredAccessibility);
            Assert.Same(module2, localM1.Locations.Single().MetadataModule);

            Assert.Equal("void TC10.M2(System.Int32 m1_1)", localM2.ToTestDisplayString());
            Assert.True(localM2.ReturnsVoid);
            Assert.Equal(Accessibility.Protected, localM2.DeclaredAccessibility);

            var localM1_1 = localM2.Parameters[0];

            Assert.IsType<PEParameterSymbol>(localM1_1);
            Assert.Same(localM1_1.ContainingSymbol, localM2);
            Assert.Equal(SymbolKind.Parameter, localM1_1.Kind);
            Assert.Equal(Accessibility.NotApplicable, localM1_1.DeclaredAccessibility);
            Assert.False(localM1_1.IsAbstract);
            Assert.False(localM1_1.IsSealed);
            Assert.False(localM1_1.IsVirtual);
            Assert.False(localM1_1.IsOverride);
            Assert.False(localM1_1.IsStatic);
            Assert.False(localM1_1.IsExtern);
            Assert.Equal(0, localM1_1.CustomModifiers.Length);

            Assert.Equal("TC8 TC10.M3()", localM3.ToTestDisplayString());
            Assert.False(localM3.ReturnsVoid);
            Assert.Equal(Accessibility.Protected, localM3.DeclaredAccessibility);

            Assert.Equal("C1<System.Type> TC10.M4(ref C1<System.Type> x, ref TC8 y)", localM4.ToTestDisplayString());
            Assert.False(localM4.ReturnsVoid);
            Assert.Equal(Accessibility.Internal, localM4.DeclaredAccessibility);

            Assert.Equal("void TC10.M5(ref C1<System.Type>[,,] x, ref TC8[] y)", localM5.ToTestDisplayString());
            Assert.True(localM5.ReturnsVoid);
            Assert.Equal(Accessibility.ProtectedOrInternal, localM5.DeclaredAccessibility);

            var localM6 = varTC10.GetMembers("M6");
            Assert.Equal(0, localM6.Length);

            var localC107 = module1.GlobalNamespace.GetTypeMembers("C107").Single();
            var varC108 = localC107.GetMembers("C108").Single();
            Assert.Equal(SymbolKind.NamedType, varC108.Kind);

            var csharpC1 = module3.GlobalNamespace.GetTypeMembers("C1").Single();
            var sameName1 = csharpC1.GetMembers("SameName").Single();
            var sameName2 = csharpC1.GetMembers("sameName").Single();
            Assert.Equal(SymbolKind.NamedType, sameName1.Kind);
            Assert.Equal("SameName", sameName1.Name);
            Assert.Equal(SymbolKind.Method, sameName2.Kind);
            Assert.Equal("sameName", sameName2.Name);

            Assert.Equal(2, csharpC1.GetMembers("SameName2").Length);
            Assert.Equal(1, csharpC1.GetMembers("sameName2").Length);

            Assert.Equal(0, csharpC1.GetMembers("DoesntExist").Length);

            var basicC1 = module4.GlobalNamespace.GetTypeMembers("C1").Single();

            var basicC1_M1 = (MethodSymbol)basicC1.GetMembers("M1").Single();
            var basicC1_M2 = (MethodSymbol)basicC1.GetMembers("M2").Single();
            var basicC1_M3 = (MethodSymbol)basicC1.GetMembers("M3").Single();
            var basicC1_M4 = (MethodSymbol)basicC1.GetMembers("M4").Single();

            Assert.False(basicC1_M1.Parameters[0].IsOptional);
            Assert.False(basicC1_M1.Parameters[0].HasExplicitDefaultValue);
            Assert.Same(module4, basicC1_M1.Parameters[0].Locations.Single().MetadataModule);

            Assert.True(basicC1_M2.Parameters[0].IsOptional);
            Assert.False(basicC1_M2.Parameters[0].HasExplicitDefaultValue);

            Assert.True(basicC1_M3.Parameters[0].IsOptional);
            Assert.True(basicC1_M3.Parameters[0].HasExplicitDefaultValue);

            Assert.True(basicC1_M4.Parameters[0].IsOptional);
            Assert.False(basicC1_M4.Parameters[0].HasExplicitDefaultValue);

            var emptyStructure = module4.GlobalNamespace.GetTypeMembers("EmptyStructure").Single();
            Assert.Equal(1, emptyStructure.GetMembers().Length); //synthesized parameterless constructor
            Assert.Equal(0, emptyStructure.GetMembers("NoMembersOrTypes").Length);

            var basicC1_M5 = (MethodSymbol)basicC1.GetMembers("M5").Single();
            var basicC1_M6 = (MethodSymbol)basicC1.GetMembers("M6").Single();
            var basicC1_M7 = (MethodSymbol)basicC1.GetMembers("M7").Single();
            var basicC1_M8 = (MethodSymbol)basicC1.GetMembers("M8").Single();
            var basicC1_M9 = basicC1.GetMembers("M9").OfType<MethodSymbol>().ToArray();

            Assert.False(basicC1_M5.IsGenericMethod); // Check genericity before cracking signature
            Assert.True(basicC1_M6.ReturnsVoid);
            Assert.False(basicC1_M6.IsGenericMethod); // Check genericity after cracking signature

            Assert.True(basicC1_M7.IsGenericMethod); // Check genericity before cracking signature
            Assert.Equal("void C1.M7<T>(System.Int32 x)", basicC1_M7.ToTestDisplayString());
            Assert.True(basicC1_M6.ReturnsVoid);
            Assert.True(basicC1_M8.IsGenericMethod); // Check genericity after cracking signature
            Assert.Equal("void C1.M8<T>(System.Int32 x)", basicC1_M8.ToTestDisplayString());

            Assert.Equal(2, basicC1_M9.Count());
            Assert.Equal(1, basicC1_M9.Count(m => m.IsGenericMethod));
            Assert.Equal(1, basicC1_M9.Count(m => !m.IsGenericMethod));

            var basicC1_M10 = (MethodSymbol)basicC1.GetMembers("M10").Single();
            Assert.Equal("void C1.M10<T1>(T1 x)", basicC1_M10.ToTestDisplayString());

            var basicC1_M11 = (MethodSymbol)basicC1.GetMembers("M11").Single();
            Assert.Equal("T3 C1.M11<T2, T3>(T2 x)", basicC1_M11.ToTestDisplayString());
            Assert.Equal(0, basicC1_M11.TypeParameters[0].ConstraintTypes.Length);
            Assert.Same(basicC1, basicC1_M11.TypeParameters[1].ConstraintTypes.Single());

            var basicC1_M12 = (MethodSymbol)basicC1.GetMembers("M12").Single();
            Assert.Equal(0, basicC1_M12.TypeArguments.Length);
            Assert.False(basicC1_M12.IsVararg);
            Assert.False(basicC1_M12.IsExtern);
            Assert.False(basicC1_M12.IsStatic);

            var loadLibrary = (MethodSymbol)basicC1.GetMembers("LoadLibrary").Single();
            Assert.True(loadLibrary.IsExtern);

            var basicC2 = module4.GlobalNamespace.GetTypeMembers("C2").Single();

            var basicC2_M1 = (MethodSymbol)basicC2.GetMembers("M1").Single();
            Assert.Equal("void C2<T4>.M1<T5>(T5 x, T4 y)", basicC2_M1.ToTestDisplayString());

            var console = module5.GlobalNamespace.GetMembers("System").OfType<NamespaceSymbol>().Single().
                GetTypeMembers("Console").Single();

            Assert.Equal(1, console.GetMembers("WriteLine").OfType<MethodSymbol>().Count(m => m.IsVararg));
            Assert.True(console.GetMembers("WriteLine").OfType<MethodSymbol>().Single(m => m.IsVararg).IsStatic);

            var basicModifiers1 = module4.GlobalNamespace.GetTypeMembers("Modifiers1").Single();

            var basicModifiers1_M1 = (MethodSymbol)basicModifiers1.GetMembers("M1").Single();
            var basicModifiers1_M2 = (MethodSymbol)basicModifiers1.GetMembers("M2").Single();
            var basicModifiers1_M3 = (MethodSymbol)basicModifiers1.GetMembers("M3").Single();
            var basicModifiers1_M4 = (MethodSymbol)basicModifiers1.GetMembers("M4").Single();
            var basicModifiers1_M5 = (MethodSymbol)basicModifiers1.GetMembers("M5").Single();
            var basicModifiers1_M6 = (MethodSymbol)basicModifiers1.GetMembers("M6").Single();
            var basicModifiers1_M7 = (MethodSymbol)basicModifiers1.GetMembers("M7").Single();
            var basicModifiers1_M8 = (MethodSymbol)basicModifiers1.GetMembers("M8").Single();
            var basicModifiers1_M9 = (MethodSymbol)basicModifiers1.GetMembers("M9").Single();

            Assert.True(basicModifiers1_M1.IsAbstract);
            Assert.False(basicModifiers1_M1.IsVirtual);
            Assert.False(basicModifiers1_M1.IsSealed);
            Assert.True(basicModifiers1_M1.HidesBaseMethodsByName);
            Assert.False(basicModifiers1_M1.IsOverride);

            Assert.False(basicModifiers1_M2.IsAbstract);
            Assert.True(basicModifiers1_M2.IsVirtual);
            Assert.False(basicModifiers1_M2.IsSealed);
            Assert.True(basicModifiers1_M2.HidesBaseMethodsByName);
            Assert.False(basicModifiers1_M2.IsOverride);

            Assert.False(basicModifiers1_M3.IsAbstract);
            Assert.False(basicModifiers1_M3.IsVirtual);
            Assert.False(basicModifiers1_M3.IsSealed);
            Assert.False(basicModifiers1_M3.HidesBaseMethodsByName);
            Assert.False(basicModifiers1_M3.IsOverride);

            Assert.False(basicModifiers1_M4.IsAbstract);
            Assert.False(basicModifiers1_M4.IsVirtual);
            Assert.False(basicModifiers1_M4.IsSealed);
            Assert.True(basicModifiers1_M4.HidesBaseMethodsByName);
            Assert.False(basicModifiers1_M4.IsOverride);

            Assert.False(basicModifiers1_M5.IsAbstract);
            Assert.False(basicModifiers1_M5.IsVirtual);
            Assert.False(basicModifiers1_M5.IsSealed);
            Assert.True(basicModifiers1_M5.HidesBaseMethodsByName);
            Assert.False(basicModifiers1_M5.IsOverride);

            Assert.True(basicModifiers1_M6.IsAbstract);
            Assert.False(basicModifiers1_M6.IsVirtual);
            Assert.False(basicModifiers1_M6.IsSealed);
            Assert.False(basicModifiers1_M6.HidesBaseMethodsByName);
            Assert.False(basicModifiers1_M6.IsOverride);

            Assert.False(basicModifiers1_M7.IsAbstract);
            Assert.True(basicModifiers1_M7.IsVirtual);
            Assert.False(basicModifiers1_M7.IsSealed);
            Assert.False(basicModifiers1_M7.HidesBaseMethodsByName);
            Assert.False(basicModifiers1_M7.IsOverride);

            Assert.True(basicModifiers1_M8.IsAbstract);
            Assert.False(basicModifiers1_M8.IsVirtual);
            Assert.False(basicModifiers1_M8.IsSealed);
            Assert.True(basicModifiers1_M8.HidesBaseMethodsByName);
            Assert.False(basicModifiers1_M8.IsOverride);

            Assert.False(basicModifiers1_M9.IsAbstract);
            Assert.True(basicModifiers1_M9.IsVirtual);
            Assert.False(basicModifiers1_M9.IsSealed);
            Assert.True(basicModifiers1_M9.HidesBaseMethodsByName);
            Assert.False(basicModifiers1_M9.IsOverride);

            var basicModifiers2 = module4.GlobalNamespace.GetTypeMembers("Modifiers2").Single();

            var basicModifiers2_M1 = (MethodSymbol)basicModifiers2.GetMembers("M1").Single();
            var basicModifiers2_M2 = (MethodSymbol)basicModifiers2.GetMembers("M2").Single();
            var basicModifiers2_M6 = (MethodSymbol)basicModifiers2.GetMembers("M6").Single();
            var basicModifiers2_M7 = (MethodSymbol)basicModifiers2.GetMembers("M7").Single();

            Assert.True(basicModifiers2_M1.IsAbstract);
            Assert.False(basicModifiers2_M1.IsVirtual);
            Assert.False(basicModifiers2_M1.IsSealed);
            Assert.True(basicModifiers2_M1.HidesBaseMethodsByName);
            Assert.True(basicModifiers2_M1.IsOverride);

            Assert.False(basicModifiers2_M2.IsAbstract);
            Assert.False(basicModifiers2_M2.IsVirtual);
            Assert.True(basicModifiers2_M2.IsSealed);
            Assert.True(basicModifiers2_M2.HidesBaseMethodsByName);
            Assert.True(basicModifiers2_M2.IsOverride);

            Assert.True(basicModifiers2_M6.IsAbstract);
            Assert.False(basicModifiers2_M6.IsVirtual);
            Assert.False(basicModifiers2_M6.IsSealed);
            Assert.False(basicModifiers2_M6.HidesBaseMethodsByName);
            Assert.True(basicModifiers2_M6.IsOverride);

            Assert.False(basicModifiers2_M7.IsAbstract);
            Assert.False(basicModifiers2_M7.IsVirtual);
            Assert.True(basicModifiers2_M7.IsSealed);
            Assert.False(basicModifiers2_M7.HidesBaseMethodsByName);
            Assert.True(basicModifiers2_M7.IsOverride);

            var basicModifiers3 = module4.GlobalNamespace.GetTypeMembers("Modifiers3").Single();

            var basicModifiers3_M1 = (MethodSymbol)basicModifiers3.GetMembers("M1").Single();
            var basicModifiers3_M6 = (MethodSymbol)basicModifiers3.GetMembers("M6").Single();

            Assert.False(basicModifiers3_M1.IsAbstract);
            Assert.False(basicModifiers3_M1.IsVirtual);
            Assert.False(basicModifiers3_M1.IsSealed);
            Assert.True(basicModifiers3_M1.HidesBaseMethodsByName);
            Assert.True(basicModifiers3_M1.IsOverride);

            Assert.False(basicModifiers3_M6.IsAbstract);
            Assert.False(basicModifiers3_M6.IsVirtual);
            Assert.False(basicModifiers3_M6.IsSealed);
            Assert.False(basicModifiers3_M6.HidesBaseMethodsByName);
            Assert.True(basicModifiers3_M6.IsOverride);

            var csharpModifiers1 = module3.GlobalNamespace.GetTypeMembers("Modifiers1").Single();

            var csharpModifiers1_M1 = (MethodSymbol)csharpModifiers1.GetMembers("M1").Single();
            var csharpModifiers1_M2 = (MethodSymbol)csharpModifiers1.GetMembers("M2").Single();
            var csharpModifiers1_M3 = (MethodSymbol)csharpModifiers1.GetMembers("M3").Single();
            var csharpModifiers1_M4 = (MethodSymbol)csharpModifiers1.GetMembers("M4").Single();

            Assert.True(csharpModifiers1_M1.IsAbstract);
            Assert.False(csharpModifiers1_M1.IsVirtual);
            Assert.False(csharpModifiers1_M1.IsSealed);
            Assert.False(csharpModifiers1_M1.HidesBaseMethodsByName);
            Assert.False(csharpModifiers1_M1.IsOverride);

            Assert.False(csharpModifiers1_M2.IsAbstract);
            Assert.True(csharpModifiers1_M2.IsVirtual);
            Assert.False(csharpModifiers1_M2.IsSealed);
            Assert.False(csharpModifiers1_M2.HidesBaseMethodsByName);
            Assert.False(csharpModifiers1_M2.IsOverride);

            Assert.False(csharpModifiers1_M3.IsAbstract);
            Assert.False(csharpModifiers1_M3.IsVirtual);
            Assert.False(csharpModifiers1_M3.IsSealed);
            Assert.False(csharpModifiers1_M3.HidesBaseMethodsByName);
            Assert.False(csharpModifiers1_M3.IsOverride);

            Assert.False(csharpModifiers1_M4.IsAbstract);
            Assert.True(csharpModifiers1_M4.IsVirtual);
            Assert.False(csharpModifiers1_M4.IsSealed);
            Assert.False(csharpModifiers1_M4.HidesBaseMethodsByName);
            Assert.False(csharpModifiers1_M4.IsOverride);

            var csharpModifiers2 = module3.GlobalNamespace.GetTypeMembers("Modifiers2").Single();

            var csharpModifiers2_M1 = (MethodSymbol)csharpModifiers2.GetMembers("M1").Single();
            var csharpModifiers2_M2 = (MethodSymbol)csharpModifiers2.GetMembers("M2").Single();
            var csharpModifiers2_M3 = (MethodSymbol)csharpModifiers2.GetMembers("M3").Single();

            Assert.False(csharpModifiers2_M1.IsAbstract);
            Assert.False(csharpModifiers2_M1.IsVirtual);
            Assert.True(csharpModifiers2_M1.IsSealed);
            Assert.False(csharpModifiers2_M1.HidesBaseMethodsByName);
            Assert.True(csharpModifiers2_M1.IsOverride);

            Assert.True(csharpModifiers2_M2.IsAbstract);
            Assert.False(csharpModifiers2_M2.IsVirtual);
            Assert.False(csharpModifiers2_M2.IsSealed);
            Assert.False(csharpModifiers2_M2.HidesBaseMethodsByName);
            Assert.True(csharpModifiers2_M2.IsOverride);

            Assert.False(csharpModifiers2_M3.IsAbstract);
            Assert.True(csharpModifiers2_M3.IsVirtual);
            Assert.False(csharpModifiers2_M3.IsSealed);
            Assert.False(csharpModifiers2_M3.HidesBaseMethodsByName);
            Assert.False(csharpModifiers2_M3.IsOverride);

            var csharpModifiers3 = module3.GlobalNamespace.GetTypeMembers("Modifiers3").Single();

            var csharpModifiers3_M1 = (MethodSymbol)csharpModifiers3.GetMembers("M1").Single();
            var csharpModifiers3_M3 = (MethodSymbol)csharpModifiers3.GetMembers("M3").Single();
            var csharpModifiers3_M4 = (MethodSymbol)csharpModifiers3.GetMembers("M4").Single();

            Assert.False(csharpModifiers3_M1.IsAbstract);
            Assert.False(csharpModifiers3_M1.IsVirtual);
            Assert.False(csharpModifiers3_M1.IsSealed);
            Assert.False(csharpModifiers3_M1.HidesBaseMethodsByName);
            Assert.True(csharpModifiers3_M1.IsOverride);

            Assert.False(csharpModifiers3_M3.IsAbstract);
            Assert.False(csharpModifiers3_M3.IsVirtual);
            Assert.False(csharpModifiers3_M3.IsSealed);
            Assert.False(csharpModifiers3_M3.HidesBaseMethodsByName);
            Assert.False(csharpModifiers3_M3.IsOverride);

            Assert.True(csharpModifiers3_M4.IsAbstract);
            Assert.False(csharpModifiers3_M4.IsVirtual);
            Assert.False(csharpModifiers3_M4.IsSealed);
            Assert.False(csharpModifiers3_M4.HidesBaseMethodsByName);
            Assert.False(csharpModifiers3_M4.IsOverride);

            var byrefReturnMethod = byrefReturn.GlobalNamespace.GetTypeMembers("ByRefReturn").Single().GetMembers("M").OfType<MethodSymbol>().Single();
            Assert.Equal(TypeKind.Error, byrefReturnMethod.ReturnType.TypeKind);
            Assert.IsType<ByRefReturnErrorTypeSymbol>(byrefReturnMethod.ReturnType);
        }

        [Fact]
        public void TestExplicitImplementationSimple()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(
                TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.CSharp);

            var globalNamespace = assembly.GlobalNamespace;

            var @interface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Interface").Single();
            Assert.Equal(TypeKind.Interface, @interface.TypeKind);

            var interfaceMethod = (MethodSymbol)@interface.GetMembers("Method").Single();

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Class").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);
            Assert.True(@class.Interfaces.Contains(@interface));

            var classMethod = (MethodSymbol)@class.GetMembers("Interface.Method").Single();
            Assert.Equal(MethodKind.ExplicitInterfaceImplementation, classMethod.MethodKind);

            var explicitImpl = classMethod.ExplicitInterfaceImplementations.Single();
            Assert.Equal(interfaceMethod, explicitImpl);
        }

        [Fact]
        public void TestExplicitImplementationMultiple()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(
                TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.IL);

            var globalNamespace = assembly.GlobalNamespace;

            var interface1 = (NamedTypeSymbol)globalNamespace.GetTypeMembers("I1").Single();
            Assert.Equal(TypeKind.Interface, interface1.TypeKind);

            var interface1Method = (MethodSymbol)interface1.GetMembers("Method1").Single();

            var interface2 = (NamedTypeSymbol)globalNamespace.GetTypeMembers("I2").Single();
            Assert.Equal(TypeKind.Interface, interface2.TypeKind);

            var interface2Method = (MethodSymbol)interface2.GetMembers("Method2").Single();

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("C").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);
            Assert.True(@class.Interfaces.Contains(interface1));
            Assert.True(@class.Interfaces.Contains(interface2));

            var classMethod = (MethodSymbol)@class.GetMembers("Method").Single();   //  the method is considered to be Ordinary 
            Assert.Equal(MethodKind.Ordinary, classMethod.MethodKind);              //  because it has name without '.'

            var explicitImpls = classMethod.ExplicitInterfaceImplementations;
            Assert.Equal(2, explicitImpls.Length);
            Assert.Equal(interface1Method, explicitImpls[0]);
            Assert.Equal(interface2Method, explicitImpls[1]);
        }

        [Fact]
        public void TestExplicitImplementationGeneric()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                mrefs: new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.CSharp,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var @interface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("IGeneric").Single();
            Assert.Equal(TypeKind.Interface, @interface.TypeKind);

            var interfaceMethod = (MethodSymbol)@interface.GetMembers("Method").Last(); //this assumes decl order
            Assert.Equal("void IGeneric<T>.Method<U>(T t, U u)", interfaceMethod.ToTestDisplayString()); //make sure we got the one we expected

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Generic").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);

            var substitutedInterface = @class.Interfaces.Single();
            Assert.Equal(@interface, substitutedInterface.ConstructedFrom);

            var substitutedInterfaceMethod = (MethodSymbol)substitutedInterface.GetMembers("Method").Last(); //this assumes decl order
            Assert.Equal("void IGeneric<S>.Method<U>(S t, U u)", substitutedInterfaceMethod.ToTestDisplayString()); //make sure we got the one we expected
            Assert.Equal(interfaceMethod, substitutedInterfaceMethod.OriginalDefinition);

            var classMethod = (MethodSymbol)@class.GetMembers("IGeneric<S>.Method").Last(); //this assumes decl order
            Assert.Equal("void Generic<S>.IGeneric<S>.Method<V>(S s, V v)", classMethod.ToTestDisplayString()); //make sure we got the one we expected
            Assert.Equal(MethodKind.ExplicitInterfaceImplementation, classMethod.MethodKind);

            var explicitImpl = classMethod.ExplicitInterfaceImplementations.Single();
            Assert.Equal(substitutedInterfaceMethod, explicitImpl);
        }

        [Fact]
        public void TestExplicitImplementationConstructed()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.CSharp,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var @interface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("IGeneric").Single();
            Assert.Equal(TypeKind.Interface, @interface.TypeKind);

            var interfaceMethod = (MethodSymbol)@interface.GetMembers("Method").Last(); //this assumes decl order
            Assert.Equal("void IGeneric<T>.Method<U>(T t, U u)", interfaceMethod.ToTestDisplayString()); //make sure we got the one we expected

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Constructed").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);

            var substitutedInterface = @class.Interfaces.Single();
            Assert.Equal(@interface, substitutedInterface.ConstructedFrom);

            var substitutedInterfaceMethod = (MethodSymbol)substitutedInterface.GetMembers("Method").Last(); //this assumes decl order
            Assert.Equal("void IGeneric<System.Int32>.Method<U>(System.Int32 t, U u)", substitutedInterfaceMethod.ToTestDisplayString()); //make sure we got the one we expected
            Assert.Equal(interfaceMethod, substitutedInterfaceMethod.OriginalDefinition);

            var classMethod = (MethodSymbol)@class.GetMembers("IGeneric<System.Int32>.Method").Last(); //this assumes decl order
            Assert.Equal("void Constructed.IGeneric<System.Int32>.Method<W>(System.Int32 i, W w)", classMethod.ToTestDisplayString()); //make sure we got the one we expected
            Assert.Equal(MethodKind.ExplicitInterfaceImplementation, classMethod.MethodKind);

            var explicitImpl = classMethod.ExplicitInterfaceImplementations.Single();
            Assert.Equal(substitutedInterfaceMethod, explicitImpl);
        }

        [Fact]
        public void TestExplicitImplementationInterfaceCycleSuccess()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(
                TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.IL);

            var globalNamespace = assembly.GlobalNamespace;

            var cyclicInterface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("ImplementsSelf").Single();
            Assert.Equal(TypeKind.Interface, cyclicInterface.TypeKind);

            var implementedInterface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("I1").Single();
            Assert.Equal(TypeKind.Interface, implementedInterface.TypeKind);

            var interface2Method = (MethodSymbol)implementedInterface.GetMembers("Method1").Single();

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("InterfaceCycleSuccess").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);
            Assert.True(@class.Interfaces.Contains(cyclicInterface));
            Assert.True(@class.Interfaces.Contains(implementedInterface));

            var classMethod = (MethodSymbol)@class.GetMembers("Method").Single();   //  the method is considered to be Ordinary 
            Assert.Equal(MethodKind.Ordinary, classMethod.MethodKind);              //  because it has name without '.'

            var explicitImpl = classMethod.ExplicitInterfaceImplementations.Single();
            Assert.Equal(interface2Method, explicitImpl);
        }

        [Fact]
        public void TestExplicitImplementationInterfaceCycleFailure()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(
                TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.IL);

            var globalNamespace = assembly.GlobalNamespace;

            var cyclicInterface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("ImplementsSelf").Single();
            Assert.Equal(TypeKind.Interface, cyclicInterface.TypeKind);

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("InterfaceCycleFailure").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);
            Assert.True(@class.Interfaces.Contains(cyclicInterface));

            var classMethod = (MethodSymbol)@class.GetMembers("Method").Single();
            //we couldn't find an interface method that's explicitly implemented, so we have no reason to believe the method isn't ordinary
            Assert.Equal(MethodKind.Ordinary, classMethod.MethodKind);

            var explicitImpls = classMethod.ExplicitInterfaceImplementations;
            Assert.False(explicitImpls.Any());
        }

        /// <summary>
        /// A type def explicitly implements an interface, also a type def, but only
        /// indirectly, via a type ref.
        /// </summary>
        [Fact]
        public void TestExplicitImplementationDefRefDef()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.CSharp,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var defInterface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Interface").Single();
            Assert.Equal(TypeKind.Interface, defInterface.TypeKind);

            var defInterfaceMethod = (MethodSymbol)defInterface.GetMembers("Method").Single();

            var refInterface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("IGenericInterface").Single();
            Assert.Equal(TypeKind.Interface, defInterface.TypeKind);
            Assert.True(refInterface.Interfaces.Contains(defInterface));

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("IndirectImplementation").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);

            var classInterfacesConstructedFrom = @class.Interfaces.Select(i => i.ConstructedFrom);
            Assert.Equal(2, classInterfacesConstructedFrom.Count());
            Assert.Contains(defInterface, classInterfacesConstructedFrom);
            Assert.Contains(refInterface, classInterfacesConstructedFrom);

            var classMethod = (MethodSymbol)@class.GetMembers("Interface.Method").Single();
            Assert.Equal(MethodKind.ExplicitInterfaceImplementation, classMethod.MethodKind);

            var explicitImpl = classMethod.ExplicitInterfaceImplementations.Single();
            Assert.Equal(defInterfaceMethod, explicitImpl);
        }

        /// <summary>
        /// IL type explicitly overrides a class (vs interface) method.  
        /// ExplicitInterfaceImplementations should be empty.
        /// </summary>
        [Fact]
        public void TestExplicitImplementationOfClassMethod()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(
                TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.IL);

            var globalNamespace = assembly.GlobalNamespace;

            var baseClass = (NamedTypeSymbol)globalNamespace.GetTypeMembers("ExplicitlyImplementedClass").Single();
            Assert.Equal(TypeKind.Class, baseClass.TypeKind);

            var derivedClass = (NamedTypeSymbol)globalNamespace.GetTypeMembers("ExplicitlyImplementsAClass").Single();
            Assert.Equal(TypeKind.Class, derivedClass.TypeKind);
            Assert.Equal(baseClass, derivedClass.BaseType);

            var derivedClassMethod = (MethodSymbol)derivedClass.GetMembers("Method").Single();
            Assert.Equal(MethodKind.Ordinary, derivedClassMethod.MethodKind);
            Assert.Equal(0, derivedClassMethod.ExplicitInterfaceImplementations.Length);
        }

        /// <summary>
        /// IL type explicitly overrides an interface method on an unrelated interface.
        /// ExplicitInterfaceImplementations should be empty.
        /// </summary>
        [Fact]
        public void TestExplicitImplementationOfUnrelatedInterfaceMethod()
        {
            var assembly = MetadataTestHelpers.GetSymbolForReference(
                TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.IL);

            var globalNamespace = assembly.GlobalNamespace;

            var @interface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("IUnrelated").First(); //decl order
            Assert.Equal(0, @interface.Arity);
            Assert.Equal(TypeKind.Interface, @interface.TypeKind);

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("ExplicitlyImplementsUnrelatedInterfaceMethods").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);
            Assert.Equal(0, @class.AllInterfaces.Length);

            var classMethod = (MethodSymbol)@class.GetMembers("Method1").Single();
            Assert.Equal(MethodKind.Ordinary, classMethod.MethodKind);
            Assert.Equal(0, classMethod.ExplicitInterfaceImplementations.Length);

            var classGenericMethod = (MethodSymbol)@class.GetMembers("Method1").Single();
            Assert.Equal(MethodKind.Ordinary, classGenericMethod.MethodKind);
            Assert.Equal(0, classGenericMethod.ExplicitInterfaceImplementations.Length);
        }

        /// <summary>
        /// IL type explicitly overrides an interface method on an unrelated generic interface.
        /// ExplicitInterfaceImplementations should be empty.
        /// </summary>
        [Fact]
        public void TestExplicitImplementationOfUnrelatedGenericInterfaceMethod()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.IL,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var @interface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("IUnrelated").Last(); //decl order
            Assert.Equal(1, @interface.Arity);
            Assert.Equal(TypeKind.Interface, @interface.TypeKind);

            var @class = (NamedTypeSymbol)globalNamespace.GetTypeMembers("ExplicitlyImplementsUnrelatedInterfaceMethods").Single();
            Assert.Equal(TypeKind.Class, @class.TypeKind);
            Assert.Equal(0, @class.AllInterfaces.Length);

            var classMethod = (MethodSymbol)@class.GetMembers("Method2").Single();
            Assert.Equal(MethodKind.Ordinary, classMethod.MethodKind);
            Assert.Equal(0, classMethod.ExplicitInterfaceImplementations.Length);

            var classGenericMethod = (MethodSymbol)@class.GetMembers("Method2").Single();
            Assert.Equal(MethodKind.Ordinary, classGenericMethod.MethodKind);
            Assert.Equal(0, classGenericMethod.ExplicitInterfaceImplementations.Length);
        }

        /// <summary>
        /// In metadata, nested types implicitly share all type parameters of their containing types.
        /// This results in some extra computations when mapping a type parameter position to a type
        /// parameter symbol.
        /// </summary>
        [Fact]
        public void TestTypeParameterPositions()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                new[]
                {
                    TestReferences.NetFx.v4_0_30319.mscorlib,
                    TestReferences.SymbolsTests.ExplicitInterfaceImplementation.Methods.CSharp,
                });

            var globalNamespace = assemblies.ElementAt(1).GlobalNamespace;

            var outerInterface = (NamedTypeSymbol)globalNamespace.GetTypeMembers("IGeneric2").Single();
            Assert.Equal(1, outerInterface.Arity);
            Assert.Equal(TypeKind.Interface, outerInterface.TypeKind);

            var outerInterfaceMethod = outerInterface.GetMembers().Single();

            var outerClass = (NamedTypeSymbol)globalNamespace.GetTypeMembers("Outer").Single();
            Assert.Equal(1, outerClass.Arity);
            Assert.Equal(TypeKind.Class, outerClass.TypeKind);

            var innerInterface = (NamedTypeSymbol)outerClass.GetTypeMembers("IInner").Single();
            Assert.Equal(1, innerInterface.Arity);
            Assert.Equal(TypeKind.Interface, innerInterface.TypeKind);

            var innerInterfaceMethod = innerInterface.GetMembers().Single();

            var innerClass1 = (NamedTypeSymbol)outerClass.GetTypeMembers("Inner1").Single();
            CheckInnerClassHelper(innerClass1, "IGeneric2<A>.Method", outerInterfaceMethod);

            var innerClass2 = (NamedTypeSymbol)outerClass.GetTypeMembers("Inner2").Single();
            CheckInnerClassHelper(innerClass2, "IGeneric2<T>.Method", outerInterfaceMethod);

            var innerClass3 = (NamedTypeSymbol)outerClass.GetTypeMembers("Inner3").Single();
            CheckInnerClassHelper(innerClass3, "Outer<T>.IInner<C>.Method", innerInterfaceMethod);

            var innerClass4 = (NamedTypeSymbol)outerClass.GetTypeMembers("Inner4").Single();
            CheckInnerClassHelper(innerClass4, "Outer<T>.IInner<T>.Method", innerInterfaceMethod);
        }

        private static void CheckInnerClassHelper(NamedTypeSymbol innerClass, string methodName, Symbol interfaceMethod)
        {
            var @interface = interfaceMethod.ContainingType;

            Assert.Equal(1, innerClass.Arity);
            Assert.Equal(TypeKind.Class, innerClass.TypeKind);
            Assert.Equal(@interface, innerClass.Interfaces.Single().ConstructedFrom);

            var innerClassMethod = (MethodSymbol)innerClass.GetMembers(methodName).Single();
            var innerClassImplementingMethod = innerClassMethod.ExplicitInterfaceImplementations.Single();
            Assert.Equal(interfaceMethod, innerClassImplementingMethod.OriginalDefinition);
            Assert.Equal(@interface, innerClassImplementingMethod.ContainingType.ConstructedFrom);
        }

        [Fact]
        public void TestVirtualnessFlags_Invoke()
        {
            var source = @"
class Invoke
{
    void Foo(MetadataModifiers m)
    {
        m.M00();
        m.M01();
        m.M02();
        m.M03();
        m.M04();
        m.M05();
        m.M06();
        m.M07();
        m.M08();
        m.M09();
        m.M10();
        m.M11();
        m.M12();
        m.M13();
        m.M14();
        m.M15();
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source, new[] { TestReferences.SymbolsTests.Methods.ILMethods });
            compilation.VerifyDiagnostics(); // No errors, as in Dev10
        }

        [Fact]
        public void TestVirtualnessFlags_NoOverride()
        {
            var source = @"
class Abstract : MetadataModifiers
{
    //CS0534 for methods 2, 5, 8, 9, 11, 12, 14, 15
}
";
            var compilation = CreateCompilationWithMscorlib(source, new[] { TestReferences.SymbolsTests.Methods.ILMethods });
            compilation.VerifyDiagnostics(
                // (2,7): error CS0534: 'Abstract' does not implement inherited abstract member 'MetadataModifiers.M02()'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Abstract").WithArguments("Abstract", "MetadataModifiers.M02()"),
                // (2,7): error CS0534: 'Abstract' does not implement inherited abstract member 'MetadataModifiers.M05()'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Abstract").WithArguments("Abstract", "MetadataModifiers.M05()"),
                // (2,7): error CS0534: 'Abstract' does not implement inherited abstract member 'MetadataModifiers.M08()'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Abstract").WithArguments("Abstract", "MetadataModifiers.M08()"),
                // (2,7): error CS0534: 'Abstract' does not implement inherited abstract member 'MetadataModifiers.M09()'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Abstract").WithArguments("Abstract", "MetadataModifiers.M09()"),
                // (2,7): error CS0534: 'Abstract' does not implement inherited abstract member 'MetadataModifiers.M11()'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Abstract").WithArguments("Abstract", "MetadataModifiers.M11()"),
                // (2,7): error CS0534: 'Abstract' does not implement inherited abstract member 'MetadataModifiers.M12()'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Abstract").WithArguments("Abstract", "MetadataModifiers.M12()"),
                // (2,7): error CS0534: 'Abstract' does not implement inherited abstract member 'MetadataModifiers.M14()'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Abstract").WithArguments("Abstract", "MetadataModifiers.M14()"),
                // (2,7): error CS0534: 'Abstract' does not implement inherited abstract member 'MetadataModifiers.M15()'
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Abstract").WithArguments("Abstract", "MetadataModifiers.M15()"));
        }

        [Fact]
        public void TestVirtualnessFlags_Override()
        {
            var source = @"
class Override : MetadataModifiers
{
    public override void M00() { } //CS0506
    public override void M01() { } //CS0506
    public override void M02() { }
    public override void M03() { }
    public override void M04() { } //CS0506
    public override void M05() { }
    public override void M06() { }
    public override void M07() { } //CS0506
    public override void M08() { }
    public override void M09() { }
    public override void M10() { } //CS0239 (Dev10 reports CS0506, presumably because MetadataModifiers.M10 isn't overriding anything)
    public override void M11() { }
    public override void M12() { }
    public override void M13() { } //CS0506
    public override void M14() { }
    public override void M15() { }
}
";
            var compilation = CreateCompilationWithMscorlib(source, new[] { TestReferences.SymbolsTests.Methods.ILMethods });
            compilation.VerifyDiagnostics(
                // (4,26): error CS0506: 'Override.M00()': cannot override inherited member 'MetadataModifiers.M00()' because it is not marked virtual, abstract, or override
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "M00").WithArguments("Override.M00()", "MetadataModifiers.M00()"),
                // (5,26): error CS0506: 'Override.M01()': cannot override inherited member 'MetadataModifiers.M01()' because it is not marked virtual, abstract, or override
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "M01").WithArguments("Override.M01()", "MetadataModifiers.M01()"),
                // (8,26): error CS0506: 'Override.M04()': cannot override inherited member 'MetadataModifiers.M04()' because it is not marked virtual, abstract, or override
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "M04").WithArguments("Override.M04()", "MetadataModifiers.M04()"),
                // (11,26): error CS0506: 'Override.M07()': cannot override inherited member 'MetadataModifiers.M07()' because it is not marked virtual, abstract, or override
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "M07").WithArguments("Override.M07()", "MetadataModifiers.M07()"),
                // (14,26): error CS0239: 'Override.M10()': cannot override inherited member 'MetadataModifiers.M10()' because it is sealed
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "M10").WithArguments("Override.M10()", "MetadataModifiers.M10()"),
                // (17,26): error CS0506: 'Override.M13()': cannot override inherited member 'MetadataModifiers.M13()' because it is not marked virtual, abstract, or override
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "M13").WithArguments("Override.M13()", "MetadataModifiers.M13()"));
        }

        [ClrOnlyFact]
        public void TestVirtualnessFlags_CSharpRepresentation()
        {
            // All combinations of VirtualContract, NewSlotVTable, AbstractImpl, and FinalContract - without explicit overriding
            // NOTE: some won't peverify (newslot/final/abstract without virtual, abstract with final)

            CheckLoadingVirtualnessFlags(SymbolVirtualness.NonVirtual, 0, isExplicitOverride: false);

            CheckLoadingVirtualnessFlags(SymbolVirtualness.Override, MethodAttributes.Virtual, isExplicitOverride: false);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.NonVirtual, MethodAttributes.NewSlot, isExplicitOverride: false);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.Abstract, MethodAttributes.Abstract, isExplicitOverride: false);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.NonVirtual, MethodAttributes.Final, isExplicitOverride: false);

            CheckLoadingVirtualnessFlags(SymbolVirtualness.Virtual, MethodAttributes.Virtual | MethodAttributes.NewSlot, isExplicitOverride: false);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.AbstractOverride, MethodAttributes.Virtual | MethodAttributes.Abstract, isExplicitOverride: false);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.SealedOverride, MethodAttributes.Virtual | MethodAttributes.Final, isExplicitOverride: false);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.Abstract, MethodAttributes.NewSlot | MethodAttributes.Abstract, isExplicitOverride: false);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.NonVirtual, MethodAttributes.NewSlot | MethodAttributes.Final, isExplicitOverride: false);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.Abstract, MethodAttributes.Abstract | MethodAttributes.Final, isExplicitOverride: false);

            CheckLoadingVirtualnessFlags(SymbolVirtualness.Abstract, MethodAttributes.NewSlot | MethodAttributes.Abstract | MethodAttributes.Final, isExplicitOverride: false);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.AbstractOverride, MethodAttributes.Virtual | MethodAttributes.Abstract | MethodAttributes.Final, isExplicitOverride: false);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.NonVirtual, MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final, isExplicitOverride: false);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.Abstract, MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Abstract, isExplicitOverride: false);

            CheckLoadingVirtualnessFlags(SymbolVirtualness.Abstract, MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Abstract | MethodAttributes.Final, isExplicitOverride: false);

            // All combinations of VirtualContract, NewSlotVTable, AbstractImpl, and FinalContract - with explicit overriding

            CheckLoadingVirtualnessFlags(SymbolVirtualness.NonVirtual, 0, isExplicitOverride: true);

            CheckLoadingVirtualnessFlags(SymbolVirtualness.Override, MethodAttributes.Virtual, isExplicitOverride: true);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.NonVirtual, MethodAttributes.NewSlot, isExplicitOverride: true);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.Abstract, MethodAttributes.Abstract, isExplicitOverride: true);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.NonVirtual, MethodAttributes.Final, isExplicitOverride: true);

            CheckLoadingVirtualnessFlags(SymbolVirtualness.Override, MethodAttributes.Virtual | MethodAttributes.NewSlot, isExplicitOverride: true); //differs from above
            CheckLoadingVirtualnessFlags(SymbolVirtualness.AbstractOverride, MethodAttributes.Virtual | MethodAttributes.Abstract, isExplicitOverride: true);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.SealedOverride, MethodAttributes.Virtual | MethodAttributes.Final, isExplicitOverride: true);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.Abstract, MethodAttributes.NewSlot | MethodAttributes.Abstract, isExplicitOverride: true);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.NonVirtual, MethodAttributes.NewSlot | MethodAttributes.Final, isExplicitOverride: true);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.Abstract, MethodAttributes.Abstract | MethodAttributes.Final, isExplicitOverride: true);

            CheckLoadingVirtualnessFlags(SymbolVirtualness.Abstract, MethodAttributes.NewSlot | MethodAttributes.Abstract | MethodAttributes.Final, isExplicitOverride: true);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.AbstractOverride, MethodAttributes.Virtual | MethodAttributes.Abstract | MethodAttributes.Final, isExplicitOverride: true);
            CheckLoadingVirtualnessFlags(SymbolVirtualness.SealedOverride, MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final, isExplicitOverride: true); //differs from above
            CheckLoadingVirtualnessFlags(SymbolVirtualness.AbstractOverride, MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Abstract, isExplicitOverride: true); //differs from above

            CheckLoadingVirtualnessFlags(SymbolVirtualness.AbstractOverride, MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Abstract | MethodAttributes.Final, isExplicitOverride: true); //differs from above
        }

        private void CheckLoadingVirtualnessFlags(SymbolVirtualness expectedVirtualness, MethodAttributes flags, bool isExplicitOverride)
        {
            const string ilTemplate = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{{
  .method public hidebysig newslot virtual 
          instance void  M() cil managed
  {{
    ret
  }}

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {{
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }}
}} // end of class Base

.class public abstract auto ansi beforefieldinit Derived
       extends Base
{{
  .method public hidebysig{0} instance void 
          M() cil managed
  {{
    {1}
    {2}
  }}

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {{
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }}
}} // end of class Derived
";

            string modifiers = "";
            if ((flags & MethodAttributes.NewSlot) != 0)
            {
                modifiers += " newslot";
            }
            if ((flags & MethodAttributes.Abstract) != 0)
            {
                modifiers += " abstract";
            }
            if ((flags & MethodAttributes.Virtual) != 0)
            {
                modifiers += " virtual";
            }
            if ((flags & MethodAttributes.Final) != 0)
            {
                modifiers += " final";
            }

            string explicitOverride = isExplicitOverride ? ".override Base::M" : "";
            string body = ((flags & MethodAttributes.Abstract) != 0) ? "" : "ret";

            CompileWithCustomILSource("", string.Format(ilTemplate, modifiers, explicitOverride, body), compilation =>
            {
                var derivedClass = compilation.GlobalNamespace.GetMember<PENamedTypeSymbol>("Derived");
                var method = derivedClass.GetMember<MethodSymbol>("M");

                switch (expectedVirtualness)
                {
                    case SymbolVirtualness.NonVirtual:
                        Assert.False(method.IsVirtual);
                        Assert.False(method.IsOverride);
                        Assert.False(method.IsAbstract);
                        Assert.False(method.IsSealed);
                        break;
                    case SymbolVirtualness.Virtual:
                        Assert.True(method.IsVirtual);
                        Assert.False(method.IsOverride);
                        Assert.False(method.IsAbstract);
                        Assert.False(method.IsSealed);
                        break;
                    case SymbolVirtualness.Override:
                        Assert.False(method.IsVirtual);
                        Assert.True(method.IsOverride);
                        Assert.False(method.IsAbstract);
                        Assert.False(method.IsSealed);
                        break;
                    case SymbolVirtualness.SealedOverride:
                        Assert.False(method.IsVirtual);
                        Assert.True(method.IsOverride);
                        Assert.False(method.IsAbstract);
                        Assert.True(method.IsSealed);
                        break;
                    case SymbolVirtualness.Abstract:
                        Assert.False(method.IsVirtual);
                        Assert.False(method.IsOverride);
                        Assert.True(method.IsAbstract);
                        Assert.False(method.IsSealed);
                        break;
                    case SymbolVirtualness.AbstractOverride:
                        Assert.False(method.IsVirtual);
                        Assert.True(method.IsOverride);
                        Assert.True(method.IsAbstract);
                        Assert.False(method.IsSealed);
                        break;
                    default:
                        Assert.False(true, "Unexpected enum value " + expectedVirtualness);
                        break;
                }
            });
        }

        // Note that not all combinations are possible.
        private enum SymbolVirtualness
        {
            NonVirtual,
            Virtual,
            Override,
            SealedOverride,
            Abstract,
            AbstractOverride,
        }

        [Fact]
        public void Constructors1()
        {
            string ilSource = @"
.class private auto ansi cls1
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

  .method public specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } 
} 

.class private auto ansi Instance_vs_Static
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          static void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

  .method public specialname rtspecialname instance 
          void  .cctor() cil managed 
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } 
} 

.class private auto ansi ReturnAValue1
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance int32  .ctor(int32 x) cil managed
  {
    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } 

  .method private specialname rtspecialname static 
          int32  .cctor() cil managed
  {
    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } 
} 

.class private auto ansi ReturnAValue2
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname static 
          int32  .cctor() cil managed
  {
    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } 
} 

.class private auto ansi Generic1
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor<T>() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

  .method private specialname rtspecialname static 
          void  .cctor<T>() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } 
} 

.class private auto ansi Generic2
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname static 
          void  .cctor<T>() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } 
} 

.class private auto ansi HasParameter
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname static 
          void  .cctor(int32 x) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } 
} 

.class private auto ansi Virtual
       extends [mscorlib]System.Object
{
  .method public newslot strict virtual specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 
} 
";

            var compilation = CreateCompilationWithCustomILSource("", ilSource);

            foreach (var m in compilation.GetTypeByMetadataName("cls1").GetMembers())
            {
                Assert.Equal(m.Name == ".cctor" ? MethodKind.StaticConstructor : MethodKind.Constructor, ((MethodSymbol)m).MethodKind);
            }

            foreach (var m in compilation.GetTypeByMetadataName("Instance_vs_Static").GetMembers())
            {
                Assert.Equal(MethodKind.Ordinary, ((MethodSymbol)m).MethodKind);
            }

            foreach (var m in compilation.GetTypeByMetadataName("ReturnAValue1").GetMembers())
            {
                Assert.Equal(MethodKind.Ordinary, ((MethodSymbol)m).MethodKind);
            }

            foreach (var m in compilation.GetTypeByMetadataName("ReturnAValue2").GetMembers())
            {
                Assert.Equal(MethodKind.Ordinary, ((MethodSymbol)m).MethodKind);
            }

            foreach (var m in compilation.GetTypeByMetadataName("Generic1").GetMembers())
            {
                Assert.Equal(MethodKind.Ordinary, ((MethodSymbol)m).MethodKind);
            }

            foreach (var m in compilation.GetTypeByMetadataName("Generic2").GetMembers())
            {
                Assert.Equal(MethodKind.Ordinary, ((MethodSymbol)m).MethodKind);
            }

            foreach (var m in compilation.GetTypeByMetadataName("HasParameter").GetMembers())
            {
                Assert.Equal(MethodKind.Ordinary, ((MethodSymbol)m).MethodKind);
            }

            foreach (var m in compilation.GetTypeByMetadataName("Virtual").GetMembers())
            {
                Assert.Equal(MethodKind.Ordinary, ((MethodSymbol)m).MethodKind);
            }
        }

        [Fact]
        public void OverridesAndLackOfNewSlot()
        {
            string ilSource = @"
.class interface public abstract auto ansi serializable Microsoft.FSharp.Control.IDelegateEvent`1<([mscorlib]System.Delegate) TDelegate>
{
  .method public hidebysig abstract virtual 
          instance void  AddHandler(!TDelegate 'handler') cil managed
  {
  } // end of method IDelegateEvent`1::AddHandler

  .method public hidebysig abstract virtual 
          instance void  RemoveHandler(!TDelegate 'handler') cil managed
  {
  } // end of method IDelegateEvent`1::RemoveHandler

} // end of class Microsoft.FSharp.Control.IDelegateEvent`1
";

            var compilation = CreateCompilationWithCustomILSource("", ilSource);

            foreach (var m in compilation.GetTypeByMetadataName("Microsoft.FSharp.Control.IDelegateEvent`1").GetMembers())
            {
                Assert.False(((MethodSymbol)m).IsVirtual);
                Assert.True(((MethodSymbol)m).IsAbstract);
                Assert.False(((MethodSymbol)m).IsOverride);
            }
        }

        [Fact]
        public void MemberSignature_LongFormType()
        {
            string source = @"
public class D
{
    public static void Main()
    {
        string s = C.RT();
        double d = C.VT();
    }
}
";
            var longFormRef = MetadataReference.CreateFromImage(TestResources.MetadataTests.Invalid.LongTypeFormInSignature);

            var c = CreateCompilationWithMscorlib(source, new[] { longFormRef });

            c.VerifyDiagnostics(
                // (6,20): error CS0570: 'C.RT()' is not supported by the language
                Diagnostic(ErrorCode.ERR_BindToBogus, "RT").WithArguments("C.RT()"),
                // (7,20): error CS0570: 'C.VT()' is not supported by the language
                Diagnostic(ErrorCode.ERR_BindToBogus, "VT").WithArguments("C.VT()"));
        }

        [WorkItem(7971, "https://github.com/dotnet/roslyn/issues/7971")]
        [Fact(Skip = "7971")]
        public void MemberSignature_CycleTrhuTypeSpecInCustomModifiers()
        {
            string source = @"
class P
{
    static void Main()
    {
        User.X(new Extender());
    }
}
";
            var lib = MetadataReference.CreateFromImage(TestResources.MetadataTests.Invalid.Signatures.SignatureCycle2);

            var c = CreateCompilationWithMscorlib(source, new[] { lib });

            c.VerifyDiagnostics();
        }

        [WorkItem(7970, "https://github.com/dotnet/roslyn/issues/7970")]
        [Fact]
        public void MemberSignature_TypeSpecInWrongPlace()
        {
            string source = @"
class P
{
    static void Main()
    {
        User.X(new System.Collections.Generic.List<int>());
    }
}
";
            var lib = MetadataReference.CreateFromImage(TestResources.MetadataTests.Invalid.Signatures.TypeSpecInWrongPlace);

            var c = CreateCompilationWithMscorlib(source, new[] { lib });

            c.VerifyDiagnostics(
                // (6,14): error CS0570: 'User.X(?)' is not supported by the language
                //         User.X(new System.Collections.Generic.List<int>());
                Diagnostic(ErrorCode.ERR_BindToBogus, "X").WithArguments("User.X(?)"));
        }

        [WorkItem(666162, "DevDiv")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void Repro666162()
        {
            var il = @"
.assembly extern mscorlib { }
.assembly extern Missing { }
.assembly Lib { }

.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .method public hidebysig instance class Test modreq (bool)&
          M() cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class Test
";

            var ilRef = CompileIL(il, appendDefaultHeader: false);
            var comp = CreateCompilation("", new[] { ilRef });

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            var method = type.GetMember<MethodSymbol>("M");
            Assert.NotNull(method.ReturnType);
        }
    }
}
