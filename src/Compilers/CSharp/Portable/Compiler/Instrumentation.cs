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
        private static MethodSymbol _compareExchange = null;
        private static MethodSymbol _addPayload = null;

        internal static BoundBlock InjectInstrumentation(MethodSymbol method, BoundBlock methodBody, int methodOrdinal, TypeCompilationState compilationState, CSharpCompilation compilation, DiagnosticBag diagnostics, DebugDocumentProvider debugDocumentProvider, out ImmutableArray<SourceSpan> dynamicAnalysisSpans)
        {
            if (methodBody != null)
            {
                // Create the symbol for the instrumentation payload.
                SyntheticBoundNodeFactory factory = new SyntheticBoundNodeFactory(method, methodBody.Syntax, compilationState, diagnostics);
                TypeSymbol boolType = compilation.GetSpecialType(SpecialType.System_Boolean);
                TypeSymbol payloadElementType = boolType;
                ArrayTypeSymbol payloadType = ArrayTypeSymbol.CreateCSharpArray(compilation.Assembly, payloadElementType);
                FieldSymbol payloadField = GetPayloadField(method, methodOrdinal, payloadType, factory);

                // Synthesize the instrumentation and collect the spans of interest.

                ArrayBuilder<SourceSpan> spansBuilder = new ArrayBuilder<SourceSpan>();
                BoundTreeRewriter collector = new InstrumentationInjectionWalker(method, spansBuilder, payloadField, compilationState, diagnostics, debugDocumentProvider);
                BoundBlock newMethodBody = (BoundBlock)collector.Visit(methodBody);

                dynamicAnalysisSpans = spansBuilder.ToImmutableAndFree();

                // Synthesize the initialization of the instrumentation payload array. It should be done either statically or with concurrency-safe code:
                //
                // if (payloadField == null)
                //     if (Interlocked.CompareExchange(ref payloadField, new PayloadType[] { default0, default1, ... defaultN }, null) == null)
                //         Instrumentation.AddPayload(method, payloadField);
                //
                // Or, less correctly, if Interlocked.CompareExchange is not available:
                //
                // if (payloadField == null)
                // {
                //     payloadField = new PayloadType[] { default0, default1, ... defaultN };
                //     Instrumentation.AddPayload(method, payloadField);
                // }

                ArrayBuilder<BoundExpression> elementsBuilder = new ArrayBuilder<BoundExpression>(dynamicAnalysisSpans.Length);
                for (int i = 0; i < dynamicAnalysisSpans.Length; i++)
                {
                    elementsBuilder.Add(factory.Literal(false));
                }
                BoundExpression payloadArrayCreation = factory.Array(payloadElementType, elementsBuilder.ToImmutableAndFree());

                BoundStatement addPayloadCall = null;
                MethodSymbol addPayload = GetAddPayload(compilation);
                if (addPayload != null)
                {
                    // The method information is probably better expressed as a method token rather than as a MethodImfo -- figure out how to emit such a thing.
                    BoundExpression methodInformation = factory.MethodInfo(method);
                    addPayloadCall = factory.ExpressionStatement(factory.Call(null, addPayload, methodInformation, factory.Field(null, payloadField)));
                }
                else
                {
                    addPayloadCall = factory.NoOp(NoOpStatementFlavor.Default);
                }

                BoundStatement payloadStatement;
                MethodSymbol CompareExchange = GetCompareExchange(compilation, payloadType);
                if (CompareExchange != null)
                {
                    BoundCall interlockedExchangeCall = factory.Call(null, CompareExchange, ImmutableArray.Create(RefKind.Ref, RefKind.None, RefKind.None), ImmutableArray.Create(factory.Field(null, payloadField), payloadArrayCreation, factory.Null(payloadType)));
                    BoundExpression interlockedExchangeComparison = factory.Binary(BinaryOperatorKind.ObjectEqual, boolType, interlockedExchangeCall, factory.Null(compilation.ObjectType));
                    payloadStatement = factory.If(interlockedExchangeComparison, addPayloadCall);
                }
                else
                {
                    BoundStatement payloadAssignment = factory.Assignment(factory.Field(null, payloadField), payloadArrayCreation);
                    payloadStatement = factory.Block(ImmutableArray.Create(payloadAssignment, addPayloadCall));
                }
                
                BoundExpression payloadNullTest = factory.Binary(BinaryOperatorKind.ObjectEqual, boolType, factory.Field(null, payloadField), factory.Null(payloadType));
                BoundStatement payloadIf = factory.If(payloadNullTest, payloadStatement);

                ImmutableArray<BoundStatement> newStatements = newMethodBody.Statements.Insert(0, payloadIf);
                newMethodBody = newMethodBody.Update(newMethodBody.Locals, newMethodBody.LocalFunctions, newStatements);

                return newMethodBody;
            }

            dynamicAnalysisSpans = ImmutableArray<SourceSpan>.Empty;
            return methodBody;
        }

        private static FieldSymbol GetPayloadField(MethodSymbol method, int methodOrdinal, TypeSymbol payloadType, SyntheticBoundNodeFactory factory)
        {
            // If the type containing the method is generic, synthesize a helper type and put the payload field there.
            // If the payload field is part of a generic type, there will be a new instance of the field per instantiation of the generic,
            // and so the payload field must be a member of another type.

            SynthesizedFieldSymbol payloadField = new SynthesizedFieldSymbol(method.ContainingType, payloadType, method.Name + "*instrumentation*" + methodOrdinal.ToString(), isStatic: true);
            factory.AddField(method.ContainingType, payloadField);

            return payloadField;
        }

        private static MethodSymbol GetCompareExchange(CSharpCompilation compilation, TypeSymbol payloadType)
        {
            if (_compareExchange == null)
            {
                NamedTypeSymbol interlocked = compilation.GetTypeByMetadataName("System.Threading.Interlocked");
                if (interlocked != null)
                {
                    ImmutableArray<Symbol> compareExchanges = interlocked.GetMembers("CompareExchange");
                    if (compareExchanges.Length > 0)
                    {
                        foreach (Symbol candidate in compareExchanges)
                        {
                            MethodSymbol candidateMethod = candidate as MethodSymbol;
                            if (candidateMethod != null)
                            {
                                // Add some more checks to make this more robust.
                                if (candidateMethod.ParameterCount == 3 && candidateMethod.IsGenericMethod && candidateMethod.ReturnType.TypeKind == TypeKind.TypeParameter)
                                {
                                    _compareExchange = candidateMethod.Construct(ImmutableArray.Create(payloadType));
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return _compareExchange;
        }

        private static MethodSymbol GetAddPayload(CSharpCompilation compilation)
        {
            if (_addPayload == null)
            {
                NamedTypeSymbol instrumentationType = compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.Runtime.Instrumentation");
                if (instrumentationType != null)
                {
                    ImmutableArray<Symbol> addPayloads = instrumentationType.GetMembers("AddPayload");
                    if (addPayloads.Length == 1)
                    {
                        MethodSymbol addPayload = addPayloads[1] as MethodSymbol;
                        // Add checks for parameter types.
                        if (addPayload != null && addPayload.IsStatic && addPayload.ParameterCount == 2 && addPayload.Parameters[0].Name == "method" && addPayload.Parameters[2].Name == "newPayload")
                        {
                            _addPayload = addPayload;
                        }
                    }
                }
            }

            return _addPayload;
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
