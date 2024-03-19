' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary> 
    ''' Manages symbols from automatically embedded syntax trees. 
    ''' </summary>
    Partial Friend NotInheritable Class EmbeddedSymbolManager

        Private Shared s_embeddedSyntax As SyntaxTree = Nothing
        Private Shared s_vbCoreSyntax As SyntaxTree = Nothing
        Private Shared s_internalXmlHelperSyntax As SyntaxTree = Nothing

        Private Shared Function ParseResourceText(text As String) As SyntaxTree
            Return VisualBasicSyntaxTree.ParseText(SourceText.From(text, Encoding.UTF8, SourceHashAlgorithms.Default))
        End Function

        Friend Shared Function GetEmbeddedKind(tree As SyntaxTree) As EmbeddedSymbolKind
            Debug.Assert(tree IsNot Nothing)
            If tree Is Nothing Then
                Return EmbeddedSymbolKind.None
            ElseIf tree Is s_embeddedSyntax Then
                Return EmbeddedSymbolKind.EmbeddedAttribute
            ElseIf tree Is s_vbCoreSyntax Then
                Return EmbeddedSymbolKind.VbCore
            ElseIf tree Is s_internalXmlHelperSyntax Then
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
                If s_embeddedSyntax Is Nothing Then
                    Interlocked.CompareExchange(s_embeddedSyntax, ParseResourceText(EmbeddedResources.Embedded), Nothing)
                    If s_embeddedSyntax.GetDiagnostics().Any() Then
                        Throw ExceptionUtilities.Unreachable
                    End If
                End If
                Return s_embeddedSyntax
            End Get
        End Property

        ''' <summary>
        ''' Lazily created parsed representation of VB Core content
        ''' </summary>
        Public Shared ReadOnly Property VbCoreSyntaxTree As SyntaxTree
            Get
                If s_vbCoreSyntax Is Nothing Then
                    Interlocked.CompareExchange(s_vbCoreSyntax, ParseResourceText(EmbeddedResources.VbCoreSourceText), Nothing)
                    If s_vbCoreSyntax.GetDiagnostics().Any() Then
                        Throw ExceptionUtilities.Unreachable
                    End If
                End If
                Return s_vbCoreSyntax
            End Get
        End Property

        Public Shared ReadOnly Property InternalXmlHelperSyntax As SyntaxTree
            Get
                If s_internalXmlHelperSyntax Is Nothing Then
                    Interlocked.CompareExchange(s_internalXmlHelperSyntax, ParseResourceText(EmbeddedResources.InternalXmlHelper), Nothing)
                    If s_internalXmlHelperSyntax.GetDiagnostics().Any() Then
                        Throw ExceptionUtilities.Unreachable
                    End If
                End If
                Return s_internalXmlHelperSyntax
            End Get
        End Property

    End Class

End Namespace
