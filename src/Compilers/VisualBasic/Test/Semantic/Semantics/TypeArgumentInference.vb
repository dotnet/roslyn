' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class TypeArgumentInference
        Inherits BasicTestBase

        <Fact>
        Public Sub Test1()
            Dim compilationDef =
<compilation name="TypeArgumentInference1">
    <file name="a.vb">
Imports System.Collections.Generic

Module Module1

    Sub Main()
        M1(1, Nothing)
        M2(2S, Nothing)
        M3(New Double() {}, Nothing)
        M4(New Dictionary(Of Byte, Boolean)(), Nothing, Nothing)

        M1(1, y:=Nothing)
        M2(2S, y:=Nothing)
        M3(New Double() {}, y:=Nothing)
        M4(New Dictionary(Of Byte, Boolean)(), y:=Nothing, z:=Nothing)

        M1(x:=1, y:=Nothing)
        M2(x:=2S, y:=Nothing)
        M3(x:=New Double() {}, y:=Nothing)
        M4(x:=New Dictionary(Of Byte, Boolean)(), y:=Nothing, z:=Nothing)

        M1(y:=Nothing, x:=1)
        M2(y:=Nothing, x:=2S)
        M3(y:=Nothing, x:=New Double() {})
        M4(y:=Nothing, z:=Nothing, x:=New Dictionary(Of Byte, Boolean)())
    End Sub

    Sub M1(Of T)(x As T, y As T)
        System.Console.WriteLine(x.GetType())
    End Sub

    Sub M2(Of T)(ByRef x As T, y As T)
        System.Console.WriteLine(x.GetType())
    End Sub

    Sub M3(Of T)(x As T(), y As T)
        System.Console.WriteLine(x.GetType())
    End Sub

    Sub M4(Of T, S)(x As Dictionary(Of T, S), y As T, z As S)
        System.Console.WriteLine(x.GetType())
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
System.Int32
System.Int16
System.Double[]
System.Collections.Generic.Dictionary`2[System.Byte,System.Boolean]
System.Int32
System.Int16
System.Double[]
System.Collections.Generic.Dictionary`2[System.Byte,System.Boolean]
System.Int32
System.Int16
System.Double[]
System.Collections.Generic.Dictionary`2[System.Byte,System.Boolean]
System.Int32
System.Int16
System.Double[]
System.Collections.Generic.Dictionary`2[System.Byte,System.Boolean]
]]>)
        End Sub

        <Fact>
        Public Sub Test2()
            Dim compilationDef =
<compilation name="TypeArgumentInference2">
    <file name="a.vb">
Module Module1

    Sub Main()
        M1(1I)
        M1(1.5, 1S, 2I)
        M1(New Date() {})
    End Sub

    Sub M1(Of T)(ParamArray x As T())
        System.Console.WriteLine(x.GetType())
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
System.Int32[]
System.Double[]
System.DateTime[]
]]>)
        End Sub

        <Fact>
        Public Sub Test3()
            Dim compilationDef =
<compilation name="TypeArgumentInference3">
    <file name="a.vb">
Imports System.Collections.Generic

Module Module1

    Sub Main()
        M1(New Dictionary(Of Byte, Integer)())

        Dim x As New Test1(Of Integer)
        Dim y As IDerived(Of Long, Byte) = Nothing

        x.Goo(y)
    End Sub

    Sub M1(Of T, S)(x As IDictionary(Of T, S))
        System.Console.WriteLine(CObj(x).GetType())
    End Sub

End Module

Interface IBase(Of T, S)
End Interface

Interface IDerived(Of T, S)
    Inherits IBase(Of T, S)

End Interface

Class Test1(Of T)
    Sub Goo(Of S)(x As IBase(Of T, S))
        Dim x1 As T = Nothing
        Dim x2 As S = Nothing

        System.Console.WriteLine(CObj(x1).GetType())
        System.Console.WriteLine(CObj(x2).GetType())
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
System.Collections.Generic.Dictionary`2[System.Byte,System.Int32]
System.Int32
System.Byte
]]>)
        End Sub

        <Fact>
        Public Sub Test4()
            Dim compilationDef =
<compilation name="TypeArgumentInference3">
    <file name="a.vb">
Module Module1

    Sub Main()
        Dim x As New Test1(Of Integer)
        Dim y As IDerived(Of Long, Byte) = Nothing

        x.Goo(y)
    End Sub

End Module

Class IBase(Of T, S)
End Class

Class IDerived(Of T, S)
    Inherits IBase(Of T, S)

End Class

Class Test1(Of T)
    Sub Goo(Of S)(x As IBase(Of T, S))
        Dim x1 As T = Nothing
        Dim x2 As S = Nothing

        System.Console.WriteLine(x1.GetType())
        System.Console.WriteLine(x2.GetType())
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'IDerived(Of Long, Byte)' cannot be converted to 'IBase(Of Integer, Byte)'.
        x.Goo(y)
              ~
</expected>)
        End Sub

        <Fact>
        Public Sub Test5()
            Dim compilationDef =
<compilation name="TypeArgumentInference3">
    <file name="a.vb">
Option Strict On

Imports System

Module Module1

    Sub Main()
        Dim x As Integer

        x = M1(x)
    End Sub

    Function M1(Of T As Structure)(x As Nullable(Of T)) As Nullable(Of T)
        Return Nothing
    End Function

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30512: Option Strict On disallows implicit conversions from 'Integer?' to 'Integer'.
        x = M1(x)
            ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestLambda1()
            Dim compilationDef =
<compilation name="TypeArgumentInferenceLambda1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Sub Main()
        M1(Function() Nothing)

        M1(Function()
               Return Nothing
           End Function)
    End Sub

    Sub M1(Of T)(x As Func(Of T))
        System.Console.WriteLine(x.GetType())
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Object]
System.Func`1[System.Object]
]]>)
        End Sub

        <Fact>
        Public Sub TestLambda2()
            Dim compilationDef =
<compilation name="TypeArgumentInferenceLambda2">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Sub Main()

        Dim o As New Test(Of Byte)

        o.M1(Function(i) Nothing, "")

        o.M1(Function(i)
                 Return Nothing
             End Function, "")
    End Sub

End Module


Class Test(Of U)
    Sub M1(Of T, S)(x As Func(Of T, S), y As T)
        System.Console.WriteLine(x.GetType())
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
System.Func`2[System.String,System.Object]
System.Func`2[System.String,System.Object]
]]>)
        End Sub

        <Fact>
        Public Sub TestLambda3()
            Dim compilationDef =
<compilation name="TypeArgumentInferenceLambda3">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Sub Main()

        M1(Function(i) 1, 1.5)

        M1(Function(i)
               If i > 0 Then
                   Return 1
               Else
                   Return -1L
               End If
           End Function, 1.5)

        M1(Function(i) As SByte
               If i > 0 Then
                   Return 1
               Else
                   Return -1L
               End If
           End Function, 1.5)

        M2(Function(i As Byte) 1)

        M2(Function(i As Short)
               If i > 0 Then
                   Return 1
               Else
                   Return -1L
               End If
           End Function)
    End Sub

    Sub M1(Of T, S)(x As Func(Of T, S), y As T)
        System.Console.WriteLine(x.GetType())
    End Sub

    Sub M2(Of T, S)(x As Func(Of T, S))
        System.Console.WriteLine(x.GetType())
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
System.Func`2[System.Double,System.Int32]
System.Func`2[System.Double,System.Int64]
System.Func`2[System.Double,System.SByte]
System.Func`2[System.Byte,System.Int32]
System.Func`2[System.Int16,System.Int64]
]]>)
        End Sub

        <Fact(), WorkItem(545209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545209")>
        Public Sub TestLambda4()
            Dim compilationDef =
<compilation name="TypeArgumentInferenceLambda4">
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Module M
    Sub Goo(Of T)(ParamArray a As Action(Of List(Of T))())
        System.Console.WriteLine(GetType(T))
    End Sub

    Sub Main()
        Goo({Sub(x As IList(Of String)) Exit Sub}) 
        Goo(Sub(x As IList(Of String)) Exit Sub) 
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertNoErrors(compilation)

            CompileAndVerify(compilation,
            <![CDATA[
System.String
System.String
]]>)
        End Sub

        <Fact>
        Public Sub TestResolutionBasedOnInferenceKind1()
            Dim compilationDef =
<compilation name="TestResolutionBasedOnInferenceKind1">
    <file name="a.vb">
Option Strict Off

Module Module1

    Sub Main()
        Dim val As Integer = 0

        M1(1, Function(x As Integer) As Integer
                  Return 2
              End Function, 1, val)
    End Sub

    Sub M1(Of T, U)(x As T, y As System.Func(Of Integer, Integer), z As U, ParamArray v() As Long)
        System.Console.WriteLine(1)
    End Sub

    Sub M1(Of T)(x As Integer, y As System.Func(Of Integer, T), z As Integer, v As Integer)
        System.Console.WriteLine(2)
    End Sub

    'Sub M1(Of T, U)(x As T, y As System.Func(Of Integer, T), z As U, v As Long)
    '    System.Console.WriteLine(3)
    'End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
1
]]>)
        End Sub

        <Fact>
        Public Sub TestResolutionBasedOnInferenceKind2()
            Dim compilationDef =
<compilation name="TestResolutionBasedOnInferenceKind2">
    <file name="a.vb">
Option Strict Off

Module Module1

    Sub Main()
        Dim val As Integer = 0

        M1(1, Function(x As Integer) As Integer
                  Return 2
              End Function, 1, val)
    End Sub

    Sub M1(Of T, U)(x As T, y As System.Func(Of Integer, Integer), z As U, ParamArray v() As Long)
        System.Console.WriteLine(1)
    End Sub

    Sub M1(Of T)(x As Integer, y As System.Func(Of Integer, T), z As Integer, v As Integer)
        System.Console.WriteLine(2)
    End Sub

    Sub M1(Of T, U)(x As T, y As System.Func(Of Integer, T), z As U, v As Long)
        System.Console.WriteLine(3)
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
2
]]>)
        End Sub

        <Fact>
        Public Sub TestResolutionBasedOnInferenceKind3()
            Dim compilationDef =
<compilation name="TestResolutionBasedOnInferenceKind3">
    <file name="a.vb">
Option Strict Off

Module Module1

    Sub Main()
        Dim val As Integer = 0

        M1(1, Function(x As Integer) As Integer
                  Return 2
              End Function, 1, v:=val)
    End Sub

    Sub M1(Of T, U)(x As T, y As System.Func(Of Integer, Integer), z As U, v As Long, ParamArray vv() As Long)
        System.Console.WriteLine(1)
    End Sub

    Sub M1(Of T)(x As Integer, y As System.Func(Of Integer, T), z As Integer, v As Integer)
        System.Console.WriteLine(2)
    End Sub

    'Sub M1(Of T, U)(x As T, y As System.Func(Of Integer, T), z As U, v As Long)
    '    System.Console.WriteLine(3)
    'End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
1
]]>)
        End Sub

        <Fact>
        Public Sub TestResolutionBasedOnInferenceKind4()
            Dim compilationDef =
<compilation name="TestResolutionBasedOnInferenceKind4">
    <file name="a.vb">
Option Strict Off

Module Module1

    Sub Main()
        Dim val As Integer = 0

        M1(1, Function(x As Integer) As Integer
                  Return 2
              End Function, 1, v:=val)
        M1(1, Function(x As Integer) As Integer
                  Return 2
              End Function, 1, val)
    End Sub

    Sub M1(Of T, U)(x As T, y As System.Func(Of Integer, Integer), z As U, v As Long, ParamArray vv() As Long)
        System.Console.WriteLine(1)
    End Sub

    Sub M1(Of T)(x As Integer, y As System.Func(Of Integer, T), z As Integer, v As Integer)
        System.Console.WriteLine(2)
    End Sub

    Sub M1(Of T, U)(x As T, y As System.Func(Of Integer, T), z As U, v As Long)
        System.Console.WriteLine(3)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:=
"2
2")
        End Sub

        <Fact>
        Public Sub ERRID_UnboundTypeParam2()
            Dim compilationDef =
<compilation name="ERRID_UnboundTypeParam2">
    <file name="a.vb">
Module GM
    Public Function Fred1(Of T1, T2)(P As T1) As T2
        Return Fred1(3)
    End Function
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC32050: Type parameter 'T2' for 'Public Function Fred1(Of T1, T2)(P As T1) As T2' cannot be inferred.
        Return Fred1(3)
               ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ERRID_UnboundTypeParam1()
            Dim compilationDef =
<compilation name="ERRID_UnboundTypeParam1">
    <file name="a.vb">
Module GM
    Public Function Fred2(Of T2)(P As Object) As T2
        Return Fred2(3)
    End Function

    Public Function Fred2(Of T1, T2)(P As T1) As T2
        return Nothing
    End Function
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30518: Overload resolution failed because no accessible 'Fred2' can be called with these arguments:
    'Public Function Fred2(Of T2)(P As Object) As T2': Type parameter 'T2' cannot be inferred.
    'Public Function Fred2(Of T1, T2)(P As T1) As T2': Type parameter 'T2' cannot be inferred.
        Return Fred2(3)
               ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ERRID_TypeInferenceFailureAmbiguous2()
            Dim compilationDef =
<compilation name="ERRID_TypeInferenceFailureAmbiguous2">
    <file name="a.vb">
Module GM
    Public Function barney1(Of T)(p As T, p1 As T) As T
        Return barney1(3, "Zip")
    End Function
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36651: Data type(s) of the type parameter(s) in method 'Public Function barney1(Of T)(p As T, p1 As T) As T' cannot be inferred from these arguments because more than one type is possible. Specifying the data type(s) explicitly might correct this error.
        Return barney1(3, "Zip")
               ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ERRID_TypeInferenceFailureAmbiguous1()
            Dim compilationDef =
<compilation name="ERRID_TypeInferenceFailureAmbiguous1">
    <file name="a.vb">
Module GM
    Public Function barney2(Of T)(p As T, p1 As T, x As Integer) As T
        Return barney2(3, "Zip", 1)
    End Function

    Public Function barney2(Of T)(p As T, p1 As T, x As UInteger) As T
        Return Nothing
    End Function
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30518: Overload resolution failed because no accessible 'barney2' can be called with these arguments:
    'Public Function barney2(Of T)(p As T, p1 As T, x As Integer) As T': Data type(s) of the type parameter(s) cannot be inferred from these arguments because more than one type is possible. Specifying the data type(s) explicitly might correct this error.
    'Public Function barney2(Of T)(p As T, p1 As T, x As UInteger) As T': Data type(s) of the type parameter(s) cannot be inferred from these arguments because more than one type is possible. Specifying the data type(s) explicitly might correct this error.
        Return barney2(3, "Zip", 1)
               ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ERRID_TypeInferenceFailureNoBest2()
            Dim compilationDef =
<compilation name="ERRID_TypeInferenceFailureNoBest2">
    <file name="a.vb">
Namespace Case1
    Class B
    End Class
    Class C
    End Class
    Class D
    End Class
End Namespace
Namespace Case2
    Class B
    End Class
    Class C
    End Class
    Class D
    End Class
End Namespace
Module Module2
    Sub Goo(Of T)(ByVal x As T, ByVal y As T)
    End Sub
    Sub Main()
        Goo(New Case1.B, New Case1.C)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36657: Data type(s) of the type parameter(s) in method 'Public Sub Goo(Of T)(x As T, y As T)' cannot be inferred from these arguments because they do not convert to the same type. Specifying the data type(s) explicitly might correct this error.
        Goo(New Case1.B, New Case1.C)
        ~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ERRID_TypeInferenceFailureNoBest1()
            Dim compilationDef =
<compilation name="ERRID_TypeInferenceFailureNoBest1">
    <file name="a.vb">
Namespace Case1
    Class B
    End Class
    Class C
    End Class
    Class D
    End Class
End Namespace
Namespace Case2
    Class B
    End Class
    Class C
    End Class
    Class D
    End Class
End Namespace
Module Module2
    Sub Goo(Of T)(ByVal x As T, ByVal y As T, z As Integer)
    End Sub
    Sub Goo(Of T)(ByVal x As T, ByVal y As T, z As UInteger)
    End Sub
    Sub Main()
        Goo(New Case1.B, New Case1.C, 1)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30518: Overload resolution failed because no accessible 'Goo' can be called with these arguments:
    'Public Sub Goo(Of T)(x As T, y As T, z As Integer)': Data type(s) of the type parameter(s) cannot be inferred from these arguments because they do not convert to the same type. Specifying the data type(s) explicitly might correct this error.
    'Public Sub Goo(Of T)(x As T, y As T, z As UInteger)': Data type(s) of the type parameter(s) cannot be inferred from these arguments because they do not convert to the same type. Specifying the data type(s) explicitly might correct this error.
        Goo(New Case1.B, New Case1.C, 1)
        ~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ERRID_TypeInferenceFailure2()
            Dim compilationDef =
<compilation name="ERRID_TypeInferenceFailure2">
    <file name="a.vb">
Module Module2
    Sub Sub3(Of X, Y)(ByVal p1 As Y, ByVal p2 As X)
    End Sub
    Sub Main()
        Sub3(10, Nothing)
    End Sub
End Module    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36645: Data type(s) of the type parameter(s) in method 'Public Sub Sub3(Of X, Y)(p1 As Y, p2 As X)' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Sub3(10, Nothing)
        ~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ERRID_TypeInferenceFailure1()
            Dim compilationDef =
<compilation name="ERRID_TypeInferenceFailure1">
    <file name="a.vb">
Module Module2
    Sub Sub3(Of X, Y)(ByVal p1 As Y, ByVal p2 As X, p3 As Integer)
    End Sub
    Sub Sub3(Of X, Y)(ByVal p1 As Y, ByVal p2 As X, p3 As UInteger)
    End Sub
    Sub Main()
        Sub3(10, Nothing, 1)
    End Sub
End Module    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30518: Overload resolution failed because no accessible 'Sub3' can be called with these arguments:
    'Public Sub Sub3(Of X, Y)(p1 As Y, p2 As X, p3 As Integer)': Data type(s) of the type parameter(s) cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
    'Public Sub Sub3(Of X, Y)(p1 As Y, p2 As X, p3 As UInteger)': Data type(s) of the type parameter(s) cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Sub3(10, Nothing, 1)
        ~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ERRID_StrictDisallowImplicitObjectLambda()
            Dim compilationDef =
<compilation name="ERRID_StrictDisallowImplicitObjectLambda">
    <file name="a.vb">
Imports System
        
Module Module2
    Sub HandleFuncOfTST(Of T, S)(x As T, del As Func(Of S, T))
    End Sub

    Sub Main()
        HandleFuncOfTST(20, Function(x) CInt(x) + 1000)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36642: Option Strict On requires each lambda expression parameter to be declared with an 'As' clause if its type cannot be inferred.
        HandleFuncOfTST(20, Function(x) CInt(x) + 1000)
                                     ~
</expected>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Off))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact>
        Public Sub InferFromAddressOf1()
            Dim compilationDef =
<compilation name="InferFromAddressOf1">
    <file name="a.vb">
Imports System

Module Module1

    Sub Main()
        M1(AddressOf M2, 1)
        M3(AddressOf M4)
    End Sub

    Sub M1(Of T, S)(x As Func(Of T, S), y As T)
        System.Console.WriteLine(x.GetType())
    End Sub

    Function M2(y As Integer) As Double
        Return 0
    End Function

    Sub M3(Of T)(x As Func(Of T))
        System.Console.WriteLine(x.GetType())
    End Sub

    Function M4() As Integer
        Return 0
    End Function

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
System.Func`2[System.Int32,System.Double]
System.Func`1[System.Int32]
]]>)
        End Sub

        <Fact>
        Public Sub InferFromAddressOf2()
            Dim compilationDef =
<compilation name="InferFromAddressOf2">
    <file name="a.vb">
Imports System

Module Module1

    Sub Main()
        M1(AddressOf M2)
        M3(AddressOf M4)
    End Sub

    Sub M1(Of T)(x As Action(Of Integer))
    End Sub

    Function M2(y As Integer) As Double
        Return 0
    End Function

    Sub M3(Of T)(x As Func(Of T))
    End Sub

    Sub M4()
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC32050: Type parameter 'T' for 'Public Sub M1(Of T)(x As Action(Of Integer))' cannot be inferred.
        M1(AddressOf M2)
        ~~
BC36645: Data type(s) of the type parameter(s) in method 'Public Sub M3(Of T)(x As Func(Of T))' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        M3(AddressOf M4)
        ~~
</expected>)
        End Sub

        <Fact>
        Public Sub InferForAddressOf1()
            Dim compilationDef =
<compilation name="InferForAddressOf1">
    <file name="a.vb">
Imports System

Module Module1

    Sub Main()
        Dim x As Func(Of Integer, Double) = AddressOf M1
        x.Invoke(1)
    End Sub

    Function M1(Of T)(y As Integer) As T
        System.Console.WriteLine(y)
        Return Nothing
    End Function

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="1")
        End Sub

        <Fact>
        Public Sub InferForAddressOf2()
            Dim compilationDef =
<compilation name="InferForAddressOf2">
    <file name="a.vb">
Imports System

Module Module1

    Sub Main()
        Dim x As Action(Of Integer) = AddressOf M1
    End Sub

    Function M1(Of T)(y As Integer) As T
        return Nothing
    End Function

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36564: Type arguments could not be inferred from the delegate.
        Dim x As Action(Of Integer) = AddressOf M1
                                                ~~
</expected>)
        End Sub

        <WorkItem(540950, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540950")>
        <Fact>
        Public Sub InferForAddressOf3()
            Dim source =
<compilation name="InferForAddressOf2">
    <file name="a.vb">
Imports System
 
Module Program
    Sub Main()
        Dim x As Func(Of Integer, Long) = AddressOf Goo
    End Sub
 
    Function Goo(Of T)(x As T) As T
        Return Nothing
    End Function
End Module
    </file>
</compilation>

            Dim comp1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Off))
            CompilationUtils.AssertNoErrors(comp1)
            Dim comp2 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertNoErrors(comp2)
        End Sub

        <WorkItem(540951, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540951")>
        <Fact>
        Public Sub InferForAddressOf4()
            Dim source =
<compilation name="InferForAddressOf2">
    <file name="a.vb">
Option Strict Off

Imports System
Imports System.Collections.Generic

Module Program
    Sub Main()
        Dim x As Func(Of List(Of Integer)) = AddressOf Goo
    End Sub

    Function Goo(Of T)() As IList(Of T)
        Return Nothing
    End Function
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(542040, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542040")>
        <Fact>
        Public Sub InferInPresenceOfOverloadsAndSubLambda()
            Dim source =
<compilation name="InferForAddressOf2">
    <file name="a.vb">
Option Strict Off
Imports System
Imports System.Collections.Generic
Module M1
    Sub goo(Of TT, UU, VV)(x As Func(Of TT, UU, VV),
                           y As Func(Of UU, VV, TT),
                           z As Func(Of VV, TT, UU))
    End Sub
    Sub goo(Of TT, UU)(xx As TT,
                       yy As UU,
                       zz As Action)
    End Sub
    Public Sub Test()
        goo(1, 2, Sub()
                  End Sub)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub AnonymousDelegates1()
            Dim compilationDef =
<compilation name="TypeArgumentInference3">
    <file name="a.vb">
Imports System
        
Class QueryAble(Of T)

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)()
    End Function

    Public Function Where(Of S)(x As S) As QueryAble(Of T)
        System.Console.WriteLine("Where {0}", x)
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q1 As New QueryAble(Of Integer)
        Dim q As Object
        q = From i In q1 Where i > 0
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36594: Definition of method 'Where' is not accessible in this context.
        q = From i In q1 Where i > 0
                         ~~~~~
BC36648: Data type(s) of the type parameter(s) in method 'Public Function Where(Of S)(x As S) As QueryAble(Of Integer)' cannot be inferred from these arguments.
        q = From i In q1 Where i > 0
                         ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AnonymousDelegates2()
            Dim compilationDef =
<compilation name="TypeArgumentInference3">
    <file name="a.vb">
Imports System
        
Class QueryAble(Of T)

    Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
        System.Console.WriteLine("Select {0}", x)
        Return New QueryAble(Of S)()
    End Function

    Public Function Where(Of S)(x As S) As QueryAble(Of T)
        System.Console.WriteLine("Where {0}", x)
        Return Me
    End Function
End Class

Module Module1

    Sub Main()
        Dim q1 As New QueryAble(Of Integer)
        Dim q As Object

        q = q1.Where(Function(x As Long) x)

        Dim f = Function(x As Double) x
        q = q1.Where(f)
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
Where VB$AnonymousDelegate_0`2[System.Int64,System.Int64]
Where VB$AnonymousDelegate_0`2[System.Double,System.Double]
]]>)
        End Sub

        <Fact()>
        Public Sub AnonymousDelegates3()
            Dim compilationDef =
<compilation name="TypeArgumentInference3">
    <file name="a.vb">
Imports System
        
Module Module1

    Sub Test1(Of T)(x As Func(Of T))
        System.Console.WriteLine(x)
    End Sub

    Sub Test2(Of T)(x As Action(Of T))
        System.Console.WriteLine(x)
    End Sub

    Sub Test3(Of T, S)(x As Func(Of T, S))
        System.Console.WriteLine(x)
    End Sub

    Sub Test4(Of T, S)(x As Func(Of T, S), y As T)
        System.Console.WriteLine(x)
    End Sub

    Sub Main()
        Dim d1 = Function() 1
        Test1(d1)

        Dim d2 = Sub(x As Long) System.Console.WriteLine(x)
        Test2(d2)

        Dim d3 = Function(x As Double)
                     System.Console.WriteLine(x)
                     Return CInt(x)
                 End Function
        Test3(d3)
        Test2(d3)

        Test4(d1, 2UI)
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Int32]
System.Action`1[System.Int64]
System.Func`2[System.Double,System.Int32]
System.Action`1[System.Double]
System.Func`2[System.UInt32,System.Int32]
]]>)
        End Sub

        <Fact()>
        Public Sub AnonymousDelegates4()
            Dim compilationDef =
<compilation name="TypeArgumentInference3">
    <file name="a.vb">
Imports System
        
Module Module1

    Sub Test1(Of T)(x As Func(Of T))
        System.Console.WriteLine(x)
    End Sub

    Delegate Sub Dt2(Of T)(ByRef x As T)
    Sub Test2(Of T)(x As Dt2(Of T))
        System.Console.WriteLine(x)
    End Sub

    Sub Test3(Of T, S)(x As Func(Of T, S))
        System.Console.WriteLine(x)
    End Sub

    Sub Test4(Of T, S)(x As Func(Of T, S), y As T)
        System.Console.WriteLine(x)
    End Sub

    Sub Main()
        Dim d1 = Function() 1
        Test3(d1)

        Dim d2 = Sub(x As Long) System.Console.WriteLine(x)
        Test2(d2)

        Dim d3 = Function(x As Double)
                     System.Console.WriteLine(x)
                     Return CInt(x)
                 End Function
        Test1(d3)

        Test4(d2, 1UI)
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36645: Data type(s) of the type parameter(s) in method 'Public Sub Test3(Of T, S)(x As Func(Of T, S))' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Test3(d1)
        ~~~~~
BC36645: Data type(s) of the type parameter(s) in method 'Public Sub Test2(Of T)(x As Module1.Dt2(Of T))' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Test2(d2)
        ~~~~~
BC36645: Data type(s) of the type parameter(s) in method 'Public Sub Test1(Of T)(x As Func(Of T))' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Test1(d3)
        ~~~~~
BC36645: Data type(s) of the type parameter(s) in method 'Public Sub Test4(Of T, S)(x As Func(Of T, S), y As T)' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Test4(d2, 1UI)
        ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub PropertyInByRefContext1()
            Dim compilationDef =
<compilation name="TypeArgumentInference3">
    <file name="a.vb">
Imports System
        
Module Module1

    Class B1
        Public Property B2 As B2
        Public Property B4 As B4
    End Class

    Class B2

        Shared Widening Operator CType(x As B2) As B3
            Return Nothing
        End Operator

        'Shared Widening Operator CType(x As B3) As B2
        '    Return Nothing
        'End Operator

    End Class

    Class B3
    End Class

    Class B4

        Shared Widening Operator CType(x As B4) As B3
            Return Nothing
        End Operator

    End Class

    Sub Test(Of T)(ByRef x As T, y As T, z As T)
        System.Console.WriteLine(GetType(T))
    End Sub

    Sub Main()

        Dim x As New B1
        Dim y As New B3

        Test(x.B2, y, x.B4)
        Test(Of B3)(x.B2, y, x.B4)
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36657: Data type(s) of the type parameter(s) in method 'Public Sub Test(Of T)(ByRef x As T, y As T, z As T)' cannot be inferred from these arguments because they do not convert to the same type. Specifying the data type(s) explicitly might correct this error.
        Test(x.B2, y, x.B4)
        ~~~~
BC33037: Cannot copy the value of 'ByRef' parameter 'x' back to the matching argument because type 'Module1.B3' cannot be converted to type 'Module1.B2'.
        Test(Of B3)(x.B2, y, x.B4)
                    ~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub PropertyInByRefContext2()
            Dim compilationDef =
<compilation name="TypeArgumentInference3">
    <file name="a.vb">
Imports System
        
Module Module1

    Class B1
        Public Property B2 As B2
        Public Property B4 As B4
    End Class

    Class B2

        Shared Widening Operator CType(x As B2) As B3
            Return Nothing
        End Operator

        Shared Widening Operator CType(x As B3) As B2
            Return Nothing
        End Operator

    End Class

    Class B3
    End Class

    Class B4

        Shared Widening Operator CType(x As B4) As B3
            Return Nothing
        End Operator

    End Class

    Sub Test(Of T)(ByRef x As T, y As T, z As T)
        System.Console.WriteLine(GetType(T))
    End Sub

    Sub Main()

        Dim x As New B1
        Dim y As New B3

        Test(x.B2, y, x.B4)
        Test(Of B3)(x.B2, y, x.B4)
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Module1+B3
Module1+B3
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact()>
        Public Sub PropertyInByRefContext2a()
            Dim compilationDef =
<compilation name="TypeArgumentInference3">
    <file name="a.vb">
Imports System
        
Module Module1

    Class B1
        Public readonly Property B2 As B2
        Public readonly Property B4 As B4
    End Class

    Class B2

        Shared Widening Operator CType(x As B2) As B3
            Return Nothing
        End Operator

        'Shared Widening Operator CType(x As B3) As B2
        '    Return Nothing
        'End Operator

    End Class

    Class B3
    End Class

    Class B4

        Shared Widening Operator CType(x As B4) As B3
            Return Nothing
        End Operator

    End Class

    Sub Test(Of T)(ByRef x As T, y As T, z As T)
        System.Console.WriteLine(GetType(T))
    End Sub

    Sub Main()
        Dim x As New B1
        Dim y As New B3

        Test(x.B2, y, x.B4)
        Test(Of B3)(x.B2, y, x.B4)
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Module1+B3
Module1+B3
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact()>
        Public Sub PropertyInByRefContext2b()
            Dim compilationDef =
<compilation name="TypeArgumentInference3">
    <file name="a.vb">
Imports System
        
Module Module1

    Class B1
        Public readonly Property B2 As B2
        Public readonly Property B4 As B4

        public sub new
            Dim y As New B3

            Test(me.B2, y, me.B4)
            Test(Of B3)(me.B2, y, me.B4)
        end sub
    End Class

    Class B2

        Shared Widening Operator CType(x As B2) As B3
            Return Nothing
        End Operator

        'Shared Widening Operator CType(x As B3) As B2
        '    Return Nothing
        'End Operator

    End Class

    Class B3
    End Class

    Class B4

        Shared Widening Operator CType(x As B4) As B3
            Return Nothing
        End Operator

    End Class

    Sub Test(Of T)(ByRef x As T, y As T, z As T)
        System.Console.WriteLine(GetType(T))
    End Sub

    Sub Main()
        dim o as new B1
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
    BC36657: Data type(s) of the type parameter(s) in method 'Public Sub Test(Of T)(ByRef x As T, y As T, z As T)' cannot be inferred from these arguments because they do not convert to the same type. Specifying the data type(s) explicitly might correct this error.
            Test(me.B2, y, me.B4)
            ~~~~
BC33037: Cannot copy the value of 'ByRef' parameter 'x' back to the matching argument because type 'Module1.B3' cannot be converted to type 'Module1.B2'.
            Test(Of B3)(me.B2, y, me.B4)
                        ~~~~~
</expected>)
        End Sub

        <Fact, WorkItem(545092, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545092")>
        Public Sub Bug13357()
            Dim compilationDef =
<compilation name="TypeArgumentInference1">
    <file name="a.vb">
Imports System

Module Module1
    Sub Main(args As String())
        Test(New MethodCompiler, New NamespaceSymbol, New SyntaxTree)
    End Sub

    Sub Test(compiler As MethodCompiler, root As NamespaceSymbol, tree As SyntaxTree)
        root.Accept(compiler, Function(sym) sym Is root OrElse sym.IsDefinedInSourceTree(tree))
        root.Accept(compiler, Function(sym) sym Is root)
    End Sub
End Module

Friend MustInherit Class SymbolVisitor(Of TArgument, TResult)

End Class

Class SyntaxTree
End Class

Class Symbol
    Friend Overridable Function IsDefinedInSourceTree(tree As SyntaxTree) As Boolean
        Return False
    End Function
End Class

Class NamespaceSymbol
    Inherits Symbol

    Friend Function Accept(Of TArgument, TResult)(visitor As SymbolVisitor(Of TArgument, TResult), arg As TArgument) As TResult
        System.Console.WriteLine(GetType(TArgument))
        System.Console.WriteLine(GetType(TResult))
        Return Nothing
    End Function

End Class

Friend Class MethodCompiler
    Inherits SymbolVisitor(Of Predicate(Of Symbol), Boolean)

End Class    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
System.Predicate`1[Symbol]
System.Boolean
System.Predicate`1[Symbol]
System.Boolean    
]]>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
        Public Sub Regress14477()
            Dim compilationDef =
<compilation name="Regress14477">
    <file name="a.vb">
Option Strict Off

Imports System
Imports System.Diagnostics
Imports System.Reflection
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Dim x As Object = New List(Of Integer)
        Try
            fun(x)
        Catch e As Exception
            Console.WriteLine(e)
        End Try
    End Sub

    Sub fun(Of X)(ByVal a As List(Of X))
        Console.WriteLine("X: " &amp; GetType(X).FullName)

        Dim lateBound As Boolean = False

        For Each frame As StackFrame In New StackTrace().GetFrames()
            If (frame.GetMethod().Name = "Invoke") Then
                lateBound = True
            End If
        Next

        If (lateBound) Then
            Console.WriteLine("LATE BOUND")
        Else
            Console.WriteLine("NOT latebound")
        End If
    End Sub

End Module

    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
X: System.Int32
LATE BOUND
]]>)
        End Sub

        <Fact, WorkItem(545812, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545812")>
        Public Sub Bug14478()
            Dim compilationDef =
<compilation name="TypeArgumentInference2">
    <file name="a.vb">
Option Strict On
 
Imports System

Public Module Test
 
    Delegate Function Func(Of T)() As T
    Delegate Function Func(Of A0, T)(arg0 As A0) As T
 
 
    Public Sub Main()
        TestNormal()
    End Sub
    Sub TestNormal()
        f5(AddressOf t5)
    End Sub
 
    '-----------------------------------
    Sub f5(Of T)(a1 As Func(Of Integer, T))
        Console.WriteLine("f5 - T: (" + GetType(T).FullName + ")")
    End Sub
 
    ' useless to infer on
    Function t5(Of S)(a1 As Integer) As S
        Return Nothing
    End Function
    ' useful to infer on
    Function t5(Of S)(a1 As S) As S
        Return Nothing
    End Function
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
f5 - T: (System.Int32)    
]]>)
        End Sub

        <Fact, WorkItem(545812, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545812")>
        Public Sub Bug14478_2()
            Dim compilationDef =
<compilation name="TypeArgumentInference2">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1
            Sub Scen2(Of R As S, S)(ByVal x As R, ByVal y As S)
                Dim w As Func(Of R, R) = AddressOf Scen2(Of R)

                Gen1A(y, x, w)
            End Sub

            Sub Gen1A(Of T, U)(ByVal x As T, ByVal y As U, ByVal z As Func(Of T, U))
                Console.WriteLine("Gen1A: " & GetType(T).FullName & " - " & GetType(U).FullName)
                z(x)
            End Sub

            Function Scen2(Of K)(ByVal x As K) As K
                Console.WriteLine("scen2: " & GetType(K).FullName)
            End Function

            Sub Main()
                Scen2(New CB(), CType(New CB(), CA))
            End Sub
        End Module

        Class CA
        End Class

        Class CB : Inherits CA
        End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
Gen1A: CB - CB
scen2: CB
]]>)
        End Sub

        <Fact, WorkItem(629539, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629539")>
        Public Sub Bug629539()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Collections.Generic
Imports System.Linq

Class SnapshotSpan
    Public Property Length As Integer
    Public Property Text As String
    Public Function CreateTrackingSpan() As ITrackingSpan
        Return Nothing
    End Function
End Class
Public Interface ITrackingSpan
End Interface
Module Program
    Sub Main()
        Dim sourceSpans = New List(Of SnapshotSpan)().AsReadOnly()
        Dim replacementSpans = sourceSpans.Select(Function(ss)
                                                      If ss.Length = 2 Then
                                                          Return ss.Text
                                                      Else
                                                          Return ss.CreateTrackingSpan()
                                                      End If
                                                  End Function).ToList()
    End Sub
End Module


    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemCoreRef}, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompileAndVerify(compilation)

            AssertTheseDiagnostics(compilation,
<expected>
BC42021: Cannot infer a return type because more than one type is possible; 'Object' assumed.
        Dim replacementSpans = sourceSpans.Select(Function(ss)
                                                  ~~~~~~~~~~~~
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            AssertTheseDiagnostics(compilation,
<expected>
BC36734: Cannot infer a return type because more than one type is possible. Consider adding an 'As' clause to specify the return type.
        Dim replacementSpans = sourceSpans.Select(Function(ss)
                                                  ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact, WorkItem(811902, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/811902")>
        Public Sub Bug811902()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Sub Baz(a As Action)
    End Sub
 
    Sub Baz(Of T)(a As Func(Of T))
    End Sub
 
    Sub Goo(ByRef a As Integer)
        Baz(Sub()
                Console.WriteLine(a)
            End Sub)
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation,
<expected>
BC36639: 'ByRef' parameter 'a' cannot be used in a lambda expression.
                Console.WriteLine(a)
                                  ~
</expected>)
        End Sub

        <Fact>
        <WorkItem(22329, "https://github.com/dotnet/roslyn/issues/22329")>
        Public Sub ShapeMismatchInOneArgument_01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class C3
    Shared Sub Main()
        Test2(Nullable(New VT(Of Integer, Integer)()), New VT(Of Integer, Integer)())
        Test2(New VT(Of Integer, Integer)(), Nullable(New VT(Of Integer, Integer)()))
    End Sub
    Shared Function Nullable(Of T As Structure)(x As T) As T?
        Return x
    End Function
    Shared Sub Test2(Of T, U)(x As VT(Of T, U), y As VT(Of T, U))
        System.Console.Write(1) 
    End Sub
    Public Structure VT(Of T, S)
    End Structure
End Class
    </file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC36645: Data type(s) of the type parameter(s) in method 'Public Shared Sub Test2(Of T, U)(x As C3.VT(Of T, U), y As C3.VT(Of T, U))' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Test2(Nullable(New VT(Of Integer, Integer)()), New VT(Of Integer, Integer)())
        ~~~~~
BC36645: Data type(s) of the type parameter(s) in method 'Public Shared Sub Test2(Of T, U)(x As C3.VT(Of T, U), y As C3.VT(Of T, U))' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Test2(New VT(Of Integer, Integer)(), Nullable(New VT(Of Integer, Integer)()))
        ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(22329, "https://github.com/dotnet/roslyn/issues/22329")>
        Public Sub ShapeMismatchInOneArgument_02()
            Dim verifier = CompileAndVerify(
<compilation>
    <file name="a.vb">
Public Class C3
    Shared Sub Main()
        Test2(Nullable(New VT(Of Integer, Integer)()), New VT(Of Integer, Integer)())
        Test2(New VT(Of Integer, Integer)(), Nullable(New VT(Of Integer, Integer)()))
    End Sub
    Shared Function Nullable(Of T As Structure)(x As T) As T?
        Return x
    End Function
    Shared Sub Test2(Of T)(x As T, y As T)
        System.Console.Write(1) 
    End Sub
    Public Structure VT(Of T, S)
    End Structure
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
11
]]>)
        End Sub

    End Class
End Namespace

