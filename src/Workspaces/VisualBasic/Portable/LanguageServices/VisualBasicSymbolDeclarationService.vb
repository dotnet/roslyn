' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageService(GetType(ISymbolDeclarationService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSymbolDeclarationService
        Implements ISymbolDeclarationService

#If CODE_STYLE Then
        Public Shared ReadOnly Instance As ISymbolDeclarationService = New VisualBasicSymbolDeclarationService()
#End If

        <ImportingConstructor>
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

        ''' <summary>
        ''' If "node" is the begin statement of a declaration block, return that block, otherwise
        ''' return node.
        ''' </summary>
        Private Shared Function GetBlockFromBegin(node As SyntaxNode) As SyntaxNode
            Dim parent As SyntaxNode = node.Parent
            Dim begin As SyntaxNode = Nothing

            If parent IsNot Nothing Then
                Select Case parent.Kind
                    Case SyntaxKind.NamespaceBlock
                        begin = DirectCast(parent, NamespaceBlockSyntax).NamespaceStatement

                    Case SyntaxKind.ModuleBlock, SyntaxKind.StructureBlock, SyntaxKind.InterfaceBlock, SyntaxKind.ClassBlock
                        begin = DirectCast(parent, TypeBlockSyntax).BlockStatement

                    Case SyntaxKind.EnumBlock
                        begin = DirectCast(parent, EnumBlockSyntax).EnumStatement

                    Case SyntaxKind.SubBlock, SyntaxKind.FunctionBlock, SyntaxKind.ConstructorBlock,
                         SyntaxKind.OperatorBlock, SyntaxKind.GetAccessorBlock, SyntaxKind.SetAccessorBlock,
                         SyntaxKind.AddHandlerAccessorBlock, SyntaxKind.RemoveHandlerAccessorBlock, SyntaxKind.RaiseEventAccessorBlock
                        begin = DirectCast(parent, MethodBlockBaseSyntax).BlockStatement

                    Case SyntaxKind.PropertyBlock
                        begin = DirectCast(parent, PropertyBlockSyntax).PropertyStatement

                    Case SyntaxKind.EventBlock
                        begin = DirectCast(parent, EventBlockSyntax).EventStatement

                    Case SyntaxKind.VariableDeclarator
                        If DirectCast(parent, VariableDeclaratorSyntax).Names.Count = 1 Then
                            begin = node
                        End If
                End Select
            End If

            If begin Is node Then
                Return parent
            Else
                Return node
            End If
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
