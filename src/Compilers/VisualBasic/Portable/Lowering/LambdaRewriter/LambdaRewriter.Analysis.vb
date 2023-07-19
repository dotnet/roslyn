' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class LambdaRewriter

        ''' <summary>
        ''' Perform a first analysis pass in preparation for removing all lambdas from a method body.  The entry point is Analyze.
        ''' The results of analysis are placed in the fields seenLambda, blockParent, variableBlock, captured, and captures.
        ''' </summary>
        Friend NotInheritable Class Analysis
            Inherits BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator

            Private ReadOnly _diagnostics As BindingDiagnosticBag
            Private ReadOnly _method As MethodSymbol

            Private _currentParent As MethodSymbol
            Private _currentBlock As BoundNode

            ''' <summary>
            ''' Set to true of any lambda expressions were seen in the analyzed method body.
            ''' </summary>
            Friend seenLambda As Boolean = False

            ' seenBackBranches is used to decide whether closures should attempt copy-constructing.
            ' We do not want a very complicated analysis here as redundant attempt to copy-construct
            ' should not cause a lot of overhead (passing extra argument + null check).
            '
            ' However we want to check for methods without back branches as those are common and
            ' easy to detect cases that do not need copyconstructing.

            ''' <summary>
            ''' Set to true if method body contains any back branches (loops).
            ''' </summary>
            Friend seenBackBranches As Boolean = False

            ''' <summary>
            ''' For each statement with captured variables, identifies the nearest enclosing statement with captured variables.
            ''' </summary>
            Friend blockParent As Dictionary(Of BoundNode, BoundNode) = New Dictionary(Of BoundNode, BoundNode)()
            Friend lambdaParent As Dictionary(Of LambdaSymbol, MethodSymbol) = New Dictionary(Of LambdaSymbol, MethodSymbol)(ReferenceEqualityComparer.Instance)

            ''' <summary>
            ''' For each captured variable, identifies the statement in which it will be moved to a frame class.  This is
            ''' normally the block where the variable is introduced, but method parameters are moved
            ''' to a frame class within the body of the method.
            ''' </summary>
            Friend variableScope As Dictionary(Of Symbol, BoundNode) = New Dictionary(Of Symbol, BoundNode)(ReferenceEqualityComparer.Instance)

            ''' <summary>
            ''' For a given label, the nearest enclosing block that captures variables
            ''' </summary>
            Friend labelBlock As Dictionary(Of LabelSymbol, BoundNode) = New Dictionary(Of LabelSymbol, BoundNode)(ReferenceEqualityComparer.Instance)

            ''' <summary>
            ''' For a given goto, the nearest enclosing block that captures variables
            ''' </summary>
            Friend gotoBlock As Dictionary(Of BoundGotoStatement, BoundNode) = New Dictionary(Of BoundGotoStatement, BoundNode)()

            ''' <summary>
            ''' Blocks that contain (recursively) a lambda that is lifting.
            ''' Such blocks are considered as potentially needing closure initialization when doing jump verification.
            ''' </summary>
            Friend containsLiftingLambda As HashSet(Of BoundNode) = New HashSet(Of BoundNode)()

            ''' <summary>
            ''' Blocks that are positioned between a block declaring some lifted variables
            ''' and a block that contains the lambda that lifts said variables.
            ''' If such block itself requires a closure, then it must lift parent frame pointer into the closure
            ''' in addition to whatever else needs to be lifted.
            '''
            ''' NOTE: This information is computed in addition to the regular analysis of the tree and only needed for rewriting.
            ''' If someone only needs diagnostics or information about captures, this information is not necessary.
            ''' ComputeLambdaScopesAndFrameCaptures needs to be called to compute this.
            ''' </summary>
            Friend needsParentFrame As HashSet(Of BoundNode)

            ''' <summary>
            ''' Optimized locations of lambdas.
            '''
            ''' Lambda does not need to be placed in a frame that corresponds to its lexical scope if lambda does not reference any local state in that scope.
            ''' It is advantageous to place lambdas higher in the scope tree, ideally in the innermost scope of all scopes that contain variables captured by a given lambda.
            ''' Doing so reduces indirections needed when captured local are accessed. For example locals from the innermost scope can be accessed with no indirection at all.
            '''
            ''' NOTE: This information is computed in addition to the regular analysis of the tree and only needed for rewriting.
            ''' If someone only needs diagnostics or information about captures, this information is not necessary.
            ''' ComputeLambdaScopesAndFrameCaptures needs to be called to compute this.
            ''' </summary>
            Friend lambdaScopes As Dictionary(Of LambdaSymbol, BoundNode)

            ''' <summary>
            ''' The set of captured variables seen in the method body.
            ''' </summary>
            Friend capturedVariables As HashSet(Of Symbol) = New HashSet(Of Symbol)(ReferenceEqualityComparer.Instance)

            ''' <summary>
            ''' For each lambda in the code, the set of variables that it captures.
            ''' </summary>
            Friend capturedVariablesByLambda As MultiDictionary(Of LambdaSymbol, Symbol) = New MultiDictionary(Of LambdaSymbol, Symbol)(ReferenceEqualityComparer.Instance)

            ''' <summary>
            ''' The set of variables that were declared anywhere inside an expression lambda.
            ''' </summary>
            Friend ReadOnly declaredInsideExpressionLambda As New HashSet(Of Symbol)(ReferenceEqualityComparer.Instance)

            ''' <summary>
            ''' Set to true while we are analyzing the interior of an expression lambda.
            ''' </summary>
            Private _inExpressionLambda As Boolean

            ''' <summary>
            ''' All symbols that should never be captured with a copy constructor of a closure.
            ''' </summary>
            Friend ReadOnly symbolsCapturedWithoutCopyCtor As ISet(Of Symbol)

            Private Sub New(method As MethodSymbol, symbolsCapturedWithoutCopyCtor As ISet(Of Symbol), diagnostics As BindingDiagnosticBag)
                Me._currentParent = method
                Me._method = method
                Me.symbolsCapturedWithoutCopyCtor = symbolsCapturedWithoutCopyCtor
                Me._diagnostics = diagnostics
                Me._inExpressionLambda = False
            End Sub

            ''' <summary>
            ''' Analyzes method body that belongs to the given method symbol.
            ''' </summary>
            Public Shared Function AnalyzeMethodBody(node As BoundBlock, method As MethodSymbol, symbolsCapturedWithoutCtor As ISet(Of Symbol), diagnostics As BindingDiagnosticBag) As Analysis
                Debug.Assert(Not node.HasErrors)

                Dim analysis = New Analysis(method, symbolsCapturedWithoutCtor, diagnostics)
                analysis.Analyze(node)
                Return analysis
            End Function

            Private Sub Analyze(node As BoundNode)
                If node Is Nothing Then
                    Return
                End If

                _currentBlock = node

                If _method IsNot Nothing Then
                    For Each parameter In _method.Parameters
                        variableScope.Add(parameter, _currentBlock)
                        If _inExpressionLambda Then
                            declaredInsideExpressionLambda.Add(parameter)
                        End If
                    Next
                End If

                Visit(node)
            End Sub

            ''' <summary>
            ''' Create the optimized plan for the location of lambda methods and whether scopes need access to parent scopes
            '''  </summary>
            Friend Sub ComputeLambdaScopesAndFrameCaptures()
                lambdaScopes = New Dictionary(Of LambdaSymbol, BoundNode)(ReferenceEqualityComparer.Instance)
                needsParentFrame = New HashSet(Of BoundNode)

                For Each kvp In capturedVariablesByLambda
                    ' get innermost and outermost scopes from which a lambda captures

                    Dim innermostScopeDepth As Integer = -1
                    Dim innermostScope As BoundNode = Nothing

                    Dim outermostScopeDepth As Integer = Integer.MaxValue
                    Dim outermostScope As BoundNode = Nothing

                    For Each v In kvp.Value
                        Dim curBlock As BoundNode = Nothing
                        Dim curBlockDepth As Integer

                        If Not variableScope.TryGetValue(v, curBlock) Then
                            ' this is something that is not defined in a block, like "Me"
                            ' Since it is defined outside of the method, the depth is -1
                            curBlockDepth = -1
                        Else
                            curBlockDepth = BlockDepth(curBlock)
                        End If

                        If curBlockDepth > innermostScopeDepth Then
                            innermostScopeDepth = curBlockDepth
                            innermostScope = curBlock
                        End If

                        If curBlockDepth < outermostScopeDepth Then
                            outermostScopeDepth = curBlockDepth
                            outermostScope = curBlock
                        End If
                    Next

                    ' 1) if there is innermost scope, lambda goes there as we cannot go any higher.
                    ' 2) scopes in [innermostScope, outermostScope) chain need to have access to the parent scope.
                    '
                    ' Example:
                    '   if a lambda captures a method's parameter and Me,
                    '   its innermost scope depth is 0 (method locals and parameters)
                    '   and outermost scope is -1
                    '   Such lambda will be placed in a closure frame that corresponds to the method's outer block
                    '   and this frame will also lift original Me as a field when created by its parent.
                    '   Note that it is completely irrelevant how deeply the lexical scope of the lambda was originally nested.
                    If innermostScope IsNot Nothing Then
                        lambdaScopes.Add(kvp.Key, innermostScope)

                        While innermostScope IsNot outermostScope
                            needsParentFrame.Add(innermostScope)
                            blockParent.TryGetValue(innermostScope, innermostScope)
                        End While
                    End If
                Next
            End Sub

            ''' <summary>
            ''' Compute the nesting depth of a given block.
            ''' Topmost block (where method locals and parameters are defined) are at the depth 0.
            ''' </summary>
            Private Function BlockDepth(node As BoundNode) As Integer
                ' TODO: this could be precomputed and stored by analysis phase
                Dim result As Integer = -1
                While node IsNot Nothing
                    result = result + 1
                    If Not blockParent.TryGetValue(node, node) Then
                        Exit While
                    End If

                End While

                Return result
            End Function

            Public Function PushBlock(node As BoundNode, locals As ImmutableArray(Of LocalSymbol)) As BoundNode
                If (locals.IsEmpty) Then
                    Return _currentBlock
                End If

                Dim previousBlock = _currentBlock
                _currentBlock = node
                If _currentBlock IsNot previousBlock Then
                    blockParent.Add(_currentBlock, previousBlock)
                End If

                For Each local In locals
                    Debug.Assert(local.ContainingSymbol = Me._currentParent OrElse
                                 local.ContainingSymbol.Kind <> SymbolKind.Method,
                                 "locals should be owned by current method")

                    variableScope.Add(local, _currentBlock)
                    If _inExpressionLambda Then
                        declaredInsideExpressionLambda.Add(local)
                    End If
                Next

                Return previousBlock
            End Function

            Public Sub PopBlock(previousBlock As BoundNode)
                _currentBlock = previousBlock
            End Sub

            Public Overrides Function VisitCatchBlock(node As BoundCatchBlock) As BoundNode
                If node.LocalOpt Is Nothing Then
                    Return MyBase.VisitCatchBlock(node)
                End If

                Dim previousBlock = PushBlock(node, ImmutableArray.Create(Of LocalSymbol)(node.LocalOpt))
                Dim result = MyBase.VisitCatchBlock(node)
                PopBlock(previousBlock)
                Return result
            End Function

            Public Overrides Function VisitBlock(node As BoundBlock) As BoundNode
                Dim previousBlock = PushBlock(node, node.Locals)
                Dim result = MyBase.VisitBlock(node)
                PopBlock(previousBlock)
                Return result
            End Function

            Public Overrides Function VisitSequence(node As BoundSequence) As BoundNode
                Dim previousBlock = PushBlock(node, node.Locals)
                Dim result = MyBase.VisitSequence(node)
                PopBlock(previousBlock)
                Return result
            End Function

            Public Overrides Function VisitLambda(node As BoundLambda) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Private Overloads Function VisitLambda(node As BoundLambda, convertToExpressionTree As Boolean) As BoundNode
                Debug.Assert(node.LambdaSymbol IsNot Nothing)
                seenLambda = True
                Dim oldParent = _currentParent
                Dim oldBlock = _currentBlock
                _currentParent = node.LambdaSymbol
                _currentBlock = node.Body
                blockParent.Add(_currentBlock, oldBlock)
                lambdaParent.Add(node.LambdaSymbol, oldParent)

                Dim wasInExpressionLambda As Boolean = Me._inExpressionLambda
                Me._inExpressionLambda = _inExpressionLambda OrElse convertToExpressionTree

                For Each parameter In node.LambdaSymbol.Parameters
                    variableScope.Add(parameter, _currentBlock)
                    If _inExpressionLambda Then
                        declaredInsideExpressionLambda.Add(parameter)
                    End If
                Next

                For Each local In node.Body.Locals
                    variableScope.Add(local, _currentBlock)
                    If _inExpressionLambda Then
                        declaredInsideExpressionLambda.Add(local)
                    End If
                Next

                Dim result = MyBase.VisitBlock(node.Body)

                Me._inExpressionLambda = wasInExpressionLambda

                _currentParent = oldParent
                _currentBlock = oldBlock

                Return result
            End Function

            Public Overrides Function VisitTryCast(node As BoundTryCast) As BoundNode
                Debug.Assert(node.RelaxationLambdaOpt Is Nothing)

                Dim lambda As BoundLambda = TryCast(node.Operand, BoundLambda)
                If lambda Is Nothing Then
                    Return MyBase.VisitTryCast(node)
                End If

                Return VisitLambda(lambda, (node.ConversionKind And ConversionKind.ConvertedToExpressionTree) <> 0)
            End Function

            Public Overrides Function VisitDirectCast(node As BoundDirectCast) As BoundNode
                Debug.Assert(node.RelaxationLambdaOpt Is Nothing)

                Dim lambda As BoundLambda = TryCast(node.Operand, BoundLambda)
                If lambda Is Nothing Then
                    Return MyBase.VisitDirectCast(node)
                End If

                Return VisitLambda(lambda, (node.ConversionKind And ConversionKind.ConvertedToExpressionTree) <> 0)
            End Function

            Public Overrides Function VisitConversion(conversion As BoundConversion) As BoundNode
                Debug.Assert(conversion.ExtendedInfoOpt Is Nothing)

                Dim lambda As BoundLambda = TryCast(conversion.Operand, BoundLambda)
                If lambda Is Nothing Then
                    Return MyBase.VisitConversion(conversion)
                End If

                Return VisitLambda(lambda, (conversion.ConversionKind And ConversionKind.ConvertedToExpressionTree) <> 0)
            End Function

            ''' <summary>
            ''' Once we see a lambda lifting something
            ''' We mark all scopes from the current up to the one that declares lifted symbol as
            ''' containing a lifting lambda.
            ''' This is needed so that we could reject jumps that might jump over frame allocations.
            '''
            ''' NOTE: because of optimizations lambda _might_ be placed in a frame higher
            '''       than its lexical scope and thus make a jump technically legal.
            '''       However, we explicitly do not consider frame optimizations in this analysis.
            ''' </summary>
            Private Sub RecordCaptureInIntermediateBlocks(variableOrParameter As Symbol)
                Dim curBlock As BoundNode = _currentBlock

                Dim declBlock As BoundNode = Nothing
                If Not variableScope.TryGetValue(variableOrParameter, declBlock) Then
                    Debug.Assert(DirectCast(variableOrParameter, ParameterSymbol).IsMe)
                End If

                containsLiftingLambda.Add(curBlock)

                While curBlock IsNot declBlock AndAlso curBlock IsNot Nothing
                    If blockParent.TryGetValue(curBlock, curBlock) Then
                        containsLiftingLambda.Add(curBlock)
                    End If
                End While
            End Sub

            ''' <summary>
            ''' This method is called on every variable reference.
            ''' It checks for cases where variable is declared outside of the lambda in which it is being accessed
            ''' If capture is detected, than it marks variable as capturED and all lambdas involved as capturING
            ''' </summary>
            Private Sub ReferenceVariable(variableOrParameter As Symbol, syntax As SyntaxNode)
                ' No need to do anything if we are not in a lambda.
                If _currentParent.MethodKind <> MethodKind.LambdaMethod Then
                    Return
                End If

                If variableOrParameter.Kind = SymbolKind.Local Then
                    Dim local = DirectCast(variableOrParameter, LocalSymbol)
                    If local.IsConst Then
                        ' Don't capture local constants
                        Return
                    End If
                End If

                Dim container = variableOrParameter.ContainingSymbol
                Debug.Assert(container IsNot Nothing)

                Dim parent = _currentParent
                Dim isCaptured As Boolean = False

                If parent IsNot Nothing AndAlso parent <> container Then
                    capturedVariables.Add(variableOrParameter)
                    isCaptured = True
                    RecordCaptureInIntermediateBlocks(variableOrParameter)

                    Do
                        Dim lambda = DirectCast(parent, LambdaSymbol)
                        capturedVariablesByLambda.Add(lambda, variableOrParameter)
                        parent = lambdaParent(lambda)
                    Loop While parent.MethodKind = MethodKind.LambdaMethod AndAlso parent IsNot container
                    '  the loop exits when the sequence of nested lambdas ends or one of
                    '   the lambdas is the variable or parameter's container
                End If

                If isCaptured Then
                    VerifyCaptured(variableOrParameter, syntax)
                End If
            End Sub

            Private Sub VerifyCaptured(variableOrParameter As Symbol, syntax As SyntaxNode)
                Dim type As TypeSymbol
                Dim asParameter = TryCast(variableOrParameter, ParameterSymbol)

                If asParameter IsNot Nothing Then
                    type = asParameter.Type
                Else
                    Dim asVariable = DirectCast(variableOrParameter, LocalSymbol)
                    type = asVariable.Type

                    If asVariable.IsByRef Then
                        Throw ExceptionUtilities.UnexpectedValue(asVariable.IsByRef)
                    End If
                End If

                If type.IsRestrictedType Then
                    If Binder.IsTopMostEnclosingLambdaAQueryLambda(_currentParent, variableOrParameter.ContainingSymbol) Then
                        _diagnostics.Add(ERRID.ERR_CannotLiftRestrictedTypeQuery, syntax.GetLocation(), type)
                    Else
                        _diagnostics.Add(ERRID.ERR_CannotLiftRestrictedTypeLambda, syntax.GetLocation(), type)
                    End If
                End If
            End Sub

            Public Overrides Function VisitMeReference(node As BoundMeReference) As BoundNode
                ReferenceVariable(Me._method.MeParameter, node.Syntax)
                Return MyBase.VisitMeReference(node)
            End Function

            Public Overrides Function VisitMyClassReference(node As BoundMyClassReference) As BoundNode
                ReferenceVariable(Me._method.MeParameter, node.Syntax)
                Return MyBase.VisitMyClassReference(node)
            End Function

            Public Overrides Function VisitMyBaseReference(node As BoundMyBaseReference) As BoundNode
                ReferenceVariable(Me._method.MeParameter, node.Syntax)
                Return MyBase.VisitMyBaseReference(node)
            End Function

            Public Overrides Function VisitParameter(node As BoundParameter) As BoundNode
                ReferenceVariable(node.ParameterSymbol, node.Syntax)
                Return MyBase.VisitParameter(node)
            End Function

            Public Overrides Function VisitLocal(node As BoundLocal) As BoundNode
                ReferenceVariable(node.LocalSymbol, node.Syntax)
                Return MyBase.VisitLocal(node)
            End Function

            Public Overrides Function VisitRangeVariable(node As BoundRangeVariable) As BoundNode
                Throw ExceptionUtilities.Unreachable
            End Function

            Public Overrides Function VisitLabelStatement(node As BoundLabelStatement) As BoundNode
                labelBlock.Add(node.Label, _currentBlock)

                Return MyBase.VisitLabelStatement(node)
            End Function

            Public Overrides Function VisitConditionalGoto(node As BoundConditionalGoto) As BoundNode
                ' if we have seen this label already
                ' it is a back-branch
                If labelBlock.ContainsKey(node.Label) Then
                    seenBackBranches = True
                End If

                Return MyBase.VisitConditionalGoto(node)
            End Function

            ''' <summary>
            ''' For performance reason we may not want to check if synthetic gotos are legal.
            ''' Those are the majority, but should not be ever illegal (how user would fix them?).
            ''' </summary>
            Private Shared Function MayParticipateInIllegalBranch(node As BoundGotoStatement) As Boolean
#If DEBUG Then
                ' Validate synthetic branches in debug too.
                Return True
#Else
                'TODO:  synthetic gotos should be marked as compiler generated.
                '       There are lots of them and they are not supposed to be ever illegal.
                Return Not node.WasCompilerGenerated
#End If
            End Function

            Public Overrides Function VisitGotoStatement(node As BoundGotoStatement) As BoundNode
                ' if we have seen this label already
                ' it is a back-branch
                If labelBlock.ContainsKey(node.Label) Then
                    seenBackBranches = True
                End If

                If MayParticipateInIllegalBranch(node) Then
                    gotoBlock.Add(node, _currentBlock)
                End If

                Return MyBase.VisitGotoStatement(node)
            End Function

            Public Overrides Function VisitMethodGroup(node As BoundMethodGroup) As BoundNode
                Debug.Assert(False, "should not have nodes like this")
                Return Nothing
            End Function
        End Class
    End Class
End Namespace

