' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.FindSymbols
    Friend Module FindSymbolsUtilities
        Public Function GetDeclaredSymbolInfoKind(typeBlock As TypeBlockSyntax) As DeclaredSymbolInfoKind
            Select Case typeBlock.Kind()
                Case SyntaxKind.ClassBlock
                    Return DeclaredSymbolInfoKind.Class
                Case SyntaxKind.InterfaceBlock
                    Return DeclaredSymbolInfoKind.Interface
                Case SyntaxKind.ModuleBlock
                    Return DeclaredSymbolInfoKind.Module
                Case Else
                    Return DeclaredSymbolInfoKind.Struct
            End Select
        End Function

        Public Function GetAccessibility(container As SyntaxNode, node As StatementSyntax, modifiers As SyntaxTokenList) As Accessibility
            Dim sawFriend = False

            For Each modifier In modifiers
                Select Case modifier.Kind()
                    Case SyntaxKind.PublicKeyword : Return Accessibility.Public
                    Case SyntaxKind.PrivateKeyword : Return Accessibility.Private
                    Case SyntaxKind.ProtectedKeyword : Return Accessibility.Protected
                    Case SyntaxKind.FriendKeyword
                        sawFriend = True
                        Continue For
                End Select
            Next

            If sawFriend Then
                Return Accessibility.Internal
            End If

            ' No accessibility modifiers
            Select Case container.Kind()
                Case SyntaxKind.ClassBlock
                    ' In a class, fields and shared-constructors are private by default,
                    ' everything Else Is Public
                    If node.Kind() = SyntaxKind.FieldDeclaration Then
                        Return Accessibility.Private
                    End If

                    If node.Kind() = SyntaxKind.SubNewStatement AndAlso
                       DirectCast(node, SubNewStatementSyntax).Modifiers.Any(SyntaxKind.SharedKeyword) Then
                        Return Accessibility.Private
                    End If

                    Return Accessibility.Public
                Case SyntaxKind.StructureBlock, SyntaxKind.InterfaceBlock, SyntaxKind.ModuleBlock
                    ' Everything in a struct/interface/module is public
                    Return Accessibility.Public
            End Select

            ' Otherwise, it's internal
            Return Accessibility.Internal
        End Function
    End Module
End Namespace
