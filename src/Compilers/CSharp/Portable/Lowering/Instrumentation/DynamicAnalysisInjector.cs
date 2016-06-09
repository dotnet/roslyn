// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This type provides means for instrumenting compiled methods for dynamic analysis.
    /// It can be combined with other <see cref="Instrumenter"/>s.
    /// </summary>
    internal partial class DynamicAnalysisInjector : CompoundInstrumenter
    {
        private readonly bool _doInstrumentation;
        private readonly MethodSymbol _method;
        private readonly MethodSymbol _createPayload;
        private readonly ArrayBuilder<SourceSpan> _spansBuilder;
        private readonly ArrayTypeSymbol _payloadType;
        private readonly LocalSymbol _methodPayload;
        private readonly TypeCompilationState _compilationState;
        private readonly DiagnosticBag _diagnostics;
        private readonly DebugDocumentProvider _debugDocumentProvider;
        private readonly bool _methodHasExplicitBlock;
        private readonly SyntheticBoundNodeFactory _factory;

        public DynamicAnalysisInjector(MethodSymbol method, BoundStatement methodBody, SyntheticBoundNodeFactory factory, TypeCompilationState compilationState, DiagnosticBag diagnostics, DebugDocumentProvider debugDocumentProvider, Instrumenter previous)
            : base (previous)
        {
            CSharpCompilation compilation = compilationState.Compilation;
            _createPayload = GetCreatePayload(compilation, methodBody.Syntax, diagnostics);

            // Do not instrument the instrumentation helpers if they are part of the current compilation (which occurs only during testing). GetCreatePayload will fail with an infinite recursion if it is instrumented.
            // PROTOTYPE (https://github.com/dotnet/roslyn/issues/10266): It is not correct to always skip implict methods, because that will miss field initializers.
            if ((object)_createPayload != null && !method.IsImplicitlyDeclared && !method.Equals(_createPayload))
            {
                _doInstrumentation = true;
                _method = method;
                _spansBuilder = ArrayBuilder<SourceSpan>.GetInstance();
                TypeSymbol payloadElementType = factory.SpecialType(SpecialType.System_Boolean);
                _payloadType = ArrayTypeSymbol.CreateCSharpArray(compilation.Assembly, payloadElementType);
                _methodPayload = factory.SynthesizedLocal(_payloadType, kind: SynthesizedLocalKind.InstrumentationPayload, syntax: methodBody.Syntax); ;
                _compilationState = compilationState;
                _diagnostics = diagnostics;
                _debugDocumentProvider = debugDocumentProvider;
                _methodHasExplicitBlock = MethodHasExplicitBlock(method);
                _factory = factory;
            }
        }

        public BoundStatement AddInstrumentationPrologue(BoundBlock rewrittenMethodBody, ref ImmutableArray<SourceSpan> dynamicAnalysisSpans)
        {
            if (_doInstrumentation)
            {
                dynamicAnalysisSpans = _spansBuilder.ToImmutableAndFree();
                // PROTOTYPE (https://github.com/dotnet/roslyn/issues/10411): In the future there will be multiple analysis kinds.
                const int analysisKind = 0;

                ArrayTypeSymbol modulePayloadType = ArrayTypeSymbol.CreateCSharpArray(_compilationState.Compilation.Assembly, _payloadType);

                // Synthesize the initialization of the instrumentation payload array, using concurrency-safe code:
                //
                // var payload = PID.PayloadRootField[methodIndex];
                // if (payload == null)
                //     payload = Instrumentation.CreatePayload(mvid, methodIndex, ref PID.PayloadRootField[methodIndex], payloadLength);

                BoundStatement payloadInitialization = _factory.Assignment(_factory.Local(_methodPayload), _factory.ArrayAccess(_factory.InstrumentationPayloadRoot(analysisKind, modulePayloadType), ImmutableArray.Create(_factory.MethodDefIndex(_method))));
                BoundExpression mvid = _factory.ModuleVersionId();
                BoundExpression methodToken = _factory.MethodDefIndex(_method);
                BoundExpression payloadSlot = _factory.ArrayAccess(_factory.InstrumentationPayloadRoot(analysisKind, modulePayloadType), ImmutableArray.Create(_factory.MethodDefIndex(_method)));
                BoundStatement createPayloadCall = _factory.Assignment(_factory.Local(_methodPayload), _factory.Call(null, _createPayload, mvid, methodToken, payloadSlot, _factory.Literal(dynamicAnalysisSpans.Length)));

                BoundExpression payloadNullTest = _factory.Binary(BinaryOperatorKind.ObjectEqual, _factory.SpecialType(SpecialType.System_Boolean), _factory.Local(_methodPayload), _factory.Null(_payloadType));
                BoundStatement payloadIf = _factory.If(payloadNullTest, createPayloadCall);

                // There must be a sequence point before any executable code. A sequence point
                // won't be generated automatically for the synthesized prologue because the prologue is compiler generated, so
                // force the generation of a sequence point for the prologue.
                payloadInitialization = _factory.SequencePoint(payloadInitialization.Syntax, payloadInitialization);

                ImmutableArray<BoundStatement> newStatements = ImmutableArray.Create<BoundStatement>(payloadInitialization, payloadIf).AddRange(rewrittenMethodBody.Statements);
                rewrittenMethodBody = rewrittenMethodBody.Update(rewrittenMethodBody.Locals.Add(_methodPayload), rewrittenMethodBody.LocalFunctions, newStatements);
            }

            return rewrittenMethodBody;
        }

        public override BoundStatement InstrumentNoOpStatement(BoundNoOpStatement original, BoundStatement rewritten)
        {
            return AddDynamicAnalysis(original, base.InstrumentNoOpStatement(original, rewritten));
        }

        public override BoundStatement InstrumentBreakStatement(BoundBreakStatement original, BoundStatement rewritten)
        {
            return AddDynamicAnalysis(original, base.InstrumentBreakStatement(original, rewritten));
        }

        public override BoundStatement InstrumentContinueStatement(BoundContinueStatement original, BoundStatement rewritten)
        {
            return AddDynamicAnalysis(original, base.InstrumentContinueStatement(original, rewritten));
        }

        public override BoundStatement InstrumentExpressionStatement(BoundExpressionStatement original, BoundStatement rewritten)
        {
            rewritten = base.InstrumentExpressionStatement(original, rewritten);

            if (!_methodHasExplicitBlock)
            {
                // The assignment statement for a property set method defined without a block is compiler generated, but requires instrumentation.
                return CollectDynamicAnalysis(original, rewritten);
            }

            return AddDynamicAnalysis(original, rewritten);
        }

        public override BoundStatement InstrumentFieldOrPropertyInitializer(BoundExpressionStatement original, BoundStatement rewritten)
        {
            rewritten = base.InstrumentExpressionStatement(original, rewritten);
            CSharpSyntaxNode syntax = original.Syntax;

            switch (syntax.Parent.Parent.Kind())
            {
                case SyntaxKind.VariableDeclarator:
                case SyntaxKind.PropertyDeclaration:
                    return AddDynamicAnalysis(original, rewritten);

                default:
                    throw ExceptionUtilities.UnexpectedValue(syntax.Parent.Parent.Kind());
            }
        }

        public override BoundStatement InstrumentGotoStatement(BoundGotoStatement original, BoundStatement rewritten)
        {
            return AddDynamicAnalysis(original, base.InstrumentGotoStatement(original, rewritten));
        }

        public override BoundStatement InstrumentThrowStatement(BoundThrowStatement original, BoundStatement rewritten)
        {
            return AddDynamicAnalysis(original, base.InstrumentThrowStatement(original, rewritten));
        }

        public override BoundStatement InstrumentYieldBreakStatement(BoundYieldBreakStatement original, BoundStatement rewritten)
        {
            return AddDynamicAnalysis(original, base.InstrumentYieldBreakStatement(original, rewritten));
        }

        public override BoundStatement InstrumentYieldReturnStatement(BoundYieldReturnStatement original, BoundStatement rewritten)
        {
            return AddDynamicAnalysis(original, base.InstrumentYieldReturnStatement(original, rewritten));
        }

        public override BoundStatement InstrumentForEachStatementIterationVarDeclaration(BoundForEachStatement original, BoundStatement iterationVarDecl)
        {
            return AddDynamicAnalysis(original, base.InstrumentForEachStatementIterationVarDeclaration(original, iterationVarDecl));
        }
        
        public override BoundStatement InstrumentIfStatement(BoundIfStatement original, BoundStatement rewritten)
        {
            return AddDynamicAnalysis(original, base.InstrumentIfStatement(original, rewritten));
        }

        public override BoundStatement InstrumentWhileStatementConditionalGotoStart(BoundWhileStatement original, BoundStatement ifConditionGotoStart)
        {
            return AddDynamicAnalysis(original, base.InstrumentWhileStatementConditionalGotoStart(original, ifConditionGotoStart));
        }

        public override BoundStatement InstrumentLocalInitialization(BoundLocalDeclaration original, BoundStatement rewritten)
        {
            return AddDynamicAnalysis(original, base.InstrumentLocalInitialization(original, rewritten));
        }

        public override BoundStatement InstrumentLockTargetCapture(BoundLockStatement original, BoundStatement lockTargetCapture)
        {
            return AddDynamicAnalysis(original, base.InstrumentLockTargetCapture(original, lockTargetCapture));
        }

        public override BoundStatement InstrumentReturnStatement(BoundReturnStatement original, BoundStatement rewritten)
        {
            rewritten = base.InstrumentReturnStatement(original, rewritten);

            // A synthesized return statement that does not return a value never requires instrumentation.
            // A property set method defined without a block has such a synthesized return statement.
            if (!_methodHasExplicitBlock && ((BoundReturnStatement)original).ExpressionOpt != null)
            {
                // The return statement for value-returning methods defined without a block is compiler generated, but requires instrumentation.
                return CollectDynamicAnalysis(original, rewritten);
            }

            return AddDynamicAnalysis(original, rewritten);
        }

        public override BoundStatement InstrumentSwitchStatement(BoundSwitchStatement original, BoundStatement rewritten)
        {
            return AddDynamicAnalysis(original, base.InstrumentSwitchStatement(original, rewritten));
        }

        public override BoundStatement InstrumentUsingTargetCapture(BoundUsingStatement original, BoundStatement usingTargetCapture)
        {
            return AddDynamicAnalysis(original, base.InstrumentUsingTargetCapture(original, usingTargetCapture));
        }
        
        private BoundStatement AddDynamicAnalysis(BoundStatement original, BoundStatement rewritten)
        {
            if (!original.WasCompilerGenerated)
            {
                // Do not instrument implicit constructor initializers
                if (!original.IsConstructorInitializer() || original.Syntax.Kind() != SyntaxKind.ConstructorDeclaration)
                {
                    return CollectDynamicAnalysis(original, rewritten);
                }
            }

            return rewritten;
        }

        private BoundStatement CollectDynamicAnalysis(BoundStatement original, BoundStatement rewritten)
        {
            if (_doInstrumentation)
            {
                // Add an entry in the spans array.

                CSharpSyntaxNode syntaxForSpan = SyntaxForSpan(original);
                Location location = syntaxForSpan.GetLocation();
                FileLinePositionSpan spanPosition = location.GetMappedLineSpan();
                string path = spanPosition.Path;
                if (path.Length == 0)
                {
                    path = syntaxForSpan.SyntaxTree.FilePath;
                }

                int spansIndex = _spansBuilder.Count;
                _spansBuilder.Add(new SourceSpan(_debugDocumentProvider.Invoke(path, ""), spanPosition.StartLinePosition.Line, spanPosition.StartLinePosition.Character, spanPosition.EndLinePosition.Line, spanPosition.EndLinePosition.Character));

                // Generate "_payload[pointIndex] = true".

                SyntheticBoundNodeFactory statementFactory = new SyntheticBoundNodeFactory(_method, rewritten.Syntax, _compilationState, _diagnostics);
                BoundArrayAccess payloadCell = statementFactory.ArrayAccess(statementFactory.Local(_methodPayload), statementFactory.Literal(spansIndex));
                BoundExpressionStatement cellAssignment = statementFactory.Assignment(payloadCell, statementFactory.Literal(true));

                return statementFactory.Block(ImmutableArray.Create(cellAssignment, rewritten));
            }

            return rewritten;
        }

        private static CSharpSyntaxNode SyntaxForSpan(BoundStatement statement)
        {
            CSharpSyntaxNode syntaxForSpan;

            switch (statement.Kind)
            {
                case BoundKind.IfStatement:
                    syntaxForSpan = ((BoundIfStatement)statement).Condition.Syntax;
                    break;
                case BoundKind.WhileStatement:
                    syntaxForSpan = ((BoundWhileStatement)statement).Condition.Syntax;
                    break;
                case BoundKind.ForEachStatement:
                    // PROTOTYPE (https://github.com/dotnet/roslyn/issues/10141): Also include the declaration of the loop variable in the span.
                    syntaxForSpan = ((BoundForEachStatement)statement).Expression.Syntax;
                    break;
                case BoundKind.DoStatement:
                    syntaxForSpan = ((BoundDoStatement)statement).Condition.Syntax;
                    break;
                case BoundKind.UsingStatement:
                    {
                        BoundUsingStatement usingStatement = (BoundUsingStatement)statement;
                        syntaxForSpan = ((BoundNode)usingStatement.ExpressionOpt ?? usingStatement.DeclarationsOpt).Syntax;
                        break;
                    }
                case BoundKind.FixedStatement:
                    syntaxForSpan = ((BoundFixedStatement)statement).Declarations.Syntax;
                    break;
                case BoundKind.LockStatement:
                    syntaxForSpan = ((BoundLockStatement)statement).Argument.Syntax;
                    break;
                case BoundKind.SwitchStatement:
                    syntaxForSpan = ((BoundSwitchStatement)statement).Expression.Syntax;
                    break;
                default:
                    syntaxForSpan = statement.Syntax;
                    break;
            }

            return syntaxForSpan;
        }

        private static bool HasInitializer(BoundLocalDeclaration local)
        {
            return local.InitializerOpt != null;
        }

        private static bool HasInitializer(BoundMultipleLocalDeclarations multiple)
        {
            foreach (BoundLocalDeclaration local in multiple.LocalDeclarations)
            {
                if (HasInitializer(local))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MethodHasExplicitBlock(MethodSymbol method)
        {
            SourceMethodSymbol asSourceMethod = method.ConstructedFrom as SourceMethodSymbol;
            if ((object)asSourceMethod != null)
            {
                return asSourceMethod.BodySyntax is BlockSyntax;
            }

            return false;
        }

        private static MethodSymbol GetCreatePayload(CSharpCompilation compilation, CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
        {
            return (MethodSymbol)Binder.GetWellKnownTypeMember(compilation, WellKnownMember.Microsoft_CodeAnalysis_Runtime_Instrumentation__CreatePayload, diagnostics, syntax: syntax);
        }
    }
}