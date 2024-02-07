' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    ' Rewriting for select case statement.
    ' 
    ' Select case statements can be rewritten as: Switch Table or an If list.
    ' There are certain restrictions and heuristics for determining which approach
    ' is being chosen for each select case statement. This determination
    ' is done in Binder.RecommendSwitchTable method and the result is stored in
    ' BoundSelectStatement.RecommendSwitchTable flag.

    ' For switch table based approach we have an option of completely rewriting the switch header
    ' and switch sections into simpler constructs, i.e. we can rewrite the select header
    ' using bound conditional goto statements and the rewrite the case blocks into
    ' bound labeled statements.
    ' However, all the logic for emitting the switch jump tables is language agnostic
    ' and includes IL optimizations. Hence we delay the switch jump table generation
    ' till the emit phase. This way we also get additional benefit of sharing this code
    ' between both VB and C# compilers.

    ' For integral type switch table based statements, we delay almost all the work
    ' to the emit phase.

    ' For string type switch table based statements, we need to determine if we are generating a hash
    ' table based jump table or a non hash jump table, i.e. linear string comparisons
    ' with each case clause string constant. We use the Dev10 C# compiler's heuristic to determine this
    ' (see SwitchStringJumpTableEmitter.ShouldGenerateHashTableSwitch() for details).
    ' If we are generating a hash table based jump table, we use a simple customizable
    ' hash function to hash the string constants corresponding to the case labels.
    ' See SwitchStringJumpTableEmitter.ComputeStringHash().
    ' We need to emit this function to compute the hash value into the compiler generated
    ' <PrivateImplementationDetails> class. 
    ' If we have at least one string type select case statement in a module that needs a
    ' hash table based jump table, we generate a single public string hash synthesized method (SynthesizedStringSwitchHashMethod)
    ' that is shared across the module.

    ' CONSIDER: Ideally generating the SynthesizedStringSwitchHashMethod in <PrivateImplementationDetails>
    ' CONSIDER: class must be done during code generation, as the lowering does not mention or use
    ' CONSIDER: the hash function in any way.
    ' CONSIDER: However this would mean that <PrivateImplementationDetails> class can have 
    ' CONSIDER: different sets of methods during the code generation phase,
    ' CONSIDER: which might be problematic if we want to make the emitter multithreaded?

    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitSelectStatement(node As BoundSelectStatement) As BoundNode
            Return RewriteSelectStatement(node, node.Syntax, node.ExpressionStatement, node.ExprPlaceholderOpt, node.CaseBlocks, node.RecommendSwitchTable, node.ExitLabel)
        End Function

        Protected Function RewriteSelectStatement(
            node As BoundSelectStatement,
            syntaxNode As SyntaxNode,
            selectExpressionStmt As BoundExpressionStatement,
            exprPlaceholderOpt As BoundRValuePlaceholder,
            caseBlocks As ImmutableArray(Of BoundCaseBlock),
            recommendSwitchTable As Boolean,
            exitLabel As LabelSymbol
        ) As BoundNode
            Dim statementBuilder = ArrayBuilder(Of BoundStatement).GetInstance()
            Dim instrument = Me.Instrument(node)

            If instrument Then
                ' Add select case begin sequence point
                Dim prologue As BoundStatement = _instrumenterOpt.CreateSelectStatementPrologue(node)
                If prologue IsNot Nothing Then
                    statementBuilder.Add(prologue)
                End If
            End If

            ' Rewrite select expression
            Dim rewrittenSelectExpression As BoundExpression = Nothing
            Dim tempLocals As ImmutableArray(Of LocalSymbol) = Nothing
            Dim endSelectResumeLabel As BoundLabelStatement = Nothing

            Dim generateUnstructuredExceptionHandlingResumeCode As Boolean = ShouldGenerateUnstructuredExceptionHandlingResumeCode(node)

            Dim rewrittenSelectExprStmt = RewriteSelectExpression(generateUnstructuredExceptionHandlingResumeCode,
                                                                  selectExpressionStmt,
                                                                  rewrittenSelectExpression,
                                                                  tempLocals,
                                                                  statementBuilder,
                                                                  caseBlocks,
                                                                  recommendSwitchTable,
                                                                  endSelectResumeLabel)

            ' Add the select expression placeholder to placeholderReplacementMap
            If exprPlaceholderOpt IsNot Nothing Then
                AddPlaceholderReplacement(exprPlaceholderOpt, rewrittenSelectExpression)
            Else
                Debug.Assert(node.WasCompilerGenerated)
            End If

            ' Rewrite select statement
            If Not caseBlocks.Any() Then
                ' Add rewritten select expression statement.
                statementBuilder.Add(rewrittenSelectExprStmt)

            ElseIf recommendSwitchTable Then
                If rewrittenSelectExpression.Type.IsStringType Then
                    ' If we are emitting a hash table based string switch, then we need to create a
                    ' SynthesizedStringSwitchHashMethod and add it to the compiler generated <PrivateImplementationDetails> class.
                    EnsureStringHashFunction(node)
                End If

                statementBuilder.Add(node.Update(rewrittenSelectExprStmt, exprPlaceholderOpt, VisitList(caseBlocks), recommendSwitchTable:=True, exitLabel:=exitLabel))

            Else
                ' Rewrite select statement case blocks as IF List
                Dim lazyConditionalBranchLocal As LocalSymbol = Nothing

                statementBuilder.Add(RewriteCaseBlocksRecursive(node,
                                                                generateUnstructuredExceptionHandlingResumeCode,
                                                                caseBlocks,
                                                                startFrom:=0,
                                                                lazyConditionalBranchLocal:=lazyConditionalBranchLocal))

                If lazyConditionalBranchLocal IsNot Nothing Then
                    tempLocals = tempLocals.Add(lazyConditionalBranchLocal)
                End If

                ' Add label statement for exit label
                statementBuilder.Add(New BoundLabelStatement(syntaxNode, exitLabel))
            End If

            If exprPlaceholderOpt IsNot Nothing Then
                RemovePlaceholderReplacement(exprPlaceholderOpt)
            End If

            Dim epilogue As BoundStatement = endSelectResumeLabel
            If instrument Then
                ' Add End Select sequence point
                epilogue = _instrumenterOpt.InstrumentSelectStatementEpilogue(node, epilogue)
            End If

            If epilogue IsNot Nothing Then
                statementBuilder.Add(epilogue)
            End If

            Return New BoundBlock(syntaxNode, Nothing, tempLocals, statementBuilder.ToImmutableAndFree()).MakeCompilerGenerated()
        End Function

        Private Sub EnsureStringHashFunction(node As BoundSelectStatement)
            Dim selectCaseExpr = node.ExpressionStatement.Expression

            ' Prefer embedded version of the member if present
            Dim embeddedOperatorsType As NamedTypeSymbol = Compilation.GetWellKnownType(WellKnownType.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators)
            Dim compareStringMember As WellKnownMember =
                If(embeddedOperatorsType.IsErrorType AndAlso TypeOf embeddedOperatorsType Is MissingMetadataTypeSymbol,
                   WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareStringStringStringBoolean,
                   WellKnownMember.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators__CompareStringStringStringBoolean)

            Dim compareStringMethod = DirectCast(Compilation.GetWellKnownTypeMember(compareStringMember), MethodSymbol)
            Me.ReportMissingOrBadRuntimeHelper(selectCaseExpr, compareStringMember, compareStringMethod)

            Const stringCharsMember As SpecialMember = SpecialMember.System_String__Chars
            Dim stringCharsMethod = DirectCast(ContainingAssembly.GetSpecialTypeMember(stringCharsMember), MethodSymbol)
            Me.ReportMissingOrBadRuntimeHelper(selectCaseExpr, stringCharsMember, stringCharsMethod)

            Me.ReportBadType(selectCaseExpr, Compilation.GetSpecialType(SpecialType.System_Int32))
            Me.ReportBadType(selectCaseExpr, Compilation.GetSpecialType(SpecialType.System_UInt32))
            Me.ReportBadType(selectCaseExpr, Compilation.GetSpecialType(SpecialType.System_String))

            If _emitModule Is Nothing Then
                Return
            End If

            If Not ShouldGenerateHashTableSwitch(node) Then
                Return
            End If

            ' If we have already generated this helper method, possibly for another select case
            ' or on another thread, we don't need to regenerate it.
            Dim privateImplClass = _emitModule.GetPrivateImplClass(node.Syntax, _diagnostics.DiagnosticBag)
            If privateImplClass.GetMethod(PrivateImplementationDetails.SynthesizedStringHashFunctionName) IsNot Nothing Then
                Return
            End If

            Dim method = New SynthesizedStringSwitchHashMethod(_emitModule.SourceModule, privateImplClass)
            privateImplClass.TryAddSynthesizedMethod(method.GetCciAdapter())
        End Sub

        Private Function RewriteSelectExpression(
            generateUnstructuredExceptionHandlingResumeCode As Boolean,
            selectExpressionStmt As BoundExpressionStatement,
            <Out()> ByRef rewrittenSelectExpression As BoundExpression,
            <Out()> ByRef tempLocals As ImmutableArray(Of LocalSymbol),
            statementBuilder As ArrayBuilder(Of BoundStatement),
            caseBlocks As ImmutableArray(Of BoundCaseBlock),
            recommendSwitchTable As Boolean,
            <Out()> ByRef endSelectResumeLabel As BoundLabelStatement
        ) As BoundExpressionStatement

            Debug.Assert(statementBuilder IsNot Nothing)
            Debug.Assert(Not caseBlocks.IsDefault)

            Dim selectExprStmtSyntax = selectExpressionStmt.Syntax

            If generateUnstructuredExceptionHandlingResumeCode Then
                RegisterUnstructuredExceptionHandlingResumeTarget(selectExprStmtSyntax, canThrow:=True, statements:=statementBuilder)
                ' If the Select throws, a Resume Next should branch to the End Select.
                endSelectResumeLabel = RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(selectExprStmtSyntax)
            Else
                endSelectResumeLabel = Nothing
            End If

            ' Rewrite select expression
            rewrittenSelectExpression = VisitExpressionNode(selectExpressionStmt.Expression)

            ' We will need to store the select case expression into a temporary local if we have at least one case block
            ' and one of the following is true:
            ' (1) We are generating If list based code.
            ' (2) We are generating switch table based code and the select expression is not a bound local expression.
            '     We need a local for this case because during codeGen we are going to perform a binary search with
            '     select expression result as the key and might need to load the key multiple times.
            Dim needTempLocal = caseBlocks.Any() AndAlso (Not recommendSwitchTable OrElse rewrittenSelectExpression.Kind <> BoundKind.Local)

            If needTempLocal Then
                Dim selectExprType = rewrittenSelectExpression.Type

                ' Store the select expression result in a temp
                Dim selectStatementSyntax = DirectCast(selectExprStmtSyntax.Parent, SelectBlockSyntax).SelectStatement
                Dim tempLocal = New SynthesizedLocal(Me._currentMethodOrLambda, selectExprType, SynthesizedLocalKind.SelectCaseValue, selectStatementSyntax)
                tempLocals = ImmutableArray.Create(Of LocalSymbol)(tempLocal)

                Dim boundTemp = New BoundLocal(rewrittenSelectExpression.Syntax, tempLocal, selectExprType)
                statementBuilder.Add(New BoundAssignmentOperator(syntax:=selectExprStmtSyntax,
                                                                 left:=boundTemp,
                                                                 right:=rewrittenSelectExpression,
                                                                 suppressObjectClone:=True,
                                                                 type:=selectExprType).ToStatement().MakeCompilerGenerated())
                rewrittenSelectExpression = boundTemp.MakeRValue()
            Else

                tempLocals = ImmutableArray(Of LocalSymbol).Empty
            End If

            Return selectExpressionStmt.Update(rewrittenSelectExpression)
        End Function

        ' Rewrite select statement case blocks as IF List
        Private Function RewriteCaseBlocksRecursive(
            selectStatement As BoundSelectStatement,
            generateUnstructuredExceptionHandlingResumeCode As Boolean,
            caseBlocks As ImmutableArray(Of BoundCaseBlock),
            startFrom As Integer,
            ByRef lazyConditionalBranchLocal As LocalSymbol
        ) As BoundStatement
            Debug.Assert(startFrom <= caseBlocks.Length)

            If startFrom = caseBlocks.Length Then
                Return Nothing
            End If

            Dim rewrittenStatement As BoundStatement

            Dim curCaseBlock = caseBlocks(startFrom)

            Debug.Assert(generateUnstructuredExceptionHandlingResumeCode = ShouldGenerateUnstructuredExceptionHandlingResumeCode(curCaseBlock))

            Dim unstructuredExceptionHandlingResumeTarget As ImmutableArray(Of BoundStatement) = Nothing
            Dim rewrittenCaseCondition As BoundExpression = RewriteCaseStatement(generateUnstructuredExceptionHandlingResumeCode, curCaseBlock.CaseStatement, unstructuredExceptionHandlingResumeTarget)
            Dim rewrittenBody = DirectCast(VisitBlock(curCaseBlock.Body), BoundBlock)

            If generateUnstructuredExceptionHandlingResumeCode AndAlso startFrom < caseBlocks.Length - 1 Then
                ' Add a Resume entry to protect against fall-through to the next Case block.
                rewrittenBody = AppendToBlock(rewrittenBody, RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(curCaseBlock.Syntax))
            End If

            Dim instrument = Me.Instrument(selectStatement)

            If rewrittenCaseCondition Is Nothing Then
                ' This must be a Case Else Block
                Debug.Assert(curCaseBlock.Syntax.Kind = SyntaxKind.CaseElseBlock)
                Debug.Assert(Not curCaseBlock.CaseStatement.CaseClauses.Any())

                ' Only the last block can be a Case Else block 
                Debug.Assert(startFrom = caseBlocks.Length - 1)

                ' Case Else statement needs a sequence point
                If instrument Then
                    rewrittenStatement = _instrumenterOpt.InstrumentCaseElseBlock(curCaseBlock, rewrittenBody)
                Else
                    rewrittenStatement = rewrittenBody
                End If
            Else
                Debug.Assert(curCaseBlock.Syntax.Kind = SyntaxKind.CaseBlock)
                If instrument Then
                    rewrittenCaseCondition = _instrumenterOpt.InstrumentSelectStatementCaseCondition(selectStatement, rewrittenCaseCondition, _currentMethodOrLambda, lazyConditionalBranchLocal)
                End If

                ' EnC: We need to insert a hidden sequence point to handle function remapping in case 
                ' the containing method is edited while methods invoked in the condition are being executed.
                rewrittenStatement = RewriteIfStatement(
                    syntaxNode:=curCaseBlock.Syntax,
                    rewrittenCondition:=rewrittenCaseCondition,
                    rewrittenConsequence:=rewrittenBody,
                    rewrittenAlternative:=RewriteCaseBlocksRecursive(selectStatement, generateUnstructuredExceptionHandlingResumeCode, caseBlocks, startFrom + 1, lazyConditionalBranchLocal),
                    unstructuredExceptionHandlingResumeTarget:=unstructuredExceptionHandlingResumeTarget,
                    instrumentationTargetOpt:=If(instrument, curCaseBlock, Nothing))
            End If

            Return rewrittenStatement
        End Function

        ' Rewrite case statement as conditional expression
        Private Function RewriteCaseStatement(
            generateUnstructuredExceptionHandlingResumeCode As Boolean,
            node As BoundCaseStatement,
            <Out()> ByRef unstructuredExceptionHandlingResumeTarget As ImmutableArray(Of BoundStatement)
        ) As BoundExpression
            If node.CaseClauses.Any() Then
                ' Case block
                Debug.Assert(node.Syntax.Kind = SyntaxKind.CaseStatement)
                Debug.Assert(node.ConditionOpt IsNot Nothing)
                unstructuredExceptionHandlingResumeTarget = If(generateUnstructuredExceptionHandlingResumeCode,
                                                               RegisterUnstructuredExceptionHandlingResumeTarget(node.Syntax, canThrow:=True),
                                                               Nothing)
                Return VisitExpressionNode(node.ConditionOpt)
            Else
                ' Case Else block
                Debug.Assert(node.Syntax.Kind = SyntaxKind.CaseElseStatement)
                unstructuredExceptionHandlingResumeTarget = Nothing
                Return Nothing
            End If
        End Function

        ' Checks whether we are generating a hash table based string switch
        Private Shared Function ShouldGenerateHashTableSwitch(node As BoundSelectStatement) As Boolean
            Debug.Assert(Not node.HasErrors)
            Debug.Assert(node.ExpressionStatement.Expression.Type.IsStringType)
            Debug.Assert(node.RecommendSwitchTable)

            ' compute unique string constants from select clauses.
            Dim uniqueStringConstants = New HashSet(Of ConstantValue)

            For Each caseBlock In node.CaseBlocks
                For Each caseClause In caseBlock.CaseStatement.CaseClauses
                    Dim constant As ConstantValue = Nothing
                    Select Case caseClause.Kind
                        Case BoundKind.SimpleCaseClause
                            Dim simpleCaseClause = DirectCast(caseClause, BoundSimpleCaseClause)

                            Debug.Assert(simpleCaseClause.ValueOpt IsNot Nothing)
                            Debug.Assert(simpleCaseClause.ConditionOpt Is Nothing)

                            constant = simpleCaseClause.ValueOpt.ConstantValueOpt

                        Case BoundKind.RelationalCaseClause
                            Dim relationalCaseClause = DirectCast(caseClause, BoundRelationalCaseClause)

                            Debug.Assert(relationalCaseClause.OperatorKind = BinaryOperatorKind.Equals)
                            Debug.Assert(relationalCaseClause.ValueOpt IsNot Nothing)
                            Debug.Assert(relationalCaseClause.ConditionOpt Is Nothing)

                            constant = relationalCaseClause.ValueOpt.ConstantValueOpt

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(caseClause.Kind)
                    End Select

                    Debug.Assert(constant IsNot Nothing)
                    uniqueStringConstants.Add(constant)
                Next
            Next

            Return SwitchStringJumpTableEmitter.ShouldGenerateHashTableSwitch(uniqueStringConstants.Count)
        End Function

        Public Overrides Function VisitCaseBlock(node As BoundCaseBlock) As BoundNode
            Dim rewritten = DirectCast(MyBase.VisitCaseBlock(node), BoundCaseBlock)
            Dim rewrittenBody = rewritten.Body

            ' Add a Resume entry to protect against fall-through to the next Case block.
            If ShouldGenerateUnstructuredExceptionHandlingResumeCode(node) Then
                rewrittenBody = AppendToBlock(rewrittenBody, RegisterUnstructuredExceptionHandlingNonThrowingResumeTarget(node.Syntax))
            End If

            Return rewritten.Update(rewritten.CaseStatement, rewrittenBody)
        End Function

    End Class
End Namespace
