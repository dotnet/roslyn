' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts

Namespace Microsoft.CodeAnalysis.VisualBasic

    ' This portion of the binder converts an ExpressionSyntax into a BoundExpression

    Partial Friend Class Binder

        ' !!! PLEASE KEEP BindExpression FUNCTION AT THE TOP !!!

        Public Function BindExpression(
            node As ExpressionSyntax,
            diagnostics As BindingDiagnosticBag
        ) As BoundExpression
            Return BindExpression(node, False, False, False, diagnostics)
        End Function

        ''' <summary>
        ''' The dispatcher method that handles syntax nodes for all stand-alone expressions.
        ''' </summary>
        Public Function BindExpression(
            node As ExpressionSyntax,
            isInvocationOrAddressOf As Boolean,
            isOperandOfConditionalBranch As Boolean,
            eventContext As Boolean,
            diagnostics As BindingDiagnosticBag
        ) As BoundExpression

            If IsEarlyAttributeBinder AndAlso Not EarlyWellKnownAttributeBinder.CanBeValidAttributeArgument(node, Me) Then
                Return BadExpression(node, ErrorTypeSymbol.UnknownResultType)
            End If

            Select Case node.Kind
                Case SyntaxKind.MeExpression
                    Return BindMeExpression(DirectCast(node, MeExpressionSyntax), diagnostics)

                Case SyntaxKind.MyBaseExpression
                    Return BindMyBaseExpression(DirectCast(node, MyBaseExpressionSyntax), diagnostics)

                Case SyntaxKind.MyClassExpression
                    Return BindMyClassExpression(DirectCast(node, MyClassExpressionSyntax), diagnostics)

                Case SyntaxKind.IdentifierName, SyntaxKind.GenericName
                    Return BindSimpleName(DirectCast(node, SimpleNameSyntax), isInvocationOrAddressOf, diagnostics)

                Case SyntaxKind.PredefinedType, SyntaxKind.NullableType
                    Return BindNamespaceOrTypeExpression(DirectCast(node, TypeSyntax), diagnostics)

                Case SyntaxKind.SimpleMemberAccessExpression
                    Return BindMemberAccess(DirectCast(node, MemberAccessExpressionSyntax), eventContext, diagnostics:=diagnostics)

                Case SyntaxKind.DictionaryAccessExpression
                    Return BindDictionaryAccess(DirectCast(node, MemberAccessExpressionSyntax), diagnostics)

                Case SyntaxKind.InvocationExpression
                    Return BindInvocationExpression(DirectCast(node, InvocationExpressionSyntax), diagnostics)

                Case SyntaxKind.CollectionInitializer
                    Return BindArrayLiteralExpression(DirectCast(node, CollectionInitializerSyntax), diagnostics)

                Case SyntaxKind.AnonymousObjectCreationExpression
                    Return BindAnonymousObjectCreationExpression(DirectCast(node, AnonymousObjectCreationExpressionSyntax), diagnostics)

                Case SyntaxKind.ArrayCreationExpression
                    Return BindArrayCreationExpression(DirectCast(node, ArrayCreationExpressionSyntax), diagnostics)

                Case SyntaxKind.ObjectCreationExpression
                    Return BindObjectCreationExpression(DirectCast(node, ObjectCreationExpressionSyntax), diagnostics)

                Case SyntaxKind.NumericLiteralExpression,
                     SyntaxKind.StringLiteralExpression,
                     SyntaxKind.CharacterLiteralExpression,
                     SyntaxKind.TrueLiteralExpression,
                     SyntaxKind.FalseLiteralExpression,
                     SyntaxKind.NothingLiteralExpression,
                     SyntaxKind.DateLiteralExpression
                    Return BindLiteralConstant(DirectCast(node, LiteralExpressionSyntax), diagnostics)

                Case SyntaxKind.ParenthesizedExpression
                    ' Parenthesis tokens are ignored, and operand is bound in the context of
                    ' parent expression.

                    ' Dev10 allows parenthesized type expressions, let's bind as a general expression first.
                    Dim operand As BoundExpression = BindExpression(DirectCast(node, ParenthesizedExpressionSyntax).Expression,
                                                                    isInvocationOrAddressOf:=False,
                                                                    isOperandOfConditionalBranch:=isOperandOfConditionalBranch,
                                                                    eventContext, diagnostics)

                    If operand.Kind = BoundKind.TypeExpression Then
                        Dim asType = DirectCast(operand, BoundTypeExpression)
                        Return New BoundTypeExpression(node, asType.UnevaluatedReceiverOpt, asType.AliasOpt, operand.Type, operand.HasErrors)

                    ElseIf operand.Kind = BoundKind.ArrayLiteral Then
                        ' Convert the array literal to its inferred array type, reporting warnings/errors if necessary.
                        ' It would have been nice to put this reclassification in ReclassifyAsValue, however, that is called in too many situations.  We only
                        ' want to reclassify the array literal this early when it is within parentheses. 
                        Dim arrayLiteral = DirectCast(operand, BoundArrayLiteral)
                        Dim reclassified = ReclassifyArrayLiteralExpression(SyntaxKind.CTypeKeyword, arrayLiteral.Syntax, ConversionKind.Widening, False, arrayLiteral, arrayLiteral.InferredType, diagnostics)
                        Return New BoundParenthesized(node, reclassified, reclassified.Type)
                    Else
                        Return New BoundParenthesized(node, operand, operand.Type)
                    End If

                Case SyntaxKind.UnaryPlusExpression,
                     SyntaxKind.UnaryMinusExpression,
                     SyntaxKind.NotExpression
                    Return BindUnaryOperator(DirectCast(node, UnaryExpressionSyntax), diagnostics)

                Case SyntaxKind.AddExpression,
                     SyntaxKind.ConcatenateExpression,
                     SyntaxKind.LikeExpression,
                     SyntaxKind.EqualsExpression,
                     SyntaxKind.NotEqualsExpression,
                     SyntaxKind.LessThanOrEqualExpression,
                     SyntaxKind.GreaterThanOrEqualExpression,
                     SyntaxKind.LessThanExpression,
                     SyntaxKind.GreaterThanExpression,
                     SyntaxKind.SubtractExpression,
                     SyntaxKind.MultiplyExpression,
                     SyntaxKind.ExponentiateExpression,
                     SyntaxKind.DivideExpression,
                     SyntaxKind.ModuloExpression,
                     SyntaxKind.IntegerDivideExpression,
                     SyntaxKind.LeftShiftExpression,
                     SyntaxKind.RightShiftExpression,
                     SyntaxKind.ExclusiveOrExpression,
                     SyntaxKind.OrExpression,
                     SyntaxKind.OrElseExpression,
                     SyntaxKind.AndExpression,
                     SyntaxKind.AndAlsoExpression

                    Return BindBinaryOperator(DirectCast(node, BinaryExpressionSyntax), isOperandOfConditionalBranch, diagnostics)

                Case SyntaxKind.IsExpression,
                     SyntaxKind.IsNotExpression

                    Return BindIsExpression(DirectCast(node, BinaryExpressionSyntax), diagnostics)

                Case SyntaxKind.GetTypeExpression
                    Return BindGetTypeExpression(DirectCast(node, GetTypeExpressionSyntax), diagnostics)

                Case SyntaxKind.NameOfExpression
                    Return BindNameOfExpression(DirectCast(node, NameOfExpressionSyntax), diagnostics)

                Case SyntaxKind.AddressOfExpression
                    Return BindAddressOfExpression(node, diagnostics)

                Case SyntaxKind.CTypeExpression,
                     SyntaxKind.TryCastExpression,
                     SyntaxKind.DirectCastExpression
                    Return BindCastExpression(DirectCast(node, CastExpressionSyntax), diagnostics)

                Case SyntaxKind.PredefinedCastExpression
                    Return BindPredefinedCastExpression(DirectCast(node, PredefinedCastExpressionSyntax), diagnostics)

                Case SyntaxKind.TypeOfIsExpression,
                     SyntaxKind.TypeOfIsNotExpression

                    Return BindTypeOfExpression(DirectCast(node, TypeOfExpressionSyntax), diagnostics)

                Case SyntaxKind.BinaryConditionalExpression
                    Return BindBinaryConditionalExpression(DirectCast(node, BinaryConditionalExpressionSyntax), diagnostics)

                Case SyntaxKind.TernaryConditionalExpression
                    Return BindTernaryConditionalExpression(DirectCast(node, TernaryConditionalExpressionSyntax), diagnostics)

                Case SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression,
                     SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression
                    Return BindLambdaExpression(DirectCast(node, LambdaExpressionSyntax), diagnostics)

                Case SyntaxKind.GlobalName
                    Return New BoundNamespaceExpression(node, Nothing, Compilation.GlobalNamespace)

                Case SyntaxKind.QueryExpression
                    Return BindQueryExpression(DirectCast(node, QueryExpressionSyntax), diagnostics)

                Case SyntaxKind.GroupAggregation
                    Return BindGroupAggregationExpression(DirectCast(node, GroupAggregationSyntax), diagnostics)

                Case SyntaxKind.FunctionAggregation
                    Return BindFunctionAggregationExpression(DirectCast(node, FunctionAggregationSyntax), diagnostics)

                Case SyntaxKind.NextLabel,
                    SyntaxKind.NumericLabel,
                    SyntaxKind.IdentifierLabel
                    Return BindLabel(DirectCast(node, LabelSyntax), diagnostics)

                Case SyntaxKind.QualifiedName
                    ' This code is not used during method body binding, but it might be used by SemanticModel for erroneous cases.
                    Return BindQualifiedName(DirectCast(node, QualifiedNameSyntax), diagnostics)

                Case SyntaxKind.GetXmlNamespaceExpression
                    Return BindGetXmlNamespace(DirectCast(node, GetXmlNamespaceExpressionSyntax), diagnostics)

                Case SyntaxKind.XmlComment
                    Return BindXmlComment(DirectCast(node, XmlCommentSyntax), rootInfoOpt:=Nothing, diagnostics:=diagnostics)

                Case SyntaxKind.XmlDocument
                    Return BindXmlDocument(DirectCast(node, XmlDocumentSyntax), diagnostics)

                Case SyntaxKind.XmlProcessingInstruction
                    Return BindXmlProcessingInstruction(DirectCast(node, XmlProcessingInstructionSyntax), diagnostics)

                Case SyntaxKind.XmlEmptyElement
                    Return BindXmlEmptyElement(DirectCast(node, XmlEmptyElementSyntax), rootInfoOpt:=Nothing, diagnostics:=diagnostics)

                Case SyntaxKind.XmlElement
                    Return BindXmlElement(DirectCast(node, XmlElementSyntax), rootInfoOpt:=Nothing, diagnostics:=diagnostics)

                Case SyntaxKind.XmlEmbeddedExpression
                    ' This case handles embedded expressions that are outside of XML
                    ' literals (error cases). The parser will have reported BC31172
                    ' already, so no error is reported here. (Valid uses of embedded
                    ' expressions are handled explicitly in BindXmlElement, etc.)
                    Return BindXmlEmbeddedExpression(DirectCast(node, XmlEmbeddedExpressionSyntax), diagnostics:=diagnostics)

                Case SyntaxKind.XmlCDataSection
                    Return BindXmlCData(DirectCast(node, XmlCDataSectionSyntax), rootInfoOpt:=Nothing, diagnostics:=diagnostics)

                Case SyntaxKind.XmlElementAccessExpression
                    Return BindXmlElementAccess(DirectCast(node, XmlMemberAccessExpressionSyntax), diagnostics)

                Case SyntaxKind.XmlAttributeAccessExpression
                    Return BindXmlAttributeAccess(DirectCast(node, XmlMemberAccessExpressionSyntax), diagnostics)

                Case SyntaxKind.XmlDescendantAccessExpression
                    Return BindXmlDescendantAccess(DirectCast(node, XmlMemberAccessExpressionSyntax), diagnostics)

                Case SyntaxKind.AwaitExpression
                    Return BindAwait(DirectCast(node, AwaitExpressionSyntax), diagnostics)

                Case SyntaxKind.ConditionalAccessExpression
                    Return BindConditionalAccessExpression(DirectCast(node, ConditionalAccessExpressionSyntax), diagnostics)

                Case SyntaxKind.InterpolatedStringExpression
                    Return BindInterpolatedStringExpression(DirectCast(node, InterpolatedStringExpressionSyntax), diagnostics)

                Case SyntaxKind.TupleExpression
                    Return BindTupleExpression(DirectCast(node, TupleExpressionSyntax), diagnostics)

                Case Else
                    ' e.g. SyntaxKind.MidExpression is handled elsewhere
                    ' NOTE: There were too many "else" cases to justify listing them explicitly and throwing on
                    ' anything unexpected.
                    Debug.Assert(IsSemanticModelBinder OrElse node.ContainsDiagnostics, String.Format("Unexpected {0} syntax does not have diagnostics", node.Kind))
                    Return BadExpression(node, ImmutableArray(Of BoundExpression).Empty, ErrorTypeSymbol.UnknownResultType)

            End Select
        End Function

        ''' <summary>
        ''' Create a BoundBadExpression node for the given syntax node. No symbols or bound nodes are associated with it.
        ''' </summary>
        Protected Shared Function BadExpression(node As SyntaxNode, resultType As TypeSymbol) As BoundBadExpression
            Return New BoundBadExpression(node, LookupResultKind.Empty, ImmutableArray(Of Symbol).Empty, ImmutableArray(Of BoundExpression).Empty, resultType, hasErrors:=True)
        End Function

        ''' <summary>
        ''' Create a BoundBadExpression node for the given child-expression, which is preserved as a sub-expression. 
        ''' No ResultKind is associated
        ''' </summary>
        Private Shared Function BadExpression(node As SyntaxNode, expr As BoundExpression, resultType As TypeSymbol) As BoundBadExpression
            Return New BoundBadExpression(node, LookupResultKind.Empty, ImmutableArray(Of Symbol).Empty, ImmutableArray.Create(expr), resultType, hasErrors:=True)
        End Function

        ''' <summary>
        ''' Create a BoundBadExpression node for the given child-expression, which is preserved as a sub-expression. 
        ''' A ResultKind explains why the node is bad.
        ''' </summary>
        Private Shared Function BadExpression(node As SyntaxNode, expr As BoundExpression, resultKind As LookupResultKind, resultType As TypeSymbol) As BoundBadExpression
            Return New BoundBadExpression(node, resultKind, ImmutableArray(Of Symbol).Empty, ImmutableArray.Create(expr), resultType, hasErrors:=True)
        End Function

        ''' <summary>
        ''' Create a BoundBadExpression node for the given child expression, which is preserved as a sub-expression. Symbols
        ''' associated with the child node are not given a result kind.
        ''' </summary>
        Private Shared Function BadExpression(node As SyntaxNode, exprs As ImmutableArray(Of BoundExpression), resultType As TypeSymbol) As BoundBadExpression
            Return New BoundBadExpression(node, LookupResultKind.Empty, ImmutableArray(Of Symbol).Empty, exprs, resultType, hasErrors:=True)
        End Function

        Private Shared Function BadExpression(expr As BoundExpression) As BoundBadExpression
            Return BadExpression(LookupResultKind.Empty, expr)
        End Function

        ' Use this for a bad expression with no known type, and a single child whose type and syntax is preserved as the type of the bad expression.
        Private Shared Function BadExpression(resultKind As LookupResultKind, wrappedExpression As BoundExpression) As BoundBadExpression
            Dim wrappedBadExpression As BoundBadExpression = TryCast(wrappedExpression, BoundBadExpression)
            If wrappedBadExpression IsNot Nothing Then
                Return New BoundBadExpression(wrappedBadExpression.Syntax, resultKind, wrappedBadExpression.Symbols, wrappedBadExpression.ChildBoundNodes, wrappedBadExpression.Type, hasErrors:=True)
            Else
                Return New BoundBadExpression(wrappedExpression.Syntax, resultKind, ImmutableArray(Of Symbol).Empty, ImmutableArray.Create(wrappedExpression), wrappedExpression.Type, hasErrors:=True)
            End If
        End Function

        Private Function BindTupleExpression(node As TupleExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            Dim arguments As SeparatedSyntaxList(Of SimpleArgumentSyntax) = node.Arguments
            Dim numElements As Integer = arguments.Count

            If numElements < 2 Then
                ' this should be a parse error already.
                Dim args = If(numElements = 1,
                    ImmutableArray.Create(BindRValue(arguments(0).Expression, diagnostics)),
                    ImmutableArray(Of BoundExpression).Empty)

                Return BadExpression(node, args, ErrorTypeSymbol.UnknownResultType)
            End If

            Dim hasNaturalType = True
            Dim hasInferredType = True

            Dim boundArguments = ArrayBuilder(Of BoundExpression).GetInstance(arguments.Count)
            Dim elementTypes = ArrayBuilder(Of TypeSymbol).GetInstance(arguments.Count)
            Dim elementLocations = ArrayBuilder(Of Location).GetInstance(arguments.Count)

            ' prepare names
            Dim names = ExtractTupleElementNames(arguments, diagnostics)
            Dim elementNames = names.elementNames
            Dim inferredPositions = names.inferredPositions
            Dim hasErrors = names.hasErrors

            ' prepare types and locations
            For i As Integer = 0 To numElements - 1
                Dim argumentSyntax As SimpleArgumentSyntax = arguments(i)
                Dim nameSyntax As IdentifierNameSyntax = argumentSyntax.NameColonEquals?.Name

                If nameSyntax IsNot Nothing Then
                    elementLocations.Add(nameSyntax.GetLocation)

                    '  check type character
                    Dim typeChar As TypeCharacter = nameSyntax.Identifier.GetTypeCharacter()
                    If typeChar <> TypeCharacter.None Then
                        ReportDiagnostic(diagnostics, nameSyntax, ERRID.ERR_TupleLiteralDisallowsTypeChar)
                    End If
                Else
                    elementLocations.Add(argumentSyntax.GetLocation)
                End If

                Dim boundArgument As BoundExpression = BindValue(argumentSyntax.Expression, diagnostics)
                Dim elementType = GetTupleFieldType(boundArgument, argumentSyntax, diagnostics, hasNaturalType)

                If elementType Is Nothing Then
                    hasInferredType = False
                End If

                If boundArgument.Type IsNot Nothing Then
                    boundArgument = MakeRValue(boundArgument, diagnostics)
                End If

                boundArguments.Add(boundArgument)
                elementTypes.Add(elementType)
            Next

            Dim elements = elementTypes.ToImmutableAndFree()
            Dim locations = elementLocations.ToImmutableAndFree()

            Dim inferredType As TupleTypeSymbol = Nothing
            If hasInferredType Then
                Dim disallowInferredNames = Me.Compilation.LanguageVersion.DisallowInferredTupleElementNames()

                inferredType = TupleTypeSymbol.Create(node.GetLocation, elements, locations, elementNames, Me.Compilation,
                                                      shouldCheckConstraints:=True,
                                                      errorPositions:=If(disallowInferredNames, inferredPositions, Nothing),
                                                      syntax:=node, diagnostics:=diagnostics)
            End If

            Dim tupleTypeOpt As NamedTypeSymbol = If(hasNaturalType, inferredType, Nothing)

            '' Always track the inferred positions in the bound node, so that conversions don't produce a warning
            '' for "dropped names" when the name was inferred.
            Return New BoundTupleLiteral(node, inferredType, elementNames, inferredPositions, boundArguments.ToImmutableAndFree(), tupleTypeOpt, hasErrors)
        End Function

        Private Shared Function ExtractTupleElementNames(arguments As SeparatedSyntaxList(Of SimpleArgumentSyntax), diagnostics As BindingDiagnosticBag) _
            As (elementNames As ImmutableArray(Of String), inferredPositions As ImmutableArray(Of Boolean), hasErrors As Boolean)

            Dim hasErrors = False

            ' set of names already used
            Dim uniqueFieldNames = New HashSet(Of String)(IdentifierComparison.Comparer)

            Dim elementNames As ArrayBuilder(Of String) = Nothing
            Dim inferredElementNames As ArrayBuilder(Of String) = Nothing

            ' prepare and check element names and types
            Dim numElements As Integer = arguments.Count
            For i As Integer = 0 To numElements - 1
                Dim argumentSyntax As SimpleArgumentSyntax = arguments(i)
                Dim name As String = Nothing
                Dim inferredName As String = Nothing

                Dim nameSyntax As IdentifierNameSyntax = argumentSyntax.NameColonEquals?.Name

                If nameSyntax IsNot Nothing Then
                    name = nameSyntax.Identifier.ValueText

                    If Not CheckTupleMemberName(name, i, argumentSyntax.NameColonEquals.Name, diagnostics, uniqueFieldNames) Then
                        hasErrors = True
                    End If
                Else
                    inferredName = InferTupleElementName(argumentSyntax.Expression)
                End If

                CollectTupleFieldMemberName(name, i, numElements, elementNames)
                CollectTupleFieldMemberName(inferredName, i, numElements, inferredElementNames)
            Next

            RemoveDuplicateInferredTupleNamesAndFreeIfEmptied(inferredElementNames, uniqueFieldNames)

            Dim result = MergeTupleElementNames(elementNames, inferredElementNames)
            elementNames?.Free()
            inferredElementNames?.Free()
            Return (result.names, result.inferred, hasErrors)
        End Function

        Private Shared Function MergeTupleElementNames(elementNames As ArrayBuilder(Of String),
                                                       inferredElementNames As ArrayBuilder(Of String)) As (names As ImmutableArray(Of String),
                                                       inferred As ImmutableArray(Of Boolean))
            If elementNames Is Nothing Then
                If inferredElementNames Is Nothing Then
                    Return (Nothing, Nothing)
                Else
                    Dim finalNames = inferredElementNames.ToImmutable()
                    Return (finalNames, finalNames.SelectAsArray(Function(n) n IsNot Nothing))
                End If
            End If

            If inferredElementNames Is Nothing Then
                Return (elementNames.ToImmutable(), Nothing)
            End If

            Debug.Assert(elementNames.Count = inferredElementNames.Count)
            Dim builder = ArrayBuilder(Of Boolean).GetInstance(elementNames.Count)
            For i = 0 To elementNames.Count - 1

                Dim inferredName As String = inferredElementNames(i)
                If elementNames(i) Is Nothing AndAlso inferredName IsNot Nothing Then
                    elementNames(i) = inferredName
                    builder.Add(True)
                Else
                    builder.Add(False)
                End If
            Next
            Return (elementNames.ToImmutable(), builder.ToImmutableAndFree())
        End Function

        ''' <summary>
        ''' Removes duplicate entries in <paramref name="inferredElementNames"/> and frees it if only nulls remain.
        ''' </summary>
        Private Shared Sub RemoveDuplicateInferredTupleNamesAndFreeIfEmptied(ByRef inferredElementNames As ArrayBuilder(Of String), uniqueFieldNames As HashSet(Of String))
            If inferredElementNames Is Nothing Then
                Return
            End If

            ' Inferred names that duplicate an explicit name or a previous inferred name are tagged for removal
            Dim toRemove = New HashSet(Of String)(IdentifierComparison.Comparer)
            For Each name In inferredElementNames
                If name IsNot Nothing AndAlso Not uniqueFieldNames.Add(name) Then
                    toRemove.Add(name)
                End If
            Next

            For index = 0 To inferredElementNames.Count - 1
                Dim inferredName As String = inferredElementNames(index)
                If inferredName IsNot Nothing AndAlso toRemove.Contains(inferredName) Then
                    inferredElementNames(index) = Nothing
                End If
            Next

            If inferredElementNames.All(Function(n) n Is Nothing) Then
                inferredElementNames.Free()
                inferredElementNames = Nothing
            End If
        End Sub

        Private Shared Function InferTupleElementName(element As ExpressionSyntax) As String
            Dim ignore As XmlNameSyntax = Nothing
            Dim nameToken As SyntaxToken = element.ExtractAnonymousTypeMemberName(ignore)
            If nameToken.Kind() = SyntaxKind.IdentifierToken Then
                Dim name As String = nameToken.ValueText
                ' Reserved names are never candidates to be inferred names, at any position
                If TupleTypeSymbol.IsElementNameReserved(name) = -1 Then
                    Return name
                End If
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Returns the type to be used as a field type.
        ''' </summary>
        Private Function GetTupleFieldType(expression As BoundExpression,
                                                  errorSyntax As VisualBasicSyntaxNode,
                                                  diagnostics As BindingDiagnosticBag,
                                                  ByRef hasNaturalType As Boolean) As TypeSymbol
            Dim expressionType As TypeSymbol = expression.Type

            If expressionType Is Nothing Then
                hasNaturalType = False

                ' Dig through parenthesized.
                If Not expression.IsNothingLiteral Then
                    expression = expression.GetMostEnclosedParenthesizedExpression()
                End If

                Select Case expression.Kind
                    Case BoundKind.UnboundLambda
                        expressionType = DirectCast(expression, UnboundLambda).InferredAnonymousDelegate.Key

                    Case BoundKind.TupleLiteral
                        expressionType = DirectCast(expression, BoundTupleLiteral).InferredType

                    Case BoundKind.ArrayLiteral
                        expressionType = DirectCast(expression, BoundArrayLiteral).InferredType

                    Case Else
                        If expression.IsNothingLiteral Then
                            expressionType = GetSpecialType(SpecialType.System_Object, expression.Syntax, diagnostics)
                        End If
                End Select

            End If

            Return expressionType
        End Function

        Private Shared Sub CollectTupleFieldMemberName(name As String, elementIndex As Integer, tupleSize As Integer, ByRef elementNames As ArrayBuilder(Of String))
            ' add the name to the list
            ' names would typically all be there or none at all
            ' but in case we need to handle this in error cases
            If elementNames IsNot Nothing Then
                elementNames.Add(name)
            Else
                If name IsNot Nothing Then
                    elementNames = ArrayBuilder(Of String).GetInstance(tupleSize)
                    For j As Integer = 1 To elementIndex
                        elementNames.Add(Nothing)
                    Next
                    elementNames.Add(name)
                End If
            End If
        End Sub

        Private Shared Function CheckTupleMemberName(name As String, index As Integer, syntax As SyntaxNodeOrToken, diagnostics As BindingDiagnosticBag, uniqueFieldNames As HashSet(Of String)) As Boolean
            Dim reserved As Integer = TupleTypeSymbol.IsElementNameReserved(name)
            If reserved = 0 Then
                Binder.ReportDiagnostic(diagnostics, syntax, ERRID.ERR_TupleReservedElementNameAnyPosition, name)
                Return False

            ElseIf reserved > 0 AndAlso reserved <> index + 1 Then
                Binder.ReportDiagnostic(diagnostics, syntax, ERRID.ERR_TupleReservedElementName, name, reserved)
                Return False

            ElseIf (Not uniqueFieldNames.Add(name)) Then
                Binder.ReportDiagnostic(diagnostics, syntax, ERRID.ERR_TupleDuplicateElementName)
                Return False

            End If

            Return True
        End Function

        Public Function BindNamespaceOrTypeExpression(node As TypeSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            Dim symbol = Me.BindNamespaceOrTypeOrAliasSyntax(node, diagnostics)

            Dim [alias] = TryCast(symbol, AliasSymbol)
            If [alias] IsNot Nothing Then
                symbol = [alias].Target

                '  check for use site errors
                ReportUseSite(diagnostics, node, symbol)
            End If

            Dim [type] = TryCast(symbol, TypeSymbol)
            If [type] IsNot Nothing Then
                Return New BoundTypeExpression(node, Nothing, [alias], [type])
            End If
            Dim ns = TryCast(symbol, NamespaceSymbol)
            If ns IsNot Nothing Then
                Return New BoundNamespaceExpression(node, Nothing, [alias], ns)
            End If

            ' BindNamespaceOrTypeSyntax should always return a type or a namespace (might be an error type).
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Function BindNamespaceOrTypeOrExpressionSyntaxForSemanticModel(node As ExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            If (node.Kind = SyntaxKind.PredefinedType) OrElse
               (((TypeOf node Is NameSyntax) OrElse node.Kind = SyntaxKind.ArrayType OrElse node.Kind = SyntaxKind.TupleType) AndAlso SyntaxFacts.IsInNamespaceOrTypeContext(node)) Then
                Dim result As BoundExpression = Me.BindNamespaceOrTypeExpression(DirectCast(node, TypeSyntax), diagnostics)

                ' Deal with the case of a namespace group. We may need to bind more in order to see if the ambiguity can be resolved.
                If node.Parent IsNot Nothing AndAlso
                   node.Parent.Kind = SyntaxKind.QualifiedName AndAlso
                   DirectCast(node.Parent, QualifiedNameSyntax).Left Is node AndAlso
                   result.Kind = BoundKind.NamespaceExpression Then
                    Dim namespaceExpr = DirectCast(result, BoundNamespaceExpression)

                    If namespaceExpr.NamespaceSymbol.NamespaceKind = NamespaceKindNamespaceGroup Then
                        Dim boundParent As BoundExpression = BindNamespaceOrTypeOrExpressionSyntaxForSemanticModel(DirectCast(node.Parent, QualifiedNameSyntax), BindingDiagnosticBag.Discarded)

                        Dim symbols = ArrayBuilder(Of Symbol).GetInstance()

                        BindNamespaceOrTypeSyntaxForSemanticModelGetExpressionSymbols(boundParent, symbols)

                        If symbols.Count = 0 Then
                            ' If we didn't get anything, let's bind normally and see if any symbol comes out.
                            boundParent = BindExpression(DirectCast(node.Parent, QualifiedNameSyntax), BindingDiagnosticBag.Discarded)
                            BindNamespaceOrTypeSyntaxForSemanticModelGetExpressionSymbols(boundParent, symbols)
                        End If

                        result = AdjustReceiverNamespace(namespaceExpr, symbols)
                        symbols.Free()
                    End If
                End If

                Return result
            Else
                Return Me.BindExpression(node, isInvocationOrAddressOf:=SyntaxFacts.IsInvocationOrAddressOfOperand(node), diagnostics:=diagnostics, isOperandOfConditionalBranch:=False, eventContext:=False)
            End If
        End Function

        Private Shared Sub BindNamespaceOrTypeSyntaxForSemanticModelGetExpressionSymbols(expression As BoundExpression, symbols As ArrayBuilder(Of Symbol))
            expression.GetExpressionSymbols(symbols)

            If symbols.Count = 1 AndAlso symbols(0).Kind = SymbolKind.ErrorType Then
                Dim errorType = DirectCast(symbols(0), ErrorTypeSymbol)
                symbols.Clear()
                Dim diagnosticInfo = TryCast(errorType.ErrorInfo, IDiagnosticInfoWithSymbols)

                If diagnosticInfo IsNot Nothing Then
                    diagnosticInfo.GetAssociatedSymbols(symbols)
                End If
            End If
        End Sub

        ''' <summary>
        ''' This function is only needed for SemanticModel to perform binding for erroneous cases.
        ''' </summary>
        Private Function BindQualifiedName(name As QualifiedNameSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            Return Me.BindMemberAccess(name, BindExpression(name.Left, diagnostics), name.Right, eventContext:=False, diagnostics:=diagnostics)
        End Function

        Private Function BindGetTypeExpression(node As GetTypeExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            ' Create a special binder that allows unbound types
            Dim getTypeBinder = New GetTypeBinder(node.Type, Me)

            ' GetType is more permissive on what is considered a valid type.
            ' for example it allows modules, System.Void or open generic types.

            'returns either a type, an alias that refers to a type, or an error type
            Dim typeOrAlias As Symbol = TypeBinder.BindTypeOrAliasSyntax(node.Type, getTypeBinder, diagnostics,
                                                                         suppressUseSiteError:=False, inGetTypeContext:=True, resolvingBaseType:=False)
            Dim aliasSym As AliasSymbol = TryCast(typeOrAlias, AliasSymbol)
            Dim typeSym As TypeSymbol = DirectCast(If(aliasSym IsNot Nothing, aliasSym.Target, typeOrAlias), TypeSymbol)
            Dim typeExpression = New BoundTypeExpression(node.Type, Nothing, aliasSym, typeSym, typeSym.IsErrorType())

            ' System.Void() is not allowed for VB.
            If typeSym.IsArrayType AndAlso DirectCast(typeSym, ArrayTypeSymbol).ElementType.SpecialType = SpecialType.System_Void Then
                ReportDiagnostic(diagnostics, node.Type, ErrorFactory.ErrorInfo(ERRID.ERR_VoidArrayDisallowed))
            End If

            Return New BoundGetType(node, typeExpression, GetWellKnownType(WellKnownType.System_Type, node, diagnostics))
        End Function

        Private Function BindNameOfExpression(node As NameOfExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression

            ' Suppress diagnostics if argument has syntax errors
            If node.Argument.HasErrors Then
                diagnostics = BindingDiagnosticBag.Discarded
            End If

            Dim value As String = Nothing

            Select Case node.Argument.Kind
                Case SyntaxKind.SimpleMemberAccessExpression
                    value = DirectCast(node.Argument, MemberAccessExpressionSyntax).Name.Identifier.ValueText

                Case SyntaxKind.IdentifierName,
                     SyntaxKind.GenericName
                    value = DirectCast(node.Argument, SimpleNameSyntax).Identifier.ValueText

                Case Else
                    ' Must be a syntax error
                    Debug.Assert(node.Argument.HasErrors)
            End Select

            ' Bind the argument
            Dim argument As BoundExpression = BindExpression(node.Argument, diagnostics)

            Select Case argument.Kind
                Case BoundKind.MethodGroup

                    Dim group = DirectCast(argument, BoundMethodGroup)

                    If group.ResultKind = LookupResultKind.Inaccessible Then
                        ReportDiagnostic(diagnostics,
                                         If(node.Argument.Kind = SyntaxKind.SimpleMemberAccessExpression,
                                            DirectCast(node.Argument, MemberAccessExpressionSyntax).Name,
                                            node.Argument),
                                         GetInaccessibleErrorInfo(group.Methods.First))

                    ElseIf group.ResultKind = LookupResultKind.Good AndAlso group.TypeArgumentsOpt IsNot Nothing Then
                        ReportDiagnostic(diagnostics, group.TypeArgumentsOpt.Syntax, ERRID.ERR_MethodTypeArgsUnexpected)
                    Else
                        For Each method In group.Methods
                            diagnostics.AddDependency(method.ContainingAssembly)
                        Next
                    End If

                Case BoundKind.PropertyGroup

                    Dim group = DirectCast(argument, BoundPropertyGroup)

                    If group.ResultKind = LookupResultKind.Inaccessible Then
                        ReportDiagnostic(diagnostics,
                                         If(node.Argument.Kind = SyntaxKind.SimpleMemberAccessExpression,
                                            DirectCast(node.Argument, MemberAccessExpressionSyntax).Name,
                                            node.Argument),
                                         GetInaccessibleErrorInfo(group.Properties.First))
                    Else
                        For Each prop In group.Properties
                            diagnostics.AddDependency(prop.ContainingAssembly)
                        Next
                    End If

                Case BoundKind.NamespaceExpression
                    diagnostics.AddAssembliesUsedByNamespaceReference(DirectCast(argument, BoundNamespaceExpression).NamespaceSymbol)
            End Select

            Return New BoundNameOfOperator(node, argument, ConstantValue.Create(value), GetSpecialType(SpecialType.System_String, node, diagnostics))
        End Function

        Private Function BindTypeOfExpression(node As TypeOfExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression

            Dim operand = BindRValue(node.Expression, diagnostics, isOperandOfConditionalBranch:=False)
            Dim operandType = operand.Type

            Dim operatorIsIsNot = (node.Kind = SyntaxKind.TypeOfIsNotExpression)

            Dim targetSymbol As Symbol = BindTypeOrAliasSyntax(node.Type, diagnostics)
            Dim targetType = DirectCast(If(TryCast(targetSymbol, TypeSymbol), DirectCast(targetSymbol, AliasSymbol).Target), TypeSymbol)

            Dim resultType As TypeSymbol = GetSpecialType(SpecialType.System_Boolean, node, diagnostics)

            If operand.HasErrors OrElse operandType.IsErrorType() OrElse targetType.IsErrorType() Then
                ' If operand is bad or either the source or target types have errors, bail out preventing more cascading errors.
                Return New BoundTypeOf(node, operand, operatorIsIsNot, targetType, resultType)
            End If

            If Not operandType.IsReferenceType AndAlso
               Not operandType.IsTypeParameter() Then

                ReportDiagnostic(diagnostics, node.Expression, ERRID.ERR_TypeOfRequiresReferenceType1, operandType)

            Else
                Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)
                Dim convKind As ConversionKind = Conversions.ClassifyTryCastConversion(operandType, targetType, useSiteInfo)

                If diagnostics.Add(node, useSiteInfo) Then
                    ' Suppress any additional diagnostics
                    diagnostics = BindingDiagnosticBag.Discarded
                ElseIf Not Conversions.ConversionExists(convKind) Then
                    ReportDiagnostic(diagnostics, node, ERRID.ERR_TypeOfExprAlwaysFalse2, operandType, targetType)
                End If
            End If

            If operandType.IsTypeParameter() Then
                operand = ApplyImplicitConversion(node, GetSpecialType(SpecialType.System_Object, node.Expression, diagnostics), operand, diagnostics)
            End If

            Return New BoundTypeOf(node, operand, operatorIsIsNot, targetType, resultType)

        End Function

        ''' <summary>
        ''' BindValue evaluates the node and returns a BoundExpression.  BindValue snaps expressions to values.  For now that means that method groups
        ''' become invocations.
        ''' </summary>
        Friend Function BindValue(
             node As ExpressionSyntax,
             diagnostics As BindingDiagnosticBag,
             Optional isOperandOfConditionalBranch As Boolean = False
         ) As BoundExpression
            Dim expr = BindExpression(node, diagnostics:=diagnostics, isOperandOfConditionalBranch:=isOperandOfConditionalBranch, isInvocationOrAddressOf:=False, eventContext:=False)

            Return MakeValue(expr, diagnostics)
        End Function

        Private Function AdjustReceiverTypeOrValue(receiver As BoundExpression,
                              node As SyntaxNode,
                              isShared As Boolean,
                              diagnostics As BindingDiagnosticBag,
                              ByRef resolvedTypeOrValueExpression As BoundExpression) As BoundExpression
            Dim unused As QualificationKind
            Return AdjustReceiverTypeOrValue(receiver, node, isShared, True, diagnostics, unused, resolvedTypeOrValueExpression)
        End Function

        Private Function AdjustReceiverTypeOrValue(receiver As BoundExpression,
                              node As SyntaxNode,
                              isShared As Boolean,
                              diagnostics As BindingDiagnosticBag,
                              ByRef qualKind As QualificationKind) As BoundExpression
            Dim unused As BoundExpression = Nothing
            Return AdjustReceiverTypeOrValue(receiver, node, isShared, False, diagnostics, qualKind, unused)
        End Function

        ''' <summary>
        ''' Adjusts receiver of a call or a member access.
        '''  * will turn Unknown property access into Get property access
        '''  * will turn TypeOrValueExpression into a value expression
        ''' </summary>
        Private Function AdjustReceiverTypeOrValue(receiver As BoundExpression,
                              node As SyntaxNode,
                              isShared As Boolean,
                              clearIfShared As Boolean,
                              diagnostics As BindingDiagnosticBag,
                              ByRef qualKind As QualificationKind,
                              ByRef resolvedTypeOrValueExpression As BoundExpression) As BoundExpression
            If receiver Is Nothing Then
                Return receiver
            End If

            If isShared Then
                If receiver.Kind = BoundKind.TypeOrValueExpression Then
                    Dim typeOrValue = DirectCast(receiver, BoundTypeOrValueExpression)
                    diagnostics.AddRange(typeOrValue.Data.TypeDiagnostics)
                    receiver = typeOrValue.Data.TypeExpression
                    qualKind = QualificationKind.QualifiedViaTypeName
                    resolvedTypeOrValueExpression = receiver
                End If

                If clearIfShared Then
                    receiver = Nothing
                End If
            Else
                If receiver.Kind = BoundKind.TypeOrValueExpression Then
                    Dim typeOrValue = DirectCast(receiver, BoundTypeOrValueExpression)
                    diagnostics.AddRange(typeOrValue.Data.ValueDiagnostics)
                    receiver = MakeValue(typeOrValue.Data.ValueExpression, diagnostics)
                    qualKind = QualificationKind.QualifiedViaValue
                    resolvedTypeOrValueExpression = receiver
                End If

                receiver = AdjustReceiverValue(receiver, node, diagnostics)
            End If

            Return receiver
        End Function

        ''' <summary>
        ''' Adjusts receiver of a call or a member access if the receiver is an
        ''' ambiguous BoundTypeOrValueExpression. This can only happen if the
        ''' receiver is the LHS of a member access expression in which the
        ''' RHS cannot be resolved (i.e. the RHS is an error or a late-bound
        ''' invocation/access).
        ''' </summary>
        Private Shared Function AdjustReceiverAmbiguousTypeOrValue(receiver As BoundExpression, diagnostics As BindingDiagnosticBag) As BoundExpression
            If receiver IsNot Nothing AndAlso receiver.Kind = BoundKind.TypeOrValueExpression Then
                Dim typeOrValue = DirectCast(receiver, BoundTypeOrValueExpression)
                diagnostics.AddRange(typeOrValue.Data.ValueDiagnostics)
                receiver = typeOrValue.Data.ValueExpression
            End If

            Return receiver
        End Function

        Private Shared Function AdjustReceiverAmbiguousTypeOrValue(ByRef group As BoundMethodOrPropertyGroup, diagnostics As BindingDiagnosticBag) As BoundExpression
            Debug.Assert(group IsNot Nothing)

            Dim receiver = group.ReceiverOpt
            If receiver IsNot Nothing AndAlso receiver.Kind = BoundKind.TypeOrValueExpression Then
                receiver = AdjustReceiverAmbiguousTypeOrValue(receiver, diagnostics)

                Select Case group.Kind
                    Case BoundKind.MethodGroup
                        Dim methodGroup = DirectCast(group, BoundMethodGroup)
                        group = methodGroup.Update(methodGroup.TypeArgumentsOpt,
                                                   methodGroup.Methods,
                                                   methodGroup.PendingExtensionMethodsOpt,
                                                   methodGroup.ResultKind,
                                                   receiver,
                                                   methodGroup.QualificationKind)

                    Case BoundKind.PropertyGroup
                        Dim propertyGroup = DirectCast(group, BoundPropertyGroup)
                        group = propertyGroup.Update(propertyGroup.Properties,
                                                     propertyGroup.ResultKind,
                                                     receiver,
                                                     propertyGroup.QualificationKind)

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(group.Kind)
                End Select
            End If

            Return receiver
        End Function

        ''' <summary>
        ''' Adjusts receiver of a call or a member access if it is a value
        '''  * will turn Unknown property access into Get property access
        ''' </summary>
        Private Function AdjustReceiverValue(receiver As BoundExpression,
                      node As SyntaxNode,
                      diagnostics As BindingDiagnosticBag) As BoundExpression

            If Not receiver.IsValue() Then
                receiver = MakeValue(receiver, diagnostics)
            End If

            If Not receiver.IsLValue AndAlso Not receiver.IsPropertyOrXmlPropertyAccess() Then
                receiver = MakeRValue(receiver, diagnostics)
            End If

            Dim type = receiver.Type

            If type Is Nothing OrElse type.IsErrorType() Then
                Return BadExpression(node, receiver, LookupResultKind.NotAValue, ErrorTypeSymbol.UnknownResultType)
            End If

            Return receiver
        End Function

        Friend Function ReclassifyAsValue(
           expr As BoundExpression,
           diagnostics As BindingDiagnosticBag
        ) As BoundExpression
            If expr.Kind = BoundKind.ConditionalAccess AndAlso expr.Type Is Nothing Then
                Dim conditionalAccess = DirectCast(expr, BoundConditionalAccess)
                Dim access As BoundExpression = Me.MakeRValue(conditionalAccess.AccessExpression, diagnostics)

                Dim resultType As TypeSymbol = access.Type

                If Not resultType.IsErrorType() Then
                    If resultType.IsValueType AndAlso Not resultType.IsRestrictedType Then
                        If Not resultType.IsNullableType() Then
                            resultType = GetSpecialType(SpecialType.System_Nullable_T, expr.Syntax, diagnostics).Construct(resultType)
                        End If
                    ElseIf Not resultType.IsReferenceType Then
                        ' Access cannot have unconstrained generic type or a restricted type
                        ReportDiagnostic(diagnostics, access.Syntax, ERRID.ERR_CannotBeMadeNullable1, resultType)
                        resultType = ErrorTypeSymbol.UnknownResultType
                    End If
                End If

                Return conditionalAccess.Update(conditionalAccess.Receiver, conditionalAccess.Placeholder, access, resultType)
            End If

            If expr.HasErrors Then
                Return expr
            End If

            Select Case expr.Kind
                Case BoundKind.Parenthesized
                    If Not expr.IsNothingLiteral() Then
                        Return MakeRValue(expr, diagnostics)
                    End If

                Case BoundKind.MethodGroup,
                     BoundKind.PropertyGroup

                    Dim group = DirectCast(expr, BoundMethodOrPropertyGroup)

                    If IsGroupOfConstructors(group) Then

                        '  cannot reclassify a constructor call
                        ReportDiagnostic(diagnostics, group.Syntax, ERRID.ERR_InvalidConstructorCall)
                        Return New BoundBadVariable(expr.Syntax, expr, ErrorTypeSymbol.UnknownResultType, hasErrors:=True)
                    Else

                        expr = BindInvocationExpression(expr.Syntax,
                                                        expr.Syntax,
                                                        ExtractTypeCharacter(expr.Syntax),
                                                        group,
                                                        s_noArguments,
                                                        Nothing,
                                                        diagnostics,
                                                        callerInfoOpt:=expr.Syntax)
                    End If

                Case BoundKind.TypeExpression

                    ' Try default instance property through DefaultInstanceAlias
                    Dim instance As BoundExpression = TryDefaultInstanceProperty(DirectCast(expr, BoundTypeExpression), diagnostics)

                    If instance Is Nothing Then
                        Dim type = expr.Type
                        ReportDiagnostic(diagnostics, expr.Syntax, GetTypeNotExpressionErrorId(type), type)
                        Return New BoundBadVariable(expr.Syntax, expr, ErrorTypeSymbol.UnknownResultType, hasErrors:=True)
                    End If

                    expr = instance

                Case BoundKind.EventAccess
                    ReportDiagnostic(diagnostics, expr.Syntax, ERRID.ERR_CannotCallEvent1, DirectCast(expr, BoundEventAccess).EventSymbol)
                    Return New BoundBadVariable(expr.Syntax, expr, ErrorTypeSymbol.UnknownResultType, hasErrors:=True)

                Case BoundKind.NamespaceExpression
                    ReportDiagnostic(diagnostics, expr.Syntax, ERRID.ERR_NamespaceNotExpression1, DirectCast(expr, BoundNamespaceExpression).NamespaceSymbol)
                    Return New BoundBadVariable(expr.Syntax, expr, ErrorTypeSymbol.UnknownResultType, hasErrors:=True)

                Case BoundKind.Label
                    ReportDiagnostic(diagnostics, expr.Syntax, ERRID.ERR_VoidValue, DirectCast(expr, BoundLabel).Label.Name)
                    Return New BoundBadVariable(expr.Syntax, expr, ErrorTypeSymbol.UnknownResultType, hasErrors:=True)
            End Select

            Debug.Assert(expr.IsValue)
            Return expr
        End Function

        Friend Overridable ReadOnly Property IsDefaultInstancePropertyAllowed As Boolean
            Get
                Return m_containingBinder.IsDefaultInstancePropertyAllowed
            End Get
        End Property

        Friend Overridable ReadOnly Property SuppressCallerInfo As Boolean
            Get
                Return m_containingBinder.SuppressCallerInfo
            End Get
        End Property

        Friend Function TryDefaultInstanceProperty(typeExpr As BoundTypeExpression, diagnostics As BindingDiagnosticBag) As BoundExpression

            If Not IsDefaultInstancePropertyAllowed Then
                Return Nothing
            End If

            ' See Semantics::CheckForDefaultInstanceProperty.

            Dim type As TypeSymbol = typeExpr.Type

            If type.IsErrorType() OrElse
               SourceModule IsNot type.ContainingModule OrElse
               type.TypeKind <> TYPEKIND.Class Then
                Return Nothing
            End If

            Dim classType = DirectCast(type, NamedTypeSymbol)

            If classType.IsGenericType Then
                Return Nothing
            End If

            Dim prop As SynthesizedMyGroupCollectionPropertySymbol = SourceModule.GetMyGroupCollectionPropertyWithDefaultInstanceAlias(classType)

            If prop Is Nothing Then
                Return Nothing
            End If

            Debug.Assert(prop.Type Is classType)

            ' Lets try to parse and bind an expression of the following form:
            '     <DefaultInstanceAlias>.<MyGroupCollectionProperty name>
            ' If any error happens, return Nothing without reporting any diagnostics.

            ' Note, native compiler doesn't escape DefaultInstanceAlias if it is a reserved keyword.

            Dim codeToParse As String =
                "Class DefaultInstanceAlias" & vbCrLf &
                    "Function DefaultInstanceAlias()" & vbCrLf &
                        "Return " & prop.DefaultInstanceAlias & "." & prop.Name & vbCrLf &
                    "End Function" & vbCrLf &
                "End Class" & vbCrLf

            ' It looks like Dev11 ignores project level conditional compilation here, which makes sense since expression cannot contain #If directives.
            Dim tree = VisualBasicSyntaxTree.ParseText(SourceText.From(codeToParse))
            Dim root As CompilationUnitSyntax = tree.GetCompilationUnitRoot()
            Dim hasErrors As Boolean = False

            For Each diag As Diagnostic In tree.GetDiagnostics(root)
                Dim cdiag = TryCast(diag, DiagnosticWithInfo)
                Debug.Assert(cdiag Is Nothing OrElse Not cdiag.HasLazyInfo,
                             "If we decide to allow lazy syntax diagnostics, we'll have to check all call sites of SyntaxTree.GetDiagnostics")
                If diag.Severity = DiagnosticSeverity.Error Then
                    Return Nothing
                End If
            Next

            Dim classBlock = DirectCast(root.Members(0), ClassBlockSyntax)
            Dim functionBlock = DirectCast(classBlock.Members(0), MethodBlockSyntax)

            ' We expect there to be only one statement, which is [Return] statement.
            If functionBlock.Statements.Count > 1 Then
                Return Nothing
            End If

            Dim ret = DirectCast(functionBlock.Statements(0), ReturnStatementSyntax)
            Dim exprDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics:=True, withDependencies:=diagnostics.AccumulatesDependencies)
            Dim result As BoundExpression = (New DefaultInstancePropertyBinder(Me)).BindValue(ret.Expression, exprDiagnostics)

            If result.HasErrors OrElse exprDiagnostics.HasAnyErrors() Then
                exprDiagnostics.Free()
                Return Nothing
            End If

            diagnostics.AddDependencies(exprDiagnostics)
            exprDiagnostics.Free()

            ' if the default inst expression cannot be correctly bound to an instance of the same type as the class
            ' ignore it.
            If result.Type IsNot classType Then
                Return Nothing
            End If

            If ContainingType Is classType AndAlso Not ContainingMember.IsShared Then
                ReportDiagnostic(diagnostics, typeExpr.Syntax, ERRID.ERR_CantReferToMyGroupInsideGroupType1, classType)
            End If

            ' We need to change syntax node for the result to match typeExpr's syntax node.
            ' This will allow SemanticModel to report the node as a default instance access rather than 
            ' as a type reference.
            Select Case result.Kind
                Case BoundKind.PropertyAccess
                    Dim access = DirectCast(result, BoundPropertyAccess)
                    result = New BoundPropertyAccess(typeExpr.Syntax, access.PropertySymbol, access.PropertyGroupOpt, access.AccessKind,
                                                     isWriteable:=access.IsWriteable,
                                                     isLValue:=False,
                                                     receiverOpt:=access.ReceiverOpt,
                                                     arguments:=access.Arguments,
                                                     defaultArguments:=access.DefaultArguments,
                                                     type:=access.Type,
                                                     hasErrors:=access.HasErrors)

                Case BoundKind.FieldAccess
                    Dim access = DirectCast(result, BoundFieldAccess)
                    result = New BoundFieldAccess(typeExpr.Syntax, access.ReceiverOpt, access.FieldSymbol, access.IsLValue,
                                                  access.SuppressVirtualCalls, access.ConstantsInProgressOpt, access.Type, access.HasErrors)

                Case BoundKind.Call
                    Dim [call] = DirectCast(result, BoundCall)
                    result = New BoundCall(typeExpr.Syntax, [call].Method, [call].MethodGroupOpt, [call].ReceiverOpt, [call].Arguments,
                                           [call].DefaultArguments, [call].ConstantValueOpt,
                                           isLValue:=False,
                                           suppressObjectClone:=[call].SuppressObjectClone,
                                           type:=[call].Type,
                                           hasErrors:=[call].HasErrors)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(result.Kind)
            End Select

            Return result
        End Function

        Private Class DefaultInstancePropertyBinder
            Inherits Binder

            Public Sub New(containingBinder As Binder)
                MyBase.New(containingBinder)
            End Sub

            Public Overrides ReadOnly Property ImplicitVariableDeclarationAllowed As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property IsDefaultInstancePropertyAllowed As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides ReadOnly Property SuppressCallerInfo As Boolean
                Get
                    Return True
                End Get
            End Property
        End Class

        Private Shared Function GetTypeNotExpressionErrorId(type As TypeSymbol) As ERRID
            Select Case type.TypeKind

                Case TYPEKIND.Class
                    Return ERRID.ERR_ClassNotExpression1

                Case TYPEKIND.Interface
                    Return ERRID.ERR_InterfaceNotExpression1

                Case TYPEKIND.Enum
                    Return ERRID.ERR_EnumNotExpression1

                Case TYPEKIND.Structure
                    Return ERRID.ERR_StructureNotExpression1

                    ' TODO Modules??

                Case Else
                    Return ERRID.ERR_TypeNotExpression1

            End Select
        End Function

        Private Function MakeValue(
           expr As BoundExpression,
           diagnostics As BindingDiagnosticBag
        ) As BoundExpression

            If expr.Kind = BoundKind.Parenthesized Then
                If Not expr.IsNothingLiteral() Then
                    Dim parenthesized = DirectCast(expr, BoundParenthesized)
                    Dim enclosed As BoundExpression = MakeValue(parenthesized.Expression, diagnostics)
                    Return parenthesized.Update(enclosed, enclosed.Type)
                End If
            End If

            expr = ReclassifyAsValue(expr, diagnostics)

            If expr.HasErrors Then

                If Not expr.IsValue OrElse expr.Type Is Nothing OrElse expr.Type.IsVoidType Then
                    Return BadExpression(expr.Syntax, expr, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
                Else
                    Return expr
                End If
            End If

            Dim exprType = expr.Type
            Dim syntax = expr.Syntax

            If Not expr.IsValue() OrElse
               (exprType IsNot Nothing AndAlso exprType.SpecialType = SpecialType.System_Void) Then

                ReportDiagnostic(diagnostics, syntax, ERRID.ERR_VoidValue)
                Return BadExpression(syntax, expr, LookupResultKind.NotAValue, ErrorTypeSymbol.UnknownResultType)
            ElseIf expr.Kind = BoundKind.PropertyAccess Then

                Dim propertyAccess = DirectCast(expr, BoundPropertyAccess)

                Select Case propertyAccess.AccessKind
                    Case PropertyAccessKind.Set
                        ReportDiagnostic(diagnostics, syntax, ERRID.ERR_VoidValue)
                        Return BadExpression(syntax, expr, LookupResultKind.NotAValue, ErrorTypeSymbol.UnknownResultType)

                    Case PropertyAccessKind.Unknown

                        Dim hasError = True
                        Dim propertySymbol = propertyAccess.PropertySymbol
                        If Not propertySymbol.IsReadable Then
                            ReportDiagnostic(diagnostics, syntax, ERRID.ERR_NoGetProperty1, CustomSymbolDisplayFormatter.ShortErrorName(propertySymbol))
                        Else
                            Dim getMethod = propertySymbol.GetMostDerivedGetMethod()
                            Debug.Assert(getMethod IsNot Nothing)

                            If Not ReportUseSite(diagnostics, syntax, getMethod) Then
                                Dim accessThroughType = GetAccessThroughType(propertyAccess.ReceiverOpt)
                                Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)

                                If IsAccessible(getMethod, useSiteInfo, accessThroughType) OrElse
                                   Not IsAccessible(propertySymbol, useSiteInfo, accessThroughType) Then
                                    hasError = False
                                Else
                                    ReportDiagnostic(diagnostics, syntax, ERRID.ERR_NoAccessibleGet, CustomSymbolDisplayFormatter.ShortErrorName(propertySymbol))
                                End If

                                diagnostics.Add(syntax, useSiteInfo)
                            End If
                        End If

                        If hasError Then
                            Return BadExpression(syntax, expr, LookupResultKind.NotAValue, propertySymbol.Type)
                        End If
                End Select

            ElseIf expr.IsLateBound() Then
                If (expr.GetLateBoundAccessKind() And (LateBoundAccessKind.Set Or LateBoundAccessKind.Call)) <> 0 Then
                    ReportDiagnostic(diagnostics, syntax, ERRID.ERR_VoidValue)
                    Return BadExpression(syntax, expr, LookupResultKind.NotAValue, ErrorTypeSymbol.UnknownResultType)
                End If

            ElseIf expr.Kind = BoundKind.AddressOfOperator Then
                Return expr
            End If

            Return expr
        End Function

        Private Function GetAccessThroughType(receiverOpt As BoundExpression) As TypeSymbol
            If receiverOpt Is Nothing OrElse receiverOpt.Kind = BoundKind.MyBaseReference Then
                ' NOTE: If we are accessing the symbol via MyBase reference we may 
                '       assume the access is being performed via 'Me'
                Return ContainingType
            Else
                Debug.Assert(receiverOpt.Type IsNot Nothing)
                Return receiverOpt.Type
            End If
        End Function

        ''' <summary>
        ''' BindRValue evaluates the node and returns a BoundExpression.  
        ''' It ensures that the expression is a value that can be used on the right hand side of an assignment.  
        ''' If not, it reports an error.
        ''' 
        ''' Note that this function will reclassify all expressions to have their "default" type, i.e.
        ''' Anonymous Delegate for a lambda, default array type for an array literal, will report an error 
        ''' for an AddressOf, etc. So, if you are in a context where there is a known target type for the 
        ''' expression, do not use this function. Instead, use BindValue followed by 
        ''' ApplyImplicitConversion/ApplyConversion.  
        ''' </summary>
        Private Function BindRValue(
           node As ExpressionSyntax,
           diagnostics As BindingDiagnosticBag,
           Optional isOperandOfConditionalBranch As Boolean = False
       ) As BoundExpression
            Dim expr = BindExpression(node, diagnostics:=diagnostics, isOperandOfConditionalBranch:=isOperandOfConditionalBranch, isInvocationOrAddressOf:=False, eventContext:=False)

            Return MakeRValue(expr, diagnostics)
        End Function

        Friend Function MakeRValue(
           expr As BoundExpression,
           diagnostics As BindingDiagnosticBag
        ) As BoundExpression

            If expr.Kind = BoundKind.Parenthesized AndAlso Not expr.IsNothingLiteral() Then
                Dim parenthesized = DirectCast(expr, BoundParenthesized)
                Dim enclosed As BoundExpression = MakeRValue(parenthesized.Expression, diagnostics)
                Return parenthesized.Update(enclosed, enclosed.Type)

            ElseIf expr.Kind = BoundKind.XmlMemberAccess Then
                Dim memberAccess = DirectCast(expr, BoundXmlMemberAccess)
                Dim enclosed = MakeRValue(memberAccess.MemberAccess, diagnostics)
                Return memberAccess.Update(enclosed)

            End If

            expr = MakeValue(expr, diagnostics)

            If expr.HasErrors Then
                Return expr.MakeRValue()
            End If

            Debug.Assert(expr.IsValue())

            Dim exprType = expr.Type

            If exprType Is Nothing Then
                Return ReclassifyExpression(expr, diagnostics)
            End If

            ' Transform LValue to RValue.
            If expr.IsLValue Then
                expr = expr.MakeRValue()

            ElseIf expr.Kind = BoundKind.PropertyAccess Then

                Dim propertyAccess = DirectCast(expr, BoundPropertyAccess)
                Dim getMethod = propertyAccess.PropertySymbol.GetMostDerivedGetMethod()
                Debug.Assert(getMethod IsNot Nothing)

                ReportUseSite(diagnostics, expr.Syntax, getMethod)
                ReportDiagnosticsIfObsoleteOrNotSupported(diagnostics, getMethod, expr.Syntax)

                Select Case propertyAccess.AccessKind
                    Case PropertyAccessKind.Get
                        ' Nothing to do.

                    Case PropertyAccessKind.Unknown
                        Debug.Assert(propertyAccess.PropertySymbol.IsReadable)
                        WarnOnRecursiveAccess(propertyAccess, PropertyAccessKind.Get, diagnostics)
                        expr = propertyAccess.SetAccessKind(PropertyAccessKind.Get)

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(propertyAccess.AccessKind)
                End Select

            ElseIf expr.IsLateBound() Then

                Select Case expr.GetLateBoundAccessKind()
                    Case LateBoundAccessKind.Get
                        ' Nothing to do.

                    Case LateBoundAccessKind.Unknown
                        expr = expr.SetLateBoundAccessKind(LateBoundAccessKind.Get)

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(expr.GetLateBoundAccessKind())
                End Select
            End If

            Return expr
        End Function

        Private Function MakeRValueAndIgnoreDiagnostics(
           expr As BoundExpression
        ) As BoundExpression
            expr = MakeRValue(expr, BindingDiagnosticBag.Discarded)
            Return expr
        End Function

        Friend Function ReclassifyExpression(
           expr As BoundExpression,
           diagnostics As BindingDiagnosticBag
        ) As BoundExpression
            If expr.IsNothingLiteral() Then
                ' This is a Nothing literal without a type.
                ' Reclassify as Object.
                Return New BoundConversion(expr.Syntax, expr, ConversionKind.WideningNothingLiteral, False, False, expr.ConstantValueOpt,
                                           GetSpecialType(SpecialType.System_Object, expr.Syntax, diagnostics), Nothing)

            End If

            Select Case expr.Kind
                Case BoundKind.Parenthesized
                    ' Reclassify enclosed expression.
                    Dim parenthesized = DirectCast(expr, BoundParenthesized)
                    Dim enclosed As BoundExpression = ReclassifyExpression(parenthesized.Expression, diagnostics)
                    Return parenthesized.Update(enclosed, enclosed.Type)

                Case BoundKind.UnboundLambda
                    Return ReclassifyUnboundLambdaExpression(DirectCast(expr, UnboundLambda), diagnostics)

                Case BoundKind.AddressOfOperator
                    Dim address = DirectCast(expr, BoundAddressOfOperator)

                    If address.MethodGroup.ResultKind = LookupResultKind.Inaccessible Then
                        If address.MethodGroup.Methods.Length = 1 Then
                            ReportDiagnostic(diagnostics, address.MethodGroup.Syntax, GetInaccessibleErrorInfo(address.MethodGroup.Methods(0)))
                        Else
                            ReportDiagnostic(diagnostics, address.MethodGroup.Syntax, ERRID.ERR_NoViableOverloadCandidates1,
                                             address.MethodGroup.Methods(0).Name)
                        End If
                    Else
                        Debug.Assert(address.MethodGroup.ResultKind = LookupResultKind.Good)
                    End If

                Case BoundKind.ArrayLiteral
                    Return ReclassifyArrayLiteralExpression(DirectCast(expr, BoundArrayLiteral), diagnostics)

                Case BoundKind.TupleLiteral
                    Dim tupleLiteral = DirectCast(expr, BoundTupleLiteral)

                    If tupleLiteral.InferredType IsNot Nothing Then
                        Return ReclassifyTupleLiteralExpression(tupleLiteral, diagnostics)
                    End If

                Case Else
            End Select

            'TODO: We need to do other expression reclassifications here.
            '      For now, we simply report an error.

            ReportDiagnostic(diagnostics, expr.Syntax, ERRID.ERR_VoidValue)
            Return BadExpression(expr.Syntax, expr, ErrorTypeSymbol.UnknownResultType)

        End Function

        Private Function ReclassifyArrayLiteralExpression(conversionSemantics As SyntaxKind,
                                                          tree As SyntaxNode,
                                                          conv As ConversionKind,
                                                          isExplicit As Boolean,
                                                          arrayLiteral As BoundArrayLiteral,
                                                          destination As TypeSymbol,
                                                          diagnostics As BindingDiagnosticBag) As BoundExpression

            Debug.Assert((conv And ConversionKind.UserDefined) = 0)
            Debug.Assert(Not (TypeOf destination Is ArrayLiteralTypeSymbol)) 'An array literal should never be reclassified as an array literal.

            If Conversions.NoConversion(conv) AndAlso (conv And ConversionKind.FailedDueToArrayLiteralElementConversion) = 0 Then

                If Not arrayLiteral.HasDominantType Then
                    ' When there is a conversion error and there isn't a dominant type, report "error BC36717: Cannot infer an element type. 
                    ' Specifying the type of the array might correct this error." instead of the specific conversion error because the inferred
                    ' type used in classification was just a guess.
                    ReportDiagnostic(diagnostics, arrayLiteral.Syntax, ERRID.ERR_ArrayInitNoType)
                Else
                    ReportNoConversionError(arrayLiteral.Syntax, arrayLiteral.InferredType, destination, diagnostics, Nothing)
                End If

                ' Because we've already reported a no conversion error, ignore any diagnostics in ApplyImplicitConversion
                Dim argument As BoundExpression = ApplyImplicitConversion(arrayLiteral.Syntax, arrayLiteral.InferredType, arrayLiteral, BindingDiagnosticBag.Discarded)

                If conversionSemantics = SyntaxKind.CTypeKeyword Then
                    argument = New BoundConversion(tree, argument, conv, False, isExplicit, destination)
                ElseIf conversionSemantics = SyntaxKind.DirectCastKeyword Then
                    argument = New BoundDirectCast(tree, argument, conv, destination)
                ElseIf conversionSemantics = SyntaxKind.TryCastKeyword Then
                    argument = New BoundTryCast(tree, argument, conv, destination)
                Else
                    Throw ExceptionUtilities.UnexpectedValue(conversionSemantics)
                End If

                Return argument
            End If

            ' This code must be kept in sync with Conversions.ClassifyArrayLiteralConversion
            Dim sourceType = arrayLiteral.InferredType
            Dim targetType = TryCast(destination, NamedTypeSymbol)
            Dim originalTargetType = If(targetType IsNot Nothing, targetType.OriginalDefinition, Nothing)
            Dim targetArrayType As ArrayTypeSymbol = TryCast(destination, ArrayTypeSymbol)
            Dim targetElementType As TypeSymbol = Nothing

            If targetArrayType IsNot Nothing AndAlso (sourceType.Rank = targetArrayType.Rank OrElse arrayLiteral.IsEmptyArrayLiteral) Then
                targetElementType = targetArrayType.ElementType
                sourceType = targetArrayType

            ElseIf (sourceType.Rank = 1 OrElse arrayLiteral.IsEmptyArrayLiteral) AndAlso
                originalTargetType IsNot Nothing AndAlso
                (originalTargetType.SpecialType = SpecialType.System_Collections_Generic_IEnumerable_T OrElse
                 originalTargetType.SpecialType = SpecialType.System_Collections_Generic_IList_T OrElse
                 originalTargetType.SpecialType = SpecialType.System_Collections_Generic_ICollection_T OrElse
                 originalTargetType.SpecialType = SpecialType.System_Collections_Generic_IReadOnlyList_T OrElse
                 originalTargetType.SpecialType = SpecialType.System_Collections_Generic_IReadOnlyCollection_T) Then

                targetElementType = targetType.TypeArgumentsNoUseSiteDiagnostics(0)
                sourceType = ArrayTypeSymbol.CreateVBArray(targetElementType, Nothing, 1, Compilation)

            Else
                ' Use the inferred type
                targetArrayType = sourceType
                targetElementType = sourceType.ElementType
            End If

            ReportArrayLiteralDiagnostics(arrayLiteral, targetArrayType, diagnostics)

            Dim arrayInitialization As BoundArrayInitialization
            Dim bounds As ImmutableArray(Of BoundExpression)

            If arrayLiteral.IsEmptyArrayLiteral Then
                Dim knownSizes(sourceType.Rank - 1) As DimensionSize
                arrayInitialization = ReclassifyEmptyArrayInitialization(arrayLiteral, sourceType.Rank)
                bounds = CreateArrayBounds(arrayLiteral.Syntax, knownSizes, diagnostics)
            Else
                arrayInitialization = ReclassifyArrayInitialization(arrayLiteral.Initializer, targetElementType, diagnostics)
                bounds = arrayLiteral.Bounds
            End If

            ' Mark as compiler generated so that semantic model does not select the array initialization bound node.
            ' The array initialization node is not a real expression and lacks a type.
            arrayInitialization.SetWasCompilerGenerated()
            Debug.Assert(Not Conversions.IsIdentityConversion(conv))
            Dim arrayCreation = New BoundArrayCreation(arrayLiteral.Syntax, bounds, arrayInitialization, arrayLiteral, conv, sourceType)

            If conversionSemantics = SyntaxKind.CTypeKeyword Then
                Return ApplyConversion(tree, destination, arrayCreation, isExplicit, diagnostics)
            Else
                Dim expr As BoundExpression = arrayCreation

                ' Apply char() to string conversion before directcast/trycast
                conv = Conversions.ClassifyStringConversion(sourceType, destination)

                If Conversions.IsWideningConversion(conv) Then
                    expr = CreatePredefinedConversion(arrayLiteral.Syntax, arrayCreation, conv, isExplicit, destination, diagnostics)
                End If

                If conversionSemantics = SyntaxKind.DirectCastKeyword Then
                    Return ApplyDirectCastConversion(tree, expr, destination, diagnostics)
                ElseIf conversionSemantics = SyntaxKind.TryCastKeyword Then
                    Return ApplyTryCastConversion(tree, expr, destination, diagnostics)
                Else
                    Throw ExceptionUtilities.UnexpectedValue(conversionSemantics)
                End If
            End If

        End Function

        Private Sub ReportArrayLiteralDiagnostics(arrayLiteral As BoundArrayLiteral, targetArrayType As ArrayTypeSymbol, diagnostics As BindingDiagnosticBag)
            If targetArrayType Is arrayLiteral.InferredType Then
                ' Note, array type symbols do not preserve identity. If the target array is the same as the inferred array then we
                ' assume that the target has inferred its type from the array literal.
                ReportArrayLiteralInferredTypeDiagnostics(arrayLiteral, diagnostics)
            End If
        End Sub

        Private Sub ReportArrayLiteralInferredTypeDiagnostics(arrayLiteral As BoundArrayLiteral, diagnostics As BindingDiagnosticBag)
            Dim targetElementType = arrayLiteral.InferredType.ElementType

            If targetElementType.IsRestrictedType Then
                ReportDiagnostic(diagnostics, arrayLiteral.Syntax, ERRID.ERR_RestrictedType1, targetElementType)

            ElseIf Not arrayLiteral.HasDominantType Then
                ReportDiagnostic(diagnostics, arrayLiteral.Syntax, ERRID.ERR_ArrayInitNoType)
            Else
                ' Possibly warn or report an error depending on the value of option strict
                Select Case OptionStrict
                    Case VisualBasic.OptionStrict.On
                        If arrayLiteral.NumberOfCandidates = 0 Then
                            ReportDiagnostic(diagnostics, arrayLiteral.Syntax, ERRID.ERR_ArrayInitNoTypeObjectDisallowed)
                        ElseIf arrayLiteral.NumberOfCandidates > 1 Then
                            ReportDiagnostic(diagnostics, arrayLiteral.Syntax, ERRID.ERR_ArrayInitTooManyTypesObjectDisallowed)
                        End If
                    Case VisualBasic.OptionStrict.Custom
                        If arrayLiteral.NumberOfCandidates = 0 Then
                            ReportDiagnostic(diagnostics, arrayLiteral.Syntax, ErrorFactory.ErrorInfo(ERRID.WRN_ObjectAssumed1,
                                                                                                      ErrorFactory.ErrorInfo(ERRID.WRN_ArrayInitNoTypeObjectAssumed)))
                        ElseIf arrayLiteral.NumberOfCandidates > 1 Then
                            ReportDiagnostic(diagnostics, arrayLiteral.Syntax, ErrorFactory.ErrorInfo(ERRID.WRN_ObjectAssumed1,
                                                                                                      ErrorFactory.ErrorInfo(ERRID.WRN_ArrayInitTooManyTypesObjectAssumed)))

                        End If
                End Select
            End If

        End Sub

        Private Function ReclassifyArrayInitialization(arrayInitialization As BoundArrayInitialization, elementType As TypeSymbol, diagnostics As BindingDiagnosticBag) As BoundArrayInitialization
            Dim initializers = ArrayBuilder(Of BoundExpression).GetInstance

            ' Apply implicit conversion to the elements.
            For Each expr In arrayInitialization.Initializers
                If expr.Kind = BoundKind.ArrayInitialization Then
                    expr = ReclassifyArrayInitialization(DirectCast(expr, BoundArrayInitialization), elementType, diagnostics)
                Else
                    expr = ApplyImplicitConversion(expr.Syntax, elementType, expr, diagnostics)
                End If
                initializers.Add(expr)
            Next

            arrayInitialization = New BoundArrayInitialization(arrayInitialization.Syntax, initializers.ToImmutableAndFree, Nothing)
            Return arrayInitialization
        End Function

        Private Function ReclassifyEmptyArrayInitialization(arrayLiteral As BoundArrayLiteral, rank As Integer) As BoundArrayInitialization

            Dim arrayInitialization As BoundArrayInitialization = arrayLiteral.Initializer

            If rank = 1 Then
                Return arrayInitialization
            End If

            Dim initializers = ImmutableArray(Of BoundExpression).Empty

            For i = 1 To rank - 1
                arrayInitialization = New BoundArrayInitialization(arrayInitialization.Syntax, initializers, Nothing).MakeCompilerGenerated()
                initializers = ImmutableArray.Create(Of BoundExpression)(arrayInitialization)
            Next

            Return New BoundArrayInitialization(arrayInitialization.Syntax, initializers, Nothing)
        End Function

        Private Function ReclassifyTupleLiteralExpression(
           tupleLiteral As BoundTupleLiteral,
           diagnostics As BindingDiagnosticBag
        ) As BoundExpression

            Return ApplyImplicitConversion(tupleLiteral.Syntax,
                                           tupleLiteral.InferredType,
                                           tupleLiteral,
                                           diagnostics)
        End Function

        Private Function ReclassifyArrayLiteralExpression(
                                                         arrayLiteral As BoundArrayLiteral,
                                                         diagnostics As BindingDiagnosticBag
                                                         ) As BoundExpression
            Return ApplyImplicitConversion(arrayLiteral.Syntax,
                                           arrayLiteral.InferredType,
                                           arrayLiteral,
                                           diagnostics)
        End Function

        Private Function ReclassifyUnboundLambdaExpression(
           lambda As UnboundLambda,
           diagnostics As BindingDiagnosticBag
        ) As BoundExpression
            Return ApplyImplicitConversion(lambda.Syntax,
                                           lambda.InferredAnonymousDelegate.Key,
                                           lambda,
                                           diagnostics,
                                           isOperandOfConditionalBranch:=False)
        End Function

        Private Function BindAssignmentTarget(
            node As ExpressionSyntax,
            diagnostics As BindingDiagnosticBag
        ) As BoundExpression
            Dim expression = BindExpression(node, diagnostics)

            Return BindAssignmentTarget(node, expression, diagnostics)
        End Function

        Private Function BindAssignmentTarget(
            node As SyntaxNode,
            expression As BoundExpression,
            diagnostics As BindingDiagnosticBag
        ) As BoundExpression
            expression = ReclassifyAsValue(expression, diagnostics)

            If Not IsValidAssignmentTarget(expression) Then
                If Not expression.HasErrors Then
                    ReportAssignmentToRValue(expression, diagnostics)
                End If

                expression = BadExpression(node, expression, LookupResultKind.NotAVariable, ErrorTypeSymbol.UnknownResultType)

            ElseIf expression.Kind = BoundKind.LateInvocation Then
                ' Since this is a target of an assignment, it is guaranteed to be an array or a property, 
                ' therefore, arguments will be passed ByVal, let's capture this fact in the tree,
                ' this will simplify analysis later.
                Dim invocation = DirectCast(expression, BoundLateInvocation)

                If Not invocation.ArgumentsOpt.IsEmpty Then
                    Dim newArguments(invocation.ArgumentsOpt.Length - 1) As BoundExpression
                    For i As Integer = 0 To newArguments.Length - 1
                        newArguments(i) = MakeRValue(invocation.ArgumentsOpt(i), diagnostics)
                    Next

                    expression = invocation.Update(invocation.Member,
                                                   newArguments.AsImmutableOrNull(),
                                                   invocation.ArgumentNamesOpt,
                                                   invocation.AccessKind,
                                                   invocation.MethodOrPropertyGroupOpt,
                                                   invocation.Type)
                End If
            End If

            Return expression
        End Function

        Friend Shared Function IsValidAssignmentTarget(expression As BoundExpression) As Boolean
            Select Case expression.Kind
                Case BoundKind.PropertyAccess
                    Dim propertyAccess = DirectCast(expression, BoundPropertyAccess)
                    Dim [property] = propertyAccess.PropertySymbol
                    Dim receiver = propertyAccess.ReceiverOpt

                    Debug.Assert(propertyAccess.AccessKind <> PropertyAccessKind.Get)
                    Return propertyAccess.AccessKind <> PropertyAccessKind.Get AndAlso
                        ([property].IsShared OrElse
                        receiver Is Nothing OrElse
                        receiver.IsLValue() OrElse
                        receiver.IsMeReference() OrElse
                        receiver.IsMyClassReference() OrElse
                        Not receiver.Type.IsValueType) ' If this logic changes, logic in UseTwiceRewriter.UseTwicePropertyAccess might need to change too.

                Case BoundKind.XmlMemberAccess
                    Return IsValidAssignmentTarget(DirectCast(expression, BoundXmlMemberAccess).MemberAccess)

                Case BoundKind.Call
                    Return DirectCast(expression, BoundCall).IsLValue

                Case BoundKind.LateInvocation
                    Dim invocation = DirectCast(expression, BoundLateInvocation)
                    Debug.Assert(invocation.AccessKind <> LateBoundAccessKind.Get AndAlso invocation.AccessKind <> LateBoundAccessKind.Call)
                    Return invocation.AccessKind <> LateBoundAccessKind.Get AndAlso invocation.AccessKind <> LateBoundAccessKind.Call

                Case BoundKind.LateMemberAccess
                    Dim member = DirectCast(expression, BoundLateMemberAccess)
                    Debug.Assert(member.AccessKind <> LateBoundAccessKind.Get AndAlso member.AccessKind <> LateBoundAccessKind.Call)
                    Return member.AccessKind <> LateBoundAccessKind.Get AndAlso member.AccessKind <> LateBoundAccessKind.Call

                Case Else
                    Return expression.IsLValue

            End Select
        End Function

        Private Shared Sub ReportAssignmentToRValue(expr As BoundExpression, diagnostics As BindingDiagnosticBag)
            Dim err As ERRID

            If expr.IsConstant Then
                err = ERRID.ERR_CantAssignToConst

            ElseIf ExpressionRefersToReadonlyVariable(expr) Then
                err = ERRID.ERR_ReadOnlyAssignment

            Else
                err = ERRID.ERR_LValueRequired
            End If

            ReportDiagnostic(diagnostics, expr.Syntax, err)
        End Sub

        Public Shared Function ExpressionRefersToReadonlyVariable(
            node As BoundExpression,
            Optional digThroughProperty As Boolean = True
        ) As Boolean

            ' TODO: Check base expressions for properties if digThroughProperty==true.

            If node.Kind = BoundKind.FieldAccess Then
                Dim field = DirectCast(node, BoundFieldAccess)

                If field.FieldSymbol.IsReadOnly Then
                    Return True
                End If

                Dim base = field.ReceiverOpt

                If base IsNot Nothing AndAlso base.IsValue() AndAlso
                   base.Type.IsValueType Then
                    Return ExpressionRefersToReadonlyVariable(base, False)
                End If

            ElseIf node.Kind = BoundKind.Local Then
                Return DirectCast(node, BoundLocal).LocalSymbol.IsReadOnly
            End If

            Return False
        End Function

        ''' <summary>
        ''' Determine whether field access should be treated as LValue. 
        ''' </summary>
        Friend Function IsLValueFieldAccess(field As FieldSymbol, receiver As BoundExpression) As Boolean

            If field.IsConst Then
                Return False
            End If

            If Not field.IsShared AndAlso
                receiver IsNot Nothing AndAlso
                receiver.IsValue() Then

                Dim receiverType = receiver.Type
                Debug.Assert(Not receiverType.IsTypeParameter() OrElse receiverType.IsReferenceType,
                            "Member variable access through non-class constrained type param unexpected!!!")

                ' Dev10 comment:
                ' Note that this is needed so that we can determine whether the structure
                ' is not an LValue (eg: RField.m_x = 20 where goo is a readonly field of a
                ' structure type). In such cases, the structure's field m_x cannot be modified.
                '
                ' This does not apply to type params because for type params we do want to
                ' allow setting the fields even in such scenarios because only class constrained
                ' type params have fields and readonly reference typed fields' fields can be
                ' modified.

                If Not receiverType.IsTypeParameter() AndAlso
                    receiverType.IsValueType AndAlso
                    Not receiver.IsLValue() AndAlso
                    Not receiver.IsMeReference() AndAlso
                    Not receiver.IsMyClassReference() Then
                    Return False
                End If
            End If

            If Not field.IsReadOnly Then
                Return True
            End If

            Dim containingMethodKind As MethodKind = Me.KindOfContainingMethodAtRunTime()

            If containingMethodKind = MethodKind.Constructor Then
                If field.IsShared OrElse Not (receiver IsNot Nothing AndAlso receiver.IsInstanceReference()) Then
                    Return False
                End If
            ElseIf containingMethodKind = MethodKind.SharedConstructor Then
                If Not field.IsShared Then
                    Return False
                End If
            Else
                Return False
            End If

            ' We are in constructor, now verify that the field belongs to constructor's type.

            ' Note, ReadOnly fields accessed in lambda within the constructor are not LValues because the
            ' lambda in the end will be generated as a separate procedure where the ReadOnly field is not 
            ' an LValue. In this case, containingMember will be a LambdaSymbol rather than a symbol for
            ' constructor.

            ' We duplicate a bug in the native compiler for compatibility in non-strict mode
            Return If(Me.Compilation.FeatureStrictEnabled,
                Me.ContainingMember.ContainingSymbol Is field.ContainingSymbol,
                Me.ContainingMember.ContainingSymbol.OriginalDefinition Is field.ContainingSymbol.OriginalDefinition)
        End Function

        ''' <summary>
        ''' Return MethodKind corresponding to the method the code being interpreted is going to end up in.
        ''' </summary>
        Private Function KindOfContainingMethodAtRunTime() As MethodKind
            Dim containingMember = Me.ContainingMember

            If containingMember IsNot Nothing Then
                Select Case containingMember.Kind
                    Case SymbolKind.Method
                        ' Binding a method body.
                        Return DirectCast(containingMember, MethodSymbol).MethodKind

                    Case SymbolKind.Field, SymbolKind.Property
                        ' Binding field or property initializer.
                        If containingMember.IsShared Then
                            Return MethodKind.SharedConstructor
                        Else
                            Return MethodKind.Constructor
                        End If

                    Case SymbolKind.NamedType, SymbolKind.Namespace, SymbolKind.Parameter
                        Exit Select

                    Case Else
                        ' What else can it be?
                        Throw ExceptionUtilities.UnexpectedValue(containingMember.Kind)
                End Select
            End If

            Return MethodKind.Ordinary ' Looks like a good default.
        End Function

        Private Function BindTernaryConditionalExpression(node As TernaryConditionalExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression

            '  bind arguments as values
            Dim boundConditionArg = BindBooleanExpression(node.Condition, diagnostics)
            Dim boundWhenTrueArg = BindValue(node.WhenTrue, diagnostics)
            Dim boundWhenFalseArg = BindValue(node.WhenFalse, diagnostics)

            Dim hasErrors = boundConditionArg.HasErrors OrElse boundWhenTrueArg.HasErrors OrElse boundWhenFalseArg.HasErrors

            '  infer dominant type of the resulting expression
            Dim dominantType As TypeSymbol
            If boundWhenTrueArg.IsNothingLiteral AndAlso boundWhenFalseArg.IsNothingLiteral Then
                ' From Dev10: backwards compatibility with Orcas... IF(b,Nothing,Nothing) infers Object with no complaint
                dominantType = GetSpecialType(SpecialType.System_Object, node, diagnostics)

            Else
                Dim numCandidates As Integer = 0
                Dim array = ArrayBuilder(Of BoundExpression).GetInstance(2)
                array.Add(boundWhenTrueArg)
                array.Add(boundWhenFalseArg)

                dominantType = InferDominantTypeOfExpressions(node, array, diagnostics, numCandidates)
                array.Free()

                '  check the resulting type
                If Not hasErrors Then
                    hasErrors = GenerateDiagnosticsForDominantTypeInferenceInIfExpression(dominantType, numCandidates, node, diagnostics)
                End If
            End If

            '  Void type will be filtered out in BindValue calls
            Debug.Assert(dominantType Is Nothing OrElse Not dominantType.IsVoidType())

            '  convert arguments to the dominant type if necessary
            If Not hasErrors OrElse dominantType IsNot Nothing Then

                boundWhenTrueArg = Me.ApplyImplicitConversion(node.WhenTrue, dominantType, boundWhenTrueArg, diagnostics)
                boundWhenFalseArg = Me.ApplyImplicitConversion(node.WhenFalse, dominantType, boundWhenFalseArg, diagnostics)

                hasErrors = hasErrors OrElse boundWhenTrueArg.HasErrors OrElse boundWhenFalseArg.HasErrors
            Else
                boundWhenTrueArg = MakeRValueAndIgnoreDiagnostics(boundWhenTrueArg)
                boundWhenFalseArg = MakeRValueAndIgnoreDiagnostics(boundWhenFalseArg)
            End If

            '  check for a constant value
            Dim constVal As ConstantValue = Nothing
            If Not hasErrors AndAlso IsConstantAllowingCompileTimeFolding(boundWhenTrueArg) AndAlso
                    IsConstantAllowingCompileTimeFolding(boundWhenFalseArg) AndAlso IsConstantAllowingCompileTimeFolding(boundConditionArg) Then
                constVal = If(boundConditionArg.ConstantValueOpt.BooleanValue, boundWhenTrueArg.ConstantValueOpt, boundWhenFalseArg.ConstantValueOpt)
            End If

            Return New BoundTernaryConditionalExpression(node,
                                                         boundConditionArg,
                                                         boundWhenTrueArg,
                                                         boundWhenFalseArg,
                                                         constVal,
                                                         If(dominantType, ErrorTypeSymbol.UnknownResultType),
                                                         hasErrors:=hasErrors)
        End Function

        Private Shared Function IsConstantAllowingCompileTimeFolding(candidate As BoundExpression) As Boolean
            Return candidate.IsConstant AndAlso
                   Not candidate.ConstantValueOpt.IsBad AndAlso
                   (candidate.IsNothingLiteral OrElse (candidate.Type IsNot Nothing AndAlso candidate.Type.AllowsCompileTimeOperations()))
        End Function

        Private Function BindBinaryConditionalExpression(node As BinaryConditionalExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression

            '  bind arguments
            Dim boundFirstArg = BindValue(node.FirstExpression, diagnostics)
            Dim boundSecondArg = BindValue(node.SecondExpression, diagnostics)

            Dim hasErrors = boundFirstArg.HasErrors OrElse boundSecondArg.HasErrors OrElse node.ContainsDiagnostics

            '  infer dominant type of the resulting expression
            Dim dominantType As TypeSymbol
            If boundFirstArg.IsNothingLiteral AndAlso boundSecondArg.IsNothingLiteral Then
                ' SPECIAL CASE (Dev10): IF(Nothing,Nothing) yields type System.Object
                ' NOTE: Reuse System.Object from boundFirstArg or boundSecondArg if exists
                dominantType = If(boundFirstArg.Type, If(boundSecondArg.Type, GetSpecialType(SpecialType.System_Object, node, diagnostics)))

            ElseIf boundFirstArg.Type IsNot Nothing AndAlso boundFirstArg.Type.IsNullableType AndAlso boundSecondArg.IsNothingLiteral Then
                ' SPECIAL CASE (Dev10): IF(Nullable<T>, Nothing) yields type Nullable<T>, whereas IF(Nullable<Int>, Int) yields Int.
                dominantType = boundFirstArg.Type

            Else
                '  calculate dominant type
                Dim numCandidates As Integer = 0
                Dim array = ArrayBuilder(Of BoundExpression).GetInstance(2)
                If boundFirstArg.Type IsNot Nothing AndAlso boundFirstArg.Type.IsNullableType AndAlso
                            Not (boundSecondArg.Type IsNot Nothing AndAlso boundSecondArg.Type.IsNullableType) Then

                    ' From Dev10: Special case: "nullable lifting": when the first argument has a value of nullable 
                    '               data type and the second does not, the first is being converted to underlying type
                    '  create a temp variable
                    Dim underlyingType = boundFirstArg.Type.GetNullableUnderlyingType
                    array.Add(New BoundRValuePlaceholder(node.FirstExpression,
                                                underlyingType))
                Else
                    array.Add(boundFirstArg)
                End If

                array.Add(boundSecondArg)

                dominantType = InferDominantTypeOfExpressions(node, array, diagnostics, numCandidates)
                array.Free()

                '  check the resulting type
                If Not hasErrors Then
                    hasErrors = GenerateDiagnosticsForDominantTypeInferenceInIfExpression(dominantType, numCandidates, node, diagnostics)
                End If

            End If

            '  check for a constant value
            If Not hasErrors AndAlso IsConstantAllowingCompileTimeFolding(boundFirstArg) AndAlso
                    IsConstantAllowingCompileTimeFolding(boundSecondArg) AndAlso
                    (boundFirstArg.IsNothingLiteral OrElse boundFirstArg.ConstantValueOpt.IsString) Then

                Dim constVal As ConstantValue

                If (boundFirstArg.IsNothingLiteral) Then
                    constVal = boundSecondArg.ConstantValueOpt

                    If Not boundSecondArg.IsNothingLiteral Then
                        dominantType = boundSecondArg.Type
                    Else
                        Debug.Assert(dominantType.IsObjectType)
                    End If
                Else
                    constVal = boundFirstArg.ConstantValueOpt
                    dominantType = boundFirstArg.Type
                End If

                '  return binary conditional expression to be constant-folded later
                Return AnalyzeConversionAndCreateBinaryConditionalExpression(
                                    node,
                                    testExpression:=boundFirstArg,
                                    elseExpression:=boundSecondArg,
                                    constantValueOpt:=constVal,
                                    type:=dominantType,
                                    hasErrors:=False,
                                    diagnostics:=diagnostics)
            End If
            ' NOTE: no constant folding after this point

            ' By this time Void type will be filtered out in BindValue calls, and empty dominant type reported as an error
            Debug.Assert(hasErrors OrElse (dominantType IsNot Nothing AndAlso Not dominantType.IsVoidType()))

            '  address some cases of Type being Nothing before making an RValue of it
            If Not hasErrors AndAlso boundFirstArg.Type Is Nothing Then
                If boundFirstArg.IsNothingLiteral Then
                    '  leave Nothing literal unchanged
                Else
                    '  convert lambdas, AddressOf, etc. to dominant type
                    boundFirstArg = Me.ApplyImplicitConversion(node.FirstExpression, dominantType, boundFirstArg, diagnostics)
                    hasErrors = boundFirstArg.HasErrors
                End If
            End If

            ' TODO: Address array initializers, they might need to change type.

            '  make r-value out of boundFirstArg; this will reclassify property access from Unknown to Get and
            '                                     also mark not-nothing expressions without type with errors
            If boundFirstArg.IsNothingLiteral Then
                ' Don't do anything for nothing literal
            ElseIf Not hasErrors Then
                boundFirstArg = MakeRValue(boundFirstArg, diagnostics)
                hasErrors = boundFirstArg.HasErrors
            Else
                boundFirstArg = MakeRValueAndIgnoreDiagnostics(boundFirstArg)
            End If

            ' Type of the first expression should be set by now
            Debug.Assert(hasErrors OrElse boundFirstArg.IsNothingLiteral OrElse boundFirstArg.Type IsNot Nothing)

            Dim boundSecondArgWithConversions As BoundExpression = boundSecondArg
            If Not hasErrors Then
                boundSecondArgWithConversions = Me.ApplyImplicitConversion(node.SecondExpression, dominantType, boundSecondArg, diagnostics)
                hasErrors = boundSecondArgWithConversions.HasErrors
            Else
                boundSecondArgWithConversions = MakeRValueAndIgnoreDiagnostics(boundSecondArg)
            End If

            ' If there are still no errors check the original type of the first argument. First, we check
            ' the pre-VB 16.0 condition, which is the first operand must be Nothing, a reference type, or
            ' a nullable value type
            If Not hasErrors AndAlso Not (boundFirstArg.IsNothingLiteral OrElse boundFirstArg.Type.IsNullableType OrElse boundFirstArg.Type.IsReferenceType) Then
                ' VB 16 changed the requirements on the first operand to permit unconstrained type parameters. If we're in that scenario,
                ' ensure that the feature is enabled and report an error if it is not
                If Not boundFirstArg.Type.IsValueType Then
                    InternalSyntax.Parser.CheckFeatureAvailability(diagnostics,
                                                                   node.Location,
                                                                   DirectCast(node.SyntaxTree.Options, VisualBasicParseOptions).LanguageVersion,
                                                                   InternalSyntax.Feature.UnconstrainedTypeParameterInConditional)
                Else
                    ReportDiagnostic(diagnostics, node.FirstExpression, ERRID.ERR_IllegalCondTypeInIIF)
                    hasErrors = True
                End If
            End If

            Return AnalyzeConversionAndCreateBinaryConditionalExpression(
                                    node,
                                    testExpression:=boundFirstArg,
                                    elseExpression:=boundSecondArgWithConversions,
                                    constantValueOpt:=Nothing,
                                    type:=If(dominantType, ErrorTypeSymbol.UnknownResultType),
                                    hasErrors:=hasErrors,
                                    diagnostics:=diagnostics)
        End Function

        Private Function AnalyzeConversionAndCreateBinaryConditionalExpression(
                                        syntax As SyntaxNode,
                                        testExpression As BoundExpression,
                                        elseExpression As BoundExpression,
                                        constantValueOpt As ConstantValue,
                                        type As TypeSymbol,
                                        hasErrors As Boolean,
                                        diagnostics As BindingDiagnosticBag,
                                        Optional explicitConversion As Boolean = False) As BoundExpression

            Dim convertedTestExpression As BoundExpression = Nothing
            Dim placeholder As BoundRValuePlaceholder = Nothing

            If Not hasErrors Then

                ' Do we need to apply a placeholder?
                If Not testExpression.IsConstant Then
                    Debug.Assert(Not testExpression.IsLValue)
                    placeholder = New BoundRValuePlaceholder(testExpression.Syntax, testExpression.Type.GetNullableUnderlyingTypeOrSelf())
                End If

                ' apply a conversion
                convertedTestExpression = ApplyConversion(testExpression.Syntax, type,
                                                          If(placeholder, testExpression),
                                                          explicitConversion, diagnostics)

                If convertedTestExpression Is If(placeholder, testExpression) Then
                    convertedTestExpression = Nothing
                    placeholder = Nothing
                End If
            End If

            Return New BoundBinaryConditionalExpression(syntax,
                                                        testExpression:=testExpression,
                                                        convertedTestExpression:=convertedTestExpression,
                                                        testExpressionPlaceholder:=placeholder,
                                                        elseExpression:=elseExpression,
                                                        constantValueOpt:=constantValueOpt,
                                                        type:=type,
                                                        hasErrors:=hasErrors)
        End Function

        ''' <summary> Process the result of dominant type inference, generate diagnostics </summary>
        Private Function GenerateDiagnosticsForDominantTypeInferenceInIfExpression(dominantType As TypeSymbol, numCandidates As Integer,
                                       node As ExpressionSyntax, diagnostics As BindingDiagnosticBag) As Boolean
            Dim hasErrors As Boolean = False
            If dominantType Is Nothing Then
                ReportDiagnostic(diagnostics, node, ERRID.ERR_IfNoType)
                hasErrors = True

            ElseIf numCandidates = 0 Then

                '  TODO: Is this reachable? Check and add tests
                If OptionStrict = VisualBasic.OptionStrict.On Then
                    ReportDiagnostic(diagnostics, node, ERRID.ERR_IfNoTypeObjectDisallowed)
                    hasErrors = True

                ElseIf OptionStrict = VisualBasic.OptionStrict.Custom Then
                    ReportDiagnostic(diagnostics, node, ErrorFactory.ErrorInfo(ERRID.WRN_ObjectAssumed1, ErrorFactory.ErrorInfo(ERRID.WRN_IfNoTypeObjectAssumed)))
                End If

            ElseIf numCandidates > 1 Then

                If OptionStrict = VisualBasic.OptionStrict.On Then
                    ReportDiagnostic(diagnostics, node, ERRID.ERR_IfTooManyTypesObjectDisallowed)
                    hasErrors = True

                ElseIf OptionStrict = VisualBasic.OptionStrict.Custom Then
                    ReportDiagnostic(diagnostics, node, ErrorFactory.ErrorInfo(ERRID.WRN_ObjectAssumed1, ErrorFactory.ErrorInfo(ERRID.WRN_IfTooManyTypesObjectAssumed)))
                End If

            End If
            Return hasErrors
        End Function

        ''' <summary>
        ''' True if inside in binding arguments of constructor 
        ''' call with {'Me'/'MyClass'/'MyBase'}.New(...) from another constructor
        ''' </summary>
        Protected Overridable ReadOnly Property IsInsideChainedConstructorCallArguments As Boolean
            Get
                Return Me.ContainingBinder.IsInsideChainedConstructorCallArguments
            End Get
        End Property

        Private Function IsMeOrMyBaseOrMyClassInSharedContext() As Boolean
            ' If we are inside an attribute then we are not in an instance context.
            If Me.BindingLocation = VisualBasic.BindingLocation.Attribute Then
                Return True
            End If

            Dim containingMember = Me.ContainingMember
            If containingMember IsNot Nothing Then
                Select Case containingMember.Kind
                    Case SymbolKind.Method, SymbolKind.Property
                        Return containingMember.IsShared OrElse
                               Me.ContainingType.IsModuleType

                    Case SymbolKind.Field
                        Return containingMember.IsShared OrElse
                               Me.ContainingType.IsModuleType OrElse
                               DirectCast(containingMember, FieldSymbol).IsConst
                End Select
            End If
            Return True
        End Function

        Private Function CheckMeOrMyBaseOrMyClassInSharedOrDisallowedContext(implicitReference As Boolean, <Out()> ByRef errorId As ERRID) As Boolean
            errorId = Nothing

            ' Any executable statement in a script class can access Me/MyClass/MyBase implicitly but not explicitly.
            ' No code in a script class is shared.
            Dim containingType = Me.ContainingType
            If containingType IsNot Nothing AndAlso containingType.IsScriptClass Then
                If implicitReference Then
                    Return True
                Else
                    errorId = ERRID.ERR_KeywordNotAllowedInScript
                    Return False
                End If
            End If

            If IsMeOrMyBaseOrMyClassInSharedContext() Then
                errorId = If(implicitReference,
                             ERRID.ERR_BadInstanceMemberAccess,
                             If(containingType IsNot Nothing AndAlso containingType.IsModuleType,
                                ERRID.ERR_UseOfKeywordFromModule1,
                                ERRID.ERR_UseOfKeywordNotInInstanceMethod1))
                Return False

            ElseIf IsInsideChainedConstructorCallArguments Then
                errorId = If(implicitReference, ERRID.ERR_InvalidImplicitMeReference, ERRID.ERR_InvalidMeReference)
                Return False
            End If

            Return True
        End Function

        ''' <summary>
        ''' Can we access MyBase in this location. If False is returned, 
        ''' also returns the error id associated with that.
        ''' </summary>
        Private Function CanAccessMyBase(implicitReference As Boolean, <Out()> ByRef errorId As ERRID) As Boolean
            errorId = Nothing

            If Not CheckMeOrMyBaseOrMyClassInSharedOrDisallowedContext(implicitReference, errorId) Then
                Return False
            End If

            If ContainingType.IsStructureType Then
                errorId = ERRID.ERR_UseOfKeywordFromStructure1
                Return False
            End If

            '  TODO: Find a test case for ERRID_UseOfKeywordOutsideClass1
            Debug.Assert(ContainingType.IsClassType)

            ' TODO: Check for closures

            Return True
        End Function

        Private Function CanAccessMeOrMyClass(implicitReference As Boolean, <Out()> ByRef errorId As ERRID) As Boolean
            errorId = Nothing
            Return CheckMeOrMyBaseOrMyClassInSharedOrDisallowedContext(implicitReference, errorId)
        End Function

        ''' <summary>
        ''' Can we access Me in this location. If False is returned, 
        ''' also returns the error id associated with that.
        ''' </summary>
        Friend Function CanAccessMe(implicitReference As Boolean, <Out()> ByRef errorId As ERRID) As Boolean
            errorId = Nothing

            '  TODO: Find a test case for ERRID_UseOfKeywordOutsideClass1
            Return CanAccessMeOrMyClass(implicitReference, errorId)
        End Function

        ''' <summary>
        ''' Can we access MyClass in this location. If False is returned, 
        ''' also returns the error id associated with that.
        ''' </summary>
        Private Function CanAccessMyClass(implicitReference As Boolean, <Out()> ByRef errorId As ERRID) As Boolean
            errorId = Nothing

            If Me.ContainingType IsNot Nothing AndAlso Me.ContainingType.IsModuleType Then
                errorId = ERRID.ERR_MyClassNotInClass
                Return False
            End If

            Return CanAccessMeOrMyClass(implicitReference, errorId)
        End Function

        Private Function BindMeExpression(node As MeExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundMeReference
            Dim err As ERRID = Nothing

            If Not CanAccessMe(False, err) Then
                ReportDiagnostic(diagnostics, node, err, SyntaxFacts.GetText(node.Keyword.Kind))
                Return New BoundMeReference(node, If(Me.ContainingType, ErrorTypeSymbol.UnknownResultType), hasErrors:=True)
            End If

            Return CreateMeReference(node)
        End Function

        ' Create a reference to Me, without error checking.
        Private Function CreateMeReference(node As SyntaxNode, Optional isSynthetic As Boolean = False) As BoundMeReference
            Dim containingMethod = TryCast(ContainingMember, MethodSymbol)
            Dim result = New BoundMeReference(node, If(Me.ContainingType, ErrorTypeSymbol.UnknownResultType))

            If isSynthetic Then
                result.SetWasCompilerGenerated()
            End If

            Return result
        End Function

        Private Function BindMyBaseExpression(node As MyBaseExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundMyBaseReference
            Dim err As ERRID = Nothing

            If Not CanAccessMyBase(False, err) Then
                ReportDiagnostic(diagnostics, node, err, SyntaxFacts.GetText(node.Keyword.Kind))
                Return New BoundMyBaseReference(node, If(Me.ContainingType IsNot Nothing, Me.ContainingType.BaseTypeNoUseSiteDiagnostics, ErrorTypeSymbol.UnknownResultType), hasErrors:=True)
            End If

            Dim containingMethod = TryCast(ContainingMember, MethodSymbol)
            Return New BoundMyBaseReference(node, If(Me.ContainingType IsNot Nothing, Me.ContainingType.BaseTypeNoUseSiteDiagnostics, ErrorTypeSymbol.UnknownResultType))
        End Function

        Private Function BindMyClassExpression(node As MyClassExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundMyClassReference
            Dim err As ERRID = Nothing

            If Not CanAccessMyClass(False, err) Then
                ReportDiagnostic(diagnostics, node, err, SyntaxFacts.GetText(node.Keyword.Kind))
                Return New BoundMyClassReference(node, If(Me.ContainingType, ErrorTypeSymbol.UnknownResultType), hasErrors:=True)
            End If

            Dim containingMethod = TryCast(ContainingMember, MethodSymbol)
            Return New BoundMyClassReference(node, If(Me.ContainingType, ErrorTypeSymbol.UnknownResultType))
        End Function

        ' Can the given name syntax be an implicit declared variables. Only some syntactic locations are permissible
        ' for implicitly declared variables. They are disallowed in:
        '    target of invocation
        '    LHS of member access
        '    
        ' Also, For, For Each, and Catch can implicitly declare a variable, but that implicit declaration has
        ' different rules that is handled directly in the binding of those statements. Thus, they are disallowed here.
        '
        ' Finally, Dev10 disallows 3 special names: "Null", "Empty", and "Rnd".
        Private Shared Function CanBeImplicitVariableDeclaration(nameSyntax As SimpleNameSyntax) As Boolean
            ' Disallow generic names.
            If nameSyntax.Kind <> SyntaxKind.IdentifierName Then
                Return False
            End If

            Dim parent As VisualBasicSyntaxNode = nameSyntax.Parent

            If parent IsNot Nothing Then
                Select Case parent.Kind
                    Case SyntaxKind.SimpleMemberAccessExpression ' intentionally NOT SyntaxKind.DictionaryAccess
                        If DirectCast(parent, MemberAccessExpressionSyntax).Expression Is nameSyntax Then
                            Return False
                        End If

                    Case SyntaxKind.InvocationExpression
                        If DirectCast(parent, InvocationExpressionSyntax).Expression Is nameSyntax Then
                            ' Name is the expression part of an invocation. 
                            Return False
                        End If

                    Case SyntaxKind.ConditionalAccessExpression
                        Dim conditionalAccess = DirectCast(parent, ConditionalAccessExpressionSyntax)

                        If conditionalAccess.Expression Is nameSyntax Then
                            Dim leaf As ExpressionSyntax = conditionalAccess.GetLeafAccess()

                            If leaf IsNot Nothing AndAlso
                               (leaf.Kind = SyntaxKind.SimpleMemberAccessExpression OrElse leaf.Kind = SyntaxKind.InvocationExpression) Then
                                Return False
                            End If
                        End If

                    Case SyntaxKind.CatchStatement
                        If DirectCast(parent, CatchStatementSyntax).IdentifierName Is nameSyntax Then
                            Return False
                        End If
                End Select
            End If

            ' Dev10 disallows implicit variable creation for "Null", "Empty", and "RND".
            Dim name As String = MakeHalfWidthIdentifier(nameSyntax.Identifier.ValueText)
            If CaseInsensitiveComparison.Equals(name, "Null") OrElse CaseInsensitiveComparison.Equals(name, "Empty") OrElse CaseInsensitiveComparison.Equals(name, "RND") Then
                Return False
            End If

            Return True
        End Function

        ' "isInvocationOrAddressOf" indicates that the name is being bound as the left hand side of an invocation
        ' or the argument of an AddressOf, and the return value variable should not be bound to.
        Private Function BindSimpleName(node As SimpleNameSyntax,
                                        isInvocationOrAddressOf As Boolean,
                                        diagnostics As BindingDiagnosticBag,
                                        Optional skipLocalsAndParameters As Boolean = False) As BoundExpression
            Dim name As String
            Dim typeArguments As TypeArgumentListSyntax

#If DEBUG Then
            If CanBeImplicitVariableDeclaration(node) Then
                CheckSimpleNameBindingOrder(node)
            End If
#End If
            If node.Kind = SyntaxKind.GenericName Then
                Dim genericName = DirectCast(node, GenericNameSyntax)
                typeArguments = genericName.TypeArgumentList
                name = genericName.Identifier.ValueText
            Else
                Debug.Assert(node.Kind = SyntaxKind.IdentifierName)
                typeArguments = Nothing
                name = DirectCast(node, IdentifierNameSyntax).Identifier.ValueText
            End If

            If String.IsNullOrEmpty(name) Then
                ' Empty string must have been a syntax error. 
                ' Just produce a bad expression and get out without producing any new errors.
                Return BadExpression(node, ErrorTypeSymbol.UnknownResultType)
            End If

            Dim options As LookupOptions = LookupOptions.AllMethodsOfAnyArity ' overload resolution filters methods by arity.
            If isInvocationOrAddressOf Then
                options = options Or LookupOptions.MustNotBeReturnValueVariable
            End If

            If skipLocalsAndParameters Then
                options = options Or LookupOptions.MustNotBeLocalOrParameter
            End If

            ' Handle a case of being able to refer to System.Int32 through System.Integer.
            ' Same for other intrinsic types with intrinsic name different from emitted name.
            If node.Kind = SyntaxKind.IdentifierName AndAlso DirectCast(node, IdentifierNameSyntax).Identifier.IsBracketed AndAlso
               MemberLookup.GetTypeForIntrinsicAlias(name) <> SpecialType.None Then
                options = options Or LookupOptions.AllowIntrinsicAliases
            End If

            Dim arity As Integer = If(typeArguments IsNot Nothing, typeArguments.Arguments.Count, 0)
            Dim result As LookupResult = LookupResult.GetInstance()

            Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)
            Me.Lookup(result, name, arity, options, useSiteInfo)
            diagnostics.Add(node, useSiteInfo)

            If Not result.IsGoodOrAmbiguous AndAlso
               Me.ImplicitVariableDeclarationAllowed AndAlso
               Not Me.AllImplicitVariableDeclarationsAreHandled AndAlso
               CanBeImplicitVariableDeclaration(node) Then
                ' Declare an implicit local variable.
                Dim implicitLocal As LocalSymbol = DeclareImplicitLocalVariable(DirectCast(node, IdentifierNameSyntax), diagnostics)
                result.SetFrom(implicitLocal)
            End If

            If Not result.HasSymbol Then
                ' Did not find anything with that name.
                result.Free()

                ' If the name represents an imported XML namespace prefix, report a specific error.
                Dim [namespace] As String = Nothing
                Dim fromImports = False
                If LookupXmlNamespace(name, ignoreXmlNodes:=True, [namespace]:=[namespace], fromImports:=fromImports) Then
                    Return ReportDiagnosticAndProduceBadExpression(diagnostics, node, ERRID.ERR_XmlPrefixNotExpression, name)
                End If

                Dim errorInfo As DiagnosticInfo = Nothing

                If node.Kind = SyntaxKind.IdentifierName Then
                    Select Case KeywordTable.TokenOfString(name)
                        Case SyntaxKind.AwaitKeyword
                            errorInfo = GetAwaitInNonAsyncError()
                    End Select
                End If

                If errorInfo Is Nothing Then
                    'Check for My and use of VB Embed Runtime usage for different diagnostic
                    If IdentifierComparison.Equals(MissingRuntimeMemberDiagnosticHelper.MyVBNamespace, name) AndAlso Me.Compilation.Options.EmbedVbCoreRuntime Then
                        errorInfo = ErrorFactory.ErrorInfo(ERRID.ERR_PlatformDoesntSupport, MissingRuntimeMemberDiagnosticHelper.MyVBNamespace)
                    Else
                        errorInfo = ErrorFactory.ErrorInfo(If(Me.IsInQuery, ERRID.ERR_QueryNameNotDeclared, ERRID.ERR_NameNotDeclared1),
                                                       name)
                    End If
                End If

                Return ReportDiagnosticAndProduceBadExpression(diagnostics, node, errorInfo)
            End If

            Dim boundExpr As BoundExpression = BindSimpleName(result, node, options, typeArguments, diagnostics)
            result.Free()

            Return boundExpr
        End Function

        ''' <summary>
        ''' Second part of BindSimpleName.
        ''' It is a separate function so that it could be called directly 
        ''' when we have already looked up for the name.
        ''' </summary>
        Private Function BindSimpleName(result As LookupResult,
                                        node As VisualBasicSyntaxNode,
                                        options As LookupOptions,
                                        typeArguments As TypeArgumentListSyntax,
                                        diagnostics As BindingDiagnosticBag) As BoundExpression

            ' An implicit Me is inserted if we found something in the immediate containing type.
            ' Note that validation of whether Me can actually be used in this case is deferred until after 
            ' overload resolution determines if we are accessing a static or instance member.
            Dim receiver As BoundExpression = Nothing
            Dim containingType = Me.ContainingType

            If containingType IsNot Nothing Then
                If containingType.IsScriptClass Then
                    Dim memberDeclaringType = result.Symbols(0).ContainingType
                    If memberDeclaringType IsNot Nothing Then
                        receiver = TryBindInteractiveReceiver(node, Me.ContainingMember, containingType, memberDeclaringType)
                    End If
                End If

                If receiver Is Nothing Then
                    Dim symbol = result.Symbols(0)

                    If symbol.IsReducedExtensionMethod() OrElse BindSimpleNameIsMemberOfType(symbol, containingType) Then
                        receiver = CreateMeReference(node, isSynthetic:=True)
                    End If
                End If
            End If

            Dim boundExpr As BoundExpression = BindSymbolAccess(node, result, options, receiver, typeArguments, QualificationKind.Unqualified, diagnostics)
            Return boundExpr
        End Function

        Private Shared Function BindSimpleNameIsMemberOfType(member As Symbol, type As NamedTypeSymbol) As Boolean
            Debug.Assert(type IsNot Nothing)
            Debug.Assert(member IsNot Nothing)

            Select Case member.Kind
                Case SymbolKind.Field, SymbolKind.Method, SymbolKind.Property, SymbolKind.Event
                    Dim container = member.ContainingType
                    If container Is Nothing Then Return False

                    Dim currentType = type

                    Do While currentType IsNot Nothing

                        If container.Equals(currentType) Then
                            Return True
                        End If

                        currentType = currentType.BaseTypeNoUseSiteDiagnostics
                    Loop
            End Select

            Return False
        End Function

        Private Function TryBindInteractiveReceiver(syntax As VisualBasicSyntaxNode, currentMember As Symbol, currentType As NamedTypeSymbol, memberDeclaringType As NamedTypeSymbol) As BoundExpression
            If currentType.TypeKind = TYPEKIND.Submission AndAlso Not currentMember.IsShared Then
                If memberDeclaringType.TypeKind = TYPEKIND.Submission Then
                    Return New BoundPreviousSubmissionReference(syntax, currentType, memberDeclaringType)
                Else
                    ' TODO (tomat): host object binding
                    'Dim hostObjectType As TypeSymbol = Compilation.GetHostObjectTypeSymbol()
                    'If hostObjectType IsNot Nothing AndAlso (hostObjectType = memberDeclaringType OrElse hostObjectType.BaseClassesContain(memberDeclaringType)) Then
                    '    Return New BoundHostObjectMemberReference(syntax, hostObjectType)
                    'End If
                End If
            End If

            Return Nothing
        End Function

        Private Function BindMemberAccess(node As MemberAccessExpressionSyntax, eventContext As Boolean, diagnostics As BindingDiagnosticBag) As BoundExpression
            Dim leftOpt = node.Expression
            Dim boundLeft As BoundExpression = Nothing
            Dim rightName As SimpleNameSyntax = node.Name

            If leftOpt Is Nothing Then
                ' 11.6 Member Access Expressions: "1.  If E is omitted, then the expression from the
                ' immediately containing With statement is substituted for E and the member access
                ' is performed. If there is no containing With statement, a compile-time error occurs."

                ' NOTE: If there are no enclosing anonymous type creation or With statement, the method below will 
                '       report error ERR_BadWithRef; otherwise 'the closest' binder (either AnonymousTypeCreationBinder
                '       or WithStatementBinder (to be created)) should handle binding of such expression

                Dim wholeMemberAccessExpressionBound As Boolean = False

                Dim conditionalAccess As ConditionalAccessExpressionSyntax = node.GetCorrespondingConditionalAccessExpression()

                If conditionalAccess IsNot Nothing Then
                    boundLeft = GetConditionalAccessReceiver(conditionalAccess)
                Else
                    boundLeft = Me.TryBindOmittedLeftForMemberAccess(node, diagnostics, Me, wholeMemberAccessExpressionBound)
                End If

                If boundLeft Is Nothing Then
                    Debug.Assert(Not wholeMemberAccessExpressionBound)
                    Return ReportDiagnosticAndProduceBadExpression(diagnostics, node, ERRID.ERR_BadWithRef)
                End If

                If wholeMemberAccessExpressionBound Then
                    ' In case TryBindOmittedLeftForMemberAccess bound the whole member 
                    ' access expression syntax node just return the result
                    Return boundLeft
                End If

            Else
                boundLeft = BindLeftOfPotentialColorColorMemberAccess(node, leftOpt, diagnostics)
            End If

            Return Me.BindMemberAccess(node, boundLeft, rightName, eventContext, diagnostics)
        End Function

        Private Function BindLeftOfPotentialColorColorMemberAccess(parentNode As MemberAccessExpressionSyntax, leftOpt As ExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            ' handle for Color Color case:  
            '
            ' =======  11.6.1 Identical Type and Member Names
            ' It is not uncommon to name members using the same name as their type. In that situation, however, 
            ' inconvenient name hiding can occur:
            '
            '        Enum Color
            '            Red
            '            Green
            '            Yellow
            '        End Enum
            '
            '        Class Test
            '            ReadOnly Property Color() As Color
            '                Get
            '                    Return Color.Red
            '                End Get
            '            End Property
            '
            '            Shared Function DefaultColor() As Color
            '                Return Color.Green    ' Binds to the instance property!
            '            End Function
            '        End Class
            '
            '  In the previous example, the simple name Color in DefaultColor binds to the instance property 
            '  instead of the type. Because an instance member cannot be referenced in a shared member, 
            '  this would normally be an error.
            '
            '  However, a special rule allows access to the type in this case. If the base expression 
            '  of a member access expression is a simple name and binds to a constant, field, property, 
            '  local variable or parameter whose type has the same name, then the base expression can refer 
            '  either to the member or the type. This can never result in ambiguity because the members 
            '  that can be accessed off of either one are the same.
            '
            '  In the case that such a base expression binds to an instance member but the binding occurs
            '  within a context in which "Me" is not accessible, the expression instead binds to the
            '  type (if applicable).
            '
            '  If the base expression cannot be successfully disambiguated by the context in which it
            '  occurs, it binds to the member. This can occur in particular in late-bound calls or
            '  error conditions.

            If leftOpt.Kind = SyntaxKind.IdentifierName Then
                Dim node = DirectCast(leftOpt, SimpleNameSyntax)
                Dim leftDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics)
                Dim boundLeft = Me.BindSimpleName(node, False, leftDiagnostics)

                Dim boundValue = boundLeft
                Dim propertyDiagnostics As BindingDiagnosticBag = Nothing
                If boundLeft.Kind = BoundKind.PropertyGroup Then
                    propertyDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics)
                    boundValue = Me.AdjustReceiverValue(boundLeft, node, propertyDiagnostics)
                End If

                Dim leftSymbol = boundValue.ExpressionSymbol
                If leftSymbol IsNot Nothing Then
                    Dim leftType As TypeSymbol
                    Dim isInstanceMember As Boolean

                    Select Case leftSymbol.Kind
                        Case SymbolKind.Field, SymbolKind.Property
                            Debug.Assert(boundValue.Type IsNot Nothing)
                            leftType = boundValue.Type
                            isInstanceMember = Not leftSymbol.IsShared

                        Case SymbolKind.Local, SymbolKind.Parameter, SymbolKind.RangeVariable
                            Debug.Assert(boundValue.Type IsNot Nothing)
                            leftType = boundValue.Type
                            isInstanceMember = False

                        Case Else
                            leftType = Nothing
                            isInstanceMember = False
                    End Select

                    If leftType IsNot Nothing Then
                        Dim leftName = node.Identifier.ValueText
                        If CaseInsensitiveComparison.Equals(leftType.Name, leftName) AndAlso leftType.TypeKind <> TYPEKIND.TypeParameter Then
                            Dim typeDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics)
                            Dim boundType = Me.BindNamespaceOrTypeExpression(node, typeDiagnostics)
                            If TypeSymbol.Equals(boundType.Type, leftType, TypeCompareKind.ConsiderEverything) Then
                                Dim err As ERRID = Nothing
                                If isInstanceMember AndAlso (Not CanAccessMe(implicitReference:=True, errorId:=err) OrElse Not BindSimpleNameIsMemberOfType(leftSymbol, ContainingType)) Then
                                    diagnostics.AddRange(typeDiagnostics)
                                    leftDiagnostics.Free()

                                    Return boundType
                                End If

                                Dim valueDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics)
                                valueDiagnostics.AddRangeAndFree(leftDiagnostics)
                                If propertyDiagnostics IsNot Nothing Then
                                    valueDiagnostics.AddRangeAndFree(propertyDiagnostics)
                                End If

                                Return New BoundTypeOrValueExpression(leftOpt, New BoundTypeOrValueData(boundValue, valueDiagnostics.ToReadOnlyAndFree(), boundType, typeDiagnostics.ToReadOnlyAndFree()), leftType)
                            End If

                            typeDiagnostics.Free()
                        End If
                    End If
                End If

                If propertyDiagnostics IsNot Nothing Then
                    propertyDiagnostics.Free()
                End If

                diagnostics.AddRangeAndFree(leftDiagnostics)
                Return boundLeft
            End If

            ' Not a Color Color case; just bind the LHS as an expression.
            If leftOpt.Kind = SyntaxKind.SimpleMemberAccessExpression Then
                Return BindMemberAccess(DirectCast(leftOpt, MemberAccessExpressionSyntax), eventContext:=False, diagnostics:=diagnostics)
            Else
                Return Me.BindExpression(leftOpt, diagnostics)
            End If
        End Function

        ''' <summary> 
        ''' Method binds member access in case when we got hold 
        ''' of a bound node representing the left expression 
        ''' </summary>
        ''' <remarks> 
        ''' The method is protected, so that it can be called from other 
        ''' binders overriding TryBindMemberAccessWithLeftOmitted
        ''' </remarks>
        Protected Function BindMemberAccess(node As VisualBasicSyntaxNode, left As BoundExpression, right As SimpleNameSyntax, eventContext As Boolean, diagnostics As BindingDiagnosticBag) As BoundExpression
            Debug.Assert(node IsNot Nothing)
            Debug.Assert(left IsNot Nothing)
            Debug.Assert(right IsNot Nothing)

            Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)

            ' Check if 'left' is of type which is a class or struct and 'name' is "New"
            Dim leftTypeSymbol As TypeSymbol = left.Type
            If leftTypeSymbol IsNot Nothing AndAlso (right.Kind = SyntaxKind.IdentifierName OrElse right.Kind = SyntaxKind.GenericName) Then

                ' Get the name syntax token
                Dim identifier = If(right.Kind = SyntaxKind.IdentifierName,
                                    DirectCast(right, IdentifierNameSyntax).Identifier,
                                    DirectCast(right, GenericNameSyntax).Identifier)

                If Not identifier.IsBracketed AndAlso
                        CaseInsensitiveComparison.Equals(identifier.ValueText, SyntaxFacts.GetText(SyntaxKind.NewKeyword)) Then

                    If leftTypeSymbol.IsArrayType() Then
                        ' No instance constructors found. Can't call constructor on an array type.
                        If (left.HasErrors) Then
                            Return BadExpression(node, left, ErrorTypeSymbol.UnknownResultType)
                        End If

                        Return ReportDiagnosticAndProduceBadExpression(
                                        diagnostics, node, ErrorFactory.ErrorInfo(ERRID.ERR_ConstructorNotFound1, leftTypeSymbol), left)
                    End If

                    Dim leftTypeKind As TYPEKIND = leftTypeSymbol.TypeKind

                    If leftTypeKind = TYPEKIND.Class OrElse leftTypeKind = TYPEKIND.Structure OrElse leftTypeKind = TYPEKIND.Module Then

                        ' Bind to method group representing available instance constructors
                        Dim namedLeftTypeSymbol = DirectCast(leftTypeSymbol, NamedTypeSymbol)

                        Dim accessibleConstructors = GetAccessibleConstructors(namedLeftTypeSymbol, useSiteInfo)

                        diagnostics.Add(node, useSiteInfo)
                        useSiteInfo = New CompoundUseSiteInfo(Of AssemblySymbol)(useSiteInfo)

                        If accessibleConstructors.IsEmpty Then

                            ' No instance constructors found
                            If (left.HasErrors) Then
                                Return BadExpression(node, left, ErrorTypeSymbol.UnknownResultType)
                            End If

                            Return ReportDiagnosticAndProduceBadExpression(
                                            diagnostics, node, ErrorFactory.ErrorInfo(ERRID.ERR_ConstructorNotFound1, namedLeftTypeSymbol), left)
                        Else

                            Dim hasErrors As Boolean = left.HasErrors
                            If Not hasErrors AndAlso right.Kind = SyntaxKind.GenericName Then
                                ' Report error BC30282
                                ReportDiagnostic(diagnostics, node, ERRID.ERR_InvalidConstructorCall)
                                hasErrors = True
                            End If

                            ' Create a method group consisting of all instance constructors
                            Return New BoundMethodGroup(node, Nothing, accessibleConstructors, LookupResultKind.Good, left,
                                                        If(left.Kind = BoundKind.TypeExpression, QualificationKind.QualifiedViaTypeName, QualificationKind.QualifiedViaValue),
                                                        hasErrors)
                        End If
                    End If
                End If
            End If

            Dim type As TypeSymbol

            Dim rightName As String
            Dim typeArguments As TypeArgumentListSyntax

            If right.Kind = SyntaxKind.GenericName Then
                Dim genericName = DirectCast(right, GenericNameSyntax)
                typeArguments = genericName.TypeArgumentList
                rightName = genericName.Identifier.ValueText
            Else
                Debug.Assert(right.Kind = SyntaxKind.IdentifierName)
                typeArguments = Nothing
                rightName = DirectCast(right, IdentifierNameSyntax).Identifier.ValueText
            End If

            Dim rightArity As Integer = If(typeArguments IsNot Nothing, typeArguments.Arguments.Count, 0)
            Dim lookupResult As LookupResult = lookupResult.GetInstance()
            Dim options As LookupOptions = LookupOptions.AllMethodsOfAnyArity

            Try
                If left.Kind = BoundKind.NamespaceExpression Then

                    If String.IsNullOrEmpty(rightName) Then
                        ' Must have been a syntax error.
                        Return BadExpression(node, left, ErrorTypeSymbol.UnknownResultType)
                    End If

                    Dim ns As NamespaceSymbol = DirectCast(left, BoundNamespaceExpression).NamespaceSymbol

                    ' Handle a case of being able to refer to System.Int32 through System.Integer.
                    ' Same for other intrinsic types with intrinsic name different from emitted name.
                    If right.Kind = SyntaxKind.IdentifierName AndAlso node.Kind = SyntaxKind.SimpleMemberAccessExpression Then
                        options = options Or LookupOptions.AllowIntrinsicAliases
                    End If

                    MemberLookup.Lookup(lookupResult, ns, rightName, rightArity, options, Me, useSiteInfo) ' overload resolution filters methods by arity.

                    If lookupResult.HasSymbol Then
                        Return BindSymbolAccess(node, lookupResult, options, left, typeArguments, QualificationKind.QualifiedViaNamespace, diagnostics)
                    Else
                        Return ReportDiagnosticAndProduceBadExpression(diagnostics, node, ErrorFactory.ErrorInfo(ERRID.ERR_NameNotMember2, rightName, ns), left)
                    End If

                ElseIf left.Kind = BoundKind.TypeExpression Then
                    type = DirectCast(left, BoundTypeExpression).Type

                    If type.TypeKind = TYPEKIND.TypeParameter Then
                        Return ReportDiagnosticAndProduceBadExpression(diagnostics, node, ErrorFactory.ErrorInfo(ERRID.ERR_TypeParamQualifierDisallowed), left)
                    Else
                        If String.IsNullOrEmpty(rightName) Then
                            ' Must have been a syntax error.
                            Return BadExpression(node, left, ErrorTypeSymbol.UnknownResultType)
                        End If

                        LookupMember(lookupResult, type, rightName, rightArity, options, useSiteInfo) ' overload resolution filters methods by arity.

                        If lookupResult.HasSymbol Then
                            Return BindSymbolAccess(node, lookupResult, options, left, typeArguments, QualificationKind.QualifiedViaTypeName, diagnostics)
                        Else
                            Return ReportDiagnosticAndProduceBadExpression(diagnostics, node, ErrorFactory.ErrorInfo(ERRID.ERR_NameNotMember2, rightName, type), left)
                        End If
                    End If

                Else
                    left = AdjustReceiverValue(left, node, diagnostics)

                    type = left.Type

                    If type Is Nothing OrElse type.IsErrorType() Then
                        Return BadExpression(node, left, ErrorTypeSymbol.UnknownResultType)
                    End If

                    If String.IsNullOrEmpty(rightName) Then
                        ' Must have been a syntax error.
                        Return BadExpression(node, left, ErrorTypeSymbol.UnknownResultType)
                    End If

                    Dim effectiveOptions = If(left.Kind <> BoundKind.MyBaseReference, options,
                                              options Or LookupOptions.UseBaseReferenceAccessibility)
                    If eventContext Then
                        effectiveOptions = effectiveOptions Or LookupOptions.EventsOnly
                    End If

                    LookupMember(lookupResult, type, rightName, rightArity, effectiveOptions, useSiteInfo) ' overload resolution filters methods by arity.

                    If lookupResult.HasSymbol Then
                        Return BindSymbolAccess(node, lookupResult, effectiveOptions, left, typeArguments, QualificationKind.QualifiedViaValue, diagnostics)

                    ElseIf (type.IsObjectType AndAlso Not left.IsMyBaseReference) OrElse type.IsExtensibleInterfaceNoUseSiteDiagnostics Then
                        Return BindLateBoundMemberAccess(node, rightName, typeArguments, left, type, diagnostics)

                    ElseIf left.HasErrors Then
                        Return BadExpression(node, left, ErrorTypeSymbol.UnknownResultType)

                    Else
                        If type.IsInterfaceType() Then
                            ' In case IsExtensibleInterfaceNoUseSiteDiagnostics above failed because there were bad inherited interfaces.
                            type.AllInterfacesWithDefinitionUseSiteDiagnostics(useSiteInfo)
                        End If

                        Return ReportDiagnosticAndProduceBadExpression(diagnostics, node, ErrorFactory.ErrorInfo(ERRID.ERR_NameNotMember2, rightName, type), left)
                    End If
                End If
            Finally
                diagnostics.Add(node, useSiteInfo)
                lookupResult.Free()
            End Try
        End Function

        ''' <summary> 
        ''' Returns a bound node for left part of member access node with omitted left syntax. 
        ''' In particular it handles member access inside With statement.
        ''' 
        ''' By default the method delegates the work to it's containing binder or returns Nothing.
        ''' </summary>
        ''' <param name="accessingBinder">
        ''' Specifies the binder which requests an access to the bound node for omitted left.
        ''' </param>
        ''' <param name="wholeMemberAccessExpressionBound">
        ''' NOTE: in some cases, like for binding inside anonymous object creation expression, this 
        ''' method returns bound node for the whole expression rather than only for omitted left part. 
        ''' </param>
        Protected Friend Overridable Function TryBindOmittedLeftForMemberAccess(node As MemberAccessExpressionSyntax,
                                                                                diagnostics As BindingDiagnosticBag,
                                                                                accessingBinder As Binder,
                                                                                <Out> ByRef wholeMemberAccessExpressionBound As Boolean) As BoundExpression
            Debug.Assert(Me.ContainingBinder IsNot Nothing)
            Return Me.ContainingBinder.TryBindOmittedLeftForMemberAccess(node, diagnostics, accessingBinder, wholeMemberAccessExpressionBound)
        End Function

        Protected Friend Overridable Function TryBindOmittedLeftForXmlMemberAccess(node As XmlMemberAccessExpressionSyntax,
                                                                                   diagnostics As BindingDiagnosticBag,
                                                                                   accessingBinder As Binder) As BoundExpression
            Debug.Assert(Me.ContainingBinder IsNot Nothing)
            Return Me.ContainingBinder.TryBindOmittedLeftForXmlMemberAccess(node, diagnostics, accessingBinder)
        End Function

        Private Function IsBindingImplicitlyTypedLocal(symbol As LocalSymbol) As Boolean
            For Each s In Me.ImplicitlyTypedLocalsBeingBound
                If s = symbol Then
                    Return True
                End If
            Next
            Return False
        End Function

        ''' <summary>
        ''' Given a localSymbol and a syntaxNode where the symbol is used, safely return the symbol's type.
        ''' </summary>
        ''' <param name="localSymbol">The local symbol</param>
        ''' <param name="node">The syntax node that references the symbol</param>
        ''' <param name="diagnostics">diagnostic bag if errors are to be reported</param>
        ''' <returns>Returns the symbol's type or an ErrorTypeSymbol if the local is referenced before its definition or if the symbol is still being bound.</returns>
        ''' <remarks>This method safely returns a local symbol's type by checking for circular references or references before declaration.</remarks>
        Private Function GetLocalSymbolType(localSymbol As LocalSymbol, node As VisualBasicSyntaxNode, Optional diagnostics As BindingDiagnosticBag = Nothing) As TypeSymbol
            Dim localType As TypeSymbol = Nothing
            ' Check if local symbol is used before it's definition.
            ' Do span comparison first in order to optimize performance for non-error cases. 
            If node IsNot Nothing AndAlso
               node.SpanStart < localSymbol.IdentifierToken.SpanStart Then

                Dim declarationLocation As Location = localSymbol.IdentifierLocation
                Dim referenceLocation As Location = node.GetLocation()

                If Not localSymbol.IsImplicitlyDeclared AndAlso
                   declarationLocation.IsInSource AndAlso
                   referenceLocation IsNot Nothing AndAlso referenceLocation.IsInSource AndAlso
                   declarationLocation.SourceTree Is referenceLocation.SourceTree Then

                    localType = localSymbol.UseBeforeDeclarationResultType

                    If diagnostics IsNot Nothing Then
                        ReportDiagnostic(diagnostics, node, ERRID.ERR_UseOfLocalBeforeDeclaration1, localSymbol)
                    End If
                End If

            ElseIf IsBindingImplicitlyTypedLocal(localSymbol) Then
                ' We are currently in the process of binding this symbol.

                ' if constant knows its type, there is no circularity
                ' Example:
                '   Const x as Color = x.Red
                If localSymbol.IsConst AndAlso localSymbol.ConstHasType Then
                    Return localSymbol.Type
                End If

                ' NOTE: OptionInfer does not need to be checked before reporting the error
                '       locals only get to ImplicitlyTypedLocalsBeingBound if we actually infer
                '       their type, either because Option Infer is On or for other reason,
                '       we use UnknownResultType for such locals.
                If diagnostics IsNot Nothing Then
                    If localSymbol.IsConst Then
                        ReportDiagnostic(diagnostics, node, ERRID.ERR_CircularEvaluation1, localSymbol)
                    Else
                        ReportDiagnostic(diagnostics, node, ERRID.ERR_CircularInference1, localSymbol)
                    End If
                End If

                localType = ErrorTypeSymbol.UnknownResultType
            End If

            If localType Is Nothing Then
                ' It is safe to get the type from the symbol.
                localType = localSymbol.Type
            End If

            Return localType
        End Function

        ' Bind access to a symbol, either qualified (LHS.Symbol) or unqualified (Symbol). This kind of qualification is indicated by qualKind.
        ' receiver is set to a value expression indicating the receiver that the symbol is being accessed off of.
        ' lookupResult must refer to one or more symbols. If lookupResult has a diagnostic associated with it, that diagnostic is reported.
        Private Function BindSymbolAccess(node As VisualBasicSyntaxNode,
                                          lookupResult As LookupResult,
                                          lookupOptionsUsed As LookupOptions,
                                          receiver As BoundExpression,
                                          typeArgumentsOpt As TypeArgumentListSyntax,
                                          qualKind As QualificationKind,
                                          diagnostics As BindingDiagnosticBag) As BoundExpression
            Debug.Assert(lookupResult.HasSymbol)

            Dim hasError As Boolean = False ' Is there an ERROR (not a warning).

            If receiver IsNot Nothing Then
                hasError = receiver.HasErrors   ' don't report subsequent errors if LHS was already an error.

                ' If receiver is a namespace group, let's check if we can collapse it to a single or more narrow namespace
                receiver = AdjustReceiverNamespace(lookupResult, receiver)
            End If

            Dim reportedLookupError As Boolean = False
            Dim resultKind As LookupResultKind = lookupResult.Kind

            If lookupResult.HasDiagnostic AndAlso
               ((lookupResult.Symbols(0).Kind <> SymbolKind.Method AndAlso lookupResult.Symbols(0).Kind <> SymbolKind.Property) OrElse
                    resultKind <> LookupResultKind.Inaccessible) Then
                Debug.Assert(resultKind <> LookupResultKind.Good)

                ' Report the diagnostic with the symbol.
                Dim di As DiagnosticInfo = lookupResult.Diagnostic

                If Not hasError Then
                    If typeArgumentsOpt IsNot Nothing AndAlso
                        (lookupResult.Kind = LookupResultKind.WrongArity OrElse lookupResult.Kind = LookupResultKind.WrongArityAndStopLookup) Then
                        ' Arity errors are reported on the type arguments only.
                        ReportDiagnostic(diagnostics, typeArgumentsOpt, di)
                    Else
                        ReportDiagnostic(diagnostics, node, di)
                    End If

                    If di.Severity = DiagnosticSeverity.Error Then
                        hasError = True
                        reportedLookupError = True
                    End If
                End If

                ' For non-overloadable symbols (everything but property/method)
                ' Create a BoundBadExpression to encapsulate all the 
                ' symbols and the result kind. 
                ' The type of the expression is the common type of the symbols, so further Intellisense
                ' works well if all the symbols are of common type.
                ' For property/method, we create a BoundMethodGroup/PropertyGroup so that we continue to do overload 
                ' resolution.

                Dim symbols As ImmutableArray(Of Symbol)
                If TypeOf di Is AmbiguousSymbolDiagnostic Then
                    ' Lookup had an ambiguity between Imports or Modules.  
                    Debug.Assert(lookupResult.Kind = LookupResultKind.Ambiguous)
                    symbols = DirectCast(di, AmbiguousSymbolDiagnostic).AmbiguousSymbols
                Else
                    symbols = lookupResult.Symbols.ToImmutable()
                End If

                Return New BoundBadExpression(node,
                                              lookupResult.Kind,
                                              symbols,
                                              If(receiver IsNot Nothing, ImmutableArray.Create(receiver), ImmutableArray(Of BoundExpression).Empty),
                                              GetCommonExpressionTypeForErrorRecovery(node, symbols, ConstantFieldsInProgress), hasErrors:=True)
            End If

            Select Case lookupResult.Symbols(0).Kind ' all symbols in a lookupResult must be of the same kind.
                Case SymbolKind.Method
                    'TODO: Deal with errors reported by BindTypeArguments. Should we adjust hasError?

                    Return CreateBoundMethodGroup(
                                node,
                                lookupResult,
                                lookupOptionsUsed,
                                diagnostics.AccumulatesDependencies,
                                receiver,
                                BindTypeArguments(typeArgumentsOpt, diagnostics),
                                qualKind,
                                hasError)

                Case SymbolKind.Property
                    ' UNDONE: produce error if type arguments were present.

                    Debug.Assert(lookupResult.Kind = LookupResultKind.Good OrElse lookupResult.Kind = LookupResultKind.Inaccessible)
                    Return New BoundPropertyGroup(
                        node,
                        lookupResult.Symbols.ToDowncastedImmutable(Of PropertySymbol),
                        lookupResult.Kind,
                        receiver,
                        qualKind,
                        hasErrors:=hasError)

                Case SymbolKind.Event
                    Dim eventSymbol = DirectCast(lookupResult.SingleSymbol, EventSymbol)
                    If eventSymbol.IsShared And qualKind = QualificationKind.Unqualified Then
                        receiver = Nothing
                    End If

                    If Not reportedLookupError Then
                        ReportUseSite(diagnostics, node, eventSymbol)
                    End If

                    If Not hasError Then
                        If receiver IsNot Nothing AndAlso receiver.Kind = BoundKind.TypeOrValueExpression Then
                            receiver = AdjustReceiverTypeOrValue(receiver, node, isShared:=eventSymbol.IsShared, diagnostics:=diagnostics, qualKind:=qualKind)
                        End If

                        If Not IsNameOfArgument(node) Then
                            hasError = CheckSharedSymbolAccess(node, eventSymbol.IsShared, receiver, qualKind, diagnostics)
                        End If
                    End If

                    ReportDiagnosticsIfObsoleteOrNotSupported(diagnostics, eventSymbol, node)

                    If receiver IsNot Nothing AndAlso receiver.IsPropertyOrXmlPropertyAccess() Then
                        receiver = MakeRValue(receiver, diagnostics)
                    End If

                    Return New BoundEventAccess(
                        node,
                        receiver,
                        eventSymbol,
                        eventSymbol.Type,
                        hasErrors:=hasError)

                Case SymbolKind.Field
                    Dim fieldSymbol As FieldSymbol = DirectCast(lookupResult.SingleSymbol, FieldSymbol)

                    If fieldSymbol.IsShared And qualKind = QualificationKind.Unqualified Then
                        receiver = Nothing
                    End If

                    ' TODO: Check if this is a constant field with missing or bad value and report an error.

                    If Not hasError Then
                        If receiver IsNot Nothing AndAlso receiver.Kind = BoundKind.TypeOrValueExpression Then
                            receiver = AdjustReceiverTypeOrValue(receiver, node, isShared:=fieldSymbol.IsShared, diagnostics:=diagnostics, qualKind:=qualKind)
                        End If

                        hasError = CheckSharedSymbolAccess(node, fieldSymbol.IsShared, receiver, qualKind, diagnostics)
                    End If

                    If Not reportedLookupError Then
                        If Not ReportUseSite(diagnostics, node, fieldSymbol) Then
                            CheckMemberTypeAccessibility(diagnostics, node, fieldSymbol)
                        End If
                    End If

                    ReportDiagnosticsIfObsoleteOrNotSupported(diagnostics, fieldSymbol, node)

                    ' const fields may need to determine the type because it's inferred
                    ' This is why using .Type was replaced by .GetInferredType to detect cycles.
                    Dim fieldAccessType = fieldSymbol.GetInferredType(ConstantFieldsInProgress)

                    Dim asMemberAccess = TryCast(node, MemberAccessExpressionSyntax)
                    If asMemberAccess IsNot Nothing AndAlso Not fieldAccessType.IsErrorType() Then
                        VerifyTypeCharacterConsistency(asMemberAccess.Name, fieldAccessType.GetEnumUnderlyingTypeOrSelf, diagnostics)
                    End If

                    If receiver IsNot Nothing AndAlso receiver.IsPropertyOrXmlPropertyAccess() Then
                        receiver = MakeRValue(receiver, diagnostics)
                    End If

                    Return New BoundFieldAccess(node,
                                                receiver,
                                                fieldSymbol,
                                                isLValue:=IsLValueFieldAccess(fieldSymbol, receiver),
                                                suppressVirtualCalls:=False,
                                                constantsInProgressOpt:=Me.ConstantFieldsInProgress,
                                                type:=fieldAccessType,
                                                hasErrors:=hasError OrElse fieldAccessType.IsErrorType)

                Case SymbolKind.Local
                    Dim localSymbol = DirectCast(lookupResult.SingleSymbol, LocalSymbol)

                    If localSymbol.IsFunctionValue AndAlso Not IsNameOfArgument(node) Then
                        Dim method = DirectCast(localSymbol.ContainingSymbol, MethodSymbol)

                        If method.IsAsync OrElse method.IsIterator Then
                            ReportDiagnostic(diagnostics, node, ERRID.ERR_BadResumableAccessReturnVariable)
                            Return BadExpression(node, ErrorTypeSymbol.UnknownResultType)
                        End If
                    End If

                    Dim localAccessType As TypeSymbol = GetLocalSymbolType(localSymbol, node, diagnostics)

                    Dim asSimpleName = TryCast(node, SimpleNameSyntax)
                    If asSimpleName IsNot Nothing AndAlso Not localAccessType.IsErrorType() Then
                        VerifyTypeCharacterConsistency(asSimpleName, localAccessType.GetEnumUnderlyingTypeOrSelf, diagnostics)
                    End If

                    If localSymbol.IsFor Then
                        ' lifting iteration variable produces a warning
                        Dim localSymbolContainingSymbol As Symbol = localSymbol.ContainingSymbol

                        If ContainingMember IsNot localSymbolContainingSymbol Then
                            ' Need to go up the chain of containers and see if the last lambda we see
                            ' is a QueryLambda, before we reach local's container. 
                            If IsTopMostEnclosingLambdaAQueryLambda(ContainingMember, localSymbolContainingSymbol) Then
                                ReportDiagnostic(diagnostics, node, ERRID.WRN_LiftControlVariableQuery, localSymbol.Name)
                            Else
                                ReportDiagnostic(diagnostics, node, ERRID.WRN_LiftControlVariableLambda, localSymbol.Name)
                            End If
                        End If
                    End If

                    ' Debug.Assert(localSymbol.GetUseSiteInfo().DiagnosticInfo Is Nothing) ' Not true in the debugger.
                    Return New BoundLocal(node, localSymbol, localAccessType, hasErrors:=hasError)

                Case SymbolKind.RangeVariable
                    Dim rangeVariable = DirectCast(lookupResult.SingleSymbol, RangeVariableSymbol)
                    Debug.Assert(rangeVariable.GetUseSiteInfo().IsEmpty)
                    Return New BoundRangeVariable(node, rangeVariable, rangeVariable.Type, hasErrors:=hasError)

                Case SymbolKind.Parameter
                    Dim parameterSymbol = DirectCast(lookupResult.SingleSymbol, ParameterSymbol)

                    Dim parameterType = parameterSymbol.Type
                    Dim asSimpleName = TryCast(node, SimpleNameSyntax)
                    If asSimpleName IsNot Nothing AndAlso Not parameterType.IsErrorType() Then
                        VerifyTypeCharacterConsistency(asSimpleName, parameterType.GetEnumUnderlyingTypeOrSelf, diagnostics)
                    End If

                    Debug.Assert(parameterSymbol.GetUseSiteInfo().IsEmpty)
                    Return New BoundParameter(node, parameterSymbol, parameterType, hasErrors:=hasError)

                Case SymbolKind.NamedType, SymbolKind.ErrorType
                    ' Note: arity already checked by lookup process.
                    ' Bind the type arguments.
                    Dim typeArguments As BoundTypeArguments = Nothing
                    If typeArgumentsOpt IsNot Nothing Then
                        ' Bind the type arguments and report errors in the current context. 
                        typeArguments = BindTypeArguments(typeArgumentsOpt, diagnostics)
                    End If

                    ' If I identifies a type, then the result is that type constructed with the given type arguments.
                    ' Construct the type if it is generic! See ConstructAndValidateConstraints().
                    Dim typeSymbol = TryCast(lookupResult.SingleSymbol, NamedTypeSymbol)
                    If typeSymbol IsNot Nothing AndAlso typeArguments IsNot Nothing Then
                        ' Construct the type and validate constraints.
                        Dim constructedType = ConstructAndValidateConstraints(
                            typeSymbol, typeArguments.Arguments, node, typeArgumentsOpt.Arguments, diagnostics)

                        ' Put the constructed type in. Note that this preserves any error associated with the lookupResult.
                        lookupResult.ReplaceSymbol(constructedType)
                    End If

                    ReportDiagnosticsIfObsoleteOrNotSupported(diagnostics, typeSymbol, node)

                    If Not hasError Then
                        receiver = AdjustReceiverTypeOrValue(receiver, node, isShared:=True, diagnostics:=diagnostics, qualKind:=qualKind)
                        hasError = CheckSharedSymbolAccess(node, True, receiver, qualKind, diagnostics)
                    End If

                    If Not reportedLookupError Then
                        ReportUseSite(diagnostics, node, If(typeSymbol, lookupResult.SingleSymbol))
                    End If

                    Dim type As TypeSymbol = DirectCast(lookupResult.SingleSymbol, TypeSymbol)

                    Dim asSimpleName = TryCast(node, SimpleNameSyntax)
                    If asSimpleName IsNot Nothing AndAlso Not type.IsErrorType() Then
                        VerifyTypeCharacterConsistency(asSimpleName, type.GetEnumUnderlyingTypeOrSelf, diagnostics)
                    End If

                    Return New BoundTypeExpression(node, receiver, Nothing, type, hasErrors:=hasError)

                Case SymbolKind.TypeParameter
                    ' Note: arity already checked by lookup process.

                    Debug.Assert(lookupResult.SingleSymbol.GetUseSiteInfo().IsEmpty)
                    Return New BoundTypeExpression(node, DirectCast(lookupResult.SingleSymbol, TypeSymbol), hasErrors:=hasError)

                Case SymbolKind.Namespace
                    ' Note: arity already checked by lookup process.

                    Debug.Assert(lookupResult.SingleSymbol.GetUseSiteInfo().IsEmpty)
                    Return New BoundNamespaceExpression(node, receiver, DirectCast(lookupResult.SingleSymbol, NamespaceSymbol), hasErrors:=hasError)

                Case SymbolKind.Alias
                    Dim [alias] = DirectCast(lookupResult.SingleSymbol, AliasSymbol)

                    Debug.Assert([alias].GetUseSiteInfo().IsEmpty)
                    Dim symbol = [alias].Target

                    Select Case symbol.Kind
                        Case SymbolKind.NamedType, SymbolKind.ErrorType
                            If Not reportedLookupError Then
                                ReportUseSite(diagnostics, node, symbol)
                            End If

                            Return New BoundTypeExpression(node, Nothing, [alias], DirectCast(symbol, TypeSymbol), hasErrors:=hasError)
                        Case SymbolKind.Namespace
                            Debug.Assert(symbol.GetUseSiteInfo().IsEmpty)
                            Return New BoundNamespaceExpression(node, Nothing, [alias], DirectCast(symbol, NamespaceSymbol), hasErrors:=hasError)
                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(symbol.Kind)
                    End Select

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(lookupResult.Symbols(0).Kind)
            End Select
        End Function

        Private Function AdjustReceiverNamespace(lookupResult As LookupResult, receiver As BoundExpression) As BoundExpression
            If receiver.Kind = BoundKind.NamespaceExpression Then
                Dim namespaceReceiver = DirectCast(receiver, BoundNamespaceExpression)
                If namespaceReceiver.NamespaceSymbol.NamespaceKind = NamespaceKindNamespaceGroup Then
                    Dim symbols As ArrayBuilder(Of Symbol) = lookupResult.Symbols

                    If lookupResult.HasDiagnostic Then
                        Dim di As DiagnosticInfo = lookupResult.Diagnostic
                        If TypeOf di Is AmbiguousSymbolDiagnostic Then
                            ' Lookup had an ambiguity 
                            Debug.Assert(lookupResult.Kind = LookupResultKind.Ambiguous)
                            Dim ambiguous As ImmutableArray(Of Symbol) = DirectCast(di, AmbiguousSymbolDiagnostic).AmbiguousSymbols
                            symbols = ArrayBuilder(Of Symbol).GetInstance()
                            symbols.AddRange(ambiguous)
                        End If
                    End If

                    receiver = AdjustReceiverNamespace(namespaceReceiver, symbols)

                    If symbols IsNot lookupResult.Symbols Then
                        symbols.Free()
                    End If
                End If
            End If

            Return receiver
        End Function

        Private Function AdjustReceiverNamespace(namespaceReceiver As BoundNamespaceExpression, symbols As ArrayBuilder(Of Symbol)) As BoundNamespaceExpression
            If symbols.Count > 0 Then
                Dim namespaces = New SmallDictionary(Of NamespaceSymbol, Boolean)()

                For Each candidate In symbols
                    If Not AddReceiverNamespaces(namespaces, candidate, Me.Compilation) Then
                        namespaces = Nothing
                        Exit For
                    End If
                Next

                If namespaces IsNot Nothing AndAlso namespaces.Count < namespaceReceiver.NamespaceSymbol.ConstituentNamespaces.Length Then
                    Return AdjustReceiverNamespace(namespaceReceiver, DirectCast(namespaceReceiver.NamespaceSymbol, MergedNamespaceSymbol).Shrink(namespaces.Keys))
                End If
            End If

            Return namespaceReceiver
        End Function

        Friend Shared Function AddReceiverNamespaces(namespaces As SmallDictionary(Of NamespaceSymbol, Boolean), candidate As Symbol, compilation As VisualBasicCompilation) As Boolean
            If candidate.Kind = SymbolKind.Namespace AndAlso
               DirectCast(candidate, NamespaceSymbol).NamespaceKind = NamespaceKindNamespaceGroup Then
                For Each constituent In DirectCast(candidate, NamespaceSymbol).ConstituentNamespaces
                    If Not AddContainingNamespaces(namespaces, constituent, compilation) Then
                        Return False
                    End If
                Next

                Return True
            Else
                Return AddContainingNamespaces(namespaces, candidate, compilation)
            End If
        End Function

        Private Shared Function AddContainingNamespaces(namespaces As SmallDictionary(Of NamespaceSymbol, Boolean), candidate As Symbol, compilation As VisualBasicCompilation) As Boolean
            If candidate Is Nothing OrElse candidate.Kind = SymbolKind.ErrorType Then
                Return False
            End If

            Dim containingNamespace = candidate.ContainingNamespace
            If containingNamespace IsNot Nothing Then
                namespaces(compilation.GetCompilationNamespace(containingNamespace)) = False
            Else
                Debug.Assert(containingNamespace IsNot Nothing)
                ' Should not get here, I believe.
                Return False
            End If

            Return True
        End Function

        Private Function AdjustReceiverNamespace(namespaceReceiver As BoundNamespaceExpression, adjustedNamespace As NamespaceSymbol) As BoundNamespaceExpression
            If adjustedNamespace IsNot namespaceReceiver.NamespaceSymbol Then
                Dim receiver As BoundExpression = namespaceReceiver.UnevaluatedReceiverOpt

                If receiver IsNot Nothing AndAlso receiver.Kind = BoundKind.NamespaceExpression Then
                    Dim parentNamespace = DirectCast(receiver, BoundNamespaceExpression)

                    If parentNamespace.NamespaceSymbol.NamespaceKind = NamespaceKindNamespaceGroup AndAlso
                       IsNamespaceGroupIncludesButNotEquivalentTo(parentNamespace.NamespaceSymbol, adjustedNamespace.ContainingNamespace) Then
                        receiver = AdjustReceiverNamespace(parentNamespace, adjustedNamespace.ContainingNamespace)
                    End If
                End If

                Return namespaceReceiver.Update(receiver, namespaceReceiver.AliasOpt, adjustedNamespace)
            End If

            Return namespaceReceiver
        End Function

        Private Shared Function IsNamespaceGroupIncludesButNotEquivalentTo(namespaceGroup As NamespaceSymbol, other As NamespaceSymbol) As Boolean
            Debug.Assert(namespaceGroup.NamespaceKind = NamespaceKindNamespaceGroup)
            Dim result As Boolean

            If other.NamespaceKind <> NamespaceKindNamespaceGroup Then
                result = namespaceGroup.ConstituentNamespaces.Contains(other)
            Else
                Dim groupConstituents As ImmutableArray(Of NamespaceSymbol) = namespaceGroup.ConstituentNamespaces
                Dim otherConstituents As ImmutableArray(Of NamespaceSymbol) = other.ConstituentNamespaces

                If groupConstituents.Length > otherConstituents.Length Then
                    result = True

                    Dim lookup = New SmallDictionary(Of NamespaceSymbol, Boolean)()

                    For Each item In groupConstituents
                        lookup(item) = False
                    Next

                    For Each item In otherConstituents
                        If Not lookup.TryGetValue(item, Nothing) Then
                            result = False
                            Exit For
                        End If
                    Next
                Else
                    result = False
                End If
            End If

            Debug.Assert(result)
            Return result
        End Function

        Private Sub CheckMemberTypeAccessibility(diagnostics As BindingDiagnosticBag, node As SyntaxNode, member As Symbol)
            ' We are not doing this check during lookup due to a performance impact it has on IDE scenarios.
            ' In any case, an accessible member with inaccessible type is beyond language spec, so we have
            ' some freedom how to deal with it.

            Dim memberType As TypeSymbol

            Select Case member.Kind
                Case SymbolKind.Method
                    memberType = DirectCast(member, MethodSymbol).ReturnType
                    Exit Select

                Case SymbolKind.Property
                    memberType = DirectCast(member, PropertySymbol).Type
                    Exit Select

                Case SymbolKind.Field
                    ' Getting the type of a source field that is a constant can cause infinite
                    ' recursion if that field has an inferred type. Rather than passing in fields
                    ' currently being evaluated to break the recursion, we simply note that inferred 
                    ' types can never be inaccessible, so we don't check their types.

                    Dim fieldSym = DirectCast(member, FieldSymbol)
                    If fieldSym.HasDeclaredType Then
                        memberType = fieldSym.Type
                    Else
                        Return
                    End If

                Case Else
                    ' Somewhat strangely, event types are not checked.
                    Throw ExceptionUtilities.UnexpectedValue(member.Kind)
            End Select

            Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)
            If CheckAccessibility(memberType, useSiteInfo, accessThroughType:=Nothing) <> AccessCheckResult.Accessible Then
                ReportDiagnostic(diagnostics, node,
                                 New BadSymbolDiagnostic(member,
                                                   ERRID.ERR_InaccessibleReturnTypeOfMember2,
                                                   CustomSymbolDisplayFormatter.WithContainingType(member)))
            End If

            diagnostics.Add(node, useSiteInfo)
        End Sub

        Public Shared Function IsTopMostEnclosingLambdaAQueryLambda(containingMember As Symbol, stopAtContainer As Symbol) As Boolean
            Dim topMostEnclosingLambdaIsQueryLambda As Boolean = False

            ' Need to go up the chain of containers and see if the last lambda we see
            ' is a QueryLambda, before we reach the stopAtContainer. 
            Dim currentContainer As Symbol = containingMember

            While currentContainer IsNot Nothing AndAlso currentContainer IsNot stopAtContainer
                Debug.Assert(currentContainer.IsLambdaMethod OrElse stopAtContainer Is Nothing)
                If currentContainer.IsLambdaMethod Then
                    topMostEnclosingLambdaIsQueryLambda = currentContainer.IsQueryLambdaMethod
                Else
                    Exit While
                End If

                currentContainer = currentContainer.ContainingSymbol
            End While

            Return topMostEnclosingLambdaIsQueryLambda
        End Function

        Public Function BindLabel(node As LabelSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            Dim labelName As String = node.LabelToken.ValueText

            Dim result = LookupResult.GetInstance()
            Me.Lookup(result, labelName, arity:=0, options:=LookupOptions.LabelsOnly, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded)

            Dim symbol As LabelSymbol = Nothing
            Dim hasErrors As Boolean = False
            If result.IsGood AndAlso result.HasSingleSymbol Then
                symbol = DirectCast(result.Symbols.First(), LabelSymbol)
            Else
                If result.HasDiagnostic Then
                    ReportDiagnostic(diagnostics, node, result.Diagnostic)
                Else
                    ' The label is undefined
                    ReportDiagnostic(diagnostics, node, ERRID.ERR_LabelNotDefined1, labelName)
                End If

                hasErrors = True
            End If

            result.Free()

            If symbol Is Nothing Then
                Return New BoundBadExpression(node,
                                              LookupResultKind.Empty,
                                              ImmutableArray(Of Symbol).Empty,
                                              ImmutableArray(Of BoundExpression).Empty,
                                              Nothing,
                                              hasErrors:=True)
            Else
                Return New BoundLabel(node, symbol, Nothing, hasErrors:=hasErrors)
            End If
        End Function

        Private Function BindTypeArguments(
            typeArgumentsOpt As TypeArgumentListSyntax,
            diagnostics As BindingDiagnosticBag
        ) As BoundTypeArguments

            If typeArgumentsOpt Is Nothing Then
                Return Nothing
            End If

            Dim arguments = typeArgumentsOpt.Arguments

            'TODO: What should we do if count is 0? Can we get in a situation like this?
            '      Perhaps for a missing type argument case [Goo(Of )].

            Dim boundArguments(arguments.Count - 1) As TypeSymbol

            For i As Integer = 0 To arguments.Count - 1 Step 1
                boundArguments(i) = BindTypeSyntax(arguments(i), diagnostics)
            Next

            ' TODO: Should we set HasError flag if any of the BindTypeSyntax calls report errors?
            Return New BoundTypeArguments(typeArgumentsOpt, boundArguments.AsImmutableOrNull())
        End Function

        ''' <summary>
        ''' Report diagnostics relating to access shared/nonshared symbols. Returns true if an ERROR (but not a warning)
        ''' was reported. Also replaces receiver as a type with DefaultPropertyInstance when appropriate.
        ''' </summary>
        Private Function CheckSharedSymbolAccess(node As SyntaxNode, isShared As Boolean, <[In], Out> ByRef receiver As BoundExpression, qualKind As QualificationKind, diagnostics As BindingDiagnosticBag) As Boolean
            If isShared Then
                If qualKind = QualificationKind.QualifiedViaValue AndAlso receiver IsNot Nothing AndAlso
                        receiver.Kind <> BoundKind.TypeOrValueExpression AndAlso receiver.Kind <> BoundKind.MyBaseReference AndAlso
                        Not receiver.HasErrors Then

                    ' NOTE: Since using MyBase is the only way to call a method from base type
                    '       in some cases, calls with 'MyBase' receiver should not be marked
                    '       with this WRN_SharedMemberThroughInstance; 
                    ' WARNING: This differs from DEV10

                    ' we do not want to report this diagnostic in the case of an initialization of a field/property
                    ' through an object initializer. In that case we will output an error 
                    ' "BC30991: Member '{0}' cannot be initialized in an object initializer expression because it is shared." 
                    ' instead.
                    If node.Parent Is Nothing OrElse
                        node.Parent.Kind <> SyntaxKind.NamedFieldInitializer Then
                        ReportDiagnostic(diagnostics, node, ERRID.WRN_SharedMemberThroughInstance)
                    End If
                End If
            Else
                If qualKind = QualificationKind.QualifiedViaTypeName OrElse
                    (qualKind = QualificationKind.Unqualified AndAlso receiver Is Nothing) Then

                    If qualKind = QualificationKind.QualifiedViaTypeName AndAlso receiver IsNot Nothing AndAlso
                       receiver.Kind = BoundKind.TypeExpression Then

                        ' Try default instance property through DefaultInstanceAlias
                        Dim instance As BoundExpression = TryDefaultInstanceProperty(DirectCast(receiver, BoundTypeExpression), diagnostics)

                        If instance IsNot Nothing Then
                            receiver = instance
                            Return False
                        End If
                    End If

                    ' We don't have a valid qualifier for this instance method.
                    If receiver IsNot Nothing AndAlso receiver.Kind = BoundKind.TypeExpression AndAlso IsReceiverOfNameOfArgument(receiver.Syntax) Then
                        receiver = New BoundTypeAsValueExpression(receiver.Syntax, DirectCast(receiver, BoundTypeExpression), receiver.Type).MakeCompilerGenerated()
                        Return False
                    Else
                        ReportDiagnostic(diagnostics, node, ERRID.ERR_ObjectReferenceNotSupplied)
                        Return True
                    End If
                End If

                Dim errorId As ERRID = Nothing
                If qualKind = QualificationKind.Unqualified AndAlso Not IsNameOfArgument(node) AndAlso Not CanAccessMe(True, errorId) Then
                    ' We can't use implicit Me here.
                    ReportDiagnostic(diagnostics, node, errorId)
                    Return True
                End If
            End If

            Return False
        End Function

        Private Shared Function IsReceiverOfNameOfArgument(syntax As SyntaxNode) As Boolean
            Dim parent = syntax.Parent

            Return parent IsNot Nothing AndAlso
                   parent.Kind = SyntaxKind.SimpleMemberAccessExpression AndAlso
                   DirectCast(parent, MemberAccessExpressionSyntax).Expression Is syntax AndAlso
                   IsNameOfArgument(parent)
        End Function

        Private Shared Function IsNameOfArgument(syntax As SyntaxNode) As Boolean
            Return syntax.Parent IsNot Nothing AndAlso
                   syntax.Parent.Kind = SyntaxKind.NameOfExpression AndAlso
                   DirectCast(syntax.Parent, NameOfExpressionSyntax).Argument Is syntax
        End Function

        ''' <summary> 
        ''' Returns a bound node for left part of dictionary access node with omitted left syntax. 
        ''' In particular it handles dictionary access inside With statement.
        ''' 
        ''' By default the method delegates the work to it's containing binder or returns Nothing.
        ''' </summary>
        Protected Overridable Function TryBindOmittedLeftForDictionaryAccess(node As MemberAccessExpressionSyntax,
                                                                             accessingBinder As Binder,
                                                                             diagnostics As BindingDiagnosticBag) As BoundExpression
            Debug.Assert(Me.ContainingBinder IsNot Nothing)
            Return Me.ContainingBinder.TryBindOmittedLeftForDictionaryAccess(node, accessingBinder, diagnostics)
        End Function

        Private Function BindDictionaryAccess(node As MemberAccessExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            Dim leftOpt = node.Expression
            Dim left As BoundExpression
            If leftOpt Is Nothing Then
                ' Spec 11.7: "If an exclamation point is specified with no expression, the
                ' expression from the immediately containing With statement is assumed.
                ' If there is no containing With statement, a compile-time error occurs."

                Dim conditionalAccess As ConditionalAccessExpressionSyntax = node.GetCorrespondingConditionalAccessExpression()

                If conditionalAccess IsNot Nothing Then
                    left = GetConditionalAccessReceiver(conditionalAccess)
                Else
                    left = TryBindOmittedLeftForDictionaryAccess(node, Me, diagnostics)
                End If

                If left Is Nothing Then
                    ' Didn't find binder that can handle member access with omitted left part

                    Return BadExpression(
                        node,
                        ImmutableArray.Create(
                            ReportDiagnosticAndProduceBadExpression(diagnostics, node, ERRID.ERR_BadWithRef),
                            New BoundLiteral(
                                node.Name,
                                ConstantValue.Create(node.Name.Identifier.ValueText),
                                GetSpecialType(SpecialType.System_String, node.Name, diagnostics))),
                        ErrorTypeSymbol.UnknownResultType)
                End If
            Else
                left = Me.BindExpression(leftOpt, diagnostics)
            End If

            If Not left.IsLValue AndAlso left.Kind <> BoundKind.LateMemberAccess Then
                left = MakeRValue(left, diagnostics)
                Debug.Assert(left IsNot Nothing)
            End If

            Dim type = left.Type
            Debug.Assert(type IsNot Nothing)

            If Not type.IsErrorType() Then

                If type.SpecialType = SpecialType.System_Object OrElse type.IsExtensibleInterfaceNoUseSiteDiagnostics() Then
                    Dim name = node.Name
                    Dim arg = New BoundLiteral(name, ConstantValue.Create(node.Name.Identifier.ValueText), GetSpecialType(SpecialType.System_String, name, diagnostics))
                    Dim boundArguments = ImmutableArray.Create(Of BoundExpression)(arg)
                    Return BindLateBoundInvocation(node, Nothing, left, boundArguments, Nothing, diagnostics)
                End If

                If type.IsInterfaceType Then
                    Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)
                    ' In case IsExtensibleInterfaceNoUseSiteDiagnostics above failed because there were bad inherited interfaces.
                    type.AllInterfacesWithDefinitionUseSiteDiagnostics(useSiteInfo)
                    diagnostics.Add(node, useSiteInfo)
                End If

                Dim defaultPropertyGroup As BoundExpression = BindDefaultPropertyGroup(node, left, diagnostics)
                Debug.Assert(defaultPropertyGroup Is Nothing OrElse defaultPropertyGroup.Kind = BoundKind.PropertyGroup OrElse
                             defaultPropertyGroup.Kind = BoundKind.MethodGroup OrElse defaultPropertyGroup.HasErrors)

                ' Dev10 limits Dictionary access to properties.
                If defaultPropertyGroup IsNot Nothing AndAlso defaultPropertyGroup.Kind = BoundKind.PropertyGroup Then
                    Dim name = node.Name
                    Dim arg = New BoundLiteral(name, ConstantValue.Create(node.Name.Identifier.ValueText), GetSpecialType(SpecialType.System_String, name, diagnostics))
                    Return BindInvocationExpression(
                        node,
                        left.Syntax,
                        TypeCharacter.None,
                        DirectCast(defaultPropertyGroup, BoundPropertyGroup),
                        boundArguments:=ImmutableArray.Create(Of BoundExpression)(arg),
                        argumentNames:=Nothing,
                        diagnostics:=diagnostics,
                        isDefaultMemberAccess:=True,
                        callerInfoOpt:=node)

                ElseIf defaultPropertyGroup Is Nothing OrElse Not defaultPropertyGroup.HasErrors Then
                    Select Case type.TypeKind
                        Case TYPEKIND.Array, TYPEKIND.Enum
                            ReportQualNotObjectRecord(left, diagnostics)
                        Case TYPEKIND.Class
                            If type.SpecialType = SpecialType.System_Array Then
                                ReportDefaultMemberNotProperty(left, diagnostics)
                            Else
                                ReportNoDefaultProperty(left, diagnostics)
                            End If
                        Case TYPEKIND.TypeParameter, TYPEKIND.Interface
                            ReportNoDefaultProperty(left, diagnostics)
                        Case TYPEKIND.Structure
                            If type.IsIntrinsicValueType() Then
                                ReportQualNotObjectRecord(left, diagnostics)
                            Else
                                ReportNoDefaultProperty(left, diagnostics)
                            End If
                        Case Else
                            ReportDefaultMemberNotProperty(left, diagnostics)
                    End Select
                End If
            End If

            Return BadExpression(
                node,
                ImmutableArray.Create(
                    left,
                    New BoundLiteral(
                        node.Name,
                        ConstantValue.Create(node.Name.Identifier.ValueText),
                        GetSpecialType(SpecialType.System_String, node.Name, diagnostics))),
                ErrorTypeSymbol.UnknownResultType)
        End Function

        Private Shared Sub ReportNoDefaultProperty(expr As BoundExpression, diagnostics As BindingDiagnosticBag)
            Dim type = expr.Type
            Dim syntax = expr.Syntax
            Select Case type.TypeKind
                Case TYPEKIND.Class
                    ' "Class '{0}' cannot be indexed because it has no default property."
                    ReportDiagnostic(diagnostics, syntax, ERRID.ERR_NoDefaultNotExtend1, type)
                Case TYPEKIND.Structure
                    ' "Structure '{0}' cannot be indexed because it has no default property."
                    ReportDiagnostic(diagnostics, syntax, ERRID.ERR_StructureNoDefault1, type)
                Case TYPEKIND.Error
                    ' We should have reported an error elsewhere.
                Case Else
                    ' "'{0}' cannot be indexed because it has no default property."
                    ReportDiagnostic(diagnostics, syntax, ERRID.ERR_InterfaceNoDefault1, type)
            End Select
        End Sub

        Private Shared Sub ReportQualNotObjectRecord(expr As BoundExpression, diagnostics As BindingDiagnosticBag)
            ' "'!' requires its left operand to have a type parameter, class or interface type, but this operand has the type '{0}'."
            ReportDiagnostic(diagnostics, expr.Syntax, ERRID.ERR_QualNotObjectRecord1, expr.Type)
        End Sub

        Private Shared Sub ReportDefaultMemberNotProperty(expr As BoundExpression, diagnostics As BindingDiagnosticBag)
            ' "Default member '{0}' is not a property."
            ' Note: The error argument is the expression type
            ' rather than the expression text used in Dev10.
            ReportDiagnostic(diagnostics, expr.Syntax, ERRID.ERR_DefaultMemberNotProperty1, expr.Type)
        End Sub

        Private Shared Function GenerateBadExpression(node As InvocationExpressionSyntax, target As BoundExpression, boundArguments As ImmutableArray(Of BoundExpression)) As BoundExpression
            Dim children = ArrayBuilder(Of BoundExpression).GetInstance()
            children.Add(target)
            children.AddRange(boundArguments)
            Return BadExpression(node, children.ToImmutableAndFree(), ErrorTypeSymbol.UnknownResultType)
        End Function

        Private Shared Sub VerifyTypeCharacterConsistency(nodeOrToken As SyntaxNodeOrToken, type As TypeSymbol, typeChar As TypeCharacter, diagnostics As BindingDiagnosticBag)
            Dim typeCharacterString As String = Nothing
            Dim specialType As SpecialType = GetSpecialTypeForTypeCharacter(typeChar, typeCharacterString)

            If specialType <> Microsoft.CodeAnalysis.SpecialType.None Then

                If type.IsArrayType() Then
                    type = DirectCast(type, ArrayTypeSymbol).ElementType
                End If

                type = type.GetNullableUnderlyingTypeOrSelf()

                If type.SpecialType <> specialType Then
                    ReportDiagnostic(diagnostics, nodeOrToken, ERRID.ERR_TypecharNoMatch2, typeCharacterString, type)
                End If
            End If
        End Sub

        Private Shared Sub VerifyTypeCharacterConsistency(name As SimpleNameSyntax, type As TypeSymbol, diagnostics As BindingDiagnosticBag)
            Dim typeChar As TypeCharacter = name.Identifier.GetTypeCharacter()
            If typeChar = TypeCharacter.None Then
                Return
            End If

            Dim typeCharacterString As String = Nothing
            Dim specialType As SpecialType = GetSpecialTypeForTypeCharacter(typeChar, typeCharacterString)

            If specialType <> specialType.None Then
                If type.IsArrayType() Then
                    type = DirectCast(type, ArrayTypeSymbol).ElementType
                End If

                type = type.GetNullableUnderlyingTypeOrSelf()

                If type.SpecialType <> specialType Then
                    ReportDiagnostic(diagnostics, name, ERRID.ERR_TypecharNoMatch2, typeCharacterString, type)
                End If
            End If
        End Sub

        Private Function BindArrayAccess(node As InvocationExpressionSyntax, expr As BoundExpression, boundArguments As ImmutableArray(Of BoundExpression), argumentNames As ImmutableArray(Of String), diagnostics As BindingDiagnosticBag) As BoundExpression
            Debug.Assert(node IsNot Nothing)
            Debug.Assert(expr IsNot Nothing)

            If expr.IsLValue Then
                expr = expr.MakeRValue()
            End If

            Dim convertedArguments = ArrayBuilder(Of BoundExpression).GetInstance(boundArguments.Length)
            Dim int32Type = GetSpecialType(SpecialType.System_Int32, node.ArgumentList, diagnostics)

            For Each argument In boundArguments
                convertedArguments.Add(ApplyImplicitConversion(argument.Syntax, int32Type, argument, diagnostics))
            Next

            boundArguments = convertedArguments.ToImmutableAndFree()

            Dim exprType = expr.Type
            If exprType Is Nothing Then
                Return New BoundArrayAccess(node, expr, boundArguments, Nothing, hasErrors:=True)
            End If

            If Not argumentNames.IsDefault AndAlso argumentNames.Length > 0 Then
                ReportDiagnostic(diagnostics, node, ERRID.ERR_NamedSubscript)
            End If

            Dim arrayType As ArrayTypeSymbol = DirectCast(expr.Type, ArrayTypeSymbol)
            Dim rank As Integer = arrayType.Rank
            If boundArguments.Length <> arrayType.Rank Then
                Dim err As ERRID
                If boundArguments.Length > arrayType.Rank Then
                    err = ERRID.ERR_TooManyIndices
                Else
                    err = ERRID.ERR_TooFewIndices
                End If
                ReportDiagnostic(diagnostics, node.ArgumentList, err)
                Return New BoundArrayAccess(node, expr, boundArguments, arrayType.ElementType, hasErrors:=True)
            End If

            Return New BoundArrayAccess(node, expr, boundArguments, arrayType.ElementType)
        End Function

        ' Get the common return type of a set of symbols, or error type if no common return type. Used
        ' in error cases to give a type in ambiguity situations.
        ' If we can't find a common type, create an error type. If all the types have a common name,
        ' that name is used as the type of the error type (useful in ambiguous type lookup situations)
        Private Function GetCommonExpressionTypeForErrorRecovery(
            symbolReference As VisualBasicSyntaxNode,
            symbols As ImmutableArray(Of Symbol),
            constantFieldsInProgress As ConstantFieldsInProgress
        ) As TypeSymbol
            Dim commonType As TypeSymbol = Nothing
            Dim commonName As String = Nothing
            Dim noCommonType As Boolean = False
            Dim noCommonName As Boolean = False

            Dim discardedDiagnostics = BindingDiagnosticBag.Discarded

            For i As Integer = 0 To symbols.Length - 1

                Dim expressionType As TypeSymbol = GetExpressionType(symbolReference, symbols(i), constantFieldsInProgress, discardedDiagnostics)

                If expressionType IsNot Nothing Then
                    If commonType Is Nothing Then
                        commonType = expressionType
                    ElseIf Not noCommonType AndAlso Not commonType.Equals(expressionType) Then
                        noCommonType = True
                    End If

                    If commonName Is Nothing Then
                        commonName = expressionType.Name
                    ElseIf Not noCommonName AndAlso Not CaseInsensitiveComparison.Equals(commonName, expressionType.Name) Then
                        noCommonName = True
                    End If
                End If
            Next

            If noCommonType Then
                If noCommonName Then
                    Return ErrorTypeSymbol.UnknownResultType
                Else
                    Return New ExtendedErrorTypeSymbol(Nothing, commonName)
                End If
            Else
                Return commonType
            End If
        End Function

        ' Get the "expression type" of a symbol when used in an expression.
        Private Function GetExpressionType(
            symbolReference As VisualBasicSyntaxNode,
            s As Symbol,
            constantFieldsInProgress As ConstantFieldsInProgress,
            diagnostics As BindingDiagnosticBag
        ) As TypeSymbol
            Select Case s.Kind
                Case SymbolKind.Method
                    Return DirectCast(s, MethodSymbol).ReturnType
                Case SymbolKind.Field
                    ' const fields may need to determine the type because it's inferred
                    ' This is why using .Type was replaced by .GetInferredType to detect cycles.
                    Return DirectCast(s, FieldSymbol).GetInferredType(constantFieldsInProgress)
                Case SymbolKind.Property
                    Return DirectCast(s, PropertySymbol).Type
                Case SymbolKind.Parameter
                    Return DirectCast(s, ParameterSymbol).Type
                Case SymbolKind.Local
                    Return GetLocalSymbolType(DirectCast(s, LocalSymbol), symbolReference, diagnostics)
                Case SymbolKind.RangeVariable
                    Return DirectCast(s, RangeVariableSymbol).Type
                Case Else
                    Dim type = TryCast(s, TypeSymbol)
                    If type IsNot Nothing Then
                        Return type
                    End If
            End Select

            Return Nothing
        End Function

        ' Given the expression part of a named argument, get the token of it's name. We use this for error reported, and its more efficient
        ' to calculate it only when needed when reported a diagnostic.
        Private Shared Function GetNamedArgumentIdentifier(argumentExpression As SyntaxNode) As SyntaxToken
            Dim parent = TryCast(argumentExpression.Parent, SimpleArgumentSyntax)

            If parent Is Nothing OrElse Not parent.IsNamed Then
                Debug.Assert(False, "Did not found a NamedArgumentSyntax where one should have been")
                Return argumentExpression.GetFirstToken() ' since we use this for error reporting, this gives us something close, anyway.
            Else
                Return parent.NameColonEquals.Name.Identifier
            End If
        End Function

        Private Structure DimensionSize
            Public Enum SizeKind As Byte
                Unknown
                Constant
                NotConstant
            End Enum

            Public ReadOnly Kind As SizeKind
            Public ReadOnly Size As Integer

            Private Sub New(size As Integer, kind As SizeKind)
                Me.Size = size
                Me.Kind = kind
            End Sub

            Public Shared Function ConstantSize(size As Integer) As DimensionSize
                Return New DimensionSize(size, SizeKind.Constant)
            End Function

            Public Shared Function VariableSize() As DimensionSize
                Return New DimensionSize(0, SizeKind.NotConstant)
            End Function
        End Structure

        ''' <summary>
        '''  Handle ArrayCreationExpressionSyntax
        '''   new integer(n)(,) {...}
        '''   new integer() {...}
        ''' </summary>
        Private Function BindArrayCreationExpression(node As ArrayCreationExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression

            ' Bind the type
            Dim baseType = BindTypeSyntax(node.Type, diagnostics)

            Dim arrayBoundsOpt = node.ArrayBounds

            Dim boundArguments As ImmutableArray(Of BoundExpression) = Nothing

            ' Get the resulting array type by applying the array rank specifiers and the array bounds
            Dim arrayType = CreateArrayOf(baseType, node.RankSpecifiers, arrayBoundsOpt, diagnostics)

            Dim knownSizes(arrayType.Rank - 1) As DimensionSize

            ' Bind the bounds.  This returns the known sizes of each dimension from the optional bounds
            boundArguments = BindArrayBounds(arrayBoundsOpt, diagnostics, knownSizes)

            ' Bind the array initializer.  This may update the known sizes for dimensions with initializers but that are missing explicit bounds
            Dim boundInitializers = BindArrayInitializerList(node.Initializer, arrayType, knownSizes, diagnostics)

            'Construct a set of size expressions if we were not given any.

            If boundArguments.Length = 0 Then
                boundArguments = CreateArrayBounds(node, knownSizes, diagnostics)
            End If

            Return New BoundArrayCreation(node, boundArguments, boundInitializers, arrayType)

        End Function

        Private Function BindArrayLiteralExpression(node As CollectionInitializerSyntax,
                                                    diagnostics As BindingDiagnosticBag) As BoundExpression

            ' Inspect the collection initializer to determine the literal's rank
            ' Per 11.1.1, the array literal is reclassified to a value whose type is an array of rank equal to the level of nesting is used.
            Dim rank = ComputeArrayLiteralRank(node)

            Dim knownSizes(rank - 1) As DimensionSize
            Dim hasDominantType As Boolean
            Dim numberOfCandidates As Integer
            Dim inferredElementType As TypeSymbol = Nothing
            Dim arrayInitializer = BindArrayInitializerList(node, knownSizes, hasDominantType, numberOfCandidates, inferredElementType, diagnostics)

            ' Similar to ReclassifyArrayLiteralExpression:
            ' Mark as compiler generated so that semantic model does not select the array initialization bound node.
            ' The array initialization node is not a real expression and lacks a type.
            arrayInitializer.SetWasCompilerGenerated()

            Dim inferredArrayType = ArrayTypeSymbol.CreateVBArray(inferredElementType, Nothing, knownSizes.Length, Compilation)

            Dim sizes As ImmutableArray(Of BoundExpression) = CreateArrayBounds(node, knownSizes, diagnostics)

            Return New BoundArrayLiteral(node, hasDominantType, numberOfCandidates, inferredArrayType, sizes, arrayInitializer, Me)
        End Function

        Private Function CreateArrayBounds(node As SyntaxNode, knownSizes() As DimensionSize, diagnostics As BindingDiagnosticBag) As ImmutableArray(Of BoundExpression)
            Dim rank = knownSizes.Length
            Dim sizes = New BoundExpression(rank - 1) {}
            Dim Int32Type = GetSpecialType(SpecialType.System_Int32, node, diagnostics)
            For i As Integer = 0 To knownSizes.Length - 1
                Dim size = knownSizes(i)

                'It is possible in error scenarios that some of the bounds were not determined. 
                'Use default values (0) for those.
                Dim sizeExpr = New BoundLiteral(
                                   node,
                                   ConstantValue.Create(size.Size),
                                   Int32Type
                               )

                sizeExpr.SetWasCompilerGenerated()
                sizes(i) = sizeExpr
            Next

            Return sizes.AsImmutableOrNull
        End Function

        Private Shared Function ComputeArrayLiteralRank(node As CollectionInitializerSyntax) As Integer
            Dim rank As Integer = 1

            Do
                Dim initializers = node.Initializers
                If initializers.Count = 0 Then
                    Exit Do
                End If

                Dim expr = initializers(0)
                node = TryCast(expr, CollectionInitializerSyntax)
                If node Is Nothing Then
                    Exit Do
                End If

                rank += 1
            Loop

            Return rank
        End Function

        ''' <summary>
        ''' Binds CollectionInitializeSyntax. i.e. { expr, ... } from an ArrayCreationExpressionSyntax
        ''' </summary>
        ''' <param name="node">The collection initializer syntax</param>
        ''' <param name="type">The type of array.</param>
        ''' <param name="knownSizes">This is in/out.  It comes in with sizes from explicit bounds but will be updated based on the number of initializers for dimensions without bounds</param>
        ''' <param name="diagnostics">Where to put errors</param>
        Private Function BindArrayInitializerList(node As CollectionInitializerSyntax,
                                               type As ArrayTypeSymbol,
                                               knownSizes As DimensionSize(),
                                               diagnostics As BindingDiagnosticBag) As BoundArrayInitialization
            Debug.Assert(type IsNot Nothing)

            Dim result = BindArrayInitializerList(node, type, knownSizes, 1, Nothing, diagnostics)

            Return result
        End Function

        ''' <summary>
        ''' Binds CollectionInitializeSyntax. i.e. { expr, ... } from an ArrayCreationExpressionSyntax
        ''' </summary>
        ''' <param name="node">The collection initializer syntax</param>
        ''' <param name="knownSizes">This is in/out.  It comes in with sizes from explicit bounds but will be updated based on the number of initializers for dimensions without bounds</param>
        ''' <param name="hasDominantType">When the inferred type is Object() indicates that the dominant type algorithm computed this type.</param>
        ''' <param name="numberOfCandidates" >The number of candidates found during inference</param>
        ''' <param name="inferredElementType" >The inferred element type</param>
        ''' <param name="diagnostics">Where to put errors</param>
        Private Function BindArrayInitializerList(node As CollectionInitializerSyntax,
                                       knownSizes As DimensionSize(),
                                       <Out> ByRef hasDominantType As Boolean,
                                       <Out> ByRef numberOfCandidates As Integer,
                                       <Out> ByRef inferredElementType As TypeSymbol,
                                       diagnostics As BindingDiagnosticBag) As BoundArrayInitialization

            ' Infer the type for this array literal

            Dim initializers As ArrayBuilder(Of BoundExpression) = ArrayBuilder(Of BoundExpression).GetInstance

            Dim result = BindArrayInitializerList(node, Nothing, knownSizes, 1, initializers, diagnostics)

            inferredElementType = InferDominantTypeOfExpressions(node, initializers, diagnostics, numberOfCandidates)

            If inferredElementType Is Nothing Then
                ' When no dominant type exists, use Object but remember that there wasn't a dominant type.
                inferredElementType = GetSpecialType(SpecialType.System_Object, node, diagnostics)
                hasDominantType = False
            Else
                hasDominantType = True
            End If

            initializers.Free()

            Return result
        End Function

        Private Function BindArrayInitializerList(node As CollectionInitializerSyntax,
                                                  type As ArrayTypeSymbol,
                                                  knownSizes As DimensionSize(),
                                                  dimension As Integer,
                                                  allInitializers As ArrayBuilder(Of BoundExpression),
                                                  diagnostics As BindingDiagnosticBag) As BoundArrayInitialization
            Debug.Assert(type IsNot Nothing OrElse allInitializers IsNot Nothing)

            Dim initializers = ArrayBuilder(Of BoundExpression).GetInstance

            Dim arrayInitType As TypeSymbol
            If dimension = 1 Then
                ' binding the outer-most initializer list; the result type is the array type being created.
                arrayInitType = type
            Else
                ' binding an inner initializer list; the result type is nothing.
                arrayInitType = Nothing
            End If

            Dim rank As Integer = knownSizes.Length

            If dimension <> 1 OrElse node.Initializers.Count <> 0 Then

                Debug.Assert(type Is Nothing OrElse type.Rank = rank)

                If dimension = rank Then

                    ' We are processing the nth dimension of a rank-n array. We expect
                    ' that these will only be values, not array initializers.
                    Dim elemType As TypeSymbol = If(type IsNot Nothing, type.ElementType, Nothing)

                    For Each expressionSyntax In node.Initializers

                        Dim boundExpression As BoundExpression

                        If expressionSyntax.Kind <> SyntaxKind.CollectionInitializer Then
                            boundExpression = BindValue(expressionSyntax, diagnostics)

                            If elemType IsNot Nothing Then
                                boundExpression = ApplyImplicitConversion(expressionSyntax, elemType, boundExpression, diagnostics)
                            End If
                        Else
                            boundExpression = ReportDiagnosticAndProduceBadExpression(diagnostics, expressionSyntax, ERRID.ERR_ArrayInitializerTooManyDimensions)
                        End If

                        initializers.Add(boundExpression)

                        If allInitializers IsNot Nothing Then
                            allInitializers.Add(boundExpression)
                        End If

                    Next
                Else
                    ' Inductive case; we'd better have another array initializer
                    For Each expr In node.Initializers
                        Dim init As BoundArrayInitialization = Nothing

                        If expr.Kind = SyntaxKind.CollectionInitializer Then
                            init = Me.BindArrayInitializerList(DirectCast(expr, CollectionInitializerSyntax), type, knownSizes, dimension + 1, allInitializers, diagnostics)

                        Else
                            ReportDiagnostic(diagnostics, expr, ERRID.ERR_ArrayInitializerTooFewDimensions)
                            init = New BoundArrayInitialization(expr, ImmutableArray(Of BoundExpression).Empty, arrayInitType, hasErrors:=True)
                        End If

                        initializers.Add(init)
                    Next
                End If

                Dim curSize = knownSizes(dimension - 1)

                If curSize.Kind = DimensionSize.SizeKind.Unknown Then
                    knownSizes(dimension - 1) = DimensionSize.ConstantSize(initializers.Count)

                ElseIf curSize.Kind = DimensionSize.SizeKind.NotConstant Then
                    ReportDiagnostic(diagnostics, node, ERRID.ERR_ArrayInitializerForNonConstDim)
                    Return New BoundArrayInitialization(node, initializers.ToImmutableAndFree(), arrayInitType, hasErrors:=True)

                ElseIf curSize.Size < initializers.Count Then
                    ReportDiagnostic(diagnostics, node, ERRID.ERR_InitializerTooManyElements1, initializers.Count - curSize.Size)
                    Return New BoundArrayInitialization(node, initializers.ToImmutableAndFree(), arrayInitType, hasErrors:=True)

                ElseIf curSize.Size > initializers.Count Then
                    ReportDiagnostic(diagnostics, node, ERRID.ERR_InitializerTooFewElements1, curSize.Size - initializers.Count)
                    Return New BoundArrayInitialization(node, initializers.ToImmutableAndFree(), arrayInitType, hasErrors:=True)

                End If
            End If

            Return New BoundArrayInitialization(node, initializers.ToImmutableAndFree(), arrayInitType)
        End Function

        Private Sub CheckRangeArgumentLowerBound(rangeArgument As RangeArgumentSyntax, diagnostics As BindingDiagnosticBag)
            Dim lowerBound = BindValue(rangeArgument.LowerBound, diagnostics)

            ' This check was moved from the parser to the binder.  For backwards compatibility with Dev10, the constant must
            ' be integral.  This seems a bit inconsistent because the range argument allows (0 to 5.0) but not (0.0 to 5.0)
            Dim lowerBoundConstantValueOpt As ConstantValue = lowerBound.ConstantValueOpt

            If lowerBoundConstantValueOpt Is Nothing OrElse Not lowerBoundConstantValueOpt.IsIntegral OrElse Not lowerBoundConstantValueOpt.IsDefaultValue Then
                ReportDiagnostic(diagnostics, rangeArgument.LowerBound, ERRID.ERR_OnlyNullLowerBound)
            End If
        End Sub

        ''' <summary>
        ''' Bind the array bounds and return the sizes for each dimension.
        ''' </summary>
        ''' <param name="arrayBoundsOpt">The bounds</param>
        ''' <param name="diagnostics">Where to put the errors</param>
        ''' <param name="knownSizes">The bounds if they are constants, if argument is not specified this info is not returned </param>
        Private Function BindArrayBounds(arrayBoundsOpt As ArgumentListSyntax,
                                         diagnostics As BindingDiagnosticBag,
                                         Optional knownSizes As DimensionSize() = Nothing,
                                         Optional errorOnEmptyBound As Boolean = False) As ImmutableArray(Of BoundExpression)

            If arrayBoundsOpt Is Nothing Then
                Return s_noArguments
            End If

            Dim arguments As SeparatedSyntaxList(Of ArgumentSyntax) = arrayBoundsOpt.Arguments
            Dim boundArgumentsBuilder As ArrayBuilder(Of BoundExpression) = ArrayBuilder(Of BoundExpression).GetInstance
            Dim int32Type = GetSpecialType(SpecialType.System_Int32, arrayBoundsOpt, diagnostics)

            ' check if there is any nonempty array bound
            ' in such case we will require all other bounds be nonempty
            For Each argumentSyntax In arguments
                Select Case argumentSyntax.Kind
                    Case SyntaxKind.SimpleArgument, SyntaxKind.RangeArgument
                        errorOnEmptyBound = True
                        Exit For
                End Select
            Next

            For i As Integer = 0 To arguments.Count - 1
                Dim upperBound As BoundExpression = Nothing
                Dim upperBoundSyntax As ExpressionSyntax = Nothing

                Dim argumentSyntax = arguments(i)

                Select Case argumentSyntax.Kind

                    Case SyntaxKind.SimpleArgument

                        Dim simpleArgument = DirectCast(argumentSyntax, SimpleArgumentSyntax)

                        If simpleArgument.NameColonEquals IsNot Nothing Then
                            ReportDiagnostic(diagnostics, argumentSyntax, ERRID.ERR_NamedSubscript)
                        End If

                        upperBoundSyntax = simpleArgument.Expression

                    Case SyntaxKind.RangeArgument
                        Dim rangeArgument = DirectCast(argumentSyntax, RangeArgumentSyntax)
                        CheckRangeArgumentLowerBound(rangeArgument, diagnostics)
                        upperBoundSyntax = rangeArgument.UpperBound

                    Case SyntaxKind.OmittedArgument
                        If errorOnEmptyBound Then
                            ReportDiagnostic(diagnostics, argumentSyntax, ERRID.ERR_MissingSubscript)
                            GoTo lElseClause
                        Else
                            Continue For
                        End If
                    Case Else
lElseClause:
                        ' TODO - What expression should be generated in this case?  Note, the parser already generates a syntax error.
                        ' The syntax is a missing Identifier and not an OmittedArgumentSyntax.
                        upperBound = BadExpression(argumentSyntax, ErrorTypeSymbol.UnknownResultType)

                End Select

                If upperBoundSyntax IsNot Nothing Then
                    upperBound = BindValue(upperBoundSyntax, diagnostics)
                    upperBound = ApplyImplicitConversion(upperBoundSyntax, int32Type, upperBound, diagnostics)
                End If

                ' Add 1 to the upper bound to get the array size
                ' Dev10 does not consider checked/unchecked here when folding the addition
                ' in a case of overflow exception will be thrown at run time
                Dim upperBoundConstantValueOpt As ConstantValue = upperBound.ConstantValueOpt

                If upperBoundConstantValueOpt IsNot Nothing AndAlso Not upperBoundConstantValueOpt.IsBad Then
                    ' -1 is a valid value it means 0 - length array
                    ' anything less is invalid.
                    If upperBoundConstantValueOpt.Int32Value < -1 Then
                        ReportDiagnostic(diagnostics, argumentSyntax, ERRID.ERR_NegativeArraySize)
                    End If
                End If

                Dim one = New BoundLiteral(argumentSyntax, ConstantValue.Create(1), int32Type)
                one.SetWasCompilerGenerated()

                ' Try folding the size.
                Dim integerOverflow As Boolean = False
                Dim divideByZero As Boolean = False
                ' Note: the value may overflow, but we ignore this and use the overflown value. 
                Dim value = OverloadResolution.TryFoldConstantBinaryOperator(BinaryOperatorKind.Add, upperBound, one, int32Type, integerOverflow, divideByZero, Nothing)

                If knownSizes IsNot Nothing Then
                    If value IsNot Nothing Then
                        knownSizes(i) = DimensionSize.ConstantSize(value.Int32Value)
                    Else
                        knownSizes(i) = DimensionSize.VariableSize
                    End If
                End If

                Dim actualSize = New BoundBinaryOperator(
                        argumentSyntax,
                        BinaryOperatorKind.Add,
                        upperBound,
                        one,
                        CheckOverflow,
                        value,
                        int32Type
                    )

                actualSize.SetWasCompilerGenerated()
                boundArgumentsBuilder.Add(actualSize)

            Next

            Return boundArgumentsBuilder.ToImmutableAndFree
        End Function

        Private Function BindLiteralConstant(node As LiteralExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundLiteral
            Dim value = node.Token.Value

            Dim cv As ConstantValue
            Dim type As TypeSymbol = Nothing

            If value Is Nothing Then
                ' this is for Null
                cv = ConstantValue.Null
            Else
                Debug.Assert(Not value.GetType().GetTypeInfo().IsEnum)

                Dim specialType As SpecialType = SpecialTypeExtensions.FromRuntimeTypeOfLiteralValue(value)

                ' VB literals can't be of type byte, sbyte
                Debug.Assert(specialType <> specialType.None AndAlso
                             specialType <> specialType.System_Byte AndAlso
                             specialType <> specialType.System_SByte)

                cv = ConstantValue.Create(value, specialType)
                type = GetSpecialType(specialType, node, diagnostics)
            End If

            Return New BoundLiteral(node, cv, type)
        End Function

        Friend Function InferDominantTypeOfExpressions(
            syntax As SyntaxNode,
            Expressions As ArrayBuilder(Of BoundExpression),
            diagnostics As BindingDiagnosticBag,
            ByRef numCandidates As Integer,
            Optional ByRef errorReasons As InferenceErrorReasons = InferenceErrorReasons.Other
        ) As TypeSymbol

            ' Arguments: "expressions" is a list of expressions from which to infer dominant type
            ' Output: we might return Nothing / NumCandidates==0 (if we couldn't find a dominant type)
            ' Or we might return Object / NumCandidates==0 (if we had to assume Object because no dominant type was found)
            ' Or we might return Object / NumCandidates>=2 (if we had to assume Object because multiple candidates were found)
            ' Or we might return a real dominant type from one of the candidates / NumCandidates==1
            ' In the last case, "winner" is set to one of the expressions who proposed that winning dominant type.
            ' "Winner" information might be useful if you are calculating the dominant type of "{}" and "{Nothing}"
            ' and you need to know who the winner is so you can report appropriate warnings on him.

            ' The dominant type of a list of elements means:
            ' (1) for each element, attempt to classify it as a value in a context where the target
            ' type is unknown. So unbound lambdas get classified as anonymous delegates, and array literals get
            ' classified according to their dominant type (if they have one), and Nothing is ignored, and AddressOf too.
            ' But skip over empty array literals.
            ' (2) Consider the types of all elements for which we got a type, and feed these into the dominant-type
            ' algorithm: if there are multiple candidates such that all other types convert to it through identity/widening,
            ' then pick the dominant type out of this set. Otherwise, if there is a single all-widening/identity candidate,
            ' pick this. Otherwise, if there is a single all-widening/identity/narrowing candidate, then pick this.
            ' (3) Otherwise, if the dominant type algorithm has failed and every element was an empty array literal {}
            ' then pick Object() and report a warning "Object assumed"
            ' (4) Otherwise, if every element converts to Object, then pick Object and report a warning "Object assumed".
            ' (5) Otherwise, there is no dominant type; return Nothing and report an error.

            numCandidates = 0
            Dim count As Integer = 0
            Dim countOfEmptyArrays As Integer = 0 ' To detect case (3)
            Dim anEmptyArray As BoundArrayLiteral = Nothing ' Used for case (3), so we'll return one of them
            Dim allConvertibleToObject As Boolean = True ' To detect case (4)

            Dim typeList As New TypeInferenceCollection()

            For Each expression As BoundExpression In Expressions
                count += 1

                Debug.Assert(expression IsNot Nothing)
                Debug.Assert(expression.IsValue())

                ' Dig through parenthesized.
                If Not expression.IsNothingLiteral Then
                    expression = expression.GetMostEnclosedParenthesizedExpression()
                End If

                Dim expressionKind As BoundKind = expression.Kind
                Dim expressionType As TypeSymbol = expression.Type

                If expressionKind = BoundKind.UnboundLambda Then
                    expressionType = DirectCast(expression, UnboundLambda).InferredAnonymousDelegate.Key
                    typeList.AddType(expressionType, RequiredConversion.Any, expression)

                ElseIf expressionKind = BoundKind.TupleLiteral Then
                    expressionType = DirectCast(expression, BoundTupleLiteral).InferredType
                    If expressionType IsNot Nothing Then
                        typeList.AddType(expressionType, RequiredConversion.Any, expression)
                    End If

                ElseIf expressionKind = BoundKind.ArrayLiteral Then
                    Dim arrayLiteral = DirectCast(expression, BoundArrayLiteral)

                    ' Empty array literals {} should not be constraints on the dominant type algorithm.
                    ' Array's without a dominant type should not be constraints on the dominant type algorithm.
                    If arrayLiteral.IsEmptyArrayLiteral Then
                        countOfEmptyArrays += 1
                        anEmptyArray = arrayLiteral
                    ElseIf arrayLiteral.HasDominantType Then
                        expressionType = New ArrayLiteralTypeSymbol(arrayLiteral)
                        typeList.AddType(expressionType, RequiredConversion.Any, expression)
                    End If

                ElseIf expressionType IsNot Nothing AndAlso Not expressionType.IsVoidType() AndAlso
                            Not (expressionType.IsArrayType() AndAlso DirectCast(expressionType, ArrayTypeSymbol).ElementType.IsVoidType()) Then

                    typeList.AddType(expressionType, RequiredConversion.Any, expression)

                    If expressionType.IsRestrictedType() Then
                        ' this element is a restricted type; not convertible to object
                        allConvertibleToObject = False
                    End If
                Else
                    ' What else?
                    Debug.Assert(expressionType Is Nothing)

                    If Not expression.IsNothingLiteral Then
                        ' NOTE: Some expressions without type are still convertible to System.Object, example: Nothing literal
                        allConvertibleToObject = False
                    End If

                    ' this will pick up AddressOf expressions.
                End If
            Next

            ' Here we calculate the dominant type.
            ' Note: if there were no candidate types in the list, this will fail with errorReason = NoneBest.
            errorReasons = InferenceErrorReasons.Other
            Dim results = ArrayBuilder(Of DominantTypeData).GetInstance()
            Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)
            typeList.FindDominantType(results, errorReasons, useSiteInfo)

            If diagnostics.Add(syntax, useSiteInfo) Then
                ' Suppress additional diagnostics
                diagnostics = BindingDiagnosticBag.Discarded
            End If

            Dim dominantType As TypeSymbol

            If results.Count = 1 AndAlso errorReasons = InferenceErrorReasons.Other Then
                ' main case: we succeeded in finding a dominant type
                Debug.Assert(Not results(0).ResultType.IsVoidType(), "internal logic error: how could void have won the dominant type algorithm?")
                numCandidates = 1
                dominantType = results(0).ResultType

            ElseIf count = countOfEmptyArrays AndAlso count > 0 Then
                ' special case: the dominant type of a list of empty arrays is Object(), not Object.
                Debug.Assert(anEmptyArray IsNot Nothing, "internal logic error: if we got at least one empty array, then AnEmptyArray should not be null")
                numCandidates = 1
                ' Use the inferred Object() from the array literal.  ReclassifyArrayLiteral depends on the array identity for correct error reporting
                ' of inference errors.
                dominantType = anEmptyArray.InferredType
                Debug.Assert(dominantType.IsArrayType AndAlso DirectCast(dominantType, ArrayTypeSymbol).Rank = 1 AndAlso DirectCast(dominantType, ArrayTypeSymbol).ElementType.SpecialType = SpecialType.System_Object)

            ElseIf allConvertibleToObject AndAlso (errorReasons And InferenceErrorReasons.Ambiguous) <> 0 Then
                ' special case: there were multiple dominant types, so we fall back to Object
                Debug.Assert(results.Count > 1, "internal logic error: if InferenceErrorReasonsAmbiguous, you'd have expected more than 1 candidate")
                numCandidates = results.Count
                dominantType = GetSpecialType(SpecialType.System_Object, syntax, diagnostics)

            ElseIf allConvertibleToObject Then
                ' fallback case: we didn't find a dominant type, but can fall back to Object
                numCandidates = 0
                dominantType = GetSpecialType(SpecialType.System_Object, syntax, diagnostics)

            Else
                numCandidates = 0
                dominantType = Nothing
            End If

            ' Ensure that ArrayLiteralType is not returned from the dominant type algorithm
            Dim arrayLiteralType = TryCast(dominantType, ArrayLiteralTypeSymbol)
            If arrayLiteralType IsNot Nothing Then
                dominantType = arrayLiteralType.ArrayLiteral.InferredType
            End If

            results.Free()
            Return dominantType
        End Function

        Public Function IsInAsyncContext() As Boolean
            Return ContainingMember.Kind = SymbolKind.Method AndAlso DirectCast(ContainingMember, MethodSymbol).IsAsync
        End Function

        Public Function IsInIteratorContext() As Boolean
            Return ContainingMember.Kind = SymbolKind.Method AndAlso DirectCast(ContainingMember, MethodSymbol).IsIterator
        End Function

        Private Function BindAwait(
            node As AwaitExpressionSyntax,
            diagnostics As BindingDiagnosticBag,
            Optional bindAsStatement As Boolean = False
        ) As BoundExpression

            If IsInQuery Then
                ReportDiagnostic(diagnostics, node.AwaitKeyword, ERRID.ERR_BadAsyncInQuery)
            ElseIf Not IsInAsyncContext() Then
                ReportDiagnostic(diagnostics, node.AwaitKeyword, GetAwaitInNonAsyncError())
            End If

            Dim operand As BoundExpression = BindExpression(node.Expression, diagnostics)

            Return BindAwait(node, operand, diagnostics, bindAsStatement)
        End Function

        Private Function BindAwait(
            node As VisualBasicSyntaxNode,
            operand As BoundExpression,
            diagnostics As BindingDiagnosticBag,
            bindAsStatement As Boolean
        ) As BoundExpression

            ' If the user tries to do "await f()" or "await expr.f()" where f is an async sub,
            ' then we'll give a more helpful error message...
            If Not operand.HasErrors AndAlso
               operand.Type IsNot Nothing AndAlso
               operand.Type.IsVoidType() AndAlso
               operand.Kind = BoundKind.Call Then
                Dim method As MethodSymbol = DirectCast(operand, BoundCall).Method

                If method.IsSub AndAlso method.IsAsync Then
                    ReportDiagnostic(diagnostics, operand.Syntax, ERRID.ERR_CantAwaitAsyncSub1, method.Name)
                    Return BadExpression(node, operand, ErrorTypeSymbol.UnknownResultType)
                End If
            End If

            operand = MakeRValue(operand, diagnostics)

            If operand.IsNothingLiteral() Then
                ReportDiagnostic(diagnostics, node, ERRID.ERR_BadAwaitNothing)
                Return BadExpression(node, operand, ErrorTypeSymbol.UnknownResultType)
            ElseIf operand.Type.IsObjectType() Then
                ' Late-bound pattern.
                If OptionStrict = OptionStrict.On Then
                    ReportDiagnostic(diagnostics, node, ERRID.ERR_StrictDisallowsLateBinding)
                    Return BadExpression(node, operand, ErrorTypeSymbol.UnknownResultType)
                ElseIf OptionStrict = OptionStrict.Custom Then
                    ReportDiagnostic(diagnostics, node, ERRID.WRN_LateBindingResolution)
                End If
            End If

            If operand.HasErrors Then
                ' Disable error reporting going forward.
                diagnostics = BindingDiagnosticBag.Discarded
            End If

            Dim ignoreDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics:=True, withDependencies:=diagnostics.AccumulatesDependencies)

            ' Will accumulate all ignored diagnostics in case we want to add them
            Dim allIgnoreDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics)

            Dim awaitableInstancePlaceholder = New BoundRValuePlaceholder(operand.Syntax, operand.Type).MakeCompilerGenerated()
            Dim awaiterInstancePlaceholder As BoundLValuePlaceholder = Nothing

            Dim getAwaiter As BoundExpression = Nothing
            Dim isCompleted As BoundExpression = Nothing
            Dim getResult As BoundExpression = Nothing

            If operand.Type.IsObjectType Then
                ' Late-bound pattern.

                getAwaiter = BindLateBoundMemberAccess(node, WellKnownMemberNames.GetAwaiter, Nothing, awaitableInstancePlaceholder, operand.Type,
                                                       ignoreDiagnostics, suppressLateBindingResolutionDiagnostics:=True).MakeCompilerGenerated()
                getAwaiter = DirectCast(getAwaiter, BoundLateMemberAccess).SetAccessKind(LateBoundAccessKind.Get)

                Debug.Assert(getAwaiter.Type.IsObjectType())
                awaiterInstancePlaceholder = New BoundLValuePlaceholder(operand.Syntax, getAwaiter.Type).MakeCompilerGenerated()

                isCompleted = BindLateBoundMemberAccess(node, WellKnownMemberNames.IsCompleted, Nothing, awaiterInstancePlaceholder, awaiterInstancePlaceholder.Type,
                                                       ignoreDiagnostics, suppressLateBindingResolutionDiagnostics:=True).MakeCompilerGenerated()
                isCompleted = DirectCast(isCompleted, BoundLateMemberAccess).SetAccessKind(LateBoundAccessKind.Get)

                Debug.Assert(isCompleted.Type.IsObjectType())

                getResult = BindLateBoundMemberAccess(node, WellKnownMemberNames.GetResult, Nothing, awaiterInstancePlaceholder, awaiterInstancePlaceholder.Type,
                                                       ignoreDiagnostics, suppressLateBindingResolutionDiagnostics:=True).MakeCompilerGenerated()

                Debug.Assert(getResult.Type.IsObjectType())
                getResult = DirectCast(getResult, BoundLateMemberAccess).SetAccessKind(If(bindAsStatement, LateBoundAccessKind.Call, LateBoundAccessKind.Get))

                Debug.Assert(operand.Type.IsErrorType() OrElse ignoreDiagnostics.DiagnosticBag.IsEmptyWithoutResolution())
            Else
                Dim lookupResult As LookupResult = lookupResult.GetInstance()
                Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)

                ' 11.25 Await Operator
                '
                '1.	C contains an accessible instance or extension method named GetAwaiter which has no arguments and which returns some type E;
                LookupMember(lookupResult, awaitableInstancePlaceholder.Type, WellKnownMemberNames.GetAwaiter, 0, LookupOptions.AllMethodsOfAnyArity, useSiteInfo)

                Dim methodGroup As BoundMethodGroup = Nothing

                If lookupResult.Kind = LookupResultKind.Good AndAlso lookupResult.Symbols(0).Kind = SymbolKind.Method Then
                    methodGroup = CreateBoundMethodGroup(
                                node,
                                lookupResult,
                                LookupOptions.Default,
                                ignoreDiagnostics.AccumulatesDependencies,
                                awaitableInstancePlaceholder,
                                Nothing,
                                QualificationKind.QualifiedViaValue).MakeCompilerGenerated()

                    ignoreDiagnostics.Clear()
                    getAwaiter = MakeRValue(BindInvocationExpression(node,
                                                                     operand.Syntax,
                                                                     TypeCharacter.None,
                                                                     methodGroup,
                                                                     ImmutableArray(Of BoundExpression).Empty,
                                                                     Nothing,
                                                                     ignoreDiagnostics,
                                                                     callerInfoOpt:=node).MakeCompilerGenerated(),
                                            ignoreDiagnostics).MakeCompilerGenerated()

                    ' The result we are looking for is:
                    ' 1) a non-latebound call of an instance (extension method is considered instance) method;
                    ' 2) method doesn't have any parameters, optional parameters should be ruled out;
                    ' 3) method is not a Sub;
                    ' 4) result is not Object.
                    If getAwaiter.HasErrors OrElse
                       DiagnosticBagHasErrorsOtherThanObsoleteOnes(ignoreDiagnostics.DiagnosticBag) OrElse
                       getAwaiter.Kind <> BoundKind.Call OrElse
                       getAwaiter.Type.IsObjectType() Then

                        getAwaiter = Nothing
                    Else
                        allIgnoreDiagnostics.AddRange(ignoreDiagnostics)
                        Dim method As MethodSymbol = DirectCast(getAwaiter, BoundCall).Method

                        If method.IsShared OrElse method.ParameterCount <> 0 Then
                            getAwaiter = Nothing
                        End If
                    End If

                    Debug.Assert(getAwaiter Is Nothing OrElse Not DiagnosticBagHasErrorsOtherThanObsoleteOnes(ignoreDiagnostics.DiagnosticBag))
                End If

                If getAwaiter IsNot Nothing AndAlso Not getAwaiter.Type.IsErrorType() Then
                    ' 2.	E contains a readable instance property named IsCompleted which takes no arguments and has type Boolean;
                    awaiterInstancePlaceholder = New BoundLValuePlaceholder(operand.Syntax, getAwaiter.Type).MakeCompilerGenerated()

                    lookupResult.Clear()
                    LookupMember(lookupResult, awaiterInstancePlaceholder.Type, WellKnownMemberNames.IsCompleted, 0,
                                 LookupOptions.AllMethodsOfAnyArity Or LookupOptions.IgnoreExtensionMethods, useSiteInfo)

                    If lookupResult.Kind = LookupResultKind.Good AndAlso lookupResult.Symbols(0).Kind = SymbolKind.Property Then
                        Dim propertyGroup = New BoundPropertyGroup(node,
                                                                   lookupResult.Symbols.ToDowncastedImmutable(Of PropertySymbol),
                                                                   lookupResult.Kind,
                                                                   awaiterInstancePlaceholder,
                                                                   QualificationKind.QualifiedViaValue).MakeCompilerGenerated()

                        ignoreDiagnostics.Clear()
                        isCompleted = MakeRValue(BindInvocationExpression(node,
                                                                          operand.Syntax,
                                                                          TypeCharacter.None,
                                                                          propertyGroup,
                                                                          ImmutableArray(Of BoundExpression).Empty,
                                                                          Nothing,
                                                                          ignoreDiagnostics,
                                                                          callerInfoOpt:=node).MakeCompilerGenerated(),
                                                 ignoreDiagnostics).MakeCompilerGenerated()

                        ' The result we are looking for is:
                        ' 1) a non-latebound get of an instance property;
                        ' 2) property doesn't have any parameters, optional parameters should be ruled out;
                        ' 3) result is Boolean.
                        If isCompleted.HasErrors OrElse
                           DiagnosticBagHasErrorsOtherThanObsoleteOnes(ignoreDiagnostics.DiagnosticBag) OrElse
                           isCompleted.Kind <> BoundKind.PropertyAccess OrElse
                           Not isCompleted.Type.IsBooleanType() Then

                            isCompleted = Nothing
                        Else
                            allIgnoreDiagnostics.AddRange(ignoreDiagnostics)
                            Debug.Assert(DirectCast(isCompleted, BoundPropertyAccess).AccessKind = PropertyAccessKind.Get)
                            Dim prop As PropertySymbol = DirectCast(isCompleted, BoundPropertyAccess).PropertySymbol

                            If prop.IsShared OrElse prop.ParameterCount <> 0 Then
                                isCompleted = Nothing
                            End If
                        End If

                        Debug.Assert(isCompleted Is Nothing OrElse Not DiagnosticBagHasErrorsOtherThanObsoleteOnes(ignoreDiagnostics.DiagnosticBag))
                    End If

                    ' 3.	E contains an accessible instance method named GetResult which takes no arguments;
                    lookupResult.Clear()
                    LookupMember(lookupResult, awaiterInstancePlaceholder.Type, WellKnownMemberNames.GetResult, 0,
                                 LookupOptions.AllMethodsOfAnyArity Or LookupOptions.IgnoreExtensionMethods, useSiteInfo)

                    If lookupResult.Kind = LookupResultKind.Good AndAlso lookupResult.Symbols(0).Kind = SymbolKind.Method Then
                        methodGroup = CreateBoundMethodGroup(
                                    node,
                                    lookupResult,
                                    LookupOptions.Default,
                                    ignoreDiagnostics.AccumulatesDependencies,
                                    awaiterInstancePlaceholder,
                                    Nothing,
                                    QualificationKind.QualifiedViaValue).MakeCompilerGenerated()

                        ignoreDiagnostics.Clear()
                        getResult = BindInvocationExpression(node,
                                                             operand.Syntax,
                                                             TypeCharacter.None,
                                                             methodGroup,
                                                             ImmutableArray(Of BoundExpression).Empty,
                                                             Nothing,
                                                             ignoreDiagnostics,
                                                             callerInfoOpt:=node).MakeCompilerGenerated()

                        ' The result we are looking for is:
                        ' 1) a non-latebound call of an instance (extension methods ignored) method;
                        ' 2) method doesn't have any parameters, optional parameters should be ruled out;
                        If getResult.HasErrors OrElse
                           DiagnosticBagHasErrorsOtherThanObsoleteOnes(ignoreDiagnostics.DiagnosticBag) OrElse
                           getResult.Kind <> BoundKind.Call Then

                            getResult = Nothing
                        Else
                            allIgnoreDiagnostics.AddRange(ignoreDiagnostics)
                            Dim method As MethodSymbol = DirectCast(getResult, BoundCall).Method
                            Debug.Assert(Not method.IsReducedExtensionMethod)

                            If method.IsShared OrElse method.ParameterCount <> 0 OrElse
                               (method.IsSub AndAlso method.IsConditional) Then
                                getResult = Nothing
                            End If
                        End If

                        Debug.Assert(getResult Is Nothing OrElse Not DiagnosticBagHasErrorsOtherThanObsoleteOnes(ignoreDiagnostics.DiagnosticBag))
                    End If

                    ' 4.	E implements either System.Runtime.CompilerServices.INotifyCompletion or ICriticalNotifyCompletion.
                    Dim notifyCompletion As NamedTypeSymbol = GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_INotifyCompletion, node, diagnostics)

                    ' ICriticalNotifyCompletion inherits from INotifyCompletion, so a check for INotifyCompletion is sufficient.
                    If Not notifyCompletion.IsErrorType() AndAlso
                       Not Conversions.IsWideningConversion(Conversions.ClassifyDirectCastConversion(getAwaiter.Type, notifyCompletion, useSiteInfo)) Then
                        ReportDiagnostic(diagnostics, node, ERRID.ERR_DoesntImplementAwaitInterface2, getAwaiter.Type, notifyCompletion)
                    End If
                End If

                diagnostics.Add(node, useSiteInfo)
                lookupResult.Free()
            End If

            Dim hasErrors As Boolean = False

            If getAwaiter Is Nothing Then
                hasErrors = True
                ReportDiagnostic(diagnostics, node, ERRID.ERR_BadGetAwaiterMethod1, operand.Type)

                Debug.Assert(isCompleted Is Nothing AndAlso getResult Is Nothing)
                getAwaiter = BadExpression(node, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
            ElseIf getAwaiter.Type.IsErrorType() Then
                hasErrors = True
            ElseIf isCompleted Is Nothing OrElse getResult Is Nothing Then
                hasErrors = True
                ReportDiagnostic(diagnostics, node, ERRID.ERR_BadIsCompletedOnCompletedGetResult2, getAwaiter.Type, operand.Type)
            End If

            If awaiterInstancePlaceholder Is Nothing Then
                Debug.Assert(hasErrors)
                awaiterInstancePlaceholder = New BoundLValuePlaceholder(node, getAwaiter.Type).MakeCompilerGenerated()
            End If

            If isCompleted Is Nothing Then
                isCompleted = BadExpression(node, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
            End If

            If getResult Is Nothing Then
                getResult = BadExpression(node, ErrorTypeSymbol.UnknownResultType).MakeCompilerGenerated()
            End If

            If Not hasErrors Then
                diagnostics.AddRange(allIgnoreDiagnostics)
            End If
            allIgnoreDiagnostics.Free()
            ignoreDiagnostics.Free()

            Return New BoundAwaitOperator(node, operand,
                                          awaitableInstancePlaceholder, getAwaiter,
                                          awaiterInstancePlaceholder, isCompleted, getResult,
                                          type:=getResult.Type, hasErrors)
        End Function

        Private Shared Function DiagnosticBagHasErrorsOtherThanObsoleteOnes(bag As DiagnosticBag) As Boolean
            If bag.IsEmptyWithoutResolution Then
                Return False
            End If

            For Each diag In bag.AsEnumerable()
                If diag.Severity = DiagnosticSeverity.Error Then
                    Select Case diag.Code
                        Case ERRID.ERR_UseOfObsoletePropertyAccessor2,
                             ERRID.ERR_UseOfObsoletePropertyAccessor3,
                             ERRID.ERR_UseOfObsoleteSymbolNoMessage1,
                             ERRID.ERR_UseOfObsoleteSymbol2
                            ' ignore

                        Case Else
                            Return True
                    End Select

                End If
            Next
            Return False
        End Function

        Private Function GetAwaitInNonAsyncError() As DiagnosticInfo
            If Me.IsInLambda Then
                Return ErrorFactory.ErrorInfo(ERRID.ERR_BadAwaitInNonAsyncLambda)
            ElseIf ContainingMember.Kind = SymbolKind.Method Then
                Dim method = DirectCast(ContainingMember, MethodSymbol)

                If method.IsSub Then
                    Return ErrorFactory.ErrorInfo(ERRID.ERR_BadAwaitInNonAsyncVoidMethod)
                Else
                    Return ErrorFactory.ErrorInfo(ERRID.ERR_BadAwaitInNonAsyncMethod, method.ReturnType)
                End If
            End If

            Return ErrorFactory.ErrorInfo(ERRID.ERR_BadAwaitNotInAsyncMethodOrLambda)
        End Function

    End Class
End Namespace
