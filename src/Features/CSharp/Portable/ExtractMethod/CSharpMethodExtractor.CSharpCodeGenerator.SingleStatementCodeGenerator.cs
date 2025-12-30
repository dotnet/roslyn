// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod;

internal sealed partial class CSharpExtractMethodService
{
    internal sealed partial class CSharpMethodExtractor
    {
        private abstract partial class CSharpCodeGenerator
        {
            public sealed class SingleStatementCodeGenerator(
                SelectionResult selectionResult,
                AnalyzerResult analyzerResult,
                ExtractMethodGenerationOptions options,
                bool localFunction) : CSharpCodeGenerator(selectionResult, analyzerResult, options, localFunction)
            {
                protected override SyntaxToken CreateMethodName()
                    => GenerateMethodNameForStatementGenerators();

                protected override ImmutableArray<StatementSyntax> GetInitialStatementsForMethodDefinitions()
                {
                    Contract.ThrowIfFalse(this.SelectionResult.IsExtractMethodOnSingleStatement);

                    return [this.SelectionResult.GetFirstStatement()];
                }

                protected override SyntaxNode GetFirstStatementOrInitializerSelectedAtCallSite()
                    => this.SelectionResult.GetFirstStatement();

                protected override SyntaxNode GetLastStatementOrInitializerSelectedAtCallSite()
                {
                    // it is a single statement case. either first statement is same as last statement or
                    // last statement belongs (embedded statement) to the first statement.
                    return this.SelectionResult.GetFirstStatement();
                }

                protected override async Task<SyntaxNode> GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(CancellationToken cancellationToken)
                {
                    var statement = GetStatementContainingInvocationToExtractedMethodWorker();
                    return statement.WithAdditionalAnnotations(CallSiteAnnotation);
                }
            }
        }
    }
}
