﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports System.Collections.Immutable
Imports System.Diagnostics

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' This type provides means for instrumenting compiled methods for dynamic analysis.
    ''' It can be combined with other <see cref= "Instrumenter"/>s.
    ''' </summary>
    Friend NotInheritable Class DynamicAnalysisInjector
        Inherits CompoundInstrumenter

        Private ReadOnly _method As MethodSymbol
        Private ReadOnly _methodBody As BoundStatement
        Private ReadOnly _createPayload As MethodSymbol
        Private ReadOnly _spansBuilder As ArrayBuilder(Of SourceSpan)
        Private _dynamicAnalysisSpans As ImmutableArray(Of SourceSpan) = ImmutableArray(Of SourceSpan).Empty
        Private ReadOnly _methodEntryInstrumentation As BoundStatement
        Private ReadOnly _payloadType As ArrayTypeSymbol
        Private ReadOnly _methodPayload As LocalSymbol
        Private ReadOnly _diagnostics As DiagnosticBag
        Private ReadOnly _debugDocumentProvider As DebugDocumentProvider
        Private ReadOnly _methodBodyFactory As SyntheticBoundNodeFactory

        Public Shared Function TryCreate(method As MethodSymbol, methodBody As BoundStatement, methodBodyFactory As SyntheticBoundNodeFactory, diagnostics As DiagnosticBag, debugDocumentProvider As DebugDocumentProvider, previous As Instrumenter) As DynamicAnalysisInjector
            ' Do not instrument implicitly-declared methods, except for constructors.
            ' Instrument implicit constructors in order to instrument member initializers.
            If Not method.IsImplicitlyDeclared OrElse method.IsAnyConstructor Then
                Dim createPayload As MethodSymbol = GetCreatePayload(methodBodyFactory.Compilation, methodBody.Syntax, diagnostics)

                ' Do not instrument any methods if CreatePayload is not present.
                ' Do not instrument CreatePayload if it is part of the current compilation (which occurs only during testing).
                ' Do not instrument if the syntax node does not have a valid mapped line span.
                ' CreatePayload will fail at run time with an infinite recursion if it Is instrumented.
                If createPayload IsNot Nothing AndAlso Not method.Equals(createPayload) AndAlso HasValidMappedLineSpan(methodBody.Syntax) Then
                    Return New DynamicAnalysisInjector(method, methodBody, methodBodyFactory, createPayload, diagnostics, debugDocumentProvider, previous)
                End If
            End If

            Return Nothing
        End Function

        Private Shared Function HasValidMappedLineSpan(syntax As SyntaxNode) As Boolean
            Return syntax.GetLocation().GetMappedLineSpan().IsValid
        End Function

        Public ReadOnly Property DynamicAnalysisSpans As ImmutableArray(Of SourceSpan)
            Get
                Return _dynamicAnalysisSpans
            End Get
        End Property

        Private Sub New(method As MethodSymbol, methodBody As BoundStatement, methodBodyFactory As SyntheticBoundNodeFactory, createPayload As MethodSymbol, diagnostics As DiagnosticBag, debugDocumentProvider As DebugDocumentProvider, previous As Instrumenter)
            MyBase.New(previous)
            _createPayload = createPayload
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

        Public Overrides Function CreateBlockPrologue(trueOriginal As BoundBlock, original As BoundBlock, ByRef synthesizedLocal As LocalSymbol) As BoundStatement
            Dim previousPrologue As BoundStatement = MyBase.CreateBlockPrologue(trueOriginal, original, synthesizedLocal)

            If _methodBody Is trueOriginal Then
                _dynamicAnalysisSpans = _spansBuilder.ToImmutableAndFree()
                ' In the future there will be multiple analysis kinds.
                Const analysisKind As Integer = 0

                Dim modulePayloadType As ArrayTypeSymbol = ArrayTypeSymbol.CreateVBArray(_payloadType, ImmutableArray(Of CustomModifier).Empty, 1, _methodBodyFactory.Compilation.Assembly)

                ' Synthesize the initialization of the instrumentation payload array, using concurrency-safe code
                '
                ' Dim payload = PID.PayloadRootField(methodIndex)
                ' If payload Is Nothing Then
                '     payload = Instrumentation.CreatePayload(mvid, methodIndex, fileIndex, PID.PayloadRootField(methodIndex), payloadLength)
                ' End If

                Dim payloadInitialization As BoundStatement = _methodBodyFactory.Assignment(_methodBodyFactory.Local(_methodPayload, isLValue:=True), _methodBodyFactory.ArrayAccess(_methodBodyFactory.InstrumentationPayloadRoot(analysisKind, modulePayloadType, isLValue:=False), isLValue:=False, indices:=ImmutableArray.Create(_methodBodyFactory.MethodDefIndex(_method))))
                Dim mvid As BoundExpression = _methodBodyFactory.ModuleVersionId(isLValue:=False)
                Dim methodToken As BoundExpression = _methodBodyFactory.MethodDefIndex(_method)
                Dim fileIndex As BoundExpression = _methodBodyFactory.SourceDocumentIndex(GetSourceDocument(_methodBody.Syntax))
                Dim payloadSlot As BoundExpression = _methodBodyFactory.ArrayAccess(_methodBodyFactory.InstrumentationPayloadRoot(analysisKind, modulePayloadType, isLValue:=False), isLValue:=False, indices:=ImmutableArray.Create(_methodBodyFactory.MethodDefIndex(_method)))
                Dim createPayloadCall As BoundStatement = _methodBodyFactory.Assignment(_methodBodyFactory.Local(_methodPayload, isLValue:=True), _methodBodyFactory.Call(Nothing, _createPayload, mvid, methodToken, fileIndex, payloadSlot, _methodBodyFactory.Literal(_dynamicAnalysisSpans.Length)))

                Dim payloadNullTest As BoundExpression = _methodBodyFactory.Binary(BinaryOperatorKind.Equals, _methodBodyFactory.SpecialType(SpecialType.System_Boolean), _methodBodyFactory.Local(_methodPayload, False), _methodBodyFactory.Null(_payloadType))
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

        Private Function GetSourceDocument(syntax As SyntaxNode) As Cci.DebugSourceDocument
            Return GetSourceDocument(syntax, syntax.GetLocation().GetMappedLineSpan())
        End Function

        Private Function GetSourceDocument(syntax As SyntaxNode, span As FileLinePositionSpan) As Cci.DebugSourceDocument
            Dim path As String = span.Path
            ' If the path for the syntax node is empty, try the path for the entire syntax tree.
            If path.Length = 0 Then
                path = syntax.SyntaxTree.FilePath
            End If

            Return _debugDocumentProvider.Invoke(path, basePath:="")
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
            _spansBuilder.Add(New SourceSpan(GetSourceDocument(syntaxForSpan, span), span.StartLinePosition.Line, span.StartLinePosition.Character, span.EndLinePosition.Line, span.EndLinePosition.Character))

            ' Generate "_payload(pointIndex) = True".
            Dim payloadCell As BoundArrayAccess = statementFactory.ArrayAccess(statementFactory.Local(_methodPayload, isLValue:=False), isLValue:=True, indices:=ImmutableArray.Create(Of BoundExpression)(statementFactory.Literal(spansIndex)))
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

        Private Shared Function GetCreatePayload(compilation As VisualBasicCompilation, syntax As SyntaxNode, diagnostics As DiagnosticBag) As MethodSymbol
            Return DirectCast(Binder.GetWellKnownTypeMember(compilation, WellKnownMember.Microsoft_CodeAnalysis_Runtime_Instrumentation__CreatePayload, syntax, diagnostics), MethodSymbol)
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
