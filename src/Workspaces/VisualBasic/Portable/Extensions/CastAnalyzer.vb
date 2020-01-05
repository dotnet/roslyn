' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Class CastAnalyzer
        Private ReadOnly _castNode As ExpressionSyntax
        Private ReadOnly _castExpressionNode As ExpressionSyntax
        Private ReadOnly _semanticModel As SemanticModel
        Private ReadOnly _assumeCallKeyword As Boolean
        Private ReadOnly _cancellationToken As CancellationToken

        Private Sub New(
            castNode As ExpressionSyntax,
            castExpressionNode As ExpressionSyntax,
            semanticModel As SemanticModel,
            assumeCallKeyword As Boolean,
            cancellationToken As CancellationToken
        )
            _castNode = castNode
            _castExpressionNode = castExpressionNode
            _semanticModel = semanticModel
            _assumeCallKeyword = assumeCallKeyword
            _cancellationToken = cancellationToken
        End Sub

        Private Function CastPassedToParamArrayDefinitelyCantBeRemoved(castType As ITypeSymbol) As Boolean
            If _castExpressionNode.WalkDownParentheses().IsKind(SyntaxKind.NothingLiteralExpression) Then
                Dim argument = TryCast(_castNode.WalkUpParentheses().Parent, ArgumentSyntax)
                If argument IsNot Nothing Then
                    Dim parameter = argument.DetermineParameter(_semanticModel, cancellationToken:=_cancellationToken)
                    If parameter IsNot Nothing AndAlso parameter.IsParams Then
                        Debug.Assert(TypeOf parameter.Type Is IArrayTypeSymbol)

                        Dim parameterType = DirectCast(parameter.Type, IArrayTypeSymbol)

                        Dim conversion = _semanticModel.Compilation.ClassifyConversion(castType, parameterType)
                        If conversion.Exists AndAlso conversion.IsWidening Then
                            Return False
                        End If

                        Dim conversionElementType = _semanticModel.Compilation.ClassifyConversion(castType, parameterType.ElementType)
                        If conversionElementType.Exists AndAlso (conversionElementType.IsIdentity OrElse conversionElementType.IsWidening) Then
                            Return True
                        End If
                    End If
                End If
            End If

            Return False
        End Function

        Private Shared Function GetOuterCastType(expression As ExpressionSyntax, expressionTypeInfo As TypeInfo, semanticModel As SemanticModel, cancellationToken As CancellationToken) As ITypeSymbol
            expression = expression.WalkUpParentheses()
            Dim parent = expression.Parent

            Dim parentExpression = TryCast(parent, ExpressionSyntax)
            If parentExpression IsNot Nothing Then
                If TypeOf parentExpression Is CastExpressionSyntax OrElse
               TypeOf parentExpression Is PredefinedCastExpressionSyntax Then
                    Return semanticModel.GetTypeInfo(parentExpression, cancellationToken).Type
                End If
            End If

            If Not Object.Equals(expressionTypeInfo.Type, expressionTypeInfo.ConvertedType) Then
                Return expressionTypeInfo.ConvertedType
            End If

            Dim parentEqualsValue = TryCast(parent, EqualsValueSyntax)
            If parentEqualsValue IsNot Nothing Then
                Dim returnedType = AsTypeInVariableDeclarator(parentEqualsValue.Parent, semanticModel)

                If returnedType IsNot Nothing Then
                    Return returnedType
                End If
            End If

            Dim parentForEach = TryCast(parent, ForEachStatementSyntax)
            If parentForEach IsNot Nothing Then
                Dim returnedType = AsTypeInVariableDeclarator(parentForEach.ControlVariable, semanticModel)

                If returnedType IsNot Nothing Then
                    Return returnedType
                End If
            End If

            Dim parentAssignmentStatement = TryCast(parent, AssignmentStatementSyntax)
            If parentAssignmentStatement IsNot Nothing AndAlso parent.Kind = SyntaxKind.SimpleAssignmentStatement Then
                Return semanticModel.GetTypeInfo(parentAssignmentStatement.Left).Type
            End If

            Dim parentUnaryExpression = TryCast(parentExpression, UnaryExpressionSyntax)
            If parentUnaryExpression IsNot Nothing AndAlso Not semanticModel.GetConversion(expression).IsUserDefined Then
                Dim parentTypeInfo = semanticModel.GetTypeInfo(parentUnaryExpression, cancellationToken)
                Return GetOuterCastType(parentUnaryExpression, parentTypeInfo, semanticModel, cancellationToken)
            End If

            Dim parentTernaryConditional = TryCast(parent, TernaryConditionalExpressionSyntax)
            If parentTernaryConditional IsNot Nothing AndAlso
               parentTernaryConditional.Condition IsNot expression Then

                Dim otherExpression = If(parentTernaryConditional.WhenTrue Is expression,
                                         parentTernaryConditional.WhenFalse,
                                         parentTernaryConditional.WhenTrue)

                Return semanticModel.GetTypeInfo(otherExpression).Type
            End If

            Return expressionTypeInfo.ConvertedType
        End Function

        Private Shared Function GetSpeculatedExpressionToOuterTypeConversion(speculatedExpression As ExpressionSyntax, speculationAnalyzer As SpeculationAnalyzer, cancellationToken As CancellationToken, <Out> ByRef speculatedExpressionOuterType As ITypeSymbol) As Conversion
            Dim innerSpeculatedExpression = speculatedExpression.WalkDownParentheses()
            Dim typeInfo = speculationAnalyzer.SpeculativeSemanticModel.GetTypeInfo(innerSpeculatedExpression, cancellationToken)
            Dim conv = speculationAnalyzer.SpeculativeSemanticModel.GetConversion(innerSpeculatedExpression, cancellationToken)

            If Not conv.IsIdentity OrElse Not Object.Equals(typeInfo.Type, typeInfo.ConvertedType) Then
                speculatedExpressionOuterType = typeInfo.ConvertedType
                Return conv
            End If

            speculatedExpression = speculatedExpression.WalkUpParentheses()
            typeInfo = speculationAnalyzer.SpeculativeSemanticModel.GetTypeInfo(speculatedExpression, cancellationToken)
            speculatedExpressionOuterType = GetOuterCastType(speculatedExpression, typeInfo, speculationAnalyzer.SpeculativeSemanticModel, cancellationToken)
            If speculatedExpressionOuterType Is Nothing Then
                Return Nothing
            End If

            Return speculationAnalyzer.SpeculativeSemanticModel.ClassifyConversion(speculatedExpression, speculatedExpressionOuterType)
        End Function

        Private Shared Function AsTypeInVariableDeclarator(node As SyntaxNode, semanticModel As SemanticModel) As ITypeSymbol
            If node Is Nothing Then
                Return Nothing
            End If

            Dim variableDeclarator = TryCast(node, VariableDeclaratorSyntax)
            If variableDeclarator IsNot Nothing Then

                Dim asClause = TryCast(variableDeclarator.AsClause, SimpleAsClauseSyntax)
                If (asClause IsNot Nothing) Then
                    Return semanticModel.GetTypeInfo(asClause.Type).Type
                End If
            End If

            Return Nothing
        End Function

        Private Function IsStartOfExecutableStatement() As Boolean
            Dim parentExpression = _castNode.WalkUpParentheses()

            Dim parentStatement = parentExpression.FirstAncestorOrSelf(Of ExecutableStatementSyntax)()
            If parentStatement Is Nothing Then
                Return False
            End If

            ' If we are assuming that a call keyword will be inserted, then
            ' we can assume that we are not at the start of the part executable
            ' statement if it's a Call statement.
            If _assumeCallKeyword AndAlso
               parentStatement.IsKind(SyntaxKind.ExpressionStatement) AndAlso DirectCast(parentStatement, ExpressionStatementSyntax).Expression.IsKind(SyntaxKind.InvocationExpression) Then
                Return False
            End If

            Return parentStatement.GetFirstToken() = parentExpression.GetFirstToken()
        End Function

        Private Function ExpressionCanStartExecutableStatement() As Boolean
            Dim innerExpression = _castExpressionNode.WalkDownParentheses()

            Return TypeOf innerExpression Is CastExpressionSyntax OrElse
                   TypeOf innerExpression Is PredefinedCastExpressionSyntax
        End Function

        Private Function IsUnnecessary() As Boolean
            Dim speculationAnalyzer = New SpeculationAnalyzer(_castNode, _castExpressionNode, _semanticModel, _cancellationToken,
                                                              skipVerificationForReplacedNode:=True, failOnOverloadResolutionFailuresInOriginalCode:=True)

            ' First, check to see if the node ultimately parenting this cast has any
            ' syntax errors. If so, we bail.
            If speculationAnalyzer.SemanticRootOfOriginalExpression.ContainsDiagnostics() Then
                Return False
            End If

            If IsStartOfExecutableStatement() AndAlso Not ExpressionCanStartExecutableStatement() Then
                Return False
            End If

            Dim castTypeInfo = _semanticModel.GetTypeInfo(_castNode, _cancellationToken)
            Dim castType = castTypeInfo.Type

            If castType Is Nothing OrElse castType.IsErrorType() Then
                Return False
            End If

            Dim castExpressionType As ITypeSymbol

            If _castExpressionNode.Kind = SyntaxKind.CollectionInitializer Then
                ' Get type of the array literal in context without the target type
                castExpressionType = _semanticModel.GetSpeculativeTypeInfo(_castExpressionNode.SpanStart, _castExpressionNode, SpeculativeBindingOption.BindAsExpression).ConvertedType
            Else
                castExpressionType = _semanticModel.GetTypeInfo(_castExpressionNode, _cancellationToken).Type
            End If

            If castExpressionType IsNot Nothing AndAlso castExpressionType.IsErrorType() Then
                Return False
            End If

            If CastPassedToParamArrayDefinitelyCantBeRemoved(castType) Then
                Return False
            End If

            ' A casts to object can always be removed from an expression inside of an interpolation, since it'll be converted to object
            ' in order to call string.Format(...) anyway.
            If (castType?.SpecialType = SpecialType.System_Object).GetValueOrDefault() AndAlso
                _castNode.WalkUpParentheses().IsParentKind(SyntaxKind.Interpolation) Then
                Return True
            End If

            ' If removing the cast will result in a change in semantics of any of the parenting nodes, we won't remove it.
            ' We do this even for identity casts in case removing that cast might affect type inference.
            If speculationAnalyzer.ReplacementChangesSemantics() Then
                Return False
            End If

            Dim expressionToCastType = _semanticModel.ClassifyConversion(_castNode.SpanStart, _castExpressionNode, castType)

            If expressionToCastType.IsIdentity Then
                ' Simple case: If the conversion from the inner expression to the cast type is identity,
                ' the cast can be removed.
                Return True
            ElseIf expressionToCastType.IsNarrowing AndAlso expressionToCastType.IsReference
                ' If the conversion from the inner expression to the cast type is narrowing reference conversion,
                ' the cast cannot be removed.
                Return False
            End If

            Dim outerType = GetOuterCastType(_castNode, castTypeInfo, _semanticModel, _cancellationToken)

            If outerType IsNot Nothing Then
                Dim castToOuterType As Conversion = _semanticModel.ClassifyConversion(_castNode.SpanStart, _castNode, outerType)
                Dim expressionToOuterType As Conversion
                Dim speculatedExpressionOuterType As ITypeSymbol = Nothing
                Dim outerSpeculatedExpression = _castNode.WalkUpParentheses()

                If outerSpeculatedExpression.IsParentKind(SyntaxKind.DirectCastExpression) OrElse
                    outerSpeculatedExpression.IsParentKind(SyntaxKind.TryCastExpression) OrElse
                    outerSpeculatedExpression.IsParentKind(SyntaxKind.CTypeExpression) Then
                    speculatedExpressionOuterType = outerType
                    expressionToOuterType = _semanticModel.ClassifyConversion(_castExpressionNode.WalkDownParentheses(), speculatedExpressionOuterType)
                Else
                    expressionToOuterType = GetSpeculatedExpressionToOuterTypeConversion(speculationAnalyzer.ReplacedExpression, speculationAnalyzer, _cancellationToken, speculatedExpressionOuterType)
                End If

                ' CONSIDER: Anonymous function conversions cannot be compared from different semantic models as lambda symbol comparison requires syntax tree equality. Should this be a compiler bug?
                ' For now, just revert back to computing expressionToOuterType using the original semantic model.
                If expressionToOuterType.IsLambda Then
                    expressionToOuterType = _semanticModel.ClassifyConversion(_castNode.SpanStart, _castExpressionNode, outerType)
                End If

                ' If there is an user-defined conversion from the expression to the cast type or the cast
                ' to the outer type, we need to make sure that the same user-defined conversion will be 
                ' called if the cast is removed.
                If castToOuterType.IsUserDefined OrElse expressionToCastType.IsUserDefined Then
                    Return (HaveSameUserDefinedConversion(expressionToCastType, expressionToOuterType) OrElse
                            HaveSameUserDefinedConversion(castToOuterType, expressionToOuterType)) AndAlso
                           (UserDefinedConversionIsAllowed(_castNode, _semanticModel) AndAlso
                            Not expressionToCastType.IsNarrowing)
                ElseIf expressionToOuterType.IsUserDefined Then
                    Return False
                End If

                If (expressionToOuterType.IsIdentity OrElse
                      (_castExpressionNode.Kind = SyntaxKind.CollectionInitializer AndAlso expressionToOuterType.IsWidening AndAlso speculatedExpressionOuterType.IsArrayType())) AndAlso
                   expressionToCastType.IsWidening Then
                    Return True
                End If

                If Not (castToOuterType.IsNullableValueType AndAlso castToOuterType.IsWidening) Then
                    Dim expressionToCastTypeIsWideningRefOrDefault As Boolean = expressionToCastType.IsWidening AndAlso (expressionToCastType.IsReference OrElse expressionToCastType.IsDefault)
                    Dim expressionToOuterTypeIsWideningRefOrDefault As Boolean = expressionToOuterType.IsWidening AndAlso (expressionToOuterType.IsReference OrElse expressionToOuterType.IsDefault)

                    If (expressionToCastTypeIsWideningRefOrDefault AndAlso expressionToOuterTypeIsWideningRefOrDefault) Then
                        If expressionToCastType.IsDefault Then
                            Return Not CastRemovalChangesDefaultValue(castType, outerType)
                        End If

                        Return True
                    ElseIf expressionToCastTypeIsWideningRefOrDefault OrElse expressionToOuterTypeIsWideningRefOrDefault Then
                        Return Equals(castType, speculatedExpressionOuterType)
                    End If

                    If expressionToCastType.IsWidening AndAlso expressionToCastType.IsLambda AndAlso
                        expressionToOuterType.IsWidening AndAlso expressionToOuterType.IsLambda Then

                        Return Not speculationAnalyzer.ReplacementChangesSemanticsOfUnchangedLambda(_castExpressionNode, speculationAnalyzer.ReplacedExpression)
                    End If
                End If

                If Not castToOuterType.IsValueType AndAlso castToOuterType = expressionToOuterType Then
                    If (castToOuterType.IsNullableValueType) Then
                        Return expressionToOuterType.IsWidening AndAlso
                            DirectCast(castExpressionType.OriginalDefinition, ITypeSymbol).SpecialType = SpecialType.System_Nullable_T
                    ElseIf expressionToCastType.IsWidening AndAlso expressionToCastType.IsNumeric AndAlso Not castToOuterType.IsIdentity Then
                        ' Some widening numeric conversions can cause loss of precision and must not be removed.
                        Return Not IsRequiredWideningNumericConversion(castExpressionType, castType)
                    End If

                    Return True
                End If

                If castToOuterType.IsIdentity AndAlso
                   expressionToCastType = expressionToOuterType Then
                    If expressionToCastType.Exists Then
                        If expressionToCastType.IsWidening Then
                            Return True
                        End If
                        If expressionToCastType.IsNarrowing AndAlso
                            Not _semanticModel.OptionStrict = OptionStrict.On Then
                            Return True
                        End If
                    Else
                        Return True
                    End If
                End If

                If expressionToOuterType.IsIdentity AndAlso
                        castToOuterType.IsWidening AndAlso
                        castToOuterType.IsReference Then
                    Debug.Assert(Not (expressionToCastType.IsNarrowing AndAlso expressionToCastType.IsReference))
                    Return True
                End If
            End If

            Return False
        End Function

        Private Shared Function HaveSameUserDefinedConversion(conversion1 As Conversion, conversion2 As Conversion) As Boolean
            Return conversion1.IsUserDefined AndAlso conversion2.IsUserDefined AndAlso conversion1.MethodSymbol.Equals(conversion2.MethodSymbol)
        End Function

        Private Shared Function UserDefinedConversionIsAllowed(expression As ExpressionSyntax, semanticModel As SemanticModel) As Boolean
            expression = expression.WalkUpParentheses()

            Dim parentNode = expression.Parent
            If parentNode Is Nothing Then
                Return False
            End If

            If parentNode.IsKind(SyntaxKind.ThrowStatement) Then
                Return False
            End If

            Return True
        End Function

        Private Shared Function IsRequiredWideningNumericConversion(sourceType As ITypeSymbol, destinationType As ITypeSymbol) As Boolean
            ' VB Language Specification: Section 8.3 Numeric Conversions

            ' Conversions from UInteger, Integer, ULong, Long, or Decimal to Single or Double are rounded to the nearest Single or Double value.
            ' While this conversion may cause a loss of precision, it will never cause a loss of magnitude

            Select Case destinationType.SpecialType
                Case SpecialType.System_Single, SpecialType.System_Double
                    Select Case sourceType.SpecialType
                        Case SpecialType.System_UInt32, SpecialType.System_Int32,
                            SpecialType.System_UInt64, SpecialType.System_Int64,
                            SpecialType.System_Decimal

                            Return True
                    End Select
            End Select

            Return False
        End Function

        Private Shared Function CastRemovalChangesDefaultValue(castType As ITypeSymbol, outerType As ITypeSymbol) As Boolean
            If castType.IsNumericType() Then
                Return Not outerType.IsNumericType()
            ElseIf castType.SpecialType = SpecialType.System_DateTime
                Return Not outerType.SpecialType = SpecialType.System_DateTime
            ElseIf castType.SpecialType = SpecialType.System_Boolean
                Return Not (outerType.IsNumericType OrElse outerType.SpecialType = SpecialType.System_Boolean)
            End If

            If castType.OriginalDefinition?.SpecialType = SpecialType.System_Nullable_T Then
                ' Don't allow casts of Nothing to T? to be removed unless the outer type is T? or Object.
                ' Otherwise, Nothing will lose its "nullness" and get the default value of T.
                '
                ' So, this is OK:
                '
                '   Dim x As Object = DirectCast(Nothing, Integer?)
                '
                ' But this is not:
                '
                '   Dim x As Integer = DirectCast(Nothing, Integer?)

                If castType.Equals(outerType) OrElse outerType.SpecialType = SpecialType.System_Object Then
                    Return False
                End If

                Return True
            End If

            Return False
        End Function

        Public Shared Function IsUnnecessary(
            castNode As ExpressionSyntax,
            castExpressionNode As ExpressionSyntax,
            semanticModel As SemanticModel,
            assumeCallKeyword As Boolean,
            cancellationToken As CancellationToken
        ) As Boolean

            Return New CastAnalyzer(castNode, castExpressionNode, semanticModel, assumeCallKeyword, cancellationToken).IsUnnecessary()
        End Function

    End Class
End Namespace
