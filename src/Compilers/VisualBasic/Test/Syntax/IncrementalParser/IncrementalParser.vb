' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities

Public Class IncrementalParser

    Private ReadOnly _s As String = <![CDATA[
'-----------------------
'
'  Copyright (c)
'
'-----------------------

#const BLAH = true

Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.VisualBasic

Public Module ParseExprSemantics
    Dim text = SourceText.From("")
    Dim tree As SyntaxTree = Nothing

    #const x = 1
    ''' <summary>
    ''' This is just gibberish to test parser
    ''' </summary>
    ''' <param name="ITERS"> haha </param>
    ''' <remarks></remarks>
    Public Sub Run(ByVal ITERS As Long)
        Console.WriteLine()

#if BLAH       
        Console.WriteLine("==== Parsing file: " & "Sample_ExpressionSemantics.vb")
        Console.WriteLine("Iterations:" & ITERS)
#end if

        Dim str = IO.File.ReadAllText("Sample_ExpressionSemantics.vb")
        Dim lineNumber = 28335
        Dim root As SyntaxNode = Nothing

        dim s1 = sub () If True Then Console.WriteLine(1) :
        dim s2 = sub () If True Then Console.WriteLine(1) ::Console.WriteLine(1)::
        dim s3 = sub() If True Then Console.WriteLine(1) :::: Console.WriteLine(1)

        Dim sw = System.Diagnostics.Stopwatch.StartNew()
        For i As Integer = 0 To ITERS - 1
            tree = SyntaxTree.Parse(text, Nothing)
            root = tree.Root
            Console.Write(".")
            Dim highWater As Integer = Math.Max(highWater, System.GC.GetTotalMemory(False))
        Next

Dim grouped = From 
node 
In root.GetNodesWhile(root.FullSpan, Function() True)
                        Group By node.Kind Into Group, Count()
                        Order By Count Descending
                        Take 30

        Console.WriteLine("Quick token cache-hits: {0} ({1:G2}%)", Stats.quickReturnedToken, 100.0 * Stats.quickReturnedToken / Stats.quickAttempts)

    End Sub
End Module]]>.Value

    <Fact>
    Public Sub FakeEdits()
        Dim text As SourceText = SourceText.From(_s)
        Dim tree As SyntaxTree = Nothing
        Dim root As SyntaxNode = Nothing
        tree = VisualBasicSyntaxTree.ParseText(text)
        root = tree.GetRoot()

        Assert.Equal(False, root.ContainsDiagnostics)

        For i As Integer = 0 To text.Length - 11
            Dim span = New TextSpan(i, 10)
            text = text.WithChanges(New TextChange(span, text.ToString(span)))

            tree = tree.WithChangedText(text)

            Dim prevRoot = root
            root = tree.GetRoot()

            Assert.True(prevRoot.IsEquivalentTo(root))
        Next
    End Sub

    <Fact>
    Public Sub TypeAFile()
        Dim text As SourceText = SourceText.From("")
        Dim tree As SyntaxTree = Nothing
        tree = VisualBasicSyntaxTree.ParseText(text)

        Assert.Equal(False, tree.GetRoot().ContainsDiagnostics)

        For i As Integer = 0 To _s.Length - 1
            ' add next character in file 's' to text
            Dim newText = text.WithChanges(New TextChange(New TextSpan(text.Length, 0), _s.Substring(i, 1)))
            Dim newTree = tree.WithChangedText(newText)
            Dim tmpTree = VisualBasicSyntaxTree.ParseText(newText)

            VerifyEquivalent(newTree, tmpTree)
            text = newText
            tree = newTree
        Next
    End Sub

    <Fact>
    Public Sub Preprocessor()
        Dim oldText = SourceText.From(_s)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        ' commenting out the #const
        Dim pos = _s.IndexOf("#const", StringComparison.Ordinal)
        Dim newText = oldText.WithChanges(New TextChange(New TextSpan(pos, 0), "'"))
        Dim newTree = oldTree.WithChangedText(newText)
        Dim tmpTree = VisualBasicSyntaxTree.ParseText(newText)

        VerifyEquivalent(newTree, tmpTree)

        ' removes ' from the '#const
        Dim newString = newText.ToString()
        pos = newString.IndexOf("'#const", StringComparison.Ordinal)
        Dim anotherText = newText.WithChanges(New TextChange(New TextSpan(pos, 1), ""))
        newTree = newTree.WithChangedText(anotherText)
        tmpTree = VisualBasicSyntaxTree.ParseText(anotherText)

        VerifyEquivalent(newTree, tmpTree)
    End Sub

#Region "Regressions"
    <WorkItem(899264, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseWithEventsFollowingProperty()
        'Unable to verify this using CDATA, since CDATA value only has Cr appended at end of each line, 
        'where as this bug is reproducible only with CrLf at the end of each line
        Dim code As String = "Public Class HasPublicMembersToConflictWith" & vbCrLf &
    "Public ConflictWithProp" & vbCrLf &
    "" & vbCrLf &
    "Public Property _ConflictWithBF() As String" & vbCrLf &
    "    Get" & vbCrLf &
    "    End Get" & vbCrLf &
    "    Set(value As String)" & vbCrLf &
    "    End Set" & vbCrLf &
    "End Property" & vbCrLf &
    "" & vbCrLf &
    "Public WithEvents ConflictWithBoth As Ob"

        ParseAndVerify(code,
        <errors>
            <error id="30481"/>
        </errors>)

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "j",
        .changeSpan = New TextSpan(code.Length, 0),
        .changeType = ChangeType.Insert})
    End Sub

    <WorkItem(899596, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseClassFollowingDocComments()
        Dim code As String = <![CDATA[Class VBQATestcase
    '''-----------------------------------------------------------------------------
    ''' <summary>
    '''Text
    ''' </summary>
    '''-----------------------------------------------------------------------------]]>.Value

        ParseAndVerify(code,
                       <errors>
                           <error id="30481"/>
                       </errors>)

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = vbCrLf & "Pub",
        .changeSpan = New TextSpan(code.Length, 0),
        .changeType = ChangeType.Insert})
    End Sub

    <WorkItem(899918, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseDirInElse()
        Dim code As String = "Sub Sub1()" & vbCr &
"If true Then" & vbCr &
"goo("""")" & vbCr &
"Else" & vbCr & vbCr &
"#If Not ULTRAVIOLET Then" & vbCr

        ParseAndVerify(code,
                       <errors>
                           <error id="30012"/>
                           <error id="30081"/>
                           <error id="30026"/>
                       </errors>)

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "a",
        .changeSpan = New TextSpan(code.Length, 0),
        .changeType = ChangeType.Insert})
    End Sub

    <WorkItem(899938, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseNamespaceFollowingEvent()
        Dim code As String = "Class cls1" & vbCrLf &
"Custom Event myevent As del" & vbCrLf &
"End Event" & vbCrLf &
"End Class" & vbCrLf &
"Namespace r"

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "e",
        .changeSpan = New TextSpan(code.Length, 0),
        .changeType = ChangeType.Insert})
    End Sub

    <WorkItem(900209, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseCaseElse()
        Dim code As String = (<![CDATA[
      Sub main()
         Select Case 5
            Case Else
               vvv = 6
            Case Else
               vvv = 7
]]>).Value

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = vbCrLf,
        .changeSpan = New TextSpan(code.Length, 0),
        .changeType = ChangeType.Insert})
    End Sub

    <WorkItem(901386, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseExplicitOnGroupBy()
        Dim code As String = (<![CDATA[
Option Explicit On
Sub goo()
Dim q2 = From x In col let y = x Group x, y By]]>).Value

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "a",
        .changeSpan = New TextSpan(code.Length, 0),
        .changeType = ChangeType.Insert})
    End Sub

    <WorkItem(901639, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseExprLambdaInSubContext()
        Dim code As String = (<![CDATA[Function() NewTextPI.UnwrapObject().FileCodeModel)
If True Then
End If
End Sub
Class WillHaveAnError
End class
Class willBeReused
End class]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "(",
        .changeSpan = New TextSpan(0, 0),
        .changeType = ChangeType.InsertBefore})

    End Sub

    <Fact>
    Public Sub IncParseExprLambdaInSubContext2()
        Dim code As String = (<![CDATA[Function() NewTextPI.UnwrapObject().FileCodeModel)
If True Then
Else
End If
End Sub
Class WillHaveAnError
End class
Class willBeReused
End class]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "(",
        .changeSpan = New TextSpan(0, 0),
        .changeType = ChangeType.InsertBefore})

    End Sub

    <WorkItem(901645, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseExitFunction()
        Dim code As String = (<![CDATA[Function
If strSwitches <> "" Then strCLine = strCLine & " " & strSwitches
End Sub]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "Exit ",
        .changeSpan = New TextSpan(0, 0),
        .changeType = ChangeType.InsertBefore})

    End Sub

    <WorkItem(901655, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseDateLiteral()

        IncParseAndVerify(New IncParseNode With {
        .oldText = "",
        .changeText = "#10/18/1969# hello 123",
        .changeSpan = New TextSpan(0, 0),
        .changeType = ChangeType.InsertBefore})

    End Sub

    <Fact>
    Public Sub IncParsePPElse()
        Dim code As String = (<![CDATA[
Function goo() As Boolean

#Else

    Dim roleName As Object
    For Each roleName In wbirFields
    Next roleName


End Function
]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "#End If",
        .changeSpan = New TextSpan(code.IndexOf("Next roleName", StringComparison.Ordinal) + 15, 2),
        .changeType = ChangeType.Replace})
    End Sub

    <Fact>
    Public Sub IncParsePPElse1()
        Dim code As String = (<![CDATA[
Function goo() As Boolean

#Else

    Dim roleName As Object
    For Each roleName In wbirFields
    Next roleName

#End IF

End Function
]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "#If true " & vbCrLf,
        .changeSpan = New TextSpan(code.IndexOf("#Else", StringComparison.Ordinal), 0),
        .changeType = ChangeType.Replace})
    End Sub

    <WorkItem(901669, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseXmlTagWithExprHole()
        Dim code As String = (<![CDATA[e a=<%= b %>>]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "<",
        .changeSpan = New TextSpan(0, 0),
        .changeType = ChangeType.InsertBefore})
    End Sub

    <WorkItem(901671, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseEndBeforeSubWithX()
        Dim code As String = (<![CDATA[Sub
        End Class
    X]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "End ",
        .changeSpan = New TextSpan(0, 0),
        .changeType = ChangeType.InsertBefore})
    End Sub

    <WorkItem(901676, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseInterfaceFollByConstructs()
        Dim code As String = (<![CDATA[
        Public Interface I2
        End Interface
        Sub SEHIllegal501()
            Try
            Catch
            End Try
            Exit Sub
        End Sub
    X]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "Interface",
        .changeSpan = New TextSpan(0, 0),
        .changeType = ChangeType.InsertBefore})
    End Sub

    <WorkItem(901680, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseLCFunctionCompoundAsn()
        Dim code As String = (<![CDATA[Public Function goo() As String
            For i As Integer = 0 To  1
                total += y(i)
            Next
End Function]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "> _" & vbCrLf,
        .changeSpan = New TextSpan(0, 0),
        .changeType = ChangeType.InsertBefore})
    End Sub

    <WorkItem(902710, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseInsertFunctionBeforeEndClass()
        Dim code As String = (<![CDATA[End Class
MustInherit Class C10
End Class]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "Function" & vbCrLf,
        .changeSpan = New TextSpan(0, 0),
        .changeType = ChangeType.InsertBefore})
    End Sub

    <WorkItem(903134, "DevDiv/Personal")>
    <Fact>
    Public Sub InsertSubBeforeCustomEvent()
        Dim code As String = (<![CDATA[            Custom Event e As del
                AddHandler(ByVal value As del)
                End AddHandler
            End Event]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "Sub" & vbCrLf,
        .changeSpan = New TextSpan(0, 0),
        .changeType = ChangeType.InsertBefore})
    End Sub

    <WorkItem(903555, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseMergedForEachAndDecl()
        Dim code As String = (<![CDATA[#Region "abc"
Function goo() As Boolean
Dim roleName As Object
For Each roleName In wbirFields
Next roleName
End Function
#End Region]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = vbCrLf,
        .changeSpan = New TextSpan(code.IndexOf("Dim roleName As Object", StringComparison.Ordinal) + 22, 2),
        .changeType = ChangeType.Remove})
    End Sub

    <WorkItem(903805, "DevDiv/Personal")>
    <Fact>
    Public Sub ParseEnumWithoutEnd()
        Dim code As String = (<![CDATA[Public Class Class2
    Protected Enum e
        e1
        e2
	End Enum
    Public Function Goo(ByVal arg1 As e) As e
    End Function
End Class]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = vbCrLf,
        .changeSpan = New TextSpan(code.IndexOf("e2", StringComparison.Ordinal) + 2, 2),
        .changeType = ChangeType.Remove})
    End Sub

    <WorkItem(903826, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseWrongSelectFollByIf()
        Dim code As String = (<![CDATA[        Sub goo()
                Select Case lng
                    Case 44
                        int1 = 4
                End Select
                If true Then
                End If
        End Sub]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "End ",
        .changeSpan = New TextSpan(code.IndexOf("End ", StringComparison.Ordinal), 4),
        .changeType = ChangeType.Remove})
    End Sub

    <WorkItem(904768, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseDoLoop()
        Dim code As String = (<![CDATA[        Sub AnonTConStmnt()
                Do
                    i += 1
                Loop Until true
        End Sub
]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = vbCrLf,
        .changeSpan = New TextSpan(code.IndexOf("Do", StringComparison.Ordinal) + 2, 2),
        .changeType = ChangeType.Remove})
    End Sub

    <WorkItem(904771, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseClassWithOpDecl()
        Dim code As String = (<![CDATA[
Friend Module m1
Class Class1
Shared Operator -(ByVal x As Class1) As Boolean
End Operator
End Class
End Module
]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "Class ",
        .changeSpan = New TextSpan(code.IndexOf("Class ", StringComparison.Ordinal), 6),
        .changeType = ChangeType.Remove})
    End Sub

    <WorkItem(904782, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParsePropFollIncompleteLambda()
        Dim code As String = (<![CDATA[        Class c1

            Public Function goo() As Object
                Dim res = Function(x As Integer) c1.Goo(x)
            End Function

            Default Public Property Prop(ByVal y As String) As Integer
                Get
                End Get
                Set(ByVal value As Integer)
                End Set
            End Property
        End Class]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = ")",
        .changeSpan = New TextSpan(code.IndexOf("x As Integer)", StringComparison.Ordinal) + 12, 1),
        .changeType = ChangeType.Remove})
    End Sub

    <WorkItem(904792, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseErroneousGroupByQuery()
        Dim code As String = (<![CDATA[        Sub goo() 
                Dim q2 = From i In str Group i By key1 = x
                Dim q3 =From j In str Group By key = i 
        End Sub]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = " By",
        .changeSpan = New TextSpan(code.IndexOf(" By key1", StringComparison.Ordinal), 3),
        .changeType = ChangeType.Remove})
    End Sub

    <WorkItem(904804, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseSetAfterIncompleteSub()
        Dim code As String = (<![CDATA[Sub goo()
End Sub
Public WriteOnly Property bar() as short
Set
End Set
End Property]]>).Value
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = vbCrLf,
        .changeSpan = New TextSpan(code.IndexOf("End Sub", StringComparison.Ordinal) + 7, 2),
        .changeType = ChangeType.Remove})
    End Sub

    <WorkItem(911100, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseEmbeddedIfsInsideCondCompile()
        Dim code As String = "Sub bar() " & vbCrLf &
"#If true Then" & vbCrLf &
    "if true Then goo()" & vbCrLf &
 "If Command() <" & vbCrLf
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = ">",
        .changeSpan = New TextSpan(code.IndexOf("If Command() <", StringComparison.Ordinal) + 14, 0),
        .changeType = ChangeType.Insert})
    End Sub

    <WorkItem(911103, "DevDiv/Personal")>
    <Fact>
    Public Sub IncParseErrorIfStatement()
        Dim code As String = "Public Sub Run() " & vbCrLf &
"If NewTextPI.DTE Is Nothing Then End" & vbCrLf &
 "End Sub"

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "NewTextPI",
        .changeSpan = New TextSpan(code.IndexOf("NewTextPI.DTE", StringComparison.Ordinal), 9),
        .changeType = ChangeType.Remove})
    End Sub

    <WorkItem(537168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537168")>
    <Fact>
    Public Sub IncParseSubBeforePartialClass()
        Dim code As String = (<![CDATA[End Class
Partial Class class3
End Class]]>).Value

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "Sub" & vbCrLf,
        .changeSpan = New TextSpan(0, 0),
        .changeType = ChangeType.InsertBefore})
    End Sub

    <WorkItem(537172, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537172")>
    <Fact>
    Public Sub IncParseInterfaceDeleteWithColon()
        Dim code As String = (<![CDATA[Interface I : Sub Goo() : End Interface]]>).Value

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "Interface ",
        .changeSpan = New TextSpan(0, 10),
        .changeType = ChangeType.Remove})
    End Sub

    <WorkItem(537174, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537174")>
    <Fact>
    Public Sub IncParseMissingEndAddHandler()
        Dim code As String = (<![CDATA[
                Class C
                    Custom Event e As del
                        AddHandler(ByVal value As del)
                        End AddHandler
                        RemoveHandler(ByVal value As del)
                        End RemoveHandler
                        RaiseEvent()
                        End RaiseEvent
                    End Event
                End Class
]]>).Value
        Dim change = "End AddHandler"
        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf(change, StringComparison.Ordinal), change.Length),
        .changeType = ChangeType.Remove})
    End Sub

    <WorkItem(539038, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539038")>
    <Fact>
    Public Sub IncParseInvalidText()
        Dim code As String = (<![CDATA[1. Verify that INT accepts an constant of each type as the
        '                  argument.]]>).Value

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = "os:    ",
        .changeSpan = New TextSpan(0, 0),
        .changeType = ChangeType.InsertBefore})
    End Sub

    <WorkItem(539053, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539053")>
    <Fact>
    Public Sub IncParseAddSubValid()
        Dim code As String = (<![CDATA[Class CGoo
    Public S()
        Dim x As Integer = 0
    End Sub
End Class]]>).Value

        Dim oldText = SourceText.From(code)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim newText = oldText.WithChanges(New TextChange(New TextSpan(22, 0), " Sub "))

        Dim incTree = oldTree.WithChangedText(newText)
        Dim newTree = VisualBasicSyntaxTree.ParseText(newText)

        Dim exp1 = newTree.GetRoot().ChildNodesAndTokens()(0).ChildNodesAndTokens()(1)
        Dim inc1 = incTree.GetRoot().ChildNodesAndTokens()(0).ChildNodesAndTokens()(1)
        Assert.Equal(SyntaxKind.SubBlock, exp1.Kind())
        Assert.Equal(exp1.Kind(), inc1.Kind())

        Dim exp2 = exp1.ChildNodesAndTokens()(1)
        Dim inc2 = inc1.ChildNodesAndTokens()(1)
        Assert.Equal(SyntaxKind.LocalDeclarationStatement, exp2.Kind())
        Assert.Equal(exp2.Kind(), inc2.Kind())

        ' this XML output is too much
        'IncParseAndVerify(New IncParseNode With {
        '.oldText = code,
        '.changeText = " Sub ",
        '.changeSpan = New TextSpan(22, 0),
        '.changeType = ChangeType.Insert})
    End Sub

    <WorkItem(538577, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538577")>
    <Fact>
    Public Sub IncParseAddSpaceAfterForNext()
        Dim code As String = (<![CDATA[Module M
  Sub Main()
   Dim i(1) As Integer
   For i(0) = 1 To 10
     For j = 1 To 10
   Next j, i(0) 
  End Sub
End Module 
]]>).Value

        Dim oldText = SourceText.From(code)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim newText = oldText.WithChanges(New TextChange(New TextSpan(103, 0), " "))
        Dim incTree = oldTree.WithChangedText(newText)
        Dim newTree = VisualBasicSyntaxTree.ParseText(newText)

        Assert.Equal(False, oldTree.GetRoot().ContainsDiagnostics)
        Assert.Equal(False, newTree.GetRoot().ContainsDiagnostics)
        Assert.Equal(False, incTree.GetRoot().ContainsDiagnostics)
        VerifyEquivalent(incTree, newTree)
    End Sub

    <Fact>
    <WorkItem(540667, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540667")>
    Public Sub IncrementalParseAddSpaceInSingleLineIf()
        ' The code below intentionally is missing a space between the "Then" and "Console"
        Dim code As String = (<![CDATA[
Module M
  Sub Main()
    If False ThenConsole.WriteLine("FIRST") : Console.WriteLine("TEST") Else Console.WriteLine("TRUE!") : 'comment
  End Sub
End Module 
]]>).Value

        Dim oldText = SourceText.From(code)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim insertionPoint = code.IndexOf("Console", StringComparison.Ordinal)
        Dim newText = oldText.WithChanges(New TextChange(New TextSpan(insertionPoint, 0), " "))
        Dim expectedTree = VisualBasicSyntaxTree.ParseText(newText)
        Dim incrementalTree = oldTree.WithChangedText(newText)

        Assert.Equal(False, expectedTree.GetRoot().ContainsDiagnostics)
        Assert.Equal(False, incrementalTree.GetRoot().ContainsDiagnostics)
        VerifyEquivalent(incrementalTree, expectedTree)
    End Sub

    <Fact>
    <WorkItem(405887, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=405887")>
    Public Sub IncrementalParseInterpolationInSingleLineIf()
        Dim code As String = (<![CDATA[
Module Module1
    Sub Test1(val1 As Integer)
        If val1 = 1 Then System.Console.WriteLine($"abc '" & sServiceName & "'")
    End Sub
End Module
]]>).Value

        Dim oldText = SourceText.From(code)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Const replace = """ &"
        Dim insertionPoint = code.IndexOf(replace, StringComparison.Ordinal)
        Dim newText = oldText.WithChanges(New TextChange(New TextSpan(insertionPoint, replace.Length), "{"))
        Dim expectedTree = VisualBasicSyntaxTree.ParseText(newText)
        Dim incrementalTree = oldTree.WithChangedText(newText)

        Assert.Equal(True, expectedTree.GetRoot().ContainsDiagnostics)
        Assert.Equal(True, incrementalTree.GetRoot().ContainsDiagnostics)
        VerifyEquivalent(incrementalTree, expectedTree)
    End Sub
#End Region

    <WorkItem(543489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543489")>
    <Fact>
    Public Sub Bug11296()

        Dim source As String = <![CDATA[    
Module M
    Sub Main()
        GoTo 100
        Dim Flag1 = 1
        If Flag1 = 1 Then
            Flag1 = 100
        Else 100:
        End If
    End Sub
End Module
]]>.Value

        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        Assert.Equal(1, oldTree.GetDiagnostics().Count)
        Assert.Equal("Syntax error.", oldTree.GetDiagnostics()(0).GetMessage(EnsureEnglishUICulture.PreferredOrNull))
        Assert.Equal("[131..134)", oldTree.GetDiagnostics()(0).Location.SourceSpan.ToString)

        ' commenting out the goto
        Dim pos = source.IndexOf("GoTo 100", StringComparison.Ordinal)
        Dim newText = oldText.WithChanges(New TextChange(New TextSpan(pos, 0), "'"))

        Dim newTree = oldTree.WithChangedText(newText)

        Dim tmpTree = VisualBasicSyntaxTree.ParseText(newText)
        Assert.Equal(1, tmpTree.GetDiagnostics().Count)
        Assert.Equal("Syntax error.", tmpTree.GetDiagnostics()(0).GetMessage(EnsureEnglishUICulture.PreferredOrNull))
        Assert.Equal("[132..135)", tmpTree.GetDiagnostics()(0).Location.SourceSpan.ToString)
    End Sub

    <Fact>
    Public Sub IncParseTypeNewLine()
        Dim code As String = (<![CDATA[
Module m
Sub s
End Sub
End Module     
]]>).Value

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = vbCrLf,
        .changeSpan = New TextSpan(15, 0),
        .changeType = ChangeType.Insert})
    End Sub

    <WorkItem(545667, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545667")>
    <Fact>
    Public Sub Bug14266()
        Dim source = <![CDATA[
Enum E
    A
End Enum
]]>.Value.Trim()
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        ' Insert a single character at the beginning.
        Dim newText = oldText.Replace(start:=0, length:=0, newText:="B")
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <WorkItem(546680, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546680")>
    <Fact>
    Public Sub Bug16533()
        Dim source = <![CDATA[
Module M
    Sub M()
        If True Then M() Else : 
    End Sub
End Module
]]>.Value
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Replace "True" with "True".
        Dim str = "True"
        Dim position = oldText.ToString().IndexOf(str, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=str.Length, newText:=str)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, oldTree)
    End Sub

    <WorkItem(546685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546685")>
    <Fact>
    Public Sub MultiLineIf()
        Dim source = <![CDATA[
Module M
    Sub M(b As Boolean)
        If b Then
        End If
    End Sub
End Module
]]>.Value.Trim()
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        ' Change "End Module" to "End module".
        Dim position = oldText.ToString().LastIndexOf("Module", StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=1, newText:="m")
        Dim newTree = oldTree.WithChangedText(newText)
        Dim diffs = SyntaxDifferences.GetRebuiltNodes(oldTree, newTree)
        ' MultiLineIfBlock should not have been reused.
        Assert.True(diffs.Any(Function(n) n.IsKind(SyntaxKind.MultiLineIfBlock)))
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    ''' <summary>
    ''' Changes before a multi-line If should
    ''' not affect reuse of the If nodes.
    ''' </summary>
    <Fact>
    Public Sub MultiLineIf_2()
        Dim source = <![CDATA[
Module M
    Sub M()
        Dim b = False
        b = True
        If b Then
        End If
    End Sub
End Module
]]>.Value.Trim()
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        ' Change "False" to "True".
        Dim position = oldText.ToString().IndexOf("False", StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=5, newText:="True")
        Dim newTree = oldTree.WithChangedText(newText)
        Dim diffs = SyntaxDifferences.GetRebuiltNodes(oldTree, newTree)
        ' MultiLineIfBlock should have been reused and should not appear in diffs.
        Assert.False(diffs.Any(Function(n) n.IsKind(SyntaxKind.MultiLineIfBlock)))
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    ''' <summary>
    ''' Changes sufficiently far after a multi-line If
    ''' should not affect reuse of the If nodes.
    ''' </summary>
    <Fact>
    Public Sub MultiLineIf_3()
        Dim source = <![CDATA[
Module M
    Sub M(b As Boolean)
        If b Then
        End If
        While b
        End While
    End Sub
End Module
]]>.Value.Trim()
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        ' Change "End Module" to "End module".
        Dim position = oldText.ToString().LastIndexOf("Module", StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=1, newText:="m")
        Dim newTree = oldTree.WithChangedText(newText)
        Dim diffs = SyntaxDifferences.GetRebuiltNodes(oldTree, newTree)
        ' MultiLineIfBlock should have been reused and should not appear in diffs.
        Assert.False(diffs.Any(Function(n) n.IsKind(SyntaxKind.MultiLineIfBlock)))
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <WorkItem(546692, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546692")>
    <Fact>
    Public Sub Bug16575()
        Dim source = <![CDATA[
Module M
    Sub M()
        If True Then Else Dim x = 1 : Dim y = x
        If True Then Else Dim x = 1 : Dim y = x
    End Sub
End Module
]]>.Value
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Add newline after first single line If
        Dim str = "y = x"
        Dim position = oldText.ToString().IndexOf(str, StringComparison.Ordinal) + str.Length
        Dim newText = oldText.Replace(start:=position, length:=0, newText:=vbCrLf)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <WorkItem(546698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546698")>
    <Fact>
    Public Sub Bug16596()
        Dim source = ToText(<![CDATA[
Module M
    Sub M()
        If True Then
            Dim x = Sub() If True Then Return : 'Else
        End If
    End Sub
End Module
]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Uncomment "Else".
        Dim position = oldText.ToString().IndexOf("'Else", StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=1, newText:="")
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <WorkItem(530662, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530662")>
    <Fact>
    Public Sub Bug16662()
        Dim source = ToText(<![CDATA[
Module M
    Sub M()
        ''' <[
        1: X
    End Sub
End Module
]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Remove "X".
        Dim position = oldText.ToString().IndexOf("X"c)
        Dim newText = oldText.Replace(start:=position, length:=1, newText:="")
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <WorkItem(546774, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546774")>
    <Fact>
    Public Sub Bug16786()
        Dim source = <![CDATA[
Namespace N
    ''' <summary/>
    Class A
    End Class
End Namespace
Class B
End Class
Class C
End Class
]]>.Value.Trim()
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        ' Append "Class".
        Dim position = oldText.ToString().Length
        Dim newText = oldText.Replace(start:=position, length:=0, newText:=vbCrLf & "Class")
        Dim newTree = oldTree.WithChangedText(newText)
        Dim diffs = SyntaxDifferences.GetRebuiltNodes(oldTree, newTree)
        ' Original Namespace should have been reused.
        Assert.False(diffs.Any(Function(n) n.IsKind(SyntaxKind.NamespaceBlock)))
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <WorkItem(530841, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530841")>
    <Fact>
    Public Sub Bug17031()
        Dim source = ToText(<![CDATA[
Module M
    Sub M()
        If True Then
        Else

        End If
    End Sub
End Module
]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Insert blank line at start of method.
        Dim str = "Sub M()"
        Dim position = oldText.ToString().IndexOf(str, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position + str.Length, length:=0, newText:=vbCrLf)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <WorkItem(531017, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531017")>
    <Fact>
    Public Sub Bug17409()
        Dim source = ToText(<![CDATA[
Module M
    Sub M()
        Dim ch As Char
        Dim ch As Char
        Select Case ch
            Case "~"c

            Case Else
        End Select
    End Sub
End Module
]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Remove second instance of "Dim ch As Char".
        Const str = "Dim ch As Char"
        Dim position = oldText.ToString().LastIndexOf(str, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=str.Length, newText:=String.Empty)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact, WorkItem(547242, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547242")>
    Public Sub IncParseAddRemoveStopAtAofAs()
        Dim code As String = <![CDATA[
Module M
    Public obj0 As Object
    Public obj1 A]]>.Value

        Dim tree = VisualBasicSyntaxTree.ParseText(code)
        Dim oldIText = tree.GetText()
        ' Remove first N characters.
        Dim span = New TextSpan(0, code.IndexOf("j0", StringComparison.Ordinal))
        Dim change = New TextChange(span, "")
        Dim newIText = oldIText.WithChanges(change)
        Dim newTree = tree.WithChangedText(newIText)
        Dim fulltree = VisualBasicSyntaxTree.ParseText(newIText.ToString())

        Dim children1 = newTree.GetRoot().ChildNodesAndTokens()
        Dim children2 = fulltree.GetRoot().ChildNodesAndTokens()
        Assert.Equal(children2.Count, children1.Count)
    End Sub

    <Fact, WorkItem(547242, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547242")>
    Public Sub IncParseAddRemoveStopAtAofAs02()
        Dim code As String = <![CDATA[
Module M
    Sub M()
        Try
        Catch ex A]]>.Value

        Dim fullTree = VisualBasicSyntaxTree.ParseText(code)
        Dim fullText = fullTree.GetText()
        Dim newTree = fullTree.WithChangedText(fullText)
        Assert.NotSame(newTree, fullTree) ' Relies on #550027 where WithChangedText returns an instance with changes.
        Assert.Equal(fullTree.GetRoot().ToFullString(), newTree.GetRoot().ToFullString())
    End Sub

    <Fact, WorkItem(547251, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547251")>
    Public Sub IncParseAddRemoveStopAtAofPropAs()
        Dim code As String =
        <![CDATA[Class C
    Inherits Attribute
]]>.Value
        Dim code1 As String = <![CDATA[    Property goo() A]]>.Value

        Dim tree = VisualBasicSyntaxTree.ParseText(code)
        Dim oldIText = tree.GetText()
        ' insert code1 after code
        Dim span = New TextSpan(oldIText.Length, 0)
        Dim change = New TextChange(span, code1)
        Dim newIText = oldIText.WithChanges(change)
        Dim newTree = tree.WithChangedText(newIText)

        ' remove
        span = New TextSpan(0, code1.Length)
        change = New TextChange(span, "")
        newIText = newIText.WithChanges(change)
        ' InvalidCastException
        newTree = newTree.WithChangedText(newIText)

        Dim fulltree = VisualBasicSyntaxTree.ParseText(newIText.ToString())
        Assert.Equal(fulltree.GetRoot().ToFullString(), newTree.GetRoot().ToFullString())

    End Sub

    <Fact, WorkItem(547303, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547303")>
    Public Sub IncParseAddRemoveStopAtTofThen()
        Dim code As String = <![CDATA[
Module M
    Sub M()
        If True Then
        ElseIf False T]]>.Value

        Dim fullTree = VisualBasicSyntaxTree.ParseText(code)
        Dim fullText = fullTree.GetText()
        Dim newTree = fullTree.WithChangedText(fullText)
        Assert.NotSame(newTree, fullTree) ' Relies on #550027 where WithChangedText returns an instance with changes.
        Assert.Equal(fullTree.GetRoot().ToFullString(), newTree.GetRoot().ToFullString())
    End Sub

    <WorkItem(571105, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/571105")>
    <Fact()>
    Public Sub IncParseInsertLineBreakBeforeLambda()
        Dim code As String = <![CDATA[
Module M
    Sub F()
        Dim a1 = If(Sub()
                    End Sub, Nothing)
    End Sub
End Module]]>.Value

        Dim tree = VisualBasicSyntaxTree.ParseText(code)
        Dim oldText = tree.GetText()
        ' insert line break after '='
        Dim span = New TextSpan(code.IndexOf("="c), 0)
        Dim change = New TextChange(span, vbCrLf)
        Dim newText = oldText.WithChanges(change)
        Dim newTree = tree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <WorkItem(578279, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578279")>
    <Fact()>
    Public Sub IncParseInsertLineBreakBetweenEndSub()
        Dim code As String = <![CDATA[Class C
    Sub M()
    En Sub
    Private F = 1
End Class]]>.Value
        Dim oldText = SourceText.From(code)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' insert line break
        Dim position = code.IndexOf("En ", StringComparison.Ordinal)
        Dim change = New TextChange(New TextSpan(position, 2), "End" + vbCrLf)
        Dim newText = oldText.WithChanges(change)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact()>
    Public Sub InsertWithinLookAhead()
        Dim code As String = <![CDATA[
Module M
    Function F(s As String)
        Return From c In s
    End Function
End Module]]>.Value
        Dim oldText = SourceText.From(code)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Insert "Select c" at end of method.
        Dim position = code.IndexOf("    End Function", StringComparison.Ordinal)
        Dim change = New TextChange(New TextSpan(position, 0), "               Select c" + vbCrLf)
        Dim newText = oldText.WithChanges(change)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

#Region "Async & Iterator"
    <Fact>
    Public Sub AsyncToSyncMethod()
        Dim source = ToText(<![CDATA[
Class C
    Async Function M(t As Task) As Task
        Await (t)
    End Function

    Function Await(t)
        Return Nothing
    End Function
End Class
]]>)

        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Insert blank line at start of method.
        Dim str = "Async"
        Dim position = oldText.ToString().IndexOf(str, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=str.Length, newText:="")
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub AsyncToSyncLambda()
        Dim source = ToText(<![CDATA[
Class C
    Function M(t As Task)
        Dim lambda = Async Function() Await(t)
    End Function

    Function Await(t)
        Return Nothing
    End Function
End Class
]]>)

        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Insert blank line at start of method.
        Dim str = "Async"
        Dim position = oldText.ToString().IndexOf(str, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=str.Length, newText:="")
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub SyncToAsyncMethod()
        Dim source = ToText(<![CDATA[
Class C
    Function M(t As Task) As Task
        Await (t)
    End Function

    Function Await(t)
        Return Nothing
    End Function
End Class
]]>)

        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Insert blank line at start of method.
        Dim str = "Function "
        Dim position = oldText.ToString().IndexOf(str, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=str.Length, newText:="Async Function ")
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub SyncToAsyncLambda()
        Dim source = ToText(<![CDATA[
Class C
    Function M(t As Task)
        Dim lambda = Function() Await(t)
    End Function

    Function Await(t)
        Return Nothing
    End Function
End Class
]]>)

        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Insert blank line at start of method.
        Dim str = "Function()"
        Dim position = oldText.ToString().IndexOf(str, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=str.Length, newText:="Async Function ")
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub AsyncToSyncMethodDecl()
        Dim source = ToText(<![CDATA[
Class C
    Async Function M(a As Await, t As Task) As Task
        Await t
    End Function
End Class

Class Await
End Class
]]>)

        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Insert blank line at start of method.
        Dim str = "Async"
        Dim position = oldText.ToString().IndexOf(str, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=str.Length, newText:="")
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub SyncToAsyncMethodDecl()
        Dim source = ToText(<![CDATA[
Class C
    Function M(a As Await, t As Task) As Task
        Await t
    End Function
End Class

Class Await
End Class
]]>)

        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Insert blank line at start of method.
        Dim str = "Function "
        Dim position = oldText.ToString().IndexOf(str, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=str.Length, newText:="Async Function ")
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IteratorToNonIteratorMethod()
        Dim source = ToText(<![CDATA[
Module Program
    Iterator Function Goo() As IEnumerable
        Yield (1)
    End Function
End Module
]]>)

        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Insert blank line at start of method.
        Dim str = "Iterator"
        Dim position = oldText.ToString().IndexOf(str, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=str.Length, newText:="")
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub NonIteratorToIteratorMethod()
        Dim source = ToText(<![CDATA[
Module Program
    Function Goo() As IEnumerable
        Yield (1)
    End Function
End Module
]]>)

        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Insert blank line at start of method.
        Dim str = "Function "
        Dim position = oldText.ToString().IndexOf(str, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=str.Length, newText:="Iterator Function ")
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IteratorToNonIteratorMethodDecl()
        Dim source = ToText(<![CDATA[
Module Program
    Iterator Function Goo(Yield As Integer) As IEnumerable
        Yield (1)
    End Function
End Module
]]>)

        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Insert blank line at start of method.
        Dim str = "Iterator"
        Dim position = oldText.ToString().IndexOf(str, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=str.Length, newText:="")
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub NonIteratorToIteratorMethodDecl()
        Dim source = ToText(<![CDATA[
Module Program
    Function Goo(Yield As Integer) As IEnumerable
        Yield (1)
    End Function
End Module
]]>)

        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Insert blank line at start of method.
        Dim str = "Function "
        Dim position = oldText.ToString().IndexOf(str, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=str.Length, newText:="Iterator Function ")
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

#End Region

    <WorkItem(554442, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554442")>
    <Fact>
    Public Sub SplitCommentAtPreprocessorSymbol()
        Dim source = ToText(<![CDATA[
Module M
    Function F()
        ' comment # 1 and # 2
        ' comment
        ' comment
        Return Nothing
    End Function
End Module
]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Split comment at "#".
        Dim position = oldText.ToString().IndexOf("#"c)
        Dim newText = oldText.Replace(start:=position, length:=0, newText:=vbCrLf)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <WorkItem(586698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/586698")>
    <Fact>
    Public Sub SortUsings()
        Dim oldSource = ToText(<![CDATA[
Imports System.Linq
Imports System
Imports Microsoft.VisualBasic
Module Module1
    Sub Main()
    End Sub
End Module
]]>)
        Dim newSource = ToText(<![CDATA[
Imports System
Imports System.Linq
Imports Microsoft.VisualBasic
Module Module1
    Sub Main()
    End Sub
End Module
]]>)
        Dim oldText = SourceText.From(oldSource)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Changes:
        ' 1. "" => "System\r\nImports "
        ' 2. "System\r\nImports " => ""
        Dim newText = oldText.WithChanges(
            New TextChange(TextSpan.FromBounds(8, 8), "System" + vbCrLf + "Imports "),
            New TextChange(TextSpan.FromBounds(29, 45), ""))
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub Reformat()
        Dim oldSource = ToText(<![CDATA[
Class C
    Sub Method()
                                Dim i = 1
Select          Case            i               
                                            Case            1           ,           2           ,       3           
                                                    End             Select              
    End Sub
End Class
]]>)
        Dim newSource = ToText(<![CDATA[
Class C
    Sub Method()
        Dim i = 1
        Select Case i
            Case 1, 2, 3
        End Select
    End Sub
End Class
]]>)
        Dim oldText = SourceText.From(oldSource)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        Dim startOfNew = newSource.IndexOf("Dim", StringComparison.Ordinal)
        Dim endOfNew = newSource.LastIndexOf("Select", StringComparison.Ordinal) + 6
        Dim startOfOld = startOfNew
        Dim endOfOld = oldSource.Length - newSource.Length + endOfNew
        Dim newText = oldText.Replace(TextSpan.FromBounds(startOfOld, endOfOld), newSource.Substring(startOfNew, endOfNew - startOfNew + 1))
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <WorkItem(604044, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604044")>
    <Fact>
    Public Sub BunchALabels()
        Dim source = ToText(<![CDATA[
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
    End Sub
End Module

]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Add enter after &HFFFFFFFFFF:.
        Dim position = oldText.ToString().IndexOf("&HFFFFFFFFFF:", StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position + "&HFFFFFFFFFF:".Length, length:=0, newText:=vbCrLf)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <WorkItem(625612, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/625612")>
    <Fact()>
    Public Sub LabelAfterColon()
        ' Label following another label on separate lines.
        LabelAfterColonCore(True, <![CDATA[Module M
            Sub M()
        10:
        20:
        30:
        40:
        50:
        60:
        70:
            End Sub
        End Module
        ]]>.Value)
        ' Label following another label on separate lines.
        LabelAfterColonCore(True, <![CDATA[Module M
            Sub M()
        10: : 
        20:
        30:
        40:
        50:
        60:
        70:
            End Sub
        End Module
        ]]>.Value)
        ' Label following on the same line as another label.
        LabelAfterColonCore(False, <![CDATA[Module M
    Sub M()
10: 20:
30:
40:
50:
60:
70:
    End Sub
End Module
]]>.Value)
        ' Label following on the same line as another label.
        LabelAfterColonCore(False, <![CDATA[Module M
    Sub M()
10: : 20:
30:
40:
50:
60:
70:
    End Sub
End Module
]]>.Value)
        ' Label following on the same line as another statement.
        LabelAfterColonCore(False, <![CDATA[Module M
    Sub M()
M() : 20:
30:
40:
50:
60:
70:
    End Sub
End Module
]]>.Value)
        ' Label following a colon within a single-line statement.
        LabelAfterColonCore(False, <![CDATA[Module M
    Sub M()
If True Then M() : 20:
30:
40:
50:
60:
70:
    End Sub
End Module
]]>.Value)
    End Sub

    Private Sub LabelAfterColonCore(valid As Boolean, code As String)
        Dim oldText = SourceText.From(code)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        Dim diagnostics = oldTree.GetDiagnostics()
        Assert.Equal(valid, diagnostics.Count = 0)
        ' Replace "70".
        Dim position = code.IndexOf("70", StringComparison.Ordinal)
        Dim change = New TextChange(New TextSpan(position, 2), "71")
        Dim newText = oldText.WithChanges(change)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <WorkItem(529260, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529260")>
    <Fact()>
    Public Sub DoNotReuseAnnotatedNodes()
        Dim text As String = <![CDATA[
Class C
End Class
Class D
End Class
]]>.Value.Replace(vbCr, vbCrLf)

        ' NOTE: We're using the class statement, rather than the block, because the
        ' change region is expanded enough to impinge on the block.
        Dim extractGreenClassC As Func(Of SyntaxTree, Syntax.InternalSyntax.VisualBasicSyntaxNode) =
            Function(tree) DirectCast(tree.GetRoot().DescendantNodes().First(Function(n) n.IsKind(SyntaxKind.ClassStatement)), VisualBasicSyntaxNode).VbGreen

        ''''''''''
        ' Check reuse after a trivial change in an unannotated tree.
        ''''''''''
        Dim oldTree1 = VisualBasicSyntaxTree.ParseText(text)
        Dim newTree1 = oldTree1.WithInsertAt(text.Length, " ")

        ' Class declaration is reused.
        Assert.Same(extractGreenClassC(oldTree1), extractGreenClassC(newTree1))

        ''''''''''
        ' Check reuse after a trivial change in an annotated tree.
        ''''''''''
        Dim tempTree2 = VisualBasicSyntaxTree.ParseText(text)
        Dim tempRoot2 = tempTree2.GetRoot()
        Dim tempToken2 = tempRoot2.DescendantTokens().First(Function(t) t.Kind = SyntaxKind.IdentifierToken)
        Dim oldRoot2 = tempRoot2.ReplaceToken(tempToken2, tempToken2.WithAdditionalAnnotations(New SyntaxAnnotation()))
        Assert.True(oldRoot2.ContainsAnnotations, "Should contain annotations.")
        Assert.Equal(text, oldRoot2.ToFullString())

        Dim oldTree2 = VisualBasicSyntaxTree.Create(DirectCast(oldRoot2, VisualBasicSyntaxNode), DirectCast(tempTree2.Options, VisualBasicParseOptions), tempTree2.FilePath, Encoding.UTF8)
        Dim newTree2 = oldTree2.WithInsertAt(text.Length, " ")

        Dim oldClassC2 = extractGreenClassC(oldTree2)
        Dim newClassC2 = extractGreenClassC(newTree2)

        Assert.True(oldClassC2.ContainsAnnotations, "Should contain annotations")
        Assert.False(newClassC2.ContainsAnnotations, "Annotations should have been removed.")

        ' Class declaration is not reused...
        Assert.NotSame(oldClassC2, newClassC2)
        ' ...even though the text is the same.
        Assert.Equal(oldClassC2.ToFullString(), newClassC2.ToFullString())

        Dim oldToken2 = DirectCast(oldClassC2, Syntax.InternalSyntax.ClassStatementSyntax).Identifier
        Dim newToken2 = DirectCast(newClassC2, Syntax.InternalSyntax.ClassStatementSyntax).Identifier

        Assert.True(oldToken2.ContainsAnnotations, "Should contain annotations")
        Assert.False(newToken2.ContainsAnnotations, "Annotations should have been removed.")

        ' Token is not reused...
        Assert.NotSame(oldToken2, newToken2)
        ' ...even though the text is the same.
        Assert.Equal(oldToken2.ToFullString(), newToken2.ToFullString())

    End Sub

    <Fact>
    Public Sub IncrementalParsing_NamespaceBlock_TryLinkSyntaxMethodsBlock()
        'Sub Block
        'Function Block
        'Property Block
        Dim source = ToText(<![CDATA[
Namespace N
    Module M
        Function F() as Boolean
            Return True
        End Function    
        Sub M()
        End Sub
        Function Fn() as Boolean
            Return True
        End Function
        Property as Integer = 1
    End Module
End Namespace

]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim TextToRemove As String = "Module M"
        Dim TextToAdd As String = ""
        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IncrementalParsing_ExecutableStatementBlock_TryLinkSyntaxClass()
        'Class Block         
        Dim source = ToText(<![CDATA[
Module M        
    Sub M()
    End Sub
    Dim x = Nothing

    Public Class C1        
    End Class

    Class C2        
    End Class
End Module
]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim TextToRemove As String = "End Sub"
        Dim TextToAdd As String = ""
        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IncrementalParsing_ExecutableStatementBlock_TryLinkSyntaxStructure()
        'Structure Block
        Dim source = ToText(<![CDATA[
Module M        
    Sub M()
    End Sub
    Dim x = Nothing

     Structure S2       1
        Dim i as integer
    End Structure

    Public Structure S2        
        Dim i as integer
    End Structure
End Module
]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim TextToRemove As String = "End Sub"
        Dim TextToAdd As String = ""
        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact()>
    Public Sub IncrementalParsing_ExecutableStatementBlock_TryLinkSyntaxOptionStatement()
        'Option Statement
        Dim source = ToText(<![CDATA[
Option Strict Off
Option Infer On
Option Explicit On

]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim TextToRemove As String = "End Sub"
        Dim TextToAdd As String = "Module Module1" & Environment.NewLine & "Sub Goo()" & Environment.NewLine
        Dim position = 0
        Dim newText = oldText.Replace(start:=position, length:=1, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact()>
    Public Sub IncrementalParsing_ExecutableStatementBlock_TryLinkSyntaxImports()
        'Imports Statement
        Dim source = ToText(<![CDATA[
Imports System
Imports System.Collections
Imports Microsoft.Visualbasic
]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim TextToRemove As String = "End Sub"
        Dim TextToAdd As String = "Module Module1" & Environment.NewLine & "Sub Goo()" & Environment.NewLine
        Dim position = 0
        Dim newText = oldText.Replace(start:=position, length:=1, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IncrementalParsing_ExecutableStatementBlock_TryLinkSyntaxDelegateSub()
        'ExecutableStatementBlock -> DelegateSub
        Dim source = ToText(<![CDATA[
Module Module1
    Sub Main()

    End Sub
    Dim x = Nothing
    Delegate Sub Goo()
    Public Delegate Sub GooWithModifier()
End Module

]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim TextToRemove As String = "End Sub"
        Dim TextToAdd As String = ""
        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IncrementalParsing_DeclarationContextBlock_TryLinkSyntaxOperatorBlock()
        Dim source = ToText(<![CDATA[
Module Module1
    Sub Main()
        Dim x As New SomeClass
        Dim y As Boolean = -x
    End Sub

    Class SomeClass
        Dim member As Long = 2
        Public Overloads Shared Operator -(ByVal value As SomeClass) As Boolean           
            If value.member Mod 5 = 0 Then                                
                Return True
            End If
            Return False
        End Operator
    End Class
End Module

]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim TextToRemove As String = "Class SomeClass"
        Dim TextToAdd As String = ""

        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IncrementalParsing_DeclarationContextBlock_TryLinkSyntaxEventBlock()
        Dim source = ToText(<![CDATA[
Module Module1
    Sub Main()
        
    End Sub

    Class SomeClass
        Dim member As Long = 2
        Public Custom Event AnyName As EventHandler
            AddHandler(ByVal value As EventHandler)
            End AddHandler
            RemoveHandler(ByVal value As EventHandler)
            End RemoveHandler
            RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
            End RaiseEvent
        End Event
    End Class
End Module

]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim TextToRemove As String = "Class SomeClass"
        Dim TextToAdd As String = ""

        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IncrementalParsing_DeclarationContextBlock_TryLinkSyntaxPropertyBlock()
        Dim source = ToText(<![CDATA[
Module Module1
    Sub Main()        
    End Sub
    Class SomeClass
        Dim member As Long = 2
        Public Property abc As Integer
            Set(value As Integer)

            End Set
            Get

            End Get
        End Property
    End Class
End Module
]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        Dim TextToRemove As String = "Class SomeClass"
        Dim TextToAdd As String = ""

        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IncrementalParsing_DeclarationContextBlock_TryLinkSyntaxNamespaceModuleBlock()
        Dim source = ToText(<![CDATA[

Namespace NS1
    Module Module1
        Sub Goo()

        End Sub
        Dim x
    End Module 'Remove
    Namespace vs

    End Namespace
    Module Module1
    
        Dim x
    End Module
End Namespace
]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        Dim TextToRemove As String = "End Module 'Remove"
        Dim TextToAdd As String = ""

        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IncrementalParsing_DeclarationContextBlock_TryLinkSyntaxNamespaceNamespaceBlock()
        Dim source = ToText(<![CDATA[

Namespace NS1
    Module Module1
        Sub Goo()

        End Sub
        Dim x
    End Module 'Remove
    Namespace vs
        Namespace vs2

        End Namespace
    End Namespace
    Namespace vs3

    End Namespace
End Namespace
]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        Dim TextToRemove As String = "End Module 'Remove"
        Dim TextToAdd As String = ""

        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IncrementalParsing_DeclarationContextBlock_TryLinkSyntaxItems()
        Dim source = ToText(<![CDATA[
    Class SomeClass
        Dim member As Long = 2
        Sub abc() 'Remove
            Dim xyz as integer = 2
            IF member = 1 then Console.writeline("TEST");
            Dim SingleLineDeclare as integer = 2
        End Sub

        Dim member2 As Long = 2
        Dim member3 As Long = 2

        Enum EnumItem 
                Item1
        End Enum
    End Class
]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        Dim TextToRemove As String = "Sub abc() 'Remove"
        Dim TextToAdd As String = ""

        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IncrementalParsing_PropertyContextBlock_TryLinkSyntaxSetAccessor()
        'PropertyContextBlock -> SetAccessor 
        Dim source = ToText(<![CDATA[
Class C
    Private _p As Integer = 0

    Property p2 As Integer
        Set(value As Integer)

        End Set
        Get

        End Get
    End Property

    Private _d As Integer = 1
End Class

]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim TextToRemove As String = "End Property"
        Dim TextToAdd As String = ""

        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IncrementalParsing_BlockContext_TryLinkSyntaxSelectBlock()
        Dim source = ToText(<![CDATA[
Module Module1
    Private _p As Integer = 0
    Sub Bar()

    End Sub

    Function Goo(i As Integer) As Integer
        Dim y As Integer = i
        Select Case y
            Case 1
            Case 2, 3
            Case Else
        End Select

        Return y + 1

        Try
        Catch ex As exception
        End Try

        If y = 1 Then Console.WriteLine("Test")
        Y = 1

        While y <= 10
            y = y + 1
        End While

        Using f As New Goo
        End Using

        Dim Obj_C As New OtherClass
        With Obj_C

        End With

        SyncLock Obj_C

        End SyncLock

        Select Case y
            Case 10
            Case Else
        End Select

        y = 0
        Do
            y = y + 1
            If y >= 3 Then Exit Do
        Loop

        y = 0
        Do While y < 4
            y = y + 1
        Loop

        y = 0
        Do Until y > 5
            y = y + 1
        Loop
    End Function
End Module

Class Goo
    Implements IDisposable

#Region "IDisposable Support"
    Private disposedValue As Boolean ' To detect redundant calls
    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
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

Class OtherClass

End Class
]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim TextToRemove As String = "End Select"
        Dim TextToAdd As String = ""

        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IncrementalParsing_EventBlockContext_TryLinkSyntax1()
        Dim source = ToText(<![CDATA[
Module Module1
    Public Custom Event AnyName As EventHandler
        AddHandler(ByVal value As EventHandler)
            _P = 1
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
            _P = 2
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
            _P = 3
        End RaiseEvent
    End Event
    Private _p As Integer = 0
    Sub Main()
    End Sub
End Module
]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim TextToRemove As String = "RemoveHandler(ByVal value As EventHandler)"
        Dim TextToAdd As String = ""

        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IncrementalParsing_EventBlockContext_TryLinkSyntax2()
        Dim source = ToText(<![CDATA[
Module Module1
    Public Custom Event AnyName As EventHandler
        AddHandler(ByVal value As EventHandler)
            _P = 1
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
            _P = 2          
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
            _P = 3
            _P = _P +1
        End RaiseEvent
    End Event
    Private _p As Integer = 0
    Sub Main()
    End Sub
End Module

]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim TextToRemove As String = "_P = 3"
        Dim TextToAdd As String = ""

        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IncrementalParsing_InterfaceBlockContext_TryLinkSyntaxClass()
        Dim source = ToText(<![CDATA[
Module Module1
    Sub Main()
    End Sub
    Interface IGoo
    End Interface
    Dim _p
    Class C
    End Class
End Module

]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim TextToRemove As String = "End Interface"
        Dim TextToAdd As String = ""

        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IncrementalParsing_InterfaceBlockContext_TryLinkSyntaxEnum()
        Dim source = ToText(<![CDATA[
Module Module1
    Sub Main()

    End Sub
    Interface IGoo

    End Interface

    Dim _p

    Public Enum TestEnum
        Item
    End Enum
End Module

]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim TextToRemove As String = "End Interface"
        Dim TextToAdd As String = ""

        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IncrementalParsing_CaseBlockContext_TryLinkSyntaxCase()
        Dim source = ToText(<![CDATA[
Module Module1
    Sub Goo()
        Dim i As Integer
        Dim y As Integer
        Select Case i
            Case 1
                _p = 1
            Case 2, 3
                _p = 2
            Case Else
                _p = 3                
        End Select
    End Sub
    Private _p As Integer = 0
    Sub Main()

    End Sub
End Module
]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim TextToRemove As String = "Case 2, 3"
        Dim TextToAdd As String = ""
        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IncrementalParsing_CatchContext_TryLinkSyntaxCatch()
        Dim source = ToText(<![CDATA[
Module Module1
    Sub Goo()
        Dim x1 As Integer = 1
        Try
            x1 = 2
        Catch ex As NullReferenceException
            Dim z = 1
        Catch ex As ArgumentException 'Remove
            Dim z = 1
        Catch ex As Exception
            _p = 3
            Dim s = Bar()
        Finally
            _p = 4
        End Try
    End Sub
    Private _p As Integer = 0
    Function Bar() As String
    End Function
End Module

]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim TextToRemove As String = "Catch ex As ArgumentException 'Remove"
        Dim TextToAdd As String = ""

        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact()>
    Public Sub IncrementalParsing_NamespaceBlockContext_TryLinkSyntaxModule()
        Dim source = ToText(<![CDATA[
Namespace NS1
Module ModuleTemp 'Remove
End Module
Module Module1 'Remove
    Sub Goo()
        Dim x1 As Integer = 1                
    End Sub
    Private _p As Integer = 0
    Function Bar() As String
    End Function
End Module
End Namespace
]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim TextToRemove As String = "Module ModuleTemp 'Remove"
        Dim TextToAdd As String = ""

        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <Fact>
    Public Sub IncrementalParsing_IfBlockContext_TryLinkSyntax()
        Dim source = ToText(<![CDATA[

Module Module1
    Private _p As Integer = 0

    Sub Goo()
        If x = 1 Then                
            _p=1
        elseIf x = 2 Then                
            _p=2
        elseIf x = 3 Then                
            _p=3
        else 
            If y = 1 Then            
                _p=2
            End If 
        End If 
    End Sub
End Module
]]>)
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)

        Dim TextToRemove As String = "elseIf x = 2 Then"
        Dim TextToAdd As String = ""

        Dim position = oldText.ToString.IndexOf(TextToRemove, StringComparison.Ordinal)
        Dim newText = oldText.Replace(start:=position, length:=TextToRemove.Length, newText:=TextToAdd)
        Dim newTree = oldTree.WithChangedText(newText)
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <WorkItem(719787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/719787")>
    <Fact()>
    Public Sub Bug719787_EOF()
        Dim source = <![CDATA[
Namespace N
    Class C
        Property P As Integer
    End Class
    Structure S
        Private F As Object
    End Structure
End Namespace
]]>.Value.Trim()
        ' Add two line breaks at end.
        source += vbCrLf
        Dim position = source.Length
        source += vbCrLf
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        ' Add "Delegate" to end of file between line breaks.
        Dim newText = oldText.Replace(start:=position, length:=0, newText:="Delegate")
        Dim newTree = oldTree.WithChangedText(newText)
        Dim diffs = SyntaxDifferences.GetRebuiltNodes(oldTree, newTree)
        ' Most of the Namespace should have been reused.
        Assert.False(diffs.Any(Function(n) n.IsKind(SyntaxKind.StructureStatement)))
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

    <WorkItem(719787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/719787")>
    <Fact>
    Public Sub Bug719787_MultiLineIf()
        Dim source = <![CDATA[
Class C
    Sub M()
        If e1 Then
            If e2 Then
                M11()
            Else
                M12()
            End If
        ElseIf e2 Then 
            If e2 Then
                M21()
            Else
                M22()
            End If
        ElseIf e3 Then 
            If e2 Then
                M31()
            Else
                M32()
            End If
        ElseIf e4 Then 
            If e2 Then
                M41()
            Else
                M42()
            End If
        Else 
            If e2 Then
                M51()
            Else
                M52()
            End If
        End If
    End Sub
    ' Comment
End Class
]]>.Value.Trim()
        Dim oldText = SourceText.From(source)
        Dim oldTree = VisualBasicSyntaxTree.ParseText(oldText)
        Dim toReplace = "' Comment"
        Dim position = source.IndexOf(toReplace, StringComparison.Ordinal)
        ' Replace "' Comment" with "Property"
        Dim newText = oldText.Replace(start:=position, length:=toReplace.Length, newText:="Property")
        Dim newTree = oldTree.WithChangedText(newText)
        Dim diffs = SyntaxDifferences.GetRebuiltNodes(oldTree, newTree)
        ' The ElseIfBlocks should have been reused.
        Assert.False(diffs.Any(Function(n) n.IsKind(SyntaxKind.ElseIfBlock)))
        VerifyEquivalent(newTree, VisualBasicSyntaxTree.ParseText(newText))
    End Sub

#Region "Helpers"

    Private Shared Function GetTokens(root As SyntaxNode) As InternalSyntax.VisualBasicSyntaxNode()
        Return root.DescendantTokens().Select(Function(t) DirectCast(t.Node, InternalSyntax.VisualBasicSyntaxNode)).ToArray()
    End Function

    Private Shared Sub VerifyTokensEquivalent(rootA As SyntaxNode, rootB As SyntaxNode)
        Dim tokensA = GetTokens(rootA)
        Dim tokensB = GetTokens(rootB)
        Assert.Equal(tokensA.Count, tokensB.Count)

        For i = 0 To tokensA.Count - 1
            Dim tokenA = tokensA(i)
            Dim tokenB = tokensB(i)
            Assert.Equal(tokenA.Kind, tokenB.Kind)
            Assert.Equal(tokenA.ToFullString(), tokenB.ToFullString())
        Next
    End Sub

    Private Shared Sub VerifyEquivalent(treeA As SyntaxTree, treeB As SyntaxTree)
        Dim rootA = treeA.GetRoot()
        Dim rootB = treeB.GetRoot()

        VerifyTokensEquivalent(rootA, rootB)

        Dim diagnosticsA = treeA.GetDiagnostics()
        Dim diagnosticsB = treeB.GetDiagnostics()

        Assert.True(rootA.IsEquivalentTo(rootB))
        Assert.Equal(diagnosticsA.Count, diagnosticsB.Count)

        For i = 0 To diagnosticsA.Count - 1
            Assert.Equal(diagnosticsA(i).Inspect(), diagnosticsB(i).Inspect())
        Next
    End Sub

    Private Shared Function ToText(code As XCData) As String
        Dim str = code.Value.Trim()
        ' Normalize line terminators.
        Dim builder = ArrayBuilder(Of Char).GetInstance()
        For i = 0 To str.Length - 1
            Dim c = str(i)
            If (c = vbLf(0)) AndAlso
                ((i = str.Length - 1) OrElse (str(i + 1) <> vbCr(0))) Then
                builder.AddRange(vbCrLf)
            Else
                builder.Add(c)
            End If
        Next
        Return New String(builder.ToArrayAndFree())
    End Function
#End Region

End Class
