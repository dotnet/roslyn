// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class Instrumentation
    {
        private static MethodSymbol _createPayload = null;
        private static MethodSymbol _flushPayload = null;

        internal static BoundBlock InjectInstrumentation(MethodSymbol method, BoundBlock methodBody, int methodOrdinal, TypeCompilationState compilationState, CSharpCompilation compilation, DiagnosticBag diagnostics, DebugDocumentProvider debugDocumentProvider, out ImmutableArray<SourceSpan> dynamicAnalysisSpans)
        {
            if (methodBody != null && method.Name != "CreatePayload" && method.Name != "FlushPayload")
            {
                MethodSymbol createPayload = GetCreatePayload(compilation);
                MethodSymbol flushPayload = GetFlushPayload(compilation);
                if (createPayload != null && flushPayload != null)
                {
                    // Create the symbol for the instrumentation payload.
                    SyntheticBoundNodeFactory factory = new SyntheticBoundNodeFactory(method, methodBody.Syntax, compilationState, diagnostics);
                    TypeSymbol boolType = compilation.GetSpecialType(SpecialType.System_Boolean);
                    TypeSymbol payloadElementType = boolType;
                    ArrayTypeSymbol payloadType = ArrayTypeSymbol.CreateCSharpArray(compilation.Assembly, payloadElementType);
                    FieldSymbol payloadField = GetPayloadField(method, methodOrdinal, payloadType, factory, compilation);

                    // Synthesize the instrumentation and collect the spans of interest.

                    ArrayBuilder<SourceSpan> spansBuilder = ArrayBuilder<SourceSpan>.GetInstance();
                    BoundTreeRewriter collector = new InstrumentationInjectionWalker(method, spansBuilder, payloadField, compilationState, diagnostics, debugDocumentProvider);
                    BoundBlock newMethodBody = (BoundBlock)collector.Visit(methodBody);

                    dynamicAnalysisSpans = spansBuilder.ToImmutableAndFree();

                    // Synthesize the initialization of the instrumentation payload array. It should be done either statically or with concurrency-safe code:
                    //
                    // if (payloadField == null)
                    //     Instrumentation.CreatePayload(mvid, method, ref payloadField, payloadLength);

                    // ToDo: The containing module's mvid should be computed statically and stored in a static field rather than being
                    // recomputed in each method prologue.
                    BoundExpression mvid = factory.Property(factory.Property(factory.TypeofBeforeRewriting(method.ContainingType), "Module"), "ModuleVersionId");
                    BoundExpression methodToken = factory.MethodToken(method);
                    BoundStatement createPayloadCall = factory.ExpressionStatement(factory.Call(null, createPayload, mvid, methodToken, factory.Field(null, payloadField), factory.Literal(dynamicAnalysisSpans.Length)));

                    BoundExpression payloadNullTest = factory.Binary(BinaryOperatorKind.ObjectEqual, boolType, factory.Field(null, payloadField), factory.Null(payloadType));
                    BoundStatement payloadIf = factory.If(payloadNullTest, createPayloadCall);

                    ImmutableArray<BoundStatement> newStatements = newMethodBody.Statements.Insert(0, payloadIf);
                    newMethodBody = newMethodBody.Update(newMethodBody.Locals, newMethodBody.LocalFunctions, newStatements);

                    if (IsTestMethod(method))
                    {
                        // If the method is a test method, wrap the body in:
                        //
                        // Instrumentation.FlushPayload();
                        // try
                        // {
                        //     ... body ...
                        // }
                        // finally
                        // {
                        //     Instrumentation.FlushPayload();
                        // }

                        BoundStatement firstFlush = factory.ExpressionStatement(factory.Call(null, flushPayload));
                        BoundStatement secondFlush = factory.ExpressionStatement(factory.Call(null, flushPayload));
                        BoundStatement tryFinally = factory.Try(newMethodBody, ImmutableArray<BoundCatchBlock>.Empty, factory.Block(ImmutableArray.Create(secondFlush)));
                        newMethodBody = factory.Block(ImmutableArray.Create(firstFlush, tryFinally));
                    }

                    return newMethodBody;
                }
            }

            dynamicAnalysisSpans = ImmutableArray<SourceSpan>.Empty;
            return methodBody;
        }

        private static FieldSymbol GetPayloadField(MethodSymbol method, int methodOrdinal, TypeSymbol payloadType, SyntheticBoundNodeFactory factory, CSharpCompilation compilation)
        {
            // If the type containing the method is generic, synthesize a helper type and put the payload field there.
            // If the payload field is part of a generic type, there will be a new instance of the field per instantiation of the generic,
            // and so the payload field must be a member of another type.
            NamedTypeSymbol containingType = method.ContainingType;

            SynthesizedFieldSymbol payloadField = new SynthesizedFieldSymbol(containingType, payloadType, method.Name + "*instrumentation*" + methodOrdinal.ToString(), isStatic: true);
            factory.AddField(containingType, payloadField);
#if false
            SynthesizedFieldSymbol typeField = null;
            string typeFieldName = containingType.Name + "*type";
            ImmutableArray<Symbol> typeFields = containingType.GetMembers(typeFieldName);
            if (typeFields.Length == 0)
            {
                typeField = new SynthesizedFieldSymbol(containingType, compilation.GetWellKnownType(WellKnownType.System_Type), typeFieldName);
                factory.AddField(containingType, typeField);
            }
            else if (typeFields.Length == 1)
            {
                typeField = typeFields[0] as SynthesizedFieldSymbol;
            }
#endif   
            return payloadField;
        }
        
        private static MethodSymbol GetCreatePayload(CSharpCompilation compilation)
        {
            if (_createPayload == null)
            {
                NamedTypeSymbol instrumentationType = compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.Runtime.Instrumentation");
                if (instrumentationType != null)
                {
                    ImmutableArray<Symbol> createPayloads = instrumentationType.GetMembers("CreatePayload");
                    if (createPayloads.Length == 1)
                    {
                        MethodSymbol createPayload = createPayloads[0] as MethodSymbol;
                        // Add checks for parameter types.
                        if (createPayload != null && createPayload.IsStatic && createPayload.ParameterCount == 4 && createPayload.Parameters[0].Name == "mvid" && createPayload.Parameters[1].Name == "methodToken" && createPayload.Parameters[2].Name == "payload" && createPayload.Parameters[3].Name == "payloadLength")
                        {
                            _createPayload = createPayload;
                        }
                    }
                }
            }

            return _createPayload;
        }

        private static MethodSymbol GetFlushPayload(CSharpCompilation compilation)
        {
            if (_flushPayload == null)
            {
                NamedTypeSymbol instrumentationType = compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.Runtime.Instrumentation");
                if (instrumentationType != null)
                {
                    ImmutableArray<Symbol> flushPayloads = instrumentationType.GetMembers("FlushPayload");
                    if (flushPayloads.Length == 1)
                    {
                        MethodSymbol flushPayload = flushPayloads[0] as MethodSymbol;
                        if (flushPayload != null && flushPayload.IsStatic && flushPayload.ParameterCount == 0)
                        {
                            _flushPayload = flushPayload;
                        }
                    }
                }
            }

            return _flushPayload;
        }

        private static bool IsTestMethod(MethodSymbol method)
        {
            return method.Name.StartsWith("Test");
        }
    }

    internal sealed class InstrumentationInjectionWalker : BoundTreeRewriterWithStackGuard
    {
        private readonly MethodSymbol _method;
        private readonly ArrayBuilder<SourceSpan> _spansBuilder;
        private readonly FieldSymbol _payload;
        private readonly TypeCompilationState _compilationState;
        private readonly DiagnosticBag _diagnostics;
        private readonly DebugDocumentProvider _debugDocumentProvider;

        public InstrumentationInjectionWalker(MethodSymbol method, ArrayBuilder<SourceSpan> spansBuilder, FieldSymbol payload, TypeCompilationState compilationState, DiagnosticBag diagnostics, DebugDocumentProvider debugDocumentProvider)
        {
            _method = method;
            _spansBuilder = spansBuilder;
            _payload = payload;
            _compilationState = compilationState;
            _diagnostics = diagnostics;
            _debugDocumentProvider = debugDocumentProvider;
        }

        public override BoundNode VisitBadStatement(BoundBadStatement node)
        {
            return CollectDynamicAnalysis(base.VisitBadStatement(node));
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            return CollectDynamicAnalysis(base.VisitBlock(node));
        }

        public override BoundNode VisitBreakStatement(BoundBreakStatement node)
        {
            return CollectDynamicAnalysis(base.VisitBreakStatement(node));
        }

        public override BoundNode VisitContinueStatement(BoundContinueStatement node)
        {
            return CollectDynamicAnalysis(base.VisitContinueStatement(node));
        }

        public override BoundNode VisitDoStatement(BoundDoStatement node)
        {
            return CollectDynamicAnalysis(base.VisitDoStatement(node));
        }

        public override BoundNode VisitExpressionStatement(BoundExpressionStatement node)
        {
            return CollectDynamicAnalysis(base.VisitExpressionStatement(node));
        }

        public override BoundNode VisitFixedStatement(BoundFixedStatement node)
        {
            return CollectDynamicAnalysis(base.VisitFixedStatement(node));
        }

        public override BoundNode VisitForEachStatement(BoundForEachStatement node)
        {
            return CollectDynamicAnalysis(base.VisitForEachStatement(node));
        }

        public override BoundNode VisitForStatement(BoundForStatement node)
        {
            return CollectDynamicAnalysis(base.VisitForStatement(node));
        }

        public override BoundNode VisitGotoStatement(BoundGotoStatement node)
        {
            return CollectDynamicAnalysis(base.VisitGotoStatement(node));
        }

        public override BoundNode VisitIfStatement(BoundIfStatement node)
        {
            return CollectDynamicAnalysis(base.VisitIfStatement(node));
        }

        public override BoundNode VisitLabeledStatement(BoundLabeledStatement node)
        {
            // This construct can be ignored in favor of the underlying statement.
            return base.VisitLabeledStatement(node);
        }

        public override BoundNode VisitLabelStatement(BoundLabelStatement node)
        {
            return CollectDynamicAnalysis(base.VisitLabelStatement(node));
        }

        public override BoundNode VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            return CollectDynamicAnalysis(base.VisitLocalDeclaration(node));
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            return CollectDynamicAnalysis(base.VisitLocalFunctionStatement(node));
        }

        public override BoundNode VisitLockStatement(BoundLockStatement node)
        {
            return CollectDynamicAnalysis(base.VisitLockStatement(node));
        }

        public override BoundNode VisitMultipleLocalDeclarations(BoundMultipleLocalDeclarations node)
        {
            return CollectDynamicAnalysis(base.VisitMultipleLocalDeclarations(node));
        }

        public override BoundNode VisitNoOpStatement(BoundNoOpStatement node)
        {
            return CollectDynamicAnalysis(base.VisitNoOpStatement(node));
        }

        public override BoundNode VisitReturnStatement(BoundReturnStatement node)
        {
            return CollectDynamicAnalysis(base.VisitReturnStatement(node));
        }

        public override BoundNode VisitSequencePointWithSpan(BoundSequencePointWithSpan node)
        {
            // This construct can be ignored in favor of the underlying statement.
            return base.VisitSequencePointWithSpan(node);
        }

        public override BoundNode VisitStatementList(BoundStatementList node)
        {
            return CollectDynamicAnalysis(base.VisitStatementList(node));
        }

        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            return CollectDynamicAnalysis(base.VisitSwitchStatement(node));
        }

        public override BoundNode VisitThrowStatement(BoundThrowStatement node)
        {
            return CollectDynamicAnalysis(base.VisitThrowStatement(node));
        }

        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            return CollectDynamicAnalysis(base.VisitTryStatement(node));
        }

        public override BoundNode VisitUsingStatement(BoundUsingStatement node)
        {
            return CollectDynamicAnalysis(base.VisitUsingStatement(node));
        }

        public override BoundNode VisitWhileStatement(BoundWhileStatement node)
        {
            return CollectDynamicAnalysis(base.VisitWhileStatement(node));
        }

        public override BoundNode VisitYieldBreakStatement(BoundYieldBreakStatement node)
        {
            return CollectDynamicAnalysis(base.VisitYieldBreakStatement(node));
        }

        public override BoundNode VisitYieldReturnStatement(BoundYieldReturnStatement node)
        {
            return CollectDynamicAnalysis(base.VisitYieldReturnStatement(node));
        }

        private BoundNode CollectDynamicAnalysis(BoundNode node)
        {
            BoundStatement statement = node as BoundStatement;
            if (statement != null)
            {
                return CollectDynamicAnalysis(statement);
            }

            return node;
        }

        private BoundNode CollectDynamicAnalysis(BoundStatement statement)
        {
            if (statement.WasCompilerGenerated)
            {
                return statement;
            }

            Location statementLocation = statement.Syntax.GetLocation();
            FileLinePositionSpan lineSpan = statementLocation.GetMappedLineSpan();
            string path = lineSpan.Path;
            if (path == "")
            {
                path = statement.Syntax.SyntaxTree.FilePath;
            }

            int spansIndex = _spansBuilder.Count;
            _spansBuilder.Add(new SourceSpan(_debugDocumentProvider.Invoke(path, ""), lineSpan.StartLinePosition.Line, lineSpan.StartLinePosition.Character, lineSpan.EndLinePosition.Line, lineSpan.EndLinePosition.Character));

            // Generate "_payload[pointIndex] = true".

            SyntheticBoundNodeFactory statementFactory = new SyntheticBoundNodeFactory(_method, statement.Syntax, _compilationState, _diagnostics);
            BoundArrayAccess payloadCell = statementFactory.ArrayAccess(statementFactory.Field(null, _payload), statementFactory.Literal(spansIndex));
            BoundExpressionStatement cellAssignment = statementFactory.Assignment(payloadCell, statementFactory.Literal(true));
            
            return statementFactory.Block(ImmutableArray.Create(cellAssignment, statement));
        }
    }
}
