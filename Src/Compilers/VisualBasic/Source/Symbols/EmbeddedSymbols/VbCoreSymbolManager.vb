'-----------------------------------------------------------------------------
' Copyright (c) Microsoft Corporation. All rights reserved.
'-----------------------------------------------------------------------------

Imports System
Imports System.Threading

Namespace Roslyn.Compilers.VisualBasic

    <Flags()>
    Friend Enum EmbeddedSymbolKind
        None = 0
        Unset = 1
        EmbeddedAttribute = 2
        VbCore = 4
        XmlHelper = 8
        All = (EmbeddedAttribute Or VbCore Or XmlHelper)
    End Enum

    ''' <summary> 
    ''' Manages symbols from VB Code runtime. 
    ''' </summary>
    Partial Friend NotInheritable Class VbCoreSymbolManager

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

        Friend Shared Function IsVbCoreSyntaxTree(tree As SyntaxTree) As Boolean
            Debug.Assert(tree IsNot Nothing)
            Return (tree IsNot Nothing) AndAlso (tree Is _vbCoreSyntax)
        End Function

        Public Shared ReadOnly Property EmbeddedSyntax As SyntaxTree
            Get
                If _embeddedSyntax Is Nothing Then
                    Interlocked.CompareExchange(_embeddedSyntax,
                                                SyntaxTree.ParseText(VbCoreResources.Embedded),
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
                                                SyntaxTree.ParseText(VbCoreResources.VbCoreSourceText),
                                                Nothing)
                End If
                Return _vbCoreSyntax
            End Get
        End Property

        Public Shared ReadOnly Property InternalXmlHelperSyntax As SyntaxTree
            Get
                If _internalXmlHelperSyntax Is Nothing Then
                    Interlocked.CompareExchange(_internalXmlHelperSyntax,
                                                SyntaxTree.ParseText(VbCoreResources.InternalXmlHelper),
                                                Nothing)
                End If
                Return _internalXmlHelperSyntax
            End Get
        End Property

    End Class

End Namespace
