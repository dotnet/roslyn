' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Turns the bound initializers into a list of bound assignment statements
    ''' </summary>
    Friend Class InitializerRewriter

        ''' <summary>
        ''' Builds a constructor body. 
        ''' </summary>
        ''' <remarks>
        ''' Lowers initializers to fields assignments if not lowered yet and the first statement of the body isn't 
        ''' a call to another constructor of the containing class. 
        ''' </remarks>
        ''' <returns>
        ''' Bound block including 
        '''  - call to a base constructor
        '''  - field initializers and top-level code
        '''  - remaining constructor statements (empty for a submission)
        ''' </returns>
        Friend Shared Function BuildConstructorBody(
            compilationState As TypeCompilationState,
            constructorMethod As MethodSymbol,
            constructorInitializerOpt As BoundStatement,
            processedInitializers As Binder.ProcessedFieldOrPropertyInitializers,
            block As BoundBlock) As BoundBlock

            Dim hasMyBaseConstructorCall As Boolean = False
            Dim containingType = constructorMethod.ContainingType

            If HasExplicitMeConstructorCall(block, containingType, hasMyBaseConstructorCall) AndAlso Not hasMyBaseConstructorCall Then
                Return block
            End If

            ' rewrite initializers just once, statements will be reused when emitting all constructors with field initializers:
            If Not processedInitializers.BoundInitializers.IsDefaultOrEmpty AndAlso processedInitializers.InitializerStatements.IsDefault Then
                processedInitializers.InitializerStatements = RewriteInitializersAsStatements(constructorMethod, processedInitializers.BoundInitializers)
                Debug.Assert(processedInitializers.BoundInitializers.Length = processedInitializers.InitializerStatements.Length)
            End If

            Dim initializerStatements = processedInitializers.InitializerStatements
            Dim blockStatements As ImmutableArray(Of BoundStatement) = block.Statements

            Dim boundStatements = ArrayBuilder(Of BoundStatement).GetInstance()

            If constructorInitializerOpt IsNot Nothing Then
                ' Inserting base constructor call 
                Debug.Assert(Not hasMyBaseConstructorCall)
                boundStatements.Add(constructorInitializerOpt)

            ElseIf hasMyBaseConstructorCall Then
                ' Using existing constructor call -- it must be the first statement in the block
                boundStatements.Add(blockStatements(0))

            ElseIf Not constructorMethod.IsShared AndAlso containingType.IsValueType Then
                ' TODO: this can be skipped if we have equal number of initializers and fields
                ' assign all fields
                ' Me = Nothing
                Dim syntax = block.Syntax
                boundStatements.Add(
                    New BoundExpressionStatement(
                    syntax,
                    New BoundAssignmentOperator(
                                    syntax,
                                    New BoundValueTypeMeReference(syntax, containingType),
                                    New BoundConversion(
                                        syntax,
                                        New BoundLiteral(syntax, ConstantValue.Null, Nothing),
                                        ConversionKind.WideningNothingLiteral,
                                        checked:=False,
                                        explicitCastInCode:=False,
                                        type:=containingType),
                                    suppressObjectClone:=True,
                                    type:=containingType)))
            End If

            ' add hookups for Handles if needed
            For Each member In containingType.GetMembers()
                If member.Kind = SymbolKind.Method Then
                    Dim methodMember = DirectCast(member, MethodSymbol)
                    Dim handledEvents = methodMember.HandledEvents

                    If handledEvents.IsEmpty Then
                        Continue For
                    End If

                    ' if method has definition and implementation parts
                    ' their "Handles" should be merged.
                    If methodMember.IsPartial Then
                        Dim implementationPart = methodMember.PartialImplementationPart
                        If implementationPart IsNot Nothing Then
                            handledEvents = handledEvents.Concat(implementationPart.HandledEvents)
                        Else
                            ' partial methods with no implementation do not handle anything
                            Continue For
                        End If
                    End If

                    For Each handledEvent In handledEvents
                        ' it should be either Constructor or SharedConstructor
                        ' if it is an instance constructor it should apply to all instance constructors.
                        If handledEvent.hookupMethod.MethodKind = constructorMethod.MethodKind Then

                            Dim eventSymbol = DirectCast(handledEvent.EventSymbol, EventSymbol)
                            Dim addHandlerMethod = eventSymbol.AddMethod
                            Dim delegateCreation = handledEvent.delegateCreation
                            Dim syntax = delegateCreation.Syntax

                            Dim receiver As BoundExpression = Nothing
                            If Not addHandlerMethod.IsShared Then
                                Dim meParam = constructorMethod.MeParameter
                                If addHandlerMethod.ContainingType = containingType Then
                                    receiver = New BoundMeReference(syntax, meParam.Type).MakeCompilerGenerated()
                                Else
                                    'Dev10 always performs base call if event is in the base class. 
                                    'Even if Me/MyClass syntax was used. It seems to be somewhat of a bug 
                                    'that noone cared about. For compat reasons we will do the same.
                                    receiver = New BoundMyBaseReference(syntax, meParam.Type).MakeCompilerGenerated()
                                End If
                            End If

                            ' Normally, we would synthesize lowered bound nodes, but we know that these nodes will
                            ' be run through the LocalRewriter.  Let the LocalRewriter handle the special code for
                            ' WinRT events.
                            boundStatements.Add(
                                New BoundAddHandlerStatement(
                                    syntax:=syntax,
                                    eventAccess:=New BoundEventAccess(syntax, receiver, eventSymbol, eventSymbol.Type).MakeCompilerGenerated(),
                                    handler:=delegateCreation).MakeCompilerGenerated())

                        End If
                    Next
                End If
            Next

            ' insert initializers AFTER implicit or explicit call to a base constructor
            ' and after Handles hookup if there were any
            If Not initializerStatements.IsDefault Then
                For Each initializer In initializerStatements
                    boundStatements.Add(initializer)
                Next
            End If

            ' Add InitializeComponent call, if need to.
            If Not constructorMethod.IsShared AndAlso compilationState.InitializeComponentOpt IsNot Nothing AndAlso constructorMethod.IsImplicitlyDeclared Then
                boundStatements.Add(New BoundCall(constructorMethod.Syntax,
                                                  compilationState.InitializeComponentOpt, Nothing,
                                                  New BoundMeReference(constructorMethod.Syntax, compilationState.InitializeComponentOpt.ContainingType),
                                                  ImmutableArray(Of BoundExpression).Empty,
                                                  Nothing,
                                                  compilationState.InitializeComponentOpt.ReturnType).
                                        MakeCompilerGenerated().ToStatement().MakeCompilerGenerated())
            End If

            ' nothing was added
            If boundStatements.Count = 0 Then
                boundStatements.Free()
                Return block
            End If

            ' move the rest of the statements
            For statementIndex = If(hasMyBaseConstructorCall, 1, 0) To blockStatements.Length - 1
                boundStatements.Add(blockStatements(statementIndex))
            Next

            Return New BoundBlock(block.Syntax, block.StatementListSyntax, block.Locals, boundStatements.ToImmutableAndFree(), block.HasErrors)
        End Function

        ''' <summary>
        ''' Rewrites GlobalStatementInitializers to ExpressionStatements and gets the initializers for fields and properties.
        ''' </summary>
        ''' <remarks>
        ''' Initializers for fields and properties cannot be rewritten to their final form at this place because they might need 
        ''' to be rewritten to replace their placeholder expressions to the final locals or temporaries (e.g. in case of a field
        ''' declaration with "AsNew" and multiple variable names. The final rewriting will during local rewriting.
        ''' The statement list returned by this function can be copied into all constructors without reprocessing it.
        ''' </remarks>
        Private Shared Function RewriteInitializersAsStatements(constructor As MethodSymbol,
                                                                boundInitializers As ImmutableArray(Of BoundInitializer)) As ImmutableArray(Of BoundStatement)
            Debug.Assert(Not boundInitializers.IsEmpty)

            Dim boundStatements = ArrayBuilder(Of BoundStatement).GetInstance(boundInitializers.Length)
            For Each init In boundInitializers
                Select Case init.Kind
                    Case BoundKind.FieldOrPropertyInitializer
                        boundStatements.Add(init)

                    Case BoundKind.GlobalStatementInitializer
                        Dim stmtInit = DirectCast(init, BoundGlobalStatementInitializer)
                        Dim syntax = init.Syntax

                        If constructor.IsSubmissionConstructor AndAlso init Is boundInitializers.Last AndAlso stmtInit.Statement.Kind = BoundKind.ExpressionStatement Then
                            Dim submissionResultVariable = New BoundParameter(syntax, constructor.Parameters(1), constructor.Parameters(1).Type)
                            Dim expr = DirectCast(stmtInit.Statement, BoundExpressionStatement).Expression

                            Debug.Assert(expr.Type IsNot Nothing)
                            If expr.Type.SpecialType <> SpecialType.System_Void Then
                                boundStatements.Add(New BoundExpressionStatement(
                                                         syntax,
                                                         New BoundAssignmentOperator(
                                                            syntax,
                                                            submissionResultVariable,
                                                            expr,
                                                            False,
                                                            expr.Type)))
                                Exit Select
                            End If
                        End If

                        boundStatements.Add(stmtInit.Statement)

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(init.Kind)
                End Select
            Next

            Return boundStatements.ToImmutableAndFree()
        End Function

        ''' <summary> 
        ''' Determines if this constructor calls another constructor of the constructor's containing class. 
        ''' </summary>
        Friend Shared Function HasExplicitMeConstructorCall(block As BoundBlock, container As TypeSymbol, <Out()> ByRef isMyBaseConstructorCall As Boolean) As Boolean
            isMyBaseConstructorCall = False

            If block.Statements.Any Then
                Dim firstBoundStatement As BoundStatement = block.Statements.First()

                ' NOTE: it is assumed that an explicit constructor call from another constructor should
                '       NOT be nested into any statement lists and to be the first constructor of the 
                '       block's statements; otherwise it would complicate this rewriting because we 
                '       will have to insert field initializers right after constructor call

                ' NOTE: If in future some rewriters break this assumption, the insersion 
                '       of initializers as well as the following code should be revised

                If firstBoundStatement.Kind = BoundKind.ExpressionStatement Then
                    Dim expression = DirectCast(firstBoundStatement, BoundExpressionStatement).Expression

                    If expression.Kind = BoundKind.Call Then
                        Dim callExpression = DirectCast(expression, BoundCall)

                        Dim receiver = callExpression.ReceiverOpt
                        If receiver IsNot Nothing AndAlso receiver.IsInstanceReference Then
                            Dim methodSymbol = callExpression.Method
                            If methodSymbol.MethodKind = MethodKind.Constructor Then
                                isMyBaseConstructorCall = receiver.IsMyBaseReference
                                Return methodSymbol.ContainingType = container
                            End If
                        End If
                    End If
                End If
            End If

            Return False
        End Function
    End Class
End Namespace
