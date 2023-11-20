// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpMethodExtractor
    {
        private partial class CSharpCodeGenerator
        {
            public sealed class SingleStatementCodeGenerator(
                CSharpSelectionResult selectionResult,
                AnalyzerResult analyzerResult,
                CSharpCodeGenerationOptions options,
                bool localFunction) : CSharpCodeGenerator(selectionResult, analyzerResult, options, localFunction)
            {
                protected override SyntaxToken CreateMethodName() => GenerateMethodNameForStatementGenerators();

                protected override ImmutableArray<StatementSyntax> GetInitialStatementsForMethodDefinitions()
                {
                    Contract.ThrowIfFalse(this.SelectionResult.IsExtractMethodOnSingleStatement());

                    return ImmutableArray.Create(this.SelectionResult.GetFirstStatement());
                }

                protected override SyntaxNode GetFirstStatementOrInitializerSelectedAtCallSite()
                    => this.SelectionResult.GetFirstStatement();

                protected override SyntaxNode GetLastStatementOrInitializerSelectedAtCallSite()
                {
                    // it is a single statement case. either first statement is same as last statement or
                    // last statement belongs (embedded statement) to the first statement.
                    return this.SelectionResult.GetFirstStatement();
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
