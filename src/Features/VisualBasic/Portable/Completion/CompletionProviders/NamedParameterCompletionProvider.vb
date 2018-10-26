﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.ErrorReporting

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Partial Friend Class NamedParameterCompletionProvider
        Inherits CommonCompletionProvider

        Friend Const s_colonEquals As String = ":="

        Friend Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return CompletionUtilities.IsDefaultTriggerCharacter(text, characterPosition, options)
        End Function

        Public Overrides Async Function ProvideCompletionsAsync(context As CompletionContext) As Task
            Try
                Dim document = context.Document
                Dim position = context.Position
                Dim cancellationToken = context.CancellationToken

                Dim syntaxTree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
                If syntaxTree.IsInNonUserCode(position, cancellationToken) OrElse
                    syntaxTree.IsInSkippedText(position, cancellationToken) Then
                    Return
                End If

                Dim token = syntaxTree.GetTargetToken(position, cancellationToken)

                If Not token.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CommaToken) Then
                    Return
                End If

                Dim argumentList = TryCast(token.Parent, ArgumentListSyntax)
                If argumentList Is Nothing Then
                    Return
                End If

                If token.Kind = SyntaxKind.CommaToken Then
                    ' Consider refining this logic to mandate completion with an argument name, if preceded by an out-of-position name
                    ' See https://github.com/dotnet/roslyn/issues/20657
                    Dim languageVersion = DirectCast(document.Project.ParseOptions, VisualBasicParseOptions).LanguageVersion
                    If languageVersion < LanguageVersion.VisualBasic15_5 AndAlso token.IsMandatoryNamedParameterPosition() Then
                        context.IsExclusive = True
                    End If
                End If

                Dim semanticModel = Await document.GetSemanticModelForNodeAsync(argumentList, cancellationToken).ConfigureAwait(False)
                Dim parameterLists = GetParameterLists(semanticModel, position, argumentList.Parent, cancellationToken)
                If parameterLists Is Nothing Then
                    Return
                End If

                Dim existingNamedParameters = GetExistingNamedParameters(argumentList, position)
                parameterLists = parameterLists.Where(Function(p) IsValid(p, existingNamedParameters))

                Dim unspecifiedParameters = parameterLists.SelectMany(Function(pl) pl).
                                                           Where(Function(p) Not existingNamedParameters.Contains(p.Name))

                Dim text = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)

                For Each parameter In unspecifiedParameters
                    context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
                        displayText:=parameter.Name,
                        displayTextSuffix:=s_colonEquals,
                        insertionText:=parameter.Name.ToIdentifierToken().ToString() & s_colonEquals,
                        symbols:=ImmutableArray.Create(parameter),
                        contextPosition:=position,
                        rules:=s_itemRules))
                Next
            Catch e As Exception When FatalError.ReportWithoutCrashUnlessCanceled(e)
                ' nop
            End Try
        End Function

        ' Typing : or = should not filter the list, but they should commit the list.
        Private Shared s_itemRules As CompletionItemRules = CompletionItemRules.Default.
            WithFilterCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ":"c, "="c)).
            WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Add, ":"c, "="c))

        Protected Overrides Function GetDescriptionWorkerAsync(document As Document, item As CompletionItem, cancellationToken As CancellationToken) As Task(Of CompletionDescription)
            Return SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken)
        End Function

        Private Function IsValid(parameterList As ImmutableArray(Of ISymbol), existingNamedParameters As ISet(Of String)) As Boolean
            ' A parameter list is valid if it has parameters that match in name all the existing ;
            ' named parameters that have been provided.
            Return existingNamedParameters.Except(parameterList.Select(Function(p) p.Name)).IsEmpty()
        End Function

        Private Function GetExistingNamedParameters(argumentList As ArgumentListSyntax, position As Integer) As ISet(Of String)
            Dim existingArguments =
                argumentList.Arguments.OfType(Of SimpleArgumentSyntax).
                                       Where(Function(n) n.IsNamed AndAlso Not n.NameColonEquals.ColonEqualsToken.IsMissing AndAlso n.NameColonEquals.Span.End <= position).
                                       Select(Function(a) a.NameColonEquals.Name.Identifier.ValueText).
                                       Where(Function(i) Not String.IsNullOrWhiteSpace(i))

            Return existingArguments.ToSet()
        End Function

        Private Function GetParameterLists(semanticModel As SemanticModel,
                                           position As Integer,
                                           invocableNode As SyntaxNode,
                                           cancellationToken As CancellationToken) As IEnumerable(Of ImmutableArray(Of ISymbol))
            Return invocableNode.TypeSwitch(
                Function(attribute As AttributeSyntax) GetAttributeParameterLists(semanticModel, position, attribute, cancellationToken),
                Function(invocationExpression As InvocationExpressionSyntax) GetInvocationExpressionParameterLists(semanticModel, position, invocationExpression, cancellationToken),
                Function(objectCreationExpression As ObjectCreationExpressionSyntax) GetObjectCreationExpressionParameterLists(semanticModel, position, objectCreationExpression, cancellationToken))
        End Function

        Private Function GetObjectCreationExpressionParameterLists(semanticModel As SemanticModel,
                                                                   position As Integer,
                                                                   objectCreationExpression As ObjectCreationExpressionSyntax,
                                                                   cancellationToken As CancellationToken) As IEnumerable(Of ImmutableArray(Of ISymbol))
            Dim type = TryCast(semanticModel.GetTypeInfo(objectCreationExpression, cancellationToken).Type, INamedTypeSymbol)
            Dim within = semanticModel.GetEnclosingNamedType(position, cancellationToken)

            If type IsNot Nothing AndAlso within IsNot Nothing AndAlso type.TypeKind <> TypeKind.[Delegate] Then
                Return type.InstanceConstructors.Where(Function(c) c.IsAccessibleWithin(within)).
                                                 Select(Function(c) c.Parameters.As(Of ISymbol)())
            End If

            Return Nothing
        End Function

        Private Function GetAttributeParameterLists(semanticModel As SemanticModel,
                                                    position As Integer,
                                                    attribute As AttributeSyntax,
                                                    cancellationToken As CancellationToken) As IEnumerable(Of ImmutableArray(Of ISymbol))
            Dim within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken)
            Dim attributeType = TryCast(semanticModel.GetTypeInfo(attribute, cancellationToken).Type, INamedTypeSymbol)

            Dim namedParameters = attributeType.GetAttributeNamedParameters(semanticModel.Compilation, within)
            Return SpecializedCollections.SingletonEnumerable(
                ImmutableArray.CreateRange(namedParameters))
        End Function

        Private Function GetInvocationExpressionParameterLists(semanticModel As SemanticModel,
                                                               position As Integer,
                                                               invocationExpression As InvocationExpressionSyntax,
                                                               cancellationToken As CancellationToken) As IEnumerable(Of ImmutableArray(Of ISymbol))
            Dim within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken)
            Dim expression = invocationExpression.GetExpression()
            If within IsNot Nothing AndAlso expression IsNot Nothing Then
                Dim memberGroup = semanticModel.GetMemberGroup(expression, cancellationToken)
                Dim expressionType = semanticModel.GetTypeInfo(expression, cancellationToken).Type
                Dim indexers = If(expressionType Is Nothing,
                                   SpecializedCollections.EmptyList(Of IPropertySymbol),
                                   semanticModel.LookupSymbols(position, expressionType, includeReducedExtensionMethods:=True).OfType(Of IPropertySymbol).Where(Function(p) p.IsIndexer).ToList())

                If memberGroup.Length > 0 Then
                    Dim accessibleMembers = memberGroup.Where(Function(m) m.IsAccessibleWithin(within))
                    Dim methodParameters = accessibleMembers.OfType(Of IMethodSymbol).Select(Function(m) m.Parameters.As(Of ISymbol)())
                    Dim propertyParameters = accessibleMembers.OfType(Of IPropertySymbol).Select(Function(p) p.Parameters.As(Of ISymbol)())
                    Return methodParameters.Concat(propertyParameters)
                ElseIf expressionType.IsDelegateType() Then
                    Dim delegateType = DirectCast(expressionType, INamedTypeSymbol)
                    Return SpecializedCollections.SingletonEnumerable(delegateType.DelegateInvokeMethod.Parameters.As(Of ISymbol)())
                ElseIf indexers.Count > 0 Then
                    Return indexers.Where(Function(i) i.IsAccessibleWithin(within, throughTypeOpt:=expressionType)).
                                    Select(Function(i) i.Parameters.As(Of ISymbol)())
                End If
            End If

            Return Nothing
        End Function

        Private Sub GetInvocableNode(token As SyntaxToken, ByRef invocableNode As SyntaxNode, ByRef argumentList As ArgumentListSyntax)
            Dim current = token.Parent

            While current IsNot Nothing
                If TypeOf current Is AttributeSyntax Then
                    invocableNode = current
                    argumentList = (DirectCast(current, AttributeSyntax)).ArgumentList
                    Return
                End If

                If TypeOf current Is InvocationExpressionSyntax Then
                    invocableNode = current
                    argumentList = (DirectCast(current, InvocationExpressionSyntax)).ArgumentList
                    Return
                End If

                If TypeOf current Is ObjectCreationExpressionSyntax Then
                    invocableNode = current
                    argumentList = (DirectCast(current, ObjectCreationExpressionSyntax)).ArgumentList
                    Return
                End If

                If TypeOf current Is TypeArgumentListSyntax Then
                    Exit While
                End If

                current = current.Parent
            End While

            invocableNode = Nothing
            argumentList = Nothing
        End Sub

        Protected Overrides Function GetTextChangeAsync(selectedItem As CompletionItem, ch As Char?, cancellationToken As CancellationToken) As Task(Of TextChange?)
            Dim symbolItem = selectedItem
            Dim insertionText = SymbolCompletionItem.GetInsertionText(selectedItem)
            Dim change As TextChange
            If ch.HasValue AndAlso ch.Value = ":"c Then
                change = New TextChange(symbolItem.Span, insertionText.Substring(0, insertionText.Length - s_colonEquals.Length))
            ElseIf ch.HasValue AndAlso ch.Value = "="c Then
                change = New TextChange(selectedItem.Span, insertionText.Substring(0, insertionText.Length - (s_colonEquals.Length - 1)))
            Else
                change = New TextChange(symbolItem.Span, insertionText)
            End If
            Return Task.FromResult(Of TextChange?)(change)
        End Function
    End Class
End Namespace
