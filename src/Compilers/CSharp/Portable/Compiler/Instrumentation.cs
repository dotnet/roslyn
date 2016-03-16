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
    internal static class Instrumentation
    {
        private static MethodSymbol _createPayload = null;
        private static bool _triedCreatePayload = false;
        private static MethodSymbol _flushPayload = null;
        private static bool _triedFlushPayload = false;

        internal static BoundBlock InjectInstrumentation(MethodSymbol method, BoundBlock methodBody, int methodOrdinal, TypeCompilationState compilationState, CSharpCompilation compilation, DiagnosticBag diagnostics, DebugDocumentProvider debugDocumentProvider, out ImmutableArray<SourceSpan> dynamicAnalysisSpans)
        {
            if (methodBody != null)
            {
                MethodSymbol createPayload = GetCreatePayload(compilation, diagnostics);
                MethodSymbol flushPayload = GetFlushPayload(compilation, diagnostics);

                // Do not instrument the instrumentation helpers if they are part of the current compilation (which occurs only during testing). GetCreatePayload will fail with an infinite recursion if it is instrumented.
                if ((object)createPayload != null && (object)flushPayload != null && (object)method != createPayload && (object)method != flushPayload)
                {
                    // Create the symbol for the instrumentation payload.
                    SyntheticBoundNodeFactory factory = new SyntheticBoundNodeFactory(method, methodBody.Syntax, compilationState, diagnostics);
                    TypeSymbol boolType = compilation.GetSpecialType(SpecialType.System_Boolean);
                    TypeSymbol payloadElementType = boolType;
                    ArrayTypeSymbol payloadType = ArrayTypeSymbol.CreateCSharpArray(compilation.Assembly, payloadElementType);
                    FieldSymbol payloadField = GetPayloadField(method, methodOrdinal, payloadType, factory, compilation);

                    // Synthesize the instrumentation and collect the spans of interest.

                    BoundBlock newMethodBody;
                    InstrumentationInjectionRewriter.InstrumentMethod(method, methodBody, payloadField, compilationState, diagnostics, debugDocumentProvider, out dynamicAnalysisSpans, out newMethodBody);

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
            // ToDo: If the type containing the method is generic, synthesize a helper type and put the payload field there.
            // If the payload field is part of a generic type, there will be a new instance of the field per instantiation of the generic,
            // and so the payload field must be a member of another type.
            NamedTypeSymbol containingType = method.ContainingType;

            SynthesizedFieldSymbol payloadField = new SynthesizedFieldSymbol(containingType, payloadType, GeneratedNames.MakeSynthesizedInstrumentationPayloadFieldName(method, methodOrdinal), isStatic: true);
            factory.AddField(containingType, payloadField);
            return payloadField;
        }
        
        private static MethodSymbol GetInstrumentationHelper(CSharpCompilation compilation, WellKnownMember member, DiagnosticBag diagnostics)
        {
            MethodSymbol helper = compilation.GetWellKnownTypeMember(member) as MethodSymbol;

            if ((object)helper == null)
            {
                RuntimeMembers.MemberDescriptor memberDescriptor = WellKnownMembers.GetDescriptor(member);
                diagnostics.Add(Diagnostic.Create(new CSDiagnosticInfo(ErrorCode.ERR_MissingPredefinedMember, memberDescriptor.DeclaringTypeMetadataName, memberDescriptor.Name)));
            }

            return helper;
        }

        private static MethodSymbol GetCreatePayload(CSharpCompilation compilation, DiagnosticBag diagnostics)
        {
            if (!_triedCreatePayload)
            {
                _createPayload = GetInstrumentationHelper(compilation, WellKnownMember.Microsoft_CodeAnalysis_Runtime_Instrumentation__CreatePayload, diagnostics);
                _triedCreatePayload = true;
            }

            return _createPayload;
        }

        private static MethodSymbol GetFlushPayload(CSharpCompilation compilation, DiagnosticBag diagnostics)
        {
            if (!_triedFlushPayload)
            {
                _flushPayload = GetInstrumentationHelper(compilation, WellKnownMember.Microsoft_CodeAnalysis_Runtime_Instrumentation__FlushPayload, diagnostics);
                _triedFlushPayload = true;
            }

            return _flushPayload;
        }

        private static bool IsTestMethod(MethodSymbol method)
        {
            // ToDo: Make this real.
            return method.Name.StartsWith("Test");
        }
    }

    internal sealed class InstrumentationInjectionRewriter : BoundTreeRewriterWithStackGuard
    {
        private readonly MethodSymbol _method;
        private readonly ArrayBuilder<SourceSpan> _spansBuilder;
        private readonly FieldSymbol _payload;
        private readonly TypeCompilationState _compilationState;
        private readonly DiagnosticBag _diagnostics;
        private readonly DebugDocumentProvider _debugDocumentProvider;

        public static void InstrumentMethod(MethodSymbol method, BoundBlock methodBody, FieldSymbol payloadField, TypeCompilationState compilationState, DiagnosticBag diagnostics, DebugDocumentProvider debugDocumentProvider, out ImmutableArray<SourceSpan> dynamicAnalysisSpans, out BoundBlock newMethodBody)
        {
            ArrayBuilder<SourceSpan> spansBuilder = ArrayBuilder<SourceSpan>.GetInstance();
            BoundTreeRewriter collector = new InstrumentationInjectionRewriter(method, spansBuilder, payloadField, compilationState, diagnostics, debugDocumentProvider);
            newMethodBody = (BoundBlock)collector.Visit(methodBody);
            dynamicAnalysisSpans = spansBuilder.ToImmutableAndFree();
        }

        private InstrumentationInjectionRewriter(MethodSymbol method, ArrayBuilder<SourceSpan> spansBuilder, FieldSymbol payload, TypeCompilationState compilationState, DiagnosticBag diagnostics, DebugDocumentProvider debugDocumentProvider)
        {
            _method = method;
            _spansBuilder = spansBuilder;
            _payload = payload;
            _compilationState = compilationState;
            _diagnostics = diagnostics;
            _debugDocumentProvider = debugDocumentProvider;
        }

        public override BoundNode Visit(BoundNode node)
        {
            BoundStatement statement = node as BoundStatement;
            if (statement != null)
            {
                switch (statement.Kind)
                {
                    case BoundKind.SwitchSection:
                    case BoundKind.SwitchLabel:
                    case BoundKind.PatternSwitchSection:
                    case BoundKind.PatternSwitchLabel:
                    case BoundKind.CatchBlock:
                    // A labeled statement or a sequence point can be ignored with respect to instrumentation in favor of the underlying statement.
                    case BoundKind.SequencePoint:
                    case BoundKind.SequencePointWithSpan:
                    case BoundKind.LabeledStatement:
                        break;
                    default:
                        return CollectDynamicAnalysis(base.Visit(node));
                }
            }

            return base.Visit(node);
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
            if (path.Length == 0)
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
