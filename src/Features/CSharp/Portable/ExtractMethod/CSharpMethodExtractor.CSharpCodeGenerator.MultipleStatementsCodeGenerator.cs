// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
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
                    bool extractLocalFunction)
                    : base(insertionPoint, selectionResult, analyzerResult, extractLocalFunction)
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

                protected override SyntaxToken CreateMethodName()
                {
                    // change this to more smarter one.
                    var semanticModel = this.SemanticDocument.SemanticModel;
                    var nameGenerator = new UniqueNameGenerator(semanticModel);
                    var scope = this.CSharpSelectionResult.GetContainingScope();

                    return SyntaxFactory.Identifier(nameGenerator.CreateUniqueMethodName(scope, "NewMethod", GetLocalFunctionNamesIfScopeIsMethod(scope)));
                }

                protected override IEnumerable<StatementSyntax> GetInitialStatementsForMethodDefinitions()
                {
                    var firstSeen = false;
                    var firstStatementUnderContainer = this.CSharpSelectionResult.GetFirstStatementUnderContainer();
                    var lastStatementUnderContainer = this.CSharpSelectionResult.GetLastStatementUnderContainer();

                    var list = new List<StatementSyntax>();
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

                    return list;
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
                        var firstStatement = this.CSharpSelectionResult.GetFirstStatementUnderContainer();
                        return firstStatement.Parent;
                    }
                }

                private SyntaxList<StatementSyntax> GetStatementsFromContainer(SyntaxNode node)
                {
                    Contract.ThrowIfNull(node);
                    Contract.ThrowIfFalse(node.IsStatementContainerNode());

                    return node switch
                    {
                        BlockSyntax blockNode => blockNode.Statements,
                        SwitchSectionSyntax switchSectionNode => switchSectionNode.Statements,
                        _ => Contract.FailWithReturn<SyntaxList<StatementSyntax>>("unknown statements container!"),
                    };
                }

                protected override SyntaxNode GetFirstStatementOrInitializerSelectedAtCallSite()
                {
                    return this.CSharpSelectionResult.GetFirstStatementUnderContainer();
                }

                protected override SyntaxNode GetLastStatementOrInitializerSelectedAtCallSite()
                {
                    return this.CSharpSelectionResult.GetLastStatementUnderContainer();
                }

                protected override Task<SyntaxNode> GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(
                    SyntaxAnnotation callSiteAnnotation, CancellationToken cancellationToken)
                {
                    var statement = GetStatementContainingInvocationToExtractedMethodWorker();
                    return Task.FromResult<SyntaxNode>(statement.WithAdditionalAnnotations(callSiteAnnotation));
                }
            }
        }
    }
}
