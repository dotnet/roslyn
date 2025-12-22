// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod;

internal sealed partial class CSharpExtractMethodService
{
    internal sealed partial class CSharpMethodExtractor
    {
        private abstract partial class CSharpCodeGenerator
        {
            public sealed class MultipleStatementsCodeGenerator(
                SelectionResult selectionResult,
                AnalyzerResult analyzerResult,
                ExtractMethodGenerationOptions options,
                bool localFunction) : CSharpCodeGenerator(selectionResult, analyzerResult, options, localFunction)
            {
                protected override SyntaxToken CreateMethodName()
                    => GenerateMethodNameForStatementGenerators();

                protected override ImmutableArray<StatementSyntax> GetInitialStatementsForMethodDefinitions()
                {
                    var firstSeen = false;
                    var firstStatementUnderContainer = this.SelectionResult.GetFirstStatementUnderContainer();
                    var lastStatementUnderContainer = this.SelectionResult.GetLastStatementUnderContainer();

                    using var _ = ArrayBuilder<StatementSyntax>.GetInstance(out var list);
                    foreach (var statement in GetStatementsFromContainer(firstStatementUnderContainer.Parent))
                    {
                        // reset first seen
                        if (!firstSeen)
                        {
                            firstSeen = statement == firstStatementUnderContainer;
                        }

                        // continue until we see the first statement
                        if (!firstSeen)
                        {
                            continue;
                        }

                        list.Add(statement);

                        // exit if we see last statement
                        if (statement == lastStatementUnderContainer)
                        {
                            break;
                        }
                    }

                    return list.ToImmutableAndClear();
                }

                private static IEnumerable<StatementSyntax> GetStatementsFromContainer(SyntaxNode node)
                {
                    Contract.ThrowIfNull(node);
                    Contract.ThrowIfFalse(node.IsStatementContainerNode());

                    return node switch
                    {
                        BlockSyntax blockNode => blockNode.Statements,
                        SwitchSectionSyntax switchSectionNode => switchSectionNode.Statements,
                        GlobalStatementSyntax globalStatement => ((CompilationUnitSyntax)globalStatement.Parent).Members.OfType<GlobalStatementSyntax>().Select(globalStatement => globalStatement.Statement),
                        _ => throw ExceptionUtilities.UnexpectedValue(node),
                    };
                }

                protected override SyntaxNode GetFirstStatementOrInitializerSelectedAtCallSite()
                    => this.SelectionResult.GetFirstStatementUnderContainer();

                protected override SyntaxNode GetLastStatementOrInitializerSelectedAtCallSite()
                    => this.SelectionResult.GetLastStatementUnderContainer();

                protected override async Task<SyntaxNode> GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(CancellationToken cancellationToken)
                {
                    var statement = GetStatementContainingInvocationToExtractedMethodWorker();
                    return statement.WithAdditionalAnnotations(CallSiteAnnotation);
                }
            }
        }
    }
}
