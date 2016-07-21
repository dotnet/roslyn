// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This type provides means for instrumenting compiled methods for dynamic analysis.
    /// It can be combined with other <see cref="Instrumenter"/>s.
    /// </summary>
    internal sealed class DynamicAnalysisInjector : CompoundInstrumenter
    {
        private readonly MethodSymbol _method;
        private readonly BoundStatement _methodBody;
        private readonly MethodSymbol _createPayload;
        private readonly ArrayBuilder<SourceSpan> _spansBuilder;
        private ImmutableArray<SourceSpan> _dynamicAnalysisSpans = ImmutableArray<SourceSpan>.Empty;
        private readonly BoundStatement _methodEntryInstrumentation;
        private readonly ArrayTypeSymbol _payloadType;
        private readonly LocalSymbol _methodPayload;
        private readonly DiagnosticBag _diagnostics;
        private readonly DebugDocumentProvider _debugDocumentProvider;
        private readonly bool _methodHasExplicitBlock;
        private readonly SyntheticBoundNodeFactory _methodBodyFactory;

        public static DynamicAnalysisInjector TryCreate(MethodSymbol method, BoundStatement methodBody, SyntheticBoundNodeFactory methodBodyFactory, DiagnosticBag diagnostics, DebugDocumentProvider debugDocumentProvider, Instrumenter previous)
        {
            // Do not instrument implicitly-declared methods.
            if (!method.IsImplicitlyDeclared)
            {
                MethodSymbol createPayload = GetCreatePayload(methodBodyFactory.Compilation, methodBody.Syntax, diagnostics);

                // Do not instrument any methods if CreatePayload is not present.
                // Do not instrument CreatePayload if it is part of the current compilation (which occurs only during testing).
                // CreatePayload will fail at run time with an infinite recursion if it Is instrumented.
                if ((object)createPayload != null && !method.Equals(createPayload))
                {
                    return new DynamicAnalysisInjector(method, methodBody, methodBodyFactory, createPayload, diagnostics, debugDocumentProvider, previous);
                }
            }

            return null;
        }

        private DynamicAnalysisInjector(MethodSymbol method, BoundStatement methodBody, SyntheticBoundNodeFactory methodBodyFactory, MethodSymbol createPayload, DiagnosticBag diagnostics, DebugDocumentProvider debugDocumentProvider, Instrumenter previous)
            : base(previous)
        {
            _createPayload = createPayload;
            _method = method;
            _methodBody = methodBody;
            _spansBuilder = ArrayBuilder<SourceSpan>.GetInstance();
            TypeSymbol payloadElementType = methodBodyFactory.SpecialType(SpecialType.System_Boolean);
            _payloadType = ArrayTypeSymbol.CreateCSharpArray(methodBodyFactory.Compilation.Assembly, payloadElementType);
            _methodPayload = methodBodyFactory.SynthesizedLocal(_payloadType, kind: SynthesizedLocalKind.InstrumentationPayload, syntax: methodBody.Syntax);
            _diagnostics = diagnostics;
            _debugDocumentProvider = debugDocumentProvider;
            _methodHasExplicitBlock = MethodHasExplicitBlock(method);
            _methodBodyFactory = methodBodyFactory;

            // The first point indicates entry into the method and has the span of the method definition.
            _methodEntryInstrumentation = AddAnalysisPoint(MethodDeclarationIfAvailable(methodBody.Syntax), methodBodyFactory);
        }

        public override BoundStatement CreateBlockPrologue(BoundBlock original, out LocalSymbol synthesizedLocal)
        {
            BoundStatement previousPrologue = base.CreateBlockPrologue(original, out synthesizedLocal);
            if (_methodBody == original)
            {
                _dynamicAnalysisSpans = _spansBuilder.ToImmutableAndFree();
                // In the future there will be multiple analysis kinds.
                const int analysisKind = 0;

                ArrayTypeSymbol modulePayloadType = ArrayTypeSymbol.CreateCSharpArray(_methodBodyFactory.Compilation.Assembly, _payloadType);

                // Synthesize the initialization of the instrumentation payload array, using concurrency-safe code:
                //
                // var payload = PID.PayloadRootField[methodIndex];
                // if (payload == null)
                //     payload = Instrumentation.CreatePayload(mvid, methodIndex, fileIndex, ref PID.PayloadRootField[methodIndex], payloadLength);

                BoundStatement payloadInitialization = _methodBodyFactory.Assignment(_methodBodyFactory.Local(_methodPayload), _methodBodyFactory.ArrayAccess(_methodBodyFactory.InstrumentationPayloadRoot(analysisKind, modulePayloadType), ImmutableArray.Create(_methodBodyFactory.MethodDefIndex(_method))));
                BoundExpression mvid = _methodBodyFactory.ModuleVersionId();
                BoundExpression methodToken = _methodBodyFactory.MethodDefIndex(_method);
                BoundExpression fileIndex = _methodBodyFactory.SourceDocumentIndex(GetSourceDocument(_methodBody.Syntax));
                BoundExpression payloadSlot = _methodBodyFactory.ArrayAccess(_methodBodyFactory.InstrumentationPayloadRoot(analysisKind, modulePayloadType), ImmutableArray.Create(_methodBodyFactory.MethodDefIndex(_method)));
                BoundStatement createPayloadCall = _methodBodyFactory.Assignment(_methodBodyFactory.Local(_methodPayload), _methodBodyFactory.Call(null, _createPayload, mvid, methodToken, fileIndex, payloadSlot, _methodBodyFactory.Literal(_dynamicAnalysisSpans.Length)));

                BoundExpression payloadNullTest = _methodBodyFactory.Binary(BinaryOperatorKind.ObjectEqual, _methodBodyFactory.SpecialType(SpecialType.System_Boolean), _methodBodyFactory.Local(_methodPayload), _methodBodyFactory.Null(_payloadType));
                BoundStatement payloadIf = _methodBodyFactory.If(payloadNullTest, createPayloadCall);

                Debug.Assert(synthesizedLocal == null);
                synthesizedLocal = _methodPayload;

                ArrayBuilder<BoundStatement> prologueStatements = ArrayBuilder<BoundStatement>.GetInstance(previousPrologue == null ? 3 : 4);
                prologueStatements.Add(payloadInitialization);
                prologueStatements.Add(payloadIf);
                prologueStatements.Add(_methodEntryInstrumentation);
                if (previousPrologue != null)
                {
                    prologueStatements.Add(previousPrologue);
                }

                return _methodBodyFactory.StatementList(prologueStatements.ToImmutableAndFree());
            }

            return previousPrologue;
        }

        public ImmutableArray<SourceSpan> DynamicAnalysisSpans => _dynamicAnalysisSpans;

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
            return AddDynamicAnalysis(original, base.InstrumentExpressionStatement(original, rewritten));
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
            // Instrument the statement using a factory with the same syntax as the statement, so that the instrumentation appears to be part of the statement.
            SyntheticBoundNodeFactory statementFactory = new SyntheticBoundNodeFactory(_method, original.Syntax, _methodBodyFactory.CompilationState, _diagnostics);
            return statementFactory.StatementList(AddAnalysisPoint(SyntaxForSpan(original), statementFactory), rewritten);
        }

        private Cci.DebugSourceDocument GetSourceDocument(CSharpSyntaxNode syntax)
        {
            return GetSourceDocument(syntax, syntax.GetLocation().GetMappedLineSpan());
        }

        private Cci.DebugSourceDocument GetSourceDocument(CSharpSyntaxNode syntax, FileLinePositionSpan span)
        {
            string path = span.Path;
            // If the path for the syntax node is empty, try the path for the entire syntax tree.
            if (path.Length == 0)
            {
                path = syntax.SyntaxTree.FilePath;
            }

            return _debugDocumentProvider.Invoke(path, basePath: "");
        }

        private BoundStatement AddAnalysisPoint(CSharpSyntaxNode syntaxForSpan, SyntheticBoundNodeFactory statementFactory)
        {
            // Add an entry in the spans array.

            FileLinePositionSpan spanPosition = syntaxForSpan.GetLocation().GetMappedLineSpan();
            int spansIndex = _spansBuilder.Count;
            _spansBuilder.Add(new SourceSpan(GetSourceDocument(syntaxForSpan, spanPosition), spanPosition.StartLinePosition.Line, spanPosition.StartLinePosition.Character, spanPosition.EndLinePosition.Line, spanPosition.EndLinePosition.Character));

            // Generate "_payload[pointIndex] = true".

            BoundArrayAccess payloadCell = statementFactory.ArrayAccess(statementFactory.Local(_methodPayload), statementFactory.Literal(spansIndex));
            return statementFactory.Assignment(payloadCell, statementFactory.Literal(true));
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
        
        private static bool MethodHasExplicitBlock(MethodSymbol method)
        {
            SourceMethodSymbol asSourceMethod = method.OriginalDefinition as SourceMethodSymbol;
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

        private static CSharpSyntaxNode MethodDeclarationIfAvailable(CSharpSyntaxNode body)
        {
            CSharpSyntaxNode parent = body.Parent;
            if (parent != null)
            {
                switch (parent.Kind())
                {
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.PropertyDeclaration:
                    case SyntaxKind.GetAccessorDeclaration:
                    case SyntaxKind.SetAccessorDeclaration:
                        return parent;
                }
            }

            return body;
        }
    }
}