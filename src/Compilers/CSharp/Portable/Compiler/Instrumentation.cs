// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Provides means for instrumenting compiled methods for dynamic analysis.
    /// </summary>
    internal static class Instrumentation
    {
        internal static BoundBlock InjectInstrumentation(MethodSymbol method, BoundBlock methodBody, TypeCompilationState compilationState, DiagnosticBag diagnostics, DebugDocumentProvider debugDocumentProvider, out ImmutableArray<SourceSpan> dynamicAnalysisSpans)
        {
            if (methodBody != null)
            {
                CSharpCompilation compilation = compilationState.Compilation;

                MethodSymbol createPayload = GetCreatePayload(compilation, methodBody.Syntax, diagnostics);
                MethodSymbol flushPayload = GetFlushPayload(compilation, methodBody.Syntax, diagnostics);

                // Do not instrument the instrumentation helpers if they are part of the current compilation (which occurs only during testing). GetCreatePayload will fail with an infinite recursion if it is instrumented.
                // PROTOTYPE (https://github.com/dotnet/roslyn/issues/10266): It is not correct to always skip implict methods, because that will miss field initializers.
                if ((object)createPayload != null && (object)flushPayload != null && !method.IsImplicitlyDeclared && !method.Equals(createPayload) && !method.Equals(flushPayload))
                {
                    // Create the symbol for the instrumentation payload.
                    SyntheticBoundNodeFactory factory = new SyntheticBoundNodeFactory(method, methodBody.Syntax, compilationState, diagnostics);
                    TypeSymbol boolType = factory.SpecialType(SpecialType.System_Boolean);
                    TypeSymbol payloadElementType = boolType;
                    ArrayTypeSymbol payloadType = ArrayTypeSymbol.CreateCSharpArray(compilation.Assembly, payloadElementType);
                    bool methodHasExplicitBlock = MethodHasExplicitBlock(method);
                    LocalSymbol methodPayload = factory.SynthesizedLocal(payloadType);
                    ArrayTypeSymbol modulePayloadType = ArrayTypeSymbol.CreateCSharpArray(compilation.Assembly, payloadType);
                    // PROTOTYPE (https://github.com/dotnet/roslyn/issues/10411): In the future there will be multiple analysis kinds.
                    int analysisKind = 0;
                    // Synthesize the instrumentation and collect the spans of interest.

                    // PROTOTYPE (https://github.com/dotnet/roslyn/issues/9819): Try to integrate instrumentation with lowering, to avoid an extra pass over the bound tree.
                    BoundBlock newMethodBody = InstrumentationInjectionRewriter.InstrumentMethod(method, methodBody, methodHasExplicitBlock, methodPayload, compilationState, diagnostics, debugDocumentProvider, out dynamicAnalysisSpans);

                    // Synthesize the initialization of the instrumentation payload array, using concurrency-safe code:
                    //
                    // var payload = PID.PayloadField[methodIndex];
                    // if (payload == null)
                    //     payload = Instrumentation.CreatePayload(mvid, methodIndex, ref PID.PayloadField[methodIndex], payloadLength);

                    var payloadInitialization = factory.Assignment(factory.Local(methodPayload), factory.ArrayAccess(factory.InstrumentationPayload(analysisKind, modulePayloadType), ImmutableArray.Create(factory.MethodDefinitionToken(method))));
                    BoundExpression mvid = factory.ModuleVersionId();
                    BoundExpression methodToken = factory.MethodDefinitionToken(method);
                    BoundExpression payloadSlot = factory.ArrayAccess(factory.InstrumentationPayload(analysisKind, modulePayloadType), ImmutableArray.Create(factory.MethodDefinitionToken(method)));
                    BoundStatement createPayloadCall = factory.Assignment(factory.Local(methodPayload), factory.Call(null, createPayload, mvid, methodToken, payloadSlot, factory.Literal(dynamicAnalysisSpans.Length)));

                    BoundExpression payloadNullTest = factory.Binary(BinaryOperatorKind.ObjectEqual, boolType, factory.Local(methodPayload), factory.Null(payloadType));
                    BoundStatement payloadIf = factory.If(payloadNullTest, createPayloadCall);

                    // Methods defined without block syntax won't naturally get a sequence point for the entry of the method.
                    // It is a requirement that there be a sequence point before any executable code. A sequence point
                    // won't be generated automatically for the synthesized if statement because the if statement is compiler generated, so
                    // force the generation of a sequence point for the if statement.
                    if (!methodHasExplicitBlock)
                    {
                        payloadIf = factory.SequencePoint(payloadIf.Syntax, payloadIf);
                    }

                    ImmutableArray<BoundStatement> newStatements = newMethodBody.Statements.Insert(0, payloadIf);
                    newMethodBody = newMethodBody.Update(newMethodBody.Locals.Add(methodPayload), newMethodBody.LocalFunctions, newStatements);

                    if (IsTestMethod(method))
                    {
                        // If the method is a test method, wrap the body in:
                        //
                        // try
                        // {
                        //     ... body ...
                        // }
                        // finally
                        // {
                        //     Instrumentation.FlushPayload();
                        // }

                        BoundStatement flush = factory.ExpressionStatement(factory.Call(null, flushPayload));
                        BoundStatement tryFinally = factory.Try(newMethodBody, ImmutableArray<BoundCatchBlock>.Empty, factory.Block(ImmutableArray.Create(flush)));
                        newMethodBody = factory.Block(ImmutableArray.Create(tryFinally));
                    }

                    return newMethodBody;
                }
            }

            dynamicAnalysisSpans = ImmutableArray<SourceSpan>.Empty;
            return methodBody;
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

        private static MethodSymbol GetFlushPayload(CSharpCompilation compilation, CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
        {
            return (MethodSymbol)Binder.GetWellKnownTypeMember(compilation, WellKnownMember.Microsoft_CodeAnalysis_Runtime_Instrumentation__FlushPayload, diagnostics, syntax: syntax);
        }

        private static bool IsTestMethod(MethodSymbol method)
        {
            // PROTOTYPE (https://github.com/dotnet/roslyn/issues/9811): Make this real. 
            var attributes = method.GetAttributes();
            foreach (var attribute in attributes)
            {
                if (attribute.IsTargetAttribute("Xunit", "FactAttribute"))
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal sealed class InstrumentationInjectionRewriter : BoundTreeRewriterWithStackGuard
    {
        private readonly MethodSymbol _method;
        private readonly ArrayBuilder<SourceSpan> _spansBuilder;
        private readonly LocalSymbol _payload;
        private readonly TypeCompilationState _compilationState;
        private readonly DiagnosticBag _diagnostics;
        private readonly DebugDocumentProvider _debugDocumentProvider;
        private readonly bool _methodHasExplicitBlock;

        public static BoundBlock InstrumentMethod(MethodSymbol method, BoundBlock methodBody, bool methodHasExplicitBlock, LocalSymbol payload, TypeCompilationState compilationState, DiagnosticBag diagnostics, DebugDocumentProvider debugDocumentProvider, out ImmutableArray<SourceSpan> dynamicAnalysisSpans)
        {
            ArrayBuilder<SourceSpan> spansBuilder = ArrayBuilder<SourceSpan>.GetInstance();
            BoundTreeRewriter collector = new InstrumentationInjectionRewriter(method, methodHasExplicitBlock, spansBuilder, payload, compilationState, diagnostics, debugDocumentProvider);
            BoundBlock newMethodBody = (BoundBlock)collector.Visit(methodBody);
            dynamicAnalysisSpans = spansBuilder.ToImmutableAndFree();
            return newMethodBody;
        }

        private InstrumentationInjectionRewriter(MethodSymbol method, bool methodHasExplicitBlock, ArrayBuilder<SourceSpan> spansBuilder, LocalSymbol payload, TypeCompilationState compilationState, DiagnosticBag diagnostics, DebugDocumentProvider debugDocumentProvider)
        {
            _method = method;
            _spansBuilder = spansBuilder;
            _payload = payload;
            _compilationState = compilationState;
            _diagnostics = diagnostics;
            _debugDocumentProvider = debugDocumentProvider;
            _methodHasExplicitBlock = methodHasExplicitBlock;
        }

        public override BoundNode Visit(BoundNode node)
        {
            BoundNode visited = base.Visit(node);
            BoundStatement statement = node as BoundStatement;
            if (statement != null)
            {
                // The default behavior is to instrument a statement unless it is compiler generated.
                // Filter out statements that are not to be instrumented, and force instrumentation of some compiler-generated statements.
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
                    case BoundKind.Block:
                    case BoundKind.TryStatement:
                    // A for statement implicitly gets coverage for the initial statements and the increment statement.
                    case BoundKind.ForStatement:
                        return visited;
                    case BoundKind.ReturnStatement:
                        // A synthesized return statement that does not return a value never requires instrumentation.
                        // A property set method defined without a block has such a synthesized return statement.
                        if (!_methodHasExplicitBlock && ((BoundReturnStatement)statement).ExpressionOpt != null)
                        {
                            // The return statement for value-returning methods defined without a block is compiler generated, but requires instrumentation.
                            return CollectDynamicAnalysis(visited);
                        }
                        break;
                    case BoundKind.ExpressionStatement:
                        if (!_methodHasExplicitBlock)
                        {
                            // The assignment statement for a property set method defined without a block is compiler generated, but requires instrumentation.
                            return CollectDynamicAnalysis(visited);
                        }
                        break;
                    case BoundKind.LocalDeclaration:
                        if (statement.Syntax.Parent.Kind() == SyntaxKind.VariableDeclaration)
                        {
                            VariableDeclarationSyntax declarationSyntax = (VariableDeclarationSyntax)statement.Syntax.Parent;
                            if (declarationSyntax.Variables.Count > 1)
                            {
                                // This declaration is part of a MultipleLocalDeclarations statement,
                                // and so should not be treated as a separate statement.
                                return visited;
                            }

                            // A statement that represents the declarations in a using or fixed statement should not be treated as a separate statement.
                            switch (declarationSyntax.Parent.Kind())
                            {
                                case SyntaxKind.UsingStatement:
                                    if (declarationSyntax == ((UsingStatementSyntax)declarationSyntax.Parent).Declaration)
                                    {
                                        return visited;
                                    }
                                    break;
                                case SyntaxKind.FixedStatement:
                                    if (declarationSyntax == ((FixedStatementSyntax)declarationSyntax.Parent).Declaration)
                                    {
                                        return visited;
                                    }
                                    break;
                            }
                        }

                        // Declarations without initializers are not instrumented.
                        if (!HasInitializer((BoundLocalDeclaration)statement))
                        {
                            return visited;
                        }

                        break;
                    case BoundKind.MultipleLocalDeclarations:
                        // Using and fixed statements have a multiple local declarations node even if they contain only one declaration.
                        switch (statement.Syntax.Parent.Kind())
                        {
                            case SyntaxKind.UsingStatement:
                                if (statement.Syntax == ((UsingStatementSyntax)statement.Syntax.Parent).Declaration)
                                {
                                    // This statement represents the declarations in a Using statement, and should not be treated as a separate statement.
                                    return visited;
                                }
                                break;
                            case SyntaxKind.FixedStatement:
                                if (statement.Syntax == ((FixedStatementSyntax)statement.Syntax.Parent).Declaration)
                                {
                                    // This statement represents the declarations in a Fixed statement, and should not be treated as a separate statement.
                                    return visited;
                                }
                                break;
                        }

                        // Declarations without initializers are not instrumented.
                        // Ultimately the individual initializers will be implemented, but for now instrument the statement if it has any initializers.
                        if (!HasInitializer((BoundMultipleLocalDeclarations)statement))
                        {
                            return visited;
                        }

                        break;
                    default:
                        break;
                }

                if (!statement.WasCompilerGenerated)
                {
                    return CollectDynamicAnalysis(visited);
                }
            }

            return visited;
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
            // Add an entry in the spans array.

            CSharpSyntaxNode syntaxForSpan = SyntaxForSpan(statement);
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

            SyntheticBoundNodeFactory statementFactory = new SyntheticBoundNodeFactory(_method, statement.Syntax, _compilationState, _diagnostics);
            BoundArrayAccess payloadCell = statementFactory.ArrayAccess(statementFactory.Local(_payload), statementFactory.Literal(spansIndex));
            BoundExpressionStatement cellAssignment = statementFactory.Assignment(payloadCell, statementFactory.Literal(true));
            
            return statementFactory.Block(ImmutableArray.Create(cellAssignment, statement));
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
    }
}
