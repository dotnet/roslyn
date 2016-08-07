' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.ObjectModel
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities

Public Class IPEndBlockStatements

    ''' <summary>
    ''' Test1a - Inserts End Function at various places in code
    ''' </summary>
    <WorkItem(899390, "DevDiv/Personal")>
    <Fact>
    Public Sub InsertEndFunctionAtFileEnd()
        '===================================================================================================================
        'Scenario1: At the end of file
        '===================================================================================================================

        Dim code As String =
"Public Sub Foo()
Dim i = 1
"
        Dim change As String = "End Function"

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.Length, 0),
        .changeType = ChangeType.Insert})
    End Sub

    <Fact>
    Public Sub InsertEndFunctionAtFileMid()
        '===================================================================================================================
        'Scenario2: In the middle of the file
        '===================================================================================================================
        Dim code =
"Namespace n1
Public Module m1
Public Sub Foo()
Dim i = 1
End Module
End Namespace
"
        Dim change =
"End Function
"

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf(" 1", StringComparison.Ordinal) + 4, 0),
        .changeType = ChangeType.Insert})
    End Sub

    <Fact>
    Public Sub InsertEndFunctionForStatLambda()

        '===================================================================================================================
        'Scenario3: For a statement lambda
        '===================================================================================================================
        Dim code =
"Namespace n1
Public Module m1
Public Sub Foo()
Dim i = Sub()
End Sub
End Module
End Namespace
"
        Dim change =
"End Function
"

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf(" Sub()", StringComparison.Ordinal) + 8, 0),
        .changeType = ChangeType.Insert})
    End Sub

    ''' <summary>
    ''' Test1b - Removes End Function at various places in code
    ''' </summary>
    <WorkItem(899392, "DevDiv/Personal")>
    <Fact>
    Public Sub RemoveEndFunctionFileEnd()
        '===================================================================================================================
        'Scenario1: At the end of file
        '===================================================================================================================

        Dim code As String =
"Public Sub Foo()
Dim i = 1
End Function
"
        Dim change As String = "End Function"

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf("End Function", StringComparison.Ordinal), change.Length),
        .changeType = ChangeType.Remove})

    End Sub

    <Fact>
    Public Sub RemoveEndFunctionFileMid()
        '===================================================================================================================
        'Scenario2: In the middle of the file
        '===================================================================================================================
        Dim code =
"Namespace n1
Public Module m1
Public Sub Foo()
Dim i = 1
End Function
End Module
End Namespace
"
        Dim change = "End Function" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf(" 1", StringComparison.Ordinal) + 4, change.Length),
        .changeType = ChangeType.Remove})

    End Sub

    <Fact>
    Public Sub RemoveEndFunctionStatLambda()
        '===================================================================================================================
        'Scenario3: For a statement lambda
        '===================================================================================================================
        Dim code =
"Namespace n1
Public Module m1
Public Sub Foo()
Dim i = Sub()
End Function
End Sub
End Module
End Namespace
"
        Dim change = "End Function" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf(" Sub()", StringComparison.Ordinal) + 8, change.Length),
        .changeType = ChangeType.Remove})
    End Sub


    ''' <summary>
    ''' Test2b - Removes End Sub at various places in code
    ''' </summary>
    <Fact>
    Public Sub InsertEndSubFileEnd()
        '===================================================================================================================
        'Scenario1: At the end of file
        '===================================================================================================================

        Dim code As String =
"Public Function Foo()
Dim i = 1
"
        Dim change As String = "End Sub"

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.Length, 0),
        .changeType = ChangeType.Insert})

    End Sub

    <Fact>
    Public Sub InsertEndSubFileMid()
        '===================================================================================================================
        'Scenario2: In the middle of the file
        '===================================================================================================================
        Dim code =
"Namespace n1
Public Module m1
Public Function Foo()
Dim i = 1
End Module
End Namespace
"
        Dim change = "End Sub" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf(" 1", StringComparison.Ordinal) + 4, 0),
        .changeType = ChangeType.Insert})

    End Sub

    <Fact>
    Public Sub InsertEndSubStatLambda()

        '===================================================================================================================
        'Scenario3: For a statement lambda
        '===================================================================================================================
        Dim code =
"Namespace n1
Public Module m1
Public Sub Foo()
Dim i = Function() 
End Sub
End Module
End Namespace
"
        Dim change = "End Sub" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf(" Function()", StringComparison.Ordinal) + 13, 0),
        .changeType = ChangeType.Insert})
    End Sub

    ''' <summary>
    ''' Test2b - Removes End Sub at various places in code
    ''' </summary>
    <Fact>
    Public Sub RemoveEndSubFileEnd()
        '===================================================================================================================
        'Scenario1: At the end of file
        '===================================================================================================================

        Dim code As String =
"Public Function Foo()
Dim i = 1
End Sub
"
        Dim change As String = "End Sub"

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf("End Sub", StringComparison.Ordinal), change.Length),
        .changeType = ChangeType.Remove})

    End Sub

    ''' <summary>
    ''' Test2b - Removes End Sub at various places in code
    ''' </summary>
    <Fact>
    Public Sub RemoveEndSubFileMid()
        '===================================================================================================================
        'Scenario2: In the middle of the file
        '===================================================================================================================
        Dim code =
"Namespace n1
Public Module m1
Public Function Foo()
Dim i = 1
End Sub
End Module
End Namespace
"
        Dim change = "End Sub" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf(" 1", StringComparison.Ordinal) + 4, change.Length),
        .changeType = ChangeType.Remove})

    End Sub

    ''' <summary>
    ''' Test2b - Removes End Sub at various places in code
    ''' </summary>
    <Fact>
    Public Sub RemoveEndSubFileStatLambda()
        '===================================================================================================================
        'Scenario3: For a statement lambda
        '===================================================================================================================
        Dim code =
"Namespace n1
Public Module m1
Public Sub Foo()
Dim i = Function()
End Sub
End Sub
End Module
End Namespace
"
        Dim change = "End Sub" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf("= Function()", StringComparison.Ordinal) + 14, change.Length),
        .changeType = ChangeType.Remove})
    End Sub

    ''' <summary>
    ''' Test3a - Inserts End If at various places in code
    ''' </summary>
    <Fact>
    Public Sub InsertEndIfFileEnd()
        '===================================================================================================================
        'Scenario1: At the end of file
        '===================================================================================================================

        Dim code As String =
"Namespace n1
Public Module m1
Public Function Foo()
While True
"
        Dim change = "End If" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.Length, 0),
        .changeType = ChangeType.Insert})
    End Sub

    ''' <summary>
    ''' Test3a - Inserts End If at various places in code
    ''' </summary>
    <Fact>
    Public Sub InsertEndIfFileMid()
        '===================================================================================================================
        'Scenario2: In the middle of the file
        '===================================================================================================================
        Dim code =
"Namespace n1
Public Module m1
Public Function Foo()
While True
End Function
End Module
End Namespace
"
        Dim change = "End If" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf(" True", StringComparison.Ordinal) + 7, 0),
        .changeType = ChangeType.Insert})
    End Sub

    ''' <summary>
    ''' Test3a - Inserts End If at various places in code
    ''' </summary>
    <Fact>
    Public Sub InsertEndIfStatLambda()
        '===================================================================================================================
        'Scenario3: For a statement lambda
        '===================================================================================================================
        Dim code =
"Namespace n1
Public Module m1
Public Sub Foo()
If True Then
End Sub
End Module
End Namespace
"
        Dim change = "End Sub" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf(" Then", StringComparison.Ordinal) + 7, 0),
        .changeType = ChangeType.Insert})
    End Sub

    ''' <summary>
    ''' Test3b - Removes End IF at various places in code
    ''' </summary>
    <Fact>
    Public Sub RemoveEndIfFileEnd()
        '===================================================================================================================
        'Scenario1: At the end of file
        '===================================================================================================================

        Dim code As String =
"Namespace n1
Public Module m1
Public Function Foo()
While True
End If
"
        Dim change = "End If" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf("End If", StringComparison.Ordinal), change.Length),
        .changeType = ChangeType.Remove})
    End Sub

    ''' <summary>
    ''' Test3b - Removes End IF at various places in code
    ''' </summary>
    <Fact>
    Public Sub RemoveEndIfFileMid()

        '===================================================================================================================
        'Scenario2: In the middle of the file
        '===================================================================================================================
        Dim code =
"Namespace n1
Public Module m1
Public Function Foo()
While True
End If
End Sub
End Module
End Namespace
"
        Dim change = "End If" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf(" True", StringComparison.Ordinal) + 7, change.Length),
        .changeType = ChangeType.Remove})
    End Sub

    ''' <summary>
    ''' Test3b - Removes End IF at various places in code
    ''' </summary>
    <Fact>
    Public Sub RemoveEndIfStatLambda()

        '===================================================================================================================
        'Scenario3: For a statement lambda
        '===================================================================================================================
        Dim code =
"Namespace n1
Public Module m1
Public Sub Foo()
If True Then
End If
End Sub
End Module
End Namespace
"
        Dim change = "End If" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf(" Then", StringComparison.Ordinal) + 7, change.Length),
        .changeType = ChangeType.Remove})
    End Sub

    ''' <summary>
    ''' Test4a - Inserts End Select at various places in code
    ''' </summary>
    <Fact>
    Public Sub InsertEndSelectFileEnd()
        '===================================================================================================================
        'Scenario1: At the end of file
        '===================================================================================================================

        Dim code As String =
"Namespace n1
Public Module m1
Public Function Foo()
Dim i = From el In {1, 2} 
Select el 
"
        Dim change = "End Select" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.Length, 0),
        .changeType = ChangeType.Insert})
    End Sub

    ''' <summary>
    ''' Test4a - Inserts End Select at various places in code
    ''' </summary>
    <Fact>
    Public Sub InsertEndSelectFileMid()

        '===================================================================================================================
        'Scenario2: In the middle of the file
        '===================================================================================================================
        Dim code =
"Namespace n1
Public Module m1
Public Function Foo()
Dim i = From el in {1,2} 
Select el 
End Function
End Module
End Namespace
"
        Dim change = "End Select" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf("Select el ", StringComparison.Ordinal) + 12, 0),
        .changeType = ChangeType.Insert})

    End Sub

    ''' <summary>
    ''' Test4b - Removes End Select at various places in code
    ''' </summary>
    <Fact>
    Public Sub RemoveEndSelectFileEnd()
        '===================================================================================================================
        'Scenario1: At the end of file
        '===================================================================================================================

        Dim code As String =
"Namespace n1
Public Module m1
Public Function Foo()
Dim i = From el in {1,2} 
Select el 
End Select
"
        Dim change = "End Select" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf("End Select", StringComparison.Ordinal), change.Length),
        .changeType = ChangeType.Remove})

    End Sub

    ''' <summary>
    ''' Test4b - Removes End Select at various places in code
    ''' </summary>
    <Fact>
    Public Sub RemoveEndSelectFileMid()

        '===================================================================================================================
        'Scenario2: In the middle of the file
        '===================================================================================================================
        Dim code =
"Namespace n1
Public Module m1
Public Function Foo()
Dim i = From el In {1, 2} 
Select el 
End Select
End Function
End Module
End Namespace
"
        Dim change = "End Select" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf("Select el ", StringComparison.Ordinal) + 12, change.Length),
        .changeType = ChangeType.Remove})

    End Sub

    ''' <summary>
    ''' Test5a - Inserts End Using at various places in code
    ''' </summary>
    <Fact>
    Public Sub InsertEndUsingFileEnd()
        '===================================================================================================================
        'Scenario1: At the end of file
        '===================================================================================================================

        Dim code As String =
"Namespace n1
Public Module m1
Public Function Foo()
With New Integer 
"
        Dim change = "End Using" & vbCrLf


        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.Length, 0),
        .changeType = ChangeType.Insert})

    End Sub

    ''' <summary>
    ''' Test5a - Inserts End Using at various places in code
    ''' </summary>
    <Fact>
    Public Sub InsertEndUsingFileMid()

        '===================================================================================================================
        'Scenario2: In the middle of the file
        '===================================================================================================================
        Dim code =
"Namespace n1
Public Module m1
Public Function Foo()
With New Integer 
End Function
End Module
End Namespace
"
        Dim change = "End Using" & vbCrLf


        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf("New Integer ", StringComparison.Ordinal) + 14, 0),
        .changeType = ChangeType.Insert})

    End Sub

    ''' <summary>
    ''' Test5a - Inserts End Using at various places in code
    ''' </summary>
    <Fact>
    Public Sub InsertEndUsingFileMidReplace()

        '===================================================================================================================
        'Scenario3: Replacing In the middle of the file
        '===================================================================================================================
        Dim code =
"Namespace n1
Public Module m1
Public Function Foo()
With New Integer 
End With
End Function
End Module
End Namespace
"
        Dim change = "Using" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf("End With", StringComparison.Ordinal) + 4, 4),
        .changeType = ChangeType.Replace})

    End Sub

    ''' <summary>
    ''' Test5b - Removes End Using at various places in code
    ''' </summary>
    <Fact>
    Public Sub RemoveEndUsingFileEnd()
        '===================================================================================================================
        'Scenario1: At the end of file
        '===================================================================================================================

        Dim code As String =
"Namespace n1
Public Module m1
Public Function Foo()
With New Integer 
"
        Dim change = "End Using" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf("New", StringComparison.Ordinal), change.Length),
        .changeType = ChangeType.Remove})

    End Sub

    ''' <summary>
    ''' Test5b - Removes End Using at various places in code
    ''' </summary>
    <Fact>
    Public Sub RemoveEndUsingFileMid()

        '===================================================================================================================
        'Scenario2: In the middle of the file
        '===================================================================================================================
        Dim code =
"Namespace n1
Public Module m1
Public Function Foo()
Dim i = From el In {1, 2} 
Select el 
End Select
End Function
End Module
End Namespace
"
        Dim change = "End Select" & vbCrLf

        IncParseAndVerify(New IncParseNode With {
        .oldText = code,
        .changeText = change,
        .changeSpan = New TextSpan(code.IndexOf("Select el ", StringComparison.Ordinal) + 12, change.Length),
        .changeType = ChangeType.Remove})

    End Sub
End Class
