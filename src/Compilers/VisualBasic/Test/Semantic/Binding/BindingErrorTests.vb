' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Basic.Reference.Assemblies

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    ' this place is dedicated to binding related error tests
    Public Class BindingErrorTests
        Inherits BasicTestBase

#Region "Targeted Error Tests - please arrange tests in the order of error code"

        <Fact()>
        Public Sub BC0ERR_None_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="None">
    <file name="a.vb">
        Imports System

        Class TimerState
            Public Delegate Sub MyEventHandler(ByVal sender As Object, ByVal e As System.EventArgs)
            Private m_MyEvent As MyEventHandler
            Public Custom Event MyEvent As MyEventHandler
                RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
                    m_MyEvent.Invoke(sender, e)
                End RaiseEvent
                AddHandler(ByVal value As MyEventHandler)
                    m_MyEvent = DirectCast ( _
                    [Delegate].Combine(m_MyEvent, value), _
                    MyEventHandler) : End addHandler
                RemoveHandler(ByVal value As MyEventHandler)
                    m_MyEvent = DirectCast ( _
                    [Delegate].RemoveAll(m_MyEvent, value), _
                    MyEventHandler)
                End RemoveHandler
            End Event
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC0ERR_None_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="None">
    <file name="a.vb">
        Class AAA
            Private _name As String
            Public ReadOnly Property Name() As String
                Get
                    Return _name : End Get
            End Property
            Private _age As String
            Public ReadOnly Property Age() As String
                Get
                    Return _age
        : End Get
            End Property
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC0ERR_None_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="None">
    <file name="a.vb">
        Module M1
            Function B As string
                Dim x = 1: End Function
            Function C As string
                Dim x = 2
            :End Function
        End Module
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC0ERR_None_4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="None">
    <file name="a.vb">
       Public Class S1
            Public Sub New()
                Dim cond = True
                GoTo l1
                If False Then
        l1:
                End If
            End Sub
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC0ERR_None_5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="None">
    <file name="a.vb">
       Public Structure S1
            Function FOO As String
                Return "h"
            End Function
            Sub Main()
                FOO
                FOO()
            End Sub
        End Structure
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC0ERR_None_6()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="None">
    <file name="a.vb">
       Public Structure S1
            Sub Main()
                dim a?()(,) as integer
                dim b(2) as integer
                dim c as integer(,)
            End Sub
        End Structure
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC0ERR_None_7()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="None">
    <file name="a.vb">
        Public Class D
            Public Class Foo
                Public x As Integer
            End Class
            Public Class FooD
                Inherits Foo
                Public Sub Baz()
                End Sub
            End Class
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC0ERR_None_8()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="None">
    <file name="a.vb">
        Public Class D
            Public Shared Sub Main()
                Dim x As Integer = 1
                Dim b As Boolean = x = 1
                System.Console.Write(b )
                Dim l As Long = 5
                System.Console.Write(l &gt; 6 )
                Dim f As Single = 25
                System.Console.Write(f &gt;= 25 )
                Dim d As Double = 3
                System.Console.Write(d &lt;= f )
            End Sub
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC0ERR_None_9()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="None">
    <file name="a.vb">
        Public Class C
            Public Shared Sub Main()
                System.Console.Write("{0}{1}{2}{3}{4}{5}", "a", "b", "c", "d", "e", "f" )
            End Sub
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC0ERR_None_10()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="None">
    <file name="a.vb">
        Public Class D
            Public Class Moo(Of T)
                Public Sub Boo(x As T)
                    Dim local As T = x
                End Sub
            End Class
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC0ERR_None_11()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="None">
    <file name="a.vb">
        Public Class C
            public class Moo
                public shared S as integer
            end class
            Public Shared Sub Main()
                System.Console.Write(Moo.S )
                Moo.S =  42
                System.Console.Write(Moo.S )
            End Sub
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC0ERR_None_12()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="None">
    <file name="a.vb">
        Public Class C
            Private Shared Function M(ByVal x As Integer, ByVal y As Integer, ByVal z As Integer) As Integer
                Return y
            End Function
            Public Shared Sub Main()
                System.Console.Write(M(0, 42, 1))
            End Sub
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC0ERR_None_13()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="None">
    <file name="a.vb">
        Public Class C
            Private Shared Function M() As System.AppDomain
                dim y as object = System.AppDomain.CreateDomain("qq")
                dim z as System.AppDomain = ctype(y,System.AppDomain)
                return  z
            End Function
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC0ERR_None_14()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="None">
    <file name="a.vb">
        Imports System
            Class Program
                Public Class C
                    Public Function Foo(ByVal p1 As Short, ByVal p2 As UShort) As UInteger
                        Return CUShort (p1 + p2)
                    End Function
                    Public Function Foo(ByVal p1 As Short, ByVal p2 As String) As UInteger
                        Return CUInt (p1)
                    End Function
                    Public Function Foo(ByVal p1 As Short, ByVal ParamArray p2 As UShort()) As UInteger
                        Return CByte (p2(0) + p2(1))
                    End Function
                End Class
            End Class
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC0ERR_None_15()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="None">
    <file name="a.vb">
        Class C
            Private Property P As Integer
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC0ERR_None_16()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC0ERR_None_16">
    <file name="a.vb">
Class Tests
    Public Sub New()
    End Sub
    Public Sub New(i As Integer)
        Me.New()
    End Sub
    Public Sub New(s As String)
        :
        Call Me.New(1)
    End Sub
    Public Sub New(s As Double)
        ' comment
        Call Me.New(1)
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC0ERR_None_17()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC0ERR_None_17">
    <file name="a.vb">
Class Tests
    Public Sub New()
    End Sub
    Public Sub New(i As Integer)
#Const a = 1
        Me.New()
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC0ERR_None_18()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC0ERR_None_18">
    <file name="a.vb">
Class Tests
    Public Sub New()
    End Sub
    Public Sub New(i As Integer)
#Const a = 1
#If a = 0 Then
        Me.New()
#End If
        Me.New()
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BC0ERR_None_19()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC0ERR_None_19">
    <file name="a.vb">
Class b
    Public Sub New(ParamArray t() As Integer)
    End Sub
End Class

Class d
    Inherits b
    Public Sub New()
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(540629, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540629")>
        <Fact()>
        Public Sub BC30002ERR_UndefinedType1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InvalidModuleAttribute1">
        <file name="a.vb"><![CDATA[
Imports System
Class Outer
    <AttributeUsage(AttributeTargets.All)> Class MyAttribute
        Inherits Attribute

    End Class
End Class

<MyAttribute()>
Class Test
End Class
        ]]></file>
    </compilation>).
            VerifyDiagnostics(Diagnostic(ERRID.ERR_UndefinedType1, "MyAttribute").WithArguments("MyAttribute"))

        End Sub

        <Fact()>
        Public Sub BC30020ERR_IsOperatorRequiresReferenceTypes1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Public Structure zzz
    Shared Sub Main()
        Dim a As New yyy
        Dim b As New yyy
        System.Console.WriteLine(a Is b)
        b = a
        System.Console.WriteLine(a Is b)
    End Sub
End Structure
Public Structure yyy
    Public i As Integer
    Public Sub abc()
        System.Console.WriteLine(i)
    End Sub
End Structure
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30020: 'Is' operator does not accept operands of type 'yyy'. Operands must be reference or nullable types.
        System.Console.WriteLine(a Is b)
                                 ~
BC30020: 'Is' operator does not accept operands of type 'yyy'. Operands must be reference or nullable types.
        System.Console.WriteLine(a Is b)
                                      ~
BC30020: 'Is' operator does not accept operands of type 'yyy'. Operands must be reference or nullable types.
        System.Console.WriteLine(a Is b)
                                 ~
BC30020: 'Is' operator does not accept operands of type 'yyy'. Operands must be reference or nullable types.
        System.Console.WriteLine(a Is b)
                                      ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30020ERR_IsOperatorRequiresReferenceTypes1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface I
End Interface
Class A
End Class
Class B(Of T As Structure)
End Class
Class C
    Shared Sub M1(Of T1)(_1 As T1)
        If _1 Is Nothing Then
        End If
        If Nothing Is _1 Then
        End If
    End Sub
    Shared Sub M2(Of T2 As Class)(_2 As T2)
        If _2 Is Nothing Then
        End If
        If Nothing Is _2 Then
        End If
    End Sub
    Shared Sub M3(Of T3 As Structure)(_3 As T3)
        If _3 Is Nothing Then
        End If
        If Nothing Is _3 Then
        End If
    End Sub
    Shared Sub M4(Of T4 As New)(_4 As T4)
        If _4 Is Nothing Then
        End If
        If Nothing Is _4 Then
        End If
    End Sub
    Shared Sub M5(Of T5 As I)(_5 As T5)
        If _5 Is Nothing Then
        End If
        If Nothing Is _5 Then
        End If
    End Sub
    Shared Sub M6(Of T6 As A)(_6 As T6)
        If _6 Is Nothing Then
        End If
        If Nothing Is _6 Then
        End If
    End Sub
    Shared Sub M7(Of T7 As U, U)(_7 As T7)
        If _7 Is Nothing Then
        End If
        If Nothing Is _7 Then
        End If
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30020: 'Is' operator does not accept operands of type 'T3'. Operands must be reference or nullable types.
        If _3 Is Nothing Then
           ~~
BC30020: 'Is' operator does not accept operands of type 'T3'. Operands must be reference or nullable types.
        If Nothing Is _3 Then
                      ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30029ERR_CantRaiseBaseEvent()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="CantRaiseBaseEvent">
    <file name="a.vb">
    Option Explicit On
    Class class1
        Public Event MyEvent()
    End Class
    Class class2
        Inherits class1
        Sub RaiseEvt()
            'COMPILEERROR: BC30029,"MyEvent"
            RaiseEvent MyEvent()
        End Sub
    End Class
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_CantRaiseBaseEvent, "MyEvent"))

        End Sub

        <Fact()>
        Public Sub BC30030ERR_TryWithoutCatchOrFinally()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="TryWithoutCatchOrFinally">
    <file name="a.vb">
    Module M1
        Sub Scen1()
            'COMPILEERROR: BC30030, "Try"
            Try
            End Try
        End Sub
    End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30030: Try must have at least one 'Catch' or a 'Finally'.
            Try
            ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30038ERR_StrictDisallowsObjectOperand1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="StrictDisallowsObjectOperand1">
    <file name="a.vb">
Imports System

    Structure myStruct1
        shared result = New Guid() And New Guid()
    End structure
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30452: Operator 'And' is not defined for types 'Guid' and 'Guid'.
        shared result = New Guid() And New Guid()
                        ~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30038ERR_StrictDisallowsObjectOperand1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="StrictDisallowsObjectOperand1">
    <file name="a.vb">
    Option Infer Off
    option Strict on
        Structure myStruct1
            sub foo()
                Dim x1$ = "hi"
                Dim [dim]  = "hi" &amp; "hello"
                Dim x31 As integer = x1 &amp; [dim]
            end sub
        End structure
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
                Dim [dim]  = "hi" &amp; "hello"
                    ~~~~~
BC30038: Option Strict On prohibits operands of type Object for operator '&amp;'.
                Dim x31 As integer = x1 &amp; [dim]
                                          ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30038ERR_StrictDisallowsObjectOperand1_1a()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="StrictDisallowsObjectOperand1">
    <file name="a.vb">
        <![CDATA[
option Strict on
Structure myStruct1
    sub foo()
        Dim x1$ = 33 & 2.34 'No inference here
    end sub
End structure
        ]]>
    </file>
</compilation>)
            AssertTheseEmitDiagnostics(compilation,
<expected><![CDATA[
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Conversions.ToString' is not defined.
        Dim x1$ = 33 & 2.34 'No inference here
                  ~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Conversions.ToString' is not defined.
        Dim x1$ = 33 & 2.34 'No inference here
                       ~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub BC30039ERR_LoopControlMustNotBeProperty()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Module M
    Sub M()
        For <x/>.@a = "" To ""
        Next
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30039: Loop control variable cannot be a property or a late-bound indexed array.
        For <x/>.@a = "" To ""
            ~~~~~~~
BC30337: 'For' loop control variable cannot be of type 'String' because the type does not support the required operators.
        For <x/>.@a = "" To ""
            ~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC30039ERR_LoopControlMustNotBeProperty_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Class C
    Property P() As Integer
    Dim F As Integer
    Sub Method1(A As Integer, ByRef B As Integer)
        ' error
        For Each P In {1}
        Next
        ' warning
        For Each F In {2}
        Next
        For Each Me.F In {3}
        Next
        For Each A In {4}
        Next
        For Each B In {5}
        Next
    End Sub
    Shared Sub Main()
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30039: Loop control variable cannot be a property or a late-bound indexed array.
        For Each P In {1}
                 ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30039ERR_LoopControlMustNotBeProperty_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Option Strict Off
Option Infer On
Public Class MyClass1
    Public Property z As Integer
    Public Shared Sub Main()
    End Sub
    Public Sub Foo()
        For z = 1 To 10
        Next
        For x = 1 To z Step z
        Next
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30039: Loop control variable cannot be a property or a late-bound indexed array.
        For z = 1 To 10
            ~
</expected>)
        End Sub

        <Fact(), WorkItem(545641, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545641")>
        Public Sub MissingLatebindingHelpers()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class Program
    Shared Sub Main()
        Dim Result As Object
        For Result = 1 To 2
        Next
    End Sub
End Class
    </file>
</compilation>)
            AssertTheseEmitDiagnostics(compilation,
<expected>
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.ObjectFlowControl+ForLoopControl.ForLoopInitObj' is not defined.
        For Result = 1 To 2
        ~~~~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.ObjectFlowControl+ForLoopControl.ForNextCheckObj' is not defined.
        For Result = 1 To 2
        ~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub MissingLatebindingHelpersObjectFor()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Class Program
    Shared Sub Main()
        Dim obj As Object = New cls1
        obj.P1 = 42                         ' assignment    (Set)
        obj.P1()                            ' side-effect   (Call)
        Console.WriteLine(obj.P1)           ' value         (Get)
    End Sub

    Class cls1
        Private _p1 As Integer
        Public Property p1 As Integer
            Get
                Console.Write("Get")
                Return _p1
            End Get
            Set(value As Integer)
                Console.Write("Set")
                _p1 = value
            End Set
        End Property
    End Class
End Class
    </file>
</compilation>)
            AssertTheseEmitDiagnostics(compilation,
<expected>
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateSet' is not defined.
        obj.P1 = 42                         ' assignment    (Set)
        ~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateCall' is not defined.
        obj.P1()                            ' side-effect   (Call)
        ~~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet' is not defined.
        Console.WriteLine(obj.P1)           ' value         (Get)
                          ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30043ERR_UseOfKeywordNotInInstanceMethod1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="UseOfKeywordNotInInstanceMethod1">
    <file name="a.vb">
        Class [ident1]
            Public k As Short
            Public Shared Function foo2() As String
                'COMPILEERROR: BC30043, "Me"
                Me.k = 333
                Return Nothing
            End Function
        End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30043: 'Me' is valid only within an instance method.
                Me.k = 333
                ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30043ERR_UseOfKeywordNotInInstanceMethod1_WithinFieldInitializers()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="FieldsUsingMe">
    <file name="a.vb">
Option strict on
imports system

Class C1
    private f1 as integer = 21
    private shared f2 as integer = Me.f1 + 1
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30043: 'Me' is valid only within an instance method.
    private shared f2 as integer = Me.f1 + 1
                                   ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30043ERR_UseOfKeywordNotInInstanceMethod1_MeInAttribute()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30043ERR_UseOfKeywordNotInInstanceMethod1_MeInAttribute">
    <file name="a.vb">
Option Strict On
Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices

&lt;Assembly: AssemblyCulture(Me.AAA)&gt;

&lt;StructLayout(Me.AAA)&gt;
Structure S1
End Structure
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30043: 'Me' is valid only within an instance method.
&lt;Assembly: AssemblyCulture(Me.AAA)&gt;
                           ~~
BC30043: 'Me' is valid only within an instance method.
&lt;StructLayout(Me.AAA)&gt;
              ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30043ERR_UseOfKeywordNotInInstanceMethod1_MyBaseInAttribute()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30043ERR_UseOfKeywordNotInInstanceMethod1_MyBaseInAttribute">
    <file name="a.vb">
Option Strict On
Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices

&lt;Assembly: AssemblyCulture(MyBase.AAA)&gt;

&lt;StructLayout(MyBase.AAA)&gt;
Structure S1
End Structure
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30043: 'MyBase' is valid only within an instance method.
&lt;Assembly: AssemblyCulture(MyBase.AAA)&gt;
                           ~~~~~~
BC30043: 'MyBase' is valid only within an instance method.
&lt;StructLayout(MyBase.AAA)&gt;
              ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30043ERR_UseOfKeywordNotInInstanceMethod1_MyClassInAttribute()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30043ERR_UseOfKeywordNotInInstanceMethod1_MyClassInAttribute">
    <file name="a.vb">
Option Strict On
Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices

&lt;Assembly: AssemblyCulture(MyClass.AAA)&gt;

&lt;StructLayout(MyClass.AAA)&gt;
Structure S1
End Structure
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30043: 'MyClass' is valid only within an instance method.
&lt;Assembly: AssemblyCulture(MyClass.AAA)&gt;
                           ~~~~~~~
BC30043: 'MyClass' is valid only within an instance method.
&lt;StructLayout(MyClass.AAA)&gt;
              ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30043ERR_UseOfKeywordNotInInstanceMethod1_WithinFieldInitializers_MyBase()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30043ERR_UseOfKeywordNotInInstanceMethod1_WithinFieldInitializers_MyBase">
    <file name="a.vb">
Option strict on
imports system

Class Base
    Shared Function GetBar(i As Integer) As Integer
        Return Nothing
    End Function
End Class
Class C2
    Inherits Base
    Public Shared f As Func(Of Func(Of Integer, Integer)) =
            Function() New Func(Of Integer, Integer)(AddressOf MyBase.GetBar)
    Public Shared Property P As Integer = MyBase.GetBar(1)
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30043: 'MyBase' is valid only within an instance method.
            Function() New Func(Of Integer, Integer)(AddressOf MyBase.GetBar)
                                                               ~~~~~~
BC30043: 'MyBase' is valid only within an instance method.
    Public Shared Property P As Integer = MyBase.GetBar(1)
                                          ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30043ERR_UseOfKeywordNotInInstanceMethod1_WithinFieldInitializers_MyClass()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30043ERR_UseOfKeywordNotInInstanceMethod1_WithinFieldInitializers_MyClass">
    <file name="a.vb">
Option strict on
imports system

Class C2
    Shared Function GetBar(i As Integer) As Integer
        Return Nothing
    End Function
    Public Shared f As Func(Of Func(Of Integer, Integer)) =
            Function() New Func(Of Integer, Integer)(AddressOf MyClass.GetBar)
    Public Shared Property P As Integer = MyClass.GetBar(1)
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30043: 'MyClass' is valid only within an instance method.
            Function() New Func(Of Integer, Integer)(AddressOf MyClass.GetBar)
                                                               ~~~~~~~
BC30043: 'MyClass' is valid only within an instance method.
    Public Shared Property P As Integer = MyClass.GetBar(1)
                                          ~~~~~~~
</expected>)
        End Sub

        <WorkItem(542958, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542958")>
        <Fact()>
        Public Sub BC30043ERR_UseOfKeywordNotInInstanceMethod1_MyClassInAttribute_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30043ERR_UseOfKeywordNotInInstanceMethod1_MyClassInAttribute">
    <file name="a.vb">
Option Strict On
Imports System
Imports System.Runtime.InteropServices
Public Class S1
    Const str As String = ""
    &lt;MyAttribute(MyClass.color.blue)&gt;
    Sub foo()
    End Sub
    Shared Sub main()
    End Sub
    Enum color
        blue
    End Enum
End Class
Class MyAttribute
    Inherits Attribute
    Sub New(str As S1.color)
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30043: 'MyClass' is valid only within an instance method.
    &lt;MyAttribute(MyClass.color.blue)&gt;
                 ~~~~~~~
</expected>)
        End Sub

        <WorkItem(542958, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542958")>
        <Fact()>
        Public Sub BC30043ERR_UseOfKeywordNotInInstanceMethod1_MeInAttribute_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30043ERR_UseOfKeywordNotInInstanceMethod1_MeInAttribute">
    <file name="a.vb">
Option Strict On
Imports System
Imports System.Runtime.InteropServices
Public Class S1
    Const str As String = ""
    &lt;MyAttribute(Me.color.blue)&gt;
    Sub foo()
    End Sub
    Shared Sub main()
    End Sub
    Enum color
        blue
    End Enum
End Class
Class MyAttribute
    Inherits Attribute
    Sub New(str As S1.color)
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30043: 'Me' is valid only within an instance method.
    &lt;MyAttribute(Me.color.blue)&gt;
                 ~~
</expected>)
        End Sub

        <WorkItem(542958, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542958")>
        <Fact()>
        Public Sub BC30043ERR_UseOfKeywordNotInInstanceMethod1_MyBaseInAttribute_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30043ERR_UseOfKeywordNotInInstanceMethod1_MyBaseInAttribute">
    <file name="a.vb">
Option Strict On
Imports System
Imports System.Runtime.InteropServices
Public Class BaseClass
    Enum color
        blue
    End Enum
End Class
Public Class S1
    Inherits BaseClass
    &lt;MyAttribute(MyBase.color.blue)&gt;
    Sub foo()
    End Sub
    Shared Sub main()
    End Sub
End Class
Class MyAttribute
    Inherits Attribute
    Sub New(x As S1.color)
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30043: 'MyBase' is valid only within an instance method.
    &lt;MyAttribute(MyBase.color.blue)&gt;
                 ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30044ERR_UseOfKeywordFromStructure1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="UseOfKeywordFromStructure1">
    <file name="a.vb">
        Module M1
            Structure S
                Public Overrides Function ToString() As String
                    Return MyBase.ToString()
                End Function
            End Structure
        End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30044: 'MyBase' is not valid within a structure.
                    Return MyBase.ToString()
                           ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30045ERR_BadAttributeConstructor1()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="BadAttributeConstructor1">
    <file name="a.vb"><![CDATA[
Imports System
            Module M1
                Class myattr1
                    Inherits Attribute
                    Sub New(ByVal o() As c1)
                        Me.o = o
                    End Sub
                    Public o() As c1
                End Class
                Public Class c1
                End Class
                <myattr1(Nothing)>
                Class Scen18
                End Class
                Class myattr2
                    Inherits Attribute
                    Sub New(ByVal o() As delegate1)
                        Me.o = o
                    End Sub
                    Public o() As delegate1
                End Class
                Delegate Sub delegate1()
                <myattr2(Nothing)>
                Class Scen20
                End Class
            End Module
    ]]></file>
</compilation>).
            VerifyDiagnostics(Diagnostic(ERRID.ERR_BadAttributeConstructor1, "myattr1").WithArguments("M1.c1()"),
                              Diagnostic(ERRID.ERR_BadAttributeConstructor1, "myattr2").WithArguments("M1.delegate1()"))

            Dim scen18 = compilation.GlobalNamespace.GetTypeMember("M1").GetTypeMember("Scen18")
            Dim attribute = scen18.GetAttributes().Single()
            Assert.Equal("M1.myattr1(Nothing)", attribute.ToString())
            Dim argument = attribute.CommonConstructorArguments(0)
            Assert.Null(argument.Type)
        End Sub

        <Fact, WorkItem(3380, "DevDiv_Projects/Roslyn")>
        Public Sub BC30046ERR_ParamArrayWithOptArgs()
            CreateCompilationWithMscorlib40(<compilation name="ERR_ParamArrayWithOptArgs">
                                                <file name="a.vb"><![CDATA[
                Class C1
                    Shared Sub Main()
                    End Sub
                    sub abc( optional k as string = "hi", paramarray s() as integer )
                    End Sub
                End Class
            ]]></file>
                                            </compilation>).VerifyDiagnostics(
                                          Diagnostic(ERRID.ERR_ParamArrayWithOptArgs, "s"))

        End Sub

        <Fact()>
        Public Sub BC30049ERR_ExpectedArray1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ExpectedArray1">
    <file name="a.vb">
    Module M1
        Sub Main()
            Dim boolVar_12 As Boolean
            'COMPILEERROR: BC30049, "boolVar_12"
            ReDim boolVar_12(120)

            'COMPILEERROR: BC30049, "boolVar_12", BC30811, "as"
            ReDim boolVar_12(120, 130)
        End Sub
    End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30049: 'Redim' statement requires an array.
            ReDim boolVar_12(120)
                  ~~~~~~~~~~
BC30049: 'Redim' statement requires an array.
            ReDim boolVar_12(120, 130)
                  ~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(542209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542209")>
        <Fact()>
        Public Sub BC30052ERR_ArrayRankLimit()
            CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="ArrayRankLimit">
    <file name="a.vb">
    Public Class C1
        Dim S1(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32) As Byte
        Dim S2(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33) As Byte
    End Class
    </file>
</compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_ArrayRankLimit, "(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33)"))
        End Sub

        <Fact, WorkItem(2424, "https://github.com/dotnet/roslyn/issues/2424")>
        Public Sub BC30053ERR_AsNewArray_01()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="AsNewArray">
        <file name="a.vb">
        Module M1
            Sub Foo()
                Dim c() As New System.Exception
                Dim d(), e() As New System.Exception
                Dim f(), g As New System.Exception
                Dim h, i() As New System.Exception
            End Sub

            Dim x() As New System.Exception
            Dim y(), z() As New System.Exception
            Dim u(), v As New System.Exception
            Dim w, q() As New System.Exception
        End Module
        </file>
    </compilation>)

            Dim expectedErrors1 = <errors>
BC30053: Arrays cannot be declared with 'New'.
                Dim c() As New System.Exception
                           ~~~
BC30053: Arrays cannot be declared with 'New'.
                Dim d(), e() As New System.Exception
                                ~~~
BC30053: Arrays cannot be declared with 'New'.
                Dim d(), e() As New System.Exception
                                ~~~
BC30053: Arrays cannot be declared with 'New'.
                Dim f(), g As New System.Exception
                              ~~~
BC30053: Arrays cannot be declared with 'New'.
                Dim h, i() As New System.Exception
                              ~~~
BC30053: Arrays cannot be declared with 'New'.
            Dim x() As New System.Exception
                ~~~
BC30053: Arrays cannot be declared with 'New'.
            Dim y(), z() As New System.Exception
                ~~~
BC30053: Arrays cannot be declared with 'New'.
            Dim y(), z() As New System.Exception
                     ~~~
BC30053: Arrays cannot be declared with 'New'.
            Dim u(), v As New System.Exception
                ~~~
BC30053: Arrays cannot be declared with 'New'.
            Dim w, q() As New System.Exception
                   ~~~
</errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact, WorkItem(2424, "https://github.com/dotnet/roslyn/issues/2424")>
        Public Sub BC30053ERR_AsNewArray_02()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="AsNewArray">
        <file name="a.vb">
        Module M1
            Sub Foo()
                Dim c(1) As New System.Exception
                Dim d(1), e(1) As New System.Exception
                Dim f(1), g As New System.Exception
                Dim h, i(1) As New System.Exception
            End Sub

            Dim x(1) As New System.Exception
            Dim y(1), z(1) As New System.Exception
            Dim u(1), v As New System.Exception
            Dim w, q(1) As New System.Exception
        End Module
        </file>
    </compilation>)

            Dim expectedErrors1 = <errors>
BC30053: Arrays cannot be declared with 'New'.
                Dim c(1) As New System.Exception
                            ~~~
BC30053: Arrays cannot be declared with 'New'.
                Dim d(1), e(1) As New System.Exception
                                  ~~~
BC30053: Arrays cannot be declared with 'New'.
                Dim d(1), e(1) As New System.Exception
                                  ~~~
BC30053: Arrays cannot be declared with 'New'.
                Dim f(1), g As New System.Exception
                               ~~~
BC30053: Arrays cannot be declared with 'New'.
                Dim h, i(1) As New System.Exception
                               ~~~
BC30053: Arrays cannot be declared with 'New'.
            Dim x(1) As New System.Exception
                ~~~~
BC30053: Arrays cannot be declared with 'New'.
            Dim y(1), z(1) As New System.Exception
                ~~~~
BC30053: Arrays cannot be declared with 'New'.
            Dim y(1), z(1) As New System.Exception
                      ~~~~
BC30053: Arrays cannot be declared with 'New'.
            Dim u(1), v As New System.Exception
                ~~~~
BC30053: Arrays cannot be declared with 'New'.
            Dim w, q(1) As New System.Exception
                   ~~~~
</errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30057ERR_TooManyArgs1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="TooManyArgs1">
    <file name="a.vb">
    Module M1
        Sub Main()
            test("CC", 15, 45)
        End Sub
        Sub test(ByVal name As String, ByVal age As Integer)
        End Sub
    End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30057: Too many arguments to 'Public Sub test(name As String, age As Integer)'.
            test("CC", 15, 45)
                           ~~
</expected>)
        End Sub

        ' 30057 is better here
        <WorkItem(528720, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528720")>
        <Fact()>
        Public Sub BC30057ERR_TooManyArgs1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ConstructorNotFound1">
    <file name="a.vb">
        Module M1
            Sub FOO()
                Dim DynamicArray_2() As Byte
                Dim DynamicArray_3() As Long
                'COMPILEERROR: BC30251, "New Byte(1, 2, 3, 4)"
                DynamicArray_2 = New Byte(1, 2, 3, 4)
                'COMPILEERROR: BC30251, "New Byte(1)"
                DynamicArray_3 = New Long(1)
                Exit Sub
            End Sub
        End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30057: Too many arguments to 'Public Sub New()'.
                DynamicArray_2 = New Byte(1, 2, 3, 4)
                                          ~
BC30057: Too many arguments to 'Public Sub New()'.
                DynamicArray_3 = New Long(1)
                                          ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30057ERR_TooManyArgs1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ConstructorNotFound1">
    <file name="a.vb">
        Option Infer On
        Imports System
        Module Module1
            Sub Main()
                Dim arr16 As New Integer(2, 3) { {1, 2}, {2, 1} }' Invalid
            End Sub
        End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30057: Too many arguments to 'Public Sub New()'.
                Dim arr16 As New Integer(2, 3) { {1, 2}, {2, 1} }' Invalid
                                         ~
BC30205: End of statement expected.
                Dim arr16 As New Integer(2, 3) { {1, 2}, {2, 1} }' Invalid
                                               ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30057ERR_TooManyArgs1_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ConstructorNotFound1">
    <file name="a.vb">
        Option Strict On
        Imports System
        Module Module1
            Sub Main()
                Dim myArray8 As Integer(,) = New Integer(,) 1,2,3,4,5
            End Sub
        End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30057: Too many arguments to 'Public Sub New()'.
                Dim myArray8 As Integer(,) = New Integer(,) 1,2,3,4,5
                                                         ~
BC30205: End of statement expected.
                Dim myArray8 As Integer(,) = New Integer(,) 1,2,3,4,5
                                                            ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30059ERR_RequiredConstExpr_fields()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
option Infer On

imports system
Imports microsoft.visualbasic.strings

        Class A
            Public Const X As Integer = 1
        End Class

        Class B
            Sub New(x As Action)
            End Sub

            Sub New(x As Integer)
            End Sub

            Public Const X As Integer = 2
        End Class

        Class C
            Sub New(x As Integer)
            End Sub

            Public Const X As Integer = 3
        End Class

        Class D
            Sub New(x As Func(Of Integer))
            End Sub

            Public Const X As Integer = 4
        End Class

Class C1
    Public Delegate Sub SubDel(p as integer)
    Public Shared Sub foo(p as Integer)
        Console.WriteLine("DelegateField works :) " + p.ToString())
    End Sub

    public shared function f() as integer
        return 23
    end function

    ' should work because of const propagation
    public const f1 as integer = 1 + 1

    '' should not work
    Public const f2 as SubDel = AddressOf C1.foo
    public const f3 as integer = C1.f()
    public const f4,f5 as integer = C1.f()

    '' should also give a BC30059 for inferred types
    public const f6 as object = new C1()
    public const f7 = new C1()

    public const f8 as integer = Asc(chrW(255)) ' > 127 are not const

    public const f9() as integer = new integer() {1, 2}
    public const f10 = new integer() {1, 2}

    public const f11 = GetType(Integer)
    public const f12 as system.type = GetType(Integer)

    public const f13 as integer = cint(cint(cbyte("1")))
    public const f14 as integer = cint(cint(cbyte(1))) ' works

    public const f15 as long = clng(cint(cbyte("1")))
    public const f16 as long = clng(cint(cbyte(1))) ' works


    public const ValueWorks1 as Integer = new C(23).X
    public const ValueWorks2 as Integer = new A().X
    public const ValueWorks3 as Integer = 23 + new A().X
    public const ValueWorks4 as Integer = if(nothing, 23)
    public const ValueWorks5 as Integer = if(23 = 42, 23, 42)
    public const ValueWorks6 as Integer = if(new A().X = 0, 23, 42)
    public const ValueWorks7 as Integer = if(new A(), nothing).X
    public const ValueWorks8 as Integer = if(23 = 42, 23, new A().X)
    public const ValueWorks9 as Integer = if(23 = 42, new A().X, 42)
    public const ValueWorks10 as Integer = CType("12", Integer).MaxValue ' needs option strict off ...
    public const ValueWorks11 as Integer = New B(Sub() Exit Sub).X
    public const ValueWorks12 = New D(Function() 23).X

    public const ValueDoesntWork1 as Integer = f()                       
    public const ValueDoesntWork2 as Integer = 1 + f()
    public const ValueDoesntWork3 as Integer = f() + 1

    Public shared Sub Main(args() as string)
    End sub
End Class
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
    Public const f2 as SubDel = AddressOf C1.foo
                       ~~~~~~
BC30059: Constant expression is required.
    public const f3 as integer = C1.f()
                                 ~~~~~~
BC30671: Explicit initialization is not permitted with multiple variables declared with a single type specifier.
    public const f4,f5 as integer = C1.f()
                 ~~~~~~~~~~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
    public const f4,f5 as integer = C1.f()
                                    ~~~~~~
BC30059: Constant expression is required.
    public const f6 as object = new C1()
                                ~~~~~~~~
BC30059: Constant expression is required.
    public const f7 = new C1()
                      ~~~~~~~~
BC30059: Constant expression is required.
    public const f8 as integer = Asc(chrW(255)) ' > 127 are not const
                                 ~~~~~~~~~~~~~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
    public const f9() as integer = new integer() {1, 2}
                 ~~
BC30059: Constant expression is required.
    public const f10 = new integer() {1, 2}
                       ~~~~~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
    public const f11 = GetType(Integer)
                       ~~~~~~~~~~~~~~~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
    public const f12 as system.type = GetType(Integer)
                        ~~~~~~~~~~~
BC30060: Conversion from 'String' to 'Byte' cannot occur in a constant expression.
    public const f13 as integer = cint(cint(cbyte("1")))
                                                  ~~~
BC30060: Conversion from 'String' to 'Byte' cannot occur in a constant expression.
    public const f15 as long = clng(cint(cbyte("1")))
                                               ~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
    public const ValueWorks1 as Integer = new C(23).X
                                          ~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
    public const ValueWorks2 as Integer = new A().X
                                          ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
    public const ValueWorks3 as Integer = 23 + new A().X
                                               ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
    public const ValueWorks6 as Integer = if(new A().X = 0, 23, 42)
                                             ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
    public const ValueWorks7 as Integer = if(new A(), nothing).X
                                          ~~~~~~~~~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
    public const ValueWorks8 as Integer = if(23 = 42, 23, new A().X)
                                                          ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
    public const ValueWorks9 as Integer = if(23 = 42, new A().X, 42)
                                                      ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
    public const ValueWorks10 as Integer = CType("12", Integer).MaxValue ' needs option strict off ...
                                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
    public const ValueWorks11 as Integer = New B(Sub() Exit Sub).X
                                           ~~~~~~~~~~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
    public const ValueWorks12 = New D(Function() 23).X
                                ~~~~~~~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
    public const ValueDoesntWork1 as Integer = f()                       
                                               ~~~
BC30059: Constant expression is required.
    public const ValueDoesntWork2 as Integer = 1 + f()
                                                   ~~~
BC30059: Constant expression is required.
    public const ValueDoesntWork3 as Integer = f() + 1
                                               ~~~
</expected>)
        End Sub

        ' The non-constant initializer should result in
        ' a single error, even if declaring multiple fields.
        <Fact()>
        Public Sub BC30059ERR_RequiredConstExpr_2()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Module M
    Const A, B As Integer = F()
    Function F() As Integer
        Return 0
    End Function
End Module
    </file>
</compilation>
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)
            compilation.AssertTheseDiagnostics(
<expected>
BC30671: Explicit initialization is not permitted with multiple variables declared with a single type specifier.
    Const A, B As Integer = F()
          ~~~~~~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
    Const A, B As Integer = F()
                            ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30059ERR_RequiredConstExpr_locals()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict off
imports system
Imports microsoft.visualbasic.strings

        Class A
            Public Const X As Integer = 1
        End Class

        Class B
            Sub New(x As Action)
            End Sub

            Sub New(x As Integer)
            End Sub

            Public Const X As Integer = 2
        End Class

        Class C
            Sub New(x As Integer)
            End Sub

            Public Const X As Integer = 3
        End Class

        Class D
            Sub New(x As Func(Of Integer))
            End Sub

            Public Const X As Integer = 4
        End Class

Class C1
    Public Delegate Sub SubDel(p as integer)
    Public Shared Sub foo(p as Integer)
        Console.WriteLine("DelegateField works :) " + p.ToString())
    End Sub

    public shared function f() as integer
        return 23
    end function

    Public Sub Main()
        ' should work because of const propagation
        const f1 as integer = 1 + 1

        ' should not work
        const f2 as SubDel = AddressOf C1.foo
        const f3 as integer = C1.f()
        const f4,f5 as integer = C1.f()

        ' should also give a BC30059 for inferred types
        const f6 as object = new C1()
        const f7 = new C1()

        const f8 as integer = Asc(chrW(255)) ' > 127 are not const

        const f9() as integer = new integer() {1, 2}
        const f10 = new integer() {1, 2}

        const f11 = GetType(Integer)
        const f12 as system.type = GetType(Integer)

        const f13 as integer = cint(cint(cbyte("1")))
        const f14 as integer = cint(cint(cbyte(1))) ' works

        const f15 as long = clng(cint(cbyte("1")))
        const f16 as long = clng(cint(cbyte(1))) ' works

        const ValueWorks1 as Integer = new C(23).X
        const ValueWorks2 as Integer = new A().X
        const ValueWorks3 as Integer = 23 + new A().X
        const ValueWorks4 as Integer = if(nothing, 23)
        const ValueWorks5 as Integer = if(23 = 42, 23, 42)
        const ValueWorks6 as Integer = if(new A().X = 0, 23, 42)
        const ValueWorks7 as Integer = if(new A(), nothing).X
        const ValueWorks8 as Integer = if(23 = 42, 23, new A().X)
        const ValueWorks9 as Integer = if(23 = 42, new A().X, 42)
        const ValueWorks10 as Integer = CType("12", Integer).MaxValue ' needs option strict off ...
        const ValueWorks11 as Integer = New B(Sub() Exit Sub).X
        const ValueWorks12 as Integer = New D(Function() 23).X

        const ValueDoesntWork1 as Integer = f()                       

        Dim makeThemUsed as long = f1 + f3 + f4 + f5 + f8 + f13 + f14 + f15 + f16 + 
                                   ValueWorks1 + ValueWorks2 + ValueWorks3 + ValueWorks4 + 
                                   ValueWorks5 + ValueWorks6 + ValueWorks7 + ValueWorks8 + ValueWorks9 +
                                   ValueWorks10 + ValueWorks11 + ValueWorks12
    End Sub
End Class
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
        const f2 as SubDel = AddressOf C1.foo
                    ~~~~~~
BC30059: Constant expression is required.
        const f3 as integer = C1.f()
                              ~~~~~~
BC30438: Constants must have a value.
        const f4,f5 as integer = C1.f()
              ~~
BC30671: Explicit initialization is not permitted with multiple variables declared with a single type specifier.
        const f4,f5 as integer = C1.f()
              ~~~~~~~~~~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
        const f4,f5 as integer = C1.f()
                                 ~~~~~~
BC30059: Constant expression is required.
        const f6 as object = new C1()
                             ~~~~~~~~
BC30059: Constant expression is required.
        const f7 = new C1()
                   ~~~~~~~~
BC30059: Constant expression is required.
        const f8 as integer = Asc(chrW(255)) ' > 127 are not const
                              ~~~~~~~~~~~~~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
        const f9() as integer = new integer() {1, 2}
              ~~~~
BC30059: Constant expression is required.
        const f10 = new integer() {1, 2}
                    ~~~~~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
        const f11 = GetType(Integer)
                    ~~~~~~~~~~~~~~~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
        const f12 as system.type = GetType(Integer)
                     ~~~~~~~~~~~
BC30060: Conversion from 'String' to 'Byte' cannot occur in a constant expression.
        const f13 as integer = cint(cint(cbyte("1")))
                                               ~~~
BC30060: Conversion from 'String' to 'Byte' cannot occur in a constant expression.
        const f15 as long = clng(cint(cbyte("1")))
                                            ~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        const ValueWorks1 as Integer = new C(23).X
                                       ~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        const ValueWorks2 as Integer = new A().X
                                       ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        const ValueWorks3 as Integer = 23 + new A().X
                                            ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        const ValueWorks6 as Integer = if(new A().X = 0, 23, 42)
                                          ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        const ValueWorks7 as Integer = if(new A(), nothing).X
                                       ~~~~~~~~~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        const ValueWorks8 as Integer = if(23 = 42, 23, new A().X)
                                                       ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        const ValueWorks9 as Integer = if(23 = 42, new A().X, 42)
                                                   ~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        const ValueWorks10 as Integer = CType("12", Integer).MaxValue ' needs option strict off ...
                                        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        const ValueWorks11 as Integer = New B(Sub() Exit Sub).X
                                        ~~~~~~~~~~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        const ValueWorks12 as Integer = New D(Function() 23).X
                                        ~~~~~~~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
        const ValueDoesntWork1 as Integer = f()                       
                                            ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30059ERR_RequiredConstExpr_3()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
Option Infer On
imports system

Class C1
    Public Delegate Sub SubDel(p as integer)
    Public Shared Sub foo(p as Integer)
        Console.WriteLine("DelegateField works :) " + p.ToString())
    End Sub

    public shared function f() as integer
        return 23
    end function

    public shared function g(p as integer) as integer
        return 23
    end function

    ' should not work
    Public const f2 = AddressOf C1.foo
    Public const f3 as object = AddressOf C1.foo
    public const f4 as integer = 1 + 2 + 3 + f() 
    public const f5 as boolean = not (f() = 23)
    public const f6 as integer = f() + f() + f()
    public const f7 as integer = g(1 + 2 + f())

    Public shared Sub Main(args() as string)
    End sub
End Class
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30059: Constant expression is required.
    Public const f2 = AddressOf C1.foo
                      ~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
    Public const f3 as object = AddressOf C1.foo
                                ~~~~~~~~~~~~~~~~
BC30059: Constant expression is required.
    public const f4 as integer = 1 + 2 + 3 + f() 
                                             ~~~
BC30059: Constant expression is required.
    public const f5 as boolean = not (f() = 23)
                                      ~~~
BC30059: Constant expression is required.
    public const f6 as integer = f() + f() + f()
                                 ~~~
BC30059: Constant expression is required.
    public const f6 as integer = f() + f() + f()
                                       ~~~
BC30059: Constant expression is required.
    public const f6 as integer = f() + f() + f()
                                             ~~~
BC30059: Constant expression is required.
    public const f7 as integer = g(1 + 2 + f())
                                 ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestArrayLocalConst()

            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main()
        Const A As Integer() = Nothing
        Console.Write(A)
    End Sub
End Module
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
        Const A As Integer() = Nothing
              ~
</expected>)

        End Sub

        <Fact()>
        Public Sub BC30059ERR_RequiredConstExpr_Attr()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ERR_RequiredConstExpr_Attr">
        <file name="at30059.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.All)>
Public Class MyAttribute
    Inherits Attribute
    Public Sub New(p As ULong)
    End Sub
End Class

<My(Foo.FG)>
Public Class Foo
    Public Shared FG As ULong = 12345
    Public Function F() As Byte
        Dim x As Byte = 1
        Return x
    End Function
End Class
    ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_RequiredConstExpr, "Foo.FG"))

        End Sub

        <WorkItem(542967, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542967")>
        <Fact()>
        Public Sub BC30059ERR_RequiredConstExpr_QueryInAttr()

            CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="ERR_RequiredConstExpr_Attr">
        <file name="at30059.vb"><![CDATA[
Imports System
Imports System.Linq
Class Program
    Const q As String = ""
    Sub Main(args As String())
    End Sub
    <My((From x In q Select x).Count())>
    Shared Sub sum()
    End Sub
End Class
Class MyAttribute
    Inherits Attribute
    Sub New(s As Integer)
    End Sub
End Class
    ]]></file>
    </compilation>, references:={Net40.References.SystemCore}).VerifyDiagnostics(Diagnostic(ERRID.ERR_RequiredConstExpr, "(From x In q Select x).Count()"))
        End Sub

        <WorkItem(542967, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542967")>
        <Fact()>
        Public Sub BC30059ERR_RequiredConstExpr_QueryInAttr_2()

            CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="ERR_RequiredConstExpr_Attr">
        <file name="at30059.vb"><![CDATA[
Imports System
Imports System.Linq
Class Program
    Const q As String = ""
    Sub Main(args As String())
    End Sub
    Public F1 As Object
    <My((From x In q Select F1).Count())>
    Shared Sub sum()
    End Sub
End Class
Class MyAttribute
    Inherits Attribute
    Sub New(s As Integer)
    End Sub
End Class
    ]]></file>
    </compilation>, references:={Net40.References.SystemCore}).VerifyDiagnostics(Diagnostic(ERRID.ERR_BadInstanceMemberAccess, "F1"))
        End Sub

        <WorkItem(542967, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542967")>
        <Fact()>
        Public Sub BC30059ERR_RequiredConstExpr_QueryInAttr_3()

            CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="ERR_RequiredConstExpr_Attr">
        <file name="at30059.vb"><![CDATA[
Imports System
Imports System.Linq

<My((From x In "s" Select x).Count())>
Class Program
    Public F1 As Integer
End Class

<My((From x In "s" Select Program.F1).Count())>
Class Program2
End Class

Class MyAttribute
    Inherits Attribute
    Sub New(s As Integer)
    End Sub
End Class
    ]]></file>
    </compilation>, references:={Net40.References.SystemCore}).VerifyDiagnostics({Diagnostic(ERRID.ERR_RequiredConstExpr, "(From x In ""s"" Select x).Count()"),
                                                                    Diagnostic(ERRID.ERR_ObjectReferenceNotSupplied, "Program.F1")})
        End Sub

        <Fact()>
        Public Sub BC30059ERR_RequiredConstExpr_XmlEmbeddedExpression()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Private Const F1 = Nothing
    Private Const F2 As String = "v2"
    Private Const F3 = <%= Nothing %>
    Private Const F4 = <%= "v4" %>
    Private Const F5 As String = <%= "v5" %>
    Private F6 As Object = <x a0=<%= "v0" %> a1=<%= F1 %> a2=<%= F2 %> a3=<%= F3 %> a4=<%= F4 %> a5=<%= F5 %>/>
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30059: Constant expression is required.
    Private Const F3 = <%= Nothing %>
                       ~~~~~~~~~~~~~~
BC31172: An embedded expression cannot be used here.
    Private Const F3 = <%= Nothing %>
                       ~~~~~~~~~~~~~~
BC30059: Constant expression is required.
    Private Const F4 = <%= "v4" %>
                       ~~~~~~~~~~~
BC31172: An embedded expression cannot be used here.
    Private Const F4 = <%= "v4" %>
                       ~~~~~~~~~~~
BC30059: Constant expression is required.
    Private Const F5 As String = <%= "v5" %>
                                 ~~~~~~~~~~~
BC31172: An embedded expression cannot be used here.
    Private Const F5 As String = <%= "v5" %>
                                 ~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC30060ERR_RequiredConstConversion2()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
imports system

Class C1
    ' should show issues
    public const f1 as integer = CInt("23")
    public const f2 as integer = CType("23", integer)
    public const f3 as byte = CType(300, byte)
    public const f4 as byte = CType(300, BORG)
    public const f5 as byte = 300
    public const f6 as string = 23
    public const f10 as date = CDate("November 04, 2008")
    public const f13 as decimal = Ctype("20100607",decimal)


    ' should not show issues
    public const f7 as integer = CInt(23)
    public const f8 as integer = CType(23, integer)
    public const f9 as byte = CType(254, byte)
    public const f11 as date = Ctype(#06/07/2010#,date)    
    public const f12 as decimal = Ctype(20100607,decimal)

    Public shared Sub Main(args() as string)
    End sub
End Class
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30060: Conversion from 'String' to 'Integer' cannot occur in a constant expression.
    public const f1 as integer = CInt("23")
                                      ~~~~
BC30060: Conversion from 'String' to 'Integer' cannot occur in a constant expression.
    public const f2 as integer = CType("23", integer)
                                       ~~~~
BC30439: Constant expression not representable in type 'Byte'.
    public const f3 as byte = CType(300, byte)
                                    ~~~
BC30002: Type 'BORG' is not defined.
    public const f4 as byte = CType(300, BORG)
                                         ~~~~
BC30439: Constant expression not representable in type 'Byte'.
    public const f5 as byte = 300
                              ~~~
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'String'.
    public const f6 as string = 23
                                ~~
BC30060: Conversion from 'String' to 'Date' cannot occur in a constant expression.
    public const f10 as date = CDate("November 04, 2008")
                                     ~~~~~~~~~~~~~~~~~~~
BC30060: Conversion from 'String' to 'Decimal' cannot occur in a constant expression.
    public const f13 as decimal = Ctype("20100607",decimal)
                                        ~~~~~~~~~~

</expected>)
        End Sub

        <Fact()>
        Public Sub BC30060ERR_RequiredConstConversion2_StrictOff()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict off
imports system

Class C1
    public const f6 as string = 23
    public const f7 as string = CType(23,string)

    Public shared Sub Main(args() as string)
    End sub
End Class
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30060: Conversion from 'Integer' to 'String' cannot occur in a constant expression.
    public const f6 as string = 23
                                ~~
BC30060: Conversion from 'Integer' to 'String' cannot occur in a constant expression.
    public const f7 as string = CType(23,string)
                                      ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30064ERR_ReadOnlyAssignment()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ReadOnlyAssignment">
    <file name="a.vb">
    Imports System

    Module M1

    Class ReferenceType
        Implements IDisposable
        Dim dummy As Integer
        Public Sub Dispose() Implements System.IDisposable.Dispose
        End Sub
    End Class

    Class TestClass
        ReadOnly Name As String = "Cici"
        Sub test()
            Name = "string"

            ' variables declared in a using statement are considered read only as well
            Using a As New ReferenceType(), b As New ReferenceType()
                a = New ReferenceType()
                b = New ReferenceType()
            End Using
        End Sub
    End Class
End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
            Name = "string"
            ~~~~
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
                a = New ReferenceType()
                ~
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
                b = New ReferenceType()
                ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30065ERR_ExitSubOfFunc_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ExitSubOfFunc">
        <file name="a.vb">
        Public Class C1
                    Function FOO()
                        If (True)
                            Exit Sub
                        End If
                        Return Nothing
                    End Function
                End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30065: 'Exit Sub' is not valid in a Function or Property.
                            Exit Sub
                            ~~~~~~~~
     </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30067ERR_ExitFuncOfSub_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ExitFuncOfSub">
        <file name="a.vb">
        Public Class C1
            Sub FOO()
                If (True)
        lb1:        Exit Function
                End If
            End Sub
        End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30067: 'Exit Function' is not valid in a Sub or Property.
        lb1:        Exit Function
                    ~~~~~~~~~~~~~
     </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30068ERR_LValueRequired()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LValueRequired">
    <file name="a.vb">
    Module M1
        Class TestClass
            ReadOnly Name As String = "Cici"
            Sub test()
                Dim obj As Cls1
                obj.Test = 1
                obj.Test() = 1
            End Sub
        End Class
        Class Cls1
            Public Overridable Sub Test()
            End Sub
        End Class
    End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42104: Variable 'obj' is used before it has been assigned a value. A null reference exception could result at runtime.
                obj.Test = 1
                ~~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
                obj.Test = 1
                ~~~~~~~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
                obj.Test() = 1
                ~~~~~~~~~~    
</expected>)
        End Sub

        <WorkItem(575055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/575055")>
        <Fact>
        Public Sub BC30068ERR_IdentifierWithSameNameDifferentScope()
            Dim source = <compilation>
                             <file name="DuplicateID.vb"><![CDATA[
Module Module1    
	Dim list2 As Integer() = {1}
	Dim ddd = From i In list2 Where i > Foo(Function(m1) If(True, Sub(m2) Call Function(m3)
                                                                                   Return Sub() If True Then For Each i In list2 : m1 = i : Exit Sub : Exit For : Next
                                                                               End Function(m2), Sub(m2) Call Function(m3)
                                                                                                                  Return Sub() If True Then For Each i In list2 : m1 = i : Exit Sub : Exit For : Next
                                                                                                              End Function(m2)))
	Sub Main()    
	End Sub    

	Function Foo(ByVal x)        
		Return x.Invoke(1)
	End Function
End Module
    ]]></file></compilation>

            CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedQueryableSource, "list2").WithArguments("Integer()"),
                                                                                                 Diagnostic(ERRID.ERR_LValueRequired, "i"),
                                                                                                 Diagnostic(ERRID.ERR_LValueRequired, "i"))
        End Sub

        ' change error 30098 to 30068
        <WorkItem(538107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538107")>
        <Fact()>
        Public Sub BC30068ERR_LValueRequired_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ReadOnlyProperty1">
    <file name="a.vb">
    Module SB008mod
        Public ReadOnly Name As String
        Public ReadOnly Name1 As Struct1
        Public ReadOnly Name2 As Class1

        Sub SB008()
            Name = "15"
            Name1.Field = "15"
            Name2.Field = "15"
            System.TypeCode.Boolean=0
            A().Field="15"
        End Sub

        Function A() As Struct1 
            Return Nothing
        End Function
    End Module

    Structure Struct1
        Public Field As String
    End Structure

    Class Class1
        Public Field As String
    End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
            Name = "15"
            ~~~~
BC30064: 'ReadOnly' variable cannot be the target of an assignment.
            Name1.Field = "15"
            ~~~~~~~~~~~
BC30074: Constant cannot be the target of an assignment.
            System.TypeCode.Boolean=0
            ~~~~~~~~~~~~~~~~~~~~~~~
BC30068: Expression is a value and therefore cannot be the target of an assignment.
            A().Field="15"
            ~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30068ERR_LValueRequired_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="LValueRequired">
    <file name="a.vb">
    Class A
        Property P
        Shared Property Q
    End Class
    Structure B
        Property P
        Shared Property Q
    End Structure
    Class C
        Property P As A
        Property Q As B
        Sub M()
            P.P = Nothing ' no error
            Q.P = Nothing ' BC30068
            A.Q = Nothing ' no error
            B.Q = Nothing ' no error
        End Sub
    End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30068: Expression is a value and therefore cannot be the target of an assignment.
            Q.P = Nothing ' BC30068
            ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30069ERR_ForIndexInUse1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ForIndexInUse1">
    <file name="a.vb">
    Module A
        Sub TEST()
            Dim n(3) As Integer
            Dim u As Integer
            For u = n(0) To n(3) Step n(0)
                ' BC30069: For loop control variable 'u' already in use by an enclosing For loop.
                For u = 1 To 9
                Next
            Next
        End Sub
    End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30069: For loop control variable 'u' already in use by an enclosing For loop.
                For u = 1 To 9
                    ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30070ERR_NextForMismatch1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="NextForMismatch1">
    <file name="a.vb">
    Module A
        Sub TEST()
            Dim n(3) As Integer
            Dim u As Integer
            Dim k As Integer
            For u = n(0) To n(3) Step n(0)
                For k = 1 To 9
                Next u
            Next k
        End Sub
    End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30070: Next control variable does not match For loop control variable 'k'.
                Next u
                     ~
BC30070: Next control variable does not match For loop control variable 'u'.
            Next k
                 ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30070ERR_NextForMismatch1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="NextForMismatch1">
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        Dim S As String = "ABC"
        Dim T As String = "XYZ"
        For Each x As Char In S
            For Each y As Char In T
        Next y, x

        For Each x As Char In S
            For Each y As Char In T
        Next x, y
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30070: Next control variable does not match For loop control variable 'y'.
        Next x, y
             ~
BC30451: 'y' is not declared. It may be inaccessible due to its protection level.
        Next x, y
                ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30074ERR_CantAssignToConst()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="CantAssignToConst">
    <file name="a.vb">
    Class c1_0
        Public foo As Byte
    End Class
    Class c2_1
        Inherits c1_0
        Public Shadows Const foo As Short = 15
        Sub test()
            'COMPILEERROR: BC30074, "foo"
            foo = 1
        End Sub
    End Class
    Class c3_1
        Inherits c1_0
        Public Shadows Const foo As Short = 15
    End Class
    Class c2_2
        Sub test()
            Dim obj As c3_1
            obj.foo = 10
        End Sub
    End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30074: Constant cannot be the target of an assignment.
            foo = 1
            ~~~
BC30074: Constant cannot be the target of an assignment.
            obj.foo = 10
            ~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
            obj.foo = 10
            ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30075ERR_NamedSubscript()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="NamedSubscript">
    <file name="a.vb">
    Class c1
        Sub test()
            Dim Array As Integer() = new integer(){1}
            Array(Index:=10) = 1
        End Sub
    End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30075: Named arguments are not valid as array subscripts.
            Array(Index:=10) = 1
            ~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30089ERR_ExitDoNotWithinDo()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ERR_ExitDoNotWithinDo">
    <file name="a.vb">
    Structure myStruct1
        Public Sub m(ByVal s As String)
            Select Case s
                Case "userID"
                    Exit do
            End Select
        End Sub
    End Structure
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30089: 'Exit Do' can only appear inside a 'Do' statement.
                    Exit do
                    ~~~~~~~    
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30089ERR_ExitDoNotWithinDo_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ERR_ExitDoNotWithinDo">
    <file name="a.vb">
    Structure myStruct1
        Public Sub m(ByVal s As String)
            If s Then
                Exit Do
            Else
            End If
        End Sub
    End Structure
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30089: 'Exit Do' can only appear inside a 'Do' statement.
                Exit Do
                ~~~~~~~    
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30094ERR_MultiplyDefined1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MultiplyDefined1">
    <file name="a.vb">
    Module M1
        Sub Main()
            SB008()
        End Sub
        Sub SB008()
    [cc]:
            'COMPILEERROR: BC30094, "cc"
    cc:
            Exit Sub
        End Sub
    End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30094: Label 'cc' is already defined in the current method.
    cc:
    ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub Bug585223_notMultiplyDefined()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MultiplyDefined1">
    <file name="a.vb">
        <![CDATA[

Module Program
    Sub Main()
&H100000000:
&H000000000:
    End Sub
End Module

]]>
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact()>
        Public Sub Bug585223_notMultiplyDefined_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MultiplyDefined1">
    <file name="a.vb">
        <![CDATA[

Module Program
    Sub Main()
&HF:
&HFF:
&HFFF:
&HFFFF:
&HFFFFF:
&HFFFFFF:
&HFFFFFFF:
&HFFFFFFFF:
&HFFFFFFFFF:
&HFFFFFFFFFF:
&HFFFFFFFFFFF:
&HFFFFFFFFFFFF:
&HFFFFFFFFFFFFF:
&HFFFFFFFFFFFFFF:
&HFFFFFFFFFFFFFFF:
&HFFFFFFFFFFFFFFFF:
    End Sub
End Module


]]>
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30096ERR_ExitForNotWithinFor()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ExitForNotWithinFor">
    <file name="a.vb">
    Structure myStruct1
        Public Sub m(ByVal s As String)
            Select Case s
                Case "userID"
                    Exit For
            End Select
        End Sub
    End Structure
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30096: 'Exit For' can only appear inside a 'For' statement.
                    Exit For
                    ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30096ERR_ExitForNotWithinFor_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ExitForNotWithinFor">
    <file name="a.vb">
    Structure myStruct1
        Public Sub m(ByVal s As String)
            If s Then
                Exit For
            Else
            End If
        End Sub
    End Structure
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30096: 'Exit For' can only appear inside a 'For' statement.
                Exit For
                ~~~~~~~~   
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30097ERR_ExitWhileNotWithinWhile()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ExitWhileNotWithinWhile">
    <file name="a.vb">
    Structure myStruct1
        Public Sub m(ByVal s As String)
            Select Case s
                Case "userID"
                    Exit While
            End Select
        End Sub
    End Structure
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30097: 'Exit While' can only appear inside a 'While' statement.
                    Exit While
                    ~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30097ERR_ExitWhileNotWithinWhile_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ExitWhileNotWithinWhile">
    <file name="a.vb">
    Structure myStruct1
        Public Sub m(ByVal s As String)
            If s Then
                Exit While
            Else
            End If
        End Sub
    End Structure
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30097: 'Exit While' can only appear inside a 'While' statement.
                Exit While
                ~~~~~~~~~~   
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30099ERR_ExitSelectNotWithinSelect()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ExitSelectNotWithinSelect">
    <file name="a.vb">
    Structure myStruct1
        Public Sub m(ByVal s As String)
            If s Then
                Exit Select
            Else
            End If
        End Sub
    End Structure
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30099: 'Exit Select' can only appear inside a 'Select' statement.
                Exit Select
                ~~~~~~~~~~~   
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30101ERR_BranchOutOfFinally()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="BranchOutOfFinally">
    <file name="a.vb">
    Imports System    
    Module M1
        Sub Foo()
            Try
                Try
    Label1:         
                Catch

                Finally
                    GoTo Label1
                End Try
            Catch
            Finally
            End Try
            Exit Sub
        End Sub
    End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30101: Branching out of a 'Finally' is not valid.
                    GoTo Label1
                         ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30101ERR_BranchOutOfFinally2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="BC30101ERR_BranchOutOfFinally2">
    <file name="a.vb">
    Imports System    
    Module M1
        Sub Foo()
            Try
                Try
                Catch
                Finally
Label2:
                    GoTo Label2
                End Try
            Catch
            Finally
            End Try
            Exit Sub
        End Sub
    End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub BC30101ERR_BranchOutOfFinally3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="BC30101ERR_BranchOutOfFinally3">
    <file name="a.vb">
    Imports System    
    Module M1
        Sub Foo()
            Try
            Catch
            Finally
Label2:
                GoTo Label2
            End Try
        End Sub
    End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub BC30101ERR_BranchOutOfFinally4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="BC30101ERR_BranchOutOfFinally4">
    <file name="a.vb">
    Imports System    
    Module M1
        Sub Foo()
            Try
            Catch
            Finally
                GoTo L2
L2:
                GoTo L2
            End Try
        End Sub
    End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub BC30103ERR_QualNotObjectRecord1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
    Imports System
    Enum E
        A
    End Enum
    Class C
        Shared Sub M(Of T)()
            Dim [object] As Object = Nothing
            M([object]!P)
            Dim [enum] As E = E.A
            M([enum]!P)
            Dim [boolean] As Boolean = False
            M([boolean]!P)
            Dim [char] As Char = Nothing
            M([char]!P)
            Dim [sbyte] As SByte = Nothing
            M([sbyte]!P)
            Dim [byte] As Byte = Nothing
            M([byte]!P)
            Dim [int16] As Int16 = Nothing
            M([int16]!P)
            Dim [uint16] As UInt16 = Nothing
            M([uint16]!P)
            Dim [int32] As Int32 = Nothing
            M([int32]!P)
            Dim [uint32] As UInt32 = Nothing
            M([uint32]!P)
            Dim [int64] As Int64 = Nothing
            M([int64]!P)
            Dim [uint64] As UInt64 = Nothing
            M([uint64]!P)
            Dim [decimal] As Decimal = Nothing
            M([decimal]!P)
            Dim [single] As Single = Nothing
            M([single]!P)
            Dim [double] As Double = Nothing
            M([double]!P)
            Dim [type] As Type = Nothing
            M([type]!P)
            Dim [array] As Integer() = Nothing
            M([array]!P)
            Dim [nullable] As Nullable(Of Integer) = Nothing
            M([nullable]!P)
            Dim [datetime] As DateTime = Nothing
            M([datetime]!P)
            Dim [action] As Action = Nothing
            M([action]!P)
            Dim tp As T = Nothing
            M(tp!P)
        End Sub
        Shared Sub M(o)
        End Sub
    End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'E'.
            M([enum]!P)
              ~~~~~~
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'Boolean'.
            M([boolean]!P)
              ~~~~~~~~~
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'Char'.
            M([char]!P)
              ~~~~~~
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'SByte'.
            M([sbyte]!P)
              ~~~~~~~
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'Byte'.
            M([byte]!P)
              ~~~~~~
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'Short'.
            M([int16]!P)
              ~~~~~~~
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'UShort'.
            M([uint16]!P)
              ~~~~~~~~
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'Integer'.
            M([int32]!P)
              ~~~~~~~
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'UInteger'.
            M([uint32]!P)
              ~~~~~~~~
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'Long'.
            M([int64]!P)
              ~~~~~~~
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'ULong'.
            M([uint64]!P)
              ~~~~~~~~
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'Decimal'.
            M([decimal]!P)
              ~~~~~~~~~
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'Single'.
            M([single]!P)
              ~~~~~~~~
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'Double'.
            M([double]!P)
              ~~~~~~~~
BC30367: Class 'Type' cannot be indexed because it has no default property.
            M([type]!P)
              ~~~~~~
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'Integer()'.
            M([array]!P)
              ~~~~~~~
BC30690: Structure 'Integer?' cannot be indexed because it has no default property.
            M([nullable]!P)
              ~~~~~~~~~~
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'Date'.
            M([datetime]!P)
              ~~~~~~~~~~
BC30555: Default member of 'Action' is not a property.
            M([action]!P)
              ~~~~~~~~
BC30547: 'T' cannot be indexed because it has no default property.
            M(tp!P)
              ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30103ERR_QualNotObjectRecord1a()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
    Enum E
        A
    End Enum
    Class C
        Shared Sub M(Of T)()
            Dim [string] As String = Nothing
            M([string]!P)
        End Sub
        Shared Sub M(o)
        End Sub
    End Class
    </file>
</compilation>)
            compilation.VerifyEmitDiagnostics(
                Diagnostic(ERRID.ERR_MissingRuntimeHelper, "P").WithArguments("Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger"))
        End Sub

        <Fact()>
        Public Sub BC30103ERR_QualNotObjectRecord2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="QualNotObjectRecord1">
    <file name="a.vb">
    Imports System
    Module BitOp001mod
        Sub BitOp001()
            Dim b As Byte = 2
            Dim c As Byte = 3

            Dim s As Short = 2
            Dim t As Short = 3

            Dim i As Integer = 2
            Dim j As Integer = 3

            Dim l As Long = 2
            Dim m As Long = 3
            b = b!c
            s = s!t
            i = i!j
            l = l!m
        End Sub
    End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'Byte'.
            b = b!c
                ~
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'Short'.
            s = s!t
                ~
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'Integer'.
            i = i!j
                ~
BC30103: '!' requires its left operand to have a type parameter, class or interface type, but this operand has the type 'Long'.
            l = l!m
                ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30105ERR_TooFewIndices()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="TooFewIndices">
    <file name="a.vb">
    Imports System
    Module M1
        Sub AryChg001()
            Dim a() As Integer = New Integer() {9, 10}
            ReDim a(10)
            Dim a8() As Integer = New Integer() {1, 2}
            Dim b8() As Integer = New Integer() {3, 4}
            Dim c8 As Integer
            a8() = b8
            b8 = a()
            a8() = c8
            c8 = a8()
        End Sub
    End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30105: Number of indices is less than the number of dimensions of the indexed array.
            a8() = b8
              ~~
BC30105: Number of indices is less than the number of dimensions of the indexed array.
            b8 = a()
                  ~~
BC30105: Number of indices is less than the number of dimensions of the indexed array.
            a8() = c8
              ~~
BC30105: Number of indices is less than the number of dimensions of the indexed array.
            c8 = a8()
                   ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30106ERR_TooManyIndices()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="TooManyIndices">
    <file name="a.vb">
    Imports System
    Module M1
        Sub AryChg001()
            Dim a() As Integer = New Integer() {9, 10}
            ReDim a(10)
            Dim a8() As Integer = New Integer() {1, 2}
            Dim b8() As Integer = New Integer() {3, 4}
            Dim c8 As Integer
            a8(1, 2) = b8(1)
            b8 = a(0, 4)
            a8(4, 5, 6) = c8
            c8 = a8(1, 2)
        End Sub
    End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30106: Number of indices exceeds the number of dimensions of the indexed array.
            a8(1, 2) = b8(1)
              ~~~~~~
BC30106: Number of indices exceeds the number of dimensions of the indexed array.
            b8 = a(0, 4)
                  ~~~~~~
BC30106: Number of indices exceeds the number of dimensions of the indexed array.
            a8(4, 5, 6) = c8
              ~~~~~~~~~
BC30106: Number of indices exceeds the number of dimensions of the indexed array.
            c8 = a8(1, 2)
                   ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30107ERR_EnumNotExpression1()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="EnumNotExpression1">
    <file name="a.vb">
    Option Strict On
    Module BitOp001mod1
        Sub BitOp001()
            Dim b As Byte = 2
            Dim c As Byte = 3
            Dim s As Short = 2
            Dim t As Short = 3
            Dim i As Integer = 2
            Dim j As Integer = 3
            Dim l As Long = 2
            Dim m As Long = 3
            b = b &amp; c
            b = b ^ c
            s = s &amp; t
            s = s ^ t
            i = i &amp; j
            i = i ^ j
            l = l &amp; m
            l = l ^ m
            Exit Sub
        End Sub
    End Module
    </file>
</compilation>).VerifyDiagnostics(
    Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "b & c").WithArguments("String", "Byte"),
    Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "b ^ c").WithArguments("Double", "Byte"),
    Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "s & t").WithArguments("String", "Short"),
    Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "s ^ t").WithArguments("Double", "Short"),
    Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "i & j").WithArguments("String", "Integer"),
    Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "i ^ j").WithArguments("Double", "Integer"),
    Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "l & m").WithArguments("String", "Long"),
    Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "l ^ m").WithArguments("Double", "Long"))

        End Sub

        <Fact()>
        Public Sub BC30108ERR_TypeNotExpression1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="TypeNotExpression1">
    <file name="a.vb">
    Module Module1
        Sub Main()
            Module1
        End Sub
    End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30108: 'Module1' is a type and cannot be used as an expression.
            Module1
            ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30108ERR_TypeNotExpression1_1()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="TypeNotExpression1">
    <file name="a.vb">
Imports System.Collections.Generic
Module Program
    Sub Main()
        Dim lst As New List(Of String) From {Program, "abc", "def", "ghi"}
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_TypeNotExpression1, "Program").WithArguments("Program"))
        End Sub

        <WorkItem(545166, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545166")>
        <Fact()>
        Public Sub BC30109ERR_ClassNotExpression1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ClassNotExpression1">
    <file name="a.vb">
        Imports System
        Module M1
            Sub FOO()
                Dim c As Object
                c = String(3, "Hai123")
                c = String
            End Sub
        End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30109: 'String' is a class type and cannot be used as an expression.
                c = String(3, "Hai123")
                    ~~~~~~
BC30109: 'String' is a class type and cannot be used as an expression.
                c = String
                    ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30110ERR_StructureNotExpression1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="StructureNotExpression1">
    <file name="a.vb">
        Imports System
        Structure S1
        End Structure
        Module M1
            Sub FOO()
                Dim c As Object
                c = S1(3, "Hai123")
                c = S1
            End Sub
        End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30110: 'S1' is a structure type and cannot be used as an expression.
                c = S1(3, "Hai123")
                    ~~
BC30110: 'S1' is a structure type and cannot be used as an expression.
                c = S1
                    ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30111ERR_InterfaceNotExpression1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="StructureNotExpression1">
    <file name="a.vb">
        Imports System
        Interface S1
        End Interface
        Module M1
            Sub FOO()
                Dim c As Object
                c = S1(3, "Hai123")
                c = S1
            End Sub
        End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30111: 'S1' is an interface type and cannot be used as an expression.
                c = S1(3, "Hai123")
                    ~~
BC30111: 'S1' is an interface type and cannot be used as an expression.
                c = S1
                    ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30112ERR_NamespaceNotExpression1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="NamespaceNotExpression1">
    <file name="a.vb">
        Imports System
        Module M1
            Sub Foo()
                'COMPILEERROR: BC30112, "Text$"
                Text$
            End Sub
        End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30112: 'System.Text' is a namespace and cannot be used as an expression.
                Text$
                ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30112ERR_NamespaceNotExpression2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="NamespaceNotExpression1">
    <file name="a.vb">
        Option Infer On

        Namespace X
            Class Program
                Sub Main()
                    'COMPILEERROR: BC30112, "x"
                    For Each x In ""
                    Next
                End Sub
            End Class
        End Namespace
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30112: 'X' is a namespace and cannot be used as an expression.
                    For Each x In ""
                             ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30114ERR_XmlPrefixNotExpression()
            Dim options = TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse({"<xmlns:p1=""..."">"}))
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Imports <xmlns:p2="...">
Imports <xmlns:p4="...">
Module M
    Private F1 As Object = p1
    Private F2 As Object = P1
    Private F3 As Object = xml
    Private F4 As Object = XML
    Private F5 As Object = xmlns
    Private F6 As Object = XMLNS
    Private F7 As Object = <x xmlns:p3="...">
                               <%= p2 %>
                               <%= p3 %>
                               <%= xmlns %>
                           </x>
    Private Function F8(p1 As Object) As Object
        Return p1
    End Function
    Private Function F9(xmlns As Object) As Object
        Return p2
    End Function
    Private F10 As Object = p4
End Module
    ]]></file>
    <file name="b.vb"><![CDATA[
Class p4
End Class
    ]]></file>
</compilation>, references:=XmlReferences, options:=options)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30114: 'p1' is an XML prefix and cannot be used as an expression.  Use the GetXmlNamespace operator to create a namespace object.
    Private F1 As Object = p1
                           ~~
BC30451: 'P1' is not declared. It may be inaccessible due to its protection level.
    Private F2 As Object = P1
                           ~~
BC30112: 'System.Xml' is a namespace and cannot be used as an expression.
    Private F3 As Object = xml
                           ~~~
BC30112: 'System.Xml' is a namespace and cannot be used as an expression.
    Private F4 As Object = XML
                           ~~~
BC30114: 'xmlns' is an XML prefix and cannot be used as an expression.  Use the GetXmlNamespace operator to create a namespace object.
    Private F5 As Object = xmlns
                           ~~~~~
BC30451: 'XMLNS' is not declared. It may be inaccessible due to its protection level.
    Private F6 As Object = XMLNS
                           ~~~~~
BC30114: 'p2' is an XML prefix and cannot be used as an expression.  Use the GetXmlNamespace operator to create a namespace object.
                               <%= p2 %>
                                   ~~
BC30451: 'p3' is not declared. It may be inaccessible due to its protection level.
                               <%= p3 %>
                                   ~~
BC30114: 'xmlns' is an XML prefix and cannot be used as an expression.  Use the GetXmlNamespace operator to create a namespace object.
                               <%= xmlns %>
                                   ~~~~~
BC30114: 'p2' is an XML prefix and cannot be used as an expression.  Use the GetXmlNamespace operator to create a namespace object.
        Return p2
               ~~
BC30109: 'p4' is a class type and cannot be used as an expression.
    Private F10 As Object = p4
                            ~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC30131ERR_ModuleSecurityAttributeNotAllowed1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="BC30131ERR_ModuleSecurityAttributeNotAllowed1">
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Security.Permissions
Imports System.Security.Principal

<Module: MySecurity(Security.Permissions.SecurityAction.Assert)>

<AttributeUsage(AttributeTargets.Module)>
Class MySecurityAttribute
    Inherits SecurityAttribute

    Public Sub New(action As SecurityAction)
        MyBase.New(action)
    End Sub

    Public Overrides Function CreatePermission() As Security.IPermission
        Return Nothing
    End Function
End Class

Module Foo
    Public Sub main()

    End Sub
End Module
]]>
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC36979: Security attribute 'MySecurityAttribute' is not valid on this declaration type. Security attributes are only valid on assembly, type and method declarations.
<Module: MySecurity(Security.Permissions.SecurityAction.Assert)>
         ~~~~~~~~~~
]]>
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30132ERR_LabelNotDefined1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LabelNotDefined1">
    <file name="a.vb">
        Module Implicitmod
            Sub Implicit()
                'COMPILEERROR: BC30132, "ns1"
                GoTo ns1
            End Sub
            Sub Test()
        ns1:
            End Sub
        End Module
        Namespace NS2
            Module Implicitmod
                Sub Implicit()
        ns1:
                End Sub
            End Module
        End Namespace
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30132: Label 'ns1' is not defined.
                GoTo ns1
                     ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30148ERR_RequiredNewCall2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="RequiredNewCall2">
    <file name="a.vb">
        Imports System
        Module Test
            Class clsTest0
                Sub New(ByVal strTest As String)
                End Sub
            End Class
            Class clsTest1
                Inherits clsTest0
                Private strTest As String = "Hello"
                Sub New(ByVal ArgX As String)
                    'COMPILEERROR: BC30148, "Console.WriteLine(ArgX)"
                    Console.WriteLine(ArgX)
                End Sub
            End Class
        End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30148: First statement of this 'Sub New' must be a call to 'MyBase.New' or 'MyClass.New' because base class 'Test.clsTest0' of 'Test.clsTest1' does not have an accessible 'Sub New' that can be called with no arguments.
                Sub New(ByVal ArgX As String)
                    ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30157ERR_BadWithRef()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System        
Module M1
    Sub Main()
        .xxx = 3
    End Sub
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30157: Leading '.' or '!' can only appear inside a 'With' statement.
        .xxx = 3
        ~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30157ERR_BadWithRef_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Default Property P(x As String)
        Get
            Return Nothing
        End Get
        Set(value)
        End Set
    End Property
    Sub M()
        !A = Me!B
        Me!A = !B
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30157: Leading '.' or '!' can only appear inside a 'With' statement.
        !A = Me!B
        ~~
BC30157: Leading '.' or '!' can only appear inside a 'With' statement.
        Me!A = !B
               ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30157ERR_BadWithRef_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Module M
    Sub M()
        Dim o As Object
        o = .<x>
        o = ...<x>
        .@a = .@<a>
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30157: Leading '.' or '!' can only appear inside a 'With' statement.
        o = .<x>
            ~~~~
BC30157: Leading '.' or '!' can only appear inside a 'With' statement.
        o = ...<x>
            ~~~~~~
BC30157: Leading '.' or '!' can only appear inside a 'With' statement.
        .@a = .@<a>
        ~~~
BC30157: Leading '.' or '!' can only appear inside a 'With' statement.
        .@a = .@<a>
              ~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC30182_ERR_UnrecognizedType_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UnrecognizedType">
        <file name="a.vb">
        Namespace NS
            Class C1
                Sub FOO()
                    Dim v = 1
                    Dim s = CType(v, NS)
                End Sub
            End Class
        End Namespace
        </file>
    </compilation>)

            Dim expectedErrors1 = <errors>
BC30182: Type expected.
                    Dim s = CType(v, NS)
                                     ~~
</errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30203ERR_ExpectedIdentifier()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BC30203ERR_ExpectedIdentifier">
        <file name="a.vb">
        Option Strict On
        Class C1
            Public Property 
            Public Property _ as Integer
            Shared Public
        End Class
                </file>
    </compilation>)
            Dim expectedErrors1 =
<errors>
BC30203: Identifier expected.
            Public Property 
                            ~
BC30301: 'Public Property  As Object' and 'Public Property  As Integer' cannot overload each other because they differ only by return types.
            Public Property 
                            ~
BC30203: Identifier expected.
            Public Property _ as Integer
                            ~
BC30203: Identifier expected.
            Shared Public
                         ~
</errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30209ERR_StrictDisallowImplicitObject()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="StrictDisallowImplicitObject">
        <file name="a.vb">
        Option Strict On
        Structure myStruct
            Dim s
        End Structure
                </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
        BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
            Dim s
                ~
     </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30209ERR_StrictDisallowImplicitObject_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="StrictDisallowImplicitObject">
        <file name="a.vb">
        Option Strict On
        Structure myStruct
            Sub Scen1()
                'COMPILEERROR: BC30209, "i"
                Dim i
            End Sub
        End Structure
                </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
        BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
                Dim i
                    ~
BC42024: Unused local variable: 'i'.
                Dim i
                    ~
     </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(528749, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528749")>
        Public Sub BC30209ERR_StrictDisallowImplicitObject_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="StrictDisallowImplicitObject">
        <file name="a.vb">
Option strict on
option infer off

imports system

        Class C1
            Public Const f1 = "foo"
            Public Const f2 As Object = "foo"
            Public Const f3 = 23
            Public Const f4 As Object = 42

            Public Shared Sub Main(args() As String)
                console.writeline(f1)
                console.writeline(f2)
                console.writeline(f3)
                console.writeline(f4)
            End Sub
        End Class
                </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
            Public Const f1 = "foo"
                         ~~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
            Public Const f3 = 23
                         ~~
     </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30239ERR_ExpectedRelational_SelectCase()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="StrictDisallowImplicitObject">
        <file name="a.vb">
            <file name="a.vb"><![CDATA[
Imports System        
Module M1
    Sub Main()
        Select Case 0
            Case Is << 1
        End Select
    End Sub
End Module
        ]]></file>
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30239: Relational operator expected.
            Case Is << 1
                    ~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub BC30272ERR_NamedParamNotFound2()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="NamedParamNotFound2">
    <file name="a.vb">
        Module Module1
            Class C0(Of T)
                Public whichOne As String
                Sub Foo(ByVal t1 As T)
                    whichOne = "T"
                End Sub
            End Class
            Class C1(Of T, Y)
                Inherits C0(Of T)
                Overloads Sub Foo(ByVal y1 As Y)
                    whichOne = "Y"
                End Sub
            End Class
            Sub GenUnif0060()
                Dim tc1 As New C1(Of Integer, Integer)
                ' BC30272: 't1' is not a parameter of 'Public Overloads Sub Foo(y1 As Y)'.
                Call tc1.Foo(t1:=1000)
            End Sub
        End Module
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_NamedParamNotFound2, "t1").WithArguments("t1", "Public Overloads Sub Foo(y1 As Integer)"),
    Diagnostic(ERRID.ERR_OmittedArgument2, "Foo").WithArguments("y1", "Public Overloads Sub Foo(y1 As Integer)"))

        End Sub

        <Fact()>
        Public Sub BC30274ERR_NamedArgUsedTwice2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="NamedArgUsedTwice2">
    <file name="a.vb">
        Module Module1
            Class C0
                Public whichOne As String
                Sub Foo(ByVal t1 As String)
                    whichOne = "T"
                End Sub
            End Class
            Class C1
                Inherits C0
                Overloads Sub Foo(ByVal y1 As String)
                    whichOne = "Y"
                End Sub
            End Class
            Sub test()
                Dim [ident1] As C0 = New C0()
                Dim clsNarg2get As C1 = New C1()
                Dim str1 As String = "Visual Basic"
                'COMPILEERROR: BC30274, "y"
                [ident1].Foo(1, t1:=2) = str1
                'COMPILEERROR: BC30274, "x"
                [ident1].Foo(t1:=1, t1:=1) = str1
            End Sub
        End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30274: Parameter 't1' of 'Public Sub Foo(t1 As String)' already has a matching argument.
                [ident1].Foo(1, t1:=2) = str1
                                ~~
BC30274: Parameter 't1' of 'Public Sub Foo(t1 As String)' already has a matching argument.
                [ident1].Foo(t1:=1, t1:=1) = str1
                                    ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30277ERR_TypecharNoMatch2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="TypecharNoMatch2">
    <file name="a.vb">
        Imports Microsoft.VisualBasic.Information
        Namespace NS30277
            Public Class genClass1(Of CT)
                Public Function genFun7(Of T)(ByVal x As T) As T()
                    Dim t1(2) As T
                    Return t1
                End Function
            End Class
            Module MD30277
                Sub GenMethod9102()
                    Const uiConst As UInteger = 1000
                    Dim o As New genClass1(Of Object)
                    Dim objTmp As Object = CShort(10)
                    ' BC30277: type character does not match declared data type.
                    o.genFun7%(1&amp;)
                    ' BC30277: type character does not match declared data type.
                    o.genFun7%(True)
                    ' BC30277: type character does not match declared data type.
                    o.genFun7%((True And False))
                    ' BC30277: type character does not match declared data type.
                    o.genFun7%(CDbl(1))
                    ' BC30277: type character does not match declared data type.
                    o.genFun7%(Fun1)
                    ' BC30277: type character does not match declared data type.
                    o.genFun7%(TypeName(o))
                    ' BC30277: type character does not match declared data type.
                    o.genFun7%(uiConst)
                    ' BC30277: type character does not match declared data type.
                    o.genFun7%(objTmp)
                End Sub
                Function Fun1() As Byte
                    Return 1
                End Function
            End Module
        End Namespace
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30277: Type character '%' does not match declared data type 'Long'.
                    o.genFun7%(1&amp;)
                    ~~~~~~~~~~~~~~
BC30277: Type character '%' does not match declared data type 'Boolean'.
                    o.genFun7%(True)
                    ~~~~~~~~~~~~~~~~
BC30277: Type character '%' does not match declared data type 'Boolean'.
                    o.genFun7%((True And False))
                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30277: Type character '%' does not match declared data type 'Double'.
                    o.genFun7%(CDbl(1))
                    ~~~~~~~~~~~~~~~~~~~
BC30277: Type character '%' does not match declared data type 'Byte'.
                    o.genFun7%(Fun1)
                    ~~~~~~~~~~~~~~~~
BC30277: Type character '%' does not match declared data type 'String'.
                    o.genFun7%(TypeName(o))
                    ~~~~~~~~~~~~~~~~~~~~~~~
BC30277: Type character '%' does not match declared data type 'UInteger'.
                    o.genFun7%(uiConst)
                    ~~~~~~~~~~~~~~~~~~~
BC30277: Type character '%' does not match declared data type 'Object'.
                    o.genFun7%(objTmp)
                    ~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30277ERR_TypecharNoMatch2_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="TypecharNoMatch2">
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        'declare with explicit type, use in next with a type char")
        For Each x As Integer In New Integer() {1, 1, 1}
            'COMPILEERROR: BC30277, "x#"
        Next x#
        For Each [me] As Integer In New Integer() {1, 1, 1}
        Next me%
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30277: Type character '#' does not match declared data type 'Integer'.
        Next x#
             ~~
</expected>)
        End Sub

        <WorkItem(528681, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528681")>
        <Fact()>
        Public Sub BC30277ERR_TypecharNoMatch2_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="TypecharNoMatch2">
    <file name="a.vb">
Public Class MyClass1
    Public Shared Sub Main()
        For ivar% As Long = 1 To 10
        Next
        For dvar# As Single = 1 To 10
        Next
        For cvar@ As Decimal = 1 To 10
        Next
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30302: Type character '%' cannot be used in a declaration with an explicit type.
        For ivar% As Long = 1 To 10
            ~~~~~
BC30302: Type character '#' cannot be used in a declaration with an explicit type.
        For dvar# As Single = 1 To 10
            ~~~~~
BC30302: Type character '@' cannot be used in a declaration with an explicit type.
        For cvar@ As Decimal = 1 To 10
            ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30282ERR_InvalidConstructorCall1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="InvalidConstructorCall1">
    <file name="a.vb">
        Module Error30282
            Class Class1
                Sub New()
                End Sub
            End Class
            Class Class2
                Inherits Class1
                Sub New()
                    'COMPILEERROR: BC30282, "Class1.New"
                    Class1.New()
                End Sub
            End Class
        End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30282: Constructor call is valid only as the first statement in an instance constructor.
                    Class1.New()
                    ~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30282ERR_InvalidConstructorCall2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="InvalidConstructorCall2">
    <file name="a.vb">
Imports System
Class C
    Sub New(x As Integer)
        Me.New(Of Integer)
    End Sub

    Sub New
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30282: Constructor call is valid only as the first statement in an instance constructor.
        Me.New(Of Integer)
        ~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30282ERR_InvalidConstructorCall3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="InvalidConstructorCall3">
    <file name="a.vb">
Imports System
Class C
    Sub New(x As Integer)
        Me.New
        Me.New(Of Integer)
    End Sub

    Sub New
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30282: Constructor call is valid only as the first statement in an instance constructor.
        Me.New(Of Integer)
        ~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30282ERR_InvalidConstructorCall4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="InvalidConstructorCall4">
    <file name="a.vb">
Imports System
Class C
    Sub New(x As Integer)
        Me.New(Of Integer)(1.ToString(123, 2, 3, 4))
    End Sub

    Sub New
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30282: Constructor call is valid only as the first statement in an instance constructor.
        Me.New(Of Integer)(1.ToString(123, 2, 3, 4))
        ~~~~~~~~~~~~~~~~~~
BC30516: Overload resolution failed because no accessible 'ToString' accepts this number of arguments.
        Me.New(Of Integer)(1.ToString(123, 2, 3, 4))
                             ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30282ERR_InvalidConstructorCall5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="InvalidConstructorCall5">
    <file name="a.vb">
Imports System
Class C
    Sub New(x As Integer)
        Dim a = 1 + Me.New(Of Integer)
    End Sub

    Sub New
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30282: Constructor call is valid only as the first statement in an instance constructor.
        Dim a = 1 + Me.New(Of Integer)
                    ~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30282ERR_ConstructorCallIsValidOnlyAsTheFirstStatementInAnInstanceConstructor_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30282ERR_ConstructorCallIsValidOnlyAsTheFirstStatementInAnInstanceConstructor_1">
    <file name="a.vb">
Class Tests
    Public Sub New()
    End Sub
    Public Sub New(i As Integer)
        Me.New()
    End Sub
    Public Sub New(t As Tests)
        t.New(1)
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30282: Constructor call is valid only as the first statement in an instance constructor.
        t.New(1)
        ~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub BC30282ERR_ConstructorCallIsValidOnlyAsTheFirstStatementInAnInstanceConstructor_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30282ERR_ConstructorCallIsValidOnlyAsTheFirstStatementInAnInstanceConstructor_2">
    <file name="a.vb">
Class Tests
    Public Sub New()
    End Sub
    Public Sub New(i As Integer)
        Me.New()
        Me.New()
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30282: Constructor call is valid only as the first statement in an instance constructor.
        Me.New()
        ~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub BC30282ERR_ConstructorCallIsValidOnlyAsTheFirstStatementInAnInstanceConstructor_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30282ERR_ConstructorCallIsValidOnlyAsTheFirstStatementInAnInstanceConstructor_3">
    <file name="a.vb">
Class Tests
    Public Sub New()
    End Sub
    Public Sub New(i As Integer)
l1:
        Me.New()
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30282: Constructor call is valid only as the first statement in an instance constructor.
        Me.New()
        ~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub BC30282ERR_ConstructorCallIsValidOnlyAsTheFirstStatementInAnInstanceConstructor_4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30282ERR_ConstructorCallIsValidOnlyAsTheFirstStatementInAnInstanceConstructor_4">
    <file name="a.vb">
Class Tests
    Public Sub New()
    End Sub
    Public Sub New2()
        Me.New()
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30282: Constructor call is valid only as the first statement in an instance constructor.
        Me.New()
        ~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub BC30282ERR_ConstructorCallIsValidOnlyAsTheFirstStatementInAnInstanceConstructor_5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30282ERR_ConstructorCallIsValidOnlyAsTheFirstStatementInAnInstanceConstructor_5">
    <file name="a.vb">
Class Tests
    Public Sub New()
    End Sub
    Public Sub New(i As Integer)
#Const a = 1
#If a = 1 Then
        Me.New()
#End If
        Me.New()
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30282: Constructor call is valid only as the first statement in an instance constructor.
        Me.New()
        ~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub BC30282ERR_ConstructorCallIsValidOnlyAsTheFirstStatementInAnInstanceConstructor_6()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30282ERR_ConstructorCallIsValidOnlyAsTheFirstStatementInAnInstanceConstructor_6">
    <file name="a.vb">
Class Tests
    Public Sub New()
    End Sub
    Public Sub New(i As Integer)
        Dim a As Integer = 1 + Me.New() + Me.New
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30282: Constructor call is valid only as the first statement in an instance constructor.
        Dim a As Integer = 1 + Me.New() + Me.New
                               ~~~~~~
BC30282: Constructor call is valid only as the first statement in an instance constructor.
        Dim a As Integer = 1 + Me.New() + Me.New
                                          ~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub BC30282ERR_ConstructorCallIsValidOnlyAsTheFirstStatementInAnInstanceConstructor_7()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30282ERR_ConstructorCallIsValidOnlyAsTheFirstStatementInAnInstanceConstructor_7">
    <file name="a.vb">
Class Tests
    Public Sub New(i As String)
    End Sub
    Public Sub New(i As Integer)
        Dim a As Integer = 1 + Me.New(1, 2) + Me.New
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30282: Constructor call is valid only as the first statement in an instance constructor.
        Dim a As Integer = 1 + Me.New(1, 2) + Me.New
                               ~~~~~~
BC30282: Constructor call is valid only as the first statement in an instance constructor.
        Dim a As Integer = 1 + Me.New(1, 2) + Me.New
                                              ~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub BC30282ERR_ConstructorCallIsValidOnlyAsTheFirstStatementInAnInstanceConstructor_8()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30282ERR_ConstructorCallIsValidOnlyAsTheFirstStatementInAnInstanceConstructor_8">
    <file name="a.vb">
Class Tests
    Public Sub New(i As String)
    End Sub
    Public Sub New(i As Integer)
        Tests.New(1, 2)
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30282: Constructor call is valid only as the first statement in an instance constructor.
        Tests.New(1, 2)
        ~~~~~~~~~
</errors>)
        End Sub

        <WorkItem(541012, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541012")>
        <Fact()>
        Public Sub BC30283ERR_CantOverrideConstructor()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CantOverrideConstructor">
    <file name="a.vb">
        Module Error30283
            Class Class1
                mustoverride Sub New()
                End Sub
            End Class
        End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
                                               <expected>
BC30364: 'Sub New' cannot be declared 'mustoverride'.
                mustoverride Sub New()
                ~~~~~~~~~~~~
BC30429: 'End Sub' must be preceded by a matching 'Sub'.
                End Sub
                ~~~~~~~                                                   
                                               </expected>)
        End Sub

        <Fact()>
        Public Sub BC30288ERR_DuplicateLocals1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateLocals1">
        <file name="a.vb">
        Public Class Class1
            Public Sub foo(ByVal val As Short)
                Dim i As Integer
                Dim i As String
            End Sub
        End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42024: Unused local variable: 'i'.
                Dim i As Integer
                    ~
BC30288: Local variable 'i' is already declared in the current block.
                Dim i As String
                    ~
BC42024: Unused local variable: 'i'.
                Dim i As String
                    ~
     </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(531346, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531346")>
        Public Sub UnicodeCaseInsensitiveLocals()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UnicodeCaseInsensitiveLocals">
        <file name="a.vb">
        Public Class Class1
            Public Sub foo()
                Dim X&#x130;
                'COMPILEERROR:BC30288, "xi"
                Dim xi

                Dim &#x130;
                'COMPILEERROR:BC30288, "i"
                Dim i
            End Sub
        End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42024: Unused local variable: 'X&#x130;'.
                Dim X&#x130;
                    ~~
BC30288: Local variable 'xi' is already declared in the current block.
                Dim xi
                    ~~
BC42024: Unused local variable: 'xi'.
                Dim xi
                    ~~
BC42024: Unused local variable: '&#x130;'.
                Dim &#x130;
                    ~
BC30288: Local variable 'i' is already declared in the current block.
                Dim i
                    ~
BC42024: Unused local variable: 'i'.
                Dim i
                    ~
     </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30290ERR_LocalSameAsFunc()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LocalSameAsFunc">
    <file name="a.vb">
        Module Error30290
            Class Class1
                Function Foo(ByVal Name As String)
                    'COMPILEERROR : BC30290, "Foo" 
                    Dim Foo As Date
                    Return Name
                End Function
            End Class
        End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30290: Local variable cannot have the same name as the function containing it.
                    Dim Foo As Date
                        ~~~
BC42024: Unused local variable: 'Foo'.
                    Dim Foo As Date
                        ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30290ERR_LocalSameAsFunc_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="LocalSameAsFunc">
    <file name="a.vb">
Class C
    Shared Sub Main()
    End Sub
    Function foo() as Object
        'COMPILEERROR: BC30290, 
        For Each foo As Integer In New Integer() {1, 2, 3}
        Next
        return nothing
    End Function
    Sub foo1()
        For Each foo1 As Integer In New Integer() {1, 2, 3}
        Next
    End SUB
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30290: Local variable cannot have the same name as the function containing it.
        For Each foo As Integer In New Integer() {1, 2, 3}
                 ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30297ERR_ConstructorCannotCallItself_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30297ERR_ConstructorCannotCallItself_1">
    <file name="a.vb">
Imports System
Class Tests
    Public Sub New(i As Integer)
        Me.New("")
    End Sub
    Public Sub New(i As String)
        Me.New(1)
    End Sub
    Public Sub New(i As DateTime)
        Me.New(1)
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30298: Constructor 'Public Sub New(i As Integer)' cannot call itself: 
    'Public Sub New(i As Integer)' calls 'Public Sub New(i As String)'.
    'Public Sub New(i As String)' calls 'Public Sub New(i As Integer)'.
    Public Sub New(i As Integer)
               ~~~
BC30298: Constructor 'Public Sub New(i As String)' cannot call itself: 
    'Public Sub New(i As String)' calls 'Public Sub New(i As Integer)'.
    'Public Sub New(i As Integer)' calls 'Public Sub New(i As String)'.
    Public Sub New(i As String)
               ~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub BC30297ERR_ConstructorCannotCallItself_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30297ERR_ConstructorCannotCallItself_2">
    <file name="a.vb">
Imports System
Class Tests
    Public Sub New(i As Byte)
        Me.New(CType(i, Int16))
    End Sub
    Public Sub New(i As Int16)
        Me.New(DateTime.Now)
    End Sub
    Public Sub New(i As DateTime)
        Me.New(DateTime.Now)
    End Sub
    Public Sub New(i As Int64)
        Me.New("")
    End Sub
    Public Sub New(i As Int32)
        Me.New(ctype(1, UInt32))
    End Sub
    Public Sub New(i As UInt32)
        Me.New("")
    End Sub
    Public Sub New(i As String)
        Me.New(cint(1))
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30298: Constructor 'Public Sub New(i As Date)' cannot call itself: 
    'Public Sub New(i As Date)' calls 'Public Sub New(i As Date)'.
    Public Sub New(i As DateTime)
               ~~~
BC30298: Constructor 'Public Sub New(i As Integer)' cannot call itself: 
    'Public Sub New(i As Integer)' calls 'Public Sub New(i As UInteger)'.
    'Public Sub New(i As UInteger)' calls 'Public Sub New(i As String)'.
    'Public Sub New(i As String)' calls 'Public Sub New(i As Integer)'.
    Public Sub New(i As Int32)
               ~~~
BC30298: Constructor 'Public Sub New(i As UInteger)' cannot call itself: 
    'Public Sub New(i As UInteger)' calls 'Public Sub New(i As String)'.
    'Public Sub New(i As String)' calls 'Public Sub New(i As Integer)'.
    'Public Sub New(i As Integer)' calls 'Public Sub New(i As UInteger)'.
    Public Sub New(i As UInt32)
               ~~~
BC30298: Constructor 'Public Sub New(i As String)' cannot call itself: 
    'Public Sub New(i As String)' calls 'Public Sub New(i As Integer)'.
    'Public Sub New(i As Integer)' calls 'Public Sub New(i As UInteger)'.
    'Public Sub New(i As UInteger)' calls 'Public Sub New(i As String)'.
    Public Sub New(i As String)
               ~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub BC30297ERR_ConstructorCannotCallItself_3()
            ' NOTE: Test case ensures that the error in calling the 
            '       constructor suppresses the cycle detection
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC30297ERR_ConstructorCannotCallItself_3">
    <file name="a.vb">
Imports System
Class Tests
    Public Sub New(i As Integer)
        Me.New("")
    End Sub
    Public Sub New(i As String)
        Me.New(qqq)
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30451: 'qqq' is not declared. It may be inaccessible due to its protection level.
        Me.New(qqq)
               ~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub BC30306ERR_MissingSubscript()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="MissingSubscript">
    <file name="a.vb">
        Module Mod30306
            Sub ArExtFrErr002()
                Dim scen1(,) As Integer
                ReDim scen1(2, 2)
                Dim scen4() As Integer
                'COMPILEERROR: BC30306, "(", BC30306, ","
                ReDim scen4(, )
                Dim scen8a(,,) As Integer
                'COMPILEERROR: BC30306, "(", BC30306, ",", BC30306, ","
                ReDim scen8a(, , )
                Dim Scen8b(,,) As Integer
                'COMPILEERROR: BC30306, ",", BC30306, ","
                ReDim Scen8b(5, , )
                Dim scen8c(,,) As Integer
                'COMPILEERROR: BC30306, "(", BC30306, ","
                ReDim scen8c(, , 5)
                Exit Sub
            End Sub
        End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30306: Array subscript expression missing.
                ReDim scen4(, )
                            ~
BC30306: Array subscript expression missing.
                ReDim scen4(, )
                              ~
BC30306: Array subscript expression missing.
                ReDim scen8a(, , )
                             ~
BC30306: Array subscript expression missing.
                ReDim scen8a(, , )
                               ~
BC30306: Array subscript expression missing.
                ReDim scen8a(, , )
                                 ~
BC30306: Array subscript expression missing.
                ReDim Scen8b(5, , )
                                ~
BC30306: Array subscript expression missing.
                ReDim Scen8b(5, , )
                                  ~
BC30306: Array subscript expression missing.
                ReDim scen8c(, , 5)
                             ~
BC30306: Array subscript expression missing.
                ReDim scen8c(, , 5)
                               ~
</expected>)
        End Sub

        '        <Fact()>
        '        Public Sub BC30310ERR_FieldOfValueFieldOfMarshalByRef3()
        '            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
        '<compilation name="FieldOfValueFieldOfMarshalByRef3">
        '    <file name="a.vb">

        '    </file>
        '</compilation>)
        '            CompilationUtils.AssertTheseErrors(compilation,
        '<expected>
        'BC30310: Local variable cannot have the same name as the function containing it.
        '           Dim Foo As Date
        '            ~~~~
        'BC30310: Local variable cannot have the same name as the function containing it.
        '           Dim scen16 = 3
        '            ~~~~
        '</expected>)
        '        End Sub

        <Fact()>
        Public Sub BC30311ERR_TypeMismatch2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
        Imports System
        Public Class C
            Sub FOO()
                Dim a As S1?
                Dim b As E1?
                Dim c As System.Exception
                Dim d As I1
                Dim e As C1.C2
                Dim f As C1.C2(,)
                Dim z = DirectCast (b, S1?)
                z = DirectCast (b, System.Nullable)
                z = DirectCast (a, E1?)
                z = DirectCast (a, System.Nullable)
                z = DirectCast (c, S1?)
                z = DirectCast (c, E1?)
                z = DirectCast (c, System.Nullable)
                z = DirectCast (d, S1?)
                z = DirectCast (d, E1?)
                z = DirectCast (d, System.Nullable)
                z = DirectCast (d, System.ValueType)
                z = DirectCast (e, S1?)
                z = DirectCast (e, E1?)
                z = DirectCast (e, System.Nullable)
                z = DirectCast (e, System.ValueType)
                z = DirectCast (f, S1?)
                z = DirectCast (f, E1?)
                z = DirectCast (f, System.Nullable)
                z = DirectCast (f, System.ValueType)
            End Sub
        End Class
        Structure S1
        End Structure
        Enum E1
            one
        End Enum
        Interface I1
        End Interface
        Class C1
            Public Class C2
            End Class
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'E1?' cannot be converted to 'S1?'.
                Dim z = DirectCast (b, S1?)
                                    ~
BC30311: Value of type 'E1?' cannot be converted to 'Nullable'.
                z = DirectCast (b, System.Nullable)
                                ~
BC30311: Value of type 'S1?' cannot be converted to 'E1?'.
                z = DirectCast (a, E1?)
                                ~
BC30311: Value of type 'S1?' cannot be converted to 'Nullable'.
                z = DirectCast (a, System.Nullable)
                                ~
BC30311: Value of type 'Exception' cannot be converted to 'S1?'.
                z = DirectCast (c, S1?)
                                ~
BC42104: Variable 'c' is used before it has been assigned a value. A null reference exception could result at runtime.
                z = DirectCast (c, S1?)
                                ~
BC30311: Value of type 'Exception' cannot be converted to 'E1?'.
                z = DirectCast (c, E1?)
                                ~
BC30311: Value of type 'Exception' cannot be converted to 'Nullable'.
                z = DirectCast (c, System.Nullable)
                                ~
BC30311: Value of type 'I1' cannot be converted to 'S1?'.
                z = DirectCast (d, S1?)
                                ~
BC42104: Variable 'd' is used before it has been assigned a value. A null reference exception could result at runtime.
                z = DirectCast (d, S1?)
                                ~
BC30311: Value of type 'I1' cannot be converted to 'E1?'.
                z = DirectCast (d, E1?)
                                ~
BC30311: Value of type 'Nullable' cannot be converted to 'S1?'.
                z = DirectCast (d, System.Nullable)
                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42322: Runtime errors might occur when converting 'I1' to 'Nullable'.
                z = DirectCast (d, System.Nullable)
                                ~
BC30311: Value of type 'C1.C2' cannot be converted to 'S1?'.
                z = DirectCast (e, S1?)
                                ~
BC42104: Variable 'e' is used before it has been assigned a value. A null reference exception could result at runtime.
                z = DirectCast (e, S1?)
                                ~
BC30311: Value of type 'C1.C2' cannot be converted to 'E1?'.
                z = DirectCast (e, E1?)
                                ~
BC30311: Value of type 'C1.C2' cannot be converted to 'Nullable'.
                z = DirectCast (e, System.Nullable)
                                ~
BC30311: Value of type 'C1.C2' cannot be converted to 'ValueType'.
                z = DirectCast (e, System.ValueType)
                                ~
BC30311: Value of type 'C1.C2(*,*)' cannot be converted to 'S1?'.
                z = DirectCast (f, S1?)
                                ~
BC42104: Variable 'f' is used before it has been assigned a value. A null reference exception could result at runtime.
                z = DirectCast (f, S1?)
                                ~
BC30311: Value of type 'C1.C2(*,*)' cannot be converted to 'E1?'.
                z = DirectCast (f, E1?)
                                ~
BC30311: Value of type 'C1.C2(*,*)' cannot be converted to 'Nullable'.
                z = DirectCast (f, System.Nullable)
                                ~
BC30311: Value of type 'C1.C2(*,*)' cannot be converted to 'ValueType'.
                z = DirectCast (f, System.ValueType)
                                ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30311ERR_TypeMismatch2_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System

Class C
    Shared Sub Main()
        For Each x As Integer In New Exception() {Nothing, Nothing}
        Next
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Exception' cannot be converted to 'Integer'.
        For Each x As Integer In New Exception() {Nothing, Nothing}
                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30311ERR_TypeMismatch2_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        Dim numbers2D As Integer()() = New Integer()() {New Integer() {1, 2}, New Integer() {1, 2}}
        For Each i As Integer In numbers2D
            System.Console.Write("{0} ", i)
        Next
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Integer()' cannot be converted to 'Integer'.
        For Each i As Integer In numbers2D
                                 ~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30311ERR_TypeMismatch2_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
    End Sub
    Private Function fun(Of T)(Parm1 As T) As T
        Dim temp As T
        Return If(temp, temp, 1)
    End Function
End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'T' cannot be converted to 'Boolean'.
        Return If(temp, temp, 1)
                  ~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30311ERR_TypeMismatch2_4()
            CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Public Class Test

    Public Sub Test()
        Dim at1 = New With {.f1 = Nothing, .f2 = String.Empty}
        Dim at2 = New With {.f2 = String.Empty, .f1 = Nothing}
        at1 = at2
    End Sub

End Class
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_TypeMismatch2, "at2").WithArguments("<anonymous type: f2 As String, f1 As Object>", "<anonymous type: f1 As Object, f2 As String>"))

        End Sub

        <Fact()>
        Public Sub BC30311ERR_TypeMismatch2_5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Public Sub Main()
        Dim arr1 As Integer(,) = New Integer(2, 1) {{1, 2}, {3, 4}, {5, 6}}
        arr1 = 0        ' Invalid
    End Sub
End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Integer' cannot be converted to 'Integer(*,*)'.
        arr1 = 0        ' Invalid
               ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30311ERR_TypeMismatch2_6()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module Program
    Public Sub Main()
        Dim arr As Integer(,) = New Integer(2, 1) {{6, 7}, {5, 8}, {8, 10}}
        Dim x As Integer
        x = arr        'Invalid
        arr = x        'Invalid
    End Sub
End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Integer(*,*)' cannot be converted to 'Integer'.
        x = arr        'Invalid
            ~~~
BC30311: Value of type 'Integer' cannot be converted to 'Integer(*,*)'.
        arr = x        'Invalid
              ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30311ERR_TypeMismatch2_7()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Option Strict Off
Option Infer On
Class B
    Public Shared Sub Main()
    End Sub
    Private Sub Foo(Of T)()
        Dim x As T(,) = New T(1, 2) {}
        Dim Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}        ' invalid
    End Sub
End Class

Public Class Class1(Of T)
    Private x As T(,) = New T(1, 2) {}
    Private Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}    ' invalid
    Private Sub Foo()
        Dim x As T(,) = New T(1, 2) {}
        Dim Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}        ' invalid
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Integer' cannot be converted to 'T'.
        Dim Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}        ' invalid
                                      ~
BC30311: Value of type 'Integer' cannot be converted to 'T'.
        Dim Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}        ' invalid
                                         ~
BC30311: Value of type 'Integer' cannot be converted to 'T'.
        Dim Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}        ' invalid
                                            ~
BC30311: Value of type 'Integer' cannot be converted to 'T'.
        Dim Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}        ' invalid
                                                 ~
BC30311: Value of type 'Integer' cannot be converted to 'T'.
        Dim Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}        ' invalid
                                                    ~
BC30311: Value of type 'Integer' cannot be converted to 'T'.
        Dim Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}        ' invalid
                                                       ~
BC30311: Value of type 'Integer' cannot be converted to 'T'.
    Private Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}    ' invalid
                                      ~
BC30311: Value of type 'Integer' cannot be converted to 'T'.
    Private Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}    ' invalid
                                         ~
BC30311: Value of type 'Integer' cannot be converted to 'T'.
    Private Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}    ' invalid
                                            ~
BC30311: Value of type 'Integer' cannot be converted to 'T'.
    Private Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}    ' invalid
                                                 ~
BC30311: Value of type 'Integer' cannot be converted to 'T'.
    Private Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}    ' invalid
                                                    ~
BC30311: Value of type 'Integer' cannot be converted to 'T'.
    Private Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}    ' invalid
                                                       ~
BC30311: Value of type 'Integer' cannot be converted to 'T'.
        Dim Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}        ' invalid
                                      ~
BC30311: Value of type 'Integer' cannot be converted to 'T'.
        Dim Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}        ' invalid
                                         ~
BC30311: Value of type 'Integer' cannot be converted to 'T'.
        Dim Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}        ' invalid
                                            ~
BC30311: Value of type 'Integer' cannot be converted to 'T'.
        Dim Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}        ' invalid
                                                 ~
BC30311: Value of type 'Integer' cannot be converted to 'T'.
        Dim Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}        ' invalid
                                                    ~
BC30311: Value of type 'Integer' cannot be converted to 'T'.
        Dim Y As T(,) = New T(1, 2) {{1, 2, 3}, {1, 2, 3}}        ' invalid
                                                       ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30332ERR_ConvertArrayMismatch4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ConvertArrayMismatch4">
    <file name="a.vb">
        Module M1
            Class Cls2_1
            End Class
            Class Cls2_2
            End Class
            Sub Main()
                Dim Ary1_1() As Integer
                Dim Ary1_2() As Long = New Long() {1, 2}
                'COMPILEERROR: BC30332, "Ary1_2"
                Ary1_1 = CType(Ary1_2, Integer())
                Dim Ary2_1() As Cls2_1 = New Cls2_1() {}
                Dim Ary2_2(,) As Cls2_2
                'COMPILEERROR: BC30332, "Ary2_1"
                Ary2_2 = CType(Ary2_1, Cls2_2())
                Dim Ary3_1(,) As Double = New Double(,) {}
                Dim Ary3_2(,) As Cls2_2
                'COMPILEERROR: BC30332, "Ary3_1"
                Ary3_2 = CType(Ary3_1, Cls2_2(,))
                'COMPILEERROR: BC30332, "Ary3_2"
                Ary3_1 = CType(Ary3_2, Double(,))
                Dim Ary4_1() As Integer
                Dim Ary4_2() As Object = New Object() {}
                Ary4_1 = CType(Ary4_2, Integer())
            End Sub
        End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30332: Value of type 'Long()' cannot be converted to 'Integer()' because 'Long' is not derived from 'Integer'.
                Ary1_1 = CType(Ary1_2, Integer())
                               ~~~~~~
BC30332: Value of type 'M1.Cls2_1()' cannot be converted to 'M1.Cls2_2()' because 'M1.Cls2_1' is not derived from 'M1.Cls2_2'.
                Ary2_2 = CType(Ary2_1, Cls2_2())
                               ~~~~~~
BC30332: Value of type 'Double(*,*)' cannot be converted to 'M1.Cls2_2(*,*)' because 'Double' is not derived from 'M1.Cls2_2'.
                Ary3_2 = CType(Ary3_1, Cls2_2(,))
                               ~~~~~~
BC30332: Value of type 'M1.Cls2_2(*,*)' cannot be converted to 'Double(*,*)' because 'M1.Cls2_2' is not derived from 'Double'.
                Ary3_1 = CType(Ary3_2, Double(,))
                               ~~~~~~
BC30332: Value of type 'Object()' cannot be converted to 'Integer()' because 'Object' is not derived from 'Integer'.
                Ary4_1 = CType(Ary4_2, Integer())
                               ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30332ERR_ConvertArrayMismatch4_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ConvertArrayMismatch4">
    <file name="a.vb">
        Option Strict On
Imports System
Module Module1
    Sub Main()
        Dim arrString$(,) = New Decimal(1, 2) {} ' Invalid
        Dim arrInteger%(,) = New Decimal(1, 2) {} ' Invalid
        Dim arrLong&amp;(,) = New Decimal(1, 2) {} ' Invalid
        Dim arrSingle!(,) = New Decimal(1, 2) {} ' Invalid
    End Sub
End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30332: Value of type 'Decimal(*,*)' cannot be converted to 'String(*,*)' because 'Decimal' is not derived from 'String'.
        Dim arrString$(,) = New Decimal(1, 2) {} ' Invalid
                            ~~~~~~~~~~~~~~~~~~~~
BC30332: Value of type 'Decimal(*,*)' cannot be converted to 'Integer(*,*)' because 'Decimal' is not derived from 'Integer'.
        Dim arrInteger%(,) = New Decimal(1, 2) {} ' Invalid
                             ~~~~~~~~~~~~~~~~~~~~
BC30332: Value of type 'Decimal(*,*)' cannot be converted to 'Long(*,*)' because 'Decimal' is not derived from 'Long'.
        Dim arrLong&amp;(,) = New Decimal(1, 2) {} ' Invalid
                          ~~~~~~~~~~~~~~~~~~~~
BC30332: Value of type 'Decimal(*,*)' cannot be converted to 'Single(*,*)' because 'Decimal' is not derived from 'Single'.
        Dim arrSingle!(,) = New Decimal(1, 2) {} ' Invalid
                            ~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30333ERR_ConvertObjectArrayMismatch3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ConvertObjectArrayMismatch3">
    <file name="a.vb">
       Module M1
            Sub Main()
                Dim Ary1() As Integer = New Integer() {}
                Dim Ary2() As Object
                Ary2 = CType(Ary1, Object())
            End Sub
        End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30333: Value of type 'Integer()' cannot be converted to 'Object()' because 'Integer' is not a reference type.
                Ary2 = CType(Ary1, Object())
                             ~~~~
</expected>)
        End Sub

        <WorkItem(579764, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/579764")>
        <Fact()>
        Public Sub BC30311ERR_WithArray_ParseAndDeclarationErrors()
            'This test is because previously in native command line compiler we would produce errors for both parsing and binding errors,  now 
            ' we won't produce the binding if parsing was not successful from the command line.  However, the diagnostics will display both messages and 
            ' hence the need for two tests to verify this behavior.

            Dim source =
<compilation>
    <file name="ParseErrorOnly.vb">
Module M
	Dim x As Integer() {1, 2, 3}
    	Dim y = CType({1, 2, 3}, System.Collections.Generic.List(Of Integer))

	Sub main
	End Sub
End Module        </file>
</compilation>
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_ExpectedEOS, "{"),
                                                                                                 Diagnostic(ERRID.ERR_TypeMismatch2, "{1, 2, 3}").WithArguments("Integer()", "System.Collections.Generic.List(Of Integer)"))

            ' This 2nd scenario will produce 1 error because it passed the parsing stage and now 
            ' fails in the binding
            source =
<compilation>
    <file name="ParseOK.vb">
Module M
	Dim x As Integer() = {1, 2, 3}
    	Dim y = CType({1, 2, 3}, System.Collections.Generic.List(Of Integer))

	Sub main
	End Sub
End Module        </file>
</compilation>
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_TypeMismatch2, "{1, 2, 3}").WithArguments("Integer()", "System.Collections.Generic.List(Of Integer)"))
        End Sub

        <Fact(), WorkItem(542069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542069")>
        Public Sub BC30337ERR_ForLoopType1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ForLoopType1">
    <file name="a.vb">
       Module M1
            Sub Test()
                'COMPILEERROR : BC30337, "i" 
                For i = New base To New first()
                Next

                For j = New base To New first() step new second()
                Next
            End Sub
        End Module
        Class base
        End Class
        Class first
            Inherits base
            Overloads Shared Widening Operator CType(ByVal d As first) As second
                Return New second()
            End Operator
        End Class
        Class second
            Inherits base
            Overloads Shared Widening Operator CType(ByVal d As second) As first
                Return New first()
            End Operator
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Integer' cannot be converted to 'base'.
                For i = New base To New first()
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'base' must define operator '+' to be used in a 'For' statement.
                For j = New base To New first() step new second()
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'base' must define operator '-' to be used in a 'For' statement.
                For j = New base To New first() step new second()
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'base' must define operator '&lt;=' to be used in a 'For' statement.
                For j = New base To New first() step new second()
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'base' must define operator '>=' to be used in a 'For' statement.
                For j = New base To New first() step new second()
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(542069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542069")>
        Public Sub BC30337ERR_ForLoopType1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ForLoopType1">
    <file name="a.vb">
Option Strict On
Option Infer Off
Module Program
    Sub Main(args As String())
        For x As Date = #1/2/0003# To 10
        Next
    End Sub
End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30337: 'For' loop control variable cannot be of type 'Date' because the type does not support the required operators.
        For x As Date = #1/2/0003# To 10
            ~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(542069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542069"), WorkItem(544464, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544464")>
        Public Sub BC30337ERR_ForLoopType1_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ForLoopType1">
    <file name="a.vb">
Option Strict On
Option Infer Off

Interface IFoo
End Interface

Module Program
    Sub Main(args As String())
        For x As Boolean = False To True
        Next

        Dim foo as Boolean
        For foo = False To True
        Next

        for z as IFoo = nothing to nothing
        next        
    End Sub
End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30337: 'For' loop control variable cannot be of type 'Boolean' because the type does not support the required operators.
        For x As Boolean = False To True
            ~~~~~~~~~~~~
BC30337: 'For' loop control variable cannot be of type 'Boolean' because the type does not support the required operators.
        For foo = False To True
            ~~~
BC30337: 'For' loop control variable cannot be of type 'IFoo' because the type does not support the required operators.
        for z as IFoo = nothing to nothing
            ~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30367ERR_NoDefaultNotExtend1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
        Class C
            Shared Sub M(Of T)(x As C, y As T)
                N(x(0))
                N(x!P)
                x(0)
                N(y(1))
                N(y!Q)
                y(1)
            End Sub
            Shared Sub N(o As Object)
            End Sub
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30367: Class 'C' cannot be indexed because it has no default property.
                N(x(0))
                  ~
BC30367: Class 'C' cannot be indexed because it has no default property.
                N(x!P)
                  ~
BC30454: Expression is not a method.
                x(0)
                ~
BC30547: 'T' cannot be indexed because it has no default property.
                N(y(1))
                  ~
BC30547: 'T' cannot be indexed because it has no default property.
                N(y!Q)
                  ~
BC30454: Expression is not a method.
                y(1)
                ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30369ERR_BadInstanceMemberAccess_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
       Structure S1
            Dim b3 As Integer()
            Public Shared Sub Scenario_6()
                dim b4 = b3
            End Sub
            shared Function foo() As Integer()
                Return b3
            End Function
        End Structure
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
                dim b4 = b3
                         ~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
                Return b3
                       ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30369ERR_BadInstanceMemberAccess_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
        Class C
            Property P
                Get
                    Return Nothing
                End Get
                Set
                End Set
            End Property
            Shared Sub M()
                Dim o = P
                P = o
            End Sub
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
                Dim o = P
                        ~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
                P = o
                ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30369ERR_BadInstanceMemberAccess_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
        Class C
            Shared Sub Main()
                For Each x As Integer In F(x)
                Next
            End Sub
            Private Sub F(x As Integer)
            End Sub
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
                For Each x As Integer In F(x)
                                         ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30369ERR_BadInstanceMemberAccess_4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
        Class C
            Shared Sub Main()
                For Each x As Integer In F(x)
                Next
            End Sub
            Private Function F(x As Integer) As Object
                Return New Object()
            End Function
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
                For Each x As Integer In F(x)
                                         ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30369ERR_BadInstanceMemberAccess_5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Property P1(ByVal x As Integer) As integer
        Get
            Return x +5
        End Get
        Set(ByVal Value As integer)
        End Set
    End Property
    Public Shared Sub Main()
        For Each x As integer In New integer() {P1(x), P1(x), P1(x)}
        Next
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
        For Each x As integer In New integer() {P1(x), P1(x), P1(x)}
                                                ~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
        For Each x As integer In New integer() {P1(x), P1(x), P1(x)}
                                                       ~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
        For Each x As integer In New integer() {P1(x), P1(x), P1(x)}
                                                              ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30369ERR_BadInstanceMemberAccess_6()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Public Shared Sub Main()
        For Each x As Integer In New Integer() {foo(x), foo(x), foo(x)}
        Next
    End Sub
    Function foo(ByRef x As Integer) As Integer
        x = 10
        Return x + 10
    End Function
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
        For Each x As Integer In New Integer() {foo(x), foo(x), foo(x)}
                                                ~~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
        For Each x As Integer In New Integer() {foo(x), foo(x), foo(x)}
                                                        ~~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
        For Each x As Integer In New Integer() {foo(x), foo(x), foo(x)}
                                                                ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30369ERR_BadInstanceMemberAccess_7()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Option Strict Off
Option Infer On
Public Class MyClass1
    Dim global_x As Integer = 10
    Const global_y As Long = 20
    Public Shared Sub Main()
        For global_x = global_y To 10
        Next
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
        For global_x = global_y To 10
            ~~~~~~~~
</expected>)
        End Sub

        <Fact, WorkItem(529193, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529193")>
        Public Sub BC30369ERR_BadInstanceMemberAccess_8()
            CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class derive
    Shared Sub main()
        TestEvents
    End Sub
    Shared Sub TestEvents()
        Dim Obj As New Class1
        RemoveHandler Obj.MyEvent, AddressOf EventHandler
    End Sub
    Function EventHandler()
        Return Nothing
    End Function
    Public Class Class1
        Public Event MyEvent(ByRef x As Decimal)
        Sub CauseSomeEvent()
            RaiseEvent MyEvent(x:=1)
        End Sub
    End Class
End Class
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_BadInstanceMemberAccess, "AddressOf EventHandler"))
        End Sub

        <Fact()>
        Public Sub BC30375ERR_NewIfNullOnNonClass()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="NewIfNullOnNonClass">
    <file name="a.vb">
       Module M1
            Sub Foo()
                Dim interf1 As New Interface1()
                Dim interf2 = New Interface1()
            End Sub
        End Module
        Interface Interface1
        End Interface
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30375: 'New' cannot be used on an interface.
                Dim interf1 As New Interface1()
                               ~~~~~~~~~~~~~~~~
BC30375: 'New' cannot be used on an interface.
                Dim interf2 = New Interface1()
                              ~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        ''' We decided to not implement this for Roslyn as BC30569 and BC31411 cover the scenarios that BC30376 addresses.
        <Fact()>
        Public Sub BC30376ERR_NewIfNullOnAbstractClass1()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="NewIfNullOnAbstractClass1">
    <file name="a.vb">
        Module M1
            Sub Foo()
                Throw (New C1)
            End Sub
        End Module
        MustInherit Class C1
            MustOverride Sub foo()
        End Class
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_NewOnAbstractClass, "New C1"),
    Diagnostic(ERRID.ERR_CantThrowNonException, "Throw (New C1)"))

        End Sub

        <Fact(), WorkItem(999399, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/999399")>
        Public Sub BC30387ERR_NoConstructorOnBase2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="NoConstructorOnBase2">
    <file name="a.vb">
        Module M1
            Class Base
                Sub New(ByVal x As Integer)
                End Sub
            End Class
            Class c1
                Inherits Base
            End Class
        End Module
    </file>
</compilation>)

            Dim expected =
<expected>
BC30387: Class 'M1.c1' must declare a 'Sub New' because its base class 'M1.Base' does not have an accessible 'Sub New' that can be called with no arguments.
            Class c1
                  ~~
</expected>

            CompilationUtils.AssertTheseDiagnostics(compilation, expected)

            CompilationUtils.AssertTheseDiagnostics(compilation.GetDiagnosticsForSyntaxTree(CompilationStage.Compile, compilation.SyntaxTrees.Single()), expected)
        End Sub

        <Fact()>
        Public Sub BC30389ERR_InaccessibleSymbol2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
            Module mod30389
                Sub foo()
                    'COMPILEERROR: BC30389, "Namespace1.Module1.Class1.Struct1"
                    Dim Scen3 As Namespace1.Module1.Class1.Struct1
                    Exit Sub
                End Sub
            End Module
            Namespace Namespace1
                Module Module1
                    Private Class Class1
                        Public Structure Struct1
                            Public Int As Integer
                        End Structure
                    End Class
                End Module
            End Namespace
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42024: Unused local variable: 'Scen3'.
                    Dim Scen3 As Namespace1.Module1.Class1.Struct1
                        ~~~~~
BC30389: 'Namespace1.Module1.Class1' is not accessible in this context because it is 'Private'.
                    Dim Scen3 As Namespace1.Module1.Class1.Struct1
                                 ~~~~~~~~~~~~~~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30389ERR_InaccessibleSymbol2_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
        Class C
            Protected Structure S
            End Structure
            Private F As Integer
            Private Property P As Integer
            Protected Shared ReadOnly Property Q(o)
                Get
                    Return Nothing
                End Get
            End Property
        End Class
        Class D
            Shared Sub M(o)
                Dim x As C = Nothing
                M(New C.S())
                M(x.F)
                M(x.P)
                M(C.Q(Nothing))
            End Sub
        End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30389: 'C.S' is not accessible in this context because it is 'Protected'.
                M(New C.S())
                      ~~~
BC30389: 'C.F' is not accessible in this context because it is 'Private'.
                M(x.F)
                  ~~~
BC30389: 'C.P' is not accessible in this context because it is 'Private'.
                M(x.P)
                  ~~~
BC30389: 'C.Q(o As Object)' is not accessible in this context because it is 'Protected'.
                M(C.Q(Nothing))
                  ~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30390ERR_InaccessibleMember3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
        Class C
            Protected Shared Function F()
                Return Nothing
            End Function
            Private Sub M(o)
            End Sub
        End Class
        Class D
            Shared Sub M(x As C)
                x.M(C.F())
            End Sub
        End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30390: 'C.Private Sub M(o As Object)' is not accessible in this context because it is 'Private'.
                x.M(C.F())
                ~~~
BC30390: 'C.Protected Shared Function F() As Object' is not accessible in this context because it is 'Protected'.
                x.M(C.F())
                    ~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30390ERR_InaccessibleMember3_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
        Class C1
            Public Delegate Sub myDelegate()
            private shared sub mySub()
            end sub
        End Class
        Module M1
            Sub Main()
                Dim d1 As C1.myDelegate
                d1 = New C1.myDelegate(addressof C1.mySub)
                d1 = addressof C1.mysub
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30390: 'C1.Private Shared Sub mySub()' is not accessible in this context because it is 'Private'.
                d1 = New C1.myDelegate(addressof C1.mySub)
                                                 ~~~~~~~~
BC30390: 'C1.Private Shared Sub mySub()' is not accessible in this context because it is 'Private'.
                d1 = addressof C1.mysub
                               ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30390ERR_InaccessibleMember3_2a()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BC30390ERR_InaccessibleMember3_2a">
        <file name="a.vb">
Imports System
Module M1
    Class B
        Private Sub M()
            Console.WriteLine("B.M()")
        End Sub
    End Class

    Class D
        Inherits B

        Public Sub M()
            Console.WriteLine("D.M()")
        End Sub

        Public Sub Test()
            MyBase.M()
            Me.M()
        End Sub
    End Class

    Public Sub Main()
        Call (New D()).Test()
    End Sub
End Module
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30390: 'B.Private Sub M()' is not accessible in this context because it is 'Private'.
            MyBase.M()
            ~~~~~~~~
</expected>)
        End Sub

        <WorkItem(540640, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540640")>
        <Fact()>
        Public Sub BC30390ERR_InaccessibleMember3_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BC30390ERR_InaccessibleMember3_3">
        <file name="a.vb"><![CDATA[
Imports System
Namespace AttrRegress001
    Public Class Attr
        Inherits Attribute

        Public Property PriSet() As Short
            Get
                Return 1
            End Get
            Private Set(ByVal value As Short)
            End Set
        End Property

        Public Property ProSet() As Short
            Get
                Return 2
            End Get
            Protected Set(ByVal value As Short)
            End Set
        End Property

    End Class

    'COMPILEERROR: BC30390, "foo1"
    <Attr(PriSet:=1)> Class Scen2
    End Class

    'COMPILEERROR: BC30390, "foo2"
    <Attr(ProSet:=1)> Class Scen3
    End Class

End Namespace
        ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_InaccessibleMember3, "PriSet").WithArguments("AttrRegress001.Attr", "Public Property PriSet As Short", "Private"),
                                      Diagnostic(ERRID.ERR_InaccessibleMember3, "ProSet").WithArguments("AttrRegress001.Attr", "Public Property ProSet As Short", "Protected"))

        End Sub

        <Fact()>
        Public Sub BC30392ERR_CatchNotException1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CatchNotException1">
        <file name="a.vb">
            Module M1
                Sub Foo()
                    Try
                    Catch o As Object
                        Throw
                    End Try
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30392: 'Catch' cannot catch type 'Object' because it is not 'System.Exception' or a class that inherits from 'System.Exception'.
                    Catch o As Object
                               ~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30393ERR_ExitTryNotWithinTry()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="ExitTryNotWithinTry">
    <file name="a.vb">
     Class S1
         sub FOO()
             if (true)
                 exit try
             End If
         End sub
    End CLASS
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30393: 'Exit Try' can only appear inside a 'Try' statement.
                 exit try
                 ~~~~~~~~
</expected>)
        End Sub

        <WorkItem(542801, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542801")>
        <Fact()>
        Public Sub BC30393ERR_ExitTryNotWithinTry_ExitFromFinally()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="ExitTryNotWithinTry">
    <file name="a.vb">
Imports System
Imports System.Linq
Class BaseClass
    Function Method() As String
        Dim x = New Integer() {}
        x.Where(Function(y)
                    Try
                        Exit Try
                    Catch ex1 As Exception When True
                        Exit Try
                    Finally
                        Exit Try
                    End Try
                    Return y = ""
                End Function)
        Return "x"
    End Function
End Class
Class DerivedClass
    Inherits BaseClass
    Shared Sub Main()
    End Sub
End Class
    </file>
</compilation>, {Net40.References.SystemCore})
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30393: 'Exit Try' can only appear inside a 'Try' statement.
                        Exit Try
                        ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30399ERR_MyBaseAbstractCall1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
    MustInherit Class Base
        MustOverride Sub Pearl()
    End Class
    MustInherit Class C2
        Inherits Base
        Sub foo()
            Call MyBase.Pearl()
        End Sub
    End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30399: 'MyBase' cannot be used with method 'Public MustOverride Sub Pearl()' because it is declared 'MustOverride'.
            Call MyBase.Pearl()
                 ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30399ERR_MyBaseAbstractCall1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
    MustInherit Class A
        MustOverride Property P
    End Class
    Class B
        Inherits A
        Overrides Property P
            Get
                Return Nothing
            End Get
            Set(ByVal value As Object)
            End Set
        End Property
        Sub M()
            MyBase.P = MyBase.P
        End Sub
    End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30399: 'MyBase' cannot be used with method 'Public MustOverride Property P As Object' because it is declared 'MustOverride'.
            MyBase.P = MyBase.P
            ~~~~~~~~
BC30399: 'MyBase' cannot be used with method 'Public MustOverride Property P As Object' because it is declared 'MustOverride'.
            MyBase.P = MyBase.P
                       ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30399ERR_MyBaseAbstractCall1_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
    Imports System
    MustInherit Class Base
        MustOverride Sub Pearl()
    End Class
    MustInherit Class C2
        Inherits Base
        Sub foo()
            Dim _action As Action = AddressOf MyBase.Pearl
        End Sub
    End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30399: 'MyBase' cannot be used with method 'Public MustOverride Sub Pearl()' because it is declared 'MustOverride'.
            Dim _action As Action = AddressOf MyBase.Pearl
                                              ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30399ERR_MyBaseAbstractCall1_4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
    Imports System
    MustInherit Class Base
        MustOverride Function GetBar(i As Integer) As Integer
    End Class
    MustInherit Class C2
        Inherits Base
        Sub foo()
            Dim f As Func(Of Func(Of Integer, String)) = Function() New Func(Of Integer, String)(AddressOf MyBase.GetBar)
        End Sub
    End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30399: 'MyBase' cannot be used with method 'Public MustOverride Function GetBar(i As Integer) As Integer' because it is declared 'MustOverride'.
            Dim f As Func(Of Func(Of Integer, String)) = Function() New Func(Of Integer, String)(AddressOf MyBase.GetBar)
                                                                                                           ~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30399ERR_MyBaseAbstractCall1_5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
    Imports System
    MustInherit Class Base
        MustOverride Function GetBar(i As Integer) As Integer
    End Class
    MustInherit Class C2
        Inherits Base

        Public FLD As Func(Of Func(Of Integer, String)) =
            Function() New Func(Of Integer, String)(AddressOf MyBase.GetBar)

        Public Property PROP As Func(Of Func(Of Integer, String)) =
            Function() New Func(Of Integer, String)(AddressOf MyBase.GetBar)

        Public Overrides Function GetBar(i As Integer) As Integer
            Return Nothing
        End Function
    End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30399: 'MyBase' cannot be used with method 'Public MustOverride Function GetBar(i As Integer) As Integer' because it is declared 'MustOverride'.
            Function() New Func(Of Integer, String)(AddressOf MyBase.GetBar)
                                                              ~~~~~~~~~~~~~
BC30399: 'MyBase' cannot be used with method 'Public MustOverride Function GetBar(i As Integer) As Integer' because it is declared 'MustOverride'.
            Function() New Func(Of Integer, String)(AddressOf MyBase.GetBar)
                                                              ~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30399ERR_MyBaseAbstractCall1_6()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
    Imports System
    Class Base
        Function GetBar(i As Integer) As Integer
            Return Nothing
        End Function
    End Class
    Class C2
        Inherits Base
        Sub foo()
            Dim f As Func(Of Func(Of Integer, String)) = 
                Function() New Func(Of Integer, String)(AddressOf MyBase.GetBar)
        End Sub
    End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact()>
        Public Sub BC30399ERR_MyBaseAbstractCall1_7()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
    Imports System
    Class Base
        Shared Function GetBar(i As Integer) As Integer
            Return 0
        End Function
        Function GetFoo(i As Integer) As Integer
            Return 0
        End Function
    End Class
    MustInherit Class C2
        Inherits Base

        Public FLD As Func(Of Func(Of Integer, String)) =
            Function() New Func(Of Integer, String)(AddressOf MyBase.GetBar)
        Public Property PROP As Func(Of Func(Of Integer, String)) =
            Function() New Func(Of Integer, String)(AddressOf MyBase.GetFoo)

    End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <errors></errors>)
        End Sub

        <Fact>
        Public Sub BC30399ERR_MyBaseAbstractCall1_8()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

MustInherit Class B
    Sub New()
    End Sub

    <DllImport("doo")>
    Shared Function DllImp(i As Integer) As Integer
    End Function

    Declare Function DeclareFtn Lib "foo" (i As Integer) As Integer
End Class

MustInherit Class C
    Inherits B

    Public FLD1 As Func(Of Func(Of Integer, String)) =
        Function() New Func(Of Integer, String)(AddressOf MyBase.DllImp)

    Public FLD2 As Func(Of Func(Of Integer, String)) =
        Function() New Func(Of Integer, String)(AddressOf MyBase.DeclareFtn)
End Class
]]>
    </file>
</compilation>
            CompileAndVerify(source)
        End Sub

        <Fact()>
        Public Sub BC30414ERR_ConvertArrayRankMismatch2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ConvertArrayRankMismatch2">
        <file name="a.vb">
       Module M1
            Sub Main()
                Dim Ary1() As Integer = New Integer() {1}
                Dim Ary2 As Integer(,) = CType(Ary1, Integer(,))
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30414: Value of type 'Integer()' cannot be converted to 'Integer(*,*)' because the array types have different numbers of dimensions.
                Dim Ary2 As Integer(,) = CType(Ary1, Integer(,))
                                               ~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30414ERR_ConvertArrayRankMismatch2_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ConvertArrayRankMismatch2">
        <file name="a.vb">
        Option Strict On
        Imports System
        Module Module1
            Sub Main()
                Dim arr1 As Integer(,,) = New Integer(9, 5) {} ' Invalid
                Dim arr2 As Integer() = New Integer(9, 5) {} ' Invalid
                Dim arr3() As Integer = New Integer(2, 3) {} ' Invalid
                Dim arr4(,) As Integer = New Integer(2, 3, 1) {} ' Invalid
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30414: Value of type 'Integer(*,*)' cannot be converted to 'Integer(*,*,*)' because the array types have different numbers of dimensions.
                Dim arr1 As Integer(,,) = New Integer(9, 5) {} ' Invalid
                                          ~~~~~~~~~~~~~~~~~~~~
BC30414: Value of type 'Integer(*,*)' cannot be converted to 'Integer()' because the array types have different numbers of dimensions.
                Dim arr2 As Integer() = New Integer(9, 5) {} ' Invalid
                                        ~~~~~~~~~~~~~~~~~~~~
BC30414: Value of type 'Integer(*,*)' cannot be converted to 'Integer()' because the array types have different numbers of dimensions.
                Dim arr3() As Integer = New Integer(2, 3) {} ' Invalid
                                        ~~~~~~~~~~~~~~~~~~~~
BC30414: Value of type 'Integer(*,*,*)' cannot be converted to 'Integer(*,*)' because the array types have different numbers of dimensions.
                Dim arr4(,) As Integer = New Integer(2, 3, 1) {} ' Invalid
                                         ~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30414ERR_ConvertArrayRankMismatch2_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ConvertArrayRankMismatch2">
        <file name="a.vb">
        Option Strict On
        Imports System
        Module Module1
            Sub Main()
                Dim myArray10 As Integer(,) = {1, 2}        ' Invalid
                Dim myArray11 As Integer(,) = {{{1, 2}}}    ' Invalid
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30414: Value of type 'Integer()' cannot be converted to 'Integer(*,*)' because the array types have different numbers of dimensions.
                Dim myArray10 As Integer(,) = {1, 2}        ' Invalid
                                              ~~~~~~
BC30414: Value of type 'Integer(*,*,*)' cannot be converted to 'Integer(*,*)' because the array types have different numbers of dimensions.
                Dim myArray11 As Integer(,) = {{{1, 2}}}    ' Invalid
                                              ~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30415ERR_RedimRankMismatch()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="RedimRankMismatch">
        <file name="a.vb">
            Class C1
                Sub foo(ByVal Ary() As Date)
                    ReDim Ary(4, 4)
                End Sub
            End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30415: 'ReDim' cannot change the number of dimensions of an array.
                    ReDim Ary(4, 4)
                          ~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30424ERR_ConstAsNonConstant()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ConstAsNonConstant">
        <file name="a.vb">
            Class C1(Of T)
                Const f As T = Nothing
                Const c As C1(Of T) = Nothing
            End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
                Const f As T = Nothing
                           ~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
                Const c As C1(Of T) = Nothing
                           ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30424ERR_ConstAsNonConstant02()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
imports system

Enum E
    foo
End Enum

structure S1
end structure

Class C1
    ' should work
    public const f1 as E = E.foo
    public const f2 as object = nothing
    public const f3 as boolean = True

    ' should not work
    public const f4 as C1 = nothing
    public const f5 as S1 = nothing
    public const f6() as integer = {1,2,3}
    public const f7() as S1 = {new S1(), new S1(), new S1()}

    Public shared Sub Main(args() as string)
    End sub
End Class
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
    public const f4 as C1 = nothing
                       ~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
    public const f5 as S1 = nothing
                       ~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
    public const f6() as integer = {1,2,3}
                 ~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
    public const f7() as S1 = {new S1(), new S1(), new S1()}
                 ~~
</expected>)
            ' todo: the last two errors need to be removed once collection initialization is supported
        End Sub

        <Fact()>
        Public Sub BC30438ERR_ConstantWithNoValue()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer On

imports system

Class C1
    public const f1
    public const f2 as object 
    public const f3 as boolean

    Public shared Sub Main(args() as string)
    End sub
End Class
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30438: Constants must have a value.
    public const f1
                 ~~
BC30438: Constants must have a value.
    public const f2 as object 
                 ~~
BC30438: Constants must have a value.
    public const f3 as boolean
                 ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30439ERR_ExpressionOverflow1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ConvertArrayRankMismatch2">
        <file name="a.vb">
                Public Class C1
                    Shared Sub Main()
                        Dim i As Integer
                        i = 10000000000000
                        System.Console.WriteLine(i)
                    End Sub
                End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30439: Constant expression not representable in type 'Integer'.
                        i = 10000000000000
                            ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30439ERR_ExpressionOverflow1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ConvertArrayRankMismatch2">
        <file name="a.vb">
                Public Class C1
                    Shared Sub Main()
                        Dim FIRST As UInteger = (0UI - 860UI)
                    End Sub
                End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30439: Constant expression not representable in type 'UInteger'.
                        Dim FIRST As UInteger = (0UI - 860UI)
                                                 ~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30451ERR_NameNotDeclared1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
       Class C1
            function foo() as integer
                return 1 
            End function
        End Class
        Class C2
            function foo1() as integer
                dim s = foo()
                return 1
            End function
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30451: 'foo' is not declared. It may be inaccessible due to its protection level.
                dim s = foo()
                        ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30451ERR_NameNotDeclared1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
        Class C
            Shared ReadOnly Property P
                Get
                    Return Nothing
                End Get
            End Property
            ReadOnly Property Q
                Get
                    Return Nothing
                End Get
            End Property
            Property R
            Sub M()
                set_P(get_P)
                set_Q(get_Q)
                set_R(get_R)
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30451: 'set_P' is not declared. It may be inaccessible due to its protection level.
                set_P(get_P)
                ~~~~~
BC30451: 'get_P' is not declared. It may be inaccessible due to its protection level.
                set_P(get_P)
                      ~~~~~
BC30451: 'set_Q' is not declared. It may be inaccessible due to its protection level.
                set_Q(get_Q)
                ~~~~~
BC30451: 'get_Q' is not declared. It may be inaccessible due to its protection level.
                set_Q(get_Q)
                      ~~~~~
BC30451: 'set_R' is not declared. It may be inaccessible due to its protection level.
                set_R(get_R)
                ~~~~~
BC30451: 'get_R' is not declared. It may be inaccessible due to its protection level.
                set_R(get_R)
                      ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30451ERR_NameNotDeclared1_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoDirectDelegateConstruction1">
        <file name="a.vb">
        Class C1
            Public Delegate Sub myDelegate()
            private shared sub mySub()
            end sub
        End Class
        Module M1
            Sub Main()
                Dim d1 As C1.myDelegate
                d1 = New C1.myDelegate(addressof BORG)
                d1 = addressof BORG
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30451: 'BORG' is not declared. It may be inaccessible due to its protection level.
                d1 = New C1.myDelegate(addressof BORG)
                                                 ~~~~
BC30451: 'BORG' is not declared. It may be inaccessible due to its protection level.
                d1 = addressof BORG
                               ~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30451ERR_NameNotDeclared1_4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NoDirectDelegateConstruction1">
        <file name="a.vb">
Class C
    Public Shared Sub Main()
        For Each x As Integer In New Integer() {1, 1, 1}
            'COMPILEERROR: BC30451, "y"
        Next y
        'escaped vs. nonescaped (should work)"
        For Each [x] As Integer In New Integer() {1, 1, 1}
        Next x
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30451: 'y' is not declared. It may be inaccessible due to its protection level.
        Next y
             ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30451ERR_NameNotDeclared1_5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NoDirectDelegateConstruction1">
        <file name="a.vb">
Option Infer Off
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()
        For n = 0 To 2
            For m = 1 To 2
            Next n
        Next m
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30451: 'n' is not declared. It may be inaccessible due to its protection level.
        For n = 0 To 2
            ~
BC30451: 'm' is not declared. It may be inaccessible due to its protection level.
            For m = 1 To 2
                ~
BC30451: 'n' is not declared. It may be inaccessible due to its protection level.
            Next n
                 ~
BC30451: 'm' is not declared. It may be inaccessible due to its protection level.
        Next m
             ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30451ERR_NameNotDeclared1_NoErrorDuplicationForObjectInitializer()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BC30451ERR_NameNotDeclared1_NoErrorDuplicationForObjectInitializer">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Class S
    Public Y As Object
End Class

Public Module Program
    Public Sub Main(args() As String)
        Dim a, b, c As New S() With {.Y = aaa}
    End Sub
End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30451: 'aaa' is not declared. It may be inaccessible due to its protection level.
        Dim a, b, c As New S() With {.Y = aaa}
                                          ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30451ERR_NameNotDeclared1_NoWarningDuplicationForObjectInitializer()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BC30451ERR_NameNotDeclared1_NoWarningDuplicationForObjectInitializer">
        <file name="a.vb">
Option Strict On

Imports System

Class S
    Public Y As Byte
End Class

Public Module Program
    Public Sub Main(args() As String)
        Dim x As Integer = 1
        Dim a, b, c As New S() With {.Y = x}
    End Sub
End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Byte'.
        Dim a, b, c As New S() With {.Y = x}
                                          ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30452ERR_BinaryOperands3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BinaryOperands3">
        <file name="a.vb">
option infer on            
Class C1
    dim d = new c1() + new c1.c2()
    function foo() as integer
        return 1
    End function
    Class C2
        function foo1() as integer
            dim d1 = new c1()
            dim d2 = new c2()
            dim d3 = d1 + d2
            return 1
        End function
    End Class
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30452: Operator '+' is not defined for types 'C1' and 'C1.C2'.
    dim d = new c1() + new c1.c2()
            ~~~~~~~~~~~~~~~~~~~~~~
BC30452: Operator '+' is not defined for types 'C1' and 'C1.C2'.
            dim d3 = d1 + d2
                     ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30452ERR_BinaryOperands3_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BinaryOperands3">
        <file name="a.vb">
        Class C1
                Class C2
                    function foo1(byval d1 as c1, byval d2 as c2 )as integer
                        dim d3 as object = d1 + d2
                        return 1
                    End function
                End Class
            End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30452: Operator '+' is not defined for types 'C1' and 'C1.C2'.
                        dim d3 as object = d1 + d2
                                           ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30452ERR_BinaryOperands3_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="None">
    <file name="a.vb">
        Imports System
        class myClass1
        shared result = New Guid() And New Guid()
        End class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30452: Operator 'And' is not defined for types 'Guid' and 'Guid'.
        shared result = New Guid() And New Guid()
                        ~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30452ERR_BinaryOperands3_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="None">
    <file name="a.vb">
        Imports System
        Module Program
            Sub Main(args As String())
                Dim f1 As New Foo(), f2 As New Foo(), f3 As New Foo()
                Dim b As Boolean = True
                f3 = If(b, f1 = New Foo(), f2 = New Foo())
                b = False
                f3 = If(b, f1 = New Foo(), f2 = New Foo())
            End Sub
        End Module
        Class Foo
            Public i As Integer
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30452: Operator '=' is not defined for types 'Foo' and 'Foo'.
                f3 = If(b, f1 = New Foo(), f2 = New Foo())
                           ~~~~~~~~~~~~~~
BC30452: Operator '=' is not defined for types 'Foo' and 'Foo'.
                f3 = If(b, f1 = New Foo(), f2 = New Foo())
                                           ~~~~~~~~~~~~~~
BC30452: Operator '=' is not defined for types 'Foo' and 'Foo'.
                f3 = If(b, f1 = New Foo(), f2 = New Foo())
                           ~~~~~~~~~~~~~~
BC30452: Operator '=' is not defined for types 'Foo' and 'Foo'.
                f3 = If(b, f1 = New Foo(), f2 = New Foo())
                                           ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30452ERR_BinaryOperands3_4()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="None">
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        Dim First = New With {.a = 1, .b = 2}
        Dim Second = New With {.a = 1, .b = 2}
    'COMPILEERROR: BC30452, "first = second"
        If first = second Then
        ElseIf second <> first Then
        End If
    End Sub
End Module
    ]]></file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_BinaryOperands3, "first = second").WithArguments("=", "<anonymous type: a As Integer, b As Integer>", "<anonymous type: a As Integer, b As Integer>"),
    Diagnostic(ERRID.ERR_BinaryOperands3, "second <> first").WithArguments("<>", "<anonymous type: a As Integer, b As Integer>", "<anonymous type: a As Integer, b As Integer>"))
        End Sub

        <Fact()>
        Public Sub BC30452ERR_BinaryOperands3_4a()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="BC30452ERR_BinaryOperands3_4a">
    <file name="a.vb"><![CDATA[
Imports System
Module Program
    Sub Main()
    'COMPILEERROR: BC30452, "first = second"
        If New With {.a = 1, .b = 2} = New With {.a = 1, .b = 2} Then
        ElseIf New With {.a = 1, .b = 2} <> New With {.a = 1, .b = 2} Then
        End If
    End Sub
End Module
    ]]></file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30452: Operator '=' is not defined for types '<anonymous type: a As Integer, b As Integer>' and '<anonymous type: a As Integer, b As Integer>'.
        If New With {.a = 1, .b = 2} = New With {.a = 1, .b = 2} Then
           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30452: Operator '<>' is not defined for types '<anonymous type: a As Integer, b As Integer>' and '<anonymous type: a As Integer, b As Integer>'.
        ElseIf New With {.a = 1, .b = 2} <> New With {.a = 1, .b = 2} Then
               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub BC30454ERR_ExpectedProcedure()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ExpectedProcedure">
        <file name="a.vb">
    Module IsNotError001mod
        Sub foo(byval value as integer())
            value()
        exit sub
        End Sub
    End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30454: Expression is not a method.
            value()
            ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30454ERR_ExpectedProcedure_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ExpectedProcedure">
        <file name="a.vb">
        Class C
            Private s As String
            Shared Sub M(x As C, y() As Integer)
                Dim o As Object
                o = x.s(3)
                N(x.s(3))
                x.s(3) ' BC30454
                o = y(3)
                N(y(3))
                y(3) ' BC30454
            End Sub
            Shared Sub N(o)
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30454: Expression is not a method.
                x.s(3) ' BC30454
                ~~~
BC30454: Expression is not a method.
                y(3) ' BC30454
                ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30455ERR_OmittedArgument2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
            Class C
                Default ReadOnly Property P(x, y)
                    Get
                        Return Nothing
                    End Get
                End Property
                Shared Sub M(x As C)
                    N(x!Q)
                End Sub
                Shared Sub N(o As Object)
                End Sub
            End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30455: Argument not specified for parameter 'y' of 'Public ReadOnly Default Property P(x As Object, y As Object) As Object'.
                    N(x!Q)
                      ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30455ERR_OmittedArgument2_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
            Structure C1
               &lt;System.Runtime.InteropServices.FieldOffset()&gt;
               Dim i As Integer
            End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30455: Argument not specified for parameter 'offset' of 'Public Overloads Sub New(offset As Integer)'.
               &lt;System.Runtime.InteropServices.FieldOffset()&gt;
                                               ~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30456ERR_NameNotMember2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
    Class Class1
    Private Shared foo As S1
    Class Class2
        Sub Test()
                foo.bar1()
        End Sub
    End Class
    End Class
    Structure S1
        Public shared bar As String = "Hello"
    End Structure
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30456: 'bar1' is not a member of 'S1'.
                foo.bar1()
                ~~~~~~~~
</expected>)
        End Sub

        <WorkItem(538903, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538903")>
        <Fact()>
        Public Sub BC30456ERR_NameNotMember2_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
    Module M1
        Sub FOO()
                My.Application.Exit()
        End Sub
    End Module
        </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30451: 'My' is not declared. It may be inaccessible due to its protection level.
                My.Application.Exit()
                ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30456ERR_NameNotMember2_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
    Module M1
        Sub FOO()
            Dim blnReturn As Boolean = False
            Dim x As System.Nullable(Of Integer)
            blnReturn = system.nullable.hasvalue(x)
        End Sub
    End Module
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30456: 'hasvalue' is not a member of 'Nullable'.
            blnReturn = system.nullable.hasvalue(x)
                        ~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30456ERR_NameNotMember2_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
        Class C
            Shared ReadOnly Property P
                Get
                    Return Nothing
                End Get
            End Property
            ReadOnly Property Q
                Get
                    Return Nothing
                End Get
            End Property
            Sub M()
                C.set_P(C.get_P)
                Me.set_Q(Me.get_Q)
            End Sub
        End Class
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30456: 'set_P' is not a member of 'C'.
                C.set_P(C.get_P)
                ~~~~~~~
BC30456: 'get_P' is not a member of 'C'.
                C.set_P(C.get_P)
                        ~~~~~~~
BC30456: 'set_Q' is not a member of 'C'.
                Me.set_Q(Me.get_Q)
                ~~~~~~~~
BC30456: 'get_Q' is not a member of 'C'.
                Me.set_Q(Me.get_Q)
                         ~~~~~~~~
</expected>)
        End Sub

        <WorkItem(10046, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub BC30456ERR_NameNotMember2_4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
        Option Infer On
Imports System
Class Program
    Dim x As New Product With {.Name = "paperclips", .price1 = 1.29}
End Class
Class Product
    Property price As Double
    Property Name As String
End Class
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30456: 'price1' is not a member of 'Product'.
    Dim x As New Product With {.Name = "paperclips", .price1 = 1.29}
                                                      ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30456ERR_NameNotMember2_5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BC30456ERR_NameNotMember2_4">
        <file name="a.vb">
Module M1
    Class B
    End Class

    Class D
        Inherits B

        Public Shadows Sub M()
        End Sub

        Public Sub Test()
            MyBase.M()
            Me.M()
        End Sub
    End Class
End Module
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30456: 'M' is not a member of 'M1.B'.
            MyBase.M()
            ~~~~~~~~
</expected>)
        End Sub

        <WorkItem(529710, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529710")>
        <Fact()>
        Public Sub BC30456ERR_NameNotMember3_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Namespace N
    Module X
        Sub Main()
            N.Equals("", "")
        End Sub
    End Module
End Namespace
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30456: 'Equals' is not a member of 'N'.
            N.Equals("", "")
            ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30469ERR_ObjectReferenceNotSupplied_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
       Class C1
            function foo() as integer
                return 1 
            End function
            Class C2
                function foo1() as integer
                    dim s = foo()
                    return 1
                End function
            End Class
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30469: Reference to a non-shared member requires an object reference.
                    dim s = foo()
                            ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30469ERR_ObjectReferenceNotSupplied_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ObjectReferenceNotSupplied">
        <file name="a.vb">
       Class P(Of T)
            Public ReadOnly son As T
            Class P1
                Sub New1(ByVal tval As T)
                    son = tval
                End Sub
            End Class
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30469: Reference to a non-shared member requires an object reference.
                    son = tval
                    ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30469ERR_ObjectReferenceNotSupplied_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
        Class C
            Property P
                Get
                    Return Nothing
                End Get
                Set(ByVal value)
                End Set
            End Property
            Shared Sub M()
                Dim o = C.P
                C.P = o
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30469: Reference to a non-shared member requires an object reference.
                Dim o = C.P
                        ~~~
BC30469: Reference to a non-shared member requires an object reference.
                C.P = o
                ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30469ERR_ObjectReferenceNotSupplied_4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
        Class A
            Property P
                Get
                    Return Nothing
                End Get
                Set(ByVal value)
                End Set
            End Property
            Class B
                Sub M(ByVal value)
                    P = value
                    value = P
                End Sub
            End Class
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30469: Reference to a non-shared member requires an object reference.
                    P = value
                    ~
BC30469: Reference to a non-shared member requires an object reference.
                    value = P
                            ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30469ERR_ObjectReferenceNotSupplied_FieldInitializers()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
       Class C1
            public f1 as integer
            public shared f2 as integer = C1.f1
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30469: Reference to a non-shared member requires an object reference.
            public shared f2 as integer = C1.f1
                                          ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30469ERR_ObjectReferenceNotSupplied_DelegateCreation()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoDirectDelegateConstruction1">
        <file name="a.vb">
        Class C1
            Public Delegate Sub myDelegate()
            public sub mySub()
            end sub
        End Class
        Module M1
            Sub foo()
                Dim d1 As C1.myDelegate
                d1 = New C1.myDelegate(addressof C1.mySub)
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30469: Reference to a non-shared member requires an object reference.
                d1 = New C1.myDelegate(addressof C1.mySub)
                                       ~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30470ERR_MyClassNotInClass()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="MyClassNotInClass">
        <file name="a.vb">
        Module M1
            Sub FOO()
                MyClass.New()
            End Sub
            Sub New()
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30470: 'MyClass' cannot be used outside of a class.
                MyClass.New()
                ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30487ERR_UnaryOperand2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UnaryOperand2">
        <file name="a.vb">
        Class C1
            Shared Sub FOO()
                Dim expr As c2 = new c2()
                expr = -expr
            End Sub
        End Class
        Class C2
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30487: Operator '-' is not defined for type 'C2'.
                expr = -expr
                       ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30491ERR_VoidValue()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VoidValue">
        <file name="a.vb">
        Structure C1
            Sub FOO()
                'Dim a1 = If (True, New Object, TestMethod)
                'Dim a2 = If (True, {TestMethod()}, {TestMethod()})
                Dim a3 = TestMethod() = TestMethod()
            End Sub
            Sub TestMethod()
            End Sub
        End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30491: Expression does not produce a value.
                Dim a3 = TestMethod() = TestMethod()
                         ~~~~~~~~~~~~
BC30491: Expression does not produce a value.
                Dim a3 = TestMethod() = TestMethod()
                                        ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30491ERR_VoidValue_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="VoidValue">
        <file name="a.vb">
        Imports System
            Module Program
                Sub Main(args As String())
                    Dim x = If(True, Console.WriteLine(0), Console.WriteLine(1))
                    Dim y = If(True, fun_void(), fun_int(1))
                    Dim z = If(True, fun_Exception(1), fun_int(1))
                    Dim r = If(True, fun_long(0), fun_int(1))
                    Dim s = If(False, fun_long(0), fun_int(1))
                End Sub
                Private Sub fun_void()
                    Return
                End Sub
                Private Function fun_int(x As Integer) As Integer
                    Return x
                End Function
                Private Function fun_long(x As Integer) As Long
                    Return CLng(x)
                End Function
                Private Function fun_Exception(x As Integer) As Exception
                    Return New Exception()
                End Function
            End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30491: Expression does not produce a value.
                    Dim x = If(True, Console.WriteLine(0), Console.WriteLine(1))
                                     ~~~~~~~~~~~~~~~~~~~~
BC30491: Expression does not produce a value.
                    Dim x = If(True, Console.WriteLine(0), Console.WriteLine(1))
                                                           ~~~~~~~~~~~~~~~~~~~~
BC30491: Expression does not produce a value.
                    Dim y = If(True, fun_void(), fun_int(1))
                                     ~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30491ERR_VoidValue_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="VoidValue">
        <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim x As Integer = 1
        Dim y As Object = 0
        Dim s = If(True, fun(x), y)
        Dim s1 = If(False, sub1(x), y)
    End Sub
    Private Function fun(Of T)(Parm1 As T) As T
        Return Parm1
    End Function
    Private Sub sub1(Of T)(Parm1 As T)
    End Sub
End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30491: Expression does not produce a value.
        Dim s1 = If(False, sub1(x), y)
                           ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30491ERR_VoidValue_SelectCase()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="VoidValue">
        <file name="a.vb">
        Structure C1
            Sub Foo()
                Select Case TestMethod()
                End Select
            End Sub
            Sub TestMethod()
            End Sub
        End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30491: Expression does not produce a value.
                Select Case TestMethod()
                            ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30500ERR_CircularEvaluation1()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
imports system

Class C1
    Enum E
        A = A
    End Enum

    public const f1 as integer = f2
    public const f2 as integer = f1

    Public shared Sub Main(args() as string)
        Console.WriteLine(E.A)    
    End sub
End Class
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30500: Constant 'A' cannot depend on its own value.
        A = A
        ~
BC30500: Constant 'f1' cannot depend on its own value.
    public const f1 as integer = f2
                 ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30500ERR_CircularEvaluation1_02()
            Dim source =
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer On

imports system

Class C1
    private const f1 = f2
    private const f2 = f1

    Public shared Sub Main(args() as string)
        console.writeline(f1)
        console.writeline(f2)
    End sub
End Class
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30500: Constant 'f1' cannot depend on its own value.
    private const f1 = f2
                  ~~
</expected>)

        End Sub

        <Fact(), WorkItem(528728, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528728")>
        Public Sub BC30500ERR_CircularEvaluation1_03()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CircularEvaluation1">
        <file name="a.vb">
        Module M1
            Sub New()
                Const Val As Integer = Val
            End Sub
        End Module
    </file>
    </compilation>)
            compilation.AssertTheseDiagnostics(
<expected>
BC30500: Constant 'Val' cannot depend on its own value.
                Const Val As Integer = Val
                      ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30500ERR_CircularEvaluation1_04()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Const c0, c1 = c2 + 1
    Const c2, c3 = c0 + 1
End Class
    </file>
</compilation>)
            compilation.AssertTheseDiagnostics(
<expected>
BC30500: Constant 'c0' cannot depend on its own value.
    Const c0, c1 = c2 + 1
          ~~
BC30671: Explicit initialization is not permitted with multiple variables declared with a single type specifier.
    Const c0, c1 = c2 + 1
          ~~~~~~~~~~~~~~~
BC30671: Explicit initialization is not permitted with multiple variables declared with a single type specifier.
    Const c2, c3 = c0 + 1
          ~~~~~~~~~~~~~~~
BC30060: Conversion from 'Integer' to 'Object' cannot occur in a constant expression.
    Const c2, c3 = c0 + 1
                        ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30512ERR_NarrowingConversionDisallowed2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NarrowingConversionDisallowed2">
        <file name="a.vb">
    Option Strict On
    Imports System
    Module M1
        Sub Foo()
            Dim b As Byte = 2
            Dim c As Byte = 3
            Dim s As Short = 2
            Dim t As Short = 3
            Dim i As Integer = 2
            Dim j As Integer = 3
            Dim l As Long = 2
            Dim m As Long = 3
            b = b &lt; c
            b = b ^ c
            s = s &lt; t
            s = s ^ t
            i = i &gt; j
            i = i ^ j
            l = l &gt; m
            l = l ^ m
        End Sub
    End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30512: Option Strict On disallows implicit conversions from 'Boolean' to 'Byte'.
            b = b &lt; c
                ~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Byte'.
            b = b ^ c
                ~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Boolean' to 'Short'.
            s = s &lt; t
                ~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Short'.
            s = s ^ t
                ~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Boolean' to 'Integer'.
            i = i &gt; j
                ~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Integer'.
            i = i ^ j
                ~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Boolean' to 'Long'.
            l = l &gt; m
                ~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Long'.
            l = l ^ m
                ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30512ERR_NarrowingConversionDisallowed2_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
            Option Strict On
            Class C
                Default ReadOnly Property P(i As Integer) As Object
                    Get
                        Return Nothing
                    End Get
                End Property
                Shared Sub M(x As C)
                    N(x("Q"))
                    N(x!Q)
                End Sub
                Shared Sub N(o As Object)
                End Sub
            End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Integer'.
                    N(x("Q"))
                        ~~~
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Integer'.
                    N(x!Q)
                        ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30512ERR_NarrowingConversionDisallowed2_2()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Option Strict On
Module Program
    Sub Main(args As String())
        Dim S1 As String = "3"
        Dim S1_b As Object = If(S1, 3, 2)
        Dim S2 As SByte = 4
        Dim S2_b As Object = If(S2, 4, 2)
        Dim S3 As Byte = 5
        Dim S3_b As Object = If(S3, 5, 2)
        Dim S4 As Short = 6
        Dim S4_b As Object = If(S4, 6, 2)
        Dim S5 As UShort = 7
        Dim S5_b As Object = If(S5, 7, 2)
        Dim S6 As Integer = 8
        Dim S6_b As Object = If(S6, 8, 2)
        Dim S7 As UInteger = 9
        Dim S7_b As Object = If(S7, 9, 2)
        Dim S8 As Long = 10
        Dim S8_b As Object = If(S8, 10, 2)
        Dim S9 As Short? = 5
        Dim S9_b As Object = If(S9, 3, 2)
        Dim S10 As Integer? = 51
        Dim S10_b As Object = If(S10, 3, 2)
        Dim S11 As Boolean? = Nothing
        Dim S11_b As Object = If(S11, 3, 2)
    End Sub
End Module
    </file>
    </compilation>).VerifyDiagnostics(
    Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "S1").WithArguments("String", "Boolean"),
    Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "S2").WithArguments("SByte", "Boolean"),
    Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "S3").WithArguments("Byte", "Boolean"),
    Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "S4").WithArguments("Short", "Boolean"),
    Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "S5").WithArguments("UShort", "Boolean"),
    Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "S6").WithArguments("Integer", "Boolean"),
    Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "S7").WithArguments("UInteger", "Boolean"),
    Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "S8").WithArguments("Long", "Boolean"),
    Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "S9").WithArguments("Short?", "Boolean?"),
    Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "S10").WithArguments("Integer?", "Boolean?"))

        End Sub

        <Fact()>
        Public Sub BC30512ERR_NarrowingConversionDisallowed2_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Option Strict On
Imports System
Module Program
    Sub Main(args As String())
        Dim S1_a As Short? = 5
        Dim S1_b As Integer? = 51
        Dim S1_c As Short? = If(True, S1_a, S1_b)
        Dim S1_d As Boolean? = If(True, S1_a, S1_b)
        Dim S2_a As Char
        Dim S2_b As String = "31"
        Dim S2_c As String = If(True, S2_a, S2_b)
        Dim S2_d As Char = If(False, S2_a, S2_b)
        Dim S2_e As Short = If(False, S2_a, S2_b)
    End Sub
End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30512: Option Strict On disallows implicit conversions from 'Integer?' to 'Short?'.
        Dim S1_c As Short? = If(True, S1_a, S1_b)
                             ~~~~~~~~~~~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Integer?' to 'Boolean?'.
        Dim S1_d As Boolean? = If(True, S1_a, S1_b)
                               ~~~~~~~~~~~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Char'.
        Dim S2_d As Char = If(False, S2_a, S2_b)
                           ~~~~~~~~~~~~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Short'.
        Dim S2_e As Short = If(False, S2_a, S2_b)
                            ~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30512ERR_NarrowingConversionDisallowed2_4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()
        For ivar As Integer = 0.1 To 10
        Next
        For dvar As Double = #12:00:00 AM# To 10
        Next
        For dvar As Double = True To 10
        Next
        For dvar1 As Double = 123&amp; To 10
            'ok
        Next
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Integer'.
        For ivar As Integer = 0.1 To 10
                              ~~~
BC30532: Conversion from 'Date' to 'Double' requires calling the 'Date.ToOADate' method.
        For dvar As Double = #12:00:00 AM# To 10
                             ~~~~~~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Boolean' to 'Double'.
        For dvar As Double = True To 10
                             ~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30512ERR_NarrowingConversionDisallowed2_5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Option Strict On
Imports System
Module Module1
    Sub Main()
        Dim arr As Integer(,) = New Integer(1.0, 4) {} ' Invalid
        Dim arr1 As Integer(,) = New Integer(CDbl(5), 4) {} ' Invalid
        Dim arr2 As Integer(,) = New Integer(CDec(5), 4) {} ' Invalid
        Dim arr3 As Integer(,) = New Integer(CSng(5), 4) {} ' Invalid
        Dim x As Double = 5
        Dim arr4 As Integer(,) = New Integer(x, 4) {} ' Invalid
        Dim y As Single = 5
        Dim arr5 As Integer(,) = New Integer(y, 4) {} ' Invalid
        Dim z As Decimal = 5
        Dim arr6 As Integer(,) = New Integer(z, 4) {} ' Invalid
        Dim m As Boolean = True
        Dim arr7 As Integer(,) = New Integer(m, 4) {} ' Invalid
    End Sub
End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Integer'.
        Dim arr As Integer(,) = New Integer(1.0, 4) {} ' Invalid
                                            ~~~
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Integer'.
        Dim arr1 As Integer(,) = New Integer(CDbl(5), 4) {} ' Invalid
                                             ~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Decimal' to 'Integer'.
        Dim arr2 As Integer(,) = New Integer(CDec(5), 4) {} ' Invalid
                                             ~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Single' to 'Integer'.
        Dim arr3 As Integer(,) = New Integer(CSng(5), 4) {} ' Invalid
                                             ~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Integer'.
        Dim arr4 As Integer(,) = New Integer(x, 4) {} ' Invalid
                                             ~
BC30512: Option Strict On disallows implicit conversions from 'Single' to 'Integer'.
        Dim arr5 As Integer(,) = New Integer(y, 4) {} ' Invalid
                                             ~
BC30512: Option Strict On disallows implicit conversions from 'Decimal' to 'Integer'.
        Dim arr6 As Integer(,) = New Integer(z, 4) {} ' Invalid
                                             ~
BC30512: Option Strict On disallows implicit conversions from 'Boolean' to 'Integer'.
        Dim arr7 As Integer(,) = New Integer(m, 4) {} ' Invalid
                                             ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30512ERR_NarrowingConversionDisallowed2_6()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Option Strict On
Imports System
Module Module1
    Sub Main()
        Dim myArray9 As Char(,) = New Char(2, 1) {{"a", "b"}, {"c", "d"}, {"e", "f"}}
    End Sub
End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Char'.
        Dim myArray9 As Char(,) = New Char(2, 1) {{"a", "b"}, {"c", "d"}, {"e", "f"}}
                                                   ~~~
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Char'.
        Dim myArray9 As Char(,) = New Char(2, 1) {{"a", "b"}, {"c", "d"}, {"e", "f"}}
                                                        ~~~
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Char'.
        Dim myArray9 As Char(,) = New Char(2, 1) {{"a", "b"}, {"c", "d"}, {"e", "f"}}
                                                               ~~~
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Char'.
        Dim myArray9 As Char(,) = New Char(2, 1) {{"a", "b"}, {"c", "d"}, {"e", "f"}}
                                                                    ~~~
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Char'.
        Dim myArray9 As Char(,) = New Char(2, 1) {{"a", "b"}, {"c", "d"}, {"e", "f"}}
                                                                           ~~~
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Char'.
        Dim myArray9 As Char(,) = New Char(2, 1) {{"a", "b"}, {"c", "d"}, {"e", "f"}}
                                                                                ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30512ERR_NarrowingConversionDisallowed2_7()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Option Infer On
Option Strict On
Imports System
Imports Microsoft.VisualBasic.Strings
Module Module1
    Sub Main(args As String())
        Dim arr As Integer(,) = New Integer(4, 4) {}
        Dim x As Integer = 0
        Dim idx As Double = 2.0
        Dim idx1 As ULong = 0
        Dim idx2 As Char = ChrW(3)
        arr(idx, 3) = 100      ' Invalid
        arr(idx1, x) = 100     ' Invalid
        arr(idx2, 3) = 100      'Invalid
        arr(" "c, 32) = 100     'Invalid
        Dim arr1 As Integer(,,) = {{{1, 2}, {1, 2}}, {{1, 2}, {1, 2}}}
        Dim i1 As ULong = 0
        Dim i2 As UInteger = 0
        Dim i3 As Integer = 0
        arr1(i1, i2, i3) = 9        ' Invalid
    End Sub
End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Integer'.
        arr(idx, 3) = 100      ' Invalid
            ~~~
BC30512: Option Strict On disallows implicit conversions from 'ULong' to 'Integer'.
        arr(idx1, x) = 100     ' Invalid
            ~~~~
BC32006: 'Char' values cannot be converted to 'Integer'. Use 'Microsoft.VisualBasic.AscW' to interpret a character as a Unicode value or 'Microsoft.VisualBasic.Val' to interpret it as a digit.
        arr(idx2, 3) = 100      'Invalid
            ~~~~
BC32006: 'Char' values cannot be converted to 'Integer'. Use 'Microsoft.VisualBasic.AscW' to interpret a character as a Unicode value or 'Microsoft.VisualBasic.Val' to interpret it as a digit.
        arr(" "c, 32) = 100     'Invalid
            ~~~~
BC30512: Option Strict On disallows implicit conversions from 'ULong' to 'Integer'.
        arr1(i1, i2, i3) = 9        ' Invalid
             ~~
BC30512: Option Strict On disallows implicit conversions from 'UInteger' to 'Integer'.
        arr1(i1, i2, i3) = 9        ' Invalid
                 ~~
</expected>)
        End Sub

        <WorkItem(528762, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528762")>
        <Fact>
        Public Sub BC30512ERR_NarrowingConversionDisallowed2_8()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Option Strict On
Option Infer On
Public Class Class2(Of T As Res)
    Private x As T(,) = New T(1, 1) {{New Res(), New Res()}, {New Res(), New Res()}}    ' invalid
    Public Sub Foo()
        Dim x As T(,) = New T(1, 2) {}
    End Sub
End Class
Public Class Res
End Class
    </file>
    </compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "New Res()").WithArguments("Res", "T"),
            Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "New Res()").WithArguments("Res", "T"),
            Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "New Res()").WithArguments("Res", "T"),
            Diagnostic(ERRID.ERR_NarrowingConversionDisallowed2, "New Res()").WithArguments("Res", "T"))
        End Sub

        <Fact()>
        Public Sub BC30512ERR_SelectCaseNarrowingConversionErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Option Strict On
Imports System        
Module M1
    Sub Main()
        Dim success As Boolean = True
        For count = 0 To 13
            Test(count, success)
        Next

        If success Then
            Console.Write("Success")
        Else
            Console.Write("Fail")
        End If
    End Sub

    Sub Test(count As Integer, ByRef success As Boolean)
        Dim Bo As Boolean
        Dim Ob As Object
        Dim SB As SByte
        Dim By As Byte
        Dim Sh As Short
        Dim US As UShort
        Dim [In] As Integer
        Dim UI As UInteger
        Dim Lo As Long
        Dim UL As ULong
        Dim De As Decimal
        Dim Si As Single
        Dim [Do] As Double
        Dim St As String

        Bo = False
        Ob = 1
        SB = 2
        By = 3
        Sh = 4
        US = 5
        [In] = 6
        UI = 7
        Lo = 8
        UL = 9
        Si = 10
        [Do] = 11
        De = 12D
        St = "13"

        Select Case count
            Case Bo
                success = success AndAlso If(count = 0, True, False)
            Case Is < Ob
                success = success AndAlso If(count = 1, True, False)
            Case SB
                success = success AndAlso If(count = 2, True, False)
            Case By
                success = success AndAlso If(count = 3, True, False)
            Case Sh
                success = success AndAlso If(count = 4, True, False)
            Case US
                success = success AndAlso If(count = 5, True, False)
            Case [In]
                success = success AndAlso If(count = 6, True, False)
            Case UI To Lo
                success = success AndAlso If(count = 7, True, False)
            Case Lo
                success = success AndAlso If(count = 8, True, False)
            Case UL
                success = success AndAlso If(count = 9, True, False)
            Case Si
                success = success AndAlso If(count = 10, True, False)
            Case [Do]
                success = success AndAlso If(count = 11, True, False)
            Case De
                success = success AndAlso If(count = 12, True, False)
            Case St
                success = success AndAlso If(count = 13, True, False)
            Case Else
                success = False
        End Select
    End Sub
End Module
    ]]></file>
    </compilation>)
            Dim expectedErrors = <errors><![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Boolean' to 'Integer'.
            Case Bo
                 ~~
BC30038: Option Strict On prohibits operands of type Object for operator '<'.
            Case Is < Ob
                      ~~
BC30512: Option Strict On disallows implicit conversions from 'UInteger' to 'Integer'.
            Case UI To Lo
                 ~~
BC30512: Option Strict On disallows implicit conversions from 'Long' to 'Integer'.
            Case UI To Lo
                       ~~
BC30512: Option Strict On disallows implicit conversions from 'Long' to 'Integer'.
            Case Lo
                 ~~
BC30512: Option Strict On disallows implicit conversions from 'ULong' to 'Integer'.
            Case UL
                 ~~
BC30512: Option Strict On disallows implicit conversions from 'Single' to 'Integer'.
            Case Si
                 ~~
BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Integer'.
            Case [Do]
                 ~~~~
BC30512: Option Strict On disallows implicit conversions from 'Decimal' to 'Integer'.
            Case De
                 ~~
BC30512: Option Strict On disallows implicit conversions from 'String' to 'Integer'.
            Case St
                 ~~
]]></errors>

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors)
        End Sub

        <Fact()>
        Public Sub BC30516ERR_NoArgumentCountOverloadCandidates1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoArgumentCountOverloadCandidates1">
        <file name="a.vb">
        Module Module1
            Class C0
                Public whichOne As String
                Sub Foo(ByVal t1 As String)
                    whichOne = "T"
                End Sub
            End Class
            Class C1
                Inherits C0
                Overloads Sub Foo(ByVal y1 As String)
                    whichOne = "Y"
                End Sub
            End Class
            Sub test()
                Dim [ident1] As C0 = New C0()
                Dim clsNarg2get As C1 = New C1()
                Dim str1 As String = "Visual Basic"
                'COMPILEERROR: BC30516, "y"
                clsNarg2get.Foo(1, y1:=2)
                'COMPILEERROR: BC30516, "x"
                clsNarg2get.Foo(y1:=1, y1:=1)
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.
                clsNarg2get.Foo(1, y1:=2)
                            ~~~
BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.
                clsNarg2get.Foo(y1:=1, y1:=1)
                            ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30517ERR_NoViableOverloadCandidates1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NoViableOverloadCandidates1">
        <file name="a.vb">
        Imports System
        &lt;AttributeUsage(AttributeTargets.All)&gt; Class attr2
            Inherits Attribute
            Private Sub New(ByVal i As Integer)
            End Sub
            Protected Sub New(ByVal i As Char)
            End Sub
        End Class
        &lt;attr2(1)&gt; Class target2
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30517: Overload resolution failed because no 'New' is accessible.
        &lt;attr2(1)&gt; Class target2
         ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30518ERR_NoCallableOverloadCandidates2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NoCallableOverloadCandidates2">
        <file name="a.vb">
        class M1
            Sub sub1(Of U, V) (ByVal p1 As U, ByVal p2 As V)
            End Sub
            Sub sub1(Of U, V) (ByVal p1() As V, ByVal p2() As U)
            End Sub
            Sub GenMethod6210()
                sub1(Of String, Integer) (17@, #3/3/2003#)
                sub1(Of Integer, String) (New Integer() {1, 2, 3}, New String() {"a", "b"})
            End Sub
        End class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30518: Overload resolution failed because no accessible 'sub1' can be called with these arguments:
    'Public Sub sub1(Of String, Integer)(p1 As String, p2 As Integer)': Value of type 'Date' cannot be converted to 'Integer'.
    'Public Sub sub1(Of String, Integer)(p1 As Integer(), p2 As String())': Value of type 'Decimal' cannot be converted to 'Integer()'.
    'Public Sub sub1(Of String, Integer)(p1 As Integer(), p2 As String())': Value of type 'Date' cannot be converted to 'String()'.
                sub1(Of String, Integer) (17@, #3/3/2003#)
                ~~~~~~~~~~~~~~~~~~~~~~~~
BC30518: Overload resolution failed because no accessible 'sub1' can be called with these arguments:
    'Public Sub sub1(Of Integer, String)(p1 As Integer, p2 As String)': Value of type 'Integer()' cannot be converted to 'Integer'.
    'Public Sub sub1(Of Integer, String)(p1 As Integer, p2 As String)': Value of type 'String()' cannot be converted to 'String'.
    'Public Sub sub1(Of Integer, String)(p1 As String(), p2 As Integer())': Value of type 'Integer()' cannot be converted to 'String()' because 'Integer' is not derived from 'String'.
    'Public Sub sub1(Of Integer, String)(p1 As String(), p2 As Integer())': Value of type 'String()' cannot be converted to 'Integer()' because 'String' is not derived from 'Integer'.
                sub1(Of Integer, String) (New Integer() {1, 2, 3}, New String() {"a", "b"})
                ~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(546763, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546763")>
        <Fact()>
        Public Sub BC30518ERR_NoCallableOverloadCandidates_LateBindingDisabled()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation>
        <file name="a.vb">
Option Strict On
Option Infer On
Imports System
Imports System.Threading.Tasks

Public Module Program
    Sub Main()
        Dim a As Object = Nothing
        Parallel.ForEach(a, Sub(x As Object) Console.WriteLine(x))
    End Sub
End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30518: Overload resolution failed because no accessible 'ForEach' can be called with these arguments:
    'Public Shared Overloads Function ForEach(Of TSource)(source As IEnumerable(Of TSource), body As Action(Of TSource)) As ParallelLoopResult': Data type(s) of the type parameter(s) cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
    'Public Shared Overloads Function ForEach(Of TSource)(source As IEnumerable(Of TSource), body As Action(Of TSource, ParallelLoopState)) As ParallelLoopResult': Data type(s) of the type parameter(s) cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
    'Public Shared Overloads Function ForEach(Of TSource)(source As IEnumerable(Of TSource), body As Action(Of TSource, ParallelLoopState, Long)) As ParallelLoopResult': Data type(s) of the type parameter(s) cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
    'Public Shared Overloads Function ForEach(Of TSource)(source As Partitioner(Of TSource), body As Action(Of TSource)) As ParallelLoopResult': Data type(s) of the type parameter(s) cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
    'Public Shared Overloads Function ForEach(Of TSource)(source As Partitioner(Of TSource), body As Action(Of TSource, ParallelLoopState)) As ParallelLoopResult': Data type(s) of the type parameter(s) cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
    'Public Shared Overloads Function ForEach(Of TSource)(source As OrderablePartitioner(Of TSource), body As Action(Of TSource, ParallelLoopState, Long)) As ParallelLoopResult': Data type(s) of the type parameter(s) cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Parallel.ForEach(a, Sub(x As Object) Console.WriteLine(x))
                 ~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(542956, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542956")>
        Public Sub BC30518ERR_NoCallableOverloadCandidates2_trycatch()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="ExitTryNotWithinTry">
    <file name="a.vb">
Imports System
Imports System.Linq
Class BaseClass
    Function Method() As String
        Dim x = New Integer() {}
        x.Where(Function(y)
                    Try
                        Exit Try
                    Catch ex1 As Exception When True
                        Exit Try
                    Finally
                        Exit Function
                    End Try
                    Return y = ""
                End Function)
        Return "x"
    End Function
End Class
Class DerivedClass
    Inherits BaseClass
    Shared Sub Main()
    End Sub
End Class
    </file>
</compilation>, {Net40.References.SystemCore})
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30101: Branching out of a 'Finally' is not valid.
                        Exit Function
                        ~~~~~~~~~~~~~
BC42353: Function '&lt;anonymous method>' doesn't return a value on all code paths. Are you missing a 'Return' statement?
                End Function)
                ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30519ERR_NoNonNarrowingOverloadCandidates2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoNonNarrowingOverloadCandidates2">
        <file name="a.vb">
    Module Module1
        Class C0(Of T)
            Public whichOne As String
            Sub Foo(ByVal t1 As T)
                whichOne = "T"
            End Sub
            Default Property Prop1(ByVal t1 As T) As Integer
                Get
                    Return 100
                End Get
                Set(ByVal Value As Integer)
                    whichOne = "T"
                End Set
            End Property
        End Class
        Class C1(Of T, Y)
            Inherits C0(Of T)
            Overloads Sub Foo(ByVal y1 As Y)
                whichOne = "Y"
            End Sub
            Default Overloads Property Prop1(ByVal y1 As Y) As Integer
                Get
                    Return 200
                End Get
                Set(ByVal Value As Integer)
                    whichOne = "Y"
                End Set
            End Property
        End Class
        Structure S1
            Dim i As Integer
        End Structure
        Class Scenario11
            Public Shared Narrowing Operator CType(ByVal Arg As Scenario11) As C1(Of Integer, Integer)
                Return New C1(Of Integer, Integer)
            End Operator
            Public Shared Narrowing Operator CType(ByVal Arg As Scenario11) As S1
                Return New S1
            End Operator
        End Class
        Sub GenUnif0060()
            Dim iTmp As Integer = 2000
            Dim dTmp As Decimal = CDec(2000000)
            Dim tc2 As New C1(Of S1, C1(Of Integer, Integer))
            Dim tc3 As New C1(Of Short, Long)
            Dim sc11 As New Scenario11
            ' COMPILEERROR: BC30519,"Call tc2.Foo (New Scenario11)"
            Call tc2.Foo(New Scenario11)
            ' COMPILEERROR: BC30519,"Call tc2.Foo (sc11)"
            Call tc2.Foo(sc11)
            ' COMPILEERROR: BC30519,"Call tc3.Foo (dTmp)"
            Call tc3.Foo(dTmp)
            ' COMPILEERROR: BC30519,"tc2 (New Scenario11) = 1000"
            tc2(New Scenario11) = 1000
            ' COMPILEERROR: BC30519,"tc2 (New Scenario11)"
            iTmp = tc2(New Scenario11)
            ' COMPILEERROR: BC30519,"tc3 (dTmp) = 2000"
            tc3(dTmp) = 2000
            ' COMPILEERROR: BC30519,"tc3 (dTmp)"
            iTmp = tc3(dTmp)
        End Sub
    End Module
    </file>
    </compilation>)

            compilation.VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_NoNonNarrowingOverloadCandidates2, "Foo").WithArguments("Foo", <![CDATA[
    'Public Overloads Sub Foo(y1 As Module1.C1(Of Integer, Integer))': Argument matching parameter 'y1' narrows from 'Module1.Scenario11' to 'Module1.C1(Of Integer, Integer)'.
    'Public Sub Foo(t1 As Module1.S1)': Argument matching parameter 't1' narrows from 'Module1.Scenario11' to 'Module1.S1'.]]>.Value.Replace(vbLf, Environment.NewLine)),
                    Diagnostic(ERRID.ERR_NoNonNarrowingOverloadCandidates2, "Foo").WithArguments("Foo", <![CDATA[
    'Public Overloads Sub Foo(y1 As Module1.C1(Of Integer, Integer))': Argument matching parameter 'y1' narrows from 'Module1.Scenario11' to 'Module1.C1(Of Integer, Integer)'.
    'Public Sub Foo(t1 As Module1.S1)': Argument matching parameter 't1' narrows from 'Module1.Scenario11' to 'Module1.S1'.]]>.Value.Replace(vbLf, Environment.NewLine)),
                    Diagnostic(ERRID.ERR_NoNonNarrowingOverloadCandidates2, "Foo").WithArguments("Foo", <![CDATA[
    'Public Overloads Sub Foo(y1 As Long)': Argument matching parameter 'y1' narrows from 'Decimal' to 'Long'.
    'Public Sub Foo(t1 As Short)': Argument matching parameter 't1' narrows from 'Decimal' to 'Short'.]]>.Value.Replace(vbLf, Environment.NewLine)),
             Diagnostic(ERRID.ERR_NoNonNarrowingOverloadCandidates2, "tc2").WithArguments("Prop1", <![CDATA[
    'Public Overloads Default Property Prop1(y1 As Module1.C1(Of Integer, Integer)) As Integer': Argument matching parameter 'y1' narrows from 'Module1.Scenario11' to 'Module1.C1(Of Integer, Integer)'.
    'Public Default Property Prop1(t1 As Module1.S1) As Integer': Argument matching parameter 't1' narrows from 'Module1.Scenario11' to 'Module1.S1'.]]>.Value.Replace(vbLf, Environment.NewLine)),
             Diagnostic(ERRID.ERR_NoNonNarrowingOverloadCandidates2, "tc2").WithArguments("Prop1", <![CDATA[
    'Public Overloads Default Property Prop1(y1 As Module1.C1(Of Integer, Integer)) As Integer': Argument matching parameter 'y1' narrows from 'Module1.Scenario11' to 'Module1.C1(Of Integer, Integer)'.
    'Public Default Property Prop1(t1 As Module1.S1) As Integer': Argument matching parameter 't1' narrows from 'Module1.Scenario11' to 'Module1.S1'.]]>.Value.Replace(vbLf, Environment.NewLine)),
             Diagnostic(ERRID.ERR_NoNonNarrowingOverloadCandidates2, "tc3").WithArguments("Prop1", <![CDATA[
    'Public Overloads Default Property Prop1(y1 As Long) As Integer': Argument matching parameter 'y1' narrows from 'Decimal' to 'Long'.
    'Public Default Property Prop1(t1 As Short) As Integer': Argument matching parameter 't1' narrows from 'Decimal' to 'Short'.]]>.Value.Replace(vbLf, Environment.NewLine)),
            Diagnostic(ERRID.ERR_NoNonNarrowingOverloadCandidates2, "tc3").WithArguments("Prop1", <![CDATA[
    'Public Overloads Default Property Prop1(y1 As Long) As Integer': Argument matching parameter 'y1' narrows from 'Decimal' to 'Long'.
    'Public Default Property Prop1(t1 As Short) As Integer': Argument matching parameter 't1' narrows from 'Decimal' to 'Short'.]]>.Value.Replace(vbLf, Environment.NewLine))
                )
        End Sub

        <Fact()>
        Public Sub BC30520ERR_ArgumentNarrowing3_RoslynBC30519()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ArgumentNarrowing3">
        <file name="a.vb">
    Option Strict Off
    Module Module1
        Class sample7C1(Of X)
            Enum E
                e1
                e2
                e3
            End Enum
        End Class
        Class sample7C2(Of T, Y)
            Public whichOne As String
            Sub Foo(ByVal p1 As sample7C1(Of T).E)
                whichOne = "1"
            End Sub
            Sub Foo(ByVal p1 As sample7C1(Of Y).E)
                whichOne = "2"
            End Sub
            Sub Scenario8(ByVal p1 As sample7C1(Of T).E)
                Call Me.Foo(p1)
            End Sub
        End Class
        Sub test()
            Dim tc7 As New sample7C2(Of Integer, Integer)
            Dim sc7 As New sample7C1(Of Byte)
             'COMPILEERROR: BC30520, "sample7C1(Of Long).E.e1"
            Call tc7.Foo (sample7C1(Of Long).E.e1)
            'COMPILEERROR: BC30520, "sample7C1(Of Short).E.e2"
            Call tc7.Foo (sample7C1(Of Short).E.e2)
            'COMPILEERROR: BC30520, "sc7.E.e3"
            Call tc7.Foo (sc7.E.e3)
        End Sub
    End Module
    </file>
    </compilation>)
            ' BC0000 - Test bug

            ' Roslyn BC30519 - Dev11 BC30520
            compilation.VerifyDiagnostics(
             Diagnostic(ERRID.ERR_NoNonNarrowingOverloadCandidates2, "Foo").WithArguments("Foo", <![CDATA[
    'Public Sub Foo(p1 As Module1.sample7C1(Of Integer).E)': Argument matching parameter 'p1' narrows from 'Module1.sample7C1(Of Long).E' to 'Module1.sample7C1(Of Integer).E'.
    'Public Sub Foo(p1 As Module1.sample7C1(Of Integer).E)': Argument matching parameter 'p1' narrows from 'Module1.sample7C1(Of Long).E' to 'Module1.sample7C1(Of Integer).E'.]]>.Value.Replace(vbLf, Environment.NewLine)),
             Diagnostic(ERRID.ERR_NoNonNarrowingOverloadCandidates2, "Foo").WithArguments("Foo", <![CDATA[
    'Public Sub Foo(p1 As Module1.sample7C1(Of Integer).E)': Argument matching parameter 'p1' narrows from 'Module1.sample7C1(Of Short).E' to 'Module1.sample7C1(Of Integer).E'.
    'Public Sub Foo(p1 As Module1.sample7C1(Of Integer).E)': Argument matching parameter 'p1' narrows from 'Module1.sample7C1(Of Short).E' to 'Module1.sample7C1(Of Integer).E'.]]>.Value.Replace(vbLf, Environment.NewLine)),
             Diagnostic(ERRID.WRN_SharedMemberThroughInstance, "sc7.E"),
             Diagnostic(ERRID.ERR_NoNonNarrowingOverloadCandidates2, "Foo").WithArguments("Foo", <![CDATA[
    'Public Sub Foo(p1 As Module1.sample7C1(Of Integer).E)': Argument matching parameter 'p1' narrows from 'Module1.sample7C1(Of Byte).E' to 'Module1.sample7C1(Of Integer).E'.
    'Public Sub Foo(p1 As Module1.sample7C1(Of Integer).E)': Argument matching parameter 'p1' narrows from 'Module1.sample7C1(Of Byte).E' to 'Module1.sample7C1(Of Integer).E'.]]>.Value.Replace(vbLf, Environment.NewLine))
                    )

            'CompilationUtils.AssertTheseErrors(compilation,
            '    <expected>
            'BC30520: Argument matching parameter 'p1' narrows from 'ConsoleApplication10.Module1.sample7C1(Of Long).E' to 'ConsoleApplication10.Module1.sample7C1(Of Integer).E'.	
            '            Call tc7.Foo (sample7C1(Of Long).E.e1)
            '                ~~~~
            'BC30520: Argument matching parameter 'p1' narrows from 'ConsoleApplication10.Module1.sample7C1(Of Short).E' to 'ConsoleApplication10.Module1.sample7C1(Of Integer).E'.	
            '            Call tc7.Foo (sample7C1(Of Short).E.e2)
            '                ~~~~
            'BC30520: Argument matching parameter 'p1' narrows from 'ConsoleApplication10.Module1.sample7C1(Of Byte).E' to 'ConsoleApplication10.Module1.sample7C1(Of Integer).E'.	
            '                Call tc7.Foo (sc7.E.e3)
            '                ~~~~
            '</expected>)
        End Sub

        <Fact()>
        Public Sub BC30521ERR_NoMostSpecificOverload2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoMostSpecificOverload2">
        <file name="a.vb">
            Module Module1
                Class C0(Of T)
                    Sub Foo(ByVal t1 As T)
                    End Sub
                End Class
                Class C1(Of T, Y)
                    Inherits C0(Of T)
                    Overloads Sub Foo(ByVal y1 As Y)
                    End Sub
                End Class
                Structure S1
                    Dim i As Integer
                End Structure
                Class C2
                    Public Shared Widening Operator CType(ByVal Arg As C2) As C1(Of Integer, Integer)
                        Return New C1(Of Integer, Integer)
                    End Operator
                    Public Shared Widening Operator CType(ByVal Arg As C2) As S1
                        Return New S1
                    End Operator
                End Class
                Sub test()
                    Dim C As New C1(Of S1, C1(Of Integer, Integer))
                    Call C.Foo(New C2)
                End Sub
            End Module
    </file>
    </compilation>)

            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_NoMostSpecificOverload2, "Foo").WithArguments("Foo", <![CDATA[
    'Public Overloads Sub Module1.C1(Of Module1.S1, Module1.C1(Of Integer, Integer)).Foo(y1 As Module1.C1(Of Integer, Integer))': Not most specific.
    'Public Sub Module1.C0(Of Module1.S1).Foo(t1 As Module1.S1)': Not most specific.]]>.Value.Replace(vbLf, Environment.NewLine))
                    )

        End Sub

        <Fact()>
        Public Sub BC30524ERR_NoGetProperty1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
        Class C
            WriteOnly Property P
                Set
                End Set
            End Property
            Shared WriteOnly Property Q
                Set
                End Set
            End Property
            Sub M()
                Dim o
                o = P
                o = Me.P
                o = Q
                o = C.Q
            End Sub
        End Class
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30524: Property 'P' is 'WriteOnly'.
                o = P
                    ~
BC30524: Property 'P' is 'WriteOnly'.
                o = Me.P
                    ~~~~
BC30524: Property 'Q' is 'WriteOnly'.
                o = Q
                    ~
BC30524: Property 'Q' is 'WriteOnly'.
                o = C.Q
                    ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30524ERR_NoGetProperty1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
        Interface I
            Default WriteOnly Property P(s As String)
        End Interface
        Structure S
            Default WriteOnly Property Q(s As String)
                Set(value)
                End Set
            End Property
        End Structure
        Class C
            Default WriteOnly Property R(s As String)
                Set(value)
                End Set
            End Property
            Shared Sub M(x As I, y As S, z As C)
                x!Q = x!R
                y!Q = y!R
                z!Q = z!R
            End Sub
        End Class
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30524: Property 'P' is 'WriteOnly'.
                x!Q = x!R
                      ~~~
BC30524: Property 'Q' is 'WriteOnly'.
                y!Q = y!R
                      ~~~
BC30524: Property 'R' is 'WriteOnly'.
                z!Q = z!R
                      ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30524ERR_NoGetProperty1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
        Module M1
            Sub Main()
                foo(p)
            End Sub
            WriteOnly Property p() As Single
                Set(ByVal Value As Single)
                End Set
            End Property
            Public Sub foo(ByRef x As Single)
            End Sub
        End Module
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30524: Property 'p' is 'WriteOnly'.
                foo(p)
                    ~
</expected>)
        End Sub

        <Fact(), WorkItem(6810, "DevDiv_Projects/Roslyn")>
        Public Sub BC30524ERR_NoGetProperty1_3()
            CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
        Class C
            WriteOnly Property P As Object()
                Set(value As Object())
                End Set
            End Property
            WriteOnly Property Q As System.Action(Of Object)
                Set(value As System.Action(Of Object))
                End Set
            End Property
            WriteOnly Property R As C
                Set(value As C)
                End Set
            End Property
            Default ReadOnly Property S(i As Integer)
                Get
                    Return Nothing
                End Get
            End Property
            Sub M()
                Dim o
                o = P()(1)
                o = Q()(2)
                o = R()(3)
                o = P(1)
                o = Q(2)
                o = R(3)
            End Sub
        End Class
        </file>
    </compilation>).VerifyDiagnostics(
        Diagnostic(ERRID.ERR_NoGetProperty1, "P()").WithArguments("P"),
        Diagnostic(ERRID.ERR_NoGetProperty1, "Q()").WithArguments("Q"),
        Diagnostic(ERRID.ERR_NoGetProperty1, "R()").WithArguments("R"),
        Diagnostic(ERRID.ERR_NoGetProperty1, "P").WithArguments("P"),
        Diagnostic(ERRID.ERR_NoGetProperty1, "Q").WithArguments("Q"),
        Diagnostic(ERRID.ERR_NoGetProperty1, "R").WithArguments("R"))

        End Sub

        ''' <summary>
        ''' Report BC30524 even in cases when the
        ''' expression will be ignored.
        ''' </summary>
        ''' <remarks></remarks>
        <Fact()>
        Public Sub BC30524ERR_NoGetProperty1_4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Class A
    Class B
        Friend Const F As Object = Nothing
    End Class
    Shared WriteOnly Property P As A
        Set(value As A)
        End Set
    End Property
    Shared ReadOnly Property Q As A
        Get
            Return Nothing
        End Get
    End Property
    Shared Sub M()
        Dim o As Object
        o = P.B.F
        o = Q.B.F
    End Sub
End Class
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30524: Property 'P' is 'WriteOnly'.
        o = P.B.F
            ~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        o = Q.B.F
            ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30526ERR_NoSetProperty1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
        Class C
            ReadOnly Property P
                Get
                    Return Nothing
                End Get
            End Property
            Shared ReadOnly Property Q
                Get
                    Return Nothing
                End Get
            End Property
            Sub M()
                P = Nothing
                Me.P = Nothing
                Q = Nothing
                C.Q = Nothing
            End Sub
        End Class
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30526: Property 'P' is 'ReadOnly'.
                P = Nothing
                ~~~~~~~~~~~
BC30526: Property 'P' is 'ReadOnly'.
                Me.P = Nothing
                ~~~~~~~~~~~~~~
BC30526: Property 'Q' is 'ReadOnly'.
                Q = Nothing
                ~~~~~~~~~~~
BC30526: Property 'Q' is 'ReadOnly'.
                C.Q = Nothing
                ~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30526ERR_NoSetProperty1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
        Interface I
            Default ReadOnly Property P(s As String)
        End Interface
        Structure S
            Default ReadOnly Property Q(s As String)
                Get
                    Return Nothing
                End Get
            End Property
        End Structure
        Class C
            Default ReadOnly Property R(s As String)
                Get
                    Return Nothing
                End Get
            End Property
            Shared Sub M(x As I, y As S, z As C)
                x!Q = x!R
                y!Q = y!R
                z!Q = z!R
            End Sub
        End Class
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30526: Property 'P' is 'ReadOnly'.
                x!Q = x!R
                ~~~~~~~~~
BC30526: Property 'Q' is 'ReadOnly'.
                y!Q = y!R
                ~~~~~~~~~
BC30526: Property 'R' is 'ReadOnly'.
                z!Q = z!R
                ~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30532ERR_DateToDoubleConversion()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DateToDoubleConversion">
        <file name="a.vb">
        Structure s1
            function foo() as double
                return #1/1/2000#
            End function
        End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30532: Conversion from 'Date' to 'Double' requires calling the 'Date.ToOADate' method.
                return #1/1/2000#
                       ~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30532ERR_DateToDoubleConversion_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DateToDoubleConversion">
        <file name="a.vb">
Imports System
Class C
    Shared Sub Main()
        For Each x As Double In New Date() {#12:00:00 AM#}
        Next
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30532: Conversion from 'Date' to 'Double' requires calling the 'Date.ToOADate' method.
        For Each x As Double In New Date() {#12:00:00 AM#}
                                ~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30533ERR_DoubleToDateConversion()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DoubleToDateConversion">
        <file name="a.vb">
        Structure s1
            Function foo() As Date
                Dim a As Double = 12
                Return a
            End Function
        End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30533: Conversion from 'Double' to 'Date' requires calling the 'Date.FromOADate' method.
                Return a
                       ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30542ERR_ZeroDivide()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="DateToDoubleConversion">
        <file name="a.vb">
                Module M1
                    'Const z = 0
                    Sub foo()
                        Dim s = 1 \ Nothing
                        Dim m = 1 \ 0
                        'Dim n = 1 \ z
                        If (1 \ 0 = 1) Then
                        End If
                    End Sub
                End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30542: Division by zero occurred while evaluating this expression.
                        Dim s = 1 \ Nothing
                                ~~~~~~~~~~~
BC30542: Division by zero occurred while evaluating this expression.
                        Dim m = 1 \ 0
                                ~~~~~
BC30542: Division by zero occurred while evaluating this expression.
                        If (1 \ 0 = 1) Then
                            ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30545ERR_PropertyAccessIgnored()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="PropertyAccessIgnored">
        <file name="a.vb">
        Class C
            Shared Property P
            ReadOnly Property Q
                Get
                    Return Nothing
                End Get
            End Property
            Property R(o)
                Get
                    Return Nothing
                End Get
                Set(value)
                End Set
            End Property
            Sub M(o)
                P
                M(P)
                C.P
                C.P = Nothing
                Q
                M(Q)
                Me.Q
                M(Me.Q)
                R(Nothing)
                R(Nothing) = Nothing
                Me.R(Nothing)
                M(Me.R(Nothing))
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30545: Property access must assign to the property or use its value.
                P
                ~
BC30545: Property access must assign to the property or use its value.
                C.P
                ~~~
BC30545: Property access must assign to the property or use its value.
                Q
                ~
BC30545: Property access must assign to the property or use its value.
                Me.Q
                ~~~~
BC30545: Property access must assign to the property or use its value.
                R(Nothing)
                ~~~~~~~~~~
BC30545: Property access must assign to the property or use its value.
                Me.R(Nothing)
                ~~~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(531311, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531311")>
        <Fact()>
        Public Sub BC30545ERR_PropertyAccessIgnored_Latebound()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="PropertyAccessIgnored">
        <file name="a.vb">
Structure s1
    Dim z As Integer
    Property foo(ByVal i As Integer)
        Get
            Return Nothing
        End Get
        Set(ByVal Value)
        End Set
    End Property

    Property foo(ByVal i As Double)
        Get
            Return Nothing
        End Get
        Set(ByVal Value)
        End Set
    End Property

    Sub goo()
        Dim o As Object = 1
        'COMPILEERROR: BC30545, "foo(o)"
        foo(o)
    End Sub
End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30545: Property access must assign to the property or use its value.
        foo(o)
        ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30547ERR_InterfaceNoDefault1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
        Namespace N
            Interface I
            End Interface
            Class C
                Shared Sub M(x As I)
                    N(x(0))
                    N(x!P)
                End Sub
                Shared Sub N(o As Object)
                End Sub
            End Class
        End Namespace
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30547: 'I' cannot be indexed because it has no default property.
                    N(x(0))
                      ~
BC30547: 'I' cannot be indexed because it has no default property.
                    N(x!P)
                      ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30554ERR_AmbiguousInUnnamedNamespace1()
            Dim Lib1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="App1">
        <file name="a.vb">
            Public Class C1
            End Class
        </file>
    </compilation>)
            Dim Lib2 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="App2">
        <file name="a.vb">
            Public Class C1
            End Class
        </file>
    </compilation>)
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="APP">
        <file name="a.vb">
            Imports System
            Module Module1
                Sub Main()
                    Dim obj = New C1()
                End Sub
            End Module
        </file>
    </compilation>)
            Dim ref1 = New VisualBasicCompilationReference(Lib1)
            Dim ref2 = New VisualBasicCompilationReference(Lib2)
            compilation1 = compilation1.AddReferences(ref1)
            compilation1 = compilation1.AddReferences(ref2)
            Dim expectedErrors1 = <errors>
BC30554: 'C1' is ambiguous.
                    Dim obj = New C1()
                                  ~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30555ERR_DefaultMemberNotProperty1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Imports System
Imports System.Collections.Generic
Class C
    Shared Sub M(a As Array, f As Func(Of Dictionary(Of Object, Object)))
        Dim o As Object
        o = Function()
                Return New Dictionary(Of String, String)
            End Function!a
        o = a!b
        o = f!c
        o = f()!d
    End Sub
End Class
        </file>
    </compilation>)
            ' For now, lambdas result in BC30491 which differs from Dev10.
            ' This should change once lambda support is complete.
            Dim expectedErrors1 = <errors>
BC30555: Default member of 'Function &lt;generated method&gt;() As Dictionary(Of String, String)' is not a property.
        o = Function()
            ~~~~~~~~~~~
BC30555: Default member of 'Array' is not a property.
        o = a!b
            ~
BC30555: Default member of 'Func(Of Dictionary(Of Object, Object))' is not a property.
        o = f!c
            ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30565ERR_ArrayInitializerTooFewDimensions()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ArrayInitializerTooFewDimensions">
        <file name="a.vb">
        Module Module1
            Sub test()
                Dim FixedRankArray_1(,) As Double
                'COMPILEERROR: BC30565, "(0", BC30198, ","
                FixedRankArray_1 = New Double(,) {(0.1), {2.4, 4.6}}
                Exit Sub
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30565: Array initializer has too few dimensions.
                FixedRankArray_1 = New Double(,) {(0.1), {2.4, 4.6}}
                                                  ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30566ERR_ArrayInitializerTooManyDimensions()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ArrayInitializerTooManyDimensions">
        <file name="a.vb">
        Module Module1
            Structure S1
                Public x As Long
                Public s As String
            End Structure
            Sub foo()
                Dim obj = New S1() {{1, "one"}}
                Exit Sub
            End Sub
        End Module
    </file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_ArrayInitializerTooManyDimensions, "{1, ""one""}"))

        End Sub

        ' Roslyn too many extra errors (last 4)
        <Fact()>
        Public Sub BC30566ERR_ArrayInitializerTooManyDimensions_1()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ArrayInitializerTooManyDimensions">
        <file name="a.vb">
        Module Module1
            Sub foo()
                Dim myArray As Integer(,) = New Integer(2, 1) {{{1, 2}, {3, 4}, {5, 6}}}
            End Sub
        End Module
    </file>
    </compilation>).VerifyDiagnostics(
    Diagnostic(ERRID.ERR_ArrayInitializerTooManyDimensions, "{1, 2}"),
    Diagnostic(ERRID.ERR_ArrayInitializerTooManyDimensions, "{3, 4}"),
    Diagnostic(ERRID.ERR_ArrayInitializerTooManyDimensions, "{5, 6}"),
    Diagnostic(ERRID.ERR_InitializerTooManyElements1, "{{1, 2}, {3, 4}, {5, 6}}").WithArguments("1"),
    Diagnostic(ERRID.ERR_InitializerTooFewElements1, "{{{1, 2}, {3, 4}, {5, 6}}}").WithArguments("2"))

        End Sub

        <Fact()>
        Public Sub BC30567ERR_InitializerTooFewElements1()
            CreateCompilationWithMscorlib40(
    <compilation name="InitializerTooFewElements1">
        <file name="a.vb">
        Class A
            Sub foo()
                Dim x = {{1, 2, 3}, {4, 5}}
            End Sub
        End Class
    </file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_InitializerTooFewElements1, "{4, 5}").WithArguments("1"))

        End Sub

        <Fact()>
        Public Sub BC30567ERR_InitializerTooFewElements1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InitializerTooFewElements1">
        <file name="a.vb">
        Class A
            Sub foo()
                Dim x127 As Object(,) = New System.Exception(,) {{New System.AccessViolationException, New System.ArgumentException}, {New System.Exception}}
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30567: Array initializer is missing 1 elements.
                Dim x127 As Object(,) = New System.Exception(,) {{New System.AccessViolationException, New System.ArgumentException}, {New System.Exception}}
                                                                                                                                      ~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30567ERR_InitializerTooFewElements1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Class C
    Private A() As Object = New Object(0) {}
    Private B As Object() = New Object(2) {}
    Private C As Object(,) = New Object(1, 0) {}
    Private D As Object(,) = New Object(1, 0) {{}, {2}}
    Private E As Object(,) = New Object(0, 2) {}
    Private F()() As Object = New Object(0)() {}
    Private G()() As Object = New Object(0)() {New Object(0) {}}
End Class
    </file>
    </compilation>)
            compilation.AssertTheseDiagnostics(
    <expected>
BC30567: Array initializer is missing 1 elements.
    Private D As Object(,) = New Object(1, 0) {{}, {2}}
                                               ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30568ERR_InitializerTooManyElements1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InitializerTooManyElements1">
        <file name="a.vb">
        Class A
            Sub foo()
                Dim x127 As Object(,) = New System.Exception(,) {{New System.AccessViolationException}, {New System.Exception, New System.ArgumentException}}
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30568: Array initializer has 1 too many elements.
                Dim x127 As Object(,) = New System.Exception(,) {{New System.AccessViolationException}, {New System.Exception, New System.ArgumentException}}
                                                                                                        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30569ERR_NewOnAbstractClass()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NewOnAbstractClass">
        <file name="a.vb">
        Class C1
            MustInherit Class C2
                Public foo As New C2()
            End Class
            Public foo As New C2()
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30569: 'New' cannot be used on a class that is declared 'MustInherit'.
                Public foo As New C2()
                              ~~~~~~~~
BC30569: 'New' cannot be used on a class that is declared 'MustInherit'.
            Public foo As New C2()
                          ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30574ERR_StrictDisallowsLateBinding()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="StrictDisallowsLateBinding">
        <file name="a.vb">
            Module Module1
                Dim bol As Boolean
                Class C1
                    Property Prop As Long
                End Class
                Sub foo()
                    Dim Obj As Object = New C1()
                    bol = Obj(1)
                    bol = Obj!P
                End Sub
            End Module
    </file>
    </compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30574: Option Strict On disallows late binding.
                    bol = Obj(1)
                          ~~~
BC30574: Option Strict On disallows late binding.
                    bol = Obj!P
                          ~~~~~
</expected>)

            compilation = compilation.WithOptions(compilation.Options.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42017: Late bound resolution; runtime errors could occur.
                    bol = Obj(1)
                          ~~~
BC42016: Implicit conversion from 'Object' to 'Boolean'.
                    bol = Obj(1)
                          ~~~~~~
BC42016: Implicit conversion from 'Object' to 'Boolean'.
                    bol = Obj!P
                          ~~~~~
BC42017: Late bound resolution; runtime errors could occur.
                    bol = Obj!P
                          ~~~~~
</expected>)
        End Sub

        <WorkItem(546763, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546763")>
        <Fact()>
        Public Sub BC30574ERR_StrictDisallowsLateBinding_16745()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Option Strict On
Option Infer On

Public Module Program
    Sub Main()
        Dim a As Object = Nothing
        a.DoSomething()
    End Sub
End Module
    </file>
    </compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30574: Option Strict On disallows late binding.
        a.DoSomething()
        ~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30574ERR_StrictDisallowsLateBinding1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="StrictDisallowsLateBinding">
        <file name="a.vb">
Imports System

Module Program

    Delegate Sub d1(ByRef x As Integer, y As Integer)

    Sub Main()
        Dim obj As Object = New cls1

        Dim o As d1 = AddressOf obj.foo

        Dim l As Integer = 0
        o(l, 2)

        Console.WriteLine(l)
    End Sub

    Class cls1
        Shared Sub foo(ByRef x As Integer, y As Integer)
            x = 42
            Console.WriteLine(x + y)
        End Sub
    End Class
End Module

    </file>
    </compilation>, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30574: Option Strict On disallows late binding.
        Dim o As d1 = AddressOf obj.foo
                                ~~~~~~~
</expected>)

            compilation = compilation.WithOptions(compilation.Options.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42017: Late bound resolution; runtime errors could occur.
        Dim o As d1 = AddressOf obj.foo
                                ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30577ERR_AddressOfOperandNotMethod()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="AddressOfOperandNotMethod">
        <file name="a.vb">
            Delegate Function MyDelegate()
            Module M1
                Enum MyEnum
                    One
                End Enum
                Sub Main()
                    Dim x As MyDelegate
                    Dim oEnum As MyEnum
                    x = AddressOf oEnum
                    x.Invoke()
                End Sub
            End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30577: 'AddressOf' operand must be the name of a method (without parentheses).
                    x = AddressOf oEnum
                                  ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30581ERR_AddressOfNotDelegate1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="AddressOfNotDelegate1">
        <file name="a.vb">
Module M
    Sub Main()
        Dim x = New Object()
        Dim f = AddressOf x.GetType
    End Sub
End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30581: 'AddressOf' expression cannot be converted to 'Object' because 'Object' is not a delegate type.
        Dim f = AddressOf x.GetType
                ~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30582ERR_SyncLockRequiresReferenceType1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="SyncLockRequiresReferenceType1">
        <file name="a.vb">
                Imports System
                Module M1
                    Class C
                        Private Shared count = 0
                        Sub IncrementCount()
                            Dim i As Integer
                            SyncLock i = 0
                                count = count + 1
                            End SyncLock
                        End Sub
                    End Class
                End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30582: 'SyncLock' operand cannot be of type 'Boolean' because 'Boolean' is not a reference type.
                            SyncLock i = 0
                                     ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30582ERR_SyncLockRequiresReferenceType1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="SyncLockRequiresReferenceType1">
        <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim S1_a As New Object()
        Dim S1_b As Integer? = 4
        Dim S1_c As Integer? = 41
        SyncLock If(False, S1_a, S1_a)
        End SyncLock
        SyncLock If(True, S1_b, S1_b)
        End SyncLock
        SyncLock If(False, S1_b, 1)
        End SyncLock
    End Sub
End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30582: 'SyncLock' operand cannot be of type 'Integer?' because 'Integer?' is not a reference type.
        SyncLock If(True, S1_b, S1_b)
                 ~~~~~~~~~~~~~~~~~~~~
BC30582: 'SyncLock' operand cannot be of type 'Integer?' because 'Integer?' is not a reference type.
        SyncLock If(False, S1_b, 1)
                 ~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30582ERR_SyncLockRequiresReferenceType1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="SyncLockRequiresReferenceType1">
        <file name="a.vb">
Interface I
End Interface
Class A
End Class
Class C
    Shared Sub M(Of T1, T2 As Class, T3 As Structure, T4 As New, T5 As I, T6 As A, T7 As U, U)(_1 As T1, _2 As T2, _3 As T3, _4 As T4, _5 As T5, _6 As T6, _7 As T7)
        SyncLock _1
        End SyncLock
        SyncLock _2
        End SyncLock
        SyncLock _3
        End SyncLock
        SyncLock _4
        End SyncLock
        SyncLock _5
        End SyncLock
        SyncLock _6
        End SyncLock
        SyncLock _7
        End SyncLock
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30582: 'SyncLock' operand cannot be of type 'T1' because 'T1' is not a reference type.
        SyncLock _1
                 ~~
BC30582: 'SyncLock' operand cannot be of type 'T3' because 'T3' is not a reference type.
        SyncLock _3
                 ~~
BC30582: 'SyncLock' operand cannot be of type 'T4' because 'T4' is not a reference type.
        SyncLock _4
                 ~~
BC30582: 'SyncLock' operand cannot be of type 'T5' because 'T5' is not a reference type.
        SyncLock _5
                 ~~
BC30582: 'SyncLock' operand cannot be of type 'T7' because 'T7' is not a reference type.
        SyncLock _7
                 ~~
</expected>)
        End Sub

        <Fact>
        Public Sub BC30587ERR_NamedParamArrayArgument()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NamedParamArrayArgument">
        <file name="a.vb">
                Class C1
                    Shared Sub Main()
                        Dim a As New C1
                        a.abc(s:=10)
                    End Sub
                    Sub abc(ByVal ParamArray s() As Integer)
                    End Sub
                End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30587: Named argument cannot match a ParamArray parameter.
                        a.abc(s:=10)
                              ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30588ERR_OmittedParamArrayArgument()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="OmittedParamArrayArgument">
        <file name="a.vb">
Imports System
Imports C1(Of String, Integer)
Class C1(Of T As {Class}, U As Structure)
    Public Shared Property Overloaded(ByVal ParamArray y() As Exception) As C2
        Get
            Return New C2
        End Get
        Set(ByVal value As C2)
        End Set
    End Property
End Class
Class C2
End Class
Module M1
    Sub FOO()
        Overloaded(, , ) = Nothing
    End Sub
End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30588: Omitted argument cannot match a ParamArray parameter.
        Overloaded(, , ) = Nothing
                   ~
BC30588: Omitted argument cannot match a ParamArray parameter.
        Overloaded(, , ) = Nothing
                     ~
BC30588: Omitted argument cannot match a ParamArray parameter.
        Overloaded(, , ) = Nothing
                       ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30611ERR_NegativeArraySize()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NegativeArraySize">
        <file name="a.vb">
        Class C1
            Sub foo()
                Dim x8(-2) As String
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30611: Array dimensions cannot have a negative size.
                Dim x8(-2) As String
                       ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30611ERR_NegativeArraySize_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NegativeArraySize">
        <file name="a.vb">
        Class C1
            Sub foo()
                Dim arr11 As Integer(,) = New Integer(-2, -2) {} ' Invalid
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30611: Array dimensions cannot have a negative size.
                Dim arr11 As Integer(,) = New Integer(-2, -2) {} ' Invalid
                                                      ~~
BC30611: Array dimensions cannot have a negative size.
                Dim arr11 As Integer(,) = New Integer(-2, -2) {} ' Invalid
                                                          ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30611ERR_NegativeArraySize_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NegativeArraySize">
        <file name="a.vb">
        Class C1
            Sub foo()
                Dim arr(0 To 0, 0 To -2) As Integer 'Invalid
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30611: Array dimensions cannot have a negative size.
                Dim arr(0 To 0, 0 To -2) As Integer 'Invalid
                                ~~~~~~~

</expected>)
        End Sub

        <Fact()>
        Public Sub BC30614ERR_MyClassAbstractCall1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BC30614ERR_MyClassAbstractCall1_1">
        <file name="a.vb">
        MustInherit Class C1
            Public Sub UseMyClass()
                MyClass.foo()
            End Sub
            MustOverride Sub foo()
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30614: 'MustOverride' method 'Public MustOverride Sub foo()' cannot be called with 'MyClass'.
                MyClass.foo()
                ~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30614ERR_MyClassAbstractCall1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BC30614ERR_MyClassAbstractCall1_2">
        <file name="a.vb">
        Public MustInherit Class Base1
            Public MustOverride Property P1()

            Public Sub M2()
                MyClass.P1 = MyClass.P1
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30614: 'MustOverride' method 'Public MustOverride Property P1 As Object' cannot be called with 'MyClass'.
                MyClass.P1 = MyClass.P1
                ~~~~~~~~~~
BC30614: 'MustOverride' method 'Public MustOverride Property P1 As Object' cannot be called with 'MyClass'.
                MyClass.P1 = MyClass.P1
                             ~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30614ERR_MyClassAbstractCall1_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BC30614ERR_MyClassAbstractCall1_3">
        <file name="a.vb">
Imports System
Public MustInherit Class Base1
    Public MustOverride Function F1() As Integer

    Public Sub M2()
        Dim _func As Func(Of Integer) = AddressOf MyClass.F1
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30614: 'MustOverride' method 'Public MustOverride Function F1() As Integer' cannot be called with 'MyClass'.
        Dim _func As Func(Of Integer) = AddressOf MyClass.F1
                                                  ~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30614ERR_MyClassAbstractCall1_4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BC30614ERR_MyClassAbstractCall1_4">
        <file name="a.vb">
Imports System
Public MustInherit Class Base1
    Public MustOverride Function F1() As Integer
    Public Function F2() As Integer
        Return 1
    End Function

    Public Sub M2()
        Dim _func As Func(Of Func(Of Integer)) = Function() New Func(Of Integer)(AddressOf MyClass.F1)
        Dim _func2 As Func(Of Func(Of Integer)) = Function() New Func(Of Integer)(AddressOf MyClass.F2)
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30614: 'MustOverride' method 'Public MustOverride Function F1() As Integer' cannot be called with 'MyClass'.
        Dim _func As Func(Of Func(Of Integer)) = Function() New Func(Of Integer)(AddressOf MyClass.F1)
                                                                                           ~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30614ERR_MyClassAbstractCall1_5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BC30614ERR_MyClassAbstractCall1_5">
        <file name="a.vb">
Imports System
Public MustInherit Class Base1
    Public MustOverride Function F1() As Integer
    Public Function F2() As Integer
        Return 1
    End Function

    Public FLD As Func(Of Func(Of Integer)) =
        Function() New Func(Of Integer)(AddressOf MyClass.F1)

    Public Property PROP As Func(Of Func(Of Integer)) =
        Function() New Func(Of Integer)(AddressOf MyClass.F1)

    Public FLD2 As Func(Of Func(Of Integer)) =
        Function() New Func(Of Integer)(AddressOf MyClass.F2)

    Public Property PROP2 As Func(Of Func(Of Integer)) =
        Function() New Func(Of Integer)(AddressOf MyClass.F2)

End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30614: 'MustOverride' method 'Public MustOverride Function F1() As Integer' cannot be called with 'MyClass'.
        Function() New Func(Of Integer)(AddressOf MyClass.F1)
                                                  ~~~~~~~~~~
BC30614: 'MustOverride' method 'Public MustOverride Function F1() As Integer' cannot be called with 'MyClass'.
        Function() New Func(Of Integer)(AddressOf MyClass.F1)
                                                  ~~~~~~~~~~
</expected>)
        End Sub

        ' Different error
        <Fact()>
        Public Sub BC30615ERR_EndDisallowedInDllProjects()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="EndDisallowedInDllProjects">
        <file name="a.vb">
        Class C1
            Function foo()
                End
        End Class
    </file>
    </compilation>, options:=TestOptions.ReleaseDll)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30027: 'End Function' expected.
            Function foo()
            ~~~~~~~~~~~~~~
BC30615: 'End' statement cannot be used in class library projects.
                End
                ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30616ERR_BlockLocalShadowing1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BlockLocalShadowing1">
        <file name="a.vb">
        Class C1
            Sub foo()
                dim s = 10
                if s>5
                    dim s = 5
                    if s > 7
                        dim s = 7
                    End If
                End If
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30616: Variable 's' hides a variable in an enclosing block.
                    dim s = 5
                        ~
BC30616: Variable 's' hides a variable in an enclosing block.
                        dim s = 7
                            ~
</expected>)
        End Sub

        ' spec changes in Roslyn
        <WorkItem(528680, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528680")>
        <Fact()>
        Public Sub BC30616ERR_BlockLocalShadowing1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BlockLocalShadowing1">
        <file name="a.vb">
Class C
    Private field As Integer = 0
    Private Property [property]() As Integer
        Get
            Return m_property
        End Get
        Set(value As Integer)
            m_property = value
        End Set
    End Property
    Property prop() As Integer
        Get
            Return 1
        End Get
        Set(ByVal Value As Integer)
            ' Was Dev10: COMPILEERROR: BC30616, "value"
            ' now Dev10: BC30734: 'value' is already declared as a parameter of this method.
            For Each value As Byte In New Byte() {1, 2, 3}
            Next
        End Set
    End Property
    Private m_property As Integer

    Shared Sub Main()
        Dim ints As Integer() = New Integer() {1, 2, 3}
        Dim strings As String() = New String() {"1", "2", "3"}
        Dim conflict As Integer = 1
        For Each field As Integer In ints
        Next
        For Each [property] As String In strings
        Next
        For Each conflict As String In strings
        Next
        Dim [qq] As Integer = 23
        'COMPILEERROR: BC30616, "qq"
        For Each qq As Integer In New Integer() {1, 2, 3}
        Next
        Dim ww As Integer = 23
        'COMPILEERROR: BC30616, "[ww]"
        For Each [ww] As Integer In New Integer() {1, 2, 3}
        Next
        For Each z As Integer In New Integer() {1, 2, 3}
            'COMPILEERROR: BC30616, "z"
            For Each z As Decimal In New Decimal() {1, 2, 3}
            Next
        Next
        For Each t As Long In New Long() {1, 2, 3}
            For Each u As Boolean In New Boolean() {False, True}
                'COMPILEERROR: BC30616, "t"
                Dim t As Integer
            Next
        Next
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30734: 'value' is already declared as a parameter of this method.
            For Each value As Byte In New Byte() {1, 2, 3}
                     ~~~~~
BC30616: Variable 'conflict' hides a variable in an enclosing block.
        For Each conflict As String In strings
                 ~~~~~~~~
BC30616: Variable 'qq' hides a variable in an enclosing block.
        For Each qq As Integer In New Integer() {1, 2, 3}
                 ~~
BC30616: Variable 'ww' hides a variable in an enclosing block.
        For Each [ww] As Integer In New Integer() {1, 2, 3}
                 ~~~~
BC30616: Variable 'z' hides a variable in an enclosing block.
            For Each z As Decimal In New Decimal() {1, 2, 3}
                     ~
BC30616: Variable 't' hides a variable in an enclosing block.
                Dim t As Integer
                    ~
BC42024: Unused local variable: 't'.
                Dim t As Integer
                    ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30616ERR_BlockLocalShadowing1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BlockLocalShadowing1">
        <file name="a.vb">
Option Strict Off
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        Dim var1 As Integer
        For var1 As Integer = 1 To 10
        Next
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42024: Unused local variable: 'var1'.
        Dim var1 As Integer
            ~~~~
BC30616: Variable 'var1' hides a variable in an enclosing block.
        For var1 As Integer = 1 To 10
            ~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30616ERR_BlockLocalShadowing1_4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BlockLocalShadowing1">
        <file name="a.vb">
Public Class MyClass1
    Public Shared Sub Main()
        Static var2 As Long
        For var2 As Short = 0 To 10
        Next

       var2 = 0
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30616: Variable 'var2' hides a variable in an enclosing block.
        For var2 As Short = 0 To 10
            ~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30616ERR_BlockLocalShadowing1_5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BlockLocalShadowing1">
        <file name="a.vb">
Public Class MyClass1
    Public Shared Sub Main()
        For varo As Integer = 0 To 10
            For varo As Integer = 0 To 10
            Next
        Next
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30616: Variable 'varo' hides a variable in an enclosing block.
            For varo As Integer = 0 To 10
                ~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30616ERR_BlockLocalShadowing1_6()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BlockLocalShadowing1">
        <file name="a.vb">
Public Class MyClass1
    Public Shared Sub Main()
        For varo As Integer = 0 To 10
            Dim [qqq] As Integer
            For qqq As Integer = 0 To 10
            Next
            Dim www As Integer
            For [www] As Integer = 0 To 10
            Next
        Next
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42024: Unused local variable: 'qqq'.
            Dim [qqq] As Integer
                ~~~~~
BC30616: Variable 'qqq' hides a variable in an enclosing block.
            For qqq As Integer = 0 To 10
                ~~~
BC42024: Unused local variable: 'www'.
            Dim www As Integer
                ~~~
BC30616: Variable 'www' hides a variable in an enclosing block.
            For [www] As Integer = 0 To 10
                ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30616ERR_BlockLocalShadowing1_7()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BlockLocalShadowing1">
        <file name="a.vb">
Public Class MyClass1
    Public Shared Sub Main()
        For x As Integer = 0 To 10
            Dim x As Integer
        Next
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30616: Variable 'x' hides a variable in an enclosing block.
            Dim x As Integer
                ~
BC42024: Unused local variable: 'x'.
            Dim x As Integer
                ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30616ERR_BlockLocalShadowing1_8()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BlockLocalShadowing1">
        <file name="a.vb">
Public Class MyClass1
    Public Shared Sub Main()
        For var1 As Integer = 0 To 10
            For var2 As Integer = 0 To 10
                Dim var1 As Integer
            Next
        Next
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30616: Variable 'var1' hides a variable in an enclosing block.
                Dim var1 As Integer
                    ~~~~
BC42024: Unused local variable: 'var1'.
                Dim var1 As Integer
                    ~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30616ERR_BlockLocalShadowing1_9()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DuplicateLocals1">
        <file name="a.vb">
Class C
    Shared Sub Main()
        For Each r As Integer In New Integer() {1, 2, 3}
            'Was COMPILEERROR: BC30288, "r" in Dev10
            'Now BC30616
            Dim r As Integer
        Next
    End Sub
End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30616: Variable 'r' hides a variable in an enclosing block.
            Dim r As Integer
                ~
BC42024: Unused local variable: 'r'.
            Dim r As Integer
                ~
     </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30647ERR_ReturnFromNonFunction()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ReturnFromNonFunction">
        <file name="a.vb">
        Structure S1
            shared sub foo()
                return  1
            end sub
        End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30647: 'Return' statement in a Sub or a Set cannot return a value.
                return  1
                ~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30654ERR_ReturnWithoutValue()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ReturnWithoutValue">
        <file name="a.vb">
        Structure S1
            shared Function foo 
                return  
            end Function
        End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30654: 'Return' statement in a Function, Get, or Operator must return a value.
                return  
                ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30657ERR_UnsupportedMethod1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UnsupportedMethod1">
        <file name="a.vb">
        Class C1
            Dim x As System.Threading.IOCompletionCallback
            Sub Sub1()
            End Sub
            Sub New()
                x = New System.Threading.IOCompletionCallback(AddressOf Sub1)
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30657: 'IOCompletionCallback' has a return type that is not supported or parameter types that are not supported.
                x = New System.Threading.IOCompletionCallback(AddressOf Sub1)
                                                              ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30658ERR_NoNonIndexProperty1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoNonIndexProperty1">
        <file name="a.vb">
        Option Explicit On
        Imports System
        Module M1
            Class MyAttr
                Inherits Attribute
                Public Property Prop(ByVal i As Integer) As Integer
                    Get
                        Return Nothing
                    End Get
                    Set(ByVal Value As Integer)
                    End Set
                End Property
            End Class
            &lt;MyAttr(Prop:=1)&gt;'BIND:"Prop"
            Class C1
            End Class
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30658: Property 'Prop' with no parameters cannot be found.
            &lt;MyAttr(Prop:=1)&gt;'BIND:"Prop"
                    ~~~~
</expected>)

            VerifyOperationTreeForTest(Of IdentifierNameSyntax)(compilation, "a.vb", <![CDATA[
IPropertyReferenceOperation: Property M1.MyAttr.Prop(i As System.Int32) As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: 'Prop')
  Instance Receiver: 
    null]]>.Value)
        End Sub

        <Fact()>
        Public Sub BC30659ERR_BadAttributePropertyType1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadAttributePropertyType1">
        <file name="a.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.Class, AllowMultiple:=True, Inherited:=True)>
Class MultiUseAttribute
    Inherits System.Attribute
    Public Sub New(ByVal Value As Integer)
    End Sub
End Class

<AttributeUsage(AttributeTargets.Class, Inherited:=True)>
Class SingleUseAttribute
    Inherits Attribute
    Property A() As Date
        Get
            Return Nothing
        End Get
        Set(value As Date)
        End Set
    End Property
    Public Sub New(ByVal Value As Integer)
    End Sub
End Class
<SingleUse(1, A:=1.1), MultiUse(1)>
Class Base
End Class
<SingleUse(0, A:=1.1), MultiUse(0)>
Class Derived
    Inherits Base
End Class
    ]]></file>
    </compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_BadAttributePropertyType1, "A").WithArguments("A"),
            Diagnostic(ERRID.ERR_DoubleToDateConversion, "1.1"),
            Diagnostic(ERRID.ERR_BadAttributePropertyType1, "A").WithArguments("A"),
            Diagnostic(ERRID.ERR_DoubleToDateConversion, "1.1")) ' BC30533: Dev10 NOT report

        End Sub

        <Fact()>
        Public Sub BC30661ERR_PropertyOrFieldNotDefined1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="PropertyOrFieldNotDefined1">
        <file name="a.vb"><![CDATA[
        Imports System
        <AttributeUsage(AttributeTargets.All)>
        Public Class GeneralAttribute
            Inherits Attribute
        End Class
        <General(NotExist:=10)>
        Class C1
        End Class
    ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_PropertyOrFieldNotDefined1, "NotExist").WithArguments("NotExist"))

        End Sub

        <Fact()>
        Public Sub BC30665ERR_CantThrowNonException()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
        Module M1
            Sub Foo()
                Throw (Nothing)
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30665: 'Throw' operand must derive from 'System.Exception'.
                Throw (Nothing)
                ~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30665ERR_CantThrowNonException_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Class A
End Class
Class C
    Shared Sub M1(Of T As System.Exception)(e As T)
        Throw e
    End Sub
    Shared Sub M2(Of T As {System.Exception, New})()
        Throw New T()
    End Sub
    Shared Sub M3(Of T As A)(a As T)
        Throw a
    End Sub
    Shared Sub M4(Of U As New)()
        Throw New U()
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30665: 'Throw' operand must derive from 'System.Exception'.
        Throw a
        ~~~~~~~
BC30665: 'Throw' operand must derive from 'System.Exception'.
        Throw New U()
        ~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30666ERR_MustBeInCatchToRethrow()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="MustBeInCatchToRethrow">
        <file name="a.vb">
        Imports System
        Class C1
            Sub foo()
                Try
                    Throw
                Catch ex As Exception
                End Try
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30666: 'Throw' statement cannot omit operand outside a 'Catch' statement or inside a 'Finally' statement.
                    Throw
                    ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30671ERR_InitWithMultipleDeclarators()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InitWithMultipleDeclarators">
        <file name="a.vb">
            Imports System

            Public Structure Class1
                implements IDisposable

                Public Sub Dispose() implements Idisposable.Dispose
                End Sub
            End Structure

            Public Class Class2
                Sub foo()
                    Dim a, b As Class1 = New Class1
                    a = nothing
                    b = nothing

                    Using c, d as Class1 = nothing
                    End Using
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30671: Explicit initialization is not permitted with multiple variables declared with a single type specifier.
                    Dim a, b As Class1 = New Class1
                        ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36011: 'Using' resource variable must have an explicit initialization.
                    Using c, d as Class1 = nothing
                          ~
BC30671: Explicit initialization is not permitted with multiple variables declared with a single type specifier.
                    Using c, d as Class1 = nothing
                          ~~~~~~~~~~~~~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30671ERR_InitWithMultipleDeclarators02()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
imports system

Class C1
    ' not so ok
    public i, j as integer = 23

    ' ok enough :)
    public k as integer,l as integer = 23


    Public shared Sub Main(args() as string)
    End sub
End Class
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC30671: Explicit initialization is not permitted with multiple variables declared with a single type specifier.
    public i, j as integer = 23
           ~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30672ERR_InitWithExplicitArraySizes()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InitWithExplicitArraySizes">
        <file name="a.vb">
        Structure myStruct1
            sub foo()
                Dim a6(,) As Integer
                Dim b6(5, 5) As Integer = a6
            end Sub
        End structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30672: Explicit initialization is not permitted for arrays declared with explicit bounds.
                Dim b6(5, 5) As Integer = a6
                    ~~~~~~~~
BC42104: Variable 'a6' is used before it has been assigned a value. A null reference exception could result at runtime.
                Dim b6(5, 5) As Integer = a6
                                          ~~
</expected>)
        End Sub

        <WorkItem(542258, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542258")>
        <Fact()>
        Public Sub BC30672ERR_InitWithExplicitArraySizes_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="InitWithExplicitArraySizes">
        <file name="a.vb">
        Class Cls1
            Public Arr(3) As Cls1 = New Cls1() {New Cls1}

            Sub foo
                Dim Arr(3) As Cls1 = New Cls1() {New Cls1}
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30672: Explicit initialization is not permitted for arrays declared with explicit bounds.
            Public Arr(3) As Cls1 = New Cls1() {New Cls1}
                   ~~~~~~
BC30672: Explicit initialization is not permitted for arrays declared with explicit bounds.
                Dim Arr(3) As Cls1 = New Cls1() {New Cls1}
                    ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30672ERR_InitWithExplicitArraySizes_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InitWithExplicitArraySizes">
        <file name="a.vb">
        Option Infer On
        Imports System
        Module Module1
            Sub Main()
                Dim arr14(1, 2) = New Double(1, 2) {} ' Invalid
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30672: Explicit initialization is not permitted for arrays declared with explicit bounds.
                Dim arr14(1, 2) = New Double(1, 2) {} ' Invalid
                    ~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30676ERR_NameNotEvent2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NameNotEvent2">
        <file name="a.vb">
Option Strict Off
Module M
    Sub Foo()
        Dim x As C1 = New C1
        AddHandler x.E, Sub() Console.WriteLine()
    End Sub
End Module
Class C1
    Public Dim E As String
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30676: 'E' is not an event of 'C1'.
        AddHandler x.E, Sub() Console.WriteLine()
                     ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30677ERR_AddOrRemoveHandlerEvent()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="AddOrRemoveHandlerEvent">
        <file name="a.vb">
        Module M
            Sub Main()
                AddHandler Nothing, Nothing
            End Sub
        End Module
    </file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_AddOrRemoveHandlerEvent, "Nothing"))

        End Sub

        <Fact(), WorkItem(918579, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/918579"), WorkItem(34, "CodePlex")>
        Public Sub BC30685ERR_AmbiguousAcrossInterfaces3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AmbiguousAcrossInterfaces3">
        <file name="a.vb">
        Interface A
            Sub fun(ByVal i As Integer)
        End Interface
        Interface AB
            Inherits A
            Shadows Sub fun(ByVal i As Integer)
        End Interface
        Interface AC
            Inherits A
            Shadows Sub fun(ByVal i As Integer)
        End Interface
        Interface ABS
            Inherits AB, AC
        End Interface
        Class Test
            Sub D(ByVal d As ABS)
                d.fun(2)
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30521: Overload resolution failed because no accessible 'fun' is most specific for these arguments:
    'Sub AB.fun(i As Integer)': Not most specific.
    'Sub AC.fun(i As Integer)': Not most specific.
                d.fun(2)
                  ~~~
    </expected>)
        End Sub

        <Fact()>
        Public Sub BC30686ERR_DefaultPropertyAmbiguousAcrossInterfaces4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="DefaultPropertyAmbiguousAcrossInterfaces4">
        <file name="a.vb">
Option Strict On
Interface IA(Of T)
    Default ReadOnly Property P(o As T) As Object
End Interface
Interface IB1(Of T)
    Inherits IA(Of T)
End Interface
Interface IB2(Of T)
    Inherits IA(Of T)
    Default Overloads ReadOnly Property P(x As T, y As T) As Object
End Interface
Interface IB3(Of T)
    Inherits IA(Of T)
    Default Overloads ReadOnly Property Q(x As Integer, y As Integer, z As Integer) As Object
End Interface
Interface IC1
    Inherits IA(Of String), IB1(Of String)
End Interface
Interface IC2
    Inherits IA(Of String), IB1(Of Object)
End Interface
Interface IC3
    Inherits IA(Of String), IB2(Of String)
End Interface
Interface IC4
    Inherits IA(Of String), IB3(Of String)
End Interface
Module M
    Sub M(c1 As IC1, c2 As IC2, c3 As IC3, c4 As IC4)
        Dim value As Object
        value = c1("")
        value = c2("")
        value = c3("")
        value = c4("")
    End Sub
End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC40007: Default property 'Q' conflicts with the default property 'P' in the base interface 'IA'. 'Q' will be the default property. 'Q' should be declared 'Shadows'.
    Default Overloads ReadOnly Property Q(x As Integer, y As Integer, z As Integer) As Object
                                        ~
BC30686: Default property access is ambiguous between the inherited interface members 'ReadOnly Default Property P(o As String) As Object' of interface 'IA(Of String)' and 'ReadOnly Default Property P(o As Object) As Object' of interface 'IA(Of Object)'.
        value = c2("")
                ~~
BC30686: Default property access is ambiguous between the inherited interface members 'ReadOnly Default Property P(o As String) As Object' of interface 'IA(Of String)' and 'ReadOnly Default Property P(x As String, y As String) As Object' of interface 'IB2(Of String)'.
        value = c3("")
                ~~
BC30686: Default property access is ambiguous between the inherited interface members 'ReadOnly Default Property P(o As String) As Object' of interface 'IA(Of String)' and 'ReadOnly Default Property Q(x As Integer, y As Integer, z As Integer) As Object' of interface 'IB3(Of String)'.
        value = c4("")
                ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30690ERR_StructureNoDefault1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
        Namespace N
            Structure S
            End Structure
        End Namespace
        Namespace M
            Class C
                Shared Sub M(x As N.S)
                    N(x(0))
                    N(x!P)
                End Sub
                Shared Sub N(o As Object)
                End Sub
            End Class
        End Namespace
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30690: Structure 'S' cannot be indexed because it has no default property.
                    N(x(0))
                      ~
BC30690: Structure 'S' cannot be indexed because it has no default property.
                    N(x!P)
                      ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30734ERR_LocalNamedSameAsParam1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="LocalNamedSameAsParam1">
        <file name="a.vb">
           Class cls0(Of G)
                Sub foo(Of T) (ByVal x As T)
                    Dim x As T
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30734: 'x' is already declared as a parameter of this method.
                    Dim x As T
                        ~
BC42024: Unused local variable: 'x'.
                    Dim x As T
                        ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30734ERR_LocalNamedSameAsParam1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BC30734ERR_LocalNamedSameAsParam1_2">
        <file name="a.vb">
Class C
    Private field As Integer = 0
    Private Property [property]() As Integer
        Get
            Return m_property
        End Get
        Set(value As Integer)
            m_property = value
        End Set
    End Property
    Property prop() As Integer
        Get
            Return 1
        End Get
        Set(ByVal Value As Integer)
            ' Was Dev10: COMPILEERROR: BC30616, "value"
            ' Now: BC30734
            For Each value As Byte In New Byte() {1, 2, 3}
            Next
        End Set
    End Property
    Private m_property As Integer

    Shared Sub Main()
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>    
BC30734: 'value' is already declared as a parameter of this method.
            For Each value As Byte In New Byte() {1, 2, 3}
                     ~~~~~
</expected>)
        End Sub

        <WorkItem(528680, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528680")>
        <Fact()>
        Public Sub BC30734ERR_LocalNamedSameAsParam1_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BlockLocalShadowing1">
        <file name="a.vb">
Public Class MyClass1
    Public Shared Sub Main()
    End Sub
    Sub foo(ByVal p1 As Integer)
        For p1 As Integer = 1 To 10
        Next
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30734: 'p1' is already declared as a parameter of this method.
        For p1 As Integer = 1 To 10
            ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30742ERR_CannotConvertValue2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
           Imports microsoft.visualbasic.strings
            Module M1
                    Sub foo()
                        Dim i As Integer
                        Dim c As Char
                        I% = Asc("")
                        i% = Asc("" + "" + "" + "" + "" + "" + "" + "" + "")
                        c = ChrW(65536)
                        c = ChrW(-68888)
                    End Sub
                End module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30742: Value '' cannot be converted to 'Integer'.
                        I% = Asc("")
                             ~~~~~~~
BC30742: Value '' cannot be converted to 'Integer'.
                        i% = Asc("" + "" + "" + "" + "" + "" + "" + "" + "")
                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30742: Value '65536' cannot be converted to 'Char'.
                        c = ChrW(65536)
                            ~~~~~~~~~~~
BC30742: Value '-68888' cannot be converted to 'Char'.
                        c = ChrW(-68888)
                            ~~~~~~~~~~~~
                                  </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30742ERR_CannotConvertValue2_2()
            Dim source =
<compilation>
    <file name="a.vb">
Option strict on
imports system
imports microsoft.visualbasic.strings
Imports System.Text

Class C1
    private const f1 as integer = Asc("") ' empty string
    private const f2 as integer = AscW("") ' empty string
    private const f3 as integer = Asc(CStr(nothing)) ' nothing string
    private const f4 as integer = AscW(CStr(nothing)) ' nothing string
    private const f5 as Char = ChrW(65536)

    Public shared Sub Main(args() as string)
    End sub
End Class
    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            AssertTheseDiagnostics(c1,
<expected>
BC30742: Value '' cannot be converted to 'Integer'.
    private const f1 as integer = Asc("") ' empty string
                                  ~~~~~~~
BC30742: Value '' cannot be converted to 'Integer'.
    private const f2 as integer = AscW("") ' empty string
                                  ~~~~~~~~
BC30742: Value '' cannot be converted to 'Integer'.
    private const f3 as integer = Asc(CStr(nothing)) ' nothing string
                                  ~~~~~~~~~~~~~~~~~~
BC30742: Value '' cannot be converted to 'Integer'.
    private const f4 as integer = AscW(CStr(nothing)) ' nothing string
                                  ~~~~~~~~~~~~~~~~~~~
BC30742: Value '65536' cannot be converted to 'Char'.
    private const f5 as Char = ChrW(65536)
                               ~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(574290, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/574290")>
        <Fact()>
        Public Sub BC30742ERR_PassVBNullToAsc()
            Dim source =
<compilation name="ExpressionContext">
    <file name="a.vb">
        Imports Microsoft.VisualBasic
Module Module1
Sub Main
Asc(vbnullstring)
End Sub
End MOdule
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_CannotConvertValue2, "Asc(vbnullstring)").WithArguments("", "Integer"))

        End Sub

        <Fact()>
        Public Sub BC30752ERR_OnErrorInSyncLock()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="OnErrorInSyncLock">
        <file name="a.vb">
            Imports System
            Class C
                Sub IncrementCount()
                    SyncLock GetType(C)
                        On Error GoTo 0
                    End SyncLock
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30752: 'On Error' statements are not valid within 'SyncLock' statements.
                        On Error GoTo 0
                        ~~~~~~~~~~~~~~~ 
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30753ERR_NarrowingConversionCollection2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NarrowingConversionCollection2">
        <file name="a.vb">
            option strict on
            Imports System
            class C1
                Function Main() As Microsoft.VisualBasic.Collection
                    'Dim collection As Microsoft.VisualBasic. = Nothing
                    Dim _collection As _Collection = Nothing
                    return _collection
                End function
            End Class
            Interface _Collection
            End Interface
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30753: Option Strict On disallows implicit conversions from '_Collection' to 'Collection'; the Visual Basic 6.0 collection type is not compatible with the .NET Framework collection type.
                    return _collection
                           ~~~~~~~~~~~
BC42322: Runtime errors might occur when converting '_Collection' to 'Collection'.
                    return _collection
                           ~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30754ERR_GotoIntoTryHandler()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="GotoIntoTryHandler">
        <file name="a.vb">
            Imports System
            Class C1
                Sub Main()
                    Do While (True)
                        GoTo LB1
                    Loop
                    Try
                    Catch ex As Exception
                    Finally
            LB1:
                    End Try
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30754: 'GoTo LB1' is not valid because 'LB1' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
                        GoTo LB1
                             ~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(543055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543055")>
        <Fact()>
        Public Sub BC30754ERR_GotoIntoTryHandler_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="GotoIntoTryHandler">
        <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Try
            GoTo label
            GoTo label5
        Catch ex As Exception
label:
        Finally
label5:
        End Try
    End Sub
End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30754: 'GoTo label' is not valid because 'label' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
            GoTo label
                 ~~~~~
BC30754: 'GoTo label5' is not valid because 'label5' is inside a 'Try', 'Catch' or 'Finally' statement that does not contain this statement.
            GoTo label5
                 ~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30755ERR_GotoIntoSyncLock()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="GotoIntoSyncLock">
        <file name="a.vb">
            Imports System
            Class C
                Sub IncrementCount()
                    SyncLock GetType(C)
            label:
                    End SyncLock
                    GoTo label
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30755: 'GoTo label' is not valid because 'label' is inside a 'SyncLock' statement that does not contain this statement.
                    GoTo label
                         ~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30756ERR_GotoIntoWith()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="GotoIntoWith">
        <file name="a.vb">
            Class C1
                Sub Main()
                    Dim s = New Type1()
                    With s
                        .x = 1
                        GoTo lab1
                    End With
                    With s
            lab1:
                        .x = 1
                    End With
                End Sub
            End Class
            Class Type1
                Public x As Short
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30756: 'GoTo lab1' is not valid because 'lab1' is inside a 'With' statement that does not contain this statement.
                        GoTo lab1
                             ~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30756ERR_GotoIntoWith_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="GotoIntoWith">
        <file name="a.vb">
            Class C1
                Function Main()
                    Dim s = New Type1()
                    With s
            lab1:
                        .x = 1
                    End With
                    GoTo lab1
                End Function
            End Class
            Class Type1
                Public x As Short
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30756: 'GoTo lab1' is not valid because 'lab1' is inside a 'With' statement that does not contain this statement.
                    GoTo lab1
                         ~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30757ERR_GotoIntoFor()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="GotoIntoFor">
        <file name="a.vb">
            Class C1
                Sub Main()
                    Dim s = New Type1()
                    With s
                        .x = 1
                        GoTo label1
                    End With
                    For i = 0 To 5
                        GoTo label1
label1:
                        Continue For
                    Next
                End Sub
            End Class
            Class Type1
                Public x As Short
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30757: 'GoTo label1' is not valid because 'label1' is inside a 'For' or 'For Each' statement that does not contain this statement.
                        GoTo label1
                             ~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30757ERR_GotoIntoFor_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="GotoIntoFor">
        <file name="a.vb">
            Class C1
                Function Main()
                    if (true)
                        GoTo label1
                    End If
                    For i as Integer = 0 To 5
                        GoTo label1
            label1:
                        Continue For
                    Next
                    return 1
                End Function
            End Class
            Class Type1
                Public x As Short
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30757: 'GoTo label1' is not valid because 'label1' is inside a 'For' or 'For Each' statement that does not contain this statement.
                        GoTo label1
                             ~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30757ERR_GotoIntoFor_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="GotoIntoFor">
        <file name="a.vb">
Option Infer On
Option Strict Off
Class C1
    Function Main()
        Dim s As Type1 = New Type1()
        If (True)
            GoTo label1
        End If
        For Each i In s
            GoTo label1
label1:
            Continue For
        Next
        Return 1
    End Function
End Class
Class Type1
    Public x As Short
End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30757: 'GoTo label1' is not valid because 'label1' is inside a 'For' or 'For Each' statement that does not contain this statement.
            GoTo label1
                 ~~~~~~
BC32023: Expression is of type 'Type1', which is not a collection type.
        For Each i In s
                      ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(540627, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540627")>
        <Fact()>
        Public Sub BC30758ERR_BadAttributeNonPublicConstructor()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadAttributeNonPublicConstructor">
        <file name="at30758.vb"><![CDATA[
Imports System
<AttributeUsage(AttributeTargets.All)>
Public MustInherit Class MyAttribute
    Inherits Attribute
    Friend Sub New()

    End Sub
End Class

<My()>
Class Foo
End Class
    ]]></file>
    </compilation>).VerifyDiagnostics(
    Diagnostic(ERRID.ERR_AttributeCannotBeAbstract, "My").WithArguments("MyAttribute"),
    Diagnostic(ERRID.ERR_BadAttributeNonPublicConstructor, "My"))
        End Sub

        <Fact()>
        Public Sub BC30782ERR_ContinueDoNotWithinDo()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ContinueDoNotWithinDo">
        <file name="a.vb">
            Class C1
                Sub Main()
                    While True
                        Continue Do
                    End While
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30782: 'Continue Do' can only appear inside a 'Do' statement.
                        Continue Do
                        ~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30783ERR_ContinueForNotWithinFor()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ContinueForNotWithinFor">
        <file name="a.vb">
            Class C1
                Sub Main()
                        Continue for
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30783: 'Continue For' can only appear inside a 'For' statement.
                        Continue for
                        ~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30784ERR_ContinueWhileNotWithinWhile()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ContinueWhileNotWithinWhile">
        <file name="a.vb">
            Class C1
                Sub Main()
                        Continue while
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30784: 'Continue While' can only appear inside a 'While' statement.
                        Continue while
                        ~~~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30793ERR_TryCastOfUnconstrainedTypeParam1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="TryCastOfUnconstrainedTypeParam1">
        <file name="a.vb">
            Module M1
                Sub Foo(Of T) (ByVal x As T)
                    Dim o As Object = TryCast(x, T)
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30793: 'TryCast' operands must be class-constrained type parameter, but 'T' has no class constraint.
                    Dim o As Object = TryCast(x, T)
                                                 ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30794ERR_AmbiguousDelegateBinding2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="AmbiguousDelegateBinding2">
        <file name="a.vb">
            Module Module1
                Public Delegate Sub Bar(ByVal x As Integer, ByVal y As Integer)
                Public Sub Foo(Of T, R)(ByVal x As T, ByVal y As R)
                End Sub
                Public Sub Foo(Of T)(ByVal x As T, ByVal y As T)
                End Sub
            End Module
            Class C1
                Sub FOO()
                    Dim x1 As Module1.Bar = AddressOf Module1.Foo
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30794: No accessible 'Foo' is most specific: 
    Public Sub Foo(Of Integer, Integer)(x As Integer, y As Integer)
    Public Sub Foo(Of Integer)(x As Integer, y As Integer)
                    Dim x1 As Module1.Bar = AddressOf Module1.Foo
                                                      ~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30917ERR_NoNonObsoleteConstructorOnBase3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NoNonObsoleteConstructorOnBase3">
        <file name="a.vb">
            Imports System
            Class C1
                &lt;Obsolete(Nothing, True)&gt;
                Sub New()
                End Sub
            End Class
            Class C2
                Inherits C1
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30917: Class 'C2' must declare a 'Sub New' because the 'Public Sub New()' in its base class 'C1' is marked obsolete.
            Class C2
                  ~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30918ERR_NoNonObsoleteConstructorOnBase4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NoNonObsoleteConstructorOnBase4">
        <file name="a.vb">
            Imports System
            Class C1
                &lt;Obsolete("hello", True)&gt;
                Sub New()
                End Sub
            End Class
            Class C2
                Inherits C1
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30918: Class 'C2' must declare a 'Sub New' because the 'Public Sub New()' in its base class 'C1' is marked obsolete: 'hello'.
            Class C2
                  ~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30919ERR_RequiredNonObsoleteNewCall3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="RequiredNonObsoleteNewCall3">
        <file name="a.vb">
            Imports System
            Class C1
                &lt;Obsolete(Nothing, True)&gt;
                Sub New()
                End Sub
            End Class
            Class C2
                Inherits C1
                Sub New()
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30919: First statement of this 'Sub New' must be an explicit call to 'MyBase.New' or 'MyClass.New' because the 'Public Sub New()' in the base class 'C1' of 'C2' is marked obsolete.
                Sub New()
                    ~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC30920ERR_RequiredNonObsoleteNewCall4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="RequiredNonObsoleteNewCall4">
        <file name="a.vb">
            Imports System
            Class C1
                &lt;Obsolete("hello", True)&gt;
                Sub New()
                End Sub
            End Class
            Class C2
                Inherits C1
                Sub New()
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30920: First statement of this 'Sub New' must be an explicit call to 'MyBase.New' or 'MyClass.New' because the 'Public Sub New()' in the base class 'C1' of 'C2' is marked obsolete: 'hello'.
                Sub New()
                    ~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(531309, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531309")>
        <Fact()>
        Public Sub BC30933ERR_LateBoundOverloadInterfaceCall1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="LateBoundOverloadInterfaceCall1">
        <file name="a.vb">
            Module m1
                Interface i1
                    Sub s1(ByVal p1 As Integer)
                    Sub s1(ByVal p1 As Double)
                End Interface
                Class c1
                    Implements i1
                    Public Overloads Sub s1(ByVal p1 As Integer) Implements i1.s1
                    End Sub
                    Public Overloads Sub s2(ByVal p1 As Double) Implements i1.s1
                    End Sub
                End Class
                Sub Main()
                    Dim refer As i1 = New c1
                    Dim o1 As Object = 3.1415
                    refer.s1(o1)
                End Sub
            End Module

            Module m2
                Interface i1
                    Property s1(ByVal p1 As Integer)
                    Property s1(ByVal p1 As Double)
                End Interface
                Class c1
                    Implements i1

                    Public Property s1(p1 As Double) As Object Implements i1.s1
                        Get
                            Return Nothing
                        End Get
                        Set(value As Object)

                        End Set
                    End Property

                    Public Property s1(p1 As Integer) As Object Implements i1.s1
                        Get
                            Return Nothing
                        End Get
                        Set(value As Object)

                        End Set
                    End Property
                End Class
                Sub Main()
                    Dim refer As i1 = New c1
                    Dim o1 As Object = 3.1415
                    refer.s1(o1) = 1
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30933: Late bound overload resolution cannot be applied to 's1' because the accessing instance is an interface type.
                    refer.s1(o1)
                          ~~
BC30933: Late bound overload resolution cannot be applied to 's1' because the accessing instance is an interface type.
                    refer.s1(o1) = 1
                          ~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30934ERR_RequiredAttributeConstConversion2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
<A(<a/>)>
Class A
    Inherits Attribute
    Public Sub New(o As Object)
    End Sub
End Class
        ]]></file>
    </compilation>, references:=XmlReferences)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_RequiredAttributeConstConversion2, "<a/>").WithArguments("System.Xml.Linq.XElement", "Object"))
        End Sub

        <Fact()>
        Public Sub BC30939ERR_AddressOfNotCreatableDelegate1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="AddressOfNotCreatableDelegate1">
        <file name="a.vb">
            Imports System
            Module M1
                Sub foo()
                    Dim x As [Delegate] = AddressOf main
                End Sub
                Sub main()
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30939: 'AddressOf' expression cannot be converted to '[Delegate]' because type '[Delegate]' is declared 'MustInherit' and cannot be created.
                    Dim x As [Delegate] = AddressOf main
                                          ~~~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(529157, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529157")>
        Public Sub BC30940ERR_ReturnFromEventMethod() 'diag behavior change by design- not worth investing in.
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ReturnFromEventMethod">
        <file name="a.vb">
            Class C1
                Delegate Sub EH()
                Custom Event e1 As EH
                    AddHandler(ByVal value As EH)
                        Return value
                    End AddHandler
                    RemoveHandler(ByVal value As EH)
                        Return value
                    End RemoveHandler
                    RaiseEvent()
                        Return value
                    End RaiseEvent
                End Event
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30647: 'Return' statement in a Sub or a Set cannot return a value.
                        Return value
                        ~~~~~~~~~~~~
BC30647: 'Return' statement in a Sub or a Set cannot return a value.
                        Return value
                        ~~~~~~~~~~~~
BC30647: 'Return' statement in a Sub or a Set cannot return a value.
                        Return value
                        ~~~~~~~~~~~~
BC30451: 'value' is not declared. It may be inaccessible due to its protection level.
                        Return value
                               ~~~~~
                                  </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(542270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542270")>
        <Fact()>
        Public Sub BC30949ERR_ArrayInitializerForNonConstDim()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ArrayInitializerForNonConstDim">
        <file name="a.vb">
            Option Strict On
            Module M
                Sub Main()
                    Dim x as integer = 1
                    Dim y as Object = New Integer(x) {1, 2}
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30949: Array initializer cannot be specified for a non constant dimension; use the empty initializer '{}'.
                    Dim y as Object = New Integer(x) {1, 2}
                                                     ~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(542270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542270")>
        <Fact()>
        Public Sub BC30949ERR_ArrayInitializerForNonConstDim_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ArrayInitializerForNonConstDim">
        <file name="a.vb">
            Option Strict On
            Imports System
            Class M1
                Public Shared Sub Main()
                    Dim myLength As Integer = 2
                    Dim arr As Integer(,) = New Integer(myLength - 1, 1) {{1, 2}, {3, 4}, {5, 6}}
                End Sub
                Private Class A
                    Private x As Integer = 1
                    Private arr As Integer(,) = New Integer(x, 1) {{1, 2}, {3, 4}, {5, 6}}
                End Class
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30949: Array initializer cannot be specified for a non constant dimension; use the empty initializer '{}'.
                    Dim arr As Integer(,) = New Integer(myLength - 1, 1) {{1, 2}, {3, 4}, {5, 6}}
                                                                         ~~~~~~~~~~~~~~~~~~~~~~~~
BC30949: Array initializer cannot be specified for a non constant dimension; use the empty initializer '{}'.
                    Private arr As Integer(,) = New Integer(x, 1) {{1, 2}, {3, 4}, {5, 6}}
                                                                  ~~~~~~~~~~~~~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30950ERR_DelegateBindingFailure3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="DelegateBindingFailure3">
        <file name="a.vb">
            Option Strict On
            Imports System

            Module M
                Dim f As Action(Of Object) = CType(AddressOf 1.ToString, Action(Of Object))
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
                                  </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30978ERR_IterationVariableShadowLocal1()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="IterationVariableShadowLocal1">
        <file name="a.vb">
Option Infer On
Option Strict Off
Imports System.Linq
Module M
    Sub Bar()
        Dim x = From bar In ""
    End Sub
    Function Foo()
        Dim x = From foo In ""
        Return Nothing
    End Function
End Module
        </file>
    </compilation>, {Net40.References.SystemCore})

            Dim expectedErrors1 = <errors>
BC30978: Range variable 'foo' hides a variable in an enclosing block or a range variable previously defined in the query expression.
        Dim x = From foo In ""
                     ~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30978ERR_IterationVariableShadowLocal1_1()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="IterationVariableShadowLocal1">
        <file name="a.vb">
            Option Infer Off
            Imports System
            Imports System.Linq
            Module Program
                Sub Main(args As String())
                    Dim arr As String() = New String() {"aaa", "bbb", "ccc"}
                    Dim arr_int As Integer() = New Integer() {111, 222, 333}
                    Dim x = 1
                    Dim s = If(True, (From x In arr Select x).ToList(), From y As Integer In arr_int Select y)
                End Sub
            End Module
        </file>
    </compilation>, {Net40.References.SystemCore}, options:=TestOptions.ReleaseExe)

            Dim expectedErrors1 = <errors>
BC30978: Range variable 'x' hides a variable in an enclosing block or a range variable previously defined in the query expression.
                    Dim s = If(True, (From x In arr Select x).ToList(), From y As Integer In arr_int Select y)
                                           ~
BC30978: Range variable 'x' hides a variable in an enclosing block or a range variable previously defined in the query expression.
                    Dim s = If(True, (From x In arr Select x).ToList(), From y As Integer In arr_int Select y)
                                                           ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30980ERR_CircularInference1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CircularInference2">
        <file name="a.vb">
            Module M
                Const x = 1
                Sub Main()
                    Dim x = Function() x
                End Sub
            End Module
        </file>
    </compilation>)
            ' Extra Warning in Roslyn
            compilation1.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_CircularInference1, "x").WithArguments("x")
                )

        End Sub

        <Fact()>
        Public Sub BC30980ERR_CircularInference1_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CircularInference2">
        <file name="a.vb">
Option Infer On

Imports System

Class C
    Shared Sub Main()
        Dim f As Func(Of Integer) = Function()
                                        For Each x In x + 1
                                            Return x
                                        Next
                                        return 0
                                    End Function
    End Sub
End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30980: Type of 'x' cannot be inferred from an expression containing 'x'.
                                        For Each x In x + 1
                                                      ~
BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.
                                        For Each x In x + 1
                                                      ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30980ERR_CircularInference2_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CircularInference2_2">
        <file name="a.vb">
Class C
    Shared Sub Main()
        For Each y In New Object() {New With {Key .y = y}}
        Next
    End Sub
End Class
        </file>
    </compilation>)

            Dim expectedErrors1 = <errors>
BC30980: Type of 'y' cannot be inferred from an expression containing 'y'.
        For Each y In New Object() {New With {Key .y = y}}
                                                       ~
BC42104: Variable 'y' is used before it has been assigned a value. A null reference exception could result at runtime.
        For Each y In New Object() {New With {Key .y = y}}
                                                       ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC30980ERR_CircularInference2_2a()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CircularInference2_2a">
        <file name="a.vb">
Class C
    Shared Sub Main()
        'For Each y As Object In New Object() {y}
        For Each y As Object In New Object() {New With {Key .y = y}}
        Next
    End Sub
End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42104: Variable 'y' is used before it has been assigned a value. A null reference exception could result at runtime.
        For Each y As Object In New Object() {New With {Key .y = y}}
                                                                 ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(542268, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542268")>
        <Fact()>
        Public Sub BC30980ERR_CircularInference2_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CircularInference2_3">
        <file name="a.vb">
Option Infer On
Class C
    Shared Sub Main()
        Dim a = a.b
    End Sub
End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30980: Type of 'a' cannot be inferred from an expression containing 'a'.
        Dim a = a.b
                ~
BC42104: Variable 'a' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim a = a.b
                ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(542268, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542268")>
        <Fact()>
        Public Sub BC30980ERR_CircularInference2_3a()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CircularInference2_3a">
        <file name="a.vb">
Option Infer Off
Class C
    Shared Sub Main()
        Dim a = a.ToString()
    End Sub
End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42104: Variable 'a' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim a = a.ToString()
                ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(542191, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542191")>
        Public Sub BC30982ERR_NoSuitableWidestType1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoSuitableWidestType1">
        <file name="a.vb">
            Option Infer On
            Module M
                Sub Main()
                    Dim stepVar = "1"c
                    For i = 1 To 10 Step stepVar
                    Next
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30982: Type of 'i' cannot be inferred because the loop bounds and the step clause do not convert to the same type.
                    For i = 1 To 10 Step stepVar
                        ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(12261, "DevDiv_Projects/Roslyn")>
        Public Sub BC30983ERR_AmbiguousWidestType3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="AmbiguousWidestType3">
    <file name="a.vb">
    Module modErr30983
        Sub Test()
            'COMPILEERROR : BC30983, "i" 
            For i = New first(1) To New second(2) Step New third(3)
            Next
        End Sub
    End Module
    Class base
    End Class
    Class first
        Inherits base
        Dim m_count As ULong
        Sub New(ByVal d As ULong)
            m_count = d
        End Sub
        Overloads Shared Widening Operator CType(ByVal d As first) As second
            Return New second(d.m_count)
        End Operator
    End Class
    Class second
        Inherits base
        Dim m_count As ULong
            Sub New(ByVal d As ULong)
            m_count = d
        End Sub
        Overloads Shared Widening Operator CType(ByVal d As second) As first
            Return New first(d.m_count)
        End Operator
    End Class
    Class third
        Inherits first
        Sub New(ByVal d As ULong)
            MyBase.New(d)
        End Sub
        Overloads Shared Widening Operator CType(ByVal d As third) As Integer
            Return 1
        End Operator
    End Class
    </file>
</compilation>)

            'BC30983: Type of 'i' is ambiguous because the loop bounds and the step clause do not convert to the same type.
            'For i = New first(1) To New second(2) Step New third(3)
            '    ~
            compilation.VerifyDiagnostics(Diagnostic(ERRID.ERR_AmbiguousWidestType3, "i").WithArguments("i"))

        End Sub

        <Fact()>
        Public Sub BC30989ERR_DuplicateAggrMemberInit1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="DuplicateAggrMemberInit1">
    <file name="a.vb">
            Module M
                Sub Main()
                     Dim cust = New Customer() With {.Name = "Bob", .Name = "Robert"}
                End Sub
            End Module
            Class Customer
                Property Name As String
            End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30989: Multiple initializations of 'Name'.  Fields and properties can be initialized only once in an object initializer expression.
                     Dim cust = New Customer() With {.Name = "Bob", .Name = "Robert"}
                                                                     ~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30990ERR_NonFieldPropertyAggrMemberInit1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="NonFieldPropertyAggrMemberInit1">
    <file name="a.vb">
            Module M1
                Class WithSubX
                    Public Sub x()
                    End Sub
                End Class
                Sub foo()
                    Dim z As WithSubX = New WithSubX With {.x = 5}
                End Sub
            End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30990: Member 'x' cannot be initialized in an object initializer expression because it is not a field or property.
                    Dim z As WithSubX = New WithSubX With {.x = 5}
                                                            ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30991ERR_SharedMemberAggrMemberInit1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SharedMemberAggrMemberInit1">
    <file name="a.vb">
            Module M
                Sub Main()
                    Dim cust As New Customer With {.totalCustomers = 21}
                End Sub
            End Module
            Public Class Customer
                Public Shared totalCustomers As Integer
            End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30991: Member 'totalCustomers' cannot be initialized in an object initializer expression because it is shared.
                    Dim cust As New Customer With {.totalCustomers = 21}
                                                    ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30992ERR_ParameterizedPropertyInAggrInit1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ParameterizedPropertyInAggrInit1">
    <file name="a.vb">
            Module M
                Sub Main()
                    Dim strs As New C1() With {.defaultProp = "One"}
                End Sub
            End Module
            Public Class C1
                Private myStrings() As String
                Default Property defaultProp(ByVal index As Integer) As String
                    Get
                        Return myStrings(index)
                    End Get
                    Set(ByVal Value As String)
                        myStrings(index) = Value
                    End Set
                End Property
            End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30992: Property 'defaultProp' cannot be initialized in an object initializer expression because it requires arguments.
                    Dim strs As New C1() With {.defaultProp = "One"}
                                                ~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30993ERR_NoZeroCountArgumentInitCandidates1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="NoZeroCountArgumentInitCandidates1">
    <file name="a.vb">
            Module M
                Sub Main()
                    Dim aCoinObject = nothing
                    Dim coinCollection As New C1 With {.Item = aCoinObject}
                End Sub
            End Module
            Class C1
                WriteOnly Property Item(ByVal Key As String) As Object
                    Set(ByVal value As Object)
                    End Set
                End Property
                WriteOnly Property Item(ByVal Index As Integer) As Object
                    Set(ByVal value As Object)
                    End Set
                End Property
                private WriteOnly Property Item(ByVal Index As Long) As Object
                    Set(ByVal value As Object)
                    End Set
                End Property
            End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30993: Property 'Item' cannot be initialized in an object initializer expression because all accessible overloads require arguments.
                    Dim coinCollection As New C1 With {.Item = aCoinObject}
                                                        ~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC30994ERR_AggrInitInvalidForObject()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="AggrInitInvalidForObject">
    <file name="a.vb">
            Module M
                Sub Main()
                    Dim obj = New Object With {.ToString = "hello"}
                End Sub
            End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30994: Object initializer syntax cannot be used to initialize an instance of 'System.Object'.
                    Dim obj = New Object With {.ToString = "hello"}
                                         ~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC31080ERR_ReferenceComparison3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="ReferenceComparison3">
    <file name="a.vb">
       Interface I1
            Interface I2
            End Interface
        End Interface
        Class C1
            Sub FOO()
                Dim scenario1 As I1.I2
                If scenario1 = Nothing Then
                End If
            End Sub
        End Class    
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42104: Variable 'scenario1' is used before it has been assigned a value. A null reference exception could result at runtime.
                If scenario1 = Nothing Then
                   ~~~~~~~~~
BC31080: Operator '=' is not defined for types 'I1.I2' and 'I1.I2'. Use 'Is' operator to compare two reference types.
                If scenario1 = Nothing Then
                   ~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC31082ERR_CatchVariableNotLocal1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CatchVariableNotLocal1">
    <file name="a.vb">
        Imports System
        Module M
            Dim ex As Exception
            Sub Main()
                Try
                Catch ex
                End Try
            End Sub
        End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31082: 'ex' is not a local variable or parameter, and so cannot be used as a 'Catch' variable.
                Catch ex
                      ~~
</expected>)
        End Sub

        <WorkItem(538613, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538613")>
        <Fact()>
        Public Sub BC30251_ModuleConstructorCall()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module M
  Sub [New]()
    M.New()
  End Sub
End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30251: Type 'M' has no constructors.
    M.New()
    ~~~~~
</expected>)
        End Sub

        <WorkItem(538613, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538613")>
        <Fact()>
        Public Sub BC30251_ModuleGenericConstructorCall()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module M
  Sub [New](Of T)()
    M.New(Of Integer)
  End Sub
End Module
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30251: Type 'M' has no constructors.
    M.New(Of Integer)
    ~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(570936, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/570936")>
        Public Sub BC31092ERR_ParamArrayWrongType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ParamArrayWrongType">
    <file name="a.vb">
        Module M1
            Sub Foo()
                    Dim x As New C1
                    Dim sResult As String = x.Foo(1, 2, 3, 4)
                End Sub
        End Module
        Class C1
            Function Foo(&lt;System.[ParamArray]()&gt; ByVal x As Integer) As String
                Return "He"
            End Function
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31092: ParamArray parameters must have an array type.
                    Dim sResult As String = x.Foo(1, 2, 3, 4)
                                              ~~~
</expected>)
        End Sub

        <Fact(), WorkItem(570936, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/570936")>
        Public Sub BC31092ERR_ParamArrayWrongType_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ParamArrayWrongType">
    <file name="a.vb">
        Module M1
            Sub Foo()
                    Dim x As New C1
                    Dim sResult As String = x.Foo(1)
                End Sub
        End Module
        Class C1
            Function Foo(&lt;System.[ParamArray]()&gt; ByVal x As Integer) As String
                Return "He"
            End Function
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31092: ParamArray parameters must have an array type.
                    Dim sResult As String = x.Foo(1)
                                              ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC31095ERR_InvalidMyClassReference()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC31095ERR_InvalidMyClassReference">
    <file name="a.vb">
        Class cls0
            Public s2 As String
        End Class
        Class Cls1
            Inherits cls0
            Sub New(ByVal x As Short)
            End Sub
            Sub New()
                'COMPILEERROR: BC31095, "MyClass"
                MyClass.New(MyClass.s2)
            End Sub
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31095: Reference to object under construction is not valid when calling another constructor.
                MyClass.New(MyClass.s2)
                            ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC31095ERR_InvalidMyBaseReference()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BC31095ERR_InvalidMyBaseReference">
    <file name="a.vb">
        Class cls0
            Public s2 As String
        End Class
        Class Cls1
            Inherits cls0
            Sub New(ByVal x As Short)
            End Sub
            Sub New()
                'COMPILEERROR: BC31095, "MyBase"
                MyClass.New(MyBase.s2)
            End Sub
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31095: Reference to object under construction is not valid when calling another constructor.
                MyClass.New(MyBase.s2)
                            ~~~~~~
</expected>)
        End Sub

        <WorkItem(541798, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541798")>
        <Fact()>
        Public Sub BC31095ERR_InvalidMeReference()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="InvalidMeReference">
    <file name="a.vb">
        Class cls0
            Public s2 As String
        End Class
        Class Cls1
            Inherits cls0

            Sub New(ByVal x As String)
            End Sub

            Sub New(ByVal x As Short)
                Me.New(Me.s2) 'COMPILEERROR: BC31095, "Me"
            End Sub
        End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31095: Reference to object under construction is not valid when calling another constructor.
                Me.New(Me.s2) 'COMPILEERROR: BC31095, "Me"
                       ~~
</expected>)
        End Sub

        <WorkItem(541799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541799")>
        <Fact()>
        Public Sub BC31096ERR_InvalidImplicitMeReference()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InvalidImplicitMeReference">
        <file name="a.vb">
        Imports System
        Module M1
            Class clsTest1
                Private strTest As String = "Hello"
                Sub New()
                    'COMPILEERROR: BC31096, "strTest"
                    Me.New(strTest)
                End Sub
                Sub New(ByVal ArgX As String)
                End Sub
            End Class
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC31096: Implicit reference to object under construction is not valid when calling another constructor.
                    Me.New(strTest)
                           ~~~~~~~
    </expected>)
        End Sub

        <Fact()>
        Public Sub BC31096ERR_InvalidImplicitMeReference_MyClass()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BC31096ERR_InvalidImplicitMeReference_MyClass">
        <file name="a.vb">
        Imports System
        Module M1
            Class clsTest1
                Private strTest As String = "Hello"
                Sub New()
                    'COMPILEERROR: BC31096, "strTest"
                    MyClass.New(strTest)
                End Sub
                Sub New(ByVal ArgX As String)
                End Sub
            End Class
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC31096: Implicit reference to object under construction is not valid when calling another constructor.
                    MyClass.New(strTest)
                                ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC31096ERR_InvalidImplicitMeReference_MyBase()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BC31096ERR_InvalidImplicitMeReference_MyBase">
        <file name="a.vb">
        Imports System
        Module M1
            Class clsTest0
                Public Sub New(ByVal strTest As String)
                End Sub
            End Class
            Class clsTest1
                Inherits clsTest0
                Private strTest As String = "Hello"
                Sub New()
                    'COMPILEERROR: BC31096, "strTest"
                    MyBase.New(strTest)
                End Sub
            End Class
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31096: Implicit reference to object under construction is not valid when calling another constructor.
                    MyBase.New(strTest)
                               ~~~~~~~
</expected>)
        End Sub

        ' Different error
        <Fact()>
        Public Sub BC31109ERR_InAccessibleCoClass3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="C">
        <file name="a.vb">
        Imports System.Runtime.InteropServices
        &lt;Assembly: ImportedFromTypeLib("NoPIANew1-PIA2.dll")&gt;
        &lt;Assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")&gt;
        Public Class Class1
            &lt;Guid("bd60d4b3-f50b-478b-8ef2-e777df99d810")&gt; _
            &lt;ComImport()&gt; _
            &lt;InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)&gt; _
            &lt;CoClass(GetType(FooImpl))&gt; _
            Public Interface IFoo
            End Interface
            &lt;Guid("c9dcf748-b634-4504-a7ce-348cf7c61891")&gt; _
            Friend Class FooImpl
            End Class
        End Class
    </file>
    </compilation>)
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
            <compilation name="InAccessibleCoClass3">
                <file name="a.vb">
        Public Module Module1
            Public Sub Main()
                Dim i1 As New Class1.IFoo(1)
                Dim i2 = New Class1.IFoo(Nothing)
            End Sub
        End Module
    </file>
            </compilation>)
            Dim compRef = New VisualBasicCompilationReference(compilation)
            compilation1 = compilation1.AddReferences(compRef)
            compilation1.VerifyDiagnostics(Diagnostic(ERRID.ERR_InAccessibleCoClass3, "New Class1.IFoo(1)").WithArguments("Class1.FooImpl", "Class1.IFoo", "Friend"),
                                            Diagnostic(ERRID.ERR_InAccessibleCoClass3, "New Class1.IFoo(Nothing)").WithArguments("Class1.FooImpl", "Class1.IFoo", "Friend"))
        End Sub

        <WorkItem(6977, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub BC31110ERR_MissingValuesForArraysInApplAttrs()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="MissingValuesForArraysInApplAttrs">
        <file name="a.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.All)>
Public Class MyAttribute
    Inherits Attribute
    Public Sub New(ByVal o As Object)
    End Sub
End Class

Namespace AttributeRegress003
    Friend Module AttributeRegress003mod
        'COMPILEERROR: BC31110, "{}"
        <My(New Integer(3) {})> Class Test
        End Class
    End Module
End Namespace
    ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_MissingValuesForArraysInApplAttrs, "{}"))

        End Sub

        <Fact()>
        Public Sub BC31102ERR_NoAccessibleSet()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NoAccessibleSet">
        <file name="a.vb">
        Class A
            Shared Property P
                Get
                    Return Nothing
                End Get
                Private Set
                End Set
            End Property
            Property Q
                Get
                    Return Nothing
                End Get
                Private Set
                End Set
            End Property
        End Class
        Class B
            Sub M(ByVal x As A)
                A.P = Nothing
                x.Q = Nothing
            End Sub
        End Class
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC31102: 'Set' accessor of property 'P' is not accessible.
                A.P = Nothing
                ~~~~~~~~~~~~~
BC31102: 'Set' accessor of property 'Q' is not accessible.
                x.Q = Nothing
                ~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC31103ERR_NoAccessibleGet()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NoAccessibleGet">
        <file name="a.vb">
        Class A
            Shared Property P
                Private Get
                    Return Nothing
                End Get
                Set
                End Set
            End Property
            Property Q
                Private Get
                    Return Nothing
                End Get
                Set
                End Set
            End Property
        End Class
        Class B
            Sub M(ByVal x As A)
                N(A.P)
                N(x.Q)
            End Sub
            Sub N(ByVal o As Object)
            End Sub
        End Class
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC31103: 'Get' accessor of property 'P' is not accessible.
                N(A.P)
                  ~~~
BC31103: 'Get' accessor of property 'Q' is not accessible.
                N(x.Q)
                  ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC31143ERR_DelegateBindingIncompatible2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DelegateBindingIncompatible2">
        <file name="a.vb">
        Public Class C1
            Delegate Function FunDel(ByVal i As Integer, ByVal d As Double) As Integer
            Function ExampleMethod1(ByVal m As Integer, ByVal aDate As Date) As Integer
                Return 1
            End Function
            Sub Main()
                Dim d1 As FunDel = AddressOf ExampleMethod1
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC31143: Method 'Public Function ExampleMethod1(m As Integer, aDate As Date) As Integer' does not have a signature compatible with delegate 'Delegate Function C1.FunDel(i As Integer, d As Double) As Integer'.
                Dim d1 As FunDel = AddressOf ExampleMethod1
                                             ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC31148ERR_UndefinedXmlPrefix()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Imports <xmlns:p0="http://roslyn/">
Module M
    Private F1 = GetXmlNamespace(p1)
    Private F2 = <%= GetXmlNamespace(p0) %>
    Private F3 = <%= GetXmlNamespace(p3) %>
    Private F4 = <p4:x xmlns:p4="http://roslyn/"><%= GetXmlNamespace(p4) %></p4:x>
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31148: XML namespace prefix 'p1' is not defined.
    Private F1 = GetXmlNamespace(p1)
                                 ~~
BC31172: An embedded expression cannot be used here.
    Private F2 = <%= GetXmlNamespace(p0) %>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31172: An embedded expression cannot be used here.
    Private F3 = <%= GetXmlNamespace(p3) %>
                 ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31148: XML namespace prefix 'p3' is not defined.
    Private F3 = <%= GetXmlNamespace(p3) %>
                                     ~~
BC31148: XML namespace prefix 'p4' is not defined.
    Private F4 = <p4:x xmlns:p4="http://roslyn/"><%= GetXmlNamespace(p4) %></p4:x>
                                                                     ~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31148ERR_UndefinedXmlPrefix_1()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System.Xml.Linq
Class C
    Private F1 As XElement = <p1:a q1:b="c" xmlns:p1="..." xmlns:q1="..."/>
    Private F2 As XElement = <p2:a q2:b="c"><b xmlns:p2="..." xmlns:q2="..."/></p2:a>
    Private F3 As String = <p3:a q3:b="c" xmlns:p3="..." xmlns:q3="..."/>.<p3:a>.@q3:b
End Class
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31148: XML namespace prefix 'p2' is not defined.
    Private F2 As XElement = <p2:a q2:b="c"><b xmlns:p2="..." xmlns:q2="..."/></p2:a>
                              ~~
BC31148: XML namespace prefix 'q2' is not defined.
    Private F2 As XElement = <p2:a q2:b="c"><b xmlns:p2="..." xmlns:q2="..."/></p2:a>
                                   ~~
BC31148: XML namespace prefix 'p3' is not defined.
    Private F3 As String = <p3:a q3:b="c" xmlns:p3="..." xmlns:q3="..."/>.<p3:a>.@q3:b
                                                                           ~~
BC31148: XML namespace prefix 'q3' is not defined.
    Private F3 As String = <p3:a q3:b="c" xmlns:p3="..." xmlns:q3="..."/>.<p3:a>.@q3:b
                                                                                  ~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31148ERR_UndefinedXmlPrefix_2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Imports <xmlns:p0="...">
Module M
    Private F = <x1 xmlns:p1="...">
                    <x2 xmlns:p2="..." p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="...">
                        <%=
                            <x3>
                                <x4 p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="...">
                                    <%=
                                        <x5>
                                            <x6 xmlns:p6="..." p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="...">
                                                <%=
                                                    <x7 xmlns:p7="...">
                                                        <x8 xmlns:p8="...">
                                                            <x9 p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="..."/>
                                                        </x8>
                                                    </x7>
                                                %>
                                            </x6>
                                        </x5>
                                    %>
                                </x4>
                            </x3>
                        %>
                    </x2>
                </x1>
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31148: XML namespace prefix 'p6' is not defined.
                    <x2 xmlns:p2="..." p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="...">
                                                                           ~~
BC31148: XML namespace prefix 'p7' is not defined.
                    <x2 xmlns:p2="..." p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="...">
                                                                                       ~~
BC31148: XML namespace prefix 'p8' is not defined.
                    <x2 xmlns:p2="..." p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="...">
                                                                                                   ~~
BC31148: XML namespace prefix 'p1' is not defined.
                                <x4 p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="...">
                                                ~~
BC31148: XML namespace prefix 'p2' is not defined.
                                <x4 p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="...">
                                                            ~~
BC31148: XML namespace prefix 'p6' is not defined.
                                <x4 p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="...">
                                                                        ~~
BC31148: XML namespace prefix 'p7' is not defined.
                                <x4 p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="...">
                                                                                    ~~
BC31148: XML namespace prefix 'p8' is not defined.
                                <x4 p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="...">
                                                                                                ~~
BC31148: XML namespace prefix 'p1' is not defined.
                                            <x6 xmlns:p6="..." p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="...">
                                                                           ~~
BC31148: XML namespace prefix 'p2' is not defined.
                                            <x6 xmlns:p6="..." p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="...">
                                                                                       ~~
BC31148: XML namespace prefix 'p7' is not defined.
                                            <x6 xmlns:p6="..." p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="...">
                                                                                                               ~~
BC31148: XML namespace prefix 'p8' is not defined.
                                            <x6 xmlns:p6="..." p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="...">
                                                                                                                           ~~
BC31148: XML namespace prefix 'p1' is not defined.
                                                            <x9 p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="..."/>
                                                                            ~~
BC31148: XML namespace prefix 'p2' is not defined.
                                                            <x9 p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="..."/>
                                                                                        ~~
BC31148: XML namespace prefix 'p6' is not defined.
                                                            <x9 p0:a0="..." p1:a1="..." p2:a2="..." p6:a6="..." p7:a7="..." p8:a8="..."/>
                                                                                                    ~~
]]></errors>)
        End Sub

        <WorkItem(531633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531633")>
        <Fact()>
        Public Sub BC31148ERR_UndefinedXmlPrefix_3()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Module M
    Sub Main()
        Dim x As Object
        x = <x/>.@Return:a
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31148: XML namespace prefix 'Return' is not defined.
        x = <x/>.@Return:a
                  ~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31149ERR_DuplicateXmlAttribute()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Module M
    Private F1 = <x a="b" a="c" A="d" A="e"/>
    Private F2 = <x a="b" a="c" a="d" xmlns="http://roslyn"/>
    Private F3 = <x p:a="b" p:a="c" xmlns:p="http://roslyn"/>
    Private F4 = <x xmlns:a="b" xmlns:a="c"/>
    Private F5 = <x p:a="b" q:a="c" xmlns:p="http://roslyn" xmlns:q="http://roslyn"/>
    Private F6 = <x p:a="b" P:a="c" xmlns:p="http://roslyn/p" xmlns:P="http://roslyn/P"/>
    Private F7 = <x a="b" <%= "a" %>="c" <%= "a" %>="d"/>
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31149: Duplicate XML attribute 'a'.
    Private F1 = <x a="b" a="c" A="d" A="e"/>
                          ~
BC31149: Duplicate XML attribute 'A'.
    Private F1 = <x a="b" a="c" A="d" A="e"/>
                                      ~
BC31149: Duplicate XML attribute 'a'.
    Private F2 = <x a="b" a="c" a="d" xmlns="http://roslyn"/>
                          ~
BC31149: Duplicate XML attribute 'a'.
    Private F2 = <x a="b" a="c" a="d" xmlns="http://roslyn"/>
                                ~
BC31149: Duplicate XML attribute 'p:a'.
    Private F3 = <x p:a="b" p:a="c" xmlns:p="http://roslyn"/>
                            ~~~
BC31149: Duplicate XML attribute 'xmlns:a'.
    Private F4 = <x xmlns:a="b" xmlns:a="c"/>
                                ~~~~~~~
BC31149: Duplicate XML attribute 'q:a'.
    Private F5 = <x p:a="b" q:a="c" xmlns:p="http://roslyn" xmlns:q="http://roslyn"/>
                            ~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31149ERR_DuplicateXmlAttribute_1()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports <xmlns="http://roslyn/">
Imports <xmlns:p="http://roslyn/">
Imports <xmlns:q="">
Class C
    Private Shared F1 As Object = <x a="b" a="c"/>
    Private Shared F2 As Object = <x p:a="b" a="c"/>
    Private Shared F3 As Object = <x q:a="b" a="c"/>
End Class
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31149: Duplicate XML attribute 'a'.
    Private Shared F1 As Object = <x a="b" a="c"/>
                                           ~
BC31149: Duplicate XML attribute 'a'.
    Private Shared F3 As Object = <x q:a="b" a="c"/>
                                             ~
]]></errors>)
        End Sub

        ' Should report duplicate xmlns attributes, even for xmlns
        ' attributes that match Imports since those have special handling.
        <Fact()>
        Public Sub BC31149ERR_DuplicateXmlAttribute_2()
            Dim options = TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse({"<xmlns:p=""http://roslyn/p"">"}))
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports <xmlns:q="http://roslyn/q">
Module M
    Private F1 As Object = <x xmlns:p="http://roslyn/p" xmlns:p="http://roslyn/other"/>
    Private F2 As Object = <x xmlns:q="http://roslyn/other" xmlns:q="http://roslyn/q"/>
End Module
    ]]></file>
</compilation>, references:=XmlReferences, options:=options)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31149: Duplicate XML attribute 'xmlns:p'.
    Private F1 As Object = <x xmlns:p="http://roslyn/p" xmlns:p="http://roslyn/other"/>
                                                        ~~~~~~~
BC31149: Duplicate XML attribute 'xmlns:q'.
    Private F2 As Object = <x xmlns:q="http://roslyn/other" xmlns:q="http://roslyn/q"/>
                                                            ~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31152ERR_ReservedXmlPrefix()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Imports <xmlns="http://roslyn/">
Imports <xmlns:xml="http://roslyn/xml">
Imports <xmlns:xmlns="http://roslyn/xmlns">
Imports <xmlns:Xml="http://roslyn/xml">
Imports <xmlns:Xmlns="http://roslyn/xmlns">
Module M
    Private F1 As Object = <x
                               xmlns="http://roslyn/"
                               xmlns:xml="http://roslyn/xml"
                               xmlns:xmlns="http://roslyn/xmlns"/>
    Private F2 As Object = <x
                               xmlns:XML="http://roslyn/xml"
                               xmlns:XMLNS="http://roslyn/xmlns"/>
    Private F3 As Object = <x
                               xmlns=""
                               xmlns:xml=""
                               xmlns:xmlns=""/>
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31152: XML namespace prefix 'xml' is reserved for use by XML and the namespace URI cannot be changed.
Imports <xmlns:xml="http://roslyn/xml">
               ~~~
BC31152: XML namespace prefix 'xmlns' is reserved for use by XML and the namespace URI cannot be changed.
Imports <xmlns:xmlns="http://roslyn/xmlns">
               ~~~~~
BC31152: XML namespace prefix 'xml' is reserved for use by XML and the namespace URI cannot be changed.
                               xmlns:xml="http://roslyn/xml"
                                     ~~~
BC31152: XML namespace prefix 'xmlns' is reserved for use by XML and the namespace URI cannot be changed.
                               xmlns:xmlns="http://roslyn/xmlns"/>
                                     ~~~~~
BC31152: XML namespace prefix 'xml' is reserved for use by XML and the namespace URI cannot be changed.
                               xmlns:xml=""
                                     ~~~
BC31152: XML namespace prefix 'xmlns' is reserved for use by XML and the namespace URI cannot be changed.
                               xmlns:xmlns=""/>
                                     ~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31152ERR_ReservedXmlPrefix_1()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Imports <xmlns:xml="http://www.w3.org/XML/1998/namespace">
Imports <xmlns:xmlns="http://www.w3.org/XML/1998/namespace">
Module M
    Private F1 As Object = <x
                               xmlns:xml="http://www.w3.org/2000/xmlns/"
                               xmlns:xmlns="http://www.w3.org/2000/xmlns/"/>
    Private F2 As Object = <x
                               xmlns:xml="http://www.w3.org/XML/1998/NAMESPACE"/>
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31152: XML namespace prefix 'xmlns' is reserved for use by XML and the namespace URI cannot be changed.
Imports <xmlns:xmlns="http://www.w3.org/XML/1998/namespace">
               ~~~~~
BC31152: XML namespace prefix 'xml' is reserved for use by XML and the namespace URI cannot be changed.
                               xmlns:xml="http://www.w3.org/2000/xmlns/"
                                     ~~~
BC31152: XML namespace prefix 'xmlns' is reserved for use by XML and the namespace URI cannot be changed.
                               xmlns:xmlns="http://www.w3.org/2000/xmlns/"/>
                                     ~~~~~
BC31152: XML namespace prefix 'xml' is reserved for use by XML and the namespace URI cannot be changed.
                               xmlns:xml="http://www.w3.org/XML/1998/NAMESPACE"/>
                                     ~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31168ERR_NoXmlAxesLateBinding()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Module M1
    Sub M()
        Dim a = Nothing
        Dim b As Object = Nothing
        Dim c As Object
        c = a.<x>
        c = b.@a
    End Sub
End Module
    ]]></file>
    <file name="b.vb"><![CDATA[
Option Strict On
Module M2
    Sub M()
        Dim a As Object = Nothing
        Dim b = Nothing
        Dim c As Object
        c = a...<x>
        c = b.@<a>
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31168: XML axis properties do not support late binding.
        c = a.<x>
            ~~~~~
BC31168: XML axis properties do not support late binding.
        c = b.@a
            ~~~~
BC31168: XML axis properties do not support late binding.
        c = a...<x>
            ~~~~~~~
BC31168: XML axis properties do not support late binding.
        c = b.@<a>
            ~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31172ERR_EmbeddedExpression()
            Dim options = TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse({"<xmlns:p=<%= M1.F %>>"}))
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Imports <xmlns:q=<%= M1.F %>>
Module M1
    Private F = "..."
End Module
Module M2
    Public F = <x xmlns=<%= M1.F %>/>
End Module
    ]]></file>
</compilation>, references:=XmlReferences, options:=options)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31172: Error in project-level import '<xmlns:p=<%= M1.F %>>' at '<%= M1.F %>' : An embedded expression cannot be used here.
BC31172: An embedded expression cannot be used here.
Imports <xmlns:q=<%= M1.F %>>
                 ~~~~~~~~~~~
BC31172: An embedded expression cannot be used here.
    Public F = <x xmlns=<%= M1.F %>/>
                        ~~~~~~~~~~~
BC30389: 'M1.F' is not accessible in this context because it is 'Private'.
    Public F = <x xmlns=<%= M1.F %>/>
                            ~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31183ERR_ReservedXmlNamespace()
            Dim options = TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse({"<xmlns=""http://www.w3.org/XML/1998/namespace"">", "<xmlns:p1=""http://www.w3.org/2000/xmlns/"">"}))
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Imports <xmlns="http://www.w3.org/XML/1998/namespace">
Imports <xmlns:p2="http://www.w3.org/2000/xmlns/">
Module M
    Private F1 As Object = <x
                               xmlns="http://www.w3.org/2000/xmlns/"
                               xmlns:p3="http://www.w3.org/XML/1998/namespace"/>
    Private F2 As Object = <x
                               xmlns="http://www.w3.org/2000/XMLNS/"
                               xmlns:p4="http://www.w3.org/XML/1998/NAMESPACE"/>
End Module
    ]]></file>
</compilation>, references:=XmlReferences, options:=options)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31183: Error in project-level import '<xmlns:p1="http://www.w3.org/2000/xmlns/">' at '"http://www.w3.org/2000/xmlns/"' : Prefix 'p1' cannot be bound to namespace name reserved for 'xmlns'.
BC31183: Error in project-level import '<xmlns="http://www.w3.org/XML/1998/namespace">' at '"http://www.w3.org/XML/1998/namespace"' : Prefix '' cannot be bound to namespace name reserved for 'xml'.
BC31183: Prefix '' cannot be bound to namespace name reserved for 'xml'.
Imports <xmlns="http://www.w3.org/XML/1998/namespace">
               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31183: Prefix 'p2' cannot be bound to namespace name reserved for 'xmlns'.
Imports <xmlns:p2="http://www.w3.org/2000/xmlns/">
                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31183: Prefix '' cannot be bound to namespace name reserved for 'xmlns'.
                               xmlns="http://www.w3.org/2000/xmlns/"
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31183: Prefix 'p3' cannot be bound to namespace name reserved for 'xml'.
                               xmlns:p3="http://www.w3.org/XML/1998/namespace"/>
                                        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31184ERR_IllegalDefaultNamespace()
            Dim options = TestOptions.ReleaseDll.WithGlobalImports(GlobalImport.Parse({"<xmlns="""">", "<xmlns:p="""">"}))
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Imports <xmlns="">
Imports <xmlns:q="">
Module M
    Private F As Object = <x xmlns="" xmlns:r=""/>
End Module
    ]]></file>
</compilation>, references:=XmlReferences, options:=options)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31184: Namespace declaration with prefix cannot have an empty value inside an XML literal.
    Private F As Object = <x xmlns="" xmlns:r=""/>
                                            ~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31189ERR_IllegalXmlnsPrefix()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Imports <xmlns:p="">
Imports <xmlns:XMLNS="">
Module M
    Private F1 As Object = <xmlns/>
    Private F2 As Object = <xmlns:x/>
    Private F3 As Object = <p:xmlns/>
    Private F4 As Object = <XMLNS:x/>
    Private F5 As Object = <x/>.<xmlns>
    Private F6 As Object = <x/>.<xmlns:y>
    Private F7 As Object = <x/>.<p:xmlns>
    Private F8 As Object = <x/>.<XMLNS:y>
    Private F9 As Object = <x/>.@xmlns
    Private F10 As Object = <x/>.@xmlns:z
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31189: Element names cannot use the 'xmlns' prefix.
    Private F2 As Object = <xmlns:x/>
                            ~~~~~
BC31189: Element names cannot use the 'xmlns' prefix.
    Private F6 As Object = <x/>.<xmlns:y>
                                 ~~~~~
]]></errors>)
        End Sub

        ' No ref to system.xml.dll	
        <Fact()>
        Public Sub BC31190ERR_XmlFeaturesNotAvailable()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
        Module M1
            Sub Foo()
                Dim x = Function() <aoeu>
                <%= 5 %>
                <%= (Function() "five")() %>
            </aoeu>
                Dim y = Function() <aoeu val=<%= (Function() <htns></htns>)().ToString() %>/>
            End Sub
        End Module
]]></file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31190: XML literals and XML axis properties are not available. Add references to System.Xml, System.Xml.Linq, and System.Core or other assemblies declaring System.Linq.Enumerable, System.Xml.Linq.XElement, System.Xml.Linq.XName, System.Xml.Linq.XAttribute and System.Xml.Linq.XNamespace types.
                Dim x = Function() <aoeu>
                                   ~~~~~~~
BC31190: XML literals and XML axis properties are not available. Add references to System.Xml, System.Xml.Linq, and System.Core or other assemblies declaring System.Linq.Enumerable, System.Xml.Linq.XElement, System.Xml.Linq.XName, System.Xml.Linq.XAttribute and System.Xml.Linq.XNamespace types.
                Dim y = Function() <aoeu val=<%= (Function() <htns></htns>)().ToString() %>/>
                                   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31190ERR_XmlFeaturesNotAvailable_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports <xmlns:p="...">
Module M
    Private A = <a><b><%= <c/> %></b></a>
    Private B = <a b=<%= <c/> %>/>
    Private C = <a/>.<b>.<c>
    Private D = <%= A %>
    Private E = <%= <x><%= A %></x> %>
    Private F = <a/>.<a>.<b>
    Private G = <a b="c"/>.<a>.@b
    Private H = <a/>...<b>
    Private J = <!-- comment -->
    Private K = <?xml version="1.0"?><x/>
End Module
]]></file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31190: XML literals and XML axis properties are not available. Add references to System.Xml, System.Xml.Linq, and System.Core or other assemblies declaring System.Linq.Enumerable, System.Xml.Linq.XElement, System.Xml.Linq.XName, System.Xml.Linq.XAttribute and System.Xml.Linq.XNamespace types.
    Private A = <a><b><%= <c/> %></b></a>
                ~~~~~~~~~~~~~~~~~~~~~~~~~
BC31190: XML literals and XML axis properties are not available. Add references to System.Xml, System.Xml.Linq, and System.Core or other assemblies declaring System.Linq.Enumerable, System.Xml.Linq.XElement, System.Xml.Linq.XName, System.Xml.Linq.XAttribute and System.Xml.Linq.XNamespace types.
    Private B = <a b=<%= <c/> %>/>
                ~~~~~~~~~~~~~~~~~~
BC31190: XML literals and XML axis properties are not available. Add references to System.Xml, System.Xml.Linq, and System.Core or other assemblies declaring System.Linq.Enumerable, System.Xml.Linq.XElement, System.Xml.Linq.XName, System.Xml.Linq.XAttribute and System.Xml.Linq.XNamespace types.
    Private C = <a/>.<b>.<c>
                ~~~~~~~~~~~~
BC31172: An embedded expression cannot be used here.
    Private D = <%= A %>
                ~~~~~~~~
BC31172: An embedded expression cannot be used here.
    Private E = <%= <x><%= A %></x> %>
                ~~~~~~~~~~~~~~~~~~~~~~
BC31190: XML literals and XML axis properties are not available. Add references to System.Xml, System.Xml.Linq, and System.Core or other assemblies declaring System.Linq.Enumerable, System.Xml.Linq.XElement, System.Xml.Linq.XName, System.Xml.Linq.XAttribute and System.Xml.Linq.XNamespace types.
    Private E = <%= <x><%= A %></x> %>
                    ~~~~~~~~~~~~~~~
BC31190: XML literals and XML axis properties are not available. Add references to System.Xml, System.Xml.Linq, and System.Core or other assemblies declaring System.Linq.Enumerable, System.Xml.Linq.XElement, System.Xml.Linq.XName, System.Xml.Linq.XAttribute and System.Xml.Linq.XNamespace types.
    Private F = <a/>.<a>.<b>
                ~~~~~~~~~~~~
BC31190: XML literals and XML axis properties are not available. Add references to System.Xml, System.Xml.Linq, and System.Core or other assemblies declaring System.Linq.Enumerable, System.Xml.Linq.XElement, System.Xml.Linq.XName, System.Xml.Linq.XAttribute and System.Xml.Linq.XNamespace types.
    Private G = <a b="c"/>.<a>.@b
                ~~~~~~~~~~~~~~~~~
BC31190: XML literals and XML axis properties are not available. Add references to System.Xml, System.Xml.Linq, and System.Core or other assemblies declaring System.Linq.Enumerable, System.Xml.Linq.XElement, System.Xml.Linq.XName, System.Xml.Linq.XAttribute and System.Xml.Linq.XNamespace types.
    Private H = <a/>...<b>
                ~~~~~~~~~~
BC31190: XML literals and XML axis properties are not available. Add references to System.Xml, System.Xml.Linq, and System.Core or other assemblies declaring System.Linq.Enumerable, System.Xml.Linq.XElement, System.Xml.Linq.XName, System.Xml.Linq.XAttribute and System.Xml.Linq.XNamespace types.
    Private J = <!-- comment -->
                ~~~~~~~~~~~~~~~~
BC31190: XML literals and XML axis properties are not available. Add references to System.Xml, System.Xml.Linq, and System.Core or other assemblies declaring System.Linq.Enumerable, System.Xml.Linq.XElement, System.Xml.Linq.XName, System.Xml.Linq.XAttribute and System.Xml.Linq.XNamespace types.
    Private K = <?xml version="1.0"?><x/>
                ~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31190ERR_XmlFeaturesNotAvailable_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module M
    Private F1 = &lt;x&gt;&lt;![CDATA[str]]&gt;&lt;/&gt;
    Private F2 = &lt;![CDATA[str]]&gt;
End Module
</file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors>
BC31190: XML literals and XML axis properties are not available. Add references to System.Xml, System.Xml.Linq, and System.Core or other assemblies declaring System.Linq.Enumerable, System.Xml.Linq.XElement, System.Xml.Linq.XName, System.Xml.Linq.XAttribute and System.Xml.Linq.XNamespace types.
    Private F1 = &lt;x&gt;&lt;![CDATA[str]]&gt;&lt;/&gt;
                 ~~~~~~~~~~~~~~~~~~~~~
BC31190: XML literals and XML axis properties are not available. Add references to System.Xml, System.Xml.Linq, and System.Core or other assemblies declaring System.Linq.Enumerable, System.Xml.Linq.XElement, System.Xml.Linq.XName, System.Xml.Linq.XAttribute and System.Xml.Linq.XNamespace types.
    Private F2 = &lt;![CDATA[str]]&gt;
                 ~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub BC31190ERR_XmlFeaturesNotAvailable_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Module M
    Private F = GetXmlNamespace()
End Module
]]></file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31190: XML literals and XML axis properties are not available. Add references to System.Xml, System.Xml.Linq, and System.Core or other assemblies declaring System.Linq.Enumerable, System.Xml.Linq.XElement, System.Xml.Linq.XName, System.Xml.Linq.XAttribute and System.Xml.Linq.XNamespace types.
    Private F = GetXmlNamespace()
                ~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC42361WRN_UseValueForXmlExpression3()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off
Imports System.Collections.Generic
Imports System.Xml.Linq
Class A
End Class
NotInheritable Class B
End Class
Module M
    Sub M(Of T As Class)()
        Dim _o As Object
        Dim _s As String
        Dim _a As A
        Dim _b As B
        Dim _t As T
        _o = <x/>.<y>
        _o = CType(<x/>.<y>, Object)
        _o = DirectCast(<x/>.<y>, Object)
        _o = TryCast(<x/>.<y>, Object)
        _s = <x/>.<y>
        _s = CType(<x/>.<y>, String)
        _s = DirectCast(<x/>.<y>, String)
        _s = TryCast(<x/>.<y>, String)
        _a = <x/>.<y>
        _a = CType(<x/>.<y>, A)
        _a = DirectCast(<x/>.<y>, A)
        _a = TryCast(<x/>.<y>, A)
        _b = <x/>.<y>
        _b = CType(<x/>.<y>, B)
        _b = DirectCast(<x/>.<y>, B)
        _b = TryCast(<x/>.<y>, B)
        _t = <x/>.<y>
        _t = CType(<x/>.<y>, T)
        _t = DirectCast(<x/>.<y>, T)
        _t = TryCast(<x/>.<y>, T)
    End Sub
End Module
]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC42361: Cannot convert 'IEnumerable(Of XElement)' to 'String'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of XElement)'.
        _s = <x/>.<y>
             ~~~~~~~~
BC42361: Cannot convert 'IEnumerable(Of XElement)' to 'String'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of XElement)'.
        _s = CType(<x/>.<y>, String)
                   ~~~~~~~~
BC42361: Cannot convert 'IEnumerable(Of XElement)' to 'String'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of XElement)'.
        _s = DirectCast(<x/>.<y>, String)
                        ~~~~~~~~
BC42361: Cannot convert 'IEnumerable(Of XElement)' to 'String'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of XElement)'.
        _s = TryCast(<x/>.<y>, String)
                     ~~~~~~~~
BC42322: Runtime errors might occur when converting 'IEnumerable(Of XElement)' to 'B'.
        _b = <x/>.<y>
             ~~~~~~~~
BC42322: Runtime errors might occur when converting 'IEnumerable(Of XElement)' to 'B'.
        _b = CType(<x/>.<y>, B)
                   ~~~~~~~~
BC42322: Runtime errors might occur when converting 'IEnumerable(Of XElement)' to 'B'.
        _b = DirectCast(<x/>.<y>, B)
                        ~~~~~~~~
BC42322: Runtime errors might occur when converting 'IEnumerable(Of XElement)' to 'B'.
        _b = TryCast(<x/>.<y>, B)
                     ~~~~~~~~
]]></errors>)
        End Sub

        ' Same as above but with "Option Strict On".
        <Fact()>
        Public Sub BC42361WRN_UseValueForXmlExpression3_1()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Imports System.Xml.Linq
Class A
End Class
NotInheritable Class B
End Class
Module M
    Sub M(Of T As Class)()
        Dim _o As Object
        Dim _s As String
        Dim _a As A
        Dim _b As B
        Dim _t As T
        _o = <x/>.<y>
        _o = CType(<x/>.<y>, Object)
        _o = DirectCast(<x/>.<y>, Object)
        _o = TryCast(<x/>.<y>, Object)
        _s = <x/>.<y>
        _s = CType(<x/>.<y>, String)
        _s = DirectCast(<x/>.<y>, String)
        _s = TryCast(<x/>.<y>, String)
        _a = <x/>.<y>
        _a = CType(<x/>.<y>, A)
        _a = DirectCast(<x/>.<y>, A)
        _a = TryCast(<x/>.<y>, A)
        _b = <x/>.<y>
        _b = CType(<x/>.<y>, B)
        _b = DirectCast(<x/>.<y>, B)
        _b = TryCast(<x/>.<y>, B)
        _t = <x/>.<y>
        _t = CType(<x/>.<y>, T)
        _t = DirectCast(<x/>.<y>, T)
        _t = TryCast(<x/>.<y>, T)
    End Sub
End Module
]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'IEnumerable(Of XElement)' to 'String'.
        _s = <x/>.<y>
             ~~~~~~~~
BC42361: Cannot convert 'IEnumerable(Of XElement)' to 'String'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of XElement)'.
        _s = <x/>.<y>
             ~~~~~~~~
BC42361: Cannot convert 'IEnumerable(Of XElement)' to 'String'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of XElement)'.
        _s = CType(<x/>.<y>, String)
                   ~~~~~~~~
BC42361: Cannot convert 'IEnumerable(Of XElement)' to 'String'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of XElement)'.
        _s = DirectCast(<x/>.<y>, String)
                        ~~~~~~~~
BC42361: Cannot convert 'IEnumerable(Of XElement)' to 'String'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of XElement)'.
        _s = TryCast(<x/>.<y>, String)
                     ~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'IEnumerable(Of XElement)' to 'A'.
        _a = <x/>.<y>
             ~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'IEnumerable(Of XElement)' to 'B'.
        _b = <x/>.<y>
             ~~~~~~~~
BC42322: Runtime errors might occur when converting 'IEnumerable(Of XElement)' to 'B'.
        _b = <x/>.<y>
             ~~~~~~~~
BC42322: Runtime errors might occur when converting 'IEnumerable(Of XElement)' to 'B'.
        _b = CType(<x/>.<y>, B)
                   ~~~~~~~~
BC42322: Runtime errors might occur when converting 'IEnumerable(Of XElement)' to 'B'.
        _b = DirectCast(<x/>.<y>, B)
                        ~~~~~~~~
BC42322: Runtime errors might occur when converting 'IEnumerable(Of XElement)' to 'B'.
        _b = TryCast(<x/>.<y>, B)
                     ~~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'IEnumerable(Of XElement)' to 'T'.
        _t = <x/>.<y>
             ~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC42361WRN_UseValueForXmlExpression3_2()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off
Imports System.Collections.Generic
Imports System.Xml.Linq
Class X
    Inherits XElement
    Public Sub New(name As XName)
        MyBase.New(name)
    End Sub
End Class
Interface IEnumerableOfXElement
    Inherits IEnumerable(Of XElement)
End Interface
Module M
    Sub M(Of T As XElement)(_1 As XElement,
          _2 As IEnumerable(Of XElement),
          _3 As XElement(),
          _4 As List(Of XElement),
          _5 As IEnumerable(Of XObject),
          _6 As IEnumerable(Of XElement)(),
          _7 As IEnumerableOfXElement,
          _8 As IEnumerable(Of X),
          _9 As IEnumerable(Of T))
        Dim o As String
        o = _1
        o = _2
        o = _3
        o = _4
        o = _5
        o = _6
        o = _7
        o = _8
        o = _9
    End Sub
End Module
]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC42361: Cannot convert 'IEnumerable(Of XElement)' to 'String'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of XElement)'.
        o = _2
            ~~
BC30311: Value of type 'XElement()' cannot be converted to 'String'.
        o = _3
            ~~
BC30311: Value of type 'List(Of XElement)' cannot be converted to 'String'.
        o = _4
            ~~
BC42322: Runtime errors might occur when converting 'IEnumerable(Of XObject)' to 'String'.
        o = _5
            ~~
BC30311: Value of type 'IEnumerable(Of XElement)()' cannot be converted to 'String'.
        o = _6
            ~~
BC42361: Cannot convert 'IEnumerableOfXElement' to 'String'. You can use the 'Value' property to get the string value of the first element of 'IEnumerableOfXElement'.
        o = _7
            ~~
BC42361: Cannot convert 'IEnumerable(Of X)' to 'String'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of X)'.
        o = _8
            ~~
BC42361: Cannot convert 'IEnumerable(Of T As XElement)' to 'String'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of T As XElement)'.
        o = _9
            ~~
]]></errors>)
        End Sub

        ' Conversions to IEnumerable(Of XElement). Dev11 reports BC31193
        ' when converting NotInheritable Class to IEnumerable(Of XElement).
        <Fact()>
        Public Sub BC42361WRN_UseValueForXmlExpression3_3()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off
Imports System.Collections.Generic
Imports System.Xml.Linq
Class A
End Class
NotInheritable Class B
End Class
Module M
    Sub M(Of T As Class)()
        Dim _i As IEnumerable(Of XElement)
        Dim _o As Object = Nothing
        Dim _s As String = Nothing
        Dim _a As A = Nothing
        Dim _b As B = Nothing
        Dim _t As T = Nothing
        _i = _o
        _i = _s
        _i = _a
        _i = _b
        _i = _t
    End Sub
End Module
]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC42322: Runtime errors might occur when converting 'String' to 'IEnumerable(Of XElement)'.
        _i = _s
             ~~
BC42322: Runtime errors might occur when converting 'B' to 'IEnumerable(Of XElement)'.
        _i = _b
             ~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31194ERR_TypeMismatchForXml3()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict Off
Structure S
End Structure
Module M
    Sub M()
        Dim _s As S
        _s = <x/>.<y>
        _s = CType(<x/>.<y>, S)
        _s = DirectCast(<x/>.<y>, S)
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31194: Value of type 'IEnumerable(Of XElement)' cannot be converted to 'S'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of XElement)'.
        _s = <x/>.<y>
             ~~~~~~~~
BC31194: Value of type 'IEnumerable(Of XElement)' cannot be converted to 'S'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of XElement)'.
        _s = CType(<x/>.<y>, S)
                   ~~~~~~~~
BC31194: Value of type 'IEnumerable(Of XElement)' cannot be converted to 'S'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of XElement)'.
        _s = DirectCast(<x/>.<y>, S)
                        ~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31194ERR_TypeMismatchForXml3_1()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict Off
Imports System.Collections.Generic
Imports System.Xml.Linq
Structure S
End Structure
Class X
    Inherits XElement
    Public Sub New(name As XName)
        MyBase.New(name)
    End Sub
End Class
Interface IEnumerableOfXElement
    Inherits IEnumerable(Of XElement)
End Interface
Module M
    Sub M(Of T As XElement)(_1 As XElement,
          _2 As IEnumerable(Of XElement),
          _3 As XElement(),
          _4 As List(Of XElement),
          _5 As IEnumerable(Of XObject),
          _6 As IEnumerable(Of XElement)(),
          _7 As IEnumerableOfXElement,
          _8 As IEnumerable(Of X),
          _9 As IEnumerable(Of T))
        Dim o As S
        o = _1
        o = _2
        o = _3
        o = _4
        o = _5
        o = _6
        o = _7
        o = _8
        o = _9
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30311: Value of type 'XElement' cannot be converted to 'S'.
        o = _1
            ~~
BC31194: Value of type 'IEnumerable(Of XElement)' cannot be converted to 'S'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of XElement)'.
        o = _2
            ~~
BC30311: Value of type 'XElement()' cannot be converted to 'S'.
        o = _3
            ~~
BC30311: Value of type 'List(Of XElement)' cannot be converted to 'S'.
        o = _4
            ~~
BC30311: Value of type 'IEnumerable(Of XObject)' cannot be converted to 'S'.
        o = _5
            ~~
BC30311: Value of type 'IEnumerable(Of XElement)()' cannot be converted to 'S'.
        o = _6
            ~~
BC31194: Value of type 'IEnumerableOfXElement' cannot be converted to 'S'. You can use the 'Value' property to get the string value of the first element of 'IEnumerableOfXElement'.
        o = _7
            ~~
BC31194: Value of type 'IEnumerable(Of X)' cannot be converted to 'S'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of X)'.
        o = _8
            ~~
BC31194: Value of type 'IEnumerable(Of T As XElement)' cannot be converted to 'S'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of T As XElement)'.
        o = _9
            ~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31195ERR_BinaryOperandsForXml4()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict Off
Imports System.Collections.Generic
Imports System.Xml.Linq
Class C
End Class
Class X
    Inherits XElement
    Public Sub New(name As XName)
        MyBase.New(name)
    End Sub
End Class
Interface IEnumerableOfXElement
    Inherits IEnumerable(Of XElement)
End Interface
Module M
    Sub M(Of T As XElement)(o As C,
          _1 As XElement,
          _2 As IEnumerable(Of XElement),
          _3 As XElement(),
          _4 As List(Of XElement),
          _5 As IEnumerable(Of XObject),
          _6 As IEnumerable(Of XElement)(),
          _7 As IEnumerableOfXElement,
          _8 As IEnumerable(Of X),
          _9 As IEnumerable(Of T))
        Dim b As Boolean
        b = (o = _1)
        b = (o = _2)
        b = (o = _3)
        b = (o = _4)
        b = (_5 = o)
        b = (_6 = o)
        b = (_7 = o)
        b = (_8 = o)
        b = (_9 = o)
        b = (_1 = _2)
        b = (_2 = _2)
        b = (_3 = _2)
        b = (_4 = _2)
        b = (_5 = _2)
        b = (_2 = _6)
        b = (_2 = _7)
        b = (_2 = _8)
        b = (_2 = _9)
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30452: Operator '=' is not defined for types 'C' and 'XElement'.
        b = (o = _1)
             ~~~~~~
BC31195: Operator '=' is not defined for types 'C' and 'IEnumerable(Of XElement)'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of XElement)'.
        b = (o = _2)
             ~~~~~~
BC31195: Operator '=' is not defined for types 'C' and 'XElement()'. You can use the 'Value' property to get the string value of the first element of 'XElement()'.
        b = (o = _3)
             ~~~~~~
BC31195: Operator '=' is not defined for types 'C' and 'List(Of XElement)'. You can use the 'Value' property to get the string value of the first element of 'List(Of XElement)'.
        b = (o = _4)
             ~~~~~~
BC30452: Operator '=' is not defined for types 'IEnumerable(Of XObject)' and 'C'.
        b = (_5 = o)
             ~~~~~~
BC30452: Operator '=' is not defined for types 'IEnumerable(Of XElement)()' and 'C'.
        b = (_6 = o)
             ~~~~~~
BC31195: Operator '=' is not defined for types 'IEnumerableOfXElement' and 'C'. You can use the 'Value' property to get the string value of the first element of 'IEnumerableOfXElement'.
        b = (_7 = o)
             ~~~~~~
BC31195: Operator '=' is not defined for types 'IEnumerable(Of X)' and 'C'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of X)'.
        b = (_8 = o)
             ~~~~~~
BC31195: Operator '=' is not defined for types 'IEnumerable(Of T As XElement)' and 'C'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of T As XElement)'.
        b = (_9 = o)
             ~~~~~~
BC31195: Operator '=' is not defined for types 'XElement' and 'IEnumerable(Of XElement)'. You can use the 'Value' property to get the string value of the first element of 'IEnumerable(Of XElement)'.
        b = (_1 = _2)
             ~~~~~~~
BC31080: Operator '=' is not defined for types 'IEnumerable(Of XElement)' and 'IEnumerable(Of XElement)'. Use 'Is' operator to compare two reference types.
        b = (_2 = _2)
             ~~~~~~~
BC31080: Operator '=' is not defined for types 'XElement()' and 'IEnumerable(Of XElement)'. Use 'Is' operator to compare two reference types.
        b = (_3 = _2)
             ~~~~~~~
BC31195: Operator '=' is not defined for types 'List(Of XElement)' and 'IEnumerable(Of XElement)'. You can use the 'Value' property to get the string value of the first element of 'List(Of XElement)'.
        b = (_4 = _2)
             ~~~~~~~
BC31080: Operator '=' is not defined for types 'IEnumerable(Of XObject)' and 'IEnumerable(Of XElement)'. Use 'Is' operator to compare two reference types.
        b = (_5 = _2)
             ~~~~~~~
BC31080: Operator '=' is not defined for types 'IEnumerable(Of XElement)' and 'IEnumerable(Of XElement)()'. Use 'Is' operator to compare two reference types.
        b = (_2 = _6)
             ~~~~~~~
BC31080: Operator '=' is not defined for types 'IEnumerable(Of XElement)' and 'IEnumerableOfXElement'. Use 'Is' operator to compare two reference types.
        b = (_2 = _7)
             ~~~~~~~
BC31080: Operator '=' is not defined for types 'IEnumerable(Of XElement)' and 'IEnumerable(Of X)'. Use 'Is' operator to compare two reference types.
        b = (_2 = _8)
             ~~~~~~~
BC31080: Operator '=' is not defined for types 'IEnumerable(Of XElement)' and 'IEnumerable(Of T As XElement)'. Use 'Is' operator to compare two reference types.
        b = (_2 = _9)
             ~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC31394ERR_RestrictedConversion1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="RestrictedConversion1">
        <file name="a.vb">
        Class C1
            Sub FOO()
                Dim obj As Object
                obj = New system.ArgIterator
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC31394: Expression of type 'ArgIterator' cannot be converted to 'Object' or 'ValueType'.
                obj = New system.ArgIterator
                      ~~~~~~~~~~~~~~~~~~~~~~    
    </expected>)
        End Sub

        <WorkItem(527685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527685")>
        <Fact()>
        Public Sub BC31394ERR_RestrictedConversion1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="RestrictedConversion1_1">
        <file name="a.vb">
Option Infer Off
        imports system
        Structure C1
            Sub FOO()
                Dim obj = New ArgIterator
                Dim TypeRefInstance As TypedReference
                obj = TypeRefInstance
            End Sub
        End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC31394: Expression of type 'ArgIterator' cannot be converted to 'Object' or 'ValueType'.
                Dim obj = New ArgIterator
                          ~~~~~~~~~~~~~~~
BC31394: Expression of type 'TypedReference' cannot be converted to 'Object' or 'ValueType'.
                obj = TypeRefInstance
                      ~~~~~~~~~~~~~~~
BC42109: Variable 'TypeRefInstance' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
                obj = TypeRefInstance
                      ~~~~~~~~~~~~~~~        
    </expected>)
        End Sub

        <WorkItem(527685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527685")>
        <Fact()>
        Public Sub BC31394ERR_RestrictedConversion1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="RestrictedConversion1">
        <file name="a.vb">
Option Infer Off
        imports system
        Structure C1
            Sub FOO()
                Dim obj = New ArgIterator
                obj = New RuntimeArgumentHandle
            End Sub
        End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC31394: Expression of type 'ArgIterator' cannot be converted to 'Object' or 'ValueType'.
                Dim obj = New ArgIterator
                          ~~~~~~~~~~~~~~~
BC31394: Expression of type 'RuntimeArgumentHandle' cannot be converted to 'Object' or 'ValueType'.
                obj = New RuntimeArgumentHandle
                      ~~~~~~~~~~~~~~~~~~~~~~~~~
    </expected>)
        End Sub

        <Fact(), WorkItem(529561, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529561")>
        Public Sub BC31396ERR_RestrictedType1_1()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System
Module M
    Sub Main()
        Dim x As TypedReference()
        Dim y() As ArgIterator
        Dim z = {New RuntimeArgumentHandle()}
    End Sub
End Module
    </file>
    </compilation>).AssertTheseDiagnostics(
    <expected>
BC42024: Unused local variable: 'x'.
        Dim x As TypedReference()
            ~
BC31396: 'TypedReference' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim x As TypedReference()
                 ~~~~~~~~~~~~~~~~
BC42024: Unused local variable: 'y'.
        Dim y() As ArgIterator
            ~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim y() As ArgIterator
                   ~~~~~~~~~~~
BC31396: 'RuntimeArgumentHandle' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim z = {New RuntimeArgumentHandle()}
              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31396: 'RuntimeArgumentHandle' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim z = {New RuntimeArgumentHandle()}
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC31396ERR_RestrictedType1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Imports System
Class C(Of T)
    Shared Sub F(Of U)(o As U)
    End Sub
    Shared Sub M()
        Dim o As Object
        o = New C(Of ArgIterator)
        o = New C(Of RuntimeArgumentHandle)
        o = New C(Of TypedReference)
        F(Of ArgIterator)(Nothing)
        F(Of RuntimeArgumentHandle)(Nothing)
        Dim t As TypedReference = Nothing
        F(t)
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        o = New C(Of ArgIterator)
                     ~~~~~~~~~~~
BC31396: 'RuntimeArgumentHandle' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        o = New C(Of RuntimeArgumentHandle)
                     ~~~~~~~~~~~~~~~~~~~~~
BC31396: 'TypedReference' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        o = New C(Of TypedReference)
                     ~~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        F(Of ArgIterator)(Nothing)
        ~~~~~~~~~~~~~~~~~
BC31396: 'RuntimeArgumentHandle' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        F(Of RuntimeArgumentHandle)(Nothing)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31396: 'TypedReference' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        F(t)
        ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC31396ERR_RestrictedType1_3()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System
Class C
    Private F As TypedReference
    Private G As ArgIterator()
    Private H = {New RuntimeArgumentHandle()}
    Sub M(e As ArgIterator())
    End Sub
End Class
Interface I
    ReadOnly Property P As TypedReference
    Function F() As ArgIterator()()
    Property Q As RuntimeArgumentHandle()()
End Interface
    </file>
    </compilation>).AssertTheseDiagnostics(
    <expected>
BC31396: 'TypedReference' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
    Private F As TypedReference
                 ~~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
    Private G As ArgIterator()
                 ~~~~~~~~~~~~~
BC31396: 'RuntimeArgumentHandle' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
    Private H = {New RuntimeArgumentHandle()}
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
    Sub M(e As ArgIterator())
               ~~~~~~~~~~~~~
BC31396: 'TypedReference' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
    ReadOnly Property P As TypedReference
                           ~~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
    Function F() As ArgIterator()()
                    ~~~~~~~~~~~~~~~
BC31396: 'RuntimeArgumentHandle' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
    Property Q As RuntimeArgumentHandle()()
                  ~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC31399ERR_NoAccessibleConstructorOnBase()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NoAccessibleConstructorOnBase">
        <file name="a.vb">
        Class c1
            Private Sub New()
            End Sub
        End Class
        Class c2
            Inherits c1
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC31399: Class 'c1' has no accessible 'Sub New' and cannot be inherited.
        Class c2
              ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC31419ERR_IsNotOpRequiresReferenceTypes1()
            CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
        Class A1
            Sub scen1()
                dim arr1 = New Integer() {1, 2, 3}
                dim arr2 = arr1
                Dim b As Boolean
                b = arr1(0) IsNot arr2(0)
            End Sub
        End Class
    </file>
    </compilation>).VerifyDiagnostics(
    Diagnostic(ERRID.ERR_IsNotOpRequiresReferenceTypes1, "arr1(0)").WithArguments("Integer"),
    Diagnostic(ERRID.ERR_IsNotOpRequiresReferenceTypes1, "arr2(0)").WithArguments("Integer"))

        End Sub

        <Fact()>
        Public Sub BC31419ERR_IsNotOpRequiresReferenceTypes1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
        Class A1
            Sub scen1()
                Dim a As E
                Dim b As S1
                dim o = a IsNot b
            End Sub
        End Class
        structure S1
        End structure
        ENUM E
            Dummy
        End ENUM
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC31419: 'IsNot' requires operands that have reference types, but this operand has the value type 'E'.
                dim o = a IsNot b
                        ~
BC31419: 'IsNot' requires operands that have reference types, but this operand has the value type 'S1'.
                dim o = a IsNot b
                                ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC31419ERR_IsNotOpRequiresReferenceTypes1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Interface I
End Interface
Class A
End Class
Class B(Of T As Structure)
End Class
Class C
    Shared Sub M1(Of T1)(_1 As T1)
        If _1 IsNot Nothing Then
        End If
        If Nothing IsNot _1 Then
        End If
    End Sub
    Shared Sub M2(Of T2 As Class)(_2 As T2)
        If _2 IsNot Nothing Then
        End If
        If Nothing IsNot _2 Then
        End If
    End Sub
    Shared Sub M3(Of T3 As Structure)(_3 As T3)
        If _3 IsNot Nothing Then
        End If
        If Nothing IsNot _3 Then
        End If
    End Sub
    Shared Sub M4(Of T4 As New)(_4 As T4)
        If _4 IsNot Nothing Then
        End If
        If Nothing IsNot _4 Then
        End If
    End Sub
    Shared Sub M5(Of T5 As I)(_5 As T5)
        If _5 IsNot Nothing Then
        End If
        If Nothing IsNot _5 Then
        End If
    End Sub
    Shared Sub M6(Of T6 As A)(_6 As T6)
        If _6 IsNot Nothing Then
        End If
        If Nothing IsNot _6 Then
        End If
    End Sub
    Shared Sub M7(Of T7 As U, U)(_7 As T7)
        If _7 IsNot Nothing Then
        End If
        If Nothing IsNot _7 Then
        End If
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC31419: 'IsNot' requires operands that have reference types, but this operand has the value type 'T3'.
        If _3 IsNot Nothing Then
           ~~
BC31419: 'IsNot' requires operands that have reference types, but this operand has the value type 'T3'.
        If Nothing IsNot _3 Then
                         ~~
</expected>)
        End Sub

        <WorkItem(542192, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542192")>
        <Fact()>
        Public Sub BC31428ERR_VoidArrayDisallowed()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="VoidArrayDisallowed">
        <file name="a.vb">
        Imports System

        Module M1
            Dim y As Type = GetType(Void)
            Dim x As Type = GetType(Void())
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC31428: Arrays of type 'System.Void' are not allowed in this expression.
            Dim x As Type = GetType(Void())
                                    ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub PartialMethodAndDeclareMethod()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices

Partial Class Clazz
    Partial Private Declare Sub S0 Lib "abc.dll" (p As String)
    Partial Private Sub S1(&lt;Out> p As String)
    End Sub
    Partial Private Sub S2(&lt;Out> ByRef p As String)
    End Sub
End Class

Partial Class Clazz
    Private Declare Sub S1 Lib "abc.dll" (p As String)
End Class
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30244: 'Partial' is not valid on a Declare.
    Partial Private Declare Sub S0 Lib "abc.dll" (p As String)
    ~~~~~~~
BC30345: 'Private Sub S1(p As String)' and 'Private Declare Ansi Sub S1 Lib "abc.dll" (p As String)' cannot overload each other because they differ only by parameters declared 'ByRef' or 'ByVal'.
    Partial Private Sub S1(&lt;Out> p As String)
                        ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC31435ERR_PartialMethodMustBeEmpty()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="PartialMethodMustBeEmpty">
        <file name="a.vb">
        Class C1
            Partial Private Sub Foo()
                System.Console.WriteLine()
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC31435: Partial methods must have empty method bodies.
            Partial Private Sub Foo()
                                ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC31435ERR_PartialMethodMustBeEmpty2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="PartialMethodMustBeEmpty2">
        <file name="a.vb">
        Imports System
        Class C1
            Partial Private Shared Sub PS(a As Integer)
                Console.WriteLine()
            End Sub
            Partial Private Shared Sub Ps(a As Integer)
                Console.WriteLine()
            End Sub
            Private Shared Sub PS(a As Integer)
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31435: Partial methods must have empty method bodies.
            Partial Private Shared Sub PS(a As Integer)
                                       ~~
BC31433: Method 'Ps' cannot be declared 'Partial' because only one method 'Ps' can be marked 'Partial'.
            Partial Private Shared Sub Ps(a As Integer)
                                       ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC31435ERR_PartialMethodMustBeEmpty3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="PartialMethodMustBeEmpty3">
        <file name="a.vb">
        Imports System
        Class C1
            Partial Private Shared Sub PS(a As Integer)
            End Sub
            Partial Private Shared Sub Ps(a As Integer)
                Console.WriteLine()
            End Sub
            Private Shared Sub PS(a As Integer)
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31433: Method 'Ps' cannot be declared 'Partial' because only one method 'Ps' can be marked 'Partial'.
            Partial Private Shared Sub Ps(a As Integer)
                                       ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC31440ERR_NoPartialMethodInAddressOf1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NoPartialMethodInAddressOf1">
        <file name="a.vb">
        Public Class C1
            Event x()
            Partial Private Sub Foo()
            End Sub
            Sub MethodToAddHandlerInPrivatePartial()
                AddHandler Me.x, AddressOf Me.Foo
            End Sub
            Sub MethodToRemoveHandlerInPrivatePartial()
                RemoveHandler Me.x, AddressOf Me.Foo
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC31440: 'AddressOf' cannot be applied to 'Private Sub Foo()' because 'Private Sub Foo()' is a partial method without an implementation.
                AddHandler Me.x, AddressOf Me.Foo
                                           ~~~~~~
BC31440: 'AddressOf' cannot be applied to 'Private Sub Foo()' because 'Private Sub Foo()' is a partial method without an implementation.
                RemoveHandler Me.x, AddressOf Me.Foo
                                              ~~~~~~
</expected>)
        End Sub

        ' Roslyn extra - ERR_TypeMismatch2 * 2
        <Fact()>
        Public Sub BC31440ERR_NoPartialMethodInAddressOf1a()
            CreateCompilationWithMscorlib40(
    <compilation name="NoPartialMethodInAddressOf1a">
        <file name="a.vb">
        Imports System
        Public Class C1
            Event x()
            Partial Private Sub Foo()
            End Sub
            Sub MethodToAddHandlerInPrivatePartial()
                AddHandler Me.x, New Action(AddressOf Me.Foo)
            End Sub
            Sub MethodToRemoveHandlerInPrivatePartial()
                RemoveHandler Me.x, New Action(AddressOf Me.Foo)
            End Sub
        End Class
    </file>
    </compilation>).VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_NoPartialMethodInAddressOf1, "Me.Foo").WithArguments("Private Sub Foo()"),
                    Diagnostic(ERRID.ERR_TypeMismatch2, "New Action(AddressOf Me.Foo)").WithArguments("System.Action", "C1.xEventHandler"),
                    Diagnostic(ERRID.ERR_NoPartialMethodInAddressOf1, "Me.Foo").WithArguments("Private Sub Foo()"),
                    Diagnostic(ERRID.ERR_TypeMismatch2, "New Action(AddressOf Me.Foo)").WithArguments("System.Action", "C1.xEventHandler"))
        End Sub

        <Fact()>
        Public Sub BC31440ERR_NoPartialMethodInAddressOf1b()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NoPartialMethodInAddressOf1b">
        <file name="a.vb">
        Imports System
        Public Class C1
            Sub Test()
                Dim a As Action = New Action(AddressOf Me.Foo)
                a()
            End Sub 
            Partial Private Sub Foo()
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31440: 'AddressOf' cannot be applied to 'Private Sub Foo()' because 'Private Sub Foo()' is a partial method without an implementation.
                Dim a As Action = New Action(AddressOf Me.Foo)
                                                       ~~~~~~
</expected>)
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/72431")>
        <Fact>
        Public Sub BC31440ERR_NoPartialMethodInAddressOf_02()
            Dim source =
"Delegate Sub D()
Partial Class Program
    Shared Sub Main()
        M1(AddressOf M2(Of Integer))
    End Sub
    Shared Sub M1(d As D)
    End Sub
    Private Shared Partial Sub M2(Of T)()
    End Sub
End Class"
            Dim comp = CreateCompilation(source)
            comp.AssertTheseEmitDiagnostics(
<expected>
    BC31440: 'AddressOf' cannot be applied to 'Private Shared Sub M2(Of Integer)()' because 'Private Shared Sub M2(Of Integer)()' is a partial method without an implementation.
        M1(AddressOf M2(Of Integer))
                     ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/72431")>
        <Fact>
        Public Sub BC31440ERR_NoPartialMethodInAddressOf_03()
            Dim source =
"Delegate Sub D()
Partial Class C(Of T)
    Shared Sub M1()
        M2(AddressOf C(Of Integer).M3)
    End Sub
    Shared Sub M2(d As D)
    End Sub
    Private Shared Partial Sub M3()
    End Sub
End Class"
            Dim comp = CreateCompilation(source)
            comp.AssertTheseEmitDiagnostics(
<expected>
    BC31440: 'AddressOf' cannot be applied to 'Private Shared Sub M3()' because 'Private Shared Sub M3()' is a partial method without an implementation.
        M2(AddressOf C(Of Integer).M3)
                     ~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC31500ERR_BadAttributeSharedProperty1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadAttributeSharedProperty1">
        <file name="at31500.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.All)>
Public Class MyAttribute
    Inherits Attribute
    Public Shared SharedField As String
    Public Const ConstField As String = "AAA"
End Class

<My(SharedField:="testing")>
Class Foo
    <My(ConstField:="testing")>
    Public Sub S()

    End Sub
End Class
    ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_BadAttributeSharedProperty1, "SharedField").WithArguments("SharedField"),
                                      Diagnostic(ERRID.ERR_BadAttributeSharedProperty1, "ConstField").WithArguments("ConstField")) ' Dev10: 31510

        End Sub

        ''' BC31510 in DEV10 but is BC31500 in Roslyn 
        <Fact()>
        Public Sub BC31500ERR_BadAttributeSharedProperty1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadAttributeConstField1">
        <file name="a.vb">
            Imports System
        &lt;AttributeUsage(AttributeTargets.All)&gt; Class attr
            Inherits Attribute
            Public Const c As String = "A"
        End Class
        &lt;attr(c:="test")&gt; Class c8
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC31500: 'Shared' attribute property 'c' cannot be the target of an assignment.
        &lt;attr(c:="test")&gt; Class c8
              ~
    </expected>)
        End Sub

        <Fact()>
        Public Sub BC31501ERR_BadAttributeReadOnlyProperty1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadAttributeReadOnlyProperty1">
        <file name="at31501.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.All)>
Public Class MyAttribute
    Inherits Attribute

    Public ReadOnly Property RP As String
        Get
            Return "RP"
        End Get
    End Property

    Public WriteOnly Property WP As Integer
        Set(value As Integer)
        End Set
    End Property
End Class

<MyAttribute(WP:=123, RP:="123")>
Class Test

End Class
    ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_BadAttributeReadOnlyProperty1, "RP").WithArguments("RP"))

        End Sub

        Private Shared ReadOnly s_badAttributeIl As String = <![CDATA[
.class public auto ansi beforefieldinit BaseAttribute
       extends [mscorlib]System.Attribute
{
  .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = ( 01 00 FF 7F 00 00 00 00 ) 
  .method public hidebysig newslot specialname virtual 
          instance int32  get_PROP() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method BaseAttribute::get_PROP

  .method public hidebysig newslot specialname virtual 
          instance void  set_PROP(int32 'value') cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method BaseAttribute::set_PROP

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  ret
  } // end of method BaseAttribute::.ctor

  .property instance int32 PROP()
  {
    .get instance int32 BaseAttribute::get_PROP()
    .set instance void BaseAttribute::set_PROP(int32)
  } // end of property BaseAttribute::PROP
} // end of class BaseAttribute

.class public auto ansi beforefieldinit DerivedAttribute
       extends BaseAttribute
{
  .method public hidebysig specialname virtual 
          instance int32  get_PROP() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method DerivedAttribute::get_PROP

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void BaseAttribute::.ctor()
    IL_0006:  ret
  } // end of method DerivedAttribute::.ctor

  .property instance int32 PROP()
  {
    .get instance int32 DerivedAttribute::get_PROP()
  } // end of property DerivedAttribute::PROP
} // end of class DerivedAttribute
]]>.Value.Replace(vbLf, vbNewLine)

        <WorkItem(528981, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528981")>
        <Fact()>
        Public Sub BC31501ERR_BadAttributeReadOnlyProperty2()
            Dim compilation = CompilationUtils.CreateCompilationWithCustomILSource(
    <compilation name="BadAttributeReadOnlyProperty2">
        <file name="at31501.vb"><![CDATA[
<Derived(PROP:=1)>
Class Test

End Class
    ]]></file>
    </compilation>, s_badAttributeIl).VerifyDiagnostics(Diagnostic(ERRID.ERR_BadAttributeReadOnlyProperty1, "PROP").WithArguments("PROP"))

        End Sub

        <WorkItem(540627, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540627")>
        <Fact()>
        Public Sub BC31511ERR_BadAttributeNonPublicProperty1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
            <compilation name="BadAttributeNonPublicProperty1">
                <file name="at31511.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.All)>
Public Class MyAttribute
    Inherits Attribute
    Friend Field As String
    Friend Property Prop As Long
End Class

<My(Field:="testing")>
Class Foo
    <My(Prop:=12345)>
    Public Sub S()

    End Sub
End Class
    ]]></file>
            </compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_BadAttributeNonPublicProperty1, "Field").WithArguments("Field"),
            Diagnostic(ERRID.ERR_BadAttributeNonPublicProperty1, "Prop").WithArguments("Prop")
    )
        End Sub

        <WorkItem(539101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539101")>
        <Fact()>
        Public Sub BC32000ERR_UseOfLocalBeforeDeclaration1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="LocalVariableCannotBeReferredToBeforeItIsDeclared">
        <file name="a.vb">
        Imports System
        Module X
            Sub foo()
                Dim x as integer
                x = y
                dim y as integer = 1
             End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC32000: Local variable 'y' cannot be referred to before it is declared.
                x = y
                    ~
    </expected>)
        End Sub

        <Fact()>
        Public Sub BC32001ERR_UseOfKeywordFromModule1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="UseOfKeywordFromModule1">
        <file name="a.vb">
        Imports System
        Module X
            Sub foo()
                Dim o = Me
                dim p = MyBase.GetType
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC32001: 'Me' is not valid within a Module.
                Dim o = Me
                        ~~
BC32001: 'MyBase' is not valid within a Module.
                dim p = MyBase.GetType
                        ~~~~~~
</expected>)
        End Sub

        <WorkItem(542958, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542958")>
        <Fact()>
        Public Sub BC32001ERR_UseOfKeywordFromModule1_MeAsAttributeInModule()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="UseOfKeywordFromModule1_MeAsAttributeInModule">
        <file name="a.vb">
        Option Strict On
        Imports System
        Imports System.Runtime.InteropServices
        Public Module S1
            &lt;MyAttribute(Me.color1.blue)&gt;
            Property name As String
            Sub foo()
            End Sub
            Sub main()
            End Sub
            Enum color1
                blue
            End Enum
        End Module
        Class MyAttribute
            Inherits Attribute
            Sub New(x As S1.color1)
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC32001: 'Me' is not valid within a Module.
            &lt;MyAttribute(Me.color1.blue)&gt;
                         ~~
</expected>)
        End Sub

        <WorkItem(542960, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542960")>
        <Fact()>
        Public Sub BC32001ERR_UseOfKeywordFromModule1_MyBaseAsAttributeInModule()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="UseOfKeywordFromModule1_MyBaseAsAttributeInModule">
        <file name="a.vb">
        Option Strict On
        Imports System
        Imports System.Runtime.InteropServices
        Public Module S1
            &lt;MyAttribute(MyBase.color1.blue)&gt;
            Property name As String
            Sub foo()
            End Sub
            Sub main()
            End Sub
            Enum color1
                blue
            End Enum
        End Module
        Class MyAttribute
            Inherits Attribute
            Sub New(x As S1.color1)
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC32001: 'MyBase' is not valid within a Module.
            &lt;MyAttribute(MyBase.color1.blue)&gt;
                         ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC32006ERR_CharToIntegralTypeMismatch1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CharToIntegralTypeMismatch1">
        <file name="a.vb">
        Class C1
            SUB scen1()
                Dim char1() As Char
                Dim bt(2) As Byte
                bt(0) = CType (char1(0), Byte)
                Dim char2() As Char 
                Dim sht1(2) As Short
                sht1(0) = char2(0)
                sht1(0) = CType (char2(0), Short)
            End SUB
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC42104: Variable 'char1' is used before it has been assigned a value. A null reference exception could result at runtime.
                bt(0) = CType (char1(0), Byte)
                               ~~~~~
BC32006: 'Char' values cannot be converted to 'Byte'. Use 'Microsoft.VisualBasic.AscW' to interpret a character as a Unicode value or 'Microsoft.VisualBasic.Val' to interpret it as a digit.
                bt(0) = CType (char1(0), Byte)
                               ~~~~~~~~
BC42104: Variable 'char2' is used before it has been assigned a value. A null reference exception could result at runtime.
                sht1(0) = char2(0)
                          ~~~~~
BC32006: 'Char' values cannot be converted to 'Short'. Use 'Microsoft.VisualBasic.AscW' to interpret a character as a Unicode value or 'Microsoft.VisualBasic.Val' to interpret it as a digit.
                sht1(0) = char2(0)
                          ~~~~~~~~
BC32006: 'Char' values cannot be converted to 'Short'. Use 'Microsoft.VisualBasic.AscW' to interpret a character as a Unicode value or 'Microsoft.VisualBasic.Val' to interpret it as a digit.
                sht1(0) = CType (char2(0), Short)
                                 ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC32006ERR_CharToIntegralTypeMismatch1_1()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="CharToIntegralTypeMismatch1">
        <file name="a.vb">
        Imports System.Linq
        Class C
            Shared Sub Main()
                For Each x As Integer In From c In "abc" Select c
                Next
            End Sub
        End Class
        Public Structure S
        End Structure
    </file>
    </compilation>, {Net40.References.SystemCore})
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC32006: 'Char' values cannot be converted to 'Integer'. Use 'Microsoft.VisualBasic.AscW' to interpret a character as a Unicode value or 'Microsoft.VisualBasic.Val' to interpret it as a digit.
                For Each x As Integer In From c In "abc" Select c
                                         ~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC32007ERR_IntegralToCharTypeMismatch1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CharToIntegralTypeMismatch1">
        <file name="a.vb">
                Imports System
                Module X
                    Sub Foo()
                        Dim O As Object
                        'COMPILEERROR: BC32007, "CUShort(15)"
                        O = CChar(CUShort(15))
                        Dim sb As UShort = CUShort(1)
                        'COMPILEERROR: BC32007, "sb"
                        O = CChar(sb)
                    End Sub
                End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC32007: 'UShort' values cannot be converted to 'Char'. Use 'Microsoft.VisualBasic.ChrW' to interpret a numeric value as a Unicode character or first convert it to 'String' to produce a digit.
                        O = CChar(CUShort(15))
                                  ~~~~~~~~~~~
BC32007: 'UShort' values cannot be converted to 'Char'. Use 'Microsoft.VisualBasic.ChrW' to interpret a numeric value as a Unicode character or first convert it to 'String' to produce a digit.
                        O = CChar(sb)
                                  ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC32008ERR_NoDirectDelegateConstruction1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoDirectDelegateConstruction1">
        <file name="a.vb">
        Class C1
            Public Delegate Sub myDelegate()
            public shared sub mySub()
            end sub
        End Class
        Module M1
            Sub Main()
                Dim d1 As C1.myDelegate
                d1 = New C1.myDelegate()
                d1 = New C1.myDelegate(C1.mySub)
                d1 = New C1.myDelegate(C1.mySub, 23)
                d1 = New C1.myDelegate(addressof C1.mySub, 23)
                d1 = New C1.myDelegate(,)
                d1 = New C1.myDelegate(,,23)
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC32008: Delegate 'C1.myDelegate' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
                d1 = New C1.myDelegate()
                                      ~~
BC32008: Delegate 'C1.myDelegate' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
                d1 = New C1.myDelegate(C1.mySub)
                                      ~~~~~~~~~~
BC32008: Delegate 'C1.myDelegate' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
                d1 = New C1.myDelegate(C1.mySub, 23)
                                      ~~~~~~~~~~~~~~
BC32008: Delegate 'C1.myDelegate' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
                d1 = New C1.myDelegate(addressof C1.mySub, 23)
                                      ~~~~~~~~~~~~~~~~~~~~~~~~
BC32008: Delegate 'C1.myDelegate' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
                d1 = New C1.myDelegate(,)
                                      ~~~
BC32008: Delegate 'C1.myDelegate' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
                d1 = New C1.myDelegate(,,23)
                                      ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC32008ERR_NoDirectDelegateConstruction1_2()
            Dim source =
<compilation name="NewDelegateWithAddressOf">
    <file name="a.vb">
    Imports System
    Delegate Sub D()
    Module Program
        Sub Main(args As String())
            Dim x As D
            x = New D(AddressOf Method, 23)
            x = New D(AddressOf Method, foo:=23)
            x = New D(bar:=AddressOf Method, foo:=23)
            x = New D(bar:=AddressOf Method, bar:=23)
            x = New D()
            x = New D(23)
            x = New D(nothing)
            x = New D

            Dim y1 as New D(AddressOf Method, 23)
            Dim y2 as New D(AddressOf Method, foo:=23)
            Dim y3 as New D(bar:=AddressOf Method, foo:=23)
            Dim y4 as New D(bar:=AddressOf Method, bar:=23)
            Dim y5 as New D()
            Dim y6 as New D(23)
            Dim y7 as New D(nothing)
            Dim y8 as New D
        End Sub
        Public Sub Method()
            console.writeline("Hello.")
        End Sub
    End Module

    </file>
</compilation>

            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source)

            CompilationUtils.AssertTheseDiagnostics(c1,
<expected>
BC32008: Delegate 'D' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
            x = New D(AddressOf Method, 23)
                     ~~~~~~~~~~~~~~~~~~~~~~
BC32008: Delegate 'D' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
            x = New D(AddressOf Method, foo:=23)
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32008: Delegate 'D' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
            x = New D(bar:=AddressOf Method, foo:=23)
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32008: Delegate 'D' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
            x = New D(bar:=AddressOf Method, bar:=23)
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32008: Delegate 'D' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
            x = New D()
                     ~~
BC32008: Delegate 'D' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
            x = New D(23)
                     ~~~~
BC32008: Delegate 'D' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
            x = New D(nothing)
                     ~~~~~~~~~
BC32008: Delegate 'D' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
            x = New D
                ~~~~~
BC32008: Delegate 'D' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
            Dim y1 as New D(AddressOf Method, 23)
                           ~~~~~~~~~~~~~~~~~~~~~~
BC32008: Delegate 'D' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
            Dim y2 as New D(AddressOf Method, foo:=23)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32008: Delegate 'D' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
            Dim y3 as New D(bar:=AddressOf Method, foo:=23)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32008: Delegate 'D' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
            Dim y4 as New D(bar:=AddressOf Method, bar:=23)
                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC32008: Delegate 'D' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
            Dim y5 as New D()
                           ~~
BC32008: Delegate 'D' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
            Dim y6 as New D(23)
                           ~~~~
BC32008: Delegate 'D' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
            Dim y7 as New D(nothing)
                           ~~~~~~~~~
BC32008: Delegate 'D' requires an 'AddressOf' expression or lambda expression as the only argument to its constructor.
            Dim y8 as New D
                      ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC32010ERR_AttrAssignmentNotFieldOrProp1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AttrAssignmentNotFieldOrProp1">
        <file name="at32010.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.All)>
Public Class MyAttribute
    Inherits Attribute
    Public Enum E
        Zero
        One
    End Enum
    Public Sub S()

    End Sub
    Public Function F!()
        Return 0.0F
    End Function
End Class

<My(E:=E.One, S:=Nothing)>
Class Foo
    <My(F!:=1.234F)>
    Public Sub S2()
    End Sub
End Class
    ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_NameNotDeclared1, "E").WithArguments("E"),
                                      Diagnostic(ERRID.ERR_AttrAssignmentNotFieldOrProp1, "E").WithArguments("E"),
                                      Diagnostic(ERRID.ERR_AttrAssignmentNotFieldOrProp1, "S").WithArguments("S"),
                                      Diagnostic(ERRID.ERR_AttrAssignmentNotFieldOrProp1, "F!").WithArguments("F"))

        End Sub

        <Fact()>
        Public Sub BC32013ERR_StrictDisallowsObjectComparison1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="StrictDisallowsObjectComparison1">
        <file name="a.vb">
        Option Strict On
        Class C1
            Sub scen1()
                Dim Obj As Object = New Object
                Select Case obj
                    Case "DFT"
                End Select
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC32013: Option Strict On disallows operands of type Object for operator '='. Use the 'Is' operator to test for object identity.
                Select Case obj
                            ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC32013ERR_StrictDisallowsObjectComparison1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="StrictDisallowsObjectComparison1">
        <file name="a.vb">
        Option Strict On
        Class C1
            Sub scen1()
                Dim Obj As Object = New Object
                if (obj="string")
                End If
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC32013: Option Strict On disallows operands of type Object for operator '='. Use the 'Is' operator to test for object identity.
                if (obj="string")
                    ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC32014ERR_NoConstituentArraySizes()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoConstituentArraySizes">
        <file name="a.vb">
        Option Strict On
        Imports System
        Module Module1
            Sub Main()
                Dim arr10 As Integer(,) = New Integer(9)(5) {} ' Invalid
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC30414: Value of type 'Integer()()' cannot be converted to 'Integer(*,*)' because the array types have different numbers of dimensions.
                Dim arr10 As Integer(,) = New Integer(9)(5) {} ' Invalid
                                          ~~~~~~~~~~~~~~~~~~~~
BC32014: Bounds can be specified only for the top-level array when initializing an array of arrays.
                Dim arr10 As Integer(,) = New Integer(9)(5) {} ' Invalid
                                                         ~
</expected>)
        End Sub

        <WorkItem(545621, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545621")>
        <Fact()>
        Public Sub BC32014ERR_NoConstituentArraySizes1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoConstituentArraySizes">
        <file name="a.vb">
Module M
    Sub Main()
        Dim x()(1)()
    End Sub
End Module

    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC42024: Unused local variable: 'x'.
        Dim x()(1)()
            ~
BC32014: Bounds can be specified only for the top-level array when initializing an array of arrays.
        Dim x()(1)()
                ~
    </expected>)
        End Sub

        <WorkItem(528729, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528729")>
        <Fact()>
        Public Sub BC32016ERR_FunctionResultCannotBeIndexed1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="FunctionResultCannotBeIndexed1">
        <file name="a.vb">
        Imports Microsoft.VisualBasic.FileSystem
        Module M1
            Sub foo()
                If FreeFile(1) &lt; 255 Then
                End If
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC32016: 'Public Function FreeFile() As Integer' has no parameters and its return type cannot be indexed.
                If FreeFile(1) &lt; 255 Then
                   ~~~~~~~~
</expected>)
        End Sub

        <Fact, WorkItem(543658, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543658")>
        Public Sub BC32021ERR_NamedArgAlsoOmitted2()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NamedArgAlsoOmitted2">
        <file name="a.vb">
        Public Module M1
            Public Sub foo(ByVal X As Byte, Optional ByVal Y As Byte = 0, _
                                        Optional ByVal Z As Byte = 0)
                Call foo(6, , Y:=3)
            End Sub
        End Module
    </file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_NamedArgAlsoOmitted2, "Y").WithArguments("Y", "Public Sub foo(X As Byte, [Y As Byte = 0], [Z As Byte = 0])"))

        End Sub

        <Fact()>
        Public Sub BC32022ERR_CannotCallEvent1()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CannotCallEvent1">
        <file name="a.vb">
        Public Module M1
            Public Event BurnPercent(ByVal Percent As Integer)
            Sub foo()
                BurnPercent.Invoke()
            End Sub
        End Module
    </file>
    </compilation>).AssertTheseDiagnostics(
    <expected>
BC32022: 'Public Event BurnPercent(Percent As Integer)' is an event, and cannot be called directly. Use a 'RaiseEvent' statement to raise an event.
                BurnPercent.Invoke()
                ~~~~~~~~~~~
    </expected>)
        End Sub

        <Fact()>
        Public Sub BC32022ERR_CannotCallEvent1_2()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Class C
    Private Event E As System.EventHandler
    Sub M(o As Object)
        E()
        M(E())
    End Sub
End Class
    </file>
    </compilation>).AssertTheseDiagnostics(
    <expected>
BC32022: 'Private Event E As EventHandler' is an event, and cannot be called directly. Use a 'RaiseEvent' statement to raise an event.
        E()
        ~
BC32022: 'Private Event E As EventHandler' is an event, and cannot be called directly. Use a 'RaiseEvent' statement to raise an event.
        M(E())
          ~
    </expected>)
        End Sub

        <Fact()>
        Public Sub BC32023ERR_ForEachCollectionDesignPattern1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ForEachCollectionDesignPattern1">
        <file name="a.vb">
        Option Infer On            
        Public Module M1
            Sub foo()
                Dim s As Integer
                For Each i In s
                Next
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC32023: Expression is of type 'Integer', which is not a collection type.
                For Each i In s
                              ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC32023ERR_ForEachCollectionDesignPattern1_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ForEachCollectionDesignPattern1">
        <file name="a.vb">
        Option Infer On    
        Class C
            Shared Sub Main()
                For Each x As Integer In If(x, x, x)
                Next
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC32023: Expression is of type 'Integer', which is not a collection type.
                For Each x As Integer In If(x, x, x)
                                         ~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC32023ERR_ForEachCollectionDesignPattern1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ForEachCollectionDesignPattern1">
        <file name="a.vb">
Option Infer On
Class C
    Public Shared Sub Main()
        Dim e As New Enumerable()
        For Each x In e
        Next
    End Sub
End Class

Structure Enumerable
    Public i As Integer
    Public Sub GetEnumerator()
        i = i + 1
        'Return New Enumerator()
    End Sub
End Structure

Structure Enumerator
    Private x As Integer
    Public ReadOnly Property Current() As Integer
        Get
            Return x
        End Get
    End Property
    Public Function MoveNext() As Boolean
        Return System.Threading.Interlocked.Increment(x) &lt; 4
    End Function
End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC32023: Expression is of type 'Enumerable', which is not a collection type.
        For Each x In e
                      ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC32023ERR_ForEachCollectionDesignPattern1_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ForEachCollectionDesignPattern1">
        <file name="a.vb">
Option Infer On
Class C
    Public Shared Sub Main()
        Dim e As New Enumerable()
        For Each x In e
        Next
    End Sub
End Class

Structure Enumerable
    Public i As Integer
    private Function GetEnumerator() as Enumerator
        i = i + 1
        Return New Enumerator()
    End Function
End Structure

Structure Enumerator
    Private x As Integer
    Public ReadOnly Property Current() As Integer
        Get
            Return x
        End Get
    End Property
    Public Function MoveNext() As Boolean
        Return System.Threading.Interlocked.Increment(x) &lt; 4
    End Function
End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC32023: Expression is of type 'Enumerable', which is not a collection type.
        For Each x In e
                      ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC32023ERR_ForEachCollectionDesignPattern1_4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ForEachCollectionDesignPattern1">
        <file name="a.vb">
Option Infer On
Class C
    Public Shared Sub Main()
        Dim e As New Enumerable()
        For Each x In e
        Next
    End Sub
End Class

Structure Enumerable
    Public i As Integer
End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC32023: Expression is of type 'Enumerable', which is not a collection type.
        For Each x In e
                      ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC32029ERR_StrictArgumentCopyBackNarrowing3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="StrictArgumentCopyBackNarrowing3">
        <file name="a.vb">
        Option Strict On
        Class C1
            Public intDataField As Integer
            Dim type1 As MyType
            Sub Main()
                Dim str1 As String
                Scen1(intDataField)
                Scen1(type1.intField)
                scen3(str1)
                scen3(type1)
            End Sub
            Sub Scen1(ByRef xx As Long)
            End Sub
            Sub Scen2(ByRef gg As Byte)
            End Sub
            Sub scen3(ByRef gg As Object)
            End Sub
        End Class
        Structure MyType
            Public intField As Short
        End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC32029: Option Strict On disallows narrowing from type 'Long' to type 'Integer' in copying the value of 'ByRef' parameter 'xx' back to the matching argument.
                Scen1(intDataField)
                      ~~~~~~~~~~~~
BC32029: Option Strict On disallows narrowing from type 'Long' to type 'Short' in copying the value of 'ByRef' parameter 'xx' back to the matching argument.
                Scen1(type1.intField)
                      ~~~~~~~~~~~~~~
BC32029: Option Strict On disallows narrowing from type 'Object' to type 'String' in copying the value of 'ByRef' parameter 'gg' back to the matching argument.
                scen3(str1)
                      ~~~~
BC42104: Variable 'str1' is used before it has been assigned a value. A null reference exception could result at runtime.
                scen3(str1)
                      ~~~~
BC32029: Option Strict On disallows narrowing from type 'Object' to type 'MyType' in copying the value of 'ByRef' parameter 'gg' back to the matching argument.
                scen3(type1)
                      ~~~~~        
</expected>)
        End Sub

        <Fact()>
        Public Sub BC32036ERR_NoUniqueConstructorOnBase2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NoUniqueConstructorOnBase2">
        <file name="a.vb">
            Class Base
                Sub New()
                End Sub
                Sub New(ByVal ParamArray a() As Integer)
                End Sub
            End Class
            Class Derived
                Inherits Base
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC32036: Class 'Derived' must declare a 'Sub New' because its base class 'Base' has more than one accessible 'Sub New' that can be called with no arguments.
            Class Derived
                  ~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32038ERR_RequiredNewCallTooMany2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="RequiredNewCallTooMany2">
        <file name="a.vb">
            Class Base
                Sub New()
                End Sub
                Sub New(ByVal ParamArray a() As Integer)
                End Sub
            End Class
            Class Derived
                Inherits Base
                Sub New()
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC32038: First statement of this 'Sub New' must be a call to 'MyBase.New' or 'MyClass.New' because base class 'Base' of 'Derived' has more than one accessible 'Sub New' that can be called with no arguments.
                Sub New()
                    ~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32039ERR_ForCtlVarArraySizesSpecified()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ForCtlVarArraySizesSpecified">
        <file name="a.vb">
            Imports System.Collections.Generic
            Class C1
                Sub New()
                    Dim arrayList As New List(Of Integer())
                    For Each listElement() As Integer In arrayList
                        For Each listElement(1) As Integer In arrayList
                        Next
                    Next
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30616: Variable 'listElement' hides a variable in an enclosing block.
                        For Each listElement(1) As Integer In arrayList
                                 ~~~~~~~~~~~~~~
BC32039: Array declared as for loop control variable cannot be declared with an initial size.
                        For Each listElement(1) As Integer In arrayList
                                 ~~~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32045ERR_TypeOrMemberNotGeneric1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="TypeOrMemberNotGeneric1">
        <file name="a.vb">
            Class Base(Of u)
                sub fun1(ByRef b1 As Integer)
                End sub
            End Class
            Class Derived(Of v)
                Inherits Base(Of v)
                Overloads sub fun1(Of T) (ByVal t1 As T, ByVal ParamArray t2() As UInteger)
                End sub
            End Class
            Friend Module M1F
                Sub FOO()
                    Dim c1 As New Base(Of String)
                    c1.fun1(Of Base(Of String)) (4.4@)
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC32045: 'Public Sub fun1(ByRef b1 As Integer)' has no type parameters and so cannot have type arguments.
                    c1.fun1(Of Base(Of String)) (4.4@)
                           ~~~~~~~~~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32045ERR_TypeOrMemberNotGeneric1_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="TypeOrMemberNotGeneric1">
        <file name="a.vb">
            Class Base(Of u)
                sub fun1(ByRef b1 As Integer)
                End sub
            End Class
            Class Derived(Of v)
                Inherits Base(Of v)
                Overloads sub fun1(Of T) (ByVal t1 As T, ByVal ParamArray t2() As UInteger)
                End sub
            End Class
            Friend Module M1F
                Sub FOO()
                    Dim c1 As New Base(Of String)
                    c1.fun1(Of Derived(Of String)) (3.3!, Nothing)
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC32045: 'Public Sub fun1(ByRef b1 As Integer)' has no type parameters and so cannot have type arguments.
                    c1.fun1(Of Derived(Of String)) (3.3!, Nothing)
                           ~~~~~~~~~~~~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32045ERR_TypeOrMemberNotGeneric1_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="TypeOrMemberNotGeneric1">
        <file name="a.vb">
            Class Base(Of u)
                sub fun1(ByRef b1 As Integer)
                End sub
            End Class
            Class Derived(Of v)
                Inherits Base(Of v)
                Overloads sub fun1(Of T) (ByVal t1 As T, ByVal ParamArray t2() As UInteger)
                End sub
            End Class
            Friend Module M1F
                Sub FOO()
                    Dim c1 As New Base(Of String)
                    c1.fun1(Of System.ValueType) (4.4@, 3US)
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC32045: 'Public Sub fun1(ByRef b1 As Integer)' has no type parameters and so cannot have type arguments.
                    c1.fun1(Of System.ValueType) (4.4@, 3US)
                           ~~~~~~~~~~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32045ERR_TypeOrMemberNotGeneric1_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="TypeOrMemberNotGeneric1">
        <file name="a.vb">
            Class Base(Of u)
                sub fun1(ByRef b1 As Integer)
                End sub
            End Class
            Class Derived(Of v)
                Inherits Base(Of v)
                Overloads sub fun1(Of T) (ByVal t1 As T, ByVal ParamArray t2() As UInteger)
                End sub
            End Class
            Friend Module M1F
                Sub FOO()
                    Dim c1 As Base(Of String) = New Derived(Of String)
                    c1.fun1(Of System.ValueType) (3@)
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC32045: 'Public Sub fun1(ByRef b1 As Integer)' has no type parameters and so cannot have type arguments.
                    c1.fun1(Of System.ValueType) (3@)
                           ~~~~~~~~~~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32045ERR_TypeOrMemberNotGeneric1_4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="TypeOrMemberNotGeneric1">
        <file name="a.vb">
            Class Base(Of u)
                sub fun1(ByRef b1 As Integer)
                End sub
            End Class
            Class Derived(Of v)
                Inherits Base(Of v)
                Overloads sub fun1(Of T) (ByVal t1 As T, ByVal ParamArray t2() As UInteger)
                End sub
            End Class
            Friend Module M1F
                Sub FOO()
                    Dim c1 As Base(Of String) = New Derived(Of String)
                    c1.fun1(Of System.Delegate) ("c"c, 3@)
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC32045: 'Public Sub fun1(ByRef b1 As Integer)' has no type parameters and so cannot have type arguments.
                    c1.fun1(Of System.Delegate) ("c"c, 3@)
                           ~~~~~~~~~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32046ERR_NewIfNullOnGenericParam()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Structure S(Of T, U As New)
    Sub M(Of V)()
        Dim o
        o = New T()
        o = New U()
        o = New V()
    End Sub
End Structure
Class C(Of T1 As Structure, T2 As Class)
    Sub M(Of T3 As Structure, T4 As {Class, New})()
        Dim o
        o = New T1()
        o = New T2()
        o = New T3()
        o = New T4()
    End Sub
End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC32046: 'New' cannot be used on a type parameter that does not have a 'New' constraint.
        o = New T()
                ~
BC32046: 'New' cannot be used on a type parameter that does not have a 'New' constraint.
        o = New V()
                ~
BC32046: 'New' cannot be used on a type parameter that does not have a 'New' constraint.
        o = New T2()
                ~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32050ERR_UnboundTypeParam2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="UnboundTypeParam2">
        <file name="a.vb">
            Option Strict Off
            Public Module M
                Sub Main()
                    Foo(AddressOf Bar)
                End Sub
                Sub Foo(Of T)(ByVal a As System.Action(Of T))
                End Sub
                Sub Bar(ByVal x As String)
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC32050: Type parameter 'T' for 'Public Sub Foo(Of T)(a As Action(Of T))' cannot be inferred.
                    Foo(AddressOf Bar)
                    ~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32052ERR_IsOperatorGenericParam1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface I
End Interface
Class A
End Class
Class B(Of T As Structure)
End Class
Class C(Of T)
    Shared o As Object
    Shared Sub M1(Of T1)(_1 As T1)
        If _1 Is o Then
        End If
        If o Is _1 Then
        End If
    End Sub
    Shared Sub M2(Of T2 As Class)(_2 As T2)
        If _2 Is o Then
        End If
        If o Is _2 Then
        End If
    End Sub
    Shared Sub M3(Of T3 As Structure)(_3 As T3)
        If _3 Is o Then
        End If
        If o Is _3 Then
        End If
    End Sub
    Shared Sub M4(Of T4 As New)(_4 As T4)
        If _4 Is o Then
        End If
        If o Is _4 Then
        End If
    End Sub
    Shared Sub M5(Of T5 As I)(_5 As T5)
        If _5 Is o Then
        End If
        If o Is _5 Then
        End If
    End Sub
    Shared Sub M6(Of T6 As A)(_6 As T6)
        If _6 Is o Then
        End If
        If o Is _6 Then
        End If
    End Sub
    Shared Sub M7(Of T7 As U, U)(_7 As T7)
        If _7 Is o Then
        End If
        If o Is _7 Then
        End If
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC32052: 'Is' operand of type 'T1' can be compared only to 'Nothing' because 'T1' is a type parameter with no class constraint.
        If _1 Is o Then
           ~~
BC32052: 'Is' operand of type 'T1' can be compared only to 'Nothing' because 'T1' is a type parameter with no class constraint.
        If o Is _1 Then
                ~~
BC30020: 'Is' operator does not accept operands of type 'T3'. Operands must be reference or nullable types.
        If _3 Is o Then
           ~~
BC30020: 'Is' operator does not accept operands of type 'T3'. Operands must be reference or nullable types.
        If o Is _3 Then
                ~~
BC32052: 'Is' operand of type 'T4' can be compared only to 'Nothing' because 'T4' is a type parameter with no class constraint.
        If _4 Is o Then
           ~~
BC32052: 'Is' operand of type 'T4' can be compared only to 'Nothing' because 'T4' is a type parameter with no class constraint.
        If o Is _4 Then
                ~~
BC32052: 'Is' operand of type 'T5' can be compared only to 'Nothing' because 'T5' is a type parameter with no class constraint.
        If _5 Is o Then
           ~~
BC32052: 'Is' operand of type 'T5' can be compared only to 'Nothing' because 'T5' is a type parameter with no class constraint.
        If o Is _5 Then
                ~~
BC32052: 'Is' operand of type 'T7' can be compared only to 'Nothing' because 'T7' is a type parameter with no class constraint.
        If _7 Is o Then
           ~~
BC32052: 'Is' operand of type 'T7' can be compared only to 'Nothing' because 'T7' is a type parameter with no class constraint.
        If o Is _7 Then
                ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC32059ERR_OnlyNullLowerBound()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="IsOperatorGenericParam1">
        <file name="a.vb">
            Option Infer On
            Module M
                Sub Main()
                    Dim arr1(0 To 0, 0 To -1) As Integer
                    Dim arr3(-1 To 0, 0 To -1) As Integer 'Invalid
                    Dim arr4(0 To 1, -1 To 0) As Integer 'Invalid
                    Dim arr5(0D To 1, 0.0 To 0) As Integer 'Invalid
                    Dim arr6(0! To 1, 0.0 To 0) As Integer 'Invalid
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC32059: Array lower bounds can be only '0'.
                    Dim arr3(-1 To 0, 0 To -1) As Integer 'Invalid
                             ~~
BC32059: Array lower bounds can be only '0'.
                    Dim arr4(0 To 1, -1 To 0) As Integer 'Invalid
                                     ~~
BC32059: Array lower bounds can be only '0'.
                    Dim arr5(0D To 1, 0.0 To 0) As Integer 'Invalid
                             ~~
BC32059: Array lower bounds can be only '0'.
                    Dim arr5(0D To 1, 0.0 To 0) As Integer 'Invalid
                                      ~~~
BC32059: Array lower bounds can be only '0'.
                    Dim arr6(0! To 1, 0.0 To 0) As Integer 'Invalid
                             ~~
BC32059: Array lower bounds can be only '0'.
                    Dim arr6(0! To 1, 0.0 To 0) As Integer 'Invalid
                                      ~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(542204, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542204")>
        <Fact()>
        Public Sub BC32079ERR_TypeParameterDisallowed()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
            <![CDATA[
            Imports System
            Class AttrCls1
                Inherits Attribute
                Sub New(ByVal p As Type)
                End Sub
            End Class
            Structure S1(Of T)
                Dim i As Integer
                <AttrCls1(GetType(T))> _
                Class Cls1
                End Class
            End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32079: Type parameters or types constructed with type parameters are not allowed in attribute arguments.
                <AttrCls1(GetType(T))> _
                                  ~
                                  ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(542204, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542204")>
        <Fact()>
        Public Sub BC32079ERR_OpenTypeDisallowed()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
            <![CDATA[
            Imports System
            Class AttrCls1
                Inherits Attribute
                Sub New(ByVal p As Type)
                End Sub
            End Class
            Structure S1(Of T)
                Dim i As Integer
                <AttrCls1(GetType(S1(Of T)))> _
                Class A
                End Class
                <AttrCls1(GetType(S1(Of T).A()))> _
                Class B
                End Class
            End Structure
        ]]></file>
    </compilation>)
            Dim expectedErrors1 = <errors><![CDATA[
BC32079: Type parameters or types constructed with type parameters are not allowed in attribute arguments.
                <AttrCls1(GetType(S1(Of T)))> _
                                  ~~~~~~~~
BC32079: Type parameters or types constructed with type parameters are not allowed in attribute arguments.
                <AttrCls1(GetType(S1(Of T).A()))> _
                                  ~~~~~~~~~~~~
                                  ]]></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32085ERR_NewArgsDisallowedForTypeParam()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NewArgsDisallowedForTypeParam">
        <file name="a.vb">
            Module M1
                Sub Sub1(Of T As New)(ByVal a As T)
                    a = New T()
                    a = New T(2)
                    a = New T( 2, 3,
                              4 )
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC32085: Arguments cannot be passed to a 'New' used on a type parameter.
                    a = New T(2)
                              ~
BC32085: Arguments cannot be passed to a 'New' used on a type parameter.
                    a = New T( 2, 3,
                               ~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32087ERR_NoTypeArgumentCountOverloadCand1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoTypeArgumentCountOverloadCand1">
        <file name="a.vb">
            Option Strict Off
            Module M
                Sub Main()
                    Dim x As Object = New Object()
                    x.Equals(Of Integer)()
                    x.Equals(Of Integer) = 1
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC32087: Overload resolution failed because no accessible 'Equals' accepts this number of type arguments.
                    x.Equals(Of Integer)()
                      ~~~~~~~~~~~~~~~~~~
BC32087: Overload resolution failed because no accessible 'Equals' accepts this number of type arguments.
                    x.Equals(Of Integer) = 1
                      ~~~~~~~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32096ERR_ForEachAmbiguousIEnumerable1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Option Strict On

Imports System.Collections
Imports System.Collections.Generic

Class C1
    Implements IEnumerable(Of String), IEnumerable(Of Integer)

    Public Function GetEnumerator_int() As IEnumerator(Of integer) Implements IEnumerable(Of integer).GetEnumerator
        Return Nothing
    End Function
    Public Function GetEnumerator_str() As IEnumerator(Of String) Implements IEnumerable(Of String).GetEnumerator
        Return Nothing
    End Function
    Public Function GetEnumerator1_str() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Class

Class C
    Shared Sub M(Of T1 As C1)(p As T1)
        For Each o As T1 In p
        Next
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC32096: 'For Each' on type 'T1' is ambiguous because the type implements multiple instantiations of 'System.Collections.Generic.IEnumerable(Of T)'.
        For Each o As T1 In p
                            ~
</expected>)
        End Sub

        <WorkItem(3420, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub BC32095ERR_ArrayOfRawGenericInvalid()
            Dim code = <![CDATA[
                Class C1(Of T)
                End Class
                Class C2
                    Public Sub Main()
                        Dim x = GetType(C1(Of )())
                    End Sub
                End Class 
            ]]>.Value

            ParseAndVerify(code, <errors>
                                     <error id="32095"/>
                                 </errors>)
        End Sub

        <WorkItem(543616, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543616")>
        <Fact()>
        Public Sub BC32096ERR_ForEachAmbiguousIEnumerable1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AmbiguousAcrossInterfaces3">
        <file name="a.vb">
Imports System.Collections.Generic
Class C
    Public Shared Sub Main()
    End Sub
End Class
Public Class C(Of T As {IEnumerable(Of Integer), IEnumerable(Of String)})
    Public Shared Sub TestForeach(t As T)
        For Each i As Integer In t
        Next
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC32096: 'For Each' on type 'T' is ambiguous because the type implements multiple instantiations of 'System.Collections.Generic.IEnumerable(Of T)'.
        For Each i As Integer In t
                                 ~
    </expected>)

            ' used to report, but this changed now
            ' "BC30685: 'GetEnumerator' is ambiguous across the inherited interfaces 'System.Collections.Generic.IEnumerable(Of Integer)' 
            ' and 'System.Collections.Generic.IEnumerable(Of String)'."
        End Sub

        <Fact()>
        Public Sub BC32097ERR_IsNotOperatorGenericParam1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface I
End Interface
Class A
End Class
Class B(Of T As Structure)
End Class
Class C(Of T)
    Shared o As Object
    Shared Sub M1(Of T1)(_1 As T1)
        If _1 IsNot o Then
        End If
        If o IsNot _1 Then
        End If
    End Sub
    Shared Sub M2(Of T2 As Class)(_2 As T2)
        If _2 IsNot o Then
        End If
        If o IsNot _2 Then
        End If
    End Sub
    Shared Sub M3(Of T3 As Structure)(_3 As T3)
        If _3 IsNot o Then
        End If
        If o IsNot _3 Then
        End If
    End Sub
    Shared Sub M4(Of T4 As New)(_4 As T4)
        If _4 IsNot o Then
        End If
        If o IsNot _4 Then
        End If
    End Sub
    Shared Sub M5(Of T5 As I)(_5 As T5)
        If _5 IsNot o Then
        End If
        If o IsNot _5 Then
        End If
    End Sub
    Shared Sub M6(Of T6 As A)(_6 As T6)
        If _6 IsNot o Then
        End If
        If o IsNot _6 Then
        End If
    End Sub
    Shared Sub M7(Of T7 As U, U)(_7 As T7)
        If _7 IsNot o Then
        End If
        If o IsNot _7 Then
        End If
    End Sub
End Class
    </file>
</compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC32097: 'IsNot' operand of type 'T1' can be compared only to 'Nothing' because 'T1' is a type parameter with no class constraint.
        If _1 IsNot o Then
           ~~
BC32097: 'IsNot' operand of type 'T1' can be compared only to 'Nothing' because 'T1' is a type parameter with no class constraint.
        If o IsNot _1 Then
                   ~~
BC31419: 'IsNot' requires operands that have reference types, but this operand has the value type 'T3'.
        If _3 IsNot o Then
           ~~
BC31419: 'IsNot' requires operands that have reference types, but this operand has the value type 'T3'.
        If o IsNot _3 Then
                   ~~
BC32097: 'IsNot' operand of type 'T4' can be compared only to 'Nothing' because 'T4' is a type parameter with no class constraint.
        If _4 IsNot o Then
           ~~
BC32097: 'IsNot' operand of type 'T4' can be compared only to 'Nothing' because 'T4' is a type parameter with no class constraint.
        If o IsNot _4 Then
                   ~~
BC32097: 'IsNot' operand of type 'T5' can be compared only to 'Nothing' because 'T5' is a type parameter with no class constraint.
        If _5 IsNot o Then
           ~~
BC32097: 'IsNot' operand of type 'T5' can be compared only to 'Nothing' because 'T5' is a type parameter with no class constraint.
        If o IsNot _5 Then
                   ~~
BC32097: 'IsNot' operand of type 'T7' can be compared only to 'Nothing' because 'T7' is a type parameter with no class constraint.
        If _7 IsNot o Then
           ~~
BC32097: 'IsNot' operand of type 'T7' can be compared only to 'Nothing' because 'T7' is a type parameter with no class constraint.
        If o IsNot _7 Then
                   ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC32098ERR_TypeParamQualifierDisallowed()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="TypeParamQualifierDisallowed">
        <file name="a.vb">
            Class C1(Of T As DataHolder)
                Sub Main()
                    T.Var1 = 4
                End Sub
            End Class
            Class DataHolder
                Public Var1 As Integer
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC32098: Type parameters cannot be used as qualifiers.
                    T.Var1 = 4
                    ~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(545050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545050")>
        Public Sub BC32126ERR_AddressOfNullableMethod()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="AddressOfNullableMethod">
        <file name="a.vb">
            Imports System
            Module M
                Sub Main()
                    Dim x As Integer? = Nothing
                    Dim ef1 As Func(Of Integer) = AddressOf x.GetValueOrDefault
                    Dim ef2 As Func(Of Integer, Integer) = AddressOf x.GetValueOrDefault
                End Sub
            End Module
        </file>
    </compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_AddressOfNullableMethod, "x.GetValueOrDefault").WithArguments("Integer?", "AddressOf"),
            Diagnostic(ERRID.ERR_AddressOfNullableMethod, "x.GetValueOrDefault").WithArguments("Integer?", "AddressOf"))

        End Sub

        <Fact()>
        Public Sub BC32127ERR_IsOperatorNullable1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="IsOperatorNullable1">
        <file name="a.vb">
            Module M1
                Sub FOO()
                    Dim s1_a As S1? = New S1()
                    Dim s1_b As S1? = New S1()
                    Dim B = s1_a Is s1_b
                End Sub
            End Module
            Structure S1
            End Structure
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC32127: 'Is' operand of type 'S1?' can be compared only to 'Nothing' because 'S1?' is a nullable type.
                    Dim B = s1_a Is s1_b
                            ~~~~
BC32127: 'Is' operand of type 'S1?' can be compared only to 'Nothing' because 'S1?' is a nullable type.
                    Dim B = s1_a Is s1_b
                                    ~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC32128ERR_IsNotOperatorNullable1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="IsNotOperatorNullable1">
        <file name="a.vb">
            Module M1
                Sub FOO()
                    Dim s1_a As S1? = New S1()
                    Dim s1_b As S1? = New S1()
                    Dim B = s1_a IsNot s1_b
                End Sub
            End Module
            Structure S1
            End Structure
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC32128: 'IsNot' operand of type 'S1?' can be compared only to 'Nothing' because 'S1?' is a nullable type.
                    Dim B = s1_a IsNot s1_b
                            ~~~~
BC32128: 'IsNot' operand of type 'S1?' can be compared only to 'Nothing' because 'S1?' is a nullable type.
                    Dim B = s1_a IsNot s1_b
                                       ~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(545669, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545669")>
        <Fact()>
        Public Sub BC32303ERR_IllegalCallOrIndex()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="IllegalCallOrIndex">
        <file name="a.vb">
            Class C1
                Function Foo1() as Object
                    return nothing() ' Error
                End Function

                Function Foo2() as Object
                    Return CType(Nothing, Object)() ' Error
                End Function

                Function Foo3() as Object
                    Return DirectCast(TryCast(CType(Nothing, Object), C1), Object)() ' No error
                End Function

                Sub FOO4()
                    Dim testVariable As Object = Nothing(1)
                End Sub

                Sub FOO5()
                    Nothing() ' Error, but a parsing error
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC32303: Illegal call expression or index expression.
                    return nothing() ' Error
                           ~~~~~~~~~
BC32303: Illegal call expression or index expression.
                    Return CType(Nothing, Object)() ' Error
                           ~~~~~~~~~~~~~~~~~~~~~~~~
BC32303: Illegal call expression or index expression.
                    Dim testVariable As Object = Nothing(1)
                                                 ~~~~~~~~~~
BC30035: Syntax error.
                    Nothing() ' Error, but a parsing error
                    ~~~~~~~
                                  </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC33009ERR_ParamArrayIllegal1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="UnacceptableLogicalOperator3">
        <file name="a.vb">
        Delegate Sub FooDel1(ParamArray p() as Integer)
        Delegate Function FooDel2(ParamArray p() as Integer) as Byte
        Module M1
            Sub Main()
            End Sub
        End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC33009: 'Delegate' parameters cannot be declared 'ParamArray'.
Delegate Sub FooDel1(ParamArray p() as Integer)
                     ~~~~~~~~~~
BC33009: 'Delegate' parameters cannot be declared 'ParamArray'.
        Delegate Function FooDel2(ParamArray p() as Integer) as Byte
                                  ~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC33034ERR_UnacceptableLogicalOperator3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="UnacceptableLogicalOperator3">
        <file name="a.vb">
            Imports System
    Class c1
    End Class
    Class c2
            Inherits c1
            Shared Operator And(ByVal x As c2, ByVal y As c1) As c2
            Return New c2
            End Operator
            Shared Operator Or(ByVal x As c2, ByVal y As c2) As c2
                Return New c2
            End Operator
    End Class
        Module M1
            Sub Main()
                Dim o1 As New c2, o2 As New c2, o As Object
                o = o1 AndAlso o2
                o = o1 OrElse o2
            End Sub
        End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC33034: Return and parameter types of 'Public Shared Operator And(x As c2, y As c1) As c2' must be 'c2' to be used in a 'AndAlso' expression.
                o = o1 AndAlso o2
                    ~~~~~~~~~~~~~
BC33035: Type 'c2' must define operator 'IsTrue' to be used in a 'OrElse' expression.
                o = o1 OrElse o2
                    ~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC33037ERR_CopyBackTypeMismatch3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CopyBackTypeMismatch3">
        <file name="a.vb">
        Class c2
            Shared Widening Operator CType(ByVal x As c2) As Integer
                Return 9
            End Operator
        End Class
        Module M1
            Sub FOO()
                Dim o As New c2
                FOO(o)
            End Sub
            Sub foo(ByRef x As Integer)
                x = 99
            End Sub
        End Module
        </file>
    </compilation>)

            compilation1.VerifyDiagnostics(Diagnostic(ERRID.ERR_CopyBackTypeMismatch3, "o").WithArguments("x", "Integer", "c2"))
        End Sub

        ' Roslyn extra errors (last 3)
        <Fact()>
        Public Sub BC33038ERR_ForLoopOperatorRequired2()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ForLoopOperatorRequired2">
        <file name="a.vb">
        Class S1
            Public Shared Widening Operator CType(ByVal x As Integer) As S1
                Return Nothing
            End Operator
            Sub foo()
                Dim v As S1
                For i = 1 To v
                Next
            End Sub
        End Class
        </file>
    </compilation>).VerifyDiagnostics(
    Diagnostic(ERRID.ERR_ForLoopOperatorRequired2, "For i = 1 To v").WithArguments("S1", "+"),
    Diagnostic(ERRID.ERR_ForLoopOperatorRequired2, "For i = 1 To v").WithArguments("S1", "-"),
    Diagnostic(ERRID.ERR_ForLoopOperatorRequired2, "For i = 1 To v").WithArguments("S1", "<="),
    Diagnostic(ERRID.ERR_ForLoopOperatorRequired2, "For i = 1 To v").WithArguments("S1", ">="),
    Diagnostic(ERRID.WRN_DefAsgUseNullRef, "v").WithArguments("v"))

        End Sub

        ' Roslyn extra errors (last 2)
        <Fact()>
        Public Sub BC33039ERR_UnacceptableForLoopOperator2()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="UnacceptableForLoopOperator2">
        <file name="a.vb">
        Module M1
            Class C1(Of t)
                Shared Widening Operator CType(ByVal p1 As C1(Of t)) As Integer
                    Return 1
                End Operator
                Shared Widening Operator CType(ByVal p1 As Integer) As C1(Of t)
                    Return Nothing
                End Operator
                Shared Operator -(ByVal p1 As C1(Of t), ByVal p2 As C1(Of t)) As C1(Of Short)
                    Return Nothing
                End Operator
                Shared Operator +(ByVal p1 As C1(Of t), ByVal p2 As C1(Of t)) As C1(Of Integer)
                    Return Nothing
                End Operator
            End Class
            Sub foo()
                For i As C1(Of Integer) = 1 To 10
                Next
            End Sub
        End Module
        </file>
    </compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_UnacceptableForLoopOperator2, "For i As C1(Of Integer) = 1 To 10").WithArguments("Public Shared Operator -(p1 As M1.C1(Of Integer), p2 As M1.C1(Of Integer)) As M1.C1(Of Short)", "M1.C1(Of Integer)"),
            Diagnostic(ERRID.ERR_ForLoopOperatorRequired2, "For i As C1(Of Integer) = 1 To 10").WithArguments("M1.C1(Of Integer)", "<="),
            Diagnostic(ERRID.ERR_ForLoopOperatorRequired2, "For i As C1(Of Integer) = 1 To 10").WithArguments("M1.C1(Of Integer)", ">="))

        End Sub

        <Fact()>
        Public Sub BC33107ERR_IllegalCondTypeInIIF()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="IllegalCondTypeInIIF">
        <file name="a.vb">
        Option Infer On
        Imports System
        Class S1
            Sub foo()
                Dim choice1 = 4
                Dim choice2 = 5
                Dim booleanVar = True
                Console.WriteLine(If(choice1 &lt; choice2, 1))
                Console.WriteLine(If(booleanVar, "Test returns True."))
            End Sub
        End Class
        </file>
    </compilation>)

            Dim expectedErrors1 = <errors>
BC33107: First operand in a binary 'If' expression must be a nullable value type, a reference type, or an unconstrained generic type.
                Console.WriteLine(If(choice1 &lt; choice2, 1))
                                     ~~~~~~~~~~~~~~~~~
BC33107: First operand in a binary 'If' expression must be a nullable value type, a reference type, or an unconstrained generic type.
                Console.WriteLine(If(booleanVar, "Test returns True."))
                                     ~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC33100ERR_CantSpecifyNullableOnBoth()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="CantSpecifyNullableOnBoth">
        <file name="a.vb">
            Class C1
                Sub foo()
                    Dim z? As Integer?
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42024: Unused local variable: 'z'.
                    Dim z? As Integer?
                        ~
BC33100: Nullable modifier cannot be specified on both a variable and its type.
                    Dim z? As Integer?
                           ~~~~~~~~~~~                                
     </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36006ERR_BadAttributeConstructor2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BadAttributeConstructor2">
        <file name="a.vb"><![CDATA[
Imports System

        Public Class MyAttr
            Inherits Attribute
            Public Sub New(ByVal i As Integer, ByRef V As Integer)
            End Sub
        End Class
        <MyAttr(1, 1)> Public Class MyTest
        End Class
    ]]></file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_BadAttributeConstructor2, "MyAttr").WithArguments("Integer"))
        End Sub

        <Fact()>
        Public Sub BC36009ERR_GotoIntoUsing()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="GotoIntoUsing">
        <file name="a.vb">
            Imports System

            Class C1
                Sub foo()
                    If (True)
                        GoTo label1
                    End If
                    Using o as IDisposable = nothing
            label1:
                    End Using
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC36009: 'GoTo label1' is not valid because 'label1' is inside a 'Using' statement that does not contain this statement.
                        GoTo label1
                             ~~~~~~
     </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC36010ERR_UsingRequiresDisposePattern()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UsingRequiresDisposePattern">
        <file name="a.vb">
        Class [IsNot]
            Public i As Integer
        End Class
        Class C1
            Sub FOO()
                Dim c1 as [IsNot] = nothing, c2 As [IsNot] = nothing
                Using c1 IsNot c2
                End Using

                Using new [IsNot]()
                End Using

                Using c3 As New [IsNot]()
                End Using

                Using c4 As [IsNot] = new [IsNot]()
                End Using
            End Sub
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36010: 'Using' operand of type 'Boolean' must implement 'System.IDisposable'.
                Using c1 IsNot c2
                      ~~~~~~~~~~~
BC36010: 'Using' operand of type '[IsNot]' must implement 'System.IDisposable'.
                Using new [IsNot]()
                      ~~~~~~~~~~~~~
BC36010: 'Using' operand of type '[IsNot]' must implement 'System.IDisposable'.
                Using c3 As New [IsNot]()
                      ~~~~~~~~~~~~~~~~~~~
BC36010: 'Using' operand of type '[IsNot]' must implement 'System.IDisposable'.
                Using c4 As [IsNot] = new [IsNot]()
                      ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36011ERR_UsingResourceVarNeedsInitializer()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UsingResourceVarNeedsInitializer">
        <file name="a.vb">
Option Strict On
Option Infer Off
Option Explicit Off

Imports System

Class MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Class C1
    Public Shared Sub Main()
        Using foo As MyDisposable
            Console.WriteLine("Inside Using.")
        End Using

        Using foo2 As New MyDisposable()
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
End Class  
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36011: 'Using' resource variable must have an explicit initialization.
        Using foo As MyDisposable
              ~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36012ERR_UsingResourceVarCantBeArray()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UsingResourceVarNeedsInitializer">
        <file name="a.vb">
Option Strict On
Option Infer Off
Option Explicit Off

Imports System

Structure MyDisposable
    Implements IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure

Class C1
    Public Shared Sub Main()
        Using foo?() As MyDisposable = Nothing
            Console.WriteLine("Inside Using.")
        End Using
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36012: 'Using' resource variable type can not be array type.
        Using foo?() As MyDisposable = Nothing
              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36532ERR_LambdaBindingMismatch1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="LambdaBindingMismatch1">
        <file name="a.vb">
        Option Strict Off 
        Imports System
        Public Module M
            Sub Main()
                Dim x As Func(Of Exception, Object) = Function(y$) y
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36532: Nested function does not have the same signature as delegate 'Func(Of Exception, Object)'.
                Dim x As Func(Of Exception, Object) = Function(y$) y
                                                      ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36533ERR_CannotLiftByRefParamQuery1()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="CannotLiftByRefParamQuery1">
        <file name="a.vb">
        Option Strict Off
        Imports System.Linq
        Imports System.Collections.Generic
        Public Module M
            Sub RunQuery(ByVal collection As List(Of Integer), _
                     ByRef filterValue As Integer)
                Dim queryResult = From num In collection _
                                  Where num &lt; filterValue
            End Sub
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36533: 'ByRef' parameter 'filterValue' cannot be used in a query expression.
                                  Where num &lt; filterValue
                                              ~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36534ERR_ExpressionTreeNotSupported_NoError()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="ExpressionTreeNotSupported">
        <file name="a.vb">
        Imports System.Runtime.CompilerServices 
        Imports System.Linq.Expressions
        Imports System
        Module M
            Sub Main()
                Dim x As Expression(Of Func(Of Action)) = Function() AddressOf 0.Foo
            End Sub
            &lt;Extension()&gt;
            Sub Foo(ByVal x As Integer)
                x = Nothing
            End Sub
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore})
            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact()>
        Public Sub BC36534ERR_ExpressionTreeNotSupported()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="ExpressionTreeNotSupported">
        <file name="a.vb">
        Imports System.Runtime.CompilerServices 
        Imports System.Linq.Expressions
        Imports System
        Module M
            Sub Main()
                Dim a As Integer
                Dim x As Expression(Of Func(Of Action)) = Function() Sub() a = 1
            End Sub
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore})
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36534: Expression cannot be converted into an expression tree.
                Dim x As Expression(Of Func(Of Action)) = Function() Sub() a = 1
                                                                           ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36535ERR_CannotLiftStructureMeQuery()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="CannotLiftStructureMeQuery">
        <file name="a.vb">
        Option Infer On
        Imports System.Linq
        Structure S1
            Sub test()
                Dim col = New Integer() {1, 2, 3}
                Dim x = From i In col Let j = Me
            End Sub
        End Structure
    </file>
    </compilation>, {Net40.References.SystemCore})
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36535: Instance members and 'Me' cannot be used within query expressions in structures.
                Dim x = From i In col Let j = Me
                                              ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36535ERR_CannotLiftStructureMeQuery_2()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="CannotLiftStructureMeQuery_2">
        <file name="a.vb">
        Option Infer On
        Imports System.Linq
        Structure S1
            Sub test()
                Dim col = New Integer() {1, 2, 3}
                Dim x = From i In col Let j = MyClass.ToString()
            End Sub
        End Structure
    </file>
    </compilation>, {Net40.References.SystemCore})
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36535: Instance members and 'Me' cannot be used within query expressions in structures.
                Dim x = From i In col Let j = MyClass.ToString()
                                              ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36536ERR_InferringNonArrayType1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InferringNonArrayType1">
        <file name="a.vb">
        Option Strict Off
        Option Infer On
        Module M
            Sub Main()
                Dim x() = 1
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36536: Variable cannot be initialized with non-array type 'Integer'.
                Dim x() = 1
                    ~
BC30311: Value of type 'Integer' cannot be converted to 'Object()'.
                Dim x() = 1
                          ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36538ERR_ByRefParamInExpressionTree()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="ByRefParamInExpressionTree">
        <file name="a.vb">
        Imports System
        Imports System.Linq.Expressions
        Structure S1
            Sub test()
                Foo(Function(ByRef x As Double, y As Integer) 1.1)
            End Sub
        End Structure
        Module Module1
            Delegate Function MyFunc(Of T)(ByRef x As T, ByVal y As T) As T
            Sub Foo(Of T)(ByVal x As Expression(Of MyFunc(Of T)))
            End Sub
        End Module
    </file>
    </compilation>, references:={Net40.References.SystemCore})
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36538: References to 'ByRef' parameters cannot be converted to an expression tree.
                Foo(Function(ByRef x As Double, y As Integer) 1.1)
                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36547ERR_DuplicateAnonTypeMemberName1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="DuplicateAnonTypeMemberName1">
        <file name="a.vb">
        Module M 
            Sub Main()
                Dim at1 = New With {"".ToString}
                Dim at2 = New With {Key .a = 1, .GetHashCode = "A"}
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36547: Anonymous type member or property 'ToString' is already declared.
                Dim at1 = New With {"".ToString}
                                    ~~~~~~~~~~~
BC36547: Anonymous type member or property 'GetHashCode' is already declared.
                Dim at2 = New With {Key .a = 1, .GetHashCode = "A"}
                                                ~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36548ERR_BadAnonymousTypeForExprTree()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="BadAnonymousTypeForExprTree">
        <file name="a.vb">
        Imports System
        Imports System.Linq.Expressions
        Module M
            Sub Main()
                Dim x As Expression(Of Func(Of Object)) = Function() New With {.x = 1, .y = .x}
            End Sub
        End Module
    </file>
    </compilation>, {Net40.References.System, Net40.References.SystemCore, Net40.References.MicrosoftVisualBasic})
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36548: Cannot convert anonymous type to an expression tree because a property of the type is used to initialize another property.
                Dim x As Expression(Of Func(Of Object)) = Function() New With {.x = 1, .y = .x}
                                                                                            ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36549ERR_CannotLiftAnonymousType1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="CannotLiftAnonymousType1">
        <file name="a.vb">
        Imports System.Linq
        Module M
            Dim x = New With {.y = 1, .z = From y In "" Select .y}
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore})

            CompilationUtils.AssertTheseEmitDiagnostics(compilation,
    <expected>
BC36549: Anonymous type property 'y' cannot be used in the definition of a lambda expression within the same initialization list.
            Dim x = New With {.y = 1, .z = From y In "" Select .y}
                                                               ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36557ERR_NameNotMemberOfAnonymousType2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NameNotMemberOfAnonymousType2">
        <file name="a.vb">
        Structure S1
            Sub test()
                Dim x = New With {.prop1 = New With {.prop2 = .prop1}}
            End Sub
        End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36557: 'prop1' is not a member of '&lt;anonymous type&gt;'; it does not exist in the current context.
                Dim x = New With {.prop1 = New With {.prop2 = .prop1}}
                                                              ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36558ERR_ExtensionAttributeInvalid()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System

Namespace System.Runtime.CompilerServices
    <AttributeUsage(AttributeTargets.Method Or AttributeTargets.Property Or AttributeTargets.Class)>
    Public Class ExtensionAttribute
        Inherits Attribute
    End Class
End Namespace

<System.Runtime.CompilerServices.Extension()> ' 1
Module Program

    <System.Runtime.CompilerServices.Extension()> ' 2
    Sub boo(n As String)

    End Sub

    <System.Runtime.CompilerServices.Extension()> ' 3
    Delegate Sub b()

    Sub Main(args As String())

    End Sub
End Module

<System.Runtime.CompilerServices.Extension()> ' 4
Class T
    <System.Runtime.CompilerServices.Extension()> ' 5
    Sub boo(n As String)
    End Sub
End Class
]]>
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
        <![CDATA[
BC36558: The custom-designed version of 'System.Runtime.CompilerServices.ExtensionAttribute' found by the compiler is not valid. Its attribute usage flags must be set to allow assemblies, classes, and methods.
<System.Runtime.CompilerServices.Extension()> ' 1
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36558: The custom-designed version of 'System.Runtime.CompilerServices.ExtensionAttribute' found by the compiler is not valid. Its attribute usage flags must be set to allow assemblies, classes, and methods.
    <System.Runtime.CompilerServices.Extension()> ' 2
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30662: Attribute 'ExtensionAttribute' cannot be applied to 'b' because the attribute is not valid on this declaration type.
    <System.Runtime.CompilerServices.Extension()> ' 3
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36558: The custom-designed version of 'System.Runtime.CompilerServices.ExtensionAttribute' found by the compiler is not valid. Its attribute usage flags must be set to allow assemblies, classes, and methods.
<System.Runtime.CompilerServices.Extension()> ' 4
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36550: 'Extension' attribute can be applied only to 'Module', 'Sub', or 'Function' declarations.
Class T
      ~
BC36551: Extension methods can be defined only in modules.
    <System.Runtime.CompilerServices.Extension()> ' 5
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36558: The custom-designed version of 'System.Runtime.CompilerServices.ExtensionAttribute' found by the compiler is not valid. Its attribute usage flags must be set to allow assemblies, classes, and methods.
    <System.Runtime.CompilerServices.Extension()> ' 5
     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>
    </expected>)
        End Sub

        <Fact()>
        Public Sub BC36559ERR_AnonymousTypePropertyOutOfOrder1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="AnonymousTypePropertyOutOfOrder1">
        <file name="a.vb">
        Imports System.Runtime.CompilerServices
        Module M
            Sub Foo()
                Dim x = New With {.X = .X}
            End Sub
            &lt;Extension()&gt;
            Function X(ByVal y As Object) As Object
                Return Nothing
            End Function
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore})
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36559: Anonymous type member property 'X' cannot be used to infer the type of another member property because the type of 'X' is not yet established.
                Dim x = New With {.X = .X}
                                       ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36560ERR_AnonymousTypeDisallowsTypeChar()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="AnonymousTypeDisallowsTypeChar">
        <file name="a.vb">
        Imports System.Runtime.CompilerServices
        Module M
            Sub Foo()
                Dim anon1 = New With {.ID$ = "abc"}
                Dim anon2 = New With {.ID$ = 42}
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36560: Type characters cannot be used in anonymous type declarations.
                Dim anon1 = New With {.ID$ = "abc"}
                                      ~~~~~~~~~~~~
BC36560: Type characters cannot be used in anonymous type declarations.
                Dim anon2 = New With {.ID$ = 42}
                                      ~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36564ERR_DelegateBindingTypeInferenceFails()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="DelegateBindingTypeInferenceFails">
        <file name="a.vb">
        Option Strict Off
Imports System
        Public Module M
            Sub Main()
                Dim k = {Sub() Console.WriteLine(), AddressOf Foo} 
            End Sub
            Sub Foo(Of T)()
            End Sub
        End Module
    </file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_DelegateBindingTypeInferenceFails, "Foo"))

        End Sub

        <Fact()>
        Public Sub BC36574ERR_AnonymousTypeNeedField()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="AnonymousTypeNeedField">
        <file name="a.vb">
        Public Module M
            Sub Main()
                Dim anonInstance = New With {}
            End Sub
        End Module
    </file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_AnonymousTypeNeedField, "{"))

        End Sub

        <Fact()>
        Public Sub BC36582ERR_TooManyArgs2()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="TooManyArgs2">
        <file name="a.vb">
        Module M1
            Function IntegerExtension(ByVal a As Integer) As Integer
                Dim x1 As Integer
                x1.FooGeneric01(1)
                return nothing
            End Function
        End Module
        Module Extension01
            &lt;System.Runtime.CompilerServices.Extension()&gt; Sub FooGeneric01(Of T1)(ByVal o As T1)
            End Sub
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36582: Too many arguments to extension method 'Public Sub FooGeneric01()' defined in 'Extension01'.
                x1.FooGeneric01(1)
                                ~
</expected>)
        End Sub

        <Fact(), WorkItem(543658, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543658")>
        Public Sub BC36583ERR_NamedArgAlsoOmitted3()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="NamedArgAlsoOmitted3">
        <file name="a.vb">
        Imports System.Runtime.CompilerServices
        Public Module M
            &lt;Extension()&gt; _
            Public Sub ABC(ByVal X As Integer, Optional ByVal Y As Byte = 0, _
                       Optional ByVal Z As Byte = 0)
            End Sub
            Sub FOO()
                Dim number As Integer
                number.ABC(, 4, Y:=5)
            End Sub
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore}).VerifyDiagnostics(Diagnostic(ERRID.ERR_NamedArgAlsoOmitted3, "Y").WithArguments("Y", "Public Sub ABC([Y As Byte = 0], [Z As Byte = 0])", "M"))
        End Sub

        <Fact()>
        Public Sub BC36584ERR_NamedArgUsedTwice3()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="NamedArgUsedTwice3">
        <file name="a.vb">
        Imports System.Runtime.CompilerServices
        Public Module M
            &lt;Extension()&gt; _
            Public Sub ABC(ByVal X As Integer, ByVal Y As Byte, _
                       Optional ByVal Z As Byte = 0)
            End Sub
            Sub FOO()
                Dim number As Integer
                number.ABC(1, Y:=4, Y:=5)
            End Sub
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore}).VerifyDiagnostics(Diagnostic(ERRID.ERR_NamedArgUsedTwice3, "Y").WithArguments("Y", "Public Sub ABC(Y As Byte, [Z As Byte = 0])", "M"),
    Diagnostic(ERRID.ERR_NamedArgUsedTwice3, "Y").WithArguments("Y", "Public Sub ABC(Y As Byte, [Z As Byte = 0])", "M"))

        End Sub

        <Fact()>
        Public Sub BC36589ERR_UnboundTypeParam3()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="UnboundTypeParam3">
        <file name="a.vb">
        Delegate Function Func1(Of T0, S)(ByVal arg1 As T0) As S
        Delegate Sub Func2s(Of T0, T1, S)(ByVal arg1 As T0, ByVal arg2 As T1)
        Class C1
            Function [Select](ByVal sel As Func1(Of Integer, Integer)) As C1
                Return Me
            End Function
        End Class
        Module M1
            &lt;System.Runtime.CompilerServices.Extension()&gt; _
            Function GroupJoin(Of TKey, TResult)(ByVal col As C1, ByVal coll2 As C1, ByVal key As Func1(Of Integer, TKey), ByVal key2 As Func1(Of Integer, TKey), ByVal resultSelector As Func2s(Of Integer, System.Collections.Generic.IEnumerable(Of Integer), TResult)) As System.Collections.Generic.IEnumerable(Of TResult)
                Return Nothing
            End Function
        End Module
        Module M2
            Sub foo()
                Dim x = New C1
                Dim y = From i In x Select i Group Join j In x On i Equals j Into G = Group, Count() 
            End Sub
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore}).VerifyDiagnostics(Diagnostic(ERRID.ERR_QueryOperatorNotFound, "Group Join").WithArguments("GroupJoin"))

        End Sub

        <Fact()>
        Public Sub BC36590ERR_TooFewGenericArguments2()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="TooFewGenericArguments2">
        <file name="a.vb">
        Module M1
            Sub foo()
                Dim x As cLSIfooClass(Of Integer)
                x.foo(Of String)("RR")
            End Sub
        End Module
        Module Extension01
            &lt;System.Runtime.CompilerServices.Extension()&gt; Sub foo(Of T1, t2, t3)(ByVal o As T1, ByVal p As t2, ByVal q As t3)
            End Sub
        End Module
        Public Interface Ifoo(Of t)
        End Interface
        Class cLSIfooClass(Of T)
            Implements Ifoo(Of T)
        End Class
    </file>
    </compilation>, {Net40.References.SystemCore})
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.
                x.foo(Of String)("RR")
                ~
BC36590: Too few type arguments to extension method 'Public Sub foo(Of t2, t3)(p As t2, q As t3)' defined in 'Extension01'.
                x.foo(Of String)("RR")
                     ~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36591ERR_TooManyGenericArguments2()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="TooManyGenericArguments2">
        <file name="a.vb">
        Module M1
            Sub foo()
                Dim x As cLSIfooClass(Of Integer)
                x.foo(Of Integer, String)("RR")
            End Sub
        End Module
        Module Extension01
            &lt;System.Runtime.CompilerServices.Extension()&gt; Sub foo(Of T1, t2)(ByVal o As Ifoo(Of T1), ByVal p As t2)
            End Sub
        End Module
        Public Interface Ifoo(Of t)
        End Interface
        Class cLSIfooClass(Of T)
            Implements Ifoo(Of T)
        End Class
    </file>
    </compilation>, {Net40.References.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.
                x.foo(Of Integer, String)("RR")
                ~
BC36591: Too many type arguments to extension method 'Public Sub foo(Of t2)(p As t2)' defined in 'Extension01'.
                x.foo(Of Integer, String)("RR")
                     ~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36593ERR_ExpectedQueryableSource()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ExpectedQueryableSource">
        <file name="a.vb">
        Structure S1
            Public Sub GetData()
                Dim query = From number In Me
            End Sub
        End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36593: Expression of type 'S1' is not queryable. Make sure you are not missing an assembly reference and/or namespace import for the LINQ provider.
                Dim query = From number In Me
                                           ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36594ERR_QueryOperatorNotFound()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="QueryOperatorNotFound">
        <file name="a.vb">
        Imports System
        Imports System.Collections.Generic
        Imports System.Linq
        Module M
            Sub Main()
                Dim x = (Aggregate y In {Function(e) 1} Into y())
            End Sub
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore})
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36594: Definition of method 'y' is not accessible in this context.
                Dim x = (Aggregate y In {Function(e) 1} Into y())
                                                             ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36597ERR_CannotGotoNonScopeBlocksWithClosure()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CannotGotoNonScopeBlocksWithClosure">
        <file name="a.vb">
        Module M
            Sub BadGoto()
                Dim x = 0

                If x > 5 Then
        Label1:
                    Dim y = 5
                    Dim f = Function() y
                End If

                GoTo Label1
            End Sub
        End Module
    </file>
    </compilation>)
            AssertTheseEmitDiagnostics(compilation,
<expected>
BC36597: 'Goto Label1' is not valid because 'Label1' is inside a scope that defines a variable that is used in a lambda or query expression.
                GoTo Label1
                ~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36599ERR_QueryAnonymousTypeFieldNameInference()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="QueryAnonymousTypeFieldNameInference">
        <file name="a.vb">
        Option Strict On
        Imports System.Linq
        Module M
            Sub Foo()
                From x In {""} Select x, Nothing
            End Sub
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore})

            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_QueryAnonymousTypeFieldNameInference, "Nothing"),
                Diagnostic(ERRID.ERR_ExpectedProcedure, "From x In {""""} Select x, Nothing")) ' Extra in Roslyn & NOT in vbc
        End Sub

        <Fact()>
        Public Sub BC36600ERR_QueryDuplicateAnonTypeMemberName1()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="QueryDuplicateAnonTypeMemberName1">
        <file name="a.vb">
        Option Strict On
        Imports System
        Imports System.Runtime.CompilerServices
        Imports System.Linq
        Module M
            Sub Foo()
                Dim y = From x In ""
                        Group By x
                        Into Bar(), Bar(1)
            End Sub
            &lt;Extension()&gt;
            Function Bar(ByVal x As Object, ByVal y As Func(Of Char, Object)) As String
                Return Nothing
            End Function
            &lt;Extension()&gt;
            Function Bar(ByVal x As Object) As String
                Return Nothing
            End Function
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore}).VerifyDiagnostics(
    Diagnostic(ERRID.ERR_QueryDuplicateAnonTypeMemberName1, "Bar").WithArguments("Bar"))

        End Sub

        <Fact()>
        Public Sub BC36601ERR_QueryAnonymousTypeDisallowsTypeChar()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="QueryAnonymousTypeDisallowsTypeChar">
        <file name="a.vb">
        Option Infer On
        Imports System.Linq
        Module M
            Dim q = From x% In New Integer() {1}
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36601: Type characters cannot be used in range variable declarations.
            Dim q = From x% In New Integer() {1}
                         ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36602ERR_ReadOnlyInClosure()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ReadOnlyInClosure">
        <file name="a.vb">
        Class Class1
            ReadOnly m As Integer
            Sub New()
                Dim f = Function() Test(m)
            End Sub
            Function Test(ByRef n As Integer) As String
                Return Nothing
            End Function
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
                Dim f = Function() Test(m)
                                        ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36602ERR_ReadOnlyInClosure1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ReadOnlyInClosure">
        <file name="a.vb">
        Class Class1
            ReadOnly property m As Integer
            Sub New()
                Dim f = Function() Test(m)
            End Sub
            Function Test(ByRef n As Integer) As String
                Return Nothing
            End Function
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
                Dim f = Function() Test(m)
                                        ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36603ERR_ExprTreeNoMultiDimArrayCreation()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="ExprTreeNoMultiDimArrayCreation">
        <file name="a.vb">
        Imports System
        Imports System.Linq.Expressions
        Module M
            Sub Main()
                Dim ex As Expression(Of Func(Of Object)) = Function() {{1}}
            End Sub
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore})
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36603: Multi-dimensional array cannot be converted to an expression tree.
                Dim ex As Expression(Of Func(Of Object)) = Function() {{1}}
                                                                      ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36604ERR_ExprTreeNoLateBind()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="ExprTreeNoLateBind">
        <file name="a.vb">
        Imports System
        Imports System.Linq.Expressions
        Module M
            Sub Main()
                Dim ex As Expression(Of Func(Of Object, Object)) = Function(x) x.Foo
            End Sub
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore})
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36604: Late binding operations cannot be converted to an expression tree.
                Dim ex As Expression(Of Func(Of Object, Object)) = Function(x) x.Foo
                                                                               ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36606ERR_QueryInvalidControlVariableName1()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
    <compilation name="QueryInvalidControlVariableName1">
        <file name="a.vb">
        Option Infer On
        Imports System.Linq
        Class M
            Dim x = From y In "" Select ToString()
        End Class
    </file>
    </compilation>, {Net40.References.SystemCore})
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36606: Range variable name cannot match the name of a member of the 'Object' class.
            Dim x = From y In "" Select ToString()
                                        ~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36610ERR_QueryNameNotDeclared()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(references:={Net40.References.SystemCore}, source:=
    <compilation name="QueryNameNotDeclared">
        <file name="a.vb">
Imports System
Imports System.Linq

        Module M
            Sub Main()
                Dim c = From x In {1} Group By y = 0, z = y Into Count()
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36610: Name 'y' is either not declared or not in the current scope.
                Dim c = From x In {1} Group By y = 0, z = y Into Count()
                                                          ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36614ERR_QueryAnonTypeFieldXMLNameInference()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
        Imports System.Xml.Linq
        Imports System.Collections.Generic
        Imports System.Linq
        Imports <xmlns:ns="5">
        Imports <xmlns:n-s="b">
        Module M1
            Sub foo()
                Dim x = From i In (<ns:e <%= <ns:e><%= "hello" %></ns:e> %>></ns:e>.<ns:e>) _
                                            Where i.Value <> (<<%= <ns:e></ns:e>.Name %>></>.Value) _
                                            Select <e><%= i %></e>.<ns:e-e>, i
            End Sub
        End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC36614: Range variable name cannot be inferred from an XML identifier that is not a valid Visual Basic identifier.
                                            Select <e><%= i %></e>.<ns:e-e>, i
                                                                       ~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC36617ERR_TypeCharOnAggregation()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="TypeCharOnAggregation">
        <file name="a.vb">
        Option Strict On
        Imports System.Runtime.CompilerServices
        Imports System
        Imports System.Linq
        Module M
            Sub Foo()
                Dim y = From x In ""
                        Group By x
                        Into Bar$(1)
            End Sub
            &lt;Extension()&gt;
            Function Bar$(ByVal x As Object, ByVal y As Func(Of Char, Object))
                Return Nothing
            End Function
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore}).VerifyDiagnostics(Diagnostic(ERRID.ERR_TypeCharOnAggregation, "Bar$"),
    Diagnostic(ERRID.ERR_QueryAnonymousTypeDisallowsTypeChar, "Bar$"))

        End Sub

        <Fact()>
        Public Sub BC36625ERR_LambdaNotDelegate1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="LambdaNotDelegate1">
        <file name="a.vb">
        Module LambdaSyntax
            Sub LambdaSyntax()
                If Function() True Then
                ElseIf Function(x As Boolean) x Then
                End If
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36625: Lambda expression cannot be converted to 'Boolean' because 'Boolean' is not a delegate type.
                If Function() True Then
                   ~~~~~~~~~~~~~~~
BC36625: Lambda expression cannot be converted to 'Boolean' because 'Boolean' is not a delegate type.
                ElseIf Function(x As Boolean) x Then
                       ~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36628ERR_CannotInferNullableForVariable1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CannotInferNullableForVariable1">
        <file name="a.vb">
        Option Infer on  
        Imports System  
        Module M
            Sub Foo()
                Dim except? = New Exception
                Dim obj? = New Object
                Dim stringVar? = "Open the application."
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36628: A nullable type cannot be inferred for variable 'except'.
                Dim except? = New Exception
                    ~~~~~~
BC36628: A nullable type cannot be inferred for variable 'obj'.
                Dim obj? = New Object
                    ~~~
BC36628: A nullable type cannot be inferred for variable 'stringVar'.
                Dim stringVar? = "Open the application."
                    ~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36633ERR_IterationVariableShadowLocal2()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="IterationVariableShadowLocal2">
        <file name="a.vb">
Option Explicit Off
Imports System
Imports System.Linq
Imports System.Collections.Generic
Module M1
    &lt;System.Runtime.CompilerServices.Extension()&gt; _
    Sub Show(Of T)(ByVal collection As IEnumerable(Of T))
        For Each element In collection
            Console.WriteLine(element)
        Next
    End Sub

    Sub Scen1()
        implicit = 1
        'COMPILEERROR : BC36633, "implicit" 
        Dim x = From implicit In New Integer() {1, 2, 3} Let implicit = jj Select implicit
        'COMPILEERROR : BC36633, "implicit" 
        Dim y = From i In New Integer() {1, 2, 3} Let implicit = i Select implicit
        'COMPILEERROR : BC36633, "implicit" 
        Dim z = From i In New Integer() {1, 2, 3} Let j = implicit Select implicit
        'COMPILEERROR : BC36633, "implicit" 
        Dim u = From i In New Integer() {1, 2, 3} Let j = implicit Group i By i Into implicit = Group
        'COMPILEERROR : BC36633, "implicit" 
        Dim v = From i In New Integer() {1, 2, 3} Let j = implicit Group implicit By i Into implicit()
        'COMPILEERROR : BC36633, "implicit" 
        Dim w = From i In New Integer() {1, 2, 3} Let j = implicit Group i By implicit Into implicit()
        'COMPILEERROR : BC36633, "implicit", BC36633, "implicit"
        Dim a = From i In New Integer() {1, 2, 3} Let j = implicit Group implicit By implicit Into implicit()
        'COMPILEERROR : BC36633, "implicit"
        Dim b = From i In New Integer() {1, 2, 3}, j In New Integer() {1, 2, 3} Let k = implicit Group Join implicit In New Integer() {1, 2, 3} On i Equals implicit Into implicit()
        'COMPILEERROR : BC36633, "implicit"
        Dim c = From i In New Integer() {1, 2, 3}, j In New Integer() {1, 2, 3} Let k = implicit Group Join l In New Integer() {1, 2, 3} On i Equals l Into implicit = Group
    End Sub
    Sub Scen2()
        'COMPILEERROR : BC36633, "implicit", BC36633, "implicit" 
        Dim x = From implicit In New Integer() {1, 2, 3} Let implicit = jj Select implicit
        'COMPILEERROR : BC36633, "implicit", BC36633, "implicit" 
        Dim y = From i In New Integer() {1, 2, 3} Let implicit = i Select implicit
        'COMPILEERROR : BC36633, "implicit"
        Dim z = From i In New Integer() {1, 2, 3} Let j = implicit Select implicit
        'COMPILEERROR : BC36633, "implicit" 
        Dim u = From i In New Integer() {1, 2, 3} Let j = implicit Group i By i Into implicit = Group
        'COMPILEERROR : BC36633, "implicit" 
        Dim v = From i In New Integer() {1, 2, 3} Let j = implicit Group implicit By i Into implicit()
        'COMPILEERROR : BC36633, "implicit" 
        Dim w = From i In New Integer() {1, 2, 3} Let j = implicit Group i By implicit Into implicit()
        'COMPILEERROR : BC36633, "implicit", BC36633, "implicit"
        Dim a = From i In New Integer() {1, 2, 3} Let j = implicit Group implicit By implicit Into implicit()
        'COMPILEERROR : BC36633, "implicit"
        Dim b = From i In New Integer() {1, 2, 3}, j In New Integer() {1, 2, 3} Let k = implicit Group Join implicit In New Integer() {1, 2, 3} On i Equals implicit Into implicit()
        'COMPILEERROR : BC36633, "implicit"
        Dim c = From i In New Integer() {1, 2, 3}, j In New Integer() {1, 2, 3} Let k = implicit Group Join l In New Integer() {1, 2, 3} On i Equals l Into implicit = Group
        implicit = 1
    End Sub
End Module
    </file>
    </compilation>, {Net40.References.SystemCore}).VerifyDiagnostics(Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_QueryOperatorNotFound, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_QueryOperatorNotFound, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_QueryDuplicateAnonTypeMemberName1, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_QueryOperatorNotFound, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_QueryDuplicateAnonTypeMemberName1, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_QueryOperatorNotFound, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.WRN_DefAsgUseNullRef, "jj").WithArguments("jj"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_QueryOperatorNotFound, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_QueryOperatorNotFound, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_QueryDuplicateAnonTypeMemberName1, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_QueryOperatorNotFound, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_QueryDuplicateAnonTypeMemberName1, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_QueryOperatorNotFound, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.ERR_IterationVariableShadowLocal2, "implicit").WithArguments("implicit"),
    Diagnostic(ERRID.WRN_DefAsgUseNullRef, "jj").WithArguments("jj"),
    Diagnostic(ERRID.WRN_DefAsgUseNullRef, "implicit").WithArguments("implicit"))

        End Sub

        <Fact()>
        Public Sub BC36635ERR_LambdaInSelectCaseExpr()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="LambdaInSelectCaseExpr">
        <file name="a.vb">
        Public Module M
            Sub LambdaAttribute()
                Select Case (Function(arg) arg Is Nothing)
                End Select
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36635: Lambda expressions are not valid in the first expression of a 'Select Case' statement.
                Select Case (Function(arg) arg Is Nothing)
                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36638ERR_CannotLiftStructureMeLambda()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="CannotLiftStructureMeLambda">
        <file name="a.vb">
        Imports System
        Imports System.Collections.Generic
        Imports System.Linq
        Imports System.Linq.Expressions
        Structure S
            Sub New(ByVal x As S)
                Dim a As Expression(Of Action) = Sub() ToString()
                Dim b As Expression(Of Action) = Sub() Console.WriteLine(Me)
            End Sub
        End Structure
    </file>
    </compilation>, references:={Net40.References.SystemCore})
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                Dim a As Expression(Of Action) = Sub() ToString()
                                                       ~~~~~~~~
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                Dim b As Expression(Of Action) = Sub() Console.WriteLine(Me)
                                                                         ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36638ERR_CannotLiftStructureMeLambda_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CannotLiftStructureMeLambda_2">
        <file name="a.vb">
        Imports System
        Structure S
            Sub New(ByVal x As S)
                Dim a As Action = Sub() ToString()
                Dim b As Action = Sub() Console.WriteLine(Me)
            End Sub
        End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                Dim a As Action = Sub() ToString()
                                        ~~~~~~~~
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                Dim b As Action = Sub() Console.WriteLine(Me)
                                                          ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36638ERR_CannotLiftStructureMeLambda_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CannotLiftStructureMeLambda_3">
        <file name="a.vb">
        Imports System
        Structure S
            Sub New(ByVal x As S)
                Dim a As Action = Sub() MyClass.ToString()
                Dim b As Action = Sub() Console.WriteLine(MyClass.ToString())
            End Sub
        End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                Dim a As Action = Sub() MyClass.ToString()
                                        ~~~~~~~
BC36638: Instance members and 'Me' cannot be used within a lambda expression in structures.
                Dim b As Action = Sub() Console.WriteLine(MyClass.ToString())
                                                          ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36639ERR_CannotLiftByRefParamLambda1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CannotLiftByRefParamLambda1">
        <file name="a.vb">
        Imports System
        Public Module M 
            Sub Foo(ByRef x As String)
                Dim a As Action = Sub() Console.WriteLine(x)
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36639: 'ByRef' parameter 'x' cannot be used in a lambda expression.
                Dim a As Action = Sub() Console.WriteLine(x)
                                                          ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36640ERR_CannotLiftRestrictedTypeLambda()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CannotLiftRestrictedTypeLambda">
        <file name="a.vb">
        Imports System
        Public Module M
            Sub LiftRestrictedType()
                Dim x As ArgIterator = Nothing
                Dim f As Action = Sub() x.GetNextArgType().GetModuleHandle()
            End Sub
        End Module
    </file>
    </compilation>)
            compilation.VerifyEmitDiagnostics(
                Diagnostic(ERRID.ERR_CannotLiftRestrictedTypeLambda, "x").WithArguments("System.ArgIterator"))
        End Sub

        <Fact()>
        Public Sub BC36641ERR_LambdaParamShadowLocal1()
            ' Roslyn - extra warning
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="LambdaParamShadowLocal1">
        <file name="a.vb">
        Module M1
            Class Class1
                Function FOO()
                    Dim x = Function() As Object
                    Dim z = Function() Sub(FOO) FOO = FOO
                            End Function ' 1
                End Function ' 2
            End Class
        End Module
    </file>
    </compilation>).AssertTheseDiagnostics(<expected><![CDATA[
BC36641: Lambda parameter 'FOO' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.
                    Dim z = Function() Sub(FOO) FOO = FOO
                                           ~~~
BC42105: Function '<anonymous method>' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
                            End Function ' 1
                            ~~~~~~~~~~~~
BC42105: Function 'FOO' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
                End Function ' 2
                ~~~~~~~~~~~~
                                           ]]></expected>)

        End Sub

        <Fact()>
        Public Sub BC36641ERR_LambdaParamShadowLocal1_2()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="LambdaParamShadowLocal1">
        <file name="a.vb">
Option Infer Off
Module Program
    Sub Main(args As String())
        Dim X = 1
        Dim Y = 1
        Dim S = If(True, _
        Function(x As Integer) As Integer
            Return 0
        End Function, Y = Y + 1)
    End Sub
End Module
    </file>
    </compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_LambdaParamShadowLocal1, "x").WithArguments("x")
    )
        End Sub

        <Fact()>
        Public Sub BC36642ERR_StrictDisallowImplicitObjectLambda()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="StrictDisallowImplicitObjectLambda">
        <file name="a.vb">
        Option Strict On
        Module M
            Sub Main()
                Dim x = Function(y) 1
            End Sub
        End Module
    </file>
    </compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_StrictDisallowImplicitObjectLambda, "y")
    )
        End Sub

        <Fact>
        Public Sub BC36645ERR_TypeInferenceFailureAddressOfLateBound()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="TypeInferenceFailure2">
        <file name="a.vb">
Option Strict Off
Imports System

Class Module1
    Sub Foo(Of T As Structure, S As Structure)(ByVal x As T, ByVal f As Func(Of T?, S?))
    End Sub

    Class Class2
        Function Bar(ByVal x As Integer) As Integer?
            Return Nothing
        End Function
    End Class

    Shared Sub Main()
        Dim o As Object = New Class2
        Foo(1, AddressOf o.Bar)

        Dim qo As IQueryable(Of Integer)
        Dim r = qo.Select(AddressOf o.Fee)
    End Sub
End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36645: Data type(s) of the type parameter(s) in method 'Public Sub Foo(Of T As Structure, S As Structure)(x As T, f As Func(Of T?, S?))' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
        Foo(1, AddressOf o.Bar)
        ~~~
BC30002: Type 'IQueryable' is not defined.
        Dim qo As IQueryable(Of Integer)
                  ~~~~~~~~~~~~~~~~~~~~~~
BC42104: Variable 'qo' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim r = qo.Select(AddressOf o.Fee)
                ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36657ERR_TypeInferenceFailureNoBest2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="TypeInferenceFailureNoBest2">
        <file name="a.vb">
        Option Strict On
        Imports System
        Public Module M
            Sub Main()
                Dim x As Action(Of String())
                Dim y As Action(Of Object())
                Foo(x, y)
            End Sub
            Sub Foo(Of T)(ByVal x As Action(Of T()), ByVal y As Action(Of T()))
            End Sub
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36657: Data type(s) of the type parameter(s) in method 'Public Sub Foo(Of T)(x As Action(Of T()), y As Action(Of T()))' cannot be inferred from these arguments because they do not convert to the same type. Specifying the data type(s) explicitly might correct this error.
                Foo(x, y)
                ~~~
BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.
                Foo(x, y)
                    ~
BC42104: Variable 'y' is used before it has been assigned a value. A null reference exception could result at runtime.
                Foo(x, y)
                       ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36663ERR_DelegateBindingMismatchStrictOff2()
            CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="DelegateBindingMismatchStrictOff2">
        <file name="a.vb">
        Option Strict On
        Public Module M
            Sub Main()
                Dim k = {Function() "", AddressOf System.Console.Read}
            End Sub
        End Module
    </file>
    </compilation>).VerifyDiagnostics(
    Diagnostic(ERRID.ERR_DelegateBindingMismatchStrictOff2, "System.Console.Read").WithArguments("Public Shared Overloads Function Read() As Integer", "AnonymousType Function <generated method>() As String"))

        End Sub

        <WorkItem(528732, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528732")>
        <Fact()>
        Public Sub BC36666ERR_InaccessibleReturnTypeOfMember2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="InaccessibleReturnTypeOfMember2">
        <file name="a.vb">
        Option Infer On
        Imports System
        Class TestClass
            Dim pt As PrivateType
            Public name As PrivateType
            Property prop As PrivateType
            Protected Class PrivateType
            End Class
            Function foo() As PrivateType
                Return pt
            End Function
        End Class
        Module Module1
            Sub Main()
                Dim tc As TestClass = New TestClass()
                Console.WriteLine(tc.name)
                Console.WriteLine(tc.foo)
                Console.WriteLine(tc.prop)
            End Sub
        End Module
    </file>
    </compilation>).VerifyDiagnostics(
        Diagnostic(ERRID.ERR_AccessMismatch6, "PrivateType").WithArguments("name", "TestClass.PrivateType", "namespace", "<Default>", "class", "TestClass"),
        Diagnostic(ERRID.ERR_AccessMismatch6, "PrivateType").WithArguments("prop", "TestClass.PrivateType", "namespace", "<Default>", "class", "TestClass"),
        Diagnostic(ERRID.ERR_AccessMismatch6, "PrivateType").WithArguments("foo", "TestClass.PrivateType", "namespace", "<Default>", "class", "TestClass"),
        Diagnostic(ERRID.ERR_InaccessibleReturnTypeOfMember2, "tc.name").WithArguments("Public TestClass.name As TestClass.PrivateType"),
        Diagnostic(ERRID.ERR_InaccessibleReturnTypeOfMember2, "tc.foo").WithArguments("Public Function TestClass.foo() As TestClass.PrivateType"),
        Diagnostic(ERRID.ERR_InaccessibleReturnTypeOfMember2, "tc.prop").WithArguments("Public Property TestClass.prop As TestClass.PrivateType"))
        End Sub

        <Fact()>
        Public Sub BC36667ERR_LocalNamedSameAsParamInLambda1()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="LocalNamedSameAsParamInLambda1">
        <file name="a.vb">
        Module M1
            Sub Fun()
                Dim x = Sub(y)
                            Dim y = 5
                        End Sub
            End Sub
        End Module
    </file>
    </compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_LocalNamedSameAsParamInLambda1, "y").WithArguments("y")
    )
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29568")>
        Public Sub BC36670ERR_LambdaBindingMismatch2()
            CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="LambdaBindingMismatch2">
        <file name="a.vb">
        Imports System
        Module M1
            Sub Action()
            End Sub
            Sub [Sub](ByRef x As Action)
                x = Sub() Action()
                x()
            End Sub
            Sub Fun()
                [Sub]((Sub(x As Integer(,))
                           Action()
                       End Sub))
            End Sub
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore}).VerifyDiagnostics(Diagnostic(ERRID.ERR_LambdaBindingMismatch2,
            <![CDATA[Sub(x As Integer(,))
                           Action()
                       End Sub]]>.Value.Replace(vbLf, Environment.NewLine)).WithArguments("System.Action")
            )
        End Sub

        ' Different error
        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub BC36675ERR_StatementLambdaInExpressionTree()

            CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="StatementLambdaInExpressionTree">
        <file name="a.vb">
    Imports System
        Module M1
            Sub Fun()
                Dim x = 1
                            Dim y As System.Linq.Expressions.Expression(Of func(Of Integer)) = Function()
                                                                                               Return x
                                                                                               End Function
            End Sub
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore}).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_StatementLambdaInExpressionTree, <![CDATA[Function()
                                                                                               Return x
                                                                                               End Function]]>.Value.Replace(vbLf, Environment.NewLine)))

        End Sub

        <Fact()>
        Public Sub BC36709ERR_DelegateBindingMismatchStrictOff3()

            CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="DelegateBindingMismatchStrictOff3">
        <file name="a.vb">
        Option Strict On
        Module M
            Delegate Function del1g(Of g)(ByVal x As g) As String
            Sub foo()
                Dim o As New cls1
                Dim x As New del1g(Of String)(AddressOf o.moo(Of Integer))
            End Sub
        End Module
        Class cls1
            Public Function foo(Of G)(ByVal y As G) As String
                Return "Instance"
            End Function
        End Class
        Module m1
            &lt;System.Runtime.CompilerServices.Extension()&gt; _
            Public Function moo(Of G)(ByVal this As cls1, ByVal y As G) As String
                Return "Extension"
            End Function
        End Module
    </file>
    </compilation>, {Net40.References.SystemCore}).VerifyDiagnostics(
    Diagnostic(ERRID.ERR_DelegateBindingMismatchStrictOff3, "o.moo(Of Integer)").WithArguments("Public Function moo(Of Integer)(y As Integer) As String", "Delegate Function M.del1g(Of String)(x As String) As String", "m1"))

        End Sub

        <Fact()>
        Public Sub BC36710ERR_DelegateBindingIncompatible3()
            Dim compilation = CreateCompilation(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Runtime.CompilerServices
Imports System.Xml.Linq
Delegate Function D() As Object
Module M
    Private F1 As New D(AddressOf <x/>.E1)
    Private F2 As New D(AddressOf <x/>.E2)
    <Extension()>
    Function E1(x As XElement) As Object
        Return Nothing
    End Function
    <Extension()>
    Function E2(x As XElement, y As Object) As Object
        Return Nothing
    End Function
End Module
    ]]></file>
</compilation>, targetFramework:=TargetFramework.Mscorlib461AndVBRuntime, references:=Net461XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC36710: Extension Method 'Public Function E2(y As Object) As Object' defined in 'M' does not have a signature compatible with delegate 'Delegate Function D() As Object'.
    Private F2 As New D(AddressOf <x/>.E2)
                                  ~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC36718ERR_NotACollection1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NotACollection1">
        <file name="a.vb">
        Module M1
            Dim a As New Object() From {}
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36718: Cannot initialize the type 'Object' with a collection initializer because it is not a collection type.
            Dim a As New Object() From {}
                                  ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36719ERR_NoAddMethod1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoAddMethod1">
        <file name="a.vb">
        Imports System.Collections.Generic
        Module M1
            Dim x As UDCollection1(Of Integer) = New UDCollection1(Of Integer) From {1, 2, 3}
        End Module
        Class UDCollection1(Of t)
            Implements IEnumerable(Of t)
            Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of t) Implements System.Collections.Generic.IEnumerable(Of t).GetEnumerator
                Return Nothing
            End Function
            Public Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
                Return Nothing
            End Function
        End Class
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36719: Cannot initialize the type 'UDCollection1(Of Integer)' with a collection initializer because it does not have an accessible 'Add' method.
            Dim x As UDCollection1(Of Integer) = New UDCollection1(Of Integer) From {1, 2, 3}
                                                                               ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36721ERR_EmptyAggregateInitializer()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoAddMethod1">
        <file name="a.vb">
Option Strict On
Imports System.Collections.Generic
Public Module X
    Sub Foo(ByVal x As Boolean)
        Dim y As New List(Of Integer) From {1, 2, {}}
    End Sub
End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
        BC36721: An aggregate collection initializer entry must contain at least one element.
        Dim y As New List(Of Integer) From {1, 2, {}}
                                                  ~~
    </expected>)
        End Sub

        <Fact()>
        Public Sub BC36734ERR_LambdaTooManyTypesObjectDisallowed()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="LambdaTooManyTypesObjectDisallowed">
        <file name="a.vb">
        Option Strict On
        Imports System
        Module M1
            Dim y As Action(Of Integer) = Function(a As Integer)
                                              Return 1
                                                  Return ""
                                          End Function
        End Module
    </file>
    </compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_LambdaTooManyTypesObjectDisallowed, "Function(a As Integer)")
            )
        End Sub

        <Fact()>
        Public Sub BC36751ERR_LambdaNoType()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="LambdaNoType">
        <file name="a.vb">
        Module M1
            Dim x = Function()
                         Return AddressOf System.Console.WriteLine
                     End Function
        End Module
    </file>
    </compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_LambdaNoType, "Function()"))

        End Sub

        ' Different error
        <Fact()>
        Public Sub BC36754ERR_VarianceConversionFailedOut6()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="VarianceConversionFailedOut6">
        <file name="a.vb">
        Option Strict On
        Interface IVariance(Of Out T) : Function Foo() As T : End Interface
        Interface IVariance2(Of In T) : Sub Foo(ByVal s As T) : End Interface

        Class Variance(Of T) : Implements IVariance(Of T), IVariance2(Of T)
            Public Function Foo() As T Implements IVariance(Of T).Foo
            End Function
            Public Sub Foo1(ByVal s As T) Implements IVariance2(Of T).Foo
            End Sub
        End Class
        Module M1
            Dim x As IVariance(Of Double) = New Variance(Of Short)
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36754: 'Variance(Of Short)' cannot be converted to 'IVariance(Of Double)' because 'Short' is not derived from 'Double', as required for the 'Out' generic parameter 'T' in 'Interface IVariance(Of Out T)'.
            Dim x As IVariance(Of Double) = New Variance(Of Short)
                                            ~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        ' Different error
        <Fact()>
        Public Sub BC36755ERR_VarianceConversionFailedIn6()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="VarianceConversionFailedIn6">
        <file name="a.vb">
        Option Strict On
        Interface IVariance(Of Out T) : Function Foo() As T : End Interface
        Interface IVariance2(Of In T) : Sub Foo(ByVal s As T) : End Interface

        Class Variance(Of T) : Implements IVariance(Of T), IVariance2(Of T)
            Public Function Foo() As T Implements IVariance(Of T).Foo
            End Function
            Public Sub Foo1(ByVal s As T) Implements IVariance2(Of T).Foo
            End Sub
        End Class
        Module M1
            Dim x As IVariance2(Of Short) = New Variance(Of Double)
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36755: 'Variance(Of Double)' cannot be converted to 'IVariance2(Of Short)' because 'Short' is not derived from 'Double', as required for the 'In' generic parameter 'T' in 'Interface IVariance2(Of In T)'.
            Dim x As IVariance2(Of Short) = New Variance(Of Double)
                                            ~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        ' Different error
        <Fact()>
        Public Sub BC36756ERR_VarianceIEnumerableSuggestion3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="VarianceIEnumerableSuggestion3">
        <file name="a.vb">
        Imports System.Collections.Generic
        Public Class Animals : End Class
        Public Class Cheetah : Inherits Animals : End Class
        Module M1
            Dim x As List(Of Animals) = New List(Of Cheetah)
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36756: 'List(Of Cheetah)' cannot be converted to 'List(Of Animals)'. Consider using 'IEnumerable(Of Animals)' instead.
            Dim x As List(Of Animals) = New List(Of Cheetah)
                                        ~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36757ERR_VarianceConversionFailedTryOut4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="VarianceConversionFailedTryOut4">
        <file name="a.vb">
        Option Strict On
        Public Class Animals : End Class
        Public Class Cheetah : Inherits Animals : End Class
        Interface IFoo(Of T) : End Interface
                Class MyFoo(Of T)
                    Implements IFoo(Of T)
                End Class
        Module M1
            Dim x As IFoo(Of Animals) = New MyFoo(Of Cheetah)
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36757: 'MyFoo(Of Cheetah)' cannot be converted to 'IFoo(Of Animals)'. Consider changing the 'T' in the definition of 'Interface IFoo(Of T)' to an Out type parameter, 'Out T'.
            Dim x As IFoo(Of Animals) = New MyFoo(Of Cheetah)
                                        ~~~~~~~~~~~~~~~~~~~~~

</expected>)
        End Sub

        ' Different error
        <Fact()>
        Public Sub BC36758ERR_VarianceConversionFailedTryIn4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="VarianceConversionFailedTryIn4">
        <file name="a.vb">
        Option Strict On
        Public Class Animals : End Class
        Public Class Vertebrates : Inherits Animals : End Class
        Public Class Mammals : Inherits Vertebrates : End Class
        Public Class Carnivora : Inherits Mammals : End Class
        Class Variance(Of T)
            Interface IVariance(Of In S, Out R)
            End Interface
        End Class
        Class Variance2(Of T, R As New)
            Implements Variance(Of T).IVariance(Of T, R)
        End Class
        Module M1
            Dim x As Variance(Of Mammals).IVariance(Of Mammals, Mammals) = New Variance2(Of Animals, Carnivora)
        End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36758: 'Variance2(Of Animals, Carnivora)' cannot be converted to 'Variance(Of Mammals).IVariance(Of Mammals, Mammals)'. Consider changing the 'T' in the definition of 'Class Variance(Of T)' to an In type parameter, 'In T'.
            Dim x As Variance(Of Mammals).IVariance(Of Mammals, Mammals) = New Variance2(Of Animals, Carnivora)
                                                                           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36760ERR_IdentityDirectCastForFloat()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="LambdaParamShadowLocal1">
        <file name="a.vb">
                Module Test
                    Sub Main()
                        Dim a As Double = 1
                        Dim b As Integer = DirectCast(a, Double) * 1000
                    End Sub
                End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36760: Using DirectCast operator to cast a floating-point value to the same type is not supported.
                        Dim b As Integer = DirectCast(a, Double) * 1000
                                                      ~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36807ERR_TypeDisallowsElements()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Imports System.Xml.Linq
Class MyElement
    Inherits XElement
    Public Sub New(name As XName)
        MyBase.New(name)
    End Sub
End Class
Module M
    Function F(Of T)() As T
        Return Nothing
    End Function
    Sub M()
        Dim x As Object
        x = F(Of Object).<x>
        x = F(Of XObject).<x>
        x = F(Of XContainer).<x>
        x = F(Of XElement).<x>
        x = F(Of XDocument).<x>
        x = F(Of XAttribute).<x>
        x = F(Of MyElement).<x>
        x = F(Of Object()).<x>
        x = F(Of XObject()).<x>
        x = F(Of XContainer()).<x>
        x = F(Of XElement()).<x>
        x = F(Of XDocument()).<x>
        x = F(Of XAttribute()).<x>
        x = F(Of MyElement()).<x>
        x = F(Of IEnumerable(Of Object)).<x>
        x = F(Of IEnumerable(Of XObject)).<x>
        x = F(Of IEnumerable(Of XContainer)).<x>
        x = F(Of IEnumerable(Of XElement)).<x>
        x = F(Of IEnumerable(Of XDocument)).<x>
        x = F(Of IEnumerable(Of XAttribute)).<x>
        x = F(Of IEnumerable(Of MyElement)).<x>
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31168: XML axis properties do not support late binding.
        x = F(Of Object).<x>
            ~~~~~~~~~~~~~~~~
BC36807: XML elements cannot be selected from type 'XObject'.
        x = F(Of XObject).<x>
            ~~~~~~~~~~~~~~~~~
BC36807: XML elements cannot be selected from type 'XAttribute'.
        x = F(Of XAttribute).<x>
            ~~~~~~~~~~~~~~~~~~~~
BC36807: XML elements cannot be selected from type 'Object()'.
        x = F(Of Object()).<x>
            ~~~~~~~~~~~~~~~~~~
BC36807: XML elements cannot be selected from type 'XObject()'.
        x = F(Of XObject()).<x>
            ~~~~~~~~~~~~~~~~~~~
BC36807: XML elements cannot be selected from type 'XAttribute()'.
        x = F(Of XAttribute()).<x>
            ~~~~~~~~~~~~~~~~~~~~~~
BC36807: XML elements cannot be selected from type 'IEnumerable(Of Object)'.
        x = F(Of IEnumerable(Of Object)).<x>
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36807: XML elements cannot be selected from type 'IEnumerable(Of XObject)'.
        x = F(Of IEnumerable(Of XObject)).<x>
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36807: XML elements cannot be selected from type 'IEnumerable(Of XAttribute)'.
        x = F(Of IEnumerable(Of XAttribute)).<x>
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC36808ERR_TypeDisallowsAttributes()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Imports System.Xml.Linq
Class MyElement
    Inherits XElement
    Public Sub New(name As XName)
        MyBase.New(name)
    End Sub
End Class
Module M
    Function F(Of T)() As T
        Return Nothing
    End Function
    Sub M()
        Dim x As Object
        x = F(Of Object).@a
        x = F(Of XObject).@a
        x = F(Of XContainer).@a
        x = F(Of XElement).@a
        x = F(Of XDocument).@a
        x = F(Of XAttribute).@a
        x = F(Of MyElement).@a
        x = F(Of Object()).@a
        x = F(Of XObject()).@a
        x = F(Of XContainer()).@a
        x = F(Of XElement()).@a
        x = F(Of XDocument()).@a
        x = F(Of XAttribute()).@a
        x = F(Of MyElement()).@a
        x = F(Of IEnumerable(Of Object)).@a
        x = F(Of IEnumerable(Of XObject)).@a
        x = F(Of IEnumerable(Of XContainer)).@a
        x = F(Of IEnumerable(Of XElement)).@a
        x = F(Of IEnumerable(Of XDocument)).@a
        x = F(Of IEnumerable(Of XAttribute)).@a
        x = F(Of IEnumerable(Of MyElement)).@a
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31168: XML axis properties do not support late binding.
        x = F(Of Object).@a
            ~~~~~~~~~~~~~~~
BC36808: XML attributes cannot be selected from type 'XObject'.
        x = F(Of XObject).@a
            ~~~~~~~~~~~~~~~~
BC36808: XML attributes cannot be selected from type 'XContainer'.
        x = F(Of XContainer).@a
            ~~~~~~~~~~~~~~~~~~~
BC36808: XML attributes cannot be selected from type 'XDocument'.
        x = F(Of XDocument).@a
            ~~~~~~~~~~~~~~~~~~
BC36808: XML attributes cannot be selected from type 'XAttribute'.
        x = F(Of XAttribute).@a
            ~~~~~~~~~~~~~~~~~~~
BC36808: XML attributes cannot be selected from type 'Object()'.
        x = F(Of Object()).@a
            ~~~~~~~~~~~~~~~~~
BC36808: XML attributes cannot be selected from type 'XObject()'.
        x = F(Of XObject()).@a
            ~~~~~~~~~~~~~~~~~~
BC36808: XML attributes cannot be selected from type 'XContainer()'.
        x = F(Of XContainer()).@a
            ~~~~~~~~~~~~~~~~~~~~~
BC36808: XML attributes cannot be selected from type 'XDocument()'.
        x = F(Of XDocument()).@a
            ~~~~~~~~~~~~~~~~~~~~
BC36808: XML attributes cannot be selected from type 'XAttribute()'.
        x = F(Of XAttribute()).@a
            ~~~~~~~~~~~~~~~~~~~~~
BC36808: XML attributes cannot be selected from type 'IEnumerable(Of Object)'.
        x = F(Of IEnumerable(Of Object)).@a
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36808: XML attributes cannot be selected from type 'IEnumerable(Of XObject)'.
        x = F(Of IEnumerable(Of XObject)).@a
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36808: XML attributes cannot be selected from type 'IEnumerable(Of XContainer)'.
        x = F(Of IEnumerable(Of XContainer)).@a
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36808: XML attributes cannot be selected from type 'IEnumerable(Of XDocument)'.
        x = F(Of IEnumerable(Of XDocument)).@a
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36808: XML attributes cannot be selected from type 'IEnumerable(Of XAttribute)'.
        x = F(Of IEnumerable(Of XAttribute)).@a
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)
            ' Note: The last error above ("BC30518: Overload resolution failed ...")
            ' should not be generated after variance conversions are supported.
        End Sub

        <Fact()>
        Public Sub BC36809ERR_TypeDisallowsDescendants()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Imports System.Xml.Linq
Class MyElement
    Inherits XElement
    Public Sub New(name As XName)
        MyBase.New(name)
    End Sub
End Class
Module M
    Function F(Of T)() As T
        Return Nothing
    End Function
    Sub M()
        Dim x As Object
        x = F(Of Object)...<x>
        x = F(Of XObject)...<x>
        x = F(Of XContainer)...<x>
        x = F(Of XElement)...<x>
        x = F(Of XDocument)...<x>
        x = F(Of XAttribute)...<x>
        x = F(Of MyElement)...<x>
        x = F(Of Object())...<x>
        x = F(Of XObject())...<x>
        x = F(Of XContainer())...<x>
        x = F(Of XElement())...<x>
        x = F(Of XDocument())...<x>
        x = F(Of XAttribute())...<x>
        x = F(Of MyElement())...<x>
        x = F(Of IEnumerable(Of Object))...<x>
        x = F(Of IEnumerable(Of XObject))...<x>
        x = F(Of IEnumerable(Of XContainer))...<x>
        x = F(Of IEnumerable(Of XElement))...<x>
        x = F(Of IEnumerable(Of XDocument))...<x>
        x = F(Of IEnumerable(Of XAttribute))...<x>
        x = F(Of IEnumerable(Of MyElement))...<x>
    End Sub
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC31168: XML axis properties do not support late binding.
        x = F(Of Object)...<x>
            ~~~~~~~~~~~~~~~~~~
BC36809: XML descendant elements cannot be selected from type 'XObject'.
        x = F(Of XObject)...<x>
            ~~~~~~~~~~~~~~~~~~~
BC36809: XML descendant elements cannot be selected from type 'XAttribute'.
        x = F(Of XAttribute)...<x>
            ~~~~~~~~~~~~~~~~~~~~~~
BC36809: XML descendant elements cannot be selected from type 'Object()'.
        x = F(Of Object())...<x>
            ~~~~~~~~~~~~~~~~~~~~
BC36809: XML descendant elements cannot be selected from type 'XObject()'.
        x = F(Of XObject())...<x>
            ~~~~~~~~~~~~~~~~~~~~~
BC36809: XML descendant elements cannot be selected from type 'XAttribute()'.
        x = F(Of XAttribute())...<x>
            ~~~~~~~~~~~~~~~~~~~~~~~~
BC36809: XML descendant elements cannot be selected from type 'IEnumerable(Of Object)'.
        x = F(Of IEnumerable(Of Object))...<x>
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36809: XML descendant elements cannot be selected from type 'IEnumerable(Of XObject)'.
        x = F(Of IEnumerable(Of XObject))...<x>
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36809: XML descendant elements cannot be selected from type 'IEnumerable(Of XAttribute)'.
        x = F(Of IEnumerable(Of XAttribute))...<x>
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC36907ERR_TypeOrMemberNotGeneric2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="TypeOrMemberNotGeneric2">
        <file name="a.vb">
Option Strict On
Imports System
Module M1
    Sub FOO()
        Dim x1 As Integer
        x1.FooGeneric01(Of Integer)()
    End Sub
End Module
Module Extension01
    &lt;System.Runtime.CompilerServices.Extension()&gt; Sub FooGeneric01(Of T1)(ByVal o As T1)
    End Sub
End Module
    </file>
    </compilation>, {Net40.References.SystemCore})
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36907: Extension method 'Public Sub FooGeneric01()' defined in 'Extension01' is not generic (or has no free type parameters) and so cannot have type arguments.
        x1.FooGeneric01(Of Integer)()
                       ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC36913ERR_IfTooManyTypesObjectDisallowed()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Option Strict On
Public Module X
    Sub Foo(ByVal x As Boolean)
        Dim f = If(x, Function() 1, Function() "")
    End Sub
End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36913: Cannot infer a common type because more than one type is possible.
        Dim f = If(x, Function() 1, Function() "")
                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~        
    </expected>)
        End Sub

        <Fact()>
        Public Sub BC36916ERR_LambdaNoTypeObjectDisallowed()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="LambdaNoTypeObjectDisallowed">
        <file name="a.vb">
        Option Strict On
        Imports System
        Module M1
            Dim x As Action = Function()
                                  Dim a = 2
                              End Function
        End Module
    </file>
    </compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.ERR_LambdaNoTypeObjectDisallowed, "Function()"),
            Diagnostic(ERRID.WRN_DefAsgNoRetValFuncRef1, "End Function").WithArguments("<anonymous method>")
    )
        End Sub

#End Region

#Region "Targeted Warning Tests"

        <Fact()>
        Public Sub BC40052WRN_SelectCaseInvalidRange()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="SelectCaseInvalidRange">
        <file name="a.vb">
            Module M1
                Sub foo()
                    Select Case #1/2/0003#
                        Case Date.MaxValue To Date.MinValue
                    End Select
                    Select Case 1
                        Case 1 To 0
                    End Select
                End Sub
            End Module
        </file>
    </compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))
            Dim expectedErrors1 = <errors>
BC40052: Range specified for 'Case' statement is not valid. Make sure that the lower bound is less than or equal to the upper bound.
                        Case 1 To 0
                             ~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC40052WRN_SelectCaseInvalidRange_02()
            ' Dev10 doesn't report WRN_SelectCaseInvalidRange for each invalid case range.
            ' It is performed only if bounds for all clauses in the Select are integer constants and
            ' all clauses are either range clauses or equality clause.
            ' Doing this differently will produce warnings in more scenarios - breaking change,
            ' hence we maintain compatibility with Dev10.

            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="SelectCaseInvalidRange">
        <file name="a.vb">
Module M1
    Sub foo()
        ' NO WARNING CASES

        ' with relational clause
        Select Case 1
            Case 1 To 0
            Case Is > 10
        End Select

        ' with relational clause in different order
        Select Case 1
            Case Is > 10
            Case 1 To 0
        End Select

        ' with non constant integer clause
        Dim number As Integer = 10
        Select Case 1
            Case 1 To 0
            Case number
        End Select

        ' with non integer clause
        Dim obj As Object = 10
        Select Case 1
            Case 1 To 0
            Case obj
        End Select


        ' WARNING CASES

        ' with all integer constant equality clauses
        Select Case 1
            Case 1 To 0
            Case Is = 2
            Case 1, 2, 3
        End Select

        ' with all integer constant equality clauses in different order
        Select Case 1
            Case Is = 2
            Case 1, 2, 3
            Case 1 To 0
        End Select
    End Sub
End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC40052: Range specified for 'Case' statement is not valid. Make sure that the lower bound is less than or equal to the upper bound.
            Case 1 To 0
                 ~~~~~~
BC40052: Range specified for 'Case' statement is not valid. Make sure that the lower bound is less than or equal to the upper bound.
            Case 1 To 0
                 ~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        <WorkItem(759127, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/759127")>
        Public Sub BC41000WRN_AttributeSpecifiedMultipleTimes()
            ' No warnings Expected - Previously would generate a BC41000.  The signature of Attribute 
            ' determined From attribute used in original bug.
            ' We want to verify that the diagnostics do not appear and that the attributes are both added.

            ' No values provided for attribute
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="Class1">
        <file name="a.vb">
Imports System
&lt;System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments")&gt;
&lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class, AllowMultiple:=True)&gt;
Public Class ESAttribute
    Inherits System.Attribute

    Public Sub New(assemblyGUID As String)
        If (assemblyGUID Is Nothing) Then
            Throw New System.ArgumentNullException("assemblyGuid")
        End If
    End Sub
    Public Sub New()
    End Sub
End Class
        </file>
        <file name="b.vb">
&lt;Assembly:ESAttribute()&gt;
Class Class1
    Sub Blah()
    End Sub
End Class
        </file>
        <file name="c.vb">
&lt;Assembly:ESAttribute()&gt;
Module Module1
    Sub Main()
    End Sub
End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
                                  </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors1)

            ' One value provided for attribute, the other does not have argument
            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Class1">
    <file name="a.vb">
Imports System
&lt;System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments")&gt;
&lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class, AllowMultiple:=True)&gt;
Public Class ESAttribute
    Inherits System.Attribute

    Public Sub New(assemblyGUID As String)
        If (assemblyGUID Is Nothing) Then
            Throw New System.ArgumentNullException("assemblyGuid")
        End If
    End Sub
    Public Sub New()
    End Sub
End Class
        </file>
    <file name="b.vb">
&lt;Assembly:ESAttribute()&gt;
Class Class1
    Sub Blah()
    End Sub
End Class
        </file>
    <file name="c.vb">
&lt;Assembly:ESAttribute("Test")&gt;
Module Module1
    Sub Main()
    End Sub
End Module
        </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors1)

            ' Different values for argument provided for attribute usage
            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Class1">
    <file name="a.vb">
Imports System
&lt;System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments")&gt;
&lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class, AllowMultiple:=True)&gt;
Public Class ESAttribute
    Inherits System.Attribute

    Public Sub New(assemblyGUID As String)
        If (assemblyGUID Is Nothing) Then
            Throw New System.ArgumentNullException("assemblyGuid")
        End If
    End Sub
    Public Sub New()
    End Sub
End Class
        </file>
    <file name="b.vb">
&lt;Assembly:ESAttribute("test2")&gt;
Class Class1
    Sub Blah()
    End Sub
End Class
        </file>
    <file name="c.vb">
&lt;Assembly:ESAttribute("Test")&gt;
Module Module1
    Sub Main()
    End Sub
End Module
        </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors1)
            Assert.Equal(2, compilation.Assembly.GetAttributes.Count)
            Assert.Equal("ESAttribute(""test2"")", compilation.Assembly.GetAttributes.Item(0).ToString)
            Assert.Equal("ESAttribute(""Test"")", compilation.Assembly.GetAttributes.Item(1).ToString)

            ' Different values for argument provided for attribute usage - Different Order To check the order is 
            ' not sorted but merely the order it was located in compilation 

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="Class1">
    <file name="a.vb">
Imports System
&lt;System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments")&gt;
&lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class, AllowMultiple:=True)&gt;
Public Class ESAttribute
    Inherits System.Attribute

    Public Sub New(assemblyGUID As String)
        If (assemblyGUID Is Nothing) Then
            Throw New System.ArgumentNullException("assemblyGuid")
        End If
    End Sub
    Public Sub New()
    End Sub
End Class
        </file>
    <file name="b.vb">
&lt;Assembly:ESAttribute("Test")&gt;
Class Class1
    Sub Blah()
    End Sub
End Class
        </file>
    <file name="c.vb">
&lt;Assembly:ESAttribute("Test2")&gt;
Module Module1
    Sub Main()
    End Sub
End Module
        </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors1)
            Assert.Equal(2, compilation.Assembly.GetAttributes.Count)
            Assert.Equal("ESAttribute(""Test"")", compilation.Assembly.GetAttributes.Item(0).ToString)
            Assert.Equal("ESAttribute(""Test2"")", compilation.Assembly.GetAttributes.Item(1).ToString)
        End Sub

        <Fact>
        Public Sub BC41001WRN_NoNonObsoleteConstructorOnBase3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoNonObsoleteConstructorOnBase4">
        <file name="a.vb">
            Imports System
            Class C1
                &lt;Obsolete&gt;
                Sub New()
                End Sub
            End Class
            Class C2
                Inherits C1
            End Class
        </file>
    </compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))
            Dim expectedErrors1 = <errors>
BC41001: Class 'C2' should declare a 'Sub New' because the 'Public Sub New()' in its base class 'C1' is marked obsolete.
            Class C2
                  ~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC41002WRN_NoNonObsoleteConstructorOnBase4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NoNonObsoleteConstructorOnBase4">
        <file name="a.vb">
            Imports System
            Class C1
                &lt;Obsolete("hello", False)&gt;
                Sub New()
                End Sub
            End Class
            Class C2
                Inherits C1
            End Class
        </file>
    </compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))
            Dim expectedErrors1 = <errors>
BC41002: Class 'C2' should declare a 'Sub New' because the 'Public Sub New()' in its base class 'C1' is marked obsolete: 'hello'.
            Class C2
                  ~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC41003WRN_RequiredNonObsoleteNewCall3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="RequiredNonObsoleteNewCall4">
        <file name="a.vb">
            Imports System
            Class C1
                &lt;Obsolete&gt;
                Sub New()
                End Sub
            End Class
            Class C2
                Inherits C1
                Sub New()
                End Sub
            End Class
        </file>
    </compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))
            Dim expectedErrors1 = <errors>
BC41003: First statement of this 'Sub New' should be an explicit call to 'MyBase.New' or 'MyClass.New' because the 'Public Sub New()' in the base class 'C1' of 'C2' is marked obsolete.
                Sub New()
                    ~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC41004WRN_RequiredNonObsoleteNewCall4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="RequiredNonObsoleteNewCall4">
        <file name="a.vb">
            Imports System
            Class C1
                &lt;Obsolete("hello", False)&gt;
                Sub New()
                End Sub
            End Class
            Class C2
                Inherits C1
                Sub New()
                End Sub
            End Class
        </file>
    </compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))
            Dim expectedErrors1 = <errors>
BC41004: First statement of this 'Sub New' should be an explicit call to 'MyBase.New' or 'MyClass.New' because the 'Public Sub New()' in the base class 'C1' of 'C2' is marked obsolete: 'hello'
                Sub New()
                    ~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact>
        Public Sub BC41007WRN_ConditionalNotValidOnFunction()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ConditionalNotValidOnFunction">
        <file name="a.vb">
            Imports System.Diagnostics
            Public Structure S1
                &lt;Conditional("hello")&gt;
                Public Shared Operator +(ByVal v As S1, ByVal w As S1) As Object
                    Return Nothing
                End Operator
            End Structure
        </file>
    </compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))
            Dim expectedErrors1 = <errors>
BC41007: Attribute 'Conditional' is only valid on 'Sub' declarations.
                Public Shared Operator +(ByVal v As S1, ByVal w As S1) As Object
                                       ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        <CompilerTrait(CompilerFeature.IOperation)>
        <WorkItem(23282, "https://github.com/dotnet/roslyn/issues/23282")>
        Public Sub BC41998WRN_RecursiveAddHandlerCall()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="RecursiveAddHandlerCall">
        <file name="a.vb">
            Module M1
                Public Delegate Sub test_x()
                Sub test_d1()
                End Sub
                Public Custom Event t As test_x
                    AddHandler(ByVal value As test_x)
                        AddHandler t, AddressOf test_d1
                    End AddHandler
                    RemoveHandler(ByVal value As test_x)
                        RemoveHandler t, AddressOf test_d1
                    End RemoveHandler
                    RaiseEvent()
                        RaiseEvent t()
                    End RaiseEvent
                End Event
            End Module
        </file>
    </compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))
            Dim expectedErrors1 = <errors>
BC41998: Statement recursively calls the containing 'AddHandler' for event 't'.
                        AddHandler t, AddressOf test_d1
                        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC41998: Statement recursively calls the containing 'RemoveHandler' for event 't'.
                        RemoveHandler t, AddressOf test_d1
                        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC41998: Statement recursively calls the containing 'RaiseEvent' for event 't'.
                        RaiseEvent t()
                        ~~~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors1)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)

            Dim add = tree.GetRoot().DescendantNodes().OfType(Of AddRemoveHandlerStatementSyntax)().First()

            Assert.Equal("AddHandler t, AddressOf test_d1", add.ToString())

            compilation.VerifyOperationTree(add, expectedOperationTree:=
            <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler  ... sOf test_d1')
  Expression: 
    IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler  ... sOf test_d1')
      Event Reference: 
        IEventReferenceOperation: Event M1.t As M1.test_x (Static) (OperationKind.EventReference, Type: M1.test_x) (Syntax: 't')
          Instance Receiver: 
            null
      Handler: 
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: M1.test_x, IsImplicit) (Syntax: 'AddressOf test_d1')
          Target: 
            IMethodReferenceOperation: Sub M1.test_d1() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf test_d1')
              Instance Receiver: 
                null
]]>.Value)

            Assert.Equal("Event M1.t As M1.test_x", model.GetSymbolInfo(add.EventExpression).Symbol.ToTestDisplayString())

            Dim remove = tree.GetRoot().DescendantNodes().OfType(Of AddRemoveHandlerStatementSyntax)().Last()

            Assert.Equal("RemoveHandler t, AddressOf test_d1", remove.ToString())

            compilation.VerifyOperationTree(remove, expectedOperationTree:=
            <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'RemoveHandl ... sOf test_d1')
  Expression: 
    IEventAssignmentOperation (EventRemove) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'RemoveHandl ... sOf test_d1')
      Event Reference: 
        IEventReferenceOperation: Event M1.t As M1.test_x (Static) (OperationKind.EventReference, Type: M1.test_x) (Syntax: 't')
          Instance Receiver: 
            null
      Handler: 
        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: M1.test_x, IsImplicit) (Syntax: 'AddressOf test_d1')
          Target: 
            IMethodReferenceOperation: Sub M1.test_d1() (Static) (OperationKind.MethodReference, Type: null) (Syntax: 'AddressOf test_d1')
              Instance Receiver: 
                null
]]>.Value)

            Assert.Equal("Event M1.t As M1.test_x", model.GetSymbolInfo(remove.EventExpression).Symbol.ToTestDisplayString())

            Dim raise = tree.GetRoot().DescendantNodes().OfType(Of RaiseEventStatementSyntax)().Single()

            Assert.Equal("RaiseEvent t()", raise.ToString())

            compilation.VerifyOperationTree(raise, expectedOperationTree:=
            <![CDATA[
IRaiseEventOperation (OperationKind.RaiseEvent, Type: null) (Syntax: 'RaiseEvent t()')
  Event Reference: 
    IEventReferenceOperation: Event M1.t As M1.test_x (Static) (OperationKind.EventReference, Type: M1.test_x) (Syntax: 't')
      Instance Receiver: 
        null
  Arguments(0)
]]>.Value)

            Assert.Equal("Event M1.t As M1.test_x", model.GetSymbolInfo(raise.Name).Symbol.ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub BC41999WRN_ImplicitConversionCopyBack()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ImplicitConversionCopyBack">
        <file name="a.vb">
            Namespace ns1
                Module M1
                    Sub foo(ByRef x As Object)
                    End Sub
                    Sub FOO1()
                        Dim o As New System.Exception
                        foo(o)
                    End Sub
                End Module
            End Namespace
        </file>
    </compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))
            Dim expectedErrors1 = <errors>
BC41999: Implicit conversion from 'Object' to 'Exception' in copying the value of 'ByRef' parameter 'x' back to the matching argument.
                        foo(o)
                            ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42016WRN_ImplicitConversionSubst1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ImplicitConversionSubst1">
        <file name="a.vb">
            Module Module1
                Sub Main()
                    Dim a As Integer = 2
                    Dim b As Integer = 2
                    Dim c As Integer = a / b
                    a = c
                End Sub
            End Module
        </file>
    </compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))
            Dim expectedErrors1 = <errors>
BC42016: Implicit conversion from 'Double' to 'Integer'.
                    Dim c As Integer = a / b
                                       ~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42018WRN_ObjectMath1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ObjectMath1">
        <file name="a.vb">
            Module Module1
                Sub Main()
                    Dim o As New Object
                    o = o = 1
                End Sub
            End Module
        </file>
    </compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))
            Dim expectedErrors1 = <errors>
BC42018: Operands of type Object used for operator '='; use the 'Is' operator to test object identity.
                    o = o = 1
                        ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42019WRN_ObjectMath2_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ObjectMath2">
        <file name="a.vb">
            Module Module1
                Sub Main()
                    Dim o As New Object
                    o = o + 1
                End Sub
            End Module
        </file>
    </compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))
            Dim expectedErrors1 = <errors>
BC42019: Operands of type Object used for operator '+'; runtime errors could occur.
                    o = o + 1
                        ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42019WRN_ObjectMath2_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ObjectMath2">
        <file name="a.vb">
            Module Module1
                function Main()
                    Dim o As New Object
                    return o * o
                End function
            End Module
        </file>
    </compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))
            Dim expectedErrors1 = <errors>
BC42021: Function without an 'As' clause; return type of Object assumed.
                function Main()
                         ~~~~
BC42019: Operands of type Object used for operator '*'; runtime errors could occur.
                    return o * o
                           ~
BC42019: Operands of type Object used for operator '*'; runtime errors could occur.
                    return o * o
                               ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42024WRN_UnusedLocal()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="UnusedLocal">
        <file name="a.vb">
            Public Module Module1
                Public Sub Main()
                    Dim c1 As Class1
                End Sub
                Class Class1
                End Class
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42024: Unused local variable: 'c1'.
                    Dim c1 As Class1
                        ~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42025WRN_SharedMemberThroughInstance_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
            Module Module1
                Structure S1
                    Structure S1
                        Public Shared strTemp As String
                    End Structure
                End Structure
                Sub Main()
                    Dim obj As New S1.S1
                    obj.strTemp = "S"
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                    obj.strTemp = "S"
                    ~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42025WRN_SharedMemberThroughInstance_2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
            Class C
                Shared Property P
                    Get
                        Return Nothing
                    End Get
                    Set(ByVal value)
                    End Set
                End Property
                Sub M()
                    Dim o = Me.P
                    Me.P = o
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                    Dim o = Me.P
                            ~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
                    Me.P = o
                    ~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(528718, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528718")>
        <Fact()>
        Public Sub BC42025WRN_SharedMemberThroughInstance_3()
            CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation>
        <file name="a.vb">
Option Infer On
Imports System.Runtime.CompilerServices
Module M
    Sub Main()
        For Each y In 1
        Next
    End Sub
    &lt;Extension()&gt;
    Function GetEnumerator(x As Integer) As E
        Return New E
    End Function
End Module
Class E
    Function MoveNext() As Boolean
        Return True
    End Function
    Shared Property Current As Boolean
        Get
            Return True
        End Get
        Set(ByVal value As Boolean)
        End Set
    End Property
End Class
        </file>
    </compilation>, {Net40.References.SystemCore}).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_SharedMemberThroughInstance, "1"))
        End Sub

        <Fact()>
        Public Sub BC42025WRN_SharedMemberThroughInstance_4()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Enum E
    A
    B = B.A + 1
End Enum
        </file>
</compilation>)
            comp.AssertTheseDiagnostics(<errors>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
    B = B.A + 1
        ~~~
</errors>)
        End Sub

        <Fact(), WorkItem(528734, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528734")>
        Public Sub BC42026WRN_RecursivePropertyCall()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="RecursivePropertyCall">
        <file name="a.vb">
            Module Module1
                Class c1
                    ReadOnly Property Name() As String
                        Get
                            Return Me.Name
                        End Get
                    End Property
                End Class
            End Module

Module Program

    Sub PassByRef(ByRef x As Integer)
    End Sub

    Sub PassByVal(x As Integer)
    End Sub

    Property P1 As Integer
        Get
            If Date.Now.Day = 2 Then
                Return P1() + 1 ' 1
            ElseIf Date.Now.Day = 3 Then
                P1() += 1 ' 2
                Mid(P1(), 1) = 2 ' 3
                PassByRef(P1()) '4
                PassByVal(P1()) '5
            Else
                P1() = 2
            End If

            Return 0
        End Get
        Set(value As Integer)
            If Date.Now.Day = 2 Then
                P1() = value '6
            ElseIf Date.Now.Day = 3 Then
                P1() += 1 '7
                Mid(P1(), 1) = 2 '8
                PassByRef(P1()) '9
                PassByVal(P1())
            Else
                Dim x = P1()
            End If
        End Set
    End Property

    Property P2(a As Integer) As Integer
        Get
            If Date.Now.Day = 2 Then
                Return P2(a) + 1
            ElseIf Date.Now.Day = 3 Then
                P2(a) += 2
                Mid(P2(a), 1) = 2
                PassByRef(P2(a))
                PassByVal(P2(a))
            Else
                P2(a) = 2
            End If

            Return 0
        End Get
        Set(value As Integer)
            If Date.Now.Day = 2 Then
                P2(a) = value
            ElseIf Date.Now.Day = 3 Then
                P2(a) += 2
                Mid(P2(a), 1) = 2
                PassByRef(P2(a))
                PassByVal(P2(a))
            Else
                Dim x = P2(a)
            End If
        End Set
    End Property

End Module

Class Class1

    Property P3 As Integer
        Get
            If Date.Now.Day = 2 Then
                Return P3() + 1 '10
            ElseIf Date.Now.Day = 3 Then
                P3() += 2 ' 11
                Mid(P3(), 1) = 2 ' 12
                PassByRef(P3()) ' 13
                PassByVal(P3()) ' 14
            Else
                P3() = 2
            End If

            Return 0
        End Get
        Set(value As Integer)
            If Date.Now.Day = 2 Then
                P3() = value ' 15
            ElseIf Date.Now.Day = 3 Then
                P3() += 2 ' 16
                Mid(P3(), 1) = 2 ' 17
                PassByRef(P3()) ' 18
                PassByVal(P3())
            Else
                Dim x = P3()
            End If
        End Set
    End Property

    Property P4 As Integer
        Get
            If Date.Now.Day = 2 Then
                Return P4 + 1
            ElseIf Date.Now.Day = 3 Then
                P4 += 2
                Mid(P4, 1) = 2
                PassByRef(P4)
                PassByVal(P4)
            Else
                P4 = 2
            End If

            Return 0
        End Get
        Set(value As Integer)
            If Date.Now.Day = 2 Then
                P4 = value ' 19
            ElseIf Date.Now.Day = 3 Then
                P4 += 2 ' 20
                Mid(P4, 1) = 2 ' 21
                PassByRef(P4) ' 22
                PassByVal(P4)
            Else
                Dim x = P4
            End If
        End Set
    End Property

    Property P5 As Integer
        Get
            If Date.Now.Day = 2 Then
                Return Me.P5 + 1 ' 23
            ElseIf Date.Now.Day = 3 Then
                Me.P5 += 2 ' 24
                Mid(Me.P5, 1) = 2 ' 25
                PassByRef(Me.P5) ' 26
                PassByVal(Me.P5) ' 27
            Else
                Me.P5 = 2
            End If

            Return 0
        End Get
        Set(value As Integer)
            If Date.Now.Day = 2 Then
                Me.P5 = value ' 28
            ElseIf Date.Now.Day = 3 Then
                Me.P5 += 2 ' 29
                Mid(Me.P5, 1) = 2 ' 30
                PassByRef(Me.P5) ' 31
                PassByVal(Me.P5)
            Else
                Dim x = Me.P5
            End If
        End Set
    End Property

    Property P6(a As Integer) As Integer
        Get
            If Date.Now.Day = 2 Then
                Return P6(a) + 1
            ElseIf Date.Now.Day = 3 Then
                P6(a) += 2
                Mid(P6(a), 1) = 2
                PassByRef(P6(a))
                PassByVal(P6(a))
            Else
                P6(a) = 2
            End If

            Return 0
        End Get
        Set(value As Integer)
            If Date.Now.Day = 2 Then
                P6(a) = value
            ElseIf Date.Now.Day = 3 Then
                P6(a) += 2
                Mid(P6(a), 1) = 2
                PassByRef(P6(a))
                PassByVal(P6(a))
            Else
                Dim x = P6(a)
            End If
        End Set
    End Property

    Property P7 As Integer
        Get
            Dim x = Me

            If Date.Now.Day = 2 Then
                Return x.P7() + 1
            ElseIf Date.Now.Day = 3 Then
                x.P7() += 2
                Mid(x.P7(), 1) = 2
                PassByRef(x.P7())
                PassByVal(x.P7())
            Else
                x.P7() = 2
            End If

            Return 0
        End Get
        Set(value As Integer)
            Dim x = Me

            If Date.Now.Day = 2 Then
                x.P7() = value
            ElseIf Date.Now.Day = 3 Then
                x.P7() += 2
                Mid(x.P7(), 1) = 2
                PassByRef(x.P7())
                PassByVal(x.P7())
            Else
                Dim y = x.P7()
            End If
        End Set
    End Property

End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42026: Expression recursively calls the containing property 'Public ReadOnly Property Name As String'.
                            Return Me.Name
                                   ~~~~~~~
BC42026: Expression recursively calls the containing property 'Public Property P1 As Integer'.
                Return P1() + 1 ' 1
                       ~~~~
BC42026: Expression recursively calls the containing property 'Public Property P1 As Integer'.
                P1() += 1 ' 2
                ~~~~
BC42026: Expression recursively calls the containing property 'Public Property P1 As Integer'.
                Mid(P1(), 1) = 2 ' 3
                    ~~~~
BC42026: Expression recursively calls the containing property 'Public Property P1 As Integer'.
                PassByRef(P1()) '4
                          ~~~~
BC42026: Expression recursively calls the containing property 'Public Property P1 As Integer'.
                PassByVal(P1()) '5
                          ~~~~
BC42026: Expression recursively calls the containing property 'Public Property P1 As Integer'.
                P1() = value '6
                ~~~~
BC42026: Expression recursively calls the containing property 'Public Property P1 As Integer'.
                P1() += 1 '7
                ~~~~
BC42026: Expression recursively calls the containing property 'Public Property P1 As Integer'.
                Mid(P1(), 1) = 2 '8
                    ~~~~
BC42026: Expression recursively calls the containing property 'Public Property P1 As Integer'.
                PassByRef(P1()) '9
                          ~~~~
BC42026: Expression recursively calls the containing property 'Public Property P3 As Integer'.
                Return P3() + 1 '10
                       ~~~~
BC42026: Expression recursively calls the containing property 'Public Property P3 As Integer'.
                P3() += 2 ' 11
                ~~~~
BC42026: Expression recursively calls the containing property 'Public Property P3 As Integer'.
                Mid(P3(), 1) = 2 ' 12
                    ~~~~
BC42026: Expression recursively calls the containing property 'Public Property P3 As Integer'.
                PassByRef(P3()) ' 13
                          ~~~~
BC42026: Expression recursively calls the containing property 'Public Property P3 As Integer'.
                PassByVal(P3()) ' 14
                          ~~~~
BC42026: Expression recursively calls the containing property 'Public Property P3 As Integer'.
                P3() = value ' 15
                ~~~~
BC42026: Expression recursively calls the containing property 'Public Property P3 As Integer'.
                P3() += 2 ' 16
                ~~~~
BC42026: Expression recursively calls the containing property 'Public Property P3 As Integer'.
                Mid(P3(), 1) = 2 ' 17
                    ~~~~
BC42026: Expression recursively calls the containing property 'Public Property P3 As Integer'.
                PassByRef(P3()) ' 18
                          ~~~~
BC42026: Expression recursively calls the containing property 'Public Property P4 As Integer'.
                P4 = value ' 19
                ~~
BC42026: Expression recursively calls the containing property 'Public Property P4 As Integer'.
                P4 += 2 ' 20
                ~~
BC42026: Expression recursively calls the containing property 'Public Property P4 As Integer'.
                Mid(P4, 1) = 2 ' 21
                    ~~
BC42026: Expression recursively calls the containing property 'Public Property P4 As Integer'.
                PassByRef(P4) ' 22
                          ~~
BC42026: Expression recursively calls the containing property 'Public Property P5 As Integer'.
                Return Me.P5 + 1 ' 23
                       ~~~~~
BC42026: Expression recursively calls the containing property 'Public Property P5 As Integer'.
                Me.P5 += 2 ' 24
                ~~~~~
BC42026: Expression recursively calls the containing property 'Public Property P5 As Integer'.
                Mid(Me.P5, 1) = 2 ' 25
                    ~~~~~
BC42026: Expression recursively calls the containing property 'Public Property P5 As Integer'.
                PassByRef(Me.P5) ' 26
                          ~~~~~
BC42026: Expression recursively calls the containing property 'Public Property P5 As Integer'.
                PassByVal(Me.P5) ' 27
                          ~~~~~
BC42026: Expression recursively calls the containing property 'Public Property P5 As Integer'.
                Me.P5 = value ' 28
                ~~~~~
BC42026: Expression recursively calls the containing property 'Public Property P5 As Integer'.
                Me.P5 += 2 ' 29
                ~~~~~
BC42026: Expression recursively calls the containing property 'Public Property P5 As Integer'.
                Mid(Me.P5, 1) = 2 ' 30
                    ~~~~~
BC42026: Expression recursively calls the containing property 'Public Property P5 As Integer'.
                PassByRef(Me.P5) ' 31
                          ~~~~~
</errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42029WRN_OverlappingCatch()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
            Imports System
            Module Module1
                Sub foo1()
                    Try
                    Catch ex As Exception
                    Catch ex As SystemException
                        Console.WriteLine(ex)
                    End Try
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42029: 'Catch' block never reached, because 'SystemException' inherits from 'Exception'.
                    Catch ex As SystemException
                    ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42030WRN_DefAsgUseNullRefByRef1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
            Class C
                Shared Sub M()
                    Dim o As Object
                    M(o)
                End Sub
                Shared Sub M(ByRef o As Object)
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42030: Variable 'o' is passed by reference before it has been assigned a value. A null reference exception could result at runtime.
                    M(o)
                      ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42030WRN_DefAsgUseNullRefByRef2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="DefAsgUseNullRefByRef">
        <file name="a.vb">
            Module M1
                Sub foo(ByRef x As Object)
                End Sub
                Structure S1
                    Dim o As Object
                    Dim e As Object
                End Structure
                Sub Main()
                    Dim x1 As S1
                    x1.o = Nothing
                    foo(x1.e)
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42030: Variable 'e' is passed by reference before it has been assigned a value. A null reference exception could result at runtime.
                    foo(x1.e)
                        ~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42031WRN_DuplicateCatch()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="DuplicateCatch">
        <file name="a.vb">
            Imports System
            Module Module1
                Sub foo()
                    Try
                    Catch ex As Exception
                    Catch ex As Exception
                    End Try
                End Sub
            End Module
        </file>
    </compilation>).VerifyDiagnostics(
            Diagnostic(ERRID.WRN_DuplicateCatch, "Catch ex As Exception").WithArguments("System.Exception"))
        End Sub

        <Fact()>
        Public Sub BC42032WRN_ObjectMath1Not()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ObjectMath1Not">
        <file name="a.vb">
            Module Module1
                Sub Main()
                    Dim Result
                    Dim X
                    If Result &lt;&gt; 13 Or X &lt;&gt; Nothing Then
                    End If
                End Sub
            End Module
        </file>
    </compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom))
            Dim expectedErrors1 = <errors>
BC42020: Variable declaration without an 'As' clause; type of Object assumed.
                    Dim Result
                        ~~~~~~
BC42020: Variable declaration without an 'As' clause; type of Object assumed.
                    Dim X
                        ~
BC42032: Operands of type Object used for operator '&lt;&gt;'; use the 'IsNot' operator to test object identity.
                    If Result &lt;&gt; 13 Or X &lt;&gt; Nothing Then
                       ~~~~~~
BC42104: Variable 'Result' is used before it has been assigned a value. A null reference exception could result at runtime.
                    If Result &lt;&gt; 13 Or X &lt;&gt; Nothing Then
                       ~~~~~~
BC42019: Operands of type Object used for operator 'Or'; runtime errors could occur.
                    If Result &lt;&gt; 13 Or X &lt;&gt; Nothing Then
                       ~~~~~~~~~~~~
BC42016: Implicit conversion from 'Object' to 'Boolean'.
                    If Result &lt;&gt; 13 Or X &lt;&gt; Nothing Then
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42032: Operands of type Object used for operator '&lt;&gt;'; use the 'IsNot' operator to test object identity.
                    If Result &lt;&gt; 13 Or X &lt;&gt; Nothing Then
                                       ~
BC42104: Variable 'X' is used before it has been assigned a value. A null reference exception could result at runtime.
                    If Result &lt;&gt; 13 Or X &lt;&gt; Nothing Then
                                       ~
BC42019: Operands of type Object used for operator 'Or'; runtime errors could occur.
                    If Result &lt;&gt; 13 Or X &lt;&gt; Nothing Then
                                       ~~~~~~~~~~~~
BC42032: Operands of type Object used for operator '&lt;&gt;'; use the 'IsNot' operator to test object identity.
                    If Result &lt;&gt; 13 Or X &lt;&gt; Nothing Then
                                            ~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        'vbc module1.vb /target:library /noconfig /optionstrict:custom
        <Fact()>
        Public Sub BC42036WRN_ObjectMathSelectCase()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ObjectMathSelectCase">
        <file name="a.vb">
            Module M1
                Sub Main()
                    Dim o As Object = 1
                    Select Case o
                        Case 1
                    End Select
                End Sub
            End Module
        </file>
    </compilation>, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Custom))
            Dim expectedErrors1 = <errors>
BC42036: Operands of type Object used in expressions for 'Select', 'Case' statements; runtime errors could occur.
                    Select Case o
                                ~
BC42016: Implicit conversion from 'Object' to 'Boolean'.
                        Case 1
                             ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42036WRN_ObjectMathSelectCase_02()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ObjectMathSelectCase">
        <file name="a.vb">
            Module M1
                Sub Main()
                    Dim o As Object = 1
                    Select Case 1
                        Case 2, o
                    End Select
                End Sub
            End Module
        </file>
    </compilation>, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Custom))
            Dim expectedErrors1 = <errors>
BC42016: Implicit conversion from 'Object' to 'Boolean'.
                        Case 2, o
                        ~~~~~~~~~
BC42036: Operands of type Object used in expressions for 'Select', 'Case' statements; runtime errors could occur.
                        Case 2, o
                                ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42037WRN_EqualToLiteralNothing()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="EqualToLiteralNothing">
        <file name="a.vb">
            Module M
                Sub F(o As Integer?)
                    Dim b As Boolean
                    b = (o = Nothing)
                    b = (Nothing = o)
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42037: This expression will always evaluate to Nothing (due to null propagation from the equals operator). To check if the value is null consider using 'Is Nothing'.
                    b = (o = Nothing)
                         ~~~~~~~~~~~
BC42037: This expression will always evaluate to Nothing (due to null propagation from the equals operator). To check if the value is null consider using 'Is Nothing'.
                    b = (Nothing = o)
                         ~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42038WRN_NotEqualToLiteralNothing()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
            <![CDATA[
            Module M
                Sub F(o As Integer?)
                    Dim b As Boolean
                    b = (o <> Nothing)
                    b = (Nothing <> o)
                End Sub
            End Module
]]>
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
                                      <![CDATA[
BC42038: This expression will always evaluate to Nothing (due to null propagation from the equals operator). To check if the value is not null consider using 'IsNot Nothing'.
                    b = (o <> Nothing)
                         ~~~~~~~~~~~~
BC42038: This expression will always evaluate to Nothing (due to null propagation from the equals operator). To check if the value is not null consider using 'IsNot Nothing'.
                    b = (Nothing <> o)
                         ~~~~~~~~~~~~
]]>
                                  </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42104WRN_DefAsgUseNullRef()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
            Class C
                Shared Function F()
                    Dim v As String
                    Return v
                End Function
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42104: Variable 'v' is used before it has been assigned a value. A null reference exception could result at runtime.
                    Return v
                           ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(546820, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546820")>
        <Fact()>
        Public Sub BC42104WRN_DefAsgUseNullRef_StaticLocal()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Public Module Program
    Sub Main()
        Static TypeCharacterTable As System.Collections.Generic.Dictionary(Of String, String)
        If TypeCharacterTable Is Nothing Then
            TypeCharacterTable = New System.Collections.Generic.Dictionary(Of String, String)
        End If
    End Sub
End Module
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1, <errors></errors>)
        End Sub

        <WorkItem(542080, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542080")>
        <Fact()>
        Public Sub BC42104WRN_DefAsgUseNullRef_1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Imports System
Class C
    Public Shared Sub Main()
        Dim x As Action
        For Each x In New Action() {}
            x.Invoke()
        Next
        x.Invoke()
    End Sub
End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.
        x.Invoke()
        ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42104WRN_DefAsgUseNullRef_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Class C
    Public Shared Sub Main()
        Dim S As String
        For Each x As String In "abc"
            For Each S In "abc"
                If S = "B"c Then
                    Continue For
                End If
            Next S
        Next x
        System.Console.WriteLine(S)
    End Sub
End Class
        </file>
    </compilation>)
            AssertTheseEmitDiagnostics(compilation,
<errors>
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Conversions.ToString' is not defined.
        For Each x As String In "abc"
                                ~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Conversions.ToString' is not defined.
            For Each S In "abc"
                          ~~~~~
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Operators.CompareString' is not defined.
                If S = "B"c Then
                   ~~~~~~~~
BC42104: Variable 'S' is used before it has been assigned a value. A null reference exception could result at runtime.
        System.Console.WriteLine(S)
                                 ~
</errors>)
        End Sub

        <WorkItem(542080, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542080")>
        <Fact()>
        Public Sub BC42104WRN_DefAsgUseNullRef_3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Class C
    Public Shared Sub Main()
        For Each x As String In "abc"
            Dim S As String
            For Each S In "abc"
                If S = "B"c Then
                    Continue For
                End If
            Next S
            System.Console.WriteLine(S)
        Next x
    End Sub
End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42104: Variable 'S' is used before it has been assigned a value. A null reference exception could result at runtime.
            System.Console.WriteLine(S)
                                     ~
                </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(542080, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542080")>
        <Fact()>
        Public Sub BC42104WRN_DefAsgUseNullRef_4()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Structure S
    Public X As Integer
    Public Y As String
End Structure

Public Module Program2
    Public Sub Main(args() As String)
        Dim a, b As New S() With {.Y = b.Y}
        Dim c, d As New S() With {.Y = c.Y}
        Dim e, f As New S() With {.Y = .Y}
    End Sub
End Module
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC42104: Variable 'Y' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim a, b As New S() With {.Y = b.Y}
                                       ~~~
</errors>)
        End Sub

        <WorkItem(542080, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542080")>
        <Fact()>
        Public Sub BC42104WRN_DefAsgUseNullRef_5()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Structure S
    Public X As String
    Public Y As Object
End Structure

Public Module Program2
    Public Sub Main(args() As String)
        Dim a, b, c As New S() With {.Y = b.X, .X = c.Y}
    End Sub
End Module
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC42104: Variable 'X' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim a, b, c As New S() With {.Y = b.X, .X = c.Y}
                                          ~~~
BC42104: Variable 'Y' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim a, b, c As New S() With {.Y = b.X, .X = c.Y}
                                                    ~~~
</errors>)
        End Sub

        ''' <summary>
        ''' No warning reported for expression that will not be evaluated.
        ''' </summary>
        <Fact()>
        Public Sub BC42104WRN_DefAsgUseNullRef_ConstantUnevaluatedReceiver()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Class A
    Const F As Object = Nothing
    Shared Function M() As Object
        Dim o As A
        Return (o).F
    End Function
End Class
        </file>
    </compilation>)
            compilation.AssertTheseDiagnostics(<errors>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        Return (o).F
               ~~~~~
                 </errors>)
        End Sub

        ''' <summary>
        ''' No warning reported for expression that will not be evaluated.
        ''' </summary>
        <Fact()>
        Public Sub BC42104WRN_DefAsgUseNullRef_CallUnevaluatedReceiver()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Class A
    Shared Function F() As Object
        Return Nothing
    End Function
    Shared Function M() As Object
        Dim o As A
        Return o.F()
    End Function
End Class
        </file>
    </compilation>)
            compilation.AssertTheseDiagnostics(<errors>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        Return o.F()
               ~~~
                 </errors>)
        End Sub

        ''' <summary>
        ''' No warning reported for expression that will not be evaluated.
        ''' </summary>
        <Fact()>
        Public Sub BC42104WRN_DefAsgUseNullRef_AddressOfUnevaluatedReceiver()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Class A
    Shared Sub M()
    End Sub
    Shared Function F() As System.Action
        Dim o As A
        Return AddressOf (o).M
    End Function
End Class
        </file>
    </compilation>)
            compilation.AssertTheseDiagnostics(<errors>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        Return AddressOf (o).M
               ~~~~~~~~~~~~~~~
                 </errors>)
        End Sub

        ''' <summary>
        ''' No warning reported for expression that will not be evaluated.
        ''' </summary>
        <Fact()>
        Public Sub BC42104WRN_DefAsgUseNullRef_TypeExpressionUnevaluatedReceiver()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Class A
    Class B
        Friend Const F As Object = Nothing
    End Class
    Shared Function M() As Object
        Dim o As A
        Return o.B.F
    End Function
End Class
        </file>
    </compilation>)
            compilation.AssertTheseDiagnostics(<errors>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        Return o.B.F
               ~~~
                 </errors>)
        End Sub

        <WorkItem(528735, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528735")>
        <Fact()>
        Public Sub BC42105WRN_DefAsgNoRetValFuncRef1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="BC42105WRN_DefAsgNoRetValFuncRef1">
        <file name="a.vb">
Interface I
End Interface
Class A
End Class
Class C(Of T1, T2 As Class, T3 As Structure, T4 As New, T5 As I, T6 As A, T7 As U, U)
    Function F0() As Object
    End Function ' F0
    Function F1() As T1
    End Function ' F1
    Function F2() As T2
    End Function ' F2
    Function F3() As T3
    End Function ' F3
    Function F4() As T4
    End Function ' F4
    Function F5() As T5
    End Function ' F5
    Function F6() As T6
    End Function ' F6
    Function F7() As T7
    End Function ' F7
End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42105: Function 'F0' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
    End Function ' F0
    ~~~~~~~~~~~~
BC42105: Function 'F2' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
    End Function ' F2
    ~~~~~~~~~~~~
BC42105: Function 'F6' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
    End Function ' F6
    ~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(545313, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545313")>
        <Fact()>
        Public Sub BC42105WRN_DefAsgNoRetValFuncRef1b()
            ' Make sure initializers are analyzed for errors/warnings
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BC42105WRN_DefAsgNoRetValFuncRef1b">
        <file name="a.vb">
Option Strict On
Imports System

Module Module1
    Dim f As Func(Of String) = Function() As String
                               End Function
    Sub S()
        Dim l As Func(Of String) = Function() As String
                                   End Function
    End Sub
End Module

        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors><![CDATA[
BC42105: Function '<anonymous method>' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
                               End Function
                               ~~~~~~~~~~~~
BC42105: Function '<anonymous method>' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
                                   End Function
                                   ~~~~~~~~~~~~
]]>
</errors>)
        End Sub

        <WorkItem(837973, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/837973")>
        <Fact()>
        Public Sub Bug837973()
            Dim compilation1 = CompilationUtils.CreateEmptyCompilationWithReferences(
    <compilation>
        <file name="a.vb">
Imports System
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks

'COMPILEERRORTOTAL: 2
Public Module Async_Regress134668
    Public Function MethodFunction(x As Func(Of Object)) As Integer
        Return 1
    End Function
    Public Async Sub Async_Regress134668_Test()
        Await Task.Yield
        Dim i1 = MethodFunction(Async Function() As Task
                                    Await Task.Yield
                                End Function)
        Dim i2 = MethodFunction(Function() As Task
                                    'COMPILEWARNING: BC42105, "End Function"
        End Function) ' 1
 
        Dim i4 = MethodFunction(Function() As Task
            return Nothing
        End Function) ' 1
 
        Dim i3 = MethodFunction(Async Function() As Task(Of Integer)
                                    Await Task.Delay(10)
                                    'COMPILEWARNING: BC42105, "End Function"
        End Function) ' 2
    End Sub

    Public Function Async_Regress134668_Test2() As Object

        Async_Regress134668_Test2 = Nothing

        Dim i2 = MethodFunction(Function() As Task
        End Function) ' 3
    End Function ' 4

    Public Function Async_Regress134668_Test3() As Object
        Dim i2 = MethodFunction(Function() As Task
            Return Nothing
        End Function) 
    End Function ' 5
End Module
        </file>
    </compilation>, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseDll)

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors><![CDATA[
BC42105: Function '<anonymous method>' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
        End Function) ' 1
        ~~~~~~~~~~~~
BC42105: Function '<anonymous method>' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
        End Function) ' 2
        ~~~~~~~~~~~~
BC42105: Function '<anonymous method>' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
        End Function) ' 3
        ~~~~~~~~~~~~
BC42105: Function 'Async_Regress134668_Test3' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
    End Function ' 5
    ~~~~~~~~~~~~
]]></errors>)
        End Sub

        <WorkItem(545313, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545313")>
        <Fact()>
        Public Sub BC42105WRN_DefAsgNoRetValFuncRef1c()
            ' Make sure initializers are analyzed for errors/warnings ONLY ONCE 
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BC42105WRN_DefAsgNoRetValFuncRef1c">
        <file name="a.vb">
Option Strict On
Imports System

Class C1
    Private s As Func(Of String) = Function() As String
                                   End Function

    Public Sub New()
    End Sub

    Public Sub New(i As Integer)
        MyClass.New()
    End Sub

    Public Sub New(i As Long)
    End Sub
End Class
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
    <![CDATA[
BC42105: Function '<anonymous method>' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
                                   End Function
                                   ~~~~~~~~~~~~
]]>
</errors>)
        End Sub

        <Fact()>
        Public Sub BC42106WRN_DefAsgNoRetValOpRef1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DefAsgNoRetValOpSubst1">
        <file name="a.vb">
Interface I
End Interface
Class A
End Class
Class C(Of T1, T2 As Class, T3 As Structure, T4 As New, T5 As I, T6 As A, T7 As U, U)
    Class C0
        Shared Operator -(o As C0) As Object
        End Operator ' C0
    End Class
    Class C1
        Shared Operator -(o As C1) As T1
        End Operator ' C1
    End Class
    Class C2
        Shared Operator -(o As C2) As T2
        End Operator ' C2
    End Class
    Class C3
        Shared Operator -(o As C3) As T3
        End Operator ' C3
    End Class
    Class C4
        Shared Operator -(o As C4) As T4
        End Operator ' C4
    End Class
    Class C5
        Shared Operator -(o As C5) As T5
        End Operator ' C5
    End Class
    Class C6
        Shared Operator -(o As C6) As T6
        End Operator ' C6
    End Class
    Class C7
        Shared Operator -(o As C7) As T7
        End Operator ' C7
    End Class
End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42106: Operator '-' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
        End Operator ' C0
        ~~~~~~~~~~~~
BC42106: Operator '-' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
        End Operator ' C2
        ~~~~~~~~~~~~
BC42106: Operator '-' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
        End Operator ' C6
        ~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42107WRN_DefAsgNoRetValPropRef1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DefAsgNoRetValPropSubst1">
        <file name="a.vb">
Interface I
End Interface
Class A
End Class
Class C(Of T1, T2 As Class, T3 As Structure, T4 As New, T5 As I, T6 As A, T7 As U, U)
    ReadOnly Property P0 As Object
        Get
        End Get ' P0
    End Property
    ReadOnly Property P1 As T1
        Get
        End Get ' P1
    End Property
    ReadOnly Property P2 As T2
        Get
        End Get ' P2
    End Property
    ReadOnly Property P3 As T3
        Get
        End Get ' P3
    End Property
    ReadOnly Property P4 As T4
        Get
        End Get ' P4
    End Property
    ReadOnly Property P5 As T5
        Get
        End Get ' P5
    End Property
    ReadOnly Property P6 As T6
        Get
        End Get ' P6
    End Property
    ReadOnly Property P7 As T7
        Get
        End Get ' P7
    End Property
End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42107: Property 'P0' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
        End Get ' P0
        ~~~~~~~
BC42107: Property 'P2' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
        End Get ' P2
        ~~~~~~~
BC42107: Property 'P6' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
        End Get ' P6
        ~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(540421, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540421")>
        <Fact()>
        Public Sub BC42108WRN_DefAsgUseNullRefByRefStr()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
            Structure S
                Public Fld As String
            End Structure
            Class C
                Shared Sub M(p1 As String, ByRef p2 As String)
                    Dim s1 As S
                    Dim s2 As S
                    Dim l1 As String
                    Dim a1 As String() = New String(3) {}
                    a1(2) = "abc"

                    M(s1)
                    M(s2.Fld)
                    M(l1)
                    M(p1)
                    M(p2)
                    M(a1(2))
                End Sub
                Shared Sub M(ByRef o As Object)
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42108: Variable 's1' is passed by reference before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
                    M(s1)
                      ~~
BC42030: Variable 'Fld' is passed by reference before it has been assigned a value. A null reference exception could result at runtime.
                    M(s2.Fld)
                      ~~~~~~
BC42030: Variable 'l1' is passed by reference before it has been assigned a value. A null reference exception could result at runtime.
                    M(l1)
                      ~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42108_NoWarningForOutParameter()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Structure SSS
    Public F As String
End Structure

Module Program
    Sub Main(args As String())
        Dim s As SSS
        Call (New Dictionary(Of Integer, SSS)).TryGetValue(1, s)
    End Sub
End Module
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC42108: Variable 's' is passed by reference before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        Call (New Dictionary(Of Integer, SSS)).TryGetValue(1, s)
                                                              ~
</errors>)
        End Sub

        <Fact()>
        Public Sub BC42108_StillWarningForLateBinding()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Structure SSS
    Public F As String
End Structure

Module Program
    Sub Main(args As String())
        Dim s As SSS
        Dim o As Object = New Dictionary(Of Integer, SSS)
        o.TryGetValue(1, s)
    End Sub
End Module
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC42108: Variable 's' is passed by reference before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
        o.TryGetValue(1, s)
                         ~
</errors>)
        End Sub

        <WorkItem(540421, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540421")>
        <Fact()>
        Public Sub BC42108WRN_DefAsgUseNullRefByRefStr2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Class C
    Public Fld As String
End Class
Class CC
    Public FldC As C
    Public FldS As S
End Class
Structure S
    Public Fld As String
End Structure
Structure SS
    Public FldC As C
    Public FldS As S
End Structure
Class Main
    Shared Sub M(p1 As String, ByRef p2 As String)
        Dim s1 As S
        Dim s2 As SS
        Dim s3 As SS

        Dim c1 As C
        Dim c2 As CC
        Dim c3 As CC

        M(s1.Fld)
        M(s2.FldS.Fld)
        M(s3.FldC.Fld)

        M(c1.Fld)
        M(c2.FldS.Fld)
        M(c3.FldC.Fld)
    End Sub
    Shared Sub M(ByRef o As Object)
    End Sub
End Class
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
                                               <errors>
BC42030: Variable 'Fld' is passed by reference before it has been assigned a value. A null reference exception could result at runtime.
        M(s1.Fld)
          ~~~~~~
BC42030: Variable 'Fld' is passed by reference before it has been assigned a value. A null reference exception could result at runtime.
        M(s2.FldS.Fld)
          ~~~~~~~~~~~
BC42104: Variable 'FldC' is used before it has been assigned a value. A null reference exception could result at runtime.
        M(s3.FldC.Fld)
          ~~~~~~~
BC42104: Variable 'c1' is used before it has been assigned a value. A null reference exception could result at runtime.
        M(c1.Fld)
          ~~
BC42104: Variable 'c2' is used before it has been assigned a value. A null reference exception could result at runtime.
        M(c2.FldS.Fld)
          ~~
BC42104: Variable 'c3' is used before it has been assigned a value. A null reference exception could result at runtime.
        M(c3.FldC.Fld)
          ~~
                                               </errors>)
        End Sub

        <Fact()>
        Public Sub BC42109WRN_DefAsgUseNullRefStr()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
            Structure S
                Public F As Object
            End Structure
            Class C
                Shared Sub M()
                    Dim s As S
                    M(s)
                End Sub
                Shared Sub M(o As Object)
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42109: Variable 's' is used before it has been assigned a value. A null reference exception could result at runtime. Make sure the structure or all the reference members are initialized before use
                    M(s)
                      ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(546818, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546818")>
        <Fact()>
        Public Sub BC42109WRN_DefAsgUseNullRefStr_NoError()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Imports System

Public Structure AccObjFromWindow
    Public hwnd As IntPtr
    Public ppvObject As Object
End Structure

Class SSS
    Public Sub S()
        Dim input As AccObjFromWindow
        input.hwnd = IntPtr.Zero
        input.ppvObject = Nothing
        Dim a = input
    End Sub
End Class
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1, <errors></errors>)
        End Sub

        <WorkItem(547098, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547098")>
        <Fact()>
        Public Sub BC42109WRN_DefAsgUseNullRefStr_NoError2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System

   Module Module1
       Sub Main()
           Dim s As str1
           Dim sTmp As String = ""
           AddHandler s.e1, AddressOf s.sub1
           Call s.revent(True, sTmp)
           AddHandler s.e1, AddressOf s.sub2
           sTmp = ""
           Call s.revent(True, sTmp)  ' place BP here
        End Sub
   End Module

    Structure str1
        Dim i As Integer
        Public Event e1(ByVal b As Boolean, ByRef s As String)
        Sub revent(ByVal b As Boolean, ByRef s As String)
            RaiseEvent e1(b, s)
        End Sub
        Sub sub1(ByVal b As Boolean, ByRef s As String)
            s = s &amp; "1"
        End Sub
        Sub sub2(ByVal b As Boolean, ByRef s As String)
            s = s &amp; "2"
        End Sub
    End Structure
       </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1, <errors></errors>)
        End Sub

        <WorkItem(546377, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546377")>
        <WorkItem(546423, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546423")>
        <WorkItem(546420, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546420")>
        <Fact()>
        Public Sub Bug15747()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System 
Public Module Program
    Sub Main()
        Dim hscr As IntPtr
        Try
            hscr = New IntPtr
        Catch ex As Exception
            If Not hscr.Equals(IntPtr.Zero) Then
            End If
        End Try
    End Sub
End Module
        </file>
    </compilation>).VerifyDiagnostics()
        End Sub

        <WorkItem(546377, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546377")>
        <WorkItem(546423, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546423")>
        <WorkItem(546420, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546420")>
        <Fact()>
        Public Sub Bug15747b()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Imports System 
Public Module Program
    Sub Main()
        Dim hscr As UIntPtr
        Try
            hscr = New UIntPtr
        Catch ex As Exception
            If Not hscr.Equals(UIntPtr.Zero) Then
            End If
        End Try
    End Sub
End Module
        </file>
    </compilation>).VerifyDiagnostics()
        End Sub

        <WorkItem(546175, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546175")>
        <Fact()>
        Public Sub BC42109WRN_DefAsgUseNullRefStr_Dev11Compat()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Public Structure NonGenericReference
    Private s As String
    Public Structure GenericReference(Of T)
        Private s As String
    End Structure
End Structure

Public Structure GenericReference(Of T)
    Private s As String
    Public Structure NonGenericReference
        Private s As String
    End Structure
End Structure

Public Structure GenericReference2(Of T)
    Private s2 As Integer
End Structure
        </file>
    </compilation>)

            Dim reference1 = compilation1.EmitToImageReference()

            Dim reference2 = CreateReferenceFromIlCode(<![CDATA[
.class public sequential ansi sealed beforefieldinit GenericWithPtr`1<T>
       extends [mscorlib]System.ValueType
{
    //.field private int32* aa
} // end of class GenericWithPtr`1
]]>.Value)

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation>
        <file name="a.vb">
Module Module1
    Sub Main()
        Dim a As NonGenericReference
        Dim b As GenericReference(Of Integer)
        Dim c As GenericReference(Of String)
        Dim d As NonGenericReference.GenericReference(Of Integer)
        Dim e As NonGenericReference.GenericReference(Of String)
        Dim f As GenericReference(Of Integer).NonGenericReference
        Dim g As GenericReference(Of String).NonGenericReference
        Dim i As GenericReference2(Of Integer)
        Dim j As GenericReference2(Of String)
        Dim k As GenericWithPtr(Of Integer)
        Dim z1 = a ' No Warning
        Dim z2 = b
        Dim z3 = c
        Dim z4 = d
        Dim z5 = e
        Dim z6 = f
        Dim z7 = g
        Dim z8 = i ' No Warning
        Dim z9 = j ' No Warning
        Dim za = k ' No Warning
    End Sub
End Module
        </file>
    </compilation>, references:={reference1, reference2}).
            VerifyDiagnostics(Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "b").WithArguments("b"),
                              Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "c").WithArguments("c"),
                              Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "d").WithArguments("d"),
                              Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "e").WithArguments("e"),
                              Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "f").WithArguments("f"),
                              Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "g").WithArguments("g"))
        End Sub

        <WorkItem(546175, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546175")>
        <Fact()>
        Public Sub BC42109WRN_DefAsgUseNullRefStr_Dev11Compat2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Public Structure NonGenericReference
    Private s As String
    Public Structure GenericReference(Of T)
        Private s As String
    End Structure
End Structure

Public Structure GenericReference(Of T)
    Private s As String
    Public Structure NonGenericReference
        Private s As String
    End Structure
End Structure

Public Structure GenericReference2(Of T)
    Private s2 As Integer
End Structure
        </file>
    </compilation>)

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation>
        <file name="a.vb">
Module Module1
    Sub Main()
        Dim a As NonGenericReference
        Dim b As GenericReference(Of Integer)
        Dim c As GenericReference(Of String)
        Dim d As NonGenericReference.GenericReference(Of Integer)
        Dim e As NonGenericReference.GenericReference(Of String)
        Dim f As GenericReference(Of Integer).NonGenericReference
        Dim g As GenericReference(Of String).NonGenericReference
        Dim i As GenericReference2(Of Integer)
        Dim j As GenericReference2(Of String)
        Dim z1 = a ' Warning!!!
        Dim z2 = b
        Dim z3 = c
        Dim z4 = d
        Dim z5 = e
        Dim z6 = f
        Dim z7 = g
        Dim z8 = i ' No Warning
        Dim z9 = j ' No Warning
    End Sub
End Module
        </file>
    </compilation>, references:={New VisualBasicCompilationReference(compilation1)}).
            VerifyDiagnostics(Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "a").WithArguments("a"),
                              Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "b").WithArguments("b"),
                              Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "c").WithArguments("c"),
                              Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "d").WithArguments("d"),
                              Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "e").WithArguments("e"),
                              Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "f").WithArguments("f"),
                              Diagnostic(ERRID.WRN_DefAsgUseNullRefStr, "g").WithArguments("g"))
        End Sub

        <WorkItem(652008, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/652008")>
        <Fact()>
        Public Sub BC42110WRN_FieldInForNotExplicit_DiagnosticRemoved()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="FieldInForNotExplicit">
        <file name="a.vb">
            Class Customer
                Private Index As Integer
                Sub Main()
                    For Index = 1 To 10
                    Next
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors></errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42322WRN_InterfaceConversion2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
            Module Module1
                Sub Main()
                    Dim xx As System.Collections.Generic.IEnumerator(Of Integer) = "hello"
                End Sub
            End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42322: Runtime errors might occur when converting 'String' to 'IEnumerator(Of Integer)'.
                    Dim xx As System.Collections.Generic.IEnumerator(Of Integer) = "hello"
                                                                                   ~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(545479, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545479")>
        <Fact()>
        Public Sub BC42322WRN_InterfaceConversion2_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="c.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices
Public Interface I
End Interface
<C(DirectCast(New C(""), I))>
<ComImport()>
NotInheritable Class C
    Inherits Attribute
    Public Sub New(s As String)
    End Sub
End Class
    ]]></file>
</compilation>)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30934: Conversion from 'I' to 'String' cannot occur in a constant expression used as an argument to an attribute.
<C(DirectCast(New C(""), I))>
   ~~~~~~~~~~~~~~~~~~~~~~~~
BC42322: Runtime errors might occur when converting 'I' to 'String'.
<C(DirectCast(New C(""), I))>
   ~~~~~~~~~~~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC42324WRN_LiftControlVariableLambda()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="LiftControlVariableLambda">
        <file name="a.vb">
            Imports System
            Class Customer
                Sub Main()
                    For i As Integer = 1 To (function() i + 10)()
                        Dim exampleFunc1 As Func(Of Integer) = Function() i
                    Next

                    ' since Dev11 the scope for foreach loops has been changed; no warnings here
                    For each j as integer in (function(){1+j, 2+j})()
                        Dim exampleFunc2 As Func(Of Integer) = Function() j
                    Next
                End Sub
            End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42324: Using the iteration variable in a lambda expression may have unexpected results.  Instead, create a local variable within the loop and assign it the value of the iteration variable.
                    For i As Integer = 1 To (function() i + 10)()
                                                        ~
BC42324: Using the iteration variable in a lambda expression may have unexpected results.  Instead, create a local variable within the loop and assign it the value of the iteration variable.
                        Dim exampleFunc1 As Func(Of Integer) = Function() i
                                                                          ~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42326WRN_LambdaPassedToRemoveHandler()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="LambdaPassedToRemoveHandler">
        <file name="a.vb">
            Module Module1
                Event ProcessInteger(ByVal x As Integer)
                Sub Main()
                    AddHandler ProcessInteger, Function(m As Integer) m
                    RemoveHandler ProcessInteger, Function(m As Integer) m
                End Sub
            End Module
        </file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.WRN_LambdaPassedToRemoveHandler, "Function(m As Integer) m"))

        End Sub

        <WorkItem(545252, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545252")>
        <Fact()>
        Public Sub BC30413ERR_WithEventsIsDelegate()
            CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="LambdaPassedToRemoveHandler">
        <file name="a.vb">
Imports System
Class Derived
    WithEvents e As Action = Sub() Exit Sub

    WithEvents e1, e2 As New Integer()
End Class
        </file>
    </compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_WithEventsAsStruct, "e"),
                                      Diagnostic(ERRID.ERR_WithEventsAsStruct, "e1"),
                                      Diagnostic(ERRID.ERR_WithEventsAsStruct, "e2"))

        End Sub

        <WorkItem(545195, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545195")>
        <Fact()>
        Public Sub BC42328WRN_RelDelegatePassedToRemoveHandler()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="RelDelegatePassedToRemoveHandler">
        <file name="a.vb">
            Class A
    Public Val As Integer
    Sub New(ByVal i As Integer)
        Val = i
    End Sub
End Class
Class B
    Inherits A
    Sub New(ByVal i As Integer)
        MyBase.New(i)
    End Sub
End Class
Class C1
    Delegate Sub HandlerNum(ByVal i As Integer)
    Delegate Sub HandlerCls(ByVal a As B)
    Dim WithEvents inner As C1
    Event evtNum As HandlerNum
    Event evtCls As HandlerCls
    Sub HandleLong1(ByVal l As Long)
    End Sub
    Sub HandlesWithNum(ByVal l As Long) Handles inner.evtNum
    End Sub
    Sub foo()
        RemoveHandler evtNum, AddressOf HandleLong1
        inner = New C1()
        RemoveHandler inner.evtNum, AddressOf HandlesWithNum
    End Sub
End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42328: The 'AddressOf' expression has no effect in this context because the method argument to 'AddressOf' requires a relaxed conversion to the delegate type of the event. Assign the 'AddressOf' expression to a variable, and use the variable to add or remove the method as the handler.
        RemoveHandler evtNum, AddressOf HandleLong1
                                        ~~~~~~~~~~~
BC42328: The 'AddressOf' expression has no effect in this context because the method argument to 'AddressOf' requires a relaxed conversion to the delegate type of the event. Assign the 'AddressOf' expression to a variable, and use the variable to add or remove the method as the handler.
        RemoveHandler inner.evtNum, AddressOf HandlesWithNum
                                              ~~~~~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42335WRN_TypeInferenceAssumed3()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="TypeInferenceAssumed3">
        <file name="a.vb">
        Option Strict On
        Imports System.Collections.Generic
        Module M1
            Sub foo()
                foo1({})
                foo1({Nothing})
                Foo2({}, {})
                Foo2({Nothing}, {Nothing})
                Foo2({Nothing}, {})
            End Sub
            Sub foo1(Of t)(ByVal x As t())
            End Sub
            Sub Foo2(Of t)(ByVal x As t, ByVal y As t)
            End Sub
        End Module
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC42335: Data type of 't' in 'Public Sub foo1(Of t)(x As t())' could not be inferred. 'Object' assumed.
                foo1({})
                     ~~
BC42335: Data type of 't' in 'Public Sub foo1(Of t)(x As t())' could not be inferred. 'Object' assumed.
                foo1({Nothing})
                     ~~~~~~~~~
BC42335: Data type of 't' in 'Public Sub Foo2(Of t)(x As t, y As t)' could not be inferred. 'Object()' assumed.
                Foo2({}, {})
                     ~~
BC42335: Data type of 't' in 'Public Sub Foo2(Of t)(x As t, y As t)' could not be inferred. 'Object()' assumed.
                Foo2({Nothing}, {Nothing})
                     ~~~~~~~~~
BC42335: Data type of 't' in 'Public Sub Foo2(Of t)(x As t, y As t)' could not be inferred. 'Object()' assumed.
                Foo2({Nothing}, {})
                     ~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42349WRN_ObsoleteIdentityDirectCastForValueType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
            <compilation name="ObsoleteIdentityDirectCastForValueType">
                <file name="a.vb">
                    Class cls
                        Sub test()
                        Dim l1 = DirectCast(1L, Long) 
                        End Sub
                    End Class
                </file>
            </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42349: Using DirectCast operator to cast a value-type to the same type is obsolete.
                        Dim l1 = DirectCast(1L, Long) 
                                            ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC42349WRN_ObsoleteIdentityDirectCastForValueType_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
            <compilation name="ObsoleteIdentityDirectCastForValueType">
                <file name="a.vb">
                    Class C1
                        function test()
                            return DirectCast(True, Boolean) 
                        End function
                    End Class
                </file>
            </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42349: Using DirectCast operator to cast a value-type to the same type is obsolete.
                            return DirectCast(True, Boolean) 
                                              ~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC42351WRN_MutableStructureInUsing()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
            <compilation name="MutableStructureInUsing">
                <file name="a.vb">
                    Imports System

                    Structure MutableStructure
                        Implements IDisposable
                        Dim dummy As Integer ' this field triggers the warning
                        Public Sub Dispose() Implements System.IDisposable.Dispose
                        End Sub
                    End Structure

                    Structure ImmutableStructure
                        Implements IDisposable
                        Public Shared disposed As Integer
                        Public Sub Dispose() Implements System.IDisposable.Dispose
                            disposed = disposed + 1
                        End Sub
                    End Structure

                    Class ReferenceType
                        Implements IDisposable
                        Dim dummy As Integer 
                        Public Sub Dispose() Implements System.IDisposable.Dispose
                        End Sub
                    End Class

                    Module M1
                        Sub foo()
                            ' resource type is a concrete structure + immutable (OK)
                            Using a As New ImmutableStructure()
                            End Using
                            Using New ImmutableStructure() ' ok
                            End Using

                            ' resource type is a concrete structure + mutable (Warning)
                            Using b As New MutableStructure()
                            End Using
                            Using New MutableStructure() ' as expression also ok.
                            End Using

                            ' reference types + mutable (OK)
                            Using c As New ReferenceType()
                            End Using
                            Using New ReferenceType() ' ok
                            End Using
                        End Sub
                    End Module
                </file>
            </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42351: Local variable 'b' is read-only and its type is a structure. Invoking its members or passing it ByRef does not change its content and might lead to unexpected results. Consider declaring this variable outside of the 'Using' block.
                            Using b As New MutableStructure()
                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC42351WRN_MutableStructureInUsingGenericConstraints()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
            <compilation name="BC42351WRN_MutableStructureInUsingGenericConstraints">
                <file name="a.vb">
                    Imports System

                    Structure MutableStructure
                        Implements IDisposable
                        Dim dummy As Integer ' this field triggers the warning
                        Public Sub Dispose() Implements System.IDisposable.Dispose
                        End Sub
                    End Structure

                    Structure ImmutableStructure
                        Implements IDisposable
                        Public Shared disposed As Integer
                        Public Sub Dispose() Implements System.IDisposable.Dispose
                            disposed = disposed + 1
                        End Sub
                    End Structure

                    Class ReferenceType
                        Implements IDisposable
                        Dim dummy As Integer 
                        Public Sub Dispose() Implements System.IDisposable.Dispose
                        End Sub
                    End Class

                    Module M1
                       Sub foo(Of T as {Structure, IDisposable}, 
                                   U as {ReferenceType, New, IDisposable},
                                   V as {Structure})()

                            ' resource type is a type parameter with a structure constraint (Warning, always)
                            Using a As T = Directcast(new MutableStructure(), IDisposable)
                            End Using

                            ' resource type is a type parameter with a reference type constraint (OK)
                            Using b As U = TryCast(new ReferenceType(), U)
                            End Using

                            ' resource type is a type parameter with a structure constraint (Warning, always)
                            Using c As V = DirectCast(new ImmutableStructure(), IDisposable)
                            End Using
                        End Sub
                    End Module

                    ' Getting a concrete structure as a class constraint through overridden generic methods
                    Class BaseGeneric(Of S)
                        Public Overridable Sub MySub(Of T As S)(param As T)
                        End Sub
                    End Class

                    Class DerivedImmutable
                        Inherits BaseGeneric(Of ImmutableStructure)

                        Public Overrides Sub MySub(Of T As ImmutableStructure)(param As T)

                            Using immutable As T = New ImmutableStructure() ' OK

                            End Using
                        End Sub
                    End Class

                    Class DerivedMutable
                        Inherits BaseGeneric(Of MutableStructure)

                        Public Overrides Sub MySub(Of T As MutableStructure)(param As T)

                            Using mutable As T = New MutableStructure() ' shows warning

                            End Using
                        End Sub
                    End Class
                </file>
            </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42351: Local variable 'a' is read-only and its type is a structure. Invoking its members or passing it ByRef does not change its content and might lead to unexpected results. Consider declaring this variable outside of the 'Using' block.
                            Using a As T = Directcast(new MutableStructure(), IDisposable)
                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36010: 'Using' operand of type 'V' must implement 'System.IDisposable'.
                            Using c As V = DirectCast(new ImmutableStructure(), IDisposable)
                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42351: Local variable 'c' is read-only and its type is a structure. Invoking its members or passing it ByRef does not change its content and might lead to unexpected results. Consider declaring this variable outside of the 'Using' block.
                            Using c As V = DirectCast(new ImmutableStructure(), IDisposable)
                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42351: Local variable 'mutable' is read-only and its type is a structure. Invoking its members or passing it ByRef does not change its content and might lead to unexpected results. Consider declaring this variable outside of the 'Using' block.
                            Using mutable As T = New MutableStructure() ' shows warning
                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC42352WRN_MutableGenericStructureInUsing()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
            <compilation name="MutableGenericStructureInUsing">
                <file name="a.vb">
                    Imports System

                    Module M1
                        Class ReferenceType
                            Implements IDisposable
                            Dim dummy As Integer 
                            Public Sub Dispose() Implements System.IDisposable.Dispose
                            End Sub
                        End Class

                        Sub Foo(Of T As {New, IDisposable}, U As {New, IDisposable})(ByVal x As T)
                            'COMPILEWARNING: BC42352, "a", BC42352, "b"
                            Using a as T = DirectCast(New ReferenceType(), IDisposable), b As New U()
                            End Using
                        End Sub
                    End Module
                </file>
            </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42352: Local variable 'a' is read-only. When its type is a structure, invoking its members or passing it ByRef does not change its content and might lead to unexpected results. Consider declaring this variable outside of the 'Using' block.
                            Using a as T = DirectCast(New ReferenceType(), IDisposable), b As New U()
                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC42352: Local variable 'b' is read-only. When its type is a structure, invoking its members or passing it ByRef does not change its content and might lead to unexpected results. Consider declaring this variable outside of the 'Using' block.
                            Using a as T = DirectCast(New ReferenceType(), IDisposable), b As New U()
                                                                                         ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC42353WRN_DefAsgNoRetValFuncVal1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
            <compilation>
                <file name="a.vb">
                    Structure gStr1(Of T)
                        Function Fun1(ByVal t1 As T) As Boolean
                        End Function
                    End Structure
                </file>
            </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42353: Function 'Fun1' doesn't return a value on all code paths. Are you missing a 'Return' statement?
                        End Function
                        ~~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(542802, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542802")>
        <Fact()>
        Public Sub BC42353WRN_DefAsgNoRetValFuncVal1_Lambda()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
            <compilation>
                <file name="a.vb">
Imports System.Linq
Class BaseClass
    Sub Method()
        Dim x = New Integer() {}
        x.Where(Function(y)
                    Exit Function
                    Return y = ""
                End Function)
    End Sub
End Class
Class DerivedClass
    Inherits BaseClass
    Shared Sub Main()
    End Sub
End Class
                </file>
            </compilation>, {Net40.References.SystemCore})

            VerifyDiagnostics(compilation, Diagnostic(ERRID.WRN_DefAsgNoRetValFuncVal1, "End Function").WithArguments("<anonymous method>"))
        End Sub

        <WorkItem(542816, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542816")>
        <Fact()>
        Public Sub BC42353WRN_DefAsgNoRetValFuncVal1_Lambda_2()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
            <compilation>
                <file name="a.vb">
Imports System.Linq                    
Class BaseClass
    Function Method()
        Dim x = New Integer() {}
        x.Where(Function(y)
                    Exit Function ' 0
                    Return y = ""
                End Function) ' 1
    End Function ' 2
End Class
Class DerivedClass
    Inherits BaseClass
    Shared Sub Main()
    End Sub
End Class
                </file>
            </compilation>, {Net40.References.SystemCore})

            AssertTheseDiagnostics(compilation,
                                   <expected><![CDATA[
BC42353: Function '<anonymous method>' doesn't return a value on all code paths. Are you missing a 'Return' statement?
                End Function) ' 1
                ~~~~~~~~~~~~
BC42105: Function 'Method' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
    End Function ' 2
    ~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub BC42353WRN_DefAsgNoRetValFuncVal1_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
            <compilation>
                <file name="a.vb">
                    Structure gStr1
                        Function Fun1(y As Integer) As Boolean
                            Exit Function
                            'Return y = ""
                        End Function
                    End Structure
                </file>
            </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42353: Function 'Fun1' doesn't return a value on all code paths. Are you missing a 'Return' statement?
                        End Function
                        ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC42353WRN_DefAsgNoRetValFuncVal1_Query_Lambda()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
            <compilation>
                <file name="a.vb">
Imports System
Imports System.Linq
Imports System.Runtime.CompilerServices

Class BaseClass
    Sub Method()
        Dim x = New Integer() {}
        x.Where(Function(y)
                    Exit Function
                    Return y = ""
                End Function)
    End Sub
End Class
Class DerivedClass
    Inherits BaseClass
    Shared Sub Main()
    End Sub
End Class
                </file>
            </compilation>, {Net40.References.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42353: Function '&lt;anonymous method&gt;' doesn't return a value on all code paths. Are you missing a 'Return' statement?
                End Function)
                ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC42354WRN_DefAsgNoRetValOpVal1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
            <compilation>
                <file name="a.vb">
                    Class C
                        Shared Operator -(x As C) As Integer
                        End Operator
                    End Class
                </file>
            </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42354: Operator '-' doesn't return a value on all code paths. Are you missing a 'Return' statement?
                        End Operator
                        ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC42355WRN_DefAsgNoRetValPropVal1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
            <compilation>
                <file name="a.vb">
                Class C
                    Shared F As Boolean
                    Shared ReadOnly Property P As Integer
                        Get
                            If F Then
                                Return 0
                            End If
                        End Get
                    End Property
                End Class
                </file>
            </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42355: Property 'P' doesn't return a value on all code paths. Are you missing a 'Return' statement?
                        End Get
                        ~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub BC42356WRN_UnreachableCode_MethodBody()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
            Imports System

            Module M1
                Sub Main()
                    dim x as integer = 0
                    goto l1
                        if x > 0 then
                    l1:
                        Console.WriteLine("hello 1.")
                        end if

                    goto l2
                        Console.WriteLine("hello 2.")
                        Console.WriteLine("hello 3.")
                    l2:
                End Sub
            End Module
        </file>
    </compilation>)

            Dim expectedErrors1 = <errors>
                                  </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42356WRN_UnreachableCode_LambdaBody()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
            Imports System

            Module M1
                Sub Main()
                    dim y as Action = sub()
                        dim x as integer = 0
                        goto l1
                            if x > 0 then
                        l1:
                            Console.WriteLine("hello 1.")
                            end if

                        goto l2
                            Console.WriteLine("hello 2.")
                            Console.WriteLine("hello 3.")
                        l2:
                    End Sub
                End Sub
            End Module
        </file>
    </compilation>)

            Dim expectedErrors1 = <errors>
                                  </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub BC42359WRN_EmptyPrefixAndXmlnsLocalName()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports <xmlns:empty="">
Module M
    Private F As Object = <x empty:xmlns="http://roslyn"/>
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC42368: The xmlns attribute has special meaning and should not be written with a prefix.
    Private F As Object = <x empty:xmlns="http://roslyn"/>
                             ~~~~~~~~~~~
]]></errors>)
        End Sub

        <Fact()>
        Public Sub BC42360WRN_PrefixAndXmlnsLocalName()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="c.vb"><![CDATA[
Option Strict On
Imports <xmlns:p1="http://roslyn/">
Module M
    Private F1 As Object = <x p1:xmlns="http://roslyn/1"/>
    Private F2 As Object = <x p2:xmlns="http://roslyn/2"/>
    Private F3 As Object = <x xmlns:p3="http://roslyn/3a" p3:xmlns="http://roslyn/3b"/>
End Module
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC42360: It is not recommended to have attributes named xmlns. Did you mean to write 'xmlns:p1' to define a prefix named 'p1'?
    Private F1 As Object = <x p1:xmlns="http://roslyn/1"/>
                              ~~~~~~~~
BC31148: XML namespace prefix 'p2' is not defined.
    Private F2 As Object = <x p2:xmlns="http://roslyn/2"/>
                              ~~
BC42360: It is not recommended to have attributes named xmlns. Did you mean to write 'xmlns:p2' to define a prefix named 'p2'?
    Private F2 As Object = <x p2:xmlns="http://roslyn/2"/>
                              ~~~~~~~~
BC42360: It is not recommended to have attributes named xmlns. Did you mean to write 'xmlns:p3' to define a prefix named 'p3'?
    Private F3 As Object = <x xmlns:p3="http://roslyn/3a" p3:xmlns="http://roslyn/3b"/>
                                                          ~~~~~~~~
]]></errors>)
        End Sub

#End Region

        <Fact>
        Public Sub Bug17315_CompilationUnit()
            Dim c = CompilationUtils.CreateEmptyCompilationWithReferences(
<compilation name="C">
    <file>
End Class
    </file>
</compilation>,
    Enumerable.Empty(Of MetadataReference)())

            c.AssertTheseDiagnostics(
<errors>
BC30002: Type 'System.Void' is not defined.
End Class
~~~~~~~~~
BC30460: 'End Class' must be preceded by a matching 'Class'.
End Class
~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'C.dll' failed.
End Class
~~~~~~~~~
</errors>)
        End Sub

        <WorkItem(938459, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/938459")>
        <Fact>
        Public Sub UnimplementedMethodsIncorrectSquiggleLocationInterfaceInheritanceOrdering()
            Dim c = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="C">
    <file>
Module Module1
    Sub Main()
    End Sub
End Module

Interface IBase
    Sub method1()
End Interface

Interface IBase2
    Sub method2()
End Interface

Interface IDerived
    Inherits IBase
    Sub method3()
End Interface

Interface IDerived2
    Inherits IDerived
    Sub method4()
End Interface

Class foo
    Implements IDerived2, IDerived, IBase, IBase2

    Public Sub method1() Implements IBase.method1
    End Sub

    Public Sub method2() Implements IBase2.method2
    End Sub

    Public Sub method4() Implements IDerived2.method4
    End Sub
End Class
    </file>
</compilation>)

            c.AssertTheseDiagnostics(
            <errors>
BC30149: Class 'foo' must implement 'Sub method3()' for interface 'IDerived'.
    Implements IDerived2, IDerived, IBase, IBase2
                          ~~~~~~~~
            </errors>)

            ' Change order so interfaces are defined in a completely different order.
            ' The bug related to order of interfaces.
            c = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="C">
    <file>
Module Module1
    Sub Main()
    End Sub
End Module

Interface IBase
    Sub method1()
End Interface

Interface IBase2
    Sub method2()
End Interface

Interface IDerived
    Inherits IBase
    Sub method3()
End Interface

Interface IDerived2
    Inherits IDerived
    Sub method4()
End Interface

Class foo
    Implements IBase, IBase2, IDerived2, IDerived

    Public Sub method4() Implements IDerived2.method4
    End Sub
    
    Public Sub method1() Implements IBase.method1
    End Sub

    Public Sub method2() Implements IBase2.method2
    End Sub
End Class
    </file>
</compilation>)

            c.AssertTheseDiagnostics(
            <errors>
BC30149: Class 'foo' must implement 'Sub method3()' for interface 'IDerived'.
    Implements IBase, IBase2, IDerived2, IDerived
                                         ~~~~~~~~            </errors>)
        End Sub

        <Fact>
        Public Sub Bug17315_Namespace()
            Dim c = CompilationUtils.CreateEmptyCompilationWithReferences(
<compilation name="C">
    <file>
Namespace N
    Sub Foo
    End Sub
End Namespace
    </file>
</compilation>,
    Enumerable.Empty(Of MetadataReference)())

            c.AssertTheseDiagnostics(
<errors>
BC30002: Type 'System.Void' is not defined.
Namespace N
~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'C.dll' failed.
Namespace N
~~~~~~~~~~~~
BC30001: Statement is not valid in a namespace.
    Sub Foo
    ~~~~~~~
BC30002: Type 'System.Void' is not defined.
    Sub Foo
    ~~~~~~~~
</errors>)
        End Sub

        <Fact(), WorkItem(530126, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530126")>
        Public Sub Bug_15314_Class()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NotYetImplementedInRoslyn">
        <file name="a.vb">
Public Class C
    Public Sub New()
    Protected Class D
        Public Sub New()
    End Class
End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30026: 'End Sub' expected.
    Public Sub New()
    ~~~~~~~~~~~~~~~~
BC30289: Statement cannot appear within a method body. End of method assumed.
    Protected Class D
    ~~~~~~~~~~~~~~~~~
BC30026: 'End Sub' expected.
        Public Sub New()
        ~~~~~~~~~~~~~~~~
                                  </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(530126, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530126")>
        Public Sub Bug_15314_Interface()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NotYetImplementedInRoslyn">
        <file name="a.vb">
Public Class C
    Public Sub New()
    Protected Interface D
    End Interface
End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30026: 'End Sub' expected.
    Public Sub New()
    ~~~~~~~~~~~~~~~~
BC30289: Statement cannot appear within a method body. End of method assumed.
    Protected Interface D
    ~~~~~~~~~~~~~~~~~~~~~
                                  </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(530126, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530126")>
        Public Sub Bug_15314_Enum()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NotYetImplementedInRoslyn">
        <file name="a.vb">

Public Class CE
    Public Sub New()
    Protected Enum DE
        blah
    End Enum
End Class

        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30026: 'End Sub' expected.
    Public Sub New()
    ~~~~~~~~~~~~~~~~
BC30289: Statement cannot appear within a method body. End of method assumed.
    Protected Enum DE
    ~~~~~~~~~~~~~~~~~
                                  </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(530126, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530126")>
        Public Sub Bug_15314_Structure()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NotYetImplementedInRoslyn">
        <file name="a.vb">
Public Class CS
    Public Sub New()
    Protected Structure DS
    End Structure
End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30026: 'End Sub' expected.
    Public Sub New()
    ~~~~~~~~~~~~~~~~
BC30289: Statement cannot appear within a method body. End of method assumed.
    Protected Structure DS
    ~~~~~~~~~~~~~~~~~~~~~~
                                  </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact()>
        Public Sub Bug4185()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NotYetImplementedInRoslyn">
        <file name="a.vb">
Public Class Hello5
    Public Shared Sub Main(args As String())
        System.Console.WriteLine("Hello, World!")
        Environent.ExitCode = 0
    End Sub
End Class
        </file>
    </compilation>)
            Dim expectedErrors1 = <errors>
BC30451: 'Environent' is not declared. It may be inaccessible due to its protection level.
        Environent.ExitCode = 0
        ~~~~~~~~~~
                 </errors>
            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <Fact(), WorkItem(545179, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545179")>
        Public Sub Bug13459()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="NotYetImplementedInRoslyn">
        <file name="a.vb">
Imports System
 
Module Program
    Function ReturnVoid() As System.Void
        Throw New Exception()
    End Function

    Sub Main()
        Dim x = Function() As System.Void
                    Throw New Exception()
                End Function
    End Sub
End Module
        </file>
    </compilation>)

            Dim expectedErrors1 = <errors>
BC31422: 'System.Void' can only be used in a GetType expression.
    Function ReturnVoid() As System.Void
                             ~~~~~~~~~~~
BC31422: 'System.Void' can only be used in a GetType expression.
        Dim x = Function() As System.Void
                              ~~~~~~~~~~~
                                  </errors>

            CompilationUtils.AssertTheseDiagnostics(compilation1, expectedErrors1)
        End Sub

        <WorkItem(541066, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541066")>
        <Fact()>
        Public Sub BaseConstructorImplicitCallWithoutSystemVoid()
            Dim compilation1 = CompilationUtils.CreateEmptyCompilationWithReferences(
    <compilation name="BaseConstructorImplicitCallWithoutSystemVoid">
        <file name="a.vb">
Public Class C1 
    Public Sub New()
    End Sub
End Class

Public Class C2 
    Inherits C1
End Class
        </file>
    </compilation>,
Enumerable.Empty(Of MetadataReference)())

            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC31091: Import of type 'Object' from assembly or module 'BaseConstructorImplicitCallWithoutSystemVoid.dll' failed.
Public Class C1 
             ~~
BC30002: Type 'System.Void' is not defined.
    Public Sub New()
    ~~~~~~~~~~~~~~~~~
BC30002: Type 'System.Void' is not defined.
Public Class C2 
~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <WorkItem(541066, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541066")>
        <Fact()>
        Public Sub ParamArrayBindingCheckForAttributeConstructor()
            Dim compilation1 = CompilationUtils.CreateEmptyCompilationWithReferences(
    <compilation name="ParamArrayBindingCheckForAttributeConstructor">
        <file name="a.vb">
Public Class C1 
    Public Sub S(ParamArray p As String())
    End Sub
End Class
        </file>
    </compilation>,
Enumerable.Empty(Of MetadataReference)())
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC30002: Type 'System.Void' is not defined.
Public Class C1 
~~~~~~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'ParamArrayBindingCheckForAttributeConstructor.dll' failed.
Public Class C1 
             ~~
BC30002: Type 'System.Void' is not defined.
    Public Sub S(ParamArray p As String())
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.ParamArrayAttribute..ctor' is not defined.
    Public Sub S(ParamArray p As String())
                 ~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'System.String' is not defined.
    Public Sub S(ParamArray p As String())
                                 ~~~~~~
</errors>)
        End Sub

        <WorkItem(541066, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541066")>
        <Fact()>
        Public Sub DefaultPropertyBindingCheckForAttributeConstructor()
            Dim compilation1 = CompilationUtils.CreateEmptyCompilationWithReferences(
    <compilation name="DefaultPropertyBindingCheckForAttributeConstructor">
        <file name="a.vb">
Public Class C1
    Public Default Property P(prop As String)
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
End Class
        </file>
    </compilation>,
Enumerable.Empty(Of MetadataReference)())
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC30002: Type 'System.Void' is not defined.
Public Class C1
~~~~~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'DefaultPropertyBindingCheckForAttributeConstructor.dll' failed.
Public Class C1
             ~~
BC35000: Requested operation is not available because the runtime library function 'System.Reflection.DefaultMemberAttribute..ctor' is not defined.
    Public Default Property P(prop As String)
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'System.Object' is not defined.
    Public Default Property P(prop As String)
                            ~
BC30002: Type 'System.String' is not defined.
    Public Default Property P(prop As String)
                                      ~~~~~~
BC30002: Type 'System.Void' is not defined.
        Set
        ~~~
</errors>)
        End Sub

        <WorkItem(541066, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541066")>
        <Fact()>
        Public Sub ConstBindingCheckForDateTimeConstantConstructor()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DefaultPropertyBindingCheckForAttributeConstructor">
        <file name="a.vb">
Imports System
Public Class C1
    Const DT1 As DateTime = #01/01/2000#
    Const DT2 = #01/01/2000#
End Class
        </file>
        <file name="b.vb">
Namespace System.Runtime.CompilerServices
    Public Class DateTimeConstantAttribute
        Public Sub New(a As String)
        End Sub
    End Class
End Namespace
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC35000: Requested operation is not available because the runtime library function 'System.Runtime.CompilerServices.DateTimeConstantAttribute..ctor' is not defined.
    Const DT1 As DateTime = #01/01/2000#
                          ~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Runtime.CompilerServices.DateTimeConstantAttribute..ctor' is not defined.
    Const DT2 = #01/01/2000#
              ~~~~~~~~~~~~~~
</errors>)
        End Sub

        <WorkItem(541066, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541066")>
        <Fact()>
        Public Sub ConstBindingCheckForDecimalConstantConstructor()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="DefaultPropertyBindingCheckForAttributeConstructor">
        <file name="a.vb">
Imports System
Public Class C1
    Const DT1 As Decimal = 12d
    Const DT2 = 12d
End Class
        </file>
        <file name="b.vb">
Namespace System.Runtime.CompilerServices
    Public Class DecimalConstantAttribute
        Public Sub New(a As String)
        End Sub
    End Class
End Namespace
        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC35000: Requested operation is not available because the runtime library function 'System.Runtime.CompilerServices.DecimalConstantAttribute..ctor' is not defined.
    Const DT1 As Decimal = 12d
                         ~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Runtime.CompilerServices.DecimalConstantAttribute..ctor' is not defined.
    Const DT2 = 12d
              ~~~~~
</errors>)
        End Sub

        <WorkItem(541066, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541066")>
        <Fact()>
        Public Sub SynthesizedInstanceConstructorBinding()
            Dim compilation1 = CompilationUtils.CreateEmptyCompilationWithReferences(
    <compilation name="SynthesizedInstanceConstructorBinding">
        <file name="a.vb">
Public Class C1
End Class
        </file>
    </compilation>,
Enumerable.Empty(Of MetadataReference)())
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC30002: Type 'System.Void' is not defined.
Public Class C1
~~~~~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'SynthesizedInstanceConstructorBinding.dll' failed.
Public Class C1
             ~~
</errors>)
        End Sub

        <WorkItem(541066, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541066")>
        <Fact()>
        Public Sub SynthesizedSharedConstructorBinding()
            Dim compilation1 = CompilationUtils.CreateEmptyCompilationWithReferences(
    <compilation name="SynthesizedInstanceConstructorBinding">
        <file name="a.vb">
Public Class C1
    Const DA As DateTime = #1/1/1#
End Class
        </file>
    </compilation>,
Enumerable.Empty(Of MetadataReference)())
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC30002: Type 'System.Void' is not defined.
Public Class C1
~~~~~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'SynthesizedInstanceConstructorBinding.dll' failed.
Public Class C1
             ~~
BC30002: Type 'DateTime' is not defined.
    Const DA As DateTime = #1/1/1#
                ~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'SynthesizedInstanceConstructorBinding.dll' failed.
    Const DA As DateTime = #1/1/1#
                ~~~~~~~~
BC30002: Type 'System.DateTime' is not defined.
    Const DA As DateTime = #1/1/1#
                           ~~~~~~~
</errors>)
        End Sub

        <WorkItem(541066, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541066")>
        <Fact()>
        Public Sub EnumWithoutMscorReference()
            Dim compilation1 = CompilationUtils.CreateEmptyCompilationWithReferences(
    <compilation name="EnumWithoutMscorReference">
        <file name="a.vb">
Enum E
    A
End Enum
        </file>
    </compilation>,
Enumerable.Empty(Of MetadataReference)())
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC30002: Type 'System.Void' is not defined.
Enum E
~~~~~~~
BC30002: Type 'System.Int32' is not defined.
Enum E
     ~
BC31091: Import of type '[Enum]' from assembly or module 'EnumWithoutMscorReference.dll' failed.
Enum E
     ~
</errors>)
        End Sub

        <WorkItem(541468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541468")>
        <Fact()>
        Public Sub TypeUsedViaAlias01()
            Dim compilation1 = CompilationUtils.CreateEmptyCompilationWithReferences(
<compilation name="TypeUsedViaAlias01">
    <file name="a.vb">
Imports DnT = Microsoft.VisualBasic.DateAndTime
Class c
    Sub S0()
        Dim a = DnT.DateString
    End Sub
End Class
    </file>
</compilation>, {MsvbRef})
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors><![CDATA[
BC30002: Type 'System.Void' is not defined.
Class c
~~~~~~~~
BC30652: Reference required to assembly '<Missing Core Assembly>, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'Object'. Add one to your project.
Class c
      ~
BC30002: Type 'System.Void' is not defined.
    Sub S0()
    ~~~~~~~~~
BC30002: Type 'System.Object' is not defined.
        Dim a = DnT.DateString
            ~
BC30652: Reference required to assembly '<Missing Core Assembly>, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'Object'. Add one to your project.
        Dim a = DnT.DateString
                ~~~
BC30652: Reference required to assembly '<Missing Core Assembly>, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'String'. Add one to your project.
        Dim a = DnT.DateString
                ~~~~~~~~~~~~~~
BC30652: Reference required to assembly 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' containing the type '[Object]'. Add one to your project.
        Dim a = DnT.DateString
                ~~~~~~~~~~~~~~
]]></errors>)
        End Sub

        <WorkItem(541468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541468")>
        <Fact()>
        Public Sub TypeUsedViaAlias02()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="TypeUsedViaAlias02">
    <file name="a.vb">
Imports MyClass1 = Class1
Class c
    Dim _myclass As MyClass1 = Nothing
End Class
    </file>
</compilation>,
    {TestReferences.SymbolsTests.NoPia.NoPIAGenericsAsm1})
            compilation1.AssertTheseDiagnostics(
<errors>
BC36924: Type 'List(Of FooStruct)' cannot be used across assembly boundaries because it has a generic type argument that is an embedded interop type.
    Dim _myclass As MyClass1 = Nothing
                    ~~~~~~~~
</errors>)
        End Sub

        <WorkItem(541468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541468")>
        <Fact()>
        Public Sub TypeUsedViaAlias03()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="TypeUsedViaAlias03">
    <file name="a.vb">
Imports MyClass1 = Class1
Class c
    Sub S0()
        Dim _myclass = MyClass1.Class1Foo
    End Sub
End Class
    </file>
</compilation>,
    {TestReferences.SymbolsTests.NoPia.NoPIAGenericsAsm1})
            compilation1.AssertTheseDiagnostics(
<errors>
BC36924: Type 'List(Of FooStruct)' cannot be used across assembly boundaries because it has a generic type argument that is an embedded interop type.
        Dim _myclass = MyClass1.Class1Foo
                       ~~~~~~~~
BC36924: Type 'List(Of FooStruct)' cannot be used across assembly boundaries because it has a generic type argument that is an embedded interop type.
        Dim _myclass = MyClass1.Class1Foo
                       ~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <WorkItem(541468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541468")>
        <Fact()>
        Public Sub TypeUsedViaAlias04()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation name="TypeUsedViaAlias04">
    <file name="a.vb">
Imports MyClass1 = Class1
Class c
    Sub S0()
        Dim _myclass = directcast(nothing, MyClass1)
    End Sub
End Class
    </file>
</compilation>,
    {TestReferences.SymbolsTests.NoPia.NoPIAGenericsAsm1})
            compilation1.AssertTheseDiagnostics(
<errors>
BC36924: Type 'List(Of FooStruct)' cannot be used across assembly boundaries because it has a generic type argument that is an embedded interop type.
        Dim _myclass = directcast(nothing, MyClass1)
                                           ~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub Bug8522()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ArrayInitNoType">
        <file name="a.vb">
Imports System            

Class A
    Sub New(x As Action)
    End Sub

    Public Const X As Integer = 0
End Class

Class C
    Inherits Attribute
    Sub New(x As Integer)
    End Sub
End Class

Module M
    Friend Const main As Object=Main

    Sub Main
    End Sub
End Module
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30500: Constant 'main' cannot depend on its own value.
    Friend Const main As Object=Main
                 ~~~~
BC30260: 'Main' is already declared as 'Friend Const main As Object' in this module.
    Sub Main
        ~~~~
</expected>)
        End Sub

        <WorkItem(542596, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542596")>
        <Fact()>
        Public Sub RegularArgumentAfterNamed()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="RegularArgumentAfterNamed">
        <file name="a.vb">
Module Module1

    Sub M1(x As Integer, y As Integer)
    End Sub

    Sub M1(x As Integer, y As Long)
    End Sub

    Sub M2(x As Integer, y As Integer)
    End Sub

    Sub Main()
        M1(x:=2, 3) 'BIND:"M1(x:=2, 3)"
        M2(x:=2, 3)
    End Sub

End Module
    </file>
    </compilation>, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30241: Named argument expected. Please use language version 15.5 or greater to use non-trailing named arguments.
        M1(x:=2, 3) 'BIND:"M1(x:=2, 3)"
                 ~
BC30241: Named argument expected. Please use language version 15.5 or greater to use non-trailing named arguments.
        M2(x:=2, 3)
                 ~
</expected>)
        End Sub

        <Fact()>
        Public Sub EventMissingName()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="EventMissingName">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Program
    Event

    Sub Main(args As String())
    End Sub
End Module
    
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
    BC30203: Identifier expected.
    Event
         ~
</errors>)
        End Sub

        <Fact()>
        Public Sub EventClashSynthetic()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="EventClashSynthetic">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Program
    Event E()

    Dim EEvent As Integer

    Sub Main(args As String())
    End Sub
End Module        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
    BC31060: event 'E' implicitly defines 'EEvent', which conflicts with a member of the same name in module 'Program'.
    Event E()
          ~
</errors>)
        End Sub

        <Fact()>
        Public Sub EventClashSyntheticDelegate()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="EventClashSyntheticDelegate">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Program
    Event E()

    Dim EEventHandler As Integer

    Sub Main(args As String())
    End Sub
End Module        </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
    BC31060: event 'E' implicitly defines 'EEventHandler', which conflicts with a member of the same name in module 'Program'.
    Event E()
          ~
</errors>)
        End Sub

        <Fact()>
        Public Sub EventNotADelegate()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="EventNotADelegate">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Program
    Event E as Integer

    Sub Main(args As String())
    End Sub
End Module        
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
    BC31044: Events declared with an 'As' clause must have a delegate type.
    Event E as Integer
               ~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub EventParamsAndAs()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="EventParamsAndAs">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Program
    Event E(x as integer) as Integer

    Sub Main(args As String())
    End Sub
End Module        
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
    BC30032: Events cannot have a return type.
    Event E(x as integer) as Integer
                          ~~
</errors>)
        End Sub

        <Fact()>
        Public Sub EventDelegateReturns()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="EventDelegateReturns">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Program
    Event E as Func(of Integer)

    Sub Main()
    End Sub
End Module      
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
    BC31084: Events cannot be declared with a delegate type that has a return type.
    Event E as Func(of Integer)
               ~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub EventDelegateClash()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="EventDelegateClash">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Program
    Event E()
    Event E(x as integer)

    Sub Main()
    End Sub
End Module      
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
    BC30260: 'E' is already declared as 'Public Event E()' in this module.
    Event E(x as integer)
          ~
</errors>)
        End Sub

        <Fact()>
        Public Sub EventNameLength()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="EventNameLength">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Program

    Event eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee()

    Event eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee()

    Sub Main(args As String())
    End Sub
End Module
    
</file>
    </compilation>)
            CompilationUtils.AssertNoDiagnostics(compilation1)
            CompilationUtils.AssertTheseEmitDiagnostics(compilation1,
<errors>
    BC37220: Name 'eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeEventHandler' exceeds the maximum length allowed in metadata.
    Event eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee()
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub EventTypeChar()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="EventTypeChar">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Program
    Event e$

    Sub Main(args As String())
    End Sub
End Module
    
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
    BC30468: Type declaration characters are not valid in this context.
    Event e$
          ~~
</errors>)
        End Sub

        <Fact()>
        Public Sub EventIllegalImplements()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="EventIllegalImplements">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Interface I0
    Event e
End Interface

Interface I1
    Event e Implements I0.e
End Interface

Class Cls1 
    Implements I0

    Shared Event e Implements I0.e
End Class

Module Program
    Event e Implements I0.e

    Sub Main(args As String())
    End Sub
End Module
    
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
    BC30688: Events in interfaces cannot be declared 'Implements'.
    Event e Implements I0.e
            ~~~~~~~~~~
BC30149: Class 'Cls1' must implement 'Event e()' for interface 'I0'.
    Implements I0
               ~~
BC30505: Methods or events that implement interface members cannot be declared 'Shared'.
    Shared Event e Implements I0.e
    ~~~~~~
BC31083: Members in a Module cannot implement interface members.
    Event e Implements I0.e
            ~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub CustomEventMissingAccessors()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CustomEventMissingAccessors">
        <file name="a.vb">
Option Strict On
Imports System

Module Program
    Delegate Sub del1(x As Integer, x As Integer)

    Custom Event eeeeeee As del1
    End Event

    Sub Main(args As String())
        AddHandler eeeeeee, Nothing
        RemoveHandler eeeeeee, Nothing
        RaiseEvent eeeeeee(1,1)
    End Sub
End Module

    
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC31130: 'AddHandler' definition missing for event 'Public Event eeeeeee As Program.del1'.
    Custom Event eeeeeee As del1
                 ~~~~~~~
BC31131: 'RemoveHandler' definition missing for event 'Public Event eeeeeee As Program.del1'.
    Custom Event eeeeeee As del1
                 ~~~~~~~
BC31132: 'RaiseEvent' definition missing for event 'Public Event eeeeeee As Program.del1'.
    Custom Event eeeeeee As del1
                 ~~~~~~~
BC31132: 'RaiseEvent' definition missing for event 'eeeeeee'.
        RaiseEvent eeeeeee(1,1)
                   ~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub CustomEventDuplicateAccessors()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CustomEventDuplicateAccessors">
        <file name="a.vb">
Option Strict On
Imports System

Module Program
    Delegate Sub del1(x As Integer, x As Integer)

    Custom Event eeeeeee As del1
        AddHandler(value As del1)

        End AddHandler

        RemoveHandler(value As del1)

        End RemoveHandler

        RaiseEvent(x As Integer, x1 As Integer)

        End RaiseEvent

        AddHandler(value As del1)

        End AddHandler

        RemoveHandler(value As del1)

        End RemoveHandler

        RaiseEvent(x As Integer, x1 As Integer)

        End RaiseEvent
    End Event

    Sub Main(args As String())
        AddHandler eeeeeee, Nothing
        RemoveHandler eeeeeee, Nothing
    End Sub
End Module    
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC31127: 'AddHandler' is already declared.
        AddHandler(value As del1)
        ~~~~~~~~~~~~~~~~~~~~~~~~~
BC31128: 'RemoveHandler' is already declared.
        RemoveHandler(value As del1)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31129: 'RaiseEvent' is already declared.
        RaiseEvent(x As Integer, x1 As Integer)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub CustomEventAccessorsWrongParamNum()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CustomEventDuplicateAccessors">
        <file name="a.vb">
Option Strict On
Imports System

Module Program
    Delegate Sub del1(x As Integer, x As Integer)

    Custom Event eeeeeee As del1
        AddHandler()
        End AddHandler

        RemoveHandler(value As del1, x As Integer)
        End RemoveHandler

        RaiseEvent(x As Integer, x1 As Integer)
        End RaiseEvent
    End Event

    Sub Main(args As String())
        AddHandler eeeeeee, Nothing
        RemoveHandler eeeeeee, Nothing
    End Sub
End Module  
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC31133: 'AddHandler' and 'RemoveHandler' methods must have exactly one parameter.
        AddHandler()
        ~~~~~~~~~~~~
BC31133: 'AddHandler' and 'RemoveHandler' methods must have exactly one parameter.
        RemoveHandler(value As del1, x As Integer)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub CustomEventAccessorsWrongParamTypes()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CustomEventDuplicateAccessors">
        <file name="a.vb">
Option Strict Off
Imports System

Module Program
    Delegate Sub del1(x As Integer)

    Custom Event eeeeeee As del1
        AddHandler(value As Action(Of Integer))
        End AddHandler

        RemoveHandler(value)
        End RemoveHandler

        RaiseEvent(x)
            ' not an error (strict off)
        End RaiseEvent
    End Event

    Sub Main(args As String())
        AddHandler eeeeeee, Nothing
        RemoveHandler eeeeeee, Nothing
    End Sub
End Module 
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC31136: 'AddHandler' and 'RemoveHandler' method parameters must have the same delegate type as the containing event.
        AddHandler(value As Action(Of Integer))
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31136: 'AddHandler' and 'RemoveHandler' method parameters must have the same delegate type as the containing event.
        RemoveHandler(value)
        ~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub CustomEventAccessorsWrongParamTypes1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CustomEventDuplicateAccessors">
        <file name="a.vb">
Option Strict Off
Imports System

Module Program
    Delegate Sub del1(x As Integer)

    Custom Event eeeeeee As Object
        AddHandler(value As Object)
        End AddHandler

        RemoveHandler(value)
        End RemoveHandler

        RaiseEvent(x)
            ' not an error (strict off)
        End RaiseEvent
    End Event

    Sub Main(args As String())
        AddHandler eeeeeee, Nothing
        RemoveHandler eeeeeee, Nothing
    End Sub
End Module 
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
    BC31044: Events declared with an 'As' clause must have a delegate type.
    Custom Event eeeeeee As Object
                            ~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub CustomEventAccessorsArgModifiers()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CustomEventAccessorsArgModifiers">
        <file name="a.vb">
Option Strict Off
Imports System

Module Program
    Delegate Sub del1(ByRef x As Integer)

    Custom Event eeeeeee As del1
        AddHandler(ParamArray value() As del1)

        End AddHandler

        RemoveHandler(Optional ByRef value As del1 = Nothing)

        End RemoveHandler

        RaiseEvent(Optional ByRef x As Integer = 1)

        End RaiseEvent
    End Event

    Sub Main(args As String())
        AddHandler eeeeeee, Nothing
        RemoveHandler eeeeeee, Nothing
    End Sub
End Module
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC31136: 'AddHandler' and 'RemoveHandler' method parameters must have the same delegate type as the containing event.
        AddHandler(ParamArray value() As del1)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31138: 'AddHandler', 'RemoveHandler' and 'RaiseEvent' method parameters cannot be declared 'ParamArray'.
        AddHandler(ParamArray value() As del1)
                   ~~~~~~~~~~
BC31138: 'AddHandler', 'RemoveHandler' and 'RaiseEvent' method parameters cannot be declared 'Optional'.
        RemoveHandler(Optional ByRef value As del1 = Nothing)
                      ~~~~~~~~
BC31134: 'AddHandler' and 'RemoveHandler' method parameters cannot be declared 'ByRef'.
        RemoveHandler(Optional ByRef value As del1 = Nothing)
                               ~~~~~
BC31138: 'AddHandler', 'RemoveHandler' and 'RaiseEvent' method parameters cannot be declared 'Optional'.
        RaiseEvent(Optional ByRef x As Integer = 1)
                   ~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub CustomEventRaiseRelaxed1()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CustomEventRaiseRelaxed1">
        <file name="a.vb">
Option Strict On
Imports System

Module Program
    Delegate Sub del1(x As Integer)

    Custom Event eeeeeee1 As del1
        AddHandler(value As del1)

        End AddHandler

        RemoveHandler(value As del1)

        End RemoveHandler

        RaiseEvent(x As Short)
            ' error
        End RaiseEvent
    End Event

    Custom Event eeeeeee2 As del1
        AddHandler(value As del1)

        End AddHandler

        RemoveHandler(value As del1)

        End RemoveHandler

        RaiseEvent(x As Integer, y As Integer)
            ' error
        End RaiseEvent
    End Event

    Custom Event eeeeeee3 As del1
        AddHandler(value As del1)

        End AddHandler

        RemoveHandler(value As del1)

        End RemoveHandler

        RaiseEvent(x As Long)
            'valid
        End RaiseEvent
    End Event

    Custom Event eeeeeee4 As del1
        AddHandler(value As del1)

        End AddHandler

        RemoveHandler(value As del1)

        End RemoveHandler

        RaiseEvent()
            ' valid
        End RaiseEvent
    End Event


    Sub Main(args As String())
        AddHandler eeeeeee1, Nothing
        RemoveHandler eeeeeee1, Nothing
    End Sub
End Module
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC31137: 'RaiseEvent' method must have the same signature as the containing event's delegate type 'Program.del1'.
        RaiseEvent(x As Short)
        ~~~~~~~~~~~~~~~~~~~~~~
BC31137: 'RaiseEvent' method must have the same signature as the containing event's delegate type 'Program.del1'.
        RaiseEvent(x As Integer, y As Integer)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub CustomEventRaiseRelaxed2()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="CustomEventRaiseRelaxed3">
        <file name="a.vb">
Option Strict Off
Imports System

Module Program
    Delegate Sub del1(x As Integer)

    Custom Event eeeeeee1 As del1
        AddHandler(value As del1)

        End AddHandler

        RemoveHandler(value As del1)

        End RemoveHandler

        RaiseEvent(x As Short)
            ' valid
        End RaiseEvent
    End Event

    Custom Event eeeeeee2 As del1
        AddHandler(value As del1)

        End AddHandler

        RemoveHandler(value As del1)

        End RemoveHandler

        RaiseEvent(x As Integer, y As Integer)
            ' error
        End RaiseEvent
    End Event

    Custom Event eeeeeee3 As del1
        AddHandler(value As del1)

        End AddHandler

        RemoveHandler(value As del1)

        End RemoveHandler

        RaiseEvent(x As Long)
            'valid
        End RaiseEvent
    End Event

    Custom Event eeeeeee4 As del1
        AddHandler(value As del1)

        End AddHandler

        RemoveHandler(value As del1)

        End RemoveHandler

        RaiseEvent()
            ' valid
        End RaiseEvent
    End Event


    Sub Main(args As String())
        AddHandler eeeeeee1, Nothing
        RemoveHandler eeeeeee1, Nothing
    End Sub
End Module
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC31137: 'RaiseEvent' method must have the same signature as the containing event's delegate type 'Program.del1'.
        RaiseEvent(x As Integer, y As Integer)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub RaiseEventNotEvent()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="RaiseEventNotEvent">
        <file name="a.vb">
Imports System

Module Program
    Delegate Sub del1()

    Dim o As del1

    Sub Main(args As String())

        RaiseEvent o()

        RaiseEvent Blah

        RaiseEvent

        RaiseEvent         RaiseEvent

    End Sub
End Module
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC30676: 'o' is not an event of 'Program'.
        RaiseEvent o()
                   ~
BC30451: 'Blah' is not declared. It may be inaccessible due to its protection level.
        RaiseEvent Blah
                   ~~~~
BC30203: Identifier expected.
        RaiseEvent
                  ~
BC30451: 'RaiseEvent' is not declared. It may be inaccessible due to its protection level.
        RaiseEvent         RaiseEvent
                           ~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub RaiseEventInBase()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="RaiseEventInBase">
        <file name="a.vb">
Imports System

Module Program
    Delegate Sub del1()

    Dim o As del1

    Sub Main(args As String())
    End Sub

    Class cls1

        Public Shared Event EFieldLike As Action

        Public Shared Custom Event ECustom As Action
            AddHandler(value As Action)
            End AddHandler
            RemoveHandler(value As Action)
            End RemoveHandler
            RaiseEvent()
            End RaiseEvent
        End Event

        Class cls2
            Inherits cls1

            Sub Test()
                ' valid as in Dev10
                RaiseEvent EFieldLike()

                ' error
                RaiseEvent E1()
            End Sub
        End Class
    End Class

End Module
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC30451: 'E1' is not declared. It may be inaccessible due to its protection level.
                RaiseEvent E1()
                           ~~
</errors>)
        End Sub

        <Fact()>
        Public Sub RaiseEventStrictOn()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="RaiseEventStrictOn">
        <file name="a.vb">
Option Strict On
Imports System

Module Program
    Delegate Sub del1(ByRef x As Object)

    Event E As del1

    Sub Main()
        Dim v As String

        RaiseEvent E(v)
    End Sub

End Module
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC32029: Option Strict On disallows narrowing from type 'Object' to type 'String' in copying the value of 'ByRef' parameter 'x' back to the matching argument.
        RaiseEvent E(v)
                     ~
BC42104: Variable 'v' is used before it has been assigned a value. A null reference exception could result at runtime.
        RaiseEvent E(v)
                     ~
</errors>)
        End Sub

        <Fact()>
        Public Sub RaiseEventStrictOff()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="RaiseEventStrictOff">
        <file name="a.vb">
Option Strict Off
Imports System

Module Program
    Delegate Sub del1(ByRef x As Object)

    Event E As del1

    Sub Main()
        Dim v As String

        RaiseEvent E(v)
    End Sub

End Module
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC42030: Variable 'v' is passed by reference before it has been assigned a value. A null reference exception could result at runtime.
        RaiseEvent E(v)
                     ~
</errors>)
        End Sub

        <Fact()>
        Public Sub ImplementNotValidDelegate()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ImplementDifferentDelegate">
        <file name="a.vb">
Imports System

Module Program

    Interface i1
        Delegate Sub del1()
        Delegate Sub del2(x As Integer)
        Delegate Sub del3(x() As Integer)

        Event E1 As del1
        Event E2 As del2
        Event E3 As del3
    End Interface

    Class cls1
        Implements i1

        Private Event E1 As Integer Implements i1.E1
        Private Event E2 As Func(Of Integer) Implements i1.E2
        Private Event E3 As Implements i1.E3
        Private Event E4(ParamArray x() As Integer) Implements i1.E3
    End Class

    Sub Main()
    End Sub
End Module
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC30149: Class 'cls1' must implement 'Event E2 As Program.i1.del2' for interface 'i1'.
        Implements i1
                   ~~
BC31044: Events declared with an 'As' clause must have a delegate type.
        Private Event E1 As Integer Implements i1.E1
                            ~~~~~~~
BC31423: Event 'Private Event E1 As Integer' cannot implement event 'Event E1 As Program.i1.del1' on interface 'Program.i1' because their delegate types 'Integer' and 'Program.i1.del1' do not match.
        Private Event E1 As Integer Implements i1.E1
                                               ~~~~~
BC31084: Events cannot be declared with a delegate type that has a return type.
        Private Event E2 As Func(Of Integer) Implements i1.E2
                            ~~~~~~~~~~~~~~~~
BC30401: 'E2' cannot implement 'E2' because there is no matching event on interface 'i1'.
        Private Event E2 As Func(Of Integer) Implements i1.E2
                                                        ~~~~~
BC30180: Keyword does not name a type.
        Private Event E3 As Implements i1.E3
                            ~
BC31044: Events declared with an 'As' clause must have a delegate type.
        Private Event E3 As Implements i1.E3
                            ~
BC33009: 'Event' parameters cannot be declared 'ParamArray'.
        Private Event E4(ParamArray x() As Integer) Implements i1.E3
                         ~~~~~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub ImplementDifferentDelegate()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ImplementDifferentDelegate">
        <file name="a.vb">
Imports System

Module Program

    Interface i1
        Delegate Sub del1()
        Delegate Sub del2(x As Integer)
        Delegate Sub del3(ByRef x As Integer)

        Event E1 As del1
        Event E2 As del2
        Event E3 As del3
    End Interface

    Class cls1
        Implements i1

        Private Event E1 As action Implements i1.E1
        Private Event E2 As action Implements i1.E2
        Private Event E3 As i1.del2 Implements i1.E3
    End Class

    Sub Main()
    End Sub
End Module
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC30149: Class 'cls1' must implement 'Event E2 As Program.i1.del2' for interface 'i1'.
        Implements i1
                   ~~
BC30149: Class 'cls1' must implement 'Event E3 As Program.i1.del3' for interface 'i1'.
        Implements i1
                   ~~
BC31423: Event 'Private Event E1 As Action' cannot implement event 'Event E1 As Program.i1.del1' on interface 'Program.i1' because their delegate types 'Action' and 'Program.i1.del1' do not match.
        Private Event E1 As action Implements i1.E1
                                              ~~~~~
BC30401: 'E2' cannot implement 'E2' because there is no matching event on interface 'i1'.
        Private Event E2 As action Implements i1.E2
                                              ~~~~~
BC30401: 'E3' cannot implement 'E3' because there is no matching event on interface 'i1'.
        Private Event E3 As i1.del2 Implements i1.E3
                                               ~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub ImplementDifferentSignature()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ImplementDifferentDelegate">
        <file name="a.vb">
Imports System

Module Program

    Interface i1
        Delegate Sub del1()
        Delegate Sub del2(x As Integer)
        Delegate Sub del3(ByRef x As Integer)

        Event E1 As del1
        Event E2 As del2
        Event E3 As del3
    End Interface

    Class cls1
        Implements i1

        Private Event E1(x As Integer) Implements i1.E1
        Private Event E2() Implements i1.E2
        Private Event E3(x As Integer) Implements i1.E3
    End Class

    Sub Main()
    End Sub
End Module
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC30149: Class 'cls1' must implement 'Event E1 As Program.i1.del1' for interface 'i1'.
        Implements i1
                   ~~
BC30149: Class 'cls1' must implement 'Event E2 As Program.i1.del2' for interface 'i1'.
        Implements i1
                   ~~
BC30149: Class 'cls1' must implement 'Event E3 As Program.i1.del3' for interface 'i1'.
        Implements i1
                   ~~
BC30401: 'E1' cannot implement 'E1' because there is no matching event on interface 'i1'.
        Private Event E1(x As Integer) Implements i1.E1
                                                  ~~~~~
BC30401: 'E2' cannot implement 'E2' because there is no matching event on interface 'i1'.
        Private Event E2() Implements i1.E2
                                      ~~~~~
BC30401: 'E3' cannot implement 'E3' because there is no matching event on interface 'i1'.
        Private Event E3(x As Integer) Implements i1.E3
                                                  ~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub ImplementConflicting()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ImplementConflicting">
        <file name="a.vb">
Imports System

Module Program

    Interface i1
        Delegate Sub del1()
        Delegate Sub del2(x As Integer)

        Event E1 As del1
        Event E2 As del2

        Event E1a As del2
        Event E2a As Action(Of Integer)
    End Interface

    Class cls1
        Implements i1

        Private Event E1 As i1.del1 Implements i1.E1, i1.E2

        Private Event E2(x As Integer) Implements i1.E1a, i1.E2a
    End Class

    Sub Main()
    End Sub
End Module
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC30149: Class 'cls1' must implement 'Event E2 As Program.i1.del2' for interface 'i1'.
        Implements i1
                   ~~
BC30401: 'E1' cannot implement 'E2' because there is no matching event on interface 'i1'.
        Private Event E1 As i1.del1 Implements i1.E1, i1.E2
                                                      ~~~~~
BC31407: Event 'Private Event E2 As Program.i1.del2' cannot implement event 'Program.i1.Event E2a As Action(Of Integer)' because its delegate type does not match the delegate type of another event implemented by 'Private Event E2 As Program.i1.del2'.
        Private Event E2(x As Integer) Implements i1.E1a, i1.E2a
                                                          ~~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub ImplementTwice()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ImplementTwice">
        <file name="a.vb">
Imports System

Module Program

    Interface i1
        Delegate Sub del1()
        Delegate Sub del2(x As Integer)

        Event E1 As del1
        Event E2 As del2
    End Interface

    Class cls1
        Implements i1

        Private Event E1 As i1.del1 Implements i1.E1, i1.E1
        Private Event E2() Implements i1.E1
    End Class

    Sub Main()
    End Sub
End Module
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC30149: Class 'cls1' must implement 'Event E2 As Program.i1.del2' for interface 'i1'.
        Implements i1
                   ~~
BC30583: 'i1.E1' cannot be implemented more than once.
        Private Event E1 As i1.del1 Implements i1.E1, i1.E1
                                               ~~~~~
BC30583: 'i1.E1' cannot be implemented more than once.
        Private Event E2() Implements i1.E1
                                      ~~~~~
</errors>)
        End Sub

        <Fact()>
        Public Sub ImplementIncomplete()
            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ImplementConflicting">
        <file name="a.vb">
Imports System

Module Program

    Interface i1
        Delegate Sub del1()
        Delegate Sub del2(x As Integer)

        Event E1 As del1
        Event E2 As del2
    End Interface

    Class cls1
        Implements i1

        Private Event E1 As i1.del1 Implements
        Private Event E2() Implements i1
        Private Event E3 Implements i1.

        Private Event E4 Implements i1.Blah
    End Class

    Sub Main()
    End Sub
End Module
</file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation1,
<errors>
BC30149: Class 'cls1' must implement 'Event E1 As Program.i1.del1' for interface 'i1'.
        Implements i1
                   ~~
BC30149: Class 'cls1' must implement 'Event E2 As Program.i1.del2' for interface 'i1'.
        Implements i1
                   ~~
BC30203: Identifier expected.
        Private Event E1 As i1.del1 Implements
                                              ~
BC30287: '.' expected.
        Private Event E1 As i1.del1 Implements
                                              ~
BC30287: '.' expected.
        Private Event E2() Implements i1
                                      ~~
BC30401: 'E2' cannot implement '' because there is no matching event on interface 'i1'.
        Private Event E2() Implements i1
                                      ~~
BC30401: 'E3' cannot implement '' because there is no matching event on interface 'i1'.
        Private Event E3 Implements i1.
                                    ~~~
BC30203: Identifier expected.
        Private Event E3 Implements i1.
                                       ~
BC30401: 'E4' cannot implement 'Blah' because there is no matching event on interface 'i1'.
        Private Event E4 Implements i1.Blah
                                    ~~~~~~~
</errors>)
        End Sub

        <WorkItem(543095, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543095")>
        <Fact()>
        Public Sub SelectCase_CaseStatementError()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="SelectCase_CaseStatementError">
        <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim x As New myclass1
        Select x
Ca
End Sub
End Module
Structure myclass1
    Implements IDisposable
    Public Sub dispose() Implements IDisposable.Dispose
    End Sub
End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30095: 'Select Case' must end with a matching 'End Select'.
        Select x
        ~~~~~~~~
BC30058: Statements and labels are not valid between 'Select Case' and first 'Case'.
Ca
~~
BC30451: 'Ca' is not declared. It may be inaccessible due to its protection level.
Ca
~~
</expected>)
        End Sub

        <WorkItem(543095, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543095")>
        <Fact()>
        Public Sub SelectCase_CaseStatementError_02()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="SelectCase_CaseStatementError">
        <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim x As New myclass1
        Select x
            Case
        End Select

        Select x
            Case y
        End Select
    End Sub
End Module
Structure myclass1
    Implements IDisposable
    Public Sub dispose() Implements IDisposable.Dispose
    End Sub
End Structure
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30201: Expression expected.
            Case
                ~
BC30451: 'y' is not declared. It may be inaccessible due to its protection level.
            Case y
                 ~
</expected>)
        End Sub

        <WorkItem(543095, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543095")>
        <Fact()>
        Public Sub SelectCase()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="SelectCase">
        <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim x As New myclass1
        Select x
        ca
        End Select
    End Sub
End Module
Structure myclass1
    Implements IDisposable
    Public Sub dispose() Implements IDisposable.Dispose
    End Sub
End Structure
    </file>
    </compilation>)
            VerifyDiagnostics(compilation, Diagnostic(ERRID.ERR_ExpectedCase, "ca"),
            Diagnostic(ERRID.ERR_NameNotDeclared1, "ca").WithArguments("ca"))
        End Sub

        <WorkItem(543300, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543300")>
        <Fact()>
        Public Sub BoundConversion()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="BoundConversion">
        <file name="a.vb">
Imports System.Linq

Class C1
    sharSub MAIN()
    Dim lists = foo()
        lists.Where(Function(ByVal item)

                        SyncLock item

                        End SyncLock
                        Return item.ToString() = ""
                    End Function).ToList()
    End Sub
    Shared Function foo() As List(Of myattribute1)
        Return Nothing
    End Function
End Class

    </file>
    </compilation>)
            VerifyDiagnostics(compilation, Diagnostic(ERRID.ERR_ExpectedSpecifier, "MAIN()"),
                                           Diagnostic(ERRID.ERR_ExpectedDeclaration, "sharSub"),
                                           Diagnostic(ERRID.ERR_ExpectedDeclaration, "lists"),
                                           Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "SyncLock item"),
                                           Diagnostic(ERRID.ERR_EndSyncLockNoSyncLock, "End SyncLock"),
                                           Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Return item.ToString() = """""),
                                           Diagnostic(ERRID.ERR_InvalidEndFunction, "End Function"),
                                           Diagnostic(ERRID.ERR_ExpectedEOS, ")"),
                                           Diagnostic(ERRID.ERR_InvalidEndSub, "End Sub"),
                                           Diagnostic(ERRID.WRN_UndefinedOrEmptyNamespaceOrClass1, "System.Linq").WithArguments("System.Linq"),
                                           Diagnostic(ERRID.ERR_UndefinedType1, "myattribute1").WithArguments("myattribute1"),
                                           Diagnostic(ERRID.ERR_UndefinedType1, "List(Of myattribute1)").WithArguments("List"),
                                           Diagnostic(ERRID.HDN_UnusedImportStatement, "Imports System.Linq"))
        End Sub

        <WorkItem(543300, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543300")>
        <Fact()>
        Public Sub BoundConversion_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="BoundConversion">
        <file name="a.vb">
"".Where(Function(item)
                Return lab1
            End Function:
Return item.ToString() = ""
End Function).ToList()    
End Sub 
    </file>
    </compilation>, {Net40.References.SystemCore})
            VerifyDiagnostics(compilation, Diagnostic(ERRID.ERR_Syntax, """"""),
                Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Return lab1"),
                Diagnostic(ERRID.ERR_InvalidEndFunction, "End Function"),
                Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Return item.ToString() = """""),
                Diagnostic(ERRID.ERR_InvalidEndFunction, "End Function"),
                Diagnostic(ERRID.ERR_ExpectedEOS, ")"),
                Diagnostic(ERRID.ERR_InvalidEndSub, "End Sub"))
        End Sub

        <WorkItem(543319, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543319")>
        <Fact()>
        Public Sub CaseOnlyAppearInSelect()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Module M1
    Sub Main()
        Select ""
            Case "a"
                If (True)
                    Case "b"
                    GoTo lab1
                End If
        End Select
lab1:
    End Sub
End Module
    ]]></file>
</compilation>)

            VerifyDiagnostics(compilation, Diagnostic(ERRID.ERR_CaseNoSelect, "Case ""b"""))
        End Sub

        <WorkItem(543319, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543319")>
        <Fact()>
        Public Sub CaseOnlyAppearInSelect_1()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Class Test
    Public Shared Sub Main()
        Select Case ""
            Case "c"
                Try
                    Case "a"
                Catch ex As Exception
                    Case "b"
                End Try
        End Select
    End Sub
End Class
    ]]></file>
</compilation>)

            VerifyDiagnostics(compilation, Diagnostic(ERRID.ERR_CaseNoSelect, "Case ""a"""),
                Diagnostic(ERRID.ERR_CaseNoSelect, "Case ""b"""))
        End Sub

        <WorkItem(543333, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543333")>
        <Fact()>
        Public Sub BindReturn()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class Program
    Shared  Main()
        Return Nothing
    End sub
End Class
    ]]></file>
</compilation>)

            VerifyDiagnostics(compilation, Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Return Nothing"),
                              Diagnostic(ERRID.ERR_InvalidEndSub, "End sub"))
        End Sub

        <WorkItem(543746, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543746")>
        <Fact()>
        Public Sub LocalConstAssignedToSelf()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        Const X = X
    End Sub
End Module
    ]]></file>
</compilation>)

            VerifyDiagnostics(compilation,
                    Diagnostic(ERRID.ERR_CircularEvaluation1, "X").WithArguments("X"),
                    Diagnostic(ERRID.WRN_DefAsgUseNullRef, "X").WithArguments("X"))
        End Sub

        <WorkItem(543746, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543746")>
        <Fact>
        Public Sub LocalConstCycle()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        Const Y = Z
        Const Z = Y

        Const a = c
        Const b = a
        Const c = b
    End Sub
End Module
    ]]></file>
</compilation>)

            VerifyDiagnostics(compilation,
                    Diagnostic(ERRID.ERR_UseOfLocalBeforeDeclaration1, "Z").WithArguments("Z"),
                    Diagnostic(ERRID.ERR_UseOfLocalBeforeDeclaration1, "c").WithArguments("c"))
        End Sub

        <WorkItem(543823, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543823")>
        <Fact()>
        Public Sub LocalConstCycle02()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        Const X = 3 + Z
        Const Y = 2 + X
        Const Z = 1 + Y
    End Sub
End Module
    ]]></file>
</compilation>)

            ' Dev10: Diagnostic(ERRID.ERR_NameNotDeclared1, "Z").WithArguments("Z"))
            VerifyDiagnostics(compilation,
                              Diagnostic(ERRID.ERR_UseOfLocalBeforeDeclaration1, "Z").WithArguments("Z"),
                              Diagnostic(ERRID.ERR_RequiredConstConversion2, "2").WithArguments("Integer", "Object"),
                              Diagnostic(ERRID.ERR_RequiredConstConversion2, "1").WithArguments("Integer", "Object"))

        End Sub

        <WorkItem(543821, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543821")>
        <Fact()>
        Public Sub LocalConstCycle03()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        Const X = Y
        Const Y = Y + X
        Const Z = Y + Z
     End Sub
End Module
    ]]></file>
</compilation>)

            VerifyDiagnostics(compilation,
                              Diagnostic(ERRID.ERR_UseOfLocalBeforeDeclaration1, "Y").WithArguments("Y"),
                              Diagnostic(ERRID.ERR_CircularEvaluation1, "Y").WithArguments("Y"),
                              Diagnostic(ERRID.ERR_CircularEvaluation1, "Z").WithArguments("Z"),
                              Diagnostic(ERRID.WRN_DefAsgUseNullRef, "Y").WithArguments("Y"),
                              Diagnostic(ERRID.WRN_DefAsgUseNullRef, "Z").WithArguments("Z"))
        End Sub

        <WorkItem(543755, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543755")>
        <Fact()>
        Public Sub BracketedIdentifierMissingEndBracket()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main()
        Dim [foo as integer = 23 : Dim [goo As Char = "d"c
    End Sub
End Module
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30203: Identifier expected.
        Dim [foo as integer = 23 : Dim [goo As Char = "d"c
            ~
BC30034: Bracketed identifier is missing closing ']'.
        Dim [foo as integer = 23 : Dim [goo As Char = "d"c
            ~~~~
BC30203: Identifier expected.
        Dim [foo as integer = 23 : Dim [goo As Char = "d"c
                                       ~
BC30034: Bracketed identifier is missing closing ']'.
        Dim [foo as integer = 23 : Dim [goo As Char = "d"c
                                       ~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(536245, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536245"), WorkItem(543652, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543652")>
        Public Sub BC30192ERR_ParamArrayMustBeLast()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb"><![CDATA[
                        Class C1
                          Sub foo(byval Paramarray pArr1() as Integer, byval paramarray pArr2 as integer)
                          End Sub
                        End Class
                ]]>
                    </file>
                </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30192: End of parameter list expected. Cannot define parameters after a paramarray parameter.
                          Sub foo(byval Paramarray pArr1() as Integer, byval paramarray pArr2 as integer)
                                                                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30050: ParamArray parameter must be an array.
                          Sub foo(byval Paramarray pArr1() as Integer, byval paramarray pArr2 as integer)
                                                                                        ~~~~~    
</expected>)
        End Sub

        <WorkItem(544501, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544501")>
        <Fact()>
        Public Sub TestCombinePrivateAndNotOverridable()
            Dim compilation =
                CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        c1.Bar()
    End Sub
End Module
 
Class c1
    Partial Private Sub foo(ByRef x() As Integer)
    End Sub
End Class
                    </file>
                    <file name="b.vb">
Imports System
Partial Class c1
    Private NotOverridable Sub Foo(ByRef x() As Integer)
        Console.Write("Success")
    End Sub
 
    Shared Sub Bar()
        Dim x = New c1()
        x.foo({1})
    End Sub
End Class
                    </file>
                </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31408: 'Private' and 'NotOverridable' cannot be combined.
    Private NotOverridable Sub Foo(ByRef x() As Integer)
            ~~~~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(546098, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546098")>
        <Fact()>
        Public Sub InstanceMemberOfStructInsideSharedLambda()
            Dim compilation =
                CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb">
Imports System
Module Test
    Public Structure S1
        Dim a As Integer
        Public Shared c As Action(Of Integer) = Sub(c)
                                                    a = 2
                                                End Sub
    End Structure
End Module

                    </file>
                </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
                                                    a = 2
                                                    ~
</expected>)
        End Sub

        <WorkItem(546053, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546053")>
        <Fact()>
        Public Sub EventHandlerBindingError()
            Dim compilation =
                CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb">
Module Test
    Sub lbHnd()
    End Sub
    Dim oc As Object
    Sub Main()
        AddHandler oc.evt, AddressOf lbHnd
    End Sub
End Module
                    </file>
                </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30676: 'evt' is not an event of 'Object'.
        AddHandler oc.evt, AddressOf lbHnd
                      ~~~
</expected>)
        End Sub

        <WorkItem(530912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530912")>
        <Fact()>
        Public Sub Bug_17183_LateBinding_Object()
            Dim compilation =
                CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Strict On

Class classes
    Public Property P As Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
End Class

Module Module1
    Sub Main()
        Dim h As classes = Nothing
        Dim x = h.P(0)
    End Sub
End Module
                ]]>
                    </file>
                </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30574: Option Strict On disallows late binding.
        Dim x = h.P(0)
                  ~
</expected>)
        End Sub

        <WorkItem(530912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530912")>
        <Fact()>
        Public Sub Bug_17183_LateBinding_Array()
            Dim compilation =
                CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Class classes
    Public Property P As Array
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
End Class

Module Module1
    Sub Main()
        Dim h As classes = Nothing
        Dim x = h.P(0)
    End Sub
End Module
                ]]>
                    </file>
                </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30574: Option Strict On disallows late binding.
        Dim x = h.P(0)
                  ~
</expected>)
        End Sub

        <WorkItem(530912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530912")>
        <Fact()>
        Public Sub Bug_17183_LateBinding_COM()
            Dim compilation =
                CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Strict On
 
Imports System.Runtime.InteropServices
 
<TypeLibTypeAttribute(TypeLibTypeFlags.FDispatchable)>
Interface inter
    Default Property sss(x As Integer) As Integer
End Interface
 
Class classes
    Public Property P As inter
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
End Class
 
Module Module1
    Sub Main()
        Dim h As classes = Nothing
        Dim x = h.P(0) 'No Late Binding here
        Dim y = h.P.Member 'Late Binding here - error
    End Sub
End Module

                ]]>
                    </file>
                </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30574: Option Strict On disallows late binding.
        Dim y = h.P.Member 'Late Binding here - error
                ~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(531400, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531400")>
        <Fact()>
        Public Sub Bug18070()
            Dim compilation =
                CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb">
Module Program
    Event E()
    Sub Main()
    End Sub
    Sub Main1()
        RaiseEvent E() ' 1
        Static E As Integer = 1
        AddHandler E, AddressOf Main ' 1
        RemoveHandler E, AddressOf Main ' 1
    End Sub
    Sub Main2()
        RaiseEvent E() ' 2
        Dim E As Integer = 1
        AddHandler E, AddressOf Main ' 2
        RemoveHandler E, AddressOf Main ' 2
    End Sub
    Sub Main3(E As Integer)
        RaiseEvent E() ' 3
        AddHandler E, AddressOf Main ' 3
        RemoveHandler E, AddressOf Main ' 3
    End Sub
End Module
                    </file>
                </compilation>)

            CompileAndVerify(compilation)
        End Sub

        <Fact()>
        Public Sub Bug547318()
            Dim compilation =
                CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb">
End Set
    End Property
End Class
Font)
            _FONT = Value
                Set(ByVal Value As System.Drawing.         'BIND:"Drawing"

                    </file>
                </compilation>)

            Dim node As ExpressionSyntax = FindBindingText(Of ExpressionSyntax)(compilation, "a.vb")
            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim bindInfo1 As SemanticInfoSummary = semanticModel.GetSemanticInfoSummary(DirectCast(node, ExpressionSyntax))

        End Sub

        <WorkItem(566606, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566606")>
        <Fact()>
        Public Sub BrokenFor()
            Dim compilation =
                CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb">
                    </file>
                </compilation>)

            Dim text = <![CDATA[
Imports System                        

Module Program
    Sub Main(args As String())
        For {|stmt1:$$i|} = 1 To 20
Dim q As Action = Sub()
                      Console.WriteLine({|stmt1:i|})
End Sub
        Next
    End Sub
End Module
]]>

            compilation = compilation.AddSyntaxTrees(Parse(text))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30035: Syntax error.
        For {|stmt1:$$i|} = 1 To 20
             ~
BC30035: Syntax error.
        For {|stmt1:$$i|} = 1 To 20
             ~
BC30201: Expression expected.
        For {|stmt1:$$i|} = 1 To 20
             ~
BC30249: '=' expected.
        For {|stmt1:$$i|} = 1 To 20
             ~
BC30370: '}' expected.
        For {|stmt1:$$i|} = 1 To 20
             ~
BC30037: Character is not valid.
        For {|stmt1:$$i|} = 1 To 20
             ~
BC30037: Character is not valid.
        For {|stmt1:$$i|} = 1 To 20
                    ~
BC30037: Character is not valid.
        For {|stmt1:$$i|} = 1 To 20
                     ~
BC30037: Character is not valid.
        For {|stmt1:$$i|} = 1 To 20
                       ~
BC30198: ')' expected.
                      Console.WriteLine({|stmt1:i|})
                                         ~
BC30201: Expression expected.
                      Console.WriteLine({|stmt1:i|})
                                         ~
BC30370: '}' expected.
                      Console.WriteLine({|stmt1:i|})
                                         ~
BC30037: Character is not valid.
                      Console.WriteLine({|stmt1:i|})
                                         ~
BC30451: 'i' is not declared. It may be inaccessible due to its protection level.
                      Console.WriteLine({|stmt1:i|})
                                                ~
BC30037: Character is not valid.
                      Console.WriteLine({|stmt1:i|})
                                                 ~
</expected>)

        End Sub

        <Fact>
        Public Sub ConflictsWithTypesInVBCore_EmbeddedTypes()
            Dim source =
<compilation>
    <file><![CDATA[   
Module Module1
    Sub Main()
    End Sub
End Module

'Partial Class on Type in Microsoft.VisualBasic
Namespace Microsoft.VisualBasic
    Partial Class HideModuleNameAttribute
        Inherits Attribute
        Public Function ABC() As String
            Return "Success"
        End Function
        Public Overrides ReadOnly Property TypeId As Object
            Get
                Return "TypeID"
            End Get
        End Property
    End Class
End Namespace
]]>
    </file>
</compilation>

            Dim c = CompilationUtils.CreateEmptyCompilationWithReferences(source,
                                                                     references:={MscorlibRef, SystemRef, SystemCoreRef},
                                                                     options:=TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True)).VerifyDiagnostics(Diagnostic(ERRID.ERR_TypeClashesWithVbCoreType4, "HideModuleNameAttribute").WithArguments("class", "HideModuleNameAttribute", "class", "HideModuleNameAttribute"),
                                                                                                                                         Diagnostic(ERRID.ERR_UndefinedType1, "Attribute").WithArguments("Attribute"),
                                                                                                                                         Diagnostic(ERRID.ERR_OverrideNotNeeded3, "TypeId").WithArguments("property", "TypeId"))

        End Sub

        <Fact()>
        Public Sub ERR_ExtensionMethodCannotBeLateBound_1()
            Dim compilation =
                CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                <compilation>
                    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim x As New Test()
        Dim y As Object = x

        x.M(y)
    End Sub

    <System.Runtime.CompilerServices.Extension>
    Sub M(this As Test, x As Double)
    End Sub
End Module

Class Test
    Sub M(x As Integer)
    End Sub

    Sub M(x As UInteger)
    End Sub
End Class
                    ]]></file>
                </compilation>, {Net40.References.SystemCore}, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            AssertTheseDiagnostics(compilation,
<expected>
BC36908: Late-bound extension methods are not supported.
        x.M(y)
          ~
</expected>)
        End Sub

        <Fact()>
        Public Sub ERR_ExtensionMethodCannotBeLateBound_2()
            Dim compilation =
                CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                <compilation>
                    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim x As New Test()
        Dim y As Object = x

        x.M(y)
    End Sub

    <System.Runtime.CompilerServices.Extension>
    Sub M(this As Test, x As Double, y as String)
    End Sub
End Module

Class Test
    Sub M(x As Integer)
    End Sub

    Sub M(x As UInteger)
    End Sub
End Class
                    ]]></file>
                </compilation>, {Net40.References.SystemCore}, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            AssertTheseDiagnostics(compilation,
<expected>
BC36908: Late-bound extension methods are not supported.
        x.M(y)
          ~
</expected>)
        End Sub

        <Fact()>
        Public Sub ERR_ExtensionMethodCannotBeLateBound_3()
            Dim compilation =
                CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                <compilation>
                    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim x As New Test()
        Dim y As Object = x

        x.M(y)
    End Sub

    <System.Runtime.CompilerServices.Extension>
    Sub M(this As Test, x As Double)
    End Sub
End Module

Class Test
    Sub M(x As UInteger)
    End Sub
End Class
                    ]]></file>
                </compilation>, {Net40.References.SystemCore}, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Object' to 'UInteger'.
        x.M(y)
            ~
</expected>)

            CompileAndVerify(compilation)
        End Sub

        <Fact()>
        Public Sub ERR_ExtensionMethodCannotBeLateBound_4()
            Dim compilation =
                CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                <compilation>
                    <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Dim c1 As New C1()
        Dim x As Object = New List(Of Integer)
        Try
            c1.fun(x)
        Catch e As Exception
            Console.WriteLine(e)
        End Try
    End Sub

    <Extension>
    Sub fun(Of X)(this As C1, ByVal a As Queue(Of X))
    End Sub

End Module

Class C1
    Sub fun(Of X)(ByVal a As List(Of X))
    End Sub

    Sub fun(Of X)(ByVal a As Stack(Of X))
    End Sub
End Class
                    ]]></file>
                </compilation>, {Net40.References.SystemCore}, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            AssertTheseDiagnostics(compilation,
<expected>
BC36908: Late-bound extension methods are not supported.
            c1.fun(x)
               ~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ERR_ExtensionMethodCannotBeLateBound_5()
            Dim compilation =
                CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                <compilation>
                    <file name="a.vb"><![CDATA[
Imports System
Imports System.Linq.Expressions
Imports System.Runtime.CompilerServices
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Dim c1 As New C1()
        Dim x As Object = New List(Of Integer)
        Try
            c1.fun(x)
        Catch e As Exception
            Console.WriteLine(e)
        End Try
    End Sub

    <Extension>
    Sub fun(Of X)(this As C1, ByVal a As Queue(Of X))
    End Sub

End Module

Class C1
    Sub fun(Of X)(ByVal a As List(Of X))
    End Sub
End Class
                    ]]></file>
                </compilation>, {Net40.References.SystemCore}, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            AssertTheseDiagnostics(compilation,
<expected>
BC36908: Late-bound extension methods are not supported.
            c1.fun(x)
               ~~~
</expected>)
        End Sub

        <WorkItem(573728, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/573728")>
        <Fact()>
        Public Sub Bug573728()
            Dim compilation =
                CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb"><![CDATA[
Delegate Sub D(i As Integer)
Module M
    Sub M(o As D)
        o()
        o(1, 2)
    End Sub
End Module
                    ]]></file>
                </compilation>, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(compilation,
<expected>
BC30455: Argument not specified for parameter 'i' of 'D'.
        o()
        ~
BC30057: Too many arguments to 'D'.
        o(1, 2)
             ~
</expected>)
        End Sub

        <Fact(), WorkItem(792754, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/792754")>
        Public Sub NameClashAcrossFiles()
            Dim comp =
                CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb"><![CDATA[
Module M
    Sub Main()
    End Sub

    Private Test2 As Settings

End Module
                    ]]></file>
                    <file name="b.vb"><![CDATA[
Module M ' b.vb
    Private ReadOnly TOOL_OUTPUT_FILE_NAME As String = Settings.OutputFileName

    Function Test() As Settings
        return Nothing
    End Function
End Module
                    ]]></file>
                </compilation>, TestOptions.ReleaseExe)
            Dim allDiagnostics = comp.GetDiagnostics()

            AssertTheseDiagnostics(allDiagnostics,
<expected><![CDATA[
BC30002: Type 'Settings' is not defined.
    Private Test2 As Settings
                     ~~~~~~~~
BC30179: module 'M' and module 'M' conflict in namespace '<Default>'.
Module M ' b.vb
       ~
BC30451: 'Settings' is not declared. It may be inaccessible due to its protection level.
    Private ReadOnly TOOL_OUTPUT_FILE_NAME As String = Settings.OutputFileName
                                                       ~~~~~~~~
BC30002: Type 'Settings' is not defined.
    Function Test() As Settings
                       ~~~~~~~~
]]></expected>)

            Dim tree = comp.SyntaxTrees.Where(Function(t) t.FilePath.EndsWith("a.vb", StringComparison.Ordinal)).Single
            Dim model = comp.GetSemanticModel(tree)

            AssertTheseDiagnostics(model.GetDiagnostics(),
<expected><![CDATA[
BC30002: Type 'Settings' is not defined.
    Private Test2 As Settings
                     ~~~~~~~~
]]></expected>)

            tree = comp.SyntaxTrees.Where(Function(t) t.FilePath.EndsWith("b.vb", StringComparison.Ordinal)).Single
            model = comp.GetSemanticModel(tree)
            AssertTheseDiagnostics(model.GetDiagnostics(),
<expected><![CDATA[
BC30179: module 'M' and module 'M' conflict in namespace '<Default>'.
Module M ' b.vb
       ~
BC30451: 'Settings' is not declared. It may be inaccessible due to its protection level.
    Private ReadOnly TOOL_OUTPUT_FILE_NAME As String = Settings.OutputFileName
                                                       ~~~~~~~~
BC30002: Type 'Settings' is not defined.
    Function Test() As Settings
                       ~~~~~~~~
]]></expected>)

        End Sub

        <Fact>
        Public Sub BC42004WRN_RecursiveOperatorCall()
            Dim comp =
                CreateCompilationWithMscorlib40AndVBRuntime(
                <compilation>
                    <file name="a.vb"><![CDATA[
Friend Module RecAcc001mod
    Class cls
        Sub New()
        End Sub

        Shared Operator +(ByVal y As cls) As Object
            Return +y
        End Operator

        Shared Operator -(ByVal x As cls, ByVal y As Object) As Object
            Return x - y
        End Operator

        Public Shared Operator >>(ByVal x As cls, ByVal y As Integer) As Object
            Return x >> y
        End Operator

        Public Shared Widening Operator CType(ByVal y As cls) As Integer
            Return CType(y, Integer)
        End Operator

    End Class
End Module
                    ]]></file>
                </compilation>)

            Dim allDiagnostics = comp.GetDiagnostics()

            AssertTheseDiagnostics(allDiagnostics,
<expected><![CDATA[
BC42004: Expression recursively calls the containing Operator 'Public Shared Operator +(y As RecAcc001mod.cls) As Object'.
            Return +y
                   ~~
BC42004: Expression recursively calls the containing Operator 'Public Shared Operator -(x As RecAcc001mod.cls, y As Object) As Object'.
            Return x - y
                   ~~~~~
BC42004: Expression recursively calls the containing Operator 'Public Shared Operator >>(x As RecAcc001mod.cls, y As Integer) As Object'.
            Return x >> y
                   ~~~~~~
BC42004: Expression recursively calls the containing Operator 'Public Shared Widening Operator CType(y As RecAcc001mod.cls) As Integer'.
            Return CType(y, Integer)
                         ~
]]></expected>)

        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_ReadonlyAutoProperties()
            Dim source =
    <compilation>
        <file name="a.vb">
Class TestClass

    Public Sub New()
        'Check assignment of readonly auto property
        Test = "Test"
    End Sub

    'Check readonly auto-properties
    Public ReadOnly Property Test As String
End Class

Interface I1
    ReadOnly Property Test1 As String
    WriteOnly Property Test2 As String
    Property Test3 As String
End Interface

MustInherit Class C1
    MustOverride ReadOnly Property Test1 As String
    MustOverride WriteOnly Property Test2 As String
    MustOverride Property Test3 As String
End Class
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 12.0 does not support readonly auto-implemented properties.
    Public ReadOnly Property Test As String
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic9))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 9.0 does not support auto-implemented properties.
    Public ReadOnly Property Test As String
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_RegionEveryWhere01()
            Dim source =
    <compilation>
        <file name="a.vb">
Class TestClass

    Sub New()
#Region "Region in .ctor"
#End Region ' "Region in .ctor"
    End Sub

    Shared Sub New()
#Region "Region in .cctor"
#End Region ' "Region in .cctor"
    End Sub

    Public Sub ASub()
#Region "Region in a Sub"
#End Region ' "Region in a Sub"
    End Sub

    Public Function AFunc()
#Region "Region in a Func"
#End Region ' "Region in a Func"
    End Function

    Shared Operator +(x As TestClass, y As TestClass) As TestClass
#Region "Region in an operator"
#End Region ' "Region in an operator"
    End Operator

    Property P As Integer
        Get
#Region "Region in a get"
#End Region ' "Region in a get"
        End Get
        Set(value As Integer)
#Region "Region in a set"
#End Region ' "Region in a set"
        End Set
    End Property

    Custom Event E As System.Action
        AddHandler(value As Action)
#Region "Region in an add"
#End Region ' "Region in an add"
        End AddHandler
        RemoveHandler(value As Action)
#Region "Region in a remove"
#End Region ' "Region in a remove"
        End RemoveHandler
        RaiseEvent()
#Region "Region in a raise"
#End Region ' "Region in a raise"
        End RaiseEvent
    End Event

End Class
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#Region "Region in .ctor"
~~~~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' "Region in .ctor"
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#Region "Region in .cctor"
~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' "Region in .cctor"
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#Region "Region in a Sub"
~~~~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' "Region in a Sub"
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#Region "Region in a Func"
~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' "Region in a Func"
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#Region "Region in an operator"
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' "Region in an operator"
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#Region "Region in a get"
~~~~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' "Region in a get"
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#Region "Region in a set"
~~~~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' "Region in a set"
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#Region "Region in an add"
~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' "Region in an add"
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#Region "Region in a remove"
~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' "Region in a remove"
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#Region "Region in a raise"
~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' "Region in a raise"
~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_RegionEveryWhere02()
            Dim source =
    <compilation>
        <file name="a.vb">
Class TestClass
#Region "Region"
#End Region 
    Sub New()
    End Sub
#Region "Region"
#End Region 
    Shared Sub New()
    End Sub
#Region "Region"
#End Region 
    Public Sub ASub()
    End Sub
#Region "Region"
#End Region 
    Public Function AFunc()
    End Function
#Region "Region"
#End Region 
    Shared Operator +(x As TestClass, y As TestClass) As TestClass
    End Operator
#Region "Region"
#End Region 
    Property P As Integer
#Region "Region"
#End Region 
        Get
        End Get
#Region "Region"
#End Region 
        Set(value As Integer)
        End Set
#Region "Region"
#End Region 
    End Property
#Region "Region"
#End Region 
    Custom Event E As System.Action
#Region "Region"
#End Region 
        AddHandler(value As Action)
        End AddHandler
#Region "Region"
#End Region 
        RemoveHandler(value As Action)
        End RemoveHandler
#Region "Region"
#End Region 
        RaiseEvent()
        End RaiseEvent
#Region "Region"
#End Region 
    End Event
#Region "Region"
#End Region 
End Class
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_RegionEveryWhere03()
            Dim source =
    <compilation>
        <file name="a.vb">
Class TestClass
#Region "Region"
    Private f1 as Integer
#End Region 
    Sub New()
    End Sub
#Region "Region"
    Private f1 as Integer
#End Region 
    Shared Sub New()
    End Sub
#Region "Region"
    Private f1 as Integer
#End Region 
    Public Sub ASub()
    End Sub
#Region "Region"
    Private f1 as Integer
#End Region 
    Public Function AFunc()
    End Function
#Region "Region"
    Private f1 as Integer
#End Region 
    Shared Operator +(x As TestClass, y As TestClass) As TestClass
    End Operator
#Region "Region"
    Private f1 as Integer
#End Region 
    Property P As Integer
#Region "Region"
#End Region 
        Get
        End Get
#Region "Region"
#End Region 
        Set(value As Integer)
        End Set
#Region "Region"
#End Region 
    End Property
#Region "Region"
    Private f1 as Integer
#End Region 
    Custom Event E As System.Action
#Region "Region"
#End Region 
        AddHandler(value As Action)
        End AddHandler
#Region "Region"
#End Region 
        RemoveHandler(value As Action)
        End RemoveHandler
#Region "Region"
#End Region 
        RaiseEvent()
        End RaiseEvent
#Region "Region"
#End Region 
    End Event
#Region "Region"
    Private f1 as Integer
#End Region 
End Class
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_RegionEveryWhere04()
            Dim source =
    <compilation>
        <file name="a.vb">
Class TestClass
#Region "Region"
    Sub New()
    End Sub
#End Region 
#Region "Region"
    Shared Sub New()
    End Sub
#End Region 
#Region "Region"
    Public Sub ASub()
    End Sub
#End Region 
#Region "Region"
    Public Function AFunc()
    End Function
#End Region 
#Region "Region"
    Shared Operator +(x As TestClass, y As TestClass) As TestClass
    End Operator
#End Region 
#Region "Region"
    Property P As Integer
#Region "Region"
        Get
        End Get
#End Region 
#Region "Region"
        Set(value As Integer)
        End Set
#End Region 
    End Property
#End Region 
#Region "Region"
    Custom Event E As System.Action
#Region "Region"
        AddHandler(value As Action)
        End AddHandler
#End Region 
#Region "Region"
        RemoveHandler(value As Action)
        End RemoveHandler
#End Region 
#Region "Region"
        RaiseEvent()
        End RaiseEvent
#End Region 
    End Event
#End Region 
End Class
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_RegionEveryWhere05()
            Dim source =
    <compilation>
        <file name="a.vb">
Class TestClass
    Property P As Integer
        Get
#Region "Region"
#End Region 
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC30481: 'Class' statement must end with a matching 'End Class'.
Class TestClass
~~~~~~~~~~~~~~~
BC30025: Property missing 'End Property'.
    Property P As Integer
    ~~~~~~~~~~~~~~~~~~~~~
BC30631: 'Get' statement must end with a matching 'End Get'.
        Get
        ~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_RegionEveryWhere06()
            Dim source =
    <compilation>
        <file name="a.vb">
Class TestClass
    Property P As Integer
        Get
        End Get
#Region "Region"
#End Region</file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC30481: 'Class' statement must end with a matching 'End Class'.
Class TestClass
~~~~~~~~~~~~~~~
BC30025: Property missing 'End Property'.
    Property P As Integer
    ~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_RegionEveryWhere07()
            Dim source =
    <compilation>
        <file name="a.vb">
Class TestClass
    Property P As Integer
        Get
#Region "Region"
#End Region 
    End Property
End Class
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC30631: 'Get' statement must end with a matching 'End Get'.
        Get
        ~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_RegionEveryWhere08()
            Dim source =
    <compilation>
        <file name="a.vb">
Class TestClass
    Sub Test()
#if False
#Region "Region"
#End Region 
#End if
    End Sub
End Class
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_RegionEveryWhere09()
            Dim source =
    <compilation>
        <file name="a.vb">
#Region "Region 1"
#End Region ' 1 
        </file>
        <file name="b.vb">
#Region "Region 2"
        </file>
        <file name="c.vb">
#End Region ' 3 
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC30681: '#Region' statement must end with a matching '#End Region'.
#Region "Region 2"
~~~~~~~~~~~~~~~~~~
BC30680: '#End Region' must be preceded by a matching '#Region'.
#End Region ' 3
~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_RegionEveryWhere10()
            Dim source =
    <compilation>
        <file name="a.vb">
Namespace NS1
#Region "Region1"
#End Region ' 1
End Namespace

#Region "Region2"
Namespace NS2
#End Region ' 2
End Namespace

Namespace NS3
#Region "Region3"
End Namespace
#End Region ' 3

Namespace NS4
#Region "Region4"

End Namespace
Namespace NS5

#End Region ' 4

End Namespace

#Region "Region5"
Namespace NS6
End Namespace
#End Region ' 5
        </file>
        <file name="b.vb">
Namespace NS7
#Region "Region6"
End Namespace
        </file>
        <file name="c.vb">
Namespace NS8
#End Region ' 7
End Namespace
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 2
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 3
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 4
~~~~~~~~~~~
BC30681: '#Region' statement must end with a matching '#End Region'.
#Region "Region6"
~~~~~~~~~~~~~~~~~
BC30680: '#End Region' must be preceded by a matching '#Region'.
#End Region ' 7
~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_RegionEveryWhere11()
            Dim source =
    <compilation>
        <file name="a.vb">
Module NS1
#Region "Region1"
#End Region ' 1
End Module

#Region "Region2"
Module NS2
#End Region ' 2
End Module

Module NS3
#Region "Region3"
End Module
#End Region ' 3

Module NS4
#Region "Region4"

End Module
Module NS5

#End Region ' 4

End Module

#Region "Region5"
Module NS6
End Module
#End Region ' 5
        </file>
        <file name="b.vb">
Module NS7
#Region "Region6"
End Module
        </file>
        <file name="c.vb">
Module NS8
#End Region ' 7
End Module
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 2
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 3
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 4
~~~~~~~~~~~
BC30681: '#Region' statement must end with a matching '#End Region'.
#Region "Region6"
~~~~~~~~~~~~~~~~~
BC30680: '#End Region' must be preceded by a matching '#Region'.
#End Region ' 7
~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_RegionEveryWhere12()
            Dim source =
    <compilation>
        <file name="a.vb">
Class NS1
#Region "Region1"
#End Region ' 1
End Class

#Region "Region2"
Class NS2
#End Region ' 2
End Class

Class NS3
#Region "Region3"
End Class
#End Region ' 3

Class NS4
#Region "Region4"

End Class
Class NS5

#End Region ' 4

End Class

#Region "Region5"
Class NS6
End Class
#End Region ' 5
        </file>
        <file name="b.vb">
Class NS7
#Region "Region6"
End Class
        </file>
        <file name="c.vb">
Class NS8
#End Region ' 7
End Class
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 2
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 3
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 4
~~~~~~~~~~~
BC30681: '#Region' statement must end with a matching '#End Region'.
#Region "Region6"
~~~~~~~~~~~~~~~~~
BC30680: '#End Region' must be preceded by a matching '#Region'.
#End Region ' 7
~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_RegionEveryWhere13()
            Dim source =
    <compilation>
        <file name="a.vb">
Structure NS1
#Region "Region1"
#End Region ' 1
End Structure

#Region "Region2"
Structure NS2
#End Region ' 2
End Structure

Structure NS3
#Region "Region3"
End Structure
#End Region ' 3

Structure NS4
#Region "Region4"

End Structure
Structure NS5

#End Region ' 4

End Structure

#Region "Region5"
Structure NS6
End Structure
#End Region ' 5
        </file>
        <file name="b.vb">
Structure NS7
#Region "Region6"
End Structure
        </file>
        <file name="c.vb">
Structure NS8
#End Region ' 7
End Structure
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 2
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 3
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 4
~~~~~~~~~~~
BC30681: '#Region' statement must end with a matching '#End Region'.
#Region "Region6"
~~~~~~~~~~~~~~~~~
BC30680: '#End Region' must be preceded by a matching '#Region'.
#End Region ' 7
~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_RegionEveryWhere14()
            Dim source =
    <compilation>
        <file name="a.vb">
Interface NS1
#Region "Region1"
#End Region ' 1
End Interface

#Region "Region2"
Interface NS2
#End Region ' 2
End Interface

Interface NS3
#Region "Region3"
End Interface
#End Region ' 3

Interface NS4
#Region "Region4"

End Interface
Interface NS5

#End Region ' 4

End Interface

#Region "Region5"
Interface NS6
End Interface
#End Region ' 5
        </file>
        <file name="b.vb">
Interface NS7
#Region "Region6"
End Interface
        </file>
        <file name="c.vb">
Interface NS8
#End Region ' 7
End Interface
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 2
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 3
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 4
~~~~~~~~~~~
BC30681: '#Region' statement must end with a matching '#End Region'.
#Region "Region6"
~~~~~~~~~~~~~~~~~
BC30680: '#End Region' must be preceded by a matching '#Region'.
#End Region ' 7
~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_RegionEveryWhere15()
            Dim source =
    <compilation>
        <file name="a.vb">
Enum NS1
#Region "Region1"
#End Region ' 1
End Enum

#Region "Region2"
Enum NS2
#End Region ' 2
End Enum

Enum NS3
#Region "Region3"
End Enum
#End Region ' 3

Enum NS4
#Region "Region4"

End Enum
Enum NS5

#End Region ' 4

End Enum

#Region "Region5"
Enum NS6
End Enum
#End Region ' 5
        </file>
        <file name="b.vb">
Enum NS7
#Region "Region6"
End Enum
        </file>
        <file name="c.vb">
Enum NS8
#End Region ' 7
End Enum
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 2
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 3
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 4
~~~~~~~~~~~
BC30681: '#Region' statement must end with a matching '#End Region'.
#Region "Region6"
~~~~~~~~~~~~~~~~~
BC30680: '#End Region' must be preceded by a matching '#Region'.
#End Region ' 7
~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_RegionEveryWhere16()
            Dim source =
    <compilation>
        <file name="a.vb">
Class NS1
    Property P1 As Integer
#Region "Region1"
#End Region ' 1
        Get
        End Get
        Set
        End Set
    End Property
End Class

Class NS2
#Region "Region2"
    Property P1 As Integer
#End Region ' 2
        Get
        End Get
        Set
        End Set
    End Property
End Class

Class NS3
    Property P1 As Integer
#Region "Region3"
        Get
        End Get
        Set
        End Set
    End Property
#End Region ' 3
End Class

Class NS4
    Property P1 As Integer
#Region "Region4"
        Get
        End Get
        Set
        End Set
    End Property

    Property P2 As Integer

#End Region ' 4

        Get
        End Get
        Set
        End Set
    End Property
End Class

Class NS6
#Region "Region5"
    Property P1 As Integer
        Get
        End Get
        Set
        End Set
    End Property
#End Region ' 5
End Class
        </file>
        <file name="b.vb">
Class NS7
    Property P1 As Integer
#Region "Region6"
        Get
        End Get
        Set
        End Set
    End Property
End Class
        </file>
        <file name="c.vb">
Class NS8
    Property P1 As Integer
#End Region ' 7
        Get
        End Get
        Set
        End Set
    End Property
End Class
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 2
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 3
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 4
~~~~~~~~~~~
BC30681: '#Region' statement must end with a matching '#End Region'.
#Region "Region6"
~~~~~~~~~~~~~~~~~
BC30680: '#End Region' must be preceded by a matching '#Region'.
#End Region ' 7
~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_RegionEveryWhere17()
            Dim source =
    <compilation>
        <file name="a.vb">
Class NS1
    Custom Event E1 As System.Action
#Region "Region1"
#End Region ' 1
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Class NS2
#Region "Region2"
    Custom Event E1 As System.Action
#End Region ' 2
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Class NS3
    Custom Event E1 As System.Action
#Region "Region3"
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
#End Region ' 3
End Class

Class NS4
    Custom Event E1 As System.Action
#Region "Region4"
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event

    Custom Event E2 As System.Action

#End Region ' 4

        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Class NS6
#Region "Region5"
    Custom Event E1 As System.Action
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
#End Region ' 5
End Class
        </file>
        <file name="b.vb">
Class NS7
    Custom Event E1 As System.Action
#Region "Region6"
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
        </file>
        <file name="c.vb">
Class NS8
    Custom Event E1 As System.Action
#End Region ' 7
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 2
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 3
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 4
~~~~~~~~~~~
BC30681: '#Region' statement must end with a matching '#End Region'.
#Region "Region6"
~~~~~~~~~~~~~~~~~
BC30680: '#End Region' must be preceded by a matching '#Region'.
#End Region ' 7
~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_RegionEveryWhere18()
            Dim source =
    <compilation>
        <file name="a.vb">
#Region "Region1"
Class NS1
#Region "Region2"
        Sub Test1()
#End Region ' 2
        End Sub
End Class
#End Region ' 1
        </file>
        <file name="b.vb">
#Region "Region3"
Class NS2
        Sub Test1()
#Region "Region4"
        End Sub
#End Region ' 4
End Class
#End Region ' 3
        </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#End Region ' 2
~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support region directives within method bodies or regions crossing boundaries of declaration blocks.
#Region "Region4"
~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_CObjInAttributes()
            Dim source =
    <compilation>
        <file name="a.vb"><![CDATA[
Class TestClass1

    <System.ComponentModel.DefaultValue(CObj("Test"))>
    Public Property Test2 As String

    '<System.ComponentModel.DefaultValue(CType("Test", Object))>
    'Public Property Test3 As String

    '<System.ComponentModel.DefaultValue(DirectCast("Test", Object))>
    'Public Property Test4 As String

    '<System.ComponentModel.DefaultValue(TryCast("Test", Object))>
    'Public Property Test5 As String
End Class
        ]]></file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(source, {SystemRef}, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC36716: Visual Basic 12.0 does not support CObj in attribute arguments.
    <System.ComponentModel.DefaultValue(CObj("Test"))>
                                        ~~~~
]]></expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_MultilineStrings()
            Dim source =
    <compilation>
        <file name="a.vb"><![CDATA[
Class TestClass

    Dim test4 = "
    This is
    a muiltiline
    string"

End Class
        ]]></file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 12.0 does not support multiline string literals.
    Dim test4 = "
                ~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_LineContinuationComments()
            Dim source =
    <compilation>
        <file name="a.vb"><![CDATA[
Class TestClass

    Dim test5 As String = ""
    Dim chars = From c In test5 'This is a test of comments in a linq statement
                Let asc = Asc(c) 'VS2015 can handle this
                Select asc

    Sub Test()
        Dim chars2 = From c In test5 'This is a test of comments in a linq statement
                     Let asc = Asc(c) 'VS2015 can handle this
                     Select asc
    End Sub
End Class
        ]]></file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 12.0 does not support line continuation comments.
    Dim chars = From c In test5 'This is a test of comments in a linq statement
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support line continuation comments.
        Dim chars2 = From c In test5 'This is a test of comments in a linq statement
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic9))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 9.0 does not support implicit line continuation.
    Dim chars = From c In test5 'This is a test of comments in a linq statement
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 9.0 does not support implicit line continuation.
        Dim chars2 = From c In test5 'This is a test of comments in a linq statement
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_TypeOfIsNot()
            Dim source =
    <compilation>
        <file name="a.vb"><![CDATA[
Class TestClass

        Sub Test()
            Dim test6 As String = ""
            If TypeOf test6 IsNot System.String Then Console.WriteLine("That string isn't a string")
        End Sub

End Class
        ]]></file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 12.0 does not support TypeOf IsNot expression.
            If TypeOf test6 IsNot System.String Then Console.WriteLine("That string isn't a string")
                            ~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_YearFirstDateLiterals()
            Dim source =
    <compilation>
        <file name="a.vb"><![CDATA[
Class TestClass

        Sub Test()
            Dim d = #2015-08-23#
        End Sub

End Class
        ]]></file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 12.0 does not support year-first date literals.
            Dim d = #2015-08-23#
                    ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_Pragma()
            Dim source =
    <compilation>
        <file name="a.vb"><![CDATA[
Class TestClass

        Sub Test()
#Disable Warning BC42024
            Dim test7 As String 'Should have no "unused variable" warning
#Enable Warning BC42024
        End Sub

End Class
        ]]></file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 12.0 does not support warning directives.
#Disable Warning BC42024
~~~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support warning directives.
#Enable Warning BC42024
~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_PartialModulesAndInterfaces()
            Dim source =
    <compilation>
        <file name="a.vb"><![CDATA[
Partial Module Module1
End Module

Partial Interface IFace
End Interface
        ]]></file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(source, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseParseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 12.0 does not support partial modules.
Partial Module Module1
~~~~~~~~~~~~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support partial interfaces.
Partial Interface IFace
~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(5072, "https://github.com/dotnet/roslyn/issues/5072")>
        Public Sub LangVersion_ImplementReadonlyWithReadwrite()
            Dim source =
    <compilation>
        <file name="a.vb"><![CDATA[
Interface IReadOnly
    ReadOnly Property Test1 As String
    WriteOnly Property Test2 As String
End Interface

Class ReadWrite
    Implements IReadOnly

    Public Property Test1 As String Implements IReadOnly.Test1

    Public Property Test2 As String Implements IReadOnly.Test2
End Class
        ]]></file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(source, {SystemRef}, parseOptions:=VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36716: Visual Basic 12.0 does not support implementing read-only or write-only property with read-write property.
    Public Property Test1 As String Implements IReadOnly.Test1
                                               ~~~~~~~~~~~~~~~
BC36716: Visual Basic 12.0 does not support implementing read-only or write-only property with read-write property.
    Public Property Test2 As String Implements IReadOnly.Test2
                                               ~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(13617, "https://github.com/dotnet/roslyn/issues/13617")>
        Public Sub MissingTypeArgumentInGenericExtensionMethod()
            Dim source =
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Module FooExtensions
    <Extension()>
    Public Function ExtensionMethod0(ByVal obj As Object)
        Return GetType(Object)
    End Function
    <Extension()>
    Public Function ExtensionMethod1(Of T)(ByVal obj As Object)
        Return GetType(T)
    End Function
    <Extension()>
    Public Function ExtensionMethod2(Of T1, T2)(ByVal obj As Object)
        Return GetType(T1)
    End Function
End Module

Module Module1
    Sub Main()
        Dim omittedArg0 As Type = "string literal".ExtensionMethod0(Of )()
        Dim omittedArg1 As Type = "string literal".ExtensionMethod1(Of )()
        Dim omittedArg2 As Type = "string literal".ExtensionMethod2(Of )()
        
        Dim omittedArgFunc0 As Func(Of Object) = "string literal".ExtensionMethod0(Of )
        Dim omittedArgFunc1 As Func(Of Object) = "string literal".ExtensionMethod1(Of )
        Dim omittedArgFunc2 As Func(Of Object) = "string literal".ExtensionMethod2(Of )

        Dim moreArgs0 As Type = "string literal".ExtensionMethod0(Of Integer)()
        Dim moreArgs1 As Type = "string literal".ExtensionMethod1(Of Integer, Boolean)()
        Dim moreArgs2 As Type = "string literal".ExtensionMethod2(Of Integer, Boolean, String)()

        Dim lessArgs1 As Type = "string literal".ExtensionMethod1()
        Dim lessArgs2 As Type = "string literal".ExtensionMethod2(Of Integer)()

        Dim nonExistingMethod0 As Type = "string literal".ExtensionMethodNotFound0()
        Dim nonExistingMethod1 As Type = "string literal".ExtensionMethodNotFound1(Of Integer)()
        Dim nonExistingMethod2 As Type = "string literal".ExtensionMethodNotFound2(Of Integer, String)()

        Dim exactArgs0 As Type = "string literal".ExtensionMethod0()
        Dim exactArgs1 As Type = "string literal".ExtensionMethod1(Of Integer)()
        Dim exactArgs2 As Type = "string literal".ExtensionMethod2(Of Integer, Boolean)()
    End Sub
End Module
        ]]></file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {Net40.References.SystemCore})

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36907: Extension method 'Public Function ExtensionMethod0() As Object' defined in 'FooExtensions' is not generic (or has no free type parameters) and so cannot have type arguments.
        Dim omittedArg0 As Type = "string literal".ExtensionMethod0(Of )()
                                                                   ~~~~~
BC30182: Type expected.
        Dim omittedArg0 As Type = "string literal".ExtensionMethod0(Of )()
                                                                       ~
BC30182: Type expected.
        Dim omittedArg1 As Type = "string literal".ExtensionMethod1(Of )()
                                                                       ~
BC36590: Too few type arguments to extension method 'Public Function ExtensionMethod2(Of T1, T2)() As Object' defined in 'FooExtensions'.
        Dim omittedArg2 As Type = "string literal".ExtensionMethod2(Of )()
                                                                   ~~~~~
BC30182: Type expected.
        Dim omittedArg2 As Type = "string literal".ExtensionMethod2(Of )()
                                                                       ~
BC36907: Extension method 'Public Function ExtensionMethod0() As Object' defined in 'FooExtensions' is not generic (or has no free type parameters) and so cannot have type arguments.
        Dim omittedArgFunc0 As Func(Of Object) = "string literal".ExtensionMethod0(Of )
                                                                                  ~~~~~
BC30182: Type expected.
        Dim omittedArgFunc0 As Func(Of Object) = "string literal".ExtensionMethod0(Of )
                                                                                      ~
BC30182: Type expected.
        Dim omittedArgFunc1 As Func(Of Object) = "string literal".ExtensionMethod1(Of )
                                                                                      ~
BC36590: Too few type arguments to extension method 'Public Function ExtensionMethod2(Of T1, T2)() As Object' defined in 'FooExtensions'.
        Dim omittedArgFunc2 As Func(Of Object) = "string literal".ExtensionMethod2(Of )
                                                                                  ~~~~~
BC30182: Type expected.
        Dim omittedArgFunc2 As Func(Of Object) = "string literal".ExtensionMethod2(Of )
                                                                                      ~
BC36907: Extension method 'Public Function ExtensionMethod0() As Object' defined in 'FooExtensions' is not generic (or has no free type parameters) and so cannot have type arguments.
        Dim moreArgs0 As Type = "string literal".ExtensionMethod0(Of Integer)()
                                                                 ~~~~~~~~~~~~
BC36591: Too many type arguments to extension method 'Public Function ExtensionMethod1(Of T)() As Object' defined in 'FooExtensions'.
        Dim moreArgs1 As Type = "string literal".ExtensionMethod1(Of Integer, Boolean)()
                                                                 ~~~~~~~~~~~~~~~~~~~~~
BC36591: Too many type arguments to extension method 'Public Function ExtensionMethod2(Of T1, T2)() As Object' defined in 'FooExtensions'.
        Dim moreArgs2 As Type = "string literal".ExtensionMethod2(Of Integer, Boolean, String)()
                                                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36589: Type parameter 'T' for extension method 'Public Function ExtensionMethod1(Of T)() As Object' defined in 'FooExtensions' cannot be inferred.
        Dim lessArgs1 As Type = "string literal".ExtensionMethod1()
                                                 ~~~~~~~~~~~~~~~~
BC36590: Too few type arguments to extension method 'Public Function ExtensionMethod2(Of T1, T2)() As Object' defined in 'FooExtensions'.
        Dim lessArgs2 As Type = "string literal".ExtensionMethod2(Of Integer)()
                                                                 ~~~~~~~~~~~~
BC30456: 'ExtensionMethodNotFound0' is not a member of 'String'.
        Dim nonExistingMethod0 As Type = "string literal".ExtensionMethodNotFound0()
                                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30456: 'ExtensionMethodNotFound1' is not a member of 'String'.
        Dim nonExistingMethod1 As Type = "string literal".ExtensionMethodNotFound1(Of Integer)()
                                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30456: 'ExtensionMethodNotFound2' is not a member of 'String'.
        Dim nonExistingMethod2 As Type = "string literal".ExtensionMethodNotFound2(Of Integer, String)()
                                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

    End Class
End Namespace
