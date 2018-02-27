' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Classification

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Classification
    Public MustInherit Class AbstractVisualBasicClassifierTests
        Inherits AbstractClassifierTests

        Protected Overrides Function DefaultTestAsync(code As String, allCode As String, expected() As FormattedClassification) As Task
            Return TestAsync(code, allCode, parseOptions:=Nothing, expected)
        End Function

        Protected Overrides Function WrapInClass(className As String, code As String) As String
            Return _
$"Class {className}
{code}
End Class"
        End Function

        Protected Overrides Function WrapInExpression(code As String) As String
            Return _
$"Class C
    Sub M()
        dim q = {code}
    End Sub
End Class"
        End Function

        Protected Overrides Function WrapInMethod(className As String, methodName As String, code As String) As String
            Return _
$"Class {className}
    Sub {methodName}()
        {code}
    End Sub
End Class"
        End Function

        Protected Overrides Function WrapInNamespace(code As String) As String
            Return _
$"Namespace N
{code}
End Namespace"
        End Function

        Protected ReadOnly Property LineContinuation As FormattedClassification
            Get
                Return ClassificationBuilder.LineContinuation
            End Get
        End Property

        Protected Function [Module](value As String) As FormattedClassification
            Return ClassificationBuilder.Module(value)
        End Function

        Protected Function VBXmlName(value As String) As FormattedClassification
            Return ClassificationBuilder.VBXmlName(value)
        End Function

        Protected Function VBXmlText(value As String) As FormattedClassification
            Return ClassificationBuilder.VBXmlText(value)
        End Function

        Protected Function VBXmlProcessingInstruction(value As String) As FormattedClassification
            Return ClassificationBuilder.VBXmlProcessingInstruction(value)
        End Function

        Protected Function VBXmlEmbeddedExpression(value As String) As FormattedClassification
            Return ClassificationBuilder.VBXmlEmbeddedExpression(value)
        End Function

        Protected Function VBXmlDelimiter(value As String) As FormattedClassification
            Return ClassificationBuilder.VBXmlDelimiter(value)
        End Function

        Protected Function VBXmlComment(value As String) As FormattedClassification
            Return ClassificationBuilder.VBXmlComment(value)
        End Function

        Protected Function VBXmlCDataSection(value As String) As FormattedClassification
            Return ClassificationBuilder.VBXmlCDataSection(value)
        End Function

        Protected Function VBXmlAttributeValue(value As String) As FormattedClassification
            Return ClassificationBuilder.VBXmlAttributeValue(value)
        End Function

        Protected Function VBXmlAttributeQuotes(value As String) As FormattedClassification
            Return ClassificationBuilder.VBXmlAttributeQuotes(value)
        End Function

        Protected Function VBXmlAttributeName(value As String) As FormattedClassification
            Return ClassificationBuilder.VBXmlAttributeName(value)
        End Function

        Protected Function VBXmlEntityReference(value As String) As FormattedClassification
            Return ClassificationBuilder.VBXmlEntityReference(value)
        End Function

    End Class
End Namespace
