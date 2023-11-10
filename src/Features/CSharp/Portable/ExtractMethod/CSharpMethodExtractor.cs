// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpMethodExtractor(CSharpSelectionResult result, ExtractMethodGenerationOptions options, bool localFunction)
        : MethodExtractor<CSharpSelectionResult, StatementSyntax, ExpressionSyntax>(result, options, localFunction)
    {
        protected override CodeGenerator CreateCodeGenerator(AnalyzerResult analyzerResult)
            => CSharpCodeGenerator.Create(this.OriginalSelectionResult, analyzerResult, (CSharpCodeGenerationOptions)this.Options.CodeGenerationOptions, this.LocalFunction);

        protected override AnalyzerResult Analyze(CSharpSelectionResult selectionResult, bool localFunction, CancellationToken cancellationToken)
            => CSharpAnalyzer.Analyze(selectionResult, localFunction, cancellationToken);

        protected override SyntaxNode GetInsertionPointNode(
            AnalyzerResult analyzerResult, CancellationToken cancellationToken)
        {
            var originalSpanStart = OriginalSelectionResult.OriginalSpan.Start;
            Contract.ThrowIfFalse(originalSpanStart >= 0);

            var document = this.OriginalSelectionResult.SemanticDocument;
            var root = document.Root;

            if (LocalFunction)
            {
                // If we are extracting a local function and are within a local function, then we want the new function to be created within the
                // existing local function instead of the overarching method.
                var outermostCapturedVariable = analyzerResult.GetOutermostVariableToMoveIntoMethodDefinition(cancellationToken);
                var baseNode = outermostCapturedVariable != null
                    ? outermostCapturedVariable.GetIdentifierTokenAtDeclaration(document).Parent
                    : this.OriginalSelectionResult.GetOutermostCallSiteContainerToProcess(cancellationToken);

                if (baseNode is CompilationUnitSyntax)
                {
                    // In some sort of global statement.  Have to special case these a bit for script files.
                    var globalStatement = root.FindToken(originalSpanStart).GetAncestor<GlobalStatementSyntax>();
                    if (globalStatement is null)
                        return null;

                    return GetInsertionPointForGlobalStatement(globalStatement, globalStatement);
                }

                var currentNode = baseNode;
                while (currentNode is not null)
                {
                    if (currentNode is AnonymousFunctionExpressionSyntax anonymousFunction)
                    {
                        if (OriginalSelectionWithin(anonymousFunction.Body) || OriginalSelectionWithin(anonymousFunction.ExpressionBody))
                            return currentNode;

                        if (!OriginalSelectionResult.OriginalSpan.Contains(anonymousFunction.Span))
                        {
                            // If we encountered a function but the selection isn't within the body, it's likely the user
                            // is attempting to move the function (which is behavior that is supported). Stop looking up the 
                            // tree and assume the encapsulating member is the right place to put the local function. This is to help
                            // maintain the behavior introduced with https://github.com/dotnet/roslyn/pull/41377
                            break;
                        }
                    }

                    if (currentNode is LocalFunctionStatementSyntax localFunction)
                    {
                        if (OriginalSelectionWithin(localFunction.ExpressionBody) || OriginalSelectionWithin(localFunction.Body))
                            return currentNode;

                        if (!OriginalSelectionResult.OriginalSpan.Contains(localFunction.Span))
                        {
                            // If we encountered a function but the selection isn't within the body, it's likely the user
                            // is attempting to move the function (which is behavior that is supported). Stop looking up the 
                            // tree and assume the encapsulating member is the right place to put the local function. This is to help
                            // maintain the behavior introduced with https://github.com/dotnet/roslyn/pull/41377
                            break;
                        }
                    }

                    if (currentNode is AccessorDeclarationSyntax)
                        return currentNode;

                    if (currentNode is BaseMethodDeclarationSyntax)
                        return currentNode;

                    if (currentNode is GlobalStatementSyntax globalStatement)
                    {
                        // check whether the global statement is a statement container
                        if (!globalStatement.Statement.IsStatementContainerNode() && !root.SyntaxTree.IsScript())
                        {
                            // The extracted function will be a new global statement
                            return globalStatement.Parent;
                        }

                        return globalStatement.Statement;
                    }

                    currentNode = currentNode.Parent;
                }

                return null;
            }
            else
            {
                var baseToken = root.FindToken(originalSpanStart);
                var memberNode = baseToken.GetAncestor<MemberDeclarationSyntax>();
                Contract.ThrowIfNull(memberNode);
                Contract.ThrowIfTrue(memberNode.Kind() == SyntaxKind.NamespaceDeclaration);

                if (memberNode is GlobalStatementSyntax globalStatement)
                    return GetInsertionPointForGlobalStatement(globalStatement, memberNode);

                return memberNode;
            }

            SyntaxNode GetInsertionPointForGlobalStatement(GlobalStatementSyntax globalStatement, MemberDeclarationSyntax memberNode)
            {
                // check whether we are extracting whole global statement out
                if (OriginalSelectionResult.FinalSpan.Contains(memberNode.Span))
                    return globalStatement.Parent;

                // check whether the global statement is a statement container
                if (!globalStatement.Statement.IsStatementContainerNode() && !root.SyntaxTree.IsScript())
                {
                    // The extracted function will be a new global statement
                    return globalStatement.Parent;
                }

                return globalStatement.Statement;
            }
        }

        private bool OriginalSelectionWithin(SyntaxNode node)
        {
            if (node is null)
            {
                return false;
            }

            return node.Span.Contains(OriginalSelectionResult.OriginalSpan);
        }

        protected override async Task<TriviaResult> PreserveTriviaAsync(CSharpSelectionResult selectionResult, CancellationToken cancellationToken)
            => await CSharpTriviaResult.ProcessAsync(selectionResult, cancellationToken).ConfigureAwait(false);

        protected override Task<GeneratedCode> GenerateCodeAsync(InsertionPoint insertionPoint, CSharpSelectionResult selectionResult, AnalyzerResult analyzeResult, CodeGenerationOptions options, CancellationToken cancellationToken)
            => CSharpCodeGenerator.GenerateAsync(insertionPoint, selectionResult, analyzeResult, (CSharpCodeGenerationOptions)options, LocalFunction, cancellationToken);

        protected override AbstractFormattingRule GetCustomFormattingRule(Document document)
            => FormattingRule.Instance;

        protected override SyntaxToken? GetInvocationNameToken(IEnumerable<SyntaxToken> methodNames)
            => methodNames.FirstOrNull(t => !t.Parent.IsKind(SyntaxKind.MethodDeclaration));

        protected override SyntaxNode ParseTypeName(string name)
            => SyntaxFactory.ParseTypeName(name);

        protected override async Task<(Document document, SyntaxToken? invocationNameToken)> InsertNewLineBeforeLocalFunctionIfNecessaryAsync(
            Document document,
            SyntaxToken? invocationNameToken,
            SyntaxNode methodDefinition,
            CancellationToken cancellationToken)
        {
            // Checking to see if there is already an empty line before the local method declaration.
            var leadingTrivia = methodDefinition.GetLeadingTrivia();
            if (!leadingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) && !methodDefinition.FindTokenOnLeftOfPosition(methodDefinition.SpanStart).IsKind(SyntaxKind.OpenBraceToken))
            {
                var originalMethodDefinition = methodDefinition;
                var newLine = Options.LineFormattingOptions.NewLine;
                methodDefinition = methodDefinition.WithPrependedLeadingTrivia(SpecializedCollections.SingletonEnumerable(SyntaxFactory.EndOfLine(newLine)));

                if (!originalMethodDefinition.FindTokenOnLeftOfPosition(originalMethodDefinition.SpanStart).TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia))
                {
                    // Add a second new line since there were no line endings in the original form
                    methodDefinition = methodDefinition.WithPrependedLeadingTrivia(SpecializedCollections.SingletonEnumerable(SyntaxFactory.EndOfLine(newLine)));
                }

                // Generating the new document and associated variables.
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                document = document.WithSyntaxRoot(root.ReplaceNode(originalMethodDefinition, methodDefinition));

                var newRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                if (invocationNameToken != null)
                    invocationNameToken = newRoot.FindToken(invocationNameToken.Value.SpanStart);
            }

            return (document, invocationNameToken);
        }
    }
}
