' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Module EnumMemberGenerator

        Friend Function AddEnumMemberTo(
            destination As EnumBlockSyntax,
            enumMember As IFieldSymbol,
            options As CodeGenerationContextInfo) As EnumBlockSyntax

            Dim member = GenerateEnumMemberDeclaration(enumMember, destination, options)
            If member Is Nothing Then
                Return destination
            End If

            Dim members = New List(Of StatementSyntax)()
            members.AddRange(destination.Members)
            members.Add(member)
            Dim leadingTrivia = destination.EndEnumStatement.GetLeadingTrivia()

            Return destination.WithMembers(SyntaxFactory.List(Of StatementSyntax)(members)).
                               WithEndEnumStatement(If(destination.EndEnumStatement.IsMissing, SyntaxFactory.EndEnumStatement(), destination.EndEnumStatement))
        End Function

        Public Function GenerateEnumMemberDeclaration(enumMember As IFieldSymbol,
                                                             enumDeclarationOpt As EnumBlockSyntax,
                                                             options As CodeGenerationContextInfo) As EnumMemberDeclarationSyntax
            ' We never generate the special enum backing field.
            If enumMember.Name = WellKnownMemberNames.EnumBackingFieldName Then
                Return Nothing
            End If

            Dim reusableSyntax = GetReuseableSyntaxNodeForSymbol(Of EnumMemberDeclarationSyntax)(enumMember, options)
            If reusableSyntax IsNot Nothing Then
                Return reusableSyntax
            End If

            Dim value = CreateEnumMemberValue(enumDeclarationOpt, enumMember)
            Dim member = SyntaxFactory.EnumMemberDeclaration(enumMember.Name.ToIdentifierToken()) _
                               .WithInitializer(If(value Is Nothing, Nothing, SyntaxFactory.EqualsValue(value:=value)))

            Return AddFormatterAndCodeGeneratorAnnotationsTo(ConditionallyAddDocumentationCommentTo(member, enumMember, options))
        End Function

        Private Function CreateEnumMemberValue(destinationOpt As EnumBlockSyntax, enumMember As IFieldSymbol) As ExpressionSyntax
            If Not enumMember.HasConstantValue Then
                Return Nothing
            End If

            If TypeOf enumMember.ConstantValue IsNot Byte AndAlso
               TypeOf enumMember.ConstantValue IsNot SByte AndAlso
               TypeOf enumMember.ConstantValue IsNot UShort AndAlso
               TypeOf enumMember.ConstantValue IsNot Short AndAlso
               TypeOf enumMember.ConstantValue IsNot Integer AndAlso
               TypeOf enumMember.ConstantValue IsNot UInteger AndAlso
               TypeOf enumMember.ConstantValue IsNot Long AndAlso
               TypeOf enumMember.ConstantValue IsNot ULong Then
                Return Nothing
            End If

            Dim value = IntegerUtilities.ToInt64(enumMember.ConstantValue)

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
                        If lastExpression.Kind = SyntaxKind.LeftShiftExpression AndAlso IntegerUtilities.HasOneBitSet(value) Then
                            Dim binaryExpression = DirectCast(lastExpression, BinaryExpressionSyntax)
                            If binaryExpression.Left.Kind = SyntaxKind.NumericLiteralExpression Then
                                Dim numericLiteral = DirectCast(binaryExpression.Left, LiteralExpressionSyntax)
                                If numericLiteral.Token.ValueText = "1" Then
                                    ' The user is left shifting ones, stick with that pattern
                                    Dim shiftValue = IntegerUtilities.LogBase2(value)

                                    ' Using the numericLiteral text will ensure the correct type character, ignoring the None that is passed in below
                                    Return SyntaxFactory.LeftShiftExpression(
                                    left:=SyntaxFactory.NumericLiteralExpression(SyntaxFactory.IntegerLiteralToken(numericLiteral.Token.Text, LiteralBase.Decimal, TypeCharacter.None, 1)),
                                    right:=SyntaxFactory.NumericLiteralExpression(SyntaxFactory.IntegerLiteralToken(shiftValue.ToString(), LiteralBase.Decimal, TypeCharacter.None, IntegerUtilities.ToUnsigned(shiftValue))))
                                End If
                            End If
                        ElseIf lastExpression.Kind = SyntaxKind.NumericLiteralExpression Then
                            Dim numericLiteral = DirectCast(lastExpression, LiteralExpressionSyntax)
                            Dim numericToken = numericLiteral.Token
                            Dim numericText = numericToken.ToString()
                            If numericText.StartsWith("&H", StringComparison.OrdinalIgnoreCase) Then
                                Dim firstTwoChars = numericText.Substring(0, 2)

                                If numericText.EndsWith("US", StringComparison.OrdinalIgnoreCase) AndAlso
                               value >= UShort.MinValue AndAlso value <= UShort.MaxValue Then
                                    Dim ushortValue = CUShort(value)

                                    Dim lastTwoChars = numericText.Substring(numericText.Length - 2, 2)
                                    Return SyntaxFactory.NumericLiteralExpression(
                                    SyntaxFactory.IntegerLiteralToken(firstTwoChars + ushortValue.ToString("X") + lastTwoChars, LiteralBase.Hexadecimal, TypeCharacter.UShortLiteral, IntegerUtilities.ToUnsigned(ushortValue)))
                                ElseIf numericText.EndsWith("S", StringComparison.OrdinalIgnoreCase) AndAlso
                                   value >= Short.MinValue AndAlso value <= Short.MaxValue Then
                                    Dim shortValue = CShort(value)
                                    Return SyntaxFactory.NumericLiteralExpression(
                                    SyntaxFactory.IntegerLiteralToken(firstTwoChars + shortValue.ToString("X") + numericText.Last(), LiteralBase.Hexadecimal, TypeCharacter.ShortLiteral, IntegerUtilities.ToUnsigned(shortValue)))
                                Else
                                    Return SyntaxFactory.NumericLiteralExpression(
                                    SyntaxFactory.IntegerLiteralToken(firstTwoChars + value.ToString("X"), LiteralBase.Hexadecimal, TypeCharacter.None, IntegerUtilities.ToUnsigned(value)))
                                End If
                            ElseIf numericText.StartsWith("&O", StringComparison.OrdinalIgnoreCase) Then
                                Return SyntaxFactory.NumericLiteralExpression(SyntaxFactory.IntegerLiteralToken(numericText.Substring(0, 2) + Convert.ToString(value, 8), LiteralBase.Octal, TypeCharacter.None, IntegerUtilities.ToUnsigned(value)))
                            ElseIf numericText.StartsWith("&B", StringComparison.OrdinalIgnoreCase) Then
                                Return SyntaxFactory.NumericLiteralExpression(SyntaxFactory.IntegerLiteralToken(numericText.Substring(0, 2) + Convert.ToString(value, 2), LiteralBase.Binary, TypeCharacter.None, IntegerUtilities.ToUnsigned(value)))
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
