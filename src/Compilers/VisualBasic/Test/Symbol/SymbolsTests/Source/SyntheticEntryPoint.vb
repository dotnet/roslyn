' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.OverloadResolution
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class SyntheticEntryPoint
        Inherits BasicTestBase

        <Fact>
        Public Sub NotAForm()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class Test
    Sub Test()
        Main()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithMainTypeName("Test"))

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30420: 'Sub Main' was not found in 'Test'.
BC30451: 'Main' is not declared. It may be inaccessible due to its protection level.
        Main()
        ~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub AFormWithoutDefaultInstance1()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Namespace NS
Class Test
    Inherits Windows.Forms.Form

    Sub Test()
        Main()
    End Sub
End Class
End Namespace
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemWindowsFormsRef, SystemDrawingRef}, TestOptions.ReleaseExe.WithMainTypeName("ns.test"))

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()

            '.method public hidebysig static void  Main() cil managed
            '{
            '            .entrypoint()
            '  .custom instance void [mscorlib]System.STAThreadAttribute::.ctor() = ( 01 00 00 00 ) 
            '  // Code size       11 (0xb)
            '            .maxstack 8
            '  IL_0000:  newobj     instance void Test::.ctor()
            '  IL_0005:  call       void [System.Windows.Forms]System.Windows.Forms.Application::Run(class [System.Windows.Forms]System.Windows.Forms.Form)
            '  IL_000a:    ret()
            '} // end of method Test::Main

            verifier.VerifyIL("NS.Test.Main",
            <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  newobj     "Sub NS.Test..ctor()"
  IL_0005:  call       "Sub System.Windows.Forms.Application.Run(System.Windows.Forms.Form)"
  IL_000a:  ret
}
]]>)

        End Sub

        <Fact>
        Public Sub AFormWithDefaultInstance()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class Test
    Inherits Windows.Forms.Form

    Sub Test()
        Main()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemWindowsFormsRef, SystemDrawingRef}, TestOptions.ReleaseExe.WithMainTypeName("Test"))

            compilation.MyTemplate = GroupClassTests.WindowsFormsMyTemplateTree

            Dim verifier = CompileAndVerify(compilation,
                                            symbolValidator:=Sub(m As ModuleSymbol)
                                                                 Dim mainAttributes = m.GlobalNamespace.GetTypeMember("Test").GetMember("Main").GetAttributes()
                                                                 Assert.Equal(1, mainAttributes.Length)
                                                                 Assert.Equal("System.STAThreadAttribute", mainAttributes(0).AttributeClass.ToTestDisplayString())
                                                             End Sub).VerifyDiagnostics()

            '.method public hidebysig static void  Main() cil managed
            '{
            '            .entrypoint()
            '  .custom instance void [mscorlib]System.STAThreadAttribute::.ctor() = ( 01 00 00 00 ) 
            '  // Code size       16 (0x10)
            '            .maxstack 8
            '  IL_0000:  call       class My.MyProject/MyForms My.MyProject::get_Forms()
            '  IL_0005:  callvirt   instance class Test My.MyProject/MyForms::get_Test()
            '  IL_000a:  call       void [System.Windows.Forms]System.Windows.Forms.Application::Run(class [System.Windows.Forms]System.Windows.Forms.Form)
            '  IL_000f:    ret()
            '} // end of method Test::Main

            verifier.VerifyIL("Test.Main",
            <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  call       "Function My.MyProject.get_Forms() As My.MyProject.MyForms"
  IL_0005:  callvirt   "Function My.MyProject.MyForms.get_Test() As Test"
  IL_000a:  call       "Sub System.Windows.Forms.Application.Run(System.Windows.Forms.Form)"
  IL_000f:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub AFormIsGeneric()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class Test(Of T)
    Inherits Windows.Forms.Form

    Sub Test()
        Main()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemWindowsFormsRef, SystemDrawingRef}, TestOptions.ReleaseExe.WithMainTypeName("Test"))

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30796: None of the accessible 'Main' methods with the appropriate signatures found in 'Test(Of T)' can be the startup method since they are all either generic or nested in generic types.
BC30451: 'Main' is not declared. It may be inaccessible due to its protection level.
        Main()
        ~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub AFormWithoutDefaultInstance2()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class Test
    Inherits Windows.Forms.Form

    Sub Test()
        Main()
    End Sub

    Sub New(Optional x As Integer = 0)
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemWindowsFormsRef, SystemDrawingRef}, TestOptions.ReleaseExe.WithMainTypeName("Test"))

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()

            '.method public hidebysig static void  Main() cil managed
            '{
            '            .entrypoint()
            '  .custom instance void [mscorlib]System.STAThreadAttribute::.ctor() = ( 01 00 00 00 ) 
            '  // Code size       12 (0xc)
            '            .maxstack 8
            '  IL_0000:    ldc.i4 0.0
            '  IL_0001:  newobj     instance void Test::.ctor(int32)
            '  IL_0006:  call       void [System.Windows.Forms]System.Windows.Forms.Application::Run(class [System.Windows.Forms]System.Windows.Forms.Form)
            '  IL_000b:    ret()
            '} // end of method Test::Main

            verifier.VerifyIL("Test.Main",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     "Sub Test..ctor(Integer)"
  IL_0006:  call       "Sub System.Windows.Forms.Application.Run(System.Windows.Forms.Form)"
  IL_000b:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub AFormWithDefaultInstance3()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class Test
    Inherits Windows.Forms.Form

    Sub Test()
        Main()
    End Sub

    Sub New(Optional x As Integer = 0)
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemWindowsFormsRef, SystemDrawingRef}, TestOptions.ReleaseExe.WithMainTypeName("Test"))

            compilation.MyTemplate = GroupClassTests.WindowsFormsMyTemplateTree

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()

            ' Dev11 fails with the error without location:
            ' error BC30691: 'Test' is a type in '' and cannot be used as an expression.

            '.method public hidebysig static void  Main() cil managed
            '{
            '            .entrypoint()
            '  .custom instance void [mscorlib]System.STAThreadAttribute::.ctor() = ( 01 00 00 00 ) 
            '  // Code size       12 (0xc)
            '            .maxstack 8
            '  IL_0000:    ldc.i4 0.0
            '  IL_0001:  newobj     instance void Test::.ctor(int32)
            '  IL_0006:  call       void [System.Windows.Forms]System.Windows.Forms.Application::Run(class [System.Windows.Forms]System.Windows.Forms.Form)
            '  IL_000b:    ret()
            '} // end of method Test::Main

            verifier.VerifyIL("Test.Main",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     "Sub Test..ctor(Integer)"
  IL_0006:  call       "Sub System.Windows.Forms.Application.Run(System.Windows.Forms.Form)"
  IL_000b:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub AFormWithoutDefaultInstanceAndSuitableNew1()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class Test
    Inherits Windows.Forms.Form

    Sub Test()
        Main()
    End Sub

    Sub New(x As Integer)
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemWindowsFormsRef, SystemDrawingRef}, TestOptions.ReleaseExe.WithMainTypeName("Test"))

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30420: 'Sub Main' was not found in 'Test'.
BC30451: 'Main' is not declared. It may be inaccessible due to its protection level.
        Main()
        ~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub AFormWithoutDefaultInstanceAndSuitableNew2()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class Test
    Inherits Windows.Forms.Form

    Sub Test()
        Main()
    End Sub

    Sub New(x As Integer)
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemWindowsFormsRef, SystemDrawingRef}, TestOptions.ReleaseExe.WithMainTypeName("Test"))

            compilation.MyTemplate = GroupClassTests.WindowsFormsMyTemplateTree

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30420: 'Sub Main' was not found in 'Test'.
BC30451: 'Main' is not declared. It may be inaccessible due to its protection level.
        Main()
        ~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub AFormWithMain1()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class Test
    Inherits Windows.Forms.Form

    Sub Test()
        Main()
    End Sub

    Private Sub Main()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemWindowsFormsRef, SystemDrawingRef}, TestOptions.ReleaseExe.WithMainTypeName("Test"))

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30737: No accessible 'Main' method with an appropriate signature was found in 'Test'.
]]></expected>)
        End Sub

        <Fact>
        Public Sub AFormWithMain2()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class Test
    Inherits Windows.Forms.Form

    Sub Test()
        Main()
    End Sub

    Private Sub Main()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemWindowsFormsRef, SystemDrawingRef}, TestOptions.ReleaseExe.WithMainTypeName("Test"))

            compilation.MyTemplate = GroupClassTests.WindowsFormsMyTemplateTree

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30737: No accessible 'Main' method with an appropriate signature was found in 'Test'.
]]></expected>)
        End Sub

        <Fact>
        Public Sub AFormWithMain3()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class Test
    Inherits Windows.Forms.Form

    Sub Test()
    End Sub

    Class Main(Of T)
    End Class
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemWindowsFormsRef, SystemDrawingRef}, TestOptions.ReleaseExe.WithMainTypeName("Test"))

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30420: 'Sub Main' was not found in 'Test'.
]]></expected>)
        End Sub

        <Fact>
        Public Sub AFormWithMain4()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class Test
    Inherits Windows.Forms.Form

    Sub Test()
    End Sub

    Class Main(Of T)
    End Class
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemWindowsFormsRef, SystemDrawingRef}, TestOptions.ReleaseExe.WithMainTypeName("Test"))

            compilation.MyTemplate = GroupClassTests.WindowsFormsMyTemplateTree

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30420: 'Sub Main' was not found in 'Test'.
]]></expected>)
        End Sub

        <Fact>
        Public Sub CycleThroughConstraints()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class TestBase(Of T As New)
    Inherits Windows.Forms.Form
End Class

Class Test
    Inherits TestBase(Of Test)

    Sub Test()
	Main()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemWindowsFormsRef, SystemDrawingRef}, TestOptions.ReleaseExe.WithMainTypeName("Test"))

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

    End Class

End Namespace
