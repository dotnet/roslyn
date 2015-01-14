' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Module EmbeddedSymbolExtensions

        ''' <summary>
        ''' True if the syntax tree is an embedded syntax tree
        ''' </summary>
        <Extension()>
        Public Function IsEmbeddedSyntaxTree(tree As SyntaxTree) As Boolean
            Return EmbeddedSymbolManager.GetEmbeddedKind(tree) <> EmbeddedSymbolKind.None
        End Function

        <Extension()>
        Public Function GetEmbeddedKind(tree As SyntaxTree) As EmbeddedSymbolKind
            Return EmbeddedSymbolManager.GetEmbeddedKind(tree)
        End Function

        <Extension()>
        Public Function IsEmbeddedOrMyTemplateTree(tree As SyntaxTree) As Boolean
            Dim vbTree = TryCast(tree, VisualBasicSyntaxTree)
            Return vbTree IsNot Nothing AndAlso vbTree.IsMyTemplate OrElse vbTree.IsEmbeddedSyntaxTree
        End Function

        <Extension()>
        Public Function IsEmbeddedOrMyTemplateLocation(location As Location) As Boolean
            Return TypeOf location Is EmbeddedTreeLocation OrElse
                TypeOf location Is MyTemplateLocation
        End Function
    End Module

End Namespace
