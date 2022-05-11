// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpMethodExtractor
    {
        private partial class CSharpCodeGenerator
        {
            public sealed class SingleStatementCodeGenerator : CSharpCodeGenerator
            {
                public SingleStatementCodeGenerator(
                    InsertionPoint insertionPoint,
                    SelectionResult selectionResult,
                    AnalyzerResult analyzerResult,
                    CSharpCodeGenerationOptions options,
                    NamingStylePreferencesProvider namingPreferences,
                    bool localFunction)
                    : base(insertionPoint, selectionResult, analyzerResult, options, namingPreferences, localFunction)
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

                protected override ImmutableArray<StatementSyntax> GetInitialStatementsForMethodDefinitions()
                {
                    Contract.ThrowIfFalse(IsExtractMethodOnSingleStatement(CSharpSelectionResult));

                    return ImmutableArray.Create(CSharpSelectionResult.GetFirstStatement());
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
                        var firstStatement = CSharpSelectionResult.GetFirstStatement();
                        return firstStatement.Parent;
                    }
                }

                protected override SyntaxNode GetFirstStatementOrInitializerSelectedAtCallSite()
                    => CSharpSelectionResult.GetFirstStatement();

                protected override SyntaxNode GetLastStatementOrInitializerSelectedAtCallSite()
                {
                    // it is a single statement case. either first statement is same as last statement or
                    // last statement belongs (embedded statement) to the first statement.
                    return CSharpSelectionResult.GetFirstStatement();
                }

                protected override Task<SyntaxNode> GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(CancellationToken cancellationToken)
                {
                    var statement = GetStatementContainingInvocationToExtractedMethodWorker();
                    return Task.FromResult<SyntaxNode>(statement.WithAdditionalAnnotations(CallSiteAnnotation));
                }
            }
        }
    }
}
