' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary> 
    ''' Manages symbols from automatically embedded syntax trees. 
    ''' </summary>
    Partial Friend NotInheritable Class EmbeddedSymbolManager

        Private Shared _embeddedSyntax As SyntaxTree = Nothing
        Private Shared _vbCoreSyntax As SyntaxTree = Nothing
        Private Shared _internalXmlHelperSyntax As SyntaxTree = Nothing

        Friend Shared Function GetEmbeddedKind(tree As SyntaxTree) As EmbeddedSymbolKind
            Debug.Assert(tree IsNot Nothing)
            If tree Is Nothing Then
                Return EmbeddedSymbolKind.None
            ElseIf tree Is _embeddedSyntax Then
                Return EmbeddedSymbolKind.EmbeddedAttribute
            ElseIf tree Is _vbCoreSyntax Then
                Return EmbeddedSymbolKind.VbCore
            ElseIf tree Is _internalXmlHelperSyntax Then
                Return EmbeddedSymbolKind.XmlHelper
            Else
                Return EmbeddedSymbolKind.None
            End If
        End Function

        Friend Shared Function GetEmbeddedTree(kind As EmbeddedSymbolKind) As SyntaxTree
            Select Case kind
                Case EmbeddedSymbolKind.EmbeddedAttribute
                    Return EmbeddedSyntax
                Case EmbeddedSymbolKind.VbCore
                    Return VbCoreSyntaxTree
                Case EmbeddedSymbolKind.XmlHelper
                    Return InternalXmlHelperSyntax
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(kind)
            End Select
        End Function

        Public Shared ReadOnly Property EmbeddedSyntax As SyntaxTree
            Get
                If _embeddedSyntax Is Nothing Then
                    Interlocked.CompareExchange(_embeddedSyntax,
                                                VisualBasicSyntaxTree.ParseText(EmbeddedResources.Embedded),
                                                Nothing)
                End If
                Return _embeddedSyntax
            End Get
        End Property

        ''' <summary>
        ''' Lazily created parsed representation of VB Core content
        ''' </summary>
        Public Shared ReadOnly Property VbCoreSyntaxTree As SyntaxTree
            Get
                If _vbCoreSyntax Is Nothing Then
                    Interlocked.CompareExchange(_vbCoreSyntax,
                                                VisualBasicSyntaxTree.ParseText(EmbeddedResources.VbCoreSourceText),
                                                Nothing)
                End If
                Return _vbCoreSyntax
            End Get
        End Property

        Public Shared ReadOnly Property InternalXmlHelperSyntax As SyntaxTree
            Get
                If _internalXmlHelperSyntax Is Nothing Then
                    Interlocked.CompareExchange(_internalXmlHelperSyntax,
                                                VisualBasicSyntaxTree.ParseText(EmbeddedResources.InternalXmlHelper),
                                                Nothing)
                End If
                Return _internalXmlHelperSyntax
            End Get
        End Property

    End Class

End Namespace
