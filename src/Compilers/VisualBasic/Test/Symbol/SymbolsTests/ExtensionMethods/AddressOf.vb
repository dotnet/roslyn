' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.ExtensionMethods

    Public Class [AddressOf] : Inherits BasicTestBase

        <Fact>
        Public Sub Test1()
            Dim compilationDef =
<compilation name="Test1">
    <file name="a.vb">
Option Strict Off        

Imports System
Imports System.Console
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Test1()
        Test2()
        Test3()
        Test4()

        Dim x As New C1()
        x.Test5()
        x.Test6()

        Dim y As New S1()
        y.Test7()
        y.Test8()

        Test17()

        Test18(Of C1)(x)
        Test18(Of S1)(y)

        Test19(Of C1)(x)
        Test19(Of S1)(y)
    End Sub

    Sub Test1()
        System.Console.WriteLine("-- Test1 --")
        Dim d As Func(Of Integer) = AddressOf SideEffect1().F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test2()
        System.Console.WriteLine("-- Test2 --")
        Dim d As Func(Of Long) = AddressOf SideEffect1().F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test3()
        System.Console.WriteLine("-- Test3 --")
        Dim d As Func(Of Integer) = AddressOf SideEffect2().F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test4()
        System.Console.WriteLine("-- Test4 --")
        Dim d As Func(Of Long) = AddressOf SideEffect2().F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Public ReadOnly Property P1 As C1
        Get
            System.Console.WriteLine("P1")
            Return New C1()
        End Get
    End Property

    Sub Test17()
        System.Console.WriteLine("-- Test17 --")
        Dim d As Func(Of Integer) = AddressOf P1.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test18(Of T)(x As T)
        System.Console.WriteLine("-- Test18 --")
        Dim d As Func(Of Integer) = AddressOf x.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test19(Of T)(x As T)
        System.Console.WriteLine("-- Test19 --")
        Dim d As Func(Of Long) = AddressOf x.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Function SideEffect1() As C1
        System.Console.WriteLine("SideEffect1")
        Return New C1()
    End Function

    Function SideEffect2() As S1
        System.Console.WriteLine("SideEffect2")
        Return New S1()
    End Function

End Module


Class C1
    Public f As Integer

    Sub Test5()
        System.Console.WriteLine("-- Test5 --")
        Dim d As Func(Of Integer) = AddressOf Me.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
        System.Console.WriteLine(f)
    End Sub

    Sub Test6()
        System.Console.WriteLine("-- Test6 --")
        Dim d As Func(Of Long) = AddressOf Me.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
        System.Console.WriteLine(f)
    End Sub
End Class

Structure S1
    Public f As Integer

    Sub Test7()
        System.Console.WriteLine("-- Test7 --")
        Dim d As Func(Of Integer) = AddressOf Me.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
        System.Console.WriteLine(f)
    End Sub

    Sub Test8()
        System.Console.WriteLine("-- Test8 --")
        Dim d As Func(Of Long) = AddressOf Me.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
        System.Console.WriteLine(f)
    End Sub

End Structure

Module Extensions
    &lt;Extension()&gt;
    Function F1(this As C1) As Integer
        this.f = this.f + 1
        Return this.f
    End Function

    &lt;Extension()&gt;
    Function F1(this As S1) As Integer
        this.f = this.f - 1
        Return this.f
    End Function

    &lt;Extension()&gt;
    Function F1(Of T)(this As T) As Integer
        Dim x As C1 = TryCast(this, C1)

        If x IsNot Nothing Then
            x.f = x.f + 1
            Return x.f
        Else
            Dim y As S1 = CType(CObj(this), S1)
            y.f = y.f - 1
            Return y.f
        End If
    End Function
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             verify:=Verification.FailsILVerify,
                             expectedOutput:=
            <![CDATA[
-- Test1 --
SideEffect1
1
2
-- Test2 --
SideEffect1
1
2
-- Test3 --
SideEffect2
-1
-1
-- Test4 --
SideEffect2
-1
-1
-- Test5 --
1
2
2
-- Test6 --
3
4
4
-- Test7 --
-1
-1
0
-- Test8 --
-1
-1
0
-- Test17 --
P1
1
2
-- Test18 --
5
6
-- Test18 --
-1
-1
-- Test19 --
7
8
-- Test19 --
-1
-1
]]>)
        End Sub

        <Fact>
        Public Sub Test2()
            Dim compilationDef =
<compilation name="Test1">
    <file name="a.vb">
Option Strict Off        

Imports System
Imports System.Console
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Test1()
        Test2()
        Test3()
        Test4()

        Dim x As New C1()
        x.Test5()
        x.Test6()

        Dim y As New S1()
        y.Test7()
        y.Test8()

        Test17()

        Test18(Of C1)(x)
        System.Console.WriteLine(x.f)
        Test18(Of S1)(y)
        System.Console.WriteLine(y.f)

        Test19(Of C1)(x)
        System.Console.WriteLine(x.f)
        Test19(Of S1)(y)
        System.Console.WriteLine(y.f)

        Test21()
        Test22()
        Test23()
        Test24()
    End Sub

    Sub Test1()
        System.Console.WriteLine("-- Test1 --")
        Dim d As Func(Of Integer) = AddressOf SideEffect1().F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test2()
        System.Console.WriteLine("-- Test2 --")
        Dim d As Func(Of Long) = AddressOf SideEffect1().F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test3()
        System.Console.WriteLine("-- Test3 --")
        Dim d As Func(Of Integer) = AddressOf SideEffect2().F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test4()
        System.Console.WriteLine("-- Test4 --")
        Dim d As Func(Of Long) = AddressOf SideEffect2().F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Public Property P1 As C1
        Get
            System.Console.WriteLine("P1")
            Return New C1()
        End Get
        Set(value As C1)
            System.Console.WriteLine("SetP1!!!")
        End Set
    End Property

    Sub Test17()
        System.Console.WriteLine("-- Test17 --")
        Dim d As Func(Of Integer) = AddressOf P1.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test18(Of T)(ByRef x As T)
        System.Console.WriteLine("-- Test18 --")
        Dim d As Func(Of Integer) = AddressOf x.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub

    Sub Test19(Of T)(ByRef x As T)
        System.Console.WriteLine("-- Test19 --")
        Dim d As Func(Of Long) = AddressOf x.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
    End Sub


    Sub Test21()
        System.Console.WriteLine("-- Test21 --")
        Dim x As New C1()
        Dim d As Func(Of Integer) = AddressOf x.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
        System.Console.WriteLine(x.f)
    End Sub

    Sub Test22()
        System.Console.WriteLine("-- Test22 --")
        Dim x As New C1()
        Dim d As Func(Of Long) = AddressOf x.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
        System.Console.WriteLine(x.f)
    End Sub

    Sub Test23()
        System.Console.WriteLine("-- Test23 --")
        Dim x As New S1()
        Dim d As Func(Of Integer) = AddressOf x.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
        System.Console.WriteLine(x.f)
    End Sub

    Sub Test24()
        System.Console.WriteLine("-- Test24 --")
        Dim x As New S1()
        Dim d As Func(Of Long) = AddressOf x.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
        System.Console.WriteLine(x.f)
    End Sub

    Function SideEffect1() As C1
        System.Console.WriteLine("SideEffect1")
        Return New C1()
    End Function

    Function SideEffect2() As S1
        System.Console.WriteLine("SideEffect2")
        Return New S1()
    End Function

End Module


Class C1
    Public f As Integer

    Sub Test5()
        System.Console.WriteLine("-- Test5 --")
        Dim d As Func(Of Integer) = AddressOf Me.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
        System.Console.WriteLine(f)
    End Sub

    Sub Test6()
        System.Console.WriteLine("-- Test6 --")
        Dim d As Func(Of Long) = AddressOf Me.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
        System.Console.WriteLine(f)
    End Sub
End Class

Structure S1
    Public f As Integer

    Sub Test7()
        System.Console.WriteLine("-- Test7 --")
        Dim d As Func(Of Integer) = AddressOf Me.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
        System.Console.WriteLine(f)
    End Sub

    Sub Test8()
        System.Console.WriteLine("-- Test8 --")
        Dim d As Func(Of Long) = AddressOf Me.F1
        System.Console.WriteLine(d())
        System.Console.WriteLine(d())
        System.Console.WriteLine(f)
    End Sub

End Structure

Module Extensions
    &lt;Extension()&gt;
    Function F1(ByRef this As C1) As Integer
        Dim x As C1 = this
        x.f = x.f + 1
        Dim x1 As New C1()
        x1.f = x.f + 1000
        this = x1
        Return x.f
    End Function

    &lt;Extension()&gt;
    Function F1(ByRef this As S1) As Integer
        Dim y As S1 = this
        y.f = y.f - 1
        Dim y1 As New S1()
        y1.f = y.f - 1000
        this = y1
        Return y.f
    End Function

    &lt;Extension()&gt;
    Function F1(Of T)(ByRef this As T) As Integer
        Dim x As C1 = TryCast(this, C1)

        If x IsNot Nothing Then
            x.f = x.f + 1
            Dim x1 As New C1()
            x1.f = x.f + 1000
            this = DirectCast(CObj(x1), T)
            Return x.f
        Else
            Dim y As S1 = CType(CObj(this), S1)
            y.f = y.f - 1
            Dim y1 As New S1()
            y1.f = y.f - 1000
            this = DirectCast(CObj(y1), T)
            Return y.f
        End If
    End Function
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
-- Test1 --
SideEffect1
1
2
-- Test2 --
SideEffect1
1
2
-- Test3 --
SideEffect2
-1
-1
-- Test4 --
SideEffect2
-1
-1
-- Test5 --
1
2
2
-- Test6 --
3
4
4
-- Test7 --
-1
-1
0
-- Test8 --
-1
-1
0
-- Test17 --
P1
1
2
-- Test18 --
5
6
6
-- Test18 --
-1
-1
0
-- Test19 --
7
8
8
-- Test19 --
-1
-1
0
-- Test21 --
1
2
2
-- Test22 --
1
2
2
-- Test23 --
-1
-1
0
-- Test24 --
-1
-1
0
]]>)
        End Sub

        <Fact>
        Public Sub SubOrFunction1()
            Dim compilationDef =
<compilation name="SubOrFunction1">
    <file name="a.vb">
Option Strict Off        

Imports System
Imports System.Console
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x As New C1()
        Dim d1 As Func(Of Integer) = AddressOf x.F1
        Dim d2 As Action = AddressOf x.F1

        d1()
        d2()
    End Sub

End Module


Module M1
    &lt;Extension()&gt;
    Sub F1(this As C1)
        System.Console.WriteLine("M1.F1")
    End Sub
End Module

Module M2
    &lt;Extension()&gt;
    Function F1(this As C1) As Integer
        System.Console.WriteLine("M2.F1")
        Return 0
    End Function
End Module

Class C1
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             verify:=Verification.FailsILVerify,
                             expectedOutput:=
            <![CDATA[
M2.F1
M1.F1
]]>)
        End Sub

        <Fact>
        Public Sub SubOrFunction2()
            Dim compilationDef =
<compilation name="SubOrFunction2">
    <file name="a.vb">
Imports System
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x As New C1()
        Dim d1 As Func(Of Integer) = AddressOf x.F1
        Dim d2 As Action = AddressOf x.F1
        Dim d3 As Func(Of Integer) = AddressOf x.F2
        Dim d4 As Action = AddressOf x.F2
    End Sub

End Module


Module M1
    &lt;Extension()&gt;
    Sub F1(this As C1)
    End Sub

    &lt;Extension()&gt;
    Function F2(this As C1) As Integer
        Return 0
    End Function
End Module

Module M2
    &lt;Extension()&gt;
    Sub F1(this As C1)
    End Sub

    &lt;Extension()&gt;
    Function F2(this As C1) As Integer
        Return 0
    End Function
End Module

Class C1
End Class
Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30794: No accessible 'F1' is most specific: 
    Extension method 'Public Sub F1()' defined in 'M1'.
    Extension method 'Public Sub F1()' defined in 'M2'.
        Dim d1 As Func(Of Integer) = AddressOf x.F1
                                               ~~~~
BC30794: No accessible 'F1' is most specific: 
    Extension method 'Public Sub F1()' defined in 'M1'.
    Extension method 'Public Sub F1()' defined in 'M2'.
        Dim d2 As Action = AddressOf x.F1
                                     ~~~~
BC30794: No accessible 'F2' is most specific: 
    Extension method 'Public Function F2() As Integer' defined in 'M1'.
    Extension method 'Public Function F2() As Integer' defined in 'M2'.
        Dim d3 As Func(Of Integer) = AddressOf x.F2
                                               ~~~~
BC30794: No accessible 'F2' is most specific: 
    Extension method 'Public Function F2() As Integer' defined in 'M1'.
    Extension method 'Public Function F2() As Integer' defined in 'M2'.
        Dim d4 As Action = AddressOf x.F2
                                     ~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub BadReceiver1()
            Dim compilationDef =
<compilation name="BadReceiver1">
    <file name="a.vb">
Imports System
Imports System.Runtime.CompilerServices
Imports NS1

Module Module1
    Sub Main()
        Dim y As System.Action = AddressOf C1.Test
    End Sub
End Module

Namespace NS1
    Module Module2
        &lt;Extension()&gt;
        Sub Test(this As C1)
        End Sub
    End Module
End Namespace

Class C1

    Shared Sub Test1()
        Dim x As System.Action = AddressOf Test
    End Sub

    Sub Test2()
        Dim x As System.Action = AddressOf NS1.Test
    End Sub

    Sub Test3()
        Dim x As System.Action = AddressOf NS1.Module2.Test
    End Sub

    Sub Test4()
        Dim x As System.Action = AddressOf C1.Test
    End Sub
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30469: Reference to a non-shared member requires an object reference.
        Dim y As System.Action = AddressOf C1.Test
                                 ~~~~~~~~~~~~~~~~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
        Dim x As System.Action = AddressOf Test
                                 ~~~~~~~~~~~~~~
BC31143: Method 'Public Sub Test(this As C1)' does not have a signature compatible with delegate 'Delegate Sub Action()'.
        Dim x As System.Action = AddressOf NS1.Test
                                           ~~~~~~~~
BC31143: Method 'Public Sub Test(this As C1)' does not have a signature compatible with delegate 'Delegate Sub Action()'.
        Dim x As System.Action = AddressOf NS1.Module2.Test
                                           ~~~~~~~~~~~~~~~~
BC30469: Reference to a non-shared member requires an object reference.
        Dim x As System.Action = AddressOf C1.Test
                                 ~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub BadTarget1()
            Dim compilationDef =
<compilation name="BadReceiver1">
    <file name="a.vb">
Option Strict On

Imports System.Runtime.CompilerServices

Module Module1
    Sub Main()
        Dim x As New C1()
        Dim d1 As System.Action(Of Long) = AddressOf x.F1
        Dim d2 As System.Action(Of Long) = AddressOf x.F2
        Dim d3 As System.Action(Of Long) = AddressOf x.F3
    End Sub
End Module

Module Module2
    &lt;Extension()&gt;
    Sub F1(this As C1, y As Integer)
    End Sub

    &lt;Extension()&gt;
    Sub F2(this As C1, y As Long, z As Integer)
    End Sub

    &lt;Extension()&gt;
    Sub F3(this As C1, y As Integer)
    End Sub
End Module

Module Module3
    &lt;Extension()&gt;
    Sub F3(this As C1, y As Byte)
    End Sub
End Module

Class C1
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36709: Option Strict On does not allow narrowing in implicit type conversions between extension method 'Public Sub F1(y As Integer)' defined in 'Module2' and delegate 'Delegate Sub Action(Of Long)(obj As Long)'.
        Dim d1 As System.Action(Of Long) = AddressOf x.F1
                                                     ~~~~
BC36710: Extension Method 'Public Sub F2(y As Long, z As Integer)' defined in 'Module2' does not have a signature compatible with delegate 'Delegate Sub Action(Of Long)(obj As Long)'.
        Dim d2 As System.Action(Of Long) = AddressOf x.F2
                                                     ~~~~
BC30950: No accessible method 'F3' has a signature compatible with delegate 'Delegate Sub Action(Of Long)(obj As Long)':
    Extension method 'Public Sub F3(y As Integer)' defined in 'Module2': Argument matching parameter 'y' narrows from 'Long' to 'Integer'.
    Extension method 'Public Sub F3(y As Byte)' defined in 'Module3': Argument matching parameter 'y' narrows from 'Long' to 'Byte'.
        Dim d3 As System.Action(Of Long) = AddressOf x.F3
                                                     ~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Bug8968()
            Dim compilationDef =
<compilation name="Bug8968">
    <file name="a.vb">
Option Strict Off        

Imports System
Imports System.Console
Imports System.Runtime.CompilerServices


Interface IA(Of T)
End Interface

Interface IB
End Interface

Class C
    Implements IA(Of Integer), IB

    Sub Goo(ByVal x As Action(Of Integer))
        System.Console.WriteLine(1)
        x(1)
    End Sub

    Sub Goo(ByVal x As Action(Of String))
        System.Console.WriteLine(2)
        x("2")
    End Sub

    Sub Baz()
        Goo(AddressOf Bar)
    End Sub
End Class

Module M
    &lt;Extension()&gt;
    Sub Bar(ByVal x As C, ByVal y As String)
        System.Console.WriteLine(3)
    End Sub

    &lt;Extension()&gt;
    Sub Bar(Of T)(ByVal x As IA(Of T), ByVal y As Integer)
        System.Console.WriteLine(4)
    End Sub

    &lt;Extension()&gt;
    Sub Bar(ByVal x As IB, ByVal y As Integer)
        System.Console.WriteLine(5)
    End Sub
End Module

Module Module1

    Sub Main()
        Dim ccc As New C()
        ccc.Baz()
    End Sub

End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>
            ' ILVerify: Unrecognized arguments for delegate .ctor. { Offset = 8 }
            CompileAndVerify(compilationDef,
                             verify:=Verification.FailsILVerify,
                             expectedOutput:=
            <![CDATA[
2
3
]]>)
        End Sub

    End Class

End Namespace

