' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Module EnumMemberGenerator

        Friend Function AddEnumMemberTo(
            destination As EnumBlockSyntax,
            enumMember As IFieldSymbol,
            options As CodeGenerationOptions) As EnumBlockSyntax

            ' We never generate the special enum backing field.
            If enumMember.Name = WellKnownMemberNames.EnumBackingFieldName Then
                Return destination
            End If

            Dim members = New List(Of StatementSyntax)()
            members.AddRange(destination.Members)
            Dim member = GenerateEnumMemberDeclaration(enumMember, destination, options)

            members.Add(member)
            Dim leadingTrivia = destination.EndEnumStatement.GetLeadingTrivia()

            Return destination.WithMembers(SyntaxFactory.List(Of StatementSyntax)(members)).
                               WithEndEnumStatement(If(destination.EndEnumStatement.IsMissing, SyntaxFactory.EndEnumStatement(), destination.EndEnumStatement))
        End Function

        Public Function GenerateEnumMemberDeclaration(enumMember As IFieldSymbol,
                                                             enumDeclarationOpt As EnumBlockSyntax,
                                                             options As CodeGenerationOptions) As EnumMemberDeclarationSyntax
            Dim reusableSyntax = GetReuseableSyntaxNodeForSymbol(Of EnumMemberDeclarationSyntax)(enumMember, options)
            If reusableSyntax IsNot Nothing Then
                Return reusableSyntax
            End If

            Dim value = CreateEnumMemberValue(enumDeclarationOpt, enumMember)
            Dim member = SyntaxFactory.EnumMemberDeclaration(enumMember.Name.ToIdentifierToken()) _
                               .WithInitializer(If(value Is Nothing, Nothing, SyntaxFactory.EqualsValue(value:=value)))

            Return AddCleanupAnnotationsTo(ConditionallyAddDocumentationCommentTo(member, enumMember, options))
        End Function

        Private Function CreateEnumMemberValue(destinationOpt As EnumBlockSyntax, enumMember As IFieldSymbol) As ExpressionSyntax
            Dim valueOpt =
                If(TypeOf enumMember.ConstantValue Is IConvertible,
                    CType(IntegerUtilities.ToInt64(enumMember.ConstantValue), Long?),
                    Nothing)

            If valueOpt Is Nothing Then
                Return Nothing
            End If

            Dim value = valueOpt.Value

            If destinationOpt IsNot Nothing Then
                If destinationOpt.Members.Count = 0 Then
                    If value = 0 Then
                        Return Nothing
                    End If
                Else
                    ' if nothing in the enum has any initializers and our value is appropriate for 
                    ' the end, then don't generate a value.
                    If destinationOpt.Members.Count = value AndAlso
                        destinationOpt.Members.OfType(Of EnumMemberDeclarationSyntax).All(Function(m) m.Initializer Is Nothing) Then
                        Return Nothing
                    End If

                    ' Existing members, try to stay consistent with their style.
                    Dim lastMember = destinationOpt.Members.OfType(Of EnumMemberDeclarationSyntax).LastOrDefault(Function(m) m.Initializer IsNot Nothing)

                    If lastMember IsNot Nothing Then

                        Dim lastExpression = lastMember.Initializer.Value
                        If lastExpression.VisualBasicKind = SyntaxKind.LeftShiftExpression AndAlso IntegerUtilities.HasOneBitSet(value) Then
                            Dim binaryExpression = DirectCast(lastExpression, BinaryExpressionSyntax)
                            If binaryExpression.Left.VisualBasicKind = SyntaxKind.NumericLiteralExpression Then
                                Dim numericLiteral = DirectCast(binaryExpression.Left, LiteralExpressionSyntax)
                                If numericLiteral.Token.ValueText = "1" Then
                                    ' The user is left shifting ones, stick with that pattern
                                    Dim shiftValue = IntegerUtilities.LogBase2(value)
                                    Return SyntaxFactory.LeftShiftExpression(
                                    left:=SyntaxFactory.NumericLiteralExpression(SyntaxFactory.IntegerLiteralToken("1", LiteralBase.Decimal, TypeCharacter.None, 1)),
                                    right:=SyntaxFactory.NumericLiteralExpression(SyntaxFactory.IntegerLiteralToken(shiftValue.ToString(), LiteralBase.Decimal, TypeCharacter.None, IntegerUtilities.ToUnsigned(shiftValue))))
                                End If
                            End If
                        ElseIf lastExpression.VisualBasicKind = SyntaxKind.NumericLiteralExpression Then
                            Dim numericLiteral = DirectCast(lastExpression, LiteralExpressionSyntax)
                            Dim numericToken = numericLiteral.Token
                            Dim numericText = numericToken.ToString()
                            If numericText.StartsWith("&h") OrElse numericText.StartsWith("&H") Then
                                Dim firstTwoChars = numericText.Substring(0, 2)

                                If (numericText.EndsWith("US") OrElse numericText.EndsWith("us") OrElse
                                numericText.EndsWith("uS") OrElse numericText.EndsWith("Us")) AndAlso
                               value >= UShort.MinValue AndAlso value <= UShort.MaxValue Then
                                    Dim ushortValue = CUShort(value)

                                    Dim lastTwoChars = numericText.Substring(numericText.Length - 2, 2)
                                    Return SyntaxFactory.NumericLiteralExpression(
                                    SyntaxFactory.IntegerLiteralToken(firstTwoChars + ushortValue.ToString("X") + lastTwoChars, LiteralBase.Hexadecimal, TypeCharacter.UShortLiteral, IntegerUtilities.ToUnsigned(ushortValue)))
                                ElseIf (numericText.EndsWith("S") OrElse numericText.EndsWith("s")) AndAlso
                                   value >= Short.MinValue AndAlso value <= Short.MaxValue Then
                                    Dim shortValue = CShort(value)
                                    Return SyntaxFactory.NumericLiteralExpression(
                                    SyntaxFactory.IntegerLiteralToken(firstTwoChars + shortValue.ToString("X") + numericText.Last(), LiteralBase.Hexadecimal, TypeCharacter.ShortLiteral, IntegerUtilities.ToUnsigned(shortValue)))
                                Else
                                    Return SyntaxFactory.NumericLiteralExpression(
                                    SyntaxFactory.IntegerLiteralToken(firstTwoChars + value.ToString("X"), LiteralBase.Hexadecimal, TypeCharacter.None, IntegerUtilities.ToUnsigned(value)))
                                End If
                            ElseIf numericText.StartsWith("&o") OrElse numericText.StartsWith("&O") Then
                                Return SyntaxFactory.NumericLiteralExpression(SyntaxFactory.IntegerLiteralToken(numericText.Substring(0, 2) + Convert.ToString(value, 8), LiteralBase.Octal, TypeCharacter.None, IntegerUtilities.ToUnsigned(value)))
                            End If
                        End If
                    End If
                End If
            End If

            Dim namedType = TryCast(enumMember.Type, INamedTypeSymbol)
            Dim underlyingType = If(namedType IsNot Nothing, namedType.EnumUnderlyingType, Nothing)

            Return ExpressionGenerator.GenerateNonEnumValueExpression(
                underlyingType,
                enumMember.ConstantValue,
                canUseFieldReference:=True)
        End Function
    End Module
End Namespace