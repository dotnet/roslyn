' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageService
    Friend Class VisualBasicAccessibilityFacts
        Implements IAccessibilityFacts

        Public Shared ReadOnly Instance As IAccessibilityFacts = New VisualBasicAccessibilityFacts()

        Private Sub New()
        End Sub

        Public Function CanHaveAccessibility(declaration As SyntaxNode, Optional ignoreDeclarationModifiers As Boolean = False) As Boolean Implements IAccessibilityFacts.CanHaveAccessibility
            Select Case declaration.Kind
                Case SyntaxKind.ClassBlock,
                     SyntaxKind.ClassStatement,
                     SyntaxKind.StructureBlock,
                     SyntaxKind.StructureStatement,
                     SyntaxKind.InterfaceBlock,
                     SyntaxKind.InterfaceStatement,
                     SyntaxKind.EnumBlock,
                     SyntaxKind.EnumStatement,
                     SyntaxKind.ModuleBlock,
                     SyntaxKind.ModuleStatement,
                     SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement,
                     SyntaxKind.FieldDeclaration,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.SubBlock,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.SubStatement,
                     SyntaxKind.PropertyBlock,
                     SyntaxKind.PropertyStatement,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.OperatorStatement,
                     SyntaxKind.EventBlock,
                     SyntaxKind.EventStatement,
                     SyntaxKind.GetAccessorBlock,
                     SyntaxKind.GetAccessorStatement,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.SetAccessorStatement,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorBlock,
                     SyntaxKind.RaiseEventAccessorStatement
                    Return True

                Case SyntaxKind.ConstructorBlock,
                     SyntaxKind.SubNewStatement
                    ' Shared constructor cannot have modifiers in VB.
                    ' Module constructors are implicitly Shared and can't have accessibility modifier.
                    Return Not declaration.GetModifiers().Any(SyntaxKind.SharedKeyword) AndAlso
                        Not declaration.Parent.IsKind(SyntaxKind.ModuleBlock)

                Case SyntaxKind.ModifiedIdentifier
                    Return If(IsChildOf(declaration, SyntaxKind.VariableDeclarator),
                              CanHaveAccessibility(declaration.Parent),
                              False)

                Case SyntaxKind.VariableDeclarator
                    Return If(IsChildOfVariableDeclaration(declaration),
                              CanHaveAccessibility(declaration.Parent),
                              False)

                Case Else
                    Return False
            End Select
        End Function

        Private Shared Function IsChildOf(node As SyntaxNode, kind As SyntaxKind) As Boolean
            Return VisualBasicSyntaxFacts.IsChildOf(node, kind)
        End Function

        Private Shared Function IsChildOfVariableDeclaration(node As SyntaxNode) As Boolean
            Return VisualBasicSyntaxFacts.IsChildOfVariableDeclaration(node)
        End Function

        Public Function GetAccessibility(declaration As SyntaxNode) As Accessibility Implements IAccessibilityFacts.GetAccessibility
            If Not CanHaveAccessibility(declaration) Then
                Return Accessibility.NotApplicable
            End If

            Dim tokens = GetModifierTokens(declaration)
            Dim acc As Accessibility
            Dim mods As Modifiers
            Dim isDefault As Boolean
            GetAccessibilityAndModifiers(tokens, acc, mods, isDefault)
            Return acc
        End Function

        Public Shared Function GetModifierTokens(declaration As SyntaxNode) As SyntaxTokenList
            Select Case declaration.Kind
                Case SyntaxKind.ClassBlock
                    Return DirectCast(declaration, ClassBlockSyntax).BlockStatement.Modifiers
                Case SyntaxKind.ClassStatement
                    Return DirectCast(declaration, ClassStatementSyntax).Modifiers
                Case SyntaxKind.StructureBlock
                    Return DirectCast(declaration, StructureBlockSyntax).BlockStatement.Modifiers
                Case SyntaxKind.StructureStatement
                    Return DirectCast(declaration, StructureStatementSyntax).Modifiers
                Case SyntaxKind.InterfaceBlock
                    Return DirectCast(declaration, InterfaceBlockSyntax).BlockStatement.Modifiers
                Case SyntaxKind.InterfaceStatement
                    Return DirectCast(declaration, InterfaceStatementSyntax).Modifiers
                Case SyntaxKind.EnumBlock
                    Return DirectCast(declaration, EnumBlockSyntax).EnumStatement.Modifiers
                Case SyntaxKind.EnumStatement
                    Return DirectCast(declaration, EnumStatementSyntax).Modifiers
                Case SyntaxKind.ModuleBlock
                    Return DirectCast(declaration, ModuleBlockSyntax).ModuleStatement.Modifiers
                Case SyntaxKind.ModuleStatement
                    Return DirectCast(declaration, ModuleStatementSyntax).Modifiers
                Case SyntaxKind.DelegateFunctionStatement,
                     SyntaxKind.DelegateSubStatement
                    Return DirectCast(declaration, DelegateStatementSyntax).Modifiers
                Case SyntaxKind.FieldDeclaration
                    Return DirectCast(declaration, FieldDeclarationSyntax).Modifiers
                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.SubBlock
                    Return DirectCast(declaration, MethodBlockSyntax).BlockStatement.Modifiers
                Case SyntaxKind.ConstructorBlock
                    Return DirectCast(declaration, ConstructorBlockSyntax).BlockStatement.Modifiers
                Case SyntaxKind.FunctionStatement,
                     SyntaxKind.SubStatement
                    Return DirectCast(declaration, MethodStatementSyntax).Modifiers
                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression
                    Return DirectCast(declaration, MultiLineLambdaExpressionSyntax).SubOrFunctionHeader.Modifiers
                Case SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    Return DirectCast(declaration, SingleLineLambdaExpressionSyntax).SubOrFunctionHeader.Modifiers
                Case SyntaxKind.SubNewStatement
                    Return DirectCast(declaration, SubNewStatementSyntax).Modifiers
                Case SyntaxKind.PropertyBlock
                    Return DirectCast(declaration, PropertyBlockSyntax).PropertyStatement.Modifiers
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(declaration, PropertyStatementSyntax).Modifiers
                Case SyntaxKind.OperatorBlock
                    Return DirectCast(declaration, OperatorBlockSyntax).BlockStatement.Modifiers
                Case SyntaxKind.OperatorStatement
                    Return DirectCast(declaration, OperatorStatementSyntax).Modifiers
                Case SyntaxKind.EventBlock
                    Return DirectCast(declaration, EventBlockSyntax).EventStatement.Modifiers
                Case SyntaxKind.EventStatement
                    Return DirectCast(declaration, EventStatementSyntax).Modifiers
                Case SyntaxKind.ModifiedIdentifier
                    If IsChildOf(declaration, SyntaxKind.VariableDeclarator) Then
                        Return GetModifierTokens(declaration.Parent)
                    End If
                Case SyntaxKind.LocalDeclarationStatement
                    Return DirectCast(declaration, LocalDeclarationStatementSyntax).Modifiers
                Case SyntaxKind.VariableDeclarator
                    If IsChildOfVariableDeclaration(declaration) Then
                        Return GetModifierTokens(declaration.Parent)
                    End If
                Case SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                    SyntaxKind.RaiseEventAccessorBlock
                    Return GetModifierTokens(DirectCast(declaration, AccessorBlockSyntax).AccessorStatement)
                Case SyntaxKind.GetAccessorStatement,
                     SyntaxKind.SetAccessorStatement,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorStatement
                    Return DirectCast(declaration, AccessorStatementSyntax).Modifiers
                Case Else
                    Return Nothing
            End Select
        End Function

        Public Shared Sub GetAccessibilityAndModifiers(modifierTokens As SyntaxTokenList, ByRef accessibility As Accessibility, ByRef modifiers As Modifiers, ByRef isDefault As Boolean)
            accessibility = Accessibility.NotApplicable
            modifiers = Modifiers.None
            isDefault = False

            For Each token In modifierTokens
                Select Case token.Kind
                    Case SyntaxKind.DefaultKeyword
                        isDefault = True
                    Case SyntaxKind.PublicKeyword
                        accessibility = Accessibility.Public
                    Case SyntaxKind.PrivateKeyword
                        If accessibility = Accessibility.Protected Then
                            accessibility = Accessibility.ProtectedAndFriend
                        Else
                            accessibility = Accessibility.Private
                        End If
                    Case SyntaxKind.FriendKeyword
                        If accessibility = Accessibility.Protected Then
                            accessibility = Accessibility.ProtectedOrFriend
                        Else
                            accessibility = Accessibility.Friend
                        End If
                    Case SyntaxKind.ProtectedKeyword
                        If accessibility = Accessibility.Friend Then
                            accessibility = Accessibility.ProtectedOrFriend
                        ElseIf accessibility = Accessibility.Private Then
                            accessibility = Accessibility.ProtectedAndFriend
                        Else
                            accessibility = Accessibility.Protected
                        End If
                    Case SyntaxKind.MustInheritKeyword, SyntaxKind.MustOverrideKeyword
                        modifiers = modifiers Or Modifiers.Abstract
                    Case SyntaxKind.ShadowsKeyword
                        modifiers = modifiers Or Modifiers.[New]
                    Case SyntaxKind.OverridesKeyword
                        modifiers = modifiers Or Modifiers.Override
                    Case SyntaxKind.OverridableKeyword
                        modifiers = modifiers Or Modifiers.Virtual
                    Case SyntaxKind.SharedKeyword
                        modifiers = modifiers Or Modifiers.Static
                    Case SyntaxKind.AsyncKeyword
                        modifiers = modifiers Or Modifiers.Async
                    Case SyntaxKind.ConstKeyword
                        modifiers = modifiers Or Modifiers.Const
                    Case SyntaxKind.ReadOnlyKeyword
                        modifiers = modifiers Or Modifiers.ReadOnly
                    Case SyntaxKind.WriteOnlyKeyword
                        modifiers = modifiers Or Modifiers.WriteOnly
                    Case SyntaxKind.NotInheritableKeyword, SyntaxKind.NotOverridableKeyword
                        modifiers = modifiers Or Modifiers.Sealed
                    Case SyntaxKind.WithEventsKeyword
                        modifiers = modifiers Or Modifiers.WithEvents
                    Case SyntaxKind.PartialKeyword
                        modifiers = modifiers Or Modifiers.Partial
                End Select
            Next
        End Sub
    End Class
End Namespace
