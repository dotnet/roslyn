// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpMethodExtractor
    {
        private partial class CSharpCodeGenerator
        {
            public class MultipleStatementsCodeGenerator : CSharpCodeGenerator
            {
                public MultipleStatementsCodeGenerator(
                    InsertionPoint insertionPoint,
                    SelectionResult selectionResult,
                    AnalyzerResult analyzerResult,
                    OptionSet options,
                    bool localFunction)
                    : base(insertionPoint, selectionResult, analyzerResult, options, localFunction)
                {
                }

                public static bool IsExtractMethodOnMultipleStatements(SelectionResult code)
                {
                    var result = (CSharpSelectionResult)code;
                    var first = result.GetFirstStatement();
                    var last = result.GetLastStatement();

                    if (first != last)
                    {
                        var firstUnderContainer = result.GetFirstStatementUnderContainer();
                        var lastUnderContainer = result.GetLastStatementUnderContainer();
                        Contract.ThrowIfFalse(firstUnderContainer.Parent == lastUnderContainer.Parent);
                        return true;
                    }

                    return false;
                }

                protected override SyntaxToken CreateMethodName() => GenerateMethodNameForStatementGenerators();

                protected override ImmutableArray<StatementSyntax> GetInitialStatementsForMethodDefinitions()
                {
                    var firstSeen = false;
                    var firstStatementUnderContainer = CSharpSelectionResult.GetFirstStatementUnderContainer();
                    var lastStatementUnderContainer = CSharpSelectionResult.GetLastStatementUnderContainer();

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

                    return list.ToImmutable();
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
                        var firstStatement = CSharpSelectionResult.GetFirstStatementUnderContainer();
                        return firstStatement.Parent;
                    }
                }

                private static SyntaxList<StatementSyntax> GetStatementsFromContainer(SyntaxNode node)
                {
                    Contract.ThrowIfNull(node);
                    Contract.ThrowIfFalse(node.IsStatementContainerNode());

                    return node switch
                    {
                        BlockSyntax blockNode => blockNode.Statements,
                        SwitchSectionSyntax switchSectionNode => switchSectionNode.Statements,
                        _ => throw ExceptionUtilities.UnexpectedValue(node),
                    };
                }

                protected override SyntaxNode GetFirstStatementOrInitializerSelectedAtCallSite()
                    => CSharpSelectionResult.GetFirstStatementUnderContainer();

                protected override SyntaxNode GetLastStatementOrInitializerSelectedAtCallSite()
                    => CSharpSelectionResult.GetLastStatementUnderContainer();

                protected override Task<SyntaxNode> GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(CancellationToken cancellationToken)
                {
                    var statement = GetStatementContainingInvocationToExtractedMethodWorker();
                    return Task.FromResult<SyntaxNode>(statement.WithAdditionalAnnotations(CallSiteAnnotation));
                }
            }
        }
    }
}
