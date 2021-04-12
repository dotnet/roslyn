' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.TestMetadata

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit

    Public Class EntryPointTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub MainOverloads()
            Dim source =
<compilation>
    <file>
Public Class C
    Public Shared Sub Main(goo As Integer)
        System.Console.WriteLine(1)
    End Sub

    Public Shared Sub Main()
        System.Console.WriteLine(2)
    End Sub
End Class
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:="2")

            verifier.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub MainOverloads_Dll()
            Dim source =
<compilation>
    <file>
Public Class C
    Public Shared Sub Main(goo As Integer)
        System.Console.WriteLine(1)
    End Sub

    Public Shared Sub Main()
        System.Console.WriteLine(2)
    End Sub
End Class
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll)
            Dim verifier = CompileAndVerify(compilation)
            verifier.VerifyDiagnostics()
        End Sub

        ''' <summary>
        ''' Dev10 reports the .exe full path in CS5001. We don't. 
        ''' </summary>
        <Fact()>
        Public Sub ERR_NoEntryPoint_Overloads()
            Dim source =
<compilation name="a">
    <file>
Public Class C
    Public Shared Sub Main(goo As Integer)
        System.Console.WriteLine(1)
    End Sub

    Public Shared Sub Main(goo As Double)
        System.Console.WriteLine(2)
    End Sub

    Public Shared Sub Main(goo As String(,))
        System.Console.WriteLine(2)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe)
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("a"))
        End Sub

        ''' <summary> 
        ''' Dev10 reports the .exe full path in CS0017. We don't. 
        ''' </summary>
        <Fact()>
        Public Sub ERR_MultipleEntryPoints()
            Dim source =
<compilation name="a">
    <file>
Public Class C
    Public Shared Sub Main()
        System.Console.WriteLine(1)
    End Sub

    Public Shared Sub Main(a As String())
        System.Console.WriteLine(2)
    End Sub
End Class

Public Class D
    Public Shared Function Main() As String
        System.Console.WriteLine(3)
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe)

            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_MoreThanOneValidMainWasFound2).WithArguments("a", "C.Main(), C.Main(a As String())"))
        End Sub

        <ConditionalFact(GetType(NoUsedAssembliesValidation))> ' https://github.com/dotnet/roslyn/issues/40682: The test hook is blocked by this issue.
        <WorkItem(40682, "https://github.com/dotnet/roslyn/issues/40682")>
        Public Sub ERR_MultipleEntryPoints_Script()
            Dim vbx = <text>
Public Shared Sub Main()
    System.Console.WriteLine(1)
End Sub
</text>

            Dim vb = <text>
Public Class C
    Public Shared Sub Main()
        System.Console.WriteLine(2)
    End Sub
End Class
</text>

            Dim compilation = CreateCompilationWithMscorlib40(
                {VisualBasicSyntaxTree.ParseText(vbx.Value, options:=TestOptions.Script),
                 VisualBasicSyntaxTree.ParseText(vb.Value, options:=VisualBasicParseOptions.Default)}, options:=TestOptions.ReleaseExe)

            ' TODO: compilation.VerifyDiagnostics(Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("Main()"), Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("C.Main()"))
        End Sub

        <WorkItem(528677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528677")>
        <Fact()>
        Public Sub ERR_OneEntryPointAndOverload()
            Dim source =
<compilation>
    <file>
Public Class C
    Shared Function Main() As Integer
        Return 0
    End Function

    Shared Function Main(args As String(), i As Integer) As Integer
        Return i
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe)
            compilation.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub Namespaces()
            Dim source =
<compilation>
    <file>
Namespace N
   Namespace M
   End Namespace
End Namespace
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("N.M"))
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("N.M"))
        End Sub

        <Fact()>
        Public Sub Modules()
            Dim source =
<compilation>
    <file>
Namespace N
    Module M
        Sub Main()

        End Sub
    End Module
End Namespace
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseExe.WithMainTypeName("N.M"))
            compilation.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub Structures()
            Dim source =
<compilation>
    <file>
Structure C
    Structure D
        Public Shared Sub Main()
        End Sub
    End Structure
End Structure
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("C.D"))
            compilation.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub NestedGenericMainType()
            Dim source =
<compilation>
    <file>
Class C(Of T)
    Structure D
        Public Shared Sub Main()
        End Sub
    End Structure
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("C.D"))
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_GenericSubMainsFound1).WithArguments("C(Of T).D"))
        End Sub

        <Fact()>
        Public Sub GenericMainMethods()
            Dim vb =
<compilation>
    <file>
Imports System

Public Class C
    Public Shared Sub Main(Of T)()
        Console.WriteLine(1)
    End Sub

    Public Class CC(Of T)
        Public Shared Sub Main()
            Console.WriteLine(2)
        End Sub
    End Class
End Class

Public Class D(Of T)
    Shared Sub Main()
        Console.WriteLine(3)
    End Sub

    Public Class DD
        Public Shared Sub Main()
            Console.WriteLine(4)
        End Sub
    End Class
End Class

Public Class E
    Public Shared Sub Main()
        Console.WriteLine(5)
    End Sub
End Class

Public Interface I
    Sub Main()
End Interface
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(vb, options:=TestOptions.ReleaseExe)
            compilation.VerifyDiagnostics()
            CompileAndVerify(compilation, expectedOutput:="5")

            compilation = CreateCompilationWithMscorlib40(vb, options:=TestOptions.ReleaseExe.WithMainTypeName("C"))
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_GenericSubMainsFound1).WithArguments("C"))

            compilation = CreateCompilationWithMscorlib40(vb, options:=TestOptions.ReleaseExe.WithMainTypeName("D.DD"))
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_GenericSubMainsFound1).WithArguments("D(Of T).DD"))

            compilation = CreateCompilationWithMscorlib40(vb, options:=TestOptions.ReleaseExe.WithMainTypeName("I"))
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("I"))
        End Sub

        <Fact()>
        Public Sub MultipleArities1()
            Dim source =
<compilation>
    <file>
Public Class A
    Public Class B
        Class C
            Public Shared Sub Main()
                System.Console.WriteLine(1)
            End Sub
        End Class
    End Class

    Public Class B(Of T)
        Class C
            Public Shared Sub Main()
                System.Console.WriteLine(2)
            End Sub
        End Class
    End Class
End Class
    </file>
</compilation>

            CompileAndVerify(source, options:=TestOptions.ReleaseExe.WithMainTypeName("A.B.C"), expectedOutput:="1")
        End Sub

        <Fact()>
        Public Sub MultipleArities2()
            Dim source =
<compilation>
    <file>
Public Class A
    Public Class B(Of T)
        Class C
            Public Shared Sub Main()
                System.Console.WriteLine(2)
            End Sub
        End Class
    End Class

    Public Class B
        Class C
            Public Shared Sub Main()
                System.Console.WriteLine(1)
            End Sub
        End Class
    End Class
End Class
    </file>
</compilation>

            CompileAndVerify(source, options:=TestOptions.ReleaseExe.WithMainTypeName("A.B.C"), expectedOutput:="1")
        End Sub

        <Fact()>
        Public Sub MultipleArities3()
            Dim source =
<compilation>
    <file>
Public Class A
    Public Class B(Of S, T)
        Class C
            Public Shared Sub Main()
                System.Console.WriteLine(1)
            End Sub
        End Class
    End Class

    Public Class B(Of T)
        Class C
            Public Shared Sub Main()
                System.Console.WriteLine(2)
            End Sub
        End Class
    End Class
End Class
    </file>
</compilation>

            ' Dev10 reports error BC30420: 'Sub Main' was not found in 'A.B.C'.

            ' error BC30796: None of the accessible 'Main' methods with the appropriate signatures found in 'A.B(Of T).C'
            ' can be the startup method since they are all either generic or nested in generic types.
            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("A.B.C")).VerifyDiagnostics(Diagnostic(ERRID.ERR_GenericSubMainsFound1).WithArguments("A.B(Of T).C"))
        End Sub

        ''' <summary> 
        ''' The nongeneric is used. 
        ''' </summary>
        Public Sub ExplicitMainTypeName_GenericAndNonGeneric()
            Dim source =
<compilation>
    <file>
Class C(Of T)
    Shared Sub Main()
    End Sub
End Class

Class C
    Shared Sub Main()
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("C"))
            compilation.VerifyDiagnostics()
        End Sub

        ''' <summary> 
        ''' Dev10: the first definition of C is reported (i.e. C{S,T}). We report the one with the least arity (i.e. C{T}). 
        ''' </summary>
        <Fact()>
        Public Sub ExplicitMainTypeName_GenericMultipleArities()
            Dim source =
<compilation>
    <file>
Class C(Of T)
    Shared Sub Main()
    End Sub
End Class

Class C(Of S, T)
    Shared Sub Main()
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("C"))

            ' Dev10 reports: BC30420: 'Sub Main' was not found in 'C'.

            ' error BC30796: None of the accessible 'Main' methods with the appropriate signatures found in 'C(Of T)' 
            ' can be the startup method since they are all either generic or nested in generic types.
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_GenericSubMainsFound1).WithArguments("C(Of T)"))
        End Sub

        <Fact()>
        Public Sub ExplicitMainTypeHasMultipleMains_NoViable()
            Dim source =
<compilation>
    <file>
Public Class C
    Function Main(b As Boolean) As Integer
        Return 1
    End Function

    Shared Function Main(a As String) As Integer
        Return 1
    End Function

    Shared Function Main(a As Integer) As Integer
        Return 1
    End Function

    Shared Function Main(Of T)() As Integer
        Return 1
    End Function
End Class
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("C"))
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_GenericSubMainsFound1).WithArguments("C"))
        End Sub

        <Fact()>
        Public Sub ExplicitMainTypeHasMultipleMains_SingleViable()
            Dim source =
<compilation>
    <file>
Public Class C
    Function Main(b As Boolean) As Integer
        Return 1
    End Function

    Shared Function Main() As Integer
        Return 1
    End Function

    Shared Function Main(a As String) As Integer
        Return 1
    End Function

    Shared Function Main(a As Integer) As Integer
        Return 1
    End Function

    Shared Function Main(Of T)() As Integer
        Return 1
    End Function
End Class
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("C"))
            compilation.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub ExplicitMainTypeHasMultipleMains_MultipleViable()
            Dim source =
<compilation name="a">
    <file>
Class C
    Function Main(b As Boolean) As Integer
        Return 1
    End Function

    Shared Function Main() As Integer
        Return 1
    End Function

    Shared Function Main(a As String) As Integer
        Return 1
    End Function

    Shared Function Main(a As Integer) As Integer
        Return 1
    End Function

    Shared Function Main(a As String()) As Integer
        Return 1
    End Function

    Shared Function Main(Of T)() As Integer
        Return 1
    End Function
End Class
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("C"))

            ' Dev10 displays return type, we don't; methods can't be overloaded on return type so the type is not necessary
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_MoreThanOneValidMainWasFound2).WithArguments("a", "C.Main(), C.Main(a As String())"))
        End Sub

        <Fact()>
        Public Sub ERR_NoEntryPoint_NonMethod()
            Dim source =
<compilation name="a">
    <file>
Public Class G 
   Public Shared Main As Integer = 1
End Class
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("a"))
        End Sub

        <Fact()>
        Public Sub Script()
            Dim vbx = <text>
System.Console.WriteLine(1)
</text>
            Dim compilation = CreateEmptyCompilation(
                {VisualBasicSyntaxTree.ParseText(vbx.Value, options:=TestOptions.Script)}, options:=TestOptions.ReleaseExe, references:=LatestVbReferences)

            CompileAndVerify(compilation, expectedOutput:="1")
        End Sub

        <Fact()>
        Public Sub ScriptAndRegularFile_ExplicitMain()
            Dim vbx = <text>
System.Console.WriteLine(1)
</text>

            Dim vb = <text>
Public Class C 
   Public Shared Sub Main() 
       System.Console.WriteLine(2)
  End Sub
End Class
</text>
            Dim compilation = CreateEmptyCompilation(
                {VisualBasicSyntaxTree.ParseText(vbx.Value, options:=TestOptions.Script),
                 VisualBasicSyntaxTree.ParseText(vb.Value, options:=VisualBasicParseOptions.Default)}, options:=TestOptions.ReleaseExe, references:=LatestVbReferences)

            ' TODO: compilation.VerifyDiagnostics(Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("C.Main()"))
            CompileAndVerify(compilation, expectedOutput:="1")
        End Sub

        <Fact()>
        Public Sub ScriptAndRegularFile_ExplicitMains()
            Dim vbx = <text>
System.Console.WriteLine(1)
</text>

            Dim vb = <text>
Public Class C
    Public Shared Sub Main()
        System.Console.WriteLine(2)
    End Sub
End Class

Public Class D
    Public Shared Sub Main()
        System.Console.WriteLine(3)
    End Sub
End Class
</text>
            Dim compilation = CreateEmptyCompilation(
                {VisualBasicSyntaxTree.ParseText(vbx.Value, options:=TestOptions.Script),
                 VisualBasicSyntaxTree.ParseText(vb.Value, options:=VisualBasicParseOptions.Default)}, options:=TestOptions.ReleaseExe, references:=LatestVbReferences)

            ' TODO: compilation.VerifyDiagnostics(Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("C.Main()"), Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("D.Main()"))
            CompileAndVerify(compilation, expectedOutput:="1")
        End Sub

        <Fact()>
        Public Sub ExplicitMain()
            Dim source =
<compilation>
    <file>
Class C
    Shared Sub Main()
        System.Console.WriteLine(1)
    End Sub
End Class

Class D
    Shared Sub Main()
        System.Console.WriteLine(2)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("C"))
            CompileAndVerify(compilation, expectedOutput:="1")

            compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("D"))
            CompileAndVerify(compilation, expectedOutput:="2")
        End Sub

        <Fact()>
        Public Sub ERR_MainClassNotFound()
            Dim source =
<compilation>
    <file>
Class C
    Shared Sub Main()
        System.Console.WriteLine(1)
    End Sub
End Class
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("D"))
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("D"))
        End Sub

        <Fact()>
        Public Sub ERR_MainClassNotClass()
            Dim source =
<compilation>
    <file>
Enum C
    Main = 1
End Enum

Delegate Sub D()
Interface I

End Interface
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("C"))
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("C"))

            compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("D"))
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("D"))

            compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("I"))
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("I"))
        End Sub

        <Fact()>
        Public Sub ERR_NoMainInClass()
            Dim source =
<compilation>
    <file>
Class C
    Sub Main()
    End Sub
End Class

Class D
    Shared Sub Main(args As Double)
    End Sub
End Class

Class E
    ReadOnly Property Main As Integer
        Get
            System.Console.WriteLine(1)
            Return 1
        End Get
    End Property
End Class

Class F
    Private Shared Sub Main()
    End Sub
End Class

Class G
    Private Class P
        Public Shared Sub Main()
        End Sub

        Public Class Q 
            Public Shared Sub Main()
            End Sub
        End Class
    End Class
End Class
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("C"))
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("C"))

            compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("D"))
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("D"))

            compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("E"))
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("E"))

            compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("F"))
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("F"))

            compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("G.P"))
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("G.P"))

            compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("G.P.Q"))
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("G.P.Q"))
        End Sub

        <ConditionalFact(GetType(NoUsedAssembliesValidation))> ' https://github.com/dotnet/roslyn/issues/40682: The test hook is blocked by this issue.
        <WorkItem(40682, "https://github.com/dotnet/roslyn/issues/40682")>
        Public Sub ERR_NoMainInClass_Script()
            Dim vbx = <text>
System.Console.WriteLine(2)
</text>

            Dim vb = <text>
Class C
    Sub Main()
        System.Console.WriteLine(1)
    End Sub
End Class
</text>
            Dim compilation = CreateCompilationWithMscorlib40(
                {VisualBasicSyntaxTree.ParseText(vbx.Value, options:=TestOptions.Script),
                 VisualBasicSyntaxTree.ParseText(vb.Value, options:=TestOptions.Regular)}, options:=TestOptions.ReleaseExe.WithMainTypeName("C"))

            ' TODO: compilation.VerifyDiagnostics(Diagnostic(ErrorCode.WRN_MainIgnored).WithArguments("C"))
        End Sub

        <Fact()>
        Public Sub RefParameterForMain()
            Dim source =
<compilation name="a">
    <file>
Public Class C
    Shared Sub Main(ByRef args As String())
    End Sub
End Class
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe)
            compilation.VerifyDiagnostics(
                 Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("a"))
        End Sub

        <Fact()>
        Public Sub ERR_InValidSubMainsFound1_PartialClass_1()
            Dim source =
<compilation name="a">
    <file>
        Partial Public Class A
            Private Shared Partial Sub Main()
            End Sub
        End Class
        Partial Public Class A
        End Class
    </file>
</compilation>
            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                 Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("a"))

        End Sub

        <Fact()>
        Public Sub ERR_InValidSubMainsFound1_PartialClass_2()

            Dim source =
<compilation name="a">
    <file>
        Partial Public Class A
            Private Shared Partial Sub Main()
            End Sub
        End Class
        Partial Public Class A
            Private Shared Partial Sub Main(args As String(,))
            End Sub
            Private Shared Sub Main()
            End Sub
        End Class
    </file>
</compilation>
            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                 Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("a"))

        End Sub

        <Fact()>
        Public Sub ERR_InValidSubMainsFound1_JaggedArray()
            Dim source =
<compilation name="a">
    <file>
        Public Class A
            Public Shared Sub Main(args As String()())
            End Sub
        End Class
    </file>
</compilation>
            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                 Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("a"))

        End Sub

        <Fact()>
        Public Sub ERR_InValidSubMainsFound1_Array()
            Dim source =
<compilation name="a">
    <file>
        Imports System
        Public Class A
            Public Shared Sub Main(args As Array)
            End Sub
        End Class
    </file>
</compilation>
            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                 Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("a"))
        End Sub

        <Fact()>
        Public Sub ERR_StartupCodeNotFound1_MainIsProperty()
            Dim source =
<compilation name="a">
    <file>
        Public Class A
            Property Main As String
                Get
                    Return "Main"
                End Get
                Set(ByVal value As String)
                End Set
            End Property
        End Class
    </file>
</compilation>
            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                 Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("a"))
        End Sub

        <Fact()>
        Public Sub ERR_InValidSubMainsFound1_ReturnTypeOtherthanInteger()
            Dim source =
<compilation name="a">
    <file>
        Public Class A
            Shared Function Main() As Integer()
                Return Nothing
            End Function
        End Class
    </file>
</compilation>
            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                 Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("a"))
        End Sub

        <Fact()>
        Public Sub ParamParameterForMain()
            Dim source =
<compilation name="a">
    <file>
        Public Class A
            Shared Function Main(ParamArray ByVal x As String()) As Integer
                Return Nothing
            End Function
        End Class
    </file>
</compilation>
            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe).VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub ParamParameterForMain_1()
            Dim source =
<compilation name="a">
    <file>
        Public Class A
            Shared Function Main(ParamArray ByVal x As Integer()) As Integer
                Return Nothing
            End Function
        End Class
    </file>
</compilation>
            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("a"))
        End Sub

        <Fact()>
        Public Sub ParamParameterForMain_2()
            Dim source =
<compilation name="a">
    <file>
        Public Class A
            Shared Function Main(args as string(), ParamArray ByVal x As Integer()) As Integer
                Return Nothing
            End Function
        End Class
    </file>
</compilation>
            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("a"))
        End Sub

        <Fact()>
        Public Sub OptionalParameterForMain()
            Dim source =
<compilation name="a">
    <file>
        Public Class A
            Shared Function Main(Optional ByRef x As String() = Nothing) As Integer
                Return Nothing
            End Function
        End Class
    </file>
</compilation>
            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("a"))
        End Sub

        <Fact()>
        Public Sub OptionalParameterForMain_1()
            Dim source =
<compilation name="a">
    <file>
        Public Class A
            Shared Function Main(Optional ByRef x As integer = 1) As Integer
                Return Nothing
            End Function
        End Class
    </file>
</compilation>
            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("a"))
        End Sub

        <Fact()>
        Public Sub OptionalParameterForMain_2()
            Dim source =
<compilation name="a">
    <file>
        Public Class A
            Shared Function Main(Optional ByRef x As integer(,) = nothing) As Integer
                Return Nothing
            End Function
        End Class
    </file>
</compilation>
            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("a"))
        End Sub

        <Fact()>
        Public Sub MainAsExtensionMethod()
            Dim source =
<compilation name="a">
    <file>
        Imports System.Runtime.CompilerServices
        Class B
        End Class
        Module Extension
            &lt;Extension()&gt;
            Public Sub Main(x As B, args As String())
            End Sub
        End Module
    </file>
</compilation>
            CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {Net40.SystemCore}, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("a"))
        End Sub

        <Fact()>
        Public Sub MainAsExtensionMethod_1()
            Dim source =
<compilation name="a">
    <file>
        Imports System.Runtime.CompilerServices
        Class B
        End Class
        Module Extension
            &lt;Extension()&gt;
            Public Sub Main(x As B)
            End Sub
        End Module
    </file>
</compilation>
            CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {Net40.SystemCore}, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("a"))
        End Sub

        <Fact()>
        Public Sub MainAsExtensionMethod_2()
            Dim source =
<compilation name="a">
    <file>
        Imports System.Runtime.CompilerServices
        Class B
        End Class
        Module Extension
            &lt;Extension()&gt;
            Public Sub Main(x As String)
            End Sub
        End Module
    </file>
</compilation>
            CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {Net40.SystemCore}, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("a"))
        End Sub

        <Fact()>
        Public Sub MainIsNotCaseSensitive()
            Dim source =
<compilation name="a">
    <file>
        Class A
            Shared Function main(args As String()) As Integer
                Return Nothing
            End Function
        End Class
        Module M1
            Sub mAIN()
            End Sub
        End Module
    </file>
</compilation>
            CreateCompilationWithMscorlib40AndVBRuntime(source, Nothing, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_MoreThanOneValidMainWasFound2).WithArguments("a", "A.main(args As String()), M1.mAIN()"))
        End Sub

        <Fact()>
        Public Sub MainIsNotCaseSensitive_1()
            Dim source =
<compilation name="a">
    <file>
        Class A
            Shared Sub mAIN()
            End Sub
        End Class
        Module M1
            Sub mAIN()
            End Sub
        End Module
    </file>
</compilation>
            CreateCompilationWithMscorlib40AndVBRuntime(source, Nothing, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_MoreThanOneValidMainWasFound2).WithArguments("a", "A.mAIN(), M1.mAIN()"))
        End Sub

        <Fact, WorkItem(543591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543591")>
        Public Sub MainInPrivateClass()
            Dim source =
<compilation name="a">
    <file>
        Class A
            Private Class A
                Public Shared Sub Main()
                End Sub
            End Class
        End Class
    </file>
</compilation>

            ' Dev10 reports BC30420: 'Sub Main' was not found in 'a'.
            ' We report BC30737: No accessible 'Main' method with an appropriate signature was found in 'a'.
            CreateCompilationWithMscorlib40AndVBRuntime(source, Nothing, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("a"))
        End Sub

        <Fact, WorkItem(543591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543591")>
        Public Sub MainInPrivateClass_1()
            Dim source =
<compilation>
    <file>
        Class A
            Private Class A
                Public Shared Sub Main()
                End Sub
            End Class
            Public Shared Sub Main()
            End Sub
        End Class
    </file>
</compilation>
            CreateCompilationWithMscorlib40AndVBRuntime(source, Nothing, options:=TestOptions.ReleaseExe).VerifyDiagnostics()
        End Sub

        <Fact, WorkItem(543591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543591")>
        Public Sub MainInPrivateClass_2()
            Dim source =
<compilation>
    <file>
        Structure A
            Private Structure A
                Public Shared Sub Main()
                End Sub
            End Structure
        End Structure
        Module M1
            Public Sub Main()
            End Sub
        End Module
    </file>
</compilation>
            CreateCompilationWithMscorlib40AndVBRuntime(source, Nothing, options:=TestOptions.ReleaseExe).VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub PrivateMain()
            Dim source =
<compilation name="a">
    <file>
        Structure A
            Private Shared Sub Main()
            End Sub
        End Structure
        Module M1
            Private Sub Main()
            End Sub
        End Module
    </file>
</compilation>
            CreateCompilationWithMscorlib40AndVBRuntime(source, Nothing, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("a"))
        End Sub

        <Fact()>
        Public Sub MultipleEntryPoint_Inherit()
            Dim source =
<compilation name="a">
    <file>
        Class BaseClass
	        Public Shared Sub Main()
	        End Sub
        End Class
        Class Derived
	        Inherits BaseClass
	        Public Shared Overloads Sub Main()
	        End Sub
        End Class
    </file>
</compilation>
            CreateCompilationWithMscorlib40AndVBRuntime(source, Nothing, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_MoreThanOneValidMainWasFound2).WithArguments("a", "BaseClass.Main(), Derived.Main()"))
        End Sub

        <Fact()>
        Public Sub MainMustBeStatic()
            Dim source =
<compilation name="a">
    <file>
        Class BaseClass
            Public Sub Main()
            End Sub
        End Class
        Structure Derived
            Public Function Main(args As String()) As Integer
                Return Nothing
            End Function
        End Structure
    </file>
</compilation>
            CreateCompilationWithMscorlib40AndVBRuntime(source, Nothing, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("a"))
        End Sub

        <Fact()>
        Public Sub MainAsTypeName()
            Dim source =
<compilation name="a">
    <file>
        Class Main
            Shared Sub New()
            End Sub
        End Class
    </file>
</compilation>
            CreateCompilationWithMscorlib40AndVBRuntime(source, Nothing, options:=TestOptions.ReleaseExe).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("a"))
        End Sub

        <Fact()>
        Public Sub ExplicitMainTypeName_MainIsNotStatic()
            Dim source =
<compilation>
    <file>
Class Main
    Sub Main()
    End Sub
End Class
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("Main")).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("Main"))
        End Sub

        <Fact()>
        Public Sub ExplicitMainTypeName_ClassNameIsEmpty()
            Dim source =
<compilation>
    <file>
Class Main
    shared Sub Main()
    End Sub
End Class
    </file>
</compilation>
            AssertTheseDiagnostics(CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("")),
                                                                 <expected>
BC2014: the value '' is invalid for option 'MainTypeName'
                                                                 </expected>)

        End Sub

        <Fact()>
        Public Sub ExplicitMainTypeName_NoMainInClass()
            Dim source =
<compilation>
    <file>
Class Main
End Class
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("Main")).VerifyDiagnostics(
                 Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("Main"))

        End Sub

        <Fact()>
        Public Sub ExplicitMainTypeName_NotCaseSensitive()
            Dim source =
<compilation>
    <file>
Class Main
    shared Sub Main()
    End Sub
End Class
    </file>
</compilation>
            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("main")).VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub ExplicitMainTypeName_Extension()
            Dim source =
<compilation>
    <file>
        Imports System.Runtime.CompilerServices
        Class B
        End Class
        Module Extension
            &lt;Extension()&gt;
            Public Sub Main(x As B, args As String())
            End Sub
        End Module
    </file>
</compilation>
            CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {Net40.SystemCore}, options:=TestOptions.ReleaseExe.WithMainTypeName("B")).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("B"))

            CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {Net40.SystemCore}, options:=TestOptions.ReleaseExe.WithMainTypeName("Extension")).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InValidSubMainsFound1).WithArguments("Extension"))
        End Sub

        <Fact()>
        Public Sub ExplicitMainTypeName_InvalidType()
            Dim source =
<compilation>
    <file>
        Interface i1
            Sub main()
        End Interface
        Enum color
            blue
        End Enum
        Delegate Sub mydelegate(args As String()) 
    </file>
</compilation>
            CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {Net40.SystemCore}, options:=TestOptions.ReleaseExe.WithMainTypeName("I1")).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("i1"))

            CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {Net40.SystemCore}, options:=TestOptions.ReleaseExe.WithMainTypeName("COLOR")).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("color"))

            CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {Net40.SystemCore}, options:=TestOptions.ReleaseExe.WithMainTypeName("mydelegate")).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("mydelegate"))
        End Sub

        <Fact()>
        Public Sub ExplicitMainTypeName_Numeric()
            Dim source =
<compilation>
    <file>
        class a
        end class
    </file>
</compilation>
            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("1")).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("1"))

        End Sub

        <Fact()>
        Public Sub ExplicitMainTypeName_InvalidChar()
            Dim source =
<compilation>
    <file>
        class a
        end class
    </file>
</compilation>
            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("<")).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("<"))

        End Sub

        <WorkItem(545803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545803")>
        <Fact()>
        Public Sub ExplicitMainTypeName_PublicInBase()
            Dim source =
<compilation>
    <file>
        Class A
            Public Shared Sub Main()
            End Sub
        End Class

        Class B
            Inherits A
        End Class
    </file>
</compilation>
            Dim compilation As VisualBasicCompilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("B"))
            compilation.VerifyDiagnostics()
            Assert.Equal(compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("A").GetMember(Of MethodSymbol)("Main"),
                         compilation.GetEntryPoint(Nothing))
        End Sub

        <WorkItem(545803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545803")>
        <Fact()>
        Public Sub ExplicitMainTypeName_ProtectedInBase()
            Dim source =
<compilation>
    <file>
        Class A
            Protected Shared Sub Main()
            End Sub
        End Class

        Class B
            Inherits A
        End Class
    </file>
</compilation>
            Dim compilation As VisualBasicCompilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("B"))
            compilation.VerifyDiagnostics()
            Assert.Equal(compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("A").GetMember(Of MethodSymbol)("Main"),
                         compilation.GetEntryPoint(Nothing))
        End Sub

        <WorkItem(545803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545803")>
        <Fact()>
        Public Sub ExplicitMainTypeName_PrivateInBase()
            Dim source =
<compilation>
    <file>
        Class A
            Private Shared Sub Main()
            End Sub
        End Class

        Class B
            Inherits A
        End Class
    </file>
</compilation>
            Dim compilation As VisualBasicCompilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("B"))
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("B"))
            Assert.Null(compilation.GetEntryPoint(Nothing))
        End Sub

        <WorkItem(545803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545803")>
        <Fact()>
        Public Sub ExplicitMainTypeName_InGenericBase()
            Dim source =
<compilation>
    <file>
        Class A(Of T)
            Public Shared Sub Main()
            End Sub
        End Class

        Class B
            Inherits A(Of Integer)
        End Class
    </file>
</compilation>
            Dim compilation As VisualBasicCompilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("B"))
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_GenericSubMainsFound1).WithArguments("B"))
            Assert.Null(compilation.GetEntryPoint(Nothing))
        End Sub

        <WorkItem(545803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545803")>
        <Fact()>
        Public Sub ExplicitMainTypeName_InBaseHiddenByField()
            Dim source =
<compilation>
    <file>
        Class A
            Public Shared Sub Main()
            End Sub
        End Class

        Class B
            Inherits A
            
            Shadows Dim Main as Integer
        End Class
    </file>
</compilation>
            Dim compilation As VisualBasicCompilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("B"))
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("B"))
            Assert.Null(compilation.GetEntryPoint(Nothing))
        End Sub

        <WorkItem(545803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545803")>
        <Fact()>
        Public Sub ExplicitMainTypeName_InBaseInOtherAssembly()
            Dim source1 =
<compilation>
    <file>
        Public Class A
            Public Shared Sub Main()
            End Sub
        End Class
    </file>
</compilation>
            Dim source2 =
<compilation>
    <file>
        Class B
            Inherits A
        End Class
    </file>
</compilation>
            Dim compilation1 As VisualBasicCompilation = CreateCompilationWithMscorlib40(source1)
            Dim compilation2 As VisualBasicCompilation = CreateCompilationWithMscorlib40AndReferences(source2, {New VisualBasicCompilationReference(compilation1)}, options:=TestOptions.ReleaseExe.WithMainTypeName("B"))
            compilation2.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("B"))
            Assert.Null(compilation2.GetEntryPoint(Nothing))
        End Sub

        <WorkItem(630763, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/630763")>
        <Fact()>
        Public Sub Bug630763()
            Dim source =
<compilation>
    <file>
Public Class C
    Shared Function Main() As Integer
        Return 0
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe)
            compilation.VerifyDiagnostics()

            Dim netModule = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseModule)

            compilation = CreateCompilationWithMscorlib40AndReferences(
<compilation name="Bug630763">
    <file>
    </file>
</compilation>, {netModule.EmitToImageReference()}, options:=TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30420: 'Sub Main' was not found in 'Bug630763'.
</expected>)
        End Sub

        <WorkItem(753028, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/753028")>
        <Fact>
        Public Sub RootMemberNamedScript()
            Dim comp As VisualBasicCompilation

            comp = CompilationUtils.CreateCompilationWithMscorlib40(options:=TestOptions.ReleaseExe, source:=
<compilation name="20781949-2709-424e-b174-dec81a202016">
    <file name="a.vb">
Namespace Script
End Namespace
    </file>
</compilation>)
            comp.AssertTheseDiagnostics(<errors>
BC30420: 'Sub Main' was not found in '20781949-2709-424e-b174-dec81a202016'.
</errors>)

            comp = CompilationUtils.CreateCompilationWithMscorlib40(options:=TestOptions.ReleaseExe, source:=
<compilation name="20781949-2709-424e-b174-dec81a202017">
    <file name="a.vb">
Class Script
End Class
    </file>
</compilation>)
            comp.AssertTheseDiagnostics(<errors>
BC30420: 'Sub Main' was not found in '20781949-2709-424e-b174-dec81a202017'.
</errors>)

            comp = CompilationUtils.CreateCompilationWithMscorlib40(options:=TestOptions.ReleaseExe, source:=
<compilation name="20781949-2709-424e-b174-dec81a202018">
    <file name="a.vb">
Structure Script
End Structure
    </file>
</compilation>)
            comp.AssertTheseDiagnostics(<errors>
BC30420: 'Sub Main' was not found in '20781949-2709-424e-b174-dec81a202018'.
</errors>)

            comp = CompilationUtils.CreateCompilationWithMscorlib40(options:=TestOptions.ReleaseExe, source:=
<compilation name="20781949-2709-424e-b174-dec81a202019">
    <file name="a.vb">
Interface Script(Of T)
End Interface
    </file>
</compilation>)
            comp.AssertTheseDiagnostics(<errors>
BC30420: 'Sub Main' was not found in '20781949-2709-424e-b174-dec81a202019'.
</errors>)

            comp = CompilationUtils.CreateCompilationWithMscorlib40(options:=TestOptions.ReleaseExe, source:=
<compilation name="20781949-2709-424e-b174-dec81a202020">
    <file name="a.vb">
Enum Script
    A
End Enum
    </file>
</compilation>)
            comp.AssertTheseDiagnostics(<errors>
BC30420: 'Sub Main' was not found in '20781949-2709-424e-b174-dec81a202020'.
</errors>)

            comp = CompilationUtils.CreateCompilationWithMscorlib40(options:=TestOptions.ReleaseExe, source:=
<compilation name="20781949-2709-424e-b174-dec81a202021">
    <file name="a.vb">
Delegate Sub Script()
    </file>
</compilation>)
            comp.AssertTheseDiagnostics(<errors>
BC30420: 'Sub Main' was not found in '20781949-2709-424e-b174-dec81a202021'.
</errors>)
        End Sub

    End Class
End Namespace
