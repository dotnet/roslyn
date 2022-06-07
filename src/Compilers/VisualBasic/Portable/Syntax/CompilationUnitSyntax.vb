' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Public NotInheritable Class CompilationUnitSyntax
        Inherits VisualBasicSyntaxNode
        Implements ICompilationUnitSyntax

        Private ReadOnly Property ICompilationUnitSyntax_EndOfFileToken As SyntaxToken Implements ICompilationUnitSyntax.EndOfFileToken
            Get
                Return EndOfFileToken
            End Get
        End Property

        ''' <summary> 
        ''' Returns #r directives specified in the compilation. 
        ''' </summary>       
        Public Function GetReferenceDirectives() As IList(Of ReferenceDirectiveTriviaSyntax)
            Return GetReferenceDirectives(Nothing)
        End Function

        Friend Function GetReferenceDirectives(filter As Func(Of ReferenceDirectiveTriviaSyntax, Boolean)) As IList(Of ReferenceDirectiveTriviaSyntax)
            ' #r directives are always on the first token of the compilation unit.
            Dim firstToken = CType(Me.GetFirstToken(includeZeroWidth:=True), SyntaxNodeOrToken)
            Return firstToken.GetDirectives(Of ReferenceDirectiveTriviaSyntax)(filter)
        End Function

    End Class
End Namespace

