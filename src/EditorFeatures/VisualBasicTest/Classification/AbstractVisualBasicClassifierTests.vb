' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Classification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Classification
    Public MustInherit Class AbstractVisualBasicClassifierTests
        Inherits AbstractClassifierTests

        Protected ReadOnly Property LineContinuation As Tuple(Of String, String)
            Get
                Return ClassificationBuilder.LineContinuation
            End Get
        End Property

        Friend MustOverride Function GetClassificationSpans(code As String, textSpan As TextSpan) As IEnumerable(Of ClassifiedSpan)

        Protected Sub Test(
            code As String,
            allCode As String,
            ParamArray expected As Tuple(Of String, String)())

            Dim start = allCode.IndexOf(code, StringComparison.Ordinal)
            Dim length = code.Length
            Dim span = New TextSpan(start, length)
            Test(code, allCode, span, expected)
        End Sub

        Protected Sub Test(
            code As String,
            span As TextSpan,
            ParamArray expected As Tuple(Of String, String)())
            Test(code, code, span, expected)
        End Sub

        Protected Sub Test(
            code As String,
            allCode As String,
            span As TextSpan,
            ParamArray expected As Tuple(Of String, String)())

            Dim actual = GetClassificationSpans(allCode, span).ToList()

            Validate(allCode, expected, actual)
        End Sub

        <DebuggerStepThrough()>
        Protected Sub Test(
            code As String,
            ParamArray expected As Tuple(Of String, String)())

            Test(code, code, expected)
        End Sub

        <DebuggerStepThrough()>
        Protected Sub TestInNamespace(
            code As String,
            ParamArray expected As Tuple(Of String, String)())

            Dim allCode = "Namespace N" & vbCrLf & code & vbCrLf & "End Namespace"
            Test(code, allCode, expected)
        End Sub

        <DebuggerStepThrough()>
        Protected Sub TestInClass(
            className As String,
            code As String,
            expected As Tuple(Of String, String)())

            Dim allCode = "Class " & className & vbCrLf & code & vbCrLf & "End Class"
            Test(code, allCode, expected)
        End Sub

        <DebuggerStepThrough()>
        Protected Sub TestInClass(
            code As String,
            ParamArray expected As Tuple(Of String, String)())

            TestInClass("C", code, expected)
        End Sub

        <DebuggerStepThrough()>
        Protected Sub TestInMethod(
            className As String,
            methodName As String,
            code As String,
            expected As Tuple(Of String, String)())

            Dim allCode = "Class " & className & vbCrLf & "    Sub " & methodName & "()" & vbCrLf & "        " &
                code & vbCrLf & "    End Sub" & vbCrLf & "End Class"
            Test(code, allCode, expected)
        End Sub

        <DebuggerStepThrough()>
        Protected Sub TestInMethod(
            className As String,
            methodName As String,
            code As String,
            codeToClassify As String,
            ParamArray expected As Tuple(Of String, String)())

            Dim allCode = "Class " & className & vbCrLf & "    Sub " & methodName & "()" & vbCrLf & "        " &
                code & vbCrLf & "    End Sub" & vbCrLf & "End Class"
            Dim start = allCode.IndexOf(codeToClassify)
            Dim length = codeToClassify.Length
            Test(code, allCode, new TextSpan(start, length), expected)
        End Sub

        <DebuggerStepThrough()>
        Protected Sub TestInMethod(
            methodName As String,
            code As String,
            expected As Tuple(Of String, String)())

            TestInMethod("C", methodName, code, expected)
        End Sub

        <DebuggerStepThrough()>
        Protected Sub TestInMethod(
            code As String,
            ParamArray expected As Tuple(Of String, String)())

            TestInMethod("C", "M", code, expected)
        End Sub

        <DebuggerStepThrough()>
        Protected Sub TestInExpression(
            code As String,
            ParamArray expected As Tuple(Of String, String)())

            Dim allCode = "Class C" & vbCrLf & "    Sub M()" & vbCrLf & "        dim q = " &
                code & vbCrLf & "    End Sub" & vbCrLf & "End Class"
            Test(code, allCode, expected)
        End Sub

    End Class
End Namespace
