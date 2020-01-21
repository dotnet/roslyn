' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' The only public entry point is the Infer method.
    ''' </summary>
    Friend MustInherit Class TypeArgumentInference

        Public Shared Function Infer(
            candidate As MethodSymbol,
            arguments As ImmutableArray(Of BoundExpression),
            parameterToArgumentMap As ArrayBuilder(Of Integer),
            paramArrayItems As ArrayBuilder(Of Integer),
            delegateReturnType As TypeSymbol,
            delegateReturnTypeReferenceBoundNode As BoundNode,
            ByRef typeArguments As ImmutableArray(Of TypeSymbol),
            ByRef inferenceLevel As InferenceLevel,
            ByRef allFailedInferenceIsDueToObject As Boolean,
            ByRef someInferenceFailed As Boolean,
            ByRef inferenceErrorReasons As InferenceErrorReasons,
            <Out> ByRef inferredTypeByAssumption As BitVector,
            <Out> ByRef typeArgumentsLocation As ImmutableArray(Of SyntaxNodeOrToken),
            <[In](), Out()> ByRef asyncLambdaSubToFunctionMismatch As HashSet(Of BoundExpression),
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo),
            ByRef diagnostic As DiagnosticBag,
            Optional inferTheseTypeParameters As BitVector = Nothing
        ) As Boolean
            Debug.Assert(candidate Is candidate.ConstructedFrom)

            Return InferenceGraph.Infer(candidate, arguments, parameterToArgumentMap, paramArrayItems, delegateReturnType, delegateReturnTypeReferenceBoundNode,
                                        typeArguments, inferenceLevel, allFailedInferenceIsDueToObject, someInferenceFailed, inferenceErrorReasons,
                                        inferredTypeByAssumption, typeArgumentsLocation, asyncLambdaSubToFunctionMismatch,
                                        useSiteDiagnostics, diagnostic, inferTheseTypeParameters)
        End Function

        ' No-one should create instances of this class.
        Private Sub New()
        End Sub

        Public Enum InferenceLevel As Byte
            None = 0
            ' None is used to indicate uninitialized  but semantically it should not matter if there is a whidbey delegate
            ' or no delegate in the overload resolution hence both have value 0 such that overload resolution 
            ' will not prefer a non inferred method over an inferred one.
            Whidbey = 0
            Orcas = 1

            ' Keep invalid the biggest number
            Invalid = 2
        End Enum

        ' MatchGenericArgumentParameter:
        ' This is used in type inference, when matching an argument e.g. Arg(Of String) against a parameter Parm(Of T).
        ' In covariant contexts e.g. Action(Of _), the two match if Arg <= Parm (i.e. Arg inherits/implements Parm).
        ' In contravariant contexts e.g. IEnumerable(Of _), the two match if Parm <= Arg (i.e. Parm inherits/implements Arg).
        ' In invariant contexts e.g. List(Of _), the two match only if Arg and Parm are identical.
        ' Note: remember that rank-1 arrays T() implement IEnumerable(Of T), IList(Of T) and ICollection(Of T).
        Public Enum MatchGenericArgumentToParameter
            MatchBaseOfGenericArgumentToParameter
            MatchArgumentToBaseOfGenericParameter
            MatchGenericArgumentToParameterExactly
        End Enum

        Private Enum InferenceNodeType As Byte
            ArgumentNode
            TypeParameterNode
        End Enum

        Private MustInherit Class InferenceNode
            Inherits GraphNode(Of InferenceNode)

            Public ReadOnly NodeType As InferenceNodeType
            Public InferenceComplete As Boolean

            Protected Sub New(graph As InferenceGraph, nodeType As InferenceNodeType)
                MyBase.New(graph)
                Me.NodeType = nodeType
            End Sub

            Public Shadows ReadOnly Property Graph As InferenceGraph
                Get
                    Return DirectCast(MyBase.Graph, InferenceGraph)
                End Get
            End Property

            ''' <summary>
            ''' Returns True if the inference algorithm should be restarted.
            ''' </summary>
            Public MustOverride Function InferTypeAndPropagateHints() As Boolean

            <Conditional("DEBUG")>
            Public Sub VerifyIncomingInferenceComplete(
                    ByVal nodeType As InferenceNodeType
                )
                If Not Graph.SomeInferenceHasFailed() Then
                    For Each current As InferenceNode In IncomingEdges
                        Debug.Assert(current.NodeType = nodeType, "Should only have expected incoming edges.")
                        Debug.Assert(current.InferenceComplete, "Should have inferred type already")
                    Next
                End If
            End Sub

        End Class

        Private Class DominantTypeDataTypeInference
            Inherits DominantTypeData

            ' Fields needed for error reporting
            Public ByAssumption As Boolean ' was ResultType chosen by assumption or intention?
            Public Parameter As ParameterSymbol
            Public InferredFromObject As Boolean
            Public TypeParameter As TypeParameterSymbol
            Public ArgumentLocation As SyntaxNode

        End Class

        Private Class TypeParameterNode
            Inherits InferenceNode

            Public ReadOnly DeclaredTypeParam As TypeParameterSymbol
            Public ReadOnly InferenceTypeCollection As TypeInferenceCollection(Of DominantTypeDataTypeInference)

            Private _inferredType As TypeSymbol
            Private _inferredFromLocation As SyntaxNodeOrToken
            Private _inferredTypeByAssumption As Boolean

            ' TODO: Dev10 has two locations to track type inferred so far. 
            '       One that can be changed with time and the other one that cannot be changed.
            '       This one, cannot be changed once set. We need to clean this up later.
            Private _candidateInferredType As TypeSymbol

            Private _parameter As ParameterSymbol

            Public Sub New(graph As InferenceGraph, typeParameter As TypeParameterSymbol)
                MyBase.New(graph, InferenceNodeType.TypeParameterNode)

                DeclaredTypeParam = typeParameter
                InferenceTypeCollection = New TypeInferenceCollection(Of DominantTypeDataTypeInference)()

            End Sub

            Public ReadOnly Property InferredType As TypeSymbol
                Get
                    Return _inferredType
                End Get
            End Property

            Public ReadOnly Property CandidateInferredType As TypeSymbol
                Get
                    Return _candidateInferredType
                End Get
            End Property

            Public ReadOnly Property InferredFromLocation As SyntaxNodeOrToken
                Get
                    Return _inferredFromLocation
                End Get
            End Property

            Public ReadOnly Property InferredTypeByAssumption As Boolean
                Get
                    Return _inferredTypeByAssumption
                End Get
            End Property

            Public Sub RegisterInferredType(inferredType As TypeSymbol, inferredFromLocation As SyntaxNodeOrToken, inferredTypeByAssumption As Boolean)

                ' Make sure ArrayLiteralTypeSymbol does not leak out
                Dim arrayLiteralType = TryCast(inferredType, ArrayLiteralTypeSymbol)

                If arrayLiteralType IsNot Nothing Then

                    Dim arrayLiteral = arrayLiteralType.ArrayLiteral
                    Dim arrayType = arrayLiteral.InferredType

                    If Not (arrayLiteral.HasDominantType AndAlso arrayLiteral.NumberOfCandidates = 1) AndAlso
                        arrayType.ElementType.SpecialType = SpecialType.System_Object Then

                        ' ReportArrayLiteralInferredTypeDiagnostics in ReclassifyArrayLiteralExpression reports an error
                        ' when option strict is on and the array type is object() and there wasn't a dominant type. However,
                        ' Dev10 does not report this error when inferring a type parameter's type. Create a new object() type
                        ' to suppress the error.

                        inferredType = ArrayTypeSymbol.CreateVBArray(arrayType.ElementType, Nothing, arrayType.Rank, arrayLiteral.Binder.Compilation.Assembly)
                    Else
                        inferredType = arrayLiteral.InferredType
                    End If

                End If

                Debug.Assert(Not (TypeOf inferredType Is ArrayLiteralTypeSymbol))

                _inferredType = inferredType
                _inferredFromLocation = inferredFromLocation
                _inferredTypeByAssumption = inferredTypeByAssumption

                ' TODO: Dev10 has two locations to track type inferred so far. 
                '       One that can be changed with time and the other one that cannot be changed.
                '       We need to clean this up.
                If _candidateInferredType Is Nothing Then
                    _candidateInferredType = inferredType
                End If
            End Sub

            Public ReadOnly Property Parameter As ParameterSymbol
                Get
                    Return _parameter
                End Get
            End Property

            Public Sub SetParameter(parameter As ParameterSymbol)
                Debug.Assert(_parameter Is Nothing)
                _parameter = parameter
            End Sub


            Public Overrides Function InferTypeAndPropagateHints() As Boolean
                Dim numberOfIncomingEdges As Integer = IncomingEdges.Count
                Dim restartAlgorithm As Boolean = False
                Dim argumentLocation As SyntaxNode
                Dim numberOfIncomingWithNothing As Integer = 0

                If numberOfIncomingEdges > 0 Then
                    argumentLocation = DirectCast(IncomingEdges(0), ArgumentNode).Expression.Syntax
                Else
                    argumentLocation = Nothing
                End If

                Dim numberOfAssertions As Integer = 0
                Dim incomingFromObject As Boolean = False

                Dim list As ArrayBuilder(Of InferenceNode) = IncomingEdges

                For Each currentGraphNode As InferenceNode In IncomingEdges
                    Debug.Assert(currentGraphNode.NodeType = InferenceNodeType.ArgumentNode, "Should only have named nodes as incoming edges.")
                    Dim currentNamedNode = DirectCast(currentGraphNode, ArgumentNode)

                    If currentNamedNode.Expression.Type IsNot Nothing AndAlso
                       currentNamedNode.Expression.Type.IsObjectType() Then
                        incomingFromObject = True
                    End If

                    If Not currentNamedNode.InferenceComplete Then
                        Graph.RemoveEdge(currentNamedNode, Me)
                        restartAlgorithm = True
                        numberOfAssertions += 1
                    Else
                        ' We should not infer from a Nothing literal.
                        If currentNamedNode.Expression.IsStrictNothingLiteral() Then
                            numberOfIncomingWithNothing += 1
                        End If
                    End If
                Next


                If numberOfIncomingEdges > 0 AndAlso numberOfIncomingEdges = numberOfIncomingWithNothing Then
                    '  !! Inference has failed: All incoming type hints, were based on 'Nothing' 
                    Graph.MarkInferenceFailure()
                    Graph.ReportNotFailedInferenceDueToObject()
                End If

                Dim numberOfTypeHints As Integer = InferenceTypeCollection.GetTypeDataList().Count()

                If numberOfTypeHints = 0 Then
                    If numberOfAssertions = numberOfIncomingEdges Then
                        Graph.MarkInferenceLevel(InferenceLevel.Orcas)
                    Else
                        '  !! Inference has failed. No Type hints, and some, not all were assertions, otherwise we would have picked object for strict.
                        RegisterInferredType(Nothing, Nothing, False)
                        Graph.MarkInferenceFailure()

                        If Not incomingFromObject Then
                            Graph.ReportNotFailedInferenceDueToObject()
                        End If
                    End If

                ElseIf numberOfTypeHints = 1 Then
                    Dim typeData As DominantTypeDataTypeInference = InferenceTypeCollection.GetTypeDataList()(0)

                    If argumentLocation Is Nothing AndAlso typeData.ArgumentLocation IsNot Nothing Then
                        argumentLocation = typeData.ArgumentLocation
                    End If

                    RegisterInferredType(typeData.ResultType, argumentLocation, typeData.ByAssumption)
                Else
                    ' Run the whidbey algorithm to see if we are smarter now.
                    Dim firstInferredType As TypeSymbol = Nothing
                    Dim allTypeData As ArrayBuilder(Of DominantTypeDataTypeInference) = InferenceTypeCollection.GetTypeDataList()

                    For Each currentTypeInfo As DominantTypeDataTypeInference In allTypeData
                        If firstInferredType Is Nothing Then
                            firstInferredType = currentTypeInfo.ResultType

                        ElseIf Not firstInferredType.IsSameTypeIgnoringAll(currentTypeInfo.ResultType) Then
                            ' Whidbey failed hard here, in Orcas we added dominant type information.
                            Graph.MarkInferenceLevel(InferenceLevel.Orcas)
                        End If
                    Next

                    Dim dominantTypeDataList = ArrayBuilder(Of DominantTypeDataTypeInference).GetInstance()
                    Dim errorReasons As InferenceErrorReasons = InferenceErrorReasons.Other

                    InferenceTypeCollection.FindDominantType(dominantTypeDataList, errorReasons, Graph.UseSiteDiagnostics)

                    If dominantTypeDataList.Count = 1 Then
                        ' //consider: scottwis
                        ' //              This seems dangerous to me, that we 
                        ' //              remove error reasons here.
                        ' //              Instead of clearing these, what we should be doing is 
                        ' //              asserting that they are not set.
                        ' //              If for some reason they get set, but 
                        ' //              we enter this path, then we have a bug.
                        ' //              This code is just masking any such bugs.
                        errorReasons = errorReasons And (Not (InferenceErrorReasons.Ambiguous Or InferenceErrorReasons.NoBest))

                        Dim typeData As DominantTypeDataTypeInference = dominantTypeDataList(0)
                        RegisterInferredType(typeData.ResultType, typeData.ArgumentLocation, typeData.ByAssumption)

                        ' // Also update the location of the argument for constraint error reporting later on.
                    Else
                        If (errorReasons And InferenceErrorReasons.Ambiguous) <> 0 Then
                            '  !! Inference has failed. Dominant type algorithm found ambiguous types.
                            Graph.ReportAmbiguousInferenceError(dominantTypeDataList)
                        Else
                            ' //consider: scottwis
                            ' //              This code appears to be operating under the assumption that if the error reason is not due to an 
                            ' //              ambiguity then it must be because there was no best match.
                            ' //              We should be asserting here to verify that assertion.

                            '  !! Inference has failed. Dominant type algorithm could not find a dominant type.
                            Graph.ReportIncompatibleInferenceError(allTypeData)
                        End If

                        RegisterInferredType(allTypeData(0).ResultType, argumentLocation, False)
                        Graph.MarkInferenceFailure()
                    End If

                    Graph.RegisterErrorReasons(errorReasons)

                    dominantTypeDataList.Free()
                End If

                InferenceComplete = True

                Return restartAlgorithm
            End Function


            Public Sub AddTypeHint(
                type As TypeSymbol,
                typeByAssumption As Boolean,
                argumentLocation As SyntaxNode,
                parameter As ParameterSymbol,
                inferredFromObject As Boolean,
                inferenceRestrictions As RequiredConversion
            )

                Debug.Assert(Not typeByAssumption OrElse type.IsObjectType() OrElse TypeOf type Is ArrayLiteralTypeSymbol, "unexpected: a type which was 'by assumption', but isn't object or array literal")

                ' Don't add error types to the type argument inference collection.
                If type.IsErrorType Then
                    Return
                End If

                Dim foundInList As Boolean = False

                ' Do not merge array literals with other expressions
                If TypeOf type IsNot ArrayLiteralTypeSymbol Then
                    For Each competitor As DominantTypeDataTypeInference In InferenceTypeCollection.GetTypeDataList()

                        ' Do not merge array literals with other expressions
                        If TypeOf competitor.ResultType IsNot ArrayLiteralTypeSymbol AndAlso type.IsSameTypeIgnoringAll(competitor.ResultType) Then
                            competitor.ResultType = TypeInferenceCollection.MergeTupleNames(competitor.ResultType, type)
                            competitor.InferenceRestrictions = Conversions.CombineConversionRequirements(
                                                                        competitor.InferenceRestrictions,
                                                                        inferenceRestrictions)
                            competitor.ByAssumption = competitor.ByAssumption AndAlso typeByAssumption

                            Debug.Assert(Not foundInList, "List is supposed to be unique: how can we already find two of the same type in this list.")
                            foundInList = True
                            ' TODO: Should we simply exit the loop for RELEASE build?
                        End If
                    Next
                End If

                If Not foundInList Then
                    Dim typeData As DominantTypeDataTypeInference = New DominantTypeDataTypeInference()

                    typeData.ResultType = type
                    typeData.ByAssumption = typeByAssumption
                    typeData.InferenceRestrictions = inferenceRestrictions

                    typeData.ArgumentLocation = argumentLocation
                    typeData.Parameter = parameter
                    typeData.InferredFromObject = inferredFromObject
                    typeData.TypeParameter = DeclaredTypeParam

                    InferenceTypeCollection.GetTypeDataList().Add(typeData)
                End If
            End Sub

        End Class


        Private Class ArgumentNode
            Inherits InferenceNode

            Public ReadOnly ParameterType As TypeSymbol
            Public ReadOnly Expression As BoundExpression
            Public ReadOnly Parameter As ParameterSymbol

            Public Sub New(graph As InferenceGraph, expression As BoundExpression, parameterType As TypeSymbol, parameter As ParameterSymbol)
                MyBase.New(graph, InferenceNodeType.ArgumentNode)
                Me.Expression = expression
                Me.ParameterType = parameterType
                Me.Parameter = parameter
            End Sub

            Public Overrides Function InferTypeAndPropagateHints() As Boolean

#If DEBUG Then
                VerifyIncomingInferenceComplete(InferenceNodeType.TypeParameterNode)
#End If
                ' Check if all incoming are ok, otherwise skip inference.

                For Each currentGraphNode As InferenceNode In IncomingEdges
                    Debug.Assert(currentGraphNode.NodeType = InferenceNodeType.TypeParameterNode, "Should only have typed nodes as incoming edges.")
                    Dim currentTypedNode As TypeParameterNode = DirectCast(currentGraphNode, TypeParameterNode)

                    If currentTypedNode.InferredType Is Nothing Then

                        Dim skipThisNode As Boolean = True

                        If Expression.Kind = BoundKind.UnboundLambda AndAlso ParameterType.IsDelegateType() Then
                            ' Check here if we need to infer Object for some of the parameters of the Lambda if we weren't able
                            ' to infer these otherwise. This is only the case for arguments of the lambda that have a GenericParam
                            ' of the method we are inferring that is not yet inferred.

                            ' Now find the invoke method of the delegate
                            Dim delegateType = DirectCast(ParameterType, NamedTypeSymbol)
                            Dim invokeMethod As MethodSymbol = delegateType.DelegateInvokeMethod

                            If invokeMethod IsNot Nothing AndAlso invokeMethod.GetUseSiteErrorInfo() Is Nothing Then

                                Dim unboundLambda = DirectCast(Expression, UnboundLambda)
                                Dim lambdaParameters As ImmutableArray(Of ParameterSymbol) = unboundLambda.Parameters
                                Dim delegateParameters As ImmutableArray(Of ParameterSymbol) = invokeMethod.Parameters

                                For i As Integer = 0 To Math.Min(lambdaParameters.Length, delegateParameters.Length) - 1 Step 1
                                    Dim lambdaParameter = DirectCast(lambdaParameters(i), UnboundLambdaParameterSymbol)
                                    Dim delegateParam As ParameterSymbol = delegateParameters(i)

                                    If lambdaParameter.Type Is Nothing AndAlso
                                       delegateParam.Type.Equals(currentTypedNode.DeclaredTypeParam) Then

                                        If Graph.Diagnostic Is Nothing Then
                                            Graph.Diagnostic = New DiagnosticBag()
                                        End If

                                        ' If this was an argument to the unbound Lambda, infer Object.
                                        If Graph.ObjectType Is Nothing Then
                                            Debug.Assert(Graph.Diagnostic IsNot Nothing)
                                            Graph.ObjectType = unboundLambda.Binder.GetSpecialType(SpecialType.System_Object, lambdaParameter.IdentifierSyntax, Graph.Diagnostic)
                                        End If

                                        currentTypedNode.RegisterInferredType(Graph.ObjectType,
                                                                              lambdaParameter.TypeSyntax,
                                                                              currentTypedNode.InferredTypeByAssumption)

                                        ' 
                                        ' Port SP1 CL 2941063 to VS10
                                        ' Bug 153317
                                        ' Report an error if Option Strict On or a warning if Option Strict Off 
                                        ' because we have no hints about the lambda parameter
                                        ' and we are assuming that it is an object. 
                                        ' e.g. "Sub f(Of T, U)(ByVal x As Func(Of T, U))" invoked with "f(function(z)z)"
                                        ' needs to put the squiggly on the first "z".

                                        Debug.Assert(Graph.Diagnostic IsNot Nothing)
                                        unboundLambda.Binder.ReportLambdaParameterInferredToBeObject(lambdaParameter, Graph.Diagnostic)

                                        skipThisNode = False
                                        Exit For
                                    End If
                                Next
                            End If
                        End If

                        If skipThisNode Then
                            InferenceComplete = True
                            Return False ' DOn't restart the algorithm.
                        End If
                    End If
                Next

                Dim argumentType As TypeSymbol = Nothing
                Dim inferenceOk As Boolean = False

                Select Case Expression.Kind

                    Case BoundKind.AddressOfOperator

                        inferenceOk = Graph.InferTypeArgumentsFromAddressOfArgument(
                                                    Expression,
                                                    ParameterType,
                                                    Parameter)

                    Case BoundKind.LateAddressOfOperator
                        ' We can not infer anything for this addressOf, AddressOf can never be of type Object, so mark inference
                        ' as not failed due to object.
                        Graph.ReportNotFailedInferenceDueToObject()
                        inferenceOk = True

                    Case BoundKind.QueryLambda, BoundKind.GroupTypeInferenceLambda, BoundKind.UnboundLambda

                        ' TODO: Not sure if this is applicable to Roslyn, need to try this out when all required features are available.
                        ' BUG: 131359 If the lambda is wrapped in a delegate constructor the resultType
                        ' will be set and not be Void. In this case the lambda argument should be treated as a regular
                        ' argument so fall through in this case.

                        Debug.Assert(Expression.Type Is Nothing)

                        ' TODO: We are setting inference level before
                        '       even trying to infer something from the lambda. It is possible
                        '       that we won't infer anything, should consider changing the 
                        '       inference level after.
                        Graph.MarkInferenceLevel(InferenceLevel.Orcas)
                        inferenceOk = Graph.InferTypeArgumentsFromLambdaArgument(
                                                    Expression,
                                                    ParameterType,
                                                    Parameter)

                    Case Else
HandleAsAGeneralExpression:
                        ' We should not infer from a Nothing literal.
                        If Expression.IsStrictNothingLiteral() Then
                            InferenceComplete = True

                            ' continue without restarting, if all hints are Nothing the InferenceTypeNode will mark
                            ' the inference as failed.
                            Return False
                        End If

                        Dim inferenceRestrictions As RequiredConversion = RequiredConversion.Any

                        If Parameter IsNot Nothing AndAlso
                           Parameter.IsByRef AndAlso
                           (Expression.IsLValue() OrElse Expression.IsPropertySupportingAssignment()) Then
                            ' A ByRef parameter needs (if the argument was an lvalue) to be copy-backable into
                            ' that argument.
                            Debug.Assert(inferenceRestrictions = RequiredConversion.Any, "there should have been no prior restrictions by the time we encountered ByRef")

                            inferenceRestrictions = Conversions.CombineConversionRequirements(
                                                        inferenceRestrictions,
                                                        Conversions.InvertConversionRequirement(inferenceRestrictions))

                            Debug.Assert(inferenceRestrictions = RequiredConversion.AnyAndReverse, "expected ByRef to require AnyAndReverseConversion")
                        End If

                        Dim arrayLiteral As BoundArrayLiteral = Nothing
                        Dim argumentTypeByAssumption As Boolean = False
                        Dim expressionType As TypeSymbol

                        If Expression.Kind = BoundKind.ArrayLiteral Then
                            arrayLiteral = DirectCast(Expression, BoundArrayLiteral)
                            argumentTypeByAssumption = arrayLiteral.NumberOfCandidates <> 1
                            expressionType = New ArrayLiteralTypeSymbol(arrayLiteral)

                        ElseIf Expression.Kind = BoundKind.TupleLiteral Then
                            expressionType = DirectCast(Expression, BoundTupleLiteral).InferredType
                        Else
                            expressionType = Expression.Type
                        End If

                        ' Need to create an ArrayLiteralTypeSymbol
                        inferenceOk = Graph.InferTypeArgumentsFromArgument(
                            Expression.Syntax,
                            expressionType,
                            argumentTypeByAssumption,
                            ParameterType,
                            Parameter,
                            MatchGenericArgumentToParameter.MatchBaseOfGenericArgumentToParameter,
                            inferenceRestrictions)
                End Select


                If Not inferenceOk Then
                    '  !! Inference has failed. Mismatch of Argument and Parameter signature, so could not find type hints.
                    Graph.MarkInferenceFailure()

                    If Not (Expression.Type IsNot Nothing AndAlso Expression.Type.IsObjectType()) Then
                        Graph.ReportNotFailedInferenceDueToObject()
                    End If
                End If

                InferenceComplete = True

                Return False ' // Don't restart the algorithm;
            End Function
        End Class


        Private Class InferenceGraph
            Inherits Graph(Of InferenceNode)

            Public Diagnostic As DiagnosticBag
            Public ObjectType As NamedTypeSymbol
            Public ReadOnly Candidate As MethodSymbol
            Public ReadOnly Arguments As ImmutableArray(Of BoundExpression)
            Public ReadOnly ParameterToArgumentMap As ArrayBuilder(Of Integer)
            Public ReadOnly ParamArrayItems As ArrayBuilder(Of Integer)
            Public ReadOnly DelegateReturnType As TypeSymbol
            Public ReadOnly DelegateReturnTypeReferenceBoundNode As BoundNode
            Public UseSiteDiagnostics As HashSet(Of DiagnosticInfo)

            Private _someInferenceFailed As Boolean
            Private _inferenceErrorReasons As InferenceErrorReasons
            Private _allFailedInferenceIsDueToObject As Boolean = True ' remains true until proven otherwise.
            Private _typeInferenceLevel As InferenceLevel = InferenceLevel.None
            Private _asyncLambdaSubToFunctionMismatch As HashSet(Of BoundExpression)

            Private ReadOnly _typeParameterNodes As ImmutableArray(Of TypeParameterNode)
            Private _verifyingAssertions As Boolean

            Private Sub New(
                diagnostic As DiagnosticBag,
                candidate As MethodSymbol,
                arguments As ImmutableArray(Of BoundExpression),
                parameterToArgumentMap As ArrayBuilder(Of Integer),
                paramArrayItems As ArrayBuilder(Of Integer),
                delegateReturnType As TypeSymbol,
                delegateReturnTypeReferenceBoundNode As BoundNode,
                asyncLambdaSubToFunctionMismatch As HashSet(Of BoundExpression),
                useSiteDiagnostics As HashSet(Of DiagnosticInfo)
            )
                Debug.Assert(delegateReturnType Is Nothing OrElse delegateReturnTypeReferenceBoundNode IsNot Nothing)

                Me.Diagnostic = diagnostic
                Me.Candidate = candidate
                Me.Arguments = arguments
                Me.ParameterToArgumentMap = parameterToArgumentMap
                Me.ParamArrayItems = paramArrayItems
                Me.DelegateReturnType = delegateReturnType
                Me.DelegateReturnTypeReferenceBoundNode = delegateReturnTypeReferenceBoundNode
                Me._asyncLambdaSubToFunctionMismatch = asyncLambdaSubToFunctionMismatch
                Me.UseSiteDiagnostics = useSiteDiagnostics

                ' Allocate the array of TypeParameter nodes.
                Dim arity As Integer = candidate.Arity

                Dim typeParameterNodes(arity - 1) As TypeParameterNode

                For i As Integer = 0 To arity - 1 Step 1
                    typeParameterNodes(i) = New TypeParameterNode(Me, candidate.TypeParameters(i))
                Next

                _typeParameterNodes = typeParameterNodes.AsImmutableOrNull()
            End Sub

            Public ReadOnly Property SomeInferenceHasFailed As Boolean
                Get
                    Return _someInferenceFailed
                End Get
            End Property

            Public Sub MarkInferenceFailure()
                _someInferenceFailed = True
            End Sub

            Public ReadOnly Property AllFailedInferenceIsDueToObject As Boolean
                Get
                    Return _allFailedInferenceIsDueToObject
                End Get
            End Property

            Public ReadOnly Property InferenceErrorReasons As InferenceErrorReasons
                Get
                    Return _inferenceErrorReasons
                End Get
            End Property

            Public Sub ReportNotFailedInferenceDueToObject()
                _allFailedInferenceIsDueToObject = False
            End Sub

            Public ReadOnly Property TypeInferenceLevel As InferenceLevel
                Get
                    Return _typeInferenceLevel
                End Get
            End Property

            Public Sub MarkInferenceLevel(typeInferenceLevel As InferenceLevel)
                If _typeInferenceLevel < typeInferenceLevel Then
                    _typeInferenceLevel = typeInferenceLevel
                End If
            End Sub


            Public Shared Function Infer(
                candidate As MethodSymbol,
                arguments As ImmutableArray(Of BoundExpression),
                parameterToArgumentMap As ArrayBuilder(Of Integer),
                paramArrayItems As ArrayBuilder(Of Integer),
                delegateReturnType As TypeSymbol,
                delegateReturnTypeReferenceBoundNode As BoundNode,
                ByRef typeArguments As ImmutableArray(Of TypeSymbol),
                ByRef inferenceLevel As InferenceLevel,
                ByRef allFailedInferenceIsDueToObject As Boolean,
                ByRef someInferenceFailed As Boolean,
                ByRef inferenceErrorReasons As InferenceErrorReasons,
                <Out> ByRef inferredTypeByAssumption As BitVector,
                <Out> ByRef typeArgumentsLocation As ImmutableArray(Of SyntaxNodeOrToken),
                <[In](), Out()> ByRef asyncLambdaSubToFunctionMismatch As HashSet(Of BoundExpression),
                <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo),
                ByRef diagnostic As DiagnosticBag,
                inferTheseTypeParameters As BitVector
            ) As Boolean
                Dim graph As New InferenceGraph(diagnostic, candidate, arguments, parameterToArgumentMap, paramArrayItems,
                                                delegateReturnType, delegateReturnTypeReferenceBoundNode, asyncLambdaSubToFunctionMismatch,
                                                useSiteDiagnostics)

                ' Build a graph describing the flow of type inference data.
                ' This creates edges from "regular" arguments to type parameters and from type parameters to lambda arguments. 
                ' In the rest of this function that graph is then processed (see below for more details).  Essentially, for each
                ' "type parameter" node a list of "type hints" (possible candidates for type inference) is collected. The dominant
                ' type algorithm is then performed over the list of hints associated with each node.
                ' 
                ' The process of populating the graph also seeds type hints for type parameters referenced by explicitly typed
                ' lambda parameters. Also, hints sometimes have restrictions placed on them that limit what conversions the dominant type
                ' algorithm can consider when it processes them. The restrictions are generally driven by the context in which type
                ' parameters are used. For example if a type parameter is used as a type parameter of another type (something like IGoo(of T)),
                ' then the dominant type algorithm is not allowed to consider any conversions. There are similar restrictions for
                ' Array co-variance.

                graph.PopulateGraph()

                Dim topoSortedGraph = ArrayBuilder(Of StronglyConnectedComponent(Of InferenceNode)).GetInstance()

                ' This is the restart point of the algorithm
                Do
                    Dim restartAlgorithm As Boolean = False
                    Dim stronglyConnectedComponents As Graph(Of StronglyConnectedComponent(Of InferenceNode)) =
                          graph.BuildStronglyConnectedComponents()

                    topoSortedGraph.Clear()
                    stronglyConnectedComponents.TopoSort(topoSortedGraph)

                    ' We now iterate over the topologically-sorted strongly connected components of the graph, and generate
                    ' type hints as appropriate. 
                    '
                    ' When we find a node for an argument (or an ArgumentNode as it's referred to in the code), we infer
                    ' types for all type parameters referenced by that argument and then propagate those types as hints
                    ' to the referenced type parameters. If there are incoming edges into the argument node, they correspond
                    ' to parameters of lambda arguments that get their value from the delegate type that contains type
                    ' parameters that would have been inferred during a previous iteration of the loop. Those types are
                    ' flowed into the lambda argument.
                    '
                    ' When we encounter a "type parameter" node (or TypeParameterNode as it is called in the code), we run
                    ' the dominant type algorithm over all of it's hints and use the resulting type as the value for the
                    ' referenced type parameter.
                    ' 
                    ' If we find a strongly connected component with more than one node, it means we
                    ' have a cycle and cannot simply run the inference algorithm. When this happens,
                    ' we look through the nodes in the cycle for a type parameter node with at least
                    ' one type hint. If we find one, we remove all incoming edges to that node,
                    ' infer the type using its hints, and then restart the whole algorithm from the
                    ' beginning (recompute the strongly connected components, resort them, and then
                    ' iterate over the graph again). The source nodes of the incoming edges we
                    ' removed are added to an "assertion list". After graph traversal is done we
                    ' then run inference on any "assertion nodes" we may have created.
                    For Each sccNode As StronglyConnectedComponent(Of InferenceNode) In topoSortedGraph
                        Dim childNodes As ArrayBuilder(Of InferenceNode) = sccNode.ChildNodes

                        ' Small optimization if one node
                        If childNodes.Count = 1 Then
                            If childNodes(0).InferTypeAndPropagateHints() Then
                                ' consider: scottwis
                                '               We should be asserting here, because this code is unreachable..
                                '               There are two implementations of InferTypeAndPropagateHints,
                                '               one for "named nodes" (nodes corresponding to arguments) and another
                                '               for "type nodes" (nodes corresponding to types).
                                '               The implementation for "named nodes" always returns false, which means 
                                '               "don't restart the algorithm". The implementation for "type nodes" only returns true 
                                '               if a node has incoming edges that have not been visited previously. In order for that 
                                '               to happen the node must be inside a strongly connected component with more than one node 
                                '               (i.e. it must be involved in a cycle). If it wasn't we would be visiting it in
                                '               topological order, which means all incoming edges should have already been visited.
                                '               That means that if we reach this code, there is probably a bug in the traversal process. We
                                '               don't want to silently mask the bug. At a minimum we should either assert or generate a compiler error.
                                '               
                                '               An argument could be made that it is good to have this because
                                '               InferTypeAndPropagateHints is virtual, and should some new node type be
                                '               added it's implementation may return true, and so this would follow that
                                '               path. That argument does make some tiny amount of sense, and so we
                                '               should keep this code here to make it easier to make any such
                                '               modifications in the future. However, we still need an assert to guard
                                '               against graph traversal bugs, and in the event that such changes are
                                '               made, leave it to the modifier to remove the assert if necessary.
                                Throw ExceptionUtilities.Unreachable
                            End If

                        Else
                            Dim madeInferenceProgress As Boolean = False

                            For Each child As InferenceNode In childNodes
                                If child.NodeType = InferenceNodeType.TypeParameterNode AndAlso
                                   DirectCast(child, TypeParameterNode).InferenceTypeCollection.GetTypeDataList().Count > 0 Then

                                    If child.InferTypeAndPropagateHints() Then
                                        ' If edges were broken, restart algorithm to recompute strongly connected components.
                                        restartAlgorithm = True
                                    End If

                                    madeInferenceProgress = True
                                End If
                            Next

                            If Not madeInferenceProgress Then
                                ' Did not make progress trying to force incoming edges for nodes with TypesHints, just inferring all now,
                                ' will infer object if no type hints.
                                For Each child As InferenceNode In childNodes
                                    If child.NodeType = InferenceNodeType.TypeParameterNode AndAlso
                                       child.InferTypeAndPropagateHints() Then
                                        ' If edges were broken, restart algorithm to recompute strongly connected components.
                                        restartAlgorithm = True
                                    End If
                                Next
                            End If

                            If restartAlgorithm Then
                                Exit For ' For Each sccNode
                            End If
                        End If
                    Next

                    If restartAlgorithm Then
                        Continue Do
                    End If

                    Exit Do
                Loop


                'The commented code below is from Dev10, but it looks like
                'it doesn't do anything useful because topoSortedGraph contains
                'StronglyConnectedComponents, which have NodeType=None. 
                '
                'graph.m_VerifyingAssertions = True
                'GraphNodeListIterator assertionIter(&topoSortedGraph);
                '                While (assertionIter.MoveNext())
                '{
                '    GraphNode* currentNode = assertionIter.Current();
                '    if (currentNode->m_NodeType == TypedNodeType)
                '    {
                '        InferenceTypeNode* currentTypeNode = (InferenceTypeNode*)currentNode;
                '        currentTypeNode->VerifyTypeAssertions();
                '    }
                '}
                'graph.m_VerifyingAssertions = False

                topoSortedGraph.Free()

                Dim succeeded As Boolean = Not graph.SomeInferenceHasFailed

                someInferenceFailed = graph.SomeInferenceHasFailed
                allFailedInferenceIsDueToObject = graph.AllFailedInferenceIsDueToObject
                inferenceErrorReasons = graph.InferenceErrorReasons

                ' Make sure that allFailedInferenceIsDueToObject only stays set,
                ' if there was an actual inference failure.
                If Not someInferenceFailed OrElse delegateReturnType IsNot Nothing Then
                    allFailedInferenceIsDueToObject = False
                End If

                Dim arity As Integer = candidate.Arity
                Dim inferredTypes(arity - 1) As TypeSymbol
                Dim inferredFromLocation(arity - 1) As SyntaxNodeOrToken

                For i As Integer = 0 To arity - 1 Step 1

                    ' TODO: Should we use InferredType or CandidateInferredType here? It looks like Dev10 is using the latter,
                    '       it might not be cleaned in case of a failure. Will use the former for now.
                    Dim typeParameterNode = graph._typeParameterNodes(i)

                    Dim inferredType As TypeSymbol = typeParameterNode.InferredType

                    If inferredType Is Nothing AndAlso
                       (inferTheseTypeParameters.IsNull OrElse inferTheseTypeParameters(i)) Then
                        succeeded = False
                    End If

                    If typeParameterNode.InferredTypeByAssumption Then
                        If inferredTypeByAssumption.IsNull Then
                            inferredTypeByAssumption = BitVector.Create(arity)
                        End If
                        inferredTypeByAssumption(i) = True
                    End If

                    inferredTypes(i) = inferredType
                    inferredFromLocation(i) = typeParameterNode.InferredFromLocation
                Next

                typeArguments = inferredTypes.AsImmutableOrNull()
                typeArgumentsLocation = inferredFromLocation.AsImmutableOrNull()
                inferenceLevel = graph._typeInferenceLevel

                Debug.Assert(diagnostic Is Nothing OrElse diagnostic Is graph.Diagnostic)
                diagnostic = graph.Diagnostic

                asyncLambdaSubToFunctionMismatch = graph._asyncLambdaSubToFunctionMismatch
                useSiteDiagnostics = graph.UseSiteDiagnostics

                Return succeeded
            End Function

            Private Sub PopulateGraph()

                Dim candidate As MethodSymbol = Me.Candidate
                Dim arguments As ImmutableArray(Of BoundExpression) = Me.Arguments
                Dim parameterToArgumentMap As ArrayBuilder(Of Integer) = Me.ParameterToArgumentMap
                Dim paramArrayItems As ArrayBuilder(Of Integer) = Me.ParamArrayItems
                Dim isExpandedParamArrayForm As Boolean = (paramArrayItems IsNot Nothing)

                Dim argIndex As Integer

                For paramIndex = 0 To candidate.ParameterCount - 1 Step 1

                    Dim param As ParameterSymbol = candidate.Parameters(paramIndex)
                    Dim targetType As TypeSymbol = param.Type

                    If param.IsParamArray AndAlso paramIndex = candidate.ParameterCount - 1 Then

                        If targetType.Kind <> SymbolKind.ArrayType Then
                            Continue For
                        End If

                        If Not isExpandedParamArrayForm Then
                            argIndex = parameterToArgumentMap(paramIndex)
                            Dim paramArrayArgument = If(argIndex = -1, Nothing, arguments(argIndex))

                            Debug.Assert(paramArrayArgument Is Nothing OrElse paramArrayArgument.Kind <> BoundKind.OmittedArgument)

                            '§11.8.2 Applicable Methods
                            'If the conversion from the type of the argument expression to the paramarray type is narrowing, 
                            'then the method is only applicable in its expanded form.
                            '!!! However, there is an exception to that rule - narrowing conversion from semantical Nothing literal is Ok. !!!

                            If paramArrayArgument Is Nothing OrElse paramArrayArgument.HasErrors OrElse
                               Not ArgumentTypePossiblyMatchesParamarrayShape(paramArrayArgument, targetType) Then
                                Continue For
                            End If

                            RegisterArgument(paramArrayArgument, targetType, param)
                        Else
                            Debug.Assert(isExpandedParamArrayForm)

                            '§11.8.2 Applicable Methods
                            'If the argument expression is the literal Nothing, then the method is only applicable in its unexpanded form.
                            ' Note, that explicitly converted NOTHING is treated the same way by Dev10.
                            If paramArrayItems.Count = 1 AndAlso arguments(paramArrayItems(0)).IsNothingLiteral() Then
                                Continue For
                            End If

                            ' Otherwise, for a ParamArray parameter, all the matching arguments are passed
                            ' ByVal as instances of the element type of the ParamArray.
                            ' Perform the conversions to the element type of the ParamArray here.
                            Dim arrayType = DirectCast(targetType, ArrayTypeSymbol)

                            If Not arrayType.IsSZArray Then
                                Continue For
                            End If

                            targetType = arrayType.ElementType

                            If targetType.Kind = SymbolKind.ErrorType Then
                                Continue For
                            End If

                            For j As Integer = 0 To paramArrayItems.Count - 1 Step 1
                                If arguments(paramArrayItems(j)).HasErrors Then
                                    Continue For
                                End If

                                RegisterArgument(arguments(paramArrayItems(j)), targetType, param)
                            Next
                        End If

                        Continue For
                    End If

                    argIndex = parameterToArgumentMap(paramIndex)
                    Dim argument = If(argIndex = -1, Nothing, arguments(argIndex))

                    If argument Is Nothing OrElse argument.HasErrors OrElse targetType.IsErrorType() OrElse argument.Kind = BoundKind.OmittedArgument Then
                        Continue For
                    End If

                    RegisterArgument(argument, targetType, param)
                Next

                AddDelegateReturnTypeToGraph()
            End Sub

            Private Sub AddDelegateReturnTypeToGraph()
                If Me.DelegateReturnType IsNot Nothing AndAlso Not Me.DelegateReturnType.IsVoidType() Then
                    Dim fakeArgument As New BoundRValuePlaceholder(Me.DelegateReturnTypeReferenceBoundNode.Syntax,
                                                                  Me.DelegateReturnType)

                    Dim returnNode As New ArgumentNode(Me, fakeArgument, Me.Candidate.ReturnType, parameter:=Nothing)

                    ' Add the edges from all the current generic parameters to this named node.
                    For Each current As InferenceNode In Vertices
                        If current.NodeType = InferenceNodeType.TypeParameterNode Then
                            AddEdge(current, returnNode)
                        End If
                    Next

                    ' Add the edges from the resultType outgoing to the generic parameters.
                    AddTypeToGraph(returnNode, isOutgoingEdge:=True)
                End If
            End Sub

            Private Sub RegisterArgument(
                argument As BoundExpression,
                targetType As TypeSymbol,
                param As ParameterSymbol
            )
                ' Dig through parenthesized.
                If Not argument.IsNothingLiteral Then
                    argument = argument.GetMostEnclosedParenthesizedExpression()
                End If

                Dim argNode As New ArgumentNode(Me, argument, targetType, param)

                Select Case argument.Kind
                    Case BoundKind.UnboundLambda, BoundKind.QueryLambda, BoundKind.GroupTypeInferenceLambda
                        AddLambdaToGraph(argNode, argument.GetBinderFromLambda())
                    Case BoundKind.AddressOfOperator
                        AddAddressOfToGraph(argNode, DirectCast(argument, BoundAddressOfOperator).Binder)
                    Case BoundKind.TupleLiteral
                        AddTupleLiteralToGraph(argNode)
                    Case Else
                        AddTypeToGraph(argNode, isOutgoingEdge:=True)
                End Select
            End Sub

            Private Sub AddTypeToGraph(
                node As ArgumentNode,
                isOutgoingEdge As Boolean
            )
                AddTypeToGraph(node.ParameterType, node, isOutgoingEdge, BitVector.Create(_typeParameterNodes.Length))
            End Sub

            Private Function FindTypeParameterNode(typeParameter As TypeParameterSymbol) As TypeParameterNode
                Dim ordinal As Integer = typeParameter.Ordinal

                If ordinal < _typeParameterNodes.Length AndAlso
                   _typeParameterNodes(ordinal) IsNot Nothing AndAlso
                   typeParameter.Equals(_typeParameterNodes(ordinal).DeclaredTypeParam) Then
                    Return _typeParameterNodes(ordinal)
                End If

                Return Nothing
            End Function

            Private Sub AddTypeToGraph(
                parameterType As TypeSymbol,
                argNode As ArgumentNode,
                isOutgoingEdge As Boolean,
                ByRef haveSeenTypeParameters As BitVector
            )
                Select Case parameterType.Kind
                    Case SymbolKind.TypeParameter
                        Dim typeParameter = DirectCast(parameterType, TypeParameterSymbol)
                        Dim typeParameterNode As TypeParameterNode = FindTypeParameterNode(typeParameter)

                        If typeParameterNode IsNot Nothing AndAlso
                           Not haveSeenTypeParameters(typeParameter.Ordinal) Then

                            If typeParameterNode.Parameter Is Nothing Then
                                typeParameterNode.SetParameter(argNode.Parameter)
                            End If

                            If (isOutgoingEdge) Then
                                AddEdge(argNode, typeParameterNode)
                            Else
                                AddEdge(typeParameterNode, argNode)
                            End If

                            haveSeenTypeParameters(typeParameter.Ordinal) = True
                        End If

                    Case SymbolKind.ArrayType

                        AddTypeToGraph(DirectCast(parameterType, ArrayTypeSymbol).ElementType, argNode, isOutgoingEdge, haveSeenTypeParameters)

                    Case SymbolKind.NamedType

                        Dim possiblyGenericType = DirectCast(parameterType, NamedTypeSymbol)

                        Dim elementTypes As ImmutableArray(Of TypeSymbol) = Nothing
                        If possiblyGenericType.TryGetElementTypesIfTupleOrCompatible(elementTypes) Then
                            For Each elementType In elementTypes
                                AddTypeToGraph(elementType, argNode, isOutgoingEdge, haveSeenTypeParameters)
                            Next
                        Else

                            Do
                                For Each typeArgument In possiblyGenericType.TypeArgumentsWithDefinitionUseSiteDiagnostics(Me.UseSiteDiagnostics)
                                    AddTypeToGraph(typeArgument, argNode, isOutgoingEdge, haveSeenTypeParameters)
                                Next

                                possiblyGenericType = possiblyGenericType.ContainingType
                            Loop While possiblyGenericType IsNot Nothing
                        End If
                End Select

            End Sub

            Private Sub AddTupleLiteralToGraph(argNode As ArgumentNode)
                AddTupleLiteralToGraph(argNode.ParameterType, argNode)
            End Sub

            Private Sub AddTupleLiteralToGraph(
                parameterType As TypeSymbol,
                argNode As ArgumentNode
            )
                Debug.Assert(argNode.Expression.Kind = BoundKind.TupleLiteral)

                Dim tupleLiteral = DirectCast(argNode.Expression, BoundTupleLiteral)
                Dim tupleArguments = tupleLiteral.Arguments

                If parameterType.IsTupleOrCompatibleWithTupleOfCardinality(tupleArguments.Length) Then
                    Dim parameterElementTypes = parameterType.GetElementTypesOfTupleOrCompatible
                    For i As Integer = 0 To tupleArguments.Length - 1
                        RegisterArgument(tupleArguments(i), parameterElementTypes(i), argNode.Parameter)
                    Next

                    Return
                End If

                AddTypeToGraph(argNode, isOutgoingEdge:=True)
            End Sub

            Private Sub AddAddressOfToGraph(argNode As ArgumentNode, binder As Binder)
                AddAddressOfToGraph(argNode.ParameterType, argNode, binder)
            End Sub

            Private Sub AddAddressOfToGraph(
                parameterType As TypeSymbol,
                argNode As ArgumentNode,
                binder As Binder
            )
                Debug.Assert(argNode.Expression.Kind = BoundKind.AddressOfOperator)

                If parameterType.IsTypeParameter() Then
                    AddTypeToGraph(parameterType, argNode, isOutgoingEdge:=True, haveSeenTypeParameters:=BitVector.Create(_typeParameterNodes.Length))

                ElseIf parameterType.IsDelegateType() Then
                    Dim delegateType As NamedTypeSymbol = DirectCast(parameterType, NamedTypeSymbol)
                    Dim invoke As MethodSymbol = delegateType.DelegateInvokeMethod

                    If invoke IsNot Nothing AndAlso invoke.GetUseSiteErrorInfo() Is Nothing AndAlso delegateType.IsGenericType Then

                        Dim haveSeenTypeParameters = BitVector.Create(_typeParameterNodes.Length)
                        AddTypeToGraph(invoke.ReturnType, argNode, isOutgoingEdge:=True, haveSeenTypeParameters:=haveSeenTypeParameters) ' outgoing (name->type) edge

                        haveSeenTypeParameters.Clear()

                        For Each delegateParameter As ParameterSymbol In invoke.Parameters
                            AddTypeToGraph(delegateParameter.Type, argNode, isOutgoingEdge:=False, haveSeenTypeParameters:=haveSeenTypeParameters) ' incoming (type->name) edge
                        Next
                    End If
                ElseIf TypeSymbol.Equals(parameterType.OriginalDefinition, binder.Compilation.GetWellKnownType(WellKnownType.System_Linq_Expressions_Expression_T), TypeCompareKind.ConsiderEverything) Then
                    ' If we've got an Expression(Of T), skip through to T
                    AddAddressOfToGraph(DirectCast(parameterType, NamedTypeSymbol).TypeArgumentWithDefinitionUseSiteDiagnostics(0, Me.UseSiteDiagnostics), argNode, binder)
                End If
            End Sub

            Private Sub AddLambdaToGraph(argNode As ArgumentNode, binder As Binder)
                AddLambdaToGraph(argNode.ParameterType, argNode, binder)
            End Sub

            Private Sub AddLambdaToGraph(
                parameterType As TypeSymbol,
                argNode As ArgumentNode,
                binder As Binder
            )
                If parameterType.IsTypeParameter() Then
                    ' Lambda is bound to a generic typeParam, just infer anonymous delegate
                    AddTypeToGraph(parameterType, argNode, isOutgoingEdge:=True, haveSeenTypeParameters:=BitVector.Create(_typeParameterNodes.Length))

                ElseIf parameterType.IsDelegateType() Then
                    Dim delegateType As NamedTypeSymbol = DirectCast(parameterType, NamedTypeSymbol)
                    Dim invoke As MethodSymbol = delegateType.DelegateInvokeMethod

                    If invoke IsNot Nothing AndAlso invoke.GetUseSiteErrorInfo() Is Nothing AndAlso delegateType.IsGenericType Then

                        Dim delegateParameters As ImmutableArray(Of ParameterSymbol) = invoke.Parameters
                        Dim lambdaParameters As ImmutableArray(Of ParameterSymbol)

                        Select Case argNode.Expression.Kind
                            Case BoundKind.QueryLambda
                                lambdaParameters = DirectCast(argNode.Expression, BoundQueryLambda).LambdaSymbol.Parameters
                            Case BoundKind.GroupTypeInferenceLambda
                                lambdaParameters = DirectCast(argNode.Expression, GroupTypeInferenceLambda).Parameters
                            Case BoundKind.UnboundLambda
                                lambdaParameters = DirectCast(argNode.Expression, UnboundLambda).Parameters
                            Case Else
                                Throw ExceptionUtilities.UnexpectedValue(argNode.Expression.Kind)
                        End Select

                        Dim haveSeenTypeParameters = BitVector.Create(_typeParameterNodes.Length)

                        For i As Integer = 0 To Math.Min(delegateParameters.Length, lambdaParameters.Length) - 1 Step 1
                            If lambdaParameters(i).Type IsNot Nothing Then
                                ' Prepopulate the hint from the lambda's parameter.
                                ' !!! Unlike Dev10, we are using MatchArgumentToBaseOfGenericParameter because a value of generic 
                                ' !!! parameter will be passed into the parameter of argument type.
                                ' TODO: Consider using location for the type declaration.
                                InferTypeArgumentsFromArgument(
                                    argNode.Expression.Syntax,
                                    lambdaParameters(i).Type,
                                    argumentTypeByAssumption:=False,
                                    parameterType:=delegateParameters(i).Type,
                                    param:=delegateParameters(i),
                                    digThroughToBasesAndImplements:=MatchGenericArgumentToParameter.MatchArgumentToBaseOfGenericParameter,
                                    inferenceRestrictions:=RequiredConversion.Any)
                            End If

                            AddTypeToGraph(delegateParameters(i).Type, argNode, isOutgoingEdge:=False, haveSeenTypeParameters:=haveSeenTypeParameters)
                        Next

                        haveSeenTypeParameters.Clear()
                        AddTypeToGraph(invoke.ReturnType, argNode, isOutgoingEdge:=True, haveSeenTypeParameters:=haveSeenTypeParameters)
                    End If

                ElseIf TypeSymbol.Equals(parameterType.OriginalDefinition, binder.Compilation.GetWellKnownType(WellKnownType.System_Linq_Expressions_Expression_T), TypeCompareKind.ConsiderEverything) Then
                    ' If we've got an Expression(Of T), skip through to T
                    AddLambdaToGraph(DirectCast(parameterType, NamedTypeSymbol).TypeArgumentWithDefinitionUseSiteDiagnostics(0, Me.UseSiteDiagnostics), argNode, binder)
                End If
            End Sub

            Private Shared Function ArgumentTypePossiblyMatchesParamarrayShape(argument As BoundExpression, paramType As TypeSymbol) As Boolean
                Dim argumentType As TypeSymbol = argument.Type
                Dim isArrayLiteral As Boolean = False

                If argumentType Is Nothing Then
                    If argument.Kind = BoundKind.ArrayLiteral Then
                        isArrayLiteral = True
                        argumentType = DirectCast(argument, BoundArrayLiteral).InferredType
                    Else
                        Return False
                    End If
                End If

                While paramType.IsArrayType()

                    If Not argumentType.IsArrayType() Then
                        Return False
                    End If

                    Dim argumentArray = DirectCast(argumentType, ArrayTypeSymbol)
                    Dim paramArrayType = DirectCast(paramType, ArrayTypeSymbol)

                    ' We can ignore IsSZArray value for an inferred type of an array literal as long as its rank matches.
                    If argumentArray.Rank <> paramArrayType.Rank OrElse
                       (Not isArrayLiteral AndAlso argumentArray.IsSZArray <> paramArrayType.IsSZArray) Then
                        Return False
                    End If

                    isArrayLiteral = False
                    argumentType = argumentArray.ElementType
                    paramType = paramArrayType.ElementType
                End While

                Return True
            End Function



            Public Sub RegisterTypeParameterHint(
                genericParameter As TypeParameterSymbol,
                inferredType As TypeSymbol,
                inferredTypeByAssumption As Boolean,
                argumentLocation As SyntaxNode,
                parameter As ParameterSymbol,
                inferredFromObject As Boolean,
                inferenceRestrictions As RequiredConversion
            )
                Dim typeNode As TypeParameterNode = FindTypeParameterNode(genericParameter)

                If typeNode IsNot Nothing Then
                    typeNode.AddTypeHint(inferredType, inferredTypeByAssumption, argumentLocation, parameter, inferredFromObject, inferenceRestrictions)
                End If
            End Sub


            Private Function RefersToGenericParameterToInferArgumentFor(
                parameterType As TypeSymbol
            ) As Boolean
                Select Case parameterType.Kind
                    Case SymbolKind.TypeParameter
                        Dim typeParameter = DirectCast(parameterType, TypeParameterSymbol)
                        Dim typeNode As TypeParameterNode = FindTypeParameterNode(typeParameter)

                        ' TODO: It looks like this check can give us a false positive. For example,
                        '       if we are resolving a recursive call we might already bind a type
                        '       parameter to itself (to the same type parameter of the containing method).
                        '       So, the fact that we ran into this type parameter doesn't necessary mean
                        '       that there is anything to infer. I am not sure if this can lead to some
                        '       negative effect. Dev10 appears to have the same behavior, from what I see
                        '       in the code.
                        If typeNode IsNot Nothing Then
                            Return True
                        End If

                    Case SymbolKind.ArrayType

                        Return RefersToGenericParameterToInferArgumentFor(DirectCast(parameterType, ArrayTypeSymbol).ElementType)

                    Case SymbolKind.NamedType

                        Dim possiblyGenericType = DirectCast(parameterType, NamedTypeSymbol)

                        Dim elementTypes As ImmutableArray(Of TypeSymbol) = Nothing
                        If possiblyGenericType.TryGetElementTypesIfTupleOrCompatible(elementTypes) Then
                            For Each elementType In elementTypes
                                If RefersToGenericParameterToInferArgumentFor(elementType) Then
                                    Return True
                                End If
                            Next
                        Else
                            Do
                                For Each typeArgument In possiblyGenericType.TypeArgumentsWithDefinitionUseSiteDiagnostics(Me.UseSiteDiagnostics)
                                    If RefersToGenericParameterToInferArgumentFor(typeArgument) Then
                                        Return True
                                    End If
                                Next

                                possiblyGenericType = possiblyGenericType.ContainingType
                            Loop While possiblyGenericType IsNot Nothing
                        End If
                End Select

                Return False
            End Function

            ' Given an argument type, a parameter type, and a set of (possibly unbound) type arguments
            ' to a generic method, infer type arguments corresponding to type parameters that occur
            ' in the parameter type.
            '
            ' A return value of false indicates that inference fails.
            '
            ' If a generic method is parameterized by T, an argument of type A matches a parameter of type
            ' P, this function tries to infer type for T by using these patterns:
            '
            '   -- If P is T, then infer A for T
            '   -- If P is G(Of T) and A is G(Of X), then infer X for T
            '   -- If P is Array Of T, and A is Array Of X, then infer X for T
            '   -- If P is ByRef T, then infer A for T
            Private Function InferTypeArgumentsFromArgumentDirectly(
                argumentLocation As SyntaxNode,
                argumentType As TypeSymbol,
                argumentTypeByAssumption As Boolean,
                parameterType As TypeSymbol,
                param As ParameterSymbol,
                digThroughToBasesAndImplements As MatchGenericArgumentToParameter,
                inferenceRestrictions As RequiredConversion
            ) As Boolean

                If argumentType Is Nothing OrElse argumentType.IsVoidType() Then
                    ' We should never be able to infer a value from something that doesn't provide a value, e.g:
                    ' Goo(Of T) can't be passed Sub bar(), as in Goo(Bar())  
                    Return False
                End If

                ' If a generic method is parameterized by T, an argument of type A matching a parameter of type
                ' P can be used to infer a type for T by these patterns:
                '
                '   -- If P is T, then infer A for T
                '   -- If P is G(Of T) and A is G(Of X), then infer X for T
                '   -- If P is Array Of T, and A is Array Of X, then infer X for T
                '   -- If P is ByRef T, then infer A for T
                '   -- If P is (T, T) and A is (X, X), then infer X for T

                If parameterType.IsTypeParameter() Then

                    RegisterTypeParameterHint(
                        DirectCast(parameterType, TypeParameterSymbol),
                        argumentType,
                        argumentTypeByAssumption,
                        argumentLocation,
                        param,
                        False,
                        inferenceRestrictions)
                    Return True
                End If


                Dim parameterElementTypes As ImmutableArray(Of TypeSymbol) = Nothing
                Dim argumentElementTypes As ImmutableArray(Of TypeSymbol) = Nothing

                If parameterType.GetNullableUnderlyingTypeOrSelf().TryGetElementTypesIfTupleOrCompatible(parameterElementTypes) AndAlso
                   If(parameterType.IsNullableType(), argumentType.GetNullableUnderlyingTypeOrSelf(), argumentType).
                       TryGetElementTypesIfTupleOrCompatible(argumentElementTypes) Then

                    If parameterElementTypes.Length <> argumentElementTypes.Length Then
                        Return False
                    End If

                    For typeArgumentIndex As Integer = 0 To parameterElementTypes.Length - 1

                        Dim parameterElementType = parameterElementTypes(typeArgumentIndex)
                        Dim argumentElementType = argumentElementTypes(typeArgumentIndex)

                        ' propagate restrictions to the elements
                        If Not InferTypeArgumentsFromArgument(
                                        argumentLocation,
                                        argumentElementType,
                                        argumentTypeByAssumption,
                                        parameterElementType,
                                        param,
                                        digThroughToBasesAndImplements,
                                        inferenceRestrictions
                          ) Then
                            Return False
                        End If
                    Next

                    Return True

                ElseIf parameterType.Kind = SymbolKind.NamedType Then
                    ' e.g. handle goo(of T)(x as Bar(Of T)) We need to dig into Bar(Of T)

                    Dim parameterTypeAsNamedType = DirectCast(parameterType.GetTupleUnderlyingTypeOrSelf(), NamedTypeSymbol)

                    If parameterTypeAsNamedType.IsGenericType Then

                        Dim argumentTypeAsNamedType = If(argumentType.Kind = SymbolKind.NamedType, DirectCast(argumentType.GetTupleUnderlyingTypeOrSelf(), NamedTypeSymbol), Nothing)

                        If argumentTypeAsNamedType IsNot Nothing AndAlso argumentTypeAsNamedType.IsGenericType Then
                            If argumentTypeAsNamedType.OriginalDefinition.IsSameTypeIgnoringAll(parameterTypeAsNamedType.OriginalDefinition) Then

                                Do

                                    For typeArgumentIndex As Integer = 0 To parameterTypeAsNamedType.Arity - 1 Step 1


                                        ' The following code is subtle. Let's recap what's going on...
                                        ' We've so far encountered some context, e.g. "_" or "ICovariant(_)"
                                        ' or "ByRef _" or the like. This context will have given us some TypeInferenceRestrictions.
                                        ' Now, inside the context, we've discovered a generic binding "G(Of _,_,_)"
                                        ' and we have to apply extra restrictions to each of those subcontexts.
                                        ' For non-variant parameters it's easy: the subcontexts just acquire the Identity constraint.
                                        ' For variant parameters it's more subtle. First, we have to strengthen the
                                        ' restrictions to require reference conversion (rather than just VB conversion or
                                        ' whatever it was). Second, if it was an In parameter, then we have to invert
                                        ' the sense.
                                        '

                                        ' Processing of generics is tricky in the case that we've already encountered
                                        ' a "ByRef _". From that outer "ByRef _" we will have inferred the restriction
                                        ' "AnyConversionAndReverse", so that the argument could be copied into the parameter
                                        ' and back again. But now consider if we find a generic inside that ByRef, e.g.
                                        ' if it had been "ByRef x as G(Of T)" then what should we do? More specifically, consider a case
                                        '    "Sub f(Of T)(ByRef x as G(Of T))"  invoked with some   "dim arg as G(Of Hint)".
                                        ' What's needed for any candidate for T is that G(Of Hint) be convertible to
                                        ' G(Of Candidate), and vice versa for the copyback.
                                        ' 
                                        ' But then what should we write down for the hints? The problem is that hints inhere
                                        ' to the generic parameter T, not to the function parameter G(Of T). So we opt for a
                                        ' safe approximation: we just require CLR identity between a candidate and the hint.
                                        ' This is safe but is a little overly-strict. For example:
                                        '    Class G(Of T)
                                        '       Public Shared Widening Operator CType(ByVal x As G(Of T)) As G(Of Animal)
                                        '       Public Shared Widening Operator CType(ByVal x As G(Of Animal)) As G(Of T)
                                        '    Sub inf(Of T)(ByRef x as G(Of T), ByVal y as T)
                                        '    ...
                                        '    inf(New G(Of Car), New Animal)
                                        '    inf(Of Animal)(New G(Of Car), New Animal)
                                        ' Then the hints will be "T:{Car=, Animal+}" and they'll result in inference-failure,
                                        ' even though the explicitly-provided T=Animal ends up working.
                                        ' 
                                        ' Well, it's the best we can do without some major re-architecting of the way
                                        ' hints and type-inference works. That's because all our hints inhere to the
                                        ' type parameter T; in an ideal world, the ByRef hint would inhere to the parameter.
                                        ' But I don't think we'll ever do better than this, just because trying to do
                                        ' type inference inferring to arguments/parameters becomes exponential.
                                        ' Variance generic parameters will work the same.

                                        ' Dev10#595234: each Param'sInferenceRestriction is found as a modification of the surrounding InferenceRestriction:
                                        Dim paramInferenceRestrictions As RequiredConversion

                                        Select Case parameterTypeAsNamedType.TypeParameters(typeArgumentIndex).Variance
                                            Case VarianceKind.In

                                                paramInferenceRestrictions = Conversions.InvertConversionRequirement(
                                                Conversions.StrengthenConversionRequirementToReference(inferenceRestrictions))

                                            Case VarianceKind.Out

                                                paramInferenceRestrictions = Conversions.StrengthenConversionRequirementToReference(inferenceRestrictions)

                                            Case Else
                                                Debug.Assert(VarianceKind.None = parameterTypeAsNamedType.TypeParameters(typeArgumentIndex).Variance)
                                                paramInferenceRestrictions = RequiredConversion.Identity
                                        End Select

                                        Dim _DigThroughToBasesAndImplements As MatchGenericArgumentToParameter

                                        If paramInferenceRestrictions = RequiredConversion.Reference Then
                                            _DigThroughToBasesAndImplements = MatchGenericArgumentToParameter.MatchBaseOfGenericArgumentToParameter
                                        ElseIf paramInferenceRestrictions = RequiredConversion.ReverseReference Then
                                            _DigThroughToBasesAndImplements = MatchGenericArgumentToParameter.MatchArgumentToBaseOfGenericParameter
                                        Else
                                            _DigThroughToBasesAndImplements = MatchGenericArgumentToParameter.MatchGenericArgumentToParameterExactly
                                        End If

                                        If Not InferTypeArgumentsFromArgument(
                                                                        argumentLocation,
                                                                        argumentTypeAsNamedType.TypeArgumentWithDefinitionUseSiteDiagnostics(typeArgumentIndex, Me.UseSiteDiagnostics),
                                                                        argumentTypeByAssumption,
                                                                        parameterTypeAsNamedType.TypeArgumentWithDefinitionUseSiteDiagnostics(typeArgumentIndex, Me.UseSiteDiagnostics),
                                                                        param,
                                                                        _DigThroughToBasesAndImplements,
                                                                        paramInferenceRestrictions
                                                                  ) Then
                                            ' TODO: Would it make sense to continue through other type arguments even if inference failed for 
                                            '       the current one?
                                            Return False
                                        End If

                                    Next

                                    ' Do not forget about type parameters of containing type
                                    parameterTypeAsNamedType = parameterTypeAsNamedType.ContainingType
                                    argumentTypeAsNamedType = argumentTypeAsNamedType.ContainingType

                                Loop While parameterTypeAsNamedType IsNot Nothing

                                Debug.Assert(parameterTypeAsNamedType Is Nothing AndAlso argumentTypeAsNamedType Is Nothing)

                                Return True
                            End If

                        ElseIf parameterTypeAsNamedType.IsNullableType() Then

                            ' we reach here when the ParameterType is an instantiation of Nullable,
                            ' and the argument type is NOT a generic type.

                            ' lwischik: ??? what do array elements have to do with nullables?
                            Return InferTypeArgumentsFromArgument(
                                argumentLocation,
                                argumentType,
                                argumentTypeByAssumption,
                                parameterTypeAsNamedType.GetNullableUnderlyingType(),
                                param,
                                digThroughToBasesAndImplements,
                                Conversions.CombineConversionRequirements(inferenceRestrictions, RequiredConversion.ArrayElement))

                        End If

                        Return False
                    End If

                ElseIf parameterType.IsArrayType() Then
                    If argumentType.IsArrayType() Then
                        Dim parameterArray = DirectCast(parameterType, ArrayTypeSymbol)
                        Dim argumentArray = DirectCast(argumentType, ArrayTypeSymbol)
                        Dim argumentIsAarrayLiteral = TypeOf argumentArray Is ArrayLiteralTypeSymbol

                        ' We can ignore IsSZArray value for an inferred type of an array literal as long as its rank matches. 
                        If parameterArray.Rank = argumentArray.Rank AndAlso
                           (argumentIsAarrayLiteral OrElse parameterArray.IsSZArray = argumentArray.IsSZArray) Then
                            Return InferTypeArgumentsFromArgument(
                                    argumentLocation,
                                    argumentArray.ElementType,
                                    argumentTypeByAssumption,
                                    parameterArray.ElementType,
                                    param,
                                    digThroughToBasesAndImplements,
                                    Conversions.CombineConversionRequirements(inferenceRestrictions, If(argumentIsAarrayLiteral, RequiredConversion.Any, RequiredConversion.ArrayElement)))
                        End If
                    End If

                    Return False

                End If

                Return True
            End Function


            ' Given an argument type, a parameter type, and a set of (possibly unbound) type arguments
            ' to a generic method, infer type arguments corresponding to type parameters that occur
            ' in the parameter type.
            '
            ' A return value of false indicates that inference fails.
            '
            ' This routine is given an argument e.g. "List(Of IEnumerable(Of Int))",
            ' and a parameter e.g. "IEnumerable(Of IEnumerable(Of T))".
            ' The task is to infer hints for T, e.g. "T=int".
            ' This function takes care of allowing (in this example) List(Of _) to match IEnumerable(Of _).
            ' As for the real work, i.e. matching the contents, we invoke "InferTypeArgumentsFromArgumentDirectly"
            ' to do that.
            '
            ' Note: this function returns "false" if it failed to pattern-match between argument and parameter type,
            ' and "true" if it succeeded.
            ' Success in pattern-matching may or may not produce type-hints for generic parameters.
            ' If it happened not to produce any type-hints, then maybe other argument/parameter pairs will have produced
            ' their own type hints that allow inference to succeed, or maybe no-one else will have produced type hints,
            ' or maybe other people will have produced conflicting type hints. In those cases, we'd return True from
            ' here (to show success at pattern-matching) and leave the downstream code to produce an error message about
            ' failing to infer T.
            Friend Function InferTypeArgumentsFromArgument(
                argumentLocation As SyntaxNode,
                argumentType As TypeSymbol,
                argumentTypeByAssumption As Boolean,
                parameterType As TypeSymbol,
                param As ParameterSymbol,
                digThroughToBasesAndImplements As MatchGenericArgumentToParameter,
                inferenceRestrictions As RequiredConversion
            ) As Boolean

                If Not RefersToGenericParameterToInferArgumentFor(parameterType) Then
                    Return True
                End If

                ' First try to the things directly. Only if this fails will we bother searching for things like List->IEnumerable.
                Dim Inferred As Boolean = InferTypeArgumentsFromArgumentDirectly(
                    argumentLocation,
                    argumentType,
                    argumentTypeByAssumption,
                    parameterType,
                    param,
                    digThroughToBasesAndImplements,
                    inferenceRestrictions)

                If Inferred Then
                    Return True
                End If

                If parameterType.IsTypeParameter() Then
                    ' If we failed to match an argument against a generic parameter T, it means that the
                    ' argument was something unmatchable, e.g. an AddressOf.
                    Return False
                End If


                ' If we didn't find a direct match, we will have to look in base classes for a match.
                ' We'll either fix ParameterType and look amongst the bases of ArgumentType,
                ' or we'll fix ArgumentType and look amongst the bases of ParameterType,
                ' depending on the "DigThroughToBasesAndImplements" flag. This flag is affected by
                ' covariance and contravariance...

                If digThroughToBasesAndImplements = MatchGenericArgumentToParameter.MatchGenericArgumentToParameterExactly Then
                    Return False
                End If

                ' Special handling for Anonymous Delegates.
                If argumentType IsNot Nothing AndAlso argumentType.IsDelegateType() AndAlso parameterType.IsDelegateType() AndAlso
                   digThroughToBasesAndImplements = MatchGenericArgumentToParameter.MatchBaseOfGenericArgumentToParameter AndAlso
                   (inferenceRestrictions = RequiredConversion.Any OrElse inferenceRestrictions = RequiredConversion.AnyReverse OrElse
                    inferenceRestrictions = RequiredConversion.AnyAndReverse) Then

                    Dim argumentDelegateType = DirectCast(argumentType, NamedTypeSymbol)
                    Dim argumentInvokeProc As MethodSymbol = argumentDelegateType.DelegateInvokeMethod
                    Dim parameterDelegateType = DirectCast(parameterType, NamedTypeSymbol)
                    Dim parameterInvokeProc As MethodSymbol = parameterDelegateType.DelegateInvokeMethod

                    Debug.Assert(argumentInvokeProc IsNot Nothing OrElse Not argumentDelegateType.IsAnonymousType)

                    ' Note, null check for parameterInvokeDeclaration should also filter out MultiCastDelegate type.
                    If argumentDelegateType.IsAnonymousType AndAlso Not parameterDelegateType.IsAnonymousType AndAlso
                       parameterInvokeProc IsNot Nothing AndAlso parameterInvokeProc.GetUseSiteErrorInfo() Is Nothing Then
                        ' Some trickery relating to the fact that anonymous delegates can be converted to any delegate type.
                        ' We are trying to match the anonymous delegate "BaseSearchType" onto the delegate "FixedType". e.g.
                        ' Dim f = function(i as integer) i   // ArgumentType = VB$AnonymousDelegate`2(Of Integer,Integer)
                        ' inf(f)                             // ParameterType might be e.g. D(Of T) for some function inf(Of T)(f as D(Of T))
                        '                                    // maybe defined as Delegate Function D(Of T)(x as T) as T.
                        ' We're looking to achieve the same functionality in pattern-matching these types as we already
                        ' have for calling "inf(function(i as integer) i)" directly. 
                        ' It allows any VB conversion from param-of-fixed-type to param-of-base-type (not just reference conversions).
                        ' But it does allow a zero-argument BaseSearchType to be used for a FixedType with more.
                        ' And it does allow a function BaseSearchType to be used for a sub FixedType.
                        '
                        ' Anyway, the plan is to match each of the parameters in the ArgumentType delegate
                        ' to the equivalent parameters in the ParameterType delegate, and also match the return types.
                        '
                        ' This only works for "ConversionRequired::Any", i.e. using VB conversion semantics. It doesn't work for
                        ' reference conversions. As for the AnyReverse/AnyAndReverse, well, in Orcas that was guaranteed
                        ' to fail type inference (i.e. return a false from this function). In Dev10 we will let it succeed
                        ' with some inferred types, for the sake of better error messages, even though we know that ultimately
                        ' it will fail (because no non-anonymous delegate type can be converted to a delegate type).
                        Dim argumentParams As ImmutableArray(Of ParameterSymbol) = argumentInvokeProc.Parameters
                        Dim parameterParams As ImmutableArray(Of ParameterSymbol) = parameterInvokeProc.Parameters

                        If parameterParams.Length <> argumentParams.Length AndAlso argumentParams.Length <> 0 Then
                            ' If parameter-counts are mismatched then it's a failure.
                            ' Exception: Zero-argument relaxation: we allow a parameterless VB$AnonymousDelegate argument
                            ' to be supplied to a function which expects a parameterfull delegate.
                            Return False
                        End If

                        ' First we'll check that the argument types all match. 
                        For i As Integer = 0 To argumentParams.Length - 1
                            If argumentParams(i).IsByRef <> parameterParams(i).IsByRef Then
                                ' Require an exact match between ByRef/ByVal, since that's how type inference of lambda expressions works.
                                Return False
                            End If

                            If Not InferTypeArgumentsFromArgument(
                                        argumentLocation,
                                        argumentParams(i).Type,
                                        argumentTypeByAssumption,
                                        parameterParams(i).Type,
                                        param,
                                        MatchGenericArgumentToParameter.MatchArgumentToBaseOfGenericParameter,
                                        RequiredConversion.AnyReverse) Then  ' AnyReverse: contravariance in delegate arguments
                                Return False
                            End If
                        Next

                        ' Now check that the return type matches.
                        ' Note: we allow a *function* VB$AnonymousDelegate to be supplied to a function which expects a *sub* delegate.

                        If parameterInvokeProc.IsSub Then
                            ' A *sub* delegate parameter can accept either a *function* or a *sub* argument:
                            Return True
                        ElseIf argumentInvokeProc.IsSub Then
                            ' A *function* delegate parameter cannot accept a *sub* argument.
                            Return False
                        Else
                            ' Otherwise, a function argument VB$AnonymousDelegate was supplied to a function parameter:
                            Return InferTypeArgumentsFromArgument(
                                        argumentLocation,
                                        argumentInvokeProc.ReturnType,
                                        argumentTypeByAssumption,
                                        parameterInvokeProc.ReturnType,
                                        param,
                                        MatchGenericArgumentToParameter.MatchBaseOfGenericArgumentToParameter,
                                        RequiredConversion.Any) ' Any: covariance in delegate returns
                        End If
                    End If
                End If

                ' MatchBaseOfGenericArgumentToParameter: used for covariant situations,
                ' e.g. matching argument "List(Of _)" to parameter "ByVal x as IEnumerable(Of _)".
                ' 
                ' Otherwise, MatchArgumentToBaseOfGenericParameter, used for contravariant situations,
                ' e.g. when matching argument "Action(Of IEnumerable(Of _))" to parameter "ByVal x as Action(Of List(Of _))".

                Dim fContinue As Boolean = False

                If digThroughToBasesAndImplements = MatchGenericArgumentToParameter.MatchBaseOfGenericArgumentToParameter Then
                    fContinue = FindMatchingBase(argumentType, parameterType)
                Else
                    fContinue = FindMatchingBase(parameterType, argumentType)
                End If

                If Not fContinue Then
                    Return False
                End If

                ' NOTE: baseSearchType was a REFERENCE, to either ArgumentType or ParameterType.
                ' Therefore the above statement has altered either ArgumentType or ParameterType.
                Return InferTypeArgumentsFromArgumentDirectly(
                            argumentLocation,
                            argumentType,
                            argumentTypeByAssumption,
                            parameterType,
                            param,
                            digThroughToBasesAndImplements,
                            inferenceRestrictions)
            End Function

            Private Function FindMatchingBase(
                ByRef baseSearchType As TypeSymbol,
                ByRef fixedType As TypeSymbol
            ) As Boolean

                Dim fixedTypeAsNamedType = If(fixedType.Kind = SymbolKind.NamedType, DirectCast(fixedType, NamedTypeSymbol), Nothing)

                If fixedTypeAsNamedType Is Nothing OrElse Not fixedTypeAsNamedType.IsGenericType Then
                    ' If the fixed candidate isn't a generic (e.g. matching argument IList(Of String) to non-generic parameter IList),
                    ' then we won't learn anything about generic type parameters here:
                    Return False
                End If

                Dim fixedTypeTypeKind As TypeKind = fixedType.TypeKind

                If fixedTypeTypeKind <> TypeKind.Class AndAlso fixedTypeTypeKind <> TypeKind.Interface Then
                    ' Whatever "BaseSearchType" is, it can only inherit from "FixedType" if FixedType is a class/interface.
                    ' (it's impossible to inherit from anything else).
                    Return False
                End If

                Dim baseSearchTypeKind As SymbolKind = baseSearchType.Kind

                If baseSearchTypeKind <> SymbolKind.NamedType AndAlso baseSearchTypeKind <> SymbolKind.TypeParameter AndAlso
                   Not (baseSearchTypeKind = SymbolKind.ArrayType AndAlso DirectCast(baseSearchType, ArrayTypeSymbol).IsSZArray) Then
                    ' The things listed above are the only ones that have bases that could ever lead anywhere useful.
                    ' NamedType is satisfied by interfaces, structures, enums, delegates and modules as well as just classes.
                    Return False
                End If

                If baseSearchType.IsSameTypeIgnoringAll(fixedType) Then
                    ' If the types checked were already identical, then exit
                    Return False
                End If

                ' Otherwise, if we got through all the above tests, then it really is worth searching through the base
                ' types to see if that helps us find a match.
                Dim matchingBase As TypeSymbol = Nothing

                If fixedTypeTypeKind = TypeKind.Class Then
                    FindMatchingBaseClass(baseSearchType, fixedType, matchingBase)
                Else
                    Debug.Assert(fixedTypeTypeKind = TypeKind.Interface)
                    FindMatchingBaseInterface(baseSearchType, fixedType, matchingBase)
                End If

                If matchingBase Is Nothing Then
                    Return False
                End If

                ' And this is what we found
                baseSearchType = matchingBase

                Return True
            End Function

            Private Shared Function SetMatchIfNothingOrEqual(type As TypeSymbol, ByRef match As TypeSymbol) As Boolean
                If match Is Nothing Then
                    match = type
                    Return True
                ElseIf match.IsSameTypeIgnoringAll(type) Then
                    Return True
                Else
                    match = Nothing
                    Return False
                End If
            End Function

            ''' <summary>
            ''' Returns False if the search should be cancelled.
            ''' </summary>
            Private Function FindMatchingBaseInterface(derivedType As TypeSymbol, baseInterface As TypeSymbol, ByRef match As TypeSymbol) As Boolean

                Select Case derivedType.Kind
                    Case SymbolKind.TypeParameter
                        For Each constraint In DirectCast(derivedType, TypeParameterSymbol).ConstraintTypesWithDefinitionUseSiteDiagnostics(Me.UseSiteDiagnostics)
                            If constraint.OriginalDefinition.IsSameTypeIgnoringAll(baseInterface.OriginalDefinition) Then
                                If Not SetMatchIfNothingOrEqual(constraint, match) Then
                                    Return False
                                End If
                            End If

                            If Not FindMatchingBaseInterface(constraint, baseInterface, match) Then
                                Return False
                            End If
                        Next

                    Case Else
                        For Each [interface] In derivedType.AllInterfacesWithDefinitionUseSiteDiagnostics(Me.UseSiteDiagnostics)
                            If [interface].OriginalDefinition.IsSameTypeIgnoringAll(baseInterface.OriginalDefinition) Then
                                If Not SetMatchIfNothingOrEqual([interface], match) Then
                                    Return False
                                End If
                            End If
                        Next

                End Select

                Return True
            End Function

            ''' <summary>
            ''' Returns False if the search should be cancelled.
            ''' </summary>
            Private Function FindMatchingBaseClass(derivedType As TypeSymbol, baseClass As TypeSymbol, ByRef match As TypeSymbol) As Boolean
                Select Case derivedType.Kind
                    Case SymbolKind.TypeParameter
                        For Each constraint In DirectCast(derivedType, TypeParameterSymbol).ConstraintTypesWithDefinitionUseSiteDiagnostics(Me.UseSiteDiagnostics)
                            If constraint.OriginalDefinition.IsSameTypeIgnoringAll(baseClass.OriginalDefinition) Then
                                If Not SetMatchIfNothingOrEqual(constraint, match) Then
                                    Return False
                                End If
                            End If

                            ' TODO: Do we need to continue even if we already have a matching base class?
                            '       It looks like Dev10 continues.
                            If Not FindMatchingBaseClass(constraint, baseClass, match) Then
                                Return False
                            End If
                        Next

                    Case Else

                        Dim baseType As NamedTypeSymbol = derivedType.BaseTypeWithDefinitionUseSiteDiagnostics(Me.UseSiteDiagnostics)

                        While baseType IsNot Nothing
                            If baseType.OriginalDefinition.IsSameTypeIgnoringAll(baseClass.OriginalDefinition) Then
                                If Not SetMatchIfNothingOrEqual(baseType, match) Then
                                    Return False
                                End If
                                Exit While
                            End If

                            baseType = baseType.BaseTypeWithDefinitionUseSiteDiagnostics(Me.UseSiteDiagnostics)
                        End While

                End Select

                Return True
            End Function

            Public Function InferTypeArgumentsFromAddressOfArgument(
                argument As BoundExpression,
                parameterType As TypeSymbol,
                param As ParameterSymbol
            ) As Boolean

                If parameterType.IsDelegateType() Then
                    Dim delegateType = DirectCast(ConstructParameterTypeIfNeeded(parameterType), NamedTypeSymbol)

                    ' Now find the invoke method of the delegate
                    Dim invokeMethod As MethodSymbol = delegateType.DelegateInvokeMethod

                    If invokeMethod Is Nothing OrElse invokeMethod.GetUseSiteErrorInfo() IsNot Nothing Then
                        ' If we don't have an Invoke method, just bail.
                        Return False
                    End If

                    Dim returnType As TypeSymbol = invokeMethod.ReturnType

                    ' If the return type doesn't refer to parameters, no inference required.
                    If Not RefersToGenericParameterToInferArgumentFor(returnType) Then
                        Return True
                    End If

                    Dim addrOf = DirectCast(argument, BoundAddressOfOperator)
                    Dim fromMethod As MethodSymbol = Nothing
                    Dim methodConversions As MethodConversionKind = MethodConversionKind.Identity
                    Dim diagnostics = DiagnosticBag.GetInstance()

                    Dim matchingMethod As KeyValuePair(Of MethodSymbol, MethodConversionKind) = Binder.ResolveMethodForDelegateInvokeFullAndRelaxed(
                        addrOf,
                        invokeMethod,
                        ignoreMethodReturnType:=True,
                        diagnostics:=diagnostics)

                    diagnostics.Free()

                    fromMethod = matchingMethod.Key
                    methodConversions = matchingMethod.Value

                    If fromMethod Is Nothing OrElse (methodConversions And MethodConversionKind.AllErrorReasons) <> 0 OrElse
                       (addrOf.Binder.OptionStrict = OptionStrict.On AndAlso Conversions.IsNarrowingMethodConversion(methodConversions, isForAddressOf:=True)) Then
                        Return False
                    End If

                    If fromMethod.IsSub Then
                        ReportNotFailedInferenceDueToObject()
                        Return True
                    End If

                    Dim targetReturnType As TypeSymbol = fromMethod.ReturnType

                    If RefersToGenericParameterToInferArgumentFor(targetReturnType) Then
                        ' Return false if we didn't make any inference progress.
                        Return False
                    End If

                    Return InferTypeArgumentsFromArgument(
                                argument.Syntax,
                                targetReturnType,
                                argumentTypeByAssumption:=False,
                                parameterType:=returnType,
                                param:=param,
                                digThroughToBasesAndImplements:=MatchGenericArgumentToParameter.MatchBaseOfGenericArgumentToParameter,
                                inferenceRestrictions:=RequiredConversion.Any)
                End If

                ' We did not infer anything for this addressOf, AddressOf can never be of type Object, so mark inference
                ' as not failed due to object.
                ReportNotFailedInferenceDueToObject()
                Return True

            End Function

            Public Function InferTypeArgumentsFromLambdaArgument(
                argument As BoundExpression,
                parameterType As TypeSymbol,
                param As ParameterSymbol
            ) As Boolean

                Debug.Assert(argument.Kind = BoundKind.UnboundLambda OrElse
                             argument.Kind = BoundKind.QueryLambda OrElse
                             argument.Kind = BoundKind.GroupTypeInferenceLambda)

                If parameterType.IsTypeParameter() Then
                    Dim anonymousLambdaType As TypeSymbol = Nothing

                    Select Case argument.Kind
                        Case BoundKind.QueryLambda
                            ' Do not infer Anonymous Delegate type from query lambda.

                        Case BoundKind.GroupTypeInferenceLambda
                            ' Can't infer from this lambda.

                        Case BoundKind.UnboundLambda
                            ' Infer Anonymous Delegate type from unbound lambda.
                            Dim inferredAnonymousDelegate As KeyValuePair(Of NamedTypeSymbol, ImmutableArray(Of Diagnostic)) = DirectCast(argument, UnboundLambda).InferredAnonymousDelegate

                            If (inferredAnonymousDelegate.Value.IsDefault OrElse Not inferredAnonymousDelegate.Value.HasAnyErrors()) Then

                                Dim delegateInvokeMethod As MethodSymbol = Nothing

                                If inferredAnonymousDelegate.Key IsNot Nothing Then
                                    delegateInvokeMethod = inferredAnonymousDelegate.Key.DelegateInvokeMethod
                                End If

                                If delegateInvokeMethod IsNot Nothing AndAlso delegateInvokeMethod.ReturnType IsNot LambdaSymbol.ReturnTypeIsUnknown Then
                                    anonymousLambdaType = inferredAnonymousDelegate.Key
                                End If
                            End If
                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(argument.Kind)
                    End Select

                    If anonymousLambdaType IsNot Nothing Then
                        Return InferTypeArgumentsFromArgument(
                            argument.Syntax,
                            anonymousLambdaType,
                            argumentTypeByAssumption:=False,
                            parameterType:=parameterType,
                            param:=param,
                            digThroughToBasesAndImplements:=MatchGenericArgumentToParameter.MatchBaseOfGenericArgumentToParameter,
                            inferenceRestrictions:=RequiredConversion.Any)
                    Else
                        Return True
                    End If

                ElseIf parameterType.IsDelegateType() Then
                    Dim parameterDelegateType = DirectCast(parameterType, NamedTypeSymbol)

                    ' First, we need to build a partial type substitution using the type of
                    ' arguments as they stand right now, with some of them still being uninferred.

                    ' TODO: Doesn't this make the inference algorithm order dependent? For example, if we were to 
                    '       infer more stuff from other non-lambda arguments, we might have a better chance to have 
                    '       more type information for the lambda, allowing successful lambda interpretation.
                    '       Perhaps the graph doesn't allow us to get here until all "inputs" for lambda parameters
                    '       are inferred.
                    Dim delegateType = DirectCast(ConstructParameterTypeIfNeeded(parameterDelegateType), NamedTypeSymbol)

                    ' Now find the invoke method of the delegate

                    Dim invokeMethod As MethodSymbol = delegateType.DelegateInvokeMethod

                    If invokeMethod Is Nothing OrElse invokeMethod.GetUseSiteErrorInfo() IsNot Nothing Then
                        ' If we don't have an Invoke method, just bail.
                        Return True
                    End If

                    Dim returnType As TypeSymbol = invokeMethod.ReturnType

                    ' If the return type doesn't refer to parameters, no inference required.
                    If Not RefersToGenericParameterToInferArgumentFor(returnType) Then
                        Return True
                    End If

                    Dim lambdaParams As ImmutableArray(Of ParameterSymbol)

                    Select Case argument.Kind
                        Case BoundKind.QueryLambda
                            lambdaParams = DirectCast(argument, BoundQueryLambda).LambdaSymbol.Parameters
                        Case BoundKind.GroupTypeInferenceLambda
                            lambdaParams = DirectCast(argument, GroupTypeInferenceLambda).Parameters
                        Case BoundKind.UnboundLambda
                            lambdaParams = DirectCast(argument, UnboundLambda).Parameters
                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(argument.Kind)
                    End Select

                    Dim delegateParams As ImmutableArray(Of ParameterSymbol) = invokeMethod.Parameters

                    If lambdaParams.Length > delegateParams.Length Then
                        Return True
                    End If

                    For i As Integer = 0 To lambdaParams.Length - 1 Step 1
                        Dim lambdaParam As ParameterSymbol = lambdaParams(i)
                        Dim delegateParam As ParameterSymbol = delegateParams(i)

                        If lambdaParam.Type Is Nothing Then
                            ' If a lambda parameter has no type and the delegate parameter refers
                            ' to an unbound generic parameter, we can't infer yet.
                            If RefersToGenericParameterToInferArgumentFor(delegateParam.Type) Then
                                ' Skip this type argument and other parameters will infer it or
                                ' if that doesn't happen it will report an error.

                                ' TODO: Why does it make sense to continue here? It looks like we can infer something from
                                '       lambda's return type based on incomplete information. Also, this 'if' is redundant,
                                '       there is nothing left to do in this loop anyway, and "continue" doesn't change anything. 
                                Continue For
                            End If
                        Else
                            ' report the type of the lambda parameter to the delegate parameter.
                            ' !!! Unlike Dev10, we are using MatchArgumentToBaseOfGenericParameter because a value of generic 
                            ' !!! parameter will be passed into the parameter of argument type.
                            InferTypeArgumentsFromArgument(
                                argument.Syntax,
                                lambdaParam.Type,
                                argumentTypeByAssumption:=False,
                                parameterType:=delegateParam.Type,
                                param:=param,
                                digThroughToBasesAndImplements:=MatchGenericArgumentToParameter.MatchArgumentToBaseOfGenericParameter,
                                inferenceRestrictions:=RequiredConversion.Any)
                        End If
                    Next

                    ' OK, now try to infer delegates return type from the lambda.
                    Dim lambdaReturnType As TypeSymbol

                    Select Case argument.Kind
                        Case BoundKind.QueryLambda
                            Dim queryLambda = DirectCast(argument, BoundQueryLambda)

                            lambdaReturnType = queryLambda.LambdaSymbol.ReturnType
                            If lambdaReturnType Is LambdaSymbol.ReturnTypePendingDelegate Then
                                lambdaReturnType = queryLambda.Expression.Type

                                If lambdaReturnType Is Nothing Then
                                    If Me.Diagnostic Is Nothing Then
                                        Me.Diagnostic = New DiagnosticBag()
                                    End If

                                    Debug.Assert(Me.Diagnostic IsNot Nothing)
                                    lambdaReturnType = queryLambda.LambdaSymbol.ContainingBinder.MakeRValue(queryLambda.Expression, Me.Diagnostic).Type
                                End If
                            End If

                        Case BoundKind.GroupTypeInferenceLambda
                            lambdaReturnType = DirectCast(argument, GroupTypeInferenceLambda).InferLambdaReturnType(delegateParams)

                        Case BoundKind.UnboundLambda
                            Dim unboundLambda = DirectCast(argument, UnboundLambda)

                            If unboundLambda.IsFunctionLambda Then
                                Dim inferenceSignature As New UnboundLambda.TargetSignature(delegateParams, unboundLambda.Binder.Compilation.GetSpecialType(SpecialType.System_Void), returnsByRef:=False)
                                Dim returnTypeInfo As KeyValuePair(Of TypeSymbol, ImmutableArray(Of Diagnostic)) = unboundLambda.InferReturnType(inferenceSignature)

                                If Not returnTypeInfo.Value.IsDefault AndAlso returnTypeInfo.Value.HasAnyErrors() Then
                                    lambdaReturnType = Nothing

                                    ' Let's keep return type inference errors
                                    If Me.Diagnostic Is Nothing Then
                                        Me.Diagnostic = New DiagnosticBag()
                                    End If

                                    Me.Diagnostic.AddRange(returnTypeInfo.Value)

                                ElseIf returnTypeInfo.Key Is LambdaSymbol.ReturnTypeIsUnknown Then
                                    lambdaReturnType = Nothing

                                Else
                                    Dim boundLambda As BoundLambda = unboundLambda.Bind(New UnboundLambda.TargetSignature(inferenceSignature.ParameterTypes,
                                                                                                                          inferenceSignature.ParameterIsByRef,
                                                                                                                          returnTypeInfo.Key,
                                                                                                                          returnsByRef:=False))

                                    Debug.Assert(boundLambda.LambdaSymbol.ReturnType Is returnTypeInfo.Key)
                                    If Not boundLambda.HasErrors AndAlso Not boundLambda.Diagnostics.HasAnyErrors Then
                                        lambdaReturnType = returnTypeInfo.Key

                                        ' Let's keep return type inference warnings, if any.
                                        If Not returnTypeInfo.Value.IsDefaultOrEmpty Then
                                            If Me.Diagnostic Is Nothing Then
                                                Me.Diagnostic = New DiagnosticBag()
                                            End If

                                            Me.Diagnostic.AddRange(returnTypeInfo.Value)
                                        End If
                                    Else
                                        lambdaReturnType = Nothing

                                        ' Let's preserve diagnostics that caused the failure
                                        If Not boundLambda.Diagnostics.IsDefaultOrEmpty Then
                                            If Me.Diagnostic Is Nothing Then
                                                Me.Diagnostic = New DiagnosticBag()
                                            End If

                                            Me.Diagnostic.AddRange(boundLambda.Diagnostics)
                                        End If
                                    End If
                                End If

                                ' But in the case of async/iterator lambdas, e.g. pass "Async Function() 1" to a parameter 
                                ' of type "Func(Of Task(Of T))" then we have to dig in further and match 1 to T...
                                If (unboundLambda.Flags And (SourceMemberFlags.Async Or SourceMemberFlags.Iterator)) <> 0 AndAlso
                                        lambdaReturnType IsNot Nothing AndAlso lambdaReturnType.Kind = SymbolKind.NamedType AndAlso
                                        returnType IsNot Nothing AndAlso returnType.Kind = SymbolKind.NamedType Then

                                    ' By this stage we know that
                                    '   * we have an async/iterator lambda argument
                                    '   * the parameter-to-match is a delegate type whose result type refers to generic parameters
                                    ' The parameter might be a delegate with result type e.g. "Task(Of T)" in which case we have
                                    ' to dig in. Or it might be a delegate with result type "T" in which case we
                                    ' don't dig in.
                                    Dim lambdaReturnNamedType = DirectCast(lambdaReturnType, NamedTypeSymbol)
                                    Dim returnNamedType = DirectCast(returnType, NamedTypeSymbol)
                                    If lambdaReturnNamedType.Arity = 1 AndAlso
                                            IsSameTypeIgnoringAll(lambdaReturnNamedType.OriginalDefinition,
                                                                              returnNamedType.OriginalDefinition) Then

                                        ' We can assume that the lambda will have return type Task(Of T) or IEnumerable(Of T)
                                        ' or IEnumerator(Of T) as appropriate. That's already been ensured by the lambda-interpretation.
                                        Debug.Assert(TypeSymbol.Equals(lambdaReturnNamedType.OriginalDefinition, argument.GetBinderFromLambda().Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T), TypeCompareKind.ConsiderEverything) OrElse
                                                     TypeSymbol.Equals(lambdaReturnNamedType.OriginalDefinition, argument.GetBinderFromLambda().Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T), TypeCompareKind.ConsiderEverything) OrElse
                                                     TypeSymbol.Equals(lambdaReturnNamedType.OriginalDefinition, argument.GetBinderFromLambda().Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerator_T), TypeCompareKind.ConsiderEverything))

                                        lambdaReturnType = lambdaReturnNamedType.TypeArgumentWithDefinitionUseSiteDiagnostics(0, Me.UseSiteDiagnostics)
                                        returnType = returnNamedType.TypeArgumentWithDefinitionUseSiteDiagnostics(0, Me.UseSiteDiagnostics)
                                    End If

                                End If

                            Else
                                lambdaReturnType = Nothing

                                If Not invokeMethod.IsSub AndAlso (unboundLambda.Flags And SourceMemberFlags.Async) <> 0 Then
                                    Dim boundLambda As BoundLambda = unboundLambda.Bind(New UnboundLambda.TargetSignature(delegateParams,
                                                                            unboundLambda.Binder.Compilation.GetSpecialType(SpecialType.System_Void),
                                                                            returnsByRef:=False))

                                    If Not boundLambda.HasErrors AndAlso Not boundLambda.Diagnostics.HasAnyErrors() Then
                                        If _asyncLambdaSubToFunctionMismatch Is Nothing Then
                                            _asyncLambdaSubToFunctionMismatch = New HashSet(Of BoundExpression)(ReferenceEqualityComparer.Instance)
                                        End If

                                        _asyncLambdaSubToFunctionMismatch.Add(unboundLambda)
                                    End If
                                End If
                            End If
                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(argument.Kind)
                    End Select

                    If lambdaReturnType Is Nothing Then
                        ' Inference failed, give up.
                        Return False
                    End If

                    If lambdaReturnType.IsErrorType() Then
                        Return True
                    End If

                    ' Now infer from the result type
                    ' not ArgumentTypeByAssumption ??? lwischik: but maybe it should...
                    Return InferTypeArgumentsFromArgument(
                            argument.Syntax,
                            lambdaReturnType,
                            argumentTypeByAssumption:=False,
                            parameterType:=returnType,
                            param:=param, digThroughToBasesAndImplements:=MatchGenericArgumentToParameter.MatchBaseOfGenericArgumentToParameter,
                            inferenceRestrictions:=RequiredConversion.Any)

                ElseIf TypeSymbol.Equals(parameterType.OriginalDefinition, argument.GetBinderFromLambda().Compilation.GetWellKnownType(WellKnownType.System_Linq_Expressions_Expression_T), TypeCompareKind.ConsiderEverything) Then
                    ' If we've got an Expression(Of T), skip through to T
                    Return InferTypeArgumentsFromLambdaArgument(argument, DirectCast(parameterType, NamedTypeSymbol).TypeArgumentWithDefinitionUseSiteDiagnostics(0, Me.UseSiteDiagnostics), param)
                End If

                Return True
            End Function

            Public Function ConstructParameterTypeIfNeeded(parameterType As TypeSymbol) As TypeSymbol
                ' First, we need to build a partial type substitution using the type of
                ' arguments as they stand right now, with some of them still being uninferred.

                Dim methodSymbol As MethodSymbol = Candidate
                Dim typeArguments = ArrayBuilder(Of TypeWithModifiers).GetInstance(_typeParameterNodes.Length)

                For i As Integer = 0 To _typeParameterNodes.Length - 1 Step 1
                    Dim typeNode As TypeParameterNode = _typeParameterNodes(i)
                    Dim newType As TypeSymbol

                    If typeNode Is Nothing OrElse typeNode.CandidateInferredType Is Nothing Then
                        'No substitution
                        newType = methodSymbol.TypeParameters(i)
                    Else
                        newType = typeNode.CandidateInferredType
                    End If

                    typeArguments.Add(New TypeWithModifiers(newType))
                Next

                Dim partialSubstitution = TypeSubstitution.CreateAdditionalMethodTypeParameterSubstitution(methodSymbol.ConstructedFrom, typeArguments.ToImmutableAndFree())

                ' Now we apply the partial substitution to the delegate type, leaving uninferred type parameters as is
                Return parameterType.InternalSubstituteTypeParameters(partialSubstitution).Type
            End Function

            Public Sub ReportAmbiguousInferenceError(typeInfos As ArrayBuilder(Of DominantTypeDataTypeInference))
                Debug.Assert(typeInfos.Count() >= 2, "Must have at least 2 elements in the list")

                ' Since they get added fifo, we need to walk the list backward.
                For i As Integer = 1 To typeInfos.Count - 1 Step 1
                    Dim currentTypeInfo As DominantTypeDataTypeInference = typeInfos(i)

                    If Not currentTypeInfo.InferredFromObject Then
                        ReportNotFailedInferenceDueToObject()
                        ' TODO: Should we exit the loop? For some reason Dev10 keeps going.
                    End If
                Next
            End Sub

            Public Sub ReportIncompatibleInferenceError(
                typeInfos As ArrayBuilder(Of DominantTypeDataTypeInference))
                If typeInfos.Count < 1 Then
                    Return
                End If

                ' Since they get added fifo, we need to walk the list backward.
                For i As Integer = 1 To typeInfos.Count - 1 Step 1
                    Dim currentTypeInfo As DominantTypeDataTypeInference = typeInfos(i)

                    If Not currentTypeInfo.InferredFromObject Then
                        ReportNotFailedInferenceDueToObject()
                        ' TODO: Should we exit the loop? For some reason Dev10 keeps going.
                    End If
                Next
            End Sub

            Public Sub RegisterErrorReasons(inferenceErrorReasons As InferenceErrorReasons)
                _inferenceErrorReasons = _inferenceErrorReasons Or inferenceErrorReasons
            End Sub

        End Class
    End Class
End Namespace
