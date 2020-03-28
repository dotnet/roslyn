' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Debugging

Namespace Microsoft.CodeAnalysis.VisualBasic.Debugging

    Friend Class BreakpointResolver
        Inherits AbstractBreakpointResolver

        Private Shared ReadOnly s_identifierComparer As IEqualityComparer(Of String) = CaseInsensitiveComparison.Comparer

        Public Sub New(solution As Solution, text As String)
            MyBase.New(solution, text, LanguageNames.VisualBasic, s_identifierComparer)
        End Sub

        Protected Overrides Function GetMembers(type As INamedTypeSymbol, name As String) As IEnumerable(Of ISymbol)
            Dim members = type.GetMembers(name)

            Return If(s_identifierComparer.Equals(name, SyntaxFacts.GetText(SyntaxKind.NewKeyword)),
                      members.Concat(type.Constructors),
                      members)
        End Function

        Protected Overrides Function HasMethodBody(method As IMethodSymbol, cancellationToken As CancellationToken) As Boolean
            Dim location = method.Locations.First(Function(loc) loc.IsInSource)
            Dim tree = location.SourceTree
            Dim token = tree.GetRoot(cancellationToken).FindToken(location.SourceSpan.Start)
            Dim methodBlock = token.GetAncestor(Of MethodBlockBaseSyntax)()

            ' If there is no syntactic body then, obviously, False...
            If methodBlock Is Nothing Then
                Return False
            End If

            ' In VB, Partial method definitions have a syntactic body, but for the purpose of setting breakpoints in code,
            ' they should not be considered to have a body (because there is no executable code associated with them).
            If methodBlock.BlockStatement.Modifiers.Any(Function(t) t.IsKind(SyntaxKind.PartialKeyword)) Then
                Return False
            End If

            Return True
        End Function

        Protected Overrides Sub ParseText(ByRef nameParts As IList(Of NameAndArity), ByRef parameterCount As Integer?)
            Dim text As String = Me.Text

            Debug.Assert(text IsNot Nothing)

            Dim name = SyntaxFactory.ParseName(Me.Text, consumeFullText:=False)
            Dim lengthOfParsedText = name.FullSpan.End
            Dim parameterList = ParseParameterList(Me.Text, lengthOfParsedText)
            Dim foundIncompleteParameterList = False

            parameterCount = Nothing
            If parameterList IsNot Nothing Then
                If (parameterList.OpenParenToken.IsMissing OrElse parameterList.CloseParenToken.IsMissing) Then
                    foundIncompleteParameterList = True
                Else
                    lengthOfParsedText += parameterList.FullSpan.End
                    parameterCount = parameterList.Parameters.Count
                End If
            End If

            ' It's not obvious, but this method can handle the case were name "IsMissing" (no suitable name was be parsed).
            Dim parts = name.GetNameParts()

            ' If we could not parse a valid parameter list or there was additional trailing text that could not be
            ' interpreted, don't return any names or parameters.
            ' Also, "Break at Function" doesn't seem to support names prefixed with "Global" with the old language service.
            ' Since it doesn't seem necessary to disambiguate symbols in this scenario (there's UI to do it), I'm going to
            ' explicitly ignore names with a Global namespace prefix.  If we want to correctly support Global, we'd need to
            ' also modify the logic in FindMembersAsync to support exact matches on type name.  "Global" prefixes on
            ' parameters will be accepted, but we still only validate parameter count (as the old implementation did).
            If Not foundIncompleteParameterList AndAlso (lengthOfParsedText = Me.Text.Length) AndAlso
               Not parts.Where(Function(p) p.IsKind(SyntaxKind.GlobalName)).Any() Then
                nameParts = parts.Cast(Of SimpleNameSyntax)().Select(Function(p) New NameAndArity(p.Identifier.ValueText, p.Arity)).ToList()
            Else
                nameParts = SpecializedCollections.EmptyList(Of NameAndArity)()
            End If
        End Sub

        ' TODO:  This method can go away once https://roslyn.codeplex.com/workitem/231 is fixed.
        Private Shared Function ParseParameterList(text As String, offset As Integer) As ParameterListSyntax
            Return If(SyntaxFactory.ParseToken(text, offset).IsKind(SyntaxKind.OpenParenToken),
                      SyntaxFactory.ParseParameterList(text, offset, consumeFullText:=False),
                      Nothing)
        End Function

    End Class

End Namespace
