﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata.PE
{
    public class NoPiaInstantiationOfGenericClassAndStruct : CSharpTestBase
    {
        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForClassThatInheritsGeneric()
        {
            //Test class that inherits Generic<NoPIAType>

            var localTypeSource = @"public class NoPIAGenerics 
{
   Class1 field =  null;   
}";

            var localConsumer1 = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType1 = localConsumer1.SourceModule.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var localField = classLocalType1.GetMembers("field").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, localField.Type.TypeSymbol.BaseType().Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localField.Type.TypeSymbol.BaseType());
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForGenericType()
        {
            //Test field with Generic(Of NoPIAType())

            var localTypeSource = @"public class NoPIAGenerics 
{
   NestedConstructs nested = null;
}";

            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var localField = classLocalType.GetMembers("nested").OfType<FieldSymbol>().Single();
            var importedField = localField.Type.TypeSymbol.GetMembers("field2").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedField.Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedField.Type.TypeSymbol);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForFieldWithNestedGenericType()
        {
            //Test field with Generic(Of IGeneric(Of NoPIAType))

            var localTypeSource = @"public class NoPIAGenerics 
{
   NestedConstructs nested = null;
}";

            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var localField = classLocalType.GetMembers("nested").OfType<FieldSymbol>().Single();
            var importedField = localField.Type.TypeSymbol.GetMembers("field3").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedField.Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedField.Type.TypeSymbol);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForFieldWithTwoNestedGenericType()
        {
            //Test field with IGeneric(Of IGeneric(Of Generic(Of NoPIAType)))

            var localTypeSource = @"public class NoPIAGenerics 
{
   NestedConstructs nested = New NestedConstructs();
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var localField = classLocalType.GetMembers("nested").OfType<FieldSymbol>().Single();
            var importedField = localField.Type.TypeSymbol.GetMembers("field5").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.NamedType, importedField.Type.Kind);

            var outer = ((NamedTypeSymbol)importedField.Type.TypeSymbol).TypeArguments().Single();
            Assert.Equal(SymbolKind.NamedType, outer.Kind);

            var inner = ((NamedTypeSymbol)outer).TypeArguments().Single();
            Assert.Equal(SymbolKind.ErrorType, inner.Kind);
        }

        [Fact]
        public void NoPIAInterfaceInheritsGenericInterface()
        {
            //Test interface that inherits IGeneric(Of NoPIAType)

            var localTypeSource = @" public class NoPIAGenerics 
{
   Interface1 i1 = null;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType1 = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var var1 = classLocalType1.GetMembers("i1").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.NamedType, var1.Type.Kind);
            Assert.IsAssignableFrom<PENamedTypeSymbol>(var1.Type.TypeSymbol);
        }

        [Fact]
        public void NoPIALocalClassInheritsGenericTypeWithPIATypeParameters()
        {
            //Test class that inherits Generic(Of NoPIAType) used as method return or arguments

            var localTypeSource1 = @"public class NoPIAGenerics 
{
     InheritsMethods inheritsMethods = null;
}";
            var localConsumer = CreateCompilation(localTypeSource1);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var localField = classLocalType.GetMembers("inheritsMethods").OfType<FieldSymbol>().Single();

            foreach (MethodSymbol m in localField.Type.TypeSymbol.GetMembers("Method1").OfType<MethodSymbol>())
            {
                if (m.Parameters.Length > 0)
                {
                    Assert.Equal(SymbolKind.ErrorType, m.Parameters.Where(arg => arg.Name == "c1").Select(arg => arg).Single().Type.TypeSymbol.BaseType().Kind);
                    Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(m.Parameters.Where(arg => arg.Name == "c1").Select(arg => arg).Single().Type.TypeSymbol.BaseType());
                }
                if (m.ReturnType.TypeKind != TypeKind.Struct)
                {
                    Assert.Equal(SymbolKind.ErrorType, m.ReturnType.TypeSymbol.BaseType().Kind);
                    Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(m.ReturnType.TypeSymbol.BaseType());
                }
            }
        }

        [Fact]
        public void NoPIALocalStructImplementInterfaceThatInheritsGenericTypeWithPIATypeParameters()
        {
            //Test implementing an interface that inherits IGeneric(Of NoPIAType) 

            var localTypeSource1 = @" public class NoPIAGenerics 
{
    Interface1 i1 = new Interface1Impl();
}";
            var localConsumer = CreateCompilation(localTypeSource1);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var var1 = classLocalType.GetMembers("i1").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.NamedType, var1.Type.Kind);
            Assert.IsAssignableFrom<PENamedTypeSymbol>(var1.Type.TypeSymbol);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForPropertyThatTakesGenericOfPIAType()
        {
            //Test a static property that takes Generic(Of NoPIAType)

            var localTypeSource = @"public class NoPIAGenerics 
{
   TypeRefs1 typeRef = null;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType1 = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var local = classLocalType1.GetMembers("typeRef").OfType<FieldSymbol>().Single();
            var importedProperty = local.Type.TypeSymbol.GetMembers("Property1").OfType<PropertySymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedProperty.Parameters.Single(arg => arg.Name == "x").Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedProperty.Parameters.Single(arg => arg.Name == "x").Type.TypeSymbol);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForPropertyThatTakesGenericOfPIAType2()
        {
            //Test a static property that takes Generic(Of NoPIAType)


            var localTypeSource = @"public class NoPIAGenerics 
{
   TypeRefs1 typeRef = null;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType1 = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var local = classLocalType1.GetMembers("typeRef").OfType<FieldSymbol>().Single();
            var importedProperty = local.Type.TypeSymbol.GetMembers("Property2").OfType<PropertySymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedProperty.Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedProperty.Type.TypeSymbol);
        }


        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForStaticMethodThatTakesGenericOfPiaType()
        {
            //Test a static method that takes Generic(Of NoPIAType)

            var localTypeSource = @"public class NoPIAGenerics 
{
   TypeRefs1 typeRef = null;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType1 = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var local = classLocalType1.GetMembers("typeRef").OfType<FieldSymbol>().Single();
            var importedMethod = local.Type.TypeSymbol.GetMembers("Method1").OfType<MethodSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedMethod.Parameters.Where(arg => arg.Name == "x").Select(arg => arg).Single().Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedMethod.Parameters.Where(arg => arg.Name == "x").Select(arg => arg).Single().Type.TypeSymbol);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForStaticMethodThatTakesOptionalGenericOfPiaType()
        {
            //Test a static method that takes an Optional Generic(Of NoPIAType)

            var localTypeSource = @"public class NoPIAGenerics 
{
   TypeRefs1 typeRef = null;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType1 = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var local = classLocalType1.GetMembers("typeRef").OfType<FieldSymbol>().Single();
            var importedMethod = local.Type.TypeSymbol.GetMembers("Method2").OfType<MethodSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedMethod.Parameters.Where(arg => arg.Name == "x").Select(arg => arg).Single().Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedMethod.Parameters.Where(arg => arg.Name == "x").Select(arg => arg).Single().Type.TypeSymbol);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForMethodThatTakesGenericOfEnumPiaType()
        {
            // Test an interface method that takes Generic(Of NoPIAType)

            var localTypeSource = @" public class NoPIAGenerics 
{
   TypeRefs1.Interface2 i2 = null;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var local = classLocalType.GetMembers("i2").OfType<FieldSymbol>().Single();
            var importedMethod = local.Type.TypeSymbol.GetMembers("Method3").OfType<MethodSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedMethod.Parameters.Where(arg => arg.Name == "x").Select(arg => arg).Single().Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedMethod.Parameters.Where(arg => arg.Name == "x").Select(arg => arg).Single().Type.TypeSymbol);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForStaticMethodThatTakesGenericOfInterfacePiaType()
        {
            // Test a static method that returns Generic(Of NoPIAType)

            var localTypeSource = @"public class NoPIAGenerics 
{
   TypeRefs1 typeRef = null;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var local = classLocalType.GetMembers("typeRef").OfType<FieldSymbol>().Single();
            var importedMethod = local.Type.TypeSymbol.GetMembers("Method4").OfType<MethodSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedMethod.ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedMethod.ReturnType.TypeSymbol);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForConstructorThatTakesGenericOfStructPiaType()
        {
            // Test a constructor that takes Generic(Of NoPIAType)

            var localTypeSource = @" public class NoPIAGenerics 
{
     TypeRefs2 tr2a = new TypeRefs2(null);
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var local = classLocalType.GetMembers("tr2a").OfType<FieldSymbol>().Single();
            var importedMethod = local.Type.TypeSymbol.GetMembers(".ctor").OfType<MethodSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedMethod.Parameters.Where(arg => arg.Name == "x").Select(arg => arg).Single().Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedMethod.Parameters.Where(arg => arg.Name == "x").Select(arg => arg).Single().Type.TypeSymbol);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForOperatorThatTakesGenericOfPiaType()
        {
            // Test an operator that takes Generic(Of NoPIAType)

            var localTypeSource = @" public class NoPIAGenerics 
{
     TypeRefs2 tr2a = new TypeRefs2(null);
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var local = classLocalType.GetMembers("tr2a").OfType<FieldSymbol>().Single();
            var importedMethod = local.Type.TypeSymbol.GetMembers("op_Implicit").OfType<MethodSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedMethod.Parameters.Single(arg => arg.Name == "x").Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedMethod.Parameters.Single(arg => arg.Name == "x").Type.TypeSymbol);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForEventThatTakesGenericOfPiaType()
        {
            // Test hooking an event that takes Generic(Of NoPIAType)

            var localTypeSource = @" public class NoPIAGenerics 
{
   TypeRefs2 tr2b = null;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var local = classLocalType.GetMembers("tr2b").OfType<FieldSymbol>().Single();
            var importedField = local.Type.TypeSymbol.GetMembers("Event1").OfType<EventSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, importedField.Type.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(importedField.Type.TypeSymbol);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationForEventWithDelegateTypeThatTakesGenericOfPiaType()
        {
            //Test declaring event with delegate type that takes generic argument

            var localTypeSource = @" public class NoPIAGenerics 
{
        private event TypeRefs2.Delegate1 Event2;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var var1 = classLocalType.GetMembers("Event2").OfType<EventSymbol>().Single();

            Assert.Equal(SymbolKind.NamedType, var1.Type.Kind);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationForFieldWithDelegateTypeThatReturnGenericOfPiaType()
        {
            //Test declaring field with delegate type that takes generic argument

            var localTypeSource = @" public class NoPIAGenerics 
{
        private static TypeRefs2.Delegate2 Event3;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var var1 = classLocalType.GetMembers("Event3").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.NamedType, var1.Type.Kind);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationForStaticMethodAccessedThroughImports()
        {
            //Test static method accessed through Imports

            var localTypeSource = @" 
using MyClass1 = Class1;
public class NoPIAGenerics
{
    MyClass1 myclass = null;
    
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var localField = classLocalType.GetMembers("myclass").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, localField.Type.TypeSymbol.BaseType().Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localField.Type.TypeSymbol.BaseType());
        }

        [Fact]
        public void NoPiaStaticMethodAccessedThroughImportsOfGenericClass()
        {
            //Test static method accessed through Imports of generic class

            var localTypeSource = @"
using MyGenericClass2 = Class2<ISubFuncProp>;

public class NoPIAGenerics  
{
    MyGenericClass2 mygeneric = null;
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").Single();
            var localField = classLocalType.GetMembers("mygeneric").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.NamedType, localField.Type.Kind);
            Assert.IsType<ConstructedNamedTypeSymbol>(localField.Type.TypeSymbol);
        }

        [Fact]
        public void NoPIAClassThatInheritsGenericOfNoPIAType()
        {
            //Test class that inherits Generic(Of NoPIAType)

            var localTypeSource1 = @"public class BaseClass : System.Collections.Generic.List<FooStruct>
{
}

public class DrivedClass
{

    public static void Method1(BaseClass c1)
    {
    }

    public static BaseClass Method1()
    {
        return null;
    }

}";
            var localConsumer = CreateCompilation(localTypeSource1);
            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("DrivedClass").Single();

            foreach (MethodSymbol m in classLocalType.GetMembers("Method1").OfType<MethodSymbol>())
            {
                if (m.Parameters.Length > 0)
                {
                    Assert.Equal(SymbolKind.Parameter, m.Parameters.Where(arg => arg.Name == "c1").Select(arg => arg).Single().Kind);
                    Assert.True(m.Parameters.Where(arg => arg.Name == "c1").Select(arg => arg).Single().Type.TypeSymbol.IsFromCompilation(localConsumer));
                }
                if (m.ReturnType.TypeKind != TypeKind.Struct)
                {
                    Assert.Equal(SymbolKind.NamedType, m.ReturnType.Kind);
                    Assert.True(m.ReturnType.TypeSymbol.IsFromCompilation(localConsumer));
                }
            }
        }

        [Fact]
        public void NoPIATypeImplementingAnInterfaceThatInheritsIGenericOfNoPiaType()
        {
            //Test implementing an interface that inherits IGeneric(Of NoPIAType)

            var localTypeSource = @"public struct Interface1Impl2 : Interface4
{
}";
            var localConsumer = CreateCompilation(localTypeSource);

            NamedTypeSymbol classLocalType = localConsumer.GlobalNamespace.GetTypeMembers("Interface1Impl2").Single();

            Assert.Equal(SymbolKind.NamedType, classLocalType.Kind);
            Assert.True(classLocalType.IsFromCompilation(localConsumer));
            Assert.True(classLocalType.IsValueType);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolForAssemblyRefsWithClassThatInheritsGenericOfNoPiaType()
        {
            //Test class that inherits Generic(Of NoPIAType)

            var localConsumer = CreateCompilationWithMscorlib40(assemblyName: "Dummy", source: (string[])null,
                references: new[]
                {
                    TestReferences.SymbolsTests.NoPia.NoPIAGenericsAsm1,
                                                               });

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();

            var nestedType = localConsumerRefsAsm.Where(a => a.Name == "NoPIAGenerics1-Asm1").Single().GlobalNamespace.GetTypeMembers("NestedConstructs").Single();
            var localField = nestedType.GetMembers("field1").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.ArrayType, localField.Type.Kind);
            Assert.Equal(SymbolKind.ErrorType, ((ArrayTypeSymbol)localField.Type.TypeSymbol).ElementType.Kind);
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void NoPIAGenericsAssemblyRefsWithClassThatInheritsGenericOfNoPiaType()
        {
            //Test class that inherits Generic(Of NoPIAType)

            var localConsumer = CreateCompilation(null);

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();

            var nestedType = localConsumerRefsAsm[1].GlobalNamespace.GetTypeMembers("NestedConstructs").Single();
            var localField = nestedType.GetMembers("field1").OfType<FieldSymbol>().Single();

            Assert.Equal(SymbolKind.ArrayType, localField.Type.Kind);
            Assert.True(localField.Type.TypeSymbol is ArrayTypeSymbol);
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void NoPIAGenericsAssemblyRefs3()
        {
            //Test a static method that returns Generic(Of NoPIAType)

            var localConsumer = CreateCompilation(null);

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();

            var nestedType = localConsumerRefsAsm[1].GlobalNamespace.GetTypeMembers("TypeRefs1").Single();
            var localMethod = nestedType.GetMembers("Method4").OfType<MethodSymbol>().Single();

            Assert.Equal(SymbolKind.ErrorType, localMethod.ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localMethod.ReturnType.TypeSymbol);
        }

        [Fact]
        public void NoPiaIllegalGenericInstantiationSymbolforStaticMethodThatReturnsGenericOfNoPiaType()
        {
            //Test a static method that returns Generic(Of NoPIAType)

            var localTypeSource = @"
using System.Collections.Generic;
public class TypeRefs1
{
    public void Method1(List<FooStruct> x)
    {
    }
    public void Method2(List<ISubFuncProp> x = null)
    {
    }
    public interface Interface2
    {
        void Method3(List<FooEnum> x);
    }
    public List<ISubFuncProp> Method4()
    {
        return new List<ISubFuncProp>();
    }
    public List<FooStruct> Method4()
    {
        return null;
    }
}";

            var localType = CreateCompilation(assemblyName: "Dummy", source: localTypeSource,
                references: new[] { TestReferences.SymbolsTests.NoPia.GeneralPia.WithEmbedInteropTypes(true) });

            var localConsumer = CreateCompilation(assemblyName: "Dummy", source: (string[])null,
                references: new MetadataReference[]
                {
                    TestReferences.SymbolsTests.NoPia.GeneralPiaCopy,
                    new CSharpCompilationReference(localType)
                                                        });

            var localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies();

            var nestedType = localConsumerRefsAsm.First(arg => arg.Name == "Dummy").GlobalNamespace.GetTypeMembers("TypeRefs1").Single();
            var methodSymbol = nestedType.GetMembers("Method4").OfType<MethodSymbol>();

            foreach (MethodSymbol m in methodSymbol)
            {
                Assert.Equal(SymbolKind.ErrorType, m.ReturnType.Kind);
                Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(m.ReturnType.TypeSymbol);
            }
        }

        public CSharpCompilation CreateCompilation(string source)
        {
            return CreateCompilationWithMscorlib46(
                assemblyName: "Dummy",
                source: (null == source) ? null : new string[] { source },
                references: new[]
                {
                    TestReferences.SymbolsTests.NoPia.NoPIAGenericsAsm1,
                    TestReferences.SymbolsTests.NoPia.GeneralPiaCopy
                });
        }
    }
}

