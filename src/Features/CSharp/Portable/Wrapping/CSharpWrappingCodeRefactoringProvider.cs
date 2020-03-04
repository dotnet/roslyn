// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Wrapping.SeparatedSyntaxList;
using Microsoft.CodeAnalysis.CSharp.Wrapping.BinaryExpression;
using Microsoft.CodeAnalysis.CSharp.Wrapping.ChainedExpression;
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
                new CSharpChainedExpressionWrapper());

        [ImportingConstructor]
        public CSharpWrappingCodeRefactoringProvider()
            : base(s_wrappers)
        {
        }
    }
}
