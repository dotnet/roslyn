' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.OverloadResolution
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class ConditionalAccessTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub DisabledIfNotExperimental()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
    End Sub

    Sub Test2(x As S1?)
        System.Console.WriteLine(x?.M2())
        System.Console.WriteLine(?.M2())
        ?.M2()
    End Sub
End Module

Structure S1
    Sub M2()
        System.Console.WriteLine("S1.M2")
    End Sub
End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC36637: The '?' character cannot be used here.
        System.Console.WriteLine(x?.M2())
                                  ~
BC30201: Expression expected.
        System.Console.WriteLine(?.M2())
                                 ~
BC36637: The '?' character cannot be used here.
        System.Console.WriteLine(?.M2())
                                 ~
BC31003: Expression statement is only allowed at the end of an interactive submission.
        ?.M2()
        ~~~~~~
BC30157: Leading '.' or '!' can only appear inside a 'With' statement.
        ?.M2()
         ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub Simple1()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Test1(New S1())
        Test1(Nothing)
	System.Console.WriteLine("---------")
        Test2(New S1())
        Test2(Nothing)
	System.Console.WriteLine("---------")
        Test3(New C1())
        Test3(Nothing)
	System.Console.WriteLine("---------")
        Test4(New C1())
        Test4(Nothing)
	System.Console.WriteLine("---------")

        Test5(Of S1)(Nothing)
	System.Console.WriteLine("---------")
        Test6(Of S1)(Nothing)
	System.Console.WriteLine("---------")

        Test5(Of C1)(New C1())
        Test5(Of C1)(Nothing)
	System.Console.WriteLine("---------")
        Test6(Of C1)(New C1())
        Test6(Of C1)(Nothing)
	System.Console.WriteLine("---------")
    End Sub

    Sub Test1(x As S1?)
	Dim y = x?.P1
        System.Console.WriteLine(if(y.HasValue, y.ToString(), "Null"))
    End Sub

    Sub Test2(x As S1?)
	Dim y = x?.P2
        System.Console.WriteLine(if(y, "Null"))
    End Sub

    Sub Test3(x As C1)
	Dim y = x?.P1
        System.Console.WriteLine(if(y.HasValue, y.ToString(), "Null"))
    End Sub

    Sub Test4(x As C1)
	Dim y = x?.P2
        System.Console.WriteLine(if(y, "Null"))
    End Sub

    Sub Test5(Of T As I1)(x As T)
	Dim y = x?.P1
        System.Console.WriteLine(if(y.HasValue, y.ToString(), "Null"))
    End Sub

    Sub Test6(Of T As I1)(x As T)
	Dim y = x?.P2
        System.Console.WriteLine(if(y, "Null"))
    End Sub
End Module

Interface I1
    ReadOnly Property P1 As Integer
    ReadOnly Property P2 As String
End Interface

Structure S1
    Implements I1

    ReadOnly Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("S1.P1")
            Return 1
        End Get
    End Property

    ReadOnly Property P2 As String Implements I1.P2
        Get
            System.Console.WriteLine("S1.P2")
            Return 2
        End Get
    End Property
End Structure

Class C1
    Implements I1

    ReadOnly Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C1.P1")
            Return 3
        End Get
    End Property

    ReadOnly Property P2 As String Implements I1.P2
        Get
            System.Console.WriteLine("C1.P2")
            Return 4
        End Get
    End Property
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
S1.P1
1
Null
---------
S1.P2
2
Null
---------
C1.P1
3
Null
---------
C1.P2
4
Null
---------
S1.P1
1
---------
S1.P2
2
---------
C1.P1
3
Null
---------
C1.P2
4
Null
---------
]]>)
        End Sub

        <Fact()>
        Public Sub Simple2()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Test1(New S1())
    	System.Console.WriteLine("---------")
        Test1(Nothing)
    	System.Console.WriteLine("---------")
        Test2(New S1())
    	System.Console.WriteLine("---------")
        Test2(Nothing)
    	System.Console.WriteLine("---------")
        Test3(New S1())
    	System.Console.WriteLine("---------")
        Test3(Nothing)
    	System.Console.WriteLine("---------")
        Test4(New S1())
    	System.Console.WriteLine("---------")
        Test4(Nothing)
    	System.Console.WriteLine("---------")
        Test5(New S1())
    	System.Console.WriteLine("---------")
        Test5(Nothing)
    	System.Console.WriteLine("---------")
    End Sub

    Function GetX(x As S1?) As S1?
    	System.Console.WriteLine("GetX")
        Return x
    End Function

    Sub Test1(x As S1?)
    	System.Console.WriteLine("Test1")
	    Dim y = GetX(x)?.M1()
        System.Console.WriteLine(if(y.HasValue, y.ToString(), "Null"))
    End Sub

    Sub Test2(x As S1?)
    	System.Console.WriteLine("Test2")
	    GetX(x)?.M2()
    End Sub

    Sub Test3(x As S1?)
    	System.Console.WriteLine("Test3")
	    GetX(x)?.M2
    End Sub

    Sub Test4(x As S1?)
    	System.Console.WriteLine("Test4")
	    Call GetX(x)?.M2
    End Sub

    Sub Test5(x As S1?)
    	System.Console.WriteLine("Test5")
	    Call GetX(x)?.M1()
    End Sub
End Module

Structure S1

    Function M1() As Integer
        System.Console.WriteLine("S1.M1")
        Return 1
    End Function

    Sub M2()
        System.Console.WriteLine("S1.M2")
    End Sub
End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Test1
GetX
S1.M1
1
---------
Test1
GetX
Null
---------
Test2
GetX
S1.M2
---------
Test2
GetX
---------
Test3
GetX
S1.M2
---------
Test3
GetX
---------
Test4
GetX
S1.M2
---------
Test4
GetX
---------
Test5
GetX
S1.M1
---------
Test5
GetX
---------
]]>)
        End Sub

        <Fact()>
        Public Sub Simple3()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
    End Sub

    Sub Test2(x As S1?)
        System.Console.WriteLine(x?.M2())
    End Sub
End Module

Structure S1
    Sub M2()
        System.Console.WriteLine("S1.M2")
    End Sub
End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30491: Expression does not produce a value.
        System.Console.WriteLine(x?.M2())
                                   ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub CallContext1()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
    End Sub

    Sub Test(x As S1)
        Call x.M1(0)
        x.M1(0)
    End Sub

    Sub Test(x As S1?)
        Call x?.M1(0)
        x?.M1(0)
    End Sub
End Module

Structure S1

    Function M1() As Integer()
        System.Console.WriteLine("S1.M1")
        Return {1}
    End Function

End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30057: Too many arguments to 'Public Function M1() As Integer()'.
        Call x.M1(0)
                  ~
BC30057: Too many arguments to 'Public Function M1() As Integer()'.
        x.M1(0)
             ~
BC30057: Too many arguments to 'Public Function M1() As Integer()'.
        Call x?.M1(0)
                   ~
BC30057: Too many arguments to 'Public Function M1() As Integer()'.
        x?.M1(0)
              ~
</expected>)
        End Sub

        <Fact()>
        Public Sub CallContext2()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Test1(New S1())
        System.Console.WriteLine("---")
        Test2(New S1())
        System.Console.WriteLine("---")
        Test2(Nothing)
        System.Console.WriteLine("---")
    End Sub

    Sub Test1(x As S1)
        System.Console.WriteLine(x.M1(0))
    End Sub

    Sub Test2(x As S1?)
        System.Console.WriteLine(x?.M1(0))
    End Sub
End Module

Structure S1

    Function M1() As Integer()
        System.Console.WriteLine("S1.M1")
        Return {1}
    End Function

End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
   S1.M1
1
---
S1.M1
1
---

---
]]>)
        End Sub

        <Fact()>
        Public Sub CallContext3()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        Test1(New S1() With {.m_Array = {"1"}})
        System.Console.WriteLine("---")
        Test2(New S1() With {.m_Array = {"2"}})
        System.Console.WriteLine("---")
        Test2(New S1() With {.m_Array = {Nothing}})
        System.Console.WriteLine("---")
    End Sub

    Sub Test1(x As S1)
        Call x.M1(0).ToString()
    End Sub

    Sub Test2(x As S1)
        Call x.M1(0)?.ToString()
    End Sub
End Module

Structure S1

    Public m_Array As String()
    Function M1() As String()
        System.Console.WriteLine("S1.M1")
        Return m_Array
    End Function

End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
S1.M1
---
S1.M1
---
S1.M1
---
]]>)
        End Sub

        <Fact()>
        Public Sub CallContext4()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
    End Sub

    Sub Test(x As S1)
        Call x.P1()
        x.P1()
        x.P1
    End Sub

    Sub Test(x As S1?)
        Call x?.P1()
        x?.P1()
        x?.P1
    End Sub
End Module

Structure S1

    Property P1() As Integer

End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30545: Property access must assign to the property or use its value.
        Call x.P1()
             ~~~~~~
BC30545: Property access must assign to the property or use its value.
        x.P1()
        ~~~~~~
BC30545: Property access must assign to the property or use its value.
        x.P1
        ~~~~
BC30545: Property access must assign to the property or use its value.
        Call x?.P1()
               ~~~~~
BC30545: Property access must assign to the property or use its value.
        x?.P1()
          ~~~~~
BC30545: Property access must assign to the property or use its value.
        x?.P1
          ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub AssignmentContext()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
    End Sub

    Sub Test(x As S1?)
        x?.P1() = Nothing
        x?.P1 = Nothing
        x?.F1 = Nothing
    End Sub
End Module

Structure S1

    Property P1() As Integer
    Public F1 As Integer
End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        x?.P1() = Nothing
        ~~~~~~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        x?.P1 = Nothing
        ~~~~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        x?.F1 = Nothing
        ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ERR_CannotBeMadeNullable1_1()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
    End Sub

    Sub Test(Of T, U As Class, V As Structure)(x1 As S1(Of T)?, x2 As S1(Of T)?, x3 As S1(Of U)?, x4 As S1(Of U)?, x5 As S1(Of V)?, x6 As S1(Of V)?)
        Dim y1 = x1?.M1()
        x2?.M1()
        Dim y3 = x3?.M1()
        x4?.M1()
        Dim y5 = x5?.M1()
        x5?.M1()
    End Sub

End Module

Structure S1(Of T)

    Function M1() As T
        Return Nothing
    End Function

End Structure
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC37238: 'T' cannot be made nullable.
        Dim y1 = x1?.M1()
                    ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ERR_UnaryOperand2_1()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
    End Sub

    Sub Test(Of T, U As Class, V As Structure)(x1 As T, x2 As U, x3 As V)
        x1?.ToString()
        x2?.ToString()
        x3?.ToString()
    End Sub

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected>
BC30487: Operator '?' is not defined for type 'V'.
        x3?.ToString()
          ~
</expected>)
        End Sub

        <Fact()>
        Public Sub InvocationOrIndex_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
        System.Console.WriteLine(Invoke(Function(x) CStr(x)))
        System.Console.WriteLine(If(Invoke(Nothing), "Null"))
        System.Console.WriteLine(Index({"2"}))
        System.Console.WriteLine(If(Index(Nothing), "Null"))
        System.Console.WriteLine(DefaultProperty(New C1()))
        System.Console.WriteLine(If(DefaultProperty(Nothing), "Null"))
    End Sub


    Function Invoke(x As System.Func(Of Integer, String)) As String
        Return x?(1)
    End Function

    Function Index(x As String()) As String
        Return x?(0)
    End Function

    Function DefaultProperty(x As C1) As String
        Return x?(3)
    End Function

End Module


Class C1
    Default ReadOnly Property P1(i As Integer) As String
        Get
            Return CStr(i)
        End Get
    End Property
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
1
Null
2
Null
3
Null
]]>)
        End Sub

        <Fact()>
        Public Sub XmlMember_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq

Module Module1

    Sub Main()
        Dim x = <e0 a1="a1_1">
                    <e1>e1_1</e1>
                    <e3>
                        <e1>e1_2</e1>
                        <e2>e2_1</e2>
                    </e3>
                </e0>

        System.Console.WriteLine(Test1(x))
        System.Console.WriteLine(Test2(x).Single())
        System.Console.WriteLine(Test3(x).Single())
        System.Console.WriteLine(if(CObj(Test1(Nothing)),"Null"))
        System.Console.WriteLine(if(CObj(Test2(Nothing)),"Null"))
        System.Console.WriteLine(if(CObj(Test3(Nothing)),"Null"))
    End Sub

    Function Test1(x As XElement) As String
        Return x?.@a1
    End Function

    Function Test2(x As XElement) As IEnumerable(Of XElement)
        Return x?.<e1>
    End Function

    Function Test3(x As XElement) As IEnumerable(Of XElement)
        Return x?...<e2>
    End Function

End Module
]]>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, XmlReferences, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
a1_1
<e1>e1_1</e1>
<e2>e2_1</e2>
Null
Null
Null
]]>)
        End Sub

        <Fact()>
        Public Sub DictionaryAccess_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Module1

    Sub Main()
        System.Console.WriteLine(Test1(New C1()))
        System.Console.WriteLine(if(Test1(Nothing), "Null"))
    End Sub

    Function Test1(x As C1) As String
        Return x?!a1
    End Function

End Module


Class C1
    Default ReadOnly Property P1(i As String) As String
        Get
            Return i
        End Get
    End Property
End Class
]]>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
a1
Null
]]>)
        End Sub

        <Fact()>
        Public Sub LateBound_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Module1

    Sub Main()
        System.Console.WriteLine(Test1(New C1()))
        System.Console.WriteLine(Test2(New C1()))
        Test3(New C1())
        Test4(New C1())
        System.Console.WriteLine(Test5(New C1()))
        System.Console.WriteLine(Test6(New C1(), "a4"))

        System.Console.WriteLine(If(Test1(Nothing), "Null"))
        System.Console.WriteLine(if(Test2(Nothing), "Null"))
        Test3(Nothing)
        Test4(Nothing)
        System.Console.WriteLine(if(Test5(Nothing), "Null"))
        System.Console.WriteLine(if(Test6(Nothing, "a4"), "Null"))
    End Sub

    Function Test1(x As Object) As String
        Return x?!a1
    End Function

    Function Test2(x As Object) As String
        Return x?.P1("a2")
    End Function

    Sub Test3(x As Object)
        System.Console.WriteLine("Test3")
        x?.P1("a2")
    End Sub

    Sub Test4(x As Object)
        System.Console.WriteLine("Test4")
        Try
            x?.P2(0)
        Catch e As System.Exception
            System.Console.WriteLine(e.Message)
        End Try
    End Sub

    Function Test5(x As Object) As String
        Return x?.P2(0)
    End Function

    Function Test6(x As C1, y As Object) As String
        Return x?.M1(y)
    End Function
End Module


Class C1
    Default ReadOnly Property P1(i As String) As String
        Get
            Return i
        End Get
    End Property

    ReadOnly Property P2 As String()
        Get
            Return {"a3"}
        End Get
    End Property

    Function M1(x As String) As String
        Return x
    End Function

    Function M1(x As Integer) As Integer
        Return x
    End Function
End Class
]]>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
a1
a2
Test3
Test4
Overload resolution failed because no accessible 'P2' accepts this number of arguments.
a3
a4
Null
Null
Test3
Test4
Null
Null
]]>)
        End Sub

        <Fact()>
        Public Sub WRN_UnobservedAwaitableExpression_1()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1

    Sub Main()
    End Sub

    Async Function Test6(x As C1) As System.Threading.Tasks.Task(Of Integer)
        Dim y = Await x?.M1()
        x?.M1()
        Return 0
    End Function
End Module


Class C1
    Function M1() As System.Threading.Tasks.Task(Of Integer)
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ExperimentalReleaseExe, parseOptions:=TestOptions.ExperimentalReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected>
BC42358: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the Await operator to the result of the call.
        x?.M1()
        ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ImplicitLocal_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Option Explicit Off

Module Module1

    Sub Main()
        Dim y1 = implicit1?.ToString()
        Dim y2 = implicit2?()
        Dim y3 = implicit3.@x
        Dim y4 = implicit4.<x>
        Dim y5 = implicit5...<x>
        Dim y6 = implicit6!x
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, XmlReferences, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30451: 'implicit1' is not declared. It may be inaccessible due to its protection level.
        Dim y1 = implicit1?.ToString()
                 ~~~~~~~~~
BC30451: 'implicit2' is not declared. It may be inaccessible due to its protection level.
        Dim y2 = implicit2?()
                 ~~~~~~~~~
BC42104: Variable 'implicit3' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim y3 = implicit3.@x
                 ~~~~~~~~~
BC31168: XML axis properties do not support late binding.
        Dim y3 = implicit3.@x
                 ~~~~~~~~~~~~
BC42104: Variable 'implicit4' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim y4 = implicit4.<x>
                 ~~~~~~~~~
BC31168: XML axis properties do not support late binding.
        Dim y4 = implicit4.<x>
                 ~~~~~~~~~~~~~
BC42104: Variable 'implicit5' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim y5 = implicit5...<x>
                 ~~~~~~~~~
BC31168: XML axis properties do not support late binding.
        Dim y5 = implicit5...<x>
                 ~~~~~~~~~~~~~~~
BC42104: Variable 'implicit6' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim y6 = implicit6!x
                 ~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ImplicitLocal_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Option Explicit Off

Module Module1

    Sub Main()
        Dim y1 = implicit1?.ToString().@x.<x>...<x>!x?.ToString()
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, XmlReferences, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30451: 'implicit1' is not declared. It may be inaccessible due to its protection level.
        Dim y1 = implicit1?.ToString().@x.<x>...<x>!x?.ToString()
                 ~~~~~~~~~
BC36807: XML elements cannot be selected from type 'String'.
        Dim y1 = implicit1?.ToString().@x.<x>...<x>!x?.ToString()
                           ~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ImplicitLocal_03()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Option Explicit Off

Module Module1

    Sub Main()
        Dim y1 = implicit1?().ToString.@x.<x>...<x>!x?.ToString()
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, XmlReferences, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30451: 'implicit1' is not declared. It may be inaccessible due to its protection level.
        Dim y1 = implicit1?().ToString.@x.<x>...<x>!x?.ToString()
                 ~~~~~~~~~
BC36807: XML elements cannot be selected from type 'String'.
        Dim y1 = implicit1?().ToString.@x.<x>...<x>!x?.ToString()
                           ~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ImplicitLocal_04()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Option Explicit Off

Module Module1

    Sub Main()
        Dim y1 = implicit1?.<x>.ToString()
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, XmlReferences, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42104: Variable 'implicit1' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim y1 = implicit1?.<x>.ToString()
                 ~~~~~~~~~
BC31168: XML axis properties do not support late binding.
        Dim y1 = implicit1?.<x>.ToString()
                           ~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub Flow_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x as Object
        CStr(Nothing)?.Test(x)
        x.ToString()
    End Sub

    <Extension>
    Sub Test(this as String, ByRef x as Object)
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, XmlReferences, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.
        x.ToString()
        ~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub Flow_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x as Object
        Call "a"?.Test(x)
        x.ToString()
    End Sub

    <Extension>
    Sub Test(this as String, ByRef x as Object)
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, XmlReferences, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42030: Variable 'x' is passed by reference before it has been assigned a value. A null reference exception could result at runtime.
        Call "a"?.Test(x)
                       ~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub Flow_03()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x as Object
        GetString()?.Test(x)
        x.ToString()
    End Sub

    Function GetString() As String
        return "b"
    End Function

    <Extension>
    Sub Test(this as String, ByRef x as Object)
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, XmlReferences, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42030: Variable 'x' is passed by reference before it has been assigned a value. A null reference exception could result at runtime.
        GetString()?.Test(x)
                          ~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub WithStatement_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Module1

    Sub Main()

        Dim c1 As New C1()

        With "string"
            c1?.M1(.Length)
            Dim y = c1?(.Length)
        End With

    End Sub

End Module


Class C1
    Sub M1(x As Integer)
        System.Console.WriteLine("M1 - {0}", x)
    End Sub

    Default ReadOnly Property P1(x As Integer) As Integer
        Get
            System.Console.WriteLine("P1 - {0}", x)
            Return x
        End Get
    End Property
End Class
]]>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
M1 - 6
P1 - 6
]]>)
        End Sub

        <Fact()>
        Public Sub WithStatement_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
        <![CDATA[
Module Module1

    Sub Main()
        Test(New C1())
        Test(Nothing)
    End Sub

    Sub Test(c1 As C1)
        System.Console.WriteLine("Test - {0}", c1)

        With c1
            ?.M1()
            Dim y = ?!str
        End With
    End Sub

End Module


Class C1
    Sub M1()
        System.Console.WriteLine("M1")
    End Sub

    Default ReadOnly Property P1(x As String) As String
        Get
            System.Console.WriteLine("P1 - {0}", x)
            Return x
        End Get
    End Property
End Class
]]>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Test - C1
M1
P1 - str
Test -
]]>)
        End Sub

        <Fact()>
        Public Sub WithStatement_03()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim x1 = New C1() With { ?.P2 = "a" }
        Dim x2 = New C1() With { .P3 = ?.P4 }
        Dim x3 = New C1() With { ?!b = "c" }
        Dim x4 = New C1() With { .P5 = ?!d }

        Dim x5 = New With { ?.P2 = "a" }
        Dim x6 = New With { .P3 = ?.P4 }
        Dim x7 = New With { ?!b = "c" }
        Dim x8 = New With { .P5 = ?!d }
    End Sub

    Sub Test(c1 As C1)
            ?.M1()
            Dim y = ?!str
    End Sub
End Module


Class C1
    Sub M1()
        System.Console.WriteLine("M1")
    End Sub

    Default ReadOnly Property P1(x As String) As String
        Get
            System.Console.WriteLine("P1 - {0}", x)
            Return x
        End Get
    End Property
    Property P2 As String
    Property P3 As String
    Property P4 As String
    Property P5 As String
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ExperimentalReleaseExe)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30985: Name of field or property being initialized in an object initializer must start with '.'.
        Dim x1 = New C1() With { ?.P2 = "a" }
                                 ~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
        Dim x1 = New C1() With { ?.P2 = "a" }
                                 ~~~~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
        Dim x2 = New C1() With { .P3 = ?.P4 }
                                       ~~~~
BC30985: Name of field or property being initialized in an object initializer must start with '.'.
        Dim x3 = New C1() With { ?!b = "c" }
                                 ~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
        Dim x3 = New C1() With { ?!b = "c" }
                                 ~~~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
        Dim x4 = New C1() With { .P5 = ?!d }
                                       ~~~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
        Dim x5 = New With { ?.P2 = "a" }
                            ~~~~
BC36556: Anonymous type member name can be inferred only from a simple or qualified name with no arguments.
        Dim x5 = New With { ?.P2 = "a" }
                            ~~~~~~~~~~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
        Dim x6 = New With { .P3 = ?.P4 }
                                  ~~~~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
        Dim x7 = New With { ?!b = "c" }
                            ~~~
BC36556: Anonymous type member name can be inferred only from a simple or qualified name with no arguments.
        Dim x7 = New With { ?!b = "c" }
                            ~~~~~~~~~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
        Dim x8 = New With { .P5 = ?!d }
                                  ~~~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
            ?.M1()
            ~~~~~~
BC37239: Leading '?' can only appear inside a 'With' statement, but not inside an object member initializer.
            Dim y = ?!str
                    ~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ExpressionTree_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim x As System.Linq.Expressions.Expression(Of System.Func(Of Object, String)) = Function(y As Object) y?.ToString()
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, {SystemCoreRef}, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC37240: A null propagating operator cannot be converted into an expression tree.
        Dim x As System.Linq.Expressions.Expression(Of System.Func(Of Object, String)) = Function(y As Object) y?.ToString()
                                                                                                               ~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub AnonymousTypeMemberName_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System.Linq

Module Module1

    Sub Main()
        Dim x As System.Func(Of String) = Function() "x"
        Dim y = <a><b></b></a>

        System.Console.WriteLine(New With {x?()})
        System.Console.WriteLine(New With {y?.<b>(0)})
    End Sub

End Module
]]>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, XmlReferences, TestOptions.ExperimentalReleaseExe, TestOptions.ExperimentalReleaseExe.ParseOptions)

            ' TODO: Should succeed
            '            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            '            <![CDATA[
            ']]>)

            AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC36556: Anonymous type member name can be inferred only from a simple or qualified name with no arguments.
        System.Console.WriteLine(New With {x?()})
                                           ~~~~
BC36556: Anonymous type member name can be inferred only from a simple or qualified name with no arguments.
        System.Console.WriteLine(New With {y?.<b>(0)})
                                           ~~~~~~~~~
]]></expected>)
        End Sub

    End Class
End Namespace