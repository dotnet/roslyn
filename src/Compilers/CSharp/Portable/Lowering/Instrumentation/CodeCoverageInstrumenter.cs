// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This type provides means for instrumenting compiled methods for dynamic analysis.
    /// It can be combined with other <see cref="Instrumenter"/>s.
    /// </summary>
    internal sealed class CodeCoverageInstrumenter : CompoundInstrumenter
    {
        private readonly MethodSymbol _method;
        private readonly BoundStatement _methodBody;
        private readonly MethodSymbol _createPayloadForMethodsSpanningSingleFile;
        private readonly MethodSymbol _createPayloadForMethodsSpanningMultipleFiles;
        private readonly ArrayBuilder<SourceSpan> _spansBuilder;
        private ImmutableArray<SourceSpan> _dynamicAnalysisSpans = ImmutableArray<SourceSpan>.Empty;
        private readonly BoundStatement? _methodEntryInstrumentation;
        private readonly ArrayTypeSymbol _payloadType;
        private readonly LocalSymbol _methodPayload;
        private readonly BindingDiagnosticBag _diagnostics;
        private readonly DebugDocumentProvider _debugDocumentProvider;
        private readonly SyntheticBoundNodeFactory _methodBodyFactory;

        public static bool TryCreate(
            MethodSymbol method,
            BoundStatement methodBody,
            SyntheticBoundNodeFactory methodBodyFactory,
            BindingDiagnosticBag diagnostics,
            DebugDocumentProvider debugDocumentProvider,
            Instrumenter previous,
            [NotNullWhen(true)] out CodeCoverageInstrumenter? instrumenter)
        {
            instrumenter = null;

            // Do not instrument implicitly-declared methods, except for constructors.
            // Instrument implicit constructors in order to instrument member initializers.
            if (method.IsImplicitlyDeclared && !method.IsImplicitConstructor)
            {
                return false;
            }

            // Do not instrument methods marked with or in scope of ExcludeFromCodeCoverageAttribute.
            if (IsExcludedFromCodeCoverage(method))
            {
                return false;
            }

            MethodSymbol createPayloadForMethodsSpanningSingleFile = GetCreatePayloadOverload(
                methodBodyFactory.Compilation,
                WellKnownMember.Microsoft_CodeAnalysis_Runtime_Instrumentation__CreatePayloadForMethodsSpanningSingleFile,
                methodBody.Syntax,
                diagnostics);

            MethodSymbol createPayloadForMethodsSpanningMultipleFiles = GetCreatePayloadOverload(
                methodBodyFactory.Compilation,
                WellKnownMember.Microsoft_CodeAnalysis_Runtime_Instrumentation__CreatePayloadForMethodsSpanningMultipleFiles,
                methodBody.Syntax,
                diagnostics);

            // Do not instrument any methods if CreatePayload is not present.
            if (createPayloadForMethodsSpanningSingleFile is null || createPayloadForMethodsSpanningMultipleFiles is null)
            {
                return false;
            }

            // Do not instrument CreatePayload if it is part of the current compilation (which occurs only during testing).
            // CreatePayload will fail at run time with an infinite recursion if it is instrumented.
            if (method.Equals(createPayloadForMethodsSpanningSingleFile) || method.Equals(createPayloadForMethodsSpanningMultipleFiles))
            {
                return false;
            }

            instrumenter = new CodeCoverageInstrumenter(
                method,
                methodBody,
                methodBodyFactory,
                createPayloadForMethodsSpanningSingleFile,
                createPayloadForMethodsSpanningMultipleFiles,
                diagnostics,
                debugDocumentProvider,
                previous);

            return true;
        }

        private CodeCoverageInstrumenter(
            MethodSymbol method,
            BoundStatement methodBody,
            SyntheticBoundNodeFactory methodBodyFactory,
            MethodSymbol createPayloadForMethodsSpanningSingleFile,
            MethodSymbol createPayloadForMethodsSpanningMultipleFiles,
            BindingDiagnosticBag diagnostics,
            DebugDocumentProvider debugDocumentProvider,
            Instrumenter previous) : base(previous)
        {
            _createPayloadForMethodsSpanningSingleFile = createPayloadForMethodsSpanningSingleFile;
            _createPayloadForMethodsSpanningMultipleFiles = createPayloadForMethodsSpanningMultipleFiles;
            _method = method;
            _methodBody = methodBody;
            _spansBuilder = ArrayBuilder<SourceSpan>.GetInstance();
            TypeSymbol payloadElementType = methodBodyFactory.SpecialType(SpecialType.System_Boolean);
            _payloadType = ArrayTypeSymbol.CreateCSharpArray(methodBodyFactory.Compilation.Assembly, TypeWithAnnotations.Create(payloadElementType));
            _diagnostics = diagnostics;
            _debugDocumentProvider = debugDocumentProvider;
            _methodBodyFactory = methodBodyFactory;

            // Set the factory context to generate nodes for the current method
            var oldMethod = methodBodyFactory.CurrentFunction;
            methodBodyFactory.CurrentFunction = method;

            _methodPayload = methodBodyFactory.SynthesizedLocal(_payloadType, kind: SynthesizedLocalKind.InstrumentationPayload, syntax: methodBody.Syntax);
            // The first point indicates entry into the method and has the span of the method definition.
            SyntaxNode syntax = MethodDeclarationIfAvailable(methodBody.Syntax);
            if (!method.IsImplicitlyDeclared && method is not SynthesizedSimpleProgramEntryPointSymbol)
            {
                _methodEntryInstrumentation = AddAnalysisPoint(syntax, SkipAttributes(syntax), methodBodyFactory);
            }

            // Restore context
            methodBodyFactory.CurrentFunction = oldMethod;
        }

        protected override CompoundInstrumenter WithPreviousImpl(Instrumenter previous)
            => throw ExceptionUtilities.Unreachable(); // we don't currently need this

        private static bool IsExcludedFromCodeCoverage(MethodSymbol method)
        {
            Debug.Assert(method.MethodKind != MethodKind.LocalFunction && method.MethodKind != MethodKind.AnonymousFunction);

            var containingType = method.ContainingType;
            while (containingType is not null)
            {
                if (containingType.IsDirectlyExcludedFromCodeCoverage)
                {
                    return true;
                }

                containingType = containingType.ContainingType;
            }

            return method switch
            {
                { IsDirectlyExcludedFromCodeCoverage: true } => true,
                { AssociatedSymbol: PropertySymbol { IsDirectlyExcludedFromCodeCoverage: true } } => true,
                { AssociatedSymbol: EventSymbol { IsDirectlyExcludedFromCodeCoverage: true } } => true,
                _ => false
            };
        }

        private static BoundExpressionStatement GetCreatePayloadStatement(
            ImmutableArray<SourceSpan> dynamicAnalysisSpans,
            SyntaxNode methodBodySyntax,
            LocalSymbol methodPayload,
            MethodSymbol createPayloadForMethodsSpanningSingleFile,
            MethodSymbol createPayloadForMethodsSpanningMultipleFiles,
            BoundExpression mvid,
            BoundExpression methodToken,
            BoundExpression payloadSlot,
            SyntheticBoundNodeFactory methodBodyFactory,
            DebugDocumentProvider debugDocumentProvider)
        {
            MethodSymbol createPayloadOverload;
            BoundExpression fileIndexOrIndicesArgument;

            if (dynamicAnalysisSpans.IsEmpty)
            {
                createPayloadOverload = createPayloadForMethodsSpanningSingleFile;

                // For a compiler generated method that has no 'real' spans, we emit the index for
                // the document corresponding to the syntax node that is associated with its bound node.
                var document = GetSourceDocument(debugDocumentProvider, methodBodySyntax);
                fileIndexOrIndicesArgument = methodBodyFactory.SourceDocumentIndex(document);
            }
            else
            {
                var documents = PooledHashSet<DebugSourceDocument>.GetInstance();
                var fileIndices = ArrayBuilder<BoundExpression>.GetInstance();

                foreach (var span in dynamicAnalysisSpans)
                {
                    var document = span.Document;
                    if (documents.Add(document))
                    {
                        fileIndices.Add(methodBodyFactory.SourceDocumentIndex(document));
                    }
                }

                documents.Free();

                // At this point, we should have at least one document since we have already
                // handled the case where method has no 'real' spans (and therefore no documents) above.
                if (fileIndices.Count == 1)
                {
                    createPayloadOverload = createPayloadForMethodsSpanningSingleFile;
                    fileIndexOrIndicesArgument = fileIndices.Single();
                }
                else
                {
                    createPayloadOverload = createPayloadForMethodsSpanningMultipleFiles;

                    // Order of elements in fileIndices should be deterministic because these
                    // elements were added based on order of spans in dynamicAnalysisSpans above.
                    fileIndexOrIndicesArgument = methodBodyFactory.Array(
                        methodBodyFactory.SpecialType(SpecialType.System_Int32), fileIndices.ToImmutable());
                }

                fileIndices.Free();
            }

            return methodBodyFactory.Assignment(
                methodBodyFactory.Local(methodPayload),
                methodBodyFactory.Call(
                    null,
                    createPayloadOverload,
                    mvid,
                    methodToken,
                    fileIndexOrIndicesArgument,
                    payloadSlot,
                    methodBodyFactory.Literal(dynamicAnalysisSpans.Length)));
        }

        public override void InstrumentBlock(BoundBlock original, LocalRewriter rewriter, ref TemporaryArray<LocalSymbol> additionalLocals, out BoundStatement? prologue, out BoundStatement? epilogue, out BoundBlockInstrumentation? instrumentation)
        {
            base.InstrumentBlock(original, rewriter, ref additionalLocals, out var previousPrologue, out epilogue, out instrumentation);

            // only instrument method body block:
            if (original != rewriter.CurrentMethodBody)
            {
                prologue = previousPrologue;
                return;
            }

            _dynamicAnalysisSpans = _spansBuilder.ToImmutableAndFree();
            // In the future there will be multiple analysis kinds.
            const int analysisKind = 0;

            ArrayTypeSymbol modulePayloadType =
                ArrayTypeSymbol.CreateCSharpArray(_methodBodyFactory.Compilation.Assembly, TypeWithAnnotations.Create(_payloadType));

            // Synthesize the initialization of the instrumentation payload array, using concurrency-safe code:
            //
            // var payload = PID.PayloadRootField[methodIndex];
            // if (payload == null)
            //     payload = Instrumentation.CreatePayload(mvid, methodIndex, fileIndexOrIndices, ref PID.PayloadRootField[methodIndex], payloadLength);

            BoundStatement payloadInitialization =
                _methodBodyFactory.Assignment(
                    _methodBodyFactory.Local(_methodPayload),
                    _methodBodyFactory.ArrayAccess(
                        _methodBodyFactory.InstrumentationPayloadRoot(analysisKind, modulePayloadType),
                        ImmutableArray.Create(_methodBodyFactory.MethodDefIndex(_method))));

            BoundExpression mvid = _methodBodyFactory.ModuleVersionId();
            BoundExpression methodToken = _methodBodyFactory.MethodDefIndex(_method);

            BoundExpression payloadSlot =
                _methodBodyFactory.ArrayAccess(
                    _methodBodyFactory.InstrumentationPayloadRoot(analysisKind, modulePayloadType),
                    ImmutableArray.Create(_methodBodyFactory.MethodDefIndex(_method)));

            BoundStatement createPayloadCall =
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
                    _debugDocumentProvider);

            BoundExpression payloadNullTest =
                _methodBodyFactory.Binary(
                    BinaryOperatorKind.ObjectEqual,
                    _methodBodyFactory.SpecialType(SpecialType.System_Boolean),
                    _methodBodyFactory.Local(_methodPayload),
                    _methodBodyFactory.Null(_payloadType));

            BoundStatement payloadIf = _methodBodyFactory.If(payloadNullTest, createPayloadCall);

            additionalLocals.Add(_methodPayload);

            var prologueStatements = ArrayBuilder<BoundStatement>.GetInstance(2 + (_methodEntryInstrumentation != null ? 1 : 0) + (previousPrologue != null ? 1 : 0));

            prologueStatements.Add(payloadInitialization);
            prologueStatements.Add(payloadIf);
            if (_methodEntryInstrumentation != null)
            {
                prologueStatements.Add(_methodEntryInstrumentation);
            }

            if (previousPrologue != null)
            {
                prologueStatements.Add(previousPrologue);
            }

            prologue = _methodBodyFactory.StatementList(prologueStatements.ToImmutableAndFree());
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
            return AddDynamicAnalysis(original, base.InstrumentExpressionStatement(original, rewritten));
        }

        public override BoundStatement InstrumentFieldOrPropertyInitializer(BoundStatement original, BoundStatement rewritten)
        {
            return AddDynamicAnalysis(original, base.InstrumentFieldOrPropertyInitializer(original, rewritten));
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

        public override BoundStatement InstrumentForEachStatementDeconstructionVariablesDeclaration(BoundForEachStatement original, BoundStatement iterationVarDecl)
        {
            return AddDynamicAnalysis(original, base.InstrumentForEachStatementDeconstructionVariablesDeclaration(original, iterationVarDecl));
        }

        public override BoundStatement InstrumentIfStatementConditionalGoto(BoundIfStatement original, BoundStatement rewritten)
        {
            return AddDynamicAnalysis(original, base.InstrumentIfStatementConditionalGoto(original, rewritten));
        }

        public override BoundStatement InstrumentWhileStatementConditionalGotoStartOrBreak(BoundWhileStatement original, BoundStatement ifConditionGotoStart)
        {
            return AddDynamicAnalysis(original, base.InstrumentWhileStatementConditionalGotoStartOrBreak(original, ifConditionGotoStart));
        }

        public override BoundStatement InstrumentUserDefinedLocalInitialization(BoundLocalDeclaration original, BoundStatement rewritten)
        {
            return AddDynamicAnalysis(original, base.InstrumentUserDefinedLocalInitialization(original, rewritten));
        }

        public override BoundStatement InstrumentLockTargetCapture(BoundLockStatement original, BoundStatement lockTargetCapture)
        {
            return AddDynamicAnalysis(original, base.InstrumentLockTargetCapture(original, lockTargetCapture));
        }

        public override BoundStatement InstrumentReturnStatement(BoundReturnStatement original, BoundStatement rewritten)
        {
            rewritten = base.InstrumentReturnStatement(original, rewritten);

            // A synthesized return statement that does not return a value never requires instrumentation.
            // A property set defined without a block has such a synthesized return statement.
            // A synthesized return statement that does return a value does require instrumentation.
            // A method, property get, or lambda defined without a block has such a synthesized return statement.
            if (ReturnsValueWithinExpressionBodiedConstruct(original))
            {
                // The return statement for value-returning methods defined without a block is compiler generated, but requires instrumentation.
                return CollectDynamicAnalysis(original, rewritten);
            }

            return AddDynamicAnalysis(original, rewritten);
        }

        private static bool ReturnsValueWithinExpressionBodiedConstruct(BoundReturnStatement returnStatement)
        {
            if (returnStatement.WasCompilerGenerated &&
                returnStatement.ExpressionOpt != null &&
                returnStatement.ExpressionOpt.Syntax != null)
            {
                Debug.Assert(returnStatement.ExpressionOpt.Syntax.Parent != null);

                SyntaxKind parentKind = returnStatement.ExpressionOpt.Syntax.Parent.Kind();
                switch (parentKind)
                {
                    case SyntaxKind.ParenthesizedLambdaExpression:
                    case SyntaxKind.SimpleLambdaExpression:
                    case SyntaxKind.ArrowExpressionClause:
                        return true;
                }
            }

            return false;
        }

        public override BoundStatement InstrumentSwitchStatement(BoundSwitchStatement original, BoundStatement rewritten)
        {
            return AddDynamicAnalysis(original, base.InstrumentSwitchStatement(original, rewritten));
        }

        public override BoundStatement InstrumentSwitchWhenClauseConditionalGotoBody(BoundExpression original, BoundStatement ifConditionGotoBody)
        {
            ifConditionGotoBody = base.InstrumentSwitchWhenClauseConditionalGotoBody(original, ifConditionGotoBody);
            var whenClause = original.Syntax.FirstAncestorOrSelf<WhenClauseSyntax>();
            Debug.Assert(whenClause != null);

            // Instrument the statement using a factory with the same syntax as the clause, so that the instrumentation appears to be part of the clause.
            SyntheticBoundNodeFactory statementFactory = new SyntheticBoundNodeFactory(_method, whenClause, _methodBodyFactory.CompilationState, _diagnostics);

            // Instrument using the span of the expression
            return statementFactory.StatementList(AddAnalysisPoint(whenClause, statementFactory), ifConditionGotoBody);
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

        private static Cci.DebugSourceDocument GetSourceDocument(DebugDocumentProvider debugDocumentProvider, SyntaxNode syntax)
        {
            return GetSourceDocument(debugDocumentProvider, syntax, syntax.GetLocation().GetMappedLineSpan());
        }

        private static Cci.DebugSourceDocument GetSourceDocument(DebugDocumentProvider debugDocumentProvider, SyntaxNode syntax, FileLinePositionSpan span)
        {
            string path = span.Path;
            // If the path for the syntax node is empty, try the path for the entire syntax tree.
            if (path.Length == 0)
            {
                path = syntax.SyntaxTree.FilePath;
            }

            return debugDocumentProvider.Invoke(path, basePath: "");
        }

        private BoundStatement AddAnalysisPoint(SyntaxNode syntaxForSpan, Text.TextSpan alternateSpan, SyntheticBoundNodeFactory statementFactory)
        {
            return AddAnalysisPoint(syntaxForSpan, syntaxForSpan.SyntaxTree.GetMappedLineSpan(alternateSpan), statementFactory);
        }

        private BoundStatement AddAnalysisPoint(SyntaxNode syntaxForSpan, SyntheticBoundNodeFactory statementFactory)
        {
            return AddAnalysisPoint(syntaxForSpan, syntaxForSpan.GetLocation().GetMappedLineSpan(), statementFactory);
        }

        private BoundStatement AddAnalysisPoint(SyntaxNode syntaxForSpan, FileLinePositionSpan span, SyntheticBoundNodeFactory statementFactory)
        {
            // Add an entry in the spans array.
            int spansIndex = _spansBuilder.Count;
            _spansBuilder.Add(new SourceSpan(
                GetSourceDocument(_debugDocumentProvider, syntaxForSpan, span),
                span.StartLinePosition.Line,
                span.StartLinePosition.Character,
                span.EndLinePosition.Line,
                span.EndLinePosition.Character));

            // Generate "_payload[pointIndex] = true".
            BoundArrayAccess payloadCell =
                statementFactory.ArrayAccess(
                    statementFactory.Local(_methodPayload),
                    statementFactory.Literal(spansIndex));

            return statementFactory.Assignment(payloadCell, statementFactory.Literal(true));
        }

        private static SyntaxNode SyntaxForSpan(BoundStatement statement)
        {
            SyntaxNode syntaxForSpan;

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
                        syntaxForSpan = ((BoundNode?)usingStatement.ExpressionOpt ?? usingStatement.DeclarationsOpt)!.Syntax;
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

        private static MethodSymbol GetCreatePayloadOverload(CSharpCompilation compilation, WellKnownMember overload, SyntaxNode syntax, BindingDiagnosticBag diagnostics)
        {
            return (MethodSymbol)Binder.GetWellKnownTypeMember(compilation, overload, diagnostics, syntax: syntax);
        }

        private static SyntaxNode MethodDeclarationIfAvailable(SyntaxNode body)
        {
            SyntaxNode? parent = body.Parent;

            if (parent != null)
            {
                switch (parent.Kind())
                {
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.PropertyDeclaration:
                    case SyntaxKind.GetAccessorDeclaration:
                    case SyntaxKind.SetAccessorDeclaration:
                    case SyntaxKind.InitAccessorDeclaration:
                    case SyntaxKind.ConstructorDeclaration:
                    case SyntaxKind.OperatorDeclaration:

                        return parent;
                }
            }

            return body;
        }

        // If the method, property, etc. has attributes, the attributes are excluded from the span of the method definition.
        private static TextSpan SkipAttributes(SyntaxNode syntax)
        {
            switch (syntax.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                    MethodDeclarationSyntax methodSyntax = (MethodDeclarationSyntax)syntax;
                    return SkipAttributes(syntax, methodSyntax.AttributeLists, methodSyntax.Modifiers, keyword: default, methodSyntax.ReturnType);

                case SyntaxKind.PropertyDeclaration:
                    PropertyDeclarationSyntax propertySyntax = (PropertyDeclarationSyntax)syntax;
                    return SkipAttributes(syntax, propertySyntax.AttributeLists, propertySyntax.Modifiers, keyword: default, propertySyntax.Type);

                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.InitAccessorDeclaration:
                    AccessorDeclarationSyntax accessorSyntax = (AccessorDeclarationSyntax)syntax;
                    return SkipAttributes(syntax, accessorSyntax.AttributeLists, accessorSyntax.Modifiers, accessorSyntax.Keyword, type: null);

                case SyntaxKind.ConstructorDeclaration:
                    ConstructorDeclarationSyntax constructorSyntax = (ConstructorDeclarationSyntax)syntax;
                    return SkipAttributes(syntax, constructorSyntax.AttributeLists, constructorSyntax.Modifiers, constructorSyntax.Identifier, type: null);

                case SyntaxKind.OperatorDeclaration:
                    OperatorDeclarationSyntax operatorSyntax = (OperatorDeclarationSyntax)syntax;
                    return SkipAttributes(syntax, operatorSyntax.AttributeLists, operatorSyntax.Modifiers, operatorSyntax.OperatorKeyword, type: null);
            }

            return syntax.Span;
        }

        private static TextSpan SkipAttributes(SyntaxNode syntax, SyntaxList<AttributeListSyntax> attributes, SyntaxTokenList modifiers, SyntaxToken keyword, TypeSyntax? type)
        {
            Debug.Assert(keyword.Node != null || type != null);

            var originalSpan = syntax.Span;
            if (attributes.Count > 0)
            {
                var startSpan = modifiers.Node != null ? modifiers.Span : (keyword.Node != null ? keyword.Span : type!.Span);
                return new TextSpan(startSpan.Start, originalSpan.Length - (startSpan.Start - originalSpan.Start));
            }

            return originalSpan;
        }
    }
}
