' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.MoveType
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.MoveType
    <ExportLanguageService(GetType(IMoveTypeService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicMoveTypeService
        Inherits AbstractMoveTypeService(Of VisualBasicMoveTypeService, TypeBlockSyntax, NamespaceBlockSyntax, MethodBaseSyntax, CompilationUnitSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Async Function GetRelevantNodeAsync(document As Document, textSpan As TextSpan, cancellationToken As CancellationToken) As Task(Of TypeBlockSyntax)
            Dim someThing As TypeStatementSyntax = Await document.TryGetRelevantNodeAsync(Of TypeStatementSyntax)(textSpan, cancellationToken).ConfigureAwait(False)
            Return TryCast(someThing?.Parent, TypeBlockSyntax)
        End Function
    End Class
End Namespace
