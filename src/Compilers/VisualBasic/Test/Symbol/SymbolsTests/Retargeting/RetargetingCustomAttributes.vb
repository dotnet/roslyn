' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.[Text]
Imports System.Collections.Generic
Imports System.Linq
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities
Imports Xunit
Imports Basic.Reference.Assemblies

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Retargeting
#If Not Retargeting Then
    Public Class RetargetCustomAttributes
        Inherits BasicTestBase
        Public Sub New()
            MyBase.New()
        End Sub

        Friend Class Test01
            Public c1 As VisualBasicCompilationReference
            Public c2 As VisualBasicCompilationReference
            Public c1MscorLibAssemblyRef As AssemblySymbol
            Public c2MscorlibAssemblyRef As AssemblySymbol
            Public OldMsCorLib_debuggerTypeProxyAttributeType As NamedTypeSymbol
            Public NewMsCorLib_debuggerTypeProxyAttributeType As NamedTypeSymbol
            Public OldMsCorLib_debuggerTypeProxyAttributeCtor As MethodSymbol
            Public NewMsCorLib_debuggerTypeProxyAttributeCtor As MethodSymbol
            Public OldMsCorLib_systemType As NamedTypeSymbol
            Public NewMsCorLib_systemType As NamedTypeSymbol

            Private Shared ReadOnly s_attribute As AttributeDescription = New AttributeDescription(
                "System.Diagnostics",
                "DebuggerTypeProxyAttribute",
                {New Byte() {CByte(SignatureAttributes.Instance), 1, CByte(SignatureTypeCode.Void), CByte(SignatureTypeCode.TypeHandle), CByte(AttributeDescription.TypeHandleTarget.SystemType)}})

            Public Sub New()
                Dim source =
                    <compilation name="C1">
                        <file name="a.vb"><![CDATA[
            Imports System
            Imports System.Diagnostics

            <Assembly: DebuggerTypeProxyAttribute(GetType(System.Type), Target := GetType(Integer()), TargetTypeName := "IntArrayType")>
            <Module: DebuggerTypeProxyAttribute(GetType(System.Type), Target := GetType(Integer()), TargetTypeName := "IntArrayType")>

            <DebuggerTypeProxyAttribute(GetType(System.Type), Target := GetType(Integer()), TargetTypeName := "IntArrayType")>
            Class TestClass
                <DebuggerTypeProxyAttribute(GetType(System.Type), Target := GetType(Integer()), TargetTypeName := "IntArrayType")>
                Public testField As Integer


                <DebuggerTypeProxyAttribute(GetType(System.Type), Target := GetType(Integer()), TargetTypeName := "IntArrayType")>
                Public ReadOnly Property TestProperty() As <DebuggerTypeProxyAttribute(GetType(System.Type), Target := GetType(Integer()), TargetTypeName := "IntArrayType")> Integer
                    Get
                        Return testField
                    End Get
                End Property

                <DebuggerTypeProxyAttribute(GetType(System.Type), Target := GetType(Integer()), TargetTypeName := "IntArrayType")>
                Public Function TestMethod(Of T)(<DebuggerTypeProxyAttribute(GetType(System.Type), Target := GetType(Integer()), TargetTypeName := "IntArrayType")> ByVal testParameter As T) As <DebuggerTypeProxyAttribute(GetType(System.Type), Target := GetType(Integer()), TargetTypeName := "IntArrayType")> T
                    Return testParameter
                End Function
            End Class]]>
                        </file>
                    </compilation>

                Dim compilation1 = CreateEmptyCompilationWithReferences(source, {OldMsCorLib})
                c1 = New VisualBasicCompilationReference(compilation1)
                Dim c1Assembly = compilation1.Assembly

                Dim compilation2 = VisualBasicCompilation.Create("C2", references:={NewMsCorLib, c1})
                c2 = New VisualBasicCompilationReference(compilation2)
                Dim c1AsmRef = compilation2.GetReferencedAssemblySymbol(c1)
                Assert.NotSame(c1Assembly, c1AsmRef)

                c1MscorLibAssemblyRef = compilation1.GetReferencedAssemblySymbol(OldMsCorLib)
                c2MscorlibAssemblyRef = compilation2.GetReferencedAssemblySymbol(NewMsCorLib)
                Assert.NotSame(c1MscorLibAssemblyRef, c2MscorlibAssemblyRef)

                OldMsCorLib_systemType = c1MscorLibAssemblyRef.GetTypeByMetadataName("System.Type")
                NewMsCorLib_systemType = c2MscorlibAssemblyRef.GetTypeByMetadataName("System.Type")
                Assert.NotSame(OldMsCorLib_systemType, NewMsCorLib_systemType)

                OldMsCorLib_debuggerTypeProxyAttributeType = c1MscorLibAssemblyRef.GetTypeByMetadataName("System.Diagnostics.DebuggerTypeProxyAttribute")
                NewMsCorLib_debuggerTypeProxyAttributeType = c2MscorlibAssemblyRef.GetTypeByMetadataName("System.Diagnostics.DebuggerTypeProxyAttribute")
                Assert.NotSame(OldMsCorLib_debuggerTypeProxyAttributeType, NewMsCorLib_debuggerTypeProxyAttributeType)

                OldMsCorLib_debuggerTypeProxyAttributeCtor = DirectCast(OldMsCorLib_debuggerTypeProxyAttributeType.GetMembers(".ctor").
                    Where(Function(m) DirectCast(m, MethodSymbol).ParameterCount = 1 AndAlso TypeSymbol.Equals(DirectCast(m, MethodSymbol).Parameters(0).Type, OldMsCorLib_systemType, TypeCompareKind.ConsiderEverything)).[Single](), MethodSymbol)

                NewMsCorLib_debuggerTypeProxyAttributeCtor = DirectCast(NewMsCorLib_debuggerTypeProxyAttributeType.GetMembers(".ctor").
                    Where(Function(m) DirectCast(m, MethodSymbol).ParameterCount = 1 AndAlso TypeSymbol.Equals(DirectCast(m, MethodSymbol).Parameters(0).Type, NewMsCorLib_systemType, TypeCompareKind.ConsiderEverything)).[Single](), MethodSymbol)

                Assert.NotSame(OldMsCorLib_debuggerTypeProxyAttributeCtor, NewMsCorLib_debuggerTypeProxyAttributeCtor)
            End Sub

            Public Sub TestAttributeRetargeting(symbol As Symbol)
                ' Verify GetAttributes()
                TestAttributeRetargeting(symbol.GetAttributes())

                ' Verify GetAttributes(AttributeType from Retargeted assembly)
                TestAttributeRetargeting(symbol.GetAttributes(NewMsCorLib_debuggerTypeProxyAttributeType))

                ' GetAttributes(AttributeType from Underlying assembly) should find nothing.
                Assert.Empty(symbol.GetAttributes(OldMsCorLib_debuggerTypeProxyAttributeType))

                ' Verify GetAttributes(AttributeCtor from Retargeted assembly)
                TestAttributeRetargeting(symbol.GetAttributes(NewMsCorLib_debuggerTypeProxyAttributeCtor))

                ' Verify GetAttributes(AttributeCtor from Underlying assembly)
                Assert.Empty(symbol.GetAttributes(OldMsCorLib_debuggerTypeProxyAttributeCtor))

                ' Verify GetAttributes(namespaceName, typeName, ctorSignature)
                TestAttributeRetargeting(symbol.GetAttributes(s_attribute).AsEnumerable())
            End Sub

            Public Sub TestAttributeRetargeting_ReturnTypeAttributes(symbol As MethodSymbol)
                ' Verify ReturnTypeAttributes
                TestAttributeRetargeting(symbol.GetReturnTypeAttributes())
            End Sub

            Private Sub TestAttributeRetargeting(attributes As IEnumerable(Of VisualBasicAttributeData))
                Assert.Equal(1, attributes.Count)

                Dim attribute = attributes.First()
                Assert.IsType(Of RetargetingAttributeData)(attribute)

                Assert.Same(NewMsCorLib_debuggerTypeProxyAttributeType, attribute.AttributeClass)
                Assert.Same(NewMsCorLib_debuggerTypeProxyAttributeCtor, attribute.AttributeConstructor)
                Assert.Same(NewMsCorLib_systemType, attribute.AttributeConstructor.Parameters(0).Type)

                Assert.Equal(1, attribute.CommonConstructorArguments.Length)
                attribute.VerifyValue(0, TypedConstantKind.Type, NewMsCorLib_systemType)

                Assert.Equal(2, attribute.CommonNamedArguments.Length)
                attribute.VerifyValue(0, "Target", TypedConstantKind.Type, GetType(Integer()))
                attribute.VerifyValue(1, "TargetTypeName", TypedConstantKind.Primitive, "IntArrayType")
            End Sub

        End Class

        Private Shared ReadOnly Property OldMsCorLib As MetadataReference
            Get
                Return Net40.References.mscorlib
            End Get
        End Property

        Private Shared ReadOnly Property NewMsCorLib As MetadataReference
            Get
                Return NetFramework.mscorlib
            End Get
        End Property

        <Fact>
        Public Sub Test01_AssemblyAttribute()
            Dim test As New Test01()
            Dim c1AsmRef = test.c2.Compilation.GetReferencedAssemblySymbol(test.c1)
            Assert.IsType(Of RetargetingAssemblySymbol)(c1AsmRef)
            test.TestAttributeRetargeting(c1AsmRef)
        End Sub

        <Fact>
        Public Sub Test01_ModuleAttribute()
            Dim test As New Test01()
            Dim c1AsmRef = test.c2.Compilation.GetReferencedAssemblySymbol(test.c1)
            Dim c1ModuleSym = c1AsmRef.Modules(0)
            Assert.IsType(Of RetargetingModuleSymbol)(c1ModuleSym)
            test.TestAttributeRetargeting(c1ModuleSym)
        End Sub

        <Fact>
        Public Sub Test01_NamedTypeAttribute()
            Dim test As New Test01()
            Dim testClass = test.c2.Compilation.GlobalNamespace.GetTypeMembers("TestClass").[Single]()
            Assert.IsType(Of RetargetingNamedTypeSymbol)(testClass)
            test.TestAttributeRetargeting(testClass)
        End Sub

        <Fact>
        Public Sub Test01_FieldAttribute()
            Dim test As New Test01()
            Dim testClass = test.c2.Compilation.GlobalNamespace.GetTypeMembers("TestClass").[Single]()
            Dim testField As FieldSymbol = testClass.GetMembers("testField").OfType(Of FieldSymbol)().[Single]()
            Assert.IsType(Of RetargetingFieldSymbol)(testField)
            test.TestAttributeRetargeting(testField)
        End Sub

        <Fact>
        Public Sub Test01_PropertyAttribute()
            Dim test As New Test01()
            Dim testClass = test.c2.Compilation.GlobalNamespace.GetTypeMembers("TestClass").[Single]()
            Dim testProperty As PropertySymbol = testClass.GetMembers("TestProperty").OfType(Of PropertySymbol)().[Single]()
            Assert.IsType(Of RetargetingPropertySymbol)(testProperty)
            test.TestAttributeRetargeting(testProperty)

            Dim testMethod As MethodSymbol = testProperty.GetMethod
            Assert.IsType(Of RetargetingMethodSymbol)(testMethod)
            test.TestAttributeRetargeting_ReturnTypeAttributes(testMethod)
        End Sub

        <Fact>
        Public Sub Test01_MethodAttribute()
            Dim test As New Test01()
            Dim testClass = test.c2.Compilation.GlobalNamespace.GetTypeMembers("TestClass").[Single]()
            Dim testMethod As MethodSymbol = testClass.GetMembers("TestMethod").OfType(Of MethodSymbol)().[Single]()
            Assert.IsType(Of RetargetingMethodSymbol)(testMethod)
            test.TestAttributeRetargeting(testMethod)
        End Sub

        <Fact>
        Public Sub Test01_ParameterAttribute()
            Dim test As New Test01()
            Dim testClass = test.c2.Compilation.GlobalNamespace.GetTypeMembers("TestClass").[Single]()
            Dim testMethod As MethodSymbol = testClass.GetMembers("TestMethod").OfType(Of MethodSymbol)().[Single]()
            Assert.IsType(Of RetargetingMethodSymbol)(testMethod)
            Dim testParameter As ParameterSymbol = testMethod.Parameters(0)
            Assert.True((TryCast(testParameter, RetargetingParameterSymbol)) IsNot Nothing, "RetargetingMethodSymbol's parameter must be a RetargetingParameterSymbol")
            test.TestAttributeRetargeting(testParameter)
        End Sub

        <Fact>
        Public Sub Test01_ReturnTypeAttribute()
            Dim test As New Test01()
            Dim testClass = test.c2.Compilation.GlobalNamespace.GetTypeMembers("TestClass").[Single]()
            Dim testMethod As MethodSymbol = testClass.GetMembers("TestMethod").OfType(Of MethodSymbol)().[Single]()
            Assert.IsType(Of RetargetingMethodSymbol)(testMethod)
            test.TestAttributeRetargeting_ReturnTypeAttributes(testMethod)
        End Sub

        <Fact>
        <WorkItem(569089, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/569089")>
        Public Sub NullArrays()
            Dim source1 =
<compilation>
    <file><![CDATA[
Imports System

Public Class A
    Inherits Attribute

    Public Sub New(a As Object(), b As Integer())
    End Sub

    Public Property P As Object()

    Public F As Integer()
End Class

<A(Nothing, Nothing, P:=Nothing, F:=Nothing)>
Class C

End Class
    ]]></file>
</compilation>

            Dim source2 = <compilation><file></file></compilation>

            Dim c1 = CreateEmptyCompilationWithReferences(source1, {OldMsCorLib})
            Dim c2 = CreateEmptyCompilationWithReferences(source2, {NewMsCorLib, New VisualBasicCompilationReference(c1)})

            Dim c = c2.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Assert.IsType(Of RetargetingNamedTypeSymbol)(c)

            Dim attr = c.GetAttributes().Single()

            Dim args = attr.ConstructorArguments.ToArray()

            Assert.True(args(0).IsNull)
            Assert.Equal("Object()", args(0).Type.ToDisplayString())
            Assert.Throws(Of InvalidOperationException)(Function() args(0).Value)

            Assert.True(args(1).IsNull)
            Assert.Equal("Integer()", args(1).Type.ToDisplayString())
            Assert.Throws(Of InvalidOperationException)(Function() args(1).Value)

            Dim named = attr.NamedArguments.ToDictionary(Function(e) e.Key, Function(e) e.Value)

            Assert.True(named("P").IsNull)
            Assert.Equal("Object()", named("P").Type.ToDisplayString())
            Assert.Throws(Of InvalidOperationException)(Function() named("P").Value)

            Assert.True(named("F").IsNull)
            Assert.Equal("Integer()", named("F").Type.ToDisplayString())
            Assert.Throws(Of InvalidOperationException)(Function() named("F").Value)
        End Sub

        <Fact>
        <WorkItem(65048, "https://github.com/dotnet/roslyn/issues/65048")>
        Public Sub MissingAttributeType()
            Dim source1 = "
Imports System

Public Class A
    Inherits Attribute
End Class
"

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll)

            Dim source2 = "
<A>
Public Class C1
End Class
"

            Dim comp2 = CreateCompilation(source2, references:={comp1.ToMetadataReference()}, options:=TestOptions.DebugDll)
            Dim comp3 = CreateCompilation("", references:={comp2.ToMetadataReference()}, options:=TestOptions.DebugDll)

            Dim c1 = comp3.GetTypeByMetadataName("C1")
            Dim a = c1.GetAttributes().Single()
            Assert.Equal("A", a.ToString())
            Assert.IsAssignableFrom(Of MissingMetadataTypeSymbol)(a.AttributeClass)
            Assert.Null(a.AttributeConstructor)
        End Sub

        <Fact>
        <WorkItem(65048, "https://github.com/dotnet/roslyn/issues/65048")>
        Public Sub MissingAttributeConstructor()
            Dim source1_1 = "
Imports System

Public Class A
    Inherits Attribute
End Class
"

            Dim comp1_1 = CreateCompilation(source1_1, options:=TestOptions.DebugDll, assemblyName:="Lib65048")

            Dim source2 = "
<A>
Public Class C1
End Class
"

            Dim comp2 = CreateCompilation(source2, references:={comp1_1.ToMetadataReference()}, options:=TestOptions.DebugDll)

            Dim source1_2 = "
Imports System

Public Class A
    Inherits Attribute

    Public Sub New(x as Integer)
    End Sub
End Class
"

            Dim comp1_2 = CreateCompilation(source1_2, options:=TestOptions.DebugDll, assemblyName:="Lib65048")

            Dim comp3 = CreateCompilation("", references:={comp2.ToMetadataReference(), comp1_2.ToMetadataReference()}, options:=TestOptions.DebugDll)

            Dim c1 = comp3.GetTypeByMetadataName("C1")
            Dim a = c1.GetAttributes().Single()
            Assert.Equal("A", a.ToString())
            Assert.False(a.AttributeClass.IsErrorType())
            Assert.Null(a.AttributeConstructor)
        End Sub
    End Class
#End If
End Namespace
