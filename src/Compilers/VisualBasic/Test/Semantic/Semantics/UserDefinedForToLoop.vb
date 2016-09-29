' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.OverloadResolution
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class UserDefinedForToLoop
        Inherits BasicTestBase

        <Fact>
        Public Sub BasicTest1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Class B1

        Private val As Integer

        Public Sub New(val As Integer)
            Me.val = val
        End Sub

        Shared Operator +(x As B1, y As B1) As B1
            System.Console.WriteLine("{0} + {1}", x, y)
            Return New B1(x.val + y.val)
        End Operator

        Shared Operator -(x As B1, y As B1) As B1
            System.Console.WriteLine("{0} - {1}", x, y)
            Return New B1(x.val - y.val)
        End Operator

        Shared Operator >=(x As B1, y As B1) As Boolean
            System.Console.WriteLine("{0} >= {1}", x, y)
            Return x.val >= y.val
        End Operator
        Shared Operator <=(x As B1, y As B1) As Boolean
            System.Console.WriteLine("{0} <= {1}", x, y)
            Return x.val <= y.val
        End Operator

        Public Overrides Function ToString() As String
            Return String.Format("B({0})", val)
        End Function
    End Class

    Class B2
        Public Val As B1
    End Class

    Dim m_Index As New B2()
    ReadOnly Property Index As B2
        Get
            System.Console.WriteLine("get_Index")
            Return m_Index
        End Get
    End Property

    Function Init(val As Integer) As B1
        System.Console.WriteLine("Init")
        Return New B1(val)
    End Function

    Function [Step](val As Integer) As B1
        System.Console.WriteLine("Step")
        Return New B1(Val)
    End Function

    Function Limit(val As Integer) As B1
        System.Console.WriteLine("Limit")
        Return New B1(Val)
    End Function

    Sub Main()

        For Index.Val = Init(0) To Limit(3) Step [Step](2)
            System.Console.WriteLine("Body {0}", m_Index.Val)
        Next

        System.Console.WriteLine("-----")
        For Index.Val = Init(3) To Limit(0) Step [Step](-2)
            System.Console.WriteLine("Body {0}", m_Index.Val)
        Next
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
get_Index
Init
Limit
Step
B(2) - B(2)
B(2) >= B(0)
get_Index
B(0) <= B(3)
Body B(0)
get_Index
get_Index
B(0) + B(2)
get_Index
B(2) <= B(3)
Body B(2)
get_Index
get_Index
B(2) + B(2)
get_Index
B(4) <= B(3)
-----
get_Index
Init
Limit
Step
B(-2) - B(-2)
B(-2) >= B(0)
get_Index
B(3) >= B(0)
Body B(3)
get_Index
get_Index
B(3) + B(-2)
get_Index
B(1) >= B(0)
Body B(1)
get_Index
get_Index
B(1) + B(-2)
get_Index
B(-1) >= B(0)
]]>)
        End Sub

        <Fact>
        Public Sub BasicTest2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Class B1

        Private val As Integer

        Public Sub New(val As Integer)
            Me.val = val
        End Sub

        Shared Operator +(x As B1, y As B1) As B1
            System.Console.WriteLine("{0} + {1}", x, y)
            Return New B1(x.val + y.val)
        End Operator

        Shared Operator -(x As B1, y As B1) As B1
            System.Console.WriteLine("{0} - {1}", x, y)
            Return New B1(x.val - y.val)
        End Operator

        Shared Operator >=(x As B1, y As B1) As B3
            Dim str = String.Format("{0} >= {1}", x, y)
            System.Console.WriteLine(str)
            Return New B3(x.val >= y.val, str)
        End Operator
        Shared Operator <=(x As B1, y As B1) As B3
            Dim str = String.Format("{0} <= {1}", x, y)
            System.Console.WriteLine(str)
            Return New B3(x.val <= y.val, str)
        End Operator

        Public Overrides Function ToString() As String
            Return String.Format("B({0})", val)
        End Function
    End Class

    Class B3
        Private val As Boolean
        Private str As String

        Public Sub New(val As Boolean, str As String)
            Me.val = val
            Me.str = str
        End Sub

        Shared Operator IsTrue(x As B3) As Boolean
            System.Console.WriteLine("B3({0}).IsTrue", x.str)
            Return x.val
        End Operator

        Shared Operator IsFalse(x As B3) As Boolean
            System.Console.WriteLine("B3({0}).IsFalse", x.str)
            Return x.val
        End Operator

    End Class

    Function Init(val As Integer) As B1
        System.Console.WriteLine("Init")
        Return New B1(val)
    End Function

    Function [Step](val As Integer) As B1
        System.Console.WriteLine("Step")
        Return New B1(Val)
    End Function

    Function Limit(val As Integer) As B1
        System.Console.WriteLine("Limit")
        Return New B1(Val)
    End Function

    Sub Main()

        For i = Init(0) To Limit(3) Step [Step](2)
            System.Console.WriteLine("Body {0}", i)
        Next

        System.Console.WriteLine("-----")
        For i = Init(3) To Limit(0) Step [Step](-2)
            System.Console.WriteLine("Body {0}", i)
        Next
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
Init
Limit
Step
B(2) - B(2)
B(2) >= B(0)
B3(B(2) >= B(0)).IsTrue
B(0) <= B(3)
B3(B(0) <= B(3)).IsTrue
Body B(0)
B(0) + B(2)
B(2) <= B(3)
B3(B(2) <= B(3)).IsTrue
Body B(2)
B(2) + B(2)
B(4) <= B(3)
B3(B(4) <= B(3)).IsTrue
-----
Init
Limit
Step
B(-2) - B(-2)
B(-2) >= B(0)
B3(B(-2) >= B(0)).IsTrue
B(3) >= B(0)
B3(B(3) >= B(0)).IsTrue
Body B(3)
B(3) + B(-2)
B(1) >= B(0)
B3(B(1) >= B(0)).IsTrue
Body B(1)
B(1) + B(-2)
B(-1) >= B(0)
B3(B(-1) >= B(0)).IsTrue
]]>)
        End Sub

        <Fact>
        Public Sub BasicTest3()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Class B1

        Private val As Integer

        Public Sub New(val As Integer)
            Me.val = val
        End Sub

        Shared Widening Operator CType(x As Integer) As B1
            System.Console.WriteLine("CType({0}) As B1", x)
            Return New B1(x)
        End Operator

        Shared Operator +(x As B1, y As B1) As B1
            System.Console.WriteLine("{0} + {1}", x, y)
            Return New B1(x.val + y.val)
        End Operator

        Shared Operator -(x As B1, y As B1) As B1
            System.Console.WriteLine("{0} - {1}", x, y)
            Return New B1(x.val - y.val)
        End Operator

        Shared Operator >=(x As B1, y As B1) As B3
            Dim str = String.Format("{0} >= {1}", x, y)
            System.Console.WriteLine(str)
            Return New B3(x.val >= y.val, str)
        End Operator
        Shared Operator <=(x As B1, y As B1) As B3
            Dim str = String.Format("{0} <= {1}", x, y)
            System.Console.WriteLine(str)
            Return New B3(x.val <= y.val, str)
        End Operator

        Public Overrides Function ToString() As String
            Return String.Format("B({0})", val)
        End Function
    End Class

    Class B3
        Private val As Boolean
        Private str As String

        Public Sub New(val As Boolean, str As String)
            Me.val = val
            Me.str = str
        End Sub

        Shared Operator IsTrue(x As B3) As Boolean
            System.Console.WriteLine("B3({0}).IsTrue", x.str)
            Return x.val
        End Operator

        Shared Operator IsFalse(x As B3) As Boolean
            System.Console.WriteLine("B3({0}).IsFalse", x.str)
            Return x.val
        End Operator

    End Class

    Function Init(val As Integer) As B1
        System.Console.WriteLine("Init")
        Return New B1(val)
    End Function

    Function [Step](val As Integer) As B1
        System.Console.WriteLine("Step")
        Return New B1(Val)
    End Function

    Function Limit(val As Integer) As B1
        System.Console.WriteLine("Limit")
        Return New B1(Val)
    End Function

    Sub Main()
        For i = Init(0) To Limit(3)
            System.Console.WriteLine("Body {0}", i)
        Next
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
Init
Limit
CType(1) As B1
B(1) - B(1)
B(1) >= B(0)
B3(B(1) >= B(0)).IsTrue
B(0) <= B(3)
B3(B(0) <= B(3)).IsTrue
Body B(0)
B(0) + B(1)
B(1) <= B(3)
B3(B(1) <= B(3)).IsTrue
Body B(1)
B(1) + B(1)
B(2) <= B(3)
B3(B(2) <= B(3)).IsTrue
Body B(2)
B(2) + B(1)
B(3) <= B(3)
B3(B(3) <= B(3)).IsTrue
Body B(3)
B(3) + B(1)
B(4) <= B(3)
B3(B(4) <= B(3)).IsTrue
]]>)
        End Sub

        <Fact(), WorkItem(544375, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544375")>
        Public Sub BasicTest4()
            Dim compilationDef =
<compilation name="BasicTest4">
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Structure S
    Class C
        Private val As Boolean
        Private str As String

        Public Sub New(val As Boolean, str As String)
            Me.val = val
            Me.str = str
        End Sub

        Shared Operator IsTrue(x As C) As Boolean
            Console.WriteLine("C({0}).IsTrue", x.str)
            Return x.val
        End Operator

        Shared Operator IsFalse(x As C) As Boolean
            Console.WriteLine("C({0}).IsFalse", x.str)
            Return x.val
        End Operator
    End Class

    Private val As Integer

    Public Sub New(val As Integer)
        Me.val = val
    End Sub

    Shared Widening Operator CType(x As Integer) As S
        Console.WriteLine("CType({0}) As S", x)
        Return New S(x)
    End Operator

    Shared Operator +(x As S, y As S) As S
        Console.WriteLine("{0} + {1}", x, y)
        Return New S(x.val + y.val)
    End Operator

    Shared Operator -(x As S, y As S) As S
        Console.WriteLine("{0} - {1}", x, y)
        Return New S(x.val - y.val)
    End Operator

    Shared Operator >=(x As S, y As S) As C
        Dim str = String.Format("{0} >= {1}", x, y)
        Console.WriteLine(str)
        Return New C(x.val >= y.val, str)
    End Operator

    Shared Operator <=(x As S, y As S) As C
        Dim str = String.Format("{0} <= {1}", x, y)
        Console.WriteLine(str)
        Return New C(x.val <= y.val, str)
    End Operator

    Public Overrides Function ToString() As String
        Return String.Format("S({0})", val)
    End Function
End Structure

Module Module1

    Sub Main()
        For i As S = Nothing To Nothing
            Console.WriteLine("Body {0}", i)
        Next
    End Sub
End Module
    ]]></file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
CType(1) As S
S(1) - S(1)
S(1) >= S(0)
C(S(1) >= S(0)).IsTrue
S(0) <= S(0)
C(S(0) <= S(0)).IsTrue
Body S(0)
S(0) + S(1)
S(1) <= S(0)
C(S(1) <= S(0)).IsTrue
]]>)
        End Sub

        <Fact>
        Public Sub MissingCInt()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Class B1

        Private val As Integer

        Public Sub New(val As Integer)
            Me.val = val
        End Sub
    End Class


    Sub Main()
        For i = New B1(0) To New B1(3)
            System.Console.WriteLine("Body {0}", i)
        Next
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Integer' cannot be converted to 'Module1.B1'.
        For i = New B1(0) To New B1(3)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact>
        Public Sub MissingAddSubtractLeGe()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Class B1

        Private val As Integer

        Public Sub New(val As Integer)
            Me.val = val
        End Sub
    End Class


    Sub Main()
        For i = New B1(0) To New B1(3) Step New B1(2)
            System.Console.WriteLine("Body {0}", i)
        Next
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC33038: Type 'Module1.B1' must define operator '-' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'Module1.B1' must define operator '+' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'Module1.B1' must define operator '<=' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'Module1.B1' must define operator '>=' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)

        End Sub

        <Fact>
        Public Sub MissingSubtractLeGe()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Class B1

        Private val As Integer

        Public Sub New(val As Integer)
            Me.val = val
        End Sub

        Shared Operator +(x As B1, y As B1) As B1
            System.Console.WriteLine("{0} + {1}", x, y)
            Return New B1(x.val + y.val)
        End Operator

    End Class


    Sub Main()
        For i = New B1(0) To New B1(3) Step New B1(2)
            System.Console.WriteLine("Body {0}", i)
        Next
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC33038: Type 'Module1.B1' must define operator '-' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'Module1.B1' must define operator '<=' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'Module1.B1' must define operator '>=' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)

        End Sub

        <Fact>
        Public Sub MissingAddLeGe()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Class B1

        Private val As Integer

        Public Sub New(val As Integer)
            Me.val = val
        End Sub

        Shared Operator -(x As B1, y As B1) As B1
            System.Console.WriteLine("{0} - {1}", x, y)
            Return New B1(x.val - y.val)
        End Operator

    End Class

    Sub Main()
        For i = New B1(0) To New B1(3) Step New B1(2)
            System.Console.WriteLine("Body {0}", i)
        Next
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC33038: Type 'Module1.B1' must define operator '+' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'Module1.B1' must define operator '<=' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'Module1.B1' must define operator '>=' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)

        End Sub

        <Fact>
        Public Sub MissingLeGe()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Class B1

        Private val As Integer

        Public Sub New(val As Integer)
            Me.val = val
        End Sub

        Shared Operator +(x As B1, y As B1) As B1
            System.Console.WriteLine("{0} + {1}", x, y)
            Return New B1(x.val + y.val)
        End Operator

        Shared Operator -(x As B1, y As B1) As B1
            System.Console.WriteLine("{0} - {1}", x, y)
            Return New B1(x.val - y.val)
        End Operator

    End Class

    Sub Main()
        For i = New B1(0) To New B1(3) Step New B1(2)
            System.Console.WriteLine("Body {0}", i)
        Next
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC33038: Type 'Module1.B1' must define operator '<=' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'Module1.B1' must define operator '>=' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)

        End Sub

        <Fact>
        Public Sub MissingLe()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Class B1

        Private val As Integer

        Public Sub New(val As Integer)
            Me.val = val
        End Sub

        Shared Operator +(x As B1, y As B1) As B1
            System.Console.WriteLine("{0} + {1}", x, y)
            Return New B1(x.val + y.val)
        End Operator

        Shared Operator -(x As B1, y As B1) As B1
            System.Console.WriteLine("{0} - {1}", x, y)
            Return New B1(x.val - y.val)
        End Operator

        Shared Operator >=(x As B1, y As B1) As Boolean
            Return Nothing
        End Operator

    End Class

    Sub Main()
        For i = New B1(0) To New B1(3) Step New B1(2)
            System.Console.WriteLine("Body {0}", i)
        Next
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC33033: Matching '<=' operator is required for 'Public Shared Operator >=(x As Module1.B1, y As Module1.B1) As Boolean'.
        Shared Operator >=(x As B1, y As B1) As Boolean
                        ~~
BC33038: Type 'Module1.B1' must define operator '<=' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)

        End Sub

        <Fact>
        Public Sub MissingGe()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Class B1

        Private val As Integer

        Public Sub New(val As Integer)
            Me.val = val
        End Sub

        Shared Operator +(x As B1, y As B1) As B1
            System.Console.WriteLine("{0} + {1}", x, y)
            Return New B1(x.val + y.val)
        End Operator

        Shared Operator -(x As B1, y As B1) As B1
            System.Console.WriteLine("{0} - {1}", x, y)
            Return New B1(x.val - y.val)
        End Operator

        Shared Operator <=(x As B1, y As B1) As Boolean
            Return Nothing
        End Operator

    End Class

    Sub Main()
        For i = New B1(0) To New B1(3) Step New B1(2)
            System.Console.WriteLine("Body {0}", i)
        Next
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC33033: Matching '>=' operator is required for 'Public Shared Operator <=(x As Module1.B1, y As Module1.B1) As Boolean'.
        Shared Operator <=(x As B1, y As B1) As Boolean
                        ~~
BC33038: Type 'Module1.B1' must define operator '>=' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)

        End Sub

        <Fact>
        Public Sub MissingAddSubAndNonBooleanReturnOfLeGe()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Class B1

        Private val As Integer

        Public Sub New(val As Integer)
            Me.val = val
        End Sub

        Shared Operator <=(x As B1, y As B1) As B1
            Return Nothing
        End Operator

        Shared Operator >=(x As B1, y As B1) As B1
            Return Nothing
        End Operator
    End Class

    Sub Main()
        For i = New B1(0) To New B1(3) Step New B1(2)
            System.Console.WriteLine("Body {0}", i)
        Next
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30311: Value of type 'Module1.B1' cannot be converted to 'Boolean'.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'Module1.B1' must define operator '-' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'Module1.B1' must define operator '+' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)

        End Sub

        <Fact>
        Public Sub InvalidOperators1()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Class B1

        Private val As Integer

        Public Sub New(val As Integer)
            Me.val = val
        End Sub

        Shared Widening Operator CType(x As Integer) As B1
            Return New B1(x)
        End Operator

        Shared Widening Operator CType(x As B1) As Integer
            Return x.val
        End Operator

        Shared Operator +(x As B1, y As B1) As Integer
            Return Nothing
        End Operator

        Shared Operator -(x As B1, y As Integer) As B1
            Return Nothing
        End Operator

        Shared Operator >=(x As Integer, y As B1) As Boolean
            Return Nothing
        End Operator

        Shared Operator <=(x As Integer, y As B1) As Boolean
            Return Nothing
        End Operator
    End Class

    Sub Main()
        For i = New B1(0) To New B1(3) Step New B1(2)
            System.Console.WriteLine("Body {0}", i)
        Next
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC33039: Return and parameter types of 'Public Shared Operator -(x As Module1.B1, y As Integer) As Module1.B1' must be 'Module1.B1' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33039: Return and parameter types of 'Public Shared Operator +(x As Module1.B1, y As Module1.B1) As Integer' must be 'Module1.B1' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33040: Parameter types of 'Public Shared Operator <=(x As Integer, y As Module1.B1) As Boolean' must be 'Module1.B1' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33040: Parameter types of 'Public Shared Operator >=(x As Integer, y As Module1.B1) As Boolean' must be 'Module1.B1' to be used in a 'For' statement.
        For i = New B1(0) To New B1(3) Step New B1(2)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)

        End Sub

        <Fact>
        Public Sub InvalidOperators2()
            Dim compilationDef =
<compilation name="SimpleTest1">
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System

Module Module1

    Structure B1

        Shared Widening Operator CType(x As Integer) As B1
            Return Nothing
        End Operator

        Shared Widening Operator CType(x As B1) As Integer
            Return Nothing
        End Operator

        Shared Operator +(x As B1, y As B1) As B2
            Return Nothing
        End Operator

        Shared Operator -(x As B1, y As B2) As B1
            Return Nothing
        End Operator

        Shared Operator >=(x As B2?, y As B1) As Boolean
            Return Nothing
        End Operator

        Shared Operator <=(x As B2?, y As B1) As Boolean
            Return Nothing
        End Operator

    End Structure

    Structure B2
        Shared Widening Operator CType(x As B1) As B2
            Return Nothing
        End Operator
    End Structure

    Sub Main()

        For i = New B1?() To New B1?() Step New B1?()
        Next
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC33039: Return and parameter types of 'Public Shared Operator -(x As Module1.B1, y As Module1.B2) As Module1.B1' must be 'Module1.B1' to be used in a 'For' statement.
        For i = New B1?() To New B1?() Step New B1?()
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33039: Return and parameter types of 'Public Shared Operator +(x As Module1.B1, y As Module1.B1) As Module1.B2' must be 'Module1.B1' to be used in a 'For' statement.
        For i = New B1?() To New B1?() Step New B1?()
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33040: Parameter types of 'Public Shared Operator <=(x As Module1.B2?, y As Module1.B1) As Boolean' must be 'Module1.B1?' to be used in a 'For' statement.
        For i = New B1?() To New B1?() Step New B1?()
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33040: Parameter types of 'Public Shared Operator >=(x As Module1.B2?, y As Module1.B1) As Boolean' must be 'Module1.B1?' to be used in a 'For' statement.
        For i = New B1?() To New B1?() Step New B1?()
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)

        End Sub

    End Class

End Namespace

