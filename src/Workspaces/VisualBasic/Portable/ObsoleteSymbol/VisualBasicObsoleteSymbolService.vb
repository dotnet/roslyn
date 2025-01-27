' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.ObsoleteSymbol
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ObsoleteSymbol
    <ExportLanguageService(GetType(IObsoleteSymbolService), LanguageNames.VisualBasic)>
    <[Shared]>
    Friend Class VisualBasicObsoleteSymbolService
        Inherits AbstractObsoleteSymbolService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New(SyntaxKind.DimKeyword)
        End Sub

        Protected Overrides Sub ProcessDimKeyword(ByRef result As ArrayBuilder(Of TextSpan), semanticModel As SemanticModel, token As SyntaxToken, cancellationToken As CancellationToken)
            Dim localDeclaration = TryCast(token.Parent, LocalDeclarationStatementSyntax)
            If localDeclaration Is Nothing Then
                Return
            End If

            If localDeclaration.Declarators.Count <> 1 Then
                Return
            End If

            Dim declarator = localDeclaration.Declarators(0)
            If declarator.AsClause IsNot Nothing Then
                ' This is an explicitly typed variable, so no need to mark 'Dim' obsolete
                Return
            End If

            If declarator.Names.Count <> 1 Then
                ' More than one variable is declared
                Return
            End If

            ' Only one variable is declared
            Dim localSymbol = TryCast(semanticModel.GetDeclaredSymbol(declarator.Names(0), cancellationToken), ILocalSymbol)
            If IsSymbolObsolete(localSymbol?.Type) Then
                If result Is Nothing Then
                    result = ArrayBuilder(Of TextSpan).GetInstance()
                End If

                result.Add(token.Span)
            End If
        End Sub
    End Class
End Namespace
