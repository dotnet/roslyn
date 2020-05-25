' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Infer On
Option Explicit Off

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Linq.Expressions
Imports System.Text
Imports M = System.Math
Imports System.Collections
Imports <xmlns:ns="goo">
Imports <xmlns="goo">

#Const line = 6
#Const goo = True
#If goo Then
#Else
#End If
' There is no equivalent to #undef in VB.NET:
'#undef goo
'#warning goo
'#error goo
' There is no equivalent to 'extern alias' in VB:
'extern alias Goo;

#If DEBUG OrElse TRACE Then
Imports System.Diagnostics
#ElseIf SILVERLIGHT Then
Imports System.Diagnostics
#Else
Imports System.Diagnostics
#End If

#Region "Region"
#Region "more"
Imports ConsoleApplication2.Test
#End Region
Imports X = int1
Imports X = ABC.X(Of Integer)
Imports A.B

#End Region
<Assembly: System.Copyright("(C) 2009")> 
<Module: System.Copyright(vbLf & vbTab & ChrW(&H123).ToString() & "(C) 2009" & ChrW(&H123).ToString())> 
Friend Interface CoContra(Of Out T, In K)
End Interface
Public Delegate Sub CoContra2()

Namespace My

    Friend Interface CoContra(Of Out T, In K)
    End Interface
    Friend Delegate Sub CoContra2(Of Out T, In K)()


    Partial Public Class A
        Inherits CSType1
        Implements I
        <Obsolete()>
        Public Sub New(<Obsolete()> ByVal goo As Integer)
            MyBase.New(1)
L:
            Dim i As Integer = Len(New Integer)
            i += 1

#If DEBUG Then
            Console.WriteLine(export.iefSupplied.command)
#End If
            Const local? As Integer = Integer.MaxValue
            Const local0? As Guid = New Guid(r.ToString())

            'Inserted Compiling code 
            Dim r As Integer
            Dim Varioblelocal? As Integer = Integer.MaxValue
            Dim Varioblelocal0? As Guid = New Guid(r.ToString())

            Dim привет = local
            Dim мир = local
            Dim local3 = 0, local4 = 1
            Dim local5 = If(TryCast(Nothing, Action), Nothing)
            Dim local6 = TypeOf local5 Is Action

            Dim u = 1UI

            Dim U_Renamed = 1UI

            Dim hex As Long = &HBADC0DE, Hex_Renamed As Long = &HDEADBEEFL, l As Long = -1L, L_Renamed As Long = 1L

            Dim ul As ULong = 1UL, Ul_Renamed As ULong = 1UL, uL_Renamed2 As ULong = 1UL, UL_Renamed3 As ULong = 1UL, lu As ULong = 1UL, Lu_Renamed1 As ULong = 1UL, lU_Renamed2 As ULong = 1UL, LU_Renamed3 As ULong = 1UL

            Dim bool As Boolean
            Dim [byte] As Byte
            'ChrW(&H0130), hexchar2 = ChrW(&HBAD)
            'ChrW(&H0066), hexchar = ChrW(&H0130), hexchar2
            '"c"c, \u0066 = ChrW(&H0066), hexchar
            Dim [char] As Char = "c"c ', \u0066
            Dim [decimal] As Decimal = 1.44D

            Dim [dynamic] As Object
            Dim [double] As Double = m.PI
            Dim float As Single
            Dim int As Integer = If(local, -1)
            Dim [long] As Long
            Dim [object] As Object
            Dim [sbyte] As SByte
            Dim [short] As Short
            Dim [string] As String = """/*"
            Dim uint As UInteger
            Dim [ulong] As ULong
            Dim [ushort] As UShort


            Dim dynamic1 = local5
            Dim add = 0
            Dim ascending = 0
            Dim descending = 0
            Dim From = 0
            Dim [get] = 0
            Dim [global] = 0
            Dim group = 0
            Dim into = 0
            Dim join = 0
            Dim [let] = 0
            Dim orderby = 0
            Dim [partial] = 0
            Dim remove = 0
            Dim [select] = 0
            Dim [set] = 0
            Dim value = 0
            Dim var = 0
            Dim where = 0
            Dim yield = 0

            If i > 0 Then
                Return
            ElseIf i = 0 Then
                Throw New Exception()
            End If
            Dim o1 = New MyObject()
            Dim o2 = New MyObject(var)
            Dim o3 = New MyObject With {.A = i}
            Dim o4 = New MyObject(dynamic) With {.A = 0, .B = 0, .C = 0}
            Dim o5 = New With {Key .A = 0}
            Dim a() As Integer = {0, 1, 2, 3, 4, 5}
            Select Case i
                Case 1
                    GoTo CaseLabel1
                Case 2
CaseLabel1:
                    GoTo CaseLabel2
                    Exit Select
                Case Else
CaseLabel2:
                    Return
            End Select
            Do While i < 10
                i += 1
            Loop
            Do
                i += 1
            Loop While i < 10
            For j As Integer = 0 To 99
                Console.WriteLine(j)
            Next j

            'Modified to include items
            Dim items = {1, 2, 3, 4, 5, 6, 7, 8}
            For Each i In items
                If i = 7 Then
                    Return
                Else
                    Continue For
                End If
            Next i

            ' There is no equivalent to a 'checked' block in VB.NET
            '			checked
            i += 1

            'Modified use of synclock functions for VB
            Dim sText As String
            Dim objLock As Object = New Object()
            SyncLock objLock
                sText = "Hello"
            End SyncLock

            Using v = BeginScope()
                Using a As New A()
                    Using BeginScope()
                        Return
                    End Using
                End Using
            End Using

            ' VB does not support iterators and has no equivalent to the C# 'yield' keyword:
            'yield Return Me.items(i)
            ' VB does not support iterators and has no equivalent to the C# 'yield' keyword:
            'yield(break)
            ' There is no equivalent to a 'fixed' block in VB.NET
            'Integer* p = Nothing

            Try
                Throw New Exception 'Nothing
            Catch av As System.AccessViolationException
                Throw av
            Catch e1 As Exception
                Throw
            Finally
            End Try

            Dim anonymous = New With {.a = 1, .B = 2, .c = 3}

            Dim qry = From i1 In {1, 2, 3, 4, 5, 6}
                      Where i1 < 5
                      Select New With {.id = i1}


            Dim query = From c In customers _
                            Let d = c _
                            Where d IsNot Nothing _
                            Join c1 In customers On c1.GetHashCode() Equals c.GetHashCode() _
                            Group Join c1 In customers On c1.GetHashCode() Equals c.GetHashCode()
                            Into e() _
                            Order By g.Count() Ascending _
                            Order By g.Key Descending _
                            Select New With {.Region = g.Key, .CustCount = g.Count()}


            'XML Literals
            Dim x = <xmlliteral>
                        <test name="test"/>
                        <test name="test2"></test>
                    </xmlliteral>

        End Sub

        Protected Sub Finalize()
        End Sub
        Private ReadOnly f1 As Integer
        ' There is no VB.NET equivalent to 'volatile':

        <Obsolete(), NonExisting(), Goo.NonExisting(var, 5), Obsolete(), NonSerialized(), CLSCompliant(True OrElse False And True)>
        Private f2 As Integer

        <Obsolete()>
        Public Sub Handler(ByVal value As Object)
        End Sub

        Public Function m(Of T As {Class, New})(ByVal t1 As T) As Integer
            MyBase.m(t1)
            Return 1
        End Function
        Public Property P() As String
            Get
                Return "A"
            End Get
            Set(ByVal value As String)
            End Set
        End Property

        Public ReadOnly Property p2 As String
            Get
            End Get
        End Property

        Public Property p3 As String

        Default Public Property item(ByVal index As Integer) As Integer
            Protected Get
            End Get
            Set(ByVal value As Integer)
            End Set
        End Property

        <Obsolete(), Obsolete()>
        Public Custom Event E1 As Action
            ' This code will be run when AddHandler MyEvent, D1 is called
            AddHandler(ByVal value As Action)
            End AddHandler

            ' This code will be run when RemoveHandler MyEvent, D1 is called
            RemoveHandler(ByVal value As Action)
            End RemoveHandler

            <Obsolete()> RaiseEvent()
            End RaiseEvent
        End Event


        Public Shared Operator +(ByVal first, ByVal second)
            Dim handler As System.Delegate = New [Delegate](AddressOf Me.Handler)
            Return first.Add(second)
        End Operator

        <Obsolete()>
        Public Shared Operator IsTrue(ByVal a As A) As Boolean
            Return True
        End Operator
        Public Shared Operator IsFalse(ByVal a As A) As Boolean
            Return False
        End Operator

        Class c
        End Class

        Public Sub A(ByVal value As Integer) Implements I.A

        End Sub

        Public Property Value As String Implements I.Value
            Get

            End Get
            Set(ByVal value As String)

            End Set
        End Property
    End Class

    Public Structure S
        Implements I

        Private f1 As Integer
        ' There is no VB.NET equivalent to 'volatile':
        ' private volatile int f2;

        <Obsolete()> Private f2 As Integer

        Public Function m(Of T As {Structure, New})(ByVal s As T) As Integer
            Return 1
        End Function

        Public Property P1() As String
            Get
                Dim value As Integer = 0
                Return "A"
            End Get
            Set(ByVal value As String)
            End Set
        End Property

        'VB.NET can't support abstract member variable
        Public ReadOnly Property P2() As String
            Get
            End Get
        End Property

        Public Property p3 As String '//Auto Property

        Default Public Property item(ByVal index As Integer) As Integer
            Get
            End Get
            Friend Set(ByVal value As Integer)
            End Set
        End Property


        Public Event E()

        Public Shared Operator +(ByVal first, ByVal second)
            Return first.Add(second)
            'fixed Integer field(10)
        End Operator

        Class c
        End Class

        Public Sub A(ByVal value As Integer) Implements I.A

        End Sub

        Public Property Value As String Implements I.Value
            Get

            End Get
            Set(ByVal value As String)

            End Set
        End Property
    End Structure
    Public Interface I
        Sub A(ByVal value As Integer)
        Property Value() As String
    End Interface
    <Flags()>
    Public Enum E
        A
        B = A
        C = 2 + A

#If DEBUG Then
        D
#End If

    End Enum
    Public Delegate Sub [Delegate](ByVal P As Object)

    Namespace Test
        Public Class Список
            Public Shared Function Power(ByVal number As Integer, ByVal exponent As Integer) As IEnumerable
                Dim Список As New Список()
                Список.Main()
                Dim counter As Integer = 0
                Dim result As Integer = 0

                'Do While ++counter++ < --exponent--
                '                         result = result * number + +number + ++++number
                '                ' VB does not support iterators and has no equivalent to the C# 'yield' keyword:
                '                'yield Return result
                '            Loop
            End Function
            Shared Sub Main()
                For Each i As Integer In Power(2, 8)
                    Console.Write("{0} ", arg0:=i)
                Next i
            End Sub
        End Class
    End Namespace
End Namespace

Namespace ConsoleApplication1
    Namespace RecursiveGenericBaseType
        MustInherit Class A(Of T)
            Inherits B(Of A(Of T), A(Of T))

            Protected Overridable Function M() As A(Of T)
            End Function

            Protected MustOverride Function N() As B(Of A(Of T), A(Of T))

            Shared Function O() As B(Of A(Of T), A(Of T))
            End Function
        End Class

        Class B(Of T1, T2)
            Inherits A(Of B(Of T1, T2))

            Protected Overrides Function M() As A(Of T)
            End Function
            Protected NotOverridable Overrides Function N() As B(Of A(Of T), A(Of T))
            End Function
            Shared Shadows Function O() As A(Of T)
            End Function
        End Class
    End Namespace
End Namespace

Namespace Boo
    Public Class Bar(Of T As IComparable)
        Public f As T

        Public Class Goo(Of U)
            Implements IEnumerator(Of T)

            Public Sub Method(Of K As {IList(Of V), IList(Of T), IList(Of U)}, V As IList(Of K))(ByVal k1 As K, ByVal t1 As T, ByVal u1 As U)
                Dim a As A(Of Integer)
            End Sub

            Public ReadOnly Property Current As T Implements System.Collections.Generic.IEnumerator(Of T).Current
                Get

                End Get
            End Property

            Public ReadOnly Property Current1 As Object Implements System.Collections.IEnumerator.Current
                Get

                End Get
            End Property

            Public Function MoveNext() As Boolean Implements System.Collections.IEnumerator.MoveNext

            End Function

            Public Sub Reset() Implements System.Collections.IEnumerator.Reset

            End Sub

#Region "IDisposable Support"
            Private disposedValue As Boolean ' To detect redundant calls

            ' IDisposable
            Protected Overridable Sub Dispose(ByVal disposing As Boolean)
                If Not Me.disposedValue Then
                    If disposing Then

                    End If


                End If
                Me.disposedValue = True
            End Sub

            Public Sub Dispose() Implements IDisposable.Dispose
                Dispose(True)
                GC.SuppressFinalize(Me)
            End Sub
#End Region

        End Class
    End Class
End Namespace

Friend Class Test2
    Private Sub Bar3()
        Dim x = New Boo.Bar(Of Integer).Goo(Of Object)()
        x.Method(Of String, String)(" ", 5, New Object())

        Dim q = From i In New Integer() {1, 2, 3, 4}
                Where i > 5
                Select i
    End Sub

    Public Shared Widening Operator CType(ByVal s As String) As Test2
        Return New Test2()
    End Operator

    Public Shared Narrowing Operator CType(ByVal s As Integer) As Test2
        Return New Test2()
    End Operator

    Public goo As Integer = 5
    Private Sub Bar2()
        goo = 6
        Me.goo = 5.GetType()
        Dim t As Test2 = "sss"
    End Sub
    Private Sub Blah()
        Dim i As Integer = 5
        Dim j? As Integer = 6

        Dim e As Expression(Of Func(Of Integer)) = Function() i
    End Sub

    Public Property FGoo() As Type
        Get
            Return GetType(System.Int32)
        End Get
        Set(ByVal value As Type)
            Dim t = GetType(System.Int32)
            t.ToString()
            t = value
        End Set
    End Property
    Public Sub Constants()
        Dim i As Integer = 1 + 2 + 3 + 5
        Dim s As Global.System.String = "a" & CStr("a") & "a" & "a" & "a" & "A"
    End Sub

    Public Sub ConstructedType()
        Dim i As List(Of Integer) = Nothing
        Dim c As Integer = i.Count
    End Sub
End Class
Namespace Comments.XmlComments.UndocumentedKeywords
    ''' <summary>
    ''' Whatever 
    ''' </summary>
    ''' <!-- c -->
    ''' <![CDATA[c]]> //
    ''' <c></c> /* */
    ''' <code></code>
    ''' <example></example>
    ''' <exception cref="bla"></exception>
    ''' <include file='' path='[@name=""]'/>
    ''' <permission cref=" "></permission>
    ''' <remarks></remarks>
    ''' <see cref=""/>
    ''' <seealso cref=" "/>
    ''' <value></value>
    ''' <typeparam name="T"></typeparam>
    Class c(Of T)
        Sub M(Of U)(ByVal T1 As T, ByVal U1 As U)
            Dim intValue As Integer = 0
            intValue = intValue + 1
            Dim strValue As String = "hello" 's
            Dim c As New [MyClass]()
            Dim verbatimStr As String = "@ \\\\" 's
        End Sub
    End Class

End Namespace
Friend Class TestClassXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX 'Scen8
End Class

Friend Class TestClass1XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX22 'Scen9
End Class

Friend Class yield
    ''INSTANT VB TODO TASK: There is no equivalent to the undocumented C# '__arglist' keyword in VB:
    'Private Sub Goo(Of U)(ByVal __arglist)
    '    Dim c1 As C(Of U) = Nothing
    '    c1.M(Of Integer)(5, Nothing)
    '    Dim tr As TypedReference = __makeref(c1)
    '    Dim t As Type = __reftype(tr)

    '    	Dim j As Integer = __refvalue(tr, Integer)

    '    Params(a:=t, b:=t)
    'End Sub
    Private Sub Params(ByRef a As Object, <System.Runtime.InteropServices.Out()> ByRef b As Object, ByVal ParamArray c() As Object)
    End Sub

    'Private Sub Params(Optional <System.Runtime.InteropServices.Out()> ByRef a As dynamic = 2, Optional ByRef c As dynamic = Nothing, ParamArray ByVal c()() As dynamic)
    'End Sub
    Public Overrides Function ToString() As String
        Return MyBase.ToString()
    End Function

    Public Sub method()
        Dim a?(4) As Integer '[] bug
        ' YES []
        Dim var() As Integer = {1, 2, 3, 4, 5} ',;
        Dim i As Integer = a(i) '[]
        Dim f As New Goo(Of T)() '<> ()
        f.method()
        i = i + i - i * i \ i Mod i And i Or i Xor i '+ - * / % & | ^

        Dim b As Boolean = True And False Or True Xor False '& | ^
        b = Not b '!
        i = Not i '~i
        b = i < i AndAlso i > i '< && >

        Dim ii? As Integer = 5 '? bug
        ' NO ?
        Dim f1 As Integer = If(True, 1, 0) '? :
        ' YES :
        i += 1 '++
        i -= 1 '--
        b = True AndAlso False OrElse True '&& ||
        i = i << 5 '<<
        i = i >> 5 '>>
        b = i = i AndAlso i <> i AndAlso i <= i AndAlso i >= i '= == && != <= >=
        i += 5.0 '+=
        i -= i '-=
        i *= i '*=
        i \= i '/
        '=
        i = i Mod i '%=
        i = i And i '&=
        i = i Or i '|=
        i = i Xor i '^=
        i <<= i '<<=
        i >>= i '>>=
        Dim s As Object = Function(x) x + 1 '=>


        ' There is no equivalent to an 'unsafe' block in VB.NET
        '			unsafe
        '		Point* p = &point '* &
        '			p->x = 10 '->

        Dim p As Point
        p.X = 10
        p.Y = 12

        Dim p2 As New Point With {.X = 10, .Y = 12}


        Dim br As IO.BinaryReader = Nothing
    End Sub

    Friend Structure Point
        Public X As Integer
        Public Y As Integer
    End Structure
End Class

'Extension Method
Module Module1
    <Runtime.CompilerServices.Extension()> Function GooExtension(ByVal x As String) As String
        Return x & "test"
    End Function

    <Runtime.CompilerServices.Extension()> Function GooExtension(ByVal x As String,
                                                                 ByVal y As Integer) As String
        'With Implicit Line Continuation
        Return x & "test2"
    End Function

    Sub Goo()
        'Collections
        Dim i As New List(Of String) From {"test", "item"}
        Dim i1 As New Dictionary(Of Integer, String) From {{1, "test"}, {2, "item"}}

        'Arrays
        Dim ia1 = {1, 2, 3, 4, 5}
        Dim la2 = {1,
                   2L,
                   3,
                   4S,
                   5}
        Console.Write(GetXmlNamespace(ns))
        Dim ia3 As Integer() = {1,
                                2,
                                3, 4, 5}
        Dim ia4() As Integer = {1,
                                2, 3, 4,
                                5}

        Dim ia5 = New Integer() {1, 2, 3, 4, 5}


        Dim ia6 = {{1, 2}, {3, 4}, {5, 6}} '2d array
        Dim ia7 = {({1}), ({3, 4}), ({5, 6, 2})} 'jagged array

        'Standalone
        If {1, 2, 3}.Count = 2 Then
        ElseIf {1, 2, 3}.Count = 3 Then
        Else
        End If

    End Sub
End Module




#Region "Events"
Public Delegate Sub MyDelegate(ByVal message As String)

Class MyClass1

    Custom Event MyEvent As MyDelegate

        ' This code will be run when AddHandler MyEvent, D1
        ' is called
        AddHandler(ByVal value As MyDelegate)
            Console.WriteLine("Adding Handler for MyEvent")
            MyEventHandler = value
        End AddHandler

        ' This code will be run when RemoveHandler MyEvent, D1
        ' is called
        RemoveHandler(ByVal value As MyDelegate)
            Console.WriteLine("Removing Handler for MyEvent")
            MyEventHandler = Nothing
        End RemoveHandler

        ' This code will be run when RaiseEvent MyEvent(string)
        ' is called
        RaiseEvent(ByVal message As String)
            If Not MyEventHandler Is Nothing Then
                MyEventHandler.Invoke(message)
            Else
                Console.WriteLine("No Handler for Raised MyEvent")
            End If
        End RaiseEvent
    End Event

    Public MyEventHandler As MyDelegate

    Public Sub Raise_Event()
        RaiseEvent MyEvent("MyEvent Was Raised")
    End Sub
End Class

Module DelegateModule
    Dim Var1 As MyClass1
    Dim D1 As MyDelegate

    Sub EventsMain()
        Var1 = New MyClass1
        D1 = New MyDelegate(AddressOf MyHandler)
        AddHandler Var1.MyEvent, D1
        Var1.Raise_Event()
        RemoveHandler Var1.MyEvent, D1
    End Sub

    Sub MyHandler(ByVal message As String)
        Console.WriteLine("Event Handled: " & message)
    End Sub
End Module

#End Region

#Region "Linq"
Module LINQQueries
    Sub Join()
        Dim categories() = {"Beverages", "Condiments", "Vegetables", "Dairy Products", "Seafood"}

        Dim productList = {New With {.category = "Condiments", .name = "Ketchup"}, New With {.category = "Seafood", .name = "Code"}}

        Dim query = From c In categories _
                    Group Join p In productList On c Equals p.category Into Group _
                    From p In Group _
                    Select Category = c, p.name

        For Each v In query
            Console.WriteLine(v.name + ": " + v.Category)
        Next
    End Sub
End Module
#End Region


#Region "Lambda's"
Module Lambdas
    Dim l1 = Sub()
                 Console.WriteLine("Sub Statement")
             End Sub

    Dim L2 = Sub() Console.WriteLine("Sub Statement 2")

    Dim L3 = Function(x As Integer) x Mod 2

    Dim L4 = Function(y As Integer) As Boolean
                 If y * 2 < 10 Then
                     Return True
                 Else
                     Return False
                 End If
             End Function
End Module
#End Region

#Region "Co Contra Variance"
Public Class Cheetah

End Class
Public Class Animals

End Class
Public Interface IVariance(Of In T)
    Sub Goo(ByVal a As T)
    Property InterProperty() As IVariance(Of Cheetah)
    Property InterProperty2() As IVariance(Of Animals)
End Interface

Delegate Sub Func(Of In T)(ByVal a As T)


Public Delegate Function Func2(Of Out T)() As T
Public Interface IVariance2(Of Out T)
    Function Goo() As T
End Interface

Public Class Variance2(Of T As New) : Implements IVariance2(Of T)

    Dim type As IVariance2(Of Animals)

    Public Function Goo() As T Implements IVariance2(Of T).Goo
        Return New T
    End Function

    Function Goo(ByVal arg As IVariance2(Of T)) As String
        Return arg.GetType.ToString
    End Function

    Function Goo(ByVal arg As Func2(Of T)) As String
        Return arg.Invoke().GetType.ToString
    End Function
End Class

#End Region

Module Mod1Orcas
    Dim AT1 = New With {Key .prop1 = 1}
    Dim AT2 = New With {.prop1 = 7}
    Dim b_false As Boolean = False
    Dim n_false = False
    Dim i = If(b_false And n_false, 1, 2)
    Dim s1 = <xml_literal><%= If(Nothing, Nothing) %></xml_literal>

    Delegate Sub delfoo()
    Delegate Sub delfoo1(ByVal sender As Object, ByVal e As System.EventArgs)

    Sub Goo()
    End Sub

    Sub Method1(ByVal sender As Object, ByVal e As System.EventArgs)
    End Sub
    Sub Method1a()
    End Sub

    Sub AssignDelegate()
        Dim d As delfoo = AddressOf Goo
        d.Invoke()


        Dim d1_1 As delfoo1 = AddressOf Method1
        Dim d1_1a As delfoo1 = AddressOf Method1a 'Relaxed Delegate




        'Nullable
        Dim Value1a As Integer? = 10
        Dim Value1b As Integer = 1
        Dim Value1c? As Integer = 1
        Dim Value1c? As Integer? = 1
        Dim TestReturnValue = Value1a * Value1b
        If Value1a / Value1b > 0 Then

        End If

        Dim sNone = "None"
        Dim SSystemOnly = "SystemOnly"

        Dim XMLLiteral = <?xml version="1.0" encoding="utf-8"?>

                         <Details>
                             <FileImports>
                                 <FileImport name=<%= sNone %>>
                                 </FileImport>
                                 <FileImport name=<%= SSystemOnly %>>
        Imports System
      </FileImport>
                                 <FileImport name="Default">
        Imports System
        Imports System.Collections
      </FileImport>
                             </FileImports>

                             <CodeConstructs>
                                 <!-- Type Constructs-->
                                 <Construct name="Module" allowcodeblock="false" allowOuter="true" group="Type" allownesting="true" isnestable="false" allowsoverload="false">
                                     <Start>public Module {Identifier}</Start>
                                     <End>End Module </End>
                                     <DefaultIdent>Module_</DefaultIdent>
                                 </Construct>
                                 <Construct name="Class" allowcodeblock="false" allowOuter="true" group="Type" allownesting="true" isnestable="true" allowsoverload="false">
                                     <Start>public class {Identifier}</Start>
                                     <End>End Class</End>
                                     <DefaultIdent>Class_</DefaultIdent>
                                 </Construct>
                                 <Construct name="Structure" allowcodeblock="false" allowOuter="true" group="Type" allownesting="true" isnestable="true" allowsoverload="false">
                                     <Start>public class {Identifier}</Start>
                                     <End>End Class</End>
                                     <DefaultIdent>Struct_</DefaultIdent>
                                 </Construct>
                             </CodeConstructs>

                             <CodeBlocks>
                                 <Block name="CodeBlock0.txt" statements="1">
                                     <![CDATA[ Dim <{x0}> = Microsoft.VisualBasic.FileSystem.Dir(".") ]]>
                                 </Block>
                                 <Block name="CodeBlock1.txt" statements="1">
                                     <![CDATA[ Dim <{x0}> = 1 ]]>
                                 </Block>
                                 <Block name="CodeBlock2.txt" statements="1">
                                     <![CDATA[ Dim <{x0}> as string = "2" ]]>
                                 </Block>
                             </CodeBlocks>
                         </Details>
        Dim x = <![CDATA[ Dim <{x0}> as string = "2" ]]>
        Dim y = <!-- --> : Call <?a?>() : Dim x = <e/>
    End Sub
End Module

Class Customer
    Public Property name As String = "Default"
    Public AGe As Integer
    Public Position As String
    Public Level As Integer = 0
    Public Property age2 As Integer
End Class

Class Goo
    Structure Bar
        Dim x As Integer

        Sub LoopingMethod()
            For i = 1 To 20 Step 1
            Next i

            For Each a In {1, 2, 3, 4}
            Next

            Dim icount As Integer
            Do While icount <= 10
                icount += 1
            Loop

            icount = 0
            While icount <= 100
                icount += 1
            End While

            icount = 0
            Do Until icount >= 10
                icount += 2
            Loop
        End Sub
    End Structure
End Class

Class GooGen(Of t)
    Structure BarGen(Of u)
        Dim x As t
        Dim z As u
        Sub SelectionMethods()

            Dim icount As Integer = 1L

            If icount = 1 Then
            ElseIf icount > 1 Then
            Else
            End If

            Select Case icount
                Case 1
                Case 2, 3
                Case Is > 3
                Case Else
            End Select
        End Sub

        Sub Operators()
            Dim a As Boolean = True
            Dim b As Boolean = False

            If a And
                b Then
            End If

            If a Or
                b Then
            End If

            If Not a And
                   b Then
            End If

            If a = b AndAlso
                   b = True Then
            End If

            If a =
                b OrElse
                b =
                False Then
            End If

            If (a Or
                b) OrElse b =
            True Then
            End If

        End Sub

        Sub Method1()

            Dim x As New Customer With {.name = "Test",
                                        .AGe = 30,
                                        .Level = 1, .Position = "SDET"}


            Dim x2 As New Customer With {.name = "Test",
                                        .AGe = 30,
                                        .Level = 1, .Position = "SDET",
                                         .age2 = .AGe}

        End Sub


    End Structure
End Class


Public Class Bar

End Class

Public Class ClsPPMTest003
    Partial Private Sub Goo3()
    End Sub
End Class

Partial Public Class ClsPPMTest003
    Private Sub Goo3()
    End Sub

    Public Sub CallGooFromClass()
        Me.Goo3()
        Dim x1 As New Goo
        Dim y1 As New Bar

        If x1 Is y1 Then
        Else
            Console.WriteLine("Expected Result Occurred")
        End If

        If x1 IsNot y1 Then
        Else
            Console.WriteLine("Expected Result Occurred")
        End If

    End Sub
End Class


