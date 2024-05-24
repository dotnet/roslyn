' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ' Binding of conversion operators is implemented in this part.

    Partial Friend Class Binder

        Private Function BindCastExpression(
             node As CastExpressionSyntax,
             diagnostics As BindingDiagnosticBag
         ) As BoundExpression

            Dim result As BoundExpression

            Select Case node.Keyword.Kind
                Case SyntaxKind.CTypeKeyword
                    result = BindCTypeExpression(node, diagnostics)

                Case SyntaxKind.DirectCastKeyword
                    result = BindDirectCastExpression(node, diagnostics)

                Case SyntaxKind.TryCastKeyword
                    result = BindTryCastExpression(node, diagnostics)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Keyword.Kind)
            End Select

            Return result
        End Function

        Private Function BindCTypeExpression(
             node As CastExpressionSyntax,
             diagnostics As BindingDiagnosticBag
         ) As BoundExpression

            Debug.Assert(node.Keyword.Kind = SyntaxKind.CTypeKeyword)

            Dim argument = BindValue(node.Expression, diagnostics)
            Dim targetType = BindTypeSyntax(node.Type, diagnostics)

            Return ApplyConversion(node, targetType, argument, isExplicit:=True, diagnostics)
        End Function

        Private Function BindDirectCastExpression(
             node As CastExpressionSyntax,
             diagnostics As BindingDiagnosticBag
         ) As BoundExpression

            Debug.Assert(node.Keyword.Kind = SyntaxKind.DirectCastKeyword)

            Dim argument = BindValue(node.Expression, diagnostics)
            Dim targetType = BindTypeSyntax(node.Type, diagnostics)

            Return ApplyDirectCastConversion(node, argument, targetType, diagnostics)
        End Function

        Private Function ApplyDirectCastConversion(
             node As SyntaxNode,
             argument As BoundExpression,
             targetType As TypeSymbol,
             diagnostics As BindingDiagnosticBag
        ) As BoundExpression
            Debug.Assert(argument.IsValue)

            ' Deal with erroneous arguments
            If (argument.HasErrors OrElse targetType.IsErrorType) Then
                argument = MakeRValue(argument, diagnostics)

                Return New BoundDirectCast(node, argument, conversionKind:=Nothing, type:=targetType, hasErrors:=True)
            End If

            ' Classify conversion
            Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)
            Dim conv As ConversionKind = Conversions.ClassifyDirectCastConversion(argument, targetType, Me, useSiteInfo)

            If diagnostics.Add(node, useSiteInfo) Then
                ' Suppress any additional diagnostics
                diagnostics = BindingDiagnosticBag.Discarded
            End If

            If ReclassifyExpression(argument, SyntaxKind.DirectCastKeyword, node, conv, True, targetType, diagnostics) Then
                If argument.Syntax IsNot node Then
                    ' If its an explicit conversion, we must have a bound node that corresponds to that syntax node for GetSemanticInfo.
                    ' Insert an identity conversion if necessary.
                    Debug.Assert(argument.Kind <> BoundKind.DirectCast, "Associated wrong node with conversion?")
                    argument = New BoundDirectCast(node, argument, ConversionKind.Identity, targetType)
                End If

                Return argument
            Else
                argument = MakeRValue(argument, diagnostics)
            End If

            If argument.HasErrors Then
                Return New BoundDirectCast(node, argument, conv, targetType, hasErrors:=True)
            End If

            Dim sourceType = argument.Type

            If sourceType IsNot Nothing AndAlso sourceType.IsErrorType() Then
                Return New BoundDirectCast(node, argument, conv, targetType, hasErrors:=True)
            End If

            Debug.Assert(argument.IsNothingLiteral() OrElse (sourceType IsNot Nothing AndAlso Not sourceType.IsErrorType()))

            ' Check for special error conditions
            If Conversions.NoConversion(conv) Then
                If sourceType.IsValueType AndAlso sourceType.IsRestrictedType() AndAlso
                       (targetType.IsObjectType() OrElse targetType.SpecialType = SpecialType.System_ValueType) Then
                    ReportDiagnostic(diagnostics, argument.Syntax, ERRID.ERR_RestrictedConversion1, sourceType)
                    Return New BoundDirectCast(node, argument, conv, targetType, hasErrors:=True)
                End If
            End If

            WarnOnNarrowingConversionBetweenSealedClassAndAnInterface(conv, argument.Syntax, sourceType, targetType, diagnostics)

            If Conversions.NoConversion(conv) Then
                ReportNoConversionError(argument.Syntax, sourceType, targetType, diagnostics)
                Return New BoundDirectCast(node, argument, conv, targetType, hasErrors:=True)
            End If

            If Conversions.IsIdentityConversion(conv) Then
                If targetType.IsFloatingType() Then
                    ReportDiagnostic(diagnostics, argument.Syntax, ERRID.ERR_IdentityDirectCastForFloat)

                ElseIf targetType.IsValueType Then
                    ReportDiagnostic(diagnostics, argument.Syntax, ERRID.WRN_ObsoleteIdentityDirectCastForValueType)
                End If
            End If

            Dim integerOverflow As Boolean = False

            Dim constantResult = Conversions.TryFoldConstantConversion(
                                    argument,
                                    targetType,
                                    integerOverflow)

            If constantResult IsNot Nothing Then
                Debug.Assert(Conversions.IsIdentityConversion(conv) OrElse
                                      conv = ConversionKind.WideningNothingLiteral OrElse
                                      sourceType.GetEnumUnderlyingTypeOrSelf().IsSameTypeIgnoringAll(targetType.GetEnumUnderlyingTypeOrSelf()))
                Debug.Assert(Not integerOverflow)
                Debug.Assert(Not constantResult.IsBad)
            Else
                constantResult = Conversions.TryFoldNothingReferenceConversion(argument, conv, targetType)
            End If

            If Not Conversions.IsIdentityConversion(conv) Then
                WarnOnLockConversion(sourceType, argument.Syntax, diagnostics)
            End If

            Return New BoundDirectCast(node, argument, conv, constantResult, targetType)
        End Function

        Private Function BindTryCastExpression(
             node As CastExpressionSyntax,
             diagnostics As BindingDiagnosticBag
         ) As BoundExpression

            Debug.Assert(node.Keyword.Kind = SyntaxKind.TryCastKeyword)

            Dim argument = BindValue(node.Expression, diagnostics)
            Dim targetType = BindTypeSyntax(node.Type, diagnostics)

            Return ApplyTryCastConversion(node, argument, targetType, diagnostics)
        End Function

        Private Function ApplyTryCastConversion(
             node As SyntaxNode,
             argument As BoundExpression,
             targetType As TypeSymbol,
             diagnostics As BindingDiagnosticBag
        ) As BoundExpression
            Debug.Assert(argument.IsValue)

            ' Deal with erroneous arguments
            If (argument.HasErrors OrElse targetType.IsErrorType) Then
                argument = MakeRValue(argument, diagnostics)

                Return New BoundTryCast(node, argument, conversionKind:=Nothing, type:=targetType, hasErrors:=True)
            End If

            ' Classify conversion
            Dim conv As ConversionKind

            If targetType.IsReferenceType Then
                Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)
                conv = Conversions.ClassifyTryCastConversion(argument, targetType, Me, useSiteInfo)

                If diagnostics.Add(node, useSiteInfo) Then
                    ' Suppress any additional diagnostics
                    diagnostics = BindingDiagnosticBag.Discarded
                End If
            Else
                conv = Nothing
            End If

            If ReclassifyExpression(argument, SyntaxKind.TryCastKeyword, node, conv, True, targetType, diagnostics) Then
                If argument.Syntax IsNot node Then
                    ' If its an explicit conversion, we must have a bound node that corresponds to that syntax node for GetSemanticInfo.
                    ' Insert an identity conversion if necessary.
                    Debug.Assert(argument.Kind <> BoundKind.TryCast, "Associated wrong node with conversion?")
                    argument = New BoundTryCast(node, argument, ConversionKind.Identity, targetType)
                End If

                Return argument
            Else
                argument = MakeRValue(argument, diagnostics)
            End If

            If argument.HasErrors Then
                Return New BoundTryCast(node, argument, conv, targetType, hasErrors:=True)
            End If

            Dim sourceType = argument.Type

            If sourceType IsNot Nothing AndAlso sourceType.IsErrorType() Then
                Return New BoundTryCast(node, argument, conv, targetType, hasErrors:=True)
            End If

            Debug.Assert(argument.IsNothingLiteral() OrElse (sourceType IsNot Nothing AndAlso Not sourceType.IsErrorType()))

            ' Check for special error conditions
            If Conversions.NoConversion(conv) Then
                If targetType.IsValueType() Then
                    Dim castSyntax = TryCast(node, CastExpressionSyntax)
                    ReportDiagnostic(diagnostics, If(castSyntax IsNot Nothing, castSyntax.Type, node), ERRID.ERR_TryCastOfValueType1, targetType)
                    Return New BoundTryCast(node, argument, conv, targetType, hasErrors:=True)

                ElseIf targetType.IsTypeParameter() AndAlso Not targetType.IsReferenceType Then
                    Dim castSyntax = TryCast(node, CastExpressionSyntax)
                    ReportDiagnostic(diagnostics, If(castSyntax IsNot Nothing, castSyntax.Type, node), ERRID.ERR_TryCastOfUnconstrainedTypeParam1, targetType)
                    Return New BoundTryCast(node, argument, conv, targetType, hasErrors:=True)

                ElseIf sourceType.IsValueType AndAlso sourceType.IsRestrictedType() AndAlso
                       (targetType.IsObjectType() OrElse targetType.SpecialType = SpecialType.System_ValueType) Then
                    ReportDiagnostic(diagnostics, argument.Syntax, ERRID.ERR_RestrictedConversion1, sourceType)
                    Return New BoundTryCast(node, argument, conv, targetType, hasErrors:=True)
                End If
            End If

            WarnOnNarrowingConversionBetweenSealedClassAndAnInterface(conv, argument.Syntax, sourceType, targetType, diagnostics)

            If Conversions.NoConversion(conv) Then
                ReportNoConversionError(argument.Syntax, sourceType, targetType, diagnostics)
                Return New BoundTryCast(node, argument, conv, targetType, hasErrors:=True)
            End If

            Dim constantResult = Conversions.TryFoldNothingReferenceConversion(argument, conv, targetType)

            If Not Conversions.IsIdentityConversion(conv) Then
                WarnOnLockConversion(sourceType, argument.Syntax, diagnostics)
            End If

            Return New BoundTryCast(node, argument, conv, constantResult, targetType)
        End Function

        Private Function BindPredefinedCastExpression(
             node As PredefinedCastExpressionSyntax,
             diagnostics As BindingDiagnosticBag
         ) As BoundExpression

            Dim targetType As SpecialType

            Select Case node.Keyword.Kind
                Case SyntaxKind.CBoolKeyword : targetType = SpecialType.System_Boolean
                Case SyntaxKind.CByteKeyword : targetType = SpecialType.System_Byte
                Case SyntaxKind.CCharKeyword : targetType = SpecialType.System_Char
                Case SyntaxKind.CDateKeyword : targetType = SpecialType.System_DateTime
                Case SyntaxKind.CDecKeyword : targetType = SpecialType.System_Decimal
                Case SyntaxKind.CDblKeyword : targetType = SpecialType.System_Double
                Case SyntaxKind.CIntKeyword : targetType = SpecialType.System_Int32
                Case SyntaxKind.CLngKeyword : targetType = SpecialType.System_Int64
                Case SyntaxKind.CObjKeyword : targetType = SpecialType.System_Object
                Case SyntaxKind.CSByteKeyword : targetType = SpecialType.System_SByte
                Case SyntaxKind.CShortKeyword : targetType = SpecialType.System_Int16
                Case SyntaxKind.CSngKeyword : targetType = SpecialType.System_Single
                Case SyntaxKind.CStrKeyword : targetType = SpecialType.System_String
                Case SyntaxKind.CUIntKeyword : targetType = SpecialType.System_UInt32
                Case SyntaxKind.CULngKeyword : targetType = SpecialType.System_UInt64
                Case SyntaxKind.CUShortKeyword : targetType = SpecialType.System_UInt16
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Keyword.Kind)
            End Select

            Return ApplyConversion(node, GetSpecialType(targetType, node.Keyword, diagnostics),
                                   BindValue(node.Expression, diagnostics),
                                   isExplicit:=True, diagnostics:=diagnostics)
        End Function

        ''' <summary>
        ''' This function must return a BoundConversion node in case of non-identity conversion.
        ''' </summary>
        Friend Function ApplyImplicitConversion(
            node As SyntaxNode,
            targetType As TypeSymbol,
            expression As BoundExpression,
            diagnostics As BindingDiagnosticBag,
            Optional isOperandOfConditionalBranch As Boolean = False
        ) As BoundExpression
            Return ApplyConversion(node, targetType, expression, False, diagnostics, isOperandOfConditionalBranch:=isOperandOfConditionalBranch)
        End Function

        ''' <summary>
        ''' This function must return a BoundConversion node in case of explicit or non-identity conversion.
        ''' </summary>
        Private Function ApplyConversion(
            node As SyntaxNode,
            targetType As TypeSymbol,
            argument As BoundExpression,
            isExplicit As Boolean,
            diagnostics As BindingDiagnosticBag,
            Optional isOperandOfConditionalBranch As Boolean = False,
            Optional explicitSemanticForConcatArgument As Boolean = False
        ) As BoundExpression
            Debug.Assert(node IsNot Nothing)
            Debug.Assert(Not isOperandOfConditionalBranch OrElse Not isExplicit)
            Debug.Assert(argument.IsValue())

            ' Deal with erroneous arguments
            If targetType.IsErrorType Then
                argument = MakeRValueAndIgnoreDiagnostics(argument)

                If Not isExplicit AndAlso argument.Type.IsSameTypeIgnoringAll(targetType) Then
                    Return argument
                End If

                Return New BoundConversion(node,
                                           argument,
                                           conversionKind:=Nothing,
                                           checked:=CheckOverflow,
                                           explicitCastInCode:=isExplicit,
                                           type:=targetType,
                                           hasErrors:=True)
            End If

            If argument.HasErrors Then
                ' Suppress any additional diagnostics produced by this function
                diagnostics = BindingDiagnosticBag.Discarded
            End If

            ' Classify conversion
            Dim conv As KeyValuePair(Of ConversionKind, MethodSymbol)
            Dim applyNullableIsTrueOperator As Boolean = False
            Dim isTrueOperator As OverloadResolution.OverloadResolutionResult = Nothing
            Dim result As BoundExpression

            Debug.Assert(Not isOperandOfConditionalBranch OrElse targetType.IsBooleanType())

            Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)

            If isOperandOfConditionalBranch AndAlso targetType.IsBooleanType() Then
                Debug.Assert(Not isExplicit)
                conv = Conversions.ClassifyConversionOfOperandOfConditionalBranch(argument, targetType, Me,
                                                                                  applyNullableIsTrueOperator,
                                                                                  isTrueOperator,
                                                                                  useSiteInfo)

                If diagnostics.Add(node, useSiteInfo) Then
                    ' Suppress any additional diagnostics
                    diagnostics = BindingDiagnosticBag.Discarded
                End If

                If isTrueOperator.BestResult.HasValue Then
                    ' Apply IsTrue operator.
                    Dim isTrue As BoundUserDefinedUnaryOperator = BindUserDefinedUnaryOperator(node, UnaryOperatorKind.IsTrue, argument, isTrueOperator, diagnostics)

                    isTrue.SetWasCompilerGenerated()
                    isTrue.UnderlyingExpression.SetWasCompilerGenerated()
                    result = isTrue
                Else
                    Dim intermediateTargetType As TypeSymbol

                    If applyNullableIsTrueOperator Then
                        ' Note, use site error will be reported by ApplyNullableIsTrueOperator later.
                        Dim nullableOfT As NamedTypeSymbol = Compilation.GetSpecialType(SpecialType.System_Nullable_T)
                        intermediateTargetType = Compilation.GetSpecialType(SpecialType.System_Nullable_T).
                                                        Construct(ImmutableArray.Create(Of TypeSymbol)(targetType))
                    Else
                        intermediateTargetType = targetType
                    End If

                    result = CreateConversionAndReportDiagnostic(node, argument, conv, isExplicit, intermediateTargetType, diagnostics)
                End If

                If applyNullableIsTrueOperator Then
                    result = Binder.ApplyNullableIsTrueOperator(result, targetType)
                End If
            Else
                conv = Conversions.ClassifyConversion(argument, targetType, Me, useSiteInfo)

                If diagnostics.Add(node, useSiteInfo) Then
                    ' Suppress any additional diagnostics
                    diagnostics = BindingDiagnosticBag.Discarded
                End If

                result = CreateConversionAndReportDiagnostic(node, argument, conv, isExplicit, targetType, diagnostics,
                                                             explicitSemanticForConcatArgument:=explicitSemanticForConcatArgument)
            End If

            Return result
        End Function

        Private Shared Function ApplyNullableIsTrueOperator(argument As BoundExpression, booleanType As TypeSymbol) As BoundNullableIsTrueOperator
            Debug.Assert(argument.Type.IsNullableOfBoolean() AndAlso booleanType.IsBooleanType())
            Return New BoundNullableIsTrueOperator(argument.Syntax, argument, booleanType).MakeCompilerGenerated()
        End Function

        ''' <summary>
        ''' This function must return a BoundConversion node in case of non-identity conversion.
        ''' </summary>
        Private Function CreateConversionAndReportDiagnostic(
            tree As SyntaxNode,
            argument As BoundExpression,
            convKind As KeyValuePair(Of ConversionKind, MethodSymbol),
            isExplicit As Boolean,
            targetType As TypeSymbol,
            diagnostics As BindingDiagnosticBag,
            Optional copybackConversionParamName As String = Nothing,
            Optional explicitSemanticForConcatArgument As Boolean = False
        ) As BoundExpression
            Debug.Assert(argument.IsValue())

            ' We need to preserve any conversion that was explicitly written in code 
            ' (so that GetSemanticInfo can find the syntax in the bound tree). Implicit identity conversion
            ' don't need representation in the bound tree (they would be optimized away in emit, but are so common
            ' that this is an important way to save both time and memory).
            If (Not isExplicit OrElse explicitSemanticForConcatArgument) AndAlso Conversions.IsIdentityConversion(convKind.Key) Then
                Debug.Assert(argument.Type.IsSameTypeIgnoringAll(targetType))
                Debug.Assert(tree Is argument.Syntax)
                Return MakeRValue(argument, diagnostics)
            End If

            If (convKind.Key And ConversionKind.UserDefined) = 0 AndAlso
               ReclassifyExpression(argument, SyntaxKind.CTypeKeyword, tree, convKind.Key, isExplicit, targetType, diagnostics) Then
                argument = MakeRValue(argument, diagnostics)

                If isExplicit AndAlso argument.Syntax IsNot tree Then
                    ' If its an explicit conversion, we must have a bound node that corresponds to that syntax node for GetSemanticInfo.
                    ' Insert an identity conversion if necessary.
                    Debug.Assert(argument.Kind <> BoundKind.Conversion, "Associated wrong node with conversion?")
                    argument = New BoundConversion(tree, argument, ConversionKind.Identity, CheckOverflow, isExplicit, targetType)
                End If

                Return argument
            ElseIf Not argument.IsNothingLiteral() AndAlso argument.Kind <> BoundKind.ArrayLiteral Then
                argument = MakeRValue(argument, diagnostics)
            End If

            Debug.Assert(argument.Kind <> BoundKind.Conversion OrElse DirectCast(argument, BoundConversion).ExplicitCastInCode OrElse
                         Not argument.IsNothingLiteral() OrElse
                         TypeOf argument.Syntax.Parent Is BinaryExpressionSyntax OrElse
                         TypeOf argument.Syntax.Parent Is UnaryExpressionSyntax OrElse
                         TypeOf argument.Syntax.Parent Is TupleExpressionSyntax OrElse
                         (TypeOf argument.Syntax.Parent Is AssignmentStatementSyntax AndAlso argument.Syntax.Parent.Kind <> SyntaxKind.SimpleAssignmentStatement),
                         "Applying yet another conversion to an implicit conversion from NOTHING, probably MakeRValue was called too early.")

            Dim sourceType = argument.Type

            ' At this point if the expression is an array literal then the conversion must be user defined.
            Debug.Assert(argument.Kind <> BoundKind.ArrayLiteral OrElse (convKind.Key And ConversionKind.UserDefined) <> 0)

            Dim reportArrayLiteralElementNarrowingConversion = False
            If argument.Kind = BoundKind.ArrayLiteral Then
                ' The array will get the type from the input type of the user defined conversion.
                sourceType = convKind.Value.Parameters(0).Type

                ' If the conversion from the inferred element type to the source type of the user defined conversion is a narrowing conversion then
                ' skip to the user defined conversion. Conversion errors on the individual elements will be reported when the array literal is reclassified.
                Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)
                reportArrayLiteralElementNarrowingConversion =
                    Not isExplicit AndAlso
                    Conversions.IsNarrowingConversion(convKind.Key) AndAlso
                    Conversions.IsNarrowingConversion(Conversions.ClassifyArrayLiteralConversion(DirectCast(argument, BoundArrayLiteral), sourceType, Me, useSiteInfo))

                diagnostics.Add(argument.Syntax, useSiteInfo)

                If reportArrayLiteralElementNarrowingConversion Then
                    GoTo DoneWithDiagnostics
                End If
            End If

            If argument.HasErrors Then
                GoTo DoneWithDiagnostics
            End If

            If sourceType IsNot Nothing AndAlso sourceType.IsErrorType() Then
                GoTo DoneWithDiagnostics
            End If

            Debug.Assert(argument.IsNothingLiteral() OrElse (sourceType IsNot Nothing AndAlso Not sourceType.IsErrorType()))

            ' Check for special error conditions
            If Conversions.NoConversion(convKind.Key) Then
                If sourceType.IsValueType AndAlso sourceType.IsRestrictedType() AndAlso
                       (targetType.IsObjectType() OrElse targetType.SpecialType = SpecialType.System_ValueType) Then
                    ReportDiagnostic(diagnostics, argument.Syntax, ERRID.ERR_RestrictedConversion1, sourceType)
                    Return New BoundConversion(tree, argument, convKind.Key, CheckOverflow, isExplicit, targetType, hasErrors:=True)
                End If
            End If

            WarnOnNarrowingConversionBetweenSealedClassAndAnInterface(convKind.Key, argument.Syntax, sourceType, targetType, diagnostics)

            ' Deal with implicit narrowing conversions
            If Not isExplicit AndAlso Conversions.IsNarrowingConversion(convKind.Key) AndAlso
               (convKind.Key And ConversionKind.InvolvesNarrowingFromNumericConstant) = 0 Then

                If copybackConversionParamName IsNot Nothing Then
                    If OptionStrict = VisualBasic.OptionStrict.On Then
                        ReportDiagnostic(diagnostics, argument.Syntax, ERRID.ERR_StrictArgumentCopyBackNarrowing3,
                                               copybackConversionParamName, sourceType, targetType)
                    ElseIf OptionStrict = VisualBasic.OptionStrict.Custom Then
                        ReportDiagnostic(diagnostics, argument.Syntax, ERRID.WRN_ImplicitConversionCopyBack,
                                               copybackConversionParamName, sourceType, targetType)
                    End If

                Else
                    ' We have a narrowing conversion. This is how we might display it, depending on context:
                    ' ERR_NarrowingConversionDisallowed2 "Option Strict On disallows implicit conversions from '|1' to '|2'."
                    ' ERR_NarrowingConversionCollection2 "Option Strict On disallows implicit conversions from '|1' to '|2'; 
                    '                                     the Visual Basic 6.0 collection type is not compatible with the .NET Framework collection type."
                    ' ERR_AmbiguousCastConversion2       "Option Strict On disallows implicit conversions from '|1' to '|2' because the conversion is ambiguous."
                    ' The Collection error is for when one type is Microsoft.VisualBasic.Collection and
                    ' the other type is named _Collection.
                    ' The Ambiguous error is for when the conversion was classed as "Narrowing" for reasons of ambiguity.

                    If OptionStrict = VisualBasic.OptionStrict.On Then

                        If Not MakeVarianceConversionSuggestion(convKind.Key, argument.Syntax, sourceType, targetType, diagnostics, justWarn:=False) Then
                            Dim err As ERRID = ERRID.ERR_NarrowingConversionDisallowed2
                            Const _Collection As String = "_Collection"

                            If (convKind.Key And ConversionKind.VarianceConversionAmbiguity) <> 0 Then
                                err = ERRID.ERR_AmbiguousCastConversion2
                            ElseIf (sourceType.IsMicrosoftVisualBasicCollection() AndAlso String.Equals(targetType.Name, _Collection, StringComparison.Ordinal)) OrElse
                                    (String.Equals(sourceType.Name, _Collection, StringComparison.Ordinal) AndAlso targetType.IsMicrosoftVisualBasicCollection()) Then
                                ' Got both, so use the more specific error message
                                err = ERRID.ERR_NarrowingConversionCollection2
                            End If

                            ReportDiagnostic(diagnostics, argument.Syntax, err, sourceType, targetType)
                        End If

                    ElseIf OptionStrict = VisualBasic.OptionStrict.Custom Then

                        ' Avoid reporting a warning if narrowing caused exclusively by "zero argument" relaxation
                        ' for an Anonymous Delegate. Note, that dropping a return is widening. 
                        If (convKind.Key And ConversionKind.AnonymousDelegate) = 0 OrElse
                           (convKind.Key And ConversionKind.DelegateRelaxationLevelMask) <> ConversionKind.DelegateRelaxationLevelWideningDropReturnOrArgs Then

                            If Not MakeVarianceConversionSuggestion(convKind.Key, argument.Syntax, sourceType, targetType, diagnostics, justWarn:=True) Then
                                Dim wrnId1 As ERRID = ERRID.WRN_ImplicitConversionSubst1
                                Dim wrnId2 As ERRID = ERRID.WRN_ImplicitConversion2

                                If (convKind.Key And ConversionKind.VarianceConversionAmbiguity) <> 0 Then
                                    wrnId2 = ERRID.WRN_AmbiguousCastConversion2
                                End If

                                ReportDiagnostic(diagnostics, argument.Syntax, wrnId1, ErrorFactory.ErrorInfo(wrnId2, sourceType, targetType))
                            End If
                        End If
                    End If
                End If
            End If

            If Conversions.NoConversion(convKind.Key) Then
                If Conversions.FailedDueToNumericOverflow(convKind.Key) Then
                    Dim errorTargetType As TypeSymbol

                    If (convKind.Key And ConversionKind.UserDefined) <> 0 AndAlso convKind.Value IsNot Nothing Then
                        errorTargetType = convKind.Value.Parameters(0).Type
                    Else
                        errorTargetType = targetType
                    End If

                    ReportDiagnostic(diagnostics, argument.Syntax, ERRID.ERR_ExpressionOverflow1, errorTargetType)

                ElseIf isExplicit OrElse Not MakeVarianceConversionSuggestion(convKind.Key, argument.Syntax, sourceType, targetType, diagnostics, justWarn:=False) Then
                    ReportNoConversionError(argument.Syntax, sourceType, targetType, diagnostics, copybackConversionParamName)
                End If

                Return New BoundConversion(tree, argument, convKind.Key And (Not ConversionKind.UserDefined), CheckOverflow, isExplicit, targetType, hasErrors:=True)
            End If

DoneWithDiagnostics:
            If (convKind.Key And ConversionKind.UserDefined) <> 0 Then
                Return CreateUserDefinedConversion(tree, argument, convKind, isExplicit, targetType, reportArrayLiteralElementNarrowingConversion, diagnostics)
            End If

            If argument.HasErrors OrElse (sourceType IsNot Nothing AndAlso sourceType.IsErrorType()) Then
                Return New BoundConversion(tree, argument, convKind.Key, CheckOverflow, isExplicit, targetType, hasErrors:=True)
            End If

            Return CreatePredefinedConversion(tree, argument, convKind.Key, isExplicit, targetType, diagnostics)
        End Function

        Private Structure VarianceSuggestionTypeParameterInfo
            Private _isViable As Boolean
            Private _typeParameter As TypeParameterSymbol
            Private _derivedArgument As TypeSymbol
            Private _baseArgument As TypeSymbol

            Public Sub [Set](parameter As TypeParameterSymbol, derived As TypeSymbol, base As TypeSymbol)
                _typeParameter = parameter
                _derivedArgument = derived
                _baseArgument = base
                _isViable = True
            End Sub

            Public ReadOnly Property IsViable As Boolean
                Get
                    Return _isViable
                End Get
            End Property

            Public ReadOnly Property TypeParameter As TypeParameterSymbol
                Get
                    Return _typeParameter
                End Get
            End Property

            Public ReadOnly Property DerivedArgument As TypeSymbol
                Get
                    Return _derivedArgument
                End Get
            End Property

            Public ReadOnly Property BaseArgument As TypeSymbol
                Get
                    Return _baseArgument
                End Get
            End Property
        End Structure

        ''' <summary>
        ''' Returns True if error or warning was reported.
        ''' 
        ''' This function is invoked on the occasion of a Narrowing or NoConversion.
        ''' It looks at the conversion. If the conversion could have been helped by variance in
        ''' some way, it reports an error/warning message to that effect and returns true. This
        ''' message is a substitute for whatever other conversion-failed message might have been displayed.
        '''
        ''' Note: these variance-related messages will NOT show auto-correct suggestion of using CType. That's
        ''' because, in these cases, it's more likely than not that CType will fail, so it would be a bad suggestion
        ''' </summary>
        Private Function MakeVarianceConversionSuggestion(
            convKind As ConversionKind,
            location As SyntaxNode,
            sourceType As TypeSymbol,
            targetType As TypeSymbol,
            diagnostics As BindingDiagnosticBag,
            justWarn As Boolean
        ) As Boolean
            Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)
            Dim result As Boolean = MakeVarianceConversionSuggestion(convKind, location, sourceType, targetType, diagnostics, useSiteInfo, justWarn)
            diagnostics.AddDependencies(useSiteInfo)
            Return result
        End Function

        Private Function MakeVarianceConversionSuggestion(
            convKind As ConversionKind,
            location As SyntaxNode,
            sourceType As TypeSymbol,
            targetType As TypeSymbol,
            diagnostics As BindingDiagnosticBag,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol),
            justWarn As Boolean
        ) As Boolean

            If (convKind And ConversionKind.UserDefined) <> 0 Then
                Return False
            End If

            ' Variance scenario 2: Dim x As List(Of Animal) = New List(Of Tiger)
            ' "List(Of Tiger) cannot be converted to List(Of Animal). Consider using IEnumerable(Of Animal) instead."
            '
            ' (1) If the user attempts a conversion to DEST which is a generic binding of one of the non-variant
            '     standard generic collection types List(Of D), Collection(Of D), ReadOnlyCollection(Of D),
            '     IList(Of D), ICollection(Of D)
            ' (2) and if the conversion failed (either ConversionNarrowing or ConversionError),
            ' (3) and if the source type SOURCE implemented/inherited exactly one binding ISOURCE=G(Of S) of that
            '     generic collection type G
            ' (4) and if there is a reference conversion from S to D
            ' (5) Then report "G(Of S) cannot be converted to G(Of D). Consider converting to IEnumerable(Of D) instead."

            If targetType.Kind <> SymbolKind.NamedType Then
                Return False
            End If

            Dim targetNamedType = DirectCast(targetType, NamedTypeSymbol)

            If Not targetNamedType.IsGenericType Then
                Return False
            End If

            Dim targetGenericDefinition As NamedTypeSymbol = targetNamedType.OriginalDefinition

            If targetGenericDefinition.SpecialType = SpecialType.System_Collections_Generic_IList_T OrElse
               targetGenericDefinition.SpecialType = SpecialType.System_Collections_Generic_ICollection_T OrElse
               targetGenericDefinition.SpecialType = SpecialType.System_Collections_Generic_IReadOnlyList_T OrElse
               targetGenericDefinition.SpecialType = SpecialType.System_Collections_Generic_IReadOnlyCollection_T OrElse
               targetGenericDefinition Is Compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_List_T) OrElse
               targetGenericDefinition Is Compilation.GetWellKnownType(WellKnownType.System_Collections_ObjectModel_Collection_T) OrElse
               targetGenericDefinition Is Compilation.GetWellKnownType(WellKnownType.System_Collections_ObjectModel_ReadOnlyCollection_T) Then

                Dim sourceTypeArgument As TypeSymbol = Nothing

                If targetGenericDefinition.IsInterfaceType() Then
                    Dim matchingInterfaces As New HashSet(Of NamedTypeSymbol)()
                    If IsOrInheritsFromOrImplementsInterface(sourceType, targetGenericDefinition, useSiteInfo:=useSiteInfo, matchingInterfaces:=matchingInterfaces) AndAlso
                        matchingInterfaces.Count = 1 Then
                        sourceTypeArgument = matchingInterfaces(0).TypeArgumentsNoUseSiteDiagnostics(0)
                    End If
                Else
                    Dim typeToCheck As TypeSymbol = sourceType

                    Do
                        If typeToCheck.OriginalDefinition Is targetGenericDefinition Then
                            sourceTypeArgument = DirectCast(typeToCheck, NamedTypeSymbol).TypeArgumentsNoUseSiteDiagnostics(0)
                            Exit Do
                        End If

                        typeToCheck = typeToCheck.BaseTypeNoUseSiteDiagnostics
                    Loop While typeToCheck IsNot Nothing
                End If

                If sourceTypeArgument IsNot Nothing AndAlso
                   Conversions.IsWideningConversion(Conversions.Classify_Reference_Array_TypeParameterConversion(sourceTypeArgument,
                                                                                                                 targetNamedType.TypeArgumentsNoUseSiteDiagnostics(0),
                                                                                                                 varianceCompatibilityClassificationDepth:=0,
                                                                                                                 useSiteInfo:=useSiteInfo)) Then
                    Dim iEnumerable_T As NamedTypeSymbol = Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)

                    If Not iEnumerable_T.IsErrorType() Then
                        Dim suggestion As NamedTypeSymbol = iEnumerable_T.Construct(targetNamedType.TypeArgumentsNoUseSiteDiagnostics(0))

                        If justWarn Then
                            ReportDiagnostic(diagnostics, location, ERRID.WRN_ImplicitConversionSubst1,
                                             ErrorFactory.ErrorInfo(ERRID.WRN_VarianceIEnumerableSuggestion3, sourceType, targetType, suggestion))
                        Else
                            ReportDiagnostic(diagnostics, location, ERRID.ERR_VarianceIEnumerableSuggestion3, sourceType, targetType, suggestion)
                        End If

                        Return True
                    End If
                End If
            End If

            ' Variance scenario 1:                                  | Variance scenario 3:
            ' Dim x as IEnumerable(Of Tiger) = New List(Of Animal)  | Dim x As IGoo(Of Animal) = New MyGoo
            ' "List(Of Animal) cannot be converted to               | "MyGoo cannot be converted to IGoo(Of Animal).
            ' IEnumerable(Of Tiger) because 'Animal' is not derived | Consider changing the 'T' in the definition
            ' from 'Tiger', as required for the 'Out' generic       | of interface IGoo(Of T) to an Out type
            ' parameter 'T' in 'IEnumerable(Of Out T)'"             | parameter, Out T."
            '                                                       |
            ' (1) If the user attempts a conversion to              | (1) If the user attempts a conversion to some
            '     some target type DEST=G(Of D1,D2,...) which is    |     target type DEST=G(Of D1,D2,...) which is
            '     a generic instantiation of some variant interface/|     a generic instantiation of some interface/delegate
            '     delegate type G(Of T1,T2,...),                    |     type G(...), which NEED NOT be variant!
            ' (2) and if the conversion fails (Narrowing/Error),    | (2) and if the type G is defined in source-code,
            ' (3) and if the source type SOURCE implements/         |     not imported metadata. And the conversion fails.
            '     inherits exactly one binding INHSOURCE=           | (3) And INHSOURCE=exactly one binding of G
            '     G(Of S1,S2,...) of that generic type G,           | (4) And if ever difference is either Di/Si/Ti
            ' (4) and if the only differences between (D1,D2,...)   |     where Ti has In/Out variance, or is
            '     and (S1,S2,...) occur in positions "Di/Si"        |     Dj/Sj/Tj such that Tj has no variance and
            '     such that the corresponding Ti has either In      |     Dj has a CLR conversion to Sj or vice versa
            '     or Out variance                                   | (5) Then pick the first difference Dj/Sj
            ' (5) Then pick on the one such difference Si/Di/Ti     | (6) and report "SOURCE cannot be converted to
            ' (6) and report "SOURCE cannot be converted to DEST    |     DEST. Consider changing Tj in the definition
            '     because Si is not derived from Di, as required    |     of interface/delegate IGoo(Of T) to an
            '     for the 'In/Out' generic parameter 'T' in         |     In/Out type parameter, In/Out T".
            '     'IEnumerable(Of Out T)'"                          |
            Dim matchingGenericInstantiation As NamedTypeSymbol

            ' (1) If the user attempts a conversion 
            Select Case targetGenericDefinition.TypeKind
                Case TypeKind.Delegate
                    If sourceType.OriginalDefinition Is targetGenericDefinition Then
                        matchingGenericInstantiation = DirectCast(sourceType, NamedTypeSymbol)
                    Else
                        Return False
                    End If

                Case TypeKind.Interface
                    Dim matchingInterfaces As New HashSet(Of NamedTypeSymbol)()

                    If IsOrInheritsFromOrImplementsInterface(sourceType, targetGenericDefinition, useSiteInfo:=useSiteInfo, matchingInterfaces:=matchingInterfaces) AndAlso
                        matchingInterfaces.Count = 1 Then
                        matchingGenericInstantiation = matchingInterfaces(0)
                    Else
                        Return False
                    End If

                Case Else
                    Return False
            End Select

            ' (3) and if the source type implemented exactly one binding of it...
            Dim source As NamedTypeSymbol = matchingGenericInstantiation
            Dim destination As NamedTypeSymbol = targetNamedType

            Dim oneVariantDifference As VarianceSuggestionTypeParameterInfo = Nothing ' for Di/Si/Ti
            Dim oneInvariantConvertibleDifference As TypeParameterSymbol = Nothing 'for Dj/Sj/Tj where Sj<Dj
            Dim oneInvariantReverseConvertibleDifference As TypeParameterSymbol = Nothing ' Dj/Sj/Tj where Dj<Sj

            Do
                Dim typeParameters As ImmutableArray(Of TypeParameterSymbol) = source.TypeParameters
                Dim sourceArguments As ImmutableArray(Of TypeSymbol) = source.TypeArgumentsNoUseSiteDiagnostics
                Dim destinationArguments As ImmutableArray(Of TypeSymbol) = destination.TypeArgumentsNoUseSiteDiagnostics

                For i As Integer = 0 To typeParameters.Length - 1

                    Dim sourceArg As TypeSymbol = sourceArguments(i)
                    Dim destinationArg As TypeSymbol = destinationArguments(i)

                    If sourceArg.IsSameTypeIgnoringAll(destinationArg) Then
                        Continue For
                    End If

                    If sourceArg.IsErrorType() OrElse destinationArg.IsErrorType() Then
                        Continue For
                    End If

                    Dim conv As ConversionKind = Nothing

                    Select Case typeParameters(i).Variance
                        Case VarianceKind.Out
                            If sourceArg.IsValueType OrElse destinationArg.IsValueType Then
                                oneVariantDifference.Set(typeParameters(i), sourceArg, destinationArg)
                            Else
                                conv = Conversions.Classify_Reference_Array_TypeParameterConversion(sourceArg, destinationArg,
                                                                                                    varianceCompatibilityClassificationDepth:=0,
                                                                                                    useSiteInfo:=useSiteInfo)

                                If Not Conversions.IsWideningConversion(conv) Then
                                    If Not Conversions.IsNarrowingConversion(conv) OrElse (conv And ConversionKind.VarianceConversionAmbiguity) = 0 Then
                                        oneVariantDifference.Set(typeParameters(i), sourceArg, destinationArg)
                                    End If
                                End If
                            End If
                        Case VarianceKind.In
                            If sourceArg.IsValueType OrElse destinationArg.IsValueType Then
                                oneVariantDifference.Set(typeParameters(i), destinationArg, sourceArg)
                            Else
                                conv = Conversions.Classify_Reference_Array_TypeParameterConversion(destinationArg, sourceArg,
                                                                                                    varianceCompatibilityClassificationDepth:=0,
                                                                                                    useSiteInfo:=useSiteInfo)

                                If Not Conversions.IsWideningConversion(conv) Then
                                    If (targetNamedType.IsDelegateType AndAlso destinationArg.IsReferenceType AndAlso sourceArg.IsReferenceType) OrElse
                                       Not Conversions.IsNarrowingConversion(conv) OrElse
                                       (conv And ConversionKind.VarianceConversionAmbiguity) = 0 Then
                                        oneVariantDifference.Set(typeParameters(i), destinationArg, sourceArg)
                                    End If
                                End If
                            End If

                        Case Else
                            conv = Conversions.ClassifyDirectCastConversion(sourceArg, destinationArg, useSiteInfo)

                            If Conversions.IsWideningConversion(conv) Then
                                oneInvariantConvertibleDifference = typeParameters(i)
                            Else
                                conv = Conversions.ClassifyDirectCastConversion(destinationArg, sourceArg, useSiteInfo)

                                If Conversions.IsWideningConversion(conv) Then
                                    oneInvariantReverseConvertibleDifference = typeParameters(i)
                                Else
                                    Return False
                                End If
                            End If
                    End Select

                Next

                source = source.ContainingType
                destination = destination.ContainingType
            Loop While source IsNot Nothing

            ' (5) If a Di/Si/Ti, and no Dj/Sj/Tj nor Dk/Sk/Tk, then report...
            If oneVariantDifference.IsViable AndAlso
               oneInvariantConvertibleDifference Is Nothing AndAlso
               oneInvariantReverseConvertibleDifference Is Nothing Then

                Dim containerFormatter As FormattedSymbol

                If oneVariantDifference.TypeParameter.ContainingType.IsDelegateType Then
                    containerFormatter = CustomSymbolDisplayFormatter.DelegateSignature(oneVariantDifference.TypeParameter.ContainingSymbol)
                Else
                    containerFormatter = CustomSymbolDisplayFormatter.ErrorNameWithKind(oneVariantDifference.TypeParameter.ContainingSymbol)
                End If

                If justWarn Then
                    ReportDiagnostic(diagnostics, location, ERRID.WRN_ImplicitConversionSubst1,
                                     ErrorFactory.ErrorInfo(If(oneVariantDifference.TypeParameter.Variance = VarianceKind.Out,
                                                               ERRID.WRN_VarianceConversionFailedOut6,
                                                               ERRID.WRN_VarianceConversionFailedIn6),
                                                            oneVariantDifference.DerivedArgument,
                                                            oneVariantDifference.BaseArgument,
                                                            oneVariantDifference.TypeParameter.Name,
                                                            containerFormatter,
                                                            sourceType,
                                                            targetType))
                Else
                    ReportDiagnostic(diagnostics, location, If(oneVariantDifference.TypeParameter.Variance = VarianceKind.Out,
                                                               ERRID.ERR_VarianceConversionFailedOut6,
                                                               ERRID.ERR_VarianceConversionFailedIn6),
                                                            oneVariantDifference.DerivedArgument,
                                                            oneVariantDifference.BaseArgument,
                                                            oneVariantDifference.TypeParameter.Name,
                                                            containerFormatter,
                                                            sourceType,
                                                            targetType)
                End If

                Return True
            End If

            ' (5b) Otherwise, if a Dj/Sj/Tj and no Dk/Sk/Tk, and G came not from metadata, then report...
            If (oneInvariantConvertibleDifference IsNot Nothing OrElse oneInvariantReverseConvertibleDifference IsNot Nothing) AndAlso
                targetType.ContainingModule Is Compilation.SourceModule Then

                Dim oneInvariantDifference As TypeParameterSymbol

                If oneInvariantConvertibleDifference IsNot Nothing Then
                    oneInvariantDifference = oneInvariantConvertibleDifference
                Else
                    oneInvariantDifference = oneInvariantReverseConvertibleDifference
                End If

                Dim containerFormatter As FormattedSymbol

                If oneInvariantDifference.ContainingType.IsDelegateType Then
                    containerFormatter = CustomSymbolDisplayFormatter.DelegateSignature(oneInvariantDifference.ContainingSymbol)
                Else
                    containerFormatter = CustomSymbolDisplayFormatter.ErrorNameWithKind(oneInvariantDifference.ContainingSymbol)
                End If

                If justWarn Then
                    ReportDiagnostic(diagnostics, location, ERRID.WRN_ImplicitConversionSubst1,
                                     ErrorFactory.ErrorInfo(If(oneInvariantConvertibleDifference IsNot Nothing,
                                                               ERRID.WRN_VarianceConversionFailedTryOut4,
                                                               ERRID.WRN_VarianceConversionFailedTryIn4),
                                                            sourceType,
                                                            targetType,
                                                            oneInvariantDifference.Name,
                                                            containerFormatter))
                Else
                    ReportDiagnostic(diagnostics, location, If(oneInvariantConvertibleDifference IsNot Nothing,
                                                               ERRID.ERR_VarianceConversionFailedTryOut4,
                                                               ERRID.ERR_VarianceConversionFailedTryIn4),
                                                            sourceType,
                                                            targetType,
                                                            oneInvariantDifference.Name,
                                                            containerFormatter)
                End If

                Return True
            End If

            Return False
        End Function

        Private Function CreatePredefinedConversion(
            tree As SyntaxNode,
            argument As BoundExpression,
            convKind As ConversionKind,
            isExplicit As Boolean,
            targetType As TypeSymbol,
            diagnostics As BindingDiagnosticBag
        ) As BoundConversion
            Debug.Assert(Conversions.ConversionExists(convKind) AndAlso (convKind And ConversionKind.UserDefined) = 0)

            Dim sourceType = argument.Type

            ' Handle Anonymous Delegate conversion.
            If (convKind And ConversionKind.AnonymousDelegate) <> 0 Then

                ' Don't spend time building a narrowing relaxation stub if we already complained about the narrowing.
                If isExplicit OrElse OptionStrict <> VisualBasic.OptionStrict.On OrElse Conversions.IsWideningConversion(convKind) Then
                    Debug.Assert(Not Conversions.IsIdentityConversion(convKind))
                    Debug.Assert(sourceType.IsDelegateType() AndAlso DirectCast(sourceType, NamedTypeSymbol).IsAnonymousType AndAlso targetType.IsDelegateType() AndAlso
                                 targetType.SpecialType <> SpecialType.System_MulticastDelegate)

                    Dim boundLambdaOpt As BoundLambda = Nothing
                    Dim relaxationReceiverPlaceholderOpt As BoundRValuePlaceholder = Nothing
                    Dim methodToConvert As MethodSymbol = DirectCast(sourceType, NamedTypeSymbol).DelegateInvokeMethod

                    If (convKind And ConversionKind.NeedAStub) <> 0 Then
                        Dim relaxationBinder As Binder

                        ' If conversion is explicit, use Option Strict Off.
                        If isExplicit AndAlso Me.OptionStrict <> VisualBasic.OptionStrict.Off Then
                            relaxationBinder = New OptionStrictOffBinder(Me)
                        Else
                            relaxationBinder = Me
                        End If

                        Debug.Assert(Not isExplicit OrElse relaxationBinder.OptionStrict = VisualBasic.OptionStrict.Off)

                        boundLambdaOpt = relaxationBinder.BuildDelegateRelaxationLambda(tree, tree, argument, methodToConvert,
                                                                                        Nothing, QualificationKind.QualifiedViaValue,
                                                                                        DirectCast(targetType, NamedTypeSymbol).DelegateInvokeMethod,
                                                                                        convKind And ConversionKind.DelegateRelaxationLevelMask,
                                                                                        isZeroArgumentKnownToBeUsed:=False,
                                                                                        warnIfResultOfAsyncMethodIsDroppedDueToRelaxation:=False,
                                                                                        diagnostics:=diagnostics,
                                                                                        relaxationReceiverPlaceholder:=relaxationReceiverPlaceholderOpt)
                    End If

                    ' The conversion has the lambda stored internally to not clutter the bound tree with synthesized nodes 
                    ' in the first pass. Later the node get's rewritten into a delegate creation with the lambda if needed.
                    Return New BoundConversion(tree, argument, convKind, False, isExplicit, Nothing,
                                               If(boundLambdaOpt Is Nothing,
                                                  Nothing,
                                                  New BoundRelaxationLambda(tree, boundLambdaOpt, relaxationReceiverPlaceholderOpt).MakeCompilerGenerated()),
                                               targetType)
                Else
                    Debug.Assert(Not diagnostics.AccumulatesDiagnostics OrElse diagnostics.HasAnyErrors())
                End If
            End If

            Dim integerOverflow As Boolean = False

            Dim constantResult = Conversions.TryFoldConstantConversion(
                                    argument,
                                    targetType,
                                    integerOverflow)

            If constantResult IsNot Nothing Then
                ' Overflow should have been detected at classification time.
                Debug.Assert(Not integerOverflow OrElse Not CheckOverflow)
                Debug.Assert(Not constantResult.IsBad)
            Else
                constantResult = Conversions.TryFoldNothingReferenceConversion(argument, convKind, targetType)
            End If

            Dim tupleElements As BoundConvertedTupleElements = CreateConversionForTupleElements(tree, sourceType, targetType, convKind, isExplicit)

            If (convKind = ConversionKind.WideningReference OrElse convKind = ConversionKind.NarrowingReference) AndAlso
                sourceType.IsWellKnownTypeLock() Then
                ReportDiagnostic(diagnostics, argument.Syntax, ERRID.WRN_ConvertingLock)
            End If

            Return New BoundConversion(tree, argument, convKind, CheckOverflow, isExplicit, constantResult, tupleElements, targetType)
        End Function

        Private Function CreateConversionForTupleElements(
            tree As SyntaxNode,
            sourceType As TypeSymbol,
            targetType As TypeSymbol,
            convKind As ConversionKind,
            isExplicit As Boolean
        ) As BoundConvertedTupleElements

            If (convKind And ConversionKind.Tuple) <> 0 Then
                Dim sourceElementTypes = sourceType.GetNullableUnderlyingTypeOrSelf().GetElementTypesOfTupleOrCompatible()
                Dim targetElementTypes = targetType.GetNullableUnderlyingTypeOrSelf().GetElementTypesOfTupleOrCompatible()

                Dim placeholders = ArrayBuilder(Of BoundRValuePlaceholder).GetInstance(sourceElementTypes.Length)
                Dim converted = ArrayBuilder(Of BoundExpression).GetInstance(sourceElementTypes.Length)

                For i As Integer = 0 To sourceElementTypes.Length - 1
                    Dim placeholder = New BoundRValuePlaceholder(tree, sourceElementTypes(i)).MakeCompilerGenerated()
                    placeholders.Add(placeholder)
                    converted.Add(ApplyConversion(tree, targetElementTypes(i), placeholder, isExplicit, BindingDiagnosticBag.Discarded))
                Next

                Return New BoundConvertedTupleElements(tree, placeholders.ToImmutableAndFree(), converted.ToImmutableAndFree()).MakeCompilerGenerated()
            End If

            Return Nothing
        End Function

        Private Function CreateUserDefinedConversion(
            tree As SyntaxNode,
            argument As BoundExpression,
            convKind As KeyValuePair(Of ConversionKind, MethodSymbol),
            isExplicit As Boolean,
            targetType As TypeSymbol,
            reportArrayLiteralElementNarrowingConversion As Boolean,
            diagnostics As BindingDiagnosticBag
        ) As BoundConversion
            Debug.Assert((convKind.Key And ConversionKind.UserDefined) <> 0 AndAlso convKind.Value IsNot Nothing AndAlso
                         convKind.Value.ParameterCount = 1 AndAlso Not convKind.Value.IsSub AndAlso
                         Not convKind.Value.Parameters(0).IsByRef AndAlso convKind.Value.IsShared)

            ' Suppress any Option Strict diagnostics.
            Dim conversionBinder = New OptionStrictOffBinder(Me)

            Dim argumentSyntax = argument.Syntax
            Dim originalArgumentType As TypeSymbol = argument.Type
            Dim inType As TypeSymbol = convKind.Value.Parameters(0).Type
            Dim outType As TypeSymbol = convKind.Value.ReturnType

            Dim intermediateConv As ConversionKind
            Dim inOutConversionFlags As Byte = 0
            Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)

            If argument.Kind = BoundKind.ArrayLiteral Then
                ' For array literals, report Option Strict diagnostics for each element when reportArrayLiteralElementNarrowingConversion is true.
                Dim arrayLiteral = DirectCast(argument, BoundArrayLiteral)
                Dim arrayLiteralBinder = If(reportArrayLiteralElementNarrowingConversion, Me, conversionBinder)
                intermediateConv = Conversions.ClassifyArrayLiteralConversion(arrayLiteral, inType, arrayLiteralBinder, useSiteInfo)

                argument = arrayLiteralBinder.ReclassifyArrayLiteralExpression(SyntaxKind.CTypeKeyword, tree,
                                                            intermediateConv,
                                                            isExplicit,
                                                            arrayLiteral,
                                                            inType, diagnostics)
                originalArgumentType = inType
            Else
                intermediateConv = Conversions.ClassifyPredefinedConversion(argument, inType, conversionBinder, useSiteInfo)

                If Not Conversions.IsIdentityConversion(intermediateConv) Then
#If DEBUG Then
                    Dim oldArgument = argument
#End If
                    argument = conversionBinder.CreatePredefinedConversion(tree, argument, intermediateConv, isExplicit, inType, diagnostics).
                                                MakeCompilerGenerated()
#If DEBUG Then
                    Debug.Assert(oldArgument IsNot argument AndAlso argument.Kind = BoundKind.Conversion)
#End If
                    inOutConversionFlags = 1
                End If
            End If

            ReportUseSite(diagnostics, tree, convKind.Value)

            ReportDiagnosticsIfObsoleteOrNotSupported(diagnostics, convKind.Value, tree)

            Debug.Assert(convKind.Value.IsUserDefinedOperator())
            If Me.ContainingMember Is convKind.Value Then
                ReportDiagnostic(diagnostics, argumentSyntax, ERRID.WRN_RecursiveOperatorCall, convKind.Value)
            End If

            argument = New BoundCall(tree,
                                     method:=convKind.Value,
                                     methodGroupOpt:=Nothing,
                                     receiverOpt:=Nothing,
                                     arguments:=ImmutableArray.Create(Of BoundExpression)(argument),
                                     constantValueOpt:=Nothing,
                                     suppressObjectClone:=True,
                                     type:=outType).MakeCompilerGenerated()

            intermediateConv = Conversions.ClassifyPredefinedConversion(argument, targetType, conversionBinder, useSiteInfo)

            If Not Conversions.IsIdentityConversion(intermediateConv) Then
#If DEBUG Then
                Dim oldArgument = argument
#End If
                argument = conversionBinder.CreatePredefinedConversion(tree, argument, intermediateConv, isExplicit, targetType, diagnostics).
                                            MakeCompilerGenerated()
#If DEBUG Then
                Debug.Assert(oldArgument IsNot argument AndAlso argument.Kind = BoundKind.Conversion)
#End If
                inOutConversionFlags = inOutConversionFlags Or CByte(2)
            End If

            argument = New BoundUserDefinedConversion(tree, argument, inOutConversionFlags, originalArgumentType).MakeCompilerGenerated()

            diagnostics.Add(tree, useSiteInfo)
            Return New BoundConversion(tree, argument, convKind.Key, CheckOverflow, isExplicit, DirectCast(Nothing, ConstantValue), targetType)
        End Function

        ''' <summary>
        ''' Handle expression reclassification, if any applicable.
        ''' 
        ''' If function returns True, the "argument" parameter has been replaced
        ''' with result of reclassification (possibly an error node) and appropriate
        ''' diagnostic, if any, has been reported.
        ''' 
        ''' If function returns false, the "argument" parameter must be unchanged and no 
        ''' diagnostic should be reported. 
        ''' 
        ''' conversionSemantics can be one of these: 
        '''       SyntaxKind.CTypeKeyword, SyntaxKind.DirectCastKeyword, SyntaxKind.TryCastKeyword
        ''' </summary>
        Private Function ReclassifyExpression(
            ByRef argument As BoundExpression,
            conversionSemantics As SyntaxKind,
            tree As SyntaxNode,
            convKind As ConversionKind,
            isExplicit As Boolean,
            targetType As TypeSymbol,
            diagnostics As BindingDiagnosticBag
        ) As Boolean
            Debug.Assert(argument.Kind <> BoundKind.GroupTypeInferenceLambda)

            Select Case argument.Kind
                Case BoundKind.Parenthesized
                    If argument.Type Is Nothing AndAlso Not argument.IsNothingLiteral() Then
                        Dim parenthesized = DirectCast(argument, BoundParenthesized)
                        Dim enclosed As BoundExpression = parenthesized.Expression

                        ' Reclassify enclosed expression.
                        If ReclassifyExpression(enclosed, conversionSemantics, enclosed.Syntax, convKind, isExplicit, targetType, diagnostics) Then
                            argument = parenthesized.Update(enclosed, enclosed.Type)

                            Return True
                        End If
                    End If

                Case BoundKind.UnboundLambda
                    argument = ReclassifyUnboundLambdaExpression(DirectCast(argument, UnboundLambda), conversionSemantics, tree,
                                                                 convKind, isExplicit, targetType, diagnostics)
                    Return True

                Case BoundKind.QueryLambda

                    argument = ReclassifyQueryLambdaExpression(DirectCast(argument, BoundQueryLambda), conversionSemantics, tree,
                                                                 convKind, isExplicit, targetType, diagnostics)
                    Return True

                Case BoundKind.LateAddressOfOperator
                    Dim addressOfExpression = DirectCast(argument, BoundLateAddressOfOperator)

                    If targetType.TypeKind <> TypeKind.Delegate AndAlso targetType.TypeKind <> TypeKind.Error Then
                        ' 'AddressOf' expression cannot be converted to '{0}' because '{0}' is not a delegate type.
                        ReportDiagnostic(diagnostics, addressOfExpression.Syntax, ERRID.ERR_AddressOfNotDelegate1, targetType)
                    End If

                    argument = addressOfExpression.Update(addressOfExpression.Binder, addressOfExpression.MemberAccess, targetType)
                    Return True

                Case BoundKind.AddressOfOperator
                    Dim delegateResolutionResult As DelegateResolutionResult = Nothing
                    Dim addressOfExpression = DirectCast(argument, BoundAddressOfOperator)
                    If addressOfExpression.GetDelegateResolutionResult(targetType, delegateResolutionResult) Then

                        diagnostics.AddRange(delegateResolutionResult.Diagnostics)
                        Dim hasErrors = True

                        If Conversions.ConversionExists(delegateResolutionResult.DelegateConversions) Then
                            Dim reclassifyBinder As Binder

                            ' If conversion is explicit, use Option Strict Off.
                            If isExplicit AndAlso Me.OptionStrict <> VisualBasic.OptionStrict.Off Then
                                reclassifyBinder = New OptionStrictOffBinder(Me)
                            Else
                                reclassifyBinder = Me
                            End If

                            Debug.Assert(Not isExplicit OrElse reclassifyBinder.OptionStrict = VisualBasic.OptionStrict.Off)

                            argument = reclassifyBinder.ReclassifyAddressOf(addressOfExpression, delegateResolutionResult, targetType, diagnostics, isForHandles:=False,
                                                                            warnIfResultOfAsyncMethodIsDroppedDueToRelaxation:=Not isExplicit AndAlso tree.Kind <> SyntaxKind.ObjectCreationExpression)
                            hasErrors = argument.HasErrors

                            Debug.Assert(convKind = delegateResolutionResult.DelegateConversions)
                        End If

                        If argument.Kind <> BoundKind.DelegateCreationExpression Then
                            If conversionSemantics = SyntaxKind.CTypeKeyword Then
                                argument = New BoundConversion(tree, argument, convKind, False, isExplicit, targetType, hasErrors:=hasErrors)
                            ElseIf conversionSemantics = SyntaxKind.DirectCastKeyword Then
                                argument = New BoundDirectCast(tree, argument, convKind, targetType, hasErrors:=hasErrors)
                            ElseIf conversionSemantics = SyntaxKind.TryCastKeyword Then
                                argument = New BoundTryCast(tree, argument, convKind, targetType, hasErrors:=hasErrors)
                            Else
                                Throw ExceptionUtilities.UnexpectedValue(conversionSemantics)
                            End If
                        End If

                        Return True
                    End If

                Case BoundKind.ArrayLiteral
                    argument = ReclassifyArrayLiteralExpression(conversionSemantics, tree, convKind, isExplicit, DirectCast(argument, BoundArrayLiteral), targetType, diagnostics)
                    Return True

                Case BoundKind.InterpolatedStringExpression

                    argument = ReclassifyInterpolatedStringExpression(conversionSemantics, tree, convKind, isExplicit, DirectCast(argument, BoundInterpolatedStringExpression), targetType, diagnostics)
                    Return argument.Kind = BoundKind.Conversion

                Case BoundKind.TupleLiteral
                    Dim literal = DirectCast(argument, BoundTupleLiteral)
                    argument = ReclassifyTupleLiteral(convKind, tree, isExplicit, literal, targetType, diagnostics)

                    Return argument IsNot literal

            End Select

            Return False
        End Function

        Private Function ReclassifyUnboundLambdaExpression(
            unboundLambda As UnboundLambda,
            conversionSemantics As SyntaxKind,
            tree As SyntaxNode,
            convKind As ConversionKind,
            isExplicit As Boolean,
            targetType As TypeSymbol,
            diagnostics As BindingDiagnosticBag
        ) As BoundExpression

            Dim targetDelegateType As NamedTypeSymbol ' the target delegate type; if targetType is Expression(Of D), then this is D, otherwise targetType or Nothing.
            Dim delegateInvoke As MethodSymbol

            If targetType.IsStrictSupertypeOfConcreteDelegate() Then ' covers Object, System.Delegate, System.MulticastDelegate
                ' Reclassify the lambda as an instance of an Anonymous Delegate.
                Dim anonymousDelegate As BoundExpression = ReclassifyUnboundLambdaExpression(unboundLambda, diagnostics)

#If DEBUG Then
                Dim anonymousDelegateInfo As KeyValuePair(Of NamedTypeSymbol, ReadOnlyBindingDiagnostic(Of AssemblySymbol)) = unboundLambda.InferredAnonymousDelegate

                Debug.Assert(anonymousDelegate.Type Is anonymousDelegateInfo.Key)

                ' If we have errors for the inference, we know that there is no conversion.
                If Not anonymousDelegateInfo.Value.Diagnostics.IsDefault AndAlso anonymousDelegateInfo.Value.Diagnostics.HasAnyErrors() Then
                    Debug.Assert(Conversions.NoConversion(convKind) AndAlso (convKind And ConversionKind.DelegateRelaxationLevelMask) = 0)
                Else
                    Debug.Assert(Conversions.NoConversion(convKind) OrElse
                             (convKind And ConversionKind.DelegateRelaxationLevelMask) >= ConversionKind.DelegateRelaxationLevelWideningToNonLambda)
                End If
#End If
                ' Now convert it to the target type.
                If conversionSemantics = SyntaxKind.CTypeKeyword Then
                    Return ApplyConversion(tree, targetType, anonymousDelegate, isExplicit, diagnostics)
                ElseIf conversionSemantics = SyntaxKind.DirectCastKeyword Then
                    Return ApplyDirectCastConversion(tree, anonymousDelegate, targetType, diagnostics)
                ElseIf conversionSemantics = SyntaxKind.TryCastKeyword Then
                    Return ApplyTryCastConversion(tree, anonymousDelegate, targetType, diagnostics)
                Else
                    Throw ExceptionUtilities.UnexpectedValue(conversionSemantics)
                End If
            Else
                targetDelegateType = targetType.DelegateOrExpressionDelegate(Me)

                If targetDelegateType Is Nothing Then
                    Debug.Assert((convKind And ConversionKind.DelegateRelaxationLevelMask) = 0)
                    ReportDiagnostic(diagnostics, unboundLambda.Syntax, ERRID.ERR_LambdaNotDelegate1, targetType)
                    delegateInvoke = Nothing ' No conversion
                Else
                    delegateInvoke = targetDelegateType.DelegateInvokeMethod

                    If delegateInvoke Is Nothing Then
                        ReportDiagnostic(diagnostics, unboundLambda.Syntax, ERRID.ERR_LambdaNotDelegate1, targetDelegateType)
                        delegateInvoke = Nothing ' No conversion
                    ElseIf ReportDelegateInvokeUseSite(diagnostics, unboundLambda.Syntax, targetDelegateType, delegateInvoke) Then
                        delegateInvoke = Nothing ' No conversion

                    ElseIf unboundLambda.IsInferredDelegateForThisLambda(delegateInvoke.ContainingType) Then
                        Dim inferenceDiagnostics As ReadOnlyBindingDiagnostic(Of AssemblySymbol) = unboundLambda.InferredAnonymousDelegate.Value

                        diagnostics.AddRange(inferenceDiagnostics)

                        If Not inferenceDiagnostics.Diagnostics.IsDefaultOrEmpty AndAlso inferenceDiagnostics.Diagnostics.HasAnyErrors() Then
                            delegateInvoke = Nothing ' No conversion
                        End If
                    End If

                    Debug.Assert(delegateInvoke IsNot Nothing OrElse (convKind And ConversionKind.DelegateRelaxationLevelMask) = 0)
                End If
            End If

            Dim boundLambda As BoundLambda = Nothing

            If delegateInvoke IsNot Nothing Then
                boundLambda = unboundLambda.GetBoundLambda(New UnboundLambda.TargetSignature(delegateInvoke))

                Debug.Assert(boundLambda IsNot Nothing)
                If boundLambda Is Nothing Then
                    ' An unlikely case.
                    Debug.Assert((convKind And ConversionKind.DelegateRelaxationLevelMask) = 0)
                    ReportDiagnostic(diagnostics,
                                     unboundLambda.Syntax,
                                     If(unboundLambda.IsFunctionLambda, ERRID.ERR_LambdaBindingMismatch1, ERRID.ERR_LambdaBindingMismatch2),
                                     If(targetDelegateType.TypeKind = TypeKind.Delegate AndAlso targetDelegateType.IsFromCompilation(Me.Compilation),
                                        CType(CustomSymbolDisplayFormatter.DelegateSignature(targetDelegateType), Object),
                                        CType(targetDelegateType, Object)))
                End If
            End If

            If boundLambda Is Nothing Then
                Debug.Assert(Conversions.NoConversion(convKind))

                Dim errorRecovery As BoundLambda = unboundLambda.BindForErrorRecovery()

                If conversionSemantics = SyntaxKind.CTypeKeyword Then
                    Return New BoundConversion(tree, errorRecovery, convKind, False, isExplicit, targetType, hasErrors:=True)
                ElseIf conversionSemantics = SyntaxKind.DirectCastKeyword Then
                    Return New BoundDirectCast(tree, errorRecovery, convKind, targetType, hasErrors:=True)
                ElseIf conversionSemantics = SyntaxKind.TryCastKeyword Then
                    Return New BoundTryCast(tree, errorRecovery, convKind, targetType, hasErrors:=True)
                Else
                    Throw ExceptionUtilities.UnexpectedValue(conversionSemantics)
                End If
            End If

            Dim boundLambdaDiagnostics = boundLambda.Diagnostics

            Debug.Assert((convKind And ConversionKind.DelegateRelaxationLevelMask) >= boundLambda.DelegateRelaxation)
            Debug.Assert(Conversions.ClassifyMethodConversionForLambdaOrAnonymousDelegate(delegateInvoke, boundLambda.LambdaSymbol, CompoundUseSiteInfo(Of AssemblySymbol).Discarded) = MethodConversionKind.Identity OrElse
                         ((convKind And ConversionKind.DelegateRelaxationLevelMask) <> ConversionKind.DelegateRelaxationLevelNone AndAlso
                          boundLambda.MethodConversionKind <> MethodConversionKind.Identity))

            Dim reportedAnError As Boolean = boundLambdaDiagnostics.Diagnostics.HasAnyErrors()

            diagnostics.AddRange(boundLambdaDiagnostics)

            Dim relaxationLambdaOpt As BoundLambda = Nothing

            If (convKind And ConversionKind.DelegateRelaxationLevelMask) = ConversionKind.DelegateRelaxationLevelInvalid AndAlso
                Not reportedAnError AndAlso Not boundLambda.HasErrors Then

                ' We don't try to infer return type of a lambda that has both Async and Iterator modifiers, let's suppress the 
                ' signature mismatch error in this case.
                If unboundLambda.ReturnType IsNot Nothing OrElse unboundLambda.Flags <> (SourceMemberFlags.Async Or SourceMemberFlags.Iterator) Then
                    Dim err As ERRID

                    If unboundLambda.IsFunctionLambda Then
                        err = ERRID.ERR_LambdaBindingMismatch1
                    Else
                        err = ERRID.ERR_LambdaBindingMismatch2
                    End If

                    ReportDiagnostic(diagnostics,
                                     unboundLambda.Syntax,
                                     err,
                                     If(targetDelegateType.TypeKind = TypeKind.Delegate AndAlso targetDelegateType.IsFromCompilation(Me.Compilation),
                                        CType(CustomSymbolDisplayFormatter.DelegateSignature(targetDelegateType), Object),
                                        CType(targetDelegateType, Object)))
                End If
            ElseIf Conversions.IsStubRequiredForMethodConversion(boundLambda.MethodConversionKind) Then
                Debug.Assert(Conversions.IsDelegateRelaxationSupportedFor(boundLambda.MethodConversionKind))

                ' Need to produce a stub.

                ' First, we need to get an Anonymous Delegate of the same shape as the lambdaSymbol.
                Dim lambdaSymbol As LambdaSymbol = boundLambda.LambdaSymbol
                Dim anonymousDelegateType As NamedTypeSymbol = ConstructAnonymousDelegateSymbol(unboundLambda,
                                                                                       (lambdaSymbol.Parameters.As(Of BoundLambdaParameterSymbol)),
                                                                                       lambdaSymbol.ReturnType,
                                                                                       diagnostics)

                ' Second, reclassify the bound lambda as an instance of the Anonymous Delegate.
                Dim anonymousDelegateInstance = New BoundConversion(tree, boundLambda, ConversionKind.Widening Or ConversionKind.Lambda,
                                                                    False, False, anonymousDelegateType)
                anonymousDelegateInstance.SetWasCompilerGenerated()

                ' Third, create a method group representing Invoke method of the instance of the Anonymous Delegate.
                Dim methodGroup = New BoundMethodGroup(unboundLambda.Syntax,
                                                       Nothing,
                                                       ImmutableArray.Create(anonymousDelegateType.DelegateInvokeMethod),
                                                       LookupResultKind.Good,
                                                       anonymousDelegateInstance,
                                                       QualificationKind.QualifiedViaValue)
                methodGroup.SetWasCompilerGenerated()

                ' Fourth, create a lambda with the shape of the target delegate that calls the Invoke with appropriate conversions
                ' and drops parameters and/or return value, if needed, thus performing the relaxation.

                Dim relaxationBinder As Binder

                ' If conversion is explicit, use Option Strict Off.
                If isExplicit AndAlso Me.OptionStrict <> VisualBasic.OptionStrict.Off Then
                    relaxationBinder = New OptionStrictOffBinder(Me)
                Else
                    relaxationBinder = Me
                End If

                Debug.Assert(Not isExplicit OrElse relaxationBinder.OptionStrict = VisualBasic.OptionStrict.Off)

                relaxationLambdaOpt = relaxationBinder.BuildDelegateRelaxationLambda(unboundLambda.Syntax,
                                                                                     delegateInvoke,
                                                                                     methodGroup,
                                                                                     boundLambda.DelegateRelaxation,
                                                                                     isZeroArgumentKnownToBeUsed:=False,
                                                                                     warnIfResultOfAsyncMethodIsDroppedDueToRelaxation:=Not isExplicit AndAlso tree.Kind <> SyntaxKind.ObjectCreationExpression,
                                                                                     diagnostics:=diagnostics)
            End If

            If conversionSemantics = SyntaxKind.CTypeKeyword Then
                Return New BoundConversion(tree, boundLambda, convKind, False, isExplicit, Nothing,
                                           If(relaxationLambdaOpt Is Nothing,
                                              Nothing,
                                              New BoundRelaxationLambda(tree, relaxationLambdaOpt, receiverPlaceholderOpt:=Nothing).MakeCompilerGenerated()),
                                           targetType)
            ElseIf conversionSemantics = SyntaxKind.DirectCastKeyword Then
                Return New BoundDirectCast(tree, boundLambda, convKind, relaxationLambdaOpt, targetType)
            ElseIf conversionSemantics = SyntaxKind.TryCastKeyword Then
                Return New BoundTryCast(tree, boundLambda, convKind, relaxationLambdaOpt, targetType)
            Else
                Throw ExceptionUtilities.UnexpectedValue(conversionSemantics)
            End If
        End Function

        Private Function ReclassifyQueryLambdaExpression(
            lambda As BoundQueryLambda,
            conversionSemantics As SyntaxKind,
            tree As SyntaxNode,
            convKind As ConversionKind,
            isExplicit As Boolean,
            targetType As TypeSymbol,
            diagnostics As BindingDiagnosticBag
        ) As BoundExpression
            Debug.Assert(lambda.Type Is Nothing)

            ' the target delegate type; if targetType is Expression(Of D), then this is D, otherwise targetType or Nothing.
            Dim targetDelegateType As NamedTypeSymbol = targetType.DelegateOrExpressionDelegate(Me)

            If Conversions.NoConversion(convKind) Then

                If targetType.IsStrictSupertypeOfConcreteDelegate() AndAlso Not targetType.IsObjectType() Then
                    ReportDiagnostic(diagnostics, lambda.Syntax, ERRID.ERR_LambdaNotCreatableDelegate1, targetType)
                Else
                    If targetDelegateType Is Nothing Then
                        ReportDiagnostic(diagnostics, lambda.Syntax, ERRID.ERR_LambdaNotDelegate1, targetType)
                    Else
                        Dim invoke As MethodSymbol = targetDelegateType.DelegateInvokeMethod

                        If invoke Is Nothing Then
                            ReportDiagnostic(diagnostics, lambda.Syntax, ERRID.ERR_LambdaNotDelegate1, targetDelegateType)
                        ElseIf Not ReportDelegateInvokeUseSite(diagnostics, lambda.Syntax, targetDelegateType, invoke) Then

                            ' Conversion could fail because we couldn't convert body of the lambda
                            ' to the target delegate type. We want to report that error instead of
                            ' lambda signature mismatch.
                            If lambda.LambdaSymbol.ReturnType Is LambdaSymbol.ReturnTypePendingDelegate AndAlso
                               Not invoke.IsSub AndAlso
                               Conversions.FailedDueToQueryLambdaBodyMismatch(convKind) Then

                                lambda = lambda.Update(lambda.LambdaSymbol, lambda.RangeVariables,
                                                       ApplyImplicitConversion(lambda.Expression.Syntax,
                                                                               invoke.ReturnType,
                                                                               lambda.Expression,
                                                                               diagnostics,
                                                                               If(invoke.ReturnType.IsBooleanType,
                                                                                  lambda.ExprIsOperandOfConditionalBranch,
                                                                                  False)),
                                                       exprIsOperandOfConditionalBranch:=False)

                            Else
                                ReportDiagnostic(diagnostics, lambda.Syntax, ERRID.ERR_LambdaBindingMismatch1, targetDelegateType)
                            End If
                        End If
                    End If
                End If

                If conversionSemantics = SyntaxKind.CTypeKeyword Then
                    Return New BoundConversion(tree, lambda, convKind, False, isExplicit, targetType, hasErrors:=True).MakeCompilerGenerated()
                ElseIf conversionSemantics = SyntaxKind.DirectCastKeyword Then
                    Return New BoundDirectCast(tree, lambda, convKind, targetType, hasErrors:=True).MakeCompilerGenerated()
                ElseIf conversionSemantics = SyntaxKind.TryCastKeyword Then
                    Return New BoundTryCast(tree, lambda, convKind, targetType, hasErrors:=True).MakeCompilerGenerated()
                End If

                Throw ExceptionUtilities.UnexpectedValue(conversionSemantics)
            End If

            Dim delegateInvoke As MethodSymbol = targetDelegateType.DelegateInvokeMethod

            For Each delegateParam As ParameterSymbol In delegateInvoke.Parameters
                If delegateParam.IsByRef OrElse delegateParam.OriginalDefinition.Type.IsTypeParameter() Then
                    Dim restrictedType As TypeSymbol = Nothing
                    If delegateParam.Type.IsRestrictedTypeOrArrayType(restrictedType) Then
                        ReportDiagnostic(diagnostics, lambda.LambdaSymbol.Parameters(delegateParam.Ordinal).Locations(0),
                                         ERRID.ERR_RestrictedType1, restrictedType)
                    End If
                End If
            Next

            Dim delegateReturnType As TypeSymbol = delegateInvoke.ReturnType

            If delegateInvoke.OriginalDefinition.ReturnType.IsTypeParameter() Then
                Dim restrictedType As TypeSymbol = Nothing
                If delegateReturnType.IsRestrictedTypeOrArrayType(restrictedType) Then
                    Dim location As SyntaxNode

                    If lambda.Expression.Kind = BoundKind.RangeVariableAssignment Then
                        location = DirectCast(lambda.Expression, BoundRangeVariableAssignment).Value.Syntax
                    Else
                        location = lambda.Expression.Syntax
                    End If

                    ReportDiagnostic(diagnostics, location, ERRID.ERR_RestrictedType1, restrictedType)
                End If
            End If

            If lambda.LambdaSymbol.ReturnType Is LambdaSymbol.ReturnTypePendingDelegate Then
                lambda = lambda.Update(lambda.LambdaSymbol, lambda.RangeVariables,
                              ApplyImplicitConversion(lambda.Expression.Syntax, delegateReturnType, lambda.Expression,
                                                      diagnostics,
                                                      If(delegateReturnType.IsBooleanType(), lambda.ExprIsOperandOfConditionalBranch, False)),
                              exprIsOperandOfConditionalBranch:=False)
            Else
                lambda = lambda.Update(lambda.LambdaSymbol, lambda.RangeVariables,
                              lambda.Expression, exprIsOperandOfConditionalBranch:=False)
            End If

            If conversionSemantics = SyntaxKind.CTypeKeyword Then
                Return New BoundConversion(tree, lambda, convKind, False, isExplicit, targetType).MakeCompilerGenerated()
            ElseIf conversionSemantics = SyntaxKind.DirectCastKeyword Then
                Return New BoundDirectCast(tree, lambda, convKind, targetType).MakeCompilerGenerated()
            ElseIf conversionSemantics = SyntaxKind.TryCastKeyword Then
                Return New BoundTryCast(tree, lambda, convKind, targetType).MakeCompilerGenerated()
            End If

            Throw ExceptionUtilities.UnexpectedValue(conversionSemantics)
        End Function

        Private Function ReclassifyInterpolatedStringExpression(conversionSemantics As SyntaxKind, tree As SyntaxNode, convKind As ConversionKind, isExplicit As Boolean, node As BoundInterpolatedStringExpression, targetType As TypeSymbol, diagnostics As BindingDiagnosticBag) As BoundExpression

            If (convKind And ConversionKind.InterpolatedString) = ConversionKind.InterpolatedString Then
                Debug.Assert(targetType.Equals(Compilation.GetWellKnownType(WellKnownType.System_IFormattable)) OrElse targetType.Equals(Compilation.GetWellKnownType(WellKnownType.System_FormattableString)))
                Return New BoundConversion(tree, node, ConversionKind.InterpolatedString, False, isExplicit, targetType)
            End If

            Return node

        End Function

        Private Function ReclassifyTupleLiteral(
                       convKind As ConversionKind,
                       tree As SyntaxNode,
                       isExplicit As Boolean,
                       sourceTuple As BoundTupleLiteral,
                       destination As TypeSymbol,
                       diagnostics As BindingDiagnosticBag) As BoundExpression

            ' We have a successful tuple conversion rather than producing a separate conversion node 
            ' which is a conversion on top of a tuple literal, tuple conversion is an element-wise conversion of arguments.
            Dim isNullableTupleConversion = (convKind And ConversionKind.Nullable) <> 0
            Debug.Assert(Not isNullableTupleConversion OrElse destination.IsNullableType())

            Dim targetType = destination

            If isNullableTupleConversion Then
                targetType = destination.GetNullableUnderlyingType()
            End If

            Dim arguments = sourceTuple.Arguments
            If Not targetType.IsTupleOrCompatibleWithTupleOfCardinality(arguments.Length) Then
                Return sourceTuple
            End If

            If targetType.IsTupleType Then
                Dim destTupleType = DirectCast(targetType, TupleTypeSymbol)

                TupleTypeSymbol.ReportNamesMismatchesIfAny(targetType, sourceTuple, diagnostics)

                ' do not lose the original element names in the literal if different from names in the target
                ' Come back to this, what about locations? (https:'github.com/dotnet/roslyn/issues/11013)
                targetType = destTupleType.WithElementNames(sourceTuple.ArgumentNamesOpt)
            End If

            Dim convertedArguments = ArrayBuilder(Of BoundExpression).GetInstance(arguments.Length)
            Dim targetElementTypes As ImmutableArray(Of TypeSymbol) = targetType.GetElementTypesOfTupleOrCompatible()
            Debug.Assert(targetElementTypes.Length = arguments.Length, "converting a tuple literal to incompatible type?")

            For i As Integer = 0 To arguments.Length - 1
                Dim argument = arguments(i)
                Dim destType = targetElementTypes(i)

                convertedArguments.Add(ApplyConversion(argument.Syntax, destType, argument, isExplicit, diagnostics))
            Next

            Dim result As BoundExpression = New BoundConvertedTupleLiteral(
                sourceTuple.Syntax,
                sourceTuple.Type,
                convertedArguments.ToImmutableAndFree(),
                targetType)

            If Not TypeSymbol.Equals(sourceTuple.Type, destination, TypeCompareKind.ConsiderEverything) AndAlso convKind <> Nothing Then
                ' literal cast is applied to the literal 
                result = New BoundConversion(sourceTuple.Syntax, result, convKind, checked:=False, explicitCastInCode:=isExplicit, type:=destination)
            End If

            ' If we had a cast in the code, keep conversion in the tree.
            ' even though the literal is already converted to the target type.
            If isExplicit Then
                result = New BoundConversion(
                    tree,
                    result,
                    ConversionKind.Identity,
                    checked:=False,
                    explicitCastInCode:=isExplicit,
                    type:=destination)
            End If

            Return result
        End Function

        Private Sub WarnOnNarrowingConversionBetweenSealedClassAndAnInterface(
            convKind As ConversionKind,
            location As SyntaxNode,
            sourceType As TypeSymbol,
            targetType As TypeSymbol,
            diagnostics As BindingDiagnosticBag
        )
            If Conversions.IsNarrowingConversion(convKind) Then
                Dim interfaceType As TypeSymbol = Nothing
                Dim classType As NamedTypeSymbol = Nothing

                If sourceType.IsInterfaceType() Then
                    If targetType.IsClassType() Then
                        interfaceType = sourceType
                        classType = DirectCast(targetType, NamedTypeSymbol)
                    End If
                ElseIf sourceType.IsClassType() AndAlso targetType.IsInterfaceType() Then
                    interfaceType = targetType
                    classType = DirectCast(sourceType, NamedTypeSymbol)
                End If

                Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)

                If classType IsNot Nothing AndAlso
                    interfaceType IsNot Nothing AndAlso
                    classType.IsNotInheritable AndAlso
                    Not classType.IsComImport() AndAlso
                    Not Conversions.IsWideningConversion(Conversions.ClassifyDirectCastConversion(classType, interfaceType, useSiteInfo)) Then
                    ' Report specific warning if converting IEnumerable(Of XElement) to String.
                    If (targetType.SpecialType = SpecialType.System_String) AndAlso IsIEnumerableOfXElement(sourceType, useSiteInfo) Then
                        ReportDiagnostic(diagnostics, location, ERRID.WRN_UseValueForXmlExpression3, sourceType, targetType, sourceType)
                    Else
                        ReportDiagnostic(diagnostics, location, ERRID.WRN_InterfaceConversion2, sourceType, targetType)
                    End If
                End If

                diagnostics.AddDependencies(useSiteInfo)
            End If
        End Sub

        Private Shared Sub WarnOnLockConversion(sourceType As TypeSymbol, syntax As SyntaxNode, diagnostics As BindingDiagnosticBag)
            If sourceType IsNot Nothing AndAlso sourceType.IsWellKnownTypeLock() Then
                ReportDiagnostic(diagnostics, syntax, ERRID.WRN_ConvertingLock)
            End If
        End Sub

        Private Function IsIEnumerableOfXElement(type As TypeSymbol, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As Boolean
            Return type.IsOrImplementsIEnumerableOfXElement(Compilation, useSiteInfo)
        End Function

        Private Sub ReportNoConversionError(
            location As SyntaxNode,
            sourceType As TypeSymbol,
            targetType As TypeSymbol,
            diagnostics As BindingDiagnosticBag,
            Optional copybackConversionParamName As String = Nothing
        )
            If sourceType.IsArrayType() AndAlso targetType.IsArrayType() Then

                Dim sourceArray = DirectCast(sourceType, ArrayTypeSymbol)
                Dim targetArray = DirectCast(targetType, ArrayTypeSymbol)
                Dim sourceElement = sourceArray.ElementType
                Dim targetElement = targetArray.ElementType

                If sourceArray.Rank <> targetArray.Rank Then
                    ReportDiagnostic(diagnostics, location, ERRID.ERR_ConvertArrayRankMismatch2, sourceType, targetType)

                ElseIf sourceArray.IsSZArray <> targetArray.IsSZArray Then
                    ReportDiagnostic(diagnostics, location, ERRID.ERR_TypeMismatch2, sourceType, targetType)

                ElseIf Not (sourceElement.IsErrorType() OrElse targetElement.IsErrorType()) Then
                    Dim elemConv = Conversions.ClassifyDirectCastConversion(sourceElement, targetElement, CompoundUseSiteInfo(Of AssemblySymbol).Discarded)

                    If Not Conversions.IsIdentityConversion(elemConv) AndAlso
                       (targetElement.IsObjectType() OrElse targetElement.SpecialType = SpecialType.System_ValueType) AndAlso
                       Not sourceElement.IsReferenceType() Then

                        ReportDiagnostic(diagnostics, location, ERRID.ERR_ConvertObjectArrayMismatch3, sourceType, targetType, sourceElement)

                    ElseIf Not Conversions.IsIdentityConversion(elemConv) AndAlso
                        Not (Conversions.IsWideningConversion(elemConv) AndAlso
                             (elemConv And (ConversionKind.Reference Or ConversionKind.Value Or ConversionKind.TypeParameter)) <> 0) Then
                        ReportDiagnostic(diagnostics, location, ERRID.ERR_ConvertArrayMismatch4, sourceType, targetType, sourceElement, targetElement)

                    Else
                        ReportDiagnostic(diagnostics, location, ERRID.ERR_TypeMismatch2, sourceType, targetType)
                    End If
                End If

            ElseIf sourceType.IsDateTimeType() AndAlso targetType.IsDoubleType() Then
                ReportDiagnostic(diagnostics, location, ERRID.ERR_DateToDoubleConversion)

            ElseIf targetType.IsDateTimeType() AndAlso sourceType.IsDoubleType() Then
                ReportDiagnostic(diagnostics, location, ERRID.ERR_DoubleToDateConversion)

            ElseIf targetType.IsCharType() AndAlso sourceType.IsIntegralType() Then
                ReportDiagnostic(diagnostics, location, ERRID.ERR_IntegralToCharTypeMismatch1, sourceType)

            ElseIf sourceType.IsCharType() AndAlso targetType.IsIntegralType() Then
                ReportDiagnostic(diagnostics, location, ERRID.ERR_CharToIntegralTypeMismatch1, targetType)

            ElseIf copybackConversionParamName IsNot Nothing Then
                ReportDiagnostic(diagnostics, location, ERRID.ERR_CopyBackTypeMismatch3,
                                 copybackConversionParamName, sourceType, targetType)

            ElseIf sourceType.IsInterfaceType() AndAlso targetType.IsValueType() AndAlso IsIEnumerableOfXElement(sourceType, CompoundUseSiteInfo(Of AssemblySymbol).Discarded) Then
                ReportDiagnostic(diagnostics, location, ERRID.ERR_TypeMismatchForXml3, sourceType, targetType, sourceType)

            Else
                ReportDiagnostic(diagnostics, location, ERRID.ERR_TypeMismatch2, sourceType, targetType)
            End If
        End Sub

    End Class

End Namespace
