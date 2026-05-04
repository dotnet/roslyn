' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportCompletionProvider(NameOf(OverrideCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(CompletionListTagCompletionProvider))>
    <[Shared]>
    Friend Class OverrideCompletionProvider
        Inherits AbstractOverrideCompletionProvider

        Private _isFunction As Boolean
        Private _isSub As Boolean
        Private _isProperty As Boolean

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

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

        Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            Return CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options)
        End Function

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = CompletionUtilities.SpaceTriggerChar

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

        Public Overrides Function TryDetermineReturnType(startToken As SyntaxToken,
                                                         semanticModel As SemanticModel,
                                                         cancellationToken As CancellationToken,
                                                         ByRef returnType As ITypeSymbol, ByRef nextToken As SyntaxToken) As Boolean
            nextToken = startToken
            returnType = Nothing

            Return True
        End Function

        Public Overrides Function FilterOverrides(members As ImmutableArray(Of ISymbol),
                                                  returnType As ITypeSymbol) As ImmutableArray(Of ISymbol)
            ' Start by removing Finalize(), which we never want to show.
            Dim finalizeMethod = members.OfType(Of IMethodSymbol)().Where(Function(x) x.Name = "Finalize" AndAlso OverridesObjectMethod(x)).SingleOrDefault()
            If finalizeMethod IsNot Nothing Then
                members = members.Remove(finalizeMethod)
            End If

            If Me._isFunction Then
                ' Function: look for non-void return types
                Dim filteredMembers = members.OfType(Of IMethodSymbol)().Where(Function(m) Not m.ReturnsVoid)
                If filteredMembers.Any Then
                    Return ImmutableArray(Of ISymbol).CastUp(filteredMembers.ToImmutableArray())
                End If
            ElseIf Me._isProperty Then
                ' Property: return properties
                Dim filteredMembers = members.Where(Function(m) m.Kind = SymbolKind.Property)
                If filteredMembers.Any Then
                    Return filteredMembers.ToImmutableArray()
                End If
            ElseIf Me._isSub Then
                ' Sub: look for void return types
                Dim filteredMembers = members.OfType(Of IMethodSymbol)().Where(Function(m) m.ReturnsVoid)
                If filteredMembers.Any Then
                    Return ImmutableArray(Of ISymbol).CastUp(filteredMembers.ToImmutableArray())
                End If
            End If

            Return members.WhereAsArray(Function(m) Not m.IsKind(SymbolKind.Event))
        End Function

        Private Shared Function OverridesObjectMethod(method As IMethodSymbol) As Boolean
            Dim overriddenMember = method
            Do While overriddenMember.OverriddenMethod IsNot Nothing
                overriddenMember = overriddenMember.OverriddenMethod
            Loop

            If overriddenMember.ContainingType.SpecialType = SpecialType.System_Object Then
                Return True
            End If

            Return False
        End Function

        Protected Overrides Function GetTargetSelectionSpan(caretTarget As SyntaxNode) As TextSpan
            Dim node = DirectCast(caretTarget, SyntaxNode)

            ' MustOverride Sub | MustOverride Function: move to end of line
            Dim methodStatement = TryCast(node, MethodStatementSyntax)
            If methodStatement IsNot Nothing Then
                Return New TextSpan(methodStatement.GetLocation().SourceSpan.End, 0)
            End If

            Dim methodBlock = TryCast(node, MethodBlockBaseSyntax)
            If methodBlock IsNot Nothing Then
                Dim lastStatement = methodBlock.Statements.LastOrDefault()
                If lastStatement IsNot Nothing Then
                    Return New TextSpan(lastStatement.GetLocation().SourceSpan.End, 0)
                End If
            End If

            Dim propertyBlock = TryCast(node, PropertyBlockSyntax)
            If propertyBlock IsNot Nothing Then
                Dim firstAccessor = propertyBlock.Accessors.FirstOrDefault()
                If firstAccessor IsNot Nothing Then
                    Dim lastAccessorStatement = firstAccessor.Statements.LastOrDefault()
                    If lastAccessorStatement IsNot Nothing Then
                        Return New TextSpan(lastAccessorStatement.GetLocation().SourceSpan.End, 0)
                    End If
                End If
            End If

            Return New TextSpan(0, 0)
        End Function
    End Class
End Namespace
