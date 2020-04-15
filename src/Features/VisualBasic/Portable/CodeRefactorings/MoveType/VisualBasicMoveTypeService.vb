' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Async Function GetRelevantNodeAsync(document As Document, textSpan As TextSpan, cancellationToken As CancellationToken) As Task(Of TypeBlockSyntax)
            Dim typeStatement As TypeStatementSyntax = Await document.TryGetRelevantNodeAsync(Of TypeStatementSyntax)(textSpan, cancellationToken).ConfigureAwait(False)
            Return TryCast(typeStatement?.Parent, TypeBlockSyntax)
        End Function
    End Class
End Namespace
