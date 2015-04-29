' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Friend Class CrefCompletionProvider
        Inherits AbstractCompletionProvider

        Private _crefFormat2 As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameOnly,
                propertyStyle:=SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions:=
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Public Overrides Function IsCommitCharacter(completionItem As CompletionItem, ch As Char, textTypedSoFar As String) As Boolean
            If ch = "("c AndAlso completionItem.DisplayText.IndexOf("("c) <> -1 Then
                Return False
            End If

            If ch = " " Then
                Dim textSoFar = textTypedSoFar.TrimEnd()
                Return Not (textSoFar.Length >= 2 AndAlso Char.ToUpper(textSoFar(textSoFar.Length - 2)) = "O"c AndAlso Char.ToUpper(textSoFar(textSoFar.Length - 1)) = "F"c)
            End If

            Return CompletionUtilities.IsCommitCharacter(completionItem, ch, textTypedSoFar)
        End Function

        Public Overrides Function IsTriggerCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return CompletionUtilities.IsDefaultTriggerCharacter(text, characterPosition, options)
        End Function

        Public Overrides Function SendEnterThroughToEditor(completionItem As CompletionItem, textTypedSoFar As String) As Boolean
            Return False
        End Function

        Protected Overrides Async Function GetItemsWorkerAsync(document As Document, position As Integer, triggerInfo As CompletionTriggerInfo, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of CompletionItem))
            Dim tree = Await document.GetVisualBasicSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim text = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)

            Dim span = CompletionUtilities.GetTextChangeSpan(text, position)
            Dim token = tree.FindTokenOnLeftOfPosition(position, cancellationToken)
            Dim touchingToken = token.GetPreviousTokenIfTouchingWord(position)

            If touchingToken.Kind = SyntaxKind.None Then
                Return Nothing
            End If

            If token.GetAncestor(Of DocumentationCommentTriviaSyntax)() Is Nothing Then
                Return Nothing
            End If

            Dim semanticModel = Await document.GetVisualBasicSemanticModelForNodeAsync(touchingToken.Parent, cancellationToken).ConfigureAwait(False)
            Dim workspace = document.Project.Solution.Workspace

            If IsXmlStringContext(touchingToken) OrElse
                IsCrefStartPosition(touchingToken) Then
                ' Not after a dot, return all the available symbols

                Dim symbols = semanticModel.LookupSymbols(position)
                Return CreateCompletionItems(symbols, span, semanticModel, workspace, touchingToken.SpanStart)
            End If

            If IsTypeParameterContext(touchingToken) Then
                Return Nothing
            End If

            If IsFirstParameterContext(touchingToken) OrElse IsOtherParameterContext(touchingToken) Then
                Dim symbols = semanticModel.LookupNamespacesAndTypes(position)
                Dim items = CreateCompletionItems(symbols, span, semanticModel, workspace, touchingToken.SpanStart)

                If (IsFirstParameterContext(touchingToken)) Then
                    ' Include Of in case they're typing a type parameter
                    Return items.Concat(CreateOfCompletionItem(span))
                End If

                Return items
            End If

            If touchingToken.IsChildToken(Function(x As QualifiedNameSyntax) x.DotToken) Then
                ' Bind the name left of the dot
                Dim container = DirectCast(touchingToken.Parent, QualifiedNameSyntax).Left
                Dim leftSymbol = semanticModel.GetSymbolInfo(container, cancellationToken)

                Dim containingType = TryCast(leftSymbol.Symbol, INamespaceOrTypeSymbol)

                If containingType Is Nothing Then
                    containingType = semanticModel.GetTypeInfo(container, cancellationToken).Type
                End If

                If containingType IsNot Nothing Then
                    Dim symbols = semanticModel.LookupSymbols(position, containingType)
                    Dim constructors = GetConstructors(containingType)

                    Return CreateCompletionItems(constructors.Concat(symbols), span, semanticModel, workspace, touchingToken.SpanStart)
                End If
            End If

            Return Nothing
        End Function

        Private Function IsTypeParameterContext(touchingToken As SyntaxToken) As Boolean
            Return (touchingToken.IsChildToken(Function(t As TypeArgumentListSyntax) t.OfKeyword) OrElse
                touchingToken.IsChildSeparatorToken(Function(t As TypeArgumentListSyntax) t.Arguments)) AndAlso
                touchingToken.GetAncestor(Of XmlCrefAttributeSyntax)() IsNot Nothing
        End Function

        Private Function GetConstructors(container As ISymbol) As IEnumerable(Of ISymbol)
            Dim namedType = TryCast(container, INamedTypeSymbol)
            If namedType Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of IMethodSymbol)()
            End If

            Return namedType.Constructors
        End Function

        Private Function IsCrefContext(token As SyntaxToken) As Boolean
            If token.Parent.IsKind(SyntaxKind.XmlString) AndAlso token.Parent.Parent.IsKind(SyntaxKind.XmlAttribute) Then
                Dim attribute = DirectCast(token.Parent.Parent, XmlAttributeSyntax)
                Dim name = TryCast(attribute.Name, XmlNameSyntax)
                If name IsNot Nothing AndAlso name.LocalName.ValueText = "cref" Then
                    Return True
                End If
            End If

            If token.Parent.GetAncestor(Of XmlCrefAttributeSyntax)() IsNot Nothing Then
                Return True
            End If

            Return False
        End Function

        Private Shared Function IsFirstParameterContext(ByRef touchingToken As SyntaxToken) As Boolean
            Return touchingToken.IsChildToken(Function(x As CrefSignatureSyntax) x.OpenParenToken)
        End Function

        Private Shared Function IsOtherParameterContext(ByRef touchingToken As SyntaxToken) As Boolean
            Return touchingToken.IsChildSeparatorToken(Function(p As CrefSignatureSyntax) p.ArgumentTypes)
        End Function

        Private Shared Function IsCrefStartPosition(ByRef touchingToken As SyntaxToken) As Boolean
            Return touchingToken.IsChildToken(Function(x As XmlCrefAttributeSyntax) x.StartQuoteToken)
        End Function

        Private Function IsXmlStringContext(token As SyntaxToken) As Boolean
            If Not token.IsChildToken(Function(s As XmlStringSyntax) s.StartQuoteToken) Then
                Return False
            End If

            Dim parentAttribute = TryCast(token.Parent.Parent, XmlAttributeSyntax)
            If parentAttribute Is Nothing Then
                Return False
            End If

            Dim parentNameSyntax = TryCast(parentAttribute.Name, XmlNameSyntax)
            If parentAttribute Is Nothing Then
                Return False
            End If

            Return parentNameSyntax.LocalName.ValueText = "cref"
        End Function

        Private Function CreateCompletionItems(symbols As IEnumerable(Of ISymbol), span As TextSpan, semanticModel As SemanticModel, workspace As Workspace, position As Integer) As IEnumerable(Of CompletionItem)
            Return symbols.Select(Function(s)
                                      Dim displayString As String
                                      If s.Kind = SymbolKind.Method Then
                                          Dim method = DirectCast(s, IMethodSymbol)
                                          displayString = method.ToDisplayString(_crefFormat2) + CreateParameters(method, semanticModel, position)
                                          If method.MethodKind = MethodKind.UserDefinedOperator Then
                                              displayString = "Operator " + displayString
                                          End If
                                      ElseIf s.GetParameters().Any() Then
                                          displayString = s.ToDisplayString(_crefFormat2) + CreateParameters(s, semanticModel, position)
                                      Else
                                          displayString = s.ToDisplayString(_crefFormat2)
                                      End If

                                      Return New CompletionItem(Me, displayString, span, glyph:=s.GetGlyph(),
                                                                descriptionFactory:=CommonCompletionUtilities.CreateDescriptionFactory(workspace, semanticModel, position, s))
                                  End Function)
        End Function

        Private Function CreateOfCompletionItem(span As TextSpan) As IEnumerable(Of CompletionItem)
            Dim item = New CompletionItem(Me, "Of", span, glyph:=Glyph.Keyword,
                                      descriptionFactory:=Function(c As CancellationToken) Task.FromResult(RecommendedKeyword.CreateDisplayParts("Of", VBFeaturesResources.OfKeywordToolTip)))

            Return SpecializedCollections.SingletonEnumerable(item)
        End Function

        Protected Overrides Function IsExclusiveAsync(document As Document, position As Integer, triggerInfo As CompletionTriggerInfo, cancellationToken As CancellationToken) As Task(Of Boolean)
            Return SpecializedTasks.True
        End Function

        Private Function CreateParameters(method As ISymbol, semanticModel As SemanticModel, position As Integer) As String
            Dim parameterNames = method.GetParameters().Select(Function(p)
                                                                   If p.RefKind = RefKind.Ref Then
                                                                       Return "ByRef " + p.Type.ToMinimalDisplayString(semanticModel, position)
                                                                   End If
                                                                   Return p.Type.ToMinimalDisplayString(semanticModel, position)
                                                               End Function)

            Return String.Format("({0})", String.Join(", ", parameterNames))
        End Function

    End Class
End Namespace
