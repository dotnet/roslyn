﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpMethodExtractor
    {
        private partial class CSharpCodeGenerator
        {
            public class SingleStatementCodeGenerator : CSharpCodeGenerator
            {
                public SingleStatementCodeGenerator(
                    InsertionPoint insertionPoint,
                    SelectionResult selectionResult,
                    AnalyzerResult analyzerResult,
                    OptionSet options,
                    bool localFunction)
                    : base(insertionPoint, selectionResult, analyzerResult, options, localFunction)
                {
                }

                public static bool IsExtractMethodOnSingleStatement(SelectionResult code)
                {
                    var result = (CSharpSelectionResult)code;
                    var firstStatement = result.GetFirstStatement();
                    var lastStatement = result.GetLastStatement();

                    return firstStatement == lastStatement || firstStatement.Span.Contains(lastStatement.Span);
                }

                protected override SyntaxToken CreateMethodName() => GenerateMethodNameForStatementGenerators();

                protected override IEnumerable<StatementSyntax> GetInitialStatementsForMethodDefinitions()
                {
                    Contract.ThrowIfFalse(IsExtractMethodOnSingleStatement(this.CSharpSelectionResult));

                    return SpecializedCollections.SingletonEnumerable<StatementSyntax>(this.CSharpSelectionResult.GetFirstStatement());
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
                        var firstStatement = this.CSharpSelectionResult.GetFirstStatement();
                        return firstStatement.Parent;
                    }
                }

                protected override SyntaxNode GetFirstStatementOrInitializerSelectedAtCallSite()
                {
                    return this.CSharpSelectionResult.GetFirstStatement();
                }

                protected override SyntaxNode GetLastStatementOrInitializerSelectedAtCallSite()
                {
                    // it is a single statement case. either first statement is same as last statement or
                    // last statement belongs (embedded statement) to the first statement.
                    return this.CSharpSelectionResult.GetFirstStatement();
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
