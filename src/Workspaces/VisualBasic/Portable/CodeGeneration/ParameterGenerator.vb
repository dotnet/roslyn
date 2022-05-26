' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Class ParameterGenerator

        Public Shared Function GenerateParameterList(parameterDefinitions As ImmutableArray(Of IParameterSymbol), options As CodeGenerationContextInfo) As ParameterListSyntax
            Return GenerateParameterList(DirectCast(parameterDefinitions, IList(Of IParameterSymbol)), options)
        End Function

        Public Shared Function GenerateParameterList(parameterDefinitions As IEnumerable(Of IParameterSymbol), options As CodeGenerationContextInfo) As ParameterListSyntax
            Dim result = New List(Of ParameterSyntax)()
            Dim seenOptional = False

            For Each p In parameterDefinitions
                Dim generated = GenerateParameter(p, seenOptional, options)
                result.Add(generated)
                seenOptional = seenOptional OrElse generated.Default IsNot Nothing
            Next

            Return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(result))
        End Function

        Friend Shared Function GenerateParameter(parameter As IParameterSymbol, seenOptional As Boolean, options As CodeGenerationContextInfo) As ParameterSyntax
            Dim reusableSyntax = GetReuseableSyntaxNodeForSymbol(Of ParameterSyntax)(parameter, options)
            If reusableSyntax IsNot Nothing Then
                Return reusableSyntax
            End If

            ' TODO(cyrusn): Should we provide some way to disable this special case?

            ' If the type is actually an array, then we place the array specifier on the identifier,
            ' not on the type syntax.  

            If parameter.Type.IsArrayType() Then
                Dim arrayType = DirectCast(parameter.Type, IArrayTypeSymbol)
                Dim elementType = arrayType.ElementType

                If Not elementType.IsArrayType() AndAlso
                   elementType.OriginalDefinition.SpecialType <> SpecialType.System_Nullable_T Then

                    Dim arguments = Enumerable.Repeat(Of ArgumentSyntax)(SyntaxFactory.OmittedArgument(), arrayType.Rank)
                    Dim argumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))

                    Return SyntaxFactory.Parameter(
                        AttributeGenerator.GenerateAttributeBlocks(parameter.GetAttributes(), options),
                        GenerateModifiers(parameter, seenOptional),
                        parameter.Name.ToModifiedIdentifier.WithArrayBounds(argumentList),
                        SyntaxFactory.SimpleAsClause(type:=elementType.GenerateTypeSyntax()),
                        GenerateEqualsValue(parameter, seenOptional))
                End If
            End If

            Dim asClause = If(parameter.Type Is Nothing,
                               Nothing,
                               SyntaxFactory.SimpleAsClause(type:=parameter.Type.GenerateTypeSyntax()))
            Return SyntaxFactory.Parameter(
                AttributeGenerator.GenerateAttributeBlocks(parameter.GetAttributes(), options),
                GenerateModifiers(parameter, seenOptional),
                parameter.Name.ToModifiedIdentifier(),
                asClause,
                GenerateEqualsValue(parameter, seenOptional))
        End Function

        Private Shared Function GenerateModifiers(parameter As IParameterSymbol, seenOptional As Boolean) As SyntaxTokenList
            If parameter.IsParams Then
                Return SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ParamArrayKeyword))
            End If

            Dim modifiers = SyntaxFactory.TokenList()

            If parameter.IsRefOrOut() Then
                modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.ByRefKeyword))
            End If

            If parameter.IsOptional OrElse seenOptional Then
                modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.OptionalKeyword))
            End If

            Return modifiers
        End Function

        Private Shared Function GenerateEqualsValue(parameter As IParameterSymbol, seenOptional As Boolean) As EqualsValueSyntax
            If parameter.HasExplicitDefaultValue OrElse parameter.IsOptional OrElse seenOptional Then
                Return SyntaxFactory.EqualsValue(
                    ExpressionGenerator.GenerateExpression(
                        parameter.Type,
                        If(parameter.HasExplicitDefaultValue, parameter.ExplicitDefaultValue, Nothing),
                        canUseFieldReference:=True))
            End If

            Return Nothing
        End Function
    End Class
End Namespace
