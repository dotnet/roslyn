﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

    End Class
End Namespace
