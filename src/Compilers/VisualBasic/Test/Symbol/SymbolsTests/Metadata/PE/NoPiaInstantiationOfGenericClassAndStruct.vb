' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.[Text]
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit
Imports ReferenceManager = Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation.ReferenceManager

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE

    Public Class NoPiaInstantiationOfGenericClassAndStruct
        Inherits BasicTestBase

        <Fact>
        Public Sub NoPiaIllegalGenericInstantiationSymbolForClassThatInheritsGeneric()
            'Test class that inherits Generic<NoPIAType>
            Dim localTypeSource = <text>
public class NoPIAGenerics 
   Dim field As Class1 = Nothing
end class
</text>.Value
            Dim localConsumer1 = CreateCompilation(localTypeSource)
            Dim classLocalType1 As NamedTypeSymbol = localConsumer1.SourceModule.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim localField = classLocalType1.GetMembers("field").OfType(Of FieldSymbol)().[Single]()
            Assert.Equal(SymbolKind.ErrorType, localField.[Type].BaseType.Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(localField.[Type].BaseType)
        End Sub

        <Fact>
        Public Sub NoPiaIllegalGenericInstantiationSymbolForGenericType()
            'Test field with Generic(Of NoPIAType())
            Dim localTypeSource = <text>
public class NoPIAGenerics 
   Dim nested As NestedConstructs = Nothing
end class
</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource)
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim localField = classLocalType.GetMembers("nested").OfType(Of FieldSymbol)().[Single]()
            Dim importedField = localField.[Type].GetMembers("field2").OfType(Of FieldSymbol)().[Single]()
            Assert.Equal(SymbolKind.ErrorType, importedField.[Type].Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(importedField.[Type])
        End Sub

        <Fact>
        Public Sub NoPiaIllegalGenericInstantiationSymbolForFieldWithNestedGenericType()
            'Test field with Generic(Of IGeneric(Of NoPIAType))
            Dim localTypeSource = <text>
public class NoPIAGenerics 
   Dim nested As NestedConstructs = Nothing
End class
</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource)
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim localField = classLocalType.GetMembers("nested").OfType(Of FieldSymbol)().[Single]()
            Dim importedField = localField.[Type].GetMembers("field3").OfType(Of FieldSymbol)().[Single]()
            Assert.Equal(SymbolKind.ErrorType, importedField.[Type].Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(importedField.[Type])
        End Sub

        <Fact>
        Public Sub NoPiaIllegalGenericInstantiationSymbolForFieldWithTwoNestedGenericType()
            'Test field with IGeneric(Of IGeneric(Of Generic(Of NoPIAType)))
            Dim localTypeSource = <text>
public class NoPIAGenerics 
   Dim nested As NestedConstructs = New NestedConstructs()
end class
</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource)
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim localField = classLocalType.GetMembers("nested").OfType(Of FieldSymbol)().[Single]()
            Dim importedField = localField.[Type].GetMembers("field5").OfType(Of FieldSymbol)().[Single]()

            Assert.Equal(SymbolKind.NamedType, importedField.Type.Kind)

            Dim outer = DirectCast(importedField.Type, NamedTypeSymbol).TypeArguments.Single()
            Assert.Equal(SymbolKind.NamedType, outer.Kind)

            Dim inner = DirectCast(outer, NamedTypeSymbol).TypeArguments.Single()
            Assert.Equal(SymbolKind.ErrorType, inner.Kind)
        End Sub

        <Fact>
        Public Sub NoPIAInterfaceInheritsGenericInterface()
            'Test interface that inherits IGeneric(Of NoPIAType)
            Dim localTypeSource = <text> 
public class NoPIAGenerics 
   Dim i1 As Interface1 = Nothing
end class
</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource)
            Dim classLocalType1 As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim var1 = classLocalType1.GetMembers("i1").OfType(Of FieldSymbol)().[Single]()
            Assert.Equal(SymbolKind.NamedType, var1.[Type].Kind)
            Assert.IsType(Of VisualBasic.Symbols.Metadata.PE.PENamedTypeSymbol)(var1.[Type])
        End Sub

        <Fact>
        Public Sub NoPIALocalClassInheritsGenericTypeWithPIATypeParameters()
            'Test class that inherits Generic(Of NoPIAType) used as method return or arguments
            Dim localTypeSource1 = <text>
public class NoPIAGenerics 
     Dim inheritsMethods As InheritsMethods = Nothing
end class
</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource1)
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim localField = classLocalType.GetMembers("inheritsMethods").OfType(Of FieldSymbol)().[Single]()
            For Each m In localField.[Type].GetMembers("Method1").OfType(Of MethodSymbol)()
                If m.Parameters.Length > 0 Then
                    Assert.Equal(SymbolKind.ErrorType, m.Parameters.[Where](Function(arg) arg.Name = "c1").[Select](Function(arg) arg).[Single]().[Type].BaseType.Kind)
                    Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(m.Parameters.[Where](Function(arg) arg.Name = "c1").[Select](Function(arg) arg).[Single]().[Type].BaseType)
                End If
                If m.ReturnType.TypeKind <> TypeKind.Structure Then
                    Assert.Equal(SymbolKind.ErrorType, m.ReturnType.BaseType.Kind)
                    Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(m.ReturnType.BaseType)
                End If
            Next
        End Sub

        <Fact>
        Public Sub NoPIALocalStructImplementInterfaceThatInheritsGenericTypeWithPIATypeParameters()
            'Test implementing an interface that inherits IGeneric(Of NoPIAType)
            Dim localTypeSource1 = <text> 
public class NoPIAGenerics 
    Dim i1 As Interface1 = new Interface1Impl()
End class
</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource1)
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim var1 = classLocalType.GetMembers("i1").OfType(Of FieldSymbol)().[Single]()
            Assert.Equal(SymbolKind.NamedType, var1.[Type].Kind)
            Assert.IsType(Of VisualBasic.Symbols.Metadata.PE.PENamedTypeSymbol)(var1.[Type])
        End Sub

        <Fact>
        Public Sub NoPiaIllegalGenericInstantiationSymbolForPropertyThatTakesGenericOfPIAType()
            'Test a static property that takes Generic(Of NoPIAType)
            Dim localTypeSource = <text>
public class NoPIAGenerics 
   Dim typeRef As TypeRefs1 = Nothing
end class
</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource)
            Dim classLocalType1 As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim local = classLocalType1.GetMembers("typeRef").OfType(Of FieldSymbol)().[Single]()
            Dim importedProperty = local.[Type].GetMembers("Property1").OfType(Of PropertySymbol)().[Single]()
            Assert.Equal(SymbolKind.ErrorType, importedProperty.Parameters.Where(Function(arg) arg.Name = "x").Single().Type.Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(importedProperty.Parameters.[Where](Function(arg) arg.Name = "x").[Select](Function(arg) arg).[Single]().[Type])
        End Sub

        <Fact>
        Public Sub NoPiaIllegalGenericInstantiationSymbolForPropertyThatTakesGenericOfPIAType2()
            'Test a static property that takes Generic(Of NoPIAType)
            Dim localTypeSource = <text>
public class NoPIAGenerics 
   Dim typeRef As TypeRefs1 = Nothing
end class
</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource)
            Dim classLocalType1 As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim local = classLocalType1.GetMembers("typeRef").OfType(Of FieldSymbol)().[Single]()
            Dim importedProperty = local.[Type].GetMembers("Property2").OfType(Of PropertySymbol)().[Single]()
            Assert.Equal(SymbolKind.ErrorType, importedProperty.Type.Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(importedProperty.Type)
        End Sub

        <Fact>
        Public Sub NoPiaIllegalGenericInstantiationSymbolForStaticMethodThatTakesGenericOfPiaType()
            'Test a static method that takes Generic(Of NoPIAType)
            Dim localTypeSource = <text>
public class NoPIAGenerics 
   Dim typeRef As TypeRefs1 = Nothing
end class
</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource)
            Dim classLocalType1 As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim local = classLocalType1.GetMembers("typeRef").OfType(Of FieldSymbol)().[Single]()
            Dim importedMethod = local.[Type].GetMembers("Method1").OfType(Of MethodSymbol)().[Single]()
            Assert.Equal(SymbolKind.ErrorType, importedMethod.Parameters.[Where](Function(arg) arg.Name = "x").[Select](Function(arg) arg).[Single]().[Type].Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(importedMethod.Parameters.[Where](Function(arg) arg.Name = "x").[Select](Function(arg) arg).[Single]().[Type])
        End Sub

        <Fact>
        Public Sub NoPiaIllegalGenericInstantiationSymbolForStaticMethodThatTakesOptionalGenericOfPiaType()
            'Test a static method that takes an Optional Generic(Of NoPIAType)
            Dim localTypeSource = <text>
public class NoPIAGenerics 
   Dim typeRef As TypeRefs1 = Nothing
end class
</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource)
            Dim classLocalType1 As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim local = classLocalType1.GetMembers("typeRef").OfType(Of FieldSymbol)().[Single]()
            Dim importedMethod = local.[Type].GetMembers("Method2").OfType(Of MethodSymbol)().[Single]()
            Assert.Equal(SymbolKind.ErrorType, importedMethod.Parameters.[Where](Function(arg) arg.Name = "x").[Select](Function(arg) arg).[Single]().[Type].Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(importedMethod.Parameters.[Where](Function(arg) arg.Name = "x").[Select](Function(arg) arg).[Single]().[Type])
        End Sub

        <Fact>
        Public Sub NoPiaIllegalGenericInstantiationSymbolForMethodThatTakesGenericOfEnumPiaType()
            ' Test an interface method that takes Generic(Of NoPIAType)
            Dim localTypeSource = <text> 
public class NoPIAGenerics 
   Dim i2 As TypeRefs1.Interface2 = Nothing
end class
</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource)
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim local = classLocalType.GetMembers("i2").OfType(Of FieldSymbol)().[Single]()
            Dim importedMethod = local.[Type].GetMembers("Method3").OfType(Of MethodSymbol)().[Single]()
            Assert.Equal(SymbolKind.ErrorType, importedMethod.Parameters.[Where](Function(arg) arg.Name = "x").[Select](Function(arg) arg).[Single]().[Type].Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(importedMethod.Parameters.[Where](Function(arg) arg.Name = "x").[Select](Function(arg) arg).[Single]().[Type])
        End Sub

        <Fact>
        Public Sub NoPiaIllegalGenericInstantiationSymbolForStaticMethodThatTakesGenericOfInterfacePiaType()
            ' Test a static method that returns Generic(Of NoPIAType)
            Dim localTypeSource = <text>
public class NoPIAGenerics 
   Dim typeRef As TypeRefs1 = Nothing
end class
</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource)
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim local = classLocalType.GetMembers("typeRef").OfType(Of FieldSymbol)().[Single]()
            Dim importedMethod = local.[Type].GetMembers("Method4").OfType(Of MethodSymbol)().[Single]()
            Assert.Equal(SymbolKind.ErrorType, importedMethod.ReturnType.Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(importedMethod.ReturnType)
        End Sub

        <Fact>
        Public Sub NoPiaIllegalGenericInstantiationSymbolForConstructorThatTakesGenericOfStructPiaType()
            ' Test a constructor that takes Generic(Of NoPIAType)
            Dim localTypeSource = <text> 
public class NoPIAGenerics 
     Dim tr2a As TypeRefs2 = new TypeRefs2(Nothing)
end class
</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource)
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim local = classLocalType.GetMembers("tr2a").OfType(Of FieldSymbol)().[Single]()
            Dim importedMethod = local.[Type].GetMembers(".ctor").OfType(Of MethodSymbol)().[Single]()
            Assert.Equal(SymbolKind.ErrorType, importedMethod.Parameters.[Where](Function(arg) arg.Name = "x").[Select](Function(arg) arg).[Single]().[Type].Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(importedMethod.Parameters.[Where](Function(arg) arg.Name = "x").[Select](Function(arg) arg).[Single]().[Type])
        End Sub

        <Fact>
        Public Sub NoPiaIllegalGenericInstantiationSymbolForOperatorThatTakesGenericOfPiaType()
            ' Test an operator that takes Generic(Of NoPIAType)
            Dim localTypeSource = <text> 
public class NoPIAGenerics 
     Dim tr2a As TypeRefs2 = new TypeRefs2(Nothing)
end class
</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource)
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim local = classLocalType.GetMembers("tr2a").OfType(Of FieldSymbol)().[Single]()
            Dim importedMethod = local.[Type].GetMembers("op_Implicit").OfType(Of MethodSymbol)().[Single]()
            Assert.Equal(SymbolKind.ErrorType, importedMethod.Parameters.[Where](Function(arg) arg.Name = "x").[Single]().[Type].Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(importedMethod.Parameters.[Where](Function(arg) arg.Name = "x").[Select](Function(arg) arg).[Single]().[Type])
        End Sub

        <Fact>
        Public Sub NoPiaIllegalGenericInstantiationSymbolForEventThatTakesGenericOfPiaType()
            ' Test hooking an event that takes Generic(Of NoPIAType)
            Dim localTypeSource = <text> public class NoPIAGenerics 
   Dim tr2b As TypeRefs2 = Nothing
end class</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource)
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim local = classLocalType.GetMembers("tr2b").OfType(Of FieldSymbol)().[Single]()
            Dim importedField = local.[Type].GetMembers("Event1").OfType(Of EventSymbol)().[Single]()
            Assert.Equal(SymbolKind.ErrorType, importedField.[Type].Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(importedField.[Type])
        End Sub

        <Fact>
        Public Sub NoPiaIllegalGenericInstantiationForEventWithDelegateTypeThatTakesGenericOfPiaType()
            'Test declaring event with delegate type that takes generic argument
            Dim localTypeSource = <text> public class NoPIAGenerics 
        private event Event2 As TypeRefs2.Delegate1
end class</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource)
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim var1 = classLocalType.GetMembers("Event2").OfType(Of EventSymbol)().[Single]()
            Assert.Equal(SymbolKind.NamedType, var1.[Type].Kind)
            Assert.Equal(SymbolKind.ErrorType, var1.DelegateParameters.First().Type.Kind)
        End Sub

        <Fact>
        Public Sub NoPiaIllegalGenericInstantiationForFieldWithDelegateTypeThatReturnGenericOfPiaType()
            'Test declaring field with delegate type that takes generic argument
            Dim localTypeSource = <text> public class NoPIAGenerics 
        private shared Event3 as TypeRefs2.Delegate2
end class</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource)
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim var1 = classLocalType.GetMembers("Event3").OfType(Of FieldSymbol)().[Single]()
            Assert.Equal(SymbolKind.NamedType, var1.[Type].Kind)
        End Sub

        <Fact>
        Public Sub NoPiaIllegalGenericInstantiationForStaticMethodAccessedThroughImports()
            'Test static method accessed through Imports
            Dim localTypeSource = <text> 
imports MyClass1 = Class1
public class NoPIAGenerics
    Dim [myclass] As MyClass1 = Nothing
    
end class
</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource)
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim localField = classLocalType.GetMembers("myclass").OfType(Of FieldSymbol)().[Single]()
            Assert.Equal(SymbolKind.ErrorType, localField.[Type].BaseType.Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(localField.[Type].BaseType)
        End Sub

        <Fact>
        Public Sub NoPiaStaticMethodAccessedThroughImportsOfGenericClass()
            'Test static method accessed through Imports of generic class
            Dim localTypeSource = <text>
imports MyGenericClass2 = Class2(of ISubFuncProp)

public class NoPIAGenerics  
    Dim mygeneric As MyGenericClass2 = Nothing
end class
</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource)
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("NoPIAGenerics").[Single]()
            Dim localField = classLocalType.GetMembers("mygeneric").OfType(Of FieldSymbol)().[Single]()
            Assert.Equal(SymbolKind.NamedType, localField.[Type].Kind)
            Assert.NotNull(TryCast(localField.[Type], SubstitutedNamedType))
        End Sub

        <Fact>
        Public Sub NoPIAClassThatInheritsGenericOfNoPIAType()
            'Test class that inherits Generic(Of NoPIAType)
            Dim localTypeSource1 = <text>
public class BaseClass 
    Inherits System.Collections.Generic.List(Of FooStruct)
End class

public class DrivedClass

    public shared Sub Method1(c1 As BaseClass)
    End Sub

    public Shared Function Method1() As BaseClass
        return Nothing
    End Function

End class
</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource1)
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("DrivedClass").[Single]()

            For Each m In classLocalType.GetMembers("Method1").OfType(Of MethodSymbol)()
                If m.Parameters.Length > 0 Then
                    Assert.Equal(SymbolKind.Parameter, m.Parameters.[Where](Function(arg) arg.Name = "c1").[Select](Function(arg) arg).[Single]().Kind)
                    Assert.[True](TypeOf m.Parameters.[Where](Function(arg) arg.Name = "c1").[Select](Function(arg) arg).[Single]().Type.ContainingModule Is SourceModuleSymbol)
                End If

                If m.ReturnType.TypeKind <> TypeKind.Structure Then
                    Assert.Equal(SymbolKind.NamedType, m.ReturnType.Kind)
                    Assert.[True](TypeOf m.ReturnType.ContainingModule Is SourceModuleSymbol)
                End If
            Next
        End Sub

        <Fact>
        Public Sub NoPIATypeImplementingAnInterfaceThatInheritsIGenericOfNoPiaType()
            'Test implementing an interface that inherits IGeneric(Of NoPIAType)
            Dim localTypeSource = <text>
public structure Interface1Impl2 
    Implements Interface4
End Structure
</text>.Value
            Dim localConsumer = CreateCompilation(localTypeSource)
            Dim classLocalType As NamedTypeSymbol = localConsumer.GlobalNamespace.GetTypeMembers("Interface1Impl2").[Single]()
            Assert.Equal(SymbolKind.NamedType, classLocalType.Kind)
            Assert.[True](TypeOf classLocalType.ContainingModule Is SourceModuleSymbol)
            Assert.[True](classLocalType.IsValueType)
        End Sub

        <Fact>
        Public Sub NoPiaIllegalGenericInstantiationSymbolForAssemblyRefsWithClassThatInheritsGenericOfNoPiaType()
            'Test class that inherits Generic(Of NoPIAType)
            Dim sources = <compilation name="Dummy"></compilation>
            Dim localConsumer = CreateCompilationWithMscorlib40AndReferences(
                sources,
                references:=New List(Of MetadataReference)() From {TestReferences.SymbolsTests.NoPia.NoPIAGenericsAsm1})
            Dim localConsumerRefsAsm = localConsumer.Assembly.GetNoPiaResolutionAssemblies()

            Dim nestedType = localConsumerRefsAsm.Where(Function(a) a.Name = "NoPIAGenerics1-Asm1").Single().GlobalNamespace.GetTypeMembers("NestedConstructs").Single()
            Dim localField = nestedType.GetMembers("field1").OfType(Of FieldSymbol)().Single()

            Assert.Equal(SymbolKind.ArrayType, localField.Type.Kind)
            Assert.Equal(SymbolKind.ErrorType, DirectCast(localField.Type, ArrayTypeSymbol).ElementType.Kind)
        End Sub

        <Fact>
        Public Sub NoPIAGenericsAssemblyRefsWithClassThatInheritsGenericOfNoPiaType()
            'Test class that inherits Generic(Of NoPIAType)
            Dim localConsumer = CreateCompilation(Nothing)
            Dim localConsumerRefsAsm = localConsumer.[Assembly].GetNoPiaResolutionAssemblies()
            Dim nestedType = localConsumerRefsAsm(1).GlobalNamespace.GetTypeMembers("NestedConstructs").[Single]()
            Dim localField = nestedType.GetMembers("field1").OfType(Of FieldSymbol)().[Single]()
            Assert.Equal(SymbolKind.ArrayType, localField.[Type].Kind)
            Assert.True(TypeOf localField.[Type] Is ArrayTypeSymbol)
        End Sub

        <Fact>
        Public Sub NoPIAGenericsAssemblyRefs3()
            'Test a static method that returns Generic(Of NoPIAType)
            Dim localConsumer = CreateCompilation(Nothing)
            Dim localConsumerRefsAsm = localConsumer.[Assembly].GetNoPiaResolutionAssemblies()
            Dim nestedType = localConsumerRefsAsm(1).GlobalNamespace.GetTypeMembers("TypeRefs1").[Single]()
            Dim localMethod = nestedType.GetMembers("Method4").OfType(Of MethodSymbol)().[Single]()
            Assert.Equal(SymbolKind.ErrorType, localMethod.ReturnType.Kind)
            Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(localMethod.ReturnType)
        End Sub

        <Fact>
        Public Sub NoPiaIllegalGenericInstantiationSymbolforStaticMethodThatReturnsGenericOfNoPiaType()
            'Test a static method that returns Generic(Of NoPIAType)

            Dim localTypeCompilationDef =
<compilation name="Dummy">
    <file name="Dummy.vb">
imports System.Collections.Generic

public class TypeRefs1

    public Sub Method1(x As List(Of FooStruct))
    End Sub

    public Sub Method2(Optional x As List(Of ISubFuncProp) = Nothing)
    End Sub

    public interface Interface2
        void Method3(x As List(of FooEnum))
    end interface

    public Function Method4() As List(Of ISubFuncProp)
        return new List(Of ISubFuncProp)()
    end function

    public Function Method4() As List(Of FooStruct)
        return Nothing
    end function
End class
    </file>
</compilation>

            Dim localType = CompilationUtils.CreateCompilationWithMscorlib40(localTypeCompilationDef)

            localType = localType.AddReferences(TestReferences.SymbolsTests.NoPia.GeneralPia.WithEmbedInteropTypes(True))

            Dim localConsumerCompilationDef =
<compilation name="Dummy">
    <file name="Dummy.vb">
    </file>
</compilation>

            Dim localConsumer = CompilationUtils.CreateCompilationWithMscorlib40(localConsumerCompilationDef)
            localConsumer = localConsumer.AddReferences(TestReferences.SymbolsTests.NoPia.GeneralPiaCopy, New VisualBasicCompilationReference(localType))

            Dim localConsumerRefsAsm = localConsumer.[Assembly].GetNoPiaResolutionAssemblies()
            Dim nestedType = localConsumerRefsAsm.First(Function(arg) arg.Name = "Dummy").GlobalNamespace.GetTypeMembers("TypeRefs1").[Single]()
            Dim methodSymbol = nestedType.GetMembers("Method4").OfType(Of MethodSymbol)()
            For Each m In methodSymbol
                Assert.Equal(SymbolKind.ErrorType, m.ReturnType.Kind)
                Assert.IsType(Of NoPiaIllegalGenericInstantiationSymbol)(m.ReturnType)
            Next
        End Sub

        Public Function CreateCompilation(source As String) As VisualBasicCompilation

            Dim compilationDef =
<compilation name="Dummy">
    <file name="Dummy.vb">
        <%= source %>
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40(compilationDef)
            Dim c2 = c1.AddReferences(TestReferences.SymbolsTests.NoPia.NoPIAGenericsAsm1,
                                  TestReferences.SymbolsTests.NoPia.GeneralPiaCopy)

            Return c2
        End Function

    End Class

End Namespace
