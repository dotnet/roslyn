' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Classification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text.Classification

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Classification
    Public MustInherit Class AbstractVisualBasicClassifierTests
        Inherits AbstractClassifierTests

        Protected ReadOnly Property LineContinuation As Tuple(Of String, String)
            Get
                Return ClassificationBuilder.LineContinuation
            End Get
        End Property

        Protected Function [Module](value As String) As Tuple(Of String, String)
            Return ClassificationBuilder.Module(value)
        End Function

        Protected Function VBXmlName(value As String) As Tuple(Of String, String)
            Return ClassificationBuilder.VBXmlName(value)
        End Function

        Protected Function VBXmlText(value As String) As Tuple(Of String, String)
            Return ClassificationBuilder.VBXmlText(value)
        End Function

        Protected Function VBXmlProcessingInstruction(value As String) As Tuple(Of String, String)
            Return ClassificationBuilder.VBXmlProcessingInstruction(value)
        End Function

        Protected Function VBXmlEmbeddedExpression(value As String) As Tuple(Of String, String)
            Return ClassificationBuilder.VBXmlEmbeddedExpression(value)
        End Function

        Protected Function VBXmlDelimiter(value As String) As Tuple(Of String, String)
            Return ClassificationBuilder.VBXmlDelimiter(value)
        End Function

        Protected Function VBXmlComment(value As String) As Tuple(Of String, String)
            Return ClassificationBuilder.VBXmlComment(value)
        End Function

        Protected Function VBXmlCDataSection(value As String) As Tuple(Of String, String)
            Return ClassificationBuilder.VBXmlCDataSection(value)
        End Function

        Protected Function VBXmlAttributeValue(value As String) As Tuple(Of String, String)
            Return ClassificationBuilder.VBXmlAttributeValue(value)
        End Function

        Protected Function VBXmlAttributeQuotes(value As String) As Tuple(Of String, String)
            Return ClassificationBuilder.VBXmlAttributeQuotes(value)
        End Function

        Protected Function VBXmlAttributeName(value As String) As Tuple(Of String, String)
            Return ClassificationBuilder.VBXmlAttributeName(value)
        End Function

        Protected Function VBXmlEntityReference(value As String) As Tuple(Of String, String)
            Return ClassificationBuilder.VBXmlEntityReference(value)
        End Function

        Friend MustOverride Function GetClassificationSpansAsync(code As String, textSpan As TextSpan) As Task(Of IEnumerable(Of ClassifiedSpan))

        Protected Function GetText(tuple As Tuple(Of String, String)) As String
            Return "(" & tuple.Item1 & ", " & tuple.Item2 & ")"
        End Function

        Friend Function GetText(tuple As ClassifiedSpan) As String
            Return "(" & tuple.TextSpan.ToString() & ", " & tuple.ClassificationType & ")"
        End Function

        Protected Function GetText(tuple As Tuple(Of Microsoft.VisualStudio.Text.Span, IClassificationType)) As String
            Return "(" & tuple.Item1.ToString() & ", " & tuple.Item2.Classification & ")"
        End Function

        Protected Function TestAsync(
            code As String,
            allCode As String,
            ParamArray expected As Tuple(Of String, String)()) As Task

            Dim start = allCode.IndexOf(code, StringComparison.Ordinal)
            Dim length = code.Length
            Dim span = New TextSpan(start, length)
            Return TestAsync(code, allCode, span, expected)
        End Function

        Protected Function TestAsync(
                code As String,
                span As TextSpan,
                ParamArray expected As Tuple(Of String, String)()) As Task
            Return TestAsync(code, code, span, expected)
        End Function

        Protected Async Function TestAsync(
            code As String,
            allCode As String,
            span As TextSpan,
            ParamArray expected As Tuple(Of String, String)()) As Task

            Dim actual = (Await GetClassificationSpansAsync(allCode, span)).ToList()
            actual.Sort(Function(t1, t2) t1.TextSpan.Start - t2.TextSpan.Start)

            For i = 0 To Math.Max(expected.Length, actual.Count) - 1
                If i >= expected.Length Then
                    AssertEx.Fail("Unexpected actual classification: " & GetText(actual(i)))
                ElseIf i >= actual.Count Then
                    AssertEx.Fail("Missing classification for: " & GetText(expected(i)))
                End If

                Dim tuple = expected(i)
                Dim classification = actual(i)

                Dim text = allCode.Substring(classification.TextSpan.Start, classification.TextSpan.Length)
                Assert.Equal(tuple.Item1, text)
                Assert.Equal(tuple.Item2, classification.ClassificationType)
            Next
        End Function

        Protected Function Classifications(ParamArray expected As Tuple(Of String, IClassificationType)()) As Tuple(Of String, IClassificationType)()
            Return expected
        End Function

        <DebuggerStepThrough()>
        Protected Function TestAsync(
            code As String,
            ParamArray expected As Tuple(Of String, String)()) As Task

            Return TestAsync(code, code, expected)
        End Function

        <DebuggerStepThrough()>
        Protected Function TestInNamespaceAsync(
            code As String,
            ParamArray expected As Tuple(Of String, String)()) As Task

            Dim allCode = "Namespace N" & vbCrLf & code & vbCrLf & "End Namespace"
            Return TestAsync(code, allCode, expected)
        End Function

        <DebuggerStepThrough()>
        Protected Function TestInClassAsync(
            className As String,
            code As String,
            expected As Tuple(Of String, String)()) As Task

            Dim allCode = "Class " & className & vbCrLf & code & vbCrLf & "End Class"
            Return TestAsync(code, allCode, expected)
        End Function

        <DebuggerStepThrough()>
        Protected Function TestInClassAsync(
            code As String,
            ParamArray expected As Tuple(Of String, String)()) As Task

            Return TestInClassAsync("C", code, expected)
        End Function

        <DebuggerStepThrough()>
        Protected Function TestInMethodAsync(
            className As String,
            methodName As String,
            code As String,
            expected As Tuple(Of String, String)()) As Task

            Dim allCode = "Class " & className & vbCrLf & "    Sub " & methodName & "()" & vbCrLf & "        " &
                code & vbCrLf & "    End Sub" & vbCrLf & "End Class"
            Return TestAsync(code, allCode, expected)
        End Function

        <DebuggerStepThrough()>
        Protected Async Function TestInMethodAsync(
            className As String,
            methodName As String,
            code As String,
            codeToClassify As String,
            ParamArray expected As Tuple(Of String, String)()) As Task

            Dim allCode = "Class " & className & vbCrLf & "    Sub " & methodName & "()" & vbCrLf & "        " &
                code & vbCrLf & "    End Sub" & vbCrLf & "End Class"
            Dim start = allCode.IndexOf(codeToClassify)
            Dim length = codeToClassify.Length
            Await TestAsync(code, allCode, New TextSpan(start, length), expected)
        End Function

        <DebuggerStepThrough()>
        Protected Function TestInMethodAsync(
            methodName As String,
            code As String,
            expected As Tuple(Of String, String)()) As Task

            Return TestInMethodAsync("C", methodName, code, expected)
        End Function

        <DebuggerStepThrough()>
        Protected Function TestInMethodAsync(
            code As String,
            ParamArray expected As Tuple(Of String, String)()) As Task

            Return TestInMethodAsync("C", "M", code, expected)
        End Function

        <DebuggerStepThrough()>
        Protected Function TestInExpressionAsync(
            code As String,
            ParamArray expected As Tuple(Of String, String)()) As Task

            Dim allCode = "Class C" & vbCrLf & "    Sub M()" & vbCrLf & "        dim q = " &
                code & vbCrLf & "    End Sub" & vbCrLf & "End Class"
            Return TestAsync(code, allCode, expected)
        End Function
    End Class
End Namespace