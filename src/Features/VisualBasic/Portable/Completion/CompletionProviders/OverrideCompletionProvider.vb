' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Friend Class OverrideCompletionProvider
        Inherits AbstractOverrideCompletionProvider

        Private _isFunction As Boolean
        Private _isSub As Boolean
        Private _isProperty As Boolean

        Public Sub New()
        End Sub

        Protected Overrides Function GetSyntax(commonSyntaxToken As SyntaxToken) As SyntaxNode
            Dim token = CType(commonSyntaxToken, SyntaxToken)

            Dim propertyBlock = token.GetAncestor(Of PropertyBlockSyntax)()
            If propertyBlock IsNot Nothing Then
                Return propertyBlock
            End If

            Dim methodBlock = token.GetAncestor(Of MethodBlockBaseSyntax)()
            If methodBlock IsNot Nothing Then
                Return methodBlock
            End If

            Return token.GetAncestor(Of MethodStatementSyntax)()
        End Function

        Protected Overrides Function GetToken(completionItem As CompletionItem, syntaxTree As SyntaxTree, cancellationToken As CancellationToken) As SyntaxToken
            Dim tokenSpanEnd = MemberInsertionCompletionItem.GetTokenSpanEnd(completionItem)
            Return syntaxTree.FindTokenOnLeftOfPosition(tokenSpanEnd, cancellationToken)
        End Function


        Public Overrides Function FindStartingToken(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As SyntaxToken
            Dim token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
            Return token.GetPreviousTokenIfTouchingWord(position)
        End Function

        Friend Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options)
        End Function

        Public Overrides Function TryDetermineModifiers(startToken As SyntaxToken,
                                                        text As SourceText, startLine As Integer,
                                                        ByRef seenAccessibility As Accessibility,
                                                        ByRef modifiers As DeclarationModifiers) As Boolean

            Dim token = CType(startToken, SyntaxToken)
            modifiers = New DeclarationModifiers()
            seenAccessibility = Accessibility.NotApplicable
            Dim overridesToken = New SyntaxToken()
            Dim isMustOverride = False
            Dim isNotOverridable = False
            Me._isSub = False
            Me._isFunction = False
            Me._isProperty = False

            Do While IsOnStartLine(token.SpanStart, text, startLine)
                Select Case token.Kind
                    Case SyntaxKind.OverridesKeyword
                        overridesToken = token
                    Case SyntaxKind.MustOverrideKeyword
                        isMustOverride = True
                    Case SyntaxKind.NotOverridableKeyword
                        isNotOverridable = True
                    Case SyntaxKind.FunctionKeyword
                        _isFunction = True
                    Case SyntaxKind.PropertyKeyword
                        _isProperty = True
                    Case SyntaxKind.SubKeyword
                        _isSub = True

                    ' Filter on accessibility by keeping the first one that we see
                    Case SyntaxKind.PublicKeyword
                        If seenAccessibility = Accessibility.NotApplicable Then
                            seenAccessibility = Accessibility.Public
                        End If

                    Case SyntaxKind.FriendKeyword
                        If seenAccessibility = Accessibility.NotApplicable Then
                            seenAccessibility = Accessibility.Internal
                        End If

                        ' If we see Friend AND Protected, assume Friend Protected
                        If seenAccessibility = Accessibility.Protected Then
                            seenAccessibility = Accessibility.ProtectedOrInternal
                        End If

                    Case SyntaxKind.ProtectedKeyword
                        If seenAccessibility = Accessibility.NotApplicable Then
                            seenAccessibility = Accessibility.Protected
                        End If

                        ' If we see Protected and Friend, assume Protected Friend
                        If seenAccessibility = Accessibility.Internal Then
                            seenAccessibility = Accessibility.ProtectedOrInternal
                        End If

                    Case Else
                        ' If we see anything else, give up
                        Return False
                End Select

                Dim previousToken = token.GetPreviousToken()

                ' Consume only modifiers on the same line
                If previousToken.Kind = SyntaxKind.None OrElse Not IsOnStartLine(previousToken.SpanStart, text, startLine) Then
                    Exit Do
                End If

                token = previousToken
            Loop

            modifiers = New DeclarationModifiers(isAbstract:=isMustOverride, isOverride:=True, isSealed:=isNotOverridable)
            Return overridesToken.Kind = SyntaxKind.OverridesKeyword AndAlso IsOnStartLine(overridesToken.Parent.SpanStart, text, startLine)
        End Function

        Public Overrides Function DetermineReturnTypeAsync(
                document As Document,
                startToken As SyntaxToken,
                cancellationToken As CancellationToken) As Task(Of (succeeded As Boolean, returnType As SymbolAndProjectId(Of ITypeSymbol), nextToken As SyntaxToken))

            Return Task.FromResult(Of (succeeded As Boolean, returnType As SymbolAndProjectId(Of ITypeSymbol), nextToken As SyntaxToken))(
                (True, Nothing, Nothing))
        End Function

        Public Overrides Function FilterOverridesAsync(
                solution As Solution,
                members As ImmutableArray(Of SymbolAndProjectId),
                returnType As SymbolAndProjectId(Of ITypeSymbol),
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of SymbolAndProjectId))

            ' Start by removing Finalize(), which we never want to show.
            Dim finalizeMethod = members.Where(Function(m) TypeOf m.Symbol Is IMethodSymbol).
                                         Where(Function(x) x.Symbol.Name = "Finalize" AndAlso OverridesObjectMethod(DirectCast(x.Symbol, IMethodSymbol))).
                                         SingleOrDefault()

            If finalizeMethod.Symbol IsNot Nothing Then
                members = members.Remove(finalizeMethod)
            End If

            If Me._isFunction Then
                ' Function: look for non-void return types
                Dim filteredMembers = members.WhereAsArray(
                    Function(s) TypeOf s.Symbol Is IMethodSymbol AndAlso Not DirectCast(s.Symbol, IMethodSymbol).ReturnsVoid)

                If filteredMembers.Any Then
                    Return Task.FromResult(filteredMembers)
                End If
            ElseIf Me._isProperty Then
                ' Property: return properties
                Dim filteredMembers = members.WhereAsArray(Function(m) m.Symbol.Kind = SymbolKind.Property)
                If filteredMembers.Any Then
                    Return Task.FromResult(filteredMembers)
                End If
            ElseIf Me._isSub Then
                ' Sub: look for void return types
                Dim filteredMembers = members.WhereAsArray(
                    Function(s) TypeOf s.Symbol Is IMethodSymbol AndAlso DirectCast(s.Symbol, IMethodSymbol).ReturnsVoid)
                If filteredMembers.Any Then
                    Return Task.FromResult(filteredMembers)
                End If
            End If

            Return Task.FromResult(members.WhereAsArray(Function(m) Not m.Symbol.IsKind(SymbolKind.Event)))
        End Function

        Private Function OverridesObjectMethod(method As IMethodSymbol) As Boolean
            Dim overriddenMember = method
            Do While overriddenMember.OverriddenMethod IsNot Nothing
                overriddenMember = overriddenMember.OverriddenMethod
            Loop

            If overriddenMember.ContainingType.SpecialType = SpecialType.System_Object Then
                Return True
            End If

            Return False
        End Function

        Protected Overrides Function GetTargetCaretPosition(caretTarget As SyntaxNode) As Integer
            Dim node = DirectCast(caretTarget, SyntaxNode)

            ' MustOverride Sub | MustOverride Function: move to end of line
            Dim methodStatement = TryCast(node, MethodStatementSyntax)
            If methodStatement IsNot Nothing Then
                Return methodStatement.GetLocation().SourceSpan.End
            End If

            Dim methodBlock = TryCast(node, MethodBlockBaseSyntax)
            If methodBlock IsNot Nothing Then
                Dim lastStatement = methodBlock.Statements.LastOrDefault()
                If lastStatement IsNot Nothing Then
                    Return lastStatement.GetLocation().SourceSpan.End
                End If
            End If

            Dim propertyBlock = TryCast(node, PropertyBlockSyntax)
            If propertyBlock IsNot Nothing Then
                Dim firstAccessor = propertyBlock.Accessors.FirstOrDefault()
                If firstAccessor IsNot Nothing Then
                    Dim lastAccessorStatement = firstAccessor.Statements.LastOrDefault()
                    If lastAccessorStatement IsNot Nothing Then
                        Return lastAccessorStatement.GetLocation().SourceSpan.End
                    End If
                End If
            End If

            Return -1
        End Function
    End Class
End Namespace
