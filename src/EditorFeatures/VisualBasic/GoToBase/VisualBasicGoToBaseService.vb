' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.GoToBase
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.GoToBase
    <ExportLanguageService(GetType(IGoToBaseService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGoToBaseService
        Inherits AbstractGoToBaseService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Async Function FindNextConstructorInChainAsync(solution As Solution, constructor As IMethodSymbol, cancellationToken As CancellationToken) As Task(Of IMethodSymbol)
            Dim subNew = TryCast(constructor.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken), SubNewStatementSyntax)
            If subNew Is Nothing Then
                Return Nothing
            End If

            Dim constructorBlock = TryCast(subNew.Parent, ConstructorBlockSyntax)
            If constructorBlock Is Nothing Then
                Return Nothing
            End If

            Dim initializer As MemberAccessExpressionSyntax = Nothing
            If constructorBlock.Statements.Count = 0 OrElse
               Not constructorBlock.Statements(0).IsConstructorInitializer(initializer) Then
                Return FindBaseNoArgConstructor(constructor)
            End If

            Dim document = solution.GetDocument(constructorBlock.SyntaxTree)
            If document Is Nothing Then
                Return Nothing
            End If

            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Return TryCast(semanticModel.GetSymbolInfo(initializer, cancellationToken).GetAnySymbol(), IMethodSymbol)
        End Function
    End Class
End Namespace
