' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities
    ''' <summary>
    ''' A helper class that is constructed giving a set of modifiers. It takes these modifiers, and
    ''' breaks them down into separate categories, as well as determines what type of declaration is
    ''' being forced if certain modifiers are present.
    ''' </summary>
    Friend Class ModifierCollectionFacts
        Private ReadOnly _accessibilityKeyword As SyntaxToken
        Private ReadOnly _asyncKeyword As SyntaxToken
        Private ReadOnly _hasProtectedAndFriend As Boolean
        Private ReadOnly _inheritenceKeyword As SyntaxToken
        Private ReadOnly _iteratorKeyword As SyntaxToken
        Private ReadOnly _overridableSharedOrPartialKeyword As SyntaxToken
        Private ReadOnly _overridesOrShadowsKeyword As SyntaxToken
        Private ReadOnly _narrowingOrWideningKeyword As SyntaxToken
        Private ReadOnly _mutabilityOrWithEventsKeyword As SyntaxToken
        Private ReadOnly _defaultKeyword As SyntaxToken
        Private ReadOnly _overloadsKeyword As SyntaxToken
        Private ReadOnly _customKeyword As SyntaxToken
        Private ReadOnly _dimKeyword As SyntaxToken
        Private ReadOnly _sharedKeyword As SyntaxToken

        Private ReadOnly _declarationTypes As PossibleDeclarationTypes

        Public Sub New(syntaxTree As SyntaxTree, position As Integer, token As SyntaxToken, cancellationToken As CancellationToken)
            Debug.Assert(token = syntaxTree.GetTargetToken(position, cancellationToken))

            Dim targetToken = token

            ' First, we compute all possible types that could exist in this location
            _declarationTypes = ComputeAllowableDeclarationTypes(syntaxTree, position, token, cancellationToken)

            Dim defaultMethodFlags = PossibleDeclarationTypes.Method Or PossibleDeclarationTypes.ProtectedMember Or PossibleDeclarationTypes.OverridableMethod
            Dim defaultPropertyFlags = PossibleDeclarationTypes.Property Or PossibleDeclarationTypes.ProtectedMember Or PossibleDeclarationTypes.OverridableMethod

            Do While targetToken.IsModifier() OrElse
                     targetToken.Kind = SyntaxKind.DimKeyword OrElse
                     targetToken.HasMatchingText(SyntaxKind.AsyncKeyword) OrElse
                     targetToken.HasMatchingText(SyntaxKind.IteratorKeyword)

                Select Case targetToken.Kind
                    Case SyntaxKind.PublicKeyword
                        _accessibilityKeyword = targetToken

                    Case SyntaxKind.FriendKeyword
                        If _accessibilityKeyword.IsKind(SyntaxKind.ProtectedKeyword) Then
                            _hasProtectedAndFriend = True
                        End If

                        _accessibilityKeyword = targetToken

                        ' These exclude operators
                        _declarationTypes = _declarationTypes And Not PossibleDeclarationTypes.Operator

                    Case SyntaxKind.ProtectedKeyword, SyntaxKind.PrivateKeyword
                        If targetToken.IsKind(SyntaxKind.ProtectedKeyword) AndAlso _accessibilityKeyword.IsKind(SyntaxKind.FriendKeyword) Then
                            _hasProtectedAndFriend = True
                        End If

                        _accessibilityKeyword = targetToken

                        ' If we're in a namespace or top-level, then we must exclude types
                        If syntaxTree.IsDeclarationContextWithinTypeBlocks(position, token, True, cancellationToken, SyntaxKind.CompilationUnit, SyntaxKind.NamespaceBlock) Then
                            _declarationTypes = _declarationTypes And Not PossibleDeclarationTypes.AllTypes
                        End If

                        ' These exclude operators
                        _declarationTypes = _declarationTypes And Not PossibleDeclarationTypes.Operator

                    Case SyntaxKind.OverridesKeyword
                        _overridesOrShadowsKeyword = targetToken

                        ' Inside of a class, the only things that can have any of these keywords are methods and
                        ' properties. In structs, interfaces, etc, nothing can follow these.
                        If syntaxTree.IsDeclarationContextWithinTypeBlocks(position, token, True, cancellationToken, SyntaxKind.ClassBlock) Then
                            _declarationTypes = _declarationTypes And (defaultMethodFlags Or defaultPropertyFlags)
                        ElseIf syntaxTree.IsDeclarationContextWithinTypeBlocks(position, token, True, cancellationToken, SyntaxKind.StructureBlock) Then
                            ' In this case, we know we can only override things in System.Object, which are all methods
                            _declarationTypes = _declarationTypes And PossibleDeclarationTypes.Method
                        Else
                            _declarationTypes = Nothing
                        End If

                    Case SyntaxKind.MustOverrideKeyword, SyntaxKind.NotOverridableKeyword, SyntaxKind.OverridableKeyword
                        _overridableSharedOrPartialKeyword = targetToken

                        ' Inside of a class, the only things that can have any of these keywords are methods and
                        ' properties. In structs, interfaces, etc, nothing can follow these.
                        If syntaxTree.IsDeclarationContextWithinTypeBlocks(position, token, True, cancellationToken, SyntaxKind.ClassBlock) Then
                            _declarationTypes = _declarationTypes And (defaultMethodFlags Or
                                                                       defaultPropertyFlags Or
                                                                       PossibleDeclarationTypes.IteratorFunction Or
                                                                       PossibleDeclarationTypes.IteratorProperty)
                        Else
                            _declarationTypes = Nothing
                        End If

                    Case SyntaxKind.MustInheritKeyword, SyntaxKind.NotInheritableKeyword
                        _inheritenceKeyword = targetToken
                        _declarationTypes = _declarationTypes And PossibleDeclarationTypes.Class

                    Case SyntaxKind.SharedKeyword
                        _overridableSharedOrPartialKeyword = targetToken
                        _sharedKeyword = targetToken
                        _declarationTypes = _declarationTypes And (PossibleDeclarationTypes.Event Or
                                                                   PossibleDeclarationTypes.Field Or
                                                                   PossibleDeclarationTypes.Method Or
                                                                   PossibleDeclarationTypes.Property Or
                                                                   PossibleDeclarationTypes.Operator Or
                                                                   PossibleDeclarationTypes.IteratorFunction Or
                                                                   PossibleDeclarationTypes.IteratorProperty)

                    Case SyntaxKind.NarrowingKeyword, SyntaxKind.WideningKeyword
                        _narrowingOrWideningKeyword = targetToken
                        _declarationTypes = _declarationTypes And PossibleDeclarationTypes.Operator

                    Case SyntaxKind.ReadOnlyKeyword
                        _mutabilityOrWithEventsKeyword = targetToken
                        _declarationTypes = _declarationTypes And (defaultPropertyFlags Or
                                                                   PossibleDeclarationTypes.Field Or
                                                                   PossibleDeclarationTypes.IteratorProperty)

                    Case SyntaxKind.WriteOnlyKeyword
                        _mutabilityOrWithEventsKeyword = targetToken
                        _declarationTypes = _declarationTypes And defaultPropertyFlags
                        _declarationTypes = _declarationTypes And Not (PossibleDeclarationTypes.IteratorFunction Or
                                                                       PossibleDeclarationTypes.IteratorProperty)

                    Case SyntaxKind.ConstKeyword
                        _mutabilityOrWithEventsKeyword = targetToken
                        _declarationTypes = _declarationTypes And PossibleDeclarationTypes.Field

                    Case SyntaxKind.DefaultKeyword
                        _defaultKeyword = targetToken
                        _declarationTypes = _declarationTypes And (defaultPropertyFlags Or
                                                                   PossibleDeclarationTypes.IteratorProperty)

                    Case SyntaxKind.OverloadsKeyword
                        _overloadsKeyword = targetToken
                        _declarationTypes = _declarationTypes And (defaultMethodFlags Or
                                                                   defaultPropertyFlags Or
                                                                   PossibleDeclarationTypes.ExternalMethod Or
                                                                   PossibleDeclarationTypes.Operator)

                    Case SyntaxKind.WithEventsKeyword
                        _mutabilityOrWithEventsKeyword = targetToken
                        _declarationTypes = _declarationTypes And PossibleDeclarationTypes.Field

                    Case SyntaxKind.CustomKeyword
                        _customKeyword = targetToken
                        _declarationTypes = _declarationTypes And PossibleDeclarationTypes.Event

                    Case SyntaxKind.ShadowsKeyword
                        _overridesOrShadowsKeyword = targetToken
                        _declarationTypes = _declarationTypes And (PossibleDeclarationTypes.Property Or
                                                                   PossibleDeclarationTypes.Method Or
                                                                   PossibleDeclarationTypes.OverridableMethod Or
                                                                   PossibleDeclarationTypes.Delegate Or
                                                                   PossibleDeclarationTypes.Event Or
                                                                   PossibleDeclarationTypes.IteratorFunction Or
                                                                   PossibleDeclarationTypes.IteratorProperty)

                    Case SyntaxKind.PartialKeyword
                        _overridableSharedOrPartialKeyword = targetToken
                        _declarationTypes = _declarationTypes And (PossibleDeclarationTypes.Method Or
                                                                   PossibleDeclarationTypes.Class Or
                                                                   PossibleDeclarationTypes.Structure Or
                                                                   PossibleDeclarationTypes.Interface Or
                                                                   PossibleDeclarationTypes.Module)

                    Case SyntaxKind.DimKeyword
                        _dimKeyword = targetToken
                        _declarationTypes = _declarationTypes And PossibleDeclarationTypes.Field

                    Case SyntaxKind.IteratorKeyword
                        _iteratorKeyword = targetToken
                        _declarationTypes = _declarationTypes And (defaultMethodFlags Or
                                                                   defaultPropertyFlags Or
                                                                   PossibleDeclarationTypes.IteratorFunction Or
                                                                   PossibleDeclarationTypes.IteratorProperty)

                    Case SyntaxKind.AsyncKeyword
                        _asyncKeyword = targetToken
                        _declarationTypes = _declarationTypes And defaultMethodFlags

                    Case Else
                        If targetToken.HasMatchingText(SyntaxKind.AsyncKeyword) Then
                            ' Contextual Async keyword
                            _asyncKeyword = targetToken
                            _declarationTypes = _declarationTypes And defaultMethodFlags
                        ElseIf targetToken.HasMatchingText(SyntaxKind.IteratorKeyword) Then
                            ' Contextual Iterator keyword
                            _iteratorKeyword = targetToken
                            _declarationTypes = _declarationTypes And (defaultMethodFlags Or
                                                                       defaultPropertyFlags Or
                                                                       PossibleDeclarationTypes.IteratorFunction Or
                                                                       PossibleDeclarationTypes.IteratorProperty)
                        Else
                            Throw New InvalidOperationException("Unhandled modifier. Every modifier needs to be processed.")
                        End If
                End Select

                targetToken = targetToken.GetPreviousToken()
            Loop
        End Sub

        Public Function CouldApplyToOneOf(declarationTypes As PossibleDeclarationTypes) As Boolean
            Return (declarationTypes And _declarationTypes) <> 0
        End Function

        Public ReadOnly Property AccessibilityKeyword As SyntaxToken
            Get
                Return _accessibilityKeyword
            End Get
        End Property

        Public ReadOnly Property AsyncKeyword As SyntaxToken
            Get
                Return _asyncKeyword
            End Get
        End Property

        Public ReadOnly Property IteratorKeyword As SyntaxToken
            Get
                Return _iteratorKeyword
            End Get
        End Property

        Public ReadOnly Property HasProtectedAndFriend As Boolean
            Get
                Return _hasProtectedAndFriend
            End Get
        End Property

        Public ReadOnly Property OverridableSharedOrPartialKeyword As SyntaxToken
            Get
                Return _overridableSharedOrPartialKeyword
            End Get
        End Property

        Public ReadOnly Property OverridesOrShadowsKeyword As SyntaxToken
            Get
                Return _overridesOrShadowsKeyword
            End Get
        End Property

        Public ReadOnly Property InheritenceKeyword As SyntaxToken
            Get
                Return _inheritenceKeyword
            End Get
        End Property

        Public ReadOnly Property DefaultKeyword As SyntaxToken
            Get
                Return _defaultKeyword
            End Get
        End Property

        Public ReadOnly Property NarrowingOrWideningKeyword As SyntaxToken
            Get
                Return _narrowingOrWideningKeyword
            End Get
        End Property

        Public ReadOnly Property OverloadsKeyword As SyntaxToken
            Get
                Return _overloadsKeyword
            End Get
        End Property

        Public ReadOnly Property MutabilityOrWithEventsKeyword As SyntaxToken
            Get
                Return _mutabilityOrWithEventsKeyword
            End Get
        End Property

        Public ReadOnly Property CustomKeyword As SyntaxToken
            Get
                Return _customKeyword
            End Get
        End Property

        Public ReadOnly Property DimKeyword As SyntaxToken
            Get
                Return _dimKeyword
            End Get
        End Property

        Public ReadOnly Property SharedKeyword As SyntaxToken
            Get
                Return _sharedKeyword
            End Get
        End Property

        Private Function ComputeAllowableDeclarationTypes(syntaxTree As SyntaxTree, position As Integer, token As SyntaxToken, cancellationToken As CancellationToken) As PossibleDeclarationTypes
            Dim declarationTypes As PossibleDeclarationTypes

            If syntaxTree.IsTypeMemberDeclarationKeywordContext(position, token, cancellationToken) Then
                declarationTypes = declarationTypes Or
                    PossibleDeclarationTypes.Event Or
                    PossibleDeclarationTypes.ExternalMethod Or
                    PossibleDeclarationTypes.Field Or
                    PossibleDeclarationTypes.Method Or
                    PossibleDeclarationTypes.Operator Or
                    PossibleDeclarationTypes.Property Or
                    PossibleDeclarationTypes.Accessor Or
                    PossibleDeclarationTypes.IteratorFunction Or
                    PossibleDeclarationTypes.IteratorProperty

                If syntaxTree.IsDeclarationContextWithinTypeBlocks(position, token, True, cancellationToken, SyntaxKind.ClassBlock) Then
                    declarationTypes = declarationTypes Or
                        PossibleDeclarationTypes.ProtectedMember Or
                        PossibleDeclarationTypes.OverridableMethod
                End If
            End If

            If syntaxTree.IsInterfaceMemberDeclarationKeywordContext(position, token, cancellationToken) Then
                declarationTypes = declarationTypes Or
                    PossibleDeclarationTypes.Event Or
                    PossibleDeclarationTypes.Method Or
                    PossibleDeclarationTypes.Property
            End If

            If syntaxTree.IsTypeDeclarationKeywordContext(position, token, cancellationToken) Then
                declarationTypes = declarationTypes Or PossibleDeclarationTypes.AllTypes
            End If

            Return declarationTypes
        End Function
    End Class
End Namespace
