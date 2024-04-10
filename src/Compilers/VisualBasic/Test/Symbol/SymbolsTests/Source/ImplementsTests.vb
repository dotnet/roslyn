' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Globalization
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class ImplementsTests
        Inherits BasicTestBase

        <Fact>
        Public Sub SimpleImplementation()
            CompileAndVerify(
<compilation name="SimpleImplementation">
    <file name="a.vb">
Option Strict On

Interface IFoo
    Sub SayItWithStyle(ByVal style As String)
    Sub SayItWithStyle(ByVal answer As Integer)
End Interface

Class Foo
    Implements IFoo

    Public Sub X(ByVal style As String) Implements IFoo.SayItWithStyle
        System.Console.WriteLine("I said: {0}", style)
    End Sub

    Public Sub Y(ByVal a As Integer) Implements IFoo.SayItWithStyle
        System.Console.WriteLine("The answer is: {0}", a)
    End Sub

    Public Overridable Sub SayItWithStyle(ByVal style As String)
        System.Console.WriteLine("You don't say: {0}", style)
    End Sub
End Class

Class Bar
    Inherits Foo

    Public Overrides Sub SayItWithStyle(ByVal style As String)
        System.Console.WriteLine("You don't say: {0}", style)
    End Sub
End Class

Module Module1
    Sub Main()
        Dim f As IFoo = New Foo
        f.SayItWithStyle("Eric Clapton rocks!")
        f.SayItWithStyle(42)

        Dim g As IFoo = New Bar
        g.SayItWithStyle("Lady Gaga rules!")
        g.SayItWithStyle(13)
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
I said: Eric Clapton rocks!
The answer is: 42
I said: Lady Gaga rules!
The answer is: 13
]]>)
        End Sub

        <Fact>
        Public Sub SimpleImplementationProperties()
            CompileAndVerify(
<compilation name="SimpleImplementationProperties">
    <file name="a.vb">
Option Strict On

Interface IFoo
    Property MyString As String
    Property MyInt As Integer
End Interface

Class Foo
    Implements IFoo

    Private s As String

    Public Property IGotYourInt As Integer Implements IFoo.MyInt

    Public Property IGotYourString As String Implements IFoo.MyString
        Get
            Return "You got: " + s
        End Get
        Set(value As String)
            s = value
        End Set
    End Property

    Public Overridable Property MyString As String
End Class

Class Bar
    Inherits Foo
    Private s As String
    Public Overrides Property MyString As String
        Get
            Return "I got: " + s
        End Get
        Set(value As String)
            s = value + " and then some"
        End Set
    End Property
End Class

Module Module1
    Sub Main()
        Dim f As IFoo = New Foo
        f.MyInt = 178
        f.MyString = "Lady Gaga"
        System.Console.WriteLine(f.MyInt)
        System.Console.WriteLine(f.MyString)

        Dim g As IFoo = New Bar
        g.MyInt = 12
        g.MyString = "Eric Clapton"
        System.Console.WriteLine(g.MyInt)
        System.Console.WriteLine(g.MyString)
    End Sub
End Module

    </file>
</compilation>,
    expectedOutput:=<![CDATA[
178
You got: Lady Gaga
12
You got: Eric Clapton
]]>)

        End Sub

        <Fact>
        Public Sub SimpleImplementationOverloadedProperties()
            CompileAndVerify(
<compilation name="SimpleImplementationOverloadedProperties">
    <file name="a.vb">
Option Strict On

Interface IFoo
    Property MyString As String
    Property MyString(x As Integer) As String
    Property MyString(x As String) As String
    Property MyInt As Integer
End Interface

Class Foo
    Implements IFoo

    Private s, t, u As String

    Public Property IGotYourInt As Integer Implements IFoo.MyInt

    Public Property IGotYourString As String Implements IFoo.MyString
        Get
            Return "You got: " + s
        End Get
        Set(value As String)
            s = value
        End Set
    End Property

    Public Property IGotYourString2(x As Integer) As String Implements IFoo.MyString
        Get
            Return "You got: " &amp; t &amp; " and " &amp; x
        End Get
        Set(value As String)
            t = value + "2"
        End Set
    End Property

    Public Property IGotYourString3(x As String) As String Implements IFoo.MyString
        Get
            Return "You used to have: " &amp; u &amp; " and " &amp; x
        End Get
        Set(value As String)
            u = value + "3"
        End Set
    End Property

    Public Overridable Property MyString As String
End Class


Module Module1
    Sub Main()
        Dim f As IFoo = New Foo
        f.MyInt = 178
        f.MyString = "Lady Gaga"
        f.MyString(8) = "Eric Clapton"
        f.MyString("foo") = "Katy Perry"

        System.Console.WriteLine(f.MyInt)
        System.Console.WriteLine(f.MyString)
        System.Console.WriteLine(f.MyString(4))
        System.Console.WriteLine(f.MyString("Bob Marley"))
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
178
You got: Lady Gaga
You got: Eric Clapton2 and 4
You used to have: Katy Perry3 and Bob Marley
]]>)

        End Sub

        <Fact>
        Public Sub ReImplementation()
            CompileAndVerify(
<compilation name="ReImplementation">
    <file name="a.vb">
Option Strict On

Interface IFoo
    Sub SayItWithStyle(ByVal style As String)
    Sub SayItWithStyle(ByVal answer As Integer)
End Interface

Class Foo
    Implements IFoo

    Public Sub X(ByVal style As String) Implements IFoo.SayItWithStyle
        System.Console.WriteLine("I said: {0}", style)
    End Sub

    Public Sub Y(ByVal a As Integer) Implements IFoo.SayItWithStyle
        System.Console.WriteLine("The answer is: {0}", a)
    End Sub

    Public Overridable Sub SayItWithStyle(ByVal style As String)
        System.Console.WriteLine("You don't say: {0}", style)
    End Sub
End Class

Class Bar
    Inherits Foo
    Implements IFoo

    Private Sub Z(ByVal a As Integer) Implements IFoo.SayItWithStyle
        System.Console.WriteLine("The question is: {0}", a)
    End Sub

    Public Overrides Sub SayItWithStyle(ByVal style As String)
        System.Console.WriteLine("I don't say: {0}", style)
    End Sub
End Class

Module Module1
    Sub Main()
        Dim f As IFoo = New Foo
        f.SayItWithStyle("Eric Clapton rocks!")
        f.SayItWithStyle(42)

        Dim g As IFoo = New Bar
        g.SayItWithStyle("Lady Gaga rules!")
        g.SayItWithStyle(13)
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
I said: Eric Clapton rocks!
The answer is: 42
I said: Lady Gaga rules!
The question is: 13
]]>)
        End Sub

        <Fact>
        Public Sub ReImplementationProperties()
            CompileAndVerify(
<compilation name="ReImplementationProperties">
    <file name="a.vb">
Option Strict On

Interface IFoo
    Property MyString As String
    Property MyInt As Integer
End Interface

Class Foo
    Implements IFoo

    Private s As String

    Public Property IGotYourInt As Integer Implements IFoo.MyInt

    Public Property IGotYourString As String Implements IFoo.MyString
        Get
            Return "You got: " + s
        End Get
        Set(value As String)
            s = value
        End Set
    End Property

    Public Overridable Property MyString As String
End Class

Class Bar
    Inherits Foo
    Implements IFoo

    Private s As String

    Public Property AnotherString As String Implements IFoo.MyString
        Get
            Return "I got your: " + s
        End Get
        Set(value As String)
            s = value + " right here"
        End Set
    End Property
End Class

Module Module1
    Sub Main()
        Dim f As IFoo = New Foo
        f.MyInt = 178
        f.MyString = "Lady Gaga"
        System.Console.WriteLine(f.MyInt)
        System.Console.WriteLine(f.MyString)

        Dim g As IFoo = New Bar
        g.MyInt = 12
        g.MyString = "Eric Clapton"
        System.Console.WriteLine(g.MyInt)
        System.Console.WriteLine(g.MyString)
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
178
You got: Lady Gaga
12
I got your: Eric Clapton right here
]]>)
        End Sub

        <Fact>
        Public Sub ImplementationOfGenericMethod()
            CompileAndVerify(
<compilation name="ImplementationOfGenericMethod">
    <file name="a.vb">
Option Strict On
Imports System
Imports System.Collections.Generic

Interface IFoo
    Sub SayItWithStyle(Of T)(ByVal style As T)
    Sub SayItWithStyle(Of T)(ByVal style As IList(Of T))
End Interface

Class Foo
    Implements IFoo


    Public Sub SayItWithStyle(Of U)(ByVal style As System.Collections.Generic.IList(Of U)) Implements IFoo.SayItWithStyle
        System.Console.WriteLine("style is IList(Of U)")
    End Sub

    Public Sub SayItWithStyle(Of V)(ByVal style As V) Implements IFoo.SayItWithStyle
        System.Console.WriteLine("style is V")
    End Sub
End Class


Module Module1
    Sub Main()
        Dim f As IFoo = New Foo
        Dim g As List(Of Integer) = New List(Of Integer)
        g.Add(42)
        g.Add(13)
        g.Add(14)
        f.SayItWithStyle(Of String)("Eric Clapton rocks!")
        f.SayItWithStyle(Of Integer)(g)
    End Sub
End Module    
</file>
</compilation>,
    expectedOutput:=<![CDATA[
style is V
style is IList(Of U)
]]>)
        End Sub

        <Fact>
        Public Sub ImplementationOfGenericMethod2()
            CompileAndVerify(
<compilation name="ImplementationOfGenericMethod2">
    <file name="a.vb">
Option Strict On
Imports System
Imports System.Collections.Generic

Interface IFoo(Of K, L)
    Sub SayItWithStyle(Of T)(ByVal style As T, ByVal a As K, ByVal b As Dictionary(Of L, K))
    Sub SayItWithStyle(Of T)(ByVal style As IList(Of T), ByVal a As L, ByVal b As Dictionary(Of K, L))
End Interface

Class Foo(Of M)
    Implements IFoo(Of M, ULong)

    Public Sub SayItWithStyle(Of T)(ByVal style As T, ByVal a As M, ByVal b As Dictionary(Of ULong, M)) Implements IFoo(Of M, ULong).SayItWithStyle
        Console.WriteLine("first")
    End Sub
    Public Sub SayItWithStyle(Of T)(ByVal style As System.Collections.Generic.IList(Of T), ByVal a As ULong, ByVal b As Dictionary(Of M, ULong)) Implements IFoo(Of M, ULong).SayItWithStyle
        Console.WriteLine("second")
    End Sub
End Class

Module Module1
    Sub Main()
        Dim f As IFoo(Of String, ULong) = New Foo(Of String)()
        Dim g As List(Of Integer) = New List(Of Integer)
        g.Add(42)
        g.Add(13)
        g.Add(14)
        Dim h As Dictionary(Of String, ULong) = Nothing
        Dim i As Dictionary(Of ULong, String) = Nothing

        f.SayItWithStyle(Of String)("Eric Clapton rocks!", "hi", i)
        f.SayItWithStyle(Of Integer)(g, 17, h)
    End Sub
End Module
</file>
</compilation>,
    expectedOutput:=<![CDATA[
first
second
]]>)
        End Sub

        <Fact>
        Public Sub ImplementationOfGenericMethod3()
            CompileAndVerify(
<compilation name="ImplementationOfGenericMethod3">
    <file name="a.vb">
Option Strict On

Namespace MyNS
    Interface I1(Of T)
        Sub Foo(ByVal i As T, ByVal j As String)
    End Interface

    Interface I2
        Inherits I1(Of String)
    End Interface

    Public Class Class1
        Implements I2

        Public Sub A2(ByVal i As String, ByVal j As String) Implements I1(Of String).Foo
            System.Console.WriteLine("{0} {1}", i, j)
        End Sub
    End Class
End Namespace

Module Module1
    Sub Main()
        Dim x As MyNS.I2 = New MyNS.Class1()
        x.Foo("hello", "world")
    End Sub
End Module
</file>
</compilation>,
    expectedOutput:="hello world")
        End Sub

        <Fact>
        Public Sub ImplementNonInterface()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="ImplementNonInterface">
       <file name="a.vb">
Option Strict On

Public Structure S1
    Public Sub foo()
    End Sub
    Public Property Zip As Integer
End Structure

Public Enum E1
    Red
    green
End Enum

Public Delegate Sub D1(ByVal x As Integer)

Public Class Class1
    Public Sub foo() Implements S1.foo
    End Sub

    Public Property zap As Integer Implements S1.Zip
    Get
        return 3
    End Get
    Set
    End Set
    End Property

    Public Sub bar() Implements E1.Red
    End Sub

    Public Sub baz() Implements System.Object.GetHashCode
    End Sub

    Public Sub quux() Implements D1.Invoke
    End Sub
End Class       
</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30232: Implemented type must be an interface.
    Public Sub foo() Implements S1.foo
                                ~~
BC30232: Implemented type must be an interface.
    Public Property zap As Integer Implements S1.Zip
                                              ~~
BC30232: Implemented type must be an interface.
    Public Sub bar() Implements E1.Red
                                ~~
BC30232: Implemented type must be an interface.
    Public Sub baz() Implements System.Object.GetHashCode
                                ~~~~~~~~~~~~~
BC30232: Implemented type must be an interface.
    Public Sub quux() Implements D1.Invoke
                                 ~~
            </expected>)
        End Sub

        <WorkItem(531308, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531308")>
        <Fact>
        Public Sub ImplementsClauseAndObsoleteAttribute()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="ImplementsClauseAndObsoleteAttribute">
       <file name="a.vb">
Imports System

Interface i1
    &lt;Obsolete("", True)&gt; Sub foo()
    &lt;Obsolete("", True)&gt; Property moo()
    &lt;Obsolete("", True)&gt; Event goo()
End Interface

Class c1
    Implements i1

    'COMPILEERROR: BC30668, "i1.foo"
    Public Sub foo() Implements i1.foo
    End Sub

    'COMPILEERROR: BC30668, "i1.moo"
    Public Property moo() As Object Implements i1.moo
        Get
            Return Nothing
        End Get
        Set(ByVal Value As Object)
        End Set
    End Property

    'COMPILEERROR: BC30668, "i1.goo"
    Public Event goo() Implements i1.goo
End Class
</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC31075: 'Sub foo()' is obsolete.
    Public Sub foo() Implements i1.foo
                     ~~~~~~~~~~~~~~~~~
BC31075: 'Property moo As Object' is obsolete.
    Public Property moo() As Object Implements i1.moo
                                    ~~~~~~~~~~~~~~~~~
BC31075: 'Event goo()' is obsolete.
    Public Event goo() Implements i1.goo
                       ~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub UnimplementedInterface()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="UnimplementedInterface">
       <file name="a.vb">
Option Strict On

Namespace MyNS
    Interface I1
        Sub Bar(ByVal x As String)
        Property Zap As Integer
    End Interface

    Public Class Class1
        Public Sub Foo(ByVal x As String) Implements I1.Bar
        End Sub
        Public Property Quuz As Integer Implements I1.Zap
    End Class
End Namespace
</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC31035: Interface 'I1' is not implemented by this class.
        Public Sub Foo(ByVal x As String) Implements I1.Bar
                                                     ~~
BC31035: Interface 'I1' is not implemented by this class.
        Public Property Quuz As Integer Implements I1.Zap
                                                   ~~
            </expected>)
        End Sub

        <Fact>
        Public Sub UnimplementedInterface2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="UnimplementedInterface2">
       <file name="a.vb">
Option Strict On

Namespace MyNS
    Interface I1
        Sub Bar(ByVal x As String)
        Property Zap As Integer
    End Interface

    Public Class Class1
        Implements I1
        Public Sub Foo(ByVal x As String) Implements I1.Bar
        End Sub
        Public Property Quuz As Integer Implements I1.Zap
    End Class

    Public Class Class2
        Inherits Class1

        Public Sub Quux(ByVal x As String) Implements I1.Bar
        End Sub
        Public Property Dingo As Integer Implements I1.Zap
    End Class
End Namespace
</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC31035: Interface 'I1' is not implemented by this class.
        Public Sub Quux(ByVal x As String) Implements I1.Bar
                                                      ~~
BC31035: Interface 'I1' is not implemented by this class.
        Public Property Dingo As Integer Implements I1.Zap
                                                    ~~ 
          </expected>)
        End Sub

        <Fact>
        Public Sub ImplementUnknownType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="ImplementUnknownType">
       <file name="a.vb">
Option Strict On

Public Class Class1
    Public Sub foo() Implements UnknownType.foo
    End Sub

    Public Sub bar() Implements System.UnknownType(Of String).bar
    End Sub

    Public Property quux As Integer Implements UnknownType.foo

    Public Property quuz As String Implements System.UnknownType(Of String).bar
    Get 
        return ""
    End Get
    Set
    End Set
    End Property
End Class
</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30002: Type 'UnknownType' is not defined.
    Public Sub foo() Implements UnknownType.foo
                                ~~~~~~~~~~~
BC30002: Type 'System.UnknownType' is not defined.
    Public Sub bar() Implements System.UnknownType(Of String).bar
                                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'UnknownType' is not defined.
    Public Property quux As Integer Implements UnknownType.foo
                                               ~~~~~~~~~~~
BC30002: Type 'System.UnknownType' is not defined.
    Public Property quuz As String Implements System.UnknownType(Of String).bar
                                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            </expected>)
        End Sub

        <Fact>
        Public Sub AmbiguousInterface_01()
            ' Somewhat surprisingly, perhaps, I3.Foo is considered ambiguous between I1.Foo(String, String) and 
            ' I2.Foo(Integer), even though only I2.Foo(String, String) matches the method arguments provided. This
            ' matches Dev10 behavior.

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="AmbiguousInterface">
       <file name="a.vb">
Option Strict On

Interface I1
    Sub Foo(ByVal i As String, ByVal j As String)
End Interface

Interface I2
    Sub Foo(ByVal x As Integer)
End Interface

Interface I3
    Inherits I1, I2
End Interface

Public Class Class1
    Implements I3

    Public Sub Foo(ByVal i As String, ByVal j As String) Implements I3.Foo
    End Sub
End Class
</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30149: Class 'Class1' must implement 'Sub Foo(x As Integer)' for interface 'I2'.
    Implements I3
               ~~
            </expected>)
        End Sub

        <Fact>
        Public Sub AmbiguousInterface_02()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="AmbiguousInterface">
       <file name="a.vb">
Option Strict On

Interface I1
    Sub Foo(ByVal i As String, ByVal j As String)
End Interface

Interface I2
    Sub Foo(ByVal i As String, ByVal j As String)
End Interface

Interface I3
    Inherits I1, I2
End Interface

Public Class Class1
    Implements I3

    Public Sub Foo(ByVal i As String, ByVal j As String) Implements I3.Foo
    End Sub
End Class
</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30149: Class 'Class1' must implement 'Sub Foo(i As String, j As String)' for interface 'I2'.
    Implements I3
               ~~
BC31040: 'Foo' exists in multiple base interfaces. Use the name of the interface that declares 'Foo' in the 'Implements' clause instead of the name of the derived interface.
    Public Sub Foo(ByVal i As String, ByVal j As String) Implements I3.Foo
                                                                    ~~~~~~
                                                                 </expected>)
        End Sub

        <Fact>
        Public Sub AmbiguousInterface_03()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="AmbiguousInterface">
       <file name="a.vb">
Option Strict On

Interface I1(Of T, S)
    Sub Foo(ByVal i As T, ByVal j As S)
    Sub Foo(ByVal i As S, ByVal j As T)
End Interface

Interface I3
    Inherits I1(Of Integer, Short), I1(Of Short, Integer)
End Interface

Public Class Class1
    Implements I3

    Public Sub Foo(ByVal i As Integer, ByVal j As Short) Implements I3.Foo
    End Sub

    Public Sub Foo(ByVal i As Short, ByVal j As Integer) Implements I3.Foo
    End Sub
End Class
</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30149: Class 'Class1' must implement 'Sub Foo(i As Integer, j As Short)' for interface 'I1(Of Short, Integer)'.
    Implements I3
               ~~
BC30149: Class 'Class1' must implement 'Sub Foo(i As Short, j As Integer)' for interface 'I1(Of Short, Integer)'.
    Implements I3
               ~~
BC31040: 'Foo' exists in multiple base interfaces. Use the name of the interface that declares 'Foo' in the 'Implements' clause instead of the name of the derived interface.
    Public Sub Foo(ByVal i As Integer, ByVal j As Short) Implements I3.Foo
                                                                    ~~~~~~
BC31040: 'Foo' exists in multiple base interfaces. Use the name of the interface that declares 'Foo' in the 'Implements' clause instead of the name of the derived interface.
    Public Sub Foo(ByVal i As Short, ByVal j As Integer) Implements I3.Foo
                                                                    ~~~~~~
                                                                 </expected>)
        End Sub

        <Fact>
        Public Sub AmbiguousInterface_04()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="AmbiguousInterface">
       <file name="a.vb">
Option Strict On

Interface I1(Of T, S)
    Sub Foo(ByVal i As T)
    Sub Foo(ByVal i As S)
End Interface

Public Class Class1
    Implements I1(Of Integer, Integer)

    Public Sub Foo(ByVal i As Integer) Implements I1(Of Integer, Integer).Foo
    End Sub
End Class
</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30149: Class 'Class1' must implement 'Sub Foo(i As Integer)' for interface 'I1(Of Integer, Integer)'.
    Implements I1(Of Integer, Integer)
               ~~~~~~~~~~~~~~~~~~~~~~~
BC30937: Member 'I1(Of Integer, Integer).Foo' that matches this signature cannot be implemented because the interface 'I1(Of Integer, Integer)' contains multiple members with this same name and signature:
   'Sub Foo(i As Integer)'
   'Sub Foo(i As Integer)'
    Public Sub Foo(ByVal i As Integer) Implements I1(Of Integer, Integer).Foo
                                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                                                 </expected>)
        End Sub

        <Fact>
        Public Sub AmbiguousInterface_05()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="AmbiguousInterface">
       <file name="a.vb">
Option Strict On

Interface I1
    Sub Foo(ByVal i As String)
End Interface

Interface I2
    Inherits I1

    Overloads Sub Foo(ByVal i As String)
End Interface

Interface I3
    Inherits I1, I2
End Interface

Interface I4
    Inherits I2, I1
End Interface

Public Class Class1
    Implements I3

    Public Sub Foo(ByVal i As String) Implements I3.Foo
    End Sub
End Class

Public Class Class2
    Implements I4

    Public Sub Foo(ByVal i As String) Implements I4.Foo
    End Sub
End Class

Public Class Class3
    Implements I3

    Public Sub Foo1(ByVal i As String) Implements I3.Foo
    End Sub

    Public Sub Foo2(ByVal i As String) Implements I1.Foo
    End Sub
End Class

Public Class Class4
    Implements I4

    Public Sub Foo1(ByVal i As String) Implements I4.Foo
    End Sub

    Public Sub Foo2(ByVal i As String) Implements I1.Foo
    End Sub
End Class

</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30149: Class 'Class1' must implement 'Sub Foo(i As String)' for interface 'I1'.
    Implements I3
               ~~
BC30149: Class 'Class2' must implement 'Sub Foo(i As String)' for interface 'I1'.
    Implements I4
               ~~
                                                                 </expected>)
        End Sub

        <Fact>
        Public Sub AmbiguousInterfaceProperty()
            ' Somewhat surprisingly, perhaps, I3.Foo is considered ambiguous between I1.Foo(String, String) and 
            ' I2.Foo(Integer), even though only I2.Foo(String, String) matches the method arguments provided. This
            ' matches Dev10 behavior.

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="AmbiguousInterfaceProperty">
       <file name="a.vb">
Option Strict On

Interface I1
    Sub Foo(ByVal i As String, ByVal j As String)
End Interface

Interface I2
    Property Foo As Integer
End Interface

Interface I3
    Inherits I1, I2
End Interface

Public Class Class1
    Implements I3

    Public Property Bar As Integer Implements I3.Foo
        Get
            Return 3
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30149: Class 'Class1' must implement 'Property Foo As Integer' for interface 'I2'.
    Implements I3
               ~~
BC30149: Class 'Class1' must implement 'Sub Foo(i As String, j As String)' for interface 'I1'.
    Implements I3
               ~~
BC31040: 'Foo' exists in multiple base interfaces. Use the name of the interface that declares 'Foo' in the 'Implements' clause instead of the name of the derived interface.
    Public Property Bar As Integer Implements I3.Foo
                                              ~~~~~~
            </expected>)
        End Sub

        <Fact>
        Public Sub NoMethodOfSig()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="NoMethodOfSig">
       <file name="a.vb">
Option Strict On

Namespace MyNS
    Interface I1
        Sub Foo(ByVal i As String, ByVal j As String)
        Function Foo(ByVal i As Long) As Integer
        Property P(i As Long) As Integer
    End Interface


    Public Class Class1
        Implements I1

        Public Sub X(ByVal i As Long) Implements I1.Foo
        End Sub

        Public Sub Y(ByRef i As String, ByVal j As String) Implements I1.Foo
        End Sub

        Public Property Z1 As Integer Implements I1.P

        Public Property Z2(i as Integer) As Integer Implements I1.P
        Get
           return 3
        End Get
        Set
        End Set
        End Property

        Public Property Z3 As Integer Implements I1.Foo
    End Class
End Namespace</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30149: Class 'Class1' must implement 'Function Foo(i As Long) As Integer' for interface 'I1'.
        Implements I1
                   ~~
BC30149: Class 'Class1' must implement 'Property P(i As Long) As Integer' for interface 'I1'.
        Implements I1
                   ~~
BC30149: Class 'Class1' must implement 'Sub Foo(i As String, j As String)' for interface 'I1'.
        Implements I1
                   ~~
BC30401: 'X' cannot implement 'Foo' because there is no matching sub on interface 'I1'.
        Public Sub X(ByVal i As Long) Implements I1.Foo
                                                 ~~~~~~
BC30401: 'Y' cannot implement 'Foo' because there is no matching sub on interface 'I1'.
        Public Sub Y(ByRef i As String, ByVal j As String) Implements I1.Foo
                                                                      ~~~~~~
BC30401: 'Z1' cannot implement 'P' because there is no matching property on interface 'I1'.
        Public Property Z1 As Integer Implements I1.P
                                                 ~~~~
BC30401: 'Z2' cannot implement 'P' because there is no matching property on interface 'I1'.
        Public Property Z2(i as Integer) As Integer Implements I1.P
                                                               ~~~~
BC30401: 'Z3' cannot implement 'Foo' because there is no matching property on interface 'I1'.
        Public Property Z3 As Integer Implements I1.Foo
                                                 ~~~~~~
            </expected>)
        End Sub

        <WorkItem(577934, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/577934")>
        <Fact>
        Public Sub Bug577934a()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation>
       <file name="a.vb">
Option Strict On

Interface I
    Sub Foo(Of T)(Optional x As T = CType(Nothing, T))
End Interface

Class C
    Implements I

    Public Sub Foo(Of T)(Optional x As T = Nothing) Implements I.Foo
    End Sub
End Class
        </file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <WorkItem(577934, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/577934")>
        <Fact>
        Public Sub Bug577934b()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation>
       <file name="a.vb">
Option Strict On

Interface I
    Sub Foo(Of T)(Optional x As T = DirectCast(Nothing, T))
End Interface

Class C
    Implements I

    Public Sub Foo(Of T)(Optional x As T = Nothing) Implements I.Foo
    End Sub
End Class
        </file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <WorkItem(577934, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/577934")>
        <Fact>
        Public Sub Bug577934c()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation>
       <file name="a.vb">
Option Strict On

Interface I
    Sub Foo(Of T As Class)(Optional x As T = TryCast(Nothing, T))
End Interface

Class C
    Implements I

    Public Sub Foo(Of T As Class)(Optional x As T = Nothing) Implements I.Foo
    End Sub
End Class
        </file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <Fact>
        Public Sub NoMethodOfName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="NoMethodOfName">
       <file name="a.vb">
Option Strict On

Namespace MyNS
    Interface I1
        Sub Foo(ByVal i As String, ByVal j As String)
    End Interface


    Public Class Class1
        Implements I1

        Public Sub Foo(ByVal i As String, ByVal j As String) Implements I1.Foo
        End Sub

        Public Sub Bar() Implements MyNS.I1.Quux
        End Sub

        Public Function Zap() As Integer Implements I1.GetHashCode
            Return 0
        End Function

        Public Property Zip As Integer Implements I1.GetHashCode

        Public ReadOnly Property Zing(x As String) As String Implements I1.Zing, I1.Foo
          Get
             return ""
          End Get
        End Property
    End Class
End Namespace
</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30401: 'Bar' cannot implement 'Quux' because there is no matching sub on interface 'I1'.
        Public Sub Bar() Implements MyNS.I1.Quux
                                    ~~~~~~~~~~~~
BC30401: 'Zap' cannot implement 'GetHashCode' because there is no matching function on interface 'I1'.
        Public Function Zap() As Integer Implements I1.GetHashCode
                                                    ~~~~~~~~~~~~~~
BC30401: 'Zip' cannot implement 'GetHashCode' because there is no matching property on interface 'I1'.
        Public Property Zip As Integer Implements I1.GetHashCode
                                                  ~~~~~~~~~~~~~~
BC30401: 'Zing' cannot implement 'Zing' because there is no matching property on interface 'I1'.
        Public ReadOnly Property Zing(x As String) As String Implements I1.Zing, I1.Foo
                                                                        ~~~~~~~
BC30401: 'Zing' cannot implement 'Foo' because there is no matching property on interface 'I1'.
        Public ReadOnly Property Zing(x As String) As String Implements I1.Zing, I1.Foo
                                                                                 ~~~~~~            
</expected>)
        End Sub

        <Fact>
        Public Sub GenericSubstitutionAmbiguity()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="GenericSubstitutionAmbiguity">
       <file name="a.vb">
Option Strict On

Namespace MyNS
    Interface I1(Of T)
        Sub Foo(ByVal i As T, ByVal j As String)
        Sub Bar(ByVal x As T)
        Sub Bar(ByVal x As String)
    End Interface

    Interface I2
        Inherits I1(Of String)

        Overloads Sub Foo(ByVal i As String, ByVal j As String)
    End Interface

    Public Class Class1
        Implements I2

        Public Sub A1(ByVal i As String, ByVal j As String) Implements I2.Foo
        End Sub

        Public Sub A2(ByVal i As String, ByVal j As String) Implements I1(Of String).Foo
        End Sub

        Public Sub B(ByVal x As String) Implements I2.Bar
        End Sub
    End Class
End Namespace
</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30149: Class 'Class1' must implement 'Sub Bar(x As String)' for interface 'I1(Of String)'.
        Implements I2
                   ~~
BC30937: Member 'I1(Of String).Bar' that matches this signature cannot be implemented because the interface 'I1(Of String)' contains multiple members with this same name and signature:
   'Sub Bar(x As String)'
   'Sub Bar(x As String)'
        Public Sub B(ByVal x As String) Implements I2.Bar
                                                   ~~~~~~
                                                            </expected>)
        End Sub

        <Fact>
        Public Sub GenericSubstitutionAmbiguityProperty()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="GenericSubstitutionAmbiguityProperty">
       <file name="a.vb">
Option Strict On

Namespace MyNS
    Interface I1(Of T)
        Property Foo As T
        Property Bar(ByVal x As T) As Integer
        Property Bar(ByVal x As String) As Integer
    End Interface

    Interface I2
        Inherits I1(Of String)
        Overloads Property Foo As String
    End Interface

    Public Class Class1
        Implements I2

        Public Property A1 As String Implements I1(Of String).Foo
            Get
                Return ""
            End Get
            Set(value As String)
            End Set
        End Property

        Public Overloads Property A2 As String Implements I2.Foo
            Get
                Return ""
            End Get
            Set(value As String)
            End Set
        End Property

        Public Property B(x As String) As Integer Implements I1(Of String).Bar
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30149: Class 'Class1' must implement 'Property Bar(x As String) As Integer' for interface 'I1(Of String)'.
        Implements I2
                   ~~
BC30937: Member 'I1(Of String).Bar' that matches this signature cannot be implemented because the interface 'I1(Of String)' contains multiple members with this same name and signature:
   'Property Bar(x As String) As Integer'
   'Property Bar(x As String) As Integer'
        Public Property B(x As String) As Integer Implements I1(Of String).Bar
                                                             ~~~~~~~~~~~~~~~~~
                                                            </expected>)
        End Sub

        <Fact>
        Public Sub InterfaceReimplementation2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="InterfaceReimplementation2">
       <file name="a.vb">
Option Strict On

Namespace MyNS
    Interface I1
        Sub Bar(ByVal x As String)
        Function Baz() As Integer
        ReadOnly Property Zip As Integer
    End Interface

    Interface I2
        Inherits I1
    End Interface

    Public Class Class1
        Implements I1

        Public Sub FooXX(ByVal x As String) Implements I1.Bar
        End Sub

        Public Function BazXX() As Integer Implements I1.Baz
            Return 0
        End Function

        Public ReadOnly Property ZipXX As Integer Implements I1.Zip
            Get
               Return 0
            End Get
        End Property
    End Class

    Public Class Class2
        Inherits Class1
        Implements I2

        Public Sub Quux(ByVal x As String) Implements I1.Bar
        End Sub

        Public Function Quux2() As Integer Implements I1.Baz
            Return 0
        End Function

        Public ReadOnly Property Quux3 As Integer Implements I1.Zip
            Get
               Return 0
            End Get
        End Property
    End Class

    Public Class Class3
        Inherits Class1
        Implements I1

        Public Sub Zap(ByVal x As String) Implements I1.Bar
        End Sub

        Public Function Zap2() As Integer Implements I1.Baz
            Return 0
        End Function

        Public ReadOnly Property Zap3 As Integer Implements I1.Zip
            Get
               Return 0
            End Get
        End Property
    End Class
End Namespace
</file>
   </compilation>)

            ' BC42015 is deprecated
            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
                                                                 </expected>)
        End Sub

        <Fact>
        Public Sub UnimplementedMembers()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
   <compilation name="UnimplementedMembers">
       <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Namespace MyNS
    Interface I1(Of T, U)
        Sub Foo(ByVal i As T, ByVal j As U)
        Function Quack(ByVal o As Integer) As Long
        ReadOnly Property Zing(i As U) As T
    End Interface

    Interface I2(Of W)
        Inherits I1(Of String, IEnumerable(Of W))
        Sub Bar(ByVal i As Long)
    End Interface

    Interface I3
        Sub Zap()
    End Interface

    Public Class Class1(Of T)
        Implements I2(Of T), I3

        Public Function Quack(ByVal o As Integer) As Long Implements I1(Of String, System.Collections.Generic.IEnumerable(Of T)).Quack
            Return 0
        End Function
    End Class
End Namespace

Module Module1
    Sub Main()
    End Sub
End Module
</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30149: Class 'Class1' must implement 'ReadOnly Property Zing(i As IEnumerable(Of T)) As String' for interface 'I1(Of String, IEnumerable(Of T))'.
        Implements I2(Of T), I3
                   ~~~~~~~~
BC30149: Class 'Class1' must implement 'Sub Bar(i As Long)' for interface 'I2(Of T)'.
        Implements I2(Of T), I3
                   ~~~~~~~~
BC30149: Class 'Class1' must implement 'Sub Foo(i As String, j As IEnumerable(Of T))' for interface 'I1(Of String, IEnumerable(Of T))'.
        Implements I2(Of T), I3
                   ~~~~~~~~
BC30149: Class 'Class1' must implement 'Sub Zap()' for interface 'I3'.
        Implements I2(Of T), I3
                             ~~
</expected>)
        End Sub

        <Fact>
        Public Sub ImplementTwice()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="ImplementTwice">
       <file name="a.vb">
Interface IFoo
    Sub Foo(x As Integer)
    WriteOnly Property Bang As Integer
End Interface

Interface IBar
    Sub Bar(x As Integer)
End Interface

Public Class Class1
    Implements IFoo, IBar

    Public Sub Foo(x As Integer) Implements IFoo.Foo

    End Sub

    Public Sub Baz(x As Integer) Implements IBar.Bar, IFoo.Foo

    End Sub

    Public WriteOnly Property A As Integer Implements IFoo.Bang
        Set
        End Set
    End Property

    Public Property B As Integer Implements IFoo.Bang
        Get
           Return 0
        End Get
        Set
        End Set
    End Property
End Class       
</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30583: 'IFoo.Foo' cannot be implemented more than once.
    Public Sub Baz(x As Integer) Implements IBar.Bar, IFoo.Foo
                                                      ~~~~~~~~
BC30583: 'IFoo.Bang' cannot be implemented more than once.
    Public Property B As Integer Implements IFoo.Bang
                                            ~~~~~~~~~
</expected>)
        End Sub

        ' DEV10 gave BC31415 for this case, but (at least for now), just use BC30401 since BC31415 is such a bad
        ' error message.
        <Fact>
        Public Sub ImplementStatic()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
   <compilation name="ImplementStatic">
       <file name="a.vb">
Public Class Class1
    Implements ContainsStatic

    Public Sub Foo() Implements ContainsStatic.Bar
    End Sub

    Public Sub Baz() Implements ContainsStatic.StaticMethod
    End Sub
End Class       
</file>
   </compilation>, {TestReferences.SymbolsTests.Interface.StaticMethodInInterface})

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30401: 'Baz' cannot implement 'StaticMethod' because there is no matching sub on interface 'ContainsStatic'.
    Public Sub Baz() Implements ContainsStatic.StaticMethod
                                ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                                          </expected>)

        End Sub

        <Fact>
        Public Sub InterfaceUnification1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="InterfaceUnification1">
       <file name="a.vb">
Imports System.Collections.Generic

Namespace Q
    Interface IFoo(Of T)
        Sub M(i As T)
    End Interface

    Interface Z(Of W)
        Inherits IFoo(Of W)
    End Interface

    Class Outer(Of X)
        Class Inner(Of Y)
            Implements IFoo(Of List(Of X)), Z(Of Y)

            Public Sub M1(i As List(Of X)) Implements IFoo(Of List(Of X)).M
            End Sub

            Public Sub M2(i As Y) Implements IFoo(Of Y).M
            End Sub
        End Class
    End Class
End Namespace
       </file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC32131: Cannot implement interface 'Z(Of Y)' because the interface 'IFoo(Of Y)' from which it inherits could be identical to implemented interface 'IFoo(Of List(Of X))' for some type arguments.
            Implements IFoo(Of List(Of X)), Z(Of Y)
                                            ~~~~~~~
                                                         </expected>)

        End Sub

        <Fact>
        Public Sub InterfaceUnification2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="InterfaceUnification2">
       <file name="a.vb">
Interface I(Of T)
End Interface

Class G1(Of T1, T2)
    Implements I(Of T1), I(Of T2)  'bad
End Class

Class G2(Of T1, T2)
    Implements I(Of Integer), I(Of T2)   'bad
End Class

Class G3(Of T1, T2)
    Implements I(Of Integer), I(Of Short)   'ok
End Class

Class G4(Of T1, T2)
    Implements I(Of I(Of T1)), I(Of T1)  'ok
End Class

Class G5(Of T1, T2)
    Implements I(Of I(Of T1)), I(Of T2)  'bad
End Class

Interface I2(Of T)
    Inherits I(Of T)
End Interface

Class G6(Of T1, T2)
    Implements I(Of T1), I2(Of T2)  'bad
End Class
       </file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC32072: Cannot implement interface 'I(Of T2)' because its implementation could conflict with the implementation of another implemented interface 'I(Of T1)' for some type arguments.
    Implements I(Of T1), I(Of T2)  'bad
                         ~~~~~~~~
BC32072: Cannot implement interface 'I(Of T2)' because its implementation could conflict with the implementation of another implemented interface 'I(Of Integer)' for some type arguments.
    Implements I(Of Integer), I(Of T2)   'bad
                              ~~~~~~~~
BC32072: Cannot implement interface 'I(Of T2)' because its implementation could conflict with the implementation of another implemented interface 'I(Of I(Of T1))' for some type arguments.
    Implements I(Of I(Of T1)), I(Of T2)  'bad
                               ~~~~~~~~
BC32131: Cannot implement interface 'I2(Of T2)' because the interface 'I(Of T2)' from which it inherits could be identical to implemented interface 'I(Of T1)' for some type arguments.
    Implements I(Of T1), I2(Of T2)  'bad
                         ~~~~~~~~~
                                                            </expected>)

        End Sub

        <Fact>
        Public Sub InterfaceUnification3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="InterfaceUnification1">
       <file name="a.vb">
Interface I(Of T, S)
End Interface

Class A(Of T, S)
    Implements I(Of I(Of T, T), T)
    Implements I(Of I(Of T, S), S)
End Class
       </file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC32072: Cannot implement interface 'I(Of I(Of T, S), S)' because its implementation could conflict with the implementation of another implemented interface 'I(Of I(Of T, T), T)' for some type arguments.
    Implements I(Of I(Of T, S), S)
               ~~~~~~~~~~~~~~~~~~~
                                                            </expected>)

        End Sub

        <Fact>
        Public Sub InterfaceUnification4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="InterfaceUnification4">
       <file name="a.vb">
Class A(Of T, S)
    Class B
        Inherits A(Of B, B)
    End Class

    Interface IA
        Sub M()
    End Interface

    Interface IB
        Inherits B.IA
        Inherits B.B.IA
    End Interface   'ok 
End Class
       </file>
   </compilation>)

            CompilationUtils.AssertNoErrors(compilation)

        End Sub

        <Fact>
        Public Sub InterfaceUnification5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="InterfaceUnification5">
       <file name="a.vb">
Option Strict On
Imports System.Collections.Generic

Class Outer(Of X, Y)
    Interface IFoo(Of T, U)
    End Interface
End Class

Class OuterFoo(Of A, B)
    Class Foo(Of C, D)
        Implements Outer(Of C, B).IFoo(Of A, D), Outer(Of D, A).IFoo(Of B, C)  'error
    End Class
    Class Bar(Of C, D)
        Implements Outer(Of C, B).IFoo(Of A, D), Outer(Of D, A).IFoo(Of B, List(Of C))  ' ok
    End Class
End Class
       </file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC32072: Cannot implement interface 'Outer(Of D, A).IFoo(Of B, C)' because its implementation could conflict with the implementation of another implemented interface 'Outer(Of C, B).IFoo(Of A, D)' for some type arguments.
        Implements Outer(Of C, B).IFoo(Of A, D), Outer(Of D, A).IFoo(Of B, C)  'error
                                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                                            </expected>)

        End Sub

        <Fact>
        Public Sub SimpleImplementationApi()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="SimpleImplementation">
        <file name="a.vb">
Option Strict On

Interface IFoo
    Sub SayItWithStyle(ByVal style As String)
    Sub SayItWithStyle(ByVal answer As Integer)
End Interface

Class Foo
    Implements IFoo

    Public Sub X(ByVal style As String) Implements IFoo.SayItWithStyle
        System.Console.WriteLine("I said: {0}", style)
    End Sub

    Public Sub Y(ByVal a As Integer) Implements IFoo.SayItWithStyle
        System.Console.WriteLine("The answer is: {0}", a)
    End Sub

    Public Overridable Sub SayItWithStyle(ByVal style As String)
        System.Console.WriteLine("You don't say: {0}", style)
    End Sub
End Class

Class Bar
    Inherits Foo

    Public Overrides Sub SayItWithStyle(ByVal style As String)
        System.Console.WriteLine("You don't say: {0}", style)
    End Sub
End Class
    </file>
    </compilation>)

            Dim globalNS = comp.GlobalNamespace
            Dim iFooType = DirectCast(globalNS.GetMembers("IFoo").First(), NamedTypeSymbol)
            Dim fooType = DirectCast(globalNS.GetMembers("Foo").First(), NamedTypeSymbol)
            Dim barType = DirectCast(globalNS.GetMembers("Bar").First(), NamedTypeSymbol)

            Dim ifooMethods = iFooType.GetMembers("SayItWithStyle").AsEnumerable().Cast(Of MethodSymbol)()
            Dim ifooTypeSayWithString = (From m In ifooMethods Where m.Parameters(0).Type.SpecialType = SpecialType.System_String).First()
            Dim ifooTypeSayWithInt = (From m In ifooMethods Where m.Parameters(0).Type.SpecialType = SpecialType.System_Int32).First()

            Dim fooX = DirectCast(fooType.GetMembers("X").First(), MethodSymbol)

            Dim fooY = DirectCast(fooType.GetMembers("Y").First(), MethodSymbol)

            Dim fooSay = DirectCast(fooType.GetMembers("SayItWithStyle").First(), MethodSymbol)

            Dim barSay = DirectCast(barType.GetMembers("SayItWithStyle").First(), MethodSymbol)

            Assert.Equal(fooY, barType.FindImplementationForInterfaceMember(ifooTypeSayWithInt))
            Assert.Equal(fooX, barType.FindImplementationForInterfaceMember(ifooTypeSayWithString))
            Assert.Equal(fooY, fooType.FindImplementationForInterfaceMember(ifooTypeSayWithInt))
            Assert.Equal(fooX, fooType.FindImplementationForInterfaceMember(ifooTypeSayWithString))

            Assert.Equal(1, fooX.ExplicitInterfaceImplementations.Length)
            Assert.Equal(ifooTypeSayWithString, fooX.ExplicitInterfaceImplementations(0))

            Assert.Equal(1, fooY.ExplicitInterfaceImplementations.Length)
            Assert.Equal(ifooTypeSayWithInt, fooY.ExplicitInterfaceImplementations(0))

            Assert.Equal(0, fooSay.ExplicitInterfaceImplementations.Length)
            Assert.Equal(0, barSay.ExplicitInterfaceImplementations.Length)

            CompilationUtils.AssertNoErrors(comp)
        End Sub

        <WorkItem(545581, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545581")>
        <Fact>
        Public Sub ImplementInterfaceMethodWithNothingDefaultValue()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ImplementInterfaceMethodWithNothingDefaultValue">
        <file name="a.vb">
Option Strict On

Interface I(Of T)
    Sub Foo(Optional x As T = Nothing)
End Interface

Class C
    Implements I(Of Integer)
    Sub Foo(Optional x As Integer = 0) Implements I(Of Integer).Foo
    End Sub
End Class

    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp, <errors></errors>)
        End Sub

        <WorkItem(545581, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545581")>
        <Fact>
        Public Sub ImplementInterfaceMethodWithNothingDefaultValue2()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ImplementInterfaceMethodWithNothingDefaultValue2">
        <file name="a.vb">
Option Strict On

Interface I(Of T)
    Sub Foo(Optional x As T = Nothing)
End Interface

Class C
    Implements I(Of Date)
    Sub Foo(Optional x As Date = Nothing) Implements I(Of Date).Foo
    End Sub
End Class

    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp, <errors></errors>)
        End Sub

        <WorkItem(545581, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545581")>
        <Fact>
        Public Sub ImplementInterfaceMethodWithNothingDefaultValue3()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation name="ImplementInterfaceMethodWithNothingDefaultValue3">
        <file name="a.vb">
Option Strict On
Imports Microsoft.VisualBasic

Interface I(Of T)
    Sub Foo(Optional x As T = Nothing)
End Interface

Class C
    Implements I(Of Char)
    Sub Foo(Optional x As Char = ChrW(0)) Implements I(Of Char).Foo
    End Sub
End Class

    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp, <errors></errors>)
        End Sub

        <WorkItem(545581, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545581")>
        <Fact>
        Public Sub ImplementInterfaceMethodWithNothingDefaultValue4()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ImplementInterfaceMethodWithNothingDefaultValue4">
        <file name="a.vb">
Option Strict On

Interface I(Of T)
    Sub Foo(Optional x As T = Nothing)
End Interface

Class C
    Implements I(Of Single)
    Sub Foo(Optional x As Single = Nothing) Implements I(Of Single).Foo
    End Sub
End Class

    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp, <errors></errors>)
        End Sub

        <WorkItem(545581, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545581")>
        <Fact>
        Public Sub ImplementInterfaceMethodWithNothingDefaultValue5()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ImplementInterfaceMethodWithNothingDefaultValue5">
        <file name="a.vb">
Option Strict On
Imports System

Interface I(Of T)
    Sub Foo(Optional x As T = Nothing)
End Interface

Class C
    Implements I(Of DayOfWeek)
    Public Sub Foo(Optional x As DayOfWeek = 0) Implements I(Of DayOfWeek).Foo
        Throw New NotImplementedException()
    End Sub
End Class
    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp, <errors></errors>)
        End Sub

        <WorkItem(545891, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545891")>
        <Fact>
        Public Sub ImplementInterfaceMethodWithNothingDefaultValue6()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ImplementInterfaceMethodWithNothingDefaultValue6">
        <file name="a.vb">
Interface I(Of T)
    Sub Foo(Optional x As T = Nothing)
End Interface

Class C
    Implements I(Of Decimal)

    Public Sub Foo(Optional x As Decimal = 0.0D) Implements I(Of Decimal).Foo
    End Sub
End Class


    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp, <errors></errors>)
        End Sub

        <WorkItem(545891, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545891")>
        <Fact>
        Public Sub ImplementInterfaceMethodWithNothingDefaultValue7()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ImplementInterfaceMethodWithNothingDefaultValue7">
        <file name="a.vb">
Interface I(Of T)
    Sub Foo(Optional x As T = Nothing)
End Interface

Class C
    Implements I(Of Double)

    Public Sub Foo(Optional x As Double = -0D) Implements I(Of Double).Foo
    End Sub
End Class


    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp, <errors></errors>)
        End Sub

        <WorkItem(545891, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545891")>
        <Fact>
        Public Sub ImplementInterfaceMethodWithNothingDefaultValue8()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ImplementInterfaceMethodWithNothingDefaultValue7">
        <file name="a.vb">
Interface I(Of T)
    Sub Foo(Optional x As T = Nothing)
End Interface

Class C
    Implements I(Of Single)

    Public Sub Foo(Optional x As Single = -0D) Implements I(Of Single).Foo
    End Sub
End Class


    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp, <errors></errors>)
        End Sub

        <WorkItem(545596, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545596")>
        <Fact>
        Public Sub ImplementInterfaceMethodWithNanDefaultValue()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ImplementInterfaceMethodWithNanDefaultValue">
        <file name="a.vb">
Option Strict On

Interface I
    Sub Foo(Optional x As Double = Double.NaN)
End Interface

Class C
    Implements I
    Sub Foo(Optional x As Double = Double.NaN) Implements I.Foo
    End Sub
End Class

    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp, <errors></errors>)
        End Sub

        <WorkItem(545596, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545596")>
        <Fact>
        Public Sub ImplementInterfaceMethodWithNanDefaultValue2()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ImplementInterfaceMethodWithNanDefaultValue2">
        <file name="a.vb">
Option Strict On

Interface I
    Sub Foo(Optional x As Single = Single.NaN)
End Interface

Class C
    Implements I
    Sub Foo(Optional x As Single = Single.NaN) Implements I.Foo
    End Sub
End Class

    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp, <errors></errors>)
        End Sub

        <WorkItem(545596, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545596")>
        <Fact>
        Public Sub ImplementInterfaceMethodWithNanDefaultValue3()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ImplementInterfaceMethodWithNanDefaultValue3">
        <file name="a.vb">
Option Strict On

Interface I
    Sub Foo(Optional x As Single = Single.NaN)
End Interface

Class C
    Implements I
    Sub Foo(Optional x As Single = Double.NaN) Implements I.Foo
    End Sub
End Class

    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp, <errors></errors>)
        End Sub

        <WorkItem(545596, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545596")>
        <Fact>
        Public Sub ImplementInterfaceMethodWithNanDefaultValue4()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ImplementInterfaceMethodWithNanDefaultValue4">
        <file name="a.vb">
Option Strict On

Interface I
    Sub Foo(Optional x As Single = Single.NaN)
End Interface

Class C
    Implements I
    Sub Foo(Optional x As Single = 0/0) Implements I.Foo
    End Sub
End Class

    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp, <errors></errors>)
        End Sub

        <WorkItem(545596, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545596")>
        <Fact>
        Public Sub ImplementInterfacePropertyWithNanDefaultValue()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ImplementInterfacePropertyWithNanDefaultValue">
        <file name="a.vb">
Option Strict On

Interface I
    ReadOnly Property Foo(Optional x As Double = Double.NaN) As Integer
End Interface

Class C
    Implements I
    Public ReadOnly Property Foo(Optional x As Double = Double.NaN) As Integer Implements I.Foo
        Get
            Return 0
        End Get
    End Property
End Class
    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp, <errors></errors>)
        End Sub

        <WorkItem(545596, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545596")>
        <Fact>
        Public Sub ImplementInterfacePropertyWithNanDefaultValue2()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ImplementInterfacePropertyWithNanDefaultValue2">
        <file name="a.vb">
Option Strict On

Interface I
    ReadOnly Property Foo(Optional x As Double = Double.NegativeInfinity) As Integer
End Interface

Class C
    Implements I
    Public ReadOnly Property Foo(Optional x As Double = Single.NegativeInfinity) As Integer Implements I.Foo
        Get
            Return 0
        End Get
    End Property
End Class
    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp, <errors></errors>)
        End Sub

        <WorkItem(545596, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545596")>
        <Fact>
        Public Sub ImplementInterfacePropertyWithNanDefaultValue3()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ImplementInterfacePropertyWithNanDefaultValue3">
        <file name="a.vb">
Option Strict On

Interface I
    ReadOnly Property Foo(Optional x As Double = Double.NegativeInfinity) As Integer
End Interface

Class C
    Implements I
    Public ReadOnly Property Foo(Optional x As Double = (-1.0)/0) As Integer Implements I.Foo
        Get
            Return 0
        End Get
    End Property
End Class
    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp, <errors></errors>)
        End Sub

        <WorkItem(545596, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545596")>
        <Fact>
        Public Sub ImplementInterfacePropertyWithNanDefaultValue4()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="ImplementInterfacePropertyWithNanDefaultValue4">
        <file name="a.vb">
Option Strict On

Interface I
    ReadOnly Property Foo(Optional x As Double = Double.NegativeInfinity) As Integer
End Interface

Class C
    Implements I
    Public ReadOnly Property Foo(Optional x As Double = 1.0/0) As Integer Implements I.Foo
        Get
            Return 0
        End Get
    End Property
End Class
    </file>
    </compilation>)

            CompilationUtils.AssertTheseDiagnostics(comp,
<errors>
BC30149: Class 'C' must implement 'ReadOnly Property Foo([x As Double = -Infinity]) As Integer' for interface 'I'.
    Implements I
               ~
BC30401: 'Foo' cannot implement 'Foo' because there is no matching property on interface 'I'.
    Public ReadOnly Property Foo(Optional x As Double = 1.0/0) As Integer Implements I.Foo
                                                                                     ~~~~~
</errors>)
        End Sub

        <Fact>
        Public Sub SimpleImplementationApiProperty()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="SimpleImplementationProperty">
        <file name="a.vb">
Option Strict On

Interface IFoo
    ReadOnly Property Style(ByVal s As String) As String
    ReadOnly Property Style(ByVal answer As Integer) As String
End Interface

Class Foo
    Implements IFoo

    Public Property X(ByVal style As String) As String Implements IFoo.Style
        Get
            Return "I said: " + style
        End Get
        Set
        End Set
    End Property

    Public ReadOnly Property Y(ByVal a As Integer) As String Implements IFoo.Style
        Get
            Return "I said: " + a.ToString()
        End Get
    End Property

    Public Overridable ReadOnly Property Style(ByVal s As String) As String
        Get
            Return "You dont say: " + s
        End Get
    End Property
End Class

Class Bar
    Inherits Foo

    Public Overrides ReadOnly Property Style(ByVal s As String) As String
        Get
            Return "I dont say: " + s
        End Get
    End Property
End Class
    </file>
    </compilation>)

            Dim globalNS = comp.GlobalNamespace
            Dim iFooType = DirectCast(globalNS.GetMembers("IFoo").First(), NamedTypeSymbol)
            Dim fooType = DirectCast(globalNS.GetMembers("Foo").First(), NamedTypeSymbol)
            Dim barType = DirectCast(globalNS.GetMembers("Bar").First(), NamedTypeSymbol)

            Dim ifooProps = iFooType.GetMembers("Style").AsEnumerable().Cast(Of PropertySymbol)()
            Dim ifooTypeStyleWithString = (From m In ifooProps Where m.Parameters(0).Type.SpecialType = SpecialType.System_String).First()
            Dim ifooTypeStyleWithStringGetter = ifooTypeStyleWithString.GetMethod
            Dim ifooTypeStyleWithInt = (From m In ifooProps Where m.Parameters(0).Type.SpecialType = SpecialType.System_Int32).First()
            Dim ifooTypeStyleWithIntGetter = ifooTypeStyleWithInt.GetMethod

            Dim fooX = DirectCast(fooType.GetMembers("X").First(), PropertySymbol)
            Dim fooXGetter = fooX.GetMethod
            Dim fooXSetter = fooX.SetMethod

            Dim fooY = DirectCast(fooType.GetMembers("Y").First(), PropertySymbol)
            Dim fooYGetter = fooY.GetMethod

            Dim fooStyle = DirectCast(fooType.GetMembers("Style").First(), PropertySymbol)
            Dim fooStyleGetter = fooStyle.GetMethod

            Dim barStyle = DirectCast(barType.GetMembers("Style").First(), PropertySymbol)
            Dim barStyleGetter = barStyle.GetMethod

            Assert.Equal(fooY, barType.FindImplementationForInterfaceMember(ifooTypeStyleWithInt))
            Assert.Equal(fooYGetter, barType.FindImplementationForInterfaceMember(ifooTypeStyleWithIntGetter))
            Assert.Equal(fooX, barType.FindImplementationForInterfaceMember(ifooTypeStyleWithString))
            Assert.Equal(fooXGetter, barType.FindImplementationForInterfaceMember(ifooTypeStyleWithStringGetter))
            Assert.Equal(fooY, fooType.FindImplementationForInterfaceMember(ifooTypeStyleWithInt))
            Assert.Equal(fooYGetter, fooType.FindImplementationForInterfaceMember(ifooTypeStyleWithIntGetter))
            Assert.Equal(fooX, fooType.FindImplementationForInterfaceMember(ifooTypeStyleWithString))
            Assert.Equal(fooXGetter, fooType.FindImplementationForInterfaceMember(ifooTypeStyleWithStringGetter))

            Assert.Equal(1, fooX.ExplicitInterfaceImplementations.Length)
            Assert.Equal(1, fooXGetter.ExplicitInterfaceImplementations.Length)
            Assert.Equal(0, fooXSetter.ExplicitInterfaceImplementations.Length)
            Assert.Equal(ifooTypeStyleWithString, fooX.ExplicitInterfaceImplementations(0))
            Assert.Equal(ifooTypeStyleWithStringGetter, fooXGetter.ExplicitInterfaceImplementations(0))

            Assert.Equal(1, fooY.ExplicitInterfaceImplementations.Length)
            Assert.Equal(1, fooYGetter.ExplicitInterfaceImplementations.Length)
            Assert.Equal(ifooTypeStyleWithInt, fooY.ExplicitInterfaceImplementations(0))
            Assert.Equal(ifooTypeStyleWithIntGetter, fooYGetter.ExplicitInterfaceImplementations(0))

            Assert.Equal(0, fooStyle.ExplicitInterfaceImplementations.Length)
            Assert.Equal(0, barStyle.ExplicitInterfaceImplementations.Length)
            Assert.Equal(0, fooStyleGetter.ExplicitInterfaceImplementations.Length)
            Assert.Equal(0, barStyleGetter.ExplicitInterfaceImplementations.Length)

            CompilationUtils.AssertNoErrors(comp)
        End Sub

        <Fact>
        Public Sub UnimplementedMethods()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="SimpleImplementation">
        <file name="a.vb">
Option Strict On

Interface IFoo
    Sub SayItWithStyle(ByVal style As String)
    Sub SayItWithStyle(ByVal answer As Integer)
    Sub SayItWithStyle(ByVal answer As Long)
End Interface

Class Foo
    Implements IFoo

    Public Sub Y(ByVal a As Integer) Implements IFoo.SayItWithStyle
        System.Console.WriteLine("The answer is: {0}", a)
    End Sub

    Public Overridable Sub SayItWithStyle(ByVal answer As Long)
        System.Console.WriteLine("You don't say: {0}", answer)
    End Sub
End Class

Class Bar
    Inherits Foo
    Implements IFoo

    Public Overrides Sub SayItWithStyle(ByVal answer As Long)
        System.Console.WriteLine("You don't say: {0}", answer)
    End Sub

    Public Sub X(ByVal style As String) Implements IFoo.SayItWithStyle
        System.Console.WriteLine("I said: {0}", style)
    End Sub
End Class
    </file>
    </compilation>)

            Dim globalNS = comp.GlobalNamespace
            Dim iFooType = DirectCast(globalNS.GetMembers("IFoo").First(), NamedTypeSymbol)
            Dim fooType = DirectCast(globalNS.GetMembers("Foo").First(), NamedTypeSymbol)
            Dim barType = DirectCast(globalNS.GetMembers("Bar").First(), NamedTypeSymbol)

            Dim ifooMethods = iFooType.GetMembers("SayItWithStyle").AsEnumerable().Cast(Of MethodSymbol)()
            Dim ifooTypeSayWithString = (From m In ifooMethods Where m.Parameters(0).Type.SpecialType = SpecialType.System_String).First()
            Dim ifooTypeSayWithInt = (From m In ifooMethods Where m.Parameters(0).Type.SpecialType = SpecialType.System_Int32).First()
            Dim ifooTypeSayWithLong = (From m In ifooMethods Where m.Parameters(0).Type.SpecialType = SpecialType.System_Int64).First()

            Dim barX = DirectCast(barType.GetMembers("X").First(), MethodSymbol)
            Dim fooY = DirectCast(fooType.GetMembers("Y").First(), MethodSymbol)
            Dim fooSay = DirectCast(fooType.GetMembers("SayItWithStyle").First(), MethodSymbol)
            Dim barSay = DirectCast(barType.GetMembers("SayItWithStyle").First(), MethodSymbol)

            Assert.Equal(fooY, barType.FindImplementationForInterfaceMember(ifooTypeSayWithInt))
            Assert.Equal(barX, barType.FindImplementationForInterfaceMember(ifooTypeSayWithString))
            Assert.Equal(fooY, fooType.FindImplementationForInterfaceMember(ifooTypeSayWithInt))
            Assert.Null(fooType.FindImplementationForInterfaceMember(ifooTypeSayWithString))
            Assert.Null(fooType.FindImplementationForInterfaceMember(ifooTypeSayWithLong))
            Assert.Null(barType.FindImplementationForInterfaceMember(ifooTypeSayWithLong))

            Assert.Equal(1, barX.ExplicitInterfaceImplementations.Length)
            Assert.Equal(ifooTypeSayWithString, barX.ExplicitInterfaceImplementations(0))

            Assert.Equal(1, fooY.ExplicitInterfaceImplementations.Length)
            Assert.Equal(ifooTypeSayWithInt, fooY.ExplicitInterfaceImplementations(0))

            Assert.Equal(0, fooSay.ExplicitInterfaceImplementations.Length)
            Assert.Equal(0, barSay.ExplicitInterfaceImplementations.Length)

            CompilationUtils.AssertTheseDiagnostics(comp, <expected>
BC30149: Class 'Foo' must implement 'Sub SayItWithStyle(answer As Long)' for interface 'IFoo'.
    Implements IFoo
               ~~~~
BC30149: Class 'Foo' must implement 'Sub SayItWithStyle(style As String)' for interface 'IFoo'.
    Implements IFoo
               ~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub UnimplementedProperties()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UnimplementedProperties">
        <file name="a.vb">
Option Strict On

Interface IFoo
    ReadOnly Property Style(ByVal s As String) As String
    ReadOnly Property Style(ByVal answer As Integer) As String
    ReadOnly Property Style(ByVal answer As Long) As String
End Interface

Class Foo
    Implements IFoo

    Public ReadOnly Property Y(ByVal a As Integer) As String Implements IFoo.Style
        Get
            Return "I said: " + a.ToString()
        End Get
    End Property

    Public Overridable ReadOnly Property Style(ByVal s As Long) As String
        Get
            Return "You dont say: " 
        End Get
    End Property
End Class

Class Bar
    Inherits Foo
    Implements IFoo

    Public Overrides ReadOnly Property Style(ByVal s As Long) As String
        Get
            Return "You dont say: "
        End Get
    End Property

    Public ReadOnly Property X(ByVal a As String) As String Implements IFoo.Style
        Get
            Return "I said: " + a.ToString()
        End Get
    End Property
End Class    
</file>
    </compilation>)

            Dim globalNS = comp.GlobalNamespace
            Dim iFooType = DirectCast(globalNS.GetMembers("IFoo").First(), NamedTypeSymbol)
            Dim fooType = DirectCast(globalNS.GetMembers("Foo").First(), NamedTypeSymbol)
            Dim barType = DirectCast(globalNS.GetMembers("Bar").First(), NamedTypeSymbol)

            Dim ifooProps = iFooType.GetMembers("Style").AsEnumerable().Cast(Of PropertySymbol)()
            Dim ifooTypeStyleWithString = (From m In ifooProps Where m.Parameters(0).Type.SpecialType = SpecialType.System_String).First()
            Dim ifooTypeStyleWithStringGetter = ifooTypeStyleWithString.GetMethod
            Dim ifooTypeStyleWithInt = (From m In ifooProps Where m.Parameters(0).Type.SpecialType = SpecialType.System_Int32).First()
            Dim ifooTypeStyleWithIntGetter = ifooTypeStyleWithInt.GetMethod
            Dim ifooTypeStyleWithLong = (From m In ifooProps Where m.Parameters(0).Type.SpecialType = SpecialType.System_Int64).First()
            Dim ifooTypeStyleWithLongGetter = ifooTypeStyleWithLong.GetMethod

            Dim barX = DirectCast(barType.GetMembers("X").First(), PropertySymbol)
            Dim barXGetter = barX.GetMethod
            Dim fooY = DirectCast(fooType.GetMembers("Y").First(), PropertySymbol)
            Dim fooYGetter = fooY.GetMethod
            Dim fooStyle = DirectCast(fooType.GetMembers("Style").First(), PropertySymbol)
            Dim fooStyleGetter = fooStyle.GetMethod
            Dim barStyle = DirectCast(barType.GetMembers("Style").First(), PropertySymbol)
            Dim barStyleGetter = barStyle.GetMethod

            Assert.Equal(fooY, barType.FindImplementationForInterfaceMember(ifooTypeStyleWithInt))
            Assert.Equal(fooYGetter, barType.FindImplementationForInterfaceMember(ifooTypeStyleWithIntGetter))
            Assert.Equal(barX, barType.FindImplementationForInterfaceMember(ifooTypeStyleWithString))
            Assert.Equal(barXGetter, barType.FindImplementationForInterfaceMember(ifooTypeStyleWithStringGetter))
            Assert.Equal(fooY, fooType.FindImplementationForInterfaceMember(ifooTypeStyleWithInt))
            Assert.Equal(fooYGetter, fooType.FindImplementationForInterfaceMember(ifooTypeStyleWithIntGetter))
            Assert.Null(fooType.FindImplementationForInterfaceMember(ifooTypeStyleWithString))
            Assert.Null(fooType.FindImplementationForInterfaceMember(ifooTypeStyleWithStringGetter))
            Assert.Null(fooType.FindImplementationForInterfaceMember(ifooTypeStyleWithLong))
            Assert.Null(fooType.FindImplementationForInterfaceMember(ifooTypeStyleWithLongGetter))
            Assert.Null(barType.FindImplementationForInterfaceMember(ifooTypeStyleWithLong))
            Assert.Null(barType.FindImplementationForInterfaceMember(ifooTypeStyleWithLongGetter))

            Assert.Equal(1, barX.ExplicitInterfaceImplementations.Length)
            Assert.Equal(ifooTypeStyleWithString, barX.ExplicitInterfaceImplementations(0))
            Assert.Equal(1, barXGetter.ExplicitInterfaceImplementations.Length)
            Assert.Equal(ifooTypeStyleWithStringGetter, barXGetter.ExplicitInterfaceImplementations(0))

            Assert.Equal(1, fooY.ExplicitInterfaceImplementations.Length)
            Assert.Equal(ifooTypeStyleWithInt, fooY.ExplicitInterfaceImplementations(0))
            Assert.Equal(1, fooYGetter.ExplicitInterfaceImplementations.Length)
            Assert.Equal(ifooTypeStyleWithIntGetter, fooYGetter.ExplicitInterfaceImplementations(0))

            Assert.Equal(0, fooStyle.ExplicitInterfaceImplementations.Length)
            Assert.Equal(0, fooStyleGetter.ExplicitInterfaceImplementations.Length)
            Assert.Equal(0, barStyle.ExplicitInterfaceImplementations.Length)
            Assert.Equal(0, barStyleGetter.ExplicitInterfaceImplementations.Length)

            CompilationUtils.AssertTheseDiagnostics(comp, <expected>
BC30149: Class 'Foo' must implement 'ReadOnly Property Style(answer As Long) As String' for interface 'IFoo'.
    Implements IFoo
               ~~~~
BC30149: Class 'Foo' must implement 'ReadOnly Property Style(s As String) As String' for interface 'IFoo'.
    Implements IFoo
               ~~~~
                                                     </expected>)
        End Sub

        <Fact>
        Public Sub UnimplementedInterfaceAPI()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="UnimplementedInterfaceAPI">
        <file name="a.vb">
Option Strict On

Interface IFoo
    Sub SayItWithStyle(ByVal a As Integer)
End Interface

Class Foo
    Public Sub Y(ByVal a As Integer) Implements IFoo.SayItWithStyle
        System.Console.WriteLine("The answer is: {0}", a)
    End Sub

    Public Overridable Sub SayItWithStyle(ByVal answer As Long)
        System.Console.WriteLine("You don't say: {0}", answer)
    End Sub
End Class
    </file>
    </compilation>)

            Dim globalNS = comp.GlobalNamespace
            Dim iFooType = DirectCast(globalNS.GetMembers("IFoo").First(), NamedTypeSymbol)
            Dim fooType = DirectCast(globalNS.GetMembers("Foo").First(), NamedTypeSymbol)

            Dim ifooMethods = iFooType.GetMembers("SayItWithStyle").AsEnumerable().Cast(Of MethodSymbol)()
            Dim ifooTypeSayWithInt = (From m In ifooMethods Where m.Parameters(0).Type.SpecialType = SpecialType.System_Int32).First()

            Dim fooY = DirectCast(fooType.GetMembers("Y").First(), MethodSymbol)

            Assert.Null(fooType.FindImplementationForInterfaceMember(ifooTypeSayWithInt))

            Assert.Equal(1, fooY.ExplicitInterfaceImplementations.Length)
            Assert.Equal(ifooTypeSayWithInt, fooY.ExplicitInterfaceImplementations(0))

            CompilationUtils.AssertTheseDiagnostics(comp, <expected>
BC31035: Interface 'IFoo' is not implemented by this class.
    Public Sub Y(ByVal a As Integer) Implements IFoo.SayItWithStyle
                                                ~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub GenericInterface()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="SimpleImplementation">
        <file name="a.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic

Class Outer(Of X)
    Interface IFoo(Of T, U)
        Sub SayItWithStyle(ByVal a As T, c As X)
        Sub SayItWithStyle(ByVal b As U, c As X)
    End Interface

    Class Foo(Of Y)
        Implements IFoo(Of X, List(Of Y))

        Public Sub M1(a As X, c As X) Implements Outer(Of X).IFoo(Of X, List(Of Y)).SayItWithStyle
        End Sub

        Public Sub M2(b As List(Of Y), c As X) Implements Outer(Of X).IFoo(Of X, List(Of Y)).SayItWithStyle
        End Sub
    End Class

    <System.Serializable>
    Class FooS(Of T, U)
    End Class
End Class
    ]]></file>
    </compilation>)

            Dim globalNS = comp.GlobalNamespace
            Dim listOfT = comp.GetTypeByMetadataName("System.Collections.Generic.List`1")
            Dim listOfString = listOfT.Construct(comp.GetSpecialType(SpecialType.System_String))

            Dim outerOfX = DirectCast(globalNS.GetMembers("Outer").First(), NamedTypeSymbol)
            Dim outerOfInt = outerOfX.Construct(comp.GetSpecialType(SpecialType.System_Int32))
            Dim iFooOfIntTU = DirectCast(outerOfInt.GetMembers("IFoo").First(), NamedTypeSymbol)
            Assert.IsType(Of SubstitutedNamedType.SpecializedGenericType)(iFooOfIntTU)
            Assert.False(DirectCast(iFooOfIntTU, INamedTypeSymbol).IsSerializable)

            Dim fooSOfIntTU = DirectCast(outerOfInt.GetMembers("FooS").First(), NamedTypeSymbol)
            Assert.IsType(Of SubstitutedNamedType.SpecializedGenericType)(fooSOfIntTU)
            Assert.True(DirectCast(fooSOfIntTU, INamedTypeSymbol).IsSerializable)

            Dim iFooOfIntIntListOfString = iFooOfIntTU.Construct(comp.GetSpecialType(SpecialType.System_Int32), listOfString)
            Dim fooOfIntY = DirectCast(outerOfInt.GetMembers("Foo").First(), NamedTypeSymbol)
            Dim fooOfIntString = fooOfIntY.Construct(comp.GetSpecialType(SpecialType.System_String))

            Dim iFooOfIntIntListOfStringMethods = iFooOfIntIntListOfString.GetMembers("SayItWithStyle").AsEnumerable().Cast(Of MethodSymbol)()
            Dim ifooOfIntIntStringSay1 = (From m In iFooOfIntIntListOfStringMethods Where TypeSymbol.Equals(m.Parameters(0).Type, comp.GetSpecialType(SpecialType.System_Int32), TypeCompareKind.ConsiderEverything)).First()
            Dim ifooOfIntIntStringSay2 = (From m In iFooOfIntIntListOfStringMethods Where Not TypeSymbol.Equals(m.Parameters(0).Type, comp.GetSpecialType(SpecialType.System_Int32), TypeCompareKind.ConsiderEverything)).First()

            Dim fooOfIntStringM1 = DirectCast(fooOfIntString.GetMembers("M1").First(), MethodSymbol)
            Dim fooOfIntStringM2 = DirectCast(fooOfIntString.GetMembers("M2").First(), MethodSymbol)

            Assert.Equal(1, fooOfIntStringM1.ExplicitInterfaceImplementations.Length)
            Assert.Equal(ifooOfIntIntStringSay1, fooOfIntStringM1.ExplicitInterfaceImplementations(0))

            Assert.Equal(1, fooOfIntStringM2.ExplicitInterfaceImplementations.Length)
            Assert.Equal(ifooOfIntIntStringSay2, fooOfIntStringM2.ExplicitInterfaceImplementations(0))

            CompilationUtils.AssertNoErrors(comp)
        End Sub

        ' See MDInterfaceMapping.cs to understand this test case.
        <Fact>
        Public Sub MetadataInterfaceMapping()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
   <compilation name="ImplementStatic">
       <file name="a.vb">
Public Class D
    Inherits C
    Implements IFoo
End Class       
</file>
   </compilation>, {TestReferences.SymbolsTests.Interface.MDInterfaceMapping})

            Dim globalNS = comp.GlobalNamespace
            Dim iFoo = DirectCast(globalNS.GetMembers("IFoo").Single(), NamedTypeSymbol)
            Dim dClass = DirectCast(globalNS.GetMembers("D").Single(), NamedTypeSymbol)
            Dim iFooMethod = DirectCast(iFoo.GetMembers("Foo").Single(), MethodSymbol)

            ' IFoo.Foo should be found in A.
            Dim implementedMethod = dClass.FindImplementationForInterfaceMember(iFooMethod)
            Assert.Equal("A", implementedMethod.ContainingType.Name)

            CompilationUtils.AssertNoErrors(comp)
        End Sub

        <Fact>
        Public Sub InterfaceReimplementation()
            CompileAndVerify(
<compilation name="InterfaceReimplementation">
    <file name="a.vb">
Imports System

Interface I1
    Sub foo()
    Sub quux()
End Interface

Interface I2
    Inherits I1
    Sub bar()
End Interface

Class X
    Implements I1

    Public Sub foo1() Implements I1.foo
        Console.WriteLine("X.foo1")
    End Sub

    Public Sub quux1() Implements I1.quux
        Console.WriteLine("X.quux1")
    End Sub

    Public Overridable Sub foo()
        Console.WriteLine("x.foo")
    End Sub
End Class

Class Y
    Inherits X
    Implements I2

    Public Overridable Sub bar()
        Console.WriteLine("Y.bar")
    End Sub

    Public Overridable Sub quux()
        Console.WriteLine("Y.quux")
    End Sub

    Public Overrides Sub foo()
        Console.WriteLine("Y.foo")
    End Sub

    Public Sub bar1() Implements I2.bar
        Console.WriteLine("Y.bar1")
    End Sub
End Class

Module Module1
    Sub Main()
        Dim a As Y = New Y()
        a.foo()
        a.bar()
        a.quux()
        Dim b As I2 = a
        b.foo()
        b.bar()
        b.quux()
    End Sub
End Module
    </file>
</compilation>,
    expectedOutput:=<![CDATA[
Y.foo
Y.bar
Y.quux
X.foo1
Y.bar1
X.quux1
]]>)
        End Sub

        <Fact>
        Public Sub Bug6095()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
   <compilation name="Bug6095">
       <file name="a.vb">
Imports System
Namespace ArExtByVal001
    Friend Module ArExtByVal001mod
        Class Cls7
             Implements I7
             Interface I7
                 Function Scen7(ByVal Ary() As Single)
             End Interface
             Function Cls7_Scen7(ByVal Ary() As Single) Implements I7.Scen7
                 Ary(70) = Ary(70) + 100
                 Return Ary(0)
             End Function
        End Class
     End Module
End Namespace

</file>
   </compilation>)

            CompilationUtils.AssertNoErrors(compilation)

        End Sub

        <Fact>
        Public Sub Bug7931()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="Bug6095">
       <file name="a.vb">
Imports System
Imports System.Collections.Generic
Namespace NS
    Interface IFoo(Of T)
        Function F(Of R)(ParamArray p As R()) As R
    End Interface
    Interface IBar(Of R)
        Function F(ParamArray p As R()) As R
    End Interface
    Class Impl
        Implements NS.IFoo(Of Char)
        Implements IBar(Of Short)
        Function F(ParamArray p As Short()) As Short Implements IFoo(Of Char).F, IBar(Of Short).F
            Return Nothing
        End Function
    End Class
End Namespace
</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30149: Class 'Impl' must implement 'Function F(Of R)(ParamArray p As R()) As R' for interface 'IFoo(Of Char)'.
        Implements NS.IFoo(Of Char)
                   ~~~~~~~~~~~~~~~~
BC30401: 'F' cannot implement 'F' because there is no matching function on interface 'IFoo(Of Char)'.
        Function F(ParamArray p As Short()) As Short Implements IFoo(Of Char).F, IBar(Of Short).F
                                                                ~~~~~~~~~~~~~~~
</expected>)

        End Sub

        <WorkItem(543664, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543664")>
        <Fact()>
        Public Sub Bug11554()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="Bug11554">
       <file name="a.vb">
Interface I
    Sub M(Optional ByRef x As Short = 1)
End Interface

Class C
    'COMPILEERROR: BC30149, "I"
    Implements I

   'COMPILEERROR: BC30401, "I.M"
    Sub M(Optional ByRef x As Short = 0) Implements I.M
    End Sub
End Class
       </file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30149: Class 'C' must implement 'Sub M([ByRef x As Short = 1])' for interface 'I'.
    Implements I
               ~
BC30401: 'M' cannot implement 'M' because there is no matching sub on interface 'I'.
    Sub M(Optional ByRef x As Short = 0) Implements I.M
                                                    ~~~
</expected>)

        End Sub

        <Fact>
        Public Sub PropAccessorAgreement()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="PropAccessorAgreement">
       <file name="a.vb">
Interface I1
    Property rw1 As Integer
    Property rw2 As Integer
    Property rw3 As Integer
    ReadOnly Property ro1 As Integer
    ReadOnly Property ro2 As Integer
    ReadOnly Property ro3 As Integer
    WriteOnly Property wo1 As Integer
    WriteOnly Property wo2 As Integer
    WriteOnly Property wo3 As Integer
End Interface

Class X
    Implements I1

    Public Property a As Integer Implements I1.ro1
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    Public ReadOnly Property b As Integer Implements I1.ro2
        Get
            Return 0
        End Get
    End Property

    Public WriteOnly Property c As Integer Implements I1.ro3
        Set(value As Integer)
        End Set
    End Property

    Public Property d As Integer Implements I1.rw1
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    Public ReadOnly Property e As Integer Implements I1.rw2
        Get
            Return 0
        End Get
    End Property

    Public WriteOnly Property f As Integer Implements I1.rw3
        Set(value As Integer)
        End Set
    End Property

    Public Property g As Integer Implements I1.wo1
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    Public ReadOnly Property h As Integer Implements I1.wo2
        Get
            Return 0
        End Get
    End Property

    Public WriteOnly Property i As Integer Implements I1.wo3
        Set(value As Integer)
        End Set
    End Property
End Class
</file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC31444: 'ReadOnly Property ro3 As Integer' cannot be implemented by a WriteOnly property.
    Public WriteOnly Property c As Integer Implements I1.ro3
                                                      ~~~~~~
BC31444: 'Property rw2 As Integer' cannot be implemented by a ReadOnly property.
    Public ReadOnly Property e As Integer Implements I1.rw2
                                                     ~~~~~~
BC31444: 'Property rw3 As Integer' cannot be implemented by a WriteOnly property.
    Public WriteOnly Property f As Integer Implements I1.rw3
                                                      ~~~~~~
BC31444: 'WriteOnly Property wo2 As Integer' cannot be implemented by a ReadOnly property.
    Public ReadOnly Property h As Integer Implements I1.wo2
                                                     ~~~~~~
                                               </expected>)
        End Sub

        <Fact>
        Public Sub ImplementDefaultProperty()
            Dim source =
<compilation>
    <file name="a.vb">
        Interface I1
            Default Property P1(param1 As Integer) As String
        End Interface
        Class C1 : Implements I1
            Private _p1 As String
            Private _p2 As String
            Public Property P1(param1 As Integer) As String Implements I1.P1
                Get
                    Return _p1
                End Get
                Set(value As String)
                    _p1 = value
                End Set
            End Property
            Default Public Property P2(param1 As Integer) As String
                Get
                    Return _p2
                End Get
                Set(value As String)
                    _p2 = value
                End Set
            End Property
        End Class
        Module Program
            Sub Main(args As String())
                Dim c As C1 = New C1()
                DirectCast(c, I1)(1) = "P1"
                c(1) = "P2"
                System.Console.WriteLine(String.Join(",", c.P1(1), c.P2(1)))
            End Sub
        End Module
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="P1,P2")
        End Sub

        <Fact>
        Public Sub ImplementReadOnlyUsingReadWriteWithPrivateSet()
            Dim source =
<compilation>
    <file name="a.vb">
Interface I1
    ReadOnly Property bar() As Integer
End Interface
Public Class C2
    Implements I1
    Public Property bar() As Integer Implements I1.bar
        Get
            Return 2
        End Get
        Private Set(ByVal value As Integer)
        End Set
    End Property
End Class
    </file>
</compilation>
            CompileAndVerify(source)
        End Sub

        <WorkItem(541934, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541934")>
        <Fact>
        Public Sub ImplementGenericInterfaceProperties()
            Dim source =
    <compilation>
        <file name="a.vb">
Imports ImportedAlias = System.Int32
Interface I1(Of T)
    ReadOnly Property P1() As T
End Interface
Public Class C1(Of T)
    Implements I1(Of T)
    Public ReadOnly Property P() As T Implements I1(Of T).P1
        Get
            Return Nothing
        End Get
    End Property
End Class
Public Class C2
    Implements I1(Of Integer)
    Public ReadOnly Property P1() As Integer Implements I1(Of Integer).P1
        Get
            Return 2
        End Get
    End Property
End Class
    </file>
    </compilation>
            CompileAndVerify(source)
        End Sub

        <Fact>
        Public Sub ImplementGenericInterfaceMethods()
            Dim source =
    <compilation>
        <file name="a.vb">
Imports System.Collections.Generic
Imports ImportedAlias = System.Int32
Interface I1(Of T)
    Sub M1()
    Function M1(Of U)(x As U, y As List(Of U), z As T, w As List(Of T)) As Dictionary(Of T, List(Of U))
End Interface
Public Class C1(Of T)
    Implements I1(Of T)
    Public Sub M() Implements I1(Of T).M1
    End Sub
    Public Function M(Of U)(x As U, y As List(Of U), z As T, w As List(Of T)) As Dictionary(Of T, List(Of U)) Implements I1(Of T).M1
        Return Nothing
    End Function
End Class
Public Class C2
    Implements I1(Of Integer)
    Public Sub M1() Implements I1(Of ImportedAlias).M1
    End Sub
    Public Function M1(Of U)(x As U, y As List(Of U), z As ImportedAlias, w As List(Of ImportedAlias)) As Dictionary(Of ImportedAlias, List(Of U)) Implements I1(Of ImportedAlias).M1
        Return Nothing
    End Function
End Class
    </file>
    </compilation>
            CompileAndVerify(source)
        End Sub

        <WorkItem(543253, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543253")>
        <Fact()>
        Public Sub ImplementMethodWithOptionalParameter()
            Dim source =
    <compilation>
        <file name="a.vb">
Public Enum E1
    A
    B
    C
End Enum

Public Interface I1
    Sub S1(i as integer, optional j as integer = 10, optional e as E1 = E1.B)
End Interface

Public Class C1
    Implements I1

    Sub C1_S1(i as integer, optional j as integer = 10, optional e as E1 = E1.B) implements I1.S1
    End Sub
End Class
    </file>
    </compilation>
            CompileAndVerify(source).VerifyDiagnostics()
        End Sub

        <Fact, WorkItem(544531, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544531")>
        Public Sub VarianceAmbiguity1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="VarianceAmbiguity1">
       <file name="a.vb">
Option Strict On
Class Animals : End Class
Class Mammals : Inherits Animals
End Class
Class Fish
    Inherits Mammals
End Class

Interface IEnumerable(Of Out T)
    Function Foo() As T
End Interface

Class C
    Implements IEnumerable(Of Fish)
    Implements IEnumerable(Of Mammals)
    Implements IEnumerable(Of Animals)

    Public Function Foo() As Fish Implements IEnumerable(Of Fish).Foo
        Return New Fish
    End Function

    Public Function Foo1() As Mammals Implements IEnumerable(Of Mammals).Foo
        Return New Mammals
    End Function

    Public Function Foo2() As Animals Implements IEnumerable(Of Animals).Foo
        Return New Animals
    End Function
End Class

       </file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC42333: Interface 'IEnumerable(Of Mammals)' is ambiguous with another implemented interface 'IEnumerable(Of Fish)' due to the 'In' and 'Out' parameters in 'Interface IEnumerable(Of Out T)'.
    Implements IEnumerable(Of Mammals)
               ~~~~~~~~~~~~~~~~~~~~~~~
BC42333: Interface 'IEnumerable(Of Animals)' is ambiguous with another implemented interface 'IEnumerable(Of Fish)' due to the 'In' and 'Out' parameters in 'Interface IEnumerable(Of Out T)'.
    Implements IEnumerable(Of Animals)
               ~~~~~~~~~~~~~~~~~~~~~~~
BC42333: Interface 'IEnumerable(Of Animals)' is ambiguous with another implemented interface 'IEnumerable(Of Mammals)' due to the 'In' and 'Out' parameters in 'Interface IEnumerable(Of Out T)'.
    Implements IEnumerable(Of Animals)
               ~~~~~~~~~~~~~~~~~~~~~~~
                                                         </expected>)

        End Sub

        <Fact, WorkItem(544531, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544531")>
        Public Sub VarianceAmbiguity2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="VarianceAmbiguity2">
       <file name="a.vb">
Option Strict On
Class Animals : End Class
Class Mammals : Inherits Animals
End Class
Class Fish
    Inherits Mammals
End Class


Interface IEnumerable(Of Out T)
    Function Foo() As T
End Interface

Interface EnumFish
    Inherits IEnumerable(Of Fish)
End Interface

Interface EnumAnimals
    Inherits IEnumerable(Of Animals)
End Interface

Interface I
    Inherits EnumFish, EnumAnimals, IEnumerable(Of Mammals)
End Interface

       </file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC42333: Interface 'EnumAnimals' is ambiguous with another implemented interface 'EnumFish' due to the 'In' and 'Out' parameters in 'Interface IEnumerable(Of Out T)'.
    Inherits EnumFish, EnumAnimals, IEnumerable(Of Mammals)
                       ~~~~~~~~~~~
BC42333: Interface 'IEnumerable(Of Mammals)' is ambiguous with another implemented interface 'EnumAnimals' due to the 'In' and 'Out' parameters in 'Interface IEnumerable(Of Out T)'.
    Inherits EnumFish, EnumAnimals, IEnumerable(Of Mammals)
                                    ~~~~~~~~~~~~~~~~~~~~~~~
BC42333: Interface 'IEnumerable(Of Mammals)' is ambiguous with another implemented interface 'EnumFish' due to the 'In' and 'Out' parameters in 'Interface IEnumerable(Of Out T)'.
    Inherits EnumFish, EnumAnimals, IEnumerable(Of Mammals)
                                    ~~~~~~~~~~~~~~~~~~~~~~~
                                                         </expected>)

        End Sub

        <Fact()>
        Public Sub VarianceAmbiguity3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="VarianceAmbiguity3">
       <file name="a.vb">
Option Strict On
Public Interface IFoo(Of Out T, U)
End Interface
Class X
End Class
Class Y
End Class
Class GX(Of T)
End Class
Class GY(Of T)
End Class

Class A
    ' Not conflicting: 2nd type parameter is invariant, types are different and cannot unify
    ' Dev11 reports not conflicting
    Implements IFoo(Of X, X), IFoo(Of Y, Y)
End Class

Class B(Of T)
    ' Not conflicting: 2nd type parameter is invariant, types are different and cannot unify
    ' Dev11 reports conflicting
    Implements IFoo(Of X, GX(Of Integer)), IFoo(Of Y, GY(Of Integer))
End Class

Class C(Of T, U)
    ' Conflicting: 2nd type parameter is invariant, GX(Of T) and GX(Of U) might unify
    ' Dev11 reports conflicting
    Implements IFoo(Of X, GX(Of T)), IFoo(Of Y, GX(Of U))
End Class

Class D(Of T, U)
    ' Conflicting: 2nd type parameter is invariant, T and U might unify
    ' E.g., If D(Of String, String) is cast to IFoo(Of Object, String), its implementation is ambiguous.
    ' Dev11 reports non-conflicting
    Implements IFoo(Of X, T), IFoo(Of Y, U)
End Class
       </file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC42333: Interface 'IFoo(Of Y, GX(Of U))' is ambiguous with another implemented interface 'IFoo(Of X, GX(Of T))' due to the 'In' and 'Out' parameters in 'Interface IFoo(Of Out T, U)'.
    Implements IFoo(Of X, GX(Of T)), IFoo(Of Y, GX(Of U))
                                     ~~~~~~~~~~~~~~~~~~~~
BC42333: Interface 'IFoo(Of Y, U)' is ambiguous with another implemented interface 'IFoo(Of X, T)' due to the 'In' and 'Out' parameters in 'Interface IFoo(Of Out T, U)'.
    Implements IFoo(Of X, T), IFoo(Of Y, U)
                              ~~~~~~~~~~~~~
                                                            </expected>)

        End Sub

        <Fact()>
        Public Sub VarianceAmbiguity4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="VarianceAmbiguity4">
       <file name="a.vb">
Option Strict On
Public Interface IFoo(Of Out T, Out U)
End Interface
Class X
End Class
Class Y
End Class
Class GX(Of T)
End Class
Class GY(Of T)
End Class
Structure S(Of T)
End Structure

Class A
    ' Not conflicting: 2nd type parameter is "Out", types can't unify and one is value type
    ' Dev11 reports not conflicting
    Implements IFoo(Of X, Integer), IFoo(Of Y, Y)
End Class

Class B(Of T)
    ' Conflicting: 2nd type parameter is "Out", 2nd type parameter could unify in B(Of Integer)
    ' Dev11 reports not conflicting
    Implements IFoo(Of X, Integer), IFoo(Of Y, T)
End Class

Class C(Of T, U)
    ' Conflicting: 2nd type parameter is "Out", , S(Of T) and S(Of U) might unify
    ' Dev11 reports conflicting
    Implements IFoo(Of X, S(Of T)), IFoo(Of Y, S(Of U))
End Class

Class D(Of T, U)
    ' Not Conflicting: 2nd type parameter is "Out", String and S(Of U) can't unify and S(Of U) is a value type.
    ' Dev11 reports conflicting
    Implements IFoo(Of X, String), IFoo(Of Y, S(Of U))
End Class

Class E(Of T, U)
    ' Not Conflicting: 1st type parameter; one is Object; 2nd type parameter is "Out", S(Of T) is a value type.
    ' Dev11 reports not conflicting
    Implements IFoo(Of Object, S(Of T)), IFoo(Of X, S(Of U))
End Class
       </file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC42333: Interface 'IFoo(Of Y, T)' is ambiguous with another implemented interface 'IFoo(Of X, Integer)' due to the 'In' and 'Out' parameters in 'Interface IFoo(Of Out T, Out U)'.
    Implements IFoo(Of X, Integer), IFoo(Of Y, T)
                                    ~~~~~~~~~~~~~
BC42333: Interface 'IFoo(Of Y, S(Of U))' is ambiguous with another implemented interface 'IFoo(Of X, S(Of T))' due to the 'In' and 'Out' parameters in 'Interface IFoo(Of Out T, Out U)'.
    Implements IFoo(Of X, S(Of T)), IFoo(Of Y, S(Of U))
                                    ~~~~~~~~~~~~~~~~~~~
                                                            </expected>)

        End Sub

        <Fact()>
        Public Sub VarianceAmbiguity5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="VarianceAmbiguity5">
       <file name="a.vb">
Option Strict On
Public Interface IFoo(Of In T, In U)
End Interface
Class X
End Class
Class Y
End Class
NotInheritable Class Z
End Class
Class W
    Inherits X
End Class
Interface J
End Interface
Interface K
End Interface
Class GX(Of T)
End Class
Class GY(Of T)
End Class
Structure S(Of T)
End Structure

Class A
    ' Not conflicting: Neither X or Y derives from each other
    ' Dev11 reports conflicting
    Implements IFoo(Of X, Integer), IFoo(Of Y, Integer)
End Class

Class B
    ' Conflicting: 1st type argument could convert to type deriving from X implementing J.
    ' Dev11 reports conflicting
    Implements IFoo(Of X, Integer), IFoo(Of J, Integer)
End Class

Class C(Of T, U)
    ' Not conflicting: 1st type argument has sealed type Z.
    ' Dev11 reports conflicting
    Implements IFoo(Of J, Integer), IFoo(Of Z, Integer)
End Class

Class D(Of T, U)
    ' Not conflicting: 1st type argument has value type Integer.
    ' Dev11 reports not conflicting
    Implements IFoo(Of Integer, Integer), IFoo(Of X, Integer)
End Class

Class E
    ' Conflicting, W derives from X
    ' Dev11 reports conflicting
    Implements IFoo(Of W, Integer), IFoo(Of X, Integer)
End Class

Class F(Of T)
    ' Conflicting: X and J cause ambiguity, T and Integer could unify
    ' Dev11 reports not conflicting
    Implements IFoo(Of X, Integer), IFoo(Of J, T)
End Class

Class G(Of T)
    ' Not conflicting: X and J cause ambiguity, GX(Of T) and Integer can't unify, Integer is value type prevents ambiguity
    ' Dev11 reports conflicting
    Implements IFoo(Of X, Integer), IFoo(Of J, GX(Of T))
End Class

Class H
    ' Not conflicting, X and J cause ambiguity, neither X or Z derive from each other.
    ' Dev11 reports conflicting
    Implements IFoo(Of X, Z), IFoo(Of J, X)
End Class
       </file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC42333: Interface 'IFoo(Of J, Integer)' is ambiguous with another implemented interface 'IFoo(Of X, Integer)' due to the 'In' and 'Out' parameters in 'Interface IFoo(Of In T, In U)'.
    Implements IFoo(Of X, Integer), IFoo(Of J, Integer)
                                    ~~~~~~~~~~~~~~~~~~~
BC42333: Interface 'IFoo(Of X, Integer)' is ambiguous with another implemented interface 'IFoo(Of W, Integer)' due to the 'In' and 'Out' parameters in 'Interface IFoo(Of In T, In U)'.
    Implements IFoo(Of W, Integer), IFoo(Of X, Integer)
                                    ~~~~~~~~~~~~~~~~~~~
BC42333: Interface 'IFoo(Of J, T)' is ambiguous with another implemented interface 'IFoo(Of X, Integer)' due to the 'In' and 'Out' parameters in 'Interface IFoo(Of In T, In U)'.
    Implements IFoo(Of X, Integer), IFoo(Of J, T)
                                    ~~~~~~~~~~~~~
                                                            </expected>)

        End Sub

        <WorkItem(545863, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545863")>
        <Fact>
        Public Sub Bug14589()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation name="Bug14589">
       <file name="a.vb">
Interface I(Of T)
  Sub Foo(x As T)
End Interface
Class A(Of T)
  Class B
    Inherits A(Of B)
    Implements I(Of B.B)
  End Class
End Class
        </file>
   </compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30149: Class 'B' must implement 'Sub Foo(x As A(Of A(Of A(Of T).B).B).B)' for interface 'I(Of B)'.
    Implements I(Of B.B)
               ~~~~~~~~~
                                                            </expected>)

        End Sub

        <WorkItem(578706, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578706")>
        <Fact>
        Public Sub ImplicitImplementationSourceVsMetadata()
            Dim source1 = <![CDATA[
public interface I
{
    void Implicit();
    void Explicit();
}

public class A
{
    public void Implicit()
    {
        System.Console.WriteLine("A.Implicit");
    }

    public void Explicit()
    {
        System.Console.WriteLine("A.Explicit");
    }
}

public class B : A, I
{
}
]]>

            Dim source2 =
               <compilation name="Lib2">
                   <file name="a.vb">
Public Class C
    Inherits B

    Public Shadows Sub Implicit()
        System.Console.WriteLine("C.Implicit")
    End Sub

    Public Shadows Sub Explicit()
        System.Console.WriteLine("C.Explicit")
    End Sub
End Class

Public Class D
    Inherits C
    Implements I

    Public Shadows Sub Explicit() Implements I.Explicit
        System.Console.WriteLine("D.Explicit")
    End Sub
End Class
                    </file>
               </compilation>

            Dim source3 =
               <compilation name="Test">
                   <file name="a.vb">
Module Test
    Sub Main()
        Dim a As New A()
        Dim b As New B()
        Dim c As New C()
        Dim d As New D()

        a.Implicit()
        b.Implicit()
        c.Implicit()
        d.Implicit()

        System.Console.WriteLine()

        a.Explicit()
        b.Explicit()
        c.Explicit()
        d.Explicit()

        System.Console.WriteLine()

        Dim i As I

        'i = a
        'i.Implicit()
        'i.Explicit()

        i = b
        i.Implicit()
        i.Explicit()

        i = c
        i.Implicit()
        i.Explicit()

        i = d
        i.Implicit()
        i.Explicit()
    End Sub
End Module
                    </file>
               </compilation>

            Dim expectedOutput = <![CDATA[
A.Implicit
A.Implicit
C.Implicit
C.Implicit

A.Explicit
A.Explicit
C.Explicit
D.Explicit

A.Implicit
A.Explicit
A.Implicit
A.Explicit
A.Implicit
D.Explicit
]]>

            Dim comp1 = CreateCSharpCompilation("Lib1", source1)
            Dim ref1a = comp1.EmitToImageReference()
            Dim ref1b = comp1.EmitToImageReference()

            Dim comp2 = CreateCompilationWithMscorlib40AndReferences(source2, {ref1a})
            Dim ref2Metadata = comp2.EmitToImageReference()
            Dim ref2Source = New VisualBasicCompilationReference(comp2)

            Dim verifyComp3 As Action(Of MetadataReference, MetadataReference) =
                Sub(ref1, ref2)
                    Dim comp3 = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source3, {ref1, ref2}, TestOptions.ReleaseExe)
                    CompileAndVerify(comp3, expectedOutput:=expectedOutput)

                    Dim globalNamespace = comp3.GlobalNamespace

                    Dim typeI = globalNamespace.GetMember(Of NamedTypeSymbol)("I")
                    Dim typeA = globalNamespace.GetMember(Of NamedTypeSymbol)("A")
                    Dim typeB = globalNamespace.GetMember(Of NamedTypeSymbol)("B")
                    Dim typeC = globalNamespace.GetMember(Of NamedTypeSymbol)("C")
                    Dim typeD = globalNamespace.GetMember(Of NamedTypeSymbol)("D")

                    Dim interfaceImplicit = typeI.GetMember(Of MethodSymbol)("Implicit")
                    Dim interfaceExplicit = typeI.GetMember(Of MethodSymbol)("Explicit")

                    Assert.Null(typeA.FindImplementationForInterfaceMember(interfaceImplicit))
                    Assert.Null(typeA.FindImplementationForInterfaceMember(interfaceExplicit))

                    Assert.Equal(typeA, typeB.FindImplementationForInterfaceMember(interfaceImplicit).ContainingType)
                    Assert.Equal(typeA, typeB.FindImplementationForInterfaceMember(interfaceExplicit).ContainingType)

                    Assert.Equal(typeA, typeC.FindImplementationForInterfaceMember(interfaceImplicit).ContainingType)
                    Assert.Equal(typeA, typeC.FindImplementationForInterfaceMember(interfaceExplicit).ContainingType)

                    Assert.Equal(typeA, typeD.FindImplementationForInterfaceMember(interfaceImplicit).ContainingType)
                    Assert.Equal(typeD, typeD.FindImplementationForInterfaceMember(interfaceExplicit).ContainingType)

                    ' In metadata, D does not declare that it implements I.
                    Assert.Equal(If(TypeOf ref2 Is MetadataImageReference, 0, 1), typeD.Interfaces.Length)
                End Sub

            ' Normal
            verifyComp3(ref1a, ref2Metadata)
            verifyComp3(ref1a, ref2Source)

            ' Retargeting
            verifyComp3(ref1b, ref2Metadata)
            verifyComp3(ref1b, ref2Source)
        End Sub

        <WorkItem(578746, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578746")>
        <Fact>
        Public Sub Bug578746a()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation>
       <file name="a.vb">
Interface I(Of T)
    Sub Foo(Optional x As Integer = Nothing)
End Interface

Class C
    Implements I(Of Integer)

    Public Sub Foo(Optional x As Integer = 0) Implements I(Of Integer).Foo
    End Sub
End Class
        </file>
   </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <WorkItem(578746, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578746")>
        <Fact>
        Public Sub Bug578746b()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation>
       <file name="a.vb">
Interface I
    Sub Foo(Optional x As Decimal = Nothing)
End Interface

Class C
    Implements I

    Public Sub Foo(Optional x As Decimal = 0.0f) Implements I.Foo
    End Sub
End Class
        </file>
   </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <WorkItem(578746, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578746")>
        <Fact>
        Public Sub Bug578746c()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation>
       <file name="a.vb">
Interface I
    Sub Foo(Optional x As Object = Nothing)
End Interface

Class C
    Implements I

    Public Sub Foo(Optional x As Object = 0) Implements I.Foo
    End Sub
End Class
        </file>
   </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30149: Class 'C' must implement 'Sub Foo([x As Object = Nothing])' for interface 'I'.
    Implements I
               ~
BC30401: 'Foo' cannot implement 'Foo' because there is no matching sub on interface 'I'.
    Public Sub Foo(Optional x As Object = 0) Implements I.Foo
                                                        ~~~~~
</expected>)
        End Sub

        <WorkItem(578746, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578746")>
        <Fact>
        Public Sub Bug578746d()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation>
       <file name="a.vb">
Class A : End Class

Interface I
    Sub Foo(Of T As A)(Optional x As A = DirectCast(Nothing, T))
End Interface

Class C: Implements I
    Public Sub Foo(Of T As A)(Optional x As A = Nothing) Implements I.Foo
    End Sub
End Class
        </file>
   </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <WorkItem(578074, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578074")>
        <Fact>
        Public Sub Bug578074()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
   <compilation>
       <file name="a.vb">
Interface I
    Sub Foo(Optional x As Object = 1D)
End Interface

Class C
    Implements I
    Public Sub Foo(Optional x As Object = 1.0D) Implements I.Foo
    End Sub
End Class
        </file>
   </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
        End Sub

        <WorkItem(608228, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608228")>
        <Fact>
        Public Sub ImplementPropertyWithByRefParameter()
            Dim il = <![CDATA[
.class interface public abstract auto ansi IRef
{
  .method public newslot specialname abstract strict virtual 
          instance string  get_P(int32& x) cil managed
  {
  } // end of method IRef::get_P

  .method public newslot specialname abstract strict virtual 
          instance void  set_P(int32& x,
                               string Value) cil managed
  {
  } // end of method IRef::set_P

  .property instance string P(int32&)
  {
    .get instance string IRef::get_P(int32&)
    .set instance void IRef::set_P(int32&,
                                             string)
  } // end of property IRef::P
} // end of class IRef
]]>

            Dim source =
               <compilation>
                   <file name="a.vb">
Public Class Impl
    Implements IRef

    Public Property P(x As Integer) As String Implements IRef.P
        Get
            Return Nothing
        End Get
        Set(value As String)

        End Set
    End Property
End Class
                    </file>
               </compilation>

            Dim compilation = CreateCompilationWithCustomILSource(source, il)

            ' CONSIDER: Dev11 doesn't report ERR_UnimplementedMember3.
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_IdentNotMemberOfInterface4, "IRef.P").WithArguments("P", "P", "property", "IRef"),
                Diagnostic(ERRID.ERR_UnimplementedMember3, "IRef").WithArguments("Class", "Impl", "Property P(ByRef x As Integer) As String", "IRef"))

            Dim globalNamespace = compilation.GlobalNamespace

            Dim interfaceType = globalNamespace.GetMember(Of NamedTypeSymbol)("IRef")
            Dim interfaceProperty = interfaceType.GetMember(Of PropertySymbol)("P")

            Assert.True(interfaceProperty.Parameters.Single().IsByRef)

            Dim classType = globalNamespace.GetMember(Of NamedTypeSymbol)("Impl")
            Dim classProperty = classType.GetMember(Of PropertySymbol)("P")

            Assert.False(classProperty.Parameters.Single().IsByRef)

            Assert.Null(classType.FindImplementationForInterfaceMember(interfaceProperty))
        End Sub

        <WorkItem(718115, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718115")>
        <Fact>
        Public Sub ExplicitlyImplementedAccessorsWithoutEvent()
            Dim il = <![CDATA[
.class interface public abstract auto ansi I
{
  .method public hidebysig newslot specialname abstract virtual 
          instance void  add_E(class [mscorlib]System.Action 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance void  remove_E(class [mscorlib]System.Action 'value') cil managed
  {
  }

  .event [mscorlib]System.Action E
  {
    .addon instance void I::add_E(class [mscorlib]System.Action)
    .removeon instance void I::remove_E(class [mscorlib]System.Action)
  }
} // end of class I


.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
       implements I
{
  .method private hidebysig newslot specialname virtual final 
          instance void  I.add_E(class [mscorlib]System.Action 'value') cil managed
  {
    .override I::add_E
    ldstr      "Explicit implementation"
    call       void [mscorlib]System.Console::WriteLine(string)
    ret
  }

  .method private hidebysig newslot specialname virtual final 
          instance void  I.remove_E(class [mscorlib]System.Action 'value') cil managed
  {
    .override I::remove_E
    ret
  }

  .method family hidebysig specialname instance void 
          add_E(class [mscorlib]System.Action 'value') cil managed
  {
    ldstr      "Protected event"
    call       void [mscorlib]System.Console::WriteLine(string)
    ret
  }

  .method family hidebysig specialname instance void 
          remove_E(class [mscorlib]System.Action 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  // NOTE: No event I.E

  .event [mscorlib]System.Action E
  {
    .addon instance void Base::add_E(class [mscorlib]System.Action)
    .removeon instance void Base::remove_E(class [mscorlib]System.Action)
  }
} // end of class Base
]]>

            Dim source =
               <compilation>
                   <file name="a.vb">
Public Class Derived
    Inherits Base
    Implements I

    Public Sub Test()
        AddHandler DirectCast(Me, I).E, Nothing
    End Sub
End Class

Module Program
    Sub Main()
        Dim d As New Derived()
        d.Test()

        Dim id As I = d
        AddHandler id.E, Nothing
    End Sub
End Module
                    </file>
               </compilation>

            Dim ilRef = CompileIL(il.Value)
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, {ilRef}, TestOptions.ReleaseExe)
            CompileAndVerify(compilation, expectedOutput:=<![CDATA[
Explicit implementation
Explicit implementation
]]>)

            Dim [global] = compilation.GlobalNamespace
            Dim [interface] = [global].GetMember(Of NamedTypeSymbol)("I")
            Dim baseType = [global].GetMember(Of NamedTypeSymbol)("Base")
            Dim derivedType = [global].GetMember(Of NamedTypeSymbol)("Derived")

            Dim interfaceEvent = [interface].GetMember(Of EventSymbol)("E")
            Dim interfaceAdder = interfaceEvent.AddMethod

            Dim baseAdder = baseType.GetMembers().OfType(Of MethodSymbol)().
                Where(Function(m) m.ExplicitInterfaceImplementations.Any()).
                Single(Function(m) m.ExplicitInterfaceImplementations.Single().MethodKind = MethodKind.EventAdd)

            Assert.Equal(baseAdder, derivedType.FindImplementationForInterfaceMember(interfaceAdder))
            Assert.Equal(baseAdder, baseType.FindImplementationForInterfaceMember(interfaceAdder))

            Assert.Null(derivedType.FindImplementationForInterfaceMember(interfaceEvent))
            Assert.Null(baseType.FindImplementationForInterfaceMember(interfaceEvent))
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_01()
            Dim ilSource = <![CDATA[
.class interface public abstract auto ansi I1
{
  .method public newslot abstract strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) []  M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  } // end of method I1::M1

  .method public newslot abstract strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] x) cil managed
  {
  } // end of method I1::M2

} // end of class I1
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim v As I1 = New Implementation()

        v.M1(Nothing)
        v.M2(Nothing)
    End Sub
End Module

Class Implementation
    Implements I1

    Public Function M1(x As Integer) As Integer() Implements I1.M1
        System.Console.WriteLine("Implementation.M1")
        Return Nothing
    End Function

    Public Function M2(x() As Integer) As Integer Implements I1.M2
        System.Console.WriteLine("Implementation.M2")
        Return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(compilation,
            <![CDATA[
Implementation.M1
Implementation.M2
                             ]]>,
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim t = m.GlobalNamespace.GetTypeMember("Implementation")

                                                  Dim m1 = t.GetMember(Of MethodSymbol)("M1")
                                                  Assert.Equal(0, m1.ExplicitInterfaceImplementations.Length)

                                                  Dim m1_stub = t.GetMember(Of MethodSymbol)("$VB$Stub_M1")

                                                  Assert.Equal("Function Implementation.$VB$Stub_M1(x As System.Int32 modopt(System.Runtime.CompilerServices.IsLong)) As System.Int32 modopt(System.Runtime.CompilerServices.IsLong) ()", m1_stub.ToTestDisplayString())
                                                  Assert.Equal(1, m1_stub.ExplicitInterfaceImplementations.Length)
                                                  Assert.Equal("Function I1.M1(x As System.Int32 modopt(System.Runtime.CompilerServices.IsLong)) As System.Int32 modopt(System.Runtime.CompilerServices.IsLong) ()", m1_stub.ExplicitInterfaceImplementations(0).ToTestDisplayString())

                                                  Assert.Equal(Accessibility.Private, m1_stub.DeclaredAccessibility)
                                                  Assert.True(m1_stub.IsMetadataVirtual)
                                                  Assert.True(m1_stub.IsMetadataNewSlot)
                                                  Assert.False(m1_stub.IsOverridable)
                                              End Sub)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_02()
            Dim ilSource = <![CDATA[
.class interface public abstract auto ansi I1
{
  .method public newslot abstract strict virtual 
          instance !!T[]  M1<T>(!!T modopt([mscorlib]System.Runtime.CompilerServices.IsLong) & x) cil managed
  {
  } // end of method I1::M1

  .method public newslot abstract strict virtual 
          instance !!S modopt([mscorlib]System.Runtime.CompilerServices.IsLong)  M2<S>(!!S modopt([mscorlib]System.Runtime.CompilerServices.IsLong) []& x) cil managed
  {
  } // end of method I1::M2

} // end of class I1
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim v As I1 = New Implementation()
        Dim x1 As Integer = 1
        Dim x2 As Integer() = Nothing
        v.M1(Of Integer)(x1)
        System.Console.WriteLine(x1)
        v.M2(Of Integer)(x2)
        System.Console.WriteLine(x2)
    End Sub
End Module

Class Implementation
    Implements I1

    Public Function M1(Of U)(ByRef x As U) As U() Implements I1.M1
        System.Console.WriteLine("Implementation.M1")
        x = Nothing
        Return Nothing
    End Function

    Public Function M2(Of W)(ByRef x() As W) As W Implements I1.M2
        System.Console.WriteLine("Implementation.M2")
        x = New W() {}
        Return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(compilation,
            <![CDATA[
Implementation.M1
0
Implementation.M2
System.Int32[]
                             ]]>,
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim t = m.GlobalNamespace.GetTypeMember("Implementation")
                                                  Dim m1_stub = t.GetMember(Of MethodSymbol)("$VB$Stub_M1")
                                                  Assert.Equal("Function Implementation.$VB$Stub_M1(Of U)(ByRef x As U modopt(System.Runtime.CompilerServices.IsLong)) As U()", m1_stub.ToTestDisplayString())
                                              End Sub)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_03()
            Dim ilSource = <![CDATA[
.class interface public abstract auto ansi I1
{
  .method public newslot specialname abstract strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) []  get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  } // end of method I1::get_P1

  .method public newslot specialname abstract strict virtual 
          instance void modopt([mscorlib]System.Runtime.CompilerServices.IsLong)  set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)  x,
                                int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] Value) cil managed
  {
  } // end of method I1::set_P1

  .method public newslot specialname abstract strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong)  get_P2() cil managed
  {
  } // end of method I1::get_P2

  .method public newslot specialname abstract strict virtual 
          instance void modopt([mscorlib]System.Runtime.CompilerServices.IsLong)  set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) Value) cil managed
  {
  } // end of method I1::set_P2

  .property instance int32[] P1(int32)
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [] I1::get_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) )
    .set instance void modopt([mscorlib]System.Runtime.CompilerServices.IsLong) I1::set_P1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) ,
                                                      int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) [])
  } // end of property I1::P1
  .property instance int32 P2()
  {
    .get instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) I1::get_P2()
    .set instance void modopt([mscorlib]System.Runtime.CompilerServices.IsLong) I1::set_P2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) )
  } // end of property I1::P2
} // end of class I1
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim v As I1 = New Implementation()
        v.P1(Nothing) = v.P1(Nothing)
        v.P2 = 123
        System.Console.WriteLine(v.P2)
        System.Console.WriteLine(DirectCast(v, Implementation).P2)
    End Sub
End Module

Class Implementation
    Implements I1


    Public Property P1(x As Integer) As Integer() Implements I1.P1
        Get
            System.Console.WriteLine("Implementation.P1_get")
            Return Nothing
        End Get
        Set(value As Integer())
            System.Console.WriteLine("Implementation.P1_set")
        End Set
    End Property

    Public Property P2 As Integer Implements I1.P2
End Class
]]>
                    </file>
                </compilation>

            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(compilation,
            <![CDATA[
Implementation.P1_get
Implementation.P1_set
123
123
                             ]]>)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_04()
            Dim ilSource = <![CDATA[
.class interface public abstract auto ansi I1
{
  .method public newslot abstract strict virtual 
          instance int32[]  M1(int32 x) cil managed
  {
  } // end of method I1::M1

  .method public newslot abstract strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) []  M2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  } // end of method I1::M1
} // end of class I1
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim v As I1 = New Implementation()

        v.M1(Nothing)
        v.M2(Nothing)
    End Sub
End Module

Class Implementation
    Implements I1

    Public Function M12(x As Integer) As Integer() Implements I1.M1, I1.M2
        System.Console.WriteLine("Implementation.M12")
        Return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(compilation,
            <![CDATA[
Implementation.M12
Implementation.M12
                             ]]>,
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim t = m.GlobalNamespace.GetTypeMember("Implementation")

                                                  Dim m1 = t.GetMember(Of MethodSymbol)("M12")
                                                  Assert.Equal(1, m1.ExplicitInterfaceImplementations.Length)
                                                  Assert.Equal("Function I1.M1(x As System.Int32) As System.Int32()", m1.ExplicitInterfaceImplementations(0).ToTestDisplayString())

                                                  Dim m1_stub = t.GetMember(Of MethodSymbol)("$VB$Stub_M12")

                                                  Assert.Equal("Function Implementation.$VB$Stub_M12(x As System.Int32 modopt(System.Runtime.CompilerServices.IsLong)) As System.Int32 modopt(System.Runtime.CompilerServices.IsLong) ()", m1_stub.ToTestDisplayString())
                                                  Assert.Equal(1, m1_stub.ExplicitInterfaceImplementations.Length)
                                                  Assert.Equal("Function I1.M2(x As System.Int32 modopt(System.Runtime.CompilerServices.IsLong)) As System.Int32 modopt(System.Runtime.CompilerServices.IsLong) ()", m1_stub.ExplicitInterfaceImplementations(0).ToTestDisplayString())
                                              End Sub)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_05()
            Dim ilSource = <![CDATA[
.class interface public abstract auto ansi I1
{
  .method public newslot abstract strict virtual 
          instance int32[]  M1(int32 x) cil managed
  {
  } // end of method I1::M1

  .method public newslot abstract strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) []  M2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  } // end of method I1::M1
} // end of class I1
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim v As I1 = New Implementation()

        v.M1(Nothing)
        v.M2(Nothing)
    End Sub
End Module

Class Implementation
    Implements I1

    Public Function M12(x As Integer) As Integer() Implements I1.M2, I1.M1
        System.Console.WriteLine("Implementation.M12")
        Return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(compilation,
            <![CDATA[
Implementation.M12
Implementation.M12
                             ]]>,
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim t = m.GlobalNamespace.GetTypeMember("Implementation")

                                                  Dim m1 = t.GetMember(Of MethodSymbol)("M12")
                                                  Assert.Equal(1, m1.ExplicitInterfaceImplementations.Length)
                                                  Assert.Equal("Function I1.M1(x As System.Int32) As System.Int32()", m1.ExplicitInterfaceImplementations(0).ToTestDisplayString())

                                                  Dim m1_stub = t.GetMember(Of MethodSymbol)("$VB$Stub_M12")

                                                  Assert.Equal("Function Implementation.$VB$Stub_M12(x As System.Int32 modopt(System.Runtime.CompilerServices.IsLong)) As System.Int32 modopt(System.Runtime.CompilerServices.IsLong) ()", m1_stub.ToTestDisplayString())
                                                  Assert.Equal(1, m1_stub.ExplicitInterfaceImplementations.Length)
                                                  Assert.Equal("Function I1.M2(x As System.Int32 modopt(System.Runtime.CompilerServices.IsLong)) As System.Int32 modopt(System.Runtime.CompilerServices.IsLong) ()", m1_stub.ExplicitInterfaceImplementations(0).ToTestDisplayString())
                                              End Sub)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_06()
            Dim ilSource = <![CDATA[
.class interface public abstract auto ansi I1
{
  .method public newslot abstract strict virtual 
          instance int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  } // end of method I1::M1

  .method public newslot abstract strict virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) []  M2(int32 x) cil managed
  {
  } // end of method I1::M1
} // end of class I1
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim v As I1 = New Implementation()

        v.M1(Nothing)
        v.M2(Nothing)
    End Sub
End Module

Class Implementation
    Implements I1

    Public Function M12(x As Integer) As Integer() Implements I1.M2, I1.M1
        System.Console.WriteLine("Implementation.M12")
        Return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(compilation,
            <![CDATA[
Implementation.M12
Implementation.M12
                             ]]>,
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim t = m.GlobalNamespace.GetTypeMember("Implementation")

                                                  Dim m1 = t.GetMember(Of MethodSymbol)("M12")
                                                  Assert.Equal(0, m1.ExplicitInterfaceImplementations.Length)

                                                  Dim m12_stubs = t.GetMembers("$VB$Stub_M12")

                                                  Assert.Equal(2, m12_stubs.Length)

                                                  Dim m1_stub As MethodSymbol
                                                  Dim m2_stub As MethodSymbol

                                                  If DirectCast(m12_stubs(0), MethodSymbol).ReturnTypeCustomModifiers.IsEmpty Then
                                                      m2_stub = DirectCast(m12_stubs(0), MethodSymbol)
                                                      m1_stub = DirectCast(m12_stubs(1), MethodSymbol)
                                                  Else
                                                      m2_stub = DirectCast(m12_stubs(1), MethodSymbol)
                                                      m1_stub = DirectCast(m12_stubs(0), MethodSymbol)
                                                  End If

                                                  Assert.Equal("Function Implementation.$VB$Stub_M12(x As System.Int32 modopt(System.Runtime.CompilerServices.IsLong)) As System.Int32() modopt(System.Runtime.CompilerServices.IsLong)", m1_stub.ToTestDisplayString())
                                                  Assert.Equal(1, m1_stub.ExplicitInterfaceImplementations.Length)
                                                  Assert.Equal("Function I1.M1(x As System.Int32 modopt(System.Runtime.CompilerServices.IsLong)) As System.Int32() modopt(System.Runtime.CompilerServices.IsLong)", m1_stub.ExplicitInterfaceImplementations(0).ToTestDisplayString())

                                                  Assert.Equal("Function Implementation.$VB$Stub_M12(x As System.Int32) As System.Int32 modopt(System.Runtime.CompilerServices.IsLong) ()", m2_stub.ToTestDisplayString())
                                                  Assert.Equal(1, m2_stub.ExplicitInterfaceImplementations.Length)
                                                  Assert.Equal("Function I1.M2(x As System.Int32) As System.Int32 modopt(System.Runtime.CompilerServices.IsLong) ()", m2_stub.ExplicitInterfaceImplementations(0).ToTestDisplayString())
                                              End Sub)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_07()
            Dim ilSource = <![CDATA[
.class interface public abstract auto ansi I1
{
  .method public newslot abstract strict virtual 
          instance int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M1(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  } // end of method I1::M1

  .method public newslot abstract strict virtual 
          instance int32[] modopt([mscorlib]System.Runtime.CompilerServices.IsLong) M2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  } // end of method I1::M1
} // end of class I1
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim v As I1 = New Implementation()

        v.M1(Nothing)
        v.M2(Nothing)
    End Sub
End Module

Class Implementation
    Implements I1

    Public Function M12(x As Integer) As Integer() Implements I1.M2, I1.M1
        System.Console.WriteLine("Implementation.M12")
        Return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(compilation,
            <![CDATA[
Implementation.M12
Implementation.M12
                             ]]>,
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim t = m.GlobalNamespace.GetTypeMember("Implementation")

                                                  Dim m1 = t.GetMember(Of MethodSymbol)("M12")
                                                  Assert.Equal(0, m1.ExplicitInterfaceImplementations.Length)

                                                  Dim m12_stub = t.GetMember(Of MethodSymbol)("$VB$Stub_M12")
                                                  Assert.Equal(2, m12_stub.ExplicitInterfaceImplementations.Length)
                                              End Sub)
        End Sub

        <WorkItem(819295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819295")>
        <Fact>
        Public Sub CustomModifiers_08()
            Dim ilSource = <![CDATA[
.class interface public abstract auto ansi I1
{
  .method public newslot abstract strict virtual 
          instance int32[] M1(int32 & modopt([mscorlib]System.Runtime.CompilerServices.IsLong) x) cil managed
  {
  } // end of method I1::M1

  .method public newslot abstract strict virtual 
          instance int32[]  M2(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) & x) cil managed
  {
  } // end of method I1::M1
} // end of class I1
]]>.Value

            Dim vbSource =
                <compilation>
                    <file name="c.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim v As I1 = New Implementation()

        v.M1(Nothing)
        v.M2(Nothing)
    End Sub
End Module

Class Implementation
    Implements I1

    Public Function M12(ByRef x As Integer) As Integer() Implements I1.M2, I1.M1
        System.Console.WriteLine("Implementation.M12")
        Return Nothing
    End Function
End Class
]]>
                    </file>
                </compilation>

            Dim reference As MetadataReference = Nothing
            Using tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource)
                reference = MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path))
            End Using

            Dim compilation = CreateEmptyCompilationWithReferences(vbSource, {MscorlibRef, MsvbRef, reference}, TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(compilation,
            <![CDATA[
Implementation.M12
Implementation.M12
                             ]]>,
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim t = m.GlobalNamespace.GetTypeMember("Implementation")

                                                  Dim m1 = t.GetMember(Of MethodSymbol)("M12")
                                                  Assert.Equal(0, m1.ExplicitInterfaceImplementations.Length)

                                                  Dim m12_stubs = t.GetMembers("$VB$Stub_M12")

                                                  Assert.Equal(2, m12_stubs.Length)

                                                  For Each stub As MethodSymbol In m12_stubs
                                                      Assert.Equal(1, stub.ExplicitInterfaceImplementations.Length)
                                                  Next
                                              End Sub)
        End Sub

    End Class
End Namespace

