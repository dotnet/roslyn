' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Rewrite lambda that are being converted to LINQ expression trees (Expression(Of T))
    ''' </summary>
    ''' <remarks>
    ''' A lot of code is #If False disabled until it can be tested. 
    ''' </remarks>
    Partial Friend Class ExpressionLambdaRewriter
        Private ReadOnly _binder As Binder
        Private ReadOnly _factory As SyntheticBoundNodeFactory
        Private ReadOnly _typeMap As TypeSubstitution
        Private ReadOnly _expressionType As NamedTypeSymbol

        Private _int32Type As NamedTypeSymbol
        Private _objectType As NamedTypeSymbol
        Private _memberInfoType As NamedTypeSymbol
        Private _memberBindingType As NamedTypeSymbol
        Private _elementInitType As NamedTypeSymbol

        Private ReadOnly _parameterMap As Dictionary(Of ParameterSymbol, BoundExpression) = New Dictionary(Of ParameterSymbol, BoundExpression)()

        Private _recursionDepth As Integer

        Private Sub New(currentMethod As MethodSymbol, compilationState As TypeCompilationState, typeMap As TypeSubstitution, binder As Binder,
                        node As SyntaxNode, recursionDepth As Integer, diagnostics As BindingDiagnosticBag)
            _binder = binder
            _typeMap = typeMap
            _factory = New SyntheticBoundNodeFactory(Nothing, currentMethod, node, compilationState, diagnostics)
            _expressionType = _factory.WellKnownType(WellKnownType.System_Linq_Expressions_Expression)
            _recursionDepth = recursionDepth
        End Sub

        Public ReadOnly Property ElementInitType As NamedTypeSymbol
            Get
                If _elementInitType Is Nothing Then
                    _elementInitType = Me._factory.WellKnownType(WellKnownType.System_Linq_Expressions_ElementInit)
                End If
                Return _elementInitType
            End Get
        End Property

        Public ReadOnly Property MemberBindingType As NamedTypeSymbol
            Get
                If _memberBindingType Is Nothing Then
                    _memberBindingType = Me._factory.WellKnownType(WellKnownType.System_Linq_Expressions_MemberBinding)
                End If
                Return _memberBindingType
            End Get
        End Property

        Public ReadOnly Property MemberInfoType As NamedTypeSymbol
            Get
                If _memberInfoType Is Nothing Then
                    _memberInfoType = Me._factory.WellKnownType(WellKnownType.System_Reflection_MemberInfo)
                End If
                Return _memberInfoType
            End Get
        End Property

        Public ReadOnly Property Int32Type As NamedTypeSymbol
            Get
                If _int32Type Is Nothing Then
                    _int32Type = _factory.SpecialType(SpecialType.System_Int32)
                End If
                Return _int32Type
            End Get
        End Property

        Public ReadOnly Property ObjectType As NamedTypeSymbol
            Get
                If _objectType Is Nothing Then
                    _objectType = _factory.SpecialType(SpecialType.System_Object)
                End If
                Return _objectType
            End Get
        End Property

        ''' <summary>
        ''' Rewrite a bound lambda into a bound node that will create the corresponding expression tree at run time.
        ''' </summary>
        Friend Shared Function RewriteLambda(node As BoundLambda,
                                             currentMethod As MethodSymbol,
                                             delegateType As NamedTypeSymbol,
                                             compilationState As TypeCompilationState,
                                             typeMap As TypeSubstitution,
                                             diagnostics As BindingDiagnosticBag,
                                             rewrittenNodes As HashSet(Of BoundNode),
                                             recursionDepth As Integer) As BoundExpression

            Dim r As New ExpressionLambdaRewriter(currentMethod, compilationState, typeMap, node.LambdaSymbol.ContainingBinder, node.Syntax, recursionDepth, diagnostics)
            Dim expressionTree As BoundExpression = r.VisitLambdaInternal(node, delegateType)

            If Not expressionTree.HasErrors Then
                expressionTree = LocalRewriter.RewriteExpressionTree(expressionTree,
                                                                     currentMethod,
                                                                     compilationState,
                                                                     previousSubmissionFields:=Nothing,
                                                                     diagnostics:=diagnostics,
                                                                     rewrittenNodes:=rewrittenNodes,
                                                                     recursionDepth:=recursionDepth)
            End If

            Return expressionTree
        End Function

        Private ReadOnly Property Diagnostics As BindingDiagnosticBag
            Get
                Return _factory.Diagnostics
            End Get
        End Property

        Private Function TranslateLambdaBody(block As BoundBlock) As BoundExpression
            ' VB expression trees can be only one statement. Similar analysis is performed 
            ' in DiagnosticsPass, but it does not take into account how the statements
            ' are rewritten, so here we recheck lowered lambda bodies as well.

            ' There might be a sequence point at the beginning.
            ' There may be a label and return statement at the end also but the expression tree ignores that.
            Debug.Assert(block.Statements(0).Kind <> BoundKind.SequencePoint)
            Debug.Assert(block.Statements.Length = 1 OrElse
                         (block.Statements.Length = 2 AndAlso
                          block.Statements(1).Kind = BoundKind.ReturnStatement AndAlso
                          DirectCast(block.Statements(1), BoundReturnStatement).ExpressionOpt Is Nothing) OrElse
                         (block.Statements.Length = 3 AndAlso
                          block.Statements(1).Kind = BoundKind.LabelStatement AndAlso
                          block.Statements(2).Kind = BoundKind.ReturnStatement))

            ' The only local should be the Function Value. We'll ignore that.
            Debug.Assert(block.Locals.IsEmpty OrElse
                         (block.Locals.Length = 1 AndAlso block.Locals(0).IsFunctionValue))

            ' We only need to generate expression tree for the first statement.
            Dim stmt = block.Statements(0)

lSelect:
            Select Case stmt.Kind
                Case BoundKind.ReturnStatement
                    ' The Return statement is not directly expressed in the expression tree, just the expression being returned.
                    Dim expression As BoundExpression = (DirectCast(stmt, BoundReturnStatement)).ExpressionOpt
                    If expression IsNot Nothing Then
                        Return Visit(expression)
                    End If
                ' Otherwise fall through and generate an error

                Case BoundKind.ExpressionStatement
                    Return Visit((DirectCast(stmt, BoundExpressionStatement)).Expression)

                Case BoundKind.Block
                    Dim innerBlock = DirectCast(stmt, BoundBlock)
                    If innerBlock.Locals.IsEmpty AndAlso innerBlock.Statements.Length = 1 Then
                        stmt = innerBlock.Statements(0)
                        GoTo lSelect
                    End If

            End Select

            ' all the rest is not supported 
            Debug.Assert(False)
            Return GenerateDiagnosticAndReturnDummyExpression(ERRID.ERR_StatementLambdaInExpressionTree, block)
        End Function

        Private Function GenerateDiagnosticAndReturnDummyExpression(code As ERRID, node As BoundNode, ParamArray args As Object()) As BoundExpression
            Me.Diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(code, args), node.Syntax.GetLocation()))
            Return VisitInternal(Me._factory.Literal("Diagnostics Generated"))
        End Function

#Region "Visitor support"

        Private Function Visit(node As BoundExpression) As BoundExpression
            If node Is Nothing Then
                Return Nothing
            End If

            ' Set the syntax node for bound nodes we are generating.
            Dim old As SyntaxNode = _factory.Syntax
            _factory.Syntax = node.Syntax
            Dim result = VisitInternal(node)
            _factory.Syntax = old
            Return _factory.Convert(_expressionType, result)
        End Function

        Private Function VisitExpressionWithoutStackGuard(node As BoundExpression) As BoundExpression
            Select Case node.Kind
                Case BoundKind.ArrayCreation
                    Return VisitArrayCreation(DirectCast(node, BoundArrayCreation))
                Case BoundKind.ArrayAccess
                    Return VisitArrayAccess(DirectCast(node, BoundArrayAccess))
                Case BoundKind.ArrayLength
                    Return VisitArrayLength(DirectCast(node, BoundArrayLength))
                Case BoundKind.BadExpression
                    Return VisitBadExpression(DirectCast(node, BoundBadExpression))
                Case BoundKind.BinaryConditionalExpression
                    Return VisitBinaryConditionalExpression(DirectCast(node, BoundBinaryConditionalExpression))
                Case BoundKind.BinaryOperator
                    Return VisitBinaryOperator(DirectCast(node, BoundBinaryOperator))
                Case BoundKind.Call
                    Return VisitCall(DirectCast(node, BoundCall))
                Case BoundKind.Conversion
                    Return VisitConversion(DirectCast(node, BoundConversion))
                Case BoundKind.DelegateCreationExpression
                    Return VisitDelegateCreationExpression(DirectCast(node, BoundDelegateCreationExpression))
                Case BoundKind.DirectCast
                    Return VisitDirectCast(DirectCast(node, BoundDirectCast))
                Case BoundKind.FieldAccess
                    Dim fieldAccess = DirectCast(node, BoundFieldAccess)
                    If fieldAccess.FieldSymbol.IsCapturedFrame Then
                        Return CreateLiteralExpression(node)
                    End If
                    Return VisitFieldAccess(fieldAccess)
                Case BoundKind.Lambda
                    Return VisitLambda(DirectCast(node, BoundLambda))
                Case BoundKind.NewT
                    Return VisitNewT(DirectCast(node, BoundNewT))
                Case BoundKind.NullableIsTrueOperator
                    Return VisitNullableIsTrueOperator(DirectCast(node, BoundNullableIsTrueOperator))
                Case BoundKind.ObjectCreationExpression
                    Return VisitObjectCreationExpression(DirectCast(node, BoundObjectCreationExpression))
                Case BoundKind.Parameter
                    Return VisitParameter(DirectCast(node, BoundParameter))
                Case BoundKind.PropertyAccess
                    Return VisitPropertyAccess(DirectCast(node, BoundPropertyAccess))
                Case BoundKind.Sequence
                    Return VisitSequence(DirectCast(node, BoundSequence))
                Case BoundKind.TernaryConditionalExpression
                    Return VisitTernaryConditionalExpression(DirectCast(node, BoundTernaryConditionalExpression))
                Case BoundKind.TryCast
                    Return VisitTryCast(DirectCast(node, BoundTryCast))
                Case BoundKind.TypeOf
                    Return VisitTypeOf(DirectCast(node, BoundTypeOf))
                Case BoundKind.UnaryOperator
                    Return VisitUnaryOperator(DirectCast(node, BoundUnaryOperator))
                Case BoundKind.UserDefinedBinaryOperator
                    Return VisitUserDefinedBinaryOperator(DirectCast(node, BoundUserDefinedBinaryOperator))
                Case BoundKind.UserDefinedShortCircuitingOperator
                    Return VisitUserDefinedShortCircuitingOperator(DirectCast(node, BoundUserDefinedShortCircuitingOperator))
                Case BoundKind.UserDefinedUnaryOperator
                    Return VisitUserDefinedUnaryOperator(DirectCast(node, BoundUserDefinedUnaryOperator))

                Case BoundKind.Literal,
                     BoundKind.Local,
                     BoundKind.GetType,
                     BoundKind.MethodInfo,
                     BoundKind.FieldInfo,
                     BoundKind.MeReference,
                     BoundKind.MyClassReference

#If DEBUG Then
                    If node.Kind = BoundKind.GetType Then
                        Dim gt = DirectCast(node, BoundGetType)
                        node = gt.Update(gt.SourceType.MemberwiseClone(Of BoundTypeExpression)(), gt.GetTypeFromHandle, gt.Type)
                    Else
                        node = node.MemberwiseClone(Of BoundExpression)()
                    End If
#End If

                    Return CreateLiteralExpression(node)

                Case BoundKind.MyBaseReference
                    ' NOTE: All MyBase references should be processed when correspondent Call or 
                    '       DelegateCreation is being processed because only these nodes know 
                    '       method symbol, thus, can use proper type symbol for MyBase
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)
            End Select
        End Function

        Private Function VisitInternal(node As BoundExpression) As BoundExpression
            Dim result As BoundExpression
            _recursionDepth += 1

#If DEBUG Then
            Dim saveRecursionDepth = _recursionDepth
#End If

            If _recursionDepth > 1 Then
                StackGuard.EnsureSufficientExecutionStack(_recursionDepth)
                result = VisitExpressionWithoutStackGuard(node)
            Else
                result = VisitExpressionWithStackGuard(node)
            End If

#If DEBUG Then
            Debug.Assert(saveRecursionDepth = _recursionDepth)
#End If
            _recursionDepth -= 1
            Return result
        End Function

        Private Function VisitExpressionWithStackGuard(node As BoundExpression) As BoundExpression
            Try
                Return VisitExpressionWithoutStackGuard(node)
            Catch ex As InsufficientExecutionStackException
                Throw New BoundTreeVisitor.CancelledByStackGuardException(ex, node)
            End Try
        End Function

        Private Function VisitLambdaInternal(node As BoundLambda, delegateType As NamedTypeSymbol) As BoundExpression
            Dim parameterExpressionType = _factory.WellKnownType(WellKnownType.System_Linq_Expressions_ParameterExpression)

            ' prepare parameters so that they can be seen later
            Dim locals = ArrayBuilder(Of LocalSymbol).GetInstance()
            Dim initializers = ArrayBuilder(Of BoundExpression).GetInstance()
            Dim parameters = ArrayBuilder(Of BoundExpression).GetInstance()

            For Each p In node.LambdaSymbol.Parameters
                Debug.Assert(Not p.IsByRef, "DiagnosticsPass should have reported an error")

                Dim param = _factory.SynthesizedLocal(parameterExpressionType)
                Dim parameterReference = _factory.Local(param, False)
                Dim parameterReferenceLValue = _factory.Local(param, True)

                locals.Add(param)
                parameters.Add(parameterReference)
                _parameterMap(p) = parameterReference

                Dim parameter As BoundExpression = ConvertRuntimeHelperToExpressionTree("Parameter",
                                                                                        _factory.[Typeof](
                                                                                            p.Type.InternalSubstituteTypeParameters(_typeMap).Type,
                                                                                            _factory.WellKnownType(WellKnownType.System_Type)),
                                                                                        _factory.Literal(p.Name))
                If Not parameter.HasErrors Then
                    initializers.Add(_factory.AssignmentExpression(parameterReferenceLValue, parameter))
                End If
            Next

            Debug.Assert(Not node.LambdaSymbol.IsAsync AndAlso Not node.LambdaSymbol.IsIterator,
                         "An error should have been reported by DiagnosticsPass")
            Debug.Assert(node.WasCompilerGenerated OrElse node.IsSingleLine,
                         "An error should have been reported by DiagnosticsPass")

            Dim translatedBody As BoundExpression = TranslateLambdaBody(node.Body)
            Dim result = _factory.Sequence(locals.ToImmutableAndFree(),
                                           initializers.ToImmutableAndFree(),
                                           ConvertRuntimeHelperToExpressionTree(
                                               "Lambda",
                                               ImmutableArray.Create(Of TypeSymbol)(delegateType),
                                               translatedBody,
                                               _factory.Array(parameterExpressionType, parameters.ToImmutableAndFree())))

            For Each p In node.LambdaSymbol.Parameters
                _parameterMap.Remove(p)
            Next

            Return result
        End Function

#End Region

#Region "Visitors"

        Private Function VisitCall(node As BoundCall) As BoundExpression
            Dim method As MethodSymbol = node.Method

            Dim receiverOpt As BoundExpression = node.ReceiverOpt
            If receiverOpt IsNot Nothing Then
                If receiverOpt.Kind = BoundKind.MyBaseReference Then
#If DEBUG Then
                    receiverOpt = receiverOpt.MemberwiseClone(Of BoundExpression)()
#End If
                    receiverOpt = CreateLiteralExpression(receiverOpt, method.ContainingType)
                Else
                    receiverOpt = Visit(receiverOpt)
                End If
            End If

            If method.MethodKind = MethodKind.DelegateInvoke Then
                Return ConvertRuntimeHelperToExpressionTree("Invoke",
                                                            receiverOpt,
                                                            ConvertArgumentsIntoArray(node.Arguments))
            Else
                Return ConvertRuntimeHelperToExpressionTree("Call",
                                                            If(method.IsShared, _factory.Null(_expressionType), receiverOpt),
                                                            _factory.MethodInfo(method, _factory.WellKnownType(WellKnownType.System_Reflection_MethodInfo)), ConvertArgumentsIntoArray(node.Arguments))
            End If
        End Function

        Private Function VisitFieldAccess(node As BoundFieldAccess) As BoundExpression
            Dim origReceiverOpt As BoundExpression = node.ReceiverOpt
            Dim field As FieldSymbol = node.FieldSymbol
            Dim fieldIsShared As Boolean = field.IsShared
            Debug.Assert(origReceiverOpt IsNot Nothing OrElse fieldIsShared)

            Dim rewrittenReceiver As BoundExpression = Nothing
            If fieldIsShared Then
                rewrittenReceiver = _factory.Null()
            Else
                Debug.Assert(origReceiverOpt IsNot Nothing)
                If origReceiverOpt.Kind = BoundKind.MyBaseReference Then
#If DEBUG Then
                    origReceiverOpt = origReceiverOpt.MemberwiseClone(Of BoundExpression)()
#End If
                    rewrittenReceiver = CreateLiteralExpression(origReceiverOpt.MakeRValue, field.ContainingType)
                Else
                    rewrittenReceiver = Visit(origReceiverOpt)
                End If
            End If

            Return ConvertRuntimeHelperToExpressionTree("Field", rewrittenReceiver, _factory.FieldInfo(field))
        End Function

        Private Function VisitPropertyAccess(node As BoundPropertyAccess) As BoundExpression
            Dim origReceiverOpt As BoundExpression = node.ReceiverOpt
            Dim [property] As PropertySymbol = node.PropertySymbol
            Dim propertyIsShared As Boolean = [property].IsShared
            Debug.Assert(origReceiverOpt IsNot Nothing OrElse propertyIsShared)

            Dim rewrittenReceiver As BoundExpression = Nothing
            If propertyIsShared Then
                rewrittenReceiver = _factory.Null()
            Else
                Debug.Assert(origReceiverOpt IsNot Nothing)
                Debug.Assert(origReceiverOpt.Kind <> BoundKind.MyBaseReference AndAlso origReceiverOpt.Kind <> BoundKind.MyClassReference)
                rewrittenReceiver = Visit(origReceiverOpt)
            End If

            Dim getMethod As MethodSymbol = [property].GetMostDerivedGetMethod()
            Return ConvertRuntimeHelperToExpressionTree("Property", rewrittenReceiver, _factory.MethodInfo(getMethod, _factory.WellKnownType(WellKnownType.System_Reflection_MethodInfo)))
        End Function

        Private Function VisitLambda(node As BoundLambda) As BoundExpression
            Throw ExceptionUtilities.Unreachable
        End Function

        Private Function VisitDelegateCreationExpression(node As BoundDelegateCreationExpression) As BoundExpression
            Debug.Assert(node.RelaxationLambdaOpt Is Nothing)
            Debug.Assert(node.RelaxationReceiverPlaceholderOpt Is Nothing)
            Debug.Assert(node.MethodGroupOpt Is Nothing)

            Dim delegateType As NamedTypeSymbol = DirectCast(node.Type, NamedTypeSymbol)
            Debug.Assert(delegateType.TypeKind = TYPEKIND.Delegate)

            Dim method As MethodSymbol = node.Method
            Dim receiverOpt As BoundExpression = node.ReceiverOpt
            Dim isStaticMethodCall As Boolean = node.Method.IsShared

            If isStaticMethodCall Then
                receiverOpt = Me._factory.Convert(Me.ObjectType, Me._factory.Null())

            Else
                If receiverOpt.Kind = BoundKind.MyBaseReference Then
                    ' If the receiver is MyBase we can safely rewrite it into correspondent 'Me'
                    ' reference because the method was supposed to be transformed into wrapper
                    receiverOpt = New BoundMeReference(receiverOpt.Syntax, method.ContainingType)

                ElseIf receiverOpt.IsLValue Then
                    receiverOpt = receiverOpt.MakeRValue()
                End If

                If Not receiverOpt.Type.IsObjectType Then
                    receiverOpt = Me._factory.Convert(Me.ObjectType, receiverOpt)
                End If
            End If

            Dim result As BoundExpression

            Dim targetMethod As MethodSymbol = If(method.CallsiteReducedFromMethod, method)

            Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol) = Nothing
            Dim createDelegate = DirectCast(Binder.GetWellKnownTypeMember(Me._factory.Compilation, WellKnownMember.System_Reflection_MethodInfo__CreateDelegate, useSiteInfo), MethodSymbol)

            If createDelegate IsNot Nothing And useSiteInfo.DiagnosticInfo Is Nothing Then

                Diagnostics.AddDependencies(useSiteInfo)

                Dim methodInfo As BoundExpression = Me._factory.MethodInfo(targetMethod, createDelegate.ContainingType)

                ' beginning in 4.5, we do it this way
                result = Me._factory.Call(methodInfo,
                                          createDelegate,
                                          Me._factory.[Typeof](delegateType, createDelegate.Parameters(0).Type),
                                          receiverOpt)

            Else
                ' 4.0 and earlier we do it this way
                createDelegate = DirectCast(Me._factory.SpecialMember(SpecialMember.System_Delegate__CreateDelegate4), MethodSymbol)

                If createDelegate IsNot Nothing Then
                    result = Me._factory.Call(Me._factory.Null(Me.ObjectType),
                                              createDelegate,
                                              Me._factory.[Typeof](delegateType, createDelegate.Parameters(0).Type),
                                              receiverOpt,
                                              Me._factory.MethodInfo(targetMethod, createDelegate.Parameters(2).Type),
                                              Me._factory.Literal(False))

                Else
                    Return node ' Error should have been generated by now
                End If
            End If
            Return Convert(Visit(result), delegateType, False)
        End Function

        Private Function VisitParameter(node As BoundParameter) As BoundExpression
            Return _parameterMap(node.ParameterSymbol)
        End Function

        Private Function VisitArrayAccess(node As BoundArrayAccess) As BoundExpression
            Dim array = Visit(node.Expression)
            If node.Indices.Length = 1 Then
                Dim arg = node.Indices(0)
                Dim index = Visit(arg)
                Debug.Assert(arg.Type Is Me.Int32Type)
                Return ConvertRuntimeHelperToExpressionTree("ArrayIndex", array, index)
            Else
                Return ConvertRuntimeHelperToExpressionTree("ArrayIndex", array, BuildIndices(node.Indices))
            End If
        End Function

        Private Function BuildIndices(expressions As ImmutableArray(Of BoundExpression)) As BoundExpression
            Dim count As Integer = expressions.Length
            Dim newExpr(count - 1) As BoundExpression
            For i = 0 To count - 1
                Debug.Assert(expressions(i).Type Is Me.Int32Type)
                newExpr(i) = Visit(expressions(i))
            Next
            Return _factory.Array(_expressionType, newExpr.AsImmutableOrNull())
        End Function

        Private Function VisitBadExpression(node As BoundBadExpression) As BoundExpression
            Return node
        End Function

        Private Function VisitObjectCreationExpression(node As BoundObjectCreationExpression) As BoundExpression
            Dim visitedObjectCreation As BoundExpression = VisitObjectCreationExpressionInternal(node)
            Return VisitObjectCreationContinued(visitedObjectCreation, node.InitializerOpt)
        End Function

        Private Function VisitNewT(node As BoundNewT) As BoundExpression
            Return VisitObjectCreationContinued(ConvertRuntimeHelperToExpressionTree("New", _factory.[Typeof](node.Type, _factory.WellKnownType(WellKnownType.System_Type))), node.InitializerOpt)
        End Function

        Private Function VisitObjectCreationContinued(creation As BoundExpression, initializerOpt As BoundExpression) As BoundExpression
            If initializerOpt Is Nothing Then
                Return creation
            End If

            Select Case initializerOpt.Kind
                Case BoundKind.ObjectInitializerExpression
                    Return ConvertRuntimeHelperToExpressionTree("MemberInit", creation,
                                VisitObjectInitializer(DirectCast(initializerOpt, BoundObjectInitializerExpression)))

                Case BoundKind.CollectionInitializerExpression
                    Return ConvertRuntimeHelperToExpressionTree("ListInit", creation,
                                VisitCollectionInitializer(DirectCast(initializerOpt, BoundCollectionInitializerExpression)))

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(initializerOpt.Kind)
            End Select
        End Function

        Private Function VisitObjectInitializer(initializer As BoundObjectInitializerExpression) As BoundExpression
            Debug.Assert(initializer.CreateTemporaryLocalForInitialization,
                         "DiagnosticsPass should have generated an error")

            Dim initializers As ImmutableArray(Of BoundExpression) = initializer.Initializers
            Dim initializerCount As Integer = initializers.Length
            Dim newInitializers(initializerCount - 1) As BoundExpression

            For i = 0 To initializerCount - 1
                Debug.Assert(initializers(i).Kind = BoundKind.AssignmentOperator)
                Dim assignment = DirectCast(initializers(i), BoundAssignmentOperator)

                Debug.Assert(assignment.LeftOnTheRightOpt Is Nothing)
                Dim left As BoundExpression = assignment.Left

                Dim leftSymbol As Symbol = Nothing
                Select Case left.Kind
                    Case BoundKind.FieldAccess
                        leftSymbol = DirectCast(assignment.Left, BoundFieldAccess).FieldSymbol

                    Case BoundKind.PropertyAccess
                        Debug.Assert(DirectCast(assignment.Left, BoundPropertyAccess).AccessKind = PropertyAccessKind.Set)
                        leftSymbol = DirectCast(assignment.Left, BoundPropertyAccess).PropertySymbol

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(left.Kind)
                End Select
                Debug.Assert(leftSymbol IsNot Nothing)

                Dim right As BoundExpression = assignment.Right

                Debug.Assert(initializer.PlaceholderOpt IsNot Nothing)
                Debug.Assert(Not BoundNodeFinder.ContainsNode(right, initializer.PlaceholderOpt, _recursionDepth, convertInsufficientExecutionStackExceptionToCancelledByStackGuardException:=True), "Should be addressed in DiagnosticsPass")
                Debug.Assert(leftSymbol.Kind = SymbolKind.Field OrElse leftSymbol.Kind = SymbolKind.Property)

                Dim memberRef As BoundExpression = If(leftSymbol.Kind = SymbolKind.Field,
                                                      Me._factory.FieldInfo(DirectCast(leftSymbol, FieldSymbol)),
                                                      Me._factory.MethodInfo((DirectCast(leftSymbol, PropertySymbol)).SetMethod, _factory.WellKnownType(WellKnownType.System_Reflection_MethodInfo)))

                newInitializers(i) = _factory.Convert(MemberBindingType, ConvertRuntimeHelperToExpressionTree("Bind", memberRef, Visit(right)))
            Next

            Return _factory.Array(MemberBindingType, newInitializers.AsImmutableOrNull())
        End Function

        Private Function VisitCollectionInitializer(initializer As BoundCollectionInitializerExpression) As BoundExpression
            Dim initializers As ImmutableArray(Of BoundExpression) = initializer.Initializers
            Dim initializerCount As Integer = initializers.Length
            Dim newInitializers(initializerCount - 1) As BoundExpression

            For i = 0 To initializerCount - 1
                Debug.Assert(initializers(i).Kind = BoundKind.Call)
                Dim [call] = DirectCast(initializers(i), BoundCall)

                ' Note, for extension methods we are dropping the "Me" parameter to remove
                ' BoundCollectionInitializerExpression.PlaceholderOpt references from the tree.
                ' Otherwise, IL generation fails because it doesn't know what to do with it.
                ' At run-time, this code is going to throw because ElementInit API doesn't accept
                ' shared methods. We don't fail compilation in this scenario due to backward
                ' compatibility reasons.
                newInitializers(i) = _factory.Convert(
                                            ElementInitType,
                                            ConvertRuntimeHelperToExpressionTree(
                                                    "ElementInit",
                                                    _factory.MethodInfo([call].Method, _factory.WellKnownType(WellKnownType.System_Reflection_MethodInfo)),
                                                    ConvertArgumentsIntoArray(If([call].Method.IsShared AndAlso [call].Method.IsExtensionMethod,
                                                                                 [call].Arguments.RemoveAt(0),
                                                                                 [call].Arguments))))
            Next

            Return _factory.Array(ElementInitType, newInitializers.AsImmutableOrNull())
        End Function

        Private Function VisitObjectCreationExpressionInternal(node As BoundObjectCreationExpression) As BoundExpression
            If node.ConstantValueOpt IsNot Nothing Then
                Return CreateLiteralExpression(node)
            End If

            If node.ConstructorOpt Is Nothing OrElse
                (node.Arguments.Length = 0 AndAlso Not node.Type.IsStructureType() OrElse
                node.ConstructorOpt.IsDefaultValueTypeConstructor()) Then

                Return ConvertRuntimeHelperToExpressionTree("New", _factory.[Typeof](node.Type, _factory.WellKnownType(WellKnownType.System_Type)))
            End If

            Dim ctor = _factory.ConstructorInfo(node.ConstructorOpt)
            Dim args = ConvertArgumentsIntoArray(node.Arguments)

            If node.Type.IsAnonymousType AndAlso node.Arguments.Length <> 0 Then
                Dim anonType = DirectCast(node.Type, AnonymousTypeManager.AnonymousTypePublicSymbol)
                Dim properties As ImmutableArray(Of AnonymousTypeManager.AnonymousTypePropertyPublicSymbol) = anonType.Properties
                Debug.Assert(properties.Length = node.Arguments.Length)

                Dim methodInfos(properties.Length - 1) As BoundExpression
                For i = 0 To properties.Length - 1
                    methodInfos(i) = Me._factory.Convert(Me.MemberInfoType, Me._factory.MethodInfo(properties(i).GetMethod, _factory.WellKnownType(WellKnownType.System_Reflection_MethodInfo)))
                Next

                Return ConvertRuntimeHelperToExpressionTree("New", ctor, args, Me._factory.Array(Me.MemberInfoType, methodInfos.AsImmutableOrNull()))
            Else
                Return ConvertRuntimeHelperToExpressionTree("New", ctor, args)
            End If
        End Function

        Private Function VisitSequence(node As BoundSequence) As BoundExpression
            Dim locals As ImmutableArray(Of LocalSymbol) = node.Locals
            Dim sideEffects As ImmutableArray(Of BoundExpression) = node.SideEffects
            Dim value As BoundExpression = node.ValueOpt

            If locals.IsEmpty AndAlso sideEffects.IsEmpty AndAlso value IsNot Nothing Then
                Return VisitInternal(value)
            End If

            ' All other cases are not supported, note that some cases of invalid
            ' sequences are handled in DiagnosticsPass, but we still want to catch
            ' here those sequences created in lowering
            Return GenerateDiagnosticAndReturnDummyExpression(ERRID.ERR_ExpressionTreeNotSupported, node)
        End Function

        Private Function VisitArrayLength(node As BoundArrayLength) As BoundExpression
            Dim resultType As TypeSymbol = node.Type
            If resultType.SpecialType = SpecialType.System_Int64 Then
                Return VisitCall(
                            New BoundCall(
                                node.Syntax,
                                DirectCast(Me._factory.SpecialMember(SpecialMember.System_Array__LongLength), PropertySymbol).GetMethod,
                                Nothing,
                                node.Expression,
                                ImmutableArray(Of BoundExpression).Empty,
                                Nothing,
                                isLValue:=False,
                                suppressObjectClone:=True,
                                type:=resultType))
            Else
                Return ConvertRuntimeHelperToExpressionTree("ArrayLength", Visit(node.Expression))
            End If
        End Function

        Private Function VisitArrayCreation(node As BoundArrayCreation) As BoundExpression
            Dim arrayType = DirectCast(node.Type, ArrayTypeSymbol)
            Dim boundType As BoundExpression = _factory.[Typeof](arrayType.ElementType, _factory.WellKnownType(WellKnownType.System_Type))
            Dim initializer As BoundArrayInitialization = node.InitializerOpt
            If initializer IsNot Nothing AndAlso Not initializer.Initializers.IsEmpty Then
                Debug.Assert(arrayType.IsSZArray, "Not SZArray should be addressed in DiagnosticsPass")
                Return ConvertRuntimeHelperToExpressionTree("NewArrayInit", boundType, ConvertArgumentsIntoArray(node.InitializerOpt.Initializers))
            Else
                Return ConvertRuntimeHelperToExpressionTree("NewArrayBounds", boundType, ConvertArgumentsIntoArray(node.Bounds))
            End If
        End Function

        Private Function ConvertArgumentsIntoArray(exprs As ImmutableArray(Of BoundExpression)) As BoundExpression
            Dim newArgs(exprs.Length - 1) As BoundExpression
            For i = 0 To exprs.Length - 1
                newArgs(i) = Visit(exprs(i))
            Next
            Return _factory.Array(_expressionType, newArgs.AsImmutableOrNull)
        End Function

        Private Function VisitTypeOf(node As BoundTypeOf) As BoundExpression
            Return ConvertRuntimeHelperToExpressionTree("TypeIs", Visit(node.Operand), _factory.[Typeof](node.TargetType, _factory.WellKnownType(WellKnownType.System_Type)))
        End Function

#End Region

#Region "General utility/factory methods"

        ' Emit a call node
        Private Function [Call](receiver As BoundExpression, method As MethodSymbol, ParamArray params As BoundExpression()) As BoundExpression
            Dim factoryArgs(0 To params.Length + 1) As BoundExpression
            factoryArgs(0) = receiver
            factoryArgs(1) = _factory.MethodInfo(method, _factory.WellKnownType(WellKnownType.System_Reflection_MethodInfo))
            Array.Copy(params, 0, factoryArgs, 2, params.Length)
            Return ConvertRuntimeHelperToExpressionTree("Call", factoryArgs)
        End Function

        ' Emit a Default node for a specific type
        Private Function [Default](type As TypeSymbol) As BoundExpression
            Return ConvertRuntimeHelperToExpressionTree("Default", _factory.[Typeof](type, _factory.WellKnownType(WellKnownType.System_Type)))
        End Function

        ' Emit a New node to a specific type with a helper constructor and one argument
        Private Function [New](helper As SpecialMember, argument As BoundExpression) As BoundExpression
            Return ConvertRuntimeHelperToExpressionTree("New", _factory.ConstructorInfo(helper), argument)
        End Function

        ' Emit a negate node
        Private Function Negate(expr As BoundExpression) As BoundExpression
            Return ConvertRuntimeHelperToExpressionTree("Negate", expr)
        End Function

        Private Function InitWithParameterlessValueTypeConstructor(type As TypeSymbol) As BoundExpression
            ' The "New" overload without a methodInfo automatically generates the parameterless constructor for us.
            Debug.Assert(type.IsValueType)
            Return ConvertRuntimeHelperToExpressionTree("New", _factory.[Typeof](type, _factory.WellKnownType(WellKnownType.System_Type)))
        End Function

        Private Function IsIntegralType(type As TypeSymbol) As Boolean
            Return GetUnderlyingType(type).IsIntegralType
        End Function

        Private Function GetUnderlyingType(type As TypeSymbol) As TypeSymbol
            Return type.GetNullableUnderlyingTypeOrSelf.GetEnumUnderlyingTypeOrSelf
        End Function

        Private Function CreateLiteralExpression(node As BoundExpression) As BoundExpression
            Return CreateLiteralExpression(node, node.Type)
        End Function

        Private Function CreateLiteralExpression(node As BoundExpression, type As TypeSymbol) As BoundExpression
            Return ConvertRuntimeHelperToExpressionTree("Constant", _factory.Convert(Me.ObjectType, node), _factory.[Typeof](type, _factory.WellKnownType(WellKnownType.System_Type)))
        End Function

        ''' <summary>
        ''' Create an Expression Tree Node with the given name and arguments
        ''' </summary>
        Private Function ConvertRuntimeHelperToExpressionTree(helperMethodName As String,
                                                              ParamArray arguments As BoundExpression()) As BoundExpression

            Return ConvertRuntimeHelperToExpressionTree(helperMethodName, ImmutableArray(Of TypeSymbol).Empty, arguments)
        End Function

        ''' <summary>
        ''' Create an Expression node with the given name, type arguments, and arguments.
        ''' </summary>
        Private Function ConvertRuntimeHelperToExpressionTree(helperMethodName As String,
                                                              typeArgs As ImmutableArray(Of TypeSymbol),
                                                              ParamArray arguments As BoundExpression()) As BoundExpression
            Dim anyArgumentsWithErrors = False

            ' Check if we have any bad arguments.
            For Each a In arguments
                If a.HasErrors Then
                    anyArgumentsWithErrors = True
                End If
            Next

            ' Get the method group
            Dim methodGroup = GetExprFactoryMethodGroup(helperMethodName, typeArgs)

            If methodGroup Is Nothing OrElse anyArgumentsWithErrors Then
                Return _factory.BadExpression(arguments)
            End If

            ' Do overload resolution and bind an invocation of the method.
            Dim result = _binder.BindInvocationExpression(_factory.Syntax,
                                                          _factory.Syntax,
                                                          TypeCharacter.None,
                                                          methodGroup,
                                                          ImmutableArray.Create(Of BoundExpression)(arguments),
                                                          argumentNames:=Nothing,
                                                          diagnostics:=Diagnostics,
                                                          callerInfoOpt:=Nothing)

            Return result
        End Function

        ''' <summary>
        ''' Gets the method group for a given method name. Returns Nothing if no methods found.
        ''' </summary>
        Private Function GetExprFactoryMethodGroup(methodName As String, typeArgs As ImmutableArray(Of TypeSymbol)) As BoundMethodGroup
            Dim group As BoundMethodGroup = Nothing
            Dim result = LookupResult.GetInstance()

            Dim useSiteInfo = _binder.GetNewCompoundUseSiteInfo(Me.Diagnostics)
            _binder.LookupMember(result,
                                 Me._expressionType,
                                 methodName,
                                 arity:=0,
                                 options:=LookupOptions.AllMethodsOfAnyArity Or LookupOptions.IgnoreExtensionMethods,
                                 useSiteInfo:=useSiteInfo)
            Me.Diagnostics.Add(Me._factory.Syntax, useSiteInfo)

            If result.IsGood Then
                Debug.Assert(result.Symbols.Count > 0)
                Dim symbol0 = result.Symbols(0)
                If result.Symbols(0).Kind = SymbolKind.Method Then
                    group = New BoundMethodGroup(Me._factory.Syntax,
                                                 Me._factory.TypeArguments(typeArgs),
                                                 result.Symbols.ToDowncastedImmutable(Of MethodSymbol),
                                                 result.Kind,
                                                 Nothing,
                                                 QualificationKind.QualifiedViaTypeName)
                End If
            End If

            If group Is Nothing Then
                Diagnostics.Add(If(result.HasDiagnostic,
                                   result.Diagnostic,
                                   ErrorFactory.ErrorInfo(ERRID.ERR_NameNotMember2, methodName, Me._expressionType)),
                                Me._factory.Syntax.GetLocation())
            End If

            result.Free()
            Return group
        End Function

#End Region

    End Class
End Namespace
