// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpMethodExtractor
    {
        private abstract partial class CSharpCodeGenerator : CodeGenerator<StatementSyntax, ExpressionSyntax, SyntaxNode>
        {
            private readonly SyntaxToken _methodName;

            public static Task<GeneratedCode> GenerateAsync(
                InsertionPoint insertionPoint,
                SelectionResult selectionResult,
                AnalyzerResult analyzerResult,
                CancellationToken cancellationToken)
            {
                var codeGenerator = Create(insertionPoint, selectionResult, analyzerResult);
                return codeGenerator.GenerateAsync(cancellationToken);
            }

            private static CSharpCodeGenerator Create(
                InsertionPoint insertionPoint,
                SelectionResult selectionResult,
                AnalyzerResult analyzerResult)
            {
                if (ExpressionCodeGenerator.IsExtractMethodOnExpression(selectionResult))
                {
                    return new ExpressionCodeGenerator(insertionPoint, selectionResult, analyzerResult);
                }

                if (SingleStatementCodeGenerator.IsExtractMethodOnSingleStatement(selectionResult))
                {
                    return new SingleStatementCodeGenerator(insertionPoint, selectionResult, analyzerResult);
                }

                if (MultipleStatementsCodeGenerator.IsExtractMethodOnMultipleStatements(selectionResult))
                {
                    return new MultipleStatementsCodeGenerator(insertionPoint, selectionResult, analyzerResult);
                }

                return Contract.FailWithReturn<CSharpCodeGenerator>("Unknown selection");
            }

            protected CSharpCodeGenerator(
                InsertionPoint insertionPoint,
                SelectionResult selectionResult,
                AnalyzerResult analyzerResult)
                : base(insertionPoint, selectionResult, analyzerResult)
            {
                Contract.ThrowIfFalse(this.SemanticDocument == selectionResult.SemanticDocument);

                var nameToken = CreateMethodName();
                _methodName = nameToken.WithAdditionalAnnotations(this.MethodNameAnnotation);
            }

            private CSharpSelectionResult CSharpSelectionResult
            {
                get { return (CSharpSelectionResult)this.SelectionResult; }
            }

            protected override SyntaxNode GetPreviousMember(SemanticDocument document)
            {
                var node = this.InsertionPoint.With(document).GetContext();
                return (node.Parent is GlobalStatementSyntax) ? node.Parent : node;
            }

            protected override OperationStatus<IMethodSymbol> GenerateMethodDefinition(CancellationToken cancellationToken)
            {
                var result = CreateMethodBody(cancellationToken);

                var methodSymbol = CodeGenerationSymbolFactory.CreateMethodSymbol(
                    attributes: ImmutableArray<AttributeData>.Empty,
                    accessibility: Accessibility.Private,
                    modifiers: CreateMethodModifiers(),
                    returnType: this.AnalyzerResult.ReturnType,
                    refKind: RefKind.None,
                    explicitInterfaceImplementations: default,
                    name: _methodName.ToString(),
                    typeParameters: CreateMethodTypeParameters(),
                    parameters: CreateMethodParameters(),
                    statements: result.Data);

                return result.With(
                    this.MethodDefinitionAnnotation.AddAnnotationToSymbol(
                        Formatter.Annotation.AddAnnotationToSymbol(methodSymbol)));
            }

            protected override async Task<SyntaxNode> GenerateBodyForCallSiteContainerAsync(CancellationToken cancellationToken)
            {
                var container = this.GetOutermostCallSiteContainerToProcess(cancellationToken);
                var variableMapToRemove = CreateVariableDeclarationToRemoveMap(
                    this.AnalyzerResult.GetVariablesToMoveIntoMethodDefinition(cancellationToken), cancellationToken);
                var firstStatementToRemove = GetFirstStatementOrInitializerSelectedAtCallSite();
                var lastStatementToRemove = GetLastStatementOrInitializerSelectedAtCallSite();

                Contract.ThrowIfFalse(firstStatementToRemove.Parent == lastStatementToRemove.Parent);

                var statementsToInsert = await CreateStatementsOrInitializerToInsertAtCallSiteAsync(cancellationToken).ConfigureAwait(false);

                var callSiteGenerator =
                    new CallSiteContainerRewriter(
                        container,
                        variableMapToRemove,
                        firstStatementToRemove,
                        lastStatementToRemove,
                        statementsToInsert);

                return container.CopyAnnotationsTo(callSiteGenerator.Generate()).WithAdditionalAnnotations(Formatter.Annotation);
            }

            private async Task<IEnumerable<SyntaxNode>> CreateStatementsOrInitializerToInsertAtCallSiteAsync(CancellationToken cancellationToken)
            {
                var selectedNode = this.GetFirstStatementOrInitializerSelectedAtCallSite();

                // field initializer, constructor initializer, expression bodied member case
                if (selectedNode is ConstructorInitializerSyntax ||
                    selectedNode is FieldDeclarationSyntax ||
                    IsExpressionBodiedMember(selectedNode) ||
                    IsExpressionBodiedAccessor(selectedNode))
                {
                    var statement = await GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(this.CallSiteAnnotation, cancellationToken).ConfigureAwait(false);
                    return SpecializedCollections.SingletonEnumerable(statement);
                }

                // regular case
                var semanticModel = this.SemanticDocument.SemanticModel;
                var context = this.InsertionPoint.GetContext();
                var postProcessor = new PostProcessor(semanticModel, context.SpanStart);
                var statements = SpecializedCollections.EmptyEnumerable<StatementSyntax>();

                statements = AddSplitOrMoveDeclarationOutStatementsToCallSite(statements, cancellationToken);
                statements = postProcessor.MergeDeclarationStatements(statements);
                statements = AddAssignmentStatementToCallSite(statements, cancellationToken);
                statements = await AddInvocationAtCallSiteAsync(statements, cancellationToken).ConfigureAwait(false);
                statements = AddReturnIfUnreachable(statements);

                return statements;
            }

            private bool IsExpressionBodiedMember(SyntaxNode node)
                => node is MemberDeclarationSyntax member && member.GetExpressionBody() != null;

            private bool IsExpressionBodiedAccessor(SyntaxNode node)
                => node is AccessorDeclarationSyntax accessor && accessor.ExpressionBody != null;

            private SimpleNameSyntax CreateMethodNameForInvocation()
            {
                return this.AnalyzerResult.MethodTypeParametersInDeclaration.Count == 0
                    ? (SimpleNameSyntax)SyntaxFactory.IdentifierName(_methodName)
                    : SyntaxFactory.GenericName(_methodName, SyntaxFactory.TypeArgumentList(CreateMethodCallTypeVariables()));
            }

            private SeparatedSyntaxList<TypeSyntax> CreateMethodCallTypeVariables()
            {
                Contract.ThrowIfTrue(this.AnalyzerResult.MethodTypeParametersInDeclaration.Count == 0);

                // propagate any type variable used in extracted code
                var typeVariables = new List<TypeSyntax>();
                foreach (var methodTypeParameter in this.AnalyzerResult.MethodTypeParametersInDeclaration)
                {
                    typeVariables.Add(SyntaxFactory.ParseTypeName(methodTypeParameter.Name));
                }

                return SyntaxFactory.SeparatedList(typeVariables);
            }

            protected SyntaxNode GetCallSiteContainerFromOutermostMoveInVariable(CancellationToken cancellationToken)
            {
                var outmostVariable = GetOutermostVariableToMoveIntoMethodDefinition(cancellationToken);
                if (outmostVariable == null)
                {
                    return null;
                }

                var idToken = outmostVariable.GetIdentifierTokenAtDeclaration(this.SemanticDocument);
                var declStatement = idToken.GetAncestor<LocalDeclarationStatementSyntax>();
                Contract.ThrowIfNull(declStatement);
                Contract.ThrowIfFalse(declStatement.Parent.IsStatementContainerNode());

                return declStatement.Parent;
            }

            private DeclarationModifiers CreateMethodModifiers()
            {
                var isUnsafe = this.CSharpSelectionResult.ShouldPutUnsafeModifier();
                var isAsync = this.CSharpSelectionResult.ShouldPutAsyncModifier();
                var isStatic = !this.AnalyzerResult.UseInstanceMember;
                var isReadOnly = this.AnalyzerResult.ShouldBeReadOnly;

                return new DeclarationModifiers(
                    isUnsafe: isUnsafe,
                    isAsync: isAsync,
                    isStatic: isStatic,
                    isReadOnly: isReadOnly);
            }

            private static SyntaxKind GetParameterRefSyntaxKind(ParameterBehavior parameterBehavior)
            {
                return parameterBehavior == ParameterBehavior.Ref ?
                        SyntaxKind.RefKeyword :
                            parameterBehavior == ParameterBehavior.Out ?
                                SyntaxKind.OutKeyword : SyntaxKind.None;
            }

            private OperationStatus<ImmutableArray<SyntaxNode>> CreateMethodBody(CancellationToken cancellationToken)
            {
                var statements = GetInitialStatementsForMethodDefinitions();

                statements = SplitOrMoveDeclarationIntoMethodDefinition(statements, cancellationToken);
                statements = MoveDeclarationOutFromMethodDefinition(statements, cancellationToken);
                statements = AppendReturnStatementIfNeeded(statements);
                statements = CleanupCode(statements);

                // set output so that we can use it in negative preview
                var wrapped = WrapInCheckStatementIfNeeded(statements);
                return CheckActiveStatements(statements).With(wrapped.ToImmutableArray<SyntaxNode>());
            }

            private IEnumerable<StatementSyntax> WrapInCheckStatementIfNeeded(IEnumerable<StatementSyntax> statements)
            {
                var kind = this.CSharpSelectionResult.UnderCheckedStatementContext();
                if (kind == SyntaxKind.None)
                {
                    return statements;
                }

                if (statements.Skip(1).Any())
                {
                    return SpecializedCollections.SingletonEnumerable<StatementSyntax>(SyntaxFactory.CheckedStatement(kind, SyntaxFactory.Block(statements)));
                }

                if (statements.Single() is BlockSyntax block)
                {
                    return SpecializedCollections.SingletonEnumerable<StatementSyntax>(SyntaxFactory.CheckedStatement(kind, block));
                }

                return SpecializedCollections.SingletonEnumerable<StatementSyntax>(SyntaxFactory.CheckedStatement(kind, SyntaxFactory.Block(statements)));
            }

            private IEnumerable<StatementSyntax> CleanupCode(IEnumerable<StatementSyntax> statements)
            {
                var semanticModel = this.SemanticDocument.SemanticModel;
                var context = this.InsertionPoint.GetContext();
                var postProcessor = new PostProcessor(semanticModel, context.SpanStart);

                statements = postProcessor.RemoveRedundantBlock(statements);
                statements = postProcessor.RemoveDeclarationAssignmentPattern(statements);
                statements = postProcessor.RemoveInitializedDeclarationAndReturnPattern(statements);

                return statements;
            }

            private OperationStatus CheckActiveStatements(IEnumerable<StatementSyntax> statements)
            {
                var count = statements.Count();
                if (count == 0)
                {
                    return OperationStatus.NoActiveStatement;
                }

                if (count == 1)
                {
                    if (statements.Single() is ReturnStatementSyntax returnStatement && returnStatement.Expression == null)
                    {
                        return OperationStatus.NoActiveStatement;
                    }
                }

                foreach (var statement in statements)
                {
                    if (!(statement is LocalDeclarationStatementSyntax declStatement))
                    {
                        // found one
                        return OperationStatus.Succeeded;
                    }

                    foreach (var variable in declStatement.Declaration.Variables)
                    {
                        if (variable.Initializer != null)
                        {
                            // found one
                            return OperationStatus.Succeeded;
                        }
                    }
                }

                return OperationStatus.NoActiveStatement;
            }

            private IEnumerable<StatementSyntax> MoveDeclarationOutFromMethodDefinition(
                IEnumerable<StatementSyntax> statements, CancellationToken cancellationToken)
            {
                var variableToRemoveMap = CreateVariableDeclarationToRemoveMap(
                    this.AnalyzerResult.GetVariablesToMoveOutToCallSiteOrDelete(cancellationToken), cancellationToken);

                statements = statements.Select(s => FixDeclarationExpressionsAndDeclarationPatterns(s, variableToRemoveMap));

                foreach (var statement in statements)
                {
                    if (!(statement is LocalDeclarationStatementSyntax declarationStatement))
                    {
                        // if given statement is not decl statement.
                        yield return statement;
                        continue;
                    }

                    var expressionStatements = new List<StatementSyntax>();
                    var list = new List<VariableDeclaratorSyntax>();
                    var triviaList = new List<SyntaxTrivia>();

                    // When we modify the declaration to an initialization we have to preserve the leading trivia
                    var firstVariableToAttachTrivia = true;

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
                                expressionStatements.Add(CreateAssignmentExpressionStatement(identifier, variableDeclaration.Initializer.Value));
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
                            list.Add(variableDeclaration.WithPrependedLeadingTrivia(triviaList));
                            triviaList.Clear();
                            firstVariableToAttachTrivia = false;
                            continue;
                        }

                        firstVariableToAttachTrivia = false;
                        list.Add(variableDeclaration);
                    }

                    if (list.Count == 0 && triviaList.Count > 0)
                    {
                        // well, there are trivia associated with the node.
                        // we can't just delete the node since then, we will lose
                        // the trivia. unfortunately, it is not easy to attach the trivia
                        // to next token. for now, create an empty statement and associate the
                        // trivia to the statement

                        // TODO : think about a way to trivia attached to next token
                        yield return SyntaxFactory.EmptyStatement(SyntaxFactory.Token(SyntaxFactory.TriviaList(triviaList), SyntaxKind.SemicolonToken, SyntaxTriviaList.Create(SyntaxFactory.ElasticMarker)));
                        triviaList.Clear();
                    }

                    // return survived var decls
                    if (list.Count > 0)
                    {
                        yield return SyntaxFactory.LocalDeclarationStatement(
                            declarationStatement.Modifiers,
                            SyntaxFactory.VariableDeclaration(
                                declarationStatement.Declaration.Type,
                                SyntaxFactory.SeparatedList(list)),
                            declarationStatement.SemicolonToken.WithPrependedLeadingTrivia(triviaList));
                        triviaList.Clear();
                    }

                    // return any expression statement if there was any
                    foreach (var expressionStatement in expressionStatements)
                    {
                        yield return expressionStatement;
                    }
                }
            }

            /// <summary>
            /// If the statement has an <c>out var</c> declaration expression for a variable which
            /// needs to be removed, we need to turn it into a plain <c>out</c> parameter, so that
            /// it doesn't declare a duplicate variable.
            /// If the statement has a pattern declaration (such as <c>3 is int i</c>) for a variable
            /// which needs to be removed, we will annotate it as a conflict, since we don't have
            /// a better refactoring.
            /// </summary>
            private StatementSyntax FixDeclarationExpressionsAndDeclarationPatterns(StatementSyntax statement,
                HashSet<SyntaxAnnotation> variablesToRemove)
            {
                var replacements = new Dictionary<SyntaxNode, SyntaxNode>();

                var declarations = statement.DescendantNodes()
                    .Where(n => n.IsKind(SyntaxKind.DeclarationExpression, SyntaxKind.DeclarationPattern));

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

                                    replacements.Add(declaration, SyntaxFactory.IdentifierName(designation.Identifier)
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
                                if (!(pattern.Designation is SingleVariableDesignationSyntax designation))
                                {
                                    break;
                                }

                                var identifier = designation.Identifier;
                                var annotation = ConflictAnnotation.Create(CSharpFeaturesResources.Conflict_s_detected);
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

            private IEnumerable<StatementSyntax> SplitOrMoveDeclarationIntoMethodDefinition(
                IEnumerable<StatementSyntax> statements,
                CancellationToken cancellationToken)
            {
                var semanticModel = this.SemanticDocument.SemanticModel;
                var context = this.InsertionPoint.GetContext();
                var postProcessor = new PostProcessor(semanticModel, context.SpanStart);

                var declStatements = CreateDeclarationStatements(AnalyzerResult.GetVariablesToSplitOrMoveIntoMethodDefinition(cancellationToken), cancellationToken);
                declStatements = postProcessor.MergeDeclarationStatements(declStatements);

                return declStatements.Concat(statements);
            }

            private ExpressionSyntax CreateAssignmentExpression(SyntaxToken identifier, ExpressionSyntax rvalue)
            {
                return SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(identifier),
                    rvalue);
            }

            protected override bool LastStatementOrHasReturnStatementInReturnableConstruct()
            {
                var lastStatement = this.GetLastStatementOrInitializerSelectedAtCallSite();
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

            protected override SyntaxToken CreateIdentifier(string name)
            {
                return SyntaxFactory.Identifier(name);
            }

            protected override StatementSyntax CreateReturnStatement(string identifierName = null)
            {
                return string.IsNullOrEmpty(identifierName)
                    ? SyntaxFactory.ReturnStatement()
                    : SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(identifierName));
            }

            protected override ExpressionSyntax CreateCallSignature()
            {
                var methodName = CreateMethodNameForInvocation().WithAdditionalAnnotations(Simplifier.Annotation);

                var arguments = new List<ArgumentSyntax>();
                foreach (var argument in this.AnalyzerResult.MethodParameters)
                {
                    var modifier = GetParameterRefSyntaxKind(argument.ParameterModifier);
                    var refOrOut = modifier == SyntaxKind.None ? default : SyntaxFactory.Token(modifier);

                    arguments.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(argument.Name)).WithRefOrOutKeyword(refOrOut));
                }

                var invocation = SyntaxFactory.InvocationExpression(methodName,
                                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));

                var shouldPutAsyncModifier = this.CSharpSelectionResult.ShouldPutAsyncModifier();
                if (!shouldPutAsyncModifier)
                {
                    return invocation;
                }

                return SyntaxFactory.AwaitExpression(invocation);
            }

            protected override StatementSyntax CreateAssignmentExpressionStatement(SyntaxToken identifier, ExpressionSyntax rvalue)
            {
                return SyntaxFactory.ExpressionStatement(CreateAssignmentExpression(identifier, rvalue));
            }

            protected override StatementSyntax CreateDeclarationStatement(
                VariableInfo variable,
                ExpressionSyntax initialValue,
                CancellationToken cancellationToken)
            {
                var type = variable.GetVariableType(this.SemanticDocument);
                var typeNode = type.GenerateTypeSyntax();

                var equalsValueClause = initialValue == null ? null : SyntaxFactory.EqualsValueClause(value: initialValue);

                return SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(typeNode)
                          .AddVariables(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(variable.Name)).WithInitializer(equalsValueClause)));
            }

            protected override async Task<GeneratedCode> CreateGeneratedCodeAsync(OperationStatus status, SemanticDocument newDocument, CancellationToken cancellationToken)
            {
                if (status.Succeeded())
                {
                    // in hybrid code cases such as extract method, formatter will have some difficulties on where it breaks lines in two.
                    // here, we explicitly insert newline at the end of "{" of auto generated method decl so that anchor knows how to find out
                    // indentation of inserted statements (from users code) with user code style preserved
                    var root = newDocument.Root;
                    var methodDefinition = root.GetAnnotatedNodes<MethodDeclarationSyntax>(this.MethodDefinitionAnnotation).First();

                    var newMethodDefinition = TweakNewLinesInMethod(methodDefinition);

                    newDocument = await newDocument.WithSyntaxRootAsync(
                        root.ReplaceNode(methodDefinition, newMethodDefinition), cancellationToken).ConfigureAwait(false);
                }

                return await base.CreateGeneratedCodeAsync(status, newDocument, cancellationToken).ConfigureAwait(false);
            }

            private static MethodDeclarationSyntax TweakNewLinesInMethod(MethodDeclarationSyntax methodDefinition)
            {
                if (methodDefinition.Body != null)
                {
                    return methodDefinition.ReplaceToken(
                            methodDefinition.Body.OpenBraceToken,
                            methodDefinition.Body.OpenBraceToken.WithAppendedTrailingTrivia(
                                SpecializedCollections.SingletonEnumerable(SyntaxFactory.ElasticCarriageReturnLineFeed)));
                }
                else if (methodDefinition.ExpressionBody != null)
                {
                    return methodDefinition.ReplaceToken(
                            methodDefinition.ExpressionBody.ArrowToken,
                            methodDefinition.ExpressionBody.ArrowToken.WithPrependedLeadingTrivia(
                                SpecializedCollections.SingletonEnumerable(SyntaxFactory.ElasticCarriageReturnLineFeed)));
                }
                else
                {
                    return methodDefinition;
                }
            }

            protected StatementSyntax GetStatementContainingInvocationToExtractedMethodWorker()
            {
                var callSignature = CreateCallSignature();

                if (this.AnalyzerResult.HasReturnType)
                {
                    Contract.ThrowIfTrue(this.AnalyzerResult.HasVariableToUseAsReturnValue);
                    return SyntaxFactory.ReturnStatement(callSignature);
                }

                return SyntaxFactory.ExpressionStatement(callSignature);
            }
        }
    }
}
