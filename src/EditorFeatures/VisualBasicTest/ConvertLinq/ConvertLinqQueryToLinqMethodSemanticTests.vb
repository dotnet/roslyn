' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.ConvertLinq

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.ConvertLinq

    Public Class ConvertLinqQueryToLinqMethodSemanticTests
        Inherits AbstractVisualBasicConvertLinqTest

        Public Sub New()
            MyBase.New(New VisualBasicConvertLinqQueryToLinqMethodProvider())
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Test1() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Option Strict Off

Imports System

Class QueryAble
    Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
        System.Console.WriteLine("Select")
        Return Me
    End Function
End Class

Module Module1
    Sub Main()
        Dim q As New QueryAble()
        Dim q1 As Object = [||]From s In q 
    End Sub
End Module
    </file>
</compilation>

            Await Test(compilationDef,
                                expectedOutput:=
            <![CDATA[
Select
]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Test2() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble
            Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
                System.Console.WriteLine("Select")
                Return Me
            End Function

            Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
                System.Console.WriteLine("Where")
                Return Me
            End Function

        End Class

        Module Module1
            Sub Main()
                Dim q As New QueryAble()
                Dim q1 As Object = [||]From s In q Where s > 0 
                System.Console.WriteLine("-----")
                Dim q2 As Object = [||]From s In q Where s > 0 Where 10 > s
            End Sub
        End Module
    </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
Where
-----
Where
Where
]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Test3() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble
            Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
                System.Console.WriteLine("Select")
                Return Me
            End Function

            Public Function Where(Of T, U)(x As Func(Of T, U)) As QueryAble
                System.Console.WriteLine("Where {0}", x.GetType())
                Return Me
            End Function

        End Class

        Module Module1
            Sub Main()
                Dim q As New QueryAble()
                Dim q1 As Object = [||]From s In q Where s > 0 
            End Sub
        End Module
    </file>
</compilation>

            Await Test(compilationDef,
                                expectedOutput:=
            <![CDATA[Where System.Func`2[System.Int32,System.Boolean]
]]>)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Test7() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble1
            Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble1
                System.Console.WriteLine("Select")
                Return Me
            End Function
        End Class

        Class QueryAble2

            Function AsQueryable() As QueryAble1
                System.Console.WriteLine("AsQueryable")
                Return New QueryAble1()
            End Function

            Function AsEnumerable() As QueryAble1
                System.Console.WriteLine("AsEnumerable")
                Return New QueryAble1()
            End Function

            Function Cast(Of T)() As QueryAble2
                System.Console.WriteLine("Cast")
                Return Me
            End Function
        End Class

        Class C
            Function [Select](ByRef f As Func(Of String, String)) As C
                System.Console.WriteLine("[Select](ByRef f As Func(Of String, String))")
                Return Me
            End Function

            Function [Select](ByRef f As Func(Of Integer, String)) As C
                System.Console.WriteLine("[Select](ByRef f As Func(Of Integer, String))")
                Return Me
            End Function

            Function [Select](ByVal f As Func(Of Integer, Integer)) As C
                System.Console.WriteLine("[Select](ByVal f As Func(Of Integer, Integer))")
                Return Me
            End Function
        End Class

        Module Module1
            Sub Main()
                Dim q As New QueryAble2()
                Dim q1 As Object = [||]From s In q
                Dim y = [||]From z In New C Select z Select z = z.ToString() Select z.ToUpper()
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
AsQueryable
[Select](ByVal f As Func(Of Integer, Integer))
[Select](ByRef f As Func(Of Integer, String))
]]>)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Test8() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble1
            Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble1
                System.Console.WriteLine("Select")
                Return Me
            End Function
        End Class

        Class QueryAble2

            Function AsEnumerable() As QueryAble1
                System.Console.WriteLine("AsEnumerable")
                Return New QueryAble1()
            End Function

            Function Cast(Of T)() As QueryAble2
                System.Console.WriteLine("Cast")
                Return Me
            End Function
        End Class

        Module Module1
            Sub Main()
                Dim q As New QueryAble2()
                Dim q1 As Object = [||]From s In q
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
        AsEnumerable
        ]]>)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Test9() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble1
            Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble1
                System.Console.WriteLine("Select")
                Return Me
            End Function
        End Class

        Class QueryAble2
            Function Cast(Of T)() As QueryAble2
                System.Console.WriteLine("Cast")
                Return Me
            End Function

            Public Function Where(Of T)(x As Func(Of T, Boolean)) As QueryAble2
                System.Console.WriteLine("Where {0}", x.GetType())
                x.Invoke(CType(CObj(1), T))
                Return Me
            End Function

        End Class

        Module Module1
            Sub Main()
                Dim q As New QueryAble2()
                Dim q1 As Object = [||]From s In q
                System.Console.WriteLine("-----")
                Dim q2 As Object = [||]From s In q Where s > 0
                System.Console.WriteLine("-----")
                Dim x as Object = new Object()
                Dim q3 As Object = [||]From s In q Where DirectCast(Function() s > 0 AndAlso x IsNot Nothing, Func(Of Boolean)).Invoke()
                System.Console.WriteLine("-----")
                Dim q4 As Object = [||]From s In q Where (From s1 In q Where s > s1 ) IsNot Nothing
                System.Console.WriteLine("-----")
                Dim q5 As Object = [||]From s In q Where DirectCast(Function() 
                                                                    System.Console.WriteLine(s)
                                                                    System.Console.WriteLine(PassByRef1(s))
                                                                    System.Console.WriteLine(s)
                                                                    System.Console.WriteLine(PassByRef2(s))
                                                                    System.Console.WriteLine(s)
                                                                    return True
                                                                End Function, Func(Of Boolean)).Invoke()
            End Sub

            Function PassByRef1(ByRef x as Object) As Integer
                x=x+1
                Return x
            End Function 

            Function PassByRef2(ByRef x as Short) As Integer
                x=x+1
                Return x
            End Function 
        End Module
    </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
Cast
-----
Cast
Where System.Func`2[System.Object,System.Boolean]
-----
Cast
Where System.Func`2[System.Object,System.Boolean]
-----
Cast
Where System.Func`2[System.Object,System.Boolean]
Cast
Where System.Func`2[System.Object,System.Boolean]
-----
Cast
Where System.Func`2[System.Object,System.Boolean]
1
2
1
2
1
]]>)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Test11() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble
            Public Function [Select](x As Integer) As QueryAble
                Return Me
            End Function

            Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
                System.Console.WriteLine("Where")
                Return Me
            End Function
        End Class

        Module Module1

            &lt;System.Runtime.CompilerServices.Extension()&gt;
            Public Function [Select](this As QueryAble, x As Func(Of Integer, Integer)) As QueryAble
                Return Nothing
            End Function

            Sub Main()
                Dim q As New QueryAble()
                Dim q1 As Object = [||]From s In q Where s > 0
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

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
        Where
        ]]>)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Test25() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble
            Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
                System.Console.WriteLine("Select")
                Return Me
            End Function

            Public ReadOnly Property Where As QueryAble
                Get
                    Return Nothing
                End Get
            End Property
        End Class

        Module Module1

            &lt;System.Runtime.CompilerServices.Extension()&gt;
            Public Function Where(this As QueryAble, x As Func(Of Integer, Boolean)) As QueryAble
                System.Console.WriteLine("Where")
                Return this
            End Function

            Sub Main()
                Dim q As New QueryAble()
                Dim q1 As Object = [||]From s In q Where s > 0
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

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
        Where
        ]]>)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Test26() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble
            Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
                System.Console.WriteLine("Select")
                Return Me
            End Function

            Public Where As QueryAble
        End Class

        Module Module1

            &lt;System.Runtime.CompilerServices.Extension()&gt;
            Public Function Where(this As QueryAble, x As Func(Of Integer, Boolean)) As QueryAble
                System.Console.WriteLine("Where")
                Return this
            End Function

            Sub Main()
                Dim q As New QueryAble()
                Dim q1 As Object = [||]From s In q Where s > 0
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

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
        Where
        ]]>)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Select1() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble
            Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
                System.Console.WriteLine()
                System.Console.WriteLine("Select")
                System.Console.Write(x(1))
                Return Me
            End Function

            Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
                System.Console.WriteLine()
                System.Console.WriteLine("Where")
                System.Console.Write(x(2))
                Return Me
            End Function
        End Class

        Module Module1

            Function Num1() As Integer
                System.Console.WriteLine("Num1")
                Return -10
            End Function

            Function Num2() As Integer
                System.Console.WriteLine("Num2")
                Return -20
            End Function

            Class Index
                Default Property Item(x As String) As Integer
                    Get
                        System.Console.WriteLine("Item {0}", x)
                        Return 100
                    End Get
                    Set(value As Integer)
                    End Set
                End Property
            End Class

            Sub Main()
                Dim q As New QueryAble()
                System.Console.WriteLine("-----")
                Dim q1 As Object = [||]From s In q Select t = s * 2 Select t
                System.Console.WriteLine()
                System.Console.WriteLine("-----")
                Dim q2 As Object = [||]From s In q Select s * 3 Where 100 Select -1
                System.Console.WriteLine()
                System.Console.WriteLine("-----")
                Dim ind As New Index()

                Dim q3 As Object = [||]From s In q
                                   Select s
                                   Where s > 0
                                   Select Num1
                                   Where Num1 = -10
                                   Select Module1.Num2()
                                   Where Num2 = -10 + Num1()
                                   Select ind!Two
                                   Where Two > 0

                System.Console.WriteLine()
                System.Console.WriteLine("-----")
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
-----

Select
2
Select
1
-----

Select
3
Where
True
Select
-1
-----

Select
1
Where
True
Select
Num1
-10
Where
False
Select
Num2
-20
Where
Num1
False
Select
Item Two
100
Where
True
-----
]]>)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function ImplicitSelect4() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble
            Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
                System.Console.WriteLine("Select")
                Return Me
            End Function

            Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
                System.Console.WriteLine("Where")
                Return Me
            End Function

        End Class

        Module Module1
            &lt;System.Runtime.CompilerServices.Extension()&gt;
            Public Function [Select](this As QueryAble, x As Func(Of Integer, Long)) As QueryAble
                System.Console.WriteLine("[Select]")
                Return this
            End Function

            &lt;System.Runtime.CompilerServices.Extension()&gt;
            Public Function Where(this As QueryAble, x As Func(Of Long, Boolean)) As QueryAble
                System.Console.WriteLine("[Where]")
                Return this
            End Function


            Sub Main()
                Dim q As New QueryAble()
                Dim q1 As Object = [||]From s As Integer In q Where s > 1
                System.Console.WriteLine("------")
                Dim q2 As Object = [||]From s As Long In q Where s > 1
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

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
        Where
        ------
        [Select]
        [Where]
        ]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Where5() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble
            Public Function [Select](x As Func(Of Date, Date)) As QueryAble
                Return Me
            End Function

            Public Function Where(x As Func(Of Date, Object)) As QueryAble
                System.Console.WriteLine("Where Object")
                Return Me
            End Function

            Public Function Where(x As Func(Of Date, Boolean)) As QueryAble
                System.Console.WriteLine("Where Boolean")
                Return Me
            End Function
        End Class

        Module Module1

            Sub Main()
                Dim q As New QueryAble()
                Dim q1 As Object = [||]From s In q Where CObj(s)
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
Where Boolean
]]>)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Where6() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble
            Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
                Return Me
            End Function

            Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
                System.Console.WriteLine("Where Byte")
                Return Me
            End Function

            Public Function Where(x As Func(Of Integer, SByte)) As QueryAble
                System.Console.WriteLine("Where SByte")
                Return Me
            End Function
        End Class

        Module Module1

            Sub Main()
                Dim q As New QueryAble()
                Dim q1 As Object = [||]From s In q Where 0
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
        Where Byte
        ]]>)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Where7() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble
            Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
                Return Me
            End Function

            Public Function Where(x As Func(Of Integer, Long)) As QueryAble
                System.Console.WriteLine("Where Long")
                Return Me
            End Function

            Public Function Where(x As Func(Of Integer, System.DateTimeKind)) As QueryAble
                System.Console.WriteLine("Where System.DateTimeKind")
                Return Me
            End Function
        End Class

        Module Module1

            Sub Main()
                Dim q As New QueryAble()
                Dim q1 As Object = [||]From s In q Where 0
                q.Where(Function(s) 0)
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[Where Long
Where Long
        ]]>)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Where8() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble
            Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
                Return Me
            End Function

            Public Function Where(x As Func(Of Integer, Byte)) As QueryAble
                System.Console.WriteLine("Where Byte")
                Return Me
            End Function

            Public Function Where(x As Func(Of Integer, System.DateTimeKind)) As QueryAble
                System.Console.WriteLine("Where System.DateTimeKind")
                Return Me
            End Function
        End Class

        Module Module1

            Sub Main()
                Dim q As New QueryAble()
                Dim q1 As Object = [||]From s In q Where 0
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
        Where System.DateTimeKind
        ]]>)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Where11() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble(Of T)
            Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
                System.Console.WriteLine("Select {0}", x)
                Return New QueryAble(Of S)()
            End Function

            Public Function Where(x As Func(Of T, Boolean)) As QueryAble(Of T)
                System.Console.WriteLine("Where {0}", x)
                Return New QueryAble(Of T)()
            End Function
        End Class

        Module Module1
            Sub Main()
                Dim q1 As New QueryAble(Of Integer)()
                Dim q As Object

                q = [||]From s In q1 Where Nothing
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
        Where System.Func`2[System.Int32,System.Boolean]
        ]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Where12() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble(Of T)
            Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
                System.Console.WriteLine("Select {0}", x)
                Return New QueryAble(Of S)()
            End Function

            Public Function Where(x As Func(Of T, Boolean)) As QueryAble(Of T)
                System.Console.WriteLine("Where {0}", x)
                Return New QueryAble(Of T)()
            End Function

            Public Function Where(x As Func(Of T, Object)) As QueryAble(Of T)
                System.Console.WriteLine("Where {0}", x)
                Return New QueryAble(Of T)()
            End Function

            Public Function Where(x As Func(Of T, Integer)) As QueryAble(Of T)
                System.Console.WriteLine("Where {0}", x)
                Return New QueryAble(Of T)()
            End Function
        End Class

        Module Module1
            Sub Main()
                Dim q1 As New QueryAble(Of Integer)()
                Dim q As Object

                q = [||]From s In q1 Where Nothing
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
        Where System.Func`2[System.Int32,System.Boolean]
        ]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function While2() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble
            Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
                System.Console.WriteLine("Select")
                Return Me
            End Function

            Public Function Where(x As Func(Of Integer, Boolean)) As QueryAble
                System.Console.WriteLine("Where")
                Return Me
            End Function

            Public Function TakeWhile(x As Func(Of Integer, Boolean)) As QueryAble
                System.Console.WriteLine("TakeWhile")
                Return Me
            End Function

            Public Function SkipWhile(x As Func(Of Integer, Boolean)) As QueryAble
                System.Console.WriteLine("SkipWhile")
                Return Me
            End Function
        End Class

        Module Module1
            Sub Main()
                Dim q As New QueryAble()
                Dim q1 As Object = [||]From s In q Skip While s > 0 
                System.Console.WriteLine("-----")
                Dim q2 As Object = [||]From s In q Take While s > 0 
                System.Console.WriteLine("-----")
                Dim q3 As Object = [||]From s In q Skip While s > 0 Take While 10 > s Skip While s > 0 Select s
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
        SkipWhile
        -----
        TakeWhile
        -----
        SkipWhile
        TakeWhile
        SkipWhile
        Select
        ]]>)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Distinct2() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble
            Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
                System.Console.WriteLine("Select")
                Return Me
            End Function

            Public Function Distinct() As QueryAble
                System.Console.WriteLine("Distinct")
                Return Me
            End Function
        End Class

        Module Module1
            Sub Main()
                Dim q As New QueryAble()
                Dim q1 As Object = [||]From s In q Distinct
                System.Console.WriteLine("-----")
                Dim q2 As Object = [||]From s In q Select s + 1 Distinct Distinct
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
Distinct
-----
Select
Distinct
Distinct
]]>)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function SkipTake2() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble
            Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
                System.Console.WriteLine("Select")
                Return Me
            End Function

            Public Function Skip(count As Date) As QueryAble
                System.Console.WriteLine("Skip {0}", count.ToString("M/d/yyyy h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture))
                Return Me
            End Function

            Public Function Take(count As Integer) As QueryAble
                System.Console.WriteLine("Skip {0}", count)
                Return Me
            End Function
        End Class

        Module Module1
            Sub Main()
                Dim q As New QueryAble()

                Dim q1 As Object = [||]From s In q Skip #12:00:00 AM#
                System.Console.WriteLine("-----")
                Dim q2 As Object = [||]From s In q Take 1 Select s
                System.Console.WriteLine("-----")
                Dim q3 As Object = [||]From s In q Select s + 1 Take 2
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
Skip 1/1/0001 12:00:00 AM
-----
Skip 1
Select
-----
Select
Skip 2
]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function SkipTake3() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict On

        Imports System

        Class QueryAble(Of T)
            Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
                System.Console.WriteLine("Select {0}", x)
                Return New QueryAble(Of S)()
            End Function

            Public Function Skip(x As Integer) As QueryAble(Of T)
                System.Console.WriteLine("Skip {0}", x)
                Return New QueryAble(Of T)()
            End Function
        End Class

        Module Module1
            Sub Main()
                Dim q1 As New QueryAble(Of Integer)()
                Dim q As Object

                q = [||]From s In q1 Skip Nothing
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
        Skip 0
        ]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function OrderBy4() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble
            Public ReadOnly v As Integer

            Sub New(v As Integer)
                Me.v = v
            End Sub

            Public Function [Select](x As Func(Of Integer, Integer)) As QueryAble
                System.Console.WriteLine("Select {0}", v)
                Return New QueryAble(v + 1)
            End Function

            Public Function OrderBy(x As Func(Of Integer, Integer)) As QueryAble
                System.Console.WriteLine("OrderBy {0}", v)
                Return New QueryAble(v + 1)
            End Function

            Public Function ThenBy(x As Func(Of Integer, Byte)) As QueryAble
                System.Console.WriteLine("ThenBy {0}", v)
                Return New QueryAble(v + 1)
            End Function

            Public Function OrderByDescending(x As Func(Of Integer, Integer)) As QueryAble
                System.Console.WriteLine("OrderByDescending {0}", v)
                Return New QueryAble(v + 1)
            End Function

            Public Function ThenByDescending(x As Func(Of Integer, Byte)) As QueryAble
                System.Console.WriteLine("ThenByDescending {0}", v)
                Return New QueryAble(v + 1)
            End Function
        End Class

        Module Module1
            Sub Main()
                Dim q As New QueryAble(0)

                Dim q1 As Object = [||]From s In q
                                   Order By s, s, s Descending, s Ascending
                                   Order By s Descending, s Descending, s
                                   Order By s Ascending
                                   Select s

                System.Console.WriteLine("-----")
                Dim q2 As Object = [||]From s In q Select s + 1 Order By 0
                System.Console.WriteLine("-----")
                Dim q3 As Object = [||]From s In q Order By s
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
OrderBy 0
ThenBy 1
ThenByDescending 2
ThenBy 3
OrderByDescending 4
ThenByDescending 5
ThenBy 6
OrderBy 7
Select 8
-----
Select 0
OrderBy 1
-----
OrderBy 0
]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function OrderBy5() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble(Of T)

            Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
                System.Console.WriteLine("Select {0}", x)
                Return New QueryAble(Of S)()
            End Function

            Public Function OrderBy(Of S)(x As Func(Of T, S)) As QueryAble(Of T)
                System.Console.WriteLine("OrderBy {0}", x)
                Return New QueryAble(Of T)()
            End Function
        End Class

        Module Module1
            Sub Main()
                Dim q1 As New QueryAble(Of Integer)()
                Dim q As Object

                q = [||]From s In q1 Order By Nothing
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
        OrderBy System.Func`2[System.Int32,System.Object]
        ]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function OrderBy6() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble(Of T)

            Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
                System.Console.WriteLine("Select {0}", x)
                Return New QueryAble(Of S)()
            End Function

            Public Function OrderBy(x As Func(Of T, Integer)) As QueryAble(Of T)
                System.Console.WriteLine("OrderBy {0}", x)
                Return New QueryAble(Of T)()
            End Function
        End Class

        Module Module1
            Sub Main()
                Dim q1 As New QueryAble(Of Integer)()
                Dim q As Object

                q = [||]From s In q1 Order By Nothing
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
        OrderBy System.Func`2[System.Int32,System.Int32]
        ]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Select6() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off
        Option Infer On

        Imports System
        Imports System.Collections
        Imports System.Linq


        Module Module1
            Sub Main()
                Dim q0 As IEnumerable = [||]From s In New Integer() {1,2} Select s, t=s+1

                For Each v In q0
                   System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                Dim q1 As IEnumerable = [||]From s In New Integer() {1,-1} Select s, t=s*2 Where s > t

                For Each v In q1
                   System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                Dim q2 As IEnumerable = [||]From s In New Integer() {1,-1} Select s, t=s*2 Select t, s

                For Each v In q2
                   System.Console.WriteLine(v)
                Next
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef, additionalRefs:={LinqAssemblyRef},
                            expectedOutput:=
            <![CDATA[
{ s = 1, t = 2 }
{ s = 2, t = 3 }
------
{ s = -1, t = -2 }
------
{ t = 2, s = 1 }
{ t = -2, s = -1 }
]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Select10() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict On

        Imports System

        Class QueryAble(Of T)
            Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
                System.Console.WriteLine("Select {0}", x)
                Return New QueryAble(Of S)()
            End Function
        End Class

        Module Module1
            Sub Main()
                Dim q1 As New QueryAble(Of Integer)()
                Dim q As Object

                q = [||]From s In q1 Select Nothing
                q = [||]From s In q1 Select x=Nothing, y=Nothing
                q = [||]From s In q1 Let x = Nothing
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
Select System.Func`2[System.Int32,System.Object]
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Object,System.Object]]
Select System.Func`2[System.Int32,VB$AnonymousType_1`2[System.Int32,System.Object]]
        ]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Let1() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off
        Option Infer On

        Imports System
        Imports System.Collections
        Imports System.Linq


        Module Module1
            Sub Main()
                Dim q0 As IEnumerable = [||]From s1 In New Integer() {1} Let s2 = s1+1

                For Each v In q0
                   System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                Dim q1 As IEnumerable = [||]From s1 In New Integer() {1} Let s2 = s1+1, s3 = s2+s1

                For Each v In q1
                   System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                Dim q2 As IEnumerable = [||]From s1 In New Integer() {1} Let s2 = s1+1, s3 = s2+s1, s4 = s1+s2+s3

                For Each v In q2
                   System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                Dim q3 As IEnumerable = [||]From s1 In New Integer() {1} Let s2 = s1+1, s3 = s2+s1, s4 = s1+s2+s3, s5 = s1+s2+s3+s4

                For Each v In q3
                   System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                Dim q4 As IEnumerable = [||]From s1 In New Integer() {2} Let s2 = s1+1 Let s3 = s2+s1, s4 = s1+s2+s3, s5 = s1+s2+s3+s4

                For Each v In q4
                   System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                Dim q5 As IEnumerable = [||]From s1 In New Integer() {3} Let s2 = s1+1, s3 = s2+s1 Let s4 = s1+s2+s3, s5 = s1+s2+s3+s4

                For Each v In q5
                   System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                Dim q6 As IEnumerable = [||]From s1 In New Integer() {4} Let s2 = s1+1, s3 = s2+s1, s4 = s1+s2+s3 Let s5 = s1+s2+s3+s4

                For Each v In q6
                   System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                Dim q7 As IEnumerable = [||]From s1 In New Integer() {5} Select s1+1 Let s2 = 7

                For Each v In q7
                   System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                Dim q8 As IEnumerable = [||]From s1 In New Integer() {5} Select s1+1 Let s2 = 7, s3 = 8

                For Each v In q8
                   System.Console.WriteLine(v)
                Next
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef, additionalRefs:={LinqAssemblyRef},
                            expectedOutput:=
            <![CDATA[
{ s1 = 1, s2 = 2 }
------
{ s1 = 1, s2 = 2, s3 = 3 }
------
{ s1 = 1, s2 = 2, s3 = 3, s4 = 6 }
------
{ s1 = 1, s2 = 2, s3 = 3, s4 = 6, s5 = 12 }
------
{ s1 = 2, s2 = 3, s3 = 5, s4 = 10, s5 = 20 }
------
{ s1 = 3, s2 = 4, s3 = 7, s4 = 14, s5 = 28 }
------
{ s1 = 4, s2 = 5, s3 = 9, s4 = 18, s5 = 36 }
------
7
------
{ s2 = 7, s3 = 8 }
]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Let3() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble(Of T)
            'Inherits Base

            'Public Shadows [Select] As Byte
            Public ReadOnly v As Integer

            Sub New(v As Integer)
                Me.v = v
            End Sub

            Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
                System.Console.WriteLine("Select {0}", x)
                Return New QueryAble(Of S)(v + 1)
            End Function

            Public Function SelectMany(Of S, R)(m As Func(Of T, QueryAble(Of S)), x As Func(Of T, S, R)) As QueryAble(Of R)
                System.Console.WriteLine("SelectMany {0}", x)
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function Where(x As Func(Of T, Boolean)) As QueryAble(Of T)
                System.Console.WriteLine("Where {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function TakeWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
                System.Console.WriteLine("TakeWhile {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function SkipWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
                System.Console.WriteLine("SkipWhile {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function OrderBy(x As Func(Of T, Integer)) As QueryAble(Of T)
                System.Console.WriteLine("OrderBy {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Distinct() As QueryAble(Of T)
                System.Console.WriteLine("Distinct")
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Skip(count As Integer) As QueryAble(Of T)
                System.Console.WriteLine("Skip {0}", count)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Take(count As Integer) As QueryAble(Of T)
                System.Console.WriteLine("Take {0}", count)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
                System.Console.WriteLine("Join {0}", x)
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function GroupBy(Of K, I, R)(key As Func(Of T, K), item As Func(Of T, I), into As Func(Of K, QueryAble(Of I), R)) As QueryAble(Of R)
                System.Console.WriteLine("GroupBy {0}", item)
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of T), R)) As QueryAble(Of R)
                System.Console.WriteLine("GroupBy ")
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
                System.Console.WriteLine("GroupJoin {0}", x)
                Return New QueryAble(Of R)(v + 1)
            End Function

        End Class

        Module Module1
            Sub Main()
                Dim q As New QueryAble(Of Integer)(0)

                Dim q0 As Object = [||]From s In q Let t1 = s + 1
                System.Console.WriteLine("------")
                Dim q1 As Object = [||]From s In q Let t1 = s + 1, t2 = t1
                System.Console.WriteLine("------")
                Dim q2 As Object = [||]From s In q Let t1 = s + 1, t2 = t1, t3 = t2
                System.Console.WriteLine("------")
                Dim q3 As Object = [||]From s In q Let t1 = s + 1, t2 = t1, t3 = t2, t4 = t3
                System.Console.WriteLine("------")
                Dim q4 As Object = [||]From s In q Let t1 = s + 1 Let t2 = t1, t3 = t2, t4 = t3
                System.Console.WriteLine("------")
                Dim q5 As Object = [||]From s In q Let t1 = s + 1, t2 = t1 Let t3 = t2, t4 = t3
                System.Console.WriteLine("------")
                Dim q6 As Object = [||]From s In q Let t1 = s + 1, t2 = t1, t3 = t2 Let t4 = t3
                System.Console.WriteLine("------")
                Dim q7 As Object = [||]From s In q Let t1 = s + 1, t2 = t1, t3 = t2 Select s, t1, t2, t3, t4 = t3

                System.Console.WriteLine("------")
                Dim q8 As Object = [||]From s In q Let t1 = s + 1, t2 = t1, t3 = t2 Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 
                System.Console.WriteLine("------")
                Dim q9 As Object = [||]From s In q Let t1 = s + 1, t2 = t1, t3 = t2 Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 Select s, t1, t2, t3, t4 = t3
                System.Console.WriteLine("------")
                Dim q10 As Object = [||]From s In q Let t1 = s + 1, t2 = t1, t3 = t2 Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 Let t4 = 1
                System.Console.WriteLine("------")
                Dim q11 As Object = [||]From s In q Let t1 = s + 1, t2 = t1, t3 = t2 Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 From t4 In q
                System.Console.WriteLine("------")
                Dim q12 As Object = [||]From s In q Let t1 = s + 1, t2 = t1, t3 = t2 
                                    Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 
                                    Join t4 in q On t3 Equals t4
                System.Console.WriteLine("------")
                Dim q13 As Object = [||]From s In q Let t1 = s + 1, t2 = t1, t3 = t2 
                                    Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 
                                    Group s By t3 Into Group
                System.Console.WriteLine("------")
                Dim q14 As Object = [||]From s In q Let t1 = s + 1, t2 = t1, t3 = t2 
                                    Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 
                                    Group By t3 Into Group
                System.Console.WriteLine("------")
                Dim q15 As Object = [||]From s In q Let t1 = s + 1, t2 = t1, t3 = t2 
                                    Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 
                                    Group Join t4 in q On t3 Equals t4 Into Group
                System.Console.WriteLine("------")
                Dim q16 As Object = [||]From s In q Let t1 = s + 1, t2 = t1, t3 = t2 
                                    Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 
                                    Aggregate t4 in q Into Where(True)
                System.Console.WriteLine("------")
                Dim q17 As Object = [||]From s In q Let t1 = s + 1, t2 = t1, t3 = t2 
                                    Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0 
                                    Aggregate t4 in q Into Where(True), Distinct
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_1`3[System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],VB$AnonymousType_5`5[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],VB$AnonymousType_5`5[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],VB$AnonymousType_5`5[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],VB$AnonymousType_5`5[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],VB$AnonymousType_5`5[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32]]
Where System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32],System.Boolean]
Skip 0
Take 0
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],VB$AnonymousType_5`5[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],VB$AnonymousType_5`5[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
Skip 0
Take 0
SelectMany System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32,VB$AnonymousType_5`5[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
Skip 0
Take 0
Join System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32,VB$AnonymousType_5`5[System.Int32,System.Int32,System.Int32,System.Int32,System.Int32]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
Skip 0
Take 0
GroupBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32]]
Where System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,System.Int32,System.Int32],System.Boolean]
Skip 0
Take 0
GroupBy 
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
Skip 0
Take 0
GroupJoin System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],QueryAble`1[System.Int32],VB$AnonymousType_7`5[System.Int32,System.Int32,System.Int32,System.Int32,QueryAble`1[System.Int32]]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],VB$AnonymousType_8`5[System.Int32,System.Int32,System.Int32,System.Int32,QueryAble`1[System.Int32]]]
------
Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32]]
Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],VB$AnonymousType_9`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],QueryAble`1[System.Int32]]]
Select System.Func`2[VB$AnonymousType_9`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],System.Int32],System.Int32],QueryAble`1[System.Int32]],VB$AnonymousType_10`6[System.Int32,System.Int32,System.Int32,System.Int32,QueryAble`1[System.Int32],QueryAble`1[System.Int32]]]
]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function From3() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off
        Option Infer On

        Imports System
        Imports System.Collections
        Imports System.Linq


        Module Module1
            Sub Main()
                Dim q0 As IEnumerable
                q0 = [||]From s1 In New Integer() {1}, s2 In New Integer() {2, 3}

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1}, s2 In New Integer() {2, 3}, s3 In New Integer() {4, 5}

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1} From s2 In New Integer() {2, 3}, s3 In New Integer() {6, 7}

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1}, s2 In New Integer() {2, 3} From s3 In New Integer() {8, 9}

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1, -1} Select s1 + 1 From s2 In New Integer() {2, 3}

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1, -1} Select s1 + 1 From s2 In New Integer() {2, 3}, s3 In New Integer() {4, 5}

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1}, s2 In New Integer() {2, 3} Select s2, s1

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1}, s2 In New Integer() {2, 3} Let s3 = s1 + s2

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1}, s2 In New Integer() {2, 3} Let s3 = s1 + s2, s4 = s3 + 1

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1, 2} Select s1 + 1 From s2 In New Integer() {2, 3} Select s3 = 4, s2

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1, 2} Select s1 + 1 From s2 In New Integer() {2, 3} Let s3 = 5

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer()() {New Integer() {1, 2}, New Integer() {2, 3}}, s2 In s1 Select s3 = s1(0), s2

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef, additionalRefs:={LinqAssemblyRef},
                            expectedOutput:=
            <![CDATA[
{ s1 = 1, s2 = 2 }
{ s1 = 1, s2 = 3 }
------
{ s1 = 1, s2 = 2, s3 = 4 }
{ s1 = 1, s2 = 2, s3 = 5 }
{ s1 = 1, s2 = 3, s3 = 4 }
{ s1 = 1, s2 = 3, s3 = 5 }
------
{ s1 = 1, s2 = 2, s3 = 6 }
{ s1 = 1, s2 = 2, s3 = 7 }
{ s1 = 1, s2 = 3, s3 = 6 }
{ s1 = 1, s2 = 3, s3 = 7 }
------
{ s1 = 1, s2 = 2, s3 = 8 }
{ s1 = 1, s2 = 2, s3 = 9 }
{ s1 = 1, s2 = 3, s3 = 8 }
{ s1 = 1, s2 = 3, s3 = 9 }
------
2
3
2
3
------
{ s2 = 2, s3 = 4 }
{ s2 = 2, s3 = 5 }
{ s2 = 3, s3 = 4 }
{ s2 = 3, s3 = 5 }
{ s2 = 2, s3 = 4 }
{ s2 = 2, s3 = 5 }
{ s2 = 3, s3 = 4 }
{ s2 = 3, s3 = 5 }
------
{ s2 = 2, s1 = 1 }
{ s2 = 3, s1 = 1 }
------
{ s1 = 1, s2 = 2, s3 = 3 }
{ s1 = 1, s2 = 3, s3 = 4 }
------
{ s1 = 1, s2 = 2, s3 = 3, s4 = 4 }
{ s1 = 1, s2 = 3, s3 = 4, s4 = 5 }
------
{ s3 = 4, s2 = 2 }
{ s3 = 4, s2 = 3 }
{ s3 = 4, s2 = 2 }
{ s3 = 4, s2 = 3 }
------
{ s2 = 2, s3 = 5 }
{ s2 = 3, s3 = 5 }
{ s2 = 2, s3 = 5 }
{ s2 = 3, s3 = 5 }
------
{ s3 = 1, s2 = 1 }
{ s3 = 1, s2 = 2 }
{ s3 = 2, s2 = 2 }
{ s3 = 2, s2 = 3 }
]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function From5() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble(Of T)
            Public ReadOnly v As Integer

            Sub New(v As Integer)
                Me.v = v
            End Sub

            Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
                System.Console.WriteLine("Select {0}", x)
                Return New QueryAble(Of S)(v + 1)
            End Function

            Public Function SelectMany(Of S, R)(m As Func(Of T, QueryAble(Of S)), x As Func(Of T, S, R)) As QueryAble(Of R)
                System.Console.WriteLine("SelectMany {0}", x)
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function Where(x As Func(Of T, Boolean)) As QueryAble(Of T)
                System.Console.WriteLine("Where {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function TakeWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
                System.Console.WriteLine("TakeWhile {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function SkipWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
                System.Console.WriteLine("SkipWhile {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function OrderBy(x As Func(Of T, Integer)) As QueryAble(Of T)
                System.Console.WriteLine("OrderBy {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Distinct() As QueryAble(Of T)
                System.Console.WriteLine("Distinct")
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Skip(count As Integer) As QueryAble(Of T)
                System.Console.WriteLine("Skip {0}", count)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Take(count As Integer) As QueryAble(Of T)
                System.Console.WriteLine("Take {0}", count)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
                System.Console.WriteLine("Join {0}", x)
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function GroupBy(Of K, I, R)(key As Func(Of T, K), item As Func(Of T, I), into As Func(Of K, QueryAble(Of I), R)) As QueryAble(Of R)
                System.Console.WriteLine("GroupBy {0}", item)
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of T), R)) As QueryAble(Of R)
                System.Console.WriteLine("GroupBy ")
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
                System.Console.WriteLine("GroupJoin {0}", x)
                Return New QueryAble(Of R)(v + 1)
            End Function

        End Class

        Module Module1

            Sub Main()
                Dim qi As New QueryAble(Of Integer)(0)
                Dim qb As New QueryAble(Of Byte)(0)
                Dim qs As New QueryAble(Of Short)(0)
                Dim qu As New QueryAble(Of UInteger)(0)
                Dim ql As New QueryAble(Of Long)(0)

                Dim q0 As Object
                q0 = [||]From s1 In qi From s2 In qb
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi, s2 In qb
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi From s2 In qb, s3 In qs
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi From s2 In qb, s3 In qs, s4 In qu
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi From s2 In qb, s3 In qs, s4 In qu, s5 In ql
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi From s2 In qb From s3 In qs, s4 In qu, s5 In ql
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi From s2 In qb, s3 In qs From s4 In qu, s5 In ql
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi From s2 In qb, s3 In qs, s4 In qu From s5 In ql
                System.Console.WriteLine("------")

                q0 = [||]From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0 From s5 In ql
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0 Let s5 = 1L
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0 Select s4, s3, s2, s1
                System.Console.WriteLine("------")

                q0 = [||]From s1 In qi, s2 In qb Select s2, s1
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi From s2 In qb, s3 In qs, s4 In qu Select s4, s3, s2, s1
                System.Console.WriteLine("------")

                q0 = [||]From s1 In qi, s2 In qb Let s3 = 1L
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi From s2 In qb, s3 In qs, s4 In qu Let s5 = 1L
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi, s2 In qb Let s3 = 1S, s4 = 1UI
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi, s2 In qb Let s3 = 1S Let s4 = 1UI
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi From s2 In qb, s3 In qs, s4 In qu Let s5 = 1L Select s5, s4, s3, s2, s1
                System.Console.WriteLine("------")

                q0 = [||]From s1 In qi Select s1 + 1 From s2 In qb
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi Select s1 + 1 From s2 In qb Select s2, s3 = 1S
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi Select s1 + 1 From s2 In qb Let s3 = 1S
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0 
                     Join s5 In ql On s1 Equals s5
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0 
                     Group s1 By s2 Into Group
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0 
                     Group By s2 Into Group
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0 
                     Group Join s5 In ql On s1 Equals s5 Into Group
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0 
                     Aggregate s5 In ql Into Where(True)
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi From s2 In qb, s3 In qs, s4 In qu Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0 
                     Aggregate s5 In ql Into Where(True), Distinct
                System.Console.WriteLine("------")
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`3[System.Int32,System.Byte,System.Int16]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
SelectMany System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int64,VB$AnonymousType_5`5[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
SelectMany System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int64,VB$AnonymousType_5`5[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
SelectMany System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int64,VB$AnonymousType_5`5[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
SelectMany System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int64,VB$AnonymousType_5`5[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32]]
Where System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32],System.Boolean]
Skip 0
Take 0
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
Skip 0
Take 0
SelectMany System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int64,VB$AnonymousType_5`5[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],VB$AnonymousType_5`5[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],VB$AnonymousType_6`4[System.UInt32,System.Int16,System.Byte,System.Int32]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_7`2[System.Byte,System.Int32]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_6`4[System.UInt32,System.Int16,System.Byte,System.Int32]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_1`3[System.Int32,System.Byte,System.Int64]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_5`5[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_1`3[System.Int32,System.Byte,System.Int16]]
Select System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Byte,System.Int16],VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_1`3[System.Int32,System.Byte,System.Int16]]
Select System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Byte,System.Int16],VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_8`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,System.Int64]]
Select System.Func`2[VB$AnonymousType_8`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,System.Int64],VB$AnonymousType_9`5[System.Int64,System.UInt32,System.Int16,System.Byte,System.Int32]]
------
Select System.Func`2[System.Int32,System.Int32]
SelectMany System.Func`3[System.Int32,System.Byte,System.Byte]
------
Select System.Func`2[System.Int32,System.Int32]
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_10`2[System.Byte,System.Int16]]
------
Select System.Func`2[System.Int32,System.Int32]
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_10`2[System.Byte,System.Int16]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
Skip 0
Take 0
Join System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int64,VB$AnonymousType_5`5[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
Skip 0
Take 0
GroupBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int32]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32]]
Where System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Byte,System.Int16,System.UInt32],System.Boolean]
Skip 0
Take 0
GroupBy 
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
Skip 0
Take 0
GroupJoin System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],QueryAble`1[System.Int64],VB$AnonymousType_12`5[System.Int32,System.Byte,System.Int16,System.UInt32,QueryAble`1[System.Int64]]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],VB$AnonymousType_13`5[System.Int32,System.Byte,System.Int16,System.UInt32,QueryAble`1[System.Int64]]]
------
SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
SelectMany System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
SelectMany System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32,VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32]]
Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Int32]
Distinct
TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],System.Boolean]
Skip 0
Take 0
Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],VB$AnonymousType_14`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],QueryAble`1[System.Int64]]]
Select System.Func`2[VB$AnonymousType_14`2[VB$AnonymousType_4`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],System.UInt32],QueryAble`1[System.Int64]],VB$AnonymousType_15`6[System.Int32,System.Byte,System.Int16,System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Int64]]]
------
        ]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Join1() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off
        Option Infer On

        Imports System
        Imports System.Collections
        Imports System.Linq


        Module Module1
            Sub Main()
                Dim q0 As IEnumerable

                q0 = [||]From s1 In New Integer() {1, 3} Join s2 In New Integer() {2, 3} On s1 Equals s2

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1, 3} Join s2 In New Integer() {2, 3} On s2 + 1 Equals s1 + 2

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} On s1 + 1 Equals s2 Join s3 In New Integer() {4, 5} On s3 Equals s2 * 2

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} Join s3 In New Integer() {4, 5} On s3 Equals s2 * 2 On s1 + 1 Equals s2

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} On s1 + 1 Equals s2 Select s2, s1

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} On s1 + 1 Equals s2 Join s3 In New Integer() {4, 5} On s3 Equals s2 * 2 Select s3, s2, s1

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} Join s3 In New Integer() {4, 5} On s3 Equals s2 * 2 On s1 + 1 Equals s2 Select s3, s2, s1

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} On s1 + 1 Equals s2 Let s3 = s1 + s2

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} On s1 + 1 Equals s2 Join s3 In New Integer() {4, 5} On s3 Equals s2 * 2 Let s4 = s1 + s2 + s3

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} Join s3 In New Integer() {4, 5} On s3 Equals s2 * 2 On s1 + 1 Equals s2 Let s4 = s1 + s2 + s3

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1} Join s2 In New Integer() {2, 3} On s1 + 1 Equals s2 Let s3 = s1 + s2, s4 = s3 + 1

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1}
                     Join s2 In New Integer() {2, 3}
                     On s1 + 1 Equals s2
                     Join s3 In New Integer() {3, 4}
                     On s2 + 1 Equals s3
                     Join s4 In New Integer() {4, 5}
                         Join s5 In New Integer() {5, 6}
                         On s4 + 1 Equals s5
                         Join s6 In New Integer() {6, 7}
                         On s5 + 1 Equals s6
                     On s3 + 1 Equals s4

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1}
                     Join s2 In New Integer() {2, 3}
                     On s1 + 1 Equals s2
                     Join s3 In New Integer() {3, 4}
                     On s2 + 1 Equals s3
                     Join s4 In New Integer() {4, 5}
                         Join s5 In New Integer() {5, 6}
                         On s4 + 1 Equals s5
                         Join s6 In New Integer() {6, 7}
                         On s5 + 1 Equals s6
                     On s3 + 1 Equals s4
                     Select s1 + s2 + s3 + s4 + s5 + s6

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1}
                     Join s2 In New Integer() {2, 3}
                     On s1 + 1 Equals s2
                     Join s3 In New Integer() {3, 4}
                     On s2 + 1 Equals s3
                     Join s4 In New Integer() {4, 5}
                         Join s5 In New Integer() {5, 6}
                         On s4 + 1 Equals s5
                         Join s6 In New Integer() {6, 7}
                         On s5 + 1 Equals s6
                     On s3 + 1 Equals s4
                     Let s7 = s1 + s2 + s3 + s4 + s5 + s6

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New IComparable() {New Guid("F31A2538-E129-437E-AD69-B484F979246E")}
                     Join s2 In New Guid() {New Guid("F31A2538-E129-437E-AD69-B484F979246E")} On s1 Equals s2

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New String() {"1", "2"}
                     Join s2 In New Integer() {2, 3} On s1 Equals s2 - 1

                For Each v In q0
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                q0 = [||]From s1 In New Integer() {1, 2, 3, 4, 5}
                     Join s2 In New Integer() {1, 2, 3, 4, 5, 6, 7, 8, 9, 10} On s1 * 2 Equals s2
                     Join s3 In New Integer() {1, 2, 3, 4, 5, 6, 7, 8, 9, 10} On s1 + 1 Equals s3 And s2 - 1 Equals s3 + 1

                For Each v In q0
                    System.Console.WriteLine(v)
                Next
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef, additionalRefs:={LinqAssemblyRef},
                            expectedOutput:=
            <![CDATA[
        { s1 = 3, s2 = 3 }
        ------
        { s1 = 1, s2 = 2 }
        ------
        { s1 = 1, s2 = 2, s3 = 4 }
        ------
        { s1 = 1, s2 = 2, s3 = 4 }
        ------
        { s2 = 2, s1 = 1 }
        ------
        { s3 = 4, s2 = 2, s1 = 1 }
        ------
        { s3 = 4, s2 = 2, s1 = 1 }
        ------
        { s1 = 1, s2 = 2, s3 = 3 }
        ------
        { s1 = 1, s2 = 2, s3 = 4, s4 = 7 }
        ------
        { s1 = 1, s2 = 2, s3 = 4, s4 = 7 }
        ------
        { s1 = 1, s2 = 2, s3 = 3, s4 = 4 }
        ------
        { s1 = 1, s2 = 2, s3 = 3, s4 = 4, s5 = 5, s6 = 6 }
        ------
        21
        ------
        { s1 = 1, s2 = 2, s3 = 3, s4 = 4, s5 = 5, s6 = 6, s7 = 21 }
        ------
        { s1 = f31a2538-e129-437e-ad69-b484f979246e, s2 = f31a2538-e129-437e-ad69-b484f979246e }
        ------
        { s1 = 1, s2 = 2 }
        { s1 = 2, s2 = 3 }
        ------
        { s1 = 3, s2 = 6, s3 = 4 }
        ]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Join3() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble(Of T)
            Public ReadOnly v As Integer

            Sub New(v As Integer)
                Me.v = v
            End Sub

            Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
                System.Console.WriteLine("Select {0}", x)
                Return New QueryAble(Of S)(v + 1)
            End Function

            Public Function SelectMany(Of S, R)(m As Func(Of T, QueryAble(Of S)), x As Func(Of T, S, R)) As QueryAble(Of R)
                System.Console.WriteLine("SelectMany {0}", x)
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function Where(x As Func(Of T, Boolean)) As QueryAble(Of T)
                System.Console.WriteLine("Where {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function TakeWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
                System.Console.WriteLine("TakeWhile {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function SkipWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
                System.Console.WriteLine("SkipWhile {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function OrderBy(x As Func(Of T, Integer)) As QueryAble(Of T)
                System.Console.WriteLine("OrderBy {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Distinct() As QueryAble(Of T)
                System.Console.WriteLine("Distinct")
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Skip(count As Integer) As QueryAble(Of T)
                System.Console.WriteLine("Skip {0}", count)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Take(count As Integer) As QueryAble(Of T)
                System.Console.WriteLine("Take {0}", count)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
                System.Console.WriteLine("Join {0}", x)
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function GroupBy(Of K, I, R)(key As Func(Of T, K), item As Func(Of T, I), into As Func(Of K, QueryAble(Of I), R)) As QueryAble(Of R)
                System.Console.WriteLine("GroupBy {0}", item)
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of T), R)) As QueryAble(Of R)
                System.Console.WriteLine("GroupBy ")
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
                System.Console.WriteLine("GroupJoin {0}", x)
                Return New QueryAble(Of R)(v + 1)
            End Function

        End Class

        Module Module1

            Sub Main()
                Dim qi As New QueryAble(Of Integer)(0)
                Dim qb As New QueryAble(Of Byte)(0)
                Dim qs As New QueryAble(Of Short)(0)
                Dim qu As New QueryAble(Of UInteger)(0)
                Dim ql As New QueryAble(Of Long)(0)
                Dim qd As New QueryAble(Of Double)(0)

                Dim q0 As Object
                q0 = [||]From s1 In qi Join s2 In qb On s1 Equals s2
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Join s2 In qb
                     On s1 + 1 Equals s2
                     Join s3 In qs
                     On s2 + 1 Equals s3
                     Join s4 In qu
                         Join s5 In ql
                         On s4 + 1 Equals s5
                         Join s6 In qd
                         On s5 + 1 Equals s6
                     On s1 + s2 + s3 Equals s4 + s5 + s6

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Join s2 In qb
                     On s1 + 1 Equals s2
                     Join s3 In qs
                     On s2 + 1 Equals s3
                     Join s4 In qu
                         Join s5 In ql
                         On s4 + 1 Equals s5
                         Join s6 In qd
                         On s5 + 1 Equals s6
                     On s1 + s2 + s3 Equals s4 + s5 + s6
                     Select s6, s5, s4, s3, s2, s1

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Join s2 In qb
                     On s1 + 1 Equals s2
                     Join s3 In qs
                     On s2 + 1 Equals s3
                     Join s4 In qu
                         Join s5 In ql
                         On s4 + 1 Equals s5
                         Join s6 In qd
                         On s5 + 1 Equals s6
                     On s1 + s2 + s3 Equals s4 + s5 + s6
                     Let s7 = s6 + s5 + s4 + s3 + s2 + s1

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Join s2 In qb
                     On s1 + 1 Equals s2
                     Join s3 In qs
                     On s2 + 1 Equals s3
                     Join s4 In qu
                         Join s5 In ql
                         On s4 + 1 Equals s5
                         Join s6 In qd
                         On s5 + 1 Equals s6
                     On s1 + s2 + s3 Equals s4 + s5 + s6
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Join s2 In qb
                     On s1 + 1 Equals s2
                     Join s3 In qs
                     On s2 + 1 Equals s3
                     Join s4 In qu
                         Join s5 In ql
                         On s4 + 1 Equals s5
                         Join s6 In qd
                         On s5 + 1 Equals s6
                     On s1 + s2 + s3 Equals s4 + s5 + s6
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                     Select s6, s5, s4, s3, s2, s1

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Join s2 In qb
                     On s1 + 1 Equals s2
                     Join s3 In qs
                     On s2 + 1 Equals s3
                     Join s4 In qu
                         Join s5 In ql
                         On s4 + 1 Equals s5
                         Join s6 In qd
                         On s5 + 1 Equals s6
                     On s1 + s2 + s3 Equals s4 + s5 + s6
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                     Let s7 = s6 + s5 + s4 + s3 + s2 + s1

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Join s2 In qb
                     On s1 + 1 Equals s2
                     Join s3 In qs
                     On s2 + 1 Equals s3
                     Join s4 In qu
                         Join s5 In ql
                         On s4 + 1 Equals s5
                         Join s6 In qd
                         On s5 + 1 Equals s6
                     On s1 + s2 + s3 Equals s4 + s5 + s6
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                     Join s7 In qd On s1 Equals s7

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Join s2 In qb
                     On s1 + 1 Equals s2
                     Join s3 In qs
                     On s2 + 1 Equals s3
                     Join s4 In qu
                         Join s5 In ql
                         On s4 + 1 Equals s5
                         Join s6 In qd
                         On s5 + 1 Equals s6
                     On s1 + s2 + s3 Equals s4 + s5 + s6
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                     From s7 In qd 

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Join s2 In qb
                     On s1 + 1 Equals s2
                     Join s3 In qs
                     On s2 + 1 Equals s3
                     Join s4 In qu
                         Join s5 In ql
                         On s4 + 1 Equals s5
                         Join s6 In qd
                         On s5 + 1 Equals s6
                     On s1 + s2 + s3 Equals s4 + s5 + s6
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                     Group s1 By s2 Into Group

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Join s2 In qb
                     On s1 + 1 Equals s2
                     Join s3 In qs
                     On s2 + 1 Equals s3
                     Join s4 In qu
                         Join s5 In ql
                         On s4 + 1 Equals s5
                         Join s6 In qd
                         On s5 + 1 Equals s6
                     On s1 + s2 + s3 Equals s4 + s5 + s6
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                     Group By s2 Into Group

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Join s2 In qb
                     On s1 + 1 Equals s2
                     Join s3 In qs
                     On s2 + 1 Equals s3
                     Join s4 In qu
                         Join s5 In ql
                         On s4 + 1 Equals s5
                         Join s6 In qd
                         On s5 + 1 Equals s6
                     On s1 + s2 + s3 Equals s4 + s5 + s6
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                     Group Join s7 In qd On s1 Equals s7 Into Group

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Join s2 In qb
                     On s1 + 1 Equals s2
                     Join s3 In qs
                     On s2 + 1 Equals s3
                     Join s4 In qu
                         Join s5 In ql
                         On s4 + 1 Equals s5
                         Join s6 In qd
                         On s5 + 1 Equals s6
                     On s1 + s2 + s3 Equals s4 + s5 + s6
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                     Aggregate s7 In qd Into Where(True)

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Join s2 In qb
                     On s1 + 1 Equals s2
                     Join s3 In qs
                     On s2 + 1 Equals s3
                     Join s4 In qu
                         Join s5 In ql
                         On s4 + 1 Equals s5
                         Join s6 In qd
                         On s5 + 1 Equals s6
                     On s1 + s2 + s3 Equals s4 + s5 + s6
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                     Aggregate s7 In qd Into Where(True), Distinct
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
        Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
        ------
        Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
        Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
        Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
        Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
        Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double]]
        ------
        Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
        Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
        Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
        Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
        Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_5`6[System.Double,System.Int64,System.UInt32,System.Int16,System.Byte,System.Int32]]
        ------
        Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
        Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
        Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
        Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
        Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_6`7[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double,System.Double]]
        ------
        Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
        Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
        Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
        Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
        Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double]]
        Where System.Func`2[VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double],System.Boolean]
        Skip 0
        Take 0
        ------
        Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
        Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
        Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
        Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
        Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]]
        Where System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        Skip 0
        Take 0
        Select System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],VB$AnonymousType_5`6[System.Double,System.Int64,System.UInt32,System.Int16,System.Byte,System.Int32]]
        ------
        Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
        Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
        Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
        Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
        Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]]
        Where System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        Skip 0
        Take 0
        Select System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],VB$AnonymousType_6`7[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double,System.Double]]
        ------
        Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
        Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
        Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
        Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
        Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]]
        Where System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        Skip 0
        Take 0
        Join System.Func`3[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Double,VB$AnonymousType_6`7[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double,System.Double]]
        ------
        Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
        Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
        Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
        Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
        Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]]
        Where System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        Skip 0
        Take 0
        SelectMany System.Func`3[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Double,VB$AnonymousType_6`7[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double,System.Double]]
        ------
        Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
        Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
        Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
        Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
        Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]]
        Where System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        Skip 0
        Take 0
        GroupBy System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Int32]
        ------
        Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
        Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
        Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
        Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
        Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double]]
        Where System.Func`2[VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_2`6[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double],System.Boolean]
        Skip 0
        Take 0
        GroupBy 
        ------
        Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
        Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
        Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
        Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
        Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]]
        Where System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        Skip 0
        Take 0
        GroupJoin System.Func`3[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],QueryAble`1[System.Double],VB$AnonymousType_9`7[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double,QueryAble`1[System.Double]]]
        ------
        Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
        Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
        Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
        Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
        Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]]
        Where System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        Skip 0
        Take 0
        Select System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],VB$AnonymousType_10`7[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double,QueryAble`1[System.Double]]]
        ------
        Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_0`2[System.Int32,System.Byte]]
        Join System.Func`3[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16,VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16]]
        Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_3`2[System.UInt32,System.Int64]]
        Join System.Func`3[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]
        Join System.Func`3[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double],VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]]]
        Where System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],System.Boolean]
        Skip 0
        Take 0
        Select System.Func`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],VB$AnonymousType_11`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],QueryAble`1[System.Double]]]
        Select System.Func`2[VB$AnonymousType_11`2[VB$AnonymousType_7`2[VB$AnonymousType_1`2[VB$AnonymousType_0`2[System.Int32,System.Byte],System.Int16],VB$AnonymousType_4`2[VB$AnonymousType_3`2[System.UInt32,System.Int64],System.Double]],QueryAble`1[System.Double]],VB$AnonymousType_12`8[System.Int32,System.Byte,System.Int16,System.UInt32,System.Int64,System.Double,QueryAble`1[System.Double],QueryAble`1[System.Double]]]
        ]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function GroupBy1() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off
        Option Infer On

        Imports System
        Imports System.Collections
        Imports System.Linq


        Module Module1
            Sub Main()
                Dim q0 As IEnumerable

                For Each v In From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group By s1 Into Group
                    System.Console.WriteLine(v)
                    For Each gv In v.Group
                        System.Console.WriteLine(gv)
                    Next
                Next

                System.Console.WriteLine("------")

                For Each v In From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group By s1 Into Count()
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                For Each v In From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group s1 By s1 Into Group
                    System.Console.WriteLine(v)
                    For Each gv In v.Group
                        System.Console.WriteLine(gv)
                    Next
                Next

                System.Console.WriteLine("------")

                For Each v In From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group s1 By s1 Into Count()
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")

                For Each v In From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group s1, s1str = CStr(s1) By s1 = s1 Mod 2, s2 = s1 Mod 3 Into gr = Group, c = Count(), Max(s1)
                    System.Console.WriteLine(v)
                    For Each gv In v.gr
                        System.Console.WriteLine(gv)
                    Next
                Next

                System.Console.WriteLine("------")

                For Each v In From s1 In New Integer() {1, 2} Select s1 + 1 Group By key = 1 Into Group
                    System.Console.WriteLine(v)
                    For Each gv In v.Group
                        System.Console.WriteLine(gv)
                    Next
                Next

                System.Console.WriteLine("------")

                For Each v In From s1 In New Integer() {1, 2} Select s1 + 1 Group By key = 1 Into Group Join s1 In New Integer() {1, 2} On key Equals s1
                    System.Console.WriteLine(v)
                    For Each gv In v.Group
                        System.Console.WriteLine(gv)
                    Next
                Next

                System.Console.WriteLine("------")

                For Each v In From s1 In New Integer() {1, 2, 3, 4, 2, 3} Group s1 By s1 Into Count(s1 - 2)
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef, additionalRefs:={LinqAssemblyRef},
                            expectedOutput:=
            <![CDATA[
        { s1 = 1, Group = System.Int32[] }
        1
        { s1 = 2, Group = System.Int32[] }
        2
        2
        { s1 = 3, Group = System.Int32[] }
        3
        3
        { s1 = 4, Group = System.Int32[] }
        4
        ------
        { s1 = 1, Count = 1 }
        { s1 = 2, Count = 2 }
        { s1 = 3, Count = 2 }
        { s1 = 4, Count = 1 }
        ------
        { s1 = 1, Group = System.Int32[] }
        1
        { s1 = 2, Group = System.Int32[] }
        2
        2
        { s1 = 3, Group = System.Int32[] }
        3
        3
        { s1 = 4, Group = System.Int32[] }
        4
        ------
        { s1 = 1, Count = 1 }
        { s1 = 2, Count = 2 }
        { s1 = 3, Count = 2 }
        { s1 = 4, Count = 1 }
        ------
        { s1 = 1, s2 = 1, gr = VB$AnonymousType_2`2[System.Int32,System.String][], c = 1, Max = 1 }
        { s1 = 1, s1str = 1 }
        { s1 = 0, s2 = 2, gr = VB$AnonymousType_2`2[System.Int32,System.String][], c = 2, Max = 2 }
        { s1 = 2, s1str = 2 }
        { s1 = 2, s1str = 2 }
        { s1 = 1, s2 = 0, gr = VB$AnonymousType_2`2[System.Int32,System.String][], c = 2, Max = 3 }
        { s1 = 3, s1str = 3 }
        { s1 = 3, s1str = 3 }
        { s1 = 0, s2 = 1, gr = VB$AnonymousType_2`2[System.Int32,System.String][], c = 1, Max = 4 }
        { s1 = 4, s1str = 4 }
        ------
        { key = 1, Group = System.Int32[] }
        2
        3
        ------
        { key = 1, Group = System.Int32[], s1 = 1 }
        2
        3
        ------
        { s1 = 1, Count = 1 }
        { s1 = 2, Count = 0 }
        { s1 = 3, Count = 2 }
        { s1 = 4, Count = 1 }
        ------
        ]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function GroupBy5() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict On

        Imports System

        Class QueryAble(Of T)
            Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
                System.Console.WriteLine("Select {0}", x)
                Return New QueryAble(Of S)()
            End Function

            Public Function GroupBy(Of K, I, R)(key As Func(Of T, K), item As Func(Of T, I), into As Func(Of K, QueryAble(Of I), R)) As QueryAble(Of R)
                System.Console.WriteLine("GroupBy {0}", item)
                System.Console.WriteLine("        {0}", key)
                System.Console.WriteLine("        {0}", into)
                Return New QueryAble(Of R)()
            End Function
        End Class

        Module Module1
            Sub Main()
                Dim q1 As New QueryAble(Of Integer)()
                Dim q As Object

                q = [||]From s In q1 Group Nothing By key = Nothing Into Group
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
        GroupBy System.Func`2[System.Int32,System.Object]
                System.Func`2[System.Int32,System.Object]
                System.Func`3[System.Object,QueryAble`1[System.Object],VB$AnonymousType_0`2[System.Object,QueryAble`1[System.Object]]]
        ]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function GroupJoin1() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off
        Option Infer On

        Imports System
        Imports System.Collections
        Imports System.Linq


        Module Module1
            Sub Main()

                For Each v In From s1 In New Integer() {1, 3} Group Join s2 In New Integer() {2, 3} On s1 Equals s2 Into Group
                    System.Console.WriteLine(v)
                    For Each gv In v.Group
                        System.Console.WriteLine("    {0}", gv)
                    Next
                Next

                System.Console.WriteLine("------")

                For Each v In From s1 In New Integer() {1, 3} Group Join s2 In New Integer() {2, 3} On s2 + 1 Equals s1 + 2 Into Group
                    System.Console.WriteLine(v)
                    For Each gv In v.Group
                        System.Console.WriteLine("    {0}", gv)
                    Next
                Next

                System.Console.WriteLine("------")

                For Each v In From s1 In New Integer() {1} Group Join s2 In New Integer() {2, 3} On s1 + 1 Equals s2 Into gr1 = Group Group Join s3 In New Integer() {4, 5} On s3 Equals (s1 + 1) * 2 Into gr2 = Group
                    System.Console.WriteLine(v)
                    For Each gv In v.gr1
                        System.Console.WriteLine("    {0}", gv)
                    Next
                    For Each gv In v.gr2
                        System.Console.WriteLine("        {0}", gv)
                    Next
                Next

                System.Console.WriteLine("------")

                For Each v In From s1 In New Integer() {1} Group Join s2 In New Integer() {2, 3} Group Join s3 In New Integer() {4, 5} On s3 Equals s2 * 2 Into gr1 = Group On s1 + 1 Equals s2 Into gr2 = Group
                    System.Console.WriteLine(v)
                    For Each gr2 In v.gr2
                        System.Console.WriteLine("        {0}", gr2)
                        For Each gr1 In gr2.gr1
                            System.Console.WriteLine("    {0}", gr1)
                        Next
                    Next
                Next

                System.Console.WriteLine("------")

                For Each v In From s1 In New Integer() {1}
                                 Group Join s2 In New Integer() {2, 3}
                                 On s1 + 1 Equals s2 Into g1 = Group
                                 Group Join s3 In New Integer() {3, 4}
                                 On s1 + 2 Equals s3 Into g2 = Group
                                 Group Join s4 In New Integer() {4, 5}
                                     Group Join s5 In New Integer() {5, 6}
                                     On s4 + 1 Equals s5 Into g3 = Group
                                     Group Join s6 In New Integer() {6, 7}
                                     On s4 + 2 Equals s6 Into g4 = Group
                                 On s1 + 3 Equals s4 Into g5 = Group

                    System.Console.WriteLine(v)
                    For Each gr1 In v.g1
                        System.Console.WriteLine("    {0}", gr1)
                    Next
                    For Each gr2 In v.g2
                        System.Console.WriteLine("        {0}", gr2)
                    Next
                    For Each gr5 In v.g5
                        System.Console.WriteLine("                        {0}", gr5)
                        For Each gr3 In gr5.g3
                            System.Console.WriteLine("            {0}", gr3)
                        Next
                        For Each gr4 In gr5.g4
                            System.Console.WriteLine("                {0}", gr4)
                        Next
                    Next
                Next

                System.Console.WriteLine("------")

                For Each v In From s1 In From s1 In New Integer() {1}
                                         Group Join
                                             s2 In New Integer() {1}
                                                 Join
                                                     s3 In New Integer() {1}
                                                 On s2 Equals s3
                                                 Join
                                                     s4 In New Integer() {1}
                                                 On s2 Equals s4
                                         On s1 Equals s2 Into s3 = Group

                    System.Console.WriteLine(v)
                    For Each gv In v.s3
                        System.Console.WriteLine("    {0}", gv)
                    Next
                Next

                System.Console.WriteLine("------")

                For Each v In From s In New Integer() {1, 2}
                              Group Join
                                  s1 In New Integer() {1, 2}
                                  Group Join
                                      s In New Integer() {1, 2}
                                      Group Join
                                          s1 In New Integer() {1, 2}
                                          Group Join
                                              s In New Integer() {1, 2}
                                          On s Equals s1 Into Group
                                      On s Equals s1 Into Group
                                  On s Equals s1 Into Group
                              On s Equals s1 Into Group

                    System.Console.WriteLine(v)
                    For Each g1 In v.Group
                        System.Console.WriteLine("    {0}", g1)
                        For Each g2 In g1.Group
                            System.Console.WriteLine("        {0}", g2)
                            For Each g3 In g2.Group
                                System.Console.WriteLine("            {0}", g3)
                                For Each g4 In g3.Group
                                    System.Console.WriteLine("                {0}", g4)
                                Next
                            Next
                        Next
                    Next
                Next

                System.Console.WriteLine("------")

                For Each v In From s In New Integer() {1, 2}
                              Join
                                  s1 In New Integer() {1, 2}
                                  Join
                                      s2 In New Integer() {1, 2}
                                      Group Join
                                          s1 In New Integer() {1, 2}
                                          Group Join
                                              s In New Integer() {1, 2}
                                          On s Equals s1 Into Group
                                      On s2 Equals s1 Into Group
                                  On s2 Equals s1
                              On s Equals s1

                    System.Console.WriteLine(v)
                    For Each g1 In v.Group
                        System.Console.WriteLine("    {0}", g1)
                        For Each g2 In g1.Group
                            System.Console.WriteLine("        {0}", g2)
                        Next
                    Next
                Next

                System.Console.WriteLine("------")
                For Each v In From x In New Integer() {1, 2} Group Join y In New Integer() {0, 3, 4} On x + 1 Equals y Into Count(y + x), Group
                    System.Console.WriteLine(v)
                    For Each gv In v.Group
                        System.Console.WriteLine("    {0}", gv)
                    Next
                Next

            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef, additionalRefs:={LinqAssemblyRef},
                            expectedOutput:=
            <![CDATA[
        { s1 = 1, Group = System.Int32[] }
        { s1 = 3, Group = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
            3
        ------
        { s1 = 1, Group = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
            2
        { s1 = 3, Group = System.Int32[] }
        ------
        { s1 = 1, gr1 = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32], gr2 = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
            2
                4
        ------
        { s1 = 1, gr2 = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_4`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]] }
                { s2 = 2, gr1 = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
            4
        ------
        { s1 = 1, g1 = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32], g2 = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32], g5 = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_9`3[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32],System.Collections.Generic.IEnumerable`1[System.Int32]]] }
            2
                3
                                { s4 = 4, g3 = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32], g4 = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
                    5
                        6
        ------
        { s1 = 1, s3 = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_12`3[System.Int32,System.Int32,System.Int32]] }
            { s2 = 1, s3 = 1, s4 = 1 }
        ------
        { s = 1, Group = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[VB$AnonymousType_13`2[System.Int32,System.Collections.Generic.IEnumerable`1[VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]]]]]] }
            { s1 = 1, Group = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_13`2[System.Int32,System.Collections.Generic.IEnumerable`1[VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]]]] }
                { s = 1, Group = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]] }
                    { s1 = 1, Group = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
                        1
        { s = 2, Group = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[VB$AnonymousType_13`2[System.Int32,System.Collections.Generic.IEnumerable`1[VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]]]]]] }
            { s1 = 2, Group = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_13`2[System.Int32,System.Collections.Generic.IEnumerable`1[VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]]]] }
                { s = 2, Group = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]] }
                    { s1 = 2, Group = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
                        2
        ------
        { s = 1, s1 = 1, s2 = 1, Group = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]] }
            { s1 = 1, Group = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
                1
        { s = 2, s1 = 2, s2 = 2, Group = System.Linq.Lookup`2+Grouping[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Collections.Generic.IEnumerable`1[System.Int32]]] }
            { s1 = 2, Group = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
                2
        ------
        { x = 1, Count = 0, Group = System.Int32[] }
        { x = 2, Count = 1, Group = System.Linq.Lookup`2+Grouping[System.Int32,System.Int32] }
            3
        ]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function GroupJoin3() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class QueryAble(Of T)
            Public ReadOnly v As Integer

            Sub New(v As Integer)
                Me.v = v
            End Sub

            Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
                System.Console.WriteLine("Select {0}", x)
                Return New QueryAble(Of S)(v + 1)
            End Function

            Public Function SelectMany(Of S, R)(m As Func(Of T, QueryAble(Of S)), x As Func(Of T, S, R)) As QueryAble(Of R)
                System.Console.WriteLine("SelectMany {0}", x)
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function Where(x As Func(Of T, Boolean)) As QueryAble(Of T)
                System.Console.WriteLine("Where {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function TakeWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
                System.Console.WriteLine("TakeWhile {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function SkipWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
                System.Console.WriteLine("SkipWhile {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function OrderBy(x As Func(Of T, Integer)) As QueryAble(Of T)
                System.Console.WriteLine("OrderBy {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Distinct() As QueryAble(Of T)
                System.Console.WriteLine("Distinct")
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Skip(count As Integer) As QueryAble(Of T)
                System.Console.WriteLine("Skip {0}", count)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Take(count As Integer) As QueryAble(Of T)
                System.Console.WriteLine("Take {0}", count)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
                System.Console.WriteLine("Join {0}", x)
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function GroupBy(Of K, I, R)(key As Func(Of T, K), item As Func(Of T, I), into As Func(Of K, QueryAble(Of I), R)) As QueryAble(Of R)
                System.Console.WriteLine("GroupBy {0}", item)
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of T), R)) As QueryAble(Of R)
                System.Console.WriteLine("GroupBy ")
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
                System.Console.WriteLine("GroupJoin {0}", x)
                Return New QueryAble(Of R)(v + 1)
            End Function

        End Class

        Module Module1

            Sub Main()
                Dim qi As New QueryAble(Of Integer)(0)
                Dim qb As New QueryAble(Of Byte)(0)
                Dim qs As New QueryAble(Of Short)(0)
                Dim qu As New QueryAble(Of UInteger)(0)
                Dim ql As New QueryAble(Of Long)(0)
                Dim qd As New QueryAble(Of Double)(0)

                Dim q0 As Object
                q0 = [||]From s1 In qi Group Join s2 In qb On s1 Equals s2 Into Group
                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Group Join s2 In qb
                     On s1 + 1 Equals s2 Into g1 = Group
                     Group Join s3 In qs
                     On s1 + 2 Equals s3 Into g2 = Group
                     Group Join s4 In qu
                         Group Join s5 In ql
                         On s4 + 1 Equals s5 Into g3 = Group
                         Group Join s6 In qd
                         On s4 + 2 Equals s6 Into g4 = Group
                     On s1 Equals s4 Into g5 = Group

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Group Join s2 In qb
                     On s1 + 1 Equals s2 Into g1 = Group
                     Group Join s3 In qs
                     On s1 + 2 Equals s3 Into g2 = Group
                     Group Join s4 In qu
                         Group Join s5 In ql
                         On s4 + 1 Equals s5 Into g3 = Group
                         Group Join s6 In qd
                         On s4 + 2 Equals s6 Into g4 = Group
                     On s1 Equals s4 Into g5 = Group
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Group Join s2 In qb
                     On s1 + 1 Equals s2 Into g1 = Group
                     Group Join s3 In qs
                     On s1 + 2 Equals s3 Into g2 = Group
                     Group Join s4 In qu
                         Group Join s5 In ql
                         On s4 + 1 Equals s5 Into g3 = Group
                         Group Join s6 In qd
                         On s4 + 2 Equals s6 Into g4 = Group
                     On s1 Equals s4 Into g5 = Group
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                     Select g1, g2, g5, s1

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Group Join s2 In qb
                     On s1 + 1 Equals s2 Into g1 = Group
                     Group Join s3 In qs
                     On s1 + 2 Equals s3 Into g2 = Group
                     Group Join s4 In qu
                         Group Join s5 In ql
                         On s4 + 1 Equals s5 Into g3 = Group
                         Group Join s6 In qd
                         On s4 + 2 Equals s6 Into g4 = Group
                     On s1 Equals s4 Into g5 = Group
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                     Let s7 = s1

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Group Join s2 In qb
                     On s1 + 1 Equals s2 Into g1 = Group
                     Group Join s3 In qs
                     On s1 + 2 Equals s3 Into g2 = Group
                     Group Join s4 In qu
                         Group Join s5 In ql
                         On s4 + 1 Equals s5 Into g3 = Group
                         Group Join s6 In qd
                         On s4 + 2 Equals s6 Into g4 = Group
                     On s1 Equals s4 Into g5 = Group
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                     Join s7 In qd On s1 Equals s7

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Group Join s2 In qb
                     On s1 + 1 Equals s2 Into g1 = Group
                     Group Join s3 In qs
                     On s1 + 2 Equals s3 Into g2 = Group
                     Group Join s4 In qu
                         Group Join s5 In ql
                         On s4 + 1 Equals s5 Into g3 = Group
                         Group Join s6 In qd
                         On s4 + 2 Equals s6 Into g4 = Group
                     On s1 Equals s4 Into g5 = Group
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                     From s7 In qd

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Group Join s2 In qb
                     On s1 + 1 Equals s2 Into g1 = Group
                     Group Join s3 In qs
                     On s1 + 2 Equals s3 Into g2 = Group
                     Group Join s4 In qu
                         Group Join s5 In ql
                         On s4 + 1 Equals s5 Into g3 = Group
                         Group Join s6 In qd
                         On s4 + 2 Equals s6 Into g4 = Group
                     On s1 Equals s4 Into g5 = Group
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                     Group s1 By s2 = s1 Into Group

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Group Join s2 In qb
                     On s1 + 1 Equals s2 Into g1 = Group
                     Group Join s3 In qs
                     On s1 + 2 Equals s3 Into g2 = Group
                     Group Join s4 In qu
                         Group Join s5 In ql
                         On s4 + 1 Equals s5 Into g3 = Group
                         Group Join s6 In qd
                         On s4 + 2 Equals s6 Into g4 = Group
                     On s1 Equals s4 Into g5 = Group
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                     Group By s2 = s1 Into Group

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Group Join s2 In qb
                     On s1 + 1 Equals s2 Into g1 = Group
                     Group Join s3 In qs
                     On s1 + 2 Equals s3 Into g2 = Group
                     Group Join s4 In qu
                         Group Join s5 In ql
                         On s4 + 1 Equals s5 Into g3 = Group
                         Group Join s6 In qd
                         On s4 + 2 Equals s6 Into g4 = Group
                     On s1 Equals s4 Into g5 = Group
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                     Group Join s7 In qd On s1 Equals s7 Into Group

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Group Join s4 In qu
                         Group Join s5 In ql
                         On s4 + 1 Equals s5 Into g3 = Group
                         Group Join s6 In qd
                         On s4 + 2 Equals s6 Into g4 = Group
                         Group Join s3 In qd
                         On s4 Equals s3 Into g2 = Group
                     On s1 Equals s4 Into g5 = Where(True)

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Group Join s4 In qu
                         Join s5 In ql
                         On s4 + 1 Equals s5
                         Join s6 In qd
                         On s4 + 2 Equals s6
                         Join s3 In qd
                         On s4 Equals s3
                     On s1 Equals s4 Into g5 = Where(True)

                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Group Join s2 In qb
                     On s1 + 1 Equals s2 Into g1 = Group
                     Group Join s3 In qs
                     On s1 + 2 Equals s3 Into g2 = Group
                     Group Join s4 In qu
                         Group Join s5 In ql
                         On s4 + 1 Equals s5 Into g3 = Group
                         Group Join s6 In qd
                         On s4 + 2 Equals s6 Into g4 = Group
                     On s1 Equals s4 Into g5 = Group
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                     Aggregate s7 In qd Into Where(True)


                System.Console.WriteLine("------")
                q0 = [||]From s1 In qi
                     Group Join s2 In qb
                     On s1 + 1 Equals s2 Into g1 = Group
                     Group Join s3 In qs
                     On s1 + 2 Equals s3 Into g2 = Group
                     Group Join s4 In qu
                         Group Join s5 In ql
                         On s4 + 1 Equals s5 Into g3 = Group
                         Group Join s6 In qd
                         On s4 + 2 Equals s6 Into g4 = Group
                     On s1 Equals s4 Into g5 = Group
                     Where True Order By s1 Distinct Take While True Skip While False Skip 0 Take 0
                     Aggregate s7 In qd Into Where(True), Distinct
            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
        GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_0`2[System.Int32,QueryAble`1[System.Byte]]]
        ------
        GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
        GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
        GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
        GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
        GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
        ------
        GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
        GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
        GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
        GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
        GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
        Where System.Func`2[VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        Skip 0
        Take 0
        ------
        GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
        GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
        GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
        GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
        GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
        Where System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        Skip 0
        Take 0
        Select System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],VB$AnonymousType_7`4[QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],System.Int32]]
        ------
        GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
        GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
        GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
        GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
        GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
        Where System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        Skip 0
        Take 0
        Select System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],VB$AnonymousType_8`5[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],System.Int32]]
        ------
        GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
        GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
        GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
        GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
        GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
        Where System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        Skip 0
        Take 0
        Join System.Func`3[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Double,VB$AnonymousType_8`5[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],System.Double]]
        ------
        GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
        GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
        GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
        GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
        GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
        Where System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        Skip 0
        Take 0
        SelectMany System.Func`3[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Double,VB$AnonymousType_8`5[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],System.Double]]
        ------
        GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
        GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
        GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
        GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
        GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
        Where System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        Skip 0
        Take 0
        GroupBy System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
        ------
        GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
        GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
        GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
        GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
        GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
        Where System.Func`2[VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        Skip 0
        Take 0
        GroupBy 
        ------
        GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
        GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
        GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
        GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
        GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
        Where System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        Skip 0
        Take 0
        GroupJoin System.Func`3[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],QueryAble`1[System.Double],VB$AnonymousType_10`5[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],QueryAble`1[System.Double]]]
        ------
        GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
        GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_12`2[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double]]]
        GroupJoin System.Func`3[VB$AnonymousType_12`2[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double]],QueryAble`1[System.Double],VB$AnonymousType_13`4[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double],QueryAble`1[System.Double]]]
        GroupJoin System.Func`3[System.Int32,QueryAble`1[VB$AnonymousType_13`4[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double],QueryAble`1[System.Double]]],VB$AnonymousType_11`2[System.Int32,QueryAble`1[VB$AnonymousType_13`4[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double],QueryAble`1[System.Double]]]]]
        ------
        Join System.Func`3[System.UInt32,System.Int64,VB$AnonymousType_14`2[System.UInt32,System.Int64]]
        Join System.Func`3[VB$AnonymousType_14`2[System.UInt32,System.Int64],System.Double,VB$AnonymousType_15`2[VB$AnonymousType_14`2[System.UInt32,System.Int64],System.Double]]
        Join System.Func`3[VB$AnonymousType_15`2[VB$AnonymousType_14`2[System.UInt32,System.Int64],System.Double],System.Double,VB$AnonymousType_16`4[System.UInt32,System.Int64,System.Double,System.Double]]
        GroupJoin System.Func`3[System.Int32,QueryAble`1[VB$AnonymousType_16`4[System.UInt32,System.Int64,System.Double,System.Double]],VB$AnonymousType_11`2[System.Int32,QueryAble`1[VB$AnonymousType_16`4[System.UInt32,System.Int64,System.Double,System.Double]]]]
        ------
        GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
        GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
        GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
        GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
        GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
        Where System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        Skip 0
        Take 0
        Select System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],VB$AnonymousType_17`5[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],QueryAble`1[System.Double]]]
        ------
        GroupJoin System.Func`3[System.Int32,QueryAble`1[System.Byte],VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]]]
        GroupJoin System.Func`3[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
        GroupJoin System.Func`3[System.UInt32,QueryAble`1[System.Int64],VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]]]
        GroupJoin System.Func`3[VB$AnonymousType_4`2[System.UInt32,QueryAble`1[System.Int64]],QueryAble`1[System.Double],VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]
        GroupJoin System.Func`3[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]]]
        Where System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],System.Boolean]
        Skip 0
        Take 0
        Select System.Func`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],VB$AnonymousType_18`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],QueryAble`1[System.Double]]]
        Select System.Func`2[VB$AnonymousType_18`2[VB$AnonymousType_6`2[VB$AnonymousType_2`2[VB$AnonymousType_1`2[System.Int32,QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]]],QueryAble`1[System.Double]],VB$AnonymousType_19`6[System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[VB$AnonymousType_5`3[System.UInt32,QueryAble`1[System.Int64],QueryAble`1[System.Double]]],QueryAble`1[System.Double],QueryAble`1[System.Double]]]
        ]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Aggregate1() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off
        Option Infer On

        Imports System
        Imports System.Collections
        Imports System.Linq


        Module Module1
            Sub Main()
                System.Console.WriteLine(Aggregate y In New Integer() {3, 4} Into Count())
                System.Console.WriteLine(Aggregate y In New Integer() {3, 4} Into Count(), Sum(y \ 2))
                System.Console.WriteLine(Aggregate x In New Integer() {3, 4}, y In New Integer() {1, 3} Where x > y Into Sum(x + y))

                System.Console.WriteLine("------")
                For Each v In From x In New Integer() {3, 4} Select x + 1 Aggregate y In New Integer() {3, 4} Into Count()
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")
                For Each v In From x In New Integer() {3, 4} Select x + 1 Aggregate y In New Integer() {3, 4} Into Count(), Sum()
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")
                For Each v In From x In New Integer()() {New Integer() {3, 4}} Aggregate y In x Into Sum()
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")
                For Each v In From x In New Integer()() {New Integer() {3, 4}} Aggregate y In x Into Sum(), Count()
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")
                For Each v In From x In New Integer()() {New Integer() {3, 4}} From z In x Aggregate y In x Into Sum(z + y)
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")
                For Each v In From x In New Integer()() {New Integer() {3, 4}} From z In x Aggregate y In x Into Sum(z + y), Count()
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")
                For Each v In Aggregate x In New Integer() {1}, y In New Integer() {2}, z In New Integer() {3} Into Where(True)
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")
                For Each v In From x In New Integer() {3, 4} Select x + 1 Aggregate x In New Integer() {1}, y In New Integer() {2}, z In New Integer() {3} Into Where(True)
                    For Each vv In v
                        System.Console.WriteLine(vv)
                    Next
                Next

                System.Console.WriteLine("------")
                For Each v In Aggregate x In New Integer() {1}, y In New Integer() {2}, z In New Integer() {3}
                                  Where True Order By x Distinct Take While True Skip While False Skip 0 Take 100
                                  Select x, y, z Let w = x + y + z
                              Into Where(True)
                    System.Console.WriteLine(v)
                Next

                System.Console.WriteLine("------")
                For Each v In From x In New Integer() {3, 4} Select x + 1
                              Aggregate x In New Integer() {1}, y In New Integer() {2}, z In New Integer() {3}
                                  Where True Order By x Distinct Take While True Skip While False Skip 0 Take 100
                                  Select x, y, z Let w = x + y + z
                              Into Where(True)
                    For Each vv In v
                        System.Console.WriteLine(vv)
                    Next
                Next

            End Sub
        End Module
            </file>
</compilation>

            Await Test(compilationDef, additionalRefs:={LinqAssemblyRef},
                            expectedOutput:=
            <![CDATA[
        2
        { Count = 2, Sum = 3 }
        16
        ------
        2
        2
        ------
        { Count = 2, Sum = 7 }
        { Count = 2, Sum = 7 }
        ------
        { x = System.Int32[], Sum = 7 }
        ------
        { x = System.Int32[], Sum = 7, Count = 2 }
        ------
        { x = System.Int32[], z = 3, Sum = 13 }
        { x = System.Int32[], z = 4, Sum = 15 }
        ------
        { x = System.Int32[], z = 3, Sum = 13, Count = 2 }
        { x = System.Int32[], z = 4, Sum = 15, Count = 2 }
        ------
        { x = 1, y = 2, z = 3 }
        ------
        { x = 1, y = 2, z = 3 }
        { x = 1, y = 2, z = 3 }
        ------
        { x = 1, y = 2, z = 3, w = 6 }
        ------
        { x = 1, y = 2, z = 3, w = 6 }
        { x = 1, y = 2, z = 3, w = 6 }
        ]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Aggregate3() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb"><![CDATA[
        Option Strict Off

        Imports System

        Class QueryAble(Of T)
            'Inherits Base

            'Public Shadows [Select] As Byte
            Public ReadOnly v As Integer

            Sub New(v As Integer)
                Me.v = v
            End Sub

            Public Function [Select](Of S)(x As Func(Of T, S)) As QueryAble(Of S)
                System.Console.WriteLine("Select {0}", x)
                Return New QueryAble(Of S)(v + 1)
            End Function

            Public Function SelectMany(Of S, R)(m As Func(Of T, QueryAble(Of S)), x As Func(Of T, S, R)) As QueryAble(Of R)
                System.Console.WriteLine("SelectMany {0}", x)
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function Where(x As Func(Of T, Boolean)) As QueryAble(Of T)
                System.Console.WriteLine("Where {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function TakeWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
                System.Console.WriteLine("TakeWhile {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function SkipWhile(x As Func(Of T, Boolean)) As QueryAble(Of T)
                System.Console.WriteLine("SkipWhile {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function OrderBy(x As Func(Of T, Integer)) As QueryAble(Of T)
                System.Console.WriteLine("OrderBy {0}", x)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Distinct() As QueryAble(Of T)
                System.Console.WriteLine("Distinct")
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Skip(count As Integer) As QueryAble(Of T)
                System.Console.WriteLine("Skip {0}", count)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Take(count As Integer) As QueryAble(Of T)
                System.Console.WriteLine("Take {0}", count)
                Return New QueryAble(Of T)(v + 1)
            End Function

            Public Function Join(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, I, R)) As QueryAble(Of R)
                System.Console.WriteLine("Join {0}", x)
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function GroupBy(Of K, I, R)(key As Func(Of T, K), item As Func(Of T, I), into As Func(Of K, QueryAble(Of I), R)) As QueryAble(Of R)
                System.Console.WriteLine("GroupBy {0}", item)
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function GroupBy(Of K, R)(key As Func(Of T, K), into As Func(Of K, QueryAble(Of T), R)) As QueryAble(Of R)
                System.Console.WriteLine("GroupBy ")
                Return New QueryAble(Of R)(v + 1)
            End Function

            Public Function GroupJoin(Of I, K, R)(inner As QueryAble(Of I), outerKey As Func(Of T, K), innerKey As Func(Of I, K), x As Func(Of T, QueryAble(Of I), R)) As QueryAble(Of R)
                System.Console.WriteLine("GroupJoin {0}", x)
                Return New QueryAble(Of R)(v + 1)
            End Function

        End Class

        Module Module1
            Sub Main()
                Dim qi As New QueryAble(Of Integer)(0)
                Dim qb As New QueryAble(Of Byte)(0)
                Dim qs As New QueryAble(Of Short)(0)

                Dim q0 As Object

                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True)
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True), Distinct()
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True)
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
                     Select Where, t, s

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True), Distinct()
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
                     Select Distinct, Where, t, s

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True)
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
                     Let t4 = 1

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True), Distinct()
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
                     Let t4 = 1

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True)
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
                     From t4 In qs

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True), Distinct()
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
                     From t4 In qs

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True)
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
                     Join t4 In qs On s Equals t4

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True), Distinct()
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
                     Join t4 In qs On s Equals t4

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True)
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
                     Group s By t Into Group

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True), Distinct()
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
                     Group s By t Into Group

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True)
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
                     Group By t Into Group

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True), Distinct()
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
                     Group By t Into Group

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True)
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
                     Group Join t4 In qs On t Equals t4 Into Group

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True), Distinct()
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
                     Group Join t4 In qs On t Equals t4 Into Group

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True)
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
                     Aggregate t4 In qs Into w = Where(True)

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True), Distinct()
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
                     Aggregate t4 In qs Into w = Where(True)

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True)
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
                     Aggregate t4 In qs Into w = Where(True), d = Distinct()

                System.Console.WriteLine("------")
                q0 = [||]From s In qi Let t = s + 1
                     Aggregate x In qb Into Where(True), Distinct()
                     Where True Order By s Distinct Take While True Skip While False Skip 0 Take 0
                     Aggregate t4 In qs Into w = Where(True), d = Distinct()

                System.Console.WriteLine("------")
                q0 = [||]From i In qi, b In qb
                     Aggregate s In qs Where s > i AndAlso s < b Into Where(s > i AndAlso s < b)

                System.Console.WriteLine("------")
                q0 = [||]From i In qi, b In qb
                     Aggregate s In qs Where s > i AndAlso s < b Into Where(s > i AndAlso s < b), Distinct()

                System.Console.WriteLine("------")
                q0 = [||]From i In qi Join b In qb On b Equals i
                     Aggregate s In qs Where s > i AndAlso s < b Into Where(s > i AndAlso s < b)

                System.Console.WriteLine("------")
                q0 = [||]From i In qi Join b In qb On b Equals i
                     Aggregate s In qs Where s > i AndAlso s < b Into Where(s > i AndAlso s < b), Distinct()

                System.Console.WriteLine("------")
                q0 = [||]From i In qi Select i + 1 From b In qb
                Aggregate s In qs Where s < b Into Where(s < b)

                System.Console.WriteLine("------")
                q0 = [||]From i In qi Select i + 1 From b In qb
                Aggregate s In qs Where s < b Into Where(s < b), Distinct()

                System.Console.WriteLine("------")
                q0 = [||]From i In qi Join b In qb On b Equals i From ii As Long In qi
                     Aggregate s In qs Where s < b Into Where(s < b)
                     Select Where, ii, b, i

                System.Console.WriteLine("------")
                q0 = [||]From i In qi Join b In qb On b Equals i From ii As Long In qi
                     Aggregate s In qs Where s < b Into Where(s < b), Distinct()
                     Select Distinct, Where, ii, b, i

                System.Console.WriteLine("------")
                q0 = [||]From i In qi Join b In qb Join ii As Long In qi On b Equals ii On b Equals i
                     Aggregate s In qs Where s < b Into Where(s < b)
                     Select Where, ii, b, i

                System.Console.WriteLine("------")
                q0 = [||]From i In qi Join b In qb Join ii As Long In qi On b Equals ii On b Equals i
                     Aggregate s In qs Where s < b Into Where(s < b), Distinct()
                     Select Distinct, Where, ii, b, i
            End Sub
        End Module
            ]]></file>
</compilation>

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
        Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_5`3[QueryAble`1[System.Byte],System.Int32,System.Int32]]
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
        Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        Select System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],VB$AnonymousType_7`4[QueryAble`1[System.Byte],QueryAble`1[System.Byte],System.Int32,System.Int32]]
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_8`4[System.Int32,System.Int32,QueryAble`1[System.Byte],System.Int32]]
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
        Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        Select System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],VB$AnonymousType_9`5[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte],System.Int32]]
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        SelectMany System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int16,VB$AnonymousType_8`4[System.Int32,System.Int32,QueryAble`1[System.Byte],System.Int16]]
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
        Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        SelectMany System.Func`3[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int16,VB$AnonymousType_9`5[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte],System.Int16]]
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        Join System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int16,VB$AnonymousType_8`4[System.Int32,System.Int32,QueryAble`1[System.Byte],System.Int16]]
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
        Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        Join System.Func`3[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int16,VB$AnonymousType_9`5[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte],System.Int16]]
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        GroupBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int32]
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
        Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        GroupBy System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_1`3[System.Int32,System.Int32,QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        GroupBy 
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
        Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_3`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        GroupBy 
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        GroupJoin System.Func`3[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_11`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16]]]
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
        Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        GroupJoin System.Func`3[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],QueryAble`1[System.Int16],VB$AnonymousType_12`5[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte],QueryAble`1[System.Int16]]]
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_13`4[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16]]]
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
        Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        Select System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],VB$AnonymousType_14`5[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte],QueryAble`1[System.Int16]]]
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        Select System.Func`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_2`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
        Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_4`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],VB$AnonymousType_15`5[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[System.Int16]]]
        ------
        Select System.Func`2[System.Int32,VB$AnonymousType_0`2[System.Int32,System.Int32]]
        Select System.Func`2[VB$AnonymousType_0`2[System.Int32,System.Int32],VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]]]
        Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte]],VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]]]
        Where System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        OrderBy System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Int32]
        Distinct
        TakeWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        SkipWhile System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],System.Boolean]
        Skip 0
        Take 0
        Select System.Func`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],VB$AnonymousType_2`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],QueryAble`1[System.Int16]]]
        Select System.Func`2[VB$AnonymousType_2`2[VB$AnonymousType_6`3[VB$AnonymousType_0`2[System.Int32,System.Int32],QueryAble`1[System.Byte],QueryAble`1[System.Byte]],QueryAble`1[System.Int16]],VB$AnonymousType_16`6[System.Int32,System.Int32,QueryAble`1[System.Byte],QueryAble`1[System.Byte],QueryAble`1[System.Int16],QueryAble`1[System.Int16]]]
        ------
        SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_17`3[System.Int32,System.Byte,QueryAble`1[System.Int16]]]
        ------
        SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_18`3[System.Int32,System.Byte,QueryAble`1[System.Int16]]]
        Select System.Func`2[VB$AnonymousType_18`3[System.Int32,System.Byte,QueryAble`1[System.Int16]],VB$AnonymousType_19`4[System.Int32,System.Byte,QueryAble`1[System.Int16],QueryAble`1[System.Int16]]]
        ------
        Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_17`3[System.Int32,System.Byte,QueryAble`1[System.Int16]]]
        ------
        Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_18`3[System.Int32,System.Byte,QueryAble`1[System.Int16]]]
        Select System.Func`2[VB$AnonymousType_18`3[System.Int32,System.Byte,QueryAble`1[System.Int16]],VB$AnonymousType_19`4[System.Int32,System.Byte,QueryAble`1[System.Int16],QueryAble`1[System.Int16]]]
        ------
        Select System.Func`2[System.Int32,System.Int32]
        SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_20`2[System.Byte,QueryAble`1[System.Int16]]]
        ------
        Select System.Func`2[System.Int32,System.Int32]
        SelectMany System.Func`3[System.Int32,System.Byte,VB$AnonymousType_21`2[System.Byte,QueryAble`1[System.Int16]]]
        Select System.Func`2[VB$AnonymousType_21`2[System.Byte,QueryAble`1[System.Int16]],VB$AnonymousType_22`3[System.Byte,QueryAble`1[System.Int16],QueryAble`1[System.Int16]]]
        ------
        Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_23`2[System.Int32,System.Byte]]
        SelectMany System.Func`3[VB$AnonymousType_23`2[System.Int32,System.Byte],System.Int64,VB$AnonymousType_24`3[VB$AnonymousType_23`2[System.Int32,System.Byte],System.Int64,QueryAble`1[System.Int16]]]
        Select System.Func`2[VB$AnonymousType_24`3[VB$AnonymousType_23`2[System.Int32,System.Byte],System.Int64,QueryAble`1[System.Int16]],VB$AnonymousType_25`4[QueryAble`1[System.Int16],System.Int64,System.Byte,System.Int32]]
        ------
        Join System.Func`3[System.Int32,System.Byte,VB$AnonymousType_23`2[System.Int32,System.Byte]]
        SelectMany System.Func`3[VB$AnonymousType_23`2[System.Int32,System.Byte],System.Int64,VB$AnonymousType_26`3[VB$AnonymousType_23`2[System.Int32,System.Byte],System.Int64,QueryAble`1[System.Int16]]]
        Select System.Func`2[VB$AnonymousType_26`3[VB$AnonymousType_23`2[System.Int32,System.Byte],System.Int64,QueryAble`1[System.Int16]],VB$AnonymousType_27`4[VB$AnonymousType_23`2[System.Int32,System.Byte],System.Int64,QueryAble`1[System.Int16],QueryAble`1[System.Int16]]]
        Select System.Func`2[VB$AnonymousType_27`4[VB$AnonymousType_23`2[System.Int32,System.Byte],System.Int64,QueryAble`1[System.Int16],QueryAble`1[System.Int16]],VB$AnonymousType_28`5[QueryAble`1[System.Int16],QueryAble`1[System.Int16],System.Int64,System.Byte,System.Int32]]
        ------
        Select System.Func`2[System.Int32,System.Int64]
        Join System.Func`3[System.Byte,System.Int64,VB$AnonymousType_29`2[System.Byte,System.Int64]]
        Join System.Func`3[System.Int32,VB$AnonymousType_29`2[System.Byte,System.Int64],VB$AnonymousType_30`3[System.Int32,VB$AnonymousType_29`2[System.Byte,System.Int64],QueryAble`1[System.Int16]]]
        Select System.Func`2[VB$AnonymousType_30`3[System.Int32,VB$AnonymousType_29`2[System.Byte,System.Int64],QueryAble`1[System.Int16]],VB$AnonymousType_25`4[QueryAble`1[System.Int16],System.Int64,System.Byte,System.Int32]]
        ------
        Select System.Func`2[System.Int32,System.Int64]
        Join System.Func`3[System.Byte,System.Int64,VB$AnonymousType_29`2[System.Byte,System.Int64]]
        Join System.Func`3[System.Int32,VB$AnonymousType_29`2[System.Byte,System.Int64],VB$AnonymousType_31`3[System.Int32,VB$AnonymousType_29`2[System.Byte,System.Int64],QueryAble`1[System.Int16]]]
        Select System.Func`2[VB$AnonymousType_31`3[System.Int32,VB$AnonymousType_29`2[System.Byte,System.Int64],QueryAble`1[System.Int16]],VB$AnonymousType_32`4[System.Int32,VB$AnonymousType_29`2[System.Byte,System.Int64],QueryAble`1[System.Int16],QueryAble`1[System.Int16]]]
        Select System.Func`2[VB$AnonymousType_32`4[System.Int32,VB$AnonymousType_29`2[System.Byte,System.Int64],QueryAble`1[System.Int16],QueryAble`1[System.Int16]],VB$AnonymousType_28`5[QueryAble`1[System.Int16],QueryAble`1[System.Int16],System.Int64,System.Byte,System.Int32]]
        ]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function DefaultQueryIndexer1() As Task
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
        Option Strict Off

        Imports System

        Class DefaultQueryIndexer1
            Function [Select](x As Func(Of Integer, Integer)) As Object
                Return Nothing
            End Function

            Function ElementAtOrDefault(x As Integer) As Guid
                Return New Guid(x, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
            End Function
        End Class

        Class DefaultQueryIndexer3
            Function [Select](x As Func(Of Integer, Integer)) As Object
                Return Nothing
            End Function

            ReadOnly Property ElementAtOrDefault(x As String) As String
                Get
                    Return x
                End Get
            End Property
        End Class

        Class DefaultQueryIndexer4
            Function [Select](x As Func(Of Integer, Integer)) As Object
                Return Nothing
            End Function
        End Class

        Class DefaultQueryIndexer5
            Function [Select](x As Func(Of Integer, Integer)) As Object
                Return Nothing
            End Function

            Shared Function ElementAtOrDefault(x As Integer) As Guid
                Return New Guid(x, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
            End Function
        End Class

        Class DefaultQueryIndexer6
            Function AsEnumerable() As DefaultQueryIndexer1
                Return New DefaultQueryIndexer1()
            End Function
        End Class

        Class DefaultQueryIndexer7
            Function AsQueryable() As DefaultQueryIndexer1
                Return New DefaultQueryIndexer1()
            End Function
        End Class

        Class DefaultQueryIndexer8
            Function Cast(Of T)() As DefaultQueryIndexer1
                Return New DefaultQueryIndexer1()
            End Function
        End Class

        Module Module1

            &lt;System.Runtime.CompilerServices.Extension()&gt;
            Function ElementAtOrDefault(this As DefaultQueryIndexer4, x As String) As Integer
                Return x
            End Function

            Function TestDefaultQueryIndexer1() As DefaultQueryIndexer1
                Return New DefaultQueryIndexer1()
            End Function

            Function TestDefaultQueryIndexer5() As DefaultQueryIndexer5
                Return New DefaultQueryIndexer5()
            End Function

            Sub Main()
                Dim xx1 As New DefaultQueryIndexer1()

                System.Console.WriteLine(xx1(1))
                System.Console.WriteLine(TestDefaultQueryIndexer1(2))

                Dim xx3 As New DefaultQueryIndexer3()
                System.Console.WriteLine(xx3!aaa)

                Dim xx4 As New DefaultQueryIndexer4()
                System.Console.WriteLine(xx4(4))

                System.Console.WriteLine((New DefaultQueryIndexer5())(6))
                System.Console.WriteLine(TestDefaultQueryIndexer5(7))

                System.Console.WriteLine((New DefaultQueryIndexer6())(8))
                System.Console.WriteLine((New DefaultQueryIndexer7())(9))
                System.Console.WriteLine((New DefaultQueryIndexer8())(10))
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

            Await Test(compilationDef,
                            expectedOutput:=
            <![CDATA[
        00000001-0000-0000-0000-000000000000
        00000002-0000-0000-0000-000000000000
        aaa
        4
        00000006-0000-0000-0000-000000000000
        00000007-0000-0000-0000-000000000000
        00000008-0000-0000-0000-000000000000
        00000009-0000-0000-0000-000000000000
        0000000a-0000-0000-0000-000000000000
        ]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function Bug10127() As Task
            Dim compilationDef =
<compilation name="Bug10127">
    <file name="a.vb">
        Option Strict Off

        Imports System
        Imports System.Linq
        Imports System.Collections.Generic

        Module Module1
            Sub Main()
                Dim q As Object 
                q = Aggregate x In New Integer(){1} Into s = Sum(Nothing)
                System.Console.WriteLine(q)
                System.Console.WriteLine(q.GetType())

                System.Console.WriteLine("-------")

                q = [||]From x In New Integer() {1} Order By Nothing
                System.Console.WriteLine(DirectCast(q, IEnumerable(Of Integer))(0))
            End Sub
        End Module

            </file>
</compilation>

            Await Test(compilationDef, additionalRefs:={SystemCoreRef},
                         expectedOutput:=
            <![CDATA[
0
System.Int32
-------
1
]]>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)>
        Public Async Function SelectMany1() As Task
            Dim compilationDef =
<compilation name="Bug10127">
    <file name="a.vb">
        Option Strict Off

        Imports System
        Imports System.Linq
        Imports System.Collections.Generic

        Module Module1
            Sub Main()
                Dim q As Object 
                q = Aggregate x In New Integer(){1} Into s = Sum(Nothing)
                System.Console.WriteLine(q)
                System.Console.WriteLine(q.GetType())

                System.Console.WriteLine("-------")

                q = [||]From x In New Integer() {1} Order By Nothing
                System.Console.WriteLine(DirectCast(q, IEnumerable(Of Integer))(0))
            End Sub
        End Module

        Public Class C1
            Sub M1(a as integer, b as integer) 
                Dim result = New With {Key .a = a, Key b} 
            End Sub
        End Class


            </file>
</compilation>

            Await Test(compilationDef, additionalRefs:={SystemCoreRef},
                         expectedOutput:=
            <![CDATA[
]]>)
        End Function
    End Class
End Namespace
