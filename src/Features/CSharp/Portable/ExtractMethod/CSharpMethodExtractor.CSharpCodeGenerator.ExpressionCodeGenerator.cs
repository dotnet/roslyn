// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpMethodExtractor
    {
        private partial class CSharpCodeGenerator
        {
            private sealed class ExpressionCodeGenerator : CSharpCodeGenerator
            {
                public ExpressionCodeGenerator(
                    InsertionPoint insertionPoint,
                    SelectionResult selectionResult,
                    AnalyzerResult analyzerResult,
                    CSharpCodeGenerationOptions options,
                    NamingStylePreferencesProvider namingPreferences,
                    bool localFunction)
                    : base(insertionPoint, selectionResult, analyzerResult, options, namingPreferences, localFunction)
                {
                }

                public static bool IsExtractMethodOnExpression(SelectionResult code)
                    => code.SelectionInExpression;

                protected override SyntaxToken CreateMethodName()
                {
                    var methodName = GenerateMethodNameFromUserPreference();

                    var containingScope = CSharpSelectionResult.GetContainingScope();

                    methodName = GetMethodNameBasedOnExpression(methodName, containingScope);

                    var semanticModel = SemanticDocument.SemanticModel;
                    var nameGenerator = new UniqueNameGenerator(semanticModel);
                    return SyntaxFactory.Identifier(nameGenerator.CreateUniqueMethodName(containingScope, methodName));
                }

                private static string GetMethodNameBasedOnExpression(string methodName, SyntaxNode expression)
                {
                    if (expression.Parent != null &&
                        expression.Parent.Kind() == SyntaxKind.EqualsValueClause &&
                        expression.Parent.Parent != null &&
                        expression.Parent.Parent.Kind() == SyntaxKind.VariableDeclarator)
                    {
                        var name = ((VariableDeclaratorSyntax)expression.Parent.Parent).Identifier.ValueText;
                        return (name != null && name.Length > 0) ? MakeMethodName("Get", name, methodName.Equals(NewMethodCamelCaseStr)) : methodName;
                    }

                    if (expression is MemberAccessExpressionSyntax memberAccess)
                    {
                        expression = memberAccess.Name;
                    }

                    if (expression is NameSyntax)
                    {
                        SimpleNameSyntax unqualifiedName;

                        switch (expression.Kind())
                        {
                            case SyntaxKind.IdentifierName:
                            case SyntaxKind.GenericName:
                                unqualifiedName = (SimpleNameSyntax)expression;
                                break;
                            case SyntaxKind.QualifiedName:
                                unqualifiedName = ((QualifiedNameSyntax)expression).Right;
                                break;
                            case SyntaxKind.AliasQualifiedName:
                                unqualifiedName = ((AliasQualifiedNameSyntax)expression).Name;
                                break;
                            default:
                                throw new System.NotSupportedException("Unexpected name kind: " + expression.Kind().ToString());
                        }

                        var unqualifiedNameIdentifierValueText = unqualifiedName.Identifier.ValueText;
                        return (unqualifiedNameIdentifierValueText != null && unqualifiedNameIdentifierValueText.Length > 0) ?
                            MakeMethodName("Get", unqualifiedNameIdentifierValueText, methodName.Equals(NewMethodCamelCaseStr)) : methodName;
                    }

                    return methodName;
                }

                protected override ImmutableArray<StatementSyntax> GetInitialStatementsForMethodDefinitions()
                {
                    Contract.ThrowIfFalse(IsExtractMethodOnExpression(CSharpSelectionResult));

                    // special case for array initializer
                    var returnType = AnalyzerResult.ReturnType;
                    var containingScope = CSharpSelectionResult.GetContainingScope();

                    ExpressionSyntax expression;
                    if (returnType.TypeKind == TypeKind.Array && containingScope is InitializerExpressionSyntax)
                    {
                        var typeSyntax = returnType.GenerateTypeSyntax();

                        expression = SyntaxFactory.ArrayCreationExpression(typeSyntax as ArrayTypeSyntax, containingScope as InitializerExpressionSyntax);
                    }
                    else
                    {
                        expression = containingScope as ExpressionSyntax;
                    }

                    if (AnalyzerResult.HasReturnType)
                    {
                        return ImmutableArray.Create<StatementSyntax>(
                            SyntaxFactory.ReturnStatement(
                                WrapInCheckedExpressionIfNeeded(expression)));
                    }
                    else
                    {
                        return ImmutableArray.Create<StatementSyntax>(
                            SyntaxFactory.ExpressionStatement(
                                WrapInCheckedExpressionIfNeeded(expression)));
                    }
                }

                private ExpressionSyntax WrapInCheckedExpressionIfNeeded(ExpressionSyntax expression)
                {
                    var kind = CSharpSelectionResult.UnderCheckedExpressionContext();
                    if (kind == SyntaxKind.None)
                    {
                        return expression;
                    }

                    return SyntaxFactory.CheckedExpression(kind, expression);
                }

                protected override SyntaxNode GetOutermostCallSiteContainerToProcess(CancellationToken cancellationToken)
                {
                    var callSiteContainer = GetCallSiteContainerFromOutermostMoveInVariable(cancellationToken);
                    if (callSiteContainer != null)
                    {
                        return callSiteContainer;
                    }
                    else
                    {
                        return GetCallSiteContainerFromExpression();
                    }
                }

                private SyntaxNode GetCallSiteContainerFromExpression()
                {
                    var container = CSharpSelectionResult.GetInnermostStatementContainer();

                    Contract.ThrowIfNull(container);
                    Contract.ThrowIfFalse(container.IsStatementContainerNode() ||
                                          container is TypeDeclarationSyntax ||
                                          container is ConstructorDeclarationSyntax ||
                                          container is CompilationUnitSyntax);

                    return container;
                }

                protected override SyntaxNode GetFirstStatementOrInitializerSelectedAtCallSite()
                {
                    var scope = (SyntaxNode)CSharpSelectionResult.GetContainingScopeOf<StatementSyntax>();
                    if (scope == null)
                    {
                        scope = CSharpSelectionResult.GetContainingScopeOf<FieldDeclarationSyntax>();
                    }

                    if (scope == null)
                    {
                        scope = CSharpSelectionResult.GetContainingScopeOf<ConstructorInitializerSyntax>();
                    }

                    if (scope == null)
                    {
                        // This is similar to FieldDeclaration case but we only want to do this 
                        // if the member has an expression body.
                        scope = CSharpSelectionResult.GetContainingScopeOf<ArrowExpressionClauseSyntax>().Parent;
                    }

                    return scope;
                }

                protected override SyntaxNode GetLastStatementOrInitializerSelectedAtCallSite()
                    => GetFirstStatementOrInitializerSelectedAtCallSite();

                protected override async Task<SyntaxNode> GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(CancellationToken cancellationToken)
                {
                    var enclosingStatement = GetFirstStatementOrInitializerSelectedAtCallSite();

                    var callSignature = CreateCallSignature().WithAdditionalAnnotations(CallSiteAnnotation);

                    var sourceNode = CSharpSelectionResult.GetContainingScope();
                    Contract.ThrowIfTrue(
                        sourceNode.Parent is MemberAccessExpressionSyntax && ((MemberAccessExpressionSyntax)sourceNode.Parent).Name == sourceNode,
                        "invalid scope. given scope is not an expression");

                    // To lower the chances that replacing sourceNode with callSignature will break the user's
                    // code, we make the enclosing statement semantically explicit. This ends up being a little
                    // bit more work because we need to annotate the sourceNode so that we can get back to it
                    // after rewriting the enclosing statement.
                    var updatedDocument = SemanticDocument.Document;
                    var sourceNodeAnnotation = new SyntaxAnnotation();
                    var enclosingStatementAnnotation = new SyntaxAnnotation();
                    var newEnclosingStatement = enclosingStatement
                        .ReplaceNode(sourceNode, sourceNode.WithAdditionalAnnotations(sourceNodeAnnotation))
                        .WithAdditionalAnnotations(enclosingStatementAnnotation);

                    updatedDocument = await updatedDocument.ReplaceNodeAsync(enclosingStatement, newEnclosingStatement, cancellationToken).ConfigureAwait(false);

                    var updatedRoot = await updatedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    newEnclosingStatement = updatedRoot.GetAnnotatedNodesAndTokens(enclosingStatementAnnotation).Single().AsNode();

                    // because of the complexification we cannot guarantee that there is only one annotation.
                    // however complexification of names is prepended, so the last annotation should be the original one.
                    sourceNode = updatedRoot.GetAnnotatedNodesAndTokens(sourceNodeAnnotation).Last().AsNode();

                    return newEnclosingStatement.ReplaceNode(sourceNode, callSignature);
                }
            }
        }
    }
}
