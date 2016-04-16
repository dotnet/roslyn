' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Partial Friend Module ExpressionGenerator

        Private Const s_doubleQuote = """"

        Friend Function GenerateExpression(typedConstant As TypedConstant) As ExpressionSyntax
            Select Case typedConstant.Kind
                Case TypedConstantKind.Primitive, TypedConstantKind.Enum
                    Return GenerateExpression(typedConstant.Type, typedConstant.Value, canUseFieldReference:=True)

                Case TypedConstantKind.Array
                    If typedConstant.IsNull Then
                        Return GenerateNothingLiteral()
                    Else
                        Return SyntaxFactory.CollectionInitializer(
                            SyntaxFactory.SeparatedList(typedConstant.Values.Select(AddressOf GenerateExpression)))
                    End If
                Case TypedConstantKind.Type
                    If Not TypeOf typedConstant.Value Is ITypeSymbol Then
                        Return GenerateNothingLiteral()
                    End If

                    Return SyntaxFactory.GetTypeExpression(DirectCast(typedConstant.Value, ITypeSymbol).GenerateTypeSyntax())

                Case Else
                    Return GenerateNothingLiteral()
            End Select
        End Function

        Friend Function GenerateExpression(type As ITypeSymbol, value As Object, canUseFieldReference As Boolean) As ExpressionSyntax
            If (type.OriginalDefinition.SpecialType = SpecialType.System_Nullable_T) AndAlso
               (value IsNot Nothing) Then
                ' If the type of the argument is T?, then the type of the supplied default value can either be T 
                ' (e.g. Optional x As Integer? = 5) or it can be T? (e.g. Optional x as SomeStruct? = Nothing). The
                ' below statement handles the case where the type of the supplied default value is T.
                Return GenerateExpression(DirectCast(type, INamedTypeSymbol).TypeArguments(0), value, canUseFieldReference)
            End If

            If type.TypeKind = TypeKind.Enum AndAlso value IsNot Nothing Then
                Return DirectCast(VisualBasicFlagsEnumGenerator.Instance.CreateEnumConstantValue(DirectCast(type, INamedTypeSymbol), value), ExpressionSyntax)
            End If

            Return GenerateNonEnumValueExpression(type, value, canUseFieldReference)
        End Function

        Friend Function GenerateNonEnumValueExpression(type As ITypeSymbol, value As Object, canUseFieldReference As Boolean) As ExpressionSyntax
            If TypeOf value Is Boolean Then
                Dim boolValue = DirectCast(value, Boolean)
                If boolValue Then
                    Return SyntaxFactory.TrueLiteralExpression(SyntaxFactory.Token(SyntaxKind.TrueKeyword))
                Else
                    Return SyntaxFactory.FalseLiteralExpression(SyntaxFactory.Token(SyntaxKind.FalseKeyword))
                End If
            ElseIf TypeOf value Is String Then
                Return GenerateStringLiteralExpression(type, DirectCast(value, String))
            ElseIf TypeOf value Is Char Then
                Return GenerateCharLiteralExpression(DirectCast(value, Char))
            ElseIf TypeOf value Is SByte Then
                Return GenerateIntegralLiteralExpression(type, SpecialType.System_SByte, value, canUseFieldReference, LiteralSpecialValues.SByteSpecialValues)
            ElseIf TypeOf value Is Short Then
                Return GenerateIntegralLiteralExpression(type, SpecialType.System_Int16, value, canUseFieldReference, LiteralSpecialValues.Int16SpecialValues)
            ElseIf TypeOf value Is Integer Then
                Return GenerateIntegralLiteralExpression(type, SpecialType.System_Int32, value, canUseFieldReference, LiteralSpecialValues.Int32SpecialValues)
            ElseIf TypeOf value Is Long Then
                Return GenerateLongLiteralExpression(type, DirectCast(value, Long), canUseFieldReference)
            ElseIf TypeOf value Is Byte Then
                Return GenerateIntegralLiteralExpression(type, SpecialType.System_Byte, value, canUseFieldReference, LiteralSpecialValues.ByteSpecialValues)
            ElseIf TypeOf value Is UShort Then
                Return GenerateIntegralLiteralExpression(type, SpecialType.System_UInt16, value, canUseFieldReference, LiteralSpecialValues.UInt16SpecialValues)
            ElseIf TypeOf value Is UInteger Then
                Return GenerateIntegralLiteralExpression(type, SpecialType.System_UInt32, value, canUseFieldReference, LiteralSpecialValues.UInt32SpecialValues)
            ElseIf TypeOf value Is ULong Then
                Return GenerateIntegralLiteralExpression(type, SpecialType.System_UInt64, value, canUseFieldReference, LiteralSpecialValues.UInt64SpecialValues)
            ElseIf TypeOf value Is Single Then
                Return GenerateSingleLiteralExpression(type, DirectCast(value, Single), canUseFieldReference)
            ElseIf TypeOf value Is Double Then
                Return GenerateDoubleLiteralExpression(type, DirectCast(value, Double), canUseFieldReference)
            ElseIf TypeOf value Is Decimal Then
                Return GenerateDecimalLiteralExpression(type, DirectCast(value, Decimal), canUseFieldReference)
            ElseIf TypeOf value Is DateTime Then
                Return GenerateDateLiteralExpression(DirectCast(value, DateTime))
            Else
                Return GenerateNothingLiteral()
            End If
        End Function

        Private Function GenerateNothingLiteral() As ExpressionSyntax
            Return SyntaxFactory.NothingLiteralExpression(SyntaxFactory.Token(SyntaxKind.NothingKeyword))
        End Function

        Private Function GenerateDateLiteralExpression(value As Date) As ExpressionSyntax
            Dim literal = SymbolDisplay.FormatPrimitive(value, quoteStrings:=False, useHexadecimalNumbers:=False)
            Return SyntaxFactory.DateLiteralExpression(
                SyntaxFactory.DateLiteralToken(literal, value))
        End Function

        Private Function GenerateStringLiteralExpression(type As ITypeSymbol, value As String) As ExpressionSyntax
            Dim pieces = StringPiece.Split(value)
            If pieces.Count = 0 Then
                Return SyntaxFactory.StringLiteralExpression(SyntaxFactory.StringLiteralToken(s_doubleQuote & s_doubleQuote, String.Empty))
            End If

            If pieces.Count = 1 AndAlso pieces(0).Kind = StringPieceKind.NonPrintable Then
                If Not IsSpecialType(type, SpecialType.System_String) Then
                    Return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CStrKeyword), pieces(0).GenerateExpression())
                End If
            End If

            Dim expression As ExpressionSyntax = Nothing
            For Each piece In pieces
                Dim subExpression = piece.GenerateExpression()

                If expression Is Nothing Then
                    expression = subExpression
                Else
                    expression = SyntaxFactory.ConcatenateExpression(expression, subExpression)
                End If
            Next

            Return expression
        End Function

        Private Function GenerateMemberAccessExpression(ParamArray names As String()) As MemberAccessExpressionSyntax
            Dim expression As ExpressionSyntax = SyntaxFactory.GlobalName()
            For Each name In names
                expression = SyntaxFactory.SimpleMemberAccessExpression(
                    expression,
                    SyntaxFactory.Token(SyntaxKind.DotToken),
                    SyntaxFactory.IdentifierName(name))
            Next

            Return DirectCast(expression, MemberAccessExpressionSyntax).WithAdditionalAnnotations(Simplifier.Annotation)
        End Function

        Private Function GenerateChrWExpression(c As Char) As InvocationExpressionSyntax
            Dim factory = New VisualBasicSyntaxGenerator()
            Dim access = GenerateMemberAccessExpression("Microsoft", "VisualBasic", "Strings", "ChrW")

            Dim value = AscW(c)
            Dim argument = SyntaxFactory.SimpleArgument(
                        SyntaxFactory.NumericLiteralExpression(
                            SyntaxFactory.IntegerLiteralToken(value.ToString(Nothing, CultureInfo.InvariantCulture), LiteralBase.Decimal, TypeCharacter.None, CULng(value))))
            Dim invocation = SyntaxFactory.InvocationExpression(
                access,
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(Of ArgumentSyntax)(argument)))

            Return invocation.WithAdditionalAnnotations(Simplifier.Annotation)
        End Function

        Private Function GenerateIntegralLiteralExpression(Of TStructure)(type As ITypeSymbol,
                                                                          specialType As SpecialType,
                                                                          value As Object,
                                                                          canUseFieldReference As Boolean,
                                                                          specialValues As IEnumerable(Of KeyValuePair(Of TStructure, String))) As ExpressionSyntax

            If canUseFieldReference Then
                Dim field = GenerateFieldReference(specialType, value, specialValues)
                If field IsNot Nothing Then
                    Return field
                End If
            End If

            Dim typeSuffix As TypeCharacter = TypeCharacter.None
            Dim suffix As String = String.Empty
            DetermineSuffix(type, value, typeSuffix, suffix)

            Dim literal = DirectCast(value, IFormattable).ToString(Nothing, CultureInfo.InvariantCulture) & suffix
            Dim expression = SyntaxFactory.NumericLiteralExpression(SyntaxFactory.IntegerLiteralToken(
                    literal, LiteralBase.Decimal, typeSuffix,
                    IntegerUtilities.ToUInt64(value)))

            If TypeOf value Is Byte AndAlso Not IsSpecialType(type, SpecialType.System_Byte) Then
                Return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CByteKeyword), expression)
            ElseIf TypeOf value Is SByte AndAlso Not IsSpecialType(type, SpecialType.System_SByte) Then
                Return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(SyntaxKind.CSByteKeyword), expression)
            End If

            Return expression
        End Function

        Private Function GenerateLongLiteralExpression(type As ITypeSymbol,
                                                       value As Long,
                                                       canUseFieldReference As Boolean) As ExpressionSyntax
            If canUseFieldReference OrElse value > Long.MinValue Then
                Return GenerateIntegralLiteralExpression(type, SpecialType.System_Int64, value, canUseFieldReference, LiteralSpecialValues.Int64SpecialValues)
            End If

            ' We have to special case how Long.MinValue is printed when we can't refer to the 
            ' field directly.
            Return SyntaxFactory.NumericLiteralExpression(SyntaxFactory.IntegerLiteralToken(
                "&H8000000000000000", LiteralBase.Hexadecimal, TypeCharacter.None, IntegerUtilities.ToUInt64(value)))
        End Function

        Private Sub DetermineSuffix(type As ITypeSymbol,
                                           value As Object,
                                           ByRef typeSuffix As TypeCharacter,
                                           ByRef suffix As String)
            If TypeOf value Is Short AndAlso Not IsSpecialType(type, SpecialType.System_Int16) Then
                typeSuffix = TypeCharacter.ShortLiteral
                suffix = "S"
            ElseIf TypeOf value Is Long AndAlso Not IsSpecialType(type, SpecialType.System_Int64) Then
                typeSuffix = TypeCharacter.LongLiteral
                suffix = "L"
            ElseIf TypeOf value Is Decimal Then
                Dim d = DirectCast(value, Decimal)
                Dim scale = d.GetScale()

                Dim typeIsNotDecimal = Not IsSpecialType(type, SpecialType.System_Decimal)
                Dim scaleIsNotZero = scale <> 0
                Dim valueIsOutOfRange = d <= Long.MinValue OrElse d > Long.MaxValue

                If typeIsNotDecimal OrElse
                   scaleIsNotZero OrElse
                   valueIsOutOfRange Then
                    typeSuffix = TypeCharacter.DecimalLiteral
                    suffix = "D"
                End If
            ElseIf TypeOf value Is UShort AndAlso Not IsSpecialType(type, SpecialType.System_UInt16) Then
                typeSuffix = TypeCharacter.UShortLiteral
                suffix = "US"
            ElseIf TypeOf value Is UInteger AndAlso Not IsSpecialType(type, SpecialType.System_UInt32) Then
                typeSuffix = TypeCharacter.UIntegerLiteral
                suffix = "UI"
            ElseIf TypeOf value Is ULong Then
                Dim d = DirectCast(value, ULong)

                Dim typeIsNotULong = Not IsSpecialType(type, SpecialType.System_UInt64)
                Dim valueIsOutOfRange = d > Long.MaxValue

                If typeIsNotULong OrElse
                   valueIsOutOfRange Then
                    typeSuffix = TypeCharacter.ULongLiteral
                    suffix = "UL"
                End If
            ElseIf TypeOf value Is Single AndAlso Not IsSpecialType(type, SpecialType.System_Single) Then
                typeSuffix = TypeCharacter.SingleLiteral
                suffix = "F"
            ElseIf TypeOf value Is Double AndAlso Not IsSpecialType(type, SpecialType.System_Double) Then
                typeSuffix = TypeCharacter.DoubleLiteral
                suffix = "R"
            End If
        End Sub

        Private Function GenerateDoubleLiteralExpression(type As ITypeSymbol,
                                                                value As Double,
                                                                canUseFieldReference As Boolean) As ExpressionSyntax
            If Not canUseFieldReference Then
                If Double.IsNaN(value) Then
                    Return SyntaxFactory.DivideExpression(
                        GenerateFloatLiteral(0.0, "0.0"),
                        GenerateFloatLiteral(0.0, "0.0"))
                ElseIf Double.IsPositiveInfinity(value) Then
                    Return SyntaxFactory.DivideExpression(
                        GenerateFloatLiteral(1.0, "1.0"),
                        GenerateFloatLiteral(0.0, "0.0"))
                ElseIf (Double.IsNegativeInfinity(value)) Then
                    Return SyntaxFactory.DivideExpression(
                        SyntaxFactory.UnaryMinusExpression(GenerateFloatLiteral(1.0, "1.0")),
                        GenerateFloatLiteral(0.0, "0.0"))
                End If
            End If

            Return GenerateFloatLiteralExpression(type, SpecialType.System_Double, value, canUseFieldReference, LiteralSpecialValues.DoubleSpecialValues)
        End Function

        Private Function GenerateSingleLiteralExpression(type As ITypeSymbol,
                                                                value As Single,
                                                                canUseFieldReference As Boolean) As ExpressionSyntax
            If Not canUseFieldReference Then
                If Double.IsNaN(value) Then
                    Return SyntaxFactory.DivideExpression(
                        GenerateFloatLiteral(0.0, "0.0F"),
                        GenerateFloatLiteral(0.0, "0.0F"))
                ElseIf Double.IsPositiveInfinity(value) Then
                    Return SyntaxFactory.DivideExpression(
                        GenerateFloatLiteral(1.0, "1.0F"),
                        GenerateFloatLiteral(0.0, "0.0F"))
                ElseIf (Double.IsNegativeInfinity(value)) Then
                    Return SyntaxFactory.DivideExpression(
                        SyntaxFactory.UnaryMinusExpression(GenerateFloatLiteral(1.0, "1.0F")),
                        GenerateFloatLiteral(0.0, "0.0F"))
                End If
            End If

            Return GenerateFloatLiteralExpression(type, SpecialType.System_Single, value, canUseFieldReference, LiteralSpecialValues.SingleSpecialValues)
        End Function

        Private Function GenerateFloatLiteralExpression(Of TStructure)(type As ITypeSymbol,
                                                                       specialType As SpecialType,
                                                                       value As Object,
                                                                       canUseFieldReference As Boolean,
                                                                       specialValues As IEnumerable(Of KeyValuePair(Of TStructure, String))) As ExpressionSyntax
            If canUseFieldReference Then
                Dim field = GenerateFieldReference(specialType, value, specialValues)
                If field IsNot Nothing Then
                    Return field
                End If
            End If

            Dim typeSuffix As TypeCharacter = TypeCharacter.None
            Dim suffix As String = String.Empty
            DetermineSuffix(type, value, typeSuffix, suffix)

            Dim literal = DirectCast(value, IFormattable).ToString("R", CultureInfo.InvariantCulture) & suffix
            Return GenerateFloatLiteral(Convert.ToDouble(value), literal, typeSuffix)
        End Function

        Private Function GenerateFloatLiteral(value As Double,
                                                     literal As String,
                                                     Optional typeSuffix As TypeCharacter = TypeCharacter.None) As LiteralExpressionSyntax
            Return SyntaxFactory.NumericLiteralExpression(SyntaxFactory.FloatingLiteralToken(
                literal, typeSuffix, value))
        End Function


        Private Function GenerateCharLiteralExpression(c As Char) As ExpressionSyntax
            Dim pieces = StringPiece.Split(c.ToString())
            Dim piece = pieces(0)

            If piece.Kind = StringPieceKind.Normal Then
                Return SyntaxFactory.CharacterLiteralExpression(SyntaxFactory.CharacterLiteralToken(
                    SymbolDisplay.FormatPrimitive(c, quoteStrings:=True, useHexadecimalNumbers:=False), c))
            End If

            Return GenerateChrWExpression(c)
        End Function

        Private Function GenerateDecimalLiteralExpression(type As ITypeSymbol, value As Decimal, canUseFieldReference As Boolean) As ExpressionSyntax
            If canUseFieldReference Then
                Dim field = GenerateFieldReference(SpecialType.System_Decimal, value, LiteralSpecialValues.DecimalSpecialValues)
                If field IsNot Nothing Then
                    Return field
                End If
            End If

            Dim typeSuffix As TypeCharacter = TypeCharacter.None
            Dim suffix As String = String.Empty
            DetermineSuffix(type, value, typeSuffix, suffix)

            Dim literal = value.ToString(Nothing, CultureInfo.InvariantCulture) & suffix
            Return SyntaxFactory.NumericLiteralExpression(SyntaxFactory.DecimalLiteralToken(literal, typeSuffix, value))
        End Function

        Private Function AddSpecialTypeAnnotation(type As SpecialType, expression As MemberAccessExpressionSyntax) As MemberAccessExpressionSyntax
            If SpecialType.None <> type Then
                Return expression.WithAdditionalAnnotations(SpecialTypeAnnotation.Create(type))
            End If

            Return expression
        End Function

        Private Function GenerateFieldReference(Of TStructure)(type As SpecialType,
                                                               value As Object,
                                                               specialValues As IEnumerable(Of KeyValuePair(Of TStructure, String))) As MemberAccessExpressionSyntax
            For Each specialValue In specialValues
                If specialValue.Key.Equals(value) Then
                    Dim memberAccess = AddSpecialTypeAnnotation(type, GenerateMemberAccessExpression("System", GetType(TStructure).Name))
                    Return SyntaxFactory.SimpleMemberAccessExpression(memberAccess, SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(specialValue.Value)) _
                        .WithAdditionalAnnotations(Simplifier.Annotation)
                End If
            Next

            Return Nothing
        End Function
    End Module
End Namespace
