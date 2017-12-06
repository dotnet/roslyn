' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Operations
    Partial Friend NotInheritable Class VisualBasicOperationFactory
        Private Shared Function ConvertToOptional(value As ConstantValue) As [Optional](Of Object)
            Return If(value Is Nothing OrElse value.IsBad, New [Optional](Of Object)(), New [Optional](Of Object)(value.Value))
        End Function

        Private Shared Function IsMidStatement(node As BoundNode) As Boolean
            If node.Kind = BoundKind.Conversion Then
                node = DirectCast(node, BoundConversion).Operand

                If node.Kind = BoundKind.UserDefinedConversion Then
                    node = DirectCast(node, BoundUserDefinedConversion).Operand
                End If
            End If

            Return node.Kind = BoundKind.MidResult
        End Function

        Private Function CreateCompoundAssignment(boundAssignment As BoundAssignmentOperator) As ICompoundAssignmentOperation
            Debug.Assert(boundAssignment.LeftOnTheRightOpt IsNot Nothing)
            Dim binaryOperator As BoundExpression = Nothing
            Dim inConversion = New Conversion(Conversions.Identity)
            Dim outConversion As Conversion = inConversion
            Select Case boundAssignment.Right.Kind
                Case BoundKind.Conversion
                    Dim inConversionNode = DirectCast(boundAssignment.Right, BoundConversion)
                    Dim conversionInfo = GetConversionInfo(inConversionNode)
                    binaryOperator = conversionInfo.Operand
                    outConversion = CreateConversion(inConversionNode)
                Case BoundKind.UserDefinedBinaryOperator, BoundKind.BinaryOperator
                    binaryOperator = boundAssignment.Right
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(boundAssignment.Kind)
            End Select

            Dim operatorInfo As BinaryOperatorInfo
            Dim rightOperand As Lazy(Of IOperation)
            Select Case binaryOperator.Kind
                Case BoundKind.BinaryOperator
                    operatorInfo = GetBinaryOperatorInfo(DirectCast(binaryOperator, BoundBinaryOperator))
                    rightOperand = New Lazy(Of IOperation)(Function() Create(operatorInfo.RightOperand))
                Case BoundKind.UserDefinedBinaryOperator
                    Dim userDefinedOperator = DirectCast(binaryOperator, BoundUserDefinedBinaryOperator)
                    operatorInfo = GetUserDefinedBinaryOperatorInfo(userDefinedOperator)
                    rightOperand = New Lazy(Of IOperation)(Function() GetUserDefinedBinaryOperatorChild(userDefinedOperator, operatorInfo.RightOperand))
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(boundAssignment.Kind)
            End Select

            Dim leftOnTheRight As BoundExpression = operatorInfo.LeftOperand
            If leftOnTheRight.Kind = BoundKind.Conversion Then
                Dim outConversionNode = DirectCast(leftOnTheRight, BoundConversion)
                Dim conversionInfo = GetConversionInfo(outConversionNode)
                inConversion = CreateConversion(outConversionNode)
                leftOnTheRight = conversionInfo.Operand
            End If

            Debug.Assert(leftOnTheRight Is boundAssignment.LeftOnTheRightOpt)

            Dim leftOperand As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundAssignment.Left))
            Dim syntax As SyntaxNode = boundAssignment.Syntax
            Dim type As ITypeSymbol = boundAssignment.Type
            Dim constantValue As [Optional](Of Object) = ConvertToOptional(boundAssignment.ConstantValueOpt)
            Dim isImplicit As Boolean = boundAssignment.WasCompilerGenerated

            Return New LazyVisualBasicCompoundAssignmentOperation(leftOperand, rightOperand, inConversion, outConversion, operatorInfo.OperatorKind,
                                                                  operatorInfo.IsLifted, operatorInfo.IsChecked, operatorInfo.OperatorMethod,
                                                                  _semanticModel, syntax, type, constantValue, isImplicit)
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
            Return OperationFactory.CreateInvalidExpression(_semanticModel, [operator].UnderlyingExpression.Syntax, ImmutableArray(Of IOperation).Empty, isImplicit)
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

        Friend Function DeriveArguments(boundArguments As ImmutableArray(Of BoundExpression), parameters As ImmutableArray(Of VisualBasic.Symbols.ParameterSymbol), invocationWasCompilerGenerated As Boolean) As ImmutableArray(Of IArgumentOperation)
            Dim argumentsLength As Integer = boundArguments.Length
            Debug.Assert(argumentsLength = parameters.Length)

            Dim arguments As ArrayBuilder(Of IArgumentOperation) = ArrayBuilder(Of IArgumentOperation).GetInstance(argumentsLength)
            For index As Integer = 0 To argumentsLength - 1 Step 1
                arguments.Add(DeriveArgument(index, boundArguments(index), parameters, invocationWasCompilerGenerated))
            Next

            Return arguments.ToImmutableAndFree()
        End Function

        Private Shared Function CreateConversion(expression As BoundExpression) As Conversion
            If expression.Kind = BoundKind.Conversion Then
                Dim conversion = DirectCast(expression, BoundConversion)
                Dim conversionKind = conversion.ConversionKind
                Dim method As MethodSymbol = Nothing
                If conversionKind.HasFlag(VisualBasic.ConversionKind.UserDefined) AndAlso conversion.Operand.Kind = BoundKind.UserDefinedConversion Then
                    method = DirectCast(conversion.Operand, BoundUserDefinedConversion).Call.Method
                End If
                Return New Conversion(KeyValuePair.Create(conversionKind, method))
            ElseIf expression.Kind = BoundKind.TryCast OrElse expression.Kind = BoundKind.DirectCast Then
                Return New Conversion(KeyValuePair.Create(Of ConversionKind, MethodSymbol)(DirectCast(expression, BoundConversionOrCast).ConversionKind, Nothing))
            End If
            Return New Conversion(Conversions.Identity)
        End Function

        Private Function DeriveArgument(
            index As Integer,
            argument As BoundExpression,
            parameters As ImmutableArray(Of VisualBasic.Symbols.ParameterSymbol),
            invocationWasCompilerGenerated As Boolean
        ) As IArgumentOperation
            Dim isImplicit As Boolean = argument.WasCompilerGenerated AndAlso argument.Syntax.Kind <> SyntaxKind.OmittedArgument
            Select Case argument.Kind
                Case BoundKind.ByRefArgumentWithCopyBack
                    Dim byRefArgument = DirectCast(argument, BoundByRefArgumentWithCopyBack)
                    Dim parameter = parameters(index)
                    Dim value = Create(byRefArgument.OriginalArgument)

                    Return CreateArgumentOperation(
                        ArgumentKind.Explicit,
                        parameter,
                        value,
                        CreateConversion(byRefArgument.InConversion),
                        CreateConversion(byRefArgument.OutConversion),
                        isImplicit)
                Case Else
                    Dim lastParameterIndex = parameters.Length - 1
                    Dim kind As ArgumentKind = ArgumentKind.Explicit

                    If argument.WasCompilerGenerated AndAlso Not invocationWasCompilerGenerated Then

                        If index = lastParameterIndex AndAlso ParameterIsParamArray(parameters(lastParameterIndex)) Then
                            ' TODO: figure out if this is true:
                            '       a compiler generated argument for a ParamArray parameter is created iff
                            '       a list of arguments (including 0 argument) is provided for ParamArray parameter in source
                            '       https://github.com/dotnet/roslyn/issues/18550
                            If argument.Kind = BoundKind.ArrayCreation Then
                                kind = ArgumentKind.ParamArray
                            End If
                        Else
                            ' TODO: figure our if this is true:
                            '       a compiler generated argument for an Optional parameter is created iff
                            '       the argument is omitted from the source
                            '       https://github.com/dotnet/roslyn/issues/18550
                            kind = ArgumentKind.DefaultValue
                        End If
                    End If

                    Dim value = Create(argument)
                    Return CreateArgumentOperation(
                        kind,
                        parameters(index),
                        value,
                        New Conversion(Conversions.Identity),
                        New Conversion(Conversions.Identity),
                        isImplicit)
            End Select
        End Function

        Private Function CreateArgumentOperation(
            kind As ArgumentKind,
            parameter As IParameterSymbol,
            value As IOperation,
            inConversion As Conversion,
            outConversion As Conversion,
            isImplicit As Boolean) As IArgumentOperation

            ' put argument syntax to argument operation
            Dim argument = If(value.Syntax.Kind = SyntaxKind.OmittedArgument, value.Syntax, TryCast(value.Syntax?.Parent, ArgumentSyntax))

            ' if argument syntax doesn't exist, then this operation is implicit
            Return New VisualBasicArgument(
                kind,
                parameter,
                value,
                inConversion:=inConversion,
                outConversion:=outConversion,
                semanticModel:=_semanticModel,
                syntax:=If(argument, value.Syntax),
                constantValue:=Nothing,
                isImplicit:=isImplicit OrElse argument Is Nothing)
        End Function

        Private Function CreateReceiverOperation(node As BoundNode, symbol As ISymbol) As Lazy(Of IOperation)
            If node Is Nothing OrElse node.Kind = BoundKind.TypeExpression Then
                Return OperationFactory.NullOperation
            End If

            If symbol IsNot Nothing AndAlso
               node.WasCompilerGenerated AndAlso
               symbol.IsStatic AndAlso
               (node.Kind = BoundKind.MeReference OrElse
                node.Kind = BoundKind.WithLValueExpressionPlaceholder OrElse
                node.Kind = BoundKind.WithRValueExpressionPlaceholder) Then
                Return OperationFactory.NullOperation
            End If

            Return New Lazy(Of IOperation)(Function() Create(node))
        End Function

        Private Shared Function ParameterIsParamArray(parameter As VisualBasic.Symbols.ParameterSymbol) As Boolean
            Return If(parameter.IsParamArray AndAlso parameter.Type.Kind = SymbolKind.ArrayType, DirectCast(parameter.Type, VisualBasic.Symbols.ArrayTypeSymbol).IsSZArray, False)
        End Function

        Private Function GetChildOfBadExpression(parent As BoundNode, index As Integer) As IOperation
            Dim child = Create(GetChildOfBadExpressionBoundNode(parent, index))
            If child IsNot Nothing Then
                Return child
            End If
            Dim isImplicit As Boolean = parent.WasCompilerGenerated
            Return OperationFactory.CreateInvalidExpression(_semanticModel, parent.Syntax, ImmutableArray(Of IOperation).Empty, isImplicit)
        End Function

        Private Shared Function GetChildOfBadExpressionBoundNode(parent As BoundNode, index As Integer) As BoundExpression
            Dim badParent As BoundBadExpression = TryCast(parent, BoundBadExpression)
            If badParent?.ChildBoundNodes.Length > index Then
                Return badParent.ChildBoundNodes(index)
            End If

            Return Nothing
        End Function

        Private Function GetObjectCreationInitializers(expression As BoundObjectCreationExpression) As ImmutableArray(Of IOperation)
            Return If(expression.InitializerOpt IsNot Nothing, expression.InitializerOpt.Initializers.SelectAsArray(Function(n) Create(n)), ImmutableArray(Of IOperation).Empty)
        End Function

        Private Function GetAnonymousTypeCreationInitializers(expression As BoundAnonymousTypeCreationExpression) As ImmutableArray(Of IOperation)
            Debug.Assert(expression.Arguments.Length >= expression.Declarations.Length)

            Dim builder = ArrayBuilder(Of IOperation).GetInstance(expression.Arguments.Length)
            For i As Integer = 0 To expression.Arguments.Length - 1
                Dim value As IOperation = Create(expression.Arguments(i))
                If i >= expression.Declarations.Length Then
                    builder.Add(value)
                    Continue For
                End If

                Dim isRef As Boolean = False
                Dim target As IOperation = Create(expression.Declarations(i))
                Dim syntax As SyntaxNode = If(value.Syntax?.Parent, expression.Syntax)
                Dim type As ITypeSymbol = target.Type
                Dim constantValue As [Optional](Of Object) = value.ConstantValue
                Dim assignment = New SimpleAssignmentExpression(target, isRef, value, _semanticModel, syntax, type, constantValue, isImplicit:=expression.WasCompilerGenerated)
                builder.Add(assignment)
            Next i

            Return builder.ToImmutableAndFree()
        End Function

        Private Shared Function GetSingleValueCaseClauseValue(clause As BoundSimpleCaseClause) As BoundExpression
            If clause.ValueOpt IsNot Nothing Then
                Return clause.ValueOpt
            End If

            If clause.ConditionOpt IsNot Nothing AndAlso clause.ConditionOpt.Kind = BoundKind.BinaryOperator Then
                Dim value As BoundBinaryOperator = DirectCast(clause.ConditionOpt, BoundBinaryOperator)
                If value.OperatorKind = VisualBasic.BinaryOperatorKind.Equals Then
                    Return value.Right
                End If
            End If

            Return Nothing
        End Function

        Private Shared Function GetRelationalCaseClauseValue(clause As BoundRelationalCaseClause) As BoundExpression
            If clause.OperandOpt IsNot Nothing Then
                Return clause.OperandOpt
            End If

            If clause.ConditionOpt?.Kind = BoundKind.BinaryOperator Then
                Return DirectCast(clause.ConditionOpt, BoundBinaryOperator).Right
            End If

            Return Nothing
        End Function

        Private Function GetVariableDeclarationStatementVariables(declarations As ImmutableArray(Of BoundLocalDeclarationBase)) As ImmutableArray(Of IVariableDeclarationOperation)
            ' Group the declarations by their VariableDeclaratorSyntaxes. The issue we're compensating for here is that the
            ' the declarations that are BoundLocalDeclaration nodes have a ModifiedIdentifierSyntax as their syntax nodes,
            ' not a VariableDeclaratorSyntax. We want to group BoundLocalDeclarations by their parent VariableDeclaratorSyntax
            ' nodes, and deduplicate based on that. As an example:
            '
            ' Dim x, y = 1
            '
            ' This is an error scenario, but if we just use the BoundLocalDeclaration.Syntax.Parent directly, without deduplicating,
            ' we'll end up with two IVariableDeclarators that have the same syntax node. So, we group by VariableDeclaratorSyntax
            ' to put x and y in the same IMutliVariableDeclaration
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
                        Dim initializerValue As IOperation = Create(last.InitializerOpt)
                        Dim declaratorSyntax = DirectCast(last.Syntax.Parent, VariableDeclaratorSyntax)
                        Dim initializerSyntax As SyntaxNode = declaratorSyntax.Initializer

                        ' As New clauses with a single variable are bound as BoundLocalDeclarations, so adjust appropriately
                        Dim isImplicit As Boolean = False
                        If last.InitializedByAsNew Then
                            initializerSyntax = declaratorSyntax.AsClause
                        ElseIf initializerSyntax Is Nothing Then
                            ' There is no explicit syntax for the initializer, so we use the initializerValue's syntax and mark the operation as implicit.
                            initializerSyntax = initializerValue.Syntax
                            isImplicit = True
                        End If
                        initializer = OperationFactory.CreateVariableInitializer(initializerSyntax, initializerValue, _semanticModel, isImplicit)
                    End If
                Else
                    Dim asNewDeclarations = DirectCast(first, BoundAsNewLocalDeclarations)
                    declarators = asNewDeclarations.LocalDeclarations.SelectAsArray(AddressOf GetVariableDeclarator)
                    Dim initializerSyntax As AsClauseSyntax = DirectCast(asNewDeclarations.Syntax, VariableDeclaratorSyntax).AsClause
                    Dim initializerValue As IOperation = Create(asNewDeclarations.Initializer)
                    initializer = OperationFactory.CreateVariableInitializer(initializerSyntax, initializerValue, _semanticModel, isImplicit:=False)
                End If

                builder.Add(New VariableDeclaration(declarators,
                                                         initializer,
                                                         _semanticModel,
                                                         declarationGroup.Key,
                                                         type:=Nothing,
                                                         constantValue:=Nothing,
                                                         isImplicit:=False))
            Next

            Return builder.ToImmutableAndFree()
        End Function

        Private Function GetVariableDeclarator(boundLocalDeclaration As BoundLocalDeclaration) As IVariableDeclaratorOperation
            Dim initializer As Lazy(Of IVariableInitializerOperation) = New Lazy(Of IVariableInitializerOperation)(
                Function()
                    If boundLocalDeclaration.IdentifierInitializerOpt IsNot Nothing Then
                        Dim syntax = boundLocalDeclaration.Syntax
                        Dim initializerValue As Lazy(Of IOperation) = New Lazy(Of IOperation)(Function() Create(boundLocalDeclaration.IdentifierInitializerOpt))
                        Return New LazyVariableInitializer(initializerValue, _semanticModel, syntax, type:=Nothing, constantValue:=Nothing, isImplicit:=True)
                    Else
                        Return Nothing
                    End If
                End Function)
            Dim ignoredArguments As Lazy(Of ImmutableArray(Of IOperation)) = New Lazy(Of ImmutableArray(Of IOperation))(
                Function() ImmutableArray(Of IOperation).Empty)

            Return New LazyVariableDeclarator(boundLocalDeclaration.LocalSymbol, initializer, ignoredArguments, _semanticModel, boundLocalDeclaration.Syntax, type:=Nothing, constantValue:=Nothing, isImplicit:=boundLocalDeclaration.WasCompilerGenerated)
        End Function

        Private Function GetUsingStatementDeclaration(resourceList As ImmutableArray(Of BoundLocalDeclarationBase), syntax As SyntaxNode) As IVariableDeclarationGroupOperation
            Return New VariableDeclarationGroupOperation(
                            GetVariableDeclarationStatementVariables(resourceList),
                            _semanticModel,
                            syntax,
                            type:=Nothing,
                            constantValue:=Nothing,
                            isImplicit:=False) ' Declaration is always explicit
        End Function

        Private Function GetAddRemoveHandlerStatementExpression(statement As BoundAddRemoveHandlerStatement) As IOperation
            Dim eventAccess As BoundEventAccess = TryCast(statement.EventAccess, BoundEventAccess)
            Dim eventReference = If(eventAccess Is Nothing, Nothing, CreateBoundEventAccessOperation(eventAccess))
            Dim adds = statement.Kind = BoundKind.AddHandlerStatement
            Return New EventAssignmentOperation(
                eventReference, Create(statement.Handler), adds:=adds, semanticModel:=_semanticModel, syntax:=statement.Syntax, type:=Nothing, constantValue:=Nothing, isImplicit:=True)
        End Function

        Private Shared Function IsDelegateCreation(conversionKind As ConversionKind, conversionSyntax As SyntaxNode, operand As BoundNode, targetType As TypeSymbol) As Boolean
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

        ''' <summary>
        ''' Creates the Lazy IOperation from a delegate creation operand or a bound conversion operand, handling when the conversion
        ''' is actually a delegate creation.
        ''' </summary>
        Private Function CreateConversionOperand(operand As BoundNode, conversion As Conversion, conversionSyntax As SyntaxNode, targetType As TypeSymbol
                                                 ) As (Operation As Lazy(Of IOperation), ConversionStruct As Conversion, IsDelegateCreation As Boolean)
            If IsDelegateCreation(conversion.Kind, conversionSyntax, operand, targetType) Then
                Dim operandLazy As Lazy(Of IOperation)
                If operand.Kind = BoundKind.DelegateCreationExpression Then
                    ' If the child is a BoundDelegateCreationExpression, we don't want to generate a nested IDelegateCreationExpression.
                    ' So, the operand for the conversion will be the child of the BoundDelegateCreationExpression.
                    ' We see this in this syntax: Dim x = New Action(AddressOf M2)
                    ' This should be semantically equivalent to: Dim x = AddressOf M2
                    ' However, if we didn't fix this up, we would have nested IDelegateCreationExpressions here for the former case.
                    operandLazy = New Lazy(Of IOperation)(Function() CreateBoundDelegateCreationExpressionChildOperation(DirectCast(operand, BoundDelegateCreationExpression)))
                Else
                    ' This is either a lambda, or an AddressOf error scenario in which we have a delegate conversion, but it failed to bind correctly.
                    ' Delegate to standard operation handling for this.
                    operandLazy = New Lazy(Of IOperation)(Function() Create(operand))
                End If
                Return (operandLazy, conversion, IsDelegateCreation:=True)
            End If
            Dim nestedInfo = GetNestedInfo(operand, conversion.Kind, conversionSyntax, targetType)
            If nestedInfo.NestedType <> NestedType.None Then
                Return (CreateNestedDelegateCreationOrExpressionTreeOperand(operand, conversionSyntax),
                        nestedInfo.NestedConversionStruct,
                        IsDelegateCreation:=nestedInfo.NestedType = NestedType.DelegateCreation)
            Else
                Dim methodSymbol As MethodSymbol = Nothing
                Return (New Lazy(Of IOperation)(Function() Create(operand)), conversion, IsDelegateCreation:=False)
            End If
        End Function

        ''' <summary>
        ''' Gets the operand and method symbol from a BoundConversion, compensating for if the conversion is a user-defined conversion
        ''' </summary>
        Private Shared Function GetConversionInfo(boundConversion As BoundConversion) As (Operand As BoundExpression, Method As MethodSymbol)
            If (boundConversion.ConversionKind And ConversionKind.UserDefined) = ConversionKind.UserDefined Then
                Dim userDefinedConversion = DirectCast(boundConversion.Operand, BoundUserDefinedConversion)
                Return (Operand:=userDefinedConversion.Operand, Method:=userDefinedConversion.Call.Method)
            Else
                Return (Operand:=boundConversion.Operand, Method:=Nothing)
            End If
        End Function

        Private Enum NestedType
            None
            DelegateCreation
            ExpressionTree
            Conversion
        End Enum

        Private Shared Function GetNestedInfo(operand As BoundNode, conversionKind As ConversionKind, conversionSyntax As SyntaxNode, targetType As TypeSymbol
                                                                         ) As (NestedType As NestedType, NestedConversionStruct As Conversion)
            If conversionKind = ConversionKind.Identity AndAlso operand.Kind = BoundKind.Parenthesized Then
                Dim nestedOperand = operand
                Do
                    nestedOperand = DirectCast(nestedOperand, BoundParenthesized).Expression
                Loop While nestedOperand.Kind = BoundKind.Parenthesized

                Select Case nestedOperand.Kind
                    Case BoundKind.Conversion, BoundKind.DirectCast, BoundKind.TryCast
                        Dim nestedConversion = DirectCast(nestedOperand, BoundConversionOrCast)
                        If nestedConversion.Type <> targetType Then
                            Return (NestedType.None, NestedConversionStruct:=Nothing)
                        End If

                        If IsDelegateCreation(nestedConversion.ConversionKind, conversionSyntax, nestedConversion.Operand, targetType) Then
                            Return (NestedType.DelegateCreation, CreateConversion(nestedConversion))
                        ElseIf (nestedConversion.ConversionKind And ConversionKind.ConvertedToExpressionTree) = ConversionKind.ConvertedToExpressionTree Then
                            Return (NestedType.ExpressionTree, CreateConversion(nestedConversion))
                        ElseIf nestedConversion.Operand.Kind = BoundKind.Conversion OrElse
                               nestedConversion.Operand.Kind = BoundKind.TryCast OrElse
                               nestedConversion.Operand.Kind = BoundKind.DirectCast OrElse
                               nestedConversion.Operand.Kind = BoundKind.AddressOfOperator Then
                            ' If there is another conversion under this nested conversion, when we assume that this conversion should be pruned like in other cases
                            Return (NestedType.Conversion, CreateConversion(nestedConversion))
                        End If
                    Case BoundKind.DelegateCreationExpression
                        Dim nestedDelegateCreation = DirectCast(nestedOperand, BoundDelegateCreationExpression)
                        If nestedDelegateCreation.Type = targetType Then
                            Return (NestedType.DelegateCreation, New Conversion(KeyValuePair.Create(Of ConversionKind, MethodSymbol)(conversionKind, Nothing)))
                        End If
                End Select
            End If
            Return (NestedType.None, NestedConversionStruct:=Nothing)
        End Function

        Private Function CreateNestedDelegateCreationOrExpressionTreeOperand(operand As BoundNode, conversionSyntax As SyntaxNode) As Lazy(Of IOperation)
            Return New Lazy(Of IOperation)(Function() CreateNestedDelegateCreationOrExpressionTreeOperandCore(operand, conversionSyntax))
        End Function

        Private Function CreateNestedDelegateCreationOrExpressionTreeOperandCore(operand As BoundNode, conversionSyntax As SyntaxNode) As IOperation
            Select Case operand.Kind
                Case BoundKind.Parenthesized
                    Dim boundParenthesized = DirectCast(operand, BoundParenthesized)
                    ' Because we're in a delegate creation scenario, the underlying expression actually doesn't have a type.
                    Dim operandOperation = CreateNestedDelegateCreationOrExpressionTreeOperandCore(boundParenthesized.Expression, conversionSyntax)
                    Return New ParenthesizedExpression(
                        operandOperation,
                        _semanticModel,
                        boundParenthesized.Syntax,
                        operandOperation.Type,
                        ConvertToOptional(boundParenthesized.ConstantValueOpt),
                        boundParenthesized.WasCompilerGenerated)
                Case BoundKind.Conversion, BoundKind.DirectCast, BoundKind.TryCast
                    Dim boundConversion = DirectCast(operand, BoundConversionOrCast)
                    Return CreateConversionOperand(boundConversion.Operand, CreateConversion(boundConversion), conversionSyntax, boundConversion.Type).Operation.Value
                Case BoundKind.DelegateCreationExpression
                    Dim boundDelegateCreation = DirectCast(operand, BoundDelegateCreationExpression)
                    Return CreateBoundDelegateCreationExpressionChildOperation(boundDelegateCreation)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(operand.Kind)
            End Select
        End Function

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
