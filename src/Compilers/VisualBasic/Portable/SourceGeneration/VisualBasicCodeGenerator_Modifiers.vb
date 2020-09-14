' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.SourceGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
    Partial Friend Module VisualBasicCodeGenerator
        Private Function GenerateModifiers(
                isType As Boolean,
                declaredAccessibility As Accessibility,
                symbolModifiers As SymbolModifiers) As SyntaxTokenList

            Using temp = GetArrayBuilder(Of SyntaxToken)()
                Dim builder = temp.Builder
                Select Case declaredAccessibility
                    Case Accessibility.Private
                        builder.Add(Token(SyntaxKind.PrivateKeyword))
                    Case Accessibility.ProtectedAndFriend
                        builder.Add(Token(SyntaxKind.PrivateKeyword))
                        builder.Add(Token(SyntaxKind.ProtectedKeyword))
                    Case Accessibility.Protected
                        builder.Add(Token(SyntaxKind.ProtectedKeyword))
                    Case Accessibility.Friend
                        builder.Add(Token(SyntaxKind.FriendKeyword))
                    Case Accessibility.ProtectedOrFriend
                        builder.Add(Token(SyntaxKind.ProtectedKeyword))
                        builder.Add(Token(SyntaxKind.FriendKeyword))
                    Case Accessibility.Public
                        builder.Add(Token(SyntaxKind.PublicKeyword))
                End Select

                If symbolModifiers.HasFlag(SymbolModifiers.Static) Then
                    builder.Add(Token(SyntaxKind.SharedKeyword))
                End If

                If symbolModifiers.HasFlag(SymbolModifiers.Abstract) Then
                    If isType Then
                        builder.Add(Token(SyntaxKind.MustInheritKeyword))
                    Else
                        builder.Add(Token(SyntaxKind.MustOverrideKeyword))
                    End If
                End If

                If symbolModifiers.HasFlag(SymbolModifiers.New) Then
                    builder.Add(Token(SyntaxKind.ShadowsKeyword))
                End If

                If symbolModifiers.HasFlag(SymbolModifiers.ReadOnly) Then
                    builder.Add(Token(SyntaxKind.ReadOnlyKeyword))
                End If

                If symbolModifiers.HasFlag(SymbolModifiers.Virtual) Then
                    builder.Add(Token(SyntaxKind.OverridableKeyword))
                End If

                If symbolModifiers.HasFlag(SymbolModifiers.Override) Then
                    builder.Add(Token(SyntaxKind.OverridesKeyword))
                End If

                If symbolModifiers.HasFlag(SymbolModifiers.Sealed) Then
                    builder.Add(Token(SyntaxKind.NotOverridableKeyword))
                End If

                If symbolModifiers.HasFlag(SymbolModifiers.Const) Then
                    builder.Add(Token(SyntaxKind.ConstKeyword))
                End If

                If symbolModifiers.HasFlag(SymbolModifiers.WithEvents) Then
                    builder.Add(Token(SyntaxKind.WithEventsKeyword))
                End If

                If symbolModifiers.HasFlag(SymbolModifiers.Partial) Then
                    builder.Add(Token(SyntaxKind.PartialKeyword))
                End If

                If symbolModifiers.HasFlag(SymbolModifiers.Async) Then
                    builder.Add(Token(SyntaxKind.AsyncKeyword))
                End If

                If symbolModifiers.HasFlag(SymbolModifiers.WriteOnly) Then
                    builder.Add(Token(SyntaxKind.WriteOnlyKeyword))
                End If

                If symbolModifiers.HasFlag(SymbolModifiers.Params) Then
                    builder.Add(Token(SyntaxKind.ParamArrayKeyword))
                End If

                Return TokenList(builder)
            End Using
        End Function
    End Module
End Namespace
