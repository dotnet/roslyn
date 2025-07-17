// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod;

internal sealed partial class CSharpExtractMethodService
{
    internal sealed partial class CSharpMethodExtractor(
        SelectionResult result, ExtractMethodGenerationOptions options, bool localFunction)
        : MethodExtractor(result, options, localFunction)
    {
        protected override CodeGenerator CreateCodeGenerator(SelectionResult selectionResult, AnalyzerResult analyzerResult)
            => CSharpCodeGenerator.Create(selectionResult, analyzerResult, this.Options, this.LocalFunction);

        protected override AnalyzerResult Analyze(CancellationToken cancellationToken)
        {
            var analyzer = new CSharpAnalyzer(this.OriginalSelectionResult, this.LocalFunction, cancellationToken);
            return analyzer.Analyze();
        }

        protected override SyntaxNode GetInsertionPointNode(
            AnalyzerResult analyzerResult, CancellationToken cancellationToken)
        {
            var originalSpanStart = OriginalSelectionResult.FinalSpan.Start;
            Contract.ThrowIfFalse(originalSpanStart >= 0);

            var document = this.OriginalSelectionResult.SemanticDocument;
            var root = document.Root;

            if (LocalFunction)
            {
                // If we are extracting a local function and are within a local function, then we want the new function to be created within the
                // existing local function instead of the overarching method.
                var outermostCapturedVariable = analyzerResult.GetOutermostVariableToMoveIntoMethodDefinition();
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
                        if (SelectionWithin(anonymousFunction.Body) || SelectionWithin(anonymousFunction.ExpressionBody))
                            return currentNode;

                        if (!OriginalSelectionResult.FinalSpan.Contains(anonymousFunction.Span))
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
                        if (SelectionWithin(localFunction.ExpressionBody) || SelectionWithin(localFunction.Body))
                            return currentNode;

                        if (!OriginalSelectionResult.FinalSpan.Contains(localFunction.Span))
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
                var primaryConstructorBaseType = baseToken.GetAncestor<PrimaryConstructorBaseTypeSyntax>();
                if (primaryConstructorBaseType != null)
                    return primaryConstructorBaseType;

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

        private bool SelectionWithin(SyntaxNode node)
        {
            if (node is null)
            {
                return false;
            }

            return node.Span.Contains(OriginalSelectionResult.FinalSpan);
        }

        protected override async Task<TriviaResult> PreserveTriviaAsync(SyntaxNode root, CancellationToken cancellationToken)
        {
            var semanticDocument = this.OriginalSelectionResult.SemanticDocument;
            var preservationService = semanticDocument.Document.Project.Services.GetService<ISyntaxTriviaService>();
            var result = preservationService.SaveTriviaAroundSelection(root, this.OriginalSelectionResult.FinalSpan);
            return new CSharpTriviaResult(
                await semanticDocument.WithSyntaxRootAsync(result.Root, cancellationToken).ConfigureAwait(false),
                result);
        }

        protected override AbstractFormattingRule GetCustomFormattingRule(Document document)
            => FormattingRule.Instance;

        protected override SyntaxNode ParseTypeName(string name)
            => SyntaxFactory.ParseTypeName(name);

        protected override async Task<(Document document, SyntaxToken invocationNameToken)> InsertNewLineBeforeLocalFunctionIfNecessaryAsync(
            Document document,
            SyntaxToken invocationNameToken,
            SyntaxNode methodDefinition,
            CancellationToken cancellationToken)
        {
            // Checking to see if there is already an empty line before the local method declaration.
            var leadingTrivia = methodDefinition.GetLeadingTrivia();
            if (!leadingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia) || t.GetStructure() is EndIfDirectiveTriviaSyntax) &&
                !methodDefinition.FindTokenOnLeftOfPosition(methodDefinition.SpanStart).IsKind(SyntaxKind.OpenBraceToken))
            {
                var originalMethodDefinition = methodDefinition;
                var newLine = Options.LineFormattingOptions.NewLine;
                methodDefinition = methodDefinition.WithPrependedLeadingTrivia(SyntaxFactory.EndOfLine(newLine));

                if (!originalMethodDefinition.FindTokenOnLeftOfPosition(originalMethodDefinition.SpanStart).TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia))
                {
                    // Add a second new line since there were no line endings in the original form
                    methodDefinition = methodDefinition.WithPrependedLeadingTrivia(SyntaxFactory.EndOfLine(newLine));
                }

                // Generating the new document and associated variables.
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                document = document.WithSyntaxRoot(root.ReplaceNode(originalMethodDefinition, methodDefinition));

                var newRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                invocationNameToken = newRoot.FindToken(invocationNameToken.SpanStart);
            }

            return (document, invocationNameToken);
        }
    }
}
