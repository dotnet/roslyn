' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Wrapping.BinaryExpression
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Wrapping.BinaryExpression
    Friend Class VisualBasicBinaryExpressionWrapper
        Inherits AbstractBinaryExpressionWrapper(Of BinaryExpressionSyntax)

        Public Sub New()
            MyBase.New(
                supportsOperatorWrapping:=False,
                VisualBasicSyntaxFactsService.Instance)
        End Sub
    End Class
End Namespace
