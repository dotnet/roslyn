// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Wrapping.BinaryExpression;
using Microsoft.CodeAnalysis.CSharp.Wrapping.ChainedExpression;
using Microsoft.CodeAnalysis.CSharp.Wrapping.SeparatedSyntaxList;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Wrapping;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.Wrapping), Shared]
    internal class CSharpWrappingCodeRefactoringProvider : AbstractWrappingCodeRefactoringProvider
    {
        private static readonly ImmutableArray<ISyntaxWrapper> s_wrappers =
            ImmutableArray.Create<ISyntaxWrapper>(
                new CSharpArgumentWrapper(),
                new CSharpParameterWrapper(),
                new CSharpBinaryExpressionWrapper(),
                new CSharpChainedExpressionWrapper(),
                new CSharpInitializerExpressionWrapper());

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpWrappingCodeRefactoringProvider()
            : base(s_wrappers)
        {
        }

        protected override SyntaxWrappingOptions GetWrappingOptions(AnalyzerConfigOptions options, CodeActionOptions ideOptions)
            => CSharpSyntaxWrappingOptions.Create(options, ideOptions);
    }
}
