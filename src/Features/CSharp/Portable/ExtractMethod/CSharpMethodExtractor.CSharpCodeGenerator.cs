// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal sealed partial class CSharpExtractMethodService
{
    internal sealed partial class CSharpMethodExtractor
    {
        private abstract partial class CSharpCodeGenerator : CodeGenerator<SyntaxNode, CSharpCodeGenerationOptions>
        {
            private readonly SyntaxToken _methodName;

            private const string NewMethodPascalCaseStr = "NewMethod";
            private const string NewMethodCamelCaseStr = "newMethod";

            public static CSharpCodeGenerator Create(
                SelectionResult selectionResult,
                AnalyzerResult analyzerResult,
                ExtractMethodGenerationOptions options,
                bool localFunction)
            {
                return selectionResult.SelectionType switch
                {
                    SelectionType.Expression => new ExpressionCodeGenerator(selectionResult, analyzerResult, options, localFunction),
                    SelectionType.SingleStatement => new SingleStatementCodeGenerator(selectionResult, analyzerResult, options, localFunction),
                    SelectionType.MultipleStatements => new MultipleStatementsCodeGenerator(selectionResult, analyzerResult, options, localFunction),
                    var v => throw ExceptionUtilities.UnexpectedValue(v),
                };
            }

            protected CSharpCodeGenerator(
                SelectionResult selectionResult,
                AnalyzerResult analyzerResult,
                ExtractMethodGenerationOptions options,
                bool localFunction)
                : base(selectionResult, analyzerResult, options, localFunction)
            {
                Contract.ThrowIfFalse(SemanticDocument == selectionResult.SemanticDocument);

                var nameToken = CreateMethodName();
                _methodName = nameToken.WithAdditionalAnnotations(MethodNameAnnotation);
            }

            protected override StatementSyntax CreateBreakStatement()
                // Being explicit about trivia ensures the formatter doesn't insert newlines in undesirable places.
                => BreakStatement(BreakKeyword.WithoutTrailingTrivia(), SemicolonToken.WithoutLeadingTrivia());

            protected override StatementSyntax CreateContinueStatement()
                // Being explicit about trivia ensures the formatter doesn't insert newlines in undesirable places.
                => ContinueStatement(ContinueKeyword.WithoutTrailingTrivia(), SemicolonToken.WithoutLeadingTrivia());

            public override OperationStatus<ImmutableArray<SyntaxNode>> GetNewMethodStatements(SyntaxNode insertionPointNode, CancellationToken cancellationToken)
            {
                var statements = CreateMethodBody(insertionPointNode, cancellationToken);
                var status = CheckActiveStatements(statements);
                return status.With(statements.CastArray<SyntaxNode>());
            }

            protected override IMethodSymbol GenerateMethodDefinition(
                SyntaxNode insertionPointNode, CancellationToken cancellationToken)
            {
                var statements = CreateMethodBody(insertionPointNode, cancellationToken);
                statements = WrapInCheckStatementIfNeeded(statements);

                var methodSymbol = CodeGenerationSymbolFactory.CreateMethodSymbol(
                    attributes: [],
                    accessibility: Accessibility.Private,
                    modifiers: CreateMethodModifiers(),
                    returnType: this.GetFinalReturnType(),
                    refKind: AnalyzerResult.ReturnsByRef ? RefKind.Ref : RefKind.None,
                    explicitInterfaceImplementations: default,
                    name: _methodName.ToString(),
                    typeParameters: CreateMethodTypeParameters(),
                    parameters: CreateMethodParameters(),
                    statements: statements.CastArray<SyntaxNode>(),
                    methodKind: this.LocalFunction ? MethodKind.LocalFunction : MethodKind.Ordinary);

                return MethodDefinitionAnnotation.AddAnnotationToSymbol(
                    Formatter.Annotation.AddAnnotationToSymbol(methodSymbol));
            }

            protected override async Task<SyntaxNode> GenerateBodyForCallSiteContainerAsync(
                SyntaxNode insertionPointNode,
                SyntaxNode container,
                CancellationToken cancellationToken)
            {
                var variableMapToRemove = CreateVariableDeclarationToRemoveMap(
                    AnalyzerResult.GetVariablesToMoveIntoMethodDefinition(), cancellationToken);
                var firstStatementToRemove = GetFirstStatementOrInitializerSelectedAtCallSite();
                var lastStatementToRemove = GetLastStatementOrInitializerSelectedAtCallSite();

                var statementsToInsert = await CreateStatementsOrInitializerToInsertAtCallSiteAsync(
                    insertionPointNode, cancellationToken).ConfigureAwait(false);

                var callSiteGenerator = new CallSiteContainerRewriter(
                    container,
                    variableMapToRemove,
                    firstStatementToRemove,
                    lastStatementToRemove,
                    statementsToInsert);

                return container.CopyAnnotationsTo(callSiteGenerator.Generate()).WithAdditionalAnnotations(Formatter.Annotation);
            }

            private async Task<ImmutableArray<SyntaxNode>> CreateStatementsOrInitializerToInsertAtCallSiteAsync(
                SyntaxNode insertionPointNode, CancellationToken cancellationToken)
            {
                var selectedNode = GetFirstStatementOrInitializerSelectedAtCallSite();

                // field initializer, constructor initializer, expression bodied member case
                if (selectedNode is ConstructorInitializerSyntax or FieldDeclarationSyntax or PrimaryConstructorBaseTypeSyntax ||
                    IsExpressionBodiedMember(selectedNode) ||
                    IsExpressionBodiedAccessor(selectedNode))
                {
                    var statement = await GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(cancellationToken).ConfigureAwait(false);
                    return [statement];
                }

                // regular case
                var semanticModel = SemanticDocument.SemanticModel;
                var postProcessor = new PostProcessor(semanticModel, insertionPointNode.SpanStart);

                var statements = AddSplitOrMoveDeclarationOutStatementsToCallSite(cancellationToken);
                statements = postProcessor.MergeDeclarationStatements(statements);
                statements = AddAssignmentStatementToCallSite(statements, cancellationToken);
                statements = AddComplexFlowControlProcessingStatements(statements);
                statements = await AddInvocationAtCallSiteAsync(statements, cancellationToken).ConfigureAwait(false);
                statements = AddReturnIfUnreachable(statements, cancellationToken);

                return statements.CastArray<SyntaxNode>();
            }

            /// <summary>
            /// Adds the statements after the call to the newly extracted method to handle processing of the control
            /// flow return value, and optionally the normal return value of the method.
            /// </summary>
            private ImmutableArray<StatementSyntax> AddComplexFlowControlProcessingStatements(ImmutableArray<StatementSyntax> statements)
            {
                var flowControlInformation = this.AnalyzerResult.FlowControlInformation;
                if (!flowControlInformation.NeedsControlFlowValue())
                    return statements;

                var useBlock = ((CSharpSimplifierOptions)this.ExtractMethodGenerationOptions.SimplifierOptions).PreferBraces.Value == CodeAnalysis.CodeStyle.PreferBracesPreference.Always;
                return statements.Add(GetFlowControlStatement());

                StatementSyntax GetFlowControlStatement()
                {
                    var controlFlowValueType = flowControlInformation.ControlFlowValueType;
                    if (controlFlowValueType.SpecialType == SpecialType.System_Boolean)
                    {
                        if (flowControlInformation.TryGetFallThroughFlowValue(out _))
                        {
                            // If we have 'fallthrough' as as the final control flow value, then we'll just emit:
                            //
                            //      if (!flowControl) FlowControlConstruct; // allowing fallthrough to happen automatically.
                            return IfStatement(
                                PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, IdentifierName(FlowControlName)),
                                Block(GetFlowStatement(false)));
                        }
                        else if (flowControlInformation.BreakStatementCount == 0)
                        {
                            // Otherwise, if we have no break statements we'll emit the following as its shorter:
                            //
                            //      switch (flowControl)
                            //      {
                            //          case false: FlowControlConstruct1;
                            //          case true: FlowControlConstruct2;
                            //      }
                            return NoBreakSwitchStatement();
                        }
                        else
                        {
                            // Otherwise, we'll emit:
                            //
                            //      if (flowControl)
                            //          FlowControlConstruct1;
                            //      else
                            //          FlowControlConstruct2;
                            return IfStatement(
                                IdentifierName(FlowControlName),
                                Block(GetFlowStatement(true)),
                                ElseClause(Block(GetFlowStatement(false))));
                        }
                    }
                    else if (controlFlowValueType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                    {
                        if (flowControlInformation.TryGetFallThroughFlowValue(out _))
                        {
                            if (flowControlInformation.BreakStatementCount == 0)
                            {
                                // Otherwise, if we have no break statements we'll emit the following as its shorter:
                                //
                                //      switch (flowControl)
                                //      {
                                //          case false: FlowControlConstruct1;
                                //          case true: FlowControlConstruct2;
                                //      }
                                return NoBreakSwitchStatement();
                            }
                            else
                            {
                                // If we have 'fallthrough' as as the final control flow value, then we'll just emit:
                                //
                                //      if (flowControl == false)
                                //          FlowControlConstruct1;
                                //      else if (flowControl == true)
                                //          FlowControlConstruct2; // allowing fallthrough to happen automatically.
                                return ControlFlowIfStatement(false, ControlFlowIfStatement(true));
                            }
                        }
                        else
                        {
                            // Otherwise, we'll emit:
                            //      if (flowControl == false)
                            //          FlowControlConstruct1;
                            //      else if (flowControl == true)
                            //          FlowControlConstruct2;
                            //      else
                            //          FlowControlConstruct3;
                            return ControlFlowIfStatement(false, ControlFlowIfStatement(true, Block(GetFlowStatement(null))));
                        }
                    }
                    else
                    {
                        Contract.ThrowIfFalse(controlFlowValueType.SpecialType == SpecialType.System_Int32);
                        // We use 'int' when we have all 4 flow control cases (break, continue, return, fallthrough).
                        // fallthrough is always the last one so we only have to test the first 3.
                        return ControlFlowIfStatement(0, ControlFlowIfStatement(1, ControlFlowIfStatement(2)));
                    }
                }

                IfStatementSyntax ControlFlowIfStatement(object value, StatementSyntax elseClause = null)
                    => IfStatement(
                        BinaryExpression(SyntaxKind.EqualsExpression, IdentifierName(FlowControlName), LiteralExpression(value)),
                        Block(GetFlowStatement(value)),
                        elseClause == null ? null : ElseClause(elseClause));

                SwitchStatementSyntax NoBreakSwitchStatement()
                    => SwitchStatement(
                        IdentifierName(FlowControlName), [
                            SwitchSection(
                                [CaseSwitchLabel(CaseKeyword.WithTrailingTrivia(Space), LiteralExpression(false).WithoutTrivia(), ColonToken.WithTrailingTrivia(Space)).WithoutLeadingTrivia()],
                                [GetFlowStatement(false).WithoutTrivia()]),
                            SwitchSection(
                                [CaseSwitchLabel(CaseKeyword.WithTrailingTrivia(Space), LiteralExpression(true).WithoutTrivia(), ColonToken.WithTrailingTrivia(Space)).WithoutLeadingTrivia()],
                                [GetFlowStatement(true).WithoutTrivia()])]);

                StatementSyntax Block(StatementSyntax statement)
                    => useBlock ? SyntaxFactory.Block(statement) : statement;

                ExpressionSyntax LiteralExpression(object value)
                    => ExpressionGenerator.GenerateExpression(null, value, canUseFieldReference: false);

                StatementSyntax GetFlowStatement(object value)
                {
                    // Being explicit about trivia ensures the formatter doesn't insert newlines in undesirable places.

                    if (flowControlInformation.TryGetBreakFlowValue(out var breakValue) && Equals(breakValue, value))
                        return CreateBreakStatement();
                    else if (flowControlInformation.TryGetContinueFlowValue(out var continueValue) && Equals(continueValue, value))
                        return CreateContinueStatement();
                    else if (flowControlInformation.TryGetReturnFlowValue(out var returnValue) && Equals(returnValue, value))
                        return ReturnStatement(ReturnKeyword.WithoutTrailingTrivia(), this.AnalyzerResult.CoreReturnType.SpecialType == SpecialType.System_Void ? null : IdentifierName(ReturnValueName).WithLeadingTrivia(Space).WithoutTrailingTrivia(), SemicolonToken.WithoutLeadingTrivia());
                    else
                        throw ExceptionUtilities.Unreachable();
                }
            }

            protected override bool ShouldLocalFunctionCaptureParameter(SyntaxNode node)
                => node.SyntaxTree.Options.LanguageVersion() < LanguageVersion.CSharp8;

            private static bool IsExpressionBodiedMember(SyntaxNode node)
                => node is MemberDeclarationSyntax member && member.GetExpressionBody() != null;

            private static bool IsExpressionBodiedAccessor(SyntaxNode node)
                => node is AccessorDeclarationSyntax { ExpressionBody: not null };

            private SimpleNameSyntax CreateMethodNameForInvocation()
            {
                return AnalyzerResult.MethodTypeParametersInDeclaration.IsEmpty
                    ? IdentifierName(_methodName)
                    : GenericName(_methodName, TypeArgumentList(CreateMethodCallTypeVariables()));
            }

            private SeparatedSyntaxList<TypeSyntax> CreateMethodCallTypeVariables()
            {
                Contract.ThrowIfTrue(AnalyzerResult.MethodTypeParametersInDeclaration.IsEmpty);

                // propagate any type variable used in extracted code
                return [.. AnalyzerResult.MethodTypeParametersInDeclaration.Select(m => SyntaxFactory.ParseTypeName(m.Name))];
            }

            protected override SyntaxNode GetCallSiteContainerFromOutermostMoveInVariable()
            {
                var outmostVariable = GetOutermostVariableToMoveIntoMethodDefinition();
                if (outmostVariable == null)
                    return null;

                var idToken = outmostVariable.GetIdentifierTokenAtDeclaration(SemanticDocument);
                var declStatement = idToken.GetAncestor<LocalDeclarationStatementSyntax>();
                Contract.ThrowIfNull(declStatement);
                Contract.ThrowIfFalse(declStatement.Parent.IsStatementContainerNode());

                return declStatement.Parent;
            }

            private bool ShouldPutUnsafeModifier()
            {
                var token = this.SelectionResult.GetFirstTokenInSelection();
                var ancestors = token.GetAncestors<SyntaxNode>();

                // if enclosing type contains unsafe keyword, we don't need to put it again
                if (ancestors.Where(a => CSharp.SyntaxFacts.IsTypeDeclaration(a.Kind()))
                             .Cast<MemberDeclarationSyntax>()
                             .Any(m => m.GetModifiers().Any(SyntaxKind.UnsafeKeyword)))
                {
                    return false;
                }

                return token.Parent.IsUnsafeContext();
            }

            private DeclarationModifiers CreateMethodModifiers()
            {
                var isUnsafe = ShouldPutUnsafeModifier();
                var isAsync = this.SelectionResult.ContainsAwaitExpression();
                var isStatic = !AnalyzerResult.UseInstanceMember;
                var isReadOnly = AnalyzerResult.ShouldBeReadOnly;

                // Static local functions are only supported in C# 8.0 and later
                var languageVersion = SemanticDocument.SyntaxTree.Options.LanguageVersion();

                if (LocalFunction && (!Options.PreferStaticLocalFunction.Value || languageVersion < LanguageVersion.CSharp8))
                {
                    isStatic = false;
                }

                // UseInstanceMember will be false for interface members, but extracting a non-static
                // member to a static member has a very different meaning for interfaces so we need
                // an extra check here.
                if (!LocalFunction && IsNonStaticInterfaceMember())
                {
                    isStatic = false;
                }

                return new DeclarationModifiers(
                    isUnsafe: isUnsafe,
                    isAsync: isAsync,
                    isStatic: isStatic,
                    isReadOnly: isReadOnly);
            }

            private bool IsNonStaticInterfaceMember()
            {
                var typeDecl = SelectionResult.GetContainingScopeOf<BaseTypeDeclarationSyntax>();
                if (typeDecl is null)
                    return false;

                if (!typeDecl.IsKind(SyntaxKind.InterfaceDeclaration))
                    return false;

                var memberDecl = SelectionResult.GetContainingScopeOf<MemberDeclarationSyntax>();
                if (memberDecl is null)
                    return false;

                return !memberDecl.Modifiers.Any(SyntaxKind.StaticKeyword);
            }

            private static SyntaxKind GetParameterRefSyntaxKind(ParameterBehavior parameterBehavior)
            {
                return parameterBehavior == ParameterBehavior.Ref
                        ? SyntaxKind.RefKeyword
                            : parameterBehavior == ParameterBehavior.Out ?
                                SyntaxKind.OutKeyword : SyntaxKind.None;
            }

            private ImmutableArray<StatementSyntax> CreateMethodBody(
                SyntaxNode insertionPoint, CancellationToken cancellationToken)
            {
                var statements = GetInitialStatementsForMethodDefinitions();

                statements = ConvertComplexControlFlowStatements(statements);
                statements = SplitOrMoveDeclarationIntoMethodDefinition(insertionPoint, statements, cancellationToken);
                statements = MoveDeclarationOutFromMethodDefinition(statements, cancellationToken);
                statements = AppendReturnStatementIfNeeded(statements);
                statements = CleanupCode(statements);

                return statements;
            }

            /// <summary>
            /// Converts existing <c>break, continue, and return</c> statements into a <c>return</c> statement that
            /// returns which control flow construct was hit.
            /// </summary>
            /// <param name="statements"></param>
            private ImmutableArray<StatementSyntax> ConvertComplexControlFlowStatements(ImmutableArray<StatementSyntax> statements)
                => statements.SelectAsArray(s => ConvertComplexControlFlowStatement(s));

            private StatementSyntax ConvertComplexControlFlowStatement(StatementSyntax statement)
            {
                return statement.ReplaceNodes(
                    statement.GetAnnotatedNodes(ExitPointAnnotation),
                    (original, current) =>
                    {
                        return ConvertExitPoint(current).WithTriviaFrom(current);
                    });

                SyntaxNode ConvertExitPoint(SyntaxNode current)
                {
                    var flowControlInformation = this.AnalyzerResult.FlowControlInformation;
                    if (current is BreakStatementSyntax breakStatement)
                    {
                        // TODO: pass back more than just the control flow value if needed.
                        var returnStatement = flowControlInformation.TryGetBreakFlowValue(out var flowValue)
                            ? ReturnStatement(CreateFlowControlReturnExpression(flowControlInformation, flowValue))
                            : CreateReturnStatementForReturnedVariables();
                        return returnStatement.WithSemicolonToken(breakStatement.SemicolonToken);
                    }
                    else if (current is ContinueStatementSyntax continueStatement)
                    {
                        // TODO: pass back more than just the control flow value if needed.
                        var returnStatement = flowControlInformation.TryGetContinueFlowValue(out var flowValue)
                            ? ReturnStatement(CreateFlowControlReturnExpression(flowControlInformation, flowValue))
                            : CreateReturnStatementForReturnedVariables();
                        return returnStatement.WithSemicolonToken(continueStatement.SemicolonToken);
                    }
                    else if (current is ReturnStatementSyntax returnStatement)
                    {
                        if (flowControlInformation.TryGetReturnFlowValue(out var flowValue))
                        {
                            var returnExpr = returnStatement.Expression;
                            if (returnExpr != null)
                            {
                                // The code we're extracting is returning values as well.  Ensure that we return both
                                // the control flow value and the original value the code was returning.
                                var tupleExpression = TupleExpression([
                                    Argument(NameColon(IdentifierName(FlowControlName)), refKindKeyword: default, ExpressionGenerator.GenerateExpression(flowControlInformation.ControlFlowValueType, flowValue, canUseFieldReference: false)),
                                    Argument(NameColon(IdentifierName(ReturnValueName)), refKindKeyword: default, returnExpr.WithoutTrivia())]).WithTriviaFrom(returnExpr);
                                return returnStatement.WithExpression(tupleExpression);
                            }
                            else
                            {
                                // The code we're extracting has no other values to return outwards, just the control
                                // flow value.  In that case, we can just return the control flow value directly
                                // indicating that we hit a return statement.
                                return returnStatement.WithExpression(
                                    ExpressionGenerator.GenerateExpression(flowControlInformation.ControlFlowValueType, flowValue, canUseFieldReference: false));
                            }
                        }

                        // No advanced flow control.  Just have the return statement return as it normally did.  It can
                        // be a normal `return;` or a `return expr;`.  In the former case the caller will just call into
                        // the new method and do an immediate `return;` after that itself.  In the latter case the
                        // caller will change to `return NewMethod();` to pass that value upwards.
                        return current;
                    }
                    else
                    {
                        // A different type of flow control construct (goto, yield, perhaps others).  Just leave as is. g
                        return current;
                    }
                }

                ReturnStatementSyntax CreateReturnStatementForReturnedVariables()
                    => ReturnStatement(this.AnalyzerResult.VariablesToUseAsReturnValue.Length switch
                    {
                        0 => null,
                        1 => this.AnalyzerResult.VariablesToUseAsReturnValue[0].Name.ToIdentifierName(),
                        _ => TupleExpression([.. this.AnalyzerResult.VariablesToUseAsReturnValue.Select(
                            v => Argument(v.Name.ToIdentifierName()))]),
                    });
            }

            protected override ExpressionSyntax CreateFlowControlReturnExpression(ExtractMethodFlowControlInformation flowControlInformation, object flowValue)
            {
                var flowValueExpression = ExpressionGenerator.GenerateExpression(
                    flowControlInformation.ControlFlowValueType, flowValue, canUseFieldReference: false);
                if (this.AnalyzerResult.CoreReturnType.SpecialType == SpecialType.System_Void)
                    return flowValueExpression;

                // For reference types, return 'null', for everything else return 'default'.  TODO: in the future we
                // should update this to return `null!` or `default!` if in a nullable context and not a value type.
                var methodReturnDefaultValue = this.AnalyzerResult.CoreReturnType.IsReferenceType
                    ? LiteralExpression(SyntaxKind.NullLiteralExpression)
                    : LiteralExpression(SyntaxKind.DefaultLiteralExpression);

                return TupleExpression([
                    Argument(NameColon(IdentifierName(FlowControlName)), refKindKeyword: default, flowValueExpression),
                    Argument(NameColon(IdentifierName(ReturnValueName)), refKindKeyword: default, methodReturnDefaultValue)]);
            }

            protected SyntaxKind UnderCheckedExpressionContext()
                => UnderCheckedContext<CheckedExpressionSyntax>();

            protected SyntaxKind UnderCheckedStatementContext()
                => UnderCheckedContext<CheckedStatementSyntax>();

            protected SyntaxKind UnderCheckedContext<T>() where T : SyntaxNode
            {
                var token = this.SelectionResult.GetFirstTokenInSelection();
                var contextNode = token.Parent.GetAncestor<T>();
                if (contextNode == null)
                {
                    return SyntaxKind.None;
                }

                return contextNode.Kind();
            }

            private ImmutableArray<StatementSyntax> WrapInCheckStatementIfNeeded(ImmutableArray<StatementSyntax> statements)
            {
                var kind = UnderCheckedStatementContext();
                if (kind == SyntaxKind.None)
                    return statements;

                return statements is [BlockSyntax block]
                    ? [CheckedStatement(kind, block)]
                    : [CheckedStatement(kind, Block(statements))];
            }

            private static ImmutableArray<StatementSyntax> CleanupCode(ImmutableArray<StatementSyntax> statements)
            {
                statements = PostProcessor.RemoveRedundantBlock(statements);
                statements = PostProcessor.RemoveDeclarationAssignmentPattern(statements);
                statements = PostProcessor.RemoveInitializedDeclarationAndReturnPattern(statements);

                return statements;
            }

            private static OperationStatus CheckActiveStatements(ImmutableArray<StatementSyntax> statements)
            {
                if (statements.IsEmpty)
                    return OperationStatus.NoActiveStatement;

                if (statements is [ReturnStatementSyntax { Expression: null }])
                    return OperationStatus.NoActiveStatement;

                // Look for at least one non local-variable-decl statement, or at least one local variable with an initializer.
                foreach (var statement in statements)
                {
                    if (statement is not LocalDeclarationStatementSyntax declStatement)
                        return OperationStatus.SucceededStatus;

                    foreach (var variable in declStatement.Declaration.Variables)
                    {
                        if (variable.Initializer != null)
                            return OperationStatus.SucceededStatus;
                    }
                }

                return OperationStatus.NoActiveStatement;
            }

            private ImmutableArray<StatementSyntax> MoveDeclarationOutFromMethodDefinition(
                ImmutableArray<StatementSyntax> statements, CancellationToken cancellationToken)
            {
                using var _1 = ArrayBuilder<StatementSyntax>.GetInstance(out var result);

                var variableToRemoveMap = CreateVariableDeclarationToRemoveMap(
                    AnalyzerResult.GetVariablesToMoveOutToCallSiteOrDelete(), cancellationToken);

                statements = statements.SelectAsArray(s => FixDeclarationExpressionsAndDeclarationPatterns(s, variableToRemoveMap));

                foreach (var statement in statements)
                {
                    if (statement is not LocalDeclarationStatementSyntax declarationStatement || declarationStatement.Declaration.Variables.FullSpan.IsEmpty)
                    {
                        // if given statement is not decl statement.
                        result.Add(statement);
                        continue;
                    }

                    using var _2 = ArrayBuilder<StatementSyntax>.GetInstance(out var expressionStatements);
                    using var _3 = ArrayBuilder<VariableDeclaratorSyntax>.GetInstance(out var variables);
                    using var _4 = ArrayBuilder<SyntaxTrivia>.GetInstance(out var triviaList);

                    // When we modify the declaration to an initialization we have to preserve the leading trivia
                    var firstVariableToAttachTrivia = true;

                    var isUsingDeclarationAsReturnValue = this.AnalyzerResult.VariablesToUseAsReturnValue is [var variable] &&
                        variable.GetOriginalIdentifierToken(cancellationToken) != default &&
                        variable.GetIdentifierTokenAtDeclaration(declarationStatement) != default;

                    // go through each var decls in decl statement, and create new assignment if
                    // variable is initialized at decl.
                    foreach (var variableDeclaration in declarationStatement.Declaration.Variables)
                    {
                        if (variableToRemoveMap.HasSyntaxAnnotation(variableDeclaration))
                        {
                            if (variableDeclaration.Initializer != null)
                            {
                                var identifier = ApplyTriviaFromDeclarationToAssignmentIdentifier(declarationStatement, firstVariableToAttachTrivia, variableDeclaration);

                                // move comments with the variable here
                                expressionStatements.Add(ExpressionStatement(AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression, IdentifierName(identifier), variableDeclaration.Initializer.Value)));
                            }
                            else
                            {
                                // we don't remove trivia around tokens we remove
                                triviaList.AddRange(variableDeclaration.GetLeadingTrivia());
                                triviaList.AddRange(variableDeclaration.GetTrailingTrivia());
                            }

                            firstVariableToAttachTrivia = false;
                            continue;
                        }

                        // Prepend the trivia from the declarations without initialization to the next persisting variable declaration
                        if (triviaList.Count > 0)
                        {
                            variables.Add(variableDeclaration.WithPrependedLeadingTrivia(triviaList));
                            triviaList.Clear();
                            firstVariableToAttachTrivia = false;
                            continue;
                        }

                        firstVariableToAttachTrivia = false;
                        variables.Add(variableDeclaration);
                    }

                    if (variables.Count == 0 && triviaList.Count > 0)
                    {
                        // well, there are trivia associated with the node.
                        // we can't just delete the node since then, we will lose
                        // the trivia. unfortunately, it is not easy to attach the trivia
                        // to next token. for now, create an empty statement and associate the
                        // trivia to the statement

                        // TODO : think about a way to trivia attached to next token
                        result.Add(EmptyStatement(Token([.. triviaList], SyntaxKind.SemicolonToken, [ElasticMarker])));
                        triviaList.Clear();
                    }

                    // return survived var decls
                    if (variables.Count > 0)
                    {
                        result.Add(LocalDeclarationStatement(
                            isUsingDeclarationAsReturnValue ? default : declarationStatement.AwaitKeyword,
                            isUsingDeclarationAsReturnValue ? default : declarationStatement.UsingKeyword,
                            declarationStatement.Modifiers,
                            VariableDeclaration(
                                declarationStatement.Declaration.Type,
                                [.. variables]),
                            declarationStatement.SemicolonToken.WithPrependedLeadingTrivia(triviaList)));
                        triviaList.Clear();
                    }

                    // return any expression statement if there was any
                    result.AddRange(expressionStatements);
                }

                return result.ToImmutableAndClear();
            }

            /// <summary>
            /// If the statement has an <c>out var</c> declaration expression for a variable which
            /// needs to be removed, we need to turn it into a plain <c>out</c> parameter, so that
            /// it doesn't declare a duplicate variable.
            /// If the statement has a pattern declaration (such as <c>3 is int i</c>) for a variable
            /// which needs to be removed, we will annotate it as a conflict, since we don't have
            /// a better refactoring.
            /// </summary>
            private static StatementSyntax FixDeclarationExpressionsAndDeclarationPatterns(StatementSyntax statement,
                HashSet<SyntaxAnnotation> variablesToRemove)
            {
                var replacements = new Dictionary<SyntaxNode, SyntaxNode>();

                var declarations = statement.DescendantNodes()
                    .Where(n => n.Kind() is SyntaxKind.DeclarationExpression or SyntaxKind.DeclarationPattern);

                foreach (var node in declarations)
                {
                    switch (node.Kind())
                    {
                        case SyntaxKind.DeclarationExpression:
                            {
                                var declaration = (DeclarationExpressionSyntax)node;
                                if (declaration.Designation.Kind() != SyntaxKind.SingleVariableDesignation)
                                {
                                    break;
                                }

                                var designation = (SingleVariableDesignationSyntax)declaration.Designation;
                                var name = designation.Identifier.ValueText;
                                if (variablesToRemove.HasSyntaxAnnotation(designation))
                                {
                                    var newLeadingTrivia = new SyntaxTriviaList();
                                    newLeadingTrivia = newLeadingTrivia.AddRange(declaration.Type.GetLeadingTrivia());
                                    newLeadingTrivia = newLeadingTrivia.AddRange(declaration.Type.GetTrailingTrivia());
                                    newLeadingTrivia = newLeadingTrivia.AddRange(designation.GetLeadingTrivia());

                                    replacements.Add(declaration, IdentifierName(designation.Identifier)
                                        .WithLeadingTrivia(newLeadingTrivia));
                                }

                                break;
                            }

                        case SyntaxKind.DeclarationPattern:
                            {
                                var pattern = (DeclarationPatternSyntax)node;
                                if (!variablesToRemove.HasSyntaxAnnotation(pattern))
                                {
                                    break;
                                }

                                // We don't have a good refactoring for this, so we just annotate the conflict
                                // For instance, when a local declared by a pattern declaration (`3 is int i`) is
                                // used outside the block we're trying to extract.
                                if (pattern.Designation is not SingleVariableDesignationSyntax designation)
                                {
                                    break;
                                }

                                var identifier = designation.Identifier;
                                var annotation = ConflictAnnotation.Create(FeaturesResources.Conflict_s_detected);
                                var newIdentifier = identifier.WithAdditionalAnnotations(annotation);
                                var newDesignation = designation.WithIdentifier(newIdentifier);
                                replacements.Add(pattern, pattern.WithDesignation(newDesignation));

                                break;
                            }
                    }
                }

                return statement.ReplaceNodes(replacements.Keys, (orig, partiallyReplaced) => replacements[orig]);
            }

            private static SyntaxToken ApplyTriviaFromDeclarationToAssignmentIdentifier(LocalDeclarationStatementSyntax declarationStatement, bool firstVariableToAttachTrivia, VariableDeclaratorSyntax variable)
            {
                var identifier = variable.Identifier;
                var typeSyntax = declarationStatement.Declaration.Type;
                if (firstVariableToAttachTrivia && typeSyntax != null)
                {
                    var identifierLeadingTrivia = new SyntaxTriviaList();

                    if (typeSyntax.HasLeadingTrivia)
                    {
                        identifierLeadingTrivia = identifierLeadingTrivia.AddRange(typeSyntax.GetLeadingTrivia());
                    }

                    identifierLeadingTrivia = identifierLeadingTrivia.AddRange(identifier.LeadingTrivia);
                    identifier = identifier.WithLeadingTrivia(identifierLeadingTrivia);
                }

                return identifier;
            }

            private ImmutableArray<StatementSyntax> SplitOrMoveDeclarationIntoMethodDefinition(
                SyntaxNode insertionPointNode,
                ImmutableArray<StatementSyntax> statements,
                CancellationToken cancellationToken)
            {
                var semanticModel = SemanticDocument.SemanticModel;
                var postProcessor = new PostProcessor(semanticModel, insertionPointNode.SpanStart);

                var declStatements = CreateDeclarationStatements(AnalyzerResult.GetVariablesToSplitOrMoveIntoMethodDefinition(), cancellationToken);
                declStatements = postProcessor.MergeDeclarationStatements(declStatements);

                return [.. declStatements, .. statements];
            }

            protected override bool LastStatementOrHasReturnStatementInReturnableConstruct()
            {
                var lastStatement = GetLastStatementOrInitializerSelectedAtCallSite();
                var container = lastStatement.GetAncestorsOrThis<SyntaxNode>().FirstOrDefault(n => n.IsReturnableConstruct());
                if (container == null)
                {
                    // case such as field initializer
                    return false;
                }

                var blockBody = container.GetBlockBody();
                if (blockBody == null)
                {
                    // such as expression lambda. there is no statement
                    return false;
                }

                // check whether it is last statement except return statement
                var statements = blockBody.Statements;
                if (statements.Last() == lastStatement)
                {
                    return true;
                }

                var index = statements.IndexOf((StatementSyntax)lastStatement);
                return statements[index + 1].Kind() == SyntaxKind.ReturnStatement;
            }

            protected override ExpressionSyntax CreateCallSignature()
            {
                var methodName = CreateMethodNameForInvocation().WithAdditionalAnnotations(Simplifier.Annotation);
                ExpressionSyntax methodExpression =
                    this.AnalyzerResult.UseInstanceMember && this.ExtractMethodGenerationOptions.SimplifierOptions.QualifyMethodAccess.Value && !LocalFunction
                    ? MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), methodName)
                    : methodName;

                var isLocalFunction = LocalFunction && ShouldLocalFunctionCaptureParameter(SemanticDocument.Root);

                using var _ = ArrayBuilder<ArgumentSyntax>.GetInstance(out var arguments);

                foreach (var argument in AnalyzerResult.MethodParameters)
                {
                    if (!isLocalFunction || !argument.CanBeCapturedByLocalFunction)
                    {
                        var modifier = GetParameterRefSyntaxKind(argument.ParameterModifier);
                        var refOrOut = modifier == SyntaxKind.None ? default : Token(modifier);
                        arguments.Add(Argument(IdentifierName(argument.Name)).WithRefOrOutKeyword(refOrOut));
                    }
                }

                var invocation = (ExpressionSyntax)InvocationExpression(methodExpression, ArgumentList([.. arguments]));

                // If we're extracting any code that contained an 'await' then we'll have to await the new method we're
                // calling as well.  If we also see any use of .ConfigureAwait(false) in the extracted code, keep that
                // pattern on the await expression we produce.
                if (this.SelectionResult.ContainsAwaitExpression())
                {
                    if (this.SelectionResult.ContainsConfigureAwaitFalse())
                    {
                        invocation = InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                invocation,
                                IdentifierName(nameof(Task.ConfigureAwait))),
                            ArgumentList([Argument(LiteralExpression(SyntaxKind.FalseLiteralExpression))]));
                    }

                    invocation = AwaitExpression(invocation);
                }

                if (AnalyzerResult.ReturnsByRef)
                    invocation = RefExpression(invocation);

                return invocation;
            }

            protected override StatementSyntax CreateAssignmentExpressionStatement(
                ImmutableArray<VariableInfo> variableInfos,
                ExpressionSyntax right)
            {
                Contract.ThrowIfTrue(variableInfos.IsEmpty);

                return ExpressionStatement(AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    variableInfos is [var singleInfo]
                        ? singleInfo.Name.ToIdentifierName()
                        : TupleExpression([.. variableInfos.Select(v => Argument(v.Name.ToIdentifierName()))]),
                    right));
            }

            protected override StatementSyntax CreateDeclarationStatement(
                ImmutableArray<VariableInfo> variableInfos,
                ExpressionSyntax initialValue,
                ExtractMethodFlowControlInformation flowControlInformation,
                CancellationToken cancellationToken)
            {
                var needsControlFlowValue = flowControlInformation?.NeedsControlFlowValue() is true;
                Contract.ThrowIfTrue(variableInfos.IsEmpty && !needsControlFlowValue);

                var hasNonControlFlowReturnValue = variableInfos.Length > 0 || this.AnalyzerResult.CoreReturnType.SpecialType != SpecialType.System_Void;

                var equalsValueClause = initialValue == null ? null : EqualsValueClause(initialValue);
                if (variableInfos is [var singleVariable] && !needsControlFlowValue)
                {
                    var originalIdentifierToken = singleVariable.GetOriginalIdentifierToken(cancellationToken);

                    // Hierarchy being checked for to see if a using keyword is needed is
                    // Token -> VariableDeclarator -> VariableDeclaration -> LocalDeclaration
                    var usingKeyword = originalIdentifierToken.Parent?.Parent?.Parent is LocalDeclarationStatementSyntax { UsingKeyword.FullSpan.IsEmpty: false }
                        ? UsingKeyword
                        : default;

                    return LocalDeclarationStatement(
                        VariableDeclaration(
                            singleVariable.SymbolType.GenerateTypeSyntax(),
                            [VariableDeclarator(singleVariable.Name.ToIdentifierToken(), null, equalsValueClause)]))
                        .WithUsingKeyword(usingKeyword);
                }
                else if (!hasNonControlFlowReturnValue && needsControlFlowValue)
                {
                    // No actual return values, but we do have a control flow value.  Just generate:
                    // bool flowControl = NewMethod();
                    return LocalDeclarationStatement(
                        VariableDeclaration(
                            flowControlInformation.ControlFlowValueType.GenerateTypeSyntax(),
                            [VariableDeclarator(FlowControlName.ToIdentifierToken(), null, equalsValueClause)]));
                }

                // Otherwise we have non-control-flow and/or control-flow return values.  Generate assignments in this
                // case for all the variables that need it.
                return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, CreateLeftExpression(), initialValue));

                ExpressionSyntax CreateLeftExpression()
                {
                    // Note, we do not use "Use var when apparent" here as no types are apparent when doing `... =
                    // NewMethod()`. If we have `use var elsewhere` we may try to generate `var (a, b, c)` if we're
                    // producing new variables for all variable infos.  If we're producing new variables only for some
                    // variables, we'll need to do something like `(Type a, b, c)`.  In that case, we'll use 'var' if
                    // the type is a built-in type, and varForBuiltInTypes is true.  Otherwise, if it's not built-in,
                    // we'll use "use var elsewhere" to determine what to do.
                    var varElsewhere = ((CSharpSimplifierOptions)this.ExtractMethodGenerationOptions.SimplifierOptions).VarElsewhere.Value;

                    if (variableInfos.All(i => i.ReturnBehavior == ReturnBehavior.Initialization) && varElsewhere)
                    {
                        // Create `(a, b, c)` to represent the N values being returned.
                        VariableDesignationSyntax returnVariableParenthesizedDesignation = variableInfos.Length switch
                        {
                            0 => SingleVariableDesignation(ReturnValueName.ToIdentifierToken()),
                            1 => SingleVariableDesignation(variableInfos[0].Name.ToIdentifierToken()),
                            _ => ParenthesizedVariableDesignation([.. variableInfos.Select(v => SingleVariableDesignation(v.Name.ToIdentifierToken()))]),
                        };

                        if (needsControlFlowValue)
                        {
                            // create `var (flowControl, (a, b, c)) = NewMethod()`
                            return DeclarationExpression(
                                type: IdentifierName("var"),
                                ParenthesizedVariableDesignation([
                                    SingleVariableDesignation(FlowControlName.ToIdentifierToken()),
                                    returnVariableParenthesizedDesignation]));
                        }
                        else
                        {
                            // create `var (a, b, c) = NewMethod()`
                            return DeclarationExpression(
                                type: IdentifierName("var"),
                                returnVariableParenthesizedDesignation);
                        }
                    }
                    else
                    {
                        // Create `(int x, y, z)` to represent the N values being returned.
                        var returnVariableExpression = variableInfos.Length switch
                        {
                            0 => DeclarationExpression(this.AnalyzerResult.CoreReturnType.GenerateTypeSyntax(), SingleVariableDesignation(ReturnValueName.ToIdentifierToken())),
                            1 => CreateReturnExpression(variableInfos[0]),
                            _ => TupleExpression([.. variableInfos.Select(v => Argument(CreateReturnExpression(v)))]),
                        };

                        if (needsControlFlowValue)
                        {
                            // create `(bool flowControl, (int x, y, int z)) = NewMethod()`
                            return TupleExpression([
                                Argument(CreateFlowControlDeclarationExpression()),
                                Argument(returnVariableExpression)]);
                        }
                        else
                        {
                            // create `(int x, y, int z) = NewMethod()`
                            return returnVariableExpression;
                        }
                    }
                }

                ExpressionSyntax CreateReturnExpression(VariableInfo variableInfo)
                    => variableInfo.ReturnBehavior == ReturnBehavior.Initialization
                        ? DeclarationExpression(variableInfo.SymbolType.GenerateTypeSyntax(), SingleVariableDesignation(variableInfo.Name.ToIdentifierToken()))
                        : variableInfo.Name.ToIdentifierName();

                DeclarationExpressionSyntax CreateFlowControlDeclarationExpression()
                {
                    return DeclarationExpression(
                        flowControlInformation.ControlFlowValueType.GenerateTypeSyntax(),
                        SingleVariableDesignation(FlowControlName.ToIdentifierToken()));
                }
            }

            protected override async Task<SemanticDocument> PerformFinalTriviaFixupAsync(
                SemanticDocument newDocument, CancellationToken cancellationToken)
            {
                // in hybrid code cases such as extract method, formatter will have some difficulties on where it breaks lines in two.
                // here, we explicitly insert newline at the end of "{" of auto generated method decl so that anchor knows how to find out
                // indentation of inserted statements (from users code) with user code style preserved
                var root = newDocument.Root;
                var methodDefinition = root.GetAnnotatedNodes<SyntaxNode>(MethodDefinitionAnnotation).First();

                SyntaxNode newMethodDefinition = methodDefinition switch
                {
                    MethodDeclarationSyntax method => TweakNewLinesInMethod(method),
                    LocalFunctionStatementSyntax localFunction => TweakNewLinesInMethod(localFunction),
                    _ => throw new NotSupportedException("SyntaxNode expected to be MethodDeclarationSyntax or LocalFunctionStatementSyntax."),
                };

                newDocument = await newDocument.WithSyntaxRootAsync(
                    root.ReplaceNode(methodDefinition, newMethodDefinition), cancellationToken).ConfigureAwait(false);

                return newDocument;
            }

            private static MethodDeclarationSyntax TweakNewLinesInMethod(MethodDeclarationSyntax method)
                => TweakNewLinesInMethod(method, method.Body, method.ExpressionBody);

            private static LocalFunctionStatementSyntax TweakNewLinesInMethod(LocalFunctionStatementSyntax method)
                => TweakNewLinesInMethod(method, method.Body, method.ExpressionBody);

            private static TDeclarationNode TweakNewLinesInMethod<TDeclarationNode>(TDeclarationNode method, BlockSyntax body, ArrowExpressionClauseSyntax expressionBody) where TDeclarationNode : SyntaxNode
            {
                if (body != null)
                {
                    return method.ReplaceToken(
                            body.OpenBraceToken,
                            body.OpenBraceToken.WithAppendedTrailingTrivia(
                                ElasticCarriageReturnLineFeed));
                }
                else if (expressionBody != null)
                {
                    return method.ReplaceToken(
                            expressionBody.ArrowToken,
                            expressionBody.ArrowToken.WithPrependedLeadingTrivia(
                                ElasticCarriageReturnLineFeed));
                }
                else
                {
                    return method;
                }
            }

            protected override async Task<SemanticDocument> UpdateMethodAfterGenerationAsync(
                SemanticDocument originalDocument,
                IMethodSymbol methodSymbol,
                CancellationToken cancellationToken)
            {
                // Only need to update for nullable reference types in return
                if (methodSymbol.ReturnType.NullableAnnotation != NullableAnnotation.Annotated)
                    return originalDocument;

                var syntaxNode = originalDocument.Root.GetAnnotatedNodesAndTokens(MethodDefinitionAnnotation).FirstOrDefault().AsNode();
                var nodeIsMethodOrLocalFunction = syntaxNode is MethodDeclarationSyntax or LocalFunctionStatementSyntax;
                if (!nodeIsMethodOrLocalFunction)
                    return originalDocument;

                var nullableReturnOperations = CheckReturnOperations(syntaxNode, originalDocument, cancellationToken);
                if (nullableReturnOperations is not null)
                    return nullableReturnOperations;

                var returnType = syntaxNode is MethodDeclarationSyntax method ? method.ReturnType : ((LocalFunctionStatementSyntax)syntaxNode).ReturnType;
                var newDocument = await GenerateNewDocumentAsync(methodSymbol, returnType, originalDocument, cancellationToken).ConfigureAwait(false);

                return await SemanticDocument.CreateAsync(newDocument, cancellationToken).ConfigureAwait(false);

                static bool ReturnOperationBelongsToMethod(SyntaxNode returnOperationSyntax, SyntaxNode methodSyntax)
                {
                    var enclosingMethod = returnOperationSyntax.FirstAncestorOrSelf<SyntaxNode>(n => n switch
                    {
                        BaseMethodDeclarationSyntax _ => true,
                        AnonymousFunctionExpressionSyntax _ => true,
                        LocalFunctionStatementSyntax _ => true,
                        _ => false
                    });

                    return enclosingMethod == methodSyntax;
                }

                static SemanticDocument CheckReturnOperations(
                    SyntaxNode node,
                    SemanticDocument originalDocument,
                    CancellationToken cancellationToken)
                {
                    var semanticModel = originalDocument.SemanticModel;

                    var methodOperation = semanticModel.GetOperation(node, cancellationToken);
                    var returnOperations = methodOperation.DescendantsAndSelf().OfType<IReturnOperation>();

                    foreach (var returnOperation in returnOperations)
                    {
                        // If the return statement is located in a nested local function or lambda it
                        // shouldn't contribute to the nullability of the extracted method's return type
                        if (!ReturnOperationBelongsToMethod(returnOperation.Syntax, methodOperation.Syntax))
                            continue;

                        var syntax = returnOperation.ReturnedValue?.Syntax ?? returnOperation.Syntax;
                        var returnTypeInfo = semanticModel.GetTypeInfo(syntax, cancellationToken);
                        if (returnTypeInfo.Nullability.FlowState == NullableFlowState.MaybeNull)
                        {
                            // Flow state shows that return is correctly nullable
                            return originalDocument;
                        }
                    }

                    return null;
                }

                static async Task<Document> GenerateNewDocumentAsync(
                    IMethodSymbol methodSymbol,
                    TypeSyntax returnType,
                    SemanticDocument originalDocument,
                    CancellationToken cancellationToken)
                {
                    // Return type can be updated to not be null
                    var newType = methodSymbol.ReturnType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);

                    var oldRoot = await originalDocument.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var newRoot = oldRoot.ReplaceNode(returnType, newType.GenerateTypeSyntax(allowVar: false));

                    return originalDocument.Document.WithSyntaxRoot(newRoot);
                }
            }

            protected SyntaxToken GenerateMethodNameForStatementGenerators()
            {
                var semanticModel = SemanticDocument.SemanticModel;
                var nameGenerator = new UniqueNameGenerator(semanticModel);
                var scope = this.SelectionResult.GetContainingScope();

                // If extracting a local function, we want to ensure all local variables are considered when generating a unique name.
                if (LocalFunction)
                {
                    scope = this.SelectionResult.GetFirstTokenInSelection().Parent;
                }

                return Identifier(nameGenerator.CreateUniqueMethodName(scope, GenerateMethodNameFromUserPreference()));
            }

            protected string GenerateMethodNameFromUserPreference()
            {
                var methodName = NewMethodPascalCaseStr;
                if (!LocalFunction)
                {
                    return methodName;
                }

                // For local functions, pascal case and camel case should be the most common and therefore we only consider those cases.
                var localFunctionPreferences = Options.NamingStyle.SymbolSpecifications.Where(symbol => symbol.AppliesTo(new SymbolSpecification.SymbolKindOrTypeKind(MethodKind.LocalFunction), CreateMethodModifiers().Modifiers, null));

                var namingRules = Options.NamingStyle.Rules.NamingRules;
                var localFunctionKind = new SymbolSpecification.SymbolKindOrTypeKind(MethodKind.LocalFunction);
                if (LocalFunction)
                {
                    if (namingRules.Any(static (rule, arg) => rule.NamingStyle.CapitalizationScheme.Equals(Capitalization.CamelCase) && rule.SymbolSpecification.AppliesTo(arg.localFunctionKind, arg.self.CreateMethodModifiers().Modifiers, null), (self: this, localFunctionKind)))
                    {
                        methodName = NewMethodCamelCaseStr;
                    }
                }

                // We default to pascal case.
                return methodName;
            }
        }
    }
}
