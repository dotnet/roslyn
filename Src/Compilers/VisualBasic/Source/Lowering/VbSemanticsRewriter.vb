' ==++==
'
' Copyright (c) Microsoft Corporation. All rights reserved.
'
' ==--==

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Roslyn.Compilers.Common


Namespace Roslyn.Compilers.VisualBasic

    ''' <summary>
    ''' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    ''' !!! Within this particular rewriter there is a contract:               !!!
    ''' !!! newly produced nodes (this doesn't include nodes updated in place) !!!
    ''' !!! should be in their final form, i.e. shouldn't require any other    !!!
    ''' !!! rewrites handled by this rewriter.                                 !!!
    ''' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    ''' 
    ''' Rewrites Decimal/Date constant expressions into 
    ''' field references/constructor invocations.
    ''' 
    ''' Rewrites VB intrinsic conversion into a helper call,
    ''' if one is required. 
    ''' 
    ''' Injects GetObjectValue calls for assignments and for arguments 
    ''' passed to method calls. See VisitAndGenerateObjectClone for more information. 
    ''' TODO: Note, that Dev10 compiler injects GetObjectValue during IL generation, which means that
    ''' LINQ expression trees do not contain those calls.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class VBSemanticsRewriter
        Inherits BoundTreeRewriter

        Private ReadOnly m_Method As MethodSymbol

        Private ReadOnly m_Diagnostics As DiagnosticBag

        Private Sub New(method As MethodSymbol, diagnostics As DiagnosticBag)
            m_Method = method
            m_Diagnostics = diagnostics
        End Sub

        Private ReadOnly Property Compilation As Compilation
            Get
                Return DirectCast(m_Method.ContainingAssembly, SourceAssemblySymbol).Compilation
            End Get
        End Property

        Private ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return m_Method.ContainingAssembly
            End Get
        End Property

        Private Function GetLocation(node As BoundNode) As Location
            Return node.Syntax.GetLocation()
        End Function

        ''' <summary>
        ''' Checks for well known member and reports diagnostics if the member is Nothing or has UseSiteError.
        ''' Returns True in case diagnostics was actually reported
        ''' </summary>
        Private Function ReportMissingOrBadRuntimeHelper(node As BoundNode, wellKnownMember As WellKnownMember, memberSymbol As Symbol) As Boolean
            If memberSymbol Is Nothing Then
                ReportMissingRuntimeHelper(node, wellKnownMember)
                Return True
            Else
                Dim useSiteError = If(memberSymbol.GetUseSiteErrorInfo(), memberSymbol.ContainingType.GetUseSiteErrorInfo())
                If useSiteError IsNot Nothing Then
                    ReportRuntimeHelperError(node, useSiteError)
                    Return True
                End If
            End If
            Return False
        End Function

        Private Sub ReportMissingRuntimeHelper(node As BoundNode, wellKnownMember As WellKnownMember)
            Dim descriptor = WellKnownMembers.GetDescriptor(wellKnownMember)

            ' TODO: If the type is generic, we might want to use VB style name rather than emitted name.
            Dim typeName As String = WellKnownTypes.GetMetadataName(CType(descriptor.DeclaringTypeId, WellKnownType))
            Dim memberName As String = descriptor.Name

            ReportMissingRuntimeHelper(node, typeName, memberName)
        End Sub

        ''' <summary>
        ''' Checks for special member and reports diagnostics if the member is Nothing or has UseSiteError.
        ''' Returns True in case diagnostics was actually reported
        ''' </summary>
        Private Function ReportMissingOrBadRuntimeHelper(node As BoundNode, specialMember As SpecialMember, memberSymbol As Symbol) As Boolean
            If memberSymbol Is Nothing Then
                ReportMissingRuntimeHelper(node, specialMember)
                Return True
            Else
                Dim useSiteError = If(memberSymbol.GetUseSiteErrorInfo(), memberSymbol.ContainingType.GetUseSiteErrorInfo())
                If useSiteError IsNot Nothing Then
                    ReportRuntimeHelperError(node, useSiteError)
                    Return True
                End If
            End If
            Return False
        End Function

        Private Sub ReportMissingRuntimeHelper(node As BoundNode, specialMember As SpecialMember)
            Dim descriptor = SpecialMembers.GetDescriptor(specialMember)

            ' TODO: If the type is generic, we might want to use VB style name rather than emitted name.
            Dim typeName As String = SpecialTypes.GetMetadataName(CType(descriptor.DeclaringTypeId, SpecialType))
            Dim memberName As String = descriptor.Name

            ReportMissingRuntimeHelper(node, typeName, memberName)
        End Sub

        Private Sub ReportMissingRuntimeHelper(node As BoundNode, typeName As String, memberName As String)

            If memberName.Equals(CommonMemberNames.InstanceConstructorName) OrElse
               memberName.Equals(CommonMemberNames.StaticConstructorName) Then
                memberName = "New"
            End If

            ReportRuntimeHelperError(node, ErrorFactory.ErrorInfo(ERRID.ERR_MissingRuntimeHelper, typeName & "." & memberName))
        End Sub

        Private Sub ReportRuntimeHelperError(node As BoundNode, diagnostic As DiagnosticInfo)
            Binder.ReportDiagnostic(m_Diagnostics, New Diagnostic(diagnostic, GetLocation(node)))
        End Sub

        Private Sub ReportBadType(node As BoundNode, typeSymbol As TypeSymbol)
            Dim useSiteError = typeSymbol.GetUseSiteErrorInfo()
            If useSiteError IsNot Nothing Then
                m_Diagnostics.Add(useSiteError, GetLocation(node))
            End If
        End Sub

        Public Shared Function Rewrite(method As MethodSymbol, node As BoundNode, diagnostics As DiagnosticBag) As BoundNode
            Debug.Assert(node IsNot Nothing)
            Dim rewriter = New VBSemanticsRewriter(method, diagnostics)
            Return rewriter.Visit(node)
        End Function

        'this rewriter should not be applied to nodes with lambdas still present
        'lambdas need to be rewritten as methods or expression trees before this
        'rewrite is applicable
        Public Overrides Function VisitLambda(node As BoundLambda) As BoundNode
            Throw Contract.Unreachable
        End Function

        Public Overrides Function VisitCatchBlock(node As BoundCatchBlock) As BoundNode
            ' when starting/finishing any code associated with an exception handler (including exception filters)
            ' we need to call SetProjectError/ClearProjectError

            ' NOTE: we do not inject the helper calls via a rewrite. 
            ' SetProjectError is called with implicit argument on the stack and cannot be expressed in the tree.
            ' ClearProjectError could be added as a rewrite, but for similarity with SetProjectError we will do it in IL gen too.
            ' we will however check for the presence of the helpers and complain here if we cannot find them.

            ' TODO: when building VB runtime, this check is unnecessary as we should not emit the helpers.
            Const setProjectError As WellKnownMember = WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__SetProjectError
            Dim setProjectErrorMethod = DirectCast(Compilation.GetWellKnownTypeMember(setProjectError), MethodSymbol)
            ReportMissingOrBadRuntimeHelper(node, setProjectError, setProjectErrorMethod)

            If node.ExceptionFilterOpt Is Nothing OrElse node.ExceptionFilterOpt.Kind <> BoundKind.UnstructuredExceptionHandlingCatchFilter Then
                Const clearProjectError As WellKnownMember = WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__ClearProjectError
                Dim clearProjectErrorMethod = DirectCast(Compilation.GetWellKnownTypeMember(clearProjectError), MethodSymbol)
                ReportMissingOrBadRuntimeHelper(node, clearProjectError, clearProjectErrorMethod)
            End If

            Return MyBase.VisitCatchBlock(node)
        End Function

        Public Overrides Function VisitSelectStatement(node As BoundSelectStatement) As BoundNode
            ' If we are recommending switch table for string type, we will
            ' be using runtime helpers for string comparisons during emit.
            ' GetWellKnownTypeMember here to report diagnostics, if any.
            Dim selectCaseExpr = node.ExpressionStatement.Expression
            If node.RecommendSwitchTable AndAlso selectCaseExpr.Type.IsStringType() Then
                ' Prefer embedded version of the member if present
                Dim embeddedOperatorsType As NamedTypeSymbol = Compilation.GetWellKnownType(WellKnownType.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators)
                Dim compareStringMember As WellKnownMember =
                    If(embeddedOperatorsType.IsErrorType AndAlso TypeOf embeddedOperatorsType Is MissingMetadataTypeSymbol,
                       WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareStringStringStringBoolean,
                       WellKnownMember.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators__CompareStringStringStringBoolean)

                Dim compareStringMethod = DirectCast(Compilation.GetWellKnownTypeMember(compareStringMember), MethodSymbol)
                Me.ReportMissingOrBadRuntimeHelper(selectCaseExpr, compareStringMember, compareStringMethod)

                Const convertCharToUInt32Member As WellKnownMember = WellKnownMember.System_Convert__ToUInt32Char
                Dim convertCharToUInt32Method = DirectCast(Compilation.GetWellKnownTypeMember(convertCharToUInt32Member), MethodSymbol)
                Me.ReportMissingOrBadRuntimeHelper(selectCaseExpr, convertCharToUInt32Member, convertCharToUInt32Method)

                Const stringLengthMember As SpecialMember = SpecialMember.System_String__Length
                Dim stringLengthMethod = DirectCast(ContainingAssembly.GetSpecialTypeMember(stringLengthMember), MethodSymbol)
                Me.ReportMissingOrBadRuntimeHelper(selectCaseExpr, stringLengthMember, stringLengthMethod)

                Const stringCharsMember As SpecialMember = SpecialMember.System_String__Chars
                Dim stringCharsMethod = DirectCast(ContainingAssembly.GetSpecialTypeMember(stringCharsMember), MethodSymbol)
                Me.ReportMissingOrBadRuntimeHelper(selectCaseExpr, stringCharsMember, stringCharsMethod)

                Me.ReportBadType(selectCaseExpr, Compilation.GetSpecialType(SpecialType.System_Int32))
                Me.ReportBadType(selectCaseExpr, Compilation.GetSpecialType(SpecialType.System_UInt32))
                Me.ReportBadType(selectCaseExpr, Compilation.GetSpecialType(SpecialType.System_String))
            End If

            Return MyBase.VisitSelectStatement(node)
        End Function

        ''' <summary>
        ''' Make sure GetObjectValue calls are injected.
        ''' </summary>
        Public Overrides Function VisitAssignmentOperator(node As BoundAssignmentOperator) As BoundNode
            Debug.Assert(node.LeftOnTheRightOpt Is Nothing)

            ' if the lhs of this assignment operator is a field access, it should not 
            ' be rewritten even if it's const.
            ' if you do that, it will create ObjectCreationExpressions for Dates and Decimals which are not allowed
            ' there.
            Dim leftNode = node.Left
            Dim left As BoundExpression
            If leftNode.Kind = BoundKind.FieldAccess Then
                Dim leftFieldAccess = DirectCast(leftNode, BoundFieldAccess)
                If leftFieldAccess.IsConstant Then
                    left = DirectCast(MyBase.VisitFieldAccess(leftFieldAccess), BoundExpression)
                Else
                    left = DirectCast(Me.Visit(leftNode), BoundExpression)
                End If
            Else
                left = DirectCast(Me.Visit(leftNode), BoundExpression)
            End If


            Dim right As BoundExpression

            If node.SuppressObjectClone OrElse node.HasErrors OrElse node.Right.HasErrors() Then
                right = DirectCast(Me.Visit(node.Right), BoundExpression)
            Else
                right = VisitAndGenerateObjectClone(node.Right)
            End If

            Return node.Update(left, Nothing, right, True, node.Type)
        End Function

        ''' <summary>
        ''' Make sure GetObjectValue calls are injected.
        ''' </summary>
        Public Overrides Function VisitCall(node As BoundCall) As BoundNode
            If node.HasErrors Then
                Return MyBase.VisitCall(node)
            End If


            Dim receiverOpt As BoundExpression = DirectCast(Me.Visit(node.ReceiverOpt), BoundExpression)
            Dim arguments As ReadOnlyArray(Of BoundExpression) = VisitCallArguments(node.Method, node.Arguments, suppressObjectClone:=node.SuppressObjectClone)
            Dim method As MethodSymbol = node.Method

            ' Replace a call to AscW(<non constant char>) with a conversion, this makes sure we don't have a recursion inside AscW(Char).
            If method Is Compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_Strings__AscWCharInt32) Then
                Return New BoundConversion(node.Syntax, arguments(0), ConversionKind.WideningNumeric, checked:=False, explicitCastInCode:=True, type:=node.Type)
            End If

            ' Optimize the case where we create an instance of a delegate and invoke it right away.
            ' Skip the delegate creation and invoke the method directly. Specifically, we are targeting 
            ' lambda relaxation scenario that requires a stub, which invokes original lambda by instantiating
            ' an Anonymous Delegate and calling its Invoke method. That is why this optimization should be done
            ' after lambdas are rewritten.
            ' CONSIDER: Should we expand this optimization to all delegate types and all explicitly written code?
            '           If we decide to do this, we should be careful with extension methods because they have
            '           special treatment of 'this' parameter. 
            If method.MethodKind = MethodKind.DelegateInvoke AndAlso
               method.ContainingType.IsAnonymousType AndAlso
               receiverOpt.Kind = BoundKind.DelegateCreationExpression AndAlso
               Conversions.ClassifyMethodConversionForLambdaOrAnonymousDelegate(method,
                                DirectCast(receiverOpt, BoundDelegateCreationExpression).Method) = MethodConversionKind.Identity Then

                Dim delegateCreation = DirectCast(receiverOpt, BoundDelegateCreationExpression)
                Debug.Assert(delegateCreation.RelaxationLambdaOpt Is Nothing AndAlso delegateCreation.RelaxationReceiverTempOpt Is Nothing)

                If Not delegateCreation.Method.IsReducedExtensionMethod Then
                    method = delegateCreation.Method
                    receiverOpt = delegateCreation.ReceiverOpt
                End If
            End If

            Dim result = node.Update(method, Nothing, receiverOpt, arguments, Nothing, node.SuppressObjectClone, node.Type)

            Return result
        End Function

        ''' <summary>
        ''' Make sure GetObjectValue calls are injected.
        ''' </summary>
        Public Overrides Function VisitObjectCreationExpression(node As BoundObjectCreationExpression) As BoundNode
            If node.HasErrors Then
                Return MyBase.VisitObjectCreationExpression(node)
            End If

            Dim arguments As ReadOnlyArray(Of BoundExpression) = VisitCallArguments(node.ConstructorOpt, node.Arguments, suppressObjectClone:=False)
            Dim initializer = DirectCast(Visit(node.InitializerOpt), BoundObjectInitializerExpressionBase)

            Return node.Update(node.ConstructorOpt, arguments, initializer, node.Type)
        End Function

        Private Function VisitCallArguments(method As MethodSymbol, args As ReadOnlyArray(Of BoundExpression), suppressObjectClone As Boolean) As ReadOnlyArray(Of BoundExpression)
            If method Is Nothing Then
                '  inaccessible parameterless ValueType constructor, should be no arguments
                Debug.Assert(args.IsNullOrEmpty)
                Return args
            End If

            Dim newArgs As ArrayBuilder(Of BoundExpression) = Nothing
            Dim i As Integer = 0
            Dim n As Integer = If(args.IsNull, 0, args.Count)

            ' In case of GetObjectValue itself the call to GetObjectValue should always be suppressed.
            suppressObjectClone = suppressObjectClone OrElse
                                  method Is Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__GetObjectValueObject)

            While (i < n)
                Dim item As BoundExpression = args(i)
                Debug.Assert(item IsNot Nothing)

                Dim visited As BoundExpression

                ' Do not clone ByRef arguments, unless we are forcing ByVal semantics.
                If suppressObjectClone OrElse (method.Parameters(i).IsByRef AndAlso item.IsLValue) Then
                    visited = DirectCast(Me.Visit(item), BoundExpression)
                Else
                    visited = VisitAndGenerateObjectClone(item)
                End If

                If item IsNot visited AndAlso newArgs Is Nothing Then
                    newArgs = ArrayBuilder(Of BoundExpression).GetInstance
                    If i > 0 Then
                        newArgs.AddRange(args, i)
                    End If
                End If

                If newArgs IsNot Nothing AndAlso visited IsNot Nothing Then
                    newArgs.Add(visited)
                End If

                i += 1
            End While

            If newArgs IsNot Nothing Then
                Return newArgs.ToReadOnlyAndFree
            Else
                Return args
            End If
        End Function

        ''' <summary>
        ''' Visit expression node and apply GetObjectValue call if needed.
        ''' 
        ''' Why we are doing this rewrite during this phase?
        ''' It would be OK to do this rewrite during earlier phase, but not after this phase
        ''' because:
        '''   - This phase introduces various helper calls, which should not be affected by 
        '''     GetObjectValue injection.
        '''   - This is the last phase where we can see operators not yet replaced with helper calls, 
        '''     this simplifies logic that determines if GetObjectValue call can be omitted.
        ''' </summary>
        Private Function VisitAndGenerateObjectClone(value As BoundExpression) As BoundExpression
            Dim result As BoundExpression = DirectCast(Me.Visit(value), BoundExpression)

            ' TODO: shouldn't go into this if when we are compiling VB runtime (Microsoft.VisualBasic.Dll).
            If Not result.HasErrors AndAlso result.Type.IsObjectType() Then

                ' There are a series of object operations which we know don't require a call to GetObjectValue.
                ' These operations are math and logic operators.
                Dim nodeToCheck As BoundExpression = value

                Do
                    If nodeToCheck.IsConstant Then
                        Debug.Assert(nodeToCheck.ConstantValueOpt.IsNothing)
                        Return result
                    End If

                    Select Case nodeToCheck.Kind
                        Case BoundKind.BinaryOperator

                            Dim binaryOperator = DirectCast(nodeToCheck, BoundBinaryOperator)

                            If (binaryOperator.OperatorKind And BinaryOperatorKind.UserDefined) = 0 Then
                                Select Case (binaryOperator.OperatorKind And BinaryOperatorKind.OpMask)
                                    Case BinaryOperatorKind.Power,
                                         BinaryOperatorKind.Divide,
                                         BinaryOperatorKind.Modulo,
                                         BinaryOperatorKind.IntegerDivide,
                                         BinaryOperatorKind.Concatenate,
                                         BinaryOperatorKind.And,
                                         BinaryOperatorKind.AndAlso,
                                         BinaryOperatorKind.Or,
                                         BinaryOperatorKind.OrElse,
                                         BinaryOperatorKind.Xor,
                                         BinaryOperatorKind.Multiply,
                                         BinaryOperatorKind.Add,
                                         BinaryOperatorKind.Subtract,
                                         BinaryOperatorKind.LeftShift,
                                         BinaryOperatorKind.RightShift

                                        Return result
                                End Select
                            End If

                            Exit Do

                        Case BoundKind.UnaryOperator

                            Dim unaryOperator = DirectCast(nodeToCheck, BoundUnaryOperator)

                            If (unaryOperator.OperatorKind And UnaryOperatorKind.UserDefined) = 0 Then
                                Select Case (unaryOperator.OperatorKind And UnaryOperatorKind.IntrinsicOpMask)
                                    Case UnaryOperatorKind.Minus,
                                         UnaryOperatorKind.Plus,
                                         UnaryOperatorKind.Not

                                        Return result
                                End Select
                            End If

                            Exit Do

                        Case BoundKind.DirectCast,
                             BoundKind.TryCast,
                             BoundKind.Conversion

                            Dim conversionKind As ConversionKind

                            If nodeToCheck.Kind = BoundKind.DirectCast Then
                                Dim conversion = DirectCast(nodeToCheck, BoundDirectCast)
                                conversionKind = conversion.ConversionKind
                                nodeToCheck = conversion.Operand
                            ElseIf nodeToCheck.Kind = BoundKind.TryCast Then
                                Dim conversion = DirectCast(nodeToCheck, BoundTryCast)
                                conversionKind = conversion.ConversionKind
                                nodeToCheck = conversion.Operand
                            Else
                                Dim conversion = DirectCast(nodeToCheck, BoundConversion)
                                conversionKind = conversion.ConversionKind
                                nodeToCheck = conversion.Operand
                            End If

                            Debug.Assert((conversionKind And conversionKind.UserDefined) = 0)

                            ' there are cases where there's an explicit cast in code, that may be an identity conversion and 
                            ' it should still get ignored in order to create a call to the GetObjectValue helper.
                            ' e.g. happens in the conversion of the get method of the current property in a for each loop.
                            If Not Conversions.IsIdentityConversion(conversionKind) Then
                                Return result
                            End If

                        Case BoundKind.Call
                            ' Certain helpers introduced in the LocalRewriters shouldn't be cloned.
                            Dim method = DirectCast(nodeToCheck, BoundCall).Method
                            If method = Compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_CompilerServices_LikeOperator__LikeObjectObjectObjectCompareMethod) OrElse
                                method = Compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConcatenateObjectObjectObject) Then
                                Return result
                            End If

                            Exit Do

                        Case Else
                            Debug.Assert(nodeToCheck.Kind <> BoundKind.Parenthesized)
                            Exit Do
                    End Select
                Loop

                Const getObjectValue As WellKnownMember = WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__GetObjectValueObject
                Dim getObjectValueMethod = DirectCast(Compilation.GetWellKnownTypeMember(getObjectValue), MethodSymbol)

                If Not ReportMissingOrBadRuntimeHelper(nodeToCheck, getObjectValue, getObjectValueMethod) Then
                    result = New BoundCall(value.Syntax, getObjectValueMethod, Nothing, Nothing,
                                                      {result}.AsReadOnlyWrap(), Nothing, getObjectValueMethod.ReturnType)
                End If
            End If

            Return result
        End Function

        Public Overrides Function VisitRangeVariable(node As BoundRangeVariable) As BoundNode
            Throw Contract.Unreachable
        End Function

        Public Overrides Function VisitAnonymousTypeFieldInitializer(node As BoundAnonymousTypeFieldInitializer) As BoundNode
            Return Visit(node.Value)
        End Function

        Public Overrides Function VisitAnonymousTypeCreationExpression(node As BoundAnonymousTypeCreationExpression) As BoundNode
            ' Rewrite anonymous type creation expression into ObjectCreationExpression

            Dim fieldsCount As Integer = node.Arguments.Count
            Debug.Assert(fieldsCount > 0)

            Dim newArguments(fieldsCount - 1) As BoundExpression

            ' Those are lazily created created for each field using a local
            Dim locals As ArrayBuilder(Of LocalSymbol) = Nothing

            For index = 0 To fieldsCount - 1
                ' rewrite argument
                newArguments(index) = DirectCast(Me.Visit(node.Arguments(index)), BoundExpression)

                ' if there a local symbol is being used, create assignment
                Dim local As LocalSymbol = If(node.BinderOpt IsNot Nothing,
                                              node.BinderOpt.GetAnonymousTypePropertyLocal(index),
                                              Nothing)
                If local IsNot Nothing Then

                    If locals Is Nothing Then
                        locals = ArrayBuilder(Of LocalSymbol).GetInstance()
                    End If

                    locals.Add(local)

                    ' replace the argument with assignment expression
                    newArguments(index) = New BoundAssignmentOperator(
                                            newArguments(index).Syntax,
                                            New BoundLocal(newArguments(index).Syntax,
                                                           local, True, local.Type),
                                            newArguments(index), True, local.Type)
                End If

            Next

            Dim result As BoundExpression = New BoundObjectCreationExpression(
                                                        node.Syntax,
                                                        DirectCast(node.Type, NamedTypeSymbol).InstanceConstructors(0),
                                                        newArguments.AsReadOnlyWrap(),
                                                        Nothing,
                                                        node.Type)
            If locals IsNot Nothing Then
                result = New BoundSequence(
                                node.Syntax,
                                locals.ToReadOnlyAndFree(),
                                ReadOnlyArray(Of BoundExpression).Empty,
                                result,
                                node.Type)

            End If

            Return result
        End Function

        Public Overrides Function VisitAnonymousTypePropertyAccess(node As BoundAnonymousTypePropertyAccess) As BoundNode
            ' rewrite anonymous type property access into a bound local

            Dim local As LocalSymbol = node.Binder.GetAnonymousTypePropertyLocal(node.PropertyIndex)

            ' NOTE: if anonymous type property access is to be rewritten, the local 
            '       must be present; see comments on bound node declaration
            Contract.ThrowIfNull(local)

            Return New BoundLocal(node.Syntax, local, False, Me.VisitType(local.Type))
        End Function

        Public Overrides Function VisitTernaryConditionalExpression(node As BoundTernaryConditionalExpression) As BoundNode

            Dim result As BoundNode

            If node.Condition.IsConstant AndAlso node.WhenTrue.IsConstant AndAlso node.WhenFalse.IsConstant Then
                ' This optimization be applies if only *all three* operands are constants!!!

                Debug.Assert(node.Condition.ConstantValueOpt.IsBoolean OrElse
                             node.Condition.ConstantValueOpt.IsNothing OrElse
                             node.Condition.ConstantValueOpt.IsString)

                Dim value As Boolean = If(node.Condition.ConstantValueOpt.IsBoolean,
                                          node.Condition.ConstantValueOpt.BooleanValue,
                                          node.Condition.ConstantValueOpt.IsString)

                result = If(value, Visit(node.WhenTrue), Visit(node.WhenFalse))

            Else
                ' NOTE: C# implementation rewrites the ternary expression to handle the bug 
                '       related to IF(<condition>, DirectCast(<class1>, I1), DirectCast(<class2>, I1))
                '       VB handles this case in Emitter

                result = MyBase.VisitTernaryConditionalExpression(node)
            End If

            Return result
        End Function

        Public Overrides Function VisitBinaryConditionalExpression(node As BoundBinaryConditionalExpression) As BoundNode
            Debug.Assert(node.ConvertedTestExpression Is Nothing)   ' Those should be rewritten by now
            Debug.Assert(node.TestExpressionPlaceholder Is Nothing)

            ' NOTE: C# implementation rewrites the coalesce expression to handle the bug 
            '       related to IF(DirectCast(<class1>, I1), DirectCast(<class2>, I1))
            '       VB handles this case in Emitter

            If node.HasErrors Then
                Return MyBase.VisitBinaryConditionalExpression(node)
            End If

            Dim testExpr = node.TestExpression
            Dim testExprType = testExpr.Type
            Dim ifExpressionType = node.Type
            Dim elseExpr = node.ElseExpression

            ' Test expression may only be of a reference or nullable type
            Debug.Assert(testExpr.IsNothingLiteral OrElse testExprType.IsReferenceType OrElse testExprType.IsNullableType)

            If testExpr.IsConstant AndAlso elseExpr.IsConstant Then
                '  the only valid IF(...) with the first constant are: IF("abc", <expr>) or IF(Nothing, <expr>)
                If testExpr.ConstantValueOpt.IsNothing Then
                    ' CASE: IF(Nothing, <expr>) 
                    '   Special case: just emit ElseExpression
                    Return Visit(node.ElseExpression)
                Else
                    ' CASE: IF("abc", <expr>) 
                    '   Dominant type may be different from String, so add conversion
                    Dim conv As KeyValuePair(Of ConversionKind, MethodSymbol) = Nothing
                    Return Visit(testExpr)
                End If

            ElseIf testExprType.IsReferenceType Then
                Return MyBase.VisitBinaryConditionalExpression(node)

            Else
                Binder.ReportDiagnostic(m_Diagnostics, GetLocation(node), ERRID.ERR_NotYetImplementedInRoslyn, "nullable types in binary conditional expression")
                Return MyBase.VisitBinaryConditionalExpression(node)
            End If

            Throw Contract.Unreachable
        End Function

        Public Overrides Function VisitBinaryOperator(node As BoundBinaryOperator) As BoundNode

            Dim result As BoundNode = MyBase.VisitBinaryOperator(node)

            If result.Kind = BoundKind.BinaryOperator AndAlso Not result.HasErrors Then
                node = DirectCast(result, BoundBinaryOperator)

                If node.OperatorKind <> BinaryOperatorKind.Error Then
                    result = RewriteBinaryOperator(node)
                End If
            End If

            Return result
        End Function

        Private Function RewriteBinaryOperator(node As BoundBinaryOperator) As BoundNode
            Dim result As BoundNode = node

            Debug.Assert((node.OperatorKind And BinaryOperatorKind.Lifted) = 0)

            If (node.OperatorKind And BinaryOperatorKind.Lifted) = 0 Then

                Select Case (node.OperatorKind And BinaryOperatorKind.OpMask)
                    Case BinaryOperatorKind.Add
                        If node.Type.IsObjectType() Then
                            result = RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__AddObjectObjectObject)
                        ElseIf node.Type.IsDecimalType() Then
                            result = RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__AddDecimalDecimal)
                        End If

                    Case BinaryOperatorKind.Subtract
                        If node.Type.IsObjectType() Then
                            result = RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__SubtractObjectObjectObject)
                        ElseIf node.Type.IsDecimalType() Then
                            result = RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__SubtractDecimalDecimal)
                        End If

                    Case BinaryOperatorKind.Multiply
                        If node.Type.IsObjectType() Then
                            result = RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__MultiplyObjectObjectObject)
                        ElseIf node.Type.IsDecimalType() Then
                            result = RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__MultiplyDecimalDecimal)
                        End If

                    Case BinaryOperatorKind.Modulo
                        If node.Type.IsObjectType() Then
                            result = RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ModObjectObjectObject)
                        ElseIf node.Type.IsDecimalType() Then
                            result = RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__RemainderDecimalDecimal)
                        End If

                    Case BinaryOperatorKind.Divide
                        If node.Type.IsObjectType() Then
                            result = RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__DivideObjectObjectObject)
                        ElseIf node.Type.IsDecimalType() Then
                            result = RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__DivideDecimalDecimal)
                        End If

                    Case BinaryOperatorKind.IntegerDivide
                        If node.Type.IsObjectType() Then
                            result = RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__IntDivideObjectObjectObject)
                        End If

                    Case BinaryOperatorKind.Power
                        If node.Type.IsObjectType() Then
                            result = RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ExponentObjectObjectObject)
                        Else
                            result = RewritePowOperator(node)
                        End If

                    Case BinaryOperatorKind.LeftShift
                        If node.Type.IsObjectType() Then
                            result = RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__LeftShiftObjectObjectObject)
                        End If

                    Case BinaryOperatorKind.RightShift
                        If node.Type.IsObjectType() Then
                            result = RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__RightShiftObjectObjectObject)
                        End If

                    Case BinaryOperatorKind.OrElse,
                         BinaryOperatorKind.AndAlso
                        If node.Type.IsObjectType() Then
                            result = RewriteObjectShortCircuitOperator(node)
                        End If

                    Case BinaryOperatorKind.Equals
                        If node.Type.IsBooleanType() Then
                            Dim leftType = node.Left.Type

                            If leftType.IsDecimalType() Then
                                result = RewriteDecimalComparisonOperator(node)
                            ElseIf leftType.IsDateTimeType() Then
                                result = RewriteDateComparisonOperator(node)
                            End If
                        End If

                    Case BinaryOperatorKind.NotEquals
                       If node.Type.IsBooleanType() Then
                            Dim leftType = node.Left.Type

                            If leftType.IsDecimalType() Then
                                result = RewriteDecimalComparisonOperator(node)
                            ElseIf leftType.IsDateTimeType() Then
                                result = RewriteDateComparisonOperator(node)
                            End If
                        End If

                    Case BinaryOperatorKind.LessThanOrEqual
                        If node.Type.IsBooleanType() Then
                            Dim leftType = node.Left.Type

                            If leftType.IsDecimalType() Then
                                result = RewriteDecimalComparisonOperator(node)
                            ElseIf leftType.IsDateTimeType() Then
                                result = RewriteDateComparisonOperator(node)
                            End If
                        End If

                    Case BinaryOperatorKind.GreaterThanOrEqual
                        If node.Type.IsBooleanType() Then
                            Dim leftType = node.Left.Type

                            If leftType.IsDecimalType() Then
                                result = RewriteDecimalComparisonOperator(node)
                            ElseIf leftType.IsDateTimeType() Then
                                result = RewriteDateComparisonOperator(node)
                            End If
                        End If

                    Case BinaryOperatorKind.LessThan
                        If node.Type.IsBooleanType() Then
                            Dim leftType = node.Left.Type

                            If leftType.IsDecimalType() Then
                                result = RewriteDecimalComparisonOperator(node)
                            ElseIf leftType.IsDateTimeType() Then
                                result = RewriteDateComparisonOperator(node)
                            End If
                        End If

                    Case BinaryOperatorKind.GreaterThan
                        If node.Type.IsBooleanType() Then
                            Dim leftType = node.Left.Type

                            If leftType.IsDecimalType() Then
                                result = RewriteDecimalComparisonOperator(node)
                            ElseIf leftType.IsDateTimeType() Then
                                result = RewriteDateComparisonOperator(node)
                            End If
                        End If

                    Case BinaryOperatorKind.Xor
                        If node.Type.IsObjectType() Then
                            result = RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__XorObjectObjectObject)
                        End If

                    Case BinaryOperatorKind.Or
                        If node.Type.IsObjectType() Then
                            result = RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__OrObjectObjectObject)
                        End If

                    Case BinaryOperatorKind.And
                        If node.Type.IsObjectType() Then
                            result = RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__AndObjectObjectObject)
                        End If

                End Select
            End If

            Return result
        End Function

        Private Function RewritePowOperator(node As BoundBinaryOperator) As BoundNode

            Dim result As BoundNode = node

            If node.Type.IsDoubleType() AndAlso
               node.Left.Type.IsDoubleType() AndAlso
               node.Right.Type.IsDoubleType() Then

                ' Rewrite as follows:
                ' Math.Pow(left, right)

                Const memberId As WellKnownMember = WellKnownMember.System_Math__PowDoubleDouble
                Dim memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(memberId), MethodSymbol)

                If Not ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then
                    Debug.Assert(memberSymbol.ReturnType.IsDoubleType())

                    result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                           {node.Left, node.Right}.AsReadOnlyWrap(), Nothing, memberSymbol.ReturnType)
                End If
            End If

            Return result
        End Function

        Private Function RewriteDateComparisonOperator(node As BoundBinaryOperator) As BoundNode
            Debug.Assert(node.Left.Type.IsDateTimeType())
            Debug.Assert(node.Right.Type.IsDateTimeType())
            Debug.Assert(node.Type.IsBooleanType())

            Dim result As BoundNode = node

            If node.Left.Type.IsDateTimeType() AndAlso node.Right.Type.IsDateTimeType() Then

                ' Rewrite as follows:
                ' DateTime.Compare(left, right) [Operator] 0

                Const memberId As SpecialMember = SpecialMember.System_DateTime__CompareDateTimeDateTime
                Dim memberSymbol = DirectCast(ContainingAssembly.GetSpecialTypeMember(memberId), MethodSymbol)

                If Not ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then
                    Debug.Assert(memberSymbol.ReturnType.SpecialType = SpecialType.System_Int32)

                    Dim compare = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                                {node.Left, node.Right}.AsReadOnlyWrap(), Nothing, memberSymbol.ReturnType)

                    result = New BoundBinaryOperator(node.Syntax, (node.OperatorKind And BinaryOperatorKind.OpMask),
                                                     compare, New BoundLiteral(node.Syntax, ConstantValue.Create(0I), memberSymbol.ReturnType),
                                                     False, node.Type)
                End If
            End If

            Return result
        End Function

        Private Function RewriteDecimalComparisonOperator(node As BoundBinaryOperator) As BoundNode
            Debug.Assert(node.Left.Type.IsDecimalType())
            Debug.Assert(node.Right.Type.IsDecimalType())
            Debug.Assert(node.Type.IsBooleanType())

            Dim result As BoundNode = node

            If node.Left.Type.IsDecimalType() AndAlso node.Right.Type.IsDecimalType() Then

                ' Rewrite as follows:
                ' Decimal.Compare(left, right) [Operator] 0

                Const memberId As SpecialMember = SpecialMember.System_Decimal__CompareDecimalDecimal
                Dim memberSymbol = DirectCast(ContainingAssembly.GetSpecialTypeMember(memberId), MethodSymbol)

                If Not ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then
                    Debug.Assert(memberSymbol.ReturnType.SpecialType = SpecialType.System_Int32)

                    Dim compare = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                                {node.Left, node.Right}.AsReadOnlyWrap(), Nothing, memberSymbol.ReturnType)

                    result = New BoundBinaryOperator(node.Syntax, (node.OperatorKind And BinaryOperatorKind.OpMask),
                                                     compare, New BoundLiteral(node.Syntax, ConstantValue.Create(0I), memberSymbol.ReturnType),
                                                     False, node.Type)
                End If
            End If

            Return result
        End Function

        Private Function RewriteDecimalBinaryOperator(node As BoundBinaryOperator, member As SpecialMember) As BoundNode
            Debug.Assert(node.Left.Type.IsDecimalType())
            Debug.Assert(node.Right.Type.IsDecimalType())
            Debug.Assert(node.Type.IsDecimalType())

            Dim result As BoundNode = node

            If node.Left.Type.IsDecimalType() AndAlso node.Right.Type.IsDecimalType() Then

                ' Call Decimal.member(left, right)
                Dim memberSymbol = DirectCast(ContainingAssembly.GetSpecialTypeMember(member), MethodSymbol)

                If Not ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then
                    Debug.Assert(memberSymbol.ReturnType.IsDecimalType())
                    result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                           {node.Left, node.Right}.AsReadOnlyWrap(), Nothing, memberSymbol.ReturnType)
                End If
            End If

            Return result
        End Function

        Private Function RewriteObjectShortCircuitOperator(node As BoundBinaryOperator) As BoundNode
            Debug.Assert(node.Type.IsObjectType())

            Dim result As BoundNode = node

            If node.Left.Type.IsObjectType() AndAlso
               node.Right.Type.IsObjectType() Then

                ' This operator translates into:
                '       DirectCast(ToBoolean(left) OrElse/AndAlso ToBoolean(right), Object)
                ' Result is boxed Boolean.

                ' Dev10 uses complex routine in IL gen that emits the calls and also avoids boxing+calling the helper
                ' for result of nested OrElse/AndAlso. I will try to achieve the same effect by digging into DirectCast node
                ' on each side. Since, expressions are rewritten bottom-up, we don't need to look deeper than one level.
                ' Note, we may unwrap unnecessary DirectCast node that wasn't created by this function for nested OrElse/AndAlso, 
                ' but this should not have any negative or observable side effect.

                Dim left = node.Left
                Dim right = node.Right

                If left.Kind = BoundKind.DirectCast Then
                    Dim cast = DirectCast(left, BoundDirectCast)
                    If cast.Operand.Type.IsBooleanType() Then
                        ' Just get rid of DirectCast node.
                        left = cast.Operand
                    End If
                End If

                If right.Kind = BoundKind.DirectCast Then
                    Dim cast = DirectCast(right, BoundDirectCast)
                    If cast.Operand.Type.IsBooleanType() Then
                        ' Just get rid of DirectCast node.
                        right = cast.Operand
                    End If
                End If

                If left Is node.Left OrElse right Is node.Right Then
                    ' Need to call ToBoolean
                    Const memberId As WellKnownMember = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToBooleanObject
                    Dim memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(memberId), MethodSymbol)

                    If Not ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then

                        Debug.Assert(memberSymbol.ReturnType.IsBooleanType())
                        Debug.Assert(memberSymbol.Parameters(0).Type.IsObjectType())

                        If left Is node.Left Then
                            left = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                                 {left}.AsReadOnlyWrap(), Nothing, memberSymbol.ReturnType)
                        End If

                        If right Is node.Right Then
                            right = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                                  {right}.AsReadOnlyWrap(), Nothing, memberSymbol.ReturnType)
                        End If
                    End If
                End If

                If left IsNot node.Left AndAlso right IsNot node.Right Then
                    ' left and right are successfully rewritten
                    Debug.Assert(left.Type.IsBooleanType() AndAlso right.Type.IsBooleanType())

                    Dim op = New BoundBinaryOperator(node.Syntax, (node.OperatorKind And BinaryOperatorKind.OpMask),
                                                     left, right, False, left.Type)
                    ' Box result of the operator
                    result = New BoundDirectCast(node.Syntax, op, ConversionKind.WideningValue, node.Type, Nothing)
                End If

            Else
                Debug.Assert(False)
            End If

            Return result
        End Function

        Private Function RewriteObjectBinaryOperator(node As BoundBinaryOperator, member As WellKnownMember) As BoundNode
            Debug.Assert(node.Left.Type.IsObjectType())
            Debug.Assert(node.Right.Type.IsObjectType())
            Debug.Assert(node.Type.IsObjectType())

            Dim result As BoundNode = node

            ' Call member(left, right)
            Dim memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(member), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then
                result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                       {node.Left, node.Right}.AsReadOnlyWrap(), Nothing, memberSymbol.ReturnType)
            End If

            Return result
        End Function

        Private Function RewriteObjectComparisonOperator(node As BoundBinaryOperator, member As WellKnownMember) As BoundNode
            Debug.Assert(node.Left.Type.IsObjectType())
            Debug.Assert(node.Right.Type.IsObjectType())
            Debug.Assert(node.Type.IsObjectType() OrElse node.Type.IsBooleanType())

            Dim result As BoundNode = node
            Dim compareText As Boolean = (node.OperatorKind And BinaryOperatorKind.CompareText) <> 0

            ' Call member(left, right, compareText)
            Dim memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(member), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then
                Debug.Assert(memberSymbol.ReturnType Is node.Type)
                Debug.Assert(memberSymbol.Parameters(2).Type.IsBooleanType())

                result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                       {node.Left,
                                        node.Right,
                                        New BoundLiteral(node.Syntax, ConstantValue.Create(compareText), memberSymbol.Parameters(2).Type)}.AsReadOnlyWrap(),
                                        Nothing,
                                        memberSymbol.ReturnType)
            End If

            Return result
        End Function

        Public Overrides Function VisitUnaryOperator(node As BoundUnaryOperator) As BoundNode

            Dim result As BoundNode = MyBase.VisitUnaryOperator(node)

            If result.Kind = BoundKind.UnaryOperator AndAlso Not result.HasErrors Then
                node = DirectCast(result, BoundUnaryOperator)

                If node.OperatorKind <> UnaryOperatorKind.Error Then
                    result = RewriteUnaryOperator(node)
                End If
            End If

            Return result
        End Function

        Private Function RewriteUnaryOperator(node As BoundUnaryOperator) As BoundNode
            Dim result As BoundNode = node

            'TODO: Rewrite user-defined operators into calls.

            'TODO: Rewrite lifted operators
            If (node.OperatorKind And UnaryOperatorKind.Lifted) = 0 Then
                Dim opType = node.Type

                If opType.IsObjectType() Then
                    result = RewriteObjectUnaryOperator(node)
                ElseIf opType.IsDecimalType() Then
                    result = RewriteDecimalUnaryOperator(node)
                End If
            End If

            Return result
        End Function

        Private Function RewriteObjectUnaryOperator(node As BoundUnaryOperator) As BoundNode
            Debug.Assert(node.Operand.Type.IsObjectType() AndAlso node.Type.IsObjectType())

            Dim result As BoundNode = node
            Dim opKind = (node.OperatorKind And UnaryOperatorKind.IntrinsicOpMask)

            Dim member As WellKnownMember

            If opKind = UnaryOperatorKind.Plus Then
                member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__PlusObjectObject
            ElseIf opKind = UnaryOperatorKind.Minus Then
                member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__NegateObjectObject
            Else
                Debug.Assert(opKind = UnaryOperatorKind.Not)
                member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__NotObjectObject
            End If

            ' Call member(operand)
            Dim memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(member), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then
                result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                       {node.Operand}.AsReadOnlyWrap(), Nothing, memberSymbol.ReturnType)
            End If

            Return result
        End Function

        Private Function RewriteDecimalUnaryOperator(node As BoundUnaryOperator) As BoundNode
            Debug.Assert(node.Operand.Type.IsDecimalType() AndAlso node.Type.IsDecimalType())

            Dim result As BoundNode = node
            Dim opKind = (node.OperatorKind And UnaryOperatorKind.IntrinsicOpMask)

            If opKind = UnaryOperatorKind.Plus Then
                result = node.Operand
            Else
                Debug.Assert(opKind = UnaryOperatorKind.Minus)

                ' Call Decimal.Negate(operand)

                Const memberId As SpecialMember = SpecialMember.System_Decimal__NegateDecimal
                Dim memberSymbol = DirectCast(ContainingAssembly.GetSpecialTypeMember(memberId), MethodSymbol)

                If Not ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then
                    result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                           {node.Operand}.AsReadOnlyWrap(), Nothing, memberSymbol.ReturnType)
                End If
            End If

            Return result
        End Function

        Public Overrides Function VisitDirectCast(node As BoundDirectCast) As BoundNode
            Debug.Assert(node.RelaxationLambdaOpt Is Nothing)

            ' todo(rbeckers) enable this if delegate conversions are enabled again
            'If node.Operand.Kind = BoundKind.DelegateCreationExpression Then
            '    Return node.Operand
            'End If

            Dim result As BoundNode = MyBase.VisitDirectCast(node)

            Return result
        End Function

        Public Overrides Function VisitConversion(node As BoundConversion) As BoundNode
            Debug.Assert(node.RelaxationLambdaOpt Is Nothing)

            ' todo(rbeckers) enable this if delegate conversions are enabled again
            'If node.Operand.Kind = BoundKind.DelegateCreationExpression Then
            '    Return node.Operand
            'End If

            Dim result As BoundNode = MyBase.VisitConversion(node)
            If result.Kind = BoundKind.Conversion AndAlso Not result.HasErrors Then
                result = RewriteConversion(DirectCast(result, BoundConversion))
            End If

            Return result
        End Function

        Private Function RewriteConversion(node As BoundConversion) As BoundNode
            Dim result As BoundNode = node

            Dim underlyingTypeTo = node.Type.GetEnumUnderlyingTypeOrSelf()

            Dim operand = node.Operand

            If operand.IsNothingLiteral() Then
                Debug.Assert(node.ConversionKind = ConversionKind.WideningNothingLiteral OrElse
                             (Conversions.IsIdentityConversion(node.ConversionKind) AndAlso
                                Not underlyingTypeTo.IsTypeParameter() AndAlso underlyingTypeTo.IsReferenceType) OrElse
                             (node.ConversionKind And (ConversionKind.Reference Or ConversionKind.Array)) <> 0)

                If underlyingTypeTo.IsTypeParameter() OrElse underlyingTypeTo.IsReferenceType Then
                    result = RewriteAsDirectCast(node)
                Else
                    Debug.Assert(underlyingTypeTo.IsValueType)
                    ' Find the parameterless constructor to be used in conversion of Nothing to a value type
                    result = InitWithParameterlessValueTypeConstructor(node, DirectCast(underlyingTypeTo, NamedTypeSymbol))
                End If

            ElseIf operand.Kind = BoundKind.Lambda Then
                Throw Contract.Unreachable
            Else

                Dim underlyingTypeFrom = operand.Type.GetEnumUnderlyingTypeOrSelf()

                If underlyingTypeFrom.IsFloatingType() AndAlso underlyingTypeTo.IsIntegralType() Then
                    result = RewriteFloatingToIntegralConversion(node, underlyingTypeFrom, underlyingTypeTo)

                ElseIf underlyingTypeFrom.IsDecimalType() AndAlso
                    (underlyingTypeTo.IsBooleanType() OrElse underlyingTypeTo.IsIntegralType() OrElse underlyingTypeTo.IsFloatingType) Then
                    result = RewriteDecimalToNumericOrBooleanConversion(node, underlyingTypeFrom, underlyingTypeTo)

                ElseIf underlyingTypeTo.IsDecimalType() AndAlso
                    (underlyingTypeFrom.IsBooleanType() OrElse underlyingTypeFrom.IsIntegralType() OrElse underlyingTypeFrom.IsFloatingType) Then
                    result = RewriteNumericOrBooleanToDecimalConversion(node, underlyingTypeFrom, underlyingTypeTo)

                ElseIf underlyingTypeFrom.IsNullableType OrElse underlyingTypeTo.IsNullableType Then
                    ' conversions between nullable and reference types are not directcasts, they are boxing/unboxing conversions.
                    ' CodeGen will handle this.

                ElseIf underlyingTypeFrom.IsObjectType() AndAlso
                    (underlyingTypeTo.IsTypeParameter() OrElse underlyingTypeTo.IsIntrinsicType()) Then
                    result = RewriteFromObjectConversion(node, underlyingTypeFrom, underlyingTypeTo)

                ElseIf underlyingTypeFrom.IsTypeParameter() Then
                    result = RewriteAsDirectCast(node)

                ElseIf underlyingTypeTo.IsTypeParameter() Then
                    result = RewriteAsDirectCast(node)

                ElseIf underlyingTypeFrom.IsStringType() AndAlso
                     (underlyingTypeTo.IsCharArrayRankOne() OrElse underlyingTypeTo.IsIntrinsicValueType()) Then
                    result = RewriteFromStringConversion(node, underlyingTypeFrom, underlyingTypeTo)

                ElseIf underlyingTypeTo.IsStringType() AndAlso
                    (underlyingTypeFrom.IsCharArrayRankOne() OrElse underlyingTypeFrom.IsIntrinsicValueType()) Then
                    result = RewriteToStringConversion(node, underlyingTypeFrom, underlyingTypeTo)

                ElseIf underlyingTypeFrom.IsReferenceType AndAlso underlyingTypeTo.IsCharArrayRankOne() Then
                    result = RewriteReferenceTypeToCharArrayRankOneConversion(node, underlyingTypeFrom, underlyingTypeTo)

                ElseIf underlyingTypeTo.IsReferenceType Then
                    result = RewriteAsDirectCast(node)

                Else
                    Debug.Assert(underlyingTypeTo.IsValueType)
                    ' Find the parameterless constructor to be used in emit phase, see 'CodeGenerator.EmitConversionExpression'
                    result = InitWithParameterlessValueTypeConstructor(node, DirectCast(underlyingTypeTo, NamedTypeSymbol))
                End If
            End If

            Return result
        End Function

        ''' <summary> Given bound conversion node and the type the conversion is being done to initializes 
        ''' bound conversion node with the reference to parameterless value type constructor and returns 
        ''' modified bound node.
        ''' In case the constructor is not accessible from current context, or there is no parameterless
        ''' constructor found in the type (which should never happen, because in such cases a synthesized 
        ''' constructor is supposed to be generated)
        ''' </summary>
        Private Function InitWithParameterlessValueTypeConstructor(node As BoundConversion, typeTo As NamedTypeSymbol) As BoundNode
            Debug.Assert(typeTo.IsValueType AndAlso Not typeTo.IsTypeParameter)
            Debug.Assert(node.RelaxationLambdaOpt Is Nothing AndAlso node.RelaxationReceiverTempOpt Is Nothing)

            '  find valuetype parameterless constructor and check the accessibility
            For Each constr In typeTo.InstanceConstructors
                ' NOTE: we intentionally skip constructors with all 
                '       optional parameters; this matches Dev10 behaviour
                If constr.ParameterCount = 0 Then

                    '  check 'constr' 
                    If AccessCheck.IsSymbolAccessible(constr, Me.m_Method.ContainingType, typeTo) Then
                        ' before we use constructor symbol we need to report use site error if any
                        Dim useSiteError = constr.GetUseSiteErrorInfo()
                        If useSiteError IsNot Nothing Then
                            ReportRuntimeHelperError(node, useSiteError)
                        End If

                        ' update bound node
                        Return node.Update(node.Operand,
                                             node.ConversionKind,
                                             node.Checked,
                                             node.ExplicitCastInCode,
                                             node.ConstantValueOpt,
                                             constr,
                                             node.RelaxationLambdaOpt,
                                             node.RelaxationReceiverTempOpt,
                                             node.Type)
                    End If

                    '  exit for each in any case
                    Return node
                End If
            Next

            ' This point should not be reachable, because if there is no constructor in the 
            ' loaded value type, we should have generated a synthesized constructor.
            Debug.Assert(False)
            Return node
        End Function

        Private Function RewriteReferenceTypeToCharArrayRankOneConversion(node As BoundConversion, typeFrom As TypeSymbol, typeTo As TypeSymbol) As BoundNode
            Debug.Assert(typeFrom.IsReferenceType AndAlso typeTo.IsCharArrayRankOne())

            Dim result As BoundNode = node
            Const member As WellKnownMember = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToCharArrayRankOneObject

            Dim memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(member), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then

                Dim operand = node.Operand

                Debug.Assert(memberSymbol.Parameters(0).Type.IsObjectType())

                If Not operand.Type.IsObjectType() Then
                    Dim objectType As TypeSymbol = memberSymbol.Parameters(0).Type
                    operand = New BoundDirectCast(operand.Syntax,
                                                  operand,
                                                  Conversions.ClassifyDirectCastConversion(operand.Type, objectType),
                                                  objectType)
                End If

                result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                       {operand}.AsReadOnlyWrap(), Nothing, memberSymbol.ReturnType)

                Debug.Assert(memberSymbol.ReturnType.IsSameTypeIgnoringCustomModifiers(node.Type))
            End If

            Return result
        End Function

        Private Function RewriteAsDirectCast(node As BoundConversion) As BoundNode

            Debug.Assert(node.Operand.IsNothingLiteral() OrElse
                         (node.ConversionKind And (Not ConversionKind.DelegateRelaxationLevelMask)) =
                            Conversions.ClassifyDirectCastConversion(node.Operand.Type, node.Type))

            ' TODO: A chain of widening reference conversions that starts from NOTHING literal can be collapsed to a single node.
            '       Semantics::Convert does this in Dev10.
            '       It looks like we already achieve the same result due to folding of NOTHING conversions.

            Return New BoundDirectCast(node.Syntax, node.Operand, node.ConversionKind, node.Type, Nothing)
        End Function

        Private Function RewriteFromObjectConversion(node As BoundConversion, typeFrom As TypeSymbol, underlyingTypeTo As TypeSymbol) As BoundNode
            Debug.Assert(typeFrom.IsObjectType())

            Dim result As BoundNode = node
            Dim member As WellKnownMember = WellKnownMember.Count

            Select Case underlyingTypeTo.SpecialType
                Case SpecialType.System_Boolean : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToBooleanObject
                Case SpecialType.System_SByte : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToSByteObject
                Case SpecialType.System_Byte : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToByteObject
                Case SpecialType.System_Int16 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToShortObject
                Case SpecialType.System_UInt16 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToUShortObject
                Case SpecialType.System_Int32 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToIntegerObject
                Case SpecialType.System_UInt32 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToUIntegerObject
                Case SpecialType.System_Int64 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToLongObject
                Case SpecialType.System_UInt64 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToULongObject
                Case SpecialType.System_Single : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToSingleObject
                Case SpecialType.System_Double : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDoubleObject
                Case SpecialType.System_Decimal : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDecimalObject
                Case SpecialType.System_DateTime : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDateObject
                Case SpecialType.System_Char : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToCharObject
                Case SpecialType.System_String : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringObject

                Case Else
                    If underlyingTypeTo.IsTypeParameter() Then
                        member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToGenericParameter_T_Object
                    End If
            End Select

            If member <> WellKnownMember.Count Then

                Dim memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(member), MethodSymbol)

                If Not ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then

                    If member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToGenericParameter_T_Object Then
                        memberSymbol = memberSymbol.Construct(underlyingTypeTo)
                    End If

                    Dim operand = node.Operand

                    Debug.Assert(memberSymbol.ReturnType.IsSameTypeIgnoringCustomModifiers(underlyingTypeTo))
                    Debug.Assert(memberSymbol.Parameters(0).Type Is typeFrom)

                    result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                           {operand}.AsReadOnlyWrap(), Nothing, memberSymbol.ReturnType)

                    Dim targetResultType = node.Type

                    If Not targetResultType.IsSameTypeIgnoringCustomModifiers(memberSymbol.ReturnType) Then
                        ' Must be conversion to an enum
                        Debug.Assert(targetResultType.IsEnumType())

                        Dim conv = ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions
                        Debug.Assert(conv = Conversions.ClassifyConversion(memberSymbol.ReturnType, targetResultType).Key)

                        result = New BoundConversion(node.Syntax, DirectCast(result, BoundExpression),
                                                     conv, node.Checked, node.ExplicitCastInCode, targetResultType, Nothing)
                    End If
                End If
            End If

            Return result
        End Function

        Private Function RewriteToStringConversion(node As BoundConversion, underlyingTypeFrom As TypeSymbol, typeTo As TypeSymbol) As BoundNode
            Debug.Assert(typeTo.IsStringType())

            Dim result As BoundNode = node
            Dim memberSymbol As MethodSymbol = Nothing

            If underlyingTypeFrom.IsCharArrayRankOne() Then
                Const memberId As SpecialMember = SpecialMember.System_String__CtorSZArrayChar
                memberSymbol = DirectCast(ContainingAssembly.GetSpecialTypeMember(memberId), MethodSymbol)

                If ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then
                    memberSymbol = Nothing
                End If

            Else
                Dim member As WellKnownMember = WellKnownMember.Count

                ' Note, conversion from Object is handled by RewriteFromObjectConversion.
                Select Case underlyingTypeFrom.SpecialType
                    Case SpecialType.System_Boolean : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringBoolean
                    Case SpecialType.System_SByte,
                         SpecialType.System_Int16,
                         SpecialType.System_Int32 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringInt32

                    Case SpecialType.System_Byte : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringByte

                    Case SpecialType.System_UInt16,
                         SpecialType.System_UInt32 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringUInt32

                    Case SpecialType.System_Int64 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringInt64
                    Case SpecialType.System_UInt64 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringUInt64
                    Case SpecialType.System_Single : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringSingle
                    Case SpecialType.System_Double : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringDouble
                    Case SpecialType.System_Decimal : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringDecimal
                    Case SpecialType.System_DateTime : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringDateTime
                    Case SpecialType.System_Char : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToStringChar
                End Select

                If member <> WellKnownMember.Count Then

                    memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(member), MethodSymbol)

                    If ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then
                        memberSymbol = Nothing
                    End If
                End If
            End If

            If memberSymbol IsNot Nothing Then

                Dim operand = node.Operand
                Dim operandType = operand.Type

                If Not operandType.IsSameTypeIgnoringCustomModifiers(memberSymbol.Parameters(0).Type) Then
                    Dim conv As ConversionKind

                    If operandType.IsEnumType() Then
                        conv = ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions
                    Else
                        conv = ConversionKind.WideningNumeric
                    End If

                    Debug.Assert(conv = Conversions.ClassifyConversion(operandType, memberSymbol.Parameters(0).Type).Key)

                    operand = New BoundConversion(node.Syntax, operand, conv, node.Checked, node.ExplicitCastInCode,
                                                  memberSymbol.Parameters(0).Type, Nothing)
                End If

                If memberSymbol.MethodKind = MethodKind.Constructor Then
                    Debug.Assert(memberSymbol.ContainingType Is typeTo)

                    result = New BoundObjectCreationExpression(
                        node.Syntax,
                        memberSymbol,
                        {operand}.AsReadOnlyWrap(),
                        Nothing,
                        typeTo)
                Else
                    Debug.Assert(memberSymbol.ReturnType Is typeTo)
                    result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                           {operand}.AsReadOnlyWrap(), Nothing, memberSymbol.ReturnType)
                End If
            End If

            Return result
        End Function

        Private Function RewriteFromStringConversion(node As BoundConversion, typeFrom As TypeSymbol, underlyingTypeTo As TypeSymbol) As BoundNode
            Debug.Assert(typeFrom.IsStringType())

            Dim result As BoundNode = node
            Dim member As WellKnownMember = WellKnownMember.Count

            Select Case underlyingTypeTo.SpecialType
                Case SpecialType.System_Boolean : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToBooleanString
                Case SpecialType.System_SByte : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToSByteString
                Case SpecialType.System_Byte : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToByteString
                Case SpecialType.System_Int16 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToShortString
                Case SpecialType.System_UInt16 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToUShortString
                Case SpecialType.System_Int32 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToIntegerString
                Case SpecialType.System_UInt32 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToUIntegerString
                Case SpecialType.System_Int64 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToLongString
                Case SpecialType.System_UInt64 : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToULongString
                Case SpecialType.System_Single : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToSingleString
                Case SpecialType.System_Double : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDoubleString
                Case SpecialType.System_Decimal : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDecimalString
                Case SpecialType.System_DateTime : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDateString
                Case SpecialType.System_Char : member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToCharString
                Case Else
                    If underlyingTypeTo.IsCharArrayRankOne() Then
                        member = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToCharArrayRankOneString
                    End If
            End Select

            If member <> WellKnownMember.Count Then

                Dim memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(member), MethodSymbol)

                If Not ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then
                    Dim operand = node.Operand

                    Debug.Assert(memberSymbol.ReturnType.IsSameTypeIgnoringCustomModifiers(underlyingTypeTo))
                    Debug.Assert(memberSymbol.Parameters(0).Type Is typeFrom)

                    result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                           {operand}.AsReadOnlyWrap(), Nothing, memberSymbol.ReturnType)

                    Dim targetResultType = node.Type

                    If Not targetResultType.IsSameTypeIgnoringCustomModifiers(memberSymbol.ReturnType) Then
                        ' Must be conversion to an enum
                        Debug.Assert(targetResultType.IsEnumType())
                        Dim conv = ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions

                        Debug.Assert(conv = Conversions.ClassifyConversion(memberSymbol.ReturnType, targetResultType).Key)

                        result = New BoundConversion(node.Syntax, DirectCast(result, BoundExpression),
                                                     conv, node.Checked, node.ExplicitCastInCode, targetResultType, Nothing)
                    End If
                End If
            End If

            Return result
        End Function

        Private Function RewriteNumericOrBooleanToDecimalConversion(node As BoundConversion, underlyingTypeFrom As TypeSymbol, typeTo As TypeSymbol) As BoundNode
            Debug.Assert(typeTo.IsDecimalType() AndAlso
                (underlyingTypeFrom.IsBooleanType() OrElse underlyingTypeFrom.IsIntegralType() OrElse underlyingTypeFrom.IsFloatingType))

            Dim result As BoundNode = node
            Dim memberSymbol As MethodSymbol

            If underlyingTypeFrom.IsBooleanType() Then
                Const memberId As WellKnownMember = WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ToDecimalBoolean
                memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(memberId), MethodSymbol)

                If ReportMissingOrBadRuntimeHelper(node, memberId, memberSymbol) Then
                    memberSymbol = Nothing
                End If
            Else
                Dim member As SpecialMember

                Select Case underlyingTypeFrom.SpecialType
                    Case SpecialType.System_SByte,
                         SpecialType.System_Byte,
                         SpecialType.System_Int16,
                         SpecialType.System_UInt16,
                         SpecialType.System_Int32 : member = SpecialMember.System_Decimal__CtorInt32
                    Case SpecialType.System_UInt32 : member = SpecialMember.System_Decimal__CtorUInt32
                    Case SpecialType.System_Int64 : member = SpecialMember.System_Decimal__CtorInt64
                    Case SpecialType.System_UInt64 : member = SpecialMember.System_Decimal__CtorUInt64
                    Case SpecialType.System_Single : member = SpecialMember.System_Decimal__CtorSingle
                    Case SpecialType.System_Double : member = SpecialMember.System_Decimal__CtorDouble
                    Case Else
                        'cannot get here
                        Return result
                End Select

                memberSymbol = DirectCast(ContainingAssembly.GetSpecialTypeMember(member), MethodSymbol)

                If ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then
                    memberSymbol = Nothing
                End If
            End If

            ' Call the method.

            If memberSymbol IsNot Nothing Then

                Dim operand = node.Operand
                Dim operandType = operand.Type

                If operandType IsNot memberSymbol.Parameters(0).Type Then
                    Dim conv As ConversionKind

                    If operandType.IsEnumType() Then
                        conv = ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions
                    Else
                        conv = ConversionKind.WideningNumeric
                    End If

                    Debug.Assert(conv = Conversions.ClassifyConversion(operandType, memberSymbol.Parameters(0).Type).Key)

                    operand = New BoundConversion(node.Syntax, operand, conv, node.Checked, node.ExplicitCastInCode,
                                                  memberSymbol.Parameters(0).Type, Nothing)
                End If

                If memberSymbol.MethodKind = MethodKind.Constructor Then
                    Debug.Assert(memberSymbol.ContainingType Is typeTo)

                    result = New BoundObjectCreationExpression(
                        node.Syntax,
                        memberSymbol,
                        {operand}.AsReadOnlyWrap(),
                        Nothing,
                        typeTo)
                Else
                    Debug.Assert(memberSymbol.ReturnType Is typeTo)
                    result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                           {operand}.AsReadOnlyWrap(), Nothing, memberSymbol.ReturnType)
                End If
            End If

            Return result
        End Function

        Private Function RewriteDecimalToNumericOrBooleanConversion(node As BoundConversion, typeFrom As TypeSymbol, underlyingTypeTo As TypeSymbol) As BoundNode
            Debug.Assert(typeFrom.IsDecimalType() AndAlso
                (underlyingTypeTo.IsBooleanType() OrElse underlyingTypeTo.IsIntegralType() OrElse underlyingTypeTo.IsFloatingType))

            Dim result As BoundNode = node
            Dim member As WellKnownMember

            Select Case underlyingTypeTo.SpecialType
                Case SpecialType.System_Boolean : member = WellKnownMember.System_Convert__ToBooleanDecimal
                Case SpecialType.System_SByte : member = WellKnownMember.System_Convert__ToSByteDecimal
                Case SpecialType.System_Byte : member = WellKnownMember.System_Convert__ToByteDecimal
                Case SpecialType.System_Int16 : member = WellKnownMember.System_Convert__ToInt16Decimal
                Case SpecialType.System_UInt16 : member = WellKnownMember.System_Convert__ToUInt16Decimal
                Case SpecialType.System_Int32 : member = WellKnownMember.System_Convert__ToInt32Decimal
                Case SpecialType.System_UInt32 : member = WellKnownMember.System_Convert__ToUInt32Decimal
                Case SpecialType.System_Int64 : member = WellKnownMember.System_Convert__ToInt64Decimal
                Case SpecialType.System_UInt64 : member = WellKnownMember.System_Convert__ToUInt64Decimal
                Case SpecialType.System_Single : member = WellKnownMember.System_Convert__ToSingleDecimal
                Case SpecialType.System_Double : member = WellKnownMember.System_Convert__ToDoubleDecimal
                Case Else
                    'cannot get here
                    Return result
            End Select

            Dim memberSymbol As MethodSymbol
            ' Call the method.

            memberSymbol = DirectCast(Compilation.GetWellKnownTypeMember(member), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, member, memberSymbol) Then
                Dim operand = node.Operand

                Debug.Assert(memberSymbol.ReturnType Is underlyingTypeTo)
                Debug.Assert(memberSymbol.Parameters(0).Type Is typeFrom)

                result = New BoundCall(node.Syntax, memberSymbol, Nothing, Nothing,
                                       {operand}.AsReadOnlyWrap(), Nothing, memberSymbol.ReturnType)

                Dim targetResultType = node.Type

                If targetResultType IsNot memberSymbol.ReturnType Then
                    ' Must be conversion to an enum
                    Debug.Assert(targetResultType.IsEnumType())
                    Dim conv = ConversionKind.NarrowingNumeric Or ConversionKind.InvolvesEnumTypeConversions

                    Debug.Assert(conv = Conversions.ClassifyConversion(memberSymbol.ReturnType, targetResultType).Key)

                    result = New BoundConversion(node.Syntax, DirectCast(result, BoundExpression),
                                                 conv, node.Checked, node.ExplicitCastInCode, targetResultType, Nothing)
                End If
            End If

            Return result
        End Function

        Private Function RewriteFloatingToIntegralConversion(node As BoundConversion, typeFrom As TypeSymbol, underlyingTypeTo As TypeSymbol) As BoundNode
            Debug.Assert(typeFrom.IsFloatingType() AndAlso underlyingTypeTo.IsIntegralType())
            Dim result As BoundNode = node

            Dim mathRound As MethodSymbol
            ' Call Math.Round method to enforce VB style rounding.

            Const memberId As WellKnownMember = WellKnownMember.System_Math__RoundDouble
            mathRound = DirectCast(Compilation.GetWellKnownTypeMember(memberId), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, memberId, mathRound) Then
                ' If we got here and passed badness check, it should be safe to assume that we have 
                ' a "good" symbol for Double type

                Dim operand = node.Operand

                If typeFrom IsNot mathRound.Parameters(0).Type Then
                    ' Converting from Single
                    Debug.Assert(ConversionKind.WideningNumeric = Conversions.ClassifyConversion(typeFrom, mathRound.Parameters(0).Type).Key)
                    operand = New BoundConversion(node.Syntax, operand, ConversionKind.WideningNumeric, node.Checked, node.ExplicitCastInCode,
                                                  mathRound.Parameters(0).Type, Nothing)
                End If

                Dim callMathRound = New BoundCall(node.Syntax, mathRound, Nothing, Nothing,
                                                  {operand}.AsReadOnlyWrap(), Nothing, mathRound.ReturnType)

                Debug.Assert(node.ConversionKind = Conversions.ClassifyConversion(mathRound.ReturnType, node.Type).Key)
                result = New BoundConversion(node.Syntax, callMathRound, node.ConversionKind,
                                             node.Checked, node.ExplicitCastInCode, node.Type, Nothing)
            End If

            Return result
        End Function

        Public Overrides Function Visit(node As BoundNode) As BoundNode
            Dim expressionNode = TryCast(node, BoundExpression)

            If expressionNode IsNot Nothing Then
                Dim value = expressionNode.ConstantValueOpt
                If value IsNot Nothing Then
                    Return RewriteConstant(expressionNode, value)
                End If
            End If

            Return MyBase.Visit(node)
        End Function

        Private Function RewriteConstant(node As BoundExpression, nodeValue As ConstantValue) As BoundNode
            Dim result As BoundNode = node

            If Not node.HasErrors Then
                If nodeValue.Discriminator = ConstantValueTypeDiscriminator.Decimal Then
                    result = RewriteDecimalConstant(node, nodeValue)

                ElseIf nodeValue.Discriminator = ConstantValueTypeDiscriminator.DateTime Then
                    result = RewriteDateConstant(node, nodeValue)
                End If
            End If

            Return result
        End Function

        Private Function RewriteDateConstant(node As BoundExpression, nodeValue As ConstantValue) As BoundNode

            Dim dt = nodeValue.DateTimeValue

            ' If we are building static constructor of System.DateTime, accessing static fields 
            ' would be bad.
            If dt = Date.MinValue AndAlso
                (m_Method.MethodKind <> MethodKind.SharedConstructor OrElse
                m_Method.ContainingType.SpecialType <> SpecialType.System_DateTime) Then

                Dim dtMinValue = DirectCast(
                    ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_DateTime__MinValue), 
                    FieldSymbol)

                If dtMinValue IsNot Nothing AndAlso dtMinValue.GetUseSiteErrorInfo() Is Nothing AndAlso dtMinValue.ContainingType.GetUseSiteErrorInfo() Is Nothing Then
                    Return New BoundFieldAccess(node.Syntax, Nothing, dtMinValue, IsLValue:=False, Type:=dtMinValue.Type)
                End If
            End If

            ' This one makes a call to System.DateTime::.ctor(int64) in mscorlib

            Dim dtCtorInt64 As MethodSymbol

            Const memberId As SpecialMember = SpecialMember.System_DateTime__CtorInt64
            dtCtorInt64 = DirectCast(ContainingAssembly.GetSpecialTypeMember(memberId), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, memberId, dtCtorInt64) Then

                ' generate New Decimal(value)
                Return New BoundObjectCreationExpression(
                    node.Syntax,
                    dtCtorInt64,
                    ReadOnlyArrayExtensions.AsReadOnlyWrap(Of BoundExpression)(
                        {New BoundLiteral(node.Syntax, ConstantValue.Create(dt.Ticks), dtCtorInt64.Parameters(0).Type)}),
                    Nothing,
                    node.Type)
            End If

            Return node ' We get here only if we failed to rewrite the constant
        End Function

        Private Function RewriteDecimalConstant(node As BoundExpression, nodeValue As ConstantValue) As BoundNode

            Dim decInfo As DecimalData = nodeValue.DecimalValue.GetBits()

            Dim isNegative As Boolean = decInfo.sign

            ' if we have a number which only uses the bottom 4 bytes and
            ' has no fraction part, then we can generate more optimal code

            If decInfo.scale = 0 AndAlso decInfo.Hi32 = 0 AndAlso decInfo.Mid32 = 0 Then

                ' If we are building static constructor of System.Decimal, accessing static fields 
                ' would be bad.
                If m_Method.MethodKind <> MethodKind.SharedConstructor OrElse
                   m_Method.ContainingType.SpecialType <> SpecialType.System_Decimal Then

                    Dim useField As Symbol = Nothing

                    If decInfo.Lo32 = 0 Then
                        ' whole value == 0 if we get here
                        useField = ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_Decimal__Zero)
                    ElseIf decInfo.Lo32 = 1 Then
                        If isNegative Then
                            ' whole value == -1 if we get here
                            useField = ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_Decimal__MinusOne)
                        Else
                            ' whole value == 1 if we get here
                            useField = ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_Decimal__One)
                        End If
                    End If

                    If useField IsNot Nothing AndAlso useField.GetUseSiteErrorInfo() Is Nothing AndAlso useField.ContainingType.GetUseSiteErrorInfo() Is Nothing Then
                        Dim fieldSymbol = DirectCast(useField, FieldSymbol)
                        Return New BoundFieldAccess(node.Syntax, Nothing, fieldSymbol, IsLValue:=False, Type:=fieldSymbol.Type)
                    End If
                End If

                ' Convert from unsigned to signed.  To do this, store into a
                ' larger data type (this won't do sign extension), and then set the sign
                ' 
                Dim value As Int64 = decInfo.Lo32

                If isNegative Then
                    value = -value
                End If

                Dim decCtorInt64 As MethodSymbol

                decCtorInt64 = DirectCast(
                            ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_Decimal__CtorInt64), 
                            MethodSymbol)

                If decCtorInt64 IsNot Nothing AndAlso decCtorInt64.GetUseSiteErrorInfo() Is Nothing AndAlso decCtorInt64.ContainingType.GetUseSiteErrorInfo() Is Nothing Then

                    ' generate New Decimal(value)
                    Return New BoundObjectCreationExpression(
                        node.Syntax,
                        decCtorInt64,
                        ReadOnlyArrayExtensions.AsReadOnlyWrap(Of BoundExpression)(
                            {New BoundLiteral(node.Syntax, ConstantValue.Create(value), decCtorInt64.Parameters(0).Type)}),
                        Nothing,
                        node.Type)
                End If
            End If

            ' Looks like we have to do it the hard way
            ' Emit all parts of the value, including Sign info and Scale info
            ' 
            'Public Sub New( _
            ' lo As Integer, _
            ' mid As Integer, _
            ' hi As Integer, _
            ' isNegative As Boolean, _
            ' scale As Byte _
            ')

            Dim decCtor As MethodSymbol = Nothing

            Const memberId As SpecialMember = SpecialMember.System_Decimal__CtorInt32Int32Int32BooleanByte
            decCtor = DirectCast(ContainingAssembly.GetSpecialTypeMember(memberId), MethodSymbol)

            If Not ReportMissingOrBadRuntimeHelper(node, memberId, decCtor) Then
                ' generate New Decimal(lo, mid, hi, isNegative, scale)
                Return New BoundObjectCreationExpression(
                    node.Syntax,
                    decCtor,
                    ReadOnlyArrayExtensions.AsReadOnlyWrap(Of BoundExpression)(
                        {New BoundLiteral(node.Syntax, ConstantValue.Create(UncheckedCInt(decInfo.Lo32)), decCtor.Parameters(0).Type),
                         New BoundLiteral(node.Syntax, ConstantValue.Create(UncheckedCInt(decInfo.Mid32)), decCtor.Parameters(1).Type),
                         New BoundLiteral(node.Syntax, ConstantValue.Create(UncheckedCInt(decInfo.Hi32)), decCtor.Parameters(2).Type),
                         New BoundLiteral(node.Syntax, ConstantValue.Create(decInfo.sign), decCtor.Parameters(3).Type),
                         New BoundLiteral(node.Syntax, ConstantValue.Create(decInfo.scale), decCtor.Parameters(4).Type)}),
                   Nothing,
                   node.Type)
            End If

            Return node ' We get here only if we failed to rewrite the constant
        End Function

    End Class
End Namespace
