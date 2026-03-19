' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageService(GetType(ISymbolDeclarationService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSymbolDeclarationService
        Implements ISymbolDeclarationService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        ''' <summary>
        ''' Get the declaring syntax node for a Symbol. Unlike the DeclaringSyntaxReferences property,
        ''' this function always returns a block syntax, if there is one.
        ''' </summary>
        Public Function GetDeclarations(symbol As ISymbol) As ImmutableArray(Of SyntaxReference) Implements ISymbolDeclarationService.GetDeclarations
            Return If(symbol Is Nothing,
                      ImmutableArray(Of SyntaxReference).Empty,
                      symbol.DeclaringSyntaxReferences.SelectAsArray(Of SyntaxReference)(
                        Function(r) New BlockSyntaxReference(r)))
        End Function

        Private Class BlockSyntaxReference
            Inherits SyntaxReference

            Private ReadOnly _reference As SyntaxReference

            Public Sub New(reference As SyntaxReference)
                _reference = reference
            End Sub

            Public Overrides Function GetSyntax(Optional cancellationToken As CancellationToken = Nothing) As SyntaxNode
                Return DirectCast(GetBlockFromBegin(_reference.GetSyntax(cancellationToken)), VisualBasicSyntaxNode)
            End Function

            Public Overrides Async Function GetSyntaxAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of SyntaxNode)
                Dim node = Await _reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(False)
                Return GetBlockFromBegin(node)
            End Function

            Public Overrides ReadOnly Property Span As TextSpan
                Get
                    Return _reference.Span
                End Get
            End Property

            Public Overrides ReadOnly Property SyntaxTree As SyntaxTree
                Get
                    Return _reference.SyntaxTree
                End Get
            End Property
        End Class
    End Class
End Namespace
