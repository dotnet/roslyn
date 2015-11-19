' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class LocalRewriter
        Inherits BoundTreeRewriterWithStackGuard

        Private ReadOnly _topMethod As MethodSymbol
        Private ReadOnly _emitModule As PEModuleBuilder
        Private ReadOnly _compilationState As TypeCompilationState
        Private ReadOnly _previousSubmissionFields As SynthesizedSubmissionFields
        Private ReadOnly _globalGenerateDebugInfo As Boolean
        Private ReadOnly _diagnostics As DiagnosticBag
        Private _symbolsCapturedWithoutCopyCtor As ISet(Of Symbol)

        Private _currentMethodOrLambda As MethodSymbol
        Private _rangeVariableMap As Dictionary(Of RangeVariableSymbol, BoundExpression)
        Private _placeholderReplacementMapDoNotUseDirectly As Dictionary(Of BoundValuePlaceholderBase, BoundExpression)
        Private _hasLambdas As Boolean
        Private _inExpressionLambda As Boolean ' Are we inside a lambda converted to expression tree?
        Private _staticLocalMap As Dictionary(Of LocalSymbol, KeyValuePair(Of SynthesizedStaticLocalBackingField, SynthesizedStaticLocalBackingField))

        Private _xmlFixupData As New XmlLiteralFixupData()
        Private _xmlImportedNamespaces As ImmutableArray(Of KeyValuePair(Of String, String))

        Private _unstructuredExceptionHandling As UnstructuredExceptionHandlingState
        Private _currentLineTemporary As LocalSymbol

        Private _createSequencePointsForTopLevelNonCompilerGeneratedExpressions As Boolean
        Private _conditionalAccessReceiverPlaceholderId As Integer

#If DEBUG Then
        ''' <summary>
        ''' A map from SyntaxNode to corresponding visited BoundStatement.
        ''' Used to ensure correct generation of resumable code for Unstructured Exception Handling.
        ''' </summary>
        Private ReadOnly _unstructuredExceptionHandlingResumableStatements As New Dictionary(Of VisualBasicSyntaxNode, BoundStatement)(ReferenceEqualityComparer.Instance)

        Private ReadOnly _leaveRestoreUnstructuredExceptionHandlingContextTracker As New Stack(Of BoundNode)()
#End If

#If DEBUG Then
        ''' <summary>
        ''' Used to prevent multiple rewrite of the same nodes.
        ''' </summary>
        Private _rewrittenNodes As New HashSet(Of BoundNode)(ReferenceEqualityComparer.Instance)
#End If

        ''' <summary>
        ''' Returns substitution currently used by the rewriter for a placeholder node.
        ''' Each occurrence of the placeholder node is replaced with the node returned.
        ''' Throws if there is no substitution.
        ''' </summary>
        Private ReadOnly Property PlaceholderReplacement(placeholder As BoundValuePlaceholderBase) As BoundExpression
            Get
                Dim value = _placeholderReplacementMapDoNotUseDirectly(placeholder)
                AssertPlaceholderReplacement(placeholder, value)
                Return value
            End Get
        End Property

        <Conditional("DEBUG")>
        Private Shared Sub AssertPlaceholderReplacement(placeholder As BoundValuePlaceholderBase, value As BoundExpression)
            Debug.Assert(value.Type.IsSameTypeIgnoringCustomModifiers(placeholder.Type))

            If placeholder.IsLValue AndAlso value.Kind <> BoundKind.MeReference Then
                Debug.Assert(value.IsLValue)
            Else
                value.AssertRValue()
            End If
        End Sub

        ''' <summary>
        ''' Sets substitution used by the rewriter for a placeholder node.
        ''' Each occurrence of the placeholder node is replaced with the node returned.
        ''' Throws if there is already a substitution.
        ''' </summary>
        Private Sub AddPlaceholderReplacement(placeholder As BoundValuePlaceholderBase, value As BoundExpression)
            AssertPlaceholderReplacement(placeholder, value)

            If _placeholderReplacementMapDoNotUseDirectly Is Nothing Then
                _placeholderReplacementMapDoNotUseDirectly = New Dictionary(Of BoundValuePlaceholderBase, BoundExpression)()
            End If

            _placeholderReplacementMapDoNotUseDirectly.Add(placeholder, value)
        End Sub

        ''' <summary>
        ''' Replaces substitution currently used by the rewriter for a placeholder node with a different substitution.
        ''' Asserts if there isn't already a substitution.
        ''' </summary>
        Private Sub UpdatePlaceholderReplacement(placeholder As BoundValuePlaceholderBase, value As BoundExpression)
            AssertPlaceholderReplacement(placeholder, value)
            Debug.Assert(_placeholderReplacementMapDoNotUseDirectly.ContainsKey(placeholder))
            _placeholderReplacementMapDoNotUseDirectly(placeholder) = value
        End Sub

        ''' <summary>
        ''' Removes substitution currently used by the rewriter for a placeholder node.
        ''' Asserts if there isn't already a substitution.
        ''' </summary>
        Private Sub RemovePlaceholderReplacement(placeholder As BoundValuePlaceholderBase)
            Debug.Assert(placeholder IsNot Nothing)
            Dim removed As Boolean = _placeholderReplacementMapDoNotUseDirectly.Remove(placeholder)
            Debug.Assert(removed)
        End Sub

        Private Sub New(
            topMethod As MethodSymbol,
            currentMethod As MethodSymbol,
            compilationState As TypeCompilationState,
            previousSubmissionFields As SynthesizedSubmissionFields,
            generateDebugInfo As Boolean,
            diagnostics As DiagnosticBag,
            flags As RewritingFlags,
            recursionDepth As Integer
        )
            MyBase.New(recursionDepth)

            Me._topMethod = topMethod
            Me._currentMethodOrLambda = currentMethod
            Me._globalGenerateDebugInfo = generateDebugInfo
            Me._emitModule = compilationState.ModuleBuilderOpt
            Me._compilationState = compilationState
            Me._previousSubmissionFields = previousSubmissionFields
            Me._diagnostics = diagnostics
            Me._flags = flags
        End Sub

        Public ReadOnly Property OptimizationLevelIsDebug As Boolean
            Get
                Return Me.Compilation.Options.OptimizationLevel = OptimizationLevel.Debug
            End Get
        End Property

        Private Shared Function RewriteNode(node As BoundNode,
                                            topMethod As MethodSymbol,
                                            currentMethod As MethodSymbol,
                                            compilationState As TypeCompilationState,
                                            previousSubmissionFields As SynthesizedSubmissionFields,
                                            generateDebugInfo As Boolean,
                                            diagnostics As DiagnosticBag,
                                            <[In], Out> ByRef rewrittenNodes As HashSet(Of BoundNode),
                                            <Out> ByRef hasLambdas As Boolean,
                                            <Out> ByRef symbolsCapturedWithoutCtor As ISet(Of Symbol),
                                            flags As RewritingFlags,
                                            recursionDepth As Integer) As BoundNode

            Debug.Assert(node Is Nothing OrElse Not node.HasErrors, "node has errors")

            Dim rewriter = New LocalRewriter(topMethod, currentMethod, compilationState, previousSubmissionFields, generateDebugInfo, diagnostics, flags, recursionDepth)

#If DEBUG Then
            If rewrittenNodes IsNot Nothing Then
                rewriter._rewrittenNodes = rewrittenNodes
            Else
                rewrittenNodes = rewriter._rewrittenNodes
            End If

            Debug.Assert(rewriter._leaveRestoreUnstructuredExceptionHandlingContextTracker.Count = 0)
#End If

            Dim result As BoundNode = rewriter.Visit(node)

            If Not rewriter._xmlFixupData.IsEmpty Then
                result = InsertXmlLiteralsPreamble(result, rewriter._xmlFixupData.MaterializeAndFree())
            End If

            hasLambdas = rewriter._hasLambdas
            symbolsCapturedWithoutCtor = rewriter._symbolsCapturedWithoutCopyCtor
            Return result
        End Function

        Private Shared Function InsertXmlLiteralsPreamble(node As BoundNode, fixups As ImmutableArray(Of XmlLiteralFixupData.LocalWithInitialization)) As BoundBlock
            Dim count As Integer = fixups.Length
            Debug.Assert(count > 0)

            Dim locals(count - 1) As LocalSymbol
            Dim sideEffects(count) As BoundStatement

            For i = 0 To count - 1
                Dim fixup As XmlLiteralFixupData.LocalWithInitialization = fixups(i)
                locals(i) = fixup.Local
                Dim init As BoundExpression = fixup.Initialization
                sideEffects(i) = New BoundExpressionStatement(init.Syntax, init)
            Next

            sideEffects(count) = DirectCast(node, BoundStatement)
            Return New BoundBlock(node.Syntax, Nothing, locals.AsImmutable, sideEffects.AsImmutableOrNull)
        End Function

        Public Shared Function Rewrite(node As BoundBlock,
                                       topMethod As MethodSymbol,
                                       compilationState As TypeCompilationState,
                                       previousSubmissionFields As SynthesizedSubmissionFields,
                                       diagnostics As DiagnosticBag,
                                       <Out> ByRef rewrittenNodes As HashSet(Of BoundNode),
                                       <Out> ByRef hasLambdas As Boolean,
                                       <Out> ByRef symbolsCapturedWithoutCopyCtor As ISet(Of Symbol),
                                       flags As RewritingFlags,
                                       currentMethod As MethodSymbol) As BoundBlock

            Debug.Assert(rewrittenNodes Is Nothing)

            ' if the node already contains debug info, don't generate more debug info
            Dim generateDebugInfo = (flags And RewritingFlags.AllowSequencePoints) = 0

            Return DirectCast(RewriteNode(node,
                                          topMethod,
                                          If(currentMethod, topMethod),
                                          compilationState,
                                          previousSubmissionFields,
                                          generateDebugInfo,
                                          diagnostics,
                                          rewrittenNodes,
                                          hasLambdas,
                                          symbolsCapturedWithoutCopyCtor,
                                          flags,
                                          recursionDepth:=0), BoundBlock)
        End Function

        Public Shared Function RewriteExpressionTree(node As BoundExpression,
                                                     method As MethodSymbol,
                                                     compilationState As TypeCompilationState,
                                                     previousSubmissionFields As SynthesizedSubmissionFields,
                                                     diagnostics As DiagnosticBag,
                                                     rewrittenNodes As HashSet(Of BoundNode),
                                                     recursionDepth As Integer) As BoundExpression

            Debug.Assert(rewrittenNodes IsNot Nothing)
            Dim hasLambdas As Boolean = False
            Const generateDebugInfo = False ' don't generate debug information in expression tree lambdas
            Dim result = DirectCast(RewriteNode(node,
                                                method,
                                                method,
                                                compilationState,
                                                previousSubmissionFields,
                                                generateDebugInfo,
                                                diagnostics,
                                                rewrittenNodes,
                                                hasLambdas,
                                                SpecializedCollections.EmptySet(Of Symbol),
                                                RewritingFlags.Default,
                                                recursionDepth), BoundExpression)
            Debug.Assert(Not hasLambdas)
            Return result
        End Function

        Public Overrides Function Visit(node As BoundNode) As BoundNode
            Debug.Assert(node Is Nothing OrElse Not node.HasErrors, "node has errors")

            Dim expressionNode = TryCast(node, BoundExpression)

            If expressionNode IsNot Nothing Then
                Return VisitExpression(expressionNode)
            Else
#If DEBUG Then
                Debug.Assert(node Is Nothing OrElse Not _rewrittenNodes.Contains(node), "LocalRewriter: Rewriting the same node several times.")
#End If
                Dim result = MyBase.Visit(node)
#If DEBUG Then
                If result IsNot Nothing Then
                    If result Is node Then
                        result = result.MemberwiseClone(Of BoundNode)()
                    End If

                    _rewrittenNodes.Add(result)
                End If
#End If
                Return result
            End If
        End Function

        Private Function VisitExpression(node As BoundExpression) As BoundExpression
#If DEBUG Then
            Debug.Assert(Not _rewrittenNodes.Contains(node), "LocalRewriter: Rewriting the same node several times.")
            Dim originalNode = node
#End If

            Dim constantValue = node.ConstantValueOpt
            Dim result As BoundExpression

            Dim createSequencePoint As Boolean =
                    _createSequencePointsForTopLevelNonCompilerGeneratedExpressions AndAlso
                    GenerateDebugInfo AndAlso
                    Not node.WasCompilerGenerated AndAlso
                    node.Syntax.Kind <> SyntaxKind.GroupAggregation AndAlso
                    ((node.Syntax.Kind = SyntaxKind.SimpleAsClause AndAlso node.Syntax.Parent.Kind = SyntaxKind.CollectionRangeVariable) OrElse
                     TypeOf node.Syntax Is ExpressionSyntax)

            If createSequencePoint Then
                _createSequencePointsForTopLevelNonCompilerGeneratedExpressions = False
            End If

            If constantValue IsNot Nothing Then
                result = RewriteConstant(node, constantValue)
            Else
                result = VisitExpressionWithStackGuard(node)
            End If

            If createSequencePoint Then
                _createSequencePointsForTopLevelNonCompilerGeneratedExpressions = True
                result = New BoundSequencePointExpression(node.Syntax, result, result.Type)
            End If

#If DEBUG Then
            Debug.Assert(node.IsLValue = result.IsLValue OrElse
                         (result.Kind = BoundKind.MeReference AndAlso TypeOf node Is BoundLValuePlaceholderBase))

            If node.Type Is Nothing Then
                Debug.Assert(result.Type Is Nothing)
            Else
                Debug.Assert(result.Type IsNot Nothing)

                If (node.Kind = BoundKind.ObjectCreationExpression OrElse node.Kind = BoundKind.NewT) AndAlso
                   DirectCast(node, BoundObjectCreationExpressionBase).InitializerOpt IsNot Nothing AndAlso
                   DirectCast(node, BoundObjectCreationExpressionBase).InitializerOpt.Kind = BoundKind.ObjectInitializerExpression AndAlso
                   Not DirectCast(DirectCast(node, BoundObjectCreationExpressionBase).InitializerOpt, BoundObjectInitializerExpression).CreateTemporaryLocalForInitialization Then
                    Debug.Assert(result.Type.IsVoidType())
                Else
                    Debug.Assert(result.Type.IsSameTypeIgnoringCustomModifiers(node.Type))
                End If
            End If
#End If

#If DEBUG Then
            If result Is originalNode Then
                Select Case result.Kind
                    Case BoundKind.LValuePlaceholder,
                         BoundKind.RValuePlaceholder,
                         BoundKind.WithLValueExpressionPlaceholder,
                         BoundKind.WithRValueExpressionPlaceholder
                        ' do not clone these as they have special semantics and may 
                        ' be used for identity search after local rewriter is finished

                    Case Else
                        result = result.MemberwiseClone(Of BoundExpression)()
                End Select
            End If

            _rewrittenNodes.Add(result)
#End If

            Return result
        End Function

        Private ReadOnly Property Compilation As VisualBasicCompilation
            Get
                Return Me._topMethod.DeclaringCompilation
            End Get
        End Property

        Private ReadOnly Property ContainingAssembly As SourceAssemblySymbol
            Get
                Return DirectCast(Me._topMethod.ContainingAssembly, SourceAssemblySymbol)
            End Get
        End Property

        Private ReadOnly Property GenerateDebugInfo As Boolean
            Get
                Return _globalGenerateDebugInfo AndAlso Not _inExpressionLambda
            End Get
        End Property

        ' adds EndXXX sequence point to a statement
        ' NOTE: if target statement happens to be a block, then 
        ' sequence point goes inside the block
        ' This ensures that when we stopped on EndXXX, we are still in the block's scope
        ' and can examine locals declared in the block.
        Private Function InsertEndBlockSequencePoint(statement As BoundStatement, spSyntax As VisualBasicSyntaxNode) As BoundStatement
            If Not GenerateDebugInfo Then
                Return statement
            End If

            Return Concat(statement, New BoundSequencePoint(spSyntax, Nothing))
        End Function

        Private Function Concat(statement As BoundStatement, additionOpt As BoundStatement) As BoundStatement
            If additionOpt Is Nothing Then
                Return statement
            End If

            Dim block = TryCast(statement, BoundBlock)
            If block IsNot Nothing Then
                Dim consequenceWithEnd(block.Statements.Length) As BoundStatement
                For i = 0 To block.Statements.Length - 1
                    consequenceWithEnd(i) = block.Statements(i)
                Next

                consequenceWithEnd(block.Statements.Length) = additionOpt
                Return block.Update(block.StatementListSyntax, block.Locals, consequenceWithEnd.AsImmutableOrNull)
            Else
                Dim consequenceWithEnd(1) As BoundStatement
                consequenceWithEnd(0) = statement

                consequenceWithEnd(1) = additionOpt
                Return New BoundStatementList(statement.Syntax, consequenceWithEnd.AsImmutableOrNull)
            End If
        End Function

        Private Function AppendToBlock(block As BoundBlock, additionOpt As BoundStatement) As BoundBlock
            If additionOpt Is Nothing Then
                Return block
            End If

            Dim consequenceWithEnd(block.Statements.Length) As BoundStatement
            For i = 0 To block.Statements.Length - 1
                consequenceWithEnd(i) = block.Statements(i)
            Next

            consequenceWithEnd(block.Statements.Length) = additionOpt
            Return block.Update(block.StatementListSyntax, block.Locals, consequenceWithEnd.AsImmutableOrNull)
        End Function

        Private Function InsertBlockEpilogue(statement As BoundStatement, endBlockResumeTargetOpt As BoundStatement, sequencePointSyntax As VisualBasicSyntaxNode) As BoundStatement
            Return Concat(statement, If(GenerateDebugInfo, New BoundSequencePoint(sequencePointSyntax, endBlockResumeTargetOpt), endBlockResumeTargetOpt))
        End Function

        ' adds a sequence point before stepping on the statement
        ' NOTE: if the statement is a block the sequence point will be outside of the scope
        Private Function PrependWithSequencePoint(statement As BoundStatement, spSyntax As VisualBasicSyntaxNode) As BoundStatement
            If Not GenerateDebugInfo Then
                Return statement
            End If

            Dim consequenceWithEnd(1) As BoundStatement
            consequenceWithEnd(0) = New BoundSequencePoint(spSyntax, Nothing)
            consequenceWithEnd(1) = statement
            statement = New BoundStatementList(statement.Syntax, consequenceWithEnd.AsImmutableOrNull)
            Return statement
        End Function

        ''' <summary>
        ''' Adds a sequence point with the specified span before stepping on the statement.
        ''' </summary>
        ''' <remarks>
        ''' If the statement is a block the sequence point will be outside of the scope.
        ''' </remarks>
        Private Function PrependWithSequencePoint(statement As BoundStatement, syntax As VisualBasicSyntaxNode, span As Text.TextSpan) As BoundStatement
            If Not GenerateDebugInfo Then
                Return statement
            End If

            Return New BoundStatementList(
                statement.Syntax,
                ImmutableArray.Create(New BoundSequencePointWithSpan(syntax, Nothing, span),
                                      statement))
        End Function

        Private Function PrependWithSequencePoint(statement As BoundBlock, syntax As VisualBasicSyntaxNode) As BoundBlock
            If Not GenerateDebugInfo Then
                Return statement
            End If

            Return New BoundBlock(
                statement.Syntax,
                Nothing,
                ImmutableArray(Of LocalSymbol).Empty,
                ImmutableArray.Create(Of BoundStatement)(New BoundSequencePoint(syntax, Nothing), statement))
        End Function

        Public Overrides Function VisitSequencePointWithSpan(node As BoundSequencePointWithSpan) As BoundNode
            ' NOTE: Sequence points may not be inserted in by binder, but they may be inserted when 
            ' NOTE: code is being synthesized. In some cases, e.g. in Async rewriter and for expression 
            ' NOTE: trees, we rewrite the tree the second time, so RewritingFlags.AllowSequencePoints
            ' NOTE: should be set to make sure we don't assert and rewrite the statement properly.
            ' NOTE: GenerateDebugInfo in this case should be False as all sequence points are 
            ' NOTE: supposed to be generated by this time
            Debug.Assert((Me._flags And RewritingFlags.AllowSequencePoints) <> 0 AndAlso Not GenerateDebugInfo, "are we trying to rewrite a node more than once?")
            Return node.Update(DirectCast(Me.Visit(node.StatementOpt), BoundStatement), node.Span)
        End Function

        Public Overrides Function VisitSequencePoint(node As BoundSequencePoint) As BoundNode
            ' NOTE: Sequence points may not be inserted in by binder, but they may be inserted when 
            ' NOTE: code is being synthesized. In some cases, e.g. in Async rewriter and for expression 
            ' NOTE: trees, we rewrite the tree the second time, so RewritingFlags.AllowSequencePoints
            ' NOTE: should be set to make sure we don't assert and rewrite the statement properly.
            ' NOTE: GenerateDebugInfo in this case should be False as all sequence points are 
            ' NOTE: supposed to be generated by this time
            Debug.Assert((Me._flags And RewritingFlags.AllowSequencePoints) <> 0 AndAlso Not GenerateDebugInfo, "are we trying to rewrite a node more than once?")
            Return node.Update(DirectCast(Me.Visit(node.StatementOpt), BoundStatement))
        End Function

        Private Function MarkStatementWithSequencePoint(node As BoundStatement) As BoundStatement
            If Not GenerateDebugInfo Then
                Return node
            End If

            If node IsNot Nothing Then
                Dim syntax = node.Syntax
                If syntax IsNot Nothing AndAlso Not node.WasCompilerGenerated Then
                    Debug.Assert(node.SyntaxTree IsNot Nothing)
                    node = New BoundSequencePoint(syntax, node)
                End If
            End If

            Return node
        End Function

        Public Overrides Function VisitBadExpression(node As BoundBadExpression) As BoundNode
            ' Cannot recurse into BadExpression children since the BadExpression
            ' may represent being unable to use the child as an lvalue or rvalue.
            Throw ExceptionUtilities.Unreachable
        End Function

        Private Function RewriteReceiverArgumentsAndGenerateAccessorCall(
            syntax As VisualBasicSyntaxNode,
            methodSymbol As MethodSymbol,
            receiverOpt As BoundExpression,
            arguments As ImmutableArray(Of BoundExpression),
            constantValueOpt As ConstantValue,
            suppressObjectClone As Boolean,
            type As TypeSymbol
        ) As BoundExpression

            UpdateMethodAndArgumentsIfReducedFromMethod(methodSymbol, receiverOpt, arguments)

            Dim temporaries As ImmutableArray(Of SynthesizedLocal) = Nothing
            Dim copyBack As ImmutableArray(Of BoundExpression) = Nothing

            receiverOpt = VisitExpressionNode(receiverOpt)
            arguments = RewriteCallArguments(arguments, methodSymbol.Parameters, temporaries, copyBack, False)
            Debug.Assert(copyBack.IsDefault, "no copyback expected in accessors")

            Dim result As BoundExpression = New BoundCall(syntax,
                                     methodSymbol,
                                     Nothing,
                                     receiverOpt,
                                     arguments,
                                     constantValueOpt,
                                     suppressObjectClone,
                                     type)


            If Not temporaries.IsDefault Then
                If methodSymbol.IsSub Then
                    result = New BoundSequence(syntax,
                                               StaticCast(Of LocalSymbol).From(temporaries),
                                               ImmutableArray.Create(result),
                                               Nothing,
                                               result.Type)
                Else
                    result = New BoundSequence(syntax,
                                               StaticCast(Of LocalSymbol).From(temporaries),
                                               ImmutableArray(Of BoundExpression).Empty,
                                               result,
                                               result.Type)
                End If
            End If

            Return result
        End Function

        ' Generate a unique label with the given base name
        Private Function GenerateLabel(baseName As String) As LabelSymbol
            Return New GeneratedLabelSymbol(baseName)
        End Function

        Public Overrides Function VisitRValuePlaceholder(node As BoundRValuePlaceholder) As BoundNode
            Return PlaceholderReplacement(node)
        End Function

        Public Overrides Function VisitLValuePlaceholder(node As BoundLValuePlaceholder) As BoundNode
            Return PlaceholderReplacement(node)
        End Function

        Public Overrides Function VisitCompoundAssignmentTargetPlaceholder(node As BoundCompoundAssignmentTargetPlaceholder) As BoundNode
            Return PlaceholderReplacement(node)
        End Function

        Public Overrides Function VisitByRefArgumentPlaceholder(node As BoundByRefArgumentPlaceholder) As BoundNode
            Return PlaceholderReplacement(node)
        End Function

        Public Overrides Function VisitLValueToRValueWrapper(node As BoundLValueToRValueWrapper) As BoundNode
            Dim rewritten As BoundExpression = VisitExpressionNode(node.UnderlyingLValue)
            Return rewritten.MakeRValue()
        End Function

        ''' <summary>
        ''' Gets the special type.
        ''' </summary>
        ''' <param name="specialType">Special Type to get.</param><returns></returns>
        Private Function GetSpecialType(specialType As SpecialType) As NamedTypeSymbol
            Dim result As NamedTypeSymbol = Me._topMethod.ContainingAssembly.GetSpecialType(specialType)
            Debug.Assert(Binder.GetUseSiteErrorForSpecialType(result) Is Nothing)
            Return result
        End Function

        Private Function GetSpecialTypeWithUseSiteDiagnostics(specialType As SpecialType, syntax As SyntaxNode) As NamedTypeSymbol
            Dim result As NamedTypeSymbol = Me._topMethod.ContainingAssembly.GetSpecialType(specialType)

            Dim info = Binder.GetUseSiteErrorForSpecialType(result)
            If info IsNot Nothing Then
                Binder.ReportDiagnostic(_diagnostics, syntax, info)
            End If

            Return result
        End Function

        ''' <summary>
        ''' Gets the special type member.
        ''' </summary>
        ''' <param name="specialMember">Member of the special type.</param><returns></returns>
        Private Function GetSpecialTypeMember(specialMember As SpecialMember) As Symbol
            Return Me._topMethod.ContainingAssembly.GetSpecialTypeMember(specialMember)
        End Function

        ''' <summary>
        ''' Checks for special member and reports diagnostics if the member is Nothing or has UseSiteError.
        ''' Returns True in case diagnostics was actually reported
        ''' </summary>
        Private Function ReportMissingOrBadRuntimeHelper(node As BoundNode, specialMember As SpecialMember, memberSymbol As Symbol) As Boolean
            Return ReportMissingOrBadRuntimeHelper(node, specialMember, memberSymbol, Me._diagnostics, _compilationState.Compilation.Options.EmbedVbCoreRuntime)
        End Function

        ''' <summary>
        ''' Checks for special member and reports diagnostics if the member is Nothing or has UseSiteError.
        ''' Returns True in case diagnostics was actually reported
        ''' </summary>
        Friend Shared Function ReportMissingOrBadRuntimeHelper(node As BoundNode, specialMember As SpecialMember, memberSymbol As Symbol, diagnostics As DiagnosticBag, Optional embedVBCoreRuntime As Boolean = False) As Boolean
            If memberSymbol Is Nothing Then
                ReportMissingRuntimeHelper(node, specialMember, diagnostics, embedVBCoreRuntime)
                Return True
            Else
                Dim useSiteError = If(memberSymbol.GetUseSiteErrorInfo(), memberSymbol.ContainingType.GetUseSiteErrorInfo())
                If useSiteError IsNot Nothing Then
                    ReportDiagnostic(node, useSiteError, diagnostics)
                    Return True
                End If
            End If
            Return False
        End Function

        Private Shared Sub ReportMissingRuntimeHelper(node As BoundNode, specialMember As SpecialMember, diagnostics As DiagnosticBag, Optional embedVBCoreRuntime As Boolean = False)
            Dim descriptor = SpecialMembers.GetDescriptor(specialMember)

            ' TODO: If the type is generic, we might want to use VB style name rather than emitted name.
            Dim typeName As String = descriptor.DeclaringTypeMetadataName
            Dim memberName As String = descriptor.Name

            ReportMissingRuntimeHelper(node, typeName, memberName, diagnostics, embedVBCoreRuntime)
        End Sub

        ''' <summary>
        ''' Checks for well known member and reports diagnostics if the member is Nothing or has UseSiteError.
        ''' Returns True in case diagnostics was actually reported
        ''' </summary>
        Private Function ReportMissingOrBadRuntimeHelper(node As BoundNode, wellKnownMember As WellKnownMember, memberSymbol As Symbol) As Boolean
            Return ReportMissingOrBadRuntimeHelper(node, wellKnownMember, memberSymbol, Me._diagnostics, _compilationState.Compilation.Options.EmbedVbCoreRuntime)
        End Function

        ''' <summary>
        ''' Checks for well known member and reports diagnostics if the member is Nothing or has UseSiteError.
        ''' Returns True in case diagnostics was actually reported
        ''' </summary>
        Friend Shared Function ReportMissingOrBadRuntimeHelper(node As BoundNode, wellKnownMember As WellKnownMember, memberSymbol As Symbol, diagnostics As DiagnosticBag, embedVBCoreRuntime As Boolean) As Boolean
            If memberSymbol Is Nothing Then
                ReportMissingRuntimeHelper(node, wellKnownMember, diagnostics, embedVBCoreRuntime)
                Return True
            Else
                Dim useSiteError = If(memberSymbol.GetUseSiteErrorInfo(), memberSymbol.ContainingType.GetUseSiteErrorInfo())
                If useSiteError IsNot Nothing Then
                    ReportDiagnostic(node, useSiteError, diagnostics)
                    Return True
                End If
            End If
            Return False
        End Function

        Private Shared Sub ReportMissingRuntimeHelper(node As BoundNode, wellKnownMember As WellKnownMember, diagnostics As DiagnosticBag, embedVBCoreRuntime As Boolean)
            Dim descriptor = WellKnownMembers.GetDescriptor(wellKnownMember)

            ' TODO: If the type is generic, we might want to use VB style name rather than emitted name.
            Dim typeName As String = descriptor.DeclaringTypeMetadataName
            Dim memberName As String = descriptor.Name

            ReportMissingRuntimeHelper(node, typeName, memberName, diagnostics, embedVBCoreRuntime)
        End Sub


        Private Shared Sub ReportMissingRuntimeHelper(node As BoundNode, typeName As String, memberName As String, diagnostics As DiagnosticBag, embedVBCoreRuntime As Boolean)
            If memberName.Equals(WellKnownMemberNames.InstanceConstructorName) OrElse memberName.Equals(WellKnownMemberNames.StaticConstructorName) Then
                memberName = "New"
            End If

            Dim diag As DiagnosticInfo
            diag = GetDiagnosticForMissingRuntimeHelper(typeName, memberName, embedVBCoreRuntime)
            ReportDiagnostic(node, diag, diagnostics)
        End Sub

        Private Shared Sub ReportDiagnostic(node As BoundNode, diagnostic As DiagnosticInfo, diagnostics As DiagnosticBag)
            diagnostics.Add(New VBDiagnostic(diagnostic, node.Syntax.GetLocation()))
        End Sub

        Private Sub ReportBadType(node As BoundNode, typeSymbol As TypeSymbol)
            Dim useSiteError = typeSymbol.GetUseSiteErrorInfo()
            If useSiteError IsNot Nothing Then
                ReportDiagnostic(node, useSiteError, Me._diagnostics)
            End If
        End Sub
        ''
        '' The following nodes should be removed from the tree.
        ''
        Public Overrides Function VisitMethodGroup(node As BoundMethodGroup) As BoundNode
            Return Nothing
        End Function

        Public Overrides Function VisitParenthesized(node As BoundParenthesized) As BoundNode
            Debug.Assert(Not node.IsLValue)

            Dim enclosed = VisitExpressionNode(node.Expression)

            If enclosed.IsLValue Then
                enclosed = enclosed.MakeRValue()
            End If

            Return enclosed
        End Function

        ''' <summary>
        ''' If value is const, returns the value unchanged.
        ''' 
        ''' In a case if value is not a const, a proxy temp is created and added to "locals"
        ''' In addition to that, code that evaluates and stores the value is added to "expressions"
        ''' The access expression to the proxy temp is returned.
        ''' </summary>
        Private Shared Function CacheToLocalIfNotConst(container As Symbol,
                                                       value As BoundExpression,
                                                       locals As ArrayBuilder(Of LocalSymbol),
                                                       expressions As ArrayBuilder(Of BoundExpression),
                                                       kind As SynthesizedLocalKind,
                                                       syntaxOpt As StatementSyntax) As BoundExpression

            Debug.Assert(container IsNot Nothing)
            Debug.Assert(locals IsNot Nothing)
            Debug.Assert(expressions IsNot Nothing)
            Debug.Assert(kind = SynthesizedLocalKind.LoweringTemp OrElse syntaxOpt IsNot Nothing)

            Dim constValue As ConstantValue = value.ConstantValueOpt

            If constValue IsNot Nothing Then
                If Not value.Type.IsDecimalType() Then
                    Return value
                End If

                Select Case constValue.DecimalValue
                    Case Decimal.MinusOne, Decimal.Zero, Decimal.One
                        Return value
                End Select
            End If

            Dim local = New SynthesizedLocal(container, value.Type, kind, syntaxOpt)

            locals.Add(local)

            Dim localAccess = New BoundLocal(value.Syntax, local, local.Type)

            Dim valueStore = New BoundAssignmentOperator(
                                    value.Syntax,
                                    localAccess,
                                    value,
                                    suppressObjectClone:=True,
                                    type:=localAccess.Type
                                ).MakeCompilerGenerated

            expressions.Add(valueStore)
            Return localAccess.MakeRValue()
        End Function

        ''' <summary>
        ''' Helper method to create a bound sequence to represent the idea:
        ''' "compute this value, and then compute this side effects while discarding results"
        '''
        ''' A Bound sequence is generated for the provided expr and side-effects, say {se1, se2, se3}, as follows:
        '''
        ''' If expr is of void type:
        '''     BoundSequence { side-effects: { expr, se1, se2, se3 }, valueOpt: Nothing }
        ''' 
        ''' ElseIf expr is a constant:
        '''     BoundSequence { side-effects: { se1, se2, se3 }, valueOpt: expr }
        ''' 
        ''' Else
        '''     BoundSequence { side-effects: { tmp = expr, se1, se2, se3 }, valueOpt: tmp }
        ''' </summary>
        ''' <remarks>
        ''' NOTE: Supporting cases where side-effects change the value (or to detect such cases)
        ''' NOTE: could be complicated. We do not support this currently and instead require
        ''' NOTE: value expr to be not LValue.
        ''' </remarks>
        Friend Shared Function GenerateSequenceValueSideEffects(container As Symbol,
                                                                value As BoundExpression,
                                                                temporaries As ImmutableArray(Of LocalSymbol),
                                                                sideEffects As ImmutableArray(Of BoundExpression)) As BoundExpression
            Debug.Assert(container IsNot Nothing)
            Debug.Assert(Not value.IsLValue)
            Debug.Assert(value.Type IsNot Nothing)

            Dim syntax = value.Syntax
            Dim type = value.Type

            Dim temporariesBuilder = ArrayBuilder(Of LocalSymbol).GetInstance
            If Not temporaries.IsEmpty Then
                temporariesBuilder.AddRange(temporaries)
            End If

            Dim sideEffectsBuilder = ArrayBuilder(Of BoundExpression).GetInstance
            Dim valueOpt As BoundExpression
            If type.SpecialType = SpecialType.System_Void Then
                sideEffectsBuilder.Add(value)
                valueOpt = Nothing
            Else
                valueOpt = CacheToLocalIfNotConst(container, value, temporariesBuilder, sideEffectsBuilder, SynthesizedLocalKind.LoweringTemp, syntaxOpt:=Nothing)
                Debug.Assert(Not valueOpt.IsLValue)
            End If

            If Not sideEffects.IsDefaultOrEmpty Then
                sideEffectsBuilder.AddRange(sideEffects)
            End If

            Return New BoundSequence(syntax,
                                     locals:=temporariesBuilder.ToImmutableAndFree(),
                                     sideEffects:=sideEffectsBuilder.ToImmutableAndFree(),
                                     valueOpt:=valueOpt,
                                     type:=type)
        End Function

        ''' <summary>
        ''' Helper function that visits the given expression and returns a BoundExpression.
        ''' Please use this instead of DirectCast(Visit(expression), BoundExpression)
        ''' </summary>
        Private Function VisitExpressionNode(expression As BoundExpression) As BoundExpression
            Return DirectCast(Visit(expression), BoundExpression)
        End Function

        Public Overrides Function VisitAwaitOperator(node As BoundAwaitOperator) As BoundNode
            If Not _inExpressionLambda Then

                ' Await operator expression will be rewritten in AsyncRewriter, to do 
                ' so we need to keep placeholders unchanged in the bound Await operator

                Dim awaiterInstancePlaceholder As BoundLValuePlaceholder = node.AwaiterInstancePlaceholder
                Dim awaitableInstancePlaceholder As BoundRValuePlaceholder = node.AwaitableInstancePlaceholder

                Debug.Assert(awaiterInstancePlaceholder IsNot Nothing)
                Debug.Assert(awaitableInstancePlaceholder IsNot Nothing)

#If DEBUG Then
                Me.AddPlaceholderReplacement(awaiterInstancePlaceholder,
                                             awaiterInstancePlaceholder.MemberwiseClone(Of BoundExpression))
                Me.AddPlaceholderReplacement(awaitableInstancePlaceholder,
                                             awaitableInstancePlaceholder.MemberwiseClone(Of BoundExpression))
#Else
                Me.AddPlaceholderReplacement(awaiterInstancePlaceholder, awaiterInstancePlaceholder)
                Me.AddPlaceholderReplacement(awaitableInstancePlaceholder, awaitableInstancePlaceholder)
#End If

                Dim result = MyBase.VisitAwaitOperator(node)

                Me.RemovePlaceholderReplacement(awaiterInstancePlaceholder)
                Me.RemovePlaceholderReplacement(awaitableInstancePlaceholder)

                Return result
            End If

            Return node
        End Function

        Public Overrides Function VisitStopStatement(node As BoundStopStatement) As BoundNode
            Dim nodeFactory As New SyntheticBoundNodeFactory(_topMethod, _currentMethodOrLambda, node.Syntax, _compilationState, _diagnostics)
            Dim break As MethodSymbol = nodeFactory.WellKnownMember(Of MethodSymbol)(WellKnownMember.System_Diagnostics_Debugger__Break)

            Dim rewritten As BoundStatement = node

            If break IsNot Nothing Then
                ' Later in the codegen phase (see EmitExpression.vb), we need to insert a nop after the call to System.Diagnostics.Debugger.Break(),
                ' so the debugger can determine the current instruction pointer properly. In oder to do so, we do not mark this node as compiler generated.
                Dim boundNode = New BoundCall(nodeFactory.Syntax, break, Nothing, Nothing, ImmutableArray(Of BoundExpression).Empty, Nothing, True, break.ReturnType)
                rewritten = boundNode.ToStatement()
            End If

            If ShouldGenerateUnstructuredExceptionHandlingResumeCode(node) Then
                rewritten = RegisterUnstructuredExceptionHandlingResumeTarget(node.Syntax, rewritten, canThrow:=True)
            End If

            Return MarkStatementWithSequencePoint(rewritten)
        End Function

        Public Overrides Function VisitEndStatement(node As BoundEndStatement) As BoundNode
            Dim nodeFactory As New SyntheticBoundNodeFactory(_topMethod, _currentMethodOrLambda, node.Syntax, _compilationState, _diagnostics)
            Dim endApp As MethodSymbol = nodeFactory.WellKnownMember(Of MethodSymbol)(WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__EndApp)

            Dim rewritten As BoundStatement = node

            If endApp IsNot Nothing Then
                rewritten = nodeFactory.Call(Nothing, endApp, ImmutableArray(Of BoundExpression).Empty).ToStatement()
            End If

            Return MarkStatementWithSequencePoint(rewritten)
        End Function

        Public Overrides Function VisitGetType(node As BoundGetType) As BoundNode
            Dim result = DirectCast(MyBase.VisitGetType(node), BoundGetType)

            ' Emit needs this method.
            If Not TryGetWellknownMember(Of MethodSymbol)(Nothing, WellKnownMember.System_Type__GetTypeFromHandle, node.Syntax) Then
                Return New BoundGetType(result.Syntax, result.SourceType, result.Type, hasErrors:=True)
            End If

            Return result
        End Function

        Public Overrides Function VisitArrayCreation(node As BoundArrayCreation) As BoundNode
            ' Drop ArrayLiteralOpt
            Return MyBase.VisitArrayCreation(node.Update(node.IsParamArrayArgument,
                                                         node.Bounds,
                                                         node.InitializerOpt,
                                                         Nothing,
                                                         Nothing,
                                                         node.Type))
        End Function

    End Class
End Namespace
