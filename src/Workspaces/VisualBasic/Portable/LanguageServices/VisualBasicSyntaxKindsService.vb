' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageServices
    <ExportLanguageService(GetType(ISyntaxKindsService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicSyntaxKindsService
        Implements ISyntaxKindsService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public ReadOnly Property IfKeyword As Integer = SyntaxKind.IfKeyword Implements ISyntaxKindsService.IfKeyword
        Public ReadOnly Property LogicalAndExpression As Integer = SyntaxKind.AndAlsoExpression Implements ISyntaxKindsService.LogicalAndExpression
        Public ReadOnly Property LogicalOrExpression As Integer = SyntaxKind.OrElseExpression Implements ISyntaxKindsService.LogicalOrExpression
    End Class
End Namespace
