' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Public NotInheritable Class CompilationUnitSyntax
        Inherits VisualBasicSyntaxNode
        Implements ICompilationUnitSyntax

        Private ReadOnly Property ICompilationUnitSyntax_EndOfFileToken As SyntaxToken Implements ICompilationUnitSyntax.EndOfFileToken
            Get
                Return EndOfFileToken
            End Get
        End Property

        Private ReadOnly Property ICompilationUnitSyntax_Members As SyntaxList(Of SyntaxNode) Implements ICompilationUnitSyntax.Members
            Get
                Return Me.Members
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

