' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests
    Public Class LineEditTests
        Inherits EditingTestBase

        Private Function ToCode(element As XElement) As String
            Return element.Value.Replace(vbLf, vbCrLf)
        End Function

        Private Function ToCode(element As XCData) As String
            Return element.Value.Replace(vbLf, vbCrLf)
        End Function

#Region "Methods"

        <Fact>
        Public Sub Method_Update1()
            Dim src1 = ToCode(<text>
Class C
    Shared Sub Bar()
        Console.ReadLine(1)
    End Sub
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Shared Sub Bar()


        Console.ReadLine(2)
    End Sub
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {})
        End Sub

        <Fact>
        Public Sub Method_Reorder1()
            Dim src1 = ToCode(<text>
Class C
    Shared Sub Goo()
        Console.ReadLine(1)
    End Sub

    Shared Sub Bar()
        Console.ReadLine(2)
    End Sub
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Shared Sub Bar()
        Console.ReadLine(2)
    End Sub

    Shared Sub Goo()
        Console.ReadLine(1)
    End Sub
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New LineChange(2, 6), New LineChange(6, 2)}, {})
        End Sub

        <Fact>
        Public Sub Method_Reorder2()
            Dim src1 = ToCode(<text>
Class Program
    Shared Sub Main()
        Goo()
        Bar()
    End Sub

    Shared Function Goo() As Integer
        Return 1
    End Function

    Shared Function Bar() As Integer
        Return 2
    End Function
End Class
</text>)

            Dim src2 = ToCode(<text>
Class Program
    Shared Function Goo() As Integer
        Return 1
    End Function

    Shared Sub Main()
        Goo()
        Bar()
    End Sub

    Shared Function Bar() As Integer
        Return 2
    End Function
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New LineChange(2, 6), New LineChange(7, 2)}, {})
        End Sub

        <Fact>
        Public Sub Method_LineChange1()
            Dim src1 = ToCode(<text>
Class C
    Shared Sub Bar()
        Console.ReadLine(2)
    End Sub
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C


    Shared Sub Bar()
        Console.ReadLine(2)
    End Sub
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New LineChange(2, 4)}, {})
        End Sub

        <Fact>
        Public Sub Method_LineChangeWithLambda1()
            Dim src1 = ToCode(<text>
Class C
    Shared Sub Bar()
        F(Function() 1)
    End Sub
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C


    Shared Sub Bar()
        F(Function() 1)
    End Sub
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New LineChange(2, 4)}, {})
        End Sub

        <Fact>
        Public Sub Method_Recompile1()
            Dim src1 = ToCode(<text>
Class C
    Shared Sub Bar()
        Console.ReadLine(2)
    End Sub
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Shared Sub _
            Bar()
        Console.ReadLine(2)
    End Sub
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Shared Sub _"})
        End Sub

        <Fact>
        Public Sub Method_Recompile2()
            Dim src1 = ToCode(<text>
Class C
    Shared Sub Bar()
        Console.ReadLine(2)
    End Sub
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Shared Sub Bar()
              Console.ReadLine(2)
    End Sub
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Shared Sub Bar()"})
        End Sub

        <Fact>
        Public Sub Method_Recompile3()
            Dim src1 = ToCode(<text>
Class C
    Shared Sub Bar()
        Console.ReadLine(2)
    End Sub
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Shared Sub Bar()
        Console.ReadLine(2)
        
    End Sub
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Shared Sub Bar()"})
        End Sub

        <Fact>
        Public Sub Method_Recompile4()
            Dim src1 = ToCode(<text>
Class C
    Shared Sub Bar()

        Console.ReadLine(1)
        Console.ReadLine(2)
    End Sub
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Shared Sub Bar()
        Console.ReadLine(1)

        Console.ReadLine(2)
    End Sub
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Shared Sub Bar()"})
        End Sub

        <Fact>
        Public Sub Method_Recompile5()
            Dim src1 = "
Class C
    Shared Sub Bar()
        Dim <N:0.0>a</N:0.0> = 1
        Dim <N:0.1>b</N:0.1> = 2
        <AS:0>System.Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Shared Sub Bar()
             Dim <N:0.0>a</N:0.0> = 1
        Dim <N:0.1>b</N:0.1> = 2
        <AS:0>System.Console.WriteLine(1)</AS:0>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Shared Sub Bar()"})

            Dim active = GetActiveStatements(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifySemantics(active,
                                  {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Bar"), syntaxMap(0))})
        End Sub

        <Fact>
        Public Sub Method_Recompile6()
            Dim src1 = ToCode(<![CDATA[
Class C
    Shared Sub Bar() : End Sub
End Class
]]>)

            Dim src2 = ToCode(<![CDATA[
Class C
        Shared Sub Bar() : End Sub
End Class
]]>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Shared Sub Bar() : End Sub"})
        End Sub

        <Fact>
        Public Sub Method_RudeRecompile1()
            Dim src1 = ToCode(<text>
Class C(Of T)
    Shared Sub Bar()
        
        Console.ReadLine(2)
    End Sub
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C(Of T)
    Shared Sub Bar()
        Console.ReadLine(2)
    End Sub
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({},
                                  {"Shared Sub Bar()"},
                                  Diagnostic(RudeEditKind.GenericTypeTriviaUpdate, vbCrLf & "        ", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub Method_RudeRecompile2()
            Dim src1 = ToCode(<text>
Class C(Of T)
    Shared Sub Bar()
        Console.ReadLine(2)
    End Sub
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C(Of T)
    Shared Sub Bar()
            Console.ReadLine(2)
    End Sub
End Class
</text>)
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({},
                                  {"Shared Sub Bar()"},
                                  Diagnostic(RudeEditKind.GenericTypeTriviaUpdate, vbCrLf & "            ", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub Method_RudeRecompile3()
            Dim src1 = ToCode(<text>
Class C
    Shared Sub Bar(Of T)()
            Console.ReadLine(2)
    End Sub
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Shared Sub Bar(Of T)()
        Console.ReadLine(2)
    End Sub
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({},
                                  {"Shared Sub Bar(Of T)()"},
                                  Diagnostic(RudeEditKind.GenericMethodTriviaUpdate, vbCrLf & "        ", FeaturesResources.method))
        End Sub

        <Fact>
        Public Sub Method_RudeRecompile4()
            Dim src1 = ToCode(<text>
Class C
    Shared Async Function Bar() As Task(Of Integer)
        Console.WriteLine(2)
    End Function
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Shared Async Function Bar() As Task(Of Integer)
        Console.WriteLine(
            2)
    End Function
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({},
                                  {"Shared Async Function Bar() As Task(Of Integer)"})
        End Sub

#End Region

#Region "Constructors"

        <Fact>
        Public Sub Constructor_Recompile1()
            Dim src1 =
"Class C" & vbCrLf &
    "Shared Sub New()" & vbCrLf &
        "Console.ReadLine(2)" & vbCrLf &
    "End Sub" & vbCrLf &
"End Class"

            Dim src2 =
"Class C" & vbCrLf &
    "Shared Sub _" & vbLf &
                "New()" & vbCrLf &
        "Console.ReadLine(2)" & vbCrLf &
    "End Sub" & vbCrLf &
"End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Shared Sub _" & vbLf & "New()"})
        End Sub

        <Fact>
        Public Sub Constructor_Recompile2()
            Dim src1 =
"Class C" & vbCrLf &
    "Shared Sub New()" & vbCrLf &
        "MyBase.New()" & vbCrLf &
    "End Sub" & vbCrLf &
"End Class"

            Dim src2 =
"Class C" & vbCrLf &
    "Shared Sub _" & vbLf &
                "New()" & vbCrLf &
        "MyBase.New()" & vbCrLf &
    "End Sub" & vbCrLf &
"End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Shared Sub _" & vbLf & "New()"})
        End Sub

#End Region

#Region "Fields"

        <Fact>
        Public Sub Field_Init_Reorder1()
            Dim src1 = ToCode(<text>
Class C
    Shared Goo As Integer = 1
    Shared Bar As Integer = 2
End Class
</text>)
            Dim src2 = ToCode(<text>
Class C
    Shared Bar As Integer = 2
    Shared Goo As Integer = 1
End Class
</text>)
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New LineChange(2, 3), New LineChange(3, 2)}, {})
        End Sub

        <Fact>
        Public Sub Field_AsNew_Reorder1()
            Dim src1 = ToCode(<text>
Class C
    Shared a As New C()
    Shared c As New C()
End Class
</text>)
            Dim src2 = ToCode(<text>
Class C
    Shared c As New C()
    Shared a As New C()
End Class
</text>)
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New LineChange(2, 3), New LineChange(3, 2)}, {})
        End Sub

        <Fact>
        Public Sub Field_AsNew_Reorder2()
            Dim src1 = ToCode(<text>
Class C
    Shared a, b As New C()
    Shared c, d As New C()
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Shared c, d As New C()
    Shared a, b As New C()
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New LineChange(2, 3),
                                   New LineChange(2, 3),
                                   New LineChange(3, 2),
                                   New LineChange(3, 2)}, {})
        End Sub

        <Fact>
        Public Sub Field_Init_LineChange1()
            Dim src1 = ToCode(<text>
Class C
    Dim Goo = 1
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C


    Dim Goo = 1
End Class
</text>)
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New LineChange(2, 4)}, {})
        End Sub

        <Fact>
        Public Sub Field_Init_LineChange2()
            Dim src1 = ToCode(<text>
Class C
    Dim Goo = 1
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Dim _
        Goo = 1
End Class
</text>)
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New LineChange(2, 3)}, {})
        End Sub

        <Fact>
        Public Sub Field_AsNew_LineChange1()
            Dim src1 = ToCode(<text>
Class C
    Dim Goo As New D()
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Dim _
        Goo As New D()
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New LineChange(2, 3)}, {})
        End Sub

        <Fact>
        Public Sub Field_AsNew_LineChange2()
            Dim src1 = ToCode(<text>
Class C
    Private Shared Goo As New D()
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Private _
            Shared Goo As New D()
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New LineChange(2, 3)}, {})
        End Sub

        <Fact>
        Public Sub Field_AsNew_LineChange_WithLambda()
            Dim src1 = ToCode(<text>
Class C
    Dim Goo, Bar As New D(Function() 1)
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Dim Goo, _
             Bar As New D(Function() 1)
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New LineChange(2, 3)}, {"Goo"})
        End Sub

        <Fact>
        Public Sub Field_ArrayInit_LineChange1()
            Dim src1 = ToCode(<text>
Class C
    Dim Goo(1)
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C


    Dim Goo(1)
End Class
</text>)
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New LineChange(2, 4)}, {})
        End Sub

        <Fact>
        Public Sub Field_ArrayInit_LineChange2()
            Dim src1 = ToCode(<text>
Class C
    Dim Goo(1)
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Dim _
        Goo(1)
End Class
</text>)
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New LineChange(2, 3)}, {})
        End Sub

        <Fact>
        Public Sub Field_Init_Recompile1a()
            Dim src1 = ToCode(<text>
Class C
    Dim Goo = 1
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Dim Goo = _
              1
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Goo = _"})
        End Sub

        <Fact>
        Public Sub Field_Init_Recompile1b()
            Dim src1 = ToCode(<text>
Class C
    Dim Goo = 1
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Dim Goo _ 
            = 1
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Goo _ "})
        End Sub

        <Fact>
        Public Sub Field_Init_Recompile1c()
            Dim src1 = ToCode(<text>
Class C
    Dim Goo ? = 1
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Dim Goo _
            ? = 1
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Goo _"})
        End Sub

        <Fact>
        Public Sub Field_Init_Recompile1()
            Dim src1 = ToCode(<text>
Class C
    Dim Goo = 1
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Dim Goo =  1
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Goo =  1"})
        End Sub

        <Fact>
        Public Sub Field_Init_Recompile2()
            Dim src1 = ToCode(<text>
Class C
    Dim Goo As Integer = 1 + 1
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Dim Goo As Integer = 1 +  1
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Goo As Integer = 1 +  1"})
        End Sub

        <Fact>
        Public Sub Field_SingleAsNew_Recompile1()
            Dim src1 = ToCode(<text>
Class C
    Dim Goo As New D()
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Dim Goo As _
               New D()
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Goo As _"})
        End Sub

        <Fact>
        Public Sub Field_SingleAsNew_Recompile2()
            Dim src1 = ToCode(<text>
Class C
    Dim Goo As New D()
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Dim Goo _
            As New D()
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Goo _"})
        End Sub

        <Fact>
        Public Sub Field_MultiAsNew_Recompile1()
            Dim src1 = ToCode(<text>
Class C
    Dim Goo, Bar As New D()
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Dim Goo, _
             Bar As New D()
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)

            ' to make it simpler, we recompile the constructor (by reporting a field as a node update)
            edits.VerifyLineEdits({New LineChange(2, 3)}, {"Goo"})
        End Sub

        <Fact>
        Public Sub Field_MultiAsNew_Recompile2()
            Dim src1 = ToCode(<text>
Class C
    Dim Goo, Bar As New D()
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Dim Goo,  Bar As New D()
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)

            ' we treat "Goo + New D()" as a whole for simplicity
            edits.VerifyLineEdits({}, {"Goo", "Bar"})
        End Sub

        <Fact>
        Public Sub Field_MultiAsNew_Recompile3()
            Dim src1 = ToCode(<text>
Class C
    Dim  Goo, Bar As New D()
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Dim Goo, Bar As New D()
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Goo", "Bar"})
        End Sub

        <Fact>
        Public Sub Field_MultiAsNew_Recompile4()
            Dim src1 = ToCode(<text>
Class C
    Dim Goo, Bar As New D()
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Dim Goo, Bar As _
                    New D()
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Goo", "Bar"})
        End Sub

        <Fact>
        Public Sub Field_ArrayInit_Recompile1()
            Dim src1 = ToCode(<text>
Class C
    Dim Goo(1)
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Dim  Goo(1)
End Class
</text>)
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Goo(1)"})
        End Sub

        <Fact>
        Public Sub Field_RudeRecompile1()
            Dim src1 = ToCode(<text>
Class C(Of T)
    Dim Goo As Integer = 1 + 1
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C(Of T)
    Dim Goo As Integer = 1 +  1
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({},
                                  {"Goo As Integer = 1 +  1"},
                                  Diagnostic(RudeEditKind.GenericTypeTriviaUpdate, "  ", FeaturesResources.field))
        End Sub
#End Region

#Region "Auto-Properties"
        <Fact>
        Public Sub Property_NoChange1()
            Dim src1 = ToCode(<text>
Class C
    Property Goo As Integer = 1 Implements I.P
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Property Goo As Integer = 1 _
                                Implements I.P
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {})
        End Sub

        <Fact>
        Public Sub PropertyTypeChar_NoChange1()
            Dim src1 = ToCode(<text>
Class C
    Property Goo$ = "" Implements I.P
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Property Goo$ = "" _
                       Implements I.P
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {})
        End Sub

        <Fact>
        Public Sub PropertyAsNew_NoChange1()
            Dim src1 = ToCode(<text>
Class C
    Property Goo As New C() Implements I.P
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Property Goo As New C() _
                            Implements I.P
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {})
        End Sub

        <Fact>
        Public Sub Property_LineChange1()
            Dim src1 = ToCode(<text>
Class C
    Property Goo As Integer = 1
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C

    Property Goo As Integer = 1
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New LineChange(2, 3)}, {})
        End Sub

        <Fact>
        Public Sub Property_LineChange2()
            Dim src1 = ToCode(<text>
Class C
    Property Goo As Integer = 1
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Property _
             Goo As Integer = 1
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New LineChange(2, 3)}, {})
        End Sub

        <Fact>
        Public Sub PropertyTypeChar_LineChange2()
            Dim src1 = ToCode(<text>
Class C
    Property Goo$ = ""
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Property _
             Goo$ = ""
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New LineChange(2, 3)}, {})
        End Sub

        <Fact>
        Public Sub PropertyAsNew_LineChange1()
            Dim src1 = ToCode(<text>
Class C
    Property Goo As New C()
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Property _
             Goo As New C()
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New LineChange(2, 3)}, {})
        End Sub

        <Fact>
        Public Sub Property_Recompile1()
            Dim src1 = ToCode(<text>
Class C
    Property Goo As Integer = 1
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Property Goo _
                 As Integer = 1
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Property Goo _"})
        End Sub

        <Fact>
        Public Sub Property_Recompile2()
            Dim src1 = ToCode(<text>
Class C
    Property Goo As Integer = 1
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Property Goo As _
                    Integer = 1
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Property Goo As _"})
        End Sub

        <Fact>
        Public Sub Property_Recompile3()
            Dim src1 = ToCode(<text>
Class C
    Property Goo As Integer = 1
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Property Goo As Integer _
                            = 1
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Property Goo As Integer _"})
        End Sub

        <Fact>
        Public Sub Property_Recompile4()
            Dim src1 = ToCode(<text>
Class C
    Property Goo As Integer = 1
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Property Goo As Integer = _
                              1
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Property Goo As Integer = _"})
        End Sub

        <Fact>
        Public Sub PropertyAsNew_Recompile1()
            Dim src1 = ToCode(<text>
Class C
    Property Goo As New C()
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Property Goo As _
                    New C()
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Property Goo As _"})
        End Sub

        <Fact>
        Public Sub PropertyTypeChar_Recompile1()
            Dim src1 = ToCode(<text>
Class C
    Property Goo$ = ""
End Class
</text>)

            Dim src2 = ToCode(<text>
Class C
    Property Goo$ = _
                    ""
End Class
</text>)

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({}, {"Property Goo$ = _"})
        End Sub
#End Region

    End Class
End Namespace
