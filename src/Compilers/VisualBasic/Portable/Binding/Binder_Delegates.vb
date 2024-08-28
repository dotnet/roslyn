' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class Binder

        ''' <summary>
        ''' Structure is used to store all information which is needed to construct and classify a Delegate creation 
        ''' expression later on.
        ''' </summary>
        Friend Structure DelegateResolutionResult
            ' we store the DelegateConversions although it could be derived from MethodConversions to improve performance
            Public ReadOnly DelegateConversions As ConversionKind
            Public ReadOnly Target As MethodSymbol
            Public ReadOnly MethodConversions As MethodConversionKind
            Public ReadOnly Diagnostics As ReadOnlyBindingDiagnostic(Of AssemblySymbol)

            Public Sub New(
                DelegateConversions As ConversionKind,
                Target As MethodSymbol,
                MethodConversions As MethodConversionKind,
                Diagnostics As ReadOnlyBindingDiagnostic(Of AssemblySymbol)
            )
                Me.DelegateConversions = DelegateConversions
                Me.Target = Target
                Me.Diagnostics = Diagnostics
                Me.MethodConversions = MethodConversions
            End Sub
        End Structure

        ''' <summary>
        ''' Binds the AddressOf expression.
        ''' </summary>
        ''' <param name="node">The AddressOf expression node.</param>
        ''' <param name="diagnostics">The diagnostics.</param><returns></returns>
        Private Function BindAddressOfExpression(node As VisualBasicSyntaxNode, diagnostics As BindingDiagnosticBag) As BoundExpression

            Dim addressOfSyntax = DirectCast(node, UnaryExpressionSyntax)
            Dim boundOperand = BindExpression(addressOfSyntax.Operand, isInvocationOrAddressOf:=True, diagnostics:=diagnostics, isOperandOfConditionalBranch:=False, eventContext:=False)

            If boundOperand.Kind = BoundKind.LateMemberAccess Then
                Return New BoundLateAddressOfOperator(node, Me, DirectCast(boundOperand, BoundLateMemberAccess), boundOperand.Type)
            End If

            ' only accept MethodGroups as operands. More detailed checks (e.g. for Constructors follow later)
            If boundOperand.Kind <> BoundKind.MethodGroup Then
                If Not boundOperand.HasErrors Then
                    ReportDiagnostic(diagnostics, addressOfSyntax.Operand, ERRID.ERR_AddressOfOperandNotMethod)
                End If

                Return BadExpression(addressOfSyntax, boundOperand, LookupResultKind.NotAValue, ErrorTypeSymbol.UnknownResultType)
            End If

            Dim hasErrors As Boolean = False
            Dim group = DirectCast(boundOperand, BoundMethodGroup)

            If IsGroupOfConstructors(group) Then
                ReportDiagnostic(diagnostics, addressOfSyntax.Operand, ERRID.ERR_InvalidConstructorCall)
                hasErrors = True
            End If

            Return New BoundAddressOfOperator(node, Me, diagnostics.AccumulatesDependencies, group, hasErrors)
        End Function

        ''' <summary>
        ''' Binds the delegate creation expression.
        ''' This comes in form of e.g.
        ''' Dim del as new DelegateType(AddressOf methodName)
        ''' </summary>
        ''' <param name="delegateType">Type of the delegate.</param>
        ''' <param name="argumentListOpt">The argument list.</param>
        ''' <param name="node">Syntax node to attach diagnostics to in case the argument list is nothing.</param>
        ''' <param name="diagnostics">The diagnostics.</param><returns></returns>
        Private Function BindDelegateCreationExpression(
            delegateType As TypeSymbol,
            argumentListOpt As ArgumentListSyntax,
            node As VisualBasicSyntaxNode,
            diagnostics As BindingDiagnosticBag
        ) As BoundExpression

            Dim boundFirstArgument As BoundExpression = Nothing
            Dim argumentCount = 0
            If argumentListOpt IsNot Nothing Then
                argumentCount = argumentListOpt.Arguments.Count
            End If
            Dim hadErrorsInFirstArgument = False

            ' a delegate creation expression should have exactly one argument. 
            If argumentCount > 0 Then
                Dim argumentSyntax = argumentListOpt.Arguments(0)
                Dim expressionSyntax As ExpressionSyntax = Nothing

                ' a delegate creation expression does not care if what the name of a named argument
                ' was. Just take whatever was passed.
                If argumentSyntax.Kind = SyntaxKind.SimpleArgument Then
                    expressionSyntax = argumentSyntax.GetExpression()
                End If
                ' omitted argument will leave expressionSyntax as nothing which means no binding, which is fine.

                If expressionSyntax IsNot Nothing Then
                    If expressionSyntax.Kind = SyntaxKind.AddressOfExpression Then
                        boundFirstArgument = BindAddressOfExpression(expressionSyntax, diagnostics)

                    ElseIf expressionSyntax.IsLambdaExpressionSyntax() Then
                        ' this covers the legal cases for SyntaxKind.MultiLineFunctionLambdaExpression,
                        ' SyntaxKind.SingleLineFunctionLambdaExpression, SyntaxKind.MultiLineSubLambdaExpression and
                        ' SyntaxKind.SingleLineSubLambdaExpression, as well as all the other invalid ones.
                        boundFirstArgument = BindExpression(expressionSyntax, diagnostics)
                    End If

                    If boundFirstArgument IsNot Nothing Then
                        hadErrorsInFirstArgument = boundFirstArgument.HasErrors
                        Debug.Assert(boundFirstArgument.Kind = BoundKind.BadExpression OrElse
                                     boundFirstArgument.Kind = BoundKind.LateAddressOfOperator OrElse
                                     boundFirstArgument.Kind = BoundKind.AddressOfOperator OrElse
                                     boundFirstArgument.Kind = BoundKind.UnboundLambda)

                        If argumentCount = 1 Then
                            boundFirstArgument = ApplyImplicitConversion(node,
                                                                         delegateType,
                                                                         boundFirstArgument,
                                                                         diagnostics:=diagnostics)

                            If boundFirstArgument.Syntax IsNot node Then
                                ' We must have a bound node that corresponds to that syntax node for GetSemanticInfo.
                                ' Insert an identity conversion if necessary.
                                Debug.Assert(boundFirstArgument.Kind <> BoundKind.Conversion, "Associated wrong node with conversion?")
                                boundFirstArgument = New BoundConversion(node, boundFirstArgument, ConversionKind.Identity, CheckOverflow, True, delegateType)
                            ElseIf boundFirstArgument.Kind = BoundKind.Conversion Then
                                Debug.Assert(Not boundFirstArgument.WasCompilerGenerated)
                                Dim boundConversion = DirectCast(boundFirstArgument, BoundConversion)
                                boundFirstArgument = boundConversion.Update(boundConversion.Operand,
                                                                            boundConversion.ConversionKind,
                                                                            boundConversion.Checked,
                                                                            True, ' ExplicitCastInCode
                                                                            boundConversion.ConstantValueOpt,
                                                                            boundConversion.ExtendedInfoOpt,
                                                                            boundConversion.Type)
                            End If

                            Return boundFirstArgument
                        End If
                    End If
                Else
                    boundFirstArgument = New BoundBadExpression(argumentSyntax,
                                                                LookupResultKind.Empty,
                                                                ImmutableArray(Of Symbol).Empty,
                                                                ImmutableArray(Of BoundExpression).Empty,
                                                                ErrorTypeSymbol.UnknownResultType,
                                                                hasErrors:=True)
                End If
            End If

            Dim boundArguments(argumentCount - 1) As BoundExpression
            If boundFirstArgument IsNot Nothing Then
                boundFirstArgument = MakeRValueAndIgnoreDiagnostics(boundFirstArgument)
                boundArguments(0) = boundFirstArgument
            End If

            ' bind all arguments and ignore all diagnostics. These bound nodes will be passed to
            ' a BoundBadNode

            For argumentIndex = If(boundFirstArgument Is Nothing, 0, 1) To argumentCount - 1
                Dim expressionSyntax As ExpressionSyntax = Nothing
                Dim argumentSyntax = argumentListOpt.Arguments(argumentIndex)

                If argumentSyntax.Kind = SyntaxKind.SimpleArgument Then
                    expressionSyntax = argumentSyntax.GetExpression()
                End If

                If expressionSyntax IsNot Nothing Then
                    boundArguments(argumentIndex) = BindValue(expressionSyntax, BindingDiagnosticBag.Discarded)
                Else
                    boundArguments(argumentIndex) = New BoundBadExpression(argumentSyntax,
                                                                           LookupResultKind.Empty,
                                                                           ImmutableArray(Of Symbol).Empty,
                                                                           ImmutableArray(Of BoundExpression).Empty,
                                                                           ErrorTypeSymbol.UnknownResultType,
                                                                           hasErrors:=True)
                End If
            Next

            ' the default error message in delegate creations if the passed arguments are empty or not a addressOf
            ' should be ERRID.ERR_NoDirectDelegateConstruction1
            ' if binding an AddressOf expression caused diagnostics these should be shown instead
            If Not hadErrorsInFirstArgument OrElse
                argumentCount <> 1 Then
                ReportDiagnostic(diagnostics,
                                     If(argumentListOpt, node),
                                     ERRID.ERR_NoDirectDelegateConstruction1,
                                     delegateType)
            End If

            Return BadExpression(node,
                                 ImmutableArray.Create(boundArguments),
                                 delegateType)
        End Function

        ''' <summary>
        ''' Resolves the target method for the delegate and classifies the conversion
        ''' </summary>
        ''' <param name="addressOfExpression">The bound AddressOf expression itself.</param>
        ''' <param name="targetType">The delegate type to assign the result of the AddressOf operator to.</param>
        ''' <returns></returns>
        Friend Shared Function InterpretDelegateBinding(
            addressOfExpression As BoundAddressOfOperator,
            targetType As TypeSymbol,
            isForHandles As Boolean
        ) As DelegateResolutionResult
            Debug.Assert(targetType IsNot Nothing)

            Dim diagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics:=True, addressOfExpression.WithDependencies)
            Dim result As OverloadResolution.OverloadResolutionResult = Nothing
            Dim fromMethod As MethodSymbol = Nothing

            Dim syntaxTree = addressOfExpression.Syntax
            Dim methodConversions As MethodConversionKind = MethodConversionKind.Identity

            ' must be a delegate, and also a concrete delegate
            If targetType.SpecialType = SpecialType.System_Delegate OrElse
                targetType.SpecialType = SpecialType.System_MulticastDelegate Then
                ' 'AddressOf' expression cannot be converted to '{0}' because type '{0}' is declared 'MustInherit' and cannot be created.
                ReportDiagnostic(diagnostics, syntaxTree, ERRID.ERR_AddressOfNotCreatableDelegate1, targetType)
                methodConversions = methodConversions Or MethodConversionKind.Error_Unspecified
            ElseIf targetType.TypeKind <> TypeKind.Delegate Then
                ' 'AddressOf' expression cannot be converted to '{0}' because '{0}' is not a delegate type.
                If targetType.TypeKind <> TypeKind.Error Then
                    ReportDiagnostic(diagnostics, syntaxTree, ERRID.ERR_AddressOfNotDelegate1, targetType)
                End If
                methodConversions = methodConversions Or MethodConversionKind.Error_Unspecified
            Else

                Dim delegateInvoke = DirectCast(targetType, NamedTypeSymbol).DelegateInvokeMethod

                If delegateInvoke IsNot Nothing Then

                    If ReportDelegateInvokeUseSite(diagnostics, syntaxTree, targetType, delegateInvoke) Then
                        methodConversions = methodConversions Or MethodConversionKind.Error_Unspecified
                    Else

                        ' todo(rbeckers) if (IsLateReference(addressOfExpression))

                        Dim matchingMethod As KeyValuePair(Of MethodSymbol, MethodConversionKind) = ResolveMethodForDelegateInvokeFullAndRelaxed(
                            addressOfExpression,
                            delegateInvoke,
                            False,
                            diagnostics)

                        fromMethod = matchingMethod.Key
                        methodConversions = matchingMethod.Value
                    End If
                Else
                    ReportDiagnostic(diagnostics, syntaxTree, ERRID.ERR_UnsupportedMethod1, targetType)

                    methodConversions = methodConversions Or MethodConversionKind.Error_Unspecified
                End If
            End If

            ' show diagnostics if the an instance method is used in a shared context.
            If fromMethod IsNot Nothing Then
                '  Generate an error, but continue processing
                If addressOfExpression.Binder.CheckSharedSymbolAccess(addressOfExpression.Syntax,
                                               fromMethod.IsShared,
                                               addressOfExpression.MethodGroup.ReceiverOpt,
                                               addressOfExpression.MethodGroup.QualificationKind,
                                               diagnostics) Then

                    methodConversions = methodConversions Or MethodConversionKind.Error_Unspecified
                End If
            End If

            ' TODO: Check boxing of restricted types, report ERRID_RestrictedConversion1 and continue.

            Dim receiver As BoundExpression = addressOfExpression.MethodGroup.ReceiverOpt
            If fromMethod IsNot Nothing Then
                If fromMethod.IsMustOverride AndAlso receiver IsNot Nothing AndAlso
                    (receiver.IsMyBaseReference OrElse receiver.IsMyClassReference) Then

                    '  Generate an error, but continue processing
                    ReportDiagnostic(diagnostics, addressOfExpression.MethodGroup.Syntax,
                                     If(receiver.IsMyBaseReference,
                                        ERRID.ERR_MyBaseAbstractCall1,
                                        ERRID.ERR_MyClassAbstractCall1),
                                     fromMethod)

                    methodConversions = methodConversions Or MethodConversionKind.Error_Unspecified
                End If

                If Not fromMethod.IsShared AndAlso
                    fromMethod.ContainingType.IsNullableType AndAlso
                    Not fromMethod.IsOverrides Then

                    Dim addressOfSyntax As SyntaxNode = addressOfExpression.Syntax
                    Dim addressOfExpressionSyntax = DirectCast(addressOfExpression.Syntax, UnaryExpressionSyntax)
                    If (addressOfExpressionSyntax IsNot Nothing) Then
                        addressOfSyntax = addressOfExpressionSyntax.Operand
                    End If

                    '  Generate an error, but continue processing
                    ReportDiagnostic(diagnostics,
                                     addressOfSyntax,
                                     ERRID.ERR_AddressOfNullableMethod,
                                     fromMethod.ContainingType,
                                     SyntaxFacts.GetText(SyntaxKind.AddressOfKeyword))

                    ' There's no real need to set MethodConversionKind.Error because there are no overloads of the same method where one 
                    ' may be legal to call because it's shared and the other's not.
                    ' However to be future proof, we set it regardless.
                    methodConversions = methodConversions Or MethodConversionKind.Error_Unspecified
                End If

                addressOfExpression.Binder.ReportDiagnosticsIfObsoleteOrNotSupported(diagnostics, fromMethod, addressOfExpression.MethodGroup.Syntax)
            End If

            Dim delegateConversions As ConversionKind = Conversions.DetermineDelegateRelaxationLevel(methodConversions)

            If (delegateConversions And ConversionKind.DelegateRelaxationLevelInvalid) <> ConversionKind.DelegateRelaxationLevelInvalid Then
                If Conversions.IsNarrowingMethodConversion(methodConversions, isForAddressOf:=Not isForHandles) Then
                    delegateConversions = delegateConversions Or ConversionKind.Narrowing
                Else
                    delegateConversions = delegateConversions Or ConversionKind.Widening
                End If
            End If

            Return New DelegateResolutionResult(delegateConversions, fromMethod, methodConversions, diagnostics.ToReadOnlyAndFree())
        End Function

        Friend Shared Function ReportDelegateInvokeUseSite(
            diagBag As BindingDiagnosticBag,
            syntax As SyntaxNode,
            delegateType As TypeSymbol,
            invoke As MethodSymbol
        ) As Boolean
            Debug.Assert(delegateType IsNot Nothing)
            Debug.Assert(invoke IsNot Nothing)

            Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol) = invoke.GetUseSiteInfo()

            If useSiteInfo.DiagnosticInfo?.Code = ERRID.ERR_UnsupportedMethod1 Then
                useSiteInfo = New UseSiteInfo(Of AssemblySymbol)(ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedMethod1, delegateType))
            End If

            Return diagBag.Add(useSiteInfo, syntax)
        End Function

        ''' <summary>
        ''' Resolves the method for delegate invoke with all or relaxed arguments / return types. It also determines 
        ''' the method conversion kind.
        ''' </summary>
        ''' <param name="addressOfExpression">The AddressOf expression.</param>
        ''' <param name="toMethod">The delegate invoke method.</param>
        ''' <param name="ignoreMethodReturnType">Ignore method's return type for the purpose of calculating 'methodConversions'.</param>
        ''' <param name="diagnostics">The diagnostics.</param>
        ''' <returns>The resolved method if any.</returns>
        Friend Shared Function ResolveMethodForDelegateInvokeFullAndRelaxed(
            addressOfExpression As BoundAddressOfOperator,
            toMethod As MethodSymbol,
            ignoreMethodReturnType As Boolean,
            diagnostics As BindingDiagnosticBag
        ) As KeyValuePair(Of MethodSymbol, MethodConversionKind)

            Dim argumentDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics)
            Dim couldTryZeroArgumentRelaxation As Boolean = True

            Dim matchingMethod As KeyValuePair(Of MethodSymbol, MethodConversionKind) = ResolveMethodForDelegateInvokeFullOrRelaxed(
                addressOfExpression,
                toMethod,
                ignoreMethodReturnType,
                argumentDiagnostics,
                useZeroArgumentRelaxation:=False,
                couldTryZeroArgumentRelaxation:=couldTryZeroArgumentRelaxation)

            ' If there have been parameters and if there was no ambiguous match before, try zero argument relaxation.
            If matchingMethod.Key Is Nothing AndAlso couldTryZeroArgumentRelaxation Then

                Dim zeroArgumentDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics)
                Dim argumentMatchingMethod = matchingMethod

                matchingMethod = ResolveMethodForDelegateInvokeFullOrRelaxed(
                    addressOfExpression,
                    toMethod,
                    ignoreMethodReturnType,
                    zeroArgumentDiagnostics,
                    useZeroArgumentRelaxation:=True,
                    couldTryZeroArgumentRelaxation:=couldTryZeroArgumentRelaxation)

                ' if zero relaxation did not find something, we'll report the diagnostics of the
                ' non zero relaxation try, else the diagnostics of the zero argument relaxation.
                If matchingMethod.Key Is Nothing Then
                    diagnostics.AddRange(argumentDiagnostics)
                    matchingMethod = argumentMatchingMethod
                Else
                    diagnostics.AddRange(zeroArgumentDiagnostics)
                End If

                zeroArgumentDiagnostics.Free()
            Else
                diagnostics.AddRange(argumentDiagnostics)
            End If

            argumentDiagnostics.Free()

            ' check that there's not method returned if there is no conversion.
            Debug.Assert(matchingMethod.Key Is Nothing OrElse (matchingMethod.Value And MethodConversionKind.AllErrorReasons) = 0)

            Return matchingMethod
        End Function

        ''' <summary>
        ''' Resolves the method for delegate invoke with all or relaxed arguments / return types. It also determines 
        ''' the method conversion kind.
        ''' </summary>
        ''' <param name="addressOfExpression">The AddressOf expression.</param>
        ''' <param name="toMethod">The delegate invoke method.</param>
        ''' <param name="ignoreMethodReturnType">Ignore method's return type for the purpose of calculating 'methodConversions'.</param>
        ''' <param name="diagnostics">The diagnostics.</param>
        ''' <param name="useZeroArgumentRelaxation">if set to <c>true</c> use zero argument relaxation.</param>
        ''' <returns>The resolved method if any.</returns>
        Private Shared Function ResolveMethodForDelegateInvokeFullOrRelaxed(
            addressOfExpression As BoundAddressOfOperator,
            toMethod As MethodSymbol,
            ignoreMethodReturnType As Boolean,
            diagnostics As BindingDiagnosticBag,
            useZeroArgumentRelaxation As Boolean,
            ByRef couldTryZeroArgumentRelaxation As Boolean
        ) As KeyValuePair(Of MethodSymbol, MethodConversionKind)

            Dim boundArguments = ImmutableArray(Of BoundExpression).Empty

            If Not useZeroArgumentRelaxation Then
                ' build array of bound expressions for overload resolution (BoundLocal is easy to create)
                Dim toMethodParameters = toMethod.Parameters
                Dim parameterCount = toMethodParameters.Length
                If parameterCount > 0 Then
                    Dim boundParameterArguments(parameterCount - 1) As BoundExpression
                    Dim argumentIndex As Integer = 0
                    Dim syntaxTree As SyntaxTree
                    Dim addressOfSyntax = addressOfExpression.Syntax
                    syntaxTree = addressOfExpression.Binder.SyntaxTree
                    For Each parameter In toMethodParameters
                        Dim parameterType = parameter.Type
                        Dim tempParamSymbol = New SynthesizedLocal(toMethod, parameterType, SynthesizedLocalKind.LoweringTemp)
                        ' TODO: Switch to using BoundValuePlaceholder, but we need it to be able to appear
                        ' as an LValue in case of a ByRef parameter.
                        Dim tempBoundParameter As BoundExpression = New BoundLocal(addressOfSyntax,
                                                                                   tempParamSymbol,
                                                                                   parameterType)

                        ' don't treat ByVal parameters as lvalues in the following OverloadResolution
                        If Not parameter.IsByRef Then
                            tempBoundParameter = tempBoundParameter.MakeRValue()
                        End If

                        boundParameterArguments(argumentIndex) = tempBoundParameter
                        argumentIndex += 1
                    Next
                    boundArguments = boundParameterArguments.AsImmutableOrNull()
                Else
                    couldTryZeroArgumentRelaxation = False
                End If
            End If

            Dim delegateReturnType As TypeSymbol
            Dim delegateReturnTypeReferenceBoundNode As BoundNode

            If ignoreMethodReturnType Then
                ' Keep them Nothing such that the delegate's return type won't be taken part of in overload resolution
                ' when we are inferring the return type.
                delegateReturnType = Nothing
                delegateReturnTypeReferenceBoundNode = Nothing
            Else
                delegateReturnType = toMethod.ReturnType
                delegateReturnTypeReferenceBoundNode = addressOfExpression
            End If

            ' Let's go through overload resolution, pretending that Option Strict is Off and see if it succeeds.
            Dim resolutionBinder As Binder

            If addressOfExpression.Binder.OptionStrict <> VisualBasic.OptionStrict.Off Then
                resolutionBinder = New OptionStrictOffBinder(addressOfExpression.Binder)
            Else
                resolutionBinder = addressOfExpression.Binder
            End If

            Debug.Assert(resolutionBinder.OptionStrict = VisualBasic.OptionStrict.Off)

            Dim useSiteInfo = addressOfExpression.Binder.GetNewCompoundUseSiteInfo(diagnostics)
            Dim resolutionResult = OverloadResolution.MethodInvocationOverloadResolution(
                addressOfExpression.MethodGroup,
                boundArguments,
                Nothing,
                resolutionBinder,
                includeEliminatedCandidates:=False,
                delegateReturnType:=delegateReturnType,
                delegateReturnTypeReferenceBoundNode:=delegateReturnTypeReferenceBoundNode,
                lateBindingIsAllowed:=False,
                callerInfoOpt:=Nothing,
                useSiteInfo:=useSiteInfo)

            If diagnostics.Add(addressOfExpression.MethodGroup, useSiteInfo) Then
                couldTryZeroArgumentRelaxation = False
                If addressOfExpression.MethodGroup.ResultKind <> LookupResultKind.Inaccessible Then
                    ' Suppress additional diagnostics
                    diagnostics = BindingDiagnosticBag.Discarded
                End If
            End If

            Dim addressOfMethodGroup = addressOfExpression.MethodGroup

            If resolutionResult.BestResult.HasValue Then
                Return ValidateMethodForDelegateInvoke(
                            addressOfExpression,
                            resolutionResult.BestResult.Value,
                            toMethod,
                            ignoreMethodReturnType,
                            useZeroArgumentRelaxation,
                            diagnostics)
            End If

            ' Overload Resolution didn't find a match
            If resolutionResult.Candidates.Length = 0 Then
                resolutionResult = OverloadResolution.MethodInvocationOverloadResolution(
                    addressOfMethodGroup,
                    boundArguments,
                    Nothing,
                    resolutionBinder,
                    includeEliminatedCandidates:=True,
                    delegateReturnType:=delegateReturnType,
                    delegateReturnTypeReferenceBoundNode:=delegateReturnTypeReferenceBoundNode,
                    lateBindingIsAllowed:=False,
                    callerInfoOpt:=Nothing,
                    useSiteInfo:=useSiteInfo)
            End If

            Dim bestCandidates = ArrayBuilder(Of OverloadResolution.CandidateAnalysisResult).GetInstance()
            Dim bestSymbols = ImmutableArray(Of Symbol).Empty

            Dim commonReturnType As TypeSymbol = GetSetOfTheBestCandidates(resolutionResult, bestCandidates, bestSymbols)

            Debug.Assert(bestCandidates.Count > 0 AndAlso bestCandidates.Count > 0)

            Dim bestCandidatesState As OverloadResolution.CandidateAnalysisResultState = bestCandidates(0).State

            If bestCandidatesState = VisualBasic.OverloadResolution.CandidateAnalysisResultState.Applicable Then
                ' if there is an applicable candidate in the list, we know it must be an ambiguous match 
                ' (or there are more applicable candidates in this list), otherwise this would have been
                ' the best match.
                Debug.Assert(bestCandidates.Count > 1 AndAlso bestSymbols.Length > 1)

                ' there are multiple candidates, so it ambiguous and zero argument relaxation will not be tried,
                ' unless the candidates require narrowing.
                If Not bestCandidates(0).RequiresNarrowingConversion Then
                    couldTryZeroArgumentRelaxation = False
                End If
            End If

            If bestSymbols.Length = 1 AndAlso
               (bestCandidatesState = OverloadResolution.CandidateAnalysisResultState.ArgumentCountMismatch OrElse
               bestCandidatesState = OverloadResolution.CandidateAnalysisResultState.ArgumentMismatch) Then

                ' Dev10 has squiggles under the operand of the AddressOf. The syntax of addressOfExpression
                ' is the complete AddressOf expression, so we need to get the operand first.
                Dim addressOfOperandSyntax = addressOfExpression.Syntax
                If addressOfOperandSyntax.Kind = SyntaxKind.AddressOfExpression Then
                    addressOfOperandSyntax = DirectCast(addressOfOperandSyntax, UnaryExpressionSyntax).Operand
                End If

                If addressOfExpression.MethodGroup.ResultKind = LookupResultKind.Inaccessible Then
                    ReportDiagnostic(diagnostics, addressOfOperandSyntax,
                                     addressOfExpression.Binder.GetInaccessibleErrorInfo(
                                         bestSymbols(0)))
                Else
                    Debug.Assert(addressOfExpression.MethodGroup.ResultKind = LookupResultKind.Good)
                End If

                ReportDelegateBindingIncompatible(
                    addressOfOperandSyntax,
                    toMethod.ContainingType,
                    DirectCast(bestSymbols(0), MethodSymbol),
                    diagnostics)
            Else

                If bestCandidatesState = OverloadResolution.CandidateAnalysisResultState.HasUseSiteError OrElse
                   bestCandidatesState = OverloadResolution.CandidateAnalysisResultState.HasUnsupportedMetadata OrElse
                   bestCandidatesState = OverloadResolution.CandidateAnalysisResultState.Ambiguous Then
                    couldTryZeroArgumentRelaxation = False
                End If

                Dim unused = resolutionBinder.ReportOverloadResolutionFailureAndProduceBoundNode(
                    addressOfExpression.MethodGroup.Syntax,
                    addressOfMethodGroup,
                    bestCandidates,
                    bestSymbols,
                    commonReturnType,
                    boundArguments,
                    Nothing,
                    diagnostics,
                    delegateSymbol:=toMethod.ContainingType,
                    callerInfoOpt:=Nothing)
            End If

            bestCandidates.Free()

            Return New KeyValuePair(Of MethodSymbol, MethodConversionKind)(Nothing, MethodConversionKind.Error_OverloadResolution)
        End Function

        Private Shared Function ValidateMethodForDelegateInvoke(
            addressOfExpression As BoundAddressOfOperator,
            analysisResult As OverloadResolution.CandidateAnalysisResult,
            toMethod As MethodSymbol,
            ignoreMethodReturnType As Boolean,
            useZeroArgumentRelaxation As Boolean,
            diagnostics As BindingDiagnosticBag
        ) As KeyValuePair(Of MethodSymbol, MethodConversionKind)

            Dim methodConversions As MethodConversionKind = MethodConversionKind.Identity

            ' Dev10 has squiggles under the operand of the AddressOf. The syntax of addressOfExpression
            ' is the complete AddressOf expression, so we need to get the operand first.
            Dim addressOfOperandSyntax = addressOfExpression.Syntax
            If addressOfOperandSyntax.Kind = SyntaxKind.AddressOfExpression Then
                addressOfOperandSyntax = DirectCast(addressOfOperandSyntax, UnaryExpressionSyntax).Operand
            End If

            ' determine conversions based on return type
            Dim useSiteInfo = addressOfExpression.Binder.GetNewCompoundUseSiteInfo(diagnostics)
            Dim targetMethodSymbol = DirectCast(analysisResult.Candidate.UnderlyingSymbol, MethodSymbol)

            If Not ignoreMethodReturnType Then
                methodConversions = methodConversions Or
                                    Conversions.ClassifyMethodConversionBasedOnReturn(targetMethodSymbol.ReturnType, targetMethodSymbol.ReturnsByRef,
                                                                                      toMethod.ReturnType, toMethod.ReturnsByRef, useSiteInfo)

                If diagnostics.Add(addressOfOperandSyntax, useSiteInfo) Then
                    ' Suppress additional diagnostics 
                    diagnostics = BindingDiagnosticBag.Discarded
                End If
            End If

            If useZeroArgumentRelaxation Then
                Debug.Assert(toMethod.ParameterCount > 0)

                ' special flag for ignoring all arguments (zero argument relaxation)
                If targetMethodSymbol.ParameterCount = 0 Then
                    methodConversions = methodConversions Or MethodConversionKind.AllArgumentsIgnored
                Else
                    ' We can get here if all method's parameters are Optional/ParamArray, however, 
                    ' according to the language spec, zero arguments relaxation is allowed only
                    ' if target method has no parameters. Here is the quote:
                    ' "method referenced by the method pointer, but it is not applicable due to
                    ' the fact that it has no parameters and the delegate type does, then the method
                    ' is considered applicable and the parameters are simply ignored."
                    '
                    ' There is a bug in Dev10, sometimes it erroneously allows zero-argument relaxation against
                    ' a method with optional parameters, if parameters of the delegate invoke can be passed to 
                    ' the method (i.e. without dropping them). See unit-test Bug12211 for an example.
                    methodConversions = methodConversions Or MethodConversionKind.Error_IllegalToIgnoreAllArguments
                End If
            Else
                ' determine conversions based on arguments
                methodConversions = methodConversions Or GetDelegateMethodConversionBasedOnArguments(analysisResult, toMethod, useSiteInfo)

                If diagnostics.Add(addressOfOperandSyntax, useSiteInfo) Then
                    ' Suppress additional diagnostics 
                    diagnostics = BindingDiagnosticBag.Discarded
                End If
            End If

            ' Stubs for ByRef returning methods are not supported.
            ' We could easily support a stub for the case when return value is dropped,
            ' but enabling other kinds of stubs later can lead to breaking changes
            ' because those relaxations could be "better".
            If Not ignoreMethodReturnType AndAlso targetMethodSymbol.ReturnsByRef AndAlso
               Conversions.IsDelegateRelaxationSupportedFor(methodConversions) AndAlso
               Conversions.IsStubRequiredForMethodConversion(methodConversions) Then
                methodConversions = methodConversions Or MethodConversionKind.Error_StubNotSupported
            End If

            If Conversions.IsDelegateRelaxationSupportedFor(methodConversions) Then
                diagnostics.AddRange(analysisResult.TypeArgumentInferenceDiagnosticsOpt)

                If addressOfExpression.MethodGroup.ResultKind = LookupResultKind.Good Then
                    addressOfExpression.Binder.CheckMemberTypeAccessibility(diagnostics, addressOfOperandSyntax, targetMethodSymbol)
                    Return New KeyValuePair(Of MethodSymbol, MethodConversionKind)(targetMethodSymbol, methodConversions)
                End If

                methodConversions = methodConversions Or MethodConversionKind.Error_Unspecified
            Else
                ReportDelegateBindingIncompatible(
                    addressOfOperandSyntax,
                    toMethod.ContainingType,
                    targetMethodSymbol,
                    diagnostics)
            End If

            Debug.Assert((methodConversions And MethodConversionKind.AllErrorReasons) <> 0)

            If addressOfExpression.MethodGroup.ResultKind = LookupResultKind.Inaccessible Then
                ReportDiagnostic(diagnostics, addressOfOperandSyntax,
                                 addressOfExpression.Binder.GetInaccessibleErrorInfo(
                                    analysisResult.Candidate.UnderlyingSymbol))
            Else
                Debug.Assert(addressOfExpression.MethodGroup.ResultKind = LookupResultKind.Good)
            End If

            Return New KeyValuePair(Of MethodSymbol, MethodConversionKind)(Nothing, methodConversions)
        End Function

        Private Shared Sub ReportDelegateBindingMismatchStrictOff(
            syntax As SyntaxNode,
            delegateType As NamedTypeSymbol,
            targetMethodSymbol As MethodSymbol,
            diagnostics As BindingDiagnosticBag
        )
            ' Option Strict On does not allow narrowing in implicit type conversion between method '{0}' and delegate "{1}".
            If targetMethodSymbol.ReducedFrom Is Nothing Then
                ReportDiagnostic(diagnostics,
                                       syntax,
                                       ERRID.ERR_DelegateBindingMismatchStrictOff2,
                                       targetMethodSymbol,
                                       CustomSymbolDisplayFormatter.DelegateSignature(delegateType))
            Else
                ' This is an extension method.
                ReportDiagnostic(diagnostics,
                                       syntax,
                                       ERRID.ERR_DelegateBindingMismatchStrictOff3,
                                       targetMethodSymbol,
                                       CustomSymbolDisplayFormatter.DelegateSignature(delegateType),
                                       targetMethodSymbol.ContainingType)
            End If
        End Sub

        Private Shared Sub ReportDelegateBindingIncompatible(
            syntax As SyntaxNode,
            delegateType As NamedTypeSymbol,
            targetMethodSymbol As MethodSymbol,
            diagnostics As BindingDiagnosticBag
        )
            ' Option Strict On does not allow narrowing in implicit type conversion between method '{0}' and delegate "{1}".
            If targetMethodSymbol.ReducedFrom Is Nothing Then
                ReportDiagnostic(diagnostics,
                                       syntax,
                                       ERRID.ERR_DelegateBindingIncompatible2,
                                       targetMethodSymbol,
                                       CustomSymbolDisplayFormatter.DelegateSignature(delegateType))
            Else
                ' This is an extension method.
                ReportDiagnostic(diagnostics,
                                       syntax,
                                       ERRID.ERR_DelegateBindingIncompatible3,
                                       targetMethodSymbol,
                                       CustomSymbolDisplayFormatter.DelegateSignature(delegateType),
                                       targetMethodSymbol.ContainingType)
            End If
        End Sub

        ''' <summary>
        ''' Determines the method conversion for delegates based on the arguments.
        ''' </summary>
        ''' <param name="bestResult">The resolution result.</param>
        ''' <param name="delegateInvoke">The delegate invoke method.</param>
        Private Shared Function GetDelegateMethodConversionBasedOnArguments(
            bestResult As OverloadResolution.CandidateAnalysisResult,
            delegateInvoke As MethodSymbol,
            <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)
        ) As MethodConversionKind
            Dim methodConversions As MethodConversionKind = MethodConversionKind.Identity

            ' in contrast to the native compiler we know that there is a legal conversion and we do not 
            ' need to classify invalid conversions.
            ' however there is still the ParamArray expansion that needs special treatment.
            ' if there is one conversion needed, the array ConversionsOpt contains all conversions for all used parameters
            ' (including e.g. identity conversion). If a ParamArray was expanded, there will be a conversion for each 
            ' expanded parameter.

            Dim bestCandidate As OverloadResolution.Candidate = bestResult.Candidate
            Dim candidateParameterCount = bestCandidate.ParameterCount
            Dim candidateLastParameterIndex = candidateParameterCount - 1
            Dim delegateParameterCount = delegateInvoke.ParameterCount
            Dim lastCommonIndex = Math.Min(candidateParameterCount, delegateParameterCount) - 1

            ' IsExpandedParamArrayForm is true if there was no, one or more parameters given for the ParamArray 
            ' Note: if an array was passed, IsExpandedParamArrayForm is false.
            If bestResult.IsExpandedParamArrayForm Then
                ' Dev10 always sets the ExcessOptionalArgumentsOnTarget whenever the last parameter of the target was a 
                ' ParamArray. This forces a stub for the ParamArray conversion, that is needed for the ParamArray in any case.
                methodConversions = methodConversions Or MethodConversionKind.ExcessOptionalArgumentsOnTarget

            ElseIf candidateParameterCount > delegateParameterCount Then
                ' An omission of optional parameters for expanded ParamArray form doesn't add anything new for 
                ' the method conversion. Non-expanded ParamArray form, would be dismissed by overload resolution 
                ' if there were omitted optional parameters because it is illegal to omit the ParamArray argument 
                ' in non-expanded form.

                ' there are optional parameters that have not been exercised by the delegate.
                ' e.g. Delegate Sub(b As Byte) -> Sub Target(b As Byte, Optional c as Byte)
                methodConversions = methodConversions Or MethodConversionKind.ExcessOptionalArgumentsOnTarget
#If DEBUG Then
                ' check that all unused parameters on the target are optional
                For parameterIndex = delegateParameterCount To candidateParameterCount - 1
                    Debug.Assert(bestCandidate.Parameters(parameterIndex).IsOptional)
                Next
#End If
            ElseIf lastCommonIndex >= 0 AndAlso
                    bestCandidate.Parameters(lastCommonIndex).IsParamArray AndAlso
                    delegateInvoke.Parameters(lastCommonIndex).IsByRef AndAlso
                    bestCandidate.Parameters(lastCommonIndex).IsByRef AndAlso
                    Not bestResult.ConversionsOpt.IsDefaultOrEmpty AndAlso
                    Not Conversions.IsIdentityConversion(bestResult.ConversionsOpt(lastCommonIndex).Key) Then

                ' Dev10 has the following behavior that needs to be re-implemented:
                ' Using
                ' Sub Target(ByRef Base()) 
                ' with a
                ' Delegate Sub Del(ByRef ParamArray Base()) 
                ' does not create a stub and the values are transported ByRef
                ' however using a
                ' Sub Target(ByRef ParamArray Base())
                ' with a 
                ' Delegate Del(ByRef Derived()) (with or without ParamArray, works with Option Strict Off only)
                ' creates a stub and transports the values ByVal.
                ' Note: if the ParamArray is not expanded, the parameter count must match

                Debug.Assert(candidateParameterCount = delegateParameterCount)
                Debug.Assert(Conversions.IsWideningConversion(bestResult.ConversionsOpt(lastCommonIndex).Key))

                Dim conv = Conversions.ClassifyConversion(bestCandidate.Parameters(lastCommonIndex).Type,
                                                          delegateInvoke.Parameters(lastCommonIndex).Type,
                                                          useSiteInfo)

                methodConversions = methodConversions Or
                                    Conversions.ClassifyMethodConversionBasedOnArgumentConversion(conv.Key,
                                                                                                  delegateInvoke.Parameters(lastCommonIndex).Type)
            End If

            ' the overload resolution does not consider ByRef/ByVal mismatches, so we need to check the  
            ' parameters here.
            ' first iterate over the common parameters
            For parameterIndex = 0 To lastCommonIndex
                If delegateInvoke.Parameters(parameterIndex).IsByRef <> bestCandidate.Parameters(parameterIndex).IsByRef Then
                    methodConversions = methodConversions Or MethodConversionKind.Error_ByRefByValMismatch
                    Exit For
                End If
            Next
            ' after the loop above the remaining parameters on the target can only be optional and/or a ParamArray 
            If bestResult.IsExpandedParamArrayForm AndAlso
                (methodConversions And MethodConversionKind.Error_ByRefByValMismatch) <> MethodConversionKind.Error_ByRefByValMismatch Then
                ' if delegateParameterCount is smaller than targetParameterCount the for loop does not 
                ' execute
                Dim lastTargetParameterIsByRef = bestCandidate.Parameters(candidateLastParameterIndex).IsByRef
                Debug.Assert(bestCandidate.Parameters(candidateLastParameterIndex).IsParamArray)
                For parameterIndex = lastCommonIndex + 1 To delegateParameterCount - 1
                    ' test against the last parameter of the target method
                    If delegateInvoke.Parameters(parameterIndex).IsByRef <> lastTargetParameterIsByRef Then
                        methodConversions = methodConversions Or MethodConversionKind.Error_ByRefByValMismatch
                        Exit For
                    End If
                Next
            End If

            ' there have been conversions, check them all
            If Not bestResult.ConversionsOpt.IsDefaultOrEmpty Then
                For conversionIndex = 0 To bestResult.ConversionsOpt.Length - 1
                    Dim conversion = bestResult.ConversionsOpt(conversionIndex)
                    Dim delegateParameterType = delegateInvoke.Parameters(conversionIndex).Type
                    methodConversions = methodConversions Or
                                        Conversions.ClassifyMethodConversionBasedOnArgumentConversion(conversion.Key,
                                                                                                      delegateParameterType)
                Next
            End If

            ' in case of ByRef, there might also be backward conversions
            If Not bestResult.ConversionsBackOpt.IsDefaultOrEmpty Then
                For conversionIndex = 0 To bestResult.ConversionsBackOpt.Length - 1
                    Dim conversion = bestResult.ConversionsBackOpt(conversionIndex)
                    If Not Conversions.IsIdentityConversion(conversion.Key) Then
                        Dim targetMethodParameterType = bestCandidate.Parameters(conversionIndex).Type
                        methodConversions = methodConversions Or
                                            Conversions.ClassifyMethodConversionBasedOnArgumentConversion(conversion.Key,
                                                                                                          targetMethodParameterType)
                    End If
                Next
            End If

            Return methodConversions
        End Function

        ''' <summary>
        ''' Classifies the address of conversion. 
        ''' </summary>
        ''' <param name="source">The bound AddressOf expression.</param>
        ''' <param name="destination">The target type to convert this AddressOf expression to.</param><returns></returns>
        Friend Shared Function ClassifyAddressOfConversion(
            source As BoundAddressOfOperator,
            destination As TypeSymbol
        ) As ConversionKind
            Return source.GetConversionClassification(destination)
        End Function

        Private Shared ReadOnly s_checkDelegateParameterModifierCallback As CheckParameterModifierDelegate = AddressOf CheckDelegateParameterModifier

        ''' <summary>
        ''' Checks if a parameter is a ParamArray and reports this as an error.
        ''' </summary>
        ''' <param name="container">The containing type.</param>
        ''' <param name="token">The current parameter token.</param>
        ''' <param name="flag">The flags of this parameter.</param>
        ''' <param name="diagnostics">The diagnostics.</param>
        Private Shared Function CheckDelegateParameterModifier(
            container As Symbol,
            token As SyntaxToken,
            flag As SourceParameterFlags,
            diagnostics As BindingDiagnosticBag
        ) As SourceParameterFlags
            ' 9.2.5.4: ParamArray parameters may not be specified in delegate or event declarations.
            If (flag And SourceParameterFlags.ParamArray) = SourceParameterFlags.ParamArray Then
                Dim location = token.GetLocation()
                diagnostics.Add(ERRID.ERR_ParamArrayIllegal1, location, GetDelegateOrEventKeywordText(container))
                flag = flag And (Not SourceParameterFlags.ParamArray)
            End If

            ' 9.2.5.3 Optional parameters may not be specified on delegate or event declarations
            If (flag And SourceParameterFlags.Optional) = SourceParameterFlags.Optional Then
                Dim location = token.GetLocation()
                diagnostics.Add(ERRID.ERR_OptionalIllegal1, location, GetDelegateOrEventKeywordText(container))
                flag = flag And (Not SourceParameterFlags.Optional)
            End If

            Return flag
        End Function

        Private Shared Function GetDelegateOrEventKeywordText(sym As Symbol) As String
            Dim keyword As SyntaxKind
            If sym.Kind = SymbolKind.Event Then
                keyword = SyntaxKind.EventKeyword
            ElseIf TypeOf sym.ContainingType Is SynthesizedEventDelegateSymbol Then
                keyword = SyntaxKind.EventKeyword
            Else
                keyword = SyntaxKind.DelegateKeyword
            End If
            Return SyntaxFacts.GetText(keyword)
        End Function

        ''' <summary>
        ''' Reclassifies the bound address of operator into a delegate creation expression (if there is no delegate 
        ''' relaxation required) or into a bound lambda expression (which gets a delegate creation expression later on)
        ''' </summary>
        ''' <param name="addressOfExpression">The AddressOf expression.</param>
        ''' <param name="delegateResolutionResult">The delegate resolution result.</param>
        ''' <param name="targetType">Type of the target.</param>
        ''' <param name="diagnostics">The diagnostics.</param><returns></returns>
        Friend Function ReclassifyAddressOf(
            addressOfExpression As BoundAddressOfOperator,
            ByRef delegateResolutionResult As DelegateResolutionResult,
            targetType As TypeSymbol,
            diagnostics As BindingDiagnosticBag,
            isForHandles As Boolean,
            warnIfResultOfAsyncMethodIsDroppedDueToRelaxation As Boolean
        ) As BoundExpression

            If addressOfExpression.HasErrors Then
                Return addressOfExpression
            End If

            Dim boundLambda As BoundLambda = Nothing
            Dim relaxationReceiverPlaceholder As BoundRValuePlaceholder = Nothing

            Dim syntaxNode = addressOfExpression.Syntax

            Dim targetMethod As MethodSymbol = delegateResolutionResult.Target
            Dim reducedFromDefinition As MethodSymbol = targetMethod.ReducedFrom

            Dim sourceMethodGroup = addressOfExpression.MethodGroup
            Dim receiver As BoundExpression = sourceMethodGroup.ReceiverOpt

            Dim resolvedTypeOrValueReceiver As BoundExpression = Nothing
            If receiver IsNot Nothing AndAlso
                Not addressOfExpression.HasErrors AndAlso
                Not delegateResolutionResult.Diagnostics.Diagnostics.HasAnyErrors Then

                receiver = AdjustReceiverTypeOrValue(receiver, receiver.Syntax, targetMethod.IsShared, diagnostics, resolvedTypeOrValueReceiver)
            End If

            If Me.OptionStrict = OptionStrict.On AndAlso Conversions.IsNarrowingConversion(delegateResolutionResult.DelegateConversions) Then

                Dim addressOfOperandSyntax = addressOfExpression.Syntax
                If addressOfOperandSyntax.Kind = SyntaxKind.AddressOfExpression Then
                    addressOfOperandSyntax = DirectCast(addressOfOperandSyntax, UnaryExpressionSyntax).Operand
                End If

                ' Option Strict On does not allow narrowing in implicit type conversion between method '{0}' and delegate "{1}".
                ReportDelegateBindingMismatchStrictOff(addressOfOperandSyntax, DirectCast(targetType, NamedTypeSymbol), targetMethod, diagnostics)
            Else

                ' When the target method is an extension method, we are creating so called curried delegate.
                ' However, CLR doesn't support creating curried delegates that close over a ByRef 'this' argument.
                ' A similar problem exists when the 'this' argument is a value type. For these cases we need a stub too, 
                ' but they are not covered by MethodConversionKind.
                If Conversions.IsStubRequiredForMethodConversion(delegateResolutionResult.MethodConversions) OrElse
                   (reducedFromDefinition IsNot Nothing AndAlso
                        (reducedFromDefinition.Parameters(0).IsByRef OrElse
                         targetMethod.ReceiverType.IsTypeParameter() OrElse
                         targetMethod.ReceiverType.IsValueType)) Then

                    ' because of a delegate relaxation there is a conversion needed to create a delegate instance.
                    ' We will create a lambda with the exact signature of the delegate. This lambda itself will 
                    ' call the target method.

                    boundLambda = BuildDelegateRelaxationLambda(syntaxNode, sourceMethodGroup.Syntax, receiver, targetMethod,
                                                                sourceMethodGroup.TypeArgumentsOpt, sourceMethodGroup.QualificationKind,
                                                                DirectCast(targetType, NamedTypeSymbol).DelegateInvokeMethod,
                                                                delegateResolutionResult.DelegateConversions And ConversionKind.DelegateRelaxationLevelMask,
                                                                isZeroArgumentKnownToBeUsed:=(delegateResolutionResult.MethodConversions And MethodConversionKind.AllArgumentsIgnored) <> 0,
                                                                diagnostics:=diagnostics,
                                                                warnIfResultOfAsyncMethodIsDroppedDueToRelaxation:=warnIfResultOfAsyncMethodIsDroppedDueToRelaxation,
                                                                relaxationReceiverPlaceholder:=relaxationReceiverPlaceholder)
                End If
            End If

            Dim target As MethodSymbol = delegateResolutionResult.Target

            ' Check if the target is a partial method without implementation provided
            If Not isForHandles AndAlso target.IsPartialWithoutImplementation Then
                ReportDiagnostic(diagnostics, addressOfExpression.MethodGroup.Syntax, ERRID.ERR_NoPartialMethodInAddressOf1, target)
            End If

            Dim newReceiver As BoundExpression
            If receiver IsNot Nothing Then
                If receiver.IsPropertyOrXmlPropertyAccess() Then
                    receiver = MakeRValue(receiver, diagnostics)
                End If
                newReceiver = Nothing
            Else
                newReceiver = If(resolvedTypeOrValueReceiver, sourceMethodGroup.ReceiverOpt)
            End If

            sourceMethodGroup = sourceMethodGroup.Update(sourceMethodGroup.TypeArgumentsOpt,
                                                         sourceMethodGroup.Methods,
                                                         sourceMethodGroup.PendingExtensionMethodsOpt,
                                                         sourceMethodGroup.ResultKind,
                                                         newReceiver,
                                                         sourceMethodGroup.QualificationKind)

            ' the delegate creation has the lambda stored internally to not clutter the bound tree with synthesized nodes 
            ' in the first pass. Later on in the DelegateRewriter the node get's rewritten with the lambda if needed.
            Return New BoundDelegateCreationExpression(syntaxNode,
                                                       receiver,
                                                       target,
                                                       boundLambda,
                                                       relaxationReceiverPlaceholder,
                                                       sourceMethodGroup,
                                                       targetType,
                                                       hasErrors:=False)
        End Function

        Private Function BuildDelegateRelaxationLambda(
            syntaxNode As SyntaxNode,
            methodGroupSyntax As SyntaxNode,
            receiver As BoundExpression,
            targetMethod As MethodSymbol,
            typeArgumentsOpt As BoundTypeArguments,
            qualificationKind As QualificationKind,
            delegateInvoke As MethodSymbol,
            delegateRelaxation As ConversionKind,
            isZeroArgumentKnownToBeUsed As Boolean,
            warnIfResultOfAsyncMethodIsDroppedDueToRelaxation As Boolean,
            diagnostics As BindingDiagnosticBag,
            <Out()> ByRef relaxationReceiverPlaceholder As BoundRValuePlaceholder
        ) As BoundLambda

            relaxationReceiverPlaceholder = Nothing
            Dim unconstructedTargetMethod As MethodSymbol = targetMethod.ConstructedFrom

            If typeArgumentsOpt Is Nothing AndAlso unconstructedTargetMethod.IsGenericMethod Then
                typeArgumentsOpt = New BoundTypeArguments(methodGroupSyntax,
                                                          targetMethod.TypeArguments)

                typeArgumentsOpt.SetWasCompilerGenerated()
            End If

            Dim actualReceiver As BoundExpression = receiver

            ' Figure out if we need to capture the receiver in a temp before creating the lambda
            ' in order to enforce correct semantics.
            If actualReceiver IsNot Nothing AndAlso actualReceiver.IsValue() AndAlso Not actualReceiver.HasErrors Then
                If actualReceiver.IsInstanceReference() AndAlso targetMethod.ReceiverType.IsReferenceType Then
                    Debug.Assert(Not actualReceiver.Type.IsTypeParameter())
                    Debug.Assert(Not actualReceiver.IsLValue) ' See the comment below why this is important.
                Else
                    ' Will need to capture the receiver in a temp, rewriter do the job. 
                    relaxationReceiverPlaceholder = New BoundRValuePlaceholder(actualReceiver.Syntax, actualReceiver.Type)
                    actualReceiver = relaxationReceiverPlaceholder
                End If
            End If

            Dim methodGroup = New BoundMethodGroup(methodGroupSyntax,
                                                   typeArgumentsOpt,
                                                   ImmutableArray.Create(unconstructedTargetMethod),
                                                   LookupResultKind.Good,
                                                   actualReceiver,
                                                   qualificationKind)
            methodGroup.SetWasCompilerGenerated()

            Return BuildDelegateRelaxationLambda(syntaxNode,
                                                 delegateInvoke,
                                                 methodGroup,
                                                 delegateRelaxation,
                                                 isZeroArgumentKnownToBeUsed,
                                                 warnIfResultOfAsyncMethodIsDroppedDueToRelaxation,
                                                 diagnostics)
        End Function

        ''' <summary>
        ''' Build a lambda that has a shape of the [delegateInvoke] and calls 
        ''' the only method from the [methodGroup] passing all parameters of the lambda
        ''' as arguments for the call.
        ''' Note, that usually the receiver of the [methodGroup] should be captured before entering the 
        ''' relaxation lambda in order to prevent its reevaluation every time the lambda is invoked and 
        ''' prevent its mutation. 
        ''' 
        '''             !!! Therefore, it is not common to call this overload directly. !!!
        ''' 
        ''' </summary>
        ''' <param name="syntaxNode">Location to use for various synthetic nodes and symbols.</param>
        ''' <param name="delegateInvoke">The Invoke method to "implement".</param>
        ''' <param name="methodGroup">The method group with the only method in it.</param>
        ''' <param name="delegateRelaxation">Delegate relaxation to store within the new BoundLambda node.</param>
        ''' <param name="diagnostics"></param>
        Private Function BuildDelegateRelaxationLambda(
            syntaxNode As SyntaxNode,
            delegateInvoke As MethodSymbol,
            methodGroup As BoundMethodGroup,
            delegateRelaxation As ConversionKind,
            isZeroArgumentKnownToBeUsed As Boolean,
            warnIfResultOfAsyncMethodIsDroppedDueToRelaxation As Boolean,
            diagnostics As BindingDiagnosticBag
        ) As BoundLambda
            Debug.Assert(delegateInvoke.MethodKind = MethodKind.DelegateInvoke)
            Debug.Assert(methodGroup.Methods.Length = 1)
            Debug.Assert(methodGroup.PendingExtensionMethodsOpt Is Nothing)
            Debug.Assert((delegateRelaxation And (Not ConversionKind.DelegateRelaxationLevelMask)) = 0)

            ' build lambda symbol parameters matching the invocation method exactly. To do this,
            ' we'll create a BoundLambdaParameterSymbol for each parameter of the invoke method.
            Dim delegateInvokeReturnType = delegateInvoke.ReturnType
            Dim invokeParameters = delegateInvoke.Parameters
            Dim invokeParameterCount = invokeParameters.Length

            Dim lambdaSymbolParameters(invokeParameterCount - 1) As BoundLambdaParameterSymbol
            Dim addressOfLocation As Location = syntaxNode.GetLocation()

            For parameterIndex = 0 To invokeParameterCount - 1
                Dim parameter = invokeParameters(parameterIndex)
                lambdaSymbolParameters(parameterIndex) = New BoundLambdaParameterSymbol(GeneratedNames.MakeDelegateRelaxationParameterName(parameterIndex),
                                                                                        parameter.Ordinal,
                                                                                        parameter.Type,
                                                                                        parameter.IsByRef,
                                                                                        syntaxNode,
                                                                                        addressOfLocation)
            Next

            ' even if the return value is dropped, we're using the delegate's return type for 
            ' this lambda symbol.
            Dim lambdaSymbol = New SynthesizedLambdaSymbol(SynthesizedLambdaKind.DelegateRelaxationStub,
                                                           syntaxNode,
                                                           lambdaSymbolParameters.AsImmutable(),
                                                           delegateInvokeReturnType,
                                                           Me)

            ' the body of the lambda only contains a call to the target (or a return of the return value of 
            ' the call in case of a function)

            ' for each parameter of the lambda symbol/invoke method we will create a bound parameter, except
            ' we are implementing a zero argument relaxation.
            ' These parameters will be used in the method invocation as passed parameters.
            Dim method As MethodSymbol = methodGroup.Methods(0)
            Dim droppedArguments = isZeroArgumentKnownToBeUsed OrElse (invokeParameterCount > 0 AndAlso method.ParameterCount = 0)
            Dim targetParameterCount = If(droppedArguments, 0, invokeParameterCount)
            Dim lambdaBoundParameters(targetParameterCount - 1) As BoundExpression

            If Not droppedArguments Then
                For parameterIndex = 0 To lambdaSymbolParameters.Length - 1
                    Dim lambdaSymbolParameter = lambdaSymbolParameters(parameterIndex)
                    Dim boundParameter = New BoundParameter(syntaxNode,
                                                            lambdaSymbolParameter,
                                                            lambdaSymbolParameter.Type)
                    boundParameter.SetWasCompilerGenerated()
                    lambdaBoundParameters(parameterIndex) = boundParameter
                Next
            End If

            'The invocation of the target method must be bound in the context of the lambda
            'The reason is that binding the invoke may introduce local symbols and they need 
            'to be properly parented to the lambda and not to the outer method.
            Dim lambdaBinder = New LambdaBodyBinder(lambdaSymbol, Me)

            ' Dev10 ignores the type characters used in the operand of an AddressOf operator.
            ' NOTE: we suppress suppressAbstractCallDiagnostics because it 
            '       should have been reported already
            Dim boundInvocationExpression As BoundExpression = lambdaBinder.BindInvocationExpression(syntaxNode,
                                                                                        syntaxNode,
                                                                                        TypeCharacter.None,
                                                                                        methodGroup,
                                                                                        lambdaBoundParameters.AsImmutable(),
                                                                                        Nothing,
                                                                                        diagnostics,
                                                                                        suppressAbstractCallDiagnostics:=True,
                                                                                        callerInfoOpt:=Nothing)
            boundInvocationExpression.SetWasCompilerGenerated()

            ' In case of a function target that got assigned to a sub delegate, the return value will be dropped
            Dim statementList As ImmutableArray(Of BoundStatement) = Nothing
            If lambdaSymbol.IsSub Then
                Dim statements(1) As BoundStatement
                Dim boundStatement As BoundStatement = New BoundExpressionStatement(syntaxNode, boundInvocationExpression)
                boundStatement.SetWasCompilerGenerated()
                statements(0) = boundStatement
                boundStatement = New BoundReturnStatement(syntaxNode, Nothing, Nothing, Nothing)
                boundStatement.SetWasCompilerGenerated()
                statements(1) = boundStatement
                statementList = statements.AsImmutableOrNull

                If warnIfResultOfAsyncMethodIsDroppedDueToRelaxation AndAlso
                   Not method.IsSub Then

                    If Not method.IsAsync Then
                        warnIfResultOfAsyncMethodIsDroppedDueToRelaxation = False

                        If method.MethodKind = MethodKind.DelegateInvoke AndAlso
                           methodGroup.ReceiverOpt IsNot Nothing AndAlso
                           methodGroup.ReceiverOpt.Kind = BoundKind.Conversion Then
                            Dim receiver = DirectCast(methodGroup.ReceiverOpt, BoundConversion)

                            If Not receiver.ExplicitCastInCode AndAlso
                               receiver.Operand.Kind = BoundKind.Lambda AndAlso
                               DirectCast(receiver.Operand, BoundLambda).LambdaSymbol.IsAsync AndAlso
                               receiver.Type.IsDelegateType() AndAlso
                               receiver.Type.IsAnonymousType Then
                                warnIfResultOfAsyncMethodIsDroppedDueToRelaxation = True
                            End If
                        End If
                    Else
                        warnIfResultOfAsyncMethodIsDroppedDueToRelaxation = method.ContainingAssembly Is Compilation.Assembly
                    End If

                    If warnIfResultOfAsyncMethodIsDroppedDueToRelaxation Then
                        ReportDiagnostic(diagnostics, syntaxNode, ERRID.WRN_UnobservedAwaitableDelegate)
                    End If
                End If
            Else
                ' process conversions between the return types of the target and invoke function if needed.
                boundInvocationExpression = lambdaBinder.ApplyImplicitConversion(syntaxNode,
                                                                                 delegateInvokeReturnType,
                                                                                 boundInvocationExpression,
                                                                                 diagnostics)

                Dim returnstmt As BoundStatement = New BoundReturnStatement(syntaxNode,
                                                                            boundInvocationExpression,
                                                                            Nothing,
                                                                            Nothing)
                returnstmt.SetWasCompilerGenerated()
                statementList = ImmutableArray.Create(returnstmt)
            End If

            Dim lambdaBody = New BoundBlock(syntaxNode,
                                            Nothing,
                                            ImmutableArray(Of LocalSymbol).Empty,
                                            statementList)
            lambdaBody.SetWasCompilerGenerated()

            Dim boundLambda = New BoundLambda(syntaxNode,
                                          lambdaSymbol,
                                          lambdaBody,
                                          ReadOnlyBindingDiagnostic(Of AssemblySymbol).Empty,
                                          Nothing,
                                          delegateRelaxation,
                                          MethodConversionKind.Identity)
            boundLambda.SetWasCompilerGenerated()

            Return boundLambda
        End Function

    End Class
End Namespace
