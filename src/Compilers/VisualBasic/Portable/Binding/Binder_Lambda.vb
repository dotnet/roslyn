' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class Binder

        Private Function BindLambdaExpression(
             node As LambdaExpressionSyntax,
             diagnostics As DiagnosticBag
         ) As BoundExpression

            Const asyncIterator As SourceMemberFlags = SourceMemberFlags.Async Or SourceMemberFlags.Iterator

            ' Decode the modifiers.
            Dim modifiers As SourceMemberFlags = DecodeModifiers(node.SubOrFunctionHeader.Modifiers, asyncIterator, ERRID.ERR_InvalidLambdaModifier, Accessibility.Public, diagnostics).FoundFlags And asyncIterator

            If (modifiers And asyncIterator) = asyncIterator Then
                ReportModifierError(node.SubOrFunctionHeader.Modifiers, ERRID.ERR_InvalidAsyncIteratorModifiers, diagnostics, InvalidAsyncIterator)
            End If

            Dim parameters As ImmutableArray(Of ParameterSymbol)
            parameters = DecodeParameterList(Me.ContainingMember, True, modifiers, node.SubOrFunctionHeader.ParameterList, diagnostics)

            For Each param In parameters
                ' Look up in container binders for name clashes with other locals and parameters.
                Dim identifierSyntax As SyntaxNodeOrToken = DirectCast(param, UnboundLambdaParameterSymbol).IdentifierSyntax
                Me.VerifyNameShadowingInMethodBody(param, identifierSyntax, identifierSyntax, diagnostics)
            Next

            Dim returnType As TypeSymbol = Nothing
            Dim hasErrors As Boolean = False

            If node.Kind = SyntaxKind.MultiLineFunctionLambdaExpression AndAlso
               node.SubOrFunctionHeader.AsClause IsNot Nothing Then
                returnType = BindTypeSyntax(node.SubOrFunctionHeader.AsClause.Type, diagnostics)

                If returnType.IsRestrictedType() Then
                    ReportDiagnostic(diagnostics, node.SubOrFunctionHeader.AsClause.Type, ERRID.ERR_RestrictedType1, returnType)
                    hasErrors = True

                ElseIf Not returnType.IsErrorType() Then
                    If Not (modifiers And asyncIterator) = asyncIterator Then

                        If modifiers = SourceMemberFlags.Async AndAlso
                           Not returnType.OriginalDefinition.Equals(Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T)) AndAlso
                           Not returnType.Equals(Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task)) Then
                            ReportDiagnostic(diagnostics, node.SubOrFunctionHeader.AsClause.Type, ERRID.ERR_BadAsyncReturn)
                        End If

                        If modifiers = SourceMemberFlags.Iterator Then
                            Dim originalRetTypeDef = returnType.OriginalDefinition
                            If originalRetTypeDef.SpecialType <> SpecialType.System_Collections_Generic_IEnumerable_T AndAlso
                                originalRetTypeDef.SpecialType <> SpecialType.System_Collections_Generic_IEnumerator_T AndAlso
                                returnType.SpecialType <> SpecialType.System_Collections_IEnumerable AndAlso
                                returnType.SpecialType <> SpecialType.System_Collections_IEnumerator Then
                                ReportDiagnostic(diagnostics, node.SubOrFunctionHeader.AsClause.Type, ERRID.ERR_BadIteratorReturn)
                            End If
                        End If
                    End If
                End If
            ElseIf node.Kind = SyntaxKind.MultiLineSubLambdaExpression OrElse
                node.Kind = SyntaxKind.SingleLineSubLambdaExpression Then

                returnType = GetSpecialType(SpecialType.System_Void, node.SubOrFunctionHeader, diagnostics)

                If modifiers = SourceMemberFlags.Iterator Then
                    ReportDiagnostic(diagnostics, node.SubOrFunctionHeader.DeclarationKeyword, ERRID.ERR_BadIteratorReturn)
                End If
            End If

            Return New UnboundLambda(node, Me, modifiers, parameters, returnType, New UnboundLambda.UnboundLambdaBindingCache(), hasErrors)
        End Function


        Friend Function BuildBoundLambdaParameters(
            source As UnboundLambda,
            targetSignature As UnboundLambda.TargetSignature,
            diagnostics As DiagnosticBag
        ) As ImmutableArray(Of BoundLambdaParameterSymbol)

            If source.Parameters.Length = 0 Then
                Return ImmutableArray(Of BoundLambdaParameterSymbol).Empty
            End If

            Dim unboundParams As ImmutableArray(Of ParameterSymbol) = source.Parameters
            Dim parameters(unboundParams.Length - 1) As BoundLambdaParameterSymbol
            Dim minCount As Integer = Math.Min(parameters.Length, targetSignature.ParameterTypes.Length)

            For i As Integer = 0 To minCount - 1 Step 1
                Dim unboundParam = DirectCast(unboundParams(i), UnboundLambdaParameterSymbol)

                Dim unboundType As TypeSymbol = unboundParam.Type
                Dim delegateType As TypeSymbol = targetSignature.ParameterTypes(i)

                If unboundType Is Nothing Then
                    ' Get the type from the target.
                    unboundType = delegateType

                    If Not unboundParam.IsByRef AndAlso source.Flags <> 0 AndAlso unboundType.IsRestrictedType Then
                        ReportDiagnostic(diagnostics, unboundParam.IdentifierSyntax, ERRID.ERR_RestrictedResumableType1, unboundType)
                    Else
                        ' Other cases with restricted types are not interesting because either the target delegate type is "bad"
                        ' due to the way restricted type is used in its signature, or there is a ByRef/ByVal mismatch for the parameter.
                    End If
                End If

                parameters(i) = New BoundLambdaParameterSymbol(unboundParam.Name,
                                                               unboundParam.Ordinal,
                                                               unboundType,
                                                               unboundParam.IsByRef,
                                                               unboundParam.Syntax,
                                                               unboundParam.Locations(0))
            Next

            If parameters.Length <> targetSignature.ParameterTypes.Length Then
                ' Create the rest of the parameters
                Dim objectType As TypeSymbol = Nothing

                For i As Integer = minCount To parameters.Length - 1 Step 1
                    Dim unboundParam = DirectCast(unboundParams(i), UnboundLambdaParameterSymbol)
                    Dim unboundType As TypeSymbol = unboundParam.Type

                    If unboundType Is Nothing Then
                        If objectType Is Nothing Then
                            objectType = GetSpecialType(SpecialType.System_Object, unboundParam.IdentifierSyntax, diagnostics)
                        End If

                        unboundType = objectType
                        ReportLambdaParameterInferredToBeObject(unboundParam, diagnostics)
                    End If

                    parameters(i) = New BoundLambdaParameterSymbol(unboundParam.Name,
                                                                   unboundParam.Ordinal,
                                                                   unboundType,
                                                                   unboundParam.IsByRef,
                                                                   unboundParam.Syntax,
                                                                   unboundParam.Locations(0))
                Next
            End If

            Return parameters.AsImmutableOrNull()
        End Function


        Friend Function BindUnboundLambda(source As UnboundLambda, target As UnboundLambda.TargetSignature) As BoundLambda
            Debug.Assert(Me Is source.Binder)

            Dim maxRelaxationLevel As ConversionKind = ConversionKind.DelegateRelaxationLevelNone
            Dim diagnostics As DiagnosticBag = DiagnosticBag.GetInstance()
            Dim targetReturnType As TypeSymbol

            If source.ReturnType IsNot Nothing Then
                targetReturnType = source.ReturnType

                If source.IsFunctionLambda Then
                    If targetReturnType.IsVoidType() Then
                        targetReturnType = Microsoft.CodeAnalysis.VisualBasic.Symbols.LambdaSymbol.ReturnTypeVoidReplacement
                    End If
                End If
            Else
                Debug.Assert(source.IsFunctionLambda)

                ' For Async | Iterator lambdas with nongeneric target delegate return type, we still want to do return type inference
                ' in order to infer more specific return type Task(Of T) | IEnumerable(of T), if possible.
                If target.ReturnType.SpecialType <> SpecialType.System_Void AndAlso
                   Not ((source.Flags And SourceMemberFlags.Async) <> 0 AndAlso target.ReturnType Is Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task)) AndAlso
                   Not ((source.Flags And SourceMemberFlags.Iterator) <> 0 AndAlso (target.ReturnType.SpecialType = SpecialType.System_Collections_IEnumerable OrElse
                                                                                    target.ReturnType.SpecialType = SpecialType.System_Collections_IEnumerator)) Then

                    targetReturnType = target.ReturnType

                    If Not targetReturnType.IsErrorType() Then
                        If source.Flags = SourceMemberFlags.Async Then
                            If Not TypeSymbol.Equals(targetReturnType.OriginalDefinition, Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T), TypeCompareKind.ConsiderEverything) Then
                                ReportDiagnostic(diagnostics, LambdaHeaderErrorNode(source), ERRID.ERR_BadAsyncReturn)
                            End If
                        End If

                        If source.Flags = SourceMemberFlags.Iterator Then
                            Dim origTargetReturnType = targetReturnType.OriginalDefinition
                            If origTargetReturnType.SpecialType <> SpecialType.System_Collections_Generic_IEnumerable_T AndAlso
                                origTargetReturnType.SpecialType <> SpecialType.System_Collections_Generic_IEnumerator_T Then
                                ReportDiagnostic(diagnostics, LambdaHeaderErrorNode(source), ERRID.ERR_BadIteratorReturn)
                            End If
                        End If
                    End If

                Else
                    ' Target signature is a Sub, but lambda is a function; or target return type is Task and lambda is Async.
                    ' Have to infer the type
                    Debug.Assert(target.ReturnType.IsVoidType() OrElse
                                 ((source.Flags And SourceMemberFlags.Async) <> 0 AndAlso target.ReturnType.Equals(Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task))))

                    Dim targetForInference As UnboundLambda.TargetSignature = target

                    If Not targetForInference.ReturnType.IsVoidType() Then
                        targetForInference = New UnboundLambda.TargetSignature(targetForInference.ParameterTypes, targetForInference.ParameterIsByRef,
                                                                               Compilation.GetSpecialType(SpecialType.System_Void), ' No need to report use-site error.
                                                                               returnsByRef:=False)
                    End If

                    Dim typeInfo As KeyValuePair(Of TypeSymbol, ImmutableArray(Of Diagnostic)) = source.InferReturnType(targetForInference)

                    targetReturnType = typeInfo.Key
                    If Not typeInfo.Value.IsEmpty Then
                        diagnostics.AddRange(typeInfo.Value)
                    End If
                End If
            End If

            ' Create parameters
            Dim parameters As ImmutableArray(Of BoundLambdaParameterSymbol) = BuildBoundLambdaParameters(source, target, diagnostics)
            Dim lambdaSymbol As New SourceLambdaSymbol(source.Syntax, source, parameters, targetReturnType, Me)

            Dim delegateRelaxation As ConversionKind = Nothing
            Dim lambdaBinder As LambdaBodyBinder = Nothing

            Dim block As BoundBlock = BindLambdaBody(lambdaSymbol, diagnostics, lambdaBinder)

            If block.HasErrors OrElse diagnostics.HasAnyErrors() Then
                delegateRelaxation = ConversionKind.DelegateRelaxationLevelInvalid 'No conversion
            Else
                ' Add information about delegate relaxation level across all Return statements within the lambda body.
                If lambdaSymbol.IsSub Then
                    delegateRelaxation = ConversionKind.DelegateRelaxationLevelNone
                Else
                    Dim seenReturnWithAValue As Boolean = False
                    Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                    delegateRelaxation = LambdaRelaxationVisitor.DetermineDelegateRelaxationLevel(lambdaSymbol, source.Flags = SourceMemberFlags.Iterator, block, seenReturnWithAValue, useSiteDiagnostics)

                    diagnostics.Add(LambdaHeaderErrorNode(source), useSiteDiagnostics)

                    ' Dev11#94373: we also need to track whether there were any returns with operands, since
                    ' turning "Async Function() : End Function" into a Func(Of Task(Of Integer)) is a delegate
                    ' relaxation.
                    If Not seenReturnWithAValue AndAlso lambdaSymbol.IsAsync AndAlso delegateRelaxation < ConversionKind.DelegateRelaxationLevelWideningDropReturnOrArgs AndAlso
                       lambdaSymbol.ReturnType.OriginalDefinition.Equals(Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T)) Then
                        delegateRelaxation = ConversionKind.DelegateRelaxationLevelWideningDropReturnOrArgs
                    End If
                End If
            End If

            ' See if we need to relax the lambda
            Dim methodConversions As MethodConversionKind = MethodConversionKind.Error_Unspecified

            If delegateRelaxation <> ConversionKind.DelegateRelaxationLevelInvalid Then
                ' Figure out conversion kind.
                Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                methodConversions = Conversions.ClassifyMethodConversionForLambdaOrAnonymousDelegate(target, lambdaSymbol, useSiteDiagnostics)

                If diagnostics.Add(LambdaHeaderErrorNode(source), useSiteDiagnostics) Then
                    ' Suppress additional diagnostics
                    diagnostics = New DiagnosticBag()
                End If

                If Conversions.IsDelegateRelaxationSupportedFor(methodConversions) Then
                    If Conversions.IsStubRequiredForMethodConversion(methodConversions) Then
                        ' We will need a stub for this lambda, which means that we will need to instantiate
                        ' an Anonymous Delegate matching the signature of the lambdaSymbol.
                        ' Anonymous Delegate has some limitations on parameter and return types.
                        For Each param In lambdaSymbol.Parameters
                            ' Verify for restricted types.
                            Dim restrictedType As TypeSymbol = Nothing
                            If param.Type.IsRestrictedTypeOrArrayType(restrictedType) Then
                                ReportDiagnostic(diagnostics,
                                                 DirectCast(source.Parameters(param.Ordinal), UnboundLambdaParameterSymbol).TypeSyntax,
                                                 ERRID.ERR_RestrictedType1, restrictedType)
                                delegateRelaxation = ConversionKind.DelegateRelaxationLevelInvalid 'No conversion
                                methodConversions = methodConversions Or MethodConversionKind.Error_RestrictedType
                                Exit For
                            End If
                        Next

                        If delegateRelaxation <> ConversionKind.DelegateRelaxationLevelInvalid Then
                            Debug.Assert(targetReturnType Is lambdaSymbol.ReturnType)

                            ' Check return type as well, but complain only if we "inherited" it from the target signature. If we got
                            ' it from the lambda's signature or inferred it, we already complained about it.
                            Dim restrictedType As TypeSymbol = Nothing
                            If targetReturnType.IsRestrictedTypeOrArrayType(restrictedType) Then
                                delegateRelaxation = ConversionKind.DelegateRelaxationLevelInvalid 'No conversion
                                methodConversions = methodConversions Or MethodConversionKind.Error_RestrictedType

                                If source.ReturnType Is Nothing AndAlso target.ReturnType.SpecialType <> SpecialType.System_Void Then
                                    ReportDiagnostic(diagnostics, LambdaHeaderErrorNode(source), ERRID.ERR_RestrictedType1, restrictedType)
                                End If
                            End If
                        End If
                    End If
                End If

                delegateRelaxation = CType(Math.Max(delegateRelaxation, Conversions.DetermineDelegateRelaxationLevel(methodConversions)), ConversionKind)
            End If

            ' the control flow of lambda expression bodies have not yet been analyzed. This will report unreachable code, ...
            ControlFlowPass.Analyze(New FlowAnalysisInfo(Compilation, lambdaSymbol, block), diagnostics, True)

            Dim hasAnyErrors = diagnostics.HasAnyErrors()

            ' handle cases where control flow reports errors (such as illegal goto out of Finally)
            ' this will not change the outcome of overload resolution since control flow errors will happen
            ' regardless of substitutions.
            If hasAnyErrors Then
                delegateRelaxation = ConversionKind.DelegateRelaxationLevelInvalid 'No conversion
            End If

            Dim sealedDiagnostics = diagnostics.ToReadOnlyAndFree()

            Return New BoundLambda(source.Syntax, lambdaSymbol, block,
                                   sealedDiagnostics,
                                   lambdaBinder,
                                   delegateRelaxation,
                                   methodConversions,
                                   hasAnyErrors)
        End Function

        Private Class LambdaRelaxationVisitor
            Inherits StatementWalker

            Private ReadOnly _lambdaSymbol As LambdaSymbol
            Private ReadOnly _isIterator As Boolean
            Private _delegateRelaxationLevel As ConversionKind = ConversionKind.DelegateRelaxationLevelNone
            Private _seenReturnWithAValue As Boolean
            Private _useSiteDiagnostics As HashSet(Of DiagnosticInfo)

            Private Sub New(lambdaSymbol As LambdaSymbol, isIterator As Boolean)
                _lambdaSymbol = lambdaSymbol
                _isIterator = isIterator
            End Sub

            Public Shared Function DetermineDelegateRelaxationLevel(
                lambdaSymbol As LambdaSymbol,
                isIterator As Boolean,
                lambdaBlock As BoundBlock,
                <Out()> ByRef seenReturnWithAValue As Boolean,
                <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            ) As ConversionKind
                Dim visitor As New LambdaRelaxationVisitor(lambdaSymbol, isIterator)
                visitor._useSiteDiagnostics = useSiteDiagnostics
                visitor.VisitBlock(lambdaBlock)
                seenReturnWithAValue = visitor._seenReturnWithAValue
                useSiteDiagnostics = visitor._useSiteDiagnostics
                Return visitor._delegateRelaxationLevel
            End Function

            Public Overrides Function Visit(node As BoundNode) As BoundNode
                'Do not visit expressions
                If node Is Nothing OrElse TypeOf node Is BoundExpression Then
                    Return Nothing
                End If

                Return MyBase.Visit(node)
            End Function

            Public Overrides Function VisitLambda(node As BoundLambda) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public Overrides Function VisitReturnStatement(node As BoundReturnStatement) As BoundNode
                ' not interested in Returns in an iterator.
                If _isIterator Then
                    Return Nothing
                End If

                ' Ignore implicit return at the end of the body.
                If node.ExpressionOpt IsNot Nothing Then

                    If node.ExpressionOpt.Kind = BoundKind.Local Then
                        Dim local As LocalSymbol = DirectCast(node.ExpressionOpt, BoundLocal).LocalSymbol

                        If local.IsFunctionValue AndAlso local.ContainingSymbol Is _lambdaSymbol Then
                            Return Nothing
                        End If
                    End If

                    _seenReturnWithAValue = True
                End If

                Dim returnRelaxation As ConversionKind = Conversions.DetermineDelegateRelaxationLevelForLambdaReturn(node.ExpressionOpt, _useSiteDiagnostics)

                If returnRelaxation > _delegateRelaxationLevel Then
                    _delegateRelaxationLevel = returnRelaxation
                End If

                Return Nothing
            End Function

            Public Overrides Function VisitYieldStatement(node As BoundYieldStatement) As BoundNode
                If _isIterator Then
                    Dim returnRelaxation As ConversionKind = Conversions.DetermineDelegateRelaxationLevelForLambdaReturn(node.Expression, _useSiteDiagnostics)

                    If returnRelaxation > _delegateRelaxationLevel Then
                        _delegateRelaxationLevel = returnRelaxation
                    End If
                End If

                Return Nothing
            End Function

        End Class


        Private Function BindLambdaBody(
            lambdaSymbol As LambdaSymbol,
            diagnostics As DiagnosticBag,
            ByRef lambdaBinder As LambdaBodyBinder
        ) As BoundBlock
            Dim implicitVariablesBinder As Binder = Nothing

            If ContainingBinder.OptionExplicit = False AndAlso Not ContainingBinder.ImplicitVariableDeclarationAllowed Then
                ' Option Explicit is Off, but we're not in a location than allows implicit variable declaration (i.e., a field initializer).
                ' Install an implicit variables binder to capture implicit variables in this lambda.
                implicitVariablesBinder = New ImplicitVariableBinder(Me, lambdaSymbol)
                lambdaBinder = New LambdaBodyBinder(lambdaSymbol, implicitVariablesBinder)
            Else
                lambdaBinder = New LambdaBodyBinder(lambdaSymbol, Me)
            End If

#If DEBUG Then
            lambdaBinder.EnableSimpleNameBindingOrderChecks(True)
#End If
            Dim lambdaSyntax = lambdaSymbol.Syntax
            Dim bodyBinder = lambdaBinder.GetBinder(lambdaSyntax)
            Dim endSyntax As SyntaxNode = lambdaSyntax

            Dim block As BoundBlock

            Select Case lambdaSyntax.Kind
                Case SyntaxKind.SingleLineFunctionLambdaExpression
                    Debug.Assert(lambdaSymbol.ReturnType Is Nothing OrElse Not lambdaSymbol.ReturnType.IsVoidType())
                    Dim expression As BoundExpression = bodyBinder.BindValue(
                                                            DirectCast(
                                                                DirectCast(lambdaSyntax, SingleLineLambdaExpressionSyntax).Body,
                                                                ExpressionSyntax),
                                                            diagnostics)

                    If lambdaSymbol.ReturnType IsNot LambdaSymbol.ReturnTypeIsBeingInferred Then
                        If lambdaSymbol.ReturnType IsNot LambdaSymbol.ReturnTypeIsUnknown Then

                            Dim retType As TypeSymbol = lambdaSymbol.ReturnType

                            If lambdaSymbol.IsAsync Then
                                If retType.OriginalDefinition.Equals(Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T)) Then
                                    retType = DirectCast(retType, NamedTypeSymbol).TypeArgumentsNoUseSiteDiagnostics(0)
                                Else
                                    ' We should have forced Task(Of T) inference.
                                    Debug.Assert(Not retType.Equals(Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task)))
                                End If
                            End If

                            expression = bodyBinder.ApplyImplicitConversion(expression.Syntax, retType, expression, diagnostics, False)
                        Else
                            expression = bodyBinder.MakeRValueAndIgnoreDiagnostics(expression)
                        End If
                    End If

                    Dim boundReturn = New BoundReturnStatement(expression.Syntax,
                                                               expression,
                                                               bodyBinder.GetLocalForFunctionValue(),
                                                               bodyBinder.GetReturnLabel(),
                                                               expression.HasErrors)
                    boundReturn.SetWasCompilerGenerated()

                    block = New BoundBlock(lambdaSyntax, Nothing, ImmutableArray(Of LocalSymbol).Empty,
                                           ImmutableArray.Create(Of BoundStatement)(boundReturn),
                                           expression.HasErrors).MakeCompilerGenerated()

                Case SyntaxKind.SingleLineSubLambdaExpression
                    Debug.Assert(lambdaSymbol.ReturnType IsNot Nothing AndAlso lambdaSymbol.ReturnType.IsVoidType())

                    Dim singleLineLambdaSyntax = DirectCast(lambdaSyntax, SingleLineLambdaExpressionSyntax)
                    Dim statement = DirectCast(singleLineLambdaSyntax.Body, StatementSyntax)

                    If statement.Kind = SyntaxKind.LocalDeclarationStatement Then
                        ' A local declaration is not allowed in a single line lambda as the top level statement.  Report the error here because it is legal
                        ' to have a single line if which contains a local declaration.  If the error reporting is done in BindStatement then all local 
                        ' declarations are prohibited.

                        ' Bind local declaration, discard diagnostics
                        Dim ignoredDiagnostics = DiagnosticBag.GetInstance()
                        block = bodyBinder.BindBlock(lambdaSyntax, singleLineLambdaSyntax.Statements, ignoredDiagnostics).MakeCompilerGenerated()
                        ignoredDiagnostics.Free()

                        ' Generate a diagnostic and a bad statement node
                        ReportDiagnostic(diagnostics, statement, ERRID.ERR_SubDisallowsStatement)
                    Else
                        block = bodyBinder.BindBlock(lambdaSyntax, singleLineLambdaSyntax.Statements, diagnostics).MakeCompilerGenerated()
                    End If

                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression

                    Dim blockSyntax = DirectCast(lambdaSyntax, MultiLineLambdaExpressionSyntax)
                    endSyntax = blockSyntax.EndSubOrFunctionStatement

                    block = bodyBinder.BindBlock(lambdaSyntax, blockSyntax.Statements, diagnostics).MakeCompilerGenerated()

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(lambdaSyntax.Kind)
            End Select

            Dim localBuilder = ArrayBuilder(Of LocalSymbol).GetInstance()

            ' TODO: this is adapted from BindMethodBody. It handles adding a return value local if needed and 
            '       a function epilogue for the exit label
            '       for single line lambdas it may make sense to do in-place 
            '
            ' add indirect return sequence
            ' and maybe an indirect result local (if this is a function)
            Dim statements = ArrayBuilder(Of BoundStatement).GetInstance
            statements.AddRange(block.Statements)
            Select Case lambdaSyntax.Kind
                Case SyntaxKind.SingleLineFunctionLambdaExpression,
                    SyntaxKind.MultiLineFunctionLambdaExpression

                    Dim localForFunctionValue = bodyBinder.GetLocalForFunctionValue
                    localBuilder.Add(localForFunctionValue)

                    Dim returnLabel = New BoundLabelStatement(endSyntax, bodyBinder.GetReturnLabel())
                    Dim boundLocal = New BoundLocal(endSyntax, localForFunctionValue, isLValue:=False, type:=localForFunctionValue.Type).MakeCompilerGenerated()
                    Dim returnStmt = New BoundReturnStatement(endSyntax, boundLocal, Nothing, Nothing)

                    If lambdaSyntax.Kind = SyntaxKind.SingleLineFunctionLambdaExpression OrElse endSyntax Is lambdaSyntax Then
                        returnLabel.SetWasCompilerGenerated()
                        returnStmt.SetWasCompilerGenerated()
                    End If

                    statements.Add(returnLabel)
                    statements.Add(returnStmt)

                Case SyntaxKind.SingleLineSubLambdaExpression,
                    SyntaxKind.MultiLineSubLambdaExpression

                    Dim returnLabel = New BoundLabelStatement(endSyntax, bodyBinder.GetReturnLabel())
                    Dim returnStmt = New BoundReturnStatement(endSyntax, Nothing, Nothing, Nothing)

                    If lambdaSyntax.Kind = SyntaxKind.SingleLineSubLambdaExpression OrElse endSyntax Is lambdaSyntax Then
                        returnLabel.SetWasCompilerGenerated()
                        returnStmt.SetWasCompilerGenerated()
                    End If

                    statements.Add(returnLabel)
                    statements.Add(returnStmt)
            End Select

#If DEBUG Then
            lambdaBinder.EnableSimpleNameBindingOrderChecks(False)
#End If

            If implicitVariablesBinder IsNot Nothing Then
                ' Add any implicitly declared variables to the block.
                implicitVariablesBinder.DisallowFurtherImplicitVariableDeclaration(diagnostics)
                localBuilder.AddRange(implicitVariablesBinder.ImplicitlyDeclaredVariables)
            End If

            If Not block.Locals.IsEmpty Then
                localBuilder.AddRange(block.Locals)
            End If

            block = block.Update(block.StatementListSyntax, localBuilder.ToImmutableAndFree(), statements.ToImmutableAndFree)
            block.SetWasCompilerGenerated()

            If lambdaSymbol.IsAsync AndAlso Not CheckAwaitWalker.VisitBlock(bodyBinder, block, diagnostics) AndAlso
               Not block.HasErrors AndAlso Not lambdaSymbol.IsIterator Then
                ReportDiagnostic(diagnostics,
                                 DirectCast(lambdaSyntax, LambdaExpressionSyntax).SubOrFunctionHeader.DeclarationKeyword,
                                 ERRID.WRN_AsyncLacksAwaits)
            End If

            Return block
        End Function

        Private Class CheckAwaitWalker
            Inherits BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator

            Private ReadOnly _binder As Binder
            Private ReadOnly _diagnostics As DiagnosticBag
            Private _isInCatchFinallyOrSyncLock As Boolean
            Private _containsAwait As Boolean

            Private Sub New(binder As Binder, diagnostics As DiagnosticBag)
                _diagnostics = diagnostics
                _binder = binder
            End Sub

            Public Shared Shadows Function VisitBlock(
                binder As Binder,
                block As BoundBlock,
                diagnostics As DiagnosticBag
            ) As Boolean
                Debug.Assert(binder.IsInAsyncContext())

                Try
                    Dim walker As New CheckAwaitWalker(binder, diagnostics)
                    walker.Visit(block)
                    Debug.Assert(Not walker._isInCatchFinallyOrSyncLock)

                    Return walker._containsAwait
                Catch ex As CancelledByStackGuardException
                    ex.AddAnError(diagnostics)
                    Return True
                End Try
            End Function

            Public Overrides Function VisitTryStatement(node As BoundTryStatement) As BoundNode
                Debug.Assert(Not node.WasCompilerGenerated)

                Visit(node.TryBlock)

                Dim save_m_isInCatchFinallyOrSyncLock As Boolean = _isInCatchFinallyOrSyncLock
                _isInCatchFinallyOrSyncLock = True

                VisitList(node.CatchBlocks)
                Visit(node.FinallyBlockOpt)

                _isInCatchFinallyOrSyncLock = save_m_isInCatchFinallyOrSyncLock
                Return Nothing
            End Function

            Public Overrides Function VisitSyncLockStatement(node As BoundSyncLockStatement) As BoundNode
                Debug.Assert(Not node.WasCompilerGenerated)
                Dim save_m_isInCatchFinallyOrSyncLock As Boolean = _isInCatchFinallyOrSyncLock
                _isInCatchFinallyOrSyncLock = True

                MyBase.VisitSyncLockStatement(node)

                _isInCatchFinallyOrSyncLock = save_m_isInCatchFinallyOrSyncLock
                Return Nothing
            End Function

            Public Overrides Function VisitAwaitOperator(node As BoundAwaitOperator) As BoundNode
                Debug.Assert(_binder.IsInAsyncContext())

                _containsAwait = True

                If _isInCatchFinallyOrSyncLock Then
                    ReportDiagnostic(_diagnostics, node.Syntax, ERRID.ERR_BadAwaitInTryHandler)
                End If

                Return MyBase.VisitAwaitOperator(node)
            End Function

            Public Overrides Function VisitLambda(node As BoundLambda) As BoundNode
                ' Do not dive into the lambdas.
                Return Nothing
            End Function
        End Class


        Public Sub ReportLambdaParameterInferredToBeObject(unboundParam As UnboundLambdaParameterSymbol, diagnostics As DiagnosticBag)
            If OptionStrict = OptionStrict.On Then
                ReportDiagnostic(diagnostics, unboundParam.IdentifierSyntax, ERRID.ERR_StrictDisallowImplicitObjectLambda)
            ElseIf OptionStrict = OptionStrict.Custom Then
                ReportDiagnostic(diagnostics, unboundParam.IdentifierSyntax, ERRID.WRN_ObjectAssumedVar1, ErrorFactory.ErrorInfo(ERRID.WRN_MissingAsClauseinVarDecl))
            End If
        End Sub

        ''' <summary>
        ''' If we are inside a lambda in a constructor and are passing ByRef a non-LValue field, which 
        ''' would be an LValue field, if it were referred to in the constructor outside of a lambda, 
        ''' we need to report an error because the operation will result in a simulated pass by
        ''' ref (through a temp, without a copy back), which might be not the intent.
        ''' </summary>
        Public Function Report_ERRID_ReadOnlyInClosure(argument As BoundExpression) As Boolean

            Debug.Assert(Not argument.IsLValue())

            If argument.HasErrors Then
                Return False
            End If

            Dim containingMember As Symbol = Me.ContainingMember
            If containingMember Is Nothing Then
                Return False
            End If


            Dim receiverOpt As BoundExpression
            Dim field As FieldSymbol

            If argument.Kind = BoundKind.PropertyAccess Then
                Dim propAccess = DirectCast(argument, BoundPropertyAccess)
                Dim propSym = TryCast(propAccess.PropertySymbol, SourcePropertySymbol)

                If propSym Is Nothing OrElse Not propSym.IsReadOnly Then
                    Return False
                End If

                field = propSym.AssociatedField
                receiverOpt = propAccess.ReceiverOpt

                If field Is Nothing Then
                    Return False
                End If

            ElseIf argument.Kind = BoundKind.FieldAccess Then
                Dim fieldAccess = DirectCast(argument, BoundFieldAccess)
                receiverOpt = fieldAccess.ReceiverOpt
                field = fieldAccess.FieldSymbol
                receiverOpt = fieldAccess.ReceiverOpt

            Else
                Return False

            End If

            ' The business of figuring out whether a field access should be considered an LValue is rather complicated, 
            ' it is easy to get wrong and Dev10 gets it wrong for this check in many scenarios. What we will do instead, 
            ' we will recalculate LValue status for the field access in context of the binder for the constructor or 
            ' binder for a field or property initializer. 
            ' We will construct some throw-away nodes in the process, but I believe it is a good tradeoff.

            Dim nonLambdaBinder As Binder = Nothing

            If containingMember.IsLambdaMethod Then

                Dim binderForExpressionContainingLambda As Binder = Nothing

                ' Ok, we are inside a lambda. 
                ' Bubble up to the containing method or initializer.
                Do
                    binderForExpressionContainingLambda = DirectCast(containingMember, LambdaSymbol).ContainingBinder
                    containingMember = containingMember.ContainingSymbol
                    Debug.Assert(binderForExpressionContainingLambda.ContainingMember Is containingMember)
                Loop While containingMember IsNot Nothing AndAlso containingMember.IsLambdaMethod

                Dim containingMethodKind As MethodKind = binderForExpressionContainingLambda.KindOfContainingMethodAtRunTime()

                Select Case containingMethodKind
                    Case MethodKind.SharedConstructor, MethodKind.Constructor
                        nonLambdaBinder = binderForExpressionContainingLambda
                End Select
            End If

            If nonLambdaBinder Is Nothing Then
                ' Either we are not in a lambda or not in a constructor.
                Return False
            End If

            If receiverOpt Is Nothing OrElse receiverOpt.Kind <> BoundKind.FieldAccess Then
                ' Simple case only one field access in the chain.
                Return nonLambdaBinder.IsLValueFieldAccess(field, receiverOpt)
            Else
                Dim fields = ArrayBuilder(Of FieldSymbol).GetInstance()

                fields.Add(field)

                Do
                    Dim fieldAccess = DirectCast(receiverOpt, BoundFieldAccess)
                    fields.Add(fieldAccess.FieldSymbol)
                    receiverOpt = fieldAccess.ReceiverOpt
                Loop While receiverOpt IsNot Nothing AndAlso receiverOpt.Kind = BoundKind.FieldAccess

                For i As Integer = fields.Count - 1 To 0 Step -1
                    Dim fieldSymbol As FieldSymbol = fields(i)
                    receiverOpt = New BoundFieldAccess(argument.Syntax, receiverOpt, fieldSymbol,
                                                       nonLambdaBinder.IsLValueFieldAccess(fieldSymbol, receiverOpt),
                                                       type:=fieldSymbol.Type)
                Next

                fields.Free()

                Return receiverOpt.IsLValue()
            End If
        End Function

        Friend Function InferAnonymousDelegateForLambda(source As UnboundLambda) As KeyValuePair(Of NamedTypeSymbol, ImmutableArray(Of Diagnostic))
            Debug.Assert(Me Is source.Binder)

            Dim diagnostics = DiagnosticBag.GetInstance()

            ' Using Void as return type, because BuildBoundLambdaParameters doesn't use it and it is as good as any other value.
            Dim targetSignature As New UnboundLambda.TargetSignature(ImmutableArray(Of ParameterSymbol).Empty, Compilation.GetSpecialType(SpecialType.System_Void), returnsByRef:=False)
            Dim parameters As ImmutableArray(Of BoundLambdaParameterSymbol) = BuildBoundLambdaParameters(source, targetSignature, diagnostics)

            Dim returnTypeInfo As KeyValuePair(Of TypeSymbol, ImmutableArray(Of Diagnostic))
            returnTypeInfo = source.InferReturnType(New UnboundLambda.TargetSignature(StaticCast(Of ParameterSymbol).From(parameters), targetSignature.ReturnType, targetSignature.ReturnsByRef))
            Dim returnType As TypeSymbol = returnTypeInfo.Key

            If Not returnTypeInfo.Value.IsDefaultOrEmpty Then
                diagnostics.AddRange(returnTypeInfo.Value)
            End If

            Dim delegateType As NamedTypeSymbol = ConstructAnonymousDelegateSymbol(source, parameters, returnType, diagnostics)

            Return New KeyValuePair(Of NamedTypeSymbol, ImmutableArray(Of Diagnostic))(delegateType, diagnostics.ToReadOnlyAndFree())
        End Function

        Private Function ConstructAnonymousDelegateSymbol(
            source As UnboundLambda,
            parameters As ImmutableArray(Of BoundLambdaParameterSymbol),
            returnType As TypeSymbol,
            diagnostics As DiagnosticBag
        ) As NamedTypeSymbol
            Debug.Assert(source.IsFunctionLambda = Not returnType.IsVoidType())

            Dim parameterDescriptors(parameters.Length) As AnonymousTypeField
            Dim i As Integer

            For i = 0 To parameters.Length - 1
                Dim sourceParameter = DirectCast(source.Parameters(i), UnboundLambdaParameterSymbol)
                parameterDescriptors(i) = New AnonymousTypeField(
                    parameters(i).Name, parameters(i).Type, sourceParameter.Syntax.GetLocation(), parameters(i).IsByRef)

                ' Verify for restricted types.
                If parameters(i).Type.IsRestrictedType() Then
                    ReportDiagnostic(diagnostics, sourceParameter.TypeSyntax, ERRID.ERR_RestrictedType1, parameters(i).Type)
                End If
            Next

            Dim returnParamName = AnonymousTypeDescriptor.GetReturnParameterName(source.IsFunctionLambda)
            parameterDescriptors(i) = New AnonymousTypeField(returnParamName, returnType, source.Syntax.GetLocation(), False)

            Dim typeDescriptor As New AnonymousTypeDescriptor(parameterDescriptors.AsImmutableOrNull(), source.Syntax.GetLocation(), True)
            Return Me.Compilation.AnonymousTypeManager.ConstructAnonymousDelegateSymbol(typeDescriptor)
        End Function

        Friend Function BindLambdaForErrorRecovery(source As UnboundLambda) As BoundLambda
            If source.BindingCache.ErrorRecoverySignature Is Nothing Then
                ' Let's examine what target signatures did we try to apply to this lambda and let's
                ' get common types from them.
                Dim commonReturnType As TypeSymbol = Nothing
                Dim commonParameterTypes(source.Parameters.Length - 1) As TypeSymbol

                For Each pair In source.BindingCache.InferredReturnType
                    Dim target As UnboundLambda.TargetSignature = pair.Key

                    For i As Integer = 0 To Math.Min(target.ParameterTypes.Length, commonParameterTypes.Length) - 1
                        BindLambdaForErrorRecoveryInferCommonType(commonParameterTypes(i), target.ParameterTypes(i))
                    Next

                    BindLambdaForErrorRecoveryInferCommonType(commonReturnType, pair.Value.Key)
                Next

                For Each pair In source.BindingCache.BoundLambdas
                    Dim target As UnboundLambda.TargetSignature = pair.Key

                    For i As Integer = 0 To Math.Min(target.ParameterTypes.Length, commonParameterTypes.Length) - 1
                        BindLambdaForErrorRecoveryInferCommonType(commonParameterTypes(i), target.ParameterTypes(i))
                    Next

                    BindLambdaForErrorRecoveryInferCommonType(commonReturnType, target.ReturnType)
                Next

                Dim isByRef = BitVector.Empty

                For i As Integer = 0 To commonParameterTypes.Length - 1
                    If source.Parameters(i).Type IsNot Nothing Then
                        commonParameterTypes(i) = source.Parameters(i).Type
                    ElseIf commonParameterTypes(i) Is Nothing OrElse commonParameterTypes(i) Is LambdaSymbol.ErrorRecoveryInferenceError Then
                        ' Use Object for types we couldn't infer
                        commonParameterTypes(i) = Compilation.GetSpecialType(SpecialType.System_Object)
                    End If

                    If source.Parameters(i).IsByRef Then
                        isByRef(i) = True
                    End If
                Next

                If source.ReturnType IsNot Nothing Then
                    commonReturnType = If(source.IsFunctionLambda AndAlso source.ReturnType.IsVoidType(), LambdaSymbol.ReturnTypeVoidReplacement, source.ReturnType)
                ElseIf commonReturnType Is Nothing OrElse commonReturnType Is LambdaSymbol.ErrorRecoveryInferenceError Then
                    commonReturnType = source.InferReturnType(New UnboundLambda.TargetSignature(commonParameterTypes.AsImmutableOrNull(),
                                                                                                isByRef,
                                                                                                Compilation.GetSpecialType(SpecialType.System_Void),
                                                                                                returnsByRef:=False)).Key
                End If

                Interlocked.CompareExchange(source.BindingCache.ErrorRecoverySignature,
                                            New UnboundLambda.TargetSignature(commonParameterTypes.AsImmutableOrNull(), isByRef, commonReturnType, returnsByRef:=False),
                                            Nothing)
            End If

            Return source.Bind(source.BindingCache.ErrorRecoverySignature)
        End Function

        Private Shared Sub BindLambdaForErrorRecoveryInferCommonType(ByRef result As TypeSymbol, candidate As TypeSymbol)
            If result Is Nothing Then
                result = candidate
            ElseIf result IsNot LambdaSymbol.ErrorRecoveryInferenceError Then
                If Not result.IsSameTypeIgnoringAll(candidate) Then
                    result = LambdaSymbol.ErrorRecoveryInferenceError
                End If
            End If
        End Sub

        Friend Function InferFunctionLambdaReturnType(
            source As UnboundLambda,
            targetParameters As UnboundLambda.TargetSignature
        ) As KeyValuePair(Of TypeSymbol, ImmutableArray(Of Diagnostic))
            Debug.Assert(Me Is source.Binder AndAlso source.IsFunctionLambda AndAlso
                         source.ReturnType Is Nothing AndAlso targetParameters.ReturnType.IsVoidType())

            ' If both Async and Iterator are specified, we cannot really infer return type.
            If source.Flags = (SourceMemberFlags.Async Or SourceMemberFlags.Iterator) Then
                ' No need to report any error because we complained about conflicting modifiers.
                Return New KeyValuePair(Of TypeSymbol, ImmutableArray(Of Diagnostic))(LambdaSymbol.ReturnTypeIsUnknown, ImmutableArray(Of Diagnostic).Empty)
            End If

            Dim diagnostics = DiagnosticBag.GetInstance()

            ' Clone parameters. 
            Dim parameters As ImmutableArray(Of BoundLambdaParameterSymbol) = BuildBoundLambdaParameters(source, targetParameters, diagnostics)

            Dim symbol = New SourceLambdaSymbol(source.Syntax, source, parameters, LambdaSymbol.ReturnTypeIsBeingInferred, Me)
            Dim block As BoundBlock = BindLambdaBody(symbol, diagnostics, lambdaBinder:=Nothing)

            If block.HasErrors OrElse diagnostics.HasAnyErrors() Then
                Return New KeyValuePair(Of TypeSymbol, ImmutableArray(Of Diagnostic))(LambdaSymbol.ReturnTypeIsUnknown, diagnostics.ToReadOnlyAndFree())
            End If

            diagnostics.Clear()

            Dim lambdaReturnType As TypeSymbol
            Dim returnExpressions = ArrayBuilder(Of BoundExpression).GetInstance()

            LambdaReturnStatementsVisitor.CollectReturnExpressions(block, returnExpressions, source.Flags = SourceMemberFlags.Iterator)

            If returnExpressions.Count = 0 AndAlso source.Flags = SourceMemberFlags.Async Then
                ' It's fine if there were no return statements in an Async Function.
                ' It simply returns "Task".
                lambdaReturnType = GetWellKnownType(WellKnownType.System_Threading_Tasks_Task, source.Syntax, diagnostics)

            ElseIf returnExpressions.Count = 0 AndAlso source.Flags = SourceMemberFlags.Iterator Then
                ' It's fine if there were no yield statements in an Iterator Function.
                ' It simply returns IEnumerable(of Object).
                lambdaReturnType = GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T, source.Syntax, diagnostics).
                    Construct(GetSpecialType(SpecialType.System_Object, source.Syntax, diagnostics))

            Else
                ' Inference is different for Expression and Statement lambdas
                If source.IsSingleLine Then
                    Debug.Assert(returnExpressions.Count < 2)

                    Dim returnExpression As BoundExpression = Nothing

                    If returnExpressions.Count > 0 Then
                        returnExpression = MakeRValue(returnExpressions(0), diagnostics)
                    End If

                    If returnExpression IsNot Nothing AndAlso Not returnExpression.HasErrors AndAlso Not diagnostics.HasAnyErrors() Then
                        lambdaReturnType = returnExpression.Type
                    Else
                        lambdaReturnType = GetSpecialType(SpecialType.System_Object, source.Syntax, diagnostics)
                    End If

                    diagnostics.Clear()

                    Dim restrictedType As TypeSymbol = Nothing
                    If lambdaReturnType.IsRestrictedTypeOrArrayType(restrictedType) Then
                        ReportDiagnostic(diagnostics, LambdaHeaderErrorNode(source), ERRID.ERR_RestrictedType1, restrictedType)
                    End If
                Else
                    Dim numCandidates As Integer = 0
                    lambdaReturnType = InferDominantTypeOfExpressions(source.Syntax, returnExpressions, diagnostics, numCandidates)

                    Debug.Assert(lambdaReturnType IsNot Nothing OrElse numCandidates = 0)

                    Dim restrictedType As TypeSymbol = Nothing
                    If lambdaReturnType Is Nothing Then
                        ' "Cannot infer a return type. Specifying the return type might correct this error."
                        ReportDiagnostic(diagnostics, LambdaHeaderErrorNode(source), ERRID.ERR_LambdaNoType)
                        lambdaReturnType = LambdaSymbol.ReturnTypeIsUnknown

                    ElseIf lambdaReturnType.IsRestrictedTypeOrArrayType(restrictedType) Then
                        ReportDiagnostic(diagnostics, LambdaHeaderErrorNode(source), ERRID.ERR_RestrictedType1, restrictedType)

                    ElseIf numCandidates <> 1 Then
                        If OptionStrict = OptionStrict.On Then
                            If numCandidates = 0 Then
                                ' "Cannot infer a return type, and Option Strict On does not allow 'Object' to be assumed. Specifying the return type might correct this error."
                                ReportDiagnostic(diagnostics, LambdaHeaderErrorNode(source), ERRID.ERR_LambdaNoTypeObjectDisallowed)
                                Debug.Assert(lambdaReturnType.IsObjectType())
                            Else
                                ' "Cannot infer a return type because more than one type is possible. Specifying the return type might correct this error."
                                ReportDiagnostic(diagnostics, LambdaHeaderErrorNode(source), ERRID.ERR_LambdaTooManyTypesObjectDisallowed)
                                Debug.Assert(lambdaReturnType.IsObjectType())
                            End If
                        ElseIf OptionStrict = OptionStrict.Custom Then
                            If numCandidates = 0 Then
                                ' "Cannot infer a return type; 'Object' assumed."
                                ReportDiagnostic(diagnostics, LambdaHeaderErrorNode(source), ERRID.WRN_ObjectAssumed1, ErrorFactory.ErrorInfo(ERRID.WRN_LambdaNoTypeObjectAssumed))
                                Debug.Assert(lambdaReturnType.IsObjectType())
                            Else
                                ' "Cannot infer a return type because more than one type is possible; 'Object' assumed."
                                ReportDiagnostic(diagnostics, LambdaHeaderErrorNode(source), ERRID.WRN_ObjectAssumed1, ErrorFactory.ErrorInfo(ERRID.WRN_LambdaTooManyTypesObjectAssumed))
                                Debug.Assert(lambdaReturnType.IsObjectType())
                            End If
                        End If
                    End If
                End If

                If source.Flags = SourceMemberFlags.Async Then
                    ' There were some returns with values from Async lambda, infer Task(Of T) as return type of the lambda.
                    lambdaReturnType = GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T, source.Syntax, diagnostics).Construct(lambdaReturnType)

                ElseIf source.Flags = SourceMemberFlags.Iterator Then
                    ' There were some returns with values from Iterator lambda, infer IEnumerable(Of T) as return type of the lambda.
                    lambdaReturnType = GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T, source.Syntax, diagnostics).Construct(lambdaReturnType)

                End If
            End If

            returnExpressions.Free()

            Return New KeyValuePair(Of TypeSymbol, ImmutableArray(Of Diagnostic))(lambdaReturnType, diagnostics.ToReadOnlyAndFree())
        End Function

        Private Shared Function LambdaHeaderErrorNode(source As UnboundLambda) As SyntaxNode
            Dim lambdaSyntax = TryCast(source.Syntax, LambdaExpressionSyntax)

            If lambdaSyntax IsNot Nothing Then
                Return lambdaSyntax.SubOrFunctionHeader
            End If

            Return source.Syntax
        End Function

        Private Class LambdaReturnStatementsVisitor
            Inherits StatementWalker

            Private ReadOnly _builder As ArrayBuilder(Of BoundExpression)
            Private ReadOnly _isIterator As Boolean

            Private Sub New(builder As ArrayBuilder(Of BoundExpression), isIterator As Boolean)
                Me._builder = builder
                Me._isIterator = isIterator
            End Sub

            ''' <summary>
            ''' Collects expressions that are effective return values of the lambda body.
            ''' In iterators those would be arguments of Yield statements.
            ''' </summary>
            Public Shared Sub CollectReturnExpressions(lambdaBlock As BoundBlock,
                                                       arrayToFill As ArrayBuilder(Of BoundExpression),
                                                       isIterator As Boolean)

                Dim visitor = New LambdaReturnStatementsVisitor(arrayToFill, isIterator)
                visitor.VisitBlock(lambdaBlock)
            End Sub

            Public Overrides Function Visit(node As BoundNode) As BoundNode
                'Do not visit expressions
                If node Is Nothing OrElse TypeOf node Is BoundExpression Then
                    Return Nothing
                End If

                Return MyBase.Visit(node)
            End Function

            Public Overrides Function VisitLambda(node As BoundLambda) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public Overrides Function VisitReturnStatement(node As BoundReturnStatement) As BoundNode
                ' not interested in Returns in an iterator.
                If _isIterator Then
                    Return Nothing
                End If

                Dim expr As BoundExpression = node.ExpressionOpt

                ' Skip synthetic return for the function variable.
                If expr Is Nothing OrElse expr.Type Is LambdaSymbol.ReturnTypeIsBeingInferred Then
                    Return Nothing
                End If

                _builder.Add(expr)

                Return Nothing
            End Function

            Public Overrides Function VisitYieldStatement(node As BoundYieldStatement) As BoundNode
                If _isIterator Then
                    _builder.Add(node.Expression)
                End If

                Return Nothing
            End Function
        End Class
    End Class

    ''' <summary>
    ''' Provides context for binding body of a Lambda.
    ''' </summary>
    Friend NotInheritable Class LambdaBodyBinder
        Inherits SubOrFunctionBodyBinder

        Private ReadOnly _functionValue As LocalSymbol

        Public Sub New(lambdaSymbol As LambdaSymbol, containingBinder As Binder)
            MyBase.New(lambdaSymbol, lambdaSymbol.Syntax, containingBinder)
            _functionValue = CreateFunctionValueLocal(lambdaSymbol)
        End Sub

        Private Shared Function CreateFunctionValueLocal(lambdaSymbol As LambdaSymbol) As LocalSymbol
            ' synthesized lambdas may not result from a LambdaExpressionSyntax (e.g. an AddressOf expression that 
            ' needs relaxation). In this case there will be no need to create a local here, this is done when generating the 
            ' lambda body.
            If lambdaSymbol.IsImplicitlyDeclared OrElse lambdaSymbol.IsSub Then
                Return Nothing
            End If

            Dim header As LambdaHeaderSyntax = DirectCast(lambdaSymbol.Syntax, LambdaExpressionSyntax).SubOrFunctionHeader
            Return New SynthesizedLocal(lambdaSymbol, lambdaSymbol.ReturnType, SynthesizedLocalKind.FunctionReturnValue, header)
        End Function

        Public Overrides Function GetLocalForFunctionValue() As LocalSymbol
            Return _functionValue
        End Function

        Public Overrides Function GetContinueLabel(continueSyntaxKind As SyntaxKind) As LabelSymbol
            Return Nothing
        End Function

        Public Overrides Function GetExitLabel(exitSyntaxKind As SyntaxKind) As LabelSymbol
            Return Nothing
        End Function

        Public Overrides Function GetReturnLabel() As LabelSymbol
            Return Nothing
        End Function

        Friend Overrides Sub LookupInSingleBinder(lookupResult As LookupResult, name As String, arity As Integer, options As LookupOptions, originalBinder As Binder,
                                                     <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo))
            MyBase.LookupInSingleBinder(lookupResult, name, arity, options, originalBinder, useSiteDiagnostics)

            If (options And LookupOptions.LabelsOnly) = LookupOptions.LabelsOnly Then
                If lookupResult.Kind = LookupResultKind.Empty Then
                    lookupResult.SetFrom(SingleLookupResult.EmptyAndStopLookup)
                End If
            End If
        End Sub
    End Class
End Namespace

