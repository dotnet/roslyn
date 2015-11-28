﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' A program location in MyTemplate code.
    ''' </summary>
    Friend NotInheritable Class MyTemplateLocation
        Inherits VBLocation

        Private ReadOnly _span As TextSpan
        Private ReadOnly _tree As SyntaxTree

        Public Overrides ReadOnly Property Kind As LocationKind
            Get
                Return LocationKind.None
            End Get
        End Property

        Friend Overrides ReadOnly Property PossiblyEmbeddedOrMySourceSpan As TextSpan
            Get
                Return _span
            End Get
        End Property

        Friend Overrides ReadOnly Property PossiblyEmbeddedOrMySourceTree As SyntaxTree
            Get
                Return _tree
            End Get
        End Property

        Public Sub New(tree As SyntaxTree, span As TextSpan)
            Debug.Assert(tree.IsMyTemplate)

            _span = span
            _tree = tree
        End Sub

        Public Overloads Function Equals(other As MyTemplateLocation) As Boolean
            If Me Is other Then
                Return True
            End If

            Return other IsNot Nothing AndAlso _tree Is other._tree AndAlso other._span.Equals(_span)
        End Function

        Public Overloads Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, MyTemplateLocation))
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _span.GetHashCode()
        End Function
    End Class
End Namespace
