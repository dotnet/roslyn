' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class GroupClassTests
        Inherits BasicTestBase

        Private Shared Function ParseTemplateTree(text As String, Optional path As String = Nothing) As SyntaxTree
            Return VisualBasicSyntaxTree.ParseText(
                SourceText.From(text, encoding:=Nothing, checksumAlgorithm:=SourceHashAlgorithms.Default),
                isMyTemplate:=True,
                path:=path)
        End Function

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/34467")>
        Public Sub SimpleTest1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq

Module Module1
    Sub Main()
        Dim gr As Type = GetType(MyTests)

        Dim bindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Static Or Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.DeclaredOnly
        Dim fields = gr.GetFields(bindingFlags).OrderBy(Function(f) f.Name)

        For Each field In fields
            System.Console.WriteLine("{0} {1} {2}", field.Name, field.FieldType, field.Attributes)
            For Each attribute In field.GetCustomAttributes(False)
                System.Console.WriteLine("  {0}", attribute)
            Next
        Next

        System.Console.WriteLine("----------------------")
        Dim methods = gr.GetMethods(bindingFlags).OrderBy(Function(f) f.Name)

        For Each method In methods
            System.Console.WriteLine("{0} {1} {2}", method.Name, method.Attributes, CInt(method.GetMethodImplementationFlags()))
            For Each attribute In method.GetCustomAttributes(False)
                System.Console.WriteLine("  {0}", attribute)
            Next
        Next

        System.Console.WriteLine("----------------------")
        Dim properties = gr.GetProperties(bindingFlags).OrderBy(Function(f) f.Name)

        For Each prop In properties
            System.Console.WriteLine("{0} {1}", prop.Name, prop.Attributes)
            For Each attribute In prop.GetCustomAttributes(False)
                System.Console.WriteLine("  {0}", attribute)
            Next
        Next

        Dim x As New MyTests()
        Dim y = x.m_DefaultInstanceTest1
        y = x.DefaultInstanceTest1
        x.DefaultInstanceTest1 = Nothing
    End Sub
End Module

<Microsoft.VisualBasic.MyGroupCollection("defaultInstanceteSt", "Create", "Dispose", "Factory.Tests")> _
Public NotInheritable Class MyTests
    Private Shared Function Create(Of T As {New, DefaultInstanceTest}) _
        (Instance As T) As T
        If Instance Is Nothing Then
            Return New T()
        Else
            Return Instance
        End If
    End Function

    Private Shared Sub Dispose(Of T As DefaultInstanceTest)(ByRef Instance As T)
        Instance.Close()
        Instance = Nothing
    End Sub

End Class

Public Class DefaultInstanceTest
    Sub Close()
    End Sub

    Public F1 As Integer
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTest
End Class

Public Class DefaultInstanceTest2
    Inherits DefaultInstanceTest
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef,
                                                                                     {SystemCoreRef},
                                                                                     TestOptions.DebugExe)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim expected() As String = {"Sub MyTests..ctor()",
                                        "Function MyTests.Create(Of T)(Instance As T) As T",
                                        "Sub MyTests.Dispose(Of T)(ByRef Instance As T)",
                                        "MyTests.m_DefaultInstanceTest1 As DefaultInstanceTest1",
                                        "MyTests.m_DefaultInstanceTest2 As DefaultInstanceTest2",
                                        "Function MyTests.get_DefaultInstanceTest1() As DefaultInstanceTest1",
                                        "Function MyTests.get_DefaultInstanceTest2() As DefaultInstanceTest2",
                                        "Sub MyTests.set_DefaultInstanceTest1(Value As DefaultInstanceTest1)",
                                        "Sub MyTests.set_DefaultInstanceTest2(Value As DefaultInstanceTest2)",
                                        "Property MyTests.DefaultInstanceTest1 As DefaultInstanceTest1",
                                        "Property MyTests.DefaultInstanceTest2 As DefaultInstanceTest2"}

            Dim members = MyTests.GetMembers()
            Assert.Equal(expected.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected(i), members(i).ToTestDisplayString())
            Next

            Dim expectedOutput =
            <![CDATA[
m_DefaultInstanceTest1 DefaultInstanceTest1 Public
  System.ComponentModel.EditorBrowsableAttribute
m_DefaultInstanceTest2 DefaultInstanceTest2 Public
  System.ComponentModel.EditorBrowsableAttribute
----------------------
Create PrivateScope, Private, Static 0
Dispose PrivateScope, Private, Static 0
get_DefaultInstanceTest1 PrivateScope, Public, SpecialName 0
  System.Diagnostics.DebuggerHiddenAttribute
get_DefaultInstanceTest2 PrivateScope, Public, SpecialName 0
  System.Diagnostics.DebuggerHiddenAttribute
set_DefaultInstanceTest1 PrivateScope, Public, SpecialName 0
  System.Diagnostics.DebuggerHiddenAttribute
set_DefaultInstanceTest2 PrivateScope, Public, SpecialName 0
  System.Diagnostics.DebuggerHiddenAttribute
----------------------
DefaultInstanceTest1 None
DefaultInstanceTest2 None
]]>

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=expectedOutput).VerifyDiagnostics()

            verifier.VerifyIL("MyTests.get_DefaultInstanceTest1",
            <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldfld      "MyTests.m_DefaultInstanceTest1 As DefaultInstanceTest1"
  IL_0007:  call       "Function MyTests.Create(Of DefaultInstanceTest1)(DefaultInstanceTest1) As DefaultInstanceTest1"
  IL_000c:  stfld      "MyTests.m_DefaultInstanceTest1 As DefaultInstanceTest1"
  IL_0011:  ldarg.0
  IL_0012:  ldfld      "MyTests.m_DefaultInstanceTest1 As DefaultInstanceTest1"
  IL_0017:  br.s       IL_0019
  IL_0019:  ret
}
]]>)

            verifier.VerifyIL("MyTests.set_DefaultInstanceTest1",
            <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  ldfld      "MyTests.m_DefaultInstanceTest1 As DefaultInstanceTest1"
  IL_0007:  ceq
  IL_0009:  brfalse.s  IL_000d
  IL_000b:  br.s       IL_0029
  IL_000d:  ldarg.1
  IL_000e:  ldnull
  IL_000f:  cgt.un
  IL_0011:  brfalse.s  IL_001e
  IL_0013:  ldstr      "Property can only be set to Nothing"
  IL_0018:  newobj     "Sub System.ArgumentException..ctor(String)"
  IL_001d:  throw
  IL_001e:  ldarg.0
  IL_001f:  ldflda     "MyTests.m_DefaultInstanceTest1 As DefaultInstanceTest1"
  IL_0024:  call       "Sub MyTests.Dispose(Of DefaultInstanceTest1)(ByRef DefaultInstanceTest1)"
  IL_0029:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub GroupClassesReferToEachOther()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Public Class DefaultInstanceTestBase8
End Class

Public Class DefaultInstanceTestBase9
End Class

<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase8", "Create1", "Dispose1", "")> _
Public Class MyTests15
    Inherits DefaultInstanceTestBase9

    Private Function Create1(Of T As {New})(Instance As T) As T
        Throw New NotImplementedException()
    End Function

    Private Sub Dispose1(Of T)(ByRef Instance As T)
        Throw New NotImplementedException()
    End Sub
End Class

<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase9", "Create1", "Dispose1", "")> _
Public Class MyTests16
    Inherits DefaultInstanceTestBase8

    Private Function Create1(Of T As {New})(Instance As T) As T
        Throw New NotImplementedException()
    End Function

    Private Sub Dispose1(Of T)(ByRef Instance As T)
        Throw New NotImplementedException()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests15 = compilation.GetTypeByMetadataName("MyTests15")

            Dim expected15() As String = {"Sub MyTests15..ctor()",
                                        "Function MyTests15.Create1(Of T)(Instance As T) As T",
                                        "Sub MyTests15.Dispose1(Of T)(ByRef Instance As T)",
                                        "MyTests15.m_MyTests16 As MyTests16",
                                        "Function MyTests15.get_MyTests16() As MyTests16",
                                        "Sub MyTests15.set_MyTests16(Value As MyTests16)",
                                        "Property MyTests15.MyTests16 As MyTests16"}

            Dim members = MyTests15.GetMembers()
            Assert.Equal(expected15.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected15(i), members(i).ToTestDisplayString())
            Next

            Dim MyTests16 = compilation.GetTypeByMetadataName("MyTests16")

            Dim expected16() As String = {"Sub MyTests16..ctor()",
                                        "Function MyTests16.Create1(Of T)(Instance As T) As T",
                                        "Sub MyTests16.Dispose1(Of T)(ByRef Instance As T)",
                                        "MyTests16.m_MyTests15 As MyTests15",
                                        "Function MyTests16.get_MyTests15() As MyTests15",
                                        "Sub MyTests16.set_MyTests15(Value As MyTests15)",
                                        "Property MyTests16.MyTests15 As MyTests15"}

            members = MyTests16.GetMembers()
            Assert.Equal(expected16.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected16(i), members(i).ToTestDisplayString())
            Next

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub GroupClassRefersToItself()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Public Class DefaultInstanceTestBase8
End Class

<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase8", "Create1", "Dispose1", "")> _
Public Class MyTests15
    Inherits DefaultInstanceTestBase8

    Sub New()
    End Sub

    Private Function Create1(Of T As {New})(Instance As T) As T
        Throw New NotImplementedException()
    End Function

    Private Sub Dispose1(Of T)(ByRef Instance As T)
        Throw New NotImplementedException()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests15 = compilation.GetTypeByMetadataName("MyTests15")

            Dim expected15() As String = {"Sub MyTests15..ctor()",
                                        "Function MyTests15.Create1(Of T)(Instance As T) As T",
                                        "Sub MyTests15.Dispose1(Of T)(ByRef Instance As T)",
                                        "MyTests15.m_MyTests15 As MyTests15",
                                        "Function MyTests15.get_MyTests15() As MyTests15",
                                        "Sub MyTests15.set_MyTests15(Value As MyTests15)",
                                        "Property MyTests15.MyTests15 As MyTests15"}

            Dim members = MyTests15.GetMembers()
            Assert.Equal(expected15.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected15(i), members(i).ToTestDisplayString())
            Next

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub GroupClassIsMyGroupCollectionAttribute()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Namespace Microsoft.VisualBasic

    Class MyGroupCollectionAttributeBase
        Inherits System.Attribute
    End Class


    <Microsoft.VisualBasic.MyGroupCollection("Microsoft.VisualBasic.MyGroupCollectionAttributeBase", "Create", "Dispose", "")> _
    Class MyGroupCollectionAttribute
        Inherits MyGroupCollectionAttributeBase

        Public Sub New()
        End Sub

        Public Sub New( _
            typeToCollect As String, _
            createInstanceMethodName As String, _
            disposeInstanceMethodName As String, _
            defaultInstanceAlias As String _
        )

        End Sub

        Private Shared Function Create(Of T As New) _
            (Instance As T) As T
            If Instance Is Nothing Then
                Return New T()
            Else
                Return Instance
            End If
        End Function

        Private Shared Sub Dispose(Of T)(Instance As T)
            Instance = Nothing
        End Sub
    End Class
End Namespace
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(compilationDef, options:=TestOptions.ReleaseDll)

            Dim MyGroupCollectionAttribute = compilation.GetTypeByMetadataName("Microsoft.VisualBasic.MyGroupCollectionAttribute")

            Dim members = MyGroupCollectionAttribute.GetMembers()
            Assert.Equal(4, members.Length)

            AssertTheseDeclarationDiagnostics(compilation,
<expected><![CDATA[
BC37201: MyGroupCollectionAttribute cannot be applied to itself.
    <Microsoft.VisualBasic.MyGroupCollection("Microsoft.VisualBasic.MyGroupCollectionAttributeBase", "Create", "Dispose", "")> _
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub ReferToItselfInAttributeArgument()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTest", MyTests.CreateName, "Dispose", "Factory.Tests")> _
Public NotInheritable Class MyTests
    Public Const CreateName As String = "Create"

    Private Shared Function Create(Of T As {New, DefaultInstanceTest}) _
        (Instance As T) As T
        If Instance Is Nothing Then
            Return New T()
        Else
            Return Instance
        End If
    End Function

    Private Shared Sub Dispose(Of T As DefaultInstanceTest)(ByRef Instance As T)
        Instance.Close()
        Instance = Nothing
    End Sub

End Class

Public Class DefaultInstanceTest
    Sub Close()
    End Sub

    Public F1 As Integer
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTest
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim members = MyTests.GetMembers()
            Assert.Equal(4, members.Length)

            AssertTheseDeclarationDiagnostics(compilation,
<expected><![CDATA[
BC37202: Literal expected.
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTest", MyTests.CreateName, "Dispose", "Factory.Tests")> _
                                                                ~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub StructureTest()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTest", "Create", "Dispose", "Factory.Tests")> _
Public Structure MyTests
    Private Shared Function Create(Of T As {New, DefaultInstanceTest}) _
        (Instance As T) As T
        If Instance Is Nothing Then
            Return New T()
        Else
            Return Instance
        End If
    End Function

    Private Shared Sub Dispose(Of T As DefaultInstanceTest)(ByRef Instance As T)
        Instance.Close()
        Instance = Nothing
    End Sub

End Structure

Public Class DefaultInstanceTest
    Sub Close()
    End Sub

    Public F1 As Integer
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTest
End Class

Public Class DefaultInstanceTest2
    Inherits DefaultInstanceTest
End Class

Namespace Microsoft.VisualBasic
    Class MyGroupCollectionAttribute
        Inherits Attribute

        Public Sub New( _
            typeToCollect As String, _
            createInstanceMethodName As String, _
            disposeInstanceMethodName As String, _
            defaultInstanceAlias As String _
        )

        End Sub
    End Class
End Namespace
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(compilationDef, options:=TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim members = MyTests.GetMembers()
            Assert.Equal(3, members.Length)

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub GenericGroupClass()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTest", "Create", "Dispose", "Factory.Tests")> _
Public Class MyTests(Of S)
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub
End Class

Public Class DefaultInstanceTest
    Sub Close()
    End Sub

    Public F1 As Integer
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTest
End Class

Public Class DefaultInstanceTest2
    Inherits DefaultInstanceTest
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests`1")

            Dim members = MyTests.GetMembers()
            Assert.Equal(3, members.Length)

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub AttributeArguments1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.MyGroupCollection(Nothing, Nothing, Nothing, Nothing)> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub
End Class

Public Class DefaultInstanceTest
    Sub Close()
    End Sub

    Public F1 As Integer
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTest
End Class

Public Class DefaultInstanceTest2
    Inherits DefaultInstanceTest
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim members = MyTests.GetMembers()
            Assert.Equal(3, members.Length)

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub AttributeArguments2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1_1,DefaultInstanceTestBase1_2", "Create, Create", "Dispose", "Factory.Tests")> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub
End Class

Public Class DefaultInstanceTestBase1_1
End Class

Public Class DefaultInstanceTestBase1_2
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1_1
End Class

Public Class DefaultInstanceTest2
    Inherits DefaultInstanceTestBase1_2
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim expected() As String = {"Sub MyTests..ctor()",
                                        "Function MyTests.Create(Of T)(Instance As T) As T",
                                        "Sub MyTests.Dispose(Of T)(ByRef Instance As T)",
                                        "MyTests.m_DefaultInstanceTest1 As DefaultInstanceTest1",
                                        "MyTests.m_DefaultInstanceTest2 As DefaultInstanceTest2",
                                        "Function MyTests.get_DefaultInstanceTest1() As DefaultInstanceTest1",
                                        "Function MyTests.get_DefaultInstanceTest2() As DefaultInstanceTest2",
                                        "Sub MyTests.set_DefaultInstanceTest1(Value As DefaultInstanceTest1)",
                                        "Property MyTests.DefaultInstanceTest1 As DefaultInstanceTest1",
                                        "ReadOnly Property MyTests.DefaultInstanceTest2 As DefaultInstanceTest2"}

            Dim members = MyTests.GetMembers()
            Assert.Equal(expected.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected(i), members(i).ToTestDisplayString())
            Next

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub AttributeArguments3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1_1,DefaultInstanceTestBase1_2", "Create, Create", ",Dispose", ",Factory.Tests")> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub
End Class

Public Class DefaultInstanceTestBase1_1
End Class

Public Class DefaultInstanceTestBase1_2
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1_1
End Class

Public Class DefaultInstanceTest2
    Inherits DefaultInstanceTestBase1_2
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim expected() As String = {"Sub MyTests..ctor()",
                                        "Function MyTests.Create(Of T)(Instance As T) As T",
                                        "Sub MyTests.Dispose(Of T)(ByRef Instance As T)",
                                        "MyTests.m_DefaultInstanceTest1 As DefaultInstanceTest1",
                                        "MyTests.m_DefaultInstanceTest2 As DefaultInstanceTest2",
                                        "Function MyTests.get_DefaultInstanceTest1() As DefaultInstanceTest1",
                                        "Function MyTests.get_DefaultInstanceTest2() As DefaultInstanceTest2",
                                        "Sub MyTests.set_DefaultInstanceTest2(Value As DefaultInstanceTest2)",
                                        "ReadOnly Property MyTests.DefaultInstanceTest1 As DefaultInstanceTest1",
                                        "Property MyTests.DefaultInstanceTest2 As DefaultInstanceTest2"}

            Dim members = MyTests.GetMembers()
            Assert.Equal(expected.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected(i), members(i).ToTestDisplayString())
            Next

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub AttributeArguments4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1_1,DefaultInstanceTestBase1_2", "Create, ", ",Dispose", ",Factory.Tests")> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub
End Class

Public Class DefaultInstanceTestBase1_1
End Class

Public Class DefaultInstanceTestBase1_2
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1_1
End Class

Public Class DefaultInstanceTest2
    Inherits DefaultInstanceTestBase1_2
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim expected() As String = {"Sub MyTests..ctor()",
                                        "Function MyTests.Create(Of T)(Instance As T) As T",
                                        "Sub MyTests.Dispose(Of T)(ByRef Instance As T)",
                                        "MyTests.m_DefaultInstanceTest1 As DefaultInstanceTest1",
                                        "Function MyTests.get_DefaultInstanceTest1() As DefaultInstanceTest1",
                                        "ReadOnly Property MyTests.DefaultInstanceTest1 As DefaultInstanceTest1"}

            Dim members = MyTests.GetMembers()
            Assert.Equal(expected.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected(i), members(i).ToTestDisplayString())
            Next

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub AttributeArguments5()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1_1,", "Create, Create", ",Dispose", ",Factory.Tests")> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub
End Class

Public Class DefaultInstanceTestBase1_1
End Class

Public Class DefaultInstanceTestBase1_2
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1_1
End Class

Public Class DefaultInstanceTest2
    Inherits DefaultInstanceTestBase1_2
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim expected() As String = {"Sub MyTests..ctor()",
                                        "Function MyTests.Create(Of T)(Instance As T) As T",
                                        "Sub MyTests.Dispose(Of T)(ByRef Instance As T)",
                                        "MyTests.m_DefaultInstanceTest1 As DefaultInstanceTest1",
                                        "Function MyTests.get_DefaultInstanceTest1() As DefaultInstanceTest1",
                                        "ReadOnly Property MyTests.DefaultInstanceTest1 As DefaultInstanceTest1"}

            Dim members = MyTests.GetMembers()
            Assert.Equal(expected.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected(i), members(i).ToTestDisplayString())
            Next

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub AttributeArguments6()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1_1,DefaultInstanceTestBase1_2", ",Create", ",Dispose", ",Factory.Tests")> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub
End Class

Public Class DefaultInstanceTestBase1_1
End Class

Public Class DefaultInstanceTestBase1_2
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1_1
End Class

Public Class DefaultInstanceTest2
    Inherits DefaultInstanceTestBase1_2
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim expected() As String = {"Sub MyTests..ctor()",
                                        "Function MyTests.Create(Of T)(Instance As T) As T",
                                        "Sub MyTests.Dispose(Of T)(ByRef Instance As T)"}

            Dim members = MyTests.GetMembers()
            Assert.Equal(expected.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected(i), members(i).ToTestDisplayString())
            Next

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub AttributeArguments7()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.MyGroupCollection(" ,DefaultInstanceTestBase1_2", "Create,Create", ",Dispose", ",Factory.Tests")> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub
End Class

Public Class DefaultInstanceTestBase1_1
End Class

Public Class DefaultInstanceTestBase1_2
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1_1
End Class

Public Class DefaultInstanceTest2
    Inherits DefaultInstanceTestBase1_2
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim expected() As String = {"Sub MyTests..ctor()",
                                        "Function MyTests.Create(Of T)(Instance As T) As T",
                                        "Sub MyTests.Dispose(Of T)(ByRef Instance As T)"}

            Dim members = MyTests.GetMembers()
            Assert.Equal(expected.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected(i), members(i).ToTestDisplayString())
            Next

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub NonClasses()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.MyGroupCollection("System.Object,System.ValueType,System.Enum,System.Delegate", "Create,Create,Create,Create", "", "")> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub
End Class

Class TestClass
End Class

Structure TestStructure
End Structure

Delegate Sub TestDelegate()

Enum TestEnum
    x
End Enum

Module TestModule
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim expected() As String = {"Sub MyTests..ctor()",
                                        "Function MyTests.Create(Of T)(Instance As T) As T",
                                        "Sub MyTests.Dispose(Of T)(ByRef Instance As T)"}

            Dim members = MyTests.GetMembers()
            Assert.Equal(expected.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected(i), members(i).ToTestDisplayString())
            Next

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub DerivedGeneric()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1_1", "Create", "Dispose", "")> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub
End Class

Public Class DefaultInstanceTestBase1_1
End Class

Public Class DefaultInstanceTest1(Of T)
    Inherits DefaultInstanceTestBase1_1
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim expected() As String = {"Sub MyTests..ctor()",
                                        "Function MyTests.Create(Of T)(Instance As T) As T",
                                        "Sub MyTests.Dispose(Of T)(ByRef Instance As T)"}

            Dim members = MyTests.GetMembers()
            Assert.Equal(expected.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected(i), members(i).ToTestDisplayString())
            Next

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub DerivedMustInherit()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1_1", "Create", "Dispose", "")> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub
End Class

Public Class DefaultInstanceTestBase1_1
End Class

Public MustInherit Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1_1

    Public Sub New()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim expected() As String = {"Sub MyTests..ctor()",
                                        "Function MyTests.Create(Of T)(Instance As T) As T",
                                        "Sub MyTests.Dispose(Of T)(ByRef Instance As T)"}

            Dim members = MyTests.GetMembers()
            Assert.Equal(expected.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected(i), members(i).ToTestDisplayString())
            Next

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub DerivedIsNested()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "Create", "Dispose", "")> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub
End Class

Public Class DefaultInstanceTestBase1
End Class

Class Parent
    Public Class DefaultInstanceTest1
        Inherits DefaultInstanceTestBase1
    End Class

    Public Class DefaultInstanceTest2
        Inherits DefaultInstanceTestBase1
    End Class
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim expected() As String = {"Sub MyTests..ctor()",
                                        "Function MyTests.Create(Of T)(Instance As T) As T",
                                        "Sub MyTests.Dispose(Of T)(ByRef Instance As T)"}

            Dim members = MyTests.GetMembers()
            Assert.Equal(expected.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected(i), members(i).ToTestDisplayString())
            Next

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub NoPublicParameterlessConstructor()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "Create", "Dispose", "")> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub
End Class

Public Class DefaultInstanceTestBase1
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1
    Public Sub New(x As Integer)
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim expected() As String = {"Sub MyTests..ctor()",
                                        "Function MyTests.Create(Of T)(Instance As T) As T",
                                        "Sub MyTests.Dispose(Of T)(ByRef Instance As T)"}

            Dim members = MyTests.GetMembers()
            Assert.Equal(expected.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected(i), members(i).ToTestDisplayString())
            Next

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub MangleNames()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase2", "Create", "Dispose", "")> _
Public Class MyTests
    Private Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub

End Class

Public Class DefaultInstanceTestBase2
End Class

Namespace NS1
    Public Class DefaultInstanceTest3
        Inherits DefaultInstanceTestBase2
    End Class
End Namespace

Namespace NS2
    Public Class DefaultInstanceTest3
        Inherits DefaultInstanceTestBase2
    End Class
End Namespace
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim expected() As String = {"Sub MyTests..ctor()",
                                        "Function MyTests.Create(Of T)(Instance As T) As T",
                                        "Sub MyTests.Dispose(Of T)(ByRef Instance As T)",
                                        "MyTests.m_NS1_DefaultInstanceTest3 As NS1.DefaultInstanceTest3",
                                        "MyTests.m_NS2_DefaultInstanceTest3 As NS2.DefaultInstanceTest3",
                                        "Function MyTests.get_NS1_DefaultInstanceTest3() As NS1.DefaultInstanceTest3",
                                        "Function MyTests.get_NS2_DefaultInstanceTest3() As NS2.DefaultInstanceTest3",
                                        "Sub MyTests.set_NS1_DefaultInstanceTest3(Value As NS1.DefaultInstanceTest3)",
                                        "Sub MyTests.set_NS2_DefaultInstanceTest3(Value As NS2.DefaultInstanceTest3)",
                                        "Property MyTests.NS1_DefaultInstanceTest3 As NS1.DefaultInstanceTest3",
                                        "Property MyTests.NS2_DefaultInstanceTest3 As NS2.DefaultInstanceTest3"}

            Dim members = MyTests.GetMembers()
            Assert.Equal(expected.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected(i), members(i).ToTestDisplayString())
            Next

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub AmbiguousMatch()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Public Class DefaultInstanceTestBase6
End Class

Public Class DefaultInstanceTestBase7
    Inherits DefaultInstanceTestBase6
End Class

Class DefaultInstanceTest6
    Inherits DefaultInstanceTestBase7
End Class

<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase6,DefaultInstanceTestBase7", "Create1,Create2", "Dispose1,Dispose2", "")> _
Public Class MyTests
    Private Function Create1(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Sub Dispose1(Of T)(ByRef Instance As T)
    End Sub

    Private Function Create2(Of T As {New})(Instance As T) As T
        Return Nothing
    End Function

    Private Sub Dispose2(Of T)(ByRef Instance As T)
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim expected() As String = {"Sub MyTests..ctor()",
                                        "Function MyTests.Create1(Of T)(Instance As T) As T",
                                        "Sub MyTests.Dispose1(Of T)(ByRef Instance As T)",
                                        "Function MyTests.Create2(Of T)(Instance As T) As T",
                                        "Sub MyTests.Dispose2(Of T)(ByRef Instance As T)",
                                        "MyTests.m_DefaultInstanceTest6 As DefaultInstanceTest6",
                                        "MyTests.m_DefaultInstanceTestBase7 As DefaultInstanceTestBase7",
                                        "Function MyTests.get_DefaultInstanceTest6() As DefaultInstanceTest6",
                                        "Function MyTests.get_DefaultInstanceTestBase7() As DefaultInstanceTestBase7",
                                        "Sub MyTests.set_DefaultInstanceTest6(Value As DefaultInstanceTest6)",
                                        "Sub MyTests.set_DefaultInstanceTestBase7(Value As DefaultInstanceTestBase7)",
                                        "Property MyTests.DefaultInstanceTest6 As DefaultInstanceTest6",
                                        "Property MyTests.DefaultInstanceTestBase7 As DefaultInstanceTestBase7"}

            Dim members = MyTests.GetMembers()
            Assert.Equal(expected.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected(i), members(i).ToTestDisplayString())
            Next

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()

            ' Verify IL to make sure the right versions of Create/Dispose are called.
            verifier.VerifyIL("MyTests.get_DefaultInstanceTest6",
            <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldarg.0
  IL_0003:  ldfld      "MyTests.m_DefaultInstanceTest6 As DefaultInstanceTest6"
  IL_0008:  call       "Function MyTests.Create2(Of DefaultInstanceTest6)(DefaultInstanceTest6) As DefaultInstanceTest6"
  IL_000d:  stfld      "MyTests.m_DefaultInstanceTest6 As DefaultInstanceTest6"
  IL_0012:  ldarg.0
  IL_0013:  ldfld      "MyTests.m_DefaultInstanceTest6 As DefaultInstanceTest6"
  IL_0018:  ret
}
]]>)

            verifier.VerifyIL("MyTests.set_DefaultInstanceTestBase7",
            <![CDATA[
{
  // Code size       36 (0x24)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  ldfld      "MyTests.m_DefaultInstanceTestBase7 As DefaultInstanceTestBase7"
  IL_0007:  beq.s      IL_0023
  IL_0009:  ldarg.1
  IL_000a:  brfalse.s  IL_0017
  IL_000c:  ldstr      "Property can only be set to Nothing"
  IL_0011:  newobj     "Sub System.ArgumentException..ctor(String)"
  IL_0016:  throw
  IL_0017:  ldarg.0
  IL_0018:  ldarg.0
  IL_0019:  ldflda     "MyTests.m_DefaultInstanceTestBase7 As DefaultInstanceTestBase7"
  IL_001e:  call       "Sub MyTests.Dispose1(Of DefaultInstanceTestBase7)(ByRef DefaultInstanceTestBase7)"
  IL_0023:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ConflictWithMember1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "Create", "Dispose", "")> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub

    Private m_DefaultInstanceTest1 As Integer
End Class

Public Class DefaultInstanceTestBase1
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim members = MyTests.GetMembers()
            Assert.Equal(4, members.Length)

            AssertTheseDeclarationDiagnostics(compilation,
<expected><![CDATA[
BC36015: 'Private m_DefaultInstanceTest1 As Integer' has the same name as a member used for type 'DefaultInstanceTest1' exposed in a 'My' group. Rename the type or its enclosing namespace.
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "Create", "Dispose", "")> _
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub ConflictWithMember2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "Create", "Dispose", "")> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub

    Private DefaultInstanceTest1 As Integer
End Class

Public Class DefaultInstanceTestBase1
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim members = MyTests.GetMembers()
            Assert.Equal(4, members.Length)

            AssertTheseDeclarationDiagnostics(compilation,
<expected><![CDATA[
BC36015: 'Private DefaultInstanceTest1 As Integer' has the same name as a member used for type 'DefaultInstanceTest1' exposed in a 'My' group. Rename the type or its enclosing namespace.
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "Create", "Dispose", "")> _
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub ConflictWithMember3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "Create", "Dispose", "")> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub

    Private GET_DefaultInstanceTest1 As Integer
End Class

Public Class DefaultInstanceTestBase1
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim members = MyTests.GetMembers()
            Assert.Equal(4, members.Length)

            AssertTheseDeclarationDiagnostics(compilation,
<expected><![CDATA[
BC36015: 'Private GET_DefaultInstanceTest1 As Integer' has the same name as a member used for type 'DefaultInstanceTest1' exposed in a 'My' group. Rename the type or its enclosing namespace.
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "Create", "Dispose", "")> _
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub ConflictWithMember4()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "Create", "Dispose", "")> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub

    Private set_DefaultInstanceTest1 As Integer
End Class

Public Class DefaultInstanceTestBase1
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim members = MyTests.GetMembers()
            Assert.Equal(4, members.Length)

            AssertTheseDeclarationDiagnostics(compilation,
<expected><![CDATA[
BC36015: 'Private set_DefaultInstanceTest1 As Integer' has the same name as a member used for type 'DefaultInstanceTest1' exposed in a 'My' group. Rename the type or its enclosing namespace.
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "Create", "Dispose", "")> _
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub ConflictWithMember5()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "Create", "", "")> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub

    Private set_DefaultInstanceTest1 As Integer
End Class

Public Class DefaultInstanceTestBase1
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim members = MyTests.GetMembers()
            Assert.Equal(7, members.Length)

            AssertTheseDeclarationDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact>
        Public Sub ConflictWithType1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "Create", "Dispose", "")> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub

    Class DefaultInstanceTest1
    End Class
End Class

Public Class DefaultInstanceTestBase1
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim members = MyTests.GetMembers()
            Assert.Equal(4, members.Length)

            AssertTheseDeclarationDiagnostics(compilation,
<expected><![CDATA[
BC36015: 'MyTests.DefaultInstanceTest1' has the same name as a member used for type 'DefaultInstanceTest1' exposed in a 'My' group. Rename the type or its enclosing namespace.
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "Create", "Dispose", "")> _
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub BaseIsNestedAndGeneric()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Class C1(Of S)
    Public Class DefaultInstanceTestBase5
    End Class

    Public Class DefaultInstanceTestBase5(Of T)
    End Class
End Class

Class DefaultInstanceTest5_1
    Inherits C1(Of Byte).DefaultInstanceTestBase5
End Class

Class DefaultInstanceTest5_2
    Inherits C1(Of Byte).DefaultInstanceTestBase5(Of Integer)
End Class

<Microsoft.VisualBasic.MyGroupCollection("c1.DefaultInstanceTestBase5", "Create", "Dispose", "")> _
Public Class MyTests
    Private Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub

End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim expected() As String = {"Sub MyTests..ctor()",
                                        "Function MyTests.Create(Of T)(Instance As T) As T",
                                        "Sub MyTests.Dispose(Of T)(ByRef Instance As T)",
                                        "MyTests.m_DefaultInstanceTest5_1 As DefaultInstanceTest5_1",
                                        "MyTests.m_DefaultInstanceTest5_2 As DefaultInstanceTest5_2",
                                        "Function MyTests.get_DefaultInstanceTest5_1() As DefaultInstanceTest5_1",
                                        "Function MyTests.get_DefaultInstanceTest5_2() As DefaultInstanceTest5_2",
                                        "Sub MyTests.set_DefaultInstanceTest5_1(Value As DefaultInstanceTest5_1)",
                                        "Sub MyTests.set_DefaultInstanceTest5_2(Value As DefaultInstanceTest5_2)",
                                        "Property MyTests.DefaultInstanceTest5_1 As DefaultInstanceTest5_1",
                                        "Property MyTests.DefaultInstanceTest5_2 As DefaultInstanceTest5_2"}

            Dim members = MyTests.GetMembers()
            Assert.Equal(expected.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected(i), members(i).ToTestDisplayString())
            Next

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub ReservedKeywords()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Public Class DefaultInstanceTestBase6
End Class

Class [For]
    Inherits DefaultInstanceTestBase6
End Class

Namespace [While]
    <Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase6", "Redim", "[Erase]", "")> _
    Public Class [Do]
        Private Function [Redim](Of T)(Instance As T) As T
            Return Nothing
        End Function

        Private Sub [Erase](Of T)(ByRef Instance As T)
        End Sub
    End Class
End Namespace
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("While.Do")

            Dim expected() As String = {"Sub [While].Do..ctor()",
                                        "Function [While].Do.Redim(Of T)(Instance As T) As T",
                                        "Sub [While].Do.Erase(Of T)(ByRef Instance As T)",
                                        "[While].Do.m_For As [For]",
                                        "Function [While].Do.get_For() As [For]",
                                        "Sub [While].Do.set_For(Value As [For])",
                                        "Property [While].Do.For As [For]"}

            Dim members = MyTests.GetMembers()
            Assert.Equal(expected.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected(i), members(i).ToTestDisplayString())
            Next

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub ComplexExpressions()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "MyFactory(Of DefaultInstanceTestBase1).Create", "MyFactory(Of DefaultInstanceTestBase1).GetDisposer().Dispose", "")> _
Public Class MyTests
End Class

Class MyFactory(Of S)
    Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Shared Function GetDisposer() As MyFactory(Of S)
        Return Nothing
    End Function

    Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub
End Class

Public Class DefaultInstanceTestBase1
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim members = MyTests.GetMembers()
            Assert.Equal(5, members.Length)

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub SyntaxErrors()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "MyFactory(Of DefaultInstanceTestBase1).", "(Of DefaultInstanceTestBase1).GetDisposer().Dispose", "")> _
Public Class MyTests
End Class

Class MyFactory(Of S)
    Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Shared Function GetDisposer() As MyFactory(Of S)
        Return Nothing
    End Function

    Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub
End Class

Public Class DefaultInstanceTestBase1
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim members = MyTests.GetMembers()
            Assert.Equal(5, members.Length)

            AssertTheseDeclarationDiagnostics(compilation, <expected></expected>)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30035: Syntax error.
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "MyFactory(Of DefaultInstanceTestBase1).", "(Of DefaultInstanceTestBase1).GetDisposer().Dispose", "")> _
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30203: Identifier expected.
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "MyFactory(Of DefaultInstanceTestBase1).", "(Of DefaultInstanceTestBase1).GetDisposer().Dispose", "")> _
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub BindingErrors()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "MyFactory.Create", "MyFactory(Of DefaultInstanceTestBase1).Dispose", "")> _
Public Class MyTests
End Class

Class MyFactory(Of S)
    Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Shared Function GetDisposer() As MyFactory(Of S)
        Return Nothing
    End Function

    Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub
End Class

Public Class DefaultInstanceTestBase1
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("MyTests")

            Dim members = MyTests.GetMembers()
            Assert.Equal(5, members.Length)

            AssertTheseDeclarationDiagnostics(compilation, <expected></expected>)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30469: Reference to a non-shared member requires an object reference.
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "MyFactory.Create", "MyFactory(Of DefaultInstanceTestBase1).Dispose", "")> _
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32042: Too few type arguments to 'MyFactory(Of S)'.
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "MyFactory.Create", "MyFactory(Of DefaultInstanceTestBase1).Dispose", "")> _
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub GroupClassIsNested()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Interface IX

    <Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "Create", "Dispose", "")> _
    Class MyTests
        Private Shared Function Create(Of T)(Instance As T) As T
            Return Nothing
        End Function

        Private Shared Sub Dispose(Of T)(ByRef Instance As T)
            Instance = Nothing
        End Sub
    End Class
End Interface

Public Class DefaultInstanceTestBase1
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests = compilation.GetTypeByMetadataName("IX+MyTests")

            Dim members = MyTests.GetMembers()
            Assert.Equal(7, members.Length)

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        Friend Shared ReadOnly WindowsFormsMyTemplateSource As String =
        <![CDATA[
Option Strict On
Option Explicit On
Option Compare Binary

#Const _MYTYPE = "WindowsForms"

'* Copyright (C) Microsoft Corporation. All Rights Reserved.

#If TARGET = "module" AndAlso _MYTYPE = "" Then
#Const _MYTYPE="Empty"
#End If

#If _MYTYPE = "WindowsForms" Then

#Const _MYFORMS = True
#Const _MYWEBSERVICES = True
#Const _MYUSERTYPE = "Windows"
#Const _MYCOMPUTERTYPE = "Windows"
#Const _MYAPPLICATIONTYPE = "WindowsForms"

#ElseIf _MYTYPE = "WindowsFormsWithCustomSubMain" Then

#Const _MYFORMS = True
#Const _MYWEBSERVICES = True
#Const _MYUSERTYPE = "Windows"
#Const _MYCOMPUTERTYPE = "Windows"
#Const _MYAPPLICATIONTYPE = "Console"

#ElseIf _MYTYPE = "Windows" OrElse _MYTYPE = "" Then

#Const _MYWEBSERVICES = True
#Const _MYUSERTYPE = "Windows"
#Const _MYCOMPUTERTYPE = "Windows"
#Const _MYAPPLICATIONTYPE = "Windows"

#ElseIf _MYTYPE = "Console" Then

#Const _MYWEBSERVICES = True
#Const _MYUSERTYPE = "Windows"
#Const _MYCOMPUTERTYPE = "Windows"
#Const _MYAPPLICATIONTYPE = "Console"

#ElseIf _MYTYPE = "Web" Then

#Const _MYFORMS = False
#Const _MYWEBSERVICES = False
#Const _MYUSERTYPE = "Web"
#Const _MYCOMPUTERTYPE = "Web"

#ElseIf _MYTYPE = "WebControl" Then

#Const _MYFORMS = False
#Const _MYWEBSERVICES = True
#Const _MYUSERTYPE = "Web"
#Const _MYCOMPUTERTYPE = "Web"

#ElseIf _MYTYPE = "Custom" Then

#ElseIf _MYTYPE <> "Empty" Then

#Const _MYTYPE = "Empty"

#End If

#If _MYTYPE <> "Empty" Then

Namespace My

#If _MYAPPLICATIONTYPE = "WindowsForms" OrElse _MYAPPLICATIONTYPE = "Windows" OrElse _MYAPPLICATIONTYPE = "Console" Then

    <Global.System.CodeDom.Compiler.GeneratedCodeAttribute("MyTemplate", "11.0.0.0")> _
    <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> Partial Friend Class MyApplication

#If _MYAPPLICATIONTYPE = "WindowsForms" Then
        Inherits Global.Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase
#If TARGET = "winexe" Then
        <Global.System.STAThread(), Global.System.Diagnostics.DebuggerHidden(), Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Advanced)> _
        Friend Shared Sub Main(ByVal Args As String())
            Try
               Global.System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(MyApplication.UseCompatibleTextRendering())
            Finally
            End Try               
            My.Application.Run(Args)
        End Sub
#End If

#ElseIf _MYAPPLICATIONTYPE = "Windows" Then
        Inherits Global.Microsoft.VisualBasic.ApplicationServices.ApplicationBase
#ElseIf _MYAPPLICATIONTYPE = "Console" Then
        Inherits Global.Microsoft.VisualBasic.ApplicationServices.ConsoleApplicationBase	
#End If '_MYAPPLICATIONTYPE = "WindowsForms"

    End Class

#End If '#If _MYAPPLICATIONTYPE = "WindowsForms" Or _MYAPPLICATIONTYPE = "Windows" or _MYAPPLICATIONTYPE = "Console"

#If _MYCOMPUTERTYPE <> "" Then

    <Global.System.CodeDom.Compiler.GeneratedCodeAttribute("MyTemplate", "11.0.0.0")> _
    <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> Partial Friend Class MyComputer

#If _MYCOMPUTERTYPE = "Windows" Then
        Inherits Global.Microsoft.VisualBasic.Devices.Computer
#ElseIf _MYCOMPUTERTYPE = "Web" Then
        Inherits Global.Microsoft.VisualBasic.Devices.ServerComputer
#End If
        <Global.System.Diagnostics.DebuggerHidden()> _
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
        Public Sub New()
            MyBase.New()
        End Sub
    End Class
#End If

    <Global.Microsoft.VisualBasic.HideModuleName()> _
    <Global.System.CodeDom.Compiler.GeneratedCodeAttribute("MyTemplate", "11.0.0.0")> _
    Friend Module MyProject

#If _MYCOMPUTERTYPE <> "" Then
        <Global.System.ComponentModel.Design.HelpKeyword("My.Computer")> _
        Friend ReadOnly Property Computer() As MyComputer
            <Global.System.Diagnostics.DebuggerHidden()> _
            Get
                Return m_ComputerObjectProvider.GetInstance()
            End Get
        End Property

        Private ReadOnly m_ComputerObjectProvider As New ThreadSafeObjectProvider(Of MyComputer)
#End If

#If _MYAPPLICATIONTYPE = "Windows" Or _MYAPPLICATIONTYPE = "WindowsForms" Or _MYAPPLICATIONTYPE = "Console" Then
        <Global.System.ComponentModel.Design.HelpKeyword("My.Application")> _
        Friend ReadOnly Property Application() As MyApplication
            <Global.System.Diagnostics.DebuggerHidden()> _
            Get
                Return m_AppObjectProvider.GetInstance()
            End Get
        End Property
        Private ReadOnly m_AppObjectProvider As New ThreadSafeObjectProvider(Of MyApplication)
#End If

#If _MYUSERTYPE = "Windows" Then
        <Global.System.ComponentModel.Design.HelpKeyword("My.User")> _
        Friend ReadOnly Property User() As Global.Microsoft.VisualBasic.ApplicationServices.User
            <Global.System.Diagnostics.DebuggerHidden()> _
            Get
                Return m_UserObjectProvider.GetInstance()
            End Get
        End Property
        Private ReadOnly m_UserObjectProvider As New ThreadSafeObjectProvider(Of Global.Microsoft.VisualBasic.ApplicationServices.User)
#ElseIf _MYUSERTYPE = "Web" Then
        <Global.System.ComponentModel.Design.HelpKeyword("My.User")> _
        Friend ReadOnly Property User() As Global.Microsoft.VisualBasic.ApplicationServices.WebUser
            <Global.System.Diagnostics.DebuggerHidden()> _
            Get
                Return m_UserObjectProvider.GetInstance()
            End Get
        End Property
        Private ReadOnly m_UserObjectProvider As New ThreadSafeObjectProvider(Of Global.Microsoft.VisualBasic.ApplicationServices.WebUser)
#End If

#If _MYFORMS = True Then

#Const STARTUP_MY_FORM_FACTORY = "My.MyProject.Forms"

        <Global.System.ComponentModel.Design.HelpKeyword("My.Forms")> _
        Friend ReadOnly Property Forms() As MyForms
            <Global.System.Diagnostics.DebuggerHidden()> _
            Get
                Return m_MyFormsObjectProvider.GetInstance()
            End Get
        End Property

        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
        <Global.Microsoft.VisualBasic.MyGroupCollection("System.Windows.Forms.Form", "Create__Instance__", "Dispose__Instance__", "My.MyProject.Forms")> _
        Friend NotInheritable Class MyForms
            <Global.System.Diagnostics.DebuggerHidden()> _
            Private Shared Function Create__Instance__(Of T As {New, Global.System.Windows.Forms.Form})(ByVal Instance As T) As T
                If Instance Is Nothing OrElse Instance.IsDisposed Then
                    If m_FormBeingCreated IsNot Nothing Then
                        If m_FormBeingCreated.ContainsKey(GetType(T)) = True Then
                            Throw New Global.System.InvalidOperationException(Global.Microsoft.VisualBasic.CompilerServices.Utils.GetResourceString("WinForms_RecursiveFormCreate"))
                        End If
                    Else
                        m_FormBeingCreated = New Global.System.Collections.Hashtable()
                    End If
                    m_FormBeingCreated.Add(GetType(T), Nothing)
                    Try
                        Return New T()
                    Catch ex As Global.System.Reflection.TargetInvocationException When ex.InnerException IsNot Nothing
                        Dim BetterMessage As String = Global.Microsoft.VisualBasic.CompilerServices.Utils.GetResourceString("WinForms_SeeInnerException", ex.InnerException.Message)
                        Throw New Global.System.InvalidOperationException(BetterMessage, ex.InnerException)
                    Finally
                        m_FormBeingCreated.Remove(GetType(T))
                    End Try
                Else
                    Return Instance
                End If
            End Function

            <Global.System.Diagnostics.DebuggerHidden()> _
            Private Sub Dispose__Instance__(Of T As Global.System.Windows.Forms.Form)(ByRef instance As T)
                instance.Dispose()
                instance = Nothing
            End Sub

            <Global.System.Diagnostics.DebuggerHidden()> _
            <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
            Public Sub New()
               MyBase.New()
            End Sub

            <Global.System.ThreadStatic()> Private Shared m_FormBeingCreated As Global.System.Collections.Hashtable

            <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)> Public Overrides Function Equals(ByVal o As Object) As Boolean
                Return MyBase.Equals(o)
            End Function
            <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)> Public Overrides Function GetHashCode() As Integer
                Return MyBase.GetHashCode
            End Function
            <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)> _
            Friend Overloads Function [GetType]() As Global.System.Type
                Return GetType(MyForms)
            End Function
            <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)> Public Overrides Function ToString() As String
                Return MyBase.ToString
            End Function
        End Class

        Private m_MyFormsObjectProvider As New ThreadSafeObjectProvider(Of MyForms)

#End If

#If _MYWEBSERVICES = True Then

        <Global.System.ComponentModel.Design.HelpKeyword("My.WebServices")> _
        Friend ReadOnly Property WebServices() As MyWebServices
            <Global.System.Diagnostics.DebuggerHidden()> _
            Get
                Return m_MyWebServicesObjectProvider.GetInstance()
            End Get
        End Property

        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
        <Global.Microsoft.VisualBasic.MyGroupCollection("System.Web.Services.Protocols.SoapHttpClientProtocol", "Create__Instance__", "Dispose__Instance__", "")> _
        Friend NotInheritable Class MyWebServices

            <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never), Global.System.Diagnostics.DebuggerHidden()> _
            Public Overrides Function Equals(ByVal o As Object) As Boolean
                Return MyBase.Equals(o)
            End Function
            <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never), Global.System.Diagnostics.DebuggerHidden()> _
            Public Overrides Function GetHashCode() As Integer
                Return MyBase.GetHashCode
            End Function
            <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never), Global.System.Diagnostics.DebuggerHidden()> _
            Friend Overloads Function [GetType]() As Global.System.Type
                Return GetType(MyWebServices)
            End Function
            <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never), Global.System.Diagnostics.DebuggerHidden()> _
            Public Overrides Function ToString() As String
                Return MyBase.ToString
            End Function

            <Global.System.Diagnostics.DebuggerHidden()> _
            Private Shared Function Create__Instance__(Of T As {New})(ByVal instance As T) As T
                If instance Is Nothing Then
                    Return New T()
                Else
                    Return instance
                End If
            End Function

            <Global.System.Diagnostics.DebuggerHidden()> _
            Private Sub Dispose__Instance__(Of T)(ByRef instance As T)
                instance = Nothing
            End Sub

            <Global.System.Diagnostics.DebuggerHidden()> _
            <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
            Public Sub New()
                MyBase.New()
            End Sub
        End Class

        Private ReadOnly m_MyWebServicesObjectProvider As New ThreadSafeObjectProvider(Of MyWebServices)
#End If

#If _MYTYPE = "Web" Then

        <Global.System.ComponentModel.Design.HelpKeyword("My.Request")> _
        Friend ReadOnly Property Request() As Global.System.Web.HttpRequest
            <Global.System.Diagnostics.DebuggerHidden()> _
            Get
                Dim CurrentContext As Global.System.Web.HttpContext = Global.System.Web.HttpContext.Current
                If CurrentContext IsNot Nothing Then
                    Return CurrentContext.Request
                End If
                Return Nothing
            End Get
        End Property

        <Global.System.ComponentModel.Design.HelpKeyword("My.Response")> _
        Friend ReadOnly Property Response() As Global.System.Web.HttpResponse
            <Global.System.Diagnostics.DebuggerHidden()> _
            Get
                Dim CurrentContext As Global.System.Web.HttpContext = Global.System.Web.HttpContext.Current
                If CurrentContext IsNot Nothing Then
                    Return CurrentContext.Response
                End If
                Return Nothing
            End Get
        End Property

        <Global.System.ComponentModel.Design.HelpKeyword("My.Application.Log")> _
        Friend ReadOnly Property Log() As Global.Microsoft.VisualBasic.Logging.AspLog
            <Global.System.Diagnostics.DebuggerHidden()> _
            Get
                Return m_LogObjectProvider.GetInstance()
            End Get
        End Property

        Private ReadOnly m_LogObjectProvider As New ThreadSafeObjectProvider(Of Global.Microsoft.VisualBasic.Logging.AspLog)

#End If  '_MYTYPE="Web"

        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
        <Global.System.Runtime.InteropServices.ComVisible(False)> _
        Friend NotInheritable Class ThreadSafeObjectProvider(Of T As New)
            Friend ReadOnly Property GetInstance() As T
#If TARGET = "library" Then
                <Global.System.Diagnostics.DebuggerHidden()> _
                Get
                    Dim Value As T = m_Context.Value
                    If Value Is Nothing Then
                        Value = New T
                        m_Context.Value() = Value
                    End If
                    Return Value
                End Get
#Else
                <Global.System.Diagnostics.DebuggerHidden()> _
                Get
                    If m_ThreadStaticValue Is Nothing Then m_ThreadStaticValue = New T
                    Return m_ThreadStaticValue
                End Get
#End If
            End Property

            <Global.System.Diagnostics.DebuggerHidden()> _
            <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
            Public Sub New()
                MyBase.New()
            End Sub

#If TARGET = "library" Then
            Private ReadOnly m_Context As New Global.Microsoft.VisualBasic.MyServices.Internal.ContextValue(Of T)
#Else
            <Global.System.Runtime.CompilerServices.CompilerGenerated(), Global.System.ThreadStatic()> Private Shared m_ThreadStaticValue As T
#End If
        End Class
    End Module
End Namespace
#End If
]]>.Value

        Friend Shared ReadOnly WindowsFormsMyTemplateTree As SyntaxTree = ParseTemplateTree(WindowsFormsMyTemplateSource, path:="17d14f5c-a337-4978-8281-53493378c107.vb") ' The name used by native compiler

        <Fact>
        Public Sub DefaultInstanceAlias1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="types.vb"><![CDATA[
Imports System

Namespace Global
    Public Class Form2
        Inherits Windows.Forms.Form
        Public Property P2 As Integer
        Default Property P3(x As Integer) As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property

        Event E2 As System.Action
        Public Shared F2 As Integer
    End Class
End Namespace

Namespace Global.WindowsApplication1
    Public Class Form1
        Inherits Windows.Forms.Form

        Public Property P1 As Integer
        Event E1 As System.Action
        Public Shared F1 As Integer
    End Class
End Namespace
    ]]></file>
    <file name="Test1.vb"><![CDATA[
Imports System

Namespace Global
    Module Module1
        Sub Test1()
            Dim x1 = WindowsApplication1.Form1
            Dim x2 = WindowsApplication1.Form1.P1
            Dim x3 = Form2
            Dim x4 = Form2.P2

            Dim x5 = WindowsApplication1.My.MyProject.Forms.Form1
            Dim x6 = WindowsApplication1.My.MyProject.Forms.Form2

            WindowsApplication1.Form1 = x5
            WindowsApplication1.Form1.P1 = 1
            Form2 = x6
            Form2.P2 = 2

            Dim x7 = Form2(1)
            Form2(1) = 3

            AddHandler WindowsApplication1.Form1.E1, AddressOf Test1
            RemoveHandler WindowsApplication1.Form1.E1, AddressOf Test1
            AddHandler Form2.E2, AddressOf Test1
            RemoveHandler Form2.E2, AddressOf Test1

            Dim x8 = WindowsApplication1.Form1.F1
            Dim x9 = Form2.F2
        End Sub
    End Module
End Namespace
    ]]></file>
    <file name="Test2.vb"><![CDATA[
Namespace Global.WindowsApplication1
    Module Module1
        Sub Test15()
            Dim x108 = Form2()
        End Sub
        Sub Test16(x107 As Integer)
            Form2() = x107
        End Sub
        Sub Test17()
            Call Form2(1)
        End Sub
    End Module
End Namespace
    ]]></file>
    <file name="Test3.vb"><![CDATA[
Namespace Global.WindowsApplication1
    Module Module2
        Sub Test1()
            Dim x101 = WindowsApplication1.Form1
        End Sub
        Sub Test2()
            Dim x102 = WindowsApplication1.Form1.P1 'BIND2:"WindowsApplication1.Form1"
        End Sub
        Sub Test3()
            Dim x103 = Form1
        End Sub
        Sub Test4()
            Dim x104 = Form1.P1
        End Sub
        Sub Test5()
            Dim x105 = Form2
        End Sub
        Sub Test6()
            Dim x106 = Form2.P2
        End Sub
        Sub Test7(x101 As WindowsApplication1.Form1)
            WindowsApplication1.Form1 = x101
        End Sub
        Sub Test8(x102 As Integer)
            WindowsApplication1.Form1.P1 = x102
        End Sub
        Sub Test9(x103 As Form1)
            Form1 = x103
        End Sub
        Sub Test10(x104 As Integer)
            Form1.P1 = x104
        End Sub
        Sub Test11(x105 As Form2)
            Form2 = x105
        End Sub
        Sub Test12(x106 As Integer)
            Form2.P2 = x106
        End Sub
        Sub Test13()
            Dim x107 = Form2(1)
        End Sub
        Sub Test14(x107 As Integer)
            Form2(1) = x107
        End Sub
        Sub Test18()
            AddHandler WindowsApplication1.Form1.E1, AddressOf Test18
            RemoveHandler WindowsApplication1.Form1.E1, AddressOf Test18
            AddHandler Form1.E1, AddressOf Test18
            RemoveHandler Form1.E1, AddressOf Test18
            AddHandler Form2.E2, AddressOf Test18
            RemoveHandler Form2.E2, AddressOf Test18
        End Sub

        Sub Test19()
            Dim x109 = WindowsApplication1.Form1.F1 'BIND1:"WindowsApplication1.Form1"
        End Sub
        Sub Test20()
            Dim x110 = Form1.F1
        End Sub
        Sub Test21()
            Dim x111 = Form2.F2
        End Sub
    End Module
End Namespace
    ]]></file>
    <file name="Form.vb"><![CDATA[
Namespace Global.System.Windows.Forms
    Public Class Form
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub

        Public ReadOnly Property IsDisposed As Boolean
            Get
                Return False
            End Get
        End Property
    End Class
End Namespace
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemRef},
                                                                                     TestOptions.ReleaseDll.WithRootNamespace("WindowsApplication1"))

            compilation.MyTemplate = WindowsFormsMyTemplateTree

            AssertTheseDiagnostics(compilation,
<expected>
BC30109: 'Form1' is a class type and cannot be used as an expression.
            Dim x1 = WindowsApplication1.Form1
                     ~~~~~~~~~~~~~~~~~~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            Dim x2 = WindowsApplication1.Form1.P1
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30109: 'Form2' is a class type and cannot be used as an expression.
            Dim x3 = Form2
                     ~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            Dim x4 = Form2.P2
                     ~~~~~~~~
BC30109: 'Form1' is a class type and cannot be used as an expression.
            WindowsApplication1.Form1 = x5
            ~~~~~~~~~~~~~~~~~~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            WindowsApplication1.Form1.P1 = 1
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30109: 'Form2' is a class type and cannot be used as an expression.
            Form2 = x6
            ~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            Form2.P2 = 2
            ~~~~~~~~
BC30109: 'Form2' is a class type and cannot be used as an expression.
            Dim x7 = Form2(1)
                     ~~~~~
BC30109: 'Form2' is a class type and cannot be used as an expression.
            Form2(1) = 3
            ~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            AddHandler WindowsApplication1.Form1.E1, AddressOf Test1
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            RemoveHandler WindowsApplication1.Form1.E1, AddressOf Test1
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            AddHandler Form2.E2, AddressOf Test1
                       ~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            RemoveHandler Form2.E2, AddressOf Test1
                          ~~~~~~~~
BC30455: Argument not specified for parameter 'x' of 'Public Default Property P3(x As Integer) As Integer'.
            Dim x108 = Form2()
                       ~~~~~
BC30455: Argument not specified for parameter 'x' of 'Public Default Property P3(x As Integer) As Integer'.
            Form2() = x107
            ~~~~~
BC30109: 'Form2' is a class type and cannot be used as an expression.
            Call Form2(1)
                 ~~~~~
</expected>)

            compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemRef},
                                                                                 TestOptions.ReleaseDll.WithRootNamespace("WindowsApplication1"))

            compilation = compilation.AddSyntaxTrees(VisualBasicSyntaxTree.ParseText(WindowsFormsMyTemplateSource))

            compilation.MyTemplate = Nothing

            AssertTheseDiagnostics(compilation,
<expected>
BC30109: 'Form1' is a class type and cannot be used as an expression.
            Dim x1 = WindowsApplication1.Form1
                     ~~~~~~~~~~~~~~~~~~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            Dim x2 = WindowsApplication1.Form1.P1
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30109: 'Form2' is a class type and cannot be used as an expression.
            Dim x3 = Form2
                     ~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            Dim x4 = Form2.P2
                     ~~~~~~~~
BC30109: 'Form1' is a class type and cannot be used as an expression.
            WindowsApplication1.Form1 = x5
            ~~~~~~~~~~~~~~~~~~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            WindowsApplication1.Form1.P1 = 1
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30109: 'Form2' is a class type and cannot be used as an expression.
            Form2 = x6
            ~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            Form2.P2 = 2
            ~~~~~~~~
BC30109: 'Form2' is a class type and cannot be used as an expression.
            Dim x7 = Form2(1)
                     ~~~~~
BC30109: 'Form2' is a class type and cannot be used as an expression.
            Form2(1) = 3
            ~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            AddHandler WindowsApplication1.Form1.E1, AddressOf Test1
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            RemoveHandler WindowsApplication1.Form1.E1, AddressOf Test1
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            AddHandler Form2.E2, AddressOf Test1
                       ~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            RemoveHandler Form2.E2, AddressOf Test1
                          ~~~~~~~~
BC30109: 'Form2' is a class type and cannot be used as an expression.
            Dim x108 = Form2()
                       ~~~~~
BC30109: 'Form2' is a class type and cannot be used as an expression.
            Form2() = x107
            ~~~~~
BC30109: 'Form2' is a class type and cannot be used as an expression.
            Call Form2(1)
                 ~~~~~
BC30109: 'Form1' is a class type and cannot be used as an expression.
            Dim x101 = WindowsApplication1.Form1
                       ~~~~~~~~~~~~~~~~~~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            Dim x102 = WindowsApplication1.Form1.P1 'BIND2:"WindowsApplication1.Form1"
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30109: 'Form1' is a class type and cannot be used as an expression.
            Dim x103 = Form1
                       ~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            Dim x104 = Form1.P1
                       ~~~~~~~~
BC30109: 'Form2' is a class type and cannot be used as an expression.
            Dim x105 = Form2
                       ~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            Dim x106 = Form2.P2
                       ~~~~~~~~
BC30109: 'Form1' is a class type and cannot be used as an expression.
            WindowsApplication1.Form1 = x101
            ~~~~~~~~~~~~~~~~~~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            WindowsApplication1.Form1.P1 = x102
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30109: 'Form1' is a class type and cannot be used as an expression.
            Form1 = x103
            ~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            Form1.P1 = x104
            ~~~~~~~~
BC30109: 'Form2' is a class type and cannot be used as an expression.
            Form2 = x105
            ~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            Form2.P2 = x106
            ~~~~~~~~
BC30109: 'Form2' is a class type and cannot be used as an expression.
            Dim x107 = Form2(1)
                       ~~~~~
BC30109: 'Form2' is a class type and cannot be used as an expression.
            Form2(1) = x107
            ~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            AddHandler WindowsApplication1.Form1.E1, AddressOf Test18
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            RemoveHandler WindowsApplication1.Form1.E1, AddressOf Test18
                          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            AddHandler Form1.E1, AddressOf Test18
                       ~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            RemoveHandler Form1.E1, AddressOf Test18
                          ~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            AddHandler Form2.E2, AddressOf Test18
                       ~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
            RemoveHandler Form2.E2, AddressOf Test18
                          ~~~~~~~~
</expected>)

            compilationDef.Elements()(1).Remove()
            compilationDef.Elements()(1).Remove()
            compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemRef},
                                                                                 TestOptions.ReleaseDll.WithRootNamespace("WindowsApplication1"))

            compilation.MyTemplate = WindowsFormsMyTemplateTree

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "Test3.vb").Single()
            Dim semanticInfo1 As SemanticInfoSummary
            Dim semanticInfo2 As SemanticInfoSummary

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "Test3.vb", 1)
            Dim node2 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "Test3.vb", 2)

            Assert.NotEqual(node1, node2)

            semanticInfo1 = CompilationUtils.GetSemanticInfoSummary(semanticModel, node1)
            semanticInfo2 = CompilationUtils.GetSemanticInfoSummary(semanticModel, node2)

            Assert.Equal("WindowsApplication1.Form1", semanticInfo1.Symbol.ToTestDisplayString())
            Assert.Equal("Property WindowsApplication1.My.MyProject.MyForms.Form1 As WindowsApplication1.Form1", semanticInfo2.Symbol.ToTestDisplayString())
            Assert.Equal(semanticInfo1.Type, semanticInfo2.Type)
            Assert.Equal(semanticInfo1.ImplicitConversion, semanticInfo2.ImplicitConversion)
            Assert.Equal(semanticInfo1.ConvertedType, semanticInfo2.ConvertedType)
            Assert.Equal(semanticInfo1.ConstantValue, semanticInfo2.ConstantValue)
            Assert.Equal(semanticInfo1.CandidateReason, semanticInfo2.CandidateReason)
            Assert.Equal(semanticInfo1.CandidateSymbols.Length, semanticInfo2.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo2.CandidateSymbols.Length)
            Assert.Equal(semanticInfo1.AllSymbols.Length, semanticInfo2.AllSymbols.Length)
            Assert.Equal(1, semanticInfo2.AllSymbols.Length)
            Assert.Equal(0, semanticInfo1.MemberGroup.Length)
            Assert.Equal(0, semanticInfo2.MemberGroup.Length)

            verifier.VerifyIL("WindowsApplication1.Module2.Test1",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  call       "Function WindowsApplication1.My.MyProject.get_Forms() As WindowsApplication1.My.MyProject.MyForms"
  IL_0005:  callvirt   "Function WindowsApplication1.My.MyProject.MyForms.get_Form1() As WindowsApplication1.Form1"
  IL_000a:  pop
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("WindowsApplication1.Module2.Test2",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  call       "Function WindowsApplication1.My.MyProject.get_Forms() As WindowsApplication1.My.MyProject.MyForms"
  IL_0005:  callvirt   "Function WindowsApplication1.My.MyProject.MyForms.get_Form1() As WindowsApplication1.Form1"
  IL_000a:  callvirt   "Function WindowsApplication1.Form1.get_P1() As Integer"
  IL_000f:  pop
  IL_0010:  ret
}
]]>)

            verifier.VerifyIL("WindowsApplication1.Module2.Test3",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  call       "Function WindowsApplication1.My.MyProject.get_Forms() As WindowsApplication1.My.MyProject.MyForms"
  IL_0005:  callvirt   "Function WindowsApplication1.My.MyProject.MyForms.get_Form1() As WindowsApplication1.Form1"
  IL_000a:  pop
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("WindowsApplication1.Module2.Test4",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  call       "Function WindowsApplication1.My.MyProject.get_Forms() As WindowsApplication1.My.MyProject.MyForms"
  IL_0005:  callvirt   "Function WindowsApplication1.My.MyProject.MyForms.get_Form1() As WindowsApplication1.Form1"
  IL_000a:  callvirt   "Function WindowsApplication1.Form1.get_P1() As Integer"
  IL_000f:  pop
  IL_0010:  ret
}
]]>)

            verifier.VerifyIL("WindowsApplication1.Module2.Test5",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  call       "Function WindowsApplication1.My.MyProject.get_Forms() As WindowsApplication1.My.MyProject.MyForms"
  IL_0005:  callvirt   "Function WindowsApplication1.My.MyProject.MyForms.get_Form2() As Form2"
  IL_000a:  pop
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("WindowsApplication1.Module2.Test6",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  call       "Function WindowsApplication1.My.MyProject.get_Forms() As WindowsApplication1.My.MyProject.MyForms"
  IL_0005:  callvirt   "Function WindowsApplication1.My.MyProject.MyForms.get_Form2() As Form2"
  IL_000a:  callvirt   "Function Form2.get_P2() As Integer"
  IL_000f:  pop
  IL_0010:  ret
}
]]>)

            verifier.VerifyIL("WindowsApplication1.Module2.Test7",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  call       "Function WindowsApplication1.My.MyProject.get_Forms() As WindowsApplication1.My.MyProject.MyForms"
  IL_0005:  ldarg.0
  IL_0006:  callvirt   "Sub WindowsApplication1.My.MyProject.MyForms.set_Form1(WindowsApplication1.Form1)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("WindowsApplication1.Module2.Test8",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  call       "Function WindowsApplication1.My.MyProject.get_Forms() As WindowsApplication1.My.MyProject.MyForms"
  IL_0005:  callvirt   "Function WindowsApplication1.My.MyProject.MyForms.get_Form1() As WindowsApplication1.Form1"
  IL_000a:  ldarg.0
  IL_000b:  callvirt   "Sub WindowsApplication1.Form1.set_P1(Integer)"
  IL_0010:  ret
}
]]>)

            verifier.VerifyIL("WindowsApplication1.Module2.Test9",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  call       "Function WindowsApplication1.My.MyProject.get_Forms() As WindowsApplication1.My.MyProject.MyForms"
  IL_0005:  ldarg.0
  IL_0006:  callvirt   "Sub WindowsApplication1.My.MyProject.MyForms.set_Form1(WindowsApplication1.Form1)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("WindowsApplication1.Module2.Test10",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  call       "Function WindowsApplication1.My.MyProject.get_Forms() As WindowsApplication1.My.MyProject.MyForms"
  IL_0005:  callvirt   "Function WindowsApplication1.My.MyProject.MyForms.get_Form1() As WindowsApplication1.Form1"
  IL_000a:  ldarg.0
  IL_000b:  callvirt   "Sub WindowsApplication1.Form1.set_P1(Integer)"
  IL_0010:  ret
}
]]>)

            verifier.VerifyIL("WindowsApplication1.Module2.Test11",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  call       "Function WindowsApplication1.My.MyProject.get_Forms() As WindowsApplication1.My.MyProject.MyForms"
  IL_0005:  ldarg.0
  IL_0006:  callvirt   "Sub WindowsApplication1.My.MyProject.MyForms.set_Form2(Form2)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("WindowsApplication1.Module2.Test12",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  call       "Function WindowsApplication1.My.MyProject.get_Forms() As WindowsApplication1.My.MyProject.MyForms"
  IL_0005:  callvirt   "Function WindowsApplication1.My.MyProject.MyForms.get_Form2() As Form2"
  IL_000a:  ldarg.0
  IL_000b:  callvirt   "Sub Form2.set_P2(Integer)"
  IL_0010:  ret
}
]]>)

            verifier.VerifyIL("WindowsApplication1.Module2.Test13",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  call       "Function WindowsApplication1.My.MyProject.get_Forms() As WindowsApplication1.My.MyProject.MyForms"
  IL_0005:  callvirt   "Function WindowsApplication1.My.MyProject.MyForms.get_Form2() As Form2"
  IL_000a:  ldc.i4.1
  IL_000b:  callvirt   "Function Form2.get_P3(Integer) As Integer"
  IL_0010:  pop
  IL_0011:  ret
}
]]>)

            verifier.VerifyIL("WindowsApplication1.Module2.Test14",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  3
  IL_0000:  call       "Function WindowsApplication1.My.MyProject.get_Forms() As WindowsApplication1.My.MyProject.MyForms"
  IL_0005:  callvirt   "Function WindowsApplication1.My.MyProject.MyForms.get_Form2() As Form2"
  IL_000a:  ldc.i4.1
  IL_000b:  ldarg.0
  IL_000c:  callvirt   "Sub Form2.set_P3(Integer, Integer)"
  IL_0011:  ret
}
]]>)

            verifier.VerifyIL("WindowsApplication1.Module2.Test19",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldsfld     "WindowsApplication1.Form1.F1 As Integer"
  IL_0005:  pop
  IL_0006:  ret
}
]]>)

            verifier.VerifyIL("WindowsApplication1.Module2.Test20",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldsfld     "WindowsApplication1.Form1.F1 As Integer"
  IL_0005:  pop
  IL_0006:  ret
}
]]>)

            verifier.VerifyIL("WindowsApplication1.Module2.Test21",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldsfld     "Form2.F2 As Integer"
  IL_0005:  pop
  IL_0006:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub DefaultInstancePropertyCycle()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Sub Main()
        Dim x = DefaultInstanceTest1
    End Sub
End Module

Public Class DefaultInstanceTest
    Sub Close()
    End Sub

    Public F1 As Integer
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTest

    Public Factory As MyTests
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            compilation.MyTemplate = ParseTemplateTree(
            <![CDATA[
Imports System

<Microsoft.VisualBasic.MyGroupCollection("defaultInstanceteSt", "Create", "Dispose", "DefaultInstanceTest1.Factory")> _
Public NotInheritable Class MyTests
    Private Shared Function Create(Of T As {New, DefaultInstanceTest}) _
        (Instance As T) As T
        If Instance Is Nothing Then
            Return New T()
        Else
            Return Instance
        End If
    End Function

    Private Shared Sub Dispose(Of T As DefaultInstanceTest)(ByRef Instance As T)
        Instance.Close()
        Instance = Nothing
    End Sub

End Class
]]>.Value)

            AssertTheseDiagnostics(compilation,
<expected>
BC30109: 'DefaultInstanceTest1' is a class type and cannot be used as an expression.
        Dim x = DefaultInstanceTest1
                ~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub DefaultInstancePropertyAmbiguity1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Sub Main()
        Dim x = DefaultInstanceTest1
    End Sub
End Module

Public Class DefaultInstanceTest
    Sub Close()
    End Sub

    Public F1 As Integer
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTest

    Public Factory As MyTests
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            compilation.MyTemplate = ParseTemplateTree(
            <![CDATA[
Imports System

<Microsoft.VisualBasic.MyGroupCollection("defaultInstanceteSt", "Create", "Dispose", "Factory.MyTests")> _
Public NotInheritable Class MyTests
    Private Shared Function Create(Of T As {New, DefaultInstanceTest}) _
        (Instance As T) As T
        If Instance Is Nothing Then
            Return New T()
        Else
            Return Instance
        End If
    End Function

    Private Shared Sub Dispose(Of T As DefaultInstanceTest)(ByRef Instance As T)
        Instance.Close()
        Instance = Nothing
    End Sub
End Class

<Microsoft.VisualBasic.MyGroupCollection("defaultInstanceteSt", "Create", "Dispose", "Factory.MyTests")> _
Public NotInheritable Class MyTests1
    Private Shared Function Create(Of T As {New, DefaultInstanceTest}) _
        (Instance As T) As T
        If Instance Is Nothing Then
            Return New T()
        Else
            Return Instance
        End If
    End Function

    Private Shared Sub Dispose(Of T As DefaultInstanceTest)(ByRef Instance As T)
        Instance.Close()
        Instance = Nothing
    End Sub
End Class

Module Factory
    Public Mytests As MyTests
End Module
]]>.Value)

            AssertTheseDiagnostics(compilation,
<expected>
BC30109: 'DefaultInstanceTest1' is a class type and cannot be used as an expression.
        Dim x = DefaultInstanceTest1
                ~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub DefaultInstancePropertyAmbiguity2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Sub Main()
        Dim x = DefaultInstanceTest1
    End Sub
End Module

Public Class DefaultInstanceTest
    Sub Close()
    End Sub

    Public F1 As Integer
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTest

    Public Factory As MyTests
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            compilation.MyTemplate = ParseTemplateTree(
            <![CDATA[
Imports System

<Microsoft.VisualBasic.MyGroupCollection("defaultInstanceteSt", "Create", "Dispose", "Factory.MyTests")> _
Public NotInheritable Class MyTests
    Private Shared Function Create(Of T As {New, DefaultInstanceTest}) _
        (Instance As T) As T
        If Instance Is Nothing Then
            Return New T()
        Else
            Return Instance
        End If
    End Function

    Private Shared Sub Dispose(Of T As DefaultInstanceTest)(ByRef Instance As T)
        Instance.Close()
        Instance = Nothing
    End Sub
End Class

<Microsoft.VisualBasic.MyGroupCollection("defaultInstanceteSt", "Create", "Dispose", "Factory.MyTests")> _
Public NotInheritable Class MyTests1
    Private Shared Function Create(Of T As {New, DefaultInstanceTest}) _
        (Instance As T) As T
        If Instance Is Nothing Then
            Return New T()
        Else
            Return Instance
        End If
    End Function

    Private Shared Sub Dispose(Of T As DefaultInstanceTest)(ByRef Instance As T)
        Instance.Close()
        Instance = Nothing
    End Sub
End Class

<Microsoft.VisualBasic.MyGroupCollection("defaultInstanceteSt", "Create", "Dispose", "Factory.MyTests")> _
Public NotInheritable Class MyTests2
    Private Shared Function Create(Of T As {New, DefaultInstanceTest}) _
        (Instance As T) As T
        If Instance Is Nothing Then
            Return New T()
        Else
            Return Instance
        End If
    End Function

    Private Shared Sub Dispose(Of T As DefaultInstanceTest)(ByRef Instance As T)
        Instance.Close()
        Instance = Nothing
    End Sub
End Class

Module Factory
    Public Mytests As MyTests
End Module
]]>.Value)

            AssertTheseDiagnostics(compilation,
<expected>
BC30109: 'DefaultInstanceTest1' is a class type and cannot be used as an expression.
        Dim x = DefaultInstanceTest1
                ~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub DefaultInstancePropertyInvalidSyntax1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Sub Main()
        Dim x = DefaultInstanceTest1
    End Sub
End Module

Public Class DefaultInstanceTest
    Sub Close()
    End Sub

    Public F1 As Integer
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTest
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            compilation.MyTemplate = ParseTemplateTree(
            <![CDATA[
Imports System

<Microsoft.VisualBasic.MyGroupCollection("defaultInstanceteSt", "Create", "Dispose", "Factory.+MyTests")> _
Public NotInheritable Class MyTests
    Private Shared Function Create(Of T As {New, DefaultInstanceTest}) _
        (Instance As T) As T
        If Instance Is Nothing Then
            Return New T()
        Else
            Return Instance
        End If
    End Function

    Private Shared Sub Dispose(Of T As DefaultInstanceTest)(ByRef Instance As T)
        Instance.Close()
        Instance = Nothing
    End Sub
End Class

Module Factory
    Public Mytests As MyTests
End Module
]]>.Value)

            AssertTheseDiagnostics(compilation,
<expected>
BC30109: 'DefaultInstanceTest1' is a class type and cannot be used as an expression.
        Dim x = DefaultInstanceTest1
                ~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub DefaultInstancePropertyInvalidSyntax2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Sub Main()
        Dim x = DefaultInstanceTest1
    End Sub
End Module

Public Class DefaultInstanceTest
    Sub Close()
    End Sub

    Public F1 As Integer
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTest
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            compilation.MyTemplate = ParseTemplateTree(
            <![CDATA[
Imports System

<Microsoft.VisualBasic.MyGroupCollection("defaultInstanceteSt", "Create", "Dispose", "Factory.MyTests.DefaultInstanceTest1 : System.Console.WriteLine()")> _
Public NotInheritable Class MyTests
    Private Shared Function Create(Of T As {New, DefaultInstanceTest}) _
        (Instance As T) As T
        If Instance Is Nothing Then
            Return New T()
        Else
            Return Instance
        End If
    End Function

    Private Shared Sub Dispose(Of T As DefaultInstanceTest)(ByRef Instance As T)
        Instance.Close()
        Instance = Nothing
    End Sub
End Class

Module Factory
    Public Mytests As MyTests
End Module
]]>.Value)

            AssertTheseDiagnostics(compilation,
<expected>
BC30109: 'DefaultInstanceTest1' is a class type and cannot be used as an expression.
        Dim x = DefaultInstanceTest1
                ~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub DefaultInstancePropertyInvalidType()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Sub Main()
        Dim x = DefaultInstanceTest1 'BIND1:"DefaultInstanceTest1"
        Dim y = DefaultInstanceTest2 'BIND2:"DefaultInstanceTest2"
    End Sub
End Module

Public Class DefaultInstanceTest
    Sub Close()
    End Sub

    Public F1 As Integer
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTest
End Class
Public Class DefaultInstanceTest2
    Inherits DefaultInstanceTest
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            compilation.MyTemplate = ParseTemplateTree(
            <![CDATA[
Imports System

<Microsoft.VisualBasic.MyGroupCollection("defaultInstanceteSt", "Create", "Dispose", "Factory")> _
Public NotInheritable Class MyTests
    Private Shared Function Create(Of T As {New, DefaultInstanceTest}) _
        (Instance As T) As T
        If Instance Is Nothing Then
            Return New T()
        Else
            Return Instance
        End If
    End Function

    Private Shared Sub Dispose(Of T As DefaultInstanceTest)(ByRef Instance As T)
        Instance.Close()
        Instance = Nothing
    End Sub
End Class

Module Factory
    Public DefaultInstanceTest1 As DefaultInstanceTest
    Public DefaultInstanceTest2 As DefaultInstanceTest2
End Module
]]>.Value)

            AssertTheseDiagnostics(compilation,
<expected>
BC30109: 'DefaultInstanceTest1' is a class type and cannot be used as an expression.
        Dim x = DefaultInstanceTest1 'BIND1:"DefaultInstanceTest1"
                ~~~~~~~~~~~~~~~~~~~~
</expected>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim symbolInfo As SymbolInfo
            Dim typeInfo As TypeInfo

            Dim node1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)
            Assert.Equal("= DefaultInstanceTest1", node1.Parent.ToString())
            symbolInfo = semanticModel.GetSymbolInfo(node1)
            Assert.Equal(SymbolKind.NamedType, symbolInfo.CandidateSymbols.Single().Kind)
            Assert.Equal("DefaultInstanceTest1", symbolInfo.CandidateSymbols.Single().ToTestDisplayString())
            Assert.Equal(CandidateReason.NotAValue, symbolInfo.CandidateReason)
            typeInfo = semanticModel.GetTypeInfo(node1)
            Assert.Equal("DefaultInstanceTest1", typeInfo.Type.ToTestDisplayString())

            Dim node2 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 2)
            Assert.Equal("= DefaultInstanceTest2", node2.Parent.ToString())
            symbolInfo = semanticModel.GetSymbolInfo(node2)
            Assert.Equal(SymbolKind.Field, symbolInfo.Symbol.Kind)
            Assert.Equal("Factory.DefaultInstanceTest2 As DefaultInstanceTest2", symbolInfo.Symbol.ToTestDisplayString())
            typeInfo = semanticModel.GetTypeInfo(node2)
            Assert.Equal("DefaultInstanceTest2", typeInfo.Type.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub DefaultInstancePropertyAsFunction()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Sub Main()
        Dim y = DefaultInstanceTest2 'BIND2:"DefaultInstanceTest2"
    End Sub
End Module

Public Class DefaultInstanceTest
    Sub Close()
    End Sub

    Public F1 As Integer
End Class

Public Class DefaultInstanceTest2
    Inherits DefaultInstanceTest
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            compilation.MyTemplate = ParseTemplateTree(
            <![CDATA[
Imports System

<Microsoft.VisualBasic.MyGroupCollection("defaultInstanceteSt", "Create", "Dispose", "Factory")> _
Public NotInheritable Class MyTests
    Private Shared Function Create(Of T As {New, DefaultInstanceTest}) _
        (Instance As T) As T
        If Instance Is Nothing Then
            Return New T()
        Else
            Return Instance
        End If
    End Function

    Private Shared Sub Dispose(Of T As DefaultInstanceTest)(ByRef Instance As T)
        Instance.Close()
        Instance = Nothing
    End Sub
End Class

Module Factory
    Public Function DefaultInstanceTest2() As DefaultInstanceTest2
        Return Nothing
    End Function
End Module
]]>.Value)

            AssertNoDiagnostics(compilation)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim symbolInfo As SymbolInfo
            Dim typeInfo As TypeInfo

            Dim node2 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 2)
            Assert.Equal("= DefaultInstanceTest2", node2.Parent.ToString())
            symbolInfo = semanticModel.GetSymbolInfo(node2)
            Assert.Equal(SymbolKind.Method, symbolInfo.Symbol.Kind)
            Assert.Equal("Function Factory.DefaultInstanceTest2() As DefaultInstanceTest2", symbolInfo.Symbol.ToTestDisplayString())
            typeInfo = semanticModel.GetTypeInfo(node2)
            Assert.Equal("DefaultInstanceTest2", typeInfo.Type.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub ERRID_CantReferToMyGroupInsideGroupType1_1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Imports System

Public Class DefaultInstanceTest
    Sub Close()
    End Sub

    Public F1 As Integer
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTest

    Shared F2 As DefaultInstanceTest1 = DefaultInstanceTest1
    Private F3 As DefaultInstanceTest1 = DefaultInstanceTest1

    Shared Sub Test1()
        Dim x = DefaultInstanceTest1
    End Sub

    Sub Test2()
        Dim y = DefaultInstanceTest1
    End Sub

End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            compilation.MyTemplate = ParseTemplateTree(
            <![CDATA[
Imports System

<Microsoft.VisualBasic.MyGroupCollection("defaultInstanceteSt", "Create", "Dispose", "Factory")> _
Public NotInheritable Class MyTests
    Private Shared Function Create(Of T As {New, DefaultInstanceTest}) _
        (Instance As T) As T
        If Instance Is Nothing Then
            Return New T()
        Else
            Return Instance
        End If
    End Function

    Private Shared Sub Dispose(Of T As DefaultInstanceTest)(ByRef Instance As T)
        Instance.Close()
        Instance = Nothing
    End Sub
End Class

Module Factory
    Public DefaultInstanceTest1 As DefaultInstanceTest1
End Module
]]>.Value)

            AssertTheseDiagnostics(compilation,
<expected>
BC31139: 'DefaultInstanceTest1' cannot refer to itself through its default instance; use 'Me' instead.
    Private F3 As DefaultInstanceTest1 = DefaultInstanceTest1
                                         ~~~~~~~~~~~~~~~~~~~~
BC31139: 'DefaultInstanceTest1' cannot refer to itself through its default instance; use 'Me' instead.
        Dim y = DefaultInstanceTest1
                ~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ERRID_CantReferToMyGroupInsideGroupType1_2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase1", "Create", "Dispose", "")> _
Public Class MyTests
    Private Shared Function Create(Of T)(Instance As T) As T
        Return Nothing
    End Function

    Private Shared Sub Dispose(Of T)(ByRef Instance As T)
        Instance = Nothing
    End Sub
End Class

Public Class DefaultInstanceTestBase1
End Class

Public Class DefaultInstanceTest1
    Inherits DefaultInstanceTestBase1

    Sub Test(x As MyTests)
        Dim y = x.DefaultInstanceTest1
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            ' Native compiler reports BC31139, but I do not believe it is appropriate in this scenario.
            CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(DesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/27979")>
        Public Sub Is_IsNot()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="types.vb"><![CDATA[
Imports System

Namespace Global
    Public Class Form2
        Inherits Windows.Forms.Form
    End Class
End Namespace

Namespace Global.WindowsApplication1
    Public Class Form1
        Inherits Windows.Forms.Form
    End Class
End Namespace
    ]]></file>
    <file name="Test1.vb"><![CDATA[
Imports System

Public Module TestM
    Public Sub Main()
        If Form1 Is Nothing Then
            System.Console.WriteLine("True")
        Else
            System.Console.WriteLine("False")
        End If

        If Nothing IsNot Form1 Then
            System.Console.WriteLine("True")
        Else
            System.Console.WriteLine("False")
        End If

        If Form2 Is Form1 Then
            System.Console.WriteLine("True")
        Else
            System.Console.WriteLine("False")
        End If

        Test(Function() Form1 Is Nothing)
        Test(Function() Form2 IsNot Nothing)
        Test(Function() Form1 Is Nothing AndAlso Form2 IsNot Nothing)
    End Sub

    Sub Test(x As System.Linq.Expressions.Expression(Of Func(Of Boolean)))
        System.Console.WriteLine(x.ToString())
    End Sub
End Module
    ]]></file>
    <file name="Form.vb"><![CDATA[
Namespace Global.System.Windows.Forms
    Public Class Form
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub

        Public ReadOnly Property IsDisposed As Boolean
            Get
                Return False
            End Get
        End Property
    End Class
End Namespace
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef,
                                                                                     {SystemCoreRef},
                                                                                     TestOptions.ReleaseExe.WithRootNamespace("WindowsApplication1"))

            compilation.MyTemplate = WindowsFormsMyTemplateTree

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
True
False
True
() => (Convert(MyProject.Forms.m_Form1) == null)
() => (Convert(MyProject.Forms.m_Form2) != null)
() => ((Convert(MyProject.Forms.m_Form1) == null) AndAlso (Convert(MyProject.Forms.m_Form2) != null))
]]>).VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(DesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/27979")>
        Public Sub BackingFieldToHaveEditorBrowsableNeverAttribute()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="types.vb"><![CDATA[
Imports System

Namespace Global.System.Windows.Forms
    Public Class Form
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub

        Public ReadOnly Property IsDisposed As Boolean
            Get
                Return False
            End Get
        End Property
    End Class
End Namespace

Namespace Global.WindowsApplication1
    Public Class Form1
        Inherits Windows.Forms.Form
    End Class
End Namespace

Public Module TestM
    Public Sub Main()
        For Each member In My.MyProject.Forms.GetType().GetMember("m_Form1")
            Dim attrs = member.GetCustomAttributes(GetType(Global.System.ComponentModel.EditorBrowsableAttribute), True)
            Console.Write(attrs.Length)
            Console.Write(" ")
            Console.Write([Enum].GetName(GetType(Global.System.ComponentModel.EditorBrowsableState),
                                         DirectCast(attrs(0), Global.System.ComponentModel.EditorBrowsableAttribute).State))
        Next
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef,
                                                                                     {SystemCoreRef},
                                                                                     TestOptions.ReleaseExe.WithRootNamespace("WindowsApplication1"))

            compilation.MyTemplate = WindowsFormsMyTemplateTree

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="1 Never").VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(DesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/27979")>
        Public Sub Using001()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="types.vb"><![CDATA[
Imports System

Namespace Global.System.Windows.Forms
    Public Class Form
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
            System.Console.WriteLine("disposed")
        End Sub

        Public ReadOnly Property IsDisposed As Boolean
            Get
                Return False
            End Get
        End Property
    End Class
End Namespace

Namespace Global.WindowsApplication1
    Public Class Form1
        Inherits Windows.Forms.Form
    End Class
End Namespace

Public Module TestM
    Public Sub Main()
        Using Form1
        end using
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef,
                                                                                     {SystemCoreRef},
                                                                                     TestOptions.DebugExe.WithRootNamespace("WindowsApplication1"))

            compilation.MyTemplate = WindowsFormsMyTemplateTree

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="disposed").VerifyDiagnostics()
        End Sub

        <Fact, WorkItem(560657, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/560657")>
        Public Sub Bug560657()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Class DefaultInstanceTestBase8
End Class

<Microsoft.VisualBasic.MyGroupCollection("DefaultInstanceTestBase8", "Create1", "Dispose1", "")> _
Public Class MyTests15
    Sub New()
    End Sub

    Private Function Create1(Of T As {New})(Instance As T) As T
        Throw New NotImplementedException()
    End Function

    Private Sub Dispose1(Of T)(ByRef Instance As T)
        Throw New NotImplementedException()
    End Sub
End Class

Public Class DefaultInstanceTestBase9
    Inherits DefaultInstanceTestBase8
End Class

Public Class DefaultInstanceTestBase10
    Inherits DefaultInstanceTestBase9
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            Dim MyTests15 = compilation.GetTypeByMetadataName("MyTests15")

            Dim expected15() As String = {"Sub MyTests15..ctor()",
                                        "Function MyTests15.Create1(Of T)(Instance As T) As T",
                                        "Sub MyTests15.Dispose1(Of T)(ByRef Instance As T)",
                                        "MyTests15.m_DefaultInstanceTestBase10 As DefaultInstanceTestBase10",
                                        "MyTests15.m_DefaultInstanceTestBase9 As DefaultInstanceTestBase9",
                                        "Function MyTests15.get_DefaultInstanceTestBase10() As DefaultInstanceTestBase10",
                                        "Function MyTests15.get_DefaultInstanceTestBase9() As DefaultInstanceTestBase9",
                                        "Sub MyTests15.set_DefaultInstanceTestBase10(Value As DefaultInstanceTestBase10)",
                                        "Sub MyTests15.set_DefaultInstanceTestBase9(Value As DefaultInstanceTestBase9)",
                                        "Property MyTests15.DefaultInstanceTestBase10 As DefaultInstanceTestBase10",
                                        "Property MyTests15.DefaultInstanceTestBase9 As DefaultInstanceTestBase9"}

            Dim members = MyTests15.GetMembers()
            Assert.Equal(expected15.Length, members.Length)

            For i As Integer = 0 To members.Length - 1
                Assert.Equal(expected15(i), members(i).ToTestDisplayString())
            Next

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub SemanticModelTest_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class Form1
    Inherits Windows.Forms.Form

    Shared Sub M1()
    End Sub

    Default Readonly Property P1(x as Integer) As Integer
        Get
            Return x
        End Get
    End Property
End Class

Class Test
    Sub Test1()
        Form1.M1() 'BIND1:"Form1"
        Form1.Close() 'BIND2:"Form1"

        Dim f1 = Form1 'BIND3:"Form1"
        Console.WriteLine(f1)

        Dim p1 = Form1(2) 'BIND4:"Form1"
        Console.WriteLine(p1)
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemWindowsFormsRef, SystemDrawingRef})

            compilation.MyTemplate = GroupClassTests.WindowsFormsMyTemplateTree

            compilation.AssertNoDiagnostics()

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim symbolInfo As SymbolInfo
            Dim typeInfo As TypeInfo

            Dim node1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)
            Assert.Equal("Form1.M1", node1.Parent.ToString())
            symbolInfo = semanticModel.GetSymbolInfo(node1)
            Assert.Equal(SymbolKind.NamedType, symbolInfo.Symbol.Kind)
            Assert.Equal("Form1", symbolInfo.Symbol.ToTestDisplayString())
            typeInfo = semanticModel.GetTypeInfo(node1)
            Assert.Equal("Form1", typeInfo.Type.ToTestDisplayString())

            Dim node2 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 2)
            Assert.Equal("Form1.Close", node2.Parent.ToString())
            symbolInfo = semanticModel.GetSymbolInfo(node2)
            Assert.Equal(SymbolKind.Property, symbolInfo.Symbol.Kind)
            Assert.Equal("Property My.MyProject.MyForms.Form1 As Form1", symbolInfo.Symbol.ToTestDisplayString())
            typeInfo = semanticModel.GetTypeInfo(node2)
            Assert.Equal("Form1", typeInfo.Type.ToTestDisplayString())

            Dim node3 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 3)
            Assert.Equal("f1 = Form1", node3.Parent.Parent.ToString())
            symbolInfo = semanticModel.GetSymbolInfo(node3)
            Assert.Equal(SymbolKind.Property, symbolInfo.Symbol.Kind)
            Assert.Equal("Property My.MyProject.MyForms.Form1 As Form1", symbolInfo.Symbol.ToTestDisplayString())
            typeInfo = semanticModel.GetTypeInfo(node3)
            Assert.Equal("Form1", typeInfo.Type.ToTestDisplayString())

            Dim node4 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 4)
            Assert.Equal("= Form1(2)", node4.Parent.Parent.ToString())
            symbolInfo = semanticModel.GetSymbolInfo(node4)
            Assert.Equal(SymbolKind.Property, symbolInfo.Symbol.Kind)
            Assert.Equal("Property My.MyProject.MyForms.Form1 As Form1", symbolInfo.Symbol.ToTestDisplayString())
            typeInfo = semanticModel.GetTypeInfo(node3)
            Assert.Equal("Form1", typeInfo.Type.ToTestDisplayString())
        End Sub

    End Class

End Namespace
