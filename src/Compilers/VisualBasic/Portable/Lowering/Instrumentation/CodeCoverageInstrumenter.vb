' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' This type provides means for instrumenting compiled methods for dynamic analysis.
    ''' It can be combined with other <see cref= "Instrumenter"/>s.
    ''' </summary>
    Friend NotInheritable Class CodeCoverageInstrumenter
        Inherits CompoundInstrumenter

        Private ReadOnly _method As MethodSymbol
        Private ReadOnly _methodBody As BoundStatement
        Private ReadOnly _createPayloadForMethodsSpanningSingleFile As MethodSymbol
        Private ReadOnly _createPayloadForMethodsSpanningMultipleFiles As MethodSymbol
        Private ReadOnly _spansBuilder As ArrayBuilder(Of SourceSpan)
        Private _dynamicAnalysisSpans As ImmutableArray(Of SourceSpan) = ImmutableArray(Of SourceSpan).Empty
        Private ReadOnly _methodEntryInstrumentation As BoundStatement
        Private ReadOnly _payloadType As ArrayTypeSymbol
        Private ReadOnly _methodPayload As LocalSymbol
        Private ReadOnly _diagnostics As BindingDiagnosticBag
        Private ReadOnly _debugDocumentProvider As DebugDocumentProvider
        Private ReadOnly _methodBodyFactory As SyntheticBoundNodeFactory

        Public Shared Function TryCreate(
            method As MethodSymbol,
            methodBody As BoundStatement,
            methodBodyFactory As SyntheticBoundNodeFactory,
            diagnostics As BindingDiagnosticBag,
            debugDocumentProvider As DebugDocumentProvider,
            previous As Instrumenter) As CodeCoverageInstrumenter

            ' Do not instrument implicitly-declared methods, except for constructors.
            ' Instrument implicit constructors in order to instrument member initializers.
            If method.IsImplicitlyDeclared AndAlso Not method.IsAnyConstructor Then
                Return Nothing
            End If

            ' Do not instrument if the syntax node does not have a valid mapped line span.
            If Not HasValidMappedLineSpan(methodBody.Syntax) Then
                Return Nothing
            End If

            ' Do not instrument methods marked with or in scope of ExcludeFromCodeCoverageAttribute
            If IsExcludedFromCodeCoverage(method) Then
                Return Nothing
            End If

            Dim createPayloadForMethodsSpanningSingleFile As MethodSymbol = GetCreatePayloadOverload(
                methodBodyFactory.Compilation,
                WellKnownMember.Microsoft_CodeAnalysis_Runtime_Instrumentation__CreatePayloadForMethodsSpanningSingleFile,
                methodBody.Syntax,
                diagnostics)

            Dim createPayloadForMethodsSpanningMultipleFiles As MethodSymbol = GetCreatePayloadOverload(
                methodBodyFactory.Compilation,
                WellKnownMember.Microsoft_CodeAnalysis_Runtime_Instrumentation__CreatePayloadForMethodsSpanningMultipleFiles,
                methodBody.Syntax,
                diagnostics)

            ' Do not instrument any methods if CreatePayload is not present.
            If createPayloadForMethodsSpanningSingleFile Is Nothing OrElse createPayloadForMethodsSpanningMultipleFiles Is Nothing Then
                Return Nothing
            End If

            ' Do not instrument CreatePayload if it is part of the current compilation (which occurs only during testing).
            ' CreatePayload will fail at run time with an infinite recursion if it is instrumented.
            If method.Equals(createPayloadForMethodsSpanningSingleFile) OrElse method.Equals(createPayloadForMethodsSpanningMultipleFiles) Then
                Return Nothing
            End If

            Return New CodeCoverageInstrumenter(
                method,
                methodBody,
                methodBodyFactory,
                createPayloadForMethodsSpanningSingleFile,
                createPayloadForMethodsSpanningMultipleFiles,
                diagnostics,
                debugDocumentProvider,
                previous)
        End Function

        Private Shared Function HasValidMappedLineSpan(syntax As SyntaxNode) As Boolean
            Return syntax.GetLocation().GetMappedLineSpan().IsValid
        End Function

        Public ReadOnly Property DynamicAnalysisSpans As ImmutableArray(Of SourceSpan)
            Get
                Return _dynamicAnalysisSpans
            End Get
        End Property

        Private Sub New(
            method As MethodSymbol,
            methodBody As BoundStatement,
            methodBodyFactory As SyntheticBoundNodeFactory,
            createPayloadForMethodsSpanningSingleFile As MethodSymbol,
            createPayloadForMethodsSpanningMultipleFiles As MethodSymbol,
            diagnostics As BindingDiagnosticBag,
            debugDocumentProvider As DebugDocumentProvider,
            previous As Instrumenter)

            MyBase.New(previous)
            _createPayloadForMethodsSpanningSingleFile = createPayloadForMethodsSpanningSingleFile
            _createPayloadForMethodsSpanningMultipleFiles = createPayloadForMethodsSpanningMultipleFiles
            _method = method
            _methodBody = methodBody
            _spansBuilder = ArrayBuilder(Of SourceSpan).GetInstance()
            Dim payloadElementType As TypeSymbol = methodBodyFactory.SpecialType(SpecialType.System_Boolean)
            _payloadType = ArrayTypeSymbol.CreateVBArray(payloadElementType, ImmutableArray(Of CustomModifier).Empty, 1, methodBodyFactory.Compilation.Assembly)
            _methodPayload = methodBodyFactory.SynthesizedLocal(_payloadType, kind:=SynthesizedLocalKind.InstrumentationPayload, syntax:=methodBody.Syntax)
            _diagnostics = diagnostics
            _debugDocumentProvider = debugDocumentProvider
            _methodBodyFactory = methodBodyFactory

            ' The first point indicates entry into the method and has the span of the method definition.
            Dim bodySyntax As SyntaxNode = methodBody.Syntax
            If Not method.IsImplicitlyDeclared Then
                _methodEntryInstrumentation = AddAnalysisPoint(bodySyntax, SkipAttributes(bodySyntax), methodBodyFactory)
            End If
        End Sub

        Private Shared Function IsExcludedFromCodeCoverage(method As MethodSymbol) As Boolean
            Dim containingType = method.ContainingType

            While containingType IsNot Nothing
                If containingType.IsDirectlyExcludedFromCodeCoverage Then
                    Return True
                End If

                containingType = containingType.ContainingType
            End While

            ' Skip lambdas. They can't have custom attributes.
            Dim nonLambda = method.ContainingNonLambdaMember()
            If nonLambda?.Kind = SymbolKind.Method Then
                method = DirectCast(nonLambda, MethodSymbol)

                If method.IsDirectlyExcludedFromCodeCoverage Then
                    Return True
                End If

                Dim associatedSymbol = method.AssociatedSymbol
                Select Case associatedSymbol?.Kind
                    Case SymbolKind.Property
                        If DirectCast(associatedSymbol, PropertySymbol).IsDirectlyExcludedFromCodeCoverage Then
                            Return True
                        End If

                    Case SymbolKind.Event
                        If DirectCast(associatedSymbol, EventSymbol).IsDirectlyExcludedFromCodeCoverage Then
                            Return True
                        End If
                End Select
            End If

            Return False
        End Function

        Private Shared Function GetCreatePayloadStatement(
            dynamicAnalysisSpans As ImmutableArray(Of SourceSpan),
            methodBodySyntax As SyntaxNode,
            methodPayload As LocalSymbol,
            createPayloadForMethodsSpanningSingleFile As MethodSymbol,
            createPayloadForMethodsSpanningMultipleFiles As MethodSymbol,
            mvid As BoundExpression,
            methodToken As BoundExpression,
            payloadSlot As BoundExpression,
            methodBodyFactory As SyntheticBoundNodeFactory,
            debugDocumentProvider As DebugDocumentProvider) As BoundExpressionStatement

            Dim createPayloadOverload As MethodSymbol
            Dim fileIndexOrIndicesArgument As BoundExpression

            If dynamicAnalysisSpans.IsEmpty Then
                createPayloadOverload = createPayloadForMethodsSpanningSingleFile

                ' For a compiler generated method that has no 'real' spans, we emit the index for
                ' the document corresponding to the syntax node that is associated with its bound node.
                Dim document = GetSourceDocument(debugDocumentProvider, methodBodySyntax)
                fileIndexOrIndicesArgument = methodBodyFactory.SourceDocumentIndex(document)
            Else
                Dim documents = PooledHashSet(Of DebugSourceDocument).GetInstance()
                Dim fileIndices = ArrayBuilder(Of BoundExpression).GetInstance()

                For Each span In dynamicAnalysisSpans
                    Dim document = span.Document
                    If documents.Add(document) Then
                        fileIndices.Add(methodBodyFactory.SourceDocumentIndex(document))
                    End If
                Next

                documents.Free()

                ' At this point, we should have at least one document since we have already
                ' handled the case where method has no 'real' spans (and therefore no documents) above.
                If fileIndices.Count = 1 Then
                    createPayloadOverload = createPayloadForMethodsSpanningSingleFile
                    fileIndexOrIndicesArgument = fileIndices.Single()
                Else
                    createPayloadOverload = createPayloadForMethodsSpanningMultipleFiles

                    ' Order of elements in fileIndices should be deterministic because these
                    ' elements were added based on order of spans in dynamicAnalysisSpans above.
                    fileIndexOrIndicesArgument = methodBodyFactory.Array(
                        methodBodyFactory.SpecialType(SpecialType.System_Int32), fileIndices.ToImmutable())
                End If

                fileIndices.Free()
            End If

            Return methodBodyFactory.Assignment(
                methodBodyFactory.Local(methodPayload, isLValue:=True),
                methodBodyFactory.Call(
                    Nothing,
                    createPayloadOverload,
                    mvid,
                    methodToken,
                    fileIndexOrIndicesArgument,
                    payloadSlot,
                    methodBodyFactory.Literal(dynamicAnalysisSpans.Length)))
        End Function

        Public Overrides Function CreateBlockPrologue(trueOriginal As BoundBlock, original As BoundBlock, ByRef synthesizedLocal As LocalSymbol) As BoundStatement
            Dim previousPrologue As BoundStatement = MyBase.CreateBlockPrologue(trueOriginal, original, synthesizedLocal)

            If _methodBody Is trueOriginal Then
                _dynamicAnalysisSpans = _spansBuilder.ToImmutableAndFree()
                ' In the future there will be multiple analysis kinds.
                Const analysisKind As Integer = 0

                Dim modulePayloadType As ArrayTypeSymbol =
                    ArrayTypeSymbol.CreateVBArray(_payloadType, ImmutableArray(Of CustomModifier).Empty, 1, _methodBodyFactory.Compilation.Assembly)

                ' Synthesize the initialization of the instrumentation payload array, using concurrency-safe code
                '
                ' Dim payload = PID.PayloadRootField(methodIndex)
                ' If payload Is Nothing Then
                '     payload = Instrumentation.CreatePayload(mvid, methodIndex, fileIndexOrIndices, PID.PayloadRootField(methodIndex), payloadLength)
                ' End If

                Dim payloadInitialization As BoundStatement =
                    _methodBodyFactory.Assignment(
                        _methodBodyFactory.Local(_methodPayload, isLValue:=True),
                        _methodBodyFactory.ArrayAccess(
                            _methodBodyFactory.InstrumentationPayloadRoot(analysisKind, modulePayloadType, isLValue:=False),
                            isLValue:=False,
                            indices:=ImmutableArray.Create(_methodBodyFactory.MethodDefIndex(_method))))

                Dim mvid As BoundExpression = _methodBodyFactory.ModuleVersionId(isLValue:=False)
                Dim methodToken As BoundExpression = _methodBodyFactory.MethodDefIndex(_method)

                Dim payloadSlot As BoundExpression =
                    _methodBodyFactory.ArrayAccess(
                        _methodBodyFactory.InstrumentationPayloadRoot(analysisKind, modulePayloadType, isLValue:=False),
                        isLValue:=True,
                        indices:=ImmutableArray.Create(_methodBodyFactory.MethodDefIndex(_method)))

                Dim createPayloadCall As BoundStatement =
                    GetCreatePayloadStatement(
                        _dynamicAnalysisSpans,
                        _methodBody.Syntax,
                        _methodPayload,
                        _createPayloadForMethodsSpanningSingleFile,
                        _createPayloadForMethodsSpanningMultipleFiles,
                        mvid,
                        methodToken,
                        payloadSlot,
                        _methodBodyFactory,
                        _debugDocumentProvider)

                Dim payloadNullTest As BoundExpression =
                    _methodBodyFactory.Binary(
                        BinaryOperatorKind.Equals,
                        _methodBodyFactory.SpecialType(SpecialType.System_Boolean),
                        _methodBodyFactory.Local(_methodPayload, False),
                        _methodBodyFactory.Null(_payloadType))

                Dim payloadIf As BoundStatement = _methodBodyFactory.If(payloadNullTest, createPayloadCall)

                Debug.Assert(synthesizedLocal Is Nothing)
                synthesizedLocal = _methodPayload

                Dim prologueStatements As ArrayBuilder(Of BoundStatement) = ArrayBuilder(Of BoundStatement).GetInstance(If(previousPrologue Is Nothing, 3, 4))
                prologueStatements.Add(payloadInitialization)
                prologueStatements.Add(payloadIf)
                If _methodEntryInstrumentation IsNot Nothing Then
                    prologueStatements.Add(_methodEntryInstrumentation)
                End If
                If previousPrologue IsNot Nothing Then
                    prologueStatements.Add(previousPrologue)
                End If

                Return _methodBodyFactory.StatementList(prologueStatements.ToImmutableAndFree())
            End If

            Return previousPrologue
        End Function

        Public Overrides Function InstrumentExpressionStatement(original As BoundExpressionStatement, rewritten As BoundStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentExpressionStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentStopStatement(original As BoundStopStatement, rewritten As BoundStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentStopStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentEndStatement(original As BoundEndStatement, rewritten As BoundStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentEndStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentContinueStatement(original As BoundContinueStatement, rewritten As BoundStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentContinueStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentExitStatement(original As BoundExitStatement, rewritten As BoundStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentExitStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentGotoStatement(original As BoundGotoStatement, rewritten As BoundStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentGotoStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentRaiseEventStatement(original As BoundRaiseEventStatement, rewritten As BoundStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentRaiseEventStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentReturnStatement(original As BoundReturnStatement, rewritten As BoundStatement) As BoundStatement
            Dim previous As BoundStatement = MyBase.InstrumentReturnStatement(original, rewritten)
            If Not original.IsEndOfMethodReturn Then
                If original.ExpressionOpt IsNot Nothing Then
                    ' Synthesized return statements require instrumentation if they return values,
                    ' e.g. in simple expression lambdas.
                    Return CollectDynamicAnalysis(original, previous)
                Else
                    Return AddDynamicAnalysis(original, previous)
                End If
            End If
            Return previous
        End Function

        Public Overrides Function InstrumentThrowStatement(original As BoundThrowStatement, rewritten As BoundStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentThrowStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentOnErrorStatement(original As BoundOnErrorStatement, rewritten As BoundStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentOnErrorStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentResumeStatement(original As BoundResumeStatement, rewritten As BoundStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentResumeStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentAddHandlerStatement(original As BoundAddHandlerStatement, rewritten As BoundStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentAddHandlerStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentRemoveHandlerStatement(original As BoundRemoveHandlerStatement, rewritten As BoundStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentRemoveHandlerStatement(original, rewritten))
        End Function

        Public Overrides Function InstrumentSyncLockObjectCapture(original As BoundSyncLockStatement, rewritten As BoundStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentSyncLockObjectCapture(original, rewritten))
        End Function

        Public Overrides Function InstrumentWhileStatementConditionalGotoStart(original As BoundWhileStatement, ifConditionGotoStart As BoundStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentWhileStatementConditionalGotoStart(original, ifConditionGotoStart))
        End Function

        Public Overrides Function InstrumentDoLoopStatementEntryOrConditionalGotoStart(original As BoundDoLoopStatement, ifConditionGotoStartOpt As BoundStatement) As BoundStatement
            Dim previous As BoundStatement = MyBase.InstrumentDoLoopStatementEntryOrConditionalGotoStart(original, ifConditionGotoStartOpt)
            If original.ConditionOpt IsNot Nothing Then
                Return AddDynamicAnalysis(original, previous)
            End If
            Return previous
        End Function

        Public Overrides Function InstrumentIfStatementConditionalGoto(original As BoundIfStatement, condGoto As BoundStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentIfStatementConditionalGoto(original, condGoto))
        End Function

        Public Overrides Function CreateSelectStatementPrologue(original As BoundSelectStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.CreateSelectStatementPrologue(original))
        End Function

        Public Overrides Function InstrumentFieldOrPropertyInitializer(original As BoundFieldOrPropertyInitializer, rewritten As BoundStatement, symbolIndex As Integer, createTemporary As Boolean) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentFieldOrPropertyInitializer(original, rewritten, symbolIndex, createTemporary))
        End Function

        Public Overrides Function InstrumentForEachLoopInitialization(original As BoundForEachStatement, initialization As BoundStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentForEachLoopInitialization(original, initialization))
        End Function

        Public Overrides Function InstrumentForLoopInitialization(original As BoundForToStatement, initialization As BoundStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentForLoopInitialization(original, initialization))
        End Function

        Public Overrides Function InstrumentLocalInitialization(original As BoundLocalDeclaration, rewritten As BoundStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.InstrumentLocalInitialization(original, rewritten))
        End Function

        Public Overrides Function CreateUsingStatementPrologue(original As BoundUsingStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.CreateUsingStatementPrologue(original))
        End Function

        Public Overrides Function CreateWithStatementPrologue(original As BoundWithStatement) As BoundStatement
            Return AddDynamicAnalysis(original, MyBase.CreateWithStatementPrologue(original))
        End Function

        Private Function AddDynamicAnalysis(original As BoundStatement, rewritten As BoundStatement) As BoundStatement
            If Not original.WasCompilerGenerated Then
                Return CollectDynamicAnalysis(original, rewritten)
            End If

            Return rewritten
        End Function

        Private Function CollectDynamicAnalysis(original As BoundStatement, rewritten As BoundStatement) As BoundStatement
            ' Instrument the statement using a factory with the same syntax as the statement, so that the instrumentation appears to be part of the statement.
            Dim statementFactory As New SyntheticBoundNodeFactory(_methodBodyFactory.TopLevelMethod, _method, original.Syntax, _methodBodyFactory.CompilationState, _diagnostics)
            Dim analysisPoint As BoundStatement = AddAnalysisPoint(SyntaxForSpan(original), statementFactory)
            Return If(rewritten IsNot Nothing, statementFactory.StatementList(analysisPoint, rewritten), analysisPoint)
        End Function

        Private Shared Function GetSourceDocument(debugDocumentProvider As DebugDocumentProvider, syntax As SyntaxNode) As Cci.DebugSourceDocument
            Return GetSourceDocument(debugDocumentProvider, syntax, syntax.GetLocation().GetMappedLineSpan())
        End Function

        Private Shared Function GetSourceDocument(debugDocumentProvider As DebugDocumentProvider, syntax As SyntaxNode, span As FileLinePositionSpan) As Cci.DebugSourceDocument
            Dim path As String = span.Path
            ' If the path for the syntax node is empty, try the path for the entire syntax tree.
            If path.Length = 0 Then
                path = syntax.SyntaxTree.FilePath
            End If

            Return debugDocumentProvider.Invoke(path, basePath:="")
        End Function

        Private Function AddAnalysisPoint(syntaxForSpan As SyntaxNode, alternateSpan As Text.TextSpan, statementFactory As SyntheticBoundNodeFactory) As BoundStatement
            Return AddAnalysisPoint(syntaxForSpan, syntaxForSpan.SyntaxTree.GetMappedLineSpan(alternateSpan), statementFactory)
        End Function

        Private Function AddAnalysisPoint(syntaxForSpan As SyntaxNode, statementFactory As SyntheticBoundNodeFactory) As BoundStatement
            Return AddAnalysisPoint(syntaxForSpan, syntaxForSpan.GetLocation().GetMappedLineSpan(), statementFactory)
        End Function

        Private Function AddAnalysisPoint(syntaxForSpan As SyntaxNode, span As FileLinePositionSpan, statementFactory As SyntheticBoundNodeFactory) As BoundStatement
            ' Add an entry in the spans array.
            Dim spansIndex As Integer = _spansBuilder.Count
            _spansBuilder.Add(New SourceSpan(
                GetSourceDocument(_debugDocumentProvider, syntaxForSpan, span),
                span.StartLinePosition.Line,
                span.StartLinePosition.Character,
                span.EndLinePosition.Line,
                span.EndLinePosition.Character))

            ' Generate "_payload(pointIndex) = True".
            Dim payloadCell As BoundArrayAccess =
                statementFactory.ArrayAccess(
                    statementFactory.Local(_methodPayload, isLValue:=False),
                    isLValue:=True,
                    indices:=ImmutableArray.Create(Of BoundExpression)(statementFactory.Literal(spansIndex)))

            Return statementFactory.Assignment(payloadCell, statementFactory.Literal(True))
        End Function

        Private Shared Function SyntaxForSpan(statement As BoundStatement) As SyntaxNode
            Select Case statement.Kind
                Case BoundKind.IfStatement
                    Return DirectCast(statement, BoundIfStatement).Condition.Syntax
                Case BoundKind.WhileStatement
                    Return DirectCast(statement, BoundWhileStatement).Condition.Syntax
                Case BoundKind.ForToStatement
                    Return DirectCast(statement, BoundForToStatement).InitialValue.Syntax
                Case BoundKind.ForEachStatement
                    Return DirectCast(statement, BoundForEachStatement).Collection.Syntax
                Case BoundKind.DoLoopStatement
                    Return DirectCast(statement, BoundDoLoopStatement).ConditionOpt.Syntax
                Case BoundKind.UsingStatement
                    Dim usingStatement As BoundUsingStatement = DirectCast(statement, BoundUsingStatement)
                    Return If(usingStatement.ResourceExpressionOpt, DirectCast(usingStatement, BoundNode)).Syntax
                Case BoundKind.SyncLockStatement
                    Return DirectCast(statement, BoundSyncLockStatement).LockExpression.Syntax
                Case BoundKind.SelectStatement
                    Return DirectCast(statement, BoundSelectStatement).ExpressionStatement.Expression.Syntax
                Case BoundKind.LocalDeclaration
                    Dim initializer As BoundExpression = DirectCast(statement, BoundLocalDeclaration).InitializerOpt
                    If initializer IsNot Nothing Then
                        Return initializer.Syntax
                    End If
                Case BoundKind.FieldInitializer, BoundKind.PropertyInitializer
                    Dim equalsValue = TryCast(statement.Syntax, EqualsValueSyntax)
                    If equalsValue IsNot Nothing Then
                        Return equalsValue.Value
                    End If
                    Dim asNew = TryCast(statement.Syntax, AsNewClauseSyntax)
                    If asNew IsNot Nothing Then
                        Return asNew._newExpression
                    End If
            End Select

            Return statement.Syntax
        End Function

        Private Shared Function GetCreatePayloadOverload(compilation As VisualBasicCompilation, overload As WellKnownMember, syntax As SyntaxNode, diagnostics As BindingDiagnosticBag) As MethodSymbol
            Return DirectCast(Binder.GetWellKnownTypeMember(compilation, overload, syntax, diagnostics), MethodSymbol)
        End Function

        ' If the method, property, etc. has attributes, the attributes are excluded from the span of the method definition.
        Private Shared Function SkipAttributes(syntax As SyntaxNode) As Text.TextSpan
            Select Case syntax.Kind()
                Case SyntaxKind.SubBlock, SyntaxKind.FunctionBlock
                    Dim methodSyntax As MethodStatementSyntax = DirectCast(syntax, MethodBlockSyntax).SubOrFunctionStatement
                    Return SkipAttributes(syntax, methodSyntax.AttributeLists, methodSyntax.Modifiers, methodSyntax.SubOrFunctionKeyword)

                Case SyntaxKind.PropertyBlock
                    Dim propertySyntax As PropertyStatementSyntax = DirectCast(syntax, PropertyBlockSyntax).PropertyStatement
                    Return SkipAttributes(syntax, propertySyntax.AttributeLists, propertySyntax.Modifiers, propertySyntax.PropertyKeyword)

                Case SyntaxKind.GetAccessorBlock, SyntaxKind.SetAccessorBlock
                    Dim accessorSyntax As AccessorStatementSyntax = DirectCast(syntax, AccessorBlockSyntax).AccessorStatement
                    Return SkipAttributes(syntax, accessorSyntax.AttributeLists, accessorSyntax.Modifiers, accessorSyntax.AccessorKeyword)

                Case SyntaxKind.ConstructorBlock
                    Dim constructorSyntax As SubNewStatementSyntax = DirectCast(syntax, ConstructorBlockSyntax).SubNewStatement
                    Return SkipAttributes(syntax, constructorSyntax.AttributeLists, constructorSyntax.Modifiers, constructorSyntax.SubKeyword)

                Case SyntaxKind.OperatorBlock
                    Dim operatorSyntax As OperatorStatementSyntax = DirectCast(syntax, OperatorBlockSyntax).OperatorStatement
                    Return SkipAttributes(syntax, operatorSyntax.AttributeLists, operatorSyntax.Modifiers, operatorSyntax.OperatorKeyword)
            End Select

            Return syntax.Span
        End Function

        Private Shared Function SkipAttributes(syntax As SyntaxNode, attributes As SyntaxList(Of AttributeListSyntax), modifiers As SyntaxTokenList, keyword As SyntaxToken) As Text.TextSpan
            Dim originalSpan As Text.TextSpan = syntax.Span
            If attributes.Count > 0 Then
                Dim startSpan As Text.TextSpan = If(modifiers.Node IsNot Nothing, modifiers.Span, keyword.Span)
                Return New Text.TextSpan(startSpan.Start, originalSpan.Length - (startSpan.Start - originalSpan.Start))
            End If

            Return originalSpan
        End Function
    End Class
End Namespace
