' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities
    Friend Class NameSyntaxComparer
        Implements IComparer(Of NameSyntax)
        Private ReadOnly _tokenComparer As IComparer(Of SyntaxToken)
        Friend typeComparer As TypeSyntaxComparer

        Friend Sub New(tokenComparer As IComparer(Of SyntaxToken))
            Me._tokenComparer = tokenComparer
        End Sub

        Public Shared Function Create() As IComparer(Of NameSyntax)
            Return Create(TokenComparer.NormalInstance)
        End Function

        Public Shared Function Create(tokenComparer As IComparer(Of SyntaxToken)) As IComparer(Of NameSyntax)
            Dim nameComparer = New NameSyntaxComparer(tokenComparer)
            Dim typeComparer = New TypeSyntaxComparer(tokenComparer)

            nameComparer.typeComparer = typeComparer
            typeComparer.nameComparer = nameComparer

            Return nameComparer
        End Function

        Public Function Compare(x As NameSyntax, y As NameSyntax) As Integer Implements IComparer(Of NameSyntax).Compare
            If x Is y Then
                Return 0
            End If

            If x.IsMissing AndAlso y.IsMissing Then
                Return 0
            End If

            If x.IsMissing Then
                Return -1
            ElseIf y.IsMissing Then
                Return 1
            End If

            ' If we have a basic name, then it's simple to compare.  Just
            ' check that token versus whatever the other name has as the
            ' first token.
            If TypeOf x Is IdentifierNameSyntax AndAlso TypeOf y Is IdentifierNameSyntax Then
                Return _tokenComparer.Compare(x.GetFirstToken(), y.GetFirstToken())
            ElseIf TypeOf x Is GenericNameSyntax AndAlso TypeOf y Is GenericNameSyntax Then
                ' if both names are generic, then use a specialized routine
                ' that will check the names *and* the arguments.
                Return Compare(DirectCast(x, GenericNameSyntax), DirectCast(y, GenericNameSyntax))
            ElseIf TypeOf x Is IdentifierNameSyntax AndAlso TypeOf y Is GenericNameSyntax Then
                Dim value = _tokenComparer.Compare(x.GetFirstToken(), y.GetFirstToken())
                If (value <> 0) Then
                    Return value
                End If
                ' Goo goes before Goo(of T)
                Return -1
            ElseIf TypeOf x Is GenericNameSyntax AndAlso TypeOf y Is IdentifierNameSyntax Then
                Dim value = _tokenComparer.Compare(x.GetFirstToken(), y.GetFirstToken())
                If (value <> 0) Then
                    Return value
                End If
                ' Goo(of T) goes after Goo
                Return 1
            End If

            ' At this point one or both of the nodes is a dotted name or
            ' aliased name.  Break them apart into individual pieces and
            ' compare those.

            Dim xNameParts = DecomposeNameParts(x)
            Dim yNameParts = DecomposeNameParts(y)

            For i = 0 To Math.Min(xNameParts.Count, yNameParts.Count) - 1
                Dim value = Compare(xNameParts(i), yNameParts(i))
                If (value <> 0) Then
                    Return value
                End If
            Next

            ' they matched up to this point.  The shorter one should come
            ' first.
            Return xNameParts.Count - yNameParts.Count
        End Function

        Private Function DecomposeNameParts(name As NameSyntax) As IList(Of NameSyntax)
            Dim result = New List(Of NameSyntax)()
            DecomposeNameParts(name, result)
            Return result
        End Function

        Private Sub DecomposeNameParts(name As NameSyntax, result As List(Of NameSyntax))
            Select Case name.Kind
                Case SyntaxKind.QualifiedName
                    Dim dottedName = DirectCast(name, QualifiedNameSyntax)
                    result.AddRange(DecomposeNameParts(dottedName.Left))
                    result.AddRange(DecomposeNameParts(SyntaxFactory.IdentifierName(dottedName.Right.Identifier)))
                    Return
                Case SyntaxKind.IdentifierName,
                     SyntaxKind.GenericName
                    result.Add(name)
                Case SyntaxKind.GlobalName
                    Dim globalName = DirectCast(name, GlobalNameSyntax)
                    result.Add(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(globalName.GlobalKeyword.ToString())))
            End Select
        End Sub

        Private Function Compare(x As GenericNameSyntax, y As GenericNameSyntax) As Integer
            Dim value = Compare(SyntaxFactory.IdentifierName(x.Identifier), SyntaxFactory.IdentifierName(y.Identifier))
            If (value <> 0) Then
                Return value
            End If

            ' The one with less type params comes first.
            value = x.TypeArgumentList.Arguments.Count - y.TypeArgumentList.Arguments.Count
            If (value <> 0) Then
                Return value
            End If

            ' Same name, same parameter count.  Compare each parameter.
            For i = 0 To x.TypeArgumentList.Arguments.Count
                Dim xArg = x.TypeArgumentList.Arguments(i)
                Dim yArg = y.TypeArgumentList.Arguments(i)

                value = typeComparer.Compare(xArg, yArg)
                If (value <> 0) Then
                    Return value
                End If
            Next

            Return 0
        End Function
    End Class
End Namespace
