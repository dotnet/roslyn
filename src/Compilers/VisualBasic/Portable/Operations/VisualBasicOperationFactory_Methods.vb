' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.AnonymousTypeManager
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Operations
    Partial Friend NotInheritable Class VisualBasicOperationFactory
        Private Shared Function IsMidStatement(node As BoundNode) As Boolean
            If node.Kind = BoundKind.Conversion Then
                node = DirectCast(node, BoundConversion).Operand

                If node.Kind = BoundKind.UserDefinedConversion Then
                    node = DirectCast(node, BoundUserDefinedConversion).Operand
                End If
            End If

            Return node.Kind = BoundKind.MidResult
        End Function

        Friend Function CreateCompoundAssignmentRightOperand(boundAssignment As BoundAssignmentOperator) As IOperation
            Dim binaryOperator As BoundExpression = Nothing
            Select Case boundAssignment.Right.Kind
                Case BoundKind.Conversion
                    Dim inConversionNode = DirectCast(boundAssignment.Right, BoundConversion)
                    binaryOperator = GetConversionOperand(inConversionNode)
                Case BoundKind.UserDefinedBinaryOperator, BoundKind.BinaryOperator
                    binaryOperator = boundAssignment.Right
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(boundAssignment.Kind)
            End Select

            Dim operatorInfo As BinaryOperatorInfo
            Select Case binaryOperator.Kind
                Case BoundKind.BinaryOperator
                    operatorInfo = GetBinaryOperatorInfo(DirectCast(binaryOperator, BoundBinaryOperator))
                    Return Create(operatorInfo.RightOperand)
                Case BoundKind.UserDefinedBinaryOperator
                    Dim userDefinedOperator = DirectCast(binaryOperator, BoundUserDefinedBinaryOperator)
                    operatorInfo = GetUserDefinedBinaryOperatorInfo(userDefinedOperator)
                    Return GetUserDefinedBinaryOperatorChild(userDefinedOperator, operatorInfo.RightOperand)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(boundAssignment.Kind)
            End Select
        End Function

        Private Function CreateCompoundAssignment(boundAssignment As BoundAssignmentOperator) As ICompoundAssignmentOperation
            Debug.Assert(boundAssignment.LeftOnTheRightOpt IsNot Nothing)
            Dim inConversion = New Conversion(Conversions.Identity)
            Dim outConversion As Conversion = inConversion
            Dim binaryOperator As BoundExpression = Nothing
            Select Case boundAssignment.Right.Kind
                Case BoundKind.Conversion
                    Dim inConversionNode = DirectCast(boundAssignment.Right, BoundConversion)
                    outConversion = CreateConversion(inConversionNode)
                    binaryOperator = GetConversionOperand(inConversionNode)
                Case BoundKind.UserDefinedBinaryOperator, BoundKind.BinaryOperator
                    binaryOperator = boundAssignment.Right
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(boundAssignment.Kind)
            End Select

            Dim operatorInfo As BinaryOperatorInfo
            Select Case binaryOperator.Kind
                Case BoundKind.BinaryOperator
                    operatorInfo = GetBinaryOperatorInfo(DirectCast(binaryOperator, BoundBinaryOperator))
                Case BoundKind.UserDefinedBinaryOperator
                    Dim userDefinedOperator = DirectCast(binaryOperator, BoundUserDefinedBinaryOperator)
                    operatorInfo = GetUserDefinedBinaryOperatorInfo(userDefinedOperator)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(boundAssignment.Kind)
            End Select

            Dim leftOnTheRight As BoundExpression = operatorInfo.LeftOperand
            If leftOnTheRight.Kind = BoundKind.Conversion Then
                Dim outConversionNode = DirectCast(leftOnTheRight, BoundConversion)
                inConversion = CreateConversion(outConversionNode)
                leftOnTheRight = GetConversionOperand(outConversionNode)
            End If

            Debug.Assert(leftOnTheRight Is boundAssignment.LeftOnTheRightOpt)

            Dim target As IOperation = Create(boundAssignment.Left)
            Dim value As IOperation = CreateCompoundAssignmentRightOperand(boundAssignment)
            Dim syntax As SyntaxNode = boundAssignment.Syntax
            Dim type As ITypeSymbol = boundAssignment.Type
            Dim isImplicit As Boolean = boundAssignment.WasCompilerGenerated

            Return New CompoundAssignmentOperation(inConversion, outConversion, operatorInfo.OperatorKind, operatorInfo.IsLifted,
                                                   operatorInfo.IsChecked, operatorInfo.OperatorMethod, constrainedToType:=Nothing, target, value,
                                                   _semanticModel, syntax, type, isImplicit)
        End Function

        Private Structure BinaryOperatorInfo
            Public Sub New(leftOperand As BoundExpression,
                           rightOperand As BoundExpression,
                           binaryOperatorKind As BinaryOperatorKind,
                           operatorMethod As MethodSymbol,
                           isLifted As Boolean,
                           isChecked As Boolean,
                           isCompareText As Boolean)
                Me.LeftOperand = leftOperand
                Me.RightOperand = rightOperand
                Me.OperatorKind = binaryOperatorKind
                Me.OperatorMethod = operatorMethod
                Me.IsLifted = isLifted
                Me.IsChecked = isChecked
                Me.IsCompareText = isCompareText
            End Sub

            Public ReadOnly LeftOperand As BoundExpression
            Public ReadOnly RightOperand As BoundExpression
            Public ReadOnly OperatorKind As BinaryOperatorKind
            Public ReadOnly OperatorMethod As MethodSymbol
            Public ReadOnly IsLifted As Boolean
            Public ReadOnly IsChecked As Boolean
            Public ReadOnly IsCompareText As Boolean
        End Structure

        Private Shared Function GetBinaryOperatorInfo(boundBinaryOperator As BoundBinaryOperator) As BinaryOperatorInfo
            Return New BinaryOperatorInfo(
                leftOperand:=boundBinaryOperator.Left,
                rightOperand:=boundBinaryOperator.Right,
                binaryOperatorKind:=Helper.DeriveBinaryOperatorKind(boundBinaryOperator.OperatorKind, boundBinaryOperator.Left),
                operatorMethod:=Nothing,
                isLifted:=(boundBinaryOperator.OperatorKind And VisualBasic.BinaryOperatorKind.Lifted) <> 0,
                isChecked:=boundBinaryOperator.Checked,
                isCompareText:=(boundBinaryOperator.OperatorKind And VisualBasic.BinaryOperatorKind.CompareText) <> 0)
        End Function

        Private Shared Function GetUserDefinedBinaryOperatorInfo(boundUserDefinedBinaryOperator As BoundUserDefinedBinaryOperator) As BinaryOperatorInfo
            Return New BinaryOperatorInfo(
                leftOperand:=GetUserDefinedBinaryOperatorChildBoundNode(boundUserDefinedBinaryOperator, 0),
                rightOperand:=GetUserDefinedBinaryOperatorChildBoundNode(boundUserDefinedBinaryOperator, 1),
                binaryOperatorKind:=Helper.DeriveBinaryOperatorKind(boundUserDefinedBinaryOperator.OperatorKind, leftOpt:=Nothing),
                operatorMethod:=If(boundUserDefinedBinaryOperator.UnderlyingExpression.Kind = BoundKind.Call, boundUserDefinedBinaryOperator.Call.Method, Nothing),
                isLifted:=(boundUserDefinedBinaryOperator.OperatorKind And VisualBasic.BinaryOperatorKind.Lifted) <> 0,
                isChecked:=boundUserDefinedBinaryOperator.Checked,
                isCompareText:=False)
        End Function

        Private Function GetUserDefinedBinaryOperatorChild([operator] As BoundUserDefinedBinaryOperator, child As BoundExpression) As IOperation
            If child IsNot Nothing Then
                Return Create(child)
            End If
            Dim isImplicit As Boolean = [operator].UnderlyingExpression.WasCompilerGenerated
            Return OperationFactory.CreateInvalidOperation(_semanticModel, [operator].UnderlyingExpression.Syntax, ImmutableArray(Of IOperation).Empty, isImplicit)
        End Function

        Private Shared Function GetUserDefinedBinaryOperatorChildBoundNode([operator] As BoundUserDefinedBinaryOperator, index As Integer) As BoundExpression
            If [operator].UnderlyingExpression.Kind = BoundKind.Call Then
                If index = 0 Then
                    Return [operator].Left
                ElseIf index = 1 Then
                    Return [operator].Right
                Else
                    Throw ExceptionUtilities.UnexpectedValue(index)
                End If
            End If

            Return GetChildOfBadExpressionBoundNode([operator].UnderlyingExpression, index)
        End Function

        Friend Function DeriveArguments(boundNode As BoundNode) As ImmutableArray(Of IArgumentOperation)
            Select Case boundNode.Kind
                Case BoundKind.Call
                    Dim boundCall = DirectCast(boundNode, BoundCall)
                    Return DeriveArguments(boundCall.Arguments, boundCall.Method.Parameters, boundCall.DefaultArguments)
                Case BoundKind.ObjectCreationExpression
                    Dim boundCreation = DirectCast(boundNode, BoundObjectCreationExpression)
                    If boundCreation.Arguments.IsDefault Then
                        Return ImmutableArray(Of IArgumentOperation).Empty
                    End If
                    Return If(boundCreation.ConstructorOpt Is Nothing, ImmutableArray(Of IArgumentOperation).Empty, DeriveArguments(boundCreation.Arguments, boundCreation.ConstructorOpt.Parameters, boundCreation.DefaultArguments))
                Case BoundKind.PropertyAccess
                    Dim boundProperty = DirectCast(boundNode, BoundPropertyAccess)
                    Return If(boundProperty.Arguments.Length = 0, ImmutableArray(Of IArgumentOperation).Empty, DeriveArguments(boundProperty.Arguments, boundProperty.PropertySymbol.Parameters, boundProperty.DefaultArguments))
                Case BoundKind.RaiseEventStatement
                    Dim boundRaiseEvent = DirectCast(boundNode, BoundRaiseEventStatement)
                    Return DeriveArguments(DirectCast(boundRaiseEvent.EventInvocation, BoundCall))
                Case BoundKind.Attribute
                    Dim boundAttribute = DirectCast(boundNode, BoundAttribute)
                    Return DeriveArguments(boundAttribute.ConstructorArguments, boundAttribute.Constructor.Parameters, boundAttribute.ConstructorDefaultArguments)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(boundNode.Kind)
            End Select
        End Function

        Friend Function DeriveArguments(boundArguments As ImmutableArray(Of BoundExpression), parameters As ImmutableArray(Of VisualBasic.Symbols.ParameterSymbol), ByRef defaultArguments As BitVector) As ImmutableArray(Of IArgumentOperation)
            Dim argumentsLength As Integer = boundArguments.Length
            Debug.Assert(argumentsLength = parameters.Length)

            Dim arguments As ArrayBuilder(Of IArgumentOperation) = ArrayBuilder(Of IArgumentOperation).GetInstance(argumentsLength)
            For index As Integer = 0 To argumentsLength - 1 Step 1
                arguments.Add(DeriveArgument(index, boundArguments(index), parameters, defaultArguments(index)))
            Next

            Return arguments.ToImmutableAndFree()
        End Function

        Private Function DeriveArgument(
            index As Integer,
            argument As BoundExpression,
            parameters As ImmutableArray(Of VisualBasic.Symbols.ParameterSymbol),
            isDefault As Boolean
        ) As IArgumentOperation
            Dim isImplicit As Boolean = argument.WasCompilerGenerated AndAlso argument.Syntax.Kind <> SyntaxKind.OmittedArgument
            Select Case argument.Kind
                Case BoundKind.ByRefArgumentWithCopyBack
                    Dim byRefArgument = DirectCast(argument, BoundByRefArgumentWithCopyBack)
                    Dim parameter = parameters(index)

                    Return CreateArgumentOperation(
                        ArgumentKind.Explicit,
                        parameter,
                        byRefArgument.OriginalArgument,
                        CreateConversion(byRefArgument.InConversion),
                        CreateConversion(byRefArgument.OutConversion),
                        isImplicit)
                Case Else
                    Dim lastParameterIndex = parameters.Length - 1
                    Dim kind As ArgumentKind = ArgumentKind.Explicit

                    If argument.WasCompilerGenerated Then
                        If isDefault Then
                            kind = ArgumentKind.DefaultValue
                        ElseIf argument.Kind = BoundKind.ArrayCreation AndAlso DirectCast(argument, BoundArrayCreation).IsParamArrayArgument Then
                            kind = ArgumentKind.ParamArray
                        End If
                    End If

                    Return CreateArgumentOperation(
                        kind,
                        parameters(index),
                        argument,
                        New Conversion(Conversions.Identity),
                        New Conversion(Conversions.Identity),
                        isImplicit)
            End Select
        End Function

        Private Function CreateArgumentOperation(
            kind As ArgumentKind,
            parameter As IParameterSymbol,
            valueNode As BoundNode,
            inConversion As Conversion,
            outConversion As Conversion,
            isImplicit As Boolean) As IArgumentOperation

            ' put argument syntax to argument operation
            Dim syntax = If(valueNode.Syntax.Kind = SyntaxKind.OmittedArgument, valueNode.Syntax, TryCast(valueNode.Syntax?.Parent, ArgumentSyntax))
            Dim value = Create(valueNode)

            If syntax Is Nothing Then
                syntax = value.Syntax
                isImplicit = True
            End If

            Return New ArgumentOperation(
                kind,
                parameter,
                value,
                inConversion,
                outConversion,
                _semanticModel,
                syntax,
                isImplicit)
        End Function

        Friend Function CreateReceiverOperation(node As BoundNode, symbol As ISymbol) As IOperation
            If node Is Nothing OrElse node.Kind = BoundKind.TypeExpression Then
                Return Nothing
            End If

            If symbol IsNot Nothing AndAlso
               node.WasCompilerGenerated AndAlso
               symbol.IsStatic AndAlso
               (node.Kind = BoundKind.MeReference OrElse
                node.Kind = BoundKind.WithLValueExpressionPlaceholder OrElse
                node.Kind = BoundKind.WithRValueExpressionPlaceholder) Then
                Return Nothing
            End If

            Return Create(node)
        End Function

        Private Function GetChildOfBadExpression(parent As BoundNode, index As Integer) As IOperation
            Dim child = Create(GetChildOfBadExpressionBoundNode(parent, index))
            If child IsNot Nothing Then
                Return child
            End If
            Dim isImplicit As Boolean = parent.WasCompilerGenerated
            Return OperationFactory.CreateInvalidOperation(_semanticModel, parent.Syntax, ImmutableArray(Of IOperation).Empty, isImplicit)
        End Function

        Private Shared Function GetChildOfBadExpressionBoundNode(parent As BoundNode, index As Integer) As BoundExpression
            Dim badParent As BoundBadExpression = TryCast(parent, BoundBadExpression)
            If badParent?.ChildBoundNodes.Length > index Then
                Return badParent.ChildBoundNodes(index)
            End If

            Return Nothing
        End Function

        Friend Function GetAnonymousTypeCreationInitializers(expression As BoundAnonymousTypeCreationExpression) As ImmutableArray(Of IOperation)
            ' For error cases and non-assignment initializers, the binder generates only the argument.
            Debug.Assert(expression.Arguments.Length >= expression.Declarations.Length)

            Dim properties = DirectCast(expression.Type, AnonymousTypePublicSymbol).Properties
            Debug.Assert(properties.Length = expression.Arguments.Length)

            Dim builder = ArrayBuilder(Of IOperation).GetInstance(expression.Arguments.Length)
            Dim currentDeclarationIndex = 0
            For i As Integer = 0 To expression.Arguments.Length - 1
                Dim value As IOperation = Create(expression.Arguments(i))

                Dim target As IOperation
                Dim isImplicitAssignment As Boolean

                ' Find matching declaration for the current argument
                If currentDeclarationIndex >= expression.Declarations.Length OrElse
                   i <> expression.Declarations(currentDeclarationIndex).PropertyIndex Then
                    ' No matching declaration, synthesize a property reference with an implicit receiver to be assigned.
                    Dim [property] As IPropertySymbol = properties(i)
                    Dim instance As IInstanceReferenceOperation = CreateAnonymousTypePropertyAccessImplicitReceiverOperation([property], expression.Syntax)
                    target = New PropertyReferenceOperation(
                        [property], constrainedToType:=Nothing,
                        ImmutableArray(Of IArgumentOperation).Empty,
                        instance,
                        _semanticModel,
                        value.Syntax,
                        [property].Type,
                        isImplicit:=True)
                    isImplicitAssignment = True
                Else
                    Debug.Assert(i = expression.Declarations(currentDeclarationIndex).PropertyIndex)
                    target = CreateBoundAnonymousTypePropertyAccessOperation(expression.Declarations(currentDeclarationIndex))
                    currentDeclarationIndex = currentDeclarationIndex + 1
                    isImplicitAssignment = expression.WasCompilerGenerated
                End If

                Dim isRef As Boolean = False
                Dim syntax As SyntaxNode = If(value.Syntax?.Parent, expression.Syntax)
                Dim type As ITypeSymbol = target.Type
                Dim constantValue As ConstantValue = value.GetConstantValue()
                Dim assignment = New SimpleAssignmentOperation(isRef, target, value, _semanticModel, syntax, type, constantValue, isImplicitAssignment)
                builder.Add(assignment)
            Next i

            Debug.Assert(currentDeclarationIndex = expression.Declarations.Length)
            Return builder.ToImmutableAndFree()
        End Function

        Private Shared Function GetSingleValueCaseClauseValue(clause As BoundSingleValueCaseClause) As BoundExpression
            Return GetCaseClauseValue(clause.ValueOpt, clause.ConditionOpt)
        End Function

        Friend Shared Function GetCaseClauseValue(valueOpt As BoundExpression, conditionOpt As BoundExpression) As BoundExpression
            If valueOpt IsNot Nothing Then
                Return valueOpt
            End If

            Select Case conditionOpt.Kind
                Case BoundKind.BinaryOperator
                    Dim binaryOp As BoundBinaryOperator = DirectCast(conditionOpt, BoundBinaryOperator)
                    Return binaryOp.Right

                Case BoundKind.UserDefinedBinaryOperator
                    Dim binaryOp As BoundUserDefinedBinaryOperator = DirectCast(conditionOpt, BoundUserDefinedBinaryOperator)
                    Return GetUserDefinedBinaryOperatorChildBoundNode(binaryOp, 1)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(conditionOpt.Kind)
            End Select
        End Function

        Friend Function GetVariableDeclarationStatementVariables(declarations As ImmutableArray(Of BoundLocalDeclarationBase)) As ImmutableArray(Of IVariableDeclarationOperation)
            ' Group the declarations by their VariableDeclaratorSyntaxes. The issue we're compensating for here is that the
            ' the declarations that are BoundLocalDeclaration nodes have a ModifiedIdentifierSyntax as their syntax nodes,
            ' not a VariableDeclaratorSyntax. We want to group BoundLocalDeclarations by their parent VariableDeclaratorSyntax
            ' nodes, and deduplicate based on that. As an example:
            '
            ' Dim x, y = 1
            '
            ' This is an error scenario, but if we just use the BoundLocalDeclaration.Syntax.Parent directly, without deduplicating,
            ' we'll end up with two IVariableDeclarators that have the same syntax node. So, we group by VariableDeclaratorSyntax
            ' to put x and y in the same IMultiVariableDeclaration
            Dim groupedDeclarations = declarations.GroupBy(Function(declaration)
                                                               If declaration.Kind = BoundKind.LocalDeclaration AndAlso
                                                                  declaration.Syntax.IsKind(SyntaxKind.ModifiedIdentifier) Then
                                                                   Debug.Assert(declaration.Syntax.Parent.IsKind(SyntaxKind.VariableDeclarator))
                                                                   Return declaration.Syntax.Parent
                                                               Else
                                                                   Return declaration.Syntax
                                                               End If
                                                           End Function)

            Dim builder = ArrayBuilder(Of IVariableDeclarationOperation).GetInstance()
            For Each declarationGroup In groupedDeclarations
                Dim first = declarationGroup.First()
                Dim declarators As ImmutableArray(Of IVariableDeclaratorOperation) = Nothing
                Dim initializer As IVariableInitializerOperation = Nothing
                If first.Kind = BoundKind.LocalDeclaration Then
                    declarators = declarationGroup.Cast(Of BoundLocalDeclaration).SelectAsArray(AddressOf GetVariableDeclarator)

                    ' The initializer we use for this group is the initializer attached to the last declaration in this declarator, as that's
                    ' where it will be parsed in an error case.
                    ' Initializer is only created if it's not the array initializer for the variable. That initializer is the initializer
                    ' of the VariableDeclarator child.
                    Dim last = DirectCast(declarationGroup.Last(), BoundLocalDeclaration)
                    If last.DeclarationInitializerOpt IsNot Nothing Then
                        Debug.Assert(last.Syntax.IsKind(SyntaxKind.ModifiedIdentifier))
                        Dim declaratorSyntax = DirectCast(last.Syntax.Parent, VariableDeclaratorSyntax)
                        Dim initializerSyntax As SyntaxNode = declaratorSyntax.Initializer

                        ' As New clauses with a single variable are bound as BoundLocalDeclarations, so adjust appropriately
                        Dim isImplicit As Boolean = False
                        If last.InitializedByAsNew Then
                            initializerSyntax = declaratorSyntax.AsClause
                        ElseIf initializerSyntax Is Nothing Then
                            ' There is no explicit syntax for the initializer, so we use the initializerValue's syntax and mark the operation as implicit.
                            initializerSyntax = last.InitializerOpt.Syntax
                            isImplicit = True
                        End If
                        Debug.Assert(last.InitializerOpt IsNot Nothing)
                        Dim value = Create(last.InitializerOpt)
                        initializer = New VariableInitializerOperation(locals:=ImmutableArray(Of ILocalSymbol).Empty, value, _semanticModel, initializerSyntax, isImplicit)
                    End If
                Else
                    Dim asNewDeclarations = DirectCast(first, BoundAsNewLocalDeclarations)
                    declarators = asNewDeclarations.LocalDeclarations.SelectAsArray(AddressOf GetVariableDeclarator)
                    Dim initializerSyntax As AsClauseSyntax = DirectCast(asNewDeclarations.Syntax, VariableDeclaratorSyntax).AsClause
                    Dim initializerValue As IOperation = Create(asNewDeclarations.Initializer)
                    Debug.Assert(asNewDeclarations.Initializer IsNot Nothing)
                    Dim value = Create(asNewDeclarations.Initializer)
                    initializer = New VariableInitializerOperation(locals:=ImmutableArray(Of ILocalSymbol).Empty, value, _semanticModel, initializerSyntax, isImplicit:=False)
                End If

                builder.Add(New VariableDeclarationOperation(declarators,
                                                             initializer,
                                                             ImmutableArray(Of IOperation).Empty,
                                                             _semanticModel,
                                                             declarationGroup.Key,
                                                             isImplicit:=False))
            Next

            Return builder.ToImmutableAndFree()
        End Function

        Private Function GetVariableDeclarator(boundLocalDeclaration As BoundLocalDeclaration) As IVariableDeclaratorOperation
            Dim initializer As IVariableInitializerOperation = Nothing
            If boundLocalDeclaration.IdentifierInitializerOpt IsNot Nothing Then
                Dim syntax = boundLocalDeclaration.Syntax
                Dim initializerValue As IOperation = Create(boundLocalDeclaration.IdentifierInitializerOpt)
                initializer = New VariableInitializerOperation(locals:=ImmutableArray(Of ILocalSymbol).Empty, initializerValue, _semanticModel, syntax, isImplicit:=True)
            End If

            Dim ignoredArguments = ImmutableArray(Of IOperation).Empty

            Return New VariableDeclaratorOperation(boundLocalDeclaration.LocalSymbol, initializer, ignoredArguments, _semanticModel, boundLocalDeclaration.Syntax, isImplicit:=boundLocalDeclaration.WasCompilerGenerated)
        End Function

        Private Function GetUsingStatementDeclaration(resourceList As ImmutableArray(Of BoundLocalDeclarationBase), syntax As SyntaxNode) As IVariableDeclarationGroupOperation
            Return New VariableDeclarationGroupOperation(
                            GetVariableDeclarationStatementVariables(resourceList),
                            _semanticModel,
                            syntax,
                            isImplicit:=False) ' Declaration is always explicit
        End Function

        Friend Function GetAddRemoveHandlerStatementExpression(statement As BoundAddRemoveHandlerStatement) As IOperation
            Dim eventReference As IOperation = Create(statement.EventAccess)
            Dim handlerValue As IOperation = Create(statement.Handler)
            Dim adds = statement.Kind = BoundKind.AddHandlerStatement
            Return New EventAssignmentOperation(eventReference, handlerValue, adds, _semanticModel, statement.Syntax, type:=Nothing, isImplicit:=True)
        End Function

#Region "Conversions"

        ''' <summary>
        ''' Creates the Lazy IOperation from a delegate creation operand or a bound conversion operand, handling when the conversion
        ''' is actually a delegate creation.
        ''' </summary>
        Private Function GetConversionInfo(boundConversion As BoundConversionOrCast
                                          ) As (Operation As IOperation, Conversion As Conversion, IsDelegateCreation As Boolean)
            Dim conversion = CreateConversion(boundConversion)
            Dim boundOperand = GetConversionOperand(boundConversion)
            If conversion.IsIdentity AndAlso boundConversion.ExplicitCastInCode Then
                Dim adjustedInfo = TryGetAdjustedConversionInfo(boundConversion, boundOperand)

                If adjustedInfo.Operation IsNot Nothing Then
                    Return adjustedInfo
                End If
            End If

            If IsDelegateCreation(boundConversion.Syntax, boundOperand, boundConversion.Type) Then
                Return (CreateDelegateCreationConversionOperand(boundOperand),
                    conversion, IsDelegateCreation:=True)
            Else
                Return (Create(boundOperand), conversion, IsDelegateCreation:=False)
            End If
        End Function

        Private Function TryGetAdjustedConversionInfo(topLevelConversion As BoundConversionOrCast, boundOperand As BoundExpression
                                                     ) As (Operation As IOperation, Conversion As Conversion, IsDelegateCreation As Boolean)
            ' Dig through the bound tree to see if this is an artificial nested conversion. If so, let's erase the nested conversion.
            ' Artificial conversions are added on top of BoundConvertedTupleLiteral in Binder.ReclassifyTupleLiteral, or in
            ' ReclassifyUnboundLambdaExpression and the like. We need to use conversion information from the nested conversion
            ' because that is where the real conversion information is stored.
            If boundOperand.Kind = BoundKind.Parenthesized Then
                Dim adjustedInfo = TryGetAdjustedConversionInfo(topLevelConversion, DirectCast(boundOperand, BoundParenthesized).Expression)
                If adjustedInfo.Operation IsNot Nothing Then
                    Return (Operation:=New ParenthesizedOperation(adjustedInfo.Operation,
                                                                   _semanticModel,
                                                                   boundOperand.Syntax,
                                                                   adjustedInfo.Operation.Type,
                                                                   boundOperand.ConstantValueOpt,
                                                                   boundOperand.WasCompilerGenerated),
                            adjustedInfo.Conversion,
                            adjustedInfo.IsDelegateCreation)
                End If
            ElseIf boundOperand.Kind = topLevelConversion.Kind Then
                Dim nestedConversion = DirectCast(boundOperand, BoundConversionOrCast)
                Dim nestedOperand As BoundExpression = GetConversionOperand(nestedConversion)

                If nestedConversion.Syntax Is nestedOperand.Syntax AndAlso
                   Not TypeSymbol.Equals(nestedConversion.Type, nestedOperand.Type, TypeCompareKind.ConsiderEverything) AndAlso
                   nestedConversion.ExplicitCastInCode AndAlso
                   TypeSymbol.Equals(topLevelConversion.Type, nestedConversion.Type, TypeCompareKind.ConsiderEverything) Then

                    Return GetConversionInfo(nestedConversion)
                End If
            ElseIf boundOperand.Syntax.IsKind(SyntaxKind.AddressOfExpression) AndAlso
                   TypeSymbol.Equals(topLevelConversion.Type, boundOperand.Type, TypeCompareKind.ConsiderEverything) AndAlso
                   IsDelegateCreation(topLevelConversion.Syntax, boundOperand, boundOperand.Type) Then

                Return (CreateDelegateCreationConversionOperand(boundOperand), Conversion:=Nothing, IsDelegateCreation:=True)
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Gets the operand from a BoundConversion, compensating for if the conversion is a user-defined conversion
        ''' </summary>
        Private Shared Function GetConversionOperand(boundConversion As BoundConversionOrCast) As BoundExpression
            If (boundConversion.ConversionKind And ConversionKind.UserDefined) = ConversionKind.UserDefined Then
                Dim userDefinedConversion = DirectCast(boundConversion.Operand, BoundUserDefinedConversion)
                Return userDefinedConversion.Operand
            Else
                Return boundConversion.Operand
            End If
        End Function

        Private Function CreateDelegateCreationConversionOperand(operand As BoundExpression) As IOperation
            If operand.Kind = BoundKind.DelegateCreationExpression Then
                ' If the child is a BoundDelegateCreationExpression, we don't want to generate a nested IDelegateCreationExpression.
                ' So, the operand for the conversion will be the child of the BoundDelegateCreationExpression.
                ' We see this in this syntax: Dim x = New Action(AddressOf M2)
                ' This should be semantically equivalent to: Dim x = AddressOf M2
                ' However, if we didn't fix this up, we would have nested IDelegateCreationExpressions here for the former case.
                Return CreateBoundDelegateCreationExpressionChildOperation(DirectCast(operand, BoundDelegateCreationExpression))
            Else
                Return Create(operand)
            End If
        End Function

        Private Shared Function CreateConversion(expression As BoundExpression) As Conversion
            If expression.Kind = BoundKind.Conversion Then
                Dim conversion = DirectCast(expression, BoundConversion)
                Dim conversionKind = conversion.ConversionKind
                Dim method As MethodSymbol = Nothing
                If conversionKind.HasFlag(VisualBasic.ConversionKind.UserDefined) AndAlso conversion.Operand.Kind = BoundKind.UserDefinedConversion Then
                    method = DirectCast(conversion.Operand, BoundUserDefinedConversion).Call.Method
                End If
                Return New Conversion(KeyValuePairUtil.Create(conversionKind, method))
            ElseIf expression.Kind = BoundKind.TryCast OrElse expression.Kind = BoundKind.DirectCast Then
                Return New Conversion(KeyValuePairUtil.Create(Of ConversionKind, MethodSymbol)(DirectCast(expression, BoundConversionOrCast).ConversionKind, Nothing))
            End If
            Return New Conversion(Conversions.Identity)
        End Function

        Private Shared Function IsDelegateCreation(conversionSyntax As SyntaxNode, operand As BoundNode, targetType As TypeSymbol) As Boolean
            If Not targetType.IsDelegateType() Then
                Return False
            End If

            ' Any of the explicit cast types, as well as New DelegateType(AddressOf Method)
            ' Additionally, AddressOf, if the child AddressOf is the same SyntaxNode (ie, an implicit delegate creation)
            ' In the case of AddressOf, the operand can be a BoundDelegateCreationExpression, a BoundAddressOfOperator, or
            ' a BoundBadExpression. For simplicity, we just do a syntax check to make sure it's an AddressOfExpression so
            ' we don't have to compare against all 3 BoundKinds
            Dim validAddressOfConversionSyntax = operand.Syntax.Kind() = SyntaxKind.AddressOfExpression AndAlso
                                                 (conversionSyntax.Kind() = SyntaxKind.CTypeExpression OrElse
                                                  conversionSyntax.Kind() = SyntaxKind.DirectCastExpression OrElse
                                                  conversionSyntax.Kind() = SyntaxKind.TryCastExpression OrElse
                                                  conversionSyntax.Kind() = SyntaxKind.ObjectCreationExpression OrElse
                                                  (conversionSyntax.Kind() = SyntaxKind.AddressOfExpression AndAlso
                                                   conversionSyntax Is operand.Syntax))

            Dim validLambdaConversionNode = operand.Kind = BoundKind.Lambda OrElse
                                              operand.Kind = BoundKind.QueryLambda OrElse
                                              operand.Kind = BoundKind.UnboundLambda

            Return validAddressOfConversionSyntax OrElse validLambdaConversionNode
        End Function

#End Region

        Friend Class Helper
            Friend Shared Function DeriveUnaryOperatorKind(operatorKind As VisualBasic.UnaryOperatorKind) As UnaryOperatorKind
                Select Case operatorKind And VisualBasic.UnaryOperatorKind.OpMask
                    Case VisualBasic.UnaryOperatorKind.Plus
                        Return UnaryOperatorKind.Plus
                    Case VisualBasic.UnaryOperatorKind.Minus
                        Return UnaryOperatorKind.Minus
                    Case VisualBasic.UnaryOperatorKind.Not
                        Return UnaryOperatorKind.Not
                    Case VisualBasic.UnaryOperatorKind.IsTrue
                        Return UnaryOperatorKind.True
                    Case VisualBasic.UnaryOperatorKind.IsFalse
                        Return UnaryOperatorKind.False
                    Case Else
                        Return UnaryOperatorKind.None
                End Select
            End Function

            Friend Shared Function DeriveBinaryOperatorKind(operatorKind As VisualBasic.BinaryOperatorKind, leftOpt As BoundExpression) As BinaryOperatorKind
                Select Case operatorKind And VisualBasic.BinaryOperatorKind.OpMask
                    Case VisualBasic.BinaryOperatorKind.Add
                        Return BinaryOperatorKind.Add
                    Case VisualBasic.BinaryOperatorKind.Subtract
                        Return BinaryOperatorKind.Subtract
                    Case VisualBasic.BinaryOperatorKind.Multiply
                        Return BinaryOperatorKind.Multiply
                    Case VisualBasic.BinaryOperatorKind.Divide
                        Return BinaryOperatorKind.Divide
                    Case VisualBasic.BinaryOperatorKind.IntegerDivide
                        Return BinaryOperatorKind.IntegerDivide
                    Case VisualBasic.BinaryOperatorKind.Modulo
                        Return BinaryOperatorKind.Remainder
                    Case VisualBasic.BinaryOperatorKind.And
                        Return BinaryOperatorKind.And
                    Case VisualBasic.BinaryOperatorKind.Or
                        Return BinaryOperatorKind.Or
                    Case VisualBasic.BinaryOperatorKind.Xor
                        Return BinaryOperatorKind.ExclusiveOr
                    Case VisualBasic.BinaryOperatorKind.AndAlso
                        Return BinaryOperatorKind.ConditionalAnd
                    Case VisualBasic.BinaryOperatorKind.OrElse
                        Return BinaryOperatorKind.ConditionalOr
                    Case VisualBasic.BinaryOperatorKind.LeftShift
                        Return BinaryOperatorKind.LeftShift
                    Case VisualBasic.BinaryOperatorKind.RightShift
                        Return BinaryOperatorKind.RightShift
                    Case VisualBasic.BinaryOperatorKind.LessThan
                        Return BinaryOperatorKind.LessThan
                    Case VisualBasic.BinaryOperatorKind.LessThanOrEqual
                        Return BinaryOperatorKind.LessThanOrEqual
                    Case VisualBasic.BinaryOperatorKind.Equals
                        Return If(leftOpt?.Type?.SpecialType = SpecialType.System_Object, BinaryOperatorKind.ObjectValueEquals, BinaryOperatorKind.Equals)
                    Case VisualBasic.BinaryOperatorKind.NotEquals
                        Return If(leftOpt?.Type?.SpecialType = SpecialType.System_Object, BinaryOperatorKind.ObjectValueNotEquals, BinaryOperatorKind.NotEquals)
                    Case VisualBasic.BinaryOperatorKind.Is
                        Return BinaryOperatorKind.Equals
                    Case VisualBasic.BinaryOperatorKind.IsNot
                        Return BinaryOperatorKind.NotEquals
                    Case VisualBasic.BinaryOperatorKind.GreaterThanOrEqual
                        Return BinaryOperatorKind.GreaterThanOrEqual
                    Case VisualBasic.BinaryOperatorKind.GreaterThan
                        Return BinaryOperatorKind.GreaterThan
                    Case VisualBasic.BinaryOperatorKind.Power
                        Return BinaryOperatorKind.Power
                    Case VisualBasic.BinaryOperatorKind.Like
                        Return BinaryOperatorKind.Like
                    Case VisualBasic.BinaryOperatorKind.Concatenate
                        Return BinaryOperatorKind.Concatenate
                    Case Else
                        Return BinaryOperatorKind.None
                End Select
            End Function
        End Class
    End Class
End Namespace
