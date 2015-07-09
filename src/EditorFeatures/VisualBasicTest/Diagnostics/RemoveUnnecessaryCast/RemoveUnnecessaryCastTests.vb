' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.RemoveUnnecessaryCast
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics.RemoveUnnecessaryCast

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.RemoveUnnecessaryCast
    Partial Public Class RemoveUnnecessaryCastTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(New VisualBasicRemoveUnnecessaryCastDiagnosticAnalyzer(), New RemoveUnnecessaryCastCodeFixProvider())
        End Function

        <WorkItem(545979)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCastToErrorType()
            Dim markup =
<File>
Module M
    Sub Main()
        Dim x = [|CType(0, ErrorType)|]
    End Sub
End Module
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(545148)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub ParenthesizeToKeepParseTheSame1()
            Dim markup =
<File>
Imports System.Collections
Imports System.Linq
 
Module Program
    Sub Main
        Dim a = CType([|CObj(From x In "" Select x)|], IEnumerable)
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System.Collections
Imports System.Linq
 
Module Program
    Sub Main
        Dim a = CType((From x In "" Select x), IEnumerable)
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(530762)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub ParenthesizeToKeepParseTheSame2()
            Dim markup =
<File>
Module Program
    Sub Main()
        Dim x = 0 &lt; [|CInt(&lt;x/&gt;.GetHashCode)|]
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Dim x = 0 &lt; (&lt;x/&gt;.GetHashCode)
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(530762)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub ParenthesizeToKeepParseTheSame3()
            Dim markup =
<File>
Module Program
    Sub Main()
        Dim x = 0 &lt; [|CInt(&lt;x/&gt;.GetHashCode)|]
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Dim x = 0 &lt; (&lt;x/&gt;.GetHashCode)
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545149)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub InsertCallKeywordIfNecessary1()
            Dim markup =
<File>
Module Program
    Sub Main()
        [|CInt(1)|].ToString
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Call 1.ToString
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545150)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub InsertCallKeywordIfNecessary2()
            Dim markup =
<File>
Module Program
    Sub Main()
        [|CStr(Mid())|].GetType
    End Sub
    Function Mid() As String
    End Function
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        [Mid]().GetType
    End Sub
    Function Mid() As String
    End Function
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545229)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Sub InsertCallKeywordIfNecessary3()
            Dim code =
<File>
Imports System
Class C1
    Sub M()
#If True Then
        [|CInt(1)|].ToString()
#End If
    End Sub
End Class
</File>

            Dim expected =
<File>
Imports System
Class C1
    Sub M()
#If True Then
        Call 1.ToString()
#End If
    End Sub
End Class
</File>

            Test(code, expected)
        End Sub

        <WorkItem(545528)>
        <WorkItem(16488, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub AddExplicitArgumentListIfNecessary1()
            Dim markup =
<File>
Imports System
Module Program
    Sub Main()
        Dim x As Action = Sub() Console.WriteLine("Hello")
        [|CType(x, Action)|] : Console.WriteLine()
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System
Module Program
    Sub Main()
        Dim x As Action = Sub() Console.WriteLine("Hello")
        x() : Console.WriteLine()
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545134)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveConversionFromNullableLongToIComparable()
            Dim markup =
<File>
Option Strict On

Class M
    Sub Main()
        Dim y As System.IComparable(Of Long) = [|CType(1, Long?)|]
    End Sub
End Class
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(545151)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveArrayLiteralConversion()
            Dim markup =
<File>
Module Program
    Sub Main()
        Dim x As Object = [|CType({1}, Long())|]
        Console.WriteLine(x.GetType)
    End Sub
End Module
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(545152)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveAddressOfCastToDelegate()
            Dim markup =
<File>
Imports System

Module Program
    Sub Main()
        Dim x As Object = [|CType(AddressOf Console.WriteLine, Action)|]
    End Sub
End Module
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(545311)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnneededCastInLambda1()
            Dim markup =
<File>
Module Program
    Sub Main()
        Dim f As Func(Of Long) = Function() [|CLng(5)|]
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Dim f As Func(Of Long) = Function() 5
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545311)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnneededCastInLambda2()
            Dim markup =
<File>
Module Program
    Sub Main()
        Dim f As Func(Of Long) = Function()
                                     Return [|CLng(5)|]
                                 End Function
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Dim f As Func(Of Long) = Function()
                                     Return 5
                                 End Function
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545311)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnneededCastInLambda3()
            Dim markup =
<File>
Imports System
Module Module1
    Sub Main()
        Dim lambda As Func(Of Action(Of Integer, Long)) = Function()
                                                              Return [|CType(Sub(x As Integer, y As Long)
                                                                           End Sub, Action(Of Integer, Long))|]
                                                          End Function
    End Sub
End Module

</File>

            Dim expected =
<File>
Imports System
Module Module1
    Sub Main()
        Dim lambda As Func(Of Action(Of Integer, Long)) = Function()
                                                              Return Sub(x As Integer, y As Long)
                                                                     End Sub
                                                          End Function
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545311)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnneededCastInFunctionStatement()
            Dim markup =
<File>
Module Program
    Function M() As Long
        Return [|CLng(5)|]
    End Function
End Module
</File>

            Dim expected =
<File>
Module Program
    Function M() As Long
        Return 5
    End Function
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545311)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnneededCastInFunctionVariableAssignment()
            Dim markup =
<File>
Module Program
    Function M() As Long
        M = [|CLng(5)|]
    End Function
End Module
</File>

            Dim expected =
<File>
Module Program
    Function M() As Long
        M = 5
    End Function
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545312)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnneededCastInBinaryExpression()
            Dim markup =
<File>
Module Module1
    Sub Main()
        Dim m As Integer = 3
        Dim n? As Integer = 2
        Dim comparer = [|CType(m, Integer?)|] > n
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Module1
    Sub Main()
        Dim m As Integer = 3
        Dim n? As Integer = 2
        Dim comparer = m > n
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545423)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnneededCastInsideCaseLabel()
            Dim markup =
<File>
Module Module1
    Sub Main()
        Select Case 5L
            Case [|CType(5, Long)|]
        End Select
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Module1
    Sub Main()
        Select Case 5L
            Case 5
        End Select
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545421)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnneededCastInOptionalParameterValue()
            Dim markup =
<File>
Module Program
    Function test(Optional ByVal x? As Integer = [|CType(Nothing, Object)|]) As Boolean
        Return x.HasValue
    End Function
End Module
</File>

            Dim expected =
<File>
Module Program
    Function test(Optional ByVal x? As Integer = Nothing) As Boolean
        Return x.HasValue
    End Function
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545579)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnneededCastInRangeCaseClause1()
            Dim markup =
<File>
Module Module1
    Sub Main()
        Select Case 5L
            Case CType(5, Long)
            Case [|CType(1, Long)|] To CType(5, Long)
        End Select
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Module1
    Sub Main()
        Select Case 5L
            Case CType(5, Long)
            Case 1 To CType(5, Long)
        End Select
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545579)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnneededCastInRangeCaseClause2()
            Dim markup =
<File>
Module Module1
    Sub Main()
        Select Case 5L
            Case CType(5, Long)
            Case CType(1, Long) To [|CType(5, Long)|]
        End Select
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Module1
    Sub Main()
        Select Case 5L
            Case CType(5, Long)
            Case CType(1, Long) To 5
        End Select
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545580)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnneededCastForLoop1()
            Dim markup =
<File>
Module Module1
    Sub Main()
        For i As Long = [|CLng(0)|] To CLng(4) Step CLng(5)
        Next
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Module1
    Sub Main()
        For i As Long = 0 To CLng(4) Step CLng(5)
        Next
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545580)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnneededCastForLoop2()
            Dim markup =
<File>
Module Module1
    Sub Main()
        For i As Long = CLng(0) To [|CLng(4)|] Step CLng(5)
        Next
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Module1
    Sub Main()
        For i As Long = CLng(0) To 4 Step CLng(5)
        Next
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545580)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnneededCastForLoop3()
            Dim markup =
<File>
Module Module1
    Sub Main()
        For i As Long = CLng(0) To CLng(4) Step [|CLng(5)|]
        Next
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Module1
    Sub Main()
        For i As Long = CLng(0) To CLng(4) Step 5
        Next
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545599)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveNeededCastWithUserDefinedConversionsAndOptionStrictOff()
            Dim markup =
<File>
Option Strict Off

Public Class X
    Sub Foo()
        Dim x As New X()
        Dim y As Integer = [|CDbl(x)|]
    End Sub

    Public Shared Widening Operator CType(ByVal x As X) As Double
    End Operator
    Public Shared Widening Operator CType(ByVal x As X) As Single?
    End Operator
End Class
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(529535)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveNeededCastWhenResultIsAmbiguous()
            Dim markup =
<File>
Option Strict On
Interface IEnumerable(Of Out Tout)
End Interface
Class A : End Class
Class B
    Inherits A
End Class
Class ControlList
    Implements IEnumerable(Of A)
    Implements IEnumerable(Of B)
End Class

Module VarianceExample
    Sub Main()
        Dim _ctrlList As IEnumerable(Of A) = [|CType(New ControlList, IEnumerable(Of A))|]
    End Sub
End Module
</File>

            Dim expected =
<File>
Option Strict On
Interface IEnumerable(Of Out Tout)
End Interface
Class A : End Class
Class B
    Inherits A
End Class
Class ControlList
    Implements IEnumerable(Of A)
    Implements IEnumerable(Of B)
End Class

Module VarianceExample
    Sub Main()
        Dim _ctrlList As IEnumerable(Of A) = New ControlList
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545261)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnnecessaryCastToNothingInArrayInitializer()
            Dim markup =
<File>
Module Program
    Sub Main(args As String())
        Dim NothingArray = {([|CType(Nothing, Object)|])}
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main(args As String())
        Dim NothingArray = {(Nothing)}
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545526)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCastThatResultsInDifferentStringRepresentations()
            Dim markup =
<File>
Option Strict Off

Module M
    Sub Main()
        Foo([|CType(1000000000000000, Double)|]) ' Prints 1E+15
        Foo(1000000000000000) ' Prints 1000000000000000
    End Sub
    Sub Foo(x As String)
        Console.WriteLine(x)
    End Sub
End Module
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(545631)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCastThatChangesArrayLiteralTypeAndBreaksOverloadResolution()
            Dim markup =
<File>
Module Program
    Sub Main()
        Dim a = {[|CLng(Nothing)|]}
        Foo(a)
    End Sub

    Sub Foo(a() As Long)
    End Sub
End Module
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(545456)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveCastInAttribute()
            Dim markup =
<File>
Imports System
Class FooAttribute
    Inherits Attribute

    Sub New(o As Object)
    End Sub

End Class

&lt;Foo([|CObj(1)|])&gt;
Class C
End Class
</File>

            Dim expected =
<File>
Imports System
Class FooAttribute
    Inherits Attribute

    Sub New(o As Object)
    End Sub

End Class

&lt;Foo(1)&gt;
Class C
End Class
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545701)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub AddParenthesesIfCopyBackAffected1()
            Dim markup =
<File>
Module M
    Sub Main()
        Dim x = 1
        Foo([|CInt(x)|])
        Console.WriteLine(x)
    End Sub
    Sub Foo(ByRef x As Integer)
        x = 2
    End Sub
End Module
</File>

            Dim expected =
<File>
Module M
    Sub Main()
        Dim x = 1
        Foo((x))
        Console.WriteLine(x)
    End Sub
    Sub Foo(ByRef x As Integer)
        x = 2
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545701)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub AddParenthesesIfCopyBackAffected2()
            Dim markup =
<File>
Module M
    Private x As Integer = 1
    Sub Main()
        Foo([|CInt(x)|])
        Console.WriteLine(x)
    End Sub
    Sub Foo(ByRef x As Integer)
        x = 2
    End Sub
End Module
</File>

            Dim expected =
<File>
Module M
    Private x As Integer = 1
    Sub Main()
        Foo((x))
        Console.WriteLine(x)
    End Sub
    Sub Foo(ByRef x As Integer)
        x = 2
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545701)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub AddParenthesesIfCopyBackAffected3()
            Dim markup =
<File>
Module M
    Private Property x As Integer = 1
    Sub Main()
        Foo([|CInt(x)|])
        Console.WriteLine(x)
    End Sub
    Sub Foo(ByRef x As Integer)
        x = 2
    End Sub
End Module
</File>

            Dim expected =
<File>
Module M
    Private Property x As Integer = 1
    Sub Main()
        Foo((x))
        Console.WriteLine(x)
    End Sub
    Sub Foo(ByRef x As Integer)
        x = 2
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545971)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveNecessaryCastPassedToParamArray1()
            Dim markup =
<File>
Module M
    Sub Main()
        Foo([|CObj(Nothing)|])
    End Sub
    Sub Foo(ParamArray x As Object())
        Console.WriteLine(x.Length)
    End Sub
End Module
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(545971)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveNecessaryCastPassedToParamArray2()
            Dim markup =
<File>
Module M
    Sub Main()
        Foo([|CStr(Nothing)|])
    End Sub
    Sub Foo(ParamArray x As Object())
        Console.WriteLine(x.Length)
    End Sub
End Module
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(545971)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnnecessaryCastPassedToParamArray1()
            Dim markup =
<File>
Module M
    Sub Main()
        Foo([|CObj(New Object)|])
    End Sub
    Sub Foo(ParamArray x As Object())
        Console.WriteLine(x.Length)
    End Sub
End Module
</File>

            Dim expected =
<File>
Module M
    Sub Main()
        Foo(New Object)
    End Sub
    Sub Foo(ParamArray x As Object())
        Console.WriteLine(x.Length)
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545971)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnnecessaryCastPassedToParamArray2()
            Dim markup =
<File>
Module M
    Sub Main()
        Foo([|CStr("")|])
    End Sub
    Sub Foo(ParamArray x As Object())
        Console.WriteLine(x.Length)
    End Sub
End Module
</File>

            Dim expected =
<File>
Module M
    Sub Main()
        Foo("")
    End Sub
    Sub Foo(ParamArray x As Object())
        Console.WriteLine(x.Length)
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545971)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnnecessaryCastPassedToParamArray3()
            Dim markup =
<File>
Imports System
Module M
    Sub Main()
        Foo([|DirectCast(New Exception, Object)|])
    End Sub
    Sub Foo(ParamArray x As Object())
        Console.WriteLine(x.GetType)
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System
Module M
    Sub Main()
        Foo(New Exception)
    End Sub
    Sub Foo(ParamArray x As Object())
        Console.WriteLine(x.GetType)
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545971)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnnecessaryCastPassedToParamArray4()
            Dim markup =
<File>
Imports System
Module M
    Sub Main()
        Foo([|DirectCast(Nothing, Object())|])
    End Sub
    Sub Foo(ParamArray x As Object())
        Console.WriteLine(x.GetType)
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System
Module M
    Sub Main()
        Foo(Nothing)
    End Sub
    Sub Foo(ParamArray x As Object())
        Console.WriteLine(x.GetType)
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545971)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnnecessaryCastPassedToParamArray5()
            Dim markup =
<File>
Imports System
Module M
    Sub Main()
        Foo([|DirectCast(Nothing, String())|])
    End Sub
    Sub Foo(ParamArray x As Object())
        Console.WriteLine(x.GetType)
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System
Module M
    Sub Main()
        Foo(Nothing)
    End Sub
    Sub Foo(ParamArray x As Object())
        Console.WriteLine(x.GetType)
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnnecessaryCastToArrayLiteral1()
            Dim markup =
<File>
Module M
    Sub Main()
        Dim i = [|CType({1, 2, 3}, Integer())|]
    End Sub
End Module
</File>

            Dim expected =
<File>
Module M
    Sub Main()
        Dim i = {1, 2, 3}
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveNecessaryCastToArrayLiteral2()
            Dim markup =
<File>
Module Program
    Sub Main()
        Dim a = {[|CLng(Nothing)|]}
        Foo(a)
    End Sub
 
    Sub Foo(a() As Long)
    End Sub
End Module
</File>

            TestMissing(markup)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveNecessaryCastToArrayLiteral()
            Dim markup =
<File>
Module M
    Sub Main()
        Dim i = [|CType({1, 2, 3}, Long())|]
    End Sub
End Module
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(545972)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnnecessaryCastInBinaryIf1()
            Dim markup =
<File>
Class Test
    Public Shared Sub Main()
        Dim a1 As Long = If((0 = 0), [|CType(1, Long)|], CType(2, Long))
    End Sub
End Class
</File>

            Dim expected =
<File>
Class Test
    Public Shared Sub Main()
        Dim a1 As Long = If((0 = 0), 1, CType(2, Long))
    End Sub
End Class
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545972)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnnecessaryCastInBinaryIf2()
            Dim markup =
<File>
Class Test
    Public Shared Sub Main()
        Dim a1 As Long = If((0 = 0), CType(1, Long), [|CType(2, Long)|])
    End Sub
End Class
</File>

            Dim expected =
<File>
Class Test
    Public Shared Sub Main()
        Dim a1 As Long = If((0 = 0), CType(1, Long), 2)
    End Sub
End Class
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545974)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnnecessaryCastInObjectCreationExpression()
            Dim markup =
<File>
Imports System
Module M
    Sub Main()
        Dim t1 As Type = [|CType(New ArgumentException(), Exception)|].GetType()
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System
Module M
    Sub Main()
        Dim t1 As Type = New ArgumentException().GetType()
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545973)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnnecessaryCastInSelectCase()
            Dim markup =
<File>
Imports System
Module Module1
    Sub Main()
        Select Case [|CType(2, Integer)|]
            Case 2 To CType(5, Object)
                Console.WriteLine("true")
        End Select
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System
Module Module1
    Sub Main()
        Select Case 2
            Case 2 To CType(5, Object)
                Console.WriteLine("true")
        End Select
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545526)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCastToDoubleInOptionStrictOff()
            Dim markup =
<File>
Option Strict Off

Module M
    Sub Main()
        Foo([|CType(1000000000000000, Double)|]) ' Prints 1E+15
        Foo(1000000000000000) ' Prints 1000000000000000
    End Sub
    Sub Foo(x As String)
        Console.WriteLine(x)
    End Sub
End Module
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(545828)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCStrInCharToStringToObjectChain()
            Dim markup =
<File>
Imports System
Module Program
    Sub Main()
        Dim x As Object = [|CStr(" "c)|]
        Console.WriteLine(x.GetType())
    End Sub
End Module
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(545808)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveNecessaryCastWithMultipleUserDefinedConversionsAndOptionStrictOff()
            Dim markup =
<File>
Option Strict Off

Public Class X
    Shared Sub Main()
        Dim x As New X()
        Dim y As Integer = [|CDbl(x)|]
        Console.WriteLine(y)
    End Sub

    Public Shared Widening Operator CType(ByVal x As X) As Double
        Return 1
    End Operator
    Public Shared Widening Operator CType(ByVal x As X) As Single
        Return 2
    End Operator
End Class
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(545998)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCastWhichWouldChangeAttributeOverloadResolution()
            Dim markup =
<File>
Imports System

&lt;A({[|CLng(0)|]})&gt;
Class A
    Inherits Attribute

    Sub New(x As Integer())
    End Sub

    Sub New(x As Long())
    End Sub
End Class
</File>

            TestMissing(markup)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontMoveTrailingComment()
            Dim markup =
<File>
Module Program
    Sub Main()
        With ""
            Dim y = [|CInt(1 + 2)|] ' Blah
        End With
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        With ""
            Dim y = 1 + 2 ' Blah
        End With
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveCastInFieldInitializer()
            Dim markup =
<File>
Imports System.Collections.Generic
Class B
    Dim list = [|CObj(GetList())|]

    Private Shared Function GetList() As List(Of String)
        Return New List(Of String) From {"abc", "def", "ghi"}
    End Function
End Class
</File>

            Dim expected =
<File>
Imports System.Collections.Generic
Class B
    Dim list = GetList()

    Private Shared Function GetList() As List(Of String)
        Return New List(Of String) From {"abc", "def", "ghi"}
    End Function
End Class
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontDuplicateTrivia()
            Dim markup =
<File>
Imports System
Module M
    Sub Main()
        [|CType(x(), Action)|] ' Remove redundant cast
    End Sub
    Function x() As Action
        Return Sub() Console.WriteLine(1)
    End Function
End Module
</File>

            Dim expected =
<File>
Imports System
Module M
    Sub Main()
        x()() ' Remove redundant cast
    End Sub
    Function x() As Action
        Return Sub() Console.WriteLine(1)
    End Function
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(531479)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub EscapeNextStatementIfNeeded()
            Dim markup =
<File>
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main()
        Dim y = [|CType(From z In "" Distinct, IEnumerable(Of Char))|]
        Take()
    End Sub

    Sub Take()
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main()
        Dim y = From z In "" Distinct
        [Take]()
    End Sub

    Sub Take()
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(607749)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub Bugfix_607749()
            Dim markup =
<File>
Imports System
Interface I
    Property A As Action
End Interface
 
Class C
    Implements I
    Property A As Action = [|CType(Sub() If True Then, Action)|] Implements I.A
End Class
</File>

            Dim expected =
<File>
Imports System
Interface I
    Property A As Action
End Interface
 
Class C
    Implements I
    Property A As Action = (Sub() If True Then) Implements I.A
End Class
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(609477)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub Bugfix_609477()
            Dim markup =
<File>
Imports System
Module Program
    Sub Main()
        If True Then : Dim x As Action = [|CType(Sub() If True Then, Action)|] : Else : Return : End If
    End Sub
End Module

</File>

            Dim expected =
<File>
Imports System
Module Program
    Sub Main()
        If True Then : Dim x As Action = (Sub() If True Then) : Else : Return : End If
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(552813)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCastWhileNarrowingWithOptionOn()
            Dim markup =
<File>
Option Strict On
Module Program
    Public Function IsFailFastSuppressed() As Boolean
        Dim value = New Object()
        Return value IsNot Nothing AndAlso [|DirectCast(value, Boolean)|]
    End Function
End Module
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(577929)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCastWhileDefaultingNullables()
            Dim markup =
<File>
Module M
    Sub Main()
        Dim x? As Date = [|CDate(Nothing)|]
        Console.WriteLine(x)
    End Sub
End Module
</File>

            TestMissing(markup)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveCastAroundAction()
            Dim markup =
<File>
Imports System
Module Program
    Sub Main()
        Dim x As Action = Sub() Console.WriteLine("Hello")
        [|CType(x, Action)|] : Console.WriteLine() 
    End Sub
End Module

</File>

            Dim expected =
<File>
Imports System
Module Program
    Sub Main()
        Dim x As Action = Sub() Console.WriteLine("Hello")
        x() : Console.WriteLine() 
    End Sub
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(578016)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCStr()
            Dim markup =
<File>Option Strict On

Module M
    Sub Main()
        Foo()
    End Sub
    Sub Foo(Optional x As Object = [|CStr|](Chr(1)))
        Console.WriteLine(x.GetType())
    End Sub
End Module
</File>
            TestMissing(markup)
        End Sub

        <WorkItem(530105)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveNumericCast()
            Dim markup =
<File>
Interface I
    [|Sub Foo(Optional x As Object = CByte(1))|]
End Interface
</File>
            TestMissing(markup)
        End Sub

        <WorkItem(530104)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCTypeFromNumberToEnum()
            Dim markup =
<File>
Option Strict On

Interface I
    [|Sub Foo(Optional x As DayOfWeek = CType(-1, DayOfWeek))|]
End Interface
</File>
            TestMissing(markup)
        End Sub

        <WorkItem(530077)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCastForLambdaToDelegateConversionWithOptionStrictOn()
            Dim markup =
 <File>
Option Strict On
Imports System
Module Program
    Sub Main(args As String())
        Dim x = 1
        Dim y As Func(Of Integer) = Function()
                                        Return [|CType(x.ToString(), Integer)|]
                                    End Function
    End Sub
End Module
</File>
            TestMissing(markup)
        End Sub

        <WorkItem(529966)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveForNarrowingConversionFromObjectWithOptionStrictOnInsideQueryExpression()
            Dim markup =
<File>
Option Strict On
Imports System
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim o3 As Object = ""hi""
        Dim col = {o3, o3}
        Dim q3 = From i As String In [|CType(col, String())|]
    End Sub
End Module
</File>
            TestMissing(markup)
        End Sub

        <WorkItem(530650)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnnecessaryCastFromLambdaToDelegateParenthesizeLambda()
            Dim markup =
<File>
Imports System

Module M
    Sub Main()
        [|CType(Sub() Return, Action)|] : Return
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System

Module M
    Sub Main()
        Call (Sub() Return) : Return
    End Sub
End Module
</File>
            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(707189)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnnecessaryCastFromInvocationStatement()
            Dim markup =
<File>
Imports System
Imports System.Collections.Generic
Module Module1
    Sub Main()
        [|DirectCast(GetEnumerator(), IDisposable).Dispose()|]
    End Sub
    Function GetEnumerator() As List(Of Integer).Enumerator
        Return Nothing
    End Function
End Module
</File>

            Dim expected =
<File>
Imports System
Imports System.Collections.Generic
Module Module1
    Sub Main()
        GetEnumerator().Dispose()
    End Sub
    Function GetEnumerator() As List(Of Integer).Enumerator
        Return Nothing
    End Function
End Module
</File>
            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(707189)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnnecessaryCastFromInvocationStatement2()
            Dim markup =
<File>
Interface I1
Sub Foo()
End Interface
Class M
    Implements I1
    Shared Sub Main()
        [|CType(New M(), I1).Foo()|]
    End Sub
 
    Public Sub Foo() Implements I1.Foo
    End Sub
End Class
</File>

            Dim expected =
<File>
Interface I1
Sub Foo()
End Interface
Class M
    Implements I1
    Shared Sub Main()
        Call New M().Foo()
    End Sub
 
    Public Sub Foo() Implements I1.Foo
    End Sub
End Class
</File>
            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(768895)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveUnnecessaryCastInTernary()
            Dim markup =
<File>
Class Program
    Private Shared Sub Main(args As String())
        Dim x As Object = Nothing
        Dim y As Integer = If([|CBool(x)|], 1, 0)
    End Sub
End Class
</File>

            Dim expected =
<File>
Class Program
    Private Shared Sub Main(args As String())
        Dim x As Object = Nothing
        Dim y As Integer = If(x, 1, 0)
    End Sub
End Class
</File>
            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(770187)>
        <Fact(Skip:="770187"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveNecessaryCastInSelectCaseExpression()
            ' Cast removal invokes a different user defined operator, hence the cast is necessary.

            Dim markup =
<File>
    <![CDATA[
Namespace ConsoleApplication23
    Class Program
        Public Shared Sub Main(args As String())
            Dim foo As Integer = 0
            Select Case [|CType(0, Short)|]
                Case New A
                    Return
            End Select
        End Sub
    End Class

    Class A
        Public Shared Operator =(ByVal p1 As Short, ByVal p2 As A) As Boolean
            Console.WriteLine("Short =")
            Return 0
        End Operator

        Public Shared Operator <>(ByVal p1 As Short, ByVal p2 As A) As Boolean
            Console.WriteLine("Short <>")
            Return 0
        End Operator

        Public Shared Operator =(ByVal p1 As Integer, ByVal p2 As A) As Boolean
            Console.WriteLine("Integer =")
            Throw New NotImplementedException
        End Operator

        Public Shared Operator <>(ByVal p1 As Integer, ByVal p2 As A) As Boolean
            Console.WriteLine("Integer <>")
            Throw New NotImplementedException
        End Operator
    End Class
End Namespace]]>
</File>
            TestMissing(markup)
        End Sub

        <WorkItem(770187)>
        <Fact(Skip:="770187"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveNecessaryCastInSelectCaseExpression2()
            ' Cast removal invokes a different user defined operator, hence the cast is necessary.

            Dim markup =
<File>
    <![CDATA[
Namespace ConsoleApplication23
    Class Program
        Public Shared Sub Main(args As String())
            Dim foo As Integer = 0
            Select Case [|CType(0, Short)|]
                Case < New A
                    Return
            End Select
        End Sub
    End Class

    Class A
        Public Shared Operator <(ByVal p1 As Short, ByVal p2 As A) As Boolean
            Console.WriteLine("Short <")
            Return 0
        End Operator

        Public Shared Operator >(ByVal p1 As Short, ByVal p2 As A) As Boolean
            Console.WriteLine("Short >")
            Return 0
        End Operator

        Public Shared Operator <(ByVal p1 As Integer, ByVal p2 As A) As Boolean
            Console.WriteLine("Integer <")
            Throw New NotImplementedException
        End Operator

        Public Shared Operator >(ByVal p1 As Integer, ByVal p2 As A) As Boolean
            Console.WriteLine("Integer >")
            Throw New NotImplementedException
        End Operator
    End Class
End Namespace]]>
</File>
            TestMissing(markup)
        End Sub

        <WorkItem(770187)>
        <Fact(Skip:="770187"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveNecessaryCastInSelectCaseExpression3()
            ' Cast removal invokes a different user defined operator, hence the cast is necessary.

            Dim markup =
<File>
    <![CDATA[
Namespace ConsoleApplication23
    Class Program
        Public Shared Sub Main(args As String())
            Dim foo As Integer = 0
            Select Case [|CType(0, Short)|]
                Case New A To New A
                    Return
            End Select
        End Sub
    End Class

    Class A
        Public Shared Operator <=(ByVal p1 As Short, ByVal p2 As A) As Boolean
            Console.WriteLine("Short <=")
            Return 0
        End Operator

        Public Shared Operator >=(ByVal p1 As Short, ByVal p2 As A) As Boolean
            Console.WriteLine("Short >=")
            Return 0
        End Operator

        Public Shared Operator <=(ByVal p1 As Integer, ByVal p2 As A) As Boolean
            Console.WriteLine("Integer <=")
            Throw New NotImplementedException
        End Operator

        Public Shared Operator >=(ByVal p1 As Integer, ByVal p2 As A) As Boolean
            Console.WriteLine("Integer >=")
            Throw New NotImplementedException
        End Operator
    End Class
End Namespace]]>
</File>
            TestMissing(markup)
        End Sub

#Region "Interface Casts"

        <WorkItem(545889)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCastToInterfaceForUnsealedType()
            Dim markup =
<File>
Imports System

Class X
    Implements IDisposable
    Private Shared Sub Main()
        Dim x As X = New Y()
        [|DirectCast(x, IDisposable)|].Dispose()
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        Console.WriteLine("X.Dispose")
    End Sub
End Class

Class Y
	Inherits X
	Implements IDisposable
	Private Sub IDisposable_Dispose() Implements IDisposable.Dispose
		Console.WriteLine("Y.Dispose")
	End Sub
End Class
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(545890)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveCastToInterfaceForSealedType1()
            ' Note: The cast below can be removed because C is sealed and the
            ' unspecified optional parameters of I.Foo() and C.Foo() have the
            ' same default values.

            Dim markup =
<File>
Imports System

Interface I
    Sub Foo(Optional x As Integer = 0)
End Interface

NotInheritable Class C
    Implements I
    Public Sub Foo(Optional x As Integer = 0) Implements I.Foo
        Console.WriteLine(x)
    End Sub

    Private Shared Sub Main()
        [|DirectCast(New C(), I)|].Foo()
    End Sub
End Class
</File>

            Dim expected =
<File>
Imports System

Interface I
    Sub Foo(Optional x As Integer = 0)
End Interface

NotInheritable Class C
    Implements I
    Public Sub Foo(Optional x As Integer = 0) Implements I.Foo
        Console.WriteLine(x)
    End Sub

    Private Shared Sub Main()
        Call New C().Foo()
    End Sub
End Class
</File>
            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545890)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveCastToInterfaceForSealedType2()
            ' Note: The cast below can be removed because C is sealed and the
            ' interface member has no parameters.

            Dim markup =
<File>
Imports System

Interface I
    ReadOnly Property Foo() As String
End Interface

NotInheritable Class C
    Implements I
    Public ReadOnly Property Foo() As String Implements I.Foo
        Get
            Return "Nikov Rules"
        End Get
    End Property

    Private Shared Sub Main()
        Console.WriteLine([|DirectCast(New C(), I)|].Foo)
    End Sub
End Class
</File>

            Dim expected =
<File>
Imports System

Interface I
    ReadOnly Property Foo() As String
End Interface

NotInheritable Class C
    Implements I
    Public ReadOnly Property Foo() As String Implements I.Foo
        Get
            Return "Nikov Rules"
        End Get
    End Property

    Private Shared Sub Main()
        Console.WriteLine(New C().Foo)
    End Sub
End Class
</File>
            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545890)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveCastToInterfaceForSealedType3()
            ' Note: The cast below can be removed because C is sealed and the
            ' interface member has no parameters.

            Dim markup =
<File>
Imports System

Interface I
    ReadOnly Property Foo() As String
End Interface

NotInheritable Class C
    Implements I
    Public Shared ReadOnly Property Instance() As C
        Get
            Return New C()
        End Get
    End Property

    Public ReadOnly Property Foo() As String Implements I.Foo
        Get
            Return "Nikov Rules"
        End Get
    End Property

    Private Shared Sub Main()
        Console.WriteLine([|DirectCast(Instance, I)|].Foo)
    End Sub
End Class
</File>

            Dim expected =
<File>
Imports System

Interface I
    ReadOnly Property Foo() As String
End Interface

NotInheritable Class C
    Implements I
    Public Shared ReadOnly Property Instance() As C
        Get
            Return New C()
        End Get
    End Property

    Public ReadOnly Property Foo() As String Implements I.Foo
        Get
            Return "Nikov Rules"
        End Get
    End Property

    Private Shared Sub Main()
        Console.WriteLine(Instance.Foo)
    End Sub
End Class
</File>
            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545890)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCastToInterfaceForSealedType4()
            ' Note: The cast below can't be removed (even though C is sealed)
            ' because the unspecified optional parameter default values differ.

            Dim markup =
<File>
Imports System

Interface I
    Sub Foo(Optional x As Integer = 0)
End Interface

NotInheritable Class C
    Implements I
    Public Sub Foo(Optional x As Integer = 1) Implements I.Foo
        Console.WriteLine(x)
    End Sub

    Private Shared Sub Main()
        [|DirectCast(New C(), I)|].Foo()
    End Sub
End Class
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(545890)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCastToInterfaceForSealedType5()
            ' Note: The cast below cannot be removed (even though C is sealed)
            ' because default values differ for optional parameters and
            ' hence the method is not considered an implementation. 

            Dim markup =
<File>
Imports System

Interface I
    Sub Foo(Optional x As Integer = 0)
End Interface

NotInheritable Class C
    Implements I
    Public Sub Foo(Optional x As Integer = 1) Implements I.Foo
        Console.WriteLine(x)
    End Sub

    Private Shared Sub Main()
        [|DirectCast(New C(), I)|].Foo(2)
    End Sub
End Class
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(545888)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCastToInterfaceForSealedType6()
            ' Note: The cast below can't be removed (even though C is sealed)
            ' because the specified named arguments refer to parameters that
            ' appear at different positions in the member signatures.

            Dim markup =
<File>
Imports System

Interface I
    Sub Foo(Optional x As Integer = 0, Optional y As Integer = 0)
End Interface

NotInheritable Class C
    Implements I
    Public Sub Foo(Optional y As Integer = 0, Optional x As Integer = 0) Implements I.Foo
        Console.WriteLine(x)
    End Sub

    Private Shared Sub Main()
        [|DirectCast(New C(), I)|].Foo(x:=1)
    End Sub
End Class
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(545888)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveCastToInterfaceForSealedType7()
            ' Note: The cast below can be removed as C is sealed and
            ' because the specified named arguments refer to parameters that
            ' appear at same positions in the member signatures.

            Dim markup =
<File>
Imports System

Interface I
    Function Foo(Optional x As Integer = 0, Optional y As Integer = 0) As Integer
End Interface

NotInheritable Class C
    Implements I
    Public Function Foo(Optional x As Integer = 0, Optional y As Integer = 0) As Integer Implements I.Foo
        Return x * 2
    End Function

    Private Shared Sub Main()
        Console.WriteLine([|DirectCast(New C(), I)|].Foo(x:=1))
    End Sub
End Class
</File>

            Dim expected =
<File>
Imports System

Interface I
    Function Foo(Optional x As Integer = 0, Optional y As Integer = 0) As Integer
End Interface

NotInheritable Class C
    Implements I
    Public Function Foo(Optional x As Integer = 0, Optional y As Integer = 0) As Integer Implements I.Foo
        Return x * 2
    End Function

    Private Shared Sub Main()
        Console.WriteLine(New C().Foo(x:=1))
    End Sub
End Class
</File>
            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545888)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCastToInterfaceForSealedType9()
            ' Note: The cast below can't be removed (even though C is sealed)
            ' because it would result in binding to a Dispose method that doesn't
            ' implement IDisposable.Dispose().

            Dim markup =
<File>
Imports System
Imports System.IO

NotInheritable Class C
    Inherits MemoryStream
    Private Shared Sub Main()
        Dim s As New C()
        [|DirectCast(s, IDisposable)|].Dispose()
    End Sub

    Public Shadows Sub Dispose()
        Console.WriteLine("new Dispose()")
    End Sub
End Class
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(545887)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCastToInterfaceForStruct1()
            ' Note: The cast below can't be removed because the cast boxes 's' and
            ' unboxing would change program behavior.

            Dim markup =
<File>
Imports System

Interface IIncrementable
    ReadOnly Property Value() As Integer
    Sub Increment()
End Interface

Structure S
    Implements IIncrementable
    Public Property Value() As Integer Implements IIncrementable.Value
        Get
            Return m_Value
        End Get
        Private Set
            m_Value = Value
        End Set
    End Property
    Private m_Value As Integer
    Public Sub Increment() Implements IIncrementable.Increment
        Value += 1
    End Sub

    Private Shared Sub Main()
        Dim s = New S()
        [|DirectCast(s, IIncrementable)|].Increment()
        Console.WriteLine(s.Value)
    End Sub
End Structure
</File>

            TestMissing(markup)
        End Sub

        <WorkItem(545834), WorkItem(530073)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveCastToInterfaceForStruct2()
            ' Note: The cast below can be removed because we are sure to have
            ' a fresh copy of the struct from the GetEnumerator() method.

            Dim markup =
<File>
Imports System
Imports System.Collections.Generic

Class Program
    Private Shared Sub Main()
        Call [|DirectCast(GetEnumerator(), IDisposable)|].Dispose()
    End Sub

    Private Shared Function GetEnumerator() As List(Of Integer).Enumerator
        Dim x = New List(Of Integer)() From {1, 2, 3}
        Return x.GetEnumerator()
    End Function
End Class
</File>

            Dim expected =
<File>
Imports System
Imports System.Collections.Generic

Class Program
    Private Shared Sub Main()
        Call GetEnumerator().Dispose()
    End Sub

    Private Shared Function GetEnumerator() As List(Of Integer).Enumerator
        Dim x = New List(Of Integer)() From {1, 2, 3}
        Return x.GetEnumerator()
    End Function
End Class
</File>
            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(544655)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveCastToICloneableForDelegate()
            ' Note: The cast below can be removed because delegates are implicitly sealed.

            Dim markup =
<File>
Imports System

Class C
    Private Shared Sub Main()
        Dim a As Action = Sub()
                          End Sub
        Dim c = [|DirectCast(a, ICloneable)|].Clone()
    End Sub
End Class
</File>

            Dim expected =
<File>
Imports System

Class C
    Private Shared Sub Main()
        Dim a As Action = Sub()
                          End Sub
        Dim c = a.Clone()
    End Sub
End Class
</File>
            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545926)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveCastToICloneableForArray()
            ' Note: The cast below can be removed because arrays are implicitly sealed.

            Dim markup =
<File>
Imports System

Class C
    Private Shared Sub Main()
        Dim a = New Integer() {1, 2, 3}
        Dim c = [|DirectCast(a, ICloneable)|].Clone()
    End Sub
End Class
</File>

            Dim expected =
<File>
Imports System

Class C
    Private Shared Sub Main()
        Dim a = New Integer() {1, 2, 3}
        Dim c = a.Clone()
    End Sub
End Class
</File>
            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(529937)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveCastToICloneableForArray2()
            ' Note: The cast below can be removed because arrays are implicitly sealed.

            Dim markup =
<File>
Imports System

Module module1
    Sub Main()
        Dim c = [|DirectCast({1}, ICloneable)|].Clone
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System

Module module1
    Sub Main()
        Dim c = {1}.Clone
    End Sub
End Module
</File>
            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(529897)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveCastToIConvertibleForEnum()
            ' Note: The cast below can be removed because enums are implicitly sealed.

            Dim markup =
<File>
Imports System

Class Program
    Private Shared Sub Main()
        Dim e As [Enum] = DayOfWeek.Monday
        Dim y = [|DirectCast(e, IConvertible)|].GetTypeCode()
    End Sub
End Class
</File>

            Dim expected =
<File>
Imports System

Class Program
    Private Shared Sub Main()
        Dim e As [Enum] = DayOfWeek.Monday
        Dim y = e.GetTypeCode()
    End Sub
End Class
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(844482)>
        <WorkItem(1031406)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DoNotRemoveCastFromDerivedToBaseWithImplicitReference()
            ' Cast removal changes the runtime behavior of the program.
            Dim markup =
<File>
Module Program
    Sub Main(args As String())
        Dim x As C = new C
        Dim y As C = [|DirectCast(x, D)|]
    End Sub
End Module

Class C
End Class

Class D
    Inherits C
End Class
</File>
            TestMissing(markup)
        End Sub

        <WorkItem(995908)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveCastIntroducesDuplicateAnnotations()
            Dim markup =
<File>
    <![CDATA[
Imports System.Runtime.CompilerServices
Imports N

Interface INamedTypeSymbol
    Inherits INamespaceOrTypeSymbol
End Interface

Interface INamespaceOrTypeSymbol
End Interface

Namespace N
    Friend Module INamespaceOrTypeSymbolExtensions
        <Extension>
        Public Sub ExtensionMethod(symbol As INamespaceOrTypeSymbol)
        End Sub
    End Module
End Namespace

Module Program
    Sub Main(args As String())
        Dim symbol As INamedTypeSymbol = Nothing
        [|DirectCast(symbol, INamespaceOrTypeSymbol).ExtensionMethod()|]
    End Sub
End Module
]]>
</File>

            Dim expected =
<File>
    <![CDATA[
Imports System.Runtime.CompilerServices
Imports N

Interface INamedTypeSymbol
    Inherits INamespaceOrTypeSymbol
End Interface

Interface INamespaceOrTypeSymbol
End Interface

Namespace N
    Friend Module INamespaceOrTypeSymbolExtensions
        <Extension>
        Public Sub ExtensionMethod(symbol As INamespaceOrTypeSymbol)
        End Sub
    End Module
End Namespace

Module Program
    Sub Main(args As String())
        Dim symbol As INamedTypeSymbol = Nothing
        symbol.ExtensionMethod()
    End Sub
End Module
]]>
</File>
            Test(markup, expected, compareTokens:=False)
        End Sub

#End Region

        <WorkItem(739, "#739")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveAroundArrayLiteralInInterpolation1()
            Dim markup =
<File>
Module M
    Dim x = $"{ [|CObj({})|] }" ' Remove unnecessary cast
End Module
</File>

            Dim expected =
<File>
Module M
    Dim x = $"{ {} }" ' Remove unnecessary cast
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(739, "#739")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveAroundArrayLiteralInInterpolation2()
            Dim markup =
<File>
Module M
    Dim x = $"{[|CObj({})|] }" ' Remove unnecessary cast
End Module
</File>

            Dim expected =
<File>
Module M
    Dim x = $"{({}) }" ' Remove unnecessary cast
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(739, "#739")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub RemoveAroundArrayLiteralInInterpolation3()
            Dim markup =
<File>
Module M
    Dim x = $"{ [|CObj({})|]}" ' Remove unnecessary cast
End Module
</File>

            Dim expected =
<File>
Module M
    Dim x = $"{ {}}" ' Remove unnecessary cast
End Module
</File>

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(2761, "https://github.com/dotnet/roslyn/issues/2761")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCastFromBaseToDerivedWithNarrowingReference()
            Dim markup =
<File>
Module Module1
    Private Function NewMethod(base As Base) As Base
        Return If([|TryCast(base, Derived1)|], New Derived1())
    End Function
End Module

Class Base
End Class

Class Derived1 : Inherits Base
End Class

Class Derived2 : Inherits Base
End Class
</File>
            TestMissing(markup)
        End Sub

        <WorkItem(3254, "https://github.com/dotnet/roslyn/issues/3254")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCastToTypeParameterWithExceptionConstraint()
            Dim markup =
<File>
Imports System

Class Program
    Private Shared Sub RequiresCondition(Of TException As Exception)(condition As Boolean, messageOnFalseCondition As String)
        If Not condition Then
            Throw [|DirectCast(Activator.CreateInstance(GetType(TException), messageOnFalseCondition), TException)|]
        End If
    End Sub
End Class
</File>
            TestMissing(markup)
        End Sub

        <WorkItem(3254, "https://github.com/dotnet/roslyn/issues/3254")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DontRemoveCastToTypeParameterWithExceptionSubTypeConstraint()
            Dim markup =
<File>
Imports System

Class Program
    Private Shared Sub RequiresCondition(Of TException As ArgumentException)(condition As Boolean, messageOnFalseCondition As String)
        If Not condition Then
            Throw [|DirectCast(Activator.CreateInstance(GetType(TException), messageOnFalseCondition), TException)|]
        End If
    End Sub
End Class
</File>
            TestMissing(markup)
        End Sub

        <WorkItem(3163, "https://github.com/dotnet/roslyn/issues/3163")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DoNotRemoveCastInUserDefinedNarrowingConversionStrictOn()
            Dim markup =
<File>
Option Strict On

Module Module1

    Sub Main()
        Dim red = ColorF.FromArgb(255, 255, 0, 0)
        Dim c As Color = [|CType(red, Color)|]
    End Sub

End Module

Public Structure ColorF
    Public A, R, G, B As Single
    Public Shared Function FromArgb(a As Double, r As Double, g As Double, b As Double) As ColorF
        Return New ColorF With {.A = CSng(a), .R = CSng(r), .G = CSng(g), .B = CSng(b)}
    End Function
    Public Shared Widening Operator CType(x As Color) As ColorF
        Return ColorF.FromArgb(x.A / 255, x.R / 255, x.G / 255, x.B / 255)
    End Operator
    Public Shared Narrowing Operator CType(x As ColorF) As Color
        Return Color.FromArgb(CByte(x.A * 255), CByte(x.R * 255), CByte(x.G * 255), CByte(x.B * 255))
    End Operator
End Structure

Public Structure Color
    Public A, R, G, B As Byte
    Public Shared Function FromArgb(a As Byte, r As Byte, g As Byte, b As Byte) As Color
        Return New Color With {.A = a, .R = r, .G = g, .B = b}
    End Function
End Structure
</File>
            TestMissing(markup)
        End Sub

        <WorkItem(3163, "https://github.com/dotnet/roslyn/issues/3163")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)>
        Public Sub DoNotRemoveCastInUserDefinedNarrowingConversionStrictOff()
            Dim markup =
<File>
Option Strict Off

Module Module1

    Sub Main()
        Dim red = ColorF.FromArgb(255, 255, 0, 0)
        Dim c As Color = [|CType(red, Color)|]
    End Sub

End Module

Public Structure ColorF
    Public A, R, G, B As Single
    Public Shared Function FromArgb(a As Double, r As Double, g As Double, b As Double) As ColorF
        Return New ColorF With {.A = CSng(a), .R = CSng(r), .G = CSng(g), .B = CSng(b)}
    End Function
    Public Shared Widening Operator CType(x As Color) As ColorF
        Return ColorF.FromArgb(x.A / 255, x.R / 255, x.G / 255, x.B / 255)
    End Operator
    Public Shared Narrowing Operator CType(x As ColorF) As Color
        Return Color.FromArgb(CByte(x.A * 255), CByte(x.R * 255), CByte(x.G * 255), CByte(x.B * 255))
    End Operator
End Structure

Public Structure Color
    Public A, R, G, B As Byte
    Public Shared Function FromArgb(a As Byte, r As Byte, g As Byte, b As Byte) As Color
        Return New Color With {.A = a, .R = r, .G = g, .B = b}
    End Function
End Structure
</File>
            TestMissing(markup)
        End Sub
    End Class
End Namespace
